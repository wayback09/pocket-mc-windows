using System;
using System.Runtime.InteropServices;

namespace PocketMC.Desktop.Features.Shell.Native
{
    /// <summary>
    /// Applies native Windows 10 acrylic blur using SetWindowCompositionAttribute.
    /// Uses ACCENT_ENABLE_ACRYLICBLURBEHIND for the real Settings/Start-menu acrylic look.
    /// Safe best-effort: failures are silently swallowed so the app never crashes.
    /// </summary>
    internal static class Windows10Blur
    {
        private const int WCA_ACCENT_POLICY = 19;
        private const int ACCENT_DISABLED = 0;
        private const int ACCENT_ENABLE_ACRYLICBLURBEHIND = 4;

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
        /// Enables acrylic blur-behind on the given window handle.
        /// Uses a dark translucent tint (0x70181818) matching the Windows 10 Settings look.
        /// The AABBGGRR format means: A=0x70 alpha, BB=0x18, GG=0x18, RR=0x18.
        /// </summary>
        public static void Enable(IntPtr hwnd)
        {
            try
            {
                if (hwnd == IntPtr.Zero) return;

                var accent = new AccentPolicy
                {
                    AccentState = ACCENT_ENABLE_ACRYLICBLURBEHIND,
                    AccentFlags = 2,    // required flag for acrylic
                    GradientColor = 0x70181818,
                    AnimationId = 0
                };

                SetAccent(hwnd, ref accent);
            }
            catch
            {
                // Best-effort only — older builds or hosted environments may reject this.
            }
        }

        /// <summary>
        /// Disables the blur effect, returning the window to a normal composition state.
        /// </summary>
        public static void Disable(IntPtr hwnd)
        {
            try
            {
                if (hwnd == IntPtr.Zero) return;

                var accent = new AccentPolicy
                {
                    AccentState = ACCENT_DISABLED,
                    AccentFlags = 0,
                    GradientColor = 0,
                    AnimationId = 0
                };

                SetAccent(hwnd, ref accent);
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
