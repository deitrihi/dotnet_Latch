// 페어링, 디바이스 토큰 검증, 정책 변경과 에이전트 push를 담당하는 서비스
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Latch.Server.Data;
using Latch.Server.Hubs;
using Latch.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Latch.Server.Services;

public class DeviceService(LatchDb db, IHubContext<DeviceHub> hub, TimeProvider clock)
{
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    public static readonly TimeSpan PairingCodeLifetime = TimeSpan.FromMinutes(10);

    public async Task<PairingCode> CreatePairingCodeAsync()
    {
        var now = clock.GetUtcNow().UtcDateTime;
        await db.PairingCodes.Where(p => p.ExpiresAtUtc <= now).ExecuteDeleteAsync();

        var code = new PairingCode
        {
            Code = RandomNumberGenerator.GetString(CodeAlphabet, 8),
            ExpiresAtUtc = now + PairingCodeLifetime,
        };
        db.PairingCodes.Add(code);
        await db.SaveChangesAsync();
        return code;
    }

    /// <summary>유효한 일회용 코드면 디바이스를 만들고 토큰을 발급. 아니면 null.</summary>
    public async Task<PairResponse?> PairAsync(PairRequest req)
    {
        var code = (req.Code ?? "").Trim().ToUpperInvariant().Replace("-", "");
        var now = clock.GetUtcNow().UtcDateTime;
        var entry = await db.PairingCodes.FindAsync(code);
        if (entry is null) return null;

        db.PairingCodes.Remove(entry);
        if (entry.ExpiresAtUtc <= now)
        {
            await db.SaveChangesAsync();
            return null;
        }

        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        var name = (req.MachineName ?? "").Trim();
        var device = new Device
        {
            Id = Guid.NewGuid(),
            Name = name.Length == 0 ? "자녀 PC" : name[..Math.Min(name.Length, 64)],
            TokenHash = HashToken(token),
            CreatedAtUtc = now,
        };
        db.Devices.Add(device);
        await db.SaveChangesAsync();
        return new PairResponse(device.Id, token);
    }

    public Task<Device?> FindByTokenAsync(string token) =>
        db.Devices.FirstOrDefaultAsync(d => d.TokenHash == HashToken(token));

    /// <summary>정책을 수정하고 버전을 올린 뒤, 접속 중인 에이전트에 즉시 전송.</summary>
    public async Task<bool> UpdatePolicyAsync(Guid id, Action<Device> change)
    {
        var device = await db.Devices.FindAsync(id);
        if (device is null) return false;

        change(device);
        device.PolicyVersion++;
        await db.SaveChangesAsync();

        await hub.Clients.Group(id.ToString()).SendAsync(AgentHub.ApplyPolicy, ToPolicy(device));
        return true;
    }

    public static DevicePolicy ToPolicy(Device d) =>
        new(d.PolicyVersion, d.Locked, d.LockMessage, SplitLines(d.BlockedApps), SplitLines(d.BlockedDomains));

    public static ConnectionSnapshot? ReadSnapshot(Device d) =>
        d.LastSnapshotJson is null ? null : JsonSerializer.Deserialize<ConnectionSnapshot>(d.LastSnapshotJson);

    public static string[] SplitLines(string s) =>
        s.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
