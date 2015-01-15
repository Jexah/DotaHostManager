using DotaHostClientLibrary;
using System;

namespace DotaHostLibrary
{
    public class GameServer : Kv
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

        public ushort Port
        {
            get
            {
                return Convert.ToUInt16(GetValue("1"));
            }
            set
            {
                SetValue("1", value.ToString());
            }
        }

        public Lobby Lobby
        {
            get
            {
                return new Lobby(GetKv("2"));
            }
            set
            {
                SetKey("2", value);
            }
        }


        public GameServer()
        {
            InitObject();
        }


        public GameServer(Kv source)
        {
            InheritSource(source);
        }
    }
}
