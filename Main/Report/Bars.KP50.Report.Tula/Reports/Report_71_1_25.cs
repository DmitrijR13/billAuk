using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Base.Parameters;
using Bars.KP50.Report.Tula.Properties;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Tula.Reports
{
    class Report71125 : BaseSqlReport
    {
        /// <summary>Наименование отчета</summary>
        public override string Name {
            get { return "71.1.25 Ежедневный отчет по капремонту"; }
        }

        /// <summary>Описание отчета</summary>
        public override string Description {
            get { return "71.1.25 Ежедневный отчет по капремонту"; }
        }

        /// <summary>Группа отчета</summary>
        public override IList<ReportGroup> ReportGroups {
            get { return new List<ReportGroup> { ReportGroup.Reports }; }
        }

        /// <summary>Предварительный просмотр</summary>
        public override bool IsPreview {
            get { return false; }
        }

        /// <summary>Файл-шаблон отчета</summary>
        protected override byte[] Template {
            get { return Resources.Report_71_1_25; }
        }

        /// <summary>Тип отчета</summary>
        public override IList<ReportKind> ReportKinds {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        #region~~~~~~~~~~~~~~~~~~~~~Параметры~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary> Период - с </summary>
        protected DateTime DatS { get; set; }

        /// <summary> Период -  по </summary>
        protected DateTime DatPo { get; set; }

        /// <summary>Территория</summary>
        protected List<int> Banks { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Список префиксов банков в БД</summary>  
        private List<string> PrefBanks { get; set; }

        #endregion~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>Параметры для отображение на странице браузера</summary>
        /// <returns>Список параметров</returns>
        public override List<UserParam> GetUserParams() {
            return new List<UserParam>
			{
				new PeriodParameter(DateTime.Now, DateTime.Now),
				new BankParameter()
			};
        }

        /// <summary>Заполнить параметры</summary>
        protected override void PrepareParams() {
            var period = UserParamValues["Period"].GetValue<string>();
            DateTime d1;
            DateTime d2;
            PeriodParameter.GetValues(period, out d1, out d2);
            DatS = d1;
            DatPo = d2; 
            Banks = UserParamValues["Banks"].GetValue<List<int>>();
        }

        /// <summary>Заполнить параметры в отчете</summary>
        /// <param name="report">Объект формы отчета</param>
        protected override void PrepareReport(FastReport.Report report) {
            string period = DatS == DatPo
                ? "за " + DatS.ToShortDateString()
                : "c " + DatS.ToShortDateString() + " по " + DatPo.ToShortDateString();
            report.SetParameterValue("period", period);
            report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
            report.SetParameterValue("TIME", DateTime.Now.ToShortTimeString());

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            report.SetParameterValue("headerParam", headerParam);
        }

        /// <summary>Выборка данных</summary>
        /// <returns>Кеш данных</returns>
        public override DataSet GetData() {
            
            string prefData = ReportParams.Pref + DBManager.sDataAliasRest,
                   centralKernel = ReportParams.Pref + DBManager.sKernelAliasRest,
                   sql;

            GetwhereWp();
            string whereSupp = " AND a.nzp_supp IN (900,901)"; //900cx
            foreach (var pref in PrefBanks)
            {   
                string localData = pref + DBManager.sDataAliasRest;

                for (int i = DatS.Year*12 + DatS.Month; i < DatPo.Year*12 + DatPo.Month + 1; i++)
                {
                    var year = i/12;
                    var month = i%12;
                    if (month == 0)
                    {
                        year--;
                        month = 12;
                    }

                    string sumInsaldo = ((month == DatS.Month) & (year == DatS.Year)) ? "sum_insaldo" : "0";
                    string sumOutsaldo = ((month == DatPo.Month) & (year == DatPo.Year)) ? "sum_outsaldo" : "0";

                    string pack = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") +
                                  DBManager.tableDelimiter +
                                  "pack ";
                    string pack_ls = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") +
                                     DBManager.tableDelimiter +
                                     "pack_ls ";
                    string chargeYY = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                      "charge_" + month.ToString("00");
                    string perekidka = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                       "perekidka";
                    string tableFromSupplier = pref + "_charge_" + (year - 2000).ToString("00") +
                                               DBManager.tableDelimiter + "from_supplier ";
                    string tableFnSupplier = pref + "_charge_" + (year - 2000).ToString("00") +
                                             DBManager.tableDelimiter + "fn_supplier" + month.ToString("00");
                    string tableToSupplier = pref + "_charge_" + (year - 2000).ToString("00") +
                                             DBManager.tableDelimiter + "to_supplier" + month.ToString("00");

                    if (TempTableInWebCashe(chargeYY))
                    {
                        string datS = "1." + month + "." + year,
                            datPo = DateTime.DaysInMonth(year, month) + "." + month + "." + year;
                        
                        sql =
                            " INSERT INTO t_oplat(pref, point, dat_uchet, nzp_dom, nzp_supp, nzp_kvar, money_from, money_supp)" +
                            " SELECT '" + pref + month + year + "', '" + pref + "',a.dat_uchet, nzp_dom, nzp_supp,  k.nzp_kvar," +
                            " SUM(case when a.kod_sum in (40) then sum_prih end), SUM(case when a.kod_sum in (49,50,35) then sum_prih end) " +
                            " FROM " + tableFromSupplier + " a, " +
                            pref+ DBManager.sDataAliasRest + " kvar k " +
                            " WHERE a.num_ls = k.num_ls and nzp_serv=206   " +
                            " AND a.dat_uchet >= '" + datS + "' " +
                            " AND a.dat_uchet <= '" + datPo + "'" +
                            " AND a.dat_uchet >= '" + DatS.ToShortDateString() + "' " +
                            " AND a.dat_uchet <= '" + DatPo.ToShortDateString() + "'" + whereSupp +
                            " GROUP BY 1,2,3,4,5,6";
                        ExecSQL(sql);
                     
                        sql =
                            " INSERT INTO t_oplat(pref,  point, dat_uchet, nzp_dom, nzp_supp, nzp_kvar, money_to)" +
                            " SELECT '" + pref + month + year + "', '" + pref + "', a.dat_uchet, nzp_dom,nzp_supp, k.nzp_kvar, SUM(sum_prih)" +
                            " FROM " + tableFnSupplier + " a, " +
                            localData  + " kvar k " +
                            " WHERE  a.num_ls = k.num_ls and nzp_serv=206  and a.kod_sum=33 " +
                            " AND a.dat_uchet >= '" + DatS.ToShortDateString() + "' " +
                            " AND a.dat_uchet <= '" + DatPo.ToShortDateString() + "'" + whereSupp +
                            " GROUP BY 1,2,3,4,5,6 ";
                        ExecSQL(sql);

                        sql =
                            " INSERT INTO t_nach(pref, point, nzp_dom,nzp_supp, nzp_kvar, tarif, sum_insaldo, rsum_tarif, sum_money, sum_outsaldo, reval, real_charge, sum_real ) " +
                            " SELECT '" + pref + month + year + "' AS pref,  '" + pref + "'," +
                            " nzp_dom,nzp_supp, k.nzp_kvar, " +
                            " SUM(tarif) , " +
                            " SUM(" + sumInsaldo + ") , " +
                            " SUM(sum_tarif) , " +
                            " SUM(sum_money) , " +
                            " SUM(" + sumOutsaldo + ") , " +
                            " SUM(reval),  SUM(real_charge),  SUM(sum_tarif+reval+real_charge) " +
                            " FROM " + localData + "kvar k INNER JOIN " + chargeYY + " a ON a.nzp_kvar = k.nzp_kvar " +
                            " WHERE a.nzp_serv = 206 " +
                            " AND dat_charge IS NULL " +
                            whereSupp +
                            " GROUP BY 1,2,3,4,5 ";
                        ExecSQL(sql);  
   
                        ExecSQL(DBManager.sUpdStat + " t_oplat ");
                        ExecSQL(DBManager.sUpdStat + " t_nach ");

                        sql = " UPDATE t_nach " +
                              " SET real_insaldo = ( SELECT "+DBManager.sNvlWord+"(SUM(sum_rcl),0)" +
                              " FROM " + perekidka + " p " +
                              " WHERE p.type_rcl in (100,20) " +
                              " AND p.nzp_kvar = t_nach.nzp_kvar " +
                              " AND p.nzp_serv=206 AND p.nzp_supp = t_nach.nzp_supp " +
                              " AND p.month_ = " + month + ") " +
                              " WHERE pref = '" + pref + month + year + "' ";
                        ExecSQL(sql);

                        ExecSQL("DELETE FROM t_epd_71_1_23 ");
                        sql = " INSERT INTO t_epd_71_1_23" +
                              " SELECT nzp_dom, " +
                                     " COUNT(DISTINCT nzp_serv) AS count_serv, " +
                                     " MAX(CASE WHEN nzp_serv = 206 THEN 1 ELSE 0 END) AS is_kapremont " +
                              " FROM " + localData + "kvar k INNER JOIN " + chargeYY + " c ON c.nzp_kvar = k.nzp_kvar " + 
                              " WHERE c.nzp_serv > 1 " +
                                " AND dat_charge IS NULL" +
                                " GROUP BY 1 ";
                         ExecSQL(sql);

                        ExecSQL(DBManager.sUpdStat + " t_epd_71_1_23 ");

                        sql = " UPDATE t_nach SET is_epd = " +
                              " (SELECT (CASE WHEN count_serv = 1 AND is_kapremont = 1 THEN 1 ELSE 0 END) " +
                               " FROM t_epd_71_1_23 t " +
                               " WHERE t.nzp_dom = t_nach.nzp_dom) " +
                              " WHERE pref = '" + pref + month + year + "' ";
                        ExecSQL(sql);

                    }
                }
                sql = " INSERT INTO t_chet (nzp_dom, chet_perech, payer) " +
                      " SELECT nzp, TRIM(val_prm), " +
                      " cast(case substring(TRIM(val_prm),10,4)  " +
                      " when '6600' then 'ОТДЕЛЕНИЕ №8604 СБЕРБАНКА РОССИИ  Г. ТУЛА' " +
                      " when '0049' then 'ФИЛИАЛ ТРУ ОАО МИНБ' " +
                      " when '0125' then 'ФИЛИАЛ ОАО БАНК ВТБ В Г.ВОРОНЕЖЕ Г. ВОРОНЕЖ'   " +
                      " when '0100' then 'ТУЛЬСКИЙ РФ ОАО РОССЕЛЬХОЗБАНК'  " +
                      " when '0004' then 'Ф-Л ГПБ (ОАО) В Г.ТУЛЕ'   " +
                      " when '6960' then 'ОТДЕЛЕНИЕ №8604 СБЕРБАНКА РОССИИ  Г. ТУЛА'  " +
                      " else ''    " +
                      " end as varchar) as payer " +
                      " FROM " + localData + "prm_2 p " +
                      " WHERE p.is_actual <> 100 " +
                      " AND p.nzp_prm = 2486 " +
                      " AND dat_s <= '" + DatPo + "'" +
                      " AND dat_po >=  '" + DatS + "'";
                ExecSQL(sql);
            }

            sql =
                " INSERT INTO t_oplat1(pref,point, nzp_dom, nzp_supp,  money_from, money_supp, money_to)" +
                " SELECT pref,point,nzp_dom,nzp_supp,  sum(money_from), sum(money_supp), sum(money_to)" +
                " FROM  t_oplat" +
                " GROUP BY 1,2,3,4 ";
            ExecSQL(sql);

            sql =
                " INSERT INTO t_nach1(pref,point, nzp_dom, nzp_supp, is_epd, tarif, sum_insaldo,  nzp_kvar, rsum_tarif," +
                " sum_money, sum_outsaldo, reval, real_charge, real_insaldo, sum_real )" +
                " SELECT pref, point, nzp_dom, nzp_supp, max(is_epd), max(tarif), sum(sum_insaldo),count(distinct nzp_kvar), sum(rsum_tarif)," +
                " sum(sum_money), sum(sum_outsaldo), sum(reval), sum(real_charge-real_insaldo), sum(real_insaldo), sum(sum_real) " +
                " FROM  t_nach" +
                " GROUP BY 1,2,3,4";
            ExecSQL(sql);

            foreach (var pref in PrefBanks)
            {
                string prm_2 = pref + DBManager.sDataAliasRest + "prm_2";
                sql = " UPDATE t_nach1 SET s_ob=(SELECT  val_prm" + DBManager.sConvToNum + " " +
                      " FROM " + prm_2 + " p" +
                      " WHERE is_actual<>100 AND  nzp_prm=40 " +
                      " AND p.nzp=t_nach1.nzp_dom " +
                      " ORDER BY dat_po" +
                      " LIMIT 1)" +
                      " WHERE point='"+pref+"'";
                ExecSQL(sql);
            } 

            sql = " UPDATE t_nach1 SET s_dom = (SELECT s_ob " +
                  " FROM t_nach1 t1 WHERE t_nach1.nzp_dom=t1.nzp_dom LIMIT 1) " +
                  " WHERE nzp_supp=900";
            ExecSQL(sql);
            sql = " UPDATE t_nach1 SET s_dom = (SELECT s_ob " +
                  " FROM t_nach1 t1 WHERE t_nach1.nzp_dom=t1.nzp_dom  LIMIT 1) " +
                  " WHERE nzp_supp=901 AND nzp_dom NOT IN (SELECT nzp_dom FROM t_nach1 WHERE nzp_supp=900)";
            ExecSQL(sql);       
            
 
            sql = " SELECT * " +
                  " FROM t_chet ";
            MyDataReader reader;
            ExecRead(out reader, sql);
            while (reader.Read())
            {
                var chet_perech = reader["chet_perech"].ToString().Trim();
                var payer = reader["payer"].ToString().Trim();
                var nzp_dom = reader["nzp_dom"].ToString().Trim();

                sql =
                    " UPDATE t_nach1 SET chet_perech = chet_perech || '" + chet_perech + "'||'\n' ," +
                    " payer = payer || '" + payer + "'||'\n' " +
                    " WHERE t_nach1.nzp_supp=900 AND  t_nach1.nzp_dom=" + nzp_dom;
                ExecSQL(sql);
            }


            ExecSQL(" drop table t_fin ");
            sql = " SELECT   (CASE WHEN TRIM(nkor) ='-' OR TRIM(nkor) ='' THEN TRIM(ndom) ELSE TRIM(ndom) ||' корп.'||TRIM(nkor) END)  AS ndom, " +
                  " idom, " +
                  " TRIM(ulica) AS ulica, " +
                  " TRIM(ulicareg) AS ulicareg, " +
                  " TRIM(rajon) AS rajon, " +
                  " TRIM(town) AS town, " +
                  " p.point as pref," +
                  " trim(payer) as payer, " +
                  " TRIM(chet_perech) AS chet_perech, d.nzp_dom, t_n.nzp_supp, 1 as c , " +
                  " max(t_n.nzp_kvar) AS count_ls, " +
                  " MAX(is_epd) AS is_epd, " +
                  " max(s_ob) AS s_ob, " +
                  " max(s_dom) AS s_dom, " +                  
                  " max(tarif) AS tarif, " +
                  " max(sum_insaldo) AS sum_insaldo, " +
                  " max(rsum_tarif) AS rsum_tarif, " +
                  " SUM(sum_money) AS sum_money, " +
                  " SUM(money_supp) AS money_supp, " +
                  " SUM(t1.money_to) AS money_to, " +
                  " SUM(money_from) AS money_from, " +
                  " max(real_charge) AS real_charge, " +
                  " max(real_insaldo) AS real_insaldo, " +
                  " max(reval) AS reval, " +
                  " max(sum_real) AS sum_real, " +
                  " max(sum_outsaldo) AS sum_outsaldo" +
                  " into t_fin " +
                  " FROM  t_nach1 t_n left outer join t_oplat1 t1 on t1.nzp_dom=t_n.nzp_dom  and t1.nzp_supp=t_n.nzp_supp INNER JOIN " +
                  prefData + "dom d ON d.nzp_dom = t_n.nzp_dom " +
                  " INNER JOIN " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                  " INNER JOIN " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                  " INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "s_point p" +
                  " WHERE d.nzp_wp=p.nzp_wp " +
                  " GROUP BY 1,2,3,4,5,6,7,8,9,10,11,12 " +
                  " ORDER BY p.point, town, trim(rajon), trim(ulica), ulicareg, idom,ndom ";
            ExecSQL(sql);

            sql = " UPDATE t_fin  SET c = (SELECT COUNT(nzp_supp)" +
                  " FROM t_fin tf " +
                  " WHERE t_fin.nzp_dom=tf.nzp_dom )";
            ExecSQL(sql);

            sql = " SELECT * FROM t_fin  ORDER BY pref, town, rajon, ulica, ulicareg, idom,ndom, nzp_supp ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }


        /// <summary>Ограничение по банку данных</summary>
        /// <returns>Условие выборки для sql-запроса</returns>
        private string GetwhereWp()
        {
            string whereWp = String.Empty;
            if (Banks != null)
            {
                whereWp = Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","));
            }
            else
            {
                whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            }
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            PrefBanks = new List<string>();
            if (!string.IsNullOrEmpty(whereWp))
            {
                TerritoryHeader = String.Empty;
                string sql = " SELECT point,bd_kernel FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWp;
                DataTable terrTable = ExecSQLToTable(sql);

                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                    PrefBanks.Add(row["bd_kernel"].ToString().Trim());
                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }
            else
            {
                string sql = " SELECT bd_kernel FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 and flag=2";
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    PrefBanks.Add(row["bd_kernel"].ToString().Trim());
                }
            }

            return whereWp;
        }



        /// <summary>Создание временных таблиц</summary>
        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_nach (" +
                         " nzp_dom INTEGER, " +
                         " nzp_supp INTEGER, " +
                         " chet_perech CHARACTER(150), " +
                         " nzp_kvar INTEGER, " +
                         " pref CHARACTER(30), " +
                         " point CHARACTER(10), " +
                         " is_epd INTEGER, " +
                         " tarif " + DBManager.sDecimalType + "(14,2), " +
                         " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                         " rsum_tarif  " + DBManager.sDecimalType + "(14,2), " +
                         " sum_money " + DBManager.sDecimalType + "(14,2), " +
                         " reval " + DBManager.sDecimalType + "(14,2), " +
                         " real_charge " + DBManager.sDecimalType + "(14,2), " +
                         " real_insaldo " + DBManager.sDecimalType + "(14,2), " +
                         " sum_real " + DBManager.sDecimalType + "(14,2), " +
                         " sum_outsaldo " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_nach1 (" +
                  " nzp_dom INTEGER, " +
                  " nzp_supp INTEGER, " +
                  " chet_perech CHARACTER(150) DEFAULT '', " +
                  " payer CHARACTER(150) DEFAULT '' , " +
                  " nzp_kvar INTEGER, " +
                  " pref CHARACTER(30), " +
                  " point CHARACTER(10), " +
                  " is_epd INTEGER, " +
                  " tarif " + DBManager.sDecimalType + "(14,2), " +
                  " s_ob " + DBManager.sDecimalType + "(14,2), " +
                  " s_dom " + DBManager.sDecimalType + "(14,2), " +
                  " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                  " rsum_tarif  " + DBManager.sDecimalType + "(14,2), " +
                  " sum_money " + DBManager.sDecimalType + "(14,2), " +
                  " reval " + DBManager.sDecimalType + "(14,2), " +
                  " real_charge " + DBManager.sDecimalType + "(14,2), " +
                  " real_insaldo " + DBManager.sDecimalType + "(14,2), " +
                  " sum_real " + DBManager.sDecimalType + "(14,2), " +
                  " sum_outsaldo " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_oplat (" +  
                  " count_ls INTEGER, " +
                  " nzp_supp INTEGER, " +
                  " nzp_dom INTEGER, " +
                  " nzp_kvar INTEGER, " +
                  " nzp_bank INTEGER, " +
                  " pref CHARACTER(30), " +
                  " point CHARACTER(10), " +
                  " dat_uchet DATE, " +
                  " money_to " + DBManager.sDecimalType + "(14,2) default 0, " +
                  " money_from  " + DBManager.sDecimalType + "(14,2) default 0, " +
                  " money_supp " + DBManager.sDecimalType + "(14,2) default 0 " +
                  " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_oplat1 (" +
                  " nzp_dom INTEGER, " +
                  " nzp_supp INTEGER, " +
                  " nzp_bank INTEGER, " +
                  " nzp_kvar INTEGER, " +
                  " pref CHARACTER(30), " +
                  " point CHARACTER(10), " +
                  " dat_uchet DATE, " +
                  " money_to " + DBManager.sDecimalType + "(14,2) default 0, " +
                  " money_from  " + DBManager.sDecimalType + "(14,2) default 0, " +
                  " money_supp " + DBManager.sDecimalType + "(14,2) default 0 " +
                  " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            ExecSQLToTable(" CREATE INDEX ix_t_nach ON t_nach(nzp_dom) ");
            ExecSQLToTable(" CREATE INDEX ix_t_oplat ON t_oplat(nzp_dom) ");

            sql = " CREATE TEMP TABLE t_epd_71_1_23(" +
                  " nzp_dom INTEGER, " +
                  " count_serv INTEGER, " +
                  " is_kapremont INTEGER)" + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            ExecSQLToTable(" CREATE INDEX ix_t_epd_71_1_23 ON t_epd_71_1_23(nzp_dom) ");

            sql = " CREATE TEMP TABLE t_chet(" +
                  " nzp_dom INTEGER, " +
                  " chet_perech char(50), " +
                  " payer char(50))" + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            ExecSQLToTable(" CREATE INDEX ix_t_chet_71_1_23 ON t_chet(nzp_dom) ");

        }

        /// <summary>Удаление временных таблиц</summary>
        protected override void DropTempTable() {
            ExecSQL("DROP TABLE t_chet");
            ExecSQL("DROP TABLE t_nach");
            ExecSQL("DROP TABLE t_nach1");
            ExecSQL("DROP TABLE t_oplat");
            ExecSQL("DROP TABLE t_oplat1");
            ExecSQL("DROP TABLE t_epd_71_1_23");
        }
    }
}
