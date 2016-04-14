using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Sakha.Properties;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Sakha.Reports
{
    public class Report1413 : BaseSqlReport
    {
        public override string Name
        {
            get { return "14.1.3 Реестр начислений по поставщикам "; }
        }

        public override string Description
        {
            get { return "14.1.3 Реестр начислений по поставщикам "; }
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
            get { return Resources.Report_14_1_3; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }


        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }



        /// <summary>Заголовок отчета</summary>
        protected string ServiceHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AreaHeader { get; set; }

        private bool _rowCount;

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new SupplierAndBankParameter(),
                new AreaParameter()
            };
        }

        public override DataSet GetData()
        {

            string sql = " select bd_kernel as pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " where nzp_wp>1 " + GetWhereWp();
            DataTable dpref = ExecSQLToTable(sql);
       
            foreach (DataRow dr in dpref.Rows)
            {
                if (dr["pref"] == null) continue;
                string pref = dr["pref"].ToString().Trim();
                string chargeXx = pref + "_charge_" + (Year - 2000).ToString("00") +
                                  DBManager.tableDelimiter + "charge_" + Month.ToString("00");

                //Проверка на существование базы
                if (TempTableInWebCashe(chargeXx))
                {

                    sql = " INSERT into t_reestr_nach_supp (nzp_area,area,nzp_supp,name_supp,nzp_serv,service,sum_tarif,sum_lgota,sum_sn,sum_charge )" +
                        " SELECT  k.nzp_area, max(a.area) as area,max(ch.nzp_supp), p.payer as name_supp,ch.nzp_serv, max(serv.service) as service, " +
                        " sum(ch.rsum_tarif) as sum_tarif, sum(ch.sum_lgota) as sum_lgota,  " +
                        " sum(ch.real_charge+ch.reval+ch.sum_nedop) as sum_sn, " +
                        " sum(ch.sum_charge) as sum_charge " +
                        " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier sup," +
                        " " + ReportParams.Pref + DBManager.sKernelAliasRest + "services serv," +
                        " " + chargeXx + " ch, " +
                        "" + ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer p, " +
                        "" + pref + DBManager.sDataAliasRest + "kvar k, " +
                        "" + ReportParams.Pref + DBManager.sDataAliasRest + "s_area a " +
                        " WHERE ch.nzp_supp=sup.nzp_supp and serv.nzp_serv=ch.nzp_serv and k.nzp_kvar=ch.nzp_kvar and k.nzp_area=a.nzp_area " +
                        " and sup.nzp_supp=ch.nzp_supp and sup.nzp_payer_supp=p.nzp_payer and abs(ch.sum_tarif)>0.0001 " +
                        " and ch.dat_charge is null and ch.nzp_serv>1 " + GetWhereAdr() + GetWhereSupp() +
                        " GROUP BY k.nzp_area, p.payer, ch.nzp_serv ";

                    ExecSQL(sql);

                }
            }

            sql = " SELECT * FROM t_reestr_nach_supp ORDER BY nzp_area,nzp_supp,nzp_serv";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            if (dt.Rows.Count > 70000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = dt.Rows.Cast<DataRow>().Skip(70000).ToArray();
                dtr.ForEach(dt.Rows.Remove);
                _rowCount = true;
            }
            else
            {
                _rowCount = false;
            }

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }



        protected override void CreateTempTable()
        {
            const string sql = " create temp table t_reestr_nach_supp (     " +
                               " nzp_area integer," +
                               " area varchar(100)," +
                               " nzp_supp integer," +
                               " name_supp varchar(100)," +
                               " nzp_serv integer," +
                               " service varchar(100)," +
                               " sum_tarif " + DBManager.sDecimalType + "(14,2) default 0.00," + //начисление плановое
                               " sum_lgota  " + DBManager.sDecimalType + "(14,2) default 0.00," + //льготы
                               " sum_sn  " + DBManager.sDecimalType + "(14,2) default 0.00," + //снятия
                               " sum_charge  " + DBManager.sDecimalType + "(14,2) default 0.00" + //начиcление фактическое
                               " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            try
            {
                ExecSQL(" drop table t_reestr_nach_supp ");
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Отчет 14.1.3 " + e.Message, MonitorLog.typelog.Error, false);
            }
        }


        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("month", Month);
            report.SetParameterValue("year", Year);
            report.SetParameterValue("reportHeader", Name);
            report.SetParameterValue("period", CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Month) + " месяц " + Year + " года");
            report.SetParameterValue("excel",
              _rowCount
                  ? "Выборка записей ограничена первыми 70000 строками. Выберите другой формат экспортируемого файла, либо поставьте другие ограничения для отчета"
                  : "");
            report.SetParameterValue("supplier", GetListNameSupp());
            report.SetParameterValue("adres", "Все адреса");
            report.SetParameterValue("banks", GetListNameBanks());
        }

        /// <summary>
        /// Получить условия органичения по банкам
        /// </summary>
        /// <returns></returns>
        private string GetWhereWp()
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
            return whereWp;
        }

        /// <summary>
        /// Получает наименования банков
        /// </summary>
        /// <returns></returns>
        private string GetListNameBanks()
        {
            string names = "Все округа";
            if (Banks != null)
            {
                string sql = " SELECT point FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                             " WHERE 1=1 " + GetWhereWp();
                DataTable dt = ExecSQLToTable(sql);
                string[] listNames = new string[dt.Rows.Count];
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    listNames[i] = dt.Rows[i]["point"] != DBNull.Value
                        ? dt.Rows[i]["point"].ToString().Trim()
                        : "";
                }
                names = string.Join(",", listNames);
            }
            return names;
        }


        /// <summary>
        /// Получает условия ограничения по территории
        /// </summary>
        /// <returns></returns>
        private string GetWhereAdr()
        {
            string whereAdr = String.Empty;
            if (Areas != null)
            {
                whereAdr = Areas.Aggregate(whereAdr, (current, nzpArea) => current + (nzpArea + ","));
            }

            whereAdr = whereAdr.TrimEnd(',');
            if (String.IsNullOrEmpty(whereAdr)) whereAdr = ReportParams.GetRolesCondition(Constants.role_sql_area);

            if (!String.IsNullOrEmpty(whereAdr))
            {
                whereAdr = " AND k.nzp_area in (" + whereAdr + ")";

                if (!String.IsNullOrEmpty(AreaHeader))
                {
                    string sql = " SELECT area from " +
                                 ReportParams.Pref + DBManager.sDataAliasRest + "s_area " +
                                 " WHERE nzp_area > 0 " + whereAdr;
                    DataTable area = ExecSQLToTable(sql);
                    foreach (DataRow dr in area.Rows)
                    {
                        AreaHeader += dr["area"].ToString().Trim() + ",";
                    }
                    AreaHeader = AreaHeader.TrimEnd(',');
                }
            }
            return whereAdr;
        }

        /// <summary>
        /// Получает условия ограничения по поставщику
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp()
        {
            string whereSupp = String.Empty;
            if (Suppliers != null)
                whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
            whereSupp = whereSupp.TrimEnd(',');
            if (String.IsNullOrEmpty(whereSupp)) whereSupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);
            if (!String.IsNullOrEmpty(whereSupp)) whereSupp = " and ch.nzp_supp in (" + whereSupp + ")";
            return whereSupp;
        }

        /// <summary>
        /// Получает наименования поставщиков
        /// </summary>
        /// <returns></returns>
        private string GetListNameSupp()
        {
            string names = "Все поставщики";
            if (Suppliers != null)
            {
                string sql = " SELECT name_supp FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier ch " +
                             " WHERE 1=1 " + GetWhereSupp();
                DataTable dt = ExecSQLToTable(sql);
                string[] listNames = new string[dt.Rows.Count];
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    listNames[i] = dt.Rows[i]["name_supp"] != DBNull.Value
                        ? dt.Rows[i]["name_supp"].ToString().Trim()
                        : "";
                }
                names = string.Join(",", listNames);
            }
            return names;
        }



        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].Value.To<int>();

            if (!String.IsNullOrEmpty(ReportParams.User.date_begin))
            {
                if (Convert.ToDateTime(ReportParams.User.date_begin) > new DateTime(Year, Month, 01))
                {
                    Month = Convert.ToDateTime(ReportParams.User.date_begin).Month;
                    Year = Convert.ToDateTime(ReportParams.User.date_begin).Year;
                }

            }

            Areas = UserParamValues["Areas"].Value.To<List<int>>();
            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
        }
    }
}
