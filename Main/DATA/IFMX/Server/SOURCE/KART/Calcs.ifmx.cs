using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.DataBase;
using System.Collections;
using System.Data;

namespace STCLINE.KP50.DataBase
{
    public class DBCalcs : DataBaseHead
    {
#if PG
        private readonly string pgDefaultDb = "public";
#else
#endif

         public void FindCalcs(int nzp_user, string date_year, string date_month, out Returns ret, string numt)
        {
            ret = Utils.InitReturns();
            if (nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return;
            }
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

#if PG
             ExecSQL(conn_web, "set search_path to public", false);
#endif

            string tXX_calcs = "t" + nzp_user + "_calcs_" + date_month + "_" + date_year;

            //удаляем если есть такая таблица в базе
            if (TempTableInWebCashe(conn_web, sDefaultSchema+tXX_calcs))
            {
                ExecSQL(conn_web, " Drop table " + sDefaultSchema + tXX_calcs, false);
            }


            //создать таблицу webdata:tXX_calc
            try
            {
#if PG
                ret = ExecSQL(conn_web,
                          " Create table " + sDefaultSchema+tXX_calcs +
                          " ( nzp_calc serial primary key , " +
                          "   nzp_dom  integer not null, " +
                          "   nzp_kvar integer,  " +
                          "   adr      char(50), " +
                          "   serv     char(50), " +
                          "   ed_serv  char(50), " +
                          "   val1     char(50)," +
                          "   val2     char(50), " +
                      
#else
                ret = ExecSQL(conn_web,
                          " Create table webdb." + tXX_calcs +
                          " ( nzp_calc serial primary key , " +
                          "   nzp_dom  integer not null, " +
                          "   nzp_kvar integer,  " +
                          "   adr      char(50), " +
                          "   serv     char(50), " +
                          "   ed_serv  char(50), " +
                          "   val1     char(50)," +
                          "   val2     char(50), " +                       
#endif
                          "   val3     char(50), " +
                          "   squ1     char(50), " +
                          "   squ2     char(50), " +
                          "   cnt1     char(50), " +
                          "   gil1     char(50)," +
                          "   dlt_calc char(50)," +
                          "   kf307    char(50) " +
                          " ) ", true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при создании таблицы calcs_mm_yy" + ex.Message, MonitorLog.typelog.Error, true);
            }
            //if (ret.result) { ret = ExecSQL(conn_web, " Create index webdb.ix_calc_2 on webdb." + tXX_calcs + " (nzp_dom) ", true); }
            //if (ret.result) { ret = ExecSQL(conn_web, " Update statistics for table " + tXX_calcs + " ", true); }
            
            //else
            //{
            //    conn_web.Close();
            //    return;
            //}



            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            IDataReader reader;
            string sql = "";

            //считываем префиксы
#if PG
            string sql_part_one = pgDefaultDb;
#else
            string sql_part_one = conn_web.Database + "@" + DBManager.getServer(conn_web);
#endif
            string sql_part_two = ": t" + nzp_user + "_spls ";

            string table_in = sql_part_one + ": " + tXX_calcs;
            string t_spls = sql_part_one + sql_part_two;

#if PG
            ExecRead(conn_db, out reader, "SELECT distinct pref from " + t_spls, true);
#else
            ExecRead(conn_db,out reader,"SELECT unique pref from " + t_spls,true);
#endif

            while (reader.Read())
            {
                if (reader["pref"] == DBNull.Value) 
                    continue;

                string pref = reader["pref"].ToString().Trim();

#if PG
                sql +=" Insert into " + table_in + " (nzp_dom, nzp_kvar, adr, serv, ed_serv , val1, val2, val3, squ1, squ2, cnt1, gil1, dlt_calc, kf307)" +
                      " Select distinct l.nzp_dom, l.nzp_kvar, adr, s. service_name, s. ed_izmer, c. val1, c. val2, c. val3, c. squ1, c. squ2, c. cnt1, c. gil1, c. dlt_calc, c. kf307 " +
                      " From " + pref + "_charge_" + date_year + ". counters_" + date_month +
                      " c, " + sql_part_one + sql_part_two + "l, " + Points.Pref + "_kernel. services s" +
                      " Where c.nzp_dom = l.nzp_dom AND s. nzp_serv = c.nzp_serv AND Stek =3 AND nzp_type=1 AND c.nzp_kvar = 0 AND dat_charge is null AND l.mark = 1";   
#else
                sql +=" Insert into " + table_in + " (nzp_dom, nzp_kvar, adr, serv, ed_serv , val1, val2, val3, squ1, squ2, cnt1, gil1, dlt_calc, kf307)" +
                      " Select unique l.nzp_dom, l.nzp_kvar, adr, s. service_name, s. ed_izmer, c. val1, c. val2, c. val3, c. squ1, c. squ2, c. cnt1, c. gil1, c. dlt_calc, c. kf307 " +
                      " From " + pref + "_charge_" + date_year + ": counters_" + date_month +
                      " c, " + sql_part_one + sql_part_two + "l, " + Points.Pref + "_kernel: services s" +
                      " Where c.nzp_dom = l.nzp_dom AND s. nzp_serv = c.nzp_serv AND Stek =3 AND nzp_type=1 AND c.nzp_kvar = 0 AND dat_charge is null AND l.mark = 1";   
#endif
            }
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки pref" + sql, MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                conn_web.Close();
                ret.result = false;
                return;
            }
            conn_db.Close();
            conn_web.Close();
        }

         //----------------------------------------------------------------------
         public void CloseDomCalcs(string sql, IDbConnection conn_web, IDbConnection conn_db)
         {
            MonitorLog.WriteLog("Ошибка выборки в FindDomCalcs " + sql, MonitorLog.typelog.Error, 20, 201, true);
            conn_db.Close();
            conn_web.Close();
         }
         public void CloseGrPUCalcs(string sql, IDbConnection conn_web, IDbConnection conn_db)
         {
             MonitorLog.WriteLog("Ошибка выборки в FindGrPuCalcs " + sql, MonitorLog.typelog.Error, 20, 201, true);
             conn_db.Close();
             conn_web.Close();
         }


         public void FindDomCalcs(Calcs finder, out Returns ret, string numt)
         //----------------------------------------------------------------------
         {
             string mm = finder.month_.ToString("00");
             string yy = (finder.year_ % 100).ToString("00");

             ret = Utils.InitReturns();
             if (finder.nzp_user < 1)
             {
                 ret.result = false;
                 ret.text = "Не определен пользователь";
                 return;
             }
             IDbConnection conn_web = GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);
             ret = OpenDb(conn_web, true);
             if (!ret.result) return;

             string tXX_calcs = "t" + finder.nzp_user + "_calcs_" + mm + "_" + yy;

             //удаляем если есть такая таблица в базе
             if (TempTableInWebCashe(conn_web, sDefaultSchema+tXX_calcs))
             {
                 ExecSQL(conn_web, " Drop table " + sDefaultSchema + tXX_calcs, false);
             }

             IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Kernel);
             ret = OpenDb(conn_db, true);
             if (!ret.result)
             {
                 conn_web.Close();
                 return;
             }

             string sql = "";

             //создать таблицу webdata:tXX_calc
#if PG
             string sAlsTbl = "public.";
             //считываем префиксы
             string sql_part_one = pgDefaultDb;
             string sql_part_two = ".t" + finder.nzp_user + "_spls ";
             string table_in = sql_part_one + "." + tXX_calcs;
#else
             string sAlsTbl = "webdb.";
             //считываем префиксы
             string sql_part_one = conn_web.Database + "@" + DBManager.getServer(conn_web);
             string sql_part_two = ": t" + finder.nzp_user.ToString() + "_spls ";
             string table_in = sql_part_one + ": " + tXX_calcs;
#endif
             try
             {
                 ret = ExecSQL(conn_web,

                     " Create table " + sAlsTbl + tXX_calcs +
                     " ( nzp_calc serial primary key , " +                          
                     "   nzp_serv  integer, " +             //Услуга код
                     "   nzp_dom  integer not null, " +     //Дом код
                     "   serv     char(100), " +            //Услуга
                     "   ed_serv  char(50), " +             //Ед. измерения
                     "   val1     char(50)," +              //Нормативный расход
                     "   val2     char(50), " +             //Расход по ИПУ
                     "   val3     char(50), " +             //Расчетный расход по дому (с учетом Пост.344)
                     "   val4     char(50), " +             //Расход по ОДПУ
                     "   squ1     char(50), " +             //Площадь всех лс
                     "   squ2     char(50), " +             //Площадь всех лс без ИПУ
                     "   cls1_rash integer default 0," +    // кол-во ЛС с рассчитанными расходами (по counters_xx)
                     "   cls1      integer default 0," +    // кол-во начисленных ЛС (по calc_gku_xx)
                     "   cnt1     char(50), " +             //Кол-во жильцов
                     "   gil1     char(50)," +              //Кол-во жильцов с учетом временных выбывших
                     "   dlt_calc char(50)," +              //Сумма начисленных расходов по ЛС
                     "   dlt_reval "       + sDecimalType + "(15,7)," +     //Изменение расхода-перерасчет ИПУ
                     "   dlt_real_charge " + sDecimalType + "(15,7)," +     //Изменение расхода-вручную
                     "   kf_dpu_ls "       + sDecimalType + "(15,7)," +     //Расход ОДН
                     "   gl7kw "           + sDecimalType + "(15,7)," +     //Коэффициент П354 для ГКал по ГВС
                     "   kf307    char(50), " +             //Коэффициент П354 для ЛС с ИПУ
                     "   kf307n   char(50), " +             //Коэффициент П354 для ЛС нормативных   
                     "   kod_info integer,  " +             //код расчета ОДН
                     "   is_dpu   integer default 0, " +    //наличие ОДПУ =1, иначе =0
                     "   nzp_measure integer, " +           //код ед.изм.
                     "   val1_calc " + sDecimalType + "(15,7)," +     //сумма начисленных расходов по ЛС по норме
                     "   val2_calc "  + sDecimalType + "(15,7)," +     //сумма начисленных расходов по ЛС по ИПУ
                     "   dop87_calc " + sDecimalType + "(15,7)," +     //сумма начисленных расходов по ЛС на ОДН
                     "   sum_val1_val2 char(50) " +         //суммарный начисленный расход
                     //formula - Способ расчета
                     " ) ", true);
             }
             catch (Exception ex)
             {
                 MonitorLog.WriteLog("Ошибка при создании таблицы calcs_mm_yy" + ex.Message, MonitorLog.typelog.Error, true);
             }
          
             string counters = finder.pref + "_charge_{0}" + tableDelimiter + "counters{1}{2}_{3}";
             string calc_gku = finder.pref + "_charge_{0}" + tableDelimiter + "calc_gku{1}{2}_{3}";
             string where = "";//" and dat_charge is null";
             if (finder.month > 0 && finder.year > 0)
             {
                 if (finder.month == finder.month_ && finder.year == finder.year_)
                 {
                     counters = String.Format(counters, yy, "", "", mm);
                     calc_gku = String.Format(calc_gku, yy, "", "", mm);
                 }
                 else
                 {
                     counters = String.Format(counters, yy, (finder.year%100).ToString("00"), finder.month.ToString("00"), mm);
                     calc_gku = String.Format(calc_gku, yy, (finder.year%100).ToString("00"), finder.month.ToString("00"), mm);
                     where = "";
                     if (!TempTableInWebCashe(conn_db, counters))
                     {
                         conn_db.Close();
                         conn_web.Close();
                         ret.result = true;
                         ret.text = "Нет данных";
                         ret.tag = -1;
                         return;
                     }

                     if (!TempTableInWebCashe(conn_db, calc_gku))
                     {
                         conn_db.Close();
                         conn_web.Close();
                         ret.result = true;
                         ret.text = "Нет данных";
                         ret.tag = -1;
                         return;
                     }
                 }
             }
             else
             {
                 counters = String.Format(counters, yy, "", "", mm);
                 calc_gku = String.Format(calc_gku, yy, "", "", mm);
             }

             // подготовка списка строк по дому
             sql = " Insert into " + table_in + " (nzp_dom, nzp_serv, nzp_measure, serv, ed_serv)" +
                   " Select distinct " +
                     " c.nzp_dom, s.nzp_serv," +
                        " (case when c.stek=39" +
                        " then 4" +
                        " else (case when c.stek=3 and c.nzp_serv=9 then 3 else s.nzp_measure end)" +
                        " end) nzp_measure, " +
                     " s.service_name," +
                        " (case when c.stek=39" +
                        " then 'ГКал'" +
                        " else (case when c.stek=3 and c.nzp_serv=9 then 'куб.м' else s.ed_izmer end)" +
                        " end) ed_izmer " +
                   " From " + counters + " c, " + Points.Pref + "_kernel" + tableDelimiter + "services s " +
                   " Where s.nzp_serv = c.nzp_serv AND c.Stek in (3,39) AND c.nzp_type=1 " +
                   " AND exists (select 1 from " + Points.Pref + "_kernel" + tableDelimiter + "serv_odn d where d.nzp_serv_link=c.nzp_serv) " +
                   " AND c.nzp_kvar = 0 AND c.nzp_dom = " + finder.nzp_dom + where;
             ret = ExecSQL(conn_db, sql, true);
             if (!ret.result) { CloseDomCalcs(sql, conn_web, conn_db); return; }

             sql = " Insert into " + table_in + " (nzp_dom, nzp_serv, nzp_measure, serv, ed_serv)" +
                   " select d.nzp_dom, s.nzp_serv, s.nzp_measure, s.service_name, s.ed_izmer " +
                   " From " + Points.Pref + "_kernel" + tableDelimiter + "services s," + finder.pref + "_data" + tableDelimiter + "dom d " +
                   " Where s.nzp_serv=14 AND d.nzp_dom = " + finder.nzp_dom + where;
             ret = ExecSQL(conn_db, sql, true);
             if (!ret.result) { CloseDomCalcs(sql, conn_web, conn_db); return; }

             // установка параметров расчета и расходов по начисленным расходам
             sql = " Update " + table_in +
                   " set " +
                     " cls1 = z.cls, squ1 = z.squ, squ2=z.squout, cnt1=z.cnt1, gil1=z.gil, dlt_calc=z.rashod," +
                     " val1_calc = z.val1, val2_calc = z.val2, dop87_calc = z.dop87 " +
                   " from (" +
                   " select nzp_serv,count(*) cls, sum(squ) squ, sum(case when is_device in (1,9) then 0 else squ end) squout, sum(gil_g) cnt1, sum(gil) gil,  " +
                   " sum( rashod + (case when dop87>0 then dop87 else 0 end) ) rashod," +
                   " sum(case when is_device in (1,9) then 0 else valm end) val1," +
                   " sum(case when is_device in (1,9) then valm else 0 end) val2," +
                   " sum(dop87) dop87" +
                   " from " + calc_gku +
                   " where stek=3 and nzp_dom=" + finder.nzp_dom + where +
                   " group by 1" +
                   " ) z" +
                   " where " + table_in + ".nzp_serv = z.nzp_serv ";
             ret = ExecSQL(conn_db, sql, true);
             if (!ret.result) { CloseDomCalcs(sql, conn_web, conn_db); return; }

             // установка расхода ГВС от ГКал по начисленным расходам
             sql = " Update " + table_in +
                   " set " +
                     " dlt_calc=z.rashod, val1_calc = z.val1, val2_calc = z.val2, dop87_calc = z.dop87 " +
                   " from (" +
                   " select " +
                   " sum( (case when f.nzp_measure=3 and g.rsh2>0 then g.rashod*g.rsh2 else g.rashod end)" +
                      " + (case when g.dop87>0 then (case when f.nzp_measure=3 and g.rsh2>0 then g.dop87*g.rsh2 else g.dop87 end) else 0 end)) rashod," +
                   " sum( case when g.is_device in (1,9) then 0 else (case when f.nzp_measure=3 and g.rsh2>0 then g.valm*g.rsh2 else g.valm end) end) val1," +
                   " sum( case when g.is_device in (1,9) then (case when f.nzp_measure=3 and g.rsh2>0 then g.valm*g.rsh2 else g.valm end) else 0 end) val2," +
                   " sum( (case when f.nzp_measure=3 and g.rsh2>0 then g.dop87*g.rsh2 else g.dop87 end) ) dop87" +
                   " from " + calc_gku + " g," + Points.Pref + "_kernel" + tableDelimiter + "formuls f" +
                   " where g.nzp_frm=f.nzp_frm and g.stek=3 and g.nzp_serv=9 and g.nzp_dom=" + finder.nzp_dom + where +
                   " ) z" +
                   " where nzp_serv=9 and nzp_measure=4 ";
             ret = ExecSQL(conn_db, sql, true);
             if (!ret.result) { CloseDomCalcs(sql, conn_web, conn_db); return; }

             //  установка расхода ГВС от куб.м по начисленным расходам
             sql = " Update " + table_in +
                   " set " +
                     " dlt_calc=z.rashod, val1_calc = z.val1, val2_calc = z.val2, dop87_calc = z.dop87 " +
                   " from (" +
                   " select " +
                   " sum( (case when f.nzp_measure=4 and g.rsh2>0 then g.rashod/g.rsh2 else g.rashod end)" +
                      " + (case when g.dop87>0 then (case when f.nzp_measure=4 and g.rsh2>0 then g.dop87/g.rsh2 else g.dop87 end) else 0 end)) rashod," +
                   " sum( case when g.is_device in (1,9) then 0 else (case when f.nzp_measure=4 and g.rsh2>0 then g.valm/g.rsh2 else g.valm end) end) val1," +
                   " sum( case when g.is_device in (1,9) then (case when f.nzp_measure=4 and g.rsh2>0 then g.valm/g.rsh2 else g.valm end) else 0 end) val2," +
                   " sum( (case when f.nzp_measure=4 and g.rsh2>0 then g.dop87/g.rsh2 else g.dop87 end) ) dop87" +
                   " from " + calc_gku + " g," + Points.Pref + "_kernel" + tableDelimiter + "formuls f" +
                   " where g.nzp_frm=f.nzp_frm and g.stek=3 and g.nzp_serv=9 and g.nzp_dom=" + finder.nzp_dom + where +
                   " ) z" +
                   " where nzp_serv=9 and nzp_measure=3 ";
             ret = ExecSQL(conn_db, sql, true);
             if (!ret.result) { CloseDomCalcs(sql, conn_web, conn_db); return; }

             // установка учета ОДПУ по расходам дома + переопределение площади, т.к. она режется при расчете ОДН по дням
             sql = " Update " + table_in +
                   " set" +
                     " cls1_rash = z.cls1, val1 = z.val1, val2 = z.val2, val4 = z.val4, val3 = z.val3, " +
                     " kf307 = z.kf307 , kf307n = z.kf307n, kod_info = z.kod_info, kf_dpu_ls = z.kf_dpu_ls, " +
                     " dlt_reval = z.dlt_reval,  dlt_real_charge = z.dlt_real_charge, squ1 = z.squ1 " +
                   " From (" +
                   " select " +
                     " nzp_serv, cls1, val1, val2, (case when cnt_stage in (1,9) then val4 else 0 end) val4, val3, " +
                     " kf307, kf307n, kod_info, kf_dpu_ls, " +
                     " dlt_reval,  dlt_real_charge, squ1 " +
                   " from " + counters +
                   " where nzp_type=1 and stek=3 and nzp_dom=" + finder.nzp_dom + where +
                   " ) z " +
                   " where (case when " + table_in + ".nzp_serv = 14 then 9 else " + table_in + ".nzp_serv end) = z.nzp_serv";
             ret = ExecSQL(conn_db, sql, true);
             if (!ret.result) { CloseDomCalcs(sql, conn_web, conn_db); return; }

             // переопределение площади по ЛС в расходах, т.к. она режется при расчете ОДН по дням
             sql = " Update " + table_in +
                   " set" +
                     " squ2=z.squout " +
                   " From (" +
                   " select " +
                     " nzp_serv, sum(case when cnt_stage in (1,9) then 0 else squ1 end) squout " +
                   " from " + counters +
                   " where nzp_type=3 and stek=3 and nzp_dom=" + finder.nzp_dom + where +
                   " group by 1" +
                   " ) z " +
                   " where (case when " + table_in + ".nzp_serv = 14 then 9 else " + table_in + ".nzp_serv end) = z.nzp_serv";
             ret = ExecSQL(conn_db, sql, true);
             if (!ret.result) { CloseDomCalcs(sql, conn_web, conn_db); return; }

             // установка расхода ОДН по канализации по начисленным расходам (м.б. в доме есть, а начислено = 0!)
             sql = " Update " + table_in +
                   " set kf_dpu_ls = z.rashod_odn " +
                   " From (" +
                   " select sum(case when n.rashod>0 then n.rashod else 0 end) rashod_odn " +
                   " from " + calc_gku + " n left outer join " + Points.Pref + "_kernel" + tableDelimiter + "serv_odn s on n.nzp_serv=s.nzp_serv" +
                   " where s.nzp_serv_link=7 and n.stek=3 and n.nzp_dom=" + finder.nzp_dom + where +
                   " ) z " +
                   " where " + table_in + ".nzp_serv = 7 ";
             ret = ExecSQL(conn_db, sql, true);
             if (!ret.result) { CloseDomCalcs(sql, conn_web, conn_db); return; }

             // установка расхода ОДПУ по расходам дома для пропорционального учета ОДПУ
             sql = " Update " + table_in +
                   " set val4 = z.val_dpu, is_dpu =1 " +
                   " From (" +
                   " select nzp_serv, sum(val1) val_dpu " +
                   " from " + counters +
                   " where nzp_type=1 and stek=1 and nzp_dom=" + finder.nzp_dom + where +
                   " group by 1" +
                   " ) z " +
                   " where " + table_in + ".nzp_serv = z.nzp_serv and " + table_in + ".kod_info>100 ";
             ret = ExecSQL(conn_db, sql, true);
             if (!ret.result) { CloseDomCalcs(sql, conn_web, conn_db); return; }

             // установка расхода ОДПУ по среднему по расходам дома для пропорционального учета ОДПУ
             sql = " Update " + table_in +
                   " set val4 = z.val_sred " +
                   " From (" +
                   " select nzp_serv, sum(val1) val_sred " +
                   " from " + counters +
                   " where nzp_type=1 and stek=2 and nzp_dom=" + finder.nzp_dom + where +
                   " group by 1" +
                   " ) z " +
                   " where " + table_in + ".nzp_serv = z.nzp_serv and " + table_in + ".is_dpu = 0 and " + table_in + ".kod_info>100 ";
             ret = ExecSQL(conn_db, sql, true);
             if (!ret.result) { CloseDomCalcs(sql, conn_web, conn_db); return; }

             // установка учета ОДПУ от ГКал по расходам дома
             sql = " Update " + table_in +
                   " set " +
                     " val1 = z.val1, val2 = z.val2, val4 = z.val4, val3 = z.val3, kf307 = z.kf307 , kf307n = z.kf307n, kod_info = z.kod_info, " +
                     " kf_dpu_ls = z.kf_dpu_ls, dlt_reval= z.dlt_reval, dlt_real_charge= z.dlt_real_charge, gl7kw = z.gl7kw " +
                   " From (" +
                   " select " +
                     " val1*gl7kw val1, val2*gl7kw val2, (case when cnt_stage in (1,9) then val_s else 0 end) val4,val_po val3,kf307,kf307n,kod_info, " +
                     " kf_dpu_ls*gl7kw kf_dpu_ls,cnt_stage,dlt_reval*gl7kw dlt_reval,dlt_real_charge*gl7kw dlt_real_charge,gl7kw " +
                   " from " + counters +
                   " where nzp_serv=9 and nzp_type=1 and stek=39 and kod_info in (21,31) and nzp_dom=" + finder.nzp_dom + where +
                   " ) z " +
                   " where nzp_serv=9 and nzp_measure=4 ";
             ret = ExecSQL(conn_db, sql, true);
             if (!ret.result) { CloseDomCalcs(sql, conn_web, conn_db); return; }

             // установка итогового расхода по начисленным расходам
             sql = " Update " + table_in +
                   " set " +
                   " dlt_calc = (val1_calc + val2_calc + dop87_calc)" + sConvToNum +","+
                   " sum_val1_val2 =" +
                   " ( " + sNvlWord + "(val1" + sConvToNum + ",0)" +
                   " + " + sNvlWord + "(val2" + sConvToNum + ",0)" +
                   " + " + sNvlWord + "(kf_dpu_ls,0))" + sConvToNum;
             ret = ExecSQL(conn_db, sql, true);
             if (!ret.result) { CloseDomCalcs(sql, conn_web, conn_db); return; }

             conn_db.Close();
             conn_web.Close();
         }

         public void FindGrPuCalcs(Calcs finder, out Returns ret, string numt)
         //----------------------------------------------------------------------
         {
             string mm = finder.month_.ToString("00");
             string yy = (finder.year_ % 100).ToString("00");

             ret = Utils.InitReturns();
             if (finder.nzp_user < 1)
             {
                 ret.result = false;
                 ret.text = "Не определен пользователь";
                 return;
             }
             IDbConnection conn_web = GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);
             ret = OpenDb(conn_web, true);
             if (!ret.result) return;  
             
             IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Kernel);
             ret = OpenDb(conn_db, true);
             if (!ret.result)
             {
                 conn_web.Close();
                 return;
             }
             string sql = "", nzp_counters = "";
             IDataReader reader;
             if (finder.nzp_kvar > 0)
             {
                 nzp_counters = " and c.nzp_counter in (select nzp_counter from " + finder.pref + "_data" + tableDelimiter + "counters_link " +
                     " where nzp_kvar = " + finder.nzp_kvar + ")";               
             }

             string tXX_calcs = "t" + finder.nzp_user + "_grpucalcs_" + mm + "_" + yy;

             //удаляем если есть такая таблица в базе
             if (TempTableInWebCashe(conn_web, sDefaultSchema+tXX_calcs))
             {
                 ExecSQL(conn_web, " Drop table " + sDefaultSchema + tXX_calcs, false);
             }

#if PG
             string sAlsTbl = "public.";
             //считываем префиксы
             string sql_part_one = pgDefaultDb;
             string table_in = sql_part_one + "." + tXX_calcs;
#else
             string sAlsTbl = "public.";
             //считываем префиксы
             string sql_part_one = conn_web.Database + "@" + DBManager.getServer(conn_web);
             string table_in = sql_part_one + ": " + tXX_calcs;
#endif

             //создать таблицу webdata:tXX_calc
             try
             {
                 ret = ExecSQL(conn_web,
                     " Create table " + sAlsTbl + tXX_calcs +                         
                     " ( nzp_calc serial primary key , " +
                     "   nzp_dom  integer not null, " +     //код дома
                     "   serv     char(50), " +             //наименование услуги
                     "   nzp_serv  integer, " +             //код услуги
                     "   ed_serv  char(50), " +             //ед. изм.
                     "   val1     char(50)," +              //сумма расходов по ЛС по норме
                     "   val2     char(50), " +             //сумма расходов по ЛС по ИПУ
                     "   val3     char(50), " +             //Расчетный расход по дому (с учетом Пост.344)
                     "   val4     char(50), " +             //Расход по ОДПУ
                     "   squ1     char(50), " +             //Площадь всех лс
                     "   squ2     char(50), " +             //Площадь всех лс без ИПУ
                     "   cls1_rash integer default 0,  " +  //кол-во ЛС с рассчитанными расходами (по counters_xx)
                     "   cls1     integer,  " +             //кол-во начисленных ЛС (по calc_gku_xx)
                     "   cnt1     char(50), " +             //суммарное кол-во жильцов
                     "   gil1     char(50)," +              //суммарное кол-во жильцов с учетом временных выбывших
                     "   dlt_calc char(50)," +              //Сумма начисленных расходов по ЛС
                     "   dlt_reval "       + sDecimalType + "(15,7)," +  //Сумма перерасчетов расходов по ЛС
                     "   dlt_real_charge " + sDecimalType + "(15,7)," +  //Сумма перекидок вручную расходов по ЛС
                     "   kf_dpu_ls "       + sDecimalType + "(15,7)," +  //Расход ОДН
                     "   gl7kw "           + sDecimalType + "(15,7)," +  //Коэффициент П354 для ГКал по ГВС
                     "   kf307    char(50), " +             //Коэффициент П354 для ЛС по ИПУ
                     "   kf307n   char(50), " +             //Коэффициент П354 для ЛС по норме
                     "   kod_info integer,  " +             //код расчета ОДН
                     "   is_dpu integer default 0,  " +     //наличие ГрПУ =1, иначе =0
                     "   is_gkal integer default 0,  " +    //признак ГрПУ от ГКал =1, иначе =0
                     "   nzp_measure integer,  " +          //код ед.изм.
                     "   rsh2 " + sDecimalType + "(15,7) NOT NULL DEFAULT 0.0000000," +  //норма в ГКал на 1 куб.м
                     "   val1_calc "  + sDecimalType + "(15,7)," +  //сумма начисленных расходов по ЛС по норме
                     "   val2_calc "  + sDecimalType + "(15,7)," +  //сумма начисленных расходов по ЛС по ИПУ
                     "   dop87_calc " + sDecimalType + "(15,7)," +  //сумма начисленных расходов по ЛС на ОДН
                     "   nzp_counter integer,  " +          //код ГрПУ
                     "   sum_val1_val2 char(50), " +        //Сумма нормативного расхода, расхода по ИПУ и расхода на ОДН
                     "   num_cnt char(20) " +               //заводской № ГрПУ
                     " ) ", true);
             }
             catch (Exception ex)
             {
                 MonitorLog.WriteLog("Ошибка при создании таблицы grpucalcs_mm_yy" + ex.Message, MonitorLog.typelog.Error, true);
             }
             

             string counters = finder.pref + "_charge_{0}" + tableDelimiter + "counters{1}{2}_{3}";
             string where = "";//" and dat_charge is null";
             string calc_gku = finder.pref + "_charge_{0}" + tableDelimiter + "calc_gku{1}{2}_{3}";
             if (finder.month > 0 && finder.year > 0)
             {
                 if (finder.month == finder.month_ && finder.year == finder.year_)
                 {
                     counters = String.Format(counters, yy, "", "", mm);
                     calc_gku = String.Format(calc_gku, yy, "", "", mm);
                 }
                 else
                 {
                     counters = String.Format(counters, yy, (finder.year % 100).ToString("00"), finder.month.ToString("00"), mm);
                     calc_gku = String.Format(calc_gku, yy, (finder.year % 100).ToString("00"), finder.month.ToString("00"), mm);
                     where = "";
                     if (!TempTableInWebCashe(conn_db, counters))
                     {
                         conn_db.Close();
                         conn_web.Close();
                         ret.result = true;
                         ret.text = "Нет данных";
                         ret.tag = -1;
                         return;
                     }

                     if (!TempTableInWebCashe(conn_db, calc_gku))
                     {
                         conn_db.Close();
                         conn_web.Close();
                         ret.result = true;
                         ret.text = "Нет данных";
                         ret.tag = -1;
                         return;
                     }
                 }
             }
             else
             {
                 counters = String.Format(counters, yy, "", "", mm);
                 calc_gku = String.Format(calc_gku, yy, "", "", mm);
             }

             sql = " Insert into " + table_in + " (nzp_dom, nzp_counter, nzp_serv, nzp_measure, serv, ed_serv, is_gkal, num_cnt)" +
                    " Select c.nzp_dom, c.nzp_counter, s.nzp_serv," +
                        " (case when c.stek=39" +
                        " then 4" +
                        " else (case when c.stek=3 and c.nzp_serv=9 then 3 else s.nzp_measure end)" +
                        " end) nzp_measure, " +
                    " s.service_name," +
                        " (case when c.stek=39" +
                        " then 'ГКал'" +
                        " else (case when c.stek=3 and c.nzp_serv=9 then 'куб.м' else s.ed_izmer end)" +
                        " end) ed_izmer," +
                    "(case when c.stek=39 then 1 else 0 end) is_gkal, cs.num_cnt " +
                    " from " + counters + " c, " +
                        finder.pref + "_data" + tableDelimiter + " counters_spis cs," +
                        Points.Pref + "_kernel" + tableDelimiter + " services s" +
                    " Where s.nzp_serv = c.nzp_serv AND c.stek in (3,39) AND c.nzp_type=2 " +
                    " AND exists (select 1 from " + Points.Pref + "_kernel" + tableDelimiter + "serv_odn d where d.nzp_serv_link=c.nzp_serv) " +
                    " AND c.nzp_dom = " + finder.nzp_dom +
                    " and cs.nzp_counter = c.nzp_counter " + nzp_counters +
                    where;
             ret = ExecSQL(conn_db, sql, true);
             if (!ret.result) { CloseGrPUCalcs(sql, conn_web, conn_db); return; }

             // подготовка списка строк по дому
             sql = " Insert into " + table_in + " (nzp_dom, nzp_counter, nzp_serv, nzp_measure, serv, ed_serv, is_gkal, num_cnt)" +
                    " Select distinct c.nzp_dom, c.nzp_counter,14 nzp_serv, s.nzp_measure, s.service_name, s.ed_izmer, 1 is_gkal, cs.num_cnt " +
                    " from " + counters + " c, " +
                        finder.pref + "_data" + tableDelimiter + " counters_spis cs," +
                        Points.Pref + "_kernel" + tableDelimiter + " services s" +
                    " Where s.nzp_serv=14 and c.nzp_serv=9 AND c.stek=39 AND c.nzp_type=2 " +
                    " AND c.nzp_dom = " + finder.nzp_dom +
                    " and cs.nzp_counter = c.nzp_counter " + nzp_counters +
                    where;
             ret = ExecSQL(conn_db, sql, true);
             if (!ret.result) { CloseGrPUCalcs(sql, conn_web, conn_db); return; }

             // установка параметров расчета и расходов по начисленным расходам
             sql = " Update " + table_in +
                   " set" +
                        " cls1 = z.cls, squ1 = z.squ, squ2=z.squout, cnt1=z.cnt1, gil1=z.gil, dlt_calc=z.rashod," +
                        " val1_calc = z.val1, val2_calc = z.val2, dop87_calc = z.dop87, rsh2= z.rsh2 " +
                   " from (" +
                   " select l.nzp_counter,g.nzp_serv,count(*) cls, sum(g.squ) squ, sum(case when g.is_device in (1,9) then 0 else g.squ end) squout, " +
                   " sum(g.gil_g) cnt1, sum(g.gil) gil, " +
                   " sum(" +
                   " case when f.nzp_measure=4 and g.rsh2>0 and p.is_gkal=0" +
                   "  then g.rashod/g.rsh2 + (case when g.dop87>0 then g.dop87/g.rsh2 else 0 end)" +
                   "  else g.rashod + (case when g.dop87>0 then g.dop87 else 0 end)" +
                   " end" +
                   " ) rashod," +
                   " sum(" +
                   " case when g.is_device in (1,9) " +
                   " then 0 " +
                   " else" +
                   "  case when f.nzp_measure=4 and g.rsh2>0 and p.is_gkal=0" +
                   "  then g.valm/g.rsh2" +
                   "  else g.valm" +
                   "  end" +
                   " end" +
                   " ) val1," +
                   " sum(" +
                   " case when g.is_device in (1,9) " +
                   " then " +
                   "  case when f.nzp_measure=4 and g.rsh2>0 and p.is_gkal=0" +
                   "  then g.valm/g.rsh2" +
                   "  else g.valm" +
                   "  end" +
                   " else 0" + 
                   " end) val2," +
                   " sum(" +
                   "  case when f.nzp_measure=4 and g.rsh2>0 and p.is_gkal=0" +
                   "  then g.dop87/g.rsh2" +
                   "  else g.dop87" +
                   "  end" +
                   " ) dop87," +
                   " max(g.rsh2) rsh2" +
                   " from " + 
                     calc_gku + " g," + finder.pref + "_data" + tableDelimiter + "counters_link l," + 
                     Points.Pref + "_kernel" + tableDelimiter + "formuls f," + table_in + " p" +
                   " where g.stek=3 and l.nzp_kvar=g.nzp_kvar and l.nzp_counter=p.nzp_counter and g.nzp_serv=p.nzp_serv and g.nzp_frm=f.nzp_frm" +
                     " and g.nzp_dom=" + finder.nzp_dom +
                   " group by 1,2" +
                   ") z" +
                   " where " + table_in + ".nzp_counter = z.nzp_counter and " + table_in + ".nzp_serv = z.nzp_serv  ";
             ret = ExecSQL(conn_db, sql, true);
             if (!ret.result) { CloseGrPUCalcs(sql, conn_web, conn_db); return; }

             //установка учета ОДПУ по расходам дома
             sql = " Update " + table_in +
                   " set kf307 = z.kf307 , kf307n = z.kf307n, kod_info = z.kod_info " +
                   " from (" +
                   " select c.nzp_counter,c.nzp_serv, max(c.kf307) kf307, max(c.kf307n) kf307n, max(c.kod_info) kod_info" +
                   " from " + counters + " c " +
                   " where c.nzp_type=2 and c.stek in (3,39) and c.nzp_dom=" + finder.nzp_dom +
                   " group by 1,2" +
                   ") z" +
                   " where " + table_in + ".nzp_counter = z.nzp_counter and " + table_in + ".nzp_serv = z.nzp_serv  ";
             ret = ExecSQL(conn_db, sql, true);
             if (!ret.result) { CloseGrPUCalcs(sql, conn_web, conn_db); return; }

             sql = " Update " + table_in +
                   " set" +
                     " cls1_rash = z.cls1, val1 = z.val1, val2 = z.val2, val3 = z.val3, kf_dpu_ls = z.kf_dpu_ls," +
                     " dlt_reval = z.dlt_reval,  dlt_real_charge = z.dlt_real_charge " +
                   " from (" +
                   " select c.nzp_counter,c.nzp_serv," +
                   "  count(*) cls1," +
                   "  sum(c.val1) val1, " +
                   "  sum(c.val2) val2, " +
                   "  sum(c.val3) val3, " +
                   "  sum(c.kf_dpu_ls) kf_dpu_ls, " +
                   "  sum(c.dlt_reval) dlt_reval,  " +
                   "  sum(c.dlt_real_charge) dlt_real_charge " +
                   " from " + counters + " c " +
                   " where c.nzp_type=2 and c.stek in (3,39) and c.nzp_dom=" + finder.nzp_dom +
                   " group by 1,2" +
                   ") z" +
                   " where " + table_in + ".nzp_counter = z.nzp_counter " +
                     " and (case when " + table_in + ".nzp_serv=14 then 9 else " + table_in + ".nzp_serv end) = z.nzp_serv  ";
             ret = ExecSQL(conn_db, sql, true);
             if (!ret.result) { CloseGrPUCalcs(sql, conn_web, conn_db); return; }

             // перевод их куб.м в Гкал расходов от расчета ОДН
             sql = " Update " + table_in +
                   " set " +
                   "  val1 = val1" + sConvToNum + " * (case when rsh2>0 then rsh2 else 1 end), " +
                   "  val2 = val2" + sConvToNum + " * (case when rsh2>0 then rsh2 else 1 end), " +
                   "  val3 = val3" + sConvToNum + " * (case when rsh2>0 then rsh2 else 1 end), " +
                   "  kf_dpu_ls = kf_dpu_ls * (case when rsh2>0 then rsh2 else 1 end), " +
                   "  dlt_reval = dlt_reval * (case when rsh2>0 then rsh2 else 1 end), "  +
                   "  dlt_real_charge = dlt_real_charge * (case when rsh2>0 then rsh2 else 1 end)" +
                   " where is_gkal=1 and nzp_measure=4 ";
             ret = ExecSQL(conn_db, sql, true);
             if (!ret.result) { CloseGrPUCalcs(sql, conn_web, conn_db); return; }

             // установка расхода ОДН по канализации по начисленным расходам (м.б. в доме есть, а начислено = 0!) 
             // nzp_counter - не используется!!! нет связки! т.е. ВСЕГО по КАН... про ВСЕМ ГрПУ
             sql = " Update " + table_in +
                   " set kf_dpu_ls = z.rashod_odn" +
                   " from (" +
                   " select s.nzp_serv_link,sum(case when n.rashod>0 then n.rashod else 0 end) rashod_odn " +
                   " from " + calc_gku + " n," + finder.pref + "_data" + tableDelimiter + "counters_link l," +
                     Points.Pref + "_kernel" + tableDelimiter + "serv_odn s" +
                   " where n.nzp_serv=s.nzp_serv and l.nzp_kvar=n.nzp_kvar and s.nzp_serv_link=7 and n.stek=3 and n.nzp_dom=" + finder.nzp_dom +
                   " group by 1" +
                   ") z" +
                   " where " + table_in + ".nzp_serv=7 ";
             ret = ExecSQL(conn_db, sql, true);
             if (!ret.result) { CloseGrPUCalcs(sql, conn_web, conn_db); return; }

             //установка расхода ОДПУ по расходам дома для пропорционального учета ОДПУ
             sql = " Update " + table_in +
                   " set val4 = z.val_dpu, is_dpu =1 " +
                   " from (" +
                   " select nzp_counter,nzp_serv, sum(val1) val_dpu" +
                   " from " + counters + " c " +
                   " where c.nzp_type=2 and c.stek in (1,9) and c.nzp_dom=" + finder.nzp_dom +
                   " group by 1,2" +
                   ") z" +
                   " where " + table_in + ".nzp_counter = z.nzp_counter and " + table_in + ".nzp_serv = z.nzp_serv  ";
             ret = ExecSQL(conn_db, sql, true);
             if (!ret.result) { CloseGrPUCalcs(sql, conn_web, conn_db); return; }

             //установка расхода ОДПУ по среднему по расходам дома для пропорционального учета ОДПУ
             sql = " Update " + table_in +
                   " set val4 = z.val_sred " +
                   " from (" +
                   " select nzp_counter,nzp_serv, sum(val1) val_sred" +
                   " from " + counters + " c " +
                   " where c.nzp_type=2 and c.stek=2 and c.nzp_dom=" + finder.nzp_dom +
                   " group by 1,2" +
                   ") z" +
                   " where " + table_in + ".nzp_counter = z.nzp_counter and " + table_in + ".nzp_serv = z.nzp_serv  ";
             ret = ExecSQL(conn_db, sql, true);
             if (!ret.result) { CloseGrPUCalcs(sql, conn_web, conn_db); return; }

             //установка учета ОДПУ от ГКал по расходам дома
             sql = " Update " + table_in +
                   " set " +
                     " val1 = z.val1, val2 = z.val2, val3 = z.val3, kf307 = z.kf307 , kf307n = z.kf307n, kod_info = z.kod_info, " +
                     " kf_dpu_ls = z.kf_dpu_ls, dlt_reval= z.dlt_reval, dlt_real_charge= z.dlt_real_charge, gl7kw = z.gl7kw " +
                   " from (" +
                   " select nzp_counter," +
                     " sum(val1*gl7kw) val1, sum(val2*gl7kw) val2, sum(val_po) val3,max(kf307) kf307,max(kf307n) kf307n,max(kod_info) kod_info," +
                     " sum(kf_dpu_ls*gl7kw) kf_dpu_ls,max(cnt_stage) cnt_stage,sum(dlt_reval*gl7kw) dlt_reval," +
                     " sum(dlt_real_charge*gl7kw) dlt_real_charge,max(gl7kw) gl7kw" +
                   " from " + counters +
                   " where nzp_serv=9 and nzp_type=2 and stek=39 and kod_info in (21,31) and nzp_dom=" + finder.nzp_dom +
                   " group by 1" +
                   ") z" +
                   " where " + table_in + ".nzp_counter = z.nzp_counter and nzp_serv = 9  and nzp_measure = 4 ";
             ret = ExecSQL(conn_db, sql, true);
             if (!ret.result) { CloseGrPUCalcs(sql, conn_web, conn_db); return; }

             // установка итогового расхода по начисленным расходам
             sql = " Update " + table_in +
                   " set " +
                   " dlt_calc = (val1_calc + val2_calc + dop87_calc)" + sConvToNum + "," +
                   " sum_val1_val2 =" +
                   " ( " + sNvlWord + "(val1" + sConvToNum + ",0)" +
                   " + " + sNvlWord + "(val2" + sConvToNum + ",0)" +
                   " + " + sNvlWord + "(kf_dpu_ls,0))" + sConvToNum;
             ret = ExecSQL(conn_db, sql, true);
             if (!ret.result) { CloseGrPUCalcs(sql, conn_web, conn_db); return; }

             conn_db.Close();
             conn_web.Close();
         }

        //----------------------------------------------------------------------
         public DataTable VerificationCalcs(Ls finder, string date_year_from, string date_month_from, string date_year_to, string date_month_to, out Returns ret)
         //----------------------------------------------------------------------
         {
             Utils.setCulture();
             int nzp_user = finder.nzp_user;
             int num_ls = finder.num_ls;
             int nzp_kvar = finder.nzp_kvar;
             string pref = finder.pref;
             MonitorLog.WriteLog("Запуск процедуры работы с базой отчета VerificationCalcs:nzp_kvar - " + finder.nzp_kvar, MonitorLog.typelog.Info, true);
             DataTable Data_Table = new DataTable();
             decimal sum_outsaldo = 0.0M;
             int year_from = Convert.ToInt32(date_year_from);//год с
             int year_to = Convert.ToInt32(date_year_to);//год по
             int month_from = Convert.ToInt32(date_month_from);//месяц с
             int month_to = Convert.ToInt32(date_month_to);//месяц по

             int table_count = 0;

             string temp_year = "20" + year_from.ToString("00");
             int temp_y = Convert.ToInt32(temp_year);

             #region Проверка периода выгрузки

             if (Points.BeginCalc.year_ > temp_y || (Points.BeginWork.year_ == temp_y && Points.BeginWork.month_ > month_from))
             {
                 month_from = Points.BeginCalc.month_;
                 string year_f = Points.BeginWork.year_.ToString().Substring(Points.BeginWork.year_.ToString().Length - 2, 2);
                 year_from = Convert.ToInt32(year_f);
             }

             #endregion

             int pref_count = year_to - year_from;

             if (pref_count == 0)
             {
                 table_count = month_to - month_from;
             }
             if (pref_count >= 1)
             {
                 table_count = 12 - month_from + month_to + 12 * ((pref_count)-1);
             }

             ret = Utils.InitReturns();
             if (nzp_user < 1)
             {
                 ret.result = false;
                 ret.text = "Не определен пользователь!";
                 return null;
             }

             IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);
             ret = OpenDb(conn_db, true);
             if (!ret.result) return null;

             //создаем временные таблицы
             string temp_table_1 = "temp_charge_1" + nzp_user.ToString();
             string temp_table_2 = "temp_charge_2" + nzp_user.ToString();

             //если существуют в базе - удаляем
             try
             {
                 ExecSQL(conn_db, " Drop table " + temp_table_1, false);
                 ExecSQL(conn_db, " Drop table " + temp_table_2, false);
             }
             catch (Exception)
             {
             }
             //создание таблиц temp_charge_1 и temp_charge_2
             try
             {
#if PG
                 ret = ExecSQL(conn_db,
                              " Create temp table " + temp_table_1 +
                              " ( num_ls             INTEGER," +
                              "   dat_mm             INTEGER," +
                              "   dat_yy             INTEGER," +
                              "   sum_insaldo        DECIMAL(14,2), " +
                              "   sum_charge         DECIMAL(14,2), " +
                              "   subsid             DECIMAL(14,2), " +
                              "   sum_outsaldo       DECIMAL(14,2) " +
                              " )", true);
                 ret = ExecSQL(conn_db,
                              " Create temp table " + temp_table_2 +
                              " ( num_ls             INTEGER, " +
                              "   dat_mm             INTEGER," +
                              "   dat_yy             INTEGER," +
                              "   dat_prih           DATE, " +
                              "   sum_prih           DECIMAL(14,2) " +
                              " )", true);
#else
                 ret = ExecSQL(conn_db,
                              " Create temp table " + temp_table_1 +
                              " ( num_ls             INTEGER," +
                              "   dat_mm             INTEGER," +
                              "   dat_yy             INTEGER," +
                              "   sum_insaldo        DECIMAL(14,2), " +
                              "   sum_charge         DECIMAL(14,2), " +
                              "   subsid             DECIMAL(14,2), " +
                              "   sum_outsaldo       DECIMAL(14,2) " +
                              " ) with no log", true);

                 ret = ExecSQL(conn_db,
                              " Create temp table " + temp_table_2 +
                              " ( num_ls             INTEGER, " +
                              "   dat_mm             INTEGER," +
                              "   dat_yy             INTEGER," +
                              "   dat_prih           DATE, " +
                              "   sum_prih           DECIMAL(14,2) " +
                              " ) with no log", true);
#endif
             }

             catch (Exception ex)
             {
                 MonitorLog.WriteLog("Ошибка при создании таблиц  temp_charge: " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                 conn_db.Close();
                 return null;
             }

             IDataReader reader;
             IDataReader reader1;
             string sql = "";

            #region входящее сальдо за предыдущий месяц

             int start_year = 0;
             int start_month = 0;
             if (month_from == 1)
             {
                 start_month = 12;
                 start_year = year_from - 1;
             }
             else {
                 start_month = month_from - 1;
                 start_year = year_from;
             }
#if PG
             sql = " Select SUM(sum_outsaldo) as sum_outsaldo " +
                        " From " + pref + "_charge_" + start_year.ToString("00") + ". charge_" + start_month.ToString("00") +
                        " Where nzp_serv > 1 and nzp_kvar = " + nzp_kvar + " and dat_charge is null GROUP BY num_ls";
#else
             sql = " Select SUM(sum_outsaldo) as sum_outsaldo " +
                        " From " + pref + "_charge_" + start_year.ToString("00") + ": charge_" + start_month.ToString("00") +
                        " Where nzp_serv > 1 and nzp_kvar = " + nzp_kvar + " and dat_charge is null GROUP BY num_ls";
#endif
             ret = ExecRead(conn_db, out reader, sql, true);
             if (!ret.result)
             {
                 MonitorLog.WriteLog("Ошибка выборки данных для расчётов VerificationCalcs " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                 conn_db.Close();
                 return null;
             }
             try
             {
                 if (reader != null)
                 {
                     //temp.Load(reader, LoadOption.OverwriteChanges);//загрузка в DataTable
                     while (reader.Read())
                     {
                         if (reader["sum_outsaldo"] != DBNull.Value) sum_outsaldo = Convert.ToDecimal(reader["sum_outsaldo"]);
                     }
                 }
             }
             catch (Exception ex)
             {
                 MonitorLog.WriteLog("!!!" + ex.Message, MonitorLog.typelog.Error, true);
                 conn_db.Close();
                 reader.Close();
                 return null;
             }

            #endregion

             #region Выгрузка во временные таблицы

             for (int j = 0; j <= table_count; j++)
             
             {
                 if (month_from == 13)
                 {
                     year_from++;
                     month_from = 1;
                 }

                 string point = "";

#if PG
                 point = ".";
#else 
                 point=":";
#endif

                 sql =  " Insert into " + temp_table_1 + " (num_ls, dat_mm, dat_yy, sum_insaldo, sum_charge, subsid , sum_outsaldo)" +
                        " Select num_ls, " + month_from.ToString() +" as dat_mm, " + (year_from+2000).ToString("00") + " as dat_yy, SUM(sum_insaldo),SUM(sum_charge),'0.00', SUM(sum_outsaldo)" +
                        " From " + pref + "_charge_" + year_from.ToString("00") + point+" charge_" + month_from.ToString("00") +
                        " Where nzp_serv > 1 and nzp_kvar = " + nzp_kvar + " and dat_charge is null GROUP BY num_ls";

                 ret = ExecSQL(conn_db, sql, true);
                 if (!ret.result)
                 {
                     MonitorLog.WriteLog("Ошибка при выгрузке расчётов VerificationCalcs " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                     conn_db.Close();
                     return null;
                 }

#if PG
                 sql = " Insert into " + temp_table_2 + " (num_ls, dat_mm, dat_yy, dat_prih, sum_prih)" +
                           " Select num_ls, date_part ('month', dat_month) as dat_mm, date_part ('year', dat_month) as dat_yy, dat_vvod, SUM(g_sum_ls)" +
                           " From " + Points.Pref + "_fin_" + year_from.ToString("00") + ". pack_ls" +
                           " Where num_ls = " + num_ls + " and  date_part('month', dat_uchet) = " + month_from.ToString("00") + " and date_part('year', dat_uchet) = " + "20" + year_from.ToString("00") + " GROUP BY 1,2,3,4 ";
#else
             sql = " Insert into " + temp_table_2 + " (num_ls, dat_mm, dat_yy, dat_prih, sum_prih)" +
                       " Select num_ls, month(dat_month) as dat_mm, year(dat_month) as dat_yy, dat_vvod, SUM(g_sum_ls)" +
                       " From " + Points.Pref + "_fin_" + year_from.ToString("00") + ": pack_ls" +
                       " Where num_ls = " + num_ls + " and month(dat_uchet) = " + month_from.ToString("00") + " and year(dat_uchet) = " + "20" + year_from.ToString("00") + " GROUP BY 1,2,3,4 ";
#endif

                 ret = ExecSQL(conn_db, sql, true);
                 if (!ret.result)
                 {
                     MonitorLog.WriteLog("Ошибка при выгрузке расчётов VerificationCalcs " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                     conn_db.Close();
                     return null;
                 }
                 month_from++;
             }
            
             #endregion

             #region Выборка из временных таблиц

             DataTable calcs = new DataTable();

             sql = " Select num_ls, dat_mm, dat_yy, dat_prih, sum_prih " +
                             " From " + temp_table_2;
             ret = ExecRead(conn_db, out reader1, sql, true);
             if (!ret.result)
             {
                 MonitorLog.WriteLog("Ошибка выборки данных для расчётов VerificationCalcs " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                 conn_db.Close();
                 return null;
             }

             #endregion
             
             try
             {
                 if (reader1 != null)
                 {
                     calcs.Load(reader1, LoadOption.OverwriteChanges);//загрузка в DataTable
                 }

#if PG
                 sql = " Select a.dat_mm, a.dat_yy, (a. sum_insaldo)||'', (a. sum_charge)||'', (a. subsid)||'', coalesce(b. dat_prih, NULL), coalesce(b. sum_prih,0)||'', (a. sum_charge - coalesce(b. sum_prih,0))||'' as dabt, (a. sum_outsaldo)||''" +
                " From " + temp_table_1 + " a, " + temp_table_2 + " b " +
                " Where a.dat_mm = b.dat_mm and a.dat_yy = b.dat_yy order by dat_yy, dat_mm ";
#else
                 sql = " Select a.dat_mm, a.dat_yy, a. sum_insaldo, a. sum_charge, a. subsid , nvl(b. dat_prih,''), nvl(b. sum_prih,0), (a. sum_charge - nvl(b. sum_prih,0)) as dabt, (a. sum_outsaldo) " +
                " From " + temp_table_1 + " a, OUTER " + temp_table_2 + " b " +
                " Where a. dat_mm = b. dat_mm and a. dat_yy = b. dat_yy order by dat_yy, dat_mm ";
#endif
                 ret = ExecRead(conn_db, out reader, sql, true);
                 if (!ret.result)
                 {
                     MonitorLog.WriteLog("Ошибка выборки данных для расчётов VerificationCalcs " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                     conn_db.Close();
                     return null;
                 }

                 Utils.setCulture();
                 if (reader != null)
                 {
                     Data_Table.Load(reader, LoadOption.OverwriteChanges);//загрузка в DataTable
                 }

                 #region перерасчет вх. сальдо, если есть оплаты до этого периода
                 
                 //начало периода
                 DateTime start_date = new DateTime(start_year+2000, start_month, 1).AddMonths(1);
                 for (int i = 0; i < calcs.Rows.Count; i++)
                 {
                     //дата оплаты
                     if (!String.IsNullOrEmpty(calcs.Rows[i][2].ToString()))
                     {
                         DateTime date_month = new DateTime(Convert.ToInt32(calcs.Rows[i][2]), Convert.ToInt32(calcs.Rows[i][1]), 1);
                         if (date_month < start_date)
                         {
                             //перерасчет входящего сальдо
                             sum_outsaldo = sum_outsaldo - Convert.ToDecimal(calcs.Rows[i][4]);
                         }
                     }
                 }

                 #endregion

                 int old_dat = 0;

                 //расчет сальдо нарастающим итогом
                 for (int i = 0; i < Data_Table.Rows.Count; i++)
                 {
                     if (old_dat == (Int32)Data_Table.Rows[i][0] + (Int32)Data_Table.Rows[i][0] * 12)
                     {
                         Data_Table.Rows[i][3] = "0";
                         Data_Table.Rows[i][8] = "0";
                         if (Data_Table.Rows[i][6] != DBNull.Value)
                             Data_Table.Rows[i][7] = -(Decimal)Data_Table.Rows[i][6];
                     }

                     old_dat = (Int32)Data_Table.Rows[i][0] + (Int32)Data_Table.Rows[i][0] * 12;

                     if (i == 0)
                     {
                         Data_Table.Rows[i][8] = sum_outsaldo + Convert.ToDecimal(Data_Table.Rows[i][7]);
                         ret.sql_error = sum_outsaldo.ToString();
                     }
                     else
                     {
                         Data_Table.Rows[i][8] = Convert.ToDecimal(Data_Table.Rows[i - 1][8]) + Convert.ToDecimal(Data_Table.Rows[i][7]);
                     }
                 }
             }
             catch (Exception ex)
             {
                 MonitorLog.WriteLog("!!!" + ex.Message, MonitorLog.typelog.Error, true);
                 conn_db.Close();
                 reader.Close();
                 return null;
             }
             if (reader != null)
             {
                 reader.Close();
             }
             conn_db.Close();
             return Data_Table;
         }



         //----------------------------------------------------------------------
         public DataTable GetStateGilFond(string date_year_from, string date_month_from, string date_year_to, string date_month_to, out Returns ret)
         //----------------------------------------------------------------------
         {
             MonitorLog.WriteLog("Start StateGilFond", MonitorLog.typelog.Info, true);
             DataTable Data_Table = new DataTable();
             ret = Utils.InitReturns();
             
             #region Выполнение запросов


             #endregion

             return Data_Table;
         }
         
        
        //----------------------------------------------------------------------
         public List<Calcs> GetDomCalcsCollection(out Returns ret, Calcs finder)//(out Returns ret, string Nzp_user, string mm, string yy, int rows, int skip)
         //-----------------------------------------------------------------------------------------------
         {    
             //IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
             IDbConnection conn_db = null;
             IDbConnection conn_web = null;
             IDataReader reader = null;
             ret = Utils.InitReturns();
             string sql = "";


             List<Calcs> CalcsCollect = new List<Calcs>();

             try
             {
                 string Nzp_user = finder.nzp_user.ToString();
                 string mm = finder.month_.ToString("00");
                 string yy = (finder.year_ % 100).ToString("00");

                 conn_db = GetConnection(Constants.cons_Kernel);
                 conn_web = GetConnection(Constants.cons_Webdata);

                 ret = OpenDb(conn_web, true);
                 if (!ret.result)
                 {
                     MonitorLog.WriteLog("Расходы по дому : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                     return null;
                 }

                 ret = OpenDb(conn_db, true);
                 if (!ret.result)
                 {
                     MonitorLog.WriteLog("Расходы по дому : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                     return null;
                 }

                 sql =  
                     "select c.* , st.name_small " +
#if PG
                     " from  public.t" + Nzp_user + "_calcs_" + mm + "_" + yy + " c " +
                     " left join " + Points.Pref + "_kernel.s_type_alg st " +
                     " on c.kod_info = st.nzp_type_alg" +
#else
                     "  from  " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":t" + Nzp_user + "_calcs_" + mm + "_" + yy + " c, " +
                     "  outer " + Points.Pref + "_kernel@" + DBManager.getServer(conn_db) + ":  s_type_alg st " +
                     "  where c.kod_info = st.nzp_type_alg " +
#endif
                     " order by nzp_serv,ed_serv";
                 ret = ExecRead(conn_db, out reader, sql, true);
                 if (!ret.result)
                 {
                     MonitorLog.WriteLog("Ошибка выборки " + sql, MonitorLog.typelog.Error, 20, 201, true);                    
                     return null;
                 }

                 if (reader != null)
                 {
                     while (reader.Read())
                     {
                         int kod_info = -1;

                         //заполнение CalcsCollect
                         Calcs calc = new Calcs();
                         if (reader["serv"] != DBNull.Value) calc.service = reader["serv"].ToString().Trim();
                         if (reader["ed_serv"] != DBNull.Value) calc.ed_serv = reader["ed_serv"].ToString().Trim();
                         if (reader["squ1"] != DBNull.Value) calc.squ1 = Math.Round(Convert.ToDecimal(reader["squ1"]), 2);
                         if (reader["squ2"] != DBNull.Value) calc.squ2 = Math.Round(Convert.ToDecimal(reader["squ2"]), 2);
                         if (reader["cnt1"] != DBNull.Value) calc.cnt1 = (int)Math.Round(Convert.ToDecimal(reader["cnt1"]));
                         if (reader["gil1"] != DBNull.Value) calc.gil1 = Math.Round(Convert.ToDecimal(reader["gil1"]), 7);
                         if (reader["dlt_calc"] != DBNull.Value) calc.dlt_calc = Math.Round(Convert.ToDecimal(reader["dlt_calc"]), 7);
                         if (reader["val1"] != DBNull.Value) calc.val1 = Math.Round(Convert.ToDecimal(reader["val1"]), 7);
                         if (reader["val2"] != DBNull.Value) calc.val2 = Math.Round(Convert.ToDecimal(reader["val2"]), 7);
                         if (reader["kf_dpu_ls"] != DBNull.Value) calc.kf_dpu_ls = Math.Round(Convert.ToDecimal(reader["kf_dpu_ls"]), 7);
                         if (reader["sum_val1_val2"] != DBNull.Value) calc.sum_val1_val2 = Math.Round(Convert.ToDecimal(reader["sum_val1_val2"]), 7);
                         if (reader["val4"] != DBNull.Value) calc.val4 = Math.Round(Convert.ToDecimal(reader["val4"]), 7);
                         if (reader["val3"] != DBNull.Value) calc.val3 = Math.Round(Convert.ToDecimal(reader["val3"]), 7);
                         if (reader["dlt_real_charge"] != DBNull.Value) calc.dlt_real_charge = Math.Round(Convert.ToDecimal(reader["dlt_real_charge"]), 7);
                         if (reader["dlt_reval"] != DBNull.Value) calc.dlt_reval = Math.Round(Convert.ToDecimal(reader["dlt_reval"]), 7);
                         if (reader["kf307"] != DBNull.Value) calc.kf307 = Math.Round(Convert.ToDecimal(reader["kf307"]), 7);
                         if (reader["kf307n"] != DBNull.Value) calc.kf307n = Math.Round(Convert.ToDecimal(reader["kf307n"]), 7);
                         if (reader["kod_info"] != DBNull.Value) kod_info = Convert.ToInt32(reader["kod_info"]);
                         if (reader["name_small"] != DBNull.Value)
                         {
                             calc.formula = Convert.ToString(reader["name_small"]);
                         }
                         else
                         {
                             calc.formula = "-";
                         }

                         CalcsCollect.Add(calc);
                     }
                     
                     //ret.tag = count;
                     return CalcsCollect;
                 }
                 else
                 {
                     MonitorLog.WriteLog("ExcelReport : Reader пуст! ", MonitorLog.typelog.Error, true);
                     ret.text = "Reader пуст!";
                     ret.result = false;                    
                     return null;
                 }                          
             }
             catch (Exception ex)
             {
                 MonitorLog.WriteLog("Расходы по дому : " + ex.Message, MonitorLog.typelog.Error, true);

                 ret.result = false;
                 ret.text = ex.Message;
                 return null;
             }
             finally
             {
                 if (reader != null)
                 {
                     reader.Close();
                 }

                 sql.Remove(0, sql.Length);

                 if (conn_db != null)
                 {
                     conn_db.Close();
                 }
                 if (conn_web != null)
                 {
                     conn_web.Close();
                 }
             }             
         }

         public List<Calcs> GetGrpuCalcsCollection(out Returns ret, Calcs finder)//(out Returns ret, string Nzp_user, string mm, string yy, int rows, int skip)
         //-----------------------------------------------------------------------------------------------
         {
             //IDbConnection conn_db = GetConnection(Constants.cons_Webdata);
             IDbConnection conn_db = null;
             IDbConnection conn_web = null;
             IDataReader reader = null;
             ret = Utils.InitReturns();
             string sql = "";


             List<Calcs> CalcsCollect = new List<Calcs>();

             try
             {
                 string Nzp_user = finder.nzp_user.ToString();
                 string mm = finder.month_.ToString("00");
                 string yy = (finder.year_ % 100).ToString("00");

                 conn_db = GetConnection(Constants.cons_Kernel);
                 conn_web = GetConnection(Constants.cons_Webdata);

                 ret = OpenDb(conn_web, true);
                 if (!ret.result)
                 {
                     MonitorLog.WriteLog("Расходы по ГрПу : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                     return null;
                 }

                 ret = OpenDb(conn_db, true);
                 if (!ret.result)
                 {
                     //MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                     MonitorLog.WriteLog("Расходы по ГрПу : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                     return null;
                 }

                 sql = "select  c.* , st.name_small " +
#if PG
                    "from  public.t" + Nzp_user + "_grpucalcs_" + mm + "_" + yy + " c " +
                    " left join " + Points.Pref + "_kernel.s_type_alg st " +
                    " on c.kod_info = st.nzp_type_alg " + 
#else
                    "  from  " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":t" + Nzp_user + "_grpucalcs_" + mm + "_" + yy + " c, " +
                    "  outer " + Points.Pref + "_kernel@" + DBManager.getServer(conn_db) + ":  s_type_alg st " +
                    "  where c.kod_info = st.nzp_type_alg " +
#endif
                 " order by num_cnt,nzp_serv";

                 ret = ExecRead(conn_db, out reader, sql, true);
                 if (!ret.result)
                 {
                     MonitorLog.WriteLog("Ошибка выборки " + sql, MonitorLog.typelog.Error, 20, 201, true);
                     ret.result = false;
                     return null;
                 }

                 if (reader != null)
                 {
                     while (reader.Read())
                     {
                         int kod_info = -1;

                         //заполнение CalcsCollect
                         Calcs calc = new Calcs();
                         if (reader["serv"] != DBNull.Value) calc.service = reader["serv"].ToString().Trim();
                         if (reader["ed_serv"] != DBNull.Value) calc.ed_serv = reader["ed_serv"].ToString().Trim();
                         if (reader["num_cnt"] != DBNull.Value) calc.num_cnt = reader["num_cnt"].ToString().Trim();
                         if (reader["nzp_counter"] != DBNull.Value) calc.nzp_counter = Convert.ToInt32(reader["nzp_counter"]);
                         if (reader["squ1"] != DBNull.Value) calc.squ1 = Math.Round(Convert.ToDecimal(reader["squ1"]), 2);
                         if (reader["squ2"] != DBNull.Value) calc.squ2 = Math.Round(Convert.ToDecimal(reader["squ2"]), 2);
                         if (reader["cnt1"] != DBNull.Value) calc.cnt1 = (int)Math.Round(Convert.ToDecimal(reader["cnt1"]));
                         if (reader["gil1"] != DBNull.Value) calc.gil1 = Math.Round(Convert.ToDecimal(reader["gil1"]));
                         if (reader["dlt_calc"] != DBNull.Value) calc.dlt_calc = Math.Round(Convert.ToDecimal(reader["dlt_calc"]), 4);
                         if (reader["val1"] != DBNull.Value) calc.val1 = Math.Round(Convert.ToDecimal(reader["val1"]), 4);
                         if (reader["val2"] != DBNull.Value) calc.val2 = Math.Round(Convert.ToDecimal(reader["val2"]), 4);
                         if (reader["kf_dpu_ls"] != DBNull.Value) calc.kf_dpu_ls = Math.Round(Convert.ToDecimal(reader["kf_dpu_ls"]), 7);
                         if (reader["sum_val1_val2"] != DBNull.Value) calc.sum_val1_val2 = Math.Round(Convert.ToDecimal(reader["sum_val1_val2"]), 4);
                         if (reader["val4"] != DBNull.Value) calc.val4 = Math.Round(Convert.ToDecimal(reader["val4"]), 4);
                         if (reader["val3"] != DBNull.Value) calc.val3 = Math.Round(Convert.ToDecimal(reader["val3"]), 4);
                         if (reader["dlt_real_charge"] != DBNull.Value) calc.dlt_real_charge = Math.Round(Convert.ToDecimal(reader["dlt_real_charge"]), 4);
                         if (reader["dlt_reval"] != DBNull.Value) calc.dlt_reval = Math.Round(Convert.ToDecimal(reader["dlt_reval"]), 4);
                         if (reader["kf307"] != DBNull.Value) calc.kf307 = Math.Round(Convert.ToDecimal(reader["kf307"]), 7);
                         if (reader["kf307n"] != DBNull.Value) calc.kf307n = Math.Round(Convert.ToDecimal(reader["kf307n"]), 7);
                         if (reader["kod_info"] != DBNull.Value) kod_info = Convert.ToInt32(reader["kod_info"]);
                         if (reader["name_small"] != DBNull.Value)
                         {
                             calc.formula = Convert.ToString(reader["name_small"]);
                         }
                         else
                         {
                             calc.formula = "-";
                         }
                       
                         CalcsCollect.Add(calc);
                     }

                     return CalcsCollect;
                 }
                 else
                 {
                     MonitorLog.WriteLog("ExcelReport : Reader пуст! ", MonitorLog.typelog.Error, true);
                     ret.text = "Reader пуст!";
                     ret.result = false;
                     return null;
                 }
             }
             catch (Exception ex)
             {
                 MonitorLog.WriteLog("Расходы по ГрПу : " + ex.Message, MonitorLog.typelog.Error, true);

                 ret.result = false;
                 ret.text = ex.Message;
                 return null;
             }
             finally
             {
                 if (reader != null)
                 {
                     reader.Close();
                 }

                 sql.Remove(0, sql.Length);

                 if (conn_db != null)
                 {
                     conn_db.Close();
                 }
                 if (conn_web != null)
                 {
                     conn_web.Close();
                 }
             }
         }


        /// <summary>
        /// прроцедура получения КОЛЛЕКЦИИ Начислений по квартире из tXX_calcs_MM_ГГ
        /// </summary>
        public List<Calcs> GetKvarCalcsCollection(out Returns ret, Calcs finder)
        {
            ret = Utils.InitReturns();
            List<Calcs> CalcsCollect = new List<Calcs>();

            IDbConnection conn_db = null;
            IDataReader reader = null;

            StringBuilder sql = new StringBuilder();

            try
            {
                string Nzp_user = finder.nzp_user.ToString();
                string mm = finder.month_.ToString("00");
                string yy = (finder.year_ % 100).ToString("00");

                conn_db = GetConnection(Constants.cons_Webdata);


                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Расходы по квартире : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }

#if PG
                ExecSQL(conn_db, " set search_path to 'public'", false);
                
#endif

#if PG
                sql.Append(" select  c.* , (C .val1::numeric(14,7) + C .val2::numeric(14,7)) AS sum_val1_val2 ");
                sql.Append(" from  t" + Nzp_user + "_kvarcalcs_" + mm + "_" + yy + " c ");
#else
          sql.Append(" select  c.*, (c.val1 + c.val2) AS val4 ");
                sql.Append(" from  t" + Nzp_user + "_kvarcalcs_" + mm + "_" + yy + " c ");
#endif

                ret = ExecRead(conn_db, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);                  
                    ret.result = false;
                    return null;
                }

                if (reader != null)
                {
                    while (reader.Read())
                    //for (int i = 0; i < rows; i++ )
                    {
                        //bool res = reader.Read();
                        //if (res)
                        //{
                        //заполнение CalcsCollect
                        Calcs calc = new Calcs();
                        if (reader["serv"] != DBNull.Value) calc.service = reader["serv"].ToString().Trim();
                        if (reader["nzp_serv"] != DBNull.Value) calc.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                        if (reader["ed_serv"] != DBNull.Value) calc.ed_serv = reader["ed_serv"].ToString().Trim();
                        if (reader["rashod"] != DBNull.Value) calc.rashod = Math.Round(Convert.ToDecimal(reader["rashod"]), 7);
                        if (reader["val1"] != DBNull.Value) calc.val1 = Math.Round(Convert.ToDecimal(reader["val1"]), 7);
                        if (reader["val2"] != DBNull.Value) calc.val2 = Math.Round(Convert.ToDecimal(reader["val2"]), 7);
                        if (reader["val4"] != DBNull.Value) calc.val4 = Math.Round(Convert.ToDecimal(reader["val4"]), 7);
                        if (reader["sum_val1_val2"] != DBNull.Value) calc.sum_val1_val2 = Math.Round(Convert.ToDecimal(reader["sum_val1_val2"]), 7);
                        if (reader["dlt_real_charge"] != DBNull.Value) calc.dlt_real_charge = Math.Round(Convert.ToDecimal(reader["dlt_real_charge"]), 7);
                        if (reader["dlt_reval"] != DBNull.Value) calc.dlt_reval = Math.Round(Convert.ToDecimal(reader["dlt_reval"]), 7);
                         
                        if (reader["squ1"] != DBNull.Value) calc.squ1 = Math.Round(Convert.ToDecimal(reader["squ1"]), 2);
                        if (reader["squ2"] != DBNull.Value) calc.squ2 = Math.Round(Convert.ToDecimal(reader["squ2"]), 2);
                        if (calc.nzp_serv == 8)//отопление
                            calc.squ1 = calc.squ2;
                        if (reader["cnt1"] != DBNull.Value) calc.cnt1 = Convert.ToInt32(reader["cnt1"]);
                        if (reader["gil1"] != DBNull.Value) calc.gil1 = Math.Round(Convert.ToDecimal(reader["gil1"]));
                        if (reader["dop87"] != DBNull.Value) calc.dop87 = Math.Round(Convert.ToDecimal(reader["dop87"]), 7);


                        CalcsCollect.Add(calc);
                        //}
                        //else
                        //{
                        //    break;
                        //}
                    }                    

                    //ret.tag = count;
                    return CalcsCollect;
                }
                else
                {
                    MonitorLog.WriteLog("ExcelReport : Reader пуст! ", MonitorLog.typelog.Error, true);
                    ret.text = "Reader пуст!";
                    ret.result = false;
                    return null;
                }         

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Расходы по квартире : " + ex.Message, MonitorLog.typelog.Error, true);

                ret.result = false;
                ret.text = ex.Message;
                return null;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                sql.Remove(0, sql.Length);
            }                           
        }



        //----------------------------------------------------------------------
        public void FindKvarCalcs(Calcs finder, out Returns ret, string numt)
        //----------------------------------------------------------------------
        {
            string mm = finder.month_.ToString("00");
            string yy = (finder.year_ % 100).ToString("00");
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return;
            }
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

            string tXX_calcs = "t" + finder.nzp_user.ToString() + "_kvarcalcs_" + mm + "_" + yy;

#if PG
            // установить схему public, чтобы правильно отработала функция TableInWebCashe
            ExecSQL(conn_web, " set search_path to 'public'", false);
#endif

            //удаляем если есть такая таблица в базе
            if (TableInWebCashe(conn_web, tXX_calcs))
            {
                ExecSQL(conn_web, " Drop table " + tXX_calcs, false);
            }


            //создать таблицу webdata:tXX_calc
            try
            {
                ret = ExecSQL(conn_web,
#if PG
                          " Create table public." + tXX_calcs +
#else
                          " Create table webdb." + tXX_calcs +
#endif
                          " ( nzp_calc serial primary key , " +
                          "   nzp_dom  integer not null, " +
                          "   nzp_kvar integer,  " +
                          "   nzp_serv integer,  " + 
                          "   serv     char(100), " +
                          "   ed_serv  char(50), " +
                          "   rashod   char(50), " +
                          "   dlt_reval DECIMAL(14,7)," +
                          "   dlt_real_charge DECIMAL(14,7)," +
                          "   dop87    char(50), " +
                          "   val1     char(50), " +
                          "   val2     char(50), " +
                           "   val4     char(50), " +
                          "   squ1     char(50), " +
                          "   squ2     char(50), " +
                          "   cnt1     char(50), " +
                          "   gil1     char(50)" +
                          " ) ", true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при создании таблицы calcs_mm_yy" + ex.Message, MonitorLog.typelog.Error, true);
            }
            //if (ret.result) { ret = ExecSQL(conn_web, " Create index webdb.ix_calc_2 on webdb." + tXX_calcs + " (nzp_dom) ", true); }
            //if (ret.result) { ret = ExecSQL(conn_web, " Update statistics for table " + tXX_calcs + " ", true); }

            //else
            //{
            //    conn_web.Close();
            //    return;
            //}



            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            IDataReader reader;
            string sql = "";

            //считываем префиксы
#if PG
            string sql_part_one = pgDefaultDb;
            string table_in = sql_part_one + "." + tXX_calcs;
#else
            string sql_part_one = conn_web.Database + "@" + DBManager.getServer(conn_web);
          
            string table_in = sql_part_one + ": " + tXX_calcs;
#endif
            string counters = finder.pref + "_charge_{0}" + tableDelimiter + "counters{1}{2}_{3}";
            string where = " and dat_charge is null";
            if (finder.month > 0 && finder.year > 0)
            {
                if (finder.month == finder.month_ && finder.year == finder.year_)
                {
                    counters = String.Format(counters, yy, "", "", mm);
                }
                else
                {
                    counters = String.Format(counters, yy, (finder.year % 100).ToString("00"), finder.month.ToString("00"), mm);
                    where = "";
                    if (!TempTableInWebCashe(conn_db, counters))
                    {
                        conn_db.Close();
                        conn_web.Close();
                        ret.result = true;
                        ret.text = "Нет данных";
                        ret.tag = -1;
                        return;
                    }
                }
            }
            else counters = String.Format(counters, yy, "", "", mm);

#if PG
                sql += " Insert into " + table_in + " (nzp_dom, nzp_kvar, nzp_serv, serv, ed_serv , rashod, dlt_reval, dlt_real_charge, dop87 ,val1, val2, val4, squ1, squ2, cnt1, gil1)" +
                    " Select distinct c.nzp_dom, c.nzp_kvar, c.nzp_serv, s. service_name, m.measure, c. rashod, c.dlt_reval, c.dlt_real_charge, c.dop87 ,c. val1, c. val2, c.val4, c. squ1, c. squ2, c. cnt1, c. gil1" +
                    " From " + counters +
                    " c, " + Points.Pref + "_kernel.services s," +
                     Points.Pref + "_kernel.s_counts sc," +
                     Points.Pref + "_kernel.s_measure m " +
                    " Where  s. nzp_serv = c.nzp_serv and s.nzp_serv = sc.nzp_serv and m.nzp_measure = sc.nzp_measure AND stek =3 and nzp_type=3 and c.nzp_kvar = " + finder.nzp_kvar + where;
#else
                sql += " Insert into " + table_in + " (nzp_dom, nzp_kvar, nzp_serv, serv, ed_serv , rashod, dlt_reval, dlt_real_charge, dop87 ,val1, val2, val4, squ1, squ2, cnt1, gil1)" +
                      " Select unique c.nzp_dom, c.nzp_kvar, c.nzp_serv, s. service_name,s. ed_izmer, c. rashod, c.dlt_reval, c.dlt_real_charge, c.dop87 ,c. val1, c. val2, c.val4, c. squ1, c. squ2, c. cnt1, c. gil1" +
                      " From " + counters +
                      " c, " + Points.Pref + "_kernel: services s" +
                      " Where  s. nzp_serv = c.nzp_serv AND stek =3 and nzp_type=3 and c.nzp_kvar = " + finder.nzp_kvar + where;
#endif
                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки pref" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                //reader.Close();
                sql.Remove(0, sql.Length);
                conn_db.Close();
                conn_web.Close();
                ret.result = false;
                return;
            }
            conn_db.Close();
            conn_web.Close();
        }

       
        //----------------------------------------------------------------------
        public List<DataTable> DebtCalcs(Ls finder, string date_year_from, string date_month_from, string date_year_to, string date_month_to, out Returns ret)
        //----------------------------------------------------------------------
        {
            #region Заполенение необходимых данными

            int nzp_user = finder.nzp_user;
            int num_ls = finder.num_ls;
            int nzp_kvar = finder.nzp_kvar;
            string pref = finder.pref;
            MonitorLog.WriteLog("Start Verf", MonitorLog.typelog.Info, true);

            List<DataTable> F_Data = new List<DataTable>();
 
            int year_from = Convert.ToInt32(date_year_from);//год с
            int year_to = Convert.ToInt32(date_year_to);//год по
            int month_from = Convert.ToInt32(date_month_from);//месяц с
            int month_to = Convert.ToInt32(date_month_to);//месяц по

            int table_count = 0;

            string temp_year = "20" + year_from.ToString();
            int temp_y = Convert.ToInt32(temp_year);

            #endregion

            #region Проверка периода выгрузки

            if (Points.BeginCalc.year_ > temp_y || (Points.BeginCalc.year_ == temp_y && Points.BeginCalc.month_ > month_from))
            {
                month_from = Points.BeginCalc.month_;
                string year_f = Points.BeginCalc.year_.ToString().Substring(Points.BeginCalc.year_.ToString().Length - 2, 2);
                year_from = Convert.ToInt32(year_f);
            }

            #endregion

            #region Счетчик для цикла 

            int pref_count = year_to - year_from;
            
            if (pref_count == 0)
            {
                table_count = month_to - month_from;
            }
            if (pref_count >= 1)
            {
                table_count = 12 - month_from + month_to + 12 * ((pref_count) - 1);
            }

            #endregion

            ret = Utils.InitReturns();
            if (nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь!";
                return null;
            }

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            IDataReader reader;
            reader = null;
            string sql = "";

            #region Выборка

            for (int j = 0; j <= table_count; j++)
            {
                if (month_from == 13)
                {
                    year_from++;
                    month_from = 1;
                }
                string yf = String.Format("{0:00}", year_from);
                string mf = String.Format("{0:00}", month_from);

                #region Первая выгрузка

#if PG
                sql = " Select '" + mf + yf + "', c.service_small, a.tarif, SUM(a.sum_charge), SUM(a.izm_saldo), SUM(a.sum_outsaldo), SUM(a.sum_insaldo) " +
                                   " From " + pref + "_charge_" + yf + ". charge_" + mf + " a, " + "public. services c " +
                                   " Where num_ls = " + num_ls + " And a.nzp_serv = c.nzp_serv And a.nzp_serv > 1" +
                                   " Group by c.service_small, a.tarif";
#else
   sql = " Select '" + mf + yf + "', c.service_small, a.tarif, SUM(a.sum_charge), SUM(a.izm_saldo), SUM(a.sum_outsaldo), SUM(a.sum_insaldo) " +
                      " From " + pref + "_charge_" + yf + ": charge_" + mf + " a, " + conn_web.Database + ": services c " +
                      " Where num_ls = " + num_ls + " And a.nzp_serv = c.nzp_serv And a.nzp_serv > 1" +
                      " Group by c.service_small, a.tarif";
#endif
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка выборки данных DebtCalcs(1) " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    conn_web.Close();
                    return null;
                }

                #endregion

                #region Запись первой выгрузки в DataTable


                System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                culture.NumberFormat.NumberDecimalSeparator = ".";
                culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;

                try
                {
                    if (reader != null)
                    {
                        DataTable Service_Data = new DataTable();
                        Service_Data.Load(reader,LoadOption.OverwriteChanges);
                        F_Data.Add(Service_Data);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при записи данных в DataTable в DebtCalcs(1)" + ex.Message, MonitorLog.typelog.Error, true);
                    conn_db.Close();
                    reader.Close();
                    return null;
                }
                #endregion

                #region Вторая выгрузка

#if PG
                sql = " Select '" + mf + yf + "', f.dat_vvod,  m.payer, SUM(f.g_sum_ls) " +
                                   " From " + pref + "_fin_" + yf + ". pack_ls f," + pref + "_fin_" + yf + ". pack e," +
                                     Points.Pref+"_kernel" + ". s_bank l," + Points.Pref+"_kernel" + ". s_payer m " +
                                   " Where f.num_ls = " + num_ls + " And f.nzp_pack = e.nzp_pack " +
                                   " And  e.nzp_bank = l.nzp_bank And l.nzp_payer = m.nzp_payer " +
                                   " And (extract(MONTH FROM f.dat_uchet) ) = " + Convert.ToInt32(mf) + " and (extract(YEAR FROM f.dat_uchet) ) = " + Convert.ToInt32("20" + yf) +
                                   " Group by f.dat_vvod, m.payer";
#else
   sql = " Select '" + mf + yf + "', f.dat_vvod,  m.payer, SUM(f.g_sum_ls) " +
                      " From " + pref + "_fin_" + yf + ": pack_ls f," + pref + "_fin_" + yf + ": pack e," +
                        conn_db.Database + ": s_bank l," + conn_db.Database + ": s_payer m " +
                      " Where f.num_ls = " + num_ls + " And f.nzp_pack = e.nzp_pack " +
                      " And  e.nzp_bank = l.nzp_bank And l.nzp_payer = m.nzp_payer " +
                      " And month(f.dat_uchet) = " + Convert.ToInt32(mf) + " and year(f.dat_uchet) = " + Convert.ToInt32("20" + yf) +
                      " Group by f.dat_vvod, m.payer";
#endif

                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка выборки данных DebtCalcs(2) " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    conn_web.Close();
                    return null;
                }

                #endregion

                #region Запись второй выгрузки в DataTable
                try
                {
                    if (reader != null)
                    {
                        DataTable Payment_Data = new DataTable();
                        Payment_Data.Load(reader, LoadOption.OverwriteChanges);
                        F_Data.Add(Payment_Data);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при записи данных в DataTable в DebtCalcs(2)" + ex.Message, MonitorLog.typelog.Error, true);
                    conn_db.Close();
                    reader.Close();
                    return null;
                }
                #endregion

                month_from++;
            }

            #endregion

            sql.Remove(0, sql.Length);
            reader.Close();
            conn_db.Close();
            conn_web.Close();
            return F_Data;
        }

        public Returns CheckDatabaseExist(string pref, enDataBaseType en, string year_from, string year_to)
        {
            Returns ret = Utils.InitReturns();
            List<int> years = new List<int>();
            string dt_part = "";
            int y1 = 0, y2 = 0;

            IDbConnection conn_db = null;
            
            try
            {
                switch (en)
                {
                    case enDataBaseType.charge:
                        dt_part = "charge";
                        break;
                    case enDataBaseType.fin:
                        dt_part = "fin";
                        break;
                    default: throw new Exception("Неверное имя базы данных");
                }

                if (!String.IsNullOrEmpty(year_from)) y1 = Convert.ToInt32(year_from);
                if (!String.IsNullOrEmpty(year_to)) y2 = Convert.ToInt32(year_to);

                if (!String.IsNullOrEmpty(pref) && (y1 > 0 || y2 > 0))
                {
                    if (y1 > 0 && y2 > 0)
                    {
                        if (y1 > y2) throw new Exception("Год начала больше года окончания");

                        for (int i = y1; i <= y2; i++)
                        {
                            years.Add(i);
                        }
                    }
                    else if (y1 > 0)
                        years.Add(y1);
                    else
                        years.Add(y2);

                    conn_db = GetConnection(Constants.cons_Kernel);
                    ret = OpenDb(conn_db, true);
                    if (!ret.result) return ret;

                    foreach (int year in years)
                    {
#if PG
                        string sql_check = "set search_path TO '" + pref + "_" + dt_part + "_" + (year % 100).ToString("00") + "'";
#else
string sql_check = "Database " + pref + "_" + dt_part + "_" + (year % 100).ToString("00");
#endif

                        ret = ExecSQL(conn_db, sql_check, true);
                        if (!ret.result)
                        {

#if PG
                            ExecSQL(conn_db, "set search_path TO '" + Points.Pref + "_kernel '", true);
#else
                              ExecSQL(conn_db, "database " + Points.Pref + "_kernel", true);
#endif

                            throw new Exception("Введен некорректный период. Проверьте правильность ввода периода выгрузки.");
                        }
                    }
                }
                else
                {
                    throw new Exception("Неверные входные параметры");
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка в функции CheckDatabaseExist\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (conn_db != null) conn_db.Close();
                conn_db = null;
            }
            return ret;
        }

        //----------------------------------------------------------------------
        public List<DataTable> NoticeCalcs(Ls finder, string date_year, string date_month, out Returns ret)
        //----------------------------------------------------------------------
        {
            #region Заполенение необходимых данными

            int nzp_user = finder.nzp_user;
            int num_ls = finder.num_ls;
            int nzp_kvar = finder.nzp_kvar;
            string pref = finder.pref;

            List<DataTable> Data = new List<DataTable>(); ;

            int year = Convert.ToInt32(date_year);
            int month = Convert.ToInt32(date_month);

            #endregion


            ret = Utils.InitReturns();
            if (nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь!";
                return null;
            }

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;


            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            IDataReader reader;
            reader = null;
            string sql = "";

            #region Выборка

            string yf = String.Format("{0:00}", year);
            string mf = String.Format("{0:00}", month);

            #region Первая выгрузка

#if PG
            sql = " Select c.service_small, a.tarif, SUM(a.rsum_tarif), Max(a.c_sn), d.measure, SUM(a.sum_charge), SUM(a.sum_nedop), " +
                           " SUM(Case when real_charge < 0 then real_charge else 0 end) as black_real, SUM(Case when a.reval > 0 then a.reval else 0 end) as red_real_charge " +
                           " From " + pref + "_charge_" + yf + ". charge_" + mf + " a, " + "public" + ". services c, " + Points.Pref+"_kernel" + ". s_measure d," + Points.Pref+"_kernel" + ". formuls e" +
                           " Where num_ls = " + num_ls + " And a.nzp_serv = c.nzp_serv And a.nzp_serv > 1 And a.nzp_frm = e.nzp_frm And e.nzp_measure = d.nzp_measure" +
                           " Group by c.service_small, a.tarif, d.measure";
#else
   sql = " Select c.service_small, a.tarif, SUM(a.rsum_tarif), Max(a.c_sn), d.measure, SUM(a.sum_charge), SUM(a.sum_nedop), " + 
                  " SUM(Case when real_charge < 0 then real_charge else 0 end) as black_real, SUM(Case when a.reval > 0 then a.reval else 0 end) as red_real_charge " +
                  " From " + pref + "_charge_" + yf + ": charge_" + mf + " a, " + conn_web.Database + ": services c, " + conn_db.Database + ": s_measure d," + conn_db.Database + ": formuls e" +
                  " Where num_ls = " + num_ls + " And a.nzp_serv = c.nzp_serv And a.nzp_serv > 1 And a.nzp_frm = e.nzp_frm And e.nzp_measure = d.nzp_measure" +
                  " Group by c.service_small, a.tarif, d.measure";
#endif

            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка выборки данных NoticeCalcs(1) " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                conn_web.Close();
                return null;
            }

            #endregion

            #region Запись первой выгрузки в DataTable


            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;

            try
            {
                if (reader != null)
                {
                    DataTable First_Data = new DataTable();
                    First_Data.Load(reader, LoadOption.OverwriteChanges);
                    Data.Add(First_Data);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при записи данных в DataTable в DebtCalcs(1)" + ex.Message, MonitorLog.typelog.Error, true);
                conn_db.Close();
                reader.Close();
                return null;
            }
            #endregion

            #region Вторая выгрузка

#if PG
            sql = " Select a.service, b.tarif, b.tarif, SUM(b.rsum_tarif), SUM(b.sum_charge) " +
                            " From " + Points.Pref+"_kernel" + ". services a, " + pref + "_charge_" + yf + ". charge_" + mf + " b, " + Points.Pref+"_kernel" + ". service_union c" +
                            " Where num_ls = " + num_ls + " And b.nzp_serv = a.nzp_serv And b.nzp_serv > 1 And c.nzp_serv_uni = b.nzp_serv And c.nzp_serv_base = 17" +
                            " Group by a.service, b.tarif, b.rsum_tarif";
#else
  sql = " Select a.service, b.tarif, b.tarif, SUM(b.rsum_tarif), SUM(b.sum_charge) " +
                  " From " + conn_web.Database + ": services a, " + pref + "_charge_" + yf + ": charge_" + mf + " b, " + conn_db.Database + ": service_union c" +
                  " Where num_ls = " + num_ls + " And b.nzp_serv = a.nzp_serv And b.nzp_serv > 1 And c.nzp_serv_uni = b.nzp_serv And c.nzp_serv_base = 17" +
                  " Group by a.service, b.tarif, b.rsum_tarif";
#endif

            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка выборки данных NoticeCalcs(2) " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                conn_web.Close();
                return null;
            }

            #endregion

            #region Запись второй выгрузки в DataTable
            try
            {
                if (reader != null)
                {
                    DataTable Second_Data = new DataTable();
                    Second_Data.Load(reader, LoadOption.OverwriteChanges);
                    Data.Add(Second_Data);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при записи данных в DataTable в DebtCalcs(2)" + ex.Message, MonitorLog.typelog.Error, true);
                conn_db.Close();
                reader.Close();
                return null;
            }
            #endregion

            #endregion

            sql.Remove(0, sql.Length);
            reader.Close();
            conn_db.Close();
            conn_web.Close();
            return Data;
        }


        //----------------------------------------------------------------------
        public DataTable DeliveredServicesPayment (Ls finder, int nzp_supp, string date_year,  string date_month, out Returns ret)
        //----------------------------------------------------------------------
        {
            DataTable dt = new DataTable();
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;

            IDataReader reader = null;

            StringBuilder sql = new StringBuilder();

            List<string> prefix = new List<string>();
            try
            {
                #region Открытие соединения с БД

                con_web = GetConnection(Constants.cons_Webdata);
                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_web, true);
                if (ret.result)
                {
                    ret = OpenDb(con_db, true);
                }
                if (!ret.result)
                {
                    MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }

                #endregion

                //sql.Append(" select unique  pref from  " + con_web.Database + ": t" + finder.nzp_user + "_spls; ");

                //if (!ExecRead(con_web, out reader, sql.ToString(), true).result)
                //{
                //    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                //    ret.result = false;
                //    return null;
                //}

                //while (reader.Read())
                //{
                //    if (reader["pref"] != null) prefix.Add(reader["pref"].ToString().Trim());
                //}


                #region Ограничения
                string where_wp = "";
                string where_supp = "";
                string where_serv = "";
                string where_area = "";
                string where_geu = "";
                string where_dom = "";


                if (finder.RolesVal != null)
                {
                    if (finder.RolesVal.Count > 0)
                    {
                        foreach (_RolesVal role in finder.RolesVal)
                        {
                            if (role.tip == Constants.role_sql)
                            {
                                if (role.kod == Constants.role_sql_serv)
                                    where_serv += " and c.nzp_serv in (" + role.val + ") ";
                                if (role.kod == Constants.role_sql_supp)
                                    where_supp += " and c.nzp_supp in (" + role.val + ") ";
                                if (role.kod == Constants.role_sql_wp)
                                    where_wp += " and nzp_wp in (" + role.val + ") ";
                                if (role.kod == Constants.role_sql_area)
                                    where_area += " and b.nzp_area in (" + role.val + ") ";
                                if (role.kod == Constants.role_sql_geu)
                                    where_geu += " and b.nzp_geu in (" + role.val + ") ";


                            }
                        }
                    }
                }
                if (finder.nzp_geu > 0) where_geu += " and b.nzp_geu=" + finder.nzp_geu.ToString();
                if (finder.nzp_area > 0) where_area += " and b.nzp_area=" + finder.nzp_area.ToString();
                if (finder.nzp_dom > 0) where_dom += " and b.nzp_dom=" + finder.nzp_dom.ToString();
                if (finder.nzp_ul > 0) where_dom += " and b.nzp_dom in (select nzp_dom from " +
                    con_db.Database.Replace("_kernel", "data") + "@" +
                    DBManager.getServer(con_db) + ":dom where nzp_ul=" + finder.nzp_ul.ToString()+")";
                #endregion

                #region Получение списка префиксов
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select * ");
                sql.Append(" from  " + "public" + DBManager.getServer(con_db) + ".s_point ");
                sql.Append(" where nzp_wp>1 " + where_wp);
#else
        sql.Append(" select * ");
                sql.Append(" from  " + con_db.Database + "@" + DBManager.getServer(con_db) + ":s_point ");
                sql.Append(" where nzp_wp>1 " + where_wp);
#endif

                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    //con_web.Close();
                    //sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }

                while (reader.Read())
                {
                    if (reader["bd_kernel"] != null) prefix.Add(reader["bd_kernel"].ToString().Trim());
                }

                //проверка на префиксы
                if (prefix.Count == 0)
                {
                    MonitorLog.WriteLog("Отсутствуют префиксы бд", MonitorLog.typelog.Warn, true);
                    return null;
                }

                #endregion

                #region Цикл по префиксам + создание временной таблицы

                //удаляем если есть такая таблица в базе
                ret = ExecSQL(con_db, " Drop table temp_services; ", false);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка удаления временной таблицы temp_services : " + ret.text, MonitorLog.typelog.Error, true);
                    return null;
                }
                else
                {
                    //создание временной таблицы
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" create temp table temp_services (" +
                                " nzp_kvar        INTEGER, " +
                                " nzp_serv        INTEGER, " +
                                " area            char(40), " +
                                " uslugi          char(30), " +
                                " el_plit         INTEGER, " +
                                " credit_one      DECIMAL(14,2), " +
                                " debit_one       DECIMAL(14,2), " +
                                " c_calc          DECIMAL(14,2), " +
                                " sum_tarif       DECIMAL(14,2), " +
                                " c_reval         DECIMAL(14,2), " +
                                " reval           DECIMAL(14,2), " +
                                " sum_money       DECIMAL(14,2), " +
                                " debet_two       DECIMAL(14,2), " +
                                " credit_two      DECIMAL(14,2), " +
                                " calc_rost       DECIMAL(14,2), " +
                                " sum_rost        DECIMAL(14,2), " +
                                " oplata_rost_rub DECIMAL(14,2), " +
                                " oplata_rost_prc DECIMAL(14,2), " +
                                " sum_outsaldo    DECIMAL(14,2) " +
                                 " ) "
                              );
#else
                    sql.Append(" create temp table temp_services (" +
                                " nzp_kvar        INTEGER, " +
                                " nzp_serv        INTEGER, " +
                                " area            char(40), " +
                                " uslugi          char(30), " +
                                " el_plit         INTEGER, " +
                                " credit_one      DECIMAL(14,2), " +
                                " debit_one       DECIMAL(14,2), " +
                                " c_calc          DECIMAL(14,2), " +
                                " sum_tarif       DECIMAL(14,2), " +
                                " c_reval         DECIMAL(14,2), " +
                                " reval           DECIMAL(14,2), " +
                                " sum_money       DECIMAL(14,2), " +
                                " debet_two       DECIMAL(14,2), " +
                                " credit_two      DECIMAL(14,2), " +
                                " calc_rost       DECIMAL(14,2), " +
                                " sum_rost        DECIMAL(14,2), " +
                                " oplata_rost_rub DECIMAL(14,2), " +
                                " oplata_rost_prc DECIMAL(14,2), " +
                                " sum_outsaldo    DECIMAL(14,2) " +
                                 " ) with no log "
                              );
#endif

                    ret = ExecSQL(con_db, sql.ToString(), true);

                    //проверка создания
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка создания временной таблицы temp_services : " + ret.text, MonitorLog.typelog.Error, true);
                        return null;
                    }
                }

                foreach (string pref in prefix)
                {
                    #region заполнение полей nzp_kvar, nzp_serv, uslugi

                    sql.Remove(0, sql.Length);

#if PG
                    sql.Append(" Insert into temp_services (area, nzp_kvar, nzp_serv, uslugi) " +
                                       " Select  a.area, b.nzp_kvar, c.nzp_serv, s.service From " +
                                         "public" + " .services s, " +
                                          "public.s_area a, " +
                                         pref + "_data. kvar b, " +
                                         pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ". charge_" + String.Format("{0:00}", Convert.ToInt32(date_month)) + " c" +
                                       " Where " +
                                       " b.nzp_area = a.nzp_area " +
                                       " and c.nzp_kvar = b.nzp_kvar " + where_serv + where_supp + where_area + where_geu + where_dom +
                                       " and c.nzp_serv = s.nzp_serv " +
                                       " and c.nzp_serv in (9,25,210,6,7) ");
                  
#else
            sql.Append(" Insert into temp_services (area, nzp_kvar, nzp_serv, uslugi) " +
                               " Select  a.area, b.nzp_kvar, c.nzp_serv, s.service From " +
                                 con_db.Database + " :services s, " +
                                 con_db.Database.Replace("kernel", "data") + ":s_area a, " +
                                 pref + "_data: kvar b, " +
                                 pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ": charge_" + String.Format("{0:00}", Convert.ToInt32(date_month)) + " c" +
                               " Where " +
                               " b.nzp_area = a.nzp_area " +
                               " and c.nzp_kvar = b.nzp_kvar " + where_serv + where_supp + where_area + where_geu + where_dom +
                               " and c.nzp_serv = s.nzp_serv " +
                               " and c.nzp_serv in (9,25,210,6,7) ");
#endif
                    //фильтр по поставщикам
                    if (nzp_supp != -1)
                    {
                        sql.Append(" and c.nzp_supp = " + nzp_supp);
                    }

                    ret = ExecSQL(con_db, sql.ToString(), true);

                    //проверка на успех создания
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка во время заполения полей nzp_kvar, uslugi во временной таблице temp_services : " + ret.text, MonitorLog.typelog.Error, true);
                        return null;
                    }

                    #endregion



                    #region создание временной таблицы temp_prm_nzp

                    //удаляем если есть такая таблица в базе
                    ret = ExecSQL(con_db, " Drop table temp_prm_nzp; ", false);


                    sql.Remove(0, sql.Length);

#if PG
                    sql.Append("Create temp table temp_prm_nzp (" +
                                     " nzp INTEGER " +
                                     " )   ");
#else
              sql.Append("Create temp table temp_prm_nzp (" +
                               " nzp INTEGER " +
                               " ) With no log ");
#endif
                    ret = ExecSQL(con_db, sql.ToString(), true);

                    //проверка на успех создания
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка в создании временной таблицы temp_prm_nzp : " + ret.text, MonitorLog.typelog.Error, true);
                        return null;
                    }

                    #endregion


                    #region запись во временную таблицу temp_prm_nzp

                    sql.Remove(0, sql.Length);

#if PG
                    sql.Append("Insert into temp_prm_nzp (nzp) " +
                                       "Select nzp from " + pref + "_data. prm_1" +
                                       " Where nzp_prm = 19 And is_actual = 1 " +
                                       " And dat_s <= '01." + String.Format("{0:00}", Convert.ToInt32(date_month)) + ".20" + String.Format("{0:00}", Convert.ToInt32(date_year)) + "' " +
                                       " And dat_po >= '01." + String.Format("{0:00}", Convert.ToInt32(date_month)) + ".20" + String.Format("{0:00}", Convert.ToInt32(date_year)) + "'"
                                                   );
#else
     sql.Append("Insert into temp_prm_nzp (nzp) " + 
                        "Select nzp from " + pref + "_data: prm_1" +
                        " Where nzp_prm = 19 And is_actual = 1 "     +
                        " And dat_s <= '01." + String.Format("{0:00}", Convert.ToInt32(date_month)) + ".20" + String.Format("{0:00}", Convert.ToInt32(date_year)) + "' " +
                        " And dat_po >= '01." + String.Format("{0:00}", Convert.ToInt32(date_month)) + ".20" + String.Format("{0:00}", Convert.ToInt32(date_year)) + "'"
                                    );
#endif
                    ret = ExecSQL(con_db, sql.ToString(), true);

                    //проверка на успех создания
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка во время заполения поля nzp во временной таблице temp_prm_nzp : " + ret.text, MonitorLog.typelog.Error, true);
                        return null;
                    }

                    #endregion


                    #region Соединение с временной таблицей temp_services

                    sql.Remove(0, sql.Length);

                    sql.Append("Update  temp_services set el_plit = 1 " +
                       " where nzp_kvar in ( " +
                       " select nzp from temp_prm_nzp)"
                       );
                    ret = ExecSQL(con_db, sql.ToString(), true);

                    //проверка на успех создания
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка во время заполения полей el_plit во временной таблице temp_services : " + ret.text, MonitorLog.typelog.Error, true);
                        return null;
                    }

                    #endregion


                    #region первый update

                    sql.Remove(0, sql.Length);

                    if (nzp_supp != -1)
                    {
#if PG
                        sql.Append("Update temp_services set credit_one  = ( " +
                                             " (select   SUM(Case when a.sum_insaldo < 0 then a.sum_insaldo else 0 end) as credit_one " +
                                        
                                             " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ". charge_" + String.Format("{0:00}", Convert.ToInt32(date_month)) + " a" +
                                             " Where temp_services.nzp_kvar = a.nzp_kvar and a.nzp_supp = " + nzp_supp + " and " +
                                             " temp_services.nzp_serv = a.nzp_serv and dat_charge is null)), ");

                        sql.Append("  debit_one  = ( " +
                               " (select  " +
                               "  SUM(Case when a.sum_insaldo > 0 then a.sum_insaldo else 0 end) as debit_one " +
                               " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ". charge_" + String.Format("{0:00}", Convert.ToInt32(date_month)) + " a" +
                               " Where temp_services.nzp_kvar = a.nzp_kvar and a.nzp_supp = " + nzp_supp + " and " +
                               " temp_services.nzp_serv = a.nzp_serv and dat_charge is null)), ");

                        sql.Append("  c_calc  = ( " +
                               " (select   " +
                               "           SUM(c_calc)  " +
                               " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ". charge_" + String.Format("{0:00}", Convert.ToInt32(date_month)) + " a" +
                               " Where temp_services.nzp_kvar = a.nzp_kvar and a.nzp_supp = " + nzp_supp + " and " +
                               " temp_services.nzp_serv = a.nzp_serv and dat_charge is null)), ");

                        sql.Append("  sum_tarif  = ( " +
                               " (select   " +
                               "  SUM(sum_tarif)  " +
                               " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ". charge_" + String.Format("{0:00}", Convert.ToInt32(date_month)) + " a" +
                               " Where temp_services.nzp_kvar = a.nzp_kvar and a.nzp_supp = " + nzp_supp + " and " +
                               " temp_services.nzp_serv = a.nzp_serv and dat_charge is null)), ");

                        sql.Append("  c_reval  = ( " +
                               " (select  " +
                               "   SUM(c_reval)  " +
                               " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ". charge_" + String.Format("{0:00}", Convert.ToInt32(date_month)) + " a" +
                               " Where temp_services.nzp_kvar = a.nzp_kvar and a.nzp_supp = " + nzp_supp + " and " +
                               " temp_services.nzp_serv = a.nzp_serv and dat_charge is null)), ");

                        sql.Append(" reval  = ( " +
                               " (select   " +
                               "  SUM(reval) " +
                               " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ". charge_" + String.Format("{0:00}", Convert.ToInt32(date_month)) + " a" +
                               " Where temp_services.nzp_kvar = a.nzp_kvar and a.nzp_supp = " + nzp_supp + " and " +
                               " temp_services.nzp_serv = a.nzp_serv and dat_charge is null)), ");

                        sql.Append(" sum_money = ( " +
                               " (select SUM(sum_money) " +
                               " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ". charge_" + String.Format("{0:00}", Convert.ToInt32(date_month)) + " a" +
                               " Where temp_services.nzp_kvar = a.nzp_kvar and a.nzp_supp = " + nzp_supp + " and " +
                               " temp_services.nzp_serv = a.nzp_serv and dat_charge is null))");

#else
               sql.Append("Update temp_services set (credit_one, debit_one, c_calc, sum_tarif, c_reval, reval, sum_money) = ( " +
                                    " (select   SUM(Case when a.sum_insaldo < 0 then a.sum_insaldo else 0 end) as credit_one, " +
                                    "           SUM(Case when a.sum_insaldo > 0 then a.sum_insaldo else 0 end) as debit_one, " +
                                    "           SUM(c_calc),SUM(sum_tarif),SUM(c_reval),SUM(reval),SUM(sum_money) " +
                                    " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ": charge_" + String.Format("{0:00}", Convert.ToInt32(date_month)) + " a" +
                                    " Where temp_services.nzp_kvar = a.nzp_kvar and a.nzp_supp = " + nzp_supp + " and " +
                                    " temp_services.nzp_serv = a.nzp_serv and dat_charge is null))"
                                        );
#endif
                    }
                    else
                    {
#if PG
                        sql.Append("Update temp_services set credit_one  = ( " +
                                       " (select   SUM(Case when a.sum_insaldo < 0 then a.sum_insaldo else 0 end) as credit_one " +
                                       " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ". charge_" + String.Format("{0:00}", Convert.ToInt32(date_month)) + " a" +
                                       " Where temp_services.nzp_kvar = a.nzp_kvar and temp_services.nzp_serv = a.nzp_serv and dat_charge is null)), ");

                        sql.Append("   debit_one = ( " +
                                  " (select   SUM(Case when a.sum_insaldo > 0 then a.sum_insaldo else 0 end) as debit_one " +
                                " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ". charge_" + String.Format("{0:00}", Convert.ToInt32(date_month)) + " a" +
                                  " Where temp_services.nzp_kvar = a.nzp_kvar and temp_services.nzp_serv = a.nzp_serv and dat_charge is null)), ");

                        sql.Append("  c_calc  = ( " +
                                  " (select   SUM(c_calc) " +
                                  " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ". charge_" + String.Format("{0:00}", Convert.ToInt32(date_month)) + " a" +
                                  " Where temp_services.nzp_kvar = a.nzp_kvar and temp_services.nzp_serv = a.nzp_serv and dat_charge is null)), ");

                        sql.Append("  sum_tarif,  = ( " +
                                  " (select  SUM(sum_tarif) " +
                                  " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ". charge_" + String.Format("{0:00}", Convert.ToInt32(date_month)) + " a" +
                                  " Where temp_services.nzp_kvar = a.nzp_kvar and temp_services.nzp_serv = a.nzp_serv and dat_charge is null)), ");

                        sql.Append("  c_reval  = ( " +
                                  " (select   SUM(c_reval) " +
                                  " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ". charge_" + String.Format("{0:00}", Convert.ToInt32(date_month)) + " a" +
                                  " Where temp_services.nzp_kvar = a.nzp_kvar and temp_services.nzp_serv = a.nzp_serv and dat_charge is null)), ");

                        sql.Append("  reval  = ( " +
                                  " (select  SUM(reval) " +
                                  " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ". charge_" + String.Format("{0:00}", Convert.ToInt32(date_month)) + " a" +
                                  " Where temp_services.nzp_kvar = a.nzp_kvar and temp_services.nzp_serv = a.nzp_serv and dat_charge is null)), ");

                        sql.Append(" sum_money = ( " +
                                  " (select   (sum_money)" +
                                  " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ". charge_" + String.Format("{0:00}", Convert.ToInt32(date_month)) + " a" +
                                  " Where temp_services.nzp_kvar = a.nzp_kvar and temp_services.nzp_serv = a.nzp_serv and dat_charge is null))");
#else
                     sql.Append("Update temp_services set (credit_one, debit_one, c_calc, sum_tarif, c_reval, reval, sum_money) = ( " +
                                    " (select   SUM(Case when a.sum_insaldo < 0 then a.sum_insaldo else 0 end) as credit_one, " +
                                    "           SUM(Case when a.sum_insaldo > 0 then a.sum_insaldo else 0 end) as debit_one, " +
                                    "           SUM(c_calc),SUM(sum_tarif),SUM(c_reval),SUM(reval),SUM(sum_money)" +
                                    " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ": charge_" + String.Format("{0:00}", Convert.ToInt32(date_month)) + " a" +
                                    " Where temp_services.nzp_kvar = a.nzp_kvar and " +
                                    " temp_services.nzp_serv = a.nzp_serv and dat_charge is null))"
                                        );
#endif
                    }
                    ret = ExecSQL(con_db, sql.ToString(), true);

                    //проверка на успех создания
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка во время первого update в таблице temp_services : " + ret.text, MonitorLog.typelog.Error, true);
                        return null;
                    }

                    #endregion

                    #region второй update

                    sql.Remove(0, sql.Length);

                    if (nzp_supp != -1)
                    {
#if PG

                        sql.Append("Update  temp_services set debet_two =  " +
                                   " (select   SUM(Case when a.sum_insaldo < 0 then a.sum_insaldo else 0 end) as credit_two " +
                                   " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) +
                                   ". charge_01 a" +
                                   " Where   temp_services.nzp_kvar = a.nzp_kvar and temp_services.nzp_serv = a.nzp_serv and a.nzp_supp = " +
                                   nzp_supp + " ), " +


                                  " credit_two  = ( " +

                                              " select   SUM(Case when a.sum_insaldo > 0 then a.sum_insaldo else 0 end) as debit_two " +
                                              " From " + pref + "_charge_" +
                                              String.Format("{0:00}", Convert.ToInt32(date_year)) + ". charge_01 a" +
                                              " Where   temp_services.nzp_kvar = a.nzp_kvar and temp_services.nzp_serv = a.nzp_serv and a.nzp_supp = " +
                                              nzp_supp + " )::numeric, " +



                                                  "   sum_outsaldo = ( (select   SUM(Case when a.sum_outsaldo > 0 then a.sum_outsaldo else 0 end) as sum_outsaldo " +
                                                  " From " + pref + "_charge_" +
                                                  String.Format("{0:00}", Convert.ToInt32(date_year)) + ". charge_" +
                                                  String.Format("{0:00}", Convert.ToInt32(date_month)) + " a" +
                                                  " Where   temp_services.nzp_kvar = a.nzp_kvar and temp_services.nzp_serv = a.nzp_serv and a.nzp_supp = " +
                                                  nzp_supp + " ))");
#else
        sql.Append("Update  temp_services set (debet_two, credit_two, sum_outsaldo) = ( " +
                                   " (select   SUM(Case when a.sum_insaldo < 0 then a.sum_insaldo else 0 end) as credit_two " +
                                   " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ": charge_01 a" +
                                   " Where   temp_services.nzp_kvar = a.nzp_kvar and temp_services.nzp_serv = a.nzp_serv and a.nzp_supp = " + nzp_supp + " ), " +
                                   " (select   SUM(Case when a.sum_insaldo > 0 then a.sum_insaldo else 0 end) as debit_two " +
                                   " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ": charge_01 a" +
                                   " Where   temp_services.nzp_kvar = a.nzp_kvar and temp_services.nzp_serv = a.nzp_serv and a.nzp_supp = " + nzp_supp + " ), " +
                                   " (select   SUM(Case when a.sum_outsaldo > 0 then a.sum_outsaldo else 0 end) as sum_outsaldo " +
                                   " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ": charge_" + String.Format("{0:00}", Convert.ToInt32(date_month)) + " a" +
                                   " Where   temp_services.nzp_kvar = a.nzp_kvar and temp_services.nzp_serv = a.nzp_serv and a.nzp_supp = " + nzp_supp + " ))");
#endif
                    }
                    else
                    {
#if PG
                        sql.Append("Update  temp_services set debet_two  = ( " +
                                               " (select   SUM(Case when a.sum_insaldo < 0 then a.sum_insaldo else 0 end) as credit_two " +
                                               " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ". charge_01 a" +
                                               " Where   temp_services.nzp_kvar = a.nzp_kvar and temp_services.nzp_serv = a.nzp_serv)), " +

                                              "  credit_two  = ( " +

                                               " (select   SUM(Case when a.sum_insaldo > 0 then a.sum_insaldo else 0 end) as debit_two " +
                                               " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ". charge_01 a" +
                                               " Where   temp_services.nzp_kvar = a.nzp_kvar and temp_services.nzp_serv = a.nzp_serv))::numeric, " +

                                               "Update  sum_outsaldo = ( " +

                                               " (select   SUM(Case when a.sum_outsaldo > 0 then a.sum_outsaldo else 0 end) as sum_outsaldo " +
                                               " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ". charge_" + String.Format("{0:00}", Convert.ToInt32(date_month)) + " a" +
                                               " Where   temp_services.nzp_kvar = a.nzp_kvar and temp_services.nzp_serv = a.nzp_serv))");
#else
        sql.Append("Update  temp_services set (debet_two, credit_two, sum_outsaldo) = ( " +
                               " (select   SUM(Case when a.sum_insaldo < 0 then a.sum_insaldo else 0 end) as credit_two " +
                               " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ": charge_01 a" +
                               " Where   temp_services.nzp_kvar = a.nzp_kvar and temp_services.nzp_serv = a.nzp_serv), " +
                               " (select   SUM(Case when a.sum_insaldo > 0 then a.sum_insaldo else 0 end) as debit_two " +
                               " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ": charge_01 a" +
                               " Where   temp_services.nzp_kvar = a.nzp_kvar and temp_services.nzp_serv = a.nzp_serv), " +
                               " (select   SUM(Case when a.sum_outsaldo > 0 then a.sum_outsaldo else 0 end) as sum_outsaldo " +
                               " From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ": charge_" + String.Format("{0:00}", Convert.ToInt32(date_month)) + " a" +
                               " Where   temp_services.nzp_kvar = a.nzp_kvar and temp_services.nzp_serv = a.nzp_serv))");
#endif
                    }
                    ret = ExecSQL(con_db, sql.ToString(), true);

                    //проверка на успех создания
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка во время второго update в таблице temp_services : " + ret.text, MonitorLog.typelog.Error, true);
                        return null;
                    }

                    for (int i = 1; i <= Convert.ToInt32(date_month); i++)
                    {
                        int month = Convert.ToInt32(date_month);
                        sql.Remove(0, sql.Length);

                        if (nzp_supp != -1)
                        {
#if PG
                            sql.Append(" Update  temp_services set calc_rost = (c_calc + c_reval), ");
                            sql.Append(" sum_rost =  (sum_tarif + reval), ");
                            sql.Append("  oplata_rost_rub =  (select SUM(a.sum_money) From " + pref + "_charge_" +
                                       String.Format("{0:00}", Convert.ToInt32(date_year)) + ". charge_" +
                                       String.Format("{0:00}", i) + " a " +
                                       "  Where temp_services.nzp_kvar = a.nzp_kvar and temp_services.nzp_serv = a.nzp_serv and a.nzp_supp = " +
                                       nzp_supp + " ), ");
                                                                   
                            sql.Append(" oplata_rost_prc =(CASE WHEN oplata_rost_rub>0 then (sum_rost/oplata_rost_rub) else 0 end) ");
#else
 sql.Append(" Update  temp_services set (calc_rost, sum_rost, oplata_rost_rub, oplata_rost_prc) = ( " +
                                            " (c_calc + c_reval), " +
                                            " (sum_tarif + reval), " +
                                            " (select SUM(a.sum_money) From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ": charge_" + String.Format("{0:00}", i) + " a " +
                                            "  Where temp_services.nzp_kvar = a.nzp_kvar and temp_services.nzp_serv = a.nzp_serv and a.nzp_supp = " + nzp_supp + " ), " +
                                            " (CASE WHEN oplata_rost_rub>0 then (sum_rost/oplata_rost_rub) else 0 end))");
#endif
                        }
                        else
                        {
#if PG
                            sql.Append(" Update  temp_services set calc_rost=  (c_calc + c_reval), ");

                            sql.Append("  sum_rost =   (sum_tarif + reval), ");

                            sql.Append("  oplata_rost_rub =  (select SUM(a.sum_money) From " + pref + "_charge_" +
                                       String.Format("{0:00}", Convert.ToInt32(date_year)) + ". charge_" +
                                       String.Format("{0:00}", i) + " a " +
                                       "  Where temp_services.nzp_kvar = a.nzp_kvar and temp_services.nzp_serv = a.nzp_serv), ");
                                                   
                            sql.Append(" oplata_rost_prc) =  (CASE WHEN oplata_rost_rub>0 then (sum_rost/oplata_rost_rub) else 0 end)"
                                                      );
#else
            sql.Append(" Update  temp_services set (calc_rost, sum_rost, oplata_rost_rub, oplata_rost_prc) = ( " +
                                             " (c_calc + c_reval), " +
                                             " (sum_tarif + reval), " +
                                             " (select SUM(a.sum_money) From " + pref + "_charge_" + String.Format("{0:00}", Convert.ToInt32(date_year)) + ": charge_" + String.Format("{0:00}", i) + " a " +
                                             "  Where temp_services.nzp_kvar = a.nzp_kvar and temp_services.nzp_serv = a.nzp_serv), " +
                                             " (CASE WHEN oplata_rost_rub>0 then (sum_rost/oplata_rost_rub) else 0 end))"
                                             ); 
#endif
                        }
                        ret = ExecSQL(con_db, sql.ToString(), true);

                        //проверка на успех создания
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("Ошибка во время третьего update в таблице temp_services : " + ret.text, MonitorLog.typelog.Error, true);
                            return null;
                        }
                    }

                    #endregion 

                    #region Выборка данных для выгрузки

                    sql.Remove(0, sql.Length);

                    sql.Append( " Select  MAX(trim(area)) as area, " +
                                " 1 as ord_, " +
                                " 'коммунальные услуги, в том числе:' as service, " +
                                " SUM(credit_one)/1000," + 
                                " SUM(debit_one)/1000, " +
                                " SUM(c_calc), " +
                                " SUM(sum_tarif)/1000, " + 
                                " SUM(c_reval),SUM(reval)/1000, " + 
                                " SUM(sum_money)/1000, " +  
                                " SUM(debet_two)/1000, " +  
                                " SUM(credit_two)/1000, " +  
                                " SUM(calc_rost), " + 
                                " SUM(sum_rost)/1000, " +  
                                " SUM(oplata_rost_rub)/1000, " +  
                                " SUM(oplata_rost_prc) " +  
                                " From temp_services " +  
                                " Group by 2, 3 " +  
                                " Union All " +  
                                " Select MAX(area), " + 
                                " 2 as ord_, " +
                                " ' - горячая вода' as service, " +
                                " SUM(credit_one)/1000, " + 
                                " SUM(debit_one)/1000, " +
                                " SUM(c_calc), " + 
                                " SUM(sum_tarif)/1000, " +
                                " SUM(c_reval),SUM(reval)/1000, " +
                                " SUM(sum_money)/1000, " + 
                                " SUM(debet_two)/1000, " + 
                                " SUM(credit_two)/1000, " + 
                                " SUM(calc_rost), " +
                                " SUM(sum_rost)/1000, " + 
                                " SUM(oplata_rost_rub)/1000, " + 
                                " SUM(oplata_rost_prc) " +
                                " From temp_services Where nzp_serv = 9 " +
                                " Group by 2,3 " +  
                                " Union All " +    
                                " Select MAX(area), " + 
                                " 3 as ord_, " +
                                " ' - Электроэнергия (всего)' as service, " +
                                " SUM(credit_one)/1000, " + 
                                " SUM(debit_one)/1000, " +
                                " SUM(c_calc), " + 
                                " SUM(sum_tarif)/1000, " +
                                " SUM(c_reval),SUM(reval)/1000, " +
                                " SUM(sum_money)/1000, " + 
                                " SUM(debet_two)/1000, " + 
                                " SUM(credit_two)/1000, " +
                                " SUM(calc_rost), " +
                                " SUM(sum_rost)/1000, " + 
                                " SUM(oplata_rost_rub)/1000, " + 
                                " SUM(oplata_rost_prc) " +
                                " From temp_services Where (nzp_serv = 25 or nzp_serv = 210) " +
                                " Group by 2,3 " + 
                                " Union All " +
                                " Select MAX(area), " +
                                " 4 as ord_, " +
                                " ' - для квартир с газ. плитами (всего)' as service, " +
                                " SUM(credit_one)/1000, " + 
                                " SUM(debit_one)/1000, " +
                                " SUM(c_calc), " + 
                                " SUM(sum_tarif)/1000, " + 
                                " SUM(c_reval),SUM(reval)/1000, " +
                                " SUM(sum_money)/1000, " + 
                                " SUM(debet_two)/1000, " + 
                                " SUM(credit_two)/1000, " +
                                " SUM(calc_rost), " +
                                " SUM(sum_rost)/1000, " + 
                                " SUM(oplata_rost_rub)/1000, " + 
                                " SUM(oplata_rost_prc) " +
                                " From temp_services Where (nzp_serv = 25 or nzp_serv = 210) and el_plit < 1 " +
                                " Group by 2,3 " + 
                                " Union All " +
                                " Select MAX(area), " + 
                                " 5 as ord_, " +
                                " ' - в том числе день' as service, " +
                                " SUM(credit_one)/1000, " + 
                                " SUM(debit_one)/1000, " +
                                " SUM(c_calc), " + 
                                " SUM(sum_tarif)/1000, " + 
                                " SUM(c_reval),SUM(reval)/1000, " +
                                " SUM(sum_money)/1000, " + 
                                " SUM(debet_two)/1000, " + 
                                " SUM(credit_two)/1000, " + 
                                " SUM(calc_rost), " +
                                " SUM(sum_rost)/1000, " + 
                                " SUM(oplata_rost_rub)/1000, " + 
                                " SUM(oplata_rost_prc) " +
                                " From temp_services Where nzp_serv = 25 and el_plit < 1 " +
                                " Group by 2,3 " + 
                                " Union All " +  

                                " Select MAX(area), " + 
                                " 6 as ord_, " +
                                " ' - в том числе ночь' as service, " +
                                " SUM(credit_one)/1000, " + 
                                " SUM(debit_one)/1000, " +
                                " SUM(c_calc), " + 
                                " SUM(sum_tarif)/1000, " + 
                                " SUM(c_reval),SUM(reval)/1000, " +
                                " SUM(sum_money)/1000, " + 
                                " SUM(debet_two)/1000, " + 
                                " SUM(credit_two)/1000, " + 
                                " SUM(calc_rost), " +
                                " SUM(sum_rost)/1000, " + 
                                " SUM(oplata_rost_rub)/1000, " + 
                                " SUM(oplata_rost_prc) " +
                                " From temp_services Where nzp_serv = 210 and el_plit < 1 " +
                                " Group by 2,3 " + 
                                " Union All " +  
                                " Select MAX(area), " + 
                                " 7 as ord_, " +
                                " '- для квартир с эл. плитами (всего)' as service, " +
                                " SUM(credit_one)/1000, " + 
                                " SUM(debit_one)/1000, " +
                                " SUM(c_calc), " + 
                                " SUM(sum_tarif)/1000, " + 
                                " SUM(c_reval),SUM(reval)/1000, " +
                                " SUM(sum_money)/1000, " + 
                                " SUM(debet_two)/1000, " + 
                                " SUM(credit_two)/1000, " + 
                                " SUM(calc_rost), " +
                                " SUM(sum_rost)/1000, " + 
                                " SUM(oplata_rost_rub)/1000, " + 
                                " SUM(oplata_rost_prc) " +
                                " From temp_services Where (nzp_serv = 25 or nzp_serv = 210) and el_plit = 1 " +
                                " Group by 2,3  " +
                                " Union All " +  
                                " Select MAX(area), " + 
                                " 8 as ord_, " +
                                " ' - в том числе день' as service, " +
                                " SUM(credit_one)/1000, " + 
                                " SUM(debit_one)/1000, " +
                                " SUM(c_calc), " + 
                                " SUM(sum_tarif)/1000, " +
                                " SUM(c_reval),SUM(reval)/1000, " +
                                " SUM(sum_money)/1000, " + 
                                " SUM(debet_two)/1000,  " +
                                " SUM(credit_two)/1000, " +
                                " SUM(calc_rost), " +
                                " SUM(sum_rost)/1000,  " + 
                                " SUM(oplata_rost_rub)/1000, " + 
                                " SUM(oplata_rost_prc) " +
                                " From temp_services Where nzp_serv = 25 and el_plit = 1 " +
                                " Group by 2,3 " + 
                                " Union All  " + 
                                " Select MAX(area), " + 
                                " 8 as ord_, " +
                                " ' - в том числе ночь' as service, " +
                                " SUM(credit_one)/1000, " + 
                                " SUM(debit_one)/1000, " +
                                " SUM(c_calc),  " +
                                " SUM(sum_tarif)/1000,  " +
                                " SUM(c_reval),SUM(reval)/1000, " +
                                " SUM(sum_money)/1000, " + 
                                " SUM(debet_two)/1000, " + 
                                " SUM(credit_two)/1000, " +
                                " SUM(calc_rost), " +
                                " SUM(sum_rost)/1000, " + 
                                " SUM(oplata_rost_rub)/1000, " + 
                                " SUM(oplata_rost_prc) " +
                                " From temp_services Where nzp_serv = 210 and el_plit < 1 " +
                                " Group by 2,3 " + 
                                " Union All " +  
                                " Select MAX(area), " + 
                                " 10 as ord_, " +
                                " ' - отопление' as service, " +
                                " SUM(credit_one)/1000, " + 
                                " SUM(debit_one)/1000, " +
                                " SUM(c_calc),  " +
                                " SUM(sum_tarif)/1000,  " +
                                " SUM(c_reval),SUM(reval)/1000, " +
                                " SUM(sum_money)/1000, " + 
                                " SUM(debet_two)/1000, " + 
                                " SUM(credit_two)/1000, " + 
                                " SUM(calc_rost), " +
                                " SUM(sum_rost)/1000,  " +
                                " SUM(oplata_rost_rub)/1000,  " +
                                " SUM(oplata_rost_prc) " +
                                " From temp_services Where nzp_serv = 8 " +
                                " Group by 2,3  " + 
                                " Union All   " +
                                " Select MAX(area), " +
                                " 11 as ord_, " +
                                " ' - водоснабжение, водоотведение' as service, " +
                                " SUM(credit_one)/1000,  " +
                                " SUM(debit_one)/1000, " +
                                " SUM(c_calc),  " +
                                " SUM(sum_tarif)/1000,  " +
                                " SUM(c_reval),SUM(reval)/1000, " +
                                " SUM(sum_money)/1000,  " +
                                " SUM(debet_two)/1000,  " +
                                " SUM(credit_two)/1000,  " +
                                " SUM(calc_rost), " +
                                " SUM(sum_rost)/1000,  " +
                                " SUM(oplata_rost_rub)/1000,  " +
                                " SUM(oplata_rost_prc) " +
                                " From temp_services Where (nzp_serv = 6 or nzp_serv = 7) " +
                                " Group by 2,3   " +

                                " Order by 1,2");

                    ret = ExecRead(con_db, out reader, sql.ToString(), true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки данных temp_services" + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                        con_db.Close();
                        con_web.Close();
                        return null;
                    }

                    #region Запись выгрузки в DataTable


                    System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                    culture.NumberFormat.NumberDecimalSeparator = ".";
                    culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                    System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                    System.Threading.Thread.CurrentThread.CurrentCulture = culture;

                    try
                    {
                        if (reader != null)
                        {
                            dt.Load(reader,LoadOption.OverwriteChanges);
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка при записи данных в DataTable в DebtCalcs(1)" + ex.Message, MonitorLog.typelog.Error, true);
                        con_db.Close();
                        reader.Close();
                        return null;
                    }
                    #endregion

                    #endregion
                }


                return dt;

                #endregion

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  DeliveredServicesPayment : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений
                //удаляем временную таблицу
                ret = ExecSQL(con_db, " Drop table temp_services; ", true);

                if (con_db != null)
                {
                    con_db.Close();
                }

                if (con_web != null)
                {
                    con_web.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }
    }
}
