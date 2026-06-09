using System;
using System.Collections.Generic;
using System.Text;

namespace GEN_Shell_MobileBackend_API.Models
{
    public class Artifact
    {
        public string source { get; set; }
        public List<Key> Keys { get; set; }
    }

    public class Key
    {
        public string key { get; set; }
        public string value { get; set; }
    }

    public class DBResponseModel
    {
        public List<Artifact> artifacts { get; set; }
    }


}
