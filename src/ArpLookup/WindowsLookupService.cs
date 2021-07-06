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
    internal static class WindowsLookupService
    {
        public static bool IsSupported => PlatformHelpers.IsWindows();

        // based on https://github.com/nikeee/wake-on-lan/blob/5bdcecc/src/WakeOnLan/ArpRequest.cs
        /// <summary>
        /// Call ApHlpApi.SendARP to lookup the mac address on windows-based systems.
        /// </summary>
        /// <exception cref="Win32Exception">If IpHlpApi.SendARP returns non-zero.</exception>
        public static PhysicalAddress Lookup(IPAddress ip)
        {
            if (!IsSupported)
                throw new PlatformNotSupportedException();
            if (ip == null)
                throw new ArgumentNullException(nameof(ip));

            int destIp = BitConverter.ToInt32(ip.GetAddressBytes(), 0);

            var addr = new byte[6];
            var len = addr.Length;

            var res = NativeMethods.SendARP(destIp, 0, addr, ref len);

            if (res == 0)
                return new PhysicalAddress(addr);
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
