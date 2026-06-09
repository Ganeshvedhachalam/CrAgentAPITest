using System;
using System.Collections.Generic;
using System.Text;

namespace GEN_Shell_MobileBackend_API.Models
{
    public class CapillaryMemberShipResponse
    {
        public bool Success { get; set; }
        public int AvailablePoints { get; set; }
        //public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }
}
