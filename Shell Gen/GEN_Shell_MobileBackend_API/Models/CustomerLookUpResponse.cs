using System;
using System.Collections.Generic;
using System.Text;

namespace GEN_Shell_MobileBackend_API.Models.CustomerLookUp
{

    public class Error
    {
        public bool status { get; set; }
        public string message { get; set; }
        public int code { get; set; }
    }
    public class Warnings
    {
        public bool status { get; set; }
        public string message { get; set; }
        public int code { get; set; }
    }

    public class LinkedPartnerProgram
    {
        public int partnerProgramId { get; set; }
        public string partnerProgramName { get; set; }
        public string partnerProgramDescription { get; set; }
        public string partnerProgramType { get; set; }
        public DateTime partnerProgramMembershipStartDate { get; set; }
        public DateTime partnerProgramMembershipEndDate { get; set; }
        public DateTime partnerProgramMembershipLastUpdatedDate { get; set; }
        public string partnerProgramMembershipLastUpdatedActivity { get; set; }
        public bool tierBased { get; set; }
    }


    public class CreatedBy
    {
        public int id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public string type { get; set; }
    }

    public class ModifiedBy
    {
        public int id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public string type { get; set; }
    }

    public class Attribution
    {
        public DateTime createDate { get; set; }
        public CreatedBy createdBy { get; set; }
        public ModifiedBy modifiedBy { get; set; }
        public DateTime modifiedDate { get; set; }
    }

    // public class Fields
    // {
    //     // public string vehicle_segment { get; set; }
    // }

    public class Identifier
    {
        public string type { get; set; }
        public string value { get; set; }
    }

    public class Meta
    {
        public bool residence { get; set; }
        public bool office { get; set; }
    }

    public class Attributes
    {
    }

    public class CommChannel
    {
        public string type { get; set; }
        public string value { get; set; }
        public bool primary { get; set; }
        public bool verified { get; set; }
        public Meta meta { get; set; }
        public Attributes attributes { get; set; }
    }

    public class Profile
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public Attribution attribution { get; set; }
        // public Fields fields { get; set; }
        public IDictionary<string, string> Fields { get; set; }
        public List<Identifier> identifiers { get; set; }
        public List<CommChannel> commChannels { get; set; }
        public string source { get; set; }
        public int userId { get; set; }
        public string accountId { get; set; }
        public List<object> conflictingProfileList { get; set; }
        public DateTime autoUpdateTime { get; set; }
    }

    public class CreatedBy2
    {
        public int id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public string type { get; set; }
    }

    public class ModifiedBy2
    {
        public int id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public string type { get; set; }
    }

    public class AttributionV2
    {
        public DateTime createDate { get; set; }
        public CreatedBy2 createdBy { get; set; }
        public ModifiedBy2 modifiedBy { get; set; }
        public DateTime modifiedDate { get; set; }
        public string createdFromSource { get; set; }
    }

    public class LoyaltyInfo
    {
        public string loyaltyType { get; set; }
        public AttributionV2 attributionV2 { get; set; }
        public double lifetimePurchases { get; set; }
    }

    public class Segments
    {
    }

    public class ExtendedFields
    {
        public string member_type { get; set; }
        public string deleted { get; set; }
    }
    public class FraudDetails
    {
        public DateTime modifiedOn { get; set; }
        public string status { get; set; }
    }

    public class PointsSummary
    {
        public double redeemed { get; set; }
        public double expired { get; set; }
        public double returned { get; set; }
        public double adjusted { get; set; }
        public double lifetimePoints { get; set; }
        public double loyaltyPoints { get; set; }
        public double cumulativePurchases { get; set; }
        public int loyaltyId { get; set; }
        public string currentSlab { get; set; }
        public string nextSlab { get; set; }
        public int nextSlabSerialNumber { get; set; }
        public string nextSlabDescription { get; set; }
        public int slabSNo { get; set; }
        public DateTime slabExpiryDate { get; set; }
        public int programId { get; set; }
        public double delayedPoints { get; set; }
        public double delayedReturnedPoints { get; set; }
        public double totalAvailablePoints { get; set; }
        public double totalReturnedPoints { get; set; }
        public List<LinkedPartnerProgram> linkedPartnerPrograms { get; set; }
        public string programTitle { get; set; }
        public string programDescription { get; set; }
        public double programPointsToCurrencyRatio { get; set; }
    }

    public class CardDetail
    {
        public int cardId { get; set; }
        public DateTime issuedDate { get; set; }
        public string createdDate { get; set; }
        public int expiryDays { get; set; }
        public string seriesName { get; set; }
        public int customerId { get; set; }
        public int maxActiveCards { get; set; }
        public string cardNumber { get; set; }
        public int seriesId { get; set; }
        public string seriesCode { get; set; }
        public int orgId { get; set; }
        public int entityId { get; set; }
        public StatusInfo statusInfo { get; set; }
        public int id { get; set; }
        public bool transactionNotAllowed { get; set; }
        public DateTime expiryDate { get; set; }
    }

    public class StatusInfo
    {
        public string reason { get; set; }
        public int createdBy { get; set; }
        public IList<object> actions { get; set; }
        public string autoUpdateTime { get; set; }
        public DateTime createdOn { get; set; }
        public int entityId { get; set; }
        public int id { get; set; }
        public bool isActive { get; set; }
        public int labelId { get; set; }
        public string label { get; set; }
        public string status { get; set; }
    }


    public class CustomerLookUpResponse
    {
        public string id { get; set; }
        public List<Profile> profiles { get; set; }
        public LoyaltyInfo loyaltyInfo { get; set; }
        public FraudDetails fraudDetails { get; set; }
        public Segments segments { get; set; }
        public PointsSummary pointsSummary { get; set; }
        public string associatedWith { get; set; }
        public ExtendedFields extendedFields { get; set; }
        public List<Warnings> warnings { get; set; }
        public List<CardDetail> cardDetails { get; set; }
        public int entity { get; set; }
        public string statusLabel { get; set; }
        public string statusLabelReason { get; set; }
        public List<Error> errors { get; set; }
        public SubscriptionInfo subscriptionInfo { get; set; }
    }
    public class SubscriptionInfo
    {
        public List<Subscription> subscriptions { get; set; }
        
    }
    public class Subscription
    {
        public string channel { get; set; }
        public string accountId { get; set; }
        public string priority { get; set; }
        public string type { get; set; }
        public string sourceName { get; set; }
    }
}
