using System;

namespace Capillary.ShellProxy.Model
{
    public class CancelTransactionResponse
    {
        // public bool data { get; set; }
        public Data data { get; set; }
        public Errors[] errors { get; set; }
    }

    public class Errors
    {
        public int code { get; set; }
        public string message { get; set; }
    }
    public class Data
    {
        public bool status { get; set; }
        public int code { get; set; }
        public string message { get; set; }
    }
}
