namespace Capillary.ShellProxy.Model.CouponModel
{
    public class CustomerCoupon
    {
        public string CustomerKey { get; set; }
        public string DiscountAmount { get; set; }
        public string CustomerValue { get; set; }
        public string CouponCode { get; set; }
        public string TransactionNumber { get; set; }
        public string Amount { get; set; }
        public string LineItemId { get; set; }
        public bool IsRedeem { get; set; }
        public int ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public int discountUpto{ get ; set; }
        public int discountValue{ get ; set; }
        public string discountType{ get ; set; }
        public string CouponType{ get ; set; }
        public string CRMProductID{ get ; set; }
        public string RedeemFailReason{ get ; set; }

    }
}