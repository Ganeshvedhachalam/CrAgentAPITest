using System;
using System.Collections.Generic;

namespace Capillary.ShellProxy.Model.TransactionModel.Response
{
    public class Status
    {
        public string success { get; set; }
        public int code { get; set; }
        public string message { get; set; }
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
    }

    public class PointsSummaries
    {
        public List<PointsSummary> points_summary { get; set; }
    }

    public class Customer
    {
        public string user_id { get; set; }
        public string mobile { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string email { get; set; }
        public string external_id { get; set; }
        public string lifetime_points { get; set; }
        public string loyalty_points { get; set; }
        public string current_slab { get; set; }
        public string tier_expiry_date { get; set; }
        public PointsSummaries points_summaries { get; set; }
        public string lifetime_purchases { get; set; }
        public string type { get; set; }
        public string source { get; set; }
    }

    public class Effect
    {
        public int id { get; set; }
        public string coupon_type { get; set; }
        public string coupon_code { get; set; }
        public string valid_till { get; set; }
        public string description { get; set; }
        public string discount_code { get; set; }
        public string type { get; set; }
    }

    public class SideEffects
    {
        public List<Effect> effect { get; set; }
    }

    public class ItemStatus
    {
        public string success { get; set; }
        public int code { get; set; }
        public string message { get; set; }
    }

    public class Transaction
    {
        public Transaction()
        {
            item_status = new ItemStatus();
            customer = new Customer();
            side_effects = new SideEffects();
        }
        public string id { get; set; }
        public string shipping_source_till_code { get; set; }
        public string number { get; set; }
        public string bill_client_id { get; set; }
        public string type { get; set; }
        public string delivery_status { get; set; }
        public string parent_bill_number { get; set; }
        public string outlier_status { get; set; }
        public Customer customer { get; set; }
        public SideEffects side_effects { get; set; }
        public string source { get; set; }
        public ItemStatus item_status { get; set; }
    }

    public class Transactions
    {
        public Transactions()
        {
            transaction = new List<Transaction>();
        }
        public List<Transaction> transaction { get; set; }
    }

    public class Response
    {
        // public Response()
        // {
        //     status = new Status();
        //     transactions = new Transactions();
        // }
        public Status status { get; set; }
        public Transactions transactions { get; set; }
    }

    public class TransactionResponse
    {
        // public TransactionResponse()
        // {
        //     response = new Response();
        // }
        public Response response { get; set; }
    }
}
