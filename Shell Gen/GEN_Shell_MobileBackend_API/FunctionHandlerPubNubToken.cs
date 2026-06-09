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
using GEN_Shell_MobileBackend_API.PubNubAPIModel.Request;
using GEN_Shell_MobileBackend_API.PubNubAPIModel.Response;
using System.Net;
using System.Linq;
using PubnubApi;

namespace GEN_Shell_MobileBackend_API
{

    public class FunctionHandlerPubNubToken
    {
        IDBService _dynamoService;
        public FunctionHandlerPubNubToken()
        {
            _dynamoService = new DynamoService();

        }
        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse GenerateToken(APIGatewayProxyRequest request, ILambdaContext context)
        {
            string requestId = Guid.NewGuid().ToString("N");
            int TTL = 0;
            try
            {

                //API Authentication
                var Auth = Helper.API_Authentication(requestId, request);
                if (Auth != "success")
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(new APIErrorClass { message = Auth }), 401);

                //Get mobile configurations keys
                request.Headers.TryGetValue("X-Cap-OrgId", out string OrgID);
                request.Headers.TryGetValue("X-Cap-Environment", out string Environment);
                var mobileKeysJson = _dynamoService.GetMobileConfigsAsync(requestId, OrgID, Environment).Result;
                if (string.IsNullOrEmpty(mobileKeysJson))
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(new APIErrorClass { message = "Org not found" }), 401);
                var mobileKeys = JsonConvert.DeserializeObject<DBResponseModel>(mobileKeysJson);
                var pubNubKeys = mobileKeys.artifacts.Where(c => c.source == "pubnub").FirstOrDefault();
                if (pubNubKeys == null)
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(new APIErrorClass { message = "PubNub keys not found for this org" }), 401);

                var requestBody = JsonConvert.DeserializeObject<PubNubAPIRequest>(request.Body.Replace(System.Environment.NewLine, ""));
                if (requestBody == null)
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(new APIErrorClass { message = "Request Body is null" }), 403);

                PNConfiguration configs = new PNConfiguration(userId: requestBody.AuthorizeUserId);
                configs.PublishKey = pubNubKeys.Keys.Where(c => c.key == "PublishKey").FirstOrDefault().value;
                configs.SubscribeKey = pubNubKeys.Keys.Where(c => c.key == "SubscribeKey").FirstOrDefault().value;
                configs.SecretKey = pubNubKeys.Keys.Where(c => c.key == "SecretKey").FirstOrDefault().value;
                TTL = Convert.ToInt32(pubNubKeys.Keys.Where(c => c.key == "TTL").FirstOrDefault().value);

                Pubnub pubnub = new Pubnub(configs);
                var grantTokenResponse = pubnub.GrantToken().TTL(TTL)
                                                             //.AuthorizedUserId(requestBody.AuthorizeUserId)
                                                             .AuthorizedUuid(requestBody.AuthorizeUserId)
                                                             .Resources(new PNTokenResources
                                                             {
                                                                Channels = new Dictionary<string, PNTokenAuthValues>{
                                                                    { requestBody.ChannelName, new PNTokenAuthValues() { Read = true, Write = true } }
                                                                }
                                                                //  Spaces = new Dictionary<string, PNTokenAuthValues>{
                                                                //     { requestBody.ChannelName, new PNTokenAuthValues() { Read = true, Write = true } }
                                                                // }
                                                             })
                                                             .ExecuteAsync().Result;
                PNStatus grantTokenStatus = grantTokenResponse.Status;
                if (!grantTokenStatus.Error && grantTokenResponse != null)
                {
                    Console.WriteLine("RequestId:{0}. PubNub Token : {1}", requestId, grantTokenResponse.Result.Token);
                    var tokenDetails = pubnub.ParseToken(grantTokenResponse.Result.Token);
                    Console.WriteLine("RequestId:{0}. PubNub Token Details : {1}", requestId, JsonConvert.SerializeObject(tokenDetails));
                    return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(new PubNubAPIResponse { PNArtifact = grantTokenResponse.Result.Token, PNSubscriberKey = configs.SubscribeKey }, Formatting.None), 200);
                }
                Console.WriteLine("RequestId:{0}. PubNub API Error message : {1}", request, pubnub.JsonPluggableLibrary.SerializeToJsonString(grantTokenStatus));
                return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(new APIErrorClass { message = grantTokenResponse.Status.ErrorData.Information }), 200);

            }
            catch (Exception ex)
            {
                Console.WriteLine("RequestId:{0}. Unknown exception occured with message {1}", requestId, ex.Message);
                return funcSendResponse(requestId, HttpStatusCode.InternalServerError, JsonConvert.SerializeObject(new APIErrorClass { message = "Unknown Error" }), 500);
            }
        }
        Func<string, HttpStatusCode, string, int, APIGatewayProxyResponse> funcSendResponse = (requestId, httpStatusCode, body, returnCode) =>
        {
            Console.WriteLine("RequestId:{0}. Response:{1}", requestId, body.Replace(System.Environment.NewLine, " "));
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)httpStatusCode,
                Headers = new Dictionary<string, string> { { "content-type", "application/json" }, { "INTG-RequestID", requestId } },
                Body = body
            };
        };
    }

}
