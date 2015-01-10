
using DotaHostClientLibrary;
using System;

namespace DotaHostLibrary
{
    public class BoxManager : KV
    {

        public string Ip
        {
            get
            {
                return getValue("0");
            }
            set
            {
                setValue("0", value);
            }

        }

        public byte Cpu
        {
            get
            {
                return Convert.ToByte(getValue("1"));
            }
            set
            {
                setValue("1", value.ToString());
            }
        }

        public ushort RamAvailable
        {
            get
            {
                return Convert.ToUInt16(getValue("2"));
            }
            set
            {
                setValue("2", value.ToString());
            }
        }

        public ushort RamTotal
        {
            get
            {
                return Convert.ToUInt16(getValue("3"));
            }
            set
            {
                setValue("3", value.ToString());
            }
        }

        public uint Upload
        {
            get
            {
                return Convert.ToUInt32(getValue("4"));
            }
            set
            {
                setValue("4", value.ToString());
            }
        }

        public uint Download
        {
            get
            {
                return Convert.ToUInt32(getValue("5"));
            }
            set
            {
                setValue("5", value.ToString());
            }
        }

        public bool ThirdParty
        {
            get
            {
                return Convert.ToBoolean(getValue("6"));
            }
            set
            {
                setValue("6", value.ToString());
            }
        }

        public string InstanceID
        {
            get
            {
                return getValue("7");
            }
            set
            {
                setValue("7", value);
            }
        }

        public string Region
        {
            get
            {
                return getValue("8");
            }
            set
            {
                setValue("8", value);
            }
        }

        public byte Status
        {
            get
            {
                return Convert.ToByte(getValue("9"));
            }
            set
            {
                setValue("9", value.ToString());
            }
        }

        public GameServers GameServers
        {
            get
            {
                return new GameServers(getKV("a"));
            }
            set
            {
                setKey("a", value);
            }
        }


        public BoxManager()
        {
            initObject();
            setValue("6", true.ToString());
        }



        public BoxManager(KV source)
        {
            inheritSource(source);
        }
    }
}
