using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostBoxManager
{
    public class Addon
    {
        private string name;
        private string id;

        public Addon(string name, string id)
        {
            this.name = name;
            this.id = id;
        }

        public string getName()
        {
            return this.name;
        }

        public string getID()
        {
            return this.id;
        }
    }
}
