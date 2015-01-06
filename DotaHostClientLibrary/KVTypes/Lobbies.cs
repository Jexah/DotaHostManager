
using System.Collections.Generic;
namespace DotaHostClientLibrary
{
    public class Lobbies : KV
    {
        public bool addLobby(Lobby lobby)
        {
            if (!containsKey(lobby.Name))
            {
                setKey(lobby.Name, lobby);
                return true;
            }

            return false;
        }

        public void removeLobby(Lobby lobby)
        {
            removeKey(lobby.Name);
        }

        public void removeLobby(string key)
        {
            removeKey(key);
        }

        public Lobby getLobby(string key)
        {
            return new Lobby(getKV(key));
        }

        public List<Lobby> getLobbies()
        {
            List<Lobby> lobbies = new List<Lobby>();
            foreach (KeyValuePair<string, KV> kvp in getKeys())
            {
                lobbies.Add(new Lobby(kvp.Value));
            }
            return lobbies;
        }


        public Lobbies()
        {
            initObject();
        }


        public Lobbies(KV source)
        {
            inheritSource(source);
        }

    }
}
