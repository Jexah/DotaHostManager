using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostLibrary
{
    public static class Vultr
    {
        // Server Manager IP
        //public const string SERVER_MANAGER_IP = "110.175.72.12";
        //public const string SERVER_MANAGER_IP = "127.0.0.1";
        public const string SERVER_MANAGER_IP = "108.61.169.195";

        // Server Manager Port
        public const int SERVER_MANAGER_PORT = 3875;

        // Define region consts based on Vultr API
        public const byte AMERICA = 3; // Dallas, USA
        public const byte EUROPE = 7; // Amsterdam, Nederlands
        public const byte AUSTRALIA = 19; // Sydney, Australia

        // Define string -> byte region map
        public static readonly Dictionary<string, byte> NAME_TO_REGION_ID;

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
        public const byte SNAPSHOT_ID = 0;

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
        }

        // Vultr API functions
        public static void createServer(byte region)
        {
            HTTPRequestManager.startRequest("https://api.vultr.com/v1/server/create", "POST", (jsonObj) => { }, new Dictionary<string, string>(){
                //{ "api_key", VULTR_API_KEY },
                //{ "DCID", region.ToString() },
                //{ "VPSPLANID", PLAN_IDS[region].ToString() },
                //{ "OSID", SNAPSHOT_OS.ToString() },
                //{ "SNAPSHOTID", SNAPSHOT_ID.ToString() }
            });
        }

        public static void destroyServer(int subID)
        {
            HTTPRequestManager.startRequest("https://api.vultr.com/v1/server/destroy", "POST", (jsonObj) => { }, new Dictionary<string, string>(){
                //{ "api_key", VULTR_API_KEY },
                //{ "SUBID", subID.ToString() }
            });
        }

        public static void getServers(Action<dynamic> func)
        {
            HTTPRequestManager.startRequest("https://api.vultr.com/v1/server/list", "GET", func, new Dictionary<string, string>(){
                { "api_key", VULTR_API_KEY }
            });
        }
    
    }
}
