using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace ArpLookup
{
    /// <summary>
    /// Provides ARP lookup functionality (IP address to MAC/Hardware address translation) on Windows and Linux platforms.
    /// </summary>
    public static class Arp
    {
        /// <summary>
        /// Gets a value indicating whether the ARP lookup functionality is supported on the current plattform.
        /// </summary>
        public static bool IsSupported => WindowsLookupService.IsSupported || LinuxLookupService.IsSupported;

        /// <summary>
        /// Gets or sets the timeout for pings that are performed on Linux platforms, if an IP address can not be found in the ARP table right away. Has no effect on other platforms.
        /// </summary>
        public static TimeSpan LinuxPingTimeout { get; set; } = TimeSpan.FromMilliseconds(750);

        /// <summary>
        /// This tries to lookup the MAC address that corresponds to an IP address using a way supported on the current platform. Windows and Linux are supported.
        /// On Windows an API call to IpHlpApi.SendARP is used. Beware that this implementation is not truly async but just returns a finished task containing the result.
        /// On Linux the /proc/net/arp file, which contains systems the arp cache is read. If the IP address is found there the corresponding MAC address is returned directly.
        /// Otherwise, an ICMP ping is sent to the given IP address and the arp cache lookup is repeated afterwards. This implementation uses async file IO and the framework's async ping implementation.
        /// </summary>
        /// <param name="ip">The IP address to look the mac address up for.</param>
        /// <returns>The mac address if found, null otherwise.</returns>
        public static async Task<PhysicalAddress> LookupAsync(IPAddress ip)
        {
            if (WindowsLookupService.IsSupported)
            {
                return WindowsLookupService.Lookup(ip);
            }

            if (LinuxLookupService.IsSupported)
            {
                var mac = await LinuxLookupService.TryReadFromArpTableAsync(ip).ConfigureAwait(false);
                if (mac != null)
                {
                    return mac;
                }

                return await LinuxLookupService.PingThenTryReadFromArpTableAsync(ip, LinuxPingTimeout).ConfigureAwait(false);
            }

            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// This tries to lookup the MAC address that corresponds to an IP address using a way supported on the current platform. Windows and Linux are supported.
        /// On Windows an API call to IpHlpApi.SendARP is used.
        /// On Linux the /proc/net/arp file, which contains systems the arp cache is read. If the IP address is found there the corresponding MAC address is returned directly.
        /// Otherwise, an ICMP ping is sent to the given IP address and the arp cache lookup is repeated afterwards. This implementation uses synchronous code.
        /// </summary>
        /// <param name="ip">The IP address to look the mac address up for.</param>
        /// <returns>The mac address if found, null otherwise.</returns>
        public static PhysicalAddress Lookup(IPAddress ip)
        {
            if (WindowsLookupService.IsSupported)
            {
                return WindowsLookupService.Lookup(ip);
            }

            if (LinuxLookupService.IsSupported)
            {
                var mac = LinuxLookupService.TryReadFromArpTable(ip);
                if (mac != null)
                {
                    return mac;
                }

                return LinuxLookupService.PingThenTryReadFromArpTable(ip, LinuxPingTimeout);
            }

            throw new PlatformNotSupportedException();
        }
    }
}
