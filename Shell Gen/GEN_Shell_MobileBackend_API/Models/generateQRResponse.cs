using System;
using System.Collections.Generic;
using System.Text;

namespace GEN_Shell_MobileBackend_API.Models
{
    public class GenerateQRResponse
    {
        public string EncryptedString { get; set; }
        public string KeyExpiredTime { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
    }
}
