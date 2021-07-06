using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace ArpLookup
{
    /// <summary>
    /// Provides the necessary implementations for ARP lookups on Windows platforms.
    /// </summary>
    internal static class WindowsLookupService
    {
        /// <summary>
        /// Gets a value indicating whether this class can be used on the current platform.
        /// </summary>
        public static bool IsSupported => PlatformHelpers.IsWindows();

        /// <summary>
        /// Call IpHlpApi.SendARP to lookup the mac address on windows-based systems.
        /// </summary>
        /// <param name="ip">The <see cref="IPAddress"/> to pass to the Win32 API.</param>
        /// <exception cref="Win32Exception">If IpHlpApi.SendARP returns non-zero.</exception>
        /// <returns>A <see cref="PhysicalAddress"/> instance that represents the address found by IpHlpApi.SendARP.</returns>
        public static PhysicalAddress Lookup(IPAddress ip)
        {
            _ = ip ?? throw new ArgumentNullException(nameof(ip));
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException();
            }

            // based on https://github.com/nikeee/wake-on-lan/blob/5bdcecc/src/WakeOnLan/ArpRequest.cs
            var destIp = BitConverter.ToInt32(ip.GetAddressBytes(), 0);

            var addr = new byte[6];
            var len = addr.Length;

            var res = NativeMethods.SendARP(destIp, 0, addr, ref len);

            if (res == 0)
            {
                return new PhysicalAddress(addr);
            }

            throw new Win32Exception(res);
        }

        // based on https://github.com/nikeee/wake-on-lan/blob/4dfa0fd/src/WakeOnLan/NativeMethods.cs
        private static class NativeMethods
        {
            private const string IphlpApi = "iphlpapi.dll";

            [DllImport(IphlpApi, ExactSpelling = true)]
            [SecurityCritical]
            internal static extern int SendARP(int destinationIp, int sourceIp, byte[] macAddress, ref int physicalAddrLength);
        }
    }
}
