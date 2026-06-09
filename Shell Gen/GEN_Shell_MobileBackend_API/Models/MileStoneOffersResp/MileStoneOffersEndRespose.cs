using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GEN_Shell_MobileBackend_API.Models.MileStoneOffersResp
{
    public class MileStoneOffersEndRespose
    {
        public bool status {  get; set; }
        public IList<Milestone> milestones { get; set; }
        public IList<Datum> data { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string errorCode { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string errorDescription { get; set; }
    }
    public class Milestone
    {
        public string promotionId { get; set; }
        public bool isExpired { get; set; }
        public long milestoneExpiry { get; set; }
        //public string earnedPromotionId { get; set; }        
        public string promotionName { get; set; }
        public string imageUrl { get; set; }
        //public long validTill { get; set; }
        //public long unlockedDate { get; set; }
        //public int customerId { get; set; }
        //public string earnedType { get; set; }
        //public string earnedStatus { get; set; }
        //public string promotionStatus { get; set; }
        //public string applicationMode { get; set; }
        //public long redeemableFrom { get; set; }
        //public Customfieldvalues customFieldValues { get; set; }
        //public Restrictions restrictions { get; set; }
        //public long eventTime { get; set; }
        public int mileStoneId { get; set; }
        public int targetGroupId { get; set; }
        public IList<Availableinstance> availableInstances { get; set; }
        public IList<Expiredinstance> expiredInstances { get; set; }
        public Activetargetdetails activeTargetDetails { get; set; }
    }

    public class Activetargetdetails
    {
        public string targetEvaluationType { get; set; }
        public string targetId { get; set; }
        public int periodId { get; set; }
        public string periodStatus { get; set; }
        public string periodRefCode { get; set; }
        public string periodStartDate { get; set; }
        public string periodEndDate { get; set; }
        public float targetValue { get; set; }
        public float targetAchievedValue { get; set; }
        public float targetProgress { get; set; }
        public string targetName { get; set; }
        public string targetType { get; set; }
        public string targetEntity { get; set; }
        public int targetRuleId { get; set; }
        public bool currentPeriod { get; set; }
        public object milestones { get; set; }
    }

    public class Availableinstance
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
        public int mileStoneId { get; set; }
        public int targetGroupId { get; set; }
        public string applicationMode { get; set; }
        public long redeemableFrom { get; set; }
        //public Customfieldvalues customFieldValues { get; set; }
        public IDictionary<string, string> customFieldValues { get; set; }
        public Restrictions restrictions { get; set; }
        public long eventTime { get; set; }
    }

    public class Expiredinstance
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
        public int mileStoneId { get; set; }
        public int targetGroupId { get; set; }
        public string applicationMode { get; set; }
        public long redeemableFrom { get; set; }        
        public IDictionary<string, string> customFieldValues { get; set; }
        public Restrictions restrictions { get; set; }
        public long eventTime { get; set; }
    }

    public class Customfieldvalues
    {
        public string purpose { get; set; }
        public string promo_type { get; set; }
    }

    public class Restrictions
    {
        public List<Earn> Earn { get; set; }
        public List<Cart> Cart { get; set; }
    }

    public class Earn
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
        //public Customfieldvalues1 customFieldValues { get; set; }
        public Restrictions1 restrictions { get; set; }
        public long eventTime { get; set; }
    }

    public class Customfieldvalues1
    {
        public string purpose { get; set; }
        public string standard_image_1 { get; set; }
    }

    public class Restrictions1
    {
        public List<Customer1> Customer { get; set; }
        public List<Cart1> Cart { get; set; }
    }

    public class Customer1
    {
        public string kpi { get; set; }
        public string maxLimit { get; set; }
        public string remainingRedemption { get; set; }
    }

    public class Cart1
    {
        public string kpi { get; set; }
        public string maxLimit { get; set; }
        public string remainingRedemption { get; set; }
    }

}
