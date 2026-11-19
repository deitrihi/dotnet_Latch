// 에이전트(서비스)가 쓰고 LockUI(사용자 세션)가 읽는 로컬 잠금 상태 파일 계약
namespace Latch.Shared;

public sealed record LocalUiState(bool Locked, string Message)
{
    /// <summary>%ProgramData%\Latch\ui — Users 읽기 전용 폴더.</summary>
    public static string Directory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Latch", "ui");

    public static string FilePath { get; } = Path.Combine(Directory, "state.json");
}
