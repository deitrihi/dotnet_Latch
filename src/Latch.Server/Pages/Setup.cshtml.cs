// 부모 계정이 없을 때만 비밀번호를 생성하는 초기 설정 페이지
using Latch.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Latch.Server.Pages;

[EnableRateLimiting("auth")]
public class SetupModel(LatchDb db, IPasswordHasher<AdminAccount> hasher) : PageModel
{
    [BindProperty] public string Password { get; set; } = "";
    [BindProperty] public string Confirm { get; set; } = "";
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync() =>
        await db.Admins.AnyAsync() ? RedirectToPage("/Login") : Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (await db.Admins.AnyAsync()) return RedirectToPage("/Login");

        if (Password.Length < 8) Error = "비밀번호는 8자 이상이어야 합니다.";
        else if (Password != Confirm) Error = "비밀번호가 일치하지 않습니다.";
        if (Error is not null) return Page();

        var admin = new AdminAccount();
        admin.PasswordHash = hasher.HashPassword(admin, Password);
        db.Admins.Add(admin);
        await db.SaveChangesAsync();
        return RedirectToPage("/Login");
    }
}
