// Latch 서버 진입점: DB, 인증, SignalR, Razor Pages, 리버스 프록시 설정
using System.Threading.RateLimiting;
using Latch.Server.Data;
using Latch.Server.Hubs;
using Latch.Server.Services;
using Latch.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Docker에서는 Latch__DataDir=/data 로 NAS 볼륨을 지정
var dataDir = Path.GetFullPath(builder.Configuration["Latch:DataDir"] ?? "data");
Directory.CreateDirectory(dataDir);

builder.Services.AddDbContext<LatchDb>(o => o.UseSqlite($"Data Source={Path.Combine(dataDir, "latch.db")}"));
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDir, "keys")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ConnectionTracker>();
builder.Services.AddScoped<DeviceService>();
builder.Services.AddSingleton<IPasswordHasher<AdminAccount>, PasswordHasher<AdminAccount>>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Login";
        o.ExpireTimeSpan = TimeSpan.FromDays(14);
        o.SlidingExpiration = true;
    })
    .AddScheme<AuthenticationSchemeOptions, DeviceAuthHandler>(DeviceAuthHandler.SchemeName, null);
builder.Services.AddAuthorization();

builder.Services.AddRazorPages(o =>
{
    o.Conventions.AuthorizeFolder("/");
    o.Conventions.AllowAnonymousToPage("/Login");
    o.Conventions.AllowAnonymousToPage("/Setup");
});
builder.Services.AddSignalR();

// 인터넷 노출 대비: 로그인·페어링 시도 횟수 제한
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddFixedWindowLimiter("auth", l =>
    {
        l.PermitLimit = 10;
        l.Window = TimeSpan.FromMinutes(1);
        l.QueueLimit = 0;
    });
});

// Nginx Proxy Manager 뒤에서 원래 스킴(https)·클라이언트 IP 인식
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<LatchDb>().Database.EnsureCreated();

app.UseForwardedHeaders();
app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost(AgentHub.PairPath, async (PairRequest req, DeviceService devices) =>
        await devices.PairAsync(req) is { } res ? Results.Ok(res) : Results.BadRequest("유효하지 않거나 만료된 코드입니다."))
    .RequireRateLimiting("auth");

app.MapHub<DeviceHub>(AgentHub.Path);
app.MapRazorPages();

app.Run();

public partial class Program;
