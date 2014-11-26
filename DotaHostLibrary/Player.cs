using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostBoxManager
{
    public class Player
    {
        private string steamID;
        private string playerID;
        private string alias;

        public Player(string steamID, string playerID, string alias)
        {
            this.steamID = steamID;
            this.playerID = playerID;
            this.alias = alias;
        }
    }
}
