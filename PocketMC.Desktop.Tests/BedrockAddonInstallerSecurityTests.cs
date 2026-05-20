using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using PocketMC.Desktop.Features.Mods;

namespace PocketMC.Desktop.Tests;

public sealed class BedrockAddonInstallerSecurityTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "PocketMC.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task UninstallAsync_DeletesPackAndScrubsWorldJson()
    {
        Directory.CreateDirectory(_tempDirectory);
        string serverDir = Path.Combine(_tempDirectory, "server");
        string packDir = Path.Combine(serverDir, "behavior_packs", "TestPack");
        Directory.CreateDirectory(packDir);

        await File.WriteAllTextAsync(
            Path.Combine(packDir, "manifest.json"),
            """
            {
              "header": {
                "name": "TestPack",
                "uuid": "d15c1f4d-a87f-4edc-8af1-2b5683cb6698",
                "version": [1, 0, 0]
              },
              "modules": [
                { "type": "data" }
              ]
            }
            """);

        string worldDir = Path.Combine(serverDir, "worlds", "Bedrock level");
        Directory.CreateDirectory(worldDir);
        string worldJson = Path.Combine(worldDir, "world_behavior_packs.json");
        await File.WriteAllTextAsync(
            worldJson,
            """
            [
              { "pack_id": "d15c1f4d-a87f-4edc-8af1-2b5683cb6698", "version": [1, 0, 0] },
              { "pack_id": "11111111-1111-1111-1111-111111111111", "version": [1, 0, 0] }
            ]
            """);

        var installer = new BedrockAddonInstaller(NullLogger<BedrockAddonInstaller>.Instance);

        await installer.UninstallAsync("TestPack", serverDir);

        Assert.False(Directory.Exists(packDir));
        string updatedJson = await File.ReadAllTextAsync(worldJson);
        Assert.DoesNotContain("d15c1f4d-a87f-4edc-8af1-2b5683cb6698", updatedJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("11111111-1111-1111-1111-111111111111", updatedJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InstallAsync_DoesNotDeletePackRoot_WhenManifestNameSanitizesToEmpty()
    {
        Directory.CreateDirectory(_tempDirectory);
        string serverDir = Path.Combine(_tempDirectory, "server");
        string behaviorPacksDir = Path.Combine(serverDir, "behavior_packs");
        Directory.CreateDirectory(behaviorPacksDir);
        string canaryPath = Path.Combine(behaviorPacksDir, "canary.txt");
        await File.WriteAllTextAsync(canaryPath, "still here");

        string addonPath = Path.Combine(_tempDirectory, "bad.mcpack");
        using (var archive = ZipFile.Open(addonPath, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("manifest.json");
            await using var stream = entry.Open();
            await using var writer = new StreamWriter(stream, Encoding.UTF8);
            await writer.WriteAsync("""
            {
              "header": {
                "name": "...",
                "uuid": "d15c1f4d-a87f-4edc-8af1-2b5683cb6698",
                "version": [1, 0, 0]
              },
              "modules": [
                { "type": "data" }
              ]
            }
            """);
        }

        var installer = new BedrockAddonInstaller(NullLogger<BedrockAddonInstaller>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => installer.InstallAsync(addonPath, serverDir));
        Assert.True(Directory.Exists(behaviorPacksDir));
        Assert.True(File.Exists(canaryPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
