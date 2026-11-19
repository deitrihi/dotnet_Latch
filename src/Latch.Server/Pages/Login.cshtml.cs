// 부모 비밀번호를 검증하고 인증 쿠키를 발급하는 로그인 페이지
using System.Security.Claims;
using Latch.Server.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Latch.Server.Pages;

[EnableRateLimiting("auth")]
public class LoginModel(LatchDb db, IPasswordHasher<AdminAccount> hasher) : PageModel
{
    [BindProperty] public string Password { get; set; } = "";
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync() =>
        await db.Admins.AnyAsync() ? Page() : RedirectToPage("/Setup");

    public async Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        var admin = await db.Admins.OrderBy(a => a.Id).FirstOrDefaultAsync();
        if (admin is null) return RedirectToPage("/Setup");

        if (hasher.VerifyHashedPassword(admin, admin.PasswordHash, Password) == PasswordVerificationResult.Failed)
        {
            Error = "비밀번호가 올바르지 않습니다.";
            return Page();
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "parent")],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }
}
