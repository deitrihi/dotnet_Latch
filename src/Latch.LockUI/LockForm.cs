// 모니터 하나를 덮는 전체화면 잠금 창 (사용자가 닫을 수 없음)
namespace Latch.LockUI;

public sealed class LockForm : Form
{
    private const int WsExToolWindow = 0x80; // Alt+Tab 목록에서 숨김
    private readonly Label _label;
    private bool _allowClose;

    public LockForm(Screen screen, string message)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = screen.Bounds;
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = Color.FromArgb(18, 20, 26);
        ForeColor = Color.White;

        _label = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Malgun Gothic", 26, FontStyle.Bold),
        };
        Controls.Add(_label);
        SetMessage(message);
    }

    public void SetMessage(string message) =>
        _label.Text = "🔒 이 PC는 잠겨 있습니다\n\n"
                      + (string.IsNullOrWhiteSpace(message) ? "" : message + "\n\n")
                      + "부모님이 잠금을 해제하면 다시 사용할 수 있어요.";

    public void ForceClose()
    {
        _allowClose = true;
        Close();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WsExToolWindow;
            return cp;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Alt+F4 등 사용자 닫기 차단. Windows 종료·로그오프는 허용.
        if (!_allowClose && e.CloseReason == CloseReason.UserClosing) e.Cancel = true;
        base.OnFormClosing(e);
    }
}
