﻿using System.Collections.Generic;

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
        private byte cpuPercent;
        private short[] ram;
        private int[] network;
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

        // Get/set network status
        public int[] getNetwork()
        {
            return this.network;
        }
        public void setNetwork(int[] network)
        {
            this.network = network;
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
