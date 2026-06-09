using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using GEN_Shell_MobileBackend_API.Models;
using GEN_Shell_MobileBackend_API.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using GEN_Shell_MobileBackend_API.Services;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon;

namespace GEN_Shell_MobileBackend_API
{
    public class FunctionHandlerOnBoardingHash
    {
        IDBService _dynamoService;
        ICrmService _crmService;
        string _digitalCardSeriesCode;
        string _seriesCode;
        public FunctionHandlerOnBoardingHash()
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
                _digitalCardSeriesCode = Environment.GetEnvironmentVariable("DiGITALCARD_SERIES_CODE");
                _seriesCode = Environment.GetEnvironmentVariable("SERIES_CODE");
            }
            else
            {
                region = RegionEndpoint.GetBySystemName("us-east-1");
                lambdaVersion = "0";
                intouchSvcUrl = "https://apac2.api.capillarytech.com";
                username = "demo.shell.mly.10208771.01";
                password = "7d235ffc623ed667ccda39e92930040c";
                _digitalCardSeriesCode = "digitalcard";
                _seriesCode = "BLTEST";

            }
            _dynamoService = new DynamoService();
            _crmService = new IntouchService(intouchSvcUrl, username, password, lambdaVersion);
        }
        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse EncryptAES(APIGatewayProxyRequest request, ILambdaContext context)
        {
            ErrorResponse errorResponse = new ErrorResponse();
            string requestId = Guid.NewGuid().ToString("N");
            OnboardingHashReuest requestAes = new OnboardingHashReuest();
            OnBoardingHashResponse responseHash = new OnBoardingHashResponse();
            string inputString = string.Empty;
            string partnerToken = string.Empty;
            string partnerId = string.Empty;
            try
            {
                //API Authentication
                var Auth = Helper.API_Authentication(requestId, request);
                if (Auth != "success")
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(new APIErrorClass { message = Auth }), 401);
                if (request.QueryStringParameters == null || request.QueryStringParameters.Count <= 0 || !request.QueryStringParameters.TryGetValue("event", out string param))
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, "{\"message\": \"event field is missing in query parameter\"}", 400);
                //Get mobile configurations keys
                request.Headers.TryGetValue("X-Cap-OrgId", out string OrgID);
                request.Headers.TryGetValue("X-Cap-Environment", out string Environment);
                request.Headers.TryGetValue("X-Cap-Profile-Identifier", out string Profile_identifier);
                request.Headers.TryGetValue("Crypto-Type", out string CryptoType);
                var mobileKeysJson = _dynamoService.GetMobileConfigsAsync(requestId, OrgID, Environment).Result;
                if (string.IsNullOrEmpty(mobileKeysJson))
                {
                    errorResponse.message = "Org Not Found";
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(errorResponse), 401);
                }
                var mobileKeys = JsonConvert.DeserializeObject<DBResponseModel>(mobileKeysJson);
                var bonusLinkKeys = mobileKeys.artifacts.Where(c => c.source == "bonuslink").FirstOrDefault();
                if (bonusLinkKeys == null)
                {
                    errorResponse.message = "BonusLink keys not found for this org";
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(errorResponse), 401);
                }
                var partnerTokenDB = bonusLinkKeys.Keys.Where(c => c.key == "partnerToken").FirstOrDefault();
                var partnerID = bonusLinkKeys.Keys.Where(c => c.key == "partnerId").FirstOrDefault();
                if (partnerTokenDB == null)
                {
                    errorResponse.message = "Partner token not configured for this org";
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(errorResponse), 401);
                }
                if (partnerID == null)
                {
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, "{\"message\": \"Partner Id not configured for this org\"}", 401);
                }
                partnerToken = partnerTokenDB.value;

                if (!String.IsNullOrEmpty(Profile_identifier))
                {
                    var customerResponseTask = _crmService.CustomersLookupGetAsync(requestId, Profile_identifier).Result;
                    if (!string.IsNullOrEmpty(CryptoType) && CryptoType.ToLower().Equals("rsa"))
                    {                        
                        var rsaPublicKey = bonusLinkKeys.Keys.Where(c => c.key == "RSA_PublicKey").FirstOrDefault();
                        if (rsaPublicKey == null)
                        {
                            errorResponse.message = "Rsa public key not configured for this org";
                            return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(errorResponse), 401);
                        }
                        Console.WriteLine("Request Id {0} ,RSA_PublicKey Extracted from DB", requestId);
                        if (customerResponseTask.errors == null)
                        {
                            string cardNumber = customerResponseTask.cardDetails.Where(x => x.seriesCode == _digitalCardSeriesCode).Select(y => y.cardNumber).FirstOrDefault();
                            //API core logic
                            OnboardingHashReuest requestRsa = JsonConvert.DeserializeObject<OnboardingHashReuest>(request.Body);
                            Console.WriteLine("Request Id {0} Input request {1}", requestId, requestRsa);
                            if (param == "onboarding")
                            {
                                // Bonuslink expect the mobile number to not include +/6 and it starts with 0
                                Profile_identifier = Profile_identifier.TrimStart('+');
                                Profile_identifier = Profile_identifier.TrimStart('6');
                                inputString = String.Format("uid={0}&idno={1}&idtype={2}&mobileno={3}&name={4}&email={5}&identifier={6}&source={7}&partnertoken={8}", requestRsa.uid, string.Empty, string.Empty, Profile_identifier, requestRsa.name, requestRsa.email, cardNumber, requestRsa.source, partnerToken);
                            }
                            else if (param == "changepin")
                            {
                                string membercardno = customerResponseTask.cardDetails.Where(x => x.seriesCode.ToLower() == _seriesCode.ToLower()).Select(y => y.cardNumber).FirstOrDefault();
                                inputString = String.Format("uid={0}&membercardno={1}&identifier={2}&source={3}&partnertoken={4}", requestRsa.uid, membercardno, cardNumber, requestRsa.source, partnerToken);
                            }
                            else if (param == "changecard")
                            {
                                string membercardno = customerResponseTask.cardDetails.Where(x => x.seriesCode.ToLower() == _seriesCode.ToLower()).Select(y => y.cardNumber).FirstOrDefault();
                                inputString = String.Format("uid={0}&membercardno={1}&identifier={2}&source={3}&partnertoken={4}", requestRsa.uid, membercardno, cardNumber, requestRsa.source, partnerToken);
                            }
                            else
                            {
                                errorResponse.message = "Invalid Parameter or Payload";
                                errorResponse.code = 500;
                                return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(errorResponse), 403);
                            }
                        }
                        else
                        {
                            errorResponse.message = customerResponseTask.errors[0].message;
                            errorResponse.code = customerResponseTask.errors[0].code;
                            return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(errorResponse), 403);
                        }
                        Console.WriteLine("RequestId:{0} Concatenated string {1}", requestId, inputString);
                        string encryptedMessage = RsaCrypto.EncryptDataBatchByte(requestId, rsaPublicKey.value, inputString);
                        Console.WriteLine("RequestId:{0} Encrypted Data: {1}", requestId, encryptedMessage);
                        responseHash.HashedPayload = encryptedMessage;
                        responseHash.PartnerId = partnerID.value;
                        //this needs to be commented before prod
                        //responseHash.inputString = inputString;
                        string response = JsonConvert.SerializeObject(responseHash);
                        Console.WriteLine("RequestId:{0}  Returned response", requestId);
                        
                        return funcSendResponse(requestId, HttpStatusCode.OK, response, 200);
                    }
                    else //AES default
                    {
                        Console.WriteLine("Request Id {0} ,Default AES Encryption", requestId);
                        if (customerResponseTask.errors == null)
                        {
                            string cardNumber = customerResponseTask.cardDetails.Where(x => x.seriesCode == _digitalCardSeriesCode).Select(y => y.cardNumber).FirstOrDefault();

                            Console.WriteLine("Partner Token {0}", partnerToken);
                            //API core logic
                            requestAes = JsonConvert.DeserializeObject<OnboardingHashReuest>(request.Body);                            
                            Console.WriteLine("Request Id {0} Input request {1}", requestId, requestAes);
                            if (param == "onboarding")
                            {
                                // Bonuslink expect the mobile number to not include +/6 and it starts with 0
                                Profile_identifier = Profile_identifier.TrimStart('+');
                                Profile_identifier = Profile_identifier.TrimStart('6');
                                inputString = String.Format("uid={0}&idno={1}&idtype={2}&mobileno={3}&name={4}&email={5}&identifier={6}&source={7}&partnertoken={8}", requestAes.uid, string.Empty, string.Empty, Profile_identifier, requestAes.name, requestAes.email, cardNumber, requestAes.source, partnerToken);
                            }
                            else if (param == "changepin")
                            {
                                string membercardno = customerResponseTask.cardDetails.Where(x => x.seriesCode.ToLower() == _seriesCode.ToLower()).Select(y => y.cardNumber).FirstOrDefault();
                                inputString = String.Format("uid={0}&membercardno={1}&identifier={2}&source={3}&partnertoken={4}", requestAes.uid, membercardno, cardNumber, requestAes.source, partnerToken);
                            }
                            else if (param == "changecard")
                            {
                                string membercardno = customerResponseTask.cardDetails.Where(x => x.seriesCode.ToLower() == _seriesCode.ToLower()).Select(y => y.cardNumber).FirstOrDefault();
                                inputString = String.Format("uid={0}&membercardno={1}&identifier={2}&source={3}&partnertoken={4}", requestAes.uid, membercardno, cardNumber, requestAes.source, partnerToken);
                            }
                            else
                            {
                                errorResponse.message = "Invalid Parameter or Payload";
                                errorResponse.code = 500;
                                return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(errorResponse), 403);
                            }
                        }
                        else
                        {
                            errorResponse.message = customerResponseTask.errors[0].message;
                            errorResponse.code = customerResponseTask.errors[0].code;
                            return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(errorResponse), 403);

                        }


                        Console.WriteLine("Request Id {0} Concatenated string {0}",requestId, inputString);
                        AesEncryption aesEncrypt = new AesEncryption();
                        string encryptedMessage = aesEncrypt.Encrypt(inputString, partnerToken);
                        Console.WriteLine("Request Id {0} Encrypted string {0}", requestId,encryptedMessage);
                        responseHash.HashedPayload = encryptedMessage;
                        responseHash.PartnerId = partnerID.value;
                        //this needs to be commented before prod
                        //responseHash.inputString = inputString;
                        string response = JsonConvert.SerializeObject(responseHash);
                        Console.WriteLine("Request Id {0} Returned response",requestId);
                        return funcSendResponse(requestId, HttpStatusCode.OK, response, 200);


                    }

                }
                else
                {
                    errorResponse.message = "Invalid Parameter or Payload";
                    errorResponse.code = 500;
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(errorResponse), 403);
                    // return funcSendResponse(requestId, HttpStatusCode.BadRequest, "{\"message\": \"Invalid Parameter or Payload\"}", 400);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Request Id {0} , Exception {1}", requestId, ex.Message);
                errorResponse.message = "Invalid Parameter or Payload";
                errorResponse.code = 500;
                return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(errorResponse), 403);
                //return funcSendResponse(requestId, HttpStatusCode.BadRequest, "{\"message\": \"Invalid Profile Identifier\"}", 400);
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
