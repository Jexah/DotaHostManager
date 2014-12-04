
namespace DotaHostClientLibrary
{
    public class Lobbies : KV
    {
        public void addLobby(Lobby lobby)
        {
            if (!containsKey(lobby.Name))
            {
                setKey(lobby.Name, lobby);
                return;
            }
        }

        public void removeLobby(Lobby lobby)
        {
            removeKey(lobby);
        }

        public void removeLobby(string key)
        {
            removeKey(key);
        }

        public Lobby getLobby(string key)
        {
            return new Lobby(getKV(key));
        }


        public Lobbies()
        {
            initObject();
        }


        public Lobbies(KV source)
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
