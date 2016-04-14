using System;
using System.Text;
using System.Data;
using STCLINE.KP50.Global;

using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public partial class ExcelRep
    {


        public DataTable GetGilFondStat(Prm prm, out Returns ret, string Nzp_user)
        {
            ret = Utils.InitReturns();
            return GetGilFondStatZhig(prm, out ret, Nzp_user);

            //            #region Подключение к БД
            //            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);
            //            IDataReader reader;
            //            IDataReader reader2;

            //            ret = OpenDb(conn_web, true);
            //            if (!ret.result)
            //            {
            //                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
            //                return null;
            //            }


            //            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);

            //            ret = OpenDb(conn_db, true);
            //            if (!ret.result)
            //            {
            //                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с локальной БД ", MonitorLog.typelog.Error, true);
            //                conn_web.Close();
            //                ret.result = false;
            //                return null;
            //            }

            //            #endregion

            //            StringBuilder sql = new StringBuilder();
            //            StringBuilder sql2 = new StringBuilder();
            //#if PG
            //            string tXX_spls = "public" + "." + "t" + Nzp_user + "_spls";
            //#else
            //            string tXX_spls = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "t" + Nzp_user + "_spls";
            //#endif
            //            string pref = "";
            //#if PG
            //            string dataUsl = " and dat_s::date<=date('01." + prm.month_.ToString() + "." + prm.year_.ToString() + "') " +
            //                             " and dat_po>=date('01." + prm.month_.ToString() + "." + prm.year_.ToString() + "') ";
            //#else
            //            string dataUsl = " and dat_s<=date('01." + prm.month_.ToString() + "." + prm.year_.ToString() + "') " +
            //                             " and dat_po>=date('01." + prm.month_.ToString() + "." + prm.year_.ToString() + "') ";
            //#endif
            //            #region Выборка по локальным банкам
            //            DataTable LocalTable = new DataTable();

            //            ExecSQL(conn_db, "drop table t_svod", false);
            //#if PG
            //            sql2.Remove(0, sql2.Length);
            //            sql2.Append(" create unlogged  table t_svod( ");
            //            sql2.Append(" tip_str integer,");            // 0 обычное 1 по услугам 2 итого
            //            sql2.Append(" tip_doma char(100),");
            //            sql2.Append(" frm_name char(100),");
            //            sql2.Append(" count_gilp integer,");
            //            sql2.Append(" count_dom integer,");
            //            sql2.Append(" pl_dom numeric(14,2),");
            //            sql2.Append(" pl_mop numeric(14,2),");
            //            sql2.Append(" pl_ngil numeric(14,2),");
            //            sql2.Append(" pl_kvar_year numeric(14,2),");
            //            sql2.Append(" pl_kvar numeric(14,2),");
            //            sql2.Append(" pl_gil numeric(14,2),");
            //            sql2.Append(" pl_calc numeric(14,2),");
            //            sql2.Append(" count_ls integer,");
            //            sql2.Append(" count_gil integer,");
            //            sql2.Append(" rsum_tarif numeric(14,2),");
            //            sql2.Append(" count_close integer,");
            //            sql2.Append(" pl_close numeric(14,2),");
            //            sql2.Append(" pl_calc_close numeric(14,2),");
            //            sql2.Append(" count_ls_i integer,");
            //            sql2.Append(" count_ls_k integer,");
            //            sql2.Append(" count_gil_i integer, ");
            //            sql2.Append(" count_gil_k integer, ");
            //            sql2.Append(" tarif numeric(14,2), ");
            //            sql2.Append(" tarif_i numeric(14,2), ");
            //            sql2.Append(" tarif_k numeric(14,2),");
            //            sql2.Append(" pl_calc_i numeric(14,2), ");
            //            sql2.Append(" pl_calc_k numeric(14,2),");
            //            sql2.Append(" rsum_tarif_close numeric(14,2), ");
            //            sql2.Append(" rsum_tarif_i numeric(14,2), ");
            //            sql2.Append(" rsum_tarif_k numeric(14,2))  ");
            //#else
            //            sql2.Remove(0, sql2.Length);
            //            sql2.Append(" create temp table t_svod( ");
            //            sql2.Append(" tip_str integer,");            // 0 обычное 1 по услугам 2 итого
            //            sql2.Append(" tip_doma char(100),");
            //            sql2.Append(" frm_name char(100),");
            //            sql2.Append(" count_gilp integer,");
            //            sql2.Append(" count_dom integer,");
            //            sql2.Append(" pl_dom Decimal(14,2),");
            //            sql2.Append(" pl_mop Decimal(14,2),");
            //            sql2.Append(" pl_ngil Decimal(14,2),");
            //            sql2.Append(" pl_kvar_year Decimal(14,2),");
            //            sql2.Append(" pl_kvar Decimal(14,2),");
            //            sql2.Append(" pl_gil Decimal(14,2),");
            //            sql2.Append(" pl_calc Decimal(14,2),");
            //            sql2.Append(" count_ls integer,");
            //            sql2.Append(" count_gil integer,");
            //            sql2.Append(" rsum_tarif Decimal(14,2),");
            //            sql2.Append(" count_close integer,");
            //            sql2.Append(" pl_close Decimal(14,2),");
            //            sql2.Append(" pl_calc_close Decimal(14,2),");
            //            sql2.Append(" count_ls_i integer,");
            //            sql2.Append(" count_ls_k integer,");
            //            sql2.Append(" count_gil_i integer, ");
            //            sql2.Append(" count_gil_k integer, ");
            //            sql2.Append(" tarif Decimal(14,2), ");
            //            sql2.Append(" tarif_i Decimal(14,2), ");
            //            sql2.Append(" tarif_k Decimal(14,2),");
            //            sql2.Append(" pl_calc_i Decimal(14,2), ");
            //            sql2.Append(" pl_calc_k Decimal(14,2),");
            //            sql2.Append(" rsum_tarif_close Decimal(14,2), ");
            //            sql2.Append(" rsum_tarif_i Decimal(14,2), ");
            //            sql2.Append(" rsum_tarif_k Decimal(14,2)) with no log");
            //#endif
            //            if (!ExecSQL(conn_db, sql2.ToString(), true).result)
            //            {
            //                MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
            //                conn_web.Close();
            //                conn_db.Close();
            //                ret.result = false;
            //                return null;
            //            }

            //            ExecSQL(conn_db, "drop table t_sodergkv", false);

            //            sql.Remove(0, sql.Length);
            //            sql.Append(" select nzp_dom, nzp_kvar, nzp_area, pref ");
            //#if PG
            //            sql.Append("  into unlogged  t_sodergkv  from  " + tXX_spls + " ");
            //#else
            //            sql.Append(" from  " + tXX_spls + " into temp t_sodergkv with no log");
            //#endif
            //            if (!ExecSQL(conn_db, sql.ToString(), true).result)
            //            {
            //                MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
            //                conn_web.Close();
            //                conn_db.Close();
            //                ret.result = false;
            //                return null;
            //            }
            //            conn_web.Close();

            //            sql.Remove(0, sql.Length);
            //            sql.Append(" select pref ");
            //            sql.Append(" from  t_sodergkv group by 1 ");
            //            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            //            {
            //                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
            //                ret.result = false;
            //                conn_db.Close();
            //                return null;
            //            }
            //            while (reader.Read())
            //            {
            //                if (reader["pref"] != null)
            //                {
            //                    pref = Convert.ToString(reader["pref"]).Trim();
            //                    string dbCharge = pref + "_charge_" + (prm.year_ - 2000).ToString("00");

            //                    #region Заполнение sel_kvar8
            //                    ExecRead(conn_db, out reader2, "drop table sel_kvar8", false);
            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" create unlogged  table sel_kvar8( ");
            //                    sql2.Append(" nzp_kvar integer,");
            //                    sql2.Append(" nzp_dom integer,");
            //                    sql2.Append(" nzp_frm integer,");
            //                    sql2.Append(" isol integer default 1,");
            //                    sql2.Append(" isclose integer default 0,");
            //                    sql2.Append(" count_gil integer,");
            //                    sql2.Append(" count_gilp integer,");
            //                    sql2.Append(" pl_kvar numeric(14,2),");
            //                    sql2.Append(" pl_kvar_year numeric(14,2),");
            //                    sql2.Append(" tarif numeric(14,2),");
            //                    sql2.Append(" pl_gil numeric(14,2),");
            //                    sql2.Append(" pl_calc numeric(14,2))  ");
            //#else
            //                    sql2.Append(" create temp table sel_kvar8( ");
            //                    sql2.Append(" nzp_kvar integer,");
            //                    sql2.Append(" nzp_dom integer,");
            //                    sql2.Append(" nzp_frm integer,");
            //                    sql2.Append(" isol integer default 1,");
            //                    sql2.Append(" isclose integer default 0,");
            //                    sql2.Append(" count_gil integer,");
            //                    sql2.Append(" count_gilp integer,");
            //                    sql2.Append(" pl_kvar Decimal(14,2),");
            //                    sql2.Append(" pl_kvar_year Decimal(14,2),");
            //                    sql2.Append(" tarif Decimal(14,2),");
            //                    sql2.Append(" pl_gil Decimal(14,2),");
            //                    sql2.Append(" pl_calc Decimal(14,2)) with no log");
            //#endif
            //                    if (!ExecSQL(conn_db, sql2.ToString(), true).result)
            //                    {
            //                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
            //                        conn_db.Close();
            //                        ret.result = false;
            //                        return null;
            //                    }



            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" insert into sel_kvar8(nzp_dom, nzp_kvar, isol, isclose)");
            //                    sql2.Append(" select k.nzp_dom, k.nzp_kvar, 1 as isol,0 ");
            //                    sql2.Append(" from t_sodergkv k, " + pref + "_data.dom d ");
            //                    sql2.Append(" where k.nzp_dom=d.nzp_dom and k.nzp_area=d.nzp_area and ");
            //                    sql2.Append(" 0 = (select count(*) from " + pref + "_data.prm_2 p where nzp_prm=2029 ");
            //                    sql2.Append(" and is_actual = 1 " + dataUsl + " and val_prm::date<=date('01." + prm.month_ + "." + prm.year_ + "') and k.nzp_dom=nzp )");
            //                    sql2.Append(" and pref ='" + pref + "'");
            //#else
            //                    sql2.Append(" insert into sel_kvar8(nzp_dom, nzp_kvar, isol, isclose)");
            //                    sql2.Append(" select k.nzp_dom, k.nzp_kvar, 1 as isol,0 ");
            //                    sql2.Append(" from t_sodergkv k, " + pref + "_data:dom d ");
            //                    sql2.Append(" where k.nzp_dom=d.nzp_dom and k.nzp_area=d.nzp_area and ");
            //                    sql2.Append(" 0 = (select count(*) from " + pref + "_data:prm_2 p where nzp_prm=2029 ");
            //                    sql2.Append(" and is_actual = 1 " + dataUsl + " and val_prm<=date('01." + prm.month_ + "." + prm.year_ + "') and k.nzp_dom=nzp )");
            //                    sql2.Append(" and pref ='" + pref + "'");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);



            //                    sql2.Remove(0, sql2.Length);
            //                    sql2.Append("create index ix_tmpk_01 on sel_kvar8(nzp_kvar)");
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append("analyze sel_kvar8 ");
            //#else
            //                    sql2.Append("update statistics for table sel_kvar8 ");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    //Изолированные /коммунальные
            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" update sel_kvar8 set isol=2");
            //                    sql2.Append(" where nzp_kvar in (select nzp from ");
            //                    sql2.Append(pref + "_data.prm_1 where nzp_prm=3 and val_prm='2' ");
            //                    sql2.Append(" and is_actual=1 " + dataUsl + ")");
            //#else
            //                    sql2.Append(" update sel_kvar8 set isol=2");
            //                    sql2.Append(" where nzp_kvar in (select nzp from ");
            //                    sql2.Append(pref + "_data:prm_1 where nzp_prm=3 and val_prm='2' ");
            //                    sql2.Append(" and is_actual=1 " + dataUsl + ")");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    //Открытые /закрытые
            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" update sel_kvar8 set isclose=1 ");
            //                    sql2.Append(" where nzp_kvar in (select nzp from ");
            //                    sql2.Append(pref + "_data.prm_3 where nzp_prm=51 and val_prm='2' ");
            //                    sql2.Append(" and is_actual=1 " + dataUsl + ")");
            //#else
            //                    sql2.Append(" update sel_kvar8 set isclose=1 ");
            //                    sql2.Append(" where nzp_kvar in (select nzp from ");
            //                    sql2.Append(pref + "_data:prm_3 where nzp_prm=51 and val_prm='2' ");
            //                    sql2.Append(" and is_actual=1 " + dataUsl + ")");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);


            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" update sel_kvar8 set pl_kvar = " +
            //                        " (select max(squ) ");
            //                    sql2.Append(" from  " + dbCharge + ".calc_gku_" + prm.month_.ToString("00") + " d ");
            //                    sql2.Append(" where d.nzp_kvar=sel_kvar8.nzp_kvar and d.tarif>0  ");
            //                    sql2.Append("  ), count_gil= " +
            //                                                " (select   max(gil) ");
            //                    sql2.Append(" from  " + dbCharge + ".calc_gku_" + prm.month_.ToString("00") + " d ");
            //                    sql2.Append(" where d.nzp_kvar=sel_kvar8.nzp_kvar and d.tarif>0 ) ");
            //#else
            //                    sql2.Append(" update sel_kvar8 set (pl_calc, count_gil) = ((select max(squ), max(gil) ");
            //                    sql2.Append(" from  " + dbCharge + ":calc_gku_" + prm.month_.ToString("00") + " d ");
            //                    sql2.Append(" where d.nzp_kvar=sel_kvar8.nzp_kvar and d.tarif>0  ");
            //                    sql2.Append("  ))");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);




            //                    //Площадь квартир
            //                    sql2.Remove(0, sql2.Length);
            //                    sql2.Append(" update sel_kvar8 set pl_kvar=(select max(Replace(val_prm,',','.')+0)");
            //                    sql2.Append(" from " + pref + "_data:prm_1 a where nzp_prm=4 and sel_kvar8.nzp_kvar=a.nzp ");
            //                    sql2.Append(" and is_actual=1 " + dataUsl + ")");
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    //Площадь квартир на начало года
            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" update sel_kvar8 set pl_kvar_year=(select max(Replace(val_prm,',','.'))::NUMERIC");
            //                    sql2.Append(" from " + pref + "_data.prm_1 a where nzp_prm=4 and sel_kvar8.nzp_kvar=a.nzp ");
            //                    sql2.Append(" and is_actual=1 and dat_s<=MDY(01,01," + prm.year_.ToString() + ")");
            //                    sql2.Append(" and dat_po>=MDY(01,01," + prm.year_.ToString() + "))");
            //#else
            //                    sql2.Append(" update sel_kvar8 set pl_kvar_year=(select max(Replace(val_prm,',','.')+0)");
            //                    sql2.Append(" from " + pref + "_data:prm_1 a where nzp_prm=4 and sel_kvar8.nzp_kvar=a.nzp ");
            //                    sql2.Append(" and is_actual=1 and dat_s<=MDY(01,01," + prm.year_.ToString() + ")");
            //                    sql2.Append(" and dat_po>=MDY(01,01," + prm.year_.ToString() + "))");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    //Жилая площадь
            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" update sel_kvar8 set pl_gil=(select max(Replace(val_prm,',','.'))::numeric ");
            //                    sql2.Append(" from " + pref + "_data.prm_1 a where nzp_prm=6 and sel_kvar8.nzp_kvar=a.nzp ");
            //                    sql2.Append(" and is_actual=1 " + dataUsl + ")");
            //#else
            //                    sql2.Append(" update sel_kvar8 set pl_gil=(select max(Replace(val_prm,',','.')+0)");
            //                    sql2.Append(" from " + pref + "_data:prm_1 a where nzp_prm=6 and sel_kvar8.nzp_kvar=a.nzp ");
            //                    sql2.Append(" and is_actual=1 " + dataUsl + ")");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    //Количество жильцов
            //                    //Временно потом из calc_gku
            //                    /*   sql2.Remove(0, sql2.Length);
            //                    sql2.Append(" update sel_kvar8 set count_gil = ((select max(cnt1) ");
            //                    sql2.Append(" from " + dbCharge + ":counters_");
            //                    sql2.Append(prm.month_.ToString("00") + " a");
            //                    sql2.Append(" where a.nzp_kvar = sel_kvar8.nzp_kvar and nzp_type=3 and stek=3))");
            //                    ExecRead(conn_db, out reader2, sql2.ToString(), true);*/

            //                    //sql2.Remove(0, sql2.Length);
            //                    //sql2.Append(" update sel_kvar8 set count_gil=(select max(val_prm+0)");
            //                    //sql2.Append(" from " + pref + "_data:prm_1 a where nzp_prm=5 and sel_kvar8.nzp_kvar=a.nzp ");
            //                    //sql2.Append(" and is_actual=1 " + dataUsl + ")");
            //                    //ExecSQL(conn_db, sql2.ToString(), true);

            //                    //Определить расчетную площадь для изолированных квартир как общую, для коммунальных - как жилую
            //                    //sql2.Remove(0, sql2.Length);
            //                    //sql2.Append(" update sel_kvar8 set pl_calc = (case when isol = 1 then pl_kvar else pl_gil end)");
            //                    //ExecSQL(conn_db, sql2.ToString(), true);

            //                    /////////////////

            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" update sel_kvar8 set count_gilp=(select max(val_prm)::int");
            //                    sql2.Append(" from " + pref + "_data.prm_1 a where nzp_prm=2005 and sel_kvar8.nzp_kvar=a.nzp ");
            //                    sql2.Append(" and is_actual=1 " + dataUsl + ")");
            //#else
            //                    sql2.Append(" update sel_kvar8 set count_gilp=(select max(val_prm+0)");
            //                    sql2.Append(" from " + pref + "_data:prm_1 a where nzp_prm=2005 and sel_kvar8.nzp_kvar=a.nzp ");
            //                    sql2.Append(" and is_actual=1 " + dataUsl + ")");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);


            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" update sel_kvar8 set nzp_frm=(select max(nzp_frm)");
            //                    sql2.Append(" from " + pref + "_data.tarif a where nzp_serv=17 and sel_kvar8.nzp_kvar=a.nzp_kvar ");
            //                    sql2.Append(" and is_actual=1 " + dataUsl + ")");
            //#else
            //                    sql2.Append(" update sel_kvar8 set nzp_frm=(select max(nzp_frm)");
            //                    sql2.Append(" from " + pref + "_data:tarif a where nzp_serv=17 and sel_kvar8.nzp_kvar=a.nzp_kvar ");
            //                    sql2.Append(" and is_actual=1 " + dataUsl + ")");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" update sel_kvar8 set tarif=(select max(tarif_new)");
            //                    sql2.Append(" from " + Points.Pref + "_data.a_trf_smr a ");
            //                    sql2.Append(" where sel_kvar8.nzp_frm=a.nzp_frm)");
            //#else
            //                    sql2.Append(" update sel_kvar8 set tarif=(select max(tarif_new)");
            //                    sql2.Append(" from " + Points.Pref + "_data:a_trf_smr a ");
            //                    sql2.Append(" where sel_kvar8.nzp_frm=a.nzp_frm)");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);


            //                    /*sql2.Remove(0, sql2.Length);
            //                    sql2.Append(" update sel_kvar8 set pl_calc=nvl((select sum(rashod)");
            //                    sql2.Append(" from " + dbCharge + ":calc_gku_" + prm.month_.ToString("00") + " a");
            //                    sql2.Append(" where sel_kvar8.nzp_kvar=a.nzp_kvar and nzp_serv=17 ),0) where is_close = 0 ");
            //                    ExecRead(conn_db, out reader2, sql2.ToString(), true);


            //                    sql2.Remove(0, sql2.Length);
            //                    sql2.Append(" update sel_kvar8 set is_close=4 where pl_calc>0 and is_close=");
            //                    ExecRead(conn_db, out reader2, sql2.ToString(), true);*/
            //                    #endregion


            //                    ExecSQL(conn_db, "drop table t1", false);
            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" select nzp_dom, (case when a.nzp_frm=0 then b.nzp_frm else a.nzp_frm end) as nzp_frm, ");
            //                    sql2.Append(" a.tarif, count(distinct num_ls) as count_ls,");
            //                    sql2.Append(" sum(rsum_tarif) as rsum_tarif, sum(pl_calc) as pl_calc,");
            //                    sql2.Append(" sum(count_gil) as count_gil");
            //                    sql2.Append("  into unlogged t1  from  " + dbCharge + ".charge_" + prm.month_.ToString("00") + " a, sel_kvar8 b");
            //                    sql2.Append(" where nzp_serv=17 and a.nzp_kvar=b.nzp_kvar ");
            //                    sql2.Append(" and dat_charge is null and coalesce(isol,1)=1 and isclose=0 and a.tarif>0 ");
            //                    sql2.Append(" group by 1,2,3");
            //                    sql2.Append("");
            //#else
            //                    sql2.Append(" select nzp_dom, (case when a.nzp_frm=0 then b.nzp_frm else a.nzp_frm end) as nzp_frm, ");
            //                    sql2.Append(" a.tarif, count(unique num_ls) as count_ls,");
            //                    sql2.Append(" sum(rsum_tarif) as rsum_tarif, sum(pl_calc) as pl_calc,");
            //                    sql2.Append(" sum(count_gil) as count_gil");
            //                    sql2.Append(" from  " + dbCharge + ":charge_" + prm.month_.ToString("00") + " a, sel_kvar8 b");
            //                    sql2.Append(" where nzp_serv=17 and a.nzp_kvar=b.nzp_kvar ");
            //                    sql2.Append(" and dat_charge is null and nvl(isol,1)=1 and isclose=0 and a.tarif>0 ");
            //                    sql2.Append(" group by 1,2,3");
            //                    sql2.Append(" into temp t1 with no log");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);



            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" select nzp_dom, (case when a.nzp_frm=0 then b.nzp_frm else a.nzp_frm end) as nzp_frm, ");
            //                    sql2.Append(" a.tarif, count(distinct num_ls) as count_ls,");
            //                    sql2.Append(" sum(rsum_tarif) as rsum_tarif, sum(pl_calc) as pl_calc,");
            //                    sql2.Append(" sum(count_gil) as count_gil");
            //                    sql2.Append(" into unlogged  t2   from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ".charge_");
            //                    sql2.Append(prm.month_.ToString("00") + " a , sel_kvar8 b");
            //                    sql2.Append(" where nzp_serv=17 and a.nzp_kvar=b.nzp_kvar and isclose=0  and a.tarif>0 ");
            //                    sql2.Append(" and dat_charge is null  and isol=2");
            //                    sql2.Append(" group by 1,2,3");
            //                    sql2.Append(" ");
            //#else
            //                    sql2.Append(" select nzp_dom, (case when a.nzp_frm=0 then b.nzp_frm else a.nzp_frm end) as nzp_frm, ");
            //                    sql2.Append(" a.tarif, count(unique num_ls) as count_ls,");
            //                    sql2.Append(" sum(rsum_tarif) as rsum_tarif, sum(pl_calc) as pl_calc,");
            //                    sql2.Append(" sum(count_gil) as count_gil");
            //                    sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ":charge_");
            //                    sql2.Append(prm.month_.ToString("00") + " a a, sel_kvar8 b");
            //                    sql2.Append(" where nzp_serv=17 and a.nzp_kvar=b.nzp_kvar and isclose=0  and a.tarif>0 ");
            //                    sql2.Append(" and dat_charge is null  and isol=2");
            //                    sql2.Append(" group by 1,2,3");
            //                    sql2.Append(" into temp t2 with no log");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);


            //                    /*   sql2.Remove(0, sql2.Length);
            //                       sql2.Append(" select nzp_dom, (case when a.nzp_frm=0 then b.nzp_frm else a.nzp_frm end) as nzp_frm, ");
            //                       sql2.Append(" a.tarif, count(unique num_ls) as count_ls,");
            //                       sql2.Append(" sum(pl_calc) as pl_calc, sum(pl_calc*b.tarif) as rsum_tarif ");
            //                       sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ":charge_");
            //                       sql2.Append(prm.month_.ToString("00") + " a, sel_kvar8 b");
            //                       sql2.Append(" where nzp_serv=17 and a.nzp_kvar=b.nzp_kvar and isclose=1 ");
            //                       sql2.Append(" and dat_charge is null ");
            //                       sql2.Append(" group by 1,2,3");
            //                       sql2.Append(" into temp t3 with no log");
            //                       ExecSQL(conn_db, sql2.ToString(), true);*/

            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" select nzp_dom, (case when a.nzp_frm=0 then b.nzp_frm else a.nzp_frm end) as nzp_frm, ");
            //                    sql2.Append(" a.tarif, count(distinct num_ls) as count_ls,");
            //                    sql2.Append(" sum(pl_calc) as pl_calc, sum(Round(pl_calc*b.tarif,2)) as rsum_tarif ");
            //                    sql2.Append(" into unlogged t3  from " + pref + "_data.tarif a, sel_kvar8 b");
            //                    sql2.Append(" where nzp_serv=17 and a.nzp_kvar=b.nzp_kvar and isclose=1 ");
            //                    sql2.Append(" and is_actual=1 " + dataUsl + "  ");
            //                    sql2.Append(" group by 1,2,3");
            //                    sql2.Append(" ");
            //#else
            //                    sql2.Append(" select nzp_dom, (case when a.nzp_frm=0 then b.nzp_frm else a.nzp_frm end) as nzp_frm, ");
            //                    sql2.Append(" a.tarif, count(unique num_ls) as count_ls,");
            //                    sql2.Append(" sum(pl_calc) as pl_calc, sum(Round(pl_calc*b.tarif,2)) as rsum_tarif ");
            //                    sql2.Append(" from " + pref + "_data:tarif a, sel_kvar8 b");
            //                    sql2.Append(" where nzp_serv=17 and a.nzp_kvar=b.nzp_kvar and isclose=1 ");
            //                    sql2.Append(" and is_actual=1 " + dataUsl + "  ");
            //                    sql2.Append(" group by 1,2,3");
            //                    sql2.Append(" into temp t3 with no log");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);


            //                    /* sql2.Remove(0, sql2.Length);
            //                     sql2.Append(" select nzp_dom, (case when a.nzp_frm=0 then b.nzp_frm else a.nzp_frm end) as nzp_frm, ");
            //                     sql2.Append(" a.tarif, count(unique num_ls) as count_ls,");
            //                     sql2.Append(" sum(count_gil) as count_gil,");
            //                     sql2.Append(" sum(pl_calc) as pl_calc, sum(pl_kvar) as pl_kvar, sum(pl_gil) as pl_gil ");
            //                     sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ":charge_");
            //                     sql2.Append(prm.month_.ToString("00") + " a, sel_kvar8 b");
            //                     sql2.Append(" where nzp_serv=17 and a.nzp_kvar=b.nzp_kvar ");
            //                     sql2.Append(" and dat_charge is null ");
            //                     sql2.Append(" group by 1,2,3");
            //                     sql2.Append(" into temp t4 with no log");
            //                     ExecSQL(conn_db, sql2.ToString(), true);*/


            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" select nzp_dom, (case when a.nzp_frm=0 then b.nzp_frm else a.nzp_frm end) as nzp_frm, ");
            //                    sql2.Append(" a.tarif, count(distinct num_ls) as count_ls,");
            //                    sql2.Append(" sum(count_gil) as count_gil,");
            //                    sql2.Append(" sum(pl_calc) as pl_calc, sum(pl_kvar) as pl_kvar, sum(pl_gil) as pl_gil ");
            //                    sql2.Append("  into unlogged t4 from " + pref + "_data.tarif a, sel_kvar8 b");
            //                    sql2.Append(" where nzp_serv=17 and a.nzp_kvar=b.nzp_kvar  ");
            //                    sql2.Append(" and is_actual=1 " + dataUsl + "");
            //                    sql2.Append(" group by 1,2,3");
            //                    sql2.Append("");
            //#else
            //                    sql2.Append(" select nzp_dom, (case when a.nzp_frm=0 then b.nzp_frm else a.nzp_frm end) as nzp_frm, ");
            //                    sql2.Append(" a.tarif, count(unique num_ls) as count_ls,");
            //                    sql2.Append(" sum(count_gil) as count_gil,");
            //                    sql2.Append(" sum(pl_calc) as pl_calc, sum(pl_kvar) as pl_kvar, sum(pl_gil) as pl_gil ");
            //                    sql2.Append(" from " + pref + "_data:tarif a, sel_kvar8 b");
            //                    sql2.Append(" where nzp_serv=17 and a.nzp_kvar=b.nzp_kvar  ");
            //                    sql2.Append(" and is_actual=1 " + dataUsl + "");
            //                    sql2.Append(" group by 1,2,3");
            //                    sql2.Append(" into temp t4 with no log");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);


            //                    sql2.Remove(0, sql2.Length);
            //                    sql2.Append(" create index ixtmm_01 on t1(nzp_frm)");
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" analyze t1");
            //#else
            //                    sql2.Append(" update statistics for table t1");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);


            //                    sql2.Remove(0, sql2.Length);
            //                    sql2.Append(" create index ixtmm_02 on t2(nzp_frm)");
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" analyze t2");
            //#else
            //                    sql2.Append(" update statistics for table t2");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    sql2.Remove(0, sql2.Length);
            //                    sql2.Append(" create index ixtmm_03 on t3(nzp_frm)");
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" analyze t3");
            //#else
            //                    sql2.Append(" update statistics for table t3");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    //По всем
            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" insert into t_svod(tip_str,tip_doma, frm_name, count_ls, count_gil, ");
            //                    sql2.Append(" pl_calc, pl_kvar, pl_gil)");
            //                    sql2.Append(" select 0,name_y, name_frm, count_ls, count_gil, pl_calc, pl_kvar, pl_gil ");
            //                    sql2.Append(" from  " + pref + "_kernel.formuls f, t4 a  left outer join  ");
            //                    sql2.Append(pref + "_data.prm_2 p on  a.nzp_dom=p.nzp left outer join  " + pref + "_kernel.res_y y on    coalesce(p.val_prm::int,0)=y.nzp_y ");
            //                    sql2.Append(" where a.nzp_frm=f.nzp_frm");
            //                    sql2.Append(" and p.nzp_prm=2001 ");
            //                    sql2.Append(" ");
            //                    sql2.Append(" and is_actual=1 ");
            //                    sql2.Append(" and p.dat_s<=now() ");
            //                    sql2.Append(" and p.dat_po>=now() ");
            //                    sql2.Append(" ");
            //                    sql2.Append(" and nzp_res=3000 ");
            //#else
            //                    sql2.Append(" insert into t_svod(tip_str,tip_doma, frm_name, count_ls, count_gil, ");
            //                    sql2.Append(" pl_calc, pl_kvar, pl_gil)");
            //                    sql2.Append(" select 0,name_y, name_frm, count_ls, count_gil, pl_calc, pl_kvar, pl_gil ");
            //                    sql2.Append(" from t4 a, " + pref + "_kernel:formuls f, outer (");
            //                    sql2.Append(pref + "_data:prm_2 p, " + pref + "_kernel:res_y y)");
            //                    sql2.Append(" where a.nzp_frm=f.nzp_frm");
            //                    sql2.Append(" and p.nzp_prm=2001 ");
            //                    sql2.Append(" and a.nzp_dom=p.nzp");
            //                    sql2.Append(" and is_actual=1 ");
            //                    sql2.Append(" and p.dat_s<=today");
            //                    sql2.Append(" and p.dat_po>=today ");
            //                    sql2.Append(" and nvl(p.val_prm,0)=y.nzp_y");
            //                    sql2.Append(" and nzp_res=3000 ");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);


            //                    //Изолированные
            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" insert into t_svod(tip_str,tip_doma, frm_name, count_ls_i, count_gil_i, tarif_i,");
            //                    sql2.Append(" pl_calc_i, rsum_tarif_i)");
            //                    sql2.Append(" select 0,name_y, name_frm, count_ls, count_gil, a.tarif, pl_calc, rsum_tarif ");
            //                    sql2.Append(" from  " + pref + "_kernel.formuls f, t1 a left outer join ");
            //                    sql2.Append(pref + "_data.prm_2 p on  a.nzp_dom=p.nzp left outer join " + pref + "_kernel.res_y y ");
            //                    sql2.Append(" on  coalesce(p.val_prm::int,0)=y.nzp_y  where a.nzp_frm=f.nzp_frm");
            //                    sql2.Append(" and p.nzp_prm=2001 ");
            //                    sql2.Append("");
            //                    sql2.Append(" and is_actual=1 ");
            //                    sql2.Append(" and p.dat_s<=now()");
            //                    sql2.Append(" and p.dat_po>=now() ");
            //                    sql2.Append("");
            //                    sql2.Append(" and nzp_res=3000 ");
            //#else
            //                    sql2.Append(" insert into t_svod(tip_str,tip_doma, frm_name, count_ls_i, count_gil_i, tarif_i,");
            //                    sql2.Append(" pl_calc_i, rsum_tarif_i)");
            //                    sql2.Append(" select 0,name_y, name_frm, count_ls, count_gil, a.tarif, pl_calc, rsum_tarif ");
            //                    sql2.Append(" from t1 a, " + pref + "_kernel:formuls f, outer (");
            //                    sql2.Append(pref + "_data:prm_2 p, " + pref + "_kernel:res_y y)");
            //                    sql2.Append(" where a.nzp_frm=f.nzp_frm");
            //                    sql2.Append(" and p.nzp_prm=2001 ");
            //                    sql2.Append(" and a.nzp_dom=p.nzp");
            //                    sql2.Append(" and is_actual=1 ");
            //                    sql2.Append(" and p.dat_s<=today");
            //                    sql2.Append(" and p.dat_po>=today ");
            //                    sql2.Append(" and nvl(p.val_prm,0)=y.nzp_y");
            //                    sql2.Append(" and nzp_res=3000 ");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    //коммунальные
            //#if PG
            //                    sql2.Remove(0, sql2.Length);
            //                    sql2.Append(" insert into t_svod(tip_str,tip_doma, frm_name, count_ls_k, count_gil_k, tarif_k,");
            //                    sql2.Append(" pl_calc_k, rsum_tarif_k)");
            //                    sql2.Append(" select 0,name_y, name_frm, count_ls, count_gil, a.tarif, pl_calc, rsum_tarif ");
            //                    sql2.Append(" from " + pref + "_kernel.formuls f, t2 a left outer join  " + pref + "_data.prm_2 p on    a.nzp_dom=p.nzp left outer join ");
            //                    sql2.Append(pref + "_kernel.res_y y on COALESCE (P .val_prm::int , 0) = y.nzp_y ");
            //                    sql2.Append(" where a.nzp_frm=f.nzp_frm");
            //                    sql2.Append(" and p.nzp_prm=2001 ");
            //                    sql2.Append("  ");
            //                    sql2.Append(" and is_actual=1 ");
            //                    sql2.Append(" and p.dat_s<=now() ");
            //                    sql2.Append(" and p.dat_po>=now() ");
            //                    sql2.Append("  ");
            //                    sql2.Append(" and nzp_res=3000 ");
            //#else
            //                    sql2.Remove(0, sql2.Length);
            //                    sql2.Append(" insert into t_svod(tip_str,tip_doma, frm_name, count_ls_k, count_gil_k, tarif_k,");
            //                    sql2.Append(" pl_calc_k, rsum_tarif_k)");
            //                    sql2.Append(" select 0,name_y, name_frm, count_ls, count_gil, a.tarif, pl_calc, rsum_tarif ");
            //                    sql2.Append(" from t2 a, " + pref + "_kernel:formuls f, outer (" + pref + "_data:prm_2 p, ");
            //                    sql2.Append(pref + "_kernel:res_y y)");
            //                    sql2.Append(" where a.nzp_frm=f.nzp_frm");
            //                    sql2.Append(" and p.nzp_prm=2001 ");
            //                    sql2.Append(" and a.nzp_dom=p.nzp");
            //                    sql2.Append(" and is_actual=1 ");
            //                    sql2.Append(" and p.dat_s<=today");
            //                    sql2.Append(" and p.dat_po>=today ");
            //                    sql2.Append(" and nvl(p.val_prm,0)=y.nzp_y");
            //                    sql2.Append(" and nzp_res=3000 ");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);


            //                    //Закрытые ЛС
            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" insert into t_svod(tip_str,tip_doma, frm_name, count_close, tarif, ");
            //                    sql2.Append(" pl_close, pl_calc_close, rsum_tarif_close)");
            //                    sql2.Append(" select 0,name_y, name_frm, count_ls, a.tarif, pl_calc, pl_calc, rsum_tarif ");
            //                    sql2.Append(" from  " + pref + "_kernel.formuls f , t3 a left outer join " + pref + "_data.prm_2 p on  a.nzp_dom=p.nzp  left outer join  ");
            //                    sql2.Append(pref + "_kernel.res_y y on  coalesce(p.val_prm::int ,0)=y.nzp_y ");
            //                    sql2.Append(" where a.nzp_frm=f.nzp_frm");
            //                    sql2.Append(" and p.nzp_prm=2001 ");
            //                    sql2.Append(" ");
            //                    sql2.Append(" and is_actual=1 ");
            //                    sql2.Append(" and p.dat_s<=current_date ");
            //                    sql2.Append(" and p.dat_po>=current_date ");
            //                    sql2.Append(" ");
            //                    sql2.Append(" and nzp_res=3000 ");
            //#else
            //                    sql2.Append(" insert into t_svod(tip_str,tip_doma, frm_name, count_close, tarif, ");
            //                    sql2.Append(" pl_close, pl_calc_close, rsum_tarif_close)");
            //                    sql2.Append(" select 0,name_y, name_frm, count_ls, a.tarif, pl_calc, pl_calc, rsum_tarif ");
            //                    sql2.Append(" from t3 a, " + pref + "_kernel:formuls f, outer (" + pref + "_data:prm_2 p, ");
            //                    sql2.Append(pref + "_kernel:res_y y)");
            //                    sql2.Append(" where a.nzp_frm=f.nzp_frm");
            //                    sql2.Append(" and p.nzp_prm=2001 ");
            //                    sql2.Append(" and a.nzp_dom=p.nzp");
            //                    sql2.Append(" and is_actual=1 ");
            //                    sql2.Append(" and p.dat_s<=today");
            //                    sql2.Append(" and p.dat_po>=today ");
            //                    sql2.Append(" and nvl(p.val_prm,0)=y.nzp_y");
            //                    sql2.Append(" and nzp_res=3000 ");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    sql2.Remove(0, sql2.Length);
            //                    sql2.Append(" drop table t2");
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    sql2.Remove(0, sql2.Length);
            //                    sql2.Append(" drop table t1");
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    sql2.Remove(0, sql2.Length);
            //                    sql2.Append(" drop table t3");
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    sql2.Remove(0, sql2.Length);
            //                    sql2.Append(" drop table t4");
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    #region Заполнение таблицы Оборудование жилищного фонда по услугам
            //                    #region Старый вариант. Изменил Андрей Кайнов 18.07.2013
            //                    //sql2.Remove(0, sql2.Length);
            //                    //sql2.Append(" select a.nzp_kvar, nzp_serv ");
            //                    //sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ":charge_");
            //                    //sql2.Append(prm.month_.ToString("00") + " a, sel_kvar8 k");
            //                    //sql2.Append(" where a.nzp_kvar=k.nzp_kvar and a.nzp_serv>1 and dat_charge is null ");
            //                    //sql2.Append(" group by 1,2 into temp t_allserv with no log ");

            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" select a.nzp_kvar, nzp_serv, max(nzp_supp) as nzp_supp ");
            //                    sql2.Append(" into unlogged t_allserv  from " + pref + "_data.tarif a, sel_kvar8 k");
            //                    sql2.Append(" where a.nzp_kvar=k.nzp_kvar and a.nzp_serv not in (510,513,515,17,15) ");
            //                    sql2.Append(" and is_actual=1  " + dataUsl);
            //                    sql2.Append(" group by 1,2   ");
            //#else
            //                    sql2.Append(" select a.nzp_kvar, nzp_serv, max(nzp_supp) as nzp_supp ");
            //                    sql2.Append(" from " + pref + "_data:tarif a, sel_kvar8 k");
            //                    sql2.Append(" where a.nzp_kvar=k.nzp_kvar and a.nzp_serv not in (510,513,515,17,15) ");
            //                    sql2.Append(" and is_actual=1  " + dataUsl);
            //                    sql2.Append(" group by 1,2 into temp t_allserv with no log ");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    ExecSQL(conn_db, "drop table t9_serv", false);

            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" select nzp_kvar, nzp_supp ");
            //                    sql2.Append(" into unlogged t9_serv  from t_allserv ");
            //                    sql2.Append(" where nzp_serv=9  ");
            //#else
            //                    sql2.Append(" select nzp_kvar, nzp_supp ");
            //                    sql2.Append(" from t_allserv ");
            //                    sql2.Append(" where nzp_serv=9 into temp t9_serv with no log ");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //#if PG
            //                    ExecSQL(conn_db, "create index on t9_serv(nzp_kvar,nzp_supp)", false);
            //                    ExecSQL(conn_db, "update table statistics for table t9_serv", false);
            //#else
            //                    ExecSQL(conn_db, "create index on t9_serv(nzp_kvar,nzp_supp)", false);
            //                    ExecSQL(conn_db, "update table statistics for table t9_serv", false);
            //#endif

            //                    sql2.Remove(0, sql2.Length);
            //                    sql2.Append(" delete from t_allserv where nzp_serv=14 and 0<(select count(*) ");
            //                    sql2.Append(" from t9_serv a where a.nzp_kvar=t_allserv.nzp_kvar and a.nzp_supp=t_allserv.nzp_supp )");
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    ExecSQL(conn_db, "drop table t9_serv", true);

            //                    //Остальные услуги
            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" insert into t_svod(tip_str,tip_doma, frm_name, count_ls, count_gil,  pl_calc,");
            //                    sql2.Append(" pl_kvar, pl_gil) ");
            //                    sql2.Append(" select 1,'ИТОГО', service, count(distinct a.nzp_kvar), sum(count_gil), sum(pl_calc),");
            //                    sql2.Append(" sum(pl_kvar), sum(pl_gil)");
            //                    sql2.Append(" from t_allserv a, sel_kvar8 k, " + pref + "_kernel.services s");
            //                    sql2.Append(" where a.nzp_kvar=k.nzp_kvar ");
            //                    sql2.Append(" and a.nzp_serv=s.nzp_serv group by 1,2,3");
            //#else
            //                    sql2.Append(" insert into t_svod(tip_str,tip_doma, frm_name, count_ls, count_gil,  pl_calc,");
            //                    sql2.Append(" pl_kvar, pl_gil) ");
            //                    sql2.Append(" select 1,'ИТОГО', service, count(unique a.nzp_kvar), sum(count_gil), sum(pl_calc),");
            //                    sql2.Append(" sum(pl_kvar), sum(pl_gil)");
            //                    sql2.Append(" from t_allserv a, sel_kvar8 k, " + pref + "_kernel:services s");
            //                    sql2.Append(" where a.nzp_kvar=k.nzp_kvar ");
            //                    sql2.Append(" and a.nzp_serv=s.nzp_serv group by 1,2,3");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);
            //                    #endregion

            //                    //#region Новый вариант
            //                    //sql2.Remove(0, sql2.Length);
            //                    //sql2.Append(" select a.nzp_kvar, nzp_serv, max(squ-rsh2) as pl_calc ");
            //                    //sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ":calc_gku_" + prm.month_.ToString("00") + " a, sel_kvar8 k");
            //                    //sql2.Append(" where a.nzp_kvar = k.nzp_kvar and a.nzp_serv > 1 and dat_charge is null");
            //                    //sql2.Append(" group by 1,2 into temp t_allserv with no log ");
            //                    //ExecRead(conn_db, out reader2, sql2.ToString(), true);

            //                    //sql2.Remove(0, sql2.Length);
            //                    //sql2.Append(" insert into t_svod(tip_str,tip_doma, frm_name, count_ls, count_gil,  pl_calc, pl_kvar, pl_gil) ");
            //                    //sql2.Append(" select 1,'ИТОГО', service, count(unique a.nzp_kvar), sum(k.count_gil), sum(a.pl_calc), sum(k.pl_kvar), sum(k.pl_gil)");
            //                    //sql2.Append(" from t_allserv a, sel_kvar8 k, " + pref + "_kernel:services s");
            //                    //sql2.Append(" where a.nzp_kvar = k.nzp_kvar and a.nzp_serv = s.nzp_serv");
            //                    //sql2.Append(" group by 1,2,3");
            //                    //ExecRead(conn_db, out reader2, sql2.ToString(), true);
            //                    //#endregion

            //                    ExecSQL(conn_db, "drop table t_allserv", true);
            //                    #endregion

            //                    //Остальные услуги
            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" insert into t_svod(tip_str,tip_doma, count_ls, count_gil, count_gilp, ");
            //                    sql2.Append(" pl_kvar, pl_calc, count_close, pl_close, pl_calc_close)");
            //                    sql2.Append(" select 2,'ВСЕГО',  count(distinct nzp_kvar), sum(count_gil), sum(count_gilp), ");
            //                    sql2.Append(" sum(pl_kvar), sum(pl_calc),");
            //                    sql2.Append(" sum(isclose) as count_close,sum(case when isclose=1 then pl_kvar else 0 end) as pl_close,");
            //                    sql2.Append(" sum(case when isclose=1 then  pl_calc else 0 end) as pl_calc_close  ");
            //                    sql2.Append(" from sel_kvar8 k ");
            //                    sql2.Append(" group by 1,2 ");
            //#else
            //                    sql2.Append(" insert into t_svod(tip_str,tip_doma, count_ls, count_gil, count_gilp, ");
            //                    sql2.Append(" pl_kvar, pl_calc, count_close, pl_close, pl_calc_close)");
            //                    sql2.Append(" select 2,'ВСЕГО',  count(unique nzp_kvar), sum(count_gil), sum(count_gilp), ");
            //                    sql2.Append(" sum(pl_kvar), sum(pl_calc),");
            //                    sql2.Append(" sum(isclose) as count_close,sum(case when isclose=1 then pl_kvar else 0 end) as pl_close,");
            //                    sql2.Append(" sum(case when isclose=1 then  pl_calc else 0 end) as pl_calc_close  ");
            //                    sql2.Append(" from sel_kvar8 k ");
            //                    sql2.Append(" group by 1,2 ");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);


            //                    //Количество домов
            //                    ExecRead(conn_db, out reader2, "drop table t111", false);
            //                    //sql2.Remove(0, sql2.Length);
            //                    //sql2.Append(" select s.nzp_raj, d.nzp_dom,  d.nzp_dom as nzp_dom_base, ulica, ndom, nkor, sum(nvl(val_prm,0)+0) as pl_mop  ");
            //                    //sql2.Append(" from  " + pref + "_data:dom d, " + pref + "_data:s_ulica s, outer ");
            //                    //sql2.Append(pref + "_data:prm_2 p");
            //                    //sql2.Append(" where d.nzp_ul=s.nzp_ul ");
            //                    //sql2.Append(" and d.nzp_dom=p.nzp and nzp_prm=2049 and is_actual=1 ");
            //                    ////sql2.Append(" and p.dat_s<=today and p.dat_po>=today ");
            //                    //sql2.Append(" and d.nzp_dom in (select nzp_dom from sel_kvar8 group by 1) ");
            //                    //sql2.Append(" " + dataUsl);
            //                    //sql2.Append(" group by 1,2,3,4,5,6 into temp t111 with no log ");
            //                    //ExecSQL(conn_db, sql2.ToString(), true);


            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" select d.nzp_dom, max(coalesce(val_prm::numeric,0)) as pl_mop  ");
            //                    sql2.Append("  into unlogged t111  from  sel_kvar8 d left outer join " + pref + "_data.prm_2 p on d.nzp_dom=p.nzp ");
            //                    sql2.Append(" where  nzp_prm=2049 and is_actual=1 ");
            //                    sql2.Append(" " + dataUsl);
            //                    sql2.Append(" group by 1 ");
            //#else
            //                    sql2.Append(" select d.nzp_dom, max(nvl(val_prm,0)+0) as pl_mop  ");
            //                    sql2.Append(" from  sel_kvar8 d, outer " + pref + "_data:prm_2 p");
            //                    sql2.Append(" where d.nzp_dom=p.nzp and nzp_prm=2049 and is_actual=1 ");
            //                    sql2.Append(" " + dataUsl);
            //                    sql2.Append(" group by 1 into temp t111 with no log ");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    //ExecSQL(conn_db, "insert into faug_data:atmp_house(nzp_dom) select nzp_dom from t111", true);

            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" update t111 set nzp_dom=(select nzp_dom_base from " + pref + "_data.link_dom_lit t ");
            //                    sql2.Append(" where t111.nzp_dom=t.nzp_dom) ");
            //                    sql2.Append(" where nzp_dom in (select nzp_dom from " + pref + "_data.link_dom_lit )");
            //#else
            //                    sql2.Append(" update t111 set nzp_dom=(select nzp_dom_base from " + pref + "_data:link_dom_lit t ");
            //                    sql2.Append(" where t111.nzp_dom=t.nzp_dom) ");
            //                    sql2.Append(" where nzp_dom in (select nzp_dom from " + pref + "_data:link_dom_lit )");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);
            //                    //ExecSQL(conn_db, "insert into faug_data:atmp_house(nzp_dom_base) select nzp_dom from t111", true);


            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" update t_svod set count_dom = coalesce(count_dom,0) + coalesce((select count(distinct nzp_dom) ");
            //                    sql2.Append(" from t111 k),0)");
            //#else
            //                    sql2.Append(" update t_svod set count_dom = nvl(count_dom,0) + nvl((select count(unique nzp_dom) ");
            //                    sql2.Append(" from t111 k),0)");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" update t_svod set pl_mop = coalesce(pl_mop,0) + coalesce((select sum(pl_mop) ");
            //                    sql2.Append(" from t111 k),0)");
            //#else
            //                    sql2.Append(" update t_svod set pl_mop = nvl(pl_mop,0) + nvl((select sum(pl_mop) ");
            //                    sql2.Append(" from t111 k),0)");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" update t_svod set pl_ngil = coalesce(pl_ngil,0)+ coalesce((select sum(val_prm::numeric ) ");
            //                    sql2.Append(" from " + pref + "_data.prm_2 where nzp_prm=2051 and is_actual=1 ");
            //                    sql2.Append(dataUsl + " and nzp in (select nzp_dom from sel_kvar8  )),0) ");
            //#else
            //                    sql2.Append(" update t_svod set pl_ngil = nvl(pl_ngil,0)+ nvl((select sum(val_prm+0) ");
            //                    sql2.Append(" from " + pref + "_data:prm_2 where nzp_prm=2051 and is_actual=1 ");
            //                    sql2.Append(dataUsl + " and nzp in (select nzp_dom from sel_kvar8  )),0) ");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);

            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" update t_svod set pl_dom =coalesce(pl_dom,0)+ coalesce((select sum(val_prm::numeric) ");
            //                    sql2.Append(" from " + pref + "_data.prm_2 ");
            //                    sql2.Append(" where nzp_prm=40 and is_actual=1 ");
            //                    sql2.Append(dataUsl + " and nzp in (select nzp_dom from sel_kvar8 )),0) ");
            //#else
            //                    sql2.Append(" update t_svod set pl_dom =nvl(pl_dom,0)+ nvl((select sum(val_prm+0) ");
            //                    sql2.Append(" from " + pref + "_data:prm_2 ");
            //                    sql2.Append(" where nzp_prm=40 and is_actual=1 ");
            //                    sql2.Append(dataUsl + " and nzp in (select nzp_dom from sel_kvar8 )),0) ");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);



            //                    ExecSQL(conn_db, "drop table t111", true);



            //                    //Площадь квартир  на начало года
            //                    sql2.Remove(0, sql2.Length);
            //#if PG
            //                    sql2.Append(" update t_svod set pl_kvar_year = coalesce(pl_kvar_year,0) + coalesce((select sum(pl_kvar_year) ");
            //                    sql2.Append(" from sel_kvar8 k),0)");
            //#else
            //                    sql2.Append(" update t_svod set pl_kvar_year = nvl(pl_kvar_year,0) + nvl((select sum(pl_kvar_year) ");
            //                    sql2.Append(" from sel_kvar8 k),0)");
            //#endif
            //                    ExecSQL(conn_db, sql2.ToString(), true);


            //                    ExecSQL(conn_db, "drop table sel_kvar8", true);

            //                }
            //            }
            //            reader.Close();
            //            ExecSQL(conn_db, "drop table t_sodergkv", true);

            //            sql2.Remove(0, sql2.Length);
            //#if PG
            //            sql2.Append(" select tip_str,");
            //            sql2.Append(" tip_doma,");
            //            sql2.Append(" frm_name, ");
            //            sql2.Append(" max(coalesce(count_dom,0)) as count_dom,");
            //            sql2.Append(" max(coalesce(pl_mop,0)) as pl_mop,");
            //            sql2.Append(" max(coalesce(pl_ngil,0)) as pl_ngil,");
            //            sql2.Append(" max(coalesce(pl_dom,0)) as pl_dom,");
            //            sql2.Append(" sum(coalesce(count_gilp,0)) as count_gilp,");
            //            sql2.Append(" sum(coalesce(pl_kvar_year,0)) as pl_kvar_year,");
            //            sql2.Append(" sum(coalesce(pl_kvar,0)) as pl_kvar,");
            //            sql2.Append(" sum(coalesce(pl_gil,0)) as pl_gil,");
            //            sql2.Append(" sum(coalesce(pl_calc,0)) as pl_calc,");
            //            sql2.Append(" sum(coalesce(count_ls,0)) as count_ls,");
            //            sql2.Append(" sum(coalesce(count_gil,0)) as  count_gil,");
            //            sql2.Append(" sum(coalesce(rsum_tarif,0)) as rsum_tarif,");
            //            sql2.Append(" sum(coalesce(count_close,0)) as count_close,");
            //            sql2.Append(" sum(coalesce(pl_close,0)) as pl_close ,");
            //            sql2.Append(" sum(coalesce(pl_calc_close,0)) as pl_calc_close ,");
            //            sql2.Append(" sum(coalesce(count_ls_i,0)) as count_ls_i,");
            //            sql2.Append(" sum(coalesce(count_ls_k,0)) as count_ls_k,");
            //            sql2.Append(" sum(coalesce(count_gil_i,0)) as count_gil_i, ");
            //            sql2.Append(" sum(coalesce(count_gil_k,0)) as count_gil_k, ");
            //            sql2.Append(" max(coalesce(tarif,0)) as tarif , ");
            //            sql2.Append(" max(coalesce(tarif_i,0)) as tarif_i, ");
            //            sql2.Append(" max(coalesce(tarif_k,0)) as tarif_k,");
            //            sql2.Append(" sum(coalesce(pl_calc_i,0)) as pl_calc_i, ");
            //            sql2.Append(" sum(coalesce(pl_calc_k,0)) as pl_calc_k,");
            //            sql2.Append(" sum(coalesce(rsum_tarif_i,0)) as rsum_tarif_i, ");
            //            sql2.Append(" sum(coalesce(rsum_tarif_k,0))  as rsum_tarif_k, ");
            //            sql2.Append(" sum(coalesce(rsum_tarif_close,0))  as rsum_tarif_close");
            //            sql2.Append(" from t_svod a ");
            //            sql2.Append(" group by  tip_str, tip_doma, frm_name  ");
            //            sql2.Append(" order by tip_str, tip_doma, frm_name  ");
            //#else
            //            sql2.Append(" select tip_str,");
            //            sql2.Append(" tip_doma,");
            //            sql2.Append(" frm_name, ");
            //            sql2.Append(" max(nvl(count_dom,0)) as count_dom,");
            //            sql2.Append(" max(nvl(pl_mop,0)) as pl_mop,");
            //            sql2.Append(" max(nvl(pl_ngil,0)) as pl_ngil,");
            //            sql2.Append(" max(nvl(pl_dom,0)) as pl_dom,");
            //            sql2.Append(" sum(nvl(count_gilp,0)) as count_gilp,");
            //            sql2.Append(" sum(nvl(pl_kvar_year,0)) as pl_kvar_year,");
            //            sql2.Append(" sum(nvl(pl_kvar,0)) as pl_kvar,");
            //            sql2.Append(" sum(nvl(pl_gil,0)) as pl_gil,");
            //            sql2.Append(" sum(nvl(pl_calc,0)) as pl_calc,");
            //            sql2.Append(" sum(nvl(count_ls,0)) as count_ls,");
            //            sql2.Append(" sum(nvl(count_gil,0)) as  count_gil,");
            //            sql2.Append(" sum(nvl(rsum_tarif,0)) as rsum_tarif,");
            //            sql2.Append(" sum(nvl(count_close,0)) as count_close,");
            //            sql2.Append(" sum(nvl(pl_close,0)) as pl_close ,");
            //            sql2.Append(" sum(nvl(pl_calc_close,0)) as pl_calc_close ,");
            //            sql2.Append(" sum(nvl(count_ls_i,0)) as count_ls_i,");
            //            sql2.Append(" sum(nvl(count_ls_k,0)) as count_ls_k,");
            //            sql2.Append(" sum(nvl(count_gil_i,0)) as count_gil_i, ");
            //            sql2.Append(" sum(nvl(count_gil_k,0)) as count_gil_k, ");
            //            sql2.Append(" max(nvl(tarif,0)) as tarif , ");
            //            sql2.Append(" max(nvl(tarif_i,0)) as tarif_i, ");
            //            sql2.Append(" max(nvl(tarif_k,0)) as tarif_k,");
            //            sql2.Append(" sum(nvl(pl_calc_i,0)) as pl_calc_i, ");
            //            sql2.Append(" sum(nvl(pl_calc_k,0)) as pl_calc_k,");
            //            sql2.Append(" sum(nvl(rsum_tarif_i,0)) as rsum_tarif_i, ");
            //            sql2.Append(" sum(nvl(rsum_tarif_k,0))  as rsum_tarif_k, ");
            //            sql2.Append(" sum(nvl(rsum_tarif_close,0))  as rsum_tarif_close");
            //            sql2.Append(" from t_svod a ");
            //            sql2.Append(" group by  tip_str, tip_doma, frm_name  ");
            //            sql2.Append(" order by tip_str, tip_doma, frm_name  ");
            //#endif
            //            if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
            //            {
            //                MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
            //                conn_db.Close();
            //                ret.result = false;
            //                return null;
            //            }


            //            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
            //            culture.NumberFormat.NumberDecimalSeparator = ".";
            //            culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            //            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            //            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            //            if (reader2 != null)
            //            {
            //                try
            //                {

            //                    LocalTable.Load(reader2, LoadOption.OverwriteChanges);
            //                }
            //                catch (Exception ex)
            //                {
            //                    MonitorLog.WriteLog("!!!" + ex.Message, MonitorLog.typelog.Error, true);
            //                    conn_web.Close();
            //                    conn_db.Close();
            //                    reader.Close();
            //                    return null;
            //                }
            //            }
            //            if (reader2 != null) reader2.Close();
            //            reader.Close();

            //            ExecSQL(conn_db, "drop table t_svod", true);
            //            conn_db.Close();





            //            #endregion

            //            return LocalTable;

        }

        public DataTable GetGilFondStatZhig(Prm prm, out Returns ret, string Nzp_user)
        {
            ret = Utils.InitReturns();

            #region Подключение к БД
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);
            IDataReader reader;
            IDataReader reader2;

            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }


            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с локальной БД ", MonitorLog.typelog.Error, true);
                conn_web.Close();
                ret.result = false;
                return null;
            }

            #endregion

            StringBuilder sql = new StringBuilder();
            StringBuilder sql2 = new StringBuilder();

            string tXX_spls = DBManager.GetFullBaseName(conn_web) + DBManager.tableDelimiter + "t" + Nzp_user + "_spls";
            conn_web.Close();
            string pref = "";

            string dataUsl = " and dat_s<='01." + prm.month_ + "." + prm.year_ + "' " +
                             " and dat_po>='01." + prm.month_ + "." + prm.year_ + "' ";


            DataTable LocalTable = new DataTable();

            ExecSQL(conn_db, "drop table t_svod", false);

            sql2.Remove(0, sql2.Length);
            sql2.Append(" create temp table t_svod( ");
            sql2.Append(" tip_str integer,");            // 0 обычное 1 по услугам 2 итого
            sql2.Append(" tip_doma char(100),");
            sql2.Append(" frm_name char(100),");
            sql2.Append(" count_gilp integer,");
            sql2.Append(" count_dom integer,");
            sql2.Append(" pl_dom Decimal(14,2),");
            sql2.Append(" pl_mop Decimal(14,2),");
            sql2.Append(" pl_ngil Decimal(14,2),");
            sql2.Append(" pl_kvar_year Decimal(14,2),");
            sql2.Append(" pl_kvar Decimal(14,2),");
            sql2.Append(" pl_gil Decimal(14,2),");
            sql2.Append(" pl_calc Decimal(14,2),");
            sql2.Append(" count_ls integer,");
            sql2.Append(" count_gil integer,");
            sql2.Append(" rsum_tarif Decimal(14,2),");
            sql2.Append(" count_close integer,");
            sql2.Append(" pl_close Decimal(14,2),");
            sql2.Append(" pl_calc_close Decimal(14,2),");
            sql2.Append(" count_ls_i integer,");
            sql2.Append(" count_ls_k integer,");
            sql2.Append(" count_gil_i integer, ");
            sql2.Append(" count_gil_k integer, ");
            sql2.Append(" tarif Decimal(14,2), ");
            sql2.Append(" tarif_i Decimal(14,2), ");
            sql2.Append(" tarif_k Decimal(14,2),");
            sql2.Append(" pl_calc_i Decimal(14,2), ");
            sql2.Append(" pl_calc_k Decimal(14,2),");
            sql2.Append(" rsum_tarif_close Decimal(14,2), ");
            sql2.Append(" rsum_tarif_i Decimal(14,2), ");
            sql2.Append(" rsum_tarif_k Decimal(14,2)) with no log");

            ExecSQL(conn_db, sql2.ToString(), true);


            ExecSQL(conn_db, "drop table t_sodergkv", false);

            string sql3 =
                "create temp table t_sodergkv(nzp_dom integer, nzp_kvar integer, nzp_area integer, pref char(10))" + DBManager.sUnlogTempTable;
            ExecSQL(conn_db, sql3, true);

            sql3 = "insert into t_sodergkv(nzp_dom, nzp_kvar, nzp_area, pref)" +
                   " select nzp_dom, nzp_kvar, nzp_area, pref " +
                   " from  " + tXX_spls;
            ExecSQL(conn_db, sql3, true);




            sql3 = " select pref " +
                   " from  t_sodergkv group by 1 ";
            ExecRead(conn_db, out reader, sql3, true);

            while (reader.Read())
            {
                if (reader["pref"] != null)
                {
                    pref = Convert.ToString(reader["pref"]).Trim();
                    string baseData = pref + DBManager.sDataAliasRest;
                    string dbCharge = pref + "_charge_" + (prm.year_ - 2000).ToString("00");


                    ExecRead(conn_db, out reader2, "drop table sel_kvar8", false);
                    sql2.Remove(0, sql2.Length);

                    sql2.Append(" create temp table sel_kvar8( ");
                    sql2.Append(" nzp_kvar integer,");
                    sql2.Append(" nzp_dom integer,");
                    sql2.Append(" nzp_frm integer,");
                    sql2.Append(" isol integer default 1,");
                    sql2.Append(" isclose integer default 0,");
                    sql2.Append(" count_gil integer,");
                    sql2.Append(" count_gilp integer,");
                    sql2.Append(" pl_kvar Decimal(14,2),");
                    sql2.Append(" pl_kvar_year Decimal(14,2),");
                    sql2.Append(" tarif Decimal(14,2),");
                    sql2.Append(" pl_gil Decimal(14,2),");
                    sql2.Append(" pl_calc Decimal(14,2))" + DBManager.sUnlogTempTable);

                    ExecSQL(conn_db, sql2.ToString(), true);


                    sql2.Remove(0, sql2.Length);

                    sql2.Append(" insert into sel_kvar8(nzp_dom, nzp_kvar, isol, isclose)");
                    sql2.Append(" select k.nzp_dom, k.nzp_kvar, 1 as isol,0 ");
                    sql2.Append(" from t_sodergkv k, " + baseData + "dom d ");
                    sql2.Append(" where k.nzp_dom=d.nzp_dom and k.nzp_area=d.nzp_area and ");
                    sql2.Append(" 0 = (select count(*) from " + baseData + "prm_2 p where nzp_prm=2029 ");
                    sql2.Append(" and is_actual = 1 " + dataUsl + " and date(val_prm)<=date('01." + prm.month_ + "." + prm.year_ + "') and k.nzp_dom=nzp )");
                    sql2.Append(" and k.pref ='" + pref + "'");

                    ExecSQL(conn_db, sql2.ToString(), true);





                    ExecSQL(conn_db, "create index ix_tmpk_01 on sel_kvar8(nzp_kvar)", true);
                    ExecSQL(conn_db, DBManager.sUpdStat + " sel_kvar8 ", true);

                    //Изолированные /коммунальные
                    sql2.Remove(0, sql2.Length);

                    sql2.Append(" update sel_kvar8 set isol=2");
                    sql2.Append(" where nzp_kvar in (select nzp from ");
                    sql2.Append(baseData + "prm_1 where nzp_prm=3 and val_prm='2' ");
                    sql2.Append(" and is_actual=1 " + dataUsl + ")");

                    ExecSQL(conn_db, sql2.ToString(), true);

                    //Открытые /закрытые
                    sql2.Remove(0, sql2.Length);

                    sql2.Append(" update sel_kvar8 set isclose=1 ");
                    sql2.Append(" where nzp_kvar in (select nzp from ");
                    sql2.Append(baseData + "prm_3 where nzp_prm=51 and val_prm='2' ");
                    sql2.Append(" and is_actual=1 " + dataUsl + ")");
                    ExecSQL(conn_db, sql2.ToString(), true);


                    sql2.Remove(0, sql2.Length);

                    sql2.Append(" update sel_kvar8 set count_gil = (select max(round(gil)) ");
                    sql2.Append(" from  " + dbCharge + DBManager.tableDelimiter + "calc_gku_" + prm.month_.ToString("00") + " d ");
                    sql2.Append(" where d.nzp_kvar=sel_kvar8.nzp_kvar and d.tarif>0  ");
                    sql2.Append("  )");
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update sel_kvar8 set pl_calc = (select max(squ) ");
                    sql2.Append(" from  " + dbCharge + DBManager.tableDelimiter + "calc_gku_" + prm.month_.ToString("00") + " d ");
                    sql2.Append(" where d.nzp_kvar=sel_kvar8.nzp_kvar and d.tarif>0  ");
                    sql2.Append("  )");
                    ExecSQL(conn_db, sql2.ToString(), true);


                    //Площадь квартир
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update sel_kvar8 set pl_kvar=(select max(val_prm" + DBManager.sConvToNum + ")");
                    sql2.Append(" from " + baseData + "prm_1 a where nzp_prm=4 and sel_kvar8.nzp_kvar=a.nzp ");
                    sql2.Append(" and is_actual=1 " + dataUsl + ")");
                    ExecSQL(conn_db, sql2.ToString(), true);

                    //Площадь квартир на начало года
                    sql2.Remove(0, sql2.Length);

                    sql2.Append(" update sel_kvar8 set pl_kvar_year=(select max(val_prm" + DBManager.sConvToNum + ")");
                    sql2.Append(" from " + baseData + "prm_1 a where nzp_prm=4 and sel_kvar8.nzp_kvar=a.nzp ");
                    sql2.Append(" and is_actual=1 and dat_s<=MDY(01,01," + prm.year_ + ")");
                    sql2.Append(" and dat_po>=MDY(01,01," + prm.year_ + "))");
                    ExecSQL(conn_db, sql2.ToString(), true);

                    //Жилая площадь
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update sel_kvar8 set pl_gil=(select max(val_prm" + DBManager.sConvToNum + ")");
                    sql2.Append(" from " + baseData + "prm_1 a where nzp_prm=6 and sel_kvar8.nzp_kvar=a.nzp ");
                    sql2.Append(" and is_actual=1 " + dataUsl + ")");
                    ExecSQL(conn_db, sql2.ToString(), true);

                    //Количество жильцов


                    sql2.Remove(0, sql2.Length);

                    sql2.Append(" update sel_kvar8 set count_gilp=(select max(val_prm" + DBManager.sConvToNum + ")");
                    sql2.Append(" from " + baseData + "prm_1 a where nzp_prm=2005 and sel_kvar8.nzp_kvar=a.nzp ");
                    sql2.Append(" and is_actual=1 " + dataUsl + ")");
                    ExecSQL(conn_db, sql2.ToString(), true);




                    #region Заполнение таблицы Оборудование жилищного фонда по услугам
                    #region Старый вариант. Изменил Андрей Кайнов 18.07.2013

                    sql2.Remove(0, sql2.Length);

                    //sql2.Append(" select a.nzp_kvar, nzp_serv, max(nzp_supp) as nzp_supp ");
                    //sql2.Append(" from " + baseData + "tarif a, sel_kvar8 k");
                    //sql2.Append(" where a.nzp_kvar=k.nzp_kvar and a.nzp_serv not in (510,513,515,17,15) ");
                    //sql2.Append(" and is_actual=1  " + dataUsl);
                    //sql2.Append(" group by 1,2 into temp t_allserv with no log ");
                    //ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Append(" select a.nzp_kvar, nzp_serv, max(nzp_supp) as nzp_supp ");
                    sql2.Append(" from " + dbCharge + DBManager.tableDelimiter+"calc_gku_" + prm.month_.ToString("00") + " a, sel_kvar8 k");
                    sql2.Append(" where a.nzp_kvar=k.nzp_kvar and a.nzp_serv not in (510,513,515,17,15) ");
                    sql2.Append(" and a.tarif>0 ");
                    sql2.Append(" group by 1,2 into temp t_allserv with no log ");
                    ExecSQL(conn_db, sql2.ToString(), true);


                    //Остальные услуги
                    sql2.Remove(0, sql2.Length);

                    sql2.Append(" insert into t_svod(tip_str,tip_doma, frm_name, count_ls, count_gil,  pl_calc,");
                    sql2.Append(" pl_kvar, pl_gil) ");
                    sql2.Append(" select 1,'ИТОГО', service, count(distinct a.nzp_kvar), sum(count_gil), sum(pl_calc),");
                    sql2.Append(" sum(pl_kvar), sum(pl_gil)");
                    sql2.Append(" from t_allserv a, sel_kvar8 k, " + pref + DBManager.sKernelAliasRest + "services s");
                    sql2.Append(" where a.nzp_kvar=k.nzp_kvar ");
                    sql2.Append(" and a.nzp_serv=s.nzp_serv group by 1,2,3");

                    ExecSQL(conn_db, sql2.ToString(), true);
                    #endregion


                    ExecSQL(conn_db, "drop table t_allserv", true);
                    #endregion

                    //Остальные услуги
                    sql2.Remove(0, sql2.Length);

                    sql2.Append(" insert into t_svod(tip_str,tip_doma, count_ls, count_gil, count_gilp, ");
                    sql2.Append(" pl_kvar, pl_calc, count_close, pl_close, pl_calc_close)");
                    sql2.Append(" select 2,'ВСЕГО',  count(distinct nzp_kvar), sum(count_gil), sum(count_gilp), ");
                    sql2.Append(" sum(pl_kvar), sum(pl_calc),");
                    sql2.Append(" sum(isclose) as count_close,sum(case when isclose=1 then pl_kvar else 0 end) as pl_close,");
                    sql2.Append(" sum(case when isclose=1 then  pl_calc else 0 end) as pl_calc_close  ");
                    sql2.Append(" from sel_kvar8 k ");
                    sql2.Append(" group by 1,2 ");
                    ExecSQL(conn_db, sql2.ToString(), true);


                    //Количество домов
                    ExecRead(conn_db, out reader2, "drop table t111", false);


                    sql3 = "create temp table t111(nzp_dom integer, pl_mop decimal(14,2))";
                    ExecSQL(conn_db, sql3, true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append("insert into t111(nzp_dom, pl_mop)  ");
                    sql2.Append(" select d.nzp_dom, max(nvl(val_prm,0)+0) as pl_mop  ");
                    sql2.Append(" from  sel_kvar8 d, outer " + baseData + "prm_2 p");
                    sql2.Append(" where d.nzp_dom=p.nzp and nzp_prm=2049 and is_actual=1 ");
                    sql2.Append(" " + dataUsl);
                    sql2.Append(" group by 1 ");

                    ExecSQL(conn_db, sql2.ToString(), true);

                    //ExecSQL(conn_db, "insert into faug_data:atmp_house(nzp_dom) select nzp_dom from t111", true);

                    sql2.Remove(0, sql2.Length);

                    sql2.Append(" update t111 set nzp_dom=(select nzp_dom_base from " + baseData + "link_dom_lit t ");
                    sql2.Append(" where t111.nzp_dom=t.nzp_dom) ");
                    sql2.Append(" where nzp_dom in (select nzp_dom from " + baseData + "link_dom_lit )");

                    ExecSQL(conn_db, sql2.ToString(), true);
                    //ExecSQL(conn_db, "insert into faug_data:atmp_house(nzp_dom_base) select nzp_dom from t111", true);



                    sql2.Remove(0, sql2.Length);

                    sql2.Append(" update t_svod set count_dom = " + DBManager.sNvlWord + "(count_dom,0) + " +
                        DBManager.sNvlWord + "((select count(distinct nzp_dom) from t111 k),0)");
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);

                    sql2.Append(" update t_svod set pl_mop = " + DBManager.sNvlWord + "(pl_mop,0) + " + DBManager.sNvlWord + "((select sum(pl_mop) ");
                    sql2.Append(" from t111 k),0)");

                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);

                    sql2.Append(" update t_svod set pl_ngil = " + DBManager.sNvlWord + "(pl_ngil,0)+ " +
                        DBManager.sNvlWord + "((select sum(val_prm" + DBManager.sConvToNum + ") ");
                    sql2.Append(" from " + baseData + "prm_2 where nzp_prm=2051 and is_actual=1 ");
                    sql2.Append(dataUsl + " and nzp in (select nzp_dom from sel_kvar8  )),0) ");
                    ExecSQL(conn_db, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);


                    sql2.Append(" update t_svod set pl_dom =" + DBManager.sNvlWord + "(pl_dom,0)+ " +
                        DBManager.sNvlWord + "((select sum(val_prm" + DBManager.sConvToNum + ") ");
                    sql2.Append(" from " + baseData + "prm_2 ");
                    sql2.Append(" where nzp_prm=40 and is_actual=1 ");
                    sql2.Append(dataUsl + " and nzp in (select nzp_dom from sel_kvar8 )),0) ");
                    ExecSQL(conn_db, sql2.ToString(), true);



                    ExecSQL(conn_db, "drop table t111", true);



                    //Площадь квартир  на начало года
                    sql2.Remove(0, sql2.Length);

                    sql2.Append(" update t_svod set pl_kvar_year = " + DBManager.sNvlWord + "(pl_kvar_year,0) + " +
                        DBManager.sNvlWord + "((select sum(pl_kvar_year) ");
                    sql2.Append(" from sel_kvar8 k),0)");

                    ExecSQL(conn_db, sql2.ToString(), true);


                    ExecSQL(conn_db, "drop table sel_kvar8", true);

                }
            }
            reader.Close();
            ExecSQL(conn_db, "drop table t_sodergkv", true);


            sql3 = " select tip_str," +
                   " tip_doma," +
                   " frm_name, " +
                   " max(nvl(count_dom,0)) as count_dom," +
                   " max(nvl(pl_mop,0)) as pl_mop," +
                   " max(nvl(pl_ngil,0)) as pl_ngil," +
                   " max(nvl(pl_dom,0)) as pl_dom," +
                   " sum(nvl(count_gilp,0)) as count_gilp," +
                   " sum(nvl(pl_kvar_year,0)) as pl_kvar_year," +
                   " sum(nvl(pl_kvar,0)) as pl_kvar," +
                   " sum(nvl(pl_gil,0)) as pl_gil," +
                   " sum(nvl(pl_calc,0)) as pl_calc," +
                   " sum(nvl(count_ls,0)) as count_ls," +
                   " sum(nvl(count_gil,0)) as  count_gil," +
                   " sum(nvl(rsum_tarif,0)) as rsum_tarif," +
                   " sum(nvl(count_close,0)) as count_close," +
                   " sum(nvl(pl_close,0)) as pl_close ," +
                   " sum(nvl(pl_calc_close,0)) as pl_calc_close " +
                   " from t_svod a " +
                   " group by  tip_str, tip_doma, frm_name  " +
                   " order by tip_str, tip_doma, frm_name  ";

            LocalTable = DBManager.ExecSQLToTable(conn_db, sql3);


            ExecSQL(conn_db, "drop table t_svod", true);
            conn_db.Close();






            return LocalTable;

        }

        public DataTable GetKvarStat(Prm prm, out Returns ret, string Nzp_user)
        {
            //MonitorLog.WriteLog("ExcelReport start ifmx", MonitorLog.typelog.Info, true);
            ret = Utils.InitReturns();

            #region Подключение к БД
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);
            IDataReader reader;
            IDataReader reader2;

            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }


            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);//new IDbConnection(Constants.cons_Webdata);

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с локальной БД ", MonitorLog.typelog.Error, true);
                conn_web.Close();
                ret.result = false;
                return null;
            }

            #endregion

            StringBuilder sql = new StringBuilder();
            StringBuilder sql2 = new StringBuilder();
#if PG
            string tXX_spls = "public." + "t" + Nzp_user + "_spls";
#else
            string tXX_spls = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "t" + Nzp_user + "_spls";
#endif
            string pref = "";

            string dataUsl = " and dat_s<='01." + prm.month_ + "." + prm.year_ + "' " +
                          " and dat_po>='01." + prm.month_ + "." + prm.year_ + "' ";
            #region Выборка по локальным банкам
            DataTable LocalTable = new DataTable();

            ExecRead(conn_db, out reader2, "drop table t_svod", false);

            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" create unlogged table t_svod( ");
            sql2.Append(" count_room integer,");
            sql2.Append(" count_kv integer,");
            sql2.Append(" count_izol integer,");
            sql2.Append(" count_komm integer,");
            sql2.Append(" count_priv integer,");
            sql2.Append(" count_2priv integer,");
            sql2.Append(" count_work integer,");
            sql2.Append(" count_sobstv integer,");
            sql2.Append(" count_jur integer,");
            sql2.Append(" count_npriv integer,");
            sql2.Append(" pl_kvar numeric(14,2),");
            sql2.Append(" pl_gil numeric(14,2),");
            sql2.Append(" pl_calc numeric(14,2),");
            sql2.Append(" count_gil integer) ");
#else
            sql2.Append(" create temp table t_svod( ");
            sql2.Append(" count_room integer,");
            sql2.Append(" count_kv integer,");
            sql2.Append(" count_izol integer,");
            sql2.Append(" count_komm integer,");
            sql2.Append(" count_priv integer,");
            sql2.Append(" count_2priv integer,");
            sql2.Append(" count_work integer,");
            sql2.Append(" count_sobstv integer,");
            sql2.Append(" count_jur integer,");
            sql2.Append(" count_npriv integer,");
            sql2.Append(" pl_kvar Decimal(14,2),");
            sql2.Append(" pl_gil Decimal(14,2),");
            sql2.Append(" pl_calc Decimal(14,2),");
            sql2.Append(" count_gil integer) with no log");
#endif
            if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_web.Close();
                conn_db.Close();
                ret.result = false;
                return null;
            }

            sql.Remove(0, sql.Length);
            sql.Append(" select pref ");
            sql.Append(" from  " + tXX_spls + " group by 1");

            if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_web.Close();
                sql.Remove(0, sql.Length);
                ret.result = false;
                return null;
            }
            while (reader.Read())
            {
                if (reader["pref"] != null)
                {
                    pref = Convert.ToString(reader["pref"]).Trim();
                    string baseData = pref + DBManager.sDataAliasRest;


                    #region Заполнение sel_kvar9
                    ExecRead(conn_db, out reader2, "drop table sel_kvar9", false);
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" create unlogged table sel_kvar9( ");
                    sql2.Append(" nzp_kvar integer,");
                    sql2.Append(" kv char(20),");
                    sql2.Append(" count_room integer,");
                    sql2.Append(" isol integer,");
                    sql2.Append(" type_kv integer,");
                    sql2.Append(" pl_kvar numeric(14,2),");
                    sql2.Append(" pl_gil numeric(14,2),");
                    sql2.Append(" pl_calc numeric(14,2),");
                    sql2.Append(" count_gil integer) ");
#else
                    sql2.Append(" create temp table sel_kvar9( ");
                    sql2.Append(" nzp_kvar integer,");
                    sql2.Append(" kv char(20),");
                    sql2.Append(" count_room integer,");
                    sql2.Append(" isol integer,");
                    sql2.Append(" type_kv integer,");
                    sql2.Append(" pl_kvar Decimal(14,2),");
                    sql2.Append(" pl_gil Decimal(14,2),");
                    sql2.Append(" pl_calc Decimal(14,2),");
                    sql2.Append(" count_gil integer) with no log");
#endif
                    if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        conn_web.Close();
                        conn_db.Close();
                        ret.result = false;
                        return null;
                    }


                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into sel_kvar9( nzp_kvar, isol, kv)");
                    sql2.Append(" select  nzp_kvar, 1 as isol, trim(nzp_dom||' '||nkvar) ");
                    sql2.Append(" from " + tXX_spls + " k ");
                    sql2.Append(" where pref='" + pref + "'");
                    sql2.Append(" and sostls='открыт'");
                    sql2.Append(" and 0 = (select count(*) from " + baseData + "prm_2 p where nzp_prm=2029 ");
                    sql2.Append(" and is_actual = 1 " + dataUsl + " and date(val_prm)<=date('01." + prm.month_ + "." + prm.year_ + "') and k.nzp_dom=nzp )");


                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append("create index ix_tmpk_01 on sel_kvar9(nzp_kvar)");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append("analyze sel_kvar9 ");
#else
                    sql2.Append("update statistics for table sel_kvar9 ");
#endif
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    //Изолированные /коммунальные
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update sel_kvar9 set isol=2");
                    sql2.Append(" where nzp_kvar in (select nzp from ");
                    sql2.Append(pref + "_data.prm_1 where nzp_prm=3 and val_prm='2' ");
                    sql2.Append(" and is_actual=1 and dat_s<=current_date and dat_po>=current_date)");
#else
                    sql2.Append(" update sel_kvar9 set isol=2");
                    sql2.Append(" where nzp_kvar in (select nzp from ");
                    sql2.Append(pref + "_data:prm_1 where nzp_prm=3 and val_prm='2' ");
                    sql2.Append(" and is_actual=1 " + dataUsl + ")");
#endif
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update sel_kvar9 set count_room=(select max(val_prm::numeric)");
                    sql2.Append(" from " + pref + "_data.prm_1 a where nzp_prm=107 and sel_kvar9.nzp_kvar=a.nzp ");
                    sql2.Append(" and is_actual=1 and dat_s<=current_date and dat_po>=current_date)");
#else
                    sql2.Append(" update sel_kvar9 set count_room=(select max(val_prm+0)");
                    sql2.Append(" from " + pref + "_data:prm_1 a where nzp_prm=107 and sel_kvar9.nzp_kvar=a.nzp ");
                    sql2.Append(" and is_actual=1 " + dataUsl + ")");
#endif
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    //Площадь квартир
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update sel_kvar9 set pl_kvar=(select max(Replace(val_prm,',','.')::numeric)");
                    sql2.Append(" from " + pref + "_data.prm_1 a where nzp_prm=4 and sel_kvar9.nzp_kvar=a.nzp ");
                    sql2.Append(" and is_actual=1 and dat_s<=current_date and dat_po>=current_date)");
#else
                    sql2.Append(" update sel_kvar9 set pl_kvar=(select max(Replace(val_prm,',','.')+0)");
                    sql2.Append(" from " + pref + "_data:prm_1 a where nzp_prm=4 and sel_kvar9.nzp_kvar=a.nzp ");
                    sql2.Append(" and is_actual=1 " + dataUsl + ")");
#endif
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    //Жилая площадь
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update sel_kvar9 set pl_gil=(select max(Replace(val_prm,',','.')::numeric)");
                    sql2.Append(" from " + pref + "_data.prm_1 a where nzp_prm=6 and sel_kvar9.nzp_kvar=a.nzp ");
                    sql2.Append(" and is_actual=1 and dat_s<=current_date and dat_po>=current_date)");
#else
                    sql2.Append(" update sel_kvar9 set pl_gil=(select max(Replace(val_prm,',','.')+0)");
                    sql2.Append(" from " + pref + "_data:prm_1 a where nzp_prm=6 and sel_kvar9.nzp_kvar=a.nzp ");
                    sql2.Append(" and is_actual=1 " + dataUsl + ")");
#endif
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    //Количество жильцов
                    //Временно потом из calc_gku
                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update sel_kvar9 set count_gil = ((select max(cnt1) ");
                    sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ".counters_");
                    sql2.Append(prm.month_.ToString("00") + " a");
                    sql2.Append(" where a.nzp_kvar = sel_kvar9.nzp_kvar and nzp_type=3 and stek=3))");
#else
                    sql2.Append(" update sel_kvar9 set count_gil = (select max(round(gil)) ");
                    sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ":calc_gku_");
                    sql2.Append(prm.month_.ToString("00") + " a");
                    sql2.Append(" where a.nzp_kvar = sel_kvar9.nzp_kvar and tarif>0 )");
#endif
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    //Определить расчетную площадь для изолированных квартир как общую, для коммунальных - как жилую
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update sel_kvar9 set pl_calc =  (select max(squ) ");
                    sql2.Append(" from " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") + ":calc_gku_");
                    sql2.Append(prm.month_.ToString("00") + " a");
                    sql2.Append(" where a.nzp_kvar = sel_kvar9.nzp_kvar and tarif>0)");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);


                    /////////////////

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" update sel_kvar9 set type_kv=(select max(val_prm::numeric)");
                    sql2.Append(" from " + pref + "_data.prm_1 a where nzp_prm=2009 and sel_kvar9.nzp_kvar=a.nzp ");
                    sql2.Append(" and is_actual=1 and dat_s<=current_date and dat_po>=current_date)");
#else
                    sql2.Append(" update sel_kvar9 set type_kv=(select max(val_prm+0)");
                    sql2.Append(" from " + pref + "_data:prm_1 a where nzp_prm=2009 and sel_kvar9.nzp_kvar=a.nzp ");
                    sql2.Append(" and is_actual=1 " + dataUsl + ")");
#endif
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);

                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" update sel_kvar9 set type_kv=0 where type_kv is null");
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);
                    #endregion

                    sql2.Remove(0, sql2.Length);
#if PG
                    sql2.Append(" select kv ,");
                    sql2.Append(" sum(count_room) as count_room,");
                    sql2.Append(" sum(isol) as isol,");
                    sql2.Append(" max(type_kv) as type_kv,");
                    sql2.Append(" sum(pl_kvar) as pl_kvar,");
                    sql2.Append(" sum(pl_gil) as pl_gil,");
                    sql2.Append(" sum(pl_calc) as pl_calc,");
                    sql2.Append(" sum(count_gil) as count_gil ");
                    sql2.Append(" into unlogged sel_kvar91   from sel_kvar9 ");
                    sql2.Append(" group by 1 ");
#else
                    sql2.Append(" select kv ,");
                    sql2.Append(" sum(count_room) as count_room,");
                    sql2.Append(" sum(isol) as isol,");
                    sql2.Append(" max(type_kv) as type_kv,");
                    sql2.Append(" sum(pl_kvar) as pl_kvar,");
                    sql2.Append(" sum(pl_gil) as pl_gil,");
                    sql2.Append(" sum(pl_calc) as pl_calc,");
                    sql2.Append(" sum(count_gil) as count_gil ");
                    sql2.Append(" from sel_kvar9 ");
                    sql2.Append(" group by 1 into temp sel_kvar91 with no log ");
#endif
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);
                    ExecRead(conn_db, out reader2, "drop table sel_kvar9", true);


                    //По всем
#if PG
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into t_svod(count_room,count_kv, count_izol, count_komm, count_priv, count_2priv, ");
                    sql2.Append(" count_work, count_sobstv, count_jur, count_npriv, pl_kvar, pl_gil, pl_calc, count_gil)");
                    sql2.Append(" select coalesce(count_room,0) as count_room, ");
                    sql2.Append(" count(distinct kv), sum(case when isol=1 then isol else 0 end), ");
                    sql2.Append(" sum(0),  ");
                    sql2.Append(" sum(case when type_kv = 1 then 1 else 0 end) as count_priv,  ");
                    sql2.Append(" sum(case when type_kv = 2 then 1 else 0 end) as count_2priv,  ");
                    sql2.Append(" sum(case when type_kv = 3 then 1 else 0 end) as count_work,  ");
                    sql2.Append(" sum(case when type_kv = 4 then 1 else 0 end) as count_sobstv,  ");
                    sql2.Append(" sum(case when type_kv = 5 then 1 else 0 end) as count_jur,  ");
                    sql2.Append(" sum(case when type_kv = 0 then 1 else 0 end) as count_npriv,  ");
                    sql2.Append(" sum(pl_kvar) as pl_kvar,  ");
                    sql2.Append(" sum(pl_gil) as pl_gil,  ");
                    sql2.Append(" sum(pl_calc) as pl_calc,  ");
                    sql2.Append(" sum(count_gil) as count_gil  ");
                    sql2.Append(" from sel_kvar91 a group by 1");
#else
                    sql2.Remove(0, sql2.Length);
                    sql2.Append(" insert into t_svod(count_room,count_kv, count_izol, count_komm, count_priv, count_2priv, ");
                    sql2.Append(" count_work, count_sobstv, count_jur, count_npriv, pl_kvar, pl_gil, pl_calc, count_gil)");
                    sql2.Append(" select nvl(count_room,0) as count_room, ");
                    sql2.Append(" count(unique kv), sum(case when isol=1 then isol else 0 end), ");
                    sql2.Append(" sum(0),  ");
                    sql2.Append(" sum(case when type_kv = 1 then 1 else 0 end) as count_priv,  ");
                    sql2.Append(" sum(case when type_kv = 2 then 1 else 0 end) as count_2priv,  ");
                    sql2.Append(" sum(case when type_kv = 3 then 1 else 0 end) as count_work,  ");
                    sql2.Append(" sum(case when type_kv = 4 then 1 else 0 end) as count_sobstv,  ");
                    sql2.Append(" sum(case when type_kv = 5 then 1 else 0 end) as count_jur,  ");
                    sql2.Append(" sum(case when type_kv = 0 then 1 else 0 end) as count_npriv,  ");
                    sql2.Append(" sum(pl_kvar) as pl_kvar,  ");
                    sql2.Append(" sum(pl_gil) as pl_gil,  ");
                    sql2.Append(" sum(pl_calc) as pl_calc,  ");
                    sql2.Append(" sum(count_gil) as count_gil  ");
                    sql2.Append(" from sel_kvar91 a group by 1");
#endif
                    ExecRead(conn_db, out reader2, sql2.ToString(), true);




                    ExecRead(conn_db, out reader2, "drop table sel_kvar91", true);

                }
            }


            sql2.Remove(0, sql2.Length);
#if PG
            sql2.Append(" select (case when count_room>4 then 4 else count_room end) as count_room, ");
            sql2.Append(" sum(count_kv) as count_kv, ");
            sql2.Append(" sum(count_izol) as count_izol, ");
            sql2.Append(" sum(count_kv - count_izol) as count_komm, ");
            sql2.Append(" sum(count_priv) as count_priv, ");
            sql2.Append(" sum(count_2priv) as count_2priv, ");
            sql2.Append(" sum(count_work) as count_work, ");
            sql2.Append(" sum(count_sobstv) as count_sobstv, ");
            sql2.Append(" sum(count_jur) as count_jur,  sum(count_npriv) as count_npriv, ");
            sql2.Append(" sum(coalesce(pl_kvar,0)) as pl_kvar, ");
            sql2.Append(" sum(coalesce(pl_gil,0)) as pl_gil, ");
            sql2.Append(" sum(coalesce(pl_calc,0)) as pl_calc, ");
            sql2.Append(" sum(coalesce(count_gil,0)) as count_gil");
            sql2.Append(" from t_svod a ");
            sql2.Append(" group by  1 ");
            sql2.Append(" order by 1  ");
#else
            sql2.Append(" select (case when count_room>4 then 4 else count_room end) as count_room, ");
            sql2.Append(" sum(count_kv) as count_kv, ");
            sql2.Append(" sum(count_izol) as count_izol, ");
            sql2.Append(" sum(count_kv - count_izol) as count_komm, ");
            sql2.Append(" sum(count_priv) as count_priv, ");
            sql2.Append(" sum(count_2priv) as count_2priv, ");
            sql2.Append(" sum(count_work) as count_work, ");
            sql2.Append(" sum(count_sobstv) as count_sobstv, ");
            sql2.Append(" sum(count_jur) as count_jur,  sum(count_npriv) as count_npriv, ");
            sql2.Append(" sum(nvl(pl_kvar,0)) as pl_kvar, ");
            sql2.Append(" sum(nvl(pl_gil,0)) as pl_gil, ");
            sql2.Append(" sum(nvl(pl_calc,0)) as pl_calc, ");
            sql2.Append(" sum(nvl(count_gil,0)) as count_gil");
            sql2.Append(" from t_svod a ");
            sql2.Append(" group by  1 ");
            sql2.Append(" order by 1  ");
#endif
            if (!ExecRead(conn_db, out reader2, sql2.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки " + sql2.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                conn_db.Close();
                conn_web.Close();
                ret.result = false;
                return null;
            }


            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            if (reader2 != null)
            {
                try
                {

                    LocalTable.Load(reader2, LoadOption.OverwriteChanges);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("!!!" + ex.Message, MonitorLog.typelog.Error, true);
                    conn_web.Close();
                    conn_db.Close();
                    reader.Close();
                    return null;
                }
            }
            if (reader2 != null) reader2.Close();
            sql2.Remove(0, sql2.Length);
            sql2.Append("drop table t_svod");
            ExecRead(conn_db, out reader2, sql2.ToString(), true);
            conn_db.Close();




            reader.Close();
            #endregion

            conn_web.Close();
            return LocalTable;

        }
       


    }
}
