using System.Collections.Generic;

namespace DotaHostLibrary
{

    
    public class BoxManager
    {
        public const byte ACTIVE = 0;
        public const byte IDLE = 1;
        public const byte MIA = 2;
        public const byte INACTIVE = 3;
        public const byte DEACTIVATED = 4;

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
        }

    }
}
