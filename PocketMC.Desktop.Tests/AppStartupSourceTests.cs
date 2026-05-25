namespace PocketMC.Desktop.Tests;

public sealed class AppStartupSourceTests
{
    [Fact]
    public void OnStartup_UsesParsedStartupOptionsForTrayStartup()
    {
        string source = File.ReadAllText(TestSourceFileResolver.Resolve(
            "PocketMC.Desktop",
            "App.xaml.cs"));

        Assert.Contains("AppStartupOptions.Parse(e.Args)", source);
        Assert.Contains("startupOptions.ShouldStartMinimizedToTray", source);
        Assert.Contains("mainWindow.ShowMinimizedToTray()", source);
        Assert.Contains("mainWindow.Show()", source);
    }
}
