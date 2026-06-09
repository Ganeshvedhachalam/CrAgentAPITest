using System;
using System.Collections.Generic;
using System.Text;
namespace Capillary.ShellProxy.Model
{
public class SiteData
    {
        public string countryCode { get; set; }
        public string siteID { get; set; }
    }

    public class PriceData
    {
        public string productCode { get; set; }
        public DateTime priceChangeTimeStamp { get; set; }
        public Decimal sellPrice { get; set; }
    }

    public class UpdateProduct
    {
        public SiteData siteData { get; set; }
        public List<PriceData> priceData { get; set; }
    }
}