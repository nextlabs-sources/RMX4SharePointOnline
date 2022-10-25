using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RmxForSPOWeb.Models
{
    public class ScheduleModel
    {
        public bool IsSelected { get; set; } = false;
        public string ScheduleType { get; set; } = "";
        public string TimeInterval { get; set; } = "";
        public string SpecificDays { get; set; } = "";
        public string StartTime { get; set; } = "";
        public string DisplayText { get; set; } = "";
    }
}