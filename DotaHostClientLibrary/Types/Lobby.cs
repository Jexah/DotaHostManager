using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostClientLibrary
{
    public class Lobby : KV
    {
        public string Name
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

        public Teams Teams
        {
            get
            {
                return (Teams)getKV("teams");
            }
            set
            {
                setKey("teams", value);
            }
        }

        public Addons Addons
        {
            get
            {
                return (Addons)getKV("addons");
            }
            set
            {
                setKey("addons", value);
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

        public byte CurrentPlayers
        {
            get
            {
                return Convert.ToByte(getValue("currentPlayers"));
            }
            set
            {
                setValue("currentPlayers", value.ToString());
            }
        }
    }
}
