using Newtonsoft.Json;

namespace EMVCardReader.Models
{
    public class CDOL
    {
        [JsonProperty("Transaction Date", NullValueHandling = NullValueHandling.Ignore)]
        public string TransactionDate { get; set; }

        [JsonProperty("Unpredictable Number", NullValueHandling = NullValueHandling.Ignore)]
        public string UnpredictableNumber { get; set; }

        [JsonProperty("Terminal Country Code", NullValueHandling = NullValueHandling.Ignore)]
        public string TerminalCountryCode { get; set; }

        [JsonProperty("Transaction Country Code", NullValueHandling = NullValueHandling.Ignore)]
        public string TransactionCountryCode { get; set; }

        [JsonProperty("Transaction Type", NullValueHandling = NullValueHandling.Ignore)]
        public string TransactionType { get; set; }

        [JsonProperty("Amount, Authorised", NullValueHandling = NullValueHandling.Ignore)]
        public string AmountAuthorised { get; set; }

        [JsonProperty("Amount, Other (Numeric)", NullValueHandling = NullValueHandling.Ignore)]
        public string AmountOther { get; set; }

        [JsonProperty("Terminal Verification Results", NullValueHandling = NullValueHandling.Ignore)]
        public string TerminalVerificationResults { get; set; }

        [JsonProperty("Authorization Response Code", NullValueHandling = NullValueHandling.Ignore)]
        public string AuthorizationResponseCode { get; set; }

        [JsonProperty("Terminal Type", NullValueHandling = NullValueHandling.Ignore)]
        public string TerminalType { get; set; }

        [JsonProperty("Data Authentication Code", NullValueHandling = NullValueHandling.Ignore)]
        public string DataAuthenticationCode { get; set; }

        [JsonProperty("ICC Dynamic Number", NullValueHandling = NullValueHandling.Ignore)]
        public string IccDynamicNumber { get; set; }

        [JsonProperty("Issuer Authentication Data", NullValueHandling = NullValueHandling.Ignore)]
        public string IssuerAuthenticationData { get; set; }
    }
}
