using System;
using System.Windows;
using Velopack;

namespace PocketMC.Desktop;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Velopack MUST be bootstrapped before any WPF code runs.
        // This handles squirrel-style install/uninstall hooks and
        // delta-patch application on startup.
        VelopackApp.Build().Run();

        // Enforce single instance rule before starting WPF
        if (!Infrastructure.SingleInstanceService.InitializeAsFirstInstance(args))
        {
            // Another instance is running, and we just sent it a message to show itself.
            // Exit immediately.
            return;
        }

        try
        {
            // Normal WPF startup
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
        finally
        {
            Infrastructure.SingleInstanceService.Cleanup();
        }
    }
}
