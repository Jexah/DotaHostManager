using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostClientLibrary
{
    public class Options : KV
    {
        public void setOption(string key, string value)
        {
            setValue(key, value);
        }

        public Options()
        {
            initObject();
        }
    }
}
