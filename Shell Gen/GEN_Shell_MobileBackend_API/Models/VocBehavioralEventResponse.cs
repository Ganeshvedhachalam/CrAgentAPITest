using System;
using System.Collections.Generic;

namespace GEN_Shell_MobileBackend_API.Models
{
    

    public class ApiStatus
{
    public int code { get; set; }
    public string message { get; set; }
}

public class VocBehavioralEventResponse
{
    public ApiStatus status { get; set; }
    public string requestId { get; set; }
}
}