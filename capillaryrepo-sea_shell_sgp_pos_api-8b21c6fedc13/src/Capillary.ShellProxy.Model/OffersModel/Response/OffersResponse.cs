using System;
using System.Collections.Generic;
using System.Text;

namespace Capillary.ShellProxy.Model.OffersModel.Response
{
    public class CustomerData
    {
        public string customerDataType { get; set; }
        public string customerDataValue { get; set; }
        public string loyaltyType { get; set; }
        public string pointsRedeemed { get; set; }
    }

    public class Receipt
    {
        public List<string> receiptLines { get; set; }
    }
    
    public class Messages
    {
        public string cashierMessage { get; set; }
    }

    public class ResponseData
    {
        public int actionCode { get; set; }
        public string actionCodeDescription { get; set; }
        public string workstationID { get; set; }
        public string overallResult { get; set; }
        public string requestID { get; set; }
        public string requestType { get; set; }
        public string referenceNumber { get; set; }
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
        public string promotionType { get; set; }
        public string loyaltyOfferID { get; set; }
        public int priceAdjustmentID { get; set; } = 0;
        public string priceAdjustmentType { get; set; }
        public double quantity { get; set; }
        public string reason { get; set; }
        public decimal unitPrice { get; set; }
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

    public class SaleItem
    {
        public string additionalProductCode { get; set; }
        public string additionalProductInfo { get; set; }
        public double amount { get; set; }
        public string categoryCode { get; set; }
        public int itemID { get; set; }
        public List<LoyaltyOffer> loyaltyOffers { get; set; }
        public double originalAmount { get; set; }
        public List<PriceAdjustment> priceAdjustments { get; set; }
        public string productCode { get; set; }
        public double quantity { get; set; }
        public string saleItemType { get; set; }
        public string unitMeasure { get; set; }
        public double unitPrice { get; set; }
        public double vatRate { get; set; }
    }
    public class Product
    {
        public string productCode { get; set; }
        public string categoryCode { get; set; }
        public string additionalProductInfo { get; set; }
    }

    public class VoucherCodesResult
    {
        public string voucherCode { get; set; }
        public int actionCode { get; set; }
        public string actionCodeDescription { get; set; }
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
    }
    public class ApplicableVoucher
    {
        public string voucherCode { get; set; }
        public string voucherType { get; set; }
        public string voucherValue { get; set; }
        public string totalVoucherValue { get; set; }
        public string additionalVoucherInfo { get; set; }
        public string expiryDate { get; set; }
        public string referenceID { get; set; }
        public string promotionType { get; set; }
        public List<Product> products { get; set; }
        public string productCode { get; set; }
        public string categoryCode { get; set; }
        public string subCategoryCode { get; set; }
        public string additionalProductCode { get; set; }
        public string additionalProductInfo { get; set; }
    }
    public class PredictedTender
    {
        public string methodOfPayment { get; set; }
        public string acquirer { get; set; }
        public bool substractDiscountAmount { get; set; }
        public double amount { get; set; }
    }

    public class OffersResponse
    {
        public List<CustomerData> customerData { get; set; }
         public List<VoucherCodesResult> voucherCodesResult { get; set; }
        public Receipt receipt { get; set; }
        public ResponseData responseData { get; set; }
        public List<SaleItem> saleItems { get; set; }
        public List<ApplicableVoucher> applicableVouchers { get; set; }
        public List<Tender> tenders { get; set; }
        public Messages Messages { get; set; }
        public PredictedTender predictedTender { get; set; } 
        public double remainder { get; set; }
        public double totalAmount { get; set; }

    }
}
