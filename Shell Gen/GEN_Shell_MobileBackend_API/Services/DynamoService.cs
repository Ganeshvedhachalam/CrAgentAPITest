using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using System.Linq;
using System.Diagnostics;
using Newtonsoft.Json;
using GEN_Shell_MobileBackend_API.Utilities;

namespace GEN_Shell_MobileBackend_API.Services
{
    public interface IDBService
    {
        Task<string> GetMobileConfigsAsync(string requestId, string orgID, string environment);
        Task<string> GetAPIAccessKeyAsync(string requestId, string orgID, string environment);

        Task<string> GetQRCodeKeyAsync(string requestId, string orgID, string environment);
        Task PutDynamoQRAsync(string requestId, string CustomerIdentifierNumber, string expiredTime);

    }

    public class DynamoService : IDBService
    {
        private AmazonDynamoDBClient _dynamoClient;
        public DynamoService()
        {
           _dynamoClient = new AmazonDynamoDBClient();
        }


        public async Task<string> GetMobileConfigsAsync(string requestId, string orgID, string environment)
        {
            var sw = Stopwatch.StartNew();
            var jsonResponse = string.Empty;

            try
            {
                var request = new BatchGetItemRequest
                {
                    RequestItems = new Dictionary<string, KeysAndAttributes>
                    {
                        {Constants.ShellMobileBackendKeys ,  new KeysAndAttributes
                            {
                                Keys = new List<Dictionary<string, AttributeValue>>
                                {
                                    new Dictionary<string, AttributeValue>
                                    {
                                        {"OrgId", new AttributeValue{S = orgID }},
                                        {"Environment", new AttributeValue{S = environment }},
                                    }
                                }
                            }
                        }
                    }
                };
            

                BatchGetItemResponse response;
                do
                {
                    Console.WriteLine("RequestId:{0}. GetMobileConfigsAsync.Request for orgID : {1} & Environment : {2}", requestId, orgID, environment);

                    response = await _dynamoClient.BatchGetItemAsync(request);
                    if (response != null)
                    {
                        var responses = response.Responses;
                                                
                        responses.TryGetValue(Constants.ShellMobileBackendKeys, out List<Dictionary<string, AttributeValue>> configkeysResponses);
                        if (configkeysResponses != null && configkeysResponses.Count > 0)
                        {
                            foreach (var configkeysResponse in configkeysResponses)
                            {
                                AttributeValue a;
                                if (configkeysResponse.TryGetValue("Artifacts", out a))
                                {
                                    Console.WriteLine("RequestId:{0}. GetMobileConfigsAsync. Artifacts data has been received from DB", requestId);
                                    return a.S.ToString();
                                }
                            }
                        }

                        // Any unprocessed keys? could happen if you exceed ProvisionedThroughput or some other error.
                        request.RequestItems = response.UnprocessedKeys;
                    }

                } while (response.UnprocessedKeys.Count > 0);

            }
            catch (AmazonServiceException e)
            {
                Console.WriteLine("RequestId:{0}.AmazonServiceException encountered in DynamoService.GetMobileConfigsAsync().Message:'{1}'", requestId, e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in DynamoService.GetMobileConfigsAsync().Message:'{1}'", requestId, e.Message);
            }

            sw.Stop();
            //Console.WriteLine("RequestId:{0}. Failed to fetch mobile configurations configurations from DB",requestId);
            return string.Empty;
        }

        public async Task<string> GetAPIAccessKeyAsync(string requestId, string orgID, string environment)
        {
            var sw = Stopwatch.StartNew();
            var jsonResponse = string.Empty;

            try
            {
                var request = new BatchGetItemRequest
                {
                    RequestItems = new Dictionary<string, KeysAndAttributes>
                    {
                        { "Shell_MobileBackend_APIAccessKeys",  new KeysAndAttributes
                            {
                                Keys = new List<Dictionary<string, AttributeValue>>
                                {
                                    new Dictionary<string, AttributeValue>
                                    {
                                        {"OrgId", new AttributeValue{S = orgID }},
                                        {"Environment", new AttributeValue{S = environment }},
                                    }
                                }
                            }
                        }
                    }
                };


                BatchGetItemResponse response;
                do
                {
                    Console.WriteLine("RequestId:{0}. GetAPIAccessKeyAsync.Request for orgID : {1} & Environment : {2}", requestId, orgID, environment);

                    response = await _dynamoClient.BatchGetItemAsync(request);
                    if (response != null)
                    {
                        var responses = response.Responses;

                        responses.TryGetValue("Shell_MobileBackend_APIAccessKeys", out List<Dictionary<string, AttributeValue>> configkeysResponses);
                        if (configkeysResponses != null && configkeysResponses.Count > 0)
                        {
                            foreach (var configkeysResponse in configkeysResponses)
                            {
                                AttributeValue a;
                                AttributeValue b;
                                AttributeValue c;
                                if (configkeysResponse.TryGetValue("Active", out a) && configkeysResponse.TryGetValue("APIKey", out b)
                                    && configkeysResponse.TryGetValue("LastUpdated", out c))
                                {
                                    Console.WriteLine("RequestId:{0}. GetAPIAccessKeyAsync.Response: API key : {1}, KeyActive : {2}, LastUpdtaed : {3}", requestId, b.S, a.BOOL, c.S);
                                    if(a.BOOL)
                                        return b.S.ToString();
                                    else
                                    {
                                        return string.Empty;
                                    }
                                }
                            }
                        }

                        // Any unprocessed keys? could happen if you exceed ProvisionedThroughput or some other error.
                        request.RequestItems = response.UnprocessedKeys;
                    }

                } while (response.UnprocessedKeys.Count > 0);

            }
            catch (AmazonServiceException e)
            {
                Console.WriteLine("RequestId:{0}.AmazonServiceException encountered in DynamoService.GetAPIAccessKeyAsync().Message:'{1}'", requestId, e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in DynamoService.GetAPIAccessKeyAsync().Message:'{1}'", requestId, e.Message);
            }

            sw.Stop();
            Console.WriteLine("RequestId:{0}. Failed to fetch API access keys from DB");
            return string.Empty;
        }

        private List<Dictionary<string, AttributeValue>> MapItems(string key, IEnumerable<string> items)
        {
            var dataItems = new List<Dictionary<string, AttributeValue>>();
            items = items.Distinct();
            foreach (var item in items)
            {
                dataItems.Add(new Dictionary<string, AttributeValue>{
                    {key, new AttributeValue{S=item}}
                });
            }
            return dataItems;
        }

        public async Task<string> GetQRCodeKeyAsync(string requestId, string CustomerIdentifierNumber, string Datetime)
        {
            var sw = Stopwatch.StartNew();
            var jsonResponse = string.Empty;

            try
            {
                var request = new BatchGetItemRequest
                {
                    RequestItems = new Dictionary<string, KeysAndAttributes>
                    {
                        {Constants.DynamicQRCache ,  new KeysAndAttributes
                            {
                                Keys = new List<Dictionary<string, AttributeValue>>
                                {
                                    new Dictionary<string, AttributeValue>
                                    {
                                        {"CustomerIdentifierNumber", new AttributeValue{S = CustomerIdentifierNumber }},
                                    }
                                }
                            }
                        }
                    }
                };


                BatchGetItemResponse response;
                do
                {
                    Console.WriteLine("RequestId:{0}. GetMobileConfigsAsync.Request for orgID : {1} & Environment : {2}", requestId, CustomerIdentifierNumber, Datetime);

                    response = await _dynamoClient.BatchGetItemAsync(request);
                    if (response != null)
                    {
                        var responses = response.Responses;

                        responses.TryGetValue(Constants.DynamicQRCache, out List<Dictionary<string, AttributeValue>> configkeysResponses);
                        if (configkeysResponses != null && configkeysResponses.Count > 0)
                        {
                            foreach (var configkeysResponse in configkeysResponses)
                            {
                                AttributeValue a;
                                //AttributeValue b;
                                //if (configkeysResponse.TryGetValue("CustomerIdentifierNumber", out a) && configkeysResponse.TryGetValue("DateTime", out b))
                                if (configkeysResponse.TryGetValue("DateTime", out a))
                                {
                                    Console.WriteLine("RequestId:{0}. GetQRCahe. DateTime data has been received from DB", requestId);
                                    return a.N.ToString();
                                }
                            }
                        }

                        // Any unprocessed keys? could happen if you exceed ProvisionedThroughput or some other error.
                        request.RequestItems = response.UnprocessedKeys;
                    }

                } while (response.UnprocessedKeys.Count > 0);

            }
            catch (AmazonServiceException e)
            {
                Console.WriteLine("RequestId:{0}.AmazonServiceException encountered in DynamoService.GetMobileConfigsAsync().Message:'{1}'", requestId, e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in DynamoService.GetMobileConfigsAsync().Message:'{1}'", requestId, e.Message);
            }

            sw.Stop();
            Console.WriteLine("RequestId:{0}. Failed to fetch mobile configurations configurations from DB");
            return string.Empty;
        }

        public async Task PutDynamoQRAsync(string requestId, string CustomerIdentifierNumber, string expiredTime)
        {
            
            try
            {
                Console.WriteLine("RequestId:{0}.Putting Item to DB ", requestId);
                TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                int secondsSinceEpoch = (int)t.TotalSeconds;
                await _dynamoClient.PutItemAsync(new PutItemRequest
                {
                    TableName = Constants.DynamicQRCache,
                    Item = new Dictionary<string, AttributeValue>
                    {
                        { Constants.CustomerIdentifierNumber, new AttributeValue { S = CustomerIdentifierNumber.ToString()}},
                        { Constants.DateTime, new AttributeValue { N =  expiredTime}}
                       
                    }
                });
                Console.WriteLine("RequestId:{0}.Request Reached here ", requestId);
            }
            catch (AmazonServiceException e)
            {
                Console.WriteLine("RequestId:{0}.AmazonServiceException encountered in DynamoService.CachePromogramNameAsync().Message:'{1}'", requestId, e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in DynamoService.CachePromogramNameAsync().Message:'{1}'", requestId, e.Message);
            }

            Console.WriteLine("RequestId:{0}. Data has been pushed to Dynamo", requestId);
        }
    }
}