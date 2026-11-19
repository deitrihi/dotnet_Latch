// 현재 SignalR로 접속 중인 디바이스를 추적 (온라인 표시용)
using System.Collections.Concurrent;

namespace Latch.Server.Services;

public class ConnectionTracker
{
    private readonly ConcurrentDictionary<Guid, int> _counts = new();

    public void Connected(Guid id) => _counts.AddOrUpdate(id, 1, (_, n) => n + 1);

    public void Disconnected(Guid id)
    {
        if (_counts.AddOrUpdate(id, 0, (_, n) => n - 1) <= 0) _counts.TryRemove(id, out _);
    }

    public bool IsOnline(Guid id) => _counts.ContainsKey(id);
}
