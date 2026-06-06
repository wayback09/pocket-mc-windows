namespace PocketMC.Desktop.Features.RemoteControl.Auth;

public sealed class RemoteExchangeResult
{
    private RemoteExchangeResult(bool success, RemoteAuthFailure failure, string? deviceToken, string? deviceId)
    {
        Success = success;
        Failure = failure;
        DeviceToken = deviceToken;
        DeviceId = deviceId;
    }

    public bool Success { get; }
    public RemoteAuthFailure Failure { get; }
    public string? DeviceToken { get; }
    public string? DeviceId { get; }

    public static RemoteExchangeResult Successful(string deviceToken, string deviceId) =>
        new(true, RemoteAuthFailure.None, deviceToken, deviceId);

    public static RemoteExchangeResult Failed(RemoteAuthFailure failure) =>
        new(false, failure, null, null);
}
