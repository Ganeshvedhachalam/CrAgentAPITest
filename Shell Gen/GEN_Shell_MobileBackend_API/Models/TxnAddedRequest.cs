using System;
using System.Collections.Generic;

namespace GEN_Shell_MobileBackend_API.Models
{
    public class EnteredBy
    {
        public int Id { get; set; }
        public Till Till { get; set; }
        public Store Store { get; set; }
    }

    public class Till
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }

    public class Store
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string ExternalId { get; set; }
        public string ExternalId1 { get; set; }
        public string ExternalId2 { get; set; }
    }

    public class Instore
    {
        public string Mobile { get; set; }
    }

    public class CustomerIdentifiers
    {
        public int CustomerId { get; set; }
        public Instore Instore { get; set; }
    }

    public class Data
    {
        public double Amount { get; set; }
        public string BillClientId { get; set; }
        public string BillNumber { get; set; }
        public long EnteredAt { get; set; }
        public int TransactionId { get; set; }
        public string DeliveryStatus { get; set; }
        public string BillType { get; set; }
        public int LineItemCount { get; set; }
        public double Discount { get; set; }
        public double GrossAmount { get; set; }
        public string CurrencyCode { get; set; }
        public EnteredBy EnteredBy { get; set; }
        public CustomerIdentifiers CustomerIdentifiers { get; set; }
    }

    public class TxnAddedRequest
    {
        public string EventName { get; set; }
        public string EventId { get; set; }
        public int OrgId { get; set; }
        public string RefId { get; set; }
        public string ApiRequestId { get; set; }
        public long CreatedAt { get; set; }
        public Data Data { get; set; }
        public object LoyaltyEventId { get; set; }
    }
}
