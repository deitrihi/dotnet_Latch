// Windows 방화벽 COM API로 차단 앱의 아웃바운드 연결을 막는 규칙을 동기화
using System.Diagnostics;

namespace Latch.Agent.Enforcement;

public sealed class FirewallBlocker(ILogger<FirewallBlocker> log)
{
    public const string Group = "Latch";
    private const int DirectionOut = 2;
    private const int ActionBlock = 0;
    private const int AllProfiles = 0x7FFFFFFF;

    /// <summary>현재 차단 목록에 맞게 Latch 그룹 규칙을 추가/삭제.</summary>
    public void Sync(IReadOnlyCollection<string> blockedApps)
    {
        dynamic policy = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2")!)!;

        var existing = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // path → rule name
        foreach (dynamic rule in policy.Rules)
            if ((string?)rule.Grouping == Group && rule.ApplicationName is string app)
                existing[app] = (string)rule.Name;

        var desired = ComputeDesired(blockedApps, existing.Keys, FindRunningPaths(blockedApps));

        foreach (var (path, name) in existing)
            if (!desired.Contains(path))
            {
                policy.Rules.Remove(name);
                log.LogInformation("방화벽 차단 해제: {Path}", path);
            }

        foreach (var path in desired.Where(p => !existing.ContainsKey(p)))
        {
            dynamic rule = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule")!)!;
            rule.Name = $"Latch: {path}";
            rule.Grouping = Group;
            rule.Description = "Latch 자녀 PC 관리 — 웹에서 설정한 앱 차단";
            rule.ApplicationName = path;
            rule.Direction = DirectionOut;
            rule.Action = ActionBlock;
            rule.Profiles = AllProfiles;
            rule.Enabled = true;
            policy.Rules.Add(rule);
            log.LogInformation("방화벽 차단: {Path}", path);
        }
    }

    /// <summary>
    /// 차단해야 할 실행 파일 경로 집합.
    /// - 전체 경로 항목은 그대로.
    /// - 이름 항목(Discord.exe)은 실행 중이거나 이미 규칙이 있는 같은 이름의 경로.
    /// </summary>
    public static HashSet<string> ComputeDesired(
        IEnumerable<string> entries, IEnumerable<string> existingPaths, IEnumerable<string> runningPaths)
    {
        var desired = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries)
        {
            if (IsFullPath(e)) desired.Add(e);
            else names.Add(e);
        }
        foreach (var p in existingPaths.Concat(runningPaths))
            if (names.Contains(Path.GetFileName(p))) desired.Add(p);
        return desired;
    }

    public static bool IsFullPath(string entry) => entry.Contains('\\') || entry.Contains('/');

    private static IEnumerable<string> FindRunningPaths(IEnumerable<string> entries)
    {
        foreach (var name in entries.Where(e => !IsFullPath(e)))
            foreach (var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(name)))
            {
                string? path = null;
                try { path = p.MainModule?.FileName; }
                catch (Exception) { /* 보호된 프로세스 등 경로 조회 불가 */ }
                finally { p.Dispose(); }
                if (path is not null) yield return path;
            }
    }
}
