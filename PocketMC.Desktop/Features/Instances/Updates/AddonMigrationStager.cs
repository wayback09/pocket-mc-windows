using System.IO;
using System.Net.Http;
using PocketMC.Desktop.Features.Marketplace;
using PocketMC.Desktop.Infrastructure.Security;

namespace PocketMC.Desktop.Features.Instances.Updates;

public sealed class AddonMigrationStager
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AddonMigrationStager(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task StageAsync(
        AddonMigrationPlan plan,
        string addonStagingDirectory,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        string stagingRoot = Path.GetFullPath(addonStagingDirectory);
        Directory.CreateDirectory(stagingRoot);

        foreach (AddonMigrationItem item in plan.Items.Where(item =>
                     item.Action is AddonMigrationAction.Update or AddonMigrationAction.AddDependency))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(item.DownloadUrl))
            {
                throw new InvalidOperationException($"Addon '{item.ProjectId}' does not include a download URL.");
            }

            string safeFileName = MarketplaceFileNameSanitizer.RequireSafeFileName(item.TargetFileName);
            string? stagedPath = PathSafety.ValidateContainedPath(stagingRoot, safeFileName)
                ?? throw new InvalidOperationException($"Invalid staged addon file path for '{safeFileName}'.");

            using HttpClient client = _httpClientFactory.CreateClient("PocketMC.Downloads");
            using HttpResponseMessage response = await client.GetAsync(
                item.DownloadUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            await using (Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (FileStream fileStream = new(
                             stagedPath,
                             FileMode.Create,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 81920,
                             useAsync: true))
            {
                await contentStream.CopyToAsync(fileStream, cancellationToken);
            }

            ValidateNonEmptyFile(stagedPath);
            item.StagedFilePath = stagedPath;
        }
    }

    private static void ValidateNonEmptyFile(string filePath)
    {
        var info = new FileInfo(filePath);
        if (!info.Exists || info.Length == 0)
        {
            throw new IOException($"Staged addon artifact '{filePath}' is missing or empty.");
        }
    }
}
