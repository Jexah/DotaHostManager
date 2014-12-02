using DotaHostClientLibrary;

namespace DotaHostLibrary
{
    public class BoxManagers : KV
    {
        public void addBoxManager(BoxManager boxManager)
        {
            setKey(boxManager.Ip, boxManager);
        }

        public void removeBoxManager(BoxManager boxManager)
        {
            removeKey(boxManager);
        }

        public void removeBoxManager(string key)
        {
            removeKey(key);
        }

        public BoxManager getBoxManager(string key)
        {
            return new BoxManager(getKV(key));
        }


        public BoxManagers()
        {
            initObject();
        }


        public BoxManagers(KV source)
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
