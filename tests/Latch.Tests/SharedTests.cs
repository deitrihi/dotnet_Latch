// Latch.Shared 계약 직렬화와 입력 정규화 규칙 테스트
using System.Text.Json;
using Latch.Shared;

namespace Latch.Tests;

public class SharedTests
{
    [Theory]
    [InlineData("youtube.com", "youtube.com")]
    [InlineData("https://www.YouTube.com/watch?v=1", "youtube.com")]
    [InlineData("  m.naver.com:443  ", "m.naver.com")]
    [InlineData("roblox.com.", "roblox.com")]
    [InlineData("localhost", null)]
    [InlineData("not a domain", null)]
    [InlineData("", null)]
    public void NormalizeDomain(string input, string? expected) =>
        Assert.Equal(expected, PolicyRules.NormalizeDomain(input));

    [Theory]
    [InlineData("Discord.exe", "Discord.exe")]
    [InlineData("\"C:\\Games\\game.exe\"", "C:\\Games\\game.exe")]
    [InlineData("notepad", null)]
    public void NormalizeApp(string input, string? expected) =>
        Assert.Equal(expected, PolicyRules.NormalizeApp(input));

    [Fact]
    public void NormalizeList_DropsInvalidAndDuplicates()
    {
        var result = PolicyRules.NormalizeList(
            ["youtube.com", "www.youtube.com", "bad", "Roblox.com"], PolicyRules.NormalizeDomain);
        Assert.Equal(["youtube.com", "roblox.com"], result);
    }

    [Fact]
    public void DevicePolicy_RoundTripsThroughJson()
    {
        var policy = new DevicePolicy(3, true, "숙제 시간", ["Discord.exe"], ["youtube.com"]);
        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var back = JsonSerializer.Deserialize<DevicePolicy>(JsonSerializer.Serialize(policy, opts), opts)!;

        Assert.Equal(policy.Version, back.Version);
        Assert.Equal(policy.Locked, back.Locked);
        Assert.Equal(policy.LockMessage, back.LockMessage);
        Assert.Equal(policy.BlockedApps, back.BlockedApps);
        Assert.Equal(policy.BlockedDomains, back.BlockedDomains);
    }
}
