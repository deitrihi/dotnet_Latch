// 정책을 hosts·방화벽·잠금 상태에 적용하고, 주기적으로 LockUI와 앱 차단을 재확인
using Latch.Agent.Enforcement;
using Latch.Shared;

namespace Latch.Agent;

public sealed class PolicyEnforcer(FirewallBlocker firewall, LockUiLauncher lockUi, ILogger<PolicyEnforcer> log)
{
    private static readonly TimeSpan FirewallRescan = TimeSpan.FromSeconds(10);
    private readonly Lock _gate = new();
    private DevicePolicy _current = DevicePolicy.Empty;
    private DateTime _lastFirewallSync = DateTime.MinValue;

    /// <summary>서비스 시작 시: 서버 연결 전이라도 마지막 정책을 즉시 적용.</summary>
    public void ApplyStored() => Apply(AgentStore.LoadPolicy() ?? DevicePolicy.Empty, persist: false);

    public void Apply(DevicePolicy policy, bool persist = true)
    {
        lock (_gate)
        {
            _current = policy;
            log.LogInformation("정책 v{Version} 적용: 잠금={Locked}, 앱 {Apps}개, 사이트 {Domains}개",
                policy.Version, policy.Locked, policy.BlockedApps.Length, policy.BlockedDomains.Length);

            Try("정책 저장", () => { if (persist) AgentStore.SavePolicy(policy); });
            Try("잠금 상태", () => AgentStore.WriteUiState(new LocalUiState(policy.Locked, policy.LockMessage)));
            Try("hosts", () => HostsFile.Apply(policy.BlockedDomains));
            SyncFirewall();
        }
    }

    /// <summary>몇 초마다 호출: LockUI 생존 확인, 이름으로 등록된 앱의 새 실행 경로 차단.</summary>
    public void Tick()
    {
        lock (_gate)
        {
            Try("LockUI", lockUi.EnsureRunning);
            if (_current.BlockedApps.Any(a => !FirewallBlocker.IsFullPath(a))
                && DateTime.UtcNow - _lastFirewallSync > FirewallRescan)
                SyncFirewall();
        }
    }

    private void SyncFirewall()
    {
        _lastFirewallSync = DateTime.UtcNow;
        Try("방화벽", () => firewall.Sync(_current.BlockedApps));
    }

    private void Try(string what, Action action)
    {
        try { action(); }
        catch (Exception e) { log.LogError(e, "{What} 적용 실패", what); }
    }
}
