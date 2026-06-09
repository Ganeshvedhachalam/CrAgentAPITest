using System;
using System.Collections.Generic;
using System.Text;

namespace Capillary.ShellProxy.Model.ShellTransactionModel.Request
{

    public class MetaData
    {
        public int totalCount { get; set; }
    }

    public class CustomerData
    {
        public double pointsRedeemed { get; set; }
        public string customerDataType { get; set; }
        public string customerDataValue { get; set; }
        public string loyaltyType { get; set; }
    }

    public class PosData
    {
        public string clerkID { get; set; }
        public string terminalID { get; set; }
        public string posTimeStamp { get; set; }
        public string originalSalePosTimeStamp { get; set; }
        public string originalTransactionNumber { get; set; }
        public string transactionNumber { get; set; }
    }

    public class RequestData
    {
        public int batchID { get; set; }
        public string requestType { get; set; }
        public string requestID { get; set; }
        public string workstationID { get; set; }
        public string cartEvaluationID { get; set; }
        public string referenceNumber { get; set; }
    }

    public class LoyaltyOffer
    {
        public string loyaltyOfferID { get; set; }
        public string loyaltyOfferCode { get; set; }
        public string referenceID  { get; set; }
        public string loyaltyOfferDescription { get; set; }
    }

    public class PriceAdjustment
    {
        public string referenceID  { get; set; }
        public string promotionType  { get; set; }
        public int loyaltyOfferRefID { get; set; }
        public string additionalProductCode { get; set; }
        public double amount { get; set; }
        public string categoryCode { get; set; }
        public string loyaltyOfferID { get; set; }
        public int priceAdjustmentID { get; set; } = 0;
        public string priceAdjustmentType { get; set; }
        public string offerType { get; set; }
        public double quantity { get; set; }
        public string reason { get; set; }
        public double vat { get; set; }
        public double unitVAT { get; set; }
        public double unitPrice { get; set; }
        public List<TaxSplit> taxSplit { get; set; } 
    }

    public class SaleItem
    {
        public bool markDownIndicator { get; set; }
        public double amount { get; set; }
        public double originalAmount { get; set; }
        public double netAmount { get; set; }
        public double originalNetAmount { get; set; }
        public int saleChannel { get; set; }
        public string subCategoryCode { get; set; }
        public double unitVat { get; set; }
        public double vat { get; set; }
        public string additionalProductCode { get; set; }
        public string additionalProductInfo { get; set; }
        public string categoryCode { get; set; }
        public int itemID { get; set; }
        public List<LoyaltyOffer> loyaltyOffers { get; set; }
        public List<PriceAdjustment> priceAdjustments { get; set; }
        public double quantity { get; set; }
        public string saleItemType { get; set; }
        public string unitMeasure { get; set; }
        public double unitPrice { get; set; }
        public double vatRate { get; set; }
        public string productCode { get; set; }
        public string legacyProductCode { get; set; }
        public string legacyCategoryCode { get; set; }
    }

    public class SiteData
    {
        public string countryCode { get; set; }
        public string siteID { get; set; }
    }

    public class Tender
    {
        public string currencyCode { get; set; }
        public string methodOfPayment { get; set; }
        public string methodOfPaymentID { get; set; }
        public int tenderID { get; set; }
        public decimal totalAmount { get; set; }
        public string cardPAN { get; set; }
        public string acquirerID { get; set; }
        public double netTenderAmount { get; set; }
        public bool substractDiscountAmount { get; set; }
        public List<VoucherRule> voucherRules { get; set; }
    }

    public class Product
    {
        public string productCode { get; set; }
        public int categoryCode { get; set; }
        public string additionalProductInfo { get; set; }
    }

     public class VoucherRule
     {
        public string additionalVoucherInfo { get; set; }
         public int tenderID { get; set; }
        public string voucherCode { get; set; }
        public string voucherType { get; set; }
        public double voucherValue { get; set; }
        public int voucherQuantity { get; set; }
        public string referenceID { get; set; }
        public string promotionType { get; set; }
        public List<Product> products { get; set; }
     }

    public class TaxSplit    
    {
        public int taxID { get; set; } 
        public string code { get; set; } 
        public double rate { get; set; } 
        public double amount { get; set; } 
        public double additionalAmount { get; set; } 
    }

    public class Object
    {
        public List<CustomerData> customerData { get; set; }
        public PosData posData { get; set; }
        public RequestData requestData { get; set; }
        public List<SaleItem> saleItems { get; set; }
        public SiteData siteData { get; set; }
        public List<Tender> tenders { get; set; }
        public Decimal totalAmount { get; set; }
    }

    public class RetailTransactionRequest
    {
        public MetaData metaData { get; set; }
        public List<Object> objects { get; set; }
        public SiteData siteData { get; set; }
    }


}
