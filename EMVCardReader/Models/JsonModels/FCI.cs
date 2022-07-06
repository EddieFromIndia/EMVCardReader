using Newtonsoft.Json;

namespace EMVCardReader.Models
{
    public class FCI
    {
        [JsonProperty("Dedicated File (DF) Name", NullValueHandling = NullValueHandling.Ignore)]
        public string DF { get; set; }

        [JsonProperty("Issuer Country Code (alpha2 format)", NullValueHandling = NullValueHandling.Ignore)]
        public string IssuerCountryCode2 { get; set; }

        [JsonProperty("Issuer Country Code (alpha3 format)", NullValueHandling = NullValueHandling.Ignore)]
        public string IssuerCountryCode3 { get; set; }

        [JsonProperty("Application Priority Indicator", NullValueHandling = NullValueHandling.Ignore)]
        public string ApplicationPriorityIndicator { get; set; }

        [JsonProperty("Issuer Identification Number", NullValueHandling = NullValueHandling.Ignore)]
        public string IssuerIdentificationNumber { get; set; }

        [JsonProperty("Application Label", NullValueHandling = NullValueHandling.Ignore)]
        public string ApplicationLabel { get; set; }

        [JsonProperty("Short File Identifier", NullValueHandling = NullValueHandling.Ignore)]
        public string SFI { get; set; }

        [JsonProperty("Language Preference", NullValueHandling = NullValueHandling.Ignore)]
        public string LanguagePreference { get; set; }
    }
}
