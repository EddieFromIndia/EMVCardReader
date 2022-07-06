using Newtonsoft.Json;

namespace EMVCardReader.Models
{
    public class Track2E
    {
        [JsonProperty("PAN", NullValueHandling = NullValueHandling.Ignore)]
        public string PAN { get; set; }

        [JsonProperty("Expiration Data", NullValueHandling = NullValueHandling.Ignore)]
        public string ExpirationData { get; set; }

        [JsonProperty("Service Code", NullValueHandling = NullValueHandling.Ignore)]
        public string ServiceCode { get; set; }

        [JsonProperty("Discretionary Data", NullValueHandling = NullValueHandling.Ignore)]
        public string DiscretionaryData { get; set; }

        [JsonProperty("Cheсk Digit", NullValueHandling = NullValueHandling.Ignore)]
        public string CheckDigit { get; set; }

        [JsonProperty("Account Number", NullValueHandling = NullValueHandling.Ignore)]
        public string AccountNumber { get; set; }

        [JsonProperty("Major Industry Identifier", NullValueHandling = NullValueHandling.Ignore)]
        public string MajorIndustryIdentifier { get; set; }

        [JsonProperty("Issuer Identifier Number", NullValueHandling = NullValueHandling.Ignore)]
        public string IssuerIdentifierNumber { get; set; }
    }
}
