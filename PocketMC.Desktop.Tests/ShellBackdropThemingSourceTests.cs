namespace PocketMC.Desktop.Tests;

public sealed class ShellBackdropThemingSourceTests
{
    [Fact]
    public void MainWindow_DoesNotHardcodeMicaBackdropAndDefinesTintLayer()
    {
        string xaml = File.ReadAllText(TestSourceFileResolver.Resolve(
            "PocketMC.Desktop",
            "Features",
            "Shell",
            "MainWindow.xaml"));

        Assert.DoesNotContain("WindowBackdropType=\"Mica\"", xaml);
        Assert.Contains("Background=\"#FF242424\"", xaml);
        Assert.Contains("x:Name=\"BackdropTintLayer\"", xaml);
        Assert.Contains("Activated=\"Window_Activated\"", xaml);
        Assert.Contains("Deactivated=\"Window_Deactivated\"", xaml);
    }

    [Fact]
    public void ShellVisualServiceContract_ExposesWindowActivationState()
    {
        string source = File.ReadAllText(TestSourceFileResolver.Resolve(
            "PocketMC.Desktop",
            "Features",
            "Shell",
            "Interfaces",
            "IShellVisualService.cs"));

        Assert.Contains("SetWindowActive(bool isActive)", source);
    }

    [Fact]
    public void ShellVisualService_InactiveStateDisablesBackdropAndUsesDarkFallback()
    {
        string source = File.ReadAllText(TestSourceFileResolver.Resolve(
            "PocketMC.Desktop",
            "Features",
            "Shell",
            "ShellVisualService.cs"));

        Assert.Contains("DwmSetWindowAttribute", source);
        Assert.Contains("WindowBackdropType.None", source);
        Assert.Contains("#FF242424", source);
        Assert.Contains("#CC202020", source);
        Assert.Contains("#B8202020", source);
    }
}
