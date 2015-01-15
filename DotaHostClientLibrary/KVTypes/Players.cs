
using System.Collections.Generic;
using System.Linq;

namespace DotaHostClientLibrary
{
    public class Players : Kv
    {

        public void AddPlayer(Player player)
        {
            for (byte i = 0; ; ++i)
            {
                if (ContainsKey(i.ToString())) continue;
                SetKey(i.ToString(), player);
                return;
            }
        }

        public void AddPlayer(Player player, string slot)
        {
            SetKey(slot, player);
        }

        public void RemovePlayer(Player player)
        {
            foreach (var kvp in from kvp in GetKeys() let p = new Player(kvp.Value) where player.SteamId == p.SteamId select kvp)
            {
                RemoveKey(kvp.Key);
                return;
            }
        }

        public void RemovePlayer(string id)
        {
            RemoveKey(id);
        }

        public Player GetPlayer(string id)
        {
            return new Player(GetKv(id));
        }

        public List<Player> GetPlayers()
        {
            return GetKeys().Select(kvp => new Player(kvp.Value)).ToList();
        }

        public Players()
        {
            InitObject();
        }



        public Players(Kv source)
        {
            InheritSource(source);
        }
    }
}
