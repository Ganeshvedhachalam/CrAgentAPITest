using System;
using System.Collections.Generic;

namespace Capillary.ShellProxy.Model
{
 public class Datum
    {
        public string createdByUser { get; set; }
        public string reason { get; set; }
        public int createdBy { get; set; }
        public List<object> actions { get; set; }
        public string autoUpdateTime { get; set; }
        public string createdOn { get; set; }
        public int entityId { get; set; }
        public bool isActive { get; set; }
        public string label { get; set; }
        public string status { get; set; }
        public string prevLabel { get; set; }
    }

    public class CustomerStatusResponse
    {
        public List<Datum> data { get; set; }
        public List<object> warnings { get; set; }
        public List<object> errors { get; set; }
    }
}