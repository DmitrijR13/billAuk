using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Samara.Properties;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using Constants = STCLINE.KP50.Global.Constants;

namespace Bars.KP50.Report.Samara.Reports
{
    class Report3001016 : BaseSqlReport
    {
        public override string Name
        {
            get { return "30.1.16 Список домов по УК"; }
        }

        public override string Description
        {
            get { return "Список домов по УК"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Reports };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_30_1_16; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }



        /// <summary>Дата</summary>
        private DateTime Date { get; set; }

        /// <summary>УК</summary>
        private int Area { get; set; }

        /// <summary>УК</summary>
        private string AreaHeader { get; set; }
        private bool RowCount { get; set; }

        public override List<UserParam> GetUserParams()
        {
            DateTime datS =DateTime.Now;
            return new List<UserParam>
            {
                new PeriodParameter(datS) { Name = "Дата"},
                new AreaParameter(false) { Require = true }
            };
        }

        protected override void PrepareParams()
        {
            DateTime date;
            var period = UserParamValues["Period"].GetValue<string>();
            PeriodParameter.GetValues(period, out date);
            Date = date;
            Area = UserParamValues["Areas"].GetValue<int>();
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("date", DateTime.Now.ToLongDateString());
            report.SetParameterValue("time", DateTime.Now.ToShortTimeString());
            report.SetParameterValue("main_date", Date.ToLongDateString());
            report.SetParameterValue("area", AreaHeader);

            report.SetParameterValue("excel",
                RowCount
                    ? "Выборка записей ограничена первыми 70000 строками. Выберите другой формат экспортируемого файла, либо поставьте другие ограничения для отчета"
                    : "");
        }

        public override DataSet GetData()
        {
            IDataReader reader;

            #region выборка в temp таблицу

            string whereAdr = GetWhereAdr("k.");

            string sql = " SELECT bd_kernel AS pref " +
                         " FROM " + DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "s_point " +
                         " WHERE nzp_wp>1 " + GetwhereWp();

            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();

                string chargeTable = pref + "_charge_" + (Date.Year - 2000).ToString("00") +
                    DBManager.tableDelimiter + "charge_" + Date.Month.ToString("00");

                if (TempTableInWebCashe(chargeTable))
                {
                    sql = " INSERT INTO t_money (nzp_kvar, sum_insaldo, sum_tarif, sum_money, sum_outsaldo) " +
                          " SELECT nzp_kvar, " +
                          " sum(sum_insaldo) as sum_insaldo, " +
                          " sum(sum_tarif) as sum_tarif, " +
                          " sum(sum_money) as sum_money, " +
                          " sum(sum_outsaldo) as sum_outsaldo " +
                          " FROM " + chargeTable +
                          " WHERE dat_charge is null AND nzp_serv > 1 " +
                          " GROUP BY 1 ";
                    ExecSQL(sql);


                    sql = " INSERT INTO t_adr (nzp_kvar, town, rajon, ulica, idom, ndom, nkor, ikvar, nkvar, fio) " +
                          " SELECT nzp_kvar, town, rajon, ulica, idom, ndom, nkor, ikvar, nkvar, fio " +
                          " FROM " +
                            pref + DBManager.sDataAliasRest + "kvar k, " +
                            pref + DBManager.sDataAliasRest + "dom d, " +
                            pref + DBManager.sDataAliasRest + "s_ulica u, " +
                            pref + DBManager.sDataAliasRest + "s_rajon r, " +
                            pref + DBManager.sDataAliasRest + "s_town t " +
                          " WHERE k.nzp_dom = d.nzp_dom " + 
                          " AND d.nzp_ul = u.nzp_ul " +
                          " AND u.nzp_raj = r.nzp_raj " +
                          " AND r.nzp_town = t.nzp_town " + whereAdr;
                    ExecSQL(sql);

                    ExecSQL(" UPDATE t_adr SET rajon = town WHERE rajon = '-' ");
                    ExecSQL(" UPDATE t_adr SET nkor = '' WHERE nkor = '-'  ");
                    ExecSQL(" UPDATE t_adr SET nkvar = '' WHERE nkvar = '-' OR nkvar = '0' ");
                }
            }

            ExecSQL(" UPDATE t_money SET debt = ROUND(CASE WHEN sum_tarif<>0 THEN sum_insaldo/sum_tarif ELSE 0 END) ");

            ExecSQL(" CREATE INDEX money_ndx on t_money(nzp_kvar) ");
            ExecSQL(DBManager.sUpdStat + " t_money ");

            ExecSQL(" CREATE INDEX adr_ndx on t_adr(nzp_kvar) ");
            ExecSQL(DBManager.sUpdStat + " t_adr ");

            reader.Close();
            #endregion

            sql = " SELECT rajon, ulica, idom, ndom, nkor, ikvar, nkvar, fio, " +
                  " sum_insaldo, sum_tarif, sum_money, sum_outsaldo, debt " +
                  " FROM t_money m, t_adr a " +
                  " WHERE m.nzp_kvar = a.nzp_kvar " +
                  " ORDER BY 1,2,3,4,5,6,7 ";
            DataTable dt;

            #region Ограничение на кол-во строк 
            try
            {
                dt = ExecSQLToTable(sql);
            }
            catch
            {
                dt = ExecSQLToTable(DBManager.SetLimitOffset(sql,70000,0));
            }

            if (dt.Rows.Count > 70000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = dt.Rows.Cast<DataRow>().Skip(70000).ToArray();
                dtr.ForEach(dt.Rows.Remove);
                RowCount = true;
            }
            else
            {
                RowCount = false;
            }
            #endregion

            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }


        /// <summary>
        /// Получает условия ограничения по территории
        /// </summary>
        /// <returns></returns>
        private string GetWhereAdr(string prefix)
        {
            string whereAdr = " AND " + prefix +"nzp_area in (" + Area + ")";

            string sql = " SELECT area from " +
                           ReportParams.Pref + DBManager.sDataAliasRest + "s_area " + prefix.TrimEnd('.') +
                         " WHERE nzp_area > 0 " + whereAdr;
            DataTable area = ExecSQLToTable(sql);
            AreaHeader = area.Rows.Count >0 ? area.Rows[0]["area"].ToString().Trim() : "";
            
            return whereAdr;
        }


        /// <summary>
        /// Получить условия органичения по банкам данных
        /// </summary>
        /// <returns></returns>
        private string GetwhereWp()
        {
            string whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            return whereWp;
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_money ( " +
                                " nzp_kvar integer, " +
                                " sum_insaldo " + DBManager.sDecimalType + "(14,2),  " +
                                " sum_tarif " + DBManager.sDecimalType + "(14,2),  " +
                                " sum_money " + DBManager.sDecimalType + "(14,2),  " +
                                " sum_outsaldo " + DBManager.sDecimalType + "(14,2),  " +
                                " debt integer) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            sql = " CREATE TEMP TABLE t_adr ( " +
                                " nzp_kvar integer, " +
                                " town character(30), " +
                                " rajon character(30), " +
                                " ulica character(40), " +
                                " idom integer, " +
                                " ndom character(15), " +
                                " nkor character(15), " +
                                " ikvar integer, " +
                                " nkvar character(10), " +
                                " fio character(40)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_money ");
            ExecSQL(" DROP TABLE t_adr ");
        }
    }
}
