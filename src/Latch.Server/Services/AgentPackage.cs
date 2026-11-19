// 자녀 PC 설치 패키지(zip)의 위치와 존재 여부·크기 정보를 제공
namespace Latch.Server.Services;

public class AgentPackage(IConfiguration config, IWebHostEnvironment env)
{
    public const string FileName = "latch-agent-win-x64.zip";
    public const string DownloadPath = "/download/agent";

    /// <summary>Docker 이미지에서는 /app/downloads 에 들어 있다. Latch:AgentPackagePath 로 변경 가능.</summary>
    public string FilePath { get; } =
        config["Latch:AgentPackagePath"] ?? Path.Combine(env.ContentRootPath, "downloads", FileName);

    public FileInfo? Info => File.Exists(FilePath) ? new FileInfo(FilePath) : null;
}
