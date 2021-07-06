using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArpLookup
{
    /// <summary>
    /// Provides the necessary implementations for ARP lookups on Linux platforms.
    /// </summary>
    internal static class LinuxLookupService
    {
        private const string ArpTablePath = "/proc/net/arp";
        private static readonly Regex LineRegex = new(@"^((?:[0-9]{1,3}\.){3}[0-9]{1,3})(?:\s+\w+){2}\s+((?:[0-9A-Fa-f]{2}[:-]){5}(?:[0-9A-Fa-f]{2}))");

        /// <summary>
        /// Gets a value indicating whether this class can be used on the current platform.
        /// </summary>
        public static bool IsSupported => PlatformHelpers.IsLinux() && File.Exists(ArpTablePath);

        /// <summary>
        /// Pings the given <see cref="IPAddress"/> and waits for an answer for up to the specified timeout duration.
        /// Afterwards tries to find an entry for the given <see cref="IPAddress"/> in the ARP table/local ARP cache.
        /// </summary>
        /// <param name="ip">The <see cref="IPAddress"/> to ping and look for.</param>
        /// <param name="timeout">The duration to wait for an answer to the ping.</param>
        /// <returns>A <see cref="Task{PhysicalAddress}"/> representing the result of the asynchronous operation:
        /// A <see cref="PhysicalAddress"/> for the given <see cref="IPAddress"/> or null if the <see cref="IPAddress"/>
        /// could not be found in the ARP cache after the ping completed.</returns>
        public static async Task<PhysicalAddress?> PingThenTryReadFromArpTableAsync(IPAddress ip, TimeSpan timeout)
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException();
            }

            using var ping = new Ping();
            var reply = await ping.SendPingAsync(ip, (int)timeout.TotalMilliseconds).ConfigureAwait(false);
            return await TryReadFromArpTableAsync(ip).ConfigureAwait(false);
        }

        /// <summary>
        /// Tries to find an entry for the given <see cref="IPAddress"/> in the ARP table/local ARP cache.
        /// </summary>
        /// <param name="ip">The <see cref="IPAddress"/> to look for.</param>
        /// <returns>A <see cref="Task{PhysicalAddress}"/> representing the result of the asynchronous operation:
        /// A <see cref="PhysicalAddress"/> for the given <see cref="IPAddress"/> or null if the <see cref="IPAddress"/>
        /// could not be found in the ARP cache.</returns>
        public static async Task<PhysicalAddress?> TryReadFromArpTableAsync(IPAddress ip)
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException();
            }

            using var arpFile = new FileStream(ArpTablePath, FileMode.Open, FileAccess.Read);
            return await ParseProcNetArpAsync(arpFile, ip).ConfigureAwait(false);
        }

        private static async Task<PhysicalAddress?> ParseProcNetArpAsync(Stream content, IPAddress ip)
        {
            using var reader = new StreamReader(content);
            await reader.ReadLineAsync().ConfigureAwait(false); // first line is header, skip
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(line))
                {
                    return null;
                }

                try
                {
                    var mac = ParseIfMatch(line, ip);
                    if (mac != null)
                    {
                        return mac;
                    }
                }
                catch (FormatException)
                {
                    throw new PlatformNotSupportedException();
                }
            }

            return null;
        }

        /// <summary>
        /// Pings the given <see cref="IPAddress"/> and waits for an answer for up to the specified timeout duration.
        /// Afterwards tries to find an entry for the given <see cref="IPAddress"/> in the ARP table/local ARP cache.
        /// </summary>
        /// <param name="ip">The <see cref="IPAddress"/> to ping and look for.</param>
        /// <param name="timeout">The duration to wait for an answer to the ping.</param>
        /// <returns>A <see cref="PhysicalAddress"/> for the given <see cref="IPAddress"/> or null if the <see cref="IPAddress"/>
        /// could not be found in the ARP cache after the ping completed.</returns>
        public static PhysicalAddress? PingThenTryReadFromArpTable(IPAddress ip, TimeSpan timeout)
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException();
            }

            using var ping = new Ping();
            var reply = ping.Send(ip, (int)timeout.TotalMilliseconds);
            return TryReadFromArpTable(ip);
        }

        /// <summary>
        /// Tries to find an entry for the given <see cref="IPAddress"/> in the ARP table/local ARP cache.
        /// </summary>
        /// <param name="ip">The <see cref="IPAddress"/> to look for.</param>
        /// <returns>A <see cref="PhysicalAddress"/> for the given <see cref="IPAddress"/> or null if
        /// the <see cref="IPAddress"/> could not be found in the ARP cache.</returns>
        public static PhysicalAddress? TryReadFromArpTable(IPAddress ip)
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException();
            }

            using var arpFile = new FileStream(ArpTablePath, FileMode.Open, FileAccess.Read);
            return ParseProcNetArp(arpFile, ip);
        }

        private static PhysicalAddress? ParseProcNetArp(Stream content, IPAddress ip)
        {
            using var reader = new StreamReader(content);
            reader.ReadLine(); // first line is header, skip
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    return null;
                }

                try
                {
                    var mac = ParseIfMatch(line, ip);
                    if (mac != null)
                    {
                        return mac;
                    }
                }
                catch (FormatException)
                {
                    throw new PlatformNotSupportedException();
                }
            }

            return null;
        }

        private static PhysicalAddress? ParseIfMatch(string line, IPAddress ip)
        {
            var m = LineRegex.Match(line);
            if (!m.Success || m.Groups.Count != 3)
            {
                throw new FormatException($"The given line '{line}' was not in the expected /proc/net/arp format.");
            }

            var tableIpStr = m.Groups[1].Value;
            var tableMacStr = m.Groups[2].Value;
            var tableIp = IPAddress.Parse(tableIpStr);
            if (!tableIp.Equals(ip))
            {
                return null;
            }

            return tableMacStr.ParseMacAddress();
        }
    }
}
