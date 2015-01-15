
using System.Collections.Generic;
using System.Linq;

namespace DotaHostClientLibrary
{
    public class Addons : Kv
    {
        public void AddAddon(Addon addon)
        {
            for (byte i = 0; ; ++i)
            {
                if (ContainsKey(i.ToString())) continue;
                SetKey(i.ToString(), addon);
                return;
            }
        }

        public void RemoveAddon(Addon addon)
        {
            RemoveKey(addon);
        }

        public void RemoveAddon(string key)
        {
            RemoveKey(key);
        }

        public Addon GetAddon(string key)
        {
            return new Addon(GetKv(key));
        }

        public List<Addon> GetAddons()
        {
            return GetKeys().Select(kvp => new Addon(kvp.Value)).ToList();
        }

        public Addons()
        {
            InitObject();
        }

        public Addons(Kv source)
        {
            InheritSource(source);
        }

    }
}
