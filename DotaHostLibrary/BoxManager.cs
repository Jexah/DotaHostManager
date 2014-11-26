using DotaHostLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostLibrary
{

    
    public class BoxManager
    {
        public const byte ACTIVE = 0;
        public const byte IDLE = 1;
        public const byte MIA = 2;
        public const byte INACTIVE = 3;

        private string ip;
        private string boxName;
        private byte cpuPercent;
        private short[] ram;
        private byte status;

        private List<GameServer> GameServers;

        public BoxManager()
        {

        }

        // Get/set ip
        public string getIP()
        {
            return ip;
        }
        public void setIP(string ip)
        {
            this.ip = ip;
        }
        // Get/set box name
        public string getName()
        {
            return this.boxName;
        }
        public void setName(string name)
        {
            this.boxName = name;
        }

        // Get/set cpu status
        public byte getCpuPercent()
        {
            return this.cpuPercent;
        }
        public void setCpuPercent(byte percent)
        {
            this.cpuPercent = percent;
        }

        // Get/set ram status
        public short[] getRam()
        {
            return this.ram;
        }
        public void setRam(short[] ramPair)
        {
            this.ram = ramPair;
        }

        // Get/set box status
        public byte getStatus()
        {
            return this.status;
        }
        public void setStatus(byte status)
        {
            this.status = status;
        }
    }
}
