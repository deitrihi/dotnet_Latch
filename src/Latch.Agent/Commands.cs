// 관리자용 명령줄 기능: 서버 페어링(pair), 모든 차단 해제(reset)
using System.Net.Http.Json;
using System.Security.Principal;
using Latch.Agent.Enforcement;
using Latch.Shared;
using Microsoft.Extensions.Logging.Abstractions;

namespace Latch.Agent;

public static class Commands
{
    public static async Task<int> PairAsync(string serverUrl, string code)
    {
        if (!RequireAdmin()) return 1;
        if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out var baseUri))
        {
            Console.Error.WriteLine($"잘못된 서버 주소: {serverUrl}");
            return 1;
        }

        using var http = new HttpClient { BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(20) };
        HttpResponseMessage res;
        try
        {
            res = await http.PostAsJsonAsync(AgentHub.PairPath, new PairRequest(code, Environment.MachineName));
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            Console.Error.WriteLine($"서버에 연결할 수 없습니다 ({baseUri}): {e.Message}");
            return 1;
        }
        if (!res.IsSuccessStatusCode)
        {
            Console.Error.WriteLine($"페어링 실패 ({(int)res.StatusCode}): {await res.Content.ReadAsStringAsync()}");
            return 1;
        }

        var pair = (await res.Content.ReadFromJsonAsync<PairResponse>())!;
        AgentStore.EnsureDirectories();
        AgentStore.SaveConfig(new AgentConfig(baseUri.ToString(), pair.DeviceId, pair.Token));
        Console.WriteLine($"페어링 완료 (디바이스 {pair.DeviceId}). 서비스를 재시작하세요: sc stop LatchAgent && sc start LatchAgent");
        return 0;
    }

    /// <summary>제거 전 정리: 방화벽 규칙·hosts 구역 삭제, 잠금 해제.</summary>
    public static int Reset()
    {
        if (!RequireAdmin()) return 1;
        AgentStore.EnsureDirectories();
        new FirewallBlocker(NullLogger<FirewallBlocker>.Instance).Sync([]);
        HostsFile.Apply([]);
        AgentStore.DeletePolicy();
        AgentStore.WriteUiState(new LocalUiState(false, ""));
        Console.WriteLine("방화벽 규칙과 hosts 차단을 모두 제거했습니다.");
        return 0;
    }

    private static bool RequireAdmin()
    {
        if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)) return true;
        Console.Error.WriteLine("관리자 권한으로 실행해야 합니다.");
        return false;
    }
}
