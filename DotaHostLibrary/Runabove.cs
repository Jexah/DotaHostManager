using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DotaHostLibrary
{
    public static class Runabove
    {

        // Server Manager IP
        //public const string SERVER_MANAGER_IP = "127.0.0.1";
        // Vultr Texas
        public const string ServerManagerIp = "***REMOVED***";
        // Runabove Canada
        //public const string SERVER_MANAGER_IP = "192.99.64.198";

        // Server Manager Port
        public const int ServerManagerPort = 3875;

        // Define region consts based on Vultr API
        //public const byte AMERICA = 3;      // Dallas, USA
        //public const byte EUROPE = 7;       // Amsterdam, Nederlands
        //public const byte AUSTRALIA = 19;   // Sydney, Australia
        public const string Canada = "BHS-1";
        public const string France = "SBG-1";


        // Define name:string -> id:string region map
        //public static readonly Dictionary<string, byte> NAME_TO_REGION_ID;
        public static readonly Dictionary<string, string> NameToRegionId;

        // Define id:string -> name:string region map
        //public static readonly Dictionary<byte, string> REGION_ID_TO_NAME;
        public static readonly Dictionary<string, string> RegionIdToName;

        // Vultr API key
        public const string VultrApiKey = "***REMOVED***";

        // Runabove api key:
        //public const string RUNABOVE_API_KEY = "***REMOVED***";
        //public const string RUNABOVE_CONSUMER_KEY = "***REMOVED***";
        //public const string RUNABOVE_APP_KEY = "***REMOVED***";
        //public const string RUNABOVE_APP_SECRET = "***REMOVED***";
        public const string OpenstackAuthToken = "***REMOVED***";

        // Vultr $15 plan
        //public const byte AMERICA_PLAN_ID = 3;      // 3TB
        //public const byte EUROPE_PLAN_ID = 3;       // 3TB
        //public const byte AUSTRALIA_PLAN_ID = 8;    // 0.6TB

        // Runabove large sandbox
        public const string LargeSandbox = "ra.intel.sb.l";

        // Runabove large sandbox per region
        public const string LargeSandboxCanada = "***REMOVED***";
        public const string LargeSandboxFrance = "***REMOVED***";

        // Set plan IDs into 
        //public static readonly Dictionary<byte, byte> PLAN_IDS;
        public static readonly Dictionary<string, string> PlanIds;

        // Vultr OS IDs
        //public const byte CUSTOM_OS = 159;
        //public const byte SNAPSHOT_OS = 164;

        // Vultr BoxManager snapshot ID
        //public const string SNAPSHOT_ID = "54783ffe9a1f3";


        public const byte BoxActive = 0;
        public const byte BoxIdle = 1;
        public const byte BoxMia = 2;
        public const byte BoxInactive = 3;
        public const byte BoxDeactivated = 4;


        // Initialize the static readonlys
        static Runabove()
        {
            /*PLAN_IDS = new Dictionary<byte, byte>()
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
            };*/

            PlanIds = new Dictionary<string, string>()
            {
                { Canada, LargeSandboxCanada },
                { France, LargeSandboxFrance }
            };

            NameToRegionId = new Dictionary<string, string>()
            {
                { "Canada", Canada },
                { "France", France }
            };

            RegionIdToName = new Dictionary<string, string>()
            {
                { Canada, "Canada" },
                { France, "France" }
            };
        }

        // Vultr create server with snapshot in given region
        public static void CreateServer(string region)
        {
            /*HTTPRequestManager.startRequest("https://api.vultr.com/v1/server/create", "POST", (jsonObj) =>
            {

            }, new Dictionary<string, string>(){
                { "api_key", VULTR_API_KEY },
                { "DCID", region.ToString() },
                { "VPSPLANID", PLAN_IDS[region].ToString() },
                { "OSID", SNAPSHOT_OS.ToString() },
                { "SNAPSHOTID", SNAPSHOT_ID }
            });*/
        }

        // Destroy the server with the given subid
        public static void DestroyServer(string instanceId)
        {
            /*
            HTTPRequestManager.startRequest("https://api.vultr.com/v1/server/destroy", "POST", (jsonObj) =>
            {

            }, new Dictionary<string, string>(){
                { "api_key", VULTR_API_KEY },
                { "SUBID", subID.ToString() }
            });
            */
        }

        // Return an object of the server list
        public static void GetServers(Action<OpenStackServerList> func)
        {
            /*HTTPRequestManager.startRequest("https://api.vultr.com/v1/server/list", "POST", (body) =>
                {
                    // Take the raw JSON body and convert it into a dictionary of server properties
                    Dictionary<string, VultrServerProperties> data = JsonConvert.DeserializeObject<Dictionary<string, VultrServerProperties>>(body);
                    func(data);
                }, new Dictionary<string, string>(){
                    { "api_key", VULTR_API_KEY }
                }
            );*/

            HttpRequestManager.StartRequest("https://compute.bhs-1.runabove.io/v2/***REMOVED***/servers/detail", "GET", (body) =>
            {
                // Take the raw JSON body and convert it into a dictionary of server properties
                dynamic data = JsonConvert.DeserializeObject<dynamic>(body);
                var serverList = new OpenStackServerList(data);
                func(serverList);
            }, null,
            new Dictionary<string, string>()
            {
                {"Content-Type", "application/json"},
                {"X-Auth-Token", OpenstackAuthToken}
            });

        }
    }
}
