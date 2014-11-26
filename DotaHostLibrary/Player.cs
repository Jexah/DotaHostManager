
namespace DotaHostLibrary
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
