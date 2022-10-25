using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RmxForSPOWeb.Models
{
    public class GeneralSettingModel
    {
        public string JavaPcHost { get; set; }
        public string OAUTHHost { get; set; }
        public string ClientSecureID { get; set; }
        public string ClientSecureKey { get; set; }
        public string SecureViewURL { get; set; }
        public string RouterURL { get; set; }
        public string AppId { get; set; }
        public string AppKey { get; set; }
        public string CertificatefileContent { get; set; }
        public string CertificatefileName { get; set; }
        public string CertificatefilePassword { get; set; }
    }
}