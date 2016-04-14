using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Global
{

    [DataContract]
    public class KLADRData
    {
        [DataMember]
        public string fullname { get; set; }
        [DataMember]
        public string code { get; set; }
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string socr { get; set; }
    }

    [DataContract]
    public class KLADRFinder : Finder
    {
        [DataMember]
        public string query { get; set; }

        [DataMember]
        public bool loadStreets { get; set; }

        [DataMember]
        public bool clearAddrSpace { get; set; }

        [DataMember]
        public string level { get; set; }

        [DataMember]
        public KLADRData region { get; set; }
        [DataMember]
        public KLADRData district { get; set; }
        [DataMember]
        public KLADRData city { get; set; }
        [DataMember]
        public KLADRData settlement { get; set; }
        [DataMember]
        public KLADRData street { get; set; }

        [DataMember]
        public List<KLADRData> regionList { get; set; }
        [DataMember]
        public List<KLADRData> districtList { get; set; }
        [DataMember]
        public List<KLADRData> cityList { get; set; }
        [DataMember]
        public List<KLADRData> settlementList { get; set; }
        [DataMember]
        public List<KLADRData> streetList { get; set; }

        [DataMember]
        public string regionCode { get; set; }
        [DataMember]
        public string districtCode { get; set; }
        [DataMember]
        public string cityCode { get; set; }
        [DataMember]
        public string settlementCode { get; set; }
        [DataMember]
        public string streetCode { get; set; }

        [DataMember]
        public string tableName { get; set; }

    }
}
