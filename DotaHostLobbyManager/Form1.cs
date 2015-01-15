using Alchemy.Classes;
using DotaHostClientLibrary;
using DotaHostLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DotaHostLobbyManager
{
    public partial class Form1 : Form
    {
        // Lobbies list
        private static readonly Lobbies Lobbies = new Lobbies();

        // Websocket server for connecting to clients using browsers
        private static readonly WebSocketServer WsServer = new WebSocketServer(Global.LobbyManagerPort);

        // Websocket client for connecting to the server manager
        private static readonly WebSocketClient WsClient = new WebSocketClient("ws://127.0.0.1:" + Runabove.ServerManagerPort + "/");

        // A dictionary of steamid:player
        private static readonly Dictionary<string, Player> PlayerCache = new Dictionary<string, Player>();

        // Lookup tables for players
        private static readonly Dictionary<string, string> SteamIdtoIp = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> IpToSteamId = new Dictionary<string, string>();

        // A dictionary of steamid:lobby
        private static readonly Dictionary<string, Lobby> PlayersInLobbies = new Dictionary<string, Lobby>();

        // Players that are ready with the lobby key <lobbyName:string, List<steamid:string>>
        private static readonly Dictionary<string, List<string>> PlayersReady = new Dictionary<string, List<string>>();

        // Each lobby has its own timer stored under its name in this dictionary. Simply call the value to dispose the timer.
        private static readonly Dictionary<string, Timers.EndTimer> LobbyNameToTimer = new Dictionary<string, Timers.EndTimer>();

        // Temp lobby
        private static bool _lobbiesChanged;

        // Sets the timer in which lobby info is checked.
        private static readonly System.Timers.Timer LobbySendInterval = new System.Timers.Timer();


        public Form1()
        {
            // Delete the log file if it exists.
            File.Delete("log.txt");

            // Generate the form.
            InitializeComponent();

            // Hook websocket events.
            HookWSocketEvents();

            // Set lobby timer interval, hook the function, and start the timer.
            LobbySendInterval.Interval = 2000;
            LobbySendInterval.Elapsed += LobbySendTick;
            LobbySendInterval.Start();

            // Start the websocket client and server
            WsServer.Start();
            WsClient.Start();
        }

        public static void HookWSocketEvents()
        {
            // Log all incoming streams.
            WsServer.AddHook(WebSocketServer.TypeReceive, ReceiveHook);

            // This is a request for the lobbies from a browser, send lobbies back.
            WsServer.AddHook("getLobbies", GetLobbiesHook);

            // A request from the browser to validate their account
            WsServer.AddHook("validate", ValidateHook);

            // A request from the browser to create a lobby
            WsServer.AddHook("createLobby", CreateLobbyHook);

            // Request from user to join lobby
            WsServer.AddHook("joinLobby", JoinLobbyHook);

            // Get lobby info for a specific lobby.
            WsServer.AddHook("getLobby", GetLobbyHook);

            // Request from a user to swap teams
            WsServer.AddHook("swapTeam", SwapTeamHook);

            // Temp request to start game
            WsServer.AddHook("startGames", StartGamesHook);

            // Gets the current page that the user should be at
            WsServer.AddHook("getPage", GetPageHook);

            // User says something in chat
            WsServer.AddHook("chat", ChatHook);

            // User requests to leave lobby
            WsServer.AddHook("leaveLobby", LeaveLobbyHook);

            // User says they're ready to play
            WsServer.AddHook("ready", ReadyHook);

            // User declines game
            WsServer.AddHook("decline", DeclineHook);



            WsClient.AddHook(WebSocketClient.TypeConnected, ConnectedHook);

            // We got some game server info from the server manager
            WsClient.AddHook("gameServerInfo", GameServerInfoHook);


            WsClient.AddHook("gameServerExit", GameServerExitHook);

        }

        private static void CreateLobbyHook(UserContext c, string[] x)
        {
            // Check args length
            if (x.Length != 2)
            {
                Helpers.Log("createLobby: args too short.");
                return;
            }

            // If the player is recorded as being validated
            if (!PlayerCache.ContainsKey(c.ClientAddress.ToString()))
            {
                // Woah there, player is not validated
                Helpers.Log("2:" + c.ClientAddress);
                Helpers.Log("createLobby: playerCache key not found.");
                return;
            }

            // Player is validated, let's use them
            var player = PlayerCache[c.ClientAddress.ToString()];

            // Create a lobby using the data the user gave us.
            var lobby = new Lobby(Kv.Parse(x[1], true));
            //Lobby lobby = new Lobby();

            // And then override most of it :DDD
            var radiant = new Team
            {
                TeamName = "Radiant",
                MaxPlayers = 5,
                Players = new Players()
            };
            var dire = new Team
            {
                TeamName = "Dire",
                MaxPlayers = 5,
                Players = new Players()
            };
            var unallocated = new Team
            {
                TeamName = "Unallocated",
                MaxPlayers = 10,
                Players = new Players()
            };
            lobby.Teams = new Teams();
            lobby.Teams.AddTeam(radiant);
            lobby.Teams.AddTeam(dire);
            lobby.Teams.AddTeam(unallocated);
            var lod = new Addon
            {
                Id = "lod",
                Options = lobby.Addons.GetAddon("0").Options
            };

            lobby.Addons = new Addons();
            lobby.Addons.AddAddon(lod);
            lobby.MaxPlayers = 10;
            lobby.CurrentPlayers = 0;
            lobby.Region = Runabove.Canada;
            // End temp stuff

            // If the lobby name isn't null, and the name isn't taken
            if (lobby.Name != null && Lobbies.AddLobby(lobby))
            {
                // Create that sucka
                PlayersReady.Add(lobby.Name, new List<string>());
                Helpers.Log("createLobby: send to join lobby.");
                JoinLobby(Lobbies.GetLobby(lobby.Name), player, c);
            }
            else
            {
                // Lobby name was taken or ==null
                Helpers.Log("createLobby: failed");
                c.Send(Helpers.PackArguments("createLobby", "failed"));
            }
        }

        private static void GetLobbiesHook(UserContext c, string[] x)
        {
            // Generate a json string of the lobbies.
            //string lobbiesJson = lobbies.toJSON();
            //byte[] data = ASCIIEncoding.ASCII.GetBytes(lobbiesJson);
            //Console.WriteLine("Original is: " + data.Length.ToString());
            //byte[] compressed = SevenZip.Compression.LZMA.SevenZipHelper.Compress(data);
            //Console.WriteLine("Compressed is: " + compressed.Length.ToString());
            //c.Send(Helpers.packArguments("getLobbies", System.Text.Encoding.Default.GetString(compressed)));

            // Send it
            //c.Send(Helpers.packArguments("getLobbies", lobbiesJson));

            //c.Send(compressed);
        }

        private static void ValidateHook(UserContext c, string[] x)
        {
            // Check args length
            if (x.Length != 3)
            {
                return;
            }

            // Attempt validation
            Validate(x[1], x[2], c.ClientAddress.ToString(), player =>
            {
                // Success

                c.Send(Helpers.PackArguments("validate", "success"));
            }, () => c.Send(Helpers.PackArguments("validate", "failure")));
        }

        private static void JoinLobbyHook(UserContext c, string[] x)
        {
            // Check valid request
            if (x.Length != 2)
            {
                return;
            }
            // Get client IP
            string ip = c.ClientAddress.ToString();

            // If user is invalid, exit
            if (x[1] == null || !PlayerCache.ContainsKey(ip)) return;

            // Get user
            var player = PlayerCache[ip];

            // Check if lobby exists
            if (x[1] != null && Lobbies.ContainsKey(x[1]))
            {
                if (Lobbies.GetLobby(x[1]).Status != Lobby.Waiting)
                {
                    // NOPE
                    c.Send(Helpers.PackArguments("joinLobby", "failed", "active"));
                    return;
                }

                // Lobby exists, attempt to join the lobby.
                JoinLobby(Lobbies.GetLobby(x[1]), player, c);
            }
            else
            {
                // Lobby does not exist
                c.Send(Helpers.PackArguments("joinLobby", "failed", "notFound"));
            }
        }

        private static void GetLobbyHook(UserContext c, string[] x)
        {
            // Check args length
            if (x.Length != 2)
            {
                return;
            }

            // Get lobby (should check it exists)
            var lobby = new Lobby(Lobbies.GetLobby(x[1]));

            // Send lobby info
            c.Send(Helpers.PackArguments("getLobby", lobby.ToString()));
        }

        private static void SwapTeamHook(UserContext c, string[] x)
        {
            // Get their IP
            string ip = c.ClientAddress.ToString();

            // Check args length and that the player is validated
            if (x.Length != 3 || !IpToSteamId.ContainsKey(ip))
            {
                return;
            }

            // Assign args
            string slotId = x[1];
            string teamId = x[2];
            string steamId = IpToSteamId[ip];

            // If this player is not in a lobby
            if (!PlayersInLobbies.ContainsKey(steamId))
            {
                // gtfo
                return;
            }

            // Get the lobby that the player is in.
            var lobby = PlayersInLobbies[steamId];
            if (!lobby.Teams.ContainsKey(teamId) || lobby.Status != Lobby.Waiting)
            {
                // Trying to join an invalid team, gtfo
                // Or lobby is already in ready/active state
                return;
            }

            // Local bool to check if the game is ready
            bool startGame = false;

            // If swapping teams was a failure, return
            if (!SwapTeam(lobby.Teams.GetTeam(teamId), slotId, PlayerCache[ip])) return;

            // Cancel the lobby starting
            CancelLobbyStart(lobby.Name);

            // For each player in the server
            lobby.ForEachPlayer(player =>
            {
                // If steamid cannot be converted to IP, re
                if (!SteamIdtoIp.ContainsKey(player.SteamId)) return;
                // Let them know the swap occured
                WsServer.Send(Helpers.PackArguments("swapTeam", teamId, slotId, steamId), SteamIdtoIp[player.SteamId]);

                // If the game is full
                if (lobby.Teams.GetTeam("0").Players.GetPlayers().Count +
                    lobby.Teams.GetTeam("1").Players.GetPlayers().Count != 2) return;

                // Let them know if the game is starting...
                WsServer.Send("lobbyFull", SteamIdtoIp[player.SteamId]);

                // Set lobby status to READY
                lobby.Status = Lobby.Ready;

                // local bool
                startGame = true;
            });

            // Game should be ready
            if (!startGame) return;

            // If the lobby timer exists, cancel it
            if (LobbyNameToTimer.ContainsKey(lobby.Name))
            {
                CancelLobbyStart(lobby.Name);
                LobbyNameToTimer.Remove(lobby.Name);
            }

            // Begin the timeout for requestGameServer
            LobbyNameToTimer.Add(lobby.Name, Timers.SetTimeout(20, Timers.Seconds, () =>
            {
                // If the number of players ready is full at end of time out
                if (PlayersReady[lobby.Name].Count == 2)//lobby.MaxPlayers)
                {
                    // Get game server
                    RequestGameServer(lobby);
                }
                else
                {
                    // Number of player who are ready is not the full lobby
                    // For each player in the lobby
                    lobby.ForEachPlayer(player =>
                    {
                        // If the player isn't ready
                        if (!PlayersReady[lobby.Name].Contains(player.SteamId))
                        {
                            // Kick them
                            RemoveFromLobby(lobby, player, true);
                        }
                    });
                }
            }));
        }

        private static void StartGamesHook(UserContext c, string[] x)
        {
            foreach (var l in Lobbies.GetLobbies())
            {
                RequestGameServer(l);
            }
        }

        private static void GetPageHook(UserContext c, string[] x)
        {
            // Gets user IP
            string ip = c.ClientAddress.ToString();

            // Checks if they're verified
            if (!IpToSteamId.ContainsKey(ip) || !PlayersInLobbies.ContainsKey(IpToSteamId[ip]))
            {
                // No? Say hello to the home page.
                SendHomePage(c);
                return;
            }

            // Check if they're in a lobby
            if (!PlayersInLobbies.ContainsKey(IpToSteamId[ip])) return;

            // If so, get the lobby info and send it to them.
            var l = PlayersInLobbies[IpToSteamId[ip]];
            c.Send(Helpers.PackArguments("page", "lobby", l.ToJson()));
        }

        private static void ChatHook(UserContext c, string[] x)
        {
            // Get ip
            string ip = c.ClientAddress.ToString();

            // Not verified?
            if (!PlayerCache.ContainsKey(ip))
            {
                // gtfo
                return;
            }

            // Get player
            var player = PlayerCache[ip];

            // Not loitering in a lobby?
            if (!PlayersInLobbies.ContainsKey(player.SteamId))
            {
                // gtfo
                return;
            }

            // Get the lobby that the player is in.
            var lobby = PlayersInLobbies[player.SteamId];

            // For each player in the server
            lobby.ForEachPlayer(p =>
            {
                if (p.SteamId == player.SteamId) return;

                if (SteamIdtoIp.ContainsKey(p.SteamId))
                {
                    // Send them the chat update
                    WsServer.Send(Helpers.PackArguments("chat", player.SteamId, x[1]), SteamIdtoIp[p.SteamId]);
                }
            });
        }

        private static void LeaveLobbyHook(UserContext c, string[] x)
        {
            // Check args length
            if (x.Length != 1)
            {
                return;
            }

            // Get player ip
            string ip = c.ClientAddress.ToString();

            // Is user validated?
            if (!PlayerCache.ContainsKey(ip))
            {
                // Player is not in PlayerCache
                return;
            }

            // Okay let's get their player object
            var player = PlayerCache[c.ClientAddress.ToString()];

            // Check if they're in a lobby
            if (!PlayersInLobbies.ContainsKey(player.SteamId))
            {
                // Player is not in lobby
                return;
            }

            // Yeah they're in a lobby, get their object and remove them from the lobby
            var lobby = PlayersInLobbies[player.SteamId];

            if (lobby.Status != Lobby.Waiting)
            {
                // NOPE
                return;
            }

            RemoveFromLobby(lobby, player, true);

            // Tell them it was successful
            c.Send(Helpers.PackArguments("leaveLobby"));
        }

        private static void ReadyHook(UserContext c, string[] x)
        {
            // Assign IP
            string ip = c.ClientAddress.ToString();

            // Check if ip is not validated
            if (!IpToSteamId.ContainsKey(ip))
            {
                // IP is not valid, exit.
                return;
            }

            // Get steamid
            string steamid = IpToSteamId[ip];

            // Check if player is not in lobby
            if (!PlayersInLobbies.ContainsKey(steamid))
            {
                // Player is not in lobby, exit.
                return;
            }
            // Get lobby
            var lobby = PlayersInLobbies[steamid];

            if (lobby.Status != Lobby.Ready)
            {
                // Lobby is either waiting or active
                return;
            }
            // If player is not ready
            if (PlayersReady[lobby.Name].Contains(steamid)) return;

            // Set player to ready
            PlayersReady[lobby.Name].Add(steamid);

            // If all players are ready
            if (PlayersReady.Count != 2) return;

            // Request game server
            RequestGameServer(lobby);

            // Cancel countdown
            LobbyNameToTimer[lobby.Name]();
        }

        private static void DeclineHook(UserContext c, string[] x)
        {
            string ip = c.ClientAddress.ToString();

            if (!IpToSteamId.ContainsKey(ip)) return;

            string steamid = IpToSteamId[ip];

            if (!PlayersInLobbies.ContainsKey(steamid)) return;

            var lobby = PlayersInLobbies[steamid];

            if (PlayerCache.ContainsKey(ip))
            {
                RemoveFromLobby(lobby, PlayerCache[ip], true);
            }
        }

        private static void GameServerInfoHook(UserContext c, string[] x)
        {
            // Lets generate a game server based on the info they gave us.
            var gameServer = new GameServer(Kv.Parse(x[2]));
            var lobby = gameServer.Lobby;

            // Are there teams in the game server?
            if (lobby.Teams == null) return;

            // Great, for each of them, set the LoD defaults.
            foreach (var kvp in gameServer.Lobby.Teams.GetKeys())
            {
                if (kvp.Value != null) continue;

                var team = new Team
                {
                    TeamName = kvp.Key,
                    MaxPlayers = 5,
                    Players = new Players()
                };
                gameServer.Lobby.Teams.AddTeam(team);
            }

            // For each player in each team...
            lobby.ForEachPlayer(player =>
            {
                if (x[1] == "success")
                {
                    // Tell them the game server is ready
                    WsServer.Send(Helpers.PackArguments("gameServerInfo", "success", gameServer.Ip.Split(':')[0] + ":" + gameServer.Port), SteamIdtoIp[player.SteamId]);
                }
                else
                {
                    // Tell them sumphing fukt up
                    WsServer.Send(Helpers.PackArguments("gameServerInfo", "failed", SteamIdtoIp[player.SteamId]));
                }
            });
        }

        private static void GameServerExitHook(UserContext c, string[] x)
        {
            Helpers.Log("gameServerExit received");
            var gameServer = new GameServer(Kv.Parse(x[1]));
            var lobby = gameServer.Lobby;
            lobby.ForEachPlayer(player =>
            {
                if (!SteamIdtoIp.ContainsKey(player.SteamId)) return;

                string playerIp = SteamIdtoIp[player.SteamId];
                SendHomePage(playerIp);
            });
            DeleteLobby(gameServer.Lobby);
        }

        private static void ConnectedHook(UserContext c)
        {

            c.Send("lobbyManager");
        }

        private static void ReceiveHook(UserContext c)
        {

            Helpers.Log("Received: " + c.DataFrame);
        }


        // This gets called on each tick of the lobby timer interval
        private static void LobbySendTick(Object myObject, EventArgs myEventArgs)
        {

            // Have the lobbies not changed yet?
            if (!_lobbiesChanged)
            {
                // gtfo
                return;
            }

            Helpers.Log(_lobbiesChanged.ToString());

            // Set lobbies changed to false, so we don't send the same info again.
            _lobbiesChanged = false;

            // Lobbies have changed, for each player validated...
            foreach (var kvp in PlayerCache.Where(kvp => !PlayersInLobbies.ContainsKey(kvp.Value.SteamId)).Where(kvp => SteamIdtoIp.ContainsKey(kvp.Value.SteamId)))
            {
                SendHomePage(SteamIdtoIp[kvp.Value.SteamId]);
            }
        }

        // Send the player their homepage
        private static void SendHomePage(UserContext c)
        {
            SendHomePage(c.ClientAddress.ToString());
        }

        private static void DeleteLobby(Lobby lobby)
        {
            DeleteLobby(lobby.Name);
        }

        private static void DeleteLobby(string lobbyName)
        {
            Lobbies.GetLobby(lobbyName).ForEachPlayer(player =>
            {
                PlayersInLobbies.Remove(player.SteamId);
            });
            Lobbies.RemoveLobby(lobbyName);
            _lobbiesChanged = true;
            PlayersReady.Remove(lobbyName);
        }

        // Send the player their homepage
        private static void SendHomePage(string ip)
        {
            var lobbiesWaiting = new Lobbies(Lobbies.GetLobbies().Where(lobby => lobby.Status == Lobby.Waiting));
            string lobbiesJson = lobbiesWaiting.ToJson();
            //byte[] data = ASCIIEncoding.ASCII.GetBytes(lobbiesJson);
            WsServer.Send(Helpers.PackArguments("page", "home", lobbiesJson), ip);
        }

        private static void CancelLobbyStart(string lobbyName)
        {
            if (LobbyNameToTimer.ContainsKey(lobbyName))
            {
                LobbyNameToTimer[lobbyName]();
            }

            if (!Lobbies.ContainsKey(lobbyName)) return;

            var lobby = Lobbies.GetLobby(lobbyName);

            lobby.ForEachPlayer(player =>
            {
                if (SteamIdtoIp.ContainsKey(player.SteamId))
                {
                    WsServer.Send("cancelBeginGame", SteamIdtoIp[player.SteamId]);
                }
            });
        }

        private static bool SwapTeam(Team newTeam, string newSlot, Player player)
        {
            if (newTeam.Players.GetPlayers().Count < newTeam.MaxPlayers)
            {
                Helpers.Log("1");
                if (newTeam.Players.ContainsKey(newSlot))
                {
                    Helpers.Log("something");
                    Helpers.Log(newTeam.Players.GetKv(newSlot).ToString());
                }
                Helpers.Log("2");
                if (!newTeam.Players.ContainsKey(newSlot) && newTeam.Players.GetKv(newSlot) != null)
                {
                    return false;
                }
                Helpers.Log("3");
                if (newTeam.Players.GetKv(newSlot) != null)
                {
                    return false;
                }

                var lobby = PlayersInLobbies[player.SteamId];
                RemoveFromLobby(lobby, player, false);
                newTeam.Players.AddPlayer(player, newSlot);
                return true;
            }
            Helpers.Log("5");
            return false;
        }

        private static void JoinLobby(Lobby lobby, Player player, UserContext c)
        {
            bool joined = false;
            if (player.SteamId == null)
            {
                Helpers.Log("Join lobby: Steam ID invalid");
                return;
            }
            if (lobby.Teams != null)
            {
                foreach (var kvp in lobby.Teams.GetKeys())
                {
                    if (kvp.Value != null) continue;

                    var team = new Team
                    {
                        TeamName = kvp.Key,
                        MaxPlayers = 5,
                        Players = new Players()
                    };
                    lobby.Teams.AddTeam(team);
                }
                if (PlayersInLobbies.ContainsKey(player.SteamId))
                {
                    RemoveFromLobby(PlayersInLobbies[player.SteamId], player, true);
                }
                var unallocated = lobby.Teams.GetTeam("2");
                if (unallocated.Players == null)
                {
                    unallocated.Players = new Players();
                }
                if (unallocated.Players.GetKeys().Count < unallocated.MaxPlayers)
                {
                    unallocated.Players.AddPlayer(player);
                    joined = true;
                    lobby.CurrentPlayers++;
                    PlayersInLobbies.Add(player.SteamId, lobby);
                    _lobbiesChanged = true;
                }
            }
            if (!joined)
            {
                Helpers.Log("Join lobby failed: Full");
                c.Send(Helpers.PackArguments("joinLobby", "failed", "full"));
            }
            else
            {
                Helpers.Log("Join lobbysuccess");
                c.Send(Helpers.PackArguments("joinLobby", "success", lobby.ToJson()));
                CancelLobbyStart(lobby.Name);
                lobby.ForEachPlayer(p =>
                {
                    if (player.SteamId == p.SteamId) return;

                    WsServer.Send(Helpers.PackArguments("addPlayerToLobby", player.ToJson(), "2"), SteamIdtoIp[p.SteamId]);

                    if (lobby.Teams.GetTeam("0").Players.GetPlayers().Count + lobby.Teams.GetTeam("1").Players.GetPlayers().Count == 2)//lobby.MaxPlayers)
                    {
                        WsServer.Send("lobbyFull", SteamIdtoIp[p.SteamId]);
                    }
                });
                if (lobby.Teams.GetTeam("0").Players.GetPlayers().Count +
                    lobby.Teams.GetTeam("1").Players.GetPlayers().Count != 2) return;

                if (LobbyNameToTimer.ContainsKey(lobby.Name))
                {
                    CancelLobbyStart(lobby.Name);
                }
                LobbyNameToTimer.Add(lobby.Name, Timers.SetTimeout(5, Timers.Seconds, () =>
                {
                    Helpers.Log("Requested game server");
                    RequestGameServer(lobby);
                }));
            }
        }

        public static void RemoveFromLobby(Lobby lobby, Player player, bool exit)
        {
            string teamid = "";
            string slotid = "";
            foreach (var teamsKvp in lobby.Teams.GetKeys())
            {
                var t = new Team(teamsKvp.Value);
                foreach (var playersKvp in from playersKvp in t.Players.GetKeys() let p = new Player(playersKvp.Value) where p.SteamId == player.SteamId select playersKvp)
                {
                    teamid = teamsKvp.Key;
                    slotid = playersKvp.Key;
                }
            }
            foreach (var team in lobby.Teams.GetTeams().Where(team => RemoveFromTeam(team, player)))
            {
                if (exit)
                {
                    PlayersInLobbies.Remove(player.SteamId);
                    lobby.CurrentPlayers--;
                    if (lobby.CurrentPlayers == 0)
                    {
                        DeleteLobby(lobby);
                    }
                }
                if (LobbyNameToTimer.ContainsKey(lobby.Name))
                {
                    LobbyNameToTimer[lobby.Name]();
                }
                _lobbiesChanged = true;
                if (exit)
                {
                    lobby.ForEachPlayer(p =>
                    {
                        if (p.SteamId == player.SteamId) return;

                        if (SteamIdtoIp.ContainsKey(p.SteamId))
                        {
                            WsServer.Send(Helpers.PackArguments("leaveLobby", slotid, teamid), SteamIdtoIp[p.SteamId]);
                        }
                    });
                }
            }
        }

        public static bool RemoveFromTeam(Team team, Player player)
        {
            var toRemove = team.Players.GetPlayers().Find(item => item.SteamId == player.SteamId);

            if (toRemove == null) return false;

            team.Players.RemovePlayer(toRemove);

            return true;
        }

        private static void Validate(string token, string steamid, string ip, Action<Player> successCallback, Action failureCallback)
        {
            if (PlayerCache.ContainsKey(ip))
            {
                successCallback(PlayerCache[ip]);
                return;
            }
            var data = new Dictionary<string, string>
            {
                {"token", token}, 
                {"steamID", steamid}
            };

            HttpRequestManager.StartRequest("http://127.0.0.1/validate.php", "GET", r =>
            {
                Helpers.Log(r);
                if (r != "get the fuck out of here")
                {
                    // Do stuff with r (response) to get it into 4 variables, rest is complete

                    var player = new Player(Kv.Parse(r, true));

                    Helpers.Log("1: " + ip);
                    PlayerCache.Add(ip, player);
                    try
                    {
                        if (SteamIdtoIp.ContainsKey(steamid))
                        {
                            if (IpToSteamId.ContainsKey(SteamIdtoIp[steamid]))
                            {
                                IpToSteamId.Remove(SteamIdtoIp[steamid]);
                            }
                            SteamIdtoIp[steamid] = ip;
                        }
                        else
                        {
                            SteamIdtoIp.Add(steamid, ip);
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                    try
                    {
                        IpToSteamId.Add(ip, steamid);
                    }
                    catch
                    {
                        // ignored
                    }


                    successCallback(player);

                }
                else
                {
                    failureCallback();
                }
            }, data);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        private static void RequestGameServer(Lobby lobby)
        {
            // Request game server from ServerManager
            WsClient.Send(Helpers.PackArguments("createGameServer", lobby.ToString()));

            // Tell each player the game is starting
            lobby.ForEachPlayer(player =>
            {
                if (SteamIdtoIp.ContainsKey(player.SteamId))
                {
                    WsServer.Send("generatingServer", SteamIdtoIp[player.SteamId]);
                }
            });
        }

        public static List<int> Compress(string uncompressed)
        {
            // build the dictionary
            var dictionary = new Dictionary<string, int>();
            for (int i = 0; i < 256; i++)
            {
                dictionary.Add(((char)i).ToString(), i);
            }

            string w = string.Empty;
            var compressed = new List<int>();

            foreach (char c in uncompressed)
            {
                string wc = w + c;
                if (dictionary.ContainsKey(wc))
                {
                    w = wc;
                }
                else
                {
                    // write w to output
                    compressed.Add(dictionary[w]);
                    // wc is a new sequence; add it to the dictionary
                    dictionary.Add(wc, dictionary.Count);
                    w = c.ToString();
                }
            }

            // write remaining output if necessary
            if (!string.IsNullOrEmpty(w))
                compressed.Add(dictionary[w]);

            return compressed;
        }

        public static string Decompress(List<int> compressed)
        {
            // build the dictionary
            var dictionary = new Dictionary<int, string>();
            for (int i = 0; i < 256; i++)
            {
                dictionary.Add(i, ((char)i).ToString());
            }

            string w = dictionary[compressed[0]];
            compressed.RemoveAt(0);
            var decompressed = new StringBuilder(w);

            foreach (int k in compressed)
            {
                string entry = null;
                if (dictionary.ContainsKey(k))
                    entry = dictionary[k];
                else if (k == dictionary.Count)
                    entry = w + w[0];

                decompressed.Append(entry);

                // new sequence; add it to the dictionary
                if (entry == null) continue;

                dictionary.Add(dictionary.Count, w + entry[0]);

                w = entry;
            }

            return decompressed.ToString();
        }

    }
}
