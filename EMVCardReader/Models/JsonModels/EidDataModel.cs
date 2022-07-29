using Newtonsoft.Json;

namespace EMVCardReader.Models
{
    public class EidDataModel
    {
        [JsonProperty("Full Name", NullValueHandling = NullValueHandling.Ignore)]
        public string FullName { get; set; }

        [JsonProperty("Place of Birth", NullValueHandling = NullValueHandling.Ignore)]
        public string PlaceOfBirth { get; set; }

        [JsonProperty("Date of Birth", NullValueHandling = NullValueHandling.Ignore)]
        public string DateOfBirth { get; set; }

        [JsonProperty("Gender", NullValueHandling = NullValueHandling.Ignore)]
        public string Gender { get; set; }

        [JsonProperty("Nationality", NullValueHandling = NullValueHandling.Ignore)]
        public string Nationality { get; set; }

        [JsonProperty("National Number", NullValueHandling = NullValueHandling.Ignore)]
        public string NationalNumber { get; set; }

        [JsonProperty("Address", NullValueHandling = NullValueHandling.Ignore)]
        public string Address { get; set; }
    }
}
