using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GEN_Shell_MobileBackend_API.Models
{
    public class DecryptResponse
    {
        public string DecryptedData { get; set; }        
        public int Code { get; set; }
        public string Message { get; set; }
    }
}
