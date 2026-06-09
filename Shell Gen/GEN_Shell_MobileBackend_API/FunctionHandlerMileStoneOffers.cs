using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Runtime.Internal;
using GEN_Shell_MobileBackend_API.Models;
using GEN_Shell_MobileBackend_API.Models.MileStoneOffersResp;
using GEN_Shell_MobileBackend_API.Services;
using GEN_Shell_MobileBackend_API.Utilities;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GEN_Shell_MobileBackend_API
{
    public class FunctionHandlerMileStoneOffers
    {
        IDBService _dynamoService;
        ICrmService _crmService;
        string lambdaVersion;
        string intouchSvcUrl;

        public FunctionHandlerMileStoneOffers()
        {
            RegionEndpoint region;            
            var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION");
            string username;
            string password;

            if (!string.IsNullOrEmpty(awsRegion))
            {
                region = RegionEndpoint.GetBySystemName(awsRegion);
                lambdaVersion = Environment.GetEnvironmentVariable("LAMBDA_VERSION");
                intouchSvcUrl = Environment.GetEnvironmentVariable("CRM_SVC_URL");                
            }
            else
            {
                region = RegionEndpoint.GetBySystemName("us-east-1");
                lambdaVersion = "0";
                intouchSvcUrl = "https://apac2.api.capillarytech.com";                
            }
            _dynamoService = new DynamoService();            
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse GetMileStoneOffers(APIGatewayProxyRequest request, ILambdaContext context)
        {
            string requestId = Guid.NewGuid().ToString("N");
            MileStoneOffersErrorResp errorResponse = new MileStoneOffersErrorResp();
            Mapper mapper = new Mapper();
            try
            {
                request.Headers.TryGetValue("X-Cap-OrgId", out string OrgID);
                request.Headers.TryGetValue("X-Cap-Environment", out string Environment);
                request.Headers.TryGetValue("X-Cap-Profile-Identifier", out string Profile_identifier);
                string apiParamModeValue = string.Empty;

                if (request.QueryStringParameters != null && request.QueryStringParameters.Count > 0)
                {
                    request.QueryStringParameters.TryGetValue("mode", out apiParamModeValue);
                    if (string.IsNullOrEmpty(apiParamModeValue))
                        apiParamModeValue = "DISCOUNT";
                }                

                //log tracking with mobile
                requestId = requestId + "_" + Profile_identifier;

                //API Authentication and Header validation
                var Auth = Helper.API_Authentication(requestId, request);
                Console.WriteLine("RequestId:{0}. API Authentication message : {1}", requestId, Auth.ToString());
                if (Auth != "success")
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(new APIErrorClass { message = Auth }), 401);


                var mobileKeysJson = _dynamoService.GetMobileConfigsAsync(requestId, OrgID, Environment).Result;
                if (string.IsNullOrEmpty(mobileKeysJson))
                {
                    errorResponse.errorDescription = "Org Not Found"; errorResponse.errorCode = 401;
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(errorResponse), 401);
                }
                var mobileKeys = JsonConvert.DeserializeObject<DBResponseModel>(mobileKeysJson);
                var crmKeys = mobileKeys.artifacts.Where(c => c.source == "cap_crm").FirstOrDefault();
                if (crmKeys == null)
                {
                    errorResponse.errorDescription = "cap_crm keys not found for this org"; errorResponse.errorCode = 401;
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(errorResponse), 401);
                }
                var username = crmKeys.Keys.Where(c => c.key == "username").FirstOrDefault().value;
                var password = crmKeys.Keys.Where(c => c.key == "password").FirstOrDefault().value;
                if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password))
                {
                    errorResponse.errorDescription = "credentials not found for this org"; errorResponse.errorCode = 401;
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(errorResponse), 401);
                }                
                
                _crmService = new IntouchService(intouchSvcUrl, username, password, lambdaVersion);    
                
                var getCustomerResp = _crmService.CustomersLookupGetAsync(requestId, Profile_identifier).Result;
                if (getCustomerResp != null && getCustomerResp.errors == null)
                {
                    var getCustomerPromotionResp = _crmService.GetCustomerPromotionAsync(requestId, getCustomerResp.id, apiParamModeValue.ToUpper()).Result;
                    if (getCustomerPromotionResp == null || (getCustomerPromotionResp.data == null || getCustomerPromotionResp.data.Count == 0))
                    {
                        errorResponse.errorDescription = getCustomerPromotionResp == null ? "All requests failed.Please try again later" :
                            getCustomerPromotionResp.errorCode == 0 ?  "No Offers Available" : getCustomerPromotionResp.message;
                        errorResponse.errorCode = getCustomerPromotionResp == null || getCustomerPromotionResp.errorCode == 0 ? 200 :getCustomerPromotionResp.errorCode ;
                        errorResponse.status = false;
                        return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(errorResponse), 200);
                    }                    
                    var API_EndResponse = mapper.Map(requestId,getCustomerPromotionResp, getCustomerResp.id, intouchSvcUrl,username, password);
                    return funcSendResponse(requestId, HttpStatusCode.OK, API_EndResponse,200);
                }
                else
                {
                    errorResponse.errorDescription = getCustomerResp.errors[0].message;
                    errorResponse.errorCode = getCustomerResp.errors[0].code;
                    errorResponse.status = false;
                    return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(errorResponse), 200);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("RequestId:{0}. Unknown exception occured with message {1}", requestId, ex.Message);
                errorResponse.errorDescription = "Unknown Error.Please try again later";
                errorResponse.errorCode = 500;
                errorResponse.status = false;
                return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(errorResponse), 500);
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









