using PocketMC.Desktop.Core.Interfaces;
using PocketMC.Desktop.Features.Instances.Services;
using PocketMC.Desktop.Features.RemoteControl.Models;
using PocketMC.Desktop.Models;

namespace PocketMC.Desktop.Features.RemoteControl.Services;

public sealed class RemoteStatusService
{
    private readonly InstanceRegistry _registry;
    private readonly IServerLifecycleService _lifecycleService;
    private readonly IResourceMonitorService _resourceMonitorService;

    public RemoteStatusService(
        InstanceRegistry registry,
        IServerLifecycleService lifecycleService,
        IResourceMonitorService resourceMonitorService)
    {
        _registry = registry;
        _lifecycleService = lifecycleService;
        _resourceMonitorService = resourceMonitorService;
    }

    public IReadOnlyList<RemoteInstanceDto> GetInstances() =>
        _registry.GetAll()
            .OrderBy(instance => instance.Name, StringComparer.OrdinalIgnoreCase)
            .Select(instance => new RemoteInstanceDto
            {
                Id = instance.Id,
                Name = instance.Name,
                ServerType = instance.ServerType,
                IsRunning = _lifecycleService.IsRunning(instance.Id),
                State = GetState(instance.Id)
            })
            .ToList();

    public RemoteInstanceStatusDto? GetInstanceStatus(Guid instanceId)
    {
        InstanceMetadata? metadata = _registry.GetById(instanceId);
        if (metadata == null)
        {
            return null;
        }

        bool isRunning = _lifecycleService.IsRunning(instanceId);
        DateTime? sessionStartUtc = _lifecycleService.GetSessionStartTime(instanceId);
        int uptimeSeconds = isRunning && sessionStartUtc.HasValue
            ? Math.Max(0, (int)(DateTime.UtcNow - sessionStartUtc.Value).TotalSeconds)
            : 0;

        _resourceMonitorService.Metrics.TryGetValue(instanceId, out InstanceMetrics? metrics);
        var process = _lifecycleService.GetProcess(instanceId);
        IReadOnlyList<string> onlinePlayers = process?.OnlinePlayerNames ?? Array.Empty<string>();

        return new RemoteInstanceStatusDto
        {
            InstanceId = metadata.Id,
            Name = metadata.Name,
            ServerType = metadata.ServerType,
            State = GetState(instanceId),
            IsRunning = isRunning,
            UptimeSeconds = uptimeSeconds,
            PlayerCount = process?.PlayerCount ?? metrics?.PlayerCount ?? 0,
            MaxPlayers = metadata.MaxPlayers,
            OnlinePlayers = onlinePlayers,
            CpuUsage = metrics?.CpuUsage ?? 0,
            RamUsageMb = metrics?.RamUsageMb ?? 0,
            MaxRamMb = metadata.MaxRamMb
        };
    }

    private string GetState(Guid instanceId)
    {
        var process = _lifecycleService.GetProcess(instanceId);
        if (process != null)
        {
            return process.State.ToString();
        }

        if (_lifecycleService.IsWaitingToRestart(instanceId))
        {
            return "Restarting";
        }

        return _lifecycleService.IsRunning(instanceId) ? "Online" : "Offline";
    }
}
