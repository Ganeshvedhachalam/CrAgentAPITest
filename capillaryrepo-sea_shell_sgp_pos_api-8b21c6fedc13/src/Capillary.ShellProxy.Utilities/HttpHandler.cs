using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Capillary.ShellProxy.Utilities
{
    public static class HttpHandler
    {
        public static async Task<T> GetAsync<T>(string requestId, string uri, Dictionary<string, string> headers, string apiName)
        {
            try
            {
                int elapsedtimeInMs = 0;
                HttpClientHandler handler = new HttpClientHandler()
                {
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip
                };
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    foreach (var h in headers)
                        client.DefaultRequestHeaders.Add(h.Key, h.Value);
                    client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");

                    if (uri.Contains(Constants.EndpointPromoDetails))
                        client.DefaultRequestHeaders.Add("Accept-Language", "id_ID");

                    Console.WriteLine("RequestId:{0}.External call:{1}", requestId, uri);

                    var sw = Stopwatch.StartNew();

                    var taskGet = await client.GetAsync(uri).ContinueWith(async x =>
                    {
                        sw.Stop();
                        elapsedtimeInMs = sw.Elapsed.Milliseconds;
                        using (HttpResponseMessage response = x.Result)
                        {
                            Console.WriteLine("RequestId:{0}.External call:{1}. HttpResponse received. ElapsedTimeInMs:{2}", requestId, uri, elapsedtimeInMs);

                            string responseBody = await response.Content.ReadAsStringAsync();
                            Console.WriteLine("RequestId:{0}.External call:{1}. ReadAsStringAsync completed. ElapsedTimeInMs:{2}", requestId, uri, elapsedtimeInMs);

                            Console.WriteLine("RequestId:{0}.External response when calling {1} -->.Statuscode:{2};X-Cap-RequestID:{3};ElapsedTimeInMs={4};Response content:{5}",
                                    requestId, uri, response.StatusCode,
                                    response.Headers.TryGetValues("X-Cap-RequestID", out var values) ? values.FirstOrDefault() : string.Empty,
                                    elapsedtimeInMs, responseBody.Replace(Environment.NewLine, ""));

                            //await SendCloudWatchMetrics(requestId, uri, sw, response, apiName, elapsedtimeInMs);

                            return JsonConvert.DeserializeObject<T>(responseBody);
                        }
                    });
                    return taskGet.Result;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in HttpHandler.GetAsync() when calling {1}.Message:'{2}'", requestId, uri, e.Message);
            }

            return default(T);
        }



        public static async Task<TOut> PostAsync<TIn, TOut>(string requestId, string uri, Dictionary<string, string> headers,
                                                      TIn content, string apiName) where TIn : HttpContent
        {
            try
            {
                HttpClientHandler handler = new HttpClientHandler()
                {
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip
                };
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    if (headers != null)
                        foreach (var h in headers)
                            client.DefaultRequestHeaders.Add(h.Key, h.Value);
                    client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");

                    if (uri.Contains(Constants.EndpointTransactionAdd))
                    {
                        //client.DefaultRequestHeaders.Add("WAIT_FOR_DOWNSTREAM", "false");
                        client.DefaultRequestHeaders.Add("X-CAP-DIRECT-REPLAY", "TRUE");
                    }

                    Console.WriteLine("RequestId:{0}.External call:{1}", requestId, uri);

                    var sw = Stopwatch.StartNew();

                    var taskPost = await client.PostAsync(uri, content).ContinueWith(async x =>
                    {
                        sw.Stop();
                        int elapsedtimeInMs = sw.Elapsed.Milliseconds;
                        using (HttpResponseMessage response = x.Result)
                        {
                            Console.WriteLine("RequestId:{0}.External call:{1}. HttpResponse received. ElapsedTimeInMs:{2}", requestId, uri, elapsedtimeInMs);

                            string responseBody = await response.Content.ReadAsStringAsync();
                            Console.WriteLine("RequestId:{0}.External call:{1}. ReadAsStringAsync completed. ElapsedTimeInMs:{2}", requestId, uri, elapsedtimeInMs);

                            Console.WriteLine("RequestId:{0}.External response when calling {1} -->.Statuscode:{2};X-Cap-RequestID:{3};ElapsedTimeInMs={4};Response content:{5}",
                                    requestId, uri, response.StatusCode,
                                    response.Headers.TryGetValues("X-CAP-REQUEST-ID", out var values) ? values.FirstOrDefault() : string.Empty,
                                    elapsedtimeInMs, responseBody.Replace(Environment.NewLine, ""));

                            //await SendCloudWatchMetrics(requestId ,uri, sw, response, apiName, elapsedtimeInMs);

                            return JsonConvert.DeserializeObject<TOut>(responseBody);
                        }
                    });

                    return taskPost.Result;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in HttpHandler.PostAsync() when calling {1}.Message:'{2}'", requestId, uri, e.Message);
            }

            return default(TOut);
        }

        public static async Task<TOut> PutAsync<TIn, TOut>(string requestId, string uri, Dictionary<string, string> headers,
                                                              TIn content, string apiName) where TIn : HttpContent
        {
            try
            {
                HttpClientHandler handler = new HttpClientHandler()
                {
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip
                };
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    if (headers != null)
                        foreach (var h in headers)
                            client.DefaultRequestHeaders.Add(h.Key, h.Value);
                    client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");

                    if (uri.Contains(Constants.EndpointTransactionAdd))
                    {   
                        client.DefaultRequestHeaders.Add("X-CAP-DIRECT-REPLAY", "TRUE");
                    }

                    Console.WriteLine("RequestId:{0}.External call:{1}", requestId, uri);

                    var sw = Stopwatch.StartNew();

                    var taskPost = await client.PutAsync(uri, content).ContinueWith(async x =>
                    {
                        sw.Stop();
                        int elapsedtimeInMs = sw.Elapsed.Milliseconds;
                        using (HttpResponseMessage response = x.Result)
                        {
                            Console.WriteLine("RequestId:{0}.External call:{1}. HttpResponse received. ElapsedTimeInMs:{2}", requestId, uri, elapsedtimeInMs);

                            string responseBody = await response.Content.ReadAsStringAsync();
                            Console.WriteLine("RequestId:{0}.External call:{1}. ReadAsStringAsync completed. ElapsedTimeInMs:{2}", requestId, uri, elapsedtimeInMs);

                            Console.WriteLine("RequestId:{0}.External response when calling {1} -->.Statuscode:{2};X-Cap-RequestID:{3};ElapsedTimeInMs={4};Response content:{5}",
                                    requestId, uri, response.StatusCode,
                                    response.Headers.TryGetValues("X-CAP-REQUEST-ID", out var values) ? values.FirstOrDefault() : string.Empty,
                                    elapsedtimeInMs, responseBody.Replace(Environment.NewLine, ""));

                            return JsonConvert.DeserializeObject<TOut>(responseBody);
                        }
                    });

                    return taskPost.Result;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in HttpHandler.PutAsync() when calling {1}.Message:'{2}'", requestId, uri, e.Message);
            }

            return default(TOut);
        }


        private static async Task SendCloudWatchMetrics(string requestId, string uri, Stopwatch sw, HttpResponseMessage response, string apiName, int elapsedtimeInMs)
        {
            try
            {
                await CloudwatchUtils.StatusMetric((int)response.StatusCode, apiName);
                Console.WriteLine("RequestId:{0}.StatusMetric added", requestId);

                await CloudwatchUtils.TimeMetric(elapsedtimeInMs, apiName);
                Console.WriteLine("RequestId:{0}.TimeMetric added", requestId);
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in HttpHandler.SendCloudWatchMetrics().Message:'{1}'", requestId, e.Message);
            }
        }
    }
}
