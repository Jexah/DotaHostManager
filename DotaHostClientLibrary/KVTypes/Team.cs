using System;

namespace DotaHostClientLibrary
{
    public class Team : Kv
    {
        public string TeamName
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

        public byte MaxPlayers
        {
            get
            {
                return Convert.ToByte(GetValue("1"));
            }
            set
            {
                SetValue("1", value.ToString());
            }
        }

        public Players Players
        {
            get
            {
                return new Players(GetKv("2"));
            }
            set
            {
                SetKey("2", value);
            }
        }

        public Team()
        {
            InitObject();
        }



        public Team(Kv source)
        {
            InheritSource(source);
        }
    }
}
