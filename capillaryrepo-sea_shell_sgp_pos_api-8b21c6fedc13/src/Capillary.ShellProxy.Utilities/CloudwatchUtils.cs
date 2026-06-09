using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace Capillary.ShellProxy.Utilities
{
    public static class CloudwatchUtils
    {
        public static async Task StatusMetric(int statusCode, string eventName)
        {
            using (var cloudWatchClient = new AmazonCloudWatchClient())
            {
                var metricRequest = new PutMetricDataRequest
                {
                    MetricData = new List<MetricDatum>(),
                    Namespace = Constants.MetricNamespace
                };

                metricRequest.MetricData.Add(new MetricDatum
                {
                    MetricName = Constants.MetricStatus,
                    Unit = StandardUnit.Count,
                    Value = 1,
                    Dimensions = new List<Dimension>
                                                    {
                                                        new Dimension{Name=Constants.DimEventName, Value=eventName},
                                                        new Dimension{Name=Constants.DimStatusCode, Value=statusCode.ToString()}
                                                    }
                });
                PutMetricDataResponse metricResponse = await cloudWatchClient.PutMetricDataAsync(metricRequest);
            }
        }

        public static async Task TimeMetric(int timeInMs, string eventName)
        {
            using (var cloudWatchClient = new AmazonCloudWatchClient())
            {
                var metricRequest = new PutMetricDataRequest
                {
                    MetricData = new List<MetricDatum>(),
                    Namespace = Constants.MetricNamespace
                };

                metricRequest.MetricData.Add(new MetricDatum
                {
                    MetricName = Constants.MetricTime,
                    Unit = StandardUnit.Milliseconds,
                    Value = timeInMs,
                    Dimensions = new List<Dimension>
                                                    {
                                                        new Dimension{Name=Constants.DimEventName, Value=eventName},
                                                    }
                });
                PutMetricDataResponse metricResponse = await cloudWatchClient.PutMetricDataAsync(metricRequest);
            }
        }
    }
}