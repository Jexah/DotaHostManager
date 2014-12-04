
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

        public Addons()
        {
            initObject();
        }

        public Addons(KV source)
        {
            if (source == null)
            {
                this.sort = 1;
                this.keys = null;
                this.values = null;
                return;
            }
            this.sort = source.getSort();
            this.keys = source.getKeys();
            this.values = source.getValues();
        }

    }
}
