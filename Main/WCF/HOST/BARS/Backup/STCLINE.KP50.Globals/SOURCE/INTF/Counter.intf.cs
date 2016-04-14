using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Data;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_Counter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="oper">Принимает значения: SrvGet, SrvFind, SrvFindVal, SrvLoadCntTypeUchet</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        [OperationContract]
        List<Counter> GetPu(Counter finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        List<CounterCnttype> LoadCntType(Counter finder, out Returns ret);

        [OperationContract]
        Returns SaveCntType(CounterCnttype finder);

        [OperationContract]
        Returns FindCountersOrd(CounterOrd finder);

        [OperationContract]
        Returns SaveCountersOrd(List<CounterOrd> finder);

        [OperationContract]
        List<CounterOrd> GetCountersOrd(CounterOrd finder, out Returns ret);

        [OperationContract]
        List<CounterVal> GetCountersVals(CounterVal finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        Returns SaveCountersVals(List<CounterVal> newVals, enSrvOper oper);

        [OperationContract]
        Returns SaveCounter(Counter newCounter, string dat_calc);

        [OperationContract]
        Counter LoadCounter(Counter finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        Returns UnlockCounter(CounterVal finder);

        [OperationContract]
        List<Group> GetAllLocalLsGroup(Group finder, out Returns ret);

        [OperationContract]
        Returns PrepareReportPuData(CounterVal finder, List<Dom> houseList);

        [OperationContract]
        DataTable PrepareReportRashodPu(CounterVal finder, List<Dom> houseList, out Returns ret);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="oper">Принимает значения: SrvGetLsGroupCounter, SrvGetLsDomNotGroupCnt</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        [OperationContract]
        List<Ls> GetLsGroupCounter(Counter finder, enSrvOper oper, out Returns ret);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="list_nzp_kvar"></param>
        /// <param name="dat_calc"></param>
        /// <param name="oper">Принимает значения: SrvAddLsForGroupCnt, SrvDelLsFromGroupCnt</param>
        /// <returns></returns>
        [OperationContract]
        Returns ChangeLsForGroupCnt(Counter finder, List<int> list_nzp_kvar, string dat_calc, enSrvOper oper);

        /// <summary> Удаление прибора учета
        /// </summary>
        [OperationContract]
        Returns DeleteCounter(Counter finder);

        /// <summary> Загрузка показаний из электронного файла
        /// </summary>
        [OperationContract]
        Returns SaveUploadedCounterReadings(Finder finder);

        [OperationContract]
        /// <summary>
        /// Проверка возможности сохранения показаний приборов учета
        /// </summary>
        List<string> CheckSaveCounterVals(List<CounterVal> newVals, out Returns ret);

        [OperationContract]
        Returns CalcForPU(Finder finder, int p_year, int p_month);

        [OperationContract]
        Returns ReadReestr(FilesImported finder);
    }

    //----------------------------------------------------------------------
    public enum CounterKinds
    {
        /// <summary>
        /// Неопределенное значение
        /// </summary>
        None = 0,

        /// <summary>
        /// Общедомовой прибор учета
        /// </summary>
        Dom = 1,

        /// <summary>
        /// Групповой прибор учета
        /// </summary>
        Group = 2,

        /// <summary>
        /// Индивидуальный прибор учета
        /// </summary>
        Kvar = 3,

        /// <summary>
        /// Общеквартирный прибор учета
        /// </summary>
        Communal = 4
    }

    /// <summary>
    /// Виды приборов учета
    /// </summary>
    public static class CounterKind
    {
        /// <summary>
        /// Возвращает наименование вида прибора учета
        /// </summary>
        /// <param name="kind"></param>
        /// <returns></returns>
        public static string GetKindName(CounterKinds kind)
        { 
            switch(kind)
            {
                case CounterKinds.Dom: return "Общедомовой";
                case CounterKinds.Group: return "Групповой";
                case CounterKinds.Kvar: return "Индивидуальный";
                case CounterKinds.Communal: return "Общеквартирный";
                default: return "";
            }
        }

        public static string GetKindNameById(int id)
        {
            return GetKindName(GetKindById(id));
        }

        public static CounterKinds GetKindById(int id)
        {
            switch (id)
            {
                case (int)CounterKinds.Dom: return CounterKinds.Dom;
                case (int)CounterKinds.Group: return CounterKinds.Group;
                case (int)CounterKinds.Kvar: return CounterKinds.Kvar;
                case (int)CounterKinds.Communal: return CounterKinds.Communal;
                default: return CounterKinds.None;
            }
        }
    }

    [DataContract]
    public class Counter : Ls 
    {
        string _name_type;
        string _num_cnt;

        string _dat_prov;
        string _dat_provnext;
        string _dat_oblom;
        string _dat_poch;
        string _dat_close;
        string _dat_uchet;

        string _dat_prov_po;
        string _dat_provnext_po;
        string _dat_oblom_po;
        string _dat_poch_po;
        string _dat_close_po;
        string _dat_uchet_po;

        string _comment;
        string _service;
        string _cnt_type;
        int _cnt_state;

        [DataMember]
        public int nzp_counter  { get; set; }
        [DataMember]
        public long nzp { get; set; }
        [DataMember]
        public int nzp_serv     { get; set; }
        [DataMember]
        public int nzp_cnt { get; set; }
        [DataMember]
        public int is_gkal { get; set; }
        [DataMember]
        public int nzp_cnttype  { get; set; } //ссылка на тип ПУ
        [DataMember]
        public int nzp_type     { get; set; } //1,2,3
        [DataMember]
        public int cnt_stage    { get; set; } //разрядность ПУ 
        [DataMember]
        public decimal mmnog        { get; set; }  //множитель
        [DataMember]
        public string num_cnt   { get { return Utils.ENull(_num_cnt); }   set { _num_cnt = value; } } //номер ПУ

        [DataMember]
        public string dat_prov    { get { return Utils.ENull(_dat_prov); }     set { _dat_prov = value; } }
        [DataMember]
        public string dat_prov_po { get { return Utils.ENull(_dat_prov_po); }  set { _dat_prov_po = value; } } 

        [DataMember]
        public string dat_provnext    { get { return Utils.ENull(_dat_provnext); }    set { _dat_provnext = value; } }
        [DataMember]
        public string dat_provnext_po { get { return Utils.ENull(_dat_provnext_po); } set { _dat_provnext_po = value; } }

        [DataMember]
        public string dat_oblom    { get { return Utils.ENull(_dat_oblom); }    set { _dat_oblom = value; } }
        [DataMember]
        public string dat_oblom_po { get { return Utils.ENull(_dat_oblom_po); } set { _dat_oblom_po = value; } }

        [DataMember]
        public string dat_poch    { get { return Utils.ENull(_dat_poch); }    set { _dat_poch = value; } }
        [DataMember]
        public string dat_poch_po { get { return Utils.ENull(_dat_poch_po); } set { _dat_poch_po = value; } }

        [DataMember]
        public string dat_close    { get { return Utils.ENull(_dat_close); }    set { _dat_close = value; } }
        [DataMember]
        public string dat_close_po { get { return Utils.ENull(_dat_close_po); } set { _dat_close_po = value; } }

        [DataMember]
        public string dat_uchet    { get { return Utils.ENull(_dat_uchet); }    set { _dat_uchet = value; } } //дата показания
        [DataMember]
        public string dat_uchet_po { get { return Utils.ENull(_dat_uchet_po); } set { _dat_uchet_po = value; } } //дата показания

        [DataMember]
        public decimal val_cnt   { get; set; }  //само показание
        [DataMember]
        public string val_cnt_s { get; set; }
        [DataMember]
        public string val_cnt_pred_s { get; set; }
        [DataMember]
        public decimal val_cnt_po{ get; set; }

        [DataMember]
        public string dat_uchet_pred { get; set; }
        [DataMember]
        public decimal val_cnt_pred { get; set; }
        [DataMember]
        public int visible { get; set; }
        [DataMember]
        public string name_uchet { get; set; }
        [DataMember]
        public int is_pl { get; set; }
        [DataMember]
        public int cnt_ls { get; set; }
        [DataMember]
        public int is_uchet_ls { get; set; }
        [DataMember]
        public string measure { get; set; }
        [DataMember]
        public decimal ngp_cnt { get; set; }
        [DataMember]
        public string ngp_cnt_s { get; set; }
        [DataMember]
        public decimal ngp_lift { get; set; }
        [DataMember]
        public string ngp_lift_s { get; set; }
        [DataMember]
        public int is_doit { get; set; }
        [DataMember]
        public int is_actual { get; set; }

        [DataMember]
        public string name_type  { get { return Utils.ENull(_name_type); } set { _name_type = value; } } //название типа ПУ
        [DataMember]
        public string comment    { get { return Utils.ENull(_comment); } set { _comment = value; } }
        [DataMember]
        public string rashod    { get; set; }  //расход
        
        [DataMember]
        public string plan_rashod { get; set; }  //плановый расход
        [DataMember]
        public string normativ { get; set; }  //норматив

        [DataMember]
        public string service    { get { return Utils.ENull(_service); } set { _service = value; } } //услуга
        [DataMember]
        public string cnt_type { get { return Utils.ENull(_cnt_type); } set { _cnt_type = value; } } //название типа ПУ (квар,дом,групп)
        [DataMember]
        public string smmnog { get; set; }
        [DataMember]
        public string sname_type { get; set; }

        [DataMember]
        public int nzp_cv { get; set; }

        [DataMember]
        public string prm { get; set; }

        [DataMember]
        public string block { get; set; }

        [DataMember]
        public string dat_when { get; set; }

        [DataMember]
        public int year_ { get; set; }
        [DataMember]
        public int month_ { get; set; }

        [DataMember]
        public string dat_s { get; set; }
        [DataMember]
        public double rashod_d { get; set; }
        
        [DataMember]
        public string sred_rashod { get; set; }

        [DataMember]
        public bool show_ngp_lift { get; set; }

        [DataMember]
        public int CounterState { get { return Convert.ToInt32(Utils.ENull(_cnt_state.ToString())); } set { _cnt_state = value; } }

        /// <summary> Расход, вычисленный на основе текущего и предыдущего показаний
        /// </summary>
        [DataMember]
        public double calculatedRashod
        {
            get 
            {
                double rashod = 0;
                if (dat_uchet_pred != "")
                {
                    rashod = Convert.ToDouble((val_cnt - val_cnt_pred) * mmnog - ngp_cnt - ngp_lift);
                    if (rashod < 0)
                        rashod += Math.Pow(10, cnt_stage) * Convert.ToDouble(mmnog);
                }
                return rashod;
            }
            set { }
        }

        [DataMember]
        public int is_new { get; set; }
        
        public Counter()
            : base()
        {
            nzp_counter  = 0;
            nzp = 0;
            nzp_serv     = 0;
            nzp_cnttype  = 0;  
            nzp_type     = 0;  
            cnt_stage    = 0;
            name_type    = ""; 
            mmnog        = 0;
            num_cnt      = "";
            dat_prov     = "";
            dat_provnext = "";
            dat_oblom    = "";
            dat_poch     = "";
            dat_close    = "";
            comment      = "";

            dat_uchet    = "";
            val_cnt      = 0;
            val_cnt_s    = "";
            rashod       = "";
            plan_rashod = "";
            normativ = "";
            is_pl        = -1;
            cnt_ls       = 0;

            service      = "";
            cnt_type     = "";
            dat_uchet_pred = "";
            val_cnt_pred = 0;

            visible = 1;
            block = "";

            name_uchet = "";
            is_uchet_ls = 0;
            measure = "";
            ngp_cnt = 0;
            is_gkal = -1;
            ngp_lift = 0;
            is_doit = 0;
            is_actual = 0;
            nzp_cv = 0;
            prm = "";
            dat_when = "";

            sred_rashod = "";
            
            year_ = 0;
            month_ = 0;

            is_new = 0;

            dat_s = "";
            ngp_cnt_s = "";
            ngp_lift_s = "";

            show_ngp_lift = true;
            _cnt_state = 0;
        }
    }

    [DataContract]
    public class CounterVal : Counter
    {
        /// <summary>
        /// Источники поступления показания прибора учета
        /// </summary>
        public enum Ist
        {
            None = -1,      //значение не определено
            Operator = 9,   //показание вводится оператором
            File = 11,      //показание загружено из Самарского DBF файла
            Gilec = 175,    //показание введено жильцом через портал
            Bank = 12       //показания приборов учета, переданных вместе с оплатой от банка
        }

        /// <summary>
        /// Возвращает источник поступления показания прибора учета по коду
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Ist GetIstById(int id)
        {
            if (id == (int)Ist.File) return Ist.File;
            else if (id == (int)Ist.Operator) return Ist.Operator;
            else if (id == (int)Ist.Gilec) return Ist.Gilec;
            else return Ist.None;
        }

        [DataMember]
        public string dat_month { get; set; }
        [DataMember]
        public int ist { get; set; }
        [DataMember]
        public string ist_name { get; set; }
        [DataMember]
        public int nzp_account { get; set; }
        [DataMember]
        public bool is_editable { get; set; }
        [DataMember]
        public int nzp_group { get; set; }
        [DataMember]
        public string ngroup { get; set; }
        [DataMember]
        public string group_pref { get; set; }
        
        public CounterVal()
            : base()
        {
            dat_month = "";
            ist = 0;
            ist_name = "";
            nzp_account = 0;
            is_editable = false;
            nzp_group = 0;
            ngroup = "";
            group_pref = "";
        }
    }

    [DataContract]
    public class CounterOrd: Counter
    {
        [DataMember]
        public int nzp_ck { get; set; }
        [DataMember]
        public string dat_month { get; set; }
        [DataMember]
        public string formula { get; set; }
        [DataMember]
        public int order_num { get; set; }
        [DataMember]
        public int is_prov { get; set; }
        [DataMember]
        public string dat_vvod { get; set; }
        [DataMember]
        public int nzp_pack_ls { get; set; }
        [DataMember]
        public string dat_load { get; set; }
        [DataMember]
        public int nzp_account { get; set; }
        [DataMember]
        public string ist_name { get; set; }
        [DataMember]
        public int ist { get; set; }

        public CounterOrd(): base()
        {
            nzp_ck = 0;
            dat_month = "";
            formula = "";
            order_num = 0;
            is_prov = 0;
            dat_vvod = "";
            nzp_pack_ls = 0;
            dat_load = "";
            nzp_account = 0;
            ist_name = "";
            ist = 0;
            year_ = 0;
            month_ = 0;
        }
    }

    [DataContract]
    public class CounterCnttype : Finder, IEquatable<CounterCnttype>, IComparable<CounterCnttype>, IComparer<CounterCnttype>
    {
        [DataMember]
        public int nzp_cnttype { get; set; }

        [DataMember]
        public int cnt_stage { get; set; }

        [DataMember]
        public string name_type { get; set; }

        [DataMember]
        public decimal mmnog { get; set; }

        [DataMember]
        public string name { get; set; }

        public CounterCnttype() : base()
        {
            nzp_cnttype = 0;
            cnt_stage = 0;
            name_type = "";
            mmnog = 0;
            name = "";
        }

        public CounterCnttype(int _nzp_cnttype, string _name_type)
            : base()
        {
            nzp_cnttype = _nzp_cnttype;
            cnt_stage = 0;
            name_type = _name_type;
            mmnog = 0;
        }

        public bool Equals(CounterCnttype other)
        {
            if (this.nzp_cnttype == other.nzp_cnttype &
                this.cnt_stage == other.cnt_stage &
                this.name_type == other.name_type &
                this.mmnog == other.mmnog)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public int Compare(CounterCnttype x, CounterCnttype y)
        {
            if (x == null)
            {
                if (y == null) return 0; // x = y
                else return -1; // x < y
            }
            else
            {
                if (y == null) return 1; // x > y
                else
                {
                    if (x.cnt_stage > y.cnt_stage) return 1; // x > y
                    else if (x.cnt_stage < y.cnt_stage) return -1; // x < y
                    return (x.name_type.CompareTo(y.name_type));
                }
            }
        }

        public int CompareTo(CounterCnttype other)
        {
            return Compare(this, other);
        }
    }

    /// <summary>
    /// Показания приборов учета, загруженные из dbf файла (Самара)
    /// </summary>
    public class CounterValDbf
    { 
        public string predpr;
        public string geu;
        public string lc;
        public string llc;
        public string adr;
        public string usl;
        public string ns;
        public int rs;
        public string dold;
        public decimal? zold;
        public string dnew;
        public decimal? znew;
        public string message;
        public string don;
        public string nuk;
        public string ud;
        public int nzp_kvar;
        public string pref;
        public int nzp_counter;
        public string subpkod;
        /// <summary>
        /// причина, по которой показание не нагружено
        /// </summary>
        public string reason;

        public CounterValDbf()
        {
            predpr = "";
            geu = "";
            lc = "";
            llc = "";
            adr = "";
            usl = "";
            ns = "";
            rs = 0;
            dold = "";
            zold = null;
            dnew = "";
            znew = null;
            message = "";
            don = "";
            nuk = "";
            ud = "";
            nzp_kvar = 0;
            pref = "";
            nzp_counter = 0;
            reason = "";
        }

        public static string GetHeaderString()
        {
            return String.Format("{0,-3} {1,-3} {2,-5} {3, 1} {4,-60} {5,-50} {6,-6} {7,-3} {8, -11} {9,-16} {10,-10} {11,-16} {12,-10} {13,-80} {14,-8} {15,-2} {16}",
                new object[] { "Р/с", "ЖЭУ", "ЛС", "", "Адрес", "УК", "Услуга", "№сч", "", "Дата пред.показ.", "Пред.показ", "Дата показания", "Показание", "Комментарий", "", "", "Причина" });
        }

        public override string ToString()
        {
            return String.Format("{0,-3} {1,-3} {2,-5} {3, 1} {4,-60} {5,-50} {6,-6} {7,-3} {8, -11} {9,-16} {10,-10} {11,-16} {12,-10} {13,-80} {14,-8} {15,-2} {16}",
                new object[] { predpr, geu, lc, llc, adr, nuk, usl, ns, rs, dold, zold, dnew, znew, message, don, ud, reason });
        }
    }
}

