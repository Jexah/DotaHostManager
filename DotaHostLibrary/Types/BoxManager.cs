
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

        public string Cpu
        {
            get
            {
                return getValue("1");
            }
            set
            {
                setValue("1", value);
            }
        }

        public string RamAvailable
        {
            get
            {
                return getValue("2");
            }
            set
            {
                setValue("2", value);
            }
        }

        public string RamTotal
        {
            get
            {
                return getValue("3");
            }
            set
            {
                setValue("3", value);
            }
        }

        public string Upload
        {
            get
            {
                return getValue("4");
            }
            set
            {
                setValue("4", value);
            }
        }

        public string Download
        {
            get
            {
                return getValue("5");
            }
            set
            {
                setValue("5", value);
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

        public string subID
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

        public string region
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



        public BoxManager()
        {
            initObject();
        }
    }
}
