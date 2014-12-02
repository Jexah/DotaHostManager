
namespace DotaHostClientLibrary
{
    public class Players : KV
    {

        public void addPlayer(Player player)
        {
            for (byte i = 0; true; ++i)
            {
                if (!containsKey(i.ToString()))
                {
                    setKey(i.ToString(), player);
                }
            }
        }

        public void removePlayer(Player player)
        {
            removeKey(player);
        }

        public void removePlayer(string id)
        {
            removeKey(id);
        }

        public Player getPlayer(string id)
        {
            return (Player)getKV(id);
        }

        public Players()
        {
            initObject();
        }

    }
}
