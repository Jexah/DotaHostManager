using System;

namespace DotaHostClientLibrary
{
    public class Lobby : KV
    {
        public string Name
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

        public Teams Teams
        {
            get
            {
                return new Teams(getKV("1"));
            }
            set
            {
                setKey("1", value);
            }
        }

        public Addons Addons
        {
            get
            {
                return new Addons(getKV("2"));
            }
            set
            {
                setKey("2", value);
            }
        }

        public byte MaxPlayers
        {
            get
            {
                return Convert.ToByte(getValue("3"));
            }
            set
            {
                setValue("3", value.ToString());
            }
        }

        public byte CurrentPlayers
        {
            get
            {
                return Convert.ToByte(getValue("4"));
            }
            set
            {
                setValue("4", value.ToString());
            }
        }

        public Lobby()
        {
            initObject();
        }


        public Lobby(KV source)
        {
            if (source == null)
            {
                this.sort = 1;
                this.keys = null;
                this.values = null;
                return;
            }
            this.sort = source.getSort();
            this.keys = source.getKeys();
            this.values = source.getValues();
        }

    }
}
