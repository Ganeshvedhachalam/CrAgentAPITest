using System;
using System.Collections.Generic;
using System.Text;

namespace Capillary.ShellProxy.Model.CartModel.Response
{

  // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class CartPromotionRule    {
        public string Description { get; set; } 
        public double DiscountAmount { get; set; } 
        public double RuleId { get; set; } 
        public string Title { get; set; } 
    }

    public class CartItem    {
        public double ProductId { get; set; } 
        public double VariantProductId { get; set; } 
        public double MRP { get; set; } 
        public double WebPrice { get; set; } 
        public double Quantity { get; set; } 
        public string description { get; set; } 
        public string SupplierId { get; set; } 
        public string CartReferenceKey { get; set; } 
        public bool IsFreeProduct { get; set; } 
        public bool PriceCapped { get; set; } 
        public double TotalCap { get; set; } 
        public string CappedRefKey { get; set; } 
        public double CatalogpromotionDiscount { get; set; } 
        public string BundleCartReferenceKey { get; set; } 
        public bool IsPrimaryProduct { get; set; } 
        public double ItemPromotionDiscountAmount { get; set; } 
        public bool IsPromotionProduct { get; set; } 
        public string Por { get; set; } 
        public bool IsDefaultBundleItem { get; set; } 
        public string ProductImage { get; set; } 
        public List<CartPromotionRule> CartPromotionRules { get; set; } 
        public string CategoryId { get; set; } 
        public object CategoryName { get; set; } 
        public string BrandId { get; set; } 
        public string BrandName { get; set; } 
        public List<string> ParentCartItems { get; set; } 
    }

    public class Supplier    {
        public string SupplierId { get; set; } 
        public string SupplierName { get; set; } 
        public bool IsSelected { get; set; } 
        public object OrderStatus { get; set; } 
    }

    public class ShippingOption    {
        public string SupplierId { get; set; } 
        public string ShippingMode { get; set; } 
        public double ShippingModeId { get; set; } 
        public bool isselected { get; set; } 
    }

    public class Carts    {
        public string MerchantId { get; set; } 
        public double ProductCost { get; set; } 
        public double ShippingCost { get; set; } 
        public double VoucherDiscount { get; set; } 
        public double PromotionDiscount { get; set; } 
        public double TaxAmount { get; set; } 
        public double OrderTotal { get; set; } 
        public string VoucherCode { get; set; } 
        public string UserSelectedCurrency { get; set; } 
        public object Bill_FirstName { get; set; } 
        public string Bill_LastName { get; set; } 
        public string Bill_Address1 { get; set; } 
        public object Bill_Address2 { get; set; } 
        public string Bill_CountryCode { get; set; } 
        public object _Bill_StateCode { get; set; } 
        public string Bill_City { get; set; } 
        public string Bill_CityCode { get; set; } 
        public object Bill_OtherCityName { get; set; } 
        public object Bill_Telephone { get; set; } 
        public object Bill_Mobile { get; set; } 
        public object Bill_PostCode { get; set; } 
        public string Bill_Email { get; set; } 
        public object Ship_FirstName { get; set; } 
        public string Ship_LastName { get; set; } 
        public string Ship_Address1 { get; set; } 
        public object Ship_Address2 { get; set; } 
        public string Ship_CountryCode { get; set; } 
        public object Ship_StateCode { get; set; } 
        public object Ship_City { get; set; } 
        public object Ship_CityCode { get; set; } 
        public object Ship_OtherCityName { get; set; } 
        public object Ship_Telephone { get; set; } 
        public object Ship_Mobile { get; set; } 
        public object Ship_PostCode { get; set; } 
        public object Ship_Email { get; set; } 
        public List<CartItem> CartItems { get; set; } 
        public List<Supplier> Suppliers { get; set; } 
        public List<ShippingOption> ShippingOptions { get; set; } 
        public List<object> PaymentOptionsChannel { get; set; } 
        public object ErrorCollection { get; set; } 
        public string GiftMsg { get; set; } 
        public DateTime DemandedDeliveryDate { get; set; } 
        public double RemainTotal { get; set; } 
        public object ShippingZoneType { get; set; } 
        public double DeliverySlotID { get; set; } 
        public object FailedProducts { get; set; } 
        public string PickupLastName { get; set; } 
        public string PickupEmail { get; set; } 
        public string LocationId { get; set; } 
        public List<object> TaxDetail { get; set; } 
        public List<object> ComboSuggestion { get; set; } 
        public List<object> ConvertedDeals { get; set; } 
        public List<object> BusinessRuleDescriptionView { get; set; } 
        public object AppliedPromotionDetailsList { get; set; } 
    }

    public class ListOfError    {
        public double ErrorCode { get; set; } 
        public string ErrorMessage { get; set; } 
    }

    public class FailedItem    {
        public string ProductId { get; set; } 
        public string VariantProductId { get; set; } 
        public string ErrorType { get; set; } 
        public string ErrorMessage { get; set; } 
        public List<object> ChildItems { get; set; } 
        public List<ListOfError> ListOfErrors { get; set; } 
    }

    public class CartResponse    {
        public string messageCode { get; set; } 
        public string Message { get; set; } 
        public Carts Carts { get; set; } 
        public List<FailedItem> FailedItems { get; set; } 
        public double ErrorCode { get; set; } 
    }



}
