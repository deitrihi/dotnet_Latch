// 에이전트 설정·마지막 정책·UI 상태 파일을 권한이 제한된 ProgramData 폴더에 저장
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using Latch.Shared;

namespace Latch.Agent;

public sealed record AgentConfig(string ServerUrl, Guid DeviceId, string Token);

public static class AgentStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static string Root { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Latch");

    private static string ConfigPath => Path.Combine(Root, "config.json");
    private static string PolicyPath => Path.Combine(Root, "policy.json");

    /// <summary>
    /// Root: SYSTEM·Administrators만 접근 (토큰·정책 보호).
    /// ui: 추가로 Users 읽기 허용 (LockUI가 상태를 읽음).
    /// </summary>
    public static void EnsureDirectories()
    {
        CreateRestricted(Root, allowUsersRead: false);
        CreateRestricted(LocalUiState.Directory, allowUsersRead: true);
    }

    public static AgentConfig? LoadConfig() => Read<AgentConfig>(ConfigPath);
    public static void SaveConfig(AgentConfig config) => Write(ConfigPath, config);

    public static DevicePolicy? LoadPolicy() => Read<DevicePolicy>(PolicyPath);
    public static void SavePolicy(DevicePolicy policy) => Write(PolicyPath, policy);
    public static void DeletePolicy() => File.Delete(PolicyPath);

    public static void WriteUiState(LocalUiState state) => Write(LocalUiState.FilePath, state);

    private static T? Read<T>(string path) where T : class
    {
        try { return File.Exists(path) ? JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json) : null; }
        catch (JsonException) { return null; }
    }

    /// <summary>임시 파일에 쓴 뒤 교체 — 읽는 쪽이 반쯤 쓰인 파일을 보지 않게.</summary>
    private static void Write<T>(string path, T value)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(value, Json));
        File.Move(tmp, path, overwrite: true);
    }

    private static void CreateRestricted(string path, bool allowUsersRead)
    {
        const InheritanceFlags inherit = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        foreach (var sid in new[] { WellKnownSidType.LocalSystemSid, WellKnownSidType.BuiltinAdministratorsSid })
            security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(sid, null),
                FileSystemRights.FullControl, inherit, PropagationFlags.None, AccessControlType.Allow));
        if (allowUsersRead)
            security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null),
                FileSystemRights.ReadAndExecute, inherit, PropagationFlags.None, AccessControlType.Allow));

        new DirectoryInfo(path).Create(security);
        new DirectoryInfo(path).SetAccessControl(security);
    }
}
