using System;
using System.Collections.Generic;
using System.Text;

namespace Capillary.ShellProxy.Model.GiftCatalog.Request
{
    public class CustomerData
    {
        public string customerDataType { get; set; }
        public string customerDataValue { get; set; }
    }

    public class PosData
    {
        public string languageCode { get; set; }
        public DateTime posTimeStamp { get; set; }
        public string transactionNumber { get; set; }
    }

    public class RequestData
    {
        public string deliveryMode { get; set; }
        public string requestType { get; set; }
        public string requestID { get; set; }
        public string workstationID { get; set; }
    }

    public class SiteData
    {
        public string countryCode { get; set; }
        public string siteID { get; set; }
    }

    public class GiftCatalogRequest
    {
        public List<CustomerData> customerData { get; set; }
        public PosData posData { get; set; }
        public RequestData requestData { get; set; }
        public SiteData siteData { get; set; }
    }


}
