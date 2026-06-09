using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace GEN_Shell_MobileBackend_API.Utilities
{
    public class Constants
    {
        public const string LambdaVersion = "0";
        //CloudWatch
        public const string MetricNamespace = "shellInd-custom-metrics";
        public const string MetricStatus = "status";
        public const string MetricTime = "time";
        public const string DimEventName = "eventName";
        public const string DimStatusCode = "statusCode";
        public const string DimMetricTime = "time";

        public const string HeaderAuthorization = "Authorization";
        public const string HeaderUserAgent = "User-Agent";

        public const string CustomerIdentifierNumber = "CustomerIdentifierNumber";
        public const string DynamicQRCache = "Shell_DynamicQR_Test";
        public const string DateTime = "DateTime";
        public const string OrgId = "OrgId";
        public const string ShellMobileBackendKeys = "Shell_MobileBackend_Keys";

        public const string MileStoneEarnedType = "MILESTONE_EARN";
        public const string MileStoneTargetTypeCyclicWindow = "CYCLIC_WINDOW";
        public const string MileStoneTargetTypeFixedWindow = "FIXED_CALENDAR_WINDOW";
        //Api Endpoints
        public const string EndpointIntouchSvcUrl = "https://apac2.api.capillarytech.com";
        public const string EndpointCardDetails = "/v2/card?number={0}";
        public const string EndpointCustomerLookup = "/v2/customers/lookup/customerDetails?source=INSTORE&identifierName=mobile&identifierValue={0}";
        public const string EndpointCustomerAdd = "/v2/customers/{0}?source={1}";
        // confirm above too #&accountId={2}"
        public const string EndpointCustomerTransaction = "/v1.1/customer/transactions?format=json&mobile={0}&transaction_id={1}&tenders=true";
        public const string EndpointGetPayment = "/api_gateway/orchestrator/payments/app/v1/fuelling-status?customerId={0}&paymentId={1}";
        public const string MembershipCheck = "/MembershipCheck";
        public const string SendEmail = "/v1.1/communications/email";
        public const string EndpointGetTargetDetails = "/v3/users/{0}/targetGroups?includeInactive=true";
        public const string EndpointGetCustomerPromotion = "/api_gateway/v1/promotions/customer/{0}?limit=10000&format=json&includeExpired=true&includeRedemptions=true&includeSupplementaryPromotions=true";
        public const string EndpointPromoDetails = "/api_gateway/v1/promotions/config?";
        public const string EndpointGetStoreDetails = "/v1.1/store/get?format=json&code={0}";

        //EReceipt 
        public const string MYcurrencyType = "RM";
        public const string SGcurrencyType = "SGD";

        public const string CardImageUrlMastercard = "https://s3-ap-southeast-1.amazonaws.com/fs.capillary.sg/intouch_creative_assets/38cc9cfb-4f32-4b02-aeec-b7cc6c0f.png";
        public const string CardImageUrlVisa = "https://s3-ap-southeast-1.amazonaws.com/fs.capillary.sg/intouch_creative_assets/c050676f-5dc7-46fa-8d97-56027cef.png";
        public const string CardImageUrlDefault = "https://s3-ap-southeast-1.amazonaws.com/fs.capillary.sg/intouch_creative_assets/e96bdc06-2bc4-4c27-97fc-8f2485bb.png";
      
        public const string DynamicVoucherHtml = "<tr> <td align=\"left\" class=\"lable-tag pb wid3\"> {0} </td> <td align=\"right\" class=\"lable-data pb wid4 \" style=\"font-family: 'ShellBook';width: 10%;\">{1}</td><td align=\"right\" class=\"lable-data pb wid4 \" style=\"font-family: 'ShellBook';width: 12%;\"> {2} </td></tr>";
        public const string DynamicMopHtml = "\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t\r\n\t         \t\t\t\t\t\t\r\n\t         \t\t\t\t\t\t <td align=\"left\" class=\"lable-tag pb wid3\">{0}</td>\r\n\t         \t\t\t\t\t\t<td align=\"right\" class=\"lable-data pb wid4 \" style=\"font-family: 'ShellBook';width: 10%;\">{1}</td><td align=\"right\" class=\"lable-data pb wid4 \" style=\"font-family: 'ShellBook';width: 12%;\">{2}</td></tr>";
        public const string DynamicTableVoucherHtml = "\r\n\t         \t\t\t<table border=\"0\" cellspacing=\"0\" cellpadding=\"0\" width=\"100%\">\r\n\t         \t\t\t\t<tbody> {0}  {1} ";

        public const string DynamicTotalSavedHtml = "\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tagbold sixhrd wid1\"><span class=\"sixspan\">Total saved </span></td>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-data green wid2\">{0} {1} </td>\r\n\t         \t\t\t\t\t </ tr >\r\n\t         \t\t\t\t\t";
        public const string DynamicPointsEarnedHtml = "<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tagbold sixhrd pb wid1\"><span class=\"sixspan\">Points earned </span></td>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-data pb green wid2\"> + {0}</td>\r\n\t         \t\t\t\t\t</tr>";
        public const string DynamicItemDiscHtml = "\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tagbold sixhrd wid1\"><span class=\"sixspan\">Discount: </span></td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data black wid2\">{0} {1} </td>\r\n\t         \t\t\t\t\t </ tr >\r\n\t         \t\t\t\t\t";

        //VOCSurvey
        public const string embed = "SUBSCRIPTIONS";
        public const string LastTxnDate = "last_transactiondate";
        public const string RewardDate = "reward";
        public const string surveyFlag = "survey_flag";
        public const string InstoreSource = "INSTORE";
        public const string reward = "True";
        public const string xApiKeyHeader = "x-api-key";
    }
}
