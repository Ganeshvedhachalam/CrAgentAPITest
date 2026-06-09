using System;
using System.Collections.Generic;
using System.Text;

namespace Capillary.ShellProxy.Model
{
    public class ErrorReport
    {
        public string SrNo { get; set; }
        public string Category { get; set; }
        public string Transaction { get; set; }
        public string Cost_centre { get; set; }
        public string Loyalty_Action { get; set; }
        public string Claim_Type { get; set; }
        public string Sold_To { get; set; }
        public string Ship_To { get; set; }
        public string Value { get; set; }
        public string Txn_Req { get; set; }
        public string Txn_Res { get; set; }    
        public string CAP_TxnResponse { get; set; }    
        public string Failure_reason { get; set; }
        public string IdentifierType { get; set; }
        public string IdentifierValue { get; set; } 
        public string CRMProductID { get; set; }  
        public string CouponCode { get; set; }       
    }
}
