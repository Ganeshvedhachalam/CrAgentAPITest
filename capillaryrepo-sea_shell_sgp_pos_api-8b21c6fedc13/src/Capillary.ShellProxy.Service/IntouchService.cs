using System;
using Capillary.ShellProxy.Utilities;
using couponreq = Capillary.ShellProxy.Model.CouponModel.Request;
using couponres = Capillary.ShellProxy.Model.CouponModel.Response;
using System.Collections.Generic;
using transactionreq = Capillary.ShellProxy.Model.TransactionModel.v2.Request;
using transactionres = Capillary.ShellProxy.Model.TransactionModel.v2.Response;
using System.Threading.Tasks;
using Capillary.ShellProxy.Model.CustomerModel.Response;
using Capillary.ShellProxy.Model.OffersModel.Request;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using System.Linq;
using Capillary.ShellProxy.Model.CouponModel;
using Capillary.ShellProxy.Model.CustomerCouponModel.Response;
using Capillary.ShellProxy.Model;
using Capillary.ShellProxy.Model.OrgModel;
using promoreq = Capillary.ShellProxy.Model.PromotionModel.Request;
using promores = Capillary.ShellProxy.Model.PromotionModel.Response;
using cardRes = Capillary.ShellProxy.Model.CardsModel.Response;
using promoDetails = Capillary.ShellProxy.Model.PromotionDeailsModel.Response;

namespace Capillary.ShellProxy.Service
{
    public interface ICrmService
    {
        Task<CustomerResponse> CustomerGetAsync(string requestId, string username, string key, string value);
        Task<CustomerCouponResponse> CustomerCouponsGetAsync(string requestId, string username, string key, string value);
        Task<lookupResponse> CustomerLookUpAsync(string requestId, string username, string key, string value);
        Task<cardRes.CardResponse> CardDetailsGetAsync(string requestId, string username, string value);
        Task<StoreDetailsResponse> StoreDetailsGetAsync(string requestId, string username, string code);
        Task<OrgCustomFieldsResponse> OrgCustomFieldsGetAsync(string requestId, string username, string key);
        Task<List<CustomerCoupon>> CouponRedeemAsync(string requestId, string username, List<CustomerCoupon> customerCoupons, string transactioNo, Model.ShellTransactionModel.Request.Object transactionRequest);
        Task<List<CustomerCoupon>> IsRedeemableAsync(string requestId, string username, (string IdentifierName, string IdentifierValue, bool isNotInterested) customerIdInfo, IEnumerable<Model.CustomerModel.Response.Coupon> coupons, OffersRequest offersRequest);
        Task<transactionres.TransactionResponse> TransactionAddAsync(string requestId, string username, List<transactionreq.Transaction> request);
        Task<GetCouponSeriesResponse> GetCouponSeries(string requestId, string username, string id);
        Task<promores.PromotionResponse> PromoEvaluateAsync(string requestId, string username, promoreq.PromotionRequest request);
        Task<promoDetails.PromotionDetailsResponse> PromotionDetailsGetAsync(string requestId, string username, List<string> promotionIDs);
        Task<CancelTransactionResponse> TransactionCancelAsync(string requestId, string username,string customerId,string cartEvaluationId);

    }

    public class IntouchService : ICrmService
    {
        //HttpHandler _handler;
        string _serviceUrl;
        string _password;
        Dictionary<string, string> _headers;


        public IntouchService(string serviceUrl, string password, string lambdaVersion)
        {
            _serviceUrl = serviceUrl;
            _password = password;
            //_handler = new HttpHandler();

            _headers = new Dictionary<string, string>{
                {Constants.HeaderUserAgent,string.Format(Constants.HeaderUserAgent,lambdaVersion)},
                {Constants.HeaderAuthorization, string.Empty}};
        }


        public async Task<List<CustomerCoupon>> CouponRedeemAsync(string requestId, string username, List<CustomerCoupon> customerCoupons, string transactioNo, Model.ShellTransactionModel.Request.Object transactionRequest)
        {
            SetAuthHeader(username);
            List<CustomerCoupon> ResponseCustomerCoupons = new List<CustomerCoupon>();
            try
            {
                string url = string.Format("{0}{1}", _serviceUrl, Constants.EndpointCouponRedeem);

                var couponRequest = new couponreq.CouponRequest
                {
                    billAmount = transactionRequest.totalAmount.ToString(),
                    transactionNumber = transactioNo,
                    user = (customerCoupons[0].CustomerKey == "mobile") ?
                        new couponreq.User { mobile = customerCoupons[0].CustomerValue } :
                        new couponreq.User { externalId = customerCoupons[0].CustomerValue }
                };

                List<couponreq.RedemptionRequestList> Coupons = new List<couponreq.RedemptionRequestList>();
                foreach (var customerCoupon in customerCoupons)
                {
                    var coupon = new couponreq.RedemptionRequestList
                    {
                        code = customerCoupon.CouponCode,
                        customFields = new couponreq.CustomFields
                        {
                            siteid = transactionRequest.siteData.siteID,
                            amount = customerCoupon.DiscountAmount,
                            item_code = string.IsNullOrEmpty(customerCoupon.CRMProductID) ? null : customerCoupon.CRMProductID
                        }
                    };

                    Coupons.Add(coupon);
                }
                couponRequest.redemptionRequestList = Coupons;

                Console.WriteLine("RequestId:{0}.Coupon Redeem Request Body:'{1}'", requestId, (JsonConvert.SerializeObject(couponRequest,
                            Newtonsoft.Json.Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            })).Replace(Environment.NewLine, ""));

                var couponresponses = await HttpHandler.PostAsync<HttpContent, couponres.CouponResponse>(requestId, url,
                                            _headers, Helper.CreateStringContent<couponreq.CouponRequest>(couponRequest), "CouponRedeem");

                if (couponresponses.response != null)
                {
                    foreach (var couponresponse in couponresponses.response)
                    {
                        var ResponseCustomerCoupon = new CustomerCoupon
                        {
                            LineItemId = customerCoupons.Where(c => c.CouponCode.ToUpper() == couponresponse.result.code).FirstOrDefault().LineItemId,
                            CouponCode = couponresponse.result.code,
                            ResponseCode = couponresponse.errors == null || couponresponse.errors.Count() == 0 ? 700 : 500,
                            RedeemFailReason = couponresponse.errors != null && couponresponse.errors.Count() > 0 ? couponresponse.errors[0].message : null

                        };
                        ResponseCustomerCoupons.Add(ResponseCustomerCoupon);
                    }
                    return ResponseCustomerCoupons;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.CouponRedeem().Message:'{1}'", requestId, e.Message);
            }

            return default(List<CustomerCoupon>);
        }

        public async Task<List<CustomerCoupon>> IsRedeemableAsync(string requestId, string username, (string IdentifierName, string IdentifierValue, bool isNotInterested) customerIdInfo, IEnumerable<Model.CustomerModel.Response.Coupon> coupons, OffersRequest offersRequest)
        {
            SetAuthHeader(username);
            List<CustomerCoupon> custCopouns = new List<CustomerCoupon>();
            List<CustomerCoupon> isRedeemCoupons = new List<CustomerCoupon>();
            string url = string.Format("{0}{1}", _serviceUrl, Constants.EndpointIsRedeemable);
            try
            {
                for (int i = 0; i < coupons.Count(); i = i + 20)
                {
                    var items = coupons.Skip(i).Take(20);
                    var couponRequest = new couponreq.CouponRequest
                    {
                        billAmount = offersRequest.totalAmount.ToString(),
                        transactionNumber = string.Format("{0}_{1}_{2}_{3}", offersRequest.requestData.workstationID, offersRequest.requestData.requestID, offersRequest.siteData.siteID, offersRequest.posData.transactionNumber),
                        user = (customerIdInfo.IdentifierName.Contains("mobile")) ?
                        new couponreq.User { mobile = customerIdInfo.IdentifierValue } :
                        new couponreq.User { externalId = customerIdInfo.IdentifierValue },
                        redemptionRequestList = items.Select(c => new couponreq.RedemptionRequestList { code = c.code }).ToList()
                    };
                    Console.WriteLine("RequestId:{0}.Coupon IsRedeem Request Body:'{1}'", requestId, (JsonConvert.SerializeObject(couponRequest,
                                                                                                    Newtonsoft.Json.Formatting.None,
                                                                                                    new JsonSerializerSettings
                                                                                                    {
                                                                                                        NullValueHandling = NullValueHandling.Ignore
                                                                                                    })).Replace(Environment.NewLine, ""));

                    var isRedeemResponses = await HttpHandler.PostAsync<HttpContent, couponres.CouponResponse>(requestId, url,
                                                _headers, Helper.CreateStringContent<couponreq.CouponRequest>(couponRequest), "IsRedeemable");

                    if (isRedeemResponses == null)
                        return null;
                    custCopouns = isRedeemResponses.response.Select(r => new CustomerCoupon
                    {
                        CouponCode = r.result.code,
                        ResponseCode = r.result.redemptionStatus.code,
                        IsRedeem = r.result.redemptionStatus.code == 749 || r.result.redemptionStatus.code == 700,
                        ResponseMessage = r.result.redemptionStatus.message,
                        DiscountAmount = r.result.couponValue.ToString(),
                        discountType = r.result.discountType,
                        discountValue = r.result.discountValue,
                        discountUpto = r.result.discountUpto
                    }).ToList();
                    isRedeemCoupons.AddRange(custCopouns);
                }
                if (custCopouns == null)
                    return null;
                return isRedeemCoupons;

            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.IsRedeemableV2() .Message:'{1}'", requestId, e.Message);

            }
            return default(List<CustomerCoupon>);
        }

        public async Task<CustomerResponse> CustomerGetAsync(string requestId, string username, string key, string value)
        {
            try
            {
                SetAuthHeader(username);
                string url = string.Format("{0}{1}", _serviceUrl, Constants.EndpointCustomerGet);
                url = string.Format(url, key, value);

                Console.WriteLine("RequestId:{0}. CustomerGetAsync.Request:{1}={2}", requestId, key, value);

                var customerResponse = await HttpHandler.GetAsync<CustomerResponse>(requestId, url, _headers, "CustomerGet");

                if (customerResponse == null || customerResponse.response.status.code != 200)
                    return null;

                Console.WriteLine("RequestId:{0}. CustomerGetAsync.Response {1}={2}. Status message:{3}.", requestId, key, value,
                                               customerResponse.response.status.message);

                return customerResponse;
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.CustomerGetAsync().Message:'{1}'", requestId, e.Message);
            }

            return null;
        }

        public async Task<CustomerCouponResponse> CustomerCouponsGetAsync(string requestId, string username, string key, string value)
        {
            try
            {
                SetAuthHeader(username);
                string url = string.Format("{0}{1}", _serviceUrl, Constants.EndpointCustomerCouponsGet);
                if (value.Contains("+"))
                    value = value.Replace("+", string.Empty);
                url = string.Format(url, key, value);

                Console.WriteLine("RequestId:{0}. CustomerCouponsGetAsync.Request:{1}={2}", requestId, key, value);

                var customerCouponResponse = await HttpHandler.GetAsync<CustomerCouponResponse>(requestId, url, _headers, "CustomerCouponsGet");

                Console.WriteLine("RequestId:{0}. CustomerCouponsGetAsync.Response {1}={2}.Status message:{3}", requestId, key, value,
                                                customerCouponResponse.response.status.message);

                return customerCouponResponse;

            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.CustomerCouponsGetAsync().Message:'{1}'", requestId, e.Message);
            }

            return default(CustomerCouponResponse);
        }

        public async Task<lookupResponse> CustomerLookUpAsync(string requestId, string username, string key, string value)
        {
            try
            {
                SetAuthHeader(username);
                string url = string.Format("{0}{1}", _serviceUrl, Constants.EndpointCustomerlookup);
                if (key.Contains("external_id"))
                    key = "externalId";
                url = string.Format(url, key, value);

                Console.WriteLine("RequestId:{0}. CustomerLookUpAsync.Request:{1}={2}", requestId, key, value);

                var customerCouponResponse = await HttpHandler.GetAsync<lookupResponse>(requestId, url, _headers, "CustomerLookUpAsync");

                Console.WriteLine("RequestId:{0}. CustomerLookUpAsync.Response {1}={2}.Response message:{3}", requestId, key, value,
                                                JsonConvert.SerializeObject(customerCouponResponse));

                if (customerCouponResponse != null && customerCouponResponse.errors == null)
                {
                    //var customerResponse = CustomerStatus(customerCouponResponse.id, requestId).Result;
                    //if (string.Compare(customerCouponResponse.extendedFields.member_type, "deleted", true) == 0)
                     string status = string.IsNullOrEmpty(customerCouponResponse.statusLabel) ? null : customerCouponResponse.statusLabel;
                    if(status != null)
                    {
                        if(status.ToUpper() =="DELETED" || status.ToUpper() == "DORMANT" || status.ToUpper() == "FRAUD_CONFIRMED" || status.ToUpper() =="INACTIVE" || status.ToUpper() == "CONFIRMED" || status.ToUpper() == "INTERNAL" || status.ToUpper() == "DELETION_PENDING")
                        {
                            customerCouponResponse.errors = new List<Error>();
                            if(status.ToUpper() == "CONFIRMED")
                            {
                                status = "FRAUD CONFIRMED";
                                customerCouponResponse.errors.Add(new Error { message = "Customer has been marked as "+ status+"", code = 500 });
                            }
                            else
                                customerCouponResponse.errors.Add(new Error { message = "Customer has been marked as "+ status+"", code = 500 });
                        }
                    }
                }

                return customerCouponResponse;

            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.CustomerCouponsGetAsync().Message:'{1}'", requestId, e.Message);
            }

            return default(lookupResponse);
        }

        public async Task<cardRes.CardResponse> CardDetailsGetAsync(string requestId, string username, string value)
        {
            try
            {
                SetAuthHeader(username);
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

        public async Task<StoreDetailsResponse> StoreDetailsGetAsync(string requestId, string username, string code)
        {
            try
            {
                SetAuthHeader(username);
                string url = string.Format("{0}{1}", _serviceUrl, Constants.EndpointStoreDetailsGet);
                url = string.Format(url, code);

                Console.WriteLine("RequestId:{0}. StoreDetailsGetAsync.Request:StoreCode={1}", requestId, code);

                var storeDetailsResponse = await HttpHandler.GetAsync<StoreDetailsResponse>(requestId, url, _headers, "StoreDetailsGet");

                Console.WriteLine("RequestId:{0}. StoreDetailsResponse.Response:.StoreCode={1}.Status message:{2}", requestId, code,
                                               storeDetailsResponse.response.status.message);

                return storeDetailsResponse;

            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.StoreDetailsResponse().Message:'{1}'", requestId, e.Message);
            }

            return default(StoreDetailsResponse);
        }

        public async Task<OrgCustomFieldsResponse> OrgCustomFieldsGetAsync(string requestId, string username, string key)
        {
            try
            {
                SetAuthHeader(username);
                string url = string.Format("{0}{1}", _serviceUrl, Constants.EndpointOrgCustomFieldsGet);

                Console.WriteLine("RequestId:{0}. OrgCustomFieldsGetAsync.Request:key={1}", requestId, key);

                var orgCustomFieldsResponse = await HttpHandler.GetAsync<OrgCustomFieldsResponse>(requestId, url, _headers, "OrgCustomFieldsGet");

                Console.WriteLine("RequestId:{0}. OrgCustomFieldsResponse.Response:Status message:{1}", requestId, orgCustomFieldsResponse.response.status.message);

                return orgCustomFieldsResponse;

            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.OrgCustomFieldsResponse().Message:'{1}'", requestId, e.Message);
            }

            return default(OrgCustomFieldsResponse);
        }

        public async Task<transactionres.TransactionResponse> TransactionAddAsync(string requestId, string username, List<transactionreq.Transaction> request)
        {
            try
            {
                SetAuthHeader(username);
                string url = string.Format("{0}{1}", _serviceUrl, Constants.EndpointTransactionAdd);

                var serRequest = JsonConvert.SerializeObject(request,
                            Newtonsoft.Json.Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            });
                Console.WriteLine("RequestId:{0}.TransactionAddAsync.Request:TransactionNo={1}, Body:{2}", requestId, request[0].billNumber,
                                                                                                        serRequest.Replace(Environment.NewLine, ""));

                var transactionResponse = await HttpHandler.PostAsync<HttpContent, transactionres.TransactionResponse>(requestId, url,
                                         _headers, Helper.CreateStringContent<List<transactionreq.Transaction>>(request), "TransactionAdd");

                if (transactionResponse != null)
                {
                    Console.WriteLine("RequestId:{0}. TransactionAddAsync.Response:TransactionNo={1}. Transaction push status:{2}", requestId,
                                                request[0].billNumber, transactionResponse.response[0].errors != null && transactionResponse.response[0].errors.Count() > 0 ? "Fail" : "Success");
                }

                return transactionResponse;
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.TransactionAdd().Message:'{1}'", requestId, e.Message);
            }

            return default(transactionres.TransactionResponse);
        }

        //Phase-2
        public async Task<promores.PromotionResponse> PromoEvaluateAsync(string requestId, string username, promoreq.PromotionRequest request)
        {
            try
            {
                SetAuthHeader(username);
                string url = string.Format("{0}{1}", _serviceUrl, Constants.EndpointPromoEvalute);

                var serRequest = JsonConvert.SerializeObject(request,
                            Newtonsoft.Json.Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore,
                                DefaultValueHandling = DefaultValueHandling.Ignore
                            });
                Console.WriteLine("RequestId:{0}.PromoEvaluateAsync.Request Body:{1}", requestId, serRequest.Replace(Environment.NewLine, ""));

                var promoResponse = await HttpHandler.PostAsync<HttpContent, promores.PromotionResponse>(requestId, url,
                                         _headers, Helper.CreateStringIgnoringDefaultValuesContent<promoreq.PromotionRequest>(request), "PromoEvaluate");

                if (promoResponse != null)
                {
                    Console.WriteLine("RequestId:{0}.PromoEvaluateAsync.Response :{1}", requestId, JsonConvert.SerializeObject(promoResponse).Replace(Environment.NewLine, ""));
                }

                return promoResponse;
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.PromoEvaluateAsync().Message:'{1}'", requestId, e.Message);
            }

            return default(promores.PromotionResponse);
        }

        public async Task<promoDetails.PromotionDetailsResponse> PromotionDetailsGetAsync(string requestId, string username, List<string> promotionIDs)
        {
            StringBuilder queryParam = new StringBuilder();
            try
            {
                SetAuthHeader(username);
                foreach (var promotionID in promotionIDs)
                    queryParam.Append("promotionIds=" + promotionID + "&");
                queryParam.Remove(queryParam.Length - 1, 1);
                string url = string.Format("{0}{1}{2}", _serviceUrl, Constants.EndpointPromoDetails, queryParam);

                Console.WriteLine("RequestId:{0}. PromotionDetailsAsync.RequestURL : {1}", requestId, url);

                var promotionDetails = await HttpHandler.GetAsync<promoDetails.PromotionDetailsResponse>(requestId, url, _headers, "StoreDetailsGet");
                return promotionDetails;

            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.PromotionDetailsGetAsync().Message:'{1}'", requestId, e.Message);
            }

            return default(promoDetails.PromotionDetailsResponse);
        }

        public async Task<GetCouponSeriesResponse> GetCouponSeries(string requestId, string username, string id)
        {
            try
            {
                SetAuthHeader(username);
                string url = string.Format("{0}{1}", _serviceUrl, Constants.EndpointCouponSeriesGet);
                url = string.Format(url, id);

                Console.WriteLine("RequestId:{0}. GetCouponSeries.Request:{1} ", requestId, id);

                var couponseriesresponse = await HttpHandler.GetAsync<GetCouponSeriesResponse>(requestId, url, _headers, "CouponSeriesGet");

                Console.WriteLine("RequestId:{0}. GetCouponSeries.Response for {1} is {2}", requestId, id, JsonConvert.SerializeObject(couponseriesresponse));

                return couponseriesresponse;

            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.GetCouponSeries().Message:'{1}'", requestId, e.Message);
            }

            return default(GetCouponSeriesResponse);
        }

        private void SetAuthHeader(string username)
        {
            //username = "kar_1";
            var byteArray = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", username, _password));
            _headers[Constants.HeaderAuthorization] = string.Format("Basic {0}", Convert.ToBase64String(byteArray));
        }

        private async Task<CustomerStatusResponse> CustomerStatus(string userId, string requestId)
        {
            CustomerStatusResponse custResponse = new CustomerStatusResponse();
            string urlCustomeStatus = string.Format("{0}{1}", _serviceUrl, Constants.EndPointCustomerStatus);
            urlCustomeStatus = string.Format(urlCustomeStatus, userId);
            custResponse = await HttpHandler.GetAsync<CustomerStatusResponse>(requestId, urlCustomeStatus, _headers, "CustomerStatus");
            return custResponse;
        }
        public async Task<CancelTransactionResponse> TransactionCancelAsync(string requestId, string username,string customerId,string cartEvaluationId)
        {
            try
            {
                SetAuthHeader(username);
                string url = string.Format("{0}{1}", _serviceUrl, Constants.EndPointCustomerCancelEvaluation);
                url = string.Format(url, customerId, cartEvaluationId);
                
                var transactionCancelResponse = await HttpHandler.PutAsync<HttpContent, CancelTransactionResponse>(requestId, url,
                                         _headers, null, "TransactionCancel");
                return transactionCancelResponse;
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IntouchService.TransactionCancel().Message:'{1}'", requestId, e.Message);
            }

            return default(CancelTransactionResponse);
        }
       
    }
}
