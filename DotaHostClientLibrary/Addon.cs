using System.Collections.Generic;

namespace DotaHostClientLibrary
{
    public class Addon
    {
        // ID of the addon
        public string Id { get; set; }

        // List of options for this addon
        public Dictionary<string, string> Options { get; set; }

        public Addon(string id, Dictionary<string, string> options)
        {
            this.Id = id;
            this.Options = options;
        }

    }
}
