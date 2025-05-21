using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

class SubnetPingScanner
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Subnet Ping Scanner");
        Console.WriteLine("Usage: subnetpingscanner <subnet> (e.g., 192.168.1.0/24)");
        Console.WriteLine();

        if (args.Length == 0)
        {
            Console.WriteLine("Please specify a subnet in CIDR notation (e.g., 192.168.1.0/24)");
            return;
        }

        string subnet = args[0];
        if (!TryParseSubnet(subnet, out IPAddress baseAddress, out int prefixLength))
        {
            Console.WriteLine("Invalid subnet format. Please use CIDR notation (e.g., 192.168.1.0/24)");
            return;
        }

        Console.WriteLine($"Scanning subnet: {subnet}");
        Console.WriteLine("Active IP addresses:");

        byte[] addressBytes = baseAddress.GetAddressBytes();
        int hostBits = 32 - prefixLength;
        uint numberOfHosts = (uint)Math.Pow(2, hostBits) - 1;

        // Limit the number of concurrent pings to avoid overwhelming the network
        var options = new ParallelOptions { MaxDegreeOfParallelism = 50 };

        await Parallel.ForEachAsync(Enumerable.Range(1, (int)numberOfHosts), options, async (host, cancellationToken) =>
        {
            byte[] currentAddressBytes = (byte[])addressBytes.Clone();

            // Calculate the current IP address
            uint currentAddress = BitConverter.ToUInt32(currentAddressBytes.Reverse().ToArray());
            currentAddress += (uint)host;
            byte[] newBytes = BitConverter.GetBytes(currentAddress).Reverse().ToArray();

            IPAddress targetAddress = new IPAddress(newBytes);

            using var ping = new Ping();
            try
            {
                PingReply reply = await ping.SendPingAsync(targetAddress, 1000); // 1000ms (1s) timeout
                if (reply.Status == IPStatus.Success)
                {
                    Console.WriteLine($"{targetAddress} is active (RoundTrip time: {reply.RoundtripTime}ms)");
                }
            }
            catch (PingException)
            {
                // Ignore ping exceptions (host not reachable, etc.)
            }
        });
    }

    private static bool TryParseSubnet(string subnet, out IPAddress baseAddress, out int prefixLength)
    {
        baseAddress = null;
        prefixLength = 0;

        string[] parts = subnet.Split('/');
        if (parts.Length != 2) return false;

        if (!IPAddress.TryParse(parts[0], out baseAddress)) return false;
        if (!int.TryParse(parts[1], out prefixLength)) return false;

        if (prefixLength < 0 || prefixLength > 32) return false;

        return true;
    }
}
