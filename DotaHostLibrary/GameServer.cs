using DotaHostClientLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostLibrary
{
    public class GameServer
    {
        // Name of game server, perhaps same as lobby name?
        public string Name { get; set; }

        // List of addons to load
        public List<Addon> Addons { get; set; }

        // List of players
        public List<List<Player>> Players { get; set; }

        // List of options
        public Dictionary<string, string> Options { get; set; }

        public GameServer()
        {
            Name = String.Empty;
            Addons = new List<Addon>();
            Players = new List<List<Player>>();
            Options = new Dictionary<string, string>();
        }

    }
}
