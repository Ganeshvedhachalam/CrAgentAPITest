using System;
using System.Collections.Generic;

namespace Capillary.ShellProxy.Model.IssueReward.Request
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Reward
    {
        public int quantity { get; set; }
        public int rewardId { get; set; }
    }

    public class IssueRewardRequest
    {
        public string notes { get; set; }
        public string brand { get; set; }
        public string mobile { get; set; }
        public List<Reward> rewards { get; set; }
        public string redemptionTime { get; set; }
        public string transactionNumber { get; set; }
    }

}
