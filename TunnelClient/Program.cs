using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICMPTunnel;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {

            TunnelClient client = new TunnelClient(IPAddress.Parse(args[0]),new IPEndPoint(IPAddress.Loopback, 8888));
            GlobalEventLoop.Instance().EventLoop();
        }
    }
}
