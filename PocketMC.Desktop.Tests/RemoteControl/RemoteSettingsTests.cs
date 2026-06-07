using System.Text.Json;
using System.Security.Cryptography;
using PocketMC.Desktop.Features.RemoteControl.Models;
using PocketMC.Desktop.Features.Settings;

namespace PocketMC.Desktop.Tests.RemoteControl;

public sealed class RemoteSettingsTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "PocketMC.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Load_NormalizesRemoteControlWithSafeDefaults()
    {
        Directory.CreateDirectory(_tempDirectory);
        string settingsPath = Path.Combine(_tempDirectory, "settings.json");
        File.WriteAllText(settingsPath, "{}");

        var settings = new SettingsManager(settingsPath).Load();

        Assert.NotNull(settings.RemoteControl);
        Assert.False(settings.RemoteControl.Enabled);
        Assert.Equal(25580, settings.RemoteControl.Port);
        Assert.Equal(RemoteAccessMode.LanOnly, settings.RemoteControl.AccessMode);
        Assert.Equal("none", settings.RemoteControl.TunnelProviderId);
        Assert.False(settings.RemoteControl.AllowRemoteConsoleCommands);
        Assert.True(settings.RemoteControl.AllowRemotePlayerActions);
        Assert.Null(settings.RemoteControl.PlayitTunnelId);
        Assert.Empty(settings.RemoteControl.PairedDevices);
    }

    [Fact]
    public void Load_RemovesNullRemoteDeviceEntriesFromMalformedSettings()
    {
        Directory.CreateDirectory(_tempDirectory);
        string settingsPath = Path.Combine(_tempDirectory, "settings.json");
        File.WriteAllText(settingsPath, """
        {
          "RemoteControl": {
            "PairedDevices": [ null ]
          }
        }
        """);

        var settings = new SettingsManager(settingsPath).Load();

        Assert.Empty(settings.RemoteControl.PairedDevices);
    }

    [Fact]
    public void Save_PersistsRemoteControlWithoutPlainDeviceToken()
    {
        Directory.CreateDirectory(_tempDirectory);
        string settingsPath = Path.Combine(_tempDirectory, "settings.json");
        var settings = new SettingsManager(settingsPath).Load();
        settings.RemoteControl.PairedDevices.Add(new RemoteDeviceSession
        {
            DisplayName = "Phone",
            TokenSalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            TokenHash = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        new SettingsManager(settingsPath).Save(settings);

        string persisted = File.ReadAllText(settingsPath);
        Assert.Contains("RemoteControl", persisted, StringComparison.Ordinal);
        Assert.DoesNotContain("RAW_DEVICE_TOKEN", persisted, StringComparison.Ordinal);

        var roundTripped = JsonSerializer.Deserialize<Dictionary<string, object>>(persisted);
        Assert.NotNull(roundTripped);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
