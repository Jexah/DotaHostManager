using DotaHostClientLibrary;

namespace DotaHostLibrary
{
    public class BoxManagers : KV
    {
        public void addBoxManager(BoxManager boxManager)
        {
            for (byte i = 0; true; ++i)
            {
                if (!containsKey(i.ToString()))
                {
                    setKey(i.ToString(), boxManager);
                }
            }
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
            return (BoxManager)getKV(key);
        }


        public BoxManagers()
        {
            initObject();
        }
    }
}
