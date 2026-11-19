// 서버와 SignalR 연결을 유지하며 정책 수신·연결 목록 보고·주기 점검을 수행하는 백그라운드 작업
using Latch.Agent.Monitoring;
using Latch.Shared;
using Microsoft.AspNetCore.SignalR.Client;

namespace Latch.Agent;

public sealed class AgentWorker(PolicyEnforcer enforcer, ILogger<AgentWorker> log) : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan ReportInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        AgentStore.EnsureDirectories();
        enforcer.ApplyStored();

        var config = AgentStore.LoadConfig();
        if (config is null)
            log.LogWarning("페어링되지 않았습니다. 관리자 권한으로 'Latch.Agent.exe pair <서버주소> <코드>'를 실행하세요.");

        await using var hub = config is null ? null : BuildHub(config);

        var backoff = TickInterval;
        var nextConnect = DateTime.UtcNow;
        var nextReport = DateTime.UtcNow;
        using var timer = new PeriodicTimer(TickInterval);
        do
        {
            enforcer.Tick();
            if (hub is null) continue;

            if (hub.State == HubConnectionState.Disconnected && DateTime.UtcNow >= nextConnect)
            {
                try
                {
                    await hub.StartAsync(ct);
                    enforcer.Apply(await hub.InvokeAsync<DevicePolicy>(AgentHub.GetPolicy, ct));
                    log.LogInformation("서버 연결됨: {Url}", config!.ServerUrl);
                    backoff = TickInterval;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    log.LogWarning("서버 연결 실패 ({Delay}초 후 재시도): {Message}", backoff.TotalSeconds, e.Message);
                    nextConnect = DateTime.UtcNow + backoff;
                    backoff = TimeSpan.FromTicks(Math.Min(backoff.Ticks * 2, MaxBackoff.Ticks));
                }
            }

            if (hub.State == HubConnectionState.Connected && DateTime.UtcNow >= nextReport)
            {
                nextReport = DateTime.UtcNow + ReportInterval;
                try { await hub.InvokeAsync(AgentHub.ReportConnections, TcpConnections.Capture(), ct); }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    log.LogWarning("연결 목록 보고 실패: {Message}", e.Message);
                }
            }
        } while (await timer.WaitForNextTickAsync(ct));
    }

    private HubConnection BuildHub(AgentConfig config)
    {
        var hub = new HubConnectionBuilder()
            .WithUrl(new Uri(new Uri(config.ServerUrl), AgentHub.Path),
                o => o.AccessTokenProvider = () => Task.FromResult<string?>(config.Token))
            .Build();

        hub.On<DevicePolicy>(AgentHub.ApplyPolicy, p => enforcer.Apply(p));
        hub.Closed += e =>
        {
            log.LogWarning("서버 연결 끊김: {Message}", e?.Message);
            return Task.CompletedTask;
        };
        return hub;
    }
}
