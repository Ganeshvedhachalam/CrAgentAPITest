using System;
using System.Collections.Generic;

namespace Capillary.ShellProxy.Model.CustomerCouponModel.Response
{
    public class Pagination    {
        public string limit { get; set; } 
        public string offset { get; set; } 
        public int total { get; set; } 
    }

    public class Status    {
        public string success { get; set; } 
        public int code { get; set; } 
        public string message { get; set; } 
    }

    public class IssuedAt    {
        public string code { get; set; } 
        public string name { get; set; } 
    }

    public class RedeemedAt    {
        public string code { get; set; } 
        public string name { get; set; } 
    }

    public class Redemption    {
        public DateTime date { get; set; } 
        public string transaction_number { get; set; } 
        public RedeemedAt redeemed_at { get; set; } 
    }

    public class Redemptions    {
        public List<Redemption> redemption { get; set; } 
    }

    public class Coupon    {
        public string id { get; set; } 
        public string series_id { get; set; } 
        public string series_name { get; set; } 
        public int redemption_count { get; set; } 
        public DateTime created_date { get; set; } 
        public string valid_till { get; set; } 
        public string code { get; set; } 
        public object transaction_number { get; set; } 
        public IssuedAt issued_at { get; set; } 
        public Redemptions redemptions { get; set; } 
    }

    public class Coupons    {
        public List<Coupon> coupon { get; set; } 
    }

    public class ItemStatus    {
        public string success { get; set; } 
        public int code { get; set; } 
        public string message { get; set; } 
    }

    public class Customer    {
        public string firstname { get; set; } 
        public string lastname { get; set; } 
        public object email { get; set; } 
        public string external_id { get; set; } 
        public string mobile { get; set; } 
        public string id { get; set; } 
        public Coupons coupons { get; set; } 
        public ItemStatus item_status { get; set; } 
    }

    public class Customers    {
        public List<Customer> customer { get; set; } 
    }

    public class Response    {
        public Pagination pagination { get; set; } 
        public Status status { get; set; } 
        public Customers customers { get; set; } 
    }

    public class CustomerCouponResponse    {
        public Response response { get; set; } 
    }


}