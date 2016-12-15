using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICMPTunnel;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            byte clientId=0;
            var serv = new TunnelServer(clientId,new IPEndPoint(IPAddress.Loopback, 8899));
            var client = new TunnelClient(clientId,Utils.DetectHost(),new IPEndPoint(IPAddress.Loopback, 8888));
            GlobalEventLoop.Instance().EventLoop();
        }
    }
}
