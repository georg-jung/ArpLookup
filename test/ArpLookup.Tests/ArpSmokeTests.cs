// Copyright (c) Georg Jung. Licensed under the MIT license. See LICENSE.txt in the project root for license information.

using System.Net;
using System.Net.NetworkInformation;

namespace ArpLookup.Tests;

/// <summary>
/// Smoke tests that exercise the public API on a Linux host.
/// These tests verify the code doesn't throw unexpected exceptions;
/// actual ARP resolution results depend on the network environment.
/// </summary>
public class ArpSmokeTests
{
    [Test]
    public async Task IsSupported_ReturnsTrue_OnLinux()
    {
        await Assert.That(Arp.IsSupported).IsTrue();
    }

    [Test]
    public async Task LookupAsync_Loopback_DoesNotThrow()
    {
        // Loopback won't have an ARP entry, but the call should not throw.
        var result = await Arp.LookupAsync(IPAddress.Loopback);

        // Loopback is not in the ARP table, so null is expected.
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Lookup_Loopback_DoesNotThrow()
    {
        var result = Arp.Lookup(IPAddress.Loopback);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task LookupAsync_DefaultGateway_ReturnsResult()
    {
        var gateway = GetDefaultGateway();
        if (gateway is null)
        {
            Skip.Test("No gateway available in this environment");
        }

        // Should succeed or return null – must not throw.
        var result = await Arp.LookupAsync(gateway);

        // On a typical machine the gateway is reachable and in the ARP cache.
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task Lookup_DefaultGateway_ReturnsResult()
    {
        var gateway = GetDefaultGateway();
        if (gateway is null)
        {
            Skip.Test("No gateway available in this environment");
        }

        var result = Arp.Lookup(gateway);

        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task LookupAsync_NonRoutableAddress_ReturnsNull()
    {
        // 192.0.2.1 is TEST-NET-1 (RFC 5737), should not be reachable.
        var result = await Arp.LookupAsync(IPAddress.Parse("192.0.2.1"));

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Lookup_NonRoutableAddress_ReturnsNull()
    {
        var result = Arp.Lookup(IPAddress.Parse("192.0.2.1"));

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task LinuxPingTimeout_CanBeSetAndRead()
    {
        var original = Arp.LinuxPingTimeout;
        try
        {
            var newTimeout = TimeSpan.FromSeconds(2);
            Arp.LinuxPingTimeout = newTimeout;
            await Assert.That(Arp.LinuxPingTimeout).IsEqualTo(newTimeout);
        }
        finally
        {
            Arp.LinuxPingTimeout = original;
        }
    }

    private static IPAddress? GetDefaultGateway()
    {
        return NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
            .SelectMany(ni => ni.GetIPProperties().GatewayAddresses)
            .Select(g => g.Address)
            .FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                                 && !IPAddress.IsLoopback(a)
                                 && !a.Equals(IPAddress.Any));
    }
}
