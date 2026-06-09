namespace Capillary.ShellProxy.Model.CouponModel
{
    public class AvailableCoupon
    {
        public string CouponCode { get; set; }
        public string SeriesId { get; set; }
        public string CouponName { get; set; }
        public string LineItemId { get; set; }
        public string EcomProductID { get ; set ; }
        public string Type { get; set; }
        public double Value { get; set; }
        public string ExpiryDate { get; set; }
        public string CategoryCode { get; set; }
        public string SubCategoryCode { get; set; }
        public string AdditionalProductCode { get; set; }
        public string AdditionalProductInfo { get; set; }
        public string itemtype { get; set; }
        public string itemsubstype { get; set; }
        public PromoLevel PromoLevel { get; set; }

    }
}