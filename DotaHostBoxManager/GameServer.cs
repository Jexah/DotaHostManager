using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostBoxManager
{
    public class GameServer
    {
        private string name;
        private List<Addon> addons;
        private List<Player> players;

        public GameServer(string name, List<Addon> addons, List<Player> players)
        {
            this.name = name;
            this.addons = addons;
            this.players = players;
        }

        public string getName()
        {
            return this.name;
        }
        public List<Addon> getAddons()
        {
            return this.addons;
        }
        public List<Player> getPlayers()
        {
            return this.players;
        }
    }
}
