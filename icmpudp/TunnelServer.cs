using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;

namespace ICMPTunnel
{
    public class TunnelServer:RawEventListener{

        ConcurrentDictionary<IPAddress,Connection> _remoteMapConnection=new ConcurrentDictionary<IPAddress,Connection>();

        IPAddress _myAddress = Utils.DetectHost();

        IPEndPoint _targetPoint;
        public TunnelServer(IPEndPoint targetEndPoint)
        {
            Log.Name="Server";
            _targetPoint = targetEndPoint;
            TransportLayer.Instance().Listeners.Add(this);
        }

        public const int ID = byte.MaxValue-TunnelClient.ID;

        public void OnRawRead(IPAddress remoteAddr,byte id,byte [] data,int offset,int size){
            if(id!=ID){
                //Log.T("packet id {0} not mine",id);
                return;
            }

            Connection conn;

            while (!_remoteMapConnection.TryGetValue(remoteAddr, out conn))
            {
                conn = new Connection(remoteAddr, _myAddress, _targetPoint);
                var added = _remoteMapConnection.TryAdd(remoteAddr, conn);
                if (added)
                {
                    conn.Log.Name = "ServerConn:" + remoteAddr;
                    conn.Id=ID;
                    conn.Start();
                    break;
                }else
                {
                    conn.Stop();
                }
            }

            Log.D("raw to socket {0} send {1}",_targetPoint,size);

            conn.SendToUdp(data, offset, size);
        }

        public Logger Log = new Logger();

        public void OnRawError(){
            Log.E("raw error");
        }
    }
}
