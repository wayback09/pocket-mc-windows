using PocketMC.Desktop.Features.Shell;

namespace PocketMC.Desktop.Tests;

public sealed class AppStartupOptionsTests
{
    [Fact]
    public void Parse_DetectsWindowsStartupAndMinimizedSwitches()
    {
        AppStartupOptions options = AppStartupOptions.Parse(new[] { "--windows-startup", "--minimized" });

        Assert.True(options.IsWindowsStartup);
        Assert.True(options.IsMinimized);
        Assert.True(options.ShouldStartMinimizedToTray);
    }

    [Fact]
    public void Parse_IsCaseInsensitive()
    {
        AppStartupOptions options = AppStartupOptions.Parse(new[] { "--Windows-Startup", "--MINIMIZED" });

        Assert.True(options.IsWindowsStartup);
        Assert.True(options.IsMinimized);
    }

    [Fact]
    public void Parse_RequiresBothSwitchesForTrayStartup()
    {
        AppStartupOptions options = AppStartupOptions.Parse(new[] { "--minimized" });

        Assert.False(options.IsWindowsStartup);
        Assert.True(options.IsMinimized);
        Assert.False(options.ShouldStartMinimizedToTray);
    }
}
