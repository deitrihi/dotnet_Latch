// GetExtendedTcpTable로 프로세스별 외부 TCP 연결 목록을 수집
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using Latch.Shared;

namespace Latch.Agent.Monitoring;

public static class TcpConnections
{
    private const int MaxEntries = 500;
    private const int AfInet = 2, AfInet6 = 23;
    private const int TcpTableOwnerPidAll = 5;
    private const int Ipv4RowSize = 24, Ipv6RowSize = 56;

    private static readonly string[] StateNames =
        ["", "Closed", "Listen", "SynSent", "SynReceived", "Established", "FinWait1", "FinWait2",
         "CloseWait", "Closing", "LastAck", "TimeWait", "DeleteTcb"];

    public static ConnectionSnapshot Capture()
    {
        var rows = ReadTable(AfInet).Concat(ReadTable(AfInet6))
            .Where(r => r.State is 3 or 5 && !IPAddress.IsLoopback(r.Remote) && !r.Remote.Equals(IPAddress.Any)
                        && !r.Remote.Equals(IPAddress.IPv6Any))
            .Take(MaxEntries)
            .ToList();

        var names = new Dictionary<int, string>();
        var list = rows.Select(r => new ConnectionInfo(
            r.Pid,
            ProcessName(r.Pid, names),
            $"{r.Local}:{r.LocalPort}",
            r.Remote.ToString(),
            r.RemotePort,
            StateNames[r.State])).ToArray();

        return new ConnectionSnapshot(DateTimeOffset.UtcNow, list);
    }

    private static string ProcessName(int pid, Dictionary<int, string> cache)
    {
        if (cache.TryGetValue(pid, out var n)) return n;
        try { using var p = Process.GetProcessById(pid); n = p.ProcessName; }
        catch (Exception) { n = "?"; }
        return cache[pid] = n;
    }

    private readonly record struct Row(IPAddress Local, int LocalPort, IPAddress Remote, int RemotePort, int State, int Pid);

    private static List<Row> ReadTable(int family)
    {
        var size = 0;
        GetExtendedTcpTable(0, ref size, false, family, TcpTableOwnerPidAll, 0);
        var buf = Marshal.AllocHGlobal(size);
        try
        {
            if (GetExtendedTcpTable(buf, ref size, false, family, TcpTableOwnerPidAll, 0) != 0) return [];

            var count = Marshal.ReadInt32(buf);
            var rows = new List<Row>(count);
            var rowSize = family == AfInet ? Ipv4RowSize : Ipv6RowSize;
            var bytes = new byte[rowSize];
            for (var i = 0; i < count; i++)
            {
                Marshal.Copy(buf + 4 + i * rowSize, bytes, 0, rowSize);
                var s = bytes.AsSpan();
                rows.Add(family == AfInet
                    // state, localAddr, localPort, remoteAddr, remotePort, pid
                    ? new Row(new IPAddress(s[4..8]), Port(s[8..]), new IPAddress(s[12..16]), Port(s[16..]),
                        BitConverter.ToInt32(s[0..]), BitConverter.ToInt32(s[20..]))
                    // localAddr[16], localScope, localPort, remoteAddr[16], remoteScope, remotePort, state, pid
                    : new Row(new IPAddress(s[0..16]), Port(s[20..]), new IPAddress(s[24..40]), Port(s[44..]),
                        BitConverter.ToInt32(s[48..]), BitConverter.ToInt32(s[52..])));
            }
            return rows;
        }
        finally { Marshal.FreeHGlobal(buf); }
    }

    /// <summary>포트는 DWORD 하위 2바이트에 네트워크 바이트 순서로 들어 있다.</summary>
    private static int Port(ReadOnlySpan<byte> s) => (s[0] << 8) | s[1];

    [DllImport("iphlpapi.dll")]
    private static extern uint GetExtendedTcpTable(nint table, ref int size, bool order, int family, int tableClass, uint reserved);
}
