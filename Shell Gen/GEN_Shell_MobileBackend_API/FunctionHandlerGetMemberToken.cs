using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GEN_Shell_MobileBackend_API.Models;
using GEN_Shell_MobileBackend_API.Services;
using GEN_Shell_MobileBackend_API.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Linq;

namespace GEN_Shell_MobileBackend_API
{
    public class FunctionHandlerGetMemberToken
    {
        ICrmService _crmService;
        IDBService _dynamoService;
        string seriesCode;
        public FunctionHandlerGetMemberToken()
        {
            RegionEndpoint region;
            string lambdaVersion;
            string intouchSvcUrl;
            string username;
            string password;

            var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION");
            if (!string.IsNullOrEmpty(awsRegion))
            {

                region = RegionEndpoint.GetBySystemName(awsRegion);
                lambdaVersion = Environment.GetEnvironmentVariable("LAMBDA_VERSION");
                intouchSvcUrl = Environment.GetEnvironmentVariable("CRM_SVC_URL");
                username = Environment.GetEnvironmentVariable("CRM_SVC_USERNAME");
                password = Environment.GetEnvironmentVariable("CRM_PASSWORD");
                seriesCode = Environment.GetEnvironmentVariable("SERIES_CODE");
            }
            else
            {
                region = RegionEndpoint.GetBySystemName("us-east-1");
                lambdaVersion = "0";
                intouchSvcUrl = "http://apac2.api.capillarytech.com";
                username = "demo.shell.mly.10208771.01";
                password = "7d235ffc623ed667ccda39e92930040c";
                seriesCode = "BLTEST";
            }
            _crmService = new IntouchService(intouchSvcUrl, username, password, lambdaVersion);
            _dynamoService = new DynamoService();
        }
        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse GetMemberToken(APIGatewayProxyRequest request, ILambdaContext context)
        {
            ErrorResponse errorResponse = new ErrorResponse();
            AesEncryption aesEncrypt = new AesEncryption();
            ErrorResponse errResponse = new ErrorResponse();
            string requestId = Guid.NewGuid().ToString("N");
            string aesEncryptKey = string.Empty;
            try
            {
                //API Authentication
                var Auth = Helper.API_Authentication(requestId, request);
                if (Auth != "success")
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(new APIErrorClass { message = Auth }), 401);
                //Get mobile configurations keys
                request.Headers.TryGetValue("X-Cap-OrgId", out string OrgID);
                request.Headers.TryGetValue("X-Cap-Environment", out string Environment);
                request.Headers.TryGetValue("X-Cap-Profile-Identifier", out string Profile_identifier);
                var mobileKeysJson = _dynamoService.GetMobileConfigsAsync(requestId, OrgID, Environment).Result;
                if (string.IsNullOrEmpty(mobileKeysJson))
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, "{\"message\": \"Org not found\"}", 401);
                var mobileKeys = JsonConvert.DeserializeObject<DBResponseModel>(mobileKeysJson);
                var bonusLinkKeys = mobileKeys.artifacts.Where(c => c.source == "bonuslink").FirstOrDefault();
                if (bonusLinkKeys == null)
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, "{\"message\": \"BonusLink keys not found for this org\"}", 401);
                var aesKey_DB = bonusLinkKeys.Keys.Where(c => c.key == "tokenEncyptionKey").FirstOrDefault();

                if (aesKey_DB == null)
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, "{\"message\": \"Encryption key not configured for this org\"}", 401);
                aesEncryptKey = aesKey_DB.value;
                Console.WriteLine("Encryption key {0}", aesEncryptKey);
                string inputString = string.Empty;
                GetTieringHashRequest requestAes = new GetTieringHashRequest();
                OnBoardingHashResponse responseHash = new OnBoardingHashResponse();
                requestAes = JsonConvert.DeserializeObject<GetTieringHashRequest>(request.Body);
                Console.WriteLine("Input Request {0}", requestAes);
                if (!String.IsNullOrEmpty(Profile_identifier))
                {
                    var customerResponseTask = _crmService.CustomersLookupGetAsync(requestId, Profile_identifier).Result;
                    string cardNumber = customerResponseTask.cardDetails.Where(x => x.seriesCode.ToLower() == seriesCode.ToLower()).Select(y => y.cardNumber).FirstOrDefault();
                    //call get card 
                    var customerResponse = _crmService.CardDetailsGetAsync(requestId, cardNumber).Result;
                    if (customerResponse.customFields != null)
                    {
                        string decryptedPin = string.Empty;
                        try
                        {
                            if (!String.IsNullOrEmpty(customerResponse.customFields.bl_token))
                            {
                                decryptedPin = aesEncrypt.Decrypt(customerResponse.customFields.bl_token, aesEncryptKey);
                            }
                            else
                            {
                                errorResponse.message = "Custom Fields Not Present";
                                errorResponse.code = 500;
                                return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(errorResponse), 500);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Decryption Failed with error");
                            Console.WriteLine("RequestId:{0}. Unknown exception occured with message {1}", requestId, ex.Message);
                            errorResponse.message = "Decryption Failed";
                            errorResponse.code = 500;
                            return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(errorResponse), 500);
                        }
                        if (!String.IsNullOrEmpty(decryptedPin))
                        {
                            Console.WriteLine("Decrypted Pin {0}", decryptedPin);
                            GetMemberTokenResponse tokenResponse = new GetMemberTokenResponse();
                            tokenResponse.CardNumber = cardNumber;
                            tokenResponse.MemberToken = decryptedPin;
                            string response = JsonConvert.SerializeObject(tokenResponse);
                            return funcSendResponse(requestId, HttpStatusCode.OK, response, 200);
                        }
                        else
                        {
                            Console.WriteLine("Decryption Failed ");

                            return funcSendResponse(requestId, HttpStatusCode.BadRequest, "{\"message\": \"Empty Custom Field\"}", 500);
                        }
                    }
                    else
                    {
                        errorResponse.message = "Custom Field Not Present";
                        errorResponse.code = 500;
                        return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(errorResponse), 500);
                    }
                }
                else
                {
                    errorResponse.message = "Invalid Parameter or Payload";
                    errorResponse.code = 500;
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(errorResponse), 500);
                    // return funcSendResponse(requestId, HttpStatusCode.BadRequest, "{\"message\": \"Invalid Parameter or Payload\"}", 400);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("RequestId:{0}. Unknown exception occured with message {1}", requestId, ex.Message);
                return funcSendResponse(requestId, HttpStatusCode.OK, "{\"message\": \"Decryption Failed\"}", 500);
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
