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
    internal static class LinuxLookupService
    {
        private const string ArpTablePath = "/proc/net/arp";
        private static readonly Regex lineRegex = new Regex(@"^((?:[0-9]{1,3}\.){3}[0-9]{1,3})(?:\s+\w+){2}\s+((?:[0-9A-Fa-f]{2}[:-]){5}(?:[0-9A-Fa-f]{2}))");

        public static bool IsSupported => PlatformHelpers.IsLinux() && File.Exists(ArpTablePath);

        #region "Asynchronous implementations"
        public static async Task<PhysicalAddress?> PingThenTryReadFromArpTableAsync(IPAddress ip, TimeSpan timeout)
        {
            if (!IsSupported) throw new PlatformNotSupportedException();
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(ip, (int)timeout.TotalMilliseconds).ConfigureAwait(false);
            return await TryReadFromArpTableAsync(ip).ConfigureAwait(false);
        }

        public static async Task<PhysicalAddress?> TryReadFromArpTableAsync(IPAddress ip)
        {
            if (!IsSupported) throw new PlatformNotSupportedException();
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
                    return null;
                try
                {
                    var mac = ParseIfMatch(line, ip);
                    if (mac != null)
                        return mac;
                }
                catch (FormatException)
                {
                    throw new PlatformNotSupportedException();
                }
            }

            return null;
        }
        #endregion

        #region "Synchronous implementations"
        public static PhysicalAddress? PingThenTryReadFromArpTable(IPAddress ip, TimeSpan timeout)
        {
            if (!IsSupported) throw new PlatformNotSupportedException();
            using var ping = new Ping();
            var reply = ping.Send(ip, (int)timeout.TotalMilliseconds);
            return TryReadFromArpTable(ip);
        }

        public static PhysicalAddress? TryReadFromArpTable(IPAddress ip)
        {
            if (!IsSupported) throw new PlatformNotSupportedException();
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
                    return null;
                try
                {
                    var mac = ParseIfMatch(line, ip);
                    if (mac != null)
                        return mac;
                }
                catch (FormatException)
                {
                    throw new PlatformNotSupportedException();
                }
            }

            return null;
        }
    #endregion 

        private static PhysicalAddress? ParseIfMatch(string line, IPAddress ip)
        {
            var m = lineRegex.Match(line);
            if (!m.Success || m.Groups.Count != 3)
                throw new FormatException($"The given line '{line}' was not in the expected /proc/net/arp format.");
            var tableIpStr = m.Groups[1].Value;
            var tableMacStr = m.Groups[2].Value;
            var tableIp = IPAddress.Parse(tableIpStr);
            if (!tableIp.Equals(ip))
                return null;
            return tableMacStr.ParseMacAddress();
        }
    }
}
