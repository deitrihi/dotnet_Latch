// 에이전트의 Bearer 디바이스 토큰을 검증하는 인증 핸들러
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Latch.Server.Services;

public class DeviceAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    DeviceService devices)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Device";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? token = null;
        var header = Request.Headers.Authorization.ToString();
        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            token = header["Bearer ".Length..].Trim();
        else if (Request.Query.TryGetValue("access_token", out var q))
            token = q.ToString();

        if (string.IsNullOrEmpty(token)) return AuthenticateResult.NoResult();

        var device = await devices.FindByTokenAsync(token);
        if (device is null) return AuthenticateResult.Fail("Unknown device token");

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, device.Id.ToString()), new Claim(ClaimTypes.Name, device.Name)],
            SchemeName);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}
