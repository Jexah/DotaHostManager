using Alchemy.Classes;
using DotaHostClientLibrary;
using DotaHostLibrary;
using System;
using System.Collections.Generic;
using System.Net;
using System.Windows.Forms;

namespace DotaHostLobbyManager
{
    public partial class Form1 : Form
    {

        private static Lobbies lobbies = new Lobbies();

        private static WebSocketServer wsServer = new WebSocketServer(IPAddress.Any, Global.LOBBY_MANAGER_PORT);

        private static Dictionary<KeyValuePair<string, string>, Player> playerCache = new Dictionary<KeyValuePair<string, string>, Player>();


        public Form1()
        {
            InitializeComponent();

            hookWSocketEvents();


            Lobby l = new Lobby();
            Addons ads = new Addons();
            Addon ad = new Addon();
            ad.Id = "lod";
            ad.Options = new Options();
            ads.addAddon(ad);
            l.Addons = ads;
            l.CurrentPlayers = 3;
            l.MaxPlayers = 5;
            l.Name = "trolol";
            Teams ts = new Teams();
            Team t = new Team();
            t.MaxPlayers = 5;
            Players ps = new Players();
            Player p = new Player();
            p.Avatar = "avatar URL here";
            p.PersonaName = "some personan name";
            p.ProfileURL = "http://steamcommunity.com/jexah";
            p.SteamID = "32-bit steam id";
            ps.addPlayer(p);
            t.Players = ps;
            t.TeamName = "teamMeowingtons";
            ts.addTeam(t);
            l.Teams = ts;
            lobbies.addLobby(l);

            wsServer.start();
        }

        public static void hookWSocketEvents()
        {
            wsServer.addHook("getLobbies", (c, x) =>
            {
                c.Send("getLobbies;" + lobbies.toString());
            });

            wsServer.addHook("createLobby", (c, x) =>
            {
                if (x.Length != 4)
                {
                    return;
                }
                validate(x[1], x[2], (player) =>
                {
                    // Have to implement KV.parseJson and kv.toJson
                    /*
                    Lobby lobby = KV.parseJson(x[3]);
                    lobbies.addLobby(lobby);
                    joinLobby(lobby, player);
                    */
                });
            });

            wsServer.addHook("joinLobby", (c, x) =>
            {
                if (x.Length != 4)
                {
                    return;
                }
                validate(x[1], x[2], (player) =>
                {
                    joinLobby(lobbies.getLobby(x[3]), player, c);
                });
            });

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

        private static void validate(string token, string steamid, Action<Player> callback)
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

                HTTPRequestManager.startRequest("http://dotahost.net/validate.php", "GET", (r) =>
                {
                    if (r != "get the fuck out of here")
                    {
                        // Do stuff with r (response) to get it into 4 variables, rest is complete

                        Player player = new Player();
                        // player.SteamID = steamid;
                        player.SteamID = "28090256";
                        // player.PersonaName = personaname;
                        player.PersonaName = "Ash47";
                        // player.Avatar = avatar;
                        player.Avatar = "http://cdn.akamai.steamstatic.com/steamcommunity/public/images/avatars/d2/d24b838a3e82a455bae9ed4f2ec0e4e478082984.jpg";
                        // player.ProfileURL = profile;
                        player.ProfileURL = "http://steamcommunity.com/id/Ash47/";

                        playerCache.Add(new KeyValuePair<string, string>(token, steamid), player);

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

        }
    }
}
