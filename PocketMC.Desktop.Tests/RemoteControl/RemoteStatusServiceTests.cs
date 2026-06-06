using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using PocketMC.Desktop.Core.Interfaces;
using PocketMC.Desktop.Features.Instances.Models;
using PocketMC.Desktop.Features.Instances.Services;
using PocketMC.Desktop.Features.RemoteControl.Models;
using PocketMC.Desktop.Features.RemoteControl.Services;
using PocketMC.Desktop.Features.Shell;
using PocketMC.Desktop.Models;

namespace PocketMC.Desktop.Tests.RemoteControl;

#pragma warning disable CS0067
public sealed class RemoteStatusServiceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "PocketMC.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void GetInstances_ProjectsRegistryAndLifecycleState()
    {
        var metadata = new InstanceMetadata
        {
            Id = Guid.NewGuid(),
            Name = "Survival",
            ServerType = "Paper",
            MaxPlayers = 20
        };

        var lifecycle = new FakeLifecycleService();
        lifecycle.RunningInstances.Add(metadata.Id);
        var service = CreateService(metadata, lifecycle, new FakeResourceMonitorService());

        var instance = Assert.Single(service.GetInstances());

        Assert.Equal(metadata.Id, instance.Id);
        Assert.Equal("Survival", instance.Name);
        Assert.Equal("Paper", instance.ServerType);
        Assert.True(instance.IsRunning);
        Assert.Equal("Online", instance.State);
    }

    [Fact]
    public void GetInstanceStatus_IncludesMetricsAndUptime()
    {
        var metadata = new InstanceMetadata
        {
            Id = Guid.NewGuid(),
            Name = "Survival",
            ServerType = "Paper",
            MaxPlayers = 20,
            MaxRamMb = 4096
        };

        var lifecycle = new FakeLifecycleService();
        lifecycle.RunningInstances.Add(metadata.Id);
        lifecycle.SessionStartTimes[metadata.Id] = DateTime.UtcNow.AddSeconds(-90);
        var monitor = new FakeResourceMonitorService();
        monitor.Metrics[metadata.Id] = new InstanceMetrics
        {
            CpuUsage = 12.5,
            RamUsageMb = 1024.5,
            PlayerCount = 2
        };

        var service = CreateService(metadata, lifecycle, monitor);

        RemoteInstanceStatusDto status = service.GetInstanceStatus(metadata.Id)!;

        Assert.Equal(metadata.Id, status.InstanceId);
        Assert.True(status.IsRunning);
        Assert.InRange(status.UptimeSeconds, 1, 120);
        Assert.Equal(2, status.PlayerCount);
        Assert.Equal(20, status.MaxPlayers);
        Assert.Equal(12.5, status.CpuUsage);
        Assert.Equal(1024.5, status.RamUsageMb);
        Assert.Equal(4096, status.MaxRamMb);
    }

    private RemoteStatusService CreateService(
        InstanceMetadata metadata,
        FakeLifecycleService lifecycle,
        FakeResourceMonitorService monitor)
    {
        Directory.CreateDirectory(_tempDirectory);
        var state = new ApplicationState();
        state.ApplySettings(new AppSettings { AppRootPath = _tempDirectory });
        var registry = new InstanceRegistry(new InstancePathService(state), NullLogger<InstanceRegistry>.Instance);
        string instancePath = Path.Combine(_tempDirectory, "servers", metadata.Name);
        Directory.CreateDirectory(instancePath);
        registry.Register(metadata, instancePath);

        return new RemoteStatusService(registry, lifecycle, monitor);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private sealed class FakeLifecycleService : IServerLifecycleService
    {
        public HashSet<Guid> RunningInstances { get; } = new();
        public Dictionary<Guid, DateTime> SessionStartTimes { get; } = new();

        public event Action<Guid, ServerState>? OnInstanceStateChanged;
        public event Action<Guid, int>? OnRestartCountdownTick;

        public Task StartAsync(InstanceMetadata meta) => Task.CompletedTask;
        public Task StopAsync(Guid instanceId) => Task.CompletedTask;
        public void Kill(Guid instanceId) { }
        public void KillAll() { }
        public bool IsRunning(Guid instanceId) => RunningInstances.Contains(instanceId);
        public bool IsWaitingToRestart(Guid instanceId) => false;
        public void AbortRestartDelay(Guid instanceId) { }
        public Task RestartAsync(Guid instanceId) => Task.CompletedTask;
        public ServerProcess? GetProcess(Guid instanceId) => null;
        public DateTime? GetSessionStartTime(Guid instanceId) =>
            SessionStartTimes.TryGetValue(instanceId, out DateTime start) ? start : null;
        public Task ReleaseInstanceAsync(Guid instanceId) => Task.CompletedTask;
    }

    private sealed class FakeResourceMonitorService : IResourceMonitorService
    {
        public ConcurrentDictionary<Guid, InstanceMetrics> Metrics { get; } = new();
        public GlobalResourceSummary? CurrentSummary => null;
        public event EventHandler<InstanceMetricsUpdatedEventArgs>? InstanceMetricsUpdated;
        public event EventHandler? GlobalMetricsUpdated;
        public double GetTotalCommittedRamMb() => Metrics.Values.Sum(x => x.RamUsageMb);
    }
}
#pragma warning restore CS0067
