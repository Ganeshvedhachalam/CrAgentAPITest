using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.APIGatewayEvents;

using Capillary.ShellProxy;
using Capillary.ShellProxy.API;

namespace Capillary.ShellProxy.Tests
{
    public class FunctionTest
    {
        public FunctionTest()
        {
        }

        [Fact]
        public void TAdd_Mobile_Redeem_TAddPromo_Test()
        {
            TestLambdaContext context = new TestLambdaContext();

            var sw = Stopwatch.StartNew();

            APIGatewayProxyRequest request = new APIGatewayProxyRequest();
            request.QueryStringParameters = new Dictionary<string,string>();
            request.QueryStringParameters.Add("source","retry");
            request.Body = "{\r\n    \"metaData\": {\r\n        \"totalCount\": 1\r\n    },\r\n    \"objects\": [\r\n        {\r\n            \"requestData\": {\r\n                \"requestType\": \"RetailTransaction\",\r\n                \"workstationID\": \"SG002701\",\r\n                \"requestID\": \"64417311\",\r\n                \"cartEvaluationID\": \"62b5985831fa9e2f1d876ff5\",\r\n                \"referenceNumber\": \"{7D5AB8EE-ADBF-473C-9643-6980BCA9C390}\",\r\n                \"batchID\": 1\r\n            },\r\n            \"siteData\": {\r\n                \"countryCode\": \"SG\",\r\n                \"siteID\": \"0084\"\r\n            },\r\n            \"posData\": {\r\n                \"posTimeStamp\": \"2022-06-24T18:55:59+08:00\",\r\n                \"clerkID\": \"2700\",\r\n                \"transactionNumber\": \"00662\",\r\n                \"TerminalID\": \"01\"\r\n            },\r\n            \"tenders\": [\r\n                {\r\n                    \"tenderID\": 1,\r\n                    \"methodOfPaymentID\": 6,\r\n                    \"methodOfPayment\": \"Cash\",\r\n                    \"totalAmount\": 31.11,\r\n                    \"currencyCode\": \"SGD\",\r\n                    \"cardPAN\": \"\",\r\n                    \"acquirerID\": \"CASH\",\r\n                    \"netTenderAmount\": 31.1,\r\n                    \"substractDiscountAmount\": false\r\n                }\r\n            ],\r\n            \"customerData\": [\r\n                {\r\n                    \"customerDataType\": \"DigitalLoyaltyCard\",\r\n                    \"customerDataValue\": \"D0000000000000001\",\r\n                    \"loyaltyType\": \"Shell\"\r\n                }\r\n            ],\r\n            \"totalAmount\": 31.11,\r\n            \"totalDiscountAmount\": 0,\r\n            \"extraDiscountAmount\": 0,\r\n            \"currencyCode\": \"SGD\",\r\n            \"saleItems\": [\r\n                {\r\n                    \"itemID\": 1,\r\n                    \"saleItemType\": \"Sale\",\r\n                    \"productCode\": \"26\",\r\n                    \"categoryCode\": \"0136\",\r\n                    \"amount\": 31.11,\r\n                    \"originalAmount\": 32.75,\r\n                    \"vatRate\": 7,\r\n                    \"unitMeasure\": \"LTR\",\r\n                    \"unitPrice\": 3.56,\r\n                    \"quantity\": 9.2,\r\n                    \"additionalProductCode\": \"26\",\r\n                    \"additionalProductInfo\": \"VPower\",\r\n                    \"saleChannel\": 1,\r\n                    \"markDownIndicator\": false,\r\n                    \"PriceAdjustments\": [\r\n                        {\r\n                            \"referenceID\": \"eyJwcm9tb3Rpb25JZCI6IjYyMmVlZTFjMTkzNmI1NDkyZDE3Yjk0ZSIsImRpc2NvdW50IjoiMS42Mzc0OTkiLCJhbW91bnQiOiIzMi43NTAwMDAiLCJkaXNjb3VudEFwcGxpZWRRdHkiOiI5LjIwMDAwMCIsInByb21vdGlvbkFwcGxpZWRRdHkiOiI5LjIwMDAwMCIsInJlZGVtcHRpb25Db3VudCI6MSwic2t1IjoiMjYiLCJ2ZXJzaW9uIjoidjEifQ==\",\r\n                            \"promotionType\": \"lineitem\",\r\n                            \"priceAdjustmentID\": 1,\r\n                            \"priceAdjustmentType\": \"RealtimeOffer-A\",\r\n                            \"amount\": 1.64,\r\n                            \"unitPrice\": 0.18,\r\n                            \"quantity\": 9.2,\r\n                            \"categoryCode\": \"0136\",\r\n                            \"additionalProductCode\": \"26\",\r\n                            \"reason\": \"5% off canopy Discount_New\",\r\n                            \"loyaltyOfferID\": \"622eee1c1936b5492d17b94e\"\r\n                        }\r\n                    ],\r\n                    \"loyaltyOffers\": [\r\n                        {\r\n                            \"loyaltyOfferID\": \"622eee1c1936b5492d17b94e\",\r\n                            \"loyaltyOfferDescription\": \"5% off canopy Discount_New\",\r\n                            \"referenceID\": \"eyJwcm9tb3Rpb25JZCI6IjYyMmVlZTFjMTkzNmI1NDkyZDE3Yjk0ZSIsImRpc2NvdW50IjoiMS42Mzc0OTkiLCJhbW91bnQiOiIzMi43NTAwMDAiLCJkaXNjb3VudEFwcGxpZWRRdHkiOiI5LjIwMDAwMCIsInByb21vdGlvbkFwcGxpZWRRdHkiOiI5LjIwMDAwMCIsInJlZGVtcHRpb25Db3VudCI6MSwic2t1IjoiMjYiLCJ2ZXJzaW9uIjoidjEifQ==\",\r\n                            \"promotionType\": \"lineitem\",\r\n                            \"loyaltyOfferAmount\": 0\r\n                        }\r\n                    ]\r\n                }\r\n            ]\r\n        }\r\n    ]\r\n}";

            var functionHandlerTxn = new FunctionHandlerAddTxn();

            APIGatewayProxyResponse response = functionHandlerTxn.TransactionAdd(request, context);

            sw.Stop();
            var elapsedtimeInMs = sw.Elapsed.Milliseconds;
            Console.WriteLine(elapsedtimeInMs.ToString());
            Assert.Equal(200, response.StatusCode);
            Assert.NotEmpty(response.Body);
        }


        [Fact]
        public void OGet_Mobile_Test()
        {
            TestLambdaContext context = new TestLambdaContext();
            Dictionary<string, string> _headers = new Dictionary<string, string>();
            APIGatewayProxyRequest request = new APIGatewayProxyRequest();
            _headers.Add("X-Cap-Origin-Source", "OTa1");
            
            //request.Body = "{\r\n    \"customerData\": [\r\n        {\r\n            \"customerDataType\": \"MobileNumber\",\r\n            \"customerDataValue\": \"+918281170451\"\r\n        }\r\n    ],\r\n    \"posData\": {\r\n        \"posTimeStamp\": \"2020-09-04T17:41:45+05:30\",\r\n        \"transactionNumber\": \"26\"\r\n    },\r\n    \"requestData\": {\r\n        \"requestID\": \"25\",\r\n        \"requestType\": \"OfferQuery\",\r\n        \"workstationID\": \"1\"\r\n    },\r\n    \"saleItems\": [\r\n        {\r\n            \"additionalProductCode\": \" \",\r\n            \"additionalProductInfo\": \"V-PowerUNL\",\r\n            \"amount\": 1518.42,\r\n            \"categoryCode\": \"10\",\r\n            \"itemID\": 1,\r\n            \"originalAmount\": 1518.42,\r\n            \"productCode\": \"F0101MU04\",\r\n            \"quantity\": 15.51,\r\n            \"saleItemType\": \"Sale\",\r\n            \"unitMeasure\": \"LTR\",\r\n            \"unitPrice\": 9.79,\r\n            \"vatRate\": 14.5\r\n        }\r\n    ],\r\n    \"siteData\": {\r\n        \"countryCode\": \"IN\",\r\n        \"siteID\": \"10048854\"\r\n    },\r\n    \"tenders\": [],\r\n    \"totalAmount\": 1518.42\r\n}";
            request.Body = "{\r\n    \"requestData\": {\r\n        \"requestType\": \"OfferQuery\",\r\n        \"workstationID\": \"87000201\",\r\n        \"requestID\": \"6326Test4010\",\r\n        \"referenceNumber\": \"{90F62378-5A11-42F9-93D9-6C63AFD8C560}\",\r\n        \"cartEvaluationId\": \"\"\r\n    },\r\n    \"siteData\": {\r\n        \"countryCode\": \"SG\",\r\n        \"siteID\": \"0001\"\r\n    },\r\n    \"posData\": {\r\n        \"posTimeStamp\": \"2022-11-02T18:13:33+08:00\"\r\n    },\r\n    \"tenders\": [],\r\n    \"customerData\": [\r\n        {\r\n            \"customerDataType\": \"DigitalLoyaltyCard\",\r\n            \"customerDataValue\": \"R/o02wg5OdTN+s3U8k6UPD/SXa5yCBs1mNT4g/k6Kdh9QtmXCRbSl9cAbmzeEay3KfWs79GnMAo=\",\r\n            \"loyaltyType\": \"Shell\"\r\n        }\r\n    ],\r\n    \"totalAmount\": 100,\r\n    \"remainder\": 100,\r\n    \"predictedTender\": {\r\n        \"methodOfPayment\": \"Cash\",\r\n        \"acquirer\": \"CASH\",\r\n        \"substractDiscountAmount\": false,\r\n        \"amount\": 100\r\n    },\r\n    \"saleItems\": [\r\n        {\r\n            \"itemID\": 1,\r\n            \"saleItemType\": \"Sale\",\r\n            \"productCode\": \"22\",\r\n            \"categoryCode\": \"0136\",\r\n            \"amount\": 100,\r\n            \"originalAmount\": 100,\r\n            \"vatRate\": 100,\r\n            \"unitMeasure\": \"LTR\",\r\n            \"unitPrice\": 100,\r\n            \"quantity\": 1,\r\n            \"additionalProductCode\": \"22\",\r\n            \"additionalProductInfo\": \"FuelSave95\",\r\n            \"saleChannel\": 1,\r\n            \"markDownIndicator\": false\r\n        }\r\n    ]\r\n}";
            //request.Body = "{\r\n    \"requestData\": {\r\n        \"requestType\": \"OfferQuery\",\r\n        \"workstationID\": \"87000201\",\r\n        \"requestID\": \"6326Test4010\",\r\n        \"referenceNumber\": \"{90F62378-5A11-42F9-93D9-6C63AFD8C560}\",\r\n        \"cartEvaluationId\": \"\"\r\n    },\r\n    \"siteData\": {\r\n        \"countryCode\": \"SG\",\r\n        \"siteID\": \"0001\"\r\n    },\r\n    \"posData\": {\r\n        \"posTimeStamp\": \"2022-11-02T18:13:33+08:00\"\r\n    },\r\n    \"tenders\": [],\r\n    \"customerData\": [\r\n        {\r\n            \"customerDataType\": \"MobileNumber\",\r\n            \"customerDataValue\": \"+918281170451\",\r\n            \"loyaltyType\": \"Shell\"\r\n        }\r\n    ],\r\n    \"totalAmount\": 100,\r\n    \"remainder\": 100,\r\n    \"predictedTender\": {\r\n        \"methodOfPayment\": \"Cash\",\r\n        \"acquirer\": \"CASH\",\r\n        \"substractDiscountAmount\": false,\r\n        \"amount\": 100\r\n    },\r\n    \"saleItems\": [\r\n        {\r\n            \"itemID\": 1,\r\n            \"saleItemType\": \"Sale\",\r\n            \"productCode\": \"22\",\r\n            \"categoryCode\": \"0136\",\r\n            \"amount\": 100,\r\n            \"originalAmount\": 100,\r\n            \"vatRate\": 100,\r\n            \"unitMeasure\": \"LTR\",\r\n            \"unitPrice\": 100,\r\n            \"quantity\": 1,\r\n            \"additionalProductCode\": \"22\",\r\n            \"additionalProductInfo\": \"FuelSave95\",\r\n            \"saleChannel\": 1,\r\n            \"markDownIndicator\": false\r\n        }\r\n    ]\r\n}";
            request.Headers = _headers;
            var functionHandlerOffers = new FunctionHandlerPromoOffers();
            APIGatewayProxyResponse response = functionHandlerOffers.GetPromoOffers(request, context);
            Assert.Equal(200, response.StatusCode);
        }

        
        [Fact]
        public void TGet_GiftCatalog()
        {
            TestLambdaContext context = new TestLambdaContext();
            APIGatewayProxyRequest request = new APIGatewayProxyRequest();
            request.Body = "{\r\n  \"requestData\": {\r\n    \"requestType\": \"OfferQuery\",\r\n    \"workstationID\": \"87000101\",\r\n    \"requestID\": \"57964135\",\r\n    \"referenceNumber\": \"{CDEFEB56-332C-499A-A6F1-DE10D8F6735B}\"\r\n  },\r\n  \"siteData\": {\r\n    \"countryCode\": \"SG\",\r\n    \"siteID\": \"0084\"\r\n  },\r\n  \"posData\": {\r\n    \"posTimeStamp\": \"2022-07-18T15:52:29+08:00\"\r\n  },\r\n  \"tenders\": [{\r\n      \"tenderID\": 1,\r\n      \"methodOfPaymentID\": 6,\r\n      \"methodOfPayment\": \"cash\",\r\n      \"totalAmount\": 10,\r\n      \"currencyCode\": \"SGD\",\r\n      \"cardPAN\": \"\",\r\n      \"acquirerID\": \"CASH\",\r\n      \"netTenderAmount\": 10,\r\n      \"substractDiscountAmount\": false\r\n    },\r\n    {\r\n      \"tenderID\": 1,\r\n      \"methodOfPaymentID\": 6,\r\n      \"methodOfPayment\": \"cash\",\r\n      \"totalAmount\": 30,\r\n      \"currencyCode\": \"SGD\",\r\n      \"cardPAN\": \"\",\r\n      \"acquirerID\": \"CASH\",\r\n      \"netTenderAmount\": 30,\r\n      \"substractDiscountAmount\": false\r\n    }],\r\n  \"customerData\": [\r\n    {\r\n      \"customerDataType\": \"DigitalLoyaltyCard\",\r\n      \"customerDataValue\": \"D00000000000000011\",\r\n      \"loyaltyType\": \"Shell\"\r\n    }\r\n  ],\r\n  \"totalAmount\": 50.0,\r\n  \"remainder\" : 37.5,\r\n  \"predictedTender\": {\r\n    \"methodOfPayment\": \"Cash\",\r\n    \"acquirer\": \"CASH\",\r\n    \"substractDiscountAmount\": false,\r\n    \"amount\" : 35\r\n  },\r\n  \"priceAdjustments\": [\r\n    \r\n  ],\r\n  \"saleItems\": [\r\n    {\r\n      \"itemID\": 1,\r\n      \"saleItemType\": \"Sale\",\r\n      \"productCode\": \"26\",\r\n      \"categoryCode\": \"0136\",\r\n      \"amount\": 50.0,\r\n      \"originalAmount\": 50.0,\r\n      \"vatRate\": 8.0,\r\n      \"unitMeasure\": \"LTR\",\r\n      \"unitPrice\": 5.0,\r\n      \"quantity\": 10.0,\r\n      \"additionalProductCode\": \"26\",\r\n      \"additionalProductInfo\": \"VPower\",\r\n      \"saleChannel\": 1,\r\n      \"markDownIndicator\": false\r\n    }\r\n  ]\r\n}";
            var functionHandlergiftCatalog = new FunctionHandlerGiftCatalog();
            APIGatewayProxyResponse response = functionHandlergiftCatalog.GiftCatalog(request, context);
            Assert.Equal(200, response.StatusCode);
            Assert.NotEmpty(response.Body);
         }
    }
}
 