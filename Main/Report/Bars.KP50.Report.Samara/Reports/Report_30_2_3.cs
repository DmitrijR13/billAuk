using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Samara.Properties;
using Bars.KP50.Utils;
using Castle.MicroKernel.ModelBuilder.Descriptors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Samara.Reports
{
    class Report3023 : BaseSqlReport
    {
        public override string Name
        {
            get { return "30.2.3 Справка для предоставления в суд"; }
        }

        public override string Description
        {
            get { return "Справка для предоставления в суд"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Cards };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_30_2_3; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.LC }; }
        }



        /// <summary>Примечание</summary>
        protected DateTime Dats { get; set; }

        /// <summary>Примечание</summary>
        protected DateTime Datpo { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }


        public override List<UserParam> GetUserParams()
        {

            DateTime datS = DateTime.Now;
            DateTime datPo =  DateTime.Now;
            return new List<UserParam>
            {
                new PeriodParameter(datS, datPo),
                new SupplierAndBankParameter()
            };
        }

        protected override void PrepareParams()
        {
            DateTime begin;
            DateTime end;
            var period = UserParamValues["Period"].GetValue<string>();
            PeriodParameter.GetValues(period, out begin, out end);
            Dats = begin;
            Datpo = end;

            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Января","Февраля",
                 "Марта","Апреля","Мая","Июня","Июля","Августа","Сентября",
                 "Октября","Ноября","Декабря"};
            report.SetParameterValue("day", DateTime.Now.Day);
            report.SetParameterValue("month", months[DateTime.Now.Month]);
            report.SetParameterValue("year", DateTime.Now.Year);
        }

        public override DataSet GetData()
        {
            IDataReader reader;

            var sql = " select pref " +
                         "from  " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar " +
                         "where nzp_kvar=" + ReportParams.NzpObject + GetwhereWp();
            ExecRead(out reader, sql);

            if (reader.Read())
            {
                string pref = reader["pref"].ToString().Trim();


                //Общая площадь

                sql = " INSERT INTO t_fio (ob_s) " +
                      " SELECT val_prm as ob_s " +
                      " FROM " + pref + DBManager.sDataAliasRest + "prm_1 " +
                      " WHERE nzp=" + ReportParams.NzpObject +
                      "        AND is_actual=1 " +
                      "        AND dat_s<=" + DBManager.sCurDate +
                      "        AND dat_po>=" + DBManager.sCurDate +
                      "        AND nzp_prm = 4 ";
                ExecSQL(sql);


                //Количество зарегистрированных

                sql = " INSERT INTO t_fio (reg_count) " +
                      " SELECT count(distinct nzp_gil) " +
                      " FROM " + pref + DBManager.sDataAliasRest + "kart " +
                      " WHERE nzp_kvar=" + ReportParams.NzpObject +
                      "        AND isactual='1' " +
                      "        AND nzp_tkrt = 1 and nzp_kvar =(select max(nzp) from " +
                      "                " + pref + DBManager.sDataAliasRest + "prm_1 " +
                      "                WHERE nzp_prm=130  and is_actual=1" +
                      "                       AND  nzp=" + ReportParams.NzpObject +
                      "                       AND dat_s<='" + DBManager.sCurDate + "'" +
                      "                       AND dat_po>='" + DBManager.sCurDate + "')";
                ExecSQL(sql);



                sql = " INSERT INTO t_fio (reg_count) " +
                      " SELECT val_prm" + DBManager.sConvToInt + " " +
                      " FROM " + pref + DBManager.sDataAliasRest + "prm_1 p" +
                      " WHERE nzp_prm=5 and p.nzp=" + ReportParams.NzpObject +
                      "        AND p.is_actual=1  " +
                      "        AND p.dat_s<=" + DBManager.sCurDate +
                      "        AND p.dat_po>=" + DBManager.sCurDate +
                      "        AND 0 =(select count(*) from " +
                      "                " + pref + DBManager.sDataAliasRest + "prm_1 " +
                      "                WHERE nzp_prm=130  and is_actual=1" +
                      "                       AND  nzp=" + ReportParams.NzpObject +
                      "                       AND dat_s<=" + DBManager.sCurDate + "" +
                      "                       AND dat_po>=" + DBManager.sCurDate + ")";
                ExecSQL(sql);

                //Количество проживающих
                sql = " INSERT INTO t_fio (gil_count) " +
                      " SELECT (select max(reg_count) FROM t_fio) + " + DBManager.sNvlWord + "(max(val_prm" + DBManager.sConvToInt + "),0) - count(distinct nzp_gilec) " +
                      " FROM " +
                        pref + DBManager.sDataAliasRest + "prm_1 p, " +
                        pref + DBManager.sDataAliasRest + "gil_periods gp " +
                      " WHERE gp.nzp_kvar=" + ReportParams.NzpObject +
                      "        AND gp.is_actual=1 " +
                      "        AND gp.dat_s<='" + DBManager.sCurDate + "'" +
                      "        AND gp.dat_po>='" + DBManager.sCurDate + "'" +
                      "        AND gp.nzp_tkrt = 1 " +
                      "        AND gp.nzp_kvar = (select max(nzp) from " +
                      "                " + pref + DBManager.sDataAliasRest + "prm_1 " +
                      "                WHERE nzp_prm=131  and is_actual=1" +
                      "                       AND  nzp=" + ReportParams.NzpObject +
                      "                       AND dat_s<='" + DBManager.sCurDate + "'" +
                      "                       AND dat_po>='" + DBManager.sCurDate + "')";
                ExecSQL(sql);

                for (int i = Dats.Year*12 + Dats.Month; i < Datpo.Year*12 + Datpo.Month + 1; i++)
                {
                    var year = i/12;
                    var month = i%12;
                    if (month == 0)
                    {
                        year--;
                        month = 12;
                    }

                    var monthPeriod = new DateTime(year, month, 1);

                    string chargeTable = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                  "charge_" + month.ToString("00");
                    string packls = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") +
                                          DBManager.tableDelimiter +
                                          "pack_ls";

                    if (TempTableInWebCashe(chargeTable) && TempTableInWebCashe(packls))
                    sql = " insert into t_svod (dat, sum_tarif, serv_17, serv_2, serv_22, serv_233, serv_6, " +
                          " serv_8, serv_9, serv_14, serv_25, serv_213, serv_13, serv_12, serv_15, serv_7, serv_18, " +
                          " serv_11, serv_215, tarif_17, tarif_2, tarif_22, tarif_233, tarif_6, tarif_8, tarif_9, " +
                          " tarif_14, tarif_25, tarif_213, tarif_13, tarif_12, tarif_15, tarif_7, tarif_18, " +
                          " tarif_11, tarif_215, debt_relief, sum_money, dat_prih, perech, sum_outsaldo) " +
                          " select '" + month.ToString("00") + "/" + year + "' as dat, " +
                          " sum(sum_tarif) as sum_tarif, " +
                          " sum(case when c.nzp_serv = 17 then sum_tarif end) as serv_17, " +
                          " sum(case when c.nzp_serv = 2 then sum_tarif end) as serv_2, " +
                          " sum(case when c.nzp_serv = 22 then sum_tarif end) as serv_22, " +
                          " sum(case when c.nzp_serv = 233 then sum_tarif end) as serv_233, " +
                          " sum(case when c.nzp_serv = 6 then sum_tarif end) as serv_6, " +
                          " sum(case when c.nzp_serv = 8 then sum_tarif end) as serv_8, " +
                          " sum(case when c.nzp_serv = 9 then sum_tarif end) as serv_9, " +
                          " sum(case when c.nzp_serv = 14 then sum_tarif end) as serv_14, " +
                          " sum(case when c.nzp_serv = 25 then sum_tarif end) as serv_25, " +
                          " sum(case when c.nzp_serv = 213 then sum_tarif end) as serv_213, " +
                          " sum(case when c.nzp_serv = 13 then sum_tarif end) as serv_13, " +
                          " sum(case when c.nzp_serv = 12 then sum_tarif end) as serv_12, " +
                          " sum(case when c.nzp_serv = 15 then sum_tarif end) as serv_15, " +
                          " sum(case when c.nzp_serv = 7 then sum_tarif end) as serv_7, " +
                          " sum(case when c.nzp_serv = 18 then sum_tarif end) as serv_18, " +
                          " sum(case when c.nzp_serv = 11 then sum_tarif end) as serv_11, " +
                          " sum(case when c.nzp_serv = 215 then sum_tarif end) as serv_215, " +
                          " max(case when c.nzp_serv = 17 then tarif end) as tarif_17, " +
                          " max(case when c.nzp_serv = 2 then tarif end) as tarif_2, " +
                          " max(case when c.nzp_serv = 22 then tarif end) as tarif_22, " +
                          " max(case when c.nzp_serv = 233 then tarif end) as tarif_233, " +
                          " max(case when c.nzp_serv = 6 then tarif end) as tarif_6, " +
                          " max(case when c.nzp_serv = 8 then tarif end) as tarif_8, " +
                          " max(case when c.nzp_serv = 9 then tarif end) as tarif_9, " +
                          " max(case when c.nzp_serv = 14 then tarif end) as tarif_14, " +
                          " max(case when c.nzp_serv = 25 then tarif end) as tarif_25, " +
                          " max(case when c.nzp_serv = 213 then tarif end) as tarif_213, " +
                          " max(case when c.nzp_serv = 13 then tarif end) as tarif_13, " +
                          " max(case when c.nzp_serv = 12 then tarif end) as tarif_12, " +
                          " max(case when c.nzp_serv = 15 then tarif end) as tarif_15, " +
                          " max(case when c.nzp_serv = 7 then tarif end) as tarif_7, " +
                          " max(case when c.nzp_serv = 18 then tarif end) as tarif_18, " +
                          " max(case when c.nzp_serv = 11 then tarif end) as tarif_11, " +
                          " max(case when c.nzp_serv = 215 then tarif end) as tarif_215, " +
                          " sum(reval + real_charge) as debt_relief, " +
                          " sum(sum_money) as sum_money, " +
                          " max(dat_vvod) as dat_prih, " +
                          " sum(0) as perech, " +
                          " sum(sum_outsaldo) as sum_outsaldo " +
                          " from " + chargeTable + " c, " + packls + " pls " +
                          " where c.num_ls = pls.num_ls " +
                          " and c.num_ls = " + ReportParams.NzpObject +
                          " and dat_charge is null " +
                          " and c.nzp_serv in (17,2,22,233,6,8,9,14,25,213,13,12,15,7,18,11,215) " + GetWhereSupp() +
                          " and dat_vvod between '" + monthPeriod.ToShortDateString() + "' and '" + monthPeriod.AddMonths(1).AddDays(-1).ToShortDateString() + "' ";
                    ExecSQL(sql);

                    sql = " insert into t_date (dat, dat_prih) " +
                           " select '" + month.ToString("00") + "/" + year + "' as dat, dat_vvod " +
                           " from  " + packls +
                           " where dat_vvod between '" + monthPeriod.ToShortDateString() + "' and '" + monthPeriod.AddMonths(1).AddDays(-1).ToShortDateString() + "' " +
                           " and num_ls = " + ReportParams.NzpObject + GetWhereSupp();
                    ExecSQL(sql);
                }
            }

            //Адрес
            sql = " select fio, town, " +
                  " case when rajon<>'-' then rajon end as rajon, ulica, ndom, " +
                  " case when nkor<>'-' then nkor end as nkor, " +
                  " case when nkvar<>'-' and nkvar<>'0' then nkvar end as nkvar, " +
                  " case when nkvar_n<>'-' then nkvar_n end as nkvar_n, " +
                  " max(ob_s) as ob_s, max(reg_count) as reg_count, max(gil_count) as gil_count " +
                  " from t_fio tf, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "s_town t, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "kvar k " +
                  " where k.nzp_dom = d.nzp_dom " +
                  " and d.nzp_ul = u.nzp_ul " +
                  " and u.nzp_raj = r.nzp_raj " +
                  " and r.nzp_town = t.nzp_town " +
                  " and num_ls = " + ReportParams.NzpObject +
                  " group by 1,2,3,4,5,6,7,8";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            DataTable dt1 = ExecSQLToTable(" select * from t_svod ");
            DataTable dt2 = ExecSQLToTable(" select * from t_date ");
            dt1.TableName = "Q_master1";
            dt2.TableName = "Q_master2";

            var ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables.Add(dt1);
            ds.Tables.Add(dt2);
            return ds;
        }


        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp()
        {
            string whereSupp = String.Empty;
            if (Suppliers != null)
            {
                whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
            }
            else
            {
                whereSupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);
            }
            whereSupp = whereSupp.TrimEnd(',');
            whereSupp = !String.IsNullOrEmpty(whereSupp) ? " AND c.nzp_supp in (" + whereSupp + ")" : String.Empty;
            return whereSupp;
        }

        /// <summary>
        /// Получить условия органичения по банкам данных
        /// </summary>
        /// <returns></returns>
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
            if (!String.IsNullOrEmpty(whereWp))
            {
                DataTable wp = ExecSQLToTable(" select bd_kernel from " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point where nzp_wp in (" + whereWp + ") ");
                whereWp = String.Join(",", wp.Rows.Cast<DataRow>().Select(x => x[0].ToString().Trim()).ToArray());
                whereWp = !String.IsNullOrEmpty(whereWp) ? " AND pref in (" + whereWp + ")" : String.Empty;
            }
            return whereWp;
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_fio( " +
                         " ob_s " + DBManager.sDecimalType + "(14,2), " +
                         " reg_count integer, " +
                         " gil_count integer) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_svod  ( " +
                  " dat char(15), " +
                  " sum_tarif " + DBManager.sDecimalType + "(14,2), " +
                  " serv_17 " + DBManager.sDecimalType + "(14,2), " +
                  " serv_2 " + DBManager.sDecimalType + "(14,2), " +
                  " serv_22 " + DBManager.sDecimalType + "(14,2), " +
                  " serv_233 " + DBManager.sDecimalType + "(14,2), " +
                  " serv_6 " + DBManager.sDecimalType + "(14,2), " +
                  " serv_8 " + DBManager.sDecimalType + "(14,2), " +
                  " serv_9 " + DBManager.sDecimalType + "(14,2), " +
                  " serv_14 " + DBManager.sDecimalType + "(14,2), " +
                  " serv_25 " + DBManager.sDecimalType + "(14,2), " +
                  " serv_213 " + DBManager.sDecimalType + "(14,2), " +
                  " serv_13 " + DBManager.sDecimalType + "(14,2), " +
                  " serv_12 " + DBManager.sDecimalType + "(14,2), " +
                  " serv_15 " + DBManager.sDecimalType + "(14,2), " +
                  " serv_7 " + DBManager.sDecimalType + "(14,2), " +
                  " serv_18 " + DBManager.sDecimalType + "(14,2), " +
                  " serv_11 " + DBManager.sDecimalType + "(14,2), " +
                  " serv_215 " + DBManager.sDecimalType + "(14,2), " +
                  " tarif_17 " + DBManager.sDecimalType + "(14,2), " +
                  " tarif_2 " + DBManager.sDecimalType + "(14,2), " +
                  " tarif_22 " + DBManager.sDecimalType + "(14,2), " +
                  " tarif_233 " + DBManager.sDecimalType + "(14,2), " +
                  " tarif_6 " + DBManager.sDecimalType + "(14,2), " +
                  " tarif_8 " + DBManager.sDecimalType + "(14,2), " +
                  " tarif_9 " + DBManager.sDecimalType + "(14,2), " +
                  " tarif_14 " + DBManager.sDecimalType + "(14,2), " +
                  " tarif_25 " + DBManager.sDecimalType + "(14,2), " +
                  " tarif_213 " + DBManager.sDecimalType + "(14,2), " +
                  " tarif_13 " + DBManager.sDecimalType + "(14,2), " +
                  " tarif_12 " + DBManager.sDecimalType + "(14,2), " +
                  " tarif_15 " + DBManager.sDecimalType + "(14,2), " +
                  " tarif_7 " + DBManager.sDecimalType + "(14,2), " +
                  " tarif_18 " + DBManager.sDecimalType + "(14,2), " +
                  " tarif_11 " + DBManager.sDecimalType + "(14,2), " +
                  " tarif_215 " + DBManager.sDecimalType + "(14,2), " +
                  " debt_relief " + DBManager.sDecimalType + "(14,2), " +
                  " sum_money " + DBManager.sDecimalType + "(14,2), " +
                  " dat_prih date, " +
                  " perech " + DBManager.sDecimalType + "(14,2), " +
                  " sum_outsaldo " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            ExecSQL(" create temp table t_date (dat char(15), dat_prih date)" + DBManager.sUnlogTempTable);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_fio ");
            ExecSQL(" DROP TABLE t_svod ");
            ExecSQL(" DROP TABLE t_date ");
        }
    }
}
