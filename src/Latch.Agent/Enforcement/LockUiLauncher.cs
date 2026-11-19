// 활성 사용자 세션에 Latch.LockUI가 항상 실행되도록 감시하고 없으면 실행
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting.WindowsServices;

namespace Latch.Agent.Enforcement;

public sealed class LockUiLauncher(ILogger<LockUiLauncher> log)
{
    private const string ProcessName = "Latch.LockUI";
    private static readonly string ExePath = Path.Combine(AppContext.BaseDirectory, ProcessName + ".exe");
    private bool _warnedMissing;

    public void EnsureRunning()
    {
        if (!File.Exists(ExePath))
        {
            if (!_warnedMissing) log.LogWarning("LockUI 실행 파일 없음: {Path}", ExePath);
            _warnedMissing = true;
            return;
        }

        // 개발 중 콘솔 실행: 현재 세션에 바로 실행
        if (!WindowsServiceHelpers.IsWindowsService())
        {
            var mine = Process.GetCurrentProcess().SessionId;
            if (!IsRunningIn(mine)) Process.Start(new ProcessStartInfo(ExePath) { UseShellExecute = true });
            return;
        }

        var session = WTSGetActiveConsoleSessionId();
        if (session == uint.MaxValue || IsRunningIn((int)session)) return;
        if (StartInSession(session)) log.LogInformation("LockUI 실행 (세션 {Session})", session);
    }

    private static bool IsRunningIn(int sessionId)
    {
        var procs = Process.GetProcessesByName(ProcessName);
        try { return procs.Any(p => p.SessionId == sessionId); }
        finally { foreach (var p in procs) p.Dispose(); }
    }

    /// <summary>SYSTEM 서비스에서 로그인한 사용자 권한으로 프로세스 실행.</summary>
    private bool StartInSession(uint session)
    {
        // 로그인한 사용자가 없으면 실패 (로그온 화면) — 다음 주기에 다시 시도
        if (!WTSQueryUserToken(session, out var userToken)) return false;

        nint primary = 0, env = 0;
        try
        {
            if (!DuplicateTokenEx(userToken, MaximumAllowed, 0, SecurityImpersonation, TokenPrimary, out primary))
                return Fail("DuplicateTokenEx");
            if (!CreateEnvironmentBlock(out env, primary, false))
                return Fail("CreateEnvironmentBlock");

            var si = new StartupInfo { cb = Marshal.SizeOf<StartupInfo>(), lpDesktop = @"winsta0\default" };
            if (!CreateProcessAsUser(primary, null, $"\"{ExePath}\"", 0, 0, false, CreateUnicodeEnvironment,
                    env, Path.GetDirectoryName(ExePath), ref si, out var pi))
                return Fail("CreateProcessAsUser");

            CloseHandle(pi.hThread);
            CloseHandle(pi.hProcess);
            return true;
        }
        finally
        {
            if (env != 0) DestroyEnvironmentBlock(env);
            if (primary != 0) CloseHandle(primary);
            CloseHandle(userToken);
        }
    }

    private bool Fail(string api)
    {
        log.LogWarning("{Api} 실패: {Error}", api, Marshal.GetLastPInvokeError());
        return false;
    }

    // ── Win32 ──────────────────────────────────────────────
    private const uint MaximumAllowed = 0x02000000;
    private const int SecurityImpersonation = 2;
    private const int TokenPrimary = 1;
    private const uint CreateUnicodeEnvironment = 0x00000400;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct StartupInfo
    {
        public int cb;
        public string? lpReserved, lpDesktop, lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public nint lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        public nint hProcess, hThread;
        public int dwProcessId, dwThreadId;
    }

    [DllImport("kernel32.dll")]
    private static extern uint WTSGetActiveConsoleSessionId();

    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSQueryUserToken(uint sessionId, out nint token);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool DuplicateTokenEx(nint token, uint access, nint attributes,
        int impersonationLevel, int tokenType, out nint newToken);

    [DllImport("userenv.dll", SetLastError = true)]
    private static extern bool CreateEnvironmentBlock(out nint env, nint token, bool inherit);

    [DllImport("userenv.dll")]
    private static extern bool DestroyEnvironmentBlock(nint env);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcessAsUser(nint token, string? appName, string cmdLine,
        nint processAttributes, nint threadAttributes, bool inheritHandles, uint flags, nint env,
        string? currentDir, ref StartupInfo startupInfo, out ProcessInformation processInfo);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(nint handle);
}
