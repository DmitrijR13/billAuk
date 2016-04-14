using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Collections;
using STCLINE.KP50.Global;
using System.Diagnostics;

namespace STCLINE.KP50.SrvUpdaters
{
    [ServiceContract]   
    public interface IUpdaters
    {
        //основняа процедура обновления
        [OperationContract]       
        [WebGet(UriTemplate = "GetData", BodyStyle = WebMessageBodyStyle.Bare)]
        DictsClass GetDownloadPath(Dictionary<string, string> lib1, Dictionary<string, string> lib2, string raj_ip);


        //возвращает полную информацию об обновлении заданного типа
        [OperationContract]
        [WebGet(UriTemplate = "GetData", BodyStyle = WebMessageBodyStyle.Bare)]
        DictsClass GetUpdatesFullInfo();


        //возвращает ответ на запрос
        [OperationContract]
        [WebGet(UriTemplate = "GetData", BodyStyle = WebMessageBodyStyle.Bare)]
        Dictionary<string,object> GetPatchResult(ArrayList sql_str, string data_base_type, byte[] soup);
    }

    [DataContract]
    //[KnownType(typeof(BESSMISLA))]
    //[KnownType(typeof(UpData))]
    //[KnownType(typeof(UpData[]))]
    public class DictsClass
    {

        //ArrayList host_list;

        //ArrayList web_list;

        List<UpData2> l1;
        List<UpData2> l2;

        [DataMember]
        public List<UpData2> L1
        {
            set { l1 = value; }
            get { return l1; }
        }

        [DataMember]
        public List<UpData2> L2
        {
            set { l2 = value; }
            get { return l2; }
        }
    }

    [DataContract]
    public class UpData2 //Данные об обновлении
    {
        [DataMember]
        public double Version;
        [DataMember]
        public string status;
        [DataMember]
        public string typeUp;
        [DataMember]
        public string Path;
        [DataMember]
        public string key;
        [DataMember]
        public string soup;
        [DataMember]
        public string nzp;
        [DataMember]
        public string web_path;
        [DataMember]
        public string report;


        public UpData2()
        {
            this.Version = -1;
            this.status = "NO DATA";
            this.Path = "NO DATA";
            this.key = "NO DATA";
            this.soup = "NO DATA";
            this.nzp = "NO DATA";
            this.web_path = "NO DATA";
            this.report = "NO DATA";

        }
        public UpData2(UpData upd)
        {
            this.Version = upd.Version;
            this.status = upd.status;
            this.typeUp = upd.typeUp;
            this.report = upd.report;
        }

    }

   

}
