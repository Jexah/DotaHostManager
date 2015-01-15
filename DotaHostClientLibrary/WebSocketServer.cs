using Alchemy.Classes;
using System;
using System.Collections.Generic;
using System.Net;

namespace DotaHostClientLibrary
{

    // Delegates for asynchronous socket and download events
    public delegate void ReceiveDel(UserContext c, string[] args);
    public delegate void SocketDel(UserContext c);


    public class WebSocketServer
    {
        // Web socket server we gonna use
        private readonly Alchemy.WebSocketServer _wsServer;

        // Dictionaries containing the socket and download functions
        private readonly Dictionary<string, List<ReceiveDel>> _wsReceive = new Dictionary<string, List<ReceiveDel>>();
        private readonly List<SocketDel>[] _wsHooks = new List<SocketDel>[5];

        // List of currently connected users
        private readonly Dictionary<string, UserContext> _userIdToContext = new Dictionary<string, UserContext>();
        private readonly Dictionary<UserContext, string> _userContextToId = new Dictionary<UserContext, string>();

        // Message queue
        private readonly List<string> _wsQueue = new List<string>();

        // Message type IDs
        public const byte TypeReceive = 0;
        public const byte TypeSend = 1;
        public const byte TypeConnect = 2;
        public const byte TypeConnected = 3;
        public const byte TypeDisconnected = 4;


        public WebSocketServer(int port)
        {
            // Initialize wsHooks
            for (byte i = 0; i < _wsHooks.Length; ++i)
            {
                _wsHooks[i] = new List<SocketDel>();
            }

            // Hook default functions
            HookDefaultFunctions();

            // Set up websocket server
            _wsServer = new Alchemy.WebSocketServer(false, port, IPAddress.Any)
            {
                OnReceive = c => { CheckAndCall(c, _wsReceive); CallEventFunc(c, _wsHooks[TypeReceive]); },
                OnSend = c => { CallEventFunc(c, _wsHooks[TypeSend]); },
                OnConnect = c => { CallEventFunc(c, _wsHooks[TypeConnect]); },
                OnConnected = c => { CallEventFunc(c, _wsHooks[TypeConnected]); },
                OnDisconnect = c => { CallEventFunc(c, _wsHooks[TypeDisconnected]); },
                TimeOut = new TimeSpan(24, 0, 0)
            };
        }

        // Starts the websocket server
        public void Start()
        {
            _wsServer.Start();
            Helpers.Log("[Socket] Server started!");
        }

        // Adds a default hook to onConnected
        private void HookDefaultFunctions()
        {
            // Log on connect begin
            AddHook(TypeConnect, ConnectHook);

            // Log on connected, send all waiting messages, and store client ID
            AddHook(TypeConnected, ConnectedHook);

            // Remove log disconnected, delete userID
            AddHook(TypeDisconnected, DisconnectedHook);

        }

        private static void ConnectHook(UserContext c)
        {
            Helpers.Log("[Socket] Connecting...");
        }

        private void ConnectedHook(UserContext c)
        {
            Helpers.Log("[Socket] Connected!");

            // Loop through queue and send all waiting packets
            for (byte i = 0; i < _wsQueue.Count; ++i)
            {
                c.Send(_wsQueue[i]);
            }
            _wsQueue.Clear();

            // Assign userid
            string ip = c.ClientAddress.ToString();
            if (_userIdToContext.ContainsKey(ip))
            {
                c.Send(Helpers.PackArguments("id", ip));
                _userIdToContext[ip] = c;
                _userContextToId[c] = ip;
                return;
            }
            // Send client uid
            c.Send(Helpers.PackArguments("id", ip));

            // Add connected user
            try { _userIdToContext.Add(ip, c); }
            catch
            {
                // ignored
            }
            try { _userContextToId.Add(c, ip); }
            catch
            {
                // ignored
            }
        }

        private void DisconnectedHook(UserContext c)
        {
            // Remove userid
            Helpers.Log("[Socket] Disconnected!");
            if (!_userContextToId.ContainsKey(c)) return;

            Helpers.Log("1");

            if (_userIdToContext.ContainsKey(_userContextToId[c]))
            {
                Helpers.Log("2");
                _userIdToContext.Remove(_userContextToId[c]);
            }
            Helpers.Log("3");
            _userContextToId.Remove(c);
            Helpers.Log("4");
        }


        public List<UserContext> GetConnections()
        {
            return new List<UserContext>(_userIdToContext.Values);
        }
        public int GetConnectionsCount()
        {
            return _userIdToContext.Count;
        }

        // Loops through generic function calls for the given event type
        private static void CallEventFunc(UserContext c, IReadOnlyList<SocketDel> funcList)
        {
            // Find key if exists, run function with given args
            for (byte i = 0; i < funcList.Count; ++i)
            {
                funcList[i](c);
            }
        }

        // Checks the received message for matching functions, and calls them all
        private static void CheckAndCall(UserContext c, IReadOnlyDictionary<string, List<ReceiveDel>> state)
        {
            // Find key if exists, run function with given args
            string[] args = c.DataFrame.ToString().Split(Global.MsgSep);
            if (!state.ContainsKey(args[0])) return;
            for (byte i = 0; i < state[args[0]].Count; ++i)
            {
                state[args[0]][i](c, args);
            }
        }

        // Adds a hook of the given type
        public void AddHook(byte type, SocketDel func)
        {
            // Add the hook to the given list
            _wsHooks[type].Add(func);
        }

        // Adds a hook to onReceive with a given name
        public void AddHook(string funcName, ReceiveDel func)
        {
            // Add the hook to the given dictionary
            if (_wsReceive.ContainsKey(funcName))
            {
                _wsReceive[funcName].Add(func);
            }
            else
            {
                _wsReceive.Add(funcName, new List<ReceiveDel> { func });
            }
        }

        // Attempts to send a message to all connected users, otherwise stores it in the queue
        public void Send(string message)
        {
            if (_userIdToContext.Count != 0)
            {
                foreach (var i in _userIdToContext.Keys)
                {
                    _userIdToContext[i].Send(message);
                }
            }
            else
            {
                _wsQueue.Add(message);
            }
        }

        // Sends the message to the connected context with matching ID
        public void Send(string message, string id)
        {
            _userIdToContext[id].Send(message);
        }

    }
}
