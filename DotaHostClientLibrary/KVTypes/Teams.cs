
using System.Collections.Generic;
using System.Linq;

namespace DotaHostClientLibrary
{
    public class Teams : Kv
    {

        public void AddTeam(Team team)
        {
            for (byte i = 0; ; ++i)
            {
                if (ContainsKey(i.ToString())) continue;
                SetKey(i.ToString(), team);
                return;
            }
        }

        public void RemoveTeam(string teamName)
        {
            RemoveKey(teamName);
        }

        public void RemoveTeam(Team team)
        {
            RemoveKey(team.TeamName);
        }

        public Team GetTeam(string teamName)
        {
            return new Team(GetKv(teamName));
        }

        public List<Team> GetTeams()
        {
            return GetKeys().Select(kvp => new Team(kvp.Value)).ToList();
        }

        public Teams()
        {
            InitObject();
        }



        public Teams(Kv source)
        {
            InheritSource(source);
        }
    }
}
