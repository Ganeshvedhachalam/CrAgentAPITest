using System;
using System.Collections.Generic;
using System.Text;

namespace GEN_Shell_MobileBackend_API.Models
{

    public class EmailRequest
    {
        public EmailRequest()
        {
            root = new Root();
        }
        public Root root { get; set; }
    }
    public class Attachment
    {
        public string file_name { get; set; }
        public string file_type { get; set; }
        public string file_data { get; set; }
        public string file_encoding_type { get; set; }
    }

    public class Attachments
    {
        public List<Attachment> attachment { get; set; }
    }

    public class Email
    {
        public string to { get; set; }
        public string cc { get; set; }
        public string from { get; set; }
        public string subject { get; set; }
        public string body { get; set; }
        public Attachments attachments { get; set; }
    }

    public class Root
    {
        public Root()
        {
            email = new List<Email>();
        }
        public List<Email> email { get; set; }
    }

}
