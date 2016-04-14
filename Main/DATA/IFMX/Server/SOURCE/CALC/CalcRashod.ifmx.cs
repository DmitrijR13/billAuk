using Bars.KP50.Utils;

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

#endregion Подключаемые модули

#region здесь производится подсчет расходов
namespace STCLINE.KP50.DataBase
{
   
    //здась находятся классы для подсчета расходов
    public partial class DbCalc : DbCalcClient
    {
    #region Функции и структуры для подсчета расходов
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

        #region Функция обновления статистик по Charge_XX
        /// <summary>
        /// Обновить статистику таблицы в базе данных PREF_charge_XX за расчетный месяц
        /// </summary>
        /// <param name="alldata"></param>
        /// <param name="pc">испольуются поля nzp_kvar, nzp_dom, pref, calc_yy</param>
        /// <param name="tab">наименование таблицы</param>
        /// <param name="ret">результат выполнения операции</param>
        void UpdateStatistics(bool alldata, ParamCalc pc, string tab, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (pc.nzp_kvar == 0 && (alldata || pc.nzp_dom == 0))
            {
                string conn_kernel = Points.GetConnByPref(pc.pref);
                IDbConnection conn_db2 = GetConnection(conn_kernel);

                ret = OpenDb(conn_db2, true);
                if (!ret.result)
                {
                    return;
                }
                ret = ExecSQL(conn_db2,
#if PG
                " SET search_path TO '" + pc.pref + "_charge_" + (pc.calc_yy - 2000).ToString("00") + "'"
#else
                " Database " + pc.pref + "_charge_" + (pc.calc_yy - 2000).ToString("00")
#endif
                    , true);
                if (!ret.result)
                {
                    conn_db2.Close();
                    return;
                }
#if PG
                ret = ExecSQL(conn_db2, "vacuum " + sUpdStat + " " + tab, true);
#else
                ret = ExecSQL(conn_db2, sUpdStat + " " + tab, true);
#endif
                if (!ret.result)
                {
                    conn_db2.Close();
                    return;
                }

#if PG
                ret = ExecSQL(conn_db2, " SET search_path TO '" + Points.Pref + "_kernel'", true);
#else
                ret = ExecSQL(conn_db2, " Database " + Points.Pref + "_kernel", true);
#endif
                if (!ret.result)
                {
                    conn_db2.Close();
                    return;
                }
                conn_db2.Close();
            }

        }
        #endregion Функция обновления статистик по Charge_XX

        #region Структура Расход Rashod
        struct Rashod
        {
            public ParamCalc paramcalc;

            public string counters_xx;
            public string counters_tab;
            public string countlnk_xx;
            public string countlnk_tab;
            //public string cur_counters;
            //public string charge_xx;
            public string gil_xx;
            public string charge_xx;
            public string delta_xx_cur;
            
            public string where_dom;
            public string where_kvar;
            public string where_kvarK;
            public string where_kvarA;


            public bool   calcv; //считать виртуальный расходы
            public bool   k307;
            public int    nzp_type_alg;


            public Rashod(ParamCalc _paramcalc)
            {
                paramcalc = _paramcalc;
                calcv   = false;
                k307    = false;
                nzp_type_alg = 6;  //алгоритм учета ДПУ


                //cur_counters = paramcalc.pref + "_charge_" + (paramcalc.cur_yy - 2000).ToString("00") + ":counters_" + paramcalc.cur_mm.ToString("00");
                //charge_xx    = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + ":charge_" + paramcalc.calc_mm.ToString("00");

                counters_tab    = "counters" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                counters_xx     = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") +
                    tableDelimiter + counters_tab;

                countlnk_tab = "countlnk" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                countlnk_xx = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") +
                    tableDelimiter + countlnk_tab;

                gil_xx          = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") +
                    tableDelimiter + "gil" + 
                    paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");

                charge_xx       = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") +
                    tableDelimiter + "charge" +
                    paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");

                delta_xx_cur = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") +
                    tableDelimiter + "delta_" + 
                    paramcalc.calc_mm.ToString("00");


                string s = "";
                if (paramcalc.b_reval)
                    s = " in ( Select nzp_dom From t_selkvar) ";
                else
                {
                    if (paramcalc.nzp_dom > 0)
                        //s = " = " + paramcalc.nzp_dom;
                        s = " in ( Select nzp_dom From t_selkvar) ";
                    else
                        s = " > 0 ";
                }

                where_dom = "nzp_dom " + s;
                

                where_kvar  = "";
                where_kvarK = "";
                where_kvarA = "";

                if (paramcalc.nzp_kvar > 0)
                {
                    where_kvar  = " and nzp_kvar   = " + paramcalc.nzp_kvar;
                    where_kvarK = " and k.nzp_kvar = " + paramcalc.nzp_kvar;
                    where_kvarA = " and a.nzp_kvar = " + paramcalc.nzp_kvar;
                }
            }
        }

        #endregion Структура Расход

        #region Структура Расход Rashod2
        struct Rashod2 
        {
            public string tab; 
            public string dat_s; 
            public string dat_po; 
            public string p_TAB; 
            public string p_KEY; 
            public string p_INSERT; 
            public string p_ACTUAL;

            public string counters_xx;

            public ParamCalc paramcalc;

            public Rashod2(string _counters_xx, ParamCalc _paramcalc)
            {
                paramcalc = _paramcalc;
                counters_xx = _counters_xx;
                tab         = ""; 
                dat_s       = ""; 
                dat_po      = ""; 
                p_TAB       = ""; 
                p_KEY       = ""; 
                p_INSERT    = "";
                p_ACTUAL    = "";
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
                cmnlc    = _cmnlc;
                nzp_serv = _nzp_serv;
                norma    = _norma;

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

        #region Функция создания временных таблиц - аналогов CountersXX
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
                    ExecByStep(conn_db2, rashod.counters_xx, "nzp_cntx", sql , 100000, " ", out ret);
                }


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
                " set search_path to '" + rashod.paramcalc.pref + "_charge_" + (rashod.paramcalc.calc_yy - 2000).ToString("00")+"'"
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
                      "    kod_info    integer default 0 ) "                     //выбранный способ учета (=1)
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

        #endregion Функция создания временных таблиц - аналогов CountersXX

        #region Функция выборки значений счетчиков используя структуру Rashod2

        //--------------------------------------------------------------------------------
        bool LoadVals(IDbConnection conn_db, Rashod2 rashod2, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

           //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
           //выберем показания, которые полностью покрывают месяц!
           //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
           //           |      |
           //     ^-------------------^
           //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            string sql =
            rashod2.p_INSERT +
            "   and a.dat_uchet = ( Select max(c.dat_uchet) From " + rashod2.tab + " c " +
                                  " Where c.nzp_counter = a.nzp_counter " +
                                  "   and c.dat_uchet <= " + rashod2.dat_s +
                                  "   " + rashod2.p_ACTUAL + " ) " +
            "   and b.dat_uchet = ( Select min(c.dat_uchet) From " + rashod2.tab + " c " +
                                  " Where c.nzp_counter = a.nzp_counter " +
                                  "   and c.dat_uchet > " + rashod2.dat_po +
                                  "   " + rashod2.p_ACTUAL + " ) ";

            if (rashod2.paramcalc.nzp_kvar > 0 || rashod2.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql + " Group by 1,2,3,4,5,6,7 ", true);
            }
            else
            {
                ExecByStep(conn_db, rashod2.p_TAB, rashod2.p_KEY, sql, 100000, " Group by 1,2,3,4,5,6,7 ", out ret);
            }
            if (!ret.result)
            {
                return false;
            }
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //выбор других интервалов, кроме уже выбранных в counters_xx
            //придеться изголяться, чтобы выбрать ближайшие показания (избежать выбора большого интервала)
            //зато понятно
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //           |      |
            //   ^-----------^
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            ret = ExecSQL(conn_db,
#if PG
                " Select distinct nzp_counter Into temp ttt_aid_c1 From " + rashod2.counters_xx 
#else
                " Select unique nzp_counter From " + rashod2.counters_xx +
                " Into temp ttt_aid_c1 With no log "
#endif
                , true);
            if (!ret.result)
            {
                return false;
            }
            ret = ExecSQL(conn_db,
                " Create unique index ix_aid22_ttt1 on ttt_aid_c1 (nzp_counter) ", true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);
                return false;
            }

            sql = 
            rashod2.p_INSERT +
            "   and 1 > ( Select count(*) From ttt_aid_c1 n Where a.nzp_counter = n.nzp_counter ) " +
            "   and a.dat_uchet = ( Select max(c.dat_uchet) From " + rashod2.tab + " c " +
                                  " Where c.nzp_counter = a.nzp_counter " +
                                  "   and c.dat_uchet <= " + rashod2.dat_s +
                                  "   " + rashod2.p_ACTUAL + " ) " +
            "   and b.dat_uchet = ( Select max(c.dat_uchet) From " + rashod2.tab + " c " +
                                  " Where c.nzp_counter = b.nzp_counter " +
                                  "   and c.dat_uchet <= " + rashod2.dat_po +
                                  "   " + rashod2.p_ACTUAL + " ) ";

            if (rashod2.paramcalc.nzp_kvar > 0 || rashod2.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql + " Group by 1,2,3,4,5,6,7 ", true);
            }
            else
            {
                ExecByStep(conn_db, rashod2.p_TAB, rashod2.p_KEY, sql, 100000, " Group by 1,2,3,4,5,6,7 ", out ret);
            }
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);
                return false;
            }
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //           |      |
            //              ^-------------^
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            ret = ExecSQL(conn_db,
#if PG
                " Select distinct nzp_counter Into temp ttt_aid_c1 From " + rashod2.counters_xx 
#else
                " Select unique nzp_counter From " + rashod2.counters_xx +
                " Into temp ttt_aid_c1 With no log "
#endif
                , true);
            if (!ret.result)
            {
                return false;
            }
            ret = ExecSQL(conn_db,
                " Create unique index ix_aid22_ttt1 on ttt_aid_c1 (nzp_counter) ", true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);
                return false;
            }

            sql =
            rashod2.p_INSERT +
            "   and 1 > ( Select count(*) From ttt_aid_c1 n Where a.nzp_counter = n.nzp_counter ) " +
            "   and a.dat_uchet = ( Select min(c.dat_uchet) From " + rashod2.tab + " c " +
                                  " Where c.nzp_counter = a.nzp_counter " +
                                  "   and c.dat_uchet >= " + rashod2.dat_s +
                                  "   " + rashod2.p_ACTUAL + " ) " +
            "   and b.dat_uchet = ( Select min(c.dat_uchet) From " + rashod2.tab + " c " +
                                  " Where c.nzp_counter = b.nzp_counter " +
                                  "   and c.dat_uchet >= " + rashod2.dat_po +
                                  "   " + rashod2.p_ACTUAL + " ) ";

            if (rashod2.paramcalc.nzp_kvar > 0 || rashod2.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql + " Group by 1,2,3,4,5,6,7 ", true);
            }
            else
            {
                ExecByStep(conn_db, rashod2.p_TAB, rashod2.p_KEY, sql, 100000, " Group by 1,2,3,4,5,6,7 ", out ret);
            }
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);
                return false;
            }
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //           |      |
            //             ^--^
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            ret = ExecSQL(conn_db,
#if PG
                " Select distinct nzp_counter Into temp ttt_aid_c1 From " + rashod2.counters_xx,
#else
                " Select unique nzp_counter From " + rashod2.counters_xx +
                " Into temp ttt_aid_c1 With no log ",
#endif
                true);
            if (!ret.result)
            {
                return false;
            }
            ret = ExecSQL(conn_db,
                " Create unique index ix_aid22_ttt1 on ttt_aid_c1 (nzp_counter) ", true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);
                return false;
            }

            sql =
            rashod2.p_INSERT +
                "   and 1 > ( Select count(*) From ttt_aid_c1 n Where a.nzp_counter = n.nzp_counter ) " +
                "   and a.dat_uchet = ( Select min(c.dat_uchet) From " + rashod2.tab + " c " +
                                      " Where c.nzp_counter = a.nzp_counter " +
                                      "   and c.dat_uchet >= " + rashod2.dat_s +
                                      "   " + rashod2.p_ACTUAL + " ) " +
                "   and b.dat_uchet = ( Select max(c.dat_uchet) From " + rashod2.tab + " c " +
                                      " Where c.nzp_counter = b.nzp_counter " +
                                      "   and c.dat_uchet <= " + rashod2.dat_po +
                                      "   " + rashod2.p_ACTUAL + " ) ";

            if (rashod2.paramcalc.nzp_kvar > 0 || rashod2.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql + " Group by 1,2,3,4,5,6,7 ", true);
            }
            else
            {
                ExecByStep(conn_db, rashod2.p_TAB, rashod2.p_KEY, sql, 100000, " Group by 1,2,3,4,5,6,7 ", out ret);
            }


            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);
            return true;
        }
        #endregion Функция выборки значений счетчиков

        #region Функция расчета расходов по пост 87 и 262 (РТ)
        //--------------------------------------------------------------------------------
        bool Calc87(IDbConnection conn_db, Rashod rashod, Call87 call87, ParamCalc paramcalc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            //----------------------------------------------------------------
            //Постановление 87 и 262 по дому - stek=6 & nzp_type = 1,3
            //----------------------------------------------------------------
            // cnt1   - кол-во жильцов с КПУ
            // cnt2   - кол-во жильцов без КПУ
            // gil1   - дробное кол-во жильцов с КПУ
            // gil2   - дробное кол-во жильцов без КПУ

            // val1 - нормативщики (Vnn)
            // val2 - расходы КПУ (Vnp)
            // val3 - расход ДПУ (Vd)
            // squ2 - площадь лс без КПУ (Sd)
            // rvirt - вирт. расход (на всякий случай)
            // dop87 - 7 кВт или добавка (87 П)
            // gl7kw - 7 кВт КПУ (учитывая корректировку)
            // vl210- расход 210
            // pu7kw- 7 кВт или откорректированный множитель
            // kf307- коэфициент

            //выбрать дома, удовлетворяющие критерию 87 (есть ДПУ, КПУ и нормативщики )
            ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);

            ret = ExecSQL(conn_db,
            " Select 6 stek,1 nzp_type,dat_charge, 0 nzp_kvar,nzp_dom,1870 nzp_counter, dat_s,dat_po, " + call87.nzp_serv + " nzp_serv, " +

                " sum(case when nzp_type = 3 and nzp_serv = " + call87.nzp_serv + " and cnt_stage = 0 " + " then val1 else 0 end) as val1,  " + //нормативы (Vnn)
                " sum(case when nzp_type = 3 and nzp_serv = " + call87.nzp_serv + " then val2 else 0 end) as val2,  " + //КПУ       (Vnp)
                " sum(case when nzp_type = 1 and nzp_serv = " + call87.nzp_serv + " then val3 else 0 end) as val3,  " + //ДПУ       (Vd)

                " sum(case when nzp_type = 1 and nzp_serv = " + call87.nzp_serv + " and dlt_cur>0 " + " then val3 else 0 end) as dlt_cur, " + //дельта расхода (когда ДПУ > КПУ и нормативы)

                " sum(case when nzp_type = 3 and nzp_serv = " + call87.nzp_serv + " and cnt_stage = 0 " + call87.mmnog0 + " then squ1 else 0 end) as squ2,  " + //площадь лс без КПУ (Sd)
                " sum(case when nzp_type = 3 and nzp_serv = " + call87.nzp_serv + call87.mmnog0 + " then rvirt else 0 end) as rvirt,  " + //вирт. расход

                " sum(case when nzp_type = 3 and nzp_serv = " + call87.nzp_serv + " and cnt_stage = 1 " + call87.mmnog0 + " then gil1 * " + call87.norma + " else 0 end) as gl7kw," + //7кВт c КПУ

                " sum(case when nzp_type = 3 and nzp_serv = " + call87.nzp_serv + " and cnt_stage = 1 " + call87.mmnog0 + " then gil1   else 0 end) as gil1," + //жильцов c КПУ
                " sum(case when nzp_type = 3 and nzp_serv = " + call87.nzp_serv + " and cnt_stage = 0 " + call87.mmnog0 + " then gil1   else 0 end) as gil2," +  //жильцов без КПУ - нормативщики

                //210 услуга вычленить расход, чтобы потом убрать из общего расхода - выберается только когда nzp_serv = 25, иначе 0
                " sum(case when nzp_type = 3 and nzp_serv = 210 then case when cnt_stage = 0 then val1 else val2 end else 0 end ) as vl210  " + //расход 210
#if PG
                " Into temp ttt_aid_stat  "+
#else
                "  "+
#endif
            " From " + rashod.counters_xx + " a " +
            " Where nzp_type in (1,3) and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                call87.and_serv +
            " Group by 1,2,3,4,5,6,7,8,9 " +
#if PG
            "  "
#else
            " Into temp ttt_aid_stat With no log "
#endif
                , true);
            if (!ret.result)
            {
                return false;
            }
            ret = ExecSQL(conn_db,
                " Create unique index ix_aid44_st on ttt_aid_stat (nzp_dom,nzp_serv) ", true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                return false;
            }
            ret = ExecSQL(conn_db,
                " Create        index ix_aid45_st on ttt_aid_stat (nzp_type) ", true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                return false;
            }

            string sql = 
                " Insert into " + rashod.counters_xx +
                " ( stek,nzp_type,dat_charge,nzp_kvar,nzp_dom,nzp_counter,dat_s,dat_po, nzp_serv,  gil2,gil1, val1,val2,val3,squ2,rvirt,gl7kw,vl210,pu7kw ) " +
                " Select 6,1,    dat_charge,0 nzp_kvar,nzp_dom,nzp_counter,dat_s,dat_po,nzp_serv,  gil2,gil1, val1,val2,val3,squ2,rvirt,gl7kw,vl210, " + call87.norma + " " +
                " From ttt_aid_stat " +
                " Where abs(val1)>0.0001 and abs(val2)>0.0001 " + //где есть КПУ и нормативщики
                "   and dlt_cur > 0 "; //есть дельта расхода

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, "ttt_aid_stat", "nzp_dom", sql , 10000, "", out ret);
            }

            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                return false;
            }

            UpdateStatistics(false, paramcalc, rashod.counters_tab, out ret);
            //UpdateStatisticsCounters_xx(rashod, out ret);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                return false;
            }

            //текущая дельта
            sql = 
                " Update " + rashod.counters_xx +
                " Set dlt_cur = (val3-vl210) - (val2+gl7kw) - val1 " + // ДПУ - 210 - КПУ с учетом 7 кВт - нормативщики
                " Where nzp_type = 1 and stek = 6 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge;

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, "", out ret);
            }
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                return false;
            }

            //коэфициент
            sql = 
                " Update " + rashod.counters_xx +
                " Set kf307 = dlt_cur / squ2  " + //
                " Where nzp_type = 1 and stek = 6 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                "   and abs(squ2) > 0 ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, "", out ret);
            }
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                return false;
            }

            //----------------------------------------------------------------
            //если dlt_cur < 0, то надо скорректировать по письму НЧ №  5089 от 14.05.2010
            //----------------------------------------------------------------
            IDataReader reader;
            ret = ExecRead(conn_db, out reader,
            " Select * From " + rashod.counters_xx + " a " +
            " Where nzp_type = 1 and stek = 6 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                call87.and_serv +
            "   and dlt_cur < 0 "
                , true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                return false;
            }
            try
            {
                //dlt_cur = (val3-vl210) - (val2+gl7kw) - val1 "+ // ДПУ - 210 - КПУ с учетом 7 кВт - нормативщики(в т.ч. 7 кВт)
                while (reader.Read())
                {
                    decimal k = (decimal)reader["dlt_cur"] + (decimal)reader["gl7kw"];
                    if (k > 0)
                    {
                        //т.е. достаточно уменьшать 7 кВт ДПУ (gl7kw) - письмо НЧ № 7248 от 17.08.2009
                        decimal n = k / (int)reader["gil1"]; //новое значение 7 кВт КПУ

                        ret = ExecSQL(conn_db,
                        " Update " + rashod.counters_xx +
                        " Set gl7kw   = " + k.ToString() +
                            ", pu7kw   = " + n.ToString() +
                            ", dlt_cur = (val3-vl210) - (val2+" + k.ToString() + ") - val1 " + //=0
                            ", nzp_counter = 1871 " + //оставили признак корректировки
                        " Where nzp_cntx  = " + (int)reader["nzp_cntx"]
                            , true);
                        if (!ret.result)
                        {
                            reader.Close();
                            ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                            return false;
                        }

                        ret = ExecSQL(conn_db,
                        " Update " + rashod.counters_xx +
                        " Set kf307 = dlt_cur / squ2  " + //коэфициент
                        " Where nzp_cntx  = " + (int)reader["nzp_cntx"]
                            , true);
                        if (!ret.result)
                        {
                            reader.Close();
                            ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                            return false;
                        }
                    }
                    else
                    {
                        //сложный случай, надо корректировать еще и нормативщиков и только в пределах 7 кВт
                        //(val3-vl210) - (val2+0) - val1 < 0

                        decimal n = 0;
                        decimal.TryParse(call87.norma, out n);
                        n = n * (int)reader["gil2"]; //предел уменьшения норматива

                        //мы должны уменьшить норматив val1 в пределах 7кВт на жильца, т.е. д.б. отрицательный dop87!
                        string tab;
                        if (k + n > 0)
                            tab = "1872";    //k:=k (n покрывает k)
                        else
                        {
                            k = -n;       //n НЕ покрывает k, вычитаем все 7 кВт
                            tab = "1873";
                        }

                        n = k / (decimal)reader["squ2"]; //новое значение 7 кВт для нормативщиков

                        ret = ExecSQL(conn_db,
                        " Update " + rashod.counters_xx +
                        " Set gl7kw   = 0 " +  //
                            ", pu7kw   = 0 " +
                            ", kf307   = " + n.ToString() +
                            ", val1    =  val1 + (" + k.ToString() + ")" +
                            ", dlt_cur = (val3-vl210) - (val2+0) - (val1 + (" + k.ToString() + ") ) " + //=0
                            ", nzp_counter = " + tab + //оставили признак корректировки
                        " Where nzp_cntx  = " + (int)reader["nzp_cntx"]
                            , true);
                        if (!ret.result)
                        {
                            reader.Close();
                            ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                            return false;
                        }
                    }
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();
                ret.result = false;
                ret.text = ex.Message;
                return false;
            }

            //вставить лс
            ret = ExecRead(conn_db, out reader,
                " Select * From " + rashod.counters_xx +
                " Where nzp_type = 1 and stek = 6 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge
                //"   and abs(kf307) > 1 "; //применять только когда коэфициент > 1
                , true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                return false;
            }
            try
            {
                //решил оставить на стеке = 3, но в других полях (dop87) - добавок к основному расходу (7кВт или проп.норматив (87 П) )
                while (reader.Read())
                {
                    decimal kf307 = (decimal)reader["kf307"];
                    decimal pu7kw = (decimal)reader["pu7kw"];

                    //перезаписать расход 307
                    if (rashod.nzp_type_alg==8)
                    {
                        sql = 
                            " Update " + rashod.counters_xx +
                            " Set rashod = case when cnt_stage = 0" +
                                    " then val1 " + kf307.ToString() + " * squ1 " + //начислить норматив
                                    " else val2 " + pu7kw.ToString() + " * gil1 " + //КПУ
                                    " end " +
                            " Where nzp_type = 3 and stek = 3 and nzp_dom = " + (int)reader["nzp_dom"] + rashod.paramcalc.per_dat_charge +
                            "   and nzp_serv = " + (int)reader["nzp_serv"];

                        if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                        {
                            ret = ExecSQL(conn_db, sql, true);
                        }
                        else
                        {
                            ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, "", out ret);
                        }
                        if (!ret.result)
                        {
                            reader.Close();
                            ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                            return false;
                        }
                    }

                    sql =
                          " Update " + rashod.counters_xx +
                          " Set dop87 = case when cnt_stage = 0 " +
                                     " then " + kf307.ToString() + " * squ1 " +
                                     " else " + pu7kw.ToString() + " * gil1 " +
                                     " end " +
                             ",pu7kw = case when cnt_stage = 0 " +  //примененный к-фт 87
                                     " then " + kf307.ToString() +
                                     " else " + pu7kw.ToString() +
                                     " end " +
                          " Where nzp_type = 3 and stek = 3 and nzp_dom = " + (int)reader["nzp_dom"] + rashod.paramcalc.per_dat_charge +
                          "   and nzp_serv = " + (int)reader["nzp_serv"]
                           + call87.mmnog0; //или кроме коммуналок, или по всем

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, "", out ret);
                    }
                    if (!ret.result)
                    {
                        reader.Close();
                        ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                        return false;
                    }

                }
                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();
                ret.result = false;
                ret.text = ex.Message;
                return false;
            }


            ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
            return true;
        }

        #endregion Функция расчета расходов по пост 87 и 262 (РТ)

        #region Функция пересоздания временных таблиц по префиксам Cnt_NotCond
        //--------------------------------------------------------------------------------
        bool Cnt_NotCond(IDbConnection conn_db, ParamCalc paramcalc/*Rashod rashod*/, byte recreate, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            //recreate = 1 - значит надо пересоздать!
            //recreate = 0 - не надо!
            //recreate = 2 - удалить!

            IDataReader reader;
            ret = ExecRead(conn_db, out reader,
            " Select * From aid_i" + paramcalc.pref
                , false);
            if (!ret.result)
            {
                recreate = 1; //по-любому надо пересоздать!
            }
            else
            {
                if (reader.Read())
                {
                    //интервалы созданы - выходим!
                    reader.Close();
                    if (recreate == 0)
                        return true;
                }
                reader.Close();
            }

            ExecSQL(conn_db, " Drop table aid_i" + paramcalc.pref, false);
            ExecSQL(conn_db, " Drop table aid_d" + paramcalc.pref, false);
            
            //команда удаления временных таблиц!!!
            if (recreate == 2)
            {
                //написать код перебора префиксов!!!
                return true;
            }

            ret = ExecSQL(conn_db,
                " Create temp table aid_i" + paramcalc.pref +
                //" Create table aid_i" + paramcalc.pref +
                " ( nzp_key serial not null, " +
                "   nzp_counter integer, " +
                "   dat_s  date, " +
                "   dat_po date  " +
#if PG
                " ) "
#else
                " ) With no log "
#endif
                //" ) "
                , true);
            if (!ret.result)
            {
                ExecSQL(conn_db, " Drop table aid_i" + paramcalc.pref, false);
                //ExecSQL(conn_db, " Drop table aid_d" + rashod.paramcalc.pref, false);
                return false;
            }
            ExecSQL(conn_db, " Create unique index ix_aid33_" + paramcalc.pref + " on aid_i" + paramcalc.pref + " (nzp_key) ", true);
            ExecSQL(conn_db, " Create        index ix_aid44_" + paramcalc.pref + " on aid_i" + paramcalc.pref + " (nzp_counter, dat_s, dat_po) ", true);

            //ExecSQL(conn_db, " Create        index ix_aid22_" + rashod.paramcalc.pref + " on aid_i" + rashod.paramcalc.pref + " (nzp_counter, dat_s, dat_po) ", true);

            ExecSQL(conn_db, sUpdStat + " aid_i" + paramcalc.pref, true);
            
            //отберем некондиционные интервалы
            //выбираем искомые таблицы
            string[] arr = { "temp_counters",
                             "temp_cnt_spis",
                             "temp_counters_dom",
                             paramcalc.data_alias + "counters_domspis"
                           };
            
            //признак учета даты закрытия на текущий момент curd
            DateTime dat_799 = new DateTime(3000, 1, 1);

            ret = ExecRead(conn_db, out reader,
                " Select val_prm From " + paramcalc.data_alias + "prm_10 " +
                " Where nzp_prm = 799 " +
                "   and is_actual <> 100 " +
                "   and dat_s <= '" + paramcalc.curd.ToShortDateString() + "'" +
                "   and dat_po>= '" + paramcalc.curd.ToShortDateString() + "'"
                , false);
            try
            {
                if (reader.Read())
                {
                    if (reader["val_prm"] != DBNull.Value)
                    {
                        string s = (string)reader["val_prm"];
                        DateTime.TryParse(s.Trim(), out dat_799);
                    }
                }
            }
            catch
            {
            }
            reader.Close();

            for (int i=0; i<=3; i++)
            {
                //string swhere = " and " + rashod.where_dom;
                string swhere = " and nzp_dom in ( Select nzp_dom From t_selkvar) ";
                if (i == 0)
                {
                    //квартирные ПУ
                    swhere = " and nzp_kvar in ( Select nzp_kvar From t_selkvar) ";
                }
                if (i == 1)
                {
                    //квартирные ПУ
                    swhere = " and nzp_type=3 and nzp in ( Select nzp_kvar From t_selkvar) ";
                }

                //сначала период починки
                ret = ExecSQL(conn_db,
                    " Insert into aid_i" + paramcalc.pref + " (nzp_counter, dat_s, dat_po) " +
                    " Select " + sUniqueWord + " nzp_counter, dat_oblom, dat_poch " +
                    " From " + arr[i] +
                    " Where is_actual <> 100 " +
                    "   and dat_oblom is not null and dat_poch is not null " + swhere
                    , true);
                if (!ret.result)
                {
                    ExecSQL(conn_db, " Drop table aid_i" + paramcalc.pref, false);
                    ExecSQL(conn_db, " Drop table aid_d" + paramcalc.pref, false);
                    return false;
                }

                //затем период закрытия ПУ
                //выберем все даты закрытия
                ExecSQL(conn_db, " Drop table aid_dclose", false);
                ExecSQL(conn_db, " Drop table aid_dclose1", false);

                ret = ExecSQL(conn_db,
                    " Select nzp_counter, dat_close "+
#if PG
                    " Into temp aid_dclose1 "+
#else
                    "  "+
#endif
                    "From " + arr[i] +
                    " Where is_actual <> 100 and dat_close is not null " + swhere +
#if PG
                    "  "
#else
                    " Into temp aid_dclose1 With no log "
#endif
                    , true);
                if (!ret.result)
                {
                    ExecSQL(conn_db, " Drop table aid_i" + paramcalc.pref, false);
                    ExecSQL(conn_db, " Drop table aid_d" + paramcalc.pref, false);
                    ExecSQL(conn_db, " Drop table aid_dclose", false);
                    ExecSQL(conn_db, " Drop table aid_dclose1", false);
                    return false;
                }

                ExecSQL(conn_db, " CREATE INDEX ix_aid_dclose1 ON aid_dclose1(nzp_counter, dat_close) ", true);
                ExecSQL(conn_db, sUpdStat + " aid_dclose1 ", true);

                ret = ExecSQL(conn_db,
                    " Select nzp_counter, min(dat_close) as dat_close " +
#if PG
                    " Into temp aid_dclose "+
#else
                    "  "+
#endif
                    " From aid_dclose1 " +
                    " Group by 1 " +
#if PG
                    "  "
#else
                    " Into temp aid_dclose With no log "
#endif
                    , true);
                if (!ret.result)
                {
                    ExecSQL(conn_db, " Drop table aid_i" + paramcalc.pref, false);
                    ExecSQL(conn_db, " Drop table aid_d" + paramcalc.pref, false);
                    ExecSQL(conn_db, " Drop table aid_dclose", false);
                    ExecSQL(conn_db, " Drop table aid_dclose1", false);
                    return false;
                }

                ExecSQL(conn_db, " Create unique index ix_aid22_cl on aid_dclose (nzp_counter, dat_close) ", true);
                ExecSQL(conn_db, sUpdStat + " aid_dclose ", true);

                //потом max между dat_close и dat_799 (период действия)
                ret = ExecSQL(conn_db,
                    " Insert into aid_i" + paramcalc.pref + " (nzp_counter, dat_po, dat_s) " +
                    " Select " + sUniqueWord + " nzp_counter, " + sPublicForMDY + "mdy(1,1,3000), " +
                    " ( case when dat_close >=date('" + dat_799.ToShortDateString() + "')" +
                      " then dat_close else date('" + dat_799.ToShortDateString() + "') end )+1 " +
                    " From aid_dclose "
                    , true);
                if (!ret.result)
                {
                    ExecSQL(conn_db, " Drop table aid_i" + paramcalc.pref, false);
                    ExecSQL(conn_db, " Drop table aid_d" + paramcalc.pref, false);
                    ExecSQL(conn_db, " Drop table aid_dclose", false);
                    ExecSQL(conn_db, " Drop table aid_dclose1", false);
                    return false;
                }

                ExecSQL(conn_db, " Drop table aid_dclose", false);
                ExecSQL(conn_db, " Drop table aid_dclose1", false);


                //выберем все даты поверки
                ExecSQL(conn_db, " Drop table aid_d" + paramcalc.pref, false);

                ret = ExecSQL(conn_db,
                    " Create temp table aid_d" + paramcalc.pref +
                    //" Create table aid_d" + paramcalc.pref +
                    " ( nzp_counter integer, " +
                    "   dp  date, " +
                    "   dpn date  " +
#if PG
                    " ) "
#else
                    " ) With no log "
#endif
                    //" ) "
                    , true);
                if (!ret.result)
                {
                    ExecSQL(conn_db, " Drop table aid_i" + paramcalc.pref, false);
                    ExecSQL(conn_db, " Drop table aid_d" + paramcalc.pref, false);
                    return false;
                }

                ret = ExecSQL(conn_db,
                    " Insert into aid_d" + paramcalc.pref + " (nzp_counter, dp, dpn) " +
                    " Select " + sUniqueWord + " nzp_counter, " + sNvlWord + "(dat_prov, " + sPublicForMDY + "mdy(1,1,1923)), " + 
                      sNvlWord + "(dat_provnext, " + sPublicForMDY + "mdy(1,1,3000)) " +
                    " From " + arr[i] +
                    " Where is_actual <> 100 " + swhere +
                    "   and nzp_counter not in ( Select nzp_counter From aid_i" + paramcalc.pref + ") "
                    , true);
                if (!ret.result)
                {
                    ExecSQL(conn_db, " Drop table aid_i" + paramcalc.pref, false);
                    ExecSQL(conn_db, " Drop table aid_d" + paramcalc.pref, false);
                    return false;
                }

                if (i == 0)
                {
                    ExecSQL(conn_db, " Create index ix_aid11_" + paramcalc.pref + " on aid_d" + paramcalc.pref + " (nzp_counter, dp) ", true);
                }
                ExecSQL(conn_db, sUpdStat + " aid_d" + paramcalc.pref, true);

                //начинаем анализировать даты поверки
                //лейтмотив следующий - показании не действуют после dpn до следующего dp
                ret = ExecSQL(conn_db,
                    " Insert into " + "aid_i" + paramcalc.pref + " (nzp_counter, dat_s) " +
                    " Select " + sUniqueWord + " nzp_counter, dpn " +
                    " From aid_d" + paramcalc.pref +
                    " Where dpn < " + sPublicForMDY + "mdy(1,1,3000) "
                    , true);
                if (!ret.result)
                {
                    ExecSQL(conn_db, " Drop table aid_i" + paramcalc.pref, false);
                    ExecSQL(conn_db, " Drop table aid_d" + paramcalc.pref, false);
                    return false;
                }
                //if (i == 0)
                //{
                //    ExecSQL(conn_db, " Create index ix_aid22_" + rashod.paramcalc.pref + " on aid_i" + rashod.paramcalc.pref + " (nzp_counter, dat_s, dat_po) ", true);
                //}
                ExecSQL(conn_db, sUpdStat + " aid_i" + paramcalc.pref, true);

                string sql =
                        " Update aid_i" + paramcalc.pref +
                        " Set dat_po = ( Select min(dp) From aid_d" + paramcalc.pref + " a " +
                                       " Where aid_i" + paramcalc.pref + ".nzp_counter = a.nzp_counter " +
                                       "   and aid_i" + paramcalc.pref + ".dat_s <= a.dp ) " +
                        " Where 0 < ( Select count(*) From aid_d" + paramcalc.pref + " a " +
                                    " Where aid_i" + paramcalc.pref + ".nzp_counter = a.nzp_counter )";

                if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    ExecByStep(conn_db, "aid_i" + paramcalc.pref, "nzp_key", sql, 50000, " ", out ret);
                }
                if (!ret.result)
                {
                    ExecSQL(conn_db, " Drop table aid_i" + paramcalc.pref, false);
                    ExecSQL(conn_db, " Drop table aid_d" + paramcalc.pref, false);
                    return false;
                }


            }
            ExecSQL(conn_db, " Drop table aid_d" + paramcalc.pref, false);
        
            return true;
        }
        #endregion Функция пересоздания временных таблиц по префиксам

        #region Функция пересоздания всех временных таблиц DropTempTablesRahod
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
            ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
#if PG
            ExecSQL(conn_db, " Drop view ttt_aid_norma ", false);
#else
            ExecSQL(conn_db, " Drop table ttt_aid_norma ", false);
#endif
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
            
        }
        #endregion Функция пересоздания всех временных таблиц DropTempTablesRahod

        #region Функция заполнения других временных таблиц LoadTempTableOther
        //--------------------------------------------------------------------------------
        void LoadTempTableOther(IDbConnection conn_db, ParamCalc paramcalc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            string s   = "  Where 1 = 1 ";
            string ss  = "  Where 1 = 1 ";
            string sss = "  Where 1 = 1 ";
            if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0)
            {
                s  = "  ,t_selkvar b Where a.nzp_kvar = b.nzp_kvar ";
                ss = "  ,t_selkvar b Where a.nzp = b.nzp_kvar ";
                sss = " Where 0<(select count(*) from t_selkvar b Where a.nzp = b.nzp_dom) ";
            }

            //counters
            ExecSQL(conn_db, " Drop table temp_counters", false);
            ret = ExecSQL(conn_db,
#if PG
                " Select a.* Into temp temp_counters From " + paramcalc.pref + "_data.counters a " +
                    s + "   and a.is_actual <> 100 " 
#else
                " Select a.* From " + paramcalc.pref +"_data:counters a " +
                    s + "   and a.is_actual <> 100 " +
                " Into temp temp_counters With no log "
#endif
                , true);
            if (!ret.result)
            {
                //ret.text = " ";
                return;
            }

            //построить индексы
            ret = ExecSQL(conn_db, " Create index ix1_temp_counters on temp_counters (nzp_cr) ", true);
            ret = ExecSQL(conn_db, " Create index ix2_temp_counters on temp_counters (nzp_kvar,num_ls,nzp_counter, dat_uchet) ", true);
            ret = ExecSQL(conn_db, " Create index ix3_temp_counters on temp_counters (nzp_kvar, nzp_serv, nzp_cnttype, num_cnt, dat_uchet ) ", true);
            ret = ExecSQL(conn_db, " Create index ix4_temp_counters on temp_counters (nzp_counter, dat_uchet) ", true);
            ExecSQL(conn_db, sUpdStat + " temp_counters ", true);


            //counters_spis
            ExecSQL(conn_db, " Drop table temp_cnt_spis", false);
            ret = ExecSQL(conn_db,
                " Select a.* "+
#if PG
                " Into temp temp_cnt_spis  "+
#else
                " " +
#endif
                "From " + paramcalc.data_alias + "counters_spis a " +
                ss  + " and a.nzp_type=3 and a.is_actual <> 100 " +
                " Union all" +
                " Select a.* From " + paramcalc.data_alias + "counters_spis a " + 
                sss + " and a.nzp_type=1 and a.is_actual <> 100 " +
#if PG
                "  "
#else
                " Into temp temp_cnt_spis With no log "
#endif
                , true);
            if (!ret.result)
            {
                //ret.text = " ";
                return;
            }

            //построить индексы
            ret = ExecSQL(conn_db, " Create index ix1_temp_cnt_spis on temp_cnt_spis (nzp_counter) ", true);
            ret = ExecSQL(conn_db, " Create index ix2_temp_cnt_spis on temp_cnt_spis (nzp, nzp_counter) ", true);
            ret = ExecSQL(conn_db, " Create index ix3_temp_cnt_spis on temp_cnt_spis (nzp, nzp_serv, nzp_cnttype, num_cnt) ", true);
            ExecSQL(conn_db, sUpdStat + " temp_cnt_spis ", true);


            //nedop_kvar
            ExecSQL(conn_db, " Drop table ttc_nedo ", false);
            ret = ExecSQL(conn_db,
                " Select a.nzp_nedop, a.nzp_kvar, a.nzp_serv, a.nzp_supp, " +
                        sNvlWord + "(a.dat_s, " + MDY(1,1,1901)+") as dat_s ," + 
                        sNvlWord + "(a.dat_po," + MDY(1,1,3000)+") as dat_po, a.tn, a.nzp_kind " +
#if PG
                        " Into temp ttc_nedo  " +
#else
                        " " +
#endif
                " From " + paramcalc.data_alias + "nedop_kvar a " +
                    s + " and a.is_actual <> 100 " +
#if PG
                    " "
#else
                    " Into temp ttc_nedo With no log "
#endif
                , true);
            if (!ret.result)
            {
                //ret.text = " ";
                return;
            }
            ret = ExecSQL(conn_db, " Create unique index ix1_ttc_nedo on ttc_nedo (nzp_nedop) ", true);
            ret = ExecSQL(conn_db, " Create index ix3_ttc_nedo on ttc_nedo (nzp_kvar, nzp_serv, nzp_supp, nzp_kind, tn ) ", true);
            ret = ExecSQL(conn_db, " Create index ix4_ttc_nedo on ttc_nedo (nzp_kvar, dat_s, dat_po) ", true);
            ret = ExecSQL(conn_db, " Create index ix5_ttc_nedo on ttc_nedo (dat_s, dat_po) ", true);
            ExecSQL(conn_db, sUpdStat + " ttc_nedo ", true);


            //tarif
            ExecSQL(conn_db, " Drop table ttc_tarif ", false);
            ret = ExecSQL(conn_db,
                " Select a.nzp_tarif, a.nzp_kvar, a.nzp_serv, a.nzp_supp, a.nzp_frm, "+
#if PG
                "coalesce(a.dat_s, "+MDY(1,1,1901)+") as dat_s , coalesce(a.dat_po, " + MDY(1,1,3000) +") as dat_po " +
#else
                "nvl(a.dat_s, mdy(1,1,1901)) as dat_s , nvl(a.dat_po, mdy(1,1,3000) ) as dat_po " +
#endif
#if PG
                " Into temp ttc_tarif " +
#else
                "  "+
#endif
                " From " + paramcalc.data_alias + "tarif a " +
                    s + " and a.is_actual <> 100 " +
#if PG
                    "  "
#else
                    " Into temp ttc_tarif With no log "
#endif
                , true);
            if (!ret.result)
            {
                //ret.text = " ";
                return;
            }
            ret = ExecSQL(conn_db, " Create unique index ix1_ttc_tarif on ttc_tarif (nzp_tarif) ", true);
            ret = ExecSQL(conn_db, " Create index ix3_ttc_tarif on ttc_tarif (nzp_kvar, nzp_serv, nzp_supp, nzp_frm ) ", true);
            ret = ExecSQL(conn_db, " Create index ix4_ttc_tarif on ttc_tarif (nzp_kvar, dat_s, dat_po) ", true);
            ret = ExecSQL(conn_db, " Create index ix5_ttc_tarif on ttc_tarif (dat_s, dat_po) ", true);
            ExecSQL(conn_db, sUpdStat + " ttc_tarif ", true);


            //prm_1
            if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0)
                s = "  ,t_selkvar b Where a.nzp = b.nzp_kvar ";
            else
                s = "  Where 1 = 1 ";
#if PG
            ExecSQL(conn_db, " Drop view ttt_prm_1f ", false);
#else
            ExecSQL(conn_db, " Drop table ttt_prm_1f ", false);
#endif
            ret = ExecSQL(conn_db,
#if PG
                "Create temp view ttt_prm_1f as "+
#endif
                " Select a.nzp_key, a.nzp,a.nzp_prm, replace( " +
#if PG
                    "coalesce(a.val_prm,'0'), ',', '.') val_prm, coalesce(a.dat_s, " + MDY(1, 1, 1901) + ") as dat_s , coalesce(a.dat_po, " + MDY(1, 1, 3000) + ") as dat_po " +
#else
                    "nvl(a.val_prm,'0'), ',', '.') val_prm, nvl(a.dat_s, " + MDY(1, 1, 1901) + ") as dat_s , nvl(a.dat_po, mdy(1,1,3000)) as dat_po " +
#endif
                    " From " + paramcalc.data_alias + "prm_1 a " +
                    s + " and a.is_actual <> 100 " +
#if PG
                    " "
#else
                    " Into temp ttt_prm_1f with no log "
#endif
, true);
            if (!ret.result)
            {
                return;
            }
#if PG
#else
            ExecSQL(conn_db, " Create unique index ix1_ttt_prm_1f on ttt_prm_1f (nzp_key) ", true);
            ExecSQL(conn_db, " Create index ix2_ttt_prm_1f on ttt_prm_1f (nzp,dat_s,dat_po) ", true);
            ExecSQL(conn_db, " Create index ix3_ttt_prm_1f on ttt_prm_1f (dat_s,dat_po) ", true);
            ExecSQL(conn_db, " Update statistics for table ttt_prm_1f ", true);
#endif


            //prm_2
            if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0)
                s = "  ,t_selkvar b Where a.nzp = b.nzp_dom ";
            else
                s = "  Where 1 = 1 ";
#if PG
            ExecSQL(conn_db, " Drop view ttt_prm_2f ", false);
#else
            ExecSQL(conn_db, " Drop table ttt_prm_2f ", false);
#endif
            ret = ExecSQL(conn_db,
#if PG
                "Create temp view ttt_prm_2f as " +
                " Select " +
                "distinct a.nzp_key, a.nzp, a.nzp_prm, replace( coalesce(a.val_prm,'0'), ',', '.') val_prm, coalesce(a.dat_s, "+MDY(1,1,1901)+") as dat_s , coalesce(a.dat_po, "+MDY(1,1,3000)+") as dat_po " +
                " From " + paramcalc.pref +
                "_data.prm_2 a " +
                s + " and a.is_actual <> 100 "
#else
                " Select "+
                "unique a.nzp_key, a.nzp, a.nzp_prm, replace( nvl(a.val_prm,'0'), ',', '.') val_prm, nvl(a.dat_s, mdy(1,1,1901)) as dat_s , nvl(a.dat_po, mdy(1,1,3000) ) as dat_po " + 
                " From " + paramcalc.pref +
                "_data:prm_2 a " +
                s + " and a.is_actual <> 100 " +
                " Into temp ttt_prm_2f With no log "
#endif
                , true);
            if (!ret.result)
            {
                //ret.text = " ";
                return;
            }
#if PG
#else
            ExecSQL(conn_db, " Create unique index ix1_ttt_prm_2f on ttt_prm_2f (nzp_key) ", true);
            ExecSQL(conn_db, " Create index ix2_ttt_prm_2f on ttt_prm_2f (nzp,dat_s,dat_po) ", true);
            ExecSQL(conn_db, " Create index ix3_ttt_prm_2f on ttt_prm_2f (dat_s,dat_po) ", true);
            ExecSQL(conn_db, " Update statistics for table ttt_prm_2f ", true);
#endif


            //counters_dom
            ExecSQL(conn_db, " Drop table temp_counters_dom ", false);
            ret = ExecSQL(conn_db,
                " CREATE temp TABLE temp_counters_dom(" +
                //" CREATE TABLE are.temp_counters_dom(" +
                "   nzp_crd INTEGER," +
                "   nzp_dom INTEGER,"+
                "   nzp_serv INTEGER,"+
                "   nzp_cnttype INTEGER,"+
                "   num_cnt CHAR(20),"+
                "   dat_prov DATE,"+
                "   dat_provnext DATE,"+
                "   dat_uchet DATE,"+
                "   val_cnt FLOAT,"+
                "   kol_gil_dom INTEGER,"+
                "   is_actual INTEGER,"+
                "   nzp_user INTEGER,"+
                "   comment CHAR(200),"+
                "   sum_pl " + sDecimalType + "(14,2)," +
                "   is_doit INTEGER default 1 NOT NULL,"+
                "   is_pl INTEGER,"+
                "   sum_otopl " + sDecimalType + "(14,2)," +
                "   cnt_ls INTEGER,"+
                "   dat_when DATE,"+
                "   is_uchet_ls INTEGER default 0 NOT NULL,"+
                "   nzp_cntkind INTEGER default 1,"+
                "   nzp_measure INTEGER,"+
                "   is_gkal INTEGER,"+
                "   ngp_cnt " + sDecimalType + "(14,7) default 0.0000000," +
                "   cur_unl INTEGER default 0,"+
                "   nzp_wp INTEGER default 1,"+
                "   ngp_lift " + sDecimalType + "(14,7) default 0.0000000," +
                "   dat_oblom DATE,"+
                "   dat_poch DATE,"+
                "   dat_close DATE,"+
                "   nzp_counter INTEGER default 0 NOT NULL,"+
                "   month_calc DATE,"+
                "   user_del INTEGER,"+
                "   dat_del DATE,"+
                "   dat_s DATE,"+
                "   dat_po DATE " +
#if PG
                "   ) "
#else
                "   ) with no log "
#endif
                , true);
            if (!ret.result)
            {
                //ret.text = " ";
                return;
            }


            if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0)
                s = "  Where a.nzp_dom in (select b.nzp_dom from t_selkvar b) ";
            else
                s = "  Where 1 = 1 ";

            ret = ExecSQL(conn_db,
                " insert into temp_counters_dom" + 
                " Select a.* From " + paramcalc.data_alias + "counters_dom a " +
                    s + "   and a.is_actual <> 100 "
                //+ " Into temp temp_counters_dom With no log "
                , true);
            if (!ret.result)
            {
                //ret.text = " ";
                return;
            }

            //построить индексы
            ret = ExecSQL(conn_db, " Create index ix1_temp_counters_dom on temp_counters_dom (nzp_crd) ", true);
            ret = ExecSQL(conn_db, " Create index ix2_temp_counters_dom on temp_counters_dom (nzp_dom,nzp_counter, dat_uchet) ", true);
            ret = ExecSQL(conn_db, " Create index ix3_temp_counters_dom on temp_counters_dom (nzp_dom, nzp_serv, nzp_cnttype, num_cnt, dat_uchet ) ", true);
            ret = ExecSQL(conn_db, " Create index ix4_temp_counters_dom on temp_counters_dom (nzp_counter, dat_uchet) ", true);
            ExecSQL(conn_db, sUpdStat + " temp_counters_dom ", true);

        }
        #endregion Функция заполнения других временных таблиц

        #region Выбрать временные таблицы для текущего месяца LoadTempTablesForMonth
        //--------------------------------------------------------------------------------
        void LoadTempTablesForMonth(IDbConnection conn_db, ParamCalc paramcalc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            //nedop_kvar
            ExecSQL(conn_db, " Drop table temp_table_nedop ", false);
            ret = ExecSQL(conn_db,
                " Select " + sUniqueWord + " n.*, k.nzp_dom, k.num_ls " +
#if PG
                " Into temp temp_table_nedop " +
#else
                " " +
#endif
                " From ttc_nedo n, t_opn k " +
                " Where n.nzp_kvar = k.nzp_kvar " +
                "   and n.dat_s  <= " + paramcalc.dat_po +
                "   and n.dat_po >= " + paramcalc.dat_s +
#if PG
                " "
#else
                " Into temp temp_table_nedop With no log "
#endif
                , true);
            if (!ret.result)
            {
                return;
            }
            //построить индексы
            ret = ExecSQL(conn_db, " Create index ix1_temp_table_nedop on temp_table_nedop (nzp_nedop) ", true);
            ret = ExecSQL(conn_db, " Create index ix2_temp_table_nedop on temp_table_nedop (nzp_kvar,num_ls, tn) ", true);
            ret = ExecSQL(conn_db, " Create index ix21_temp_table_nedop on temp_table_nedop (nzp_kvar, tn) ", true);
            ret = ExecSQL(conn_db, " Create index ix3_temp_table_nedop on temp_table_nedop (nzp_kvar, nzp_serv, nzp_kind ) ", true);
            ret = ExecSQL(conn_db, " Create index ix4_temp_table_nedop on temp_table_nedop (nzp_dom) ", true);
#if PG
            ExecSQL(conn_db, " analyze temp_table_nedop ", true);
#else
ExecSQL(conn_db, " Update statistics for table temp_table_nedop ", true);
#endif

            string s = "";
            if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0)
                s = "  ,t_selkvar b Where a.nzp_kvar = b.nzp_kvar ";
            else
                s = "  Where 1 = 1 ";

            //tarif
            ExecSQL(conn_db, " Drop table temp_table_tarif ", false);
            string sfind_tmp =
                " Select a.* "+
#if PG
                " Into temp temp_table_tarif "+
#else

#endif
                "From ttc_tarif a " +
                    s +
                "   and a.dat_s  <= " + paramcalc.dat_po +
                "   and a.dat_po >= " + paramcalc.dat_s
#if PG
                ;
#else
                +" Into temp temp_table_tarif with no log ";
#endif
            ret = ExecSQL(conn_db, sfind_tmp, true);
            if (!ret.result)
            {
                return;
            }
            ret = ExecSQL(conn_db, " Create unique index ix1_temp_table_tariff on temp_table_tarif (nzp_tarif) ", true);
            ret = ExecSQL(conn_db, " Create index ix3_temp_table_tariff on temp_table_tarif (nzp_kvar, nzp_serv, nzp_supp, nzp_frm ) ", true);
            ret = ExecSQL(conn_db, " Create index ix4_temp_table_tarif on temp_table_tarif (nzp_kvar, dat_s, dat_po) ", true);
            ret = ExecSQL(conn_db, " Create index ix5_temp_table_tarif on temp_table_tarif (dat_s, dat_po) ", true);
#if PG
            ExecSQL(conn_db, " analyze temp_table_tarif ", true);
#else
            ExecSQL(conn_db, " Update statistics for table temp_table_tarif ", true);
#endif



            //prm_1
            if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0)
                s = "  ,t_selkvar b Where a.nzp = b.nzp_kvar ";
            else
                s = "  Where 1 = 1 ";
#if PG
            ExecSQL(conn_db, " Drop view ttt_prm_1 ", false);
#else
            ExecSQL(conn_db, " Drop table ttt_prm_1 ", false);
#endif
            ret = ExecSQL(conn_db,
#if PG
                "Create temp view ttt_prm_1 as " +
#endif
                " Select min(a.nzp_key) nzp_key,a.nzp,a.nzp_prm,max(a.val_prm) val_prm,min(a.dat_s) dat_s,max(a.dat_po) dat_po "+
                "From ttt_prm_1f a " +
                s + 
                "   and a.dat_s  <= " + paramcalc.dat_po +
                "   and a.dat_po >= " + paramcalc.dat_s +
                " group by 2,3 "
#if PG
#else
                +" Into temp ttt_prm_1 with no log "
#endif
                , true);
            if (!ret.result)
            {
                return;
            }
#if PG
#else
            ExecSQL(conn_db, " Create index ix1_ttt_prm_1 on ttt_prm_1 (nzp,nzp_prm) ", true);
            ExecSQL(conn_db, " Create index ix2_ttt_prm_1 on ttt_prm_1 (nzp_prm) ", true);
            ExecSQL(conn_db, " Update statistics for table ttt_prm_1 ", true);
#endif


            //prm_2
            if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0)
                s = "  ,t_selkvar b Where a.nzp = b.nzp_dom ";
            else
                s = "  Where 1 = 1 ";

            ExecSQL(conn_db, " Drop table ttt_prm_2 ", false);
            ret = ExecSQL(conn_db,
                " Select min(a.nzp_key) nzp_key,a.nzp,a.nzp_prm,max(a.val_prm) val_prm,min(a.dat_s) dat_s,max(a.dat_po) dat_po "+
#if PG
                " Into temp ttt_prm_2  "+
#else
#endif
                "From ttt_prm_2f a " +
                    s + 
                "   and a.dat_s  <= " + paramcalc.dat_po +
                "   and a.dat_po >= " + paramcalc.dat_s +
                " group by 2,3 "
#if PG
#else
                +" Into temp ttt_prm_2 with no log "
#endif
                , true);
            if (!ret.result)
            {
                return;
            }
            ExecSQL(conn_db, " Create index ix1_ttt_prm_2 on ttt_prm_2 (nzp,nzp_prm) ", true);
            ExecSQL(conn_db, " Create index ix2_ttt_prm_2 on ttt_prm_2 (nzp_prm) ", true);
#if PG
            ExecSQL(conn_db, " analyze ttt_prm_2 ", true);
#else
            ExecSQL(conn_db, " Update statistics for table ttt_prm_2 ", true);
#endif



        }
        #endregion Выбрать временные таблицы для текущего месяца

        #region Удаление  временных таблиц DropTempTables
        //--------------------------------------------------------------------------------
        void DropTempTables(IDbConnection conn_db, string pref)
        //--------------------------------------------------------------------------------
        {
            ExecSQL(conn_db, " Drop table temp_counters", false);
            ExecSQL(conn_db, " Drop table temp_cnt_spis", false);
            ExecSQL(conn_db, " Drop table temp_counters_dom", false);
            ExecSQL(conn_db, " Drop table aid_i" + pref, false);

            ExecSQL(conn_db, " Drop table ttc_nedo", false);
            ExecSQL(conn_db, " Drop table ttc_tarif", false);
#if PG
            ExecSQL(conn_db, " Drop view ttt_prm_1f ", false);
            ExecSQL(conn_db, " Drop view ttt_prm_2f", false);
#else
            ExecSQL(conn_db, " Drop table ttt_prm_1f ", false);
            ExecSQL(conn_db, " Drop table ttt_prm_2f", false);
#endif
            DropTempTablesForMonth(conn_db);
        }
        #endregion Удаление  временных таблиц DropTempTables

        #region Удаление  временных таблиц месяца DropTempTablesForMonth
        //--------------------------------------------------------------------------------
        void DropTempTablesForMonth(IDbConnection conn_db)
        //--------------------------------------------------------------------------------
        {
            ExecSQL(conn_db, " Drop table temp_table_tarif", false);
            ExecSQL(conn_db, " Drop table temp_table_tarif", false);
            ExecSQL(conn_db, " Drop table ttt_prm_1 ", false);
            ExecSQL(conn_db, " Drop table ttt_prm_2", false);

        }

        #endregion Удаление  временных таблиц месяца DropTempTablesForMonth

        #region Заполнение  временных вспомогательных таблиц  LoadTempTablesRashod
        //--------------------------------------------------------------------------------
        public void LoadTempTablesRashod(IDbConnection conn_db, ParamCalc paramcalc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            LoadTempTableOther(conn_db, paramcalc, out ret);
            if (!ret.result)
            {
                //DropTempTablesRahod(conn_db, paramcalc.pref);
                return;
            }

            //выбрать некондиционные интервалы
            Cnt_NotCond(conn_db, paramcalc, 0, out ret);
            if (!ret.result)
            {
                //DropTempTablesRahod(conn_db, paramcalc.pref);
                return;
            }
        }
        #endregion Заполнение  временных вспомогательных таблиц  LoadTempTablesRashod

        #region Заполнение таблиц deltaxx по текущему месяцу  - UseDeltaCntsForMonth
        //--------------------------------------------------------------------------------
        bool UseDeltaCntsForMonth(IDbConnection conn_db, Rashod rashod, ParamCalc paramcalc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            ExecSQL(conn_db, " Drop table ttt_ans_delta", false);
            if ((rashod.paramcalc.cur_yy == rashod.paramcalc.calc_yy) && (rashod.paramcalc.cur_mm == rashod.paramcalc.calc_mm))
            {
                // записать в стек =4 перекидки по объемам в текщем расчетном месяце

                ret = ExecSQL(conn_db,
                      " delete from " + rashod.delta_xx_cur +
                      " Where stek=4 and year_=" + rashod.paramcalc.cur_yy + " and month_=" + rashod.paramcalc.cur_mm + 
                      " and nzp_kvar in (select nzp_kvar from t_selkvar) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ret = ExecSQL(conn_db,
                      " delete from " + rashod.delta_xx_cur +
                      " Where stek=104 and nzp_kvar in (select nzp_kvar from t_selkvar) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                string perekidka_tab = paramcalc.pref + "_charge_" + (rashod.paramcalc.cur_yy - 2000).ToString("00") + tableDelimiter + "perekidka ";

#if PG
                string serial_fld = "";
                string serial_val = "";
#else
                string serial_fld = ",nzp_delta";
                string serial_val = ",0";
#endif
                ret = ExecSQL(conn_db,
                      " Insert into " + rashod.delta_xx_cur +
                      " (nzp_kvar, nzp_dom, nzp_serv, year_, month_, stek, val1, val2, val3, cnt_stage, kod_info, is_used" + serial_fld + ")" +
                      " Select p.nzp_kvar, k.nzp_dom, p.nzp_serv, " + rashod.paramcalc.cur_yy + " year_, p.month_, 4 stek," +
                      " sum(p.volum) val1,0 val2,0 val3,0 cnt_stage,0 kod_info,0 is_used" + serial_val + 
                      " From " + perekidka_tab +" p, t_selkvar k " +
                      " Where p.nzp_kvar=k.nzp_kvar and p.type_rcl>100 and abs(p.volum)>0 and p.month_= " + rashod.paramcalc.cur_mm + 
                      " group by 1,2,3,4,5 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                //
                // ... beg если включен самарский алгоритм учета перерасчета расходов в расходе тек.месяца
                //1988|Учет перерасчета расходов ИПУ для Самары в текущем месяце|||bool||5||||
                IDbCommand cmd1988 = DBManager.newDbCommand(
                    " select count(*) cnt " +
                    " from " + paramcalc.data_alias + "prm_5 p " +
                    " where p.nzp_prm=1988 and p.is_actual<>100 "
                , conn_db);
                bool bUchetRecalcRash = false;
                try
                {
                    string scntvals = Convert.ToString(cmd1988.ExecuteScalar());
                    bUchetRecalcRash = (Convert.ToInt32(scntvals) > 0);
                }
                catch
                {
                    bUchetRecalcRash = false;
                }
                if (bUchetRecalcRash)
                {
                    string revalXX_tab = paramcalc.pref + "_charge_" + (rashod.paramcalc.cur_yy - 2000).ToString("00") + tableDelimiter +
                        "reval_" + (rashod.paramcalc.cur_mm).ToString("00");

                    ExecSQL(conn_db, " Drop table t_recalc_cnt ", false);
                    ExecSQL(conn_db, " Drop table t_recalc_all ", false);

                    ret = ExecSQL(conn_db,
                        " select b.nzp_kvar,k.nzp_dom,b.nzp_serv,b.nzp_supp,b.tarif,b.year_,b.month_," +
                        " sum(b.sum_tarif-b.sum_tarif_p) reval_tarif,max(f.nzp_measure) nzp_mea," +
                        " sum(b.c_calc) c_calc,sum(b.c_calcm_p) c_calcm_p,sum(b.c_calc_p) c_calc_p," +
                        " max(b.type_rsh) type_rsh,max(" + sPublicForMDY + "mdy(b.month_p,1,b.year_p)) dat_charge_p " +
#if PG
                        " into temp t_recalc_cnt "+
#else
#endif
                        " from " + revalXX_tab + " b, " + paramcalc.kernel_alias + "formuls f" +
                        ", t_selkvar k" +
                        " where k.nzp_kvar=b.nzp_kvar and b.nzp_frm=f.nzp_frm" +
                        " and b.tarif>0 and b.nzp_serv in (6,9,14,25) and f.nzp_measure in (3,4,5) " +
                        " and abs(b.reval) > 0.0001 " +
                        " and 0<(select count(*) from temp_counters a where b.nzp_kvar=a.nzp_kvar" +
                          " and (case when b.nzp_serv=14 then 9 else b.nzp_serv end)=a.nzp_serv" +
                          " and a.month_calc='01." + rashod.paramcalc.cur_mm + "." + rashod.paramcalc.cur_yy + "')" +
                        " group by 1,2,3,4,5,6,7 " +

                        " union all " +

                        " select b.nzp_kvar,k.nzp_dom,b.nzp_serv,b.nzp_supp,b.tarif,b.year_,b.month_," +
                        " sum(b.sum_tarif-b.sum_tarif_p) reval_tarif,max(f.nzp_measure) nzp_mea," +
                        " sum(b.c_calc) c_calc,sum(b.c_calcm_p) c_calcm_p,sum(b.c_calc_p) c_calc_p," +
                        " max(b.type_rsh) type_rsh,max(" + sPublicForMDY + "mdy(b.month_p,1,b.year_p)) dat_charge_p " +
                        " from " + revalXX_tab + " b, " + paramcalc.kernel_alias + "formuls f" +
                        ", t_selkvar k" +
                        " where k.nzp_kvar=b.nzp_kvar and b.nzp_frm=f.nzp_frm" +
                        " and b.tarif>0 and b.nzp_serv=7 and f.nzp_measure=3 " +
                        " and abs(b.reval) > 0.0001 " +
                        " and 0<(select count(*) from temp_counters a where b.nzp_kvar=a.nzp_kvar" +
                          " and a.nzp_serv in (6,9)" +
                          " and a.month_calc='01." + rashod.paramcalc.cur_mm + "." + rashod.paramcalc.cur_yy + "')" +
                        " group by 1,2,3,4,5,6,7 "

#if PG
#else
                        + " into temp t_recalc_cnt with no log "
#endif
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    ret = ExecSQL(conn_db, " create index ix2_recalc_cnt on t_recalc_cnt(nzp_kvar,nzp_serv,nzp_supp) ", true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    ret = ExecSQL(conn_db, sUpdStat + " t_recalc_cnt ", true);

                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    //
                    ExecSQL(conn_db, " Drop table t_recalc_all", false);

                    ret = ExecSQL(conn_db,
                          " Select 0 nzp_delta, p.nzp_kvar, p.nzp_dom, p.nzp_serv, p.year_, p.month_," +
                          " max(p.tarif) val1, max(p.tarif) val2," +
                          " sum(case when p.nzp_mea=4 and p.nzp_serv=9 then p.reval_tarif/(p.tarif*0.0611) else  p.reval_tarif/p.tarif end) val3," +
                          " sum(case when p.nzp_mea=4 and p.nzp_serv=9 then p.c_calc/0.0611 else p.c_calc end) c_calc," +
                          " sum(case when p.nzp_mea=4 and p.nzp_serv=9 and p.type_rsh<>0 then p.c_calcm_p/0.0611 else p.c_calcm_p end) c_calcm_p," +
                          " sum(p.c_calc_p) c_calc_p," +
                          " 0 cnt_stage,-1 kod_info,0 is_used," +
                          " max(p.tarif) valm,max(p.tarif) valm_p, max(p.tarif) dop87,max(p.tarif) dop87_p," +
                          " max(p.type_rsh) type_rsh,max(p.dat_charge_p) dat_charge_p,0 is_good_rsh,max(p.tarif) rsh_all " +
#if PG
                          " into temp t_recalc_all  "+
#else
#endif
                          " From t_recalc_cnt p Where 1=1" +
                          " group by 2,3,4,5,6 "
#if PG
#else
                         +" into temp t_recalc_all with no log "
#endif
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    ret = ExecSQL(conn_db, " create index ix_recalc_all on t_recalc_all(nzp_kvar,nzp_serv) ", true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    ret = ExecSQL(conn_db, sUpdStat + " t_recalc_all ", true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    ret = ExecSQL(conn_db, " update t_recalc_all set val1=0,val2=0,valm=0,valm_p=0,dop87=0,dop87_p=0,rsh_all=0 where 1=1 ", true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    //

                    //ViewTbl(conn_db, " select * from t_recalc_all ");

                    MyDataReader reader;

                    string sDateBegin = MDY(07,01,2013);
                    string countersXX_tab = "";
                    // 1. проставим расходы за текущий месяц расчета (начисляемый)
                    ret = ExecRead(conn_db, out reader,
                        " Select year_, month_, nzp_serv  From t_recalc_all " +
                        //" where mdy(month_,01,year_) >= " + sDateBegin +
                        " Group by 1,2,3 Order by 1,2,3 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    try
                    {
                        while (reader.Read())
                        {
                            //
                            int tek_yy = 0;
                            int tek_mm = 0;
                            int tek_serv = 0;
                            if (reader["month_"] != DBNull.Value)
                            {
                                tek_mm = System.Convert.ToInt32(reader["month_"]);

                            }
                            if (reader["year_"] != DBNull.Value)
                            {
                                tek_yy = System.Convert.ToInt32(reader["year_"]);

                            }
                            if (reader["nzp_serv"] != DBNull.Value)
                            {
                                tek_serv = System.Convert.ToInt32(reader["nzp_serv"]);

                            }
                            if ((tek_mm > 0) && (tek_yy > 0) && (tek_serv > 0))
                            {
                                countersXX_tab = paramcalc.pref + "_charge_" + (tek_yy - 2000).ToString("00") + tableDelimiter +
                                    "counters" + (paramcalc.cur_yy - 2000).ToString("00") + (paramcalc.cur_mm).ToString("00") + "_" +
                                    (tek_mm).ToString("00");

                                ret = ExecSQL(conn_db,
                                  " update t_recalc_all set valm=" +
                                    "(select sum(t.val1+t.val2) from " + countersXX_tab + " t" +
                                    " where t.nzp_kvar=t_recalc_all.nzp_kvar and t.nzp_serv=(case when t_recalc_all.nzp_serv=14 then 9 else t_recalc_all.nzp_serv end)" +
                                    " and t.nzp_type=3 and t.stek=3" +
                                    " ) " +
                                  " where year_=" + tek_yy + " and month_=" + tek_mm + " and nzp_serv=" + tek_serv +
                                    " and 0<" +
                                    "(select count(*) from " + countersXX_tab + " t" +
                                    " where t.nzp_kvar=t_recalc_all.nzp_kvar and t.nzp_serv=(case when t_recalc_all.nzp_serv=14 then 9 else t_recalc_all.nzp_serv end)" +
                                    " and t.nzp_type=3 and t.stek=3" +
                                    " ) "
                                    , true);
                                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                                countersXX_tab = paramcalc.pref + "_charge_" + (tek_yy - 2000).ToString("00") + tableDelimiter +
                                    "counters_" + (tek_mm).ToString("00");
                                // ОДН вегда берется из первого месяца ! - не пересчитывается
                                ret = ExecSQL(conn_db,
                                  " update t_recalc_all set dop87_p=" +
                                    "(select sum(t.dop87) from " + countersXX_tab + " t" +
                                    " where t.nzp_kvar=t_recalc_all.nzp_kvar and t.nzp_serv=(case when t_recalc_all.nzp_serv=14 then 9 else t_recalc_all.nzp_serv end)" +
                                    " and t.nzp_type=3 and t.stek=3" +
                                    " ) " +
                                  " where year_=" + tek_yy + " and month_=" + tek_mm + " and nzp_serv=" + tek_serv +
                                    " and 0<" +
                                    "(select count(*) from " + countersXX_tab + " t" +
                                    " where t.nzp_kvar=t_recalc_all.nzp_kvar and t.nzp_serv=(case when t_recalc_all.nzp_serv=14 then 9 else t_recalc_all.nzp_serv end)" +
                                    " and t.nzp_type=3 and t.stek=3" +
                                    " ) "
                                    , true);
                                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                            }
                            //
                        }
                    }
                    catch
                    {
                        ret.result = false;
                    }

                    reader.Close();

                    //ViewTbl(conn_db, " select * from t_recalc_all ");

                    // 2. проставим расходы за прошлый месяц расчета (перерасчитываемый)
                    ret = ExecRead(conn_db, out reader,
#if PG
                        " Select EXTRACT(year from dat_charge_p) year_p,EXTRACT(month from dat_charge_p) month_p," +
#else
                        " Select year(dat_charge_p) year_p,month(dat_charge_p) month_p," +
#endif
                        " nzp_serv,year_,month_  From t_recalc_all " +
                        " where (dat_charge_p >= " + sDateBegin + ") or dat_charge_p = " + MDY(1,1,1901) +
                        " Group by 1,2,3,4,5 Order by 1,2,3,4,5 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    try
                    {
                        while (reader.Read())
                        {
                            //
                            int tek_yy = 0;
                            int tek_mm = 0;
                            int tekp_yy = 0;
                            int tekp_mm = 0;
                            int tek_serv = 0;
                            if (reader["month_p"] != DBNull.Value)
                            {
                                tekp_mm = System.Convert.ToInt32(reader["month_p"]);

                            }
                            if (reader["year_p"] != DBNull.Value)
                            {
                                tekp_yy = System.Convert.ToInt32(reader["year_p"]);

                            }
                            if (reader["nzp_serv"] != DBNull.Value)
                            {
                                tek_serv = System.Convert.ToInt32(reader["nzp_serv"]);

                            }
                            if (reader["month_"] != DBNull.Value)
                            {
                                tek_mm = System.Convert.ToInt32(reader["month_"]);

                            }
                            if (reader["year_"] != DBNull.Value)
                            {
                                tek_yy = System.Convert.ToInt32(reader["year_"]);

                            }
                            if ((tek_mm > 0) && (tek_yy > 0) && (tek_serv > 0))
                            {
                                string sAliasPref = (tekp_yy - 2000).ToString("00") + (tekp_mm).ToString("00");
                                countersXX_tab = paramcalc.pref + "_charge_" + (tek_yy - 2000).ToString("00") + tableDelimiter +
                                    "counters" + sAliasPref + "_" + (tek_mm).ToString("00");
                                if ((tekp_mm == 1) && (tekp_yy == 1901))
                                {
                                    countersXX_tab = paramcalc.pref + "_charge_" + (tek_yy - 2000).ToString("00") + tableDelimiter +
                                            "counters_" + (tek_mm).ToString("00");
                                }

                                ret = ExecSQL(conn_db,
                                  " update t_recalc_all set is_good_rsh=1, valm_p=" +
                                    "(select sum(t.val1+t.val2) from " + countersXX_tab + " t" +
                                    " where t.nzp_kvar=t_recalc_all.nzp_kvar and t.nzp_serv=(case when t_recalc_all.nzp_serv=14 then 9 else t_recalc_all.nzp_serv end)" +
                                    " and t.nzp_type=3 and t.stek=3" +
                                    " ) " +
#if PG
                                    " where EXTRACT(year from dat_charge_p)=" + tekp_yy + " and EXTRACT(month from dat_charge_p)=" + tekp_mm + 
#else
                                    " where year(dat_charge_p)=" + tekp_yy + " and month(dat_charge_p)=" + tekp_mm + 
#endif
                                    " and nzp_serv=" + tek_serv +
                                    " and year_=" + tek_yy + " and month_=" + tek_mm +
                                    " and 0<" +
                                    "(select count(*) from " + countersXX_tab + " t" +
                                    " where t.nzp_kvar=t_recalc_all.nzp_kvar and t.nzp_serv=(case when t_recalc_all.nzp_serv=14 then 9 else t_recalc_all.nzp_serv end)" +
                                    " and t.nzp_type=3 and t.stek=3" +
                                    " ) "
                                    , true);
                                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                            }
                            //
                        }
                    }
                    catch
                    {
                        ret.result = false;
                    }

                    reader.Close();

                    //ViewTbl(conn_db, " select * from t_recalc_all ");

                    ret = ExecSQL(conn_db,
                          " update t_recalc_all set valm_p = (case when type_rsh<>0 then c_calcm_p else c_calc_p end) " +
                          " Where is_good_rsh = 0 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    //
                    ret = ExecSQL(conn_db,
                          " update t_recalc_all" +
                          " set rsh_all = valm + ((case when dop87<0 then dop87 else 0 end)-(case when dop87_p<0 then dop87_p else 0 end))," +
                          " val1 = (valm - valm_p) + ((case when dop87<0 then dop87 else 0 end)-(case when dop87_p<0 then dop87_p else 0 end)) " +
                          " Where 1 = 1 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    //
                    ret = ExecSQL(conn_db,
                          " Insert into " + rashod.delta_xx_cur +
                          " (nzp_kvar, nzp_dom, nzp_serv, year_, month_, stek, val1, val2, val3, cnt_stage, kod_info, is_used, valm, valm_p, dop87, dop87_p" + serial_fld + ")" +
                          " Select p.nzp_kvar, p.nzp_dom, p.nzp_serv, " +
                          " p.year_, p.month_, 104 stek," +
                          " sum( val1 ), 0 val2,sum(val3) val3,0 cnt_stage,-1 kod_info,0 is_used,sum(valm),sum(valm_p),sum(dop87),sum(dop87_p)" + serial_val +
                          " From t_recalc_all p Where 1=1" +
                          " group by 1,2,3,4,5 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    //string serial_fld = ",nzp_delta";  string serial_val = ",0";
                    ret = ExecSQL(conn_db,
                          " Insert into " + rashod.delta_xx_cur +
                          " (nzp_kvar, nzp_dom, nzp_serv, year_, month_, stek, val1, val2, val3, cnt_stage, kod_info, is_used, valm, valm_p, dop87, dop87_p" + serial_fld + ")" +
                          " Select p.nzp_kvar, p.nzp_dom, p.nzp_serv, " +
                          rashod.paramcalc.cur_yy + " year_, " + rashod.paramcalc.cur_mm + " month_, 4 stek," +
                          " sum( val1 ), 0 val2,sum(val3) val3,0 cnt_stage,-1 kod_info,0 is_used,sum(valm),sum(valm_p),sum(dop87),sum(dop87_p)" + serial_val + 
                          " From t_recalc_all p Where 1=1" +
                          " group by 1,2,3,4,5 "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    ret = ExecSQL(conn_db,
                        " update " + revalXX_tab + " set kod_info=1" +
                        " where 0<(select count(*) from t_recalc_cnt a where a.nzp_kvar=" + revalXX_tab + ".nzp_kvar and a.nzp_serv=" + revalXX_tab + ".nzp_serv" +
                        " and a.nzp_supp=" + revalXX_tab + ".nzp_supp) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    ExecSQL(conn_db, " Drop table t_recalc_cnt ", false);
                    ExecSQL(conn_db, " Drop table t_recalc_all", false);
                }
                // ... end если включен самарский алгоритм учета перерасчета расходов в расходе тек.месяца

                // в текущем расчетном месяце записать учтенные дельты в месяцы, за которые они образовались

                ret = ExecSQL(conn_db,
//                  " Create table are.ttt_ans_delta" +
                    " Create temp table ttt_ans_delta" +
                    "  ( nzp_delta  integer not null, " +
                    "    nzp_kvar   integer not null, " +
                    "    nzp_dom    integer not null, " +
                    "    nzp_serv   integer not null, " +
                    "    year_      integer not null, " +
                    "    month_     integer not null, " +
                    "    stek       integer not null, " +
                    "    val1       " + sDecimalType + "(15,7) default 0.00, " +
                    "    val2       " + sDecimalType + "(15,7) default 0.00, " +
                    "    val3       " + sDecimalType + "(15,7) default 0.00, " +
                    "    cnt_stage  integer not null, " +
                    "    kod_info   integer not null, " +
                    "    is_used    integer default 0 " +
#if PG
                    "  ) "
#else
                    "  ) with no log "
//                  "  ) "
#endif
                , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ret = ExecSQL(conn_db,
                      " Insert into ttt_ans_delta" +
                      " (nzp_delta,nzp_kvar,nzp_dom,nzp_serv,year_,month_,stek,val1,val2,val3,cnt_stage,kod_info,is_used)" +
                      " Select nzp_delta,nzp_kvar,nzp_dom,nzp_serv,year_,month_,stek,val1,val2,val3,cnt_stage,kod_info,is_used " +
                      " From " + rashod.delta_xx_cur +
                      " Where stek in (4) and " + rashod.where_dom + rashod.where_kvar
                      //" Where stek in (1,4) and " + rashod.where_dom + rashod.where_kvar
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ExecSQL(conn_db, " Create index ix1_ttt_ans_delta on ttt_ans_delta (nzp_dom,nzp_serv) ", true);
                ExecSQL(conn_db, " Create index ix2_ttt_ans_delta on ttt_ans_delta (nzp_kvar,nzp_serv,year_,month_) ", true);
                ExecSQL(conn_db, " Create index ix3_ttt_ans_delta on ttt_ans_delta (is_used,year_,month_) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_ans_delta ", true);

                //
                ExecSQL(conn_db, " Drop table ttt_dlt_dom", false);

                ret = ExecSQL(conn_db,
                      " Select nzp_dom,nzp_serv," +
                      " sum(case when kod_info= -1 then val1+val2 else 0 end) dlt," +
                      " sum(case when kod_info<>-1 then val1+val2 else 0 end) dlt_rcl" +
#if PG
                      " Into temp ttt_dlt_dom " +
#else
#endif
                      " From ttt_ans_delta " +
                      " Group by 1,2 "
#if PG
#else
                    + " Into temp ttt_dlt_dom with no log " 
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                ExecSQL(conn_db, " Create unique index ix1_ttt_dlt_dom on ttt_dlt_dom (nzp_dom,nzp_serv) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_dlt_dom ", true);

                //записать сумму дельт для ЛС дома с ОДПУ
                string sql =
                        " Update " + rashod.counters_xx + " Set " +
#if PG
                        " dlt_reval =" +
                        " (select sum(case when kod_info= -1 then val1+val2 else 0 end)" +
                        " from ttt_ans_delta k" +
                        "  where k.nzp_kvar=" + rashod.counters_xx + ".nzp_kvar and k.nzp_serv=" + rashod.counters_xx + ".nzp_serv)" +
                        ",dlt_real_charge =" +
                        " (select sum(case when kod_info<>-1 then val1+val2 else 0 end)" +
                        " from ttt_ans_delta k" +
                        "  where k.nzp_kvar=" + rashod.counters_xx + ".nzp_kvar and k.nzp_serv=" + rashod.counters_xx + ".nzp_serv)" +
#else
                        "(dlt_reval,dlt_real_charge) =" +
                        " ((select sum(case when kod_info= -1 then val1+val2 else 0 end)," +
                        " sum(case when kod_info<>-1 then val1+val2 else 0 end)" + 
                        " from ttt_ans_delta k" +
                        "  where k.nzp_kvar=" + rashod.counters_xx + ".nzp_kvar and k.nzp_serv=" + rashod.counters_xx + ".nzp_serv))" +
#endif
                        " Where nzp_type = 3 and stek = 3 and cnt_stage in (1,9) and " + rashod.where_dom +
                        "  and 0<(select count(*) from ttt_ans_delta k" +
                                " where k.nzp_kvar=" + rashod.counters_xx + ".nzp_kvar and k.nzp_serv=" + rashod.counters_xx + ".nzp_serv) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                //записать сумму дельт для домов с ОДПУ
                sql =
                        " Update " + rashod.counters_xx + " Set " +
#if PG
                        " dlt_reval =" +
                        " (select d.dlt from ttt_dlt_dom d" +
                        "  where d.nzp_dom=" + rashod.counters_xx + ".nzp_dom and d.nzp_serv=" + rashod.counters_xx + ".nzp_serv)" +
                        ",dlt_real_charge =" +
                        " (select d.dlt_rcl from ttt_dlt_dom d" +
                        "  where d.nzp_dom=" + rashod.counters_xx + ".nzp_dom and d.nzp_serv=" + rashod.counters_xx + ".nzp_serv)" +
#else
                        " (dlt_reval,dlt_real_charge) =" +
                        " ((select d.dlt,d.dlt_rcl from ttt_dlt_dom d" +
                        "  where d.nzp_dom=" + rashod.counters_xx + ".nzp_dom and d.nzp_serv=" + rashod.counters_xx + ".nzp_serv))" +
#endif
                        " Where nzp_type = 1 and stek = 3 and cnt_stage = 1 and " + rashod.where_dom +
                        "  and 0<(select count(*) from ttt_dlt_dom d" +
                                " where d.nzp_dom=" + rashod.counters_xx + ".nzp_dom and d.nzp_serv=" + rashod.counters_xx + ".nzp_serv)";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ret = ExecSQL(conn_db,
                      " Update " + rashod.delta_xx_cur + " Set is_used=1" +
                      " Where stek in (4) and " + rashod.where_dom + rashod.where_kvar
                      //" Where stek in (1,4) and " + rashod.where_dom + rashod.where_kvar
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }
            else
            {
                // при перерасчетах в перерасчетном месяце снять учтенные дельты для избежания двойного учета,
                // если дельты были когда либо учтены

                // снимаем 1 раз!
            }
            return true;
        }
        #endregion Заполнение таблиц deltaxx
        
        public DataTable ViewTbl(IDbConnection conn_db, string sql)
        {
            //return null;

            IDataReader reader;
            DataTable Data_Table= new DataTable();

                if (!ExecRead(conn_db, out reader, sql, true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                }


                if (reader != null)
                {
                    try
                    {
                        //заполнение DataTable
                        Data_Table.Load(reader, LoadOption.OverwriteChanges);
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("!!!" + ex.Message, MonitorLog.typelog.Error, true);
                        return null;
                    }

                    return Data_Table;
                }
                else return null;

        }

        #region Заполнение таблиц deltaxx по текущему месяцу в лиц счете - UseDeltaCntsLSForMonth

        //--------------------------------------------------------------------------------
        bool UseDeltaCntsLSForMonth(IDbConnection conn_db, Rashod rashod, ParamCalc paramcalc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            ExecSQL(conn_db, " Drop table ttt_ans_delta", false);
            if ((rashod.paramcalc.cur_yy == rashod.paramcalc.calc_yy) && (rashod.paramcalc.cur_mm == rashod.paramcalc.calc_mm))
            {
                // в текущем расчетном месяце записать учтенные дельты в месяцы, за которые они образовались

                ret = ExecSQL(conn_db,
                    //                    " Create table are.ttt_ans_delta" +
                    " Create temp table ttt_ans_delta" +
                    "  ( nzp_delta  integer not null, " +
                    "    nzp_kvar   integer not null, " +
                    "    nzp_dom    integer not null, " +
                    "    nzp_serv   integer not null, " +
                    "    year_      integer not null, " +
                    "    month_     integer not null, " +
                    "    stek       integer not null, " +
#if PG
                    "    val1       numeric(15,7) default 0.00, " +
                    "    val2       numeric(15,7) default 0.00, " +
                    "    val3       numeric(15,7) default 0.00, " +
#else
                    "    val1       decimal(15,7) default 0.00, " +
                    "    val2       decimal(15,7) default 0.00, " +
                    "    val3       decimal(15,7) default 0.00, " +
#endif
                    "    cnt_stage  integer not null, " +
                    "    kod_info   integer not null, " +
                    "    is_used    integer default 0 " +
#if PG
                    "  )  "
#else
                    "  ) with no log "
#endif
                    //                    "  ) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ret = ExecSQL(conn_db,
                      " Insert into ttt_ans_delta" +
                      " (nzp_delta,nzp_kvar,nzp_dom,nzp_serv,year_,month_,stek,val1,val2,val3,cnt_stage,kod_info,is_used)" +
                      " Select nzp_delta,nzp_kvar,nzp_dom,nzp_serv,year_,month_,stek,val1,val2,val3,cnt_stage,kod_info,is_used " +
                      " From " + rashod.delta_xx_cur +
                      " Where stek in (4) and " + rashod.where_dom + rashod.where_kvar
                      //" Where stek in (1,4) and " + rashod.where_dom + rashod.where_kvar
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ExecSQL(conn_db, " Create index ix1_ttt_ans_delta on ttt_ans_delta (nzp_dom,nzp_serv) ", true);
                ExecSQL(conn_db, " Create index ix2_ttt_ans_delta on ttt_ans_delta (nzp_kvar,nzp_serv,year_,month_) ", true);
                ExecSQL(conn_db, " Create index ix3_ttt_ans_delta on ttt_ans_delta (is_used,year_,month_) ", true);
#if PG
                ExecSQL(conn_db, " analyze ttt_ans_delta ", true);
#else
ExecSQL(conn_db, " Update statistics for table ttt_ans_delta ", true);
#endif
            }
            return true;
        }

        #endregion Заполнение таблиц deltaxx по текущему месяцу в лиц счете - UseDeltaCntsLSForMonth

        #region запись дельт расходов при расчете ОДН за тек.месяц без перерасчета ОДН WriteDeltaCntsForMonth
        //--------------------------------------------------------------------------------
        //
        // ... запись дельт расходов при расчете ОДН за тек.месяц без перерасчета ОДН ...
        //
        // может эта функция вообще НЕ нужна! зачем вставлять в прошлое? 
        // ведь учитывается только дельта расходов, а она рассчитывается от предыдущего расхода в counters_XX!
        // если будет перерасчет ОДН, то пусть считает от реального расхода - т.е. нужно будет пересчитать ВЕСЬ период "дурацкого" расчета ОДН
        //
        bool WriteDeltaCntsForMonth(IDbConnection conn_db, Rashod rashod, ParamCalc paramcalc, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            if ((rashod.paramcalc.cur_yy == rashod.paramcalc.calc_yy) && (rashod.paramcalc.cur_mm == rashod.paramcalc.calc_mm))
            {
                // для ОДН
                //
                ExecSQL(conn_db, " Drop table ttt_dlt_dom", false);
                ret = ExecSQL(conn_db,
                    " Create temp table ttt_dlt_dom" +
                    "  ( nzp_dom    integer not null, " +
                    "    nzp_serv   integer not null  " +
#if PG
                    "  ) "
#else
                    "  ) with no log "
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ret = ExecSQL(conn_db,
                  " insert into ttt_dlt_dom (nzp_dom, nzp_serv) " +
                  " Select nzp_dom, nzp_serv " +
                  " From " + rashod.counters_xx +
                  " Where nzp_type = 1 and stek = 3 and kod_info>0 and " + rashod.where_dom +
                  " group by 1,2 "
                , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                ExecSQL(conn_db, " Create unique index ix1_ttt_dlt_dom on ttt_dlt_dom (nzp_dom,nzp_serv) ", true);
#if PG
                ExecSQL(conn_db, " analyze ttt_dlt_dom ", true);
#else
ExecSQL(conn_db, " Update statistics for table ttt_dlt_dom ", true);
#endif

                // вставить по месяцам в прошлом
                IDataReader reader;
                ret = ExecRead(conn_db, out reader,
                      " Select a.year_, a.month_ From ttt_ans_delta a,ttt_dlt_dom d " +
                      " Where d.nzp_dom=a.nzp_dom and d.nzp_serv=a.nzp_serv and a.stek<>4 " +
                      " Group by 1,2 Order by 1,2 "
                      , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                try
                {
                    while (reader.Read())
                    {
                        int imn = (int)reader["month_"];
                        int iyr = (int)reader["year_"];

                        string delta_xx_rab = paramcalc.pref + "_charge_" + (iyr - 2000).ToString("00") +
#if PG
                            ".delta_"
#else
                            ":delta_" 
#endif
                            + imn.ToString("00");

                        ret = ExecSQL(conn_db,
                            " Insert into " + delta_xx_rab +
                            " (nzp_delta,nzp_kvar,nzp_dom,nzp_serv,year_,month_,stek,val1,val2,val3,cnt_stage,kod_info,is_used) " +
                            " Select" +
                            " 0,a.nzp_kvar,a.nzp_dom,a.nzp_serv," + rashod.paramcalc.cur_yy + "," + rashod.paramcalc.cur_mm + 
                            ", 2, a.val1,a.val2,a.val3,a.cnt_stage,a.kod_info,a.is_used " +
                            " From ttt_ans_delta a,ttt_dlt_dom d " +
                            " Where d.nzp_dom=a.nzp_dom and d.nzp_serv=a.nzp_serv and a.year_=" + iyr + " and a.month_=" + imn
                            , true);
                        if (!ret.result)
                        {
                            reader.Close();
                            DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                            return false;
                        }
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    reader.Close();
                    ret.result = false;
                    ret.text = ex.Message;
                    return false;
                }
                //
            }
            else
            {
                // при перерасчетах в перерасчетном месяце снять учтенные дельты для избежания двойного учета,
                // если дельты были когда либо учтены

                // снимаем 1 раз!
            }
            return true;
        }
        #endregion запись дельт расходов при расчете ОДН за тек.месяц без перерасчета ОДН
    #endregion Функции и структуры для подсчета расходов

    #region Функция подсчета расходов CalcRashod
        #region Вызов и инициализация функции CalcRashod(IDbConnection conn_db, ParamCalc paramcalc, out Returns ret)
        //--------------------------------------------------------------------------------
        public bool CalcRashod(IDbConnection conn_db, ParamCalc paramcalc, out Returns ret)
        //--------------------------------------------------------------------------------
        {

            ret = Utils.InitReturns();

        #endregion Вызов и инициализация функции CalcRashod(IDbConnection conn_db, ParamCalc paramcalc, out Returns ret)

            #region Выставить признак необходимости генерации средних расходов
            //----------------------------------------------------------------
            // генерация средних значений расходов ИПУ
            //----------------------------------------------------------------
            if (Constants.Trace) Utility.ClassLog.WriteLog("генерация средних значений расходов ИПУ");
            //
            //1987|Отменить расчет средних расходов ИПУ при расчете всей БД|||bool||5||||
            IDbCommand cmd1987 = DBManager.newDbCommand(
                " select count(*) cnt " +
                " from " + paramcalc.data_alias + "prm_5 p " +
                " where p.nzp_prm=1987 and p.is_actual<>100 "
            , conn_db);
            bool bDoCalcSredRash = true;
            try
            {
                string scntvals = Convert.ToString(cmd1987.ExecuteScalar());
                bDoCalcSredRash = (Convert.ToInt32(scntvals) == 0);
            }
            catch
            {
                bDoCalcSredRash = true;
            }
            #endregion Выставить признак необходимости генерации средних расходов

            #region Генерация средних расходов ИПУ
            if (bDoCalcSredRash && (paramcalc.nzp_kvar == 0) && (paramcalc.nzp_dom == 0) && (paramcalc.calc_yy == paramcalc.cur_yy) && (paramcalc.calc_mm == paramcalc.cur_mm))
            {
                ExecSQL(conn_db, " Drop table t_lsipu ", false);
                ret = ExecSQL(conn_db,
                    " Create temp table t_lsipu " +
                    " (  nzp_dom   integer not null, " +
                    "    nzp_kvar  integer not null, " +
                    "    mark      integer default 1," +
                    "    pref      char(20) " +
#if PG
                    " ) "
#else
                    " ) with no log "
#endif
                          , true);
                if (!ret.result) { conn_db.Close(); return false; }

                ret = ExecSQL(conn_db,
                    " Insert Into t_lsipu (nzp_dom,nzp_kvar,pref)" +
                    " Select nzp_dom,nzp_kvar,'" + paramcalc.pref + "' pref from t_opn Where 1=1 "
                    , true);
                if (!ret.result) { conn_db.Close(); return false; }

                ExecSQL(conn_db, sUpdStat + " t_lsipu ", false);

                int nzp_user = 1;
                Call_GenSrZnKPUWithOutConnect(conn_db, nzp_user, paramcalc.cur_yy, paramcalc.cur_mm, "t_lsipu", out ret);

                ExecSQL(conn_db, " Drop table t_lsipu ", false);
            }
            #endregion Генерация средних расходов ИПУ

            #region Описание типов стека расходов от 1 до 6
            //----------------------------------------------------------------
            //Стек расходов:
            //----------------------------------------------------------------
            // 1 - ПУ
            // 2 - отсутствие показаний, но есть ПУ (средний расход по ИПУ)
            // 3 - нормативные значения
            // 4 - средние значения
            // 5 - изменения приборов
            // 6 - 87 П
            // 7 - ....
            // 8 - ....
            #endregion Описание типов стека расходов от 1 до 6

            #region Подготовка к подсчету - заполнение временных таблиц

            Rashod rashod = new Rashod(paramcalc);

            DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
            //---------------------------------------------------
            //выбрать множество лицевых счетов
            //---------------------------------------------------
            if (!TempTableInWebCashe(conn_db, "t_selkvar"))
            {
                ChoiseTempKvar(conn_db, ref paramcalc, out ret);
                if (!ret.result)
                {
                    conn_db.Close();
                    return false;
                }
            }

            CreateCountersXX(conn_db, rashod, out ret);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            string p_dat_charge = DateNullString;
            if (!rashod.paramcalc.b_cur)
                p_dat_charge = MDY(rashod.paramcalc.cur_mm, 28, rashod.paramcalc.cur_yy);

            //----------------------------------------------------------------
            // проводится расчет 1-го л/с
            //----------------------------------------------------------------
            bool b_calc_kvar = (rashod.paramcalc.nzp_kvar > 0);

            IDataReader reader;

            #region помеченное к удалению 
            /*
            if (!rashod.paramcalc.b_cur)
            {
                //вставить строки из charge_xx в самый первый пересчет
                ret = ExecRead(conn_db, out reader,
                " Select * From " + rashod.counters_xx +
                " Where " + rashod.where_dom + rashod.where_kvar
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                if (!reader.Read())
                {
                    ExecByStep(conn_db, rashod.charge_xx, "nzp_charge",
                       " Insert into " + rashod.counters_xx +
                       " ( stek,nzp_type,dat_charge,nzp_kvar,nzp_dom,nzp_counter,nzp_serv, rashod, dat_s,dat_po ) " +
                       " Select 3,3, '' ,k.nzp_kvar,k.nzp_dom, -1, b.nzp_serv, max( case when b.tarif > 0 then b.rsum_tarif/b.tarif else 0 end) " +
                            "," + rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po +
                       " From " + rashod.paramcalc.pref + "_data:kvar k, " + rashod.charge_xx + " b " +
                       " Where k.nzp_kvar = b.nzp_kvar " +
                       "   and k." + rashod.where_dom + rashod.where_kvarK +
                       "   and b.nzp_serv in ( Select nzp_serv From " + rashod.paramcalc.pref + "_kernel:s_counts ) " +
                       "   and nvl( b.dat_charge, MDY(1,1,3000) ) = ((" +
                               " Select max( nvl( c.dat_charge, MDY(1,1,3000) ) ) From " + rashod.charge_xx + " c " +
                               " Where b.nzp_kvar = c.nzp_kvar  " +
                               "   and b.nzp_serv = c.nzp_serv ))"
                      , 100000, " Group by 1,2,3,4,5,6,7 ", out ret);

                    if (!ret.result)
                    {
                        reader.Close();
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                }
                reader.Close();


                //выбрать мно-во лицевых счетов из must_calc!
                ExecSQL(conn_db, " Drop table ttt_mustcalc ", false);

                ret = ExecSQL(conn_db,
                    " Select unique k.nzp_kvar,m.nzp_serv From " + rashod.paramcalc.pref + "_data:must_calc m, t_selkvar k " +
                    " Where k.nzp_kvar = m.nzp_kvar " +
                    "   and m.year_ = " + rashod.paramcalc.cur_yy +
                    "   and m.month_ = " + rashod.paramcalc.cur_mm +
                    "   and dat_s  <= MDY(" + rashod.paramcalc.calc_mm + "," + DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm) + "," + rashod.paramcalc.calc_yy + ")" +
                    "   and dat_po >= MDY(" + rashod.paramcalc.calc_mm + ",1," + rashod.paramcalc.calc_yy + ")" +
                    " Into temp ttt_mustcalc with no log "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                ExecSQL(conn_db, " Create unique index ix_ttt_mustcalc on ttt_mustcalc (nzp_kvar,nzp_serv) ", true);
                ExecSQL(conn_db, " Update statistics for table ttt_mustcalc ", true);

            }
            */
            #endregion помеченное к удалению

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
                LoadTempTablesRashod(conn_db, rashod.paramcalc, out ret);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }




            if (paramcalc.isPortal)
            {
                //учтем текущий charge_cnts во временной таблице (накроем показания)
                Portal_UchetCounters(conn_db, paramcalc, out ret);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false ;
                }
            }

            #endregion Подготовка к подсчету - заполнение временных таблиц

            #region Создание временной таблицы tpok
            ExecSQL(conn_db, " Drop table tpok ", false);
            
            ret = ExecSQL(conn_db,
                " Create temp table tpok " +
                //" Create table are.tpok " +
                " ( nzp_cr      serial, " +
                "   nzp_kvar    integer," +
                "   nzp_dom     integer," +
                "   nzp_counter integer," +
                "   nzp_serv    integer," +
                "   dat_uchet   date    " +
                //" ) "
#if PG
                " ) "
#else
                " ) With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            #endregion Создание временной таблицы tpok

            #region настройка алгоритма по параметрам базы (next_month7,1994,9000,1989)
            /*
            ret = ExecRead(conn_db, out reader,
            " Select count(*) cnt From " + rashod.paramcalc.pref + "_data:prm_5 p "+
            " Where p.nzp_prm = 2000 " + 
            "   and p.is_actual <> 100 "+
            "   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s + " "
            , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            */
            bool b_next_month = false;
            bool b_next_month7 = false;

            // Points.IsSmr -> nzp_prm=2000 & Points.Is50 -> nzp_prm=2000

            if (Points.IsSmr) //(reader.Read())
            {
                b_next_month = Points.IsSmr; // (Convert.ToInt32(reader["cnt"]) > 0);
                b_next_month7 = b_next_month;
            }
            //reader.Close();
            /*
            if (Points.Is50)
            {
                b_next_month7 = false;
            }
            */
            /* отмена учета КПУ на месяц назад для Самары - Демо-версия */
            /*
            ret = ExecRead(conn_db, out reader,
            " Select count(*) cnt From " + rashod.paramcalc.pref + "_data:prm_5 p "+
            " Where p.nzp_prm = 1999 " +
              " and p.is_actual <> 100 and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s + " "
            , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            */
            if (Points.IsDemo) //(reader.Read())
            {
                b_next_month = b_next_month && !Points.IsDemo; // !(Convert.ToInt32(reader["cnt"]) > 0);
            }
            //reader.Close();

            /* включить алгоритм учета дельт расходов при расчете ОДН - для ЕИРЦ Самара val_prm='2'! - временно пока  здесь ! - перенести в sprav.ifmx.cs */
            bool b_is_delta = false;
            ret = ExecRead(conn_db, out reader,
            " Select count(*) cnt From " + rashod.paramcalc.data_alias + "prm_5 p " +
            " Where p.nzp_prm = 1994 and p.val_prm='2' " +
              " and p.is_actual <> 100 and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s + " "
            , true);
            if (!ret.result)
            {
                reader.Close();
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            if (reader.Read())
            {
                b_is_delta = (Convert.ToInt32(reader["cnt"]) > 0);
            }
            reader.Close();

            string sDatUchet = "a.dat_uchet";
            if (b_next_month)
            {
                //sDatUchet = "ADD_MONTHS(a.dat_uchet,1) dat_uchet";
            }
            // ... Saha
            bool bIsSaha = false;
            object count = ExecScalar(conn_db,
                " Select count(*) From " + rashod.paramcalc.data_alias + "prm_5" +
                " Where nzp_prm=9000 and is_actual<>100 ",
                out ret, true);
            if (ret.result)
            {
                try
                {
                    bIsSaha = (Convert.ToInt32(count) > 0);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }

            bool bIsCalcSubsidyBill = false;
            count = ExecScalar(conn_db,
                " select count(*) cnt " +
                " from " + rashod.paramcalc.data_alias + "prm_5 p " +
                " where p.nzp_prm=1989 and p.is_actual<>100 ",
                out ret, true);
            if (ret.result)
            {
                try
                {
                    bIsCalcSubsidyBill = (Convert.ToInt32(count) > 0);
                }
                catch //(Exception ex)
                {
                    bIsCalcSubsidyBill = false;
                }
            }
            // ...


            //ниже откзался от выборки ttt_counters, поскольку показания уже выбираются в temp_counters
            /*
            string smust_calc = "";
            string sfind_tmp =
                " Select a.nzp_kvar,a.nzp_counter,a.nzp_serv,a.dat_uchet" +
                " From " + rashod.paramcalc.pref + "_data:counters a" +
                " Where a.is_actual  <> 100 "+
                " Into temp ttt_counters with no log ";

            // если расчет по дому или л/с, то соединить с t_selkvar для min времени выборки
            if ((rashod.paramcalc.nzp_dom > 0) || (rashod.paramcalc.nzp_kvar > 0))
            {
                sfind_tmp =
                " Select a.nzp_kvar,a.nzp_counter,a.nzp_serv,a.dat_uchet" +
                " From " + rashod.paramcalc.pref + "_data:counters a, t_selkvar k" +
                " Where a.is_actual <> 100 and a.nzp_kvar = k.nzp_kvar "+
                " Into temp ttt_counters with no log ";
            }

            ret = ExecSQL(conn_db, sfind_tmp, true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            ExecSQL(conn_db, " Create index ix_ttt_counters1 on ttt_counters (nzp_counter,dat_uchet) ", true);
            ExecSQL(conn_db, " Create index ix_ttt_counters2 on ttt_counters (nzp_kvar) ", true);
            ExecSQL(conn_db, " Update statistics for table ttt_counters ", true);
            */

            #endregion настройка алгоритма по параметрам базы

            #region Выборка уникальных счетчиков  с учетом выбранного списка temp_counters&t_selkvar
            ret = ExecSQL(conn_db,
                " Insert into tpok (nzp_cr,nzp_kvar,nzp_dom,nzp_counter,nzp_serv,dat_uchet)" +
                " Select 0,k.nzp_kvar,k.nzp_dom,a.nzp_counter,a.nzp_serv," + sDatUchet +
                " From temp_counters a, t_selkvar k" +
                " Where a.nzp_kvar    = k.nzp_kvar" +

                "   and 1 > (" +
                "     Select count(*) From aid_i" + rashod.paramcalc.pref + " n" +
                "     Where a.nzp_counter = n.nzp_counter and a.dat_uchet >= n.dat_s and a.dat_uchet <= n.dat_po" +
                "     )"
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref); 
                return false;
            }
            ExecSQL(conn_db, " Create index ix_tpok on tpok (nzp_counter,dat_uchet) ", true);
            ExecSQL(conn_db, sUpdStat + " tpok ", true);

            #endregion Выборка уникальных счетчиков  с учетом выбранного списка temp_counters&t_selkvar

            #region Выборка квартирных ПУ (уменьшение основной выборки )
            //----------------------------------------------------------------
            //заполнение квартирных ПУ (только по открытым лс)
            //----------------------------------------------------------------
            //таблица показаний, где даты показаний
            //ta_mr <= dat_s
            //ta_br >= dat_s
            //ta_b  >  dat_s and < dat_po

            //tb_b  >  dat_po
            //tb_br >= dat_po
            //tb_mr <= dat_po


            ExecSQL(conn_db, " Drop table ta_mr ", false);
            ret = ExecSQL(conn_db,
                " Select nzp_counter,max(dat_uchet) dat_uchet "+
#if PG
                " into temp ta_mr "+
#else
#endif
                " From tpok Where dat_uchet <=" + rashod.paramcalc.dat_s + 
                " Group by 1 "
#if PG
#else
                +" into temp ta_mr with no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            ExecSQL(conn_db, " Create unique index ix_ta_mr on ta_mr (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " ta_mr ", true);


            //специально взято макс справа, чтобы наиболее точно апроксимировать подневной расход ???!!!
            //надо подумать, нафига так делать, ваще не надо!
            ExecSQL(conn_db, " Drop table ta_bi ", false);
            ret = ExecSQL(conn_db,
                /*
                " Select nzp_counter,max(dat_uchet) dat_uchet From tpok "+ 
                " Where dat_uchet >=" + rashod.paramcalc.dat_s + 
                " Group by 1 into temp ta_br with no log "
                */
                " Select nzp_counter,min(dat_uchet) dat_uchet "+
#if PG
                " into temp ta_bi  " +
#else
#endif
                "From tpok " +
                " Where dat_uchet > " + rashod.paramcalc.dat_s +
                "   and dat_uchet <=" + rashod.paramcalc.dat_po +
                " Group by 1 "
#if PG
#else
                +" into temp ta_bi  with no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            ExecSQL(conn_db, " Create unique index ix_ta_bi on ta_bi (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " ta_bi ", true);


            ExecSQL(conn_db, " Drop table tb_b ", false);
            ret = ExecSQL(conn_db,
                " Select nzp_counter,min(dat_uchet) dat_uchet "+
#if PG
                " into temp tb_b " +
#else
#endif
                " From tpok "+
                " Where dat_uchet > " + rashod.paramcalc.dat_po + 
                " Group by 1 "
#if PG
#else
                +" into temp tb_b  with no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            ExecSQL(conn_db, " Create unique index ix_tb_b on tb_b (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " tb_b ", true);


            ExecSQL(conn_db, " Drop table tb_br ", false);
            ret = ExecSQL(conn_db,
                " Select nzp_counter,min(dat_uchet) dat_uchet "+
#if PG
                " into temp tb_br  "+
#else
#endif
                " From tpok "+
                " Where dat_uchet >=" + rashod.paramcalc.dat_po + 
                " Group by 1 "
#if PG
#else
                +" into temp tb_br with no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            ExecSQL(conn_db, " Create unique index ix_tb_br on tb_br (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " tb_br ", true);


            ExecSQL(conn_db, " Drop table tb_mr ", false);
            ret = ExecSQL(conn_db,
                " Select nzp_counter,min(dat_uchet) dat_uchet "+
#if PG
                " into temp tb_mr " +
#else
#endif
                " From tpok " +
                " Where dat_uchet <=" + rashod.paramcalc.dat_po +
                " Group by 1 "
#if PG
#else
                +" into temp tb_mr with no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            ExecSQL(conn_db, " Create unique index ix_tb_mr on tb_mr (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " tb_mr ", true);


            //показание в середине
            ExecSQL(conn_db, " Drop table ta_b ", false);
            ret = ExecSQL(conn_db,
                " Select nzp_counter,min(dat_uchet) dat_uchet "+
#if PG
                " into temp ta_b " +
#else
#endif
                " From tpok " +
                " Where dat_uchet > " + rashod.paramcalc.dat_s +
                "   and dat_uchet < " + rashod.paramcalc.dat_po +
                " Group by 1 "
#if PG
#else
                + " into temp ta_b  with no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            ExecSQL(conn_db, " Create unique index ix_ta_b on ta_b (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " ta_b ", true);


            #endregion Выборка квартирных ПУ (уменьшение основной выборки )

            #region Выборка показаний захватывающих расчетный месяц t_inscnt
            ExecSQL(conn_db, " Drop table t_inscnt ", false);

            ret = ExecSQL(conn_db,
                " Create temp table t_inscnt" +
                //" Create table are.t_inscnt" +
                " ( nzp_cr      serial, " +
                "   nzp_kvar    integer," +
                "   nzp_dom     integer," +
                "   nzp_counter integer," +
                "   nzp_serv    integer," +
                "   dat_s       date,   " +
                "   dat_po      date    " +
                //" ) "
#if PG
                " )  "
#else
                " ) With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //выберем показания, которые полностью покрывают месяц!
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //           |      |
            //     ^-------------------^
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ret = ExecSQL(conn_db,
                " Insert into t_inscnt (nzp_cr,nzp_kvar,nzp_dom,nzp_counter,nzp_serv,dat_s,dat_po)" +
                " Select 0,a.nzp_kvar,a.nzp_dom,a.nzp_counter,a.nzp_serv,max(a.dat_uchet),min(b.dat_uchet)" +
                " From tpok a, tpok b" +
                " Where a.nzp_counter = b.nzp_counter" +
                " and a.dat_uchet   < b.dat_uchet" +
                " and a.dat_uchet = ( Select c.dat_uchet From ta_mr c Where c.nzp_counter = a.nzp_counter )" +
                " and b.dat_uchet = ( Select p.dat_uchet From tb_b  p Where p.nzp_counter = b.nzp_counter )" +
                " Group by 1,2,3,4,5 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);
            ret = ExecSQL(conn_db,
#if PG
                " Select distinct nzp_counter Into temp ttt_aid_c1 From t_inscnt "
#else
                " Select unique nzp_counter From t_inscnt Into temp ttt_aid_c1 With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            ExecSQL(conn_db, " Create unique index ix_aid22_ttt1 on ttt_aid_c1 (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);

            

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //выбор других интервалов, кроме уже выбранных в counters_xx
            //придеться изголяться, чтобы выбрать ближайшие показания (избежать выбора большого интервала)
            //зато понятно
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //           |      |
            //   ^-----------^
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ret = ExecSQL(conn_db,
                " Insert into t_inscnt (nzp_cr,nzp_kvar,nzp_dom,nzp_counter,nzp_serv,dat_s,dat_po)" +
                " Select 0,a.nzp_kvar,a.nzp_dom,a.nzp_counter,a.nzp_serv,max(a.dat_uchet),min(b.dat_uchet)" +
                " From tpok a, tpok b" +
                " Where a.nzp_counter = b.nzp_counter" +
                "   and a.dat_uchet   < b.dat_uchet" +
                "   and 0 = ( Select count(*) From ttt_aid_c1 n Where a.nzp_counter = n.nzp_counter ) " +
                "   and a.dat_uchet = ( Select c.dat_uchet From ta_mr c Where c.nzp_counter = a.nzp_counter )" +
                "   and b.dat_uchet = ( Select p.dat_uchet From tb_mr p Where p.nzp_counter = b.nzp_counter)" +
                " Group by 1,2,3,4,5 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);
            ret = ExecSQL(conn_db,
#if PG
                " Select distinct nzp_counter Into temp ttt_aid_c1 From t_inscnt "
#else
                " Select unique nzp_counter From t_inscnt Into temp ttt_aid_c1 With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            ExecSQL(conn_db, " Create unique index ix_aid22_ttt1 on ttt_aid_c1 (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);


            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //           |      |
            //              ^-------------^
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ret = ExecSQL(conn_db,
                " Insert into t_inscnt (nzp_cr,nzp_kvar,nzp_dom,nzp_counter,nzp_serv,dat_s,dat_po)" +
                " Select 0,a.nzp_kvar,a.nzp_dom,a.nzp_counter,a.nzp_serv,max(a.dat_uchet),min(b.dat_uchet)" +
                " From tpok a, tpok b" +
                " Where a.nzp_counter = b.nzp_counter" +
                "   and a.dat_uchet   < b.dat_uchet" +
                "   and 0 = ( Select count(*) From ttt_aid_c1 n Where a.nzp_counter = n.nzp_counter ) " +
              //"   and a.dat_uchet = ( Select c.dat_uchet From ta_br c Where c.nzp_counter = a.nzp_counter )" +
                "   and a.dat_uchet = ( Select c.dat_uchet From ta_bi c Where c.nzp_counter = a.nzp_counter )" +
                "   and b.dat_uchet = ( Select p.dat_uchet From tb_b  p Where p.nzp_counter = b.nzp_counter )" +
                //"   and a.dat_uchet = ( Select c.dat_uchet From ta_b c Where c.nzp_counter = a.nzp_counter )" +
                //"   and b.dat_uchet = ( Select p.dat_uchet From tb_br p Where p.nzp_counter = b.nzp_counter )" +
                " Group by 1,2,3,4,5 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);
            ret = ExecSQL(conn_db,
#if PG
                " Select distinct nzp_counter Into temp ttt_aid_c1 From t_inscnt "
#else
                " Select unique nzp_counter From t_inscnt Into temp ttt_aid_c1 With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            ExecSQL(conn_db, " Create unique index ix_aid22_ttt1 on ttt_aid_c1 (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);



            //Надо УБРАТЬ учет таких периодов, ибо фигня получается (ситуация не обработана!!!)
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //           |      |
            //             ^--^
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ret = ExecSQL(conn_db,
                " insert into t_inscnt (nzp_cr,nzp_kvar,nzp_dom,nzp_counter,nzp_serv,dat_s,dat_po)" +
                " Select 0,a.nzp_kvar,a.nzp_dom,a.nzp_counter,a.nzp_serv,max(a.dat_uchet),min(b.dat_uchet)" +
                " From tpok a, tpok b" +
                " Where a.nzp_counter = b.nzp_counter" +
                "   and a.dat_uchet   < b.dat_uchet" +
                "   and 0 = ( Select count(*) From ttt_aid_c1 n Where a.nzp_counter = n.nzp_counter ) " +
               //"   and a.dat_uchet = ( Select c.dat_uchet From ta_br c Where c.nzp_counter = a.nzp_counter )" +
                "   and a.dat_uchet = ( Select c.dat_uchet From ta_b c Where c.nzp_counter = a.nzp_counter )" +
                "   and b.dat_uchet = ( Select p.dat_uchet From tb_mr p Where p.nzp_counter = b.nzp_counter )" +
                " Group by 1,2,3,4,5 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);
            ret = ExecSQL(conn_db,
#if PG
                " Select distinct nzp_counter Into temp ttt_aid_c1 From t_inscnt "
#else
                " Select unique nzp_counter From t_inscnt Into temp ttt_aid_c1 With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            ExecSQL(conn_db, " Create unique index ix_aid22_ttt1 on ttt_aid_c1 (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);

            #endregion Выборка показаний захватывающих расчетный месяц t_inscnt

            #region Заполнение постоянных таблиц с расходами rashod.counters_xx на основе t_inscnt
            string sql = 
                    " Insert into " + rashod.counters_xx +
                    " ( stek,nzp_type,dat_charge,nzp_kvar,nzp_dom,nzp_counter,nzp_serv,dat_s,dat_po ) " +
                    " Select 1,3, " + p_dat_charge + ",nzp_kvar,nzp_dom,nzp_counter,nzp_serv,dat_s,dat_po " +
                    " From t_inscnt Where 1=1 ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, "t_inscnt", "nzp_cr", sql, 100000, " ", out ret);
            }

            #endregion Заполнение постоянных таблиц с расходами rashod.counters_xx на основе t_inscnt

            #region Удалить временные таблицы tpok t_inscnt ttt_aid_c1
            ExecSQL(conn_db, " Drop table tpok ", false);
            ExecSQL(conn_db, " Drop table t_inscnt ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);


            UpdateStatistics(true, paramcalc, rashod.counters_tab, out ret);

            #endregion Удалить временные таблицы tpok t_inscnt ttt_aid_c1

            #region Начинаем заполнять квартирные ПУ
            //----------------------------------------------------------------
            //заполнение квартирных ПУ
            //----------------------------------------------------------------
            sDatUchet = "a.dat_uchet";
            if (b_next_month)
            {
                //sDatUchet = "ADD_MONTHS(a.dat_uchet,1)";
            }

            string tab = "temp_counters"; // rashod.paramcalc.pref + "_data:counters";

            
            //выбрать все показания по лс
            //надо добавить индекс counters (nzp_counter,dat_uchet)


            ExecSQL(conn_db, " Drop table tpok_s ", false);
            ExecSQL(conn_db, " Drop table tpok_po ", false);

            ret = ExecSQL(conn_db,
                //" Select unique a.nzp_counter,a.dat_uchet, a.val_cnt " +
                " Select a.nzp_counter," + sDatUchet + " as dat_uchet, max(a.val_cnt) val_cnt " +
#if PG
                " Into temp tpok_s "+
#else
#endif
                " From t_selkvar k, " + tab + " a, " + rashod.counters_xx + " b " +
                " Where k.nzp_kvar = a.nzp_kvar " +
                //"   and a.is_actual <> 100 " +
                "   and k.nzp_kvar = b.nzp_kvar " +
                "   and a.nzp_counter = b.nzp_counter " +
                "   and b.nzp_type = 3 and b.stek = 1 " + rashod.paramcalc.per_dat_charge +
                "   and b.dat_s = " + sDatUchet +
                " group by 1,2 "
#if PG
#else
                +" Into temp tpok_s With no log "
#endif
                , true, 300);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ret = ExecSQL(conn_db,
                //" Select unique a.nzp_counter," + sDatUchet + " as dat_uchet, a.val_cnt " +
                " Select a.nzp_counter," + sDatUchet + " as dat_uchet, max(a.val_cnt) val_cnt " +
#if PG
                " Into temp tpok_po "+
#else
#endif
                " From t_selkvar k, " + tab + " a, " + rashod.counters_xx + " b " +
                " Where k.nzp_kvar = a.nzp_kvar " +
                //"   and a.is_actual <> 100 " +
                "   and k.nzp_kvar = b.nzp_kvar " +
                "   and a.nzp_counter = b.nzp_counter " +
                "   and b.nzp_type = 3 and b.stek = 1 " + rashod.paramcalc.per_dat_charge +
                "   and b.dat_po = " + sDatUchet +
                " group by 1,2 "
#if PG
#else
                +" Into temp tpok_po With no log "
#endif
                , true, 300);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            ExecSQL(conn_db, " Create unique index ix_tpok_s  on tpok_s  (nzp_counter,dat_uchet) ", true);
            ExecSQL(conn_db, " Create unique index ix_tpok_po on tpok_po (nzp_counter,dat_uchet) ", true);
            ExecSQL(conn_db, sUpdStat + " tpok_s ", true);
            ExecSQL(conn_db, sUpdStat + " tpok_po ", true);


            sql =
                    " Update " + rashod.counters_xx +
                    " Set val_s = ( Select max(val_cnt) From tpok_s a Where " + rashod.counters_xx + ".nzp_counter = a.nzp_counter " +
                                                                    "   and " + rashod.counters_xx + ".dat_s = a.dat_uchet ) " +
                    " Where nzp_type = 3 and stek = 1 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge;

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 50000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            sql =
                    " Update " + rashod.counters_xx +
                    " Set val_po = ( Select max(val_cnt) From tpok_po a Where " + rashod.counters_xx + ".nzp_counter = a.nzp_counter " +
                                                                      "   and " + rashod.counters_xx + ".dat_po = a.dat_uchet ) " +
                    " Where nzp_type = 3 and stek = 1 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge;

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 50000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Drop table tpok_s ", false);
            ExecSQL(conn_db, " Drop table tpok_po ", false);
            //ExecSQL(conn_db, " Drop table t_selkvar ", false); -- anes

            /*
            //очень тяжелый update, пришлось оптимизировать!
            ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx",
                " Update " + rashod.counters_xx +
                " Set val_s = ( Select max(val_cnt) From " + tab + " a " +
                " Where " + rashod.counters_xx + ".nzp_counter = a.nzp_counter " +
                "   and a.is_actual <> 100 " +
                "   and " + rashod.counters_xx + ".dat_s = " + sDatUchet + " ) " +
                " Where nzp_type = 3 and stek = 1 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge
              , 50000, " ", out ret);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx",
                " Update " + rashod.counters_xx +
                " Set val_po =( Select max(val_cnt) From " + tab + " a " +
                " Where " + rashod.counters_xx + ".nzp_counter = a.nzp_counter " +
                "   and a.is_actual <> 100 " +
                "   and " + rashod.counters_xx + ".dat_po = " + sDatUchet + " ) " +
                " Where nzp_type = 3 and stek = 1 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge
              , 50000, " ", out ret);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            */
            #endregion Начинаем заполнять квартирные ПУ

            #region заполнение домовых ПУ
            //----------------------------------------------------------------
            //заполнение домовых ПУ
            //----------------------------------------------------------------
            if (!rashod.paramcalc.b_again && !b_calc_kvar)
            {
                Rashod2 rashod2 = new Rashod2(rashod.counters_xx, rashod.paramcalc);
                tab = "temp_counters_dom"; //rashod.paramcalc.pref + "_data:counters_dom";
                rashod2.tab = tab;
                rashod2.dat_s = rashod.paramcalc.dat_s;
                rashod2.dat_po = rashod.paramcalc.dat_po;
                rashod2.p_TAB = rashod2.tab + " a";
                rashod2.p_KEY = "a.nzp_crd";
                rashod2.p_ACTUAL = " and c.is_actual <> 100";

                /*
                if (!rashod.paramcalc.b_cur)
                    smust_calc = " and 0 < ( Select count(*) From ttt_mustcalc m, t_selkvar k " +
                                           " Where k.nzp_kvar = m.nzp_kvar " +
                                           "   and a.nzp_dom = k.nzp_dom " +
                                           "   and b.nzp_dom = k.nzp_dom " +
                                           "   and (m.nzp_serv = 1 or a.nzp_serv = m.nzp_serv and b.nzp_serv = m.nzp_serv ) )";
                */

                rashod2.p_INSERT =
                " Insert into " + rashod.counters_xx +
                " ( stek,nzp_type,dat_charge,nzp_kvar,nzp_dom,nzp_counter,nzp_serv, dat_s,dat_po ) " +
                " Select 1,1, " + p_dat_charge + " ,0,a.nzp_dom, a.nzp_counter,a.nzp_serv, max(a.dat_uchet), min(b.dat_uchet) " +
                " From " + tab + " a, " + tab + " b " +
                " Where a.nzp_counter = b.nzp_counter " +
                "   and a.dat_uchet   < b.dat_uchet " +
                "   and a." + rashod.where_dom +

                    //и чтобы даты показаний не попадали в некондиционные интервалы
               "   and 1 > ( Select count(*) From aid_i" + rashod.paramcalc.pref + " n Where a.nzp_counter = n.nzp_counter and a.dat_uchet >= n.dat_s and a.dat_uchet <= n.dat_po ) " +
               "   and 1 > ( Select count(*) From aid_i" + rashod.paramcalc.pref + " n Where a.nzp_counter = n.nzp_counter and b.dat_uchet >= n.dat_s and b.dat_uchet <= n.dat_po ) ";

                LoadVals(conn_db, rashod2, out ret);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }


                UpdateStatistics(true, paramcalc, rashod.counters_tab, out ret);

                sql =
                    " Update " + rashod.counters_xx +
                    " Set (val_s,val2) = (" +
#if PG
                      "( Select max(val_cnt) From " + tab + " a " +
                      " Where " + rashod.counters_xx + ".nzp_counter = a.nzp_counter " +
                      "   and " + rashod.counters_xx + ".dat_s = a.dat_uchet )," +
                      "( Select max(ngp_cnt+ngp_lift) From " + tab + " a " +
                      " Where " + rashod.counters_xx + ".nzp_counter = a.nzp_counter " +
                      "   and " + rashod.counters_xx + ".dat_s = a.dat_uchet )" +
#else
                      "( Select max(val_cnt),max(ngp_cnt+ngp_lift) From " + tab + " a " +
                      " Where " + rashod.counters_xx + ".nzp_counter = a.nzp_counter " +
                      "   and " + rashod.counters_xx + ".dat_s = a.dat_uchet )" +
#endif
                    ") " +
                    " Where nzp_type = 1 and stek = 1 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge;

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                sql =
                    " Update " + rashod.counters_xx +
                    " Set (val_po,val3)=(" +
#if PG
                      "( Select max(val_cnt) From " + tab + " a " +
                      " Where " + rashod.counters_xx + ".nzp_counter = a.nzp_counter " +
                      "   and " + rashod.counters_xx + ".dat_po = a.dat_uchet )," +
                      "( Select max(ngp_cnt+ngp_lift) From " + tab + " a " +
                      " Where " + rashod.counters_xx + ".nzp_counter = a.nzp_counter " +
                      "   and " + rashod.counters_xx + ".dat_po = a.dat_uchet )" +
#else
                      "( Select max(val_cnt),max(ngp_cnt+ngp_lift) From " + tab + " a " +
                      " Where " + rashod.counters_xx + ".nzp_counter = a.nzp_counter " +
                      "   and " + rashod.counters_xx + ".dat_po = a.dat_uchet )" +
#endif
                    ") " +
                    " Where nzp_type = 1 and stek = 1 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge;

                ret = ExecSQL(conn_db, sql, true);

                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                //----------------------------------------------------------------
                //заполнение средних значений ДПУ
                //----------------------------------------------------------------
                ret = ExecSQL(conn_db,
                    " Insert into " + rashod.counters_xx +
                    " ( stek,nzp_type,dat_charge,nzp_kvar,nzp_dom,nzp_counter,nzp_serv, dat_s,dat_po, val_s,val_po, val1, val4 ) " +
                    " Select 4,1, " + p_dat_charge + " ,0,p.nzp, 0,s.nzp_serv, " +
                    rashod.paramcalc.dat_s + ", " + rashod.paramcalc.dat_po + "," +
                    " 0,max(replace( " + sNvlWord + "(p.val_prm,'0'), ',', '.')" + sConvToNum + ") ," +
                    "max(replace( " + sNvlWord + "(p.val_prm,'0'), ',', '.')" + sConvToNum + "), 0 " +
                    " From ttt_prm_2 p, " + rashod.paramcalc.kernel_alias + "s_counts s, t_selkvar k " +
                    " Where p.nzp_prm = s.nzp_prm_sred_dom and s.nzp_prm_sred_dom>0 " +
                    "   and p.nzp = k.nzp_dom " +
                    " group by 1,2,3,4,5,6,7,8,9 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                //----------------------------------------------------------------
                //заполнение групповых ПУ
                //----------------------------------------------------------------
                tab = rashod.paramcalc.data_alias + "counters_group";
                rashod2.tab = tab;
                rashod2.dat_s = rashod.paramcalc.dat_s;
                rashod2.dat_po = rashod.paramcalc.dat_po;
                rashod2.p_TAB = rashod2.tab + " a";
                rashod2.p_KEY = "a.nzp_cg";
                rashod2.p_ACTUAL = " and c.is_actual <> 100";

                rashod2.p_INSERT =
                    " Insert into " + rashod.counters_xx +
                    " ( stek,nzp_type,dat_charge,nzp_kvar,nzp_dom,nzp_counter,nzp_serv, dat_s,dat_po ) " +
                    " Select 1,2, " + p_dat_charge + " ,0,d.nzp_dom, a.nzp_counter,d.nzp_serv, max(a.dat_uchet), min(b.dat_uchet) " +
                    " From " + tab + " a, " + tab + " b, " + rashod.paramcalc.data_alias + "counters_domspis d " +
                    " Where a.nzp_counter = b.nzp_counter " +
                    "   and a.nzp_counter = d.nzp_counter " +
                    "   and a.dat_uchet   < b.dat_uchet " +
                    "   and d." + rashod.where_dom +

                    //и чтобы даты показаний не попадали в некондиционные интервалы
                    "   and 1 > ( Select count(*) From aid_i" + rashod.paramcalc.pref + " n Where a.nzp_counter = n.nzp_counter and a.dat_uchet >= n.dat_s and a.dat_uchet <= n.dat_po ) " +
                    "   and 1 > ( Select count(*) From aid_i" + rashod.paramcalc.pref + " n Where a.nzp_counter = n.nzp_counter and b.dat_uchet >= n.dat_s and b.dat_uchet <= n.dat_po ) ";

                LoadVals(conn_db, rashod2, out ret);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                UpdateStatistics(true, paramcalc, rashod.counters_tab, out ret);

                sql =
                        " Update " + rashod.counters_xx +
                        " Set val_s = ( Select max(val_cnt) From " + tab + " a " +
                        " Where " + rashod.counters_xx + ".nzp_counter = a.nzp_counter " +
                        "   and " + rashod.counters_xx + ".dat_s = a.dat_uchet ) " +
                        " Where nzp_type = 2 and stek = 1 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge;

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                sql =
                        " Update " + rashod.counters_xx +
                        " Set val_po =( Select max(val_cnt) From " + tab + " a " +
                        " Where " + rashod.counters_xx + ".nzp_counter = a.nzp_counter " +
                        "   and " + rashod.counters_xx + ".dat_po = a.dat_uchet ) " +
                        " Where nzp_type = 2 and stek = 1 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge;

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }


                //rashod.pbool = true;
                //UpdateStatisticsCounters_xx(rashod, out ret);
                UpdateStatistics(true, paramcalc, rashod.counters_tab, out ret);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }

            #endregion заполнение домовых ПУ

            #region заполнение расходов ПУ (можно включить как в 2.0)

            //----------------------------------------------------------------
            //заполнить параметры типа
            //----------------------------------------------------------------
            sql =
                    " Update " + rashod.counters_xx +
                    
#if PG
                    " Set cnt_stage=( Select cnt_stage " +
                    " From " + rashod.paramcalc.data_alias + "counters_spis a, " + rashod.paramcalc.kernel_alias + "s_counttypes b " +
                    " Where a.nzp_cnttype = b.nzp_cnttype " +
                    "   and " + rashod.counters_xx + ".nzp_counter = a.nzp_counter ),"+
                    " mmnog = ( Select mmnog " +
                    " From " + rashod.paramcalc.data_alias + "counters_spis a, " + rashod.paramcalc.kernel_alias + "s_counttypes b " +
                    " Where a.nzp_cnttype = b.nzp_cnttype " +
                    "   and " + rashod.counters_xx + ".nzp_counter = a.nzp_counter ) " +
#else
                    " Set (cnt_stage,mmnog) = (( Select cnt_stage,mmnog " +
                    " From " + rashod.paramcalc.data_alias + "counters_spis a, " + rashod.paramcalc.kernel_alias + "s_counttypes b " +
                    " Where a.nzp_cnttype = b.nzp_cnttype " +
                    "   and " + rashod.counters_xx + ".nzp_counter = a.nzp_counter )) " +
#endif

                    " Where 1 = 1 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge;

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            //заполнение для неопределенных значений ДПУ
            ret = ExecSQL(conn_db,
                " Update " + rashod.counters_xx +
                " Set cnt_stage=10 ,mmnog=1 " +
                " Where (cnt_stage is null or mmnog is null) and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            
            //заполнение для средних значений ДПУ
            ret = ExecSQL(conn_db,
                " Update " + rashod.counters_xx +
                " Set cnt_stage=10 ,mmnog=1 " +
                " Where stek = 4 and nzp_type = 1 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            //заполнение для без ДПУ 
            ret = ExecSQL(conn_db,
                " Update " + rashod.counters_xx +
                " Set cnt_stage=10 ,mmnog=1 " +
                " Where cnt_stage is null and nzp_type = 1 and stek=333 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            //----------------------------------------------------------------
            //посчитать расход общий по счетчику
            //----------------------------------------------------------------
            //надо подумать, как застраховаться от гигантских расходов (иначе свалится update) - ограничу 1000000
            sql =
                    " Update " + rashod.counters_xx +
                    " Set val4 = case when (" +                                                                                                 //val2 не учитывается!
                       " (case when val_po - val_s>-0.0001 then (val_po - val_s)*mmnog else (pow(10,cnt_stage)+val_po-val_s)*mmnog end) - (val3+0))  > 1000000 then 1000000 else " +
                       " (case when val_po - val_s>-0.0001 then (val_po - val_s)*mmnog else (pow(10,cnt_stage)+val_po-val_s)*mmnog end) - (val3+0)  end " +
                    " Where 1 = 1 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge;

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }


            #region посчитать расход, который приходится в текущем месяце
            //----------------------------------------------------------------
            //посчитать расход, который приходится в текущем месяце
            //----------------------------------------------------------------
            //st1:=IntToStr(count_days(nedoXX.calc_yy,nedoXX.calc_mm)); //кол-во дней в месяце
            string st1 = (DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm)).ToString();

#if PG
            string intrv1 = "interval '1 day'";    
            string tExtDayBeg = "EXTRACT(day from ";
            string tExtDayEnd = ")";
#else
            string intrv1 = "1";
            string tExtDayBeg = "";
            string tExtDayEnd = "";
#endif

            bool bGoodCalcIPU = true;
            #region Выставить признак "Включить расчет расходов ИПУ как в 2.0 (неправильный вариант)"
            //
            //3000|Включить расчет расходов ИПУ как в 2.0 (неправильный вариант)|||bool||5||||
            IDbCommand cmd3000 = DBManager.newDbCommand(
                " select count(*) cnt " +
                " from " + paramcalc.data_alias + "prm_5 p " +
                " where p.nzp_prm=3000 and p.is_actual<>100 " +
                "   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s + " "
            , conn_db);
            try
            {
                string scntvals = Convert.ToString(cmd3000.ExecuteScalar());
                bGoodCalcIPU = (Convert.ToInt32(scntvals) == 0);
            }
            catch
            {
                bGoodCalcIPU = true;
            }
            #endregion Выставить признак "Включить расчет расходов ИПУ как в 2.0 (неправильный вариант)"

            sql =
                " Update " + rashod.counters_xx +
                " Set val1 = (case when dat_s >= " + rashod.paramcalc.dat_s +
                " and dat_po <= " + rashod.paramcalc.dat_po + " + " + intrv1;

            if (bGoodCalcIPU)
            {
                //правильный код!
                sql = sql.Trim() +
                " and dat_s < dat_po " +
                " then val4 " +
                " else (val4 / (dat_po - dat_s)) * " +

                  //если показание в середине месяца, то нельзя умножать на весь месяц, а только на период до конца месяца!!
                  //причем, показание в середине месяца появятся только тогда, когда это первое показание, иначе показания всегда покрывают месяц!
                  " (case when dat_s > " + rashod.paramcalc.dat_s + " and dat_s < " + rashod.paramcalc.dat_po +
                  "  then " + tExtDayBeg + "(" + rashod.paramcalc.dat_po + " + " + intrv1 + " - dat_s)" + tExtDayEnd +
                  // st1 = DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm)
                  "  else " + st1 + " end) ";

            }
            else
            {
                //потом удалить, неправильный код!
                //для сопоставления расчета Анэса!
                sql = sql.Trim() +
                " and dat_s - " + intrv1 + " < dat_po " +
                " then val4" +
                " else (val4 / (dat_po - dat_s - " + intrv1 + ")) * " +

                  " (case when dat_s > " + rashod.paramcalc.dat_s + " and dat_s < " + rashod.paramcalc.dat_po +
                  "  then " + tExtDayBeg + "(" + rashod.paramcalc.dat_po + " + " + intrv1 + " - dat_s)" + tExtDayEnd +
                  // st1 = DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm)
                  "  else " + st1 + " end) " + 
                  " + (case when dat_s >= " + rashod.paramcalc.dat_s + " and dat_s < " + rashod.paramcalc.dat_po +
                     " then val4 - (val4 / (dat_po - dat_s - " + intrv1 + ")) * (dat_po - dat_s) " +
                     " else 0 " +
                     " end)";
            }
            sql = sql.Trim() +
                " end )  " +
                " Where 1 = 1 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge;

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            #endregion посчитать расход, который приходится в текущем месяце

            //----------------------------------------------------------------
            //посчитать доп.виртуальные расходы на конец месяца
            //----------------------------------------------------------------
            /*
            if (rashod.calcv)
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx",
                    " Update " + rashod.counters_xx +
                    " Set rvirt = (case when dat_po > " + rashod.paramcalc.dat_po + " then 0 else (val1/" + DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm) + 
                                ")*(" + rashod.paramcalc.dat_po + " - dat_po) end )  " +
                    " Where nzp_type = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge
                  , 100000, " ", out ret);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx",
                    " Update " + rashod.counters_xx +
                    " Set val1 = val1 + rvirt " +
                    " Where nzp_type = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge
                  , 100000, " ", out ret);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }
            else
            {
                //иначе обнулить неполный расход, чтобы считался по нормативу!
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx",
                    " Update " + rashod.counters_xx +
                    " Set val1 = 0 " +
                    " Where nzp_type = 3 and " + rashod.where_dom + rashod.where_kvar +  rashod.paramcalc.per_dat_charge +
                    "   and dat_po <= " + rashod.paramcalc.dat_po
                  , 100000, " ", out ret);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }
            */


            //UpdateStatisticsCounters_xx(rashod, out ret);
            UpdateStatistics(false, paramcalc, rashod.counters_tab, out ret);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            #endregion заполнение расходов ПУ (можно включить как в 2.0)

            #region Заполнение площадей
            //----------------------------------------------------------------
            //вытащим площади и услуги лс
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_aid_sq ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_sq " +
                " ( no       serial, " +
                "   nzp_kvar integer," +
                "   nzp_dom  integer," +
                "   nzp_serv integer, " +
                "   kod      integer default 0, " +
                "   sq       " + sDecimalType + "(12,4) default 0," +
                "   sqgil    " + sDecimalType + "(12,4) default 0," +
                "   sqot     " + sDecimalType + "(12,4) default 0 " +
#if PG
                " )  "
#else
                " ) With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            // только открытые лс
            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_sq (nzp_kvar,nzp_dom, nzp_serv, sq) " +
                " Select k.nzp_kvar,k.nzp_dom, t.nzp_serv, " + sNvlWord + "(p.val_prm,'0') " + sConvToNum +
                " From temp_table_tarif t, t_opn k " +
#if PG
                " left outer join ttt_prm_1 p on k.nzp_kvar = p.nzp and p.nzp_prm = 4 " +
                " Where  " +
#else
                ",  outer ttt_prm_1 p "+
                " Where k.nzp_kvar = p.nzp and p.nzp_prm = 4 and  " +
#endif
                "   k.nzp_kvar = t.nzp_kvar " +
                "   and " + rashod.where_dom + rashod.where_kvarK +
                "   and t.nzp_serv in ( Select nzp_serv From " + rashod.paramcalc.kernel_alias + "s_counts ) " +
                " Group by 1,2,3,4 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Create unique index ix_aid00_sq on ttt_aid_sq (no) ", true);
            ExecSQL(conn_db, " Create unique index ix_aid11_sq on ttt_aid_sq (nzp_kvar,nzp_serv,kod) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_sq ", true);

            // признак ПУ
            sql =
                    " Update ttt_aid_sq " +
                    " Set kod = 1 " +
                    " Where 0 < ( Select max(1) From " + rashod.counters_xx + " k " +
                                " Where ttt_aid_sq.nzp_kvar = k.nzp_kvar " +
                                "   and ttt_aid_sq.nzp_serv = k.nzp_serv " +
                                "   and k." + rashod.where_dom + rashod.where_kvarK +
                                " ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, "ttt_aid_sq", "no", sql, 50000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            // отапливаемая площадь
            sql =
                    " Update ttt_aid_sq " +
                    " Set sqot = ( select p.val_prm from ttt_prm_1 p where p.nzp=ttt_aid_sq.nzp_kvar and p.nzp_prm=133 )" + sConvToNum +
                    " Where  0 < ( select count(*)  from ttt_prm_1 p where p.nzp=ttt_aid_sq.nzp_kvar and p.nzp_prm=133 ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, "ttt_aid_sq", "no", sql, 50000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            // жилая площадь - нужна для коммуналок - для разделения расхода по Пост.354
            sql =
                    " Update ttt_aid_sq " +
                    " Set sqgil = ( select p.val_prm from ttt_prm_1 p where p.nzp=ttt_aid_sq.nzp_kvar and p.nzp_prm=6 )" + sConvToNum +
                    " Where  0 < ( select count(*)  from ttt_prm_1 p where p.nzp=ttt_aid_sq.nzp_kvar and p.nzp_prm=6 ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, "ttt_aid_sq", "no", sql, 50000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, sUpdStat + " ttt_aid_sq ", true);

            #endregion Заполнение площадей

            #region стек 2 - отсутствие показаний в лс в расчетном месяце, но есть ПУ (норма или среднее)
            //----------------------------------------------------------------
            //стек 2 - отсутствие показаний в лс в расчетном месяце, но есть ПУ
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_ans_kpu ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_ans_kpu " +
                " ( nzp_kvar    integer, "+
                "   nzp_dom     integer, "+
                "   nzp_type    integer default 3," +
                "   nzp_counter integer, "+
                "   sq          " + sDecimalType + "(15,7) default 0.00, " +
                "   sqot        " + sDecimalType + "(15,7) default 0.00, " +
                "   nzp_serv    integer, " +
                "   val         " + sDecimalType + "(15,7) " +
#if PG
                " ) "
#else
                " ) With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            //stek,nzp_type,dat_charge,nzp_kvar,nzp_dom,nzp_counter,dat_s,dat_po, squ1, nzp_serv, val1

            // ИПУ
            ret = ExecSQL(conn_db,
                " Insert into ttt_ans_kpu (nzp_kvar, nzp_dom, nzp_counter, sq, sqot, nzp_serv, val) " +
                " Select k.nzp_kvar,k.nzp_dom, t.nzp_counter, k.sq, k.sqot, t.nzp_serv, "+
                " max(replace( " + sNvlWord + "(a.val_prm,'0'), ',', '.')" + sConvToNum + ") as val  " +
                " From " + rashod.paramcalc.data_alias + "prm_17 a, ttt_aid_sq k, " + rashod.paramcalc.data_alias + "counters_spis t " +
                " Where k.nzp_kvar = t.nzp and t.nzp_type = 3 and t.is_actual <> 100 and k.kod = 0 " +

                "   and t.nzp_counter = a.nzp " +
                "   and a.is_actual <> 100 " +
                "   and a.dat_s  <= " + rashod.paramcalc.dat_po +
                "   and a.dat_po >= " + rashod.paramcalc.dat_s +
                "   and a.nzp_prm = 979 " +

                "   and 1 > ( " +
                "     Select count(*) From aid_i" + rashod.paramcalc.pref + " n" +
                "     Where a.nzp = n.nzp_counter and a.dat_po >= n.dat_s and a.dat_s <= n.dat_po" +
                "     ) " +

                " Group by 1,2,3,4,5,6 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            if (!b_calc_kvar)
            {
                // ОДПУ
                ret = ExecSQL(conn_db,
                    " Insert into ttt_ans_kpu (nzp_kvar, nzp_dom, nzp_counter, sq, sqot, nzp_serv, val, nzp_type) " +
                    " Select 0 nzp_kvar,k.nzp_dom, t.nzp_counter,0 sq,0 sqot, t.nzp_serv, "+
                    "max(replace( " + sNvlWord + "(a.val_prm,'0'), ',', '.')" + sConvToNum + ") as val  " +
                    ",1 From " + rashod.paramcalc.data_alias + "prm_17 a, t_opn k, " + rashod.paramcalc.data_alias + "counters_spis t " +
                    " Where k.nzp_dom = t.nzp and t.nzp_type = 1 and t.is_actual <> 100 " +

                    "   and t.nzp_counter = a.nzp " +
                    "   and a.is_actual <> 100 " +
                    "   and a.dat_s  <= " + rashod.paramcalc.dat_po +
                    "   and a.dat_po >= " + rashod.paramcalc.dat_s +
                    "   and a.nzp_prm = 979 " +

                    "   and 1 > ( " +
                    "     Select count(*) From aid_i" + rashod.paramcalc.pref + " n" +
                    "     Where a.nzp = n.nzp_counter and a.dat_po >= n.dat_s and a.dat_s <= n.dat_po" +
                    "     ) " +

                    " Group by 1,2,3,4,5,6 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }

            ExecSQL(conn_db, " Create index ix_ttt_ans_kpu on ttt_ans_kpu (nzp_serv,nzp_type) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_ans_kpu ", true);

            // для отопления val1 - на квартиру, val3 - на 1 кв.метр
            ret = ExecSQL(conn_db,
              " Insert into " + rashod.counters_xx +
              " ( stek,nzp_type,dat_charge,nzp_kvar,nzp_dom,nzp_counter,dat_s,dat_po, squ1, squ2, nzp_serv, val1, val3 ) " +
              " Select " + sUniqueWord +
              " 2,3, " + p_dat_charge + " , nzp_kvar, nzp_dom, nzp_counter," + rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po + "," +
              " sq, sqot, nzp_serv, val, case when sqot > 0  then val / sqot else 0 end " +
              " From ttt_ans_kpu where nzp_serv = 8 and nzp_type = 3 "
              , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            // остальные услуги
            ret = ExecSQL(conn_db,
              " Insert into " + rashod.counters_xx +
              " ( stek,nzp_type,dat_charge,nzp_kvar,nzp_dom,nzp_counter,dat_s,dat_po, squ1, squ2, nzp_serv, val1 ) " +
              " Select " + sUniqueWord +
              " 2,3, " + p_dat_charge + " , nzp_kvar, nzp_dom, nzp_counter," + rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po + "," +
              " sq, sqot, nzp_serv, val " +
              " From ttt_ans_kpu where nzp_serv <> 8 and nzp_type = 3 "
              , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            // для ОДПУ
            ret = ExecSQL(conn_db,
              " Insert into " + rashod.counters_xx +
              " ( stek,nzp_type,dat_charge,nzp_kvar,nzp_dom,nzp_counter,dat_s,dat_po, squ1, squ2, nzp_serv, val1 ) " +
              " Select " + sUniqueWord +
              " 2,1, " + p_dat_charge + " , nzp_kvar, nzp_dom, nzp_counter," + rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po + "," +
              " sq, sqot, nzp_serv, val " +
              " From ttt_ans_kpu where nzp_type = 1 "
              , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            #endregion стек 2 - отсутствие показаний в лс в расчетном месяце, но есть ПУ (норма или среднее)

            #region стек 3 - сформировать итоговые суммы по лс, по-умолчанию нормативы

                #region 1 стек 3 - сформировать итоговые суммы по лс, по-умолчанию нормативы
                //----------------------------------------------------------------
                //стек 3 - сформировать итоговые суммы по лс, по-умолчанию нормативы
                //----------------------------------------------------------------
            
                ret = ExecSQL(conn_db,
                    " Insert into " + rashod.counters_xx +
                    " ( mmnog,stek,nzp_type,dat_charge,nzp_kvar,nzp_dom,nzp_counter,dat_s,dat_po, squ1, squ2, nzp_serv ) " +
                    " Select " + sUniqueWord +
                    " -100,3,3, " + p_dat_charge + " , nzp_kvar, nzp_dom, 0," + rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po + ", sq, sqot, nzp_serv " +
                    " From ttt_aid_sq Where 1 = 1 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }


                UpdateStatistics(false, paramcalc, rashod.counters_tab, out ret);
                //UpdateStatisticsCounters_xx(rashod, out ret);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                #endregion 1 стек 3 - сформировать итоговые суммы по лс, по-умолчанию нормативы

                #region стек 3 - отдельно выделим лс, где вообще нет ПУ по услуге (на всякий случай)
                //----------------------------------------------------------------
                //стек 3 - отдельно выделим лс, где вообще нет ПУ по услуге (на всякий случай)
                //----------------------------------------------------------------
                sql =
                        " Update " + rashod.counters_xx +
                        " Set nzp_counter = -2 " + //
                        " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                        "   and 1 > ( Select count(*) From " + rashod.paramcalc.data_alias + "counters_spis c " +
                                    " Where " + rashod.counters_xx + ".nzp_kvar = c.nzp " +
                                    "   and " + rashod.counters_xx + ".nzp_serv = c.nzp_serv " +
                                    "   and c.nzp_type = 3 " +
                    //"   and 1 > ( Select count(*) From aid_nc"+pref n Where c.nzp_counter = n.nzp_counter ) "+
                           " ) ";

                if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                }
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                #endregion стек 3 - отдельно выделим лс, где вообще нет ПУ по услуге (на всякий случай)

                #region стек 3 - отдельно выделим коммунальные лс - заполним в mmnog ikvar?, нет - nzp_kvar!, т.к. ikvar м.б. = 0
                //----------------------------------------------------------------
                //стек 3 - отдельно выделим коммунальные лс - заполним в mmnog ikvar?, нет - nzp_kvar!, т.к. ikvar м.б. = 0
                //----------------------------------------------------------------
                ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

                // добавить назначенные коммунальные квартиры
                ret = ExecSQL(conn_db,
                      " Select k.nzp_kvar,k.nzp_dom,t.nzp_kvar_base as mmnog " +
#if PG
                      " Into temp ttt_aid_c1 "+
#else

#endif
                      " From " + rashod.paramcalc.data_alias + "kvar k," + rashod.paramcalc.data_alias + "link_ls_lit t, ttt_prm_1 p  " +
                      " Where k." + rashod.where_dom + rashod.where_kvarK +
                      "   and k.nzp_kvar = t.nzp_kvar " +
                      "   and k.nzp_kvar = p.nzp " +
                      "   and p.nzp_prm = 3 " +
                      "   and p.val_prm = '2' " +
                      " Group by 1,2,3 "
#if PG

#else
                    +" Into temp ttt_aid_c1 With no log "
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                // добавить остальные коммунальные квартиры
                ret = ExecSQL(conn_db,
                      " insert into ttt_aid_c1 (nzp_kvar,nzp_dom,mmnog) " +
                      " Select k.nzp_kvar,k.nzp_dom,max(case when k.ikvar > 0 then k.ikvar else k.nzp_kvar end) as mmnog " +
                      " From " + rashod.paramcalc.data_alias + "kvar k, ttt_prm_1 p  " +
                      " Where k." + rashod.where_dom + rashod.where_kvarK +
                      "   and k.nzp_kvar = p.nzp " +
                      "   and p.nzp_prm = 3 " +
                      "   and p.val_prm = '2' " +
                      "   and 0=(select count(*) from " + rashod.paramcalc.data_alias + "link_ls_lit a where a.nzp_kvar=k.nzp_kvar)" +
                      " Group by 1,2 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            

                ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1 on ttt_aid_c1 (nzp_kvar) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);

                #endregion стек 3 - отдельно выделим коммунальные лс - заполним в mmnog ikvar?, нет - nzp_kvar!, т.к. ikvar м.б. = 0

                #region Сохранить mmnog в counters_xx
                sql =
                        " Update " + rashod.counters_xx +
                        " Set mmnog = (( Select mmnog From ttt_aid_c1 k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar )) " +
                        " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                        "   and 0 < ( Select count(*) From ttt_aid_c1 k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar ) ";
                        /*
                        " Update " + rashod.counters_xx +
                        " Set mmnog = (( Select max(case when k.ikvar > 0 then k.ikvar else k.nzp_kvar end) " +
                                       " From " + rashod.paramcalc.pref + "_data:kvar k, " + rashod.paramcalc.pref + "_data:prm_1 p  " +
                                       " Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar " +
                                       "   and k." + rashod.where_dom +
                                       "   and k.nzp_kvar = p.nzp " +
                                       "   and p.nzp_prm = 3 " +
                                       "   and p.val_prm = '2' " +
                                       "   and p.is_actual <> 100 " +
                                       "   and p.dat_s  <= " + rashod.paramcalc.dat_po +
                                       "   and p.dat_po >= " + rashod.paramcalc.dat_s +
                                       " )) " + //
                        " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                        "   and 0 < ( Select count(*) From " + rashod.paramcalc.pref + "_data:kvar k, " + rashod.paramcalc.pref + "_data:prm_1 p  " +
                                    " Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar " +
                                    "   and k." + rashod.where_dom +
                                    "   and k.nzp_kvar = p.nzp " +
                                    "   and p.nzp_prm = 3 " +
                                    "   and p.val_prm = '2' " +
                                    "   and p.is_actual <> 100 " +
                                    "   and p.dat_s  <= " + rashod.paramcalc.dat_po +
                                    "   and p.dat_po >= " + rashod.paramcalc.dat_s +
                                    " ) "
                        */

                if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 30000, " ", out ret);
                }
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                // информациию о коммуналках пока не удаляем - ttt_aid_c1 - сейчас используем для стека 29
                #endregion Сохранить mmnog в counters_xx

            #endregion стек 3 - сформировать итоговые суммы по лс, по-умолчанию нормативы

            #region стек 3 - сформировать итоговые строки по дому (пока без сумм!)

            if (!b_calc_kvar)
            {
                //
                ret = ExecSQL(conn_db,
                    " Insert into " + rashod.counters_xx +
                    " ( stek,nzp_type,dat_charge,nzp_kvar,nzp_dom,nzp_counter,dat_s,dat_po, squ1, nzp_serv ) " +
                    " Select " + sUniqueWord +
                    " 3,1, " + p_dat_charge + " ,0, nzp_dom, 0," + rashod.paramcalc.dat_s + "," +
                    rashod.paramcalc.dat_po + ", 0, nzp_serv " +
                    " From ttt_aid_sq Where 1 = 1 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }

            #endregion стек 3 - сформировать итоговые строки по дому (пока без сумм!)

            #region Подготовить перечень коммуналок и их параметров 
            
            // пока приготовим перечень коммуналок
            ExecSQL(conn_db, " Drop table t_ans_kommunal ", false);
            ret = ExecSQL(conn_db,
                " Create temp table t_ans_kommunal (" +
                " nzp_no serial not null," +
                " nzp_dom  integer not null, " +
                " nzp_kvar integer not null, " +
                " nkvar  " + sDecimalType + "(14,7) not null, " +
                " val1   " + sDecimalType + "(14,7) default 0.0000000, " +
                " val2   " + sDecimalType + "(14,7) default 0.0000000, " +
                " val3   " + sDecimalType + "(14,7) default 0.0000000, " +
                " val4   " + sDecimalType + "(14,7) default 0.0000000, " +
                " kf307  " + sDecimalType + "(14,7) default 0.0000000, " +
                " kf307n " + sDecimalType + "(14,7) default 0.0000000, " +
                " kf354  " + sDecimalType + "(14,7) default 0.0000000, " +
                " squ1   " + sDecimalType + "(14,7) default 0.0000000, " +
                " squ2   " + sDecimalType + "(14,7) default 0.0000000, " +
                " gil1   " + sDecimalType + "(14,7) default 0.0000000, " +
                " cnt1 integer default 0, " +
                " rastot " + sDecimalType + "(14,7) default 0.0000000, " +   //rashod
                " rasind " + sDecimalType + "(14,7) default 0.0000000, " +   //val_s 
                " kol_komn integer default 0, " +               //cnt5
                " cnt3 integer     default 0, " +               //nzp_res
                " is2075 integer   default 0  " +               // признак спец коммуналки     
#if PG
                " ) "
#else
                " ) With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

#if PG
            string serial_fld = "";
            string serial_val = "";
#else
            string serial_fld = ",nzp_no";
            string serial_val = ",0";
#endif
            if (!b_calc_kvar)
            {
                //----------------------------------------------------------------
                // заполнить стек 29 для сохранения доли коммуналки в площади квартиры для Пост 354 только при расчете дома
                // при расчете по 1 л/с используется чтобы не пересчитывать все коммунальные комнаты квартиры
                //----------------------------------------------------------------

                // список коммуналок с жилыми и общими площадями - по ttt_aid_sq вставятся только открытые ЛС!
                ret = ExecSQL(conn_db,
                    " Insert into t_ans_kommunal " +
                    " (nzp_kvar,nzp_dom,nkvar,squ1,squ2" + serial_fld + ") " +
                    " Select a.nzp_kvar,a.nzp_dom,b.mmnog,max(a.sq),max(a.sqgil)" + serial_val + 
                    " From ttt_aid_sq a,ttt_aid_c1 b" +
                    " Where a.nzp_kvar=b.nzp_kvar " +
                    " group by 1,2,3 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // дополнить список коммуналок закрытыми коммуналками с жилыми и общими площадями
                ExecSQL(conn_db, " Drop table t_ans_komm_sq ", false);
                ret = ExecSQL(conn_db,
                    " Create temp table t_ans_komm_sq " +
                    " ( nzp_kvar integer," +
                    "   nzp_dom  integer," +
                    "   mmnog    integer," +
                    "   sq       " + sDecimalType + "(12,4) default 0," +
                    "   sqgil    " + sDecimalType + "(12,4) default 0," +
                    "   sqot     " + sDecimalType + "(12,4) default 0 " +
#if PG
                    " )  "
#else
                    " ) With no log "
#endif
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // только открытые лс
                ret = ExecSQL(conn_db,
                    " Insert into t_ans_komm_sq (nzp_kvar,nzp_dom,mmnog) " +
                    " Select t.nzp_kvar,t.nzp_dom,t.mmnog " +
                    " From ttt_aid_c1 t" +
                    " Where 0=(Select count(*) From t_ans_kommunal a where t.nzp_kvar=a.nzp_kvar) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Create unique index ix_ans11_sq on t_ans_komm_sq (nzp_kvar) ", true);
                ExecSQL(conn_db, sUpdStat + " t_ans_komm_sq ", true);

                // общая площадь
                sql =
                    " Update t_ans_komm_sq " +
                    " Set sq   = ( select p.val_prm from ttt_prm_1 p where p.nzp=t_ans_komm_sq.nzp_kvar and p.nzp_prm=4 )" + sConvToNum.Trim() + " " +
                    " Where  0 < ( select count(*)  from ttt_prm_1 p where p.nzp=t_ans_komm_sq.nzp_kvar and p.nzp_prm=4 ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // отапливаемая площадь
                sql =
                    " Update t_ans_komm_sq " +
                    " Set sqot = ( select p.val_prm from ttt_prm_1 p where p.nzp=t_ans_komm_sq.nzp_kvar and p.nzp_prm=133 ) " + sConvToNum.Trim() + " " +
                    " Where  0 < ( select count(*)  from ttt_prm_1 p where p.nzp=t_ans_komm_sq.nzp_kvar and p.nzp_prm=133 ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // жилая площадь - нужна для коммуналок - для разделения расхода по Пост.354
                sql =
                    " Update t_ans_komm_sq " +
                    " Set sqgil = ( select p.val_prm from ttt_prm_1 p where p.nzp=t_ans_komm_sq.nzp_kvar and p.nzp_prm=6 ) " + sConvToNum.Trim() + " " +
                    " Where   0 < ( select count(*)  from ttt_prm_1 p where p.nzp=t_ans_komm_sq.nzp_kvar and p.nzp_prm=6 ) ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into t_ans_kommunal " +
                    " (nzp_kvar,nzp_dom,nkvar,squ1,squ2" + serial_fld + ") " +
                    " Select nzp_kvar,nzp_dom,mmnog,max(sq),max(sqgil)" + serial_val +
                    " From t_ans_komm_sq " +
                    " Where sq > 0 or sqgil > 0 " +
                    " group by 1,2,3 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                //
                ExecSQL(conn_db, " Drop table t_ans_komm_sq ", false);
            }
            else
            {
                // для расчета по л/с t_ans_kommunal не создан!
                // nzp_kvar в cur_zap!!! иначе удалится пр расчете 1го л/с! пока ничего другого не придумал!?
                ret = ExecSQL(conn_db,
                    " Insert into t_ans_kommunal" +
                    " (nzp_dom,nzp_kvar,nkvar,val1,val2,val3,val4,squ1,squ2,gil1,cnt1,kf307,kf307n, rastot, rasind,kol_komn, is2075" + serial_fld + ")" +
                    " select nzp_dom,cur_zap,mmnog,val1,val2,val3,val4,squ1,squ2,gil1,cnt1,kf307,kf307n, rashod, val_s, cnt5, cnt4" + serial_val +
                    " From " + rashod.counters_xx +
                    " Where nzp_type = 3 and stek = 29  " + rashod.where_kvar.Replace("nzp_kvar","cur_zap") + rashod.paramcalc.per_dat_charge
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }
            ExecSQL(conn_db, " Create unique index ix1_t_ans_kommunal on t_ans_kommunal (nzp_no) ", true);
            ExecSQL(conn_db, " Create unique index ix2_t_ans_kommunal on t_ans_kommunal (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " t_ans_kommunal ", true);

            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            #endregion Подготовить перечень коммуналок и их параметров

            #region стек 4 - изменения счетчика: выбрать расходы
            //----------------------------------------------------------------
            //стек 4 - изменения счетчика: выбрать расходы
            //----------------------------------------------------------------
#if PG
            ExecSQL(conn_db, " Drop view ttt_aid_norma ", false);
#else
            ExecSQL(conn_db, " Drop table ttt_aid_norma ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_norma " +
                " ( no       serial, " +
                "   nzp_kvar integer, " +
                "   nzp_dom  integer, " +
                "   nzp_serv integer, " +
                "   val1     " + sDecimalType + "(12,4)," +
                "   sq       " + sDecimalType + "(12,4)," +
                "   sqot     " + sDecimalType + "(12,4) " +
                " ) With no log "
                , true);
#endif
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            /*
            25 60	Изменение счетчика эл/эн	                    0    	float	0	1	0	1000000	7
            6  112	Изменение счетчика хол.воды	                    0    	float	0	1	0	1000000	7
            9  113	Изменение счетчика гор.воды	                    0    	float	0	1	0	1000000	7
            10 114	Изменение счетчика газа	                        0    	float	0	1	0	1000000	7
            8  115	Изменение счетчика отопления	                0    	float	0	1	0	1000000	7
            7  116	Изменение счетчика канализации	                0    	float	0	1	0	1000000	7

            210 381	Изменение счетчика ночное эл/эн	                0    	float	0	1	0	1000000	7
                389	Изменение счетчика по Эл/Эн лифтов (квт/час)	1    	float	0	1	0	1000000	7
            200 413	Изменение счетчика полив	                    0    	float	0	1	0	1000000	7
                442	Изменение счетчика бани	                        0    	float	0	1	0	1000000	7
                473	Изменение счетчика ХП эл/эн	                    0    	float	0	1	0	1000000	7
            253 707	Изменение счетчика питьевой воды                0    	float	0	1	0	1000000	7

               229	Изменение домового счетчика по отоплению	    1    	float	0	2	0	1000000	7
            */
            // s_counts.nzp_prm_val - изменение счетчика на ЛС
            ret = ExecSQL(conn_db,
#if PG
                "Create temp view ttt_aid_norma as " +
#else
                " Insert into ttt_aid_norma (nzp_kvar,nzp_dom,nzp_serv,sq,sqot, val1) " +
#endif
                " Select c.nzp_kvar,c.nzp_dom, c.nzp_serv, c.sq, c.sqot, p.val_prm" + sConvToNum + " as val1 " +
                " From ttt_aid_sq c, ttt_prm_1 p," + rashod.paramcalc.kernel_alias + "s_counts s " +
                " Where p.nzp    = c.nzp_kvar " +
                  " and p.nzp_prm=s.nzp_prm_val and c.nzp_serv=s.nzp_serv " +
                /*
                "   and p.nzp_prm in (60,112,113,114,115,116,381,413,707) " +
                "   and p.nzp_prm=(case when c.nzp_serv =25 then 60 " +
                      "  else case when c.nzp_serv =6 then 112 " +
                            " else case when c.nzp_serv =9 then 113 " +
                                 " else case when c.nzp_serv =10 then 114 " +
                                      " else case when c.nzp_serv =8 then 115 " +
                                           " else case when c.nzp_serv =7 then 116 " +
                                                " else case when c.nzp_serv =210 then 381 " +
                                                     " else case when c.nzp_serv =200 then 413 " +

                                                          " else case when c.nzp_serv =253 then 707 else 0 " +
                                                               " end " +

                                                          " end " +
                                                     " end " +
                                                " end " +
                                           " end " +
                                      " end " +
                                 " end " +
                            " end " +
                      "  end ) " +
                */
                " Group by 1,2,3,4,5,6 "
                 , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
#if PG
#else
            ExecSQL(conn_db, " Create unique index ix_aid00_n on ttt_aid_norma (no) ", true);
            ExecSQL(conn_db, " Create        index ix_aid11_n on ttt_aid_norma (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_norma ", true);
#endif

            sql =
                    " Insert into " + rashod.counters_xx +
                    " ( stek,nzp_type,dat_charge,nzp_kvar,nzp_dom,nzp_counter, squ1, squ2,val1,val3,dat_s,dat_po,nzp_serv ) " +
                    " Select 5,3, " + p_dat_charge + " ,nzp_kvar,nzp_dom, 0, sq, sqot," +
                    " case when nzp_serv=8 then val1 * sqot else val1 end, case when nzp_serv=8 then val1 else 0 end," +
                    rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po + ", nzp_serv " +
                    " From ttt_aid_norma Where 1=1 ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, "ttt_aid_norma", "no", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            UpdateStatistics(false, paramcalc, rashod.counters_tab, out ret);
            //UpdateStatisticsCounters_xx(rashod, out ret);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            #endregion стек 4 - изменения счетчика: выбрать расходы

            #region стек 5 - средние значения: выбрать расходы
            //----------------------------------------------------------------
            //стек 5 - средние значения: выбрать расходы
            //----------------------------------------------------------------
#if PG
            ExecSQL(conn_db, " Drop view ttt_aid_norma ", false);
#else
            ExecSQL(conn_db, " Drop table ttt_aid_norma ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_norma " +
                " ( no       serial, " +
                "   nzp_kvar integer, " +
                "   nzp_dom  integer, " +
                "   nzp_serv integer, " +
                "   val1     " + sDecimalType + "(12,4)," +
                "   sq       " + sDecimalType + "(12,4)," +
                "   sqot     " + sDecimalType + "(12,4) " +
                " ) With no log "
                , true);
#endif
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            /*
            25 22	Среднее значение счетчика электроэнергии	        1		float	0	1
            6  135	Среднее значение счетчика хол.воды	                1		float	0	1
            9  136	Среднее значение счетчика гор.воды	                1		float	0	1
            10 137	Среднее значение счетчика газа	                    0		float	0	1
            8  138	Среднее значение счетчика отопления	                1		float	0	1
            7  139	Среднее значение счетчика канализации	            1		float	0	1
            210 380	Среднее значение счетчика электроэнергии	        1		float	0	1
            200 412	Среднее значение счетчика полив         	        1		float	0	1
            706 253	Изменение счетчика питьевой воды                    0    	float	0	1

            228	Среднее значение домового счетчика по отоплению	        1	    float	0	2
            373	Среднее значение домового счетчика по хол.воде	        1	    float	0	2
            374	Среднее значение домового счетчика по гор.воде	        1	    float	0	2
            375	Среднее значение домового счетчика по канализации	    1	    float	0	2
            376	Среднее значение домового счетчика по электроэнергии	1       float	0	2
            */
            // s_counts.nzp_prm_sred - Среднее счетчика на ЛС
            ret = ExecSQL(conn_db,
#if PG
                "Create temp view ttt_aid_norma as " +
#else
                " Insert into ttt_aid_norma (nzp_kvar,nzp_dom,nzp_serv,sq,sqot, val1) " +
#endif
                " Select c.nzp_kvar,c.nzp_dom, c.nzp_serv, c.sq, c.sqot, p.val_prm"+ sConvToNum + " as val1 " +
                " From ttt_aid_sq c, ttt_prm_1 p," + rashod.paramcalc.kernel_alias + "s_counts s " +
                " Where p.nzp    = c.nzp_kvar " +
                  " and p.nzp_prm=s.nzp_prm_sred and c.nzp_serv=s.nzp_serv " +
                /*
                "   and p.nzp_prm in (22,135,136,137,138,139,380,412) " +
                "   and p.nzp_prm=(case when c.nzp_serv =25 then 22 " +
                      "  else case when c.nzp_serv =6 then 135 " +
                            " else case when c.nzp_serv =9 then 136 " +
                                 " else case when c.nzp_serv =10 then 137 " +
                                      " else case when c.nzp_serv =8 then 138 " +
                                           " else case when c.nzp_serv =7 then 139 " +
                                                " else case when c.nzp_serv =210 then 380 " +
                                                     " else case when c.nzp_serv =200 then 412 " +

                                                          " else case when c.nzp_serv =253 then 706 else 0 " +
                                                               " end " +

                                                          " end " +
                                                     " end " +
                                                " end " +
                                           " end " +
                                      " end " +
                                 " end " +
                            " end " +
                      "  end ) " +
                */
                " Group by 1,2,3,4,5,6 "
                 , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
#if PG
#else
            ExecSQL(conn_db, " Create unique index ix_aid00_n on ttt_aid_norma (no) ", true);
            ExecSQL(conn_db, " Create        index ix_aid11_n on ttt_aid_norma (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_norma ", true);
#endif

            sql =
                    " Insert into " + rashod.counters_xx +
                    " ( stek,nzp_type,dat_charge,nzp_kvar,nzp_dom,nzp_counter, squ1, squ2,val1,val3,dat_s,dat_po,nzp_serv ) " +
                    " Select 4,3, " + p_dat_charge + " ,nzp_kvar,nzp_dom, 0, sq, sqot," +
                    " case when nzp_serv=8 then val1 * sqot else val1 end, case when nzp_serv=8 then val1 else 0 end," +
                    rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po + ", nzp_serv " +
                    " From ttt_aid_norma Where 1=1 ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, "ttt_aid_norma", "no", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            UpdateStatistics(false, paramcalc, rashod.counters_tab, out ret);
            //UpdateStatisticsCounters_xx(rashod, out ret);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            #endregion стек 5 - средние значения: выбрать расходы

            #region площади по лс - уже вставлены при  insert в stek=3 из ttt_aid_sq (заремлено )
            //----------------------------------------------------------------
            //площади по лс - уже вставлены при  insert в stek=3 из ttt_aid_sq
            //----------------------------------------------------------------
            /*
            ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx",
                " Update " + rashod.counters_xx +
                " Set squ1 = ( Select max(sq) From ttt_aid_sq a " +
                             " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar ) " +
                " Where nzp_type = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge
              , 100000, " ", out ret);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            */

            #endregion площади по лс - уже вставлены при  insert в stek=3 из ttt_aid_sq (заремлено )

            #region Заполнение количества жильцов 
            //----------------------------------------------------------------
            //кол-во жильцов по лс
            //----------------------------------------------------------------
            // gil_xx - cnt1 - целое число жильцов без учета правила 15 дней, но с учетом временно выбывших
            // gil_xx - cnt2 - кол-во жильцов с учетом правила 15 дней без учета временно выбывших
            // gil_xx - val1 - дробное число жильцов без учета правила 15 дней, но с учетом временно выбывших
            // gil_xx - val2 - кол-во жильцов цифрой из БД (выбран nzp_prm=5 из prm_1)
            // gil_xx - val3 - кол-во временно прибывших (выбран nzp_prm=131 из prm_1)
            // gil_xx - val5 - временно выбывших (nzp_prm=10 - это всегда из gil_periods)
              
            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            ret = ExecSQL(conn_db,
                  " Select nzp_kvar, max(cnt2 - val5 + val3) as vald, max(cnt2 + val3) as vald_g " +
#if PG
                    " Into temp ttt_aid_c1  " +
#else
#endif
                  " From " + rashod.gil_xx + " g " +
                  " Where " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge + " and g.stek = 3 " +
                  " Group by 1 "
#if PG
#else
                    +" Into temp ttt_aid_c1 With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1 on ttt_aid_c1 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);

            //дробное кол-в жильцов (ссылка get_kol_gil)
            sql =
                    " Update " + rashod.counters_xx + " Set "+
#if PG
                    " gil1 = ( Select vald From ttt_aid_c1 k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar ),"+
                    " gil1_g = ( Select vald_g From ttt_aid_c1 k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar ) " +
#else
                    " (gil1,gil1_g) = (( Select vald,vald_g From ttt_aid_c1 k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar )) " +
#endif
                    " Where nzp_type = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                    "   and 0 < ( Select count(*) From ttt_aid_c1 k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            sql =
                    " Update " + rashod.counters_xx +
                    " Set cnt1 = Round(gil1),cnt1_g = Round(gil1_g) " +
                    " Where nzp_type = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                    "   and gil1_g > 0 ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            #endregion Заполнение количества жильцов

            #region 29 стек для коммуналок в случае если расчет домовой !b_calc_kvar
            if (!b_calc_kvar)
            {
                //----------------------------------------------------------------
                // заполнить стек 29 для сохранения доли коммуналки в площади квартиры для Пост 354 только при расчете дома
                // при расчете по 1 л/с используется чтобы не пересчитывать все коммунальные комнаты квартиры
                //----------------------------------------------------------------

                // сохраним кол-во жильцов для коммуналок для стека 29 - поможет при показе распределний по коммуналкам расходов
                ret = ExecSQL(conn_db,
                    " Update t_ans_kommunal " +
                    " Set gil1 = ( Select vald From ttt_aid_c1 k Where t_ans_kommunal.nzp_kvar = k.nzp_kvar ) " +
                    " Where 0 < ( Select count(*) From ttt_aid_c1 k Where t_ans_kommunal.nzp_kvar = k.nzp_kvar ) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ret = ExecSQL(conn_db,
                    " Update t_ans_kommunal " +
                    " Set cnt1 = Round(gil1) " +
                    " Where 1=1 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                // расчет суммарных площадей и кол-ва жильцов по квартирам, где есть коммуналки
                ExecSQL(conn_db, " Drop table t_ans_itog ", false);
                ret = ExecSQL(conn_db,
                    " Select nzp_dom,nkvar,sum(squ1) squ1,sum(squ2) squ2,sum(gil1) gil1,sum(cnt1) cnt1 " +
#if PG
                    " Into temp t_ans_itog "+
#else
#endif
                    " From t_ans_kommunal " +
                    " Group by 1,2 "
#if PG
#else
                    +" Into temp t_ans_itog with no log "
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                ExecSQL(conn_db, " Create index ix_t_ans_itog on t_ans_itog (nzp_dom,nkvar) ", true);
                ExecSQL(conn_db, sUpdStat + " t_ans_itog ", true);

                ret = ExecSQL(conn_db,
                    " Update t_ans_kommunal Set " +
#if PG
                    " val1 = ( Select squ1 From t_ans_itog k Where t_ans_kommunal.nzp_dom = k.nzp_dom and t_ans_kommunal.nkvar = k.nkvar )," +
                    " val2 = ( Select squ2 From t_ans_itog k Where t_ans_kommunal.nzp_dom = k.nzp_dom and t_ans_kommunal.nkvar = k.nkvar )," +
                    " val3 = ( Select gil1 From t_ans_itog k Where t_ans_kommunal.nzp_dom = k.nzp_dom and t_ans_kommunal.nkvar = k.nkvar )," +
                    " val4 = ( Select cnt1 From t_ans_itog k Where t_ans_kommunal.nzp_dom = k.nzp_dom and t_ans_kommunal.nkvar = k.nkvar ) " +
#else
                    " (val1,val2,val3,val4) =" +
                    " (( Select squ1,squ2,gil1,cnt1 From t_ans_itog k" +
                    "    Where t_ans_kommunal.nzp_dom = k.nzp_dom and t_ans_kommunal.nkvar = k.nkvar )) " +
#endif
                    " Where 0 < ( Select count(*) From t_ans_itog k" +
                    "             Where t_ans_kommunal.nzp_dom = k.nzp_dom and t_ans_kommunal.nkvar = k.nkvar ) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                if (Points.IsSmr)
                {
                // для самары коэф-т по жилой площади округлить до 4х знаков
                    sql =
                    " kf307  = case when val2>0.0001 then Round( (squ2/val2) * 10000 )/10000 else 0 end, " +
                    " kf307n = case when val3>0.0001 then gil1/val3 else 0 end  ";

                }
                else
                {
                    sql =
                    " kf307  = case when val2>0.0001 then squ2/val2 else 0 end, " +
                    " kf307n = case when val3>0.0001 then gil1/val3 else 0 end  ";
                }
                ret = ExecSQL(conn_db,
                    " Update t_ans_kommunal " +
                    " Set" + sql +
                    " Where 1=1 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }


                #region Признак дома с общагами домовой
                ret = ExecSQL(conn_db,
                    " Update t_ans_kommunal " +
                    " Set is2075 =1 " +
                    " Where 0 < ( Select count(*) From ttt_prm_2 k Where t_ans_kommunal.nzp_dom = k.nzp and k.nzp_prm =2075 and k.val_prm ='1' ) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                ret = ExecSQL(conn_db,
                    " Update t_ans_kommunal " +
                    " Set is2075 =1 " +
                    " Where 0 < ( Select count(*) From ttt_prm_1 k Where t_ans_kommunal.nzp_kvar = k.nzp and k.nzp_prm =2076 and k.val_prm ='1' ) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                #endregion Признак дома с общагами домовой

                //#region Количество комнат для общежитий проставить в t_ans_kommunal.kol_komn
                //sql = " Update t_ans_kommunal " +
                //        " Set kol_komn =1 " +
                //        " Where  0 < ( Select count(*) From ttt_aid_c1 k Where t_ans_kommunal.nzp_kvar = k.nzp_kvar ) and is2075=1 ";
                //ret = ExecSQL(conn_db, sql, true);
                //#endregion Количество комнат для общежитий проставить в t_ans_kommunal

                #region Перенес сохранение в другое место 29 стека
                //// nzp_kvar сохраним в cur_zap!!! иначе удалится пр расчете 1го л/с! пока ничего другого не придумал!?
                //sql =
                //    " Insert into " + rashod.counters_xx +
                //    " ( stek,nzp_type,dat_charge,nzp_kvar,cur_zap,nzp_dom,nzp_counter,nzp_serv,mmnog,val1,val2,val3,val4," +
                //    "   squ1,squ2,gil1,cnt1,kf307,kf307n,dat_s,dat_po ) " +
                //    " Select 29,3, " + p_dat_charge + ", 0,nzp_kvar,nzp_dom, 0, 0, nkvar," +
                //    " val1,val2,val3,val4,squ1,squ2,gil1,cnt1,kf307,kf307n,mdy(1,1,1900),mdy(1,1,1900) " +
                //    " From t_ans_kommunal Where 1=1 ";

                //if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                //{
                //    ret = ExecSQL(conn_db, sql, true);
                //}
                //else
                //{
                //    ExecByStep(conn_db, "t_ans_kommunal", "nzp_no", sql, 100000, " ", out ret);
                //}
                //if (!ret.result)
                //{
                //    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                //    return false;
                //}

                //UpdateStatistics(false, paramcalc, rashod.counters_tab, out ret);
                //if (!ret.result)
                //{
                //    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                //    return false;
                //}
                #endregion Перенес сохранение в другое место
            }
            #endregion 29 стек для коммуналок в случае если расчет домовой !b_calc_kvar
     
            #region Заполнение кол-во комнат для Электричества
            //----------------------------------------------------------------
            //кол-во комнат для ЕЕ
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            ret = ExecSQL(conn_db,
                  " Select nzp as nzp_kvar, max(p.val_prm) as val3 " +
#if PG
                    " Into temp ttt_aid_c1 "+
#else
#endif
                  " From " + rashod.counters_xx + " a, ttt_prm_1 p " +
                  " Where a.nzp_kvar = p.nzp " +
                  "   and p.nzp_prm = 107 " +
                  "   and a.nzp_type = 3 and a.stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                  "   and a.nzp_serv in (25,210) " +
                  " Group by 1 "
#if PG
#else
                +" Into temp ttt_aid_c1 With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1 on ttt_aid_c1 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);

            sql =
                    " Update " + rashod.counters_xx +
                    " Set cnt2 = ( Select val3 From ttt_aid_c1 k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar )" + sConvToInt.Trim() +
                    " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                    "   and nzp_serv in (25,210) " +
                    "   and 0 < ( Select count(*) From ttt_aid_c1 k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            //если кол-во комнат нет, то по выставим опосредованно по площади nzp_res = 44
            sql =
                    " Update " + rashod.counters_xx +
                    " Set cnt2 = ( Select min( case when trim(b.value)" + sConvToInt.Trim() + " = 5 then 5 else trim(b.value)" + sConvToInt.Trim() + " - 1 end ) " +
                                 " From " + rashod.paramcalc.kernel_alias + "res_values a, " + rashod.paramcalc.kernel_alias + "res_values b  " +
                                 " Where a.nzp_res = 44 " +
                                 "   and a.nzp_res = b.nzp_res " +
                                 "   and a.nzp_y   = b.nzp_y   " +
                                 "   and b.nzp_x   > a.nzp_x   " +
                                 "   and " + rashod.counters_xx + ".squ1 > " + rashod.paramcalc.data_alias + "sortnum( trim(a.value) ) " +
                                 " ) " +
                    " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                    "   and nzp_serv in (25,210) " +
                    "   and squ1 > 0 " +
                    "   and cnt2 = 0 ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            #endregion Заполнение кол-во комнат для Электричества

            #region Заполнение тип водоснабжения для ХВС-ГВС
            //----------------------------------------------------------------
            //тип водоснабжения для ХВС-ГВС
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_c1 " +
                " ( nzp_kvar integer, " +
                "   nzp_serv integer, " +
                "   val3     " + sDecimalType + "(12,4) " +
#if PG
                " ) "
#else
                " ) With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            // ХВС
            ret = ExecSQL(conn_db,
                  " insert into ttt_aid_c1 (nzp_kvar, nzp_serv, val3) " +
                  " Select nzp as nzp_kvar, 6, max(p.val_prm" + sConvToNum + ") as val3 " +
                  " From " + rashod.counters_xx + " a, ttt_prm_1 p " +
                  " Where a.nzp_kvar = p.nzp " +
                  "   and p.nzp_prm = 7 " +
                  "   and a.nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                  "   and a.nzp_serv=6 "+
                  " Group by 1 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            // КАН
            int iNzpRes = 7;
            if (b_next_month7)
            {
                iNzpRes = 2007;
            }
            ret = ExecSQL(conn_db,
                  " insert into ttt_aid_c1 (nzp_kvar, nzp_serv, val3) " +
                  " Select nzp as nzp_kvar, a.nzp_serv, max(p.val_prm" + sConvToNum + ") as val3 " +
                  " From " + rashod.counters_xx + " a, ttt_prm_1 p " +
                  " Where a.nzp_kvar = p.nzp " +
                  "   and p.nzp_prm = " + iNzpRes.ToString() +
                  "   and a.nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                  "   and a.nzp_serv in (7,324) " +
                  " Group by 1,2 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            // ГВС
            // на дом
            ret = ExecSQL(conn_db,
                  " insert into ttt_aid_c1 (nzp_kvar, nzp_serv, val3) " +
                  " Select a.nzp_kvar, a.nzp_serv, max(replace( "+
                  sNvlWord + "(val_prm,'0'), ',', '.')" + sConvToNum + ") as val3 " +
                  " From " + rashod.counters_xx + " a, ttt_prm_2 p "+
                  " Where a.nzp_dom = p.nzp " +
                  "   and a.nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                  "   and a.nzp_serv in (9,281,323) " +
                  "   and p.nzp_prm = 38 " + 
                  " Group by 1,2 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            // на квартиру - в первую очередь
            ret = ExecSQL(conn_db,
                  " Update ttt_aid_c1 " +
                  " Set val3 = ( Select p.val_prm From ttt_prm_1 p Where ttt_aid_c1.nzp_kvar = p.nzp and p.nzp_prm = 463 ) " + sConvToNum + 
                  " Where nzp_serv in (9,281,323) " +
                  "   and 0  < ( Select count(*)  From ttt_prm_1 p Where ttt_aid_c1.nzp_kvar = p.nzp and p.nzp_prm = 463 ) "
                  , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Drop table ttt_aid_c1x ", false);
            ret = ExecSQL(conn_db, 
                " Select nzp_kvar,nzp_serv "+
#if PG
                " into temp ttt_aid_c1x " +
                " From ttt_aid_c1 Where nzp_serv in (9,281,323) "
#else
                " From ttt_aid_c1 Where nzp_serv in (9,281,323) " +
                " into temp ttt_aid_c1x with no log "
#endif
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1x on ttt_aid_c1x (nzp_kvar,nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1x ", true);

            ret = ExecSQL(conn_db,
                  " insert into ttt_aid_c1 (nzp_kvar, nzp_serv, val3) " +
                  " Select a.nzp,9, a.val_prm" + sConvToNum + 
                  " From ttt_prm_1 a " +
                  " Where a.nzp_prm = 463 " +
                  "   and 0  < ( Select count(*) From ttt_prm_1   p Where a.nzp = p.nzp and p.nzp_prm = 463 ) " +
                  "   and 0  = ( Select count(*) From ttt_aid_c1x b Where a.nzp = b.nzp_kvar and b.nzp_serv=9 ) "
                  , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ret = ExecSQL(conn_db,
                  " insert into ttt_aid_c1 (nzp_kvar, nzp_serv, val3) " +
                  " Select a.nzp,281, a.val_prm" + sConvToNum + 
                  " From ttt_prm_1 a " +
                  " Where a.nzp_prm = 463 " +
                  "   and 0  < ( Select count(*) From ttt_prm_1   p Where a.nzp = p.nzp and p.nzp_prm = 463 ) " +
                  "   and 0  = ( Select count(*) From ttt_aid_c1x b Where a.nzp = b.nzp_kvar and b.nzp_serv=281 ) "
                  , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ret = ExecSQL(conn_db,
                  " insert into ttt_aid_c1 (nzp_kvar, nzp_serv, val3) " +
                  " Select a.nzp,323, a.val_prm" + sConvToNum + 
                  " From ttt_prm_1 a " +
                  " Where a.nzp_prm = 463 " +
                  "   and 0  < ( Select count(*) From ttt_prm_1   p Where a.nzp = p.nzp and p.nzp_prm = 463 ) " +
                  "   and 0  = ( Select count(*) From ttt_aid_c1x b Where a.nzp = b.nzp_kvar and b.nzp_serv=323 ) "
                  , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1 on ttt_aid_c1 (nzp_kvar, nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);

            ExecSQL(conn_db, " Drop table ttt_aid_c1x ", false);

            sql =
                    " Update " + rashod.counters_xx +
                    " Set cnt2 = ( Select val3 From ttt_aid_c1 k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar" +
                      " and " + rashod.counters_xx + ".nzp_serv = k.nzp_serv ) " + sConvToInt +
                    " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                      " and nzp_serv in (6, 9, 7, 281, 323, 324) " +
                      " and 0 < ( Select count(*) From ttt_aid_c1 k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar" +
                      " and " + rashod.counters_xx + ".nzp_serv = k.nzp_serv ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                /*
                " Update " + rashod.counters_xx +
                " Set cnt2 = (( " +
                          " Select max(replace( nvl(val_prm,'0'), ',', '.')+0) " +
                          " From " + rashod.paramcalc.pref + "_data:prm_1 p " +
                          " Where " + rashod.counters_xx + ".nzp_kvar = p.nzp " +
                          "   and p.nzp_prm = 7 " +
                          "   and p.is_actual <> 100 " +
                          "   and p.dat_s  <= " + rashod.paramcalc.dat_po +
                          "   and p.dat_po >= " + rashod.paramcalc.dat_s +
                            " )) " +
                " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                "   and nzp_serv in (6,9, 7) "
              , 100000, " ", out ret);
              */
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            #endregion Заполнение тип водоснабжения для ХВС-ГВС

            #region Выборка нормативов

            //----------------------------------------------------------------
            //ссылки на нормативные значения (resolution.nzp_res) - в зависимости от месяца расчета!
            //----------------------------------------------------------------
            #region N: электроснабжение
            //N: электроснабжение
            int iNzpPrmEE = 730;
            if ( (new DateTime (rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm, 01)) >= (new DateTime(2012,09,01) ) )
            {
                iNzpPrmEE = 1079;
            }

            sql =
                    " Update " + rashod.counters_xx +
                    " Set cnt4 = " + iNzpPrmEE + //дом не-МКД (по всем нормативным строкам лс и домам)
                    " Where stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                    "   and nzp_serv in (25,210) " +
                    "   and 0 < (" +
                              " Select count(*) From ttt_prm_2 p " + 
                              " Where " + rashod.counters_xx + ".nzp_dom = p.nzp " +
                              "   and p.nzp_prm = " + iNzpPrmEE +
                              " ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);
            ret = ExecSQL(conn_db,
#if PG
                " create temp table ttt_aid_c1 (nzp_kvar integer) "
#else
                " create temp table ttt_aid_c1 (nzp_kvar integer) with no log "
#endif
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                  " Insert into ttt_aid_c1 (nzp_kvar)" +  
                  " Select nzp as nzp_kvar " +
                  " From " + rashod.counters_xx + " a, ttt_prm_1 p " +
                  " Where a.nzp_kvar = p.nzp " +
                  "   and p.nzp_prm = 19 " + // электроплита
                  "   and a.nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                  "   and a.nzp_serv in (25,210) " +
                  " Group by 1 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Drop table ttt_aid_c1x ", false);
            ret = ExecSQL(conn_db,
#if PG
                " Select nzp_kvar  into temp ttt_aid_c1x From ttt_aid_c1 Where 1=1 "
#else
                " Select nzp_kvar From ttt_aid_c1 Where 1=1 into temp ttt_aid_c1x with no log "
#endif
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1x on ttt_aid_c1x (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1x ", true);

            ret = ExecSQL(conn_db,
                  " Insert into ttt_aid_c1 (nzp_kvar)" +
                  " Select a.nzp_kvar " +
                  " From " + rashod.counters_xx + " a, ttt_prm_2 p " +
                  " Where a.nzp_dom = p.nzp " +
                  "   and p.nzp_prm = 28 " + // электроплита на дом
                  "   and a.nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                  "   and a.nzp_serv in (25,210) " +
                  "   and 0 = ( Select count(*) From ttt_aid_c1x k Where a.nzp_kvar = k.nzp_kvar ) " +
                  " Group by 1 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1 on ttt_aid_c1 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);

            ExecSQL(conn_db, " Drop table ttt_aid_c1x ", false);

            ret = ExecRead(conn_db, out reader,
            " Select val_prm" + sConvToInt + " val_prm From " + rashod.paramcalc.data_alias + "prm_13 " +
            " Where nzp_prm = 181 and is_actual <> 100 and dat_s  <= " + rashod.paramcalc.dat_po + " and dat_po >= " + rashod.paramcalc.dat_s
            , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            int iNzpResMKD = 0;
            if (reader.Read())
            {
                iNzpResMKD = Convert.ToInt32(reader["val_prm"]);
            }
            reader.Close();

            if (bIsSaha)
            {
                iNzpRes = iNzpResMKD;
            }
            else
            {

                ret = ExecRead(conn_db, out reader,
                " Select val_prm" + sConvToInt + " val_prm From " + rashod.paramcalc.data_alias + "prm_13 " +
                " Where nzp_prm = 183 and is_actual <> 100 and dat_s  <= " + rashod.paramcalc.dat_po + " and dat_po >= " + rashod.paramcalc.dat_s
                , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                iNzpRes = 0;
                if (reader.Read())
                {
                    iNzpRes = Convert.ToInt32(reader["val_prm"]);
                }
                reader.Close();
            }
            if ((iNzpRes != 0) && (iNzpResMKD != 0))
            {
                sql =
                        " Update " + rashod.counters_xx +
                        " Set cnt3 = (case when cnt4 = " + iNzpPrmEE + " then " + iNzpRes.ToString() + " else " + iNzpResMKD.ToString() + " end) " + //лс с электроплитой (в зависимости от МКД)
                        " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                        "   and nzp_serv in (25,210) " +
                        "   and 0 < ( Select count(*) From ttt_aid_c1 k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar ) ";

                if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                }
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }

            if (bIsSaha)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_c1x ", false);

                ret = ExecSQL(conn_db,
                     " Select a.nzp_kvar " +
#if PG
                     " into temp ttt_aid_c1x " +
#else
#endif
                      " From " + rashod.counters_xx + " a, ttt_prm_1 p " +
                      " Where a.nzp_kvar = p.nzp " +
                      "   and p.nzp_prm = 1172 " + // огневая плита
                      "   and a.nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                      "   and a.nzp_serv in (25,210) " +
                      " Group by 1 "
#if PG
#else
                    +" into temp ttt_aid_c1x with no log "
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                

                ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1x on ttt_aid_c1x (nzp_kvar) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_c1x ", true);

                ret = ExecRead(conn_db, out reader,
                " Select val_prm" + sConvToInt + " val_prm From " + rashod.paramcalc.data_alias + "prm_13 " +
                " Where nzp_prm = 182 and is_actual <> 100 and dat_s  <= " + rashod.paramcalc.dat_po + " and dat_po >= " + rashod.paramcalc.dat_s
                , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                iNzpRes = 0;
                if (reader.Read())
                {
                    iNzpRes = Convert.ToInt32(reader["val_prm"]);
                }
                reader.Close();

                if (iNzpRes != 0)
                {
                    sql =
                            " Update " + rashod.counters_xx +
                            " Set cnt3 = " + iNzpRes.ToString() + 
                            " Where stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                            "   and nzp_serv in (25,210) " +
                            "   and 0 < ( Select count(*) From ttt_aid_c1x k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar ) ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                }

                ExecSQL(conn_db, " Drop table ttt_aid_c1x ", false);
            }

            ret = ExecRead(conn_db, out reader,
            " Select val_prm" + sConvToInt + " val_prm From " + rashod.paramcalc.data_alias + "prm_13 " +
            " Where nzp_prm = 180 and is_actual <> 100 and dat_s  <= " + rashod.paramcalc.dat_po + " and dat_po >= " + rashod.paramcalc.dat_s
            , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            iNzpResMKD = 0;
            if (reader.Read())
            {
                iNzpResMKD = Convert.ToInt32(reader["val_prm"]);
            }
            reader.Close();

            if (bIsSaha)
            {
                iNzpRes = iNzpResMKD;
            }
            else
            {

                ret = ExecRead(conn_db, out reader,
                " Select val_prm" + sConvToInt + " val_prm From " + rashod.paramcalc.data_alias + "prm_13 " +
                " Where nzp_prm = 182 and is_actual <> 100 and dat_s  <= " + rashod.paramcalc.dat_po + " and dat_po >= " + rashod.paramcalc.dat_s
                , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                iNzpRes = 0;
                if (reader.Read())
                {
                    iNzpRes = Convert.ToInt32(reader["val_prm"]);
                }
                reader.Close();
            }

            if ((iNzpRes != 0) && (iNzpResMKD != 0))
            {
                sql =
                        " Update " + rashod.counters_xx +
                        " Set cnt3 = (case when cnt4 = " + iNzpPrmEE + " then " + iNzpRes.ToString() + " else " + iNzpResMKD.ToString() + " end) " + //значит остальные без электроплиты (где <> -2;-17)
                        " Where stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                        "   and nzp_serv in (25,210) " +
                        "   and cnt3 = 0 ";

                if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                }
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }

            string sKolGil = "1";
            string sKolGil_g = "1";
            if (Points.IsSmr || bIsSaha)
            {
                sKolGil = "cnt1"; sKolGil_g = "cnt1_g";
            }
            else
            {
                if ((new DateTime(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm, 01)) >= (new DateTime(2012, 09, 01))) //|| Points.Is50)
                {
                    sKolGil = "cnt1"; sKolGil_g = "cnt1_g";
                }
            }


            if (bIsSaha)
            {
                sql =
                        " Update " + rashod.counters_xx +
                        " Set val1 = (" + sNvlWord.Trim() 
                        +"(( Select a.value From " + rashod.paramcalc.kernel_alias + "res_values a " +
                                     " Where a.nzp_res = cnt3 " +

                                     "   and a.nzp_y = (" +
                                         " Select b.nzp_y From " + rashod.paramcalc.kernel_alias + "res_values b," +
                                         rashod.paramcalc.kernel_alias + "res_values pn," + rashod.paramcalc.kernel_alias + "res_values pk " +
                                         " Where b.nzp_res = cnt3 and pn.nzp_res = cnt3 and pk.nzp_res = cnt3 " +
                                         " and b.nzp_y=pn.nzp_y and pn.nzp_y=pk.nzp_y and b.nzp_x=1 and pn.nzp_x=2 and pk.nzp_x=3 " +
                                         " and (b.value+0)=(case when cnt2 >=4 then 4 else cnt2 end) " +
                                         " and pn.value<=squ1 and pk.value>=squ1 " +
                                         " ) " + //кол-во комнат (cnt2 >=4) & площадь в диапазоне!

                                     "   and a.nzp_x = (case when cnt1 >=5 then 5+3 else cnt1+3 end) " + //кол-во людей
                                     " ),'0') " + sConvToNum + ") " +
                                     " * " + sKolGil +
                        " , rash_norm_one = " + sNvlWord.Trim() +
                            "(( Select a.value From " + rashod.paramcalc.kernel_alias + "res_values a " +
                                     " Where a.nzp_res = cnt3 " +

                                     "   and a.nzp_y = (" +
                                         " Select b.nzp_y From " + rashod.paramcalc.kernel_alias + "res_values b," + 
                                         rashod.paramcalc.kernel_alias + "res_values pn," + rashod.paramcalc.kernel_alias + "_kernel.res_values pk " +
                                         " Where b.nzp_res = cnt3 and pn.nzp_res = cnt3 and pk.nzp_res = cnt3 " +
                                         " and b.nzp_y=pn.nzp_y and pn.nzp_y=pk.nzp_y and b.nzp_x=1 and pn.nzp_x=2 and pk.nzp_x=3 " +
                                         " and (b.value+0)=(case when cnt2 >=4 then 4 else cnt2 end) " +
                                         " and pn.value<=squ1 and pk.value>=squ1 " +
                                         " ) " + //кол-во комнат (cnt2 >=4) & площадь в диапазоне!

                                     "   and a.nzp_x = (case when cnt1 >=5 then 5+3 else cnt1+3 end) " + //кол-во людей
                                     " ),'0') " + sConvToNum +
                        " , val1_g = (" + sNvlWord.Trim() 
                        +"(( Select a.value From " + rashod.paramcalc.kernel_alias + "res_values a " +
                                     " Where a.nzp_res = cnt3 " +

                                     "   and a.nzp_y = (" +
                                         " Select b.nzp_y From " + rashod.paramcalc.kernel_alias + "res_values b," +
                                         rashod.paramcalc.kernel_alias + "res_values pn," + rashod.paramcalc.kernel_alias + "res_values pk " +
                                         " Where b.nzp_res = cnt3 and pn.nzp_res = cnt3 and pk.nzp_res = cnt3 " +
                                         " and b.nzp_y=pn.nzp_y and pn.nzp_y=pk.nzp_y and b.nzp_x=1 and pn.nzp_x=2 and pk.nzp_x=3 " +
                                         " and (b.value+0)=(case when cnt2 >=4 then 4 else cnt2 end) " +
                                         " and pn.value<=squ1 and pk.value>=squ1 " +
                                         " ) " + //кол-во комнат (cnt2 >=4) & площадь в диапазоне!

                                     "   and a.nzp_x = (case when cnt1_g >=5 then 5+3 else cnt1_g+3 end) " + //кол-во людей
                                     " ),'0') " + sConvToNum + ") " +
                                     " * " + sKolGil_g +
                        " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                        "   and nzp_serv in (25,210) " +
                        "   and cnt1_g > 0 and cnt2 > 0 " +
                        "   and cnt3 in ( Select nzp_res From " + rashod.paramcalc.kernel_alias + "res_values ) ";
            }
            else
            {
                #region применить спец.таблицу расходов ЭЭ в РТ - если стоит признак на базу (nzp_prm=163) и наличие ИПУ (nzp_prm=101)

                ExecSQL(conn_db, " Drop table ttt_aid_c1x ", false);

                ret = ExecSQL(conn_db,
                    " create temp table ttt_aid_c1x (nzp_kvar integer)" +
#if PG
                    " "
#else
                    " with no log "
#endif
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // 101|Наличие ЛС счетчика эл/эн|1||bool||1||||
                ret = ExecSQL(conn_db,
                      " Insert into ttt_aid_c1x (nzp_kvar)" +
                      " Select a.nzp_kvar " +
                      " From " + rashod.counters_xx + " a, ttt_prm_1 p " +
                      " Where a.nzp_kvar = p.nzp " +
                      "   and p.nzp_prm = 101 " +
                      "   and a.nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                      "   and a.nzp_serv in (25,210) " +

                      // 163|Отменить таблицу нормативов по эл/энергии Пост.№761|1||bool||5||||
                      "   and 0 < ( Select count(*) From " + paramcalc.data_alias + "prm_5 p5 " +
                           " where p5.nzp_prm=163 and p5.val_prm='1' and p5.is_actual<>100 " +
                           "   and p5.dat_s  <= " + rashod.paramcalc.dat_po + " and p5.dat_po >= " + rashod.paramcalc.dat_s +
                           "   ) " +

                      " Group by 1 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1x on ttt_aid_c1x (nzp_kvar) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_c1x ", true);

                sql =
                        " Update " + rashod.counters_xx +
                        " Set cnt3 = 13 " +
                        " Where stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                        "   and nzp_serv in (25,210) " +
                        "   and 0 < ( Select count(*) From ttt_aid_c1x k Where " + rashod.counters_xx +".nzp_kvar = k.nzp_kvar ) " +
                        " ";

                if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                }
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #endregion применить спец.таблицу расходов ЭЭ в РТ - если стоит признак на базу (nzp_prm=163) и наличие ИПУ (nzp_prm=101)

                sql =
                        " Update " + rashod.counters_xx +
                        " Set val1 = (" + sNvlWord +
                                   "(( Select value From " + rashod.paramcalc.kernel_alias + "res_values " +
                                     " Where nzp_res = cnt3 " +
                                     "   and nzp_y = (case when cnt3 = 13" + //кол-во людей
                                                    " then (case when cnt1 >11 then 11 else cnt1 end)" +
                                                    " else (case when cnt1 > 6 then  6 else cnt1 end)" +
                                                    " end) " +
                                     "   and nzp_x = (case when cnt3 = 13" + //кол-во комнат
                                                    " then 1" +
                                                    " else (case when cnt2 > 5 then  5 else cnt2 end)" +
                                                    " end) " +
                                     " ),'0')" + sConvToNum + ") " +
                                     " * (case when cnt3 = 13 then 1 else " + sKolGil +" end)" +
                        " , rash_norm_one = " + sNvlWord +
                                   "(( Select value From " + rashod.paramcalc.kernel_alias + "res_values " +
                                     " Where nzp_res = cnt3 " +
                                     "   and nzp_y = (case when cnt3 = 13" + //кол-во людей
                                                    " then (case when cnt1 >11 then 11 else cnt1 end)" +
                                                    " else (case when cnt1 > 6 then  6 else cnt1 end)" +
                                                    " end) " +
                                     "   and nzp_x = (case when cnt3 = 13" + //кол-во комнат
                                                    " then 1" +
                                                    " else (case when cnt2 > 5 then  5 else cnt2 end)" +
                                                    " end) " +
                                     " ),'0') " + sConvToNum +
                                     " / (case when cnt3 = 13 and " + sKolGil + " > 0 then " + sKolGil + " else 1 end)" +
                        " , val1_g = (" + sNvlWord +
                                   "(( Select value From " + rashod.paramcalc.kernel_alias + "res_values " +
                                     " Where nzp_res = cnt3 " +
                                     "   and nzp_y = (case when cnt3 = 13" + //кол-во людей
                                                    " then (case when cnt1_g >11 then 11 else cnt1_g end)" +
                                                    " else (case when cnt1_g > 6 then  6 else cnt1_g end)" +
                                                    " end) " +
                                     "   and nzp_x = (case when cnt3 = 13" + //кол-во комнат
                                                    " then 1" +
                                                    " else (case when cnt2 > 5 then  5 else cnt2 end)" +
                                                    " end) " +
                                     " ),'0')" + sConvToNum + ") " +
                                     " * (case when cnt3 = 13 then 1 else " + sKolGil_g + " end)" +
                        " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                        "   and nzp_serv in (25,210) " +
                        "   and cnt1_g > 0 and cnt2 > 0 " +
                        "   and cnt3 in ( Select nzp_res From " + rashod.paramcalc.kernel_alias + "res_values ) ";
                
                ExecSQL(conn_db, " Drop table ttt_aid_c1x ", false);
                ExecSQL(conn_db, " Drop table ttt_aid_c1xx ", false);
            }

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #region Поправки в данные  для коммуналок
            if (bIsSaha) { /* В сахе ничего не делаем */ }
            else
            {
                #region Количество комнат для общежитий проставить в t_ans_kommunal.kol_komn
                
                //2075|Применить норматив ЭлЭн для коммуналок в общежитии|||bool||2||||

                sql = " Update t_ans_kommunal " + 
                      " Set kol_komn =" + sNvlWord.Trim() +
                          "((select cnt2 from " + rashod.counters_xx + 
                          "  where stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                          "   and nzp_serv in (25,210) and t_ans_kommunal.nzp_kvar= " + rashod.counters_xx + ".nzp_kvar ),0) " +
                      " Where   is2075=1 ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                #endregion Количество комнат для общежитий проставить в t_ans_kommunal

                #region Тип из нормативов  cnt3 =дом не-МКД (по всем нормативным строкам лс и домам)
                sql = " Update t_ans_kommunal " + 
                      " Set cnt3  =" + sNvlWord.Trim() + 
                      "((select cnt3 from " + rashod.counters_xx + 
                         "  where stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                         "   and nzp_serv in (25,210) and t_ans_kommunal.nzp_kvar= " + rashod.counters_xx + ".nzp_kvar ),0) " +
                      " Where is2075=1 ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #endregion Тип из нормативов
                #region Определить общую норму и индивидуальную
                sql = " Update t_ans_kommunal " +
                       " Set rastot = " + sNvlWord.Trim() +
                       "(( Select value From " + rashod.paramcalc.kernel_alias + "res_values " +
                                    " Where nzp_res = cnt3 " +
                                    "   and nzp_y = (case when val3 > 6 then 6 else val3 end) " + //кол-во людей
                                    "   and nzp_x = (case when kol_komn > 5 then 5 else kol_komn end) " + //кол-во комнат  ? 
                                    " ),'0')" + sConvToNum +
                       " Where  cnt3 in ( Select nzp_res From " + rashod.paramcalc.kernel_alias + "res_values ) " +
                       " and is2075=1 ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                #endregion Определить общую норму и индивидуальную

                #region Подсчитать расход (временно убывших не учитываем, возможно потом)
                sql = " Update " + rashod.counters_xx +
                       " Set   val1   =" + sNvlWord.Trim() +
                       "((select  rastot*gil1 from t_ans_kommunal t where t.nzp_kvar=" + rashod.counters_xx + ".nzp_kvar and t.is2075=1),0)  " +
                       "     , rash_norm_one ="  + sNvlWord.Trim() +
                       "((select  rastot from t_ans_kommunal t where t.nzp_kvar=" + rashod.counters_xx + ".nzp_kvar and t.is2075=1),0)  " +
                       "     , val1_g =" + sNvlWord.Trim() +
                       "((select  rastot*gil1 from t_ans_kommunal t where t.nzp_kvar=" + rashod.counters_xx + ".nzp_kvar and t.is2075=1),0)  " +
                       "     , gil2   =" + sNvlWord.Trim() +
                       "((select  val3   from t_ans_kommunal t where t.nzp_kvar=" + rashod.counters_xx + ".nzp_kvar and t.is2075=1),0) " +
                       " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                       "   and nzp_serv in (25,210) " +
                       "   and cnt1_g > 0 and cnt2 > 0 " +
                       "   and 0< (select t.nzp_kvar from t_ans_kommunal t where t.nzp_kvar=" + rashod.counters_xx + ".nzp_kvar and is2075=1 ) ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                #endregion Подсчитать расход

                #region Сохранить в 29 стеке
                if (!b_calc_kvar)
                {
                    sql =
                        " Insert into " + rashod.counters_xx +
                        " ( stek,nzp_type,dat_charge,nzp_kvar,cur_zap,nzp_dom,nzp_counter,nzp_serv,mmnog,val1,val2,val3,val4," +
                        "   squ1,squ2,gil1,cnt1,kf307,kf307n,dat_s,dat_po, rashod, val_s , cnt5, cnt4 ) " +
                        " Select 29,3, " + p_dat_charge + ", 0,nzp_kvar,nzp_dom, 0, 0, nkvar," +
                        " val1,val2,val3,val4,squ1,squ2,gil1,cnt1,kf307,kf307n,"+MDY(1,1,1900)+", "+MDY(1,1,1900)+
                        ", rastot, rasind, kol_komn, is2075 " +
                        " From t_ans_kommunal Where 1=1 ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, "t_ans_kommunal", "nzp_no", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    UpdateStatistics(false, paramcalc, rashod.counters_tab, out ret);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                }
                #endregion Сохранить в 29 стеке
            }

            #endregion Поправки в данные  для коммуналок

            #endregion N: электроснабжение

            #region N: хвс гвс канализация
            //----------------------------------------------------------------
            //N: хвс гвс канализация
            //----------------------------------------------------------------
            if (Constants.Trace) Utility.ClassLog.WriteLog("N: хвс гвс канализация");
            ret = ExecRead(conn_db, out reader,
            " Select val_prm" + sConvToInt + " val_prm From " + rashod.paramcalc.data_alias + "prm_13 " +
            " Where nzp_prm = 172 and is_actual <> 100 and dat_s  <= " + rashod.paramcalc.dat_po + " and dat_po >= " + rashod.paramcalc.dat_s
            , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            iNzpRes = 0;
            if (reader.Read())
            {
                iNzpRes = Convert.ToInt32(reader["val_prm"]);
            }
            reader.Close();

            if (iNzpRes != 0)
            {
                sql =
                        " Update " + rashod.counters_xx +
                        " Set cnt3 = " + iNzpRes.ToString() + // 17
                        " Where nzp_type = 3 and nzp_type = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                        "   and nzp_serv in (6,7,324) ";

                if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                }
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }

            ret = ExecRead(conn_db, out reader,
            " Select val_prm" + sConvToInt + " val_prm From " + rashod.paramcalc.data_alias + "prm_13 " +
            " Where nzp_prm = 177 and is_actual <> 100 and dat_s  <= " + rashod.paramcalc.dat_po + " and dat_po >= " + rashod.paramcalc.dat_s
            , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            iNzpRes = 0;
            if (reader.Read())
            {
                iNzpRes = Convert.ToInt32(reader["val_prm"]);
            }
            reader.Close();

            if (iNzpRes != 0)
            {
                sql =
                        " Update " + rashod.counters_xx +
                        " Set cnt3 = " + iNzpRes.ToString() + // 17
                        " Where nzp_type = 3 and nzp_type = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                        "   and nzp_serv in (9,281,323) ";

                if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                }
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }

            string sdn = "";
            if (bIsSaha && 
                (Convert.ToDateTime("01." + rashod.paramcalc.cur_mm.ToString() + "." + rashod.paramcalc.cur_yy.ToString()) < Convert.ToDateTime("01.07.2013"))
                ) //&& ()) !!! before '01.07.2013' !!!
            {
                // вид дома по ГВС
                ExecSQL(conn_db, " Drop table t1189 ", false);
                ret = ExecSQL(conn_db,
                " create temp table t1189 (nzp_dom integer) " +
#if PG
                " "
#else
                " with no log "
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                sdn = " * gl7kw ";
                // для Сахи до '01.07.2013' умножим на количество дней:

                // для биллинга в Сахе кол-во дней пропорционально 365/12 - таблица s_ot_period не использовать для биллинга
                if (bIsCalcSubsidyBill)
                {
                    // !) круглогодичное оказание услуги. умножить на количество дней месяца = 365/12
                    sql =
                        " Update " + rashod.counters_xx + " Set " +
                        " gl7kw = 365/12 " +
                        " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                        "   and nzp_serv in (6,7,324, 9,281,323) " +
                        "   and cnt2 > 0 ";
                }
                else
                {
                    // выбрать дома с учетом количества дней по отопительному периоду - p.nzp_prm=p.1189 and p.val_prm+0>1
                    // p.val_prm = 1; - централизованное ГВС - круглый год
                    ret = ExecSQL(conn_db,
                        " insert into t1189 (nzp_dom) select k.nzp_dom From ttt_prm_1 p,t_selkvar k" +
                        " Where p.nzp_prm=1189 and p.val_prm" + sConvToInt + ">1 and p.nzp=k.nzp_kvar" +
                        " group by 1 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    ExecSQL(conn_db, " Create index ix_t1189 on t1189 (nzp_dom) ", true);
                    ExecSQL(conn_db, sUpdStat + " t1189 ", true);
                    // 1) оказание услуги в отопительный период. умножить на количество дней отопления в месяце - s_ot_period.mХХ = месяц и s_ot_period.nzp_town = дом
                    sql =
                        " Update " + rashod.counters_xx + " Set " +
                        " gl7kw = ( Select s.m" + rashod.paramcalc.cur_mm.ToString("00") +
                            " From " + rashod.paramcalc.kernel_alias + "s_ot_period s, " +
                            rashod.paramcalc.data_alias + "s_rajon r, " + rashod.paramcalc.data_alias + "s_ulica u, " + rashod.paramcalc.data_alias + "dom d " +
                            " where s.year_=" + rashod.paramcalc.cur_yy.ToString() + "" +
                            "   and s.nzp_town = r.nzp_town and r.nzp_raj=u.nzp_raj and u.nzp_ul=d.nzp_ul and d.nzp_dom=" + rashod.counters_xx + ".nzp_dom" +
                            " ) " +
                        " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                        "   and nzp_serv in (6,7,324, 9,281,323) " +
                        "   and cnt2 > 0 and 0<(select count(*) from t1189 where t1189.nzp_dom=" + rashod.counters_xx + ".nzp_dom)";
                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    // 2) круглогодичное оказание услуги. умножить на количество дней месяца - s_ot_period.mХХ = месяц и s_ot_period.nzp_town = 0
                    sql =
                        " Update " + rashod.counters_xx + " Set " +
                        " gl7kw = ( Select s.m" + rashod.paramcalc.cur_mm.ToString("00") +
                            " From " + rashod.paramcalc.kernel_alias + "s_ot_period s " +
                            " where s.year_=" + rashod.paramcalc.cur_yy.ToString() + "" +
                            "   and s.nzp_town = 0" +
                            " ) " +
                        " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                        "   and nzp_serv in (6,7,324, 9,281,323) " +
                        "   and cnt2 > 0 and 0=(select count(*) from t1189 where t1189.nzp_dom=" + rashod.counters_xx + ".nzp_dom)";
                }

                if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                }
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ExecSQL(conn_db, " Drop table t1189 ", false);
            }

            sql =
                " Update " + rashod.counters_xx + " Set " +
                " val1 = (( Select value From " + rashod.paramcalc.kernel_alias + "res_values " +
                    "  Where nzp_res = cnt3 " +
                    "   and nzp_y = cnt2 " + //тип водоснабжения
                    "   and nzp_x = (case when nzp_serv=6 then 2 else 4 end) " + // на нужды ГВ
                    " )" + sConvToNum + ") " + 
                    " * gil1" + sdn + ", " +
                " val4 = (( Select value From " + rashod.paramcalc.kernel_alias + "res_values " +
                    " Where nzp_res = cnt3 " +
                    "   and nzp_y = cnt2 " + //тип водоснабжения
                    "   and nzp_x = (case when nzp_serv=6 then 1 else 3 end) " + // на нужды ХВ
                    " )" + sConvToNum + ") " + 
                    " * gil1" + sdn + " " +
                ",rash_norm_one = (( Select value From " + rashod.paramcalc.kernel_alias + "res_values " +
                    " Where nzp_res = cnt3 " +
                    "   and nzp_y = cnt2 " + //тип водоснабжения
                    "   and nzp_x = (case when nzp_serv=6 then 1 else 3 end) " + // на нужды ХВ
                    " ) " + sConvToNum + ") " + 
                    sdn + " " +
                ",val1_g = (( Select value From " + rashod.paramcalc.kernel_alias + "res_values " +
                    "  Where nzp_res = cnt3 " +
                    "   and nzp_y = cnt2 " + //тип водоснабжения
                    "   and nzp_x = (case when nzp_serv=6 then 2 else 4 end) " + // на нужды ГВ
                    " )" + sConvToNum + ") " + 
                    " * gil1_g" + sdn +
                " + (( Select value From " + rashod.paramcalc.kernel_alias + "res_values " +
                    " Where nzp_res = cnt3 " +
                    "   and nzp_y = cnt2 " + //тип водоснабжения
                    "   and nzp_x = (case when nzp_serv=6 then 1 else 3 end) " + // на нужды ХВ
                    " )" + sConvToNum + ") " + 
                    " * gil1_g" + sdn + " " +
                " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and nzp_serv in (6,7,324) " +
                "   and cnt2 > 0 " +
                "   and cnt3 in ( Select nzp_res From " + rashod.paramcalc.kernel_alias + "res_values ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            sql =
                " Update " + rashod.counters_xx + " Set " +
                "  val1 = (case when nzp_serv=6 then 0 else val1 end) + val4 " + // нужды ХВ + нужды ГВ !!! для ХВ пока расход ГВ НЕ добавлять !!!
                " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and nzp_serv in (6,7,324) " +
                "   and cnt2 > 0 " +
                "   and cnt3 in ( Select nzp_res From " + rashod.paramcalc.kernel_alias + "res_values ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            sql =
                " Update " + rashod.counters_xx + " Set " +
                "  val1 = (( Select value From " + rashod.paramcalc.kernel_alias + "res_values " +
                    "  Where nzp_res = cnt3 " +
                    "   and nzp_y = cnt2 " + //тип водоснабжения
                    "   and nzp_x = 2 " +
                    " )" + sConvToNum + ") " +
                    " * gil1 " + sdn +
                ", rash_norm_one = (( Select value From " + rashod.paramcalc.kernel_alias + "res_values " +
                    "  Where nzp_res = cnt3 " +
                    "   and nzp_y = cnt2 " + //тип водоснабжения
                    "   and nzp_x = 2 " +
                    " ) " + sConvToNum + ") " +
                    sdn +
                ",val1_g = (( Select value From " + rashod.paramcalc.kernel_alias + "res_values " +
                    "  Where nzp_res = cnt3 " +
                    "   and nzp_y = cnt2 " + //тип водоснабжения
                    "   and nzp_x = 2 " +
                    " )" + sConvToNum + ") " +
                    " * gil1_g " + sdn +
                " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and nzp_serv in (9,281,323) " +
                "   and cnt2 > 0 " +
                "   and cnt3 in ( Select nzp_res From " + rashod.paramcalc.kernel_alias + "res_values ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            #endregion N: хвс гвс канализация

            #region N: отопление
            //----------------------------------------------------------------
            //N: отопление
            //----------------------------------------------------------------
            sql =
                    " Update " + rashod.counters_xx +
                    " Set cnt3 = 61" + // норма по площади
                    " Where nzp_type = 3 and stek = 3 and nzp_serv in (8,322,325) and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge;

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            string sKolGPl = "5";
            if (bIsSaha)
            {
                sKolGPl = "2";
            }

            // норма по площади на 1 человека (Кж=cnt1)
            sql =
                    " Update " + rashod.counters_xx +
                    " Set cnt2 = ( Select value From " + rashod.paramcalc.kernel_alias + "res_values " +
                                 " Where nzp_res = cnt3 " +
                                 "   and nzp_y = (case when cnt1 > " + sKolGPl + " then " + sKolGPl + " else cnt1 end) " + //кол-во людей
                                 "   and nzp_x = 2 " + //
                                 " )" + sConvToInt +
                    " Where nzp_type = 3 and stek = 3 and nzp_serv in (8,322,325) and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                    "   and cnt1 > 0 " +
                    "   and cnt3 in ( Select nzp_res From " + rashod.paramcalc.kernel_alias + "res_values ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            // ... расчет норматива на 1 кв.м ...
            ret = ExecRead(conn_db, out reader,
            " Select val_prm From " + rashod.paramcalc.data_alias + "prm_5 " +
            " Where nzp_prm = 478 and is_actual <> 100" +
            "   and dat_s  <= " + rashod.paramcalc.dat_po + " and dat_po >= " + rashod.paramcalc.dat_s + " "
            , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            bool bCalcNormOtopl = true;
            if (reader.Read())
            {
                bCalcNormOtopl = !(Convert.ToInt32(reader["val_prm"]) == 1);
            }
            reader.Close();

            ret = ExecSQL(conn_db,
                " create temp table t_norm_otopl ( " +
                //" create table are.t_norm_otopl ( " +
                " nzp_dom         integer, " +
                " nzp_kvar        integer, " +
                " s_otopl         " + sDecimalType + "( 8,2) default 0, " +    // отапливаемая площадь
                " rashod_gkal_m2f " + sDecimalType + "(12,8) default 0, " +    // расход в ГКал на 1 м2 фактический
                // вид учтенного расхода на л/с (1-норматив/2-ИПУ расход ГКал/3-дом.норма ГКал/4-ОДПУ расход ГКал)
                " vid_gkal_ls     integer default 0,       " +
                " rashod_gkal_dom " + sDecimalType + "(12,8) default 0, " +    // расход в ГКал на 1 м2
                " vid_gkal_dom    integer default 0,       " +    // вид учтенного домового расхода

                " rashod_gkal_m2  " + sDecimalType + "(12,8) default 0, " +    // нормативный расход в ГКал на 1 м2
                // для расчета норматива по отоплению
                " koef_god_pere   " + sDecimalType + "( 6,4) default 1, " +    // коэффициент перерасчета по итогам года
                " ugl_kv          integer default 0,       " +    // признак угловой квартиры (0-обычная/1-угловая)
                " vid_alg         integer default 1,       " +    // вид методики расчета норматива
                " tmpr_vnutr_vozd " + sDecimalType + "( 8,4) default 0, " +    // температура внутреннего воздуха (обычно = 20, для угловых = 22)
                " tmpr_vnesh_vozd " + sDecimalType + "( 8,4) default 0, " +    // средняя температура внешнего воздуха  (обычно = -5.7)
                " otopl_period     integer default 0,      " +    // продолжительность отопительного периода в сутках (обычно = 218)
                " nzp_res0         integer default 0,      " +    // таблица нормативов
                " dom_klimatz      integer default 0,      " +    // Климатическая зона
                // vid_alg=1 - памфиловская методика расчета норматива
                " dom_objem        " + sDecimalType + "(12,2) default 0, " +   // объем дома
                " dom_pol_pl       " + sDecimalType + "(12,2) default 1, " +   // полезная/отапливаемая площадь дома
                " dom_ud_otopl_har " + sDecimalType + "(12,8) default 1, " +   // удельная отопительная характеристика дома
                " dom_otopl_koef   " + sDecimalType + "( 8,4) default 1, " +   // поправочно-отопительный коэффициент для дома
                // vid_alg=2 - методика расчета норматива по Пост306 без интерполяции удельного расхода тепловой энергии
                // vid_alg=3 - методика расчета норматива по Пост306  с интерполяцией удельного расхода тепловой энергии
                // vid_alg=4 - методика расчета норматива - табличное значение от этажа и года постройки дома
                " dom_dat_postr    date default '1.1.1900', " +   // дата постройки дома
                " dom_kol_etag     integer default 0,       " +   // количество этажей дома (этажность)
                " pos_etag         integer default 0,       " +   // позиция по количеству этажей дома в таблице удельных расходов тепловой энергии
                " pos_narug_vozd   integer default 0,       " +   // позиция по температуре наружного воздуха в таблице удельных расходов тепловой энергии
                " dom_ud_tepl_en1  " + sDecimalType + "(12,8) default 0, " +   // минимальный  удельный расход тепловой энергии для дома по температуре и этажности
                " dom_ud_tepl_en2  " + sDecimalType + "(12,8) default 0, " +   // максимальный удельный расход тепловой энергии для дома по температуре и этажности
                " tmpr_narug_vozd1 " + sDecimalType + "( 8,4) default 0, " +   // минимально  близкая температура наружного воздуха в таблице
                " tmpr_narug_vozd2 " + sDecimalType + "( 8,4) default 0, " +   // максимально близкая температура наружного воздуха в таблице
                " tmpr_narug_vozd  " + sDecimalType + "( 8,4) default 0, " +   // температура наружного воздуха по проекту (паспорту) дома
                " dom_ud_tepl_en   " + sDecimalType + "(12,8) default 0  " +   // удельный расход тепловой энергии для дома по температуре и этажности
#if PG
                " ) "
#else
                //" ) "
                " ) With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            // === перечень л/с для расчета нормативов ===
            ret = ExecSQL(conn_db,
                  " insert into t_norm_otopl (nzp_dom,nzp_kvar,s_otopl)" +
                  " select nzp_dom,nzp_kvar,max(squ2) from " + rashod.counters_xx +
                  " where nzp_serv=8 and nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                  " group by 1,2 "
                  , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " create index ix1_norm_otopl on t_norm_otopl (nzp_kvar) ", true);
            ExecSQL(conn_db, " create index ix2_norm_otopl on t_norm_otopl (nzp_dom) ", true);
            ExecSQL(conn_db, sUpdStat + " t_norm_otopl ", true);

            // если разрешено рассчитывать норматив по отоплению
            if (bCalcNormOtopl)
            {
            #region N: отопление - расчет нормативов по типам

                // === параметры для всех алгоритмов расчета нормативов ===

                // нормативы от этажей (для РТ) и климатических зон (для сах РС-Я)
                ret = ExecRead(conn_db, out reader,
                " Select val_prm" + sConvToInt + " val_prm " +
                " From " + rashod.paramcalc.data_alias + "prm_13 " +
                " Where nzp_prm = 186 and is_actual <> 100 and dat_s  <= " + rashod.paramcalc.dat_po + " and dat_po >= " + rashod.paramcalc.dat_s
                , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                iNzpRes = 0;
                if (reader.Read())
                {
                    iNzpRes = Convert.ToInt32(reader["val_prm"]);
                }
                reader.Close();

                if (iNzpRes != 0)
                {
                    ret = ExecSQL(conn_db,
                          " Update t_norm_otopl " +
                          " Set nzp_res0 = " + iNzpRes.ToString() + " Where 1=1 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                }

                // количество этажей дома (этажность)
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set dom_kol_etag=" +
                      "    ( select max( replace(p.val_prm,',','.')" + sConvToInt + " ) from ttt_prm_2 p " +
                      "      where t_norm_otopl.nzp_dom=p.nzp " +
                                " and p.nzp_prm=37 " +
                      " )" +
                      " where 0<" +
                      "    ( select count(*) from ttt_prm_2 p " +
                      "      where t_norm_otopl.nzp_dom=p.nzp " +
                                " and p.nzp_prm=37 " +
                      " ) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                //ViewTbl(conn_db, " select * from t_norm_otopl where nzp_kvar=3829 ");

                // поиск указанного норматива в ГКал на 1 кв. метр. вид методики расчета норматива = 0
                if (bIsSaha)
                {
                    // позиция по количеству этажей дома в таблице расходов: 1-5
                    ret = ExecSQL(conn_db,
                          " update t_norm_otopl set pos_etag=" +
                          "    (case when dom_kol_etag<5 then (case when dom_kol_etag<=0 then 1 else dom_kol_etag end) else 5 end)" +
                          " where 1=1 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    // Климатическая зона
                    ret = ExecSQL(conn_db,
                          " update t_norm_otopl set dom_klimatz=" +
                          "    ( select max( replace(p.val_prm,',','.')" + sConvToInt + " ) from ttt_prm_2 p " +
                          "      where t_norm_otopl.nzp_dom=p.nzp " +
                                    " and p.nzp_prm=1180 " +
                          " )" +
                          " where 0<" +
                          "    ( select count(*) from ttt_prm_2 p " +
                          "      where t_norm_otopl.nzp_dom=p.nzp " +
                                    " and p.nzp_prm=1180 " +
                          " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // позиция по Климатическая зона дома в таблице расходов: 1-5
                    ret = ExecSQL(conn_db,
                          " update t_norm_otopl set dom_klimatz=" +
                          "    (case when dom_klimatz<5 then (case when dom_klimatz<=0 then 1 else dom_klimatz end) else 5 end)" +
                          " where 1=1 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    ret = ExecSQL(conn_db,
                        " Update t_norm_otopl Set " +
                        "  vid_alg=0, dom_ud_tepl_en = ( Select max(r3.value" + sConvToNum + ")" +
                        " From " + rashod.paramcalc.kernel_alias + "res_values r1, " + rashod.paramcalc.kernel_alias + "res_values r2, " + 
                            rashod.paramcalc.kernel_alias + "res_values r3 " +
                            "  Where r1.nzp_res = nzp_res0 and r2.nzp_res = nzp_res0 and r3.nzp_res = nzp_res0" +
                            "   and r1.nzp_x = 1 and r2.nzp_x = 2 and r3.nzp_x = 3 and r1.nzp_y=r2.nzp_y and r1.nzp_y=r3.nzp_y " +
                            "   and r1.value = dom_klimatz " +
                            "   and r2.value = pos_etag " +
                            " ) " +
                        " Where dom_klimatz > 0 and pos_etag>0 " +
                        "   and nzp_res0 in ( Select nzp_res From " + rashod.paramcalc.kernel_alias + "res_values ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    //////////////
                    // для биллинга в Сахе расход в ГКАл пропорционально 365/12 - таблицу s_ot_raspred не использовать для биллинга
                    if (bIsCalcSubsidyBill)
                    {
                        // !) круглогодичное пропорциональное оказание услуги = 365/12
                        ret = ExecSQL(conn_db,
                            " Update t_norm_otopl Set rashod_gkal_m2 = dom_ud_tepl_en Where 1=1 "
                            , true);
                        if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    }
                    else
                    {
                        // оказание услуги в отопительный период. умножить на процент распределения норматива отопления по месяцам
                        // - s_ot_raspred.mХХ = месяц и s_ot_raspred.nzp_town = дом
                        ret = ExecSQL(conn_db, 
                            " Update t_norm_otopl Set " +
                            " koef_god_pere = ( Select s.m" + rashod.paramcalc.cur_mm.ToString("00") +
                                " From " + rashod.paramcalc.kernel_alias + "s_ot_raspred s, " +
                                rashod.paramcalc.data_alias + "s_rajon r, " + rashod.paramcalc.data_alias + "s_ulica u, " + rashod.paramcalc.data_alias + "dom d " +
                                " where s.year_=" + rashod.paramcalc.cur_yy.ToString() + "" +
                                "   and s.nzp_town = r.nzp_town and r.nzp_raj=u.nzp_raj and u.nzp_ul=d.nzp_ul and d.nzp_dom=t_norm_otopl.nzp_dom" +
                                " ) " +
                            " Where 1=1 "
                            , true);
                        if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                        ret = ExecSQL(conn_db,
                            " Update t_norm_otopl Set rashod_gkal_m2 = dom_ud_tepl_en * 12 * koef_god_pere / 100 Where 1=1 "
                            , true);
                        if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    }
                    //////////////
                }
                else
                {
                    ret = ExecSQL(conn_db,
                        " create temp table t_otopl_m2 ( " +
                        //" create table are.t_otopl_m2 ( " +
                        " nzp_dom         integer, " +
                        " rashod_gkal_m2  " + sDecimalType + "(12,8) default 0 " +    // нормативный расход в ГКал на 1 м2
                        //" ) "
#if PG
                        " )  "
#else
                        " ) With no log "
#endif
                         , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    // === алгоритм расчет нормативов 0 ===
                    ret = ExecSQL(conn_db,
                          " Insert into t_otopl_m2 (nzp_dom,rashod_gkal_m2)" +
                          " Select p.nzp,max( replace(p.val_prm,',','.')" + sConvToNum + ") " +
                          " From ttt_prm_2 p " +
                          " Where p.nzp_prm=723 " +
                          "    and 0<(select count(*) from t_norm_otopl t where t.nzp_dom=p.nzp) " +
                          " group by 1 "
                          , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    // вид методики расчета норматива
                    ret = ExecSQL(conn_db,
                          " Update t_norm_otopl set vid_alg=" +
                          "    ( Select max( replace(p.val_prm,',','.') " + sConvToInt + ") " +
                               " From ttt_prm_2 p " +
                          "      Where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=709 " +
                          " )" +
                          " Where vid_alg<>0 and 0<" +
                          "    ( Select count(*) From ttt_prm_2 p" +
                          "      Where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=709 " +
                          " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // коэффициент перерасчета по итогам года
                    ret = ExecSQL(conn_db,
                          " update t_norm_otopl set koef_god_pere=" +
                          "    ( select max( replace(p.val_prm,',','.')" + sConvToNum + ") " +
                               " from " + rashod.paramcalc.data_alias + "prm_5 p" +
                          "      where p.is_actual<>100 and p.nzp_prm=108" +
                          "        and p.dat_s  <= " + rashod.paramcalc.dat_po +
                          "        and p.dat_po >= " + rashod.paramcalc.dat_s + " )" +
                          " where 0<" +
                          "    ( select count(*)" +
                               " from " + rashod.paramcalc.data_alias + "prm_5 p" +
                          "      where p.is_actual<>100 and p.nzp_prm=108" +
                          "        and p.dat_s  <= " + rashod.paramcalc.dat_po +
                          "        and p.dat_po >= " + rashod.paramcalc.dat_s +
                          " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    ExecSQL(conn_db, " create index ix1_otopl_m2 on t_otopl_m2 (nzp_dom) ", true);
                    ExecSQL(conn_db, sUpdStat + " t_otopl_m2 ", true);

                    ret = ExecSQL(conn_db,
                          " Update t_norm_otopl Set vid_alg=0,rashod_gkal_m2=( Select rashod_gkal_m2 From t_otopl_m2 p Where t_norm_otopl.nzp_dom=p.nzp_dom )" +
                          " Where 0<( Select count(*) From t_otopl_m2 p Where t_norm_otopl.nzp_dom=p.nzp_dom ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    //
                }

                // === алгоритмы расчета нормативов 1, 2, 3, 4 ===

                // признак угловой квартиры (0-обычная/1-угловая)
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set ugl_kv=1" +
                      " where 0<(select count(*) from ttt_prm_1 p where t_norm_otopl.nzp_kvar=p.nzp and p.nzp_prm=310) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                // vid_alg=1 ==================================

                // === параметры на всю БД (prm_5 - в 2.0 указаны в формуле!) ===
                // tmpr_vnutr_vozd -- температура внутреннего воздуха (обычно = 20, для угловых = 22)
                // tmpr_vnesh_vozd -- средняя температура внешнего воздуха  (обычно = -5.7)
                // otopl_period    -- продолжительность отопительного периода в сутках (обычно = 218)
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set" +
                      "  tmpr_vnutr_vozd = (case when ugl_kv=1 then 22 else 20 end)," +
                      "  tmpr_vnesh_vozd = -5.7," +
                      "  otopl_period    = 218" +
                      " where vid_alg=1 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // === домовые параметры (prm_2) ===
                // поправочно-отопительный коэффициент для дома
                ret = ExecSQL(conn_db,
                      " Update t_norm_otopl Set dom_otopl_koef=" +
                      "    ( Select max( replace(p.val_prm,',','.')" + sConvToNum + ") " +
                           " From ttt_prm_2 p Where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=33 " +
                      " )" +
                      " Where vid_alg=1 and 0<" +
                      "    ( Select count(*)" +
                           " From ttt_prm_2 p Where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=33 " +
                      " ) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // объем дома
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set dom_objem=" +
                      "    ( select max( replace(p.val_prm,',','.')" + sConvToNum + ") " +
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=32 " +
                      " )" +
                      " where vid_alg=1 and 0<" +
                      "    ( select count(*)" +
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=32 " +
                      " ) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // полезная/отапливаемая площадь дома
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set dom_pol_pl=" +
                      "    ( select max( replace(p.val_prm,',','.')" + sConvToNum + ") " +
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=36 " +
                      " )" +
                      " where vid_alg=1 and 0<" +
                      "    ( select count(*)" +
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=36 " +
                      " ) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // удельная отопительная характеристика дома
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set dom_ud_otopl_har=" +
                      "    ( select max( replace(p.val_prm,',','.')" + sConvToNum + ") " +
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=31 " +
                      " )" +
                      " where vid_alg=1 and 0<" +
                      "    ( select count(*)" +
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=31 " +
                      " ) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                // vid_alg=2 / 3 / 4 ==================================

                // === параметры на всю БД (prm_5) ===
                // температура внутреннего воздуха (обычно = 20, для угловых = 22)
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set tmpr_vnutr_vozd=" +
                      "    ( select max( replace(p.val_prm,',','.')" + sConvToNum + ") " +
                           " from " + rashod.paramcalc.data_alias + "prm_5 p" +
                      "      where p.is_actual<>100 and p.nzp_prm=54" +
                      "        and p.dat_s  <= " + rashod.paramcalc.dat_po +
                      "        and p.dat_po >= " + rashod.paramcalc.dat_s + " )" +
                      " where vid_alg in (2,3) and 0<" +
                      "    ( select count(*) from " + rashod.paramcalc.data_alias + "prm_5 p" +
                      "      where p.is_actual<>100 and p.nzp_prm=54" +
                      "        and p.dat_s  <= " + rashod.paramcalc.dat_po +
                      "        and p.dat_po >= " + rashod.paramcalc.dat_s + " ) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // для угловых
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set tmpr_vnutr_vozd=" +
                      "    ( select max( replace(p.val_prm,',','.')" + sConvToNum + ") " +
                           " from " + rashod.paramcalc.data_alias + "prm_5 p" +
                      "      where p.is_actual<>100 and p.nzp_prm=713" +
                      "        and p.dat_s  <= " + rashod.paramcalc.dat_po +
                      "        and p.dat_po >= " + rashod.paramcalc.dat_s + " )" +
                      " where vid_alg in (2,3) and ugl_kv=1 and 0<" +
                      "    ( select count(*) from " + rashod.paramcalc.data_alias + "prm_5 p" +
                      "      where p.is_actual<>100 and p.nzp_prm=713" +
                      "        and p.dat_s  <= " + rashod.paramcalc.dat_po +
                      "        and p.dat_po >= " + rashod.paramcalc.dat_s + " ) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // средняя температура внешнего воздуха  (обычно = -5.7)
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set tmpr_vnesh_vozd=" +
                      "    ( select max( replace(p.val_prm,',','.')" + sConvToNum + ") " +
                           " from " + rashod.paramcalc.data_alias + "prm_5 p" +
                      "      where p.is_actual<>100 and p.nzp_prm=710" +
                      "        and p.dat_s  <= " + rashod.paramcalc.dat_po +
                      "        and p.dat_po >= " + rashod.paramcalc.dat_s + " )" +
                      " where vid_alg in (2,3) and 0<" +
                      "    ( select count(*) from " + rashod.paramcalc.data_alias + "prm_5 p" +
                      "      where p.is_actual<>100 and p.nzp_prm=710" +
                      "        and p.dat_s  <= " + rashod.paramcalc.dat_po +
                      "        and p.dat_po >= " + rashod.paramcalc.dat_s + " ) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // продолжительность отопительного периода в сутках (обычно = 218)
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set otopl_period=" +
                      "    ( select max( replace(p.val_prm,',','.')" + sConvToNum + ") " +
                           " from " + rashod.paramcalc.data_alias + "prm_5 p" +
                      "      where p.is_actual<>100 and p.nzp_prm=712" +
                      "        and p.dat_s  <= " + rashod.paramcalc.dat_po +
                      "        and p.dat_po >= " + rashod.paramcalc.dat_s + " )" +
                      " where vid_alg in (2,3) and 0<" +
                      "    ( select count(*) from " + rashod.paramcalc.data_alias + "prm_5 p" +
                      "      where p.is_actual<>100 and p.nzp_prm=712" +
                      "        and p.dat_s  <= " + rashod.paramcalc.dat_po +
                      "        and p.dat_po >= " + rashod.paramcalc.dat_s + " ) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // === домовые параметры (prm_2) ===
                // дата постройки дома
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set dom_dat_postr=" +
                      "    ( select max( replace(p.val_prm,',','.')" + sConvToDate + ") " + 
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=150 " +
                      " )" +
                      " where vid_alg in (2,3,4) and 0<" +
                      "    ( select count(*)" +
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=150 " +
                      " ) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // количество этажей дома (этажность)
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set dom_kol_etag=" +
                      "    ( select max( replace(p.val_prm,',','.')" + sConvToInt + ") " +
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=37 " +
                      " )" +
                      " where vid_alg in (2,3,4) and 0<" +
                      "    ( select count(*)" +
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=37 " +
                      " ) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // температура наружного воздуха по проекту (паспорту) дома
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set tmpr_narug_vozd=" +
                      "    ( select max( replace(p.val_prm,',','.')" + sConvToNum + ") " +
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=711 " +
                      " )" +
                      " where vid_alg in (2,3) and 0<" +
                      "    ( select count(*)" +
                           " from ttt_prm_2 p where t_norm_otopl.nzp_dom=p.nzp and p.nzp_prm=711 " +
                      " ) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                // таблица диапазонов этажей для таблицы удельных расходов тепловой энергии (Пост.306) до 1999 года
                    /*
                      " select " +
                      " r.nzp_y y1,r.value val1" +
                      ",(case when strpos( r.value,'-')=0" +
                      "       then (r.value::int) else substring(r.value,1,strpos( r.value,'-')-1)::int" +
                      "  end) etag1 " +
                      ",coalesce((case when strpos(b.value,'-')=0" +
                      "       then (b.value::int) else substr(b.value,1,strpos( b.value,'-')-1)::int" +
                      "       end),9999) etag2 " +
                       " into temp t_etag1999  " +
                      " from " + rashod.paramcalc.pref + "_kernel.res_values r "+
                      " left outer join " + rashod.paramcalc.pref + "_kernel.res_values b on r.nzp_y=b.nzp_y-1 and b.nzp_res=9996 and b.nzp_x=1 " +
                      " where r.nzp_res=9996 and r.nzp_x=1 "  
                      */
                string ssql = " create temp table t_etag1999(y1 int,val1 char(20),etag1 integer,etag2 integer) " +
#if PG
                      " ";
#else
                      " with no log ";
#endif
                ret = ExecSQL(conn_db, ssql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

#if PG
                string sposfun = "strpos";
#else
                string sposfun = rashod.paramcalc.data_alias + "pos";
#endif

                ssql =
                    " insert into t_etag1999(y1,val1,etag1,etag2) " +
                    " select " +
                    " r.nzp_y y1,r.value val1" +
                    ",(case when " + sposfun + "(r.value,'-')=0" +
                    "       then (r.value" + sConvToInt + ") else substr(r.value,1," +
                    sposfun + "(r.value,'-')-1)" + sConvToInt +
                    "  end) etag1 " +
                    "," + sNvlWord + "((case when " + sposfun + "(b.value,'-')=0" +
                    "       then (b.value" + sConvToInt + ") else substr(b.value,1," +
                    sposfun + "(b.value,'-')-1)" + sConvToInt +
                    "  end),9999) etag2 " +
                    " from " + rashod.paramcalc.kernel_alias + "res_values r " +
#if PG
                    " left outer join " + rashod.paramcalc.kernel_alias + "res_values b " +
                    " on r.nzp_y=b.nzp_y-1 and b.nzp_res=9996 and b.nzp_x=1 where 1=1" +
#else
                    ",outer " + rashod.paramcalc.kernel_alias + "res_values b " +
                    " where b.nzp_res=9996 and b.nzp_x=1 and r.nzp_y=b.nzp_y-1 " +
#endif
                    " and r.nzp_res=9996 and r.nzp_x=1  ";
                ret = ExecSQL(conn_db, ssql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " create index ix_etag1999 on t_etag1999 (etag1,etag2) ", true);
                ExecSQL(conn_db, sUpdStat + " t_etag1999 ", true);

                // таблица диапазонов этажей для таблицы удельных расходов тепловой энергии (Пост.306) после 1999 года
                ssql = " create temp table t_etag(y1 int,val1 char(20),etag1 integer,etag2 integer) " +
#if PG
                      " ";
#else
                      " with no log ";
#endif
                ret = ExecSQL(conn_db, ssql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ssql =
                    " insert into t_etag(y1,val1,etag1,etag2) " +
                    " select " +
                    " r.nzp_y y1,r.value val1" +
                    ",(case when " + sposfun + "(r.value,'-')=0" +
                    "       then (r.value" + sConvToInt + ") else substr(r.value,1," +
                    sposfun + "(r.value,'-')-1)" + sConvToInt +
                    "  end) etag1 " +
                    "," + sNvlWord + "((case when " + sposfun + "(b.value,'-')=0" +
                    "       then (b.value" + sConvToInt + ") else substr(b.value,1," +
                    sposfun + "(b.value,'-')-1)" + sConvToInt +
                    "       end),9999) etag2 " +
                    " from " + rashod.paramcalc.kernel_alias + "res_values r " +
#if PG
                    " left outer join " + rashod.paramcalc.kernel_alias + "res_values b"+
                    " on r.nzp_y=b.nzp_y-1 and b.nzp_res=9997 and b.nzp_x=1 where 1=1 " +
#else
                    ",outer " + rashod.paramcalc.kernel_alias + "res_values b " +
                    " where b.nzp_res=9997 and b.nzp_x=1 and r.nzp_y=b.nzp_y-1" + 
#endif
                    " and r.nzp_res=9997 and r.nzp_x=1 ";
                ret = ExecSQL(conn_db, ssql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " create index ix_etag on t_etag (etag1,etag2) ", true);
                ExecSQL(conn_db, sUpdStat + " t_etag ", true);

                // таблица диапазонов температур для таблицы удельных расходов тепловой энергии (Пост.306)
                ret = ExecSQL(conn_db,
#if PG
                      " select " +
                      " r.nzp_y y1,r.value val1" +
                      ",(case when strpos(r.value,'-')=0" +
                      "       then (r.value::int) else substring(r.value,1,strpos(r.value,'-')-1)::int" +
                      "  end) tmpr1 " +
                      ",coalesce((case when strpos(b.value,'-')=0" +
                      "       then (b.value::int) else substring(b.value,1,strpos(b.value,'-')-1)::int" +
                      "       end),9999) tmpr2 " +
                      " into temp t_tmpr  "+
                      " from " + rashod.paramcalc.pref + "_kernel.res_values r "+
                      " left outer join " + rashod.paramcalc.pref + "_kernel.res_values b on r.nzp_y=b.nzp_y-1 and b.nzp_res=9991 and b.nzp_x=1 " +
                      " where r.nzp_res=9991 and r.nzp_x=1 "
                     
#else
                      " select " +
                      " r.nzp_y y1,r.value val1" +
                      ",(case when " + rashod.paramcalc.pref + "_data:pos(r.value,'-')=0" +
                      "       then (r.value+0) else substr(r.value,1," + rashod.paramcalc.pref + "_data:pos(r.value,'-')-1)+0" +
                      "  end) tmpr1 " +
                      ",nvl((case when " + rashod.paramcalc.pref + "_data:pos(b.value,'-')=0" +
                      "       then (b.value+0) else substr(b.value,1," + rashod.paramcalc.pref + "_data:pos(b.value,'-')-1)+0" +
                      "       end),9999) tmpr2 " +
                      " from " + rashod.paramcalc.pref + "_kernel:res_values r,outer " + rashod.paramcalc.pref + "_kernel:res_values b " +
                      " where r.nzp_res=9991 and r.nzp_x=1 and b.nzp_res=9991 and b.nzp_x=1 and r.nzp_y=b.nzp_y-1" +
                      " into temp t_tmpr with no log "
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                ExecSQL(conn_db, " create index ix_tmpr on t_tmpr (tmpr1,tmpr2) ", true);
                ExecSQL(conn_db, sUpdStat + " t_tmpr ", true);

                // позиция по количеству этажей дома в таблице удельных расходов тепловой энергии до 1999
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set pos_etag=" +
                      "    (select max(b.y1) from t_etag1999 b where t_norm_otopl.dom_kol_etag>=b.etag1 and t_norm_otopl.dom_kol_etag<b.etag2)" +
                      " where vid_alg in (2,3) and dom_dat_postr<=" + MDY(1,1,1999)
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // позиция по количеству этажей дома в таблице удельных расходов тепловой энергии после 1999
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set pos_etag=" +
                      "    (select max(b.y1) from t_etag b where t_norm_otopl.dom_kol_etag>=b.etag1 and t_norm_otopl.dom_kol_etag<b.etag2)" +
                      " where vid_alg in (2,3) and dom_dat_postr>" + MDY(1,1,1999)
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // позиция по количеству этажей дома в таблице нормативных расходов тепловой энергии с 09.2012г в РТ 
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set pos_etag=" +
                      "    (case when dom_kol_etag>16 then 16 else dom_kol_etag end)" +
                      " where vid_alg=4 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                //ViewTbl(conn_db, " select * from t_norm_otopl where nzp_kvar=3829 ");

                // pos_narug_vozd   - позиция по температуре наружного воздуха в таблице удельных расходов тепловой энергии
                // tmpr_narug_vozd1 - минимально  близкая температура наружного воздуха в таблице
                // tmpr_narug_vozd2 - максимально близкая температура наружного воздуха в таблице
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set (pos_narug_vozd,tmpr_narug_vozd1,tmpr_narug_vozd2)=" +
#if PG
                      "    ((select max(b.y1) from t_tmpr b " +
                      "      where abs(t_norm_otopl.tmpr_narug_vozd)>=b.tmpr1 and abs(t_norm_otopl.tmpr_narug_vozd)<b.tmpr2),"+
                            "(select max(abs(b.tmpr1)) from t_tmpr b " +
                      "      where abs(t_norm_otopl.tmpr_narug_vozd)>=b.tmpr1 and abs(t_norm_otopl.tmpr_narug_vozd)<b.tmpr2),"+
                            "(select max(abs(b.tmpr2)) from t_tmpr b " +
                      "      where abs(t_norm_otopl.tmpr_narug_vozd)>=b.tmpr1 and abs(t_norm_otopl.tmpr_narug_vozd)<b.tmpr2))" +
#else
                      "    ((select max(b.y1),max(abs(b.tmpr1)),max(abs(b.tmpr2)) from t_tmpr b " +
                      "      where abs(t_norm_otopl.tmpr_narug_vozd)>=b.tmpr1 and abs(t_norm_otopl.tmpr_narug_vozd)<b.tmpr2))" +
#endif
                      " where vid_alg in (2,3) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // минимальный  удельный расход тепловой энергии для дома по температуре и этажности
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set dom_ud_tepl_en1=" +
                      "    (select max(replace(r.value,',','.')" + sConvToNum + ") " +
                      " from " + rashod.paramcalc.kernel_alias + "res_values r" +
                      "     where r.nzp_res=9996 and t_norm_otopl.pos_etag=r.nzp_y and t_norm_otopl.pos_narug_vozd=r.nzp_x)" +
                      " where vid_alg in (2,3) and dom_dat_postr<=" + MDY(1,1,1999)
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set dom_ud_tepl_en1=" +
                      "    (select max(replace(r.value,',','.')" + sConvToNum + ") " +
                      " from " + rashod.paramcalc.kernel_alias + "res_values r" +
                      "     where r.nzp_res=9997 and t_norm_otopl.pos_etag=r.nzp_y and t_norm_otopl.pos_narug_vozd=r.nzp_x)" +
                      " where vid_alg in (2,3) and dom_dat_postr> " + MDY(1,1,1999)
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // максимальный удельный расход тепловой энергии для дома по температуре и этажности
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set dom_ud_tepl_en2=" +
                      "    (select max(replace(r.value,',','.')" + sConvToNum + ") " +
                      " from " + rashod.paramcalc.kernel_alias + "res_values r" +
                      "     where r.nzp_res=9996 and t_norm_otopl.pos_etag=r.nzp_y and t_norm_otopl.pos_narug_vozd+1=r.nzp_x)" +
                      " where vid_alg in (2,3) and dom_dat_postr<=" + MDY(1,1,1999)
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                ret = ExecSQL(conn_db,
                      " Update t_norm_otopl set dom_ud_tepl_en2=" +
                      "    (select max(replace(r.value,',','.')" + sConvToNum + ") " +
                      " From " + rashod.paramcalc.kernel_alias + "res_values r" +
                      "     where r.nzp_res=9997 and t_norm_otopl.pos_etag=r.nzp_y and t_norm_otopl.pos_narug_vozd+1=r.nzp_x)" +
                      " Where vid_alg in (2,3) and dom_dat_postr> " + MDY(1,1,1999)
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // dom_ud_tepl_en - удельный расход тепловой энергии для дома по температуре и этажности
                // без интерполяции
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set dom_ud_tepl_en=dom_ud_tepl_en1" +
                      " where vid_alg=2 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // с интерполяцией
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set dom_ud_tepl_en=dom_ud_tepl_en1+" +
                      "   (dom_ud_tepl_en2-dom_ud_tepl_en1)*(abs(tmpr_narug_vozd)-tmpr_narug_vozd1)/(tmpr_narug_vozd2-tmpr_narug_vozd1)" +
                      " where vid_alg=3 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                // === расчет нормативов в ГКал на 1 кв.м - vid_alg=1 ===
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set rashod_gkal_m2 = 0.98 * dom_ud_otopl_har * dom_objem / dom_pol_pl where vid_alg=1 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // === расчет нормативов в ГКал на 1 кв.м - vid_alg in (2,3) ===
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set rashod_gkal_m2 = dom_ud_tepl_en / (tmpr_vnutr_vozd - tmpr_narug_vozd) where vid_alg in (2,3) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // === расчет нормативов в ГКал на 1 кв.м - общее для vid_alg in (1,2,3) ===
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set rashod_gkal_m2 = rashod_gkal_m2 * (tmpr_vnutr_vozd - tmpr_vnesh_vozd) * otopl_period * 24 * 0.000001 / 12 * koef_god_pere where vid_alg in (1,2,3) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // === установить норматив в ГКал на 1 кв.м - из таблицы для vid_alg=4 ===
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set rashod_gkal_m2=" +
                      "    (select max(replace(r.value,',','.')" + sConvToNum + ") " +
                      " from " + rashod.paramcalc.kernel_alias + "res_values r" +
                      "     where r.nzp_res=t_norm_otopl.nzp_res0 and t_norm_otopl.pos_etag=r.nzp_y"+
                      " and r.nzp_x=(case when dom_dat_postr<=" + MDY(1, 1, 1999) + " then 1 else 2 end) )" +
                      " where vid_alg=4 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

            #endregion N: отопление - расчет нормативов по типам
            }
            else
            {
                ret = ExecSQL(conn_db,
                      " update t_norm_otopl set vid_alg=0,rashod_gkal_m2=0 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }
            //ViewTbl(conn_db, " select * from t_norm_otopl where nzp_kvar=3829 ");

            // ===  установка норматива по отоплению в counters_xx ===
            sql =
                    " Update " + rashod.counters_xx +
                    " Set (val1,val3,kod_info) = "+
#if PG
                    "((Select rashod_gkal_m2 * s_otopl From t_norm_otopl k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar),"+
                    "(Select rashod_gkal_m2 From t_norm_otopl k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar),"+
                    "(Select vid_alg From t_norm_otopl k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar)) " +
#else
                    "((Select rashod_gkal_m2 * s_otopl,rashod_gkal_m2,vid_alg From t_norm_otopl k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar)) " +
#endif
                    " Where nzp_type = 3 and stek = 3 and nzp_serv=8 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                    "   and 0 < ( Select count(*) From t_norm_otopl k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar and k.vid_alg in (0,1,2,3,4) ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            #endregion N: отопление

            #region N: электроотопление
            //----------------------------------------------------------------
            //N: электроотопление
            //----------------------------------------------------------------
            ret = ExecSQL(conn_db,
                " create temp table t_norm_eeot ( " +
                //" create table are.t_norm_otopl ( " +
                " nzp_dom       integer, " +
                " nzp_kvar      integer, " +
                " s_otopl       " + sDecimalType + "( 8,2) default 0, " +    // отапливаемая площадь
                " rashod_kvt_m2 " + sDecimalType + "(12,8) default 0, " +    // нормативный расход в квт*час на 1 м2
                // для расчета норматива по отоплению
                " nzp_res0      integer default 0,      " +    // таблица нормативов
                " dom_klimatz   integer default 0,      " +    // Климатическая зона
                " dom_kol_etag  integer default 0,       " +   // количество этажей дома (этажность)
                " pos_etag      integer default 0        " +   // позиция по количеству этажей дома в таблице удельных расходов тепловой энергии
#if PG
                " ) "
#else
                //" ) "
                " ) With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            // === перечень л/с для расчета нормативов ===
            ret = ExecSQL(conn_db,
                  " insert into t_norm_eeot (nzp_dom,nzp_kvar,s_otopl)" +
                  " select nzp_dom,nzp_kvar,max(squ2) from " + rashod.counters_xx +
                  " where nzp_serv=322 and nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                  " group by 1,2 "
                  , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " create index ix1_norm_eeot on t_norm_eeot (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " t_norm_eeot ", true);

            // поиск указанного норматива в Квт*час на 1 кв. метр. вид методики расчета норматива = 0

            if (bIsSaha)
            {
                // нормативы для сах РС(Я) от этажей и климатических зон
                ret = ExecRead(conn_db, out reader,
                " Select val_prm" + sConvToInt + " val_prm" +
                " From " + rashod.paramcalc.data_alias + "prm_13 " +
                " Where nzp_prm = 183 and is_actual <> 100 and dat_s  <= " + rashod.paramcalc.dat_po + " and dat_po >= " + rashod.paramcalc.dat_s
                , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                iNzpRes = 0;
                if (reader.Read())
                {
                    iNzpRes = Convert.ToInt32(reader["val_prm"]);
                }
                reader.Close();

                if (iNzpRes != 0)
                {
                    ret = ExecSQL(conn_db,
                          " Update t_norm_eeot " +
                          " Set nzp_res0 = " + iNzpRes.ToString() + " Where 1=1 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                }
                // количество этажей дома (этажность)
                ret = ExecSQL(conn_db,
                      " update t_norm_eeot set dom_kol_etag=" +
                      "    ( select max( replace(p.val_prm,',','.') ) from ttt_prm_2 p " +
                      "      where t_norm_eeot.nzp_dom=p.nzp " +
                                " and p.nzp_prm=37 " +
                      " )" +
                      " where 0<" +
                      "    ( select count(*) from ttt_prm_2 p " +
                      "      where t_norm_eeot.nzp_dom=p.nzp " +
                                " and p.nzp_prm=37 " +
                      " ) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // позиция по количеству этажей дома в таблице расходов: 1-5
                ret = ExecSQL(conn_db,
                      " update t_norm_eeot set pos_etag=" +
                      "    (case when dom_kol_etag<5 then (case when dom_kol_etag<=0 then 1 else dom_kol_etag end) else 5 end)" +
                      " where 1=1 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                // Климатическая зона
                ret = ExecSQL(conn_db,
                      " update t_norm_eeot set dom_klimatz=" +
                      "    ( select max( replace(p.val_prm,',','.') ) from ttt_prm_2 p " +
                      "      where t_norm_eeot.nzp_dom=p.nzp " +
                                " and p.nzp_prm=1180 " +
                      " )" +
                      " where 0<" +
                      "    ( select count(*) from ttt_prm_2 p " +
                      "      where t_norm_eeot.nzp_dom=p.nzp " +
                                " and p.nzp_prm=1180 " +
                      " ) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // позиция по Климатическая зона дома в таблице расходов: 1-5
                ret = ExecSQL(conn_db,
                      " update t_norm_eeot set dom_klimatz=" +
                      "    (case when dom_klimatz<5 then (case when dom_klimatz<=0 then 1 else dom_klimatz end) else 5 end)" +
                      " where 1=1 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ret = ExecSQL(conn_db,
                    " Update t_norm_eeot Set " +
                    "  rashod_kvt_m2 = ( Select max(r3.value)" +
                        " From " + rashod.paramcalc.kernel_alias + "res_values r1, " + 
                          rashod.paramcalc.kernel_alias + "res_values r2, " + rashod.paramcalc.kernel_alias + "res_values r3 " +
                        "  Where r1.nzp_res = nzp_res0 and r2.nzp_res = nzp_res0 and r3.nzp_res = nzp_res0" +
                        "   and r1.nzp_x = 1 and r2.nzp_x = 2 and r3.nzp_x = 3 and r1.nzp_y=r2.nzp_y and r1.nzp_y=r3.nzp_y " +
                        "   and r1.value = dom_klimatz " +
                        "   and r2.value = pos_etag " +
                        " ) " +
                    " Where dom_klimatz > 0 and pos_etag>0 " +
                    "   and nzp_res0 in ( Select nzp_res From " + rashod.paramcalc.kernel_alias + "res_values ) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }
            // ===  установка норматива по электроотоплению в counters_xx ===
            sql =
                    " Update " + rashod.counters_xx +
                    " Set (val1,val3) = "+
#if PG
                    "((Select rashod_kvt_m2 * s_otopl From t_norm_eeot k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar),"+
                    "(Select rashod_kvt_m2 From t_norm_eeot k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar)) " +
#else
                    "((Select rashod_kvt_m2 * s_otopl,rashod_kvt_m2 From t_norm_eeot k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar)) " +
#endif
                    " Where nzp_type = 3 and stek = 3 and nzp_serv=322 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                    "   and 0 < ( Select count(*) From t_norm_eeot k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            #endregion N: электроотопление

            #region N: газовое отопление
            //----------------------------------------------------------------
            //N: газовое отопление
            //----------------------------------------------------------------
            ret = ExecSQL(conn_db,
                " create temp table t_norm_gasot ( " +
                //" create table are.t_norm_gasot ( " +
                " nzp_dom       integer, " +
                " nzp_kvar      integer, " +
                " s_otopl       decimal( 8,2) default 0, " +    // отапливаемая площадь
                " rashod_kbm_m2 " + sDecimalType + "(12,8) default 0  " +    // нормативный расход в куб.м газа на 1 м2
#if PG
                " )  "
#else
                //" ) "
                " ) With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            // === перечень л/с для расчета нормативов ===
            ret = ExecSQL(conn_db,
                  " insert into t_norm_gasot (nzp_dom,nzp_kvar,s_otopl)" +
                  " select nzp_dom,nzp_kvar,max(squ2) from " + rashod.counters_xx +
                  " where nzp_serv=325 and nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                  " group by 1,2 "
                  , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " create index ix1_norm_gasot on t_norm_gasot (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " t_norm_gasot ", true);

            ret = ExecSQL(conn_db,
                  " update t_norm_gasot set rashod_kbm_m2=" +
                  " (Select max(val_prm" + sConvToInt + ")" +
                   " From " + rashod.paramcalc.data_alias + "prm_5 " +
                   " Where nzp_prm = 169 and is_actual <> 100" +
                   " and dat_s  <= " + rashod.paramcalc.dat_po + " and dat_po >= " + rashod.paramcalc.dat_s +" ) "
                  , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            ret = ExecSQL(conn_db,
                  " update t_norm_gasot set rashod_kbm_m2=0 Where rashod_kbm_m2 is null "
                  , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            // ===  установка норматива по газовому отоплению в counters_xx ===
            sql =
                " Update " + rashod.counters_xx +
                " Set (val1,val3) = "+
#if PG
                "((Select rashod_kbm_m2 * s_otopl From t_norm_gasot k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar),"+
                "(Select rashod_kbm_m2 From t_norm_gasot k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar)) " +
#else
                "((Select rashod_kbm_m2 * s_otopl,rashod_kbm_m2 From t_norm_gasot k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar)) " +
#endif
                " Where nzp_type = 3 and stek = 3 and nzp_serv=325 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and 0 < ( Select count(*) From t_norm_gasot k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            #endregion N: газовое отопление

            #region N: газ -
            //----------------------------------------------------------------
            //N: газ - 
            //----------------------------------------------------------------
            if (Constants.Trace) Utility.ClassLog.WriteLog("N: газ - ");
            ret = ExecSQL(conn_db,
                " create temp table t_norm_gas ( " +
                //" create table are.t_norm_gas ( " +
                " nzp_dom    integer, " +
                " nzp_kvar   integer, " +
                " gil1       " + sDecimalType + "(14,7) default 0, " +    // количество жильцов
                " gil1_g     " + sDecimalType + "(14,7) default 0, " +    // количество жильцов без учета вр.выбывших
                " rashod_kbm " + sDecimalType + "(12,8) default 0, " +    // нормативный расход в куб.м газа на 1 человека
                " nzp_res0   integer default 0,       " +    // таблица нормативов
                " is_gp      integer default 0,      " +   // Климатическая зона
                " is_gvs     integer default 0,      " +   // количество этажей дома (этажность)
                " is_gk      integer default 0       " +   // позиция по количеству этажей дома в таблице удельных расходов тепловой энергии
#if PG
                " ) "
#else
                //" ) "
                " ) With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            // === перечень л/с для расчета нормативов ===
            ret = ExecSQL(conn_db,
                  " insert into t_norm_gas (nzp_dom,nzp_kvar,gil1,gil1_g)" +
                  " select nzp_dom,nzp_kvar,max(gil1),max(gil1_g) from " + rashod.counters_xx +
                  " where nzp_serv=10 and nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                  " group by 1,2 "
                  , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " create index ix1_norm_gas on t_norm_gas (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " t_norm_gas ", true);

            // поиск указанного норматива в куб.м на 1 человека

            // нормативы
            ret = ExecRead(conn_db, out reader,
            " Select val_prm" + sConvToInt + " val_prm" +
            " From " + rashod.paramcalc.data_alias + "prm_13 " +
            " Where nzp_prm = 173 and is_actual <> 100 and dat_s  <= " + rashod.paramcalc.dat_po + " and dat_po >= " + rashod.paramcalc.dat_s
            , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            iNzpRes = 0;
            if (reader.Read())
            {
                iNzpRes = Convert.ToInt32(reader["val_prm"]);
            }
            reader.Close();

            if (iNzpRes != 0)
            {
                ret = ExecSQL(conn_db,
                      " Update t_norm_gas " +
                      " Set nzp_res0 = " + iNzpRes.ToString() + " Where 1=1 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }
            // наличие газовой плиты
            ret = ExecSQL(conn_db,
                  " update t_norm_gas set is_gp=1" +
                  " where 0<( select count(*) from ttt_prm_1 p where t_norm_gas.nzp_kvar=p.nzp and p.nzp_prm=551 ) "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            // наличие газовой колонки (водонагревателя)
            ret = ExecSQL(conn_db,
                  " update t_norm_gas set is_gk=1" +
                  " where 0<( select count(*) from ttt_prm_1 p where t_norm_gas.nzp_kvar=p.nzp and p.nzp_prm=1 ) "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            // наличие ГВС
            sql =
                " update t_norm_gas set is_gvs=1" +
                " where 0<( select count(*) from ttt_prm_1 p where t_norm_gas.nzp_kvar=p.nzp and p.nzp_prm=7 " +
                " and p.val_prm" + sConvToInt + " in (";

            if (bIsSaha) 
            {
                //  8, 9,10,11,12,13,14,15,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37 - after 01.07.2013!!!
                sql = sql.Trim() + "01,02,03,04,05,12,13,14,15,16,17,18,19,20,24,25,26,27,28,30,31,32,36,37,38,39";
            }
            else if (Points.IsSmr)
            {
                sql = sql.Trim() + "10";
            }
            else
            {
                sql = sql.Trim() + "05,07,08,09,14,15,16,17";
            }
            sql = sql.Trim() + ") ) ";

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ret = ExecSQL(conn_db,
                " Update t_norm_gas Set " +
                "  rashod_kbm = ( Select max(r2.value" + sConvToNum + ")" +
                " From " + rashod.paramcalc.kernel_alias + "res_values r1, " + rashod.paramcalc.kernel_alias + "res_values r2 " +
                    "  Where r1.nzp_res = nzp_res0 and r2.nzp_res = nzp_res0 " +
                    "   and r1.nzp_x = 1 and r2.nzp_x = 2 and r1.nzp_y=r2.nzp_y " +
                    "   and trim(r1.value) = ("+
                        " (case when is_gp =1 then '1' else '0' end) ||" +
                        " (case when is_gvs=1 then '1' else '0' end) ||" +
                        " (case when is_gk =1 then '1' else '0' end)" +
                    ") " +
                    " ) " +
                " Where (is_gp > 0 or is_gk>0) " +
                "   and nzp_res0 in ( Select nzp_res From " + rashod.paramcalc.kernel_alias + "res_values ) "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            ret = ExecSQL(conn_db,
                " Update t_norm_gas Set rashod_kbm = 0 Where rashod_kbm is null "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            // ===  установка норматива по газу в counters_xx ===
            sql =
                " Update " + rashod.counters_xx +
                " Set (val1,val1_g,val3,rash_norm_one) = "+
#if PG
                "((Select rashod_kbm * gil1 From t_norm_gas k" +
                " Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar),"+
                "(Select rashod_kbm * gil1_g From t_norm_gas k" +
                " Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar),"+
                "(Select rashod_kbm From t_norm_gas k" +
                " Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar),"+
                "(Select rashod_kbm From t_norm_gas k" +
                " Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar)) " +
#else
                "((Select rashod_kbm * gil1,rashod_kbm * gil1_g,rashod_kbm,rashod_kbm From t_norm_gas k" +
                " Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar)) " +
#endif
                " Where nzp_type = 3 and stek = 3 and nzp_serv=10 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and 0 < ( Select count(*) From t_norm_gas k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            #endregion N: газ -

            #region N: полив
            //

            //----------------------------------------------------------------
            //N: полив
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_c1 " +
                " ( nzp_kvar integer, " +
                "   ival1 integer default 0, " +
                "   rval1  " + sDecimalType + "(12,4) default 0.00," +
                "   rval2  " + sDecimalType + "(12,4) default 0.00 " +
#if PG
                " )  "
#else
                " ) With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            // для РТ нормативный расход полива = объем на сотку * кол-во соток
            string sNorm200 =
            ",1 as ival1 " +
            ",max(case when nzp_prm=262 then p.val_prm" + sConvToInt + " else 0 end) as rval1 " +
            ",max(case when nzp_prm=390 then p.val_prm" + sConvToInt + " else 0 end) as rval2 ";
            string sFlds200 = "262,390";
            if (Points.IsSmr)
            {
                // для Самары нормативный расход полива = кол-во поливок * площадь полива в кв.м * объем на 1 кв.м
                sNorm200 =
                ",max(case when nzp_prm=2044 then p.val_prm" + sConvToInt + " else 0 end) as ival1 " +
                ",max(case when nzp_prm=2011 then p.val_prm" + sConvToInt + " else 0 end) as rval1 " +
                ",max(case when nzp_prm=2043 then p.val_prm" + sConvToInt + " else 0 end) as rval2 ";
                sFlds200 = "2011,2043,2044";
            }

            ret = ExecSQL(conn_db,
                  " insert into ttt_aid_c1 (nzp_kvar, ival1, rval1, rval2) " +
                  " Select nzp as nzp_kvar" + sNorm200 +
                  " From " + rashod.counters_xx + " a, ttt_prm_1 p " +
                  " Where a.nzp_kvar = p.nzp " +
                  "   and p.nzp_prm in (" + sFlds200 + ") " +
                  "   and a.nzp_type = 3 and a.stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                  "   and a.nzp_serv=200 " +
                  " Group by 1 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1 on ttt_aid_c1 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);

            sql =
                    " Update " + rashod.counters_xx +
                    " Set val1 = ( Select k.ival1 * k.rval1 * k.rval2 From ttt_aid_c1 k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar ) " +
                    " , rash_norm_one = ( Select k.ival1 * k.rval2 From ttt_aid_c1 k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar ) " +
                    " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                    "   and nzp_serv=200 " +
                    "   and 0 < ( Select count(*) From ttt_aid_c1 k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            #endregion N: полив

            #region N: вода для бани

            //----------------------------------------------------------------
            //N: вода для бани
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_c1 " +
                " ( nzp_kvar integer, " +
                "   gil1   " + sDecimalType + "(12,4) default 0.00," +
                "   gil1_g " + sDecimalType + "(12,4) default 0.00," +
                "   norm   " + sDecimalType + "(12,4) default 0.00 " +
#if PG
                " )  "
#else
                " ) With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            // для РТ нормативный расход вода для бани = объем на Кж * Норма на 1 чел.
            sFlds200 = "268";
            ret = ExecSQL(conn_db,
                  " insert into ttt_aid_c1 (nzp_kvar, gil1, gil1_g, norm) " +
                  " Select nzp as nzp_kvar, gil1, gil1_g, max(p.val_prm" + sConvToNum + ") " +
                  " From " + rashod.counters_xx + " a, ttt_prm_1 p " +
                  " Where a.nzp_kvar = p.nzp " +
                  "   and p.nzp_prm in (" + sFlds200 + ") " +
                  "   and a.nzp_type = 3 and a.stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                  "   and a.nzp_serv=203 " +
                  " Group by 1,2,3 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Create index ix_ttt_aid_c1 on ttt_aid_c1 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);

            sql =
                    " Update " + rashod.counters_xx +
                    " Set" +
                      " cnt1 = " + sFlds200 +
                      ",val1 = ( Select k.gil1 * k.norm From ttt_aid_c1 k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar )" +
                      ",val1_g = ( Select k.gil1_g * k.norm From ttt_aid_c1 k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar )" +
                      ",rash_norm_one = ( Select k.norm From ttt_aid_c1 k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar )" +
                    " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                      " and nzp_serv=203 " +
                      " and 0 < ( Select count(*) From ttt_aid_c1 k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar ) ";

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            #endregion N: вода для бани

            #region N: питьевая вода
            //----------------------------------------------------------------
            //N: питьевая вода
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_c1 " +
                " ( nzp_kvar integer, " +
                "   ival1 integer default 0, " +
                "   rval1  "  + sDecimalType + "(12,4) default 0.00," +
                "   rval1_g " + sDecimalType + "(12,4) default 0.00," +
                "   rval2  "  + sDecimalType + "(12,4) default 0.00 " +
#if PG
                " ) "
#else
                " ) With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            // для РТ нормативный расход пит.воды = кол-во жильцов * кол-во литров (норма на дом)
            ret = ExecSQL(conn_db,
                " insert into ttt_aid_c1 (nzp_kvar, ival1, rval1, rval1_g, rval2) " +
                " Select nzp_kvar,max(cnt1),max(gil1),max(gil1_g),max( replace(" + sNvlWord + "(p.val_prm,'0'),',','.')" + sConvToNum + " )" +
                " From " + rashod.counters_xx + " a "+
#if PG
                " left outer join ttt_prm_2 p on a.nzp_dom=p.nzp and p.nzp_prm=705 " +
                  " Where 1=1 " +
#else
                ", outer ttt_prm_2 p " +
                  " Where a.nzp_dom=p.nzp and p.nzp_prm=705 "+
#endif
                "   and a.nzp_type = 3 and a.stek = 3 and " + 
                rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and a.nzp_serv=253 " +
                " Group by 1 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1 on ttt_aid_c1 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);

            sql =
                    " Update " + rashod.counters_xx +
                    " Set (val1,val1_g,rash_norm_one) ="+
#if PG
                    " (( Select k.rval1 * k.rval2 From ttt_aid_c1 k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar ),"+
                    "( Select k.rval1_g * k.rval2 From ttt_aid_c1 k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar ),"+
                    "( Select k.rval2 From ttt_aid_c1 k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar )) " +
#else
                    " (( Select k.rval1 * k.rval2,k.rval1_g * k.rval2,k.rval2 From ttt_aid_c1 k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar )) " +
#endif
                    " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                    "   and nzp_serv=253 " +
                    "   and 0 < ( Select count(*) From ttt_aid_c1 k Where " + rashod.counters_xx + ".nzp_kvar = k.nzp_kvar ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            #endregion N: питьевая вода

            #region  сохранить стек 3 (нормативы!) в стек 30 для Анэса
            //----------------------------------------------------------------
            // сохранить стек 3 (нормативы!) в стек 30 для Анэса
            ExecSQL(conn_db, " Drop table ttt_aid_kpu ", false);
            ret = ExecSQL(conn_db,
                  " Select nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, "+
                        " dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, "+
                        " cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info " +
                        " ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, rash_norm_one " +
#if PG
                        " Into temp ttt_aid_kpu  "+
#else
#endif
                        " From " + rashod.counters_xx +
                  " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge
#if PG
#else
                +" Into temp ttt_aid_kpu With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " create index ix_ttt_aid_kpu on ttt_aid_kpu (nzp_kvar,nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_kpu ", true);

            #region сохраним параметры расчета норматива отопления
            // в ttt_aid_kpu уже nzp_type = 3 and stek = 3 !

            // сохраним параметры расчета норматива отопления

            // val1 = rashod_gkal_m2 * s_otopl
            // rashod_gkal_m2   -> val3        - нормативный расход в ГКал на 1 м2
            // s_otopl          -> squ2        - отапливаемая площадь
            // vid_alg          -> kod_info    - вид методики расчета норматива
            // ugl_kv           -> cnt4        - признак угловой квартиры (0-обычная/1-угловая)
            // otopl_period     -> cnt5        - продолжительность отопительного периода в сутках (обычно = 218)
            // koef_god_pere    -> gil2        - коэффициент перерасчета по итогам года
            // tmpr_vnutr_vozd  -> pu7kw       - температура внутреннего воздуха (обычно = 20, для угловых = 22)
            // tmpr_vnesh_vozd  -> gl7kw       - средняя температура внешнего воздуха  (обычно = -5.7)

            // vid_alg=1 - памфиловская методика расчета норматива

            // dom_objem        -> kf_dpu_kg   - объем дома
            // dom_pol_pl       -> kf_dpu_plob - полезная/отапливаемая площадь дома
            // dom_ud_otopl_har -> kf_dpu_plot - удельная отопительная характеристика дома
            // dom_otopl_koef   -> kf_dpu_ls   - поправочно-отопительный коэффициент для дома

            ret = ExecSQL(conn_db,
                " Update ttt_aid_kpu Set ("+
                " cnt4       ,cnt5       ,gil2       ,pu7kw    ,gl7kw    ," +
                " kf_dpu_kg  ,kf_dpu_plob,kf_dpu_plot,kf_dpu_ls           " +
#if PG
                ") = (( Select ugl_kv  From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar ),"+
                "( Select otopl_period From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar ),"+
                "( Select koef_god_pere From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar ),"+
                "( Select tmpr_vnutr_vozd From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar ),"+
                "( Select tmpr_vnesh_vozd From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar ),"+
                "( Select dom_objem  From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar ),"+
                "( Select dom_pol_pl From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar ),"+
                "( Select dom_ud_otopl_har From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar ),"+
                "( Select dom_otopl_koef From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar )) " +
#else
                ") = (( Select " +
                " ugl_kv         ,otopl_period    ,koef_god_pere   ,tmpr_vnutr_vozd,tmpr_vnesh_vozd," +
                " dom_objem      ,dom_pol_pl      ,dom_ud_otopl_har,dom_otopl_koef                  " +
                " From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar )) " +
#endif
                " Where  nzp_serv=8 " +
                "   and 0 < ( Select count(*) From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar and k.vid_alg=1 ) "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            
            // vid_alg=2 - методика расчета норматива по Пост306 без интерполяции удельного расхода тепловой энергии
            // vid_alg=3 - методика расчета норматива по Пост306  с интерполяцией удельного расхода тепловой энергии

            // dom_kol_etag     -> cnt_stage   - количество этажей дома (этажность)
            // pos_etag         -> cls1        - позиция по количеству этажей дома в таблице удельных расходов тепловой энергии
            // pos_narug_vozd   -> cls2        - позиция по температуре наружного воздуха в таблице удельных расходов тепловой энергии
            // dom_dat_postr    -> dat_s       - дата постройки дома
            // dom_ud_tepl_en1  -> kf_dpu_kg   - минимальный  удельный расход тепловой энергии для дома по температуре и этажности
            // dom_ud_tepl_en2  -> kf_dpu_plob - максимальный удельный расход тепловой энергии для дома по температуре и этажности
            // tmpr_narug_vozd1 -> kf_dpu_plot - минимально  близкая температура наружного воздуха в таблице
            // tmpr_narug_vozd2 -> kf_dpu_ls   - максимально близкая температура наружного воздуха в таблице
            // tmpr_narug_vozd  -> kf307n      - температура наружного воздуха по проекту (паспорту) дома
            // dom_ud_tepl_en   -> kf307f9     - удельный расход тепловой энергии для дома по температуре и этажности

            
            ret = ExecSQL(conn_db,
                " Update ttt_aid_kpu Set (" +
                " cnt4       ,cnt5       ,gil2       ,pu7kw    ,gl7kw    ," +
                " cnt_stage  ,cls1       ,cls2       ,dat_s    ,kf_dpu_kg," +
                " kf_dpu_plob,kf_dpu_plot,kf_dpu_ls  ,kf307n   ,kf307f9   " +
#if PG
                ") = (( Select  ugl_kv From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar ),"+
                "( Select otopl_period From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar ),"+
                "( Select koef_god_pere From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar ),"+
                "( Select tmpr_vnutr_vozd From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar ),"+
                "( Select tmpr_vnesh_vozd From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar ),"+
                 "( Select  dom_kol_etag  From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar )," +
                 "( Select pos_etag From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar )," +
                 "( Select pos_narug_vozd From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar )," +
                 "( Select dom_dat_postr From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar )," +
                 "( Select dom_ud_tepl_en1 From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar )," +
                 "( Select dom_ud_tepl_en2 From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar )," +
                 "( Select tmpr_narug_vozd1 From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar )," +
                 "( Select tmpr_narug_vozd2 From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar )," +
                "( Select tmpr_narug_vozd From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar ),"+
                "( Select dom_ud_tepl_en From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar )"+
                ") " +
#else
                ") = (( Select " +
                " ugl_kv         ,otopl_period    ,koef_god_pere   ,tmpr_vnutr_vozd,tmpr_vnesh_vozd," +
                " dom_kol_etag   ,pos_etag        ,pos_narug_vozd  ,dom_dat_postr  ,dom_ud_tepl_en1," +
                " dom_ud_tepl_en2,tmpr_narug_vozd1,tmpr_narug_vozd2,tmpr_narug_vozd,dom_ud_tepl_en  " +
                " From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar )) " +
#endif
                " Where  nzp_serv=8 " +
                "   and 0 < ( Select count(*) From t_norm_otopl k Where ttt_aid_kpu.nzp_kvar = k.nzp_kvar and k.vid_alg in (2,3) ) "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            #endregion сохраним параметры расчета норматива отопления

            #region вставка нормативов и параметров в стек 30
            //-- вставка нормативов и параметров --------------------------------------------------------------
            ret = ExecSQL(conn_db,
                  " Insert into " + rashod.counters_xx + 
                        " ( nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                        "   dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                        "   cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info, rash_norm_one ) " +
                  " Select nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, 30 stek, rashod, " +
                       "   dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                       "   cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info, rash_norm_one " +
                  " From ttt_aid_kpu "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            #endregion вставка нормативов и параметров в стек 30

            #endregion  сохранить стек 3 (нормативы!) в стек 30 для Анэса

            #endregion Выборка нормативов

            #region вставка ИПУ по лс - stek=3 & nzp_type = 3
            //----------------------------------------------------------------
            //итого по лс - stek=3 & nzp_type = 3
            //----------------------------------------------------------------
            // cnt1 - кол-во жильцов (целое)
            // gil1 - кол-во жильцов (с учетом времен. выбывших, дробное)
            // val1 - нормативные расходы (уже заполнены всем)
            // val2 - расходы КПУ
            // squ1 - площадь лс (уже заполнены)
            // rvirt - вирт. расход

            // 87 постановление:
            // dop87 - 7 кВт или добавок  (87 П)
            // gl7kw - 7 кВт КПУ (учитывая корректировку) 

            #region Выбрать расходы по ИПУ
            //
            // ... beg проставить ИПУ в stek = 3 ...
            //
            ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_virt0 " +
                " ( nzp_kvar integer, " +
                "   nzp_serv integer, " +
                "   nzp_counter integer, " +
                "   stek   integer, " +
                "   dat_s  date not null," +
                "   val1   " + sDecimalType + "(15,7) default 0.00," +
                "   val3   " + sDecimalType + "(15,7) default 0.00," +
                "   rvirt  " + sDecimalType + "(15,7) default 0.00 " +
#if PG
                " )  "
#else
                " ) With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ret = ExecSQL(conn_db,
                  " Insert into ttt_aid_virt0 (nzp_kvar,nzp_serv,nzp_counter,stek,dat_s,val1,val3,rvirt) " +
                  " Select nzp_kvar, nzp_serv, nzp_counter, stek, dat_s, val1, val3, rvirt" +
                  " From " + rashod.counters_xx +
                  " Where nzp_type = 3 and stek = 1 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge 
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Create index ix1_ttt_aid_virt0 on ttt_aid_virt0 (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);

            ExecSQL(conn_db, " Drop table ttt_ans_kpu ", false);
            ret = ExecSQL(conn_db,
#if PG
                " Select distinct nzp_counter into temp ttt_ans_kpu From ttt_aid_virt0 "
#else
                " Select unique nzp_counter From ttt_aid_virt0 into temp ttt_ans_kpu with no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            ExecSQL(conn_db, " Create index ix1_ttt_ans_kpu on ttt_ans_kpu (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_ans_kpu ", true);

            ret = ExecSQL(conn_db,
                  " Insert into ttt_aid_virt0 (nzp_kvar,nzp_serv,nzp_counter,stek,dat_s,val1,val3,rvirt) " +
                  " Select nzp_kvar, nzp_serv, nzp_counter, stek, dat_s, val1, val3, rvirt" +
                  " From " + rashod.counters_xx + " a " +
                  " Where nzp_type = 3 and stek = 2 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                  " and 0=(select count(*) from ttt_ans_kpu b where a.nzp_counter=b.nzp_counter) "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Create index ix2_ttt_aid_virt0 on ttt_aid_virt0 (nzp_kvar,nzp_serv,nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);

            #endregion Выбрать расходы по ИПУ

            #region убрать ИПУ если их несколько для ЛС и по каким-то нет показаний
            // убрать ИПУ если их несколько для ЛС и по каким-то нет показаний
            // ... для РТ надо подумать как лучше??? с периода ??? dat_close лежит в counters! ...
            if (Points.IsSmr)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_kpu ", false);

                ret = ExecSQL(conn_db,
                    " Create temp table ttt_aid_kpu " +
                    " ( nzp_kvar integer, " +
                    "   nzp_serv integer, " +
                    "   nzp_counter integer " +
#if PG
                    " )  "
#else
                    " ) With no log "
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                // выбрать ИПУ по ЛС и услуге, по которые не учтены
                ret = ExecSQL(conn_db,
                    " Insert into ttt_aid_kpu (nzp_kvar,nzp_serv,nzp_counter)" +
                    " Select s.nzp,s.nzp_serv,s.nzp_counter " +
                    " From temp_cnt_spis s  " +
                    " Where s.dat_close is Null " +
                    " and 0 = (select count(*) from ttt_aid_virt0 c Where c.nzp_kvar = s.nzp and c.nzp_serv = s.nzp_serv and c.nzp_counter = s.nzp_counter) "
                    , true);

                ExecSQL(conn_db, " Create index ix_aid33_kpu on ttt_aid_kpu (nzp_kvar,nzp_serv) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_kpu ", true);

                // исключить ИПУ из расчета, по ЛС и услуге по которым есть действующие ИПУ без расходов в месяце
                ret = ExecSQL(conn_db,
                    " delete from ttt_aid_virt0 " +
                    " Where 0 < (select count(*) from ttt_aid_kpu c Where c.nzp_kvar = ttt_aid_virt0.nzp_kvar and c.nzp_serv = ttt_aid_virt0.nzp_serv) "
                    , true);
            }

            #endregion убрать ИПУ если их несколько для ЛС и по каким-то нет показаний

            #region Вставить ИПУ + Если ИПУ снят в середине месяца - часть месяца считать по нормативу

            ExecSQL(conn_db, " Drop table ttt_aid_kpu ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_kpu " +
                " ( nzp_kvar integer, " +
                "   nzp_serv integer, " +
                "   squ      " + sDecimalType + "( 8,2) default 0, " +
                "   val1     " + sDecimalType + "(15,7) default 0.00," +
                "   val1_mid " + sDecimalType + "(15,7) default 0.00," + 
                "   stek     integer, " +
                "   days_mid integer default 0," +
                "   val3     " + sDecimalType + "(15,7) default 0.00," +
                "   rvirt    " + sDecimalType + "(15,7) default 0.00 " +
#if PG
                " )  "
#else
                " ) With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            //показания КПУ
            ret = ExecSQL(conn_db,
                  /*
                  " Insert into ttt_aid_kpu (nzp_kvar, nzp_serv, stek_1,val1_1, val1_mid,days_mid, val3_1,rvirt_1,val1_2,val3_2,rvirt_2 ) " +
                  " Select nzp_kvar, nzp_serv, " +

                         " sum(case when stek =1 then 1     else 0 end) as stek_1, " +
                         " sum(case when stek =1 then val1  else 0 end) as val1_1, " +

                         //показание в середине месяца
                         " 0 as val1_mid, " + //норматив
                         " max(case when stek =1 " +
                                    " and dat_s > " + rashod.paramcalc.dat_s +
                                    " and dat_s < " + rashod.paramcalc.dat_po +
                                    " then (dat_s - " + rashod.paramcalc.dat_s + " )*1 else 0 end) as days_mid, " +

                         " sum(case when stek =1 then val3  else 0 end) as val3_1, " +
                         " sum(case when stek =1 then rvirt else 0 end) as rvirt_1, " +

                         " sum(case when stek =2 then val1  else 0 end) as val1_2, " +
                         " sum(case when stek =2 then val3  else 0 end) as val3_2, " +
                         " sum(case when stek =2 then rvirt else 0 end) as rvirt_2 " +

                  " From " + rashod.counters_xx +
                  " Where nzp_type = 3 and stek in (1,2 ) and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                  " Group by 1,2 "
                  */
                  " Insert into ttt_aid_kpu (nzp_kvar, nzp_serv, stek, val1, val1_mid, days_mid, val3, rvirt) " +
                  " Select nzp_kvar, nzp_serv, " +

                         " min(stek) as stek, " +
                         " sum(val1) as val1, " +

                         //показание в середине месяца
                         " 0 as val1_mid, " + //норматив
                         " max(case when stek =1 " +
                                    " and dat_s > " + rashod.paramcalc.dat_s +
                                    " and dat_s <=" + rashod.paramcalc.dat_po +
#if PG
                         " then EXTRACT('days' from (dat_s - " + rashod.paramcalc.dat_s + " ))*1 else 0 end) as days_mid, " +
#else
                         " then (dat_s - " + rashod.paramcalc.dat_s + " )*1 else 0 end) as days_mid, " +
#endif

                         " sum(val3) as val3, " +
                         " sum(rvirt) as rvirt " +

                  " From ttt_aid_virt0 " +
                  " Where 1=1 " +
                  " Group by 1,2 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Create unique index ix_aid33_kpu on ttt_aid_kpu (nzp_kvar,nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_kpu ", true);

            //надо заполнить val1_mid, т.к. в пред. запросе не выбирался stek=3
            ret = ExecSQL(conn_db,
                " Update ttt_aid_kpu " +
                " Set val1_mid = ( Select max(val1) From " + rashod.counters_xx + " a" +
                                 " Where ttt_aid_kpu.nzp_kvar = a.nzp_kvar  " +
                                 "   and ttt_aid_kpu.nzp_serv = a.nzp_serv " +
                                 "   and nzp_type = 3 and stek = 30 " +
                                 "   and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                                 " ) " +
                " Where 0 < ( Select max(1) From " + rashod.counters_xx + " a" +
                                 " Where ttt_aid_kpu.nzp_kvar = a.nzp_kvar  " +
                                 "   and ttt_aid_kpu.nzp_serv = a.nzp_serv " +
                                 "   and nzp_type = 3 and stek = 30 " +
                                 "   and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                                 " ) "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ret = ExecSQL(conn_db,
                " Update ttt_aid_kpu " + 
                " Set squ = ( Select max(sqot) From ttt_aid_sq a Where ttt_aid_kpu.nzp_kvar = a.nzp_kvar ) " +
                " Where nzp_serv = 8 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }


            // расход на 1 кв.метр для отопления (для средних по ИПУ уже есть! stek = 2)
            ret = ExecSQL(conn_db,
                " Update ttt_aid_kpu " +
                " Set val3 = val1 / squ " +
                " Where nzp_serv = 8 and squ > 0 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            if (Points.IsSmr) // округлить для Самары т.к. отгображение в СФ до 4х знаков
            {
                ret = ExecSQL(conn_db,
                    " Update ttt_aid_kpu " +
                    " Set val1=round(val1,4), rvirt=round(rvirt,4) " +
                    " Where nzp_serv <> 8  "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }
            // для всех услуг кроме отопления
            sql =
                    " Update " + rashod.counters_xx +
                    " Set (val2,rvirt,cnt_stage) = (( " +  //cnt_stage = 1 - признак наличия КПУ / = 9 - среднее ИПУ
#if PG
                    " Select val1 From ttt_aid_kpu a " +
                    " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar  " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                    " ),"+
                    " (Select rvirt From ttt_aid_kpu a " +
                    " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar  " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                    " ),"+
                    " (Select (case when stek=1 then 1 else 9 end)" +
                    "  From ttt_aid_kpu a " +
                    " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar  " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                    " )) " +
#else
                    " Select val1, rvirt, (case when stek=1 then 1 else 9 end)" +
                    "  From ttt_aid_kpu a " +
                    " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar  " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                    " )) " +
#endif
                    " Where nzp_type = 3 and stek = 3 and nzp_serv <> 8 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                      " and 0 < ( Select count(*) From ttt_aid_kpu a " +
                                " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar  " +
                                "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                " ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            // для отопления val4 - расход ГКал на кв.метр
            sql =
                    " Update " + rashod.counters_xx +
                    " Set (val2,val4,rvirt,cnt_stage) = (( " +  //cnt_stage = 1 - признак наличия КПУ
#if PG
                    " Select val1 From ttt_aid_kpu a " +
                    " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar  " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                    " ),"+
                    " (Select val3 From ttt_aid_kpu a " +
                    " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar  " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                    " )," +
                    " (Select rvirt From ttt_aid_kpu a " +
                    " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar  " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                    " )," +
                    " (Select (case when stek=1 then 1 else 9 end) " +
                    "  From ttt_aid_kpu a " +
                    " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar  " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                    " )) " +
#else
                    " Select val1, val3, rvirt, (case when stek=1 then 1 else 9 end) " +
                    "  From ttt_aid_kpu a " +
                    " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar  " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                    " )) " +
#endif
                    " Where nzp_type = 3 and stek = 3 " +
                    "   and nzp_serv = 8 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                      " and 0 < ( Select count(*) From ttt_aid_kpu a " +
                                " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar  " +
                                "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                " ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            // ... end проставить ИПУ в stek = 3 ...

            //уменьшим норматив для показаний в середине месяца для всех услуг кроме отопления 
            sql =
                    " Update " + rashod.counters_xx +
                    " Set (kod_info,cnt5,val1,val1_g) = (( " +
#if PG
                    " Select 9901 From ttt_aid_kpu a " +
                    " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar  " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                    "   and days_mid > 0 " +
                    " ),"+
                    " (Select  max(days_mid) From ttt_aid_kpu a " +
                    " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar  " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                    "   and days_mid > 0 " +
                    " )," +
                    " (Select max(val1_mid/" + DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm) + " * days_mid) From ttt_aid_kpu a " +
                    " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar  " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                    "   and days_mid > 0 " +
                    " )," +
                    " (Select max(val1_mid/" + DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm) + " * days_mid) " +
                    " From ttt_aid_kpu a " +
                    " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar  " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                    "   and days_mid > 0 " +
                    " )) " +
#else
                    " Select 9901, max(days_mid), max(val1_mid/" + DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm) + " * days_mid)," +
                    " max(val1_mid/" + DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm) + " * days_mid) " +
                    " From ttt_aid_kpu a " +
                    " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar  " +
                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                    "   and days_mid > 0 " +
                    " )) " +
#endif
                    " Where nzp_type = 3 and stek = 3 " +
                    "   and nzp_serv <> 8 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                    "   and 0 < ( Select max(1) From ttt_aid_kpu a " +
                                " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar  " +
                                "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                "   and days_mid > 0 " +
                                " ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            #endregion Вставить ИПУ + Если ИПУ снят в середине месяца - часть месяца считать по нормативу


            #endregion вставка ИПУ по лс - stek=3 & nzp_type = 3

            // расход ВСЕГДА = val1 + val2:
            // для ИПУ val1 = 0 и val2 = ИПУ , 
            // для нормативов val1 = норма и val2 = 0
            // для ИПУ в середине месяца val1 > 0 и val2 > 0

            #region Обнулить расходы для kod_info <> 9901
            sql =
                    " Update " + rashod.counters_xx +
                    " Set val1 = 0,val1_g = 0 " +
                    " Where nzp_type = 3 and stek = 3 " +
                    "   and kod_info <> 9901 and cnt_stage in (1,9) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            #endregion Обнулить расходы для kod_info <> 9901

            #region коммуналки - уменьшим расход в пропорции общего кол-ва жильцов
            //----------------------------------------------------------------
            //коммуналки - уменьшим расход в пропорции общего кол-ва жильцов
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
            
            if (!Points.IsSmr)
            {
                //найдем все пары коммуналок - НО только, если есть показания КПУ !!!
                //string aaa = (rashod.paramcalc.b_cur ? " is null " : " = MDY(" + rashod.paramcalc.cur_mm + ",28," + rashod.paramcalc.cur_yy + ") ");

                ret = ExecSQL(conn_db,
                      " Select d.nzp_kvar,d.nzp_serv "+
#if PG
                        " Into temp cnt_d  " + //признак коммуналки
#else
#endif
                      " From " + rashod.counters_xx + " d " +
                      " Where d.nzp_type = 3 and d.stek = 3 and d." + rashod.where_dom +
                    //"   and d.dat_charge " + aaa +
                      "   and d.mmnog > 0 " +
                      " Group by 1,2 "
#if PG
#else
                    + " Into temp cnt_d With no log "  //признак коммуналки
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ExecSQL(conn_db, " Create unique index ix_cnt_d on cnt_d (nzp_kvar,nzp_serv) ", true);
                ExecSQL(conn_db, sUpdStat + " cnt_d ", true);

                ret = ExecSQL(conn_db,
                      " Select a.nzp_kvar,a.nzp_dom,a.nzp_serv,a.nzp_counter," +
                      "        c1.nzp_type,c1.nzp_cnttype,c1.num_cnt,  a.gil1, a.val1 " +
#if PG
                        " Into temp ttt_aid_a  "+
#else
#endif
                      " From " + rashod.counters_xx + " a, " +
                                 rashod.paramcalc.data_alias + "counters_spis c1 " +
                      " Where a.nzp_type = 3 and a.stek = 1 and a." + rashod.where_dom + rashod.where_kvarA +
                    //" and a.dat_charge " + aaa +
                      "   and a.val1  > 0  " +
                      "   and 0 < ( Select max(1) From cnt_d d Where a.nzp_kvar = d.nzp_kvar and a.nzp_serv = d.nzp_serv ) " +
                      "   and a.nzp_counter = c1.nzp_counter "
#if PG
#else
                        +" Into temp ttt_aid_a With no log "
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                ExecSQL(conn_db, " Create unique index ix_ttt_aid_a on ttt_aid_a (nzp_dom,nzp_type,nzp_cnttype,nzp_serv,num_cnt,nzp_kvar) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_a ", true);

                //еще раз перевыберем, покольку я предположил, что показание достаточно ввести толлько на одном приборе! ???
                ret = ExecSQL(conn_db,
                      " Select a.nzp_kvar,a.nzp_dom,a.nzp_serv,a.nzp_counter," +
                      "        c1.nzp_type,c1.nzp_cnttype,c1.num_cnt,  a.gil1 " +
#if PG
                      " Into temp ttt_aid_b  "+
#else
#endif
                      " From " + rashod.counters_xx + " a, " +
                                 rashod.paramcalc.data_alias + "counters_spis c1 " +
                      " Where a.nzp_type = 3 and a.stek = 1 and a." + rashod.where_dom + rashod.where_kvarA +
                    //" and a.dat_charge " + aaa +
                      "   and 0 < ( Select max(1) From cnt_d d Where a.nzp_kvar = d.nzp_kvar and a.nzp_serv = d.nzp_serv ) " +
                      "   and a.nzp_counter = c1.nzp_counter " +
#if PG
#else
                      " Into temp ttt_aid_b With no log " +
#endif
                   " " , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                ExecSQL(conn_db, " Create unique index ix_ttt_aid_b on ttt_aid_b (nzp_dom,nzp_type,nzp_cnttype,nzp_serv,num_cnt,nzp_kvar) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_b ", true);

                ret = ExecSQL(conn_db,
                      " Select a.nzp_dom, a.nzp_kvar, a.nzp_serv, a.nzp_type, a.nzp_cnttype, a.num_cnt, 0 as cnt_ls, " +
                             " max(a.gil1) as cnt_agil, sum(0) as cnt_bgil, max(a.val1) as common_val_kpu " +
#if PG
                      " Into temp ttt_aid_stat "+
#else
#endif
                      " From ttt_aid_a a, ttt_aid_b b  " +
                      " Where a.nzp_dom     = b.nzp_dom " +
                    //"   and a.nzp_counter = b.nzp_counter " +
                      "   and a.nzp_type    = b.nzp_type " +
                      "   and a.nzp_cnttype = b.nzp_cnttype " +
                      "   and a.nzp_serv    = b.nzp_serv " +
                      "   and a.num_cnt     = b.num_cnt  " +
                      "   and a.nzp_kvar   <> b.nzp_kvar " +
                      " Group by 1,2,3, 4,5,6 " +
#if PG
#else
                      " Into temp ttt_aid_stat With no log " +
#endif
                    " " , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                //gil1 - кол-во жильцов

                ExecSQL(conn_db, " Create index ix1_aid_stat on ttt_aid_stat (nzp_kvar, nzp_serv) ", true);
                ExecSQL(conn_db, " Create index ix2_aid_stat on ttt_aid_stat (nzp_dom, nzp_type, nzp_cnttype, nzp_serv, num_cnt) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_stat ", true);

                //вычислить общее кол-во жителей и кол-во лс в коммуналках в таблице ttt_aid_stat
                ExecSQL(conn_db, " Drop table cnt_d ", false);
                ret = ExecSQL(conn_db,
                      " Select nzp_dom, nzp_type, nzp_cnttype, nzp_serv, num_cnt,  sum(cnt_agil) as cnt_bgil, count(*) as  cnt_ls " +
#if PG
                        " Into temp cnt_d "+
#else
#endif
                      " From ttt_aid_stat " +
                      " Group by 1,2,3,4,5 "+
#if PG
#else
                    " Into temp cnt_d With no log "+
#endif
                   " " , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                ExecSQL(conn_db, " Create unique index ix2_cnt_d on cnt_d (nzp_dom, nzp_type, nzp_cnttype, nzp_serv, num_cnt) ", true);
                ExecSQL(conn_db, sUpdStat + " cnt_d ", true);

                ret = ExecSQL(conn_db,
                      " Update ttt_aid_stat " +
                      " Set (cnt_bgil, cnt_ls) = (( " +
#if PG
                                " Select  sum(cnt_bgil) From cnt_d b " +
                                " Where ttt_aid_stat.nzp_dom     = b.nzp_dom " +
                                "   and ttt_aid_stat.nzp_type    = b.nzp_type " +
                                "   and ttt_aid_stat.nzp_cnttype = b.nzp_cnttype " +
                                "   and ttt_aid_stat.nzp_serv    = b.nzp_serv " +
                                "   and ttt_aid_stat.num_cnt     = b.num_cnt  " +
                                                " ),"+
                                " (Select  max(cnt_ls) From cnt_d b " +
                                " Where ttt_aid_stat.nzp_dom     = b.nzp_dom " +
                                "   and ttt_aid_stat.nzp_type    = b.nzp_type " +
                                "   and ttt_aid_stat.nzp_cnttype = b.nzp_cnttype " +
                                "   and ttt_aid_stat.nzp_serv    = b.nzp_serv " +
                                "   and ttt_aid_stat.num_cnt     = b.num_cnt  " +
                                                " )) " +
#else
                                " Select  sum(cnt_bgil), max(cnt_ls) From cnt_d b " +
                                " Where ttt_aid_stat.nzp_dom     = b.nzp_dom " +
                                "   and ttt_aid_stat.nzp_type    = b.nzp_type " +
                                "   and ttt_aid_stat.nzp_cnttype = b.nzp_cnttype " +
                                "   and ttt_aid_stat.nzp_serv    = b.nzp_serv " +
                                "   and ttt_aid_stat.num_cnt     = b.num_cnt  " +
                                                " )) " +
#endif
                      " Where 0 < ( " +
                                " Select  max(1) From cnt_d b " +
                                " Where ttt_aid_stat.nzp_dom     = b.nzp_dom " +
                                "   and ttt_aid_stat.nzp_type    = b.nzp_type " +
                                "   and ttt_aid_stat.nzp_cnttype = b.nzp_cnttype " +
                                "   and ttt_aid_stat.nzp_serv    = b.nzp_serv " +
                                "   and ttt_aid_stat.num_cnt     = b.num_cnt  " +
                                " ) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }


                //с долями морока!!
                //У коммуналок может быть как общие ПУ, так и индивидуальные 
                //Общие надо делить в пропорции
                //А ИПУ надо учесть безусловно 
                //Такая вот вафля!!
                //в val2 в 3-ем стеке лежим просуммированные показания (как обещго, так и ИПУ)
                //а пропорцию надо применить только на показание общего ПУ
                //пипец!

                sql =
                        " Update " + rashod.counters_xx +
                        " Set val_s = val2, " + //сохраним val2 расход по КПУ в коммуналках (что было)
                            " val_po = ( " +    //выделим в val_po расходы общих ПУ, которое и будем уменьшать
                                    " Select sum(common_val_kpu) From ttt_aid_stat a " +
                                    " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                    " ) " +
                        " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                          " and cnt_stage in (1,9) " +
                          " and 0 < ( Select max(1) From ttt_aid_stat a " +
                                    " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                    " ) ";

                if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                }
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                sql =
                        " Update " + rashod.counters_xx +
                        " Set val_s = val2 - val_po " + //val_s - это теперь расходы ИПУ (не общие ПУ!)
                        " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                          " and cnt_stage in (1,9) " +
                          " and 0 < ( Select max(1) From ttt_aid_stat a " +
                                    " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                    " ) ";

                if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                }
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                //и уменьшим расход в пропорции общего кол-ва жильцов
                sql =
                        " Update " + rashod.counters_xx + //cnt5 - общее кол-во жильцов, val2 = val_po*k (напомню, val_po - это расходы общих ПУ)
                        " Set (cnt5,val2) ="+
#if PG
                         " (( Select max(a.cnt_bgil) From ttt_aid_stat a " +
                           " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                           "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                           "   and a.cnt_bgil > 0 " +
                         "),"+
                         "( Select max(" + rashod.counters_xx + ".val_po * " + rashod.counters_xx + ".gil1 / a.cnt_bgil) " +
                           " From ttt_aid_stat a " +
                           " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                           "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                           "   and a.cnt_bgil > 0 " +
                         ")) " +
#else
                         " (( Select max(a.cnt_bgil), max(" + rashod.counters_xx + ".val_po * " + rashod.counters_xx + ".gil1 / a.cnt_bgil) " +
                           " From ttt_aid_stat a " +
                           " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                           "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                           "   and a.cnt_bgil > 0 " +
                         ")) " +
#endif
                        " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                          " and cnt_stage  in (1,9) " +
                          " and 0 < ( Select max(1) From ttt_aid_stat a " +
                                    " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                    "   and a.cnt_bgil > 0 " +
                                    " ) ";

                if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                }
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                //и уменьшим расход в пропорции лицевых счетов, если кол-во жильцов = 0
                sql =
                        " Update " + rashod.counters_xx +
                        " Set (cnt4,cnt5,val2) = "+
#if PG
                        "(( Select max(a.cnt_ls) From ttt_aid_stat a " +
                              " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                              "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                              "   and a.cnt_bgil = 0 " +
                            "),"+
                            "( Select max(a.cnt_bgil) From ttt_aid_stat a " +
                              " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                              "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                              "   and a.cnt_bgil = 0 " +
                            "),"+
                            "( Select max(" + rashod.counters_xx + ".val_po / a.cnt_ls) " +
                              " From ttt_aid_stat a " +
                              " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                              "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                              "   and a.cnt_bgil = 0 " +
                            ")) " +
#else
                        "(( Select max(a.cnt_ls),max(a.cnt_bgil), max(" + rashod.counters_xx + ".val_po / a.cnt_ls) " +
                              " From ttt_aid_stat a " +
                              " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                              "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                              "   and a.cnt_bgil = 0 " +
                            ")) " +
#endif
                        " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                          " and cnt_stage  in (1,9) " +
                          " and 0 < ( Select max(1) From ttt_aid_stat a " +
                                    " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                    "   and a.cnt_bgil = 0 " +
                                    " ) ";

                if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                }
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                //добавим val_s - расход по ИПУ
                sql =
                        " Update " + rashod.counters_xx +
                        " Set val2 = val2 + val_s " +
                        " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                          " and cnt_stage  in (1,9) " +
                          " and 0 < ( Select max(1) From ttt_aid_stat a " +
                                    " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                    " ) ";

                if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                }
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
            }
            #endregion Самара #region коммуналки - уменьшим расход в пропорции общего кол-ва жильцов

            #region проставить средние и изменения в нормативы - stek = 3 и nzp_type = 3
            //----------------------------------------------------------------
            // проставить средние и изменения в нормативы - stek = 3 и nzp_type = 3
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_aid_sred ", false);

            ret = ExecSQL(conn_db,
                  " Create temp table ttt_aid_sred " +
                  " ( no       serial,  " +
                  "   nzp_kvar integer, " +
                  "   nzp_serv integer, " +
                  "   val1     " + sDecimalType + "(12,4)," +
                  "   val3     " + sDecimalType + "(12,4) " +
#if PG
                  " ) "
#else
                  " ) With no log" 
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_sred (nzp_kvar,nzp_serv,val1,val3) " +
                " Select nzp_kvar, nzp_serv, max(val1), max(val3) " +
                " From " + rashod.counters_xx +
                " Where nzp_type = 3 and stek=5 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                " group by 1,2 " 
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Create unique index ix_aid00_sred on ttt_aid_sred (no) ", true);
            ExecSQL(conn_db, " Create        index ix_aid11_sred on ttt_aid_sred (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_sred ", true);

            ExecSQL(conn_db, " Drop table ttt_aid_c1xx ", false);
            ret = ExecSQL(conn_db,
                " create temp table ttt_aid_c1xx (nzp_kvar integer, nzp_serv integer)" +
#if PG
                " "
#else
                " with no log "
#endif
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_c1xx (nzp_kvar, nzp_serv)" +
                " Select nzp_kvar, nzp_serv From ttt_aid_sred "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1xx on ttt_aid_c1xx (nzp_kvar, nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1xx ", true);

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_sred (nzp_kvar,nzp_serv,val1,val3) " +
                " Select a.nzp_kvar, a.nzp_serv, max(a.val1), max(a.val3) " +
                " From " + rashod.counters_xx + " a " +
                " Where a.nzp_type = 3 and a.stek=4 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                " and 0=(select count(*) from ttt_aid_c1xx b where a.nzp_kvar=b.nzp_kvar and a.nzp_serv=b.nzp_serv) " +
                " group by 1,2 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            ExecSQL(conn_db, sUpdStat + " ttt_aid_sred ", true);
            ExecSQL(conn_db, " Drop table ttt_aid_c1xx ", false);

            // для отопления среднее и изменеие ИПУ - val1 = расход на квартиру & val3 = на 1 кв.метр!
            ret = ExecSQL(conn_db,
                " Update " + rashod.counters_xx +
                " Set (val1,val1_g,val3) = "+
#if PG
                            "(( Select val1 From ttt_aid_sred a " +
                             " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                             "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                             " ),"+

                             "( Select val1 From ttt_aid_sred a " +
                             " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                             "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                             " ),"+
                             "( Select val3 From ttt_aid_sred a " +
                             " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                             "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                             " )) " +
#else
                            "(( Select val1,val1,val3 From ttt_aid_sred a " +
                             " Where " + rashod.counters_xx +".nzp_kvar = a.nzp_kvar " +
                             "   and " + rashod.counters_xx +".nzp_serv = a.nzp_serv " +
                             " )) " +
#endif
                " Where nzp_type = 3 and stek = 3 and cnt_stage = 0 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and 0 < ( Select count(*) From ttt_aid_sred a " +
                            " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                            "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                            " ) " +
                "   and nzp_serv = 8 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            // для остальных услуг среднее и изменение ИПУ - просто расход на квартиру
            ret = ExecSQL(conn_db,
                " Update " + rashod.counters_xx +
                " Set kod_info=(case when nzp_serv=210 then 210 else kod_info end)," +
                "     val1 = ( Select val1 From ttt_aid_sred a " +
                             " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                             "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                             " ) " +
                " , val1_g = ( Select val1 From ttt_aid_sred a " +
                             " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                             "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                             " ) " +
                " Where nzp_type = 3 and stek = 3 and cnt_stage = 0 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and 0 < ( Select count(*) From ttt_aid_sred a " +
                            " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                            "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                            " ) " +
                "   and nzp_serv <> 8 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            #endregion проставить средние и изменения в нормативы - stek = 3 и nzp_type = 3

            #region проставить расход - stek = 3 и nzp_type = 3
            //--------------------------101 - по-умолчанию, норматив или КПУ
            //для отопления: val1/val2 = расход в ГКал на квартиру, val3/val4 = расход в ГКал на 1 м2, squ2 = отапливаемая площадь
            //для остальных услуг: val1/val2 = расход на квартиру
            sql =
                    " Update " + rashod.counters_xx +
                    " Set rashod = val1 + val2, nzp_counter = 0 " +
                    " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge;

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            #endregion проставить расход - stek = 3 и nzp_type = 3

            #region изменить нормативы по ночному э/э с учетом дневного э/э
            // e/e 210
            ExecSQL(conn_db, " Drop table ttt_ans_ee210 ", false);

            ret = ExecSQL(conn_db,
                  " Create temp table ttt_ans_ee210 " +
                  " ( nzp_kvar integer, " +
                  "   val210   " + sDecimalType + "(12,4)," +
                  "   val25    " + sDecimalType + "(12,4)," +
                  "   valn     " + sDecimalType + "(12,4)" +
#if PG
                  " ) "
#else
                  " ) With no log" 
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ret = ExecSQL(conn_db,
                " Insert into ttt_ans_ee210 (nzp_kvar,val210) " +
                " Select nzp_kvar, sum(rashod) " +
                " From " + rashod.counters_xx +
                " Where nzp_type = 3 and stek=3 and cnt_stage=0 and kod_info=0 and nzp_serv=210 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                " group by 1 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Create index ix_ans_ee210 on ttt_ans_ee210 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_ans_ee210 ", true);

            ret = ExecSQL(conn_db,
                " update ttt_ans_ee210 set val25 = ( Select sum(rashod) " +
                "   From " + rashod.counters_xx + " r Where ttt_ans_ee210.nzp_kvar=r.nzp_kvar and r.nzp_type = 3 and r.stek=3 and r.nzp_serv=25 ) "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ret = ExecSQL(conn_db,
                " update ttt_ans_ee210 set valn = val210 - val25 Where 1=1 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ret = ExecSQL(conn_db,
                " Update " + rashod.counters_xx +
                " Set kod_info=25" +
                ",rashod =" +
                " (select (case when a.valn>0 then a.valn else 0 end) from ttt_ans_ee210 a where " + rashod.counters_xx + ".nzp_kvar=a.nzp_kvar) " +
                ",val1 =" +
                " (select (case when a.valn>0 then a.valn else 0 end) from ttt_ans_ee210 a where " + rashod.counters_xx + ".nzp_kvar=a.nzp_kvar) " +
                " Where nzp_type = 3 and stek = 3 and nzp_serv=210 and cnt_stage=0 " +
                  " and 0<(select count(*) from ttt_ans_ee210 a where " + rashod.counters_xx + ".nzp_kvar=a.nzp_kvar and a.val25 > 0 ) "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            #endregion изменить нормативы по ночному э/э с учетом дневного э/э

            #region расчеты ОДН по дому - stek=3 & nzp_type = 1
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
            bool bIsCalcODN = true;
            IDbCommand cmd1991 = DBManager.newDbCommand(
                " select count(*) cnt " +
                " from " + rashod.paramcalc.data_alias + "prm_5 p " +
                " where p.nzp_prm=1991 and p.val_prm='1' " +
                " and p.is_actual<>100 and p.dat_s<" + rashod.paramcalc.dat_po + " and p.dat_po>=" + rashod.paramcalc.dat_s + " "
            , conn_db);

            try
            {
                string scntvals = Convert.ToString(cmd1991.ExecuteScalar());
                bIsCalcODN = (Convert.ToInt32(scntvals) == 0);
            }
            catch
            {
                bIsCalcODN = true;
            }
            bool bIsCalcCurMonth = ((rashod.paramcalc.cur_yy == rashod.paramcalc.calc_yy) && (rashod.paramcalc.cur_mm == rashod.paramcalc.calc_mm));
            
            //bool bDoCalcOdn = bIsCalcODN;
            //if (!bDoCalcOdn) { bDoCalcOdn = bIsCalcCurMonth; }
            // ...

            if (!b_calc_kvar)
            {
                #region итого по дому - stek=3 & nzp_type = 1 - суммы по ЛС и расход ОДПУ

                ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);

                // !!! перейти на учет только из стека 3 по Where условию !!!
                ret = ExecSQL(conn_db,
                    " Select nzp_dom, nzp_serv, " +
                           //кол-во жильцов с учетом вр.выб.
                           " sum(cnt1) as cnt1, " + // целое
                           " sum(gil1) as gil1, " + // дробное

                           //кол-во жильцов по лс без ПУ
                           " sum(case when cnt_stage = 0 then gil1 else 0 end) as gil2, " +

                           //расход по нормативу - пока норматив | изменения | средние
                           " sum(case when cnt_stage = 0 then val1 else 0 end) as val1, " +

                           //расход или средние по ИПУ
                           " sum(case when cnt_stage > 0 then val2 else 0 end) as val2, " +

                           //площадь по всем лс
                           " sum(case when nzp_serv=8 then squ2 else squ1 end) as squ1, " +

                           //площадь по лс без ПУ
                           " sum(case when cnt_stage = 0 then (case when nzp_serv=8 then squ2 else squ1 end) else 0 end) as squ2, " +

                           //кол-во лс по услуге
                           " count(*) as cls1, " +

                           //кол-во лс по услуге без ПУ
                           " sum(case when cnt_stage = 0 then 1 else 0 end) as cls2, " +

                           //виртуальный расход
                           " sum(rvirt) as rvirt  " +
#if PG
                    " Into temp ttt_aid_virt0 " +
#else
#endif
                    " From " + rashod.counters_xx +
                    " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                    " Group by 1,2 " +
#if PG
                    " "
#else
                    " Into temp ttt_aid_virt0 With no log "
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ExecSQL(conn_db, " Create unique index ix_aid33_v110 on ttt_aid_virt0 (nzp_dom,nzp_serv) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);

                sql =
                    " Update " + rashod.counters_xx +
                    " Set (cnt1,gil1,gil2,val1,val2,squ1,squ2,rvirt,cls1,cls2) = ( " +
#if PG
                                 "(Select cnt1 From ttt_aid_virt0 a " +
                                 " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom   " +
                                 "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv),"+
                                 "(Select gil1 From ttt_aid_virt0 a " +
                                 " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom   " +
                                 "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv),"+
                                 "(Select gil2 From ttt_aid_virt0 a " +
                                 " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom   " +
                                 "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv),"+
                                 "(Select val1 From ttt_aid_virt0 a " +
                                 " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom   " +
                                 "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv)," +
                                 "(Select val2 From ttt_aid_virt0 a " +
                                 " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom   " +
                                 "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv)," +
                                 "(Select squ1 From ttt_aid_virt0 a " +
                                 " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom   " +
                                 "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv)," +
                                 "(Select squ2 From ttt_aid_virt0 a " +
                                 " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom   " +
                                 "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv)," +
                                 "(Select rvirt From ttt_aid_virt0 a " +
                                 " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom   " +
                                 "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv)," +
                                 "(Select cls1 From ttt_aid_virt0 a " +
                                 " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom   " +
                                 "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv)," +
                                 "(Select cls2 From ttt_aid_virt0 a " +
                                 " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom   " +
                                 "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv) " +
#else
                                 "(Select cnt1,gil1,gil2,val1,val2,squ1,squ2,rvirt,cls1,cls2 From ttt_aid_virt0 a " +
                                 " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom   " +
                                 "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv) " +
#endif
                                " ) " +
                    " Where nzp_type = 1 and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                      " and 0 < ( Select count(*) From ttt_aid_virt0 a " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                " ) ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                //расход ДПУ
                ExecSQL(conn_db, " Drop table ttt_aid_dpu ", false);

                ret = ExecSQL(conn_db,
                    " Create temp table ttt_aid_dpu " +
                    //" Create table are.ttt_aid_dpu " +
                    " ( nzp_dom     integer, " +
                    "   nzp_serv    integer, " +
                    "   nzp_counter integer default 0, " +
                    "   is_odn_pu   integer default 0, " +
                    "   val1   " + sDecimalType + "(15,7) default 0.00," +
                    "   val_ls " + sDecimalType + "(15,7) default 0.00" +
#if PG
                    " ) "
#else
                    //" ) "
                    " ) With no log "
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ret = ExecSQL(conn_db,
                      " Insert into ttt_aid_dpu (nzp_dom,nzp_serv,nzp_counter,val1) " +
                      " Select nzp_dom, nzp_serv, nzp_counter, sum(val1) as val1 " +
                      " From " + rashod.counters_xx +
                      " Where nzp_type = 1 and stek = 1 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                      " Group by 1,2,3 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                ExecSQL(conn_db, sUpdStat + " ttt_aid_dpu ", true);

                //ViewTbl(conn_db, " select * from ttt_aid_dpu ");

                ExecSQL(conn_db, " Drop table ttt_ans_dpux ", false);
                ret = ExecSQL(conn_db,
#if PG
                      " Select nzp_dom, nzp_serv Into temp ttt_ans_dpux From ttt_aid_dpu "
#else
                      " Select nzp_dom, nzp_serv From ttt_aid_dpu Into temp ttt_ans_dpux With no log "
#endif
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                ExecSQL(conn_db, " Create index ix_ans_dpux on ttt_ans_dpux (nzp_dom,nzp_serv) ", false);
                ExecSQL(conn_db, sUpdStat + " ttt_ans_dpux ", false);

                // дополнение расходов ДПУ  из стека средних значений по ОДПУ nzp_type = 1 and stek = 2
                ret = ExecSQL(conn_db,
                      " insert into ttt_aid_dpu (nzp_dom, nzp_serv, nzp_counter, val1) " +
                      " Select nzp_dom, nzp_serv, nzp_counter, sum(val1) as val1 " +
                      " From " + rashod.counters_xx + " c" +
                      " Where nzp_type = 1 and stek = 2 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                      " and 0=(select count(*) from ttt_ans_dpux t where c.nzp_dom=t.nzp_dom and c.nzp_serv=t.nzp_serv) " +
                      " Group by 1,2,3 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                
                //ViewTbl(conn_db, " select * from ttt_aid_dpux ");

                //ViewTbl(conn_db, " select * from ttt_aid_dpu ");

                ExecSQL(conn_db, " Drop table ttt_ans_dpux ", false);
                ret = ExecSQL(conn_db,
#if PG
                " Select nzp_dom, nzp_serv Into temp ttt_ans_dpux  From ttt_aid_dpu  "
#else
                " Select nzp_dom, nzp_serv From ttt_aid_dpu Into temp ttt_ans_dpux With no log "
#endif
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                ExecSQL(conn_db, " Create index ix_ans_dpux on ttt_ans_dpux (nzp_dom,nzp_serv) ", false);
                ExecSQL(conn_db, sUpdStat + " ttt_ans_dpux ", false);
                
                // дополнение расходов ДПУ  из стека средних значений nzp_type = 1 and stek = 4
                ret = ExecSQL(conn_db,
                      " insert into ttt_aid_dpu (nzp_dom, nzp_serv, nzp_counter, val1) " +
                      " Select nzp_dom, nzp_serv, nzp_counter, sum(val1) as val1 " +
                      " From " + rashod.counters_xx + " c" +
                      " Where nzp_type = 1 and stek = 4 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                      " and 0=(select count(*) from ttt_ans_dpux t where c.nzp_dom=t.nzp_dom and c.nzp_serv=t.nzp_serv) " +
                      " Group by 1,2,3 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }


                //ViewTbl(conn_db, " select * from ttt_aid_dpux ");

                //ViewTbl(conn_db, " select * from ttt_aid_dpu ");

                ExecSQL(conn_db, " Drop table ttt_ans_dpux ", false);
                ExecSQL(conn_db, " Create index ix_aid_dpu on ttt_aid_dpu (nzp_dom,nzp_serv) ", false);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_dpu ", false);

                // дополнение расходов ДПУ только на ОДН расходом по ЛС
                ret = ExecSQL(conn_db,
                      " update ttt_aid_dpu set is_odn_pu = 1 " +
                      " Where 0<(select count(*) from " + rashod.paramcalc.data_alias + "prm_17 p " +
                      " where ttt_aid_dpu.nzp_counter=p.nzp and p.nzp_prm=2068 and p.val_prm='1' " +
                      " and p.is_actual<>100 and p.dat_s<" + rashod.paramcalc.dat_po + " and p.dat_po>=" + rashod.paramcalc.dat_s + ") "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                ret = ExecSQL(conn_db,
                      " update ttt_aid_dpu set val_ls = ( " +
                                    " Select sum(a.val1 + a.val2 + a.dlt_reval + a.dlt_real_charge) From " + rashod.counters_xx + " a " +
                                    " Where ttt_aid_dpu.nzp_dom  = a.nzp_dom  " +
                                    "   and ttt_aid_dpu.nzp_serv = a.nzp_serv " +
                                    "   and nzp_type = 1 and stek = 3 ) " +
                      " Where is_odn_pu = 1 " +
                        " and 0 < ( Select count(*) From " + rashod.counters_xx + " a " +
                                    " Where ttt_aid_dpu.nzp_dom  = a.nzp_dom  " +
                                    "   and ttt_aid_dpu.nzp_serv = a.nzp_serv " +
                                    "   and nzp_type = 1 and stek = 3 ) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                //
                //ViewTbl(conn_db, " select * from ttt_aid_dpu ");

                // проставить расход ДПУ в stek = 3 - для расчета ОДН и учета ДПУ / cur_zap - признак ОДПУ только на ОДН!
                sql =
                    " Update " + rashod.counters_xx +
                    " Set cnt_stage=1,(val3,cur_zap) = (( " +
#if PG
                                " Select sum(val1 + val_ls) From ttt_aid_dpu a " +
                                 " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                 "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                 " ),"+
                                 " (Select max(is_odn_pu) From ttt_aid_dpu a " +
                                 " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                 "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                 " )) " +
#else
                                 " Select sum(val1 + val_ls),max(is_odn_pu) From ttt_aid_dpu a " +
                                 " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                 "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                 " )) " +
#endif
                    " Where nzp_type = 1 and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                      " and 0 < ( Select count(*) From ttt_aid_dpu a " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                " ) ";

                if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                }
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                #endregion итого по дому - stek=3 & nzp_type = 1 - суммы по ЛС и расход ОДПУ

                #region  учесть и записать дельты расходов в перерасчетный месяц для возможности снятия при расчетах ОДН
                //----------------------------------------------------------------
                // учесть и записать дельты расходов в перерасчетный месяц для возможности снятия при расчетах ОДН - stek=2
                //----------------------------------------------------------------
                if (b_is_delta)
                  UseDeltaCntsForMonth(conn_db, rashod, paramcalc, out ret);

                //текущая дельта для домов с ОДПУ
                sql =
                        " Update " + rashod.counters_xx +
                        " Set dlt_cur = val3 - (val1 + val2 + dlt_reval + dlt_real_charge) " +
                        " Where nzp_type = 1 and stek = 3 and cnt_stage = 1 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge;

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                #endregion  учесть и записать дельты расходов в перерасчетный месяц для возможности снятия при расчетах ОДН - stek=2

                #region сохранить стек 3 для домов (nzp_type=1 and stek=3) в стек 354 с расчетом по Пост.№354 (+ нормативы ОДН)
                //----------------------------------------------------------------
                // сохранить стек 3 для домов (nzp_type=1 and stek=3) в стек 354 для расчетов по Пост.№354
                //----------------------------------------------------------------
                
                #region выбрать домовые расходы для расчета ОДН
                ExecSQL(conn_db, " Drop table ttt_aid_kpu ", false);

                ret = ExecSQL(conn_db,
                    //" CREATE TABLE are.ttt_aid_kpu(       "+
                    " CREATE temp TABLE ttt_aid_kpu(        " +
                    "   nzp_dom INTEGER NOT NULL,           " +
                    "   nzp_kvar INTEGER default 0 NOT NULL," +
                    "   nzp_type INTEGER NOT NULL,          " +
                    "   nzp_serv INTEGER NOT NULL,          " +
                    "   dat_charge DATE,                    " +
                    "   cur_zap INTEGER default 0 NOT NULL, " +
                    "   nzp_counter INTEGER default 0,      " +
                    "   cnt_stage INTEGER default 0,        " +
                    "   mmnog " + sDecimalType + "(15,7) default 1.0000000," +
                    "   stek INTEGER default 0 NOT NULL,    " +
                    "   rashod " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                    "   dat_s DATE NOT NULL,                " +
                    "   val_s "  + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                    "   dat_po DATE NOT NULL,               " +
                    "   val_po " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                    "   val1 "   + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                    "   val2 "   + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                    "   val3 "   + sDecimalType + "(15,7) default 0.0000000, " +
                    "   val4 "   + sDecimalType + "(15,7) default 0.0000000, " +
                    "   rvirt "  + sDecimalType + "(15,7) default 0.0000000, " +
                    "   squ1 "   + sDecimalType + "(15,7) default 0.0000000, " +
                    "   squ2 "   + sDecimalType + "(15,7) default 0.0000000, " +
                    "   cls1 INTEGER default 0 NOT NULL, " +
                    "   cls2 INTEGER default 0 NOT NULL, " +
                    "   gil1 " + sDecimalType + "(15,7) default 0.0000000, " +
                    "   gil2 " + sDecimalType + "(15,7) default 0.0000000, " +
                    "   cnt1 INTEGER default 0 NOT NULL, " +
                    "   cnt2 INTEGER default 0 NOT NULL, " +
                    "   cnt3 INTEGER default 0,          " +
                    "   cnt4 INTEGER default 0,          " +
                    "   cnt5 INTEGER default 0,          " +
                    "   dop87 "  + sDecimalType + "(15,7) default 0.0000000, " +
                    "   pu7kw "  + sDecimalType + "(15,7) default 0.0000000, " +
                    "   gl7kw "  + sDecimalType + "(15,7) default 0.0000000, " +
                    "   vl210 "  + sDecimalType + "(15,7) default 0.0000000, " +
                    "   kf307 "  + sDecimalType + "(15,7) default 0.0000000, " +
                    "   kf307n " + sDecimalType + "(15,7) default 0.0000000, " +
                    "   kf307f9 "     + sDecimalType + "(15,7) default 0.0000000, " +
                    "   kf_dpu_kg "   + sDecimalType + "(15,7) default 0.0000000, " +
                    "   kf_dpu_plob " + sDecimalType + "(15,7) default 0.0000000, " +
                    "   kf_dpu_plot " + sDecimalType + "(15,7) default 0.0000000, " +
                    "   kf_dpu_ls "   + sDecimalType + "(15,7) default 0.0000000, " +
                    "   dlt_in "      + sDecimalType + "(15,7) default 0.0000000, " +
                    "   dlt_cur "     + sDecimalType + "(15,7) default 0.0000000, " +
                    "   dlt_reval "   + sDecimalType + "(15,7) default 0.0000000, " +
                    "   dlt_real_charge " + sDecimalType + "(15,7) default 0.0000000, " +
                    "   dlt_calc " + sDecimalType + "(15,7) default 0.0000000,     " +
                    "   dlt_out "  + sDecimalType + "(15,7) default 0.0000000,     " +
                    "   val_norm_odn " + sDecimalType + "(15,7) default 0.0000000, " +
                    "   is_norm  INTEGER default 0, " +
                    "   kod_info INTEGER default 0," +
                    "   i21      INTEGER default 0," +

                    "   nzp_serv_l1 INTEGER default 0 NOT NULL,               " +
                    "   squ1_l1 " + sDecimalType + "(15,7) default 0.0000000, " +
                    "   squ2_l1 " + sDecimalType + "(15,7) default 0.0000000, " +
                    "   cls1_l1 INTEGER default 0 NOT NULL, " +
                    "   cls2_l1 INTEGER default 0 NOT NULL, " +
                    "   gil1_l1 " + sDecimalType + "(15,7) default 0.0000000, " +
                    "   gil2_l1 " + sDecimalType + "(15,7) default 0.0000000, " +
                    "   val1_l1 " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                    "   val2_l1 " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                    "   dlt_reval_l1 "       + sDecimalType + "(15,7) default 0.0000000, " +
                    "   dlt_real_charge_l1 " + sDecimalType + "(15,7) default 0.0000000, " +

                    "   nzp_serv_l2 INTEGER default 0 NOT NULL,               " +
                    "   squ1_l2 " + sDecimalType + "(15,7) default 0.0000000, " +
                    "   squ2_l2 " + sDecimalType + "(15,7) default 0.0000000, " +
                    "   cls1_l2 INTEGER default 0 NOT NULL, " +
                    "   cls2_l2 INTEGER default 0 NOT NULL, " +
                    "   gil1_l2 " + sDecimalType + "(15,7) default 0.0000000, " +
                    "   gil2_l2 " + sDecimalType + "(15,7) default 0.0000000, " +
                    "   val1_l2 " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                    "   val2_l2 " + sDecimalType + "(15,7) default 0.0000000 NOT NULL, " +
                    "   dlt_reval_l2 "       + sDecimalType + "(15,7) default 0.0000000, " +
                    "   dlt_real_charge_l2 " + sDecimalType + "(15,7) default 0.0000000, " +

                    "   rash_link "          + sDecimalType + "(15,7) default 0.0000000  " +
#if PG
                    " ) "
#else
                    " ) with no log "
#endif
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                    " insert into ttt_aid_kpu (nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                          " dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                          " cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info " +
                          " ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, dlt_cur, dlt_reval, dlt_real_charge) " +
                    " Select nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                          " dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                          " cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info " +
                          " ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, dlt_cur, dlt_reval, dlt_real_charge " +
                          " From " + rashod.counters_xx +
                    " Where nzp_type = 1 and stek = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                    " and nzp_serv in (6,8,9,25,10,210,410) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " create index ix_ttt_aid_kpu on ttt_aid_kpu (nzp_dom,nzp_serv) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_kpu ", true);

                #endregion выбрать домовые расходы для расчета ОДН

                #region добавить расходы по связным услугам для расчета ОДН

                ExecSQL(conn_db, " Drop table ttt_aid_kpux ", false);

                ret = ExecSQL(conn_db,
                    " Select nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                          " dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                          " cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info " +
                          " ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, dlt_cur, dlt_reval, dlt_real_charge " +
                          " ,1 as i21" +
#if PG
                    " into temp ttt_aid_kpux " +
#else
                    " " +
#endif
                    " From ttt_aid_kpu " +
                    " Where nzp_serv in (6,25, 9,210,410) " +
#if PG
                    " "
#else
                    " into temp ttt_aid_kpux with no log "
#endif              
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " create index ix_ttt_aid_kpux on ttt_aid_kpux (nzp_dom,nzp_serv) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_kpux ", true);

                // для ЭЭ - заготовка для nzp_type_alg=21 с вычетом ЭЭ ночного(nzp_serv=210) и полупикового(nzp_serv=410)
                ret = ExecSQL(conn_db,
                    " update ttt_aid_kpu set " +
                    " (nzp_serv_l1,val1_l1,val2_l1,squ1_l1,squ2_l1,gil1_l1,gil2_l1,cls1_l1,cls2_l1,dlt_reval_l1,dlt_real_charge_l1) = (" +
                      " ( Select v.nzp_serv        From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210 )," +
                      " ( Select v.val1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210 )," +
                      " ( Select v.val2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210 )," +
                      " ( Select v.squ1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210 )," +
                      " ( Select v.squ2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210 )," +
                      " ( Select v.gil1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210 )," +
                      " ( Select v.gil2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210 )," +
                      " ( Select v.cls1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210 )," +
                      " ( Select v.cls2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210 )," +
                      " ( Select v.dlt_reval       From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210 )," +
                      " ( Select v.dlt_real_charge From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210 ) " +
                    ") where nzp_serv=25" +
                          " and 0<(select count(*) from ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=210) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // для ЭЭ - заготовка для nzp_type_alg=21 с вычетом ЭЭ ночного(nzp_serv=210) и полупикового(nzp_serv=410)
                ret = ExecSQL(conn_db,
                    " update ttt_aid_kpu set " +
                    " (nzp_serv_l2,val1_l2,val2_l2,squ1_l2,squ2_l2,gil1_l2,gil2_l2,cls1_l2,cls2_l2,dlt_reval_l2,dlt_real_charge_l2) = (" +
                      " ( Select v.nzp_serv        From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410 )," +
                      " ( Select v.val1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410 )," +
                      " ( Select v.val2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410 )," +
                      " ( Select v.squ1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410 )," +
                      " ( Select v.squ2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410 )," +
                      " ( Select v.gil1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410 )," +
                      " ( Select v.gil2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410 )," +
                      " ( Select v.cls1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410 )," +
                      " ( Select v.cls2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410 )," +
                      " ( Select v.dlt_reval       From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410 )," +
                      " ( Select v.dlt_real_charge From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410 ) " +
                    ") where nzp_serv=25" +
                          " and 0<(select count(*) from ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=410) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // для ХВС - заготовка для nzp_type_alg=26 с вычетом ГВС
                ret = ExecSQL(conn_db,
                    " update ttt_aid_kpu set " +
                    " (nzp_serv_l1,val1_l1,val2_l1,squ1_l1,squ2_l1,gil1_l1,gil2_l1,cls1_l1,cls2_l1,dlt_reval_l1,dlt_real_charge_l1) = (" +
                      " ( Select v.nzp_serv        From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9 )," +
                      " ( Select v.val1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9 )," +
                      " ( Select v.val2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9 )," +
                      " ( Select v.squ1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9 )," +
                      " ( Select v.squ2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9 )," +
                      " ( Select v.gil1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9 )," +
                      " ( Select v.gil2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9 )," +
                      " ( Select v.cls1            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9 )," +
                      " ( Select v.cls2            From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9 )," +
                      " ( Select v.dlt_reval       From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9 )," +
                      " ( Select v.dlt_real_charge From ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9 ) " +
                    ") where nzp_serv=6" +
                          " and 0<(select count(*) from ttt_aid_kpux v where v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=9) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                    " update ttt_aid_kpu set " +
                    " rash_link = val1_l1 + val2_l1 + dlt_reval_l1 + dlt_real_charge_l1 + " +
                                 "val1_l2 + val2_l2 + dlt_reval_l2 + dlt_real_charge_l2" +
                    " where nzp_serv in (6,25) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // удвоим ХВС и ЭЭ с i21=1 ! - заготовка для nzp_type_alg=21 (только ХВС) и 22 (только дневное ЭЭ)
                ret = ExecSQL(conn_db,
                    " insert into ttt_aid_kpu" +
                         " ( nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                          " dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                          " cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info " +
                          " ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, dlt_cur, dlt_reval, dlt_real_charge " +
                          " ,i21 ) " +
                    " Select nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                          " dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                          " cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info " +
                          " ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, dlt_cur, dlt_reval, dlt_real_charge " +
                          " ,i21 " +
                    " From ttt_aid_kpux " +
                    " Where nzp_serv in (6,25) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                /*
                sql =
                    " select nzp_dom, nzp_kvar, nzp_type, rash_link," +
                    " nzp_serv_l1,squ1_l1,squ2_l1,cls1_l1,cls2_l1,gil1_l1,gil2_l1,val1_l1,val2_l1,dlt_reval_l1,dlt_real_charge_l1 " +
                    " from ttt_aid_kpu where nzp_serv=25";
                DataTable DtTbl = new DataTable();
                DtTbl = ViewTbl(conn_db, sql);
                */

                #endregion добавить расходы по связным услугам для расчета ОДН

                #region нормативный расход ОДН по услугам

                // вставить норматив ОДН для домов без ОДПУ по Пост.354
                ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);
                ret = ExecSQL(conn_db,
                      " Create temp table ttt_aid_virt0 " +
                      " ( nzp_dom  integer, " +
                      "   nzp_serv integer, " +
                      "   cnt_stage integer, " +
                      "   squ_dom  " + sDecimalType + "(15,7) default 0.0000000," +
                      "   squ1     " + sDecimalType + "(15,7) default 0.0000000," +
                      "   squ2     " + sDecimalType + "(15,7) default 0.0000000," +
                      "   squ_mop  " + sDecimalType + "(15,7) default 0.0000000," +
                      "   norm_odn " + sDecimalType + "(15,7) default 0.0000000," +
                      "   dat_post date, " +
                      "   is_hvobor   integer    default 0, " +
                      "   is_liftobor integer    default 0, " +
                      "   is_obor     integer    default 0, " +
                      "   etag     integer       default 1, " +
                      "   nom_str  integer       default 1, " +
                      "   nom_kol  integer       default 1, " +
                      "   is_odn   integer       default 0, " +
                      "   val3     " + sDecimalType + "(15,7) default 0.0000" +
#if PG
                      " ) "
#else
                      " ) With no log"
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // взять ОДПУ без показаний - из-за Самары рассчитать норматив для всех домов
                ret = ExecSQL(conn_db,
                    " Insert into ttt_aid_virt0 (nzp_dom,nzp_serv,cnt_stage,squ1,squ2) " +
                    " Select nzp_dom, nzp_serv, max(cnt_stage), max(squ1), max(squ1) From ttt_aid_kpu Where 1=1" +
                    " group by 1,2 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ExecSQL(conn_db, " Create index ix_ttt_aid_virt1 on ttt_aid_virt0 (nzp_dom,nzp_serv) ", true);
                ExecSQL(conn_db, " Create index ix_ttt_aid_virt2 on ttt_aid_virt0 (is_odn,nzp_serv,dat_post) ", true);
                ExecSQL(conn_db, " Create index ix_ttt_aid_virt3 on ttt_aid_virt0 (cnt_stage) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);

                if (Points.IsSmr)
                {
                    // Для Самары

                    // проставить домовые параметры для расчета норматива ОДН - пока только электроэнергия!
                    ret = ExecSQL(conn_db,
                          " update ttt_aid_virt0 set (squ_dom,squ_mop,nom_str)= "+
#if PG
                          "((Select max( case when nzp_prm=  40 then coalesce(val_prm,'0')::numeric else 0 end ) as squ From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp " +
                          "   and p.nzp_prm in (40,2049,2050) " +
                          " ),"+
                          "(Select max( case when nzp_prm=2049 then coalesce(val_prm,'0')::numeric else 0 end ) as squ_mop From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp " +
                          "   and p.nzp_prm in (40,2049,2050) " +
                          " ),"+
                          "(Select max( case when nzp_prm=2050 then coalesce(val_prm,'0')::numeric else 0 end ) as nom_str" +
                          " From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp " +
                          "   and p.nzp_prm in (40,2049,2050) " +
                          " ))" +
#else
                          "((Select " +
                          "   max( case when nzp_prm=  40 then nvl(val_prm,'0')+0 else 0 end ) as squ," +
                          "   max( case when nzp_prm=2049 then nvl(val_prm,'0')+0 else 0 end ) as squ_mop," +
                          "   max( case when nzp_prm=2050 then nvl(val_prm,'0')+0 else 0 end ) as nom_str" +
                          " From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp " +
                          "   and p.nzp_prm in (40,2049,2050) " +
                          " ))"+
#endif
                          " where 1=1 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // ОДН есть если есть площадь МОП = общая площадь дома - сумма площадей ЛС
                    ret = ExecSQL(conn_db,
                          " update ttt_aid_virt0 set is_odn=1,squ1=squ_dom-squ_mop where squ_dom-squ_mop>0.000001 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);
                    // норматив ОДН по электроснабжению
                    ret = ExecSQL(conn_db,
                          " Update ttt_aid_virt0 set norm_odn = (select r.value from " + rashod.paramcalc.kernel_alias + "res_values r " +
                             "   where r.nzp_res=3010 and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=1 )" + sConvToNum +
                          " Where is_odn=1 and nzp_serv=25 and 0<(select count(*) from " + rashod.paramcalc.kernel_alias + "res_values r " +
                             "   where r.nzp_res=3010 and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=1 ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // норматив ОДН по ХВС и ГВС
                    ret = ExecRead(conn_db, out reader,
                    " Select val_prm" + sConvToInt.Trim() + " val_prm From " + rashod.paramcalc.data_alias + "prm_13 " +
                    " Where nzp_prm = 185 and is_actual <> 100 and dat_s  <= " + rashod.paramcalc.dat_po + " and dat_po >= " + rashod.paramcalc.dat_s
                    , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    iNzpRes = 0;
                    if (reader.Read())
                    {
                        iNzpRes = Convert.ToInt32(reader["val_prm"]);
                    }
                    reader.Close();

                    if (iNzpRes != 0)
                    {
                        sql =
                        " Update ttt_aid_virt0 set norm_odn = (select r.value from " + rashod.paramcalc.kernel_alias + "res_values r " +
                        "   where r.nzp_res=" + iNzpRes + " and r.nzp_y=1 and r.nzp_x=(case when nzp_serv=6 then 1 else 2 end) )" + sConvToNum +
                        " Where is_odn=1 and nzp_serv in (6,9) and 0<(select count(*) from " + rashod.paramcalc.kernel_alias + "res_values r " +
                        "   where r.nzp_res=" + iNzpRes + " and r.nzp_y=1 and r.nzp_x=(case when nzp_serv=6 then 1 else 2 end) ) ";
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result)
                        {
                            DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                            return false;
                        }
                    }
                }
                else
                {
                    // Для РТ

                    // проставить домовые параметры для расчета норматива ОДН
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set (etag,squ_dom,dat_post,is_hvobor,is_liftobor,is_obor)= "+
#if PG
                        "((Select max( case when nzp_prm=  37 then replace( coalesce(val_prm,'0'), ',', '.')" + sConvToInt.Trim() + " else 0 end ) as etg From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm in (37,40,150,1080,1081,1082) ),"+
                          "(Select max( case when nzp_prm=  40 then replace( coalesce(val_prm,'0'), ',', '.')" + sConvToNum.Trim() + " else 0 end ) as squ From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm in (37,40,150,1080,1081,1082) ),"+
                          " cast((Select max( case when nzp_prm= 150 then replace( coalesce(val_prm,'0'), ',', '.')   else '' end ) as dpost From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp  and p.nzp_prm in (37,40,150,1080,1081,1082) ) as date),"+
                          "(Select max( case when nzp_prm=1080 then replace( coalesce(val_prm,'0'), ',', '.')" + sConvToInt.Trim() + " else 0 end ) as hvobor From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm in (37,40,150,1080,1081,1082) ),"+
                          "(Select max( case when nzp_prm=1081 then replace( coalesce(val_prm,'0'), ',', '.')" + sConvToInt.Trim() + " else 0 end ) as liftobor From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp  and p.nzp_prm in (37,40,150,1080,1081,1082) ),"+
                          "(Select max( case when nzp_prm=1082 then replace( coalesce(val_prm,'0'), ',', '.')" + sConvToInt.Trim() + " else 0 end ) as obor From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm in (37,40,150,1080,1081,1082) " +
                        " )) " +
#else
                        "((Select " +
                          "   max( case when nzp_prm=  37 then replace( nvl(val_prm,'0'), ',', '.')+0 else 0 end ) as etg," +
                          "   max( case when nzp_prm=  40 then replace( nvl(val_prm,'0'), ',', '.')+0 else 0 end ) as squ," +
                          "   max( case when nzp_prm= 150 then replace( nvl(val_prm,'0'), ',', '.')   else '' end ) as dpost," +
                          "   max( case when nzp_prm=1080 then replace( nvl(val_prm,'0'), ',', '.')+0 else 0 end ) as hvobor," +
                          "   max( case when nzp_prm=1081 then replace( nvl(val_prm,'0'), ',', '.')+0 else 0 end ) as liftobor," +
                          "   max( case when nzp_prm=1082 then replace( nvl(val_prm,'0'), ',', '.')+0 else 0 end ) as obor" +
                          " From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp " +
                          "   and p.nzp_prm in (37,40,150,1080,1081,1082) " +
                        " )) "+
#endif
                        " where 1=1 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // площадь МОП = параметр дома "Площадь МОП..."
                    // ... для эл/энергии
                    ret = ExecSQL(conn_db,
                          " update ttt_aid_virt0 set is_odn=1, squ_mop = "+
                          " (Select max( replace( " + sNvlWord + "(val_prm,'0'), ',', '.')" + sConvToNum.Trim() + " ) as squ " +
                          " From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=2049 " +
                          " )" +
                          " where nzp_serv = 25 " +
                          " and 0<(Select count(*) From ttt_prm_2 p Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=2049 ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    // ... для ХВС
                    ret = ExecSQL(conn_db,
                          " update ttt_aid_virt0 set is_odn=1, squ_mop = "+
                          " (Select max( replace( " + sNvlWord + "(val_prm,'0'), ',', '.')" + sConvToNum.Trim() + " ) as squ " +
                          " From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=2474 " +
                          " ) " +
                          "where nzp_serv = 6 " +
                          " and 0<(Select count(*) From ttt_prm_2 p Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=2474 ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    // ... для ГВС
                    ret = ExecSQL(conn_db,
                          " update ttt_aid_virt0 set is_odn=1, squ_mop = "+
                          " (Select max( replace( " + sNvlWord + "(val_prm,'0'), ',', '.')" + sConvToNum.Trim() + " ) as squ " +
                          " From ttt_prm_2 p " +
                          " Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=2475 " +
                          " ) " +
                          " where nzp_serv = 9 " +
                          " and 0<(Select count(*) From ttt_prm_2 p Where ttt_aid_virt0.nzp_dom = p.nzp and p.nzp_prm=2475 ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    // ОДН есть если есть площадь МОП = общая площадь дома - сумма площадей ЛС и НЕ был установлен параметр дома "Площадь МОП..."
                    ret = ExecSQL(conn_db,
                          " update ttt_aid_virt0 set is_odn=1, squ_mop = squ_dom - squ1 where squ_dom-squ1>0.000001 and is_odn=0 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);
                    // nzp_x=1 для ХВС
                    // nzp_x=2 для ГВС
                    ret = ExecSQL(conn_db,
                          " update ttt_aid_virt0 set nom_kol=2 where is_odn=1 and nzp_serv=9 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // для Отопления: nzp_x=1  для даты постройки < 1999; nzp_x=2  для даты постройки >= 1999
                    ret = ExecSQL(conn_db,
                          " update ttt_aid_virt0 set nom_kol=2 where is_odn=1 and nzp_serv=8 and dat_post>=" + " cast ('01.01.1999' as date) "
                      
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // для Эл/эн: nzp_x=1..5  по лифт.оборуд./ХВ обруд./Оборуд.для э/э
                    ret = ExecSQL(conn_db,
                          " update ttt_aid_virt0 set nom_kol= " +
                          " case when is_hvobor+is_liftobor=2 then 5 else " +
                          "   case when is_hvobor+is_liftobor=1" +
                          "     then " +
                          "       case when is_hvobor=1 then 3 else 4 end " +
                          "     else " +
                          "       case when is_obor  =1 then 2 else 1 end " +
                          "   end " +
                          " end " +
                          " where is_odn=1 and nzp_serv=25 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // для Отопления: nzp_y(этаж)<=16 для остальных услуг nzp_y<=10
                    ret = ExecSQL(conn_db,
                          " update ttt_aid_virt0 set nom_str = " +
                          "   case when etag<=0 then 1 else " +
                          "     case when nzp_serv=8" +
                          "       then " +
                          "         case when etag>16 then 16 else etag end " +
                          "       else " +
                          "         case when etag>10 then 10 else etag end " +
                          "     end " +
                          "   end " +
                          " where is_odn=1 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // проставить норматив ОДН
                    ret = ExecSQL(conn_db,
                          " update ttt_aid_virt0 set norm_odn= (select r.value" + sConvToNum + " from " + rashod.paramcalc.pref +
                          sKernelAliasRest+ "res_values r " +
                          " where r.nzp_res= " +                         
                           "  (select max(val_prm)" + sConvToNum + " from " + rashod.paramcalc.pref +
                               sDataAliasRest+ "prm_13 p " +
                          "    Where p.nzp_prm=" +
                          "    (case when ttt_aid_virt0.nzp_serv in (6,9) then 185 else" +
                          "       case when ttt_aid_virt0.nzp_serv=8 then 186 else 184 end " +
                          "     end) " +
                          "    and p.is_actual <> 100 " +
                          "    and p.dat_s  <= " + rashod.paramcalc.dat_po +
                          "    and p.dat_po >= " + rashod.paramcalc.dat_s +
                          "  )" +
                          " and r.nzp_y=ttt_aid_virt0.nom_str and r.nzp_x=ttt_aid_virt0.nom_kol ) " +
                          " where is_odn=1 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                }
                #endregion нормативный расход ОДН по услугам

                #region расчет коэффициентов коррекции расхода на ОДН

                // рассчитать нормативный расход ОДН по услугам
                ret = ExecSQL(conn_db,
                      " update ttt_aid_virt0 set val3 = squ_mop * norm_odn where is_odn = 1 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                //ViewTbl(conn_db, " select * from ttt_aid_virt0 ");

                // перенести рассчитанный норматив ОДН на дом
                ret = ExecSQL(conn_db,
                      " update ttt_aid_kpu set is_norm=1," +
                      " (val_norm_odn,kf307f9,squ1,squ2,pu7kw,vl210,kf_dpu_kg)="+
#if PG
                    "((" +
                      " select v.val3 from ttt_aid_virt0 v" +
                      " where v.is_odn=1 and v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv " +
                      " ),"+
                      "(select v.squ_dom from ttt_aid_virt0 v" +
                      " where v.is_odn=1 and v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv " +
                      " ),"+
                      "(select v.squ1 from ttt_aid_virt0 v" +
                      " where v.is_odn=1 and v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv " +
                      " )," +
                      "(select v.squ2 from ttt_aid_virt0 v" +
                      " where v.is_odn=1 and v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv " +
                      " )," +
                      "(select v.squ1 from ttt_aid_virt0 v" +
                      " where v.is_odn=1 and v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv " +
                      " )," +
                      "(select v.norm_odn from ttt_aid_virt0 v" +
                      " where v.is_odn=1 and v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv " +
                      " )," +
                      " (select v.squ_mop from ttt_aid_virt0 v" +
                      " where v.is_odn=1 and v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv " +
                      " ))" +
#else
                    "((" +
                      " select v.val3,v.squ_dom,v.squ1,v.squ2,v.squ1,v.norm_odn,v.squ_mop" +
                      " from ttt_aid_virt0 v" +
                      " where v.is_odn=1 and v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv " +
                      " ))" +
#endif
                      " where 0<(select count(*) from ttt_aid_virt0 v" +
                        " where v.is_odn=1 and v.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                //ViewTbl(conn_db, " select * from ttt_aid_kpu ");

                // ОДН есть если есть площадь МОП = общая площадь дома - сумма площадей ЛС 
                // - без ОДПУ - коэф-ты коррекции рассчитаны сразу!
                ret = ExecSQL(conn_db,
                      " update ttt_aid_kpu set kod_info=" +
                           " case when nzp_serv=6 and i21=0" +
                           " then 26" +
                           " else " +
                              "(case when (nzp_serv=25 and i21=1) or (nzp_serv in (210,410))" +
                              " then 22 else 21" +
                              " end)" +
                           " end," +
                      // ОДН без обрезания нормативом по Пост.344
                      " dlt_cur   = val_norm_odn," +
                      " kf_dpu_kg = val_norm_odn," +
                      // ОДН норматив по Пост.344
                      " kf_dpu_ls = val_norm_odn," +
                      // домовой расход обрезанный нормативом по Пост.344
                      " val3 = val_norm_odn + val1 + val2 + dlt_reval + dlt_real_charge + rash_link," +
                      // полный домовой расход без обрезания нормативом по Пост.344
                      " val4 = val_norm_odn + val1 + val2 + dlt_reval + dlt_real_charge + rash_link," +
                      // норма ОДН на 1 кв.м общей площади - домовой расход обрезанный нормативом по Пост.344
                      " kf307  = case when squ1>0.000001 then val_norm_odn / squ1 else 0 end," +
                      " kf307n = case when squ1>0.000001 then val_norm_odn / squ1 else 0 end," +
                      // норма ОДН на 1 кв.м общей площади - норматив по Пост.344
                      " gl7kw  = case when squ1>0.000001 then val_norm_odn / squ1 else 0 end," +
                      // норма ОДН на 1 кв.м общей площади - полный домовой расход без обрезания нормативом по Пост.344
                      " kf_dpu_plob = case when squ1>0.000001 then val_norm_odn / squ1 else 0 end " +
                      " where cnt_stage=0 and is_norm=1 and nzp_serv<>8 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                //ViewTbl(conn_db, " select * from ttt_aid_kpu ");

                // - с ОДПУ
                ret = ExecSQL(conn_db,
                      " update ttt_aid_kpu set " +
                      " dlt_cur   = val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link)," +
                      " kf_dpu_kg = val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link)," +
                      " kf_dpu_ls = val_norm_odn," +
                      " rvirt = val_norm_odn + val1 + val2 + dlt_reval + dlt_real_charge + rash_link," +
                      " val4  = val3," +
                      " gl7kw = case when squ1>0.000001 then val_norm_odn / squ1 else 0 end, " +
                      " kf_dpu_plob   =" +
                       "( case when (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))>0.000001" +
                        " then (case when squ1>0.000001" +
                              " then (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))/squ1 else 0 end)" +
                        " else (case when gil1>0.000001 and (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))<-0.000001" +
                              " then (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))/gil1 else 0 end)" +
                        " end ) " +
                      " where cnt_stage=1 and is_norm=1 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                //ViewTbl(conn_db, " select * from ttt_aid_kpu ");

                if ((Convert.ToDateTime("01." + rashod.paramcalc.calc_mm + "." + rashod.paramcalc.calc_yy) >= Convert.ToDateTime("01.06.2013"))
                    ) // !!! after'01.06.2013' !!!
                {
                    // обрезание расхода ОДПУ на ОДН по Пост344 если нет признака разрешающего превышение!
                    // ... для ХВС
                    ret = ExecSQL(conn_db,
                          " Update ttt_aid_kpu Set val3 = rvirt " +
                          " Where cnt_stage=1 and nzp_serv = 6 and val3 > rvirt " +
                          " and 0=(Select count(*) From ttt_prm_2 p Where ttt_aid_kpu.nzp_dom = p.nzp and p.nzp_prm=1214 ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    // ... для ГВС
                    ret = ExecSQL(conn_db,
                          " Update ttt_aid_kpu Set val3 = rvirt " +
                          " Where cnt_stage=1 and nzp_serv = 9 and val3 > rvirt " +
                          " and 0=(Select count(*) From ttt_prm_2 p Where ttt_aid_kpu.nzp_dom = p.nzp and p.nzp_prm=1215 ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    // ... для эл/энергии
                    ret = ExecSQL(conn_db,
                          " Update ttt_aid_kpu Set val3 = rvirt " +
                          " Where cnt_stage=1 and nzp_serv =25 and val3 > rvirt " +
                          " and 0=(Select count(*) From ttt_prm_2 p Where ttt_aid_kpu.nzp_dom = p.nzp and p.nzp_prm=1216 ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    // ... для газа
                    ret = ExecSQL(conn_db,
                          " Update ttt_aid_kpu Set val3 = rvirt " +
                          " Where cnt_stage=1 and nzp_serv =10 and val3 > rvirt " +
                          " and 0=(Select count(*) From ttt_prm_2 p Where ttt_aid_kpu.nzp_dom = p.nzp and p.nzp_prm=1217 ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                }

                //ViewTbl(conn_db, " select * from ttt_aid_kpu ");

                //  проставить коэффициенты коррекции расходов по Пост.№354 для домов с ОДПУ! если нет ОДПУ коэф-ты уже вычислены!
                ret = ExecSQL(conn_db,
                      " update ttt_aid_kpu set " +
                      " kf307   =( case when (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))>0.000001" +
                                      " then (case when squ1>0.000001" +
                                            " then (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link)) / squ1 else 0 end)" +
                                      " else (case when gil1>0.000001 and (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))<-0.000001" +
                                            " then (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link)) / gil1 else 0 end)" +
                                 " end )," +
                      " kf307n  =( case when (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))>0.000001" +
                                      " then (case when squ1>0.0001" +
                                            " then (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link)) / squ1 else 0 end)" +
                                      " else (case when gil1>0.000001 and (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))<-0.000001" +
                                            " then (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link)) / gil1 else 0 end)" +
                                 " end )," +
                      " kod_info=" +

                                "( case when (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))>0.000001" +
                                      " then " +
                                         
                                             " case when nzp_serv=6 and i21=0" +
                                             " then 26" +
                                             " else " +
                                                "(case when (nzp_serv=25 and i21=1) or (nzp_serv in (210,410))" +
                                                " then 22 else 21" +
                                                " end)" +
                                             " end" +

                                      " else (case when (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))<-0.000001" +
                                            " then" +
                                            
                                             " case when nzp_serv=6 and i21=0" +
                                             " then 31" +
                                             " else " +
                                                "(case when (nzp_serv=25 and i21=1) or (nzp_serv in (210,410))" +
                                                " then 32 else 31" +
                                                " end)" +
                                             " end" +
                                            
                                            " else 0 end)" +
                                      " end )" +


                      " where cnt_stage=1 and nzp_serv in (6,9,25,10,210,410) "
                      , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                //
                ret = ExecSQL(conn_db,
                      " update ttt_aid_kpu set " +
                      " kf307   =( case when abs(val3 - (val1 + val2 + dlt_reval + dlt_real_charge))>0.000001 and squ1>0.000001" +
                                        " then  (val3 - (val1 + val2 + dlt_reval + dlt_real_charge)) / squ1 else 0 end )," +
                      " kf307n  =( case when abs(val3 - (val1 + val2 + dlt_reval + dlt_real_charge))>0.000001 and squ1>0.000001" +
                                        " then  (val3 - (val1 + val2 + dlt_reval + dlt_real_charge)) / squ1 else 0 end )," +
                      " kod_info=( case when    (val3 - (val1 + val2 + dlt_reval + dlt_real_charge))>0.000001" +
                              " then 23" +
                              " else (case when (val3 - (val1 + val2 + dlt_reval + dlt_real_charge))<-0.000001 then 33 else 0 end)" +
                              " end )" +
                      " where cnt_stage=1 and nzp_serv in (8) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                #endregion расчет коэффициентов коррекции расхода на ОДН

                #region обработка домов с суммарным начислением ОДН (секционных)

                ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

                ret = ExecSQL(conn_db,
                    " Create temp table ttt_aid_c1 " +
                    //" Create table are.ttt_aid_c1 " +
                    " ( nzp_dom_base integer," +
                    "   nzp_serv     integer," +
                    "   kf307     " + sDecimalType + "(15,7) default 0.00," +
                    "   kf307n    " + sDecimalType + "(15,7) default 0.00," +
                    "   kf307f9   " + sDecimalType + "(15,7) default 0.00," +
                    "   kod_info  INTEGER default 0,         " +
                    "   cnt_stage INTEGER default 0,         " +
                    "   dlt_cur   " + sDecimalType + "(15,7) default 0.0000," +
                    "   dlt_reval " + sDecimalType + "(15,7) default 0.0000," +
                    "   dlt_real_charge " + sDecimalType + "(12,4) default 0.0000," +
                    "   squ1 " + sDecimalType + "(15,7) default 0.0000, " +
                    "   gil1 " + sDecimalType + "(15,7) default 0.0000, " +
                    "   val1 " + sDecimalType + "(15,7) default 0.0000," +
                    "   val2 " + sDecimalType + "(15,7) default 0.0000," +
                    "   val3 " + sDecimalType + "(15,7) default 0.0000," +
                    "   val4 " + sDecimalType + "(15,7) default 0.0000," +
                    "   rvirt " + sDecimalType + "(15,7) default 0.0000," +
                    "   gl7kw " + sDecimalType + "(15,7) default 0.0000000,      " +
                    "   kf_dpu_kg " + sDecimalType + "(15,7) default 0.0000000,  " +
                    "   kf_dpu_plob " + sDecimalType + "(15,7) default 0.0000000," +
                    "   kf_dpu_ls " + sDecimalType + "(15,7) default 0.0000000,  " +

                    "   i21      INTEGER default 0," +
                    "   rash_link " + sDecimalType + "(15,7) default 0.0000000   " +
#if PG
                    " )  "
#else
                    //" ) "
                    " ) With no log "
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // hvs (помним! - по ХВ есть дублирование по домам 21 и 22 типы)
                ret = ExecSQL(conn_db,
                    " insert into ttt_aid_c1" +
                    " (nzp_dom_base,i21,nzp_serv,squ1,gil1,val1,val2,val3,val4,dlt_reval,dlt_real_charge,kf_dpu_kg,kf_dpu_ls,rvirt,cnt_stage,rash_link) " +
                    " select l.nzp_dom_base,d.i21, 6 nzp_serv,sum(d.squ1),sum(d.gil1),sum(d.val1),sum(d.val2),sum(d.val3),sum(d.val4)," +
                    " sum(d.dlt_reval),sum(d.dlt_real_charge),sum(d.kf_dpu_kg),sum(d.kf_dpu_ls),sum(d.rvirt),max(d.cnt_stage),sum(rash_link) " +
                    " from " + rashod.paramcalc.data_alias + "link_dom_lit l, ttt_aid_kpu d " +
                    " where d.nzp_serv= 6 and l.nzp_dom=d.nzp_dom and l.nzp_dom_base in " +
                      "(select b.nzp_dom_base" +
                      " from " + rashod.paramcalc.data_alias + "link_dom_lit b, ttt_prm_2 p " +
                      " where b.nzp_dom=p.nzp and p.nzp_prm=2069 and p.val_prm='1') " +
                    " group by 1,2 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // gvs
                ret = ExecSQL(conn_db,
                    " insert into ttt_aid_c1" +
                    " (nzp_dom_base,nzp_serv,squ1,gil1,val1,val2,val3,val4,dlt_reval,dlt_real_charge,kf_dpu_kg,kf_dpu_ls,rvirt,cnt_stage,rash_link) " +
                    " select l.nzp_dom_base, 9 nzp_serv,sum(d.squ1),sum(d.gil1),sum(d.val1),sum(d.val2),sum(d.val3),sum(d.val4)," +
                    " sum(d.dlt_reval),sum(d.dlt_real_charge),sum(d.kf_dpu_kg),sum(d.kf_dpu_ls),sum(d.rvirt),max(d.cnt_stage),sum(rash_link) " +
                    " from " + rashod.paramcalc.data_alias + "link_dom_lit l, ttt_aid_kpu d " +
                    " where d.nzp_serv= 9 and l.nzp_dom=d.nzp_dom and l.nzp_dom_base in " +
                      "(select b.nzp_dom_base" +
                      " from " + rashod.paramcalc.data_alias + "link_dom_lit b, ttt_prm_2 p " +
                      " where b.nzp_dom=p.nzp and p.nzp_prm=2070 and p.val_prm='1') " +
                    " group by 1 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // ee (помним! - по ЭЭ есть дублирование по домам 21 и 22 типы)
                ret = ExecSQL(conn_db,
                    " insert into ttt_aid_c1" +
                    " (nzp_dom_base,i21,nzp_serv,squ1,gil1,val1,val2,val3,val4,dlt_reval,dlt_real_charge,kf_dpu_kg,kf_dpu_ls,rvirt,cnt_stage,rash_link) " +
                    " select l.nzp_dom_base,d.i21,25 nzp_serv,sum(d.squ1),sum(d.gil1),sum(d.val1),sum(d.val2),sum(d.val3),sum(d.val4)," +
                    " sum(d.dlt_reval),sum(d.dlt_real_charge),sum(d.kf_dpu_kg),sum(d.kf_dpu_ls),sum(d.rvirt),max(d.cnt_stage),sum(rash_link) " +
                    " from " + rashod.paramcalc.data_alias + "link_dom_lit l, ttt_aid_kpu d " +
                    " where d.nzp_serv=25 and l.nzp_dom=d.nzp_dom and l.nzp_dom_base in " +
                      "(select b.nzp_dom_base" +
                      " from " + rashod.paramcalc.data_alias + "link_dom_lit b, ttt_prm_2 p " +
                      " where b.nzp_dom=p.nzp and p.nzp_prm=2071 and p.val_prm='1') " +
                    " group by 1,2 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // gas
                ret = ExecSQL(conn_db,
                    " insert into ttt_aid_c1" +
                    " (nzp_dom_base,nzp_serv,squ1,gil1,val1,val2,val3,val4,dlt_reval,dlt_real_charge,kf_dpu_kg,kf_dpu_ls,rvirt,cnt_stage,rash_link) " +
                    " select l.nzp_dom_base,10 nzp_serv,sum(d.squ1),sum(d.gil1),sum(d.val1),sum(d.val2),sum(d.val3),sum(d.val4)," +
                    " sum(d.dlt_reval),sum(d.dlt_real_charge),sum(d.kf_dpu_kg),sum(d.kf_dpu_ls),sum(d.rvirt),max(d.cnt_stage),sum(rash_link) " +
                    " from " + rashod.paramcalc.data_alias + "link_dom_lit l, ttt_aid_kpu d " +
                    " where d.nzp_serv=10 and l.nzp_dom=d.nzp_dom and l.nzp_dom_base in " +
                      "(select b.nzp_dom_base" +
                      " from " + rashod.paramcalc.data_alias + "link_dom_lit b, ttt_prm_2 p " +
                      " where b.nzp_dom=p.nzp and p.nzp_prm=2072 and p.val_prm='1') " +
                    " group by 1 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ExecSQL(conn_db, " create index ix1_ttt_ttt_aid_c1 on ttt_aid_c1 (nzp_dom_base,nzp_serv) ", true);
                ExecSQL(conn_db, " create index ix2_ttt_ttt_aid_c1 on ttt_aid_c1 (nzp_serv) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);

                if ((Convert.ToDateTime("01." + rashod.paramcalc.calc_mm + "." + rashod.paramcalc.calc_yy.ToString()) >= Convert.ToDateTime("01.06.2013"))
                    ) // !!! after'01.06.2013' !!!
                {
                    // восстановить необрезанный расход по ОДПУ (ранее был испорчен при расчете отдельно по домам)
                    ret = ExecSQL(conn_db,
                          " Update ttt_aid_c1 Set val3 = val4 " +
                          " Where cnt_stage=1  "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    // обрезание расхода ОДПУ на ОДН по Пост344 если нет признака разрешающего превышение!
                    // ... для ХВС
                    ret = ExecSQL(conn_db,
                          " Update ttt_aid_c1 Set val3 = rvirt " +
                          " Where cnt_stage=1 and nzp_serv = 6 and val3 > rvirt " +
                          " and 0=(Select count(*) From ttt_prm_2 p Where ttt_aid_c1.nzp_dom_base = p.nzp and p.nzp_prm=1214 ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    // ... для ГВС
                    ret = ExecSQL(conn_db,
                          " Update ttt_aid_c1 Set val3 = rvirt " +
                          " Where cnt_stage=1 and nzp_serv = 9 and val3 > rvirt " +
                          " and 0=(Select count(*) From ttt_prm_2 p Where ttt_aid_c1.nzp_dom_base = p.nzp and p.nzp_prm=1215 ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    // ... для эл/энергии
                    ret = ExecSQL(conn_db,
                          " Update ttt_aid_c1 Set val3 = rvirt " +
                          " Where cnt_stage=1 and nzp_serv =25 and val3 > rvirt " +
                          " and 0=(Select count(*) From ttt_prm_2 p Where ttt_aid_c1.nzp_dom_base = p.nzp and p.nzp_prm=1216 ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                    // ... для газа
                    ret = ExecSQL(conn_db,
                          " Update ttt_aid_c1 Set val3 = rvirt " +
                          " Where cnt_stage=1 and nzp_serv =10 and val3 > rvirt " +
                          " and 0=(Select count(*) From ttt_prm_2 p Where ttt_aid_c1.nzp_dom_base = p.nzp and p.nzp_prm=1217 ) "
                        , true);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                }

                //  проставить коэффициенты коррекции расходов по Пост.№354 для домов с суммируемым ОДПУ
                ret = ExecSQL(conn_db,
                      " update ttt_aid_c1 set dlt_cur = val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link)," +
                      " kf307   =( case when (val3-(val1+val2 + dlt_reval + dlt_real_charge + rash_link))>0.0001" +
                                      " then (case when squ1>0.0001 then (val3-(val1+val2 + dlt_reval + dlt_real_charge + rash_link))/squ1 else 0 end)" +
                                      " else (case when gil1>0.0001 and (val3-(val1+val2 + dlt_reval + dlt_real_charge + rash_link))<-0.0001" +
                                            " then (val3-(val1 + val2 + dlt_reval + dlt_real_charge + rash_link))/gil1 else 0 end)" +
                                 " end )," +
                      " kf307n  =( case when (val3-(val1 + val2 + dlt_reval + dlt_real_charge + rash_link))>0.0001" +
                                      " then (case when squ1>0.0001 then (val3-(val1 + val2 + dlt_reval + dlt_real_charge + rash_link))/squ1 else 0 end)" +
                                      " else (case when gil1>0.0001 and (val3-(val1 + val2 + dlt_reval + dlt_real_charge + rash_link))<-0.0001" +
                                            " then (val3-(val1 + val2 + dlt_reval + dlt_real_charge + rash_link))/gil1 else 0 end)" +
                                 " end )," +
                      " kod_info=" +

                                "( case when (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))>0.000001" +
                                      " then " +

                                             " case when nzp_serv=6 and i21=0" +
                                             " then 26" +
                                             " else " +
                                                "(case when (nzp_serv=25 and i21=1) or (nzp_serv in (210,410))" +
                                                " then 22 else 21" +
                                                " end)" +
                                             " end" +

                                      " else (case when (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))<-0.000001" +
                                            " then" +

                                             " case when nzp_serv=6 and i21=0" +
                                             " then 31" +
                                             " else " +
                                                "(case when (nzp_serv=25 and i21=1) or (nzp_serv in (210,410))" +
                                                " then 32 else 31" +
                                                " end)" +
                                             " end" +

                                            " else 0 end)" +
                                      " end )," +
                      " gl7kw = case when squ1>0.000001 then kf_dpu_ls / squ1 else 0 end, " +
                      " kf_dpu_plob   =" +
                       "( case when (val4 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))>0.000001" +
                        " then (case when squ1>0.000001" +
                              " then (val4 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))/squ1 else 0 end)" +
                        " else (case when gil1>0.000001 and (val3 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))<-0.000001" +
                              " then (val4 - (val1 + val2 + dlt_reval + dlt_real_charge + rash_link))/gil1 else 0 end)" +
                        " end ) " +
                      " where 1=1 "
                      , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                sql =
                      " update ttt_aid_kpu set " +
#if PG
                      " val1=(select v.val1 from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.kod_info>0 ) " +
                      ",val2=(select v.val2 from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.kod_info>0 ) " +
                      ",val3=(select v.val3 from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.kod_info>0 ) " +
                      ",val4=(select v.val4 from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.kod_info>0 ) " +
                      ",dlt_reval      =(select v.dlt_reval       from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.kod_info>0 ) " +
                      ",dlt_real_charge=(select v.dlt_real_charge from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.kod_info>0 ) " +
                      ",dlt_cur        =(select v.dlt_cur         from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.kod_info>0 ) " +
                      ",squ1=(select v.squ1 from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.kod_info>0 ) " +
                      ",gil1=(select v.gil1 from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.kod_info>0 ) " +
                      ",kf307 =(select v.kf307  from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.kod_info>0 ) " +
                      ",kf307n=(select v.kf307n from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.kod_info>0 ) " +
                      ",kod_info=(select v.kod_info from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.kod_info>0 ) " +
                      ",kf_dpu_kg  =(select v.kf_dpu_kg   from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.kod_info>0 ) " +
                      ",kf_dpu_ls  =(select v.kf_dpu_ls   from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.kod_info>0 ) " +
                      ",kf_dpu_plob=(select v.kf_dpu_plob from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.kod_info>0 ) " +
                      ",gl7kw=(select v.gl7kw from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.kod_info>0 ) " +
                      ",rvirt=(select v.rvirt from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.kod_info>0 ) " +
#else
                      " (val1,val2,val3,val4,dlt_reval,dlt_real_charge,dlt_cur,squ1,gil1,kf307,kf307n,kod_info,kf_dpu_kg,kf_dpu_ls,kf_dpu_plob,gl7kw,rvirt)=((" +
                       " select v.val1,v.val2,v.val3,v.val4,v.dlt_reval,v.dlt_real_charge,v.dlt_cur,v.squ1,v.gil1,v.kf307,v.kf307n,v.kod_info,v.kf_dpu_kg,v.kf_dpu_ls,v.kf_dpu_plob,v.gl7kw,v.rvirt" +
                       " from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l" +
                       " where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.kod_info>0  " +
                      " ))" +
#endif
                      " where 0<( select count(*)" +
                       " from ttt_aid_c1 v," + rashod.paramcalc.data_alias + "link_dom_lit l" +
                       " where v.nzp_dom_base=l.nzp_dom_base and l.nzp_dom=ttt_aid_kpu.nzp_dom and v.nzp_serv=ttt_aid_kpu.nzp_serv and v.kod_info>0  " +
                      " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                #endregion обработка домов с суммарным начислением ОДН (секционных)

                #region сохранить стек 354 для домов
                if (Points.IsSmr)
                {
                    // для Самары округлить коэф-т коррекции до 4х знаков
                    ret = ExecSQL(conn_db,
                          " update ttt_aid_kpu set " +
                          " kf307   = Round( kf307 * 10000 )/10000," +
                          " kf307n  = Round( kf307n* 10000 )/10000 " +
                          " where kod_info>0 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                }

                //-- вставка в стек 354 --------------------------------------------------------------
                ret = ExecSQL(conn_db,
                      " Insert into " + rashod.counters_xx +
                            " ( nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                            "   dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                            "   cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info " +
                            "   ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, dlt_cur, dlt_reval, dlt_real_charge) " +
                      " Select nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, 354 stek, rashod, " +
                            "  dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                            "  cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info " +
                          //" ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, dlt_cur, dlt_reval, dlt_real_charge " +
                            " ,cls1, cls2, kf_dpu_kg, kf_dpu_plob,   rash_link, kf_dpu_ls, kf307n, kf307f9, dlt_cur, dlt_reval, dlt_real_charge " +
                      " From ttt_aid_kpu where kod_info > 0 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                      " Insert into " + rashod.countlnk_xx +
                            " (nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, stek, kod_info, " +
                              "nzp_serv_l1,val1_l1,val2_l1,squ1_l1,squ2_l1,gil1_l1,gil2_l1,cls1_l1,cls2_l1,dlt_reval_l1,dlt_real_charge_l1," +
                              "nzp_serv_l2,val1_l2,val2_l2,squ1_l2,squ2_l2,gil1_l2,gil2_l2,cls1_l2,cls2_l2,dlt_reval_l2,dlt_real_charge_l2)" +
                      " Select" +
                            "  nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, i21, 354 stek, kod_info, " +
                              "nzp_serv_l1,val1_l1,val2_l1,squ1_l1,squ2_l1,gil1_l1,gil2_l1,cls1_l1,cls2_l1,dlt_reval_l1,dlt_real_charge_l1," +
                              "nzp_serv_l2,val1_l2,val2_l2,squ1_l2,squ2_l2,gil1_l2,gil2_l2,cls1_l2,cls2_l2,dlt_reval_l2,dlt_real_charge_l2 " +
                      " From ttt_aid_kpu where kod_info > 0 and abs(rash_link)>0 and (nzp_serv_l1>0 or nzp_serv_l2>0) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #endregion сохранить стек 354 для домов

                #endregion сохранить стек 3 для домов (nzp_type=1 and stek=3) в стек 354 с расчетом по Пост.№354 (+ нормативы ОДН)

                //----------------------------------------------------------------
                //-- вставка в стек 3540 для бойлеров - самостоятельное приготовление ГВС и Отопления
                //----------------------------------------------------------------
                #region сохранить стек 3 для домов (nzp_type=1 and stek=3) в стек 3540 с расчетом по Пост.№354 - спец.расчет для бойлеров

                ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);
                
                ret = ExecSQL(conn_db,
                    " Select nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, 3540 stek, rashod, " +
                           "   dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                           "   cnt5, dop87, pu7kw, gl7kw, vl210,0 kf307,0 kod_info " +
                            " ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls,0 kf307n,0 kf307f9,0 dlt_cur " +
#if PG
                      " into temp ttt_aid_c1 "+
                      " From ttt_aid_kpu where nzp_serv in (8,9) "
#else
                      " From ttt_aid_kpu where nzp_serv in (8,9) "
                      +" into temp ttt_aid_c1 with no log "
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                ExecSQL(conn_db, " create index ix_ttt_ttt_aid_c1 on ttt_aid_c1 (nzp_dom,nzp_serv) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);

                // проставить домовые параметры для расчета расходов по бойлерам
#if PG
                sql =
                      " update ttt_aid_c1 set kod_info=24," +
                      " val3 = ( Select " +
                      "   max( case when nzp_prm=1104 then replace( " + sNvlWord + "(val_prm,'0'), ',', '.')" + sConvToNum + " else 0 end ) as val3" +
                      " From ttt_prm_2 p Where ttt_aid_c1.nzp_dom = p.nzp and p.nzp_prm in (1104,1105) ) " +
                      ",vl210 = ( Select " +
                      "   max( case when nzp_prm=1105 then replace( " + sNvlWord + "(val_prm,'0'), ',', '.')" + sConvToNum + " else 0 end ) as vl210 " +
                      " From ttt_prm_2 p Where ttt_aid_c1.nzp_dom = p.nzp and p.nzp_prm in (1104,1105) ) " +
                      " where nzp_serv=8 and 0< " +
                      " (Select count(*) " +
                      "  From ttt_prm_2 p Where ttt_aid_c1.nzp_dom = p.nzp and p.nzp_prm in (1104,1105) ) ";
#else
                sql =
                      " update ttt_aid_c1 set kod_info=24,(val3,vl210)= ((Select " +
                      "   max( case when nzp_prm=1104 then replace( " + sNvlWord + "(val_prm,'0'), ',', '.')" + sConvToNum + " else 0 end ) as val3," +
                      "   max( case when nzp_prm=1105 then replace( " + sNvlWord + "(val_prm,'0'), ',', '.')" + sConvToNum + " else 0 end ) as vl210 " +
                      " From ttt_prm_2 p " + 
                      " Where ttt_aid_c1.nzp_dom = p.nzp " +
                      "   and p.nzp_prm in (1104,1105) " +
                      " )) where nzp_serv=8 and 0< " +
                      " (Select count(*) " +
                      "  From ttt_prm_2 p Where ttt_aid_c1.nzp_dom = p.nzp and p.nzp_prm in (1104,1105) ) ";
#endif
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
#if PG
                sql =
                      " update ttt_aid_c1 set kod_info=25," +
                      " val3 = ( Select " +
                        "   max( case when nzp_prm=1106 then replace( coalesce(val_prm,'0'), ',', '.')" + sConvToNum + " else 0 end ) as val3 " +
                      "  From ttt_prm_2 p Where ttt_aid_c1.nzp_dom = p.nzp and p.nzp_prm in (1106,1107) ) " +
                      ",vl210 = ( Select " +
                        "   max( case when nzp_prm=1107 then replace( coalesce(val_prm,'0'), ',', '.')" + sConvToNum + " else 0 end ) as vl210 " +
                      "  From ttt_prm_2 p Where ttt_aid_c1.nzp_dom = p.nzp and p.nzp_prm in (1106,1107) ) " +
                      " where nzp_serv=9 and 0< " +
                      " (Select count(*) " +
                      "  From ttt_prm_2 p Where ttt_aid_c1.nzp_dom = p.nzp and p.nzp_prm in (1106,1107) ) ";
#else
                sql =
                      " update ttt_aid_c1 set kod_info=25,(val3,vl210)= ((Select " +
                        "   max( case when nzp_prm=1106 then replace( nvl(val_prm,'0'), ',', '.')" + sConvToNum + " else 0 end ) as val3," +
                        "   max( case when nzp_prm=1107 then replace( nvl(val_prm,'0'), ',', '.')" + sConvToNum + " else 0 end ) as vl210 " +
                      "  From ttt_prm_2 p Where ttt_aid_c1.nzp_dom = p.nzp and p.nzp_prm in (1106,1107) )) " +
                      " where nzp_serv=9 and 0< " +
                      " (Select count(*) " +
                      "  From ttt_prm_2 p Where ttt_aid_c1.nzp_dom = p.nzp and p.nzp_prm in (1106,1107) ) ";
#endif
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                // расчет расходов по Пост.№354 для бойлеров
                ret = ExecSQL(conn_db,
                      " update ttt_aid_c1 set " +
                      " kf307   =( case when squ1>0.0001 then val3/squ1 else 0 end )," +
                      " kf307n  =( case when squ1>0.0001 then val3/squ1 else 0 end )" +
                      " where kod_info=24 and nzp_serv in (8) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                ret = ExecSQL(conn_db,
                      " update ttt_aid_c1 set " +
                      " kf307   =( case when (val1+val2)>0.0001 then val3/(val1+val2) else 0 end )," +
                      " kf307n  =( case when (val1+val2)>0.0001 then val3/(val1+val2) else 0 end )" +
                      " where kod_info=25 and nzp_serv in (9) "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                //-- вставка в стек 3540 --------------------------------------------------------------
                ret = ExecSQL(conn_db,
                      " Insert into " + rashod.counters_xx +
                            " ( nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod, " +
                            "   dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                            "   cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info " +
                            "   ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, dlt_cur) " +
                      " Select nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, cur_zap, nzp_counter, cnt_stage, mmnog, 3540 stek, rashod, " +
                            "   dat_s, val_s, dat_po, val_po, val1, val2, val3, val4, rvirt, squ1, squ2, gil1, gil2, cnt1, cnt2, cnt3, cnt4, " +
                            "   cnt5, dop87, pu7kw, gl7kw, vl210, kf307, kod_info " +
                            "   ,cls1, cls2, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, kf_dpu_ls, kf307n, kf307f9, dlt_cur " +
                      " From ttt_aid_c1 "
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                //

                ExecSQL(conn_db, " Drop table ttt_aid_dpu ", false);
                ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);
                ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);
                #endregion сохранить стек 3 для домов (nzp_type=1 and stek=3) в стек 3540 с расчетом по Пост.№354 - спец.расчет для бойлеров

                if (bIsCalcODN || bIsCalcCurMonth)
                {

                    #region начинаем бермудид по 307 и 87 постановлениям
                    //----------------------------------------------------------------
                    //начинаем бермудид по 307 и 87 постановлениям
                    //----------------------------------------------------------------

                    #region учет ОДПУ в домах без ИПУ в стек = 3 (пропорция)

                    //--------------------------103
                    //есть ДПУ и нет КПУ
                    //ДПУ в пропорции людей (или площадь на людей для отопления)

                    //ДПУ в пропорции людей
                    ret = ExecSQL(conn_db,
                        " Update " + rashod.counters_xx +
                        " Set kf_dpu_kg = val3 / gil1  " +
                        " Where nzp_type = 1 and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                        "   and nzp_serv <> 8 " +
                        "   and abs(val2)<0.001 and abs(val3)>0 and gil1>0 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    //ДПУ в пропорции сумме общих площадей
                    ret = ExecSQL(conn_db,
                        " Update " + rashod.counters_xx +
                        " Set kf_dpu_plob = val3 / squ1  " +
                        " Where nzp_type = 1 and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                        "   and nzp_serv <> 8 " +
                        "   and abs(val2)<0.001 and abs(val3)>0 and squ1>0 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    // kf_dpu_plot - коэфициент ДПУ для распределения пропорционально сумме отапливаемых площадей
                    ret = ExecSQL(conn_db,
                        " Update " + rashod.counters_xx +
                        " Set kf_dpu_plot = val3 / squ1  " +
                        " Where nzp_type = 1 and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                        "   and nzp_serv = 8 " +
                        "   and abs(val2)<0.001 and abs(val3)>0 and squ1>0 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    //ДПУ в пропорции кол-ву л/с
                    ret = ExecSQL(conn_db,
                        " Update " + rashod.counters_xx +
                        " Set kf_dpu_ls = val3 / cls1  " +
                        " Where nzp_type = 1 and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                        "   and nzp_serv <> 8 " +
                        "   and abs(val2)<0.001 and abs(val3)>0 and cls1>0 "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    //--------------------------109
                    //есть ДПУ и КПУ (+нормативы) - преславутая 9 формула

                    //коэфициент 9 формула
                    ret = ExecSQL(conn_db,
                        " Update " + rashod.counters_xx +
                        " Set kf307f9 = val3 / (val2 + val1 + dlt_reval + dlt_real_charge)  " + //коэфициент
                        " Where nzp_type = 1 and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                        "   and abs(val2)>0.001 and abs(val3)>0.001 and (val1 + val2 + dlt_reval + dlt_real_charge) > 0 "
                        //+ st1
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    #endregion учет ОДПУ в домах без ИПУ в стек = 3 (пропорция)

                    //-------------------------------------------------------------------

                    #region учет ОДН по 354 постановлению в стек = 3

                    //------------------------------------------------------------------
                    // Пост.№ 354
                    //------------------------------------------------------------------
                    if (Constants.Trace) Utility.ClassLog.WriteLog("Пост.№ 354");
                    //-------------------------------------------------------------------
                    // выбрать результат расчета из стека 354 / 3540
                    //-------------------------------------------------------------------
                    ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);
                    ret = ExecSQL(conn_db,
                          " Create temp table ttt_aid_virt0 " +
                          " ( nzp_dom  integer, " +
                          "   nzp_serv integer, " +
                          "   kf307    " + sDecimalType + "(15,7)," +
                          "   kf307n   " + sDecimalType + "(15,7)," +
                          "   kf307f9  " + sDecimalType + "(15,7)," +
                          "   val1     " + sDecimalType + "(15,7)," +
                          "   val2     " + sDecimalType + "(15,7)," +
                          "   squ1     " + sDecimalType + "(15,7), " +
                          "   gil1     " + sDecimalType + "(15,7), " +
                          "   val3     " + sDecimalType + "(15,7)," +
                          "   val4     " + sDecimalType + "(15,7)," +
                          "   vl210    " + sDecimalType + "(15,7)," +
                          "   dlt_cur  " + sDecimalType + "(15,7)," +
                          "   rvirt " + sDecimalType + "(15,7) default 0.0000000,           " +
                          "   pu7kw " + sDecimalType + "(15,7) default 0.0000000,           " +
                          "   gl7kw " + sDecimalType + "(15,7) default 0.0000000,           " +
                          "   kf_dpu_kg   " + sDecimalType + "(15,7) default 0.0000000,       " +
                          "   kf_dpu_plob " + sDecimalType + "(15,7) default 0.0000000,     " +
                          "   kf_dpu_ls   " + sDecimalType + "(15,7) default 0.0000000,       " +
                          "   dlt_reval   " + sDecimalType + "(15,7) default 0.0000000,       " +
                          "   dlt_real_charge " + sDecimalType + "(15,7) default 0.0000000, " +
                          "   kod_info integer," +
                          "   stek     integer," +
                          "   is_uchet integer default 0" +
#if PG
                          " ) "
#else
                          " ) With no log"
#endif
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    ret = ExecSQL(conn_db,
                        " Insert into ttt_aid_virt0 (nzp_dom,nzp_serv,kf307,kf307n,kf307f9,val3,val4,vl210,dlt_cur,kod_info,stek ,val1,val2,squ1,gil1," +
                          " rvirt,pu7kw,gl7kw,kf_dpu_kg,kf_dpu_plob,kf_dpu_ls,dlt_reval,dlt_real_charge)" +
                        " Select nzp_dom,nzp_serv,kf307,kf307n,kf307f9,val3,val4,vl210,dlt_cur,kod_info,stek ,val1,val2,squ1,gil1," +
                          " rvirt,pu7kw,gl7kw,kf_dpu_kg,kf_dpu_plob,kf_dpu_ls,dlt_reval,dlt_real_charge" +
                        " From " + rashod.counters_xx +
                        " where stek=354 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    ret = ExecSQL(conn_db,
                        " Insert into ttt_aid_virt0 (nzp_dom,nzp_serv,kf307,kf307n,kf307f9,val3,val4,vl210,dlt_cur,kod_info,stek ,val1,val2,squ1,gil1," +
                          " rvirt,pu7kw,gl7kw,kf_dpu_kg,kf_dpu_plob,kf_dpu_ls,dlt_reval,dlt_real_charge)" +
                        " Select nzp_dom,nzp_serv,kf307,kf307n,kf307f9,val3,val4,vl210,dlt_cur,kod_info,stek ,val1,val2,squ1,gil1," +
                          " rvirt,pu7kw,gl7kw,kf_dpu_kg,kf_dpu_plob,kf_dpu_ls,dlt_reval,dlt_real_charge" +
                        " From " + rashod.counters_xx +
                        " where stek=3540 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    ExecSQL(conn_db, " Create index ix_ttt_aid_virt1 on ttt_aid_virt0 (nzp_dom,nzp_serv) ", true);
                    ExecSQL(conn_db, " Create index ix_ttt_aid_virt2 on ttt_aid_virt0 (nzp_serv,stek) ", true);
                    ExecSQL(conn_db, " Create index ix_ttt_aid_virt3 on ttt_aid_virt0 (is_uchet) ", true);
                    ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);

                    // HV формула 11 - Пост.№ 354
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set " +
                        " is_uchet = 1" +
                        " Where nzp_serv=6 and stek=354 and kod_info in (21,31) " +
                        " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                  " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2031 " +
                                  "   and p.val_prm='3' " +
                                  " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // HV формула 11 - Пост.№ 354 с вычетом ГВС
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set " +
                        " is_uchet = 1" +
                        " Where nzp_serv=6 and stek=354 and kod_info in (26,31) " +
                        " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                  " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2031 " +
                                  "   and p.val_prm='4' " +
                                  " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // HV<-GV формула 20 - Пост.№ 354
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set " +
                        " is_uchet = 1" +
                        " Where nzp_serv=6 and stek=3540 " +
                        " and 0 < ( Select count(*) From ttt_prm_2 p " + 
                                  " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2035 " +
                                  "   and p.val_prm='4' " +
                                  " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // GV формула 11 - Пост.№ 354
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set " +
                        " is_uchet = 1" +
                        " Where nzp_serv=9 and stek=354 " +
                        " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                  " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2035 " +
                                  "   and p.val_prm='3' " +
                                  " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // GV формула 20 - Пост.№ 354
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set " +
                        " is_uchet = 1" +
                        " Where nzp_serv=9 and stek=3540 " +
                        " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                  " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2035 " +
                                  "   and p.val_prm='4' " +
                                  " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // ElEn формула 11 - Пост.№ 354
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set " +
                        " is_uchet = 1" +
                        " Where nzp_serv=25 and stek=354 and kod_info in (21,31) " +
                        " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                  " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2039 " +
                                  "   and p.val_prm='3' " +
                                  " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // ElEn формула 11 - Пост.№ 354 -> 2-х тарифный
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set " +
                        " is_uchet = 1" +
                        " Where nzp_serv=25 and stek=354 and kod_info in (22,32) " +
                        " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                  " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2039 " +
                                  "   and p.val_prm='4' " +
                                  " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // ElEn формула 11 - Пост.№ 354 -> 2-х тарифный
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set " +
                        " is_uchet = 1" +
                        " Where nzp_serv=210 and stek=354 " +
                        " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                  " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2039 " +
                                  "   and p.val_prm='4'  " +
                                  " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // ElEn формула 11 - Пост.№ 354 -> 3-х тарифный
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set " +
                        " is_uchet = 1" +
                        " Where nzp_serv=410 and stek=354 " +
                        " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                  " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2039 " +
                                  "   and p.val_prm='4'  " +
                                  " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // Otopl формула 11 - Пост.№ 354
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set " +
                        " is_uchet = 1" +
                        " Where nzp_serv=8 and stek=354 " +
                        " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                  " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2045 " +
                                  "   and p.val_prm='3' " +
                                  " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // Otopl формула 18 - Пост.№ 354
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set " +
                        " is_uchet = 1" +
                        " Where nzp_serv=8 and stek=3540 " +
                        " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                  " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2045 " +
                                  "   and p.val_prm='4' " +
                                  " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // GAS формула 11 - Пост.№ 354
                    ret = ExecSQL(conn_db,
                        " update ttt_aid_virt0 set " +
                        " is_uchet = 1" +
                        " Where nzp_serv=10 and stek=354 " +
                        " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                  " Where ttt_aid_virt0.nzp_dom  = p.nzp and p.nzp_prm = 2062 " +
                                  "   and p.val_prm='3' " +
                                  " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    // учет -> Пост.№ 354
                    sql =
                            " Update " + rashod.counters_xx + " Set " +
#if PG
                            " kf307       = (select v.kf307       from ttt_aid_virt0 v where v.is_uchet=1 and " + rashod.counters_xx + ".nzp_dom  = v.nzp_dom and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv)" +
                            ",kf307n      = (select v.kf307n      from ttt_aid_virt0 v where v.is_uchet=1 and " + rashod.counters_xx + ".nzp_dom  = v.nzp_dom and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv)" +
                            ",kf307f9     = (select v.kf307f9     from ttt_aid_virt0 v where v.is_uchet=1 and " + rashod.counters_xx + ".nzp_dom  = v.nzp_dom and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv)" +
                            ",val3        = (select v.val3        from ttt_aid_virt0 v where v.is_uchet=1 and " + rashod.counters_xx + ".nzp_dom  = v.nzp_dom and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv)" +
                            ",val4        = (select v.val4        from ttt_aid_virt0 v where v.is_uchet=1 and " + rashod.counters_xx + ".nzp_dom  = v.nzp_dom and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv)" +
                            ",vl210       = (select v.vl210       from ttt_aid_virt0 v where v.is_uchet=1 and " + rashod.counters_xx + ".nzp_dom  = v.nzp_dom and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv)" +
                            ",dlt_cur     = (select v.dlt_cur     from ttt_aid_virt0 v where v.is_uchet=1 and " + rashod.counters_xx + ".nzp_dom  = v.nzp_dom and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv)" +
                            ",kod_info    = (select v.kod_info    from ttt_aid_virt0 v where v.is_uchet=1 and " + rashod.counters_xx + ".nzp_dom  = v.nzp_dom and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv)" +
                            ",val1        = (select v.val1        from ttt_aid_virt0 v where v.is_uchet=1 and " + rashod.counters_xx + ".nzp_dom  = v.nzp_dom and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv)" +
                            ",val2        = (select v.val2        from ttt_aid_virt0 v where v.is_uchet=1 and " + rashod.counters_xx + ".nzp_dom  = v.nzp_dom and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv)" +
                            ",squ1        = (select v.squ1        from ttt_aid_virt0 v where v.is_uchet=1 and " + rashod.counters_xx + ".nzp_dom  = v.nzp_dom and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv)" +
                            ",gil1        = (select v.gil1        from ttt_aid_virt0 v where v.is_uchet=1 and " + rashod.counters_xx + ".nzp_dom  = v.nzp_dom and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv)" +
                            ",cnt1        = (select round(v.gil1) from ttt_aid_virt0 v where v.is_uchet=1 and " + rashod.counters_xx + ".nzp_dom  = v.nzp_dom and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv)" +
                            ",rvirt       = (select v.rvirt       from ttt_aid_virt0 v where v.is_uchet=1 and " + rashod.counters_xx + ".nzp_dom  = v.nzp_dom and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv)" +
                            ",pu7kw       = (select v.pu7kw       from ttt_aid_virt0 v where v.is_uchet=1 and " + rashod.counters_xx + ".nzp_dom  = v.nzp_dom and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv)" +
                            ",gl7kw       = (select v.gl7kw       from ttt_aid_virt0 v where v.is_uchet=1 and " + rashod.counters_xx + ".nzp_dom  = v.nzp_dom and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv)" +
                            ",kf_dpu_kg   = (select v.kf_dpu_kg   from ttt_aid_virt0 v where v.is_uchet=1 and " + rashod.counters_xx + ".nzp_dom  = v.nzp_dom and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv)" +
                            ",kf_dpu_plob = (select v.kf_dpu_plob from ttt_aid_virt0 v where v.is_uchet=1 and " + rashod.counters_xx + ".nzp_dom  = v.nzp_dom and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv)" +
                            ",kf_dpu_ls   = (select v.kf_dpu_ls   from ttt_aid_virt0 v where v.is_uchet=1 and " + rashod.counters_xx + ".nzp_dom  = v.nzp_dom and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv)" +
                            ",dlt_reval   = (select v.dlt_reval   from ttt_aid_virt0 v where v.is_uchet=1 and " + rashod.counters_xx + ".nzp_dom  = v.nzp_dom and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv)" +
                            ",dlt_real_charge = (select v.dlt_real_charge from ttt_aid_virt0 v where v.is_uchet=1 and " + rashod.counters_xx + ".nzp_dom  = v.nzp_dom and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv)" +
#else
                            " (kf307,kf307n,kf307f9,val3,val4,vl210,dlt_cur,kod_info ,val1,val2,squ1,gil1,cnt1," +
                            " rvirt,pu7kw,gl7kw,kf_dpu_kg,kf_dpu_plob,kf_dpu_ls,dlt_reval,dlt_real_charge) = " +
                            " ((select v.kf307,v.kf307n,v.kf307f9,v.val3,v.val4,v.vl210,v.dlt_cur,v.kod_info ,v.val1,v.val2,v.squ1,v.gil1,round(v.gil1)," +
                              "v.rvirt,v.pu7kw,v.gl7kw,v.kf_dpu_kg,v.kf_dpu_plob,v.kf_dpu_ls,v.dlt_reval,v.dlt_real_charge" +
                            "   from ttt_aid_virt0 v where v.is_uchet=1" +
                            "   and " + rashod.counters_xx + ".nzp_dom  = v.nzp_dom" +
                            "   and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv" +
                            " ))" +
#endif
                            " Where nzp_type = 1 and stek = 3 " +
                            "   and 0 < (select count(*)" +
                            "   from ttt_aid_virt0 v where v.is_uchet=1" +
                            "   and " + rashod.counters_xx + ".nzp_dom  = v.nzp_dom" +
                            "   and " + rashod.counters_xx + ".nzp_serv  = v.nzp_serv" +
                            " )";

                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    #endregion учет ОДН по 354 постановлению в стек = 3


                    #region учет ОДН по 307 постановлению в стек = 3

                    ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);
                    //------------------------------------------------------------------
                    // Пост.№ 307
                    //------------------------------------------------------------------
                    // hvs - odn
                    sql =
                            " Update " + rashod.counters_xx +
                            " Set kf307 = kf307f9, kf307n = kf307f9, kod_info = 6 " +
                            " Where kf307f9>0 and nzp_type = 1 and stek = 3 and nzp_serv=6 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2031 and p.val_prm='1' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                        " ) " +
                              " and ( kf307f9>1 or ( kf307f9<1 and 0 = ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2032 " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                        " ) ) ) ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // сохранить коэф=1 если указано, что не применять меньше 0
                    sql =
                            " Update " + rashod.counters_xx +
                            " Set kf307 = 1, kf307n = 1, kod_info = 6 " +
                            " Where kf307f9>0 and nzp_type = 1 and stek = 3 and nzp_serv=6 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2031 and p.val_prm='1' " +
                                        " ) " +
                              " and kf307f9<1 and 0 < ( Select count(*) From ttt_prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2032 " +
                                        " ) ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    // gvs - odn
                    sql =
                            " Update " + rashod.counters_xx +
                            " Set kf307 = kf307f9, kf307n = kf307f9, kod_info = 6 " +
                            " Where kf307f9>0 and nzp_type = 1 and stek = 3 and nzp_serv=9 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2035 and p.val_prm='1' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                        " ) " +
                              " and ( kf307f9>1 or ( kf307f9<1 and 0 = ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2036 " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                        " ) ) ) ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // сохранить коэф=1 если указано, что не применять меньше 0
                    sql =
                            " Update " + rashod.counters_xx +
                            " Set kf307 = 1, kf307n = 1, kod_info = 6 " +
                            " Where kf307f9>0 and nzp_type = 1 and stek = 3 and nzp_serv=9 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2035 and p.val_prm='1' " +
                                        " ) " +
                              " and kf307f9<1 and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2036 " +
                                        " ) ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    // el/en - odn
                    sql =
                            " Update " + rashod.counters_xx +
                            " Set kf307 = kf307f9, kf307n = kf307f9, kod_info = 6 " +
                            " Where kf307f9>0 and nzp_type = 1 and stek = 3 and nzp_serv=25 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2039 and p.val_prm='1' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                        " ) " +
                              " and ( kf307f9>1 or ( kf307f9<1 and 0 = ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2040 " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                        " ) ) ) ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // сохранить коэф=1 если указано, что не применять меньше 0
                    sql =
                            " Update " + rashod.counters_xx +
                            " Set kf307 = 1, kf307n = 1, kod_info = 6 " +
                            " Where kf307f9>0 and nzp_type = 1 and stek = 3 and nzp_serv=25 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2039 and p.val_prm='1' " +
                                        " ) " +
                              " and kf307f9<1 and 0 < ( Select count(*) From ttt_prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2040 " +
                                        " ) ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    // gas - odn
                    sql =
                            " Update " + rashod.counters_xx +
                            " Set kf307 = kf307f9, kf307n = kf307f9, kod_info = 6 " +
                            " Where kf307f9>0 and nzp_type = 1 and stek = 3 and nzp_serv=10 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2062 and p.val_prm='1' " +
                                        " ) " +
                              " and ( kf307f9>1 or ( kf307f9<1 and 0 = ( Select count(*) From ttt_prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2063 " +
                                        " ) ) ) ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // сохранить коэф=1 если указано, что не применять меньше 0
                    sql =
                            " Update " + rashod.counters_xx +
                            " Set kf307 = 1, kf307n = 1, kod_info = 6 " +
                            " Where kf307f9>0 and nzp_type = 1 and stek = 3 and nzp_serv=10 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2062 and p.val_prm='1' " +
                                        " ) " +
                              " and kf307f9<1 and 0 < ( Select count(*) From ttt_prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2063 " +
                                        " ) ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    // hvs - dpu kg
                    sql =
                            " Update " + rashod.counters_xx +
                            " Set kf307 = kf_dpu_kg, kf307n = kf_dpu_kg, kod_info = 101 " +
                            " Where kf_dpu_kg>0 and nzp_type = 1 and stek = 3 and nzp_serv=6 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2031 and p.val_prm='2' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                        " ) ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    // gvs - dpu kg
                    sql =
                            " Update " + rashod.counters_xx +
                            " Set kf307 = kf_dpu_kg, kf307n = kf_dpu_kg, kod_info = 101 " +
                            " Where kf_dpu_kg>0 and nzp_type = 1 and stek = 3 and nzp_serv=9 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2035 and p.val_prm='2' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                        " ) ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    // el/en - dpu kg
                    sql =
                            " Update " + rashod.counters_xx +
                            " Set kf307 = kf_dpu_kg, kf307n = kf_dpu_kg, kod_info = 101 " +
                            " Where kf_dpu_kg>0 and nzp_type = 1 and stek = 3 and nzp_serv=25 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2039 and p.val_prm='2' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                        " ) ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    // gas - dpu kg
                    sql =
                            " Update " + rashod.counters_xx +
                            " Set kf307 = kf_dpu_kg, kf307n = kf_dpu_kg, kod_info = 101 " +
                            " Where kf_dpu_kg>0 and nzp_type = 1 and stek = 3 and nzp_serv=10 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2062 and p.val_prm='2' " +
                                        " ) ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    // hvs - dpu plos
                    sql =
                            " Update " + rashod.counters_xx +
                            " Set kf307 = kf_dpu_plob, kf307n = kf_dpu_plob, kod_info = 102 " +
                            " Where kf_dpu_plob>0 and nzp_type = 1 and stek = 3 and nzp_serv=6 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2031 and p.val_prm='2' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                        " ) " +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2034 and p.val_prm='2' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                        " ) ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // gvs - dpu plos
                    sql =
                            " Update " + rashod.counters_xx +
                            " Set kf307 = kf_dpu_plob, kf307n = kf_dpu_plob, kod_info = 102 " +
                            " Where kf_dpu_plob>0 and nzp_type = 1 and stek = 3 and nzp_serv=9 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2035 and p.val_prm='2' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                        " ) " +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2038 and p.val_prm='2' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                        " ) ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    // el/en - dpu plos
                    sql =
                            " Update " + rashod.counters_xx +
                            " Set kf307 = kf_dpu_plob, kf307n = kf_dpu_plob, kod_info = 102 " +
                            " Where kf_dpu_plob>0 and nzp_type = 1 and stek = 3 and nzp_serv=25 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2039 and p.val_prm='2' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                        " ) " +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2042 and p.val_prm='2' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                        " ) ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    // otopl - dpu plos. Для отопления только по площади !!!
                    sql =
                            " Update " + rashod.counters_xx +
                            " Set kf307 = kf_dpu_plot, kf307n = kf_dpu_plot, kod_info = 102 " +
                            " Where kf_dpu_plot>0 and nzp_type = 1 and stek = 3 and nzp_serv=8 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2045 and p.val_prm='2' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                        " ) " +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2048 and p.val_prm='3' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                        " ) ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // gas - dpu plos
                    sql =
                            " Update " + rashod.counters_xx +
                            " Set kf307 = kf_dpu_plob, kf307n = kf_dpu_plob, kod_info = 102 " +
                            " Where kf_dpu_plob>0 and nzp_type = 1 and stek = 3 and nzp_serv=10 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2062 and p.val_prm='2' " +
                                        " ) " +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2065 and p.val_prm='2' " +
                                        " ) ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    // hvs - dpu ls
                    sql =
                            " Update " + rashod.counters_xx +
                            " Set kf307 = kf_dpu_ls, kf307n = kf_dpu_ls, kod_info = 104 " +
                            " Where kf_dpu_ls>0 and nzp_type = 1 and stek = 3 and nzp_serv=6 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2031 and p.val_prm='2' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                        " ) " +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2034 and p.val_prm='4' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                        " ) ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    // gvs - dpu ls
                    sql =
                            " Update " + rashod.counters_xx +
                            " Set kf307 = kf_dpu_ls, kf307n = kf_dpu_ls, kod_info = 104 " +
                            " Where kf_dpu_ls>0 and nzp_type = 1 and stek = 3 and nzp_serv=9 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2035 and p.val_prm='2' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                        " ) " +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2038 and p.val_prm='4' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                        " ) ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    // el/en - dpu ls
                    sql =
                            " Update " + rashod.counters_xx +
                            " Set kf307 = kf_dpu_ls, kf307n = kf_dpu_ls, kod_info = 104 " +
                            " Where kf_dpu_ls>0 and nzp_type = 1 and stek = 3 and nzp_serv=25 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2039 and p.val_prm='2' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                        " ) " +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2042 and p.val_prm='4' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                        " ) ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // gas - dpu ls
                    sql =
                            " Update " + rashod.counters_xx +
                            " Set kf307 = kf_dpu_ls, kf307n = kf_dpu_ls, kod_info = 104 " +
                            " Where kf_dpu_ls>0 and nzp_type = 1 and stek = 3 and nzp_serv=10 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2062 and p.val_prm='2' " +
                                        " ) " +
                              " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2065 and p.val_prm='4' " +
                                        " ) ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                    }
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }


                    // hvs - vid 307 vse ls? - kpu
                    ret = ExecSQL(conn_db,
                        " Update " + rashod.counters_xx +
                        " Set kf307n = 0 " +
                        " Where nzp_type = 1 and stek = 3 and nzp_serv=6 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2033 and p.val_prm='2' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                    " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // gvs - vid 307 vse ls? - kpu
                    ret = ExecSQL(conn_db,
                        " Update " + rashod.counters_xx +
                        " Set kf307n = 0 " +
                        " Where nzp_type = 1 and stek = 3 and nzp_serv=9 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2037 and p.val_prm='2' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                    " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // el/en - vid 307 vse ls? - kpu
                    ret = ExecSQL(conn_db,
                        " Update " + rashod.counters_xx +
                        " Set kf307n = 0 " +
                        " Where nzp_type = 1 and stek = 3 and nzp_serv=25 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2041 and p.val_prm='2' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                    " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // gas - vid 307 vse ls? - kpu
                    ret = ExecSQL(conn_db,
                        " Update " + rashod.counters_xx +
                        " Set kf307n = 0 " +
                        " Where nzp_type = 1 and stek = 3 and nzp_serv=10 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2064 and p.val_prm='2' " +
                                    " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    // hvs - vid 307 vse ls? - norma
                    ret = ExecSQL(conn_db,
                        " Update " + rashod.counters_xx +
                        " Set kf307 = 0 " +
                        " Where nzp_type = 1 and stek = 3 and nzp_serv=6 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2033 and p.val_prm='3' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                    " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // gvs - vid 307 vse ls? -  norma
                    ret = ExecSQL(conn_db,
                        " Update " + rashod.counters_xx +
                        " Set kf307 = 0 " +
                        " Where nzp_type = 1 and stek = 3 and nzp_serv=9 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2037 and p.val_prm='3' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                    " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // el/en - vid 307 vse ls? -  norma
                    ret = ExecSQL(conn_db,
                        " Update " + rashod.counters_xx +
                        " Set kf307 = 0 " +
                        " Where nzp_type = 1 and stek = 3 and nzp_serv=25 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " + //rashod.paramcalc.pref + "_data:prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2041 and p.val_prm='3' " +
                        //and p.is_actual <> 100" +
                        //"   and p.dat_s  <= " + rashod.paramcalc.dat_po + " and p.dat_po >= " + rashod.paramcalc.dat_s +
                                    " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    // gas - vid 307 vse ls? -  norma
                    ret = ExecSQL(conn_db,
                        " Update " + rashod.counters_xx +
                        " Set kf307 = 0 " +
                        " Where nzp_type = 1 and stek = 3 and nzp_serv=10 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_prm_2 p " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = p.nzp and p.nzp_prm = 2064 and p.val_prm='3' " +
                                    " ) "
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    #endregion учет ОДН по 307 постановлению в стек = 3


                    #region учет ОДН по 87 постановлению в стек = 3
                    //----------------------------------------------------------------
                    //87 Постановление
                    //----------------------------------------------------------------
                    //значения нормативов ОДН по-умолчанию
                    //norma_kBt = "7";
                    //notma_hvs = "0.17";
                    //notma_gvs = "0.003";
                    /*
                    if (!b_next_month)  // для Самары нет Пост.№87/262
                    {

                        Call87 call87 = new Call87(false, 25, "7");
                        Calc87(conn_db, rashod, call87, paramcalc, out ret);

                        call87 = new Call87(false, 6, "0.17");
                        Calc87(conn_db, rashod, call87, paramcalc, out ret);

                        call87 = new Call87(false, 9, "0.003");
                        Calc87(conn_db, rashod, call87, paramcalc, out ret);


                        //----------------------------------------------------------------
                        //распределенный (учтенный) расход
                        //----------------------------------------------------------------
                        ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);

                        ret = ExecSQL(conn_db,
                              " Select unique nzp_dom, nzp_serv, sum(rashod) as rashod, sum(dop87) as dop87 " +
                              " From " + rashod.counters_xx + " a " +
                              " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                            //"   and nzp_counter = 109 "+
                              " Group by 1,2 " +
                              " Into temp ttt_aid_stat With no log "
                            , true);
                        if (!ret.result)
                        {
                            DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                            return false;
                        }

                        ExecSQL(conn_db, " Create unique index ix_aid44_st on ttt_aid_stat (nzp_dom,nzp_serv) ", true);
                        ExecSQL(conn_db, " Update statistics for table ttt_aid_stat ", true);

                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx",
                            " Update " + rashod.counters_xx +
                            " Set (dlt_calc, dop87) = (( " +
                                        " Select sum(rashod), sum(dop87) From ttt_aid_stat a " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                        "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv )) " +
                            " Where nzp_type = 1 and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                              " and 0 < ( Select count(*) From ttt_aid_stat a " +
                                        " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                        "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                        " ) "
                          , 100000, " ", out ret);
                        if (!ret.result)
                        {
                            DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                            return false;
                        }

                        ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx",
                            " Update " + rashod.counters_xx +
                            " Set dlt_out = val3 - dlt_calc - dop87 " + //текущая дельта
                            " Where nzp_type = 1 and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                            "   and val3 > 0 "
                          , 100000, " ", out ret);
                        if (!ret.result)
                        {
                            DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                            return false;
                        }

                    }
                    */
                    #endregion учет ОДН по 87 постановлению в стек = 3

                    #endregion начинаем бермудид по 307 и 87 постановлениям

                }
                else 
                {
                    #region учет ОДН по домам из прошлого, если он был рассчитан - запись в стек = 3
                    //
                    // если не текущий месяц (т.е. - перерасчет) и запрещен перерасчет ОДН, 
                    // то поискать домовые расходы в прошлом и взять готовые
                    //
                    string cur_counters_xx = "";
                    cur_counters_xx = 
                        rashod.paramcalc.pref + "_charge_" + (rashod.paramcalc.calc_yy - 2000).ToString("00") +
#if PG
                                "." +
#else
                                ":" + 
#endif
                        "counters_" + rashod.paramcalc.calc_mm.ToString("00");
                    //
                    // месяц последнего рассчитанного расхода по дому - первый расчетный за этот месяц/год
                    //
                    ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                    ret = ExecSQL(conn_db,
                        " Create temp table ttt_aid_stat " +
                        " ( nzp_dom integer, " +
                        "   nzp_serv integer, " +
#if PG
                        "   kf307       numeric(15,7) default 0.00 , " +  //коэфициент 307 для КПУ или коэфициент 87 для нормативщиков
                        "   kf307n      numeric(15,7) default 0.00 , " +  //коэфициент 307 для нормативщиков
#else
                        "   kf307       decimal(15,7) default 0.00 , " +  //коэфициент 307 для КПУ или коэфициент 87 для нормативщиков
                        "   kf307n      decimal(15,7) default 0.00 , " +  //коэфициент 307 для нормативщиков
#endif
                        "   kod_info    integer default 0, " +            //выбранный способ учета (=1)
                        "   is_not_calc integer default 0  " +            //если рассчитан по ДПУ =0 / иначе "левый" коэф. корр. =1
#if PG
    " )  "
#else
    " ) With no log "
#endif
                        , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }

                    ret = ExecSQL(conn_db,
                      " insert into ttt_aid_stat (nzp_dom, nzp_serv, kf307, kf307n, kod_info) " +
                      " Select nzp_dom, nzp_serv, max(kf307) kf307, max(kf307n) kf307n, max(kod_info) kod_info " +
                      " From " + cur_counters_xx +
                      " Where nzp_type = 1 and stek = 3 and kod_info>0 and " + rashod.where_dom +
                      " group by 1,2 "
                    , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    ret = ExecSQL(conn_db,
                        " Update " + rashod.counters_xx +
                        " Set (kf307,kf307n,kod_info) = (( " +
#if PG
                                    " Select kf307 From ttt_aid_stat a " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                    " ),"+
                                    "(Select kf307n From ttt_aid_stat a " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                    " ),"+
                                    " (Select kod_info From ttt_aid_stat a " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                    " )) " +
#else
" Select kf307,kf307n,kod_info From ttt_aid_stat a " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                    " )) " +
#endif
                        " Where nzp_type = 1 and stek = 3 and " + rashod.where_dom +
                        " and 0 < ( Select count(*) From ttt_aid_stat a " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                    " ) "
                    , true);
                    if (!ret.result)
                    {
                        DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                        return false;
                    }
                    //    
                    #endregion учет ОДН по домам из прошлого, если он был рассчитан - запись в стек = 3
                }
            }

            #endregion расчеты ОДН по дому - stek=3 & nzp_type = 1

            #region учет ОДН по л/с
            //----------------------------------------------------------------
            // учет ОДН по л/с
            //----------------------------------------------------------------
            if (Constants.Trace) Utility.ClassLog.WriteLog("учет ОДН по л/с");
            ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_stat " +
                " ( nzp_dom integer, " +
                "   nzp_serv integer, " +
                "   kf307       " + sDecimalType + "(15,7) default 0.00 , " +  //коэфициент 307 для КПУ или коэфициент 87 для нормативщиков
                "   kf307n      " + sDecimalType + "(15,7) default 0.00 , " +  //коэфициент 307 для нормативщиков
                "   kod_info    integer default 0, " +            //выбранный способ учета (=1)
                "   is_not_calc integer default 0  " +            //если рассчитан по ДПУ =0 / иначе "левый" коэф. корр. =1
#if PG
                " )  "
#else
                " ) With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Create unique index ix_aid44_st on ttt_aid_stat (nzp_dom,nzp_serv) ", false);
            ExecSQL(conn_db, " Create index ix2_aid44_st on ttt_aid_stat (is_not_calc) ", false);

            bool b_cur_cur = rashod.paramcalc.b_cur;

            //if (!b_calc_kvar || rashod.paramcalc.b_cur)
            if (!b_calc_kvar || b_cur_cur)
            {
                //
                // если расчет/перерасчет по дому или всей базе и в текущем расчетном месяце - всегда есть расчет расходов по дому!

                ret = ExecSQL(conn_db,
                  " insert into ttt_aid_stat (nzp_dom, nzp_serv, kf307, kf307n, kod_info) " +
                  " Select nzp_dom, nzp_serv, max(kf307) kf307, max(kf307n) kf307n, max(kod_info) kod_info " +
                  " From " + rashod.counters_xx +
                  " Where nzp_type = 1 and stek = 3 and kod_info>0 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                  " group by 1,2 " 
                , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ExecSQL(conn_db, sUpdStat + " ttt_aid_stat ", false);

                ExecSQL(conn_db, " Drop table ttt_aid_statx ", false);
                ret = ExecSQL(conn_db,
                    " Create temp table ttt_aid_statx " +
                    " ( nzp_dom integer, " +
                    "   nzp_serv integer " +
#if PG
                    " )  "
#else
                    " ) With no log "
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ret = ExecSQL(conn_db,
                    " Insert into ttt_aid_statx (nzp_dom, nzp_serv) " +
                    " Select nzp_dom, nzp_serv From ttt_aid_stat "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                ExecSQL(conn_db, " Create index ix_ttt_aid_statx on ttt_aid_statx (nzp_dom,nzp_serv) ", false);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_statx ", false);

                // если не было домовых расходов по дому в текущем расчетном месяце ,
                // но есть коэф-т коррекции для программы 2.0, то использовать его
                ret = ExecSQL(conn_db,
                  " insert into ttt_aid_stat (nzp_dom, nzp_serv, kf307, kf307n, kod_info, is_not_calc) " +
                  " Select nzp_dom, nzp_serv, max(rval) kf307, max(rval_real) kf307n, max(nzp_type_alg) kod_info, 1 " +
                  " From " + rashod.paramcalc.data_alias + "counters_correct f" +
                  " Where 1=1 and " + rashod.where_dom +
                  " and f.dat_month = " + rashod.paramcalc.dat_s +
                  " and f.dat_charge = " +
                  " (Select max(b.dat_charge)" +
                  "  From " + rashod.paramcalc.data_alias + "counters_correct b" +
                  "  Where f.nzp_dom=b.nzp_dom and f.nzp_serv=b.nzp_serv and b.dat_month=" + rashod.paramcalc.dat_s + ")" +
                  " and 0 = ( Select count(*) From ttt_aid_statx a " +
                                " Where f.nzp_dom  = a.nzp_dom   " +
                                "   and f.nzp_serv = a.nzp_serv )" +
                  " group by 1,2 "
                , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ExecSQL(conn_db, sUpdStat + " ttt_aid_stat ", false);
                ExecSQL(conn_db, " Drop table ttt_aid_statx ", false);

                ExecSQL(conn_db, " Drop table ttt_aid_kpu ", false);
                ret = ExecSQL(conn_db,
                    " Create temp table ttt_aid_kpu " +
                    " ( nzp_dom integer, " +
                    "   nzp_serv integer, " +
                    "   kf307       " + sDecimalType + "(15,7) default 0.00 , " +
                    "   kf307n      " + sDecimalType + "(15,7) default 0.00 , " +
                    "   kod_info    integer default 0, " +
                    "   is_not_calc integer default 0  " +
#if PG
                    " )  "
#else
                    " ) With no log "
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ret = ExecSQL(conn_db,
                  " insert into ttt_aid_kpu (nzp_dom, nzp_serv, kf307, kf307n, kod_info, is_not_calc) " +
                  " Select nzp_dom,14 nzp_serv, kf307, kf307n, kod_info, 1 " +
                  " From ttt_aid_stat " +
                  " Where is_not_calc=1 and nzp_serv=9 "
                , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ret = ExecSQL(conn_db,
                  " insert into ttt_aid_kpu (nzp_dom, nzp_serv, kf307, kf307n, kod_info, is_not_calc) " +
                  " Select nzp_dom,210 nzp_serv, kf307, kf307n, kod_info, 1 " +
                  " From ttt_aid_stat " +
                  " Where is_not_calc=1 and nzp_serv=25 and kod_info in (5,6,7,16,21,31) "
                , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ExecSQL(conn_db, " Create unique index ix_aid33_kpu on ttt_aid_kpu (nzp_dom,nzp_serv) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_kpu ", true);

                ret = ExecSQL(conn_db,
                  " insert into ttt_aid_stat (nzp_dom, nzp_serv, kf307, kf307n, kod_info, is_not_calc) " +
                  " Select nzp_dom, nzp_serv, kf307, kf307n, kod_info, is_not_calc From ttt_aid_kpu Where 1=1 "
                , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ret = ExecSQL(conn_db,
                    " Update " + rashod.counters_xx +
                    " Set (kf307,kf307n,kod_info) = (( " +
#if PG
                                " Select kf307 From ttt_aid_stat a " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                "   and is_not_calc=1),"+
                                " (Select kf307n From ttt_aid_stat a " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                "   and is_not_calc=1),"+
                                " (Select kod_info From ttt_aid_stat a " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                "   and is_not_calc=1)) " +
#else
                                " Select kf307,kf307n,kod_info From ttt_aid_stat a " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                "   and is_not_calc=1)) " +
#endif
                    " Where nzp_type = 1 and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                    " and 0 < ( Select count(*) From ttt_aid_stat a " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                "   and is_not_calc=1) "
                , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                //
            }
            else
            {
                
                //
                // если перерасчет по 1му л/с, то нужно поискать домовые расходы в прошлом
                //
                //позиционируемся на список расчетных месяцев данного nzp_wp
                int ipos = -1;
                for (int i = 0; i < Points.PointList.Count; i++)
                {
                    if (Points.PointList[i].nzp_wp == rashod.paramcalc.nzp_wp)
                    {
                        ipos = i;
                        break;
                    }
                }
                // если nzp_wp найден
                if (ipos >= 0)
                {
                    
                    string cur_counters_alias = "";
                    string cur_counters_tab = "";
                    string cur_counters_xx = "";
                    int icountvals = 0;
                    // по найденному текущему nzp_wp с учетом отсортированности по убыванию месяцев (из будущего в прошлое до текущего месяца перерасчета)
                    for (int j = 0; j < Points.PointList[ipos].CalcMonths.Count; j++)
                    {
                        //проверим, есть ли данный месяц среди перерасчитываемых данных
                        int yy = Points.PointList[ipos].CalcMonths[j].year_;
                        int mm = Points.PointList[ipos].CalcMonths[j].month_;
                        
                        if (yy == 0 || mm == 0) continue;

                        //отсечем перерасчеты в прошлом до текущего месяца перерасчета
                        if (yy < rashod.paramcalc.calc_yy) continue;
                        if (yy == rashod.paramcalc.calc_yy && mm < rashod.paramcalc.calc_mm) continue;
                        
                        //
                        if (yy == rashod.paramcalc.calc_yy && mm == rashod.paramcalc.calc_mm)
                        {
                            cur_counters_alias = "";
                        }
                        else
                        {
                            cur_counters_alias = (yy - 2000).ToString("00") + mm.ToString("00");
                        }
                        cur_counters_tab = "counters" + cur_counters_alias + "_" + rashod.paramcalc.calc_mm.ToString("00");
                        cur_counters_xx = rashod.paramcalc.pref + "_charge_" + (rashod.paramcalc.calc_yy - 2000).ToString("00") +
                            tableDelimiter + cur_counters_tab;
                        
                        IDbCommand cmd = DBManager.newDbCommand(
                            " Select count(*) From " + cur_counters_xx + " Where nzp_type = 1 and stek = 3 and " + rashod.where_dom
                            , conn_db);
                        try
                        {
                            string scountvals = Convert.ToString(cmd.ExecuteScalar());
                            icountvals = Convert.ToInt32(scountvals);
                        }
                        catch
                        {
                            icountvals = 0;
                        }
                        if (icountvals > 0)
                        {
                            //
                            // найден месяц последнего рассчитанного расхода по дому
                            //
                            ret = ExecSQL(conn_db,
                              " insert into ttt_aid_stat (nzp_dom, nzp_serv, kf307, kf307n, kod_info) " +
                              " Select nzp_dom, nzp_serv, max(kf307) kf307, max(kf307n) kf307n, max(kod_info) kod_info " +
                              " From " + cur_counters_xx +
                              " Where nzp_type = 1 and stek = 3 and kod_info>0 and " + rashod.where_dom +
                              " group by 1,2 "
                            , true);
                            if (!ret.result)
                            {
                                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                                return false;
                            }
                            break;
                        }
                        //
                    }
                }
                //    
            }

            ExecSQL(conn_db, sUpdStat + " ttt_aid_stat ", false);

            sql =
                    " Update " + rashod.counters_xx +
                    " Set (kf307,kf307n,kod_info) = (( " +
#if PG
                                " Select kf307 From ttt_aid_stat a " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                " ),"+
                                " (Select kf307n From ttt_aid_stat a " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                " ),"+
                                " (Select kod_info From ttt_aid_stat a " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                " )) " +
#else
                    " Select kf307,kf307n,kod_info From ttt_aid_stat a " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                " )) " +
#endif
                    " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                      " and 0 < ( Select count(*) From ttt_aid_stat a " +
                                " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                " ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            #endregion учет ОДН по л/с

            #region Одним ударом семь постновлений убил: вывести расход по л/с
            // вывести расход по л/с
            // odn
            sql =
                " Update " + rashod.counters_xx +
                // доп. расход есть в val2 для нормы и в val1 для ИПУ!
                " Set rashod = (case when cnt_stage = 0 then val2 else val1 end) + " +
                //case when cnt_stage = 0 then val1 * kf307n " + //начислить норматив
                //" else val2 * kf307 " + // расход КПУ
                //" end, nzp_counter = 109 " +

                " case when cnt_stage = 0" +
                " then " + //начислить норматив

                    " case when nzp_serv=210 then " +

                    " (case when kod_info= 5 then val1 " +
                    "       when kod_info= 6 then val1 * kf307 " +
                    "       when kod_info= 7 then val1 " +
                    "       when kod_info= 8 then val1 " +
                    "       when kod_info=10 then val1 " +
                    "       when kod_info=14 then val1 * kf307n " +
                    "       when kod_info=15 then val1 " +
                    "       when kod_info=16 then val1 * kf307n " +

                    "       when kod_info=21 then val1 " +
                    "       when kod_info=22 then val1 + squ1 * kf307n " +
                    "       when kod_info=31 then val1 " +
                    "       when kod_info=32 then val1 + gil1 * kf307n " +

                    "       else val1 end)" +

                    " else " +

                    "(case when kod_info= 5 then val1 " +
                    "      when kod_info= 6 then val1 * kf307 " +
                    "      when kod_info= 7 then val1 + squ1 * kf307n " +
                    "      when kod_info= 8 then val1 + squ1 * kf307n " +
                    "      when kod_info=10 then val1 + squ1 * kf307n " +
                    "      when kod_info=14 then val1 * kf307 " +
                    "      when kod_info=15 then val1 + squ1 * kf307n " +
                    "      when kod_info=16 then val1 * kf307 " +

                    "      when kod_info=21 then val1 + squ1 * kf307n " +
                    "      when kod_info=22 then val1 + squ1 * kf307n " +
                    "      when kod_info=23 then val1 + squ1 * kf307n " +
                    "      when kod_info=24 then val1 " +
                    "      when kod_info=25 then val1 " +
                    "      when kod_info=26 then val1 + squ1 * kf307n " +
                    "      when kod_info=27 then val1 + squ1 * kf307n " +
                    "      when kod_info=31 then val1 + gil1 * kf307n " +
                    "      when kod_info=32 then val1 + gil1 * kf307n " +
                    "      when kod_info=33 then val1 + squ1 * kf307n " +

                    "      else val1 end)" +

                    "end " +

                " else " +  //расход КПУ

                    " case when nzp_serv=210 then " +

                    "(case when kod_info= 5 then val2 * kf307 " +
                    "      when kod_info= 6 then val2 * kf307 " +
                    "      when kod_info= 7 then val2 * kf307 " +
                    "      when kod_info= 8 then val2 " +
                    "      when kod_info=10 then val2 " +
                    "      when kod_info=14 then val2 * kf307 " +
                    "      when kod_info=15 then val2 " +
                    "      when kod_info=16 then val2 * kf307 " +

                    "      when kod_info=21 then val2 " +
                    "      when kod_info=22 then val2 + squ1 * kf307 " +
                    "      when kod_info=31 then val2 " +
                    "      when kod_info=32 then val2 + gil1 * kf307 " +

                    "      else val1 end)" +

                    " else " +

                    "(case when kod_info= 5 then val2 * kf307 " +
                    "      when kod_info= 6 then val2 * kf307 " +
                    "      when kod_info= 7 then val2 * kf307 " +
                    "      when kod_info= 8 then val2 + gil1 * kf307 " +
                    "      when kod_info=10 then val2 * kf307 " +
                    "      when kod_info=14 then val2 * kf307 " +
                    "      when kod_info=15 then val2 + gil1 * kf307 " +
                    "      when kod_info=16 then val2 * kf307 " +

                    "      when kod_info=21 then val2 + squ1 * kf307 " +
                    "      when kod_info=22 then val2 + squ1 * kf307" +
                    "      when kod_info=23 then val2 + squ1 * kf307 " +
                    "      when kod_info=24 then val2 " +
                    "      when kod_info=25 then val2 " +
                    "      when kod_info=26 then val2 + squ1 * kf307 " +
                    "      when kod_info=27 then val2 + squ1 * kf307 " +
                    "      when kod_info=31 then val2 + gil1 * kf307 " +
                    "      when kod_info=32 then val2 + gil1 * kf307 " +
                    "      when kod_info=33 then val2 + squ1 * kf307 " +

                    "      else val2 end)" +

                    "end " +

                "end, nzp_counter = 0, " +
                //
                " dop87 = " +

                " case when cnt_stage = 0" +
                " then " + //начислить норматив

                    " case when nzp_serv=210 then " +

                    " (case when  kod_info= 5 then 0 " +
                    "       when  kod_info= 6 then val1 * kf307 - val1 " +
                    "       when  kod_info= 7 then 0 " +
                    "       when  kod_info= 8 then 0 " +
                    "       when  kod_info=10 then 0 " +
                    "       when  kod_info=14 then val1 * kf307n - val1 " +
                    "       when  kod_info=15 then 0 " +
                    "       when  kod_info=16 then val1 * kf307n - val1 " +

                    "       when kod_info=21 then 0 " +
                    "       when kod_info=22 then squ1 * kf307n " +
                    "       when kod_info=31 then 0 " +
                    "       when kod_info=32 then gil1 * kf307n " +

                    "       else 0 end)" +

                    " else " +

                    "(case when kod_info= 5 then 0 " +
                    "      when kod_info= 6 then val1 * kf307 - val1 " +
                    "      when kod_info= 7 then squ1 * kf307n " +
                    "      when kod_info= 8 then squ1 * kf307n " +
                    "      when kod_info=10 then squ1 * kf307n " +
                    "      when kod_info=14 then val1 * kf307 - val1 " +
                    "      when kod_info=15 then squ1 * kf307n " +
                    "      when kod_info=16 then val1 * kf307 - val1 " +

                    "      when kod_info=21 then squ1 * kf307n " +
                    "      when kod_info=22 then squ1 * kf307n " +
                    "      when kod_info=23 then squ1 * kf307n " +
                    "      when kod_info=24 then 0 " +
                    "      when kod_info=25 then 0 " +
                    "      when kod_info=26 then squ1 * kf307n " +
                    "      when kod_info=31 then gil1 * kf307n " +
                    "      when kod_info=32 then gil1 * kf307n " +
                    "      when kod_info=33 then gil1 * kf307n " +

                    "      else 0 end)" +

                    "end " +

                " else " +  //расход КПУ

                    " case when nzp_serv=210 then " +

                    "(case when kod_info= 5 then val2 * kf307  - val2 " +
                    "      when kod_info= 6 then val2 * kf307  - val2 " +
                    "      when kod_info= 7 then val2 * kf307  - val2 " +
                    "      when kod_info= 8 then 0 " +
                    "      when kod_info=10 then 0 " +
                    "      when kod_info=14 then val2 * kf307  - val2 " +
                    "      when kod_info=15 then 0 " +
                    "      when kod_info=16 then val2 * kf307  - val2 " +

                    "      when kod_info=21 then 0 " +
                    "      when kod_info=22 then squ1 * kf307 " +
                    "      when kod_info=31 then 0 " +
                    "      when kod_info=32 then gil1 * kf307 " +

                    "      else 0 end)" +

                    " else " +

                    "(case when  kod_info= 5 then val2 * kf307  - val2 " +
                    "      when  kod_info= 6 then val2 * kf307  - val2 " +
                    "      when  kod_info= 7 then val2 * kf307  - val2 " +
                    "      when  kod_info= 8 then gil1 * kf307 " +
                    "      when  kod_info=10 then val2 * kf307  - val2 " +
                    "      when  kod_info=14 then val2 * kf307  - val2 " +
                    "      when  kod_info=15 then gil1 * kf307 " +
                    "      when  kod_info=16 then val2 * kf307  - val2 " +

                    "      when kod_info=21 then squ1 * kf307 " +
                    "      when kod_info=22 then squ1 * kf307" +
                    "      when kod_info=23 then squ1 * kf307 " +
                    "      when kod_info=24 then 0 " +
                    "      when kod_info=25 then 0 " +
                    "      when kod_info=26 then squ1 * kf307 " +
                    "      when kod_info=31 then gil1 * kf307 " +
                    "      when kod_info=32 then gil1 * kf307 " +
                    "      when kod_info=33 then gil1 * kf307 " +

                    "      else 0 end)" +

                    "end " +

                "end " +
                //                
                " Where nzp_type = 3 and stek = 3 and kod_info>0 and kod_info<100 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                " and nzp_serv <> 8 ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            #endregion Одним ударом семь постновлений убил: вывести расход по л/с

            #region ОДН по ДПУ - kod_info=101
            // dpu
            sql =
                    " Update " + rashod.counters_xx +
                    " Set rashod = case when cnt_stage = 0 then gil1 * kf307n " + //начислить норматив
                                 " else gil1 * kf307 " + // расход КПУ
                                 " end, nzp_counter = 0 " +
                    "   , val1_g = case when cnt_stage = 0 then gil1_g * kf307n " + // расход без учета вр.выбывших!
                                 " else gil1_g * kf307 " +
                                 " end " +
                    " Where nzp_type = 3 and stek = 3 and kod_info=101 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                    " and nzp_serv <> 8 ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            #endregion ОДН по ДПУ - kod_info=101

            #region ОДН по ДПУ - kod_info=102. Учесть ОДН отопление , пока 1 случай когда учитыается отопление!
            // пока 1 случай когда учитыается отопление!
            sql =
                    " Update " + rashod.counters_xx +
                    " Set rashod = case when cnt_stage = 0 then (case when nzp_serv=8 then squ2 else squ1 end) * kf307n " + //начислить норматив
                                 " else (case when nzp_serv=8 then squ2 else squ1 end) * kf307 " + // расход КПУ
                                 " end, nzp_counter = 0 " +
                    "   , val1_g = case when cnt_stage = 0 then (case when nzp_serv=8 then squ2 else squ1 end) * kf307n " +
                                 " else (case when nzp_serv=8 then squ2 else squ1 end) * kf307 " + // расход без учета вр.выбывших совпадает с расходом по услугЕ!
                                 " end " +
                    " Where nzp_type = 3 and stek = 3 and kod_info=102 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge;

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            #endregion ОДН по ДПУ - kod_info=102. Учесть ОДН отопление , пока 1 случай когда учитыается отопление!

            #region ОДН по ДПУ - kod_info=104
            sql =
                    " Update " + rashod.counters_xx +
                    " Set rashod = case when cnt_stage = 0 then 1 * kf307n " + //начислить норматив
                                 " else 1 * kf307 " + // расход КПУ
                                 " end, nzp_counter = 0 " +
                    "   , val1_g = case when cnt_stage = 0 then 1 * kf307n " +
                                 " else 1 * kf307 " + // расход без учета вр.выбывших совпадает с расходом по услугЕ!
                                 " end " +
                    " Where nzp_type = 3 and stek = 3 and kod_info=104 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                    " and nzp_serv <> 8 ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            #endregion ОДН по ДПУ - kod_info=104

            #region учет стека 29 - доли расхода коммуналки в расходе квартиры по Пост 354
            //----------------------------------------------------------------
            // учет стека 29 - доли расхода коммуналки в расходе квартиры по Пост 354
            //----------------------------------------------------------------

            // по квартирам, где есть коммуналки kf = squ2/val2 по жилым площадям
            // учтем долю коммуналки если был расчет по Пост 354
            ExecSQL(conn_db, " Drop table t_ans_itog ", false);
            ret = ExecSQL(conn_db,
                " Create temp table t_ans_itog (" +
                " nzp_cntx integer not null," +
                " kf307  " + sDecimalType + "(14,7) default 0.0000000, " +
                " kf354  " + sDecimalType + "(14,7) default 0.0000000, " +
                " squ1   " + sDecimalType + "(14,7) default 0.0000000, " +
                " rnorm  " + sDecimalType + "(14,7) default 0.0000000, " +
                " rpu    " + sDecimalType + "(14,7) default 0.0000000 " +
#if PG
                " )  "
#else
                " ) With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ret = ExecSQL(conn_db,
                " Insert into t_ans_itog (kf307,squ1,kf354,rnorm,rpu,nzp_cntx)" +
                " Select k.kf307,k.val1,c.kf307 kf354,c.val1 rnorm,c.val2 rpu,c.nzp_cntx " +
                " From t_ans_kommunal k, " + rashod.counters_xx +" c "+
                " where k.nzp_kvar=c.nzp_kvar and c.nzp_type = 3 and c.stek = 3 and c.kod_info in (21,22,23,26,27) "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            ExecSQL(conn_db, " Create unique index ix_t_ans_itog on t_ans_itog (nzp_cntx) ", true);
            ExecSQL(conn_db, sUpdStat + " t_ans_itog ", true);

            string sqlq = "squ1 * kf307";
            if (Points.IsSmr)
            {
                // для самары приведенную площадь для ОДН для коммуналок округлить до 2х знаков
                sqlq = "Round( squ1 * kf307 * 100 )/100";
            }

            sql =
                " Update " + rashod.counters_xx +
                " Set (rashod,dop87,kf_dpu_ls, squ1) =" +
#if PG
                " (( Select rnorm + rpu + (" + sqlq + " * kf354) From t_ans_itog k Where " + rashod.counters_xx + ".nzp_cntx = k.nzp_cntx ),"+
                "( Select " + sqlq + " * kf354 From t_ans_itog k Where " + rashod.counters_xx + ".nzp_cntx = k.nzp_cntx ),"+
                "( Select  kf307 From t_ans_itog k Where " + rashod.counters_xx + ".nzp_cntx = k.nzp_cntx ),"+
                "( Select squ1  From t_ans_itog k Where " + rashod.counters_xx + ".nzp_cntx = k.nzp_cntx )) " +
#else
                " (( Select rnorm + rpu + (" + sqlq + " * kf354), " + sqlq + " * kf354, kf307, squ1 " +
                "    From t_ans_itog k Where " + rashod.counters_xx + ".nzp_cntx = k.nzp_cntx )) " +
#endif
                " Where 0 < ( Select count(*) From t_ans_itog k Where " + rashod.counters_xx + ".nzp_cntx = k.nzp_cntx ) ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            #endregion учет стека 29 - доли расхода коммуналки в расходе квартиры по Пост 354

            #region коррекция расхода по канализации по ХВС и ГВС
            //----------------------------------------------------------------
            //коррекция расхода по канализации по ХВС и ГВС
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_ans_rsh69 ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_ans_rsh69 (" +
                " nzp_kvar integer," +
                " nzp_serv integer," +
                " pr_rash  integer default 0," +
#if PG
                " rashod   numeric(16,6) default 0," +
                " rash_kv  numeric(16,6) default 0," +
                " rash_kv_g numeric(16,6) default 0," +
                " rash_odn numeric(16,6) default 0," +
                " kf307    numeric(15,7) default 0," +
#else
                " rashod   decimal(16,6) default 0," +
                " rash_kv  decimal(16,6) default 0," +
                " rash_kv_g decimal(16,6) default 0," +
                " rash_odn decimal(16,6) default 0," +
                " kf307    decimal(15,7) default 0," +
#endif
                " kod_info  integer  default 0," +
                " is_device integer  default 0" +
#if PG
 " ) "
#else
 " ) With no log"
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            // расход ХВС по КПУ
            ret = ExecSQL(conn_db,
                " Insert into ttt_ans_rsh69 (nzp_kvar,nzp_serv,pr_rash,is_device,rashod,kf307,kod_info,rash_odn,rash_kv,rash_kv_g) " +
                " Select nzp_kvar, 6, 1, 1, max(rashod), max(kf307), max(kod_info), max(dop87), max(val2), max(val2) from " + rashod.counters_xx +
                " Where nzp_type = 3 and stek=3 and nzp_serv=6 and cnt_stage<>0 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                " group by 1 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            // расход ХВС по ДПУ для нормативных л/с
            ret = ExecSQL(conn_db,
                " Insert into ttt_ans_rsh69 (nzp_kvar,nzp_serv,pr_rash,rashod,kf307,kod_info,rash_odn,rash_kv,rash_kv_g) " +
                " Select nzp_kvar, 6, 1, max(rashod), max(kf307), max(kod_info), max(dop87), max(val1), max(val1_g) from " + rashod.counters_xx +
                " Where nzp_type = 3 and stek=3 and nzp_serv=6 and cnt_stage=0 and kod_info>0 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                " group by 1 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            // расход ГВС по КПУ
            ret = ExecSQL(conn_db,
                " Insert into ttt_ans_rsh69 (nzp_kvar,nzp_serv,pr_rash,is_device,rashod,kf307,kod_info,rash_odn,rash_kv,rash_kv_g) " +
                " Select nzp_kvar, 9, 3, 1, max(rashod), max(kf307), max(kod_info), max(dop87), max(val2), max(val2) from " + rashod.counters_xx +
                " Where nzp_type = 3 and stek=3 and nzp_serv=9 and cnt_stage<>0 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                " group by 1 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            // расход ГВС по ДПУ для нормативных л/с
            ret = ExecSQL(conn_db,
                " Insert into ttt_ans_rsh69 (nzp_kvar,nzp_serv,pr_rash,rashod,kf307,kod_info,rash_odn,rash_kv,rash_kv_g) " +
                " Select nzp_kvar, 9, 3, max(rashod), max(kf307), max(kod_info), max(dop87), max(val1), max(val1_g) from " + rashod.counters_xx +
                " Where nzp_type = 3 and stek=3 and nzp_serv=9 and cnt_stage=0 and kod_info>0 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                " group by 1 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Create unique index ix_ttt_ans_rsh69 on ttt_ans_rsh69 (nzp_kvar,nzp_serv) ", true);
#if PG
            ExecSQL(conn_db, " analyze ttt_ans_rsh69 ", true);
#else
ExecSQL(conn_db, " Update statistics for table ttt_ans_rsh69 ", true);
#endif

            ExecSQL(conn_db, " Drop table ttt_ans_rsh7 ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_ans_rsh7 (" +
                " nzp_kvar integer," +
                " pr_rash  integer default 0," +
#if PG
                " rsh_hv   numeric(16,6) default 0," +
                " rsh_gv   numeric(16,6) default 0," +
                " rsh_kv_hv  numeric(16,6) default 0," +
                " rsh_kv_gv  numeric(16,6) default 0," +
                " rsh_kv_hv_g  numeric(16,6) default 0," +
                " rsh_kv_gv_g  numeric(16,6) default 0," +
                " rsh_odn_hv numeric(16,6) default 0," +
                " rsh_odn_gv numeric(16,6) default 0," +
                " kf307hv  numeric(15,7) default 1," +
                " kf307gv  numeric(15,7) default 1," +
#else
                " rsh_hv   decimal(16,6) default 0," +
                " rsh_gv   decimal(16,6) default 0," +
                " rsh_kv_hv  decimal(16,6) default 0," +
                " rsh_kv_gv  decimal(16,6) default 0," +
                " rsh_kv_hv_g  decimal(16,6) default 0," +
                " rsh_kv_gv_g  decimal(16,6) default 0," +
                " rsh_odn_hv decimal(16,6) default 0," +
                " rsh_odn_gv decimal(16,6) default 0," +
                " kf307hv  decimal(15,7) default 1," +
                " kf307gv  decimal(15,7) default 1," +
#endif
                " kod_info_hv integer  default 0," +
                " kod_info_gv integer  default 0," +
                " kod_info    integer  default 0," +
#if PG
                " rsh_odn_minus numeric(16,6) default 0," +
#else
                " rsh_odn_minus decimal(16,6) default 0," +
#endif
                " is_device integer  default 0" +
#if PG
 " ) "
#else
  " ) With no log"
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }
            // расход ХВС и ГВС по видам для КАН
            ret = ExecSQL(conn_db,
                " Insert into ttt_ans_rsh7 (nzp_kvar,pr_rash,rsh_hv,rsh_odn_hv,kf307hv,rsh_gv,rsh_odn_gv,kf307gv,kod_info_hv,kod_info_gv,is_device,rsh_kv_hv,rsh_kv_gv,rsh_kv_hv_g,rsh_kv_gv_g) " +
                " Select nzp_kvar, sum(pr_rash)," +
                " sum(case when nzp_serv=6 then rashod else 0 end), sum(case when nzp_serv=6 then rash_odn else 0 end), sum(case when nzp_serv=6 then kf307 else 0 end)," +
                " sum(case when nzp_serv=9 then rashod else 0 end), sum(case when nzp_serv=9 then rash_odn else 0 end), sum(case when nzp_serv=9 then kf307 else 0 end)," +
                " max(case when nzp_serv=6 then kod_info else 0 end),max(case when nzp_serv=9 then kod_info else 0 end),max(is_device),"+
                " max(case when nzp_serv=6 then rash_kv else 0 end),max(case when nzp_serv=9 then rash_kv else 0 end)" +
                ",max(case when nzp_serv=6 then rash_kv_g else 0 end),max(case when nzp_serv=9 then rash_kv_g else 0 end)" +
                " from ttt_ans_rsh69" +
                " group by 1 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ExecSQL(conn_db, " Create unique index ix_ttt_ans_rsh7 on ttt_ans_rsh7 (nzp_kvar) ", true);
#if PG
            ExecSQL(conn_db, " analyze ttt_ans_rsh7 ", true);
#else
ExecSQL(conn_db, " Update statistics for table ttt_ans_rsh7 ", true);
#endif

            // добавить нормативы ХВ или ГВ если был КПУ по одной из услуг
            ret = ExecSQL(conn_db,
                " update ttt_ans_rsh7 set (rsh_gv,rsh_odn_gv,rsh_kv_gv,kod_info_gv,rsh_kv_gv_g)=" +
#if PG
            "  (( select max(s.rashod) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=3 ),"+
                "( select max(s.dop87) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=3 ),"+
                "( select max(case when s.cnt_stage=0 then s.val1 else s.val2 end) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=3 ),"+
                "( select max(s.kod_info) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=3 ),"+
                "( select max(case when s.cnt_stage=0 then s.val1_g else s.val2 end) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=3 ))" +
#else
" (( select max(s.rashod),max(s.dop87),max(case when s.cnt_stage=0 then s.val1 else s.val2 end),max(s.kod_info),max(case when s.cnt_stage=0 then s.val1_g else s.val2 end)" +
                " from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge + 
                "   and s.nzp_type=3 and s.stek=3 ))" +
#endif
                " where pr_rash=1" +
                " and 0<( select count(*) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge + 
                "   and s.nzp_type=3 and s.stek=3 )"
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            ret = ExecSQL(conn_db,
                " update ttt_ans_rsh7 set (rsh_hv,rsh_odn_hv,rsh_kv_hv,kod_info_hv,rsh_kv_hv_g)=" +
#if PG
                " (( select max(s.rashod) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=3 ),"+
                "( select max(s.dop87) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=3 ),"+
                "( select max(case when s.cnt_stage=0 then s.val1 else s.val2 end) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=3 )," +
                "( select max(s.kod_info) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=3 )," +
                "( select max(case when s.cnt_stage=0 then s.val1_g else s.val2 end)" +
                " from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=3 ))" +
#else
  " (( select max(s.rashod),max(s.dop87),max(case when s.cnt_stage=0 then s.val1 else s.val2 end),max(s.kod_info),max(case when s.cnt_stage=0 then s.val1_g else s.val2 end)" +
                " from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge + 
                "   and s.nzp_type=3 and s.stek=3 ))" +
#endif
                " where pr_rash=3" +
                " and 0<( select count(*) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge + 
                "   and s.nzp_type=3 and s.stek=3 )"
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            // вывести расход КАН по л/с
            ret = ExecSQL(conn_db,
                " update ttt_ans_rsh7 set" +
                    " rsh_odn_minus = " +
                    " case when rsh_odn_hv<0 then rsh_odn_hv else 0 end + " +
                    " case when rsh_odn_gv<0 then rsh_odn_gv else 0 end"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            ret = ExecSQL(conn_db, 
                " update ttt_ans_rsh7 set" +
                    " rsh_hv    = case when kod_info_hv in (31,32,33) and rsh_hv<0 then 0 else rsh_hv end," +
                    " rsh_odn_hv= case when kod_info_hv in (31,32,33) then 0 else rsh_odn_hv end," +
                    " rsh_kv_hv = case when kod_info_hv in (31,32,33) then (case when rsh_hv<0 then 0 else rsh_hv end) else rsh_kv_hv end," +
                    " rsh_kv_hv_g = case when kod_info_hv in (31,32,33) then (case when rsh_kv_hv_g<0 then 0 else rsh_kv_hv_g end) else rsh_kv_hv_g end," +

                    " rsh_gv    = case when kod_info_gv in (31,32,33) and rsh_gv<0 then 0 else rsh_gv end," +
                    " rsh_odn_gv= case when kod_info_gv in (31,32,33) then 0 else rsh_odn_gv end," +
                    " rsh_kv_gv = case when kod_info_gv in (31,32,33) then (case when rsh_gv<0 then 0 else rsh_gv end) else rsh_kv_gv end," +
                    " rsh_kv_gv_g = case when kod_info_gv in (31,32,33) then (case when rsh_kv_gv_g<0 then 0 else rsh_kv_gv_g end) else rsh_kv_gv_g end," +

                    " kod_info= case when kod_info_hv in (21,22,23,26)" +
                              " then kod_info_hv" +
                              " else " +
                                " case when kod_info_gv in (21,22,23,26)" +
                                " then kod_info_gv" +
                                " else 69" +
                                " end " +
                              " end "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            // записать расход КАН по л/с
            ret = ExecSQL(conn_db,
                " Update " + rashod.counters_xx +
                " Set (kod_info, rashod, dop87, val1, val4, kf307, pu7kw, val1_g) = (( " +
#if PG
                            " Select kod_info From ttt_ans_rsh7 a " +
                            " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                            " ),"+
                            " (Select rsh_hv+rsh_gv From ttt_ans_rsh7 a " +
                            " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                            " ),"+
                            " (Select rsh_odn_hv+rsh_odn_gv From ttt_ans_rsh7 a " +
                            " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                            " )," +
                            " (Select rsh_kv_hv+rsh_kv_gv From ttt_ans_rsh7 a " +
                            " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                            " )," +
                            " (Select rsh_hv From ttt_ans_rsh7 a " +
                            " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                            " )," +
                            " (Select case when kf307hv>0 then kf307hv else 0 end + case when kf307gv>0 then kf307gv else 0 end From ttt_ans_rsh7 a " +
                            " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                            " )," +
                            " (Select  rsh_odn_minus From ttt_ans_rsh7 a " +
                            " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                            " )," +
                            " (Select rsh_kv_hv_g+rsh_kv_gv_g" +
                            " From ttt_ans_rsh7 a " +
                            " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                            " )" +
                            ") " +
#else
   " Select kod_info, rsh_hv+rsh_gv, rsh_odn_hv+rsh_odn_gv, rsh_kv_hv+rsh_kv_gv, rsh_hv," +
                            " case when kf307hv>0 then kf307hv else 0 end + case when kf307gv>0 then kf307gv else 0 end," +
                            " rsh_odn_minus, rsh_kv_hv_g+rsh_kv_gv_g" +
                            " From ttt_ans_rsh7 a " +
                            " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                            " )) " +
#endif
                " Where nzp_type = 3 and stek = 3 and nzp_serv = 7 and cnt_stage=0 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                  " and 0 < ( Select count(*) From ttt_ans_rsh7 a " +
                            " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                            " ) "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            #endregion коррекция расхода по канализации по ХВС и ГВС

            #region распределенный (учтенный) расход
            //----------------------------------------------------------------
            //распределенный (учтенный) расход
            //----------------------------------------------------------------
            if (!b_calc_kvar)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);

            ret = ExecSQL(conn_db,
                      " Select nzp_dom, nzp_serv, sum(rashod) as rashod " +
#if PG
                      " Into temp ttt_aid_stat "+
#else
#endif
                      " From " + rashod.counters_xx + " a " +
                      " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                    //"   and nzp_counter = 109 "+
                      " Group by 1,2 "
#if PG
#else
                    + " Into temp ttt_aid_stat With no log "
#endif
                    , true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ExecSQL(conn_db, " Create unique index ix_aid44_st on ttt_aid_stat (nzp_dom,nzp_serv) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_stat ", true);

                sql =
                        " Update " + rashod.counters_xx +
                        " Set dlt_calc = dlt_reval + dlt_real_charge + (" +
                                    " Select sum(rashod) From ttt_aid_stat a " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv ) " +
                        " Where nzp_type = 1 and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_aid_stat a " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                    " ) ";

                if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                }
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);

                sql =
                        " Update " + rashod.counters_xx +
                        " Set dlt_cur = val3 - dlt_calc " + //текущая дельта
                        " Where nzp_type = 1 and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge;

                if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                }
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }

            #endregion распределенный (учтенный) расход

            #region записать дельты расходов в перерасчетный месяц для возможности снятия при расчетах ОДН - stek=2
            //----------------------------------------------------------------
            // записать дельты расходов в перерасчетный месяц для возможности снятия при расчетах ОДН - stek=2
            //----------------------------------------------------------------
            // если расчет 1го ЛС, то таблицу  нужно считать!
            if (b_calc_kvar)
                UseDeltaCntsLSForMonth(conn_db, rashod, paramcalc, out ret);
            if (b_is_delta)
                WriteDeltaCntsForMonth(conn_db, rashod, paramcalc, out ret);
            #endregion записать дельты расходов в перерасчетный месяц для возможности снятия при расчетах ОДН - stek=2

            #region в целях учета дельты расходов необходимо, чтобы в counters_xx присутствовали все коммунальные услуги из charge_xx !!!
            //----------------------------------------------------------------
            //в целях учета дельты расходов необходимо, чтобы в counters_xx присутствовали все коммунальные услуги из charge_xx !!!
            //----------------------------------------------------------------

            ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
            //выбрать отсутствующие услуги из charge_xx, которых нет в counters_xx
            ret = ExecSQL(conn_db,
                  " Select "+
#if PG
                        "distinct" +
#else
                        "unique"+
#endif
                  " a.nzp_dom, a.nzp_kvar, b.nzp_serv " +
#if PG
                    " Into temp ttt_aid_stat "+
#else
                    " "+
#endif
                  " From t_selkvar a, " + rashod.charge_xx + " b " +
                  " Where a.nzp_kvar = b.nzp_kvar " +
                  "   and b.nzp_serv in ( Select nzp_serv From " + rashod.paramcalc.pref +
#if PG
                            "_kernel.s_counts ) " +
#else
                            "_kernel:s_counts ) " +
#endif
                  "   and 1 > ( Select count(*) From " + rashod.counters_xx + " c " +
                              " Where c.nzp_kvar = b.nzp_kvar " +
                              "   and c.nzp_serv = b.nzp_serv " +
                              " ) " +
#if PG
                    " "
#else
                    " Into temp ttt_aid_stat With no log "
#endif
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            //вставить с нулями пустую строку расходов
            ret = ExecSQL(conn_db,
                  " Insert into " + rashod.counters_xx + " (nzp_dom, nzp_kvar, nzp_serv, nzp_type, stek, kod_info, dat_s,dat_po ) " +
                  " Select nzp_dom, nzp_kvar, nzp_serv, 3, 3,-961, " + rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po +
                  " From ttt_aid_stat "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                return false;
            }

            #endregion в целях учета дельты расходов необходимо, чтобы в counters_xx присутствовали все коммунальные услуги из charge_xx !!!

            #region Удалить временные таблицы
            DropTempTablesRahod(conn_db, rashod.paramcalc.pref);

            #endregion Удалить временные таблицы

            return true;
        }

    }
    #endregion Функция подсчета расходов

}

#endregion здесь производится подсчет расходов

#endregion Подсчет расходов


