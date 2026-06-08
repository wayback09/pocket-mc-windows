using System;
using System.Collections.Generic;
using System.Linq;
using PocketMC.Desktop.Infrastructure;

namespace PocketMC.Desktop.Features.Shell;

public sealed record AppStartupOptions(bool IsWindowsStartup, bool IsMinimized, string? ActivatedUri = null)
{
    public static AppStartupOptions NormalLaunch { get; } = new(false, false);

    public bool ShouldStartMinimizedToTray => IsWindowsStartup && IsMinimized;

    public static AppStartupOptions Parse(IEnumerable<string>? args)
    {
        string[] normalizedArgs = args?.ToArray() ?? Array.Empty<string>();

        bool isWindowsStartup = normalizedArgs.Any(arg =>
            string.Equals(arg, WindowsStartupService.WindowsStartupArgument, StringComparison.OrdinalIgnoreCase));
        bool isMinimized = normalizedArgs.Any(arg =>
            string.Equals(arg, WindowsStartupService.MinimizedArgument, StringComparison.OrdinalIgnoreCase));

        string? activatedUri = normalizedArgs.FirstOrDefault(arg => arg.StartsWith("pocketmc://", StringComparison.OrdinalIgnoreCase));

        return new AppStartupOptions(isWindowsStartup, isMinimized, activatedUri);
    }
}
