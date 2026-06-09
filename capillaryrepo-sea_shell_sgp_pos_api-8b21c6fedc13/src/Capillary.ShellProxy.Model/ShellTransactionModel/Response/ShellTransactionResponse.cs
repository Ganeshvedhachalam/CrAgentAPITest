using System;
using System.Collections.Generic;
using System.Text;

namespace Capillary.ShellProxy.Model.ShellTransactionModel.Response
{
    public class ResponseData
    {
        public string requestType { get; set; }
    }

    public class RequestData
    {
        public string requestID { get; set; }
        public string overallResult { get; set; }
    }

    public class ShellTransactionResponse
    {
        public ResponseData responseData { get; set; }
        public RequestData requestData { get; set; }
    }

    public class LstShellResponse
    {
        public List<ShellTransactionResponse> ShellResponse { get; set; }
    }
}
