using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostClientLibrary
{
    public class Teams : KV
    {

        public void addTeam(Team team)
        {
            for (byte i = 0; true; ++i)
            {
                if(!containsKey(i.ToString()))
                {
                    setKey(i.ToString(), team);
                }
            }
        }

        public void removeTeam(byte id)
        {
            removeKey(id.ToString());
        }

        public void removeTeam(Team team)
        {
            removeKey(team.TeamName);
        }

        public Team getTeam(string teamName)
        {
            return (Team)getKV(teamName);
        }

    }
}
