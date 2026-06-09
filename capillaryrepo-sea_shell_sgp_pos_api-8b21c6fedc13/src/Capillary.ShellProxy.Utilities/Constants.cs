namespace Capillary.ShellProxy.Utilities
{
    public static class Constants
    {
        //S3
        public const string FolderFailedTransactions = "failedtransactions";
         public const string FolderTransactionWarnings = "transactionswarnings";
        public const string FolderFailedRedemptions = "failedredemptions";
        public const string FolderRefundCoupons = "refundcoupons";

        //Organization Details
        public const string OrgName = "SHELLINDONESIADEMO";


        //Environment and NewRelic
        public const string production = "prod";
        public const string demo = "demo";
        public const string malaysia = "singapore";
        public const string eventName = "POS_TransactionCodes";
        public const string applicationName = "SGP-POS-API";
        public const string snsARN = "arn:aws:sns:ap-southeast-1:538938394727:Shell-Metrics";

        //DynamoDB
        public const string TableProductsMappings = "Shell_IDN_Products";
        public const string TableLocationsMappings = "ShellLocations";
        public const string TableCustomerCacheMappings = "Shell_IDN_CustomerCache";
         public const string TableTxnAmountMappings = "Shell_TxnAmount";
        public const string TableTendersMappings = "ShellTenders";
        public const string AttributeAcquirer_Id = "Acquirer_Id";
        public const string AttributeMOP_Name = "MOP_Name";
        public const string AttributeMode = "Mode";
        public const string AttributeMOP_ID = "MOP_ID";
        public const string AttributeCrmProductId = "CRMProductId";
        public const string AttributeCrmLocationtId = "CRMLocId";
        public const string AttributeClientID = "ClientID";
        public const string AttributeGlobalSiteId = "GlobalSiteId";
        public const string AttributeEcomProductId = "EcomProductId";
        public const string AttributeEcomLocationtId = "EcomLocId";
        public const string AttributeCatergoryId = "CategoryId";
        public const string AttributeIdentifierValue = "IdentifierValue";
        public const string AttributeProgramName = "ProgramName";
        public const string AttributeDateTime = "DateTime";

        public const string AttributeTxnNumber = "TxnNumber";
        public const string AttributeAmount = "Amount";

        //InTouch
        public const string EndpointCouponRedeem = "/v2/coupon/bulk/redeem";
        public const string EndpointCustomerGet = "/v1.1/customer/get?{0}={1}&coupon_active=true&coupon_limit=50";
        public const string EndpointCustomerGetSegments = "/v1.1/customer/get?{0}={1}&mlp=true&segments=true";
        public const string EndpointIsRedeemable = "/v2/coupon/bulk/redeem?is_redeemable=true";
        public const string EndpointTransactionAdd = "/v2/transactions/bulk";
        public const string EndpointCustomerCouponsGet = "/v1.1/customer/coupons?{0}={1}&status=active";
        public const string EndpointCustomerlookup = "/v2/customers/lookup/customerDetails?source=INSTORE&identifierName={0}&identifierValue={1}&embed=points,CUSTOMERSTATUS&includedFraudDetails=FALSE&includedOnlyCurrentProfile=TRUE&includedNPS=FALSE&basicIdentifierLookup=TRUE";
        public const string EndpointCardDetails = "/v2/card?number={0}";
        public const string EndpointStoreDetailsGet = "/v1.1/store/get?format=json&external_id={0}";
        public const string EndpointOrgCustomFieldsGet = "/v1.1/organization/customfields?format=json";
        public const string EndpointCouponSeriesGet = "/v2/coupon/series?ids={0}";
        public const string EndpointPromoEvalute = "/api_gateway/v1/promotions/evaluate";
         public const string EndpointPromoDetails = "/api_gateway/v1/promotions/config?includeExpired=true&";
        public const string EndpointGetRewards = "/core/v1/user/reward/brand/SHELLINDONESIADEMO?group=POS";
        public const string EndpointIssueRewards = "/core/v1/user/rewards/issue?username=demo.shell.id.11546823.1&skipValidation=true";
        public const string EndPointCustomerStatus = "/v2/customers/{0}/statusLog";

        public const string EndPointCustomerCancelEvaluation= "/api_gateway/v1/promotions/customer/{0}/evaluations/{1}/cancel";
        public const string HeaderWaitForDsAsync = "WAIT_FOR_DOWNSTREAM";
        public const string HeaderUseAsync = "use_async";
        public const string HeaderAuthorization = "Authorization";
        public const string HeaderUserAgent = "User-Agent";
        public const string HeaderUserAgentValue = "shell-MDW-{0}";
        public const string ExtendedDeletedMemberKey = "member_type";
        public const string ExtendedDeletedMemberValue = "deleted";
        //Ecom
        public const string EndpointAccessTokenGet = "/Customer/GetAccessToken/{0}";
        public const string EndpointCartAdd = "/Carts/AddCartItems/{0}";
        public const string EndpointProductUpdate = "/product/UpdateLocationWiseStock";

        //Transaction Custom Fields
        public const string CountryCode = "CountryCode";
        public const string LoyaltyType = "membership_card";

        //Transaction Extended Fields
        public const string customerDataType = "order_channel";
        public const string point_discount = "point_discount";
        public const string membership_card_swiped  = "membership_card_swiped ";


        //LineItem Attributes
        public const string subCategoryCode = "Sub_category_Code";
        public const string categoryCode = "Category_Code";
        public const string legacyCategoryCode = "legacyCategoryCode";

        //LineItem Extended Fields
        public const string netAmount = "total_unit_cost";
        public const string originalNetAmount = "amount_excluding_tax";
        public const string vat = "vat_amount";
        public const string unitVat = "vat_amount_on_unit_price";
        public const string vatRate = "vat_tax_percentage";
        public const string unitMeasure = "size";
        public const string saleChannel = "service_type";
        public const string markDownIndicator = "price_override_applied";
        public const string priceAdjustmentType = "discount_type";
        public const string Quantity = "items_in_product_set";
        public const string discount_description = "discount_description";
        public const string delivery_charge_including_tax = "delivery_charge_including_tax";
        public const string CentralGST = "CentralGST";
        public const string StateGST = "StateGST";
        public const string tax_amount = "tax_amount";
        public const string IntegratedGST = "IntegratedGST";



        //RedeemCoupon Custom fields
        public const string SiteID = "siteid";
        public const string DiscountAmount = "amount";

        //Misc
        public const string LogTransaction = "Transaction.RequestId:{0}";
        public const string LogOffer = "Offer.RequestId:{0}";


        //Shell
        public const string TenderVoucherPayment = "digital voucher";

        //CloudWatch
        public const string MetricNamespace = "shell-custom-metrics";
        public const string MetricStatus = "status";
        public const string MetricTime = "time";
        public const string DimEventName = "eventName";
        public const string DimStatusCode = "statusCode";
        public const string DimMetricTime = "time";


    }
}