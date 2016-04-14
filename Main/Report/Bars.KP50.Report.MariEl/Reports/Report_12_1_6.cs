using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Base.Parameters;
using Bars.KP50.Report.MariEl.Properties;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Report.MariEl.Reports
{
    public class Report120106 : BaseSqlReport
    {
        /// <summary>Наименование отчета</summary>
        public override string Name {
            get { return "12.1.6 Сводный отчет по начислениям и объемам"; }
        }

        /// <summary>Описание отчета</summary>
        public override string Description {
            get { return "12.1.6 Сводный отчет по начислениям и объемам"; }
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
            get { return Resources.Report_12_1_6; }
        }

        /// <summary>Тип отчета</summary>
        public override IList<ReportKind> ReportKinds {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        #region~~~~~~~~~~~~~~~~~~~~~Параметры~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>Тип отчета</summary>
        private enum TypeReport
        {
            /// <summary>по начислениям</summary>
            Charge = 1,
            /// <summary>по объёмам</summary>
            Volume = 2
        }

        /// <summary>В разрезе</summary>
        private enum TypeGroup
        {
            /// <summary>УК</summary>
            UK = 1,
            /// <summary>ЖЭУ</summary>
            ZHEU = 2
        }

        private TypeReport _typeReport;

        private TypeGroup _typeGroup;

        /// <summary>Территория(банк данных)</summary>
        private List<int> Territories { get; set; }

        /// <summary>Месяц</summary>
        private int Month { get; set; }

        /// <summary>Год</summary>
        private int Year { get; set; }

        #endregion~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>Параметры для отображение на странице браузера</summary>
        /// <returns>Список параметров</returns>
        public override List<UserParam> GetUserParams() {
            return new List<UserParam>
            {
                new BankParameter(),
                new MonthParameter{ Value = DateTime.Now.Month },
                new YearParameter{ Value = DateTime.Now.Year },
                new ComboBoxParameter
                {
                    Code = "TypeReport",
                    Name = "Сводный отчет",
                    TypeValue = typeof(int),
                    Value = "1",
                    StoreData = new List<object>
                    {
                        new { Id = 1, Name = "по начислениям" },
                        new { Id = 2, Name = "по объемам" }
                    }
                },
                new ComboBoxParameter
                {
                    Code = "TypeGroup",
                    Name = "в разрезе",
                    TypeValue = typeof(int),
                    Value = "1",
                    StoreData = new List<object>
                    {
                        new { Id = 1, Name = "УК" },
                        new { Id = 2, Name = "ЖЭУ" }
                    }
                }
            };
        }

        /// <summary>Заполнить параметры</summary>
        protected override void PrepareParams() {
            _typeReport = (TypeReport) UserParamValues["TypeReport"].GetValue<int>();
            _typeGroup = (TypeGroup) UserParamValues["TypeGroup"].GetValue<int>();
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            Territories = UserParamValues["Banks"].GetValue<List<int>>();
        }

        /// <summary>Заполнить параметры в отчете</summary>
        /// <param name="report">Объект формы отчета</param>
        protected override void PrepareReport(FastReport.Report report) {
            report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
            report.SetParameterValue("TIME", DateTime.Now.ToShortTimeString());
            string title = _typeReport == TypeReport.Charge ? "Сводный отчет по начислениям" : "Сводный отчет по объемам";
            report.SetParameterValue("title", title);
            report.SetParameterValue("column1", _typeGroup == TypeGroup.UK ? "УК" : "ЖЭУ");
            report.SetParameterValue("column2", _typeGroup == TypeGroup.UK ? "ЖЭУ" :"УК" );
        }

        /// <summary>Выборка данных</summary>
        /// <returns>Кеш данных</returns>
        public override DataSet GetData() {
            MyDataReader reader;
            string prefKernel = ReportParams.Pref + DBManager.sKernelAliasRest,
                    prefData = ReportParams.Pref + DBManager.sDataAliasRest;

            string whereWp = GetwhereWp();
            string sql = " SELECT TRIM(bd_kernel) AS bd_kernel " +
                         " FROM " + prefKernel + "s_point " +
                         " WHERE nzp_wp > 1 " + whereWp;
            ExecRead(out reader, sql);

            while (reader.Read())
            {
                string pref = reader["bd_kernel"] != DBNull.Value ? reader["bd_kernel"].ToString() : string.Empty;
                string chargeYY = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter +
                                  "charge_" + Month.ToString("00");

                sql = " INSERT INTO t_report_12_1_6(pref, nzp_geu, nzp_area, nzp_supp, nzp_serv, sum_nach, rashod) " +
                      " SELECT '" + pref + "' AS pref, k.nzp_geu, k.nzp_area, c.nzp_supp, c.nzp_serv, " +
                               " SUM(sum_tarif + reval + real_charge) AS sum_nach,  " +
                               " SUM(CASE WHEN tarif > 0 THEN (sum_tarif + reval + real_charge) / tarif ELSE 0 END) AS rashod " +
                      " FROM " + chargeYY + " c INNER JOIN " + prefData + "kvar k ON k.nzp_kvar = c.nzp_kvar " +
                      " WHERE dat_charge IS NULL " +
                        " AND c.nzp_supp > 0 " +
                        " AND c.nzp_serv > 1 " +
                      " GROUP BY 1,2,3,4,5 ";
                ExecSQL(sql);

                sql = " UPDATE t_report_12_1_6 SET geu = " +
                      " (SELECT TRIM(geu) " +
                       " FROM " + prefData + "s_geu g " +
                       " WHERE g.nzp_geu = t_report_12_1_6.nzp_geu) " +
                      " WHERE pref = '" + pref + "' ";
                ExecSQL(sql);

                sql = " UPDATE t_report_12_1_6 SET area = " +
                      " (SELECT TRIM(area) " +
                       " FROM " + prefData + "s_area a " +
                       " WHERE a.nzp_area = t_report_12_1_6.nzp_area) " +
                      " WHERE pref = '" + pref + "' ";
                ExecSQL(sql);

                sql = " UPDATE t_report_12_1_6 SET supplier = " +
                      " (SELECT TRIM(name_supp) " +
                       " FROM " + prefKernel + "supplier s " +
                       " WHERE s.nzp_supp = t_report_12_1_6.nzp_supp) " +
                      " WHERE pref = '" + pref + "' ";
                ExecSQL(sql);

                sql = " UPDATE t_report_12_1_6 SET service = " +
                      " (SELECT TRIM(service) " +
                       " FROM " + prefKernel + "services s " +
                       " WHERE s.nzp_serv = t_report_12_1_6.nzp_serv) " +
                      " WHERE pref = '" + pref + "' ";
                ExecSQL(sql);
            }
            reader.Close();

            sql = " SELECT " + (_typeGroup == TypeGroup.UK
                              ? "TRIM(area) AS column1, TRIM(geu) AS column2, "
                              : "TRIM(geu) AS column1, TRIM(area) AS column2, ") +
                           " TRIM(supplier) AS supplier, TRIM(service) AS service," +
                           (_typeReport == TypeReport.Charge ? " SUM(sum_nach) " : " SUM(rashod) ") + " AS sum_value " +
                  " FROM t_report_12_1_6 " +
                  " GROUP BY column1, column2, supplier, service " +
                  " ORDER BY column1, column2, supplier, service ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        #region~~~~~~~~~~~~~~~~~~~~~~Фильтр~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        private string GetwhereWp() {
            string wps = String.Empty;
            wps = Territories != null
                ? Territories.Aggregate(wps, (current, nzpWp) => current + (nzpWp + ","))
                : ReportParams.GetRolesCondition(Constants.role_sql_wp);
            wps = wps.TrimEnd(',');

            return !String.IsNullOrEmpty(wps) ? " AND nzp_wp in (" + wps + ")" : String.Empty;
        }

        #endregion~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>Создание временных таблиц</summary>
        protected override void CreateTempTable() {
            string sql = " CREATE TEMP TABLE t_report_12_1_6( " +
                         " pref CHARACTER(20), " +
                         " nzp_geu INTEGER, " +
                         " nzp_area INTEGER, " +
                         " nzp_supp INTEGER, " +
                         " nzp_serv INTEGER, " +
                         " geu CHARACTER(60), " +
                         " area CHARACTER(100), " +
                         " supplier CHARACTER(100)," +
                         " service CHARACTER(100), " +
                         " rashod " + DBManager.sDecimalType + "(14,3), " +
                         " sum_nach " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        /// <summary>Удаление временных таблиц</summary>
        protected override void DropTempTable() {
            ExecSQL("DROP TABLE t_report_12_1_6");
        }
    }
}
