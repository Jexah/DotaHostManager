using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostClientLibrary
{
    public class Players : KV
    {

        public void addPlayer(Player player)
        {
            for (byte i = 0; true; ++i)
            {
                if (!containsKey(i.ToString()))
                {
                    setKey(i.ToString(), player);
                }
            }
        }

        public void removePlayer(Player player)
        {
            removeKey(player);
        }

        public void removePlayer(byte id)
        {
            removeKey(id.ToString());
        }

        public Player getPlayer(byte id)
        {
            return (Player)getKV(id.ToString());
        }

    }
}
