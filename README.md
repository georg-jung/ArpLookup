<p align="center">
  <a href="https://www.nuget.org/packages/ArpLookup/">
    <img
      alt="ArpLookup"
      src="https://github.com/georg-jung/ArpLookup/blob/master/doc/logo.svg"
      width="100"
    />
  </a>
</p>

# ArpLookup

[![Build Status](https://dev.azure.com/georg-jung/ArpLookup/_apis/build/status/georg-jung.ArpLookup?branchName=master)](https://dev.azure.com/georg-jung/ArpLookup/_build/latest?definitionId=1&branchName=master)
[![NuGet version (ArpLookup)](https://img.shields.io/nuget/v/ArpLookup.svg?style=flat)](https://www.nuget.org/packages/ArpLookup/)

This is a simple .Net Standard 2.0 library supporting ARP lookups on Windows and Linux to find the MAC address corresponding to an IP address.

## Download

* [NuGet Package](https://www.nuget.org/packages/ArpLookup/)
  * `PM> Install-Package ArpLookup`
* [GitHub Releases](https://github.com/georg-jung/ArpLookup/releases/latest) for the zipped DLL or a nupkg file.

## Usage

This library currently provides one single, simple to use function (once sync, once async; only truly async on Linux, see below):

```C#
using System.Net.NetworkInformation;
using ArpLookup;

// ...

PhysicalAddress mac = Arp.Lookup(IPAddress.Parse("1.2.3.4"));

PhysicalAddress mac = await Arp.LookupAsync(IPAddress.Parse("1.2.3.4"));
```

To detect if the current platform is supported, check as follows. Lookups on unsupported platforms throw `PlatformNotSupportedException`s.

```C#
var linuxOrWindows = Arp.IsSupported;
```

## Further information

On Windows an API call to IpHlpApi.SendARP is used. Beware that this implementation is not truly async but just returns a finished task containing the result. Consider calling wrapped in `Task.Run` if the sync executing is not acceptable in your use case.

On Linux the `/proc/net/arp` file, which contains system's the arp cache, is read. If the IP address is found there the corresponding MAC address is returned directly.
Otherwise, an ICMP ping is sent to the given IP address and the arp cache lookup is repeated afterwards. This implementation uses async file IO and the framework's async ping implementation.

Per default, the library waits for ping responses for up to 750ms on Linux platforms. Technically, the responses are not required, as the arp protocol and the arp cache have nothing to do with the pings. Rather, the pings are an easy way to force the OS to figure out and provide the information we are looking for. I did not do extensive tests how long is reasonable to wait to be quite sure, the arp cache is updated. 750ms should be much more than needed in many cases - it is more of the safe option. Note that if you do recieve a ping response, the wait might be much shorter. The timeout gets relevant if the host is not available/no ping answer is received. If you want to request many addresses or are facing other time-limiting aspects, you may want to reconfigure this default:

```C#
Arp.LinuxPingTimeout = TimeSpan.FromMilliseconds(125);
```

## Supported platforms

* Windows (tested)
* Linux
  * Debian (tested)
  * Android
  * **not** WSL 1 (tested)
  * [WSL 2](https://github.com/Microsoft/WSL/issues/2279)

Note that the used method does not work in WSL 1 and might not work on every Linux distribution. Checking the `Arp.IsSupported` property accounts for this (though it does not check if you are actually allowed to access `/proc/net/arp`). In WSL 2 this library will work as on most "real" Linux distributions as [this issue describes](https://github.com/Microsoft/WSL/issues/2279). While I did not test this library on Android/Xamarin I have read in different places that reading `/proc/net/arp` is possible (given the right permissions).
