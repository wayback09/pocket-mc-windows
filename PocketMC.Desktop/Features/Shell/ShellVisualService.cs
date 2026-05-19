using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using PocketMC.Desktop.Core.Interfaces;
using PocketMC.Desktop.Features.Shell.Interfaces;
using PocketMC.Desktop.Features.Shell;
using PocketMC.Desktop.Features.Instances.Services;
using PocketMC.Desktop.Features.Instances.Models;
using PocketMC.Desktop.Features.Dashboard;
using PocketMC.Desktop.Infrastructure;
using Wpf.Ui.Controls;

namespace PocketMC.Desktop.Features.Shell
{
    public sealed class ShellVisualService : IShellVisualService, IDisposable
    {
        private readonly ApplicationState _applicationState;
        private FluentWindow? _boundWindow;

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public ShellVisualService(ApplicationState applicationState)
        {
            _applicationState = applicationState;
        }

        public void Attach(FluentWindow window)
        {
            _boundWindow = window;
            ApplyTheme();
        }

        public void RequestMicaUpdate()
        {
            if (_boundWindow == null) return;

            ApplyTheme(); // Apply theme resources first

            string backdrop = _applicationState.Settings.WindowBackdrop ?? "Acrylic";

            if (backdrop == "Mica" && Environment.OSVersion.Version.Build >= 22000)
            {
                _boundWindow.WindowBackdropType = WindowBackdropType.Mica;
            }
            else if (backdrop == "Acrylic" && Environment.OSVersion.Version.Build >= 22000)
            {
                _boundWindow.WindowBackdropType = WindowBackdropType.Acrylic;
            }
            else
            {
                _boundWindow.WindowBackdropType = WindowBackdropType.None;
            }

            // Force DWM attributes to match the theme immediately after setting the backdrop type
            if (_boundWindow.IsLoaded && backdrop != "Light")
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(_boundWindow);
                int isDark = 1;
                // Force DWM Dark Mode
                DwmSetWindowAttribute(helper.Handle, 20, ref isDark, sizeof(int)); 

                // Wpf.Ui will apply a white overlay brush if the OS is in Light Mode.
                // We must forcefully override the window background to transparent!
                if (backdrop == "Mica" || backdrop == "Acrylic")
                {
                    _boundWindow.Background = System.Windows.Media.Brushes.Transparent;
                }
            }
        }

        public void ApplyTheme(string theme = "Dark")
        {
            if (_boundWindow == null) return;
            
            if (_boundWindow.IsLoaded)
            {
                Wpf.Ui.Appearance.SystemThemeWatcher.UnWatch(_boundWindow);
            }

            string backdrop = _applicationState.Settings.WindowBackdrop ?? "Acrylic";
            if (backdrop == "Light")
            {
                Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Light);
            }
            else
            {
                Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Dark);
            }
        }

        public void Dispose()
        {
        }
    }
}
