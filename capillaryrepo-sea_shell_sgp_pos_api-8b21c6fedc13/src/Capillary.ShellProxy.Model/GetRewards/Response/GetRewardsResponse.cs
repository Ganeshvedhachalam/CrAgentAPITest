using System;
using System.Collections.Generic;
using System.Text;

namespace Capillary.ShellProxy.Model.GetRewards.Response
{
   // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Status
    {
        public bool success { get; set; }
        public int code { get; set; }
        public string message { get; set; }
    }

    public class RewardList
    {
        public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string imageId { get; set; }
        public string imageUrl { get; set; }
        public string thumbnailId { get; set; }
        public string thumbnailUrl { get; set; }
        public string termAndConditionsId { get; set; }
        public string termAndConditionsUrl { get; set; }
        public string tier { get; set; }
        public string label { get; set; }
        public int priority { get; set; }
        public int intouchPoints { get; set; }
        public string group { get; set; }
        public string startTime { get; set; }
        public string endTime { get; set; }
        public bool expired { get; set; }
        public bool started { get; set; }
        public object programId { get; set; }
    }

    public class PagingDto
    {
        public bool last { get; set; }
        public int totalElements { get; set; }
        public int totalPages { get; set; }
        public int numberOfElements { get; set; }
        public bool first { get; set; }
        public int size { get; set; }
        public int number { get; set; }
    }

    public class GetRewardsResponse
    {
        public Status status { get; set; }
        public List<RewardList> rewardList { get; set; }
        public PagingDto pagingDto { get; set; }
    }


}
