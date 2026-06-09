using System;
using System.Collections.Generic;
using System.Text;

namespace GEN_Shell_MobileBackend_API.Models
{
    public class GetMemberTokenResponse
    {
        public string MemberToken { get; set; }
        public string CardNumber { get; set; }
    }
    public class ErrorResponse
    {
        public int code { get; set; }
        public string message { get; set; }
    }
}
