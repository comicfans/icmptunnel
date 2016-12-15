using System;
using System.Net;
using System.Net.Sockets;

namespace ICMPTunnel
{
public class TunnelClient : RawEventListener
    {
        private readonly Logger Log = new Logger();

        private byte _id;

        Connection _connection;
        IPEndPoint _listenEndPoint;
        public TunnelClient(byte id ,IPAddress remoteAddress,IPEndPoint listenEndPoint)
        {
            Log.Name = "Client";
            _id=id;
            _connection= new Connection(remoteAddress, Utils.DetectHost(), null,listenEndPoint);
            _connection.Id = _id;
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
            if(id!=_id){
                //Log.T("packet id {0} is not mine",id);
                return;
            }

            _connection.SendToUdp(data,offset,size);
        }
    }
}
