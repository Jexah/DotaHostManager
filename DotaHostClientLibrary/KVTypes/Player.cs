
using System;
namespace DotaHostClientLibrary
{
    public class Player : Kv
    {
        public string SteamId
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

        public string PersonaName
        {
            get
            {
                return GetValue("1");
            }
            set
            {
                SetValue("1", value);
            }
        }

        public string Avatar
        {
            get
            {
                return GetValue("2");
            }
            set
            {
                SetValue("2", value);
            }
        }

        public string ProfileUrl
        {
            get
            {
                return GetValue("3");
            }
            set
            {
                SetValue("3", value);
            }
        }

        public byte Badges
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

        public byte Cosmetics
        {
            get
            {
                return Convert.ToByte(GetValue("5"));
            }
            set
            {
                SetValue("5", value.ToString());
            }
        }


        public Player()
        {
            InitObject();
        }


        public Player(Kv source)
        {
            InheritSource(source);
        }
    }
}
