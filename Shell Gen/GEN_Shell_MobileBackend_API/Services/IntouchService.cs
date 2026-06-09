using Newtonsoft.Json;
using GEN_Shell_MobileBackend_API.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using cardRes = GEN_Shell_MobileBackend_API.Models;
using custLookUp = GEN_Shell_MobileBackend_API.Models.CustomerLookUp;
using System.Net.Http;
using System.Linq;
using GEN_Shell_MobileBackend_API.Models;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1;
using GEN_Shell_MobileBackend_API.Models.Promotions;
using GEN_Shell_MobileBackend_API.Models.TargetDetails;
using GEN_Shell_MobileBackend_API.Models.StoreDetails;
using Capillary.ShellProxy.Model.CustomerAddModel.Request;
using Capillary.ShellProxy.Model.CustomerAddModel.Response;

namespace GEN_Shell_MobileBackend_API.Services
{
    public interface ICrmService
    {
        Task<cardRes.CardResponse> CardDetailsGetAsync(string requestId, string value);
        Task<custLookUp.CustomerLookUpResponse> CustomersLookupGetAsync(string requestId, string value, string apiUserName = "");
        Task<custLookUp.CustomerLookUpResponse> CustomersLookupGetAsync(string requestId, string value,string embed, string apiUserName = "");
        Task<cardRes.MembershipCheckResponse> MemberShipCheckAsync(string requestId, cardRes.MembershipCheckRequest request, string ApiKey,string TransactionSignature);
        Task<cardRes.PaymentGetResponse> GetPaymentDetailsAsync(string requestId, string customer_id,string paymentId);
        Task<cardRes.GetCustomerTransaction> GetCustomerTransactionAsync(string requestId, string mobile, string billnumber);
        Task<cardRes.EmailCommResponse.EmailResponse> SendEmailAsync(string requestId, cardRes.EmailRequest request);
        Task<GetCustomerPromotionResponse> GetCustomerPromotionAsync(string requestId, string value,string apiParamMode);
        Task<GetTargetDetailsResponse> GetTargetDetailsAsync(string requestId, string value);
        Task<PromotionDetailsResp> PromotionDetailsGetAsync(string requestId, List<string> promotionIDs);
        Task<GetStoreDetailsResp> GetStoreDetailsAsync(string requestId, string storeCode);
        Task<CustomerAddResponse> CustomerAddAsync(string requestId,string value,string source,CustomerAddRequest request);
    }
    public class IntouchService : ICrmService
    {
        string _serviceUrl;
        string _username;
        string _password;
        Dictionary<string, string> _headers;
        Dictionary<string, string> _bonuslinkHeaders;

        public IntouchService(string serviceUrl, string username, string password, string lambdaVersion)
        {
            _serviceUrl = serviceUrl;
            _username = username;
            _password = password;
            _headers = new Dictionary<string, string>{
                {Constants.HeaderUserAgent,string.Format(Constants.HeaderUserAgent,lambdaVersion)},
                {Constants.HeaderAuthorization, string.Empty}};
            _bonuslinkHeaders = new Dictionary<string, string>();
            
        }
        public async Task<cardRes.CardResponse> CardDetailsGetAsync(string requestId, string value)
        {
            try
            {
                SetAuthHeader(requestId);
                string url = string.Format("{0}{1}", _serviceUrl, Constants.EndpointCardDetails);
                url = string.Format(url, value);

                Console.WriteLine("RequestId:{0}. CardDetailsGetAsync.Request for card : {1}", requestId, value);

                var cardResponse = await HttpHandler.GetAsync<cardRes.CardResponse>(requestId, url, _headers, "CardDetailsGetAsync");

                Console.WriteLine("RequestId:{0}. CardDetailsGetAsync.Response for card {1} is Response message:{2}", requestId, value,
                                                JsonConvert.SerializeObject(cardResponse));

                return cardResponse;

            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.CustomerCouponsGetAsync().Message:'{1}'", requestId, e.Message);
            }

            return default(cardRes.CardResponse);
        }

        public async Task<custLookUp.CustomerLookUpResponse> CustomersLookupGetAsync(string requestId, string value, string apiUsername)
        {
            try
            {
               
                if(!string.IsNullOrEmpty(apiUsername))
                    _username = apiUsername;
                 SetAuthHeader(requestId);

                string url = string.Format("{0}{1}", _serviceUrl, Constants.EndpointCustomerLookup);
                url = string.Format(url, value);

                Console.WriteLine("RequestId:{0}. CustomerLookUpGetAsync.Request for card : {1}", requestId, value);
                var lookUpResponse = await HttpHandler.GetAsync<custLookUp.CustomerLookUpResponse>(requestId, url, _headers, "CardDetailsGetAsync");

                Console.WriteLine("RequestId:{0}. CustomerLookUpGetAsync.Response for card {1} is Response message:{2}", requestId, value,
                                                JsonConvert.SerializeObject(lookUpResponse));

                return lookUpResponse;

            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.CustomerCouponsGetAsync().Message:'{1}'", requestId, e.Message);
            }

            return default(custLookUp.CustomerLookUpResponse);
        }
        public async Task<custLookUp.CustomerLookUpResponse> CustomersLookupGetAsync(string requestId, string value,string embed, string apiUsername)
        {
            try
            {
               
                if(!string.IsNullOrEmpty(apiUsername))
                    _username = apiUsername;
                 SetAuthHeader(requestId);

                string url = string.Format("{0}{1}", _serviceUrl, Constants.EndpointCustomerLookup);
                url = string.Format(url, value);
                url = url +"&embed="+embed;

                Console.WriteLine("RequestId:{0}. CustomerLookUpGetAsync.Request for card : {1}", requestId, value);
                var lookUpResponse = await HttpHandler.GetAsync<custLookUp.CustomerLookUpResponse>(requestId, url, _headers, "CardDetailsGetAsync");

                Console.WriteLine("RequestId:{0}. CustomerLookUpGetAsync.Response for card {1} is Response message:{2}", requestId, value,
                                                JsonConvert.SerializeObject(lookUpResponse));

                return lookUpResponse;

            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.CustomerCouponsGetAsync().Message:'{1}'", requestId, e.Message);
            }

            return default(custLookUp.CustomerLookUpResponse);
        }
        public async Task<CustomerAddResponse> CustomerAddAsync(string requestId,string value,string source,CustomerAddRequest request)
        {
            //fix this
            try
            {
                SetAuthHeader(requestId);
                string url = string.Format("{0}{1}", _serviceUrl, Constants.EndpointCustomerAdd);
                url = string.Format(url,value,source);

                var serRequest = JsonConvert.SerializeObject(request,
                            Newtonsoft.Json.Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            });
                Console.WriteLine("RequestId:{0}.CustomerAddAsync.Request: Body:{1}", requestId, serRequest.Replace(Environment.NewLine, ""));

                var customerAddResponse = await HttpHandler.PutAsync<HttpContent, CustomerAddResponse>(requestId, url,
                                         _headers, Helper.CreateStringContent<CustomerAddRequest>(request), "CustomerAdd");

                if (customerAddResponse != null)
                    Console.WriteLine("RequestId:{0}. CustomerAddAsync.Response: {1}", requestId, JsonConvert.SerializeObject(customerAddResponse));

                return customerAddResponse;
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.CustomerAddAsync().Message:'{1}'", requestId, e.Message);
            }

            return default(CustomerAddResponse);
        }

        public async Task<cardRes.MembershipCheckResponse> MemberShipCheckAsync(string requestId, cardRes.MembershipCheckRequest request,string ApiKey,string TransactionSignature)
        {
            try
            {
                SetAuthHeaderBonusLink(ApiKey,TransactionSignature);
                string url = string.Format("{0}",Constants.MembershipCheck);

                var serRequest = JsonConvert.SerializeObject(request,
                            Newtonsoft.Json.Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            });
                Console.WriteLine("RequestId:{0}.memberShipCheckAsync.Request:CardNo={1}, Body:{2}", requestId, request.CardNumber,
                                                                                                        serRequest.Replace(Environment.NewLine, ""));

                var membershipCheckResponse = await HttpHandler.PostAsync<HttpContent, cardRes.MembershipCheckResponse>(requestId, url,
                                         _bonuslinkHeaders, Helper.CreateStringContent<cardRes.MembershipCheckRequest>(request), "MembershipCheck");

                if (membershipCheckResponse != null)
                {
                 
                   Console.WriteLine("RequestId:{0}. CustomerCheckAsync.Response:CardNo={1}. Transaction push status:{2}", requestId,
                                                request.CardNumber, membershipCheckResponse.ResultInfo.Success != true && membershipCheckResponse.ResultInfo.ErrorCodes.Count > 0 ? "Fail" : "Success");
                }

                return membershipCheckResponse;
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.TransactionAdd().Message:'{1}'", requestId, e.Message);
            }

            return default(cardRes.MembershipCheckResponse);
        }

        public async Task<cardRes.PaymentGetResponse> GetPaymentDetailsAsync(string requestId, string customer_id, string paymentId)
        {
            try
            {
                SetAuthHeader(requestId);
                string url = string.Format("{0}{1}", _serviceUrl, Constants.EndpointGetPayment);
                url = string.Format(url, customer_id, paymentId);

                Console.WriteLine("RequestId:{0}. GetPaymentDetails for customer_id : {1}", requestId, customer_id);

                var getPaymentResponse = await HttpHandler.GetAsync<PaymentGetResponse>(requestId, url, _headers, "CardDetailsGetAsync");

                Console.WriteLine("RequestId:{0}. GetPaymentDetails.Response for card {1} is Response message:{2}", requestId, customer_id,
                                                JsonConvert.SerializeObject(getPaymentResponse));

                return getPaymentResponse;

            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.CustomerTransaction().Message:'{1}'", requestId, e.Message);
            }

            return default(PaymentGetResponse);
        }
      

        public async Task<GetCustomerTransaction> GetCustomerTransactionAsync(string requestId, string mobile, string billnumber)
        {
            try
            {
                SetAuthHeader(requestId);
                string url = string.Format("{0}{1}", _serviceUrl, Constants.EndpointCustomerTransaction);
                url = string.Format(url, mobile, billnumber);

                Console.WriteLine("RequestId:{0}. CustomerTransaction.Request for mobile : {1}", requestId, mobile);

                var customerTransactionResponse = await HttpHandler.GetAsync<GetCustomerTransaction>(requestId, url, _headers, "CardDetailsGetAsync");

                Console.WriteLine("RequestId:{0}. CustomerTransaction.Response for card {1} is Response message:{2}", requestId, mobile,
                                                JsonConvert.SerializeObject(customerTransactionResponse));

                return customerTransactionResponse;

            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.CustomerTransaction().Message:'{1}'", requestId, e.Message);
            }

            return default(GetCustomerTransaction);
        }

        public async Task<cardRes.EmailCommResponse.EmailResponse> SendEmailAsync(string requestId, cardRes.EmailRequest request)
        {
            try
            {
                SetAuthHeader(requestId);
                string url = string.Format("{0}{1}", _serviceUrl, Constants.SendEmail);

                var serRequest = JsonConvert.SerializeObject(request,
                            Newtonsoft.Json.Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            });
                Console.WriteLine("RequestId:{0}.SendEmailAsync.Request:CardNo={1}, Body:{2}", requestId, request.root.email[0].to,
                                                                                                        serRequest.Replace(Environment.NewLine, ""));

                var emailResponse = await HttpHandler.PostAsync<HttpContent, cardRes.EmailCommResponse.EmailResponse>(requestId, url,
                                         _headers, Helper.CreateStringContent<cardRes.EmailRequest>(request), "EmailRequest");

                if (emailResponse != null)
                {
                    Console.WriteLine("RequestId:{0}. EmailSentAsync.Response:CardNo={1}. Transaction push status:{2}", requestId,
                                                 request.root.email[0].to, emailResponse.response.status.success);
                }
                return emailResponse;
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.TransactionAdd().Message:'{1}'", requestId, e.Message);
            }

            return default(cardRes.EmailCommResponse.EmailResponse);
        }

        public async Task<GetCustomerPromotionResponse> GetCustomerPromotionAsync(string requestId, string userId,string apiParamMode)
        {
            try
            {
                SetAuthHeader(requestId);
                string modeParam= string.IsNullOrEmpty(apiParamMode) ? null : "&mode=" + apiParamMode;
                string url = string.Format("{0}{1}{2}", _serviceUrl, Constants.EndpointGetCustomerPromotion, modeParam);
                url = string.Format(url, userId);

                Console.WriteLine("RequestId:{0}. GetCustomerPromotionAsync.Request for userId : {1}", requestId, userId);
                var getCustomerPromotionResp = await HttpHandler.GetAsync<GetCustomerPromotionResponse>(requestId, url, _headers, "GetCustomerPromotionAsync");
                Console.WriteLine("RequestId:{0}. GetCustomerPromotionAsync.Response for userId {1} is Response message:{2}", requestId, userId,
                                                JsonConvert.SerializeObject(getCustomerPromotionResp));
                return getCustomerPromotionResp;
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.GetCustomerPromotionAsync().Message:'{1}'", requestId, e.Message);
            }

            return default(GetCustomerPromotionResponse);
        }

        public async Task<GetTargetDetailsResponse> GetTargetDetailsAsync(string requestId, string userId)
        {
            try
            {
                SetAuthHeader(requestId);
                string url = string.Format("{0}{1}", _serviceUrl, Constants.EndpointGetTargetDetails);
                url = string.Format(url, userId);

                Console.WriteLine("RequestId:{0}. GetTargetDetailsAsync.Request for userId : {1}", requestId, userId);
                var getCustomerPromotionResp = await HttpHandler.GetAsync<GetTargetDetailsResponse>(requestId, url, _headers, "GetTargetDetailsAsync");
                Console.WriteLine("RequestId:{0}. GetTargetDetailsAsync.Response for userId {1} is Response message:{2}", requestId, userId,
                                                JsonConvert.SerializeObject(getCustomerPromotionResp));

                return getCustomerPromotionResp;
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.GetTargetDetailsAsync().Message:'{1}'", requestId, e.Message);
            }
            return default(GetTargetDetailsResponse);
        }

        public async Task<PromotionDetailsResp> PromotionDetailsGetAsync(string requestId, List<string> promotionIDs)
        {
            StringBuilder queryParam = new StringBuilder();
            try
            {
                SetAuthHeader(requestId);
                foreach (var promotionID in promotionIDs)
                    queryParam.Append("promotionIds=" + promotionID + "&");
                queryParam.Remove(queryParam.Length - 1, 1);
                string url = string.Format("{0}{1}{2}", _serviceUrl, Constants.EndpointPromoDetails, queryParam);

                Console.WriteLine("RequestId:{0}. PromotionDetailsAsync.RequestURL : {1}", requestId, url);

                var promotionDetails = await HttpHandler.GetAsync<PromotionDetailsResp>(requestId, url, _headers, "PromotionDetailsGetAsync");
                return promotionDetails;

            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.PromotionDetailsGetAsync().Message:'{1}'", requestId, e.Message);
            }

            return default(PromotionDetailsResp);
        }

        public async Task<GetStoreDetailsResp> GetStoreDetailsAsync(string requestId, string storeCode)
        {
            try
            {
                SetAuthHeader(requestId);
                string url = string.Format("{0}{1}", _serviceUrl, Constants.EndpointGetStoreDetails);
                url = string.Format(url, storeCode);

                Console.WriteLine("RequestId:{0}. GetStoreDetailsAsync.Request for storeCode : {1}", requestId, storeCode);
                var getStoreDetailsResp = await HttpHandler.GetAsync<GetStoreDetailsResp>(requestId, url, _headers, "GetStoreDetailsAsync");
                Console.WriteLine("RequestId:{0}. GetStoreDetailsAsync.Response for userId {1} is Response message:{2}", requestId, storeCode,
                                                JsonConvert.SerializeObject(getStoreDetailsResp));

                return getStoreDetailsResp;
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.GetStoreDetailsAsync().Message:'{1}'", requestId, e.Message);
            }
            return default(GetStoreDetailsResp);
        }
        private void SetAuthHeader(string requestId)
        {
            var byteArray = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", _username, _password));
            Console.WriteLine("RequestId:{0}. Username {1}", requestId, _username);
            _headers[Constants.HeaderAuthorization] = string.Format("Basic {0}", Convert.ToBase64String(byteArray));
        }
        private void SetAuthHeaderBonusLink(string ApiKey, string transactionSignature)
        {
            //var byteArray = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", _username, _password));
            //_headers[Constants.HeaderAuthorization] = string.Format("Basic {0}", Convert.ToBase64String(byteArray));
            _bonuslinkHeaders["ApiKey"] = ApiKey;
            _bonuslinkHeaders["TransactionSignature"] = transactionSignature;
            //    _bonuslinkHeaders["Content-Type"] = "application/json";
        }
    }
}
