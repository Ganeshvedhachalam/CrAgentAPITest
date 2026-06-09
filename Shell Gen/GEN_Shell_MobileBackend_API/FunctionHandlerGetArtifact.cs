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
    public class FunctionHandlerGetArtifact
    {
        IDBService _dynamoService;
        public FunctionHandlerGetArtifact()
        {
            _dynamoService = new DynamoService();
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse GetArtifact(APIGatewayProxyRequest request, ILambdaContext context)
        {
            string requestId = Guid.NewGuid().ToString("N");
            try
            {
                //API Authentication and Header validation
                var Auth = Helper.API_Authentication(requestId, request);
                Console.WriteLine("RequestId:{0}. API Authentication message : {1}", requestId, Auth.ToString());
                if (Auth != "success")
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(new APIErrorClass { message = Auth }), 401);


                //Extracting source from query parameter
                if (request.QueryStringParameters == null || request.QueryStringParameters.Count <= 0 || !request.QueryStringParameters.TryGetValue("source", out string a))
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, "{\"message\": \"source field is missing in query parameter\"}", 400);
                request.QueryStringParameters.TryGetValue("source", out string source);


                //Fetch Org details from Headers
                request.Headers.TryGetValue("X-Cap-OrgId", out string OrgID);
                request.Headers.TryGetValue("X-Cap-Environment", out string Environment);
                Console.WriteLine("RequestId:{0}. Requested API keys source : {1} for OrgId : {2} and Environment : {3}", requestId, source, OrgID, Environment);


                //fetch keys from DynamoDB
                var mobileKeysJson = _dynamoService.GetMobileConfigsAsync(requestId, OrgID, Environment).Result;
                if (string.IsNullOrEmpty(mobileKeysJson))
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, "{\"message\": \"Org not found\"}", 401);
                var mobileKeys = JsonConvert.DeserializeObject<DBResponseModel>(mobileKeysJson);
                Console.WriteLine("RequestId:{0}. Mobile Keys : {1}", requestId, mobileKeys.ToString());
                var sourceKeys = mobileKeys.artifacts.Where(c => c.source == source).FirstOrDefault();
                Console.WriteLine("RequestId:{0}. Mobile keys for source {1} are {2}", requestId, source, sourceKeys.ToString());
                if (sourceKeys == null)
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, "{\"message\": \"keys not found for this org and source\"}", 401);

                return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(sourceKeys), 200);


            }
            catch (Exception ex)
            {
                Console.WriteLine("RequestId:{0}. Unknown exception occured with message {1}", requestId, ex.Message);
                return funcSendResponse(requestId, HttpStatusCode.OK, ex.Message, 500);
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
