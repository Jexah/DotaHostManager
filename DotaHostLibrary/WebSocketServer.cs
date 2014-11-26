using Alchemy;
using Alchemy.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostLibrary
{

    // Delegates for asynchronous socket and download events
    public delegate void receiveDel(UserContext c, string[] args);
    public delegate void socketDel(UserContext c);


    public class WebSocketServer
    {
        // Web socket server we gonna use
        private Alchemy.WebSocketServer wsServer;

        // Dictionaries containing the socket and download functions
        private Dictionary<string, List<receiveDel>> wsReceive = new Dictionary<string, List<receiveDel>>();
        private List<socketDel>[] wsHooks = new List<socketDel>[5];

        // List of currently connected users
        private Dictionary<int, UserContext> userIdToContext = new Dictionary<int, UserContext>();
        private Dictionary<UserContext, int> userContextToId = new Dictionary<UserContext, int>();

        // Message queue
        private List<string> wsQueue = new List<string>();

        public const byte RECEIVE = 0;
        public const byte SEND = 1;
        public const byte CONNECT = 2;
        public const byte CONNECTED = 3;
        public const byte DISCONNECTED = 4;


        public WebSocketServer(IPAddress ip, int port)
        {
            // Initialize wsHooks
            for(byte i = 0; i < wsHooks.Length; ++i)
            {
                wsHooks[i] = new List<socketDel>();
            }

            // Hook default functions
            hookDefaultFunctions();

            // Set up websocket server
            wsServer = new Alchemy.WebSocketServer(true, port, ip)
            {
                OnReceive = new Alchemy.OnEventDelegate((c) => { checkAndCall(c, wsReceive); callEventFunc(c, wsHooks[RECEIVE]); }),
                OnSend = new Alchemy.OnEventDelegate((c) => { callEventFunc(c, wsHooks[SEND]); }),
                OnConnect = new Alchemy.OnEventDelegate((c) => { callEventFunc(c, wsHooks[CONNECT]); }),
                OnConnected = new Alchemy.OnEventDelegate((c) => { callEventFunc(c, wsHooks[CONNECTED]); }),
                OnDisconnect = new Alchemy.OnEventDelegate((c) => { callEventFunc(c, wsHooks[DISCONNECTED]); }),
                TimeOut = new TimeSpan(24, 0, 0),
            };
        }

        // Starts the websocket server
        public void start()
        {
            wsServer.Start();
            Helpers.log("[Socket] Server started!");
        }

        // Adds a default hook to onConnected
        private void hookDefaultFunctions()
        {
            addHook(CONNECT, (c) =>
            {
                Helpers.log("[Socket] Connecting...");
            });
            addHook(CONNECTED, (c) =>
            {
                Helpers.log("[Socket] Connected!");
                
                // Loop through queue and send all waiting packets
                for (byte i = 0; i < wsQueue.Count; ++i)
                {
                    c.Send(wsQueue[i], false, false);
                }
                wsQueue.Clear();

                // Assign userid
                for (int i = 0; i < 100000; ++i)
                {
                    if (!userIdToContext.ContainsKey(i))
                    {
                        // Send client uid
                        c.Send("id;" + i, false, false);

                        // Add connected user
                        userIdToContext.Add(i, c);
                        userContextToId.Add(c, i);
                    }
                }
            });
            addHook(DISCONNECTED, (c) =>
            {
                // Remove userid
                Helpers.log("[Socket] Disconnected!");
                userIdToContext.Remove(userContextToId[c]);
                userContextToId.Remove(c);
            });
        }

        // Loops through generic function calls for the given event type
        private void callEventFunc(UserContext c, List<socketDel> funcList)
        {
            // Find key if exists, run function with given args
            for (byte i = 0; i < funcList.Count; ++i)
            {
                funcList[i](c);
            }
        }

        // Checks the received message for matching functions, and calls them all
        private void checkAndCall(UserContext c, Dictionary<string, List<receiveDel>> state)
        {
            // Find key if exists, run function with given args
            string[] args = c.DataFrame.ToString().Split(';');
            if (state.ContainsKey(args[0]))
            {
                for (byte i = 0; i < state[args[0]].Count; ++i)
                {
                    state[args[0]][i](c, args);
                }
            }
        }

        // Adds a hook of the given type
        public void addHook(byte type, socketDel func)
        {
            // Add the hook to the given list
            wsHooks[type].Add(func);
        }
        
        // Adds a hook to onReceive with a given name
        public void addHook(string funcName, receiveDel func)
        {
            // Add the hook to the given dictionary
            if (wsReceive.ContainsKey(funcName))
            {
                wsReceive[funcName].Add(func);
            }
            else
            {
                wsReceive.Add(funcName, new List<receiveDel>() { func });
            }
        }

        // Attempts to send a message to all connected users, otherwise stores it in the queue
        public void send(string message)
        {
            if(userIdToContext.Count != 0)
            {
                foreach(int i in userIdToContext.Keys)
                {
                    userIdToContext[i].Send(message);
                }
            }
            else
            {
                wsQueue.Add(message);
            }
        }

    }
}
