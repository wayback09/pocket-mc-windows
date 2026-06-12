using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

namespace PocketMC.Desktop.Features.Shell;

/// <summary>
/// Detects Windows version via the registry and applies rounded-corner clipping
/// to windows running on Windows 10 (build &lt; 22000).
///
/// On Windows 11 the DWM provides native rounded corners, so this service
/// is a deliberate no-op — the existing <c>WindowCornerPreference="Round"</c>
/// attribute on <see cref="Wpf.Ui.Controls.FluentWindow"/> is sufficient.
///
/// On Windows 10 we apply two complementary clips:
/// <list type="number">
///   <item>
///     <description>
///       A Win32 <c>SetWindowRgn</c> rounded-rectangle region that physically
///       removes corner pixels from the HWND — this eliminates the white
///       background that would otherwise peek through rounded WPF content.
///     </description>
///   </item>
///   <item>
///     <description>
///       A WPF <see cref="RectangleGeometry"/> clip on the root visual for
///       smooth, anti-aliased rendering within the window.
///     </description>
///   </item>
/// </list>
///
/// This avoids <c>AllowsTransparency = true</c> (which disables hardware
/// acceleration) and avoids replacing the FluentWindow template (which would
/// break the title-bar, navigation, backdrop layers, etc.).
///
/// Call <see cref="RegisterGlobalWindowHook"/> once at app startup to
/// automatically apply rounding to every window (dialogs included).
/// </summary>
public sealed class WindowsCornerService
{
    public const int Windows11MinimumBuild = 22000;
    public const double Windows10CornerRadius = 12;

    private const string CurrentVersionRegistryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
    private const string CurrentBuildNumberValueName = "CurrentBuildNumber";

    /// <summary>
    /// Tag value stored on the root visual to prevent double-attaching
    /// the clip handlers.
    /// </summary>
    private const string ClipTag = "Win10RoundedClip";

    private readonly Lazy<int?> _currentBuildNumber;

    // ── Win32 interop ───────────────────────────────────────────────────

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, [MarshalAs(UnmanagedType.Bool)] bool bRedraw);

    // ── Constructors ────────────────────────────────────────────────────

    public WindowsCornerService()
        : this(ReadCurrentBuildNumberFromRegistry)
    {
    }

    internal WindowsCornerService(Func<int?> currentBuildNumberProvider)
    {
        ArgumentNullException.ThrowIfNull(currentBuildNumberProvider);
        _currentBuildNumber = new Lazy<int?>(currentBuildNumberProvider);
    }

    // ── Public API ──────────────────────────────────────────────────────

    public bool IsWindows10()
    {
        int? build = _currentBuildNumber.Value;
        return build.HasValue && build.Value < Windows11MinimumBuild;
    }

    public bool IsWindows11()
    {
        int? build = _currentBuildNumber.Value;
        return build.HasValue && build.Value >= Windows11MinimumBuild;
    }

    /// <summary>
    /// Registers a class-level <see cref="FrameworkElement.LoadedEvent"/>
    /// handler on <see cref="Window"/> so that <em>every</em> window in the
    /// application (main window, dialogs, popups) automatically receives
    /// rounded-corner clipping on Windows 10.
    ///
    /// On Windows 11 this method is a no-op.
    /// Call once during <see cref="Application.OnStartup"/>.
    /// </summary>
    public void RegisterGlobalWindowHook()
    {
        if (!IsWindows10())
        {
            return;
        }

        EventManager.RegisterClassHandler(
            typeof(Window),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnAnyWindowLoaded));
    }

    /// <summary>
    /// Manually applies Windows 10 rounded corners to a single window.
    /// Prefer <see cref="RegisterGlobalWindowHook"/> for app-wide coverage;
    /// use this only if you need to apply clipping before the
    /// <see cref="FrameworkElement.LoadedEvent"/> fires.
    ///
    /// On Windows 11 this method is a no-op.
    /// </summary>
    public void ApplyWindows10RoundedCorners(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (!IsWindows10())
        {
            return;
        }

        if (!window.Dispatcher.CheckAccess())
        {
            window.Dispatcher.Invoke(() => ApplyWindows10RoundedCorners(window));
            return;
        }

        if (window.IsLoaded)
        {
            AttachClipping(window);
        }
        else
        {
            // The global hook will also fire on Loaded, but AttachClipping
            // guards against double-attach so this is safe.
            window.Loaded += OnSingleWindowLoaded;
        }
    }

    // ── Event handlers ──────────────────────────────────────────────────

    private static void OnAnyWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Window window)
        {
            AttachClipping(window);
        }
    }

    private static void OnSingleWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Window window) return;

        window.Loaded -= OnSingleWindowLoaded;
        AttachClipping(window);
    }

    /// <summary>
    /// Finds the root visual element of the window's template and applies
    /// both a Win32 window region (to eliminate white corner artifacts) and
    /// a WPF <see cref="RectangleGeometry"/> clip (for anti-aliased rendering).
    /// Subscribes to <see cref="FrameworkElement.SizeChanged"/> and
    /// <see cref="Window.StateChanged"/> to keep both clips in sync.
    /// </summary>
    private static void AttachClipping(Window window)
    {
        if (VisualTreeHelper.GetChildrenCount(window) == 0) return;

        var rootVisual = VisualTreeHelper.GetChild(window, 0) as UIElement;
        if (rootVisual == null) return;

        // Guard against double-attach (global hook + manual call).
        if (rootVisual is FrameworkElement fe)
        {
            if (fe.Tag is string tag && tag == ClipTag) return;
            fe.Tag = ClipTag;
        }

        UpdateClip(rootVisual, window);

        window.SizeChanged += (_, _) => UpdateClip(rootVisual, window);
        window.StateChanged += (_, _) => UpdateClip(rootVisual, window);
    }

    /// <summary>
    /// Sets or clears both the Win32 window region and the WPF visual clip
    /// based on the current window size and state.
    /// </summary>
    private static void UpdateClip(UIElement rootVisual, Window window)
    {
        // No rounding when maximized — window must fill the screen edge-to-edge.
        if (window.WindowState == WindowState.Maximized)
        {
            rootVisual.Clip = null;
            ClearWindowRegion(window);
            return;
        }

        double width = window.ActualWidth;
        double height = window.ActualHeight;

        if (width <= 0 || height <= 0)
        {
            rootVisual.Clip = null;
            ClearWindowRegion(window);
            return;
        }

        double radius = Math.Min(Windows10CornerRadius, Math.Min(width, height) / 2);

        // ── Layer 1: Win32 region (device pixels) ───────────────────
        // Physically removes corner pixels from the HWND so the OS
        // doesn't paint the white window background behind them.
        ApplyWindowRegion(window, width, height, radius);

        // ── Layer 2: WPF visual clip (DIPs) ─────────────────────────
        // Provides smooth, anti-aliased rounded edges within WPF.
        var geometry = new RectangleGeometry(
            new Rect(0, 0, width, height),
            radius,
            radius);

        if (geometry.CanFreeze)
        {
            geometry.Freeze();
        }

        rootVisual.Clip = geometry;
    }

    // ── Win32 region helpers ────────────────────────────────────────────

    /// <summary>
    /// Applies a rounded-rectangle region to the HWND. The region is
    /// specified in device (physical) pixels, so we scale from DIPs
    /// using the window's DPI transform.
    /// </summary>
    private static void ApplyWindowRegion(Window window, double dipWidth, double dipHeight, double dipRadius)
    {
        try
        {
            var helper = new WindowInteropHelper(window);
            IntPtr hwnd = helper.Handle;
            if (hwnd == IntPtr.Zero) return;

            // Convert from DIPs to device pixels for the Win32 region.
            var source = PresentationSource.FromVisual(window);
            double dpiScaleX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            double dpiScaleY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

            int pixelWidth = (int)Math.Ceiling(dipWidth * dpiScaleX);
            int pixelHeight = (int)Math.Ceiling(dipHeight * dpiScaleY);
            int pixelRadiusX = (int)Math.Ceiling(dipRadius * dpiScaleX * 2);
            int pixelRadiusY = (int)Math.Ceiling(dipRadius * dpiScaleY * 2);

            IntPtr rgn = CreateRoundRectRgn(0, 0, pixelWidth + 1, pixelHeight + 1, pixelRadiusX, pixelRadiusY);
            if (rgn != IntPtr.Zero)
            {
                // SetWindowRgn takes ownership of the region handle — do NOT
                // call DeleteObject; the OS manages its lifetime.
                SetWindowRgn(hwnd, rgn, true);
            }
        }
        catch
        {
            // Non-critical visual polish — never crash the app.
        }
    }

    /// <summary>
    /// Removes any custom region from the window, restoring the full
    /// rectangular shape (used when maximized).
    /// </summary>
    private static void ClearWindowRegion(Window window)
    {
        try
        {
            var helper = new WindowInteropHelper(window);
            IntPtr hwnd = helper.Handle;
            if (hwnd == IntPtr.Zero) return;

            SetWindowRgn(hwnd, IntPtr.Zero, true);
        }
        catch
        {
            // Non-critical.
        }
    }

    // ── Registry detection ──────────────────────────────────────────────

    private static int? ReadCurrentBuildNumberFromRegistry()
    {
        try
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(CurrentVersionRegistryPath);
            object? value = key?.GetValue(CurrentBuildNumberValueName);
            return value switch
            {
                int build => build,
                string buildText when int.TryParse(buildText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int build) => build,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }
}
