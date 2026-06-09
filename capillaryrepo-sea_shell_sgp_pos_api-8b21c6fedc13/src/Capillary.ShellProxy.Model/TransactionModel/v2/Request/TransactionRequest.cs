using System;
using System.Collections.Generic;

namespace Capillary.ShellProxy.Model.TransactionModel.v2.Request
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Attributes
    {
        public string CouponTypeCode { get; set; }
        public string bank_name { get; set; }
        public string number {get; set;}
        public string card_type {get; set;}
        public string CardIssuerCode { get; set; }
    }

    public class PaymentMode    {
        public Attributes attributes { get; set; }
        public string mode { get; set; }
        public string value { get; set; }
        public List<string> appliedPaymentVoucherIdentifiers { get; set; }
    }

    public class ExtendedFields    {
        public string ship_first_name { get; set; } 
        public string ship_last_name { get; set; } 
    }

    public class CustomFields    {
        public string trans_cf_a { get; set; } 
    }

    public class ExtendedFields2    {
        public string MetalRate { get; set; } 
        public string GrossWeight { get; set; } 
        public string gender { get; set; } 
        public string marital_status { get; set; } 
    }

    public class CustomFields2    {
        public string cashierid { get; set; } 
        public string city { get; set; } 
    }

    public class LineItemsV2    {
        public string itemCode { get; set; } 
        public double amount { get; set; } 
        public double rate { get; set; } 
        public double discount { get; set; } 
        public double value { get; set; } 
        public double qty { get; set; } 
        public string description { get; set; } 
        public string serial { get; set; }      
        public Dictionary<string, string> extendedFields { get; set; } = new Dictionary<string, string>();
        public CustomFields2 customFields { get; set; } 
        public List<string> appliedPromotionIdentifiers { get; set; }
    }

    public class Transaction    {
        public string identifierType { get; set; } 
        public string identifierValue { get; set; } 
        public string source { get; set; } 
        public string accountId { get; set; } 
        public string type { get; set; } 
        public string returnType { get; set; } 
        public string billNumber { get; set; } 
        public double discount { get; set; } 
        public string billAmount { get; set; } 
        public string note { get; set; } 
        public string grossAmount { get; set; } 
        public string deliveryStatus { get; set; } 
        public string purchaseTime { get; set; } 
        public string billingDate { get; set; } 
        public string currencyCode	 { get; set; } 
        public string promotionEvaluationId { get; set; }
        public List<string> appliedPromotionIdentifiers { get; set; }
        public List<PaymentMode> paymentModes { get; set; } 
        public List<LineItemsV2> lineItemsV2 { get; set; } 
        public Dictionary<string, string> customFields { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> extendedFields { get; set; } = new Dictionary<string, string>();

    }

    public class TransactionRequest    {
        public List<Transaction> Transactions { get; set; } 
    }

}
