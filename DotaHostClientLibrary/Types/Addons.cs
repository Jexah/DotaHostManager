
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

        public void removeAddon(string key)
        {
            removeKey(key);
        }

        public Addon getAddon(string key)
        {
            return (Addon)getKV(key);
        }
        public Addons()
        {
            initObject();
        }

    }
}
