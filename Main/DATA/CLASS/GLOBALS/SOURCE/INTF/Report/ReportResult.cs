namespace Globals.SOURCE.INTF.Report
{
    using System.Runtime.Serialization;

    [DataContract]
    public class ReportResult
    {
        [DataMember]
        public int nzpExcelUtility { get; set; }

        [DataMember]
        public bool isPreview { get; set; }
    }
}