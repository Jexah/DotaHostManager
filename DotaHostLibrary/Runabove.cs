using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace DotaHostLibrary
{
    public static class Runabove
    {

        // Server Manager IP
        //public const string SERVER_MANAGER_IP = "127.0.0.1";
        // Vultr Texas
        //public const string SERVER_MANAGER_IP = "***REMOVED***";
        // Runabove Canada
        public const string SERVER_MANAGER_IP = "192.99.64.198";

        // Server Manager Port
        public const int SERVER_MANAGER_PORT = 3875;

        // Define region consts based on Vultr API
        //public const byte AMERICA = 3;      // Dallas, USA
        //public const byte EUROPE = 7;       // Amsterdam, Nederlands
        //public const byte AUSTRALIA = 19;   // Sydney, Australia
        public const string CANADA = "BHS-1";
        public const string FRANCE = "SBG-1";


        // Define name:string -> id:string region map
        //public static readonly Dictionary<string, byte> NAME_TO_REGION_ID;
        public static readonly Dictionary<string, string> NAME_TO_REGION_ID;

        // Define id:string -> name:string region map
        //public static readonly Dictionary<byte, string> REGION_ID_TO_NAME;
        public static readonly Dictionary<string, string> REGION_ID_TO_NAME;

        // Vultr API key
        //public const string VULTR_API_KEY = "***REMOVED***";

        // Runabove api key:
        public const string RUNABOVE_API_KEY = "***REMOVED***";
        public const string RUNABOVE_CONSUMER_KEY = "***REMOVED***";
        public const string RUNABOVE_APP_KEY = "***REMOVED***";
        public const string RUNABOVE_APP_SECRET = "***REMOVED***";

        // Vultr $15 plan
        //public const byte AMERICA_PLAN_ID = 3;      // 3TB
        //public const byte EUROPE_PLAN_ID = 3;       // 3TB
        //public const byte AUSTRALIA_PLAN_ID = 8;    // 0.6TB

        // Runabove large sandbox
        public const string LARGE_SANDBOX = "ra.intel.sb.l";

        // Runabove large sandbox per region
        public const string LARGE_SANDBOX_CANADA = "***REMOVED***";
        public const string LARGE_SANDBOX_FRANCE = "***REMOVED***";

        // Set plan IDs into 
        //public static readonly Dictionary<byte, byte> PLAN_IDS;
        public static readonly Dictionary<string, string> PLAN_IDS;

        // Vultr OS IDs
        //public const byte CUSTOM_OS = 159;
        //public const byte SNAPSHOT_OS = 164;

        // Vultr BoxManager snapshot ID
        //public const string SNAPSHOT_ID = "54783ffe9a1f3";


        public const byte BOX_ACTIVE = 0;
        public const byte BOX_IDLE = 1;
        public const byte BOX_MIA = 2;
        public const byte BOX_INACTIVE = 3;
        public const byte BOX_DEACTIVATED = 4;


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

            PLAN_IDS = new Dictionary<string, string>()
            {
                { CANADA, LARGE_SANDBOX_CANADA },
                { FRANCE, LARGE_SANDBOX_FRANCE }
            };

            NAME_TO_REGION_ID = new Dictionary<string, string>()
            {
                { "Canada", CANADA },
                { "France", FRANCE }
            };

            REGION_ID_TO_NAME = new Dictionary<string, string>()
            {
                { CANADA, "Canada" },
                { FRANCE, "France" }
            };
        }

        // Vultr create server with snapshot in given region
        public static void createServer(string region)
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
        public static void destroyServer(string instanceID)
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
        public static void getServers(Action<dynamic> func)
        {
            /*HTTPRequestManager.startRequest("https://api.vultr.com/v1/server/list", "POST", (body) =>
                {
                    // Take the raw JSON body and convert it into a dictionary of server properties
                    Dictionary<string, VultrServerProperties> data = JsonConvert.DeserializeObject<Dictionary<string, VultrServerProperties>>(body);
                    func(data);
                }, new Dictionary<string, string>(){
                    { "api_key", VULTR_API_KEY }
                },
            );*/

            /*

                        HttpWebRequest yolo = (HttpWebRequest)WebRequest.Create("https://api.runabove.com/1.0/auth/time");
                        yolo.ContentType = "application/json";
                        yolo.Method = "GET";

                        Stream Answer = yolo.GetResponse().GetResponseStream();
                        StreamReader _Answer = new StreamReader(Answer);
                        string secondsSinceEpochRunabove = _Answer.ReadToEnd();
                        Console.WriteLine(secondsSinceEpochRunabove);

                        HttpWebRequest yolo2 = (HttpWebRequest)WebRequest.Create("https://api.runabove.com/1.0/instance");
                        yolo2.ContentType = "application/json";
                        yolo2.Method = "GET";
                        yolo2.Headers["X-Ovh-Application"] = RUNABOVE_APP_KEY;
                        yolo2.Headers["X-Ovh-Timestamp"] = secondsSinceEpochRunabove;
                        yolo2.Headers["X-Ovh-Signature"] = "$1$" + SHA1HashStringForUTF8String(RUNABOVE_APP_SECRET + "+" + RUNABOVE_CONSUMER_KEY + 
                            "+GET+https://api.runabove.com/1.0/instance/++" + secondsSinceEpochRunabove);
                        yolo2.Headers["X-Ovh-Consumer"] = RUNABOVE_CONSUMER_KEY;

                        Console.WriteLine(SHA1HashStringForUTF8String(RUNABOVE_APP_SECRET + "+" + RUNABOVE_CONSUMER_KEY + "+GET+https://api.runabove.com/1.0/instance++" + secondsSinceEpochRunabove));

                        Stream Answer2 = yolo2.GetResponse().GetResponseStream();
                        StreamReader _Answer2 = new StreamReader(Answer2);

                        Console.WriteLine(_Answer2.ReadToEnd());
                        */
            /*HTTPRequestManager.startRequest("https://api.runabove.com/1.0/auth/time", "GET", (body) =>
            {
                string secondsSinceEpochRunabove = body;

                /*HTTPRequestManager.startRequest("https://api.runabove.com/1.0/instance", "GET", (body2) =>
                    {
                        Console.WriteLine(body);
                        // Take the raw JSON body and convert it into a dictionary of server properties
                        //Dictionary<string, RunaboveServerProperties> data = JsonConvert.DeserializeObject<Dictionary<string, RunaboveServerProperties>>(body);
                        //func(data);
                    }, null,
                    new Dictionary<string, string>()
                    {
                        {"Content-Type", "application/json"},
                        {"X-Ovh-Application", RUNABOVE_APP_KEY},
                        {"X-Ovh-Timestamp", secondsSinceEpochRunabove},
                        {"X-Ovh-Signature", "$1$" + sha1(RUNABOVE_APP_SECRET + "+" + RUNABOVE_CONSUMER_KEY + "+GET+https://api.runabove.com/1.0/instance++" + secondsSinceEpochRunabove)},
                        {"X-Ra-Consumer", RUNABOVE_CONSUMER_KEY}
                    }
                );*/

            /*HttpWebRequest yolo = (HttpWebRequest)WebRequest.Create("https://api.runabove.com/1.0/instance");
            yolo.ContentType = "application/json";
            yolo.Method = "GET";
            yolo.Headers["X-Ovh-Application"] = RUNABOVE_APP_KEY;
            yolo.Headers["X-Ovh-Timestampn"] = secondsSinceEpochRunabove;
            yolo.Headers["X-Ovh-Signature"] = "$1$" + sha1(RUNABOVE_APP_SECRET + "+" + RUNABOVE_CONSUMER_KEY + "+GET+https://api.runabove.com/1.0/instance++" + secondsSinceEpochRunabove);
            yolo.Headers["X-Ovh-Signature"] = RUNABOVE_CONSUMER_KEY;

            Console.WriteLine(yolo.GetResponse());

        }, null,
            new Dictionary<string, string>()
            {
            }
        );*/
            /*
            string now = DateTime.Now.ToString();

            HTTPRequestManager.startRequest("https://api.runabove.com/1.0/instance", "GET", (body) =>
                {
                    // Take the raw JSON body and convert it into a dictionary of server properties
                    Dictionary<string, RunaboveServerProperties> data = JsonConvert.DeserializeObject<Dictionary<string, RunaboveServerProperties>>(body);
                    func(data);
                }, null,
                new Dictionary<string, string>()
                {
                    {"Content-Type", "application/json"},
                    {"X-Ra-Application", RUNABOVE_APP_KEY},
                    {"X-Ra-Timestamp", now},
                    {"X-Ra-Signature", "$1$" + sha1(RUNABOVE_APP_KEY + "+GET+https://api.runabove.com/1.0/instance++" + now)},
                    {"X-Ra-Consumer", RUNABOVE_API_KEY}
                }
            );*/
        }

        public static string sha1(string input)
        {
            byte[] temp = Encoding.UTF8.GetBytes(input);
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(temp);
                return Convert.ToBase64String(hash);
            }
        }
        public static string SHA1HashStringForUTF8String(string s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);

            var sha1 = SHA1.Create();
            byte[] hashBytes = sha1.ComputeHash(bytes);

            return HexStringFromBytes(hashBytes);
        }

        /// <summary>
        /// Convert an array of bytes to a string of hex digits
        /// </summary>
        /// <param name="bytes">array of bytes</param>
        /// <returns>String of hex digits</returns>
        public static string HexStringFromBytes(byte[] bytes)
        {
            var sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                var hex = b.ToString("x2");
                sb.Append(hex);
            }
            return sb.ToString();
        }
    }
}
