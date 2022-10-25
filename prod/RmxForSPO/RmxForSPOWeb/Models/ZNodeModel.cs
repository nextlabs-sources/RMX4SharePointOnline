using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RmxForSPOWeb.Models
{
    public class ZNodeModel
    {
        public string id { get; set; } = "";
        public string name { get; set; } = "";
        public string pId { get; set; } = "";
        public bool @checked { get; set; } = false;
        public bool isParent { get; set; } = false;
        public bool isLoaded { get; set; } = false;
    }
    public class ZSiteNodeModel : ZNodeModel
    {
        public List<SitePropertyModel> siteProperties { get; set; } = new List<SitePropertyModel>();
    }
    public class SitePropertyModel
    {
        public string displayName { get; set; } = "";
        public bool @checked { get; set; } = false;
    }
}