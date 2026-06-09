using System;
using System.Collections.Generic;

namespace Capillary.ShellProxy.Model.IssueReward.Response
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Status
    {
        public bool success { get; set; }
        public int code { get; set; }
        public string message { get; set; }
    }
    public class Reward
    {
        public Status status { get; set; }
        public int rewardId { get; set; }
        public int requestedQuantity { get; set; }
        public object intouch { get; set; }
        public List<Promotion> promotions { get; set; }
        public object vendor { get; set; }
    }
    public class Promotion
    {
        public int pointsRedeemed { get; set; }
        public string promotionExpiry { get; set; }
        public string name { get; set; }
    }

    public class IssueRewardResponse
    {
        public Status status { get; set; }
        public List<Reward> rewards { get; set; }
    }
}
