using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ArpLookup
{
    /// <summary>
    /// Provides helper methods for corss platform support.
    /// </summary>
    internal static class PlatformHelpers
    {
        /// <summary>
        /// Gets a value indicating whether the current platform is Linux-based.
        /// </summary>
        /// <returns>True if Linux-based, false otherwise.</returns>
        public static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        /// <summary>
        /// Gets a value indicating whether the current platform is macOS-based.
        /// </summary>
        /// <returns>True if macOS-based, false otherwise.</returns>
        public static bool IsOsx() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        /// <summary>
        /// Gets a value indicating whether the current platform is Windows-based.
        /// </summary>
        /// <returns>True if Windows-based, false otherwise.</returns>
        public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }
}
