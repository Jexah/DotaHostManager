
using System.Collections.Generic;
namespace DotaHostClientLibrary
{
    public class Teams : KV
    {

        public void addTeam(Team team)
        {
            for (byte i = 0; true; ++i)
            {
                if (!containsKey(i.ToString()))
                {
                    setKey(i.ToString(), team);
                    return;
                }
            }
        }

        public void removeTeam(string teamName)
        {
            removeKey(teamName);
        }

        public void removeTeam(Team team)
        {
            removeKey(team.TeamName);
        }

        public Team getTeam(string teamName)
        {
            return new Team(getKV(teamName));
        }

        public List<Team> getTeams()
        {
            List<Team> teams = new List<Team>();
            foreach (KeyValuePair<string, KV> kvp in getKeys())
            {
                teams.Add(new Team(kvp.Value));
            }
            return teams;
        }

        public Teams()
        {
            initObject();
        }



        public Teams(KV source)
        {
            inheritSource(source);
        }
    }
}
