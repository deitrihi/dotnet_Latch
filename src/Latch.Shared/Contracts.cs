// 서버와 에이전트가 주고받는 정책·감시·페어링 메시지 계약
namespace Latch.Shared;

/// <summary>서버가 에이전트에 내려보내는 자녀 PC 정책. Version이 클수록 최신.</summary>
public sealed record DevicePolicy(
    long Version,
    bool Locked,
    string LockMessage,
    string[] BlockedApps,
    string[] BlockedDomains)
{
    public static DevicePolicy Empty { get; } = new(0, false, "", [], []);
}

/// <summary>자녀 PC의 TCP 연결 한 건.</summary>
public sealed record ConnectionInfo(
    int Pid,
    string ProcessName,
    string LocalEndpoint,
    string RemoteAddress,
    int RemotePort,
    string State);

/// <summary>에이전트가 주기적으로 보고하는 연결 목록.</summary>
public sealed record ConnectionSnapshot(DateTimeOffset CapturedAt, ConnectionInfo[] Connections);

/// <summary>에이전트 최초 등록 요청 (웹에서 발급한 일회용 코드 사용).</summary>
public sealed record PairRequest(string Code, string MachineName);

/// <summary>등록 성공 시 발급되는 장기 디바이스 토큰.</summary>
public sealed record PairResponse(Guid DeviceId, string Token);

/// <summary>SignalR 허브 경로와 메서드 이름.</summary>
public static class AgentHub
{
    public const string Path = "/hubs/agent";
    public const string PairPath = "/api/pair";

    /// <summary>서버 → 에이전트: 정책 적용.</summary>
    public const string ApplyPolicy = nameof(ApplyPolicy);

    /// <summary>에이전트 → 서버: 연결 목록 보고.</summary>
    public const string ReportConnections = nameof(ReportConnections);

    /// <summary>에이전트 → 서버: 현재 정책 요청.</summary>
    public const string GetPolicy = nameof(GetPolicy);
}
