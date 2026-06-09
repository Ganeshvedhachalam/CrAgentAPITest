using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GEN_Shell_MobileBackend_API.Models.TargetDetails
{
    public class GetTargetDetailsResponse
    {
        public Data data { get; set; }
        public object errors { get; set; }
        public object warnings { get; set; }
    }


    public class Data
    {
        public int userId { get; set; }
        //public Customer customer { get; set; }
        public List<Targetgroup> targetGroups { get; set; } = new List<Targetgroup> { };
    }

    public class Customer
    {
        public int id { get; set; }
        public Profile[] profiles { get; set; }
        //public Loyaltyinfo loyaltyInfo { get; set; }
        //public Customfields customFields { get; set; }
    }

    public class Loyaltyinfo
    {
        public Attribution attribution { get; set; }
        public string loyaltyType { get; set; }
        public int lifetimePurchases { get; set; }
    }

    public class Attribution
    {
        public DateTime createdOn { get; set; }
        public DateTime lastUpdatedOn { get; set; }
        public Lastupdatedby lastUpdatedBy { get; set; }
        public Createdby createdBy { get; set; }
    }

    public class Lastupdatedby
    {
        public int id { get; set; }
        public object code { get; set; }
        public object description { get; set; }
        public object name { get; set; }
        public object type { get; set; }
    }

    public class Createdby
    {
        public int id { get; set; }
        public object code { get; set; }
        public object description { get; set; }
        public object name { get; set; }
        public object type { get; set; }
    }

    public class Customfields
    {
        public string app_privacy_policy { get; set; }
        public string ota_tutorial_version { get; set; }
        public string goplus_tnc { get; set; }
        public string onboarding { get; set; }
        public string vehicle_type_date { get; set; }
        public string ota_tutorial_time { get; set; }
    }

    public class Profile
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public Commchannel[] commChannels { get; set; }
        public string source { get; set; }
        public string accountId { get; set; }
        public int userId { get; set; }
        public int orgSourceId { get; set; }
    }

    public class Commchannel
    {
        public string type { get; set; }
        public object accountId { get; set; }
        public string value { get; set; }
        public bool primary { get; set; }
        public bool verified { get; set; }
        public object subscribed { get; set; }
        public object attributes { get; set; }
    }

    public class Targetgroup
    {
        public int id { get; set; }
        public Attribution1 attribution { get; set; }
        public string name { get; set; }
        public int preferredTillId { get; set; }
        public Target[] targets { get; set; }
        public string targetEvaluationType { get; set; }
    }

    public class Attribution1
    {
        public DateTime createdOn { get; set; }
        public DateTime lastUpdatedOn { get; set; }
        public Lastupdatedby1 lastUpdatedBy { get; set; }
        public Createdby1 createdBy { get; set; }
    }

    public class Lastupdatedby1
    {
        public int id { get; set; }
        public string code { get; set; }
        public string description { get; set; }
        public string name { get; set; }
        public string type { get; set; }
    }

    public class Createdby1
    {
        public int id { get; set; }
        public string code { get; set; }
        public string description { get; set; }
        public string name { get; set; }
        public string type { get; set; }
    }

    public class Target
    {
        //public int? targetId { get; set; }
        public string targetId { get; set; }
        public int periodId { get; set; }
        public string periodRefCode { get; set; }
        public string periodStartDate { get; set; }
        public string periodEndDate { get; set; }
        public float targetValue { get; set; }
        public float targetAchievedValue { get; set; }
        public string targetName { get; set; }
        public string targetType { get; set; }
        public string targetEntity { get; set; }
        public int targetRuleId { get; set; }
        public bool currentPeriod { get; set; }
        public object milestones { get; set; }
    }

}
