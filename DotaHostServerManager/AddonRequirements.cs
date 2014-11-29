using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostServerManager
{
    public class AddonRequirements
    {
        private short ram;
        public short Ram
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

        private byte cpu;
        public byte Cpu
        {
            get
            {
                return cpu;
            }
            set
            {
                cpu = value;
            }
        }

        public AddonRequirements() { }
        public AddonRequirements(short ram, byte cpu)
        {
            this.ram = ram;
            this.cpu = cpu;
        }
    }
}
