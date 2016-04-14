using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Base.Parameters;
using Bars.KP50.Report.Main.Properties;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Main.Reports
{
    class ProverkaKorektZnachParam : BaseSqlReport
    {
        /// <summary>Наименование отчета</summary>
        public override string Name {
            get { return "Базовый - Проверка корректности значений параметров в БД"; }
        }

        /// <summary>Описание отчета</summary>
        public override string Description {
            get { return "Проверка корректности значений параметров в БД"; }
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
            get { return Resources.ProverkaKorektZnachParam; }
        }

        /// <summary>Тип отчета</summary>
        public override IList<ReportKind> ReportKinds {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        #region~~~~~~~~~~~~~~~~~~~~~Параметры~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>Банки данных</summary>
        private List<int> Banks { get; set; }

        /// <summary>Заголовок территории</summary>
        private String TerritoryHeader { get; set; }

        /// <summary>Выполнять ли проверку центрального банка</summary>
        private Byte IsCentralBank { get; set; }

        /// <summary>Список параметров</summary>
        private List<int> ListNzpPrm { get; set; }

        /// <summary>Список типов параметров</summary>
        private List<int> ListNumberPrm
        {
            get
            {
                var returnResult = new List<int>();
                if (_listTypePrm != null)
                    foreach (int typePrm in _listTypePrm)
                    {
                        switch (typePrm)
                        {
                                                                                            //номер prm-таблиц
                            case 1: returnResult.AddRange(new[] { 1, 3, 18, 19 }); break;   //1, 3, 18, 19
                            case 2: returnResult.AddRange(new[] { 2, 4 }); break;           //2, 4
                            case 3: returnResult.Add(7); break;                             //7
                            case 4: returnResult.Add(8); break;                             //8
                            case 5: returnResult.Add(11); break;                            //11
                            case 6: returnResult.Add(9); break;                             //9
                            case 7: returnResult.Add(12); break;                            //12
                            case 8: returnResult.Add(6); break;                             //6
                            case 9: returnResult.Add(17); break;                            //17
                            case 10: returnResult.AddRange(new[] { 5, 10 }); break;         //5, 10
                            case 11: returnResult.Add(13); break;                           //13
                            case 12: returnResult.Add(15); break;                           //15
                        }
                    }
                return returnResult;
            }
            set
            {
                _listTypePrm = value;
            } 
        }

        private List<int> _listTypePrm; 
        #endregion~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>Параметры для отображение на странице браузера</summary>
        /// <returns>Список параметров</returns>
        public override List<UserParam> GetUserParams() {
            return new List<UserParam>
			{
				new BankParameter(),
                new ComboBoxParameter(false)
                {
                    Name = "Центральный банк",
                    Code = "CentralBank",
                    Value = 2,
                    Require = true,
                    StoreData = new List<object>
                    {
                        new { Id = 1, Name = "проверить" },
                        new { Id = 2, Name = "не проверять" }
                    }
                },
                new TypeAndPrmParameter()
			};
        }

        /// <summary>Заполнить параметры</summary>
        protected override void PrepareParams() {
            Banks = UserParamValues["Banks"].GetValue<List<int>>();
            IsCentralBank = UserParamValues["CentralBank"].GetValue<Byte>();
            ListNzpPrm = JsonConvert.DeserializeObject<TypeAndPrmParameterValue>
                            (UserParamValues["TypeAndPrm"].Value.ToString()).NzpPrm;
            ListNumberPrm = JsonConvert.DeserializeObject<TypeAndPrmParameterValue>
                             (UserParamValues["TypeAndPrm"].Value.ToString()).TypePrm;
        }

        /// <summary>Заполнить параметры в отчете</summary>
        /// <param name="report">Объект формы отчета</param>
        protected override void PrepareReport(FastReport.Report report) {
            report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
            report.SetParameterValue("TIME", DateTime.Now.ToShortTimeString());

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader : string.Empty;
            report.SetParameterValue("headerParams", headerParam);
        }

        /// <summary>Выборка данных</summary>
        /// <returns>Кеш данных</returns>
        public override DataSet GetData() {
            MyDataReader reader;
            string centralKernel = ReportParams.Pref + DBManager.sKernelAliasRest;

            string whereWp = GetWhereWp();

            if (IsCentralBank == 1) ValidationValPrm(ReportParams.Pref);

            string sql = " SELECT bd_kernel " +
                         " FROM " + centralKernel + "s_point " +
                         " WHERE nzp_wp > 1 " + whereWp;
            ExecRead(out reader, sql);

            while (reader.Read())
            {
                string pref = reader["bd_kernel"] != DBNull.Value ? reader["bd_kernel"].ToString().Trim() : string.Empty;
                ValidationValPrm(pref);
            }
            reader.Close();

            sql = " SELECT TRIM(name_prm) AS name_prm, " +
                         " TRIM(type_prm) AS type_prm, " +
                         " TRIM(val_prm) AS val_prm, " +
                         " TRIM(name_table_prm) AS name_table_prm, " +
                         " nzp_prm, " +
                         " nzp, " +
                         " nzp_key, " +
                         " TRIM(error) AS error " +
                  " FROM proverka_korekt_znach_param " +
                  " ORDER BY name_prm, nzp_key, nzp, val_prm ";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            if (dt.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = dt.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(dt.Rows.Remove);
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
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            if (!string.IsNullOrEmpty(whereWp))
            {
                if (string.IsNullOrEmpty(TerritoryHeader))
                {
                    TerritoryHeader = string.Empty;
                    string sql = " SELECT point " +
                                 " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                                 " WHERE nzp_wp > 0 " + whereWp;
                    DataTable terrTable = ExecSQLToTable(sql);
                    foreach (DataRow row in terrTable.Rows)
                    {
                        TerritoryHeader += row["point"].ToString().Trim() + ", ";
                    }
                    TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
                }
            }
            return whereWp;
        }

        #endregion~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

        /// <summary>Проверка параметра</summary>
        /// <param name="pref">Префикс банка данных</param>
        private void ValidationValPrm(string pref) {
            if (string.IsNullOrEmpty(pref)) return;

            MyDataReader reader;
            string sql;
            string centralKernel = ReportParams.Pref + DBManager.sKernelAliasRest,
                      localData = pref + DBManager.sDataAliasRest;
            var listPrms = new List<IncorrectPrm>();
            var innPrm = new[] {445, 876, 759, 502};
            var kppPrm = new[] {503, 870, 877, 874};
            const int rsPrm = 1305;
            string whereNzpPrm = ListNzpPrm != null
                ? ListNzpPrm.Aggregate(string.Empty, (current, nzpPrm) => current + (nzpPrm + ","))
                : string.Empty;
            whereNzpPrm = !string.IsNullOrEmpty(whereNzpPrm) ? " AND p.nzp_prm IN (" + whereNzpPrm.TrimEnd(',') + ") " : string.Empty;

            string whereNumPrm = ListNumberPrm.Aggregate(string.Empty, (current, nzpPrm) => current + (nzpPrm + ","));
            whereNumPrm = !string.IsNullOrEmpty(whereNumPrm) ? " WHERE prm_num IN (" + whereNumPrm.TrimEnd(',') + ") " : string.Empty;

            ExecRead(out reader, " SELECT prm_num " +
                                 " FROM " + centralKernel + "prm_table " + whereNumPrm +
                                 " ORDER BY 1");

            while (reader.Read())
            {
                string prmNum = reader["prm_num"].ToString().Trim();
                string prmYY = localData + "prm_" + prmNum;

                if (TempTableInWebCashe(prmYY))
                {
                    sql = " SELECT p.nzp_key, " +
                                 " p.nzp, " +
                                 " p.nzp_prm, " +
                                 " p.val_prm, " +
                                 " pn.type_prm, " +
                                 " pn.low_, " +
                                 " pn.high_, " +
                                 " pn.digits_, " +
                                 " pn.name_prm, " +
                                 " '" + prmYY + "' AS prm_table_name " +
                          " FROM " + prmYY + " p INNER JOIN " + centralKernel + "prm_name pn ON pn.nzp_prm = p.nzp_prm " +
                          " WHERE p.is_actual <> 100 " + whereNzpPrm;
                    DataTable prms = ExecSQLToTable(sql);
                    foreach (DataRow prm in prms.Rows)
                    {
                        int valPrmInt;
                        string low = prm["low_"] != DBNull.Value ? prm["low_"].ToString().Trim() : string.Empty,
                            high = prm["high_"] != DBNull.Value ? prm["high_"].ToString().Trim() : string.Empty;
                        string typePrm = prm["type_prm"] != DBNull.Value
                            ? prm["type_prm"].ToString().Trim()
                            : string.Empty;
                        string valPrm = prm["val_prm"] != DBNull.Value ? prm["val_prm"].ToString().Trim() : string.Empty;
                        if (valPrm == string.Empty)
                        {
                            listPrms.Add(GetIncorrectPrm(prm, "Пустое значение"));
                            continue;
                        }
                        switch (typePrm)
                        {
                            case "bool":
                                        #region bool
                                if (int.TryParse(valPrm, out valPrmInt))
                                {
                                    if (valPrmInt != 0 || valPrmInt != 1)
                                        listPrms.Add(GetIncorrectPrm(prm, "Несоответствие типа "));
                                }
                                else listPrms.Add(GetIncorrectPrm(prm, "Несоответствие типа"));
                                break;
                                        #endregion
                            case "date":
                                        #region date
                                DateTime tempDateTime;
                                if (!DateTime.TryParse(valPrm, out tempDateTime))
                                    listPrms.Add(GetIncorrectPrm(prm, "Несоответствие типа"));
                                break;
                                #endregion
                            case "float":
                                        #region float
                                float valPrmFloat;
                                if (float.TryParse(valPrm, out valPrmFloat))
                                {
                                    float lowFloat, highFloat;
                                    if (!float.TryParse(low, out lowFloat)) lowFloat = float.MinValue;
                                    if (!float.TryParse(high, out highFloat)) highFloat = float.MaxValue;

                                    if (valPrmFloat < lowFloat)
                                        listPrms.Add(GetIncorrectPrm(prm, "Значение меньше допустимого"));
                                    if (valPrmFloat > highFloat)
                                        listPrms.Add(GetIncorrectPrm(prm, "Значение больше допустимого"));
                                }
                                break;
                                #endregion
                            case "int": 
                                        #region int
                                if (int.TryParse(valPrm, out valPrmInt))
                                {
                                    int lowInt, highInt;
                                    if (!int.TryParse(low, out lowInt)) lowInt = int.MinValue;
                                    if (!int.TryParse(high, out highInt)) highInt = int.MaxValue;

                                    if (valPrmInt < lowInt)
                                        listPrms.Add(GetIncorrectPrm(prm, "Значение меньше допустимого"));
                                    if (valPrmInt > highInt)
                                        listPrms.Add(GetIncorrectPrm(prm, "Значение больше допустимого"));
                                }
                                else listPrms.Add(GetIncorrectPrm(prm, "Несоответствие типа"));
                                break;
                                #endregion
                            case "sprav":
                                        #region sprav
                                if (int.TryParse(valPrm, out valPrmInt))
                                {
                                    string nzpPrm = prm["nzp_prm"] != DBNull.Value
                                        ? prm["nzp_prm"].ToString().Trim()
                                        : string.Empty;
                                    sql = " SELECT COUNT(*) " +
                                          " FROM " + centralKernel + "prm_name p INNER JOIN " + 
                                                     centralKernel + "res_y r ON r.nzp_res = p.nzp_res " +
                                          " WHERE p.nzp_prm = " + nzpPrm +
                                          " AND r.nzp_y = " + valPrmInt;
                                    if (Convert.ToUInt16(ExecScalar(sql)) == 0)
                                        listPrms.Add(GetIncorrectPrm(prm,
                                            "Данное значение не входит в допустимый набор вариантов данного типа"));
                                }
                                else listPrms.Add(GetIncorrectPrm(prm, "Несоответствие типа"));
                                break;
                                #endregion
                            case "char":
                                        #region char
                                int tempInt = prm["nzp_prm"] != DBNull.Value ? Convert.ToInt32(prm["nzp_prm"]) : 0;
                                Int64 tempInt64;
                                if (innPrm.Contains(tempInt))
                                {
                                    if (Int64.TryParse(valPrm, out tempInt64))
                                    {
                                        if (valPrm.Length != 10 && valPrm.Length != 12)
                                            listPrms.Add(GetIncorrectPrm(prm, "Несоответствие разрядности"));
                                    }
                                    else listPrms.Add(GetIncorrectPrm(prm, "Несоответствие типа"));
                                }
                                if (kppPrm.Contains(tempInt))
                                {
                                    if (Int64.TryParse(valPrm, out tempInt64))
                                    {
                                        if (valPrm.Length != 10)
                                            listPrms.Add(GetIncorrectPrm(prm, "Несоответствие разрядности"));
                                    }
                                    else listPrms.Add(GetIncorrectPrm(prm, "Несоответствие типа"));
                                }
                                if (rsPrm == tempInt)
                                {
                                    if (Int64.TryParse(valPrm, out tempInt64))
                                    {
                                        if (valPrm.Length != 20)
                                            listPrms.Add(GetIncorrectPrm(prm, "Несоответствие разрядности"));
                                    }
                                    else listPrms.Add(GetIncorrectPrm(prm, "Несоответствие типа"));
                                }
                                break;
                                #endregion
                        }
                    }
                }
            }
            reader.Close();

            foreach (var prm in listPrms)
            {
                sql = " INSERT INTO proverka_korekt_znach_param(name_prm, type_prm, val_prm, name_table_prm, nzp_prm, nzp, nzp_key, error) " +
                      " VALUES ('" + prm.NamePrm.Trim('\'') + "', '" +
                                     prm.TypePrm + "', '" +
                                     prm.ValPrm + "', '" +
                                     prm.NameTablePrm + "', " +
                                     prm.NzpPrm + ", " +
                                     prm.Nzp + ", " +
                                     prm.NzpKey + ", '" +
                                     prm.Error + "') ";
                ExecSQL(sql);
            }
        }

        /// <summary>Заполнение параметра</summary>
        /// <param name="prm">Строка данных</param>
        /// <param name="error">Причина некорректности</param>
        /// <returns>Параметр</returns>
        private IncorrectPrm GetIncorrectPrm(DataRow prm, string error) {
            int tempInt;
            var incorrectPrm = new IncorrectPrm
            {
                NzpKey = prm["nzp_key"] != DBNull.Value
                    ? Int32.TryParse(prm["nzp_key"].ToString().Trim(), out tempInt)
                        ? tempInt
                        : 0
                    : 0,
                Nzp = prm["nzp"] != DBNull.Value
                    ? Int32.TryParse(prm["nzp"].ToString().Trim(), out tempInt) ? tempInt : 0
                    : 0,
                NzpPrm = prm["nzp_prm"] != DBNull.Value
                    ? Int32.TryParse(prm["nzp_prm"].ToString().Trim(), out tempInt) ? tempInt : 0
                    : 0,
                ValPrm = prm["val_prm"] != DBNull.Value
                    ? prm["nzp_prm"].ToString().Trim()
                    : string.Empty,
                TypePrm = prm["type_prm"] != DBNull.Value
                    ? prm["type_prm"].ToString().Trim()
                    : string.Empty,
                NamePrm = prm["name_prm"] != DBNull.Value
                    ? prm["name_prm"].ToString().Trim()
                    : string.Empty,
                NameTablePrm = prm["prm_table_name"] != DBNull.Value
                    ? prm["prm_table_name"].ToString().Trim()
                    : string.Empty,
                Error = error
            };
            return incorrectPrm;
        }

        /// <summary>Создание временных таблиц</summary>
        protected override void CreateTempTable() {
            const string sql = " CREATE TEMP TABLE proverka_korekt_znach_param(" +
                               " name_prm CHARACTER(100), " +
                               " type_prm CHARACTER(10), " +
                               " val_prm CHARACTER(250), " +
                               " name_table_prm CHARACTER(100), " +
                               " nzp_prm INTEGER, " +
                               " nzp INTEGER, " +
                               " nzp_key INTEGER, " +
                               " error CHARACTER(100))" + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        /// <summary>Удаление временных таблиц</summary>
        protected override void DropTempTable() {
            ExecSQL("DROP TABLE proverka_korekt_znach_param");
        }

        /// <summary>Некорректный параметр</summary>
        private class IncorrectPrm
        {
            /// <summary>Id записи</summary>
            public Int32 NzpKey { get; set; }

            /// <summary>Id объекта</summary>
            public Int32 Nzp { get; set; }

            /// <summary>Id параметра</summary>
            public Int32 NzpPrm { get; set; }

            /// <summary>Значение параметра</summary>
            public string ValPrm { get; set; }

            /// <summary>Тип параметра</summary>
            public string TypePrm { get; set; }

            /// <summary>Название параметра</summary>
            public string NamePrm { get; set; }

            /// <summary>Полное название таблицы</summary>
            public string NameTablePrm { get; set; }

            /// <summary>Тип ошибки</summary>
            public string Error { get; set; }

            public IncorrectPrm() {
                NzpKey = 0;
                Nzp = 0;
                NzpPrm = 0;
                ValPrm = string.Empty;
                TypePrm = string.Empty;
                NamePrm = string.Empty;
                NameTablePrm = string.Empty;
                Error = string.Empty;
            }
        }

    }
}
