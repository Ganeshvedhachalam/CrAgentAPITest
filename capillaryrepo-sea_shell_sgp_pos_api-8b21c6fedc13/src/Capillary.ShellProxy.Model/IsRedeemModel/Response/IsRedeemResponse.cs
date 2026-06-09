using System;

namespace Capillary.ShellProxy.Model.IsRedeemModel.Response
{
    public class Status
    {
        public string success { get; set; }
        public string code { get; set; }
        public string message { get; set; }
    }

    public class ItemStatus
    {
        public string status { get; set; }
        public string code { get; set; }
        public string message { get; set; }
    }

    public class SeriesInfo
    {
        public string description { get; set; }
        public string discount_code { get; set; }
        public string valid_till { get; set; }
        public string discount_type { get; set; }
        public string discount_value { get; set; }
        public string detailed_info { get; set; }
    }

    public class Redeemable
    {
        public string mobile { get; set; }
        public string code { get; set; }
        public string is_redeemable { get; set; }
        public ItemStatus item_status { get; set; }
        public SeriesInfo series_info { get; set; }
    }

    public class Coupons
    {
        public Redeemable redeemable { get; set; }
    }

    public class Response
    {
        public Status status { get; set; }
        public Coupons coupons { get; set; }
    }

    public class IsRedeemResponse
    {
        public Response response { get; set; }
    }


}
