using System;
using System.Collections.Generic;

namespace Capillary.ShellProxy.Model
{
    public class ProductLine
    {
        public string CrmLocationCode { get; set; }
        public string EcomLocationId { get; set; }
        public string SiteClientKey { get; set; }
        public string CrmProductCode { get; set; }
        public string EcomProductId { get; set; }
        public string CategoryId { get; set; }
        public string Quantity { get; set; }
        public string Amount { get; set; }
        public string CrmItemId { get; set; }
        public double UnitPrice { get; set; }
        public List<string> EcomRuleIds { get; set; } = new List<string>();
        public PromoLevel PromoLevel { get; set; }
        public string ProgramName { get; set; }
        
    }
}
