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

    private string PackagePath => Path.Combine(_dataDir, "pkg.zip");

    public ServerTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b
                .UseSetting("Latch:DataDir", _dataDir)
                .UseSetting("Latch:AgentPackagePath", Path.Combine(_dataDir, "pkg.zip")));
    }

    /// <summary>초기 설정 → 로그인까지 거친 (쿠키를 가진) 클라이언트.</summary>
    private async Task<HttpClient> LoginAsync()
    {
        var client = _factory.CreateClient();
        foreach (var page in new[] { "/Setup", "/Login" })
        {
            var html = await client.GetStringAsync(page);
            var token = System.Text.RegularExpressions.Regex
                .Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
            var form = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Password"] = "test-pass-123",
                ["Confirm"] = "test-pass-123",
            };
            (await client.PostAsync(page, new FormUrlEncodedContent(form))).EnsureSuccessStatusCode();
        }
        return client;
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
    public async Task AgentDownload_RequiresLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var res = await client.GetAsync(AgentPackage.DownloadPath);
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.StartsWith("/Login", res.Headers.Location!.PathAndQuery);
    }

    [Fact]
    public async Task AgentDownload_ServesPackageAfterLogin()
    {
        var client = await LoginAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(AgentPackage.DownloadPath)).StatusCode);
        Assert.Contains("설치 패키지가 없습니다", await client.GetStringAsync("/"));

        Directory.CreateDirectory(_dataDir);
        await File.WriteAllBytesAsync(PackagePath, [0x50, 0x4B, 0x05, 0x06]);

        var res = await client.GetAsync(AgentPackage.DownloadPath);
        res.EnsureSuccessStatusCode();
        Assert.Equal("application/zip", res.Content.Headers.ContentType!.MediaType);
        Assert.Equal(AgentPackage.FileName, res.Content.Headers.ContentDisposition!.FileNameStar);
        Assert.Equal(4, (await res.Content.ReadAsByteArrayAsync()).Length);
        Assert.Contains("설치 파일 받기", await client.GetStringAsync("/"));
    }

    [Fact]
    public async Task IndexLockButton_TogglesLock()
    {
        var pair = await PairAsync();
        var client = await LoginAsync();

        for (var expected = true; ; expected = !expected)
        {
            var html = await client.GetStringAsync("/");
            var form = System.Text.RegularExpressions.Regex.Match(html,
                "<form method=\"post\" action=\"([^\"]*handler=Lock[^\"]*)\">(.*?)</form>",
                System.Text.RegularExpressions.RegexOptions.Singleline);
            Assert.True(form.Success, "목록 화면에 잠금 폼이 없음");

            var fields = System.Text.RegularExpressions.Regex
                .Matches(form.Groups[2].Value, "name=\"([^\"]+)\"[^>]*value=\"([^\"]*)\"")
                .ToDictionary(m => m.Groups[1].Value, m => System.Net.WebUtility.HtmlDecode(m.Groups[2].Value));
            var res = await client.PostAsync(System.Net.WebUtility.HtmlDecode(form.Groups[1].Value),
                new FormUrlEncodedContent(fields));
            res.EnsureSuccessStatusCode();

            using var scope = _factory.Services.CreateScope();
            var device = await scope.ServiceProvider.GetRequiredService<LatchDb>().Devices.FindAsync(pair.DeviceId);
            Assert.Equal(expected, device!.Locked);
            if (!expected) break;
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
