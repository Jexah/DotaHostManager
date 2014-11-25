﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostControlPanel
{

    
    public class BoxManager
    {
        private const byte ACTIVE = 0;
        private const byte IDLE = 1;
        private const byte MIA = 2;
        private const byte INACTIVE = 3;

        private string boxName;
        private byte cpuPercent;
        private short[] ram;
        private byte status;

        private List<GameServer> GameServers;

        public BoxManager()
        {

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