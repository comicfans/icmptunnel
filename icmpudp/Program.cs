using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICMPTunnel
{
    class Program
    {
        static void Main(string[] args)
        {
            TunnelServer serv = new TunnelServer(new IPEndPoint(IPAddress.Loopback, 8899));
            TunnelClient client = new TunnelClient(Utils.DetectHost(),new IPEndPoint(IPAddress.Loopback, 8888));
            GlobalEventLoop.Instance().EventLoop();
        }
    }
}
