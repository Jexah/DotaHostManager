using System;

namespace DotaHostClientLibrary
{
    public class Team : KV
    {
        public string TeamName
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

        public byte MaxPlayers
        {
            get
            {
                return Convert.ToByte(getValue("1"));
            }
            set
            {
                setValue("1", value.ToString());
            }
        }

        public Players Players
        {
            get
            {
                return new Players(getKV("2"));
            }
            set
            {
                setKey("2", value);
            }
        }

        public Team()
        {
            initObject();
        }



        public Team(KV source)
        {
            inheritSource(source);
        }
    }
}
