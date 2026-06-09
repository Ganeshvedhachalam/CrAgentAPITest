using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Capillary.ShellProxy.Model;
using Capillary.ShellProxy.Utilities;
using System.Linq;
using System.Diagnostics;

namespace Capillary.ShellProxy.Service
{
    public interface IDBService
    {
        Task<List<ProductLine>> GetMapAsync(string requestId, List<ProductLine> productLines);
        Task<SiteLocation> GetSiteKeysAsync(string requestId, string siteID);
        Task<List<TenderInformation>> GetTenderDetailsAsync(string requestId, List<string> acquirerIDs);
        Task CachePromogramNameAsync(string requestId, string customerID, string programName);
        Task CacheTotalAmountAsync(string requestId, string txnNumber, string amount);
        Task<double> GetTotalAmountAsync(string requestId, string txnNumber);
        Task<List<ProductLine>> GetMapCacheAsync(string requestId, List<ProductLine> productLines, string identifierValue);

    }

    public class DynamoService : IDBService
    {
        private static AmazonDynamoDBClient _dynamoClient;
        string _tenderTableName;
        string _locationTableName;

        public DynamoService(string tenderTableName = "", string locationTableName = "")
        {
            _dynamoClient = new AmazonDynamoDBClient();
            _tenderTableName = tenderTableName;
            _locationTableName = locationTableName;
        }

        public async Task<List<ProductLine>> GetMapAsync(string requestId, List<ProductLine> productLines)
        {
            var sw = Stopwatch.StartNew();
            
            var siteClientID = string.Empty;

            if(productLines.Count==0) return productLines;

            try
            {
                var request = new BatchGetItemRequest
                {
                    RequestItems = new Dictionary<string, KeysAndAttributes>
                    {
                        {Constants.TableProductsMappings ,  new KeysAndAttributes{Keys = MapItems(Constants.AttributeCrmProductId,productLines.Select(m=>m.CrmProductCode))}},
                        {Constants.TableLocationsMappings ,  new KeysAndAttributes{Keys = MapItems(Constants.AttributeCrmLocationtId,new[] {productLines.First().CrmLocationCode})}}
                    }
                };

                BatchGetItemResponse response;
                do
                {
                    Console.WriteLine("RequestId:{0}. GetMapAsync.Request for {1} product mappings",requestId, productLines.Count);

                    response = await _dynamoClient.BatchGetItemAsync(request);
                    if(response!=null)
                    {
                        var responses = response.Responses;

                        responses.TryGetValue(Constants.TableLocationsMappings, out List<Dictionary<string,AttributeValue>> locationsResponse);
                        if(locationsResponse!=null && locationsResponse.Count>0)
                        {
                            if(locationsResponse[0].TryGetValue(Constants.AttributeCrmLocationtId, out AttributeValue k) &&
                               locationsResponse[0].TryGetValue(Constants.AttributeClientID, out AttributeValue v))
                               {
                                    Console.WriteLine("RequestId:{0}. GetMapAsync.Response Location-->{1}:{2}", requestId,k.S,v.S);
                                    siteClientID =  v.S;
                               }
                        }

                        responses.TryGetValue(Constants.TableProductsMappings, out List<Dictionary<string,AttributeValue>> productsResponse);
                        foreach(var item in productsResponse)
                        {
                            if(item.TryGetValue(Constants.AttributeCrmProductId, out AttributeValue k) &&
                               item.TryGetValue(Constants.AttributeCatergoryId, out AttributeValue c))
                            {
                                var product = productLines.FirstOrDefault(p=>p.CrmProductCode == k.S);
                                if(product!=null)
                                {
                                    //product.EcomProductId = v.S;
                                    product.CategoryId = c.S;
                                    product.SiteClientKey = siteClientID;
                                    Console.WriteLine("RequestId:{0}. GetMapAsync.Response Product-->{1}:{2}", requestId,k.S,c.S);
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
                Console.WriteLine("RequestId:{0}.AmazonServiceException encountered in DynamoService.GetMapAsync().Message:'{1}'",requestId, e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in DynamoService.GetMapAsync().Message:'{1}'",requestId, e.Message);
            }

            sw.Stop();
            Console.WriteLine("RequestId:{0}. {1} products retreived from Dynamo. ElapsedTimeInMs:{2}", requestId, productLines.Count, sw.ElapsedMilliseconds);
            return productLines;
        }

         public async Task<List<ProductLine>> GetMapCacheAsync(string requestId, List<ProductLine> productLines, string identifierValue)
        {
            var sw = Stopwatch.StartNew();
            
            var siteClientID = string.Empty;

            if(productLines.Count==0) return productLines;

            try
            {
                var request = new BatchGetItemRequest
                {
                    RequestItems = new Dictionary<string, KeysAndAttributes>
                    {
                        {Constants.TableProductsMappings ,  new KeysAndAttributes{Keys = MapItems(Constants.AttributeCrmProductId,productLines.Select(m=>m.CrmProductCode))}},
                        {Constants.TableLocationsMappings ,  new KeysAndAttributes{Keys = MapItems(Constants.AttributeCrmLocationtId,new[] {productLines.First().CrmLocationCode})}}
                    }
                };
                if(!string.IsNullOrEmpty(identifierValue))
                    request.RequestItems.Add(Constants.TableCustomerCacheMappings ,  new KeysAndAttributes{Keys = MapItems(Constants.AttributeIdentifierValue,new[] {identifierValue})});

                BatchGetItemResponse response;
                do
                {
                    Console.WriteLine("RequestId:{0}. GetMapAsync.Request for {1} product mappings",requestId, productLines.Count);

                    response = await _dynamoClient.BatchGetItemAsync(request);
                    if(response!=null)
                    {
                        var responses = response.Responses;

                        responses.TryGetValue(Constants.TableLocationsMappings, out List<Dictionary<string,AttributeValue>> locationsResponse);
                        if(locationsResponse!=null && locationsResponse.Count>0)
                        {
                            if(locationsResponse[0].TryGetValue(Constants.AttributeCrmLocationtId, out AttributeValue k) &&
                               locationsResponse[0].TryGetValue(Constants.AttributeClientID, out AttributeValue v))
                               {
                                    Console.WriteLine("RequestId:{0}. GetMapCacheAsync.Response Location-->{1}:{2}", requestId,k.S,v.S);
                                    siteClientID =  v.S;
                               }
                        }
                        productLines.FirstOrDefault().SiteClientKey = siteClientID;

                        responses.TryGetValue(Constants.TableProductsMappings, out List<Dictionary<string,AttributeValue>> productsResponse);
                        foreach(var item in productsResponse)
                        {
                            if(item.TryGetValue(Constants.AttributeCrmProductId, out AttributeValue k) &&
                               item.TryGetValue(Constants.AttributeCatergoryId, out AttributeValue c))
                            {
                                var product = productLines.FirstOrDefault(p=>p.CrmProductCode == k.S);
                                if(product!=null)
                                {
                                    //product.EcomProductId = v.S;
                                    product.CategoryId = c.S;
                                    product.SiteClientKey = siteClientID;
                                    Console.WriteLine("RequestId:{0}. GetMapAsync.Response Product-->{1}:{2}", requestId,k.S,c.S);
                                }
                            }
                        }

                        responses.TryGetValue(Constants.TableCustomerCacheMappings, out List<Dictionary<string,AttributeValue>> cacheResponse);
                        if(cacheResponse != null)
                        {
                            foreach(var item in cacheResponse)
                            {
                                if(item.TryGetValue(Constants.AttributeProgramName, out AttributeValue m))
                                {
                                    foreach(var product in productLines)
                                        product.ProgramName = m.S;
                                    Console.WriteLine("RequestId:{0}. Program name retrivied from DB is {1}", requestId,m.S);
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
                Console.WriteLine("RequestId:{0}.AmazonServiceException encountered in DynamoService.GetMapAsync().Message:'{1}'",requestId, e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in DynamoService.GetMapAsync().Message:'{1}'",requestId, e.Message);
            }

            sw.Stop();
            Console.WriteLine("RequestId:{0}. {1} products retreived from Dynamo. ElapsedTimeInMs:{2}", requestId, productLines.Count, sw.ElapsedMilliseconds);
            return productLines;
        }

        public async Task<SiteLocation> GetSiteKeysAsync(string requestId, string siteID)
        {
            var sw = Stopwatch.StartNew();
            if(string.IsNullOrEmpty(siteID)) return null;

            try
            {
                var request = new BatchGetItemRequest
                {
                    RequestItems = new Dictionary<string, KeysAndAttributes>
                    {
                        {_locationTableName ,  new KeysAndAttributes{Keys = MapItems(Constants.AttributeCrmLocationtId,new[] {siteID})}}
                    }
                };

                BatchGetItemResponse response;
                do
                {
                    Console.WriteLine("RequestId:{0}. GetSiteKeysAsync.Request for siteID - {1}",requestId, siteID);

                    response = await _dynamoClient.BatchGetItemAsync(request);
                    if(response!=null)
                    {
                        var responses = response.Responses;

                        responses.TryGetValue(_locationTableName, out List<Dictionary<string,AttributeValue>> locationsResponse);
                        if(locationsResponse!=null && locationsResponse.Count>0)
                        {
                            if(locationsResponse[0].TryGetValue(Constants.AttributeCrmLocationtId, out AttributeValue k) &&
                               locationsResponse[0].TryGetValue(Constants.AttributeClientID, out AttributeValue v) &&
                                locationsResponse[0].TryGetValue(Constants.AttributeGlobalSiteId, out AttributeValue m))
                               {
                                    Console.WriteLine("RequestId:{0}. GetSiteKeysAsync.Response Location-->{1}:{2}:{3}", requestId,k.S,v.S,m.S);
                                    return new SiteLocation{
                                        CRMLocId = siteID,
                                        ClientID = v.S,
                                        GlobalSiteId = m.S
                                    };
                               }
                        }

                        // Any unprocessed keys? could happen if you exceed ProvisionedThroughput or some other error.
                        request.RequestItems = response.UnprocessedKeys;
                    }

                } while (response.UnprocessedKeys.Count > 0);
            }
            catch (AmazonServiceException e)
            {
                Console.WriteLine("RequestId:{0}.AmazonServiceException encountered in DynamoService.GetSiteKeysAsync().Message:'{1}'",requestId, e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in DynamoService.GetSiteKeysAsync().Message:'{1}'",requestId, e.Message);
            }

            sw.Stop();
            return default(SiteLocation);
        }

        public async Task<double> GetTotalAmountAsync(string requestId, string txnNumber)
        {
            var txnAmount = "0";
            if(string.IsNullOrEmpty(txnNumber)) return 0.0d;
            try
            {
                var request = new BatchGetItemRequest
                {
                    RequestItems = new Dictionary<string, KeysAndAttributes>
                    {
                        {Constants.TableTxnAmountMappings ,  new KeysAndAttributes{Keys = MapItems(Constants.AttributeTxnNumber,new[] {txnNumber})}}
                    }
                };

                BatchGetItemResponse response;
                do
                {
                    Console.WriteLine("RequestId:{0}. GetTotalAmountAsync.Request for Txnnumber : {1}",requestId, txnNumber);

                    response = await _dynamoClient.BatchGetItemAsync(request);
                    if(response!=null)
                    {
                        var responses = response.Responses;

                        responses.TryGetValue(Constants.TableTxnAmountMappings, out List<Dictionary<string,AttributeValue>> txnResponse);
                        if(txnResponse!=null && txnResponse.Count>0)
                        {
                            if(txnResponse[0].TryGetValue(Constants.AttributeAmount, out AttributeValue k))
                            {
                                Console.WriteLine("RequestId:{0}. GetTotalAmountAsync.Response TxnNumber : Amount -->{1}:{2}", requestId,txnNumber,k.S);
                                txnAmount =  k.S;
                            }
                        }

                        // Any unprocessed keys? could happen if you exceed ProvisionedThroughput or some other error.
                        request.RequestItems = response.UnprocessedKeys;
                    }

                } while (response.UnprocessedKeys.Count > 0);
                
            }
            catch (AmazonServiceException e)
            {
                Console.WriteLine("RequestId:{0}.AmazonServiceException encountered in DynamoService.GetTotalAmountAsync().Message:'{1}'",requestId, e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in DynamoService.GetTotalAmountAsync().Message:'{1}'",requestId, e.Message);
            }
            return Convert.ToDouble(txnAmount);
        }

        public async Task<List<TenderInformation>> GetTenderDetailsAsync(string requestId, List<string> acquirerIDs)
        {
            var sw = Stopwatch.StartNew();
            var tendersDetails = new List<TenderInformation>();
            var siteClientID = string.Empty;

            if(acquirerIDs.Count == 0 ) return null;

            try
            {
                var request = new BatchGetItemRequest
                {
                    RequestItems = new Dictionary<string, KeysAndAttributes>
                    {
                        {_tenderTableName ,  new KeysAndAttributes{Keys = MapItems(Constants.AttributeAcquirer_Id, acquirerIDs)}}
                    }
                };

                BatchGetItemResponse response;
                do
                {
                    Console.WriteLine("RequestId:{0}. GetTenderDetailsAsync.Request for AcquirerIDs - {1}",requestId,  string.Join( ",", acquirerIDs.ToArray()));

                    response = await _dynamoClient.BatchGetItemAsync(request);
                    if(response!=null)
                    {
                        var responses = response.Responses;

                        responses.TryGetValue(_tenderTableName, out List<Dictionary<string,AttributeValue>> tenderResponse);
                        if(tenderResponse != null && tenderResponse.Count > 0)
                        {
                             foreach(var tender in tenderResponse)
                             {
                                 AttributeValue a;
                                 AttributeValue b;
                                 AttributeValue c;
                                 AttributeValue d;
                                 if(tender.TryGetValue(Constants.AttributeMOP_ID, out  a) &&
                                    tender.TryGetValue(Constants.AttributeMode, out  b) &&
                                    tender.TryGetValue(Constants.AttributeMOP_Name, out  c) &&
                                    tender.TryGetValue(Constants.AttributeAcquirer_Id, out  d))
                                    {
                                        Console.WriteLine("RequestId:{0}. GetTenderDetailsAsync.Response TenderInformation-->{1}:{2}:{3}", requestId,a.S,b.S,c.S);
                                        tendersDetails.Add(new TenderInformation{
                                            MOP_Name = c.S,
                                            MOP_ID = a.S,
                                            Mode = b.S,
                                            Acquirer_Id = d.S
                                        });
                                    }
                             }
                        }

                        // Any unprocessed keys? could happen if you exceed ProvisionedThroughput or some other error.
                        request.RequestItems = response.UnprocessedKeys;
                    }

                } while (response.UnprocessedKeys.Count > 0);
                return tendersDetails;
            }
            catch (AmazonServiceException e)
            {
                Console.WriteLine("RequestId:{0}.AmazonServiceException encountered in DynamoService.GetTenderDetailsAsync().Message:'{1}'",requestId, e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in DynamoService.GetTenderDetailsAsync().Message:'{1}'",requestId, e.Message);
            }

            sw.Stop();
            Console.WriteLine("RequestId:{0}. Failed to fetch tender information");
            return null;
        }


        public async Task CachePromogramNameAsync(string requestId, string customerID, string programName)
        {

            try
            {
                TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                int secondsSinceEpoch = (int)t.TotalSeconds;
                await _dynamoClient.PutItemAsync(new PutItemRequest
                {
                    TableName = Constants.TableCustomerCacheMappings,
                    Item = new Dictionary<string, AttributeValue>
                    {
                        { Constants.AttributeIdentifierValue, new AttributeValue { S = customerID.ToString()}},
                        { Constants.AttributeProgramName, new AttributeValue { S =  programName}},
                        { Constants.AttributeDateTime, new AttributeValue { N = secondsSinceEpoch.ToString()}},
                    }
                });

                
            }
            catch (AmazonServiceException e)
            {
                Console.WriteLine("RequestId:{0}.AmazonServiceException encountered in DynamoService.CachePromogramNameAsync().Message:'{1}'",requestId, e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in DynamoService.CachePromogramNameAsync().Message:'{1}'",requestId, e.Message);
            }

            Console.WriteLine("RequestId:{0}. Data has been pushed to Dynamo", requestId);
        }

        public async Task CacheTotalAmountAsync(string requestId, string txnNumber, string amount)
        {

            try
            {
                TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                int secondsSinceEpoch = (int)t.TotalSeconds;
                await _dynamoClient.PutItemAsync(new PutItemRequest
                {
                    TableName = Constants.TableTxnAmountMappings,
                    Item = new Dictionary<string, AttributeValue>
                    {
                        { Constants.AttributeTxnNumber, new AttributeValue { S = txnNumber.ToString()}},
                        { Constants.AttributeAmount, new AttributeValue { S =  amount}}
                    }
                });

                
            }
            catch (AmazonServiceException e)
            {
                Console.WriteLine("RequestId:{0}.AmazonServiceException encountered in DynamoService.CacheTotalAmountAsync().Message:'{1}'",requestId, e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in DynamoService.CacheTotalAmountAsync().Message:'{1}'",requestId, e.Message);
            }

            Console.WriteLine("RequestId:{0}. Data has been pushed to Dynamo", requestId);
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
    }
}