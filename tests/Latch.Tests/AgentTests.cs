// 에이전트 테스트: hosts 관리 구역, 방화벽 대상 계산, TCP 테이블 파싱, 방화벽 COM 조회
using System.Net.NetworkInformation;
using Latch.Agent.Enforcement;
using Latch.Agent.Monitoring;
using Microsoft.Extensions.Logging.Abstractions;

namespace Latch.Tests;

public class AgentTests
{
    private const string Original = "# Copyright\r\n127.0.0.1 myrouter\r\n";

    [Fact]
    public void Hosts_AddsManagedBlock_AndKeepsUserLines()
    {
        var result = HostsFile.Rewrite(Original, ["youtube.com"]);

        Assert.StartsWith(Original.TrimEnd(), result);
        Assert.Contains("0.0.0.0 youtube.com", result);
        Assert.Contains("0.0.0.0 www.youtube.com", result);
        Assert.Contains(":: youtube.com", result);
    }

    [Fact]
    public void Hosts_ReplacesBlock_AndRemovesWhenEmpty()
    {
        var first = HostsFile.Rewrite(Original, ["youtube.com"]);
        var second = HostsFile.Rewrite(first, ["roblox.com"]);

        Assert.DoesNotContain("youtube.com", second);
        Assert.Contains("0.0.0.0 roblox.com", second);
        Assert.Single(second.Split(HostsFile.Begin)[1..]);

        Assert.Equal(Original, HostsFile.Rewrite(second, []));
    }

    [Fact]
    public void Hosts_UntouchedWhenNothingToBlock()
    {
        const string messy = "127.0.0.1 a\n\n\n";
        Assert.Same(messy, HostsFile.Rewrite(messy, []));
    }

    [Fact]
    public void Hosts_RewriteIsIdempotent()
    {
        var once = HostsFile.Rewrite(Original, ["a.com"]);
        Assert.Equal(once, HostsFile.Rewrite(once, ["a.com"]));
    }

    [Fact]
    public void Firewall_Desired_ResolvesNamesAndKeepsFullPaths()
    {
        var desired = FirewallBlocker.ComputeDesired(
            entries: ["Discord.exe", @"C:\Games\game.exe"],
            existingPaths: [@"C:\Old\Discord.exe", @"C:\Other\notepad.exe"],
            runningPaths: [@"C:\Users\kid\AppData\Discord\Discord.exe"]);

        Assert.Equal(
            new[] { @"C:\Games\game.exe", @"C:\Old\Discord.exe", @"C:\Users\kid\AppData\Discord\Discord.exe" }.Order(),
            desired.Order());
    }

    [Fact]
    public void Firewall_Desired_IsEmptyWhenNothingBlocked() =>
        Assert.Empty(FirewallBlocker.ComputeDesired([], [@"C:\Old\Discord.exe"], []));

    [Fact]
    public void Firewall_CanEnumerateRules()
    {
        // 기존 Latch 규칙이 없으면 조회만 수행 (관리자 권한 불필요)
        new FirewallBlocker(NullLogger<FirewallBlocker>.Instance).Sync([]);
    }

    [Fact]
    public void Tcp_CaptureMatchesDotNetView()
    {
        var snapshot = TcpConnections.Capture();
        Assert.All(snapshot.Connections, c => Assert.InRange(c.RemotePort, 1, 65535));

        var expected = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections()
            .Where(c => c.State == TcpState.Established && !System.Net.IPAddress.IsLoopback(c.RemoteEndPoint.Address))
            .Select(c => (c.RemoteEndPoint.Address.ToString(), c.RemoteEndPoint.Port))
            .ToHashSet();
        if (expected.Count == 0) return; // 외부 연결이 없는 환경

        var ours = snapshot.Connections.Select(c => (c.RemoteAddress, c.RemotePort));
        Assert.Contains(ours, expected.Contains);
    }
}
