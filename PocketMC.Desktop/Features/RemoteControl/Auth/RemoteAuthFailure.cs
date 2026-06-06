namespace PocketMC.Desktop.Features.RemoteControl.Auth;

public enum RemoteAuthFailure
{
    None,
    InvalidPairingToken,
    ExpiredPairingToken,
    UsedPairingToken,
    InvalidDeviceToken,
    RevokedDeviceToken
}
