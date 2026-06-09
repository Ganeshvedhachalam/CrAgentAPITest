using System;
using System.Collections.Generic;
using System.Text;

namespace Capillary.ShellProxy.Model.PromotionModel.Request
{

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class AppliedPromotion
    {
        public string discount { get; set; }
        public string discountAppliedOnQuantity { get; set; }
        public string id { get; set; }
        public string messageLabel { get; set; }
        public string name { get; set; }
        public string promotionAppliedOnQuantity { get; set; }
        public int redemptionCount { get; set; }
        public string redemptionIdentifier { get; set; }
        public string type { get; set; }
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
    public class CartTender
    {
        public string identifier { get; set; }
        public double amount { get; set; }
    }

    public class PromotionRequest
    {
        public string amount { get; set; }
        public List<AppliedPromotion> appliedPromotions { get; set; }
        public List<CartItem> cartItems { get; set; }
        public bool categoryHierarchySentInPayload { get; set; }
        public string customerId { get; set; }
        public string evaluationId { get; set; }
        public List<CartTender> cartTenders { get; set; }
        public List<string> promoCodes { get; set; }
        public List<string> paymentVouchers { get; set; }

    }



}
