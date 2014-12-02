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


        public GameServers()
        {
            initObject();
        }

        public GameServers(KV source)
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
