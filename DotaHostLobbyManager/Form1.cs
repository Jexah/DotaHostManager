using Alchemy.Classes;
using DotaHostClientLibrary;
using DotaHostLibrary;
using System;
using System.Collections.Generic;
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

        public Form1()
        {
            InitializeComponent();

            hookWSocketEvents();

            for (int i = 0; i < 100; ++i)
            {
                Lobby l = new Lobby();
                Addons ads = new Addons();
                Addon ad = new Addon();
                ad.Id = "lod";
                ad.Options = new Options();
                ads.addAddon(ad);
                l.Addons = ads;
                l.CurrentPlayers = 0;
                l.MaxPlayers = 10;
                if (i == 0)
                {
                    l.Name = "yolo1234";
                }
                else
                {
                    l.Name = Helpers.randomString(10);
                }
                Teams ts = new Teams();
                Team t = new Team();
                Team t2 = new Team();
                t.MaxPlayers = 5;
                Players ps = new Players();
                for (var j = 0; j < 10; ++j)
                {
                    Player p = new Player();
                    p.Avatar = Helpers.randomString(70);
                    p.PersonaName = Helpers.randomString(30);
                    p.ProfileURL = Helpers.randomString(40);
                    p.SteamID = Helpers.randomString(10);
                    ps.addPlayer(p);
                }
                t.Players = ps;
                t.TeamName = Helpers.randomString(15);
                t2.Players = ps;
                t2.TeamName = Helpers.randomString(15);
                ts.addTeam(t);
                ts.addTeam(t2);
                l.Teams = ts;
                lobbies.addLobby(l);
            }

            wsServer.start();
        }

        public static void hookWSocketEvents()
        {

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

            #region wsServer.addHook("createLobby");
            wsServer.addHook("createLobby", (c, x) =>
            {
                if (x.Length != 4)
                {
                    return;
                }
                validate(x[1], x[2], c.ClientAddress.ToString(), (player) =>
                {
                    // Have to implement KV.parseJson and kv.toJson

                    /*
                    Lobby lobby = new Lobby(KV.parseJson(x[3]));
                    lobbies.addLobby(lobby);
                    joinLobby(lobby, player);
                    */
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


            wsClient.addHook(WebSocketClient.CONNECTED, (c) =>
            {
                c.Send("lobbyManager");
            });

            #region wsClient.addHook("gameServerInfo");
            wsClient.addHook("gameServerInfo", (c, x) =>
            {
                GameServer gameServer = new GameServer(KV.parse(x[3]));
                foreach (Team team in gameServer.Lobby.Teams.getTeams())
                {
                    foreach (Player player in team.Players.getPlayers())
                    {
                        if (x[1] == "success")
                        {
                            wsServer.send("gameServerInfo;success;" + gameServer.Ip + ":" + gameServer.Port, playersIPs[player.SteamID]);
                        }
                        else
                        {
                            wsServer.send("gameServerInfo;failed");
                        }
                    }
                }
            });
            #endregion

        }

        private static void joinLobby(Lobby lobby, Player player, UserContext c)
        {
            bool joined = false;
            foreach (KeyValuePair<string, KV> kvp in lobby.Teams.getKeys())
            {
                Team team = new Team(kvp.Value);
                if (team.Players.getKeys().Count < team.MaxPlayers)
                {
                    team.Players.addPlayer(player);
                    joined = true;
                }
            }
            if (!joined)
            {
                c.Send("joinLobby;failed;full");
            }
            else
            {
                c.Send("joinLobby;success;lobbyData");
                //c.Send("joinLobby;success;" + Lobby.toJson());
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

                        Player player = new Player(KV.parse(r).getKV("player"));
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

        private void requestGameServer(Lobby lobby)
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
