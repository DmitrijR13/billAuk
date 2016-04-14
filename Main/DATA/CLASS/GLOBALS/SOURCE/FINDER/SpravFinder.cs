using System.Collections.Generic;
using STCLINE.KP50.Interfaces;
using System.Data;
using System.Runtime.Serialization;

namespace STCLINE.KP50.Global
{

    [DataContract]
    public class Payments : Finder
    {
        [DataMember]
        public string dat_s { set; get; }
        [DataMember]
        public string dat_po { set; get; }
        [DataMember]
        public List<_Point> points { set; get; }
        [DataMember]
        public string uname { set; get; }
        [DataMember]
        public string name_user { set; get; }
        [DataMember]
        public DataSet data { set; get; }
        [DataMember]
        public int nzp_area { set; get; }
        [DataMember]
        public bool checkCanChangeOperDay { set; get; }
        [DataMember]
        public bool prepareContrDistribReport { set; get; }
        [DataMember]
        public int nzp_supp { set; get; }
        [DataMember]
        public bool hide_equal { set; get; }

        public Payments()
            : base()
        {
            checkCanChangeOperDay = false;
            // подготовить отчет "Контроль распределения оплат"
            prepareContrDistribReport = true;
            hide_equal = false;
        }
    }
}
