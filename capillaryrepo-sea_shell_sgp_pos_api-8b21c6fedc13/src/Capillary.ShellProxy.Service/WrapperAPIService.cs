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
using Capillary.ShellProxy.Model.GetRewards.Response;
using Capillary.ShellProxy.Model.IssueReward.Request;
using Capillary.ShellProxy.Model.IssueReward.Response;
using promoreq = Capillary.ShellProxy.Model.PromotionModel.Request;
using promores = Capillary.ShellProxy.Model.PromotionModel.Response;

namespace Capillary.ShellProxy.Service
{
    public interface IWrapperAPIService
    {
        Task<GetRewardsResponse> GetRewardsAsync(string requestId);
        Task<IssueRewardResponse> IssueRewardAsync(string requestId, IssueRewardRequest issueRewardRequest);
        
    }

    public class WrapperAPIService : IWrapperAPIService
    {
        //HttpHandler _handler;
        string _wrapperUrl;
        string _username;
        string _password;
        Dictionary<string, string> _headers;


        public WrapperAPIService(string wrapperUrl, string userName, string password, string lambdaVersion)
        {
            _wrapperUrl = wrapperUrl;
            _password = password;
            _username = userName;
            //_handler = new HttpHandler();

            _headers = new Dictionary<string, string>{
                {Constants.HeaderUserAgent,string.Format(Constants.HeaderUserAgent,lambdaVersion)},
                {Constants.HeaderAuthorization, string.Empty}};
        }


        public async Task<GetRewardsResponse> GetRewardsAsync(string requestId)
        {
            try
            {
                WrapperAuth();
                string url = string.Format("{0}{1}", _wrapperUrl, Constants.EndpointGetRewards);

                var getRewardsResponse = await HttpHandler.GetAsync<GetRewardsResponse>(requestId, url, _headers, "CouponSeriesGet");

                Console.WriteLine("RequestId:{0}. GetRewardsAsync.Response is {1}", requestId, JsonConvert.SerializeObject(getRewardsResponse));

                return getRewardsResponse;

            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IWrapperAPIService.GetRewardsAsync().Message:'{1}'", requestId, e.Message);
            }

            return default(GetRewardsResponse);
        }
        public async Task<IssueRewardResponse> IssueRewardAsync(string requestId, IssueRewardRequest issueRewardRequest)
        {
            try
            {
                WrapperAuth();
                string url = string.Format("{0}{1}", _wrapperUrl, Constants.EndpointIssueRewards);

               Console.WriteLine("RequestId:{0}.IssueReward Request Body:'{1}'", requestId, (JsonConvert.SerializeObject(issueRewardRequest,
                            Newtonsoft.Json.Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            })).Replace(Environment.NewLine, ""));                                             
              

                var getRewardsResponse = await HttpHandler.PostAsync<HttpContent, IssueRewardResponse>(requestId, url,
                                         _headers, Helper.CreateStringContent<IssueRewardRequest>(issueRewardRequest), "IssueReward");

                Console.WriteLine("RequestId:{0}. IssueRewardAsync.Response is {1}", requestId, JsonConvert.SerializeObject(getRewardsResponse));

                return getRewardsResponse;

            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in IWrapperAPIService.IssueRewardAsync().Message:'{1}'", requestId, e.Message);
            }

            return default(IssueRewardResponse);
        }

        private void WrapperAuth()
        {
            //username = "kar_1";
            var byteArray = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", _username, _password));
            _headers[Constants.HeaderAuthorization] = string.Format("Basic {0}", Convert.ToBase64String(byteArray));
        }
    }
}
