// 트레이 아이콘으로 관리 중임을 표시하고, 상태 파일을 감시해 잠금 화면을 켜고 끄는 앱 컨텍스트
using System.Text.Json;
using Latch.Shared;

namespace Latch.LockUI;

public sealed class TrayContext : ApplicationContext
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly NotifyIcon _tray;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    private readonly bool _exempt = UserCheck.IsAdminUser();
    private readonly List<LockForm> _forms = [];
    private LocalUiState _state = new(false, "");

    public TrayContext()
    {
        _tray = new NotifyIcon
        {
            Icon = SystemIcons.Shield,
            Text = _exempt ? "Latch — 관리자 계정 (잠금 제외)" : "Latch — 부모님이 관리 중인 PC입니다",
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip(),
        };
        _tray.ContextMenuStrip.Items.Add(new ToolStripMenuItem("이 PC는 Latch로 관리되고 있습니다") { Enabled = false });

        _timer.Tick += (_, _) => Refresh();
        _timer.Start();
        Refresh();
    }

    private void Refresh()
    {
        var next = ReadState() ?? _state;
        var shouldLock = next.Locked && !_exempt;

        if (shouldLock && _forms.Count == 0) ShowLock(next.Message);
        else if (!shouldLock && _forms.Count > 0) HideLock();
        else if (shouldLock && next.Message != _state.Message) _forms.ForEach(f => f.SetMessage(next.Message));

        if (_forms.Count > 0)
        {
            // 다른 창이 위로 올라오는 경우 대비
            foreach (var f in _forms) f.TopMost = true;
            _forms[0].Activate();
        }
        _state = next;
    }

    /// <summary>파일이 없으면 잠금 해제. 일시적 읽기 오류면 null (이전 상태 유지 — 오류로 풀리지 않게).</summary>
    private static LocalUiState? ReadState()
    {
        try
        {
            if (!File.Exists(LocalUiState.FilePath)) return new LocalUiState(false, "");
            return JsonSerializer.Deserialize<LocalUiState>(File.ReadAllText(LocalUiState.FilePath), Json);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    private void ShowLock(string message)
    {
        foreach (var screen in Screen.AllScreens)
        {
            var form = new LockForm(screen, message);
            form.Show();
            _forms.Add(form);
        }
        InputBlocker.Install();
    }

    private void HideLock()
    {
        InputBlocker.Uninstall();
        foreach (var f in _forms) f.ForceClose();
        _forms.Clear();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            HideLock();
            _timer.Dispose();
            _tray.Dispose();
        }
        base.Dispose(disposing);
    }
}
