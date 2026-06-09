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

namespace GEN_Shell_MobileBackend_API.Services
{
    public interface IBonusLinkService
    {
        Task<cardRes.MembershipCheckResponse> MemberShipCheckAsync(string requestId, cardRes.MembershipCheckRequest request, string apiKey, string TransactionSignature);
    }
    public class BonusLinkService : IBonusLinkService
    {
        string _bonuslinkUrl;
        Dictionary<string, string> _bonuslinkHeaders;

        public BonusLinkService(string bonuslinkUrl)
        {
            _bonuslinkUrl = bonuslinkUrl;
            _bonuslinkHeaders = new Dictionary<string, string>();
        }
            
        
        public async Task<cardRes.MembershipCheckResponse> MemberShipCheckAsync(string requestId, cardRes.MembershipCheckRequest request, string apiKey, string TransactionSignature)
        {
            try
            {
                SetAuthHeaderBonusLink(apiKey, TransactionSignature);
                string url = string.Format("{0}{1}", _bonuslinkUrl, Constants.MembershipCheck);

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

        private void SetAuthHeaderBonusLink(string apiKey, string transactionSignature)
        {
            _bonuslinkHeaders["ApiKey"] = apiKey;
            _bonuslinkHeaders["TransactionSignature"] = transactionSignature;
        }
    }
}
