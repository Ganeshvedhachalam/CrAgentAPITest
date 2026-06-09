using System;
using System.Collections.Generic;
using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Capillary.ShellProxy.Service;
using Capillary.ShellProxy.Utilities;
using Newtonsoft.Json;
using Amazon;
using System.Linq;
using Capillary.ShellProxy.Model.CouponModel;
using Capillary.ShellProxy.Model.OffersModel.Request;
using Capillary.ShellProxy.Model.IssueReward.Request;
using Capillary.ShellProxy.Model.IssueReward.Response;
using Capillary.ShellProxy.Model;
using System.Diagnostics;
using Capillary.ShellProxy.Model.CartModel.Response;
using Capillary.ShellProxy.Model.CustomerModel.Response;
using Capillary.ShellProxy.Model.PromotionDeailsModel.Response;
using NewRelic.OpenTracing.AmazonLambda;
using System.IdentityModel.Tokens.Jwt;
using Capillary.ShellProxy.Model.OffersModel.Response;

namespace Capillary.ShellProxy.API
{
    public class FunctionHandlerPromoOffers
    {
        ICrmService _crmService;
        IDBService _dbService;
        IWrapperAPIService _wrapperAPIService;

        IEncryptionService _encryptionService;
        string _offerMessage;
        string _crmUsernameFormat;
        string _aesTokenValue;
        string _masterClientIdKey;
        string isDynamicQrActive;
        public FunctionHandlerPromoOffers()
        {
            //Environment variables
            OpenTracing.Util.GlobalTracer.Register(NewRelic.OpenTracing.AmazonLambda.LambdaTracer.Instance);
            RegionEndpoint region;
            string lambdaVersion;
            string intouchSvcUrl;
            string tillPassword;
            string tenderTableName;
            string locationTableName;

            //AWS_REGION is a reserverd env.variable, will be set only while running on AWS environment.
            var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION");

            if (!string.IsNullOrEmpty(awsRegion))
            {
                region = RegionEndpoint.GetBySystemName(awsRegion);
                lambdaVersion = Environment.GetEnvironmentVariable("LAMBDA_VERSION");
                intouchSvcUrl = Environment.GetEnvironmentVariable("CRM_SVC_URL");
                tillPassword = Environment.GetEnvironmentVariable("CRM_PASSWORD");
                _offerMessage = Environment.GetEnvironmentVariable("CRM_OFFERMESSAGE");
                _crmUsernameFormat = Environment.GetEnvironmentVariable("CRM_USERNAME_FORMAT");
                tenderTableName = Environment.GetEnvironmentVariable("Tender_TableName");
                locationTableName = Environment.GetEnvironmentVariable("Location_TableName");
                _aesTokenValue = Environment.GetEnvironmentVariable("AES_TOKEN");
                _masterClientIdKey = Environment.GetEnvironmentVariable("Master_ClientId");
                isDynamicQrActive = Environment.GetEnvironmentVariable("IS_DYNAMIC_QR_ACTIVE");
            }
            else
            {
                region = RegionEndpoint.GetBySystemName("us-east-1");
                lambdaVersion = "12";
                intouchSvcUrl = "https://apac2.api.capillarytech.com";
                tillPassword = "";
                _offerMessage = "Redeem your Shell GO+ points via the_Shell Asia App for more savings!";
                _crmUsernameFormat = "demo.shell.sg.{0}.1";
                tenderTableName = "ShellTenders";
                locationTableName = "ShellLocations";
                _aesTokenValue = "";
                _masterClientIdKey = "";
                isDynamicQrActive = "false";
            }

            Console.WriteLine("Environment variables-->region:{0};intouchSvcUrl:{1}",
                                                        region, intouchSvcUrl);

            //todo: register services with Container
            _crmService = new IntouchService(intouchSvcUrl, tillPassword, lambdaVersion);
            _dbService = new DynamoService(tenderTableName, locationTableName);
            _encryptionService = new AesEncryption();
        }


        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse GetPromoOffers(APIGatewayProxyRequest request, ILambdaContext lambdaContext)
        {
            return new TracingRequestHandler().LambdaWrapper(FunctionHandlerPromoOffersBase, request, lambdaContext);
        }


        //[LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse FunctionHandlerPromoOffersBase(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var sw = Stopwatch.StartNew();
            PromotionDetailsResponse promotionDetails = new PromotionDetailsResponse();
            lookupResponse customerResponse = new lookupResponse();
            List<TenderInformation> tendersInformation = new List<TenderInformation>();
            double pointsRedeemed = 0;
            var requestId = string.Empty;
            try
            {
                request.Headers.TryGetValue("CF-RAY", out string cfRay);
                request.Headers.TryGetValue("CF-Connecting-IP", out string cfConnectingIp);
                Console.WriteLine("OffersGet.Request.Body:{0}, CloudFlare Headers:CF-RAY={1} , CF-Connecting-IP:{2}", request.Body.Replace(Environment.NewLine, ""),cfRay,cfConnectingIp);
                if (string.IsNullOrEmpty(request.Body))
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, "{\"message\": \"Request Body is Empty\"}");

                var offersRequest = JsonConvert.DeserializeObject<OffersRequest>(request.Body);
                Console.WriteLine("siteID={2} ,CloudFlareRequest Headers: CF-RAY={0} , CF-Connecting-IP:{1}",cfRay,cfConnectingIp,offersRequest.siteData.siteID);
                requestId = string.Format("{0}.{1}.{2}", offersRequest.customerData[0].customerDataValue, offersRequest.siteData.siteID, offersRequest.posData.transactionNumber);
                Console.WriteLine("RequestId:{0}.Request Body:{1}", requestId, JsonConvert.SerializeObject(offersRequest));

                //Store specific Authentication
                var handler = new JwtSecurityTokenHandler();
                var decodedValue = handler.ReadJwtToken(request.Headers["Authorization"].ToString().Split(' ')[1]).Payload;
                if (decodedValue == null)
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, "{\"message\": \"Access token is wrong\"}");
                Console.WriteLine("RequestId:{0}.Token Decode Data : {1}", requestId, decodedValue["client_id"]);
                var siteLocation = _dbService.GetSiteKeysAsync(requestId, offersRequest.siteData.siteID).Result;
                if (siteLocation == null)
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, "{\"message\": \"Site keys fetching failed\"}");

                Console.WriteLine("RequestId:{0}. {1} compare with {2}", requestId, siteLocation.ClientID.ToString(), decodedValue["client_id"]);
                bool Authentication = Helper.CompareKeys(requestId, request.Headers["Authorization"].ToString().Split(' ')[1], siteLocation.ClientID, _masterClientIdKey);
                if (!Authentication)
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, "{\"message\": \"Site keys used are wrong\"}");
                Console.WriteLine("RequestId:{0}.Token validation is success", requestId);

                request.Headers.TryGetValue("X-Cap-Origin-Source", out string capOriginSource);
                var crmUsername = string.Format(_crmUsernameFormat, siteLocation.GlobalSiteId);

                (string idName, string idValue, bool isNotInterested) customerIdInfo = Helper.ResolveCustomerDetails(offersRequest.customerData[0].customerDataType,
                                                                                        offersRequest.customerData[0].customerDataValue);
                Console.WriteLine("RequestId:{0}.Customer not-interested:{1}", requestId, customerIdInfo.isNotInterested);
                string digitalLoyaltyCard = string.Empty;

                //string.IsNullOrEmpty(capOriginSource) --> means, X-Cap-Origin-Source Header Is not sent in API Request
                // !capOriginSource.ToUpper().Equals("OTA") --> means, X-Cap-Origin-Source Header is sent with different value in API Request, other than OTA --considered as POS
                string sourceType = (string.IsNullOrEmpty(capOriginSource) || !capOriginSource.ToUpper().Equals("OTA")) ? "POS" : "OTA";
                Console.WriteLine("RequestId:{0}. X-Cap-Origin-Source:{1} So SourceType:{2}", requestId, capOriginSource, sourceType);
                bool dynamicQrActiveStatus = bool.Parse(isDynamicQrActive);
                if (dynamicQrActiveStatus)
                {
                    if (dynamicQrActiveStatus && sourceType.Equals("POS") && offersRequest.customerData[0].customerDataType.ToLower() == "digitalloyaltycard" && !offersRequest.customerData[0].customerDataValue.StartsWith("D") && offersRequest.customerData[0].customerDataValue.Length != 17)
                    {
                        string decryptedData = _encryptionService.AESDecryptAsync(requestId, offersRequest.customerData[0].customerDataValue, _aesTokenValue);
                        bool qrCheck = false;
                        if (string.IsNullOrEmpty(decryptedData))
                        {
                            ErrorResponse errorResponse = new ErrorResponse
                            {
                                ResponseCode = 602,
                                ResponseMessage = "Invalid QR Code"
                            };
                            return funcSendResponse(requestId, HttpStatusCode.OK, Mapper.Map(requestId, offersRequest, null, _offerMessage, errorResponse, null, customerResponse));
                        }

                        digitalLoyaltyCard = decryptedData.Split("|").FirstOrDefault();
                        customerIdInfo.idValue = digitalLoyaltyCard;
                        offersRequest.customerData[0].customerDataValue = digitalLoyaltyCard;
                        if (String.IsNullOrEmpty(offersRequest.requestData.cartEvaluationID))
                            qrCheck = true;
                        if (qrCheck)
                        {
                            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                            int secondsSinceEpoch = (int)t.TotalSeconds;
                            int epochTimeFromDecrypt = Convert.ToInt32(decryptedData.Split("|").LastOrDefault());
                            if (secondsSinceEpoch < epochTimeFromDecrypt)
                            {
                                digitalLoyaltyCard = decryptedData.Split("|").FirstOrDefault();
                                customerIdInfo.idValue = digitalLoyaltyCard;
                                offersRequest.customerData[0].customerDataValue = digitalLoyaltyCard;
                            }
                            else
                            {
                                ErrorResponse errorResponse = new ErrorResponse
                                {
                                    ResponseCode = 601,
                                    ResponseMessage = "QR code expired. Please refresh QR code."
                                };
                                return funcSendResponse(requestId, HttpStatusCode.OK, Mapper.Map(requestId, offersRequest, null, _offerMessage, errorResponse, null, customerResponse));
                            }
                        }
                    }
                    else if (sourceType.Equals("POS") && offersRequest.customerData[0].customerDataType.ToLower() == "digitalloyaltycard")
                    {

                        ErrorResponse errorResponse = new ErrorResponse
                        {
                            ResponseCode = 603,
                            ResponseMessage = "Entry of Loyalty ID not acceptable"
                        };
                        return funcSendResponse(requestId, HttpStatusCode.OK, Mapper.Map(requestId, offersRequest, null, _offerMessage, errorResponse, null, customerResponse));
                    }
                }

                if (!customerIdInfo.isNotInterested)
                {
                    //Customer check
                    var customerResponseTask = _crmService.CustomerLookUpAsync(requestId, crmUsername, customerIdInfo.idName, customerIdInfo.idValue);
                    customerResponse = customerResponseTask.Result;

                    //Card not active check from error for ErrorCode = 8087
                    if (customerResponse != null && customerResponse.errors != null && customerResponse.errors.Count > 0 && customerResponse.errors[0].code == 8087)
                        return funcSendResponse(requestId, HttpStatusCode.OK, Mapper.Map(requestId, offersRequest, null, _offerMessage, new ErrorResponse { ResponseCode = 500, ResponseMessage = "Customer card is not active" }, null, customerResponse));

                    if (customerResponse == null || customerResponse.errors != null)
                        return funcSendResponse(requestId, HttpStatusCode.OK, Mapper.Map(requestId, offersRequest, null, _offerMessage, GetErrorResponse(customerResponse), null, customerResponse));
                    Console.WriteLine("RequestId:{0}. Customer with {1}={2} found in CRM", requestId, customerIdInfo.idName, customerIdInfo.idValue);

                    //Card Active check
                    if (customerIdInfo.idName.Contains("cardnumber"))
                    {
                        var cardDetail = customerResponse.cardDetails.Where(c => c.cardNumber == customerIdInfo.idValue).FirstOrDefault();
                        if (cardDetail == null || cardDetail.statusInfo.status.ToUpper() != "ACTIVE")
                            return funcSendResponse(requestId, HttpStatusCode.OK, Mapper.Map(requestId, offersRequest, null, _offerMessage, new ErrorResponse { ResponseCode = 500, ResponseMessage = "Customer card is not active" }, null, customerResponse));
                    }
                }

                //Fetching Tender Information from DynamoDB
                if (offersRequest.tenders != null && offersRequest.tenders.Count() > 0)
                {
                    List<string> acquierIDs = new List<string>();
                    foreach (var tender in offersRequest.tenders)
                    {
                        if (!string.IsNullOrEmpty(tender.acquirerID))
                            acquierIDs.Add(tender.acquirerID);
                    }
                    tendersInformation = _dbService.GetTenderDetailsAsync(requestId, acquierIDs).Result;
                }

                //Promo Engine Evaluation
                var promotionRequest = Mapper.Map(requestId, offersRequest, customerResponse.id, tendersInformation);
                var promotionResponse = _crmService.PromoEvaluateAsync(requestId, crmUsername, promotionRequest).Result;
                if (promotionResponse.errorDetails != null)
                {
                    if (promotionResponse == null || promotionResponse.errors != null || promotionResponse.errorDetails.Count() > 0)
                    {
                        ErrorResponse errorResponse = new ErrorResponse
                        {
                            ResponseCode = promotionResponse.errorCode,
                            ResponseMessage = promotionResponse.errorDetails[0]
                        };
                        return funcSendResponse(requestId, HttpStatusCode.OK, Mapper.Map(requestId, offersRequest, null, _offerMessage, errorResponse, null, customerResponse));
                    }
                }

                //Fetch applied promotion details
                List<string> promotionIDs = new List<string>();
                foreach (var paymentVoucher in promotionResponse.data.appliedPaymentVouchers)
                {
                    if (!promotionIDs.Contains(paymentVoucher.promotionId))
                        promotionIDs.Add(paymentVoucher.promotionId);
                }
                foreach (var cartItem in promotionResponse.data.cartItems)
                {
                    foreach (var promotion in cartItem.appliedPromotions)
                    {
                        if (!promotionIDs.Contains(promotion.promotionId))
                            promotionIDs.Add(promotion.promotionId);

                    }
                }
                if (promotionIDs.Count > 0)
                    promotionDetails = _crmService.PromotionDetailsGetAsync(requestId, crmUsername, promotionIDs).Result;
                else
                    promotionDetails = null;

                //Generation API response
                var API_Response = Mapper.Map(requestId, offersRequest, promotionResponse, _offerMessage, null, promotionDetails, customerResponse, tendersInformation, pointsRedeemed, customerIdInfo.isNotInterested);

                return funcSendResponse(requestId, HttpStatusCode.OK, API_Response);
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                return funcSendResponse(requestId, HttpStatusCode.BadRequest, string.Format("Parsing exception occurred.Message : {0}", e.Message));
            }
            catch (Exception e)
            {
                return funcSendResponse(requestId, HttpStatusCode.BadGateway, string.Format("Unkown error occurred.Message : {0}", e.Message));
            }
        }

        #region Private Methods

        Func<string, HttpStatusCode, string, APIGatewayProxyResponse> funcSendResponse = (requestId, httpStatusCode, body) =>
        {
            Console.WriteLine("RequestId:{0}. httpStatusCode:{1} Response:{2}", requestId, httpStatusCode, body.Replace(Environment.NewLine, " "));

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)httpStatusCode,
                Headers = new Dictionary<string, string> { { "content-type", "application/json" }, { "INTG-RequestID", requestId } },
                Body = body
            };
        };

        private ErrorResponse GetErrorResponse(lookupResponse customerResponse)
        {
            return (customerResponse == null) ?
            new ErrorResponse { ResponseCode = 500, ResponseMessage = "Customer not found in CRM" } :
            new ErrorResponse
            {
                ResponseCode = customerResponse.errors[0].code,
                ResponseMessage = customerResponse.errors[0].message
            };
        }

        #endregion
    }
}
