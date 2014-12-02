using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostClientLibrary
{
    public class Player : KV
    {
        public string SteamID
        {
            get
            {
                return getValue("steamid");
            }
            set
            {
                setValue("steamid", value);
            }
        }

        public string PersonaName
        {
            get
            {
                return getValue("personaname");
            }
            set
            {
                setValue("personaname", value);
            }
        }

        public string Avatar
        {
            get
            {
                return getValue("avatar");
            }
            set
            {
                setValue("avatar", value);
            }
        }

        public string ProfileURL
        {
            get
            {
                return getValue("profileurl");
            }
            set
            {
                setValue("profileurl", value);
            }
        }

    }
}
