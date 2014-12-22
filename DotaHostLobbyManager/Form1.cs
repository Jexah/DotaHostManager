using Alchemy.Classes;
using DotaHostClientLibrary;
using DotaHostLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace DotaHostLobbyManager
{
    public partial class Form1 : Form
    {

        private static Lobbies lobbies = new Lobbies();

        private static WebSocketServer wsServer = new WebSocketServer(IPAddress.Any, Global.LOBBY_MANAGER_PORT);

        private static WebSocketClient wsClient = new WebSocketClient("ws://127.0.0.1:" + Vultr.SERVER_MANAGER_PORT + "/");

        private static Dictionary<string, Player> playerCache = new Dictionary<string, Player>();

        private static Dictionary<string, string> steamIDToIP = new Dictionary<string, string>();
        private static Dictionary<string, string> ipToSteamID = new Dictionary<string, string>();

        private static Dictionary<string, Lobby> playersInLobbies = new Dictionary<string, Lobby>();

        private static Lobby l = new Lobby();
        private static bool lobbiesChanged = false;

        private static System.Timers.Timer lobbySendInterval = new System.Timers.Timer();


        public Form1()
        {

            File.Delete("log.txt");

            InitializeComponent();

            hookWSocketEvents();

            lobbySendInterval.Interval = 2000;
            lobbySendInterval.Elapsed += lobbySendTick;
            lobbySendInterval.Start();


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
            l.Region = Vultr.AUSTRALIA;
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

            wsServer.start();
            wsClient.start();
        }

        public static void hookWSocketEvents()
        {
            wsServer.addHook(WebSocketServer.RECEIVE, (c) =>
            {
                Helpers.log("Received: " + c.DataFrame.ToString());
            });

            #region wsServer.addHook("getLobbies");
            wsServer.addHook("getLobbies", (c, x) =>
            {
                string lobbiesJson = lobbies.toJSON();
                byte[] data = ASCIIEncoding.ASCII.GetBytes(lobbiesJson);
                Console.WriteLine("Original is: " + data.Length.ToString());
                //byte[] compressed = SevenZip.Compression.LZMA.SevenZipHelper.Compress(data);
                //Console.WriteLine("Compressed is: " + compressed.Length.ToString());
                //c.Send(Helpers.packArguments("getLobbies", System.Text.Encoding.Default.GetString(compressed)));
                c.Send(Helpers.packArguments("getLobbies", lobbiesJson));
                //c.Send(compressed);
            });
            #endregion

            #region wsServer.addHook("validate");
            wsServer.addHook("validate", (c, x) =>
            {
                if (x.Length != 3)
                {
                    return;
                }
                validate(x[1], x[2], c.ClientAddress.ToString(), (player) =>
                {
                    c.Send(Helpers.packArguments("validate", "success"));
                });
            });
            #endregion

            #region wsServer.addHook("createLobby");
            wsServer.addHook("createLobby", (c, x) =>
            {
                if (x.Length != 2)
                {
                    return;
                }
                if (!playerCache.ContainsKey(c.ClientAddress.ToString()))
                {
                    return;
                }
                Player player = playerCache[c.ClientAddress.ToString()];

                Lobby lobby = new Lobby(KV.parse(x[1], true));
                //Lobby lobby = new Lobby();

                // Temp stuff to limit to LoD
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
                // End temp stuff

                if (lobby.Name != null && lobbies.addLobby(lobby))
                {
                    joinLobby(lobbies.getLobby(lobby.Name), player, c);
                }
                else
                {
                    c.Send(Helpers.packArguments("createLobby", "failed"));
                }
            });
            #endregion

            #region wsServer.addHook("joinLobby");
            wsServer.addHook("joinLobby", (c, x) =>
            {
                if (x.Length != 2)
                {
                    return;
                }
                string ip = c.ClientAddress.ToString();
                if (playerCache.ContainsKey(ip))
                {
                    Player player = playerCache[c.ClientAddress.ToString()];
                    if (lobbies.containsKey(x[1]))
                    {
                        joinLobby(lobbies.getLobby(x[1]), player, c);
                    }
                    else
                    {
                        c.Send(Helpers.packArguments("joinLobby", "failed", "notFound"));
                    }
                }
            });
            #endregion

            #region wsServer.addHook("getLobby");
            wsServer.addHook("getLobby", (c, x) =>
            {
                if (x.Length != 2)
                {
                    return;
                }
                Lobby lobby = new Lobby(lobbies.getLobby(x[1]));
                c.Send(Helpers.packArguments("getLobby", lobby.toString()));
            });
            #endregion

            #region wsServer.addHook("swapTeam");
            wsServer.addHook("swapTeam", (c, x) =>
            {
                string ip = c.ClientAddress.ToString();
                Helpers.log("swapTeamHook: 1");
                if (x.Length != 3 || !ipToSteamID.ContainsKey(ip))
                {
                    Helpers.log("swapTeamHook: 2");
                    return;
                }
                string slotID = x[1];
                string teamID = x[2];
                string steamID = ipToSteamID[ip];
                Helpers.log("swapTeamHook: 3");
                if (!playersInLobbies.ContainsKey(steamID))
                {
                    Helpers.log("swapTeamHook: 4");
                    foreach (string s in playersInLobbies.Keys)
                    {
                        Helpers.log(s);
                    }
                    Helpers.log("final: '" + ip + "'");
                    return;
                }
                Helpers.log("swapTeamHook: 5");
                Lobby lobby = playersInLobbies[steamID];
                Helpers.log("swapTeamHook: 6");
                if (swapTeam(lobby.Teams.getTeam(teamID), slotID, playerCache[ip]))
                {
                    Helpers.log("swapTeamHook: 7");
                    foreach (Team team in lobby.Teams.getTeams())
                    {
                        Helpers.log("swapTeamHook: 8");
                        foreach (Player p2 in team.Players.getPlayers())
                        {
                            Helpers.log("swapTeamHook: 9");
                            if (p2 != null && steamIDToIP.ContainsKey(p2.SteamID))
                            {
                                foreach (KeyValuePair<string, string> s in steamIDToIP)
                                {
                                    Helpers.log(s.Key + ":" + s.Value);
                                }
                                Helpers.log(p2.SteamID);
                                Helpers.log(steamIDToIP[p2.SteamID]);
                                Helpers.log("swapTeamHook: 10");
                                wsServer.send(Helpers.packArguments("swapTeam", teamID, slotID, steamID), steamIDToIP[p2.SteamID]);
                            }
                        }
                    }
                }
            });
            #endregion

            #region wsServer.addHook("startGames");
            wsServer.addHook("startGames", (c, x) =>
            {
                foreach (Lobby l in lobbies.getLobbies())
                {
                    requestGameServer(l);
                }
            });
            #endregion

            #region wsServer.addHook("getPage");
            wsServer.addHook("getPage", (c, x) =>
            {
                string ip = c.ClientAddress.ToString();
                if (ipToSteamID.ContainsKey(ip) || !playersInLobbies.ContainsKey(ipToSteamID[ip]))
                {
                    sendHomePage(c);
                    return;
                }
                Lobby l = playersInLobbies[ipToSteamID[ip]];
                c.Send(Helpers.packArguments("page", "lobby", l.toJSON()));
            });
            #endregion

            #region wsServer.addHook("chat");
            wsServer.addHook("chat", (c, x) =>
            {
                string ip = c.ClientAddress.ToString();
                if (!playerCache.ContainsKey(ip))
                {
                    return;
                }
                Player player = playerCache[ip];
                if (!playersInLobbies.ContainsKey(player.SteamID))
                {
                    return;
                }
                Lobby lobby = playersInLobbies[player.SteamID];
                foreach (Team t in lobby.Teams.getTeams())
                {
                    foreach (Player p in t.Players.getPlayers())
                    {
                        if (p.SteamID != player.SteamID)
                        {
                            if (steamIDToIP.ContainsKey(p.SteamID))
                            {
                                wsServer.send(Helpers.packArguments("chat", player.SteamID, x[1]), steamIDToIP[p.SteamID]);
                            }
                        }
                    }
                }
            });
            #endregion


            #region wsClient.addHook(CONNECTED);
            wsClient.addHook(WebSocketClient.CONNECTED, (c) =>
            {
                c.Send("lobbyManager");

                //requestGameServer(l);

            });
            #endregion

            #region wsClient.addHook("gameServerInfo");
            wsClient.addHook("gameServerInfo", (c, x) =>
            {
                GameServer gameServer = new GameServer(KV.parse(x[2]));
                Helpers.log(gameServer.toString());
                if (gameServer.Lobby.Teams != null)
                {
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
                    foreach (Team team in gameServer.Lobby.Teams.getTeams())
                    {
                        foreach (KeyValuePair<string, string> kvp in steamIDToIP)
                        {
                            Helpers.log("KVP: " + kvp.Key + ":" + kvp.Value);
                        }
                        foreach (Player player in team.Players.getPlayers())
                        {
                            Helpers.log(player.SteamID);
                            Helpers.log("'" + player.SteamID + "' == '" + player.SteamID + "'");
                            Helpers.log((player.SteamID == player.SteamID).ToString());
                            if (x[1] == "success")
                            {
                                wsServer.send(Helpers.packArguments("gameServerInfo", "success", gameServer.Ip.Split(':')[0] + ":" + gameServer.Port), steamIDToIP[player.SteamID]);
                            }
                            else
                            {
                                wsServer.send(Helpers.packArguments("gameServerInfo", "failed", steamIDToIP[player.SteamID]));
                            }
                        }
                    }
                }
            });
            #endregion

        }

        private static void lobbySendTick(Object myObject, EventArgs myEventArgs)
        {
            if (!lobbiesChanged)
            {
                return;
            }
            foreach (KeyValuePair<string, Player> kvp in playerCache)
            {
                if (!playersInLobbies.ContainsKey(kvp.Value.SteamID))
                {
                    string lobbiesJson = lobbies.toJSON();
                    byte[] data = ASCIIEncoding.ASCII.GetBytes(lobbiesJson);
                    wsServer.send(Helpers.packArguments("getLobbies", lobbiesJson), steamIDToIP[kvp.Value.SteamID]);
                    Helpers.log(lobbiesJson);
                }
            }
            lobbiesChanged = false;
        }

        private static void sendHomePage(UserContext c)
        {
            string lobbiesJson = lobbies.toJSON();
            byte[] data = ASCIIEncoding.ASCII.GetBytes(lobbiesJson);
            c.Send(Helpers.packArguments("page", "home", lobbiesJson));
        }

        private static void refreshTeam(Team team)
        {

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
                /*if (!newTeam.Players.containsKey(newSlot) && newTeam.Players.getKV(newSlot) == null)
                {
                    return false;
                }*/
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

                removeFromLobby(playersInLobbies[player.SteamID], player, false);
                newTeam.Players.addPlayer(player, newSlot);
                return true;
            }
            Helpers.log("5");
            return false;
        }

        private static void joinLobby(Lobby lobby, Player player, UserContext c)
        {
            bool joined = false;
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
                c.Send(Helpers.packArguments("joinLobby", "failed", "full"));
            }
            else
            {
                c.Send(Helpers.packArguments("joinLobby", "success", lobby.toJSON()));
                foreach (Team t in lobby.Teams.getTeams())
                {
                    foreach (Player p in t.Players.getPlayers())
                    {
                        if (player.SteamID != p.SteamID)
                        {
                            wsServer.send(Helpers.packArguments("addPlayerToLobby", player.toJSON(), "2"), steamIDToIP[p.SteamID]);
                        }
                    }
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
                            lobbies.removeLobby(lobby);
                        }
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

        private static void validate(string token, string steamid, string ip, Action<Player> callback)
        {
            if (playerCache.ContainsKey(ip))
            {
                callback(playerCache[ip]);
            }
            else
            {
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("token", token);
                data.Add("steamID", steamid);

                HTTPRequestManager.startRequest("http://127.0.0.1/validate.php", "GET", (r) =>
                {
                    if (r != "get the fuck out of here")
                    {
                        // Do stuff with r (response) to get it into 4 variables, rest is complete

                        Player player = new Player(KV.parse(r, true));
                        player.SteamID = player.SteamID;
                        player.PersonaName = player.PersonaName;
                        player.Avatar = player.Avatar;
                        player.ProfileURL = player.ProfileURL;

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


                        callback(player);

                    }
                }, data);
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        private static void requestGameServer(Lobby lobby)
        {
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
