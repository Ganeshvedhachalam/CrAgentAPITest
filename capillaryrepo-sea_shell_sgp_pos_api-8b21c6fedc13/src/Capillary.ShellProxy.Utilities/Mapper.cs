using Transactionreq = Capillary.ShellProxy.Model.TransactionModel.v2.Request;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Capillary.ShellProxy.Model.TransactionModel.Request;
using Capillary.ShellProxy.Model.ShellTransactionModel.Response;
using Capillary.ShellProxy.Model.TransactionModel.v2.Response;
using System.Net;
using Capillary.ShellProxy.Model.OffersModel.Request;
using Capillary.ShellProxy.Model;
using Newtonsoft.Json;
using Capillary.ShellProxy.Model.ShellTransactionModel.Request;
using req = Capillary.ShellProxy.Model.ShellTransactionModel.Request;
using Capillary.ShellProxy.Model.CouponModel;
using Capillary.ShellProxy.Model.OffersModel.Response;
using System.Linq;
using Capillary.ShellProxy.Model.ProductModel.Request;
using Capillary.ShellProxy.Model.ProductModel.Response;
using Capillary.ShellProxy.Model.PromotionModel.Request;
using Capillary.ShellProxy.Model.GiftCatalog.Response;
using Capillary.ShellProxy.Model.GiftCatalog.Request;
using Capillary.ShellProxy.Model.GetRewards.Response;
using Capillary.ShellProxy.Model.PromotionDeailsModel.Response;
using GiftResponseVoucherRule = Capillary.ShellProxy.Model.GiftCatalog.Response.VoucherRule;

namespace Capillary.ShellProxy.Utilities
{
    public static class Mapper
    {
        public static string Map(string requestId, TransactionResponse APIResponse, Model.ShellTransactionModel.Request.Object retailRequest, ErrorResponse ErrorMessage)
        {
            try
            {
                var ShellResponse = new ShellTransactionResponse
                {
                    requestData = new Model.ShellTransactionModel.Response.RequestData
                    {
                        requestID = retailRequest != null ? retailRequest.requestData.requestID : "",
                        overallResult = ErrorMessage == null ? (APIResponse.response[0].errors != null && APIResponse.response[0].errors.Count() > 0 ? APIResponse.response[0].errors[0].message : "Add Transaction successful") : ErrorMessage.ResponseMessage

                    },
                    responseData = new Model.ShellTransactionModel.Response.ResponseData
                    {
                        requestType = retailRequest != null ? retailRequest.requestData.requestType : "Reconciliations"
                    }
                };
                return JsonConvert.SerializeObject(ShellResponse);
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in Mapper.Map(TransactionResponse, Model.ShellTransactionModel.Request.Object).Message:'{0}'", requestId, e.Message);
            }
            return string.Empty;
        }

        public static Tuple<List<Transactionreq.Transaction>, bool> Map(string requestId, Model.ShellTransactionModel.Request.Object inputRequest, lookupResponse getResponse, List<ProductLine> productLines, string category, bool interested)
        {
            Transactionreq.TransactionRequest transactionRequest = new Transactionreq.TransactionRequest();
            var transactionRequests = new List<Transactionreq.Transaction>();
            Decimal TotalTenderAmount = 0.0m;
            Double voucherValue = 0.0d;
            Double category_Discount = 0.0d;
            bool customQuantityLogic = false;
            try
            {

                string transactionNo = string.Format("{0}_{1}_{2}_{3}", inputRequest.requestData.workstationID, inputRequest.requestData.requestID, inputRequest.siteData.siteID, inputRequest.posData.transactionNumber);
                var requestType = inputRequest.requestData.requestType.ToLower();
                var categories = category.Split('_').ToList();

                var transaction = new Transactionreq.Transaction
                {
                    currencyCode = inputRequest.tenders[0].currencyCode,
                    //purchaseTime = Convert.ToDateTime(inputRequest.posData.posTimeStamp).ToString("yyyy-MM-ddTHH:mm:ss+05:30"),
                    billingDate = inputRequest.posData.posTimeStamp,
                    //shipping_source_till_code = retailRequest.posData.terminalID,
                    billNumber = transactionNo,
                    source = "INSTORE",
                    //bill_client_id = retailRequest.posData.transactionNumber,
                    billAmount = inputRequest.totalAmount.ToString(),
                    note = getResponse != null && getResponse.fraudDetails != null && string.Compare(getResponse.fraudDetails.status, "CONFIRMED") == 0 ? "Fraud" : string.Empty

                };

                if (string.Compare(requestType, "RetailTransaction", true) == 0)
                    transaction.type = interested ? "REGULAR" : "NOT_INTERESTED";
                else if (string.Compare(requestType, "RetailTransactionReturn", true) == 0)
                {
                    transaction.type = interested ? "RETURN" : "NOT_INTERESTED_RETURN";
                    transaction.returnType = "FULL";
                    transaction.purchaseTime = inputRequest.posData.originalSalePosTimeStamp;
                }


                if (interested)
                {
                    var customerDataType = inputRequest.customerData[0].customerDataType.ToLower();
                    transaction.identifierType = customerDataType.Contains("mobile") ? "mobile" : "externalId";
                    transaction.identifierValue = inputRequest.customerData[0].customerDataValue.Contains("+") ? inputRequest.customerData[0].customerDataValue.Replace("+", "") : inputRequest.customerData[0].customerDataValue;
                }

                //Transaction Custom fields
                transaction.customFields = new Dictionary<string, string>{
                    {Constants.CountryCode,inputRequest.siteData.countryCode},
                    {Constants.LoyaltyType,inputRequest.customerData.Count > 0 ? inputRequest.customerData[0].loyaltyType : string.Empty}
                };


                //Transaction Extended fields
                if (inputRequest.customerData != null && inputRequest.customerData.Count > 0)
                {
                    transaction.extendedFields = new Dictionary<string, string>{
                    {Constants.customerDataType,inputRequest.customerData[0].customerDataType}
                    };
                }

                if (inputRequest.tenders != null)
                {
                    double Discount = 0.0d;
                    var payments = new List<Transactionreq.PaymentMode>();
                    foreach (var tender in inputRequest.tenders)
                    {
                        if (tender.methodOfPayment.ToLower() == Constants.TenderVoucherPayment)
                        {
                            foreach (var voucher in tender.voucherRules)
                            {
                                Discount += voucher.voucherValue;
                                if (voucher.voucherType.Contains("Voucher"))
                                {
                                    voucherValue = voucher.voucherValue;
                                    customQuantityLogic = true;
                                }
                            }
                        }
                        Transactionreq.PaymentMode payment = new Transactionreq.PaymentMode
                        {
                            mode = tender.methodOfPayment,
                            value = tender.totalAmount.ToString()
                        };
                        TotalTenderAmount += tender.totalAmount;
                        payments.Add(payment);
                    }
                    //TotalTenderAmount = TotalTenderAmount + Discount;
                    transaction.paymentModes = payments;
                    transaction.discount = Discount;
                }

                var lineItems = new List<Transactionreq.LineItemsV2>();
                foreach (var saleItem in inputRequest.saleItems)
                {
                    Double delivery_charge_including_tax = 0.0d;
                    var lineItem = new Transactionreq.LineItemsV2
                    {
                        serial = saleItem.itemID.ToString(),
                        itemCode = string.IsNullOrEmpty(saleItem.productCode) ? saleItem.additionalProductCode : saleItem.productCode,
                        //base_item_code = saleItem.legacyProductCode,
                        amount = saleItem.amount,
                        rate = saleItem.unitPrice,
                        qty = Convert.ToDouble(saleItem.quantity),
                        description = saleItem.additionalProductInfo,
                        value = saleItem.originalAmount

                    };
                    if (saleItem.priceAdjustments != null)
                        lineItem.discount = saleItem.priceAdjustments[0].amount;

                    lineItem.extendedFields = new Dictionary<string, string>{
                        {Constants.netAmount,saleItem.netAmount.ToString()},
                        {Constants.originalNetAmount,saleItem.originalNetAmount.ToString()},
                        {Constants.vat,saleItem.vat.ToString()},
                        {Constants.unitVat,saleItem.unitVat.ToString()},
                        {Constants.vatRate,saleItem.vatRate.ToString()},
                        {Constants.saleChannel,saleItem.saleChannel.ToString()},
                        {Constants.markDownIndicator,"No"}
                    };
                    if (saleItem.markDownIndicator)
                    {
                        lineItem.extendedFields.Add(Constants.markDownIndicator, "Yes");
                    }

                    if (saleItem.priceAdjustments != null && saleItem.loyaltyOffers.Count > 0)
                    {
                        lineItem.extendedFields.Add(Constants.priceAdjustmentType, saleItem.priceAdjustments[0].priceAdjustmentType);
                        lineItem.extendedFields.Add(Constants.discount_description, saleItem.loyaltyOffers[0].loyaltyOfferDescription);
                        if (saleItem.priceAdjustments[0].taxSplit != null && saleItem.priceAdjustments[0].taxSplit.Count > 0)
                        {
                            foreach (var tax in saleItem.priceAdjustments[0].taxSplit)
                            {
                                string taxType = string.Empty;
                                switch (tax.code)
                                {
                                    case "SGST":
                                        taxType = Constants.StateGST;
                                        break;
                                    case "CGST":
                                        taxType = Constants.CentralGST;
                                        break;
                                    case "CESS":
                                        taxType = Constants.tax_amount;
                                        break;
                                    case "IGST":
                                        taxType = Constants.IntegratedGST;
                                        break;
                                }
                                lineItem.extendedFields.Add(taxType, (tax.amount + tax.additionalAmount).ToString());
                            }
                        }
                    }

                    //Custom logic for Quantity
                    if (!string.IsNullOrEmpty(saleItem.productCode))
                    {
                        double quantityField = 0;
                        //contains logic
                        if (customQuantityLogic)
                            quantityField = Math.Floor((saleItem.originalAmount - voucherValue) / saleItem.unitPrice);
                        else
                            quantityField = Math.Floor(saleItem.quantity);

                        lineItem.extendedFields.Add(Constants.Quantity, quantityField.ToString());

                    }

                    //ProductGift quantity based awarding
                    if (saleItem.priceAdjustments != null && saleItem.priceAdjustments[0].priceAdjustmentType.Contains("Gift"))
                        delivery_charge_including_tax = saleItem.originalAmount - Convert.ToDouble(saleItem.priceAdjustments[0].amount);
                    else
                        delivery_charge_including_tax = saleItem.originalAmount;
                    lineItem.extendedFields.Add(Constants.delivery_charge_including_tax, delivery_charge_including_tax.ToString());

                    //CR RoundOff awarding
                    if (productLines != null)
                    {
                        var productLine = productLines.Where(c => c.CrmProductCode == lineItem.itemCode).FirstOrDefault();
                        if (productLine != null)
                        {
                            if (productLine.CategoryId != null && categories.Contains(productLine.CategoryId))
                            {
                                category_Discount = category_Discount + delivery_charge_including_tax;
                            }
                        }
                    }


                    lineItems.Add(lineItem);
                }
                //CR RoundOff extended field
                transaction.extendedFields.Add(Constants.point_discount, category_Discount.ToString());

                transaction.lineItemsV2 = lineItems;
                transactionRequests = new List<Transactionreq.Transaction>{
                    {transaction}
                };
                Console.WriteLine("Transaction V2 Request Body - " + JsonConvert.SerializeObject(transactionRequests).Replace(Environment.NewLine, ""));
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in Mapper.Map(Model.ShellTransactionModel.Request.Object).Message:'{0}'", requestId, e.Message);
            }
            // if (Math.Truncate(10000 * TotalTenderAmount) / 10000 != inputRequest.totalAmount)
            //     return new Tuple<List<Transactionreq.Transaction>, bool>(transactionRequests, false);
            // else
            //     return new Tuple<List<Transactionreq.Transaction>, bool>(transactionRequests, true);
            return new Tuple<List<Transactionreq.Transaction>, bool>(transactionRequests, true);

        }

        //Phase-2 Add transaction mapping
        public static List<Transactionreq.Transaction> Map(string requestId, Model.ShellTransactionModel.Request.Object inputRequest, string programName, bool interested, List<ProductLine> productLines, string category, List<TenderInformation> tendersInformation)
        {
            Transactionreq.TransactionRequest transactionRequest = new Transactionreq.TransactionRequest();
            var transactionRequests = new List<Transactionreq.Transaction>();
            Double category_Discount = 0.0d;
            try
            {

                //string transactionNo = string.Format("{0}_{1}_{2}_{3}", inputRequest.requestData.workstationID, inputRequest.requestData.requestID, inputRequest.siteData.siteID, inputRequest.posData.transactionNumber);
                var requestType = inputRequest.requestData.requestType.ToLower();

                //Generating category codes list
                var categoriesLst = new List<string>();
                string[] ccs = category.Split('_');
                foreach (var cc in ccs)
                {
                    string[] range = cc.Split(',');
                    int startNum = Convert.ToInt32(range[0]);
                    int count = Convert.ToInt32(range[1]);
                    for (int i = startNum; i <= (startNum + count); i++)
                    {
                        categoriesLst.Add(i.ToString());
                    }
                }

                //Date format changing
                var dateTime = inputRequest.posData.posTimeStamp.Split('+');
                string posDateTime = string.Format("{0}+05:30", dateTime[0]);

                //Setting transaction standard fields
                var transaction = new Transactionreq.Transaction
                {
                    currencyCode = inputRequest.tenders[0].currencyCode,
                    billingDate = posDateTime,
                    //billNumber = transactionNo,
                    source = "INSTORE",
                    billAmount = inputRequest.totalAmount.ToString(),
                    promotionEvaluationId = inputRequest.requestData.cartEvaluationID,
                };

                //Type of transaction
                if (string.Compare(requestType, "RetailTransaction", true) == 0)
                {
                    transaction.billNumber = inputRequest.posData.transactionNumber;
                    transaction.type = interested ? "REGULAR" : "NOT_INTERESTED";
                }
                else if (string.Compare(requestType, "RetailTransactionReturn", true) == 0)
                {
                    transaction.type = interested ? "RETURN" : "NOT_INTERESTED_RETURN";
                    transaction.returnType = "FULL";
                    transaction.billNumber = inputRequest.posData.originalTransactionNumber;
                    //transaction.purchaseTime = inputRequest.posData.originalSalePosTimeStamp;
                }

                //Customer Information
                if (interested)
                {
                    var customerDataType = inputRequest.customerData[0].customerDataType.ToLower();
                    transaction.identifierType = customerDataType.Contains("mobile") ? "mobile" : "cardnumber";
                    transaction.identifierValue = inputRequest.customerData[0].customerDataValue.Contains("+") ? inputRequest.customerData[0].customerDataValue.Replace("+", "") : inputRequest.customerData[0].customerDataValue;
                }

                //Transaction Custom fields
                transaction.customFields = new Dictionary<string, string>{
                    {Constants.CountryCode,inputRequest.siteData.countryCode},
                    {Constants.LoyaltyType,inputRequest.customerData.Count > 0 ? inputRequest.customerData[0].loyaltyType : string.Empty}
                };

                //Transaction Extended fields
                transaction.extendedFields = new Dictionary<string, string>();
                if (inputRequest.customerData != null && inputRequest.customerData.Count > 0)
                    transaction.extendedFields.Add(Constants.customerDataType, inputRequest.customerData[0].customerDataType);

                //Tenders
                if (inputRequest.tenders != null)
                {
                    double Discount = 0.0d;
                    var payments = new List<Transactionreq.PaymentMode>();
                    foreach (var tender in inputRequest.tenders)
                    {
                        var tenderInformation = tendersInformation.Where(c => c.Acquirer_Id.ToUpper() == tender.acquirerID.ToUpper()).FirstOrDefault();
                        if (tenderInformation != null)
                        {
                            Transactionreq.PaymentMode payment = new Transactionreq.PaymentMode
                            {
                                mode = tenderInformation.Mode,
                                value = tender.netTenderAmount.ToString()
                            };

                            if (tenderInformation.Acquirer_Id == "LVCH")
                            {
                                Discount += tender.netTenderAmount;
                                payment.attributes = new Transactionreq.Attributes
                                {
                                    CouponTypeCode = tenderInformation.Acquirer_Id
                                };
                                payment.appliedPaymentVoucherIdentifiers = new List<string> { tender.voucherRules[0].referenceID };
                            }
                            else if (tenderInformation.Acquirer_Id.ToUpper() != "CASH" && tenderInformation.Acquirer_Id.ToUpper() != "CASH1")
                            {
                                payment.attributes = new Transactionreq.Attributes
                                {
                                    bank_name = tenderInformation.Acquirer_Id
                                };
                            }
                            payments.Add(payment);
                        }
                    }
                    transaction.paymentModes = payments;
                    transaction.discount = Discount;
                }

                //Setting LineItems
                var lineItems = new List<Transactionreq.LineItemsV2>();
                transaction.appliedPromotionIdentifiers = new List<string>();
                foreach (var saleItem in inputRequest.saleItems)
                {
                    Double SGST_value = 0.0;
                    Double CGST_value = 0.0;
                    Double IGST_value = 0.0;
                    Double CESS_value = 0.0;
                    Double vat = 0.0;
                    Double delivery_charge_including_tax = 0.0d;
                    //Double unitVat = 0.0;
                    string discount_description = string.Empty;
                    var lineItem = new Transactionreq.LineItemsV2
                    {
                        serial = saleItem.itemID.ToString(),
                        itemCode = saleItem.additionalProductCode,
                        amount = saleItem.amount,
                        rate = saleItem.unitPrice,
                        qty = Convert.ToDouble(saleItem.quantity),
                        description = saleItem.additionalProductInfo,
                        value = saleItem.originalAmount

                    };
                    lineItem.appliedPromotionIdentifiers = new List<string>();

                    if (saleItem.markDownIndicator)
                        lineItem.extendedFields.Add(Constants.markDownIndicator, "Yes");
                    else
                        lineItem.extendedFields.Add(Constants.markDownIndicator, "No");


                    //PromoEngine changes
                    delivery_charge_including_tax = saleItem.amount;
                    if (saleItem.priceAdjustments != null && saleItem.priceAdjustments.Count > 0)
                    {
                        foreach (var priceAdjustment in saleItem.priceAdjustments)
                        {
                            if (string.IsNullOrEmpty(discount_description))
                                discount_description = priceAdjustment.reason;
                            else
                                discount_description = discount_description + "," + priceAdjustment.reason;

                            vat += priceAdjustment.vat;
                            //unitVat +=priceAdjustment.unitPrice;
                            lineItem.discount += priceAdjustment.amount;
                            if (priceAdjustment.promotionType != null)
                            {
                                if (priceAdjustment.promotionType.ToUpper() == "CART")
                                {
                                    if (!transaction.appliedPromotionIdentifiers.Contains(priceAdjustment.referenceID))
                                        transaction.appliedPromotionIdentifiers.Add(priceAdjustment.referenceID);
                                }
                                else if (priceAdjustment.promotionType.ToUpper() == "LINEITEM")
                                    lineItem.appliedPromotionIdentifiers.Add(priceAdjustment.referenceID);
                            }
                            if (priceAdjustment.taxSplit != null && priceAdjustment.taxSplit.Count > 0)
                            {
                                foreach (var tax in priceAdjustment.taxSplit)
                                {
                                    string taxType = string.Empty;
                                    switch (tax.code)
                                    {
                                        case "SGST":
                                            SGST_value += tax.amount;
                                            break;
                                        case "CGST":
                                            CGST_value += tax.amount;
                                            break;
                                        case "CESS":
                                            CESS_value += tax.amount;
                                            break;
                                        case "IGST":
                                            IGST_value += tax.amount;
                                            break;
                                    }
                                }
                            }

                        }
                    }


                    lineItem.extendedFields.Add(Constants.delivery_charge_including_tax, delivery_charge_including_tax.ToString());
                    lineItem.extendedFields.Add(Constants.discount_description, discount_description);
                    lineItem.extendedFields.Add(Constants.vat, vat.ToString());
                    //lineItem.extendedFields.Add(Constants.unitVat, unitVat.ToString());
                    if (SGST_value > 0)
                        lineItem.extendedFields.Add(Constants.StateGST, SGST_value.ToString());
                    if (CGST_value > 0)
                        lineItem.extendedFields.Add(Constants.CentralGST, CGST_value.ToString());
                    if (CESS_value > 0)
                        lineItem.extendedFields.Add(Constants.tax_amount, CESS_value.ToString());
                    if (IGST_value > 0)
                        lineItem.extendedFields.Add(Constants.IntegratedGST, IGST_value.ToString());

                    //CR RoundOff awarding
                    if (productLines != null)
                    {
                        if (categoriesLst.Contains(saleItem.categoryCode))
                            category_Discount = category_Discount + delivery_charge_including_tax;
                    }

                    lineItems.Add(lineItem);
                }
                //CR RoundOff extended field
                transaction.extendedFields.Add(Constants.point_discount, category_Discount.ToString());

                transaction.lineItemsV2 = lineItems;
                transactionRequests = new List<Transactionreq.Transaction>{
                    {transaction}
                };
                Console.WriteLine("Transaction V2 Request Body - " + JsonConvert.SerializeObject(transactionRequests).Replace(Environment.NewLine, ""));
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in Mapper.Map(Model.ShellTransactionModel.Request.Object).Message:'{0}'", requestId, e.Message);
            }
            return new List<Transactionreq.Transaction>(transactionRequests);
        }

        //Phase-2 Add transaction mapping after customerIdValue decryption
    public static List<Transactionreq.Transaction> Map(string requestId, Model.ShellTransactionModel.Request.Object inputRequest, string programName, bool interested, List<ProductLine> productLines, string category, List<TenderInformation> tendersInformation,string customerIdValue)
        {
            Transactionreq.TransactionRequest transactionRequest = new Transactionreq.TransactionRequest();
            var transactionRequests = new List<Transactionreq.Transaction>();
            Double category_Discount = 0.0d;
            try
            {

                //string transactionNo = string.Format("{0}_{1}_{2}_{3}", inputRequest.requestData.workstationID, inputRequest.requestData.requestID, inputRequest.siteData.siteID, inputRequest.posData.transactionNumber);
                var requestType = inputRequest.requestData.requestType.ToLower();

                //Generating category codes list
                var categoriesLst = new List<string>();
                string[] ccs = category.Split('_');
                foreach (var cc in ccs)
                {
                    string[] range = cc.Split(',');
                    int startNum = Convert.ToInt32(range[0]);
                    int count = Convert.ToInt32(range[1]);
                    for (int i = startNum; i <= (startNum + count); i++)
                    {
                        categoriesLst.Add(i.ToString());
                    }
                }

                //Date format changing
                var dateTime = inputRequest.posData.posTimeStamp.Split('+');
                string posDateTime = string.Format("{0}+05:30", dateTime[0]);

                //Setting transaction standard fields
                var transaction = new Transactionreq.Transaction
                {
                    currencyCode = inputRequest.tenders[0].currencyCode,
                    billingDate = posDateTime,
                    //billNumber = transactionNo,
                    source = "INSTORE",
                    billAmount = inputRequest.totalAmount.ToString(),
                    promotionEvaluationId = inputRequest.requestData.cartEvaluationID,
                };

                //Type of transaction
                if (string.Compare(requestType, "RetailTransaction", true) == 0)
                {
                    transaction.billNumber = inputRequest.posData.transactionNumber;
                    transaction.type = interested ? "REGULAR" : "NOT_INTERESTED";
                }
                else if (string.Compare(requestType, "RetailTransactionReturn", true) == 0)
                {
                    transaction.type = interested ? "RETURN" : "NOT_INTERESTED_RETURN";
                    transaction.returnType = "FULL";
                    transaction.billNumber = inputRequest.posData.originalTransactionNumber;
                    //transaction.purchaseTime = inputRequest.posData.originalSalePosTimeStamp;
                }

                //Customer Information
                if (interested)
                {
                    var customerDataType = inputRequest.customerData[0].customerDataType.ToLower();
                    transaction.identifierType = customerDataType.Contains("mobile") ? "mobile" : "cardnumber";
                    transaction.identifierValue = customerIdValue.Contains("+") ? customerIdValue.Replace("+", "") : customerIdValue;
                }

                //Transaction Custom fields
                transaction.customFields = new Dictionary<string, string>{
                    {Constants.CountryCode,inputRequest.siteData.countryCode},
                    {Constants.LoyaltyType,inputRequest.customerData.Count > 0 ? inputRequest.customerData[0].loyaltyType : string.Empty}
                };

                //Transaction Extended fields
                transaction.extendedFields = new Dictionary<string, string>();
                if (inputRequest.customerData != null && inputRequest.customerData.Count > 0)
                    transaction.extendedFields.Add(Constants.customerDataType, inputRequest.customerData[0].customerDataType);

                //Tenders
                if (inputRequest.tenders != null)
                {
                    double Discount = 0.0d;
                    var payments = new List<Transactionreq.PaymentMode>();
                    foreach (var tender in inputRequest.tenders)
                    {
                        var tenderInformation = tendersInformation.Where(c => c.Acquirer_Id.ToUpper() == tender.acquirerID.ToUpper()).FirstOrDefault();
                        if (tenderInformation != null)
                        {
                            Transactionreq.PaymentMode payment = new Transactionreq.PaymentMode
                            {
                                mode = tenderInformation.Mode,
                                value = tender.netTenderAmount.ToString()
                            };

                            if (tenderInformation.Acquirer_Id == "LVCH")
                            {
                                Discount += tender.netTenderAmount;
                                payment.attributes = new Transactionreq.Attributes
                                {
                                    CouponTypeCode = tenderInformation.Acquirer_Id
                                };
                                payment.appliedPaymentVoucherIdentifiers = new List<string> { tender.voucherRules[0].referenceID };
                            }
                            else if (tenderInformation.Acquirer_Id.ToUpper() != "CASH" && tenderInformation.Acquirer_Id.ToUpper() != "CASH1")
                            {
                                payment.attributes = new Transactionreq.Attributes
                                {
                                    bank_name = tenderInformation.Acquirer_Id,
                                    number = tender.cardPAN,
                                    card_type = tenderInformation.MOP_Name,
                                    CardIssuerCode = tenderInformation.MOP_Name
                                };
                            }
                            payments.Add(payment);
                        }
                    }
                    transaction.paymentModes = payments;
                    transaction.discount = Discount;
                }

                //Setting LineItems
                var lineItems = new List<Transactionreq.LineItemsV2>();
                transaction.appliedPromotionIdentifiers = new List<string>();
                foreach (var saleItem in inputRequest.saleItems)
                {
                    Double SGST_value = 0.0;
                    Double CGST_value = 0.0;
                    Double IGST_value = 0.0;
                    Double CESS_value = 0.0;
                    Double vat = 0.0;
                    Double delivery_charge_including_tax = 0.0d;
                    //Double unitVat = 0.0;
                    string discount_description = string.Empty;
                    var lineItem = new Transactionreq.LineItemsV2
                    {
                        serial = saleItem.itemID.ToString(),
                        itemCode = saleItem.additionalProductCode,
                        amount = saleItem.amount,
                        rate = saleItem.unitPrice,
                        qty = Convert.ToDouble(saleItem.quantity),
                        description = saleItem.additionalProductInfo,
                        value = saleItem.originalAmount

                    };
                    lineItem.appliedPromotionIdentifiers = new List<string>();

                    if (saleItem.markDownIndicator)
                        lineItem.extendedFields.Add(Constants.markDownIndicator, "Yes");
                    else
                        lineItem.extendedFields.Add(Constants.markDownIndicator, "No");


                    //PromoEngine changes
                    delivery_charge_including_tax = saleItem.amount;
                    if (saleItem.priceAdjustments != null && saleItem.priceAdjustments.Count > 0)
                    {
                        foreach (var priceAdjustment in saleItem.priceAdjustments)
                        {
                            if (string.IsNullOrEmpty(discount_description))
                                discount_description = priceAdjustment.reason;
                            else
                                discount_description = discount_description + "," + priceAdjustment.reason;

                            vat += priceAdjustment.vat;
                            //unitVat +=priceAdjustment.unitPrice;
                            lineItem.discount += priceAdjustment.amount;
                            if (priceAdjustment.promotionType != null)
                            {
                                if (priceAdjustment.promotionType.ToUpper() == "CART")
                                {
                                    if (!transaction.appliedPromotionIdentifiers.Contains(priceAdjustment.referenceID))
                                        transaction.appliedPromotionIdentifiers.Add(priceAdjustment.referenceID);
                                }
                                else if (priceAdjustment.promotionType.ToUpper() == "LINEITEM")
                                    lineItem.appliedPromotionIdentifiers.Add(priceAdjustment.referenceID);
                            }
                            if (priceAdjustment.taxSplit != null && priceAdjustment.taxSplit.Count > 0)
                            {
                                foreach (var tax in priceAdjustment.taxSplit)
                                {
                                    string taxType = string.Empty;
                                    switch (tax.code)
                                    {
                                        case "SGST":
                                            SGST_value += tax.amount;
                                            break;
                                        case "CGST":
                                            CGST_value += tax.amount;
                                            break;
                                        case "CESS":
                                            CESS_value += tax.amount;
                                            break;
                                        case "IGST":
                                            IGST_value += tax.amount;
                                            break;
                                    }
                                }
                            }

                        }
                    }


                    lineItem.extendedFields.Add(Constants.delivery_charge_including_tax, delivery_charge_including_tax.ToString());
                    lineItem.extendedFields.Add(Constants.discount_description, discount_description);
                    lineItem.extendedFields.Add(Constants.vat, vat.ToString());
                    //lineItem.extendedFields.Add(Constants.unitVat, unitVat.ToString());
                    if (SGST_value > 0)
                        lineItem.extendedFields.Add(Constants.StateGST, SGST_value.ToString());
                    if (CGST_value > 0)
                        lineItem.extendedFields.Add(Constants.CentralGST, CGST_value.ToString());
                    if (CESS_value > 0)
                        lineItem.extendedFields.Add(Constants.tax_amount, CESS_value.ToString());
                    if (IGST_value > 0)
                        lineItem.extendedFields.Add(Constants.IntegratedGST, IGST_value.ToString());

                    //CR RoundOff awarding
                    if (productLines != null)
                    {
                        if (categoriesLst.Contains(saleItem.categoryCode))
                            category_Discount = category_Discount + delivery_charge_including_tax;
                    }

                    lineItems.Add(lineItem);
                }
                //CR RoundOff extended field
                transaction.extendedFields.Add(Constants.point_discount, category_Discount.ToString());

                transaction.lineItemsV2 = lineItems;
                transactionRequests = new List<Transactionreq.Transaction>{
                    {transaction}
                };
                Console.WriteLine("Transaction V2 Request Body - " + JsonConvert.SerializeObject(transactionRequests).Replace(Environment.NewLine, ""));
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in Mapper.Map(Model.ShellTransactionModel.Request.Object).Message:'{0}'", requestId, e.Message);
            }
            return new List<Transactionreq.Transaction>(transactionRequests);
        }

        public static List<ProductLine> Map(string requestId, OffersRequest offersRequest)
        {
            var productLines = new List<ProductLine>();
            try
            {
                foreach (var lineItem in offersRequest.saleItems)
                {
                    string productCode, quantity;
                    if (string.IsNullOrEmpty(lineItem.additionalProductCode?.Trim())) //fuel
                    {
                        productCode = lineItem.productCode;
                        quantity = Convert.ToString(lineItem.quantity * 1000);
                    }
                    else
                    {
                        productCode = lineItem.additionalProductCode;
                        quantity = Convert.ToString(lineItem.quantity);
                    }

                    productLines.Add(new ProductLine
                    {
                        CrmLocationCode = offersRequest.siteData.siteID,
                        CrmProductCode = productCode,
                        Quantity = quantity,
                        UnitPrice = lineItem.unitPrice,
                        Amount = Convert.ToString(lineItem.originalAmount),
                        CrmItemId = Convert.ToString(lineItem.itemID)
                    });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in Mapper.Map(OffersRequest) .Message:'{0}'", requestId, e.Message);
            }

            return productLines;
        }

        public static List<ProductLine> Map(string requestId, RetailTransactionRequest transactionRequest)
        {
            var productLines = new List<ProductLine>();
            try
            {
                foreach (var lineItem in transactionRequest.objects[0].saleItems)
                {
                    string productCode;
                    if (string.IsNullOrEmpty(lineItem.additionalProductCode?.Trim())) //fuel
                    {
                        productCode = lineItem.productCode;
                    }
                    else
                    {
                        productCode = lineItem.additionalProductCode;
                    }

                    productLines.Add(new ProductLine
                    {
                        CrmProductCode = productCode,
                        Amount = Convert.ToString(lineItem.originalAmount),
                        CrmLocationCode = transactionRequest.objects[0].siteData.siteID
                    });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in Mapper.Map(AddTansaction-DynamoMaping) .Message:'{0}'", requestId, e.Message);
            }

            return productLines;
        }

        public static List<ProductLine> Map(string requestId, UpdateProduct updateProducts)
        {
            var productLines = new List<ProductLine>();
            try
            {
                foreach (var updateProduct in updateProducts.priceData)
                {
                    if (updateProduct != null)
                    {
                        productLines.Add(new ProductLine
                        {
                            CrmProductCode = updateProduct.productCode,
                            CrmLocationCode = updateProducts.siteData.siteID
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in Mapper.Map(UpdateProduct) .Message:'{0}'", requestId, e.Message);
            }

            return productLines;
        }

        public static string Map(string requestId, Model.ShellTransactionModel.Request.Object retailrequest, Model.ShellTransactionModel.Request.SaleItem saleItem, Model.ShellTransactionModel.Request.VoucherRule voucherRule, CustomerCoupon failedCoupons)
        {
            var reportClass = new ErrorReport();
            try
            {
                reportClass.Category = "";
                reportClass.Transaction = "";
                reportClass.Cost_centre = "NA";
                reportClass.Loyalty_Action = "Redemption";
                reportClass.Sold_To = "";
                reportClass.IdentifierType = string.Compare(retailrequest.customerData[0].customerDataType, "mobilenumber", true) == 0 ? "mobile" : "externalId";
                reportClass.IdentifierValue = retailrequest.customerData[0].customerDataValue;
                reportClass.Ship_To = retailrequest.siteData.siteID;
                if (saleItem != null)
                {
                    reportClass.Value = saleItem.priceAdjustments[0].amount.ToString();
                    reportClass.CouponCode = saleItem.loyaltyOffers[0].loyaltyOfferID;
                    reportClass.Claim_Type = saleItem.priceAdjustments[0].priceAdjustmentType.Contains("Gift") ? "Product" : "Value";
                    if (saleItem.priceAdjustments[0].priceAdjustmentType.Contains("Gift"))
                    {
                        if (!string.IsNullOrEmpty(saleItem.productCode))
                            reportClass.CRMProductID = saleItem.productCode;
                        else
                            reportClass.CRMProductID = saleItem.additionalProductCode;
                    }

                }
                else if (voucherRule != null)
                {
                    reportClass.Value = voucherRule.voucherValue.ToString();
                    reportClass.Claim_Type = "Value";
                    reportClass.CouponCode = voucherRule.voucherCode;
                }
                else if (failedCoupons != null)
                {
                    reportClass.Claim_Type = failedCoupons.CouponType.Contains("Gift") ? "Product" : "Value";
                    reportClass.Value = failedCoupons.DiscountAmount.ToString();
                    reportClass.CRMProductID = failedCoupons.CRMProductID != null ? failedCoupons.CRMProductID : string.Empty;
                    reportClass.Failure_reason = failedCoupons.RedeemFailReason;
                    reportClass.CouponCode = failedCoupons.CouponCode;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in Mapper.Map(ErrorReport) .Message:'{0}'", requestId, e.Message);
            }

            return JsonConvert.SerializeObject(reportClass);
        }

        //Push failure reports tp S3
        public static string Map(string requestId, Model.ShellTransactionModel.Request.RetailTransactionRequest retailRequest, string response, string Message, string rawInputRequest, string capResponse = "")
        {
            var reportClass = new ErrorReport();
            try
            {
                reportClass.Txn_Req = retailRequest != null ? JsonConvert.SerializeObject(retailRequest) : rawInputRequest;
                reportClass.Failure_reason = Message;
                reportClass.Txn_Res = response;
                reportClass.CAP_TxnResponse = capResponse;
                reportClass.Ship_To = retailRequest != null ? retailRequest.objects[0].siteData.siteID : "0000";

            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in Mapper.Map(ErrorTransactionReport) .Message:'{0}'", requestId, e.Message);
            }

            return JsonConvert.SerializeObject(reportClass);
        }

        //Phase-2 - PromotionEngine Request
        public static PromotionRequest Map(string requestId, Model.OffersModel.Request.OffersRequest offerRequest, string customerID, List<TenderInformation> tendersInformation, double outstandingAmount = 0.0d)
        {
            var promotionRequest = new PromotionRequest();
            //bool sendTenderAmount = true;
            try
            {
                promotionRequest.amount = offerRequest.totalAmount.ToString();
                if (!string.IsNullOrEmpty(customerID))
                    promotionRequest.customerId = customerID;
                promotionRequest.evaluationId = offerRequest.requestData.cartEvaluationID;
                promotionRequest.cartItems = new List<CartItem>();
                foreach (var item in offerRequest.saleItems)
                {
                    promotionRequest.cartItems.Add(new CartItem
                    {
                        amount = item.amount.ToString(),
                        qty = item.quantity.ToString(),
                        sku = item.additionalProductCode
                    });
                }

                promotionRequest.cartTenders = new List<CartTender>();
                if (offerRequest.tenders != null)
                {
                    foreach (var payment in offerRequest.tenders)
                    {
                        var tenderInformation = tendersInformation.Where(s => s.Acquirer_Id.ToUpper() == payment.acquirerID.ToUpper()).FirstOrDefault();
                        if (tenderInformation != null)
                        {
                            if (tenderInformation.MOP_Name.ToUpper() != "CASH" && tenderInformation.Acquirer_Id != "LVCH")
                            {
                                var cartTender = new CartTender();
                                cartTender.identifier = tenderInformation.Acquirer_Id;
                                cartTender.amount = payment.totalAmount;
                                promotionRequest.cartTenders.Add(cartTender);
                            }
                            else if (tenderInformation.MOP_Name.ToUpper() == "CASH")
                            {
                                var cartTender = new CartTender();
                                cartTender.identifier = "CASH1";
                                cartTender.amount = payment.totalAmount;
                                promotionRequest.cartTenders.Add(cartTender);
                            }

                        }
                    }
                }

                //check if tender total amount is greater than for equal to Remainder (Last API call totalAmunt)
                if (offerRequest.tenders.Count() > 0 && offerRequest.tenders.LastOrDefault().totalAmount >= offerRequest.remainder)
                {
                    if (promotionRequest.cartTenders.Count() > 0)
                        promotionRequest.cartTenders.LastOrDefault().amount = 0;
                }

                //check if tender total amount is greater than for equal to predictedTender.Amount (Last API call Cashiermessage)
                if (offerRequest.tenders.Count() > 0 && offerRequest.tenders.LastOrDefault().acquirerID.ToUpper() == "CASH" && offerRequest.tenders.LastOrDefault().totalAmount >= offerRequest.predictedTender.amount)
                {
                    if (promotionRequest.cartTenders.Count() > 0)
                        promotionRequest.cartTenders.LastOrDefault().amount = 0;
                }

                // //If Tender count is equal to 1 and not equal to LVCH then consider as last tender.
                // if(offerRequest.tenders.Count() == 1 && offerRequest.tenders[0].acquirerID != "LVCH")
                // {
                //     if(promotionRequest.cartTenders.Count() > 0)
                //         promotionRequest.cartTenders[0].amount = 0;
                // }

                //Remove last tender amount if the tender is not voucher which is assumed as last MOP
                // if(promotionRequest.cartTenders != null && promotionRequest.cartTenders.Count() > 0)
                // {
                //     if(offerRequest.tenders.LastOrDefault().acquirerID != "LVCH" && offerRequest.tenders.LastOrDefault().acquirerID != "VCHR")
                //     {
                //         promotionRequest.cartTenders.LastOrDefault().amount = 0;
                //     }
                // }

                //Fetch promocodes from payload and send as paymentvouchers to promoengine
                if (offerRequest.priceAdjustments != null)
                {
                    promotionRequest.paymentVouchers = new List<string>();
                    foreach (var priceAdjustment in offerRequest.priceAdjustments)
                        promotionRequest.paymentVouchers.Add(priceAdjustment.loyaltyOfferID);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in Mapper.Map(PromotionOfferMapping) .Message:'{0}'", requestId, e.Message);
            }

            return promotionRequest;
        }


        public static string Map(string requestId, OffersRequest offersRequest, ErrorResponse ErrorMessages)
        {
            try
            {
                var ErrorResponse = new Model.OffersModel.Response.ResponseData
                {
                    actionCode = ErrorMessages.ResponseCode,
                    actionCodeDescription = ErrorMessages.ResponseMessage,
                    workstationID = offersRequest.requestData.workstationID,
                    overallResult = "Fail",
                    requestID = offersRequest.requestData.requestID,
                    requestType = offersRequest.requestData.requestType
                };
                return JsonConvert.SerializeObject(ErrorResponse);
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in Mapper.Map(OfferFailRespose) .Message:'{0}'", requestId, e.Message);
            }

            return string.Empty;
        }

        //Phase-2 GiftCatalog Response
        public static string Map(string requestId, GiftCatalogRequest catalogRequest, GetRewardsResponse catalogResponse, ErrorResponse ErrorMessages, double availablePoints = 0.0)
        {
            GiftCatalogResponse giftCatalogResponse = new GiftCatalogResponse();
            try
            {
                giftCatalogResponse.customerData = new List<Model.GiftCatalog.Response.CustomerData>
                {
                    new Model.GiftCatalog.Response.CustomerData
                    {
                        customerDataValue = catalogRequest.customerData[0].customerDataValue,
                        customerDataType = catalogRequest.customerData[0].customerDataType,
                        totalPointBalance = availablePoints
                    }
                };

                giftCatalogResponse.responseData = new Model.GiftCatalog.Response.ResponseData
                {
                    actionCode = ErrorMessages == null ? 0 : ErrorMessages.ResponseCode,
                    actionCodeDescription = ErrorMessages == null ? "OK" : ErrorMessages.ResponseMessage,
                    workstationID = catalogRequest.requestData.workstationID,
                    deliveryMode = catalogRequest.requestData.deliveryMode,
                    overallResult = ErrorMessages == null ? "Success" : ErrorMessages.ResponseMessage,
                    requestID = catalogRequest.requestData.requestID,
                    requestType = catalogRequest.requestData.requestType
                };

                if (catalogResponse != null && catalogResponse.rewardList != null)
                {
                    List<GiftData> giftItems = new List<GiftData>();
                    List<GiftResponseVoucherRule> giftVoucherItems = new List<GiftResponseVoucherRule>();
                    foreach (var giftItem in catalogResponse.rewardList)
                    {
                        if (giftItem.label.ToUpper().Contains("FUEL"))
                        {
                            var giftPetrolItem = new Model.GiftCatalog.Response.VoucherRule
                            {
                                loyaltySchemeCode = giftItem.id.ToString(),
                                pointsRedeemed = giftItem.intouchPoints,
                                loyaltyOfferID = string.Format("{0}_GIFT", giftItem.id.ToString()),
                                expiryDate = giftItem.endTime,
                                voucherType = "F",
                                additionalVoucherInfo = giftItem.description,
                                voucherCode = string.Format("{0}_GIFT", giftItem.id.ToString())
                            };
                            giftPetrolItem.products = new List<Model.GiftCatalog.Response.Product>();
                            if (giftItem.tier.Contains(','))
                            {
                                var applicableItems = giftItem.tier.Split(',');
                                foreach (var appicableItem in applicableItems)
                                {
                                    giftPetrolItem.products.Add(new Model.GiftCatalog.Response.Product
                                    {
                                        productCode = appicableItem,
                                        additionalProductInfo = giftItem.description
                                    });
                                }
                            }
                            else
                            {
                                giftPetrolItem.products.Add(new Model.GiftCatalog.Response.Product
                                {
                                    productCode = giftItem.tier,
                                    additionalProductInfo = giftItem.description
                                });

                            }
                            giftVoucherItems.Add(giftPetrolItem);
                        }

                        else if (giftItem.label.ToUpper().Contains("SHELLVOUCHERS"))
                        {
                            var giftCategory = new Model.GiftCatalog.Response.VoucherRule
                            {
                                loyaltySchemeCode = giftItem.id.ToString(),
                                pointsRedeemed = giftItem.intouchPoints,
                                loyaltyOfferID = string.Format("{0}_GIFT", giftItem.id.ToString()),
                                expiryDate = giftItem.endTime,
                                voucherType = "F",
                                additionalVoucherInfo = giftItem.description,
                                voucherCode = string.Format("{0}_GIFT", giftItem.id.ToString())
                            };
                            giftCategory.products = new List<Model.GiftCatalog.Response.Product>();

                            var giftCategoryDetails = giftItem.tier.Split('_');
                            bool categortCodePresent = giftCategoryDetails[0].ToUpper() == "CC";
                            if (giftItem.tier.Contains(',') && giftCategoryDetails.Count() >= 2)
                            {
                                var applicableItems = giftCategoryDetails[1].Split(',');
                                foreach (var appicableItem in applicableItems)
                                {
                                    var products = new Model.GiftCatalog.Response.Product();
                                    if (categortCodePresent)
                                        products.categoryCode = appicableItem;
                                    else
                                        products.subCategoryCode = appicableItem;
                                    giftCategory.products.Add(products);

                                }

                            }
                            else if (giftCategoryDetails.Count() >= 2)
                            {
                                var products = new Model.GiftCatalog.Response.Product();
                                if (categortCodePresent)
                                    products.categoryCode = string.IsNullOrEmpty(giftCategoryDetails[1]) ? string.Empty : giftCategoryDetails[1];
                                else
                                    products.subCategoryCode = string.IsNullOrEmpty(giftCategoryDetails[1]) ? string.Empty : giftCategoryDetails[1];
                                giftCategory.products.Add(products);

                            }


                            giftVoucherItems.Add(giftCategory);
                        }
                        else
                        {
                            giftItems.Add(new GiftData
                            {
                                loyaltySchemeCode = giftItem.id.ToString(),
                                additionalProductCode = giftItem.tier,
                                deliveryMode = "All",
                                loyaltyOfferID = string.Format("{0}_GIFT", giftItem.id.ToString()),
                                expiryDate = giftItem.endTime,
                                pointsRedeemed = giftItem.intouchPoints,
                                additionalProductInfo = giftItem.description,

                            });
                        }
                    }

                    giftCatalogResponse.giftItems = new List<GiftItem>{
                    new GiftItem{
                        exchangeMode = "Store",
                        giftData = giftItems,
                        voucherRules = giftVoucherItems
                    }
                };

                }


                return JsonConvert.SerializeObject(giftCatalogResponse);
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in Mapper.Map(GiftCatalogResponse Mapping) .Message:'{0}'", requestId, e.Message);
            }

            return string.Empty;
        }

        public static string Map(string requestId, OffersRequest offersRequest, List<AvailableCoupon> availableCoupons, string offerMessage, ErrorResponse errorResponse)
        {
            var offersResponse = new OffersResponse();
            if (availableCoupons != null)
            {
                availableCoupons.RemoveAll(coupon => coupon == null);
            }
            try
            {
                //Customer Data
                offersResponse.customerData = new List<Model.OffersModel.Response.CustomerData>
                {
                    {new Model.OffersModel.Response.CustomerData
                    {
                        customerDataValue = offersRequest.customerData[0].customerDataValue,
                        customerDataType = offersRequest.customerData[0].customerDataType,
                        loyaltyType = offersRequest.customerData[0].loyaltyType
                    } }
                };

                //ResponseData
                if (errorResponse != null)
                {
                    offersResponse.responseData = new Model.OffersModel.Response.ResponseData
                    {
                        actionCode = errorResponse.ResponseCode,
                        actionCodeDescription = errorResponse.ResponseMessage,
                        workstationID = offersRequest.requestData.workstationID,
                        overallResult = errorResponse.ResponseCode == 0 ? "Success" : "Fail",
                        requestID = offersRequest.requestData.requestID,
                        requestType = offersRequest.requestData.requestType
                    };
                }
                else
                {
                    offersResponse.responseData = new Model.OffersModel.Response.ResponseData
                    {
                        actionCode = 0,
                        actionCodeDescription = availableCoupons != null && availableCoupons.Count > 0 ? "Offers Available" : "No Offers Available",
                        workstationID = offersRequest.requestData.workstationID,
                        overallResult = "Success",
                        requestID = offersRequest.requestData.requestID,
                        requestType = offersRequest.requestData.requestType
                    };
                }

                //Receipt Data
                offersResponse.receipt = new Receipt { receiptLines = new List<string> { offerMessage } };

                //Applicable Vouchers
                if (availableCoupons != null)
                {
                    var applicableVouchers = new List<ApplicableVoucher>();
                    var cartVouchers = availableCoupons.Where(item => item.PromoLevel.ToString().Contains("productVoucher") || !item.PromoLevel.ToString().Contains("product")).ToList();
                    if (cartVouchers != null)
                    {
                        foreach (var cartVoucher in cartVouchers)
                        {
                            var applicableVoucher = new ApplicableVoucher
                            {
                                voucherCode = cartVoucher.CouponCode,
                                additionalVoucherInfo = cartVoucher.CouponName,
                                voucherType = cartVoucher.PromoLevel.ToString(),
                                expiryDate = Convert.ToDateTime(cartVoucher.ExpiryDate).ToString("yyyy-MM-ddTHH:MM:ss+05:30"),
                                voucherValue = cartVoucher.Value.ToString()
                            };
                            applicableVouchers.Add(applicableVoucher);
                        }
                    }
                    offersResponse.applicableVouchers = applicableVouchers;
                }

                //Tenders
                var tenders = new List<Model.OffersModel.Response.Tender>();
                if (offersRequest.tenders != null)
                {
                    foreach (var tender in offersRequest.tenders)
                    {
                        var responseTender = new Model.OffersModel.Response.Tender
                        {
                            totalAmount = tender.totalAmount,
                            cashRedeemed = tender.cashRedeemed,
                            methodOfPayment = tender.methodOfPayment,
                            methodOfPaymentID = tender.methodOfPaymentID,
                            pointsRedeemed = tender.pointsRedeemed,
                            tenderID = tender.tenderID
                        };
                        tenders.Add(responseTender);
                    }

                }
                offersResponse.tenders = tenders;

                offersResponse.saleItems = LineItemsJson(requestId, offersRequest.saleItems, availableCoupons);
                offersResponse.totalAmount = offersRequest.totalAmount;
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in Mapper.Map(ErrorReport) .Message:'{0}'", requestId, e.Message);
            }

            return JsonConvert.SerializeObject(offersResponse);
        }
        public static string Map(string requestId, ProductResponse responseProduct)
        {
            try
            {
                if (!String.IsNullOrEmpty(responseProduct.messageCode))
                {
                    if (responseProduct.messageCode == "1018")
                    {
                        responseProduct.messageCode = "0";
                    }
                    else
                    {
                        responseProduct.messageCode = "500";
                    }
                }
                else
                {
                    responseProduct.messageCode = "500";
                }

                var pructUpdateResponse = new Model.UpdateProductResponse
                {
                    responseData = new Model.ResponseData
                    {
                        actionCode = responseProduct.messageCode,
                        actionCodeDescription = responseProduct.Message,

                    },
                };
                return JsonConvert.SerializeObject(pructUpdateResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in Mapper.Map(TransactionResponse, Model.ShellTransactionModel.Request.Object).Message:'{0}'", requestId, ex.Message);
            }
            return string.Empty;
        }

        internal static List<Model.OffersModel.Response.SaleItem> LineItemsJson(string requestId, List<Model.OffersModel.Request.SaleItem> saleItems, List<AvailableCoupon> availableCoupons)
        {
            var ResponseSaleItems = new List<Model.OffersModel.Response.SaleItem>();
            var lineItemCoupons = new List<AvailableCoupon>();
            try
            {
                if (availableCoupons != null && availableCoupons.Count() > 0)
                    lineItemCoupons = availableCoupons.Where(item => !item.PromoLevel.ToString().Contains("productVoucher") && item.PromoLevel.ToString().Contains("product")).ToList();

                foreach (var saleItem in saleItems)
                {
                    try
                    {
                        var ResponseSaleItem = new Model.OffersModel.Response.SaleItem
                        {
                            additionalProductCode = saleItem.additionalProductCode,
                            additionalProductInfo = saleItem.additionalProductInfo,
                            amount = saleItem.amount,
                            categoryCode = saleItem.categoryCode,
                            itemID = saleItem.itemID,
                            originalAmount = saleItem.originalAmount,
                            productCode = saleItem.productCode,
                            quantity = saleItem.quantity,
                            saleItemType = saleItem.saleItemType,
                            unitMeasure = saleItem.unitMeasure,
                            unitPrice = saleItem.unitPrice,
                            vatRate = saleItem.vatRate
                        };
                        if (lineItemCoupons != null && lineItemCoupons.Count() > 0)
                        {
                            var ItemCoupon = lineItemCoupons.Where(item => Convert.ToInt64(item.LineItemId) == saleItem.itemID).FirstOrDefault();
                            if (ItemCoupon != null)
                            {
                                ResponseSaleItem.loyaltyOffers = new List<Model.OffersModel.Response.LoyaltyOffer>
                                {
                                    { new Model.OffersModel.Response.LoyaltyOffer{ loyaltyOfferID=ItemCoupon.CouponCode, loyaltyOfferDescription=ItemCoupon.CouponName}
                                }
                                };

                                ResponseSaleItem.priceAdjustments = new List<Model.OffersModel.Response.PriceAdjustment>
                                {
                                    {new Model.OffersModel.Response.PriceAdjustment{

                                        additionalProductCode = saleItem.additionalProductCode,
                                        priceAdjustmentType = ItemCoupon.PromoLevel.ToString().Contains("ProductDiscount") ? "RealtimeOffer-A" : "Gift" ,
                                        amount = ItemCoupon.Value,
                                        categoryCode = saleItem.categoryCode,
                                        loyaltyOfferID = ResponseSaleItem.loyaltyOffers[0].loyaltyOfferID,
                                        priceAdjustmentID = 1,
                                        quantity = saleItem.quantity,
                                        reason = ResponseSaleItem.loyaltyOffers[0].loyaltyOfferDescription,
                                        unitPrice = Convert.ToDecimal(saleItem.unitPrice)
                                    } }
                                };
                            }
                        }

                        ResponseSaleItems.Add(ResponseSaleItem);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("RequestId:{0}.Exception encountered in Mapper.Map(LineItemJson) for item {1} .Message:'{2}'", requestId, saleItem.itemID, e.Message);
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in Mapper.Map(LineItemJson) .Message:'{0}'", requestId, e.Message);
            }
            return ResponseSaleItems;
        }

        //Phase-2 Offers Response
        public static string Map(string requestId, OffersRequest offersRequest, Capillary.ShellProxy.Model.PromotionModel.Response.PromotionResponse promoResponse, string offerMessage, ErrorResponse errorResponse, PromotionDetailsResponse promotionDetails, lookupResponse customerInfo, List<TenderInformation> tendersInformation = null, double pointsredeemed = 0, bool isNotInterested = false)
        {
            var offersResponse = new OffersResponse();
            offersResponse.tenders = new List<Model.OffersModel.Response.Tender>();
            try
            {
                //Customer Data
                offersResponse.customerData = new List<Model.OffersModel.Response.CustomerData>
                {
                    {new Model.OffersModel.Response.CustomerData
                    {
                        customerDataValue = offersRequest.customerData[0].customerDataValue,
                        customerDataType = offersRequest.customerData[0].customerDataType,
                        loyaltyType = offersRequest.customerData[0].loyaltyType,
                        pointsRedeemed = pointsredeemed.ToString()
                    } }
                };
                if (offersRequest.customerData != null && offersRequest.customerData.Count() > 0 && offersRequest.customerData[0].customerDataType.ToLower().Contains("mobile") && customerInfo != null && customerInfo.cardDetails != null && customerInfo.cardDetails.Count() > 0)
                {
                    string digitalCardNumber = customerInfo.cardDetails.Where(s => s.cardNumber.Contains("D")).FirstOrDefault().cardNumber.ToString();
                    if (!string.IsNullOrEmpty(digitalCardNumber))
                    {
                        offersResponse.customerData.Add(new Model.OffersModel.Response.CustomerData
                        {
                            customerDataType = "DigitalLoyaltyCard",
                            customerDataValue = digitalCardNumber,
                            loyaltyType = offersRequest.customerData[0].loyaltyType
                        });
                    }
                }

                //Receipt Data
                if (offerMessage.Contains('_'))
                    offersResponse.receipt = new Receipt { receiptLines = offerMessage.Split('_').ToList() };
                else
                    offersResponse.receipt = new Receipt { receiptLines = new List<string> { offerMessage } };



                var itemOffers = LineItemsCoupons(requestId, offersRequest, promoResponse, promotionDetails, customerInfo);
                offersResponse.saleItems = itemOffers.saleItems;

                //if gift amount is greater than $200 then give error response
                if (itemOffers.giftAmount > 200)
                {
                    if (errorResponse == null)
                    {
                        errorResponse = new ErrorResponse();
                    }
                    errorResponse.ResponseCode = 301;
                    errorResponse.ResponseMessage = "Redemption > $200. Please remove some offers";
                }

                //ResponseData
                if (errorResponse != null)
                {
                    string overallResult = "";
                    if (errorResponse.ResponseCode == 301 || errorResponse.ResponseCode == 0)
                    {
                        overallResult = "Success";
                    }
                    else
                    {
                        overallResult = "Fail";
                    }
                    offersResponse.responseData = new Model.OffersModel.Response.ResponseData
                    {
                        actionCode = errorResponse.ResponseCode == 8087 ? 500 : errorResponse.ResponseCode,
                        actionCodeDescription = errorResponse.ResponseMessage,
                        workstationID = offersRequest.requestData.workstationID,
                        //overallResult = errorResponse.ResponseCode == 0 ? "Success" : "Fail",
                        overallResult = overallResult,
                        requestID = offersRequest.requestData.requestID,
                        requestType = offersRequest.requestData.requestType,
                        referenceNumber = offersRequest.requestData.referenceNumber,
                        cartEvaluationID = promoResponse != null ? promoResponse.data.evaluationId : string.Empty,
                        extCorrelationID = offersRequest.requestData.extCorrelationID != null ? offersRequest.requestData.extCorrelationID : string.Empty
                    };
                    offersResponse.totalAmount = offersRequest.totalAmount;
                }
                else
                {
                    double finalAmount = 0;
                    foreach (var item in offersResponse.saleItems)
                    {
                        foreach (var priceAdjustment in item.priceAdjustments)
                        {
                            item.amount = Math.Round(item.amount - priceAdjustment.amount, 2);
                        }
                        if (item.amount < 0)
                            item.amount = 0;
                        finalAmount = Math.Round(finalAmount + item.amount, 2);
                    }
                    offersResponse.applicableVouchers = itemOffers.ApplicableVouchers;

                    var vouchers = new List<Model.OffersModel.Response.VoucherRule>();
                    offersResponse.voucherCodesResult = new List<Model.OffersModel.Response.VoucherCodesResult>();

                    int tenderId = 1;
                    foreach (var voucher in offersResponse.applicableVouchers)
                    {
                        finalAmount = Math.Round(finalAmount - Convert.ToDouble(voucher.voucherValue), 2);
                        offersResponse.tenders.Add(new Model.OffersModel.Response.Tender
                        {
                            methodOfPayment = "Voucher (fixed)",
                            methodOfPaymentID = "12",
                            voucherRules = new List<Model.OffersModel.Response.VoucherRule>{
                                new Model.OffersModel.Response.VoucherRule{
                                    voucherValue = Math.Round(Convert.ToDouble(voucher.voucherValue), 2),
                                    voucherCode = voucher.voucherCode,
                                    additionalVoucherInfo = voucher.additionalVoucherInfo,
                                    voucherQuantity = 1,
                                    promotionType = "paymentvoucher",
                                    referenceID = voucher.referenceID,
                                    voucherType = voucher.voucherType
                                }
                            },
                            totalAmount = Math.Round(Convert.ToDouble(voucher.voucherValue), 2),
                            netTenderAmount = Math.Round(Convert.ToDouble(voucher.voucherValue), 2),
                            acquirerID = "LVCH",
                            tenderID = tenderId
                        });

                        //Warning if the total voucher value is not matching with the redeemable voucher value or else send success for voucher
                        // if(voucher.voucherValue != voucher.totalVoucherValue)
                        // {
                        //     offersResponse.voucherCodesResult.Add(new VoucherCodesResult
                        //     {
                        //         actionCode = 0,
                        //         actionCodeDescription = "Voucher Over Paymanet",
                        //         voucherCode = string.IsNullOrEmpty(voucher.voucherCode) ? string.Empty : voucher.voucherCode
                        //     });
                        // }
                        if (!string.IsNullOrEmpty(voucher.voucherCode))
                        {
                            offersResponse.voucherCodesResult.Add(new VoucherCodesResult
                            {
                                actionCode = 0,
                                actionCodeDescription = "Success",
                                voucherCode = voucher.voucherCode
                            });
                        }

                        tenderId = tenderId + 1;
                    }

                    if (promoResponse.data.paymentVoucherEvaluationLogs != null && promoResponse.data.paymentVoucherEvaluationLogs.Count > 0)
                    {
                        foreach (var failureLog in promoResponse.data.paymentVoucherEvaluationLogs)
                        {
                            offersResponse.voucherCodesResult.Add(new VoucherCodesResult
                            {
                                actionCode = failureLog.errorCode == 1005 ? 400 : failureLog.errorCode,
                                actionCodeDescription = failureLog.message,
                                voucherCode = failureLog.promoCode
                            });
                        }
                    }


                    if (offersRequest.tenders != null)
                    {
                        foreach (var tender in offersRequest.tenders)
                        {

                            var tenderInformation = tendersInformation.Where(c => c.Acquirer_Id.ToUpper() == tender.acquirerID.ToUpper()).FirstOrDefault();
                            if (tenderInformation != null && tenderInformation.Acquirer_Id != "LVCH")
                            {
                                if (tenderInformation.MOP_Name.ToUpper() == "CASH")
                                {

                                    //double finalCashAmount = tender.netTenderAmount - (offersRequest.totalAmount - finalAmount);
                                    //finalAmount = finalAmount - tender.netTenderAmount;
                                    var promoTenderResponse = promoResponse.data.cartTenders.Where(c => c.identifier.ToUpper() == "CASH1").FirstOrDefault();
                                    finalAmount = Math.Round(finalAmount - Convert.ToDouble(tender.totalAmount), 2);
                                    offersResponse.tenders.Add(new Model.OffersModel.Response.Tender
                                    {
                                        methodOfPayment = tenderInformation.MOP_Name,
                                        methodOfPaymentID = tender.methodOfPaymentID,
                                        tenderID = tenderId,
                                        // netTenderAmount = finalAmount <= 0 ? 0 : Math.Floor((finalAmount * 0.1) * 100) / 100,
                                        netTenderAmount = tender.substractDiscountAmount ? Math.Round(Convert.ToDouble(promoTenderResponse.adjustedAmount), 2) : Math.Round(Convert.ToDouble(promoTenderResponse.amount), 2),
                                        acquirerID = tender.acquirerID,
                                        totalAmount = tender.totalAmount,
                                        substractDiscountAmount = tender.substractDiscountAmount
                                    });
                                }
                                else
                                {
                                    var promoTenderResponse = promoResponse.data.cartTenders.Where(c => c.identifier.ToUpper() == tenderInformation.Acquirer_Id.ToUpper()).FirstOrDefault();
                                    //tenderDiscount = tenderDiscount + (Convert.ToDouble(dicountTender.amount) - Convert.ToDouble(dicountTender.adjustedAmount));
                                    if (promoTenderResponse != null)
                                    {
                                        offersResponse.tenders.Add(new Model.OffersModel.Response.Tender
                                        {
                                            methodOfPayment = tenderInformation.MOP_Name,
                                            methodOfPaymentID = tender.methodOfPaymentID,
                                            tenderID = tenderId,
                                            netTenderAmount = tender.substractDiscountAmount ? Math.Round(Convert.ToDouble(promoTenderResponse.adjustedAmount), 2) : Math.Round(Convert.ToDouble(promoTenderResponse.amount), 2),
                                            acquirerID = tender.acquirerID,
                                            totalAmount = tender.totalAmount,
                                            substractDiscountAmount = tender.substractDiscountAmount
                                        });
                                    }
                                    finalAmount = Math.Round(finalAmount - Convert.ToDouble(promoTenderResponse.adjustedAmount), 2);
                                }
                                tenderId = tenderId + 1;
                            }
                        }
                    }

                    if (finalAmount <= 0)
                        finalAmount = 0.00;


                    offersResponse.totalAmount = Math.Round(finalAmount, 2);
                    offersResponse.remainder = Math.Round(finalAmount, 2);
                    //  if (itemOffers.MOPDiscount > 0)
                    //       offersResponse.totalAmount = Math.Round(finalAmount + itemOffers.MOPDiscount, 2);

                    if (!isNotInterested)
                    {
                        if (itemOffers.MOPDiscount > 0)
                            finalAmount = Math.Round(finalAmount - itemOffers.MOPDiscount, 2);

                        if (finalAmount <= 0)
                            finalAmount = 0.00;

                        //Old Logic
                        // if (finalAmount % 0.25 > 0)
                        // {
                        //     double RoundedAmount = Math.Round(finalAmount, 1);
                        //     if ((RoundedAmount - finalAmount) > 0)
                        //         finalAmount = Math.Round(finalAmount, 1) - 0.05;
                        //     else
                        //         finalAmount = RoundedAmount;
                        // }

                        //Rounding down the 2nd decimal digit of the number to the nearest 0 or 5 value.
                        if (finalAmount.ToString().Contains('.'))
                        {
                            var number = Regex.Split(String.Format("{0:0.00}", finalAmount), @"\D+");
                            if (Convert.ToInt32(number[1]) % 10 >= 5)
                                finalAmount = finalAmount - ((Convert.ToDouble(number[1]) % 10) - 5) / 100;
                            else
                                finalAmount = finalAmount - ((Convert.ToDouble(number[1]) % 10)) / 100;
                        }


                        string cashierAmountFormat = String.Format("{0:0.00}", finalAmount);
                        if (offersRequest.predictedTender != null)
                        {
                            offersResponse.predictedTender = new Model.OffersModel.Response.PredictedTender
                            {
                                amount = Convert.ToDouble(String.Format("{0:0.00}", finalAmount)),
                                methodOfPayment = offersRequest.predictedTender.methodOfPayment,
                                acquirer = offersRequest.predictedTender.acquirer,
                                substractDiscountAmount = Convert.ToBoolean(offersRequest.predictedTender.substractDiscountAmount),
                            };
                        }
                        offersResponse.Messages = new Messages
                        {
                            cashierMessage = string.Format("Cash amount after disc will be ${0}", cashierAmountFormat)
                        };
                    }

                    //offersResponse.receipt.receiptLines.AddRange(itemOffers.ReceiptLines);;
                    offersResponse.responseData = new Model.OffersModel.Response.ResponseData
                    {
                        actionCode = 0,
                        actionCodeDescription = itemOffers.offersAvaliable ? "Offers Available" : "No Offers Available",
                        workstationID = offersRequest.requestData.workstationID,
                        overallResult = "Success",
                        referenceNumber = offersRequest.requestData.referenceNumber,
                        requestID = offersRequest.requestData.requestID,
                        requestType = offersRequest.requestData.requestType,
                        cartEvaluationID = promoResponse != null && promoResponse.data != null ? promoResponse.data.evaluationId : string.Empty,
                        extCorrelationID = offersRequest.requestData.extCorrelationID != null ? offersRequest.requestData.extCorrelationID : string.Empty
                    };
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in Mapper.Map(OffersResponse Mapping) .Message:'{0}'", requestId, e.Message);
            }

            return JsonConvert.SerializeObject(offersResponse);
        }


        //Phase-2 saleitem offers response
        internal static (List<Model.OffersModel.Response.SaleItem> saleItems, bool offersAvaliable, List<ApplicableVoucher> ApplicableVouchers, List<string> ReceiptLines, double MOPDiscount, double giftAmount) LineItemsCoupons(string requestId, OffersRequest offersRequest, Capillary.ShellProxy.Model.PromotionModel.Response.PromotionResponse promotions, PromotionDetailsResponse promotionDetails, lookupResponse customerInfo)
        {
            var ResponseSaleItems = new List<Model.OffersModel.Response.SaleItem>();
            var applicableVouchers = new List<ApplicableVoucher>();
            var saleItemcategories = new List<Model.OffersModel.Response.Product>();
            var receiptLines = new List<string>();
            bool offers = false;
            bool cashOfferEnable = false;
            bool mobileRequest = false;
            double GiftAmount = 0.0d;
            double mopDiscount = 0.0d;
            try
            {
                //Identifying whether the request is from mobile number 
                if (offersRequest.customerData != null && offersRequest.customerData.Count() > 0)
                {
                    if (!string.IsNullOrEmpty(offersRequest.customerData[0].customerDataType) && offersRequest.customerData[0].customerDataType.ToLower().Contains("mobile"))
                    {
                        Console.WriteLine("RequestId:{0}. Customer is using mobile number to fetch the offers", requestId);
                        mobileRequest = true;
                    }
                }

                //Identifying whether the request is for Parallel run users or not
                if (customerInfo != null && !string.IsNullOrEmpty(customerInfo.statusLabel) && customerInfo.statusLabel.ToUpper() == "PARALLELRUN")
                {
                    Console.WriteLine("RequestId:{0}. Customer is identified as parallel run customer", requestId);
                    mobileRequest = true;
                }


                foreach (var saleItem in offersRequest.saleItems)
                {
                    try
                    {
                        var ResponseSaleItem = new Model.OffersModel.Response.SaleItem
                        {
                            additionalProductCode = saleItem.additionalProductCode,
                            additionalProductInfo = saleItem.additionalProductInfo,
                            // amount = saleItem.amount,
                            amount = Math.Round(saleItem.amount, 2),
                            categoryCode = saleItem.categoryCode,
                            itemID = saleItem.itemID,
                            originalAmount = saleItem.originalAmount,
                            productCode = saleItem.productCode,
                            quantity = saleItem.quantity,
                            saleItemType = saleItem.saleItemType,
                            unitMeasure = saleItem.unitMeasure,
                            unitPrice = saleItem.unitPrice,
                            vatRate = saleItem.vatRate,
                            loyaltyOffers = new List<Model.OffersModel.Response.LoyaltyOffer>(),
                            priceAdjustments = new List<Model.OffersModel.Response.PriceAdjustment>()

                        };

                        //check for cash offer to apply or not
                        if (offersRequest.tenders != null && offersRequest.tenders.Count() > 0)
                        {
                            foreach (var payment in offersRequest.tenders)
                            {
                                if (!string.IsNullOrEmpty(payment.methodOfPayment) && payment.methodOfPayment.ToLower().Contains("cash"))
                                    cashOfferEnable = true;

                            }
                        }

                        if (promotions != null && promotions.data != null)
                        {
                            //string productCode = string.IsNullOrEmpty(saleItem.productCode) ? saleItem.additionalProductCode : saleItem.productCode;
                            var OfferItems = promotions.data.cartItems.Where(item => item.sku.ToUpper() == saleItem.additionalProductCode.ToUpper());

                            foreach (var OfferItem in OfferItems)
                            {
                                int priceAdjustmentID = 1;
                                foreach (var appliedpromotion in OfferItem.appliedPromotions)
                                {
                                    var promotionDetail = promotionDetails != null && promotionDetails.data != null ? promotionDetails.data.Where(c => c.promotionId == appliedpromotion.promotionId).FirstOrDefault() : null;
                                    string mobile_Applicable = promotionDetail != null && promotionDetail.customFieldValues != null && promotionDetail.customFieldValues.mobile_applicable != null ? promotionDetail.customFieldValues.mobile_applicable : "true";
                                    if (Convert.ToDouble(appliedpromotion.discount) > 0)
                                    {
                                        //group up all gift amount
                                        if (appliedpromotion.messageLabel.ToLower().Contains("gift"))
                                            GiftAmount = GiftAmount + Convert.ToDouble(appliedpromotion.discount);

                                        //control cash offer based on the input
                                        if (appliedpromotion.messageLabel.ToLower().Contains("cash") || appliedpromotion.name.ToLower().Contains("cash"))
                                        {
                                            if (appliedpromotion.tenderIdentifier.ToLower() == "cash" && appliedpromotion.tenderType.ToLower() == "cash")
                                            {
                                                cashOfferEnable = false;
                                            }
                                            //separate out cash discounts
                                            if (!cashOfferEnable)
                                            {
                                                mopDiscount = mopDiscount + Convert.ToDouble(appliedpromotion.discount);
                                                continue;
                                            }
                                        }

                                        //  //separate out cash discounts
                                        // if (appliedpromotion.messageLabel.ToLower().Contains("cash") || appliedpromotion.name.ToLower().Contains("cash"))
                                        //     mopDiscount = mopDiscount + Convert.ToDouble(appliedpromotion.discount);


                                        //ignore some offer if request is from mobile number based on the offer metadata configured
                                        if (mobile_Applicable.ToLower() == "false" && mobileRequest)
                                            continue;

                                        offers = true;
                                        var long_dec = promotionDetail != null && promotionDetail.customFieldValues != null && promotionDetail.customFieldValues.long_name != null ? promotionDetail.customFieldValues.long_name : appliedpromotion.name;
                                        ResponseSaleItem.loyaltyOffers.Add(new Model.OffersModel.Response.LoyaltyOffer
                                        {
                                            loyaltyOfferID = appliedpromotion.promotionId,
                                            promotionType = "lineitem",
                                            loyaltyOfferDescription = promotionDetail != null && promotionDetail.customFieldValues != null && promotionDetail.customFieldValues.long_name != null ? promotionDetail.customFieldValues.long_name : appliedpromotion.name,
                                            referenceID = appliedpromotion.identifier
                                        });

                                        ResponseSaleItem.priceAdjustments.Add(new Model.OffersModel.Response.PriceAdjustment
                                        {
                                            additionalProductCode = saleItem.additionalProductCode,
                                            priceAdjustmentType = appliedpromotion.messageLabel.ToUpper().Contains("GIFT") ? "Gift" : "RealtimeOffer-A",
                                            amount = Math.Round(Convert.ToDouble(appliedpromotion.discount),2),
                                            categoryCode = saleItem.categoryCode,
                                            promotionType = "lineitem",
                                            referenceID = appliedpromotion.identifier,
                                            loyaltyOfferID = appliedpromotion.promotionId,
                                            priceAdjustmentID = priceAdjustmentID,
                                            quantity = Math.Round(Convert.ToDouble(appliedpromotion.promotionAppliedOnQuantity), 3) <= 0 ? saleItem.quantity : Math.Round(Convert.ToDouble(appliedpromotion.promotionAppliedOnQuantity), 3),
                                            reason = appliedpromotion.name,
                                            unitPrice = Math.Round(Convert.ToDecimal(appliedpromotion.promotionAppliedOnQuantity) <= 0 ? Convert.ToDecimal(appliedpromotion.discount) / Convert.ToDecimal(saleItem.quantity) : Convert.ToDecimal(appliedpromotion.discount) / Convert.ToDecimal(appliedpromotion.promotionAppliedOnQuantity), 2),

                                        });
                                        priceAdjustmentID = priceAdjustmentID + 1;
                                        if (!receiptLines.Contains(long_dec))
                                            receiptLines.Add(long_dec);
                                    }
                                }
                            }
                        }

                        // if (promotions != null && promotions.data != null && promotions.data.appliedPromotions.Count > 0)
                        // {
                        //     foreach (var cartpromo in promotions.data.appliedPromotions)
                        //     {
                        //         if (Convert.ToDouble(cartpromo.discount) > 0)
                        //         {
                        //             if (cartpromo.messageLabel.ToLower().Contains("gift"))
                        //                 GiftAmount = GiftAmount + Convert.ToDouble(cartpromo.discount);

                        //             var cartPromotionDetail = promotionDetails != null && promotionDetails.data != null ? promotionDetails.data.Where(c => c.promotionId == cartpromo.promotionId).FirstOrDefault() : null;
                        //             offers = true;
                        //             var cart_long_desc = cartPromotionDetail != null && cartPromotionDetail.customFieldValues != null && cartPromotionDetail.customFieldValues.long_name != null ? cartPromotionDetail.customFieldValues.long_name : cartpromo.name;

                        //             var existingVoucher = applicableVouchers.Where(c => c.referenceID == cartpromo.identifier).FirstOrDefault();
                        //             if (existingVoucher == null)
                        //             {
                        //                 var applicableVoucher = new ApplicableVoucher
                        //                 {
                        //                     voucherCode = cartpromo.promotionId,
                        //                     additionalVoucherInfo = cartpromo.name,
                        //                     voucherType = "F",
                        //                     referenceID = cartpromo.identifier,
                        //                     promotionType = "cart",
                        //                     expiryDate = Helper.UnixTimeStampToDateTime(cartPromotionDetail.expiry).ToString("yyyy-MM-ddTHH:MM:ss+05:30"),
                        //                     voucherValue = Math.Round(Convert.ToDecimal(cartpromo.discount), 2).ToString(),
                        //                     products = null,
                        //                     //acquirerID = cartpromo.messageLabel                    
                        //                 };
                        //                 applicableVouchers.Add(applicableVoucher);
                        //             }
                        //             if (!receiptLines.Contains(cart_long_desc))
                        //                 receiptLines.Add(cart_long_desc);
                        //         }
                        //     }
                        // }

                        //saleItemcategories.Add(new Model.OffersModel.Response.Product { categoryCode = saleItem.categoryCode });
                        ResponseSaleItems.Add(ResponseSaleItem);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("RequestId:{0}.Exception encountered in Mapper.Map(LineItemJson) for item {1} .Message:'{2}'", requestId, saleItem.itemID, e.Message);
                    }
                }

                if (promotions != null && promotions.data != null)
                {
                    // foreach (var applicableVoucher in applicableVouchers)
                    // {
                    //     if (applicableVoucher.products == null)
                    //     {
                    //         applicableVoucher.products = new List<Model.OffersModel.Response.Product>();
                    //         applicableVoucher.products.AddRange(saleItemcategories);
                    //     }
                    // }

                    //new implementation for singapore for PaymentVouchers
                    foreach (var paymentVoucher in promotions.data.appliedPaymentVouchers)
                    {
                        var promotionDetail = promotionDetails != null && promotionDetails.data != null ? promotionDetails.data.Where(c => c.promotionId == paymentVoucher.promotionId).FirstOrDefault() : null;
                        string mobile_Applicable = promotionDetail != null && promotionDetail.customFieldValues != null && promotionDetail.customFieldValues.mobile_applicable != null ? promotionDetail.customFieldValues.mobile_applicable : "true";

                        //ignore some offer if request is from mobile number based on the offer metadata configured
                        if (mobile_Applicable.ToLower() == "false" && mobileRequest)
                            continue;

                        //if(paymentVoucher.totalVoucherValue == paymentVoucher.redeemableVoucherValue)
                        //{
                        applicableVouchers.Add(new ApplicableVoucher
                        {
                            voucherCode = string.IsNullOrEmpty(paymentVoucher.promoCode) ? paymentVoucher.promotionId : paymentVoucher.promoCode,
                            additionalVoucherInfo = paymentVoucher.name,
                            voucherType = paymentVoucher.messageLabel.ToLower().Contains("fuel") ? "F" : "NFR",
                            referenceID = paymentVoucher.identifier,
                            promotionType = "cart",
                            //expiryDate = Helper.UnixTimeStampToDateTime(cartPromotionDetail.expiry).ToString("yyyy-MM-ddTHH:MM:ss+05:30"),
                            voucherValue = Math.Round(Convert.ToDecimal(paymentVoucher.redeemableVoucherValue), 2).ToString(),
                            //totalVoucherValue = Math.Round(Convert.ToDecimal(paymentVoucher.totalVoucherValue), 2).ToString()
                        });
                        //}
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in Mapper.Map(LineItemJson) .Message:'{0}'", requestId, e.Message);
            }
            return (saleItems: ResponseSaleItems, offersAvaliable: offers, ApplicableVouchers: applicableVouchers, ReceiptLines: receiptLines, MOPDiscount: mopDiscount, giftAmount: GiftAmount);
        }
    
        public static string Map(string requestId, CancelTransactionResponse APIResponse, Model.ShellTransactionModel.Request.Object retailRequest, string lookupRespGetErrorMsg)
        {
            try
            {
                var ShellResponse = new ShellTransactionResponse
                {
                    requestData = new Model.ShellTransactionModel.Response.RequestData
                    {
                        requestID = retailRequest != null ? retailRequest.requestData.requestID : "",
                        overallResult = string.IsNullOrEmpty(lookupRespGetErrorMsg) 
                                        ? (APIResponse != null &&APIResponse.errors != null && APIResponse.errors.Count() > 0 ? APIResponse.errors[0].message : ( APIResponse.data.status == true ? "Cancel Transaction successful" :APIResponse.data.message))
                                        : lookupRespGetErrorMsg
                    },
                    responseData = new Model.ShellTransactionModel.Response.ResponseData
                    {
                        requestType = retailRequest != null ? retailRequest.requestData.requestType : string.Empty
                    }
                };
                return JsonConvert.SerializeObject(ShellResponse);
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in Mapper.Map(CancelTransactionResponse, Model.ShellTransactionModel.Request.Object).Message:'{0}'", requestId, e.Message);
            }
            return string.Empty;
        }
    }
}