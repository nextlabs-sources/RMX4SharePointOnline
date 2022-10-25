using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RmxForSPOWeb.Models
{
    public class BatchModeFailedModel
    {
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public string Reason { get; set; }
        public BatchModeFailedModel(string fileName,string fileUrl,string reason)
        {
            this.FileName = fileName;
            this.FileUrl = fileUrl;
            this.Reason = reason;
        }
    }
}