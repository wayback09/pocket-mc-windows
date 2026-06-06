using PocketMC.Desktop.Features.RemoteControl.Auth;

namespace PocketMC.Desktop.Tests.RemoteControl;

public sealed class RemoteTokenHasherTests
{
    [Fact]
    public void GenerateToken_ReturnsUrlSafeRandomToken()
    {
        var hasher = new RemoteTokenHasher();

        string first = hasher.GenerateToken();
        string second = hasher.GenerateToken();

        Assert.NotEqual(first, second);
        Assert.DoesNotContain("+", first, StringComparison.Ordinal);
        Assert.DoesNotContain("/", first, StringComparison.Ordinal);
        Assert.DoesNotContain("=", first, StringComparison.Ordinal);
        Assert.True(first.Length >= 40);
    }

    [Fact]
    public void HashAndVerify_AcceptsOnlyOriginalToken()
    {
        var hasher = new RemoteTokenHasher();

        RemoteTokenHash hash = hasher.Hash("correct-token");

        Assert.True(hasher.Verify("correct-token", hash.Salt, hash.Hash));
        Assert.False(hasher.Verify("wrong-token", hash.Salt, hash.Hash));
    }

    [Fact]
    public void Verify_UsesFixedTimeComparison()
    {
        string source = File.ReadAllText(TestSourceFileResolver.Resolve(
            "PocketMC.Desktop",
            "Features",
            "RemoteControl",
            "Auth",
            "RemoteTokenHasher.cs"));

        Assert.Contains("CryptographicOperations.FixedTimeEquals", source, StringComparison.Ordinal);
    }
}
