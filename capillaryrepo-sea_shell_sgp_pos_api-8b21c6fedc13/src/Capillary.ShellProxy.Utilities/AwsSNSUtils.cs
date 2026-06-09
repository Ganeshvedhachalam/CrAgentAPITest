using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System;
using Capillary.ShellProxy.Model.NewRelicModel;

namespace Capillary.ShellProxy.Utilities
{
    public static class AwsSNSUtils
    {
        public static async Task PublishSNSMessage(string requestId, string statusCode, string siteID, string env)
        {
            var snsClient = new AmazonSimpleNotificationServiceClient(Amazon.RegionEndpoint.APSoutheast1);
            try
            {


                var customMetricNR = new NRCustomMetricData
                {
                    eventType = Constants.eventName,
                    StatusCode = statusCode,
                    SiteID = siteID,
                    Country = Constants.malaysia,
                    Environment = env,
                    AppName = Constants.applicationName
                };

                var requestBody = new List<NRCustomMetricData> { customMetricNR };

                var topicRequest = new PublishRequest
                {
                    TopicArn = Constants.snsARN,
                    Message = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody)
                };

                await snsClient.PublishAsync(topicRequest);
            }
            catch (Amazon.Runtime.AmazonServiceException e)
            {
                Console.WriteLine("RequestId:{0}.AmazonServiceException encountered in AwsSNSUtils.PublishSNSMessage().Message:'{1}'", requestId, e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in AwsSNSUtils.PublishSNSMessage().Message:'{1}'", requestId, e.Message);
            }

        }
    }
}