using System;
using System.Collections.Generic;
using System.Data;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Tula.Properties;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.Report.Tula.Reports
{
    class Report71126 : BaseSqlReport
    {
        /// <summary>Наименование отчета</summary>
        public override string Name {
            get { return "71.1.26 Контрольный лицензионный отчет"; }
        }

        /// <summary>Описание отчета</summary>
        public override string Description {
            get { return "71.1.26 Контрольный лицензионный отчет"; }
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
            get { return Resources.Report_71_1_26; }
        }

        /// <summary>Тип отчета</summary>
        public override IList<ReportKind> ReportKinds {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        #region~~~~~~~~~~~~~~~~~~~~~Параметры~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary> Год </summary>
        private Int32 Year { get; set; }

        /// <summary> Месяц </summary>
        private Int32 Month { get; set; }

        /// <summary> Кол-во лицевых счетов </summary>
        private Int32 CountLs { get; set; }

        #endregion~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>Параметры для отображение на странице браузера</summary>
        /// <returns>Список параметров</returns>
        public override List<UserParam> GetUserParams() {
            return new List<UserParam>
			{
				new YearParameter(),
                new MonthParameter()
			};
        }

        /// <summary>Заполнить параметры</summary>
        protected override void PrepareParams()
        {
            Year = UserParamValues["Year"].GetValue<int>();
            Month = UserParamValues["Month"].GetValue<int>();
        }

        /// <summary>Заполнить параметры в отчете</summary>
        /// <param name="report">Объект формы отчета</param>
        protected override void PrepareReport(FastReport.Report report)
        {
            var month = new[] {"" , "Январь", "Февраль", "Март", 
                                    "Апрель", "Май",     "Июнь", 
                                    "Июль",   "Август",  "Сентябрь", 
                                    "Октябрь","Ноябрь",  "Декабрь"};
            report.SetParameterValue("month", month[Month] + " " + Year + "г.");
            report.SetParameterValue("count_ls", CountLs);
        }

        /// <summary>Выборка данных</summary>
        /// <returns>Кеш данных</returns>
        public override DataSet GetData() {
            MyDataReader reader;
            string centralKernel = ReportParams.Pref + DBManager.sKernelAliasRest;

            string sql = " SELECT bd_kernel " +
                         " FROM " + centralKernel + "s_point " +
                         " WHERE nzp_wp > 1 " ;
            ExecRead(out reader, sql);

            while (reader.Read())
            {
                string pref = reader["bd_kernel"].ToStr().ToLower().Trim();

                    string chargeYY = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter +
                                              "charge_" + Month.ToString("00");
                if (TempTableInWebCashe(chargeYY))
                    CountLs += Convert.ToInt32(ExecScalar(" SELECT COUNT(DISTINCT nzp_kvar) " +
                                                           " FROM " + chargeYY + 
                                                           " WHERE dat_charge IS NULL "));
            }
            reader.Close();

            var ds = new DataSet();
            ds.Tables.Add(new DataTable());

            return ds;
        }

        /// <summary>Создание временных таблиц</summary>
        protected override void CreateTempTable() {
        }

        /// <summary>Удаление временных таблиц</summary>
        protected override void DropTempTable() {
        }
    }
}
