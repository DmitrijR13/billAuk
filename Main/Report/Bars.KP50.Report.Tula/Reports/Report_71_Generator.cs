using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
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
using Excel = Microsoft.Office.Interop.Excel;

namespace Bars.KP50.Report.Tula.Reports
{
    public class Report71Generator : BaseSqlReport
    {




        public override string Name
        {
            get { return "71. Генератор"; }
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
            get { return Resources._71_Generator; }
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

        /// <summary>Районы </summary>
        private List<int> RaionsDoms { get; set; }

        /// <summary>Населенные пункты </summary>
        private List<int> Localitys { get; set; }

        /// <summary>ИПУ </summary>
        private int Ipu { get; set; }

        /// <summary>ОДПУ </summary>
        private int Odpu { get; set; }

        /// <summary>Признак расчета </summary>
        private int Kind { get; set; }

        /// <summary>Учитывать все ЛС в доме</summary>
        private int AllLs { get; set; }

        /// <summary>Итого</summary>
        private List<int> SummariesValues { get; set; }

        ///// <summary>Учитывать все ЛС в доме</summary>
        //private int StatusGil { get; set; }

        /// <summary>Приватизировано</summary>
        private int Privat { get; set; }

        /// <summary>Статус жилья</summary>
        private List<int> StatusGil { get; set; }

        /// <summary>Нулевые счета</summary>
        private int IsEmpty { get; set; }

        /// <summary>Кол-во строк в отчете при разбиении и архивации</summary>
        private int StringsCount { get; set; }

        private int _rowCount;
        private bool _isArchive;
        private int _totalOdpuCounters;
        private int _ye, _mo;
        private TableAlias tableAlias;
        private string _datS, _datPo;
        private List<Spoint> _prefs;
        private readonly string[] _months = {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};

        private enum Fields
        {
            Territory = 1, Geu, Uchastok, AccountStatus, IsIpu, IsOdpu, CalculationSign, OneGkalHeatingStandart, Rajon, Locality, Street,
            Building, Appartment, Account, Fio, PrescriptedCount, TemporaryLiving, TemporaryOut, TotalSquare, HeatingSquare,
            LivingSquare, Rooms, Floor, PayCode, Supplier, Service, Insaldo, Tarif, Consumption, ShortDelivery, ChargedWithShortDelivery,
            ForPayment, Paid, PrevBillingPayment, DirectSupplierPayment, ChargesCorrection, InsaldoCorrection, Charged, Reval, Outsaldo,
            Privat, EnterDate, StatusGil, DataBank, OneGkalHotWaterStandart, BoilerHotWater, BoilerHeating, WaterPumping, Normative, IsMkd, MonthYear
        }

        private enum AccountStatus { Open = 1, Closed, Undefined }
        private enum IpuEnum { Yes = 1, No, Undefined }
        private enum OdpuEnum { Yes = 1, No, Undefined }
        private enum CalculationSign { Norma = 1, CountersValues, MonthAverage, OdpuValues, NonLivingPlaces, Undefined }
        private enum Privatization { Open = 1, Closed, Undefined }
        private enum GilStatus { NotPrivate = 0, Private, SecondlyPrivate, Duty, Owner, Juristic, Fond }
        private enum AllAccounts { Yes = 1, No }
        private enum Unformat { Yes = 1, No }
        private enum Summaries { ByAccount = 1, ByBuilding }
        private enum IsEmptyAccounts { Full = 1, Old, Current }

        #endregion
        protected override Stream GetTemplate()
        {
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
                new RaionsParameter { Name = "Населенный пункт", Code = "Localitys"},
                new ComboBoxParameter(true)
                {
                    Name = "Статус ЛС",
                    Code = "StatusLS",
                    StoreData = new List<object>
                    {
                        new { Id = (int)AccountStatus.Open, Name = "Открытый" },
                        new { Id = (int)AccountStatus.Closed, Name = "Закрытый" },
                        new { Id = (int)AccountStatus.Undefined, Name = "Неопределен" }
                    }
                },
                new ComboBoxParameter(false)
                {
                    Name = "Наличие ИПУ",
                    Code = "IPU",
                    Value = (int)IpuEnum.Undefined,
                    StoreData = new List<object>
                    {
                        new { Id = (int)IpuEnum.Yes, Name = "Есть" },
                        new { Id = (int)IpuEnum.No, Name = "Нет" },
                        new { Id = (int)IpuEnum.Undefined, Name = "Неопределено" }
                    }
                },
                new ComboBoxParameter(false)
                {
                    Name = "Наличие ОДПУ",
                    Code = "ODPU",
                    Value = (int)OdpuEnum.Undefined,
                    StoreData = new List<object>
                    {
                        new { Id = (int)OdpuEnum.Yes, Name = "Есть" },
                        new { Id = (int)OdpuEnum.No, Name = "Нет" },
                        new { Id = (int)OdpuEnum.Undefined, Name = "Неопределено" }
                    }
                },
                new ComboBoxParameter(false)
                {
                    Name = "Признак расчета",
                    Code = "Kind",
                    Value = (int)CalculationSign.Undefined,
                    StoreData = new List<object>
                    {
                        new { Id = (int)CalculationSign.Norma, Name = "Норматив" },
                        new { Id = (int)CalculationSign.CountersValues, Name = "Показание ПУ" },
                        new { Id = (int)CalculationSign.MonthAverage, Name = "Среднемесячное потребление по ПУ" },
                        new { Id = (int)CalculationSign.OdpuValues, Name = "Исходя из показаний ОДПУ" },
                        new { Id = (int)CalculationSign.NonLivingPlaces, Name = "Расчетный способ для нежилых помещений" },
                        new { Id = (int)CalculationSign.Undefined, Name = "Неопределен" }
                    }
                },                     
                new ComboBoxParameter(false)
                {
                    Name = "Приватизировано",
                    Code = "Privat",
                    Value = (int)Privatization.Undefined,
                    StoreData = new List<object>
                    {
                        new { Id = (int)Privatization.Open, Name = "Да" },
                        new { Id = (int)Privatization.Closed, Name = "Нет" },
                        new { Id = (int)Privatization.Undefined, Name = "Неопределено" }
                    }
                },                     
                new ComboBoxParameter(true)
                {
                    Name = "Статус жилья",
                    Code = "StatusGil",
                    StoreData = new List<object>
                    {
                        new { Id = (int)GilStatus.NotPrivate, Name = "Неприватизированная" },
                        new { Id = (int)GilStatus.Private, Name = "Приватизированная" },
                        new { Id = (int)GilStatus.SecondlyPrivate, Name = "Вторично приватизированная" },
                        new { Id = (int)GilStatus.Duty, Name = "Служебная" },
                        new { Id = (int)GilStatus.Owner, Name = "Собственник" },
                        new { Id = (int)GilStatus.Juristic, Name = "Юр.лицо" },
                        new { Id = (int)GilStatus.Fond, Name = "Маневренный фонд" }
                    }
                },                     
                new ComboBoxParameter
                {
                    Name = "Включать нулевые начисления",
                    Code = "IsEmptyAccounts",
                    Value = (int)IsEmptyAccounts.Old,
                    StoreData = new List<object>
                    {
                        new { Id = (int)IsEmptyAccounts.Full, Name = "Нет начислений" },
                        new { Id = (int)IsEmptyAccounts.Old, Name = "Есть долг/переплата или начисления" },
                        new { Id = (int)IsEmptyAccounts.Current, Name = "Есть ежемесячные начисления" }
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
                        new { Id = (int)Fields.DataBank, Name = "Банк данных"},
                        new { Id = (int)Fields.MonthYear, Name = "Месяц"},
                        new { Id = (int)Fields.Territory, Name = "Территория (УК)"},
                        new { Id = (int)Fields.Geu, Name = "ЖЭУ"},
                        new { Id = (int)Fields.Uchastok, Name = "Участок"},
                        new { Id = (int)Fields.AccountStatus, Name = "Статус ЛС"},
                        new { Id = (int)Fields.IsIpu, Name = "Наличие ИПУ"},
                        new { Id = (int)Fields.IsOdpu, Name = "Наличие ОДПУ"},
                        new { Id = (int)Fields.CalculationSign, Name = "Признак расчета"},
                        new { Id = (int)Fields.OneGkalHeatingStandart, Name = "Домовой норматив на 1 ГКал/кв.м отопления"},
                        new { Id = (int)Fields.OneGkalHotWaterStandart, Name = "Домовой норматив на 1 ГКал/куб.м горячей воды"},
                        new { Id = (int)Fields.Rajon, Name = "Район"},
                        new { Id = (int)Fields.Locality, Name = "Населенный пункт"},
                        new { Id = (int)Fields.Street, Name = "Улица"},
                        new { Id = (int)Fields.Building, Name = "Дом"},
                        new { Id = (int)Fields.Appartment, Name = "Квартира"},
                        new { Id = (int)Fields.Account, Name = "Лицевой счет"},
                        new { Id = (int)Fields.Fio, Name = "ФИО"},
                        new { Id = (int)Fields.PrescriptedCount, Name = "Количество прописанных"},
                        new { Id = (int)Fields.TemporaryLiving, Name = "Количество временно проживающих"},
                        new { Id = (int)Fields.TemporaryOut, Name = "Количество временно выбывших"},
                        new { Id = (int)Fields.TotalSquare, Name = "Общая площадь"},
                        new { Id = (int)Fields.HeatingSquare, Name = "Отапливаемая площадь"},
                        new { Id = (int)Fields.LivingSquare, Name = "Жилая площадь"},
                        new { Id = (int)Fields.Rooms, Name = "Количество комнат"},
                        new { Id = (int)Fields.Floor, Name = "Этаж"},
                        new { Id = (int)Fields.PayCode, Name = "Платежный код"},
                        new { Id = (int)Fields.Supplier, Name = "Поставщик"},
                        new { Id = (int)Fields.Service, Name = "Услуга"},
                        new { Id = (int)Fields.Insaldo, Name = "Входящее сальдо"},
                        new { Id = (int)Fields.Tarif, Name = "Тариф"},
                        new { Id = (int)Fields.Consumption, Name = "Расход"},
                        new { Id = (int)Fields.ShortDelivery, Name = "Недопоставка"},
                        new { Id = (int)Fields.ChargedWithShortDelivery, Name = "Начислено с учетом недопоставки"},
                        new { Id = (int)Fields.ForPayment, Name = "К оплате"},
                        new { Id = (int)Fields.Paid, Name = "Оплачено"},
                        new { Id = (int)Fields.PrevBillingPayment, Name = "Оплаты предыдущих биллинговых систем"},
                        new { Id = (int)Fields.DirectSupplierPayment, Name = "Оплата напрямую поставщикам"},
                        new { Id = (int)Fields.ChargesCorrection, Name = "Корректировка начислений"},
                        new { Id = (int)Fields.InsaldoCorrection, Name = "Корректировка входящего сальдо"},
                        new { Id = (int)Fields.Charged, Name = "Начислено"},
                        new { Id = (int)Fields.Reval, Name = "Перерасчет"},
                        new { Id = (int)Fields.Outsaldo, Name = "Исходящее сальдо"},
                        new { Id = (int)Fields.Privat, Name = "Приватизировано"},
                        new { Id = (int)Fields.EnterDate, Name = "Дата последней оплаты ЛС"},
                        new { Id = (int)Fields.StatusGil, Name = "Статус жилья"},
                        new { Id = (int)Fields.BoilerHotWater, Name = "Котельная по горячей воде"},
                        new { Id = (int)Fields.BoilerHeating, Name = "Котельная по отоплению"},
                        new { Id = (int)Fields.WaterPumping, Name = "Водозабор"},
                        new { Id = (int)Fields.Normative, Name = "Норматив и объем по услуге"},
                        new { Id = (int)Fields.IsMkd, Name = "МКД/Частный сектор"}
                    }
                },
                new ComboBoxParameter
                {                    
                    Name = "Учитывать все ЛС в доме",
                    Code = "AllLs",
                    Value = (int)AllAccounts.No,
                    StoreData = new List<object>
                    {
                        new { Id = (int)AllAccounts.Yes, Name = "Да" },
                        new { Id = (int)AllAccounts.No, Name = "Нет" }
                    }
                },
                new ComboBoxParameter
                {                    
                    Name = "Без форматирования",
                    Code = "Unformat",
                    Value = (int)Unformat.No,
                    StoreData = new List<object>
                    {
                        new { Id = (int)Unformat.Yes, Name = "Да" },
                        new { Id = (int)Unformat.No, Name = "Нет" }
                    }
                },
                new RowCountParameter(),
                new ComboBoxParameter(true)
                {                    
                    Name = "Итого",
                    Code = "Summaries",
                    Require = false,
                    StoreData = new List<object>
                    {
                        new { Id = (int)Summaries.ByAccount, Name = "По лицевым счетам" },
                        new { Id = (int)Summaries.ByBuilding, Name = "По домам" }
                    }
                }
            };
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            string statusLs = string.Empty;
            if (StatusLs != null)
            {
                statusLs = StatusLs.Contains((int)AccountStatus.Open) ? "Открытый, " : string.Empty;
                statusLs += StatusLs.Contains((int)AccountStatus.Closed) ? "Закрытый, " : string.Empty;
                statusLs += StatusLs.Contains((int)AccountStatus.Undefined) ? "Неопределен, " : string.Empty;
                statusLs = statusLs.TrimEnd(',', ' ');
            }
            report.SetParameterValue("dat", DateTime.Now.ToLongDateString());
            report.SetParameterValue("time", DateTime.Now.ToLongTimeString());
            if (MonthS == MonthPo && YearS == YearPo)
            {
                report.SetParameterValue("pPeriod", _months[MonthS] + " " + YearS + "г.");
            }
            else
            {
                report.SetParameterValue("pPeriod", "c " + _months[MonthS] + " " + YearS + "г. по " + _months[MonthPo] + " " + YearPo + "г.");
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
            Localitys = UserFilterValues["Localitys"].GetValue<List<int>>();
            RaionsDoms = UserFilterValues["RaionsDoms"].GetValue<List<int>>();
            FilteredServices = JsonConvert.DeserializeObject<ServiceParameterValue>(UserFilterValues["Services"].Value.ToString());
            Ipu = UserFilterValues["IPU"].GetValue<int>();
            Odpu = UserFilterValues["ODPU"].GetValue<int>();
            Kind = UserFilterValues["Kind"].GetValue<int>();
            Privat = UserFilterValues["Privat"].GetValue<int>();
            StatusGil = UserFilterValues["StatusGil"].GetValue<List<int>>();
            IsEmpty = UserFilterValues["IsEmptyAccounts"].GetValue<int>();
            //Параметры
            Services = JsonConvert.DeserializeObject<ServiceParameterValue>(UserParamValues["Services"].Value.ToString());
            Params = UserParamValues["Params"].GetValue<List<long>>();
            SummariesValues = UserParamValues["Summaries"].GetValue<List<int>>() ?? new List<int>();
            StringsCount = UserParamValues["RowCount"].GetValue<int>();
            if (Params.Contains((int)Fields.Appartment))
            {
                if (!Params.Contains((int)Fields.Building)) { Params.Add((int)Fields.Building); }
                if (!Params.Contains((int)Fields.Street)) { Params.Add((int)Fields.Street); }
                if (!Params.Contains((int)Fields.Locality)) { Params.Add((int)Fields.Locality); }
                if (!Params.Contains((int)Fields.Rajon)) { Params.Add((int)Fields.Rajon); }
            }
            if (Params.Contains((int)Fields.Building) || SummariesValues.Contains((int)Summaries.ByBuilding))
            {
                if (!Params.Contains((int)Fields.Street)) { Params.Add((int)Fields.Street); }
                if (!Params.Contains((int)Fields.Locality)) { Params.Add((int)Fields.Locality); }
                if (!Params.Contains((int)Fields.Rajon)) { Params.Add((int)Fields.Rajon); }
            }
            if (Params.Contains((int)Fields.Street))
            {
                if (!Params.Contains((int)Fields.Locality)) { Params.Add((int)Fields.Locality); }
            }
            AllLs = UserParamValues["AllLs"].GetValue<int>();
        }


        public override DataSet GetData()
        {

            string whereSupp = GetWhereSupp("c.nzp_supp");
            string whereServFilter = GetWhereServFilter();
            string whereServ = GetWhereServ();
            string whereRaj = GetWhereLocalitys("u.");
            string whereRajDom = GetWhereRajDom("d.");
            bool listLc = GetSelectedKvars();
            var joinWord = IsEmpty == (int)IsEmptyAccounts.Full ? " LEFT JOIN " : " INNER JOIN ";

            PrepareData(listLc, joinWord, whereServFilter, whereSupp, whereRaj, whereRajDom, whereServ);

            #region изменения необходимые после циклов
            ExecSQL(" SELECT nzp_dom, nzp_kvar, nzp_serv, MAX(status) as status, MAX(ipu) as ipu, MAX(odpu) as odpu, MAX(rasch) as rasch, MAX(heating) heating, " +
                    " MAX(hotwater) as hotwater, MAX(boiler_hotwater) as boiler_hotwater, MAX(boiler_heating) as boiler_heating, MAX(water_pumping) as water_pumping, " +
                    " MAX(mkd) as mkd, MAX(norm_name) as norm_name, MAX(norm_value) as norm_value " +
                    " INTO TEMP t_params2_temp " +
                    " FROM t_params2 a" +
                    " WHERE EXISTS (SELECT nzp_kvar " +
                    "               FROM t_kvars b " +
                    "               WHERE a.nzp_kvar=b.nzp_kvar) " +
                    " GROUP BY 1,2,3 ");
            ExecSQL(" TRUNCATE t_params2 ");
            ExecSQL(" INSERT INTO t_params2 SELECT * FROM t_params2_temp ");
            ExecSQL(" DROP TABLE t_params2_temp ");

            if (Params.Contains((int)Fields.StatusGil))
            {
                var res = ReportParams.Pref + DBManager.sKernelAliasRest + "res_y ";
                if (TempTableInWebCashe(res))
                {
                    ExecSQL(" UPDATE t_params SET status_gil =  a.name_y " +
                            " FROM " + res + " a" +
                            " WHERE a.nzp_res = 3001 " +
                            " AND a.nzp_y = t_params.status_gil" + DBManager.sConvToInt);
                }
            }
            #endregion

            #region группировка сортировка
            string group = string.Empty, agreg = string.Empty;
            if (Params.Contains((int)Fields.Territory)) { group += " area, "; }
            if (Params.Contains((int)Fields.Geu)) { group += " geu, "; }
            if (Params.Contains((int)Fields.Uchastok)) { group += " uch, "; }
            if (Params.Contains((int)Fields.AccountStatus)) { agreg += " MAX(status), "; }
            if (Params.Contains((int)Fields.IsIpu)) { agreg += " SUM(ipu), "; }
            if (Params.Contains((int)Fields.IsOdpu)) { agreg += " SUM(odpu), "; }
            if (Params.Contains((int)Fields.CalculationSign)) { agreg += " MAX(rasch), "; }
            if (Params.Contains((int)Fields.OneGkalHeatingStandart)) { agreg += " MAX(heating), "; }
            if (Params.Contains((int)Fields.OneGkalHotWaterStandart)) { agreg += " MAX(hotwater), "; }
            if (Params.Contains((int)Fields.Rajon)) { group += " rajon_dom, "; }
            if (Params.Contains((int)Fields.Locality)) { group += " town, rajon, "; }
            if (Params.Contains((int)Fields.Street)) { group += " ulica, "; }
            if (Params.Contains((int)Fields.Building) || SummariesValues.Contains((int)Summaries.ByBuilding)) { group += " nzp_dom, idom, ndom, nkor, "; }
            if (Params.Contains((int)Fields.Appartment)) { group += " ikvar, nkvar, "; }
            if (Params.Contains((int)Fields.Account) || SummariesValues.Contains((int)Summaries.ByAccount)) { group += " num_ls, "; }
            if (Params.Contains((int)Fields.Fio)) { group += " fio, "; }
            if (Params.Contains((int)Fields.PrescriptedCount)) { agreg += " SUM(propis_count), "; }
            if (Params.Contains((int)Fields.TemporaryLiving)) { agreg += " SUM(gil_prib), "; }
            if (Params.Contains((int)Fields.TemporaryOut)) { agreg += " SUM(gil_ub), "; }
            if (Params.Contains((int)Fields.TotalSquare)) { agreg += " SUM(ob_s), "; }
            if (Params.Contains((int)Fields.HeatingSquare)) { agreg += " SUM(otop_s), "; }
            if (Params.Contains((int)Fields.LivingSquare)) { agreg += " SUM(gil_s), "; }
            if (Params.Contains((int)Fields.Rooms)) { agreg += " SUM(rooms), "; }
            if (Params.Contains((int)Fields.Floor)) { agreg += " MAX(floor), "; }
            if (Params.Contains((int)Fields.PayCode)) { group += " pkod, "; }
            if (Params.Contains((int)Fields.Supplier)) { group += " name_supp, "; }
            if (Params.Contains((int)Fields.Service)) { group += " nzp_serv, service, "; }
            if (Params.Contains((int)Fields.Insaldo)) { agreg += " SUM(sum_insaldo), "; }
            if (Params.Contains((int)Fields.Tarif)) { group += " tarif, "; }
            if (Params.Contains((int)Fields.Consumption)) { agreg += " SUM(c_calc), "; }
            if (Params.Contains((int)Fields.ShortDelivery)) { agreg += " SUM(sum_nedop), "; }
            if (Params.Contains((int)Fields.ChargedWithShortDelivery)) { agreg += " SUM(sum_tarif), "; }
            if (Params.Contains((int)Fields.ForPayment)) { agreg += " SUM(sum_charge), "; }
            if (Params.Contains((int)Fields.Paid)) { agreg += " SUM(money_to), "; }
            if (Params.Contains((int)Fields.PrevBillingPayment)) { agreg += " SUM(money_from), "; }
            if (Params.Contains((int)Fields.DirectSupplierPayment)) { agreg += " SUM(money_supp), "; }
            if (Params.Contains((int)Fields.ChargesCorrection)) { agreg += " SUM(real_charge), "; }
            if (Params.Contains((int)Fields.InsaldoCorrection)) { agreg += " SUM(real_insaldo), "; }
            if (Params.Contains((int)Fields.Charged)) { agreg += " SUM(rsum_tarif), "; }
            if (Params.Contains((int)Fields.Reval)) { agreg += " SUM(reval), "; }
            if (Params.Contains((int)Fields.Outsaldo)) { agreg += " SUM(sum_outsaldo), "; }
            if (Params.Contains((int)Fields.Privat)) { agreg += " MAX(privat), "; }
            if (Params.Contains((int)Fields.EnterDate)) { agreg += " MAX(dat_vvod), "; }
            if (Params.Contains((int)Fields.StatusGil)) { agreg += " MAX(status_gil), "; }
            if (Params.Contains((int)Fields.DataBank)) { group += " pref, "; }
            if (Params.Contains((int)Fields.MonthYear)) { group += " monthyear, "; }
            if (Params.Contains((int)Fields.BoilerHotWater)) { agreg += " MAX(boiler_hotwater), "; }
            if (Params.Contains((int)Fields.BoilerHeating)) { agreg += " MAX(boiler_heating), "; }
            if (Params.Contains((int)Fields.WaterPumping)) { agreg += " MAX(water_pumping), "; }
            if (Params.Contains((int)Fields.IsMkd)) { agreg += " MAX(mkd), "; }
            group = group.TrimEnd(',', ' '); agreg = agreg.TrimEnd(',', ' ');
            #endregion


            #region Группировка начислений
            ExecSQL(" INSERT INTO t_svod_grouped (nzp_kvar, " + (group != string.Empty ? group + "," : string.Empty) +
                    "   sum_insaldo, c_calc, sum_nedop, sum_tarif, sum_charge,  money_to, money_from, money_supp, " +
                    "   real_charge, real_insaldo, rsum_tarif, reval, sum_outsaldo, dat_vvod) " +
                    " SELECT nzp_kvar, " + (group != string.Empty ? group + "," : string.Empty) +
                    "   SUM(sum_insaldo), SUM(c_calc), SUM(sum_nedop), SUM(sum_tarif), SUM(sum_charge), " +
                    "   SUM(money_to), SUM(money_from), SUM(money_supp), SUM(real_charge), " +
                    "   SUM(real_insaldo), SUM(rsum_tarif), SUM(reval), SUM(sum_outsaldo), MAX(dat_vvod) " +
                    " FROM t_svod " +
                    " GROUP BY nzp_kvar " + (group != string.Empty ? "," + group : string.Empty));
            #endregion

            #region Группировка параметров
            ExecSQL(" INSERT INTO t_params_grouped (nzp_kvar, propis_count, gil_prib, gil_ub, ob_s, otop_s, " +
                    " gil_s, rooms, floor, privat, status_gil) " +
                    " SELECT nzp_kvar, MAX(propis_count), MAX(gil_prib), MAX(gil_ub), MAX(ob_s), MAX(otop_s), " +
                    " MAX(gil_s), MAX(rooms), MAX(floor), MAX(privat), MAX(status_gil) " +
                    " FROM t_params GROUP BY 1 ");
            #endregion

            #region Группировка дополнительных параметров
            ExecSQL(" INSERT INTO t_params_grouped2 (nzp_dom, nzp_kvar, nzp_serv, status, ipu, odpu, rasch, " +
                    " heating, hotwater, boiler_hotwater, boiler_heating, water_pumping, mkd, norm_name, norm_value) " +
                    " SELECT  nzp_dom, nzp_kvar, " + (Params.Contains((int)Fields.Service) ? "nzp_serv" : "MAX(0)") +
                    " , MAX(status), SUM(ipu), SUM(odpu), MAX(rasch), MAX(heating), MAX(hotwater), MAX(boiler_hotwater), " +
                    " MAX(boiler_heating), MAX(water_pumping), MAX(mkd) " + (Params.Contains((int)Fields.Normative) ? ", norm_name, " +
                    "norm_value " : ", MAX(0), MAX(0)") +
                    " FROM t_params2 " +
                    " GROUP BY " + (Params.Contains((int)Fields.Service) ? "nzp_dom, nzp_kvar, nzp_serv"
                    : "nzp_dom, nzp_kvar") + (Params.Contains((int)Fields.Normative) ? ", norm_name, norm_value " : ""));

            ExecSQL(" UPDATE t_params_grouped2 SET status = 'неопределено' WHERE status = '' OR status IS NULL ");

            ExecSQL(" CREATE INDEX svod_grouped_ndx ON t_svod_grouped(" + (group != string.Empty ? group + "," : string.Empty) + " nzp_kvar) ");
            ExecSQL(DBManager.sUpdStat + " t_svod_grouped ");
            ExecSQL(" CREATE INDEX params_grouped_ndx ON t_params_grouped(nzp_kvar) ");
            ExecSQL(DBManager.sUpdStat + " t_params_grouped ");
            ExecSQL(" CREATE INDEX params_grouped2_ndx ON t_params_grouped2(nzp_kvar) ");
            ExecSQL(DBManager.sUpdStat + " t_params_grouped2 ");
            #endregion

            if (Params.Contains((int)Fields.Normative)) { group += ", norm_name, norm_value "; }
            ExecSQL(" INSERT INTO t_svod_all (" + group + (group == string.Empty || agreg == string.Empty ? string.Empty : ",") + agreg.Replace("SUM(", "").Replace("MAX(", "").Replace(")", "") + ") " +
                    " SELECT " + group.Replace("nzp_serv", "t.nzp_serv").Replace("nzp_dom", "t.nzp_dom") + (group == string.Empty || agreg == string.Empty ? string.Empty : ",") + agreg +
                    " FROM t_svod_grouped t " +
                    " LEFT JOIN t_params_grouped p ON t.nzp_kvar = p.nzp_kvar " +
                    " LEFT JOIN t_params_grouped2 p2 ON t.nzp_kvar = p2.nzp_kvar " +
                    (Params.Contains((int)Fields.Service) ? " AND t.nzp_serv = p2.nzp_serv " : "") +
                    (group != string.Empty ? " GROUP BY " +
                    group.Replace("nzp_serv", "t.nzp_serv").Replace("nzp_dom", "t.nzp_dom").TrimStart(',') : ""));

            if (SummariesValues.Count > 0 && !string.IsNullOrEmpty(agreg))
            {
                SetSummaries(group, agreg);
            }
            else
            {
                ExecSQL(" SELECT * INTO TEMP t_all_all" +
                        " FROM t_svod_all " + (!string.IsNullOrWhiteSpace(group)
                        ? "ORDER BY " + group.TrimStart(',')
                        : string.Empty));
            }
            var ds = new DataSet();
            _rowCount = Convert.ToInt32(ExecSQLToTable(" SELECT COUNT(*) FROM t_all_all ").Rows[0][0]);
            for (int t = 0; t * StringsCount < _rowCount; t++)
            {
                ExecSQL(" SELECT * INTO TEMP t_all FROM t_all_all LIMIT " + StringsCount + " OFFSET " + t * StringsCount);
                DataTable dt;
                if (ReportParams.ExportFormat == ExportFormat.Excel2007 && UserParamValues["Unformat"].GetValue<int>() == (int)Unformat.Yes)
                    dt = ExecSQLToTable(" SELECT * FROM t_all ");
                else
                    dt = FillMatrix();
                dt.TableName = "Q_master" + (t + 1);
                ds.Tables.Add(dt);
                ExecSQL(" DROP TABLE t_all ");
            }

            ExecSQL(" DROP TABLE t_all_all ");
            return ds;
        }

        /// <summary>
        /// Запись из таблицы s_point
        /// </summary>
        private class Spoint
        {
            public string Pref { get; set; }
            public string Point { get; set; }
        }

        /// <summary>
        /// Подготовка детализированной информации для отчета
        /// </summary>
        /// <param name="listLc"></param>
        /// <param name="joinWord"></param>
        /// <param name="whereServFilter"></param>
        /// <param name="whereSupp"></param>
        /// <param name="whereRaj"></param>
        /// <param name="whereRajDom"></param>
        /// <param name="whereServ"></param>
        private void PrepareData(bool listLc, string joinWord, string whereServFilter, string whereSupp,
            string whereRaj, string whereRajDom, string whereServ)
        {
            var sql = " SELECT bd_kernel as pref, point " +
                      " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                      " WHERE nzp_wp>1 " + GetwhereWp();
            if (listLc)
                sql.Append(" AND nzp_wp in (SELECT nzp_wp FROM selected_kvars a, " +
                           ReportParams.Pref + DBManager.sDataAliasRest + "kvar k " +
                           " WHERE a.nzp_kvar=k.nzp_kvar )");

            _prefs = ExecSQLToTable(sql).Select().Select(r => new Spoint { Pref = r["pref"].ToString().Trim(), Point = r["point"].ToString().Trim() }).ToList();
            _datS = new DateTime(YearS, MonthS, 1).ToShortDateString();
            _datPo = new DateTime(YearPo, MonthPo, 1).AddMonths(1).AddDays(-1).ToShortDateString();

            //var formingTime = new Stopwatch();
            //formingTime.Start();
            BuildBaseKvar(listLc, joinWord, whereServFilter, whereSupp);
            //4var sometable = ExecSQLToTable(" SELECT * FROM t_kvars ");
            //formingTime.Stop();
            //Console.WriteLine("BuildBaseKvar" + formingTime.Elapsed.Hours + ":" + formingTime.Elapsed.Minutes + ":" + formingTime.Elapsed.Seconds + ":" + formingTime.Elapsed.Milliseconds);

            BuildMainTable(joinWord, whereRaj, whereRajDom, whereServ, whereSupp);
            UpdateMainTable(whereServ);
            FillDopParams(whereServFilter, whereServ);
        }


        /// <summary>
        /// Заполнение дополнительных параметров
        /// </summary>
        /// <param name="tableAlias"></param>
        /// <param name="datPo"></param>
        /// <param name="datS"></param>
        /// <param name="whereServFilter"></param>
        /// <param name="whereServ"></param>
        private void FillDopParams(string whereServFilter, string whereServ)
        {
            foreach (var pref in _prefs)
            {
                for (var i = YearS * 12 + MonthS; i < YearPo * 12 + MonthPo + 1; i++)
                {
                    var mo = GetMonth(i);
                    var ye = GetYear(i);
                    tableAlias = new TableAlias(pref.Pref, ye, mo);
                    var datPo = new DateTime(ye, mo, 1).AddMonths(1).AddDays(-1).ToShortDateString();
                    if (tableAlias.InitAlias(Connection))
                    {
                        if (Params.Contains((int)Fields.PrescriptedCount) ||
                            Params.Contains((int)Fields.TemporaryLiving) || Params.Contains((int)Fields.TemporaryOut))
                        {
                            ExecSQL(" INSERT INTO t_ais_pasp (nzp_kvar, propis_count, gil_prib, gil_ub) " +
                                    " SELECT g.nzp_kvar, cnt2 AS propis_count, ROUND(val3) AS gil_prib, ROUND(val5) AS gil_ub " +
                                    " FROM " + tableAlias.gilAis + " g, t_kvars t " +
                                    " WHERE g.nzp_kvar = t.nzp_kvar " +
                                    (IsEmpty == (int)IsEmptyAccounts.Old
                                        ? " AND  EXISTS (SELECT 1 FROM " + tableAlias.calcGkuTable + " c " +
                                          " WHERE g.nzp_kvar=c.nzp_kvar AND stek=3 " + whereServFilter + ")"
                                        : "") +
                                    " AND stek=3 ");

                            ExecSQL(" INSERT INTO t_params (nzp_kvar, propis_count, gil_prib, gil_ub) " +
                                    " SELECT nzp_kvar, propis_count, gil_prib, gil_ub " +
                                    " FROM t_ais_pasp ");

                            ExecSQL(" TRUNCATE t_ais_pasp ");
                        }
                        if (Params.Contains((int)Fields.IsIpu))
                        {
                            ExecSQL(" INSERT INTO t_params2 (nzp_kvar, nzp_serv, ipu) " +
                                    " SELECT nzp, nzp_serv, count(distinct nzp_counter) " +
                                    " FROM  " + tableAlias.countersSpisTable + " c, t_kvars t " +
                                    " WHERE nzp_type = 3 " +
                                    " AND c.nzp = t.nzp_kvar " + whereServFilter + whereServ +
                                    " AND (dat_close > '" + datPo + "' OR dat_close IS NULL) " +
                                    " AND is_actual = 1 " +
                                    (IsEmpty == (int)IsEmptyAccounts.Old
                                        ? " AND EXISTS (SELECT 1 FROM " + tableAlias.calcGkuTable + " g" +
                                          " WHERE c.nzp=g.nzp_kvar AND stek=3 AND c.nzp_serv=g.nzp_serv ) "
                                        : "") +
                                    " GROUP BY 1,2 ");
                        }
                        if (Params.Contains((int)Fields.Normative))
                        {
                            ExecSQL(" INSERT INTO t_params2 (nzp_kvar, nzp_serv, norm_name, norm_value) " +
                                    " SELECT c.nzp_kvar, c.nzp_serv, name_y, cg.rash_norm_one " +
                                    " FROM " + tableAlias.countersTable + " c, " + tableAlias.calcGkuTable + " cg, " +
                                    tableAlias.resyTable + " y, t_kvars t " +
                                    " WHERE cg.nzp_serv > 0 AND cg.stek = 3 AND c.stek = 3 " +
                                    " AND c.cnt2 = y.nzp_y " +
                                    " AND c.cnt3 = y.nzp_res " +
                                    " AND c.nzp_kvar = cg.nzp_kvar " +
                                    " AND c.nzp_serv = cg.nzp_serv " +
                                    " AND t.nzp_kvar = c.nzp_kvar ");
                        }
                    }
                }

                tableAlias = new TableAlias(pref.Pref, YearS, MonthS);
                if (tableAlias.InitAlias(Connection))
                {
                    if (Params.Contains((int)Fields.TotalSquare) || Params.Contains((int)Fields.HeatingSquare) ||
                        Params.Contains((int)Fields.LivingSquare) || Params.Contains((int)Fields.Rooms) ||
                        Params.Contains((int)Fields.Floor) || Params.Contains((int)Fields.Privat) ||
                        Params.Contains((int)Fields.StatusGil))
                    {
                        ExecSQL(
                            " INSERT INTO t_params (nzp_kvar, ob_s, otop_s, gil_s, rooms, floor, privat, status_gil) " +
                            " SELECT a.nzp as nzp_kvar, " +
                            " CASE WHEN a.nzp_prm = 4 THEN a.val_prm" + DBManager.sConvToNum + " END AS ob_s, " +
                            " CASE WHEN a.nzp_prm = 133 THEN a.val_prm" + DBManager.sConvToNum +
                            " END AS otop_s, " +
                            " CASE WHEN a.nzp_prm = 6 THEN a.val_prm" + DBManager.sConvToNum + " END AS gil_s, " +
                            " CASE WHEN a.nzp_prm = 107 THEN a.val_prm" + DBManager.sConvToInt +
                            " END AS rooms, " +
                            " CASE WHEN a.nzp_prm = 2 THEN a.val_prm" + DBManager.sConvToInt + " END AS floor, " +
                            " CASE WHEN a.nzp_prm = 8 THEN (CASE WHEN a.val_prm = '1' THEN 'Да' ELSE 'Нет' END) END AS privat, " +
                            " CASE WHEN a.nzp_prm = 2009 THEN a.val_prm" + DBManager.sConvToInt + " END AS status_gil " +
                            " FROM " + tableAlias.prmTableOne + " a, t_kvars k " +
                            " WHERE a.nzp_prm in (4,133,6,107,2,8,2009) AND a.is_actual = 1 AND a.nzp=k.nzp_kvar " +
                            " AND a.dat_s <= '" + _datPo + "' " +
                            " AND a.dat_po >= '" + _datS + "' ");
                    }

                    if (Params.Contains((int)Fields.AccountStatus))
                    {
                        ExecSQL(" INSERT INTO t_params2 (nzp_kvar, status)" +
                                " SELECT nzp, CASE WHEN val_prm = '1' THEN 'открыт' WHEN val_prm = '2' THEN 'закрыт' ELSE 'неопределено' END " +
                                " FROM " + tableAlias.prmTableThree + " p, t_kvars t " +
                                " WHERE dat_s <= '" + _datPo + "' " +
                                " AND dat_po >= '" + _datS + "' " +
                                " AND is_actual = 1 " +
                                " AND nzp_prm = 51 " +
                                " AND p.nzp = t.nzp_kvar ");
                    }

                    if (Params.Contains((int)Fields.IsOdpu))
                    {
                        // счетчик домовой, если группировки по ЛС или квартире нет, чтобы он не дублировался:
                        ExecSQL(" INSERT INTO t_params2 (nzp_dom, nzp_kvar, nzp_serv, odpu)  " +
                                " SELECT t.nzp_dom, " +
                                (Params.Contains((int)Fields.Appartment) ||
                                 Params.Contains((int)Fields.Account) ||
                                 SummariesValues.Contains((int)Summaries.ByAccount)
                                    ? " t.nzp_kvar "
                                    : " MAX(t.nzp_kvar) AS nzp_kvar ") +
                                ", nzp_serv, count(distinct nzp_counter) FROM  " +
                                tableAlias.countersSpisTable + " c,  t_kvars t " +
                                " WHERE c.nzp = t.nzp_dom " +
                                " AND (dat_close > '" + _datPo + "' or dat_close IS NULL) " +
                                " AND nzp_type = 1 " +
                                " AND is_actual = 1 " + whereServFilter + whereServ +
                                " GROUP BY nzp_dom, " +
                                (Params.Contains((int)Fields.Appartment) ||
                                 Params.Contains((int)Fields.Account) ||
                                 SummariesValues.Contains((int)Summaries.ByAccount)
                                    ? " t.nzp_kvar, "
                                    : "") + " nzp_serv ");

                        ExecSQL(DBManager.sUpdStat + " t_kvars");
                        _totalOdpuCounters +=
                            Convert.ToInt32(ExecSQLToTable(" SELECT count(distinct nzp_counter) FROM  " +
                                                           tableAlias.countersSpisTable + " c, t_kvars t " +
                                                           " WHERE c.nzp = t.nzp_dom " +
                                                           " AND (dat_close > '" + _datPo +
                                                           "' OR dat_close IS NULL) " +
                                                           " AND nzp_type = 1 " + whereServFilter + whereServ +
                                                           " AND is_actual = 1 ").Rows[0][0]);
                    }

                    if (Params.Contains((int)Fields.CalculationSign))
                    {
                        ExecSQL(" INSERT INTO t_params2 (nzp_kvar, nzp_serv, rasch) " +
                                " SELECT t.nzp_kvar, nzp_serv, CASE WHEN is_device = 0 THEN 'Норматив' WHEN is_device = 1 THEN 'Показание ПУ' " +
                                " WHEN is_device = 9 THEN 'Среднемесячное потребление по ПУ' ELSE ''  END || " +
                                " CASE WHEN nzp_serv > 500 THEN ', Исходя из показаний ОДПУ' ELSE ''  END || " +
                                " CASE WHEN typek = 3 THEN ', Расчетный способ для нежилых помещений' ELSE '' END " +
                                " FROM t_svod a, t_kvars t WHERE a.nzp_kvar = t.nzp_kvar");
                    }

                    if (Params.Contains((int)Fields.OneGkalHeatingStandart))
                    {
                        ExecSQL(" INSERT INTO t_params2 (nzp_dom, nzp_kvar, heating) " +
                                " SELECT t.nzp_dom, t.nzp_kvar, val_prm " +
                                " FROM " + tableAlias.prmTableTwo + " p, t_kvars t " +
                                " WHERE dat_s <= '" + _datPo + "' " +
                                " AND dat_po >= '" + _datS + "' " +
                                " AND is_actual = 1 " +
                                " AND nzp_prm = 723 " +
                                " AND p.nzp = t.nzp_dom ");
                    }

                    if (Params.Contains((int)Fields.OneGkalHotWaterStandart))
                    {
                        ExecSQL(" INSERT INTO t_params2 (nzp_dom, nzp_kvar, hotwater) " +
                                " SELECT t.nzp_dom, t.nzp_kvar, val_prm " +
                                " FROM " + tableAlias.prmTableTwo + " p,  t_kvars t " +
                                " WHERE dat_s <= '" + _datPo + "' " +
                                " AND dat_po >= '" + _datS + "' " +
                                " AND is_actual = 1 " +
                                " AND nzp_prm = 436 " +
                                " AND p.nzp = t.nzp_dom ");
                    }

                    if (Params.Contains((int)Fields.BoilerHotWater))
                    {
                        ExecSQL(" INSERT INTO t_params2 (nzp_dom, nzp_kvar, boiler_hotwater) " +
                                " SELECT t.nzp_dom, t.nzp_kvar, val_prm " +
                                " FROM " + tableAlias.prmTableTwo + " p,  t_kvars t " +
                                " WHERE dat_s <= '" + _datPo + "' " +
                                " AND dat_po >= '" + _datS + "' " +
                                " AND is_actual = 1 " +
                                " AND nzp_prm = 1010141 " +
                                " AND p.nzp = t.nzp_dom ");
                    }

                    if (Params.Contains((int)Fields.BoilerHeating))
                    {
                        ExecSQL(" INSERT INTO t_params2 (nzp_dom, nzp_kvar, boiler_hotwater) " +
                                " SELECT t.nzp_dom, t.nzp_kvar, val_prm " +
                                " FROM " + tableAlias.prmTableTwo + " p,  t_kvars t " +
                                " WHERE dat_s <= '" + _datPo + "' " +
                                " AND dat_po >= '" + _datS + "' " +
                                " AND is_actual = 1 " +
                                " AND nzp_prm = 1010142 " +
                                " AND p.nzp = t.nzp_dom  ");
                    }

                    if (Params.Contains((int)Fields.WaterPumping))
                    {
                        ExecSQL(" INSERT INTO t_params2 (nzp_dom, nzp_kvar, water_pumping) " +
                                " SELECT t.nzp_dom, t.nzp_kvar, val_prm " +
                                " FROM " + tableAlias.prmTableTwo + " p, t_kvars t " +
                                " WHERE dat_s <= '" + _datPo + "' " +
                                " AND dat_po >= '" + _datS + "' " +
                                " AND is_actual = 1 " +
                                " AND nzp_prm = 1010137 " +
                                " AND p.nzp = t.nzp_dom ");
                    }


                    if (Params.Contains((int)Fields.IsMkd))
                    {
                        ExecSQL(" INSERT INTO t_params2 (nzp_dom, nzp_kvar, mkd) " +
                                " SELECT t.nzp_dom, t.nzp_kvar, " +
                                " CASE WHEN val_prm = '1' THEN 'МКД' " +
                                " WHEN val_prm = '2' THEN 'Частный сектор' " +
                                " ELSE 'Неопределено' END as mkd " +
                                " FROM " + tableAlias.prmTableTwo + " p, t_kvars t " +
                                " WHERE dat_s <= '" + _datPo + "' " +
                                " AND dat_po >= '" + _datS + "' " +
                                " AND is_actual = 1 " +
                                " AND nzp_prm = 2030 " +
                                " AND p.nzp = t.nzp_dom ");
                    }
                }
            }

            ExecSQL(" UPDATE t_params2 SET nzp_dom = (SELECT MAX(nzp_dom) FROM t_kvars k " +
                    " WHERE t_params2.nzp_kvar = k.nzp_kvar) WHERE nzp_dom IS NULL ");

            //ExecSQL(" UPDATE t_params2 SET status = 'неопределено' WHERE status = '' OR status IS NULL ");
            //ExecSQL(" INSERT INTO t_all_kvars SELECT * FROM t_kvars ");
        }


        /// <summary>
        /// Заполнение атрибутов
        /// </summary>
        /// <param name="tableAlias"></param>
        /// <param name="whereServ"></param>
        /// <param name="datS"></param>
        /// <param name="datPo"></param>
        private void UpdateMainTable(string whereServ)
        {
            foreach (var pref in _prefs)
            {
                for (var i = YearS * 12 + MonthS; i < YearPo * 12 + MonthPo + 1; i++)
                {
                    var mo = GetMonth(i);
                    var ye = GetYear(i);
                    tableAlias = new TableAlias(pref.Pref, ye, mo);
                    var datS = new DateTime(ye, mo, 1).ToShortDateString();
                    var datPo = new DateTime(ye, mo, 1).AddMonths(1).AddDays(-1).ToShortDateString();
                    if (tableAlias.InitAlias(Connection))
                    {
                        if (Params.Contains((int)Fields.InsaldoCorrection))
                        {
                            var sql = " UPDATE t_svod SET real_insaldo = (" +
                                      " SELECT SUM(sum_rcl) " +
                                      " FROM " + tableAlias.perekidka + " a " +
                                      " WHERE a.nzp_kvar = t_svod.nzp_kvar" +
                                      " AND a.type_rcl in (100,20) " +
                                        whereServ + GetWhereSupp("a.nzp_supp") +
                                      " AND a.month_ = " + mo +
                                      " AND a.nzp_supp=t_svod.nzp_supp " +
                                      " AND a.nzp_serv=t_svod.nzp_serv " +
                                      ") WHERE real_insaldo IS NULL " +
                                      " AND pref = '" + pref.Point + "' AND monthyear = '" + _months[mo] + " " + ye + "' ";
                            ExecSQL(sql);
                        }

                        if (Params.Contains((int)Fields.ChargesCorrection))
                        {
                            var sql = " UPDATE t_svod SET real_charge = (" +
                                      " SELECT SUM(sum_rcl) " +
                                      " FROM " + tableAlias.perekidka + " a " +
                                      " WHERE a.nzp_kvar = t_svod.nzp_kvar" +
                                      " AND a.type_rcl not in (100,20) " +
                                        whereServ + GetWhereSupp("a.nzp_supp") +
                                      " AND a.month_ = " + mo +
                                      " AND a.nzp_supp=t_svod.nzp_supp " +
                                      " AND a.nzp_serv=t_svod.nzp_serv " +
                                      ") WHERE real_charge IS NULL " +
                                      " AND pref = '" + pref.Point + "' AND monthyear = '" + _months[mo] + " " + ye + "' ";
                            ExecSQL(sql);
                        }

                        if (Params.Contains((int)Fields.PrevBillingPayment))
                        {
                            ExecSQL(" UPDATE t_svod SET money_from = COALESCE(money_from,0) + ( " +
                                    " SELECT -SUM(sum_prih) " +
                                    " FROM " + tableAlias.fromSupplier + " a " +
                                    " INNER JOIN " + tableAlias.kvarTable + " k ON a.num_ls = k.num_ls " +
                                    " WHERE a.kod_sum in (49, 50, 35) " + GetWhereSupp("a.nzp_supp") +
                                    " AND t_svod.nzp_serv = a.nzp_serv  " +
                                    " AND t_svod.nzp_supp = a.nzp_supp  " + 
                                    " AND t_svod.num_ls = a.num_ls  " +
                                    " AND dat_uchet >= '" + datS + "' " +
                                    " AND dat_uchet <= '" + datPo + "') " +
                                    " WHERE pref = '" + pref.Point + "' AND monthyear = '" + _months[mo] + " " + ye + "' ");
                        }
                        if (Params.Contains((int)Fields.DirectSupplierPayment))
                        {
                            ExecSQL(" UPDATE t_svod SET money_supp = COALESCE(money_supp,0) + ( " +
                                    " SELECT SUM(sum_prih) " +
                                    " FROM " + tableAlias.fromSupplier + " a " +
                                    " INNER JOIN " + tableAlias.kvarTable + " k ON a.num_ls = k.num_ls " +
                                    " WHERE a.kod_sum in (49, 50, 35) " + GetWhereSupp("a.nzp_supp") +
                                    " AND t_svod.nzp_serv = a.nzp_serv  " +
                                    " AND t_svod.nzp_supp = a.nzp_supp  " +
                                    " AND t_svod.num_ls = a.num_ls  " +
                                    " AND dat_uchet >= '" + datS + "' " +
                                    " AND dat_uchet <= '" + datPo + "') " + 
                                    " WHERE pref = '" + pref.Point + "' AND monthyear = '" + _months[mo] + " " + ye + "' ");
                        }
                    }
                }
            }


            #region апдейты из центрального банка
            if (Params.Contains((int)Fields.Territory))
            {
                ExecSQL(" UPDATE  t_svod SET area = a.area " +
                        " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area a " +
                        " WHERE a.nzp_area = t_svod.nzp_area ");
            }
            if (Params.Contains((int)Fields.Geu))
            {
                ExecSQL(" UPDATE  t_svod SET geu = a.geu " +
                        " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "s_geu a " +
                        " WHERE a.nzp_geu = t_svod.nzp_geu ");
            }
            if (Params.Contains((int)Fields.Locality))
            {
                ExecSQL(" UPDATE t_svod SET " +
                        " town = (SELECT town FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "s_town a " +
                        " WHERE a.nzp_town = t_svod.nzp_town), " +
                        " rajon = (SELECT rajon FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon a " +
                        " WHERE a.nzp_raj = t_svod.nzp_raj) ");
            }
            if (Params.Contains((int)Fields.Rajon))
            {
                ExecSQL(" UPDATE  t_svod SET " +
                        " rajon_dom = (SELECT rajon_dom FROM " + ReportParams.Pref + DBManager.sDataAliasRest +
                        "s_rajon_dom a " +
                        " WHERE a.nzp_raj_dom = t_svod.nzp_raj_dom) ");
            }
            if (Params.Contains((int)Fields.Street))
            {
                ExecSQL(" UPDATE  t_svod SET " +
                        " ulica =  TRIM(" + DBManager.sNvlWord + "(a.ulicareg,''))||' '||TRIM(a.ulica) " +
                        " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica a " +
                        " WHERE a.nzp_ul = t_svod.nzp_ul ");
            }
            if (Params.Contains((int)Fields.Building) || SummariesValues.Contains((int)Summaries.ByBuilding))
            {
                ExecSQL(" UPDATE t_svod SET " +
                        " idom = a.idom , " +
                        " ndom = a.ndom, " +
                        " nkor =  CASE WHEN trim(a.nkor) <> '' AND trim(a.nkor) <> '-' THEN ' к.'||a.nkor END " +
                        " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "dom a " +
                        " WHERE a.nzp_dom = t_svod.nzp_dom  ");
            }
            if (Params.Contains((int)Fields.Appartment))
            {
                ExecSQL(" UPDATE  t_svod SET " +
                        " nkvar = a.nkvar, " +
                        " ikvar = a.ikvar " +
                        " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a " +
                        " WHERE a.nzp_kvar = t_svod.nzp_kvar");
            }
            if (Params.Contains((int)Fields.Supplier))
            {
                ExecSQL(" UPDATE  t_svod SET name_supp = a.name_supp " +
                        " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier a " +
                        " WHERE a.nzp_supp = t_svod.nzp_supp ");
            }
            if (Params.Contains((int)Fields.Service))
            {
                ExecSQL(" UPDATE  t_svod SET service = a.service " +
                        " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "services a " +
                        " WHERE a.nzp_serv = t_svod.nzp_serv ");
            }
            if (Params.Contains((int)Fields.EnterDate))
            {
                for (var y = YearS - 1; y <= YearPo; y++)
                {
                    var fin = ReportParams.Pref + "_fin_" + (y - 2000).ToString("00") + DBManager.tableDelimiter +
                                 "pack_ls ";
                    if (TempTableInWebCashe(fin))
                    {
                        ExecSQL(" INSERT INTO t_dat_vvod (num_ls, dat_vvod) " +
                                " SELECT num_ls, MAX(dat_vvod) FROM " + fin +
                                " GROUP BY 1 ");
                    }
                }
                ExecSQL(" REINDEX INDEX dat_vvod_ndx ");
                ExecSQL(DBManager.sUpdStat + " t_dat_vvod ");
                ExecSQL(" UPDATE t_svod SET dat_vvod = (SELECT MAX(dat_vvod) FROM t_dat_vvod v WHERE t_svod.nzp_kvar = v.num_ls) ");
                ExecSQL(" TRUNCATE t_dat_vvod ");
            }
            #endregion
        }


        /// <summary>
        /// Формирует сводную таблицу
        /// </summary>
        /// <param name="databank"></param>
        /// <param name="mo"></param>
        /// <param name="ye"></param>
        /// <param name="joinWord"></param>
        /// <param name="tableAlias"></param>
        /// <param name="whereRaj"></param>
        /// <param name="whereRajDom"></param>
        /// <param name="whereServ"></param>
        /// <param name="whereSupp"></param>
        private void BuildMainTable(string joinWord, string whereRaj, string whereRajDom, string whereServ, string whereSupp)
        {
            foreach (var pref in _prefs)
            {
                for (var i = YearS * 12 + MonthS; i < YearPo * 12 + MonthPo + 1; i++)
                {
                    var mo = GetMonth(i);
                    var ye = GetYear(i);
                    tableAlias = new TableAlias(pref.Pref, ye, mo);
                    if (tableAlias.InitAlias(Connection))
                    {
                        var databank = pref.Point;
                        var groupby = String.Empty;
                        var sql = new StringBuilder();
                        sql.Remove(0, sql.Length);
                        sql.Append(" INSERT INTO t_svod( ");
                        if (Params.Contains((int)Fields.Territory))
                        {
                            sql.Append(" nzp_area, ");
                            groupby += ", k.nzp_area";
                        }
                        if (Params.Contains((int)Fields.Geu))
                        {
                            sql.Append(" nzp_geu, ");
                            groupby += ", k.nzp_geu";
                        }
                        if (Params.Contains((int)Fields.Uchastok))
                        {
                            sql.Append(" uch, ");
                            groupby += ", uch";
                        }
                        if (Params.Contains((int)Fields.CalculationSign))
                        {
                            sql.Append(" is_device, typek, ");
                            groupby += ", is_device, typek";
                        }
                        if (Params.Contains((int)Fields.Rajon))
                        {
                            sql.Append(" nzp_raj_dom, ");
                            groupby += ", rd.nzp_raj_dom";
                        }
                        if (Params.Contains((int)Fields.Locality))
                        {
                            sql.Append(" nzp_town, nzp_raj, ");
                            groupby += ", r.nzp_town, r.nzp_raj";
                        }
                        if (Params.Contains((int)Fields.Street))
                        {
                            sql.Append(" nzp_ul, ");
                            groupby += ", u.nzp_ul";
                        }
                        if (Params.Contains((int)Fields.Building) ||
                            Params.Contains((int)Fields.OneGkalHeatingStandart) ||
                            Params.Contains((int)Fields.OneGkalHotWaterStandart) ||
                            SummariesValues.Contains((int)Summaries.ByBuilding))
                        {
                            sql.Append(" nzp_dom, ");
                            groupby += ", d.nzp_dom";
                        }
                        sql.Append(" nzp_kvar, ");
                        groupby += ", k.nzp_kvar";
                        if (Params.Contains((int)Fields.Account) || SummariesValues.Contains((int)Summaries.ByAccount) ||
                            Params.Contains((int)Fields.PrevBillingPayment) ||
                            Params.Contains((int)Fields.DirectSupplierPayment))
                        {
                            sql.Append(" num_ls, ");
                            groupby += ", k.num_ls";
                        }
                        if (Params.Contains((int)Fields.Fio))
                        {
                            sql.Append(" fio, ");
                            groupby += ", k.fio";
                        }
                        if (Params.Contains((int)Fields.PayCode))
                        {
                            sql.Append(" pkod, ");
                            groupby += ", k.pkod";
                        }
                        if (Params.Contains((int)Fields.Supplier) ||
                            Params.Contains((int)Fields.InsaldoCorrection) || 
                            Params.Contains((int)Fields.ChargesCorrection) ||
                            Params.Contains((int)Fields.PrevBillingPayment) ||
                            Params.Contains((int)Fields.DirectSupplierPayment))

                        {
                            sql.Append(" nzp_supp, ");
                            groupby += ", nzp_supp";
                        }
                        if (Params.Contains((int)Fields.Service) || Params.Contains((int)Fields.CalculationSign) ||
                            Params.Contains((int)Fields.InsaldoCorrection) ||
                            Params.Contains((int)Fields.ChargesCorrection) ||
                            Params.Contains((int)Fields.PrevBillingPayment) ||
                            Params.Contains((int)Fields.DirectSupplierPayment))
                        {
                            sql.Append(" nzp_serv, ");
                            groupby += ", nzp_serv";
                        }
                        if (Params.Contains((int)Fields.EnterDate))
                        {
                            sql.Append(" dat_vvod, ");
                            groupby += ", dat_vvod";
                        }
                        if (Params.Contains((int)Fields.DataBank) || Params.Contains((int)Fields.ChargesCorrection) || 
                            Params.Contains((int)Fields.InsaldoCorrection) || Params.Contains((int)Fields.PrevBillingPayment) || 
                            Params.Contains((int)Fields.DirectSupplierPayment))
                        {
                            sql.Append(" pref, ");
                            groupby += ", pref";
                        }
                        if (Params.Contains((int)Fields.MonthYear) || Params.Contains((int)Fields.ChargesCorrection) || 
                            Params.Contains((int)Fields.InsaldoCorrection) || Params.Contains((int)Fields.PrevBillingPayment) || 
                            Params.Contains((int)Fields.DirectSupplierPayment))
                        {
                            sql.Append(" monthyear, ");
                            groupby += ", monthyear";
                        }
                        if (Params.Contains((int)Fields.Tarif))
                        {
                            sql.Append(" tarif, ");
                            groupby += ", tarif";
                        }
                        if (Params.Contains((int)Fields.Insaldo))
                        {
                            sql.Append(" sum_insaldo, ");
                        }

                        if (Params.Contains((int)Fields.Consumption))
                        {
                            sql.Append(" c_calc, ");
                        }
                        if (Params.Contains((int)Fields.ShortDelivery))
                        {
                            sql.Append(" sum_nedop, ");
                        }
                        if (Params.Contains((int)Fields.ChargedWithShortDelivery))
                        {
                            sql.Append(" sum_tarif, ");
                        }
                        if (Params.Contains((int)Fields.ForPayment))
                        {
                            sql.Append(" sum_charge, ");
                        }
                        if (Params.Contains((int)Fields.Paid))
                        {
                            sql.Append(" money_to, ");
                        }
                        if (Params.Contains((int)Fields.PrevBillingPayment))
                        {
                            sql.Append(" money_from, ");
                        }
                        if (Params.Contains((int)Fields.Charged))
                        {
                            sql.Append(" rsum_tarif, ");
                        }
                        if (Params.Contains((int)Fields.Reval))
                        {
                            sql.Append(" reval, ");
                        }
                        if (Params.Contains((int)Fields.Outsaldo))
                        {
                            sql.Append(" sum_outsaldo, ");
                        }
                        sql.Remove(sql.Length - 2, 2);
                        sql.Append(") ");
                        sql.Append(" SELECT ");
                        if (Params.Contains((int)Fields.Territory))
                        {
                            sql.Append(" k.nzp_area, ");
                        }
                        if (Params.Contains((int)Fields.Geu))
                        {
                            sql.Append(" k.nzp_geu, ");
                        }
                        if (Params.Contains((int)Fields.Uchastok))
                        {
                            sql.Append(" uch, ");
                        }
                        if (Params.Contains((int)Fields.CalculationSign))
                        {
                            sql.Append(" is_device, typek, ");
                        }
                        if (Params.Contains((int)Fields.Rajon))
                        {
                            sql.Append(" rd.nzp_raj_dom, ");
                        }
                        if (Params.Contains((int)Fields.Locality))
                        {
                            sql.Append(" r.nzp_town, r.nzp_raj, ");
                        }
                        if (Params.Contains((int)Fields.Street))
                        {
                            sql.Append(" u.nzp_ul, ");
                        }
                        if (Params.Contains((int)Fields.Building) ||
                            Params.Contains((int)Fields.OneGkalHeatingStandart) ||
                            Params.Contains((int)Fields.OneGkalHotWaterStandart) ||
                            SummariesValues.Contains((int)Summaries.ByBuilding))
                        {
                            sql.Append(" d.nzp_dom, ");
                        }
                        sql.Append(" k.nzp_kvar, ");
                        if (Params.Contains((int)Fields.Account) || SummariesValues.Contains((int)Summaries.ByAccount) ||
                            Params.Contains((int)Fields.PrevBillingPayment) ||
                            Params.Contains((int)Fields.DirectSupplierPayment))
                        {
                            sql.Append(" k.num_ls, ");
                        }
                        if (Params.Contains((int)Fields.Fio))
                        {
                            sql.Append(" k.fio, ");
                        }
                        if (Params.Contains((int)Fields.PayCode))
                        {
                            sql.Append(" pkod, ");
                        }
                        if (Params.Contains((int)Fields.Supplier) ||
                            Params.Contains((int)Fields.InsaldoCorrection) ||
                            Params.Contains((int)Fields.ChargesCorrection) ||
                            Params.Contains((int)Fields.PrevBillingPayment) ||
                            Params.Contains((int)Fields.DirectSupplierPayment))
                        {
                            sql.Append(" c.nzp_supp, ");
                        }
                        if (Params.Contains((int)Fields.Service) || Params.Contains((int)Fields.CalculationSign) ||
                            Params.Contains((int)Fields.InsaldoCorrection) ||
                            Params.Contains((int)Fields.ChargesCorrection) ||
                            Params.Contains((int)Fields.PrevBillingPayment) ||
                            Params.Contains((int)Fields.DirectSupplierPayment))
                        {
                            sql.Append(" c.nzp_serv, ");
                        }
                        if (Params.Contains((int)Fields.EnterDate))
                        {
                            sql.Append(" '' as dat_vvod, ");
                        }
                        if (Params.Contains((int)Fields.DataBank) || Params.Contains((int)Fields.ChargesCorrection) ||
                            Params.Contains((int)Fields.InsaldoCorrection) || Params.Contains((int)Fields.PrevBillingPayment) ||
                            Params.Contains((int)Fields.DirectSupplierPayment))
                        {
                            sql.Append(" '" + databank + "' as pref, ");
                        }
                        if (Params.Contains((int)Fields.MonthYear) || Params.Contains((int)Fields.ChargesCorrection) ||
                            Params.Contains((int)Fields.InsaldoCorrection) || Params.Contains((int)Fields.PrevBillingPayment) ||
                            Params.Contains((int)Fields.DirectSupplierPayment))
                        {
                            sql.Append(" '" + _months[mo] + " " + ye + "' as monthyear, ");
                        }
                        if (Params.Contains((int)Fields.Tarif))
                        {
                            sql.Append(" tarif, ");
                        }
                        if (Params.Contains((int)Fields.Insaldo))
                        {
                            sql.Append(" SUM(" + GetSumInsaldo(mo, ye) + "), ");
                        }

                        if (Params.Contains((int)Fields.Consumption))
                        {
                            sql.Append(" SUM(c_calc), ");
                        }
                        if (Params.Contains((int)Fields.ShortDelivery))
                        {
                            sql.Append(" SUM(sum_nedop), ");
                        }
                        if (Params.Contains((int)Fields.ChargedWithShortDelivery))
                        {
                            sql.Append(" SUM(sum_tarif), ");
                        }
                        if (Params.Contains((int)Fields.ForPayment))
                        {
                            sql.Append(" SUM(sum_charge), ");
                        }
                        if (Params.Contains((int)Fields.Paid))
                        {
                            sql.Append(" SUM(money_to), ");
                        }
                        if (Params.Contains((int)Fields.PrevBillingPayment))
                        {
                            sql.Append(" SUM(money_from), ");
                        }
                        if (Params.Contains((int)Fields.Charged))
                        {
                            sql.Append(" SUM(rsum_tarif), ");
                        }
                        if (Params.Contains((int)Fields.Reval))
                        {
                            sql.Append(" SUM(reval), ");
                        }
                        if (Params.Contains((int)Fields.Outsaldo))
                        {
                            sql.Append(" SUM(" + GetSumOutSaldo(mo, ye) + "), ");
                        }
                        sql.Remove(sql.Length - 2, 2);

                        sql.Append(" FROM t_kvars t " + joinWord + tableAlias.kvarTable +
                                   " k ON t.nzp_kvar = k.nzp_kvar " + joinWord + tableAlias.chargeTable +
                                   " c ON t.nzp_kvar = c.nzp_kvar AND c.nzp_serv > 1 AND c.dat_charge IS NULL ");
                        switch (Kind)
                        {
                            case (int)CalculationSign.Norma:
                                sql.Append(" AND c.is_device = 0 ");
                                break;
                            case (int)CalculationSign.CountersValues:
                                sql.Append(" AND c.is_device = 1 ");
                                break;
                            case (int)CalculationSign.MonthAverage:
                                sql.Append(" AND c.is_device = 9 ");
                                break;
                            case (int)CalculationSign.OdpuValues:
                                sql.Append(" AND c.nzp_serv > 500 ");
                                break;
                            case (int)CalculationSign.NonLivingPlaces:
                                sql.Append(" AND k.typek = 3 ");
                                break;
                        }
                        if (Params.Contains((int)Fields.Locality)) //если выбран населенный пункт
                        {
                            sql.Append(joinWord + tableAlias.domTable + " d ON k.nzp_dom = d.nzp_dom " + whereRajDom +
                                       (Params.Contains((int)Fields.Rajon)
                                           ? " LEFT JOIN " + ReportParams.Pref + DBManager.sDataAliasRest +
                                             "s_rajon_dom rd ON d.nzp_raj = rd.nzp_raj_dom "
                                           : string.Empty) +
                                       joinWord + tableAlias.ulTable + " u ON d.nzp_ul = u.nzp_ul " +
                                       joinWord + tableAlias.rajTable + " r ON u.nzp_raj = r.nzp_raj " + whereRaj);
                        }
                        else if (Params.Contains((int)Fields.Building) ||
                                 Params.Contains((int)Fields.OneGkalHeatingStandart) ||
                                 Params.Contains((int)Fields.OneGkalHotWaterStandart) ||
                                 Params.Contains((int)Fields.Rajon) ||
                                 SummariesValues.Contains((int)Summaries.ByBuilding)) //если выбран дом
                        {
                            sql.Append(joinWord + tableAlias.domTable + " d ON k.nzp_dom = d.nzp_dom " + whereRajDom +
                                       (Params.Contains((int)Fields.Rajon)
                                           ? " LEFT JOIN " + ReportParams.Pref + DBManager.sDataAliasRest +
                                             "s_rajon_dom rd ON d.nzp_raj = rd.nzp_raj_dom "
                                           : string.Empty));
                        }
                        sql.Append(whereServ + whereSupp);
                        if (!String.IsNullOrEmpty(groupby)) sql.Append(" GROUP BY " + groupby.Substring(1));
                        ExecSQL(sql.ToString());
                    }
                }
            }
            ExecSQL(" REINDEX TABLE t_svod ");
            ExecSQL(DBManager.sUpdStat + " t_svod");
        }


        /// <summary>
        /// Получает имя поля в запросе для исходщего сальдо
        /// </summary>
        /// <param name="mo">Текущий месяц</param>
        /// <param name="ye">Текущий год</param>
        /// <returns>sum_outsaldo если год и месяц совпадают, иначе 0</returns>
        private string GetSumOutSaldo(int mo, int ye)
        {
            string sumOutsaldo = ((mo == MonthPo) & (ye == YearPo)) ? "sum_outsaldo" : "0";
            return sumOutsaldo;
        }

        /// <summary>
        /// Получает имя поля в запросе для входящего сальдо
        /// </summary>
        /// <param name="mo">Текущий месяц</param>
        /// <param name="ye">Текущий год</param>
        /// <returns>sum_insaldo если год и месяц совпадают, иначе 0</returns>
        private string GetSumInsaldo(int mo, int ye)
        {
            string sumInsaldo = ((mo == MonthS) & (ye == YearS)) ? "sum_insaldo" : "0";
            return sumInsaldo;
        }

        private static int GetMonth(int i)
        {
            int mo = i % 12;
            if (mo == 0) mo = 12;
            return mo;
        }

        private static int GetYear(int i)
        {
            return i % 12 == 0 ? (i / 12) - 1 : (i / 12);
        }

        /// <summary>
        /// Построение базовой таблицы
        /// </summary>
        /// <param name="listLc"></param>
        /// <param name="joinWord"></param>
        /// <param name="whereServFilter"></param>
        /// <param name="whereSupp"></param>
        /// <param name="prefs"></param>
        private void BuildBaseKvar(bool listLc, string joinWord, string whereServFilter, string whereSupp)
        {
            foreach (var pref in _prefs)
            {
                tableAlias = new TableAlias(pref.Pref, YearS, MonthS);
                if (tableAlias.InitAlias(Connection))
                {
                    ExecSQL(" CREATE TEMP TABLE t_kvars_local (nzp_kvar INTEGER, nzp_dom INTEGER) ");
                    #region Добавляем базовый набор ЛС открыт/закрыт
                    if (StatusLs != null)
                    {
                        var stat = " AND val_prm in ( ";
                        stat += StatusLs.Contains((int)AccountStatus.Open) ? "'1', " : string.Empty;
                        stat += StatusLs.Contains((int)AccountStatus.Closed) ? "'2', " : string.Empty;
                        stat += StatusLs.Contains((int)AccountStatus.Undefined) ? "'3', " : string.Empty;
                        stat = stat.TrimEnd(',', ' ');
                        stat += ")";
                        {
                            ExecSQL(" INSERT INTO t_kvars_local ( nzp_kvar, nzp_dom ) " +
                                    " SELECT DISTINCT nzp_kvar, nzp_dom " +
                                    " FROM " + tableAlias.prmTableThree + " p, " +
                                    (listLc ? " selected_kvars " : tableAlias.kvarTable) + " k" +
                                    " WHERE p.nzp=k.nzp_kvar " +
                                    " AND dat_s <= '" + _datPo + "' " +
                                    " AND dat_po >= '" + _datS + "' " +
                                    " AND is_actual = 1 " +
                                    " AND nzp_prm = 51 " + stat);
                        }
                    }
                    else
                    {
                        ExecSQL(" INSERT INTO t_kvars_local (nzp_kvar, nzp_dom ) " +
                                " SELECT DISTINCT k.nzp_kvar, k.nzp_dom " +
                                " FROM " + (listLc ? " selected_kvars " : tableAlias.kvarTable) + " k " +
                                joinWord + tableAlias.domTable + " d ON k.nzp_dom = d.nzp_dom " +
                                joinWord + tableAlias.ulTable + " u ON d.nzp_ul = u.nzp_ul ");
                    }
                    #endregion

                    #region Лицевые счета с прибором и без
                    if (Ipu != (int)IpuEnum.Undefined)
                    {
                        ExecSQL(" DELETE FROM t_kvars_local a " +
                                " WHERE " + (Ipu == (int)IpuEnum.No ? "" : " NOT ") + " EXISTS ( " +
                                " SELECT 1 " +
                                " FROM " + tableAlias.countersSpisTable + " cs " +
                                " WHERE nzp_type = 3 " + whereServFilter +
                                "     AND '" + _datS + "' <= COALESCE(cs.dat_po, 'infinity') " +
                                "     AND '" + _datPo + "' >= COALESCE(cs.dat_s, '-infinity') " +
                                "     AND is_actual = 1" +
                                "     AND a.nzp_kvar = cs.nzp) ");
                    }
                    #endregion

                    #region Дома с прибором и без
                    if (Odpu != (int)OdpuEnum.Undefined)
                    {
                        ExecSQL(" INSERT INTO t_doms(nzp_dom) " +
                                " SELECT DISTINCT nzp_dom " +
                                " FROM t_kvars_local t ");

                        ExecSQL(" SELECT DISTINCT d.nzp_dom " +
                                " FROM " + tableAlias.countersSpisTable + " cs, t_doms d " +
                                " WHERE cs.nzp = d.nzp_dom " +
                                "     AND '" + _datS + "' <= " + DBManager.sNvlWord + "(cs.dat_po, 'infinity') " +
                                "     AND '" + _datPo + "' >= " + DBManager.sNvlWord + "(cs.dat_s, '-infinity') " +
                                whereServFilter +
                                "     AND nzp_type = 1 " +
                                "     AND is_actual = 1 ");

                        ExecSQL(" DELETE FROM t_kvars_local k " +
                                " WHERE " + (Odpu == (int)OdpuEnum.No ? " " : " NOT") + " EXISTS (" +
                                " SELECT 1 " +
                                " FROM t_doms t  " +
                                " WHERE k.nzp_dom=t.nzp_dom )");

                        ExecSQL(" TRUNCATE t_doms ");
                    }
                    #endregion

                    #region Приватизация
                    if (Privat != (int)Privatization.Undefined)
                    {
                        ExecSQL(" DELETE FROM t_kvars_local  " +
                                " WHERE " + (Privat == 1 ? "" : " NOT ") + " EXISTS ( " +
                                " SELECT 1 FROM  " + tableAlias.prmTableOne +
                                " WHERE t_kvars_local.nzp_kvar = nzp " +
                                "     AND dat_s <= '" + _datPo + "' " +
                                "     AND dat_po >= '" + _datS + "' " +
                                "     AND is_actual = 1 " +
                                "     AND nzp_prm = 8 " +
                                "     AND val_prm = '1' )");
                    }
                    #endregion

                    #region Статус жилья
                    if (StatusGil != null)
                    {
                        ExecSQL(" DELETE FROM t_kvars_local  " +
                                " WHERE nzp_kvar NOT IN ( " +
                                " SELECT 1 FROM  " + tableAlias.prmTableOne +
                                " WHERE t_kvars_local.nzp_kvar = nzp " +
                                " AND dat_s <= '" + _datPo + "' " +
                                " AND dat_po >= '" + _datS + "' " +
                                " AND is_actual = 1 " +
                                " AND nzp_prm = 2009 " +
                                " AND val_prm IN (" + string.Join(",", StatusGil.Select(s => "'" + s + "'").ToArray()) + "))");
                    }
                    #endregion

                    #region Убираем квартиры, которые не в фильтре по услуге/поставщику
                    if ((!String.IsNullOrEmpty(whereServFilter) || !String.IsNullOrEmpty(whereSupp)) &&
                        (IsEmpty != (int)IsEmptyAccounts.Full))
                    {
                        ExecSQL("DROP table tchargekvar");
                        ExecSQL("CREATE TEMP TABLE tchargekvar(nzp_kvar integer, nzp_dom integer)");
                        for (int i = YearS * 12 + MonthS; i < YearPo * 12 + MonthPo + 1; i++)
                        {
                            tableAlias = new TableAlias(pref.Pref, GetYear(i), GetMonth(i));
                            if (tableAlias.InitAlias(Connection))
                            {
                                ExecSQL(" INSERT INTO tchargekvar(nzp_kvar, nzp_dom) " +
                                        " SELECT DISTINCT k.nzp_kvar, k.nzp_dom " +
                                        " FROM  " + tableAlias.chargeTable + " c, t_kvars_local k " +
                                        " WHERE c.nzp_kvar=k.nzp_kvar AND dat_charge IS NULL " + whereServFilter + whereSupp +
                                        (IsEmpty == (int)IsEmptyAccounts.Old
                                            ? " AND ABS(COALESCE(sum_insaldo,0))+ABS(COALESCE(sum_tarif,0))+ABS(COALESCE(reval,0))+" +
                                              " ABS(COALESCE(real_charge,0))+ABS(COALESCE(sum_money,0))+ABS(COALESCE(sum_outsaldo,0))>0.001 "
                                            : (IsEmpty == (int)IsEmptyAccounts.Current
                                                ? " AND ABS(rsum_tarif)>0.001"
                                                : "")));
                            }
                        }
                        ExecSQL("TRUNCATE t_kvars_local");
                        ExecSQL("INSERT INTO t_kvars_local(nzp_kvar, nzp_dom) SELECT DISTINCT nzp_kvar, nzp_dom FROM tchargekvar");
                    }
                    #endregion

                    //должен быть в конце всех фильтров!
                    #region Вытаскивает все лицевые счета по домам (Независимо от ограничений)
                    if (AllLs == (int)AllAccounts.Yes)
                    {
                        ExecSQL(" INSERT INTO t_doms(nzp_dom) " +
                                " SELECT DISTINCT nzp_dom " +
                                " FROM  t_kvars_local ");

                        ExecSQL(" TRUNCATE t_kvars_local ");

                        ExecSQL(" INSERT INTO t_kvars_local (nzp_kvar, nzp_dom) " +
                                " SELECT k.nzp_kvar, k.nzp_dom " +
                                " FROM " + tableAlias.kvarTable + " k, t_doms d " +
                                " WHERE k.nzp_dom = d.nzp_dom ");

                        ExecSQL(" TRUNCATE t_doms ");
                    }
                    #endregion
                    ExecSQL(" INSERT INTO t_kvars SELECT DISTINCT * FROM t_kvars_local ");
                    ExecSQL(" DROP TABLE t_kvars_local ");
                }
            }
            //ExecSQL(" SELECT DISTINCT * INTO TEMP t_kvars_distinct FROM t_kvars ");
            //ExecSQL(" TRUNCATE t_kvars ");
            //ExecSQL(" INSERT INTO t_kvars SELECT * FROM t_kvars_distinct ");
            //ExecSQL(" DROP TABLE t_kvars_distinct ");
            ExecSQL(" REINDEX INDEX t_kvars_kvar_ndx ");
            ExecSQL(" REINDEX INDEX t_kvars_dom_ndx ");
            ExecSQL(DBManager.sUpdStat + " t_kvars");
        }

        private void SetSummaries(string group, string agreg)
        {
            if (SummariesValues.Contains((int)Summaries.ByAccount))
            {
                var groupWithoutAccount = group.Replace("num_ls,", "").Replace("num_ls", "").TrimEnd(',', ' ');
                var maxInGroup = string.Join(",", groupWithoutAccount.Split(',').Select(g => "MAX(" + g + ")"));
                ExecSQL(" INSERT INTO t_svod_all (num_ls, " + (groupWithoutAccount != string.Empty ? groupWithoutAccount + "," : string.Empty) +
                        " summary_postfix, " + agreg.Replace("SUM(", "").Replace("MAX(", "").Replace(")", "") + ") " +
                        " SELECT num_ls, " + (groupWithoutAccount != string.Empty ? maxInGroup + "," : string.Empty) +
                        " 'Итого по ЛС' AS summary_postfix, " + agreg +
                        " FROM t_svod_all t GROUP BY num_ls");
            }

            if (SummariesValues.Contains((int)Summaries.ByBuilding))
            {
                var groupWithoutAccount = group.Replace("nzp_dom,", "").Replace("nzp_dom", "").TrimEnd(',', ' ');
                var maxInGroup = string.Join(",", groupWithoutAccount.Split(',').Select(g => "MAX(" + g + ")"));
                ExecSQL(" INSERT INTO t_svod_all (nzp_dom, " + (groupWithoutAccount != string.Empty ? groupWithoutAccount + "," : string.Empty) +
                        " summary_postfix, " + agreg.Replace("SUM(", "").Replace("MAX(", "").Replace(")", "") + ") " +
                        " SELECT nzp_dom, " + (groupWithoutAccount != string.Empty ? maxInGroup + "," : string.Empty) +
                        " 'Итого по дому' AS summary_postfix, " + agreg +
                        " FROM t_svod_all t GROUP BY nzp_dom");
            }

            ExecSQL(" INSERT INTO t_svod_all_ordered (" + (group == string.Empty ? string.Empty : group + ",") +
                   (" summary_postfix, " + agreg.Replace("SUM(", "").Replace("MAX(", "").Replace(")", "")).TrimEnd(' ', ',') + ")" +
                    " SELECT  " + (group == string.Empty ? string.Empty : group + ",") +
                   (" summary_postfix, " + agreg.Replace("SUM(", "").Replace("MAX(", "").Replace(")", "")).TrimEnd(' ', ',') +
                    " FROM t_svod_all t " +
                   (!string.IsNullOrEmpty(group) ? " ORDER BY " + group + " asc, summary_postfix desc " : ""));

            //group = !string.IsNullOrEmpty(group) ? group.Replace("num_ls,", "").Replace("num_ls", "").Replace("nzp_dom,", "").Replace("nzp_dom", "").TrimEnd(',', ' ').Trim() : "";
            if (!string.IsNullOrEmpty(group))
                group.Split(',').ForEach(g => ExecSQL(" UPDATE t_svod_all_ordered t SET " + g + " = NULL WHERE summary_postfix IS NOT NULL "));
            if (agreg.Contains("MAX(rasch)"))
                ExecSQL(" UPDATE t_svod_all_ordered t SET rasch = NULL WHERE summary_postfix IS NOT NULL ");
            ExecSQL(" SELECT * INTO TEMP t_all_all FROM t_svod_all_ordered ORDER BY seq ");
            //ExecSQL(" UPDATE t_svod_all_ordered t SET num_ls = NULL WHERE summary_postfix IS NOT NULL  ");

        }

        private DataTable FillMatrix()
        {
            ExecSQL(" SELECT setval('rownumber',0) ");
            ExecSQL(" DELETE FROM t_matrix ");
            if (Params.Contains((int)Fields.DataBank))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Банк данных', pref AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.MonthYear))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Месяц', monthyear AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.Territory))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Территория (УК)', area AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.Geu))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'ЖЭУ', geu AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.Uchastok))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Участок', uch AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.AccountStatus))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Статус ЛС', status AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.IsIpu))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Наличие ИПУ', ipu AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT 'Всего', " +
                        "'Наличие ИПУ', SUM(ipu) AS value FROM t_all WHERE summary_postfix IS NULL ");
            }
            if (Params.Contains((int)Fields.IsOdpu))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Наличие ОДПУ', odpu AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT 'Всего', " +
                        "'Наличие ОДПУ', MAX(" + _totalOdpuCounters + ") AS value FROM t_all ");
            }
            if (Params.Contains((int)Fields.CalculationSign))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Признак расчета', rasch AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.OneGkalHeatingStandart))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Домовой норматив на 1 ГКал/кв.м отопления', heating AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.OneGkalHotWaterStandart))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Домовой норматив на 1 ГКал/куб.м горячей воды', hotwater AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.Rajon))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Район', rajon_dom AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.Locality))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Населенный пункт', " + DBManager.sNvlWord + "(town,'')||' '||" + DBManager.sNvlWord + "(rajon,'') AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.Street))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Улица', ulica AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.Building) || (SummariesValues.Contains((int)Summaries.ByBuilding)))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Дом', " + DBManager.sNvlWord + "(ndom,'')||' '||" + DBManager.sNvlWord + "(nkor,'') ||' '||" +
                        "CASE WHEN " + DBManager.sNvlWord + "(summary_postfix,'') = 'Итого по дому' THEN " + DBManager.sNvlWord + "(summary_postfix,'') ELSE '' END AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.Appartment))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Квартира', nkvar AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.Account) || (SummariesValues.Contains((int)Summaries.ByAccount)))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Лицевой счет', " + DBManager.sNvlWord + "(num_ls::varchar,'')||' '||" +
                        "CASE WHEN " + DBManager.sNvlWord + "(summary_postfix,'') = 'Итого по ЛС' THEN " + DBManager.sNvlWord + "(summary_postfix,'') ELSE '' END AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.Fio))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'ФИО', fio AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.PrescriptedCount))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Количество прописаных', propis_count AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT 'Всего', " +
                        "'Количество прописаных', SUM(propis_count) AS value FROM t_all ");
            }
            if (Params.Contains((int)Fields.TemporaryLiving))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Количество временно проживающих', gil_prib AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT 'Всего', " +
                        "'Количество временно проживающих', SUM(gil_prib) AS value FROM t_all ");
            }
            if (Params.Contains((int)Fields.TemporaryOut))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Количество временно выбывших', gil_ub AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT 'Всего', " +
                        "'Количество временно выбывших', SUM(gil_ub) AS value FROM t_all ");
            }
            if (Params.Contains((int)Fields.TotalSquare))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Общая площадь', ob_s AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.HeatingSquare))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Отапливаемая площадь', otop_s AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.LivingSquare))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Жилая площадь', gil_s AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.Rooms))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Количество комнат', rooms AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.Floor))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Этаж', floor AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.PayCode))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Платежный код', pkod AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.Supplier))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Поставщик', name_supp AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.Service))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Услуга', service AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.Insaldo))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Входящее сальдо', sum_insaldo AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT 'Всего', " +
                        "'Входящее сальдо', SUM(sum_insaldo) AS value FROM t_all ");
            }
            if (Params.Contains((int)Fields.Tarif))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Тариф', tarif AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.Consumption))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Расход', c_calc AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT 'Всего', " +
                        "'Расход', SUM(c_calc) AS value FROM t_all ");
            }
            if (Params.Contains((int)Fields.ShortDelivery))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Недопоставка', sum_nedop AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT 'Всего', " +
                        "'Недопоставка', SUM(sum_nedop) AS value FROM t_all ");
            }
            if (Params.Contains((int)Fields.ChargedWithShortDelivery))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Начислено с учетом недопоставки', sum_tarif AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT 'Всего', " +
                        "'Начислено с учетом недопоставки', SUM(sum_tarif) AS value FROM t_all ");
            }
            if (Params.Contains((int)Fields.ForPayment))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'К оплате', sum_charge AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT 'Всего', " +
                        "'К оплате', SUM(sum_charge) AS value FROM t_all ");
            }
            if (Params.Contains((int)Fields.Paid))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Оплачено', money_to AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT 'Всего', " +
                        "'Оплачено', SUM(money_to) AS value FROM t_all ");
            }
            if (Params.Contains((int)Fields.PrevBillingPayment))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'В т.ч. оплаты предыдущих биллинговых систем', (money_from) AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT 'Всего', " +
                        "'В т.ч. оплаты предыдущих биллинговых систем', SUM(money_from) AS value FROM t_all ");
            }
            if (Params.Contains((int)Fields.DirectSupplierPayment))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Оплата напрямую поставщикам', money_supp AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT 'Всего', " +
                        "'Оплата напрямую поставщикам', SUM(money_supp) AS value FROM t_all ");
            }
            if (Params.Contains((int)Fields.ChargesCorrection))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Корректировка начислений', real_charge AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT 'Всего', " +
                        "'Корректировка начислений', SUM(real_charge) AS value FROM t_all ");
            }
            if (Params.Contains((int)Fields.InsaldoCorrection))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Корректировка входящего сальдо', real_insaldo AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT 'Всего', " +
                        "'Корректировка входящего сальдо', SUM(real_insaldo) AS value FROM t_all ");
            }
            if (Params.Contains((int)Fields.Charged))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Начислено', rsum_tarif AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT 'Всего', " +
                        "'Начислено', SUM(rsum_tarif) AS value FROM t_all ");
            }
            if (Params.Contains((int)Fields.Reval))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Перерасчет', reval AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT 'Всего', " +
                        "'Перерасчет', SUM(reval) AS value FROM t_all ");
            }
            if (Params.Contains((int)Fields.Outsaldo))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Исходящее сальдо', sum_outsaldo AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT 'Всего', " +
                        "'Исходящее сальдо', SUM(sum_outsaldo) AS value FROM t_all ");
            }
            if (Params.Contains((int)Fields.Privat))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Приватизировано', privat AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.EnterDate))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Дата последней оплаты ЛС', dat_vvod AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.StatusGil))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Статус жилья', status_gil AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.BoilerHotWater))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Котельная по горячей воде', boiler_hotwater AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.BoilerHeating))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Котельная по отоплению', boiler_heating AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.WaterPumping))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Водозабор', water_pumping AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.IsMkd))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'МКД', mkd AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }
            if (Params.Contains((int)Fields.Normative))
            {
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Норматив по услуге', norm_name AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
                ExecSQL(" INSERT INTO t_matrix(row,head,value) SELECT nextval('rownumber'), " +
                        "'Объем по услуге', norm_value AS value FROM t_all ");
                ExecSQL(" SELECT setval('rownumber',0) ");
            }

            return ExecSQLToTable(" SELECT * FROM t_matrix ");
        }

        /// <summary>
        /// Получает ограничение по районам
        /// </summary>
        /// <param name="tablePrefix"></param>
        /// <returns></returns>
        private string GetWhereLocalitys(string tablePrefix)
        {
            string whereRaj = String.Empty;
            if (Localitys != null)
            {
                whereRaj = Localitys.Aggregate(whereRaj, (current, nzpRaj) => current + (nzpRaj + ","));
                whereRaj = whereRaj.TrimEnd(',');
            }
            if (String.IsNullOrEmpty(whereRaj))
            {
                var towns = ReportParams.GetRolesCondition(Constants.role_sql_town);
                if (!String.IsNullOrEmpty(towns))
                {
                    towns = " WHERE nzp_town in (" + towns + ") ";
                    var roleRaj = ExecSQLToTable(" SELECT distinct nzp_raj FROM " + ReportParams.Pref + DBManager.sDataAliasRest + " s_rajon " + towns);
                    var roleView = new DataView(roleRaj);
                    whereRaj = roleView.Cast<DataRowView>().Aggregate(whereRaj, (current, c) => current + (c[0] + ","));
                    whereRaj = whereRaj.TrimEnd(',');
                }
            }
            if (!String.IsNullOrEmpty(whereRaj))
            {
                whereRaj = " AND " + tablePrefix + "nzp_raj in (" + whereRaj + ") ";
            }
            return whereRaj;
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
                whereSupp += " AND nzp_payer_supp in (" + supp.TrimEnd(',') + ")";
            }

            if (BankSupplier != null && BankSupplier.Principals != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Principals.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " AND nzp_payer_princip in (" + supp.TrimEnd(',') + ")";
            }

            if (BankSupplier != null && BankSupplier.Agents != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Agents.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " AND nzp_payer_agent in (" + supp.TrimEnd(',') + ")";
            }

            string oldsupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);

            whereSupp = whereSupp.TrimEnd(',');


            if (!String.IsNullOrEmpty(whereSupp) || !String.IsNullOrEmpty(oldsupp))
            {
                if (!String.IsNullOrEmpty(oldsupp))
                    whereSupp += " AND nzp_supp in (" + oldsupp + ")";

                //Поставщики
                SupplierHeader = String.Empty;
                DataTable supp = ExecSQLToTable(" SELECT name_supp FROM " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                             " WHERE nzp_supp > 0 " + whereSupp);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += "(" + dr["name_supp"].ToString().Trim() + "), ";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',', ' ');
            }
            
            return " AND " + fieldPref + " in (SELECT nzp_supp FROM " +
                    ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                    " WHERE nzp_supp>0 " + whereSupp + ")";
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
                DataTable serv = ExecSQLToTable(" SELECT service FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "services WHERE nzp_serv > 0 " + whereServ);
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
                ? " AND nzp_serv " + (FilteredServices.Status == 0 ? "in" : "not in") + " (" + whereServFilter + ")"
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
                DataTable terrTable = ExecSQLToTable(" SELECT point FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWp);
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
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                ExecSQL(" CREATE TEMP TABLE selected_kvars(" +
                        " nzp_kvar integer," +
                        " num_ls integer," +
                        " pkod Decimal(13,0)," +
                        " fio char(100), " +
                        " uch INTEGER, " +
                        " nzp_dom integer," +
                        " nzp_geu integer," +
                        " nzp_area integer," +
                        " typek integer) " +
                        DBManager.sUnlogTempTable);
            }

            ExecSQL(" CREATE TEMP TABLE t_kvars( nzp_kvar INTEGER , nzp_dom INTEGER ) " + DBManager.sUnlogTempTable);
            ExecSQL(" DROP INDEX  t_kvars_kvar_ndx", false);
            ExecSQL(" CREATE INDEX t_kvars_kvar_ndx ON t_kvars(nzp_kvar)", false);
            ExecSQL(" DROP INDEX  t_kvars_dom_ndx", false);
            ExecSQL(" CREATE INDEX t_kvars_dom_ndx ON t_kvars(nzp_dom)", false);

            ExecSQL(" CREATE TEMP TABLE t_all_kvars( nzp_kvar INTEGER , nzp_dom INTEGER ) " + DBManager.sUnlogTempTable);

            ExecSQL(" CREATE TEMP TABLE t_doms( nzp_dom INTEGER ) " + DBManager.sUnlogTempTable);
            ExecSQL(" DROP INDEX  t_doms_ndx", false);
            ExecSQL(" CREATE INDEX t_doms_ndx ON t_doms(nzp_dom)", false);


            ExecSQL(" CREATE TEMP TABLE t_ais_pasp( " +
                    " nzp_kvar integer, " +
                    " propis_count integer, " + //Количество проживающих без учета временно выбывших
                    " gil_prib integer, " + //Количество временно прибывших
                    " gil_ub integer) ");//Количество временно выбывших

            ExecSQL(" CREATE TEMP TABLE t_dat_vvod (num_ls integer, dat_vvod date) ");
            ExecSQL(" CREATE INDEX dat_vvod_ndx ON t_dat_vvod(num_ls) ");

            ExecSQL(" CREATE TEMP TABLE t_params (nzp_kvar integer, " +
                    " propis_count integer, gil_prib integer, gil_ub integer, " +
                    " ob_s " + DBManager.sDecimalType + "(14,2), " +
                    " otop_s " + DBManager.sDecimalType + "(14,2), " +
                    " gil_s " + DBManager.sDecimalType + "(14,2), " +
                    " rooms " + DBManager.sDecimalType + "(14,2), " +
                    " floor " + DBManager.sDecimalType + "(14,2), " +
                    " privat character(10), " +
                    " status_gil character(100)) " + DBManager.sUnlogTempTable);
            if (Params.Contains((int)Fields.StatusGil))
            {
                ExecSQL(" DROP INDEX  t_params_status_gil_ndx", false);
                ExecSQL(" CREATE INDEX t_params_status_gil_ndx ON t_params(status_gil) ");
            }

            ExecSQL(" CREATE TEMP TABLE t_params2 ( " +
                    " nzp_dom integer, nzp_kvar integer, nzp_serv integer, " +
                    " status VARCHAR, " +
                    " ipu INTEGER, " +
                    " odpu INTEGER, " +
                    " rasch VARCHAR, " +
                    " heating VARCHAR, " +
                    " hotwater VARCHAR, " +
                    " boiler_hotwater VARCHAR, " +
                    " boiler_heating VARCHAR, " +
                    " water_pumping VARCHAR, " +
                    " mkd VARCHAR, " +
                    " norm_name VARCHAR, " +
                    " norm_value " + DBManager.sDecimalType + "(14,7))" + DBManager.sUnlogTempTable);

            ExecSQL(" DROP INDEX  t_params2_nzp_kvar_ndx", false);
            ExecSQL(" CREATE INDEX t_params2_nzp_kvar_ndx ON t_params2(nzp_kvar) ");

            ExecSQL(" CREATE TEMP TABLE t_params_grouped (nzp_kvar integer, " +
                    " propis_count integer, gil_prib integer, gil_ub integer, " +
                    " ob_s " + DBManager.sDecimalType + "(14,2), " +
                    " otop_s " + DBManager.sDecimalType + "(14,2), " +
                    " gil_s " + DBManager.sDecimalType + "(14,2), " +
                    " rooms " + DBManager.sDecimalType + "(14,2), " +
                    " floor " + DBManager.sDecimalType + "(14,2), " +
                    " privat VARCHAR, " +
                    " status_gil VARCHAR) " + DBManager.sUnlogTempTable);

            ExecSQL(" CREATE TEMP TABLE t_params_grouped2 ( " +
                    " nzp_dom integer, nzp_kvar integer, nzp_serv integer, " +
                    " status VARCHAR, " +
                    " ipu INTEGER, " +
                    " odpu INTEGER, " +
                    " rasch VARCHAR, " +
                    " heating VARCHAR, " +
                    " hotwater VARCHAR, " +
                    " boiler_hotwater VARCHAR, " +
                    " boiler_heating VARCHAR, " +
                    " water_pumping VARCHAR, " +
                    " mkd VARCHAR, " +
                    " norm_name VARCHAR, " +
                    " norm_value " + DBManager.sDecimalType + "(14,7))" + DBManager.sUnlogTempTable);

            ExecSQL(" CREATE TEMP TABLE t_svod (" +
                    " pref VARCHAR, " +
                    " monthyear VARCHAR, " +
                    " nzp_area integer, area VARCHAR, " +
                    " nzp_geu integer, geu VARCHAR, " +
                    " uch integer, " +
                    " is_device integer, typek integer, " +
                    " nzp_raj_dom integer, rajon_dom VARCHAR, " +
                    " nzp_town integer, nzp_raj integer, town VARCHAR, rajon VARCHAR, " +
                    " nzp_ul integer, ulica VARCHAR, " +
                    " nzp_dom integer, idom integer, ndom VARCHAR, nkor VARCHAR, " +
                    " nzp_kvar integer, ikvar integer, nkvar VARCHAR, " +
                    " num_ls integer, " +
                    " fio VARCHAR, " +
                    " pkod " + DBManager.sDecimalType + "(13,0), " +
                    " nzp_supp integer, name_supp VARCHAR, " +
                    " nzp_serv integer, service VARCHAR, " +
                    " dat_vvod VARCHAR, " +
                    " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                    " tarif " + DBManager.sDecimalType + "(14,2), " +
                    " c_calc " + DBManager.sDecimalType + "(14,2), " +
                    " sum_nedop " + DBManager.sDecimalType + "(14,2), " +
                    " sum_tarif " + DBManager.sDecimalType + "(14,2), " +
                    " sum_charge " + DBManager.sDecimalType + "(14,2), " +
                    " money_to " + DBManager.sDecimalType + "(14,2), " +
                    " money_FROM " + DBManager.sDecimalType + "(14,2), " +
                    " money_supp " + DBManager.sDecimalType + "(14,2), " +
                    " real_charge " + DBManager.sDecimalType + "(14,2), " +
                    " real_insaldo " + DBManager.sDecimalType + "(14,2), " +
                    " rsum_tarif " + DBManager.sDecimalType + "(14,2), " +
                    " reval " + DBManager.sDecimalType + "(14,2), " +
                    " sum_outsaldo " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable);

            ExecSQL(" CREATE TEMP TABLE t_svod_grouped (" +
                    " pref VARCHAR, " +
                    " monthyear VARCHAR, " +
                    " area VARCHAR, " +
                    " geu VARCHAR, " +
                    " uch integer, " +
                    " rajon_dom VARCHAR, " +
                    " town VARCHAR, rajon VARCHAR, " +
                    " ulica VARCHAR, " +
                    " nzp_dom integer, idom integer, ndom VARCHAR, nkor VARCHAR, " +
                    " nzp_kvar integer, ikvar integer, nkvar VARCHAR, " +
                    " num_ls integer, " +
                    " fio VARCHAR, " +
                    " pkod " + DBManager.sDecimalType + "(13,0), " +
                    " name_supp VARCHAR, " +
                    " nzp_serv integer, service VARCHAR, " +
                    " dat_vvod VARCHAR, " +
                    " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                    " tarif " + DBManager.sDecimalType + "(14,2), " +
                    " c_calc " + DBManager.sDecimalType + "(14,2), " +
                    " sum_nedop " + DBManager.sDecimalType + "(14,2), " +
                    " sum_tarif " + DBManager.sDecimalType + "(14,2), " +
                    " sum_charge " + DBManager.sDecimalType + "(14,2), " +
                    " money_to " + DBManager.sDecimalType + "(14,2), " +
                    " money_FROM " + DBManager.sDecimalType + "(14,2), " +
                    " money_supp " + DBManager.sDecimalType + "(14,2), " +
                    " real_charge " + DBManager.sDecimalType + "(14,2), " +
                    " real_insaldo " + DBManager.sDecimalType + "(14,2), " +
                    " rsum_tarif " + DBManager.sDecimalType + "(14,2), " +
                    " reval " + DBManager.sDecimalType + "(14,2), " +
                    " sum_outsaldo " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable);

            ExecSQL(" CREATE TEMP TABLE t_svod_all (" +
                    " seq integer, " +
                    " pref VARCHAR, " +
                    " monthyear VARCHAR, " +
                    " area VARCHAR, " +
                    " geu VARCHAR, " +
                    " uch integer, " +
                    " rajon_dom VARCHAR, " +
                    " town VARCHAR, rajon VARCHAR, " +
                    " ulica VARCHAR, " +
                    " nzp_dom integer, idom integer, ndom VARCHAR, nkor VARCHAR, " +
                    " ikvar integer, nkvar VARCHAR, " +
                    " num_ls integer, " +
                    " summary_postfix VARCHAR, " +
                    " fio VARCHAR, " +
                    " pkod " + DBManager.sDecimalType + "(13,0), " +
                    " name_supp VARCHAR, " +
                    " nzp_serv integer, service VARCHAR, " +
                    " dat_vvod VARCHAR, " +
                    " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                    " tarif " + DBManager.sDecimalType + "(14,2), " +
                    " c_calc " + DBManager.sDecimalType + "(14,2), " +
                    " sum_nedop " + DBManager.sDecimalType + "(14,2), " +
                    " sum_tarif " + DBManager.sDecimalType + "(14,2), " +
                    " sum_charge " + DBManager.sDecimalType + "(14,2), " +
                    " money_to " + DBManager.sDecimalType + "(14,2), " +
                    " money_FROM " + DBManager.sDecimalType + "(14,2), " +
                    " money_supp " + DBManager.sDecimalType + "(14,2), " +
                    " real_charge " + DBManager.sDecimalType + "(14,2), " +
                    " real_insaldo " + DBManager.sDecimalType + "(14,2), " +
                    " rsum_tarif " + DBManager.sDecimalType + "(14,2), " +
                    " reval " + DBManager.sDecimalType + "(14,2), " +
                    " sum_outsaldo " + DBManager.sDecimalType + "(14,2), " +
                    " propis_count integer, gil_prib integer, gil_ub integer, " +
                    " ob_s " + DBManager.sDecimalType + "(14,2), " +
                    " otop_s " + DBManager.sDecimalType + "(14,2), " +
                    " gil_s " + DBManager.sDecimalType + "(14,2), " +
                    " rooms " + DBManager.sDecimalType + "(14,2), " +
                    " floor " + DBManager.sDecimalType + "(14,2), " +
                    " privat VARCHAR, " +
                    " status_gil VARCHAR, " +
                    " status VARCHAR, " +
                    " ipu INTEGER, " +
                    " odpu INTEGER, " +
                    " rasch VARCHAR, " +
                    " heating VARCHAR, " +
                    " hotwater VARCHAR, " +
                    " boiler_hotwater VARCHAR, " +
                    " boiler_heating VARCHAR, " +
                    " water_pumping VARCHAR, " +
                    " mkd VARCHAR, " +
                    " norm_name VARCHAR, " +
                    " norm_value " + DBManager.sDecimalType + "(14,7))" + DBManager.sUnlogTempTable);

            ExecSQL(" CREATE TEMP TABLE t_svod_all_ordered (" +
                    " seq serial, " +
                    " pref VARCHAR, " +
                    " monthyear VARCHAR, " +
                    " area VARCHAR, " +
                    " geu VARCHAR, " +
                    " uch integer, " +
                    " rajon_dom VARCHAR, " +
                    " town VARCHAR, rajon VARCHAR, " +
                    " ulica VARCHAR, " +
                    " nzp_dom integer, idom integer, ndom VARCHAR, nkor VARCHAR, " +
                    " ikvar integer, nkvar VARCHAR, " +
                    " num_ls integer, " +
                    " summary_postfix VARCHAR, " +
                    " fio VARCHAR, " +
                    " pkod " + DBManager.sDecimalType + "(13,0), " +
                    " name_supp VARCHAR, " +
                    " nzp_serv integer, service VARCHAR, " +
                    " dat_vvod VARCHAR, " +
                    " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                    " tarif " + DBManager.sDecimalType + "(14,2), " +
                    " c_calc " + DBManager.sDecimalType + "(14,2), " +
                    " sum_nedop " + DBManager.sDecimalType + "(14,2), " +
                    " sum_tarif " + DBManager.sDecimalType + "(14,2), " +
                    " sum_charge " + DBManager.sDecimalType + "(14,2), " +
                    " money_to " + DBManager.sDecimalType + "(14,2), " +
                    " money_FROM " + DBManager.sDecimalType + "(14,2), " +
                    " money_supp " + DBManager.sDecimalType + "(14,2), " +
                    " real_charge " + DBManager.sDecimalType + "(14,2), " +
                    " real_insaldo " + DBManager.sDecimalType + "(14,2), " +
                    " rsum_tarif " + DBManager.sDecimalType + "(14,2), " +
                    " reval " + DBManager.sDecimalType + "(14,2), " +
                    " sum_outsaldo " + DBManager.sDecimalType + "(14,2), " +
                    " propis_count integer, gil_prib integer, gil_ub integer, " +
                    " ob_s " + DBManager.sDecimalType + "(14,2), " +
                    " otop_s " + DBManager.sDecimalType + "(14,2), " +
                    " gil_s " + DBManager.sDecimalType + "(14,2), " +
                    " rooms " + DBManager.sDecimalType + "(14,2), " +
                    " floor " + DBManager.sDecimalType + "(14,2), " +
                    " privat VARCHAR, " +
                    " status_gil VARCHAR, " +
                    " status VARCHAR, " +
                    " ipu INTEGER, " +
                    " odpu INTEGER, " +
                    " rasch VARCHAR, " +
                    " heating VARCHAR, " +
                    " hotwater VARCHAR, " +
                    " boiler_hotwater VARCHAR, " +
                    " boiler_heating VARCHAR, " +
                    " water_pumping VARCHAR, " +
                    " mkd VARCHAR, " +
                    " norm_name VARCHAR, " +
                    " norm_value " + DBManager.sDecimalType + "(14,7))" + DBManager.sUnlogTempTable);

            ExecSQL(" DROP INDEX  t_svod_kvar_ndx", false);
            ExecSQL("CREATE INDEX t_svod_kvar_ndx ON t_svod(nzp_kvar)", false);

            if (Params.Contains((int)Fields.Building) || Params.Contains((int)Fields.OneGkalHeatingStandart) ||
                Params.Contains((int)Fields.OneGkalHotWaterStandart) ||
                SummariesValues.Contains((int)Summaries.ByBuilding))
            {
                ExecSQL(" DROP INDEX  t_svod_dom_ndx", false);
                ExecSQL("CREATE INDEX t_svod_dom_ndx ON t_svod(nzp_dom)", false);
            }
            if (Params.Contains((int)Fields.Territory))
            {
                ExecSQL(" DROP INDEX  t_svod_area_ndx", false);
                ExecSQL("CREATE INDEX t_svod_area_ndx ON t_svod(nzp_area)", false);
            }
            if (Params.Contains((int)Fields.Geu))
            {
                ExecSQL(" DROP INDEX  t_svod_geu_ndx", false);
                ExecSQL("CREATE INDEX t_svod_geu_ndx ON t_svod(nzp_geu)", false);
            }
            if (Params.Contains((int)Fields.PrevBillingPayment))
            {
                ExecSQL(" DROP INDEX  t_svod_num_ls_ndx", false);
                ExecSQL("CREATE INDEX t_svod_num_ls_ndx ON t_svod(num_ls)", false);
            }
            if (Params.Contains((int)Fields.Locality))
            {
                ExecSQL(" DROP INDEX  t_svod_raj_ndx", false);
                ExecSQL(" DROP INDEX  t_svod_town_ndx", false);
                ExecSQL("CREATE INDEX t_svod_raj_ndx ON t_svod(nzp_raj)", false);
                ExecSQL("CREATE INDEX t_svod_town_ndx ON t_svod(nzp_town)", false);
            }
            if (Params.Contains((int)Fields.Rajon))
            {
                ExecSQL(" DROP INDEX  t_svod_rajon_dom_ndx", false);
                ExecSQL("CREATE INDEX t_svod_rajon_dom_ndx ON t_svod(nzp_raj_dom)", false);
            }
            if (Params.Contains((int)Fields.Street))
            {
                ExecSQL(" DROP INDEX  t_svod_nzp_ul_ndx", false);
                ExecSQL("CREATE INDEX t_svod_nzp_ul_ndx ON t_svod(nzp_ul)", false);
            }
            if (Params.Contains((int)Fields.Supplier))
            {
                ExecSQL(" DROP INDEX  t_svod_nzp_supp_ndx", false);
                ExecSQL("CREATE INDEX t_svod_nzp_supp_ndx ON t_svod(nzp_supp)", false);
            }
            if (Params.Contains((int)Fields.Service) || Params.Contains((int)Fields.CalculationSign))
            {
                ExecSQL(" DROP INDEX  t_svod_nzp_serv_ndx", false);
                ExecSQL("CREATE INDEX t_svod_nzp_serv_ndx ON t_svod(nzp_serv)", false);
            }

            ExecSQL(" CREATE TEMP TABLE t_matrix (row varchar, head varchar, value varchar) " + DBManager.sUnlogTempTable);

            ExecSQL(" CREATE temp SEQUENCE rownumber minvalue 0 ");
        }


        protected override void DropTempTable()
        {
            try
            {
                ExecSQL(" DROP TABLE t_kvars ");
                ExecSQL(" DROP TABLE t_all_kvars ");
                ExecSQL(" DROP TABLE t_doms ");
                ExecSQL(" DROP TABLE t_svod ");
                ExecSQL(" DROP TABLE t_params ");
                ExecSQL(" DROP TABLE t_params2 ");
                ExecSQL(" DROP TABLE t_svod_grouped ");
                ExecSQL(" DROP TABLE t_params_grouped ");
                ExecSQL(" DROP TABLE t_params_grouped2 ");
                ExecSQL(" DROP TABLE t_svod_all ");
                ExecSQL(" DROP TABLE t_matrix ");
                ExecSQL(" DROP SEQUENCE IF EXISTS rownumber ");
                ExecSQL(" DROP TABLE t_svod_all_ordered ");
                ExecSQL(" DROP TABLE t_dat_vvod ");
                ExecSQL(" DROP TABLE t_ais_pasp ");
                ExecSQL(" DROP TABLE tchargekvar ");
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Отчет 'Генератор по начислениям' " + e.Message, MonitorLog.typelog.Error, false);
            }
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
                ExecSQL(" DROP TABLE selected_kvars ", true);
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
                            " INSERT INTO selected_kvars (nzp_kvar, num_ls, pkod, fio, nzp_dom, nzp_geu, nzp_area, typek) " +
                            " SELECT nzp_kvar, num_ls, pkod, fio, nzp_dom, nzp_geu, nzp_area, typek FROM " + tSpls;
                        ExecSQL(sql);
                        ExecSQL("CREATE INDEX ix_sel_kvar_09 ON selected_kvars(nzp_kvar)");
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
            if (ReportParams.ExportFormat == ExportFormat.Excel2007 && UserParamValues["Unformat"].GetValue<int>() == (int)Unformat.Yes)
            {
                ExportToExcelWithoutFormating(ds);
            }
            else if (ds.Tables.Count > 1)
            {
                _isArchive = true;
                var reports = new List<string>();

                int count = 0;
                string nzpUtil = ReportParams.PathForSave.Substring(ReportParams.PathForSave.LastIndexOf('/') + 1,
                    ReportParams.PathForSave.LastIndexOf('.') - ReportParams.PathForSave.LastIndexOf('/') - 1);

                foreach (DataTable table in ds.Tables)
                {
                    var qm = table.Copy();
                    qm.TableName = "Q_master";

                    var tempDs = new DataSet();
                    tempDs.Tables.Add(qm);

                    var report = new FastReport.Report();
                    report.Load(GetTemplate());
                    report.RegisterData(tempDs);
                    PrepareReport(report);

                    var cleanPath = ReportParams.PathForSave;
                    ReportParams.PathForSave = ReportParams.PathForSave.Insert(
                        ReportParams.PathForSave.LastIndexOf('.'),
                        "_" + (count * StringsCount + 1) + "-" +
                        (_rowCount - (count + 1) * StringsCount > 0
                            ? (count + 1) * StringsCount
                            : _rowCount));
                    if (ReportParams.ExportFormat == ExportFormat.Dbf)
                    {
                        SaveReportDbf(tempDs);
                    }
                    else
                    {
                        SaveReport(report);
                    }

                    reports.Add(ReportParams.PathForSave);
                    if (((count + 1) * StringsCount) / _rowCount < 1)
                        SetProccessPercent((((count + 1) * StringsCount) / _rowCount).ToDecimal());

                    ReportParams.PathForSave = cleanPath;
                    count++;

                }

                var exporter = GetExporter();
                if (reports.Count > 0)
                    Archive.GetInstance()
                        .Compress(Constants.ExcelDir.TrimEnd('/') + @"\" + Name + ".zip", reports.ToArray(), true);
                var finfo = new FileInfo(Constants.ExcelDir.TrimEnd('/') + @"\" + Name + ".zip");
                var fsecurity = finfo.GetAccessControl();
                var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                fsecurity.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.FullControl, AccessControlType.Allow));
                finfo.SetAccessControl(fsecurity);

                if (InputOutput.useFtp)
                {
                    ReportParams.PathForSave =
                        InputOutput.SaveOutputFile(Constants.ExcelDir.TrimEnd('/') + @"\" + Name + ".zip");
                }
                else
                {
                    ReportParams.PathForSave = Name + ".zip";
                }

                ExecSQL(" UPDATE  public.excel_utility " +
                        " SET exc_path = '" + ReportParams.PathForSave + "' " +
                        " WHERE nzp_exc = " + nzpUtil);
                ExecSQL(" UPDATE  public.excel_utility " +
                        " SET file_name = '" +
                        ReportParams.PathForSave.Substring(0, ReportParams.PathForSave.LastIndexOf('.')) + ".zip" +
                        "' " +
                        " WHERE nzp_exc = " + nzpUtil);

            }
            else if (ds.Tables.Count > 0)
            {
                ds.Tables[0].TableName = "Q_master";
                base.Generate(ds);
            }
            else if (ds.Tables.Count == 0)
            {
                ds.Tables.Add(ExecSQLToTable(" SELECT '' AS row, '' AS head, '' AS value "));
                ds.Tables[0].TableName = "Q_master";
                base.Generate(ds);
            }
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
            if (!_isArchive && InputOutput.useFtp)
            {
                ReportParams.PathForSave = InputOutput.SaveOutputFile(ReportParams.PathForSave);
            }
        }

        private void ExportToExcelWithoutFormating(DataSet ds)
        {
            Excel.Application excel = new Excel.ApplicationClass();
            var book = excel.Workbooks.Add();
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            foreach (DataTable table in ds.Tables)
            {
                if (table != ds.Tables[0]) excel.Worksheets.Add();
                DeleteUnusedColumns(table);
                var workSheet = (Excel.Worksheet)excel.ActiveSheet;
                //book.Worksheets.Add(workSheet);
                workSheet.Name = table.TableName;
                string column = (table.Columns.Count - 1 >= 26 ? alphabet[(table.Columns.Count - 1) / 26 - 1].ToString() : string.Empty) + alphabet[(table.Columns.Count - 1) % 26];

                var values = new object[table.Rows.Count, table.Columns.Count];
                for (var i = 0; i < table.Rows.Count; i++)
                {
                    var row = table.Rows[i];
                    for (var j = 0; j < table.Columns.Count; j++)
                    {
                        values[i, j] = row[j];
                    }
                }
                workSheet.Range["A3:" + column + (table.Rows.Count + 2), Type.Missing].Value2 = values;
                var range = workSheet.Range["A1"];
                range.Value2 = "Генератор " + (MonthS == MonthPo && YearS == YearPo
                    ? _months[MonthS] + " " + YearS + "г."
                    : "c " + _months[MonthS] + " " + YearS + "г. по " + _months[MonthPo] + " " + YearPo + "г.");
                range.Font.Bold = true;
                range = workSheet.Range[workSheet.Cells[1, 1], workSheet.Cells[1, 10]];
                range.Merge();
                for (var i = 0; i < table.Columns.Count; i++)
                {
                    workSheet.Cells[2, i + 1] = GetColumnName(table.Columns[i].ColumnName);
                    ((Excel.Range)workSheet.Columns[i + 1]).AutoFit();
                    range = workSheet.Range[(i >= 26 ? alphabet[i / 26 - 1].ToString() : string.Empty) + alphabet[i % 26] + "2"];
                    range.Font.Bold = true;
                    range.BorderAround(Excel.XlLineStyle.xlContinuous, Excel.XlBorderWeight.xlThick);
                }
            }
            book.SaveAs(Filename: ReportParams.PathForSave);
            book.Close();
            excel.Quit();
            if (InputOutput.useFtp)
            {
                ReportParams.PathForSave = InputOutput.SaveOutputFile(ReportParams.PathForSave);
            }
        }

        private void DeleteUnusedColumns(DataTable table)
        {
            if (table.Columns["seq"] != null) table.Columns.Remove(table.Columns["seq"]);
            if (!SummariesValues.Contains((int)Summaries.ByAccount) && !SummariesValues.Contains((int)Summaries.ByAccount) && table.Columns["summary_postfix"] != null) table.Columns.Remove(table.Columns["summary_postfix"]);
            if (table.Columns["nzp_dom"] != null) table.Columns.Remove(table.Columns["nzp_dom"]);
            if (table.Columns["idom"] != null) table.Columns.Remove(table.Columns["idom"]);
            if (table.Columns["ikvar"] != null) table.Columns.Remove(table.Columns["ikvar"]);
            if (table.Columns["nzp_serv"] != null) table.Columns.Remove(table.Columns["nzp_serv"]);
            if (!Params.Contains((int)Fields.Territory) && table.Columns["area"] != null) { table.Columns.Remove(table.Columns["area"]); }
            if (!Params.Contains((int)Fields.Geu) && table.Columns["geu"] != null) { table.Columns.Remove(table.Columns["geu"]); }
            if (!Params.Contains((int)Fields.Uchastok) && table.Columns["uch"] != null) { table.Columns.Remove(table.Columns["uch"]); }
            if (!Params.Contains((int)Fields.AccountStatus) && table.Columns["status"] != null) { table.Columns.Remove(table.Columns["status"]); }
            if (!Params.Contains((int)Fields.IsIpu) && table.Columns["ipu"] != null) { table.Columns.Remove(table.Columns["ipu"]); }
            if (!Params.Contains((int)Fields.IsOdpu) && table.Columns["odpu"] != null) { table.Columns.Remove(table.Columns["odpu"]); }
            if (!Params.Contains((int)Fields.CalculationSign) && table.Columns["rasch"] != null) { table.Columns.Remove(table.Columns["rasch"]); }
            if (!Params.Contains((int)Fields.OneGkalHeatingStandart) && table.Columns["heating"] != null) { table.Columns.Remove(table.Columns["heating"]); }
            if (!Params.Contains((int)Fields.OneGkalHotWaterStandart) && table.Columns["hotwater"] != null) { table.Columns.Remove(table.Columns["hotwater"]); }
            if (!Params.Contains((int)Fields.Rajon) && table.Columns["rajon_dom"] != null) { table.Columns.Remove(table.Columns["rajon_dom"]); }
            if (!Params.Contains((int)Fields.Locality) && table.Columns["town"] != null && table.Columns["rajon"] != null) { table.Columns.Remove(table.Columns["town"]); table.Columns.Remove(table.Columns["rajon"]); }
            if (!Params.Contains((int)Fields.Street) && table.Columns["ulica"] != null) { table.Columns.Remove(table.Columns["ulica"]); }
            if (!Params.Contains((int)Fields.Building) && !SummariesValues.Contains((int)Summaries.ByBuilding) && table.Columns["ndom"] != null && table.Columns["nkor"] != null) { table.Columns.Remove(table.Columns["ndom"]); table.Columns.Remove(table.Columns["nkor"]); }
            if (!Params.Contains((int)Fields.Appartment) && table.Columns["nkvar"] != null) { table.Columns.Remove(table.Columns["nkvar"]); }
            if (!Params.Contains((int)Fields.Account) && !SummariesValues.Contains((int)Summaries.ByAccount) && table.Columns["num_ls"] != null) { table.Columns.Remove(table.Columns["num_ls"]); }
            if (!Params.Contains((int)Fields.Fio) && table.Columns["fio"] != null) { table.Columns.Remove(table.Columns["fio"]); }
            if (!Params.Contains((int)Fields.PrescriptedCount) && table.Columns["propis_count"] != null) { table.Columns.Remove(table.Columns["propis_count"]); }
            if (!Params.Contains((int)Fields.TemporaryLiving) && table.Columns["gil_prib"] != null) { table.Columns.Remove(table.Columns["gil_prib"]); }
            if (!Params.Contains((int)Fields.TemporaryOut) && table.Columns["gil_ub"] != null) { table.Columns.Remove(table.Columns["gil_ub"]); }
            if (!Params.Contains((int)Fields.TotalSquare) && table.Columns["ob_s"] != null) { table.Columns.Remove(table.Columns["ob_s"]); }
            if (!Params.Contains((int)Fields.HeatingSquare) && table.Columns["otop_s"] != null) { table.Columns.Remove(table.Columns["otop_s"]); }
            if (!Params.Contains((int)Fields.LivingSquare) && table.Columns["gil_s"] != null) { table.Columns.Remove(table.Columns["gil_s"]); }
            if (!Params.Contains((int)Fields.Rooms) && table.Columns["rooms"] != null) { table.Columns.Remove(table.Columns["rooms"]); }
            if (!Params.Contains((int)Fields.Floor) && table.Columns["floor"] != null) { table.Columns.Remove(table.Columns["floor"]); }
            if (!Params.Contains((int)Fields.PayCode) && table.Columns["pkod"] != null) { table.Columns.Remove(table.Columns["pkod"]); }
            if (!Params.Contains((int)Fields.Supplier) && table.Columns["name_supp"] != null) { table.Columns.Remove(table.Columns["name_supp"]); }
            if (!Params.Contains((int)Fields.Service) && table.Columns["service"] != null) { table.Columns.Remove(table.Columns["service"]); }
            if (!Params.Contains((int)Fields.Insaldo) && table.Columns["sum_insaldo"] != null) { table.Columns.Remove(table.Columns["sum_insaldo"]); }
            if (!Params.Contains((int)Fields.Tarif) && table.Columns["tarif"] != null) { table.Columns.Remove(table.Columns["tarif"]); }
            if (!Params.Contains((int)Fields.Consumption) && table.Columns["c_calc"] != null) { table.Columns.Remove(table.Columns["c_calc"]); }
            if (!Params.Contains((int)Fields.ShortDelivery) && table.Columns["sum_nedop"] != null) { table.Columns.Remove(table.Columns["sum_nedop"]); }
            if (!Params.Contains((int)Fields.ChargedWithShortDelivery) && table.Columns["sum_tarif"] != null) { table.Columns.Remove(table.Columns["sum_tarif"]); }
            if (!Params.Contains((int)Fields.ForPayment) && table.Columns["sum_charge"] != null) { table.Columns.Remove(table.Columns["sum_charge"]); }
            if (!Params.Contains((int)Fields.Paid) && table.Columns["money_to"] != null) { table.Columns.Remove(table.Columns["money_to"]); }
            if (!Params.Contains((int)Fields.PrevBillingPayment) && table.Columns["money_from"] != null) { table.Columns.Remove(table.Columns["money_from"]); }
            if (!Params.Contains((int)Fields.DirectSupplierPayment) && table.Columns["money_supp"] != null) { table.Columns.Remove(table.Columns["money_supp"]); }
            if (!Params.Contains((int)Fields.ChargesCorrection) && table.Columns["real_charge"] != null) { table.Columns.Remove(table.Columns["real_charge"]); }
            if (!Params.Contains((int)Fields.InsaldoCorrection) && table.Columns["real_insaldo"] != null) { table.Columns.Remove(table.Columns["real_insaldo"]); }
            if (!Params.Contains((int)Fields.Charged) && table.Columns["rsum_tarif"] != null) { table.Columns.Remove(table.Columns["rsum_tarif"]); }
            if (!Params.Contains((int)Fields.Reval) && table.Columns["reval"] != null) { table.Columns.Remove(table.Columns["reval"]); }
            if (!Params.Contains((int)Fields.Outsaldo) && table.Columns["sum_outsaldo"] != null) { table.Columns.Remove(table.Columns["sum_outsaldo"]); }
            if (!Params.Contains((int)Fields.Privat) && table.Columns["privat"] != null) { table.Columns.Remove(table.Columns["privat"]); }
            if (!Params.Contains((int)Fields.EnterDate) && table.Columns["dat_vvod"] != null) { table.Columns.Remove(table.Columns["dat_vvod"]); }
            if (!Params.Contains((int)Fields.StatusGil) && table.Columns["status_gil"] != null) { table.Columns.Remove(table.Columns["status_gil"]); }
            if (!Params.Contains((int)Fields.DataBank) && table.Columns["pref"] != null) { table.Columns.Remove(table.Columns["pref"]); }
            if (!Params.Contains((int)Fields.MonthYear) && table.Columns["monthyear"] != null) { table.Columns.Remove(table.Columns["monthyear"]); }
            if (!Params.Contains((int)Fields.BoilerHotWater) && table.Columns["boiler_hotwater"] != null) { table.Columns.Remove(table.Columns["boiler_hotwater"]); }
            if (!Params.Contains((int)Fields.BoilerHeating) && table.Columns["boiler_heating"] != null) { table.Columns.Remove(table.Columns["boiler_heating"]); }
            if (!Params.Contains((int)Fields.WaterPumping) && table.Columns["water_pumping"] != null) { table.Columns.Remove(table.Columns["water_pumping"]); }
            if (!Params.Contains((int)Fields.IsMkd) && table.Columns["mkd"] != null) { table.Columns.Remove(table.Columns["mkd"]); }
            if (!Params.Contains((int)Fields.Normative) && table.Columns["norm_name"] != null && table.Columns["norm_value"] != null) { table.Columns.Remove(table.Columns["norm_name"]); table.Columns.Remove(table.Columns["norm_value"]); }
        }

        private string GetColumnName(string rawName)
        {
            switch (rawName)
            {
                case "seq": return "";
                case "area": return "Территория (УК)";
                case "geu": return "ЖЭУ";
                case "uch": return "Участок";
                case "rajon_dom": return "Район";
                case "town": return "Город";
                case "rajon": return "Район";
                case "ulica": return "Улица";
                case "nzp_dom": return "Id дома";
                case "idom": return "Номер дома";
                case "ndom": return "Дом";
                case "nkor": return "Корпус";
                case "ikvar": return "Номер квартиры";
                case "nkvar": return "Квартира";
                case "num_ls": return "Лицевой счет";
                case "summary_postfix": return "Итого";
                case "fio": return "ФИО";
                case "pkod": return "Платежный код";
                case "name_supp": return "Поставщик";
                case "nzp_serv": return "Id услуги";
                case "service": return "Услуга";
                case "dat_vvod": return "Дата последней оплаты ЛС";
                case "pref": return "Банк данных";
                case "monthyear": return "Месяц";
                case "sum_insaldo": return "Входящее сальдо";
                case "tarif": return "Тариф";
                case "c_calc": return "Расход";
                case "sum_nedop": return "Недопоставка";
                case "sum_tarif": return "Начислено с учетом недопоставки";
                case "sum_charge": return "К оплате";
                case "money_to": return "Оплачено";
                case "money_from": return "Оплаты предыдущих биллинговых систем";
                case "money_supp": return "Оплата напрямую поставщикам";
                case "real_charge": return "Корректировка начислений";
                case "real_insaldo": return "Корректировка входящего сальдо";
                case "rsum_tarif": return "Начислено";
                case "reval": return "Перерасчет";
                case "sum_outsaldo": return "Исходящее сальдо";
                case "propis_count": return "Количество прописанных";
                case "gil_prib": return "Количество временно проживающих";
                case "gil_ub": return "Количество временно выбывших";
                case "ob_s": return "Общая площадь";
                case "otop_s": return "Отапливаемая площадь";
                case "gil_s": return "Жилая площадь";
                case "rooms": return "Количество комнат";
                case "floor": return "Этаж";
                case "privat": return "Приватизировано";
                case "status_gil": return "Статус жилья";
                case "status": return "Статус ЛС";
                case "ipu": return "Количество ИПУ";
                case "odpu": return "Количество ОДПУ";
                case "rasch": return "Признак расчета";
                case "heating": return "Домовой норматив на 1 ГКал/кв.м отопления";
                case "hotwater": return "Домовой норматив на 1 ГКал/куб.м горячей воды";
                case "boiler_hotwater": return "Домовой норматив на 1 ГКал/куб.м горячей воды";
                case "boiler_heating": return "Домовой норматив на 1 ГКал/кв.м отопления";
                case "water_pumping": return "Водозабор";
                case "mkd": return "МКД/Частный сектор";
                case "norm_name": return "Название норматива";
                case "norm_value": return "Значение норматива";
                default: return "";
            }
        }
    }


    /// <summary>
    /// Класс отвечающий за префиксы таблиц
    /// </summary>
    public class TableAlias
    {
        //Фиксированные префиксы таблиц не зависят от месяца

        /// <summary> Префикс схемы данных </summary>
        private string _pref;

        /// <summary> Расчетный месяц </summary>
        private int _month;

        /// <summary> Расчетный год </summary>
        private int _year;

        /// <summary> Префикс схемы Data  </summary>
        public string databank;

        /// <summary> Таблица Лицевых счетов </summary>
        public string kvarTable;

        /// <summary> Таблица домов </summary>
        public string domTable;

        /// <summary> Таблица районов у домов </summary>
        public string rajonDomTable;

        /// <summary> Таблица улиц </summary>
        public string ulTable;

        /// <summary> Таблица районов или населенных пунктов </summary>
        public string rajTable;

        /// <summary> таблица счетчиков </summary>
        public string countersSpisTable;

        /// <summary> Таблица справочник </summary>
        public string resyTable;

        //Переменные префиксы таблиц зависят от месяца

        /// <summary> Таблица расходов по услугам  </summary>
        public string countersTable;
        /// <summary> Таблица расходов и тарифов по услугам </summary>
        public string calcGkuTable;
        /// <summary> Таблица параметров квартир </summary>
        public string prmTableOne;
        /// <summary> Таблица параметров домов </summary>
        public string prmTableTwo;
        /// <summary> Таблица параметров лицевого счета </summary>
        public string prmTableThree;
        /// <summary> таблица перекидок </summary>
        public string perekidka;
        /// <summary> Таблица оплат поставщиков </summary>
        public string fromSupplier;
        /// <summary> таблица количества проживающих </summary>
        public string gilAis;
        /// <summary> таблица начислений </summary>
        public string chargeTable;
        public TableAlias(string pref, int year, int month)
        {
            _pref = pref.Trim();
            _month = month;
            _year = year;
        }


        /// <summary>
        /// Подготовка алиасов таблиц и проверка их на существование
        /// </summary>
        /// <param name="connection">Подключение к БД</param>
        /// <returns>TRUE если все таблицы существуют</returns>
        public bool InitAlias(IDbConnection connection)
        {
            chargeTable = _pref + "_charge_" + (_year - 2000).ToString("00") + DBManager.tableDelimiter +
                                 "charge_" + _month.ToString("00");
            countersTable = _pref + "_charge_" + (_year - 2000).ToString("00") + DBManager.tableDelimiter +
                            "counters_" + _month.ToString("00");
            calcGkuTable = _pref + "_charge_" + (_year - 2000).ToString("00") + DBManager.tableDelimiter +
                           "calc_gku_" + _month.ToString("00");
            prmTableOne = _pref + DBManager.sDataAliasRest + "prm_1 ";
            prmTableTwo = _pref + DBManager.sDataAliasRest + "prm_2 ";
            prmTableThree = _pref + DBManager.sDataAliasRest + "prm_3 ";
            perekidka = _pref + "_charge_" + (_year - 2000).ToString("00") + DBManager.tableDelimiter +
                        "perekidka";
            fromSupplier = _pref + "_charge_" + (_year - 2000).ToString("00") + DBManager.tableDelimiter +
                           "from_supplier ";
            gilAis = _pref + "_charge_" + (_year - 2000).ToString("00") + DBManager.tableDelimiter + "gil_" + _month.ToString("00");

            kvarTable = _pref + DBManager.sDataAliasRest + "kvar ";
            domTable = _pref + DBManager.sDataAliasRest + "dom ";
            ulTable = _pref + DBManager.sDataAliasRest + "s_ulica ";
            rajTable = _pref + DBManager.sDataAliasRest + "s_rajon ";
            countersSpisTable = _pref + DBManager.sDataAliasRest + "counters_spis ";
            resyTable = _pref + DBManager.sKernelAliasRest + "res_y ";
            return TableExists(connection);
        }

        /// <summary>
        /// Проверка на существование таблиц
        /// </summary>
        /// <param name="connection">Подключеник к БЖ</param>
        /// <returns>TRUE если все таблицы существуют</returns>
        private bool TableExists(IDbConnection connection)
        {
            return DBManager.TempTableInWebCashe(connection, kvarTable) &&
                DBManager.TempTableInWebCashe(connection, domTable) &&
                DBManager.TempTableInWebCashe(connection, ulTable) &&
                DBManager.TempTableInWebCashe(connection, rajTable) &&
                   DBManager.TempTableInWebCashe(connection, prmTableOne) &&
                   DBManager.TempTableInWebCashe(connection, prmTableTwo) &&
                   DBManager.TempTableInWebCashe(connection, prmTableThree) &&
                   DBManager.TempTableInWebCashe(connection, countersSpisTable) &&
                   DBManager.TempTableInWebCashe(connection, chargeTable) &&
                   DBManager.TempTableInWebCashe(connection, fromSupplier);
        }

    }
}
