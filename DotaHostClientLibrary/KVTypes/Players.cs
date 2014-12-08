
using System.Collections.Generic;
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
            foreach (KeyValuePair<string, KV> kvp in getKeys())
            {
                Player p = new Player(kvp.Value);
                if (player.SteamID == p.SteamID)
                {
                    removeKey(kvp.Key);
                    return;
                }
            }
        }

        public void removePlayer(string id)
        {
            removeKey(id);
        }

        public Player getPlayer(string id)
        {
            return new Player(getKV(id));
        }

        public List<Player> getPlayers()
        {
            List<Player> players = new List<Player>();
            foreach (KeyValuePair<string, KV> kvp in getKeys())
            {
                players.Add(new Player(kvp.Value));
            }
            return players;
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
