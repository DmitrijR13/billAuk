using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Base.Parameters;
using Bars.KP50.Report.MariEl.Properties;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.MariEl.Reports
{
    class Report120111 : BaseSqlReport
    {
        /// <summary>Наименование отчета</summary>
		public override string Name {
            get { return "12.1.11 Отчет в Соцзащиту"; }
		}

		/// <summary>Описание отчета</summary>
		public override string Description {
            get { return "12.1.11 Отчет в Соцзащиту"; }
		}

		/// <summary>Группа отчета</summary>
		public override IList<ReportGroup> ReportGroups {
			get { return new List<ReportGroup> { 0 }; }
		}

		/// <summary>Предварительный просмотр</summary>
		public override bool IsPreview {
			get { return false; }
		}

		/// <summary>Файл-шаблон отчета</summary>
		protected override byte[] Template {
			get { return Resources.Report_12_1_11; }
		}

		/// <summary>Тип отчета</summary>
		public override IList<ReportKind> ReportKinds {
			get { return new List<ReportKind> { ReportKind.Base }; }
		}

        #region~~~~~~~~~~~~~~~~~~~~~Параметры~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>Территории</summary>
        protected List<int> Banks { get; set; }

        /// <summary>Расчетный месяц</summary>
        private int Month { get; set; }

        /// <summary>Расчетный год</summary>
        private int Year { get; set; }

        /// <summary>Превышение макс. кол-во строк для Excel</summary>
        private bool ExcessForExcel { get; set; }

        #endregion~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>Параметры для отображение на странице браузера</summary>
		/// <returns>Список параметров</returns>
		public override List<UserParam> GetUserParams() {
            return new List<UserParam>
            {
                new BankParameter(),
                new YearParameter {Value = DateTime.Today.Year },
                new MonthParameter {Value = DateTime.Today.Month },
            };
        }

        /// <summary>Заполнить параметры</summary>
        protected override void PrepareParams()
        {
            Banks = UserParamValues["Banks"].GetValue<List<int>>();
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].Value.To<int>();
        }

        /// <summary>Заполнить параметры в отчете</summary>
        /// <param name="report">Объект формы отчета</param>
        protected override void PrepareReport(FastReport.Report report)
        {
			var months = new[]
			{
			 "", "январе" , "феврале", "марте",
				 "апреле" , "мае"    , "июне", 
				 "июле"   , "августе" , "сентябре",
				 "октябре", "ноябре" , "декабре"
			};
            string period = "за " + months[Month] + " " + Year + " г.";
            report.SetParameterValue("period", period);
            report.SetParameterValue("excess_row", ExcessForExcel);
        }

        /// <summary>Выборка данных</summary>
        /// <returns>Кеш данных</returns>
        public override DataSet GetData()
        {
            MyDataReader reader;
            string prefKernel = ReportParams.Pref + DBManager.sKernelAliasRest,
                    prefData = ReportParams.Pref + DBManager.sDataAliasRest;
            string whereWp = GetWhereWp();

            string sql = " SELECT bd_kernel " +
                         " FROM " + prefKernel + "s_point " +
                         " WHERE nzp_wp > 1 " + whereWp;
            ExecRead(out reader, sql);

            while (reader.Read())
            {
                string pref = reader["bd_kernel"].ToStr().ToLower().Trim();
                    string chargeYY = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter +
                            "charge_" + Month.ToString("00");

                if (TempTableInWebCashe(chargeYY))
                {
                    sql = " INSERT INTO t_report_12_1_11(nzp_kvar, nzp_serv, nzp_frm, sum_real, tarif, c_calc, is_device) " +
                          " SELECT nzp_kvar, nzp_serv, nzp_frm, sum_real, tarif, c_calc, is_device " +
                          " FROM " + chargeYY + " c " +
                          " WHERE nzp_serv > 1 " +
                            " AND dat_charge IS NULL ";
                    ExecSQL(sql);
                }
            }
            reader.Close();

            ExecSQL(DBManager.sUpdStat + " t_report_12_1_11");

            sql = " UPDATE t_report_12_1_11 t SET measure = TRIM(m.measure) " +
                  " FROM " + prefKernel + "formuls f INNER JOIN " + prefKernel + "s_measure m ON m.nzp_measure = f.nzp_measure " +
                  " WHERE t.nzp_frm > 0 " +
                    " AND t.nzp_frm = f.nzp_frm ";
            ExecSQL(sql);

            sql = " UPDATE t_report_12_1_11 t SET measure = TRIM(m.measure) " +
                  " FROM " + prefKernel + "services s INNER JOIN " + prefKernel + "s_measure m ON m.nzp_measure = s.nzp_measure " +
                  " WHERE t.nzp_serv = s.nzp_serv ";
            ExecSQL(sql);

            sql = " SELECT DISTINCT TRIM(service) AS service, " +
                         " num_ls AS ls, " +
                         " TRIM(fio) AS fio, " +
                         " TRIM(town) || " +
                         " (CASE WHEN rajon IS NULL " +
                                 " OR TRIM(rajon) = '' " +
                                 " OR TRIM(rajon) = '-' THEN '' ELSE ', ' || TRIM(rajon) END) || " +
                         " (CASE WHEN ulica IS NULL " +
                                 " OR TRIM(ulica) = '' " +
                                 " OR TRIM(ulica) = '-' THEN '' ELSE ', ' || TRIM(ulica) END) || " +
                         " (CASE WHEN ndom IS NULL " +
                                 " OR TRIM(ndom) = '' " +
                                 " OR TRIM(ndom) = '-' THEN '' ELSE ', д. ' || TRIM(ndom) END) || " +
                         " (CASE WHEN nkor IS NULL " +
                                 " OR TRIM(nkor) = '' " +
                                 " OR TRIM(nkor) = '-' THEN '' ELSE ', корп. ' || TRIM(nkor) END) || " +
                         " (CASE WHEN nkvar IS NULL " +
                                 " OR TRIM(nkvar) = '' " +
                                 " OR TRIM(nkvar) = '-' THEN '' ELSE ', кв. ' || TRIM(nkvar) END) || " +
                         " (CASE WHEN nkvar_n IS NULL " +
                                 " OR TRIM(nkvar_n) = '' " +
                                 " OR TRIM(nkvar_n) = '-' THEN '' ELSE ', комн. ' || TRIM(nkvar_n) END) AS address, " +
                         " sum_real, " +
                         " c_calc, " +
                         " tarif, " +
                         " (CASE WHEN is_device > 0 THEN 'по счетчику' ELSE 'по нормативу' END) AS is_device," +
                         " TRIM(measure) AS measure  " +
                  " FROM t_report_12_1_11 g INNER JOIN " + prefData + "kvar k ON k.nzp_kvar = g.nzp_kvar " +
                                          " INNER JOIN " + prefData + "dom d ON d.nzp_dom = k.nzp_dom " +
                                          " INNER JOIN " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                                          " INNER JOIN " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                                          " INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town " +
                                          " INNER JOIN " + prefKernel + "services s ON s.nzp_serv = g.nzp_serv " +
                  " ORDER BY fio ";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            if (dt.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = dt.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(dt.Rows.Remove);
                ExcessForExcel = true;
            }

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        #region~~~~~~~~~~~~~~~~~~~~~~Фильтр~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>Ограничение по банку данных</summary>
        /// <returns>Условие выборки для sql-запроса</returns>
        private string GetWhereWp() {
            string whereWp = String.Empty;
            whereWp = Banks != null
                ? Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","))
                : ReportParams.GetRolesCondition(Constants.role_sql_wp);
            whereWp = whereWp.TrimEnd(',');

            return !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
        }

        #endregion~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>Создание временных таблиц</summary>
        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_report_12_1_11( " +
                         " nzp_kvar INTEGER, " +
                         " nzp_serv INTEGER, " +
                         " nzp_frm INTEGER, " +
                         " measure CHARACTER(20), " +
                         " sum_real " + DBManager.sDecimalType + "(14,2), " +
                         " tarif " + DBManager.sDecimalType + "(14,3), " +
                         " c_calc " + DBManager.sDecimalType + "(14,5), " +
                         " is_device INTEGER) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            ExecSQL(" CREATE INDEX ix_t_report_12_1_11_1 ON t_report_12_1_11(nzp_frm) ");
            ExecSQL(" CREATE INDEX ix_t_report_12_1_11_2 ON t_report_12_1_11(nzp_serv) ");
        }

        /// <summary>Удаление временных таблиц</summary>
        protected override void DropTempTable()
        {
            ExecSQL("DROP TABLE t_report_12_1_11");
        }
    }
}
