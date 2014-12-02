using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostClientLibrary
{
    public class Addon : KV
    {
        public string Id 
        { 
            get 
            { 
                return getValue("id"); 
            }
            set 
            {
                setValue("id", value); 
            }
        
        }
        public Options Options
        { 
            get
            { 
                return (Options)getKV("options"); 
            } 
            set 
            { 
                setKey("options", value); 
            } 
        }
    }
}
