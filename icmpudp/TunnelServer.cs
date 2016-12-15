using System;
using System.Net;
using System.Collections.Concurrent;

namespace ICMPTunnel
{
    public class TunnelServer:RawEventListener{

        ConcurrentDictionary<IPAddress,Connection> _remoteMapConnection=new ConcurrentDictionary<IPAddress,Connection>();

        IPAddress _myAddress = Utils.DetectHost();

        IPEndPoint _targetPoint;
        public TunnelServer(byte clientId,IPEndPoint targetEndPoint)
        {
            if(clientId>127){
                throw new ArgumentOutOfRangeException("clientId","should less than 128");
            }
            _id=(byte)(byte.MaxValue-clientId);
            Log.Name="Server";
            _targetPoint = targetEndPoint;
            TransportLayer.Instance().Listeners.Add(this);
        }

        private byte _id;

        public void OnRawRead(IPAddress remoteAddr,byte id,byte [] data,int offset,int size){
            if(id!=_id){
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
                    conn.Id=_id;
                    conn.Start();
                    break;
                }
                conn.Stop();
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
