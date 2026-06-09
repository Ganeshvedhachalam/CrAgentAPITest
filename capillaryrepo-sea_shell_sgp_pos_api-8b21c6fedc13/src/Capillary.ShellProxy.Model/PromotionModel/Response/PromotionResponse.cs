using System;
using System.Collections.Generic;
using System.Text;

namespace Capillary.ShellProxy.Model.PromotionModel.Response
{

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
   // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class AppliedPromotion
    {
        public string discount { get; set; }
        public string discountAppliedOnQuantity { get; set; }
        public string promotionId { get; set; }
        public string messageLabel { get; set; }
        public string name { get; set; }
        public string promotionAppliedOnQuantity { get; set; }
        public string redemptionCount { get; set; }
        public string identifier { get; set; }
        public string type { get; set; }
        public string tenderType{get;set;}
        public string tenderIdentifier{get;set;}
    }

    public class Attributes
    {
        public string additionalProp1 { get; set; }
        public string additionalProp2 { get; set; }
        public string additionalProp3 { get; set; }
    }

    public class CartItem
    {
        public string amount { get; set; }
        public List<AppliedPromotion> appliedPromotions { get; set; }
        public Attributes attributes { get; set; }
        public List<string> brandList { get; set; }
        public List<string> categoryList { get; set; }
        public string discount { get; set; }
        public string qty { get; set; }
        public string referenceId { get; set; }
        public string sku { get; set; }
    }
    public class AppliedPaymentVoucher
    {
        public string promotionId { get; set; }
        public string earnedPromotionId { get; set; }
        public string promoCode { get; set; }
        public string name { get; set; }
        public string messageLabel { get; set; }
        public string promotionMode { get; set; }
        public int redemptionCount { get; set; }
        public object discount { get; set; }
        public string totalVoucherValue { get; set; }
        public string redeemableVoucherValue { get; set; }
        public object discountAppliedOnQuantity { get; set; }
        public object promotionAppliedOnQuantity { get; set; }
        public string identifier { get; set; }
    }
    public class PromoCodeEvaluationLog
    {
        public string promoCode { get; set; }
        public string codeType { get; set; }
        public string message { get; set; }
        public int errorCode { get; set; }
    }

    public class PaymentVoucherEvaluationLog
    {
        public string promoCode { get; set; }
        public string codeType { get; set; }
        public string message { get; set; }
        public int errorCode { get; set; }
    }
    public class CartTender
    {
        public string identifier { get; set; }
        public string amount { get; set; }
        public string adjustedAmount { get; set; }
    }


    public class Data
    {
        public string amount { get; set; }
        public List<AppliedPromotion> appliedPromotions { get; set; }
        public List<CartItem> cartItems { get; set; }
        public bool categoryHierarchySentInPayload { get; set; }
        public int customerId { get; set; }
        public string evaluationId { get; set; }
        public List<AppliedPaymentVoucher> appliedPaymentVouchers { get; set; }
        public List<PromoCodeEvaluationLog> promoCodeEvaluationLogs { get; set; }
        public List<PaymentVoucherEvaluationLog> paymentVoucherEvaluationLogs { get; set; }
        public List<string> promoCodes { get; set; }
        public List<string> paymentVouchers { get; set; }
        public List<CartTender> cartTenders { get; set; }

    }

    public class Error
    {
        public int code { get; set; }
        public string message { get; set; }
    }

    public class PromotionResponse
    {
        public Data data { get; set; }
        public List<Error> errors { get; set; }
        public string timestamp { get; set; }
        public int errorCode { get; set; }
        public string message { get; set; }
        public List<string> errorDetails { get; set; }
    }




}
