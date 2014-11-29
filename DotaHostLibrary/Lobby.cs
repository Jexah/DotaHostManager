using DotaHostClientLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostLibrary
{
    public static class Lobby
    {
        public static Dictionary<string, string> getLobbyArgsObj(string[] gameServerArgs)
        {
            Dictionary<string, string> lobbyArgs = new Dictionary<string, string>();
            for (byte i = 0; i < gameServerArgs.Length; ++i)
            {
                string[] keyValue = gameServerArgs[i].Split('=');
                string key = keyValue[0];
                string value;
                if (keyValue.Length == 2)
                {
                    value = keyValue[1];
                }
                else
                {
                    value = "";
                }
                lobbyArgs[key] = value;
            }
            return lobbyArgs;
        }

        public static List<List<Player>> getPlayersObj(Dictionary<string, string> lobbyArgs)
        {
            List<List<Player>> teams = new List<List<Player>>();
            for (byte i = 0; i < 10; ++i)
            {
                teams.Add(new List<Player>());
            }
            for (byte i = 0; i < 10; ++i)
            {
                if (lobbyArgs.ContainsKey("team" + i) && lobbyArgs["team" + i] != "")
                {
                    string[] teamPlayers = lobbyArgs["team" + i].Split('|');
                    for (int j = 0; j < teamPlayers.Length; ++j)
                    {
                        string[] properties = teamPlayers[j].Split('-');
                        Helpers.log(properties[0]);
                        string playerID = properties[0];
                        string alias = properties[1];
                        string steamID = properties[2];
                        Player player = new Player(steamID, playerID, alias);
                        teams[i].Add(player);
                    }
                }
            }
            return teams;
        }

        public static List<Addon> getAddonsObj(Dictionary<string, string> lobbyArgs)
        {
            List<Addon> addons = new List<Addon>();
            for (byte i = 0; i < 10; ++i)
            {
                if (lobbyArgs.ContainsKey("addon" + i))
                {
                    Dictionary<string, string> addonProperties = new Dictionary<string, string>();
                    string[] addonOptions = lobbyArgs["addon" + i + "options"].Split('|');
                    for (int j = 0; j < addonOptions.Length; ++j)
                    {
                        string[] properties = addonOptions[j].Split('-');
                        string key = properties[0];
                        string value = properties[1];
                        addonProperties.Add(key, value);
                    }
                    Addon addon = new Addon(lobbyArgs["addon" + i], addonProperties);
                    addons.Add(addon);
                }
            }
            return addons;
        }


    }
}
