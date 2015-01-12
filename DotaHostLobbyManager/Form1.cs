using Alchemy.Classes;
using DotaHostClientLibrary;
using DotaHostLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace DotaHostLobbyManager
{
    public partial class Form1 : Form
    {
        // Lobbies list
        private static Lobbies lobbies = new Lobbies();

        // Websocket server for connecting to clients using browsers
        private static WebSocketServer wsServer = new WebSocketServer(Global.LOBBY_MANAGER_PORT);

        // Websocket client for connecting to the server manager
        private static WebSocketClient wsClient = new WebSocketClient("ws://127.0.0.1:" + Runabove.SERVER_MANAGER_PORT + "/");

        // A dictionary of steamid:player
        private static Dictionary<string, Player> playerCache = new Dictionary<string, Player>();

        // Lookup tables for players
        private static Dictionary<string, string> steamIDToIP = new Dictionary<string, string>();
        private static Dictionary<string, string> ipToSteamID = new Dictionary<string, string>();

        // A dictionary of steamid:lobby
        private static Dictionary<string, Lobby> playersInLobbies = new Dictionary<string, Lobby>();

        // Each lobby has its own timer stored under its name in this dictionary. Simply call the value to dispose the timer.
        private static Dictionary<string, Timers.endTimer> lobbyNameToTimer = new Dictionary<string, Timers.endTimer>();

        // Temp lobby
        private static Lobby l = new Lobby();
        private static bool lobbiesChanged = false;

        // Sets the timer in which lobby info is checked.
        private static System.Timers.Timer lobbySendInterval = new System.Timers.Timer();


        public Form1()
        {
            // Delete the log file if it exists.
            File.Delete("log.txt");

            // Generate the form.
            InitializeComponent();

            // Hook websocket events.
            hookWSocketEvents();

            // Set lobby timer interval, hook the function, and start the timer.
            lobbySendInterval.Interval = 2000;
            lobbySendInterval.Elapsed += lobbySendTick;
            lobbySendInterval.Start();

            // Temp lobby
            Addons ads = new Addons();
            Addon ad = new Addon();
            ad.Id = "lod";
            ad.Options = new Options();
            ad.Options.setOption("pickingMode", "All Pick");
            ads.addAddon(ad);
            l.Addons = ads;
            l.CurrentPlayers = 3;
            l.MaxPlayers = 5;
            l.Name = "trolol";
            l.Region = Runabove.CANADA;
            Teams ts = new Teams();

            // First team, with us on it
            Team t = new Team();
            t.MaxPlayers = 5;
            Players ps = new Players();
            Player p = new Player();
            p.Avatar = "avatar URL here";
            p.PersonaName = "some personan name";
            p.ProfileURL = "http://steamcommunity.com/jexah";
            p.SteamID = "45686503";
            //p.SteamID = "41686503";
            ps.addPlayer(p);
            Player p2 = new Player();
            p2.Avatar = "avatar URL here";
            p2.PersonaName = "some personan name";
            p2.ProfileURL = "http://steamcommunity.com/jexah";
            //p.SteamID = "45686503";
            p2.SteamID = "28090256";
            ps.addPlayer(p2);
            t.Players = ps;
            t.TeamName = "teamMeowingtons";

            // Second team, dummy player
            Team t2 = new Team();
            t2.MaxPlayers = 5;
            t2.TeamName = "teamMeowingtons";
            Players ps2 = new Players();
            Player p3 = new Player();
            p3.Avatar = "avatar URL here";
            p3.PersonaName = "some personan name";
            p3.ProfileURL = "http://steamcommunity.com/jexah";
            p3.SteamID = "28123256";
            ps2.addPlayer(p3);
            t2.Players = ps2;

            // Add second team first
            ts.addTeam(t2);
            ts.addTeam(t);
            l.Teams = ts;

            // Start the websocket client and server
            wsServer.start();
            wsClient.start();
        }

        public static void hookWSocketEvents()
        {
            // Log all incoming streams.
            #region wsServer.addHook(RECEIVE);
            wsServer.addHook(WebSocketServer.RECEIVE, (c) =>
            {
                Helpers.log("Received: " + c.DataFrame.ToString());
            });
            #endregion

            // This is a request for the lobbies from a browser, send lobbies back.
            #region wsServer.addHook("getLobbies");
            wsServer.addHook("getLobbies", (c, x) =>
            {
                // Generate a json string of the lobbies.
                string lobbiesJson = lobbies.toJSON();
                byte[] data = ASCIIEncoding.ASCII.GetBytes(lobbiesJson);
                Console.WriteLine("Original is: " + data.Length.ToString());
                //byte[] compressed = SevenZip.Compression.LZMA.SevenZipHelper.Compress(data);
                //Console.WriteLine("Compressed is: " + compressed.Length.ToString());
                //c.Send(Helpers.packArguments("getLobbies", System.Text.Encoding.Default.GetString(compressed)));

                // Send it
                c.Send(Helpers.packArguments("getLobbies", lobbiesJson));

                //c.Send(compressed);
            });
            #endregion

            // A request from the browser to validate their account
            #region wsServer.addHook("validate");
            wsServer.addHook("validate", (c, x) =>
            {
                // Check args length
                if (x.Length != 3)
                {
                    return;
                }

                // Attempt validation
                validate(x[1], x[2], c.ClientAddress.ToString(), (player) =>
                {
                    // Success
                    c.Send(Helpers.packArguments("validate", "success"));
                }, () =>
                {
                    // Failure
                    c.Send(Helpers.packArguments("validate", "failure"));
                });
            });
            #endregion

            // A request from the browser to create a lobby
            #region wsServer.addHook("createLobby");
            wsServer.addHook("createLobby", (c, x) =>
            {
                // Check args length
                if (x.Length != 2)
                {
                    Helpers.log("createLobby: args too short.");
                    return;
                }

                // If the player is recorded as being validated
                if (!playerCache.ContainsKey(c.ClientAddress.ToString()))
                {
                    // Woah there, player is not validated
                    Helpers.log("2:" + c.ClientAddress.ToString());
                    Helpers.log("createLobby: playerCache key not found.");
                    return;
                }

                // Player is validated, let's use them
                Player player = playerCache[c.ClientAddress.ToString()];

                // Create a lobby using the data the user gave us.
                Lobby lobby = new Lobby(KV.parse(x[1], true));
                //Lobby lobby = new Lobby();

                // And then override most of it :DDD
                Team radiant = new Team();
                radiant.TeamName = "Radiant";
                radiant.MaxPlayers = 5;
                radiant.Players = new Players();
                Team dire = new Team();
                dire.TeamName = "Dire";
                dire.MaxPlayers = 5;
                dire.Players = new Players();
                Team unallocated = new Team();
                unallocated.TeamName = "Unallocated";
                unallocated.MaxPlayers = 10;
                unallocated.Players = new Players();
                lobby.Teams = new Teams();
                lobby.Teams.addTeam(radiant);
                lobby.Teams.addTeam(dire);
                lobby.Teams.addTeam(unallocated);
                Addon lod = new Addon();
                lod.Id = "lod";
                lod.Options = lobby.Addons.getAddon("0").Options;
                lobby.Addons = new Addons();
                lobby.Addons.addAddon(lod);
                lobby.MaxPlayers = 10;
                lobby.CurrentPlayers = 0;
                lobby.Region = Runabove.CANADA;
                // End temp stuff

                // If the lobby name isn't null, and the name isn't taken
                if (lobby.Name != null && lobbies.addLobby(lobby))
                {
                    // Create that sucka
                    Helpers.log("createLobby: send to join lobby.");
                    joinLobby(lobbies.getLobby(lobby.Name), player, c);
                }
                else
                {
                    // Lobby name was taken or ==null
                    Helpers.log("createLobby: failed");
                    c.Send(Helpers.packArguments("createLobby", "failed"));
                }
            });
            #endregion

            // Request from user to join lobby
            #region wsServer.addHook("joinLobby");
            wsServer.addHook("joinLobby", (c, x) =>
            {
                // Check valid request
                if (x.Length != 2)
                {
                    return;
                }
                // Get client IP
                string ip = c.ClientAddress.ToString();

                // If user is validated
                if (x[1] != null && playerCache.ContainsKey(ip))
                {
                    // Get user
                    Player player = playerCache[ip];

                    // Check if lobby exists
                    if (x[1] != null && lobbies.containsKey(x[1]))
                    {
                        if (lobbies.getLobby(x[1]).Active)
                        {
                            // NOPE
                            c.Send(Helpers.packArguments("joinLobby", "failed", "active"));
                            return;
                        }

                        // Lobby exists, attempt to join the lobby.
                        joinLobby(lobbies.getLobby(x[1]), player, c);
                    }
                    else
                    {
                        // Lobby does not exist
                        c.Send(Helpers.packArguments("joinLobby", "failed", "notFound"));
                    }
                }
            });
            #endregion

            // Get lobby info for a specific lobby.
            #region wsServer.addHook("getLobby");
            wsServer.addHook("getLobby", (c, x) =>
            {
                // Check args length
                if (x.Length != 2)
                {
                    return;
                }

                // Get lobby (should check it exists)
                Lobby lobby = new Lobby(lobbies.getLobby(x[1]));

                // Send lobby info
                c.Send(Helpers.packArguments("getLobby", lobby.toString()));
            });
            #endregion

            // Request from a user to swap teams
            #region wsServer.addHook("swapTeam");
            wsServer.addHook("swapTeam", (c, x) =>
            {
                // Get their IP
                string ip = c.ClientAddress.ToString();

                // Check args length and that the player is validated
                if (x.Length != 3 || !ipToSteamID.ContainsKey(ip))
                {
                    return;
                }

                // Assign args
                string slotID = x[1];
                string teamID = x[2];
                string steamID = ipToSteamID[ip];

                // If this player is not in a lobby
                if (!playersInLobbies.ContainsKey(steamID))
                {
                    // gtfo
                    return;
                }

                // Get the lobby that the player is in.
                Lobby lobby = playersInLobbies[steamID];
                if (!lobby.Teams.containsKey(teamID) || lobby.Active)
                {
                    // Trying to join an invalid team, gtfo
                    // Or lobby is already active, too late sucka
                    return;
                }

                // If swapping teams was successful
                if (swapTeam(lobby.Teams.getTeam(teamID), slotID, playerCache[ip]))
                {
                    // For each player in the server
                    foreach (Team team in lobby.Teams.getTeams())
                    {
                        foreach (Player p2 in team.Players.getPlayers())
                        {
                            if (p2 != null && steamIDToIP.ContainsKey(p2.SteamID))
                            {
                                // Let them know the swap occured
                                wsServer.send(Helpers.packArguments("swapTeam", teamID, slotID, steamID), steamIDToIP[p2.SteamID]);

                                // Also let them know if the game is starting...
                                if (lobby.Teams.getTeam("0").Players.getPlayers().Count + lobby.Teams.getTeam("1").Players.getPlayers().Count == 1)
                                {
                                    wsServer.send("lobbyFull", steamIDToIP[p2.SteamID]);
                                }
                            }
                        }
                    }

                    // Begin the timeout for requestGameServer
                    if (lobby.Teams.getTeam("0").Players.getPlayers().Count + lobby.Teams.getTeam("1").Players.getPlayers().Count == 1)
                    {
                        lobbyNameToTimer[lobby.Name] = Timers.setTimeout(5, Timers.SECONDS, () =>
                        {
                            Helpers.log("Requested game server");
                            requestGameServer(lobby);
                        });
                    }
                }
            });
            #endregion

            // Temp request to start game
            #region wsServer.addHook("startGames");
            wsServer.addHook("startGames", (c, x) =>
            {
                foreach (Lobby l in lobbies.getLobbies())
                {
                    requestGameServer(l);
                }
            });
            #endregion

            // Gets the current page that the user should be at
            #region wsServer.addHook("getPage");
            wsServer.addHook("getPage", (c, x) =>
            {
                // Gets user IP
                string ip = c.ClientAddress.ToString();

                // Checks if they're verified
                if (!ipToSteamID.ContainsKey(ip) || !playersInLobbies.ContainsKey(ipToSteamID[ip]))
                {
                    // No? Say hello to the home page.
                    sendHomePage(c);
                    return;
                }

                // Check if they're in a lobby
                if (playersInLobbies.ContainsKey(ipToSteamID[ip]))
                {
                    // If so, get the lobby info and send it to them.
                    Lobby l = playersInLobbies[ipToSteamID[ip]];
                    c.Send(Helpers.packArguments("page", "lobby", l.toJSON()));
                }
            });
            #endregion

            // User says something in chat
            #region wsServer.addHook("chat");
            wsServer.addHook("chat", (c, x) =>
            {
                // Get ip
                string ip = c.ClientAddress.ToString();

                // Not verified?
                if (!playerCache.ContainsKey(ip))
                {
                    // gtfo
                    return;
                }

                // Get player
                Player player = playerCache[ip];

                // Not loitering in a lobby?
                if (!playersInLobbies.ContainsKey(player.SteamID))
                {
                    // gtfo
                    return;
                }

                // Get the lobby that the player is in.
                Lobby lobby = playersInLobbies[player.SteamID];

                // For each player in the server
                foreach (Team t in lobby.Teams.getTeams())
                {
                    foreach (Player p in t.Players.getPlayers())
                    {
                        if (p.SteamID != player.SteamID)
                        {
                            if (steamIDToIP.ContainsKey(p.SteamID))
                            {
                                // Send them the chat update
                                wsServer.send(Helpers.packArguments("chat", player.SteamID, x[1]), steamIDToIP[p.SteamID]);
                            }
                        }
                    }
                }
            });
            #endregion

            // User requests to leave lobby
            #region wsServer.addHook("leaveLobby");
            wsServer.addHook("leaveLobby", (c, x) =>
            {
                // Check args length
                if (x.Length != 1)
                {
                    return;
                }

                // Get player ip
                string ip = c.ClientAddress.ToString();

                // Is user validated?
                if (playerCache.ContainsKey(ip))
                {
                    // Okay let's get their player object
                    Player player = playerCache[c.ClientAddress.ToString()];

                    // Check if they're in a lobby
                    if (playersInLobbies.ContainsKey(player.SteamID))
                    {
                        // Yeah they're in a lobby, get their object and remove them from the lobby
                        Lobby lobby = playersInLobbies[player.SteamID];

                        if (lobby.Active)
                        {
                            // NOPE
                            return;
                        }

                        removeFromLobby(lobby, player, true);

                        // Tell them it was successful
                        c.Send(Helpers.packArguments("leaveLobby"));
                    }
                }
            });
            #endregion



            #region wsClient.addHook(CONNECTED);
            wsClient.addHook(WebSocketClient.CONNECTED, (c) =>
            {
                c.Send("lobbyManager");

            });
            #endregion

            // We got some game server info from the server manager
            #region wsClient.addHook("gameServerInfo");
            wsClient.addHook("gameServerInfo", (c, x) =>
            {
                // Lets generate a game server based on the info they gave us.
                GameServer gameServer = new GameServer(KV.parse(x[2]));

                // Are there teams in the game server?
                if (gameServer.Lobby.Teams != null)
                {
                    // Great, for each of them, set the LoD defaults.
                    foreach (KeyValuePair<string, KV> kvp in gameServer.Lobby.Teams.getKeys())
                    {
                        if (kvp.Value == null)
                        {
                            Team team = new Team();
                            team.TeamName = kvp.Key;
                            team.MaxPlayers = 5;
                            team.Players = new Players();
                            gameServer.Lobby.Teams.addTeam(team);
                        }
                    }

                    // For each player in each team...
                    foreach (Team team in gameServer.Lobby.Teams.getTeams())
                    {
                        foreach (KeyValuePair<string, string> kvp in steamIDToIP)
                        {
                            Helpers.log("KVP: " + kvp.Key + ":" + kvp.Value);
                        }
                        foreach (Player player in team.Players.getPlayers())
                        {
                            if (x[1] == "success")
                            {
                                // Tell them the game server is ready
                                wsServer.send(Helpers.packArguments("gameServerInfo", "success", gameServer.Ip.Split(':')[0] + ":" + gameServer.Port), steamIDToIP[player.SteamID]);
                            }
                            else
                            {
                                // Tell them sumphing fukt up
                                wsServer.send(Helpers.packArguments("gameServerInfo", "failed", steamIDToIP[player.SteamID]));
                            }
                        }
                    }
                }
            });
            #endregion


            #region wsClient.addHook("gameServerExit");
            wsClient.addHook("gameServerExit", (c, x) =>
            {
                Helpers.log("gameServerExit received");
                foreach (Lobby l in lobbies.getLobbies())
                {
                    Helpers.log(l.Name);
                }
                GameServer gameServer = new GameServer(KV.parse(x[1]));
                Helpers.log(gameServer.Lobby.Name);
                deleteLobby(gameServer.Lobby);
                foreach (Team t in gameServer.Lobby.Teams.getTeams())
                {
                    foreach (Player p in t.Players.getPlayers())
                    {
                        if (steamIDToIP.ContainsKey(p.SteamID))
                        {
                            string playerIP = steamIDToIP[p.SteamID];
                            sendHomePage(playerIP);
                        }
                    }
                }
            });
            #endregion

        }

        // This gets called on each tick of the lobby timer interval
        private static void lobbySendTick(Object myObject, EventArgs myEventArgs)
        {
            // Have the lobbies not changed yet?
            if (!lobbiesChanged)
            {
                // gtfo
                return;
            }

            // Lobbies have changed, for each player validated...
            foreach (KeyValuePair<string, Player> kvp in playerCache)
            {
                // Check if they're not in a lobby...
                if (!playersInLobbies.ContainsKey(kvp.Value.SteamID))
                {
                    // And if so, send them the latest lobbies list.
                    string lobbiesJson = lobbies.toJSON();
                    byte[] data = ASCIIEncoding.ASCII.GetBytes(lobbiesJson);
                    wsServer.send(Helpers.packArguments("getLobbies", lobbiesJson), steamIDToIP[kvp.Value.SteamID]);
                }
            }

            // Set lobbies changed to false, so we don't send the same info again.
            lobbiesChanged = false;
        }

        // Send the player their homepage
        private static void sendHomePage(UserContext c)
        {
            string lobbiesJson = lobbies.toJSON();
            byte[] data = ASCIIEncoding.ASCII.GetBytes(lobbiesJson);
            c.Send(Helpers.packArguments("page", "home", lobbiesJson));
        }

        private static void deleteLobby(Lobby lobby)
        {
            deleteLobby(lobby.Name);
        }

        private static void deleteLobby(string lobbyName)
        {
            foreach (Team t in lobbies.getLobby(lobbyName).Teams.getTeams())
            {
                foreach (Player p in t.Players.getPlayers())
                {
                    playersInLobbies.Remove(p.SteamID);
                }
            }
            lobbies.removeLobby(lobbyName);
            lobbiesChanged = true;
        }

        // Send the player their homepage
        private static void sendHomePage(string ip)
        {
            string lobbiesJson = lobbies.toJSON();
            byte[] data = ASCIIEncoding.ASCII.GetBytes(lobbiesJson);
            wsServer.send(Helpers.packArguments("page", "home", lobbiesJson), ip);
        }

        private static void refreshTeam(Team team)
        {

        }


        private static void cancelLobbyStart(string lobbyName)
        {
            if (lobbyNameToTimer.ContainsKey(lobbyName))
            {
                lobbyNameToTimer[lobbyName]();
            }
            if (lobbies.containsKey(lobbyName))
            {
                Lobby lobby = lobbies.getLobby(lobbyName);
                foreach (Team t in lobby.Teams.getTeams())
                {
                    foreach (Player p in t.Players.getPlayers())
                    {
                        if (steamIDToIP.ContainsKey(p.SteamID))
                        {
                            wsServer.send("cancelBeginGame", steamIDToIP[p.SteamID]);
                        }
                    }
                }
            }
        }

        private static bool swapTeam(Team newTeam, string newSlot, Player player)
        {
            if (newTeam.Players.getPlayers().Count < newTeam.MaxPlayers)
            {
                Helpers.log("1");
                if (newTeam.Players.containsKey(newSlot))
                {
                    Helpers.log("something");
                    Helpers.log(newTeam.Players.getKV(newSlot).toString());
                }
                Helpers.log("2");
                if (!newTeam.Players.containsKey(newSlot) && newTeam.Players.getKV(newSlot) != null)
                {
                    return false;
                }
                Helpers.log("3");
                if (newTeam.Players.getKV(newSlot) != null)
                {
                    return false;
                }

                Lobby lobby = playersInLobbies[player.SteamID];
                removeFromLobby(lobby, player, false);
                newTeam.Players.addPlayer(player, newSlot);
                cancelLobbyStart(lobby.Name);
                if (lobby.Teams.getTeam("0").Players.getPlayers().Count + lobby.Teams.getTeam("1").Players.getPlayers().Count >= 1)
                {
                    lobbyNameToTimer[lobby.Name] = Timers.setTimeout(5, Timers.SECONDS, () =>
                    {
                        Helpers.log("Requested game server");
                        requestGameServer(lobby);
                    });
                }
                return true;
            }
            Helpers.log("5");
            return false;
        }

        private static void joinLobby(Lobby lobby, Player player, UserContext c)
        {
            bool joined = false;
            if (player.SteamID == null)
            {
                Helpers.log("Join lobby: Steam ID invalid");
                return;
            }
            if (lobby.Teams != null)
            {
                foreach (KeyValuePair<string, KV> kvp in lobby.Teams.getKeys())
                {
                    if (kvp.Value == null)
                    {
                        Team team = new Team();
                        team.TeamName = kvp.Key;
                        team.MaxPlayers = 5;
                        team.Players = new Players();
                        lobby.Teams.addTeam(team);
                    }
                }
                if (playersInLobbies.ContainsKey(player.SteamID))
                {
                    removeFromLobby(playersInLobbies[player.SteamID], player, true);
                }
                Team unallocated = lobby.Teams.getTeam("2");
                if (unallocated.Players == null)
                {
                    unallocated.Players = new Players();
                }
                if (unallocated.Players.getKeys().Count < unallocated.MaxPlayers)
                {
                    unallocated.Players.addPlayer(player);
                    joined = true;
                    lobby.CurrentPlayers++;
                    playersInLobbies.Add(player.SteamID, lobby);
                    lobbiesChanged = true;
                }
            }
            if (!joined)
            {
                Helpers.log("Join lobby failed: Full");
                c.Send(Helpers.packArguments("joinLobby", "failed", "full"));
            }
            else
            {
                Helpers.log("Join lobbysuccess");
                c.Send(Helpers.packArguments("joinLobby", "success", lobby.toJSON()));
                cancelLobbyStart(lobby.Name);
                foreach (Team t in lobby.Teams.getTeams())
                {
                    foreach (Player p in t.Players.getPlayers())
                    {
                        if (player.SteamID != p.SteamID)
                        {
                            wsServer.send(Helpers.packArguments("addPlayerToLobby", player.toJSON(), "2"), steamIDToIP[p.SteamID]);
                            if (lobby.Teams.getTeam("0").Players.getPlayers().Count + lobby.Teams.getTeam("1").Players.getPlayers().Count >= 1)
                            {
                                wsServer.send("lobbyFull", steamIDToIP[p.SteamID]);
                            }
                        }
                    }
                }
                if (lobby.Teams.getTeam("0").Players.getPlayers().Count + lobby.Teams.getTeam("1").Players.getPlayers().Count >= 1)
                {
                    lobbyNameToTimer[lobby.Name] = Timers.setTimeout(5, Timers.SECONDS, () =>
                    {
                        Helpers.log("Requested game server");
                        requestGameServer(lobby);
                    });
                }
            }
        }

        public static void removeFromLobby(Lobby lobby, Player player, bool exit)
        {
            foreach (Team team in lobby.Teams.getTeams())
            {
                if (removeFromTeam(team, player))
                {
                    if (exit)
                    {
                        playersInLobbies.Remove(player.SteamID);
                        lobby.CurrentPlayers--;
                        if (lobby.CurrentPlayers == 0)
                        {
                            deleteLobby(lobby);
                        }
                    }
                    if (lobbyNameToTimer.ContainsKey(lobby.Name))
                    {
                        lobbyNameToTimer[lobby.Name]();
                    }
                    lobbiesChanged = true;
                }
            }
        }

        public static bool removeFromTeam(Team team, Player player)
        {
            Player toRemove = team.Players.getPlayers().Find(item => item.SteamID == player.SteamID);
            if (toRemove != null)
            {
                team.Players.removePlayer(toRemove);
                return true;
            }
            return false;
        }

        private static void validate(string token, string steamid, string ip, Action<Player> successCallback, Action failureCallback)
        {
            if (playerCache.ContainsKey(ip))
            {
                successCallback(playerCache[ip]);
                return;
            }
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("token", token);
            data.Add("steamID", steamid);

            HTTPRequestManager.startRequest("http://127.0.0.1/validate.php", "GET", (r) =>
            {
                if (r != "get the fuck out of here")
                {
                    // Do stuff with r (response) to get it into 4 variables, rest is complete

                    Player player = new Player(KV.parse(r, true));

                    Helpers.log("1: " + ip);
                    playerCache.Add(ip, player);
                    try
                    {
                        if (steamIDToIP.ContainsKey(steamid))
                        {
                            if (ipToSteamID.ContainsKey(steamIDToIP[steamid]))
                            {
                                ipToSteamID.Remove(steamIDToIP[steamid]);
                            }
                            steamIDToIP[steamid] = ip;
                        }
                        else
                        {
                            steamIDToIP.Add(steamid, ip);
                        }
                    }
                    catch { }
                    try
                    {
                        ipToSteamID.Add(ip, steamid);
                    }
                    catch { }


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

        private static void requestGameServer(Lobby lobby)
        {
            lobby.Active = true;
            wsClient.send(Helpers.packArguments("createGameServer", lobby.toString()));
        }

        public static List<int> Compress(string uncompressed)
        {
            // build the dictionary
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            for (int i = 0; i < 256; i++)
                dictionary.Add(((char)i).ToString(), i);

            string w = string.Empty;
            List<int> compressed = new List<int>();

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
            Dictionary<int, string> dictionary = new Dictionary<int, string>();
            for (int i = 0; i < 256; i++)
                dictionary.Add(i, ((char)i).ToString());

            string w = dictionary[compressed[0]];
            compressed.RemoveAt(0);
            StringBuilder decompressed = new StringBuilder(w);

            foreach (int k in compressed)
            {
                string entry = null;
                if (dictionary.ContainsKey(k))
                    entry = dictionary[k];
                else if (k == dictionary.Count)
                    entry = w + w[0];

                decompressed.Append(entry);

                // new sequence; add it to the dictionary
                dictionary.Add(dictionary.Count, w + entry[0]);

                w = entry;
            }

            return decompressed.ToString();
        }

    }
}
