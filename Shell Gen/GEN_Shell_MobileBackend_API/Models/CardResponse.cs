using System;
using System.Collections.Generic;
using System.Text;

namespace GEN_Shell_MobileBackend_API.Models
{
    public class StatusInfo
    {
        public string reason { get; set; }
        public int createdBy { get; set; }
        public List<object> actions { get; set; }
        public string autoUpdateTime { get; set; }
        public DateTime createdOn { get; set; }
        public int entityId { get; set; }
        public int id { get; set; }
        public bool isActive { get; set; }
        public int labelId { get; set; }
        public string label { get; set; }
        public string status { get; set; }
    }
    public class Error
    {
        public bool status { get; set; }
        public string message { get; set; }
        public int code { get; set; }
    }

    public class CardResponse
    {
        public int cardId { get; set; }
        public DateTime issuedDate { get; set; }
        public string createdDate { get; set; }
        public int expiryDays { get; set; }
        public string seriesName { get; set; }
        public int customerId { get; set; }
        public int maxActiveCards { get; set; }
        public string entityCode { get; set; }
        public string cardNumber { get; set; }
        public int seriesId { get; set; }
        public string seriesCode { get; set; }
        public int orgId { get; set; }
        public int entityId { get; set; }
        public StatusInfo statusInfo { get; set; }
        public int id { get; set; }
        public bool transactionNotAllowed { get; set; }
        public DateTime expiryDate { get; set; }
        public List<object> warnings { get; set; }
        public List<Error> errors { get; set; }
        public CustomFields customFields { get; set; }
    }
    public class CustomFields
    {
        public string bl_token { get; set; }
    }
}
