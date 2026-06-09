using System;
using System.Collections.Generic;

namespace Capillary.ShellProxy.Model.IsRedeemModel.Response
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Redemption    {
        public bool isRedeemable { get; set; } 
        public List<object> warnings { get; set; } 
        public string appendedErrorMessage { get; set; } 
        public string code { get; set; } 
        public bool isAbsolute { get; set; } 
        public int redemptionsLeft { get; set; } 
        public int numberOfRedemptionsByUser { get; set; } 
    }

    public class RedemptionStatus    {
        public bool status { get; set; } 
        public int code { get; set; } 
        public string message { get; set; } 
    }

    public class Fields    {
    }

    public class Identifier    {
        public string type { get; set; } 
        public string value { get; set; } 
    }

    public class Profile    {
        public string firstName { get; set; } 
        public string lastName { get; set; } 
        public Fields fields { get; set; } 
        public List<Identifier> identifiers { get; set; } 
        public List<object> commChannels { get; set; } 
        public int userId { get; set; } 
        public string accountId { get; set; } 
        public DateTime autoUpdateTime { get; set; } 
    }

    public class Customer    {
        public int id { get; set; } 
        public List<Profile> profiles { get; set; } 
    }

    public class IsRedeemV2Response    {
        public List<Redemption> redemption { get; set; } 
        public RedemptionStatus redemptionStatus { get; set; } 
        public Customer customer { get; set; } 
        public List<object> warnings { get; set; } 
    }



}
