using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Capillary.ShellProxy.Utilities
{
    public static class Helper
    {

        public static (string idName, string idValue, bool isNotInterested) ResolveCustomerDetails(string customerDataType, string customerDataValue)
        {
            string key= string.Empty;
            string value = string.Empty;
            bool isNotInterested = true;

            if(customerDataType.ToLower() == "digitalloyaltycard" || customerDataType.ToLower() == "loyaltycard")
            {
                key = "cardnumber";
                value = customerDataValue;
                isNotInterested = false;
            }
            else if (customerDataType.ToLower() == "mobilenumber")
            {
                key = "mobile";
                value = customerDataValue.Contains("+") ? customerDataValue.Replace("+","") : customerDataValue;
                isNotInterested = false;

            }
            if(string.IsNullOrEmpty(value) || customerDataType.ToLower() == "non-loyalty")
                isNotInterested = true;
            return (idName: key, idValue: value, isNotInterested :isNotInterested);
        }

        public static HttpContent CreateStringContent<Tin>(Tin content)
        {
            //return new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");

            return new StringContent(JsonConvert.SerializeObject(content, 
                            Newtonsoft.Json.Formatting.None, 
                            new JsonSerializerSettings { 
                                NullValueHandling = NullValueHandling.Ignore
                            }),Encoding.UTF8, "application/json");
        }

        public static HttpContent CreateStringIgnoringDefaultValuesContent<Tin>(Tin content)
        {
            //return new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");

            return new StringContent(JsonConvert.SerializeObject(content, 
                            Newtonsoft.Json.Formatting.None, 
                            new JsonSerializerSettings { 
                                NullValueHandling = NullValueHandling.Ignore,
                                DefaultValueHandling = DefaultValueHandling.Ignore
                            }),Encoding.UTF8, "application/json");
        }

        public static HttpContent CreateFormUrlEncodedContent(Dictionary<string, string> content)
        {
            return new FormUrlEncodedContent(content);
        }

        public static DateTime UnixTimeStampToDateTime( double unixTimeStamp )
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }

        public static bool CompareKeys(string requestId, string authorizationToken, string siteID, string masterClientID)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var decodedValue = handler.ReadJwtToken(authorizationToken).Payload;
                if (decodedValue == null)
                    return false;

                if (decodedValue["client_id"].ToString() == masterClientID)
                {
                    Console.WriteLine("RequestId:{0}.Master keys has been used. Hence Auth is enabled", requestId);
                    return true;
                }

                Console.WriteLine("RequestId:{0}. {1} compare with {2}", requestId, siteID, decodedValue["client_id"]);
                if (decodedValue["client_id"].ToString() != siteID)
                {
                    Console.WriteLine("RequestId:{0}.Site specific Token validation has been failed", requestId);
                    return false;
                }
                Console.WriteLine("RequestId:{0}.Site specific Token validation is success", requestId);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception in CompareKeys with message :{1}", requestId, e.Message);
            }
            return false;

        }

    }
}