using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Runtime.Internal;
using GEN_Shell_MobileBackend_API.Models;
using GEN_Shell_MobileBackend_API.Models.EmailCommResponse;
using GEN_Shell_MobileBackend_API.Services;
using GEN_Shell_MobileBackend_API.Utilities;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GEN_Shell_MobileBackend_API
{
    public class FunctionHandlerDynamicQRDecrypt
    {
        string aesEncryptKey = string.Empty;
        string lambdaVersion;        
        IDBService _dynamoService;
        public FunctionHandlerDynamicQRDecrypt()
        {
            RegionEndpoint region;
            var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION");

            if (!string.IsNullOrEmpty(awsRegion))
            {
                region = RegionEndpoint.GetBySystemName(awsRegion);
                lambdaVersion = Environment.GetEnvironmentVariable("LAMBDA_VERSION");
                aesEncryptKey = Environment.GetEnvironmentVariable("AES_ENCRYPTKEY");
            }
            else
            {
                region = RegionEndpoint.GetBySystemName("ap-southeast-1");
                lambdaVersion = "0";
                aesEncryptKey = "";
            }
            _dynamoService = new DynamoService();
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse AesDecrypt(APIGatewayProxyRequest request, ILambdaContext context)
        {
            string requestId = Guid.NewGuid().ToString("N");
            GEN_Shell_MobileBackend_API.Models.ErrorResponse errorResponse = new GEN_Shell_MobileBackend_API.Models.ErrorResponse();
            DecryptResponse decryptResponse = new DecryptResponse();
            AesEncryption aes = new AesEncryption();
            
            try
            {                
                //API Authentication
                if (!request.Headers.TryGetValue("X-Cap-OrgId", out string OrgID))
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(new APIErrorClass { message = "X-Cap-OrgId is missing in headers" }), 401);
                if (!request.Headers.TryGetValue("X-Cap-Environment", out string Environment))
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(new APIErrorClass { message = "X-Cap-Environment is missing in headers" }), 401);
                if (!request.Headers.TryGetValue("X-Cap-APIKey", out string API_Key))
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(new APIErrorClass { message = "X-Cap-APIKey is missing in headers" }), 401);
                var APIKey = _dynamoService.GetAPIAccessKeyAsync(requestId, OrgID, Environment).Result;
                if (string.IsNullOrEmpty(APIKey))
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(new APIErrorClass { message = "API Key is either inactive or not configured for the Org" }), 401);
                if (API_Key.ToUpper() != APIKey.ToUpper())
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(new APIErrorClass { message = "API key sent is wrong" }), 401);                

                DynamicQRDecryptRequest decryptRequest = JsonConvert.DeserializeObject<DynamicQRDecryptRequest>(request.Body);
                string decryptedTxt = aes.Decrypt(decryptRequest.inputString, aesEncryptKey);

                if (!string.IsNullOrEmpty(decryptedTxt))
                {                    
                    TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                    int secondsSinceEpoch = (int)t.TotalSeconds;
                    int epochTimeFromDecrypt = Convert.ToInt32(decryptedTxt.Split("|").LastOrDefault());

                    if (secondsSinceEpoch < epochTimeFromDecrypt)
                    {
                        decryptResponse.DecryptedData = decryptedTxt.Split("|").FirstOrDefault();
                        decryptResponse.Code = 200;
                        decryptResponse.Message = "Success";
                        return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(decryptResponse), 200);
                    }                        
                    else
                        return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(new APIErrorClass { message = "QR code expired. Please refresh QR code." }), 601);
                }
                else
                {
                    errorResponse.message = "Decryption Failed";
                    errorResponse.code = 500;
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(errorResponse), 500);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("RequestId:{0}. Decryption Failed {1}", requestId, ex.Message);
                errorResponse.message = "Decryption Failed";
                errorResponse.code = 500;
                return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(errorResponse), 500);
            }

        }

        Func<string, HttpStatusCode, string, int, APIGatewayProxyResponse> funcSendResponse = (requestId, httpStatusCode, body, returnCode) =>
        {
            Console.WriteLine("RequestId:{0}. Response:{1}", requestId, body.Replace(Environment.NewLine, " "));
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)httpStatusCode,
                Headers = new Dictionary<string, string> { { "content-type", "application/json" }, { "INTG-RequestID", requestId } },
                Body = body
            };
        };

    }
}
