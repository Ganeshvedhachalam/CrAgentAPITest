using System;
using System.Collections.Generic;
using System.Text;

namespace GEN_Shell_MobileBackend_API.Models
{
    public class OnboardingHashReuest
    {
        public string uid { get; set; }
        public string mobileno { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string identifier { get; set; }
        public int source { get; set; }
        public string membercardno { get; set; }
    }
}
