using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web.Configuration;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Tula.Properties;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.EPaspXsd;
using STCLINE.KP50.Global;
using Bars.KP50.Utils;
using Newtonsoft.Json;


namespace Bars.KP50.Report.Tula.Reports
{

    /// <summary>
    /// Сводный отчет по платежам для Тулы
    /// </summary>
    public class Report71001016 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71. Генератор по перерасчетам и изменениям сальдо"; }
        }

        public override string Description
        {
            get { return "Генератор по перерасчетам и изменениям сальдо"; }
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
            get { return Resources.Report_71_1_16; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
        }

        #region Параметры
        /// <summary> Месяц </summary>
        protected int Month { get; set; }

        /// <summary> Год </summary>
        protected int Year { get; set; }

        /// <summary>Поставщики</summary>
        private string SupplierHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Параметры</summary>
        private List<long> Params { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }

        private List<long> Services { get; set; }

        /// <summary>Заголовок Услуг</summary>     
        protected string ServicesHeader { get; set; }

        /// <summary>Список постфиксов банков в БД</summary>  
        private List<string> PrefBanks { get; set; }

        /// <summary>Статус</summary>
        protected int StatusLS { get; set; }

        /// <summary>Типы изменений</summary>
        private List<long> ChangeType { get; set; }

        #endregion

        public override List<UserParam> GetUserParams()
        {


            return new List<UserParam>
            {                
                
                new MonthParameter { Value = DateTime.Today.Month },
                new YearParameter  { Value = DateTime.Today.Year },
                new BankSupplierParameter(),
                new ServiceParameter(), 

                new ComboBoxParameter{
                    Code = "StatusLS",
                    Name = "Статус ЛС",
                    Value = (int)AccountStatus.All,
                    StoreData = new List<object>
                    {
                        
                        new { Id = (int)AccountStatus.All, Name = "Все" },
                        new { Id = (int)AccountStatus.Open, Name = "Только открытые" },
                        new { Id = (int)AccountStatus.Close, Name = "Только закрытые" }
                    }
                }, 

                new ComboBoxParameter(true) {
                    Name = "Типы изменения", 
                    Code = "ChangeType",
                    Value = (int)ChangeTypes.Noone,
                    StoreData = new List<object> {
                       
                        new { Id = (int)ChangeTypes.Reval, Name = "Перерасчет"},
                        new { Id = (int)ChangeTypes.SaldoCorrection, Name = "Корректировка сальдо"},
                        new { Id = (int)ChangeTypes.InsaldoCorrection, Name = "Корректировка входящего сальдо"},
                        new { Id = (int)ChangeTypes.OutsaldoCorrection, Name = "Корректировка исходящего сальдо"},
                        new { Id = (int)ChangeTypes.ChargesCorrection, Name = "Корректировка начисления"},
                        new { Id = (int)ChangeTypes.PaymentCorrection, Name = "Корректировка оплаты"},
                        new { Id = (int)ChangeTypes.AutoInsideSupplierWithPayment, Name = "Автоматическая, внутри поставщика оплатами"},
                        new { Id = (int)ChangeTypes.AutoInsidePrincipalWithPayment, Name = "Автоматическая, внутри принципала, между поставщиками оплатами"},
                        new { Id = (int)ChangeTypes.AutoBetweenPrincipalWithPayment, Name = "Автоматическая, между принципалами оплатами"},
                        new { Id = (int)ChangeTypes.AutoBetweenSupplierSaldoReplacemnet, Name = "Автоматическая, перенос сальдо между поставщиками"},
                        new { Id = (int)ChangeTypes.AutoInsideSupplierSaldoChange, Name = "Автоматическая, внутри поставщика изменением сальдо"},
                        new { Id = (int)ChangeTypes.AutoInsidePrincipalSaldoChange, Name = "Автоматическая, внутри принципала, между поставщиками изменением сальдо"},
                        new { Id = (int)ChangeTypes.AutoBetweenPrincipalSaldoChange, Name = "Автоматическая, между принципалами изменением сальдо"},
                        new { Id = (int)ChangeTypes.ConsumptionCorection, Name = "Корректировка расхода"},
                        new { Id = (int)ChangeTypes.Other, Name = "Прочие изменения"},
                    }
                },

                new ComboBoxParameter(true) {
                    Name = "Параметры отчета", 
                    Code = "Params",
                    Value = (int)Fields.Account,
                    Require = true,
                    StoreData = new List<object> {
                        new { Id = (int)Fields.Account, Name = "ЛС"},
                        new { Id = (int)Fields.AccountStatus, Name = "Статус ЛС"},
                        new { Id = (int)Fields.PayCode, Name = "Платежный код"},
                        new { Id = (int)Fields.Uk, Name = "УК"},
                        new { Id = (int)Fields.Geu, Name = "ЖЭУ"},
                        new { Id = (int)Fields.District, Name = "Район"},
                        new { Id = (int)Fields.Locality, Name = "Населенный пункт"},
                        new { Id = (int)Fields.Street, Name = "Улица"},
                        new { Id = (int)Fields.House, Name = "Дом"},
                        new { Id = (int)Fields.Appartment, Name = "Квартира"},
                        new { Id = (int)Fields.Contract, Name = "Договор"},
                        new { Id = (int)Fields.Service, Name = "Услуга"},
                        new { Id = (int)Fields.ChangeType, Name = "Тип изменения"},
                        new { Id = (int)Fields.ChangeSum, Name = "Сумма изменения"},
                        new { Id = (int)Fields.Tarif, Name = "Тариф"},
                        new { Id = (int)Fields.Volume, Name = "Объем"},
                        new { Id = (int)Fields.ChangeDate, Name = "Дата изменения"},
                        new { Id = (int)Fields.BaseDocument, Name = "Документ-основание"},
                        new { Id = (int)Fields.Comment, Name = "Комментарий"},
                        new { Id = (int)Fields.RevalSign, Name = "Признак перерасчета"},
                        new { Id = (int)Fields.RevalPeriod, Name = "Период перерасчета"},
                        new { Id = (int)Fields.User, Name = "Пользователь, создавший изменение"},
                    }
                },
            };
        }
        private enum Fields
        {
            Account = 1, AccountStatus, PayCode, Uk, Geu, District, Locality, Street, House, Appartment, Contract,
            Service, ChangeType, ChangeSum, Tarif, Volume, ChangeDate, BaseDocument, Comment, RevalSign,
            RevalPeriod, User
        }
        private enum ChangeTypes
        {
            Other = -1, Noone = 1, Reval = 0, SaldoCorrection = 20, InsaldoCorrection = 100, OutsaldoCorrection, ChargesCorrection,
            PaymentCorrection, AutoInsideSupplierWithPayment, AutoInsidePrincipalWithPayment,
            AutoBetweenPrincipalWithPayment, AutoBetweenSupplierSaldoReplacemnet,
            AutoInsideSupplierSaldoChange = 114, AutoInsidePrincipalSaldoChange, AutoBetweenPrincipalSaldoChange, ConsumptionCorection = 163
        }
        private enum AccountStatus
        {
            Open = -1, Close, All
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var MonthStr = new[]
            {
                "", "январь", "февраль", "март", "апрель", "май", "июнь",
                "июль", "август", "сентябрь", "октябрь", "ноябрь", "декабрь"
            };

            report.SetParameterValue("time", DateTime.Now.ToLongTimeString());
            report.SetParameterValue("dat", DateTime.Now.ToShortDateString());
            report.SetParameterValue("pMonth", MonthStr[Month]);

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(ServicesHeader) ? "Услуги: " + ServicesHeader + "\n" : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);

            string limit = ReportParams.ExportFormat == ExportFormat.Excel2007 ? "40000" : "100000";

        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].Value.To<int>();
            Services = UserParamValues["Services"].GetValue<List<long>>();
            Params = UserParamValues["Params"].GetValue<List<long>>();
            ChangeType = UserParamValues["ChangeType"].GetValue<List<long>>();
            StatusLS = UserParamValues["StatusLS"].GetValue<int>();
            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
        }

        public override DataSet GetData()
        {
            SetPrefBanks();
            var whereSupp = GetWhereSupp("t.nzp_supp");
            var whereServ = GetWhereServ();
            var typCh = GetTC();
            var listLc = GetSelectedKvars();

            var sql = new StringBuilder();
            var sqlgrouper = new StringBuilder();
            sql.Append(" INSERT INTO t_71_1_16( "); 
            if (Params.Contains((int)Fields.ChangeType)) { sql.Append(" prizn, "); sqlgrouper.Append(" prizn, "); }
            if (Params.Contains((int)Fields.Account) || Params.Contains((int)Fields.PayCode) || Params.Contains((int)Fields.Uk) ||
                Params.Contains((int)Fields.Geu) || Params.Contains((int)Fields.District) || Params.Contains((int)Fields.Locality) ||
                Params.Contains((int)Fields.Street) || Params.Contains((int)Fields.House) || Params.Contains((int)Fields.Appartment))
            { sql.Append(" nzp_kvar, "); sqlgrouper.Append(" t.nzp_kvar, "); sqlgrouper.Append(" t.nzp_kvar, "); }
            if (Params.Contains((int)Fields.AccountStatus)) { sql.Append(" ls_st, "); }
            if (Params.Contains((int)Fields.Contract)) { sql.Append(" nzp_supp, "); sqlgrouper.Append(" t.nzp_supp, "); }
            if (Params.Contains((int)Fields.Service)) { sql.Append(" nzp_serv, "); sqlgrouper.Append(" t.nzp_serv, "); }
            if (Params.Contains((int)Fields.ChangeType)) { sql.Append(" nzp_type, "); sqlgrouper.Append(" nzp_type, "); }
            if (Params.Contains((int)Fields.ChangeSum)) { sql.Append(" sum_, "); }
            if (Params.Contains((int)Fields.Tarif)) { sql.Append(" tarif, "); sqlgrouper.Append(" t.tarif, "); }
            if (Params.Contains((int)Fields.Volume)) { sql.Append(" ob, "); }
            if (Params.Contains((int)Fields.ChangeDate)) { sql.Append(" dat_, "); sqlgrouper.Append(" dat_, "); }
            if (Params.Contains((int)Fields.BaseDocument)) { sql.Append(" nzp_osn, "); sqlgrouper.Append(" nzp_doc_base, "); }
            if (Params.Contains((int)Fields.Comment)) { sql.Append(" comment , "); sqlgrouper.Append(" comment, "); }
            if (Params.Contains((int)Fields.RevalSign)) { sql.Append(" nzp_reason, ");  }
            if (Params.Contains((int)Fields.RevalPeriod)) { sql.Append(" period, "); sqlgrouper.Append(" period, "); }
            if (Params.Contains((int)Fields.User)) { sql.Append(" nzp_user, "); sqlgrouper.Append(" p.nzp_user, "); }
            var insrt = sql.ToString().Trim(' ', ',') + ") ";
            var grouper = sqlgrouper.Length == 0 ? "" : " GROUP BY " + sqlgrouper.Remove(sqlgrouper.Length - 2, 2);

            foreach (var pref in PrefBanks)
            {
                var chargeTable = pref + "_charge_" + (Year % 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + Month.ToString("00");
                var perekidkaTable = pref + "_charge_" + (Year % 2000).ToString("00") + DBManager.tableDelimiter + "perekidka ";
                var mustCalcTable = pref + DBManager.sDataAliasRest + "must_calc ";

                if (ChangeType == null || ChangeType.Contains((int)ChangeTypes.Reval))
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(insrt);
                    sql.Append(" SELECT ");
                    if (Params.Contains((int)Fields.ChangeType)) { sql.Append(" 1 as prizn, "); }
                    if (Params.Contains((int)Fields.Account) || Params.Contains((int)Fields.PayCode) || Params.Contains((int)Fields.Uk) ||
                        Params.Contains((int)Fields.Geu) || Params.Contains((int)Fields.District) || Params.Contains((int)Fields.Locality) ||
                        Params.Contains((int)Fields.Street) || Params.Contains((int)Fields.House) || Params.Contains((int)Fields.Appartment)) { sql.Append(" t.nzp_kvar, "); }
                    if (Params.Contains((int)Fields.AccountStatus)) { sql.Append(" MAX(trim(p3.val_prm)) as ls_st, "); }
                    if (Params.Contains((int)Fields.Contract)) { sql.Append(" t.nzp_supp, "); }
                    if (Params.Contains((int)Fields.Service)) { sql.Append(" t.nzp_serv, "); }
                    if (Params.Contains((int)Fields.ChangeType)) { sql.Append(" 0 as nzp_type, "); }
                    if (Params.Contains((int)Fields.ChangeSum)) { sql.Append(" SUM(reval) as sum_, "); }
                    if (Params.Contains((int)Fields.Tarif)) { sql.Append(" tarif, "); }
                    if (Params.Contains((int)Fields.Volume)) { sql.Append(" SUM(CASE WHEN tarif <> 0 THEN reval/tarif ELSE c_reval END) as ob, "); }
                    if (Params.Contains((int)Fields.ChangeDate)) { sql.Append(" p.dat_when as dat_, "); }
                    if (Params.Contains((int)Fields.BaseDocument)) { sql.Append(" -1 as nzp_doc_base, "); }
                    if (Params.Contains((int)Fields.Comment)) { sql.Append(" null as comment, ");  }
                    if (Params.Contains((int)Fields.RevalSign)) { sql.Append(" MAX(kod1), "); }
                    if (Params.Contains((int)Fields.RevalPeriod)) { sql.Append(" p.dat_s||'-'||p.dat_po as period, "); }
                    if (Params.Contains((int)Fields.User)) { sql.Append(" p.nzp_user, "); }
                    sql.Remove(sql.Length - 2, 2);
                    sql.Append(" FROM " + chargeTable + " t ");
                    if (Params.Contains((int)Fields.RevalPeriod) || Params.Contains((int)Fields.RevalSign) || Params.Contains((int)Fields.User) || Params.Contains((int)Fields.ChangeDate))
                    {
                        sql.Append(" LEFT JOIN " + mustCalcTable + " p ");
                        sql.Append(" ON p.nzp_kvar=t.nzp_kvar AND p.nzp_supp= t.nzp_supp AND p.nzp_serv= t.nzp_serv ");
                        sql.Append(" AND p.month_ = " + Month);
                        sql.Append(" AND p.year_ = " + Year + " "); 
                        if (Params.Contains((int)Fields.ChangeDate))
                        {
                            sql.Append(" AND p.dat_when BETWEEN '" + new DateTime(Year, Month, 1) + "' AND '" + new DateTime(Year, Month, 1).AddMonths(1).AddDays(-1) + "' ");
                        }
                    }


                    if (StatusLS != (int)AccountStatus.All || Params.Contains((int)Fields.AccountStatus)) 
                    { sql.Append(" LEFT JOIN " + pref + DBManager.sDataAliasRest + "prm_3 p3 "); }
                    if (StatusLS != (int)AccountStatus.All)
                    {
                        sql.Append(" ON p3.nzp=t.nzp_kvar AND p3.nzp_prm=51 AND p3.val_prm='" + StatusLS + "'" +
                                   " AND p3.is_actual=1 AND p3.dat_s<='" + new DateTime(Year, Month, 1).AddMonths(1).AddDays(-1) + "'" +
                                   " AND p3.dat_po>='" + new DateTime(Year, Month, 1) + "'");
                    }
                    else if (Params.Contains((int)Fields.AccountStatus))
                    { sql.Append(" ON p3.nzp=t.nzp_kvar AND p3.nzp_prm=51 " +
                                 " AND p3.is_actual=1 AND p3.dat_s<='" + new DateTime(Year, Month, 1).AddMonths(1).AddDays(-1) + "'" +
                                 " AND p3.dat_po>='" + new DateTime(Year, Month, 1) + "'");
                    }

                    if (listLc)
                    {
                        sql.Append(" INNER JOIN selected_kvars sk ON sk.nzp_kvar=t.nzp_kvar ");
                        if (Params.Contains((int)Fields.RevalPeriod) || Params.Contains((int)Fields.RevalSign) || Params.Contains((int)Fields.User) || Params.Contains((int)Fields.ChangeDate))
                            sql.Append(" AND p.nzp_kvar=sk.nzp_kvar "); 
                            
                    }
                    sql.Append(" WHERE t.nzp_serv>1 AND t.dat_charge is null ");
                    sql.Append(whereSupp);
                    sql.Append(whereServ);
                    sql.Append(grouper);
                    ExecSQL(sql.ToString());
                }

                sql.Remove(0, sql.Length);
                sql.Append(insrt);
                sql.Append(" SELECT ");
                if (Params.Contains((int)Fields.ChangeType)) { sql.Append(" 0 as prizn, "); }
                if (Params.Contains((int)Fields.Account) || Params.Contains((int)Fields.PayCode) || Params.Contains((int)Fields.Uk) ||
                    Params.Contains((int)Fields.Geu) || Params.Contains((int)Fields.District) || Params.Contains((int)Fields.Locality) ||
                    Params.Contains((int)Fields.Street) || Params.Contains((int)Fields.House) || Params.Contains((int)Fields.Appartment)) { sql.Append(" t.nzp_kvar, "); }
                if (Params.Contains((int)Fields.AccountStatus)) { sql.Append("MAX(trim(p3.val_prm)), "); }
                if (Params.Contains((int)Fields.Contract)) { sql.Append(" t.nzp_supp, "); }
                if (Params.Contains((int)Fields.Service)) { sql.Append(" t.nzp_serv, "); }
                if (Params.Contains((int)Fields.ChangeType)) { sql.Append(" type_rcl as nzp_type, "); }
                if (Params.Contains((int)Fields.ChangeSum)) { sql.Append(" SUM(sum_rcl) as sum_, "); }
                if (Params.Contains((int)Fields.Tarif)) { sql.Append(" t.tarif, "); }
                if (Params.Contains((int)Fields.Volume)) { sql.Append(" SUM(CASE WHEN t.tarif <> 0 THEN sum_rcl/t.tarif ELSE c_reval END) AS ob, "); }
                if (Params.Contains((int)Fields.ChangeDate)) { sql.Append(" p.date_rcl as dat_, "); }
                if (Params.Contains((int)Fields.BaseDocument)) { sql.Append(" p.nzp_doc_base, "); }
                if (Params.Contains((int)Fields.Comment)) { sql.Append(" comment, "); }
                if (Params.Contains((int)Fields.RevalSign)) { sql.Append(" null, "); }
                if (Params.Contains((int)Fields.RevalPeriod)) { sql.Append(" null as period, "); }
                if (Params.Contains((int)Fields.User)) { sql.Append(" p.nzp_user, "); }
                if (sql.Length > 0) sql.Remove(sql.Length - 2, 2);
                sql.Append(" FROM " + chargeTable + " t LEFT JOIN " + perekidkaTable + " p ");
                sql.Append(" ON real_charge<>0 AND p.nzp_kvar=t.nzp_kvar AND p.nzp_supp=t.nzp_supp AND p.nzp_serv= t.nzp_serv AND p.nzp_kvar > 0 AND p.month_ = " + Month);
                if (Params.Contains((int)Fields.ChangeDate))
                { sql.Append(" AND p.date_rcl BETWEEN '" + new DateTime(Year, Month, 1) + "' AND '" + new DateTime(Year, Month, 1).AddMonths(1).AddDays(-1) + "' "); }

                if (StatusLS != (int)AccountStatus.All || Params.Contains((int)Fields.AccountStatus))
                { sql.Append(" LEFT JOIN " + pref + DBManager.sDataAliasRest + "prm_3 p3 "); }
                if (StatusLS != (int)AccountStatus.All)
                {
                    sql.Append(" ON p3.nzp=t.nzp_kvar AND p3.nzp_prm=51 AND p3.val_prm='" + StatusLS + "'" +
                                   " AND is_actual=1 AND dat_s<='" + new DateTime(Year, Month, 1).AddMonths(1).AddDays(-1) + "'" +
                                   " AND dat_po>='" + new DateTime(Year, Month, 1) + "'");
                }
                else if (Params.Contains((int)Fields.AccountStatus))
                {
                    sql.Append(" ON p3.nzp=t.nzp_kvar AND p3.nzp_prm=51 " +
                                   " AND p3.is_actual=1 AND p3.dat_s<='" + new DateTime(Year, Month, 1).AddMonths(1).AddDays(-1) + "'" +
                                   " AND p3.dat_po>='" + new DateTime(Year, Month, 1) + "'");
                }

                if (listLc)
                {
                    sql.Append(" INNER JOIN selected_kvars sk ON sk.nzp_kvar=t.nzp_kvar ");
                    if (Params.Contains((int)Fields.RevalPeriod) || Params.Contains((int)Fields.RevalSign) || Params.Contains((int)Fields.User) || Params.Contains((int)Fields.ChangeDate))
                        sql.Append(" AND p.nzp_kvar=sk.nzp_kvar ");

                }
                sql.Append(" WHERE t.nzp_serv>1 ");
                sql.Append(whereSupp);
                sql.Append(whereServ);
                sql.Append(typCh);
                sql.Append(grouper);
                ExecSQL(sql.ToString());
            }

            var sort = FillTmPTable();
            ExecSQL(" CREATE INDEX t_res_71_1_16_ndx ON t_res_71_1_16(num_ls, ls_st, nkvar, ndom, ulica,rajon, area, " +
                                    " town, geu, nzp_supp, nzp_serv, name_supp, service, nzp_type, type_, nzp_user, user_name, dat_, nzp_osn, osnovanie, " +
                                    " comment, nzp_reason, reason, period, prizn,pkod, ob, tarif) ");
            ExecSQL(DBManager.sUpdStat + " t_res_71_1_16 ");
            var dt = ExecSQLToTable(" SELECT num_ls, ls_st, nkvar, ndom, ulica,rajon, area, " +
                                    " town, geu, nzp_supp, nzp_serv, name_supp, service, nzp_type, type_, nzp_user, user_name, dat_, nzp_osn, osnovanie, " +
                                    " comment, nzp_reason, reason, period, prizn,pkod, ob, tarif, SUM(sum_) as sum_ FROM t_res_71_1_16 " +
                                    " GROUP BY num_ls, ls_st, nkvar, ndom, ulica,rajon, area, " +
                                    " town, geu, nzp_supp, nzp_serv, name_supp, service, nzp_type, type_, nzp_user, user_name, dat_, nzp_osn, osnovanie, " +
                                    " comment, nzp_reason, reason, period, prizn,pkod, ob, tarif ", 3000);
            var dv = new DataView(dt) { Sort = sort };
            dt = dv.ToTable();
            dt.TableName = "Q_master";

            var sql2 = new StringBuilder();
            sql2.Remove(0, sql2.Length);
            sql2.Append(" INSERT INTO t_title_71_1_16 values( ");
            sql2.Append(Params.Contains((int)Fields.Account) ? " 'ЛС' , " : " '' , ");
            sql2.Append(Params.Contains((int)Fields.AccountStatus) ? " 'Статус ЛС' , " : " '' , ");
            sql2.Append(Params.Contains((int)Fields.PayCode) ? " 'Платежный код' , " : " '' , ");
            sql2.Append(Params.Contains((int)Fields.Uk) ? " 'УК' , " : " '' , ");
            sql2.Append(Params.Contains((int)Fields.Geu) ? " 'ЖЭУ' , " : " '' , ");
            sql2.Append(Params.Contains((int)Fields.District) ? " 'Район' , " : " '' , ");
            sql2.Append(Params.Contains((int)Fields.Locality) ? " 'Населенный пункт' , " : " '' , ");
            sql2.Append(Params.Contains((int)Fields.Street) ? " 'Улица' , " : " '' , ");
            sql2.Append(Params.Contains((int)Fields.House) ? " 'Дом' , " : " '' , ");
            sql2.Append(Params.Contains((int)Fields.Appartment) ? " 'Квартира' , " : " '' , ");
            sql2.Append(Params.Contains((int)Fields.Contract) ? " 'Договор' , " : " '' , ");
            sql2.Append(Params.Contains((int)Fields.Service) ? " 'Услуга' , " : " '' , ");
            sql2.Append(Params.Contains((int)Fields.ChangeType) ? " 'Тип изменения' , " : " '' , ");
            sql2.Append(Params.Contains((int)Fields.ChangeSum) ? " 'Сумма изменения' , " : " '' , ");
            sql2.Append(Params.Contains((int)Fields.Tarif) ? " 'Тариф' , " : " '' , ");
            sql2.Append(Params.Contains((int)Fields.Volume) ? " 'Объем' , " : " '' , ");
            sql2.Append(Params.Contains((int)Fields.ChangeDate) ? " 'Дата изменения' , " : " '' , ");
            sql2.Append(Params.Contains((int)Fields.BaseDocument) ? " 'Документ-основание' , " : " '' , ");
            sql2.Append(Params.Contains((int)Fields.Comment) ? " 'Комментарий' , " : " '' , ");
            sql2.Append(Params.Contains((int)Fields.RevalSign) ? " 'Признак перерасчета' , " : " '' , ");
            sql2.Append(Params.Contains((int)Fields.RevalPeriod) ? " 'Период перерасчета' , " : " '' , ");
            sql2.Append(Params.Contains((int)Fields.User) ? " 'Пользователь, создавший изменение' , " : " '' , ");
            if (sql2.Length > 0) sql2.Remove(sql2.Length - 2, 2);
            sql2.Append(") ");
            ExecSQL(sql2.ToString());
            var dt1 = ExecSQLToTable(" SELECT * FROM t_title_71_1_16 ");
            dt1.TableName = "Q_master1";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables.Add(dt1);
            return ds;
        }

        private string FillTmPTable()
        {

            var sql = new StringBuilder();
            var grouper = new StringBuilder();
            var order = new StringBuilder();

            sql.Append(" INSERT INTO t_res_71_1_16( ");
            if (Params.Contains((int)Fields.ChangeType)) { sql.Append(" prizn, "); }
            if (Params.Contains((int)Fields.Account) || Params.Contains((int)Fields.PayCode) || Params.Contains((int)Fields.Uk) ||
                Params.Contains((int)Fields.Geu) || Params.Contains((int)Fields.District) || Params.Contains((int)Fields.Locality) ||
                Params.Contains((int)Fields.Street) || Params.Contains((int)Fields.House) || Params.Contains((int)Fields.Appartment)) { sql.Append(" nzp_kvar, "); }
            if (Params.Contains((int)Fields.AccountStatus)) { sql.Append(" ls_st, "); }
            if (Params.Contains((int)Fields.Contract)) { sql.Append(" nzp_supp, "); }
            if (Params.Contains((int)Fields.Service)) { sql.Append(" nzp_serv, "); }
            if (Params.Contains((int)Fields.ChangeType)) { sql.Append(" nzp_type, "); }
            if (Params.Contains((int)Fields.ChangeSum)) { sql.Append(" sum_, "); }
            if (Params.Contains((int)Fields.Tarif)) { sql.Append(" tarif, "); }
            if (Params.Contains((int)Fields.Volume)) { sql.Append(" ob, "); }
            if (Params.Contains((int)Fields.ChangeDate)) { sql.Append(" dat_, "); }
            if (Params.Contains((int)Fields.BaseDocument)) { sql.Append(" nzp_osn, "); }
            if (Params.Contains((int)Fields.Comment)) { sql.Append(" comment , "); }
            if (Params.Contains((int)Fields.RevalSign)) { sql.Append(" nzp_reason, "); }
            if (Params.Contains((int)Fields.RevalPeriod)) { sql.Append(" period, "); }
            if (Params.Contains((int)Fields.User)) { sql.Append(" nzp_user, "); }
            if (sql.Length > 0) sql.Remove(sql.Length - 2, 2);
            sql.Append(") ");
            sql.Append(" SELECT "); 
            if (Params.Contains((int)Fields.ChangeType)) { sql.Append(" prizn, "); grouper.Append(" prizn, "); }
            if (Params.Contains((int)Fields.Account) || Params.Contains((int)Fields.PayCode) || Params.Contains((int)Fields.Uk) ||
                Params.Contains((int)Fields.Geu) || Params.Contains((int)Fields.District) || Params.Contains((int)Fields.Locality) ||
                Params.Contains((int)Fields.Street) || Params.Contains((int)Fields.House) || Params.Contains((int)Fields.Appartment)) { sql.Append(" nzp_kvar, "); grouper.Append(" nzp_kvar, "); }
            if (Params.Contains((int)Fields.AccountStatus)) { sql.Append(" ls_st, "); grouper.Append(" ls_st, "); order.Append(" ls_st, "); }
            if (Params.Contains((int)Fields.Contract)) { sql.Append(" nzp_supp, "); grouper.Append(" nzp_supp, "); order.Append(" name_supp, "); }
            if (Params.Contains((int)Fields.Service)) { sql.Append(" nzp_serv, "); grouper.Append(" nzp_serv, "); order.Append(" service, "); }
            if (Params.Contains((int)Fields.ChangeType)) { sql.Append(" nzp_type, "); grouper.Append(" nzp_type, "); }
            if (Params.Contains((int)Fields.ChangeSum)) { sql.Append(" SUM(sum_), "); }
            if (Params.Contains((int)Fields.Tarif)) { sql.Append(" tarif, "); grouper.Append(" tarif, "); order.Append(" tarif, "); }
            if (Params.Contains((int)Fields.Volume)) { sql.Append(" ob, "); grouper.Append(" ob, "); }
            if (Params.Contains((int)Fields.ChangeDate)) { sql.Append(" dat_, "); grouper.Append(" dat_, "); order.Append(" dat_, "); }
            if (Params.Contains((int)Fields.BaseDocument)) { sql.Append(" nzp_osn, "); grouper.Append(" nzp_osn, "); }
            if (Params.Contains((int)Fields.Comment)) { sql.Append(" comment , "); grouper.Append(" comment, "); }
            if (Params.Contains((int)Fields.RevalSign)) { sql.Append(" nzp_reason, "); grouper.Append(" nzp_reason, "); }
            if (Params.Contains((int)Fields.RevalPeriod)) { sql.Append(" period, "); grouper.Append(" period, "); order.Append(" period, "); }
            if (Params.Contains((int)Fields.User)) { sql.Append(" nzp_user, "); grouper.Append(" nzp_user, "); }
            if (sql.Length > 0) sql.Remove(sql.Length - 2, 2);
            sql.Append(" FROM t_71_1_16 ");

            if (grouper.Length > 0) { grouper.Remove(grouper.Length - 2, 2); sql.Append(" GROUP BY " + grouper); }
            if (order.Length > 0) order.Remove(order.Length - 2, 2);

            if (grouper.Length > 0) ExecSQL(" create index svod_index on t_71_1_16(" + grouper + ") ");
            ExecSQL(DBManager.sUpdStat + " t_71_1_16 ");
            ExecSQL(sql.ToString());



            if (Params.Contains((int)Fields.Account))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_16 set num_ls = (" +
                           " SELECT num_ls FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a " +
                           " WHERE a.nzp_kvar = t_res_71_1_16.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains((int)Fields.PayCode))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_16 set pkod = (" +
                           " SELECT pkod FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a " +
                           " WHERE a.nzp_kvar = t_res_71_1_16.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains((int)Fields.Uk))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_16 set area = (" +
                           " SELECT area FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a, " +
                           ReportParams.Pref + DBManager.sDataAliasRest + "s_area s " +
                           " WHERE a.nzp_kvar = t_res_71_1_16.nzp_kvar AND s.nzp_area=a.nzp_area) ");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains((int)Fields.Geu))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_16 set geu = (" +
                           " SELECT geu FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a, " +
                           ReportParams.Pref + DBManager.sDataAliasRest + "s_geu s " +
                           " WHERE a.nzp_kvar = t_res_71_1_16.nzp_kvar AND s.nzp_geu=a.nzp_geu) ");
                ExecSQL(sql.ToString());
            }


            if (Params.Contains((int)Fields.District))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_16 set rajon = (" +
                           " SELECT town FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a, " +
                           ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                           ReportParams.Pref + DBManager.sDataAliasRest + "s_town s  " +
                           " WHERE a.nzp_kvar = t_res_71_1_16.nzp_kvar AND a.nzp_dom=d.nzp_dom AND s.nzp_town=d.nzp_town) ");
                ExecSQL(sql.ToString());
            }


            if (Params.Contains((int)Fields.Locality))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_16 set town  = (" +
                           " SELECT rajon FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a, " +
                           ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                           ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon s  " +
                           " WHERE  a.nzp_kvar = t_res_71_1_16.nzp_kvar AND a.nzp_dom=d.nzp_dom AND s.nzp_raj=d.nzp_raj) ");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains((int)Fields.Street))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_16 set ulica = (" +
                           " SELECT ulica FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a, " +
                           ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                           ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica s  " +
                           " WHERE  a.nzp_kvar = t_res_71_1_16.nzp_kvar AND a.nzp_dom=d.nzp_dom AND s.nzp_ul=d.nzp_ul) ");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains((int)Fields.House))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_16 set ndom = (" +
                           " SELECT ndom FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a, " +
                           ReportParams.Pref + DBManager.sDataAliasRest + "dom d " +
                           " WHERE a.nzp_kvar = t_res_71_1_16.nzp_kvar AND a.nzp_dom=d.nzp_dom) ");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains((int)Fields.Appartment))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_16 set nkvar = (" +
                           " SELECT nkvar FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a " +
                           " WHERE a.nzp_kvar = t_res_71_1_16.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains((int)Fields.Contract))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_16 set name_supp = (" +
                           " SELECT name_supp FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier a " +
                           " WHERE a.nzp_supp = t_res_71_1_16.nzp_supp) ");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains((int)Fields.Service))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_16 set service = (" +
                           " SELECT service FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "services a " +
                           " WHERE a.nzp_serv =t_res_71_1_16.nzp_serv) ");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains((int)Fields.ChangeType))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_16 set type_ = (" +
                           " SELECT typename FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_typercl a " +
                           " WHERE a.type_rcl =t_res_71_1_16.nzp_type) " +
                           " WHERE prizn=0");
                ExecSQL(sql.ToString());

                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_16 set type_ = 'перерасчет'  WHERE prizn=1");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains((int)Fields.BaseDocument))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_16 set osnovanie = (" +
                           " SELECT comment FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "document_base a " +
                           " WHERE a.nzp_doc_base =t_res_71_1_16.nzp_osn) ");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains((int)Fields.RevalSign))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_16 set reason = (" +
                           " SELECT reason FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_reason a " +
                           " WHERE a.nzp_reason =t_res_71_1_16.nzp_reason) ");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains((int)Fields.User))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_16 set user_name = (" +
                           " SELECT comment FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "users a " +
                           " WHERE a.nzp_user =t_res_71_1_16.nzp_user) ");
                ExecSQL(sql.ToString());
            }

            return order.ToString();
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp(string fieldPref)
        {
            string whereSupp = String.Empty;
            if (BankSupplier != null)
            {
                if (BankSupplier.Suppliers != null)
                {

                    string supp = string.Empty;
                    supp = BankSupplier.Suppliers.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                    whereSupp += " AND nzp_payer_supp in (" + supp.TrimEnd(',') + ")";
                }

                if (BankSupplier.Principals != null)
                {

                    string supp = string.Empty;
                    supp = BankSupplier.Principals.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                    whereSupp += " AND nzp_payer_princip in (" + supp.TrimEnd(',') + ")";
                }

                if (BankSupplier.Agents != null)
                {

                    string supp = string.Empty;
                    supp = BankSupplier.Agents.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                    whereSupp += " AND nzp_payer_agent in (" + supp.TrimEnd(',') + ")";
                }
            }

            string oldsupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);

            whereSupp = whereSupp.TrimEnd(',');

            if (!String.IsNullOrEmpty(whereSupp) || !String.IsNullOrEmpty(oldsupp))
            {
                if (!String.IsNullOrEmpty(oldsupp))
                    whereSupp += " AND nzp_supp in (" + oldsupp + ")";

                //Поставщики
                SupplierHeader = String.Empty;
                string sql = " SELECT name_supp FROM " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                             " WHERE nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += "(" + dr["name_supp"].ToString().Trim() + "), ";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',', ' ');
            }
            return " AND " + fieldPref + " IN (SELECT nzp_supp FROM " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " WHERE nzp_supp>0 " + (String.IsNullOrEmpty(whereSupp) ? string.Empty : whereSupp) + ") ";
        }


        private string SetPrefBanks()
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
            string whereWpsql = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            PrefBanks = new List<string>();
            if (!string.IsNullOrEmpty(whereWpsql))
            {
                TerritoryHeader = String.Empty;
                string sql = " SELECT point,bd_kernel FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWpsql;
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
                string sql = " SELECT bd_kernel FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 AND flag=2";
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    PrefBanks.Add(row["bd_kernel"].ToString().Trim());
                }
            }
            string whereWpRes = !String.IsNullOrEmpty(whereWp)
                ? "AND pl.num_ls in (SELECT num_ls FROM " + ReportParams.Pref + DBManager.sKernelAliasRest +
                  "s_point s,"
                  + ((ReportParams.CurrentReportKind == ReportKind.ListLC)
                      ? "selected_kvars"
                      : ReportParams.Pref + DBManager.sDataAliasRest + "kvar kv ") +
                  "where kv.nzp_wp=s.nzp_wp AND s.nzp_wp in ( " + whereWp + ") )"
                : String.Empty;
            return whereWpRes;
        }

        private string GetTC()
        {
            var result = String.Empty;
            if (ChangeType != null)
            {
                result = ChangeType.Aggregate(result, (current, nzpT) => current + (nzpT + ","));
            }
            else
            {
                return "";
            }
            result = result.TrimEnd(',');
            if (ChangeType.Contains((int)ChangeTypes.Other))
            {
                string sel = " SELECT type_rcl FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_typercl a WHERE type_rcl not in (1,2,20,63,100,101,102,103,104,105,106,107,114,115,116,163)";
                result = !String.IsNullOrEmpty(result)
                    ? " AND (type_rcl in (" + result + ") or type_rcl in (" + sel + "))"
                    : " AND type_rcl in (" + sel + ")";
            }
            else
            {
                result = !String.IsNullOrEmpty(result) ? " AND type_rcl in (" + result + ")" : String.Empty;
            }

            return result;
        }

        private string GetWhereServ()
        {
            var result = String.Empty;
            if (Services != null)
            {
                result = Services.Aggregate(result, (current, nzpServ) => current + (nzpServ + ","));
            }
            else
            {
                result = ReportParams.GetRolesCondition(Constants.role_sql_serv);
            }
            result = result.TrimEnd(',');
            result = !String.IsNullOrEmpty(result) ? " AND t.nzp_serv in (" + result + ")" : String.Empty;
            if (!String.IsNullOrEmpty(result))
            {
                string sql = " SELECT service " +
                             " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "services t " +
                             " WHERE t.nzp_serv > 1 " + result;
                DataTable serv = ExecSQLToTable(sql);
                foreach (DataRow dr in serv.Rows)
                {
                    ServicesHeader += dr["service"].ToString().Trim() + ", ";
                }
                ServicesHeader = ServicesHeader.TrimEnd(',', ' ');
            }
            return result;
        }

        protected override void CreateTempTable()
        {
            ExecSQL(" CREATE TEMP TABLE t_res_71_1_16( " +
                    " num_ls INTEGER, " +
                    " ls_st CHAR(1)," +
                    " nzp_kvar INTEGER, " +
                    " nkvar VARCHAR," +
                    " ndom VARCHAR," +
                    " ulica VARCHAR," +
                    " rajon VARCHAR," +
                    " area VARCHAR," +
                    " town VARCHAR," +
                    " geu VARCHAR," +
                    " nzp_supp INTEGER, " +
                    " nzp_serv INTEGER, " +
                    " name_supp VARCHAR," +
                    " service VARCHAR," +
                    " nzp_type INTEGER, " +
                    " type_ VARCHAR," +
                    " nzp_user INTEGER, " +
                    " user_name VARCHAR," +
                    " dat_ DATE, " +
                    " nzp_osn INTEGER, " +
                    " osnovanie VARCHAR, " +
                    " comment VARCHAR, " +
                    " nzp_reason INTEGER, " +
                    " reason VARCHAR, " +
                    " period VARCHAR, " +
                    " prizn integer, " + //0 - perekidca, 1 - must calc
                    " pkod " + DBManager.sDecimalType + "(13,0)," +
                    " ob " + DBManager.sDecimalType + "(14,3)," +
                    " tarif " + DBManager.sDecimalType + "(14,3)," +
                    " sum_ " + DBManager.sDecimalType + "(14,2) )"
                    + DBManager.sUnlogTempTable);

            ExecSQL(" CREATE TEMP TABLE t_title_71_1_16( " +
                    " num_ls VARCHAR, " +
                    " ls_st VARCHAR, " +
                    " pkod VARCHAR, " +
                    " area VARCHAR, " +
                    " geu VARCHAR, " +
                    " rajon VARCHAR, " +
                    " nas_p VARCHAR, " +
                    " ulica VARCHAR, " +
                    " ndom VARCHAR, " +
                    " nkvar VARCHAR, " +
                    " name_supp VARCHAR, " +
                    " service VARCHAR, " +
                    " type_ VARCHAR, " +
                    " sum_ VARCHAR, " +
                    " tarif VARCHAR, " +
                    " ob VARCHAR, " +
                    " dat VARCHAR, " +
                    " osnovanie VARCHAR, " +
                    " comment VARCHAR, " +
                    " reason VARCHAR, " +
                    " period VARCHAR, " +
                    " user_name VARCHAR) "
                    + DBManager.sUnlogTempTable);

            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                ExecSQL(" CREATE TEMP TABLE selected_kvars(nzp_kvar integer) " + DBManager.sUnlogTempTable);
            }

            var sqlc = new StringBuilder();
            sqlc.Append(" CREATE TEMP TABLE t_71_1_16( ");
            if (Params.Contains((int)Fields.ChangeType)) { sqlc.Append(" prizn integer, "); }
            if (Params.Contains((int)Fields.Account) || Params.Contains((int)Fields.PayCode) || Params.Contains((int)Fields.Uk) ||
                Params.Contains((int)Fields.Geu) || Params.Contains((int)Fields.District) || Params.Contains((int)Fields.Locality) ||
                Params.Contains((int)Fields.Street) || Params.Contains((int)Fields.House) || Params.Contains((int)Fields.Appartment))
            { sqlc.Append(" nzp_kvar integer, "); }
            if (Params.Contains((int)Fields.AccountStatus)) { sqlc.Append(" ls_st char(1), "); }
            if (Params.Contains((int)Fields.Contract)) { sqlc.Append(" nzp_supp integer, "); }
            if (Params.Contains((int)Fields.Service)) { sqlc.Append(" nzp_serv integer, "); }
            if (Params.Contains((int)Fields.ChangeType)) { sqlc.Append(" nzp_type  integer, "); }
            if (Params.Contains((int)Fields.ChangeSum)) { sqlc.Append(" sum_ " + DBManager.sDecimalType + "(14, 2), "); }
            if (Params.Contains((int)Fields.Tarif)) { sqlc.Append(" tarif " + DBManager.sDecimalType + "(14,3),"); }
            if (Params.Contains((int)Fields.Volume)) { sqlc.Append(" ob " + DBManager.sDecimalType + "(14,3),"); }
            if (Params.Contains((int)Fields.ChangeDate)) { sqlc.Append(" dat_ date, "); }
            if (Params.Contains((int)Fields.BaseDocument)) { sqlc.Append(" nzp_osn integer, "); }
            if (Params.Contains((int)Fields.Comment)) { sqlc.Append(" comment VARCHAR, "); }
            if (Params.Contains((int)Fields.RevalSign)) { sqlc.Append(" nzp_reason integer, "); }
            if (Params.Contains((int)Fields.RevalPeriod)) { sqlc.Append(" period VARCHAR, "); }
            if (Params.Contains((int)Fields.User)) { sqlc.Append(" nzp_user integer, "); }
            ExecSQL(sqlc.ToString().Trim(' ', ',') + ") ");
        }

        protected override void DropTempTable()
        {
            try
            {
                ExecSQL(" DROP TABLE t_71_1_16 ");
                ExecSQL(" DROP TABLE t_res_71_1_16 ");
                ExecSQL(" DROP TABLE t_title_71_1_16 ");
                if (ReportParams.CurrentReportKind == ReportKind.ListLC)
                {
                    ExecSQL(" drop table selected_kvars " + DBManager.sUnlogTempTable);
                }
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Отчет 'Генератор по перерасчетам/изменеиям сальдо' " + e.Message, MonitorLog.typelog.Error, false);
            }
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
                            " INSERT INTO selected_kvars (nzp_kvar) SELECT nzp_kvar FROM " + tSpls;
                        ExecSQL(sql);
                        ExecSQL("create index ix_sel_kvar_09 on selected_kvars(nzp_kvar)");
                        ExecSQL(DBManager.sUpdStat + " selected_kvars");
                        return true;
                    }
                }
            }
            return false;
        }


    }
}