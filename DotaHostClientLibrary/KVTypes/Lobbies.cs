
using System.Collections.Generic;
using System.Linq;

namespace DotaHostClientLibrary
{
    public class Lobbies : Kv
    {
        public bool AddLobby(Lobby lobby)
        {
            if (ContainsKey(lobby.Name)) return false;

            SetKey(lobby.Name, lobby);
            return true;
        }

        public void RemoveLobby(Lobby lobby)
        {
            RemoveKey(lobby.Name);
        }

        public void RemoveLobby(string key)
        {
            RemoveKey(key);
        }

        public Lobby GetLobby(string key)
        {
            return new Lobby(GetKv(key));
        }

        public List<Lobby> GetLobbies()
        {
            return GetKeys().Select(kvp => new Lobby(kvp.Value)).ToList();
        }


        public Lobbies()
        {
            InitObject();
        }


        public Lobbies(Kv source)
        {
            InheritSource(source);
        }

        public Lobbies(IEnumerable<Lobby> lobbies)
        {
            InitObject();
            foreach (var lobby in lobbies)
            {
                AddLobby(lobby);
            }
        }

    }
}
