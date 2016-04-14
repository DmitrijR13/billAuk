using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using STCLINE.KP50.Global;
using System.Data;
using System.Collections;


namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_Prm
    {
        [OperationContract]
        List<Prm> GetPrm(Prm finder, enSrvOper oper, out Returns ret);       

        [OperationContract]
        Param FindPrmInfo(Prm finder, out Returns ret);

        [OperationContract]
        Returns CopyLsParams(Ls finderFrom, Ls finderTo);

        [OperationContract]
        Returns SavePrm(Param finder);

        [OperationContract]
        Prm FindPrmValue(Prm finder, out Returns ret);

        [OperationContract]
        List<Prm> FindPrmValues(Prm finder, out Returns ret);

        [OperationContract]
        List<Param> LoadParamsWithNumer(Prm finder, out Returns ret);

        [OperationContract]
        Returns DeleteLs(Ls finder);

        [OperationContract]
        List<Prm> GetNorms(Prm prm, out Returns ret);

        [OperationContract]
        List<Prm> GetPeriod(Prm prm, out Returns ret);

        [OperationContract]
        bool DeletePeriod(Prm prm, out Returns ret);

        [OperationContract]
        bool UpdateTableData(Prm prm, ArrayList list, int nzp_y, out Returns ret);

        [OperationContract]
        string GetSetRemarkForGeu(Prm finder, bool edit, string rem, out Returns ret);
        
        [OperationContract]
        DynamicTable GetTableData(Prm prm, out Returns ret);

        [OperationContract]
        List<Prm> GetKvarPrmList(Prm finder, out Returns ret);

        [OperationContract]
        Param FindParam(Param finder, out Returns ret);

        [OperationContract]
        void UpdateTarifCalculation(List<Prm> finder, enSrvOper oper, out Returns ret);
    }

    //----------------------------------------------------------------------

    /// <summary>
    /// Номера групп параметров
    /// </summary>
    public static class ParamNums
    {
        /// <summary>
        /// Параметры лицевого счета
        /// </summary>
        public static int[] lsParams = new int[] { 1, 3, 18, 19 };

        /// <summary>
        /// Параметры дома
        /// </summary>
        public static int[] domParams = new int[] { 2, 4 };

        /// <summary>
        /// Параметры улицы
        /// </summary>
        public static int[] ulicaParams = new int[] { 6 };

        /// <summary>
        /// Параметры территории
        /// </summary>
        public static int[] areaParams = new int[] { 7 };

        /// <summary>
        /// Параметры отделения
        /// </summary>
        public static int[] geuParams = new int[] { 8 };

        /// <summary>
        /// Параметры поставщика
        /// </summary>
        public static int[] supplierParams = new int[] { 11 };

        /// <summary>
        /// Параметры услуги
        /// </summary>
        public static int[] serviceParams = new int[] { 12 };

        /// <summary>
        /// Параметры прибора учета
        /// </summary>
        public static int[] counterParams = new int[] { 17 };

        /// <summary>
        /// Общесистемные параметры
        /// </summary>
        public static int[] generalParams = new int[] { 5, 10 };
    }

    /// <summary>
    /// Уникальные идентификаторы параметров
    /// </summary>
    public static class ParamIds
    {
        /// <summary>
        /// Параметры лицевого счета
        /// </summary>
        public enum LsParams
        {
            /// <summary>
            /// Комфортность
            /// </summary>
            Comfort = 3,

            /// <summary>
            /// Общая площадь
            /// </summary>
            SquareTotal = 4,

            /// <summary>
            /// Количество проживающих жильцов
            /// </summary>
            GilNumberLive = 5,

            /// <summary>
            /// Жилая/полезная площадь
            /// </summary>
            SquareLiving = 6,

            /// <summary>
            /// Приватизировано: да/нет
            /// </summary>
            Privatizirovano = 8,
            
            /// <summary>
            /// Количество прописанных жильцов
            /// </summary>
            GilNumberRegistered = 2005,

            /// <summary>
            /// Тип собственности
            /// </summary>
            TipSobstvennosti = 2009
        }

        /// <summary>
        /// Параметры дома
        /// </summary>
        public enum DomParams
        {
            /// <summary>
            /// Полезная площадь дома
            /// </summary>
            SquareUseful = 36,

            /// <summary>
            /// Полезная площадь дома
            /// </summary>
            SquareTotal = 40
        }
    }

    [DataContract]
    public class ParamCommon : Ls
    {
        string _name_tab;
        string _name_prm;
        string _type_prm;

        [DataMember]
        public int nzp_prm { get; set; }
        [DataMember]
        public string name_prm { get { return Utils.ENull(_name_prm); } set { _name_prm = value; } }
        [DataMember]
        public string type_prm { get { return Utils.ENull(_type_prm); } set { _type_prm = value; } }
        [DataMember]
        public int nzp_res { get; set; }
        [DataMember]
        public int prm_num{get;set;}
        [DataMember]
        public int numer { get; set; }
        [DataMember]
        public string prm_num_name
        {
            get
            {
                switch (prm_num)
                {
                    case 1:
                    case 18:
                    case 3: return "Квартирный";
                    case 2: return "Домовой";
                    default: return "";
                }
            }
            set { }
            
        }
        [DataMember]
        public string dat_s { get; set; }
        [DataMember]
        public string dat_po { get; set; }
        [DataMember]
        public string val_prm { get; set; }
        [DataMember]
        public long nzp { get; set; }
        [DataMember]
        public string name_tab { get { return Utils.ENull(_name_tab); } set { _name_tab = value; } }
        [DataMember]
        public string block { get; set; }
        public enIntvType intvtype { get { switch (nzp_prm) { case 51: case 508: return enIntvType.intv_Day; default: return enIntvType.intv_Month; } } }
        
        public ParamCommon()
            : base()
        {
            block = "";
            nzp_prm = 0;
            name_prm = "";
            dat_s = "";
            dat_po = "";
            type_prm = "";
            nzp_res = 0;
            nzp = 0;
            prm_num = 0;
            name_tab = "";
            val_prm = "";
            numer = 0;
        }

        public void CopyTo(ParamCommon destination)
        {
            destination.block = this.block;
            destination.nzp_prm = this.nzp_prm;
            destination.name_prm = this.name_prm;
            destination.dat_s = this.dat_s;
            destination.dat_po =this.dat_po;
            destination.type_prm = this.type_prm;
            destination.nzp_res = this.nzp_res;
            destination.nzp = this.nzp;
            destination.prm_num = this.prm_num;
            destination.name_tab = this.name_tab;
            destination.val_prm = this.val_prm;
            destination.numer = this.numer;
        }
    }

    [DataContract]
    public class Param : ParamCommon
    //----------------------------------------------------------------------
    {
        [DataMember]
        public decimal low_  { get; set; }
        [DataMember]
        public decimal high_  { get; set; }
        [DataMember]
        public int digits_  { get; set; }
        [DataMember]
        public List<Res_y> values { get; set; }


        public Param()
            : base()
        {
            low_ = -1;
            high_ = -1;
            digits_ = -1;
            values = new List<Res_y>();
        }

        public void CopyTo(Param destination)
        {
            ((ParamCommon)this).CopyTo(destination);
            destination.low_ = this.low_;
            destination.high_ = this.high_;
            destination.digits_ = this.digits_;
            destination.values = this.values;
        }
    }

    [DataContract]
    public class DynamicTable
    {
        [DataMember]
        public DataTable table { get; set; }
        
    }
    
    //----------------------------------------------------------------------
    public static class Parameters
    //----------------------------------------------------------------------
    {
        public static List<Param> ParametersList = new List<Param>(); //
    }
    //----------------------------------------------------------------------
    [DataContract]
    public class Prm : ParamCommon
    //----------------------------------------------------------------------
    {
        //string _dat_po;
        string _dat_when;
        string _dat_del;

        // ...
        string _name_link;

        [DataMember]
        public string cnt { get; set; }

        [DataMember]
        public int month_ { get; set; }
        [DataMember]
        public int year_ { get; set; }

        [DataMember]
        public string spis_prm { get; set; }

        [DataMember]
        public string dopprm { get; set; }
        //[DataMember]
        //public _Param param { get; set; }
        // ...

        [DataMember]
        public int nzp_key  { get; set; }
        [DataMember]
        public string val_prm_po  { get; set;  }
        [DataMember]
        public int is_actual { get; set; }

        [DataMember]
        public string dat_when    { get { return Utils.ENull(_dat_when); }    set { _dat_when = value; } }
        [DataMember]
        public string dat_when_po { get; set; }
        [DataMember]
        public int nzp_user_when { get; set; }
        [DataMember]
        public string user_name { get; set; }
        [DataMember]
        public string dat_del { get { return Utils.ENull(_dat_del); } set { _dat_del = value; } }
        [DataMember]
        public int user_del  { get; set; }
        [DataMember]
        public string delname { get; set; }

        [DataMember]
        public string name_link { get { return Utils.ENull(_name_link); } set { _name_link = value; } }

        [DataMember]
        public int visible { get; set; }
        [DataMember]
        public bool callFromFindPrm;

        [DataMember]
        public enCriteria criteria { get; set; }

        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public string service { get; set; }
        [DataMember]
        public int nzp_frm { get; set; }
        [DataMember]
        public string name_frm { get; set; }
        [DataMember]
        public string measure { get; set; }
        [DataMember]
        public int is_edit { get; set; }
        [DataMember]
        public Param param { get; set; }
        [DataMember]
        public bool isLoadParamInfo { get; set; }
        [DataMember]
        public int nzp_reg { get; set; }

        [DataMember]
        public int nzp_prm_calc { set; get; }
        [DataMember]
        public decimal tarif { get; set; }
        [DataMember]
        public DateTime arx9_dt { set; get; }
        [DataMember]
        public string arx9_ktr { set; get; }
        [DataMember]
        public int arx9_kkst { set; get; }
        [DataMember]
        public int arx9_nzp_conv_db { set; get; }

        [DataMember]
        public int nzp_trfl { set; get; }

        [DataMember]
        public int nzp_tarif { set; get; }


        public Prm()
            : base()
        {
            criteria = enCriteria.equal;
            cnt = "";
            month_ = 0;
            year_ = 0;
            // ...
            spis_prm = "";
            dopprm = "";
            //_Param param = new _Param();
            // ...

            nzp_key = 0;
            val_prm_po = "";
            is_actual = 0;
            dat_when = "";
            dat_when_po = "";
            nzp_user_when = 0;
            user_name = "";
            dat_del = "";
            user_del = 0;
            delname = "";
            name_link = "";

            callFromFindPrm = false;
            visible = 1;

            nzp_serv = 0;
            service = "";
            nzp_frm = 0;
            name_frm = "";
            is_edit = Constants._ZERO_;
            measure = "";

            param = null;
            isLoadParamInfo = false;
            nzp_reg = 0;
        }
    }
    [DataContract]
    public class Res_y
    {
        [DataMember]
        public int nzp_res { get; set; }
        [DataMember]
        public int nzp_y { get; set; }
        [DataMember]
        public string name_y { get; set; }

        public Res_y()
            : base()
        {
            nzp_res = 0;
            nzp_y = 0;
            name_y = "";
        }
    }

    [DataContract]
    public struct _Prm
    {
        [DataMember]
        public int nzp_prm { get; set; }
        [DataMember]
        public string name_prm { get; set; }
    }
}
