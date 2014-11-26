
namespace DotaHostLibrary
{
    public class Player
    {
        // SteamID of the player
        private string steamID;

        // The player's ID
        private string playerID;

        // The player's name
        private string alias;

        public Player(string steamID, string playerID, string alias)
        {
            this.steamID = steamID;
            this.playerID = playerID;
            this.alias = alias;
        }
    }
}
