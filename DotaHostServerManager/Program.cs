using DotaHostLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostServerManager
{
    class Program
    {
        private static List<BoxManager> boxManagers = new List<BoxManager>();

        static void Main(string[] args)
        {

        }

        private static void addBoxManager()
        {
            // TODO: Code to start up new box, box will then contact this server once it's started.
        }

        private static void removeBoxManager(BoxManager boxManager)
        {
            // TODO: Code to destroy box server
            boxManagers.Remove(boxManager);
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
