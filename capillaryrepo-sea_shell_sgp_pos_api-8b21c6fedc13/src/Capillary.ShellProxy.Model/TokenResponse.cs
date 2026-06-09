using System;
using System.Collections.Generic;
using System.Text;

namespace Capillary.ShellProxy.Model
{
 

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Token
    {
        public string AccessToken { get; set; }
        public DateTime issued_at { get; set; }
        public string UserId { get; set; }
        public bool IsLoggedInUser { get; set; }
        public string MerchantId { get; set; }
    }

    public class TokenResponse
    {
        public string messageCode { get; set; }
        public string Message { get; set; }
        public Token Token { get; set; }
        public int ErrorCode { get; set; }
    }


}
