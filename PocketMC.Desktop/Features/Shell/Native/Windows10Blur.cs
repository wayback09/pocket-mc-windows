using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace PocketMC.Desktop.Features.Shell.Native
{
    /// <summary>
    /// Applies native Windows 10 blur-behind using SetWindowCompositionAttribute.
    /// Uses ACCENT_ENABLE_BLURBEHIND (the reliable, proven approach on Windows 10).
    /// 
    /// The critical piece that makes the blur VISIBLE through WPF is setting
    /// HwndSource.CompositionTarget.BackgroundColor to transparent. Without this,
    /// WPF paints an opaque surface over the DWM composition and the blur is hidden.
    /// 
    /// Safe best-effort: failures are silently swallowed so the app never crashes.
    /// </summary>
    internal static class Windows10Blur
    {
        private const int WCA_ACCENT_POLICY = 19;
        private const int ACCENT_DISABLED = 0;
        private const int ACCENT_ENABLE_BLURBEHIND = 3;

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public int AccentState;
            public int AccentFlags;
            public uint GradientColor;   // AABBGGRR format
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        /// <summary>
        /// Enables blur-behind on the given window.
        /// Sets ACCENT_ENABLE_BLURBEHIND and makes the WPF rendering surface transparent
        /// so the DWM blur composition actually shows through.
        /// </summary>
        public static void Enable(Window window)
        {
            try
            {
                var helper = new WindowInteropHelper(window);
                if (helper.Handle == IntPtr.Zero) return;

                // ── Step 1: Make the WPF rendering surface transparent ──
                // Without this, WPF paints an opaque background OVER the DWM blur.
                var hwndSource = HwndSource.FromHwnd(helper.Handle);
                if (hwndSource?.CompositionTarget != null)
                {
                    hwndSource.CompositionTarget.BackgroundColor = Colors.Transparent;
                }

                // ── Step 2: Apply the blur accent policy ──
                var accent = new AccentPolicy
                {
                    AccentState = ACCENT_ENABLE_BLURBEHIND,
                    AccentFlags = 0,
                    GradientColor = 0,   // No DWM-level tint; tinting is done via XAML overlay
                    AnimationId = 0
                };

                SetAccent(helper.Handle, ref accent);
            }
            catch
            {
                // Best-effort only — older builds or hosted environments may reject this.
            }
        }

        /// <summary>
        /// Disables the blur effect, restores opaque WPF composition target.
        /// </summary>
        public static void Disable(Window window)
        {
            try
            {
                var helper = new WindowInteropHelper(window);
                if (helper.Handle == IntPtr.Zero) return;

                // ── Restore DWM accent to disabled ──
                var accent = new AccentPolicy
                {
                    AccentState = ACCENT_DISABLED,
                    AccentFlags = 0,
                    GradientColor = 0,
                    AnimationId = 0
                };

                SetAccent(helper.Handle, ref accent);

                // ── Restore opaque WPF composition ──
                var hwndSource = HwndSource.FromHwnd(helper.Handle);
                if (hwndSource?.CompositionTarget != null)
                {
                    // Restore to the default dark background so the window isn't transparent
                    // while in solid/inactive mode.
                    hwndSource.CompositionTarget.BackgroundColor = Color.FromRgb(0x20, 0x20, 0x20);
                }
            }
            catch
            {
                // Best-effort only.
            }
        }

        private static void SetAccent(IntPtr hwnd, ref AccentPolicy accent)
        {
            int accentSize = Marshal.SizeOf(accent);
            IntPtr accentPtr = Marshal.AllocHGlobal(accentSize);
            try
            {
                Marshal.StructureToPtr(accent, accentPtr, false);

                var data = new WindowCompositionAttributeData
                {
                    Attribute = WCA_ACCENT_POLICY,
                    Data = accentPtr,
                    SizeOfData = accentSize
                };

                SetWindowCompositionAttribute(hwnd, ref data);
            }
            finally
            {
                Marshal.FreeHGlobal(accentPtr);
            }
        }
    }
}
