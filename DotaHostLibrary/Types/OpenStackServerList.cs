
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
                return servers.Count;
            }
        }

        public List<OpenStackServer> servers;

        public string[] getAllIpsAsArray()
        {
            string[] ips = new string[servers.Count];
            forEach((server, i) =>
            {
                ips[i] = server.Ip;
                return false;
            });
            return ips;
        }
        public string getAllIpsAsString()
        {
            string[] ips = new string[servers.Count];
            forEach((server, i) =>
            {
                ips[i] = server.Ip;
                return false;
            });
            return String.Join("\n", ips);
        }

        public void forEach(Func<OpenStackServer, int, bool> func)
        {
            for (int i = 0; i < servers.Count; ++i)
            {
                if (func(servers[i], i))
                {
                    break;
                }
            }
        }

        public OpenStackServerList(dynamic d)
        {
            servers = new List<OpenStackServer>();
            for (int i = 0; i < d["servers"].Count; ++i)
            {
                servers.Add(new OpenStackServer(d["servers"][i]));
            }
        }
    }
}
