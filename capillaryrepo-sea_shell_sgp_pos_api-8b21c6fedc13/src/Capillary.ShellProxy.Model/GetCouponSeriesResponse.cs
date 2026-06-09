using System;
using System.Collections.Generic;
using System.Text;

namespace Capillary.ShellProxy.Model
{ 

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
   // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class ProductInfo    {
        public string productType { get; set; } 
        public List<int> productIds { get; set; } 
    }

    public class Entity    {
        public int id { get; set; } 
        public int orgId { get; set; } 
        public string description { get; set; } 
        public string discountCode { get; set; } 
        public string discountOn { get; set; } 
        public string discountType { get; set; } 
        public int discountValue { get; set; }
        public int discountUpto { get; set; } 
        public bool updateProductData { get; set; } 
    }

    public class GetCouponSeriesResponse    {
        public List<Entity> entity { get; set; } 
        //public List<object> warnings { get; set; } 
    }




}
