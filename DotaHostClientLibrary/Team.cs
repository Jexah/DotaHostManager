using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostClientLibrary
{
    public class Team
    {
        private string teamName;
        public string TeamName { get { return teamName; } }

        private List<Player> players;
        public List<Player> Players { get { return players; } }

        private byte maxPlayers;
        public byte MaxPlayers { get { return maxPlayers; } }

        public Team(string teamName, byte maxPlayers)
        {
            this.teamName = teamName;
            this.maxPlayers = maxPlayers;
            this.players = new List<Player>();
        }
    }
}
