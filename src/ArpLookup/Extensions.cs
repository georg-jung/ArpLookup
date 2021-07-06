using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Text;

namespace ArpLookup
{
    /// <summary>
    /// Provides simple static helper functions for internal usage as Extensions.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Parses string representations of physical addresses. Supports : as well as - as separator.
        /// </summary>
        /// <param name="mac">String representation of a physical address.</param>
        /// <returns>A <see cref="PhysicalAddress"/> instance that represents the given string. Throws if parsing fails.</returns>
        public static PhysicalAddress ParseMacAddress(this string mac)
        {
            var macString = mac?.Replace(":", "-")?.ToUpper(CultureInfo.InvariantCulture) ?? throw new ArgumentNullException(nameof(mac));
            return PhysicalAddress.Parse(macString);
        }
    }
}
