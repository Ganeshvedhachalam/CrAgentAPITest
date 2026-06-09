using System;
using System.Collections.Generic;
using System.Text;

namespace GEN_Shell_MobileBackend_API.Models
{
    
    public class GetCustomerTransaction
    {
        public Response response { get; set; }
    }
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Attribute
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class Attributes
    {
        public List<Attribute> attribute { get; set; }
    }

    public class Customer
    {
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string user_id { get; set; }
        public string mobile { get; set; }
        public string email { get; set; }
        public object external_id { get; set; }
        public int lifetime_points { get; set; }
        public int lifetime_purchases { get; set; }
        public int loyalty_points { get; set; }
        public string current_slab { get; set; }
        public string registered_on { get; set; }
        public string updated_on { get; set; }
        public string type { get; set; }
        public string source { get; set; }
        public List<object> user_groups2 { get; set; }
        public int count { get; set; }
        public string start { get; set; }
        public string delayed_points { get; set; }
        public string delayed_returned_points { get; set; }
        public string total_available_points { get; set; }
        public string total_returned_points { get; set; }
        public string rows { get; set; }
        public Transactions transactions { get; set; }
        public ItemStatus item_status { get; set; }
    }

    //public class CustomFields
    //{
    //    public List<Field> field { get; set; }
    //}

    public class ExtendedFields
    {
        public List<Field> field { get; set; }
    }

    public class Field
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class Identifiers
    {
        public List<object> field { get; set; }
    }

    public class ItemStatus
    {
        public string success { get; set; }
        public string code { get; set; }
        public string message { get; set; }
        public Warnings warnings { get; set; }
    }

    public class LineItem
    {
        public string id { get; set; }
        public string type { get; set; }
        public string return_type { get; set; }
        public string outlier_status { get; set; }
        public string serial { get; set; }
        public string item_code { get; set; }
        public string description { get; set; }
        public string qty { get; set; }
        public string rate { get; set; }
        public string value { get; set; }
        public string discount { get; set; }
        public string amount { get; set; }
        public List<LineItemPointDetail> line_item_point_details { get; set; }
        public ExtendedFields extended_fields { get; set; }
        public List<object> split_items { get; set; }
        public List<object> addon_items { get; set; }
        public List<object> combo_items { get; set; }
        public Attributes attributes { get; set; }
    }

    public class LineItemPointDetail
    {
        public string issued { get; set; }
        public string promised { get; set; }
        public string promised_returned { get; set; }
        public string redeemed { get; set; }
        public string returned { get; set; }
        public string redeemable_from { get; set; }
        public string expiry_date { get; set; }
        public string program_id { get; set; }
    }

    public class LineItems
    {
        public List<LineItem> line_item { get; set; }
    }

    public class Points
    {
        public string issued { get; set; }
        public string redeemed { get; set; }
        public string returned { get; set; }
        public string expired { get; set; }
        public string returnedPointsOnBill { get; set; }
        public string expiry_date { get; set; }
        public string program_id { get; set; }
    }

    public class Response
    {
        public Status status { get; set; }
        public Customer customer { get; set; }
    }

    

    public class Status
    {
        public string success { get; set; }
        public int code { get; set; }
        public string message { get; set; }
    }

    public class Tender
    {
        public string name { get; set; }
        public string value { get; set; }
        public Attributes attributes { get; set; }
    }

    public class Tenders
    {
        public List<Tender> tender { get; set; }
    }

    public class Transaction
    {
        public string id { get; set; }
        public string number { get; set; }
        public string type { get; set; }
        public string user_group2_id { get; set; }
        public string return_type { get; set; }
        public string amount { get; set; }
        public string outlier_status { get; set; }
        public string delivery_status { get; set; }
        public string notes { get; set; }
        public string billing_time { get; set; }
        public string auto_update_time { get; set; }
        public string gross_amount { get; set; }
        public string discount { get; set; }
        public string store { get; set; }
        public string store_code { get; set; }
        public string returned_points_on_bill { get; set; }
        public Points points { get; set; }
        public CustomFields custom_fields { get; set; }
        public ExtendedFields extended_fields { get; set; }
        public Identifiers identifiers { get; set; }
        public List<object> coupons { get; set; }
        public int basket_size { get; set; }
        public LineItems line_items { get; set; }
        public Tenders tenders { get; set; }
    }

    public class Transactions
    {
        public List<Transaction> transaction { get; set; }
    }

    public class Warnings
    {
        public List<object> warning { get; set; }
    }


}
