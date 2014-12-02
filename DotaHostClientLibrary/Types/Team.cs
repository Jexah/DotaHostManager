using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostClientLibrary
{
    public class Team : KV
    {
        public string TeamName
        {
            get
            {
                return getValue("name");
            }
            set
            {
                setValue("name", value);
            }
        }

        public Players Players
        {
            get
            {
                return (Players)getKV("players");
            }
            set
            {
                setKey("players", value);
            }
        }

        public byte MaxPlayers
        {
            get
            {
                return Convert.ToByte(getValue("maxPlayers"));
            }
            set
            {
                setValue("maxPlayers", value.ToString());
            }
        }

    }
}
