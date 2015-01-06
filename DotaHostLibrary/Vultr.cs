using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DotaHostLibrary
{
    public static class Vultr
    {
        // Server Manager IP
        //public const string SERVER_MANAGER_IP = "110.175.72.12";
        //public const string SERVER_MANAGER_IP = "127.0.0.1";
        public const string SERVER_MANAGER_IP = "***REMOVED***";

        // Server Manager Port
        public const int SERVER_MANAGER_PORT = 3875;

        // Define region consts based on Vultr API
        public const byte AMERICA = 3;      // Dallas, USA
        public const byte EUROPE = 7;       // Amsterdam, Nederlands
        public const byte AUSTRALIA = 19;   // Sydney, Australia

        // Define string -> byte region map
        public static readonly Dictionary<string, byte> NAME_TO_REGION_ID;

        // Define byte -> string region map
        public static readonly Dictionary<byte, string> REGION_ID_TO_NAME;

        // Vultr API key
        public const string VULTR_API_KEY = "***REMOVED***";

        // Vultr $15 plan
        public const byte AMERICA_PLAN_ID = 3;      // 3TB
        public const byte EUROPE_PLAN_ID = 3;       // 3TB
        public const byte AUSTRALIA_PLAN_ID = 8;    // 0.6TB

        // Set plan IDs into 
        public static readonly Dictionary<byte, byte> PLAN_IDS;

        // Vultr OS IDs
        public const byte CUSTOM_OS = 159;
        public const byte SNAPSHOT_OS = 164;

        // Vultr BoxManager snapshot ID
        public const string SNAPSHOT_ID = "54783ffe9a1f3";


        public const byte BOX_ACTIVE = 0;
        public const byte BOX_IDLE = 1;
        public const byte BOX_MIA = 2;
        public const byte BOX_INACTIVE = 3;
        public const byte BOX_DEACTIVATED = 4;


        // Initialize the static readonlys
        static Vultr()
        {
            PLAN_IDS = new Dictionary<byte, byte>()
            {
                { AMERICA, AMERICA_PLAN_ID },
                { EUROPE, EUROPE_PLAN_ID },
                { AUSTRALIA, AUSTRALIA_PLAN_ID }
            };

            NAME_TO_REGION_ID = new Dictionary<string, byte>()
            {
                { "Australia", AUSTRALIA },
                { "Amsterdam", EUROPE },
                { "Dallas", AMERICA }
            };

            REGION_ID_TO_NAME = new Dictionary<byte, string>()
            {
                { AUSTRALIA, "Australia" },
                { EUROPE, "Amsterdam" },
                { AMERICA, "Dallas" }
            };
        }

        // Vultr create server with snapshot in given region
        public static void createServer(byte region)
        {
            HTTPRequestManager.startRequest("https://api.vultr.com/v1/server/create", "POST", (jsonObj) =>
            {

            }, new Dictionary<string, string>(){
                { "api_key", VULTR_API_KEY },
                { "DCID", region.ToString() },
                { "VPSPLANID", PLAN_IDS[region].ToString() },
                { "OSID", SNAPSHOT_OS.ToString() },
                { "SNAPSHOTID", SNAPSHOT_ID }
            });
        }

        // Destroy the server with the given subid
        public static void destroyServer(int subID)
        {
            HTTPRequestManager.startRequest("https://api.vultr.com/v1/server/destroy", "POST", (jsonObj) =>
            {

            }, new Dictionary<string, string>(){
                { "api_key", VULTR_API_KEY },
                { "SUBID", subID.ToString() }
            });
        }

        // Return an object of the server list
        public static void getServers(Action<dynamic> func)
        {
            HTTPRequestManager.startRequest("https://api.vultr.com/v1/server/list", "GET", (body) =>
                {
                    // Take the raw JSON body and convert it into a dictionary of server properties
                    Dictionary<string, VultrServerProperties> data = JsonConvert.DeserializeObject<Dictionary<string, VultrServerProperties>>(body);
                    func(data);
                }, new Dictionary<string, string>(){
                { "api_key", VULTR_API_KEY }
            });
        }

    }
}
