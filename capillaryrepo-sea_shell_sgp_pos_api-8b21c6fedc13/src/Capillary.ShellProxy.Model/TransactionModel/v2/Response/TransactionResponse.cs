using System;
using System.Collections.Generic;

namespace Capillary.ShellProxy.Model.TransactionModel.v2.Response
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
   // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class ExtendedFields    {
        public string order_channel { get; set; } 
    }

    public class ExtendedFields2    {
        public string amount_excluding_tax { get; set; } 
        public string discount_type { get; set; } 
        public string price_override_applied { get; set; } 
        public string service_type { get; set; } 
        public string total_unit_cost { get; set; } 
        public string vat_amount { get; set; } 
        public string vat_amount_on_unit_price { get; set; } 
        public string vat_tax_percentage { get; set; } 
        public string items_in_product_set { get; set; } 
    }

    public class LineItemsV2    {
        public double amount { get; set; } 
        public string description { get; set; } 
        public string itemCode { get; set; } 
        public double qty { get; set; } 
        public double rate { get; set; } 
        public double discount { get; set; } 
        public double value { get; set; } 
        public int serial { get; set; } 
        public bool returnable { get; set; } 
        public int returnableDays { get; set; } 
        public List<object> comboDetails { get; set; } 
        public List<object> addOnDetails { get; set; } 
        public List<object> splitDetails { get; set; } 
        public ExtendedFields2 extendedFields { get; set; } 
    }

    public class CustomFields    {
        public string CountryCode { get; set; } 
        public string membership_card { get; set; } 
    }

    public class PaymentMode    {
        public string mode { get; set; } 
        public double value { get; set; } 
    }

    public class Result    {
        public string identifierType { get; set; } 
        public string identifierValue { get; set; } 
        public string source { get; set; } 
        public ExtendedFields extendedFields { get; set; } 
        public string currencyCode { get; set; } 
        public string type { get; set; } 
        public double billAmount { get; set; } 
        public string billNumber { get; set; } 
        public double discount { get; set; } 
        public List<LineItemsV2> lineItemsV2 { get; set; } 
        public CustomFields customFields { get; set; } 
        public DateTime purchaseTime { get; set; } 
        public string notInterestedReason { get; set; } 
        public List<PaymentMode> paymentModes { get; set; } 
        public DateTime billingDate { get; set; } 
    }

    public class Error    {
        public bool status { get; set; } 
        public string message { get; set; } 
        public int code { get; set; } 
    }

    public class Warnings    {
        public bool status { get; set; } 
        public string message { get; set; } 
        public int code { get; set; } 
    }

    public class Response    {
        public Result result { get; set; } 
        public List<Error> errors { get; set; } 
        public List<Warnings> warnings { get; set; } 
    }

    public class TransactionResponse    {
        public List<Response> response { get; set; } 
        public int totalCount { get; set; } 
        public int failureCount { get; set; } 
        public List<Error> errors { get; set; } 
        public List<Warnings> warnings { get; set; } 
    }


}
