using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using GEN_Shell_MobileBackend_API.Models;
using GEN_Shell_MobileBackend_API.Models.CustomerLookUp;
using GEN_Shell_MobileBackend_API.Services;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace GEN_Shell_MobileBackend_API.Tests
{
    public class FunctionHandlerGetTieringHashTests
    {
        private const string TestSeriesCode = "TESTCODE";
        private const string TestOrgId = "test-org";
        private const string TestEnv = "UAT";
        private const string TestApiKey = "TEST-API-KEY";

        private FunctionHandlerGetTieringHash CreateHandler(Mock<ICrmService> crmMock, Mock<IDBService> dbMock)
        {
            dbMock.Setup(d => d.GetAPIAccessKeyAsync(It.IsAny<string>(), TestOrgId, TestEnv))
                  .ReturnsAsync(TestApiKey);

            dbMock.Setup(d => d.GetMobileConfigsAsync(It.IsAny<string>(), TestOrgId, TestEnv))
                  .ReturnsAsync(JsonConvert.SerializeObject(new DBResponseModel
                  {
                      artifacts = new List<Artifact>
                      {
                          new Artifact
                          {
                              source = "bonuslink",
                              Keys = new List<Key> { new Key { key = "tokenEncyptionKey", value = "test-key" } }
                          }
                      }
                  }));

            return new FunctionHandlerGetTieringHash(crmMock.Object, dbMock.Object, TestSeriesCode);
        }

        private APIGatewayProxyRequest BuildRequest()
        {
            return new APIGatewayProxyRequest
            {
                Headers = new Dictionary<string, string>
                {
                    { "X-Cap-OrgId", TestOrgId },
                    { "X-Cap-Environment", TestEnv },
                    { "X-Cap-APIKey", TestApiKey },
                    { "X-Cap-Profile-Identifier", "9999999999" }
                },
                Body = "{}"
            };
        }

        [Fact]
        public void GetTieringHash_WhenNoCardMatchesSeries_ReturnsNoCardNumberAttachedMessage()
        {
            var crmMock = new Mock<ICrmService>();
            var dbMock = new Mock<IDBService>();

            crmMock.Setup(c => c.CustomersLookupGetAsync(It.IsAny<string>(), It.IsAny<string>()))
                   .ReturnsAsync(new CustomerLookUpResponse
                   {
                       errors = null,
                       cardDetails = new List<CardDetail>
                       {
                           new CardDetail { seriesCode = "DIFFERENT_CODE", cardNumber = "1234567890" }
                       }
                   });

            var handler = CreateHandler(crmMock, dbMock);
            var response = handler.GetTieringHash(BuildRequest(), null);

            Assert.Equal(400, response.StatusCode);
            var body = JsonConvert.DeserializeObject<ErrorResponse>(response.Body);
            Assert.Equal("No Card Number attached to the customer", body.message);
        }
    }
}
