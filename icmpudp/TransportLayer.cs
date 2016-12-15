using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections;
using System;

namespace ICMPTunnel
{
    interface RawEventListener{
        void OnRawRead(IPAddress remoteAddr,byte id,byte[] data,int offset,int size);
        void OnRawError();
    }

    class TransportLayer{



        static readonly int ICMP_MY_HEADER_LENGTH = IcmpHeader.IcmpHeaderLength+MyProtocol.MyProtocolHeaderLength;
        private static TransportLayer _instance =new TransportLayer();

        public static TransportLayer Instance(){return _instance;}

        public List<RawEventListener> Listeners=new List<RawEventListener>();

        TransportLayer(){

            Log.Name = "TransportLayer";

            var localAddress = Utils.DetectHost();
            if (localAddress == null)
            {
                throw new SocketException();
            }

            RawSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            RawSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, false);
            RawSocket.Bind(new IPEndPoint(localAddress, 0));
            RawSocket.IOControl(IOControlCode.ReceiveAll, new byte[] { 1, 0, 0, 0 }, new byte[] { 1, 0, 0, 0 });

            GlobalEventLoop.Instance().AddSocket(RawSocket,OnRawRead,null,OnRawError);

        }

        private void OnRawError(){
            foreach(var l in Listeners){
                l.OnRawError();
            }
        }


        public Socket RawSocket;

        public static readonly int MY_HEADER_OFFSET_IP_HEADER = Ipv4Header.Ipv4HeaderLength+IcmpHeader.IcmpHeaderLength;
        public static readonly int PAYLOAD_OFFSET_IP_HEADER = Ipv4Header.Ipv4HeaderLength + ICMP_MY_HEADER_LENGTH;

        public void SendToRaw(IPAddress rawAddress,byte id,byte[] buf,int offset,int size){

            var headerList = new ArrayList();
            

            var icmpHeader=new IcmpHeader();
            icmpHeader.Id = 123;
            icmpHeader.Sequence = 24;
            icmpHeader.Type = IcmpHeader.EchoReplyType;
            icmpHeader.Code = IcmpHeader.EchoReplyCode;

            headerList.Add(icmpHeader);


            var myheader=new MyProtocol();
            myheader.Id=(byte)(byte.MaxValue-id);
            headerList.Add(myheader);

            byte[] sub=new byte[size];
            Array.Copy(buf,offset,sub,0,size);

            var rawpacket=ProtocolHeader.BuildPacket(headerList,sub);

            RawSocket.BeginSendTo(rawpacket, 0, rawpacket.Length, SocketFlags.None,
                    new IPEndPoint(rawAddress,0),
                new AsyncCallback(TransportLayer.Instance().OnRawWriteComplete), rawpacket.Length);
        }
        private void OnRawReadComplete(IAsyncResult ia)
        {

            byte[] buf = (byte[])ia.AsyncState;

            int readed=RawSocket.EndReceive(ia);

            if(readed<= PAYLOAD_OFFSET_IP_HEADER){
                Log.D("raw bytes {0} <= ip+icmp header length,removed",readed);
                return;
            }


            var ipLength=(ushort)(buf[2]*256+buf[3]);

            var srcAddr = new IPAddress(new byte[]{buf[12],buf[13],buf[14],buf[15]});

            var icmpWithPayloadLength = ipLength - Ipv4Header.Ipv4HeaderLength;
            Log.D("receiving from {0} ,icmp with payload should be {1} bytes",srcAddr,icmpWithPayloadLength);


            //now buf contains whole packet
            ushort checksum = IcmpHeader.ComputeChecksum(buf,Ipv4Header.Ipv4HeaderLength,icmpWithPayloadLength);

            if (checksum != 0)
            {
                Log.E("icmp checksum {0} error, removed", checksum);
                //invalid pack
                return;
            }
            var crc=new Crc();

            var payloadLength=ipLength-PAYLOAD_OFFSET_IP_HEADER;
            byte crc8 = crc.Update(buf,MY_HEADER_OFFSET_IP_HEADER+1,payloadLength+1);

            byte packetCrc8=buf[MY_HEADER_OFFSET_IP_HEADER];

            if(crc8!=packetCrc8){
                Log.E("calced crc {0}!= {1} failed, removed",crc8,packetCrc8);
                return;
            }

            // all ok

            foreach(var l in Listeners){
                l.OnRawRead(srcAddr,buf[MY_HEADER_OFFSET_IP_HEADER+1],buf,PAYLOAD_OFFSET_IP_HEADER,payloadLength);
            }
        }




        public void OnRawWriteComplete(IAsyncResult ar){
            int shouldWrite=(int)ar.AsyncState;
            int written=RawSocket.EndSend(ar);
            if (written!=shouldWrite){
                Log.E("written to rawsocket {0} < should writen {1}", written,shouldWrite);
            }

            Log.T("written to rawsocket {0} bytes", written);
        }



        public Logger Log = new Logger();

        public void OnRawRead()
        {

            byte[] buf = new byte[2048];

            RawSocket.BeginReceive(buf, 0, buf.Length, SocketFlags.None,new AsyncCallback(OnRawReadComplete), buf);
        }


    }
}
