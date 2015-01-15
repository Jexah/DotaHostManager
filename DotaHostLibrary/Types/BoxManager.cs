
using DotaHostClientLibrary;
using System;

namespace DotaHostLibrary
{
    public class BoxManager : Kv
    {

        public string Ip
        {
            get
            {
                return GetValue("0");
            }
            set
            {
                SetValue("0", value);
            }

        }

        public byte Cpu
        {
            get
            {
                return Convert.ToByte(GetValue("1"));
            }
            set
            {
                SetValue("1", value.ToString());
            }
        }

        public ushort RamAvailable
        {
            get
            {
                return Convert.ToUInt16(GetValue("2"));
            }
            set
            {
                SetValue("2", value.ToString());
            }
        }

        public ushort RamTotal
        {
            get
            {
                return Convert.ToUInt16(GetValue("3"));
            }
            set
            {
                SetValue("3", value.ToString());
            }
        }

        public uint Upload
        {
            get
            {
                return Convert.ToUInt32(GetValue("4"));
            }
            set
            {
                SetValue("4", value.ToString());
            }
        }

        public uint Download
        {
            get
            {
                return Convert.ToUInt32(GetValue("5"));
            }
            set
            {
                SetValue("5", value.ToString());
            }
        }

        public bool ThirdParty
        {
            get
            {
                return Convert.ToBoolean(GetValue("6"));
            }
            set
            {
                SetValue("6", value.ToString());
            }
        }

        public string InstanceId
        {
            get
            {
                return GetValue("7");
            }
            set
            {
                SetValue("7", value);
            }
        }

        public string Region
        {
            get
            {
                return GetValue("8");
            }
            set
            {
                SetValue("8", value);
            }
        }

        public byte Status
        {
            get
            {
                return Convert.ToByte(GetValue("9"));
            }
            set
            {
                SetValue("9", value.ToString());
            }
        }

        public GameServers GameServers
        {
            get
            {
                return new GameServers(GetKv("a"));
            }
            set
            {
                SetKey("a", value);
            }
        }


        public BoxManager()
        {
            InitObject();
            SetValue("6", true.ToString());
        }



        public BoxManager(Kv source)
        {
            InheritSource(source);
        }
    }
}
