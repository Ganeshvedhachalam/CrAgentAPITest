using System;
using System.Collections.Generic;
using System.Text;

namespace GEN_Shell_MobileBackend_API.Models
{
    public class VocSurveyResponse{
        public StatusInformation status { get; set; }

    }
    public class StatusInformation{
        public bool success { get; set; }
        public int code { get; set; }
        public string message { get; set; }
        public string total { get; set; }
        public string success_count { get; set; }
        public string requestId { get; set; }
    

    }
}