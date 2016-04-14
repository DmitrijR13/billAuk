using STCLINE.KP50.Global;
using System;
using System.Collections;
using System.Runtime.Serialization;

namespace STCLINE.KP50.Interfaces
{
    [DataContract]
    public class SysEvents : Finder
    {
        [DataMember]
        public int num { get; set; }
        [DataMember]
        public int nzp_event { get; set; }
        [DataMember]
        public DateTime? date_ { get; set; }
        [DataMember]
        public string nzp_user_loc { get; set; }
        [DataMember]
        public int nzp_dict { get; set; }
        [DataMember]
        public long nzp_obj { get; set; }
        [DataMember]
        public int nzp_dict_event { get; set; }
        [DataMember]
        public int nzp { get; set; }
        [DataMember]
        public string note { get; set; }
        [DataMember]
        public string user { get; set; }
        [DataMember]
        public string event_name { get; set; }
        [DataMember]
        public string login { get; set; }
        [DataMember]
        public new string bank { get; set; }
        [DataMember]
        public DateTime? from_date { get; set; }
        [DataMember]
        public DateTime? to_date { get; set; }
        [DataMember]
        public ArrayList users_list { get; set; }
        [DataMember]
        public ArrayList events_list { get; set; }
        [DataMember]
        public ArrayList entity_list { get; set; }
        [DataMember]
        public ArrayList actions_list { get; set; }
        [DataMember]
        public int entity_id { get; set; }
        [DataMember]
        public string entity_name { get; set; }
        [DataMember]
        public int mode { get; set; }
        [DataMember]
        public int nzp_act { get; set; }
        [DataMember]
        public string action { get; set; }
    }
}
