using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using GEN_Shell_MobileBackend_API.Models;
using GEN_Shell_MobileBackend_API.Services;
using System;
using System.Collections.Generic;
using System.Text;
using GEN_Shell_MobileBackend_API.Utilities;
using System.Net;
using System.Linq;

namespace GEN_Shell_MobileBackend_API
{

    public class FunctionHandlerGetTieringHash
    {
        ICrmService _crmService;
        IDBService _dynamoService;
        string seriesCode;

        public FunctionHandlerGetTieringHash(ICrmService crmService, IDBService dbService, string seriesCode)
        {
            _crmService = crmService;
            _dynamoService = dbService;
            this.seriesCode = seriesCode;
        }

        public FunctionHandlerGetTieringHash()
        {
            //_dynamoService = new DynamoService();
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
        public APIGatewayProxyResponse GetTieringHash(APIGatewayProxyRequest request, ILambdaContext context)
        {
            AesEncryption aesEncrypt = new AesEncryption();
            string requestId = Guid.NewGuid().ToString("N");
            string aesEncryptKey = string.Empty;
            ErrorResponse errorResponse = new ErrorResponse();
            try
            {

                //API Authentication
                var Auth = Helper.API_Authentication(requestId, request, _dynamoService);
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
                var customerLookUpResponseTask = _crmService.CustomersLookupGetAsync(requestId, Profile_identifier).Result;
                if (customerLookUpResponseTask.errors == null)
                {
                    string cardNumber = customerLookUpResponseTask.cardDetails.Where(x => x.seriesCode == seriesCode).Select(y => y.cardNumber).FirstOrDefault();
                    //call get card 
                    // var customerResponseTask = _crmService.CardDetailsGetAsync(requestId, cardNumber).Result;
                    if (!String.IsNullOrEmpty(cardNumber))
                    {
                        var customerResponseTask = _crmService.CardDetailsGetAsync(requestId, cardNumber).Result;
                        if (customerResponseTask.customFields != null)
                        {
                            string decryptedPin = string.Empty;
                            try
                            {
                                //decryptedPin = "DummyValue";
                                decryptedPin = aesEncrypt.Decrypt(customerResponseTask.customFields.bl_token, aesEncryptKey);
                                if (string.IsNullOrEmpty(decryptedPin))
                                {
                                    Console.WriteLine("Request Id {0}  ,Error in Decryption", requestId);
                                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, "{\"message\": \"Decryption Failed\"}", 500);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Request Id {0} , Error in Decryption {1}", requestId, ex.Message);
                                return funcSendResponse(requestId, HttpStatusCode.BadRequest, "{\"message\": \"Decryption Failed\"}", 500);
                            }
                            Console.WriteLine("Decrypted Pin {0}", decryptedPin);

                            //string concatetinatedString = requestAes.CardNumber + decryptedPin;
                            Helper helper = new Helper();
                            string tierHashed = helper.GenerateSHA512(requestId, decryptedPin);

                            GetTieringHashResponse hashResponse = new GetTieringHashResponse();
                            hashResponse.HashedPayload = tierHashed;
                            string response = JsonConvert.SerializeObject(hashResponse);
                            return funcSendResponse(requestId, HttpStatusCode.OK, response, 200);
                        }
                        else
                        {
                            errorResponse.message = "Custom Field Not Present for the customer in customer details";
                            errorResponse.code = 500;
                            return funcSendResponse(requestId, HttpStatusCode.BadRequest, "{\"message\": \"Custom Field Not Present for the customer in customer details\"}", 500);
                        }
                    }
                    else
                    {
                        errorResponse.message = "No Card Number attached to the customer";
                        errorResponse.code = 500;
                        return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(errorResponse), 500);
                    }
                }

                else
                {
                    errorResponse.message = customerLookUpResponseTask.errors[0].message;
                    errorResponse.code = customerLookUpResponseTask.errors[0].code;
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(errorResponse), 500);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("RequestId:{0}. Unknown exception occured with message {1}", requestId, ex.Message);
                errorResponse.message = "Unknown Error";
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
