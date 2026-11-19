// 디바이스 목록과 페어링 코드 발급을 처리하는 메인 페이지 모델
using Latch.Server.Data;
using Latch.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Latch.Server.Pages;

public class IndexModel(LatchDb db, DeviceService devices, ConnectionTracker tracker) : PageModel
{
    // 컨테이너에 tzdata가 없어도 동작하도록 고정 오프셋으로 대체
    private static readonly TimeZoneInfo Kst =
        TimeZoneInfo.TryFindSystemTimeZoneById("Asia/Seoul", out var tz)
            ? tz
            : TimeZoneInfo.CreateCustomTimeZone("KST", TimeSpan.FromHours(9), "KST", "KST");

    public ConnectionTracker Tracker => tracker;
    public List<Device> Devices { get; private set; } = [];
    public PairingCode? NewCode { get; private set; }

    public async Task OnGetAsync() => Devices = await LoadAsync();

    public async Task OnPostCreateCodeAsync()
    {
        NewCode = await devices.CreatePairingCodeAsync();
        Devices = await LoadAsync();
    }

    private Task<List<Device>> LoadAsync() => db.Devices.OrderBy(d => d.Name).ToListAsync();

    public static string FormatTime(DateTime? utc) =>
        utc is { } t
            ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(t, DateTimeKind.Utc), Kst).ToString("MM-dd HH:mm")
            : "없음";
}
