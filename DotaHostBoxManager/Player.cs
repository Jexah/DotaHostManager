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
        private string IP;
        private string alias;

        public Player(string steamID, string IP, string alias)
        {
            this.steamID = steamID;
            this.IP = IP;
            this.alias = alias;
        }
    }
}
