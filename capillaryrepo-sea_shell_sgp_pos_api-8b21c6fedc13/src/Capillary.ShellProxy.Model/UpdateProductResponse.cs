using System;
using System.Collections.Generic;
using System.Text;

namespace Capillary.ShellProxy.Model
{
     public class ResponseData    {
        public string actionCode { get; set; } 
        public string actionCodeDescription { get; set; } 
     }

    public class UpdateProductResponse    {
        public ResponseData responseData { get; set; } 
    }
}
