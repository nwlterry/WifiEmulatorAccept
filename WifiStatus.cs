using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace WifiEmulatorAccept;

public readonly record struct WifiState(bool Connected, string Ssid, string Text)
{
    public static WifiState Disconnected { get; } = new(false, "", "Wi-Fi: disconnected");
}

public static class WifiStatus
{
    public static WifiState Read()
    {
        try
        {
            var fromWlan = ReadFromWlan();
            if (fromWlan != null)
                return fromWlan.Value;
        }
        catch
        {
            // Fall back to the managed adapter list.
        }

        var up = NetworkInterface.GetAllNetworkInterfaces().Any(nic =>
            nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
            && nic.OperationalStatus == OperationalStatus.Up);
        return up ? new WifiState(true, "", "Wi-Fi: connected") : WifiState.Disconnected;
    }

    static WifiState? ReadFromWlan()
    {
        var opened = WlanOpenHandle(2, IntPtr.Zero, out _, out var client);
        if (opened != 0)
            return null;

        IntPtr list = IntPtr.Zero;
        try
        {
            var enumerated = WlanEnumInterfaces(client, IntPtr.Zero, out list);
            if (enumerated != 0 || list == IntPtr.Zero)
                return null;

            var count = Marshal.ReadInt32(list);
            var itemSize = Marshal.SizeOf<WlanInterfaceInfo>();
            var ssids = new List<string>();
            var connected = false;
            for (var i = 0; i < count; i++)
            {
                var info = Marshal.PtrToStructure<WlanInterfaceInfo>(list + 8 + (i * itemSize));
                if (info.State != WlanInterfaceStateConnected)
                    continue;
                connected = true;
                var name = QuerySsid(client, info.InterfaceGuid);
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                var trimmed = name.Trim();
                if (!ssids.Contains(trimmed, StringComparer.Ordinal))
                    ssids.Add(trimmed);
            }

            if (!connected)
                return WifiState.Disconnected;
            if (ssids.Count == 0)
                return new WifiState(true, "", "Wi-Fi: connected");
            var joined = string.Join(", ", ssids);
            return new WifiState(true, joined, "Wi-Fi: connected to " + joined);
        }
        finally
        {
            if (list != IntPtr.Zero)
                WlanFreeMemory(list);
            _ = WlanCloseHandle(client, IntPtr.Zero);
        }
    }

    static string? QuerySsid(IntPtr client, Guid interfaceGuid)
    {
        var queried = WlanQueryInterface(
            client,
            ref interfaceGuid,
            WlanIntfOpcodeCurrentConnection,
            IntPtr.Zero,
            out var dataSize,
            out var data,
            IntPtr.Zero);
        if (queried != 0 || data == IntPtr.Zero || dataSize < SsidLengthOffset + 4)
            return null;

        try
        {
            var length = Marshal.ReadInt32(data, SsidLengthOffset);
            if (length <= 0 || length > 32 || dataSize < SsidLengthOffset + 4 + length)
                return null;
            var bytes = new byte[length];
            Marshal.Copy(data + SsidLengthOffset + 4, bytes, 0, length);
            return Encoding.UTF8.GetString(bytes);
        }
        finally
        {
            WlanFreeMemory(data);
        }
    }

    // WLAN_CONNECTION_ATTRIBUTES: state (4) + mode (4) + profile name (256 chars).
    const int SsidLengthOffset = 4 + 4 + (256 * 2);
    const int WlanInterfaceStateConnected = 1;
    const int WlanIntfOpcodeCurrentConnection = 7;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct WlanInterfaceInfo
    {
        public Guid InterfaceGuid;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Description;

        public int State;
    }

    [DllImport("wlanapi.dll")]
    static extern uint WlanOpenHandle(uint clientVersion, IntPtr reserved, out uint negotiatedVersion, out IntPtr clientHandle);

    [DllImport("wlanapi.dll")]
    static extern uint WlanCloseHandle(IntPtr clientHandle, IntPtr reserved);

    [DllImport("wlanapi.dll")]
    static extern uint WlanEnumInterfaces(IntPtr clientHandle, IntPtr reserved, out IntPtr interfaceList);

    [DllImport("wlanapi.dll")]
    static extern uint WlanQueryInterface(
        IntPtr clientHandle,
        ref Guid interfaceGuid,
        int opcode,
        IntPtr reserved,
        out uint dataSize,
        out IntPtr data,
        IntPtr opcodeType);

    [DllImport("wlanapi.dll")]
    static extern void WlanFreeMemory(IntPtr memory);
}
