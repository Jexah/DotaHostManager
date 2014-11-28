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

        private string name = String.Empty;
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        // List of addons to load
        private List<Addon> addons = new List<Addon>();
        public List<Addon> Addons
        {
            get
            {
                return addons;
            }
            set
            {
                addons = value;
            }
        }


        // List of players
        private List<List<Player>> players = new List<List<Player>>();
        public List<List<Player>> Players
        {
            get
            {
                return players;
            }
            set
            {
                players = value;
            }
        }

        // List of options
        private Dictionary<string, string> options = new Dictionary<string, string>();
        public Dictionary<string, string> Options
        {
            get
            {
                return options;
            }
            set
            {
                options = value;
            }
        }

        public GameServer()
        {

        }


    }
}
