using System.Collections.Generic;

namespace Capillary.ShellProxy.Model.NewRelicModel
{
    public class NRCustomMetricData
    {
        public string eventType { get; set; }
        public string StatusCode { get; set; }
        public string SiteID { get; set; }
        public string Country { get; set; }
        public string Environment { get; set; }
        public string AppName { get; set; }
    }

}
