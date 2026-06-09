using System;
using System.Collections.Generic;
using System.Text;

namespace Capillary.ShellProxy.Model.CouponModel.Request
{
       public class User    {
        public string mobile { get; set; } 
        public string externalId { get; set; } 
    }

    public class CustomFields    {
        public string siteid { get; set; } 
        public string amount { get; set; } 
        public string item_code { get; set; } 
    }

    public class RedemptionRequestList    {
        public string code { get; set; } 
        public CustomFields customFields { get; set; } 
    }

    public class CouponRequest    {
        public string billAmount { get; set; } 
        public string transactionNumber { get; set; } 
        public User user { get; set; } 
        public List<RedemptionRequestList> redemptionRequestList { get; set; } 
    }



}
