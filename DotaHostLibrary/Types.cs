using DotaHostClientLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostLibrary
{
    public static class Types
    {

        public static KV newGameServer(KV lobby, KV options)
        {
            KV gameServer = new KV();

            gameServer.addKey("lobby", lobby);
            gameServer.addKey("options", options);

            return gameServer;
        }

        public static KV newBoxManager()
        {
            return new KV();
            /*
            public string Ip { get; set; }

            public byte CpuPercent { get; set; }

            public short[] Ram { get; set; }

            public int[] Network { get; set; }

            public byte Status { get; set; }

            public byte Region { get; set; }

            public int SubID { get; set; }

            public bool ThirdParty { get; set; }

            public List<GameServer> GameServers { get; set; }
        
            public BoxManager()
            {
                ThirdParty = true;
                GameServers = new List<GameServer>();
            }*/
        }
    }
}
