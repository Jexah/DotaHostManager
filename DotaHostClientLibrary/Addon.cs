using System.Collections.Generic;

namespace DotaHostClientLibrary
{
    public class Addon
    {
        // ID of the addon
        private string id;
        public string Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
            }
        }

        // List of options for this addon
        private Dictionary<string, string> options = new Dictionary<string, string>();
        public Dictionary<string, string> Options
        {
            get
            {
                return options;
            }
            set
            {
                options = value;
            }
        }

        public Addon(string id, Dictionary<string, string> options)
        {
            this.id = id;
            this.options = options;
        }

    }
}
