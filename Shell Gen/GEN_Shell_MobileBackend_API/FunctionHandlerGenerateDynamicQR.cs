using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GEN_Shell_MobileBackend_API.Models;
using GEN_Shell_MobileBackend_API.Services;
using GEN_Shell_MobileBackend_API.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace GEN_Shell_MobileBackend_API
{
    public class FunctionHandlerGenerateDynamicQR
    {
        ICrmService _crmService;
        IDBService _dynamoService;
        string aesEncryptKey = string.Empty;
        string AddedTime = string.Empty;
        string lambdaVersion;
        string intouchSvcUrl;
        public FunctionHandlerGenerateDynamicQR()
        {
            RegionEndpoint region;

            var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION");
            if (!string.IsNullOrEmpty(awsRegion))
            {

                region = RegionEndpoint.GetBySystemName(awsRegion);
                lambdaVersion = Environment.GetEnvironmentVariable("LAMBDA_VERSION");
                intouchSvcUrl = Environment.GetEnvironmentVariable("CRM_SVC_URL");
                // username = Environment.GetEnvironmentVariable("CRM_SVC_USERNAME");
                // password = Environment.GetEnvironmentVariable("CRM_PASSWORD");
                aesEncryptKey = Environment.GetEnvironmentVariable("AES_ENCRYPTKEY");
                AddedTime = Environment.GetEnvironmentVariable("ADDED_TIME");

            }
            else
            {
                region = RegionEndpoint.GetBySystemName("ap-southeast-1");
                lambdaVersion = "0";
                aesEncryptKey = "04b13d01-e97f-4050-b236-5c2cba20bf8d";
                intouchSvcUrl = "http://apac2.api.capillarytech.com";
                //username = "demo.shell.mly.10208771.01";
                // username = "shell.{0}.1";
                // password = "7d235ffc623ed667ccda39e92930040c";
                AddedTime = "50";

            }
            // _crmService = new IntouchService(intouchSvcUrl, username, password, lambdaVersion);
            _dynamoService = new DynamoService();

        }
        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse generateDynamicQR(APIGatewayProxyRequest request, ILambdaContext context)
        {
            request.Headers.TryGetValue("X-Cap-OrgId", out string OrgID);
            request.Headers.TryGetValue("X-Cap-Environment", out string Environment);
            // var crmUserName = string.Format(username, OrgID);
            string requestId = Guid.NewGuid().ToString("N");
            string response = string.Empty;
            ErrorResponse errorResponse = new ErrorResponse();
            GenerateQRResponse qrResponse = new GenerateQRResponse();
            var mobileKeysJson = _dynamoService.GetMobileConfigsAsync(requestId, OrgID, Environment).Result;
            if (string.IsNullOrEmpty(mobileKeysJson))
            {
                errorResponse.message = "Org Not Found";
                return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(errorResponse), 401);
            }
            var mobileKeys = JsonConvert.DeserializeObject<DBResponseModel>(mobileKeysJson);
            var crmKeys = mobileKeys.artifacts.Where(c => c.source == "cap_crm").FirstOrDefault();
            if (crmKeys == null)
            {
                errorResponse.message = "cap_crm keys not found for this org";
                return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(errorResponse), 401);
            }
            var username = crmKeys.Keys.Where(c => c.key == "username").FirstOrDefault().value;
            var password = crmKeys.Keys.Where(c => c.key == "password").FirstOrDefault().value;
            if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password))
            {
                errorResponse.message = "credentials not found for this org";
                return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(errorResponse), 401);
            }
            var crmUserName = string.Format(username, OrgID);
            _crmService = new IntouchService(intouchSvcUrl, username, password, lambdaVersion);

            try
            {
                var Auth = Helper.API_Authentication(requestId, request);
                if (Auth != "success")
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(new APIErrorClass { message = Auth }), 401);
                request.Headers.TryGetValue("externalId", out string ExternalId);
                AesEncryption aesEncrypt = new AesEncryption();
                request.Headers.TryGetValue("X-Cap-Profile-Identifier", out string Profile_identifier);
                if (!String.IsNullOrEmpty(Profile_identifier))
                {
                    var customerResponseTask = _crmService.CustomersLookupGetAsync(requestId, Profile_identifier, crmUserName).Result;
                    if (customerResponseTask.errors != null && customerResponseTask.errors.Count > 0)
                    {
                        errorResponse.message = customerResponseTask.errors[0].message;
                        errorResponse.code = 500;
                        return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(errorResponse), 500);
                    }

                    string encryptedPin = string.Empty;
                    Console.WriteLine("RequestId:{0}, encrypted Pin {1}", requestId, encryptedPin);

                    // request.Headers.TryGetValue("X-Cap-Environment", out string Environment);
                    string ConcatString = string.Empty;
                    int timeDifference = 0;
                    string DynamoDbDatetimeValue = _dynamoService.GetQRCodeKeyAsync(requestId, customerResponseTask.cardDetails.Where(x => x.cardNumber.ToLower().StartsWith("d")).Select(y => y.cardNumber).FirstOrDefault() + "_" + OrgID, Environment).Result;
                    TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                    int secondsSinceEpochCurrent = (int)t.TotalSeconds;
                    Console.WriteLine("RequestId:{0}, current Epoch time {1}", requestId, secondsSinceEpochCurrent.ToString());
                    if (!String.IsNullOrEmpty(DynamoDbDatetimeValue))
                        timeDifference = Convert.ToInt32(DynamoDbDatetimeValue) - secondsSinceEpochCurrent;
                    if (!String.IsNullOrEmpty(DynamoDbDatetimeValue) && timeDifference > 0)
                    {
                        Console.WriteLine("RequestId:{0}, Found value in Dynamo DB {1}", requestId, customerResponseTask.cardDetails.Where(x => x.cardNumber.ToLower().StartsWith("d")).Select(y => y.cardNumber).FirstOrDefault());
                        ConcatString = customerResponseTask.cardDetails.Where(x => x.cardNumber.ToLower().StartsWith("d")).Select(y => y.cardNumber).FirstOrDefault() + "|" + DynamoDbDatetimeValue;
                        encryptedPin = aesEncrypt.Encrypt(ConcatString, aesEncryptKey);
                        if (!string.IsNullOrEmpty(encryptedPin))
                        {
                            qrResponse.EncryptedString = encryptedPin;
                            qrResponse.KeyExpiredTime = DynamoDbDatetimeValue;
                            qrResponse.Code = 200;
                            qrResponse.Message = "Success";
                            return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(qrResponse), 200);
                        }
                        else
                        {
                            errorResponse.message = "Encryption Failed";
                            errorResponse.code = 500;
                            return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(errorResponse), 500);
                        }
                    }
                    else
                    {
                        TimeSpan t1 = DateTime.UtcNow.AddSeconds(Convert.ToDouble(AddedTime)) - new DateTime(1970, 1, 1);
                        int secondsSinceEpochExpiry = (int)t1.TotalSeconds;
                        ConcatString = customerResponseTask.cardDetails.Where(x => x.cardNumber.ToLower().StartsWith("d")).Select(y => y.cardNumber).FirstOrDefault() + "|" + secondsSinceEpochExpiry;
                        Console.WriteLine("RequestId:{0} ExpiryTime {1}", requestId, secondsSinceEpochExpiry.ToString());
                        encryptedPin = aesEncrypt.Encrypt(ConcatString, aesEncryptKey);
                        if (!string.IsNullOrEmpty(encryptedPin))
                        {
                            Console.WriteLine("RequestId:{0},Value Not Found in Dynamo DB {1}", requestId, customerResponseTask.cardDetails.Where(x => x.cardNumber.ToLower().StartsWith("d")).Select(y => y.cardNumber).FirstOrDefault());
                            _dynamoService.PutDynamoQRAsync(requestId, customerResponseTask.cardDetails.Where(x => x.cardNumber.ToLower().StartsWith("d")).Select(y => y.cardNumber).FirstOrDefault() + "_" + OrgID, secondsSinceEpochExpiry.ToString()).Wait();
                            qrResponse.EncryptedString = encryptedPin;
                            qrResponse.KeyExpiredTime = secondsSinceEpochExpiry.ToString();
                            qrResponse.Code = 200;
                            qrResponse.Message = "Success";
                            return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(qrResponse), 200);
                        }
                        else
                        {
                            errorResponse.message = "Encryption Failed";
                            errorResponse.code = 500;
                            return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(errorResponse), 500);
                        }
                    }
                }
                else
                {
                    errorResponse.message = "Identifier Not Passed";
                    errorResponse.code = 500;
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(errorResponse), 500);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("RequestId:{0}. Encryption Failed {1}", requestId, ex.Message);
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
