using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Microsoft.SqlServer.Server;
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
        List<LightPrm> GetShortPrm(Prm finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        Returns SavePrmArea(Prm finder);

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
        List<NormTreeView> GetServForNorm(out Returns ret, int nzp_wp, bool showOld);

        [OperationContract]
        List<NormParam> GetAddNormParam(out Returns ret, int id);

        [OperationContract]
        Returns SaveNormFirstStage(Norm norm);

        [OperationContract]
        Returns SaveNormParamStatus(int normTypeId);


        [OperationContract]
        Returns SaveParamSecondStage(NormTypesSign finder);

        [OperationContract]
        List<PrmTypes> GetMeasuresForNorm(out Returns ret);

        [OperationContract]
        List<PrmTypes> GetKindNorm(out Returns ret);

        [OperationContract]
        List<PrmTypes> GetServNorm(out Returns ret);

        [OperationContract]
        List<PrmTypes> GetParamNorm(int TypePrm, int NormTypeId, out Returns ret);

        [OperationContract]
        List<PrmTypes> GetValueTypesNorm(out Returns ret);

        [OperationContract]
        Norm GetNormParam(out Returns ret, int id_norm);

        [OperationContract]
        Norm GetNormParamValuesOnLoadPage(out Returns ret, int id_norm);

        [OperationContract]
        List<NormParamValue> GetNormParamValues(out Returns ret, int nzp_prm, int norm_type_id);

        [OperationContract]
        Param FindParam(Param finder, out Returns ret);

        [OperationContract]
        void UpdateTarifCalculation(List<Prm> finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        List<Res_y> LoadSpravValueForPrm(Param finder, out Returns ret);

        [OperationContract]
        List<PrmTypes> NormParamValues(Param finder, out Returns ret);

        [OperationContract]
        Returns SavePrmName(Prm finder, enSrvOper oper);

        [OperationContract]
        List<PrmTypes> LoadPrmTypes(PrmTypes finder, out Returns ret);
        [OperationContract]
        List<NormParamCombination> GetCombinations(NormFinder finder, out Returns ret);

        [OperationContract]
        DataTable GetNormatives(NormFinder finder, out Returns ret);
        [OperationContract]
        Returns SaveNormatives(NormFinder finder);
        [OperationContract]
        Returns InsertNewNormValues(NormFinder finder, DataSet NewNorms);

        [OperationContract]
        List<Resolution> GetListResolution(Resolution finder, out Returns ret);

        [OperationContract]
        Returns SaveResY(Res_y finder);

        [OperationContract]
        Returns SaveResolution(Resolution finder);

        [OperationContract]
        NormParam GetTypePrmIdByNzpPrm(string nzp_prm, int normId, out Returns ret);

        [OperationContract]
        Dictionary<TypeTarif, PrmTarifs> GetGroupedTarifsData(PrmTarifFinder finder, out Returns ret);

        [OperationContract]
        Returns SetGroupedTarifsData(Dictionary<TypeTarif, PrmTarifs> newTarifs, PrmTarifFinder finder);

        [OperationContract]
        List<long> GetListLsByTarif(PrmTarifFinder finder, TypeTarif type, Tarif tarif, out Returns ret);
    }

    //----------------------------------------------------------------------

    /// <summary>
    /// Номера групп параметров
    /// </summary>
    public static class ParamNums
    {
        public const int General5 = 5;
        public const int General10 = 10;


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
        /// Параметры управляющих организаций
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
        public static int[] generalParams = new int[] { General5, General10 };
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
            /// Состояние лицевого счета
            /// </summary>
            State = 51,

            /// <summary>
            /// Количество прописанных жильцов
            /// </summary>
            GilNumberRegistered = 1010270,

            /// <summary>
            /// Тип собственности
            /// </summary>
            TipSobstvennosti = 2009,

            /// <summary>
            /// Отапливаемая площадь
            /// </summary>
            OtopSqu = 133
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

        /// <summary>
        /// Параметры дома
        /// </summary>
        public enum SystemParams
        {
            /// <summary>
            /// Расщепление - Распределение/расщепление оплат выполнять по
            /// </summary>
            PaymentDistributionMethod = 1273,

            /// <summary>
            /// Расщепление - Автоматически рассчитывать суммы к перечислению
            /// </summary>
            CalcDistributionAutomatically = 1274
        }

        /// <summary>
        /// Справочные параметры
        /// </summary>
        public enum SpravParams
        {
            /// <summary>
            /// Вид платежного кода
            /// </summary>
            TypePkod = 1281
        }
    }
    [Serializable]
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
        public string type_val_prm_name { get; set; }
        [DataMember]
        public string type_prm_name { get; set; }
        [DataMember]
        public string type_name { get; set; }
        [DataMember]
        public int nzp_res { get; set; }
        [DataMember]
        public int prm_num { get; set; }

        [DataMember]
        public int prm_type_id { get; set; }

        [DataMember]
        public int is_day_uchet { get; set; }

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
        public string old_val_prm { get; set; }
        [DataMember]
        public int val_prm_sprav { get; set; }
        [DataMember]
        public long nzp { get; set; }
        [DataMember]
        public string name_tab { get { return Utils.ENull(_name_tab); } set { _name_tab = value; } }
        [DataMember]
        public string block { get; set; }
        [DataMember]
        public string old_field { set; get; }
        [DataMember]
        public int is_required { set; get; }
        [DataMember]
        public int is_show { set; get; }
        [DataMember]
        public int is_system { set; get; }
        [DataMember]
        public decimal low_ { set; get; }
        [DataMember]
        public decimal high_ { set; get; }
        [DataMember]
        public int digits_ { set; get; }

        public enIntvType intvtype
        {
            get
            {
                if (is_day_uchet == 1) return enIntvType.intv_Day;
                else
                {
                    return enIntvType.intv_Month;
                    //switch (nzp_prm) { case 51: case 508: return enIntvType.intv_Day; default: return enIntvType.intv_Month; }
                }
            }
        }

        public ParamCommon()
            : base()
        {
            block = "";
            nzp_prm = 0;
            type_val_prm_name = type_prm_name = "";
            name_prm = "";
            dat_s = "";
            dat_po = "";
            type_prm = "";
            nzp_res = 0;
            is_show = 0;
            prm_type_id = 0;
            nzp = 0;
            prm_num = 0;
            is_day_uchet = 0;
            is_system = 0;
            name_tab = "";
            type_name = "";
            val_prm = "";
            numer = 0;
            old_field = "";
            is_required = 0;
            val_prm_sprav = 0;
            high_ = low_ = digits_ = 0;
        }

        public void CopyTo(ParamCommon destination)
        {
            destination.block = this.block;
            destination.nzp_prm = this.nzp_prm;
            destination.name_prm = this.name_prm;
            destination.dat_s = this.dat_s;
            destination.dat_po = this.dat_po;
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
    public class ExportParamsFinder : Finder
    {
        [DataMember]
        public int prm_num { get; set; }
        [DataMember]
        public string dat_s { get; set; }
        [DataMember]
        public string dat_po { get; set; }
        [DataMember]
        public int numer { get; set; }
        [DataMember]
        public List<int> paramsNzp { get; set; }
        [DataMember]
        public bool is_export_names_checked { get; set; }

        public ExportParamsFinder()
        {
            prm_num = 0;
            dat_s = "";
            dat_po = "";
            numer = 0;
            paramsNzp = new List<int>();
            is_export_names_checked = true;
        }
    }

    [Serializable]
    [DataContract]
    public class Param : ParamCommon
    //----------------------------------------------------------------------
    {
        //[DataMember]
        //public decimal low_ { get; set; }
        //[DataMember]
        //public decimal high_ { get; set; }
        //[DataMember]
        //public int digits_ { get; set; }
        [DataMember]
        public int norm_type_id { get; set; }
        [DataMember]
        public List<Res_y> values { get; set; }
        [DataMember]
        public List<PrmTypes> norm_sprav_values { get; set; }
        [DataMember]
        public int show_point { get; set; }

        [DataMember]
        public string pref_sprav { get; set; }
        [DataMember]
        public string new_val_prm { get; set; }

        public Param()
            : base()
        {
            low_ = -1;
            high_ = -1;
            digits_ = -1;
            norm_type_id = 0;
            values = new List<Res_y>();
            norm_sprav_values = new List<PrmTypes>();
            show_point = 0;
            pref_sprav = "";
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
    [Serializable]
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
        public int nzp_key { get; set; }
        [DataMember]
        public string val_prm_po { get; set; }
        [DataMember]
        public int is_actual { get; set; }

        [DataMember]
        public string dat_when { get { return Utils.ENull(_dat_when); } set { _dat_when = value; } }
        [DataMember]
        public string dat_when_po { get; set; }
        [DataMember]
        public int nzp_user_when { get; set; }
        [DataMember]
        public string user_name { get; set; }
        [DataMember]
        public string dat_del { get { return Utils.ENull(_dat_del); } set { _dat_del = value; } }
        [DataMember]
        public int user_del { get; set; }
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

        [DataMember]
        public int not_hist { set; get; }

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
            not_hist = 0;
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

    [Serializable]
    [DataContract]
    public class PrmTypes : Finder
    {
        [DataMember]
        public int id { get; set; }

        [DataMember]
        public string type_name { get; set; }

        public PrmTypes()
            : base()
        {
            id = 0;
            type_name = "";
        }
    }


    [Serializable]
    [DataContract]
    public class PrmTypesWithStringId : Finder
    {
        [DataMember]
        public string id { get; set; }

        [DataMember]
        public string type_name { get; set; }

        public PrmTypesWithStringId()
            : base()
        {
            id = "";
            type_name = "";
        }
    }

    [DataContract]
    public class Resolution : Finder
    {
        [DataMember]
        public int nzp_res { get; set; }

        [DataMember]
        public int nzp_prm { get; set; }

        [DataMember]
        public string name_short { get; set; }

        [DataMember]
        public string name_res { get; set; }

        [DataMember]
        public int is_readonly { get; set; }

         [DataMember]
        public int is_accessible { get; set; }

         public Resolution()
            : base()
        {
            nzp_res = 0;
            name_res = name_short = "";
            is_readonly = 1;
            is_accessible = 0;
            nzp_prm = 0;
        }
    }

    [DataContract]
    public class NormTreeView : PrmTypes
    {
        [DataMember]
        public List<PrmTypes> NormTypes { get; set; }

        public NormTreeView()
            : base()
        {
            NormTypes = new List<PrmTypes>();
        }
    }

    [DataContract]
    public class Norm : Finder
    {
        [DataMember]
        public int id { get; set; }

        [DataMember]
        public string norm_name { get; set; }

        [DataMember]
        public string measure { get; set; }

        [DataMember]
        public int nzp_measure { get; set; }

        [DataMember]
        public int nzp_serv { get; set; }

        [DataMember]
        public string service { get; set; }

        [DataMember]
        public string str_kind_norm { get; set; }

        [DataMember]
        public int id_kind_norm { get; set; }

        [DataMember]
        public List<int> nzp_wp { get; set; }

        [DataMember]
        public string dat_from { get; set; }

        [DataMember]
        public string dat_po { get; set; }

        [DataMember]
        public string dat_from_old { get; set; }

        [DataMember]
        public string dat_to_old { get; set; }

        [DataMember]
        public string created_on { get; set; }

        [DataMember]
        public string changed_on { get; set; }

        [DataMember]
        public string changed_by { get; set; }
        [DataMember]
        public string created_by { get; set; }

        [DataMember]
        public bool is_finished { get; set; }

        [DataMember]
        public bool is_day_norm { get; set; }

        [DataMember]
        public List<NormParam> norm_params { get; set; }

        [DataMember]
        public bool is_refreshing { get; set; }

        public Norm()
        {
            id = 0;
            norm_name = "";
            measure = "";
            nzp_measure = 0;
            nzp_serv = 0;
            service = "";
            str_kind_norm = "";
            id_kind_norm = 0;
            nzp_wp = new List<int>();
            dat_from = "";
            dat_po = "";
            dat_from_old = "";
            dat_to_old = "";
            is_finished = true;
            is_day_norm = false;
            norm_params = new List<NormParam>();
            is_refreshing = false;
            created_on = "";
            created_by = "";
            changed_on = "";
            changed_by = "";

        }
    }

    /// <summary>
    /// Данные для одной вкладки 
    /// </summary>
    [DataContract]
    public class PrmTarifs
    {
        /// <summary>
        /// Справочник услуг для вкладки
        /// </summary>
        [DataMember]
        public Dictionary<int, string> ListServices { get; set; }
        /// <summary>
        /// Список тарифов с перечисленными атрибутами
        /// </summary>
        [DataMember]
        public List<Tarif> ListTarifs { get; set; }

        public PrmTarifs()
        {
            ListServices = new Dictionary<int, string>();
            ListTarifs = new List<Tarif>();
        }
    }

    /// <summary>
    /// Базовый класс для отображения тарифа в режиме "Ведение тарифов"
    /// </summary>
    [DataContract]
    public class Tarif
    {
        [DataMember]
        public int nzp_serv { get; set; } //key part
        [DataMember]
        public string service { get; set; }

        [DataMember]
        public int nzp_supp { get; set; }//key part
        [DataMember]
        public string name_supp { get; set; }

        [DataMember]
        public int nzp_frm { get; set; }//key part
        [DataMember]
        public string name_frm { get; set; }
        [DataMember]
        public int nzp_frm_type { get; set; }//key part

        [DataMember]
        public int nzp_prm { get; set; }//key part
        [DataMember]
        public string name_prm { get; set; }

        [DataMember]
        public int count_ls { get; set; }
        [DataMember]
        public int count_houses { get; set; }

        [DataMember]
        public decimal? tarif { get; set; }//key part

        //если =null, то удаляем тарифы по ключу
        [DataMember]
        public decimal? new_tarif { get; set; }
        [DataMember]
        public DateTime? DateBegin { get; set; }

        [DataMember]
        public DateTime? DateEnd { get; set; }

    }

    /// <summary>
    /// Класс для фильтрации получения данных по ведению тарифов
    /// </summary>
    public class PrmTarifFinder
    {
        [DataMember]
        private DateTime _dateTo;
        [DataMember]
        public int listNumber { get; set; }

        [DataMember]
        public string pref { get; set; }

        /// <summary> Код пользователя необходимый для получения имени таблицы </summary>
        [DataMember]
        public int nzp_user { get; set; }
        [DataMember]
        public Dictionary<TypeTarif, int> nzp_servs { get; set; }
        /// <summary>
        /// Показывать полный перечень тарифов, включая tarif is null
        /// </summary>
        [DataMember]
        public bool show_all { get; set; }

        [DataMember]
        public DateTime date_from { get; set; }

        [DataMember]
        public DateTime date_to
        {
            get { return _dateTo; }
            set
            {
                if (value >= date_from)
                {
                    _dateTo = value;
                }
            }
        }

        public PrmTarifFinder()
        {
            pref = "";
            listNumber = 0;
            date_from = new DateTime(2000, 1, 1);
            _dateTo = new DateTime(3000, 1, DateTime.DaysInMonth(3000, 1));
            nzp_servs = new Dictionary<TypeTarif, int>
            {
                {TypeTarif.Ls, 0},
                {TypeTarif.House, 0},
                {TypeTarif.Supplier, 0},
                {TypeTarif.DataBase, 0},
            };
        }
        public PrmTarifFinder(int nzpUser, Dictionary<TypeTarif, int> nzpServs, bool showAll, DateTime dateFrom, DateTime dateTo)
        {
            _dateTo = dateTo;
            nzp_user = nzpUser;
            nzp_servs = nzpServs;
            show_all = showAll;
            date_from = dateFrom;
        }

    }

    [DataContract]
    public class NormFinder : Norm
    {
        [DataMember]
        public DataTable DT { get; set; }

        [DataMember]
        public int NormTypeId { get; set; }

        [DataMember]
        public int PageNumber { get; set; }

        [DataMember]
        public int count_influence_params { get; set; }
        public NormFinder()
        {
            NormTypeId = 0;
            PageNumber = -1;
            count_influence_params = 0;
        }
    }

    [DataContract]
    public class NormParam
    {
        [DataMember]
        public int id { get; set; }
        [DataMember]
        public string str_prm_ls { get; set; }
        [DataMember]
        public int nzp_prm_ls { get; set; }
        [DataMember]
        public string str_prm_dom { get; set; }
        [DataMember]
        public int nzp_prm_dom { get; set; }
        [DataMember]
        public string str_type_val { get; set; }
        [DataMember]
        public int id_type_val { get; set; }
        [DataMember]
        public string min_val { get; set; }
        [DataMember]
        public string max_val { get; set; }
        [DataMember]
        public int max_amount { get; set; }
        [DataMember]
        public int ordering { get; set; }

        [DataMember]
        public bool is_finished { get; set; }

        public NormParam()
        {
            id = 0;
            str_prm_ls = "";
            nzp_prm_ls = 0;
            str_prm_dom = "";
            nzp_prm_dom = 0;
            str_type_val = "";
            id_type_val = 0;
            min_val = "";
            max_val = "";
            max_amount = 0;
            ordering = 0;
            is_finished = true;
        }
    }

    [DataContract]
    public class NormParamValue
    {
        [DataMember]
        public string val_1 { get; set; }
        [DataMember]
        public string val_2 { get; set; }
        [DataMember]
        public string val_name { get; set; }
        [DataMember]
        public int ordering { get; set; }
        [DataMember]
        public int nzp_prm { get; set; }

        public NormParamValue()
        {
            val_1 = "";
            val_2 = "";
            val_name = "";
            ordering = 0;
            nzp_prm = 0;
        }



    }

    [Serializable]
    [DataContract]
    public class Res_y: Finder
    {
        [DataMember]
        public int nzp_res { get; set; }
        [DataMember]
        public int is_del { get; set; }
        [DataMember]
        public int nzp_y { get; set; }
        [DataMember]
        public string name_y { get; set; }

        public Res_y()
            : base()
        {
            nzp_res = Constants._ZERO_;
            nzp_y = Constants._ZERO_;
            name_y = "";
            is_del = 0;
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

    public enum SRegPrm
    {
        /// <summary>
        /// Показывать банк в групповых операциях - характеристики жилья
        /// </summary>
        ShowPoint = 7
    }

    public class NormTypesSign
    {

        public int id { get; set; }
        public int norm_type_id { get; set; }

        public int nzp_prm_ls { get; set; }
        public int nzp_prm_house { get; set; }
        public int min_val { get; set; }
        public int max_val { get; set; }
        public int max_count { get; set; }
        public int type_val_sign_id { get; set; }
        public int ordering { get; set; }
        public int is_finished { get; set; }

        public string getNameSTypeValSign(int type_val_sign_id)
        {
            switch (type_val_sign_id)
            {
                case (int)STypeValSign.Num: return "Число";
                case (int)STypeValSign.Date: return "Дата";
                case (int)STypeValSign.Period: return "Период";
                case (int)STypeValSign.NumPeriod: return "Период значений";
                case (int)STypeValSign.Boolean: return "Да/Нет";

                default: return "";
            }
        }

        public NormTypesSign()
        {
            norm_type_id = 0;
            nzp_prm_ls = 0;
            nzp_prm_house = 0;
            min_val = 0;
            max_val = 0;
            max_count = 0;
            type_val_sign_id = 0;
            ordering = 0;
        }
    }
    /// <summary>
    /// типы значений параметров влияющих на нормативы
    /// </summary>
    public enum STypeValSign
    {
        Num = 1,
        Date = 2,
        Period = 3,
        NumPeriod = 4,
        Boolean = 5,
        Sprav = 6
    }

    /// <summary>
    /// виды нормативов(ОДН, не ОДН и другие)
    /// </summary>
    public enum SKindNorm
    {
        Odn = 2,
        NotOdn = 1
    }

    /// <summary>
    /// Виды возвращаемых кодов ret.tag
    /// </summary>
    public enum NormKodRet
    {
        Error = -1,
        Exist = -2
    }
    public struct NormParamCombination
    {
        public int norm_type_sign_id { get; set; }
        public string name_prm { get; set; }
        public string param_value { get; set; }
        public int ordering { get; set; }
        public int nzp_prm { get; set; }
        public double param_value1 { get; set; }
        public double param_value2 { get; set; }
        public DateTime date_value1 { get; set; }
        public DateTime date_value2 { get; set; }
        public int type_val_sign { get; set; }
        public string name_prm_val { get; set; }

    }

    public struct NewNormValue
    {
        public int norm_type_id { get; set; }
        public double new_value { get; set; }
        public double old_value { get; set; }
        public int ordering_x { get; set; }
        public int ordering_y { get; set; }
    }


    //----------------------------------------------------------------------
    [Serializable]
    [DataContract]
    public class LightPrm
    //----------------------------------------------------------------------
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
        public string type_val_prm_name { get; set; }
        [DataMember]
        public string type_prm_name { get; set; }
        [DataMember]
        public string type_name { get; set; }
        [DataMember]
        public int nzp_res { get; set; }
        [DataMember]
        public int prm_num { get; set; }

        [DataMember]
        public int prm_type_id { get; set; }

        [DataMember]
        public int is_day_uchet { get; set; }

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
        public string old_val_prm { get; set; }
        [DataMember]
        public int val_prm_sprav { get; set; }
        [DataMember]
        public long nzp { get; set; }
        [DataMember]
        public string name_tab { get { return Utils.ENull(_name_tab); } set { _name_tab = value; } }
        [DataMember]
        public string block { get; set; }
        [DataMember]
        public string old_field { set; get; }
        [DataMember]
        public int is_required { set; get; }
        [DataMember]
        public int is_show { set; get; }
        [DataMember]
        public int is_system { set; get; }
        [DataMember]
        public decimal low_ { set; get; }
        [DataMember]
        public decimal high_ { set; get; }
        [DataMember]
        public int digits_ { set; get; }
        [DataMember]
        public int nzp_reg { get; set; }

        public enIntvType intvtype
        {
            get
            {
                if (is_day_uchet == 1) return enIntvType.intv_Day;
                else
                {
                    return enIntvType.intv_Month;
                }
            }
        }

        public LightPrm()
            : base()
        {
            block = "";
            nzp_prm = 0;
            type_val_prm_name = type_prm_name = "";
            name_prm = "";
            dat_s = "";
            dat_po = "";
            type_prm = "";
            nzp_res = 0;
            is_show = 0;
            prm_type_id = 0;
            nzp = 0;
            prm_num = 0;
            is_day_uchet = 0;
            is_system = 0;
            name_tab = "";
            type_name = "";
            val_prm = "";
            numer = 0;
            old_field = "";
            is_required = 0;
            val_prm_sprav = 0;
            high_ = low_ = digits_ = 0;
            nzp_reg = 0;
        }

        public LightPrm CopyFromPrm(Prm prm)
        {
            var sp = new LightPrm()
            {
                nzp_prm = prm.nzp_prm,
                name_prm = prm.name_prm,
                type_name = prm.type_name,
                prm_num = prm.prm_num,
                prm_type_id = prm.prm_type_id,
                is_day_uchet = prm.is_day_uchet,
                numer = prm.numer,
                type_prm = prm.type_prm,
                nzp_res = prm.nzp_res,
                old_field = prm.old_field,
                low_ = prm.low_,
                high_ = prm.high_,
                digits_ = prm.digits_,
                is_system = prm.is_system,
                type_prm_name = prm.type_prm_name,
                type_val_prm_name = prm.type_val_prm_name,
                nzp_reg = prm.nzp_reg,
                is_show = prm.is_show
            };
            return sp;
        }

        public Prm CopyInPrm()
        {
            var sp = new Prm()
            {
                nzp_prm = this.nzp_prm,
                name_prm = this.name_prm,
                type_name = this.type_name,
                prm_num = this.prm_num,
                prm_type_id = this.prm_type_id,
                is_day_uchet = this.is_day_uchet,
                numer = this.numer,
                type_prm = this.type_prm,
                nzp_res = this.nzp_res,
                old_field = this.old_field,
                low_ = this.low_,
                high_ = this.high_,
                digits_ = this.digits_,
                is_system = this.is_system,
                type_prm_name = this.type_prm_name,
                type_val_prm_name = this.type_val_prm_name,
                nzp_reg = this.nzp_reg,
                is_show = this.is_show
            };
            return sp;
        }
    }
}
