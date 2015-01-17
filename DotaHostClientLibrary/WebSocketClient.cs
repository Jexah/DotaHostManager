using Alchemy.Classes;
using System.Collections.Generic;

namespace DotaHostClientLibrary
{
    public class WebSocketClient
    {
        // Web socket client we gonna use
        private readonly Alchemy.WebSocketClient _wsClient;

        // Dictionarie containing the socket and download functions
        private readonly Dictionary<string, List<ReceiveDel>> _wsReceive = new Dictionary<string, List<ReceiveDel>>();


        private readonly List<SocketDel>[] _wsHooks = new List<SocketDel>[5];
        public delegate void FailedConnection();

        // Message queue
        private readonly List<string> _wsQueue = new List<string>();

        public const byte TypeReceive = 0;
        public const byte TypeSend = 1;
        public const byte TypeConnect = 2;
        public const byte TypeConnected = 3;
        public const byte TypeDisconnected = 4;

        // Connection toserver
        private UserContext _gContext;
        private FailedConnection _failedFunc;

        public WebSocketClient(string connectionLocation)
        {
            // Initialize wsHooks
            for (byte i = 0; i < _wsHooks.Length; ++i)
            {
                _wsHooks[i] = new List<SocketDel>();
            }

            // Set up websocket server
            _wsClient = new Alchemy.WebSocketClient(connectionLocation)
            {
                OnReceive = c => { CheckAndCall(c, _wsReceive); CallEventFunc(c, _wsHooks[TypeReceive]); },
                OnSend = c => { CallEventFunc(c, _wsHooks[TypeSend]); },
                OnConnect = c => { CallEventFunc(c, _wsHooks[TypeConnect]); },
                OnConnected = c => { CallEventFunc(c, _wsHooks[TypeConnected]); },
                OnDisconnect = c => { CallEventFunc(c, _wsHooks[TypeDisconnected]); }
            };

            // Hook default functions
            HookDefaultFunctions();
        }

        // Connects the websocket client to the server
        public void Start()
        {
            _wsClient.Disconnect();
            _wsClient.Connect();
        }

        // Sets on failure function
        public void OnFailure(FailedConnection func)
        {
            _failedFunc = func;
        }

        // Adds a default hook to onConnected
        private void HookDefaultFunctions()
        {
            AddHook(TypeConnect, c =>
            {
                Helpers.Log("[Socket] Connecting...");
            });
            AddHook(TypeConnected, c =>
            {
                Helpers.Log("[Socket] Connected!");

                // Loop through queue and send all waiting packets
                for (byte i = 0; i < _wsQueue.Count; ++i)
                {
                    //c.Send(wsQueue[i], false, false);
                }
                _wsQueue.Clear();

                // Set gContext to this connection
                _gContext = c;
            });
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
            var args = c.DataFrame.ToString().Split(Global.MsgSep);

            // If there is no key, return
            if (!state.ContainsKey(args[0])) return;

            // There is a key, loop through the functions
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

        // Attempts to send a message to gContext, otherwise stores it in the queue
        public void Send(string message)
        {
            if (_gContext != null)
            {
                _gContext.Send(message);
            }
            else
            {
                _wsQueue.Add(message);
            }
        }
    }
}
