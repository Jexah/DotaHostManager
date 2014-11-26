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
        private List<Addon> addons = new List<Addon>();
        private List<List<Player>> players = new List<List<Player>>();
        private Dictionary<string, string> options = new Dictionary<string, string>();

        public GameServer()
        {

        }

        public string getName()
        {
            return this.name;
        }
        public void setName(string name)
        {
            this.name = name;
        }
        public List<Addon> getAddons()
        {
            return this.addons;
        }
        public void setAddons(List<Addon> addons)
        {
            this.addons = addons;
        }
        public List<List<Player>> getPlayers()
        {
            return this.players;
        }
        public void setPlayers(List<List<Player>> players)
        {
            this.players = players;
        }
        public Dictionary<string, string> getOptions()
        {
            return this.options;
        }
        public void setOptions(Dictionary<string, string> options)
        {
            this.options = options;
        }
    }
}
