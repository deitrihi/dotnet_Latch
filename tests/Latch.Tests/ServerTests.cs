// 서버 통합 테스트: 페어링, 디바이스 토큰 인증, 정책 push, 연결 보고
using System.Net;
using System.Net.Http.Json;
using Latch.Server.Data;
using Latch.Server.Services;
using Latch.Shared;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Latch.Tests;

public sealed class ServerTests : IDisposable
{
    private readonly string _dataDir = Path.Combine(Path.GetTempPath(), "latch-test-" + Guid.NewGuid());
    private readonly WebApplicationFactory<Program> _factory;

    public ServerTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseSetting("Latch:DataDir", _dataDir));
    }

    public void Dispose()
    {
        _factory.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { Directory.Delete(_dataDir, true); } catch (IOException) { }
    }

    private async Task<PairResponse> PairAsync()
    {
        string code;
        using (var scope = _factory.Services.CreateScope())
            code = (await scope.ServiceProvider.GetRequiredService<DeviceService>().CreatePairingCodeAsync()).Code;

        var res = await _factory.CreateClient().PostAsJsonAsync(AgentHub.PairPath, new PairRequest(code, "KID-PC"));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<PairResponse>())!;
    }

    private HubConnection CreateHub(string token) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, AgentHub.Path), o =>
            {
                o.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                o.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
                o.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();

    [Fact]
    public async Task Pair_WithUnknownCode_IsRejected()
    {
        var res = await _factory.CreateClient().PostAsJsonAsync(AgentHub.PairPath, new PairRequest("NOPE1234", "x"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task PairingCode_IsSingleUse()
    {
        string code;
        using (var scope = _factory.Services.CreateScope())
            code = (await scope.ServiceProvider.GetRequiredService<DeviceService>().CreatePairingCodeAsync()).Code;

        var client = _factory.CreateClient();
        Assert.True((await client.PostAsJsonAsync(AgentHub.PairPath, new PairRequest(code, "a"))).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync(AgentHub.PairPath, new PairRequest(code, "b"))).StatusCode);
    }

    [Fact]
    public async Task Hub_RejectsUnknownToken()
    {
        await using var hub = CreateHub("bogus");
        await Assert.ThrowsAnyAsync<Exception>(() => hub.StartAsync());
    }

    [Fact]
    public async Task Hub_PushesPolicyAndStoresSnapshot()
    {
        var pair = await PairAsync();
        await using var hub = CreateHub(pair.Token);
        var pushed = new TaskCompletionSource<DevicePolicy>(TaskCreationOptions.RunContinuationsAsynchronously);
        hub.On<DevicePolicy>(AgentHub.ApplyPolicy, p => pushed.TrySetResult(p));
        await hub.StartAsync();

        var initial = await hub.InvokeAsync<DevicePolicy>(AgentHub.GetPolicy);
        Assert.False(initial.Locked);

        using (var scope = _factory.Services.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<DeviceService>();
            await svc.UpdatePolicyAsync(pair.DeviceId, d => { d.Locked = true; d.BlockedDomains = "youtube.com"; });
        }

        var policy = await pushed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(policy.Locked);
        Assert.Equal(["youtube.com"], policy.BlockedDomains);
        Assert.Equal(initial.Version + 1, policy.Version);

        var snapshot = new ConnectionSnapshot(DateTimeOffset.UtcNow,
            [new ConnectionInfo(42, "chrome", "10.0.0.2:5000", "142.250.0.1", 443, "Established")]);
        await hub.InvokeAsync(AgentHub.ReportConnections, snapshot);

        using (var scope = _factory.Services.CreateScope())
        {
            var device = await scope.ServiceProvider.GetRequiredService<LatchDb>().Devices.FindAsync(pair.DeviceId);
            Assert.Equal("chrome", DeviceService.ReadSnapshot(device!)!.Connections.Single().ProcessName);
            Assert.True(_factory.Services.GetRequiredService<ConnectionTracker>().IsOnline(pair.DeviceId));
        }
    }

    [Fact]
    public async Task WebPages_RequireLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var res = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.StartsWith("/Login", res.Headers.Location!.PathAndQuery);
    }
}
