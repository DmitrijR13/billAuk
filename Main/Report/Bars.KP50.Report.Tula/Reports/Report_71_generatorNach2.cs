using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Tula.Properties;
using Bars.KP50.Utils;
using FastReport;
using Newtonsoft.Json;
using STCLINE.KP50;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Utility;

namespace Bars.KP50.Report.Tula.Reports
{
    class Report71GenNach2 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71. Генератор по начислениям и квартирным параметрам"; }
        }

        public override string Description
        {
            get { return "Генератор по начислениям и квартирным параметрам"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Finans };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources._71_Gen_nach2; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
        }
        #region Параметры

        /// <summary>Расчетный месяц</summary>
        protected int MonthS { get; set; }

        /// <summary>Расчетный год</summary>
        protected int YearS { get; set; }


        /// <summary>Расчетный месяц</summary>
        protected int MonthPo { get; set; }

        /// <summary>Расчетный год</summary>
        protected int YearPo { get; set; }


        /// <summary>Услуги</summary>ServiceParameterValue
        private ServiceParameterValue Services { get; set; }
        /// <summary>Услуги</summary>
        private ServiceParameterValue FilteredServices { get; set; }

        /// <summary>Районы </summary>
        private List<int> RaionsDoms { get; set; }

        /// <summary>Поставщики</summary>
        private string SupplierHeader { get; set; }

        /// <summary>Услуги</summary>
        private string ServiceHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Параметры</summary>
        private List<long> Params { get; set; }

        /// <summary>Статус ЛС</summary>
        private List<int> StatusLs { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }

        /// <summary>Превышение </summary>
        private bool RowCount { get; set; }

        /// <summary>Районы </summary>
        private List<int> Raions { get; set; }

        /// <summary>ИПУ </summary>
        private int Ipu { get; set; }

        /// <summary>ОДПУ </summary>
        private int Odpu { get; set; }

        /// <summary>Признак расчета </summary>
        private int Kind { get; set; }

        /// <summary>Учитывать все ЛС в доме</summary>
        private int AllLs { get; set; }

        /// <summary>Учитывать все ЛС в доме</summary>
        private int StatusGil { get; set; }

        /// <summary>Учитывать все ЛС в доме</summary>
        private int Privat { get; set; }

        private int stringsInPack = 50000;

        private bool isArchive = false;

        private readonly string[] _months = {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};

        #endregion
        protected override Stream GetTemplate()
        {
            if (UserParamValues["Format"].GetValue<int>() == 1)
                return new MemoryStream(Resources._71_Gen_nach2_noformat);

            return new MemoryStream(Template);
        }

        public override List<UserParam> GetUserFilters()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Name = "Месяц с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new MonthParameter {Name = "Месяц по", Code = "Month1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год по", Code = "Year1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new BankSupplierParameter(),
                new ServiceParameter(true),
                new RaionsDomsParameter(),
                new RaionsParameter { Name = "Населенный пункт" },
                new ComboBoxParameter(true)
                {
                    Name = "Статус ЛС",
                    Code = "StatusLS",
                    StoreData = new List<object>
                    {
                        new { Id = 1, Name = "Открытый" },
                        new { Id = 2, Name = "Закрытый" },
                        new { Id = 3, Name = "Неопределен" }
                    }
                },
                new ComboBoxParameter(false)
                {
                    Name = "Наличие ИПУ",
                    Code = "IPU",
                    Value = 3,
                    StoreData = new List<object>
                    {
                        new { Id = 1, Name = "Есть" },
                        new { Id = 2, Name = "Нет" },
                        new { Id = 3, Name = "Неопределено" }
                    }
                },
                new ComboBoxParameter(false)
                {
                    Name = "Наличие ОДПУ",
                    Code = "ODPU",
                    Value = 3,
                    StoreData = new List<object>
                    {
                        new { Id = 1, Name = "Есть" },
                        new { Id = 2, Name = "Нет" },
                        new { Id = 3, Name = "Неопределено" }
                    }
                },
                new ComboBoxParameter(false)
                {
                    Name = "Признак расчета",
                    Code = "Kind",
                    Value = 6,
                    StoreData = new List<object>
                    {
                        new { Id = 1, Name = "Норматив" },
                        new { Id = 2, Name = "Показание ПУ" },
                        new { Id = 3, Name = "Среднемесячное потребление по ПУ" },
                        new { Id = 4, Name = "Исходя из показаний ОДПУ" },
                        new { Id = 5, Name = "Расчетный способ для нежилых помещений" },
                        new { Id = 6, Name = "Неопределен" }
                    }
                },                
                //new ComboBoxParameter(false)
                //{
                //    Name = "Статус Жилья",
                //    Code = "StatusGil",
                //    Value = 3,
                //    StoreData = new List<object>
                //    {
                //        new { Id = 1, Name = "Жилое" },
                //        new { Id = 2, Name = "Нежилое" },
                //        new { Id = 3, Name = "Неопределено" }
                //    }
                //},                
                new ComboBoxParameter(false)
                {
                    Name = "Приватизировано",
                    Code = "Privat",
                    Value = 3,
                    StoreData = new List<object>
                    {
                        new { Id = 1, Name = "Да" },
                        new { Id = 2, Name = "Нет" },
                        new { Id = 3, Name = "Неопределено" }
                    }
                }
            };
        }

        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new ServiceParameter(true),
                new ComboBoxParameter(true) {
                    Name = "Параметры отчета", 
                    Code = "Params",
                    Value = 1,
                    Require = true,
                    StoreData = new List<object> {
                        new { Id = 38, Name = "Банк данных"},
                        new { Id = 39, Name = "Месяц"},
                        new { Id = 1, Name = "Территория (УК)"},
                        new { Id = 2, Name = "ЖЭУ"},
                        new { Id = 3, Name = "Участок"},
                        new { Id = 4, Name = "Статус ЛС"},
                        new { Id = 5, Name = "Наличие ИПУ"},
                        new { Id = 6, Name = "Наличие ОДПУ"},
                        new { Id = 7, Name = "Признак расчета"},
                        new { Id = 8, Name = "Домовой норматив на 1 ГКал/кв.м отопления"},
                        new { Id = 900, Name = "Район"},
                        new { Id = 9, Name = "Населенный пункт"},
                        new { Id = 10, Name = "Улица"},
                        new { Id = 11, Name = "Дом"},
                        new { Id = 12, Name = "Квартира"},
                        new { Id = 13, Name = "Лицевой счет"},
                        new { Id = 14, Name = "ФИО"},
                        new { Id = 15, Name = "Количество прописаных"},
                        new { Id = 16, Name = "Количество временно проживающих"},
                        new { Id = 17, Name = "Количество временно выбывших"},
                        new { Id = 18, Name = "Общая площадь"},
                        new { Id = 19, Name = "Отапливаемая площадь"},
                        new { Id = 20, Name = "Жилая площадь"},
                        new { Id = 21, Name = "Количество комнат"},
                        new { Id = 22, Name = "Этаж"},
                        new { Id = 23, Name = "Платежный код"},
                        new { Id = 24, Name = "Поставщик"},
                        new { Id = 25, Name = "Услуга"},
                        new { Id = 26, Name = "Входящее сальдо"},
                        new { Id = 27, Name = "Тариф"},
                        new { Id = 28, Name = "Расход"},
                        new { Id = 29, Name = "Недопоставка"},
                        new { Id = 30, Name = "Начислено с учетом недопоставки"},
                        new { Id = 31, Name = "К оплате"},
                        new { Id = 321, Name = "Оплачено"},
                        new { Id = 322, Name = "В т.ч. оплаты предыдущих биллинговых систем"},
                        new { Id = 323, Name = "Оплата напрямую поставщикам"},
                        new { Id = 331, Name = "Корректировка начислений"},
                        new { Id = 332, Name = "Корректировка входящего сальдо"},
                        new { Id = 34, Name = "Начислено"},
                        new { Id = 35, Name = "Перерасчет"},
                        new { Id = 36, Name = "Исходящее сальдо"},
                        new { Id = 37, Name = "Приватизировано"}
                    }
                },
                new ComboBoxParameter()
                {                    
                    Name = "Учитывать все ЛС в доме",
                    Code = "AllLs",
                    Value = 2,
                    StoreData = new List<object>
                    {
                        new { Id = 1, Name = "Да" },
                        new { Id = 2, Name = "Нет" }
                    }
                },
                new ComboBoxParameter(false)
                {
                    Name = "Без форматирования",
                    Code = "Format",
                    Value = 2,
                    StoreData = new List<object>
                    {
                        new { Id = 1, Name = "Да" },
                        new { Id = 2, Name = "Нет" }
                    }
                },
            };
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            string statusLs = string.Empty;
            if (StatusLs != null)
            {
                statusLs = StatusLs.Contains(1) ? "Открытый, " : string.Empty;
                statusLs += StatusLs.Contains(2) ? "Закрытый, " : string.Empty;
                statusLs += StatusLs.Contains(3) ? "Неопределен, " : string.Empty;
                statusLs = statusLs.TrimEnd(',', ' ');
            }
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("dat", DateTime.Now.ToLongDateString());
            report.SetParameterValue("time", DateTime.Now.ToLongTimeString());
            if (MonthS == MonthPo && YearS == YearPo)
            {
                report.SetParameterValue("pPeriod", months[MonthS] + " " + YearS + "г.");
            }
            else
            {
                report.SetParameterValue("pPeriod", "c " + months[MonthS] + " " + YearS + "г. по " + months[MonthPo] + " " + YearPo + "г.");
            }

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(statusLs) ? "Статус ЛС: " + statusLs + "\n" : string.Empty;
            headerParam += Ipu != 3 ? "Наличие ИПУ по услуге: " + (Ipu == 1 ? "Да" : "Нет") + "\n" : string.Empty;
            headerParam += Odpu != 3 ? "Наличие ОДПУ по услуге: " + (Odpu == 1 ? "Да" : "Нет") + "\n" : string.Empty;
            if (Kind != 6)
                switch (Kind)
                {
                    case 1: headerParam += "Признак расчета: Норматив \n"; break;
                    case 2: headerParam += "Признак расчета: Показание ПУ \n"; break;
                    case 3: headerParam += "Признак расчета: Среднемесячное потребление по ПУ \n"; break;
                    case 4: headerParam += "Признак расчета: Исходя из показаний ОДПУ \n"; break;
                    case 5: headerParam += "Признак расчета: Расчетный способ для нежилых помещений \n"; break;
                }
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(ServiceHeader) ? "Услуги: " + ServiceHeader : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);
            report.SetParameterValue("excel",
                RowCount
                    ? "Выборка записей ограничена первыми 40000 строками. Выберите другой формат экспортируемого файла, либо поставьте другие ограничения для отчета"
                    : "");
        }

        protected override void PrepareParams()
        {
            //Фильтры
            MonthS = UserFilterValues["Month"].GetValue<int>();
            YearS = UserFilterValues["Year"].Value.To<int>();
            MonthPo = UserFilterValues["Month1"].GetValue<int>();
            YearPo = UserFilterValues["Year1"].Value.To<int>();
            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserFilterValues["BankSupplier"].Value.ToString());
            StatusLs = UserFilterValues["StatusLS"].GetValue<List<int>>();
            RaionsDoms = UserFilterValues["RaionsDoms"].GetValue<List<int>>();
            Raions = UserFilterValues["Raions"].GetValue<List<int>>();
            FilteredServices = JsonConvert.DeserializeObject<ServiceParameterValue>(UserFilterValues["Services"].Value.ToString());
            Ipu = UserFilterValues["IPU"].GetValue<int>();
            Odpu = UserFilterValues["ODPU"].GetValue<int>();
            Kind = UserFilterValues["Kind"].GetValue<int>();
            Privat = UserFilterValues["Privat"].GetValue<int>();
            //Параметры
            Services = JsonConvert.DeserializeObject<ServiceParameterValue>(UserParamValues["Services"].Value.ToString());
            Params = UserParamValues["Params"].GetValue<List<long>>();
            if (Params.Contains(12))
            {
                if (!Params.Contains(11)) { Params.Add(11); }
                if (!Params.Contains(10)) { Params.Add(10); }
                if (!Params.Contains(9)) { Params.Add(9); }
            }
            if (Params.Contains(11))
            {
                if (!Params.Contains(10)) { Params.Add(10); }
                if (!Params.Contains(9)) { Params.Add(9); }
            }
            if (Params.Contains(10))
            {
                if (!Params.Contains(9)) { Params.Add(9); }
            }
            AllLs = UserParamValues["AllLs"].GetValue<int>();
        }

        public override DataSet GetData()
        {
            var sql = new StringBuilder();
            MyDataReader reader;

            string whereSupp = GetWhereSupp("c.nzp_supp");
            string whereServFilter = GetWhereServFilter();
            string whereServ = GetWhereServ();
            string whereRaj = GetWhereRaj("u.");
            string whereRajDom = GetWhereRajDom("d.");
            bool listLc = GetSelectedKvars();


            CreateTSvod();



            sql.Remove(0, sql.Length);
            sql.Append(" select bd_kernel as pref, point " +
                  " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                  " where nzp_wp>1 " + GetwhereWp());
            ExecRead(out reader, sql.ToString());
            while (reader.Read())
            {

                string pref = reader["pref"].ToStr().Trim();
                string point = reader["point"].ToStr().Trim();
                string kvarTable = pref + DBManager.sDataAliasRest + "kvar ";
                string domTable = pref + DBManager.sDataAliasRest + "dom ";
                string ulTable = pref + DBManager.sDataAliasRest + "s_ulica ";
                string rajTable = pref + DBManager.sDataAliasRest + "s_rajon ";
                string rajDomTable = ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon_dom ";
                for (int i = YearS * 12 + MonthS; i < YearPo * 12 + MonthPo + 1; i++)
                {
                    int mo = i % 12;
                    int ye = mo == 0 ? (i / 12) - 1 : (i / 12);
                    if (mo == 0) mo = 12;
                    string sumInsaldo = ((mo == MonthS) & (ye == YearS)) ? "sum_insaldo" : "0";
                    string sumOutsaldo = ((mo == MonthPo) & (ye == YearPo)) ? "sum_outsaldo" : "0";

                    string DatS = "1." + mo + "." + ye,
                        DatPo = DateTime.DaysInMonth(ye, mo) + "." + mo + "." + ye;

                    string chargeTable = pref + "_charge_" + (ye - 2000).ToString("00") + DBManager.tableDelimiter +
                                         "charge_" + mo.ToString("00");
                    string prmTable = pref + DBManager.sDataAliasRest + "prm_1 ";
                    string perekidka = pref + "_charge_" + (ye - 2000).ToString("00") + DBManager.tableDelimiter +
                                       "perekidka";
                    string fromSupplier = pref + "_charge_" + (ye - 2000).ToString("00") + DBManager.tableDelimiter +
                                          "from_supplier ";

                    #region Дополнительные ограничения

                    ExecSQL(" DELETE FROM t_report_71_Nach2 ");
                    if (StatusLs != null)
                    {
                        string stat = string.Empty;
                        stat = " AND val_prm in ( ";
                        stat += StatusLs.Contains(1) ? "'1', " : string.Empty;
                        stat += StatusLs.Contains(2) ? "'2', " : string.Empty;
                        stat += StatusLs.Contains(3) ? "'3', " : string.Empty;
                        stat = stat.TrimEnd(',', ' ');
                        stat += ")";
                        {
                            sql.Remove(0, sql.Length);
                            sql.Append(" INSERT INTO t_report_71_Nach2 ( nzp_kvar ) " +
                                       " SELECT DISTINCT nzp" +
                                       " FROM " + pref + DBManager.sDataAliasRest + "prm_3 " +
                                       " WHERE dat_s <= '" + DatPo + "' " +
                                       " AND dat_po >= '" + DatS + "' " +
                                       " AND is_actual = 1 " +
                                       " AND nzp_prm = 51 " +
                                       stat);
                            ExecSQL(sql.ToString());
                        }
                    }
                    else
                    {
                        sql.Remove(0, sql.Length);
                        sql.Append(" INSERT INTO t_report_71_Nach2 ( nzp_kvar ) " +
                                   " SELECT DISTINCT nzp_kvar " +
                                   " FROM " + pref + DBManager.sDataAliasRest + "kvar k, " +
                                   pref + DBManager.sDataAliasRest + "dom d, " +
                                   pref + DBManager.sDataAliasRest + "s_ulica u " +
                                   " WHERE k.nzp_dom = d.nzp_dom " +
                                   " AND d.nzp_ul = u.nzp_ul ");
                        ExecSQL(sql.ToString());
                    }

                    if (Ipu != 3)
                    {
                        if (Ipu == 1)
                        {
                            sql.Remove(0, sql.Length);
                            sql.Append(" DELETE FROM t_report_71_Nach2 a " +
                                       " WHERE a.nzp_kvar NOT IN ( " +
                                       " SELECT nzp FROM  " + pref + DBManager.sDataAliasRest + "counters_spis c " +
                                       " WHERE c.nzp = a.nzp_kvar " +
                                       (!string.IsNullOrEmpty(whereServFilter) ? " AND " + whereServFilter : "") +
                                       " AND nzp_type = 3 " +
                                       " AND is_actual = 1) ");
                            ExecSQL(sql.ToString());
                        }
                        if (Ipu == 2)
                        {
                            sql.Remove(0, sql.Length);
                            sql.Append(" DELETE FROM t_report_71_Nach2 a " +
                                       " WHERE a.nzp_kvar IN ( " +
                                       " SELECT nzp FROM  " + pref + DBManager.sDataAliasRest + "counters_spis c " +
                                       " WHERE c.nzp = a.nzp_kvar " +
                                       (!string.IsNullOrEmpty(whereServFilter) ? " AND " + whereServFilter : "") +
                                       " AND nzp_type = 3 " +
                                       " AND is_actual = 1) ");
                            ExecSQL(sql.ToString());
                        }
                    }

                    if (Odpu != 3)
                    {
                        sql.Remove(0, sql.Length);
                        sql.Append(" INSERT INTO t_doms(nzp_dom) " +
                                   " SELECT DISTINCT nzp_dom " +
                                   " FROM  " + pref + DBManager.sDataAliasRest + "kvar k, t_report_71_Nach2 t  " +
                                   " WHERE k.nzp_kvar = t.nzp_kvar ");
                        ExecSQL(sql.ToString());

                        if (Odpu == 1)
                        {
                            sql.Remove(0, sql.Length);
                            sql.Append(" DELETE FROM t_report_71_Nach2 a " +
                                       " WHERE a.nzp_kvar NOT IN ( " +
                                       " SELECT k.nzp_kvar FROM  " + pref + DBManager.sDataAliasRest +
                                       "counters_spis c, t_doms d, " +
                                       pref + DBManager.sDataAliasRest + "kvar k " +
                                       " WHERE c.nzp = d.nzp_dom " +
                                       " AND k.nzp_dom = d.nzp_dom " +
                                       (!string.IsNullOrEmpty(whereServFilter) ? " AND " + whereServFilter : "") +
                                       " AND nzp_type = 1 " +
                                       " AND is_actual = 1) ");
                            ExecSQL(sql.ToString());
                        }
                        if (Odpu == 2)
                        {
                            sql.Remove(0, sql.Length);
                            sql.Append(" DELETE FROM t_report_71_Nach2 a " +
                                       " WHERE a.nzp_kvar IN ( " +
                                       " SELECT nzp FROM  " + pref + DBManager.sDataAliasRest +
                                       "counters_spis c, t_doms d, " +
                                       pref + DBManager.sDataAliasRest + "kvar k " +
                                       " WHERE c.nzp = d.nzp_dom " +
                                       " AND k.nzp_dom = d.nzp_dom " +
                                       (!string.IsNullOrEmpty(whereServFilter) ? " AND " + whereServFilter : "") +
                                       " AND nzp_type = 1 " +
                                       " AND is_actual = 1) ");
                            ExecSQL(sql.ToString());
                        }
                        ExecSQL(" DELETE FROM t_doms ");
                    }

                    if (whereServFilter != "")
                    {
                        sql.Remove(0, sql.Length);
                        sql.Append(" DELETE FROM t_report_71_Nach2  " +
                                   " WHERE nzp_kvar NOT IN ( " +
                                   " SELECT DISTINCT nzp_kvar FROM  " + chargeTable + " WHERE " + whereServFilter + ") ");
                        ExecSQL(sql.ToString());
                    }

                    if (Privat != 3)
                    {
                        sql.Remove(0, sql.Length);
                        sql.Append(" DELETE FROM t_report_71_Nach2  " +
                                   " WHERE nzp_kvar NOT IN ( " +
                                   " SELECT DISTINCT nzp FROM  " + prmTable +
                                   " WHERE dat_s <= '" + DatPo + "' " +
                                   " AND dat_po >= '" + DatS + "' " +
                                   " AND is_actual = 1 " +
                                   " AND nzp_prm = 8 " +
                                   " AND val_prm " + (Privat == 1 ? " = '1' " : " <> '1' ") + ")");
                        ExecSQL(sql.ToString());
                    }

                    //должен быть в конце всех фильтров!
                    if (AllLs == 1)
                    {
                        sql.Remove(0, sql.Length);
                        sql.Append(" INSERT INTO t_doms(nzp_dom) " +
                                   " SELECT DISTINCT nzp_dom " +
                                   " FROM  " + pref + DBManager.sDataAliasRest + "kvar k, t_report_71_Nach2 t  " +
                                   " WHERE k.nzp_kvar = t.nzp_kvar ");
                        ExecSQL(sql.ToString());

                        ExecSQL(" DELETE FROM t_report_71_Nach2 ");

                        sql.Remove(0, sql.Length);
                        sql.Append(" INSERT INTO t_report_71_Nach2 (nzp_kvar) " +
                                   " SELECT k.nzp_kvar FROM " +
                                   pref + DBManager.sDataAliasRest + "kvar k, t_doms d " +
                                   " WHERE k.nzp_dom = d.nzp_dom ");
                        ExecSQL(sql.ToString());

                        ExecSQL(" DELETE FROM t_doms ");
                    }

                    #endregion

                    //if (TempTableInWebCashe(chargeTable) && whereServFilter != "")
                    //    ExecSQL(DBManager.SetTempTable(" SELECT * FROM " + chargeTable + " WHERE " + whereServ + whereSupp, "filtered_charge"));


                    if (TempTableInWebCashe(kvarTable) && TempTableInWebCashe(domTable) && TempTableInWebCashe(ulTable) &&
                        TempTableInWebCashe(chargeTable) && TempTableInWebCashe(prmTable))
                    {
                        var groupby = string.Empty;
                        sql.Remove(0, sql.Length);
                        sql.Append(" insert into t_svod_mini( ");
                        if (Params.Contains(38) || Params.Contains(331) || Params.Contains(332)) { sql.Append(" pref, "); groupby += "pref, "; }
                        if (Params.Contains(39) || Params.Contains(331) || Params.Contains(332)) { sql.Append(" monthyear, "); groupby += "monthyear, "; }
                        if (Params.Contains(1)) { sql.Append(" nzp_area, "); groupby += "k.nzp_area, "; }
                        if (Params.Contains(2)) { sql.Append(" nzp_geu, "); groupby += "k.nzp_geu, "; }
                        if (Params.Contains(3)) { sql.Append(" uch, "); groupby += "uch, "; }
                        if (Params.Contains(7)) { sql.Append(" is_device, typek, "); groupby += "is_device, typek, "; }
                        if (Params.Contains(900)) { sql.Append(" nzp_raj_dom, "); groupby += "nzp_raj_dom, "; }
                        if (Params.Contains(9)) { sql.Append(" nzp_town, nzp_raj, "); groupby += "r.nzp_town, r.nzp_raj, "; }
                        if (Params.Contains(10)) { sql.Append(" nzp_ul, "); groupby += "u.nzp_ul, "; }
                        if (Params.Contains(11) || Params.Contains(8)) { sql.Append(" nzp_dom, "); groupby += "d.nzp_dom, "; }
                        sql.Append(" nzp_kvar, "); groupby += "k.nzp_kvar, ";
                        if (Params.Contains(13)) { sql.Append(" num_ls, "); groupby += "k.num_ls, "; }
                        if (Params.Contains(14)) { sql.Append(" fio, "); groupby += "k.fio, "; }
                        if (Params.Contains(23)) { sql.Append(" pkod, "); groupby += "pkod, "; }
                        if (Params.Contains(24) || Params.Contains(331) || Params.Contains(332)) { sql.Append(" nzp_supp, "); groupby += "c.nzp_supp, "; }
                        if (Params.Contains(25) || Params.Contains(7) || Params.Contains(331) || Params.Contains(332))
                        { sql.Append(" nzp_serv, "); groupby += "c.nzp_serv, "; }
                        if (Params.Contains(26)) { sql.Append(" sum_insaldo, "); }
                        if (Params.Contains(27)) { sql.Append(" tarif, "); groupby += "tarif, "; }
                        if (Params.Contains(28)) { sql.Append(" c_calc, "); }
                        if (Params.Contains(29)) { sql.Append(" sum_nedop, "); }
                        if (Params.Contains(30)) { sql.Append(" sum_tarif, "); }
                        if (Params.Contains(31)) { sql.Append(" sum_charge, "); }
                        if (Params.Contains(321)) { sql.Append(" money_to, "); }
                        if (Params.Contains(322)) { sql.Append(" money_from, "); }
                        if (Params.Contains(34)) { sql.Append(" rsum_tarif, "); }
                        if (Params.Contains(35)) { sql.Append(" reval, "); }
                        if (Params.Contains(36)) { sql.Append(" sum_outsaldo, "); }
                        sql.Remove(sql.Length - 2, 2);
                        sql.Append(") ");
                        sql.Append(" select ");
                        if (Params.Contains(38) || Params.Contains(331) || Params.Contains(332)) { sql.Append(" '" + point + "' AS pref, "); }
                        if (Params.Contains(39) || Params.Contains(331) || Params.Contains(332)) { sql.Append(" '" + _months[mo] + " " + ye + "' AS monthyear, "); }
                        if (Params.Contains(1)) { sql.Append(" k.nzp_area, "); }
                        if (Params.Contains(2)) { sql.Append(" k.nzp_geu, "); }
                        if (Params.Contains(3)) { sql.Append(" uch, "); }
                        if (Params.Contains(7)) { sql.Append(" is_device, typek, "); }
                        if (Params.Contains(900)) { sql.Append(" nzp_raj_dom, "); }
                        if (Params.Contains(9)) { sql.Append(" r.nzp_town, r.nzp_raj, "); }
                        if (Params.Contains(10)) { sql.Append(" u.nzp_ul, "); }
                        if (Params.Contains(11) || Params.Contains(8))
                        { sql.Append(" d.nzp_dom, "); }
                        sql.Append(" k.nzp_kvar, ");
                        if (Params.Contains(13)) { sql.Append(" k.num_ls, "); }
                        if (Params.Contains(14)) { sql.Append(" k.fio, "); }
                        if (Params.Contains(23)) { sql.Append(" pkod, "); }
                        if (Params.Contains(24) || Params.Contains(331) || Params.Contains(332)) { sql.Append(" c.nzp_supp, "); }
                        if (Params.Contains(25) || Params.Contains(7) || Params.Contains(331) || Params.Contains(332)) { sql.Append(" c.nzp_serv, "); }
                        if (Params.Contains(26)) { sql.Append(" SUM(" + sumInsaldo + "), "); }
                        if (Params.Contains(27)) { sql.Append(" tarif, "); }
                        if (Params.Contains(28)) { sql.Append(" SUM(c_calc), "); }
                        if (Params.Contains(29)) { sql.Append(" SUM(sum_nedop), "); }
                        if (Params.Contains(30)) { sql.Append(" SUM(sum_tarif), "); }
                        if (Params.Contains(31)) { sql.Append(" SUM(sum_charge), "); }
                        if (Params.Contains(321)) { sql.Append(" SUM(money_to), "); }
                        if (Params.Contains(322)) { sql.Append(" SUM(money_from), "); }
                        if (Params.Contains(34)) { sql.Append(" SUM(rsum_tarif), "); }
                        if (Params.Contains(35)) { sql.Append(" SUM(reval), "); }
                        if (Params.Contains(36)) { sql.Append(" SUM(" + sumOutsaldo + "), "); }
                        sql.Remove(sql.Length - 2, 2);

                        sql.Append(" FROM " + (listLc ? " selected_kvars k " : kvarTable + " k "));
                        sql.Append(" INNER JOIN " + chargeTable + " c ON k.nzp_kvar = c.nzp_kvar AND c.dat_charge is null AND c.nzp_serv > 1 ");
                        switch (Kind)
                        {
                            case 1:
                                sql.Append(" AND c.is_device = 0 ");
                                break;
                            case 2:
                                sql.Append(" AND c.is_device = 1 ");
                                break;
                            case 3:
                                sql.Append(" AND c.is_device = 9 ");
                                break;
                            case 4:
                                sql.Append(" AND c.nzp_serv > 500 ");
                                break;
                            case 5:
                                sql.Append(" AND k.typek = 3 ");
                                break;
                        }
                        sql.Append(" INNER JOIN t_report_71_Nach2 t ON k.nzp_kvar = t.nzp_kvar ");
                        if (Params.Contains(9))
                        {
                            sql.Append(" INNER JOIN " + domTable + " d ON k.nzp_dom = d.nzp_dom " + whereRajDom +
                                (Params.Contains(900) ? " INNER JOIN " + rajDomTable + "rd ON d.nzp_raj = rd.nzp_raj_dom " : string.Empty) +
                                " INNER JOIN " + ulTable + " u ON d.nzp_ul = u.nzp_ul " +
                                " INNER JOIN " + rajTable + " r ON u.nzp_raj = r.nzp_raj " + whereRaj);
                        }
                        else if (Params.Contains(900))
                        {
                            sql.Append(" INNER JOIN " + pref + DBManager.sDataAliasRest + "dom d ON k.nzp_dom = d.nzp_dom " + whereRajDom +
                                       " INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon_dom rd ON d.nzp_raj = rd.nzp_raj_dom ");
                        }

                        sql.Append(" WHERE 1=1 " + whereServ + whereSupp);

                        //if (BankSupplier != null && BankSupplier.Suppliers != null &&
                        //    BankSupplier.Suppliers.Contains(154522)) //исключение для ООО НАШ ДОМ, потом уничтожить
                        //    sql.Append(" and nzp_supp in (select nzp_supp from " +
                        //               pref + DBManager.sDataAliasRest + "tarif ta " +
                        //               " where ta.nzp_kvar = k.nzp_kvar " +
                        //               " and ta.nzp_serv = c.nzp_serv " +
                        //               " and ta.nzp_supp = c.nzp_supp " +
                        //               " and ta.is_actual <> 100 " +
                        //               " and '" + DatS + "' between ta.dat_s and ta.dat_po) ");
                        if (!String.IsNullOrEmpty(groupby)) sql.Append(" GROUP BY " + groupby.TrimEnd(',', ' '));
                        ExecSQL(sql.ToString());


                        if (Params.Contains(332))
                        {
                            sql.Remove(0, sql.Length);
                            sql.Append(" UPDATE t_svod_mini SET real_insaldo = (" +
                                       " SELECT SUM(sum_rcl) " +
                                       " FROM " + perekidka + " a " +
                                       " where a.nzp_kvar = t_svod_mini.nzp_kvar" +
                                       "       and a.type_rcl in (100,20) " +
                                       whereServ + GetWhereSupp("a.nzp_supp") +
                                       "       and a.month_ = " + mo);
                            sql.Append(" and a.nzp_supp=t_svod_mini.nzp_supp ");
                            sql.Append(" and a.nzp_serv=t_svod_mini.nzp_serv ");
                            sql.Append(") WHERE real_insaldo IS NULL ");
                            sql.Append(" AND pref = '" + point + "' AND monthyear = '" + _months[mo] + " " + ye + "' ");
                            ExecSQL(sql.ToString());
                        }

                        if (Params.Contains(331))
                        {
                            sql.Remove(0, sql.Length);
                            sql.Append(" UPDATE t_svod_mini SET real_charge = (" +
                                       " SELECT SUM(sum_rcl) " +
                                       " FROM " + perekidka + " a " +
                                       " where a.nzp_kvar = t_svod_mini.nzp_kvar" +
                                       "       and a.type_rcl not in (100,20) " +
                                       whereServ + GetWhereSupp("a.nzp_supp") +
                                       "       and a.month_ = " + mo);
                            sql.Append(" and a.nzp_supp=t_svod_mini.nzp_supp ");
                            sql.Append(" and a.nzp_serv=t_svod_mini.nzp_serv ");
                            sql.Append(") WHERE real_charge IS NULL ");
                            sql.Append(" AND pref = '" + point + "' AND monthyear = '" + _months[mo] + " " + ye + "' ");
                            ExecSQL(sql.ToString());
                        }

                        if (Params.Contains(322) || Params.Contains(323))
                        {
                            sql.Remove(0, sql.Length);
                            sql.Append(" INSERT INTO t_svod_mini(pref, monthyear, nzp_kvar, nzp_serv, nzp_supp, money_from, money_supp) " +
                                       " SELECT '" + point + "' AS pref, '" + _months[mo] + " " + ye + "' AS monthyear, nzp_kvar, nzp_serv , nzp_supp, -SUM(sum_prih), SUM(sum_prih) " +
                                       " FROM " + fromSupplier + " a " +
                                       " INNER JOIN " + kvarTable + " k ON a.num_ls = k.num_ls " +
                                       " WHERE a.kod_sum in (49, 50, 35) " + GetWhereSupp("a.nzp_supp") +
                                       " AND dat_uchet >= '" + DatS + "' " +
                                       " AND dat_uchet <= '" + DatPo + "' " +
                                       " GROUP BY 1,2,3,4,5 ");
                            ExecSQL(sql.ToString());

                        }


                        #region Заполнение дополнительных параметров

                        if (Params.Contains(15) || Params.Contains(16) || Params.Contains(17))
                        {
                            sql.Remove(0, sql.Length);
                            sql.Append("  insert into t_g (nzp_kvar, propis_count, gil_prib, gil_ub) " +
                                       "  select a.nzp as nzp_kvar, " +
                                       "  case when a.nzp_prm = 5 then a.val_prm" + DBManager.sConvToInt +
                                       " end as propis_count, " +
                                       "  case when a.nzp_prm = 131 then a.val_prm" + DBManager.sConvToInt +
                                       " end as gil_prib, " +
                                       "  case when a.nzp_prm = 10 then a.val_prm" + DBManager.sConvToInt +
                                       " end as gil_ub " +
                                       "  from " + prmTable + " a, " + (listLc ? " selected_kvars k " : kvarTable + " k ") +
                                       "  where a.nzp_prm in (5,131,10) and a.is_actual = 1 and a.nzp=k.nzp_kvar " +
                                       "  and a.dat_s <= '" + DatPo + "' " +
                                       "  and a.dat_po >= '" + DatS + "' ");
                            ExecSQL(sql.ToString());
                        }

                        if (Params.Contains(18) || Params.Contains(19) || Params.Contains(20) || Params.Contains(21) ||
                            Params.Contains(22) || Params.Contains(37))
                        {
                            sql.Remove(0, sql.Length);
                            sql.Append("  insert into t_s (nzp_kvar, ob_s, otop_s, gil_s, rooms, floor, privat) " +
                                       "  select a.nzp as nzp_kvar, " +
                                       "  case when a.nzp_prm = 4 then a.val_prm" + DBManager.sConvToNum + " end as ob_s, " +
                                       "  case when a.nzp_prm = 133 then a.val_prm" + DBManager.sConvToNum +
                                       " end as otop_s, " +
                                       "  case when a.nzp_prm = 6 then a.val_prm" + DBManager.sConvToNum + " end as gil_s, " +
                                       "  case when a.nzp_prm = 107 then a.val_prm" + DBManager.sConvToInt +
                                       " end as rooms, " +
                                       "  case when a.nzp_prm = 2 then a.val_prm" + DBManager.sConvToInt + " end as floor, " +
                                       "  case when a.nzp_prm = 8 then (case when a.val_prm = '1' then 'Да' else 'Нет' end) end as privat " +
                                       "  from " + prmTable + " a, " + (listLc ? " selected_kvars k " : kvarTable + " k ") +
                                       "  where a.nzp_prm in (4,133,6,107,2,8) and a.is_actual = 1 and a.nzp=k.nzp_kvar " +
                                       "  and a.dat_s <= '" + DatPo + "' " +
                                       "  and a.dat_po >= '" + DatS + "' ");
                            ExecSQL(sql.ToString());
                        }

                        if (Params.Contains(4) || Params.Contains(5) || Params.Contains(6) || Params.Contains(7) ||
                            Params.Contains(8))
                        {
                            ExecSQL(" INSERT INTO t_s2 (nzp_kvar) SELECT nzp_kvar FROM t_report_71_Nach2 ");

                            if (Params.Contains(4))
                            {
                                ExecSQL(" INSERT INTO t_s2 (nzp_kvar, status)" +
                                        " SELECT nzp, CASE WHEN val_prm = '1' THEN 'открыт' WHEN val_prm = '2' THEN 'закрыт' ELSE 'неопределено' END " +
                                        " FROM " + pref + DBManager.sDataAliasRest + "prm_3 p " +
                                        " WHERE dat_s <= '" + DatPo + "' " +
                                        " AND dat_po >= '" + DatS + "' " +
                                        " AND is_actual = 1 " +
                                        " AND nzp_prm = 51 ");
                            }

                            if (Params.Contains(5))
                            {
                                ExecSQL(" INSERT INTO t_s2 (nzp_kvar, ipu) " +
                                        " SELECT nzp, 'Есть' FROM  " + pref + DBManager.sDataAliasRest +
                                        "counters_spis c, t_report_71_Nach2 t " +
                                        " WHERE nzp_type = 3 " +
                                        " AND c.nzp = t.nzp_kvar " +
                                        " AND is_actual = 1 ");
                            }

                            if (Params.Contains(6))
                            {
                                ExecSQL(" INSERT INTO t_doms(nzp_dom) " +
                                        " SELECT nzp_dom " +
                                        " FROM  " + pref + DBManager.sDataAliasRest + "kvar k, t_report_71_Nach2 t  " +
                                        " WHERE k.nzp_kvar = t.nzp_kvar ");

                                ExecSQL(" INSERT INTO t_s2 (nzp_kvar, odpu)  " +
                                        " SELECT k.nzp_kvar, 'Есть' FROM  " + pref + DBManager.sDataAliasRest +
                                        " counters_spis c, t_doms d, " +
                                        pref + DBManager.sDataAliasRest + "kvar k " +
                                        " WHERE c.nzp = d.nzp_dom " +
                                        " AND k.nzp_dom = d.nzp_dom " +
                                        " AND nzp_type = 1 " +
                                        " AND is_actual = 1 ");

                                ExecSQL(" DELETE FROM t_doms ");
                            }

                            if (Params.Contains(7))
                            {
                                ExecSQL(" INSERT INTO t_s2 (nzp_kvar, rasch) " +
                                        " SELECT nzp_kvar, CASE WHEN is_device = 0 THEN 'Норматив' WHEN is_device = 1 THEN 'Показание ПУ' " +
                                        " WHEN is_device = 9 THEN 'Среднемесячное потребление по ПУ' ELSE ''  END || " +
                                        " CASE WHEN nzp_serv > 500 THEN ', Исходя из показаний ОДПУ' ELSE ''  END || " +
                                        " CASE WHEN typek = 3 THEN ', Расчетный способ для нежилых помещений' ELSE '' END " +
                                        " FROM t_svod_mini t ");
                            }

                            if (Params.Contains(8))
                            {
                                ExecSQL(" INSERT INTO t_doms(nzp_dom) " +
                                        " SELECT nzp_dom " +
                                        " FROM  " + pref + DBManager.sDataAliasRest + "kvar k, t_report_71_Nach2 t  " +
                                        " WHERE k.nzp_kvar = t.nzp_kvar ");

                                ExecSQL(" INSERT INTO t_s2 (nzp_kvar, gkal) " +
                                        " SELECT k.nzp_kvar, val_prm " +
                                        " FROM " + pref + DBManager.sDataAliasRest + "prm_2 p, t_doms d, " +
                                        pref + DBManager.sDataAliasRest + "kvar k " +
                                        " WHERE dat_s <= '" + DatPo + "' " +
                                        " AND dat_po >= '" + DatS + "' " +
                                        " AND is_actual = 1 " +
                                        " AND nzp_prm = 723 " +
                                        " AND p.nzp = d.nzp_dom " +
                                        " AND k.nzp_dom = d.nzp_dom ");

                                ExecSQL(" DELETE FROM t_doms ");
                            }

                            ExecSQL(" DELETE FROM t_s2 WHERE nzp_kvar NOT IN (SELECT nzp_kvar FROM t_report_71_Nach2)");
                        }

                        #endregion
                    }
                } //месяц

                //if (TempTableInWebCashe(chargeTable) && whereServFilter != "")
                //    ExecSQL(" DROP TABLE filtered_charge ");
            }  //район
            reader.Close();
            //sql.Remove(0, sql.Length);
            //sql.Append(" UPDATE t_svod_mini SET money_from = money_from - money_supp ");
            //ExecSQL(sql.ToString());

            //if (Params.Contains(322))
            //{
            //    sql.Remove(0, sql.Length);
            //    sql.Append(" UPDATE t_svod_mini SET money_supp = money_from + money_supp ");
            //    ExecSQL(sql.ToString());
            //}

            ExecSQL(" insert into t_gil_kvar (nzp_kvar, propis_count, gil_prib, gil_ub) " +
                    " select nzp_kvar, max(propis_count), max(gil_prib), max(gil_ub) " +
                    " from t_g group by 1 ");

            ExecSQL(" insert into t_prms (nzp_kvar, ob_s, otop_s, gil_s, rooms, floor, privat) " +
                    " select nzp_kvar, max(ob_s), max(otop_s), max(gil_s), max(rooms), max(floor), max(privat) " +
                    " from t_s group by 1 ");

            ExecSQL(" insert into t_prms2 (nzp_kvar, status, ipu, odpu, rasch, gkal) " +
                    " select nzp_kvar, max(status), max(ipu), max(odpu), max(rasch), max(gkal) " +
                    " from t_s2 group by 1 ");
            ExecSQL(" UPDATE t_prms2 SET status = 'неопределено' WHERE status = '' OR status IS NULL ");


            string order = FillSvodTable();

            ExecSQL(" CREATE INDEX svod_all_ndx ON t_svod_all(pref, monthyear, area, geu, uch, town, rajon, ulica, idom, ndom, nkor, ikvar, nkvar, num_ls, fio, pkod, name_supp, service, tarif) ");
            ExecSQL(DBManager.sUpdStat + " t_svod_all ");
            int p = 1;
            var dt = new DataTable();

            ExecSQL(
                        " SELECT pref, monthyear, area, geu, uch, town, rajon_dom, rajon, ulica, idom, ndom, nkor, ikvar, nkvar, num_ls, fio, " +
                        " SUM(propis_count) AS propis_count, SUM(gil_prib) AS gil_prib, SUM(gil_ub) AS gil_ub, SUM(ob_s) AS ob_s, " +
                        " SUM(otop_s) AS otop_s, SUM(gil_s) AS gil_s, SUM(rooms) AS rooms, MAX(floor) AS floor, MAX(privat) AS privat, pkod, name_supp, service, " +
                        " MAX(status) AS status, MAX(ipu) AS ipu, MAX(odpu) AS odpu, MAX(rasch) AS rasch, MAX(gkal) AS gkal, " +
                        " SUM(sum_insaldo) AS sum_insaldo, tarif, SUM(c_calc) AS c_calc, SUM(sum_nedop) AS sum_nedop, " +
                        " SUM(sum_tarif) AS sum_tarif, SUM(sum_charge) AS sum_charge, SUM(money_to) AS money_to, SUM(money_from) AS money_from ,SUM(money_supp) AS money_supp, SUM(real_charge) AS real_charge,SUM(real_insaldo) AS real_insaldo, " +
                        " SUM(rsum_tarif) AS rsum_tarif, SUM(reval) AS reval, SUM(sum_outsaldo) AS sum_outsaldo " +
                        " into temp t_svod_final" +
                        " FROM t_svod_all " +
                        " GROUP BY pref, monthyear, area, geu, uch, town, rajon_dom, rajon, ulica, idom, ndom, nkor, ikvar, nkvar, num_ls, fio, pkod, name_supp, service, tarif ");
            int stringCount = Convert.ToInt32(ExecSQLToTable("SELECT count(*) FROM t_svod_final ", 1000).Rows[0][0].ToString());

            do
            {
                var dtLocal = ExecSQLToTable(DBManager.SetLimitOffset(" SELECT * FROM t_svod_final ", (stringCount < stringsInPack ? stringCount : stringsInPack), (stringsInPack * (p - 1))), 1000);
                dt.Merge(dtLocal);
                stringCount -= stringsInPack;
                p++;
            } while (stringCount > 0);
            var dv = new DataView(dt) { Sort = order };
            dt = dv.ToTable();

            dt.TableName = "Q_master";
            RowCount = false;
            #region Заголовок
            sql.Remove(0, sql.Length);
            sql.Append(" insert into t_title values( ");
            sql.Append(Params.Contains(38) ? " 'Банк данных' , " : " '' , ");
            sql.Append(Params.Contains(39) ? " 'Месяц' , " : " '' , ");
            sql.Append(Params.Contains(1) ? " 'УК' , " : " '' , ");
            sql.Append(Params.Contains(2) ? " 'ЖЭУ' , " : " '' , ");
            sql.Append(Params.Contains(3) ? " 'Участок' , " : " '' , ");
            sql.Append(Params.Contains(4) ? " 'Статус ЛС' , " : " '' , ");
            sql.Append(Params.Contains(5) ? " 'Наличие ИПУ' , " : " '' , ");
            sql.Append(Params.Contains(6) ? " 'Наличие ОДПУ' , " : " '' , ");
            sql.Append(Params.Contains(7) ? " 'Признак расчета' , " : " '' , ");
            sql.Append(Params.Contains(8) ? " 'Домовой норматив' , " : " '' , ");
            sql.Append(Params.Contains(900) ? " 'Район' , " : " '' , ");
            sql.Append(Params.Contains(9) ? " 'Населенный пункт' , " : " '' , ");
            sql.Append(Params.Contains(10) ? " 'Улица' , " : " '' , ");
            sql.Append(Params.Contains(11) ? " 'Дом' , " : " '' , ");
            sql.Append(Params.Contains(12) ? " 'Квартира' , " : " '' , ");
            sql.Append(Params.Contains(13) ? " 'ЛС' , " : " '' , ");
            sql.Append(Params.Contains(14) ? " 'ФИО' , " : " '' , ");
            sql.Append(Params.Contains(15) ? " 'Жильцов' , " : " '' , ");
            sql.Append(Params.Contains(16) ? " 'Проживающих' , " : " '' , ");
            sql.Append(Params.Contains(17) ? " 'Выбывших' , " : " '' , ");
            sql.Append(Params.Contains(18) ? " 'Общая площадь' , " : " '' , ");
            sql.Append(Params.Contains(19) ? " 'Отапливаемая площадь' , " : " '' , ");
            sql.Append(Params.Contains(20) ? " 'Жилая площадь' , " : " '' , ");
            sql.Append(Params.Contains(21) ? " 'Комнат' , " : " '' , ");
            sql.Append(Params.Contains(22) ? " 'Этаж' , " : " '' , ");
            sql.Append(Params.Contains(23) ? " 'Пл. код' , " : " '' , ");
            sql.Append(Params.Contains(24) ? " 'Поставщик' , " : " '' , ");
            sql.Append(Params.Contains(25) ? " 'Услуга' , " : " '' , ");
            sql.Append(Params.Contains(26) ? " 'Вх. сальдо' , " : " '' , ");
            sql.Append(Params.Contains(27) ? " 'Тариф' , " : " '' , ");
            sql.Append(Params.Contains(28) ? " 'Расход' , " : " '' , ");
            sql.Append(Params.Contains(29) ? " 'Недопоставка' , " : " '' , ");
            sql.Append(Params.Contains(30) ? " 'Начислено с учетом недоп-ки' , " : " '' , ");
            sql.Append(Params.Contains(31) ? " 'К оплате' , " : " '' , ");
            sql.Append(Params.Contains(321) ? " 'Оплачено' , " : " '' , ");
            sql.Append(Params.Contains(322) ? " 'В т.ч. оплаты предыдущих биллинговых систем' , " : " '' , ");
            sql.Append(Params.Contains(323) ? " 'Оплата напрямую поставщикам' , " : " '' , ");
            sql.Append(Params.Contains(331) ? " 'Корректировка начислений' , " : " '' , ");
            sql.Append(Params.Contains(332) ? " 'Корректировка вх.сальдо' , " : " '' , ");
            sql.Append(Params.Contains(34) ? " 'Начислено' , " : " '' , ");
            sql.Append(Params.Contains(35) ? " 'Перерасчет' , " : " '' , ");
            sql.Append(Params.Contains(36) ? " 'Исх. сальдо' , " : " '' , ");
            sql.Append(Params.Contains(37) ? " 'Приватизировано' , " : " '' , ");
            sql.Remove(sql.Length - 2, 2);
            sql.Append(") ");
            ExecSQL(sql.ToString());
            DataTable dt1 = ExecSQLToTable(" select * from t_title ");
            dt1.TableName = "Q_master1";
            #endregion
            var ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables.Add(dt1);
            return ds;

        }

        private string FillSvodTable()
        {
            var sql = new StringBuilder();
            var grouper = new StringBuilder();
            var order = new StringBuilder();

            ExecSQL(" create index mini_index on t_svod_mini(nzp_kvar) ");
            ExecSQL(DBManager.sUpdStat + " t_svod_mini ");

            sql.Remove(0, sql.Length);
            sql.Append(" insert into t_svod_all( ");
            if (Params.Contains(38)) { sql.Append(" pref, "); }
            if (Params.Contains(39)) { sql.Append(" monthyear, "); }
            if (Params.Contains(1)) { sql.Append(" nzp_area, "); }
            if (Params.Contains(2)) { sql.Append(" nzp_geu, "); }
            if (Params.Contains(3)) { sql.Append(" uch, "); }
            if (Params.Contains(900)) { sql.Append(" nzp_raj_dom, "); }
            if (Params.Contains(9)) { sql.Append(" nzp_town, nzp_raj, "); }
            if (Params.Contains(10)) { sql.Append(" nzp_ul, "); }
            if (Params.Contains(11) || Params.Contains(8)) { sql.Append(" nzp_dom, "); }
            sql.Append(" nzp_kvar, ");
            if (Params.Contains(13)) { sql.Append(" num_ls, "); }
            if (Params.Contains(14)) { sql.Append(" fio, "); }
            if (Params.Contains(23)) { sql.Append(" pkod, "); }
            if (Params.Contains(24)) { sql.Append(" nzp_supp, "); }
            if (Params.Contains(25)) { sql.Append(" nzp_serv, "); }
            if (Params.Contains(27)) { sql.Append(" tarif, "); }
            if (Params.Contains(26)) { sql.Append(" sum_insaldo, "); }
            if (Params.Contains(28)) { sql.Append(" c_calc, "); }
            if (Params.Contains(29)) { sql.Append(" sum_nedop, "); }
            if (Params.Contains(30)) { sql.Append(" sum_tarif, "); }
            if (Params.Contains(31)) { sql.Append(" sum_charge, "); }
            if (Params.Contains(321)) { sql.Append(" money_to, "); }
            if (Params.Contains(322) || Params.Contains(323)) { sql.Append(" money_from, "); }
            if (Params.Contains(323) || Params.Contains(322)) { sql.Append(" money_supp, "); }
            if (Params.Contains(331)) { sql.Append(" real_charge, "); }
            if (Params.Contains(332)) { sql.Append(" real_insaldo, "); }
            if (Params.Contains(34)) { sql.Append(" rsum_tarif, "); }
            if (Params.Contains(35)) { sql.Append(" reval, "); }
            if (Params.Contains(36)) { sql.Append(" sum_outsaldo, "); }
            sql.Remove(sql.Length - 2, 2);
            sql.Append(") ");
            sql.Append(" select ");
            if (Params.Contains(38)) { sql.Append(" pref, "); grouper.Append(" pref, "); order.Append(" pref, "); }
            if (Params.Contains(39)) { sql.Append(" monthyear, "); grouper.Append(" monthyear, "); order.Append(" monthyear, "); }
            if (Params.Contains(1)) { sql.Append(" nzp_area, "); grouper.Append(" nzp_area, "); order.Append(" area, "); }
            if (Params.Contains(2)) { sql.Append(" nzp_geu, "); grouper.Append(" nzp_geu, "); order.Append(" geu, "); }
            if (Params.Contains(3)) { sql.Append(" uch, "); grouper.Append(" uch, "); order.Append(" uch, "); }
            if (Params.Contains(900)) { sql.Append(" nzp_raj_dom, "); grouper.Append(" nzp_raj_dom, "); order.Append(" rajon_dom, "); }
            if (Params.Contains(9)) { sql.Append(" nzp_town, nzp_raj, "); grouper.Append(" nzp_town, nzp_raj, "); order.Append(" town, rajon, "); }
            if (Params.Contains(10)) { sql.Append(" nzp_ul, "); grouper.Append(" nzp_ul, "); order.Append(" ulica, "); }
            if (Params.Contains(11) || Params.Contains(8)) { sql.Append(" nzp_dom, "); grouper.Append(" nzp_dom, "); order.Append(" idom,ndom, "); }
            sql.Append(" m.nzp_kvar, "); grouper.Append(" nzp_kvar, ");
            if (Params.Contains(12)) { order.Append(" ikvar, nkvar, "); }
            if (Params.Contains(13)) { sql.Append(" num_ls, "); grouper.Append(" num_ls, "); order.Append(" num_ls, "); }
            if (Params.Contains(14)) { sql.Append(" fio, "); grouper.Append(" fio, "); order.Append(" fio, "); }
            if (Params.Contains(23)) { sql.Append(" pkod, "); grouper.Append(" pkod, "); order.Append(" pkod, "); }
            if (Params.Contains(24)) { sql.Append(" nzp_supp, "); grouper.Append(" nzp_supp, "); order.Append(" name_supp, "); }
            if (Params.Contains(25)) { sql.Append(" nzp_serv, "); grouper.Append(" nzp_serv, "); order.Append(" service, "); }
            if (Params.Contains(27)) { sql.Append(" tarif, "); grouper.Append(" tarif, "); order.Append(" tarif, "); }
            if (Params.Contains(26)) { sql.Append(" sum(sum_insaldo) as sum_insaldo, "); }
            if (Params.Contains(28)) { sql.Append(" sum(c_calc) as c_calc, "); }
            if (Params.Contains(29)) { sql.Append(" sum(sum_nedop) as sum_nedop, "); }
            if (Params.Contains(30)) { sql.Append(" sum(sum_tarif) as sum_tarif, "); }
            if (Params.Contains(31)) { sql.Append(" sum(sum_charge) as sum_charge, "); }
            if (Params.Contains(321)) { sql.Append(" sum(money_to) as money_to, "); }
            if (Params.Contains(322) || Params.Contains(323)) { sql.Append(" sum(money_from) as money_from, "); }
            if (Params.Contains(323) || Params.Contains(322)) { sql.Append(" sum(money_supp) as money_supp, "); }
            if (Params.Contains(331)) { sql.Append(" sum(real_charge) as real_charge, "); }
            if (Params.Contains(332)) { sql.Append(" sum(real_insaldo) as real_insaldo, "); }
            if (Params.Contains(34)) { sql.Append(" sum(rsum_tarif) as rsum_tarif, "); }
            if (Params.Contains(35)) { sql.Append(" sum(reval) as reval, "); }
            if (Params.Contains(36)) { sql.Append(" sum(sum_outsaldo) as sum_outsaldo, "); }

            if (order.Length > 0) order.Remove(order.Length - 2, 2);
            sql.Remove(sql.Length - 2, 2);
            sql.Append(" from t_svod_mini m ");
            if (grouper.Length > 0)
            {
                grouper.Remove(grouper.Length - 2, 2);
                sql.Append(" group by " + grouper);
            }
            ExecSQL(sql.ToString());

            if (Params.Contains(1))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set area = (" +
                           " select area from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area a " +
                           " where a.nzp_area = t_svod_all.nzp_area) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(2))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set geu = (" +
                           " select geu from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_geu a " +
                           " where a.nzp_geu = t_svod_all.nzp_geu) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(900))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_svod_all SET rajon_dom = (" +
                           " SELECT rajon_dom from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon_dom a " +
                           " WHERE a.nzp_raj_dom = t_svod_all.nzp_raj_dom) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(9))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set " +
                           " town = (select town from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_town a " +
                           " where a.nzp_town = t_svod_all.nzp_town), " +
                           " rajon = (select rajon from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon a " +
                           " where a.nzp_raj = t_svod_all.nzp_raj) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(10))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set " +
                           " ulica = (select TRIM(" + DBManager.sNvlWord + "(ulicareg,''))||' '||TRIM(ulica) AS ulica from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica a " +
                           " where a.nzp_ul = t_svod_all.nzp_ul) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(11))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set " +
                           " idom = (select idom from " + ReportParams.Pref + DBManager.sDataAliasRest + "dom a " +
                           " where a.nzp_dom = t_svod_all.nzp_dom), " +
                           " ndom = (select ndom from " + ReportParams.Pref + DBManager.sDataAliasRest + "dom a " +
                           " where a.nzp_dom = t_svod_all.nzp_dom), " +
                           " nkor = (select CASE WHEN trim(nkor) <> '' AND trim(nkor) <> '-' THEN ' к.'||nkor END from " + ReportParams.Pref + DBManager.sDataAliasRest + "dom a " +
                           " where a.nzp_dom = t_svod_all.nzp_dom and nkor<>'-') ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(12))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set nkvar = (" +
                           " select nkvar from " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a " +
                           " where a.nzp_kvar = t_svod_all.nzp_kvar), " +
                           " ikvar = (" +
                           " select ikvar from " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a " +
                           " where a.nzp_kvar = t_svod_all.nzp_kvar)");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(24))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set name_supp = (" +
                           " select name_supp from " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier a " +
                           " where a.nzp_supp = t_svod_all.nzp_supp) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(25))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set service = (" +
                           " select service from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services a " +
                           " where a.nzp_serv = t_svod_all.nzp_serv) ");
                ExecSQL(sql.ToString());
            }

            ExecSQL(" create index all_index on t_svod_all(nzp_kvar) ");
            ExecSQL(DBManager.sUpdStat + " t_svod_all ");
            ExecSQL(" create index gil_index on t_gil_kvar(nzp_kvar) ");
            ExecSQL(DBManager.sUpdStat + " t_gil_kvar ");
            ExecSQL(" create index prms_index on t_prms(nzp_kvar) ");
            ExecSQL(DBManager.sUpdStat + " t_prms ");
            ExecSQL(" create index prms_index2 on t_prms2(nzp_kvar) ");
            ExecSQL(DBManager.sUpdStat + " t_prms2 ");

            if (Params.Contains(15))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set propis_count = (" +
                           " select propis_count from t_gil_kvar a " +
                           " where a.nzp_kvar = t_svod_all.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(16))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set gil_prib = (" +
                           " select gil_prib from t_gil_kvar a " +
                           " where a.nzp_kvar = t_svod_all.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(17))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set gil_ub = (" +
                           " select gil_ub from t_gil_kvar a " +
                           " where a.nzp_kvar = t_svod_all.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(18))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set ob_s = (" +
                           " select ob_s from t_prms a " +
                           " where a.nzp_kvar = t_svod_all.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(19))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set otop_s = (" +
                           " select otop_s from t_prms a " +
                           " where a.nzp_kvar = t_svod_all.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(20))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set gil_s = (" +
                           " select gil_s from t_prms a " +
                           " where a.nzp_kvar = t_svod_all.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(21))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set rooms = (" +
                           " select rooms from t_prms a " +
                           " where a.nzp_kvar = t_svod_all.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(22))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set floor = (" +
                           " select floor from t_prms a " +
                           " where a.nzp_kvar = t_svod_all.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(37))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set privat = (" +
                           " select privat from t_prms a " +
                           " where a.nzp_kvar = t_svod_all.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }


            if (Params.Contains(4))
            {
                ExecSQL(" UPDATE t_svod_all SET status = (" +
                           " SELECT status FROM t_prms2 a " +
                           " WHERE  a.nzp_kvar = t_svod_all.nzp_kvar) ");
            }
            if (Params.Contains(5))
            {
                ExecSQL(" UPDATE t_svod_all SET ipu = (" +
                           " SELECT ipu FROM t_prms2 a " +
                           " WHERE  a.nzp_kvar = t_svod_all.nzp_kvar) ");
            }
            if (Params.Contains(6))
            {
                ExecSQL(" UPDATE t_svod_all SET odpu = (" +
                           " SELECT odpu FROM t_prms2 a " +
                           " WHERE  a.nzp_kvar = t_svod_all.nzp_kvar) ");
            }
            if (Params.Contains(7))
            {
                ExecSQL(" UPDATE t_svod_all SET rasch = (" +
                           " SELECT rasch FROM t_prms2 a " +
                           " WHERE  a.nzp_kvar = t_svod_all.nzp_kvar) ");
            }
            if (Params.Contains(8))
            {
                ExecSQL(" UPDATE t_svod_all SET gkal = (" +
                           " SELECT gkal FROM t_prms2 a " +
                           " WHERE  a.nzp_kvar = t_svod_all.nzp_kvar) ");
            }

            return order.ToString();
        }

        private string GetWhereRaj(string tablePrefix)
        {
            string whereRaj = String.Empty;
            if (Raions != null)
            {
                whereRaj = Raions.Aggregate(whereRaj, (current, nzpRaj) => current + (nzpRaj + ","));
                whereRaj = whereRaj.TrimEnd(',');
            }
            if (String.IsNullOrEmpty(whereRaj))
            {
                var towns = ReportParams.GetRolesCondition(Constants.role_sql_town);
                if (!String.IsNullOrEmpty(towns))
                {
                    towns = " where nzp_town in (" + towns + ") ";
                    var roleRaj = ExecSQLToTable(" select distinct nzp_raj from " + ReportParams.Pref + DBManager.sDataAliasRest + " s_rajon " + towns);
                    var roleView = new DataView(roleRaj);
                    whereRaj = roleView.Cast<DataRowView>().Aggregate(whereRaj, (current, c) => current + (c[0] + ","));
                    whereRaj = whereRaj.TrimEnd(',');
                }
            }
            if (!String.IsNullOrEmpty(whereRaj))
            {
                whereRaj = " and " + tablePrefix + "nzp_raj in (" + whereRaj + ") ";
            }
            return whereRaj;
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp(string fieldPref)
        {
            string whereSupp = String.Empty;
            if (BankSupplier != null && BankSupplier.Suppliers != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Suppliers.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_supp in (" + supp.TrimEnd(',') + ")";
            }

            if (BankSupplier != null && BankSupplier.Principals != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Principals.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_princip in (" + supp.TrimEnd(',') + ")";
            }

            if (BankSupplier != null && BankSupplier.Agents != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Agents.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_agent in (" + supp.TrimEnd(',') + ")";
            }

            string oldsupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);

            whereSupp = whereSupp.TrimEnd(',');


            if (!String.IsNullOrEmpty(whereSupp) || !String.IsNullOrEmpty(oldsupp))
            {
                if (!String.IsNullOrEmpty(oldsupp))
                    whereSupp += " AND nzp_supp in (" + oldsupp + ")";

                //Поставщики
                SupplierHeader = String.Empty;
                string sql = " SELECT name_supp from " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                             " WHERE nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += "(" + dr["name_supp"].ToString().Trim() + "), ";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',', ' ');
            }
            return " and " + fieldPref + " in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + whereSupp + ")";
        }

        /// <summary>
        /// получает ограничение из таблицы s_rajon_dom
        /// </summary>
        /// <param name="tablePrefix"></param>
        /// <returns></returns>
        private string GetWhereRajDom(string tablePrefix)
        {
            string whereRajDom = String.Empty;
            if (RaionsDoms != null)
            {
                whereRajDom = RaionsDoms.Aggregate(whereRajDom, (current, nzpRajDom) => current + (nzpRajDom + ","));
                whereRajDom = whereRajDom.TrimEnd(',');
                whereRajDom = " AND d.nzp_raj IN (" + whereRajDom + ") ";
            }
            return whereRajDom;
        }

        /// <summary>
        /// Получить список услуг
        /// </summary>
        /// <returns></returns>
        private string GetWhereServ()
        {
            string whereServ = String.Empty;
            whereServ = Services.Services != null ? Services.Services.Aggregate(whereServ, (curr, nzpServ) => curr + (nzpServ + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_serv);
            whereServ = whereServ.TrimEnd(',');
            whereServ = !String.IsNullOrEmpty(whereServ)
                ? " AND nzp_serv " + (Services.Status == 0 ? "in" : "not in") + " (" + whereServ + ")"
                : String.Empty;

            if (!String.IsNullOrEmpty(whereServ))
            {
                ServiceHeader = string.Empty;
                string sql = " SELECT service from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services WHERE nzp_serv > 0 " + whereServ;
                DataTable serv = ExecSQLToTable(sql);
                foreach (DataRow dr in serv.Rows)
                {
                    ServiceHeader += dr["service"].ToString().Trim() + ", ";
                }
                ServiceHeader = ServiceHeader.TrimEnd(',', ' ');
            }
            return whereServ;
        }

        /// <summary>
        /// Получить условия органичения по услугам
        /// </summary>
        /// <returns></returns>
        private string GetWhereServFilter()
        {
            string whereServFilter = String.Empty;
            whereServFilter = FilteredServices.Services != null ? FilteredServices.Services.Aggregate(whereServFilter, (curr, nzpServ) => curr + (nzpServ + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_serv);
            whereServFilter = whereServFilter.TrimEnd(',');
            whereServFilter = !String.IsNullOrEmpty(whereServFilter)
                ? " nzp_serv " + (FilteredServices.Status == 0 ? "in" : "not in") + " (" + whereServFilter + ")"
                : String.Empty;
            return whereServFilter;
        }

        private string GetwhereWp()
        {
            string whereWp = String.Empty;
            if (BankSupplier != null && BankSupplier.Banks != null)
            {
                whereWp = BankSupplier.Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","));
            }
            else
            {
                whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            }
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            if (!string.IsNullOrEmpty(whereWp))
            {
                TerritoryHeader = String.Empty;
                string sql = " SELECT point FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWp;
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }
            return whereWp;
        }

        protected override void CreateTempTable()
        {
            string sql;
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                sql = " create temp table selected_kvars(" +
                              " nzp_kvar integer," +
                              " num_ls integer," +
                              " pkod Decimal(13,0)," +
                              " fio char(100), " +
                              " uch INTEGER, " +
                              " nzp_dom integer," +
                              " nzp_geu integer," +
                              " nzp_area integer," +
                              " typek integer) " +
                              DBManager.sUnlogTempTable;
                ExecSQL(sql);
            }

            sql = " CREATE TEMP TABLE t_report_71_Nach2( nzp_kvar INTEGER ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_doms( nzp_dom INTEGER ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_temp_status(nzp_kvar INTEGER, status CHARACTER(50) ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " create temp table t_g (nzp_kvar integer, propis_count integer, gil_prib integer, gil_ub integer)" + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " create temp table t_gil_kvar (nzp_kvar integer, propis_count integer, gil_prib integer, gil_ub integer)" + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " create temp table t_s (nzp_kvar integer, " +
                       " ob_s " + DBManager.sDecimalType + "(14,2), " +
                       " otop_s " + DBManager.sDecimalType + "(14,2), " +
                       " gil_s " + DBManager.sDecimalType + "(14,2), " +
                       " rooms " + DBManager.sDecimalType + "(14,2), " +
                       " floor " + DBManager.sDecimalType + "(14,2), " +
                       " privat character(10))" + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " create temp table t_prms (nzp_kvar integer, " +
                       " ob_s " + DBManager.sDecimalType + "(14,2), " +
                       " otop_s " + DBManager.sDecimalType + "(14,2), " +
                       " gil_s " + DBManager.sDecimalType + "(14,2), " +
                       " rooms " + DBManager.sDecimalType + "(14,2), " +
                       " floor " + DBManager.sDecimalType + "(14,2), " +
                       " privat character(10))" + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " create temp table t_s2 (nzp_kvar integer, " +
                       " status CHARACTER(50), " +
                       " ipu CHARACTER(10), " +
                       " odpu CHARACTER(10), " +
                       " rasch CHARACTER(100), " +
                       " gkal CHARACTER(50))" + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " create temp table t_prms2 (nzp_kvar integer, " +
                       " status CHARACTER(50), " +
                       " ipu CHARACTER(10), " +
                       " odpu CHARACTER(10), " +
                       " rasch CHARACTER(100), " +
                       " gkal CHARACTER(50))" + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        private void CreateTSvod()
        {
            var sql = new StringBuilder();
            sql.Append(" create temp table t_svod_all( ");
            sql.Append(" pref char(100), ");
            sql.Append(" monthyear char(100), ");
            sql.Append(" nzp_area integer, area char(100), ");
            sql.Append(" nzp_geu integer, geu char(100), ");
            sql.Append(" uch integer, ");
            sql.Append(" status char(50), ");
            sql.Append(" ipu char(50), ");
            sql.Append(" odpu char(50), ");
            sql.Append(" rasch char(100), ");
            sql.Append(" gkal char(50), ");
            sql.Append(" nzp_raj_dom integer, rajon_dom char(100), ");
            sql.Append(" nzp_town integer, nzp_raj integer, nzp_ul integer, town char(100), rajon char(100), ulica char(100), ");
            sql.Append(" nzp_dom integer, idom integer, ndom char(15), nkor char(15), ");
            sql.Append(" nzp_kvar integer, ikvar integer, nkvar char(40), ");
            sql.Append(" num_ls integer, ");
            sql.Append(" fio character(50), ");
            sql.Append(" propis_count integer, ");
            sql.Append(" gil_prib integer, ");
            sql.Append(" gil_ub integer, ");
            sql.Append(" ob_s " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" otop_s " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" gil_s " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" rooms integer, ");
            sql.Append(" floor integer, ");
            sql.Append(" privat character(10), ");
            sql.Append(" pkod " + DBManager.sDecimalType + "(13,0), ");
            sql.Append(" nzp_supp integer, name_supp char(100), ");
            sql.Append(" nzp_serv integer, service char(100), ");
            sql.Append(" sum_insaldo " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" tarif " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" c_calc " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" sum_nedop " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" sum_tarif " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" sum_charge " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" money_to " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" money_from " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" money_supp " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" real_charge " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" real_insaldo " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" rsum_tarif " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" reval " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" sum_outsaldo " + DBManager.sDecimalType + "(14,2), ");
            sql.Remove(sql.Length - 2, 2);
            sql.Append(") " + DBManager.sUnlogTempTable);
            ExecSQL(sql.ToString());

            sql.Remove(0, sql.Length);
            sql.Append(" create temp table t_svod_mini( ");
            if (Params.Contains(38) || Params.Contains(331) || Params.Contains(332) || Params.Contains(322) || Params.Contains(323)) { sql.Append(" pref char(100), "); }
            if (Params.Contains(39) || Params.Contains(331) || Params.Contains(332) || Params.Contains(322) || Params.Contains(323)) { sql.Append(" monthyear char(100), "); }
            if (Params.Contains(1)) { sql.Append(" nzp_area integer, "); }
            if (Params.Contains(2)) { sql.Append(" nzp_geu integer, "); }
            if (Params.Contains(3)) { sql.Append(" uch integer, "); }
            if (Params.Contains(7)) { sql.Append(" is_device integer, typek integer, "); }
            if (Params.Contains(900)) { sql.Append(" nzp_raj_dom integer, rajon_dom char(100), "); }
            if (Params.Contains(9)) { sql.Append(" nzp_town integer, nzp_raj integer, "); }
            if (Params.Contains(10)) { sql.Append(" nzp_ul integer, "); }
            if (Params.Contains(11) || Params.Contains(8)) { sql.Append(" nzp_dom integer, "); }
            sql.Append(" nzp_kvar integer, ");
            if (Params.Contains(13)) { sql.Append(" num_ls integer, "); }
            if (Params.Contains(14)) { sql.Append(" fio character(100), "); }
            //if (Params.Contains(15)) { sql.Append(" propis_count integer, "); }
            //if (Params.Contains(16)) { sql.Append(" gil_prib integer, "); }
            //if (Params.Contains(17)) { sql.Append(" gil_ub integer, "); }
            //if (Params.Contains(18)) { sql.Append(" ob_s " + DBManager.sDecimalType + "(14,2), "); }
            //if (Params.Contains(19)) { sql.Append(" otop_s " + DBManager.sDecimalType + "(14,2), "); }
            //if (Params.Contains(20)) { sql.Append(" gil_s " + DBManager.sDecimalType + "(14,2), "); }
            //if (Params.Contains(21)) { sql.Append(" rooms integer, "); }
            //if (Params.Contains(22)) { sql.Append(" floor integer, "); }
            if (Params.Contains(23)) { sql.Append(" pkod " + DBManager.sDecimalType + "(13,0), "); }
            if (Params.Contains(24) || Params.Contains(331) || Params.Contains(332) || Params.Contains(322) || Params.Contains(323)) { sql.Append(" nzp_supp integer, "); }
            if (Params.Contains(25) || Params.Contains(7) || Params.Contains(331) || Params.Contains(332) || Params.Contains(322) || Params.Contains(323)) { sql.Append(" nzp_serv integer, "); }
            if (Params.Contains(26)) { sql.Append(" sum_insaldo " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(27)) { sql.Append(" tarif " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(28)) { sql.Append(" c_calc " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(29)) { sql.Append(" sum_nedop " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(30)) { sql.Append(" sum_tarif " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(31)) { sql.Append(" sum_charge " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(321)) { sql.Append(" money_to " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(322) || Params.Contains(323)) { sql.Append(" money_from " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(323) || Params.Contains(322)) { sql.Append(" money_supp " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(331)) { sql.Append(" real_charge " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(332)) { sql.Append(" real_insaldo " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(34)) { sql.Append(" rsum_tarif " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(35)) { sql.Append(" reval " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(36)) { sql.Append(" sum_outsaldo " + DBManager.sDecimalType + "(14,2), "); }
            sql.Remove(sql.Length - 2, 2);
            sql.Append(") " + DBManager.sUnlogTempTable);
            ExecSQL(sql.ToString());


            sql.Remove(0, sql.Length);
            sql.Append(" create temp table t_title( " +
                        " pref char(50), " +
                        " monthyear char(50), " +
                        " area char(50), " +
                        " geu char(50), " +
                        " uch char(50), " +
                        " status char(50), " +
                        " ipu char(50), " +
                        " odpu char(50), " +
                        " rasch char(50), " +
                        " gkal char(50), " +
                        " rajon_dom char(50), " +
                        " rajon char(50), " +
                        " ulica char(50), " +
                        " ndom char(50), " +
                        " nkvar char(50), " +
                        " num_ls char(50), " +
                        " fio char(50), " +
                        " propis_count char(50), " +
                        " gil_prib char(50), " +
                        " gil_ub char(50), " +
                        " ob_s char(50), " +
                        " otop_s char(50), " +
                        " gil_s char(50), " +
                        " rooms char(50), " +
                        " floor char(50), " +
                        " pkod char(50), " +
                        " name_supp char(100), " +
                        " service char(50), " +
                        " sum_insaldo char(50), " +
                        " tarif char(50), " +
                        " c_calc char(50), " +
                        " sum_nedop char(50), " +
                        " sum_tarif char(50), " +
                        " sum_charge char(50), " +
                        " money_to char(50), " +
                        " money_from char(50), " +
                        " money_supp char(50), " +
                        " real_charge char(50), " +
                        " real_insaldo char(50), " +
                        " rsum_tarif char(50), " +
                        " reval char(50), " +
                        " sum_outsaldo char(50), " +
                        " privat char(50)) " + DBManager.sUnlogTempTable);
            ExecSQL(sql.ToString());

        }

        protected override void DropTempTable()
        {
            try
            {
                ExecSQL(" DROP TABLE t_report_71_Nach2 ");
                ExecSQL(" DROP TABLE t_temp_status ");
                ExecSQL(" DROP TABLE t_doms ");
                ExecSQL(" DROP TABLE t_svod_all ");
                ExecSQL(" DROP TABLE t_title ");
                ExecSQL(" DROP TABLE t_g ");
                ExecSQL(" DROP TABLE t_gil_kvar ");
                ExecSQL(" DROP TABLE t_s ");
                ExecSQL(" DROP TABLE t_s2 ");
                ExecSQL(" DROP TABLE t_prms ");
                ExecSQL(" DROP TABLE t_prms2 ");
                ExecSQL(" DROP TABLE t_svod_mini ");
                ExecSQL(" DROP TABLE t_svod_final ");
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Отчет 'Генератор по начислениям' " + e.Message, MonitorLog.typelog.Error, false);
            }
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
                ExecSQL(" drop table selected_kvars ", true);
        }

        /// <summary>
        /// Выборка списка квартир в картотеке
        ///  </summary>
        /// <returns></returns>
        private bool GetSelectedKvars()
        {
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                using (IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata))
                {
                    if (!DBManager.OpenDb(connWeb, true).result) return false;

                    string tSpls = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter +
                                   "t" + ReportParams.User.nzp_user + "_spls";
                    if (TempTableInWebCashe(tSpls))
                    {
                        string sql =
                            " insert into selected_kvars (nzp_kvar, num_ls, pkod, fio, nzp_dom, nzp_geu, nzp_area, typek) " +
                            " select nzp_kvar, num_ls, pkod, fio, nzp_dom, nzp_geu, nzp_area, typek from " + tSpls;
                        ExecSQL(sql);
                        ExecSQL("create index ix_sel_kvar_09 on selected_kvars(nzp_kvar)");
                        ExecSQL(DBManager.sUpdStat + " selected_kvars");
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>Сформировать отчет</summary>
        /// <param name="ds">Данные отчета в виде DataSet</param>
        public override void Generate(DataSet ds)
        {
            if (ds.Tables["Q_master"].Rows.Count >= stringsInPack)
            {
                isArchive = true;
                var reports = new List<string>();
                var qm = ds.Tables["Q_master"].Copy();

                if (UserParamValues["Format"].GetValue<int>() == 1)
                    ds.Tables.Remove("Q_master1");

                int count = 0;
                string nzpUtil = ReportParams.PathForSave.Substring(ReportParams.PathForSave.LastIndexOf('/') + 1,
                    ReportParams.PathForSave.LastIndexOf('.') - ReportParams.PathForSave.LastIndexOf('/') - 1);
                do
                {
                    ds.Tables.Remove("Q_master");
                    ds.Tables.Add(
                        qm.AsEnumerable()
                            .Skip(count * stringsInPack)
                            .Take(qm.Rows.Count - (count + 1) * stringsInPack > 0 ? stringsInPack : qm.Rows.Count)
                            .CopyToDataTable());
                    ds.Tables[ds.Tables.Count - 1].TableName = "Q_master";

                    var report = new FastReport.Report();
                    report.Load(GetTemplate());
                    report.RegisterData(ds);
                    PrepareReport(report);

                    var cleanPath = ReportParams.PathForSave;
                    ReportParams.PathForSave = ReportParams.PathForSave.Insert(ReportParams.PathForSave.LastIndexOf('.'),
                        "_" + (count * stringsInPack + 1) + "-" +
                        (qm.Rows.Count - (count + 1) * stringsInPack > 0
                            ? (count + 1) * stringsInPack
                            : qm.Rows.Count));
                    SaveReport(report);

                    reports.Add(ReportParams.PathForSave);
                    if (((count + 1) * stringsInPack) / qm.Rows.Count < 1)
                        SetProccessPercent((((count + 1) * stringsInPack) / qm.Rows.Count).ToDecimal());

                    ReportParams.PathForSave = cleanPath;
                    count++;
                } while (qm.Rows.Count - (count) * stringsInPack > 0);

                var exporter = GetExporter();
                if (reports.Count > 0)
                    Archive.GetInstance().Compress(Constants.ExcelDir.TrimEnd('/') + @"\" + Name + ".zip", reports.ToArray(), true);

                if (InputOutput.useFtp)
                {
                    ReportParams.PathForSave = InputOutput.SaveOutputFile(Constants.ExcelDir.TrimEnd('/') + @"\" + Name + ".zip");
                    ExecSQL(" update public.excel_utility " +
                            " set exc_path = '" + ReportParams.PathForSave + "' " +
                            " where nzp_exc = " + nzpUtil);
                    ExecSQL(" update public.excel_utility " +
                            " set file_name = '" + ReportParams.PathForSave.Substring(0, ReportParams.PathForSave.LastIndexOf('.')) + ".zip" + "' " +
                            " where nzp_exc = " + nzpUtil);
                }
            }
            else
                base.Generate(ds);
        }

        /// <summary>Сохранить отчет</summary>
        /// <param name="report">Отчет</param>
        protected override void SaveReport(FastReport.Report report)
        {
            var exporter = GetExporter();
            exporter.ShowProgress = false;
            var env = new EnvironmentSettings { ReportSettings = { ShowProgress = false } };
            report.Prepare();
            if (IsPreview)
            {
                report.SavePrepared(ReportParams.PathForSave);
            }
            else
            {
                exporter.Export(report, ReportParams.PathForSave);
            }
            if (!isArchive && InputOutput.useFtp)
            {
                ReportParams.PathForSave = InputOutput.SaveOutputFile(ReportParams.PathForSave);
            }
        }
    }
}
