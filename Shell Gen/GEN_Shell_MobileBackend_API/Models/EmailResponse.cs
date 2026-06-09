using System;
using System.Collections.Generic;
using System.Text;

namespace GEN_Shell_MobileBackend_API.Models.EmailCommResponse
{
    public class Email
    {
        public object id { get; set; }
        public string to { get; set; }
        public string cc { get; set; }
        public string bcc { get; set; }
        public string from { get; set; }
        public object status { get; set; }
        public string subject { get; set; }
        public string description { get; set; }
        public string scheduled_time { get; set; }
        public ItemStatus item_status { get; set; }
    }

    public class ItemStatus
    {
        public bool status { get; set; }
        public int code { get; set; }
        public string message { get; set; }
    }

    public class Response
    {
        public Status status { get; set; }
        public List<Email> email { get; set; }
    }

    public class EmailResponse
    {
        public Response response { get; set; }
    }

    public class Status
    {
        public bool success { get; set; }
        public int code { get; set; }
        public string message { get; set; }
        public string total { get; set; }
        public string success_count { get; set; }
    }



    //public class EmailResponse
    //{
    //    public Response response { get; set; }
    //}
    //// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    //public class Email
    //{
    //    public long id { get; set; }
    //    public string to { get; set; }
    //    public string cc { get; set; }
    //    public string bcc { get; set; }
    //    public string from { get; set; }
    //    public string status { get; set; }
    //    public string subject { get; set; }
    //    public string description { get; set; }
    //    public string scheduled_time { get; set; }
    //    public ItemStatus item_status { get; set; }
    //}

    //public class ItemStatus
    //{
    //    public bool status { get; set; }
    //    public int code { get; set; }
    //    public string message { get; set; }
    //}

    //public class Response
    //{
    //    public Status status { get; set; }
    //    public List<Email> email { get; set; }
    //}



    //public class Status
    //{
    //    public bool success { get; set; }
    //    public int code { get; set; }
    //    public string message { get; set; }
    //    public string total { get; set; }
    //    public string success_count { get; set; }
    //}

}
