using System;
using System.Collections.Generic;
using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Capillary.ShellProxy.Service;
using Capillary.ShellProxy.Utilities;
using Newtonsoft.Json;
using System.Net.Http;
using Amazon;
using System.Linq;
using Capillary.ShellProxy.Model;
using System.IdentityModel.Tokens.Jwt;
using Capillary.ShellProxy.Model.ProductModel.Response;
using Capillary.ShellProxy.Model.GiftCatalog.Request;
using Capillary.ShellProxy.Model.GiftCatalog.Response;

namespace Capillary.ShellProxy.API
{
    public class FunctionHandlerGiftCatalog
    {
        ICrmService _crmService;
        IDBService _dbService;
        IWrapperAPIService _wrapperAPIService;
        string _crmUsernameFormat;

        public FunctionHandlerGiftCatalog()
        {
            //Environment variables
            OpenTracing.Util.GlobalTracer.Register(NewRelic.OpenTracing.AmazonLambda.LambdaTracer.Instance);
            RegionEndpoint region;
            string lambdaVersion;
            string intouchSvcUrl;
            string wrapperURL;
            string wrapperUserName;
            string wrapperPassword;
            string tillPassword;

            //AWS_REGION is a reserverd env.variable, will be set only while running on AWS environment.
            var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION");

            if (!string.IsNullOrEmpty(awsRegion))
            {
                region = RegionEndpoint.GetBySystemName(awsRegion);
                lambdaVersion = Environment.GetEnvironmentVariable("LAMBDA_VERSION");
                intouchSvcUrl = Environment.GetEnvironmentVariable("CRM_SVC_URL");
                wrapperURL = Environment.GetEnvironmentVariable("CRM_WrapperURL");
                wrapperUserName = Environment.GetEnvironmentVariable("CRM_WrapperUserName");
                wrapperPassword = Environment.GetEnvironmentVariable("CRM_WrapperPassword");
                tillPassword = Environment.GetEnvironmentVariable("CRM_PASSWORD");
                _crmUsernameFormat = Environment.GetEnvironmentVariable("CRM_USERNAME_FORMAT");
            }
            else
            {
                region = RegionEndpoint.GetBySystemName("us-east-1");
                lambdaVersion = "0";
                intouchSvcUrl = "";
                wrapperURL = "";
                wrapperUserName = "";
                wrapperPassword = "";
                tillPassword = "";     
                _crmUsernameFormat = "";
            }
            Console.WriteLine("Environment variables-->region:{0};intouchSvcUrl:{1};wrapperURL:{2};",
                                                      region, intouchSvcUrl, wrapperURL);

            //todo: register services with Container
            _crmService = new IntouchService(intouchSvcUrl, tillPassword, lambdaVersion);
            _dbService = new DynamoService();
            _wrapperAPIService = new WrapperAPIService(wrapperURL, wrapperUserName, wrapperPassword, lambdaVersion);
        }
        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse GiftCatalog(APIGatewayProxyRequest request, ILambdaContext lambdaContext)
        {
            var requestId = string.Empty;

            GiftCatalogResponse giftCatalogResponse = new GiftCatalogResponse();
            GiftCatalogRequest giftcatalogRequest = new GiftCatalogRequest();
            try
            {
                Console.WriteLine("GiftCatalog.Request.Body:{0}", request.Body.Replace(Environment.NewLine, ""));
                giftcatalogRequest = JsonConvert.DeserializeObject<GiftCatalogRequest>(request.Body);
                requestId = string.Format("{0}.{1}.{2}", giftcatalogRequest.customerData[0].customerDataValue, giftcatalogRequest.siteData.siteID, giftcatalogRequest.posData.transactionNumber);
                Console.WriteLine("RequestId:{0}.Request Body:{1}", requestId, JsonConvert.SerializeObject(giftcatalogRequest));

                //Store Specific Authentication verification
                var handler = new JwtSecurityTokenHandler();
                var decodedValue = handler.ReadJwtToken(request.Headers["Authorization"].ToString().Split(' ')[1]).Payload;
                if(decodedValue == null)
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, "{\"message\": \"Access token is wrong\"}");
                Console.WriteLine("RequestId:{0}.Token Decode Data : {1}", requestId, decodedValue["client_id"]);               
                var clientID = _dbService.GetSiteKeysAsync(requestId,giftcatalogRequest.siteData.siteID).Result;
                Console.WriteLine("RequestId:{0}. {1} compare with {2}",requestId, clientID.ToString(), decodedValue["client_id"]);               
                if(decodedValue["client_id"].ToString() != clientID.ToString())
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, "{\"message\": \"Site keys used are wrong\"}");            
                Console.WriteLine("RequestId:{0}.Token validation is success", requestId);
                
                                      
                var crmUsername = string.Format(_crmUsernameFormat, giftcatalogRequest.siteData.siteID);
                (string idName, string idValue, bool isNotInterested) customerIdInfo = Helper.ResolveCustomerDetails(giftcatalogRequest.customerData[0].customerDataType, giftcatalogRequest.customerData[0].customerDataValue);
                Console.WriteLine("RequestId:{0}.Customer not-interested:{1}", requestId, customerIdInfo.isNotInterested);

                //Customer check
                var customerResponse = _crmService.CustomerLookUpAsync(requestId, crmUsername, customerIdInfo.idName, customerIdInfo.idValue).Result;              
                if (customerResponse.errors != null)
                {
                    ErrorResponse error = new ErrorResponse{
                    ResponseCode = customerResponse.errors[0].code,
                    ResponseMessage = customerResponse.errors[0].message
                    };
                    return funcSendResponse(requestId, HttpStatusCode.OK,Mapper.Map(requestId, giftcatalogRequest, null, error, 0.0));
                }
                Console.WriteLine("RequestId:{0}. Customer with {1}={2} found in CRM", requestId, customerIdInfo.idName, customerIdInfo.idValue);
                         
                //Card Active check    
                if (customerIdInfo.idName.Contains("cardnumber"))
                {              
                    var cardDetail = customerResponse.cardDetails.Where(c => c.cardNumber == customerIdInfo.idValue).FirstOrDefault();
                    if (cardDetail == null || cardDetail.statusInfo.status.ToUpper() != "ACTIVE")
                    {
                        ErrorResponse error = new ErrorResponse{
                        ResponseCode = 500,
                        ResponseMessage = "Customer Card is not active in CRM"
                    };
                    return funcSendResponse(requestId, HttpStatusCode.OK,Mapper.Map(requestId, giftcatalogRequest, null, error,customerResponse.pointsSummary.totalAvailablePoints));
                    }
                }

                //no gifts if request is received from mobile number
                if(customerIdInfo.idName.Contains("mobile"))
                    return funcSendResponse(requestId, HttpStatusCode.OK,Mapper.Map(requestId, giftcatalogRequest, null, null,customerResponse.pointsSummary.totalAvailablePoints));


                //Fetch rewards from marvel
                var catalogResponse = _wrapperAPIService.GetRewardsAsync(requestId).Result;
                if(catalogResponse != null && catalogResponse.status.code != 200)
                {
                    ErrorResponse error = new ErrorResponse{
                    ResponseCode = catalogResponse.status.code,
                    ResponseMessage = catalogResponse.status.message
                    };
                    return funcSendResponse(requestId, HttpStatusCode.OK,Mapper.Map(requestId, giftcatalogRequest, null, error,customerResponse.pointsSummary.totalAvailablePoints));
                }
                
                var response = Mapper.Map(requestId, giftcatalogRequest, catalogResponse, null, customerResponse.pointsSummary.totalAvailablePoints);
                  Console.WriteLine("RequestId:{0}. Response : {1}", requestId, response);
                return funcSendResponse(requestId, HttpStatusCode.OK,response);
               
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                Console.WriteLine("RequestId:{0}. Parsing exception occurred.Message : {1}",requestId, e.Message); 
                ErrorResponse error = new ErrorResponse{
                    ResponseCode = 500,
                    ResponseMessage = string.Format("Parsing exception occurred.Message : {0}", e.Message)
                };
                 return funcSendResponse(requestId, HttpStatusCode.BadRequest, Mapper.Map(requestId, giftcatalogRequest, null,error, 0.0));
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}. Unkown error occurred.Message : {1}", requestId, e.Message);
                ErrorResponse error = new ErrorResponse{
                    ResponseCode = 500,
                    ResponseMessage = string.Format("Unkown error occurred.Message : {0}", e.Message)
                };
                return funcSendResponse(requestId, HttpStatusCode.BadRequest, Mapper.Map(requestId, giftcatalogRequest, null, error, 0.0));
            }
        }

        Func<string, HttpStatusCode, string, APIGatewayProxyResponse> funcSendResponse = (requestId, httpStatusCode, body) =>
        {
            Console.WriteLine("RequestId:{0}. Response:{1}", requestId, body.Replace(Environment.NewLine, " "));

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)httpStatusCode,
                Headers = new Dictionary<string, string> { { "content-type", "application/json" },{"INTG-RequestID", requestId} },
                Body = body
            };
        };
    }
}