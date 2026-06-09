using System;
using System.Collections.Generic;

namespace GEN_Shell_MobileBackend_API.Models
{
    public class ValueObject
    {
        public object value { get; set; }
        public int type { get; set; }
        public List<Values> values { get; set; }
    }
    public class Values
    {
        public object value { get; set; }
        public int type { get; set; }
    }

    public class Value
    {
        public string key { get; set; }
        public ValueObject valueObject { get; set; }
        public int type { get; set; }
        public string dateStarted { get; set; }
        public string dateCompleted { get; set; }
        public string questionId { get; set; }
    }

    public class SmgSurveyRequest
    {
        public string subscriptionKey { get; set; }
        public int surveyId { get; set; }
        public int surveyRevisionId { get; set; }
        public string locationId { get; set; }
        public DateTime dateStarted { get; set; }
        public DateTime dateCompleted { get; set; }
        public string languageIsoCode { get; set; }
        public int userTimezoneOffsetMinutes { get; set; }
        public List<Value> values { get; set; }
    }
}