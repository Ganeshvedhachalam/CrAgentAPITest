using System;
using System.Collections.Generic;
using System.Text;

namespace GEN_Shell_MobileBackend_API.Models
{
    public class SmgSurveyResponse{
        public string surveyResponseId { get; set; }
        public Result results { get; set; }

    }
    public class Result{
        public string subscriptionKeyNotFound { get; set; }
        public string projectIdNotFound { get; set; }
        public string locationIdNotFound { get; set; }

    }
}