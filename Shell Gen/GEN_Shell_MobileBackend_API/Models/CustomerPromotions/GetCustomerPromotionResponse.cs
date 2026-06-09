using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GEN_Shell_MobileBackend_API.Models.Promotions
{
    public class GetCustomerPromotionResponse
    {
        public List<Datum> data { get; set; }
        public List<object> errors { get; set; }
        public long timestamp { get; set; }
        public int errorCode { get; set; }
        public string message { get; set; }
        public string[] errorDetails { get; set; }
    }
    public class Datum
    {
        public string earnedPromotionId { get; set; }
        public string promotionId { get; set; }
        public string promotionName { get; set; }
        public long validTill { get; set; }
        public long unlockedDate { get; set; }
        public int customerId { get; set; }
        public string earnedType { get; set; }
        public string earnedStatus { get; set; }
        public string promotionStatus { get; set; }
        public string applicationMode { get; set; }
        public long redeemableFrom { get; set; }
        
        public IDictionary<string, string> customFieldValues { get; set; }
        public Restrictions restrictions { get; set; }
        public long eventTime { get; set; }
        public int mileStoneId { get; set; }
        public int targetGroupId { get; set; }
    }

    public class Customfieldvalues
    {
        public string purpose { get; set; }
        public string standard_image_1 { get; set; }
        public string standard_description { get; set; }
        public string gsap { get; set; }
        public string promo_type { get; set; }
    }

    public class Restrictions
    {
        public List<Customer> Customer { get; set; }
        public List<Cart> Cart { get; set; }
        public List<Earn> Earn { get; set; }
    }

    public class Customer
    {
        public string kpi { get; set; }
        public string maxLimit { get; set; }
        public string remainingRedemption { get; set; }
    }

    public class Cart
    {
        public string kpi { get; set; }
        public string maxLimit { get; set; }
        public string remainingRedemption { get; set; }
    }

    public class Earn
    {
        public string kpi { get; set; }
        public string maxLimit { get; set; }
        public string remainingRedemption { get; set; }
    }





}
