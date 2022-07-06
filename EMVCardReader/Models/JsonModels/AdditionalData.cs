using Newtonsoft.Json;

namespace EMVCardReader.Models
{
    public class AdditionalData
    {
        [JsonProperty("Application Transaction Counter", NullValueHandling = NullValueHandling.Ignore)]
        public string ApplicationTransactionCounter { get; set; }

        [JsonProperty("Last Online ATC Register", NullValueHandling = NullValueHandling.Ignore)]
        public string LastOnlineATCRegister { get; set; }

        [JsonProperty("Pin Try Counter", NullValueHandling = NullValueHandling.Ignore)]
        public string PinTryCounter { get; set; }

        [JsonProperty("Log Entry", NullValueHandling = NullValueHandling.Ignore)]
        public string LogEntry { get; set; }

        [JsonProperty("Log Format", NullValueHandling = NullValueHandling.Ignore)]
        public string LogFormat { get; set; }
    }
}
