using System.Net;
using System.Net.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace ICMPTunnel
{
public class Connection{

        public Logger Log = new Logger();
        IPAddress _rawAddress;
        IPAddress _myAddress;
        public Socket UdpSocket;
        IPEndPoint _targetPoint;
        IPEndPoint _listenPoint;

        private ConcurrentDictionary<IPEndPoint,byte> _clients=new ConcurrentDictionary<IPEndPoint,byte>();

        public void Stop() {
            Log.I("stop");
            GlobalEventLoop.Instance().RemoveSocket(UdpSocket);
            if (!UdpSocket.Connected)
            {
                return;
            }
            UdpSocket.Disconnect(false);
            UdpSocket.Close();
        }


        public Connection(IPAddress rawAddress,IPAddress myAddress,IPEndPoint udpTargetPoint=null, IPEndPoint listenEndPoint=null){
            _rawAddress=rawAddress;
            _myAddress = myAddress;
            UdpSocket = new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);

            if (listenEndPoint == null)
            {
                //this is used as udp client by TunnelServer
                 UdpSocket.Bind(new IPEndPoint(IPAddress.Any,0));
                _targetPoint =udpTargetPoint;
            }else
            {
                //this is used as udp server by TunnelClient  
                Log.I("listen on {0}", listenEndPoint);
                _listenPoint = listenEndPoint;

                var sioUdpConnectionReset = -1744830452;
                var inValue = new byte[] { 0 };
                var outValue = new byte[] { 0 };
                UdpSocket.IOControl(sioUdpConnectionReset, inValue, outValue);
                UdpSocket.Bind(listenEndPoint);
            }
        }

        public byte Id {set;get;}

        public void Start()
        {
            Log.I("register self to event loop");
            GlobalEventLoop.Instance().AddSocket(UdpSocket, OnUdpRead, null, OnUdpError);
        }

        private void OnUdpError()
        {
            Log.E("udp error");
        }

        public void SendToUdp(byte[] data,int offset,int size)
        {
            if (_targetPoint != null)
            {
                object[] state = new object[2];
                state[0] = size;
                state[1] = _targetPoint;

                UdpSocket.BeginSendTo(data, offset, size, SocketFlags.None, _targetPoint, new AsyncCallback(
                            OnUdpWriteComplete), state);
                return;
            }

            var enu =_clients.GetEnumerator();

            while (enu.MoveNext())
            {
                object[] state = new object[2];
                state[0] = size;
                state[1] = enu.Current.Key;

                UdpSocket.BeginSendTo(data, offset, size, SocketFlags.None, enu.Current.Key, new AsyncCallback(
                            OnUdpWriteComplete), state);
            }

        }


        public void OnUdpWriteComplete(IAsyncResult ia){
            var state = (object[])(ia.AsyncState);

            int shouldSend=(int)(state[0]);

            var endPoint= state[1] as IPEndPoint;

            int sended=UdpSocket.EndSendTo(ia);

            if(sended!=shouldSend){
                Log.E("written to {0}: {1} != {2}",endPoint, sended, shouldSend);
            }else
            {
                Log.D("written to {0}: {1}", endPoint,sended);
            }
        }
            


        private void OnUdpRead(){

            Log.T("Udp is ready to read from {0}",_targetPoint!=null?_targetPoint.ToString():"");
            
            var buf=new byte[2048];

            var cast =(EndPoint) (_targetPoint ?? new IPEndPoint(IPAddress.Any,0));

            try
            {
                object[] state = new object[2] { buf,cast};
                UdpSocket.BeginReceiveFrom(buf,0,buf.Length,SocketFlags.None,
                    ref cast,new AsyncCallback(OnUdpReadComplete),state);

            }catch(SocketException ex)
            {
                Log.E("udp read failed {0}", ex.ErrorCode);
            }
        }


        private void OnUdpReadComplete(IAsyncResult ar){
            var state=(object[])ar.AsyncState;
            var buf = (byte[])(state[0]);
            var endPoint = (EndPoint)(state[1]);
            int readed=UdpSocket.EndReceiveFrom(ar,ref endPoint);

            _clients.TryAdd(endPoint as IPEndPoint, 1);

            Log.T("received form udp {0} bytes",readed);

            TransportLayer.Instance().WritePacket(_rawAddress,Id,buf,0,readed);
        }
    }
}
