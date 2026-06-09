using System;
using System.Collections.Generic;

namespace Capillary.ShellProxy.Model.CustomerAddModel.Request
{

    public class Identifier
    {
        public string type { get; set; }
        public string value { get; set; }
    }

    public class LoyaltyInfo
    {
        public string loyaltyType { get; set; }
    }

    public class Profile
    {
        public List<Identifier> identifiers { get; set; }
        public string source { get; set; }
        public string accountId { get; set; }
        public IDictionary<string, string> fields { get; set; }
    }

    public class CustomerAddRequest
    {
        public LoyaltyInfo loyaltyInfo { get; set; }
        public List<Profile> profiles { get; set; }
    }
}