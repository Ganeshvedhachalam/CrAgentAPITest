using System;
using System.Collections.Generic;
using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Capillary.ShellProxy.Service;
using Capillary.ShellProxy.Utilities;
using System.Diagnostics;
using Newtonsoft.Json;
using Amazon;
using Capillary.ShellProxy.Model.ShellTransactionModel.Request;
using System.Linq;
using System.Threading.Tasks;
using Capillary.ShellProxy.Model.CouponModel;
using Capillary.ShellProxy.Model;
using Capillary.ShellProxy.Model.CustomerModel.Response;
using Capillary.ShellProxy.Model.TransactionModel.v2.Response;
using OpenTracing.Util;
using NewRelic.OpenTracing.AmazonLambda;
using System.IdentityModel.Tokens.Jwt;

namespace Capillary.ShellProxy.API
{
    public class FunctionHandlerAddTxn
    {
        ICrmService _crmService;
        IDBService _dbService;
        IStorageService _storageService;
        IEncryptionService _encryptionService;
        string _categories;
        string _crmUsernameFormat;
        string _masterClientIdKey;
        static string _environment = Constants.production;
        string _aesTokenValue;
        string isDynamicQrActive;
        public FunctionHandlerAddTxn()
        {

            GlobalTracer.Register(NewRelic.OpenTracing.AmazonLambda.LambdaTracer.Instance);

            //Environment variables
            RegionEndpoint region;
            string bucketName;
            string tenderTableName;
            string locationTableName;
            string lambdaVersion;
            string intouchSvcUrl;
            string tillPassword;

            //This will be set only while running on AWS environment.
            var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION");

            if (!string.IsNullOrEmpty(awsRegion))
            {
                region = RegionEndpoint.GetBySystemName(awsRegion);
                bucketName = Environment.GetEnvironmentVariable("AWS_BUCKET_NAME");
                lambdaVersion = Environment.GetEnvironmentVariable("LAMBDA_VERSION");
                intouchSvcUrl = Environment.GetEnvironmentVariable("CRM_SVC_URL");
                tillPassword = Environment.GetEnvironmentVariable("CRM_PASSWORD");
                _categories = Environment.GetEnvironmentVariable("DISCOUNT_CATEGORIES");
                _crmUsernameFormat = Environment.GetEnvironmentVariable("CRM_USERNAME_FORMAT");
                tenderTableName = Environment.GetEnvironmentVariable("Tender_TableName");
                locationTableName = Environment.GetEnvironmentVariable("Location_TableName");
                _masterClientIdKey = Environment.GetEnvironmentVariable("Master_ClientId");
                _aesTokenValue = Environment.GetEnvironmentVariable("AES_TOKEN");
                isDynamicQrActive = Environment.GetEnvironmentVariable("IS_DYNAMIC_QR_ACTIVE");
            }
            else
            {
                region = RegionEndpoint.GetBySystemName("us-east-1");
                _environment = Constants.demo;
                bucketName = "shellintegrations";
                lambdaVersion = "1";
                tenderTableName = "ShellTenders";
                locationTableName = "ShellLocations";
                intouchSvcUrl = "https://apac2.api.capillarytech.com";
                tillPassword = "";
                _crmUsernameFormat = "demo.shell.sg.{0}.1";
                _categories = "1021,4_1051,3_1061,6_1071,6_1081,3_1091,4_1101,5_1111,5_2011,3_2021,2_2031,1_2041,2_2051,0_2061,0";
                _aesTokenValue = "";
                isDynamicQrActive = "false";

            }

            Console.WriteLine("Environment variables-->region:{0};bucketName:{1};intouchSvcUrl:{2};", region, bucketName, intouchSvcUrl);

            //todo: register services with Container
            _crmService = new IntouchService(intouchSvcUrl, tillPassword, lambdaVersion);
            _storageService = new S3Service(region, bucketName);
            _dbService = new DynamoService(tenderTableName, locationTableName);
            _encryptionService = new AesEncryption();
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse TransactionAdd(APIGatewayProxyRequest request, ILambdaContext lambdaContext)
        {
            return new TracingRequestHandler().LambdaWrapper(FunctionHandlerTransactionAddBase, request, lambdaContext);
        }


        //[LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse FunctionHandlerTransactionAddBase(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var requestId = string.Empty;
            string transactionNumber = string.Empty;
            string siteId = string.Empty;
            SiteLocation siteLocation = new SiteLocation();
            List<ProductLine> productLines = new List<ProductLine>();
            List<TenderInformation> tendersInformation = new List<TenderInformation>();
            string customerProgramName = string.Empty;
            if (RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("AWS_REGION")) == RegionEndpoint.GetBySystemName("us-east-1"))
                _environment = "demo";
            //string customerDataValue = string.Empty;

            RetailTransactionRequest retailRequests = new RetailTransactionRequest();
            try
            {
                request.Headers.TryGetValue("CF-RAY", out string cfRay);
                request.Headers.TryGetValue("CF-Connecting-IP", out string cfConnectingIp);
                Console.WriteLine("TransactionAdd.Request.Body:{0}, CloudFlare Headers:CF-RAY={1} , CF-Connecting-IP:{2}", request.Body.Replace(Environment.NewLine, ""),cfRay,cfConnectingIp);
                retailRequests = JsonConvert.DeserializeObject<RetailTransactionRequest>(request.Body);


                ///Reconcillation transactions API 
                if (request.Body.Contains("Reconciliations"))
                {
                    siteId = retailRequests.siteData.siteID;
                    requestId = string.Format("{0}.Reconciliations", siteId);
                    siteLocation = _dbService.GetSiteKeysAsync(requestId, siteId).Result;
                    if (siteLocation == null)
                        return funcSendResponse(requestId, HttpStatusCode.BadRequest, "{\"message\": \"Fetching site keys failed\"}", siteId, 400);

                    bool ReconAuthentication = Helper.CompareKeys(requestId, request.Headers["Authorization"].ToString().Split(' ')[1], siteLocation.ClientID, _masterClientIdKey);
                    if (ReconAuthentication)
                        return funcSendResponse(requestId, HttpStatusCode.OK, Mapper.Map(requestId, null, null, new ErrorResponse { ResponseCode = 200, ResponseMessage = "Recon Transaction" }), siteId, 0);
                    else
                        return funcSendResponse(requestId, HttpStatusCode.BadRequest, "{\"message\": \"Site keys are wrong\"}", siteId, 400);
                }

                var retailRequest = retailRequests.objects[0];
                siteId = retailRequest.siteData.siteID;
                 Console.WriteLine("siteID={2} , CloudFlareRequest Headers: CF-RAY={0} , CF-Connecting-IP:{1}",cfRay,cfConnectingIp,siteId);

                //Forming requestId from data values
                if (retailRequest.customerData.Count > 0 && retailRequest.customerData[0].customerDataValue != null)
                    requestId = string.Format("{0}.{1}.{2}", retailRequest.customerData[0].customerDataValue, siteId, retailRequest.posData.transactionNumber);
                else
                    requestId = string.Format("{0}.{1}.{2}", string.Empty, siteId, retailRequest.posData.transactionNumber);
                Console.WriteLine("RequestId:{0}.Request Body:{1}", requestId, JsonConvert.SerializeObject(retailRequests));


                //Store specific Authentication
                if (retailRequests == null || retailRequests.objects.Count == 0)
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, "{\"message\": \"RequestEmpty\"}", siteId, 400);
                siteLocation = _dbService.GetSiteKeysAsync(requestId, siteId).Result;
                if (siteLocation == null)
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, "{\"message\": \"Fetching site keys failed\"}", siteId, 400);
                bool Authentication = Helper.CompareKeys(requestId, request.Headers["Authorization"].ToString().Split(' ')[1], siteLocation.ClientID, _masterClientIdKey);
                if (!Authentication)
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, "{\"message\": \"Site keys or Products sent are wrong\"}", siteId, 400);


                //Function to push data to S3
                Func<string, string, string, Task<HttpStatusCode>> funcAddToBucket = async (folderName, key, content) =>
                {
                    Console.WriteLine("RequestId:{0}.Putting {1} to S3 folder-{2}.Content:{3}", requestId, key, folderName, content);
                    return await _storageService.AddToBucketAsync(folderName, key, content);
                };

                var crmUsername = string.Format(_crmUsernameFormat, siteLocation.GlobalSiteId);
                request.Headers.TryGetValue("X-Cap-Origin-Source", out string capOriginSource);

                //Fetching Tender Information from DynamoDB
                if (retailRequest.tenders != null && retailRequest.tenders.Count() > 0)
                {
                    List<string> acquierIDs = new List<string>();
                    foreach (var tender in retailRequest.tenders)
                    {
                        if (!string.IsNullOrEmpty(tender.acquirerID))
                            acquierIDs.Add(tender.acquirerID);
                    }
                    tendersInformation = _dbService.GetTenderDetailsAsync(requestId, acquierIDs).Result;
                }

                //Not-Interested Transaction
                transactionNumber = string.Format("{0}_{1}_{2}_{3}", retailRequest.requestData.workstationID, retailRequest.requestData.requestID, retailRequest.siteData.siteID, retailRequest.posData.transactionNumber);
                if (retailRequest.customerData == null || retailRequest.customerData.Count == 0 || string.IsNullOrEmpty(retailRequest.customerData[0].customerDataValue))
                {
                    Console.WriteLine("RequestId:{0}.Processing for not-interested Transaction", requestId);

                    var crmNonLoyalityRequest = Mapper.Map(requestId, retailRequest, null, false, productLines, _categories, tendersInformation, string.Empty);  //todo: move business logic to here.no need tuple.
                    var crmNonLoyalityResponse = _crmService.TransactionAddAsync(requestId, crmUsername, crmNonLoyalityRequest).Result;
                    if (crmNonLoyalityResponse == null || crmNonLoyalityResponse.failureCount > 0)
                    {
                        var response = Mapper.Map(requestId, null, retailRequest, GetErrorResponse(crmNonLoyalityResponse));
                        funcAddToBucket(Constants.FolderFailedTransactions, transactionNumber, Mapper.Map(requestId, retailRequests, response, crmNonLoyalityResponse.response[0].errors[0].message, null)).Wait();
                        return funcSendResponse(requestId, HttpStatusCode.OK, response, siteId, crmNonLoyalityResponse.response[0].errors[0].code);
                    }
                    return funcSendResponse(requestId, HttpStatusCode.OK, Mapper.Map(requestId, crmNonLoyalityResponse, retailRequest, null), siteId, 200);
                }

                (string idName, string idValue, bool isNotInterested) customerIdInfo = Helper.ResolveCustomerDetails(retailRequest.customerData[0].customerDataType, retailRequest.customerData[0].customerDataValue);

                //string.IsNullOrEmpty(capOriginSource) --> means, X-Cap-Origin-Source Header Is not sent in API Request
                // !capOriginSource.ToUpper().Equals("OTA") --> means, X-Cap-Origin-Source Header is sent with different value in API Request, other than OTA--considered as POS
                string sourceType = (string.IsNullOrEmpty(capOriginSource) || !capOriginSource.ToUpper().Equals("OTA")) ? "POS" : "OTA";
                Console.WriteLine("RequestId:{0}. X-Cap-Origin-Source:{1} So SourceType:{2}", requestId, capOriginSource, sourceType);

                //Identifer check for Decrypt
                bool dynamicQrActiveStatus = bool.Parse(isDynamicQrActive); 
                if (dynamicQrActiveStatus && sourceType.Equals("POS") && retailRequest.customerData[0].customerDataType.ToLower().Equals("digitalloyaltycard"))
                {
                    string decryptedData = _encryptionService.AESDecryptAsync(requestId, retailRequest.customerData[0].customerDataValue, _aesTokenValue);
                    if (string.IsNullOrEmpty(decryptedData))
                        return funcSendResponse(requestId, HttpStatusCode.OK, "Invalid QR Code", siteId, 602);

                    string digitalLoyaltyCard = decryptedData.Split("|").FirstOrDefault();
                    customerIdInfo.idValue = digitalLoyaltyCard;
                }

                requestId = string.Format("{0}.{1}.{2}", customerIdInfo.idValue, retailRequest.siteData.siteID, retailRequest.posData.transactionNumber);
                Console.WriteLine("RequestId:{0}.Customer not-interested:{1}", requestId, customerIdInfo.isNotInterested);

                if (string.Compare(retailRequest.requestData.requestType.ToLower(), "RetailTransactionRelease", true) == 0)
                {
                    //extract the customerId through get Api
                    var customerResponseTask = _crmService.CustomerLookUpAsync(requestId, crmUsername, customerIdInfo.idName, customerIdInfo.idValue).Result;
                    if (customerResponseTask.errors != null && customerResponseTask.errors.Count > 0)
                        return funcSendResponse(requestId, HttpStatusCode.OK, Mapper.Map(requestId, null, retailRequest, customerResponseTask.errors[0].message), siteId, 500);

                    var cancelTrxnReleaseResp = _crmService.TransactionCancelAsync(requestId, crmUsername, customerResponseTask.id, retailRequest.requestData.cartEvaluationID).Result;
                    if (cancelTrxnReleaseResp != null && cancelTrxnReleaseResp.errors != null && cancelTrxnReleaseResp.errors.Count() > 0)
                        return funcSendResponse(requestId, HttpStatusCode.OK, Mapper.Map(requestId, cancelTrxnReleaseResp, retailRequest, null), siteId, 200);
                    else if (cancelTrxnReleaseResp != null && cancelTrxnReleaseResp.data != null)
                        return funcSendResponse(requestId, HttpStatusCode.OK, Mapper.Map(requestId, cancelTrxnReleaseResp, retailRequest, null), siteId, 200);
                    else if (cancelTrxnReleaseResp == null || cancelTrxnReleaseResp.errors != null) //explicit error creation to save in s3
                        return funcSendResponse(requestId, HttpStatusCode.OK, cancelTrxnReleaseResp.errors[0].message, siteId, 500);
                }

                //CRM Transaction POST
                var crmTransactionRequest = Mapper.Map(requestId, retailRequest, customerProgramName, true, productLines, _categories, tendersInformation, customerIdInfo.idValue);
                var transactionResponse = _crmService.TransactionAddAsync(requestId, crmUsername, crmTransactionRequest).Result;

                //Push Txn failures to S3
                if (transactionResponse == null || transactionResponse.failureCount > 0)
                {
                    var response = Mapper.Map(requestId, null, retailRequest, GetErrorResponse(transactionResponse));
                    funcAddToBucket(Constants.FolderFailedTransactions, transactionNumber, Mapper.Map(requestId, retailRequests, response, transactionResponse.response[0].errors[0].message, null)).Wait();
                    return funcSendResponse(requestId, HttpStatusCode.OK, response, siteId, transactionResponse.response[0].errors[0].code);
                }

                var APIResponse = Mapper.Map(requestId, transactionResponse, retailRequest, null);
                //Push Txn warnings to S3
                if (transactionResponse != null && transactionResponse.warnings != null && transactionResponse.warnings.Count > 0)
                {
                    Console.WriteLine("RequestId:{0}. Transaction number {1} warning count {2}", requestId, transactionNumber, transactionResponse.warnings.Count);
                    funcAddToBucket(Constants.FolderTransactionWarnings, transactionNumber, Mapper.Map(requestId, retailRequests, APIResponse, transactionResponse.response[0].errors[0].message, null)).Wait();
                }
                return funcSendResponse(requestId, HttpStatusCode.OK, APIResponse, siteId, 200);
            }
            catch (Newtonsoft.Json.JsonReaderException e)
            {
                Console.WriteLine("Parsing exception occurred.Message : {0}", e.Message);
                var ParsingErrorResponse = Mapper.Map(requestId, null, null, new ErrorResponse { ResponseCode = 200, ResponseMessage = "Parsing exception occurred" });
                _storageService.AddToBucketAsync(Constants.FolderFailedTransactions, DateTime.Now.ToString("HHmmss"), Mapper.Map(requestId, null, ParsingErrorResponse, "Parsing exception occurred", request.Body)).Wait();
                return funcSendResponse(requestId, HttpStatusCode.BadRequest, ParsingErrorResponse, siteId, 500);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unkown error occurred.Message : {0}", e.Message);
                var transactionException = Mapper.Map(requestId, null, retailRequests.objects[0], new ErrorResponse { ResponseCode = 200, ResponseMessage = "Unkown error occurred" });
                _storageService.AddToBucketAsync(Constants.FolderFailedTransactions, transactionNumber, Mapper.Map(requestId, retailRequests, transactionException, "Unkown error occurred", null)).Wait();
                return funcSendResponse(requestId, HttpStatusCode.BadRequest, transactionException, siteId, 500);
            }
        }

        #region Private Methods

        Func<string, HttpStatusCode, string, string, int, APIGatewayProxyResponse> funcSendResponse = (requestId, httpStatusCode, body, siteId, returnCode) =>
        {
            Console.WriteLine("RequestId:{0}. httpStatusCode:{1} returnCode:{2} Response:{3}", requestId,httpStatusCode,returnCode,body.Replace(Environment.NewLine, " "));
            CloudwatchUtils.StatusMetric(returnCode, "SGP_TransactionResponceCode").Wait();
            AwsSNSUtils.PublishSNSMessage(requestId, returnCode.ToString(), siteId, _environment).Wait();

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)httpStatusCode,
                Headers = new Dictionary<string, string> { { "content-type", "application/json" }, { "INTG-RequestID", requestId } },
                Body = body
            };
        };

        private static List<CustomerCoupon> RetreiveCoupons(string requestId, Func<string, string, string, Task<HttpStatusCode>> funcAddToBucket, Model.ShellTransactionModel.Request.Object retailRequest, string transactionNumber, (string IdentifierName, string IdentifierValue, bool isNotInterested) customerIdInfo)
        {
            var customerCoupons = new List<CustomerCoupon>();
            try
            {
                foreach (var lineItem in retailRequest.saleItems)
                {
                    if (lineItem.loyaltyOffers != null && lineItem.loyaltyOffers.Any())
                    {
                        string ProductID = string.Empty;
                        if (string.Compare(retailRequest.requestData.requestType, "RetailTransactionReturn", true) == 0)
                        {
                            foreach (var o in lineItem.loyaltyOffers)
                            {
                                funcAddToBucket(Constants.FolderRefundCoupons, string.Format("{0}.{1}.{2}", transactionNumber, lineItem.itemID, o.loyaltyOfferCode), Mapper.Map(requestId, retailRequest, lineItem, null, null)).Wait();
                            }
                        }
                        else
                        {
                            foreach (var o in lineItem.loyaltyOffers)
                            {

                                if (!string.IsNullOrEmpty(lineItem.productCode))
                                    ProductID = lineItem.productCode;
                                else
                                    ProductID = lineItem.additionalProductCode;

                                customerCoupons.Add(new CustomerCoupon
                                {
                                    CustomerKey = customerIdInfo.Item1,
                                    CustomerValue = customerIdInfo.Item2,
                                    CouponCode = o.loyaltyOfferID,
                                    DiscountAmount = lineItem.priceAdjustments != null ? lineItem.priceAdjustments[0].amount.ToString() : string.Empty,
                                    CouponType = lineItem.priceAdjustments[0].priceAdjustmentType.Contains("Gift") ? "Gift" : "Value",
                                    TransactionNumber = transactionNumber,
                                    Amount = Convert.ToString(lineItem.amount),
                                    LineItemId = Convert.ToString(lineItem.itemID),
                                    CRMProductID = ProductID
                                });
                            }
                        }
                    }
                }
                Console.WriteLine("RequestId:{0}.{1} coupons to be redeemed retreived from lineitems", requestId, customerCoupons.Count());

                foreach (var t in retailRequest.tenders)
                {
                    if (t.methodOfPayment.ToLower() == Constants.TenderVoucherPayment)
                    {
                        foreach (var v in t.voucherRules)
                        {
                            if (string.Compare(retailRequest.requestData.requestType, "RetailTransactionReturn", true) == 0)
                            {
                                funcAddToBucket(Constants.FolderRefundCoupons, string.Format("{0}.{1}.{2}", transactionNumber, Constants.TenderVoucherPayment, v.voucherCode), Mapper.Map(requestId, retailRequest, null, v, null)).Wait();
                            }
                            else
                            {
                                customerCoupons.Add(new CustomerCoupon
                                {
                                    CustomerKey = customerIdInfo.Item1,
                                    CustomerValue = customerIdInfo.Item2,
                                    DiscountAmount = Convert.ToString(v.voucherValue),
                                    CouponCode = v.voucherCode,
                                    TransactionNumber = transactionNumber,
                                    Amount = Convert.ToString(v.voucherValue),
                                    LineItemId = "tender",
                                    CouponType = "Value"
                                });
                            }
                        }
                        Console.WriteLine("RequestId:{0}.Additional coupons to be redeemed retreived from tenders. Total to be redeemed:{1}", requestId, customerCoupons.Count());

                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception in RetreiveCoupons with message :{1}", requestId, e.Message);
            }
            return customerCoupons;
        }

        private async Task<List<string>> RedeemAsync(string requestId, string username, Func<string, string, string, Task<HttpStatusCode>> funcAddToBucket, string transactionNumber, List<CustomerCoupon> customerCoupon, Model.ShellTransactionModel.Request.Object retailRequest)
        {
            var failedcoupons = new List<string>();
            try
            {
                var couponRedeemResults = await _crmService.CouponRedeemAsync(requestId, username, customerCoupon, transactionNumber, retailRequest);

                foreach (var couponRedeemResult in couponRedeemResults)
                {
                    if (couponRedeemResult.ResponseCode != 700)
                    {
                        var coupon = customerCoupon.Where(c => c.CouponCode == couponRedeemResult.CouponCode).FirstOrDefault();
                        if (coupon != null)
                        {
                            coupon.RedeemFailReason = couponRedeemResult.RedeemFailReason;
                            coupon.CouponCode = couponRedeemResult.CouponCode;
                        }


                        funcAddToBucket(Constants.FolderFailedRedemptions,
                                string.Format("{0}.{1}.{2}", transactionNumber, couponRedeemResult.LineItemId, couponRedeemResult.CouponCode),
                                Mapper.Map(requestId, retailRequest, null, null, coupon)).Wait();
                        failedcoupons.Add(couponRedeemResult.CouponCode);
                    }
                    else
                        Console.WriteLine("RequestId:{0}.CouponRedeem for coupon {1} successful.", requestId, couponRedeemResult.CouponCode);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception in Redeemaync with message :{1}", requestId, e.Message);

            }
            return failedcoupons;
        }

        private ErrorResponse GetErrorResponse(TransactionResponse transactionResponse)
        {
            return (transactionResponse == null) ? null : new ErrorResponse
            {
                ResponseCode = Convert.ToInt32(transactionResponse.response[0].errors[0].code),
                ResponseMessage = transactionResponse.response[0].errors[0].message
            };
        }

        private ErrorResponse GetErrorResponse(CustomerResponse customerCouponsResponse)
        {
            return (customerCouponsResponse == null) ? null : new ErrorResponse
            {
                ResponseCode = customerCouponsResponse.response.status.code,
                ResponseMessage = customerCouponsResponse.response.status.message
            };
        }

        #endregion
    }
}
