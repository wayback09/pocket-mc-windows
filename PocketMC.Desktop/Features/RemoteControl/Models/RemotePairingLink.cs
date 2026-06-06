namespace PocketMC.Desktop.Features.RemoteControl.Models;

public sealed class RemotePairingLink
{
    public string Url { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
