using Amazon.Lambda.APIGatewayEvents;
using GEN_Shell_MobileBackend_API.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace GEN_Shell_MobileBackend_API.Utilities
{
    public class Helper
    {
       
        public static string  API_Authentication(string requestId, APIGatewayProxyRequest request)
        {
            IDBService _dynamoService;
            try
            {
                Console.WriteLine("RequestId:{0}. Enter API_Authentication",requestId);
                _dynamoService = new DynamoService();
                //Check for Header if present or not
                if (request.Headers == null && request.Headers.Count <= 0)
                    return "Headers are missing";

                //Check for mandetory headers values
                if (!request.Headers.TryGetValue("X-Cap-OrgId", out string OrgID))
                    return "X-Cap-OrgId is missing in headers";
                if (!request.Headers.TryGetValue("X-Cap-Environment", out string Environment))
                    return "X-Cap-Environment is missing in headers";
                if (!request.Headers.TryGetValue("X-Cap-APIKey", out string API_Key))
                    return "X-Cap-APIKey is missing in headers";
                if (!request.Headers.TryGetValue("X-Cap-Profile-Identifier", out string Profile_identifier))
                    return "X-Cap-Profile-Identifier is missing in headers";

                //API Authentication
                var APIKey = _dynamoService.GetAPIAccessKeyAsync(requestId, OrgID, Environment).Result;
                if (string.IsNullOrEmpty(APIKey))
                    return "API Key is either inactive or not configured for the Org";
                if (API_Key.ToUpper() != APIKey.ToUpper())
                    return "API key sent is wrong";

                return "success";

            }
            catch (Exception ex)
            {
                Console.WriteLine("RequestId:{0}. Exception at Api_Authentication {1} ",requestId, ex.Message);
                return "fail";

            }
        }

        public string GenerateSHA512(string requestId,string input)
        {
            string hash = string.Empty;
            try
            {
                using (SHA512 sha512Hash = SHA512.Create())
                {
                    //From String to byte array
                    byte[] sourceBytes = Encoding.UTF8.GetBytes(input);
                    byte[] hashBytes = sha512Hash.ComputeHash(sourceBytes);
                    hash = BitConverter.ToString(hashBytes).Replace("-", String.Empty);
                    Console.WriteLine("The SHA512 hash of " + input + " is: " + hash);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Request Id {0} Exception {1}",requestId,ex.Message);
            }
            return hash;
        }

        public static HttpContent CreateStringContent<Tin>(Tin content)
        {
            //return new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");

            return new StringContent(JsonConvert.SerializeObject(content,
                            Newtonsoft.Json.Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            }), Encoding.UTF8, "application/json");
        }

        public static long CurrentUnixTime(string requestId)
        {
            DateTimeOffset unixEpoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
            TimeSpan timeSinceEpoch = DateTimeOffset.UtcNow - unixEpoch;
            //long currentUnixTime = (long)timeSinceEpoch.TotalSeconds;
            long currentUnixTime = (long)timeSinceEpoch.TotalMilliseconds;
            Console.WriteLine("RequestId:{0}. currentUnixTime: {1} ", requestId, currentUnixTime);
            return currentUnixTime;
        }

        public static long CurrentUtcTimeFromUnixTime(string requestId,long unixEpochTime) 
        {
            //DateTimeOffset utcTime = DateTimeOffset.FromUnixTimeSeconds(unixEpochTime);
            DateTimeOffset utcTime = DateTimeOffset.FromUnixTimeMilliseconds(unixEpochTime);
            Console.WriteLine("RequestId:{0}. unixEpochTime: {1} ", requestId, unixEpochTime);
            //return utcTime.ToString("yyyy-MM-dd HH:mm:ss");
            return Convert.ToInt32(utcTime.ToString("yyyyMMdd"));
        }
    }
}
