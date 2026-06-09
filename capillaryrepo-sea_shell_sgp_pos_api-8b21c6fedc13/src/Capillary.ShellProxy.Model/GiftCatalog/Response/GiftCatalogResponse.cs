using System;
using System.Collections.Generic;
using System.Text;

namespace Capillary.ShellProxy.Model.GiftCatalog.Response
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class CustomerData
    {
        public double totalPointBalance { get; set; }
        public string customerDataType { get; set; }
        public string customerDataValue { get; set; }
    }

    public class GiftData
    {
        public string additionalProductCode { get; set; }
        public string additionalProductInfo { get; set; }
        public int cashRedeemed { get; set; }
        public string deliveryMode { get; set; }
        public string expiryDate { get; set; }
        public string loyaltyOfferID { get; set; }
        public string loyaltySchemeCode { get; set; }
        public int pointsRedeemed { get; set; }
        public string unitMeasure { get; set; }
        public int unitPrice { get; set; }
    }

    public class Product
    {
        public string additionalProductCode { get; set; }
        public string additionalProductInfo { get; set; }
        public string categoryCode { get; set; }
        public string productCode { get; set; }
        public string subCategoryCode { get; set; }
    }

    public class VoucherRule
    {
        public string loyaltySchemeCode { get; set; }
        public string loyaltyOfferID { get; set; }
        public int pointsRedeemed { get; set; }
        public int cashRedeemed { get; set; }
        public string additionalVoucherInfo { get; set; }
        public string expiryDate { get; set; }
        public List<Product> products { get; set; }
        public string voucherCode { get; set; }
        public string voucherType { get; set; }
        public int voucherValue { get; set; }
    }

    public class GiftItem
    {
        public string exchangeMode { get; set; }
        public List<GiftData> giftData { get; set; }
        public List<VoucherRule> voucherRules { get; set; }
    }

    public class ResponseData
    {
        public int actionCode { get; set; }
        public string actionCodeDescription { get; set; }
        public string workstationID { get; set; }
        public string deliveryMode { get; set; }
        public string overallResult { get; set; }
        public string requestID { get; set; }
        public string requestType { get; set; }
    }

    public class GiftCatalogResponse
    {
        public List<CustomerData> customerData { get; set; }
        public List<GiftItem> giftItems { get; set; }
        public ResponseData responseData { get; set; }
    }




}
