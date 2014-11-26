﻿using System.Collections.Generic;

namespace DotaHostLibrary
{
    public class Addon
    {
        // ID of the addon
        private string id;

        // List of options for this addon
        private Dictionary<string, string> options = new Dictionary<string, string>();

        public Addon(string id, Dictionary<string, string> options)
        {
            this.id = id;
            this.options = options;
        }

        public string getID()
        {
            return this.id;
        }
        public void setID(string id)
        {
            this.id = id;
        }

        public Dictionary<string, string> getOptions()
        {
            return this.options;
        }
        public void setOptions(Dictionary<string, string> options)
        {
            this.options = options;
        }
    }
}