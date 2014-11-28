using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostLibrary
{
    public class VultrServerProperties
    {
        public string SUBID { get; set; }
        public string os { get; set; }
        public string ram { get; set; }
        public string disk { get; set; }
        public string main_ip { get; set; }
        public string vcpu_count { get; set; }
        public string location { get; set; }
        public string DCID { get; set; }
        public string default_password { get; set; }
        public string date_created { get; set; }
        public string pending_charges { get; set; }
        public string status { get; set; }
        public string cost_per_month { get; set; }
        public string current_bandwidth_gb { get; set; }
        public string allowed_bandwidth_gb { get; set; }
        public string netmask_v4 { get; set; }
        public string gateway_v4 { get; set; }
        public string power_status { get; set; }
        public string VPSPLANID { get; set; }
        public string v6_network { get; set; }
        public string v6_main_ip { get; set; }
        public string v6_network_size { get; set; }
        public string label { get; set; }
        public string kvm_url { get; set; }
        public string auto_backups { get; set; }

        public VultrServerProperties() { }
    }
}
