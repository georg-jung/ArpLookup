using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Text;

namespace ArpLookup
{
    internal static class Extensions
    {
        public static PhysicalAddress ParseMacAddress(this string mac)
        {
            var macString = mac?.Replace(":", "-")?.ToUpper(CultureInfo.InvariantCulture) ?? throw new ArgumentNullException(nameof(mac));
            return PhysicalAddress.Parse(macString);
        }
    }
}
