using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostServerManager
{
    public class AddonRequirements
    {
        private short _ram;
        public short Ram
        {
            get
            {
                return _ram;
            }
            set
            {
                _ram = value;
            }
        }

        private byte _cpu;
        public byte Cpu
        {
            get
            {
                return _cpu;
            }
            set
            {
                _cpu = value;
            }
        }

        public AddonRequirements() { }
        public AddonRequirements(short ram, byte cpu)
        {
            this._ram = ram;
            this._cpu = cpu;
        }
    }
}
