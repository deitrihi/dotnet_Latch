// LockUI 진입점: 사용자 세션당 하나만 실행되는 트레이 앱
namespace Latch.LockUI;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, @"Local\LatchLockUI", out var created);
        if (!created) return;

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayContext());
    }
}
