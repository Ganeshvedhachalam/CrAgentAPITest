using System;
using System.Collections.Generic;
using System.Text;

namespace GEN_Shell_MobileBackend_API.Models
{
    public class OnBoardingHashRsaResp
    {
        public string partnerid { get; set; }
        public string encryptedsignature { get; set; }
        public string inputString { get; set; }
    }
}
