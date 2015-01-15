using DotaHostClientLibrary;
using System.Collections.Generic;
using System.Linq;

namespace DotaHostLibrary
{
    public class GameServers : Kv
    {
        public void AddGameServer(GameServer gameServer)
        {
            for (byte i = 0; true; ++i)
            {
                if (ContainsKey(i.ToString())) continue;
                SetKey(i.ToString(), gameServer);
                return;
            }
        }

        public void RemoveGameServer(GameServer gameServer)
        {
            RemoveKey(gameServer);
        }

        public void RemoveGameServer(byte id)
        {
            RemoveKey(id.ToString());
        }

        public GameServer GetGameServer(byte id)
        {
            return new GameServer(GetKv(id.ToString()));
        }

        public List<GameServer> GetGameServers()
        {
            return GetKeys().Select(kvp => new GameServer(kvp.Value)).ToList();
        }


        public GameServers()
        {
            InitObject();
        }

        public GameServers(Kv source)
        {
            InheritSource(source);
        }

    }
}
