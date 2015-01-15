using DotaHostClientLibrary;
using System.Collections.Generic;
using System.Linq;

namespace DotaHostLibrary
{
    public class BoxManagers : Kv
    {
        public void AddBoxManager(BoxManager boxManager)
        {
            SetKey(boxManager.Ip, boxManager);
        }

        public void RemoveBoxManager(BoxManager boxManager)
        {
            RemoveKey(boxManager);
        }

        public void RemoveBoxManager(string key)
        {
            RemoveKey(key);
        }

        public BoxManager GetBoxManager(string key)
        {
            return new BoxManager(GetKv(key));
        }


        public List<BoxManager> GetBoxManagers()
        {
            return GetKeys().Select(kvp => new BoxManager(kvp.Value)).ToList();
        }


        public BoxManagers()
        {
            InitObject();
        }


        public BoxManagers(Kv source)
        {
            InheritSource(source);
        }
    }
}
