﻿
namespace DotaHostClientLibrary
{
    public class Player : KV
    {
        public string SteamID
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

        public string PersonaName
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

        public string Avatar
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

        public string ProfileURL
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

        public Player()
        {
            initObject();
        }



        public Player(KV source)
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