using System;

namespace DotaHostClientLibrary
{
    public class Lobby : KV
    {

        public const byte WAITING = 0;
        public const byte READY = 1;
        public const byte ACTIVE = 2;

        public string Name
        {
            get
            {
                return getValue("0");
            }
            set
            {
                setValue("0", value);
            }
        }

        public Teams Teams
        {
            get
            {
                return new Teams(getKV("1"));
            }
            set
            {
                setKey("1", value);
            }
        }

        public Addons Addons
        {
            get
            {
                return new Addons(getKV("2"));
            }
            set
            {
                setKey("2", value);
            }
        }

        public byte MaxPlayers
        {
            get
            {
                return Convert.ToByte(getValue("3"));
            }
            set
            {
                setValue("3", value.ToString());
            }
        }

        public byte CurrentPlayers
        {
            get
            {
                return Convert.ToByte(getValue("4"));
            }
            set
            {
                setValue("4", value.ToString());
            }
        }

        public string Region
        {
            get
            {
                return getValue("5");
            }
            set
            {
                setValue("5", value);
            }
        }

        public byte Status
        {
            get
            {
                return Convert.ToByte(getValue("6"));
            }
            set
            {
                setValue("6", value.ToString());
            }
        }

        public Lobby()
        {
            initObject();
        }

        public void forEachPlayer(Action<Player> func)
        {
            foreach (Team t in this.Teams.getTeams())
            {
                foreach (Player p in t.Players.getPlayers())
                {
                    try
                    {
                        func(p);
                    }
                    catch { }
                }
            }
        }

        public Lobby(KV source)
        {
            inheritSource(source);
        }

    }
}
