using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
namespace ICMPTunnel
{
    class Utils
    {

        public static IPAddress DetectHost()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType != NetworkInterfaceType.Wireless80211 && ni.NetworkInterfaceType != NetworkInterfaceType.Ethernet)
                {
                    continue;
                }
                if (!ni.Name.Contains("以太网") &&
                        !ni.Name.Contains("本地连接") &&
                        !ni.Name.ToLower().Contains("local area connection") &&
                        !ni.Name.ToLower().Contains("wireless network connection"))
                {
                    continue;
                }

                if (ni.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        continue;
                    }

                    return ip.Address;
                }
            }
            return null;
        }
    }
}
