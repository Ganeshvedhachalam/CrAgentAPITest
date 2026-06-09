using System;
using System.Collections.Generic;
using System.Text;

namespace GEN_Shell_MobileBackend_API.Models
{
    public class GetTieringHashResponse
    {
        public string HashedPayload { get; set; }
    }
    public class GetTieringHashErrorResponse
    {
        public string ErrorMessage { get; set; }
    }
}
