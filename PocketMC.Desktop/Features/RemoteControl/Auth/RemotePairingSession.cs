namespace PocketMC.Desktop.Features.RemoteControl.Auth;

public sealed class RemotePairingSession
{
    public RemotePairingSession(string token, DateTimeOffset expiresAtUtc)
    {
        Token = token;
        ExpiresAtUtc = expiresAtUtc;
    }

    public string Token { get; }
    public DateTimeOffset ExpiresAtUtc { get; }
    public DateTimeOffset? LastExchangedAtUtc { get; internal set; }
}
