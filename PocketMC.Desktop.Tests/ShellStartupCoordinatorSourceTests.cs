namespace PocketMC.Desktop.Tests;

public sealed class ShellStartupCoordinatorSourceTests
{
    [Fact]
    public void WindowsStartupLaunch_SkipsServerAutoStarts()
    {
        string source = File.ReadAllText(TestSourceFileResolver.Resolve(
            "PocketMC.Desktop",
            "Features",
            "Shell",
            "ShellStartupCoordinator.cs"));

        Assert.Contains("AppStartupOptions startupOptions", source);
        Assert.Contains("!_startupOptions.IsWindowsStartup", source);
        Assert.Contains("Skipping server auto-start during Windows startup launch.", source);
    }
}
