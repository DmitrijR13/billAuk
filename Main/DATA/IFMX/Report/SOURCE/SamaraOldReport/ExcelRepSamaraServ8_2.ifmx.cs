using System;
using System.Data;
using Microsoft.Office.Interop.Excel;
using STCLINE.KP50.Global;

using STCLINE.KP50.Interfaces;
using System.Text;
using Constants = STCLINE.KP50.Global.Constants;
using DataTable = System.Data.DataTable;
using Points = STCLINE.KP50.Interfaces.Points;

namespace STCLINE.KP50.DataBase
{
    public partial class ExcelRep
    {


        public DataTable GetSpravSoderg2Heat(Prm prm, out Returns ret, string Nzp_user)
        {
            ret = Utils.InitReturns();

            IDataReader reader;
            IDataReader reader2;

            #region Подключение к БД

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata); //new IDbConnection(Constants.cons_Webdata);

            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel); //new IDbConnection(Constants.cons_Webdata);

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с локальной БД ",
                    MonitorLog.typelog.Error, true);
                conn_web.Close();
                ret.result = false;
                return null;
            }

            #endregion

            StringBuilder sql = new StringBuilder();
            StringBuilder sql2 = new StringBuilder();

            #region Выборка по локальным банкам

            DataTable LocalTable = new DataTable();

            string tXX_spls = DBManager.GetFullBaseName(conn_web) + DBManager.tableDelimiter + "t" + Nzp_user + "_spls";

            conn_web.Close();

            ExecSQL(conn_db, "drop table sel_kvar_heat", false);
            sql.Remove(0, sql.Length);
#if PG
            sql.Append(" select nzp_dom,nzp_kvar,pref, typek into temp sel_kvar_heat from " + tXX_spls + " where 1=1 ");
#else
            sql.Append(" select nzp_dom,nzp_kvar,pref, typek from " + tXX_spls +
                       " where sostls='открыт' into temp sel_kvar_heat with no log ");
#endif
            if (!ExecSQL(conn_db, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }

            ExecSQL(conn_db, "drop table t_svod_heat", false);

            sql.Remove(0, sql.Length);
#if PG
            sql.Append(" create temp  table t_svod_heat( " +
                    " nzp_d Serial, " +
                   " nzp_dom integer, " +
                   " nzp_dom2 integer, " +
                     " pl_all numeric(14,2), " + //Общая площадь
                     " pl_izol numeric(14,2), " + //Общая плозадь изолированных квартир
                     " pl_komm_all numeric(14,2), " + //Общая площадь коммунальных квартир
                     " pl_komm_gil numeric(14,2), " + //Жилая площадь коммунальных квартир
                     " pl_ngil numeric(14,2), " + //Площадь нежилых помещений
                     " pl_mop numeric(14,2), " + //Площадь мест общего пользования
                     " rsum_tarif  numeric(14,2), " +//начислено по тарифу по основной услуге
                     " c_calc_gkal  numeric(18,4), " +//объем в Гкал по отоплению
                     " vozv_rso numeric(14,2), " + //Возвраты по вине РСО в Гкал
                     " vozv_uk numeric(14,2), " + //Возвраты по вине УК в Гкал.
                     " vozv_rso_gkal numeric(18,4), " + //Возвраты по вине РСО в Гкал
                     " vozv_uk_gkal numeric(18,4), " + //Возвраты по вине УК в Гкал.
                     " sum_nedop  numeric(14,2) default 0, " +//Сумма недопоставки
                     " reval_gkal numeric(18,4), " +//объем перерасчета в Гкал.
                     " reval  numeric(14,2), " + //Сумма перерасчета
                     " sum_charge_gkal numeric(18,4), " +//Объем начислено к оплате в Гкал
                     " sum_charge  numeric(14,2), " + //Сумма начислено к оплате
                     " tarif_gkal numeric(18,4) default 0, " +//Тариф на дом на Гкал
                     " odpu_gkal numeric(18,4) , " +//Объем предъявленный жильцам в Гкал
                     " norma numeric(14,2))  ");
#else
            sql.Append(" create temp table t_svod_heat( " +
                       " nzp_d Serial(1), " +
                       " nzp_dom integer, " +
                       " nzp_dom2 integer, " +
                       " pl_all Decimal(14,2), " + //Общая площадь
                       " pl_izol Decimal(14,2), " + //Общая плозадь изолированных квартир
                       " pl_komm_all Decimal(14,2), " + //Общая площадь коммунальных квартир
                       " pl_komm_gil Decimal(14,2), " + //Жилая площадь коммунальных квартир
                       " pl_ngil Decimal(14,2), " + //Площадь нежилых помещений
                       " pl_mop Decimal(14,2), " + //Площадь мест общего пользования
                       " rsum_tarif  Decimal(14,2), " + //начислено по тарифу по основной услуге
                       " c_calc_gkal  Decimal(18,4), " + //объем в Гкал по отоплению
                       " vozv_rso Decimal(14,2), " + //Возвраты по вине РСО в Гкал
                       " vozv_uk Decimal(14,2), " + //Возвраты по вине УК в Гкал.
                       " vozv_rso_gkal Decimal(18,4), " + //Возвраты по вине РСО в Гкал
                       " vozv_uk_gkal Decimal(18,4), " + //Возвраты по вине УК в Гкал.
                       " sum_nedop  Decimal(14,2) default 0, " + //Сумма недопоставки
                       " reval_gkal Decimal(18,4), " + //объем перерасчета в Гкал.
                       " reval  Decimal(14,2), " + //Сумма перерасчета
                       " sum_charge_gkal Decimal(18,4), " + //Объем начислено к оплате в Гкал
                       " sum_charge  Decimal(14,2), " + //Сумма начислено к оплате
                       " tarif_gkal Decimal(18,4) default 0, " + //Тариф на дом на Гкал
                       " odpu_gkal Decimal(18,4) , " + //Объем предъявленный жильцам в Гкал
                       " norma decimal(14,2)) with no log ");
#endif
            if (!ExecSQL(conn_db, sql.ToString(), true).result)
                return null;

            ExecSQL(conn_db, "create index ix_tmpww_01 on t_svod_heat(nzp_dom)", true);


            sql.Remove(0, sql.Length);
            sql.Append(" select pref from sel_kvar_heat group by 1 ");
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }

            sql.Remove(0, sql.Length);
            sql.Append(" select pref from sel_kvar_heat group by 1 ");
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                ret.result = false;
                return null;
            }
            while (reader.Read())
            {
                string pref = reader["pref"].ToString().Trim();
                string sChargeAlias = pref + "_charge_" + prm.year_.ToString().Substring(2, 2);
                string sChargeTable = "charge_" + prm.month_.ToString("00");


                ExecSQL(conn_db, "drop table t_local_heat", false);
#if PG
                ExecSQL(conn_db, "select * into temp t_local_heat from t_svod_heat where nzp_dom=-1 ", true);
#else
                ExecSQL(conn_db, "select * from t_svod_heat where nzp_dom=-1 into temp t_local_heat with no log", true);
#endif

                ExecSQL(conn_db, "create index ix_tmpww_02 on t_local_heat(nzp_dom)", true);

                sql.Remove(0, sql.Length);
                sql.Append(" insert into t_local_heat(nzp_dom, nzp_dom2) ");
                sql.Append(" select nzp_dom, nzp_dom from sel_kvar_heat ");
                sql.Append(" where pref = '" + pref + "'");
                if (prm.has_pu == "2")
                    sql.Append(" and 0 < (select count(*)  " +
#if PG
 " from " + sChargeAlias + ".counters_" + prm.month_.ToString("00") +
#else
                        " from " + sChargeAlias + ":counters_" + prm.month_.ToString("00") +
#endif
 " where stek=3 and nzp_type=1 and cnt_stage>0 and nzp_serv=8) ");
                if (prm.has_pu == "3")
                    sql.Append(" and 0 = (select count(*)  " +
#if PG
 " from " + sChargeAlias + ".counters_" + prm.month_.ToString("00") +
#else
                        " from " + sChargeAlias + ":counters_" + prm.month_.ToString("00") +
#endif
 " where stek=3 and nzp_type=1 and cnt_stage>0 and nzp_serv=8) ");
                if (prm.nzp_key > -1)
                {
#if PG
                    sql.Append(" and 0<(select count(*) from " + sChargeAlias + "." + sChargeTable + " a ");
#else
                    sql.Append(" and 0<(select count(*) from " + sChargeAlias + ":" + sChargeTable + " a ");
#endif
                    sql.Append(" where sel_kvar_heat.nzp_kvar=a.nzp_kvar and ");
                    sql.Append(" dat_charge is null ");
                    sql.Append(" and a.nzp_serv=8 and a.nzp_supp=" + prm.nzp_key + ")");
                }

                sql.Append(" group by 1 ");
                ExecSQL(conn_db, sql.ToString(), true);

                /* sql.Remove(0, sql.Length);
                 sql.Append(" update t_local_heat set litera = (select max(val_prm) from " + pref + "_data:prm_1 a, ");
                 sql.Append("sel_kvar_heat k  where nzp_prm=2002 and k.nzp_dom=t_local_heat.nzp_dom  ");
                 sql.Append(" and is_actual=1 and k.nzp_kvar=a.nzp ");
                 sql.Append(" and dat_s<=today and dat_po>=today)");
                 if (!ExecSQL(conn_db, sql.ToString(), true).result)
                     return null;*/




                ExecSQL(conn_db, "drop table t_kvars", false);

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" create temp table t_kvars( " +
                           " nzp_dom integer, " +
                           " nzp_kvar integer, " +
                           " nzp_supp_uk integer, " +
                           " typek integer, " + //
                           " izol integer default 1, " + //Изолированная квартира
                           " pl_kvar numeric(14,2), " + //Общая плозадь 
                           " pl_gil numeric(14,2), " + //жилая плозадь 
                           " otop_koef numeric(14,4), " + //Отопительный коэффициент
                           " pl_all numeric(14,2))   ");
#else
                sql.Append(" create temp table t_kvars( " +
                           " nzp_dom integer, " +
                           " nzp_kvar integer, " +
                           " nzp_supp_uk integer, " +
                           " typek integer, " + //
                           " izol integer default 1, " + //Изолированная квартира
                           " pl_kvar Decimal(14,2), " + //Общая плозадь 
                           " pl_gil Decimal(14,2), " + //жилая плозадь 
                           " otop_koef Decimal(14,4), " + //Отопительный коэффициент
                           " pl_all Decimal(14,2)) with no log ");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;

                sql.Remove(0, sql.Length);
                sql.Append(" insert into t_kvars(nzp_kvar, nzp_dom, typek) ");
                sql.Append(" select nzp_kvar , b.nzp_dom, typek   ");
                sql.Append(" from t_local_heat a, sel_kvar_heat  b ");
                sql.Append(" where a.nzp_dom=b.nzp_dom ");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;


                ExecSQL(conn_db, " create index ix_t_kvars on t_kvars (nzp_dom , nzp_kvar)", true);



                sql.Remove(0, sql.Length);
#if PG
                sql.Append("  update t_kvars set pl_kvar  = (select sum(squ) ");
                sql.Append("  from " + sChargeAlias + ".calc_gku_" + prm.month_.ToString("00") + " a ");
                sql.Append("  where a.nzp_serv=8 and a.nzp_kvar=t_kvars.nzp_kvar and a.tarif>0 ) ");
#else
                sql.Append("  update t_kvars set pl_kvar  = (select sum(squ) ");
                sql.Append("  from " + sChargeAlias + ":calc_gku_" + prm.month_.ToString("00") + " a ");
                sql.Append("  where a.nzp_serv=8 and a.nzp_kvar=t_kvars.nzp_kvar and a.tarif>0 ) ");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;





                sql.Remove(0, sql.Length);
#if PG
                sql.Append("  update t_kvars set pl_all  = (select sum(val_prm::numeric) ");
                sql.Append("  from " + pref + "_data.prm_1 a ");
                sql.Append("  where a.nzp_prm = 4 and a.nzp=t_kvars.nzp_kvar ");
                sql.Append("  and a.is_actual=1  ");
                sql.Append("  and dat_po>= '01." + prm.month_.ToString() + "." + prm.year_.ToString() + "'");
                sql.Append("  and dat_s <= '" + DateTime.DaysInMonth(prm.year_, prm.month_).ToString() + ".");
#else
                sql.Append("  update t_kvars set pl_all  = (select sum(val_prm+0) ");
                sql.Append("  from " + pref + "_data:prm_1 a ");
                sql.Append("  where a.nzp_prm =4 and a.nzp=t_kvars.nzp_kvar ");
                sql.Append("  and a.is_actual=1  ");
                sql.Append("  and dat_po>= '01." + prm.month_.ToString() + "." + prm.year_.ToString() + "'");
                sql.Append("  and dat_s <= '" + DateTime.DaysInMonth(prm.year_, prm.month_).ToString() + ".");
#endif
                sql.Append(prm.month_ + "." + prm.year_.ToString() + "') ");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;

                sql.Remove(0, sql.Length);
#if PG
                sql.Append("  update t_kvars set izol  = 0 ");
                sql.Append("  where nzp_kvar in (select nzp from " + pref + "_data.prm_1 a ");
                sql.Append("  where a.nzp_prm =3 and a.nzp=t_kvars.nzp_kvar ");
                sql.Append("  and a.is_actual=1  and val_prm='2'");
                sql.Append("  and dat_po>= '01." + prm.month_.ToString() + "." + prm.year_.ToString() + "'");
                sql.Append("  and dat_s <= '" + DateTime.DaysInMonth(prm.year_, prm.month_).ToString() + ".");
                sql.Append(prm.month_ + "." + prm.year_.ToString() + "') ");
#else
                sql.Append("  update t_kvars set izol  = 0 ");
                sql.Append("  where nzp_kvar in (select nzp from " + pref + "_data:prm_1 a ");
                sql.Append("  where a.nzp_prm =3 and a.nzp=t_kvars.nzp_kvar ");
                sql.Append("  and a.is_actual=1  and val_prm='2'");
                sql.Append("  and dat_po>= '01." + prm.month_.ToString() + "." + prm.year_.ToString() + "'");
                sql.Append("  and dat_s <= '" + DateTime.DaysInMonth(prm.year_, prm.month_).ToString() + ".");
                sql.Append(prm.month_ + "." + prm.year_.ToString() + "') ");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;

                sql.Remove(0, sql.Length);
#if PG
                sql.Append("  update t_kvars set pl_gil  = (select sum(val_prm::numeric) ");
                sql.Append("  from " + pref + "_data.prm_1 a ");
                sql.Append("  where a.nzp_prm =6 and a.nzp=t_kvars.nzp_kvar ");
#else
                sql.Append("  update t_kvars set pl_gil  = (select sum(val_prm+0) ");
                sql.Append("  from " + pref + "_data:prm_1 a ");
                sql.Append("  where a.nzp_prm =6 and a.nzp=t_kvars.nzp_kvar ");
#endif
                sql.Append("  and a.is_actual=1  ");
                sql.Append("  and dat_po>= '01." + prm.month_.ToString() + "." + prm.year_.ToString() + "'");
                sql.Append("  and dat_s <= '" + DateTime.DaysInMonth(prm.year_, prm.month_).ToString() + ".");
                sql.Append(prm.month_ + "." + prm.year_.ToString() + "') ");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;


                //Временно потом из calc_gku
                sql2.Remove(0, sql2.Length);
#if PG
                sql2.Append(" update t_kvars set pl_kvar=(select max(squ) ");
                sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ".calc_gku_");
                sql2.Append(prm.month_.ToString("00") + " a");
                sql2.Append(" where a.nzp_kvar=t_kvars.nzp_kvar and nzp_serv=8 and a.tarif>0)");
#else
                sql2.Append(" update t_kvars set pl_kvar=(select max(squ) ");
                sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ":calc_gku_");
                sql2.Append(prm.month_.ToString("00") + " a");
                sql2.Append(" where a.nzp_kvar=t_kvars.nzp_kvar and nzp_serv=8 and a.tarif>0)");
#endif
                ExecRead(conn_db, out reader2, sql2.ToString(), true);


                /* sql2.Remove(0, sql2.Length);
                 sql2.Append(" update t_kvars set pl_kvar=pl_all where pl_kvar is null ");
                 ExecRead(conn_db, out reader2, sql2.ToString(), true);*/



                //Проставляем коэффициенты по отоплению
                //Изолированные
                sql2.Remove(0, sql2.Length);
#if PG
                sql2.Append(" update t_kvars set otop_koef=(select max(val_prm::numeric) ");
                sql2.Append(" from " + pref + "_data.prm_2 where val_prm~E'[0-9][0-9 ]{19}' and nzp_prm=723 and is_actual=1 ");
                sql2.Append(" and dat_s<=current_date and dat_po>=current_date and t_kvars.nzp_dom=nzp)");
                sql2.Append(" where izol=1 ");
#else
                sql2.Append(" update t_kvars set otop_koef=(select max(val_prm+0) ");
                sql2.Append(" from " + pref + "_data:prm_2 where nzp_prm=723 and is_actual=1 ");
                sql2.Append(" and dat_s<=today and dat_po>=today and t_kvars.nzp_dom=nzp)");
                sql2.Append(" where izol=1 ");
#endif
                ExecSQL(conn_db, sql2.ToString(), true);

                //Коммунальные
                sql2.Remove(0, sql2.Length);
#if PG
                sql2.Append(" update t_kvars set otop_koef=(select max(val_prm::numeric) ");
                sql2.Append(" from " + pref + "_data.prm_2 where val_prm~E'[0-9][0-9 ]{19}' and nzp_prm=2074 and is_actual=1 ");
                sql2.Append(" and dat_s<=current_date and dat_po>=current_date and t_kvars.nzp_dom=nzp)");
                sql2.Append(" where izol=0 ");
#else
                sql2.Append(" update t_kvars set otop_koef=(select max(val_prm+0) ");
                sql2.Append(" from " + pref + "_data:prm_2 where nzp_prm=2074 and is_actual=1 ");
                sql2.Append(" and dat_s<=today and dat_po>=today and t_kvars.nzp_dom=nzp)");
                sql2.Append(" where izol=0 ");
#endif
                ExecSQL(conn_db, sql2.ToString(), true);

                /* sql.Remove(0, sql.Length);
                 sql.Append("  update t_kvars set pl_kvar  = pl_all where pl_kvar is null and izol = 1");
                 if (!ExecSQL(conn_db, sql.ToString(), true).result)
                     return null;

                 sql.Remove(0, sql.Length);
                 sql.Append("  update t_kvars set pl_kvar  = pl_gil where pl_kvar is null and izol = 0");
                 if (!ExecSQL(conn_db, sql.ToString(), true).result)
                     return null;*/

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_local_heat set ");
                sql.Append(" pl_izol = (select sum( case when izol = 1 then pl_kvar else 0 end) from t_kvars a where a.nzp_dom=t_local_heat.nzp_dom), ");
                sql.Append(" pl_komm_all = (select sum( case when izol = 0 then pl_all else 0 end) from t_kvars a where a.nzp_dom=t_local_heat.nzp_dom), ");
                sql.Append(" pl_komm_gil = (select sum( case when izol = 0 then pl_kvar else 0 end) from t_kvars a where a.nzp_dom=t_local_heat.nzp_dom) ");
#else
                sql.Append(" update t_local_heat set (pl_izol, pl_komm_all, pl_komm_gil) =");
                sql.Append(" ((select sum( case when izol = 1 then pl_kvar else 0 end), ");
                sql.Append(" sum( case when izol = 0 then pl_all else 0 end), ");
                sql.Append(" sum( case when izol = 0 then pl_kvar else 0 end) ");
                sql.Append(" from t_kvars a where a.nzp_dom=t_local_heat.nzp_dom  )) ");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;

                //sql.Remove(0, sql.Length);
                //sql.Append(" update t_local_heat set pl_ngil = ");
                //sql.Append(" (select  sum(pl_kvar) ");
                //sql.Append(" from t_kvars a where a.nzp_dom=t_local_heat.nzp_dom and typek>1 ) ");
                //if (!ExecSQL(conn_db, sql.ToString(), true).result)
                //    return null;



                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_local_heat set pl_mop = ");
                sql.Append(" (select  sum(val_prm::numeric) ");
                sql.Append(" from " + pref + "_data.prm_2 b ");
                sql.Append(" where t_local_heat.nzp_dom=b.nzp ");
                sql.Append(" and b.nzp_prm =2049 and is_actual = 1");
                sql.Append("  and b.dat_po>= '01." + prm.month_.ToString() + "." + prm.year_.ToString() + "'");
                sql.Append("  and b.dat_s <= '" + DateTime.DaysInMonth(prm.year_, prm.month_).ToString() + ".");
                sql.Append(prm.month_ + "." + prm.year_.ToString() + "') ");
#else
                sql.Append(" update t_local_heat set pl_mop = ");
                sql.Append(" (select  sum(val_prm+0) ");
                sql.Append(" from " + pref + "_data:prm_2 b ");
                sql.Append(" where t_local_heat.nzp_dom=b.nzp ");
                sql.Append(" and b.nzp_prm =2049 ");
                sql.Append("  and b.dat_po>= '01." + prm.month_.ToString() + "." + prm.year_.ToString() + "'");
                sql.Append("  and b.dat_s <= '" + DateTime.DaysInMonth(prm.year_, prm.month_).ToString() + ".");
                sql.Append(prm.month_ + "." + prm.year_.ToString() + "') ");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;


                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_local_heat set pl_all = ");
                sql.Append(" (select  sum(val_prm::numeric) ");
                sql.Append(" from " + pref + "_data.prm_2 b ");
                sql.Append(" where t_local_heat.nzp_dom=b.nzp ");
                sql.Append(" and b.nzp_prm =40 and is_actual = 1 ");
                sql.Append("  and b.dat_po>= '01." + prm.month_.ToString() + "." + prm.year_.ToString() + "'");
                sql.Append("  and b.dat_s <= '" + DateTime.DaysInMonth(prm.year_, prm.month_).ToString() + ".");
                sql.Append(prm.month_ + "." + prm.year_.ToString() + "') ");
#else
                sql.Append(" update t_local_heat set pl_all = ");
                sql.Append(" (select  sum(val_prm+0) ");
                sql.Append(" from " + pref + "_data:prm_2 b ");
                sql.Append(" where t_local_heat.nzp_dom=b.nzp ");
                sql.Append(" and b.nzp_prm =40 ");
                sql.Append("  and b.dat_po>= '01." + prm.month_.ToString() + "." + prm.year_.ToString() + "'");
                sql.Append("  and b.dat_s <= '" + DateTime.DaysInMonth(prm.year_, prm.month_).ToString() + ".");
                sql.Append(prm.month_ + "." + prm.year_.ToString() + "') ");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;


                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_local_heat set pl_ngil = ");
                sql.Append(" (select  sum(val_prm::numeric) ");
                sql.Append(" from " + pref + "_data.prm_2 b ");
                sql.Append(" where t_local_heat.nzp_dom=b.nzp ");
                sql.Append(" and b.nzp_prm =2051 and is_actual = 1 ");
                sql.Append("  and b.dat_po>= '01." + prm.month_.ToString() + "." + prm.year_.ToString() + "'");
                sql.Append("  and b.dat_s <= '" + DateTime.DaysInMonth(prm.year_, prm.month_).ToString() + ".");
                sql.Append(prm.month_ + "." + prm.year_.ToString() + "') ");
#else
                sql.Append(" update t_local_heat set pl_ngil = ");
                sql.Append(" (select  sum(val_prm+0) ");
                sql.Append(" from " + pref + "_data:prm_2 b ");
                sql.Append(" where t_local_heat.nzp_dom=b.nzp ");
                sql.Append(" and b.nzp_prm =2051 ");
                sql.Append("  and b.dat_po>= '01." + prm.month_.ToString() + "." + prm.year_.ToString() + "'");
                sql.Append("  and b.dat_s <= '" + DateTime.DaysInMonth(prm.year_, prm.month_).ToString() + ".");
                sql.Append(prm.month_ + "." + prm.year_.ToString() + "') ");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;


                ExecSQL(conn_db, "drop table t_charges", false);

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select  k.nzp_dom , a.nzp_serv, 0 norma, f.nzp_measure, a.nzp_frm ,a.tarif, ");
                sql.Append(" a.is_device, sum(a.rsum_tarif) as rsum_tarif, ");
                sql.Append(" sum(a.sum_real) as sum_real, ");
                sql.Append(" sum(case when f.nzp_measure = 4 and a.tarif>0 then a.c_calc ");
                sql.Append("          when f.nzp_measure <> 4 and a.tarif>0 then Round(k.pl_kvar*k.otop_koef,4) else 0 end) as c_calc, ");
                sql.Append(" sum(a.reval) as reval, ");
                sql.Append(" sum(a.real_charge) as real_charge, sum(0) as sum_nedop   ,");
                sql.Append(" sum(a.sum_charge) as sum_charge into temp t_charges ");
                sql.Append(" from " + sChargeAlias + "." + sChargeTable + " a,  ");
                sql.Append(" t_kvars k ," + pref + "_kernel.formuls f ");
                sql.Append(" where k.nzp_kvar =a.nzp_kvar and a.nzp_serv = 8 and a.nzp_frm =f.nzp_frm and dat_charge is null ");
#else
                sql.Append(" select  k.nzp_dom , a.nzp_serv, 0 norma, f.nzp_measure, a.nzp_frm ,a.tarif, ");
                sql.Append(" a.is_device, sum(a.rsum_tarif) as rsum_tarif, ");
                sql.Append(" sum(a.sum_real) as sum_real, ");
                sql.Append(" sum(case when f.nzp_measure = 4 and a.tarif>0 then a.c_calc ");
                sql.Append(
                    "          when f.nzp_measure <> 4 and a.tarif>0 then Round(k.pl_kvar*k.otop_koef,4) else 0 end) as c_calc, ");
                sql.Append(" sum(a.reval) as reval, ");
                sql.Append(" sum(a.real_charge) as real_charge, sum(0) as sum_nedop   ,");
                sql.Append(" sum(a.sum_charge) as sum_charge ");
                sql.Append(" from " + sChargeAlias + ":" + sChargeTable + " a,  ");
                sql.Append(" t_kvars k ," + pref + "_kernel:formuls f ");
                sql.Append(
                    " where k.nzp_kvar =a.nzp_kvar and a.nzp_serv = 8 and a.nzp_frm =f.nzp_frm and dat_charge is null ");
#endif
                if (prm.nzp_key > -1)
                {
                    sql.Append(" and a.nzp_supp=" + prm.nzp_key);
                }
#if PG
                sql.Append(" group by 1, 2, 3, 4, 5, 6, 7 ");
#else
                sql.Append(" group by 1, 2, 3, 4, 5, 6, 7 into temp t_charges with no log ");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;
                //Перерасчеты
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_charges set reval = coalesce((SELECT reval from(SELECT nzp_dom, nzp_serv, sum(reval) as reval from " + sChargeAlias + ".reval_" + prm.month_.ToString("00"));
                sql.Append(" a INNER JOIN " + pref + "_data.kvar b on b.nzp_kvar = a.nzp_kvar group by 1, 2) t ");
                sql.Append(" where t_charges.nzp_serv = t.nzp_serv and t_charges.nzp_dom = t.nzp_dom), 0)");

                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;

                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_charges set reval = reval + coalesce((SELECT reval / (SELECT count(*) FROM t_charges where t_charges.nzp_dom = t.nzp_dom) from(SELECT nzp_dom, nzp_serv, sum(sum_rcl) as reval from " + sChargeAlias + ".perekidka ");
                sql.Append(" a INNER JOIN " + pref + "_data.kvar b on b.nzp_kvar = a.nzp_kvar INNER JOIN fbill_data.document_base d on d.nzp_doc_base = a.nzp_doc_base where month_ = " + prm.month_.ToString() + "  AND d.comment != 'Выравнивание сальдо' group by 1, 2) t ");
                sql.Append(" where t_charges.nzp_serv = t.nzp_serv and t_charges.nzp_dom = t.nzp_dom), 0)");

                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;

                sql.Remove(0, sql.Length);
#if PG
                sql.Append("  update t_local_heat set ");
                sql.Append(" rsum_tarif = (select sum(a.rsum_tarif) from t_charges a where t_local_heat.nzp_dom=a.nzp_dom), ");
                sql.Append(" sum_nedop = (select sum(sum_nedop) from t_charges a where t_local_heat.nzp_dom=a.nzp_dom), ");
                sql.Append(" reval = (select sum(reval+real_charge) from t_charges a where t_local_heat.nzp_dom=a.nzp_dom), ");
                sql.Append(" sum_charge = (select coalesce(sum(rsum_tarif),0) + coalesce(sum(reval),0) + coalesce(sum(sum_nedop),0) from t_charges a where t_local_heat.nzp_dom=a.nzp_dom) ");
#else
                sql.Append("  update t_local_heat set (rsum_tarif, sum_nedop, reval, sum_charge)=");
                sql.Append(" ((select  sum(a.rsum_tarif), ");
                sql.Append(" sum(sum_nedop), sum(reval+real_charge), sum(sum_charge) ");
                sql.Append(" from t_charges a where t_local_heat.nzp_dom=a.nzp_dom))");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;



                #region Выборка недопоставок

                GetSumNedopVinovnik(conn_db, prm.year_, prm.month_, 8, "sel_kvar_heat", out ret);
                if (ret.result == false) return null;

                #endregion

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_local_heat set ");
                sql.Append(" vozv_rso  = (select sum(case when nzp_supp=nzp_vinovnik then sum_nedop else 0 end) from t_nedop_sum where t_local_heat.nzp_dom= t_nedop_sum.nzp_dom ");
                if (prm.nzp_key > -1) sql.Append(" and nzp_supp=" + prm.nzp_key);
                sql.Append(" ), ");
                sql.Append(" vozv_uk = (select sum(case when nzp_supp=nzp_vinovnik then 0 else sum_nedop end) from t_nedop_sum where t_local_heat.nzp_dom= t_nedop_sum.nzp_dom ");
                if (prm.nzp_key > -1) sql.Append(" and nzp_supp=" + prm.nzp_key);
                sql.Append(" ) ");
#else
                sql.Append(" update t_local_heat set (vozv_rso, vozv_uk)=");
                sql.Append(" ((select sum(case when nzp_supp=nzp_vinovnik then sum_nedop else 0 end),");
                sql.Append(" sum(case when nzp_supp=nzp_vinovnik then 0 else sum_nedop end)  ");
                sql.Append(" from t_nedop_sum where t_local_heat.nzp_dom= t_nedop_sum.nzp_dom");
                if (prm.nzp_key > -1) sql.Append(" and nzp_supp=" + prm.nzp_key);
                sql.Append("))");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;


                #region Выборка перерасчетов прошлого периода

                ExecSQL(conn_db, "drop table t_nedop", false);
                ExecSQL(conn_db, "drop table t_sum_nedop", false);


                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" create temp table t_sum_nedop( nzp_dom integer, ");
                sql.Append(" sum_nedop numeric(14,2)) ");
#else
                sql.Append(" create temp table t_sum_nedop( nzp_dom integer, ");
                sql.Append(" sum_nedop decimal(14,2)) with no log");
#endif
                ExecSQL(conn_db, sql.ToString(), true);


                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select b.nzp_dom, a.nzp_kvar, min(date_part('year',dat_s)*12+date_part('month',dat_s)) as month_s,  max(date_part('year',dat_po)*12+date_part('month',dat_po)) as month_po");
                sql.Append(" into temp t_nedop from " + pref + "_data.nedop_kvar a, t_kvars b ");
                sql.Append(" where a.nzp_kvar=b.nzp_kvar and month_calc='01."
                    + prm.month_.ToString("00") + "." +
                    prm.year_.ToString("0000") + "' and a.nzp_serv in (9,14,414,513,514,518,1010052,1010053) ");
                sql.Append(" group by 1,2   ");
#else
                sql.Append(
                    " select b.nzp_dom, a.nzp_kvar, min(year(dat_s)*12+month(dat_s)) as month_s,  max(year(dat_po)*12+month(dat_po)) as month_po");
                sql.Append(" from " + pref + "_data:nedop_kvar a, t_kvars b ");
                sql.Append(" where a.nzp_kvar=b.nzp_kvar and month_calc='01."
                           + prm.month_.ToString("00") + "." +
                           prm.year_.ToString("0000") + "' and a.nzp_serv in (9,14,414,513,514,518) ");
                sql.Append(" group by 1,2 into temp t_nedop with no log");
#endif
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    reader.Close();
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }


                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select month_, year_ ");
                sql.Append(" from " + sChargeAlias + ".lnk_charge_" + prm.month_.ToString("00") + " b, t_nedop d ");
                sql.Append(" where  b.nzp_kvar=d.nzp_kvar and year_*12+month_>=month_s and  year_*12+month_<=month_po");
                sql.Append(" group by 1,2");
#else
                sql.Append(" select month_, year_ ");
                sql.Append(" from " + sChargeAlias + ":lnk_charge_" + prm.month_.ToString("00") + " b, t_nedop d ");
                sql.Append(" where  b.nzp_kvar=d.nzp_kvar and year_*12+month_>=month_s and  year_*12+month_<=month_po");
                sql.Append(" group by 1,2");
#endif
                if (!ExecRead(conn_db, out reader2, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    reader.Close();
                    conn_db.Close();
                    ret.result = false;
                    return null;
                }
                while (reader2.Read())
                {
                    string sTmpAlias = pref + "_charge_" +
                                       (Int32.Parse(reader2["year_"].ToString()) - 2000).ToString("00");

                    sql.Remove(0, sql.Length);
                    sql.Append(" insert into t_sum_nedop (nzp_dom,  sum_nedop) ");
                    sql.Append(" select nzp_dom,  ");
                    sql.Append(" sum(sum_nedop-sum_nedop_p) ");
#if PG
                    sql.Append(" from " + sTmpAlias + ".charge_" + Int32.Parse(reader2["month_"].ToString()).ToString("00"));
#else
                    sql.Append(" from " + sTmpAlias + ":charge_" +
                               Int32.Parse(reader2["month_"].ToString()).ToString("00"));
#endif
                    sql.Append(" b, t_nedop d ");
                    sql.Append(" where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28.");
                    sql.Append(prm.month_.ToString("00") + "." + prm.year_.ToString() + "')");
                    sql.Append(" and abs(sum_nedop)+abs(sum_nedop_p)>0.001");
                    sql.Append(" and nzp_serv = 8 ");
                    if (prm.nzp_key > -1)
                    {
                        sql.Append(" and nzp_supp=" + prm.nzp_key);
                    }
                    sql.Append(" group by 1");
                    if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }

                }
                reader2.Close();
                ExecSQL(conn_db, "drop table t_nedop", true);

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_local_heat set reval=(select coalesce((-1) * sum(reval),0)  ");
#else
                sql.Append(" update t_local_heat set reval=nvl(reval,0)+nvl((select sum(sum_nedop) ");
#endif
                sql.Append(" from t_charges where t_local_heat.nzp_dom=t_charges.nzp_dom ");
                sql.Append(" ) ");
                ExecSQL(conn_db, sql.ToString(), true);

                ExecSQL(conn_db, "drop table t_sum_nedop", true);

                #endregion

                #region Устанавливаем тарифы


                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_local_heat set tarif_gkal = coalesce(" +
#else
                sql.Append(" update t_local_heat set tarif_gkal = nvl(" +
#endif
 " (select max(tarif) from t_charges a" +
                           " where t_local_heat.nzp_dom=a.nzp_dom and a.nzp_measure = 4 ),0)");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;


                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_local_heat set tarif_gkal = coalesce(" +
                           " (select max(tarif_new_gk) from t_charges a," +
                           Points.Pref + "_data.a_trf_smr b " +
#else
                sql.Append(" update t_local_heat set tarif_gkal = nvl(" +
                           " (select max(tarif_new_gk) from t_charges a," +
                           Points.Pref + "_data:a_trf_smr b " +
#endif
 " where a.nzp_frm=b.nzp_frm and a.nzp_measure<>4 " +
                           " and t_local_heat.nzp_dom=a.nzp_dom and is_priv>0),0) where tarif_gkal = 0");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    return null;


                #endregion

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_local_heat set vozv_rso_gkal=round(coalesce(vozv_rso,0)/tarif_gkal,4),");
                sql.Append(" vozv_uk_gkal=round(coalesce(vozv_uk,0)/tarif_gkal,4), ");
                sql.Append(" reval_gkal=round(coalesce(reval,0)/tarif_gkal,4) ");
#else
                sql.Append(" update t_local_heat set vozv_rso_gkal=round(nvl(vozv_rso,0)/tarif_gkal,4),");
                sql.Append(" vozv_uk_gkal=round(nvl(vozv_uk,0)/tarif_gkal,4), ");
                sql.Append(" reval_gkal=round(nvl(reval,0)/tarif_gkal,4) ");
#endif
                sql.Append(" where tarif_gkal>0 ");
                ExecSQL(conn_db, sql.ToString(), true);



                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_local_heat set c_calc_gkal=coalesce((select sum(c_calc) ");
#else
                sql.Append(" update t_local_heat set c_calc_gkal=nvl((select sum(c_calc) ");
#endif
                sql.Append(" from t_charges where t_local_heat.nzp_dom= t_charges.nzp_dom),0) ");
                ExecSQL(conn_db, sql.ToString(), true);

                /*  sql.Remove(0, sql.Length);
                  sql.Append(" update t_local_heat set c_calc_gkal=nvl(c_calc_gkal,0)+nvl((select sum(Round(rsum_tarif/tarif_gkal,9)) ");
                  sql.Append(" from t_charges where nzp_measure<>4 and t_local_heat.nzp_dom= t_charges.nzp_dom),0) ");
                  sql.Append(" where nvl(tarif_gkal,0)>0 ");
                  ExecSQL(conn_db, sql.ToString(), true);*/



                //sql.Remove(0, sql.Length);
                //   sql.Append(" update t_local_heat set pl_all=nvl(pl_izol,0)+nvl(pl_komm_all,0)+nvl(pl_ngil,0)+nvl(pl_mop,0)");
                //  ExecSQL(conn_db, sql.ToString(), true);

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_local_heat set sum_nedop=coalesce(vozv_rso,0)+coalesce(vozv_uk,0)");
#else
                sql.Append(" update t_local_heat set sum_nedop=nvl(vozv_rso,0)+nvl(vozv_uk,0)");
#endif
                ExecSQL(conn_db, sql.ToString(), true);

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update t_local_heat set sum_charge_gkal=coalesce(c_calc_gkal,0)+coalesce(vozv_rso_gkal,0)");
                sql.Append(" -coalesce(reval_gkal,0)+coalesce(vozv_uk_gkal,0)");
#else
                sql.Append(" update t_local_heat set sum_charge_gkal=nvl(c_calc_gkal,0)+nvl(vozv_rso_gkal,0)");
                sql.Append(" +nvl(reval_gkal,0)+nvl(vozv_uk_gkal,0)");
#endif
                ExecSQL(conn_db, sql.ToString(), true);


                sql2.Remove(0, sql2.Length);
#if PG
                sql2.Append(" update t_local_heat set nzp_dom2 = (select nzp_dom_base from " + pref + "_data.link_dom_lit a");
                sql2.Append(" where a.nzp_dom=t_local_heat.nzp_dom) ");
                sql2.Append(" where nzp_dom in (select nzp_dom from " + pref + "_data.link_dom_lit)");
#else
                sql2.Append(" update t_local_heat set nzp_dom2 = (select nzp_dom_base from " + pref +
                            "_data:link_dom_lit a");
                sql2.Append(" where a.nzp_dom=t_local_heat.nzp_dom) ");
                sql2.Append(" where nzp_dom in (select nzp_dom from " + pref + "_data:link_dom_lit)");
#endif
                ExecSQL(conn_db, sql2.ToString(), true);

                sql.Remove(0, sql.Length);
                sql.Append(" insert into t_svod_heat (nzp_dom , pl_all, pl_izol, pl_komm_all,  pl_komm_gil,");
                sql.Append(" pl_ngil, pl_mop, rsum_tarif, c_calc_gkal,  vozv_rso,  vozv_uk, ");
                sql.Append(" vozv_rso_gkal, vozv_uk_gkal, sum_nedop,  reval_gkal, reval,");
                sql.Append(" sum_charge_gkal, sum_charge)");
                sql.Append(" select  nzp_dom2 , sum(pl_all), sum(pl_izol), sum(pl_komm_all),  sum(pl_komm_gil),");
                sql.Append(
                    " sum(pl_ngil), sum(pl_mop) , sum(rsum_tarif), sum(c_calc_gkal),  sum(vozv_rso),  sum(vozv_uk), ");
                sql.Append(" sum(vozv_rso_gkal), sum(vozv_uk_gkal), sum(sum_nedop),  sum(reval_gkal), sum(reval),");
                sql.Append(" sum(sum_charge_gkal), sum(sum_charge) ");
                sql.Append(" from t_local_heat ");
                sql.Append(" group by 1 ");
                ExecSQL(conn_db, sql.ToString(), true);

                ExecSQL(conn_db, "drop table t_nedop_sum", true);
                ExecSQL(conn_db, "drop table t_local_heat", true);
            }

            
            sql.Remove(0, sql.Length);
            sql.Append(@"UPDATE t_svod_heat SET rsum_tarif = CASE WHEN rsum_tarif is null THEN 0 ELSE rsum_tarif END, vozv_rso = CASE WHEN vozv_rso is null THEN 0 ELSE vozv_rso END, vozv_uk = CASE WHEN vozv_uk is null THEN 0 ELSE vozv_uk END, 
                      vozv_rso_gkal = CASE WHEN vozv_rso_gkal is null THEN 0 ELSE vozv_rso_gkal END, vozv_uk_gkal = CASE WHEN vozv_uk_gkal is null THEN 0 ELSE vozv_uk_gkal END, reval_gkal = CASE WHEN reval_gkal is null THEN 0 ELSE reval_gkal END, 
                      odpu_gkal = CASE WHEN odpu_gkal is null THEN 0 ELSE odpu_gkal END, norma = CASE WHEN norma is null THEN 0 ELSE norma END");

            ExecSQL(conn_db, sql.ToString(), true);

            sql.Remove(0, sql.Length);
            sql.Append(" select trim(case when rajon = '-' then town else rajon end)||', '||trim(ulica) as ulica," +
                       "  ndom, idom, nkor, a.*" +
                       "  from t_svod_heat a, " +
                       Points.Pref + DBManager.sDataAliasRest + "dom d, " +
                       Points.Pref + DBManager.sDataAliasRest + "s_ulica su, " +
                       Points.Pref + DBManager.sDataAliasRest + "s_rajon sr, " +
                       Points.Pref + DBManager.sDataAliasRest + "s_town st " +
                       " where a.nzp_dom=d.nzp_dom " +
                       " and d.nzp_ul=su.nzp_ul " +
                       "       and su.nzp_raj=sr.nzp_raj " +
                       "       and sr.nzp_town=st.nzp_town " +
                       " order by ulica, idom, ndom, nkor");

            try
            {

                LocalTable = DBManager.ExecSQLToTable(conn_db, sql.ToString());
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("!!!" + ex.Message, MonitorLog.typelog.Error, true);
                conn_web.Close();
                return null;
            }

            ExecSQL(conn_db, "drop table t_svod_heat", true);

            #endregion


            return LocalTable;
        }
    }
}
