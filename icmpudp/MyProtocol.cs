using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICMPTunnel
{
    class MyProtocol : ProtocolHeader
    {
        public const int MyProtocolHeaderLength = 2;
        public byte Id;
        public override byte[] GetProtocolPacketBytes(byte[] payLoad)
        {
            byte[] res = new byte[payLoad.Length + MyProtocolHeaderLength];
            res[1] = Id;
            Array.Copy(payLoad, 0, res, MyProtocolHeaderLength, payLoad.Length);

            Crc crc = new Crc();
            res[0] = crc.Update(res, 1, res.Length - 1);

            return res;
        }
    }
}
