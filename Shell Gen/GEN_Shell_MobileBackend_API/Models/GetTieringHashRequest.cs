using System;
using System.Collections.Generic;
using System.Text;

namespace GEN_Shell_MobileBackend_API.Models
{
    public class GetTieringHashRequest
    {
        public string MobileNumber { get; set; }
        public string CardNumber { get; set; }
    }
}
