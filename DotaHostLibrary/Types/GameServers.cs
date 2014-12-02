using DotaHostClientLibrary;

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
            return (GameServer)getKV(id.ToString());
        }


        public GameServers()
        {
            initObject();
        }
    }
}
