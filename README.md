# ArpLookup

[![Build Status](https://dev.azure.com/georg-jung/ArpLookup/_apis/build/status/georg-jung.ArpLookup?branchName=master)](https://dev.azure.com/georg-jung/ArpLookup/_build/latest?definitionId=1&branchName=master)
[![NuGet version (ArpLookup)](https://img.shields.io/nuget/v/ArpLookup.svg?style=flat)](https://www.nuget.org/packages/ArpLookup/)

This is a simple .Net Standard 2.0 library supporting ARP lookups on Windows and Linux to find the MAC address corresponding to an IP address.

## Download

* [NuGet Package](https://www.nuget.org/packages/ArpLookup/)
  * `PM> Install-Package ArpLookup`
* [GitHub Releases](https://github.com/georg-jung/ArpLookup/releases/latest) for the zipped DLL or a nupkg file.

## Usage

This library currently provides one single, simple to use function:

    using ArpLookup;

    // ...

    var mac = await Arp.LookupAsync(IPAddress.Parse("1.2.3.4"));

## Further information

On Windows an API call to IpHlpApi.SendARP is used. Beware that this implementation is not truly async but just returns a finished task containing the result. Consider calling wrapped in `Task.Run` if the sync executing is not acceptable in your use case.

On Linux the `/proc/net/arp` file, which contains system's the arp cache, is read. If the IP address is found there the corresponding MAC address is returned directly.
Otherwise, an ICMP ping is sent to the given IP address and the arp cache lookup is repeated afterwards. This implementation uses async file IO and the framework's async ping implementation.

## Credits

The windows version is based on [nikeee/wake-on-lan](https://github.com/nikeee/wake-on-lan).