
namespace DotaHostClientLibrary
{
    public class Lobbies : KV
    {
        public void addLobby(Lobby lobby)
        {
            for (byte i = 0; true; ++i)
            {
                if (!containsKey(i.ToString()))
                {
                    setKey(i.ToString(), lobby);
                }
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
            return (Lobby)getKV(key);
        }


        public Lobbies()
        {
            initObject();
        }
    }
}
