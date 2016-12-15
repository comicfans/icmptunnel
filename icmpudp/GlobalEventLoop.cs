using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICMPTunnel
{
    public class GlobalEventLoop
    {

        static public GlobalEventLoop Instance() {return _instance;}

        static private GlobalEventLoop _instance = new GlobalEventLoop();

        class SocketAction {
            public Action[] RWE=new Action[3] ;
        }

        class TimerAction
        {
            public int Wait;
            public Action<object> Action;
            public object Param;
        }

        Dictionary<Socket, SocketAction> _socketActions=new Dictionary<Socket, SocketAction>();

        private List<Socket> _sockets=new List<Socket>();

        private BlockingCollection<object> _pendingAdd = new BlockingCollection<object>();
        private BlockingCollection<Socket> _pendingRemove= new BlockingCollection<Socket>();

        private List<TimerAction> _sleepingMs=new List<TimerAction>();

        public void Invoke(Action<object> func,object para){
            AddTimerFunc(0,func,para);
        }

        public void AddSocket(Socket socket,Action read,Action write,Action error)
        {

            SocketAction action = new SocketAction();
            action.RWE[0]= read;
            action.RWE[1]= write;
            action.RWE[2]= error;

            _pendingAdd.Add(new object[] { socket, action });
        }

        public void AddTimerFunc(int wait,Action<object> timerFunc,object param) {
            TimerAction a = new TimerAction();
            a.Wait = wait;
            a.Action = timerFunc;
            a.Param = param;
            _pendingAdd.Add(a);
        }

        int IDLE_MS = 15;
        private bool _running = false;
        
        public void RemoveSocket(Socket toremove)
        {
            _pendingRemove.Add(toremove);
        }

        public void Stop()
        {
            _running = false;
        }
        public void EventLoop()
        {

            _running = true;
            while (_running)
            {

                object action;
                while(_pendingAdd.TryTake(out action))
                {

                    if (action is TimerAction)
                    {
                        //is timer
                        _sleepingMs.Add((TimerAction)action);
                    }
                    else
                    {
                        object[] keyAndAction = (object[])(action);
                        Socket socket = (Socket)keyAndAction[0];
                        if (!_socketActions.ContainsKey(socket))
                        {
                            _socketActions.Add(socket, (SocketAction)keyAndAction[1]);
                            _sockets.Add(socket);
                        }
                    }
                }

                _sleepingMs.Sort((lhs,rhs)=> {
                    int l = (int)(lhs.Wait);
                    int r = (int)(rhs.Wait);
                    return l == r ? 0 :
                    (l < r ? 1 : -1);
                });

                int removed=0;
                foreach(var a in _sleepingMs){
                    if (a.Wait==0){
                        a.Action(a.Param);
                        ++removed;
                    }else{
                        break;
                    }
                }

                _sleepingMs.RemoveRange(0,removed);
                


                Socket toRemove;
                bool hasRemove =_pendingRemove.TryTake(out toRemove);
                if (hasRemove)
                {
                    _socketActions.Remove(toRemove);
                    _sockets.Remove(toRemove);
                }


                var check=new List<Socket>[3];

                for(int i = 0; i < check.Length; ++i)
                {
                    check[i]=new List<Socket>(_sockets);
                }
                

                TimerAction waitAndAction=null;
                if (_sleepingMs.Count > 0)
                {
                    waitAndAction = _sleepingMs[0];
                    _sleepingMs.RemoveAt(0);
                }
                int sleep = (waitAndAction == null ? IDLE_MS : Math.Min(IDLE_MS,waitAndAction.Wait));
                Socket.Select(check[0], check[1], check[2], sleep);

                for(int i = 0; i < check.Length; ++i)
                {
                    foreach(var socket in check[i])
                    {
                        var inv=_socketActions[socket].RWE[i];
                        if(inv!=null){
                            inv.Invoke();
                        }
                    }
                }

                if (waitAndAction!= null)
                {
                    waitAndAction.Action.Invoke(waitAndAction.Param);
                }


                
                removed=0;
                foreach(var a in _sleepingMs){
                    a.Wait -= sleep;
                    if (a.Wait <= 0)
                    {
                        a.Action.Invoke(a.Param);
                        ++removed;
                    }else{
                        break;
                    }
                }
                _sleepingMs.RemoveRange(0,removed);
            }
        }
    }
}
