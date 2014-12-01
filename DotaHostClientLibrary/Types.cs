using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostClientLibrary
{
    public static class Types
    {
        public static KV newPlayer(string steamid, string personaname, string avatar, string profileurl)
        {
            KV player = new KV();
            
            player.addKey("steamid", new KV(steamid));
            player.addKey("personaname", new KV(personaname));
            player.addKey("avatar", new KV(avatar));
            player.addKey("profileurl", new KV(profileurl));

            return player;
        }

        public static KV newTeam(string teamName, KV players, int maxPlayers)
        {
            KV team = new KV();

            team.addKey("teamName", new KV(teamName));
            team.addKey("players", players);
            team.addKey("maxPlayers", new KV(maxPlayers.ToString()));

            return team;
        }

        public static KV newAddon(string id, KV options)
        {
            // ID of the addon
            KV addon = new KV();

            addon.addKey("id", new KV(id));
            addon.addKey("options", options);

            return addon;
        }

        public static KV newLobby(string name, KV teams, KV addons, int maxPlayers, int currentPlayers)
        {
            KV lobby = new KV();

            lobby.addKey("name", new KV(name));
            lobby.addKey("teams", teams);
            lobby.addKey("addons", addons);
            lobby.addKey("maxPlayers", new KV(maxPlayers.ToString()));
            lobby.addKey("currentPlayers", new KV(currentPlayers.ToString()));

            return lobby;
        }

    }
}
