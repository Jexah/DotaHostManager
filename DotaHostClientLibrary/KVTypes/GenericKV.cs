using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostClientLibrary
{
    public class GenericKV : KV
    {
        public GenericKV()
        {
            initObject();
        }

        public void setGenericKey(string key, KV kv)
        {
            setKey(key, kv);
        }

        public void setGenericValue(string key, string value)
        {
            setValue(key, value);
        }
    }
}
