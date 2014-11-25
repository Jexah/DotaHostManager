using Alchemy;
using Alchemy.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DotaHostLibrary
{

    // Delegates for asynchronous socket and download events
    public delegate void socketDel(string[] args);


    public class WebSocketServer
    {
        // Web socket server we gonna use
        private Alchemy.WebSocketServer wsServer;

        // Context to keep track of (most recent connection)
        private UserContext gContext;

        // Dictionaries containing the socket and download functions
        private Dictionary<string, socketDel>[] wsHooks = new Dictionary<string, socketDel>[5];
        
        public const int RECEIVE = 0;
        public const int SEND = 1;
        public const int CONNECT = 2;
        public const int CONNECTED = 3;
        public const int DISCONNECTED = 4;


        public WebSocketServer(IPAddress ip, int port)
        {
            // Set up the dictionaries
            for(byte i = 0; i < 5; ++i)
            {
                wsHooks[i] = new Dictionary<string, socketDel>();
            }

            // Set up websocket server
            wsServer = new Alchemy.WebSocketServer(port, ip)
            {
                OnReceive = new Alchemy.OnEventDelegate((c) => { checkAndCall(c, wsHooks[RECEIVE]); }),
                OnSend = new Alchemy.OnEventDelegate((c) => { checkAndCall(c, wsHooks[SEND]); }),
                OnConnect = new Alchemy.OnEventDelegate((c) => { checkAndCall(c, wsHooks[CONNECT]); }),
                OnConnected = new Alchemy.OnEventDelegate((c) => { checkAndCall(c, wsHooks[CONNECTED]); }),
                OnDisconnect = new Alchemy.OnEventDelegate((c) => { checkAndCall(c, wsHooks[DISCONNECTED]); }),
                TimeOut = new TimeSpan(24, 0, 0),
            };
            wsServer.Start();
        }

        // Checks if the message has a paired function, and if so, calls it
        private void checkAndCall(UserContext c, Dictionary<string, socketDel> state)
        {
            // Find key if exists, run function with given args
            string[] args = c.DataFrame.ToString().Split('|');
            if (state.ContainsKey(args[0]))
            {
                state[args[0]](args);
            }
        }

        public void addHook(int type, string id, socketDel func)
        {
            // Add the hook to the given dictionary
            wsHooks[type].Add(id, func);
        }

    }
}
