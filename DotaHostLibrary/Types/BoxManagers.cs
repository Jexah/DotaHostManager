using DotaHostClientLibrary;
using System.Collections.Generic;

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


        public List<BoxManager> getTeams()
        {
            List<BoxManager> boxManagers = new List<BoxManager>();
            foreach (KeyValuePair<string, KV> kvp in getKeys())
            {
                boxManagers.Add(new BoxManager(kvp.Value));
            }
            return boxManagers;
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
