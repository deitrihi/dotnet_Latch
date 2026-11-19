// hosts 파일의 Latch 관리 구역을 갱신해 도메인을 차단
using System.Runtime.InteropServices;

namespace Latch.Agent.Enforcement;

public static partial class HostsFile
{
    public const string Begin = "# >>> Latch managed block (do not edit) >>>";
    public const string End = "# <<< Latch managed block <<<";

    public static string SystemPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");

    /// <summary>기존 내용에서 관리 구역만 교체한 새 hosts 내용. 관리 구역 밖은 그대로 둔다.</summary>
    public static string Rewrite(string existing, IReadOnlyCollection<string> domains)
    {
        var kept = new List<string>();
        var inBlock = false;
        foreach (var line in existing.Replace("\r\n", "\n").Split('\n'))
        {
            var t = line.Trim();
            if (t == Begin) { inBlock = true; continue; }
            if (inBlock) { if (t == End) inBlock = false; continue; }
            kept.Add(line);
        }
        while (kept.Count > 0 && kept[^1].Trim().Length == 0) kept.RemoveAt(kept.Count - 1);

        if (domains.Count > 0)
        {
            kept.Add("");
            kept.Add(Begin);
            foreach (var d in domains)
                foreach (var host in new[] { d, "www." + d })
                {
                    kept.Add($"0.0.0.0 {host}");
                    kept.Add($":: {host}");
                }
            kept.Add(End);
        }
        return string.Join("\r\n", kept) + "\r\n";
    }

    /// <summary>내용이 바뀐 경우에만 쓰고 DNS 캐시를 비운다. 변경 여부 반환.</summary>
    public static bool Apply(IReadOnlyCollection<string> domains)
    {
        var existing = File.Exists(SystemPath) ? File.ReadAllText(SystemPath) : "";
        var updated = Rewrite(existing, domains);
        if (updated == existing) return false;

        File.WriteAllText(SystemPath, updated);
        DnsFlushResolverCache();
        return true;
    }

    [LibraryImport("dnsapi.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DnsFlushResolverCache();
}
