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

        private static Dictionary<KeyValuePair<string, string>, Player> playerCache = new Dictionary<KeyValuePair<string, string>, Player>();

        private static Dictionary<string, string> playersIPs = new Dictionary<string, string>();

        private static Lobby l = new Lobby();


        public Form1()
        {

            File.Delete("log.txt");

            InitializeComponent();

            hookWSocketEvents();

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
                byte[] compressed = SevenZip.Compression.LZMA.SevenZipHelper.Compress(data);
                Console.WriteLine("Compressed is: " + compressed.Length.ToString());
                //c.Send("getLobbies;" + System.Text.Encoding.Default.GetString(compressed));
                c.Send(lobbiesJson);
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
                    c.Send("validate;success");
                });
            });
            #endregion

            #region wsServer.addHook("createLobby");
            wsServer.addHook("createLobby", (c, x) =>
            {
                if (x.Length != 4)
                {
                    return;
                }
                validate(x[1], x[2], c.ClientAddress.ToString(), (player) =>
                {
                    Lobby lobby = new Lobby(KV.parse(x[3], true));

                    if (lobby.Name != null && lobbies.addLobby(lobby))
                    {
                        joinLobby(lobbies.getLobby(lobby.Name), player, c);
                    }
                    else
                    {
                        c.Send("createLobby;failed");
                    }
                });
            });
            #endregion

            #region wsServer.addHook("joinLobby");
            wsServer.addHook("joinLobby", (c, x) =>
            {
                if (x.Length != 4)
                {
                    return;
                }
                validate(x[1], x[2], c.ClientAddress.ToString(), (player) =>
                {
                    joinLobby(lobbies.getLobby(x[3]), player, c);
                });
            });
            #endregion

            #region wsServer.addHook("getLobby");
            wsServer.addHook("getLobby", (c, x) =>
            {
                if (x.Length != 2)
                {
                    return;
                }
                string send = "getLobby;";
                Lobby lobby = new Lobby(lobbies.getLobby(x[1]));
                send += lobby.toString();
                c.Send(send);
            });
            #endregion

            wsServer.addHook("startGames", (c, x) =>
            {
                foreach (Lobby l in lobbies.getLobbies())
                {
                    requestGameServer(l);
                }
            });


            wsClient.addHook(WebSocketClient.CONNECTED, (c) =>
            {
                c.Send("lobbyManager");

                //requestGameServer(l);

            });

            #region wsClient.addHook("gameServerInfo");
            wsClient.addHook("gameServerInfo", (c, x) =>
            {
                Helpers.log(String.Join(";", x));
                GameServer gameServer = new GameServer(KV.parse(x[2]));
                Helpers.log(gameServer.toString());
                foreach (Team team in gameServer.Lobby.Teams.getTeams())
                {
                    Helpers.log("2");
                    foreach (Player player in team.Players.getPlayers())
                    {
                        Helpers.log("3");
                        if (x[1] == "success")
                        {
                            Helpers.log("4");
                            wsServer.send("gameServerInfo;success;" + gameServer.Ip.Split(':')[0] + ":" + gameServer.Port, playersIPs[player.SteamID]);
                        }
                        else
                        {
                            Helpers.log("5");
                            wsServer.send("gameServerInfo;failed");
                        }
                    }
                }
                Helpers.log("6");
            });
            #endregion

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
                foreach (Team team in lobby.Teams.getTeams())
                {
                    if (team.Players == null)
                    {
                        team.Players = new Players();
                    }
                    if (team.Players.getKeys().Count < team.MaxPlayers)
                    {
                        team.Players.addPlayer(player);
                        joined = true;
                        break;
                    }
                }
            }
            if (!joined)
            {
                c.Send("joinLobby;failed;full");
            }
            else
            {
                c.Send("joinLobby;success;" + lobby.toJSON());
            }
        }

        private static void validate(string token, string steamid, string ip, Action<Player> callback)
        {
            KeyValuePair<string, string> kvp = new KeyValuePair<string, string>(token, steamid);
            if (playerCache.ContainsKey(kvp))
            {
                callback(playerCache[kvp]);
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

                        playerCache.Add(new KeyValuePair<string, string>(token, steamid), player);
                        playersIPs.Add(steamid, ip);

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
            wsClient.send("createGameServer;" + lobby.toString());
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
