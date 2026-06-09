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
using System.Text;

namespace GEN_Shell_MobileBackend_API
{

    public class FunctionHandlerGetPointBonusLink
    {
        ICrmService _crmService;
        IDBService _dynamoService;
        IBonusLinkService _bonuslinkService;
        string seriesCode;
        public FunctionHandlerGetPointBonusLink()
        {
            RegionEndpoint region;
            string lambdaVersion;
            string intouchSvcUrl;
            string username;
            string password;
            string bonuslinkSvcUrl;
            var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION");

            if (!string.IsNullOrEmpty(awsRegion))
            {

                region = RegionEndpoint.GetBySystemName(awsRegion);
                lambdaVersion = Environment.GetEnvironmentVariable("LAMBDA_VERSION");
                intouchSvcUrl = Environment.GetEnvironmentVariable("CRM_SVC_URL");
                username = Environment.GetEnvironmentVariable("CRM_SVC_USERNAME");
                password = Environment.GetEnvironmentVariable("CRM_PASSWORD");
                seriesCode = Environment.GetEnvironmentVariable("SERIES_CODE");
                bonuslinkSvcUrl = Environment.GetEnvironmentVariable("BONUSLINK_SVC_URL");
            }
            else
            {
                region = RegionEndpoint.GetBySystemName("us-east-1");
                lambdaVersion = "0";
                intouchSvcUrl = "http://apac2.api.capillarytech.com";
                username = "demo.shell.mly.10208771.01";
                password = "7d235ffc623ed667ccda39e92930040c";
                seriesCode = "BLTEST";
                bonuslinkSvcUrl = "http://211.25.202.188:8085/CommonV2.svc";
            }
            _crmService = new IntouchService(intouchSvcUrl, username, password, lambdaVersion);
            _bonuslinkService = new BonusLinkService(bonuslinkSvcUrl);
            _dynamoService = new DynamoService();

        }
        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse GetPointBonusLink(APIGatewayProxyRequest request, ILambdaContext context)
        {
            CapillaryMemberShipResponse outResponse = new CapillaryMemberShipResponse();
            string requestId = Guid.NewGuid().ToString("N");
            string aesEncryptKey = string.Empty;
            ErrorResponse errorResponse = new ErrorResponse();
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

            string bonuslinkApiKey = bonusLinkKeys.Keys.Where(c => c.key == "bonuslinkApikey").Select(x=>x.value).FirstOrDefault().ToString();
            Console.WriteLine("Bonuslink Key {0}", bonuslinkApiKey);
            string signatureSecret = bonusLinkKeys.Keys.Where(c => c.key == "signatureSecret").Select(x=>x.value).FirstOrDefault().ToString();
            Console.WriteLine("Signature Secret {0}", signatureSecret);

            if (aesKey_DB == null)
                return funcSendResponse(requestId, HttpStatusCode.Unauthorized, "{\"message\": \"Encryption key not configured for this org\"}", 401);
            aesEncryptKey = aesKey_DB.value;
            Console.WriteLine("Encryption key {0}", aesEncryptKey);
            var customerLookUpResponseTask = _crmService.CustomersLookupGetAsync(requestId, Profile_identifier).Result;
            if (customerLookUpResponseTask.errors == null)
            {
                string cardNumber = customerLookUpResponseTask.cardDetails.Where(x => x.seriesCode == seriesCode).Select(y => y.cardNumber).FirstOrDefault();
                //call get card 
                // var customerResponseTask = _crmService.CardDetailsGetAsync(requestId, cardNumber).Result;
                if (!String.IsNullOrEmpty(cardNumber))
                {
                    //BonusLink Logic to Be Implemented
                    string TransactionSignature = cardNumber + signatureSecret;
                    Helper helper = new Helper();
                    string membershipSignatureHashed = helper.GenerateSHA512(requestId, TransactionSignature);
                    MembershipCheckRequest MembershipRequest = new MembershipCheckRequest();
                    MembershipRequest.CardNumber = cardNumber;
                    var membershipCheckResponse = _bonuslinkService.MemberShipCheckAsync(requestId, MembershipRequest, bonuslinkApiKey, membershipSignatureHashed).Result;
                    
                    if (membershipCheckResponse != null && membershipCheckResponse.Records.Count >0)
                    {
                        
                        string availablePoints = membershipCheckResponse.Records.Where(x => x.CardNumber == cardNumber).Select(y => y.AvailablePoints).FirstOrDefault();
                        outResponse.Success = true;
                        outResponse.AvailablePoints = Convert.ToInt32(availablePoints.Split('.').FirstOrDefault());
                        string response = JsonConvert.SerializeObject(outResponse);
                        Console.WriteLine("RequestId {0} Returned response : {1}", requestId, response);
                        return funcSendResponse(requestId, HttpStatusCode.OK, response, 200);
                    }
                    else
                    {
                        outResponse.Success = false;
                        outResponse.AvailablePoints = 0;
                        outResponse.ErrorMessage = "Error in Bonuslink MemberCheck";
                        //outResponse.ErrorMessage = membershipCheckResponse.ResultInfo.ErrorMessages[0].ToString();
                        //outResponse.ErrorCode = Convert.ToInt32(membershipCheckResponse.ResultInfo.ErrorCodes[0]);
                        string response = JsonConvert.SerializeObject(outResponse);
                        Console.WriteLine("RequestId {0} Returned response : {1}", requestId, response);
                        return funcSendResponse(requestId, HttpStatusCode.OK, response, 200);
                    }
                }
                else
                {
                    outResponse.Success = false;
                    outResponse.AvailablePoints = 0;
                    outResponse.ErrorMessage = "Card Number Not Found";
                    //errorResponse.message = "No Card Number";
                    //errorResponse.code = 500;
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(outResponse), 500);
                }
            }

            else
            {
                outResponse.Success = false;
                outResponse.AvailablePoints = 0;
                outResponse.ErrorMessage = customerLookUpResponseTask.errors[0].message;
                //errorResponse.message = customerLookUpResponseTask.errors[0].message;
                //errorResponse.code = customerLookUpResponseTask.errors[0].code;
                return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(outResponse), 500);
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
