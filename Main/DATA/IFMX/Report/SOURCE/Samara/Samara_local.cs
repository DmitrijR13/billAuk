using System;
using System.Data;
using System.Windows.Forms.VisualStyles;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.IFMX.Report.SOURCE.Samara
{
    public class SamaraCalcLocal : SamaraBaseReportClass
    {
        private readonly string _curPref;
        private readonly string _chargeTable;
        private readonly int _year;
        private readonly int _month;

     
       
     
        private void InsertIntoMainFromLocalc()
        {
            string sql = " insert into t_calcreport" +
                         " select * from t_calcreportl";
            RunSql(sql, true);

                   sql = " insert into t_domreport" +
                        " select * from t_domreportl";
            RunSql(sql, true);
        }


        private void MadeLocalTempTable()
        {
            RunSql("drop table tTmpNach", false);
            string sql = " create temp table tTmpNach( " +
                         " nzp_serv integer," +
                         " nzp_supp integer," +
                         " nzp_frm integer," +
                         " nzp_kvar integer," +
                         " is_odn integer," +
                         " tarif " + DBManager.sDecimalType + "(14,4)," +
                         " tarif_gkal " + DBManager.sDecimalType + "(14,4)," +
                         " sum_insaldo " + DBManager.sDecimalType + "(14,4)," +
                         " rsum_tarif " + DBManager.sDecimalType + "(14,4)," +
                         " sum_tarif " + DBManager.sDecimalType + "(14,4)," +
                         " sum_nedop " + DBManager.sDecimalType + "(14,4)," +
                         " sum_nedop_supp " + DBManager.sDecimalType + "(14,4)," +
                         " sum_charge " + DBManager.sDecimalType + "(14,4)," +
                         " reval_d " + DBManager.sDecimalType + "(14,4)," +
                         " reval_k " + DBManager.sDecimalType + "(14,4)," +
                         " real_charge_d " + DBManager.sDecimalType + "(14,4)," +
                         " real_charge_k " + DBManager.sDecimalType + "(14,4)," +
                         " sum_money " + DBManager.sDecimalType + "(14,4)," +
                         " sum_outsaldo " + DBManager.sDecimalType + "(14,4)," +
                         " pl_calc " + DBManager.sDecimalType + "(14,2) default 0," +
                         " full_square " + DBManager.sDecimalType + "(14,2)," +
                         " gil_calc integer," +
                         " is_device integer," +
                         " c_calc " + DBManager.sDecimalType + "(14,4)," +
                         " c_calc_odn " + DBManager.sDecimalType + "(14,4)," +
                         " c_calc_gkal " + DBManager.sDecimalType + "(14,4))" + DBManager.sUnlogTempTable;
            RunSql(sql, true);
            RunSql("create index ix_tcr_01 on tTmpNach(nzp_serv)", true);
            RunSql("update statistics for table tTmpNach", true);

            sql = " create temp table t_localadr ( " +
                         " nzp_area integer," +
                         " nzp_geu integer," +
                         " nzp_dom integer," +
                         " num_ls integer," +
                         " nzp_kvar integer)" + DBManager.sUnlogTempTable;
            RunSql(sql, true);

            sql = " INSERT INTO t_localadr(nzp_area, nzp_geu, nzp_dom, nzp_kvar, num_ls)" +
                  " SELECT nzp_area, nzp_geu, nzp_dom, nzp_kvar, num_ls " +
                  " FROM  " + AdrTempTable + " where pref ='" + _curPref + "'";
            RunSql(sql, true);
            RunSql("create index ix_tlr_01 on t_localadr(nzp_kvar)", true);
            RunSql("create index ix_tlr_02 on t_localadr(nzp_dom)", true);
            RunSql("update statistics for table t_localadr", true);

            sql = " create temp table t_domreportl ( " +
                            " nzp_area integer," +
                            " nzp_geu integer," +
                            " nzp_dom integer," +
                            " nzp_dom_base integer default 0," +
                            " pl_dom " + DBManager.sDecimalType + "(14,4)," +
                            " pl_mop " + DBManager.sDecimalType + "(14,4)) " + DBManager.sUnlogTempTable;
            RunSql(sql, true);

            sql = " INSERT INTO t_domreportl (nzp_dom, nzp_area, nzp_geu) " +
                  " SELECT nzp_dom, nzp_area, nzp_geu " +
                  " from " + AdrTempTable +
                  " WHERE pref='" + _curPref + "' GROUP BY 1,2,3";
            RunSql(sql, true);
            RunSql("create index ix_tld_01 on t_domreportl(nzp_dom)", true);
            RunSql("update statistics for table t_domreportl", true);

        }

        public void LoadNach()
        {
            //Собираем начисления
            string sql = " INSERT INTO tTmpNach (nzp_kvar, nzp_serv," +
                         "        nzp_supp, nzp_frm,  tarif, is_odn, sum_insaldo," +
                         "        rsum_tarif, sum_tarif, sum_nedop, sum_nedop_supp, sum_charge, " +
                         "        reval_d, reval_k, real_charge_d, real_charge_k, sum_money, " +
                         "        sum_outsaldo)" +
                         " SELECT a.nzp_kvar, a.nzp_serv," +
                         "        a.nzp_supp, a.nzp_frm,  a.tarif, 0, sum(sum_insaldo) as sum_insaldo," +
                         "        sum(rsum_tarif) as rsum_tarif, sum(sum_tarif) as sum_tarif, " +
                         "        sum(0) as sum_nedop, sum(0) as sum_nedop_supp, sum(sum_charge) as sum_charge, " +
                         "        sum(case when reval>0 then reval else 0 end) as reval_d," +
                         "        sum(case when reval<0 then reval else 0 end) as reval_k, " +
                         "        sum(case when real_charge>0 then real_charge else 0 end) as real_charge_d," +
                         "        sum(case when real_charge<0 then real_charge else 0 end) as real_charge_k, " +
                         "        sum(sum_money) as sum_money, " +
                         "        sum(sum_outsaldo) as sum_outsaldo" +
                         " FROM " + String.Format(_chargeTable, "charge") + " a, t_localadr b" +
                         " WHERE a.nzp_kvar=b.nzp_kvar " +
                         " GROUP BY 1,2,3,4,5,6 ";
            RunSql(sql, true);

            //Добавляем расходы
            sql = " INSERT INTO tTmpNach (nzp_kvar, nzp_serv," +
                  "        nzp_supp, nzp_frm,  tarif, is_odn, c_calc,c_calc_odn,  " +
                  "        pl_calc, gil_calc, is_device)" +
                  " SELECT a.nzp_kvar, a.nzp_serv," +
                  "        a.nzp_supp, a.nzp_frm,  a.tarif, 0, sum(valm+dlt_reval) as c_calc," +
                  "        sum(case when valm+dlt_reval>=0 and dop87<0 and dop87< -valm-dlt_reval" +
                  "                 then -valm-dlt_reval  " +
                  "                 when valm+dlt_reval<0 and dop87<0 then 0  else dop87 end), " +
                  "        max(squ), max(gil), max(is_device) as is_device" +
                  " FROM " + String.Format(_chargeTable, "calc_gku") + " a, t_localadr b" +
                  " WHERE a.nzp_kvar=b.nzp_kvar and a.nzp_serv not in (select nzp_serv" +
                  " FROM " + Points.Pref + DBManager.sKernelAliasRest + "serv_odn)" +
                  " GROUP BY 1,2,3,4,5,6 ";
            RunSql(sql, true);

            var nedop = new SamaraReportNedop(ConnDb, AdrTempTable);

            nedop.GetNedopByKvar(_curPref, _year, _month);

            TestSql += nedop.TestSql;

            sql = " INSERT INTO tTmpNach (nzp_kvar, nzp_serv, nzp_supp, nzp_frm,  tarif, " +
                  " sum_nedop, sum_nedop_supp, reval_k, reval_d)" +
                  " SELECT nzp_kvar, nzp_serv, nzp_supp, nzp_frm,  tarif, sum(sum_nedop) as sum_nedop," +
                  " sum(case when nzp_supp=nzp_vinovnik then sum_nedop else 0 end), " +
                  " sum(case when sum_nedop_p>0 then sum_nedop_p else 0 end) as reval_k, " +
                  " sum(case when sum_nedop_p>0 then 0 else sum_nedop_p end) as reval_d " +
                  " FROM t_nedop_kvar " +
                  " GROUP BY 1,2,3,4,5 ";
            RunSql(sql, true);
            nedop.Close();

        }


        /// <summary>
        /// Подсчитываем начисления
        /// </summary>
        /// <param name="adrTempTable">Временная таблица, обязатльно наличие поля pref, 
        /// nzp_dom, nzp_area, nzp_geu</param>
        public void SetSumNach(string adrTempTable)
        {
            AdrTempTable = adrTempTable;
        

            MadeLocalTempTable();
            
            SetDomBase();

            LoadNach();

            SetTarifForZeroTarif();

            SetGkalRashod();

            SetRelationServ();

            SetNzpFrmOdn();

            SetKvarSquare();

            SetDomSquare();

            SetSumServ();

            InsertIntoMainFromLocalc();
        }

        private void SetKvarSquare()
        {
            RunSql("drop table t_prm", false);
            string sql = "CREATE TEMP TABLE t_prm(" +
                        " nzp_kvar integer, " +
                        " is_komm integer, " +
                        " full_square " + DBManager.sDecimalType + "(14,2)," + 
                        " gil_square " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable;
            RunSql(sql, true);

            sql = " insert into t_prm( nzp_kvar, is_komm, full_square, gil_square)" +
                  " select nzp_kvar, max(case when nzp_prm=3 and val_prm='2' then 1 else 0 end), " +
                  " max(case when nzp_prm=4 then val_prm" + DBManager.sConvToNum + " else 0 end)," +
                  " max(case when nzp_prm=6 then val_prm" + DBManager.sConvToNum + " else 0 end)" +
                  " from " + _curPref + DBManager.sDataAliasRest+"prm_1 a, t_localadr b " +
                  " where a.nzp = b.nzp_kvar and nzp_prm in (4,6,8) and is_actual=1" +
                  "and dat_s<=today and dat_po>=today " +
                  " group by 1";
            RunSql(sql, true);

            RunSql("create index ixtprm_09 on t_prm(nzp_kvar)", true);
            RunSql("update statistics for table t_prm", true);

            sql = "UPDATE tTmpNach set full_square=(select max(full_square) " +
                  "from t_prm where t_prm.nzp_kvar=tTmpNach.nzp_kvar) where pl_calc>0 ";
            RunSql(sql, true);
            RunSql("drop table t_prm", true);
        }

        private void SetTarifForZeroTarif()
        {
            //1.Проставляем тариф по квартирам
            // выбирая максмальный тариф по услуге в пределах квартиры
            RunSql("drop table t_tarif", false);
            string sql = "CREATE TEMP TABLE t_tarif(" +
                        " nzp_kvar integer, " +
                        " nzp_serv integer, " +
                        " tarif " + DBManager.sDecimalType + "(14,4))" + DBManager.sUnlogTempTable;
            RunSql(sql, true);

            sql = " insert into t_tarif( nzp_kvar, nzp_serv, tarif)" +
                  " select nzp_kvar, nzp_serv, max(tarif) " +
                  " from tTmpNach a " +
                  " where nzp_frm>0 " +
                  " group by 1,2";
            RunSql(sql, true);

            RunSql("create index ixt_09 on t_tarif(nzp_kvar, nzp_serv)", true);
            RunSql("update statistics for table t_tarif", true);

             sql = " update tTmpNach set tarif = (select max(tarif) " +
                  " from t_tarif " +
                  " where tTmpNach.nzp_kvar=t_tarif.nzp_kvar " +
                  "       and tTmpNach.nzp_serv=t_tarif.nzp_serv)" +
                  " where tarif<0.001 and 0<(select count(*) from t_tarif  " +
                  "       where tTmpNach.nzp_kvar=t_tarif.nzp_kvar " +
                  "             and tTmpNach.nzp_serv=t_tarif.nzp_serv)";
             RunSql(sql, true);
             RunSql("drop table t_tarif", true);
            //2.Проставляем тариф по дому
               sql = " CREATE TEMP TABLE t_tarif(" +
                     " nzp_dom integer, " +
                     " nzp_serv integer, " +
                     " tarif " + DBManager.sDecimalType + "(14,4))" + DBManager.sUnlogTempTable;
               RunSql(sql, true);

            sql = " insert into t_tarif( nzp_dom, nzp_serv, tarif)" +
                  " select nzp_dom, nzp_serv, max(tarif) " +
                  " from tTmpNach a, t_localadr b " +
                  " where nzp_frm>0" +
                  "         and a.nzp_kvar=b.nzp_kvar " +
                  " group by 1,2";
            RunSql(sql, true);
            
            RunSql("create index ixt_08 on t_tarif(nzp_dom, nzp_serv)", true);
            RunSql("update statistics for table t_tarif", true);

            sql = " update tTmpNach set tarif = (select max(tarif) " +
                 " from t_tarif a, , t_localadr b " +
                 " where tTmpNach.nzp_kvar=b.nzp_kvar and a.nzp_dom=b.nzp_dom " +
                 "             and tTmpNach.nzp_serv=a.nzp_serv)" +
                 " where tarif<0.001 and 0<(select count(*) from t_tarif a, t_localadr b  " +
                 "       where tTmpNach.nzp_kvar=b.nzp_kvar and a.nzp_dom=b.nzp_dom " +
                 "             and tTmpNach.nzp_serv=a.nzp_serv)";
            RunSql(sql, true);
            RunSql("drop table t_tarif", true);
        }

        private void SetNzpFrmOdn()
        {

            string sql = "UPDATE tTmpNach set is_odn=1 where nzp_serv in (select nzp_serv " +
                  "FROM " + Points.Pref + DBManager.sKernelAliasRest + "serv_odn)";
            RunSql(sql, true);

            RunSql("drop table t_frm", false);
                   sql = "CREATE TEMP TABLE t_frm(" +
                        " nzp_kvar integer, " +
                        " nzp_serv integer, " +
                        " nzp_frm integer, " +
                        " tarif "+DBManager.sDecimalType+"(14,4))" + DBManager.sUnlogTempTable;
            RunSql(sql, true);

            sql = " insert into t_frm( nzp_kvar, nzp_serv, nzp_frm)" +
                  " select a.nzp_kvar, a.nzp_serv, a.nzp_frm " +
                  " from tTmpNach a, "+Points.Pref+DBManager.sKernelAliasRest+"serv_odn b" +
                  " where a.nzp_serv=b.nzp_serv_link and tarif>0 and a.nzp_frm>0" +
                  " group by 1,2,3";
            RunSql(sql, true);
            RunSql("create index ixfrm_09 on t_frm(nzp_kvar,nzp_serv)", true);
            RunSql("update statistics for table t_frm", true);

            sql = " update tTmpNach set nzp_frm= (select max(nzp_frm) " +
                  " from t_frm " +
                  " where tTmpNach.nzp_kvar=t_frm.nzp_kvar " +
                  "       and tTmpNach.nzp_serv=t_frm.nzp_serv)" +
                  " where 0<(select count(*) from t_frm  " +
                  "       where tTmpNach.nzp_kvar=t_frm.nzp_kvar " +
                  "             and tTmpNach.nzp_serv=t_frm.nzp_serv)";
            RunSql(sql, true);
            RunSql("drop table t_frm", true);

        }

        private void SetGkalRashod()
        {
            string sql = " update tTmpNach set c_calc_gkal=c_calc, tarif_gkal=tarif " +
                         " where nzp_serv =9 ";
            RunSql(sql, true);

            sql = " update tTmpNach set c_calc_gkal=c_calc, tarif_gkal=tarif " +
                  " where nzp_serv =513 ";
            RunSql(sql, true);

            sql = " update tTmpNach set c_calc=0, tarif = 0 " +
                  " where nzp_serv in (9,513) ";
            RunSql(sql, true);

        }

        private void SetSumServ()
        {
            //Проставляем базовый дом
            string sql = " UPDATE tTmpNach set nzp_dom =(select nzp_dom_base from t_domreport " +
               "                             WHERE tTmpNach.nzp_dom=t_domreport.nzp_dom )" +
               " WHERE nzp_dom in (select nzp_dom from t_domreport where nzp_dom_base>0)";
            RunSql(sql, true);

            //Собираем начисления
             sql = " INSERT INTO t_calcreportl (nzp_area, nzp_geu, nzp_dom, nzp_serv," +
                         "        nzp_supp, nzp_frm,  tarif, tarif_gkal, sum_insaldo," +
                         "        rsum_tarif, sum_tarif, sum_nedop, sum_nedop_supp, sum_charge, " +
                         "        reval_d, reval_k, real_charge_d, real_charge_k, sum_money, " +
                         "        sum_outsaldo, c_calc,sum_insaldo_odn," +
                         "        rsum_tarif_odn, sum_tarif_odn, sum_nedop_odn, sum_nedop_supp_odn," +
                         "        sum_charge_odn, reval_d_odn, reval_k_odn, real_charge_d_odn, " +
                         "        real_charge_k_odn, sum_money_odn, sum_outsaldo_odn, c_calc_odn, " +
                         "        pl_calc, gil_calc, full_square)" +
                        " SELECT nzp_area, nzp_geu, nzp_dom, nzp_serv," +
                         "        nzp_supp, nzp_frm, tarif, tarif_gkal, " +
                         "        sum(case when is_odn = 0 then sum_insaldo else 0 end) as sum_insaldo," +
                         "        sum(case when is_odn = 0 then rsum_tarif else 0 end) as rsum_tarif, " +
                         "        sum(case when is_odn = 0 then sum_tarif else 0 end) as sum_tarif, " +
                         "        sum(case when is_odn = 0 then sum_nedop else 0 end) as sum_nedop, " +
                         "        sum(case when is_odn = 0 then sum_nedop_supp else 0 end) as sum_nedop_supp, " +
                         "        sum(case when is_odn = 0 then sum_charge else 0 end) as sum_charge, " +
                         "        sum(case when is_odn = 0 then reval_d else 0 end) as reval_d," +
                         "        sum(case when is_odn = 0 then reval_k else 0 end) as reval_k, " +
                         "        sum(case when is_odn = 0 then real_charge_d else 0 end) as real_charge_d," +
                         "        sum(case when is_odn = 0 then real_charge_k else 0 end) as real_charge_k, " +
                         "        sum(case when is_odn = 0 then sum_money else 0 end) as sum_money, " +
                         "        sum(case when is_odn = 0 then sum_outsaldo else 0 end) as sum_outsaldo, " +
                         "        sum(c_calc) as c_calc," +                         
                         "        sum(case when is_odn = 1 then sum_insaldo else 0 end) as sum_insaldo_odn," +
                         "        sum(case when is_odn = 1 then rsum_tarif else 0 end) as rsum_tarif_odn, " +
                         "        sum(case when is_odn = 1 then sum_tarif else 0 end) as sum_tarif_odn, " +
                         "        sum(case when is_odn = 1 then sum_nedop else 0 end) as sum_nedop_odn, " +
                         "        sum(case when is_odn = 1 then sum_nedop_supp else 0 end) as sum_nedop_supp_odn, " +
                         "        sum(case when is_odn = 1 then sum_charge else 0 end) as sum_charge_odn, " +
                         "        sum(case when is_odn = 1 then reval_d else 0 end) as reval_d_odn," +
                         "        sum(case when is_odn = 1 then reval_k else 0 end) as reval_k_odn, " +
                         "        sum(case when is_odn = 1 then real_charge_d else 0 end) as real_charge_d_odn," +
                         "        sum(case when is_odn = 1 then real_charge_k else 0 end) as real_charge_k_odn, " +
                         "        sum(case when is_odn = 1 then sum_money else 0 end) as sum_money_odn, " +
                         "        sum(case when is_odn = 1 then sum_outsaldo else 0 end) as sum_outsaldo_odn, " +
                         "        sum(c_calc_odn ) as c_calc_odn," +
                         "        sum(pl_calc), sum(gil_calc), sum(full_square) "+
                         " FROM  tTmpNach a, t_localadr b" +
                         " WHERE a.nzp_kvar=b.nzp_kvar " +
                         " GROUP BY 1,2,3,4,5,6,7,8 ";
             RunSql(sql, true);
        }

        private void SetRelationServ()
        {
            RunSql("drop table t_gvs", false);
            string sql = "CREATE TEMP TABLE t_gvs(" +
                         " nzp_kvar integer, " +
                         " nzp_serv integer, " +
                         " nzp_supp integer)" + DBManager.sUnlogTempTable;

            RunSql(sql, true);

            sql = " insert into t_gvs( nzp_kvar, nzp_serv, nzp_supp)" +
                  " select nzp_kvar, nzp_serv, nzp_supp " +
                  " from tTmpNach where nzp_serv in (9,513) "+
                  " group by 1,2,3";
            RunSql(sql, true);

            sql = " update tTmpNach set nzp_serv=9 " +
                  " where nzp_serv =14 and 0<(select count(*) " +
                  " from t_gvs " +
                  " where tTmpNach.nzp_kvar=t_gvs.nzp_kvar " +
                  "       AND tTmpNach.nzp_kvar=t_gvs.nzp_kvar " +
                  "       AND t_gvs.nzp_serv = 9 )";
            RunSql(sql, true);

            sql = " update tTmpNach set nzp_serv=513 " +
                  " where nzp_serv =514 and 0<(select count(*) " +
                  " from t_gvs " +
                  " where tTmpNach.nzp_kvar=t_gvs.nzp_kvar " +
                  "       AND tTmpNach.nzp_kvar=t_gvs.nzp_kvar " +
                  "       AND t_gvs.nzp_serv = 513 )";
            RunSql(sql, true);

            RunSql("drop table t_gvs ", true);

        }

        private void SetDomSquare()
        {
            #region Площадь дома

            string sql = " UPDATE t_domreportl SET pl_dom = (SELECT MAX(val_prm" +
                  DBManager.sConvToNum + ") " +
                  "         FROM " + _curPref + DBManager.sDataAliasRest + "prm_2 a " +
                  " WHERE nzp_prm=40 " +
                  "         AND a.nzp=t_domreportl.nzp_dom  " +
                  "         AND is_actual=1  " +
                  "         AND dat_s<=" + DBManager.sCurDate +
                  "         AND dat_po>=" + DBManager.sCurDate + ")";
            RunSql(sql, true);

            #endregion

            #region Площадь мест общего пользования дома

            sql = " UPDATE t_domreportl SET pl_mop = (SELECT MAX(val_prm" +
                  DBManager.sConvToNum + ") " +
                  "         FROM " + _curPref + DBManager.sDataAliasRest + "prm_2 a " +
                  " WHERE nzp_prm=2049 " +
                  "         AND a.nzp=t_domreportl.nzp_dom  " +
                  "         AND is_actual=1  " +
                  "         AND dat_s<=" + DBManager.sCurDate +
                  "         AND dat_po>=" + DBManager.sCurDate + ")";
            RunSql(sql, true);

            #endregion
        }

        private void SetDomBase()
        {

            string sql = " UPDATE t_domreportl SET nzp_dom_base = (SELECT max(nzp_dom_base) " +
                         "         FROM " + _curPref + DBManager.sDataAliasRest + "link_dom_lit a" +
                         "         WHERE a.nzp_dom=t_domreportl.nzp_dom) " +
                         " WHERE nzp_dom in (SELECT nzp_dom " +
                         "         FROM " + _curPref + DBManager.sDataAliasRest + "link_dom_lit)";
            RunSql(sql, true);
        }

        public SamaraCalcLocal(IDbConnection connDb, string pref, int year, int month) :
            base(connDb,"")
        {
            
            _curPref = pref;
            _year = year;
            _month = month;

            _chargeTable = pref + "_charge_" + (year - 2000).ToString("00") +
                      DBManager.tableDelimiter + "{0}_" + month;
        }

     
        public void Close()
        {
             RunSql("drop table  tTmpNach ", true);
             RunSql("drop table  t_localadr ", true);
             RunSql("drop table  t_domreportl ", true);
        }
    }
}
