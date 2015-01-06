
using System.Collections.Generic;
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
                    return;
                }
            }
        }

        public void removeAddon(Addon addon)
        {
            removeKey(addon);
        }

        public void removeAddon(string key)
        {
            removeKey(key);
        }

        public Addon getAddon(string key)
        {
            return new Addon(getKV(key));
        }

        public List<Addon> getAddons()
        {
            List<Addon> addons = new List<Addon>();
            foreach (KeyValuePair<string, KV> kvp in getKeys())
            {
                addons.Add(new Addon(kvp.Value));
            }
            return addons;
        }

        public Addons()
        {
            initObject();
        }

        public Addons(KV source)
        {
            inheritSource(source);
        }

    }
}
