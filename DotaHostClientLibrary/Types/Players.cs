
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
                    return;
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
            return new Player(getKV(id));
        }

        public Players()
        {
            initObject();
        }



        public Players(KV source)
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
