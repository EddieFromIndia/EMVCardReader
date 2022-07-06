using Newtonsoft.Json;
using System.Collections.Generic;

namespace EMVCardReader.Models
{
    public class JsonData
    {
        [JsonProperty("cold atr", NullValueHandling = NullValueHandling.Ignore)]
        public string ColdATR { get; set; }

        [JsonProperty("aids", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> AIDs { get; set; } = new List<string>();

        [JsonProperty("tlv", NullValueHandling = NullValueHandling.Ignore)]
        public List<object> TLV { get; set; } = new List<object>();

        [JsonProperty("cplc", NullValueHandling = NullValueHandling.Ignore)]
        public string CPLC { get; set; }
    }
}
