using System;

namespace ICMPTunnel
{
public class Logger
    {
        public const int DefaultLevel = 0;
        public string Name="";
        public int Level;

        public Logger(int level=DefaultLevel)
        {
            Level = level;
        }
        public void E(string fmt,params object[] objs)
        {
            Print(4, fmt, objs);
        }
        public void W(string fmt,params object[] objs)
        {
            Print(3, fmt, objs);
        }
        public void T(string fmt,params object[] objs)
        {
            Print(0, fmt, objs);
        }
        public void D(string fmt,params object[] objs)
        {
            Print(1, fmt, objs);
        }
        public void I(string fmt,params object[] objs)
        {
            Print(2, fmt, objs);
        }

        private void Print(int level,string fmt,params object[] objs)
        {
            if (level < Level)
            {
                return;
            }
            Console.WriteLine((Name==""?"":"["+Name+"]:") + fmt, objs);
        }
    }
}
