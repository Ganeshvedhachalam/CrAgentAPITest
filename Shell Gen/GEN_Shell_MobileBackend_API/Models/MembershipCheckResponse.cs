using System;
using System.Collections.Generic;
using System.Text;

namespace GEN_Shell_MobileBackend_API.Models
{
    public class MembershipCheckResponse
    {
        public List<Record> Records { get; set; }
        public ResultInfo ResultInfo { get; set; }
    }
    public class Record
    {
        public string CardNumber { get; set; }
        public string CardType { get; set; }
        public string MemberFullName { get; set; }
        public string TotalPoints { get; set; }
        public string AvailablePoints { get; set; }
        public bool SecuredAccount { get; set; }
    }

    public class ResultInfo
    {
        public bool Success { get; set; }
        public List<object> ErrorCodes { get; set; }
        public List<object> ErrorMessages { get; set; }
    }

}
