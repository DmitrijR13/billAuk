using System;
using System.Data;
using System.Diagnostics.Contracts;
using System.Globalization;
using FastReport.Utils;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Text;

namespace STCLINE.KP50.DataBase
{
    public partial class ExcelRep
    {
        public DataTable GetSpravSoderg2Water(Prm prm, out Returns ret, string Nzp_user)
        {
            var dbSpravSoderg2Water = new DbSpravSoderg2Water();
            return dbSpravSoderg2Water.GetSpravSoderg2Water(prm, out ret, Nzp_user);

        }
    }

    public class DbSpravSoderg2Water
    {

        private IDbConnection connDB;

        /// <summary>
        /// Процедура формирования отчета РСО по горячей воде для Самары
        /// </summary>
        /// <param name="prm"></param>
        /// <param name="ret"></param>
        /// <param name="Nzp_user"></param>
        /// <returns></returns>
        public DataTable GetSpravSoderg2Water(Prm prm, out Returns ret, string Nzp_user)
        {

            string tXXSpls = GetSelectedKvars(Nzp_user);

            ConnectToBd();
            var sql = new StringBuilder();




            DataTable localTable;
            try
            {



                string sqlsCr = PrepareSvodTable();

                DataTable dt = PrepareSelectedKvars(Nzp_user);
                foreach (DataRow dr in dt.Rows)
                {
                    CalcOneBank(prm, dr["pref"].ToString().Trim(), sqlsCr, sql);
                }

                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_svod_water SET vol_npu_kub = volume_all_kub - vol_ipu_kub");
                //sql.Append(" UPDATE t_svod_water SET vol_npu_kub = volume_all_kub - vol_ipu_kub, vol_npu_gkal = volume_all_gkal - vol_ipu_gkal");
                DBManager.ExecSQL(connDB, sql.ToString(), false);

                sql.Remove(0, sql.Length);
                sql.Append(" select trim(case when rajon = '-' then town else rajon end)||', '||trim(ulica) as ulica, " +
                           " ndom, idom, nkor, a.*" +
                           "  from t_svod_water a, " +
                           Points.Pref + DBManager.sDataAliasRest + "dom d, " +
                           Points.Pref + DBManager.sDataAliasRest + "s_ulica su, " +
                           Points.Pref + DBManager.sDataAliasRest + "s_rajon sr, " +
                           Points.Pref + DBManager.sDataAliasRest + "s_town st " +
                           " where a.nzp_dom=d.nzp_dom " +
                           "       and d.nzp_ul=su.nzp_ul " +
                           "       and su.nzp_raj=sr.nzp_raj " +
                           "       and sr.nzp_town=st.nzp_town " +
                           " order by ulica, idom, ndom, nkor");


                localTable = DBManager.ExecSQLToTable(connDB, sql.ToString());


            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("!!! Отчет по ГВС " + ex.Message, MonitorLog.typelog.Error, true);
                ret = Utils.InitReturns();
                ret.result = false;
                return null;
            }
            finally
            {

                DBManager.ExecSQL(connDB, "drop table t_svod_water", false);
                DBManager.ExecSQL(connDB, "drop table sel_kvar_water", false);
                connDB.Close();
            }

            ret = Utils.InitReturns();

            return localTable;
        }


        /// <summary>
        /// Подсчет одного банка
        /// </summary>
        /// <param name="prm"></param>
        /// <param name="pref"></param>
        /// <param name="sqlsCr"></param>
        /// <param name="sql"></param>
        private void CalcOneBank(Prm prm, string pref, string sqlsCr, StringBuilder sql)
        {
            string sChargeAlias = pref + "_charge_" +
                                  prm.year_.ToString(CultureInfo.InvariantCulture).Substring(2, 2) +
                                  DBManager.tableDelimiter;
            string sChargeTable = "charge_" + prm.month_.ToString("00");


            PrepareBankSvodTable(prm, pref, sqlsCr, sChargeAlias, sChargeTable);

            SetGvsNorm(prm, pref);

            PrepareTempKvar(pref);

            MakeTempCharges(prm, pref, sChargeAlias, sChargeTable);

            PrepareReval(prm, pref, sChargeAlias);

            CalcServRashod(prm, pref);

            SetServTarif();

            CalcFinalVolume(prm);

            ////Перерасчеты
            //sql.Remove(0, sql.Length);
            //sql.Append(" UPDATE t_charges set reval = (SELECT reval from(SELECT nzp_dom, nzp_serv, sum(reval) as reval from " + sChargeAlias + ".reval_" + prm.month_.ToString("00"));
            //sql.Append(" a INNER JOIN " + pref + "_data.kvar b on b.nzp_kvar = a.nzp_kvar group by 1, 2) t ");
            //sql.Append(" where t_charges.nzp_serv = t.nzp_serv and t_charges.nzp_dom = t.nzp_dom)");

            //if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
            //    throw new Exception();

            ////Перерасчеты
            //sql.Remove(0, sql.Length);
            //sql.Append(" UPDATE t_charges set reval = (SELECT reval from(SELECT nzp_dom, nzp_serv, sum(reval) as reval from " + sChargeAlias + ".reval_" + prm.month_.ToString("00"));
            //sql.Append(" a INNER JOIN " + pref + "_data.kvar b on b.nzp_kvar = a.nzp_kvar group by 1, 2) t ");
            //sql.Append(" where t_charges.nzp_serv = t.nzp_serv and t_charges.nzp_dom = t.nzp_dom)");

            //if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
            //    throw new Exception();



            sql.Remove(0, sql.Length);
#if PG
            sql.Append("  update t_local_water set " +
                       " rsum_tarif = (select sum(a.rsum_tarif)+sum(case when nzp_serv>500 and c_calc<0 then Round(c_calc*tarif,2) else 0 end) from t_charges a where t_local_water.nzp_dom=a.nzp_dom ) , " +
                       //" rsum_tarif = (select sum(a.rsum_tarif)+sum(case when nzp_serv>500 and c_calc<0 then Round(c_calc*tarif,2) else 0 end) from t_charges a where t_local_water.nzp_dom=a.nzp_dom ) , " +
                       " rsum_tarif_odn = (select sum(case when nzp_serv>500 and nzp_serv < 1010052 then Round(c_calc*tarif,2) else 0 end) from t_charges a where t_local_water.nzp_dom=a.nzp_dom ) ,  " +
                       //" sum_nedop = coalesce((select sum(a.sum_nedop) from t_charges a where t_local_water.nzp_dom=a.nzp_dom ) , 0) + coalesce((select sum(a.sum_nedop) from t_charges_saldo0 a where t_local_water.nzp_dom=a.nzp_dom ) , 0),  " +
                       " sum_nedop = coalesce((select sum(a.sum_nedop) from t_charges a where t_local_water.nzp_dom=a.nzp_dom ) , 0),  " +
                       //" sum_nedop_odn = coalesce((select sum(case when nzp_serv>500 then a.sum_nedop else 0 end) from t_charges a where t_local_water.nzp_dom=a.nzp_dom ), 0) + coalesce((select sum(case when nzp_serv>500 then a.sum_nedop else 0 end) from t_charges_saldo0 a where t_local_water.nzp_dom=a.nzp_dom ), 0),  " +
                       " sum_nedop_odn = coalesce((select sum(case when nzp_serv>500 then a.sum_nedop else 0 end) from t_charges a where t_local_water.nzp_dom=a.nzp_dom ), 0) ,  " +
                       " reval = coalesce((select sum(a.reval) from t_charges a where t_local_water.nzp_dom=a.nzp_dom ), 0) + coalesce((select sum(a.reval) from t_charges_saldo0 a where t_local_water.nzp_dom=a.nzp_dom ), 0),  " +
                       " sum_charge = coalesce((select coalesce(sum(a.rsum_tarif),0) + coalesce(sum(a.reval),0) + coalesce(sum(case when nzp_serv>500 and c_calc<0 then Round(c_calc*tarif,2) else 0 end),0) from t_charges a where t_local_water.nzp_dom=a.nzp_dom ), 0) + coalesce((select coalesce(sum(a.reval),0)  from t_charges_saldo0 a where t_local_water.nzp_dom=a.nzp_dom ), 0)   " +
                       " where nzp_dom in (select nzp_dom from t_charges group by 1)");
#else
            sql.Append("  update t_local_water set (rsum_tarif, rsum_tarif_odn, sum_nedop, " +
                       " sum_nedop_odn, reval, sum_charge)" +
                       " =((select  sum(a.rsum_tarif), " +
                       " sum(case when nzp_serv>500 then Round(c_calc*tarif,2) else 0 end), " +
                       " sum(a.sum_nedop), " +
                       " sum(case when nzp_serv>500 then a.sum_nedop else 0 end), " +
                       " sum(a.reval),sum(a.sum_charge) " +
                       " from t_charges a where t_local_water.nzp_dom=a.nzp_dom))" +
                       " where nzp_dom in (select nzp_dom from t_charges group by 1)");
#endif
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();

            if (prm.nzp_serv == 513)
            {
                sql.Remove(0, sql.Length);
                sql.Append("  update t_local_water set " +
                           " rsum_tarif = rsum_tarif - (select sum(rsum_tarif) from t_charges_odn a where t_local_water.nzp_dom=a.nzp_dom ) " +
                           " where nzp_dom in (select nzp_dom from t_charges_odn group by 1)");

                if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                    throw new Exception();
            }

            sql.Remove(0, sql.Length);
            sql.Append("  update t_local_water set " +
                          " sum_charge = sum_charge - sum_nedop");
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();

            sql.Remove(0, sql.Length);

#if PG
            sql.Append("  update t_local_water set " +
                       " count_gil_ipu = (select sum(case when is_device=1 then gil1 else 0 end) " +
                       " from " + sChargeAlias + ".counters_" + prm.month_.ToString("00") + " a, t_kvars t" +
                       " where a.nzp_dom =t_local_water.nzp_dom and nzp_type=3 and stek=3 " +
                       " and nzp_serv=9 and a.nzp_kvar=t.nzp_kvar ) , " +
                       " count_gil_npu = (select sum(case when is_device=1 then 0 else gil1 end) " +
                       " from " + sChargeAlias + ".counters_" + prm.month_.ToString("00") + " a, t_kvars t " +
                       " where a.nzp_dom =t_local_water.nzp_dom and nzp_type=3 and stek=3 " +
                       " and nzp_serv=9 and a.nzp_kvar=t.nzp_kvar ) ");

#else
            sql.Append("  update t_local_water set (count_gil_ipu, count_gil_npu)= " +
                       " ((select  sum(case when t.is_device=1 then gil1 else 0 end), " +
                       "  sum(case when t.is_device=1 then 0 else gil1 end)  " +
                       " from " + sChargeAlias + "counters_" + prm.month_.ToString("00") + " a, t_kvars t" +
                       " where a.nzp_dom =t_local_water.nzp_dom and nzp_type=3 and stek=3 " +
                       " and nzp_serv=9 and a.nzp_kvar=t.nzp_kvar ))");
#endif

            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();

            CalcDomRashod(prm, pref, sChargeAlias);

            SetSectionForDom(pref);

            sql.Remove(0, sql.Length);
            sql.Append(
                "  insert into t_svod_water(nzp_dom, nzp_dom_base,  count_gil_ipu , count_gil_npu , volume_all_kub  , " +
                " volume_all_gkal , vol_ipu_kub , vol_ipu_gkal , vol_npu_kub , " +
                " vol_npu_gkal , vol_odn_kub , vol_odn_gkal , rsum_tarif  , " +
                " rsum_tarif_odn  ,vozv_kub , vozv_gkal , vozv_odn_kub , " +
                " vozv_odn_gkal ,sum_nedop ,  sum_nedop_odn  , " +
                " reval_kub , reval_gkal , reval  , vol_charge_kub , " +
                " vol_charge_gkal , sum_charge  , odpu_kub , " +
                " odpu_gkal )" +
                " select   nzp_dom , nzp_dom, " +
                " sum(count_gil_ipu) , " + //количество граждан в ЛС с приборами учета
                " sum(count_gil_npu) , " + //количество граждан в ЛС без приборов учета
                " sum(volume_all_kub)  , " + //объем в кубометрах по дому
                " sum(volume_all_gkal) , " + //объем в Гкал по дому
                " sum(vol_ipu_kub) , " + //Объем по ЛС с ИПУ в куб.м.
                " sum(vol_ipu_gkal) , " + //Объем по ЛС с ИПУ в Гкал
                " sum(vol_npu_kub) , " + //Объем по ЛС без ИПУ в куб.м.
                " sum(vol_npu_gkal) , " + //Объем по ЛС без ИПУ в Гкал
                " sum(vol_odn_kub) , " + //объем ОДН в куб.м.
                " sum(vol_odn_gkal) , " + //объем ОДН в Гкал.
                " sum(rsum_tarif)  , " + //начислено по тарифу по основной услуге
                " sum(rsum_tarif_odn)  , " + //начислено по тарифу по ОДН
                " sum(" + DBManager.sNvlWord + "(vozv_kub,0)+coalesce(vozv_odn_kub,0)) , " + //Недопоставки в куб.м.
                " sum(" + DBManager.sNvlWord + "(vozv_gkal,0)+coalesce(vozv_odn_gkal,0)) , " + //Недопоставки в Гкал.
                " sum(vozv_odn_kub) , " + //Возвраты по недопоставкам по ОДН в куб.м.
                " sum(vozv_odn_gkal) , " + //Возвраты по недопоставкам по ОДН в Гкал.
                " sum(sum_nedop), " + //Сумма недопоставки
                " sum(sum_nedop_odn)  , " + //Сумма недопоставки по ОДН
                " sum(reval_kub) , " + //объем перерасчета в куб.м.
                " sum(reval_gkal) , " + //объем перерасчета в Гкал.
                " sum(reval)  , " + //Сумма перерасчета
                " sum(vol_charge_kub) , " + //Объем начислено к оплате в куб.м.
                " sum(vol_charge_gkal) , " + //Объем начислено к оплате в Гкал
                " sum(sum_charge)  , " + //Сумма начислено к оплате
                " sum(odpu_kub) , " + //Объем предъявленный жильцам в куб. 
                " sum(odpu_gkal) " + //Объем предъявленный жильцам в Гкал
                " from t_local_water group by 1,2 ");
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();

            DBManager.ExecSQL(connDB, "drop table t_local_water", true);
        }

        private void PrepareBankSvodTable(Prm prm, string pref, string sqlsCr, string sChargeAlias,
            string sChargeTable)
        {
            StringBuilder sql = new StringBuilder();
            DBManager.ExecSQL(connDB, "drop table t_local_water", false);

            string sqls = sqlsCr.Replace("t_svod_water", "t_local_water");

            DBManager.ExecSQL(connDB, sqls, true);

            sqls = " insert into t_local_water " +
                   " select * from t_svod_water " +
                   " where nzp_dom=-1 ";
            DBManager.ExecSQL(connDB, sqls, true);

            sql.Remove(0, sql.Length);
            sql.Append(" insert into t_local_water(nzp_dom) ");
            sql.Append(" select nzp_dom from sel_kvar_water ");
            sql.Append(" where pref = '" + pref + "'");
            if (prm.has_pu == "2")
                sql.Append(" and 0 < (select count(*)  " +
                           " from " + sChargeAlias + "counters_" + prm.month_.ToString("00") + " d" +
                           " where stek=3 and nzp_type=1 and cnt_stage>0 " +
                           "and d.nzp_serv=9 and sel_kvar_water.nzp_dom=d.nzp_dom) ");
            if (prm.has_pu == "3")
                sql.Append(" and 0 = (select count(*)  " +
                           " from " + sChargeAlias + "counters_" + prm.month_.ToString("00") + " d" +
                           " where stek=3 and nzp_type=1 and cnt_stage>0 " +
                           "and nzp_serv=9 and sel_kvar_water.nzp_dom=d.nzp_dom) ");
            if (prm.nzp_key > -1)
            {
                sql.Append(" and 0<(select count(*) from " + sChargeAlias + sChargeTable + " a ");
                sql.Append(" where a.nzp_kvar=sel_kvar_water.nzp_kvar and ");
                sql.Append(" dat_charge is null ");
                sql.Append(prm.nzp_serv == 513
                    ? " and a.nzp_serv in (9,14,414,518) and a.nzp_supp=" + prm.nzp_key + ")"
                    : " and a.nzp_serv in (9,14,414,518,513,514,1010052,1010053) and a.nzp_supp=" + prm.nzp_key + ")");
            }
            sql.Append(" group by 1 ");

            DBManager.ExecSQL(connDB, sql.ToString(), true);
        }


        /// <summary>
        /// Выборка начислений
        /// </summary>
        /// <param name="prm"></param>
        /// <param name="pref"></param>
        /// <param name="sChargeAlias"></param>
        /// <param name="sChargeTable"></param>
        private void MakeTempCharges(Prm prm, string pref, string sChargeAlias, string sChargeTable)
        {
            StringBuilder sql = new StringBuilder();
            DBManager.ExecSQL(connDB, "drop table t_charges", false);
            DBManager.ExecSQL(connDB, "drop table t_charges_saldo0", false);
            if(prm.nzp_serv == 513)
                DBManager.ExecSQL(connDB, "drop table t_charges_odn", false);

            string sqls = " Create temp table t_charges (" +
                          " nzp_kvar integer," +
                          " nzp_dom integer," +
                          " nzp_serv integer," +
                          " nzp_measure integer," +
                          " nzp_frm integer," +
                          " is_device integer," +
                          " norma " + DBManager.sDecimalType + "(14,2)," +
                          " tarif " + DBManager.sDecimalType + "(14,2)," +
                          " rsum_tarif " + DBManager.sDecimalType + "(14,2)," +
                          " sum_real " + DBManager.sDecimalType + "(14,2)," +
                          " sum_nedop " + DBManager.sDecimalType + "(14,2)," +
                          " c_calc " + DBManager.sDecimalType + "(14,5) default 0," +
                          " reval " + DBManager.sDecimalType + "(14,2)," +
                          " sum_charge " + DBManager.sDecimalType + "(14,2)" +
                          ")" + DBManager.sUnlogTempTable;
            if (!DBManager.ExecSQL(connDB, sqls, true).result)
                throw new Exception();

            if (prm.nzp_serv == 513)
            {
                sqls = " Create temp table t_charges_odn (" +
                          " nzp_kvar integer," +
                          " nzp_dom integer," +
                          " nzp_serv integer," +
                          " nzp_measure integer," +
                          " nzp_frm integer," +
                          " is_device integer," +
                          " norma " + DBManager.sDecimalType + "(14,2)," +
                          " tarif " + DBManager.sDecimalType + "(14,2)," +
                          " rsum_tarif " + DBManager.sDecimalType + "(14,2)," +
                          " sum_real " + DBManager.sDecimalType + "(14,2)," +
                          " sum_nedop " + DBManager.sDecimalType + "(14,2)," +
                          " c_calc " + DBManager.sDecimalType + "(14,5) default 0," +
                          " reval " + DBManager.sDecimalType + "(14,2)," +
                          " sum_charge " + DBManager.sDecimalType + "(14,2)" +
                          ")" + DBManager.sUnlogTempTable;
                if (!DBManager.ExecSQL(connDB, sqls, true).result)
                    throw new Exception();
            }
            

            sqls = " Create temp table t_charges_saldo0 (" +
                          " nzp_kvar integer," +
                          " nzp_dom integer," +
                          " nzp_serv integer," +
                          " nzp_measure integer," +
                          " nzp_frm integer," +
                          " is_device integer," +
                          " norma " + DBManager.sDecimalType + "(14,2)," +
                          " tarif " + DBManager.sDecimalType + "(14,2)," +
                          " rsum_tarif " + DBManager.sDecimalType + "(14,2)," +
                          " sum_real " + DBManager.sDecimalType + "(14,2)," +
                          " sum_nedop " + DBManager.sDecimalType + "(14,2)," +
                          " c_calc " + DBManager.sDecimalType + "(14,5) default 0," +
                          " reval " + DBManager.sDecimalType + "(14,2)," +
                          " sum_charge " + DBManager.sDecimalType + "(14,2)" +
                          ")" + DBManager.sUnlogTempTable;
            if (!DBManager.ExecSQL(connDB, sqls, true).result)
                throw new Exception();


            sqls = " insert into t_charges (nzp_dom , nzp_kvar, nzp_serv, norma, " +
                   " nzp_measure, nzp_frm , tarif, is_device, rsum_tarif, " +
                   " sum_real, reval, sum_nedop, sum_charge) " +
                   " select  k.nzp_dom , a.nzp_kvar, a.nzp_serv, k.norma, " +
                   DBManager.sNvlWord + "(f.nzp_measure,4) as nzp_measure, a.nzp_frm , " +
                   " a.tarif,k.is_device," +
                   " sum(a.rsum_tarif) as rsum_tarif, " +
                   " sum(a.sum_real) as sum_real, " +
                   " sum(a.reval+real_charge) as reval, " +
                   " sum(a.sum_nedop) as sum_nedop   , " +
                   " sum(a.sum_charge) as sum_charge " +
                   " from t_kvars k ," + sChargeAlias + sChargeTable + " a  " +
                   " left outer join " + pref + DBManager.sKernelAliasRest + "formuls f " +
                   " on a.nzp_frm =f.nzp_frm " +
                   (prm.nzp_prm == 513 ?
                   " where k.nzp_kvar = a.nzp_kvar and a.nzp_serv in (9,14) " :
                   " where k.nzp_kvar = a.nzp_kvar and a.nzp_serv in (9,14,414,518,513,514,1010052,1010053) ") +
                   " and dat_charge is null";

            if (prm.nzp_key > -1)
            {
                sqls += " and a.nzp_supp=" + prm.nzp_key;
            }

            sqls += "  group by 1, 2, 3 ,4 ,5  ,6, 7,8";
            if (!DBManager.ExecSQL(connDB, sqls, true).result)
                throw new Exception();



            sqls = " insert into t_charges_saldo0 (nzp_dom , nzp_kvar, nzp_serv, norma, " +
                   " nzp_measure, nzp_frm , tarif, is_device, rsum_tarif, " +
                   " sum_real, reval, sum_nedop, sum_charge) " +
                   " select  k.nzp_dom , a.nzp_kvar, a.nzp_serv, k.norma, " +
                   DBManager.sNvlWord + "(f.nzp_measure,4) as nzp_measure, a.nzp_frm , " +
                   " a.tarif,k.is_device," +
                   " 0 as rsum_tarif, " +
                   " 0 as sum_real, " +
                   " 0 as reval, " +
                   " 0 as sum_nedop   , " +
                   " 0 as sum_charge " +
                   " from t_kvars k ," + sChargeAlias + sChargeTable + " a  " +
                   " left outer join " + pref + DBManager.sKernelAliasRest + "formuls f " +
                   " on a.nzp_frm =f.nzp_frm " +
                   (prm.nzp_prm == 513 ?
                   " where k.nzp_kvar = a.nzp_kvar and a.nzp_serv in (9,14) " :
                   " where k.nzp_kvar = a.nzp_kvar and a.nzp_serv in (9,14,414,518,513,514,1010052,1010053) ") +
                   " and dat_charge is null";

            if (prm.nzp_key > -1)
            {
                sqls += " and a.nzp_supp=" + prm.nzp_key;
            }

            sqls += "  group by 1, 2, 3 ,4 ,5  ,6, 7,8";
            if (!DBManager.ExecSQL(connDB, sqls, true).result)
                throw new Exception();

            if (prm.nzp_serv == 513)
            {
                sqls = " insert into t_charges_odn (nzp_dom , nzp_kvar, nzp_serv, norma, " +
                   " nzp_measure, nzp_frm , tarif, is_device, rsum_tarif, " +
                   " sum_real, reval, sum_nedop, sum_charge) " +
                   " select  k.nzp_dom , a.nzp_kvar, a.nzp_serv, k.norma, " +
                   DBManager.sNvlWord + "(f.nzp_measure,4) as nzp_measure, a.nzp_frm , " +
                   " a.tarif,k.is_device," +
                   " sum(a.rsum_tarif) as rsum_tarif, " +
                   " 0 as sum_real, " +
                   " 0 as reval, " +
                   " 0 as sum_nedop   , " +
                   " 0 as sum_charge " +
                   " from t_kvars k ," + sChargeAlias + sChargeTable + " a  " +
                   " left outer join " + pref + DBManager.sKernelAliasRest + "formuls f " +
                   " on a.nzp_frm =f.nzp_frm " +
                   " where k.nzp_kvar = a.nzp_kvar and a.nzp_serv in (414,518,513,514,1010052,1010053) " +
                   " and dat_charge is null";

                if (prm.nzp_key > -1)
                {
                    sqls += " and a.nzp_supp=" + prm.nzp_key;
                }

                sqls += "  group by 1, 2, 3 ,4 ,5  ,6, 7,8";
                if (!DBManager.ExecSQL(connDB, sqls, true).result)
                    throw new Exception();
            }

            
        }

        private void SetSectionForDom(string pref)
        {
            StringBuilder sql = new StringBuilder();
            sql.Remove(0, sql.Length);
            sql.Append(" update t_local_water set nzp_dom = (select nzp_dom_base " +
                       "from " + pref + DBManager.sDataAliasRest + "link_dom_lit a");
            sql.Append(" where a.nzp_dom=t_local_water.nzp_dom) ");
            sql.Append(" where nzp_dom in (select nzp_dom " +
                       "from " + pref + DBManager.sDataAliasRest + "link_dom_lit)");
            DBManager.ExecSQL(connDB, sql.ToString(), true);
        }


        /// <summary>
        /// Подсчет расходов по дому
        /// </summary>
        /// <param name="prm"></param>
        /// <param name="pref"></param>
        /// <param name="sChargeAlias"></param>
        private void CalcDomRashod(Prm prm, string pref, string sChargeAlias)
        {
            StringBuilder sql = new StringBuilder();
            sql.Remove(0, sql.Length);
            sql.Append(" select a.nzp_dom, s.nzp_measure, sum(case when stek=1 then val1 else 0 end) as dpu1, ");
            sql.Append(" sum(case when stek=2 then val1 else 0 end) as dpu2 ");
#if PG
            sql.Append("  into temp t_dom_pok from " + sChargeAlias + "counters_" + prm.month_.ToString("00") + " a, ");
            sql.Append(pref + "_data.counters_spis cs,  " + pref + "_kernel.s_counts s, t_local_water t ");
            sql.Append(" where  dat_charge is null and a.nzp_dom=t.nzp_dom ");
            sql.Append(" and stek in (1,2) and a.nzp_type=1 and a.nzp_serv=s.nzp_serv and a.nzp_serv=9 ");
            sql.Append(" and a.nzp_counter=cs.nzp_counter and cs.nzp_cnt=s.nzp_cnt ");
            sql.Append(" group by 1,2 ");
#else
            sql.Append(" from " + sChargeAlias + "counters_" + prm.month_.ToString("00") + " a, ");
            sql.Append(pref + "_data:counters_spis cs,  " + pref + "_kernel:s_counts s, t_local_water t ");
            sql.Append(" where  dat_charge is null and a.nzp_dom=t.nzp_dom ");
            sql.Append(" and stek in (1,2) and a.nzp_type=1 and a.nzp_serv=s.nzp_serv and a.nzp_serv=9 ");
            sql.Append(" and a.nzp_counter=cs.nzp_counter and cs.nzp_cnt=s.nzp_cnt ");
            sql.Append(" group by 1,2 into temp t_dom_pok with no log   ");
#endif

            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();


            sql.Remove(0, sql.Length);
            sql.Append(" update t_local_water set koef_gv = (select max(val_prm" + DBManager.sConvToNum + ") ");
            sql.Append(" from " + pref + DBManager.sDataAliasRest + "prm_2 ");
            sql.Append(" where t_local_water.nzp_dom=nzp and nzp_prm=436");

            sql.Append(" and is_actual=1 ");
            sql.Append(" and dat_s<=date('01." + prm.month_.ToString("00") + "." + prm.year_.ToString("0000") +
                       "')");
            sql.Append(" and dat_s>=date('01." + prm.month_.ToString("00") + "." + prm.year_.ToString("0000") +
                       "')");
            sql.Append(" ) ");
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();

            sql.Remove(0, sql.Length);
            sql.Append(
                " update t_local_water set odpu_gkal = (select sum(case when dpu1>0 then dpu1 else dpu2 end) ");
            sql.Append(" from t_dom_pok where t_local_water.nzp_dom=t_dom_pok.nzp_dom");
            sql.Append(" and nzp_measure=4) ");
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();

            sql.Remove(0, sql.Length);
            sql.Append(
                " update t_local_water set odpu_kub = (select sum(case when dpu1>0 then dpu1 else dpu2 end) ");
            sql.Append(" from t_dom_pok where t_local_water.nzp_dom=t_dom_pok.nzp_dom");
            sql.Append(" and nzp_measure=3) ");
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();

            DBManager.ExecSQL(connDB, "drop table t_dom_pok", true);

            sql.Remove(0, sql.Length);
            sql.Append(" update t_local_water set odpu_gkal = round(odpu_kub*koef_gv,4)*tarif_gkal ");
            sql.Append(" where " + DBManager.sNvlWord + "(odpu_gkal,0) = 0");
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();
        }

        private void CalcFinalVolume(Prm prm)
        {
            //Общий объем
            StringBuilder sql = new StringBuilder();
            sql.Remove(0, sql.Length);
            sql.Append(" update t_local_water set volume_all_kub=" +
                       DBManager.sNvlWord + "((select  coalesce(sum(a.c_calc), 0)  " +
                       " from t_charges a " +
                       " where t_local_water.nzp_dom=a.nzp_dom " +
                       "  and nzp_serv in (14) ),0)");
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();

            sql.Remove(0, sql.Length);
            sql.Append(" update t_local_water set volume_all_gkal=" +
                       DBManager.sNvlWord + "((select  coalesce(sum(a.c_calc), 0)  " +
                       " from t_charges a " +
                       " where t_local_water.nzp_dom=a.nzp_dom " +
                       (prm.nzp_serv == 513 ? " and nzp_serv in (9) ),0)" : " and nzp_serv in (9, 513, 1010052) ),0)"));
                       //" and nzp_serv in (9) ),0)"); вата
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();

            sql.Remove(0, sql.Length);
            sql.Append(" update t_local_water set vol_odn_kub = " +
                       DBManager.sNvlWord + "((select  sum(a.c_calc)  " +
                       " from t_charges a " +
                       " where t_local_water.nzp_dom=a.nzp_dom and abs(rsum_tarif)>0.001 " +
                       (prm.nzp_serv == 513
                           ? " and nzp_serv in (0)),0)"
                           : " and nzp_serv in (514, 518, 1010053)),0)"));
                       //" and nzp_serv in (514, 518)),0)"); вата
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();


            //объем по лицевым счетам без ОДН
            sql.Remove(0, sql.Length);
            sql.Append(" update t_local_water set vol_ipu_kub= " +
                       " (select  coalesce(sum(a.c_calc), 0) " +
                       " from t_charges a where t_local_water.nzp_dom=a.nzp_dom " +
                       " and a.is_device = 1 and nzp_serv in (14) " +
                       " )");
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();

            sql.Remove(0, sql.Length);
            sql.Append(" update t_local_water set vol_ipu_gkal= " +
                       " (select  coalesce(sum(c_calc), 0) " +
                       " from t_charges a where t_local_water.nzp_dom=a.nzp_dom " +
                       " and a.is_device = 1 and nzp_serv = 9 " +
                       "  )");
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();

            sql.Remove(0, sql.Length);
            sql.Append(" update t_local_water  set vol_npu_kub=" +
                       " (select  coalesce(sum(a.c_calc ), 0) " +
                       " from t_charges a where t_local_water.nzp_dom=a.nzp_dom " +
                       " and (a.is_device=0 or a.is_device is null)  and nzp_serv in (14,414))");
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();


            sql.Remove(0, sql.Length);
            sql.Append(" update t_local_water set vol_npu_gkal= " +
                       " (select  coalesce(sum(c_calc), 0) " +
                       " from t_charges a where t_local_water.nzp_dom=a.nzp_dom " +
                       " and (a.is_device=0 or a.is_device is null) and nzp_serv = 9)");
            //" and (a.is_device=0 or a.is_device is null) and nzp_serv = 9 and tarif_gkal>0 )");
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();

            sql.Remove(0, sql.Length);
            sql.Append(" update t_local_water set vol_odn_gkal= " +
                       " (select  coalesce(sum(case when nzp_measure=4 then c_calc " +
                       "  when nzp_measure<>4 and tarif_gkal>0 and abs(rsum_tarif)>0.001 then rsum_tarif/tarif_gkal else 0 end), 0) " +
                       " from t_charges a where t_local_water.nzp_dom=a.nzp_dom " +
                       (prm.nzp_serv == 513 ? "  and nzp_serv in (0))" : "  and nzp_serv in (513))"));
                       //"  and nzp_serv in (0))"); вата
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();

            sql.Remove(0, sql.Length);
            sql.Append(" update t_local_water  set vol_odn_kub=" +
                       " (select  coalesce(sum(case when a.nzp_measure=2 " +
                       " then norma*a.c_calc else a.c_calc end ), 0) " +
                       " from t_charges a where t_local_water.nzp_dom=a.nzp_dom " +
                       (prm.nzp_serv == 513 ? " and nzp_serv in (0)  )" : " and nzp_serv in (514,518)  )"));
                       //" and nzp_serv in (518)  )"); вата
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();

            sql.Remove(0, sql.Length);
            sql.Append("  update t_local_water set vozv_kub= " +
                       "  (select  coalesce(sum(a.sum_nedop/tarif_kub), 0) " +
                       "  from t_charges a where t_local_water.nzp_dom=a.nzp_dom " +
                       "  and tarif_kub>0 and nzp_serv in (14,414))");
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();


            sql.Remove(0, sql.Length);
            sql.Append("  update t_local_water set vozv_gkal= " +
                       "  (select  sum(a.sum_nedop/tarif_gkal) " +
                       "  from t_charges a where t_local_water.nzp_dom=a.nzp_dom " +
                       "  and tarif_gkal>0 and nzp_serv = 9 )");
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();

            sql.Remove(0, sql.Length);
            sql.Append("  update t_local_water set vozv_odn_kub= " +
                       "  (select  coalesce(sum(a.sum_nedop/tarif_kub), 0) " +
                       "  from t_charges a where t_local_water.nzp_dom=a.nzp_dom " +
                       (prm.nzp_prm == 513
                           ? "  and tarif_kub>0 and nzp_serv in (0))"
                           : "  and tarif_kub>0 and nzp_serv in (514,518,1010053))"));
                       //"  and tarif_kub>0 and nzp_serv in (518))");вата
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();

            sql.Remove(0, sql.Length);
            sql.Append("  update t_local_water set vozv_odn_gkal= " +
                       "  (select  sum(a.sum_nedop/tarif_gkal) " +
                       "  from t_charges a where t_local_water.nzp_dom=a.nzp_dom " +
                       (prm.nzp_prm == 513
                           ? "  and tarif_gkal>0 and nzp_serv in (0))"
                           : "  and tarif_gkal>0 and nzp_serv in (513,1010052))"));
                       //"  and tarif_gkal>0 and nzp_serv in (0))"); вата
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();

            sql.Remove(0, sql.Length);
            sql.Append(" update t_local_water set reval_kub = " +
                       " (select  coalesce(sum(a.reval/tarif_kub), 0) " +
                       " from t_charges a where t_local_water.nzp_dom=a.nzp_dom " +
                       (prm.nzp_prm == 513
                           ? " and tarif_kub > 0 and nzp_serv not in (9) )"
                           : " and tarif_kub > 0 and nzp_serv not in (9,513,1010052) )"));
                       //" and tarif_kub > 0 and nzp_serv not in (9) )"); вата
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();

            sql.Remove(0, sql.Length);
            sql.Append(" update t_local_water set reval_kub = reval_kub + " +
                       " (select  coalesce(sum(a.reval/tarif_kub), 0) " +
                       " from t_charges_saldo0 a where t_local_water.nzp_dom=a.nzp_dom " +
                       (prm.nzp_prm == 513
                           ? " and tarif_kub > 0 and nzp_serv not in (9) )"
                           : " and tarif_kub > 0 and nzp_serv not in (9,513,1010052) )"));
            //" and tarif_kub > 0 and nzp_serv not in (9) )"); вата
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();


            sql.Remove(0, sql.Length);
            sql.Append(" update t_local_water set reval_gkal = " +
                       " (select  coalesce(sum(a.reval/tarif_gkal), 0) " +
                       " from t_charges a where t_local_water.nzp_dom=a.nzp_dom " +
                       (prm.nzp_prm == 513
                           ? " and tarif_gkal > 0 and nzp_serv in (9) )"
                           : " and tarif_gkal > 0 and nzp_serv in (9,513,1010052) )"));
                       //" and tarif_gkal > 0 and nzp_serv in (9) )");вата
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();

            sql.Remove(0, sql.Length);
            sql.Append(" update t_local_water set reval_gkal = reval_gkal + " +
                       " (select  coalesce(sum(a.reval/tarif_gkal), 0) " +
                       " from t_charges_saldo0 a where t_local_water.nzp_dom=a.nzp_dom " +
                       (prm.nzp_prm == 513
                           ? " and tarif_gkal > 0 and nzp_serv in (9) )"
                           : " and tarif_gkal > 0 and nzp_serv in (9,513,1010052) )"));
            //" and tarif_gkal > 0 and nzp_serv in (9) )");вата
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();

            sql.Remove(0, sql.Length);

            sql.Append(" update t_local_water   " +
                       " set vol_charge_kub= " +
                       DBManager.sNvlWord + "(volume_all_kub,0) - " +
                       DBManager.sNvlWord + "(vozv_kub,0) + " +
                       DBManager.sNvlWord + "(vol_odn_kub,0) + " +
                       DBManager.sNvlWord + "(reval_kub,0)  ");
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();


            sql.Remove(0, sql.Length);
            sql.Append(" update t_local_water   " +
                       " set vol_charge_gkal= " +
                       DBManager.sNvlWord + "(volume_all_gkal,0) - " +
                       DBManager.sNvlWord + "(vozv_gkal,0) + " +
                       DBManager.sNvlWord + "(reval_gkal,0)  ");
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();
        }


        /// <summary>
        /// Установка тарифов
        /// </summary>
        private void SetServTarif()
        {
            #region Устанавливаем тарифы
            StringBuilder sql = new StringBuilder();
            sql.Remove(0, sql.Length);
            sql.Append(" update t_local_water set tarif_gkal = " + DBManager.sNvlWord + "(" +
                       " (select coalesce(max(tarif), 0) from t_charges a" +
                       " where a.nzp_serv=9 " +
                       " and t_local_water.nzp_dom=a.nzp_dom  and nzp_measure = 4),0)");
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();


            sql.Remove(0, sql.Length);
            sql.Append(" update t_local_water set tarif_kub = (" +
                       " select coalesce(max(tarif), 0) from t_charges a " +
                       " where nzp_measure=3 and nzp_serv<500 " +
                       " and t_local_water.nzp_dom=a.nzp_dom  )");
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();

            #endregion
        }


        /// <summary>
        /// Подсчет расхода по услугам
        /// </summary>
        /// <param name="prm"></param>
        /// <param name="pref"></param>
        private void CalcServRashod(Prm prm, string pref)
        {
            //Проставляем расход по основным услугам
            StringBuilder sql = new StringBuilder();
            sql.Remove(0, sql.Length);
            sql.Append(" update t_charges set c_calc = (select sum(valm+dlt_reval) ");
            sql.Append(" from  " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") +
                       DBManager.tableDelimiter + "calc_gku_" + prm.month_.ToString("00") + " d ");
            sql.Append(" where d.nzp_kvar=t_charges.nzp_kvar  and d.tarif>0 ");
            sql.Append(" and t_charges.nzp_serv = d.nzp_serv and stek = 3 ");

            if (prm.nzp_key > -1) //Добавляем поставщика
            {
                sql.Append(" and d.nzp_supp = " + prm.nzp_key);
            }
            sql.Append(" ) where nzp_serv in (9,14) and tarif>0");
            DBManager.ExecSQL(connDB, sql.ToString(), true);


            //Проставляем расход по услугам ОДН Подогрев
            sql.Remove(0, sql.Length);
            sql.Append(" update t_charges set c_calc = (select ");
            sql.Append(
                " sum(case when valm+dlt_reval>=0 and dop87<0 and dop87< -valm-dlt_reval then -valm-dlt_reval  ");
            sql.Append("          else dop87 end) ");
            sql.Append(" from  " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") +
                       DBManager.tableDelimiter + "calc_gku_" + prm.month_.ToString("00") + " d ");
            sql.Append(" where d.nzp_kvar=t_charges.nzp_kvar  and d.tarif>0  ");
            sql.Append(" and 9 = d.nzp_serv  and stek = 3 ");

            if (prm.nzp_key > -1) //Добавляем поставщика
            {
                sql.Append(" and d.nzp_supp = " + prm.nzp_key);
            }
            sql.Append((prm.nzp_serv == 513
                ? " ) where nzp_serv in (0)  and tarif>0"
                : " ) where nzp_serv in (513,1010052)  and tarif>0"));
            //sql.Append(" ) where nzp_serv in (0)  and tarif>0");вата
            DBManager.ExecSQL(connDB, sql.ToString(), true);


            //Проставляем расход по услугам ОДН Хвс для ГВС
            sql.Remove(0, sql.Length);
            sql.Append(" update t_charges set c_calc = (select ");
            sql.Append(
                " sum(case when valm+dlt_reval>=0 and  dop87<0 and dop87< -valm-dlt_reval then -valm-dlt_reval  ");
            sql.Append("          else dop87 end) ");
            sql.Append(" from  " + pref + "_charge_" + (prm.year_ - 2000).ToString("00") +
                       DBManager.tableDelimiter + "calc_gku_" + prm.month_.ToString("00") + " d ");
            sql.Append(" where d.nzp_kvar=t_charges.nzp_kvar  and d.tarif>0 ");
            sql.Append(" and 14 = d.nzp_serv  and stek = 3 ");

            if (prm.nzp_key > -1) //Добавляем поставщика
            {
                sql.Append(" and d.nzp_supp = " + prm.nzp_key);
            }
            sql.Append((prm.nzp_serv == 513
                ? " ) where nzp_serv in (0)  and tarif>0"
                : " ) where nzp_serv in (514,1010053)  and tarif>0"));
            //sql.Append(" ) where nzp_serv in (0)  and tarif>0");вата
            DBManager.ExecSQL(connDB, sql.ToString(), true);
        }

        private void PrepareTempKvar(string pref)
        {
            string sqls;
            DBManager.ExecSQL(connDB, "drop table t_kvars", false);


            sqls = " Create temp table t_kvars (" +
                   " nzp_kvar integer," +
                   " nzp_dom integer," +
                   " is_device integer," +
                   " norma " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable;
            if (!DBManager.ExecSQL(connDB, sqls, true).result)
                throw new Exception();

            sqls = " insert into t_kvars(nzp_kvar , nzp_dom, norma) " +
                   " select skw.nzp_kvar , d.nzp_dom, d.norma " +
                   " from t_local_water d, sel_kvar_water skw " +
                   " where d.nzp_dom=skw.nzp_dom  ";
            if (!DBManager.ExecSQL(connDB, sqls, true).result)
                throw new Exception();

            sqls = " update t_kvars set is_device =1 " +
                   " where 0<(select count(*) " +
                   " from " + pref + DBManager.sDataAliasRest + "prm_1 " +
                   " where nzp_prm=103 and is_actual=1 " +
                   " and dat_s<=current_date and dat_po>=current_date and t_kvars.nzp_kvar=nzp)";
            DBManager.ExecSQL(connDB, sqls, true);

            DBManager.ExecSQL(connDB, "update t_kvars set is_device =0 WHERE is_device is null", true);

            sqls = "  create index ix_t_kvars on t_kvars (nzp_dom , nzp_kvar)";
            if (!DBManager.ExecSQL(connDB, sqls, true).result)
                throw new Exception();
        }


        /// <summary>
        /// Установка норматива на горячую воду
        /// </summary>
        /// <param name="prm"></param>
        /// <param name="pref"></param>
        private void SetGvsNorm(Prm prm, string pref)
        {
            string sqls;

            #region Устанавливаем норматив на горяую воду

            string nextMonth = new DateTime(prm.year_, prm.month_, 01).AddMonths(1).ToShortDateString();
            string curMonth = new DateTime(prm.year_, prm.month_, 01).ToShortDateString();

            sqls = " update t_local_water set norma = " +
                   "    (select max(value" + DBManager.sConvToNum + ")" +
                   "     from " + pref + DBManager.sKernelAliasRest + "res_values " +
                   "     where nzp_res = " +
                   "            (select  a.val_prm" + DBManager.sConvToInt +
                   "            from " + pref + DBManager.sDataAliasRest + "prm_13 a, " +
                   "                 " + pref + DBManager.sKernelAliasRest + "prm_name b " +
                   "            where a.nzp_prm =177 and a.nzp_prm=b.nzp_prm " +
                   "                and dat_s < Date('" + nextMonth + "')" +
                   "                and dat_po >= date('" + curMonth + "' )) " +
                   "    and  nzp_y= (select  max(val_prm" + DBManager.sConvToInt + ")" +
                   "                 from " + pref + DBManager.sDataAliasRest + "prm_2 " +
                   "                 where nzp_prm=38 " +
                   "                    and nzp=t_local_water.nzp_dom) " +
                   "    and nzp_x=2  ) " +
                   " where  norma is null ";
            if (!DBManager.ExecSQL(connDB, sqls, true).result)
                throw new Exception();

            #endregion
        }


        /// <summary>
        /// Загрузка перерасчетов прошлого периода
        /// </summary>
        /// <param name="prm"></param>
        /// <param name="pref"></param>
        /// <param name="sChargeAlias"></param>
        private void PrepareReval(Prm prm, string pref, string sChargeAlias)
        {
            DBManager.ExecSQL(connDB, "drop table t_nedop", false);
            DBManager.ExecSQL(connDB, "drop table t_sum_nedop", false);

            DBManager.ExecSQL(connDB, "drop table t_sum_nedop_saldo0", false);


            string sqls = " create temp table t_sum_nedop( nzp_dom integer, " +
                                " nzp_kvar integer, is_device integer, " +
                                " tarif Decimal(14,4),nzp_serv integer, " +
                                " rsum_tarif Decimal(14,2), " +
                                " sum_nedop decimal(14,2))" + DBManager.sUnlogTempTable;
            DBManager.ExecSQL(connDB, sqls, true);

            sqls = " create temp table t_sum_nedop_saldo0( nzp_dom integer, " +
                                " nzp_kvar integer, is_device integer, " +
                                " tarif Decimal(14,4),nzp_serv integer, " +
                                " rsum_tarif Decimal(14,2), " +
                                " sum_nedop decimal(14,2))" + DBManager.sUnlogTempTable;
            DBManager.ExecSQL(connDB, sqls, true);


            var sql = new StringBuilder();
            sql.Remove(0, sql.Length);
#if PG
            sql.Append(" select b.nzp_dom, a.nzp_kvar,is_device, min(date_part('year',dat_s)*12+date_part('month',dat_s)) as month_s,  max(date_part('year',dat_po)*12+date_part('month',dat_po)) as month_po");
            sql.Append(" into temp t_nedop from " + pref + "_data.nedop_kvar a, t_kvars b ");
            sql.Append(" where a.nzp_kvar=b.nzp_kvar and month_calc='01." + prm.month_.ToString("00") + "." +
                       (prm.nzp_serv == 513
                           ? prm.year_.ToString("0000") + "' and a.nzp_serv in (9,14,414,518) "
                           : prm.year_.ToString("0000") + "' and a.nzp_serv in (9,14,414,518,513,514,1010052,1010053) "));
            sql.Append(" group by 1,2,3 ");
#else
            sql.Append(
                " select b.nzp_dom, a.nzp_kvar, is_device, min(year(dat_s)*12+month(dat_s)) as month_s,  max(year(dat_po)*12+month(dat_po)) as month_po");
            sql.Append(" from " + pref + "_data:nedop_kvar a, t_kvars b ");
            sql.Append(" where a.nzp_kvar=b.nzp_kvar and month_calc='01." + prm.month_.ToString("00") + "." +
                       prm.year_.ToString("0000") + "' and a.nzp_serv in (9,14,414,513,514,518) ");
            sql.Append(" group by 1,2,3 into temp t_nedop with no log");
#endif
            if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                throw new Exception();


            sql.Remove(0, sql.Length);
            sql.Append(" select month_, year_ ");
            sql.Append(" from " + sChargeAlias + "lnk_charge_" +
                       prm.month_.ToString("00") + " b, t_nedop d ");
            sql.Append(
                " where  b.nzp_kvar=d.nzp_kvar and year_*12+month_>=month_s and  year_*12+month_<=month_po");
            sql.Append(" group by 1,2");
            MyDataReader reader2;
            if (!DBManager.ExecRead(connDB, out reader2, sql.ToString(), true).result)
                throw new Exception();
            while (reader2.Read())
            {
                string sTmpAlias = pref + "_charge_" +
                                   (Int32.Parse(reader2["year_"].ToString()) - 2000).ToString("00") +
                                   DBManager.tableDelimiter;

                sql.Remove(0, sql.Length);
                sql.Append(
                    " insert into t_sum_nedop (nzp_kvar,is_device,tarif, nzp_dom, nzp_serv, rsum_tarif, sum_nedop) ");
                sql.Append(" select b.nzp_kvar,d.is_device,0,nzp_dom, nzp_serv, sum(0), ");
                sql.Append(" sum(sum_nedop-sum_nedop_p) ");
                sql.Append(" from " + sTmpAlias + "charge_" +
                           Int32.Parse(reader2["month_"].ToString()).ToString("00"));
                sql.Append(" b, t_nedop d ");
                sql.Append(" where  b.nzp_kvar=d.nzp_kvar and dat_charge = date('28.");
                sql.Append(prm.month_.ToString("00") + "." + prm.year_ + "')");
                sql.Append(" and abs(sum_nedop)+abs(sum_nedop_p)>0.001");
                //sql.Append(" and nzp_serv in (9,14,414,518) "); вата
                sql.Append((prm.nzp_serv == 513
                    ? " and nzp_serv in (9,14,414,518) "
                    : " and nzp_serv in (9,14,414,518,513,514,1010052,1010053) "));
                if (prm.nzp_key > -1)
                {
                    sql.Append(" and nzp_supp=" + prm.nzp_key);
                }
                sql.Append(" group by 1,2,3,4,5");
                if (!DBManager.ExecSQL(connDB, sql.ToString(), true).result)
                    throw new Exception();
            }
            reader2.Close();
            DBManager.ExecSQL(connDB, "drop table t_nedop", true);

            #region Жигулевск недопоставка как перекидка


            sqls = "insert into t_sum_nedop (nzp_kvar,is_device, tarif, nzp_dom, " +
                  " nzp_serv, rsum_tarif, sum_nedop ) " +
                  " select t1.nzp_kvar, t1.is_device, 0, t1.nzp_dom, nzp_serv, 0, -sum(sum_rcl) " +
                  " from " + sChargeAlias + "perekidka a INNER JOIN fbill_data.document_base d on d.nzp_doc_base = a.nzp_doc_base, t_kvars t1 " +
                  " where a.nzp_kvar=t1.nzp_kvar and type_rcl = 101 and d.comment != 'Выравнивание сальдо' " +
                  " and month_=" + prm.month_.ToString("00") +
                  " and abs(sum_rcl)>0.001 and nzp_serv in (9,14) ";
            if (prm.nzp_key > -1) //Добавляем поставщика
            {
                sqls = sqls + " and a.nzp_supp = " + prm.nzp_key;
            }
            sqls = sqls + " group by 1,2,3,4,5,6 ";
            DBManager.ExecSQL(connDB, sqls, true);

            sqls = "insert into t_sum_nedop_saldo0 (nzp_kvar,is_device, tarif, nzp_dom, " +
                   " nzp_serv, rsum_tarif, sum_nedop ) " +
                   " select t1.nzp_kvar, t1.is_device, 0, t1.nzp_dom, nzp_serv, 0, -sum(sum_rcl) " +
                   " from " + sChargeAlias +
                   "perekidka a INNER JOIN fbill_data.document_base d on d.nzp_doc_base = a.nzp_doc_base, t_kvars t1 " +
                   " where a.nzp_kvar=t1.nzp_kvar and type_rcl = 102 and d.comment = 'Выравнивание сальдо' " +
                   " and month_=" + prm.month_.ToString("00") +
                   (prm.nzp_serv == 513
                       ? " and abs(sum_rcl)>0.001 and nzp_serv in (9,14) "
                       : " and abs(sum_rcl)>0.001 and nzp_serv in (9,14,513,514,1010052,1010053) ");
                  
            if (prm.nzp_key > -1) //Добавляем поставщика
            {
                sqls = sqls + " and a.nzp_supp = " + prm.nzp_key;
            }
            sqls = sqls + " group by 1,2,3,4,5,6 ";
            DBManager.ExecSQL(connDB, sqls, true);

            #endregion

            DBManager.ExecSQL(connDB, "drop table t_maxfrm", false);

            sql.Remove(0, sql.Length);
#if PG
            sql.Append(" select nzp_kvar, nzp_serv, max(nzp_frm) as nzp_frm into temp t_maxfrm from t_charges group by 1,2 ");
#else
            sql.Append(" select nzp_kvar, nzp_serv, max(nzp_frm) as nzp_frm  " +
                       " from t_charges group by 1,2 ");
            sql.Append(" into temp t_maxfrm with no log ");
#endif
            DBManager.ExecSQL(connDB, sql.ToString(), true);

            DBManager.ExecSQL(connDB, "create index ix_tmp_m01 on t_maxfrm (nzp_kvar, nzp_serv, nzp_frm)", true);
            DBManager.ExecSQL(connDB, DBManager.sUpdStat + " t_maxfrm", true);

            sql.Remove(0, sql.Length);


            sql.Append(" update t_charges set sum_nedop=" + DBManager.sNvlWord + "(sum_nedop,0)+" +
                       DBManager.sNvlWord + "((select coalesce(sum(sum_nedop),0) ");
            sql.Append(" from t_sum_nedop where t_charges.nzp_kvar=t_sum_nedop.nzp_kvar ");
            sql.Append(" and  t_charges.nzp_serv=t_sum_nedop.nzp_serv),0)  ");
            sql.Append(" where exists (select 1 from t_maxfrm a ");
            sql.Append(" where  t_charges.nzp_kvar=a.nzp_kvar " +
                       " and  t_charges.nzp_serv=a.nzp_serv ");
            sql.Append(" and t_charges.nzp_frm=a.nzp_frm) ");

            DBManager.ExecSQL(connDB, sql.ToString(), true);

            sql.Remove(0, sql.Length);
            sql.Append(" update t_charges set reval=" + DBManager.sNvlWord + "(reval,0)+" +
                       DBManager.sNvlWord + "((select coalesce(sum(sum_nedop),0) ");
            sql.Append(" from t_sum_nedop where t_charges.nzp_kvar=t_sum_nedop.nzp_kvar ");
            sql.Append(" and  t_charges.nzp_serv=t_sum_nedop.nzp_serv),0)  ");
            sql.Append(" where exists (select 1 from t_maxfrm a ");
            sql.Append(" where  t_charges.nzp_kvar=a.nzp_kvar " +
                       " and  t_charges.nzp_serv=a.nzp_serv ");
            sql.Append(" and t_charges.nzp_frm=a.nzp_frm) ");
            DBManager.ExecSQL(connDB, sql.ToString(), true);

            sql.Remove(0, sql.Length);


            sql.Append(" update t_charges_saldo0 set sum_nedop=" + DBManager.sNvlWord + "(sum_nedop,0)+" +
                       DBManager.sNvlWord + "((select coalesce(sum(sum_nedop),0) ");
            sql.Append(" from t_sum_nedop_saldo0 where t_charges_saldo0.nzp_kvar=t_sum_nedop_saldo0.nzp_kvar ");
            sql.Append(" and  t_charges_saldo0.nzp_serv=t_sum_nedop_saldo0.nzp_serv),0)  ");
            sql.Append(" where exists (select 1 from t_maxfrm a ");
            sql.Append(" where  t_charges_saldo0.nzp_kvar=a.nzp_kvar " +
                       " and  t_charges_saldo0.nzp_serv=a.nzp_serv ");
            sql.Append(" and t_charges_saldo0.nzp_frm=a.nzp_frm) ");

            DBManager.ExecSQL(connDB, sql.ToString(), true);

            sql.Remove(0, sql.Length);
            sql.Append(" update t_charges_saldo0 set reval=" + DBManager.sNvlWord + "(reval,0)+" +
                       DBManager.sNvlWord + "((select coalesce(sum(sum_nedop),0) ");
            sql.Append(" from t_sum_nedop_saldo0 where t_charges_saldo0.nzp_kvar=t_sum_nedop_saldo0.nzp_kvar ");
            sql.Append(" and  t_charges_saldo0.nzp_serv=t_sum_nedop_saldo0.nzp_serv),0)  ");
            sql.Append(" where exists (select 1 from t_maxfrm a ");
            sql.Append(" where  t_charges_saldo0.nzp_kvar=a.nzp_kvar " +
                       " and  t_charges_saldo0.nzp_serv=a.nzp_serv ");
            sql.Append(" and t_charges_saldo0.nzp_frm=a.nzp_frm) ");
            DBManager.ExecSQL(connDB, sql.ToString(), true);


            DBManager.ExecSQL(connDB, "drop table t_sum_nedop", true);
            DBManager.ExecSQL(connDB, "drop table t_sum_nedop_saldo0", true);
            DBManager.ExecSQL(connDB, "drop table t_maxfrm", true);

        }

        private string PrepareSvodTable()
        {
            DBManager.ExecSQL(connDB, "drop table t_svod_water", false);
            string result = " Create temp table t_svod_water( " +
                                 " nzp_dom integer, " +
                                 " nzp_dom_base integer, " +
                                 " count_gil_ipu integer, " + //количество граждан в ЛС с приборами учета
                                 " count_gil_npu integer, " + //количество граждан в ЛС без приборов учета
                                 " volume_all_kub " + DBManager.sDecimalType + "(14,4) default 0, " +
                //объем в кубометрах по дому
                                 " volume_all_gkal " + DBManager.sDecimalType + "(14,4), " + //объем в Гкал по дому
                                 " vol_ipu_kub " + DBManager.sDecimalType + "(14,4), " +
                //Объем по ЛС с ИПУ в куб.м.
                                 " vol_ipu_gkal " + DBManager.sDecimalType + "(14,4), " +
                //Объем по ЛС с ИПУ в Гкал
                                 " vol_npu_kub " + DBManager.sDecimalType + "(14,4), " +
                //Объем по ЛС без ИПУ в куб.м.
                                 " vol_npu_gkal " + DBManager.sDecimalType + "(14,4), " +
                //Объем по ЛС без ИПУ в Гкал
                                 " vol_odn_kub " + DBManager.sDecimalType + "(14,4), " + //объем ОДН в куб.м.
                                 " vol_odn_gkal " + DBManager.sDecimalType + "(14,4), " + //объем ОДН в Гкал.
                                 " rsum_tarif  " + DBManager.sDecimalType + "(14,4), " +
                //начислено по тарифу по основной услуге
                                 " rsum_tarif_odn  " + DBManager.sDecimalType + "(14,4), " +
                //начислено по тарифу по ОДН
                                 " vozv_kub " + DBManager.sDecimalType + "(14,4), " + //Недопоставки в куб.м.
                                 " vozv_gkal " + DBManager.sDecimalType + "(14,4), " + //Недопоставки в Гкал.
                                 " vozv_odn_kub " + DBManager.sDecimalType + "(14,4), " +
                //Возвраты по недопоставкам по ОДН в куб.м.
                                 " vozv_odn_gkal " + DBManager.sDecimalType + "(14,4), " +
                //Возвраты по недопоставкам по ОДН в Гкал.
                                 " sum_nedop  " + DBManager.sDecimalType + "(14,4) default 0, " +
                //Сумма недопоставки
                                 " sum_nedop_odn  " + DBManager.sDecimalType + "(14,4), " +
                //Сумма недопоставки по ОДН
                                 " reval_kub " + DBManager.sDecimalType + "(14,4), " + //объем перерасчета в куб.м.
                                 " reval_gkal " + DBManager.sDecimalType + "(14,4), " + //объем перерасчета в Гкал.
                                 " reval  " + DBManager.sDecimalType + "(14,4), " + //Сумма перерасчета
                                 " vol_charge_kub " + DBManager.sDecimalType + "(14,4), " +
                //Объем начислено к оплате в куб.м.
                                 " vol_charge_gkal " + DBManager.sDecimalType + "(14,4), " +
                //Объем начислено к оплате в Гкал
                                 " sum_charge  " + DBManager.sDecimalType + "(14,4), " + //Сумма начислено к оплате
                                 " tarif_gkal " + DBManager.sDecimalType + "(14,4) default 0, " +
                //Тариф на дом на Гкал
                                 " tarif_kub " + DBManager.sDecimalType + "(14,4) default 0, " +
                //Тариф на дом на Куб.
                                 " odpu_kub " + DBManager.sDecimalType + "(14,4) default 0, " +
                //Объем предъявленный жильцам в куб. 
                                 " odpu_gkal " + DBManager.sDecimalType + "(14,4) , " +
                //Объем предъявленный жильцам в Гкал
                                 " koef_gv " + DBManager.sDecimalType + "(14,4) default 0, " +
                                 " norma " + DBManager.sDecimalType + "(14,4)) " + DBManager.sUnlogTempTable;

            if (!DBManager.ExecSQL(connDB, result, true).result)
                throw new Exception("Ошибка создания сводной таблицы"); // return null;

            DBManager.ExecSQL(connDB, "create index ix_tmpww_01 on t_svod_water(nzp_dom)", true);

            return result;
        }


        /// <summary>
        /// Подготовка списка квартир
        /// </summary>
        private DataTable PrepareSelectedKvars(string nzpUser)
        {
            DBManager.ExecSQL(connDB, "drop table sel_kvar_water", false);

            string sqls = " Create temp table sel_kvar_water(" +
                          " nzp_dom integer, " +
                          " nzp_kvar integer, " +
                          " pref char(10)) " + DBManager.sUnlogTempTable;
            if (!DBManager.ExecSQL(connDB, sqls, true).result)
                throw new Exception("Ошибка создания sel_kvar_water"); // return null;


            sqls = " insert into sel_kvar_water(nzp_dom, nzp_kvar, pref)" +
                   " select nzp_dom, nzp_kvar, pref " +
                   " from " + GetSelectedKvars(nzpUser);
            if (!DBManager.ExecSQL(connDB, sqls, true).result)
                throw new Exception("Ошибка создания sel_kvar_water"); // return null;


            sqls = " select pref from sel_kvar_water group by 1 ";

            return DBManager.ExecSQLToTable(connDB, sqls);

        }


        /// <summary>
        /// Получение списка квартир 
        /// </summary>
        /// <param name="Nzp_user"></param>
        /// <returns></returns>
        private string GetSelectedKvars(string Nzp_user)
        {
            IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata);

            Returns ret = DBManager.OpenDb(connWeb, true);
            if (!ret.result)
            {
                throw new Exception("ExcelReport : Ошибка при открытии соединения с БД ");

            }
            string result = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter + "t" + Nzp_user + "_spls";
            connWeb.Close();
            return result;
        }

        private void ConnectToBd()
        {
            connDB = DBManager.GetConnection(Constants.cons_Kernel);
            Returns ret = DBManager.OpenDb(connDB, true);
            if (!ret.result)
            {
                throw new Exception("ExcelReport : Ошибка при открытии соединения с локальной БД ");
            }
        }
    }
}
