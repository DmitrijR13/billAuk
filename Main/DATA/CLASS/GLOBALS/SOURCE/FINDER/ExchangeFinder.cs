using System.Collections.Generic;
using System.Linq;
using STCLINE.KP50.Interfaces;
using System.Runtime.Serialization;

namespace STCLINE.KP50.Global
{

    [DataContract]
    public class ExFinder : Finder
    {
        [DataMember]
        public bool success { get; set; }

        [DataMember]
        public int VTB24ReestrID { get; set; }
        [DataMember]
        public int ReestrID { get; set; }

        [DataMember]
        public int Status { get; set; }
        [DataMember]
        public int nzp_bank { get; set; }
        [DataMember]
        public int nzp_supp { get; set; }

        [DataMember]
        public List<int> ListNzpWp { get; set; }

        [DataMember]
        public int activeTab { get; set; }

        [DataMember]
        public string fileName { get; set; }

        [DataMember]

        public string status { get; set; }
        [DataMember]

        public string errors { get; set; }

        [DataMember]
        public string userName { get; set; }

        public ExFinder()
            : base()
        {
            ListNzpWp = new List<int>();
        }
    }
}
