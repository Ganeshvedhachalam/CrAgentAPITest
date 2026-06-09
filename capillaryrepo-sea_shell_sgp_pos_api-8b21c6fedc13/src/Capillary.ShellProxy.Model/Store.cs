using System.Collections.Generic;

namespace Capillary.ShellProxy.Model
{
    public class Status    {
        public string success { get; set; } 
        public int code { get; set; } 
        public string message { get; set; } 
    }

    public class Field    {
        public string name { get; set; } 
        public string value { get; set; } 
    }

    public class CustomFields    {
        public List<Field> field { get; set; } 
    }

    public class Coordinates    {
        public string latitude { get; set; } 
        public string longitude { get; set; } 
    }

    public class Location    {
        public string country { get; set; } 
        public object state { get; set; } 
        public object city { get; set; } 
        public object area { get; set; } 
        public Coordinates coordinates { get; set; } 
    }

    public class BaseCurrency    {
        public object label { get; set; } 
        public object symbol { get; set; } 
    }

    public class Currencies    {
        public BaseCurrency base_currency { get; set; } 
    }

    public class BaseLanguage    {
        public object lang { get; set; } 
        public object locale { get; set; } 
    }

    public class Languages    {
        public BaseLanguage base_language { get; set; } 
    }

    public class BaseTimezone    {
        public string label { get; set; } 
        public string offset { get; set; } 
    }

    public class Timezones    {
        public BaseTimezone base_timezone { get; set; } 
    }

    public class IsoCode    {
        public object alpha { get; set; } 
        public object numeric { get; set; } 
    }

    public class Currency    {
        public object name { get; set; } 
        public object symbol { get; set; } 
        public IsoCode iso_code { get; set; } 
    }

    public class Offset    {
        public string std { get; set; } 
        public object summer { get; set; } 
    }

    public class TimeZone    {
        public string coordinates { get; set; } 
        public Offset offset { get; set; } 
    }

    public class BaseCountry    {
        public string name { get; set; } 
        public string code { get; set; } 
    }

    public class Countries    {
        public BaseCountry base_country { get; set; } 
    }

    public class Sms    {
        public string name { get; set; } 
        public string mobile { get; set; } 
        public string email { get; set; } 
    }

    public class Email    {
        public string name { get; set; } 
        public string mobile { get; set; } 
        public string email { get; set; } 
    }

    public class Template    {
        public Sms sms { get; set; } 
        public Email email { get; set; } 
    }

    public class ItemStatus    {
        public string success { get; set; } 
        public int code { get; set; } 
        public string message { get; set; } 
    }

    public class Store    {
        public string id { get; set; } 
        public string name { get; set; } 
        public string code { get; set; } 
        public string mobile { get; set; } 
        public string email { get; set; } 
        public string land_line { get; set; } 
        public string external_id { get; set; } 
        public string external_id_1 { get; set; } 
        public string external_id_2 { get; set; } 
        public CustomFields custom_fields { get; set; } 
        public Location location { get; set; } 
        public Currencies currencies { get; set; } 
        public Languages languages { get; set; } 
        public Timezones timezones { get; set; } 
        public Currency currency { get; set; } 
        public object language { get; set; } 
        public TimeZone time_zone { get; set; } 
        public Countries countries { get; set; } 
        public object state_name { get; set; } 
        public object city_name { get; set; } 
        public object area_name { get; set; } 
        public Template template { get; set; } 
        public ItemStatus item_status { get; set; } 
    }

    public class Stores    {
        public List<Store> store { get; set; } 
    }

    public class Response    {
        public Status status { get; set; } 
        public Stores stores { get; set; } 
    }

    public class StoreDetailsResponse    {
        public Response response { get; set; } 
    }


}