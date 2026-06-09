using System;
using System.Collections.Generic;

namespace Capillary.ShellProxy.Model.CustomerModel.Response
{
     public class Status
    {
        public string success { get; set; }
        public int code { get; set; }
        public string message { get; set; }
        public string total { get; set; }
        public string success_count { get; set; }
    }

    public class RegisteredStore
    {
        public string code { get; set; }
        public string name { get; set; }
    }

    public class RegisteredTill
    {
        public string code { get; set; }
        public string name { get; set; }
    }

    public class FraudDetails
    {
        public string status { get; set; }
        public string marked_by { get; set; }
        public string modified_on { get; set; }
    }

    public class Field
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class CustomFields
    {
        public List<Field> field { get; set; }
    }

    public class Field2
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class ExtendedFields
    {
        public List<Field2> field { get; set; }
    }

    public class Transaction
    {
        public string id { get; set; }
        public string number { get; set; }
        public string type { get; set; }
        public string created_date { get; set; }
        public string store { get; set; }
    }

    public class Transactions
    {
        public List<Transaction> transaction { get; set; }
    }

    public class Coupons
    {
        public List<Coupon> coupon { get; set; }
    }

    public class Coupon
    {
        public string id { get; set; }
        public string series_id { get; set; }
        public string code { get; set; }
        public string description { get; set; }
        public string created_date { get; set; }
        public string valid_till { get; set; }
        public string redeemed { get; set; }
    }

    public class LinkedPartnerPrograms
    {
        public List<object> linked_partner_program { get; set; }
    }

    public class GapToUpgrade
    {
        public List<object> upgrade_strategy { get; set; }
    }

    public class GapToRenew
    {
    }

    public class PointsSummary
    {
        public string programId { get; set; }
        public string redeemed { get; set; }
        public string expired { get; set; }
        public string returned { get; set; }
        public string adjusted { get; set; }
        public string lifetimePoints { get; set; }
        public string loyaltyPoints { get; set; }
        public string cumulativePurchases { get; set; }
        public string currentSlab { get; set; }
        public string nextSlab { get; set; }
        public string nextSlabSerialNumber { get; set; }
        public string nextSlabDescription { get; set; }
        public string slabSNo { get; set; }
        public string slabExpiryDate { get; set; }
        public string totalPoints { get; set; }
        public string delayed_points { get; set; }
        public string delayed_returned_points { get; set; }
        public string total_available_points { get; set; }
        public string total_returned_points { get; set; }
        public string program_title { get; set; }
        public string program_description { get; set; }
        public string program_points_to_currency_ratio { get; set; }
        public LinkedPartnerPrograms linked_partner_programs { get; set; }
        public GapToUpgrade gap_to_upgrade { get; set; }
        public GapToRenew gap_to_renew { get; set; }
    }

    public class PointsSummaries
    {
        public List<PointsSummary> points_summary { get; set; }
    }

    public class GroupPointsSummaries
    {
        public List<object> group_points_summary { get; set; }
    }

    public class Warnings
    {
        public List<object> warning { get; set; }
    }

    public class ItemStatus
    {
        public string success { get; set; }
        public string code { get; set; }
        public string message { get; set; }
        public Warnings warnings { get; set; }
    }

    public class Customer
    {
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string mobile { get; set; }
        public string email { get; set; }
        public object external_id { get; set; }
        public string registered_on { get; set; }
        public string updated_on { get; set; }
        public string type { get; set; }
        public string source { get; set; }
        public List<object> identifiers { get; set; }
        public object gender { get; set; }
        public string registered_by { get; set; }
        public RegisteredStore registered_store { get; set; }
        public RegisteredTill registered_till { get; set; }
        public FraudDetails fraud_details { get; set; }
        public string trackers { get; set; }
        public object current_nps_status { get; set; }
        public CustomFields custom_fields { get; set; }
        public ExtendedFields extended_fields { get; set; }
        public Transactions transactions { get; set; }
        public Coupons coupons { get; set; }
        public List<object> notes { get; set; }
        public PointsSummaries points_summaries { get; set; }
        public GroupPointsSummaries group_points_summaries { get; set; }
        public ItemStatus item_status { get; set; }
    }

    public class Customers
    {
        public List<Customer> customer { get; set; }
    }

    public class Response
    {
        public Status status { get; set; }
        public Customers customers { get; set; }
    }

    public class CustomerResponse
    {
        public Response response { get; set; }
    }

}
