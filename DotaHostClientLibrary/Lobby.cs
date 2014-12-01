using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostClientLibrary
{
    public class Lobby
    {
        public string Name { get; set; }

        public List<Team> Teams { get; set; }

        public List<Addon> Addons { get; set; }

        public int MaxPlayers { get; set; }

        public int CurrentPlayers { get; set; }


        public Lobby(string name, List<Team> teams, List<Addon> addons)
        {
            Name = name;
            Teams = teams;
            Addons = addons;
            MaxPlayers = 0;
            for (int j = 0; j < teams.Count; ++j)
            {
                MaxPlayers += teams[j].MaxPlayers;
            }
            CurrentPlayers = 0;
        }

        public bool addPlayer(Player player, int teamID)
        {
            if (CurrentPlayers < MaxPlayers)
            {
                if (Teams[teamID].Players.Count < Teams[teamID].MaxPlayers)
                {
                    Teams[teamID].Players.Add(player);
                    CurrentPlayers++;
                    return true;
                }
            }
            return false;
        }

        public bool addPlayer(Player player)
        {
            if (CurrentPlayers < MaxPlayers)
            {
                Team leastPlayers = null;
                for (int i = 0; i < Teams.Count; ++i)
                {
                    if (leastPlayers == null || Teams[i].Players.Count < leastPlayers.Players.Count)
                    {
                        leastPlayers = Teams[i];
                    }
                }
                if (leastPlayers.MaxPlayers < leastPlayers.Players.Count)
                {
                    leastPlayers.Players.Add(player);
                    CurrentPlayers++;
                    return true;
                }
            }

            return false;
        }

        public void removePlayer(Player player)
        {
            for (int i = 0; i < Teams.Count; ++i)
            {
                if (Teams[i].Players.Contains(player))
                {
                    Teams[i].Players.Remove(player);
                    CurrentPlayers--;
                    return;
                }
            }
        }







        // These should all be made obsolete with the KV library, but removing them now will cause errors. Will fix later

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
                        Player player = new Player(steamID, playerID, alias, "");
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
