using System;
using System.Net;
using System.Net.Sockets;

namespace ICMPTunnel
{
public class TunnelClient : RawEventListener
    {
        private readonly Logger Log = new Logger();

        public const int ID=0;

        Connection _connection;
        IPEndPoint _listenEndPoint;
        public TunnelClient(IPAddress remoteAddress,IPEndPoint listenEndPoint)
        {
            Log.Name = "Client";
            _connection= new Connection(remoteAddress, Utils.DetectHost(), null,listenEndPoint);
            _connection.Id = ID;
            _listenEndPoint = listenEndPoint;
            _connection.Log.Name = "ClientListen";
            _connection.Start();

            TransportLayer.Instance().Listeners.Add(this);
        }

        public void OnRawError()
        {
            Log.E("raw error");
            throw new NotImplementedException();
        }

        public void OnRawRead(IPAddress remoteAddr,byte id, byte[] data, int offset, int size)
        {
            if(id!=ID){
                //Log.T("packet id {0} is not mine",id);
                return;
            }

            _connection.SendToUdp(data,offset,size);
        }
    }
}
