using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GEN_Shell_MobileBackend_API.Models;
using GEN_Shell_MobileBackend_API.Models.StoreDetails;
using GEN_Shell_MobileBackend_API.Services;
using GEN_Shell_MobileBackend_API.Utilities;
using Microsoft.AspNetCore.Mvc;
//using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;

namespace GEN_Shell_MobileBackend_API
{
    public class FunctionHandlerGetEReceipt
    {
        ICrmService _crmService;
        IDBService _dynamoService;
        string seriesCode;
        string aesEncryptKey = string.Empty;
        string emailTo;        
        public FunctionHandlerGetEReceipt()
        {
            RegionEndpoint region;

            var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION");
            if (!string.IsNullOrEmpty(awsRegion))
                region = RegionEndpoint.GetBySystemName(awsRegion);
            else
                region = RegionEndpoint.GetBySystemName("us-east-1");
            _dynamoService = new DynamoService();

        }
        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse generateEReceipt(APIGatewayProxyRequest request, ILambdaContext context)
        {

            ErrorResponse errorResponse = new ErrorResponse();
            string requestId = Guid.NewGuid().ToString("N");
            string inputString = string.Empty;
            string partnerToken = string.Empty;
            string partnerId = string.Empty;
            string isEmail = "";
            string storeAddress = string.Empty;
            string InvoiceNumber = string.Empty;
            //API Authentication and Header validation 
            try
            {
                var Auth = Helper.API_Authentication(requestId, request);
                Console.WriteLine("RequestId:{0}. API Authentication message : {1}", requestId, Auth.ToString());
                if (Auth != "success")
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(new APIErrorClass { message = Auth }), 401);
                //Get mobile configurations keys
                request.Headers.TryGetValue("X-Cap-OrgId", out string OrgID);
                request.Headers.TryGetValue("X-Cap-Environment", out string Environment);
                request.Headers.TryGetValue("X-Cap-Profile-Identifier", out string Profile_identifier);
                var mobileKeysJson = _dynamoService.GetMobileConfigsAsync(requestId, OrgID, Environment).Result;
                if (string.IsNullOrEmpty(mobileKeysJson))
                {
                    errorResponse.message = "Org Not Found";
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(errorResponse), 401);
                }
                var mobileKeys = JsonConvert.DeserializeObject<DBResponseModel>(mobileKeysJson);
                var crmKeys = mobileKeys.artifacts.Where(c => c.source == "cap_crm").FirstOrDefault();
                if (crmKeys == null)
                {
                    errorResponse.message = "cap_crm keys not found for this org";
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(errorResponse), 401);
                }
                var username = crmKeys.Keys.Where(c => c.key == "username").FirstOrDefault().value;
                var password = crmKeys.Keys.Where(c => c.key == "password").FirstOrDefault().value;
                if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password))
                {
                    errorResponse.message = "credentials not found for this org";
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(errorResponse), 401);
                }
                Console.WriteLine("RequestId:{0} OrgId: {1}, Environment: {2}, Username:{3}", requestId, OrgID, Environment, username);

                _crmService = new IntouchService(Constants.EndpointIntouchSvcUrl, username, password, Constants.LambdaVersion);

                //Extracting source from query parameter
                if (request.QueryStringParameters != null && request.QueryStringParameters.Count > 0)
                {
                    request.QueryStringParameters.TryGetValue("sendemail", out isEmail);
                }
                if (String.IsNullOrEmpty(Profile_identifier))
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest,
                            JsonConvert.SerializeObject(new ErrorResponse { message = "No Profile Identifier Passed", code = 500 }), 500);
                else
                {
                    GetEreciptRequest requestGetEReceipt = new GetEreciptRequest();
                    requestGetEReceipt = JsonConvert.DeserializeObject<GetEreciptRequest>(request.Body);
                    Console.WriteLine("RequestId:{0}.Callig Get customer Transaction", requestId);
                    var customerTransactionResponse = _crmService.GetCustomerTransactionAsync(requestId, Profile_identifier, requestGetEReceipt.transactionId).Result;
                    if (customerTransactionResponse.response.customer.transactions.transaction.Count > 0)
                    {
                        try
                        {
                            //Checking the payment Type
                            var ota_payment_type = customerTransactionResponse.response.customer.transactions.transaction[0].extended_fields.field.Where(x => x.name == "ota_payment_type").Select(y => y.value);
                            Console.WriteLine("RequestId:{0}.OTA_Payment_Type reached", requestId);
                            if (ota_payment_type.Contains("PRE_AUTH"))
                            {
                                Console.WriteLine("RequestId:{0}.PreAuth Type Transaction", requestId);
                                if (customerTransactionResponse.response.status.success == "true")
                                {
                                    emailTo = customerTransactionResponse.response.customer.email;
                                    Console.WriteLine("RequestId:{0}. CustomerTransaction Success", requestId);
                                    var paymentResponse = _crmService.GetPaymentDetailsAsync(requestId, requestGetEReceipt.customerId, requestGetEReceipt.paymentId).Result;

                                    if (paymentResponse != null && paymentResponse.billingLines != null)
                                    {

                                        InvoiceNumber = paymentResponse.billingLines.Where(x => x.Contains("number:")).FirstOrDefault();
                                        int addressLineCount = 0;
                                        //Creating the address field
                                        foreach (var paymentNode in paymentResponse.billingLines)
                                        {

                                            if (paymentNode.Replace(" ", "").Trim().Contains("Site:"))
                                            {
                                                break;
                                            }
                                            if (addressLineCount > 0)
                                            {
                                                storeAddress = storeAddress + paymentNode + ", ";
                                            }
                                            addressLineCount++;
                                        }
                                        storeAddress = storeAddress.Remove(storeAddress.Length - 2);
                                        try
                                        {
                                            string CardImageUrl = string.Empty;
                                            string FormatedDate = customerTransactionResponse.response.customer.transactions.transaction[0].billing_time;
                                            DateTime dt = DateTime.Parse(FormatedDate);
                                            FormatedDate = dt.ToString("dd MMMM yyyy, hh:mm tt");


                                            Double totalSaved = 0;
                                            if (customerTransactionResponse.response.customer.transactions.transaction[0].extended_fields.field.Where(x => x.name == "additional_discount").Select(y => y.value).FirstOrDefault() != null)
                                                totalSaved = Convert.ToDouble(customerTransactionResponse.response.customer.transactions.transaction[0].extended_fields.field.Where(x => x.name == "additional_discount").Select(y => y.value).FirstOrDefault());
                                            if (customerTransactionResponse.response.customer.transactions.transaction[0].tenders.tender != null)
                                            {

                                                var cardDetails = customerTransactionResponse.response.customer.transactions.transaction[0].tenders.tender.Where(x => x.name.ToLower() == "card").FirstOrDefault();
                                                if (cardDetails != null)
                                                {
                                                    var cardType = cardDetails.attributes.attribute.Where(x => x.name == "card_type").FirstOrDefault().value;
                                                    // var cardType1 = cardDetails.Select(x => x.attributes).Select(x => x.attribute.Where(y => y.name == "card_type")).FirstOrDefault().Select(x => x.value).FirstOrDefault();
                                                    if (cardType.ToLower() == "mastercard")
                                                    {
                                                        CardImageUrl = @"https://s3-ap-southeast-1.amazonaws.com/fs.capillary.sg/intouch_creative_assets/38cc9cfb-4f32-4b02-aeec-b7cc6c0f.png";
                                                    }
                                                    else if (cardType.ToLower() == "visa")
                                                    {
                                                        CardImageUrl = @"https://s3-ap-southeast-1.amazonaws.com/fs.capillary.sg/intouch_creative_assets/c050676f-5dc7-46fa-8d97-56027cef.png";
                                                    }
                                                    else
                                                    {
                                                        CardImageUrl = @"https://s3-ap-southeast-1.amazonaws.com/fs.capillary.sg/intouch_creative_assets/e96bdc06-2bc4-4c27-97fc-8f2485bb.png";
                                                    }
                                                }
                                            }

                                            Console.WriteLine("RequestId:{0}.Creating the HTML template", requestId);
                                            //Filling the HTML template
                                            //var tenderVoucher = customerTransactionResponse.response.customer.transactions.transaction[0].tenders.tender.Where(x => x.name.ToLower().Contains("voucher")).Select(y => y);
                                            string dynamictd = string.Empty;
                                            string mop = string.Empty;
                                            var tenderVoucher = customerTransactionResponse.response.customer.transactions.transaction[0].tenders.tender.Select(y => y);
                                            if (tenderVoucher.Count() > 0)
                                            {
                                                foreach (var singleVoucher in tenderVoucher)
                                                {
                                                    if (singleVoucher.name.ToLower().Contains("voucher"))
                                                    {
                                                        double value = 0.00;
                                                        value = Math.Round(Convert.ToDouble(singleVoucher.value), 2);
                                                        dynamictd += "\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t\r\n\t         \t\t\t\t\t\t\r\n\t         \t\t\t\t\t\t <td align=\"left\" class=\"lable-tag pb wid3\">" + singleVoucher.name + "</td>\r\n\t         \t\t\t\t\t\t<td align=\"right\" class=\"lable-data pb wid4 \" style=\"font-family: 'ShellBook';width: 10%;\">RM</td><td align=\"right\" class=\"lable-data pb wid4 \" style=\"font-family: 'ShellBook';width: 12%;\">" + value.ToString("0.00") + "</td></tr>";
                                                    }
                                                    else
                                                    {
                                                        double value = 0.00;
                                                        value = Math.Round(Convert.ToDouble(singleVoucher.value), 2);
                                                        mop += "\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t\r\n\t         \t\t\t\t\t\t\r\n\t         \t\t\t\t\t\t <td align=\"left\" class=\"lable-tag pb wid3\">" + singleVoucher.attributes.attribute.Where(x => x.name.ToLower() == "cardissuercode").Select(y => y.value).FirstOrDefault() + "</td>\r\n\t         \t\t\t\t\t\t<td align=\"right\" class=\"lable-data pb wid4 \" style=\"font-family: 'ShellBook';width: 10%;\">RM</td><td align=\"right\" class=\"lable-data pb wid4 \" style=\"font-family: 'ShellBook';width: 12%;\">" + value.ToString("0.00") + "</td></tr>";

                                                    }
                                                }
                                            }
                                            string totalSavedTD = string.Empty;
                                            string pointsEarnedTD = string.Empty;
                                            string pointsEarned = string.Empty;
                                            if (!String.IsNullOrEmpty(customerTransactionResponse.response.customer.transactions.transaction[0].points.issued.ToString()))
                                            {
                                                pointsEarned = customerTransactionResponse.response.customer.transactions.transaction[0].points.issued.ToString();
                                            }
                                            if (totalSaved != 0)
                                            {
                                                totalSavedTD = "\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tagbold sixhrd wid1\"><span class=\"sixspan\">Total saved </span></td>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-data green wid2\">RM " + totalSaved + "</td>\r\n\t         \t\t\t\t\t </ tr >\r\n\t         \t\t\t\t\t";
                                            }
                                            if (Convert.ToDouble(pointsEarned) != 0)
                                            {
                                                pointsEarnedTD = "<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tagbold sixhrd pb wid1\"><span class=\"sixspan\">Points earned </span></td>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-data pb green wid2\"> + " + customerTransactionResponse.response.customer.transactions.transaction[0].points.issued + "</td>\r\n\t         \t\t\t\t\t</tr>";
                                            }
                                            string tableVoucher = "\r\n\t         \t\t\t<table border=\"0\" cellspacing=\"0\" cellpadding=\"0\" width=\"100%\">\r\n\t         \t\t\t\t<tbody>" + dynamictd + "" + mop + "";
                                            string html = "<!DOCTYPE html>\r\n<html>\r\n    <head>\r\n      \t<title>Shell E-bill</title>\r\n      \t<!-- <link href=\"style.css\" rel=\"stylesheet\" type=\"text/css\" /> -->\r\n      \t<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\r\n      \t<link href=\"https://fonts.googleapis.com/css?family=Lato&amp;display=swap\" rel=\"stylesheet\" />\r\n       \t<!-- <script async=\"\" crossorigin=\"anonymous\" src=\"https://fullstory.com/s/fs.js\"></script> -->\r\n      <style type=\"text/css\">\r\n         @import url('https://fonts.googleapis.com/css2?family=Source+Sans+Pro&display=swap');\r\n         \t\r\n         @font-face {\r\n\t         font-family: 'ShellBold';\r\n\t         src: url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBold.otf'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBold.ttf'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBold.eot'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBold.svg'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBold.woff'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBold.woff2');\r\n\t         font-weight: normal;\r\n\t         font-style: normal;\r\n         }\r\n         @font-face {\r\n\t         font-family: 'ShellHeavy';\r\n\t         src: url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellHeavy.otf'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellHeavy.ttf'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellHeavy.eot'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellHeavy.svg'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellHeavy.woff'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellHeavy.woff2');\r\n\t         font-weight: normal;\r\n\t         font-style: normal;\r\n         }\r\n         @font-face {\r\n\t         font-family: 'ShellBook';\r\n\t         src: url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBook.otf'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBook.ttf'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBook.eot'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBook.svg'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBook.woff'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBook.woff2');\r\n\t         font-weight: normal;\r\n\t         font-style: normal;\r\n         }\r\n\r\n         .ShellBold {\r\n         \tfont-family: 'ShellBold', Arial, Helvetica, 'Roboto';\r\n         \tcolor: #404040;\r\n         \tletter-spacing: 0.15px;\r\n         }\r\n\r\n         .ShellHeavy {\r\n         \tfont-family: 'ShellHeavy', Arial, Helvetica, 'Roboto';\r\n         }\r\n\r\n         .ShellBook {\r\n         \tfont-family: 'ShellBook', Arial, Helvetica, 'Roboto';\r\n         \tcolor: #595959;\r\n         }\r\n\r\n         *{\r\n\t         box-sizing: border-box;\r\n\t        /* font-family: 'ShellBook', Arial, Helvetica, 'Roboto';*/\r\n\t         font-family: 'Source Sans Pro', sans-serif;\r\n\t         font-size: 14px;\r\n\t         font-weight: 400;\r\n\t         color: #595959;\r\n         }\r\n\r\n         .main-layer{\r\n\t         max-width: 420px;\r\n\t         width: 100%;\r\n\t         margin:0px auto;\r\n\t         box-shadow: 0px 0px 30px 0px rgba(0,0,0,.5);\r\n\t         background: white;\r\n         }\r\n         .inner-layer{\r\n         \tpadding: 0px 15px 15px;\r\n         }\r\n         .header{\r\n         \twidth: 100%;\r\n         \ttext-align: center;\r\n         }\r\n         .header h1{\r\n         \tfont-size: 18px;\r\n         \tletter-spacing: 0.15px;\r\n         \tfont-weight: 700;\r\n         \tfont-family: 'ShellBold';\r\n         \tcolor: #404040;\r\n         }\r\n\r\n         .header-logo-img{\r\n\t        display: block;\r\n\t        border: none;\r\n         \tmargin: 0 auto;\r\n\t      }\r\n\r\n         .head_txt{\r\n\t         font-family: 'ShellBold';\r\n\t         font-size:26px;\r\n\t         padding: 15px 25px;\r\n         }\r\n\r\n         h1,h2,h3,h4,h5,h6, .bold{\r\n         \tfont-family: 'ShellBold';\r\n         }\r\n\r\n         .borderline{\r\n         \tborder-bottom: 10px solid #fbce07;\r\n         }\r\n         .borderline-grey{\r\n         \tborder-bottom: 2px solid #e9ecef;\r\n         \twidth: 100%;\r\n         }\r\n         .bill-info h3{\r\n         \tmargin-bottom: 0px;\r\n         }\r\n         table td{\r\n         \t/*padding: 10px 0px;*/\r\n         \tline-height: 30px;\r\n         }\r\n         .lable-tag{\r\n         \twidth: 40%;\t\r\n         \ttext-align: left;\r\n         }\r\n\r\n         .lable-tagbold{\r\n         \tfont-size: 16px;\r\n         \tfont-family: 'ShellBold';\r\n         \tletter-spacing: 0.466667px;\r\n         \tcolor: #404040;\r\n         }\r\n         .sevenhrd{\r\n         \tfont-weight: 700;\r\n         }\r\n         .sixhrd{\r\n         \tfont-weight: 600;\r\n         }\r\n         .lable-data{\r\n         \twidth: 60%;\r\n         \t/*font-size: 20px;*/\r\n         \ttext-align: right;\r\n         }\r\n         .lable-databold{\r\n         \ttext-align: right;\r\n         \tfont-size: 16px;\r\n         \tfont-family: 'ShellBold';\r\n         \tletter-spacing: 0.466667px;\r\n         }\r\n         .header-lable{\r\n         \tbackground: #e9ecef;\r\n         \tpadding: 10px 15px;\r\n         \tfont-size: 12px;\r\n         \tletter-spacing: 0.4px;\r\n         }\r\n         .box-pad{\r\n         \tpadding: 8px 0px 10px;\r\n         }\r\n         .pad{\r\n         \tpadding: 0px;\r\n         }\r\n         .pb{\r\n         \tpadding-bottom: 0px;\r\n         }\r\n         .green{\r\n         \tcolor: #008443;\r\n         \tfont-size: 16px;\r\n         \tletter-spacing: 0.466667px;\r\n         }\r\n         .text-center{\r\n         \ttext-align: center;\r\n         }\r\n         .thankyou p{\r\n         \tmargin: 5px;\r\n    \t\tfont-size: 12px;\r\n         }\r\n         .thankyou h1{\r\n         \tmargin-bottom: 0px;\r\n         }\r\n         .footer-txt p{\r\n         \ttext-align: center;\r\n         \tfont-size: 22px;\r\n         }\r\n         p{\r\n         \tcolor: #404040;\r\n         }\r\n         .wid1{\r\n         \twidth: 60%;\r\n         }\r\n         .wid2{\r\n         \twidth: 40%;\r\n         }\r\n         .wid3{\r\n         \twidth: 78%;\r\n         }\r\n         .thnk{\r\n         \tfont-size: 18px;\r\n         \tfont-weight: 700;\t\r\n         }\r\n         .sixspan{\r\n         \tfont-family: 'ShellBook';\r\n         \tcolor: #595959;\r\n         \tfont-weight: 600;\r\n         }\r\n         @media only screen and (min-width: 324px) and (max-width: 638px) {\r\n         \t.lable-tag, .lable-data{\r\n         \t\tfont-size: 14px;\r\n         \t}\r\n         \t.wid1, .wid3{\r\n         \t\tfont-size: 12.5px !important;\r\n\t        }\r\n\t        .wid2, .wid4{\r\n\t         \tfont-size: 12px !important;\r\n\t        }\r\n         \t.thankyou p{\r\n\t         \tmargin: 0px;\r\n\t         \tfont-size: 13px;\r\n\t         }\r\n\t         p{\r\n\t         \tfont-size: 13px;\r\n\t         }\r\n\t         h1, h3{\r\n\t         \tfont-size: 18px;\r\n\t         }\r\n\t         .footer-txt p{\r\n\t         \ttext-align: center;\r\n\t         \tfont-size: 13px;\r\n\t         }\r\n         }\r\n         @media only screen and (max-width: 323px) {\r\n         \t.inner-layer{\r\n\t         \tpadding: 0px 5px 10px;\r\n\t        }\r\n         \t.lable-tag, .lable-data{\r\n         \t\tfont-size: 14px;\r\n         \t}\r\n         \t.wid1, .wid3{\r\n         \t\tfont-size: 12.5px !important;\r\n\t        }\r\n\t        .wid2, .wid4{\r\n\t         \tfont-size: 12px !important;\r\n\t        }\r\n         \t.thankyou p{\r\n\t         \tmargin: 0px;\r\n\t         \tfont-size: 13px;\r\n\t         }\r\n\t         p{\r\n\t         \tfont-size: 13px;\r\n\t         }\r\n\t         h1 , h3{\r\n\t         \tfont-size: 18px;\r\n\t         }\r\n\t         .footer-txt p{\r\n\t         \ttext-align: center;\r\n\t         \tfont-size: 13px;\r\n\t         }\r\n         }\r\n        </style>\r\n    </head>\r\n    <body>\r\n        <div class=\"main-layer\">\r\n\t        <div class=\"inner-layer\">\r\n\t         \t<div class=\"header\">\r\n\t         \t\t<img src=\"https://s3.amazonaws.com/fileservice.in/intouch_creative_assets/2d28662e-75de-46eb-9599-35359534.png\" class=\"header-logo-img\" >\r\n\t         \t\t<h1>Receipt</h1>\r\n\t         \t</div>\r\n\t         \t<div class=\"borderline\"></div>\r\n\t         \t<div class=\"bill-header-info\">\r\n\t         \t\t<div class=\"station-info\">\r\n\t         \t\t\t<p class=\"sixspan\">" + customerTransactionResponse.response.customer.transactions.transaction[0].store + "</p>\r\n\t\t         \t\t<p class=\"sixspan\">" + paymentResponse.billingLines[0] + "</p>\r\n\t\t         \t\t<p>" + storeAddress + "</p>\r\n\t         \t\t</div>\r\n\t         \t\t<div class=\"table\">\r\n\t         \t\t\t<table border=\"0\" cellspacing=\"0\" cellpadding=\"0\" width=\"100%\">\r\n\t         \t\t\t\t<tbody>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag wid2\">Date</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data wid1\">" + FormatedDate + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag wid2\">Site ID</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data wid1\">" + paymentResponse.stationId + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag wid2\">Pump ID</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data wid1\">" + paymentResponse.pumpNumber + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag wid2\">Pay Token ID</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data wid1\">" + requestGetEReceipt.paymentId + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-tag wid2\">Invoice Number</td>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-data wid1\">" + InvoiceNumber.Split(':').LastOrDefault().Trim() + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t</tbody>\r\n\t         \t\t\t</table>\r\n\t         \t\t</div>\r\n\t         \t</div>\r\n\t        </div>\r\n\t        <div class=\"header-lable\">\r\n\t        \tPurchased Items\r\n\t        </div>\r\n\t        <div class=\"inner-layer\">\r\n\t         \t<div class=\"bill-header-info\">\r\n\t         \t\t<div class=\"table borderline-grey box-pad\">\r\n\t         \t\t\t<table border=\"0\" cellspacing=\"0\" cellpadding=\"0\" width=\"100%\">\r\n\t         \t\t\t\t<tbody>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tagbold sixhrd pb wid1\" style=\"font-family: 'ShellBook';\"> " + customerTransactionResponse.response.customer.transactions.transaction[0].line_items.line_item[0].description + "</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-databold sixhrd pb wid2\" style=\"font-family: 'ShellBook';\">RM " + customerTransactionResponse.response.customer.transactions.transaction[0].line_items.line_item[0].value + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag pad wid1\">" + customerTransactionResponse.response.customer.transactions.transaction[0].line_items.line_item[0].qty + " Litres x RM " + customerTransactionResponse.response.customer.transactions.transaction[0].line_items.line_item[0].rate + "/ Litre</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data pad wid2\"></td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t</tbody>\r\n\t         \t\t\t</table>\r\n\t         \t\t</div>\r\n\t         \t\t<div class=\"table borderline-grey box-pad\">\r\n\t         \t\t\t<table border=\"0\" cellspacing=\"0\" cellpadding=\"0\" width=\"100%\">\r\n\t         \t\t\t\t<tbody>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tagbold sevenhrd pb wid1\">Total paid</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-databold sevenhrd pb wid2\">RM " + customerTransactionResponse.response.customer.transactions.transaction[0].amount + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag pad\"></td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data pad\"></td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t</tbody>\r\n\t         \t\t\t</table>\r\n\t         \t\t</div>\r\n\t         \t\t<div class=\"table box-pad\">\r\n\t         \t\t\t<table border=\"0\" cellspacing=\"0\" cellpadding=\"0\" width=\"100%\">\r\n\t         \t\t\t\t<tbody>" + totalSavedTD + pointsEarnedTD + "\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag pad wid1\">Loyalty Card ***" + paymentResponse.digitalCard.Substring(paymentResponse.digitalCard.Length - 4) + "</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data pad wid2\"></td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t</tbody>\r\n\t         \t\t\t</table>\r\n\t         \t\t</div>\r\n\t         \t</div>\r\n\t        </div>\r\n\t        <div class=\"header-lable\">\r\n\t        \tPaid Via\r\n\t        </div>\r\n\t        <div class=\"inner-layer\">\r\n\t         \t<div class=\"bill-header-info\">\r\n\t         \t\t<div class=\"table borderline-grey box-pad\">\r\n\t   " + tableVoucher + " </tbody></table>     \t\t\t<table border=\"0\" cellspacing=\"0\" cellpadding=\"0\" width=\"100%\">\r\n\t         \t\t\t\t<tbody>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag wid2\">Method of payment</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data wid1\">\r\n\t         \t\t\t\t\t\t<img src=\"" + CardImageUrl + "\"style=\"vertical-align: middle;padding-right: 10px;width: 40px\">\r\n\t         \t\t\t\t\t\t" + paymentResponse.maskedCard + "\r\n\t         \t\t\t\t\t\t</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag pb wid2\">Merchant Auth ID</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data pb wid1\">" + paymentResponse.pspReferenceNumber + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag pad\"></td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data pad\"></td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t</tbody>\r\n\t         \t\t\t</table>\r\n\t         \t\t</div>\r\n\t         \t\t<div class=\"thankyou box-pad text-center\">\r\n\t\t         \t\t<h1 class=\"thnk\">Thank you for visiting Shell</h1>\r\n\t\t         \t\t<p>We look forward to welcoming you again</p>\r\n\t         \t\t</div>\r\n\t         \t\t<div class=\"box-pad footer-txt\">\r\n\t         \t\t</div>\r\n\t         \t</div>\r\n\t        </div>\r\n        <div>\r\n    </body>\r\n</html>";

                                            #region fill_Template_From_File
                                            /* section to fill the template from file */
                                            //string html = "<!DOCTYPE html>\r\n<html>\r\n    <head>\r\n      \t<title>Shell E-bill</title>\r\n      \t<link href=\"style.css\" rel=\"stylesheet\" type=\"text/css\" />\r\n      \t<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\r\n      \t<link href=\"https://fonts.googleapis.com/css?family=Lato&amp;display=swap\" rel=\"stylesheet\" />\r\n       \t<script async=\"\" crossorigin=\"anonymous\" src=\"https://fullstory.com/s/fs.js\"></script>\r\n        <style type=\"text/css\">\r\n         @font-face {\r\n\t         font-family: 'ShellBold';\r\n\t         src: url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBold.otf'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBold.ttf'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBold.eot'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBold.svg'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBold.woff'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBold.woff2');\r\n\t         font-weight: normal;\r\n\t         font-style: normal;\r\n         }\r\n         @font-face {\r\n\t         font-family: 'ShellHeavy';\r\n\t         src: url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellHeavy.otf'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellHeavy.ttf'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellHeavy.eot'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellHeavy.svg'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellHeavy.woff'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellHeavy.woff2');\r\n\t         font-weight: normal;\r\n\t         font-style: normal;\r\n         }\r\n         @font-face {\r\n\t         font-family: 'ShellBook';\r\n\t         src: url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBook.otf'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBook.ttf'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBook.eot'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBook.svg'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBook.woff'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBook.woff2');\r\n\t         font-weight: normal;\r\n\t         font-style: normal;\r\n         }\r\n\r\n         .ShellBold {\r\n         \tfont-family: 'ShellBold', Arial, Helvetica, 'Roboto';\r\n         }\r\n\r\n         .ShellHeavy {\r\n         \tfont-family: 'ShellHeavy', Arial, Helvetica, 'Roboto';\r\n         }\r\n\r\n         .ShellBook {\r\n         \tfont-family: 'ShellBook', Arial, Helvetica, 'Roboto';\r\n         }\r\n\r\n         *{\r\n\t         box-sizing: border-box;\r\n\t         font-family: 'ShellBook', Arial, Helvetica, 'Roboto';\r\n         }\r\n\r\n         .main-layer{\r\n\t         max-width: 650px;\r\n\t         width: 100%;\r\n\t         margin:0px auto;\r\n\t         box-shadow: 0px 0px 30px 0px rgba(0,0,0,.5);\r\n\t         background: white;\r\n         }\r\n         .inner-layer{\r\n         \tpadding: 20px;\r\n         }\r\n         .header{\r\n         \twidth: 100%;\r\n         \ttext-align: center;\r\n         }\r\n\r\n         .header-logo-img{\r\n\t        display: block;\r\n\t        border: none;\r\n         \tmargin: 0 auto;\r\n\t      }\r\n\r\n         .head_txt{\r\n\t         font-family: 'ShellBold';\r\n\t         font-size:26px;\r\n\t         padding: 15px 25px;\r\n         }\r\n\r\n         h1,h2,h3,h4,h5,h6, .bold{\r\n         \tfont-family: 'ShellBold';\r\n         }\r\n\r\n         .borderline{\r\n         \tborder-bottom: 10px solid #fbce07;\r\n         }\r\n         .borderline-grey{\r\n         \tborder-bottom: 2px solid #e9ecef;\r\n         \twidth: 100%;\r\n         }\r\n         .bill-info h3{\r\n         \tmargin-bottom: 0px;\r\n         }\r\n         table td{\r\n         \tpadding: 10px 0px;\r\n         }\r\n         .lable-tag{\r\n         \twidth: 40%;\r\n         \tfont-size: 20px;\r\n         \ttext-align: left;\r\n         }\r\n         .lable-data{\r\n         \twidth: 60%;\r\n         \tfont-size: 20px;\r\n         \ttext-align: right;\r\n         }\r\n         .header-lable{\r\n         \tbackground: #e9ecef;\r\n         \tpadding: 10px 20px;\r\n         \tfont-size: 18px;\r\n         }\r\n         .box-pad{\r\n         \tpadding: 20px 0px;\r\n         }\r\n         .pad{\r\n         \tpadding: 0px;\r\n         }\r\n         .pb{\r\n         \tpadding-bottom: 0px;\r\n         }\r\n         .green{\r\n         \tcolor: #008443;\r\n         }\r\n         .text-center{\r\n         \ttext-align: center;\r\n         }\r\n         .thankyou p{\r\n         \tmargin: 0px;\r\n         \tfont-size: 22px;\r\n         }\r\n         .thankyou h1{\r\n         \tmargin-bottom: 0px;\r\n         }\r\n         .footer-txt p{\r\n         \ttext-align: center;\r\n         \tfont-size: 22px;\r\n         }\r\n         p{\r\n         \tcolor: #404040;\r\n         }\r\n         @media only screen and (min-width: 326px) and (max-width: 638px) {\r\n         \t.lable-tag, .lable-data{\r\n         \t\tfont-size: 14px;\r\n         \t}\r\n         \t.thankyou p{\r\n\t         \tmargin: 0px;\r\n\t         \tfont-size: 13px;\r\n\t         }\r\n\t         p{\r\n\t         \tfont-size: 13px;\r\n\t         }\r\n\t         h1, h3{\r\n\t         \tfont-size: 18px;\r\n\t         }\r\n\t         .footer-txt p{\r\n\t         \ttext-align: center;\r\n\t         \tfont-size: 13px;\r\n\t         }\r\n         }\r\n         @media only screen and (max-width: 323px) {\r\n         \t.lable-tag, .lable-data{\r\n         \t\tfont-size: 14px;\r\n         \t}\r\n         \t.thankyou p{\r\n\t         \tmargin: 0px;\r\n\t         \tfont-size: 13px;\r\n\t         }\r\n\t         p{\r\n\t         \tfont-size: 13px;\r\n\t         }\r\n\t         h1 , h3{\r\n\t         \tfont-size: 18px;\r\n\t         }\r\n\t         .footer-txt p{\r\n\t         \ttext-align: center;\r\n\t         \tfont-size: 13px;\r\n\t         }\r\n         }\r\n        </style>\r\n    </head>\r\n    <body>\r\n        <div class=\"main-layer\">\r\n\t        <div class=\"inner-layer\">\r\n\t         \t<div class=\"header\">\r\n\t         \t\t<img src=\"https://s3.amazonaws.com/fileservice.in/intouch_creative_assets/2d28662e-75de-46eb-9599-35359534.png\" class=\"header-logo-img\" >\r\n\t         \t\t<h1>Receipt</h1>\r\n\t         \t</div>\r\n\t         \t<div class=\"borderline\"></div>\r\n\t         \t<div class=\"bill-header-info\">\r\n\t         \t\t<div class=\"station-info\">\r\n\t         \t\t\t<h3 >" + customerTransactionResponse.response.customer.transactions.transaction[0].store + "</h3>\r\n\t\t         \t\t<p>" + paymentResponse.billingLines[0] + "</p>\r\n\t\t         \t\t<p>" + storeAddress + "</p>\r\n\t         \t\t</div>\r\n\t         \t\t<div class=\"table\">\r\n\t         \t\t\t<table border=\"0\" cellspacing=\"0\" cellpadding=\"0\" width=\"100%\">\r\n\t         \t\t\t\t<tbody>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag\">Date</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data\"> " + customerTransactionResponse.response.customer.transactions.transaction[0].billing_time + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag\">Site ID</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data\">" + paymentResponse.stationId + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag\">Pump ID</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data\">" + paymentResponse.pumpNumber + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag\">Pay Token ID</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data\">" + requestGetEReceipt.paymentId + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-tag\">Invoice Number</td>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-data\">" + InvoiceNumber.Split(':').LastOrDefault().Trim() + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t</tbody>\r\n\t         \t\t\t</table>\r\n\t         \t\t</div>\r\n\t         \t</div>\r\n\t        </div>\r\n\t        <div class=\"header-lable\">\r\n\t        \tPurchased Items\r\n\t        </div>\r\n\t        <div class=\"inner-layer\">\r\n\t         \t<div class=\"bill-header-info\">\r\n\t         \t\t<div class=\"table borderline-grey box-pad\">\r\n\t         \t\t\t<table border=\"0\" cellspacing=\"0\" cellpadding=\"0\" width=\"100%\">\r\n\t         \t\t\t\t<tbody>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-tag pb\">" + customerTransactionResponse.response.customer.transactions.transaction[0].line_items.line_item[0].description + " </td>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-data pb\">RM " + customerTransactionResponse.response.customer.transactions.transaction[0].line_items.line_item[0].amount + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag pad\">" + customerTransactionResponse.response.customer.transactions.transaction[0].line_items.line_item[0].qty + " Litres x RM " + customerTransactionResponse.response.customer.transactions.transaction[0].line_items.line_item[0].amount + " / Litre</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data pad\"></td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag\">- Fuel rewards</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data\">" + customerTransactionResponse.response.customer.transactions.transaction[0].line_items.line_item[0].discount + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag\">- Voucher discount</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data\">" + tenderVoucher + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t</tbody>\r\n\t         \t\t\t</table>\r\n\t         \t\t</div>\r\n\t         \t\t<div class=\"table borderline-grey box-pad\">\r\n\t         \t\t\t<table border=\"0\" cellspacing=\"0\" cellpadding=\"0\" width=\"100%\">\r\n\t         \t\t\t\t<tbody>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-tag pb\">Total paid</td>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-data pb\">RM " + customerTransactionResponse.response.customer.transactions.transaction[0].amount + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag pad\"></td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data pad\"></td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t</tbody>\r\n\t         \t\t\t</table>\r\n\t         \t\t</div>\r\n\t         \t\t<div class=\"table box-pad\">\r\n\t         \t\t\t<table border=\"0\" cellspacing=\"0\" cellpadding=\"0\" width=\"100%\">\r\n\t         \t\t\t\t<tbody>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-tag\">Total saved</td>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-data green\">RM " + customerTransactionResponse.response.customer.transactions.transaction[0].discount + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-tag pb\">Points earned</td>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-data pb green\"> + " + customerTransactionResponse.response.customer.transactions.transaction[0].points.issued + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag pad\">Loyalty Card ***" + paymentResponse.digitalCard.Substring(paymentResponse.digitalCard.Length - 4) + "</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data pad\"></td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t</tbody>\r\n\t         \t\t\t</table>\r\n\t         \t\t</div>\r\n\t         \t</div>\r\n\t        </div>\r\n\t        <div class=\"header-lable\">\r\n\t        \tPayment\r\n\t        </div>\r\n\t        <div class=\"inner-layer\">\r\n\t         \t<div class=\"bill-header-info\">\r\n\t         \t\t<div class=\"table borderline-grey box-pad\">\r\n\t         \t\t\t<table border=\"0\" cellspacing=\"0\" cellpadding=\"0\" width=\"100%\">\r\n\t         \t\t\t\t<tbody>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag\">Method of payment</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data\">\r\n\t         \t\t\t\t\t\t<img src=\"https://s3-ap-southeast-1.amazonaws.com/fs.capillary.sg/intouch_creative_assets/67182cb9-c80b-42b1-8917-5e2dc9de.jpg\" style=\"width: 50px; vertical-align: bottom;\">\r\n\t         \t\t\t\t\t\t" + paymentResponse.maskedCard + "\r\n\t         \t\t\t\t\t\t</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag pb\">Merchant Auth ID</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data pb\">" + paymentResponse.pspReferenceNumber + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag pad\"></td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data pad\"></td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t</tbody>\r\n\t         \t\t\t</table>\r\n\t         \t\t</div>\r\n\t         \t\t<div class=\"thankyou borderline-grey box-pad text-center\">\r\n\t\t         \t\t<img src=\"https://s3.amazonaws.com/fileservice.in/intouch_creative_assets/2d28662e-75de-46eb-9599-35359534.png\" class=\"header-logo-img\" >\r\n\t\t         \t\t<h1>Thank you for visiting Shell</h1>\r\n\t\t         \t\t<p>We look forward to welcoming you again</p>\r\n\t         \t\t</div>\r\n\t         \t\t<div class=\"box-pad footer-txt\">\r\n\t         \t\t\t<p>The difference between your fuelling limit and the <br>transaction value will be returned to your bank within<br> X working days.</p>\r\n\t         \t\t</div>\r\n\t         \t</div>\r\n\t        </div>\r\n        </div>\r\n    </body>\r\n</html>";
                                            //string html = "<!DOCTYPE html>\r\n<html>\r\n    <head>\r\n      \t<title>Shell E-bill</title>\r\n      \t<link href=\"style.css\" rel=\"stylesheet\" type=\"text/css\" />\r\n      \t<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\r\n      \t<link href=\"https://fonts.googleapis.com/css?family=Lato&amp;display=swap\" rel=\"stylesheet\" />\r\n       \t<script async=\"\" crossorigin=\"anonymous\" src=\"https://fullstory.com/s/fs.js\"></script>\r\n        <style type=\"text/css\">\r\n         @font-face {\r\n\t         font-family: 'ShellBold';\r\n\t         src: url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBold.otf'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBold.ttf'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBold.eot'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBold.svg'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBold.woff'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBold.woff2');\r\n\t         font-weight: normal;\r\n\t         font-style: normal;\r\n         }\r\n         @font-face {\r\n\t         font-family: 'ShellHeavy';\r\n\t         src: url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellHeavy.otf'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellHeavy.ttf'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellHeavy.eot'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellHeavy.svg'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellHeavy.woff'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellHeavy.woff2');\r\n\t         font-weight: normal;\r\n\t         font-style: normal;\r\n         }\r\n         @font-face {\r\n\t         font-family: 'ShellBook';\r\n\t         src: url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBook.otf'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBook.ttf'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBook.eot'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBook.svg'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBook.woff'), url('https://d3932rkn7nfr5g.cloudfront.net/js.static.in.ecom.s3.amazonaws.com/sharingan/shellfonts/ShellBook.woff2');\r\n\t         font-weight: normal;\r\n\t         font-style: normal;\r\n         }\r\n\r\n         .ShellBold {\r\n         \tfont-family: 'ShellBold', Arial, Helvetica, 'Roboto';\r\n         }\r\n\r\n         .ShellHeavy {\r\n         \tfont-family: 'ShellHeavy', Arial, Helvetica, 'Roboto';\r\n         }\r\n\r\n         .ShellBook {\r\n         \tfont-family: 'ShellBook', Arial, Helvetica, 'Roboto';\r\n         }\r\n\r\n         *{\r\n\t         box-sizing: border-box;\r\n\t         font-family: 'ShellBook', Arial, Helvetica, 'Roboto';\r\n         }\r\n\r\n         .main-layer{\r\n\t         max-width: 650px;\r\n\t         width: 100%;\r\n\t         margin:0px auto;\r\n\t         box-shadow: 0px 0px 30px 0px rgba(0,0,0,.5);\r\n\t         background: white;\r\n         }\r\n         .inner-layer{\r\n         \tpadding: 20px;\r\n         }\r\n         .header{\r\n         \twidth: 100%;\r\n         \ttext-align: center;\r\n         }\r\n\r\n         .header-logo-img{\r\n\t        display: block;\r\n\t        border: none;\r\n         \tmargin: 0 auto;\r\n\t      }\r\n\r\n         .head_txt{\r\n\t         font-family: 'ShellBold';\r\n\t         font-size:26px;\r\n\t         padding: 15px 25px;\r\n         }\r\n\r\n         h1,h2,h3,h4,h5,h6, .bold{\r\n         \tfont-family: 'ShellBold';\r\n         }\r\n\r\n         .borderline{\r\n         \tborder-bottom: 10px solid #fbce07;\r\n         }\r\n         .borderline-grey{\r\n         \tborder-bottom: 2px solid #e9ecef;\r\n         \twidth: 100%;\r\n         }\r\n         .bill-info h3{\r\n         \tmargin-bottom: 0px;\r\n         }\r\n         table td{\r\n         \tpadding: 10px 0px;\r\n         }\r\n         .lable-tag{\r\n         \twidth: 40%;\r\n         \tfont-size: 20px;\r\n         \ttext-align: left;\r\n         }\r\n         .lable-data{\r\n         \twidth: 60%;\r\n         \tfont-size: 20px;\r\n         \ttext-align: right;\r\n         }\r\n         .header-lable{\r\n         \tbackground: #e9ecef;\r\n         \tpadding: 10px 20px;\r\n         \tfont-size: 18px;\r\n         }\r\n         .box-pad{\r\n         \tpadding: 20px 0px;\r\n         }\r\n         .pad{\r\n         \tpadding: 0px;\r\n         }\r\n         .pb{\r\n         \tpadding-bottom: 0px;\r\n         }\r\n         .green{\r\n         \tcolor: #008443;\r\n         }\r\n         .text-center{\r\n         \ttext-align: center;\r\n         }\r\n         .thankyou p{\r\n         \tmargin: 0px;\r\n         \tfont-size: 22px;\r\n         }\r\n         .thankyou h1{\r\n         \tmargin-bottom: 0px;\r\n         }\r\n         .footer-txt p{\r\n         \ttext-align: center;\r\n         \tfont-size: 22px;\r\n         }\r\n         p{\r\n         \tcolor: #404040;\r\n         }\r\n         @media only screen and (min-width: 326px) and (max-width: 638px) {\r\n         \t.lable-tag, .lable-data{\r\n         \t\tfont-size: 14px;\r\n         \t}\r\n         \t.thankyou p{\r\n\t         \tmargin: 0px;\r\n\t         \tfont-size: 13px;\r\n\t         }\r\n\t         p{\r\n\t         \tfont-size: 13px;\r\n\t         }\r\n\t         h1, h3{\r\n\t         \tfont-size: 18px;\r\n\t         }\r\n\t         .footer-txt p{\r\n\t         \ttext-align: center;\r\n\t         \tfont-size: 13px;\r\n\t         }\r\n         }\r\n         @media only screen and (max-width: 323px) {\r\n         \t.lable-tag, .lable-data{\r\n         \t\tfont-size: 14px;\r\n         \t}\r\n         \t.thankyou p{\r\n\t         \tmargin: 0px;\r\n\t         \tfont-size: 13px;\r\n\t         }\r\n\t         p{\r\n\t         \tfont-size: 13px;\r\n\t         }\r\n\t         h1 , h3{\r\n\t         \tfont-size: 18px;\r\n\t         }\r\n\t         .footer-txt p{\r\n\t         \ttext-align: center;\r\n\t         \tfont-size: 13px;\r\n\t         }\r\n         }\r\n        </style>\r\n    </head>\r\n    <body>\r\n        <div class=\"main-layer\">\r\n\t        <div class=\"inner-layer\">\r\n\t         \t<div class=\"header\">\r\n\t         \t\t<img src=\"https://s3.amazonaws.com/fileservice.in/intouch_creative_assets/2d28662e-75de-46eb-9599-35359534.png\" class=\"header-logo-img\" >\r\n\t         \t\t<h1>Receipt/Tax Invoice</h1>\r\n\t         \t</div>\r\n\t         \t<div class=\"borderline\"></div>\r\n\t         \t<div class=\"bill-header-info\">\r\n\t         \t\t<div class=\"station-info\">\r\n\t         \t\t\t<h3 style=\"font-weight:bold\">" + paymentResponse.billingLines[0] + "</h3>\r\n\t\t         \t\t<h3><p>" + storeAddress + "</p>\r\n\t         \t\t</div>\r\n\t         \t\t<div class=\"table\">\r\n\t         \t\t\t<table border=\"0\" cellspacing=\"0\" cellpadding=\"0\" width=\"100%\">\r\n\t         \t\t\t\t<tbody>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag\">Date</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data\"> " + customerTransactionResponse.response.customer.transactions.transaction[0].billing_time + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag\">Site ID</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data\">" + paymentResponse.stationId + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag\">Pump ID</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data\">" + paymentResponse.pumpNumber + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag\">Pay Token ID</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data\">" + requestGetEReceipt.paymentId + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-tag\">Invoice Number</td>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-data\">" + InvoiceNumber.Split(':').LastOrDefault().Trim() + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t</tbody>\r\n\t         \t\t\t</table>\r\n\t         \t\t</div>\r\n\t         \t</div>\r\n\t        </div>\r\n\t        <div class=\"header-lable\">\r\n\t        \tPurchased Items\r\n\t        </div>\r\n\t        <div class=\"inner-layer\">\r\n\t         \t<div class=\"bill-header-info\">\r\n\t         \t\t<div class=\"table borderline-grey box-pad\">\r\n\t         \t\t\t<table border=\"0\" cellspacing=\"0\" cellpadding=\"0\" width=\"100%\">\r\n\t         \t\t\t\t<tbody>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-tag pb\">" + customerTransactionResponse.response.customer.transactions.transaction[0].line_items.line_item[0].description + " </td>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-data pb\">RM " + customerTransactionResponse.response.customer.transactions.transaction[0].line_items.line_item[0].amount + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag pad\">" + customerTransactionResponse.response.customer.transactions.transaction[0].line_items.line_item[0].qty + " Litres x RM " + customerTransactionResponse.response.customer.transactions.transaction[0].line_items.line_item[0].amount + " / Litre</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data pad\"></td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag\">- Fuel rewards</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data\">" + customerTransactionResponse.response.customer.transactions.transaction[0].line_items.line_item[0].discount + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag\">- Voucher discount</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data\">" + tenderVoucher + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t</tbody>\r\n\t         \t\t\t</table>\r\n\t         \t\t</div>\r\n\t         \t\t<div class=\"table borderline-grey box-pad\">\r\n\t         \t\t\t<table border=\"0\" cellspacing=\"0\" cellpadding=\"0\" width=\"100%\">\r\n\t         \t\t\t\t<tbody>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-tag pb\">Total paid</td>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-data pb\">RM " + customerTransactionResponse.response.customer.transactions.transaction[0].amount + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag pad\"></td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data pad\"></td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t</tbody>\r\n\t         \t\t\t</table>\r\n\t         \t\t</div>\r\n\t         \t\t<div class=\"table box-pad\">\r\n\t         \t\t\t<table border=\"0\" cellspacing=\"0\" cellpadding=\"0\" width=\"100%\">\r\n\t         \t\t\t\t<tbody>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-tag\">Total saved</td>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-data green\">RM " + customerTransactionResponse.response.customer.transactions.transaction[0].discount + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-tag pb\">Points earned</td>\r\n\t         \t\t\t\t\t\t<td class=\"bold lable-data pb green\"> + " + customerTransactionResponse.response.customer.transactions.transaction[0].points.issued + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag pad\">Loyalty Card ***" + paymentResponse.digitalCard.Substring(paymentResponse.digitalCard.Length - 4) + "</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data pad\"></td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t</tbody>\r\n\t         \t\t\t</table>\r\n\t         \t\t</div>\r\n\t         \t</div>\r\n\t        </div>\r\n\t        <div class=\"header-lable\">\r\n\t        \tPayment\r\n\t        </div>\r\n\t        <div class=\"inner-layer\">\r\n\t         \t<div class=\"bill-header-info\">\r\n\t         \t\t<div class=\"table borderline-grey box-pad\">\r\n\t         \t\t\t<table border=\"0\" cellspacing=\"0\" cellpadding=\"0\" width=\"100%\">\r\n\t         \t\t\t\t<tbody>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag\">Method of payment</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data\">\r\n\t         \t\t\t\t\t\t<img src=\"https://s3-ap-southeast-1.amazonaws.com/fs.capillary.sg/intouch_creative_assets/67182cb9-c80b-42b1-8917-5e2dc9de.jpg\" style=\"width: 50px; vertical-align: bottom;\">\r\n\t         \t\t\t\t\t\t" + paymentResponse.maskedCard + "\r\n\t         \t\t\t\t\t\t</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag pb\">Merchant Auth ID</td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data pb\">" + paymentResponse.pspReferenceNumber + "</td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t\t<tr>\r\n\t         \t\t\t\t\t\t<td class=\"lable-tag pad\"></td>\r\n\t         \t\t\t\t\t\t<td class=\"lable-data pad\"></td>\r\n\t         \t\t\t\t\t</tr>\r\n\t         \t\t\t\t</tbody>\r\n\t         \t\t\t</table>\r\n\t         \t\t</div>\r\n\t         \t\t<div class=\"thankyou borderline-grey box-pad text-center\">\r\n\t\t         \t\t<img src=\"https://s3.amazonaws.com/fileservice.in/intouch_creative_assets/2d28662e-75de-46eb-9599-35359534.png\" class=\"header-logo-img\" >\r\n\t\t         \t\t<h1>Thank you for visiting Shell</h1>\r\n\t\t         \t\t<p>We look forward to welcoming you again</p>\r\n\t         \t\t</div>\r\n\t         \t\t<div class=\"box-pad footer-txt\">\r\n\t         \t\t\t<p>The difference between your fuelling limit and the <br>transaction value will be returned to your bank within<br> X working days.</p>\r\n\t         \t\t</div>\r\n\t         \t</div>\r\n\t        </div>\r\n        </div>\r\n    </body>\r\n</html>";

                                            //Reading from File logic (To be implemented)
                                            //string files = StringReplace(paymentResponse, storeAddress);
                                            //Cap_Store
                                            //files.Replace("Cap_Site", paymentResponse.billingLines[0]);
                                            //string template = String.Format(@files, customerTransactionResponse.response.customer.transactions.transaction[0].store, customerTransactionResponse.response.customer.transactions.transaction[0].store, storeAddress);
                                            #endregion

                                            Console.WriteLine("RequestId:{0}. sending html response", requestId);

                                            if (isEmail != null)
                                            {
                                                if (isEmail.ToLower() == "true")
                                                {
                                                    if (String.IsNullOrEmpty(customerTransactionResponse.response.customer.email))
                                                    {
                                                        errorResponse.code = 500;
                                                        errorResponse.message = "EmailId not present";
                                                        return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(errorResponse), 500);
                                                    }
                                                    if (!String.IsNullOrEmpty(isEmail))
                                                    {
                                                        EmailRequest emailRequest = new EmailRequest();
                                                        Console.WriteLine("RequestId:{0} Creating the HTML template", requestId);
                                                        emailRequest.root.email.Add(new Email
                                                        {
                                                            //to = customerTransactionResponse.response.customer.email,
                                                            to = emailTo,
                                                            cc = "",
                                                            from = "e-receipts-my@shell.com",
                                                            body = html,
                                                            subject = "Transaction Receipt for - " + InvoiceNumber
                                                        });
                                                        var emailResponse = _crmService.SendEmailAsync(requestId, emailRequest).Result;
                                                        if (emailResponse != null && emailResponse.response.email[0].item_status.status == true)
                                                        {
                                                            EReceiptResponse receiptResponse = new EReceiptResponse();
                                                            receiptResponse.code = 200;
                                                            receiptResponse.message = emailResponse.response.email[0].item_status.message;
                                                            string response = JsonConvert.SerializeObject(receiptResponse, Formatting.None);
                                                            return funcSendResponse(requestId, HttpStatusCode.OK, response, 200);
                                                        }
                                                        else
                                                            return funcSendResponse(requestId, HttpStatusCode.BadRequest,
                                                             JsonConvert.SerializeObject(new ErrorResponse { message = emailResponse.response.email[0].item_status.message, code = 500 }), 500);
                                                    }
                                                    else
                                                        return funcSendResponse(requestId, HttpStatusCode.BadRequest,
                                                             JsonConvert.SerializeObject(new ErrorResponse { message = "No Email Id Present for the Customer", code = 500 }), 500);
                                                }
                                                else
                                                {
                                                    GetEReceiptResponse eReceiptResponse = new GetEReceiptResponse();
                                                    eReceiptResponse.ReceiptPayload = html;
                                                    string response = JsonConvert.SerializeObject(eReceiptResponse, Formatting.None);
                                                    return funcSendResponse(requestId, HttpStatusCode.OK, response, 200);
                                                }
                                            }
                                            else
                                            {
                                                GetEReceiptResponse eReceiptResponse = new GetEReceiptResponse();
                                                eReceiptResponse.ReceiptPayload = html;
                                                string response = JsonConvert.SerializeObject(eReceiptResponse, Formatting.None);
                                                return funcSendResponse(requestId, HttpStatusCode.OK, response, 200);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine("RequestId {0} , Exception {1}", requestId, "Exception at creating HTML");
                                            return funcSendResponse(requestId, HttpStatusCode.BadRequest,
                                                  JsonConvert.SerializeObject(new ErrorResponse { message = ex.Message, code = 500 }), 500);
                                        }
                                    }
                                    else
                                    {
                                        return funcSendResponse(requestId, HttpStatusCode.BadRequest,
                                      JsonConvert.SerializeObject(new ErrorResponse { message = "No Payment Response", code = 500 }), 500);
                                    }

                                }
                                else
                                    return funcSendResponse(requestId, HttpStatusCode.BadRequest,
                                       JsonConvert.SerializeObject(new ErrorResponse { message = "Transaction Not Found", code = 500 }), 500);
                            }
                            else
                                return funcSendResponse(requestId, HttpStatusCode.BadRequest,
                                    JsonConvert.SerializeObject(new ErrorResponse { message = "Transaction Is not PreAuth Type", code = 500 }), 500);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("RequestId:{0} , Exception {1}", requestId, ex.Message);
                            return funcSendResponse(requestId, HttpStatusCode.BadRequest,
                           JsonConvert.SerializeObject(new ErrorResponse { message = ex.Message, code = 500 }), 500);
                        }
                    }
                    else
                        return funcSendResponse(requestId, HttpStatusCode.BadRequest,
                           JsonConvert.SerializeObject(new ErrorResponse { message = "Transaction Not Found", code = 500 }), 500);


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("RequestId:{0}. Unknown exception occured with message {1}", requestId, ex.Message);
                return funcSendResponse(requestId, HttpStatusCode.OK, ex.Message, 500);
            }
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse generateEReceiptGeneric(APIGatewayProxyRequest request, ILambdaContext context)
        {

            GEN_Shell_MobileBackend_API.Models.ErrorResponse errorResponse = new GEN_Shell_MobileBackend_API.Models.ErrorResponse();
            string requestId = Guid.NewGuid().ToString("N");
            string inputString = string.Empty;
            string partnerToken = string.Empty;
            string partnerId = string.Empty;
            string isEmail = "";
            string storeAddress = string.Empty;
            string InvoiceNumber = string.Empty;
            string market = string.Empty;
            string businessType = string.Empty;
            
            try
            {
                //API Authentication and Header validation 
                var Auth = Helper.API_Authentication(requestId, request);
                Console.WriteLine("RequestId:{0}. API Authentication message : {1}", requestId, Auth.ToString());
                if (Auth != "success")
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(new APIErrorClass { message = Auth }), 401);
                //Get mobile configurations keys
                request.Headers.TryGetValue("X-Cap-OrgId", out string OrgID);
                request.Headers.TryGetValue("X-Cap-Environment", out string Environment);
                request.Headers.TryGetValue("X-Cap-Profile-Identifier", out string Profile_identifier);                
           
                var mobileKeysJson = _dynamoService.GetMobileConfigsAsync(requestId, OrgID, Environment).Result;
                if (string.IsNullOrEmpty(mobileKeysJson))
                {
                    errorResponse.message = "Org Not Found";
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(errorResponse), 401);
                }
                var mobileKeys = JsonConvert.DeserializeObject<DBResponseModel>(mobileKeysJson);
                var crmKeys = mobileKeys.artifacts.Where(c => c.source == "cap_crm").FirstOrDefault();
                if (crmKeys == null)
                {
                    errorResponse.message = "cap_crm keys not found for this org";
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(errorResponse), 401);
                }
                var username = crmKeys.Keys.Where(c => c.key == "username").FirstOrDefault().value;
                var password = crmKeys.Keys.Where(c => c.key == "password").FirstOrDefault().value;
                if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password))
                {
                    errorResponse.message = "credentials not found for this org";
                    return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(errorResponse), 401);
                }
                Console.WriteLine("RequestId:{0} OrgId: {1}, Environment: {2}, Username:{3}", requestId, OrgID, Environment, username);

                _crmService = new IntouchService(Constants.EndpointIntouchSvcUrl, username, password, Constants.LambdaVersion);

                //Extracting source from query parameter
                if (request.QueryStringParameters != null && request.QueryStringParameters.Count > 0)
                {
                    request.QueryStringParameters.TryGetValue("sendemail", out isEmail);
                    request.QueryStringParameters.TryGetValue("market", out market);
                    request.QueryStringParameters.TryGetValue("businessType", out businessType);
                    market = market.ToUpper();
                    businessType = businessType.ToUpper();
                }
                
                if (String.IsNullOrEmpty(Profile_identifier))
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest,
                            JsonConvert.SerializeObject(new ErrorResponse { message = "No Profile Identifier Passed", code = 500 }), 500);
                
                GetEreciptRequest requestGetEReceipt = new GetEreciptRequest();
                requestGetEReceipt = JsonConvert.DeserializeObject<GetEreciptRequest>(request.Body);

                Console.WriteLine("RequestId:{0}.Callig Get customer Transaction", requestId);
                var customerTransactionResponse = _crmService.GetCustomerTransactionAsync(requestId, Profile_identifier, requestGetEReceipt.transactionId).Result;

                if (customerTransactionResponse.response.customer.transactions.transaction.Count > 0)
                {
                    try
                    {
                        string htmlFilePath = Path.Combine(AppContext.BaseDirectory, string.Format("HtmlTemplates/{0}-{1}.html", market, businessType));
                        if (!File.Exists(htmlFilePath))
                            return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(new ErrorResponse { message = "html-template is not found", code = 500 }), 500);

                        if(string.IsNullOrEmpty(customerTransactionResponse.response.customer.transactions.transaction[0].store_code))
                            return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(new ErrorResponse { message = "invalid store_code in transaction", code = 900 }), 500);

                        //Checking the payment Type
                        var ota_payment_type = customerTransactionResponse.response.customer.transactions.transaction[0].extended_fields.field.Where(x => x.name == "ota_payment_type").Select(y => y.value);
                        Console.WriteLine("RequestId:{0}.OTA_Payment_Type reached", requestId);
                        if (ota_payment_type.Contains("PRE_AUTH"))
                        {
                            Console.WriteLine("RequestId:{0}.PreAuth Type Transaction", requestId);
                            if (customerTransactionResponse.response.status.success == "true")
                            {
                                emailTo = customerTransactionResponse.response.customer.email;
                                Console.WriteLine("RequestId:{0}. CustomerTransaction Success", requestId);
                                //var paymentResponse = _crmService.GetPaymentDetailsAsync(requestId, requestGetEReceipt.customerId, requestGetEReceipt.paymentId).Result;
                                var paymentResponseTask = _crmService.GetPaymentDetailsAsync(requestId, requestGetEReceipt.customerId, requestGetEReceipt.paymentId);
                                var getStoreDetailsRespTask = _crmService.GetStoreDetailsAsync(requestId, customerTransactionResponse.response.customer.transactions.transaction[0].store_code);

                                var paymentResponse = paymentResponseTask.Result;
                                var getStoreDetailsResponse = getStoreDetailsRespTask.Result;

                                if (paymentResponse != null && paymentResponse.billingLines != null)
                                    return EReceiptForm(requestId, isEmail, market, htmlFilePath, requestGetEReceipt, customerTransactionResponse, paymentResponse, getStoreDetailsResponse);
                                else
                                {
                                    return funcSendResponse(requestId, HttpStatusCode.BadRequest,
                                  JsonConvert.SerializeObject(new ErrorResponse { message = "No Payment Response", code = 500 }), 500);
                                }
                            }
                            else
                                return funcSendResponse(requestId, HttpStatusCode.BadRequest,
                                   JsonConvert.SerializeObject(new ErrorResponse { message = "Transaction Not Found", code = 500 }), 500);
                        }
                        else
                            return funcSendResponse(requestId, HttpStatusCode.BadRequest,
                                JsonConvert.SerializeObject(new ErrorResponse { message = "Transaction Is not PreAuth Type", code = 500 }), 500);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("RequestId:{0} , Exception {1}", requestId, ex.Message);
                        return funcSendResponse(requestId, HttpStatusCode.BadRequest,
                       JsonConvert.SerializeObject(new ErrorResponse { message = ex.Message, code = 500 }), 500);
                    }
                }
                else
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest,
                       JsonConvert.SerializeObject(new ErrorResponse { message = "Transaction Not Found", code = 500 }), 500);

            }
            catch (Exception ex)
            {
                Console.WriteLine("RequestId:{0}. Unknown exception occured with message {1}", requestId, ex.Message);
                return funcSendResponse(requestId, HttpStatusCode.OK, ex.Message, 500);
            }
        }
        private static string StringReplace(string requestId, PaymentGetResponse paymentResponse, string storeAddress)
        {
            //Currently not implemented used for reading template from file
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Utilities/HtmlTemplate.txt");
            string files = File.ReadAllText(path);
            try
            {
                StreamReader objReader;
                objReader = new StreamReader(path);
                string content = objReader.ReadToEnd();
                objReader.Close();
                content = Regex.Replace(content, "Cap_Site", paymentResponse.billingLines[0]).Replace("storeAddress ", storeAddress);
                StreamWriter writer = new StreamWriter(path);
                writer.Write(content);
                writer.Close();
                return files;
            }
            catch (Exception ex)
            {
                Console.WriteLine("RequestId:{0} Exception {1}", requestId, ex.Message);
                return files;
            }
        }

        Func<string, HttpStatusCode, string, int, APIGatewayProxyResponse> funcSendResponse = (requestId, httpStatusCode, body, returnCode) =>
        {
            Console.WriteLine("RequestId:{0}. Response:{1}", requestId, body.Replace(Environment.NewLine, " "));
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)httpStatusCode,
                Headers = new Dictionary<string, string> { { "content-type", "application/json" }, { "INTG-RequestID", requestId } },
                Body = body.Replace(Environment.NewLine, " ")
            };
        };
        private class EReceiptResponse
        {
            public int code { get; set; }
            public string message { get; set; }

        }
        internal APIGatewayProxyResponse EReceiptForm(string requestId, string isEmail,string market, string htmlFilePath, GetEreciptRequest requestGetEReceipt, GetCustomerTransaction customerTransactionResponse, PaymentGetResponse paymentResponse, GetStoreDetailsResp getStoreDetails)
        {
            GEN_Shell_MobileBackend_API.Models.ErrorResponse errorResponse = new GEN_Shell_MobileBackend_API.Models.ErrorResponse();
            string storeAddress = string.Empty;
            string invoiceNumber = string.Empty;
            string htmlTemplateContent = string.Empty;
            string currencyType = string.Empty;
            string cardImageUrl = string.Empty;
            string totalSavedTD = string.Empty;
            string pointsEarnedTD = string.Empty;
            string pointsEarned = string.Empty;
            string dynamictd = string.Empty;
            string mop = string.Empty;
            string gstNumber = string.Empty;
            string gstContent = string.Empty;
            String gstAmt = string.Empty;
            string cardImageUrlMaster = Constants.CardImageUrlMastercard;
            string cardImageUrlVisa = Constants.CardImageUrlVisa;
            string cardImageUrlDefault = Constants.CardImageUrlDefault;

            Double totalSaved = 0;
           
            try
            {
                // invoiceNumber = paymentResponse.billingLines.Where(x => x.Contains("number:")).FirstOrDefault();
                
                gstNumber = paymentResponse.billingLines.Where(x => x.Contains("gst Reg No :", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                gstContent = paymentResponse.billingLines.Where(x => x.Contains("GST 8% incl", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                gstAmt = customerTransactionResponse.response.customer.transactions.transaction[0].extended_fields.field.Where(x => x.name == "tax_amount").Select(y => y.value).FirstOrDefault();
                string FormatedDate = customerTransactionResponse.response.customer.transactions.transaction[0].billing_time;
                DateTime dt = DateTime.Parse(FormatedDate);
                FormatedDate = dt.ToString("dd MMMM yyyy, hh:mm tt");
                
                Console.WriteLine("RequestId:{0},Path:'{1}'", requestId, htmlFilePath);

                htmlTemplateContent = File.ReadAllText(htmlFilePath);
                Console.WriteLine("RequestId:{0},Loaded HtmlContent:'{1}'", requestId, htmlTemplateContent);

                switch (market.ToUpper())
                {
                    case "SG":
                        invoiceNumber = customerTransactionResponse.response.customer.transactions.transaction[0].number;
                        currencyType = Constants.SGcurrencyType;
                        htmlTemplateContent = !string.IsNullOrEmpty(gstNumber) ? htmlTemplateContent.Replace("{{GstNumber}}", "GST Reg No:" + gstNumber.Split(':').LastOrDefault().Trim()) : htmlTemplateContent.Replace("{{GstNumber}}", "");
                        if (!String.IsNullOrEmpty(gstAmt))
                            htmlTemplateContent = htmlTemplateContent.Replace("{{GstAmt}}", gstAmt);
                        else
                            htmlTemplateContent = htmlTemplateContent.Replace("Inc. GST 8% of {{CT}} ", "").Replace("{{GstAmt}}", "");

                        break;
                    case "MY":
                        invoiceNumber = paymentResponse.billingLines.Where(x => x.Contains("number:")).FirstOrDefault();
                        invoiceNumber = invoiceNumber != null ? invoiceNumber.Split(':').LastOrDefault().Trim() : string.Empty;
                        currencyType = Constants.MYcurrencyType;                        
                        break;
                    default:
                        break;
                }

                Console.WriteLine("RequestId:{0}.Creating the HTML template values", requestId);

                if (customerTransactionResponse.response.customer.transactions.transaction[0].extended_fields.field.Where(x => x.name == "additional_discount").Select(y => y.value).FirstOrDefault() != null)
                    totalSaved = Convert.ToDouble(customerTransactionResponse.response.customer.transactions.transaction[0].extended_fields.field.Where(x => x.name == "additional_discount").Select(y => y.value).FirstOrDefault());
                if (customerTransactionResponse.response.customer.transactions.transaction[0].tenders.tender != null)
                {
                    var cardDetails = customerTransactionResponse.response.customer.transactions.transaction[0].tenders.tender.Where(x => x.name.ToLower() == "card").FirstOrDefault();
                    if (cardDetails != null)
                    {
                        var cardType = cardDetails.attributes.attribute.Where(x => x.name == "card_type").FirstOrDefault().value;                        
                        if (cardType.ToLower() == "mastercard")
                        {
                            cardImageUrl = cardImageUrlMaster;
                        }
                        else if (cardType.ToLower() == "visa")
                        {
                            cardImageUrl = cardImageUrlVisa;
                        }
                        else
                        {
                            cardImageUrl = cardImageUrlDefault;
                        }
                    }
                }

                var tenderVoucher = customerTransactionResponse.response.customer.transactions.transaction[0].tenders.tender != null && customerTransactionResponse.response.customer.transactions.transaction[0].tenders.tender.Count > 0
                    ? customerTransactionResponse.response.customer.transactions.transaction[0].tenders.tender.Select(y => y)
                    : null;
                if (tenderVoucher != null && tenderVoucher.Count() > 0)
                {
                    foreach (var singleVoucher in tenderVoucher)
                    {
                        if (singleVoucher.name.ToLower().Contains("voucher"))
                        {
                            double value = 0.00;
                            value = Math.Round(Convert.ToDouble(singleVoucher.value), 2);
                            dynamictd += DynamicVoucher(currencyType,singleVoucher.name, value);                                
                        }
                        else
                        {
                            double value = 0.00;
                            value = Math.Round(Convert.ToDouble(singleVoucher.value), 2);
                            mop += DynamicMop(currencyType,singleVoucher.attributes.attribute.Where(x => x.name.ToLower() == "cardissuercode").Select(y => y.value).FirstOrDefault(), value);
                        }
                    }
                }

                if (!String.IsNullOrEmpty(customerTransactionResponse.response.customer.transactions.transaction[0].points.issued.ToString()))
                    pointsEarned = customerTransactionResponse.response.customer.transactions.transaction[0].points.issued.ToString();
                
                if (Convert.ToDouble(pointsEarned) != 0)
                    pointsEarnedTD = string.Format(Constants.DynamicPointsEarnedHtml, pointsEarned);

                if (totalSaved != 0)
                    totalSavedTD = string.Format(Constants.DynamicTotalSavedHtml, currencyType, totalSaved);

                string tableVoucher = string.Format(Constants.DynamicTableVoucherHtml, dynamictd, mop);
                //double itemDiscount = customerTransactionResponse.response.customer.transactions.transaction[0].line_items.line_item.Sum(x => Convert.ToDouble(x.discount));
                string itemDiscount = customerTransactionResponse.response.customer.transactions.transaction[0].line_items.line_item.Select(x => x.discount).FirstOrDefault();
                String discountDesc = customerTransactionResponse.response.customer.transactions.transaction[0].line_items.line_item[0].extended_fields.field.Where(x => x.name == "discount_description").Select(y => y.value).FirstOrDefault();
                String companyName = (string)getStoreDetails.response.stores.store[0].custom_fields.field.Where(x => x.name == "customer_sold_name").Select(y => y.value).FirstOrDefault();
                
                string discHtml = !string.IsNullOrEmpty(itemDiscount) && Convert.ToDouble(itemDiscount) > 0 ? string.Format("- {0} {1}", currencyType, itemDiscount) : string.Empty;

                if(getStoreDetails !=null && getStoreDetails.response.stores.store[0].custom_fields != null)
                {
                    storeAddress =
                        string.Format("{0},{1},{2},{3},{4},{5},{6}",
                        (string)getStoreDetails.response.stores.store[0].custom_fields.field.Where(x => x.name == "site_street_address").Select(y => y.value).FirstOrDefault(),
                        (string)getStoreDetails.response.stores.store[0].custom_fields.field.Where(x => x.name == "city_name").Select(y => y.value).FirstOrDefault(),
                        (string)getStoreDetails.response.stores.store[0].custom_fields.field.Where(x => x.name == "territory_name").Select(y => y.value).FirstOrDefault(),
                        (string)getStoreDetails.response.stores.store[0].custom_fields.field.Where(x => x.name == "state").Select(y => y.value).FirstOrDefault(),
                        (string)getStoreDetails.response.stores.store[0].custom_fields.field.Where(x => x.name == "country").Select(y => y.value).FirstOrDefault(),
                        (string)getStoreDetails.response.stores.store[0].custom_fields.field.Where(x => x.name == "postal_code").Select(y => y.value).FirstOrDefault(),
                        (string)getStoreDetails.response.stores.store[0].custom_fields.field.Where(x => x.name == "contact").Select(y => y.value).FirstOrDefault()
                        );                    
                }
                
                storeAddress = Regex.Replace(storeAddress, ",+", ",").TrimEnd(',');

                Console.WriteLine("RequestId:{0}.Creating the HTML template", requestId);

                #region fill_Template_From_FileContent

                htmlTemplateContent = htmlTemplateContent.Replace("{{storeNameFromApi}}", customerTransactionResponse.response.customer.transactions.transaction[0].store).Replace("{{FormatedDate}}", FormatedDate);
                htmlTemplateContent = !string.IsNullOrEmpty(companyName) ? htmlTemplateContent.Replace("{{CompanyName}}",companyName) : htmlTemplateContent.Replace("{{CompanyName}}","");
                htmlTemplateContent = htmlTemplateContent.Replace("{{storeAddressFromApi}}", storeAddress);
                htmlTemplateContent = htmlTemplateContent.Replace("{{paymentResponse.billingLines[0]}}", paymentResponse.billingLines[0]).Replace("{{paymentResponse.stationId}}", paymentResponse.stationId).Replace("{{paymentResponse.pumpNumber}}", paymentResponse.pumpNumber);                                
                htmlTemplateContent = htmlTemplateContent.Replace("{{requestGetEReceipt.paymentId}}", requestGetEReceipt.paymentId);
                
                htmlTemplateContent = htmlTemplateContent.Replace("{{ItemInfo}}", customerTransactionResponse.response.customer.transactions.transaction[0].line_items.line_item[0].description).Replace("{{ItemValue}}", customerTransactionResponse.response.customer.transactions.transaction[0].line_items.line_item[0].value);                
                htmlTemplateContent = htmlTemplateContent.Replace("{{qty}}", customerTransactionResponse.response.customer.transactions.transaction[0].line_items.line_item[0].qty).Replace("{{rate}}", customerTransactionResponse.response.customer.transactions.transaction[0].line_items.line_item[0].rate);                
                htmlTemplateContent = htmlTemplateContent.Replace("{{TotalAmt}}", customerTransactionResponse.response.customer.transactions.transaction[0].amount);
                htmlTemplateContent = htmlTemplateContent.Replace("{{pspReferenceNumber}}", paymentResponse.pspReferenceNumber);
                htmlTemplateContent = !string.IsNullOrEmpty(tableVoucher) ? htmlTemplateContent.Replace("{{D.TableVoucher}}", tableVoucher) : htmlTemplateContent.Replace("{{D.TableVoucher}}", "");
                htmlTemplateContent = totalSaved != 0 ? htmlTemplateContent.Replace("{{D.Saved}}", totalSavedTD) : htmlTemplateContent.Replace("{{D.Saved}}", "");
                htmlTemplateContent = Convert.ToDouble(pointsEarned) != 0 ? htmlTemplateContent.Replace("{{D.PointsIssued}}", pointsEarnedTD) : htmlTemplateContent.Replace("{{D.PointsIssued}}", "");
                //htmlTemplateContent = Convert.ToDouble(itemDiscount) > 0 ? htmlTemplateContent.Replace("{{D.ItemDiscountAmt}}", discHtml) : htmlTemplateContent.Replace("{{D.ItemDiscountAmt}}", "");
                htmlTemplateContent = !string.IsNullOrEmpty(discHtml) ? htmlTemplateContent.Replace("{{D.ItemDiscountAmt}}", discHtml) : htmlTemplateContent.Replace("{{D.ItemDiscountAmt}}", "");
                htmlTemplateContent = !string.IsNullOrEmpty(discountDesc) ? htmlTemplateContent.Replace("{{D.DiscountDesc}}",discountDesc) : htmlTemplateContent.Replace("{{D.DiscountDesc}}","");
                
                htmlTemplateContent = htmlTemplateContent.Replace("{{CT}}", currencyType);
                htmlTemplateContent = !string.IsNullOrEmpty(invoiceNumber)? htmlTemplateContent.Replace("{{InvoiceNumber}}", invoiceNumber) : htmlTemplateContent.Replace("{{InvoiceNumber}}", "").Replace("Invoice Number", "");
                htmlTemplateContent = htmlTemplateContent.Replace("{{CardImageUrl}}", cardImageUrl).Replace("{{maskedCard}}", paymentResponse.maskedCard);
                htmlTemplateContent = htmlTemplateContent.Replace("{{4digits}}", paymentResponse.digitalCard.Substring(paymentResponse.digitalCard.Length - 4));
                
                #endregion

                string html = htmlTemplateContent;
                Console.WriteLine("RequestId:{0}. sending html response", requestId);

                if (isEmail != null)
                {
                    if (isEmail.ToLower() == "true")
                    {
                        if (String.IsNullOrEmpty(customerTransactionResponse.response.customer.email))
                        {
                            errorResponse.code = 500;
                            errorResponse.message = "EmailId not present";
                            return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(errorResponse), 500);
                        }
                        if (!String.IsNullOrEmpty(isEmail))
                        {
                            EmailRequest emailRequest = new EmailRequest();
                            Console.WriteLine("RequestId:{0} Creating the HTML template", requestId);
                            emailRequest.root.email.Add(new Email
                            {
                                to = emailTo,
                                cc = "",
                                from = "e-receipts-my@shell.com",
                                body = html,
                                subject = "Transaction Receipt for - " + invoiceNumber
                            });
                            var emailResponse = _crmService.SendEmailAsync(requestId, emailRequest).Result;
                            if (emailResponse != null && emailResponse.response.email[0].item_status.status == true)
                            {
                                EReceiptResponse receiptResponse = new EReceiptResponse();
                                receiptResponse.code = 200;
                                receiptResponse.message = emailResponse.response.email[0].item_status.message;
                                string response = JsonConvert.SerializeObject(receiptResponse, Formatting.None);
                                return funcSendResponse(requestId, HttpStatusCode.OK, response, 200);
                            }
                            else
                                return funcSendResponse(requestId, HttpStatusCode.BadRequest,
                                 JsonConvert.SerializeObject(new ErrorResponse { message = emailResponse.response.email[0].item_status.message, code = 500 }), 500);
                        }
                        else
                            return funcSendResponse(requestId, HttpStatusCode.BadRequest,
                                 JsonConvert.SerializeObject(new ErrorResponse { message = "No Email Id Present for the Customer", code = 500 }), 500);
                    }
                    else
                    {
                        GetEReceiptResponse eReceiptResponse = new GetEReceiptResponse();
                        eReceiptResponse.ReceiptPayload = html;
                        string response = JsonConvert.SerializeObject(eReceiptResponse, Formatting.None);
                        return funcSendResponse(requestId, HttpStatusCode.OK, response, 200);
                    }
                }
                else
                {
                    GetEReceiptResponse eReceiptResponse = new GetEReceiptResponse();
                    eReceiptResponse.ReceiptPayload = html;
                    string response = JsonConvert.SerializeObject(eReceiptResponse, Formatting.None);
                    return funcSendResponse(requestId, HttpStatusCode.OK, response, 200);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("RequestId {0} , Exception {1}", requestId, "Exception at creating HTML");
                return funcSendResponse(requestId, HttpStatusCode.BadRequest,
                      JsonConvert.SerializeObject(new ErrorResponse { message = ex.Message, code = 500 }), 500);
            }
        }
        internal string DynamicVoucher(string currencyType,string voucherName,Double voucherValue)
        {
            string dynamicVoucherTable = string.Format(Constants.DynamicVoucherHtml, voucherName, currencyType, voucherValue);
            return dynamicVoucherTable;
        }
        internal string DynamicMop(string currencyType,string voucherName, Double voucherValue)
        {
            string dynamicMopTable = string.Format(Constants.DynamicMopHtml, voucherName, currencyType, voucherValue);            
            return dynamicMopTable;
        }
    }
}
