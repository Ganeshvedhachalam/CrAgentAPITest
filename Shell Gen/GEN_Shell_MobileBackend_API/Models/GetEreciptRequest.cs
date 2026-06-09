using System;
using System.Collections.Generic;
using System.Text;

namespace GEN_Shell_MobileBackend_API.Models
{
    public class GetEreciptRequest
    {
        public string customerId { get; set; }
        public string paymentId { get; set; }
        //public string billNumber { get; set; }
        public string transactionId { get; set; }
    }
}
