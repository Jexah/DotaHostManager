using DotaHostClientLibrary;
using System;
using System.Net;
using System.Windows.Forms;

namespace DotaHostLobbyManager
{
    public partial class Form1 : Form
    {

        private static Lobbies lobbies = new Lobbies();

        private static WebSocketServer wsServer = new WebSocketServer(IPAddress.Any, Global.LOBBY_MANAGER_PORT);


        public Form1()
        {
            InitializeComponent();

            hookWSocketEvents();

            //lobbies.Add(new Lobby("name", new List<Team>() { new Team("yolo", 5) }, new List<Addon>() { new Addon("yolo", new Dictionary<string, string>()) }));

            wsServer.start();
        }

        public static void hookWSocketEvents()
        {
            wsServer.addHook("getLobbies", (c, x) =>
            {
                // getLobbies;lobby1;lobby2
                // getLobbies;name|players|addons;name|players|addons
                // getLobbies;name|currentPlayers-maxPlayers|addon1id-addon2id-addon3id;name2|currentPlayers2-maxPlayers2|addon1id2-addon2id2
                string send = "getLobbies;";
                lobbies.toString();
                c.Send(send);
            });

            wsServer.addHook("createLobby", (c, x) =>
            {
                // need auth here
            });

            wsServer.addHook("joinLobby", (c, x) =>
            {
                // need auth here
            });

            wsServer.addHook("getLobby", (c, x) =>
            {
                string send = "getLobby;";
                Lobby lobby = (Lobby)lobbies.getKV(x[1]);
                send += lobby.toString();
                c.Send(send);
            });
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
