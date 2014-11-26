using DotaHostLibrary;
using System;
using System.Collections.Generic;
using System.Net;

namespace DotaHostServerManager
{
    class Program
    {
        private static Dictionary<string, BoxManager> boxManagers = new Dictionary<string, BoxManager>();
        private static WebSocketServer wsServer = new WebSocketServer(IPAddress.Parse(Global.SERVER_MANAGER_IP), Global.SERVER_MANAGER_PORT);

        static void Main(string[] args)
        {
            hookWSocketServerEvents();

            wsServer.start();
        }

        private static void hookWSocketServerEvents()
        {

            wsServer.addHook(WebSocketClient.RECEIVE, (c) =>
            {
                Helpers.log(c.DataFrame.ToString());
            });


            wsServer.addHook("box", (c, x) => { 
                // TODO: Add new box code
                BoxManager boxManager = new BoxManager();
                boxManager.setIP(c.ClientAddress.ToString());
                boxManagers.Add(c.ClientAddress.ToString(), boxManager);
                Helpers.log("IP: " + c.ClientAddress.ToString());
                c.Send("system");
            });

            wsServer.addHook("system", (c, x) =>
            {
                Helpers.log("Update thingo");
                BoxManager boxManager = boxManagers[c.ClientAddress.ToString()];
                boxManager.setStatus(Convert.ToByte(x[1]));
                boxManager.setCpuPercent(Convert.ToByte(x[2]));
                boxManager.setRam(new short[] { Convert.ToInt16(x[3]), Convert.ToInt16(x[4]) });
                boxManager.setNetwork(new int[] { Convert.ToInt32(x[5]), Convert.ToInt32(x[6]), Convert.ToInt32(x[7]) });
                Timer.newTimer(10, Timer.SECONDS, () => { c.Send("system"); });
            });
        }

        private static void addBoxManager()
        {
            // TODO: Code to start up new box, box will then contact this server once it's started.
        }

        private static void removeBoxManager(BoxManager boxManager)
        {
            // TODO: Code to destroy box server
            boxManagers.Remove(boxManager.getIP());
        }

        private static void findServer(byte region, string addonID)
        {
            // TODO: Add server finding algorithm
        }

        private static void restartBox(BoxManager boxManager)
        {
            // TODO: Add box restart code here
        }
    }
}
