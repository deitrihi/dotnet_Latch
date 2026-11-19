// 현재 로그인 사용자가 관리자 계정인지 판별 (관리자 세션은 비상 해제 경로로 잠그지 않음)
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Latch.LockUI;

public static class UserCheck
{
    private const int TokenElevationType = 18;
    private const int ElevationTypeLimited = 3; // UAC로 권한이 걸러진 관리자 토큰

    public static bool IsAdminUser()
    {
        using var id = WindowsIdentity.GetCurrent();
        if (new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator)) return true;

        return GetTokenInformation(id.Token, TokenElevationType, out var type, sizeof(int), out _)
               && type == ElevationTypeLimited;
    }

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetTokenInformation(nint token, int infoClass, out int info, int length, out int returned);
}
