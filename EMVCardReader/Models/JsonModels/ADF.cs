using Newtonsoft.Json;

namespace EMVCardReader.Models
{
    public class ADF
    {
        [JsonProperty("Track 2 Equivalent Data", NullValueHandling = NullValueHandling.Ignore)]
        public Track2E Track2E { get; set; }

        [JsonProperty("Cardholder Name", NullValueHandling = NullValueHandling.Ignore)]
        public string CardholderName { get; set; }

        [JsonProperty("Track 1 Discretionary Data", NullValueHandling = NullValueHandling.Ignore)]
        public string Track1D { get; set; }

        [JsonProperty("Track 2 Discretionary Data", NullValueHandling = NullValueHandling.Ignore)]
        public string Track2D { get; set; }

        [JsonProperty("Issuer Action Code - Default", NullValueHandling = NullValueHandling.Ignore)]
        public string IssuerActionCodeDefault { get; set; }

        [JsonProperty("Issuer Action Code - Denial", NullValueHandling = NullValueHandling.Ignore)]
        public string IssuerActionCodeDenial { get; set; }

        [JsonProperty("Issuer Action Code - Online", NullValueHandling = NullValueHandling.Ignore)]
        public string IssuerActionCodeOnline { get; set; }

        [JsonProperty("Application Expiration Date", NullValueHandling = NullValueHandling.Ignore)]
        public string ApplicationExpirationDate { get; set; }

        [JsonProperty("Card Risk Management Data Object List 1 (CDOL1)", NullValueHandling = NullValueHandling.Ignore)]
        public CDOL CDOL1 { get; set; }

        [JsonProperty("Card Risk Management Data Object List 2 (CDOL2)", NullValueHandling = NullValueHandling.Ignore)]
        public CDOL CDOL2 { get; set; }

        [JsonProperty("Application Primary Account Number (PAN)", NullValueHandling = NullValueHandling.Ignore)]
        public string ApplicationPAN { get; set; }

        [JsonProperty("Application Primary Account Number (PAN) Sequence Number", NullValueHandling = NullValueHandling.Ignore)]
        public string ApplicationPANSN { get; set; }

        [JsonProperty("Application Version Number", NullValueHandling = NullValueHandling.Ignore)]
        public string ApplicationVersionNumber { get; set; }

        [JsonProperty("Issuer Country Code", NullValueHandling = NullValueHandling.Ignore)]
        public string IssuerCountryCode { get; set; }

        [JsonProperty("Cardholder Verification Method (CVM) List", NullValueHandling = NullValueHandling.Ignore)]
        public string CVMList { get; set; }

        [JsonProperty("Application Usage Control", NullValueHandling = NullValueHandling.Ignore)]
        public string ApplicationUsageControl { get; set; }
    }
}
