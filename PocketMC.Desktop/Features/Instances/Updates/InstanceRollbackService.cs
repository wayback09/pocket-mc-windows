using System.IO;
using System.Text.Json;
using PocketMC.Desktop.Infrastructure.FileSystem;
using PocketMC.Desktop.Infrastructure.Security;
using PocketMC.Desktop.Models;

namespace PocketMC.Desktop.Features.Instances.Updates;

public sealed class InstanceRollbackService
{
    private const string SnapshotManifestFileName = "snapshot_manifest.json";

    private static readonly string[] SnapshotFiles =
    [
        ".pocket-mc.json",
        "addon_manifest.json",
        "server.jar",
        "installer.jar",
        "PocketMine-MP.phar",
        "server.properties",
        "eula.txt",
        "permissions.json",
        "allowlist.json"
    ];

    private static readonly string[] SnapshotDirectories =
    [
        "libraries",
        "plugins",
        "mods",
        "behavior_packs",
        "resource_packs",
        "config"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly InstanceUpdateJournalStore _journalStore;

    public InstanceRollbackService(InstanceUpdateJournalStore journalStore)
    {
        _journalStore = journalStore;
    }

    public async Task<string> CreateSnapshotAsync(
        InstanceUpdatePlan plan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        string snapshotDirectory = _journalStore.GetOperationRoot(plan.ServerDir, "snapshots", plan.OperationId);
        if (Directory.Exists(snapshotDirectory))
        {
            await FileUtils.CleanDirectoryAsync(snapshotDirectory, cancellationToken);
        }

        Directory.CreateDirectory(snapshotDirectory);
        string serverRoot = Path.GetFullPath(plan.ServerDir);
        var manifest = new SnapshotManifest();

        foreach (string relativeFile in EnumerateSnapshotFiles(serverRoot, plan.CurrentMetadata))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? source = PathSafety.ValidateContainedPath(serverRoot, relativeFile);
            if (source == null)
            {
                throw new InvalidOperationException($"Snapshot file path '{relativeFile}' is invalid.");
            }

            manifest.Items.Add(new SnapshotItem { RelativePath = relativeFile, IsDirectory = false, Existed = File.Exists(source) });
            if (!File.Exists(source))
            {
                continue;
            }

            string destination = ResolveSnapshotPath(snapshotDirectory, relativeFile);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            await FileUtils.CopyFileAsync(source, destination, overwrite: true);
        }

        foreach (string relativeDirectory in SnapshotDirectories)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? source = PathSafety.ValidateContainedPath(serverRoot, relativeDirectory);
            if (source == null)
            {
                throw new InvalidOperationException($"Snapshot directory path '{relativeDirectory}' is invalid.");
            }

            manifest.Items.Add(new SnapshotItem { RelativePath = relativeDirectory, IsDirectory = true, Existed = Directory.Exists(source) });
            if (!Directory.Exists(source))
            {
                continue;
            }

            string destination = ResolveSnapshotPath(snapshotDirectory, relativeDirectory);
            await FileUtils.CopyDirectoryAsync(source, destination);
        }

        string manifestPath = ResolveSnapshotPath(snapshotDirectory, SnapshotManifestFileName);
        await FileUtils.AtomicWriteAllTextAsync(
            manifestPath,
            JsonSerializer.Serialize(manifest, JsonOptions),
            cancellationToken: cancellationToken);

        return snapshotDirectory;
    }

    public async Task RollbackAsync(
        InstanceUpdateJournal journal,
        bool restoreWorldBackup = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(journal);
        if (string.IsNullOrWhiteSpace(journal.SnapshotDirectory) ||
            !Directory.Exists(journal.SnapshotDirectory))
        {
            return;
        }

        journal.State = InstanceUpdateJournalState.RollingBack;
        await _journalStore.SaveAsync(journal, cancellationToken);

        string serverRoot = Path.GetFullPath(journal.ServerDir);
        string snapshotRoot = Path.GetFullPath(journal.SnapshotDirectory);
        SnapshotManifest manifest = await LoadSnapshotManifestAsync(snapshotRoot, cancellationToken);

        foreach (SnapshotItem item in manifest.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string targetPath = PathSafety.ValidateContainedPath(serverRoot, item.RelativePath)
                ?? throw new InvalidOperationException($"Rollback target path '{item.RelativePath}' is invalid.");
            string snapshotPath = ResolveSnapshotPath(snapshotRoot, item.RelativePath);

            if (item.IsDirectory)
            {
                if (Directory.Exists(targetPath))
                {
                    await FileUtils.CleanDirectoryAsync(targetPath, cancellationToken);
                }

                if (item.Existed && Directory.Exists(snapshotPath))
                {
                    await FileUtils.CopyDirectoryAsync(snapshotPath, targetPath);
                }
            }
            else
            {
                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                }

                if (item.Existed && File.Exists(snapshotPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                    await FileUtils.CopyFileAsync(snapshotPath, targetPath, overwrite: true);
                }
            }
        }

        journal.State = InstanceUpdateJournalState.RolledBack;
        await _journalStore.SaveAsync(journal, cancellationToken);
    }

    private static IEnumerable<string> EnumerateSnapshotFiles(string serverRoot, InstanceMetadata metadata)
    {
        foreach (string fileName in SnapshotFiles)
        {
            yield return fileName;
        }

        if (metadata.ServerType?.StartsWith("Bedrock", StringComparison.OrdinalIgnoreCase) != true)
        {
            yield break;
        }

        foreach (string filePath in Directory.EnumerateFiles(serverRoot, "*", SearchOption.TopDirectoryOnly))
        {
            string fileName = Path.GetFileName(filePath);
            if (fileName.Equals(SnapshotManifestFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return fileName;
        }
    }

    private static async Task<SnapshotManifest> LoadSnapshotManifestAsync(
        string snapshotRoot,
        CancellationToken cancellationToken)
    {
        string manifestPath = ResolveSnapshotPath(snapshotRoot, SnapshotManifestFileName);
        if (!File.Exists(manifestPath))
        {
            return new SnapshotManifest
            {
                Items = SnapshotFiles
                    .Select(file => new SnapshotItem { RelativePath = file, IsDirectory = false, Existed = File.Exists(ResolveSnapshotPath(snapshotRoot, file)) })
                    .Concat(SnapshotDirectories.Select(directory => new SnapshotItem { RelativePath = directory, IsDirectory = true, Existed = Directory.Exists(ResolveSnapshotPath(snapshotRoot, directory)) }))
                    .ToList()
            };
        }

        await using FileStream stream = new(manifestPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync: true);
        return await JsonSerializer.DeserializeAsync<SnapshotManifest>(stream, cancellationToken: cancellationToken)
            ?? new SnapshotManifest();
    }

    private static string ResolveSnapshotPath(string snapshotRoot, string relativePath)
    {
        return PathSafety.ValidateContainedPath(snapshotRoot, relativePath)
            ?? throw new InvalidOperationException($"Snapshot path '{relativePath}' is invalid.");
    }

    private sealed class SnapshotManifest
    {
        public List<SnapshotItem> Items { get; set; } = new();
    }

    private sealed class SnapshotItem
    {
        public string RelativePath { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public bool Existed { get; set; }
    }
}
