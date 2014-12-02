using DotaHostClientLibrary;
using System;

namespace DotaHostLibrary
{
    public class GameServer : KV
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

        public ushort Port
        {
            get
            {
                return Convert.ToUInt16(getValue("1"));
            }
            set
            {
                setValue("1", value.ToString());
            }
        }

        public Lobby Lobby
        {
            get
            {
                return (Lobby)getKV("2");
            }
            set
            {
                setKey("2", value);
            }
        }


        public GameServer()
        {
            initObject();
        }
    }
}
