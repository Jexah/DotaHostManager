using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostClientLibrary
{
    public class Player
    {
        private string steamid;
        public string Steamid { get { return steamid; } }

        private string personaname;
        public string Personaname { get { return personaname; } }

        private string avatar;
        public string Avatar { get { return avatar; } }

        private string profileurl;
        public string Profileurl { get { return profileurl; } }


        public Player(string steamid, string personaname, string avatar, string profileurl)
        {
            this.steamid = steamid;
            this.personaname = personaname;
            this.avatar = avatar;
            this.profileurl = profileurl;
        }
    }
}
