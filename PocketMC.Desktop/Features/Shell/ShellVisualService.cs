using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using PocketMC.Desktop.Core.Interfaces;
using PocketMC.Desktop.Features.Shell.Interfaces;
using PocketMC.Desktop.Features.Shell.Native;
using Wpf.Ui.Controls;

namespace PocketMC.Desktop.Features.Shell
{
    public sealed class ShellVisualService : IShellVisualService, IDisposable
    {
        private const string SolidDarkFallback = "#FF242424";
        private const string AcrylicActiveTint = "#CC202020";
        private const string MicaActiveTint = "#B8202020";
        private const string SolidLightFallback = "#FFF7F7F7";
        private const string TransparentTint = "#00FFFFFF";
        private const int DwmUseImmersiveDarkMode = 20;
        private const int DwmUseImmersiveDarkModeBefore20H1 = 19;

        // ── Blur-specific constants ──
        // Lighter tint so the real acrylic wallpaper bleed-through is visible.
        private const string BlurOverlayTint = "#73181818";
        private const string BlurInactiveFallback = "#FF202020";

        private readonly ApplicationState _applicationState;
        private FluentWindow? _boundWindow;
        private IntPtr _boundHwnd;
        private bool _isWindowActive = true;
        private bool _isNativeBlurActive;

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public ShellVisualService(ApplicationState applicationState)
        {
            _applicationState = applicationState;
        }

        public void Attach(FluentWindow window)
        {
            _boundWindow = window;

            // Capture the HWND once the window has a valid handle.
            if (window.IsLoaded)
            {
                _boundHwnd = new WindowInteropHelper(window).Handle;
            }
            else
            {
                window.Loaded += (_, _) =>
                {
                    _boundHwnd = new WindowInteropHelper(window).Handle;
                };
            }

            ApplyTheme();
            RequestMicaUpdate();
        }

        public void RequestMicaUpdate()
        {
            var window = _boundWindow;
            if (window == null) return;

            if (!window.Dispatcher.CheckAccess())
            {
                window.Dispatcher.Invoke(RequestMicaUpdate);
                return;
            }

            try
            {
                ApplyTheme();
                ApplyDwmDarkMode(window);

                string backdrop = _applicationState.Settings.WindowBackdrop ?? "Acrylic";

                // ── Inactive window ──
                if (!_isWindowActive)
                {
                    DisableNativeBlurIfActive();

                    if (IsBlurEffectiveBackdrop(backdrop))
                    {
                        // Blur inactive: solid dark window, clear tint layers.
                        window.WindowBackdropType = WindowBackdropType.None;
                        window.Background = CreateBrush(BlurInactiveFallback);
                        SetTintLayer(window, "BackdropTintLayer", TransparentTint);
                        SetTintLayer(window, "AcrylicTintLayer", TransparentTint);
                        RestoreOpaqueBrushes(window);
                        return;
                    }

                    ApplySolidFallback(window, SolidDarkFallback);
                    return;
                }

                // ── Light mode ──
                if (backdrop.Equals("Light", StringComparison.OrdinalIgnoreCase))
                {
                    DisableNativeBlurIfActive();
                    RestoreOpaqueBrushes(window);
                    window.WindowBackdropType = WindowBackdropType.None;
                    window.Background = CreateBrush(SolidLightFallback);
                    SetTintLayer(window, "BackdropTintLayer", TransparentTint);
                    SetTintLayer(window, "AcrylicTintLayer", TransparentTint);
                    return;
                }

                // ── Explicit Blur — works on all OS versions ──
                if (backdrop.Equals("Blur", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyNativeBlur(window);
                    return;
                }

                // ── Mica — Windows 11 only; fallback to native blur on Windows 10 ──
                if (backdrop.Equals("Mica", StringComparison.OrdinalIgnoreCase))
                {
                    if (Environment.OSVersion.Version.Build >= 22000)
                    {
                        DisableNativeBlurIfActive();
                        RestoreOpaqueBrushes(window);
                        window.WindowBackdropType = WindowBackdropType.Mica;
                        window.Background = Brushes.Transparent;
                        SetTintLayer(window, "BackdropTintLayer", MicaActiveTint);
                        SetTintLayer(window, "AcrylicTintLayer", TransparentTint);
                        return;
                    }

                    // Windows 10: Mica not supported — use native acrylic blur.
                    ApplyNativeBlur(window);
                    return;
                }

                // ── Acrylic — Windows 11 only; fallback to native blur on Windows 10 ──
                if (backdrop.Equals("Acrylic", StringComparison.OrdinalIgnoreCase))
                {
                    if (Environment.OSVersion.Version.Build >= 22000)
                    {
                        DisableNativeBlurIfActive();
                        RestoreOpaqueBrushes(window);
                        window.WindowBackdropType = WindowBackdropType.Acrylic;
                        window.Background = Brushes.Transparent;
                        SetTintLayer(window, "BackdropTintLayer", AcrylicActiveTint);
                        SetTintLayer(window, "AcrylicTintLayer", TransparentTint);
                        return;
                    }

                    // Windows 10: Acrylic not supported — use native acrylic blur.
                    ApplyNativeBlur(window);
                    return;
                }

                // ── "None" / Solid Dark or unrecognised ──
                DisableNativeBlurIfActive();
                RestoreOpaqueBrushes(window);
                ApplySolidFallback(window, SolidDarkFallback);
            }
            catch
            {
                ApplySolidFallbackBestEffort(window);
            }
        }

        public void ApplyTheme(string theme = "Dark")
        {
            var window = _boundWindow;
            if (window == null) return;

            if (!window.Dispatcher.CheckAccess())
            {
                window.Dispatcher.Invoke(() => ApplyTheme(theme));
                return;
            }

            try
            {
                if (window.IsLoaded)
                {
                    Wpf.Ui.Appearance.SystemThemeWatcher.UnWatch(window);
                }

                string backdrop = _applicationState.Settings.WindowBackdrop ?? "Acrylic";
                bool explicitLightMode = backdrop.Equals("Light", StringComparison.OrdinalIgnoreCase);
                Wpf.Ui.Appearance.ApplicationThemeManager.Apply(
                    explicitLightMode
                        ? Wpf.Ui.Appearance.ApplicationTheme.Light
                        : Wpf.Ui.Appearance.ApplicationTheme.Dark);
            }
            catch
            {
                // Visual polish should never block startup or server control.
            }
        }

        public void SetWindowActive(bool isActive)
        {
            _isWindowActive = isActive;
            RequestMicaUpdate();
        }

        // ── Native Blur Helpers ─────────────────────────────────────────────

        /// <summary>
        /// Returns true if the effective backdrop for the current setting would be native blur
        /// (either explicit "Blur" or Mica/Acrylic on Windows 10).
        /// </summary>
        private static bool IsBlurEffectiveBackdrop(string backdrop)
        {
            if (backdrop.Equals("Blur", StringComparison.OrdinalIgnoreCase))
                return true;

            // Mica/Acrylic on Windows 10 fall back to blur
            if (Environment.OSVersion.Version.Build < 22000 &&
                (backdrop.Equals("Mica", StringComparison.OrdinalIgnoreCase) ||
                 backdrop.Equals("Acrylic", StringComparison.OrdinalIgnoreCase)))
                return true;

            return false;
        }

        /// <summary>
        /// Applies native Windows 10 acrylic blur: transparent window, dark overlay tint,
        /// and semi-transparent WPF-UI theme brushes so the wallpaper bleeds through.
        /// </summary>
        private void ApplyNativeBlur(FluentWindow window)
        {
            // Disable WPF-UI backdrop — we own the composition now.
            window.WindowBackdropType = WindowBackdropType.None;
            window.Background = Brushes.Transparent;

            // BackdropTintLayer stays transparent — the native acrylic provides its own tint.
            SetTintLayer(window, "BackdropTintLayer", TransparentTint);

            // AcrylicTintLayer provides an additional WPF-side dark overlay.
            SetTintLayer(window, "AcrylicTintLayer", BlurOverlayTint);

            // Override WPF-UI theme brushes to semi-transparent so the blur shows through.
            ApplyTransparentBrushes(window);

            if (_boundHwnd != IntPtr.Zero)
            {
                Windows10Blur.Enable(_boundHwnd);
                _isNativeBlurActive = true;
            }
        }

        /// <summary>
        /// Disables native blur if it is currently active. Safe to call when blur is not active.
        /// </summary>
        private void DisableNativeBlurIfActive()
        {
            if (_isNativeBlurActive && _boundHwnd != IntPtr.Zero)
            {
                Windows10Blur.Disable(_boundHwnd);
                _isNativeBlurActive = false;
            }
        }

        // ── Brush Overrides ─────────────────────────────────────────────────

        /// <summary>
        /// Overrides key WPF-UI theme resource brushes with semi-transparent equivalents
        /// so that the native acrylic blur shows through cards, panes, and content areas.
        /// </summary>
        private static void ApplyTransparentBrushes(FluentWindow window)
        {
            try
            {
                var res = window.Resources;
                // NavigationView pane + content background
                res["NavigationViewContentBackground"] = Brushes.Transparent;
                res["NavigationViewPaneBackground"] = CreateBrush("#40282828");
                res["NavigationViewContentGridBorderBrush"] = Brushes.Transparent;

                // Cards, expanders, and panels → semi-transparent dark
                var cardBg = CreateBrush("#66424242");
                res["CardBackgroundFillColorDefaultBrush"] = cardBg;
                res["CardBackgroundFillColorSecondaryBrush"] = CreateBrush("#55383838");
                res["ExpanderHeaderBackground"] = cardBg;
                res["CardStrokeColorDefaultBrush"] = CreateBrush("#22FFFFFF");

                // Control backgrounds
                res["ControlFillColorDefaultBrush"] = CreateBrush("#44444444");
                res["ControlFillColorSecondaryBrush"] = CreateBrush("#33444444");

                // Subtle background for list items
                res["SubtleFillColorTransparentBrush"] = Brushes.Transparent;
            }
            catch
            {
                // Non-critical — some keys may not exist in every WPF-UI version.
            }
        }

        /// <summary>
        /// Removes the semi-transparent brush overrides from window resources,
        /// allowing the WPF-UI theme defaults to take effect again.
        /// </summary>
        private static void RestoreOpaqueBrushes(FluentWindow window)
        {
            try
            {
                var keys = new[]
                {
                    "NavigationViewContentBackground",
                    "NavigationViewPaneBackground",
                    "NavigationViewContentGridBorderBrush",
                    "CardBackgroundFillColorDefaultBrush",
                    "CardBackgroundFillColorSecondaryBrush",
                    "ExpanderHeaderBackground",
                    "CardStrokeColorDefaultBrush",
                    "ControlFillColorDefaultBrush",
                    "ControlFillColorSecondaryBrush",
                    "SubtleFillColorTransparentBrush"
                };

                foreach (var key in keys)
                {
                    if (window.Resources.Contains(key))
                        window.Resources.Remove(key);
                }
            }
            catch
            {
                // Non-critical.
            }
        }

        // ── DWM / Helpers ───────────────────────────────────────────────────

        private static void ApplyDwmDarkMode(FluentWindow window)
        {
            try
            {
                if (!window.IsLoaded) return;

                var helper = new WindowInteropHelper(window);
                if (helper.Handle == IntPtr.Zero) return;

                int isDark = 1;
                int size = sizeof(int);
                DwmSetWindowAttribute(helper.Handle, DwmUseImmersiveDarkMode, ref isDark, size);
                DwmSetWindowAttribute(helper.Handle, DwmUseImmersiveDarkModeBefore20H1, ref isDark, size);
            }
            catch
            {
                // Best-effort only. Older Windows builds or hosted previews may reject this.
            }
        }

        private static void ApplySolidFallback(FluentWindow window, string color)
        {
            window.WindowBackdropType = WindowBackdropType.None;
            window.Background = CreateBrush(color);
            SetTintLayer(window, "BackdropTintLayer", color);
            SetTintLayer(window, "AcrylicTintLayer", TransparentTint);
        }

        private static void ApplySolidFallbackBestEffort(FluentWindow window)
        {
            try
            {
                ApplySolidFallback(window, SolidDarkFallback);
            }
            catch
            {
                // Nothing else to do; failures here must not crash the shell.
            }
        }

        private static void SetTintLayer(FluentWindow window, string layerName, string color)
        {
            if (window.FindName(layerName) is Border tintLayer)
            {
                tintLayer.Background = CreateBrush(color);
            }
        }

        private static SolidColorBrush CreateBrush(string color)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!);
            brush.Freeze();
            return brush;
        }

        public void Dispose()
        {
            DisableNativeBlurIfActive();
        }
    }
}
