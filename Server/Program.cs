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
            TunnelServer serv = new TunnelServer(new IPEndPoint(IPAddress.Loopback, 8899));
        }
    }
}
