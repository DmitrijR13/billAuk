using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using STCLINE.KP50.Global;


namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_Analiz
    {
        [OperationContract]
        void LoadAnaliz1(int year, out Returns ret, bool reload);
        [OperationContract]
        void LoadAdres(Finder finder, out Returns ret, int year, bool reload);
        [OperationContract]
        void LoadSupp(AnlSupp finder, out Returns ret, int year, bool reload);

        [OperationContract]
        List<AnlXX> GetAnlXX(Finder finder, out Returns ret);
        [OperationContract]
        List<AnlDom> GetAnlDom(Finder finder, out Returns ret);
        [OperationContract]
        List<AnlSupp> GetAnlSupp(Finder finder, out Returns ret);

    }
    //----------------------------------------------------------------------
    [DataContract]
    public class AnlXX : Finder
    //----------------------------------------------------------------------
    {
        [DataMember]
        public int serial_key { get; set; }
        [DataMember]
        public int nzp_dom { get; set; }
        [DataMember]
        public int nzp_area { get; set; }
        [DataMember]
        public int nzp_geu { get; set; }
        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public int nzp_supp { get; set; }
        [DataMember]
        public int nzp_frm { get; set; }


        public AnlXX()
            : base()
        {
            serial_key = 0;
            nzp_dom = 0;
            nzp_area = 0;
            nzp_geu = 0;
            nzp_serv = 0;
            nzp_supp = 0;
            nzp_frm = 0;
        }
    }
    //----------------------------------------------------------------------
    [DataContract]
    public class AnlDom : AnlXX
    //----------------------------------------------------------------------
    {
        [DataMember]
        public string area { get; set; }
        [DataMember]
        public string geu { get; set; }
        [DataMember]
        public string ulica { get; set; }
        [DataMember]
        public string ndom { get; set; }
        [DataMember]
        public int nzp_ul { get; set; }
        [DataMember]
        public int idom { get; set; }

        public AnlDom()
            : base()
        {
            area = "";
            geu = "";
            ulica = "";
            ndom = "";
            idom = 0;
            nzp_ul = 0;
        }
    }
    //----------------------------------------------------------------------
    [DataContract]
    public class AnlSupp : AnlXX
    //----------------------------------------------------------------------
    {
        [DataMember]
        public string area { get; set; }
        [DataMember]
        public string geu { get; set; }
        [DataMember]
        public string service { get; set; }
        [DataMember]
        public string name_supp { get; set; }
        [DataMember]
        public string name_frm { get; set; }

        public AnlSupp() : base()
        {
            area = "";
            geu = "";
            service = "";
            name_supp = "";
            name_frm = "";
        }
    }
}

