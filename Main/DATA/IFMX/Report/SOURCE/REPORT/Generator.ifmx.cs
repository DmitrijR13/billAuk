using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Threading;


namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public class DbGenerator : DataBaseHead
    //----------------------------------------------------------------------
    {
        /*
        0- Услуга
        1- Поставщик
        2- Тариф
        3- Вход. сальдо
        4- Расчет за месяц
        5- Перерасчет
        6- Изменения
        7- Недопоставка
        8- Оплачено
        9- К оплате
        10-Исход. сальдо
        */

        const int showServ      = 0;
        const int showSupp      = 1;
        const int showTarif     = 2;
        const int showInSaldo   = 3;
        const int showSumTarif  = 4;
        const int showReval     = 5;
        const int showRealCharge = 6;
        const int showNedop     = 7;
        const int showMoney     = 8;
        const int showSumCharge = 9;
        const int showOutSaldo  = 10;
        const int showKvar      = 11;
        const int showDom       = 12;
        const int showArea      = 13;
        const int showGeu       = 14;
        const int showRSumTarif = 15;

        //----------------------------------------------------------------------
        public Returns GenCharge(List<int> lFields, List<int> lServ, int nzp_user, int yy, int mm, bool all_ls, bool sld2)
        //----------------------------------------------------------------------
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }

            //постройка данных
            //SaldoAll(conn_web, conn_db, lFields, null, nzp_user, yy, mm, all_ls, true, out ret);

            
            /*
            lFields = new List<int>();
            lFields.Add(11);
            //lFields.Add(0);
            lFields.Add(1);
            lFields.Add(3);
            lFields.Add(4);
            lFields.Add(10);

            lServ = new List<int>();
            lServ.Add(15);
            */

            /*
            _Service s = new _Service();
            s.nzp_serv = 2;
            lServ.Add(s);
            */

            //закачать список услуг
            /*
            DbSprav dbs = new DbSprav();
            lServ = dbs.LoadService(new Finder(), "ordering", out ret);
            dbs.Close();
            */

            //sld2 = true;

            SaldoAll(conn_web, conn_db, lFields, lServ, nzp_user, yy, mm, all_ls, sld2, out ret);

            conn_db.Close();
            conn_web.Close();

            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            return ret;
        }
        //----------------------------------------------------------------------
        private bool SaldoAll(IDbConnection conn_web, IDbConnection conn_db, List<int> lFields, List<int> lServ, int nzp_user, int yy, int mm, bool all_ls, bool sld2, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string tXX_spls = "t" + nzp_user + "_spls";
#if PG
            string tXX_spls_full =   "public" + "." + tXX_spls;
            string tXX_spdom_full ="public" + ".t" + nzp_user + "_spdom";
#else
   string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;
            string tXX_spdom_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":t" + nzp_user + "_spdom";
#endif

            string tXX_spall = "t" + nzp_user + "_saldoall ";
#if PG
            string tXX_spall_full = "public" + "." + tXX_spall;
#else
     string tXX_spall_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spall;
#endif

            string tXX_spall_2 = "t" + nzp_user + "_saldoall_2 ";

#if PG
           
            string s_ins = "";
            string s_ins_sum = "";
            string s_ins_supp = "";
            string s_ins_serv = "";
            string s_sel = "";
#else
            string s_sel = " Insert into " + tXX_spall_full + "  Select 0 ";
#endif

            string s_sel_sum = "";
            string s_table_sum = "";

            string s_sel_serv = "";
            string s_table_serv = "";
            string s_sel_supp = "";
            string s_table_supp = "";

            int groupby = 1;
            string s_table = " Create table " + tXX_spall + " ( no serial not null ";

            bool b_servList = !(lServ == null || lServ.Count == 0); 
            // bool b_servList = !(lServ == null || lServ.Count == 0); 
            //раскладка по услугам

            //если не выбраны услуги, то t_saldoall_2 не создаем!
            //if (!b_serv) sld2 = false;


            bool b_kvar = false;
            bool b_dom = false;
            bool b_area = false;
            bool b_geu = false;
            bool b_serv = false; //выбраны услуги
            bool b_supp = false; //значит были еще выбраны поставщики
            bool b_summa = false; //выбраны суммы

            
            //для целей тестирования безусловно добавим kvar!!!
            if (!b_kvar)
            {
                //lFields.Add(showKvar); //потом убрать!!!
            }


            //для начала проверим выбран ли лс!
            foreach (int i in lFields)
            {
                if (i == showKvar)
                {
                    b_kvar = true;
                    break;
                }
            }

            string dop_tables = "", dop_usl = "";

            //поготовить список выбираемых полей для запроса и создании таблицы
            foreach (int i in lFields)
            {
                switch (i)
                {
                    case showKvar:
                        {
                            s_sel += ", t.nzp_kvar ";
                            s_table += ", nzp_kvar integer ";
#if PG
                            s_ins += ", nzp_kvar ";
#endif
                            groupby += 1;

                            b_kvar = true;
                            break;
                        }
                    case showDom:
                        {
                           // if (!b_kvar)
                            {
                                s_sel += ", t.nzp_dom ";
                                s_table += ", nzp_dom integer ";
#if PG
                                s_ins += ", nzp_dom ";
#endif
                                groupby += 1;

                                b_dom = true;
                            }

                            break;
                        }
                    case showArea:
                        {
                           // if (!b_kvar)
                            {
                                s_sel += ", area, t.nzp_area ";
                                s_table += ", area char(60), nzp_area integer ";
#if PG
                                s_ins += ", area, nzp_area ";
#endif
                                groupby += 2;
                                dop_tables += ", AREA a";
                                dop_usl += " and a.nzp_area = t.nzp_area ";

                                b_area = true;
                            }

                            break;
                        }
                    case showGeu:
                        {
                           /* if (!b_kvar)*/
                            {
                                s_sel += ", geu, t.nzp_geu ";
                                s_table += ", geu char(60), nzp_geu integer ";
#if PG
                                s_ins += ", geu , nzp_geu ";
#endif
                                groupby += 2;
                                dop_tables += ", GEU g";
                                dop_usl += " and g.nzp_geu = t.nzp_geu ";
                                b_geu = true;
                            }

                            break;
                        }
                    case showServ:
                        {
                            s_sel_serv = ", service, ordering, ch.nzp_serv ";
                            s_table_serv = ", service char(60), ordering integer, nzp_serv integer ";
#if PG
                            s_ins_serv += ", service, ordering, nzp_serv";
#endif
                            groupby += 3;

                            b_serv = true;
                            break;
                        }
                    case showSupp:
                        {
                            s_sel_supp = ", name_supp, ch.nzp_supp ";
                            s_table_supp = ", name_supp char(100), nzp_supp integer ";
#if PG
                            s_ins_supp += ", name_supp , nzp_supp ";
#endif
                            groupby += 2;

                            b_supp = true; //выбраны поставщики
                            break;
                        }
                    case showTarif:
                        {
                            if (b_kvar)
                            {
                                s_sel_sum += ", max(tarif) as tarif ";
                                s_table_sum += ", tarif decimal(14,2) default 0.00 ";
#if PG
                                s_ins_sum += ", tarif ";
#endif
                            }
                            b_summa = true;
                            break;
                        }
                    case showInSaldo:
                        {
                            s_sel_sum += ", sum(sum_insaldo) as sum_insaldo ";
                            s_table_sum += ", sum_insaldo decimal(14,2) default 0.00 ";
#if PG
                            s_ins_sum += ", sum_insaldo ";
#endif
                            b_summa = true;
                            break;
                        }
                    case showSumTarif:
                        {
                            s_sel_sum += ", sum(sum_tarif) as sum_tarif ";
                            s_table_sum += ", sum_tarif decimal(14,2) default 0.00 ";
#if PG
                            s_ins_sum += ", sum_tarif ";
#endif
                            b_summa = true;
                            break;
                        }
                    case showReval:
                        {
                            s_sel_sum += ", sum(reval) as reval ";
                            s_table_sum += ", reval decimal(14,2) default 0.00 ";
#if PG
                            s_ins_sum += ", reval ";
#endif
                            b_summa = true;
                            break;
                        }
                    case showRealCharge:
                        {
                            s_sel_sum += ", sum(real_charge) as real_charge ";
                            s_table_sum += ", real_charge decimal(14,2) default 0.00 ";
#if PG
                            s_ins_sum += ", real_charge";
#endif
                            b_summa = true;
                            break;
                        }
                    case showNedop:
                        {
                            s_sel_sum += ", sum(sum_nedop) as sum_nedop ";
                            s_table_sum += ", sum_nedop decimal(14,2) default 0.00 ";
#if PG
                            s_ins_sum += ", sum_nedop ";
#endif
                            b_summa = true;
                            break;
                        }
                    case showMoney:
                        {
                            s_sel_sum += ", sum(sum_money) as sum_money ";
                            s_table_sum += ", sum_money decimal(14,2) default 0.00 ";
#if PG
                            s_ins_sum += ", sum_money ";
#endif
                            b_summa = true;
                            break;
                        }
                    case showSumCharge:
                        {
                           // if (b_kvar)
                                s_sel_sum += ", sum(sum_charge) as sum_charge ";
                          /*  else
                                s_sel_sum += ", sum(sum_nach) as sum_charge ";*/
                            s_table_sum += ", sum_charge decimal(14,2) default 0.00 ";
#if PG
                            s_ins_sum += ", sum_charge ";
#endif
                            b_summa = true;

                            break;
                        }
                    case showOutSaldo:
                        {
                            s_sel_sum += ", sum(sum_outsaldo) as sum_outsaldo ";
                            s_table_sum += ", sum_outsaldo decimal(14,2) default 0.00 ";
#if PG
                            s_ins_sum += ", sum_outsaldo ";
#endif
                            b_summa = true;
                            break;
                        }
                    case showRSumTarif:
                        {
                            s_sel_sum += ", sum(rsum_tarif) as rsum_tarif ";
                            s_table_sum += ", rsum_tarif decimal(14,2) default 0.00 ";
#if PG
                            s_ins_sum += ", rsum_tarif ";
#endif
                            b_summa = true;
                            break;
                        }
                }
            }

            if (b_servList && !b_serv && sld2) //обязательно добавить выборку услуг, если были переданы услуги
            {
                s_sel_serv = ", service,ordering, ch.nzp_serv ";
                s_table_serv = ", service char(60), ordering integer, nzp_serv integer ";
#if PG
                //air
                s_ins_serv += ", service, ordering, nzp_serv  ";
#endif
                groupby += 3;
                b_serv = true;
            }

            ExecSQL(conn_web, " Drop table " + tXX_spall, false);
            ExecSQL(conn_web, " Drop table " + tXX_spall_2, false);

            if (!b_summa)
            {
                MonitorLog.WriteLog("Не выбраны поля сумм", MonitorLog.typelog.Error, 20, 201, true);
                return false;
            }

            //создать таблицу t_saldoall
            s_table = s_table + s_table_serv + s_table_supp + s_table_sum + ")";
            ret = ExecSQL(conn_web, s_table, true);
            if (!ret.result)
            {
                return false;
            }
#if PG
            ExecSQL(conn_web, " analyze " + tXX_spall, true);
#else
   ExecSQL(conn_web, " Update statistics for table " + tXX_spall, true);
#endif


            string s_serv = " ";

            if (b_servList)
            {
                s_serv = " and ch.nzp_serv in (-888";
                foreach (int serv in lServ)
                {
                    if (serv > 1)
                        s_serv += "," + serv;
                }
                s_serv += ")";
            }

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //заполнение данных t_saldoall
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //if (b_kvar)
            {
                //выборка по лс из charge_xx
#if PG
                s_sel = s_sel + s_sel_serv + s_sel_supp + s_sel_sum +
                        " From " + tXX_spls_full +
                        " t  left outer join CHARGE_XX ch on 	T .nzp_kvar = ch.nzp_kvar,   SERVICES_XX s, SUPPLIER_XX p " +
                        dop_tables +
                        " Where  " +
                        "   ch.nzp_serv > 1 and dat_charge is null " +
                        "   and ch.nzp_serv = s.nzp_serv " +
                        "   and ch.nzp_supp = p.nzp_supp " + dop_usl +
                        s_serv +
                        "   and t.pref = 'PREF_XX'";

                for (int i = 1; i < groupby; i++)
                    s_sel = s_sel + (i == 1 ? " Group by " : ",") + i;
                s_ins = s_ins + s_ins_serv + s_ins_supp + s_ins_sum;
                s_sel = " Insert into " + tXX_spall_full + " (" + s_ins.Substring(1) + ")  Select " + s_sel; 
#else
      s_sel = s_sel + s_sel_serv + s_sel_supp + s_sel_sum +
                    " From " + tXX_spls_full + " t, outer (CHARGE_XX ch, SERVICES_XX s, SUPPLIER_XX p " +dop_tables+")"+
                    " Where t.nzp_kvar = ch.nzp_kvar " +
                    "   and ch.nzp_serv > 1 and dat_charge is null " +
                    "   and ch.nzp_serv = s.nzp_serv " +
                    "   and ch.nzp_supp = p.nzp_supp " +dop_usl+
                        s_serv +
                    "   and t.pref = 'PREF_XX'"+
                    " Group by 1 ";

                for (int i = 2; i <= groupby; i++)
                    s_sel = s_sel + "," + i;
#endif


                // цикл по pref 
                IDataReader reader;
#if PG
                if (!ExecRead(conn_db, out reader, " Select distinct pref From " + tXX_spls_full, true).result)
#else
 if (!ExecRead(conn_db, out reader, " Select unique pref From " + tXX_spls_full, true).result)
#endif
                {
                    return false;
                }
                try
                {
                    //заполнить t_saldoall
                    while (reader.Read())
                    {
                        string pref = (string)(reader["pref"]);
                        pref = pref.Trim();

#if PG
                        string charge_xx = pref + "_charge_" + (yy % 100).ToString("00") + ".charge_" + mm.ToString("00");
                        string services = pref + "_kernel.services ";
                        string supplier = pref + "_kernel.supplier ";
                        string area = pref + "_data.s_area ";
                        string geu = pref + "_data.s_geu ";
                        string s_sql;
                        s_sql = s_sel.Replace("CHARGE_XX", charge_xx);
                        s_sql = s_sql.Replace("SERVICES_XX", services);
                        s_sql = s_sql.Replace("SUPPLIER_XX", supplier);
                        s_sql = s_sql.Replace("AREA", area);
                        s_sql = s_sql.Replace("GEU", geu);
                        s_sql = s_sql.Replace("PREF_XX", pref);
#else
               string charge_xx = pref + "_charge_" + (yy % 100).ToString("00") + ":charge_" + mm.ToString("00");
                        string services = pref + "_kernel:services ";
                        string supplier = pref + "_kernel:supplier ";
                        string area = pref + "_data:s_area ";
                        string geu = pref + "_data:s_geu ";
                        string s_sql;
                        s_sql = s_sel.Replace("CHARGE_XX", charge_xx);
                        s_sql = s_sql.Replace("SERVICES_XX", services);
                        s_sql = s_sql.Replace("SUPPLIER_XX", supplier);
                        s_sql = s_sql.Replace("AREA", area);
                        s_sql = s_sql.Replace("GEU", geu);
                        s_sql = s_sql.Replace("PREF_XX", pref);
#endif

                        ret = ExecSQL(conn_db, s_sql, true);
                        if (!ret.result)
                        {
                            reader.Close();
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

                    string err;
                    if (Constants.Viewerror)
                        err = " \n " + ex.Message;
                    else
                        err = "";

                    MonitorLog.WriteLog("Ошибка заполнения выбранных значений " + err, MonitorLog.typelog.Error, 20, 201, true);

                    return false;
                }

#if PG
                ExecSQL(conn_web, " Create unique index ix1_" + tXX_spall + " on " + tXX_spall + "(no)", true);
                if (b_kvar)
                ExecSQL(conn_web, " Create index ix2_" + tXX_spall + " on " + tXX_spall + "(nzp_kvar)", true);
                if ((s_sel_serv.Trim() != "") && (b_kvar))
                    ExecSQL(conn_web, " Create index ix3_" + tXX_spall + " on " + tXX_spall + "(nzp_kvar,nzp_serv)", true);
                ExecSQL(conn_web, " analyze " + tXX_spall, true);
#else
           ExecSQL(conn_web, " Create unique index ix1_" + tXX_spall + " on " + tXX_spall + "(no)", true);
                ExecSQL(conn_web, " Create index ix2_" + tXX_spall + " on " + tXX_spall + "(nzp_kvar)", true);
                if (s_sel_serv.Trim() != "")
                    ExecSQL(conn_web, " Create index ix3_" + tXX_spall + " on " + tXX_spall + "(nzp_kvar,nzp_serv)", true);
                ExecSQL(conn_web, " Update statistics for table " + tXX_spall, true);
#endif

                //добавить недостающие лс в saldoall
                if (all_ls)
                {

                    ExecSQL(conn_web, " Drop table ttt1_sld ", false);
#if PG
                    if (b_kvar)
                  ExecSQL(conn_web,
                        " Select distinct nzp_kvar Into unlogged ttt1_sld From " + tXX_spls +
                        " Where nzp_kvar not in ( Select nzp_kvar From " + tXX_spall + " ) " , true);
#else
                    ExecSQL(conn_web,
                        " Select unique nzp_kvar From " + tXX_spls +
                        " Where nzp_kvar not in ( Select nzp_kvar From " + tXX_spall + " ) " +
                        " Into temp ttt1_sld With no log "
                        , true);
#endif
                    if (b_kvar)
                    ExecSQL(conn_web,
                        " Insert into " + tXX_spall + " (nzp_kvar) Select nzp_kvar From ttt1_sld "
                        , true);

                    ExecSQL(conn_web, " Drop table ttt1_sld ", false);

#if PG
                    ExecSQL(conn_web, " analyze " + tXX_spall, true);
#else
            ExecSQL(conn_web, " Update statistics for table " + tXX_spall, true);
#endif

                    ExecSQL(conn_web, " Drop table ttt1_sld ", false);
                }


            }
          //  else
            /*{
                //выборка из fn_ukrgudom 
                string charge_xx = Points.Pref + "_fin_" + (yy % 100).ToString("00") + ":fn_ukrgudom ";
                string services  = Points.Pref + "_kernel:services ";
                string supplier  = Points.Pref + "_kernel:supplier ";

                s_sel = s_sel + s_sel_serv + s_sel_supp + s_sel_sum +
                    " From " + tXX_spdom_full + " t, " + charge_xx + " ch, " + services + " s, " + supplier + " p " + 
                    " Where t.nzp_dom = ch.nzp_dom " +
                    "   and ch.nzp_serv > 1 " +
                    "   and ch.year_ = " + yy + 
                    "   and ch.month_ = " + mm +
                    "   and ch.nzp_serv = s.nzp_serv " +
                    "   and ch.nzp_supp = p.nzp_supp " +
                        s_serv +
                    " Group by 1 ";

                for (int i = 2; i <= groupby; i++)
                    s_sel = s_sel + "," + i;

                ret = ExecSQL(conn_db, s_sel, true);
                if (!ret.result)
                {
                    return false;
                }

                ExecSQL(conn_web, " Create unique index ix1_" + tXX_spall + " on " + tXX_spall + "(no)", true);
                if (b_dom)
                    ExecSQL(conn_web, " Create index ix2_" + tXX_spall + " on " + tXX_spall + "(nzp_dom)", true);

                ExecSQL(conn_web, " Update statistics for table " + tXX_spall, true);

                //
            }*/



            


            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //создать и заполнить t_saldoall_2 (если выбраны услуги в t_saldoall)
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            bool b_adres = (b_kvar || b_dom || b_dom || b_area || b_geu || b_supp);
            if (sld2 && b_serv && b_adres)
            {
                //надо заполнить lServ
                if (!b_servList)
                {
                    //
                    //DbSprav dbs = new DbSprav();
                    //lServ = dbs.LoadService(new Finder(), "ordering", out ret);
                    //dbs.Close();

                    //цикл по услугам
                    IDataReader reader;
#if PG
                    if (!ExecRead(conn_web, out reader,
                                   " Select distinct nzp_serv,ordering From " + tXX_spall +
                                   " Where nzp_serv is not null " +
                                   " Order by ordering "
                                   , true).result)
#else
         if (!ExecRead(conn_web, out reader,
                        " Select unique nzp_serv,ordering From " + tXX_spall +
                        " Where nzp_serv is not null " +
                        " Order by ordering "
                        , true).result)
#endif
                    {
                        return false;
                    }
                    try
                    {
                        //заполнить t_saldoall
                        while (reader.Read())
                        {
                            int nzp_serv = (int)(reader["nzp_serv"]);

                            lServ.Add(nzp_serv);
                        }
                        reader.Close();
                    }
                    catch (Exception ex)
                    {
                        reader.Close();

                        ret.result = false;
                        ret.text = ex.Message;

                        string err;
                        if (Constants.Viewerror)
                            err = " \n " + ex.Message;
                        else
                            err = "";

                        MonitorLog.WriteLog("Ошибка заполнения выбранных значений " + err, MonitorLog.typelog.Error, 20, 201, true);

                        return false;
                    }
                }

                s_table =
                    " Create table " + tXX_spall_2 +
                    " ( no serial not null ";

                s_table_supp = "";
                s_table_sum = "";
                s_sel = "#";
#if PG
          s_ins = "";
                s_ins_serv = "";
                s_ins_sum = "";
                s_ins_supp = "";
#else
 
#endif

                
#if PG
            b_kvar = false;
             b_dom = false;
            b_area = false;
          b_geu = false;
          b_serv = false; //выбраны услуги
          b_supp = false; //значит были еще выбраны поставщики
            b_summa = false; //выбраны суммы
#else
 
#endif

                //важен порядок следования столбцов!!
                foreach (int i in lFields)
                {
                    switch (i)
                    {
                        case showKvar:
                            {
                                s_sel += ", nzp_kvar ";
                                s_table += ", nzp_kvar integer ";
#if PG

                                s_ins += ", nzp_kvar   ";
                                b_kvar = true;
#endif
                                break;
                            }
                        case showDom:
                            {
                                if (!b_kvar)
                                {
                                    s_sel += ", nzp_dom ";
                                    s_table += ", nzp_dom integer ";
#if PG
                                    s_ins += ", nzp_dom  ";
                                    b_dom = true;
#endif

                                }
                                break;
                            }
                        case showArea:
                            {
                                if (!b_kvar)
                                {
                                    s_sel += ", area, nzp_area ";
                                    s_table += ", area char(60), nzp_area integer ";
#if PG

                                    s_ins += ", area, nzp_area  ";
                                    b_area = true;
#endif
                                }
                                break;
                            }
                        case showGeu:
                            {
                                if (!b_kvar)
                                {
                                    s_sel += ", geu, nzp_geu ";
                                    s_table += ", geu char(60), nzp_geu integer ";
#if PG

                                    s_ins += ", geu, nzp_geu  ";
                                    b_geu = true;
#endif
                                }
                                break;
                            }
                        case showSupp:
                            {
                                s_sel += ", name_supp, nzp_supp ";
                                s_table += ", name_supp char(100), nzp_supp integer ";
#if PG

                                s_ins += ", name_supp, nzp_supp ";
                                b_supp = true;
#endif
                                break;
                            }
                    }
                }



                foreach (int nzp_serv in lServ)
                {
                    if (nzp_serv < 2)
                        continue;

                    foreach (int i in lFields)
                    {
                        switch (i)
                        {
                            case showTarif:
                                {
                                    if (b_kvar)
                                    {
                                        s_table_sum += ", tarif_" + nzp_serv + " decimal(14,2) default 0.00 ";
                                    }
                                    break;
                                }
                            case showInSaldo:
                                {
                                    s_table_sum += ", sum_insaldo_" + nzp_serv + " decimal(14,2) default 0.00 ";
                                    break;
                                }
                            case showSumTarif:
                                {
                                    s_table_sum += ", sum_tarif_" + nzp_serv + " decimal(14,2) default 0.00 ";
                                    break;
                                }
                            case showReval:
                                {
                                    s_table_sum += ", reval_" + nzp_serv + " decimal(14,2) default 0.00 ";
                                    break;
                                }
                            case showRealCharge:
                                {
                                    s_table_sum += ", real_charge_" + nzp_serv + " decimal(14,2) default 0.00 ";
                                    break;
                                }
                            case showNedop:
                                {
                                    s_table_sum += ", sum_nedop_" + nzp_serv + " decimal(14,2) default 0.00 ";
                                    break;
                                }
                            case showMoney:
                                {
                                    s_table_sum += ", sum_money_" + nzp_serv + " decimal(14,2) default 0.00 ";
                                    break;
                                }
                            case showSumCharge:
                                {
                                    s_table_sum += ", sum_charge_" + nzp_serv + " decimal(14,2) default 0.00 ";
                                    break;
                                }
                            case showOutSaldo:
                                {
                                    s_table_sum += ", sum_outsaldo_" + nzp_serv + " decimal(14,2) default 0.00 ";
                                    break;
                                }
                            case showRSumTarif:
                                {
                                    s_table_sum += ", rsum_tarif_" + nzp_serv + " decimal(14,2) default 0.00 ";
                                    break;
                                }
                        }
                    }
                }

                s_table += s_table_supp + s_table_sum + ")"; //create table t_saldoall_2
                ret = ExecSQL(conn_web, s_table, true);
                if (!ret.result)
                {
                    return false;
                }
#if PG
                ExecSQL(conn_web, " analyze " + tXX_spall_2, true);
                //заполнить строки
                string s_sql = " Insert into " + tXX_spall_2 + " ( " + s_ins.Substring(1)+ " ) " +
                               " Select distinct " + s_ins.Substring(1) + " From " + tXX_spall +
                               " Where 1=1 ";
#else
               ExecSQL(conn_web, " Update statistics for table " + tXX_spall_2, true);
                //заполнить строки
                string s_sql = " Insert into " + tXX_spall_2 + " ( " + s_sel.Replace("#,", " ") + " ) " +
                               " Select unique " + s_sel.Replace("#,", " ") + " From " + tXX_spall + 
                               " Where 1=1 ";
#endif
                //вставим в t_saldoall_2 из t_saldoall 
                ExecByStep(conn_web, tXX_spall, "no", s_sql, 100000, " ", out ret);
                if (!ret.result)
                {
                    return false;
                }

                //построим индексы на t_saldoall_2
#if PG
                ExecSQL(conn_web, " Create unique index ix1_" + tXX_spall_2 + " on " + tXX_spall_2 + "(no)", true);
#else
 ExecSQL(conn_web, " Create unique index ix1_" + tXX_spall_2 + " on " + tXX_spall_2 + "(no)", true);
#endif

                string s_index = "#";

                if (b_kvar) s_index += ",nzp_kvar";
                if (b_dom) s_index += ",nzp_dom";
                if (b_area) s_index += ",nzp_area";
                if (b_geu) s_index += ",nzp_geu";
                if (b_supp) s_index += ",nzp_supp";

              
#if PG
                ExecSQL(conn_web, " Create index ix2_" + tXX_spall_2 + " on " + tXX_spall_2 + "(" + s_ins.Substring(1) + ")", true);
                ExecSQL(conn_web, " analyze " + tXX_spall_2, true);
#else
  ExecSQL(conn_web, " Create index ix2_" + tXX_spall_2 + " on " + tXX_spall_2 + "(" + s_index.Replace("#,", " ") + ")", true);
ExecSQL(conn_web, " Update statistics for table " + tXX_spall_2, true);
#endif



                //начинаем заполнять цифры по услугам: sum_insaldo_2,3...
                string s_update = " Update " + tXX_spall_2 + " Set ";

                string s_where = " Where 1 = 1 ";
                if (b_kvar) s_where += "  and " + tXX_spall + ".nzp_kvar = " + tXX_spall_2 + ".nzp_kvar ";
                if (b_dom)  s_where += "  and " + tXX_spall + ".nzp_dom = " + tXX_spall_2 + ".nzp_dom ";
                if (b_area) s_where += "  and " + tXX_spall + ".nzp_area = " + tXX_spall_2 + ".nzp_area ";
                if (b_geu)  s_where += "  and " + tXX_spall + ".nzp_geu = " + tXX_spall_2 + ".nzp_geu ";
                if (b_supp) s_where += "  and " + tXX_spall + ".nzp_supp = " + tXX_spall_2 + ".nzp_supp ";

                foreach (int nzp_serv in lServ)
                {
                    if (nzp_serv < 2)
                        continue;

                    string s_set = "#";
                    string s_sum = "#";

                    s_sql = "#";

 #if PG
                    bool max_tarif_b=false;
                    bool sum_insaldo_b=false;
                    bool sum_tarif_b = false;
                    bool sum_reval_b = false;
                    bool sum_real_charge_b = false;
                    bool sum_nedop_b = false;
                    bool sum_money_b = false;
                    bool sum_charge_b = false;
                    bool sum_outsaldo_b = false;
                    bool rsum_tarif_b = false;
                    
                                    string set_max_tarif = "";
                                    string sel_max_tarif = "";

                                    string set_sum_insaldo = "";
                                    string sel_sum_insaldo = "";

                                    string set_sum_tarif = "";
                                    string sel_sum_tarif = "";

                                    string set_sum_reval = "";
                                    string sel_sum_reval = "";

                                    string set_sum_real_charge = "";
                                    string sel_sum_real_charge = "";

                                    string set_sum_nedop = "";
                                    string sel_sum_nedop = "";

                                    string set_sum_money = "";
                                    string sel_sum_money = "";

                                    string set_sum_charge = "";
                                    string sel_sum_charge = "";

                                    string set_sum_outsaldo = "";
                                    string sel_sum_outsaldo = "";

                                    string set_rsum_tarif = "";
                                    string sel_rsum_tarif = "";


#else
 
#endif


                    foreach (int i in lFields)
                    {
                        switch (i)
                        {
                            case showTarif:
                                {
                                    if (b_kvar)
                                    {
                                        s_sum += ", max(tarif) ";
                                        s_set += ", tarif_" + nzp_serv;

#if PG
                                        max_tarif_b = true;
                                          set_max_tarif = " tarif_" + nzp_serv;
                                          sel_max_tarif = " max(tarif) ";
#else
 
#endif
                                    }
                                    break;
                                }
                            case showInSaldo:
                                {
                                    s_sum += ", sum(sum_insaldo)";
                                    s_set += ", sum_insaldo_" + nzp_serv;
#if PG
                                    sum_insaldo_b= true;
                                      set_sum_insaldo = " sum_insaldo_" + nzp_serv;
                                      sel_sum_insaldo = " sum(sum_insaldo)";
#else
#endif
                                    break;
                                }
                            case showSumTarif:
                                {
                                    s_sum += ", sum(sum_tarif)";
                                    s_set += ", sum_tarif_" + nzp_serv;
#if PG
                                    sum_tarif_b = true;
                                      set_sum_tarif = " sum_tarif_" + nzp_serv;
                                      sel_sum_tarif = "  sum(sum_tarif)";
#else
#endif
                                    break;
                                }
                            case showReval:
                                {
                                    s_sum += ", sum(reval)";
                                    s_set += ", reval_" + nzp_serv;
#if PG
                                    sum_reval_b = true;
                                      set_sum_reval = " reval_" + nzp_serv;
                                      sel_sum_reval = " sum(reval)";
#else
#endif
                                    break;
                                }
                            case showRealCharge:
                                {
                                    s_sum += ", sum(real_charge)";
                                    s_set += ", real_charge_" + nzp_serv;
#if PG
                                    sum_real_charge_b = true;
                                      set_sum_real_charge = " real_charge_" + nzp_serv;
                                      sel_sum_real_charge = " sum(real_charge)";
#else
#endif
                                    break;
                                }
                            case showNedop:
                                {
                                    s_sum += ", sum(sum_nedop)";
                                    s_set += ", sum_nedop_" + nzp_serv;
#if PG
                                    sum_nedop_b = true;
                                      set_sum_nedop = " sum_nedop_" + nzp_serv;
                                      sel_sum_nedop = " sum(sum_nedop)";
#else
#endif
                                    break;
                                }
                            case showMoney:
                                {
                                    s_sum += ", sum(sum_money)";
                                    s_set += ", sum_money_" + nzp_serv;
#if PG
                                    sum_money_b = true;
                                      set_sum_money = " sum_money_" + nzp_serv;
                                      sel_sum_money = " sum(sum_money)";
#else
#endif
                                    break;
                                }
                            case showSumCharge:
                                {
                                    s_sum += ", sum(sum_charge)";
                                    s_set += ", sum_charge_" + nzp_serv;
#if PG
                                    sum_charge_b = true;
                                      set_sum_charge = " sum_charge_" + nzp_serv;
                                      sel_sum_charge = " sum(sum_charge)";
#else
#endif

                                    break;
                                }
                            case showOutSaldo:
                                {
                                    s_sum += ", sum(sum_outsaldo)";
                                    s_set += ", sum_outsaldo_" + nzp_serv;
#if PG
                                    sum_outsaldo_b = true;
                                      set_sum_outsaldo = " sum_outsaldo_" + nzp_serv;
                                      sel_sum_outsaldo = " sum(sum_outsaldo)";
#else
#endif
                                    break;
                                }
                            case showRSumTarif:
                                {
                                    s_sum += ", sum(rsum_tarif)";
                                    s_set += ", rsum_tarif_" + nzp_serv;
#if PG
                                    rsum_tarif_b = true;
                                      set_rsum_tarif = " rsum_tarif_" + nzp_serv;
                                      sel_rsum_tarif = "  sum(rsum_tarif)";
#else
#endif
                                    break;
                                }
                        }
                    }

#if PG
                    s_sql = s_update;
                    string s_sql_from = " from " + tXX_spall + s_where +
                                        "   and " + tXX_spall + ".nzp_serv = " + nzp_serv + ")  ,";

                    if (max_tarif_b)
                    {
                        s_sql += set_max_tarif + "=( Select " + sel_max_tarif + s_sql_from;

                    }


                    if (sum_insaldo_b)
                    {
                        s_sql += set_sum_insaldo + "=( Select " + sel_sum_insaldo + s_sql_from;

                    }


                    if (sum_tarif_b)
                    {
                        s_sql += set_sum_tarif + "=( Select " + sel_sum_tarif + s_sql_from;
                    }

                    if (sum_reval_b)
                    {
                        s_sql += set_sum_reval + "=( Select " + sel_sum_reval + s_sql_from;
                    }

                 

                    if (sum_real_charge_b)
                    {
                        s_sql += set_sum_real_charge + "=( Select " + sel_sum_real_charge + s_sql_from;
                    }
                    if (sum_nedop_b)
                    {
                        s_sql += set_sum_nedop + "=( Select " + sel_sum_nedop + s_sql_from;
                    }

                    if (sum_money_b)
                    {
                        s_sql += set_sum_money + "=( Select " + sel_sum_money + s_sql_from;
                    }
                    if (sum_charge_b)
                    {
                        s_sql += set_sum_charge + "=( Select " + sel_sum_charge + s_sql_from;
                    }

                    if (sum_outsaldo_b)
                    {
                        s_sql += set_sum_outsaldo + "=( Select " + sel_sum_outsaldo + s_sql_from;
                    }
                    if (rsum_tarif_b)
                    {
                        s_sql += set_rsum_tarif + "=( Select " + sel_rsum_tarif + s_sql_from;
                    }




                    s_sql =  s_sql.Substring(0,s_sql.Length-1) +
                             "  Where 0 < ( Select count(*) From " + tXX_spall + s_where +
                             "   and " + tXX_spall + ".nzp_serv = " + nzp_serv + " )  ";
               
#else
                    s_sql += ",(" + s_set.Replace("#,", " ") + ") = (( " +
                        " Select " + s_sum.Replace("#,", " ") +
                        " From " + tXX_spall + s_where +
                        "   and " + tXX_spall + ".nzp_serv = " + nzp_serv +
                        " ))";
                    s_sql = s_update + s_sql.Replace("#,", " ") +
                        " Where 0 < ( Select count(*) From " + tXX_spall + s_where +
                                    "   and " + tXX_spall + ".nzp_serv = " + nzp_serv + " )  ";
#endif


                    ExecByStep(conn_web, tXX_spall_2, "no", s_sql, 10000, " ", out ret);
                    if (!ret.result)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
