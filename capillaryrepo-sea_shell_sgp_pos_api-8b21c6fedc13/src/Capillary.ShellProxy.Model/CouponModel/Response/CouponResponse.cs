using System;
using System.Collections.Generic;
using System.Text;

namespace Capillary.ShellProxy.Model.CouponModel.Response
{
    public class StatusCode    {
        public string message { get; set; } 
        public int code { get; set; } 
        public bool status { get; set; } 
    }

    public class RedemptionStatus    {
        public StatusCode statusCode { get; set; } 
        public List<object> warnings { get; set; } 
        public string message { get; set; } 
        public int code { get; set; } 
        public List<object> warningsAsStatusCode { get; set; } 
        public bool success { get; set; } 
    }

    public class Result    {
        public int id { get; set; } 
        public List<object> warnings { get; set; } 
        public string appendedErrorMessage { get; set; } 
        public string code { get; set; } 
        public string discountCode { get; set; } 
        public int seriesCode { get; set; } 
        public bool isAbsolute { get; set; } 
        public double couponValue { get; set; } 
        public RedemptionStatus redemptionStatus { get; set; } 
        public string discountType { get; set; } 
        public int discountValue { get; set; } 
        public int discountUpto { get; set; } 
    }

    public class Response    {
        public int entityId { get; set; } 
        public Result result { get; set; } 
        public List<Error> errors { get; set; } 
        public List<Warnings> warnings { get; set; } 
    }
    public class Error    {
        public bool status { get; set; } 
        public int code { get; set; } 
        public string message { get; set; } 
    }

    public class Warnings    {
        public bool status { get; set; } 
        public int code { get; set; } 
        public string message { get; set; } 
    }

    public class CouponResponse    {
        public List<Response> response { get; set; } 
        public int totalCount { get; set; } 
        public int failureCount { get; set; } 
        public List<object> warnings { get; set; } 
        public List<Error> errors { get; set; } 
    }



}
