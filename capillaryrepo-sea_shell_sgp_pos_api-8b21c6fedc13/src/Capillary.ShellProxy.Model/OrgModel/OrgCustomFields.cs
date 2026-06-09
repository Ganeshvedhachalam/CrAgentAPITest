using System.Collections.Generic;

namespace Capillary.ShellProxy.Model.OrgModel
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Status    {
        public string success { get; set; } 
        public int code { get; set; } 
        public string message { get; set; } 
    }

    public class Field    {
        public string name { get; set; } 
        public string label { get; set; } 
        public string type { get; set; } 
        public string datatype { get; set; } 
        //public string default { get; set; } 
        public string phase { get; set; } 
        public string position { get; set; } 
        public string rule { get; set; } 
        public string regex { get; set; } 
        public string error { get; set; } 
        public string options { get; set; } 
        public string scope { get; set; } 
        public string is_mandatory { get; set; } 
        public string is_updatable { get; set; } 
        public string is_disabled { get; set; } 
        public string disabled_at_server { get; set; } 
    }

    public class CustomField    {
        public List<Field> field { get; set; } 
    }

    public class Organization    {
        public List<CustomField> custom_fields { get; set; } 
    }

    public class Response    {
        public Status status { get; set; } 
        public Organization organization { get; set; } 
    }

    public class OrgCustomFieldsResponse    {
        public Response response { get; set; } 
    }



}