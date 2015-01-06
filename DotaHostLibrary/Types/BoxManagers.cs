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


        public List<BoxManager> getBoxManagers()
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
            inheritSource(source);
        }
    }
}
