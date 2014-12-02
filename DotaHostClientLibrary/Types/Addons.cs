using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostClientLibrary
{
    public class Addons : KV
    {
        public void addAddon(Addon addon)
        {
            for (byte i = 0; true; ++i)
            {
                if (!containsKey(i.ToString()))
                {
                    setKey(i.ToString(), addon);
                }
            }
        }

        public void removeAddon(Addon addon)
        {
            removeKey(addon);
        }

        public void removeAddon(byte id)
        {
            removeKey(id.ToString());
        }

        public void getAddon(byte id)
        {
            getKV(id.ToString());
        }

    }
}
