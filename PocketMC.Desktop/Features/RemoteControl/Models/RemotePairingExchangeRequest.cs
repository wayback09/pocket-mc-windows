namespace PocketMC.Desktop.Features.RemoteControl.Models;

public sealed class RemotePairingExchangeRequest
{
    public string? PairingToken { get; set; }
    public string? DeviceName { get; set; }
}
