using DotaHostClientLibrary;
using System;
using System.Collections.Generic;
using System.Net;
using System.Windows.Forms;
using DotaHostLibrary;

namespace DotaHostLobbyManager
{
    public partial class Form1 : Form
    {

        private static List<Lobby> lobbies = new List<Lobby>();

        private static WebSocketServer wsServer = new WebSocketServer(IPAddress.Any, 8080);


        public Form1()
        {
            InitializeComponent();

            hookWSocketEvents();

            lobbies.Add(new Lobby("name", new List<Team>() { new Team("yolo", 5) }, new List<Addon>() { new Addon("yolo", new Dictionary<string, string>()) }));

            wsServer.start();
        }

        public static void hookWSocketEvents()
        {
            wsServer.addHook("getLobbies", (c, x) =>
            {
                // getLobbies;lobby1;lobby2
                // getLobbies;name|players|addons;name|players|addons
                // getLobbies;name|currentPlayers-maxPlayers|addon1id-addon2id-addon3id;name2|currentPlayers2-maxPlayers2|addon1id2-addon2id2
                string send = "getLobbies";
                for (int i = 0; i < lobbies.Count; ++i)
                {
                    send += ";" + lobbies[i].Name + "|" + lobbies[i].CurrentPlayers + "-" + lobbies[i].MaxPlayers + "|";
                    for (int j = 0; j < lobbies[i].Addons.Count; ++j)
                    {
                        send += lobbies[i].Addons[j].Id + "-";
                    }
                    if (lobbies[i].Addons.Count > 0)
                    {
                        send = send.Substring(0, send.Length - 1);
                    }
                }
                c.Send(send);
            });

            wsServer.addHook("createLobby", (c, x) =>
            {
                // need auth here
            });
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }


    }
}
