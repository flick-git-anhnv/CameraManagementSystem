using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Kztek.Tool.NetWorkHelpers
{
    public class NetWorkTools
    {
        public static async Task<bool> IsPingSuccessAsync(string ipAddress, int timeOut)
        {
            Ping pingSender = new Ping();
            PingReply reply = await pingSender.SendPingAsync(ipAddress, timeOut);
            if (reply != null && reply.Status == IPStatus.Success)
                return true;
            else
            {
                reply = await pingSender.SendPingAsync(ipAddress, timeOut);
                if (reply != null && reply.Status == IPStatus.Success)
                    return true;
            }
            return false;
        }

        public static bool IsPingSuccess(string ipAddress, int timeOut)
        {
            Ping pingSender = new Ping();
            PingReply reply = pingSender.Send(ipAddress, timeOut);
            if (reply != null && reply.Status == IPStatus.Success)
                return true;

            reply = pingSender.Send(ipAddress, timeOut);
            if (reply != null && reply.Status == IPStatus.Success)
                return true;
            return false;
        }

        public static List<string> GetLocalIPAddress()
        {
            List<string> ips = new List<string>();

            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ips.Add(ip.ToString());
                }
            }
            return ips;
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public static IReadOnlyList<string> GetLocalIPAddress2()
        {
            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Environment.MachineName,
            Dns.GetHostName(),
        };

            try
            {
                foreach (var hostAddress in Dns.GetHostAddresses(Dns.GetHostName()))
                {
                    if (hostAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        values.Add(hostAddress.ToString());
                    }
                }
            }
            catch
            {
                // Ignore DNS lookup failure and keep other identifiers.
            }

            try
            {
                foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (networkInterface.OperationalStatus != OperationalStatus.Up)
                    {
                        continue;
                    }

                    foreach (var unicastAddress in networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (unicastAddress.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            values.Add(unicastAddress.Address.ToString());
                        }
                    }
                }
            }
            catch
            {
                // Ignore network enumeration failure and keep collected identifiers.
            }

            return values.ToList();
        }
    }
}

