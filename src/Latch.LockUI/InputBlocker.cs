// 저수준 키보드·마우스 훅으로 잠금 중 모든 입력을 삼킴 (Ctrl+Alt+Del은 OS가 처리하므로 차단 불가)
using System.Runtime.InteropServices;

namespace Latch.LockUI;

public static class InputBlocker
{
    private const int WhKeyboardLl = 13, WhMouseLl = 14;
    private delegate nint HookProc(int code, nint wParam, nint lParam);

    // 델리게이트가 GC되면 훅이 크래시하므로 필드로 붙잡아 둔다
    private static readonly HookProc Swallow = (code, w, l) => code >= 0 ? 1 : CallNextHookEx(0, code, w, l);
    private static nint _keyboard, _mouse;

    public static void Install()
    {
        if (_keyboard != 0) return;
        var module = GetModuleHandle(null);
        _keyboard = SetWindowsHookEx(WhKeyboardLl, Swallow, module, 0);
        _mouse = SetWindowsHookEx(WhMouseLl, Swallow, module, 0);
    }

    public static void Uninstall()
    {
        if (_keyboard != 0) UnhookWindowsHookEx(_keyboard);
        if (_mouse != 0) UnhookWindowsHookEx(_mouse);
        _keyboard = _mouse = 0;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, HookProc fn, nint hMod, uint threadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(nint hook);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hook, int code, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string? name);
}
