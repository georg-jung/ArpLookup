using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ArpLookup
{
    internal static class PlatformHelpers
    {
        public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static bool IsOsx() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }
}
