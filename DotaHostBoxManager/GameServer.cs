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
        private List<Player> players;

        public GameServer(string name, List<Player> players)
        {
            this.name = name;
            this.players = players;
        }

        public string getName()
        {
            return this.name;
        }
        public List<Player> getPlayers()
        {
            return this.players;
        }
    }
}
