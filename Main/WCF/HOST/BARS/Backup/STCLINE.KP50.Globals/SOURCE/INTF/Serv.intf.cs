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
    public interface I_Serv
    {
        [OperationContract]
        List<Service> FindLsServices(Service finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        List<_Supplier> FindServiceSuppliers(Service finder, out Returns ret);

        [OperationContract]
        List<_Formula> FindSupplierFormuls(Service finder, out Returns ret);

        [OperationContract]
        Returns SaveService(Service finder, Service primfinder);

        [OperationContract]
        List<Service> FindDomService(Service finder, out Returns ret);

        [OperationContract]
        Returns FindLSDomFromDomService(Service finder);

        [OperationContract]
        List<Service> GetGroupServ(Service finder, out Returns ret);

        [OperationContract]
        Returns ServiceIntoServpriority(ServPriority finder, enSrvOper oper);

        [OperationContract]
        List<ServPriority> GetServpriority(ServPriority finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        Returns SaveServFormula(ServFormula finder);

        [OperationContract]
        List<ServFormula> LoadSevFormuls(ServFormula finder, out Returns ret);
    }

    [DataContract]
    public class Service : Ls
    {
        string _dat_s = "";
        string _dat_po = "";
        string _dat_when = "";
        string _dat_del = "";

        [DataMember]
        public RecordMonth YM;

        [DataMember]
        public int month_
        {
            get
            {
                return YM.month_;
            }
            set
            {
                YM.month_ = value;
            }
        }
        [DataMember]
        public int year_
        {
            get
            {
                return YM.year_;
            }
            set
            {
                YM.year_ = value;
            }
        }

        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public int nzp_groupserv { get; set; }
        [DataMember]
        public int cnt_ls { get; set; }
        [DataMember]
        public string service { get; set; }
        [DataMember]
        public long nzp_supp { get; set; }
        [DataMember]
        public string name_supp { get; set; }
        [DataMember]
        public int nzp_frm { get; set; }
        [DataMember]
        public string name_frm { get; set; }
        [DataMember]
        public int nzp_tarif { get; set; }
        [DataMember]
        public decimal tarif { get; set; }
        [DataMember]
        public string tarif_s { get; set; }
        [DataMember]
        public string dat_s { get { return Utils.ENull(_dat_s); } set { _dat_s = value; } }
        [DataMember]
        public string dat_po { get { return Utils.ENull(_dat_po); } set { _dat_po = value; } }
        [DataMember]
        public int is_actual { get; set; }
        //[DataMember]
        //public string remark { get; set; }
        
        [DataMember]
        public string dat_when { get { return Utils.ENull(_dat_when); } set { _dat_when = value; } }
        [DataMember]
        public long nzp_user_when { get; set; }

        [DataMember]
        public string dat_del { get { return Utils.ENull(_dat_del); } set { _dat_del = value; } }
        [DataMember]
        public long nzp_user_del { get; set; }

        [DataMember]
        public int nzp_foss { get; set; }

        /// <summary> Признак:
        /// 1     - Услуга оказывается в заданном расчетном месяце
        /// 0     - Услуга не оказывается в заданном расчетном месяце, но она оказывалась в другие периоды
        /// -1    - Нет ни одного действующего периода оказания услуги
        /// иначе - Значение не определено
        /// </summary>
        [DataMember]
        public int activePeriod { get; set; }

        [DataMember]
        public string service_small { get; set; }
        [DataMember]
        public string service_name { get; set; }
        [DataMember]
        public string ed_izmer { get; set; }
        [DataMember]
        public int type_lgot { get; set; }
        [DataMember]
        public int ordering { get; set; }
        [DataMember]
        public int nzp_measure { get; set; }

        public Service()
            : base()
        {
            YM.month_ = 0;
            YM.year_ = 0;
            nzp_serv = 0;
            nzp_groupserv = 0;
            cnt_ls = 0;
            service = "";
            nzp_supp = 0;
            name_supp = "";
            nzp_frm = 0;
            name_frm = "";
            nzp_tarif = 0;
            tarif = 0;
            tarif_s = "";
            _dat_s = "";
            _dat_po = "";
            is_actual = 0;
            //remark = "";
            _dat_when = "";
            nzp_user_when = 0;

            _dat_when = "";
            nzp_user_del = 0;

            nzp_foss = 0;

            activePeriod = Constants._ZERO_;

            service_small = "";
            service_name = "";
            ed_izmer = "";
            type_lgot = Constants._ZERO_;
            ordering = 0;
            nzp_measure = 0;
        }
    }

    [DataContract]
    public struct _Formula
    {
        string _dat_s;
        string _dat_po;

        [DataMember]
        public int nzp_frm { get; set; }
        [DataMember]
        public string name_frm { get; set; }
        [DataMember]
        public string dat_s { get { return Utils.ENull(_dat_s); } set { _dat_s = value; } }
        [DataMember]
        public string dat_po { get { return Utils.ENull(_dat_po); } set { _dat_po = value; } }
    }

    [DataContract]
    public class ServPriority : Finder
    {
        [DataMember]
        public int nzp_key { get; set; }

        [DataMember]
        public string dat_s { get; set; }

        [DataMember]
        public string dat_po { get; set; }

        [DataMember]
        public int nzp_serv { get; set; }

        [DataMember]
        public int ordering { get; set; }

        [DataMember]
        public string service { get; set; }

        [DataMember]
        public int move { get; set; }
        
        public ServPriority()
            : base()
        {
            nzp_key = 0;
            dat_s = "";
            dat_po = "";
            nzp_serv = 0;
            ordering = 0;
            service = "";
            move = 0;
        }
    }

    [DataContract]
    public class ServFormula : Finder
    {
        [DataMember]
        public int nzp_calc_method { get; set; }

        [DataMember]
        public string rashod_name { get; set; }

        [DataMember]
        public string tarif_name { get; set; }
        
        [DataMember]
        public string formula_name { get; set; }

        [DataMember]
        public string dat_s { get; set; }

        [DataMember]
        public string dat_po { get; set; }

        [DataMember]
        public int nzp_measure{ get; set; }

        [DataMember]
        public string measure { get; set; }

        [DataMember]
        public int nzp_prm_ls{ get; set; }

        [DataMember]
        public int nzp_prm_dom { get; set; }

        [DataMember]
        public int nzp_frm_typrs { get; set; }

        [DataMember]
        public int nzp_prm_rash { get; set; }

        [DataMember]
        public int nzp_serv { get; set; }

        [DataMember]
        public int nzp_frm { get; set; }

        [DataMember]
        public string service { get; set; }

        [DataMember]
        public string created { get; set; }

        [DataMember]
        public string method_name { get; set; }

        public ServFormula()
            : base()
        {
            nzp_calc_method = 0;
            rashod_name = "";
            tarif_name = "";
            formula_name = "";
            dat_s = "";
            dat_po = "";
            nzp_measure = 0;
            nzp_prm_ls = 0;
            nzp_prm_dom = 0;
            nzp_frm_typrs = 0;
            nzp_prm_rash = 0;
            nzp_serv = 0;
            nzp_frm = 0;
            service = "";
            created = "";
            measure = "";
            method_name = "";
        }
    }
}
