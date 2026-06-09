using System;
using System.Collections.Generic;
using System.Text;

namespace Capillary.ShellProxy.Model.OffersModel.Request
{

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class CustomerData
    {
        public string customerDataType { get; set; }
        public string customerDataValue { get; set; }
        public string loyaltyType { get; set; }
    }

    public class PosData
    {
        public DateTime posTimeStamp { get; set; }
        public string transactionNumber { get; set; }
        public int redemptionPresent { get; set; }
    }

    public class RequestData
    {
        public string requestID { get; set; }
        public string requestType { get; set; }
        public string referenceNumber { get; set; }
        public string workstationID { get; set; }
        public string cartEvaluationID { get; set; }
        public string extCorrelationID { get; set; }
    }

    public class LoyaltyOffer
    {
        public string loyaltyOfferID { get; set; }
        public string referenceID { get; set; }
        public string promotionType { get; set; }
        public string loyaltyOfferDescription { get; set; }
    }

    public class PriceAdjustment
    {
        public string referenceID { get; set; }
        public string additionalProductCode { get; set; }
        public double amount { get; set; }
        public string categoryCode { get; set; }
        public string loyaltyOfferID { get; set; }
        public int priceAdjustmentID { get; set; } = 0;
        public string priceAdjustmentType { get; set; }
        public double quantity { get; set; }
        public string reason { get; set; }
        public double unitPrice { get; set; }
    }

    public class SaleItem
    {
        public string additionalProductCode { get; set; }
        public string additionalProductInfo { get; set; }
        public double amount { get; set; }
        public string categoryCode { get; set; }
        public int itemID { get; set; }
        public List<LoyaltyOffer> loyaltyOffers { get; set; }
        public List<PriceAdjustment> priceAdjustments { get; set; }
        public double originalAmount { get; set; }
        public double quantity { get; set; }
        public string saleItemType { get; set; }
        public string unitMeasure { get; set; }
        public double unitPrice { get; set; }
        public double vatRate { get; set; }
        public string productCode { get; set; }
    }

    public class SiteData
    {
        public string countryCode { get; set; }
        public string siteID { get; set; }
    }
    public class PredictedTender
    {
        public string methodOfPayment { get; set; }
        public string acquirer { get; set; }
        public string substractDiscountAmount { get; set; }
        public double amount { get; set; }
    }
    public class Product
    {
        public string productCode { get; set; }
        public string categoryCode { get; set; }
        public string additionalProductInfo { get; set; }
    }
    public class VoucherRule
    {
        public string additionalVoucherInfo { get; set; }
        public string voucherCode { get; set; }
        public string voucherType { get; set; }
        public double voucherValue { get; set; }
        public int voucherQuantity { get; set; }
        public string referenceID { get; set; }
        public string promotionType { get; set; }
        public List<Product> products { get; set; }
    }

    public class Tender
    {
        public int tenderID { get; set; }
        public double cashRedeemed { get; set; }
        public string methodOfPayment { get; set; }
        public string acquirerID { get; set; }
        public string methodOfPaymentID { get; set; }
        public double pointsRedeemed { get; set; }
        public double totalAmount { get; set; }
        public double netTenderAmount { get; set; }
        public bool substractDiscountAmount { get; set; }
        public List<VoucherRule> voucherRules { get; set; }
    }

    public class OffersRequest
    {
        public List<CustomerData> customerData { get; set; }
        public PosData posData { get; set; }
        public RequestData requestData { get; set; }
        public List<SaleItem> saleItems { get; set; }
        public SiteData siteData { get; set; }
        public List<Tender> tenders { get; set; }
        public List<PriceAdjustment> priceAdjustments { get; set; }
        public PredictedTender predictedTender { get; set; }
        public double totalAmount { get; set; }
        public double remainder { get; set; }
    }


}
