// 디바이스 상세 페이지 모델: 잠금/차단 목록 변경, 연결 목록 조회, 삭제
using Latch.Server.Data;
using Latch.Server.Services;
using Latch.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Latch.Server.Pages;

public class DeviceModel(LatchDb db, DeviceService devices, ConnectionTracker tracker) : PageModel
{
    public Device Device { get; private set; } = null!;
    public bool Online { get; private set; }
    public ConnectionSnapshot? Snapshot { get; private set; }
    public string[] Dropped { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid id) => await LoadAsync(id) ? Page() : NotFound();

    public async Task<IActionResult> OnPostLockAsync(Guid id, bool locked, string? message, string? back)
    {
        var ok = await devices.UpdatePolicyAsync(id, d =>
        {
            d.Locked = locked;
            d.LockMessage = (message ?? "").Trim();
        });
        if (!ok) return NotFound();
        return back == "index" ? RedirectToPage("/Index") : RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostListsAsync(Guid id, string? apps, string? domains)
    {
        var appLines = DeviceService.SplitLines(apps ?? "");
        var domainLines = DeviceService.SplitLines(domains ?? "");
        var cleanApps = PolicyRules.NormalizeList(appLines, PolicyRules.NormalizeApp);
        var cleanDomains = PolicyRules.NormalizeList(domainLines, PolicyRules.NormalizeDomain);

        var ok = await devices.UpdatePolicyAsync(id, d =>
        {
            d.BlockedApps = string.Join('\n', cleanApps);
            d.BlockedDomains = string.Join('\n', cleanDomains);
        });
        if (!ok) return NotFound();

        // 정규화에서 버려진 입력은 화면에 알려준다
        Dropped = appLines.Where(l => PolicyRules.NormalizeApp(l) is null)
            .Concat(domainLines.Where(l => PolicyRules.NormalizeDomain(l) is null))
            .ToArray();
        if (Dropped.Length == 0) return RedirectToPage(new { id });

        await LoadAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var device = await db.Devices.FindAsync(id);
        if (device is not null)
        {
            db.Devices.Remove(device);
            await db.SaveChangesAsync();
        }
        return RedirectToPage("/Index");
    }

    private async Task<bool> LoadAsync(Guid id)
    {
        var device = await db.Devices.FindAsync(id);
        if (device is null) return false;
        Device = device;
        Online = tracker.IsOnline(id);
        Snapshot = DeviceService.ReadSnapshot(device);
        return true;
    }
}
