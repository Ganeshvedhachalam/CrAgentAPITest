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
using System.Globalization;
using Capillary.ShellProxy.Model.CustomerAddModel.Request;
using Capillary.ShellProxy.Model.CustomerAddModel.Response;

namespace GEN_Shell_MobileBackend_API
{
    public class FunctionHandlerVocSurvey
    {
        IDBService _dynamoService;
        ICrmService _crmService;
        VocService _vocSurveyService;
        string lambdaVersion;
        string intouchSvcUrl;
        string smgServiceUrl;

        public FunctionHandlerVocSurvey()
        {
            RegionEndpoint region;
            var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION");

            if (!string.IsNullOrEmpty(awsRegion))
            {
                region = RegionEndpoint.GetBySystemName(awsRegion);
            }
            else
            {
                region = RegionEndpoint.GetBySystemName("us-east-1");
            }
            _dynamoService = new DynamoService();
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse VocSurvey(APIGatewayProxyRequest request, ILambdaContext context)
        {
            string requestId = Guid.NewGuid().ToString("N");
            VocSurveyResponse vocSurveyResponse = new VocSurveyResponse();
            Mapper mapper = new Mapper();
            try
            {
                request.Headers.TryGetValue("X-Cap-OrgId", out string OrgID);
                request.Headers.TryGetValue("X-Cap-Environment", out string Environment);
                request.Headers.TryGetValue("X-Cap-VocSurveyFlag", out string isVocSurveyFlagUpdate);
                request.Headers.TryGetValue("X-Cap-Profile-SmgSurvey", out string isVocSmgSurveyUpdate);
                request.Headers.TryGetValue("reward", out string isReward);

                Console.WriteLine("Header info -> X-Cap-Profile-SmgSurvey:{0}, X-Cap-VocSurveyFlag:{1}", isVocSmgSurveyUpdate,isVocSurveyFlagUpdate);

                requestId = OrgID + "-" + requestId;
                //API Authentication
                if (!request.Headers.TryGetValue("X-Cap-APIKey", out string API_Key))
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(new APIErrorClass { message = "X-Cap-APIKey is missing in headers" }), 401);
                var APIKey = _dynamoService.GetAPIAccessKeyAsync(requestId, OrgID, Environment).Result;
                if (string.IsNullOrEmpty(APIKey))
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(new APIErrorClass { message = "API Key is either inactive or not configured for the Org" }), 401);
                if (API_Key.ToUpper() != APIKey.ToUpper())
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(new APIErrorClass { message = "API key sent is wrong" }), 401);                

                string apiParamModeValue = string.Empty;

                var mobileKeysJson = _dynamoService.GetMobileConfigsAsync(requestId, OrgID, Environment).Result;
                if (string.IsNullOrEmpty(mobileKeysJson))
                {
                    vocSurveyResponse.status = new StatusInformation
                    {
                        success = false,
                        code = 1003,
                        message = "Org Not Found",
                        total = "0",
                        success_count = "0",
                        requestId = requestId
                    };
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(vocSurveyResponse), 400);
                }
                var mobileKeys = JsonConvert.DeserializeObject<DBResponseModel>(mobileKeysJson);
                var crmKeys = mobileKeys.artifacts.Where(c => c.source == "cap_crm").FirstOrDefault();
                if (crmKeys == null)
                {
                    vocSurveyResponse.status = new StatusInformation
                    {
                        success = false,
                        code = 1004,
                        message = "cap_crm keys not found for this org",
                        total = "0",
                        success_count = "0",
                        requestId = requestId
                    };
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(vocSurveyResponse), 400);
                }
                var username = crmKeys.Keys.Where(c => c.key == "username").FirstOrDefault().value;
                var password = crmKeys.Keys.Where(c => c.key == "password").FirstOrDefault().value;
                if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password))
                {
                    vocSurveyResponse.status = new StatusInformation
                    {
                        success = false,
                        code = 1005,
                        message = "credentials not found for this org",
                        total = "0",
                        success_count = "0",
                        requestId = requestId
                    };
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(vocSurveyResponse), 400);
                }
                var integrationKeys = mobileKeys.artifacts.Where(c => c.source == "integrations").FirstOrDefault();
                if (integrationKeys == null)
                {
                    vocSurveyResponse.status = new StatusInformation
                    {
                        success = false,
                        code = 1006,
                        message = "integrations keys not found for this org",
                        total = "0",
                        success_count = "0",
                        requestId = requestId
                    };
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(vocSurveyResponse), 400);
                }
                 var smgServiceUrlKey = integrationKeys.Keys.Where(c => c.key == "VOC_SMG_Api").FirstOrDefault();
                 intouchSvcUrl = Constants.EndpointIntouchSvcUrl;
                 lambdaVersion = Constants.LambdaVersion;
                _crmService = new IntouchService(intouchSvcUrl, username, password, lambdaVersion);

                //Webhook is called 
                if ("true".Equals(isVocSurveyFlagUpdate, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("RequestId:{0}. event triggered by TransactionAddWebhook.", requestId);
                    var vocFlagUpdtTxnReq = JsonConvert.DeserializeObject<TxnAddedRequest>(request.Body);
                    if(!vocFlagUpdtTxnReq.EventName.Equals("transactionAdded", StringComparison.OrdinalIgnoreCase) || !"regular".Equals(vocFlagUpdtTxnReq.Data.BillType,StringComparison.OrdinalIgnoreCase)){
                        vocSurveyResponse.status = new StatusInformation
                        {
                            success = false,
                            code = 1007, 
                            message = "not a sale transaction added event",
                            total = "0",
                            success_count = "0",
                            requestId = requestId
                        };
                        return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(vocSurveyResponse), 1007);
                    }
                    var mobileNumber = string.Empty;
                    try{
                        mobileNumber = vocFlagUpdtTxnReq.Data.CustomerIdentifiers.Instore.Mobile;
                    }
                    catch(Exception e){
                        Console.WriteLine("exception occurred while fetching mobile number : {0}", e.Message);
                    }
                    if(string.IsNullOrEmpty(mobileNumber)){
                        vocSurveyResponse.status = new StatusInformation
                        {
                            success = false,
                            code = 1008,
                            message = "mobile number is not found",
                            total = "0",
                            success_count = "0",
                            requestId = requestId
                        };
                        return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(vocSurveyResponse), 1008);
                    }
                    // MobileNumber = vocFlagUpdtTxnReq.Data.CustomerIdentifiers.Instore.Mobile;
                    var getCustomerResp = _crmService.CustomersLookupGetAsync(requestId, mobileNumber, Constants.embed, String.Empty).Result;
                    var surveyIntervalDaysStr = integrationKeys.Keys.Where(c => c.key == "VOC_SurveyIntervalDays").Select(y => y.value).FirstOrDefault();
                    if (surveyIntervalDaysStr == null)
                    {
                        vocSurveyResponse.status = new StatusInformation
                        {
                            success = false,
                            code = 1009,
                            message = "survey interval not found for this org",
                            total = "0",
                            success_count = "0",
                            requestId = requestId
                        };
                        return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(vocSurveyResponse), 1009);
                    }
                    var surveyIntervalDays = Convert.ToDouble(surveyIntervalDaysStr);

                    if (getCustomerResp != null && getCustomerResp.errors == null)
                    {
                        var optOut = getCustomerResp.subscriptionInfo.subscriptions.Where(x => x.sourceName.Equals("WEB_ENGAGE", StringComparison.OrdinalIgnoreCase))
                        .Where(x => x.channel.Equals("IOS", StringComparison.OrdinalIgnoreCase) || x.channel.Equals("ANDROID", StringComparison.OrdinalIgnoreCase))
                        .Where(x => x.type.Equals("OPTOUT", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                        if(optOut !=null){
                            {
                                vocSurveyResponse.status = new StatusInformation
                                {
                                    success = false,
                                    code = 1001,
                                    message = "customer is marked as OPTOUT",
                                    total = "0",
                                    success_count = "0",
                                    requestId = null
                                };
                                return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(vocSurveyResponse), 1001);
                            }
                        }
                        // foreach (var subscription in getCustomerResp.subscriptionInfo.subscriptions)
                        // {
                        //     if (subscription.sourceName.Equals("WEB_ENGAGE", StringComparison.OrdinalIgnoreCase)
                        //     && (subscription.channel.Equals("IOS", StringComparison.OrdinalIgnoreCase) || subscription.channel.Equals("ANDROID", StringComparison.OrdinalIgnoreCase))
                        //     && subscription.type.Equals("OPTOUT", StringComparison.OrdinalIgnoreCase)
                        //     )
                        //     {
                        //         vocSurveyResponse.status = new StatusInformation
                        //         {
                        //             success = false,
                        //             code = 1001,
                        //             message = "customer is marked as OPTOUT",
                        //             total = "0",
                        //             success_count = "0",
                        //             requestId = null
                        //         };
                        //         return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(vocSurveyResponse), 200);
                        //     }
                        // }
                        DateTime surveyDate = DateTime.Now; // not nullable value Type
                        bool surveyDateFlag = false;
                        string surveyFlag = null;
                        
                        var inStoreflag = getCustomerResp.profiles.Where( p => p.source.Equals("INSTORE", StringComparison.OrdinalIgnoreCase))
                        .Where(p=> p.Fields.Any(item => item.Key.Equals("survey_flag", StringComparison.OrdinalIgnoreCase))).Select( p => p.Fields["survey_flag"]).FirstOrDefault();
                        surveyFlag = inStoreflag;

                        var inStoreSurveyDate = getCustomerResp.profiles.Where( p => p.source.Equals("INSTORE", StringComparison.OrdinalIgnoreCase))
                        .Where(p=> p.Fields.Any(item => item.Key.Equals("last_survey_date", StringComparison.OrdinalIgnoreCase))).Select( p => p.Fields["last_survey_date"]).FirstOrDefault();

                        if (inStoreSurveyDate != null)
                            {
                                surveyDateFlag = true;
                                surveyDate = DateTime.ParseExact(inStoreSurveyDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                            }
                        // foreach (var profile in getCustomerResp.profiles)
                        // {
                        //     if (profile.source.Equals("INSTORE", StringComparison.OrdinalIgnoreCase))
                        //     {
                        //         foreach (var item in profile.Fields)
                        //         {
                        //             if (item.Key.Equals("last_survey_date", StringComparison.OrdinalIgnoreCase))
                        //             {
                        //                 if (String.IsNullOrEmpty(item.Value)) break;
                        //                 surveyDateFlag = true;
                        //                 surveyDate = DateTime.ParseExact(item.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        //             }
                        //             if (item.Key.Equals("survey_flag", StringComparison.OrdinalIgnoreCase))
                        //             {
                        //                 surveyFlag = item.Value;
                        //             }
                        //         }
                        //         break; // no need to further search
                        //     }
                        // }
                        if (!surveyDateFlag && "FALSE".Equals(surveyFlag, StringComparison.OrdinalIgnoreCase))
                        {
                            vocSurveyResponse.status = new StatusInformation
                            {
                                success = false,
                                code = 1002,
                                message = "last-survey-date is not available",
                                total = "0",
                                success_count = "0",
                                requestId = null
                            };
                            //this
                            return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(vocSurveyResponse), 1002);
                        }
                        CustomerAddRequest customerAddRequest = mapper.Map(surveyFlag,surveyDate,surveyIntervalDays);
                        var apiEndResponse = _crmService.CustomerAddAsync(requestId, getCustomerResp.id, Constants.InstoreSource, customerAddRequest).Result;
                        if (apiEndResponse.createdId > 0)
                        {
                            vocSurveyResponse.status = new StatusInformation
                            {
                                success = true,
                                code = 200,
                                message = "success",
                                total = "1",
                                success_count = "1",
                                requestId = requestId
                            };
                            return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(vocSurveyResponse), 200);
                        }
                        else
                        {
                            vocSurveyResponse.status = new StatusInformation
                            {
                                success = false,
                                code = 1011,
                                message = "unable to update",
                                total = "0",
                                success_count = "0",
                                requestId = null
                            };
                            return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(vocSurveyResponse), 1011);
                        }



                    }
                    else
                    {
                        vocSurveyResponse.status = new StatusInformation
                        {
                            success = false,
                            code = getCustomerResp.errors[0].code,
                            message = getCustomerResp.errors[0].message,
                            total = "0",
                            success_count = "0",
                            requestId = requestId
                        };
                        return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(vocSurveyResponse), getCustomerResp.errors[0].code);
                    }
                }
                //Mobile team is calling this api
                else if ("true".Equals(isVocSmgSurveyUpdate, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("RequestId:{0}. event triggered by MobileAppApi", requestId);

                    if (smgServiceUrlKey==null){
                        vocSurveyResponse.status = new StatusInformation
                            {
                                success = false,
                                code = 1010,
                                message = "VOC_SMG_Api key is missing",
                                total = "0",
                                success_count = "0",
                                requestId = null
                            };
                            return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(vocSurveyResponse), 400);
                    }
                    var behavioralApiUrlKey = integrationKeys.Keys.Where(c => c.key == "VOC_Behavioral_Api").FirstOrDefault();
                    var behavioralEventNameKey = integrationKeys.Keys.Where(c => c.key == "VOC_Behavioral_Event_Name").FirstOrDefault();                    
                    var xApiKey = integrationKeys.Keys.Where(c => c.key == "VOC_SMG_Api_Header_Key").FirstOrDefault();
                    if (behavioralApiUrlKey==null){
                        vocSurveyResponse.status = new StatusInformation
                            {
                                success = false,
                                code = 1014,
                                message = "VOC_Behavioral_Api key is missing",
                                total = "0",
                                success_count = "0",
                                requestId = null
                            };
                            return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(vocSurveyResponse), 400);
                    }
                    if (behavioralEventNameKey==null){
                        vocSurveyResponse.status = new StatusInformation
                            {
                                success = false,
                                code = 1015,
                                message = "VOC_Behavioral_Event_Name key is missing",
                                total = "0",
                                success_count = "0",
                                requestId = null
                            };
                            return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(vocSurveyResponse), 400);
                    }
                    if (xApiKey==null){
                        vocSurveyResponse.status = new StatusInformation
                            {
                                success = false,
                                code = 1016,
                                message = "X-API-KEY key is missing",
                                total = "0",
                                success_count = "0",
                                requestId = null
                            };
                            return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(vocSurveyResponse), 400);
                    }
                    var behavioralApiUrl = behavioralApiUrlKey.value;
                    var behavioralEventName = behavioralEventNameKey.value;
                    request.Headers.TryGetValue("X-Cap-Profile-Identifier", out string Profile_identifier);
                    var getCustomerResp = _crmService.CustomersLookupGetAsync(requestId, Profile_identifier, Constants.embed, String.Empty).Result;
                    smgServiceUrl = smgServiceUrlKey.value;
                    var smgRequest = JsonConvert.DeserializeObject<SmgSurveyRequest>(request.Body);
                    smgRequest = mapper.Map(smgRequest);
                    Console.WriteLine("RequestId:{0}. smgSurveyUpdated Request body:{1}", requestId,
                                                JsonConvert.SerializeObject(smgRequest));
                    Dictionary<String,String> smgHeaders = new Dictionary<string, string>();
                    smgHeaders.Add(Constants.xApiKeyHeader,xApiKey.value);
                    _vocSurveyService = new VocSurveyService(smgServiceUrl,smgHeaders);
                    var Smg_API_EndResponse = _vocSurveyService.smgSurveyFeedbackAsync(requestId, smgRequest).Result;
                    if (Smg_API_EndResponse.results == null || !isAnyPropertiesNotNull(Smg_API_EndResponse.results))
                    {
                        if ("true".Equals(isReward, StringComparison.OrdinalIgnoreCase)){
                            _vocSurveyService = new VocSurveyService(behavioralApiUrl);
                            VocBehavioralEventRequest vocBehavioralEventRequest =  mapper.Map(Profile_identifier,behavioralEventName);
                            var behavioralEventApiResponse = _vocSurveyService.behavioralEventTriggerSmgAsync(requestId,vocBehavioralEventRequest);
                        }
                        CustomerAddRequest customerAddRequest = mapper.Map();
                        var API_EndResponse = _crmService.CustomerAddAsync(requestId, getCustomerResp.id, Constants.InstoreSource, customerAddRequest).Result;
                        if (API_EndResponse.createdId > 0)
                        {
                            vocSurveyResponse.status = new StatusInformation
                            {
                                success = true,
                                code = 200,
                                message = "success",
                                total = "1",
                                success_count = "1",
                                requestId = requestId
                            };
                            return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(vocSurveyResponse), 200);
                        }
                        else
                        {
                            vocSurveyResponse.status = new StatusInformation
                            {
                                success = false,
                                code = 1011,
                                message = "unable to update",
                                total = "0",
                                success_count = "0",
                                requestId = null
                            };
                            return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(vocSurveyResponse), 1011);
                        }
                    }
                    else
                    {

                        vocSurveyResponse.status = new StatusInformation
                        {
                            success = false,
                            code = 1012,
                            message = "error occured at smg api",
                            total = "0",
                            success_count = "0",
                            requestId = null
                        };
                        return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(vocSurveyResponse), 1012);
                    }


                }
                else
                {
                    Console.WriteLine("RequestId:{0}. no voc-survey header found in request", requestId);
                    vocSurveyResponse.status = new StatusInformation
                    {
                        success = false,
                        code = 1013,
                        message = "no voc survey header found in request",
                        total = "0",
                        success_count = "0",
                        requestId = null
                    };
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(vocSurveyResponse), 400);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("RequestId:{0}. Unknown exception occured with message {1}", requestId, ex.Message);
                vocSurveyResponse.status = new StatusInformation
                {
                    success = false,
                    code = 500,
                    message = "Unknown Error.Please try again later",
                    total = "0",
                    success_count = "0",
                    requestId = requestId
                };
                return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(vocSurveyResponse), 500);
            }
        }


        Func<string, HttpStatusCode, string, int, APIGatewayProxyResponse> funcSendResponse = (requestId, httpStatusCode, body, returnCode) =>
            {
                Console.WriteLine("RequestId:{0}. Response:{1}. HttpStatusCode:{2}. ReturnCode:{3}", requestId, body.Replace(Environment.NewLine, " "),httpStatusCode,returnCode);
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)httpStatusCode,
                    Headers = new Dictionary<string, string> { { "content-type", "application/json" }, { "INTG-RequestID", requestId } },
                    Body = body
                };
            };
        static bool isAnyPropertiesNotNull(Object myObject){
        return myObject.GetType()
                 .GetProperties() //get all properties on object
                 .Select(pi => pi.GetValue(myObject)) //get value for the property
                 .Any(value => value != null);
    }


    }






}









