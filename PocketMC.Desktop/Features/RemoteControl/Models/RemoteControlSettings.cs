namespace PocketMC.Desktop.Features.RemoteControl.Models;

public sealed class RemoteControlSettings
{
    public bool Enabled { get; set; }
    public int Port { get; set; } = 25580;
    public RemoteAccessMode AccessMode { get; set; } = RemoteAccessMode.LanOnly;
    public string TunnelProviderId { get; set; } = "cloudflared-quick";
    public string? CloudflaredPath { get; set; }
    public bool AllowRemoteConsoleCommands { get; set; }
    public bool AllowRemotePlayerActions { get; set; } = true;
    public List<RemoteDeviceSession> PairedDevices { get; set; } = new();
}
