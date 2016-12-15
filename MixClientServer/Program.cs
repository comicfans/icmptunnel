using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ICMPTunnel;

namespace MixClientServer
{
    class Program
    {
        static void Main(string[] args)
        {
            IPAddress remoteAddr = IPAddress.Parse(args[0]);

            byte thisClientId = 1;
            byte thisServerId = 2;
            if (args[1] == "1")
            {
                var temp = thisClientId;
                thisClientId = thisServerId;
                thisServerId = temp;
            }
            var serv = new TunnelServer(thisServerId,new IPEndPoint(IPAddress.Loopback, 8899));
            var client = new TunnelClient(thisClientId,remoteAddr,new IPEndPoint(IPAddress.Loopback, 8888));
            GlobalEventLoop.Instance().EventLoop();
        }
    }
}
