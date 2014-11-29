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

        private string ip;
        public string Ip
        {
            get
            {
                return ip;
            }
            set
            {
                ip = value;
            }
        }

        private byte cpuPercent;
        public byte CpuPercent
        {
            get
            {
                return cpuPercent;
            }
            set
            {
                cpuPercent = value;
            }
        }

        private short[] ram;
        public short[] Ram
        {
            get
            {
                return ram;
            }
            set
            {
                ram = value;
            }
        }

        private int[] network;
        public int[] Network
        {
            get
            {
                return network;
            }
            set
            {
                network = value;
            }
        }

        private byte status;
        public byte Status
        {
            get
            {
                return status;
            }
            set
            {
                status = value;
            }
        }

        private byte region;
        public byte Region
        {
            get
            {
                return region;
            }
            set
            {
                region = value;
            }
        }

        private int subID;
        public int SubID
        {
            get
            {
                return subID;
            }
            set
            {
                subID = value;
            }
        }

        private bool thirdParty;
        public bool ThirdParty
        {
            get
            {
                return thirdParty;
            }
            set
            {
                thirdParty = value;
            }
        }


        private List<GameServer> gameServers = new List<GameServer>();
        
        public BoxManager()
        {
            thirdParty = true;
        }

    }
}
