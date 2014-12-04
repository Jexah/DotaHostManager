
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

        public Teams()
        {
            initObject();
        }



        public Teams(KV source)
        {
            if (source == null)
            {
                this.sort = 1;
                this.keys = null;
                this.values = null;
                return;
            }
            this.sort = source.getSort();
            this.keys = source.getKeys();
            this.values = source.getValues();
        }
    }
}
