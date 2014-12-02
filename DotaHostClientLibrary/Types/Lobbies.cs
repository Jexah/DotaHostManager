
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

        public void removeLobby(byte id)
        {
            removeKey(id.ToString());
        }

        public void getLobby(byte id)
        {
            getKV(id.ToString());
        }


        public Lobbies()
        {
            initObject();
        }
    }
}
