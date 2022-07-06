using System.Collections.Generic;

namespace EMVCardReader.Models
{
    public class ADFModel
    {
        public byte[] AID { get; set; }
        public byte[] ADF { get; set; }
        public byte[] ProcessingOptions { get; set; }
        public byte[] PDOL { get; set; }
        public List<RecordModel> AEFs { get; set; } = new List<RecordModel>();
        public byte[] ATC { get; set; }
        public byte[] LastOnlineATCRegister { get; set; }
        public byte[] PinTryCounter { get; set; }
        public byte[] LogEntry { get; set; }
        public byte[] LogFormat { get; set; }
    }
}
