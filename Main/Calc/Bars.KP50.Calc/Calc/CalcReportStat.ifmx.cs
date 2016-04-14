using System;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{

    /// <summary>
    /// Класс предназначенный для подсчета статистики по начислениям в
    /// аналитике
    /// </summary>
    public class DbCalcReportStat
    {
        private readonly IDbConnection _connection;

        public DbCalcReportStat(IDbConnection connDB)
        {
            _connection = connDB;
        }

        /// <summary>
        /// Расчет статистики по начислениям
        /// по совместительству сальдо дома
        /// </summary>
        /// <param name="month">Месяц расчета</param>
        /// <param name="year">Год расчета</param>
        /// <param name="pref">Префикс базы данных</param>
        /// <param name="nzpWp">Идентификатор базы данных</param>
        /// <param name="nzpDom"></param>
        /// <returns></returns>
        public Returns CalcReportXX(int month, int year, string pref, int nzpWp, int nzpDom)
        {
            string reportXX = Points.Pref + "_fin_" + (year - 2000).ToString("00") +
                               DBManager.tableDelimiter + "fn_ukrgucharge";
            string reportXXDom = Points.Pref + "_fin_" + (year - 2000).ToString("00") +
                               DBManager.tableDelimiter + "fn_ukrgudom";

            DBManager.ExecSQL(_connection, "drop table t_sel_dom", false);

            try
            {

                string sql = " Create temp table t_sel_kv(nzp_kvar integer)" + DBManager.sUnlogTempTable;
                DBManager.ExecSQL(_connection, sql, true);

                //---------------------------------------------------
                //выбрать множество лицевых счетов
                //---------------------------------------------------
                MakeSelKvar(pref, nzpDom);

                //---------------------------------------------------
                //выборка сумм по домам
                //---------------------------------------------------
                string chargeTable = pref + "_charge_" + (year - 2000).ToString("00") +
                    DBManager.tableDelimiter + "charge_" + month.ToString("00");


                sql = " Create temp table t_ins_ch " +
                              " (nzp_area integer, " +
                              " nzp_geu  integer, " +
                              " nzp_dom  integer, " +
                              " nzp_serv integer, " +
                              " nzp_supp integer, " +
                              " nzp_wp   integer, " +
                              " rsum_tarif " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " sum_tarif " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " sum_tarif_f " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " sum_tarif_p " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " sum_tarif_f_p " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " rsum_lgota " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " sum_lgota " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " sum_real " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " sum_izm " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " old_money " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " sum_money " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " money_to " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " money_from " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " money_del " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " reval_k " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " reval_d " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " reval " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " real_charge_k " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " real_charge_d " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " real_charge " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " sum_nedop " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " sum_nedop_p " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " sum_pere " + DBManager.sDecimalType + "(14,2)  default 0, " +
                              " sum_insaldo " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " sum_outsaldo " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " sum_insaldo_k " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " sum_outsaldo_k " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " sum_insaldo_d " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " sum_outsaldo_d " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " sum_pere_d " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " old_charge " + DBManager.sDecimalType + "(14,2) default 0, " +
                              " sum_nach " + DBManager.sDecimalType + "(14,2) default 0 " +
                              " )" + DBManager.sUnlogTempTable;

                Returns ret = DBManager.ExecSQL(_connection, sql, true);
                if (!ret.result)
                {
                    return ret;
                }


                ret = DBManager.ExecSQL(_connection,
                    " Insert into t_ins_ch (nzp_wp, nzp_dom, nzp_area,nzp_geu,nzp_supp,nzp_serv,rsum_tarif, sum_tarif, sum_tarif_f,rsum_lgota,sum_lgota,sum_real,old_charge,sum_nach, " +
                    " reval,real_charge,sum_nedop,sum_pere,sum_insaldo,sum_outsaldo, old_money,sum_money,money_to,money_from,money_del, " +
                    " sum_insaldo_k,sum_insaldo_d,sum_outsaldo_k,sum_outsaldo_d, sum_izm, reval_k, reval_d, real_charge_k, real_charge_d ) " +
                    " Select " + nzpWp + ",k.nzp_dom,k.nzp_area,k.nzp_geu, nzp_supp,nzp_serv, " +
                    " sum(rsum_tarif) rsum_tarif,sum(sum_tarif) sum_tarif,sum(sum_tarif_f) as sum_tarif, sum(rsum_lgota) rsum_lgota,sum(sum_lgota) sum_lgota," +
                    " sum(sum_real) sum_real,sum(sum_charge) sum_charge,sum(sum_charge) sum_charge, " +
                    " sum(reval) reval,sum(real_charge) real_charge,sum(sum_nedop) sum_nedop,sum(sum_pere) sum_pere,sum(sum_insaldo) sum_insaldo," +
                    " sum(sum_outsaldo) sum_outsaldo, sum(sum_money) as old_money, sum(sum_money) as sum_money, sum(money_to) money_to, " +
                    " sum(money_from) money_from,sum(money_del) money_del, " +
                    " sum(case when sum_insaldo<0 then sum_insaldo else 0 end) sum_insaldo_k, " +
                    " sum(case when sum_insaldo<0 then 0 else sum_insaldo end) sum_insaldo_d, " +
                    " sum(case when sum_outsaldo<0 then sum_outsaldo else 0 end) sum_outsaldo_k, " +
                    " sum(case when sum_outsaldo<0 then 0 else sum_outsaldo end) sum_outsaldo_d, sum(izm_saldo), " +
                    " sum(case when reval<0 then reval else 0 end) reval_k, " +
                    " sum(case when reval<0 then 0 else reval end) reval_d, " +
                    " sum(case when real_charge<0 then real_charge else 0 end) real_charge_k, " +
                    " sum(case when real_charge<0 then 0 else real_charge end) real_charge_d " +
                    " From "+chargeTable+" a, "+pref+DBManager.sDataAliasRest+"kvar k,t_sel_kv t " +
                    " Where a.nzp_kvar = k.nzp_kvar and k.nzp_kvar=t.nzp_kvar " +
                    " and dat_charge is null and nzp_serv>1" +
                    " Group by 1,2,3,4,5,6 "
                    , true, 6000);

                if (!ret.result)
                {
                    return ret;
                }
                DBManager.ExecSQL(_connection, DBManager.sUpdStat + " t_ins_ch ", true);
                if (!ret.result) return ret;

                ret = CalcReval(month, year, pref, nzpWp);
                if (!ret.result) return ret;


                //Удаление перед вставкой
                sql = " delete from " + reportXXDom +
                      " where nzp_dom in (select nzp_dom from t_ins_ch)" +
                      " and month_=" + month;
                DBManager.ExecSQL(_connection, sql, true, 6000);
                if (!ret.result) return ret;

                sql = " insert into " +reportXXDom+
                      " (year_, month_, nzp_area, nzp_geu, nzp_dom, " +
                      " nzp_serv, nzp_supp, nzp_wp, rsum_tarif, sum_tarif, sum_tarif_f, " +
                      " sum_tarif_f_p, sum_tarif_p, rsum_lgota, sum_lgota, " +
                      " sum_real, real_charge_k, real_charge_d, reval_k, " +
                      " reval_d, sum_izm, old_money, sum_money, money_to," +
                      " money_from, money_del, reval, real_charge, sum_nedop, sum_nedop_p, " +
                      " sum_pere, sum_insaldo, sum_outsaldo, old_charge, sum_nach, " +
                      " sum_insaldo_k, sum_insaldo_d, sum_outsaldo_k, sum_outsaldo_d, " +
                      " sum_pere_d) " +
                      " select "+year+", "+month+", nzp_area, nzp_geu, nzp_dom, " +
                      " nzp_serv, nzp_supp, nzp_wp, " +
                      " sum(rsum_tarif), " +
                      " sum(sum_tarif), " +
                      " sum(sum_tarif_f), " +
                      " sum(sum_tarif_f_p), " +
                      " sum(sum_tarif_p), " +
                      " sum(rsum_lgota), " +
                      " sum(sum_lgota), " +
                      " sum(sum_real), " +
                      " sum(real_charge_k), " +
                      " sum(real_charge_d), " +
                      " sum(reval_k), " +
                      " sum(reval_d), " +
                      " sum(sum_izm), " +
                      " sum(old_money), " +
                      " sum(sum_money), " +
                      " sum(money_to)," +
                      " sum(money_from), " +
                      " sum(money_del), " +
                      " sum(reval), " +
                      " sum(real_charge), " +
                      " sum(sum_nedop), " +
                      " sum(sum_nedop_p), " +
                      " sum(sum_pere), " +
                      " sum(sum_insaldo), " +
                      " sum(sum_outsaldo), " +
                      " sum(old_charge), " +
                      " sum(sum_nach), " +
                      " sum(sum_insaldo_k), " +
                      " sum(sum_insaldo_d), " +
                      " sum(sum_outsaldo_k), " +
                      " sum(sum_outsaldo_d), " +
                      " sum(sum_pere_d) " +
                      " from t_ins_ch"+
                      " group by 1,2,3,4,5,6,7,8";
                DBManager.ExecSQL(_connection, sql, true, 6000);
                if (!ret.result) return ret;

                //Удаление перед вставкой
                sql = " delete from " + reportXX +
                      " where nzp_wp in (select nzp_wp from t_ins_ch)"+
                      " and month_=" + month;
                DBManager.ExecSQL(_connection, sql, true, 6000);
                if (!ret.result) return ret;


                sql = " insert into " + reportXX +
                      " (year_, month_, nzp_area, nzp_geu, " +
                      " nzp_serv, nzp_supp, nzp_wp, rsum_tarif, sum_tarif, sum_tarif_f, " +
                      " sum_tarif_f_p, sum_tarif_p, rsum_lgota, sum_lgota, " +
                      " sum_real, real_charge_k, real_charge_d, reval_k, " +
                      " reval_d, sum_izm, old_money, sum_money, money_to," +
                      " money_from, money_del, reval, real_charge, sum_nedop, sum_nedop_p, " +
                      " sum_pere, sum_insaldo, sum_outsaldo, old_charge, sum_nach, " +
                      " sum_insaldo_k, sum_insaldo_d, sum_outsaldo_k, sum_outsaldo_d, " +
                      " sum_pere_d) " +
                      " select year_, month_, nzp_area, nzp_geu, " +
                      " nzp_serv, nzp_supp, nzp_wp, " +
                      " sum(rsum_tarif), " +
                      " sum(sum_tarif), " +
                      " sum(sum_tarif_f), " +
                      " sum(sum_tarif_f_p), " +
                      " sum(sum_tarif_p), " +
                      " sum(rsum_lgota), " +
                      " sum(sum_lgota), " +
                      " sum(sum_real), " +
                      " sum(real_charge_k), " +
                      " sum(real_charge_d), " +
                      " sum(reval_k), " +
                      " sum(reval_d), " +
                      " sum(sum_izm), " +
                      " sum(old_money), " +
                      " sum(sum_money), " +
                      " sum(money_to)," +
                      " sum(money_from), " +
                      " sum(money_del), " +
                      " sum(reval), " +
                      " sum(real_charge), " +
                      " sum(sum_nedop), " +
                      " sum(sum_nedop_p), " +
                      " sum(sum_pere), " +
                      " sum(sum_insaldo), " +
                      " sum(sum_outsaldo), " +
                      " sum(old_charge), " +
                      " sum(sum_nach), " +
                      " sum(sum_insaldo_k), " +
                      " sum(sum_insaldo_d), " +
                      " sum(sum_outsaldo_k), " +
                      " sum(sum_outsaldo_d), " +
                      " sum(sum_pere_d) " +
                      " from " + reportXXDom +
                      " where nzp_wp in (select nzp_wp from t_ins_ch)" +
                      " and month_=" + month +
                      " group by 1,2,3,4,5,6,7";
                DBManager.ExecSQL(_connection, sql, true, 6000);
                if (!ret.result) return ret;


            }
            finally
            {
                DBManager.ExecSQL(_connection, "drop table t_ins_ch ", false);
                DBManager.ExecSQL(_connection, "drop table t_prev_ch ", false);
                DBManager.ExecSQL(_connection, "drop table t_sel_kv ", false);
            }
            return new Returns(true);
        }

        /// <summary>
        /// Выборка квартир для подсчета
        /// </summary>
        private void MakeSelKvar(string pref, int nzpDom)
        {
            string sql;
            if (!DBManager.TempTableInWebCashe(_connection, "t_selkvar"))
            {
                //Сделать выборку домов
                sql = " insert into t_sel_kv(nzp_kvar)" +
                      " select nzp_kvar " +
                      " from " + pref +DBManager.sDataAliasRest+ "dom d," +
                      "      " + pref +DBManager.sDataAliasRest+ "kvar k " +
                      " where d.nzp_dom=k.nzp_dom";
                if (nzpDom > 0)
                    sql += " and (d.nzp_dom =" + nzpDom +
                           " or nzp_dom in (select a.nzp_dom " +
                           " from " + pref + DBManager.sDataAliasRest + "link_dom_lit a," +
                           "      " + pref + DBManager.sDataAliasRest + "link_dom_lit b " +
                           " where a.nzp_dom_base=b.nzp_dom_base and b.nzp_dom=" + nzpDom + "))";
                DBManager.ExecSQL(_connection, sql, true);
            }
            else
            {
                sql = " insert into t_sel_kv(nzp_kvar)" +
                      " select nzp_kvar from t_selkvar ";
                DBManager.ExecSQL(_connection, sql, true);
            }

            DBManager.ExecSQL(_connection, "create index ix_tsq_98 on t_sel_kv(nzp_kvar)", true);
            DBManager.ExecSQL(_connection, DBManager.sUpdStat + " t_sel_kv", true);
        }

        /// <summary>
        /// Подсчет перерасчета предыдущего периода
        /// </summary>
        /// <returns></returns>
        private Returns CalcReval(int month, int year, string pref, int nzpWp)
        {
            DBManager.ExecSQL(_connection, " Drop table t_prev_ch ", false);

           Returns ret = DBManager.ExecSQL(_connection,
                " Create temp table t_prev_ch " +
                "  ( nzp_area integer, " +
                " nzp_geu  integer, " +
                " nzp_dom  integer, " +
                " nzp_serv integer, " +
                " nzp_supp integer, " +
                " sum_tarif " + DBManager.sDecimalType + "(14,2)," +
                " sum_tarif_f " + DBManager.sDecimalType + "(14,2)," +
                " sum_nedop " + DBManager.sDecimalType + "(14,2) " +
                " ) " + DBManager.sUnlogTempTable
                , true);

            if (!ret.result)
            {
                return ret;
            }

            string chargeTable = pref + "_charge_" + (year - 2000).ToString("00") +
                                 DBManager.tableDelimiter + "lnk_charge_" + month.ToString("00");
            string sKernelDBName = pref + DBManager.sKernelAliasRest;

            MyDataReader reader;
            ret = DBManager.ExecRead(_connection, out reader,
                " Select month_, dbname, dbserver, year_ From " + chargeTable + " b," +
                sKernelDBName + "s_baselist a, t_sel_kv c, " +
                sKernelDBName + "logtodb ld, " +
                sKernelDBName + "s_logicdblist sl " +
                " Where a.yearr = b.year_ and b.nzp_kvar = c.nzp_kvar " +
                "   and ld.nzp_bl = a.nzp_bl and sl.nzp_ldb = ld.nzp_ldb and sl.ldbname = 'RT' " +
                "   and idtype=1 " +
                " Group by 1,2,3,4 Order by 2,1  "
                , true);

            if (!ret.result)
            {
                return ret;
            }
            while (reader.Read())
            {
                if (reader["dbname"] == DBNull.Value) continue;

                int yearReval = int.Parse(reader["year_"].ToString());
                if (yearReval < Points.BeginCalc.year_)
                    continue;

                string dbname = reader["dbname"].ToString().Trim();
                int mn = int.Parse(reader["month_"].ToString());

                dbname += DBManager.tableDelimiter + "charge_" + mn.ToString("00");
                    

                ret = DBManager.ExecSQL(_connection,
                    " Insert into t_prev_ch (nzp_area,nzp_geu,nzp_dom,nzp_serv,nzp_supp,sum_tarif, sum_tarif_f, sum_nedop) " +
                    " Select k.nzp_area,k.nzp_geu,k.nzp_dom,c.nzp_serv, nzp_supp," +
                    " sum(sum_tarif)-sum(sum_tarif_p), " +
                    " sum(sum_tarif_f)-sum(sum_tarif_f_p), " +
                    " sum(sum_nedop)-sum(sum_nedop_p)  " +
                    " From " + dbname + " c, "+pref+DBManager.sDataAliasRest+"kvar k, t_sel_kv t " +
                    " Where c.dat_charge= " + DBManager.MDY(month, 28, year) +
                    "   and  c.nzp_kvar=k.nzp_kvar and k.nzp_kvar=t.nzp_kvar and nzp_serv>1 " +
                    " Group by 1,2,3,4,5 "
                    , true);
                if (!ret.result)
                {
                    reader.Close();
                    return ret;
                }
            }
            reader.Close();

            ret = DBManager.ExecSQL(_connection,
                " Insert into t_ins_ch (nzp_wp, nzp_area,nzp_geu,nzp_dom,nzp_serv,nzp_supp, sum_tarif_p, sum_tarif_f_p, sum_nedop_p) " +
                " Select " + nzpWp +
                ",nzp_area,nzp_geu,nzp_dom,nzp_serv,nzp_supp, sum(sum_tarif), sum(sum_tarif_f), " +
                " sum(sum_nedop) " +
                " From t_prev_ch group by 1,2,3,4,5,6 "
                , true);
            if (!ret.result)
            {
                return ret;
            }
            DBManager.ExecSQL(_connection, DBManager.sUpdStat + " t_ins_ch ", true);
            return new Returns(true);
        }
    }
}
