using HotspotControl.Core.Contracts;

namespace HotspotControl.Windows;

public static class DeviceProjection
{
    public static ConnectedDevice FromReported(int number, IEnumerable<string?> hostNames, string? macAddress) =>
        new(number, hostNames.Where(name => !string.IsNullOrWhiteSpace(name)).Select(name => name!).ToArray(),
            string.IsNullOrWhiteSpace(macAddress) ? null : macAddress);
}
