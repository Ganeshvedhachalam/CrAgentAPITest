using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GEN_Shell_MobileBackend_API.Models.StoreDetails
{
    public class GetStoreDetailsResp
    {
        public Response response { get; set; }
    }

    public class Response
    {
        public Status status { get; set; }
        public Stores stores { get; set; }
    }

    public class Status
    {
        public string success { get; set; }
        public int code { get; set; }
        public string message { get; set; }
    }

    public class Stores
    {
        public Store[] store { get; set; }
    }

    public class Store
    {
        public string external_id { get; set; }
        public string code { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string mobile { get; set; }
        public string email { get; set; }
        public string land_line { get; set; }
        public string external_id_1 { get; set; }
        public string external_id_2 { get; set; }
        public Custom_Fields custom_fields { get; set; }
        
        //public Location location { get; set; }
        //public Currencies currencies { get; set; }
        //public Languages languages { get; set; }
        //public Timezones timezones { get; set; }
        //public Currency currency { get; set; }
        //public object language { get; set; }
        //public Time_Zone time_zone { get; set; }
        //public Countries countries { get; set; }
        //public object state_name { get; set; }
        //public object city_name { get; set; }
        //public object area_name { get; set; }
        //public Template template { get; set; }
        //public Item_Status item_status { get; set; }
    }

    public class Custom_Fields
    {  
        public List<Field> field { get; set; }
        //public IDictionary<string, string> Field { get; set; }
    }

    public class Field
    {  
        public string name { get; set; }
        public object value { get; set; }
    }

    public class Location
    {
        public string country { get; set; }
        public object state { get; set; }
        public object city { get; set; }
        public object area { get; set; }
        public Coordinates coordinates { get; set; }
    }

    public class Coordinates
    {
        public string latitude { get; set; }
        public string longitude { get; set; }
    }

    public class Currencies
    {
        public Base_Currency base_currency { get; set; }
    }

    public class Base_Currency
    {
        public object label { get; set; }
        public object symbol { get; set; }
    }

    public class Languages
    {
        public Base_Language base_language { get; set; }
    }

    public class Base_Language
    {
        public object lang { get; set; }
        public object locale { get; set; }
    }

    public class Timezones
    {
        public Base_Timezone base_timezone { get; set; }
    }

    public class Base_Timezone
    {
        public string label { get; set; }
        public string offset { get; set; }
    }

    public class Currency
    {
        public object name { get; set; }
        public object symbol { get; set; }
        public Iso_Code iso_code { get; set; }
    }

    public class Iso_Code
    {
        public object alpha { get; set; }
        public object numeric { get; set; }
    }

    public class Time_Zone
    {
        public string coordinates { get; set; }
        public Offset offset { get; set; }
    }

    public class Offset
    {
        public string std { get; set; }
        public object summer { get; set; }
    }

    public class Countries
    {
        public Base_Country base_country { get; set; }
    }

    public class Base_Country
    {
        public string name { get; set; }
        public string code { get; set; }
    }

    public class Template
    {
        public Sms sms { get; set; }
        //public Email email { get; set; }
    }

    public class Sms
    {
        public string name { get; set; }
        public string mobile { get; set; }
        public string email { get; set; }
    }

    //public class Email
    //{
    //    public string name { get; set; }
    //    public string mobile { get; set; }
    //    public string email { get; set; }
    //}

    public class Item_Status
    {
        public string success { get; set; }
        public int code { get; set; }
        public string message { get; set; }
    }

}
