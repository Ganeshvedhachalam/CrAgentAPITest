using System;
using System.Collections.Generic;
using System.Text;

namespace GEN_Shell_MobileBackend_API.Models.Promotions
{

   // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class StoreCriteria
    {
        public string type { get; set; }
        public List<int> values { get; set; }
        public string @operator { get; set; }
    }

    public class CustomFieldValues
    {
        public string standard_image_1 { get; set; }
        public string standard_description { get; set; }
        public string alt_text { get; set; }
        public string purpose { get; set; }
        public string short_name { get; set; }
        public string long_name { get; set; }
    }

    public class CartPromotion
    {
        public string kpi { get; set; }
        public object frequency { get; set; }
        public object minTimeBetweenRepeat { get; set; }
        public string limit { get; set; }
    }

    public class PromotionRestrictions
    {
        public List<object> Promotion { get; set; }
        public List<object> Customer { get; set; }
        public List<CartPromotion> Cart { get; set; }
    }

    public class DatumPromotion
    {
        public string promotionId { get; set; }
        public string promotionName { get; set; }
        public string promotionType { get; set; }
        public int milestoneId { get; set; }
        public int groupId { get; set; }
        public object ruleId { get; set; }
        public long expiry { get; set; }
        public string description { get; set; }
        public StoreCriteria storeCriteria { get; set; }
        public string reward { get; set; }
        public int maxEarningPerCustomer { get; set; }
        public bool isActive { get; set; }
        public CustomFieldValues customFieldValues { get; set; }
        public PromotionRestrictions promotionRestrictions { get; set; }
    }

    public class PromotionDetailsResp
    {
        public List<DatumPromotion> data { get; set; }
        public List<Error> errors { get; set; }
        public long timestamp { get; set; }
        public int errorCode { get; set; }
        public string message { get; set; }
        public string[] errorDetails { get; set; }
    }


    public class Error
    {
        public int code { get; set; }
        public string message { get; set; }
    }
}
