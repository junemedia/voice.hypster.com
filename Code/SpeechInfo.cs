using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace hypster_voice.Code
{
    public class SpeechInfo
    {
        public string version { get; set; }
        public string actionType { get; set; }
        public Dictionary<string, string> metrics { get; set; }
        public Dictionary<string, string> interpretation { get; set; }
        public string recognized { get; set; }
        public SpeechSearch search { get; set; }
    }
}