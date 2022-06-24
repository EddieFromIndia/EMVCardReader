using System.Collections.Generic;

namespace EMVCardReader
{
    public class ADFModel
    {
        public byte[] AID { get; set; }
        public int SFI { get; set; }
        public byte[] ADF { get; set; }
        public byte[] FCI { get; set; }
        public byte[] ProcessingOptions { get; set; }
        public byte[] PDOL { get; set; }
        public List<RecordModel> AEFs { get; set; } = new List<RecordModel>();
    }
}
