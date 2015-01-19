using System;
using System.Linq;

namespace DotaHostClientLibrary
{
    public class Lobby : Kv
    {

        public const byte Waiting = 0;
        public const byte Ready = 1;
        public const byte Active = 2;

        public string Name
        {
            get
            {
                return GetValue("0");
            }
            set
            {
                SetValue("0", value);
            }
        }

        public Teams Teams
        {
            get
            {
                return new Teams(GetKv("1"));
            }
            set
            {
                SetKey("1", value);
            }
        }

        public Addons Addons
        {
            get
            {
                return new Addons(GetKv("2"));
            }
            set
            {
                SetKey("2", value);
            }
        }

        public byte MaxPlayers
        {
            get
            {
                return Convert.ToByte(GetValue("3"));
            }
            set
            {
                SetValue("3", value.ToString());
            }
        }

        public byte CurrentPlayers
        {
            get
            {
                return Convert.ToByte(GetValue("4"));
            }
            set
            {
                SetValue("4", value.ToString());
            }
        }

        public string Region
        {
            get
            {
                return GetValue("5");
            }
            set
            {
                SetValue("5", value);
            }
        }

        public byte Status
        {
            get
            {
                return Convert.ToByte(GetValue("6"));
            }
            set
            {
                SetValue("6", value.ToString());
            }
        }

        public Lobby()
        {
            InitObject();
            Status = Waiting;
        }

        public void ForEachPlayer(Action<Player> func)
        {
            foreach (var p in Teams.GetTeams().SelectMany(t => t.Players.GetPlayers()))
            {
                try
                {
                    func(p);
                }
                catch
                {
                    // ignored
                }
            }
        }

        public Lobby(Kv source)
        {
            InheritSource(source);
        }

    }
}
