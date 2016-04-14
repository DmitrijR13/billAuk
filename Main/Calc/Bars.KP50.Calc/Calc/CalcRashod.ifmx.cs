using System.Data.Common;
using System.Dynamic;
using System.IO;
using Bars.KP50.Utils;
using Globals.SOURCE.Utility;

#region Подсчет расходов

#region Подключаемые модули

using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections.Generic;
using System.Globalization;

using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;

#endregion Подключаемые модули

#region здесь производится подсчет расходов
namespace STCLINE.KP50.DataBase
{

    //здась находятся классы для подсчета расходов
    public partial class DbCalcCharge : DataBaseHead
    {
        #region Функции и структуры для подсчета расходов
        // статус результата после выполнения DbCalcCharge
        public FonTask.Statuses status { get; set; }

        #region Функция для тестового вызова расчета расходов
        //--------------------------------------------------------------------------------
        public void TestCalcRashod(int nzp_dom, string pref, int yy, int mm, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            string conn_kernel = Points.GetConnByPref(pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return;
            }

            //вообще, я думаю, что с nzp_dom = 0 надо запускать отдельно, например, в hostman!
            //string prefTest = "okt03"; 
            //int yyTest = 2011;
            //int mmTest = 09;
            //int nzp_domTest = 0;

            //CalcGilXX(conn_db, nzp_domTest, prefTest, yyTest, mmTest, out ret);
            //CalcRashod(conn_db, nzp_domTest, prefTest, yyTest, mmTest, yyTest, mmTest, out ret);

            //DbCalc.IsNowCalcCharge(0, "smr1", out ret);


            //CalcRashod(conn_db, 0, 0, "avia38", 2011, 8, 2011, 8, false, false, false, out ret);

            /*
            for (int i = 1; i < 12; i++)
            {
                CalcReportXX(conn_db, 0, "smr1", 2011, i, 2011, i, out ret);
                CalcReportXX(conn_db, 0, "smr2", 2011, i, 2011, i, out ret);
                CalcReportXX(conn_db, 0, "smr3", 2011, i, 2011, i, out ret);
            }
            */


            //CalcGilXX(conn_db, 0, pref, yy, mm, out ret);
            //CalcRashod(conn_db, 0, pref, yy, mm, out ret);

            conn_db.Close();
        }
        #endregion Функция для тестового вызова расчета расходов

        #region Структура Расход Rashod
        public struct Rashod
        {
            public CalcTypes.ParamCalc paramcalc;

            public string counters_xx;
            public string counters_tab;
            public string countlnk_xx;
            public string countlnk_tab;
            //public string cur_counters;
            //public string charge_xx;
            public string gil_xx;
            public string charge_xx;
            public string delta_xx_cur;
            public string reval_xx_cur;

            public string where_dom;
            public string where_kvar;
            public string where_kvarK;
            public string where_kvarA;


            public bool calcv; //считать виртуальный расходы
            public bool k307;
            public int nzp_type_alg;


            public Rashod(CalcTypes.ParamCalc _paramcalc)
            {
                paramcalc = _paramcalc;
                calcv = false;
                k307 = false;
                nzp_type_alg = 6;  //алгоритм учета ДПУ

                //cur_counters = paramcalc.pref + "_charge_" + (paramcalc.cur_yy - 2000).ToString("00") + ":counters_" + paramcalc.cur_mm.ToString("00");
                //charge_xx    = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + ":charge_" + paramcalc.calc_mm.ToString("00");

                counters_tab = "counters" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                counters_xx = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") +
                    tableDelimiter + counters_tab;

                countlnk_tab = "countlnk" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                countlnk_xx = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") +
                    tableDelimiter + countlnk_tab;

                gil_xx = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") +
                    tableDelimiter + "gil" +
                    paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");

                charge_xx = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") +
                    tableDelimiter + "charge" +
                    paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");

                delta_xx_cur = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") +
                    tableDelimiter + "delta_" +
                    paramcalc.calc_mm.ToString("00");

                reval_xx_cur = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") +
                    tableDelimiter + "reval_" +
                    paramcalc.calc_mm.ToString("00");

                string s = "";
                if (paramcalc.b_reval)
                    s = " in ( Select nzp_dom From t_selkvar) ";
                else
                {
                    if (paramcalc.nzp_dom > 0 || paramcalc.list_dom)
                        //s = " = " + paramcalc.nzp_dom;
                        s = " in ( Select nzp_dom From t_selkvar) ";
                    else
                        s = " > 0 ";
                }

                where_dom = "nzp_dom " + s;


                where_kvar = "";
                where_kvarK = "";
                where_kvarA = "";

                if (paramcalc.nzp_kvar > 0)
                {
                    where_kvar = " and nzp_kvar   = " + paramcalc.nzp_kvar;
                    where_kvarK = " and k.nzp_kvar = " + paramcalc.nzp_kvar;
                    where_kvarA = " and a.nzp_kvar = " + paramcalc.nzp_kvar;
                }
            }
        }

        #endregion Структура Расход

        #region Структура Расход Rashod2
        public struct Rashod2
        {
            public string tab;
            public string dat_s;
            public string dat_po;
            public string p_TAB;
            public string p_KEY;
            public string p_INSERT;
            public string p_ACTUAL;

            public string counters_xx;

            public string pref;
            public string p_where;
            public string p_type;
            public string p_FROM;
            public string p_FROM_tmp;
            public string p_UPDdt_s;
            public string p_UPDdt_po;

            public CalcTypes.ParamCalc paramcalc;

            public Rashod2(string _counters_xx, CalcTypes.ParamCalc _paramcalc)
            {
                paramcalc = _paramcalc;
                counters_xx = _counters_xx;
                tab = "";
                dat_s = "";
                dat_po = "";
                p_TAB = "";
                p_KEY = "";
                p_INSERT = "";
                p_ACTUAL = "";
                pref = "";
                p_where = "";
                p_type = "";
                p_FROM = "";
                p_FROM_tmp = "";
                p_UPDdt_s = "";
                p_UPDdt_po = "";
            }
        }

        #endregion Структура Расход Rashod2

        #region Структура Call87 учитывать или не учитывать коммуналки
        struct Call87
        {
            public string mmnog0;
            public bool cmnlc; //учитывать или не учитывать коммуналки

            public int nzp_serv;
            public string and_serv;
            public string norma;

            public Call87(bool _cmnlc, int _nzp_serv, string _norma)
            {
                cmnlc = _cmnlc;
                nzp_serv = _nzp_serv;
                norma = _norma;

                if (cmnlc)
                    mmnog0 = " "; //учитывать коммуналки
                else
                    mmnog0 = " and mmnog = 0 "; //не учитывать коммуналки

                if (nzp_serv == 25)
                    and_serv = " and nzp_serv in ( 25,210 ) ";
                else
                    and_serv = " and nzp_serv in (" + nzp_serv + ") ";

            }
        }
        #endregion Структура Call87 учитывать или не учитывать коммуналки

        #region Функция создания таблиц - CountersXX и CountlnkXX для текущего или перерасчетного месяца
        //--------------------------------------------------------------------------------
        void CreateCountersXX(IDbConnection conn_db2, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            bool bNotExistCountersXX = true;
            if (TempTableInWebCashe(conn_db2, rashod.counters_xx))
            {
                string p_where = " Where 1 = 1 and stek<>333 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge;

                if (rashod.paramcalc.b_reval) //перерасчет сразу по дому!!!
                    p_where = "  Where 0 < ( Select count(*) From t_selkvar b " +
                                            " Where b.nzp_dom = " + rashod.counters_xx + ".nzp_dom ) " +
                                "  and stek<>333 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge;

                string sql =
                            " Delete From " + rashod.counters_xx +
                    //" Where 1 = 1 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge
                                p_where;

                if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db2, sql, true);
                }
                else
                {
                    ExecByStep(conn_db2, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                }

                if (!ret.result) { conn_db2.Close(); return; }

                UpdateStatistics(false, rashod.paramcalc, rashod.counters_tab, out ret);

                bNotExistCountersXX = false;
            }

            bool bNotExistCountlnkXX = true;
            if (TempTableInWebCashe(conn_db2, rashod.countlnk_xx))
            {
                string p_where = " Where 1 = 1 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge;

                if (rashod.paramcalc.b_reval) //перерасчет сразу по дому!!!
                    p_where = "  Where 0 < ( Select count(*) From t_selkvar b " +
                                            " Where b.nzp_dom = " + rashod.countlnk_xx + ".nzp_dom ) " +
                                "  and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge;

                string sql =
                            " Delete From " + rashod.countlnk_xx +
                                p_where;

                ret = ExecSQL(conn_db2, sql, true);
                if (!ret.result) { conn_db2.Close(); return; }

                UpdateStatistics(false, rashod.paramcalc, rashod.countlnk_xx, out ret);

                bNotExistCountlnkXX = false;
            }

            if (!bNotExistCountersXX && !bNotExistCountlnkXX)
            {
                return;
            }
            string conn_kernel = Points.GetConnByPref(rashod.paramcalc.pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            //IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return;
            }
            ret = ExecSQL(conn_db,
#if PG
 " set search_path to '" + rashod.paramcalc.pref + "_charge_" + (rashod.paramcalc.calc_yy - 2000).ToString("00") + "'"
#else
                " Database " + rashod.paramcalc.pref + "_charge_" + (rashod.paramcalc.calc_yy - 2000).ToString("00")
#endif
, true);

            if (bNotExistCountersXX)
            {
                ret = ExecSQL(conn_db,
                    " Create table " + tbluser + rashod.counters_tab +
                      " (  nzp_cntx    serial        not null, " +
                      "    nzp_dom     integer       not null, " +
                      "    nzp_kvar    integer       default 0 not null , " +
                      "    nzp_type    integer       not null, " +               //1,2,3
                      "    nzp_serv    integer       not null, " +
                      "    dat_charge  date, " +
                      "    cur_zap     integer       default 0 not null, " +     //0-текущее значение, >0 - ссылка на следующее значение (nzp_cntx)
                      "    prev_zap    integer       default 0 not null, " +     //0-текущее значение, >0 - ссылка на предыдыущее значение (nzp_cntx)
                      "    nzp_counter integer       default 0 , " +             //счетчик или вариатны расходов для stek=3
                    //-1 - строка с текущим расходами из charge_xx (при перерасчете)
                    //признаки в нормативном стеке (stek=3)
                    //-2 - лс, где нет никаких ПУ по услуге
                    //-3 - есть показания ПУ в текущем месяце
                      "    cnt_stage   integer       default 0 , " +             //разрядность
                      "    mmnog       " + sDecimalType + "(15,7) default 1.00 , " +             //масшт. множитель
                      "    stek        integer       default 0 not null, " +     //3-итого по лс,дому; 1-счетчик; 2,3,4,5 - стек расходов
                    //>100 - формула распределения (посчитанные дельты)
                      "    rashod      " + sDecimalType + "(15,7) default 0.00 not null, " +  //общий расход в зависимости от stek
                      "    dat_s       date          not null, " +               //дата с
                      "    val_s       " + sDecimalType + "(15,7) default 0.00 not null, " +  //значение (а также коэф-т)
                      "    dat_po      date not null, " +                        //дата по
                      "    val_po      " + sDecimalType + "(15,7) default 0.00 not null, " +  //значение
                      "    ngp_cnt       " + sDecimalType + "(14,7) default 0.0000000, " +  // расход на нежилые помещения
                      "    rash_norm_one " + sDecimalType + "(14,7) default 0.0000000, " +  // норматив на 1 человека
                      "    val1_g      " + sDecimalType + "(15,7) default 0.00 not null, " +  //расход по счетчику nzp_counter или нормативные расходы в расчетном месяце без учета вр.выбывших
                      "    val1        " + sDecimalType + "(15,7) default 0.00 not null, " +  //расход по счетчику nzp_counter или нормативные расходы в расчетном месяце
                      "    val2        " + sDecimalType + "(15,7) default 0.00 not null, " +  //дом: расход КПУ
                      "    val3        " + sDecimalType + "(15,7) default 0.00         , " +  //дом: расход нормативщики
                      "    val4        " + sDecimalType + "(15,7) default 0.00         , " +  //общий расход по счетчику nzp_counter
                      "    rvirt       " + sDecimalType + "(15,7) default 0.00         , " +  //вирт. расход
                      "    squ1        " + sDecimalType + "(15,7) default 0.00         , " +  //площадь лс, дома (по всем лс)
                      "    squ2        " + sDecimalType + "(15,7) default 0.00         , " +  //площадь лс без КПУ (для домовых строк)
                      "    cls1        integer       default 0 not null   , " +  //количество лс дома по услуге
                      "    cls2        integer       default 0 not null   , " +  //количество лс без КПУ (для домовых строк)
                      "    gil1_g      " + sDecimalType + "(15,7) default 0.00         , " +  //кол-во жильцов в лс без учета вр.выбывших
                      "    gil1        " + sDecimalType + "(15,7) default 0.00         , " +  //кол-во жильцов в лс
                      "    gil2        " + sDecimalType + "(15,7) default 0.00         , " +  //кол-во жильцов в лс
                      "    cnt1_g      integer       default 0 not null, " +     //кол-во жильцов в лс (нормативное) без учета вр.выбывших
                      "    cnt1        integer       default 0 not null, " +     //кол-во жильцов в лс (нормативное)
                      "    cnt2        integer       default 0 not null, " +     //кол-во комнат в лс
                      "    cnt3        integer       default 0, " +              //тип норматива в зависимости от услуги (ссылка на resolution.nzp_res)
                      "    cnt4        integer       default 0, " +              //1-дом не-МКД (0-МКД)
                      "    cnt5        integer       default 0, " +              //резерв
                      "    dop87       " + sDecimalType + "(15,7) default 0.00         , " +  //доп.значение 87 постановления (7кВт или добавок к нормативу  (87 П) )
                      "    pu7kw       " + sDecimalType + "(15,7) default 0.00         , " +  //7 кВт для КПУ (откорректированный множитель)
                      "    gl7kw       " + sDecimalType + "(15,7) default 0.00         , " +  //7 кВт КПУ * gil1 (учитывая корректировку)
                      "    vl210       " + sDecimalType + "(15,7) default 0.00         , " +  //расход 210 для nzp_type = 6
                      "    kf307       " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент 307 для КПУ или коэфициент 87 для нормативщиков
                      "    kf307n      " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент 307 для нормативщиков
                      "    kf307f9     " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент 307 по формуле 9
                      "    kf_dpu_kg   " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент ДПУ для распределения пропорционально кол-ву жильцов
                      "    kf_dpu_plob " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент ДПУ для распределения пропорционально сумме общих площадей
                      "    kf_dpu_plot " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент ДПУ для распределения пропорционально сумме отапливаемых площадей
                      "    kf_dpu_ls   " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент ДПУ для распределения пропорционально кол-ву л/с
                      "    dlt_in      " + sDecimalType + "(15,7) default 0.00         , " +  //входящии нераспределенный расход (остаток)
                      "    dlt_cur     " + sDecimalType + "(15,7) default 0.00         , " +  //текущая дельта
                      "    dlt_reval   " + sDecimalType + "(15,7) default 0.00         , " +  //перерасчет дельты за прошлые месяцы
                      "    dlt_real_charge " + sDecimalType + "(15,7) default 0.00     , " +  //перерасчет дельты за прошлые месяцы
                      "    dlt_calc    " + sDecimalType + "(15,7) default 0.00         , " +  //распределенный (учтенный) расход
                      "    dlt_out     " + sDecimalType + "(15,7) default 0.00         , " +  //исходящии нераспределенный расход (остаток)
                      "    kod_info    integer default 0                               , " +  //выбранный способ учета (=1)
                      "    norm_type_id integer  default 0                             , " +  //id типа норматива - для нового режима ввода нормативов
                      "    norm_tables_id integer  default 0                           , " +  //id норматива - по нему можно получить набор влияющих пар-в и их знач.
                      "    val1_source " + sDecimalType + "(15,7) default 0.00 not null, " +  //val1 без учета повышающего коэффициента
                      "    val4_source " + sDecimalType + "(15,7) default 0.00 not null, " +  //val4 без учета повышающего коэффициента
                      "    dop87_source " + sDecimalType + "(15,7) default 0.00 not null, " +  //dop87 без учета повышающего коэффициента
                      "    up_kf " + sDecimalType + "(15,7) default 1.00 not null) "          //повышающий коэффициент для нормативного расхода
                          , true);
                if (!ret.result) { conn_db.Close(); return; }

                ret = ExecSQL(conn_db, " create unique index " + tbluser + "ix1_" + rashod.counters_tab + " on " + rashod.counters_tab + " (nzp_cntx) ", true);
                if (!ret.result) { conn_db.Close(); return; }
                ret = ExecSQL(conn_db, " create index " + tbluser + "ix2_" + rashod.counters_tab + " on " + rashod.counters_tab + " (nzp_type,nzp_dom,nzp_serv,dat_charge,stek,kod_info) ", true);
                if (!ret.result) { conn_db.Close(); return; }
                ret = ExecSQL(conn_db, " create index " + tbluser + "ix3_" + rashod.counters_tab + " on " + rashod.counters_tab + " (nzp_type,nzp_kvar,nzp_serv,stek) ", true);
                if (!ret.result) { conn_db.Close(); return; }
                ret = ExecSQL(conn_db, " create index " + tbluser + "ix4_" + rashod.counters_tab + " on " + rashod.counters_tab + " (nzp_counter) ", true);
                if (!ret.result) { conn_db.Close(); return; }
                ret = ExecSQL(conn_db, " create index " + tbluser + "ix5_" + rashod.counters_tab + " on " + rashod.counters_tab + " (nzp_dom,dat_charge) ", true);
                if (!ret.result) { conn_db.Close(); return; }
                ret = ExecSQL(conn_db, " create index " + tbluser + "ix6_" + rashod.counters_tab + " on " + rashod.counters_tab + " (nzp_kvar,dat_charge,kod_info) ", true);
                if (!ret.result) { conn_db.Close(); return; }
                ret = ExecSQL(conn_db, " create index " + tbluser + "ix7_" + rashod.counters_tab + " on " + rashod.counters_tab + " (nzp_type,stek,nzp_dom,dat_charge) ", true);
                if (!ret.result) { conn_db.Close(); return; }
                ret = ExecSQL(conn_db, " create index " + tbluser + "ix8_" + rashod.counters_tab + " on " + rashod.counters_tab + " (nzp_kvar,nzp_serv) ", true);
                if (!ret.result) { conn_db.Close(); return; }
                ret = ExecSQL(conn_db, " create index " + tbluser + "ix9_" + rashod.counters_tab + " on " + rashod.counters_tab + " (cur_zap) ", true);
                if (!ret.result) { conn_db.Close(); return; }
                ret = ExecSQL(conn_db, " create index " + tbluser + "ix10_" + rashod.counters_tab + " on " + rashod.counters_tab + " (prev_zap) ", true);
                if (!ret.result) { conn_db.Close(); return; }

                ExecSQL(conn_db, sUpdStat + " " + rashod.counters_tab, true);
            }

            if (bNotExistCountlnkXX)
            {
                ret = ExecSQL(conn_db,
                " Create table " + tbluser + rashod.countlnk_tab +
                  " (  nzp_cntl   serial  not null, " +
                  "    nzp_dom    integer not null, " +
                  "    nzp_kvar   integer default 0 not null , " +
                  "    nzp_type   integer not null, " +
                  "    nzp_serv   integer not null, " +
                  "    dat_charge date,   " +
                  "    cur_zap    integer default 0 not null, " +
                  "    stek       integer default 0 not null, " +
                  "    kod_info   integer default 0 not null, " +
                    // для 1й связанной услуги
                  "    nzp_serv_l1   integer default 0 not null, " +
                  "    val1_l1       " + sDecimalType + "(15,7) default 0.00, " +
                  "    val2_l1       " + sDecimalType + "(15,7) default 0.00, " +
                  "    squ1_l1       " + sDecimalType + "(15,7) default 0.00, " +
                  "    squ2_l1       " + sDecimalType + "(15,7) default 0.00, " +
                  "    cls1_l1       integer default 0 not null, " +
                  "    cls2_l1       integer default 0 not null, " +
                  "    gil1_l1       " + sDecimalType + "(15,7) default 0.00, " +
                  "    gil2_l1       " + sDecimalType + "(15,7) default 0.00, " +
                  "    dlt_reval_l1  " + sDecimalType + "(15,7) default 0.00, " +
                  "    dlt_real_charge_l1 " + sDecimalType + "(15,7) default 0.00, " +
                    // для 2й связанной услуги
                  "    nzp_serv_l2   integer default 0 not null, " +
                  "    val1_l2       " + sDecimalType + "(15,7) default 0.00, " +
                  "    val2_l2       " + sDecimalType + "(15,7) default 0.00, " +
                  "    squ1_l2       " + sDecimalType + "(15,7) default 0.00, " +
                  "    squ2_l2       " + sDecimalType + "(15,7) default 0.00, " +
                  "    cls1_l2       integer default 0 not null, " +
                  "    cls2_l2       integer default 0 not null, " +
                  "    gil1_l2       " + sDecimalType + "(15,7) default 0.00, " +
                  "    gil2_l2       " + sDecimalType + "(15,7) default 0.00, " +
                  "    dlt_reval_l2  " + sDecimalType + "(15,7) default 0.00, " +
                  "    dlt_real_charge_l2 " + sDecimalType + "(15,7) default 0.00  " +
                  "    ) "
                      , true);
                if (!ret.result) { conn_db.Close(); return; }

                ret = ExecSQL(conn_db, " create unique index " + tbluser + "ix1_" + rashod.countlnk_tab + " on " + rashod.countlnk_tab + " (nzp_cntl) ", true);
                if (!ret.result) { conn_db.Close(); return; }
                ret = ExecSQL(conn_db, " create index " + tbluser + "ix2_" + rashod.countlnk_tab + " on " + rashod.countlnk_tab + " (nzp_dom,nzp_serv,stek,kod_info) ", true);
                if (!ret.result) { conn_db.Close(); return; }
                ret = ExecSQL(conn_db, " create index " + tbluser + "ix3_" + rashod.countlnk_tab + " on " + rashod.countlnk_tab + " (nzp_serv_l1) ", true);
                if (!ret.result) { conn_db.Close(); return; }
                ret = ExecSQL(conn_db, " create index " + tbluser + "ix4_" + rashod.countlnk_tab + " on " + rashod.countlnk_tab + " (nzp_serv_l2) ", true);
                if (!ret.result) { conn_db.Close(); return; }
                ret = ExecSQL(conn_db, " create index " + tbluser + "ix5_" + rashod.countlnk_tab + " on " + rashod.countlnk_tab + " (cur_zap) ", true);
                if (!ret.result) { conn_db.Close(); return; }

                ExecSQL(conn_db, sUpdStat + " " + rashod.countlnk_tab, true);
            }

            conn_db.Close();
        }

        #endregion Функция создания таблиц - CountersXX и CountlnkXX для текущего или перерасчетного месяца

        #region Удаление  временных таблиц DropTempTablesRahod
        //--------------------------------------------------------------------------------
        void DropTempTablesRahod(IDbConnection conn_db, string pref)
        //--------------------------------------------------------------------------------
        {
            //препарированные таблицы
            //ExecSQL(conn_db, " Drop table temp_counters_dom", false);
            //ExecSQL(conn_db, " Drop table temp_counters", false);
            //ExecSQL(conn_db, " Drop table ttt_prm_1f ", false);
            //ExecSQL(conn_db, " Drop table aid_i" + pref, false);

            //ExecSQL(conn_db, " Drop table temp_table_tarif ", false);
            //ExecSQL(conn_db, " Drop table ttt_prm_1 ", false);
            //ExecSQL(conn_db, " Drop table ttt_prm_2 ", false);
            //ExecSQL(conn_db, " Drop table ttt_mustcalc ", false);

            ExecSQL(conn_db, " Drop table tpok ", false);
            ExecSQL(conn_db, " Drop table tpok_s ", false);
            ExecSQL(conn_db, " Drop table tpok_po ", false);
            ExecSQL(conn_db, " Drop table ta_mr ", false);
            ExecSQL(conn_db, " Drop table ta_br ", false);
            ExecSQL(conn_db, " Drop table ta_b ", false);
            ExecSQL(conn_db, " Drop table tb_b ", false);
            ExecSQL(conn_db, " Drop table tb_br ", false);
            ExecSQL(conn_db, " Drop table tb_mr ", false);
            ExecSQL(conn_db, " Drop table t_inscnt ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_sq ", false);
            ExecSQL(conn_db, " Drop table ttt_counters_xx ", false);
            ExecSQL(conn_db, " Drop table ttt_counters_ipu ", false);
            ExecSQL(conn_db, " Drop table ttt_cnt_uni ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_norma ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_kpu ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_dpu ", false);
            ExecSQL(conn_db, " Drop table ttt_ans_kpu ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_sred ", false);
            ExecSQL(conn_db, " Drop table ttt_calc_dpu ", false);
            ExecSQL(conn_db, " Drop table ttt_ans_rsh69 ", false);
            ExecSQL(conn_db, " Drop table ttt_ans_rsh7 ", false);
            ExecSQL(conn_db, " Drop table cnt_d ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_a ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_b ", false);
            ExecSQL(conn_db, " Drop table ttt_ans_ee210 ", false);

            ExecSQL(conn_db, " drop table t_etag1999 ", false);
            ExecSQL(conn_db, " drop table t_etag ", false);
            ExecSQL(conn_db, " drop table t_tmpr ", false);
            ExecSQL(conn_db, " drop table t_norm_otopl ", false);
            ExecSQL(conn_db, " drop table t_otopl_m2 ", false);
            ExecSQL(conn_db, " Drop table t_ans_kommunal ", false);
            ExecSQL(conn_db, " Drop table t_ans_itog ", false);
            ExecSQL(conn_db, " Drop table ttt_ans_delta", false);
            ExecSQL(conn_db, " Drop table ttt_dlt_dom", false);
            ExecSQL(conn_db, " Drop table t_norm_eeot", false);
            ExecSQL(conn_db, " Drop table t_norm_gasot", false);
            ExecSQL(conn_db, " Drop table t_norm_gas", false);
            ExecSQL(conn_db, " Drop table t_is339 ", false);
            ExecSQL(conn_db, " Drop table ttt_counters_common_kvar ", false);

        }
        #endregion Удаление  временных таблиц DropTempTablesRahod

        #region Функция заполнения других временных таблиц LoadTempTableOther
        //--------------------------------------------------------------------------------
        public void LoadTempTableOther(IDbConnection conn_db, ref CalcTypes.ParamCalc paramcalc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            string s = "  Where 1 = 1 ";
            string ss = "  Where 1 = 1 ";
            string sss = "  Where 1 = 1 ";
            string ssss = "  Where 1 = 1 ";
            if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0 || paramcalc.list_dom)
            {
                s = " ,t_selkvar b Where a.nzp_kvar = b.nzp_kvar ";
                ss = " ,t_selkvar b Where a.nzp = b.nzp_kvar ";
                sss = " Where exists (select 1 from t_selkvar b Where a.nzp = b.nzp_dom) ";
                //" Where 0<(select count(*) from t_selkvar b Where a.nzp = b.nzp_dom) ";
                ssss = " Where exists (select 1 from t_selkvar b ," + paramcalc.data_alias + "counters_link l" +
                    //" Where 0<(select count(*) from t_selkvar b ," + paramcalc.data_alias + "counters_link l" +
                                " Where l.nzp_counter=a.nzp_counter and l.nzp_kvar = b.nzp_kvar) ";
            }

            //nedop_kvar - недопоставки по ЛС
            ExecSQL(conn_db, " Drop table ttc_nedo ", false);

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE ttc_nedo (" +
                "   nzp_nedop serial NOT NULL," +
                "   nzp_kvar  integer," +
                "   nzp_serv  integer," +
                "   nzp_supp  integer," +
                "   dat_s  " + sDateTimeType + "," +
                "   dat_po " + sDateTimeType + "," +
                "   tn     character(20)," +
                "   nzp_kind   integer," +
                "   month_calc date" +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " insert into ttc_nedo (nzp_nedop,nzp_kvar,nzp_serv,nzp_supp,dat_s,dat_po,tn,nzp_kind,month_calc)" +
                " Select a.nzp_nedop, a.nzp_kvar, a.nzp_serv, a.nzp_supp, " +
                sNvlWord + "(a.dat_s, " + MDY(1, 1, 1901) + ") as dat_s ," +
                sNvlWord + "(a.dat_po," + MDY(1, 1, 3000) + ") as dat_po, a.tn, a.nzp_kind, a.month_calc " +
                " From " + paramcalc.data_alias + "nedop_kvar a " +
                s + " and a.is_actual <> 100 "
                , true);
            if (!ret.result) { return; }

            ExecSQL(conn_db, " Create unique index ix1_ttc_nedo on ttc_nedo (nzp_nedop) ", true);
            ExecSQL(conn_db, " Create index ix3_ttc_nedo on ttc_nedo (nzp_kvar, nzp_serv, nzp_supp, nzp_kind, tn ) ", true);
            ExecSQL(conn_db, " Create index ix4_ttc_nedo on ttc_nedo (nzp_kvar, dat_s, dat_po) ", true);
            ExecSQL(conn_db, " Create index ix5_ttc_nedo on ttc_nedo (dat_s, dat_po) ", true);
            ExecSQL(conn_db, sUpdStat + " ttc_nedo ", true);


            //tarif - действующие услуги по ЛС
            ExecSQL(conn_db, " Drop table ttc_tarif ", false);

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE ttc_tarif (" +
                "   nzp_tarif serial NOT NULL," +
                "   nzp_kvar  integer," +
                "   nzp_serv  integer," +
                "   nzp_supp  integer," +
                "   nzp_frm   integer," +
                "   dat_s      date," +
                "   dat_po     date," +
                "   month_calc date" +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " insert into ttc_tarif (nzp_tarif,nzp_kvar,nzp_serv,nzp_supp,nzp_frm,dat_s,dat_po,month_calc) " +
                " Select a.nzp_tarif, a.nzp_kvar, a.nzp_serv, a.nzp_supp, a.nzp_frm, " +
                sNvlWord + "(a.dat_s, " + MDY(1, 1, 1901) + ") as dat_s , " +
                sNvlWord + "(a.dat_po, " + MDY(1, 1, 3000) + " ) as dat_po, a.month_calc " +
                " From " + paramcalc.data_alias + "tarif a " +
                s + " and a.is_actual <> 100 "
                , true);
            if (!ret.result) { return; }

            ExecSQL(conn_db, " Create unique index ix1_ttc_tarif on ttc_tarif (nzp_tarif) ", true);
            ExecSQL(conn_db, " Create index ix3_ttc_tarif on ttc_tarif (nzp_kvar, nzp_serv, nzp_supp, nzp_frm ) ", true);
            ExecSQL(conn_db, " Create index ix4_ttc_tarif on ttc_tarif (nzp_kvar, dat_s, dat_po) ", true);
            ExecSQL(conn_db, " Create index ix5_ttc_tarif on ttc_tarif (dat_s, dat_po) ", true);
            ExecSQL(conn_db, sUpdStat + " ttc_tarif ", true);


            //prm_1 - параметры ЛС
            string sp;
            if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0 || paramcalc.list_dom)
                sp = "  ,t_selkvar b Where a.nzp = b.nzp_kvar ";
            else
                sp = "  Where 1 = 1 ";

            ExecSQL(conn_db, " Drop table ttt_prm_1f ", false);

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE ttt_prm_1f (" +
                "   nzp_key serial NOT NULL," +
                "   nzp     integer," +
                "   nzp_prm integer," +
                "   val_prm character(20)," +
                "   dat_s      date," +
                "   dat_po     date," +
                "   month_calc date" +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " insert into ttt_prm_1f (nzp_key,nzp,nzp_prm,val_prm,dat_s,dat_po,month_calc) " +
                " Select a.nzp_key, a.nzp, a.nzp_prm," +
                " replace( " + sNvlWord + "(a.val_prm,'0'), ',', '.') val_prm, " +
                sNvlWord + "(a.dat_s, " + MDY(1, 1, 1901) + ") as dat_s , " +
                sNvlWord + "(a.dat_po, " + MDY(1, 1, 3000) + ") as dat_po, a.month_calc " +
                " From " + paramcalc.data_alias + "prm_1 a " +
                sp + " and a.is_actual <> 100 "
                , true);
            if (!ret.result) { return; }

            ExecSQL(conn_db, " Create unique index ix1_ttt_prm_1f on ttt_prm_1f (nzp_key) ", true);
            ExecSQL(conn_db, " Create index ix2_ttt_prm_1f on ttt_prm_1f (nzp,dat_s,dat_po) ", true);
            ExecSQL(conn_db, " Create index ix3_ttt_prm_1f on ttt_prm_1f (dat_s,dat_po) ", true);

            ExecSQL(conn_db, sUpdStat + " ttt_prm_1f ", true);

            //prm_2 - параметры домов
            if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0 || paramcalc.list_dom)
                sp = "  ,t_selkvar b Where a.nzp = b.nzp_dom ";
            else
                sp = "  Where 1 = 1 ";

            ExecSQL(conn_db, " Drop table ttt_prm_2f ", false);

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE ttt_prm_2f (" +
                "   nzp_key serial NOT NULL," +
                "   nzp     integer," +
                "   nzp_prm integer," +
                "   val_prm character(20)," +
                "   dat_s      date," +
                "   dat_po     date," +
                "   month_calc date" +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " insert into ttt_prm_2f (nzp_key,nzp,nzp_prm,val_prm,dat_s,dat_po,month_calc) " +
                " Select " + sUniqueWord +
                " a.nzp_key, a.nzp, a.nzp_prm," +
                " replace(" + sNvlWord + "(a.val_prm,'0'), ',', '.') val_prm, " +
                sNvlWord + "(a.dat_s, " + MDY(1, 1, 1901) + ") as dat_s , " +
                sNvlWord + "(a.dat_po, " + MDY(1, 1, 3000) + " ) as dat_po, a.month_calc " +
                " From " + paramcalc.data_alias + "prm_2 a " +
                sp + " and a.is_actual <> 100 "
                , true);
            if (!ret.result) { return; }

            ExecSQL(conn_db, " Create unique index ix1_ttt_prm_2f on ttt_prm_2f (nzp_key) ", true);
            ExecSQL(conn_db, " Create index ix2_ttt_prm_2f on ttt_prm_2f (nzp,dat_s,dat_po) ", true);
            ExecSQL(conn_db, " Create index ix3_ttt_prm_2f on ttt_prm_2f (dat_s,dat_po) ", true);

            ExecSQL(conn_db, sUpdStat + " ttt_prm_2f ", true);

            //counters_spis - описатели ПУ
            ExecSQL(conn_db, " Drop table temp_cnt_spis_f", false);

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE temp_cnt_spis_f (" +
                " nzp_counter  serial NOT NULL," +
                " nzp_type     integer NOT NULL," +
                " nzp          integer NOT NULL," +
                " nzp_serv     integer NOT NULL," +
                " nzp_cnttype  integer NOT NULL," +
                " num_cnt      character(40)," +
                " kod_pu       integer DEFAULT 0," +
                " kod_info     integer DEFAULT 0," +
                " dat_prov     date," +
                " dat_provnext date," +
                " dat_oblom    date," +
                " dat_poch     date," +
                " dat_close    date," +
                " comment      character(60)," +
                " nzp_cnt      integer," +
                " is_gkal      integer NOT NULL DEFAULT 0," +
                " is_actual    integer," +
                " nzp_user     integer," +
                " dat_when     date," +
                " is_pl        integer DEFAULT 0," +
                " cnt_ls       integer DEFAULT 0," +
                " dat_block    " + sDateTimeType + "," +
                " user_block   integer," +
                " month_calc   date," +
                " user_del     integer," +
                " dat_del      date," +
                " dat_s        date," +
                " dat_po       date" +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " insert into temp_cnt_spis_f (" +
                " nzp_counter,nzp_type,nzp,nzp_serv,nzp_cnttype,num_cnt," +
                " kod_pu,kod_info,dat_prov,dat_provnext,dat_oblom,dat_poch,dat_close," +
                " comment,nzp_cnt,is_actual,nzp_user,dat_when,is_pl,cnt_ls," +
                " dat_block,user_block,month_calc,user_del,dat_del,dat_s,dat_po" +
                " )" +
                " Select " +
                " a.nzp_counter,a.nzp_type,a.nzp,a.nzp_serv,a.nzp_cnttype,a.num_cnt," +
                " a.kod_pu,a.kod_info,a.dat_prov,a.dat_provnext,a.dat_oblom,a.dat_poch,a.dat_close," +
                " a.comment,a.nzp_cnt,a.is_actual,a.nzp_user,a.dat_when,a.is_pl,a.cnt_ls," +
                " a.dat_block,a.user_block,a.month_calc,a.user_del,a.dat_del,a.dat_s,a.dat_po" +
                " From " + paramcalc.data_alias + "counters_spis a " +
                ss + " and a.nzp_type=3 and a.is_actual <> 100 "
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " insert into temp_cnt_spis_f (" +
                " nzp_counter,nzp_type,nzp,nzp_serv,nzp_cnttype,num_cnt," +
                " kod_pu,kod_info,dat_prov,dat_provnext,dat_oblom,dat_poch,dat_close," +
                " comment,nzp_cnt,nzp_user,dat_when,is_pl,cnt_ls," +
                " dat_block,user_block,month_calc,user_del,dat_del,dat_s,dat_po" +
                " )" +
                " Select " +
                " a.nzp_counter,a.nzp_type,a.nzp,a.nzp_serv,a.nzp_cnttype,a.num_cnt," +
                " a.kod_pu,a.kod_info,a.dat_prov,a.dat_provnext,a.dat_oblom,a.dat_poch,a.dat_close," +
                " a.comment,a.nzp_cnt,a.nzp_user,a.dat_when,a.is_pl,a.cnt_ls," +
                " a.dat_block,a.user_block,a.month_calc,a.user_del,a.dat_del,a.dat_s,a.dat_po" +
                " From " + paramcalc.data_alias + "counters_spis a " +
                sss + " and a.nzp_type=1 and a.is_actual <> 100 "
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " insert into temp_cnt_spis_f (" +
                " nzp_counter,nzp_type,nzp,nzp_serv,nzp_cnttype,num_cnt," +
                " kod_pu,kod_info,dat_prov,dat_provnext,dat_oblom,dat_poch,dat_close," +
                " comment,nzp_cnt,nzp_user,dat_when,is_pl,cnt_ls," +
                " dat_block,user_block,month_calc,user_del,dat_del,dat_s,dat_po" +
                " )" +
                " Select " +
                " a.nzp_counter,a.nzp_type,a.nzp,a.nzp_serv,a.nzp_cnttype,a.num_cnt," +
                " a.kod_pu,a.kod_info,a.dat_prov,a.dat_provnext,a.dat_oblom,a.dat_poch,a.dat_close," +
                " a.comment,a.nzp_cnt,a.nzp_user,a.dat_when,a.is_pl,a.cnt_ls," +
                " a.dat_block,a.user_block,a.month_calc,a.user_del,a.dat_del,a.dat_s,a.dat_po" +
                " From " + paramcalc.data_alias + "counters_spis a " +
                ssss + " and a.nzp_type in (2,4) and a.is_actual <> 100 " //ГрПУ, общ.кв.ПУ
                , true);
            if (!ret.result) { return; }

            ExecSQL(conn_db, " Create index ix1_temp_cnt_spis_f on temp_cnt_spis_f (nzp_counter) ", true);
            ExecSQL(conn_db, " Create index ix2_temp_cnt_spis_f on temp_cnt_spis_f (nzp, nzp_serv, nzp_counter) ", true);
            ExecSQL(conn_db, " Create index ix3_temp_cnt_spis_f on temp_cnt_spis_f (nzp, nzp_serv, nzp_cnttype, num_cnt) ", true);
            ExecSQL(conn_db, " Create index ix4_temp_cnt_spis_f on temp_cnt_spis_f (nzp_type, dat_close) ", true);
            ExecSQL(conn_db, " Create index ix5_temp_cnt_spis_f on temp_cnt_spis_f (nzp_cnt) ", true);
            ExecSQL(conn_db, " Create index ix6_temp_cnt_spis_f on temp_cnt_spis_f (nzp, nzp_type) ", true);
            ExecSQL(conn_db, sUpdStat + " temp_cnt_spis_f ", true);

            if (paramcalc.nzp_kvar > 0) //включаем ограничение только при расчете ЛС
            {
                paramcalc.ExistsCounters = DBManager.ExecScalar<bool>(conn_db, "SELECT count(1)>0 FROM temp_cnt_spis_f", out ret, true);
            }

            if (paramcalc.ExistsCounters)
            {
                ret = ExecSQL(conn_db,
                    " Update temp_cnt_spis_f" +
                    " Set is_gkal = 1" +
                        " From " + Points.Pref + "_kernel" + tableDelimiter +
                        "s_countsdop a Where temp_cnt_spis_f.nzp_cnt=a.nzp_cnt "
                    , true);
                if (!ret.result)
                {
                    return;
                }

                ExecSQL(conn_db, " Create index ix7_temp_cnt_spis_f on temp_cnt_spis_f (is_gkal,nzp_counter) ", true);
                ExecSQL(conn_db, sUpdStat + " temp_cnt_spis_f ", true);


                //counters - квартирные ПУ
                ExecSQL(conn_db, " Drop table temp_counters", false);

                ret = ExecSQL(conn_db,
                    " CREATE TEMP TABLE temp_counters (" +
                    "  nzp_cr   serial NOT NULL," +
                    "  nzp_kvar integer," +
                    "  num_ls   integer," +
                    "  nzp_serv integer," +
                    "  nzp_cnttype integer," +
                    "  num_cnt  character(40)," +
                    "  dat_prov     date," +
                    "  dat_provnext date," +
                    "  dat_uchet    date," +
                    "  val_cnt " + sDecimalType + "(16,7)," +
                    "  norma   " + sDecimalType + "(16,7)," +
                    "  is_actual integer," +
                    "  nzp_user  integer," +
                    "  dat_when  date," +
                    "  dat_close date," +
                    "  cur_unl   integer DEFAULT 0," +
                    "  nzp_wp    integer DEFAULT 1," +
                    "  ist       integer DEFAULT 0," +
                    "  dat_oblom date," +
                    "  dat_poch  date," +
                    "  dat_del   date," +
                    "  user_del  integer," +
                    "  nzp_counter integer NOT NULL DEFAULT 0," +
                    "  nzp_counter_child integer NOT NULL DEFAULT 0," +
                    "  month_calc  date," +
                    "  dat_s       date," +
                    "  dat_po      date," +
                    "  dat_block   " + sDateTimeType + "," +
                    "  user_block  integer" +
                    " ) " + sUnlogTempTable
                    , true);
                if (!ret.result)
                {
                    return;
                }

                ret = ExecSQL(conn_db,
                    " insert into temp_counters" +
                    " (nzp_cr,nzp_kvar,num_ls,nzp_serv,nzp_cnttype,num_cnt,dat_prov,dat_provnext,dat_uchet,val_cnt," +
                    "  norma,is_actual,nzp_user,dat_when,dat_close,cur_unl,nzp_wp,ist,dat_oblom,dat_poch,dat_del,user_del," +
                    "  nzp_counter,nzp_counter_child,month_calc,dat_s,dat_po,dat_block,user_block" +
                    " )" +
                    " Select " +
                    "  a.nzp_cr,a.nzp_kvar,a.num_ls,a.nzp_serv,a.nzp_cnttype,a.num_cnt,a.dat_prov,a.dat_provnext,a.dat_uchet,a.val_cnt," +
                    "  a.norma,a.is_actual,a.nzp_user,a.dat_when,a.dat_close,a.cur_unl,a.nzp_wp,a.ist,a.dat_oblom,a.dat_poch,a.dat_del,a.user_del," +
                    "  a.nzp_counter,a.nzp_counter,a.month_calc,a.dat_s,a.dat_po,a.dat_block,a.user_block" +
                    " From " + paramcalc.data_alias + "counters a, temp_cnt_spis_f s" +
                    s + " and a.nzp_counter=s.nzp_counter and a.is_actual <> 100 and s.is_gkal=0 "
                    , true);
                if (!ret.result)
                {
                    return;
                }

                ExecSQL(conn_db, " Create index ix1_temp_counters on temp_counters (nzp_cr) ", true);
                ExecSQL(conn_db,
                    " Create index ix2_temp_counters on temp_counters (nzp_kvar,num_ls,nzp_counter, dat_uchet) ", true);
                ExecSQL(conn_db,
                    " Create index ix3_temp_counters on temp_counters (nzp_kvar, nzp_serv, nzp_cnttype, num_cnt, dat_uchet ) ",
                    true);
                ExecSQL(conn_db, " Create index ix4_temp_counters on temp_counters (nzp_counter, dat_uchet, is_actual) ",
                    true);
                ExecSQL(conn_db, sUpdStat + " temp_counters ", true);

                ExecSQL(conn_db, " Drop table temp_counters_gkal", false);

                ret = ExecSQL(conn_db,
                    " CREATE TEMP TABLE temp_counters_gkal (" +
                    "  nzp_cr   serial NOT NULL," +
                    "  nzp_kvar integer," +
                    "  num_ls   integer," +
                    "  nzp_serv integer," +
                    "  nzp_cnttype integer," +
                    "  num_cnt  character(40)," +
                    "  dat_prov     date," +
                    "  dat_provnext date," +
                    "  dat_uchet    date," +
                    "  val_cnt " + sDecimalType + "(16,7)," +
                    "  norma   " + sDecimalType + "(16,7)," +
                    "  is_actual integer," +
                    "  nzp_user  integer," +
                    "  dat_when  date," +
                    "  dat_close date," +
                    "  cur_unl   integer DEFAULT 0," +
                    "  nzp_wp    integer DEFAULT 1," +
                    "  ist       integer DEFAULT 0," +
                    "  dat_oblom date," +
                    "  dat_poch  date," +
                    "  dat_del   date," +
                    "  user_del  integer," +
                    "  nzp_counter integer NOT NULL DEFAULT 0," +
                    "  nzp_counter_child integer NOT NULL DEFAULT 0," +
                    "  month_calc  date," +
                    "  dat_s       date," +
                    "  dat_po      date," +
                    "  dat_block   " + sDateTimeType + "," +
                    "  user_block  integer" +
                    " ) " + sUnlogTempTable
                    , true);
                if (!ret.result)
                {
                    return;
                }

                ret = ExecSQL(conn_db,
                    " insert into temp_counters_gkal" +
                    " (nzp_cr,nzp_kvar,num_ls,nzp_serv,nzp_cnttype,num_cnt,dat_prov,dat_provnext,dat_uchet,val_cnt," +
                    "  norma,is_actual,nzp_user,dat_when,dat_close,cur_unl,nzp_wp,ist,dat_oblom,dat_poch,dat_del,user_del," +
                    "  nzp_counter,nzp_counter_child,month_calc,dat_s,dat_po,dat_block,user_block" +
                    " )" +
                    " Select " +
                    "  a.nzp_cr,a.nzp_kvar,a.num_ls,a.nzp_serv,a.nzp_cnttype,a.num_cnt,a.dat_prov,a.dat_provnext,a.dat_uchet,a.val_cnt," +
                    "  a.norma,a.is_actual,a.nzp_user,a.dat_when,a.dat_close,a.cur_unl,a.nzp_wp,a.ist,a.dat_oblom,a.dat_poch,a.dat_del,a.user_del," +
                    "  a.nzp_counter,a.nzp_counter,a.month_calc,a.dat_s,a.dat_po,a.dat_block,a.user_block" +
                    " From " + paramcalc.data_alias + "counters a, temp_cnt_spis_f s" +
                    s + " and a.nzp_counter=s.nzp_counter and a.is_actual <> 100 and s.is_gkal=1 "
                    , true);
                if (!ret.result)
                {
                    return;
                }

                ExecSQL(conn_db, " Create index ix1_temp_counters_gkal on temp_counters_gkal (nzp_cr) ", true);
                ExecSQL(conn_db,
                    " Create index ix2_temp_counters_gkal on temp_counters_gkal (nzp_kvar,num_ls,nzp_counter, dat_uchet) ",
                    true);
                ExecSQL(conn_db,
                    " Create index ix3_temp_counters_gkal on temp_counters_gkal (nzp_kvar, nzp_serv, nzp_cnttype, num_cnt, dat_uchet ) ",
                    true);
                ExecSQL(conn_db,
                    " Create index ix4_temp_counters_gkal on temp_counters_gkal (nzp_counter, dat_uchet, is_actual) ",
                    true);
                ExecSQL(conn_db, sUpdStat + " temp_counters_gkal ", true);

                //counters_dom - домовые ПУ
                ExecSQL(conn_db, " Drop table temp_counters_dom ", false);

                ret = ExecSQL(conn_db,
                    " CREATE temp TABLE temp_counters_dom(" +
                    "   nzp_crd INTEGER," +
                    "   nzp_dom INTEGER," +
                    "   nzp_serv INTEGER," +
                    "   nzp_cnttype INTEGER," +
                    "   num_cnt CHAR(40)," +
                    "   dat_prov DATE," +
                    "   dat_provnext DATE," +
                    "   dat_uchet DATE," +
                    "   val_cnt FLOAT," +
                    "   kol_gil_dom INTEGER," +
                    "   is_actual INTEGER," +
                    "   nzp_user INTEGER," +
                    "   comment CHAR(200)," +
                    "   sum_pl " + sDecimalType + "(14,2)," +
                    "   is_doit INTEGER default 1 NOT NULL," +
                    "   is_pl INTEGER," +
                    "   sum_otopl " + sDecimalType + "(14,2)," +
                    "   cnt_ls INTEGER," +
                    "   dat_when DATE," +
                    "   is_uchet_ls INTEGER default 0 NOT NULL," +
                    "   nzp_cntkind INTEGER default 1," +
                    "   nzp_measure INTEGER," +
                    "   is_gkal INTEGER," +
                    "   ngp_cnt " + sDecimalType + "(14,7) default 0.0000000," +
                    "   cur_unl INTEGER default 0," +
                    "   nzp_wp INTEGER default 1," +
                    "   ngp_lift " + sDecimalType + "(14,7) default 0.0000000," +
                    "   dat_oblom DATE," +
                    "   dat_poch DATE," +
                    "   dat_close DATE," +
                    "   nzp_counter INTEGER default 0 NOT NULL," +
                    "   nzp_counter_child integer NOT NULL DEFAULT 0," +
                    "   month_calc DATE," +
                    "   user_del INTEGER," +
                    "   dat_del DATE," +
                    "   dat_s DATE," +
                    "   dat_po DATE " +
                    "   ) " + sUnlogTempTable
                    , true);
                if (!ret.result)
                {
                    return;
                }

                if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0 || paramcalc.list_dom)
                    s = "  Where a.nzp_dom in (select b.nzp_dom from t_selkvar b) ";
                else
                    s = "  Where 1 = 1 ";

                ret = ExecSQL(conn_db,
                    " insert into temp_counters_dom ( " +
                    " nzp_crd,nzp_dom,nzp_serv,nzp_cnttype,num_cnt,dat_prov,dat_provnext,dat_uchet,val_cnt,kol_gil_dom, " +
                    " is_actual,nzp_user,comment,sum_pl,is_doit,is_pl,sum_otopl,cnt_ls,dat_when,is_uchet_ls,nzp_cntkind, " +
                    " nzp_measure,is_gkal,ngp_cnt,cur_unl,nzp_wp,ngp_lift,dat_oblom,dat_poch,dat_close,nzp_counter, " +
                    " nzp_counter_child,month_calc,user_del,dat_del,dat_s,dat_po) " +
                    " Select " +
                    " a.nzp_crd,a.nzp_dom,a.nzp_serv,a.nzp_cnttype,a.num_cnt,a.dat_prov,a.dat_provnext,a.dat_uchet,a.val_cnt,a.kol_gil_dom, " +
                    " a.is_actual,a.nzp_user,a.comment,a.sum_pl,a.is_doit,a.is_pl,sum_otopl,a.cnt_ls,a.dat_when,a.is_uchet_ls,a.nzp_cntkind, " +
                    " a.nzp_measure,a.is_gkal,a.ngp_cnt,a.cur_unl,a.nzp_wp,a.ngp_lift,a.dat_oblom,a.dat_poch,a.dat_close,a.nzp_counter, " +
                    " a.nzp_counter,a.month_calc,a.user_del,a.dat_del,a.dat_s,a.dat_po " +
                    " From " + paramcalc.data_alias + "counters_dom a, temp_cnt_spis_f s " +
                    s + " and a.nzp_counter=s.nzp_counter and a.is_actual <> 100 and s.is_gkal=0 "
                    , true);
                if (!ret.result)
                {
                    return;
                }

                ExecSQL(conn_db, " Create index ix1_temp_counters_dom on temp_counters_dom (nzp_crd) ", true);
                ExecSQL(conn_db,
                    " Create index ix2_temp_counters_dom on temp_counters_dom (nzp_dom,nzp_counter, dat_uchet) ", true);
                ExecSQL(conn_db,
                    " Create index ix3_temp_counters_dom on temp_counters_dom (nzp_dom, nzp_serv, nzp_cnttype, num_cnt, dat_uchet ) ",
                    true);
                ExecSQL(conn_db, " Create index ix4_temp_counters_dom on temp_counters_dom (nzp_counter, dat_uchet) ",
                    true);
                ExecSQL(conn_db, sUpdStat + " temp_counters_dom ", true);

                ExecSQL(conn_db, " Drop table temp_counters_dom_gkal ", false);

                ret = ExecSQL(conn_db,
                    " CREATE temp TABLE temp_counters_dom_gkal(" +
                    //" CREATE TABLE are.temp_counters_dom(" +
                    "   nzp_crd INTEGER," +
                    "   nzp_dom INTEGER," +
                    "   nzp_serv INTEGER," +
                    "   nzp_cnttype INTEGER," +
                    "   num_cnt CHAR(40)," +
                    "   dat_prov DATE," +
                    "   dat_provnext DATE," +
                    "   dat_uchet DATE," +
                    "   val_cnt FLOAT," +
                    "   kol_gil_dom INTEGER," +
                    "   is_actual INTEGER," +
                    "   nzp_user INTEGER," +
                    "   comment CHAR(200)," +
                    "   sum_pl " + sDecimalType + "(14,2)," +
                    "   is_doit INTEGER default 1 NOT NULL," +
                    "   is_pl INTEGER," +
                    "   sum_otopl " + sDecimalType + "(14,2)," +
                    "   cnt_ls INTEGER," +
                    "   dat_when DATE," +
                    "   is_uchet_ls INTEGER default 0 NOT NULL," +
                    "   nzp_cntkind INTEGER default 1," +
                    "   nzp_measure INTEGER," +
                    "   is_gkal INTEGER," +
                    "   ngp_cnt " + sDecimalType + "(14,7) default 0.0000000," +
                    "   cur_unl INTEGER default 0," +
                    "   nzp_wp INTEGER default 1," +
                    "   ngp_lift " + sDecimalType + "(14,7) default 0.0000000," +
                    "   dat_oblom DATE," +
                    "   dat_poch DATE," +
                    "   dat_close DATE," +
                    "   nzp_counter INTEGER default 0 NOT NULL," +
                    "   nzp_counter_child integer NOT NULL DEFAULT 0," +
                    "   month_calc DATE," +
                    "   user_del INTEGER," +
                    "   dat_del DATE," +
                    "   dat_s DATE," +
                    "   dat_po DATE " +
                    "   ) " + sUnlogTempTable
                    , true);
                if (!ret.result)
                {
                    return;
                }

                ret = ExecSQL(conn_db,
                    " insert into temp_counters_dom_gkal ( " +
                    " nzp_crd,nzp_dom,nzp_serv,nzp_cnttype,num_cnt,dat_prov,dat_provnext,dat_uchet,val_cnt,kol_gil_dom, " +
                    " is_actual,nzp_user,comment,sum_pl,is_doit,is_pl,sum_otopl,cnt_ls,dat_when,is_uchet_ls,nzp_cntkind, " +
                    " nzp_measure,is_gkal,ngp_cnt,cur_unl,nzp_wp,ngp_lift,dat_oblom,dat_poch,dat_close,nzp_counter, " +
                    " nzp_counter_child,month_calc,user_del,dat_del,dat_s,dat_po) " +
                    " Select " +
                    " a.nzp_crd,a.nzp_dom,a.nzp_serv,a.nzp_cnttype,a.num_cnt,a.dat_prov,a.dat_provnext,a.dat_uchet,a.val_cnt,a.kol_gil_dom, " +
                    " a.is_actual,a.nzp_user,a.comment,a.sum_pl,a.is_doit,a.is_pl,sum_otopl,a.cnt_ls,a.dat_when,a.is_uchet_ls,a.nzp_cntkind, " +
                    " a.nzp_measure,a.is_gkal,a.ngp_cnt,a.cur_unl,a.nzp_wp,a.ngp_lift,a.dat_oblom,a.dat_poch,a.dat_close,a.nzp_counter, " +
                    " a.nzp_counter, a.month_calc,a.user_del,a.dat_del,a.dat_s,a.dat_po " +
                    " From " + paramcalc.data_alias + "counters_dom a, temp_cnt_spis_f s " +
                    s + " and a.nzp_counter=s.nzp_counter and a.is_actual <> 100 and s.is_gkal=1 "
                    , true);
                if (!ret.result)
                {
                    return;
                }

                ExecSQL(conn_db, " Create index ix1_temp_counters_dom_gkal on temp_counters_dom_gkal (nzp_crd) ", true);
                ExecSQL(conn_db,
                    " Create index ix2_temp_counters_dom_gkal on temp_counters_dom_gkal (nzp_dom,nzp_counter, dat_uchet) ",
                    true);
                ExecSQL(conn_db,
                    " Create index ix3_temp_counters_dom_gkal on temp_counters_dom_gkal (nzp_dom, nzp_serv, nzp_cnttype, num_cnt, dat_uchet ) ",
                    true);
                ExecSQL(conn_db,
                    " Create index ix4_temp_counters_dom_gkal on temp_counters_dom_gkal (nzp_counter, dat_uchet) ", true);
                ExecSQL(conn_db, sUpdStat + " temp_counters_dom_gkal ", true);

            }
            //counters_link
            ExecSQL(conn_db, " Drop table temp_counters_link", false);

            ret = ExecSQL(conn_db,
                " CREATE temp TABLE temp_counters_link (" +
                "   nzp_counter integer NOT NULL," +
                "   nzp_kvar    integer NOT NULL " +
                "   ) " + sUnlogTempTable
                , true);
            if (!ret.result) { return; }

            if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0 || paramcalc.list_dom)
                s = "  ,t_selkvar b Where a.nzp_kvar = b.nzp_kvar ";
            else
                s = "  Where 1 = 1 ";

            ret = ExecSQL(conn_db,
                " insert into temp_counters_link (nzp_counter,nzp_kvar) " +
                " Select a.nzp_counter,a.nzp_kvar" +
                " From " + paramcalc.data_alias + "counters_link a " +
                s
                , true);
            if (!ret.result) { return; }

            ExecSQL(conn_db, " Create index ix1_temp_counters_link on temp_counters_link (nzp_kvar) ", true);
            ExecSQL(conn_db, " Create index ix2_temp_counters_link on temp_counters_link (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " temp_counters_link ", true);

            if (paramcalc.ExistsCounters)
            {
                //counters_group
                ExecSQL(conn_db, " Drop table temp_counters_group", false);

                ret = ExecSQL(conn_db,
                    " CREATE temp TABLE temp_counters_group (" +
                    "   nzp_cg      serial NOT NULL," +
                    "   nzp_counter integer NOT NULL," +
                    "   nzp_counter_child integer NOT NULL DEFAULT 0," +
                    "   dat_uchet   date," +
                    "   val_cnt     " + sDecimalType + "(16,7)," +
                    "   cnt_ls      integer," +
                    "   kol_gil_group   integer," +
                    "   sum_pl_group    " + sDecimalType + "(14,2)," +
                    "   sum_otopl_group " + sDecimalType + "(14,2)," +
                    "   is_uchet_ls     integer NOT NULL DEFAULT 0," +
                    "   is_gkal    integer," +
                    "   is_pl      integer," +
                    "   is_doit    integer NOT NULL DEFAULT 1," +
                    "   is_actual  integer," +
                    "   nzp_user   integer," +
                    "   dat_when   date," +
                    "   cur_unl    integer DEFAULT 0," +
                    "   nzp_wp     integer DEFAULT 1," +
                    "   month_calc date," +
                    "   user_del   integer," +
                    "   dat_del    date," +
                    "   dat_s      date," +
                    "   dat_po     date, " +
                    "   dat_close    date " +
                    "   ) " + sUnlogTempTable
                    , true);
                if (!ret.result) { return; }

                if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0 || paramcalc.list_dom)
                    s = "  Where a.nzp_counter in (select l.nzp_counter from t_selkvar b, temp_counters_link l Where l.nzp_kvar = b.nzp_kvar ) ";
                else
                    s = "  Where 1 = 1 ";

                ret = ExecSQL(conn_db,
                    " insert into temp_counters_group (" +
                    " nzp_cg,nzp_counter,nzp_counter_child,dat_uchet,val_cnt,cnt_ls,kol_gil_group,sum_pl_group," +
                    " sum_otopl_group,is_uchet_ls,is_gkal,is_pl,is_doit,is_actual,nzp_user,dat_when," +
                    " cur_unl,nzp_wp,month_calc,user_del,dat_del,dat_s,dat_po,dat_close" +
                    " ) " +
                    " Select " +
                    " a.nzp_cg,a.nzp_counter,a.nzp_counter,a.dat_uchet,a.val_cnt,a.cnt_ls,a.kol_gil_group,a.sum_pl_group," +
                    " a.sum_otopl_group,a.is_uchet_ls,a.is_gkal,a.is_pl,a.is_doit,a.is_actual,a.nzp_user,a.dat_when," +
                    " a.cur_unl,a.nzp_wp,a.month_calc,a.user_del,a.dat_del,a.dat_s,a.dat_po, s.dat_close" +
                    " From " + paramcalc.data_alias + "counters_group a, temp_cnt_spis_f s " +
                    s + " and a.nzp_counter=s.nzp_counter and a.is_actual <> 100 and s.is_gkal=0 and s.nzp_type = 2 "
                    , true);
                if (!ret.result) { return; }

                ExecSQL(conn_db, " Create index ix1_temp_counters_group on temp_counters_group (nzp_cg) ", true);
                ExecSQL(conn_db, " Create index ix2_temp_counters_group on temp_counters_group (nzp_counter, is_actual) ", true);
                ExecSQL(conn_db, " Create index ix3_temp_counters_group on temp_counters_group (nzp_counter, dat_uchet) ", true);
                ExecSQL(conn_db, sUpdStat + " temp_counters_group ", true);

                ExecSQL(conn_db, " Drop table temp_counters_group_gkal", false);

                ret = ExecSQL(conn_db,
                    " CREATE temp TABLE temp_counters_group_gkal (" +
                    "   nzp_cg      serial NOT NULL," +
                    "   nzp_counter integer NOT NULL," +
                    "   nzp_counter_child integer NOT NULL DEFAULT 0," +
                    "   dat_uchet   date," +
                    "   val_cnt     " + sDecimalType + "(16,7)," +
                    "   cnt_ls      integer," +
                    "   kol_gil_group   integer," +
                    "   sum_pl_group    " + sDecimalType + "(14,2)," +
                    "   sum_otopl_group " + sDecimalType + "(14,2)," +
                    "   is_uchet_ls     integer NOT NULL DEFAULT 0," +
                    "   is_gkal    integer," +
                    "   is_pl      integer," +
                    "   is_doit    integer NOT NULL DEFAULT 1," +
                    "   is_actual  integer," +
                    "   nzp_user   integer," +
                    "   dat_when   date," +
                    "   cur_unl    integer DEFAULT 0," +
                    "   nzp_wp     integer DEFAULT 1," +
                    "   month_calc date," +
                    "   user_del   integer," +
                    "   dat_del    date," +
                    "   dat_s      date," +
                    "   dat_po     date, " +
                    "   dat_close     date " +
                    "   ) " + sUnlogTempTable
                    , true);
                if (!ret.result) { return; }

                ret = ExecSQL(conn_db,
                    " insert into temp_counters_group_gkal (" +
                    " nzp_cg,nzp_counter,nzp_counter_child,dat_uchet,val_cnt,cnt_ls,kol_gil_group,sum_pl_group," +
                    " sum_otopl_group,is_uchet_ls,is_gkal,is_pl,is_doit,is_actual,nzp_user,dat_when," +
                    " cur_unl,nzp_wp,month_calc,user_del,dat_del,dat_s,dat_po,dat_close" +
                    " ) " +
                    " Select " +
                    " a.nzp_cg,a.nzp_counter,a.nzp_counter,a.dat_uchet,a.val_cnt,a.cnt_ls,a.kol_gil_group,a.sum_pl_group," +
                    " a.sum_otopl_group,a.is_uchet_ls,a.is_gkal,a.is_pl,a.is_doit,a.is_actual,a.nzp_user,a.dat_when," +
                    " a.cur_unl,a.nzp_wp,a.month_calc,a.user_del,a.dat_del,a.dat_s,a.dat_po,s.dat_close" +
                    " From " + paramcalc.data_alias + "counters_group a, temp_cnt_spis_f s " +
                    s + " and a.nzp_counter=s.nzp_counter and a.is_actual <> 100 and s.is_gkal=1 and s.nzp_type = 2 "
                    , true);
                if (!ret.result) { return; }

                ExecSQL(conn_db, " Create index ix1_temp_counters_group_gkal on temp_counters_group_gkal (nzp_cg) ", true);
                ExecSQL(conn_db, " Create index ix2_temp_counters_group_gkal on temp_counters_group_gkal (nzp_counter, is_actual) ", true);
                ExecSQL(conn_db, " Create index ix3_temp_counters_group_gkal on temp_counters_group_gkal (nzp_counter, dat_uchet) ", true);
                ExecSQL(conn_db, sUpdStat + " temp_counters_group_gkal ", true);


                ExecSQL(conn_db, " Drop table temp_counters_common_kvar_ttt", false);

                ret = ExecSQL(conn_db,
                    " CREATE temp TABLE temp_counters_common_kvar_ttt (" +
                    "   nzp_cg      serial NOT NULL," +
                    "   nzp_counter integer NOT NULL," +
                    "   nzp_counter_child integer NOT NULL DEFAULT 0," +
                    "   dat_uchet   date," +
                    "   val_cnt     " + sDecimalType + "(16,7)," +
                    "   cnt_ls      integer," +
                    "   kol_gil_group   integer," +
                    "   sum_pl_group    " + sDecimalType + "(14,2)," +
                    "   sum_otopl_group " + sDecimalType + "(14,2)," +
                    "   is_uchet_ls     integer NOT NULL DEFAULT 0," +
                    "   is_gkal    integer," +
                    "   is_pl      integer," +
                    "   is_doit    integer NOT NULL DEFAULT 1," +
                    "   is_actual  integer," +
                    "   nzp_user   integer," +
                    "   dat_when   date," +
                    "   cur_unl    integer DEFAULT 0," +
                    "   nzp_wp     integer DEFAULT 1," +
                    "   month_calc date," +
                    "   user_del   integer," +
                    "   dat_del    date," +
                    "   dat_s      date," +
                    "   dat_po     date, " +
                    "   dat_close    date " +
                    "   ) " + sUnlogTempTable
                    , true);
                if (!ret.result) { return; }

                if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0 || paramcalc.list_dom)
                    s = "  Where a.nzp_counter in (select l.nzp_counter from t_selkvar b, temp_counters_link l Where l.nzp_kvar = b.nzp_kvar ) ";
                else
                    s = "  Where 1 = 1 ";

                ret = ExecSQL(conn_db,
                    " insert into temp_counters_common_kvar_ttt (" +
                    " nzp_cg,nzp_counter,nzp_counter_child,dat_uchet,val_cnt,cnt_ls,kol_gil_group,sum_pl_group," +
                    " sum_otopl_group,is_uchet_ls,is_gkal,is_pl,is_doit,is_actual,nzp_user,dat_when," +
                    " cur_unl,nzp_wp,month_calc,user_del,dat_del,dat_s,dat_po,dat_close" +
                    " ) " +
                    " Select " +
                    " a.nzp_cg,a.nzp_counter,a.nzp_counter,a.dat_uchet,a.val_cnt,a.cnt_ls,a.kol_gil_group,a.sum_pl_group," +
                    " a.sum_otopl_group,a.is_uchet_ls,a.is_gkal,a.is_pl,a.is_doit,a.is_actual,a.nzp_user,a.dat_when," +
                    " a.cur_unl,a.nzp_wp,a.month_calc,a.user_del,a.dat_del,a.dat_s,a.dat_po, s.dat_close" +
                    " From " + paramcalc.data_alias + "counters_group a, temp_cnt_spis_f s " +
                    s + " and a.nzp_counter=s.nzp_counter and a.is_actual <> 100 and s.is_gkal=0 and s.nzp_type = 4 "
                    , true);
                if (!ret.result) { return; }

                ExecSQL(conn_db, " Create index ix1_temp_counters_common_kvar on temp_counters_common_kvar_ttt (nzp_cg) ", true);
                ExecSQL(conn_db, " Create index ix2_temp_counters_common_kvar on temp_counters_common_kvar_ttt (nzp_counter, is_actual) ", true);
                ExecSQL(conn_db, " Create index ix3_temp_counters_common_kvar on temp_counters_common_kvar_ttt (nzp_counter, dat_uchet) ", true);
                ExecSQL(conn_db, sUpdStat + " temp_counters_common_kvar_ttt ", true);


                ExecSQL(conn_db, " Drop table temp_counters_common_kvar_gkal", false);

                ret = ExecSQL(conn_db,
                    " CREATE temp TABLE temp_counters_common_kvar_gkal (" +
                    "   nzp_cg      serial NOT NULL," +
                    "   nzp_counter integer NOT NULL," +
                    "   nzp_counter_child integer NOT NULL DEFAULT 0," +
                    "   dat_uchet   date," +
                    "   val_cnt     " + sDecimalType + "(16,7)," +
                    "   cnt_ls      integer," +
                    "   kol_gil_group   integer," +
                    "   sum_pl_group    " + sDecimalType + "(14,2)," +
                    "   sum_otopl_group " + sDecimalType + "(14,2)," +
                    "   is_uchet_ls     integer NOT NULL DEFAULT 0," +
                    "   is_gkal    integer," +
                    "   is_pl      integer," +
                    "   is_doit    integer NOT NULL DEFAULT 1," +
                    "   is_actual  integer," +
                    "   nzp_user   integer," +
                    "   dat_when   date," +
                    "   cur_unl    integer DEFAULT 0," +
                    "   nzp_wp     integer DEFAULT 1," +
                    "   month_calc date," +
                    "   user_del   integer," +
                    "   dat_del    date," +
                    "   dat_s      date," +
                    "   dat_po     date, " +
                    "   dat_close     date " +
                    "   ) " + sUnlogTempTable
                    , true);
                if (!ret.result) { return; }

                ret = ExecSQL(conn_db,
                    " insert into temp_counters_common_kvar_gkal (" +
                    " nzp_cg,nzp_counter,nzp_counter_child,dat_uchet,val_cnt,cnt_ls,kol_gil_group,sum_pl_group," +
                    " sum_otopl_group,is_uchet_ls,is_gkal,is_pl,is_doit,is_actual,nzp_user,dat_when," +
                    " cur_unl,nzp_wp,month_calc,user_del,dat_del,dat_s,dat_po,dat_close" +
                    " ) " +
                    " Select " +
                    " a.nzp_cg,a.nzp_counter,a.nzp_counter,a.dat_uchet,a.val_cnt,a.cnt_ls,a.kol_gil_group,a.sum_pl_group," +
                    " a.sum_otopl_group,a.is_uchet_ls,a.is_gkal,a.is_pl,a.is_doit,a.is_actual,a.nzp_user,a.dat_when," +
                    " a.cur_unl,a.nzp_wp,a.month_calc,a.user_del,a.dat_del,a.dat_s,a.dat_po,s.dat_close" +
                    " From " + paramcalc.data_alias + "counters_group a, temp_cnt_spis_f s " +
                    s + " and a.nzp_counter=s.nzp_counter and a.is_actual <> 100 and s.is_gkal=1 and s.nzp_type = 4 "
                    , true);
                if (!ret.result) { return; }

                ExecSQL(conn_db, " Create index ix1_temp_counters_common_kvar_gkal on temp_counters_group_gkal (nzp_cg) ", true);
                ExecSQL(conn_db, " Create index ix2_temp_counters_common_kvar_gkal on temp_counters_group_gkal (nzp_counter, is_actual) ", true);
                ExecSQL(conn_db, " Create index ix3_temp_counters_common_kvar_gkal on temp_counters_group_gkal (nzp_counter, dat_uchet) ", true);
                ExecSQL(conn_db, sUpdStat + " temp_counters_common_kvar_gkal ", true);
            }
            //получаем даты начала работы счетчиков
            ret = GetPeriodsActionForCounters(conn_db, paramcalc);
            if (!ret.result) { return; }

            //выбрать связи типа: старый ПУ - новый ПУ (замена ПУ)
            var sql = " CREATE TEMP TABLE t_old_new_counters AS  " +
                  " SELECT DISTINCT r.nzp_counter_old, p.nzp_counter" +
                  " FROM " + paramcalc.data_alias + "counters_replaced  r, temp_counters_dates_start p " +
                  " WHERE p.nzp_counter=r.nzp_counter_new " +
                  " AND r.is_actual ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                "CREATE UNIQUE INDEX ix1_old_new_counters ON t_old_new_counters (nzp_counter_old,nzp_counter)",
                true);
            if (!ret.result) { return; }

            //Темповая таблица с первоначальными периодами перерасчета
            CreateTempProhibitedBegin(conn_db, out ret);
        }

        /// <summary>
        /// Получение периодов действия ПУ
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        private Returns GetPeriodsActionForCounters(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc)
        {
            Returns ret;
            //Даты начала работы приборов учета - определяем по min(dat_uchet) из показаний!
            ExecSQL(conn_db, " Drop table temp_counters_dates_start", false);

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE temp_counters_dates_start (" +
                "  nzp_counter INTEGER NOT NULL," +
                "  date_start DATE, date_last DATE)", true);
            if (!ret.result)
            {
                return ret;
            }

            if (paramcalc.ExistsCounters)
            {
                //таблицы показаний
                var tablesCounterValses = new[] 
            { 
                "temp_counters", //не гкал. показания по ИПУ
                "temp_counters_dom",  //не гкал. показания по ДПУ
                "temp_counters_group", //не гкал. показания по ГрПу, общ.кв.ПУ
                "temp_counters_gkal",  // гкал. показания по ИПУ
                "temp_counters_dom_gkal", // гкал. показания по ДПУ
                "temp_counters_group_gkal", // гкал. показания по ГрПу, общ.кв.ПУ
                "temp_counters_common_kvar_ttt",
                "temp_counters_common_kvar_gkal"
            };
                var sql = String.Empty;
                foreach (var tableCounterVals in tablesCounterValses)
                {
                    sql = string.Format(" INSERT INTO temp_counters_dates_start" +
                                            " (nzp_counter, date_start, date_last)" +
                                            " SELECT nzp_counter, MIN(dat_uchet) date_start, MAX(dat_uchet) " +
                                            " FROM {0} GROUP BY 1", tableCounterVals);
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) return ret;
                }

                ret = ExecSQL(conn_db, " CREATE INDEX ix1_temp_counters_dates_start ON temp_counters_dates_start (nzp_counter) ", true);
                if (!ret.result) return ret;

                //Если для ПУ не определено первое показание считаем его действующим всегда
                sql = " INSERT INTO temp_counters_dates_start" +
                      " (nzp_counter, date_start, date_last)" +
                      " SELECT nzp_counter, '01.01.1900'::DATE date_start, '01.01.1900'::DATE date_last " +
                      " FROM temp_cnt_spis_f f " +
                      " WHERE NOT EXISTS (SELECT 1 FROM temp_counters_dates_start s WHERE s.nzp_counter=f.nzp_counter)";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

            }
            ret = ExecSQL(conn_db, " CREATE INDEX ix2_temp_counters_dates_start ON temp_counters_dates_start (nzp_counter, date_start) ", true);

            return ret;
        }

        #endregion Функция заполнения других временных таблиц LoadTempTableOther

        #region Выбрать временные таблицы для текущего месяца LoadTempTablesForMonth
        //--------------------------------------------------------------------------------
        public void LoadTempTablesForMonth(IDbConnection conn_db, ref CalcTypes.ParamCalc paramcalc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            #region Выбрать временные таблицы для текущего месяца
            ret = Utils.InitReturns();


            //counters_spis - описатели ПУ
            ExecSQL(conn_db, " Drop table temp_cnt_spis", false);

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE temp_cnt_spis (" +
                " nzp_counter  serial NOT NULL," +
                " nzp_type     integer NOT NULL," +
                " nzp          integer NOT NULL," +
                " nzp_serv     integer NOT NULL," +
                " nzp_cnttype  integer NOT NULL," +
                " num_cnt      character(40)," +
                " kod_pu       integer DEFAULT 0," +
                " kod_info     integer DEFAULT 0," +
                " dat_prov     date," +
                " dat_provnext date," +
                " dat_oblom    date," +
                " dat_poch     date," +
                " dat_close    date," +
                " comment      character(60)," +
                " nzp_cnt      integer," +
                " is_gkal      integer NOT NULL DEFAULT 0," +
                " is_actual    integer," +
                " nzp_user     integer," +
                " dat_when     date," +
                " is_pl        integer DEFAULT 0," +
                " cnt_ls       integer DEFAULT 0," +
                " dat_block    " + sDateTimeType + "," +
                " user_block   integer," +
                " month_calc   date," +
                " user_del     integer," +
                " dat_del      date," +
                " dat_s        date," +
                " dat_po       date" +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " INSERT INTO temp_cnt_spis (" +
                " nzp_counter,nzp_type,nzp,nzp_serv,nzp_cnttype,num_cnt," +
                " kod_pu,kod_info,dat_prov,dat_provnext,dat_oblom,dat_poch,dat_close," +
                " comment,nzp_cnt,is_actual,nzp_user,dat_when,is_pl,cnt_ls," +
                " dat_block,user_block,month_calc,user_del,dat_del,dat_s,dat_po,is_gkal" +
                " )" +
                " SELECT " +
                " a.nzp_counter,a.nzp_type,a.nzp,a.nzp_serv,a.nzp_cnttype,a.num_cnt," +
                " a.kod_pu,a.kod_info,a.dat_prov,a.dat_provnext,a.dat_oblom,a.dat_poch,a.dat_close," +
                " a.comment,a.nzp_cnt,a.is_actual,a.nzp_user,a.dat_when,a.is_pl,a.cnt_ls," +
                " a.dat_block,a.user_block,a.month_calc,a.user_del,a.dat_del,a.dat_s,a.dat_po,a.is_gkal" +
                " FROM temp_cnt_spis_f a, temp_counters_dates_start s" +
                " WHERE a.nzp_counter=s.nzp_counter AND s.date_start<= " + paramcalc.dat_po
                , true);
            if (!ret.result) { return; }


            ExecSQL(conn_db, " Create index ix1_temp_cnt_spis on temp_cnt_spis (nzp_counter) ", true);
            ExecSQL(conn_db, " Create index ix2_temp_cnt_spis on temp_cnt_spis (nzp, nzp_serv, nzp_counter) ", true);
            ExecSQL(conn_db, " Create index ix3_temp_cnt_spis on temp_cnt_spis (nzp, nzp_serv, nzp_cnttype, num_cnt) ", true);
            ExecSQL(conn_db, " Create index ix4_temp_cnt_spis on temp_cnt_spis (nzp_type, dat_close) ", true);
            ExecSQL(conn_db, " Create index ix5_temp_cnt_spis on temp_cnt_spis (nzp_cnt) ", true);
            ExecSQL(conn_db, " Create index ix6_temp_cnt_spis on temp_cnt_spis (nzp, nzp_type) ", true);
            ExecSQL(conn_db, " Create index ix7_temp_cnt_spis on temp_cnt_spis (is_gkal,nzp_counter) ", true);


            //nedop_kvar
            ExecSQL(conn_db, " Drop table temp_table_nedop ", false);

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE temp_table_nedop (" +
                "   nzp_nedop serial NOT NULL," +
                "   nzp_kvar  integer," +
                "   nzp_dom   integer," +
                "   num_ls    integer," +
                "   nzp_serv  integer," +
                "   nzp_supp  integer," +
                "   dat_s  " + sDateTimeType + "," +
                "   dat_po " + sDateTimeType + "," +
                "   tn     character(20)," +
                "   nzp_kind   integer," +
                "   month_calc date" +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { return; }

            //для типа недопоставки nzp_kind=75 - 'Проведение профилактических работ' нужно исключить учет для ЛС с ИПУ.
            ret = ExecSQL(conn_db,
                " insert into temp_table_nedop (nzp_nedop,nzp_kvar,nzp_serv,nzp_supp,dat_s,dat_po,tn,nzp_kind,month_calc,nzp_dom,num_ls)" +
                " Select n.nzp_nedop,n.nzp_kvar,n.nzp_serv,n.nzp_supp,n.dat_s,n.dat_po,n.tn,n.nzp_kind,n.month_calc,k.nzp_dom,k.num_ls " +
                " From ttc_nedo n, t_opn k " +
                " Where n.nzp_kvar = k.nzp_kvar " +
                "   and n.dat_s  <= " + paramcalc.dat_po +
                "   and n.dat_po >= " + paramcalc.dat_s +
                "   and not exists (select 1" +
                  " from temp_cnt_spis cs " +
                  " where cs.nzp_serv = (case when n.nzp_serv=14 then 9 else" +
                    " (case when n.nzp_serv=374 then 6 else n.nzp_serv end) end)" +
                  " and cs.nzp_type = 3 and cs.nzp = n.nzp_kvar" +
                  " and " + sNvlWord + "(cs.dat_close," + MDY(1, 1, 3000) + ") > " + paramcalc.dat_s + " and n.nzp_kind = 75 ) " +
                "  and not exists (select 1 from " + Points.Pref + sKernelAliasRest + "serv_norm_koef kf where kf.nzp_serv=n.nzp_serv)" +
                " group by 1,2,3,4,5,6,7,8,9,10,11 "
                , true);
            if (!ret.result) { return; }

            //построить индексы
            ExecSQL(conn_db, " Create index ix1_temp_table_nedop on temp_table_nedop (nzp_nedop) ", true);
            ExecSQL(conn_db, " Create index ix2_temp_table_nedop on temp_table_nedop (nzp_kvar,num_ls, tn) ", true);
            ExecSQL(conn_db, " Create index ix21_temp_table_nedop on temp_table_nedop (nzp_kvar, tn) ", true);
            ExecSQL(conn_db, " Create index ix3_temp_table_nedop on temp_table_nedop (nzp_kvar, nzp_serv, nzp_kind ) ", true);
            ExecSQL(conn_db, " Create index ix4_temp_table_nedop on temp_table_nedop (nzp_dom) ", true);
            ExecSQL(conn_db, sUpdStat + " temp_table_nedop ", true);

            string s = "";
            if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0 || paramcalc.list_dom)
                s = "  ,t_selkvar b Where a.nzp_kvar = b.nzp_kvar ";
            else
                s = "  Where 1 = 1 ";

            //tarif
            ExecSQL(conn_db, " Drop table temp_table_tarif ", false);

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE temp_table_tarif (" +
                "   nzp_tarif serial NOT NULL," +
                "   nzp_kvar  integer," +
                "   nzp_serv  integer," +
                "   nzp_supp  integer," +
                "   nzp_frm   integer," +
                "   dat_s      date," +
                "   dat_po     date," +
                "   month_calc date," +
                "   is_ipu     integer default 0," +    // есть ИПУ
                "   is_use_knp integer default 0," +    // Не зарегистрированные граждане являются потребителями ЖКУ
                "   is_use_ctr integer default 1" +     // использовать временно выбывших если есть ИПУ
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " insert into temp_table_tarif (nzp_tarif,nzp_kvar,nzp_serv,nzp_supp,nzp_frm,dat_s,dat_po,month_calc) " +
                " Select a.nzp_tarif,a.nzp_kvar,a.nzp_serv,a.nzp_supp,a.nzp_frm,a.dat_s,a.dat_po,a.month_calc " +
                " From ttc_tarif a " +
                    s +
                "   and a.dat_s  <= " + paramcalc.dat_po +
                "   and a.dat_po >= " + paramcalc.dat_s +
                " group by 1,2,3,4,5,6,7,8 "
                , true);
            if (!ret.result) { return; }

            ExecSQL(conn_db, " Create unique index ix1_temp_table_tarif on temp_table_tarif (nzp_tarif) ", true);
            ExecSQL(conn_db, " Create index ix2_temp_table_tarif on temp_table_tarif (nzp_kvar, nzp_serv, nzp_supp, dat_s, dat_po ) ", true);
            ExecSQL(conn_db, " Create index ix3_temp_table_tarif on temp_table_tarif (nzp_kvar, nzp_serv, nzp_supp, nzp_frm ) ", true);
            ExecSQL(conn_db, " Create index ix4_temp_table_tarif on temp_table_tarif (nzp_kvar, dat_s, dat_po) ", true);
            ExecSQL(conn_db, " Create index ix5_temp_table_tarif on temp_table_tarif (dat_s, dat_po) ", true);
            ExecSQL(conn_db, sUpdStat + " temp_table_tarif ", true);

            // Не зарегистрированные граждане являются потребителями ЖКУ по договорам
            ret = ExecSQL(conn_db,
                " update temp_table_tarif set is_use_knp=1 " +
                " where exists (select 1" +
                  " from " + paramcalc.data_alias + "prm_11 p" +
                  " where p.nzp=temp_table_tarif.nzp_supp" +
                  "   and p.nzp_prm=1396 and p.val_prm='1'" +
                  "   and p.dat_s  <= " + paramcalc.dat_po +
                  "   and p.dat_po >= " + paramcalc.dat_s +
                  " and p.is_actual<>100) "
                , true);
            if (!ret.result) { return; }

            // есть ИПУ
            ret = ExecSQL(conn_db,
                " update temp_table_tarif set is_ipu=1 " +
                " where exists (select 1" +
                  " from temp_cnt_spis cs " +
                  " where cs.nzp_serv = (case when temp_table_tarif.nzp_serv=14 then 9 else" +
                    " (case when temp_table_tarif.nzp_serv=374 then 6 else temp_table_tarif.nzp_serv end) end)" +
                  " and cs.nzp_type = 3 and cs.nzp = temp_table_tarif.nzp_kvar" +
                  " and " + sNvlWord + "(cs.dat_close," + MDY(1, 1, 3000) + ") > " + paramcalc.dat_s + " ) "
                , true);
            if (!ret.result) { return; }

            // НЕиспользовать временно выбывших если есть ИПУ
            ret = ExecSQL(conn_db,
                " update temp_table_tarif set is_use_ctr=0 " +
                " where is_ipu=1 and exists ( select 1" +
                  " from " + paramcalc.data_alias + "prm_10 p " +
                  " where p.nzp_prm=1427 and p.val_prm='1' and p.is_actual<>100" +
                  "   and p.dat_s  <= " + paramcalc.dat_po +
                  "   and p.dat_po >= " + paramcalc.dat_s +
                  " ) "
                , true);
            if (!ret.result) { return; }

            //prm_1
            if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0 || paramcalc.list_dom)
                s = "  ,t_selkvar b Where a.nzp = b.nzp_kvar ";
            else
                s = "  Where 1 = 1 ";

            ExecSQL(conn_db, " Drop table ttt_prm_1 ", false);
            ExecSQL(conn_db, " Drop table ttt_prm_1d ", false);

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE ttt_prm_1 (" +
                "   nzp_key serial NOT NULL," +
                "   nzp     integer," +
                "   nzp_prm integer," +
                "   val_prm character(20)," +
                "   dat_s      date," +
                "   dat_po     date " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE ttt_prm_1d (" +
                "   nzp_key serial NOT NULL," +
                "   nzp     integer," +
                "   nzp_prm integer," +
                "   val_prm character(20)," +
                "   dat_s      date," +
                "   dat_po     date " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " insert into ttt_prm_1d (nzp_key,nzp,nzp_prm,val_prm,dat_s,dat_po) " +
                " Select min(a.nzp_key) nzp_key,a.nzp,a.nzp_prm,a.val_prm,a.dat_s,a.dat_po " +
                " From ttt_prm_1f a " +
                s +
                "   and a.dat_s  <= " + paramcalc.dat_po +
                "   and a.dat_po >= " + paramcalc.dat_s +
                " group by 2,3,4,5,6 "
                , true);
            if (!ret.result) { return; }

            ExecSQL(conn_db, " Create index ix1_ttt_prm_1d on ttt_prm_1d (nzp,nzp_prm,val_prm,dat_s,dat_po) ", true);
            ExecSQL(conn_db, " Create index ix2_ttt_prm_1d on ttt_prm_1d (nzp_prm) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_prm_1d ", true);

            ret = ExecSQL(conn_db,
                " insert into ttt_prm_1 (nzp_key,nzp,nzp_prm,val_prm,dat_s,dat_po) " +
                " Select min(a.nzp_key) nzp_key,a.nzp,a.nzp_prm,max(a.val_prm) val_prm,min(a.dat_s) dat_s,max(a.dat_po) dat_po " +
                " From ttt_prm_1d a " +
                " group by 2,3 "
                , true);
            if (!ret.result) { return; }

            ExecSQL(conn_db, " Create index ix1_ttt_prm_1 on ttt_prm_1 (nzp,nzp_prm) ", true);
            ExecSQL(conn_db, " Create index ix2_ttt_prm_1 on ttt_prm_1 (nzp_prm) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_prm_1 ", true);


            //prm_2
            if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0 || paramcalc.list_dom)
                s = " Where Exists (select 1 from t_selkvar b Where a.nzp = b.nzp_dom) ";
            else
                s = " Where 1 = 1 ";

            ExecSQL(conn_db, " Drop table ttt_prm_2 ", false);
            ExecSQL(conn_db, " Drop table ttt_prm_2d ", false);

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE ttt_prm_2 (" +
                "   nzp_key serial NOT NULL," +
                "   nzp     integer," +
                "   nzp_prm integer," +
                "   val_prm character(20)," +
                "   dat_s      date," +
                "   dat_po     date " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " CREATE TEMP TABLE ttt_prm_2d (" +
                "   nzp_key serial NOT NULL," +
                "   nzp     integer," +
                "   nzp_prm integer," +
                "   val_prm character(20)," +
                "   dat_s      date," +
                "   dat_po     date " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " insert into ttt_prm_2d (nzp_key,nzp,nzp_prm,val_prm,dat_s,dat_po) " +
                " Select min(a.nzp_key) nzp_key,a.nzp,a.nzp_prm,a.val_prm,a.dat_s,a.dat_po " +
                " From ttt_prm_2f a " +
                    s +
                "   and a.dat_s  <= " + paramcalc.dat_po +
                "   and a.dat_po >= " + paramcalc.dat_s +
                " group by 2,3,4,5,6 "
                , true);
            if (!ret.result)
            {
                return;
            }
            ExecSQL(conn_db, " Create index ix1_ttt_prm_2d on ttt_prm_2d (nzp,nzp_prm,val_prm,dat_s,dat_po) ", true);
            ExecSQL(conn_db, " Create index ix2_ttt_prm_2d on ttt_prm_2d (nzp_prm) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_prm_2d ", true);

            ret = ExecSQL(conn_db,
                " insert into ttt_prm_2 (nzp_key,nzp,nzp_prm,val_prm,dat_s,dat_po) " +
                " Select min(a.nzp_key) nzp_key,a.nzp,a.nzp_prm,max(a.val_prm) val_prm,min(a.dat_s) dat_s,max(a.dat_po) dat_po " +
                " From ttt_prm_2d a " +
                " group by 2,3 "
                , true);
            if (!ret.result)
            {
                return;
            }
            ExecSQL(conn_db, " Create index ix1_ttt_prm_2 on ttt_prm_2 (nzp,nzp_prm) ", true);
            ExecSQL(conn_db, " Create index ix2_ttt_prm_2 on ttt_prm_2 (nzp_prm) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_prm_2 ", true);

            if (paramcalc.ExistsCounters)
            {
                // средние расходы ПУ
                ExecSQL(conn_db, " Drop table ttt_prm_17 ", false);
                ret = ExecSQL(conn_db,
                    " Create temp table ttt_prm_17 " +
                    " ( nzp_key integer, " +
                    "   nzp     integer, " +
                    "   nzp_prm integer, " +
                    "   val_prm char(40)," +
                    "   dat_s   date, " +
                    "   dat_po  date  " +
                    " ) " + sUnlogTempTable
                    , true);
                if (!ret.result)
                {
                    return;
                }

                // !!! перейти на учет только из стека 3 по Where условию !!!
                ret = ExecSQL(conn_db,
                   " Insert into ttt_prm_17 (nzp_key, nzp, nzp_prm, val_prm, dat_s, dat_po) " +
                   " Select min(a.nzp_key) nzp_key,a.nzp,a.nzp_prm,max(a.val_prm) val_prm,min(a.dat_s) dat_s,max(a.dat_po) dat_po " +
                   " From " + paramcalc.data_alias + "prm_17 a, temp_cnt_spis c " +
                   " Where a.is_actual<>100 " +
                   "   and a.nzp=c.nzp_counter " +
                   "   and a.dat_s  <= " + paramcalc.dat_po +
                   "   and a.dat_po >= " + paramcalc.dat_s +
                   " group by 2,3 "
                   , true);
                if (!ret.result)
                {
                    return;
                }

                ExecSQL(conn_db, " Create index ix1_ttt_prm_17 on ttt_prm_17 (nzp,nzp_prm) ", true);
                ExecSQL(conn_db, " Create index ix2_ttt_prm_17 on ttt_prm_17 (nzp_prm) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_prm_17 ", true);

            }

            #endregion Выбрать временные таблицы для текущего месяца

            #region выборка открытых ЛС для по-дневного расчета - t_opn.is_day_calc
            // выборка
            ExecSQL(conn_db, " Drop table t_pd_periods_all ", false);
            ret = ExecSQL(conn_db,
                " Create temp table t_pd_periods_all " +
                " ( nzp_kvar integer, " +
                "   dp       " + sDateTimeType + ", " +
                "   dp_end   " + sDateTimeType + ", " +
                "   typ   integer " +
                " )" + sUnlogTempTable
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " insert into t_pd_periods_all (nzp_kvar, dp, dp_end,typ) " +
                " select t.nzp_kvar,t.dat_s,t.dat_po,1 " +
                " from temp_table_tarif t,t_opn k " +
                " where t.nzp_kvar=k.nzp_kvar and k.is_day_calc=1 " +
                " group by 1,2,3 "
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " insert into t_pd_periods_all (nzp_kvar, dp, dp_end,typ) " +
                " select t.nzp,t.dat_s,t.dat_po,2 " +
                " from ttt_prm_3 t,t_opn k " +
                " where t.nzp=k.nzp_kvar and k.is_day_calc=1 " +
                " group by 1,2,3 "
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " insert into t_pd_periods_all (nzp_kvar, dp, dp_end,typ) " +
                " select t.nzp,t.dat_s,t.dat_po,3 " +
                " From ttt_prm_1d t, " + Points.Pref + "_kernel" + tableDelimiter + "prm_name n ,t_opn k " +
                " where t.nzp=k.nzp_kvar and t.nzp_prm=n.nzp_prm and n.is_day_uchet=1 and k.is_day_calc=1 " +
                " group by 1,2,3 "
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " insert into t_pd_periods_all (nzp_kvar, dp, dp_end,typ) " +
                " select k.nzp_kvar,t.dat_s,t.dat_po,4 " +
                " From ttt_prm_2d t, " + Points.Pref + "_kernel" + tableDelimiter + "prm_name n ,t_opn k " +
                " where t.nzp=k.nzp_dom and t.nzp_prm=n.nzp_prm and n.is_day_uchet=1 and k.is_day_calc=1 " +
                " group by 1,2,3 "
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " insert into t_pd_periods_all (nzp_kvar, dp, dp_end,typ) " +
                " select k.nzp_kvar,t.dat_s,t.dat_po,5 " +
                " From " + paramcalc.data_alias + "gil_periods t, t_opn k " +
                " where t.nzp_kvar=k.nzp_kvar and k.is_day_calc=1 and t.is_actual<>100 " +
                " and t.dat_s <= " + paramcalc.dat_po + " and t.dat_po >= " + paramcalc.dat_s +
                " group by 1,2,3 "
                , true);
            if (!ret.result) { return; }

            ExecSQL(conn_db, " Create index ix3_t_pd_periods_all on t_pd_periods_all (dp) ", false);
            ExecSQL(conn_db, " Create index ix4_t_pd_periods_all on t_pd_periods_all (dp_end) ", false);
            ExecSQL(conn_db, sUpdStat + " t_pd_periods_all ", true);

            ret = ExecSQL(conn_db,
                " delete from t_pd_periods_all" +
                " where dp_end < " + paramcalc.dat_s + " "
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " update t_pd_periods_all" +
                " set dp= " + paramcalc.dat_s +
                " where dp < " + paramcalc.dat_s + " "
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " update t_pd_periods_all" +
                " set dp_end= " + paramcalc.dat_po +
                " where dp_end > " + paramcalc.dat_po + " "
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " insert into t_pd_periods_all (nzp_kvar, dp, dp_end,typ) " +
                " select k.nzp_kvar," + paramcalc.dat_s + "," + paramcalc.dat_po + ",6 " +
                " From t_opn k " +
                " where k.is_day_calc=1 "
                , true);
            if (!ret.result) { return; }

            ExecSQL(conn_db, " Create index ix1_t_pd_periods_all on t_pd_periods_all (nzp_kvar, dp) ", false);
            ExecSQL(conn_db, " Create index ix2_t_pd_periods_all on t_pd_periods_all (nzp_kvar, dp_end) ", false);
            ExecSQL(conn_db, sUpdStat + " t_pd_periods_all ", true);

            ViewTbl(conn_db, " select * from t_opn order by nzp_kvar ");
            ViewTbl(conn_db, " select * from t_pd_periods_all order by nzp_kvar, dp, typ ");
            //
            ExecSQL(conn_db, " Drop table t_pd_dats ", false);
            ret = ExecSQL(conn_db,
                " Create temp table t_pd_dats " +
                " ( nzp_kvar integer, " +
                "   dp       " + sDateTimeType + ", " +
                "   typ      integer  " +
                " )" + sUnlogTempTable
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " insert into t_pd_dats (nzp_kvar, dp, typ) " +
                " select nzp_kvar, dp, 1 " +
                " from t_pd_periods_all " +
                " group by 1,2 "
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " insert into t_pd_dats (nzp_kvar, dp, typ) " +
                " select nzp_kvar, dp_end + " +
#if PG
 " interval '1 day' " +
#else
                " 1 " +
#endif
 "  ,2 " +
                " from t_pd_periods_all " +
                " group by 1,2 "
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " insert into t_pd_dats (nzp_kvar, dp, typ) " +
                " select g.nzp_kvar, g.dat_ofor, 3 " +
                " from " + paramcalc.data_alias + "kart g,t_opn k" +
                " where g.nzp_kvar=k.nzp_kvar and k.is_day_calc=1 and g.nzp_tkrt=1 and " + sNvlWord + "(g.neuch,'0')<>'1' " +
                " and g.dat_ofor > " + paramcalc.dat_s + " and g.dat_ofor <= " + paramcalc.dat_po +
                " group by 1,2 "
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " insert into t_pd_dats (nzp_kvar, dp, typ) " +
                " select g.nzp_kvar, g.dat_ofor + " +
#if PG
 " interval '1 day' " +
#else
                " 1 " +
#endif
 "  ,4 " +
                " from " + paramcalc.data_alias + "kart g,t_opn k" +
                " where g.nzp_kvar=k.nzp_kvar and k.is_day_calc=1 and g.nzp_tkrt=2 " +
                " and g.dat_ofor >= " + paramcalc.dat_s + " and g.dat_ofor <= " + paramcalc.dat_po +
                " group by 1,2 "
                , true);
            if (!ret.result) { return; }


            var useTempRegistrationEndDate = CheckValBoolPrm(conn_db, paramcalc.data_alias, 2202, "5");
            if (useTempRegistrationEndDate)
            {
                ret = ExecSQL(conn_db, string.Format(@"
INSERT INTO t_pd_dats (nzp_kvar, dp, typ)
SELECT DISTINCT g.nzp_kvar, g.dat_oprp + INTERVAL '1 day', 4
FROM {0}kart g
     INNER JOIN t_opn k ON g.nzp_kvar = k.nzp_kvar
WHERE k.is_day_calc = 1 AND g.nzp_tkrt = 1 AND g.dat_oprp >= {1} AND g.dat_oprp <= {2} AND {3}(g.neuch, '0') != '1'",
                    paramcalc.data_alias, paramcalc.dat_s, paramcalc.dat_po, sNvlWord), true);
                if (!ret.result) { return; }
            }


            if (paramcalc.ExistsCounters)
            {
                ret = ExecSQL(conn_db,
                    " insert into t_pd_dats (nzp_kvar, dp, typ) " +
                    " select c.nzp_kvar, c.dat_uchet, 5 " +
                    " from temp_counters c,t_opn k" +
                    " where c.nzp_kvar=k.nzp_kvar and k.is_day_calc=1 " +
                    " and c.dat_uchet > " + paramcalc.dat_s + " and c.dat_uchet <= " + paramcalc.dat_po +
                    " and " + sNvlWord + "(c.dat_close," + MDY(1, 1, 3000) + ") > " + paramcalc.dat_s +
                    " group by 1,2 "
                    , true);
                if (!ret.result)
                {
                    return;
                }

                //выбираем даты закрытия ИПУ для учета средних по дням
                ret = ExecSQL(conn_db,
                    " insert into t_pd_dats (nzp_kvar, dp, typ) " +
                    " select k.nzp_kvar, c.dat_close, 6 " +
                    " from temp_cnt_spis c,t_opn k" +
                    " where c.nzp=k.nzp_kvar and k.is_day_calc=1 " +
                    " and " + sNvlWord + "(c.dat_close," + MDY(1, 1, 3000) + ")" +
                    " between " + paramcalc.dat_s + " and " + paramcalc.dat_po +
                    " and c.nzp_type=3 and c.dat_close is not null " +
                    " group by 1,2 "
                    , true);
                if (!ret.result)
                {
                    return;
                }


                //1390|Учитывать среднее значение ИПУ после даты закрытия (п.354)|||bool||5||||
                paramcalc.enableAvgOnClosedPU = DBManager.GetParamValueInPeriod<bool>(conn_db, paramcalc.pref, 1390, 10,
                    paramcalc.CalcMonth, paramcalc.CalcMonth.WithLastDayMonth(),
                    out ret);
                if (paramcalc.enableAvgOnClosedPU)
                {
                    var countMonthsWithAvg = 3;
                    var countMonthsWithAvgObj = ExecScalar(conn_db, string.Format("SELECT max(val_prm) FROM {0}{1}prm_10 WHERE nzp_prm=1448 AND is_actual<>100 AND " +
                        " '{2}'" + " BETWEEN dat_s and dat_po", paramcalc.pref, sDataAliasRest,
                        paramcalc.CalcMonth.ToShortDateString()), out ret, true);
                    if (countMonthsWithAvgObj != DBNull.Value && countMonthsWithAvgObj != null)
                    {
                        countMonthsWithAvg = Convert.ToInt32(countMonthsWithAvgObj);
                    }
                    //вставляем дату закрытия со смещением в N месяцев для подневного расчета по среднему
                    ret = ExecSQL(conn_db,
                        " insert into t_pd_dats (nzp_kvar, dp, typ) " +
                        " select k.nzp_kvar, c.dat_close + interval '" + countMonthsWithAvg + " month', 7 " +
                        " from temp_cnt_spis c,t_opn k" +
                        " where c.nzp=k.nzp_kvar and k.is_day_calc=1 " +
                        " and " + sNvlWord + "( c.dat_close ," + MDY(1, 1, 3000) + ")+ interval '" + countMonthsWithAvg + " month'" +
                        " between " + paramcalc.dat_s + " and " + paramcalc.dat_po +
                        " and c.nzp_type=3 and c.dat_close is not null " +
                        " group by 1,2 "
                        , true);
                    if (!ret.result) { return; }

                }
            }


            ExecSQL(conn_db, " Create index ix1_t_pd_dats on t_pd_dats (nzp_kvar, dp) ", false);
            ExecSQL(conn_db, " Create index ix2_t_pd_dats on t_pd_dats (dp) ", false);
            ExecSQL(conn_db, sUpdStat + " t_pd_dats ", true);

            ViewTbl(conn_db, " select * from t_pd_dats order by nzp_kvar, dp, typ ");

            ExecSQL(conn_db, " Drop table t_pd_dats_g ", false);
            ret = ExecSQL(conn_db,
                " Create temp table t_pd_dats_g " +
                " ( nzp_kvar integer, " +
                "   dp       " + sDateTimeType + ", " +
                "   typ      integer  " +
                " )" + sUnlogTempTable
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " insert into t_pd_dats_g (nzp_kvar, dp, typ) " +
                " select nzp_kvar, dp,min(typ) typ " +
                " from t_pd_dats " +
                " group by 1,2 "
                , true);
            if (!ret.result) { return; }

            ExecSQL(conn_db, " Create index ix1_t_pd_dats_g on t_pd_dats_g (nzp_kvar, dp) ", false);
            ExecSQL(conn_db, " Create index ix2_t_pd_dats_g on t_pd_dats_g (dp) ", false);
            ExecSQL(conn_db, sUpdStat + " t_pd_dats_g ", true);

            ViewTbl(conn_db, " select * from t_pd_dats_g order by nzp_kvar, dp, typ ");
            //
            ExecSQL(conn_db, " Drop table t_gku_periods ", false);
            ret = ExecSQL(conn_db,
                " Create temp table t_gku_periods " +
                " ( nzp_period serial, " +
                "   nzp_kvar   integer, " +
                "   dp       " + sDateTimeType + ", " +
                "   dp_end   " + sDateTimeType + ", " +
                "   typ  integer, " +
                "   cntd integer," +
                "   cntd_mn integer " +
                " )" + sUnlogTempTable
                , true);
            if (!ret.result) { return; }

            ret = ExecSQL(conn_db,
                " insert into t_gku_periods (nzp_kvar,typ,dp,dp_end,cntd,cntd_mn) " +
                " select a.nzp_kvar,a.typ,a.dp," +
                " min(b.dp) - " +
#if PG
 " interval '1 day' , EXTRACT(day FROM min(b.dp)-a.dp), " +
#else
                " 1 , (min(b.dp)-a.dp), " +
#endif
 DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm) +
                " from t_pd_dats_g a,t_pd_dats_g b " +
                " where a.nzp_kvar = b.nzp_kvar and a.dp < b.dp " +
                " group by 1,2,3 "
                , true);
            if (!ret.result) { return; }

            ExecSQL(conn_db, " Create index ix1_t_gku_periods on t_gku_periods (nzp_kvar) ", false);
            ExecSQL(conn_db, sUpdStat + " t_gku_periods ", true);

            ret = ExecSQL(conn_db,
                " insert into t_gku_periods (nzp_kvar,typ,dp,dp_end,cntd,cntd_mn) " +
                " select k.nzp_kvar,0," + paramcalc.dat_s + "," + paramcalc.dat_po + ", " +
                DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm) + ", " +
                DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm) +
                " From t_opn k " +
                " where NOT EXISTS (select 1 From t_gku_periods t Where k.nzp_kvar=t.nzp_kvar ) "
                //" where 0=( select count(*) From t_gku_periods t Where k.nzp_kvar=t.nzp_kvar ) "
                , true);
            if (!ret.result) { return; }

            ExecSQL(conn_db, " Create unique index ix0_t_gku_periods on t_gku_periods (nzp_period) ", false);
            ExecSQL(conn_db, " Create index ix2_t_gku_periods on t_gku_periods (nzp_kvar,dp,dp_end) ", false);
            ExecSQL(conn_db, sUpdStat + " t_gku_periods ", true);
            if (!ret.result) { return; }

            ViewTbl(conn_db, " select * from t_gku_periods order by nzp_kvar, dp, typ ");

            ret = ExecSQL(conn_db,
                " delete from t_gku_periods" +
                //" where 0=( select count(*) From ttt_prm_3 t " +
                " where NOT EXISTS (select 1 from ttt_prm_3 t " +
                         " where t_gku_periods.nzp_kvar = t.nzp and t_gku_periods.dp <= t.dat_po and t_gku_periods.dp_end >= t.dat_s) "
                , true);
            if (!ret.result) { return; }

            ExecSQL(conn_db, sUpdStat + " t_gku_periods ", true);
            if (!ret.result) { return; }

            //таблица для водоотведения по типу расхода 391
            ExecSQL(conn_db, " Drop table t_is391 ", false);
            ret = ExecSQL(conn_db,
                " create temp table t_is391 (nzp_kvar integer) " + sUnlogTempTable
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, paramcalc.pref);
                return;
            }

            ViewTbl(conn_db, " select * from t_gku_periods order by nzp_kvar, dp, typ ");
            //
            ExecSQL(conn_db, " Drop table t_pd_periods_all ", false);
            ExecSQL(conn_db, " Drop table t_pd_dats ", false);
            ExecSQL(conn_db, " Drop table t_pd_dats_g ", false);

            #endregion выборка открытых ЛС для по-дневного расчета - t_opn.is_day_calc

            //Добавить записи для запрета перерасчета в периоде
            CreateTempProhibitedRecalc(conn_db, paramcalc, out ret);
            if (!ret.result) return;
            InsertIntoTempTableProhibitedRecalc(conn_db, new CalcTypes.ChargeXX(paramcalc), out ret);
        }
        #endregion Выбрать временные таблицы для текущего месяца LoadTempTablesForMonth

        #region Удаление  временных таблиц DropTempTables
        //--------------------------------------------------------------------------------
        void DropTempTables(IDbConnection conn_db, string pref)
        //--------------------------------------------------------------------------------
        {
            ExecSQL(conn_db, " Drop table temp_counters", false);
            ExecSQL(conn_db, " Drop table temp_cnt_spis_f", false);
            ExecSQL(conn_db, " Drop table temp_counters_dom", false);
            ExecSQL(conn_db, " Drop table temp_counters_group", false);
            ExecSQL(conn_db, " Drop table temp_counters_link", false);
            ExecSQL(conn_db, " Drop table aid_i" + pref, false);

            ExecSQL(conn_db, " Drop table ttc_nedo", false);
            ExecSQL(conn_db, " Drop table ttc_tarif", false);
            ExecSQL(conn_db, " Drop table ttt_prm_1f ", false);
            ExecSQL(conn_db, " Drop table ttt_prm_2f", false);
            ExecSQL(conn_db, " Drop table temp_counters_dates_start", false);
            ExecSQL(conn_db, " Drop table t_old_new_counters", false);
            DropTempTablesForMonth(conn_db);
        }
        #endregion Удаление  временных таблиц DropTempTables

        #region Удаление  временных таблиц месяца DropTempTablesForMonth
        //--------------------------------------------------------------------------------
        void DropTempTablesForMonth(IDbConnection conn_db)
        //--------------------------------------------------------------------------------
        {
            ExecSQL(conn_db, " Drop table temp_cnt_spis ", false);
            ExecSQL(conn_db, " Drop table temp_table_nedop ", false);
            ExecSQL(conn_db, " Drop table temp_table_tarif ", false);
            ExecSQL(conn_db, " Drop table ttt_prm_1 ", false);
            ExecSQL(conn_db, " Drop table ttt_prm_2 ", false);
            ExecSQL(conn_db, " Drop table ttt_prm_17 ", false);
            ExecSQL(conn_db, " Drop table ttt_prm_1d ", false);
            ExecSQL(conn_db, " Drop table ttt_prm_2d ", false);
            //
            ExecSQL(conn_db, " Drop table t_pd_periods_all ", false);
            ExecSQL(conn_db, " Drop table t_pd_dats ", false);
            ExecSQL(conn_db, " Drop table t_gku_periods ", false);
        }

        #endregion Удаление  временных таблиц месяца DropTempTablesForMonth

        #region Заполнение  временных вспомогательных таблиц  LoadTempTablesRashod
        //--------------------------------------------------------------------------------
        public void LoadTempTablesRashod(IDbConnection conn_db, ref CalcTypes.ParamCalc paramcalc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            //создать темповую таблицу с периодами запрета перерасчета
            CreateTempProhibitedRecalc(conn_db, paramcalc, out ret);

            LoadTempTableOther(conn_db, ref paramcalc, out ret);
            if (!ret.result)
            {
                //DropTempTablesRahod(conn_db, paramcalc.pref);
                return;
            }

            //определить периоды валидности
            //и разделить счетчики на производные при наличии разрывов в показаниях
            GetValidPeriodsForCounters(conn_db, paramcalc, out ret);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка при обработке периодов валидности счетчика: " + ret.text, MonitorLog.typelog.Error, true);
            }

        }
        #endregion Заполнение  временных вспомогательных таблиц  LoadTempTablesRashod

        public DataTable ViewTbl(IDbConnection conn_db, string sql)
        {
            return null;
            //#if DEBUG

            //            IDataReader reader;
            //            DataTable Data_Table = new DataTable();

            //            if (!ExecRead(conn_db, out reader, sql, true).result)
            //            {
            //                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
            //            }


            //            if (reader != null)
            //            {
            //                try
            //                {
            //                    //заполнение DataTable
            //                    Data_Table.Load(reader, LoadOption.OverwriteChanges);
            //                }
            //                catch (Exception ex)
            //                {
            //                    MonitorLog.WriteLog("!!!" + ex.Message, MonitorLog.typelog.Error, true);
            //                    return null;
            //                }

            //                return Data_Table;
            //            }
            //            else return null;
            //#else
            //           return null;
            //#endif
        }

        #region Проверка наличия логического параметра в нужной базе и нужной таблице prm_5/10
        //--------------------------------------------------------------------------------
        public bool CheckValBoolPrm(IDbConnection conn_db, string pDataAls, int pNzpPrm, string pNumPrm)
        //--------------------------------------------------------------------------------
        {
            IDbCommand cmdCur = DBManager.newDbCommand(
                " select count(*) cnt " +
                " from " + pDataAls + "prm_" + pNumPrm.Trim() + " p " +
                " where p.nzp_prm=" + pNzpPrm + " and p.is_actual<>100 "
            , conn_db);
            bool bRetVal;
            try
            {
                string scntvals = Convert.ToString(cmdCur.ExecuteScalar());
                bRetVal = (Convert.ToInt32(scntvals) > 0);
            }
            catch
            {
                bRetVal = false;
            }
            return bRetVal;
        }

        /// <summary>
        /// Получить значени параметра в текущем расчетном месяце
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="pDataAls"></param>
        /// <param name="pNzpPrm"></param>
        /// <param name="pNumPrm"></param>
        /// <returns></returns>
        public bool CheckValBoolPrmOnDate(IDbConnection conn_db, string pDataAls, int pNzpPrm, string pNumPrm, DateTime cur_month)
        //--------------------------------------------------------------------------------
        {
            IDbCommand cmdCur = DBManager.newDbCommand(
                " select count(*) cnt " +
                " from " + pDataAls + "prm_" + pNumPrm.Trim() + " p " +
                " where p.nzp_prm=" + pNzpPrm + " and p.is_actual<>100 " +
                " and " + Utils.EStrNull(cur_month.ToShortDateString()) + " " +
                " between p.dat_s and p.dat_po"
            , conn_db);
            bool bRetVal;
            try
            {
                string scntvals = Convert.ToString(cmdCur.ExecuteScalar());
                bRetVal = (Convert.ToInt32(scntvals) > 0);
            }
            catch
            {
                bRetVal = false;
            }
            return bRetVal;
        }

        #endregion Проверка наличия логического параметра в нужной базе и нужной таблице prm_5/10

        #region Проверка наличия логического параметра в нужной базе, нужной таблице prm_5/10, по дате, значению параметра
        //--------------------------------------------------------------------------------
        public bool CheckValBoolPrmWithVal(IDbConnection conn_db, string pDataAls, int pNzpPrm, string pNumPrm, string pValPrm, string pDatS, string pDatPo)
        //--------------------------------------------------------------------------------
        {
            IDbCommand cmdCur = DBManager.newDbCommand(
                " Select count(*) cnt From " + pDataAls + "prm_" + pNumPrm.Trim() + " p " +
                " Where p.nzp_prm = " + pNzpPrm + " and p.val_prm='" + pValPrm.Trim() + "' " +
                " and p.is_actual <> 100 and p.dat_s  <= " + pDatPo + " and p.dat_po >= " + pDatS + " "
                , conn_db);
            bool bRetVal;
            try
            {
                string scntvals = Convert.ToString(cmdCur.ExecuteScalar());
                bRetVal = (Convert.ToInt32(scntvals) > 0);
            }
            catch
            {
                bRetVal = false;
            }
            return bRetVal;
        }

        #endregion Проверка наличия логического параметра в нужной базе, нужной таблице prm_5/10, по дате, значению параметра

        #region Выбрать целый параметр в нужной базе, нужной таблице prm_ХХ, по дате
        //--------------------------------------------------------------------------------
        public int LoadValPrmForNorm(IDbConnection conn_db, string pDataAls, int pNzpPrm, string pNumPrm, string pDatS, string pDatPo)
        //--------------------------------------------------------------------------------
        {
            IDbCommand cmdCur = DBManager.newDbCommand(
                " Select max(val_prm" + sConvToInt + ") val_prm From " + pDataAls + "prm_" + pNumPrm.Trim() +
                " Where nzp_prm = " + pNzpPrm + " and is_actual <> 100 and dat_s  <= " + pDatPo + " and dat_po >= " + pDatS + " "
                , conn_db);
            int iRetVal;
            try
            {
                string sval = Convert.ToString(cmdCur.ExecuteScalar());
                iRetVal = Convert.ToInt32(sval);
            }
            catch
            {
                iRetVal = 0;
            }

            return iRetVal;
        }
        #endregion Выбрать целый параметр в нужной базе, нужной таблице prm_ХХ, по дате

        #region Выбрать параметр дату в нужной базе, нужной таблице prm_ХХ, по дате
        //--------------------------------------------------------------------------------
        public DateTime LoadValPrmForNormDate(IDbConnection conn_db, string pDataAls, int pNzpPrm, string pNumPrm, string pDatS, string pDatPo)
        //--------------------------------------------------------------------------------
        {
            IDbCommand cmdCur = DBManager.newDbCommand(
                " Select max(val_prm" + sConvToInt + ") val_prm From " + pDataAls + "prm_" + pNumPrm.Trim() +
                " Where nzp_prm = " + pNzpPrm + " and is_actual <> 100 and dat_s  <= " + pDatPo + " and dat_po >= " + pDatS + " "
                , conn_db);
            DateTime iRetVal;
            try
            {
                string sval = Convert.ToString(cmdCur.ExecuteScalar());
                iRetVal = Convert.ToDateTime(sval);
            }
            catch
            {
                iRetVal = Convert.ToDateTime("01.01.1900");
            }

            return iRetVal;
        }
        #endregion Выбрать параметр дату в нужной базе, нужной таблице prm_ХХ, по дате


        #endregion Функции и структуры для подсчета расходов

        #region Функция подсчета расходов CalcRashod
        //--------------------------------------------------------------------------------
        public bool CalcRashod(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret)
        //--------------------------------------------------------------------------------
        {

            ret = Utils.InitReturns();

            #region Выставить признак необходимости генерации средних расходов
            //----------------------------------------------------------------
            // генерация средних значений расходов ИПУ
            //----------------------------------------------------------------
            if (Constants.Trace) Utility.ClassLog.WriteLog("генерация средних значений расходов ИПУ");

            //текущий расчетный месяц
            var prm = new CalcMonthParams();
            prm.pref = paramcalc.pref;
            var rec = Points.GetCalcMonth(prm);
            //текущий расчетный месяц этого банка
            var CalcDate = new DateTime(rec.year_, rec.month_, 1);


            //1987|Отменить расчет средних расходов ИПУ при расчете всей БД|||bool||10||||
            bool bDoCalcSredRash = !CheckValBoolPrmOnDate(conn_db, paramcalc.data_alias, 1987, "10", CalcDate);
            //1979|Запускать расчет средних расходов ИПУ всегда при расчете|||bool||10||||
            bool bDoCalcSredRashAlways = CheckValBoolPrmOnDate(conn_db, paramcalc.data_alias, 1979, "10", CalcDate);

            //генерировать средние показания ПУ
            var doCalcSredRash = false;
            //если нет запрета на банке по расчету среднего
            if (bDoCalcSredRash)
            {
                //если текущий расчетный месяц
                if ((paramcalc.calc_yy == paramcalc.cur_yy) && (paramcalc.calc_mm == paramcalc.cur_mm))
                {
                    //если в банке включен параметр считать всегда
                    if (bDoCalcSredRashAlways)
                    {
                        //генерируем при любом расчете
                        doCalcSredRash = true;
                    }
                    else
                    {
                        //генерируем только при расчете банка
                        doCalcSredRash = (paramcalc.nzp_kvar == 0) && (paramcalc.nzp_dom == 0);
                    }
                }

            }

            #endregion Выставить признак необходимости генерации средних расходов

            #region Генерация средних расходов ИПУ
            if (doCalcSredRash && paramcalc.ExistsCounters)  //счетчиков нет - считать нечего
            {
                ExecSQL(conn_db, " Drop table t_lsipu ", false);
                ret = ExecSQL(conn_db,
                    " Create temp table t_lsipu " +
                    " (  nzp_dom   integer not null, " +
                    "    nzp_kvar  integer not null, " +
                    "    mark      integer default 1," +
                    "    pref      char(20) " +
                    " ) " + sUnlogTempTable
                    , true);
                if (!ret.result) { conn_db.Close(); return false; }

                ret = ExecSQL(conn_db,
                    " Insert Into t_lsipu (nzp_dom,nzp_kvar,pref)" +
                    " Select nzp_dom,nzp_kvar,'" + paramcalc.pref + "' pref from t_opn Where 1=1 "
                    , true);
                if (!ret.result) { conn_db.Close(); return false; }

                ExecSQL(conn_db, sUpdStat + " t_lsipu ", false);

                int nzp_user = 1;
                MonitorLog.WriteLog("CalcRashod: Генерация средних значений ПУ", MonitorLog.typelog.Info, 1, 2, true);
                Call_GenSrZnKPUWithOutConnect(conn_db, nzp_user, paramcalc.cur_yy, paramcalc.cur_mm, "t_lsipu", out ret);
                if (!ret.result) { conn_db.Close(); return false; }

                ExecSQL(conn_db, " Drop table t_lsipu ", false);
            }
            #endregion Генерация средних расходов ИПУ

            #region Описание типов стека расходов от 1 до 6
            //----------------------------------------------------------------
            //Стек расходов:
            //----------------------------------------------------------------
            // 1 - ПУ
            // 2 - отсутствие показаний, но есть ПУ (средний расход по ИПУ)
            // 3 - итоговый расход
            // 39 - итоговый расход от ГКал
            // 4 - средние значения
            // 5 - изменения приборов
            // 6 - 87 П - не реализовано
            // 9 - расходы от ГКал по показаниям
            // 10 - по-дневной расчет - вначале норматив
            // 11 - по-дневной расчет - расходы ИПУ по показаниям
            // 19 - по-дневной расчет - средние расходы ИПУ 
            // 20 - по-дневной расчет - итоговый расход
            // 30 - нормативные расходы
            #endregion Описание типов стека расходов от 1 до 6

            #region Подготовка к подсчету - заполнение временных таблиц и считывание ПУ с портала

            Rashod rashod = new Rashod(paramcalc);

            DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
            //---------------------------------------------------
            //выбрать множество лицевых счетов
            //---------------------------------------------------
            if (!TempTableInWebCashe(conn_db, "t_selkvar"))
            {
                ChoiseTempKvar(conn_db, ref paramcalc, out ret);
                if (!ret.result) { conn_db.Close(); return false; }
            }

            CreateCountersXX(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            string p_dat_charge = DateNullString;
            if (!rashod.paramcalc.b_cur)
                p_dat_charge = MDY(rashod.paramcalc.cur_mm, 28, rashod.paramcalc.cur_yy);

            //----------------------------------------------------------------
            // признак - проводится расчет 1-го л/с
            //----------------------------------------------------------------
            bool b_calc_kvar = (rashod.paramcalc.nzp_kvar > 0);

            //MyDataReader reader;

            //----------------------------------------------------------------
            //показания приборов  - stek=1 & nzp_type = 1,2,3
            //----------------------------------------------------------------
            // val_s,dat_s,val_po,dat_po - показания прибора
            // val4 - общий расход по ПУ
            // val1 - расход в расчетном месяце
            // squ1 - площадь лс
            // rvirt - вирт. расход

            //выбрать counters_dom во временную таблицу
            if (paramcalc.b_loadtemp)
            {
                LoadTempTablesRashod(conn_db, ref rashod.paramcalc, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }

            if (paramcalc.isPortal)
            {
                //учтем текущий charge_cnts во временной таблице (накроем показания)
                Portal_UchetCounters(conn_db, paramcalc, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }

            #endregion Подготовка к подсчету - заполнение временных таблиц и считывание ПУ с портала

            #region настройка алгоритма по параметрам базы (next_month7,1994,9000,1989)
            bool b_next_month7 = false;

            // Points.IsSmr -> nzp_prm=2000 & Points.Is50 -> nzp_prm=2000

            if (Points.IsSmr) //(reader.Read())
            {
                b_next_month7 = Points.IsSmr;
            }

            /* включить алгоритм учета дельт расходов при расчете ОДН - для ЕИРЦ Самара val_prm='2'! - временно пока  здесь ! - перенести в sprav.ifmx.cs */
            bool b_is_delta_smr =
              CheckValBoolPrmWithVal(conn_db, rashod.paramcalc.data_alias, 1994, "5", "2", rashod.paramcalc.dat_s, rashod.paramcalc.dat_po);

            /* включить алгоритм учета дельт расходов при расчете ОДН только в текущем расчетном месяце - стандарт val_prm='1'! */
            bool b_is_delta_standart =
              CheckValBoolPrmWithVal(conn_db, rashod.paramcalc.data_alias, 1994, "5", "1", rashod.paramcalc.dat_s, rashod.paramcalc.dat_po);

            // ... Saha
            bool bIsSaha = CheckValBoolPrm(conn_db, rashod.paramcalc.data_alias, 9000, "5");

            bool bIsCalcSubsidyBill = CheckValBoolPrm(conn_db, rashod.paramcalc.data_alias, 1989, "5");
            // ...

            #endregion настройка алгоритма по параметрам базы

            // Выборка расходов счетчиков
            // создание ttt_counters_ipu - структуры для хранения стеков 1/11 (для ПУ от ГКал стек 9) - расходы ИПУ по периодам
            if (paramcalc.ExistsCounters)
            {
                SelRashodPUInStek1(conn_db, rashod, b_calc_kvar, p_dat_charge, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }
            #region Заполнение площадей и подготовка списка коммунальных квартир (создание и первичное заполнение ttt_counters_xx и t_ans_kommunal)

            // создание ttt_counters_xx - структуры для хранения стека 3 - структура совпадает с counters_xx!
            CreateTempRashodMain(conn_db, rashod, p_dat_charge, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            //int iNzpRes;
            // первичное заполнение ttt_counters_xx - заполнение площадей,количества жильцов,признаков ПУ,выделение коммунальных лс - стеки 3/10 - расходы по периодам
            // сохраняется - ttt_aid_c2 - кол-во жильцов по ЛС!
            SetTempRashodParams(conn_db, rashod, b_next_month7, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            // Подготовить перечень коммуналок и их параметров + 29 стек для коммуналок в случае если расчет домовой !b_calc_kvar
            // используется (потом удалается) - ttt_aid_c2 - кол-во жильцов по ЛС!
            CreateTempRashodKommunal(conn_db, rashod, b_calc_kvar, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            #endregion Заполнение площадей и подготовка списка коммунальных квартир (создание и первичное заполнение ttt_counters_xx и t_ans_kommunal)

            if (paramcalc.ExistsCounters)
            {
                // подготовка средних по ПУ (стек 2) 
                SetSteksSredPU(conn_db, rashod, b_calc_kvar, p_dat_charge, out ret);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }
            //средних по ЛС (стек 4 и стек 5)
            SetSteksSredLs(conn_db, rashod, p_dat_charge, out ret);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            if (!b_calc_kvar)
            {
                // ...
                // нужно все сделать здесь !!! т.к. потом при учете нормативов учитываются результаты расчета ПУ от ГКал:
                // расчетные параметры/нормативы от ГКал на кв.м и куб.м 
                // ...

                // сформировать стек 39 - итоговый стек расходов от ГКал (аналог стека 3 для "канонических" ед.изм. из s_counts)
                CalcStek39gkal(conn_db, rashod, p_dat_charge, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                // удаление результатов расчетов ПУ от ГКал из временных таблиц параметров
                ClearStek39UchetPUgkal(conn_db, rashod, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }

            //----------------------------------------------------------------
            //ссылки на нормативные значения (resolution.nzp_res) - в зависимости от месяца расчета!
            //----------------------------------------------------------------
            // ... Выборка нормативов
            CalcRashodNorm(conn_db, rashod, bIsSaha, b_calc_kvar, bIsCalcSubsidyBill, p_dat_charge, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            if (paramcalc.ExistsCounters)
            {
                // Размножение записей для общеквартирных приборов учета
                DivisionOKPU(conn_db, rashod, p_dat_charge, out ret);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // вставка ИПУ по лс - stek=3 & nzp_type = 3 //  используется ttt_counters_ipu - стеки 1/11 (для ПУ от ГКал стек 9) - расходы ИПУ по периодам
                CalcRashodSetPUInStek3(conn_db, rashod, p_dat_charge, out ret);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // расход ВСЕГДА = val1 + val2:
                // для ИПУ val1 = 0 и val2 = ИПУ , 
                // для нормативов val1 = норма и val2 = 0
                // для ИПУ в середине месяца val1 > 0 и val2 > 0

                // коммуналки - уменьшим расход в пропорции общего кол-ва жильцов
                CalcRashodLowPUforKommunal(conn_db, rashod, out ret);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }
            // проставить средние и изменения в нормативы - stek = 3 и nzp_type = 3
            SetSredPUInStek3(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            // проставить расход - stek = 3 и nzp_type = 3
            CalcStek3Rashod(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            // изменить нормативы по ночному э/э с учетом дневного э/э
            CorrNorm210with25(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            // стек 3 - первоначально сформировать итоговые суммы по стеку - ЛС,дом,ГрПУ (из ttt_counters_xx)
            InsStek3First(conn_db, rashod, p_dat_charge, b_calc_kvar, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            //запрет перерасчета в периоде: Обновление значений для counters_xx
            UpdateCounters(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #region расчеты ОДН по дому - stek=3 & nzp_type in (1,2)
            //----------------------------------------------------------------
            //итого по дому - stek=3 & nzp_type = 1
            //----------------------------------------------------------------
            // cnt1 - кол-во жильцов (целое число)
            // gil1 - кол-во жильцов (с учетом времен. выбывших, дробное)
            // val1 - расход по нормативу
            // val2 - расходы КПУ
            // val3 - расход ДПУ
            // squ1 - площадь всех лс
            // squ2 - площадь лс без КПУ
            // rvirt - вирт. расход
            // kf307- коэфициент

            //dlt_cur - текущая дельта
            //dlt_calc- распределенный (учтенный) расход по 307 П

            // ... nzp_prm=1991 -> запретить перерасчет ОДН - можно учитывать ОДН только в текущем расчетном месяце
            bool bIsCalcODN =
              !(CheckValBoolPrmWithVal(conn_db, rashod.paramcalc.data_alias, 1991, "5", "1", rashod.paramcalc.dat_s, rashod.paramcalc.dat_po));

            bool bIsCalcCurMonth = ((rashod.paramcalc.cur_yy == rashod.paramcalc.calc_yy) && (rashod.paramcalc.cur_mm == rashod.paramcalc.calc_mm));

            //bool bDoCalcOdn = bIsCalcODN;
            //if (!bDoCalcOdn) { bDoCalcOdn = bIsCalcCurMonth; }
            // ...

            // стеки для учета перерасчетов для ОДН
            string sStekDltCnts = "0";
            if (b_is_delta_smr)
            {
                sStekDltCnts = "104";
            }
            else if (b_is_delta_standart)
            {
                sStekDltCnts = "3"; // b_is_delta_standart = true
            }


            if (!b_calc_kvar)
            {
                #region итого по дому - stek=3 & nzp_type = (1,2) - суммы по ЛС и расход ОДПУ

                // итого по дому - stek=3 & nzp_type = 1
                UpdLsValsToODPU(conn_db, rashod, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // итого по ГрПУ - stek=3 & nzp_type = 2
                UpdLsValsToGrPU(conn_db, rashod, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                //расход ГрПУ
                SetValGrPU(conn_db, rashod, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                //расход ДПУ
                SetValODPU(conn_db, rashod, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #endregion итого по дому - stek=3 & nzp_type = (1,2) - суммы по ЛС и расход ОДПУ

                #region  учесть и записать дельты расходов в перерасчетный месяц для возможности снятия при расчетах ОДН
                //----------------------------------------------------------------
                // учесть и записать дельты расходов в перерасчетный месяц для возможности снятия при расчетах ОДН
                // дельты расходов по ЛС учитываются для ОДПУ и ГрПУ
                //----------------------------------------------------------------
                if (b_is_delta_smr)
                    UseDeltaCntsForMonth(conn_db, rashod, sStekDltCnts, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                if (b_is_delta_standart)
                    UseDeltaCntsForMonth(conn_db, rashod, sStekDltCnts, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                #endregion  учесть и записать дельты расходов в перерасчетный месяц для возможности снятия при расчетах ОДН - stek=2

                #region сохранить стек 3 для домов и ГрПУ (nzp_type= (1,2) and stek=3) в стек 354 с расчетом по Пост.№354 (+ нормативы ОДН)
                //----------------------------------------------------------------
                // сохранить стек 3 для домов (nzp_type=1 and stek=3) в стек 354 для расчетов по Пост.№354
                //----------------------------------------------------------------

                // расчет ОДН для ГрПУ - создается и обрабатывается ttt_aid_kpu
                Calc354GrPU(conn_db, rashod, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // расчет ОДН для ОДПУ - создается и обрабатывается ttt_aid_kpu
                Calc354ODPU(conn_db, rashod, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #endregion сохранить стек 3 для домов и ГрПУ (nzp_type= (1,2) and stek=3) в стек 354 с расчетом по Пост.№354 (+ нормативы ОДН)

                //----------------------------------------------------------------
                //-- вставка в стек 3540 для бойлеров - самостоятельное приготовление ГВС и Отопления только для ОДПУ
                //-- Используется ранее созданная ttt_aid_kpu для домоых ПУ! в Calc354ODPU(...)!
                //----------------------------------------------------------------
                #region сохранить стек 3 для домов (nzp_type=1 and stek=3) в стек 3540 с расчетом по Пост.№354 - спец.расчет для бойлеров

                Stek3540Make(conn_db, rashod, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #endregion сохранить стек 3 для домов (nzp_type=1 and stek=3) в стек 3540 с расчетом по Пост.№354 - спец.расчет для бойлеров

                #region расчет ОДПУ в домах без ИПУ в стек = 3 (пропорция) - для nzp_type in (1,2)

                // расчет ОДПУ в домах без ИПУ в стек = 3 (пропорция) - для nzp_type in (1,2)
                Calc307Props(conn_db, rashod, bIsCalcODN, bIsCalcCurMonth, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #endregion расчет ОДПУ в домах без ИПУ в стек = 3 (пропорция) - для nzp_type in (1,2)

                #region учет ОДН по 354 постановлению в стек = 3 & nzp_type = 1 & 2

                Stek354UchetInDom(conn_db, rashod, bIsCalcODN, bIsCalcCurMonth, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #endregion учет ОДН по 354 постановлению в стек = 3 & nzp_type = 1 & 2

                #region учет ОДН по 307 постановлению в стек = 3

                //------------------------------------------------------------------
                // Пост.№ 307
                //------------------------------------------------------------------
                Stek3Uchet307InDom(conn_db, rashod, bIsCalcODN, bIsCalcCurMonth, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #endregion учет ОДН по 307 постановлению в стек = 3

                #region учет ОДН по домам из прошлого, если он был рассчитан - запись в стек = 3

                Stek3UchetODNLast(conn_db, rashod, bIsCalcODN, bIsCalcCurMonth, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #endregion учет ОДН по домам из прошлого, если он был рассчитан - запись в стек = 3

                // отметить ученные суммы дельт по ЛС
                if (b_is_delta_standart)
                {
                    SetIsUseDeltaCntsLSForMonth(conn_db, rashod, sStekDltCnts, out ret);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                }
            }

            #endregion расчеты ОДН по дому - stek=3 & nzp_type  in (1,2)

            #region учет ОДН по л/с
            //----------------------------------------------------------------
            // учет ОДН по л/с
            //----------------------------------------------------------------

            CalcRashodSetODNInStek3forLS(conn_db, rashod, b_calc_kvar, p_dat_charge, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            CalcRashodSetODNInStek20forLS(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion учет ОДН по л/с

            #region расчет ПУ от ГКал по домам - запись в стек = 39

            if (!b_calc_kvar)
            {
                Stek39UchetPUgkal(conn_db, rashod, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }
            else
            {
                Stek39UchetPUgkalLS(conn_db, rashod, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }

            #endregion расчет ПУ от ГКал по домам - запись в стек = 39

            // коррекция расхода по канализации по ХВС и ГВС
            CalcRashodKANsumHVandGV(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            // распределенный (учтенный) расход - расчет сальдо расходов
            CalcRashodSaldo(conn_db, rashod, b_calc_kvar, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            #region записать дельты расходов в перерасчетный месяц для возможности снятия при расчетах ОДН - stek=2
            //----------------------------------------------------------------
            // записать дельты расходов в перерасчетный месяц для возможности снятия при расчетах ОДН - stek=2
            //----------------------------------------------------------------
            // если расчет 1го ЛС, то таблицу  нужно считать!

            if (b_is_delta_smr)
            {
                if (b_calc_kvar)
                    UseDeltaCntsLSForMonth(conn_db, rashod, sStekDltCnts, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                WriteDeltaCntsForMonth(conn_db, rashod, paramcalc, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }

            #endregion записать дельты расходов в перерасчетный месяц для возможности снятия при расчетах ОДН - stek=2

            // в целях учета дельты расходов необходимо, чтобы в counters_xx присутствовали все коммунальные услуги из charge_xx !!!
            UchetRashodRest(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            // обновление исходных расходов полученных не по нормативам (val1_source,up_kf)
            ClearSourceValsForPU(conn_db, rashod, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            // Удалить временные таблицы
            DropTempTablesRahod(conn_db, rashod.paramcalc.pref);

            return true;
        }
        #endregion Функция подсчета расходов
    }

}

#endregion здесь производится подсчет расходов

#endregion Подсчет расходов


