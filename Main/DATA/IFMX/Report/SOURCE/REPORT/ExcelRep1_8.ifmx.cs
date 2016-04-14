using System;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public partial class ExcelRep 
    {
        public DataTable GetSpravSoderg6(Prm prm, out Returns ret, string nzpUser)
        {
            using (var report = new GetSpravSoderg6())
            {
                return report.GetSprav(prm.year_, prm.month_,
                    prm.nzp_key, prm.has_pu, out ret, nzpUser);
            }
        }
    }


    /// <summary>
    /// Справка по содержанию по услуге холодная вода
    /// </summary>
    public class GetSpravSoderg6 : DataBaseHead
    {
        IDbConnection _connDB;

        /// <summary>
        /// Обертка для генерации исключений при ошибках
        /// </summary>
        /// <param name="sql">Текст запроса</param>
        /// <param name="inlog">Признак логгирования</param>
        private void ExecSQL(string sql, bool inlog)
        {
            Returns ret = ExecSQL(_connDB, sql, inlog);
            if (inlog && !ret.result)
            {
                throw new Exception("Ошибка выполнения SQL запроса");
            }
        }


        /// <summary>
        /// Получение отчета Справки по содержанию услуг Холодная Вода
        /// </summary>
        /// <param name="hasPU">Наличие ПУ,1 - Все, 2- только с ПУ, 3 только Без ПУ </param>
        /// <param name="ret"></param>
        /// <param name="nzpUser">код пользователя</param>
        /// <param name="year">Год расчета</param>
        /// <param name="month">Месяц расчета</param>
        /// <param name="nzpSupp">Код поставщика</param>
        /// <returns></returns>
        public DataTable GetSprav(int year, int month, Int64 nzpSupp, string hasPU, out Returns ret, string nzpUser)
        {

            if (ConnectToDb(out ret)) return null;

            string sql = String.Empty;
            
            try
            {
                #region Получение данных



                ExecSQL("drop table t_svod", false);

                sql = " create temp table t_svod( " +
                      " nzp_dom integer," +
                      " nzp_kvar integer," +
                      " nzp_geu integer," +
                      " name_frm char(100)," +
                      " nzp_measure integer, " +
                      " count_gil integer," +
                      " rsum_tarif numeric(14,2), " +
                      " tarif numeric(14,3), " +
                      " c_calc numeric(14,4), " +
                      " c_sn numeric(14,4), " +
                      " c_sn_odn numeric(14,4), " +
                      " c_calc_odn numeric(14,4), " +
                      " sum_nedop numeric(14,2), " +
                      " reval_k numeric(14,2)," +
                      " reval_d numeric(14,2), " +
                      " c_nedop numeric(14,2)," +
                      " c_reval_k numeric(14,2)," +
                      " c_reval_d numeric(14,2), " +
                      " sum_odn numeric(14,2), " +
                      " c_odn numeric(14,4), " +
                      " c_reval numeric(14,4), " +
                      " c_reval_odn numeric(14,4), " +
                      " pl_kvar numeric(14,2), " +
                      " sum_charge numeric(14,2)) ";
                ExecSQL(sql, true);


                ExecSQL("drop table sel_kvar_10", false);


                //Собираем во временную таблицу выбранные пользователем ЛС
                //т.к. во время выполнения отчета пользователь может выполнить новый поиск
                //и затереть выборку
                sql = " select nzp_dom, nzp_geu, nzp_kvar, pref " +
                      " into temp sel_kvar_10 " +
                      " from " + DBManager.sDefaultSchema + "." + "t" + nzpUser + "_spls ";
                ExecSQL(sql, true);

                #region Цикл по схемам данных
                sql = " select pref " +
                      " from  sel_kvar_10 " +
                      " group by 1";
                DataTable listPrefTable = DBManager.ExecSQLToTable(_connDB, sql);


                foreach (DataRow dr in listPrefTable.Rows)
                {
                    if (dr["pref"] != DBNull.Value)
                    {
                        FillOneSchema(year, month, nzpSupp, hasPU, dr["pref"].ToString());
                    }

                }
                #endregion

                return GetResultTable();

                #endregion

                
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выборки: " +
                                    ex + Environment.NewLine +
                                    " Последний запрос: " + sql, 
                                    MonitorLog.typelog.Error, 20, 201, true);

                ret.result = false;
                return null;
            }
            finally
            {
                
                _connDB.Close();
            }

        }


        /// <summary>
        /// Получить итоговую выборку данных для отчета
        /// </summary>
        /// <returns></returns>
        private DataTable GetResultTable()
        {
            string sql = " select nzp_dom, nzp_kvar, nzp_geu into temp t_dom from t_svod group by 1,2,3 ; ";
            ExecSQL(sql, true);

            sql = " select name_frm into temp t_frm from t_svod group by 1; ";
            ExecSQL(sql, true);


            sql = " insert into t_svod(nzp_geu,  nzp_dom, nzp_kvar, name_frm) " +
                  " select nzp_geu,  nzp_dom, nzp_kvar, name_frm " +
                  " from t_dom, t_frm ";
            ExecSQL(sql, true);

            ExecSQL("drop table t_dom", true);
            ExecSQL("drop table t_frm", true);
            ExecSQL("drop table sel_kvar_10", true);



            sql = " select geu, ulica, ndom, Replace(nkor,'-','') as nkor, idom,  " +
                  " name_frm,sum(coalesce(count_gil,0)) as count_gil, " +
                  " sum(coalesce(c_calc,0)) as volume, " +
                  " sum(coalesce(rsum_tarif,0)) as rsum_tarif,  " +
                  " sum(coalesce(rsum_tarif,0)) as rsum_tarif_wodn,  " +
                  " max(coalesce(a.tarif,0)) as tarif ," +
                  " max(coalesce(a.nzp_measure,0)) as nzp_measure," +
                  " sum(coalesce(sum_nedop,0)) as sum_nedop,  " +
                  " sum(-1*coalesce(reval_k,0)) as reval_k, " +
                  " sum(coalesce(reval_d,0)) as reval_d," +
                  " sum(coalesce(c_nedop,0)) as c_nedop,  " +
                  " sum(coalesce(c_reval_k,0)) as c_reval_k, " +
                  " sum(coalesce(c_reval_d,0)) as c_reval_d," +
                  " sum(coalesce(sum_charge,0)) as sum_charge, " +
                  " sum(coalesce(c_calc,0)) as c_calc, " +
                  " sum(coalesce(c_calc_odn,0)) as c_calc_odn, " +
                  " sum(coalesce(c_reval,0)) as c_reval, " +
                  " sum(coalesce(c_reval_odn,0)) as c_reval_odn, " +
                  " sum(coalesce(pl_kvar,0)) as pl_kvar, " +
                  " sum(coalesce(sum_odn,0)) as sum_odn " +
                  " from t_svod a, " + 
                  Points.Pref + sDataAliasRest+ "dom  d, " + 
                  Points.Pref + sDataAliasRest+ "s_ulica  su, " + 
                  Points.Pref + sDataAliasRest+ "s_geu  sg " +
                  " where a.nzp_dom=d.nzp_dom " +
                  "     and d.nzp_ul=su.nzp_ul " +
                  "     and a.nzp_geu=sg.nzp_geu " +
                  " group by  1,2,3,4,5,6" +
                  " order by geu, ulica, idom, ndom, nkor, name_frm  ";
            DataTable localTable = DBManager.ExecSQLToTable(_connDB, sql, 1200);

            ExecSQL("drop table t_svod", true);

            return localTable;
        }

        /// <summary>
        /// Заполнение данных по одной схеме
        /// </summary>
        /// <param name="year">Год расчета</param>
        /// <param name="month">Месяц расчета</param>
        /// <param name="nzpSupp">Код поставщика</param>
        /// <param name="hasPU">Наличие ПУ,1 - Все, 2- только с ПУ, 3 только Без ПУ</param>
        /// <param name="localPref">Префикс схемы</param>
        private void FillOneSchema(int year, int month, long nzpSupp, string hasPU, string localPref)
        {
            string pref = localPref.Trim();
            string sChargeAlias = pref + "_charge_" + (year - 2000).ToString("00") + '.';

            ExecSQL(" drop table sel_kvar10", false);

            FillSelKvar(month, nzpSupp, hasPU, pref, sChargeAlias);


            ExecSQL("drop table t1", false);

            #region Добавляем основную услугу

            string sql = " select nzp_geu, nzp_dom, a.nzp_kvar, nzp_frm, " +
                         " sum(0) as nzp_measure,min(nzp_serv) as nzp_serv, " +
                         " max(tarif) as tarif, max(count_gil) as count_gil," +
                         " sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop," +
                         " sum(case when real_charge<0 then real_charge else 0 end) +" +
                         " sum(case when reval<0 then reval else 0 end) as reval_k," +
                         " sum(case when real_charge>0 then real_charge else 0 end) +" +
                         " sum(case when reval>0 then reval else 0 end) as reval_d," +
                         " sum(case when nzp_serv=6 " +
                         " then 0 else rsum_tarif end) as sum_odn, " +
                         " sum(case when nzp_serv=6 and tarif>0.0001 " +
                         " then (reval + sum_nedop)/tarif else 0 end) as c_reval, " +
                         " sum(case when nzp_serv=510 and tarif>0.0001 " +
                         " then (reval + sum_nedop)/tarif else 0 end) as c_reval_odn, " +
                         "  max(cast (0 as numeric(14,4))) as c_calc, " +
                         "  max(cast (0 as numeric(14,4))) as c_calc_odn, " +
                         "  max(cast (0 as numeric(14,4))) as c_sn, " +
                         "  max(cast (0 as numeric(14,4))) as c_sn_odn, " +
                         " sum(sum_charge) as sum_charge, max(cast (0 as numeric(14,2))) as pl_kvar " +
                         " into temp t1 from  " + sChargeAlias + ".charge_" +
                         month.ToString("00") + " a, sel_kvar10 b" +
                         " where nzp_serv in (6, 510) " +
                         " and a.nzp_kvar=b.nzp_kvar " +
                         " and dat_charge is null ";
            if (nzpSupp > -1) //Добавляем поставщика
            {
                sql += " and a.nzp_supp = " + nzpSupp;
            }
            sql += " group by 1,2,3,4";
            ExecSQL(sql, true);


            sql = " update t1 set pl_kvar = (select max(pl_kvar) " +
                  " from  sel_kvar10 d" +
                  " where d.nzp_kvar=t1.nzp_kvar " +
                  " and isol=2 )" +
                  " where (6 in (select nzp_serv from " + pref
                  + sKernelAliasRest + "s_counts where nzp_serv<>8) or " +
                  " 6 in (select nzp_serv from " + pref +
                  sKernelAliasRest + "serv_odn where nzp_serv<>512)) and " +
                  " nzp_kvar in (select nzp_kvar from sel_kvar10 where isol=2) ";
            ExecSQL(sql, true);

            #region Выборка перерасчетов прошлого периода

            sql =
                " select b.nzp_geu,b.nzp_dom,a.nzp_kvar, min(date_part('year',dat_s)*12+date_part('month',dat_s)) as month_s,  max(date_part('year',dat_po)*12+date_part('month',dat_po)) as month_po" +
                " into temp t_nedop from " + pref + "_data.nedop_kvar a, sel_kvar10 b " +
                " where a.nzp_kvar=b.nzp_kvar and month_calc='01." + month.ToString("00") + "." +
                year.ToString("0000") + "'  " +
                " group by 1,2,3 ";
            ExecSQL(sql, true);


            sql = " select month_, year_ " +
                  " from " + pref + "_charge_" + (year - 2000).ToString("00")
                  + ".lnk_charge_" + month.ToString("00") + " b, t_nedop d " +
                  " where  b.nzp_kvar=d.nzp_kvar " +
                  " and year_*12+month_>=month_s and  year_*12+month_<=month_po " +
                  " group by 1,2";
            DataTable listMonth = DBManager.ExecSQLToTable(_connDB, sql);
            foreach (DataRow dp in listMonth.Rows)
            {
                string sTmpAlias = pref + "_charge_" +
                                   (Int32.Parse(dp["year_"].ToString()) - 2000).ToString("00");


                sql =
                    " insert into t1 (nzp_geu, nzp_dom, nzp_kvar, nzp_frm, tarif, sum_nedop, reval_k, reval_d)   " +
                    " select nzp_geu, nzp_dom, b.nzp_kvar, 0, 0,  " +
                    " sum(sum_nedop-sum_nedop_p),  " +
                    " sum(case when (sum_nedop-sum_nedop_p)>0 " +
                    " then sum_nedop-sum_nedop_p else 0 end ) as reval_k," +
                    " sum(case when (sum_nedop-sum_nedop_p)>0 " +
                    " then 0 else sum_nedop-sum_nedop_p end ) as reval_d" +
                    " from " + sTmpAlias + ".charge_" +
                    Int32.Parse(dp["month_"].ToString()).ToString("00") +
                    " b, t_nedop d " +
                    " where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28." +
                    month.ToString("00") + "." + year + "')" +
                    " and abs(sum_nedop)+abs(sum_nedop_p)>0.001" +
                    " and nzp_serv in (6, 510)";
                if (nzpSupp > -1) //Добавляем поставщика
                {
                    sql += " and nzp_supp = " + nzpSupp;
                }
                sql += " group by 1,2,3,4,5";
                ExecSQL(sql, true);
            }

            ExecSQL("drop table t_nedop", true);

            #endregion

            #endregion

            ExecSQL("drop table t_cg ", false);

            sql = "  Create temp table t_cg(nzp_kvar integer, " +
                  "  nzp_frm integer, " +
                  "  nzp_serv integer," +
                  "  rashod Numeric(14,4), " +
                  "  rashod_odn Numeric(14,4)) ";
            ExecSQL(sql, true);


            sql = " insert into t_cg(nzp_kvar, nzp_frm,nzp_serv, rashod, rashod_odn ) " +
                  " select d.nzp_kvar, nzp_frm, nzp_serv, sum(valm+dlt_reval),  sum(case when valm+dlt_reval>=0 and dop87<0 and " +
                  " dop87< -valm-dlt_reval then -valm-dlt_reval  " +
                  "          else dop87 end) " +
                  " from  " + pref + "_charge_" + (year - 2000).ToString("00") + tableDelimiter +
                  "calc_gku_"
                  + month.ToString("00") + " d, sel_kvar10 a " +
                  " where d.tarif>0 and nzp_serv in (6,510) " +
                  " and a.nzp_kvar = d.nzp_kvar ";
            if (nzpSupp > -1) //Добавляем поставщика
            {
                sql += " and d.nzp_supp = " + nzpSupp;
            }
            sql += " group by 1,2,3";
            ExecSQL(sql, true);

            ExecSQL("create index ix_tcg_09 on t_cg (nzp_kvar,nzp_frm,nzp_serv)", true);
            ExecSQL(sUpdStat + " t_cg ", true);


            //Проставляем расход по основным услугам

            sql = " update t1 set c_calc = d.rashod " +
                  " from t_cg d" +
                  " where d.nzp_kvar=t1.nzp_kvar " +
                  "     and d.nzp_frm =t1.nzp_frm " +
                  "     and d.nzp_serv=6 " +
                  "     and t1.tarif>0  ";
            ExecSQL(sql, true);


            sql = " update t1 set c_calc_odn= (select rashod_odn " +
                  "                            from  t_cg d " +
                  "                            where d.nzp_kvar=t1.nzp_kvar  " +
                  "                                 and t1.nzp_frm=d.nzp_frm) " +
                  " where nzp_serv=6  and tarif>0  " +
                  " and 0<( select count(*) " +
                  "         from  t_cg d " +
                  "         where d.nzp_kvar=t1.nzp_kvar  " +
                  "             and d.nzp_serv=510) ";
            ExecSQL(sql, true);

            ExecSQL("drop table t_cg ", true);

            ExecSQL("drop table t2", false);

            sql = " select nzp_kvar, max(nzp_frm) as nzp_frm, max(tarif) as tarif, " +
                  " max(pl_kvar) as pl_kvar, max(count_gil) as count_gil  " +
                  " into temp t2 " +
                  " from t1  " +
                  " where nzp_frm>0 " +
                  " group by 1 ";
            ExecSQL(sql, true);

            sql = " update t1 set  " +
                  " nzp_frm = (select max(nzp_frm) from t2 where t1.nzp_kvar=t2.nzp_kvar), " +
                  " tarif = (select max(tarif) from t2 where t1.nzp_kvar=t2.nzp_kvar), " +
                  " pl_kvar = (select sum(pl_kvar) as pl_kvar from t2 where t1.nzp_kvar=t2.nzp_kvar), " +
                  " count_gil = (select sum(count_gil) as count_gil from t2 where t1.nzp_kvar=t2.nzp_kvar) ";
            ExecSQL(sql, true);
            ExecSQL("drop table t2", false);

            ExecSQL(" create index ixtmm_01 on t1(nzp_frm)", true);
            ExecSQL(" analyze t1", true);


            sql = " update t1 set nzp_measure= coalesce((select max(nzp_measure) from " + pref +
                  "_kernel.formuls f " +
                  " where  t1.nzp_frm=f.nzp_frm),0) ";
            ExecSQL(sql, true);

            sql = " update t1 set tarif= coalesce((select max(tarif_hv_gv) from " + Points.Pref +
                  "_data.a_trf_smr f " +
                  " where  t1.nzp_frm=f.nzp_frm and is_priv=1 ),0) " +
                  " where  nzp_frm in (select nzp_frm from  " + Points.Pref + "_data.a_trf_smr  " +
                  " where  is_priv=1) and nzp_measure<>3 ";
            ExecSQL(sql, true);

            ExecSQL("drop table t121", false);
            sql = " select nzp_dom, max(tarif) as tarif  into unlogged t121 from t1 group by 1 ";
            ExecSQL(sql, true);

            sql = " update t1 set tarif= coalesce((select tarif from t121 f " +
                  " where  t1.nzp_dom=f.nzp_dom),0)" +
                  " where  coalesce(tarif,0) =0 ";
            ExecSQL(sql, true);

            ExecSQL("drop table t121", true);

            sql = " update t1 set nzp_dom = a.nzp_dom_base " +
                  " from " + pref + "_data.link_dom_lit a" +
                  " where a.nzp_dom=t1.nzp_dom";
            ExecSQL(sql, true);


            //По всем

            sql = " insert into t_svod(nzp_geu, nzp_dom, nzp_kvar, name_frm, nzp_measure, " +
                  " count_gil, rsum_tarif, tarif, " +
                  " sum_nedop, c_nedop, reval_k, reval_d, c_reval_k, c_reval_d, sum_charge, " +
                  " c_calc, c_sn, c_sn_odn, c_reval, c_calc_odn, " +
                  " c_reval_odn, pl_kvar, sum_odn )" +
                  " select nzp_geu,  a.nzp_dom,  a.nzp_kvar, " +
                  " trim(coalesce(name_frm,'Не определена формула'))||' . '||a.tarif as name_frm, " +
                  " f.nzp_measure, max(count_gil), sum(rsum_tarif), " +
                  " max(a.tarif) as tarif, " +
                  " sum(sum_nedop) as sum_nedop,  " +
                  " sum(case when a.tarif>0 then sum_nedop/a.tarif else 0 end) as c_nedop, " +
                  " sum(reval_k) as reval_k," +
                  " sum(reval_d) as reval_d," +
                  " sum(case when a.tarif>0 then -1*reval_k/a.tarif else 0 end) as c_reval_k," +
                  " sum(case when a.tarif>0 then reval_d/a.tarif else 0 end) as c_reval_d," +
                  " sum(sum_charge) as sum_charge, sum(c_calc) as c_calc, sum(c_sn) as c_sn," +
                  " sum(c_sn_odn) as c_sn_odn, sum(c_reval) as c_reval, sum(c_calc_odn) as c_calc_odn, " +
                  " sum(c_reval_odn) as c_reval_odn, max(pl_kvar) as pl_kvar, " +
                  " sum(sum_odn) as sum_odn " +
                  " from t1 a left outer join " + pref + "_kernel.formuls f on (a.nzp_frm=f.nzp_frm) " +
                  " where abs(coalesce(sum_nedop,0))+abs(coalesce(rsum_tarif,0))+" +
                  " abs(coalesce(reval_k,0)) + abs(coalesce(reval_d,0))+abs(coalesce(sum_charge,0))+ abs(coalesce(pl_kvar,0))>0.001 " +
                  " group by 1,2,3,4,5";
            ExecSQL(sql, true);


            ExecSQL("drop table t1", true);
        }


        /// <summary>
        /// Подключение к БД
        /// </summary>
        /// <param name="ret"></param>
        /// <returns></returns>
        private bool ConnectToDb(out Returns ret)
        {
            #region Подключение к БД
        
            _connDB = GetConnection(Constants.cons_Kernel);

            ret = OpenDb(_connDB, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("GetSpravSoderg6 : Ошибка при открытии соединения с локальной БД ",
                    MonitorLog.typelog.Error, true);
            
                return true;
            }

            #endregion

            return false;
        }

        /// <summary>
        /// Выборка лицевых счетов для отчета
        /// </summary>
        /// <param name="hasPU"></param>
        /// <param name="pref"></param>
        /// <param name="sChargeAlias"></param>
        /// <param name="month"></param>
        /// <param name="nzpSupp"></param>
        private void FillSelKvar(int month, Int64 nzpSupp, string hasPU, string pref, string sChargeAlias)
        {
            ExecSQL( "drop table sel_kvar10 ", true);

            string sql = " create temp table sel_kvar10 ( " +
                         " nzp_kvar integer," +
                         " nzp_dom integer," +
                         " isol integer default 1," +
                         " nzp_geu integer," +
                         " pl_kvar Numeric(14,2)," +
                         " count_gil integer) ";
            ExecSQL( sql, true);


            sql = " insert into sel_kvar10(nzp_dom, nzp_geu, nzp_kvar, count_gil)" +
                  " select nzp_dom, nzp_geu, nzp_kvar, 0 " +
                  " from sel_kvar_10 k " +
                  " where pref='" + pref + "'";
            if (hasPU == "2")
                sql += " and exists (select 1  " +
                       " from " + sChargeAlias + "counters_" + month.ToString("00") + " d" +
                       " where d.stek=3 and d.nzp_type=1 and d.cnt_stage>0 " +
                       " and d.nzp_serv=6 and k.nzp_dom=d.nzp_dom )";

            if (hasPU == "3")
                sql += " and not exists (select 1  " +
                       " from " + sChargeAlias + "counters_" + month.ToString("00") + " d" +
                       " where d.stek=3 and d.nzp_type=1 and d.cnt_stage>0 " +
                       " and d.nzp_serv=6 and d.nzp_dom=k.nzp_dom) ";
            ExecSQL( sql, true);


            ExecSQL( "create index ix_tmpk_02 on sel_kvar10(nzp_kvar)", true);

            ExecSQL( sUpdStat + " sel_kvar10 ", true);

            #region Проставляем количество жильцов и площадь

            ExecSQL( "drop table t_cg ", false);

            sql = "  Create temp table t_cg(nzp_kvar integer, " +
                  "     gil integer, " +
                  "     squ Numeric(14,2)) ";
            ExecSQL( sql, true);


            sql = "  insert into t_cg (nzp_kvar, gil, squ)" +
                  "  select a.nzp_kvar, sum(gil) as gil, sum(squ) as squ " +
                  "  from  " + sChargeAlias + "calc_gku_" + month.ToString("00") +
                  " a, sel_kvar10 b" +
                  "  where a.nzp_kvar=b.nzp_kvar " +
                  "     and nzp_serv=6 " +
                  "     and stek=3";
            if (nzpSupp > -1) sql += " and nzp_supp = " + nzpSupp; //Добавляем поставщика
            sql += " group by 1";
            ExecSQL( sql, true);

            ExecSQL( "create index ix_tcg_09 on t_cg (nzp_kvar)", true);
            ExecSQL( sUpdStat + " t_cg ", true);


            sql = " update sel_kvar10 set " +
                  " count_gil = a.gil " +
                  " from t_cg a" +
                  " where a.nzp_kvar =sel_kvar10.nzp_kvar  ";
            ExecSQL( sql, true);

            sql = " update sel_kvar10 set " +
                  " pl_kvar = a.squ " +
                  " from t_cg a" +
                  " where a.nzp_kvar =sel_kvar10.nzp_kvar  ";
            ExecSQL( sql, true);

            ExecSQL( "drop table t_cg ", true);

            #endregion

            //Проставляем коммунальные квартиры
            sql = " update sel_kvar10 set isol=2 " +
                  " where exists (select nzp " +
                  " from " + pref + sDataAliasRest + "prm_1 " +
                  " where nzp=nzp_kvar and nzp_prm=3 " +
                  " and is_actual=1 " +
                  " and val_prm='2'" +
                  " and dat_s<=" + sCurDate +
                  " and dat_po>=" + sCurDate + " )";
            ExecSQL( sql, true);

        }
    }
}
