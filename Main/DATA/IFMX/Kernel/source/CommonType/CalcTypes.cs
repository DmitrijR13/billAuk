using System;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;


namespace STCLINE.KP50.IFMX.Kernel.source.CommonType
{
    public class CalcTypes
    {

        #region параметры передачи данных в расчеты
        //----------------------------------------------------------------------------------
        public struct ParamCalc //параметры передачи данных в расчеты
        //----------------------------------------------------------------------------------
        {
            public bool ExistsCounters;//наличие приборов учета для текущей выборки за текущий месяц

            const string pref_Portal = "port";
            public string per_dat_charge
            {
                get
                {
                    if (b_cur)
                        return " and dat_charge is null ";
                    else
                        return " and dat_charge = " + DBManager.MDY(cur_mm, 28, cur_yy);
                }
            }
            /// <summary>
            /// Текущий расчетный месяц
            /// </summary>
            public DateTime CurMonth { get { return  new DateTime(cur_yy,cur_mm,1);} }
            /// <summary>
            /// Рассчитываемый месяц
            /// </summary>
            public DateTime CalcMonth { get { return new DateTime(calc_yy, calc_mm, 1); } }

            public bool enableAvgOnClosedPU { get; set; }
            public int nzp_pack
            {
                get
                {
                    if (b_pack)
                        return nzp_dom;
                    else
                        return 0;
                }
            }
            public string ol_srv;
            public string temp_table;

            public int nzp_pack_saldo;
            public int nzp_reestr;

            public int nzp_kvar;
            public int nzp_user;
            public int num_ls;
            public int nzp_dom;
            public int nzp_area;

            public int count_calc_months;
            /// <summary>
            /// расчетный год
            /// </summary>
            public int calc_yy;

            /// <summary>
            /// расчетный месяц
            /// </summary>
            public int calc_mm;
            public int prev_calc_yy;
            public int prev_calc_mm;

            /// <summary>
            /// текущий расчетный год
            /// </summary>
            public int cur_yy;

            /// <summary>
            /// текущий расчетный месяц
            /// </summary>
            public int cur_mm;

            public string pref;
            public int nzp_wp;

            public bool b_dom_in; // при  выбрке дома использовать in (...)
            public bool list_dom; //признак расчета по списку домов

            public int nzp_key; //уникальный номер задачи в текущей очереди
            //----Первый и последний день расчетного месяца

            //Таблица со списком nzp_pack-ов
            public int nzp_par_pack;
            public string dat_s
            {
                get
                {
                    return DBManager.MDY(calc_mm, 1, calc_yy);
                }
            }
            public string next_dat_s
            {
                get
                {
                    if (calc_mm + 1 == 13)
                        return DBManager.MDY(1, 1, calc_yy + 1);
                    else
                        return DBManager.MDY(calc_mm + 1, 1, calc_yy);
                }
            }
            public string dat_po
            {
                get
                {
                    return DBManager.MDY(calc_mm, DateTime.DaysInMonth(calc_yy, calc_mm), calc_yy);
                }
            }
            public string portal_dat_charge
            {
                get
                {
                    if (isPortal)
                        return DBManager.MDY(cur_mm, 1, calc_yy);
                    else
                    {
                        //в текущем расчете д.б. предыдущий месяц
                        if (cur_mm - 1 == 0)
                            return DBManager.MDY(1, 1, calc_yy - 1);
                        else
                            return DBManager.MDY(cur_mm - 1, 1, calc_yy);
                    }
                }
            }

            //----Признак выборки
            public string where_z
            {
                get
                {
                    if (nzp_kvar > 0)
                    {
                        return "nzp_kvar = " + nzp_kvar;
                    }
                    else if (list_dom) //расчет по списку домов
                    {
                        var baseName = "";
#if PG
                        baseName = "public";
#else 
            baseName = conn_web.Database + "@" + DBManager.getServer(conn_web);
#endif

                        var tableName = baseName + DBManager.tableDelimiter + "list_houses_for_calc";

                        return "nzp_dom in (select nzp_dom from " + tableName +
                            " where nzp_wp=" + nzp_wp + " and nzp_key="
                            + nzp_key + " and nzp_user=" + nzp_user + ")";
                    }
                    else if (nzp_dom > 0 && !b_pack) //поскольку при b_pack=true в nzp_dom лежит nzp_pack !
                    {
                        if (b_dom_in)
                            return "nzp_dom in (select nzp_dom from t_selkvar) ";
                        else
                            return "nzp_dom = " + nzp_dom;
                    }
                    else if (nzp_area > 0)
                    {
                        return "nzp_area > 0";
                    }
                    else
                    {
                        return "nzp_dom > 0 ";
                    }
                }
            }


            //тип таблицы
            //pref_id_bill - пустой, тогда это текущий charge
            // == "port" - порталовский charge_xx
            public int id_bill; //номер записи, для Портала решил, что будет сохраняться только последний расчет в rashod_xx etc.
            public string id_bill_pref; //port - это Портал

            public bool b_again //признак параллельной таблицы:  не будем считать домовые расходы, используем для Портала или других целей
            {
                get
                {
                    return (id_bill_pref != "" || id_bill > 0);
                }
            }
            public string alias_again //alias таблицы (для reval_xx & lnk_charge_xx)
            {
                get
                {
                    string s = ""; //будет charge_04

                    if (b_again)
                    {
                        //s = id_bill.ToString() + id_bill_pref; //будет charge1p_04
                        s = "_" + id_bill_pref; //будет charge_port_04, nedo_port_04
                    }

                    return s;
                }
            }
            public string alias //полный alias таблицы
            {
                get
                {
                    string s = ""; //будет charge_04

                    if (b_reval || b_handl)
                    {
                        //перерасчетный месяц
                        //будет порталовские charge_port1206_04, nedo_port1206_04 или обычный charge1206_04, nedo1206_04
                        s = (cur_yy - 2000).ToString() + cur_mm.ToString("00");
                    }

                    return alias_again + s;
                }
            }
            public string kernel_alias
            {
                get
                {
#if PG
                    string s = pref.Trim() + "_kernel.";
#else
                    string s = pref.Trim() + "_kernel:";
#endif
                    return s;
                }
            }
            public string data_alias
            {
                get
                {
#if PG
                    string s = pref.Trim() + "_data.";
#else
                    string s = pref.Trim() + "_data:";
#endif
                    return s;
                }
            }
            public bool isPortal //признак вызова расчета из Портала (billpro)
            {
                get
                {
                    return (id_bill_pref == pref_Portal);
                }
            }

            //----Признак текущего месяца
            public bool b_cur
            {
                get
                {
                    return (calc_yy == cur_yy && calc_mm == cur_mm);
                }
            }
            public bool b_data; //загрузить charge_cnts при первом расчете

            public bool b_loadtemp; //препарировать таблицы

            //0 - CalcGilXX      101
            //1 - CalcRashod     111
            //2 - CalcNedo       121
            //3 - CalcGkuXX      131
            //4 - CalcChargeXX   141
            //5 - CalcReportXX   200
            //6 - again - заново пересчитать пред. месяц с текущими изменениями (charge2_xx)
            //7 - reval - запись в перерасчетные таблицы
            //8 - must  - учесть must_calc при выборке лицевых счетов 

            //сделать через геттеры по таску!!!
            public bool b_gil;
            public bool b_rashod;
            public bool b_nedo;
            public bool b_gku;
            public bool b_charge;
            public bool b_report;
            public bool b_reval;
            public bool b_must;
            public bool b_pack, b_packOt, b_packDel;
            public bool b_handl;

            public bool b_refresh;

            public DateTime DateOper;
            public string dat_oper
            {
                get
                {
                    return "'" + DateOper.ToShortDateString() + "'";
                }
            }
            public string between_dat_oper
            {
                get
                {
                    int y = DateOper.Year;
                    int m = DateOper.Month;
                    int days = DateTime.DaysInMonth(y, m);

                    DateTime d1 = new DateTime(y, m, 1);
                    DateTime d2 = new DateTime(y, m, days);
                    return "between '" + d1.ToShortDateString() + "' and '" + d2.ToShortDateString() + "' ";
                }
            }


            public DateTime curd;
            public CalcFonTask calcfon;

            public ParamCalc(int _nzp_kvar, int _nzp_dom_or_pack, string _pref, int _calc_yy, int _calc_mm, int _cur_yy, int _cur_mm) : this()
            {
                calcfon = new CalcFonTask(0);

                nzp_par_pack = 0;
                id_bill_pref = "";
                temp_table = "";
                id_bill = 0;
                nzp_reestr = 0;

                b_gil = true;
                b_rashod = true;
                b_nedo = true;
                b_gku = true;
                b_charge = true;
                b_report = true;

                b_data = false;
                b_reval = false;
                b_must = false;
                b_handl = false;

                b_pack = false;
                b_packOt = false;
                b_packDel = false;
                b_refresh = false;
                b_dom_in = false; // при  выбрке дома использовать in (...)
                list_dom = false;
                curd = DateTime.Now;

                calc_yy = _calc_yy;
                calc_mm = _calc_mm;
                cur_yy = _cur_yy;
                cur_mm = _cur_mm;

                if (cur_yy == 0)
                {
                    cur_yy = Points.CalcMonth.year_;
                    cur_mm = Points.CalcMonth.month_;
                }

                prev_calc_yy = calc_yy;
                prev_calc_mm = calc_mm - 1;
                if (prev_calc_mm == 0)
                {
                    prev_calc_yy = calc_yy - 1;
                    prev_calc_mm = 12;
                }

                nzp_dom = _nzp_dom_or_pack;
                nzp_kvar = _nzp_kvar;
                nzp_user = 0;
                num_ls = 0;
                nzp_area = 0;

                pref = _pref;

                nzp_wp = 0;
                nzp_key = 0;
                foreach (_Point zap in Points.PointList)
                {
                    if (pref == zap.pref)
                    {
                        nzp_wp = zap.nzp_wp;
                        break;
                    }
                }
                ol_srv = "";
                if (Points.IsFabric)
                {
                    foreach (_Server server in Points.Servers)
                    {
                        if (Points.Point.nzp_server == server.nzp_server)
                        {
#if PG
                            ol_srv = "";
#else
                            ol_srv = "@" + server.ol_server;
#endif
                            break;
                        }
                    }
                }

                DateOper = Points.DateOper;

                b_loadtemp = true;
                nzp_pack_saldo = 0;
                count_calc_months = 0;
                ExistsCounters = true;
            }

            public ParamCalc(int _nzp_kvar, int _nzp_dom_or_pack, string _pref, int _calc_yy, int _calc_mm, int _cur_yy, int _cur_mm, int _nzp_user, int _nzp_key = 0, CalcFonTask _calcfon = null) : this()
            {
                calcfon = _calcfon ?? new CalcFonTask(0);

                id_bill_pref = "";
                id_bill = 0;
                temp_table = "";
                nzp_reestr = 0;
                nzp_par_pack = 0;
                b_gil = true;
                b_rashod = true;
                b_nedo = true;
                b_gku = true;
                b_charge = true;
                b_report = true;

                b_data = false;
                b_reval = false;
                b_must = false;
                b_handl = false;

                b_pack = false;
                b_packOt = false;
                b_packDel = false;
                b_refresh = false;
                b_dom_in = false; // при  выбрке дома использовать in (...)
                list_dom = false;
                curd = DateTime.Now;

                calc_yy = _calc_yy;
                calc_mm = _calc_mm;
                cur_yy = _cur_yy;
                cur_mm = _cur_mm;

                if (cur_yy == 0)
                {
                    cur_yy = Points.CalcMonth.year_;
                    cur_mm = Points.CalcMonth.month_;
                }

                prev_calc_yy = calc_yy;
                prev_calc_mm = calc_mm - 1;
                if (prev_calc_mm == 0)
                {
                    prev_calc_yy = calc_yy - 1;
                    prev_calc_mm = 12;
                }

                nzp_dom = _nzp_dom_or_pack;
                nzp_kvar = _nzp_kvar;
                nzp_user = _nzp_user;
                num_ls = 0;
                nzp_area = 0;

                pref = _pref;
                nzp_wp = 0;
                nzp_key = _nzp_key;
                foreach (_Point zap in Points.PointList)
                {
                    if (pref == zap.pref)
                    {
                        nzp_wp = zap.nzp_wp;
                        break;
                    }
                }
                ol_srv = "";
                if (Points.IsFabric)
                {
                    foreach (_Server server in Points.Servers)
                    {
                        if (Points.Point.nzp_server == server.nzp_server)
                        {
#if PG
                            ol_srv = "";
#else
                            ol_srv = "@" + server.ol_server;
#endif
                            break;
                        }
                    }
                }

                DateOper = Points.DateOper;

                b_loadtemp = true;
                nzp_pack_saldo = 0;
                nzp_par_pack = 0;
                count_calc_months = 0;
                ExistsCounters = true;
            }
        }
        #endregion параметры передачи данных в расчеты

        //расчет начислений charge_xx
        //---------------------------------------------------
        public struct ChargeXX
        {
            public ParamCalc paramcalc;

            public string charge_xx;
            public string charge_xx_ishod; //с чем сравнивать при перерасчете

            public string charge_tab;
            public string prev_charge_xx;
            public string prev_charge_tab;
            public string calc_gku_xx;

            public string kvar_calc_tab;
            public string kvar_calc_xx;

            public string lnk_charge_xx;
            public string lnk_tab;

            public string reval_tab;
            public string reval_xx;
            public string delta_tab;
            public string delta_xx;

            public string calc_nedo_xx;
            public string fn_supplier;
            public string del_supplier;
            public string from_supplier;
            public string perekidka;
            public string report_xx;
            public string report_xx_dom;

            public string where_report, charge_cnts, charge_nedo, charge_g, charge_cnts_prev, charge_nedo_prev, counters_vals;
            public string counters_xx;

            public string where_kvar;

            public ChargeXX(ParamCalc _paramcalc)
            {
                paramcalc = _paramcalc;
                paramcalc.b_dom_in = true;
                string cur_bd = paramcalc.pref + "_charge_" + (paramcalc.cur_yy - 2000).ToString("00");
                string calc_bd = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00");

                lnk_tab = "lnk_charge" + paramcalc.alias_again + "_" + paramcalc.cur_mm.ToString("00");
                lnk_charge_xx = cur_bd + DBManager.tableDelimiter + lnk_tab;

                reval_tab = "reval" + paramcalc.alias_again + "_" + paramcalc.cur_mm.ToString("00");
                reval_xx = cur_bd + DBManager.tableDelimiter + reval_tab;

                delta_tab = "delta" + paramcalc.alias_again + "_" + paramcalc.cur_mm.ToString("00");
                delta_xx = cur_bd + DBManager.tableDelimiter + delta_tab;

                counters_xx = calc_bd + DBManager.tableDelimiter + "counters" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                calc_nedo_xx = calc_bd + DBManager.tableDelimiter + "nedo" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                fn_supplier = calc_bd + DBManager.tableDelimiter + "fn_supplier" + paramcalc.calc_mm.ToString("00");
                from_supplier = calc_bd + DBManager.tableDelimiter + "from_supplier";
                del_supplier = calc_bd + DBManager.tableDelimiter + "del_supplier";
                perekidka = calc_bd + DBManager.tableDelimiter + "perekidka";

                prev_charge_tab = "charge_" + paramcalc.prev_calc_mm.ToString("00");
                prev_charge_xx = paramcalc.pref + "_charge_" + (paramcalc.prev_calc_yy - 2000).ToString("00") + DBManager.tableDelimiter + prev_charge_tab;

                charge_tab = "charge" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");
                charge_xx = calc_bd + DBManager.tableDelimiter + charge_tab;

                charge_xx_ishod = calc_bd + DBManager.tableDelimiter + "charge_" + paramcalc.calc_mm.ToString("00"); //с чем сравнивать при  перерасчете
                calc_gku_xx = calc_bd + DBManager.tableDelimiter + "calc_gku" + paramcalc.alias + "_" + paramcalc.calc_mm.ToString("00");

                kvar_calc_tab = "kvar_calc" + paramcalc.alias_again + "_" + paramcalc.calc_mm.ToString("00");
                kvar_calc_xx = calc_bd + DBManager.tableDelimiter + kvar_calc_tab;

                where_report = " and month_ = " + paramcalc.calc_mm + " and nzp_wp = " + paramcalc.nzp_wp;

                counters_vals = calc_bd + DBManager.tableDelimiter + "counters_vals ";
                charge_cnts = calc_bd + DBManager.tableDelimiter + "charge_cnts ";
                charge_nedo = calc_bd + DBManager.tableDelimiter + "charge_nedo ";
                charge_g = calc_bd + DBManager.tableDelimiter + "charge_g ";

                string calc_bd_prev = paramcalc.pref + "_charge_" + (paramcalc.cur_yy - 2000).ToString("00");
                if (paramcalc.cur_mm == 1)
                    calc_bd_prev = paramcalc.pref + "_charge_" + (paramcalc.cur_yy - 2001).ToString("00");

                charge_cnts_prev = calc_bd_prev + DBManager.tableDelimiter + "charge_cnts ";
                charge_nedo_prev = calc_bd_prev + DBManager.tableDelimiter + "charge_nedo ";

#if PG
                string ol_srv = "";
#else
                string ol_srv = "";
                if (Points.IsFabric)
                {
                    foreach (_Server server in Points.Servers)
                    {
                        if (Points.Point.nzp_server == server.nzp_server)
                        {
                            ol_srv = "@" + server.ol_server;
                            break;
                        }
                    }
                }
#endif

                report_xx = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + ol_srv + DBManager.tableDelimiter + "fn_ukrgucharge ";
                report_xx_dom = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + ol_srv + DBManager.tableDelimiter + "fn_ukrgudom ";

                where_kvar = " nzp_kvar in ( Select nzp_kvar From t_selkvar)";
                if (paramcalc.nzp_kvar > 0)
                    where_kvar = " nzp_kvar = " + paramcalc.nzp_kvar;
            }
        }


        public struct PackXX
        //---------------------------------------------------
        {
            public CalcTypes.ParamCalc paramcalc;

            public string fn_pa_tab;
            public string fn_pa_xx
            {
                get
                {
                    if (is_local)
#if PG
                        return paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + "." + fn_pa_tab;
#else
                        return paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":" + fn_pa_tab;
#endif
                    else
#if PG
                        return Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + "." + fn_pa_tab;
#else
                        return Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":" + fn_pa_tab;
#endif
                }
            }
            public string fn_distrib_prev
            {
                get
                {
                    if (paramcalc.DateOper.Day == 1)
                    {
                        if (paramcalc.DateOper.Month == 1)
#if PG
                            return Points.Pref + "_fin_" + (paramcalc.calc_yy - 2001).ToString("00") + paramcalc.ol_srv + ".fn_distrib_dom_12";
#else
                            return Points.Pref + "_fin_" + (paramcalc.calc_yy - 2001).ToString("00") + paramcalc.ol_srv + ":fn_distrib_dom_12";
#endif
                        else
#if PG
                            return Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ".fn_distrib_dom_" + (paramcalc.calc_mm - 1).ToString("00");
#else
                            return Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":fn_distrib_dom_" + (paramcalc.calc_mm - 1).ToString("00");
#endif
                    }
                    else
                        return fn_distrib;
                }
            }

            public string fn_supplier_tab;
            public string fn_supplier;
            public string fn_operday_log;
            public string fn_distrib_tab;
            public string fn_distrib;
            public string fn_naud;
            public string fn_perc;
            public string charge_xx;
            public string fn_sended;
            public string fn_reval;

            public string s_bank;
            public string s_payer;
            public string pack;
            public string pack_log;
            public string pack_log_tab;

            public string pack_ls;
            public string where_pack_ls;
            public string where_pack;

            public int nzp_pack_ls;


            public int nzp_pack
            {
                get
                {
                    return paramcalc.nzp_pack;
                }
            }

            public bool is_local;
            public bool all_opermonth;
            public string where_dat_oper
            {
                get
                {
                    if (all_opermonth)
                        return paramcalc.between_dat_oper;
                    else
                        return " = " + paramcalc.dat_oper;
                }
            }

            public PackXX(CalcTypes.ParamCalc _paramcalc, int _nzp_pack_ls, bool _local)
            {
                paramcalc = _paramcalc;

                all_opermonth = false;

                nzp_pack_ls = _nzp_pack_ls;
                is_local = _local;

                fn_supplier_tab = "fn" + paramcalc.alias + "_supplier" + paramcalc.calc_mm.ToString("00");
#if PG
                fn_supplier = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + "." + fn_supplier_tab;
                charge_xx = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + ".charge_" + paramcalc.calc_mm.ToString("00");
                pack = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ".pack ";
                pack_ls = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ".pack_ls ";
#else
                fn_supplier = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":" + fn_supplier_tab;
                charge_xx = paramcalc.pref + "_charge_" + (paramcalc.calc_yy - 2000).ToString("00") + ":charge_" + paramcalc.calc_mm.ToString("00");
                pack = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":pack ";
                pack_ls = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":pack_ls ";
#endif

                pack_log_tab = "pack_log";
#if PG
                pack_log = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + "." + pack_log_tab;
                fn_operday_log = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ".fn_operday_dom_mc";
#else
                pack_log = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":" + pack_log_tab;
                fn_operday_log = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":fn_operday_dom_mc";
#endif
                fn_pa_tab = "fn_pa_dom_" + (paramcalc.calc_mm).ToString("00");

#if PG
                fn_perc = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ".fn_perc_dom";
                fn_naud = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ".fn_naud_dom";
                fn_sended = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ".fn_sended_dom";
                fn_reval = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ".fn_reval_dom";
#else
                fn_perc = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":fn_perc_dom";
                fn_naud = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":fn_naud_dom";
                fn_sended = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":fn_sended_dom";
                fn_reval = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":fn_reval_dom";
#endif

                fn_distrib_tab = "fn_distrib_dom_" + (paramcalc.calc_mm).ToString("00");
#if PG
                fn_distrib = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + "." + fn_distrib_tab;
                s_bank = Points.Pref + "_kernel.s_bank ";
                s_payer = Points.Pref + "_kernel.s_payer ";
#else
                fn_distrib = Points.Pref + "_fin_" + (paramcalc.calc_yy - 2000).ToString("00") + paramcalc.ol_srv + ":" + fn_distrib_tab;
                s_bank = Points.Pref + "_kernel:s_bank ";
                s_payer = Points.Pref + "_kernel:s_payer ";
#endif

                where_pack_ls = "nzp_pack_ls > 0";
                if (nzp_pack_ls > 0)
                    where_pack_ls = "nzp_pack_ls = " + nzp_pack_ls;

                where_pack = "nzp_pack > 0";
                if (nzp_pack > 0)
                    where_pack = "nzp_pack = " + nzp_pack;

            }
        }

        public enum FunctionType
        {
            Payment = 1,
            Perekidki = 2
        }

        public enum CalcSteps
        {
            CalcGil = 1,
            CalcRashod = 2,
            CalcNedo = 3,
            CalcGku = 4,
            CalcPeni = 5,
            CalcCharge = 6,
            PrepareParams = 7,
            PrepareMonthParams = 8,
            Complete = 9

        }
    }
}
