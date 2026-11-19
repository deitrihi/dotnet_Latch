// 차단 목록 입력값(도메인·앱 경로)을 정규화하는 규칙
namespace Latch.Shared;

public static class PolicyRules
{
    /// <summary>
    /// "https://www.YouTube.com/watch" → "youtube.com". 유효하지 않으면 null.
    /// www.는 떼고, 에이전트가 www. 변형을 함께 차단한다.
    /// </summary>
    public static string? NormalizeDomain(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var s = input.Trim().ToLowerInvariant();

        var scheme = s.IndexOf("://", StringComparison.Ordinal);
        if (scheme >= 0) s = s[(scheme + 3)..];

        var end = s.IndexOfAny(['/', '?', '#', ':']);
        if (end >= 0) s = s[..end];

        s = s.TrimEnd('.');
        if (s.StartsWith("www.", StringComparison.Ordinal)) s = s[4..];

        return Uri.CheckHostName(s) == UriHostNameType.Dns && s.Contains('.') ? s : null;
    }

    /// <summary>앱 항목은 실행 파일 이름(예: "Discord.exe") 또는 전체 경로. 비어 있으면 null.</summary>
    public static string? NormalizeApp(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var s = input.Trim().Trim('"');
        return s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? s : null;
    }

    /// <summary>여러 줄 입력을 정규화·중복 제거한다.</summary>
    public static string[] NormalizeList(IEnumerable<string> lines, Func<string?, string?> normalize) =>
        lines.Select(normalize)
             .OfType<string>()
             .Distinct(StringComparer.OrdinalIgnoreCase)
             .ToArray();
}
