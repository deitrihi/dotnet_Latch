// 에이전트가 접속하는 SignalR 허브: 정책 조회·연결 목록 보고
using System.Text.Json;
using Latch.Server.Data;
using Latch.Server.Services;
using Latch.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Latch.Server.Hubs;

[Authorize(AuthenticationSchemes = DeviceAuthHandler.SchemeName)]
public class DeviceHub(LatchDb db, ConnectionTracker tracker, TimeProvider clock) : Hub
{
    private Guid DeviceId => Guid.Parse(Context.UserIdentifier!);

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, DeviceId.ToString());
        tracker.Connected(DeviceId);
        await TouchAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        tracker.Disconnected(DeviceId);
        return Task.CompletedTask;
    }

    // 메서드 이름은 AgentHub.GetPolicy / AgentHub.ReportConnections 와 일치해야 한다.
    public async Task<DevicePolicy> GetPolicy()
    {
        var device = await db.Devices.FindAsync(DeviceId);
        return device is null ? DevicePolicy.Empty : DeviceService.ToPolicy(device);
    }

    public async Task ReportConnections(ConnectionSnapshot snapshot)
    {
        var device = await TouchAsync(save: false);
        if (device is null) return;
        device.LastSnapshotJson = JsonSerializer.Serialize(snapshot);
        await db.SaveChangesAsync();
    }

    private async Task<Device?> TouchAsync(bool save = true)
    {
        var device = await db.Devices.FindAsync(DeviceId);
        if (device is null) return null;
        device.LastSeenAtUtc = clock.GetUtcNow().UtcDateTime;
        if (save) await db.SaveChangesAsync();
        return device;
    }
}
