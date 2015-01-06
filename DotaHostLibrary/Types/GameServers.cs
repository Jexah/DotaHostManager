using DotaHostClientLibrary;
using System.Collections.Generic;

namespace DotaHostLibrary
{
    public class GameServers : KV
    {
        public void addGameServer(GameServer gameServer)
        {
            for (byte i = 0; true; ++i)
            {
                if (!containsKey(i.ToString()))
                {
                    setKey(i.ToString(), gameServer);
                    return;
                }
            }
        }

        public void removeGameServer(GameServer gameServer)
        {
            removeKey(gameServer);
        }

        public void removeGamreServe(byte id)
        {
            removeKey(id.ToString());
        }

        public GameServer getGameServer(byte id)
        {
            return new GameServer(getKV(id.ToString()));
        }

        public List<GameServer> getGameServers()
        {
            List<GameServer> gameServers = new List<GameServer>();
            foreach (KeyValuePair<string, KV> kvp in getKeys())
            {
                gameServers.Add(new GameServer(kvp.Value));
            }
            return gameServers;
        }


        public GameServers()
        {
            initObject();
        }

        public GameServers(KV source)
        {
            inheritSource(source);
        }

    }
}
