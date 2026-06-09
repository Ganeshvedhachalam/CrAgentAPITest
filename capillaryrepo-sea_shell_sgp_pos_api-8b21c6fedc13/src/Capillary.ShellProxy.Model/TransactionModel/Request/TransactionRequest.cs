using System;
using System.Collections.Generic;

namespace Capillary.ShellProxy.Model.TransactionModel.Request
{
    public class Customer
    {
        public string mobile { get; set; }
        public string email { get; set; }
        public string external_id { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
    }

    public class Redemptions
    {
        public List<int> pointsRedemptions { get; set; }
        public List<string> couponRedemptions { get; set; }
    }

    public class Field
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class ExtendedFields
    {
        public List<Field> field { get; set; }
    }

    public class Attribute
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class Attributes
    {
        public List<Attribute> attribute { get; set; }
    }

    public class Payment
    {
        public object mode { get; set; }
        public object value { get; set; }
        public Attributes attributes { get; set; }
        public string notes { get; set; }
    }

    public class PaymentDetails
    {
        public List<Payment> payment { get; set; }
    }

    public class Field2
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class CustomFields
    {
        public List<Field2> field { get; set; }
    }

    public class Field3
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class ExtendedFields2
    {
        public List<Field3> field { get; set; }
    }

    public class AddonItem
    {
        public string item_code { get; set; }
        public string quantity { get; set; }
        public string description { get; set; }
        public string rate { get; set; }
        public string value { get; set; }
    }

    public class AddonItems
    {
        public List<AddonItem> addon_item { get; set; }
    }

    public class ComboItem
    {
        public string item_code { get; set; }
        public string quantity { get; set; }
        public string description { get; set; }
    }

    public class ComboItems
    {
        public List<ComboItem> combo_item { get; set; }
    }

    public class SplitItem
    {
        public string item_code { get; set; }
        public string quantity { get; set; }
        public string description { get; set; }
        public string rate { get; set; }
        public string value { get; set; }
    }

    public class SplitItems
    {
        public List<SplitItem> split_item { get; set; }
    }

    public class Attribute2
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class Attributes2
    {
        public List<Attribute2> attribute { get; set; }
    }

    public class LineItem
    {
        public string serial { get; set; }
        public string amount { get; set; }
        public string description { get; set; }
        public string item_code { get; set; }
        public string base_item_code { get; set; }
        public string discount_value { get; set; }
        public ExtendedFields2 extended_fields { get; set; }
        public string variant { get; set; }
        public AddonItems addon_items { get; set; }
        public ComboItems combo_items { get; set; }
        public SplitItems split_items { get; set; }
        public string qty { get; set; }
        public string rate { get; set; }
        public string value { get; set; }
        public Attributes2 attributes { get; set; }
        public string transaction_number { get; set; }
        public string notes { get; set; }
    }

    public class LineItems
    {
        public List<LineItem> line_item { get; set; }
    }

    public class AssociateDetails
    {
        public string code { get; set; }
        public string name { get; set; }
    }

    public class Transaction
    {
        public string bill_client_id { get; set; }
        public string type { get; set; }
        public string number { get; set; }
        public string amount { get; set; }
        public string currency_code { get; set; }
        public string entered_by { get; set; }
        public string notes { get; set; }
        public string billing_time { get; set; }
        public string gross_amount { get; set; }
        public string delivery_status { get; set; }
        public string shipping_source_till_code { get; set; }
        public string source { get; set; }
        public string outlier_status { get; set; }
        public string credit_notes { get; set; }
        public string discount { get; set; }
        public Customer customer { get; set; }
        public ExtendedFields extended_fields { get; set; }
        public PaymentDetails payment_details { get; set; }
        public CustomFields custom_fields { get; set; }
        public LineItems line_items { get; set; }
        public AssociateDetails associate_details { get; set; }
        public Redemptions redemptions { get; set; }
    }

    public class Root
    {
        public List<Transaction> transaction { get; set; }
    }

    public class TransactionRequest
    {
        public Root root { get; set; }
    }
}
