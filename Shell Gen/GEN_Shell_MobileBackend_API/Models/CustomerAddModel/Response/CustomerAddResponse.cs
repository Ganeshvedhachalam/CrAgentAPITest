using System;
using System.Collections.Generic;

namespace Capillary.ShellProxy.Model.CustomerAddModel.Response
{


    public class Error
    {
        public bool status { get; set; }
        public string message { get; set; }
        public int code { get; set; }
    }

    public class CustomerAddResponse
    {
        public int createdId { get; set; }
        public List<object> warnings { get; set; }
        public List<object> sideEffects { get; set; }
        public List<Error> errors { get; set; }
    }


}