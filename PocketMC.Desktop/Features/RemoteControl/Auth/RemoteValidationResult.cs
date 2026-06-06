namespace PocketMC.Desktop.Features.RemoteControl.Auth;

public sealed class RemoteValidationResult
{
    private RemoteValidationResult(bool success, RemoteAuthFailure failure, string? deviceId, string? displayName)
    {
        Success = success;
        Failure = failure;
        DeviceId = deviceId;
        DisplayName = displayName;
    }

    public bool Success { get; }
    public RemoteAuthFailure Failure { get; }
    public string? DeviceId { get; }
    public string? DisplayName { get; }

    public static RemoteValidationResult Successful(string deviceId, string displayName) =>
        new(true, RemoteAuthFailure.None, deviceId, displayName);

    public static RemoteValidationResult Failed(RemoteAuthFailure failure) =>
        new(false, failure, null, null);
}
