using System.Collections.Generic;

namespace Capillary.ShellProxy.Model.CouponModel
{
    public class CartPromotion
    {
        public string RuleId { get; set; }
        public int ProductId { get; set; }
        public double TotalPrice { get; set; }
        public PromoLevel PromoLevel { get; set; }
        public List<string> GroupedPIDs { get; set; } = new List<string>();
        public double DiscountAmount { get; set; }
    }

    public class GroupedPID
    {
        public string ProductId { get; set; }
        public double TotalPrice { get; set; }
    }
}