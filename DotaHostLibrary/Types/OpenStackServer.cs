
namespace DotaHostLibrary
{
    public class OpenStackServer
    {
        public dynamic Dynamic { get; set; }
        public string Ip
        {
            get
            {
                return Dynamic["addresses"]["Ext-Net"][0]["addr"];
            }
            set
            {
                Dynamic["addresses"]["Ext-Net"][0]["addr"] = value;
            }
        }
        public string FlavorId
        {
            get
            {
                return Dynamic["flavor"]["id"];
            }
            set
            {
                Dynamic["flavor"]["id"] = value;
            }
        }
        public string InstanceId
        {
            get
            {
                return Dynamic["id"];
            }
            set
            {
                Dynamic["id"] = value;
            }
        }
        public string Region
        {
            get
            {
                return FlavorId == Runabove.LargeSandboxFrance ? Runabove.France : Runabove.Canada;
            }
        }

        public OpenStackServer(dynamic d)
        {
            Dynamic = d;
        }
    }
}
