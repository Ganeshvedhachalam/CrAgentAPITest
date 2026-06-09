using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GEN_Shell_MobileBackend_API.Models.MileStoneOffersResp
{
    public class MileStoneOffersErrorResp
    {
        public bool status { get; set; }
        public int errorCode { get; set; }
        public string errorDescription { get; set; }
    }

}
