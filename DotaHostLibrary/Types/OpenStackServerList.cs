
using System;
using System.Collections.Generic;
namespace DotaHostLibrary
{
    public class OpenStackServerList
    {
        public int Count
        {
            get
            {
                return Servers.Count;
            }
        }

        public List<OpenStackServer> Servers;

        public string[] GetAllIpsAsArray()
        {
            string[] ips = new string[Servers.Count];
            ForEach((server, i) =>
            {
                ips[i] = server.Ip;
                return false;
            });
            return ips;
        }
        public string GetAllIpsAsString()
        {
            string[] ips = new string[Servers.Count];
            ForEach((server, i) =>
            {
                ips[i] = server.Ip;
                return false;
            });
            return String.Join("\n", ips);
        }

        public void ForEach(Func<OpenStackServer, int, bool> func)
        {
            for (int i = 0; i < Servers.Count; ++i)
            {
                if (func(Servers[i], i))
                {
                    break;
                }
            }
        }

        public OpenStackServerList(dynamic d)
        {
            Servers = new List<OpenStackServer>();
            for (int i = 0; i < d["servers"].Count; ++i)
            {
                Servers.Add(new OpenStackServer(d["servers"][i]));
            }
        }
    }
}
