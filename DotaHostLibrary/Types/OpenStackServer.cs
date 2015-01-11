
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
        public string FlavorID
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
        public string InstanceID
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
                return FlavorID == Runabove.LARGE_SANDBOX_FRANCE ? Runabove.FRANCE : Runabove.CANADA;
            }
        }

        public OpenStackServer(dynamic d)
        {
            Dynamic = d;
        }
    }
}
