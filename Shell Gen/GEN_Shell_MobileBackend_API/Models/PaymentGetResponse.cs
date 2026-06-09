using System;
using System.Collections.Generic;
using System.Text;

namespace GEN_Shell_MobileBackend_API.Models
{
    public class PaymentGetResponse
    {
        public int customerId { get; set; }
        public string paymentId { get; set; }
        public string pspReferenceNumber { get; set; }
        public string processInstanceId { get; set; }
        public string preAuthAmount { get; set; }
        public string billingAmount { get; set; }
        public string stationId { get; set; }
        public string pumpNumber { get; set; }
        public string mopName { get; set; }
        public string mop { get; set; }
        public string maskedCard { get; set; }
        public string digitalCard { get; set; }
        public List<string> billingLines { get; set; }
        public string transactionState { get; set; }
        public List<object> violations { get; set; }
    }


}
