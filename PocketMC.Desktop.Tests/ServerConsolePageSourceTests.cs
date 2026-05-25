namespace PocketMC.Desktop.Tests;

public sealed class ServerConsolePageSourceTests
{
    [Fact]
    public void Constructor_AllowsNullableProcessAndUsesInstancePath()
    {
        string source = File.ReadAllText(TestSourceFileResolver.Resolve(
            "PocketMC.Desktop",
            "Features",
            "Console",
            "ServerConsolePage.xaml.cs"));
        string normalizedSource = source.Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains("ServerProcess? serverProcess", source);
        Assert.Contains("string instancePath", source);
        Assert.Contains("InstanceMetadata metadata,\n            string instancePath", normalizedSource);
        Assert.Contains("if (_serverProcess != null)", source);
        Assert.Contains("_serverProcess.OnOutputLine", source);
        Assert.Contains("IsReadOnlySessionLog", source);
    }
}
