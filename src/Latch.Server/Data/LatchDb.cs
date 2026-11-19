// 부모 계정·디바이스·페어링 코드를 저장하는 SQLite DbContext
using Microsoft.EntityFrameworkCore;

namespace Latch.Server.Data;

public class LatchDb(DbContextOptions<LatchDb> options) : DbContext(options)
{
    public DbSet<AdminAccount> Admins => Set<AdminAccount>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<PairingCode> PairingCodes => Set<PairingCode>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Device>().HasIndex(d => d.TokenHash).IsUnique();
        b.Entity<PairingCode>().HasKey(p => p.Code);
    }
}

/// <summary>부모 계정 (단일). 최초 실행 시 /Setup에서 생성.</summary>
public class AdminAccount
{
    public int Id { get; set; }
    public string PasswordHash { get; set; } = "";
}

public class Device
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    /// <summary>디바이스 토큰의 SHA-256. 원문 토큰은 저장하지 않는다.</summary>
    public string TokenHash { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }

    public long PolicyVersion { get; set; }
    public bool Locked { get; set; }
    public string LockMessage { get; set; } = "";
    /// <summary>줄바꿈으로 구분된 목록.</summary>
    public string BlockedApps { get; set; } = "";
    public string BlockedDomains { get; set; } = "";

    /// <summary>가장 최근 연결 목록 (ConnectionSnapshot JSON).</summary>
    public string? LastSnapshotJson { get; set; }
}

public class PairingCode
{
    public string Code { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
}
