
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
            return (Team)getKV(teamName);
        }

        public Teams()
        {
            initObject();
        }

    }
}
