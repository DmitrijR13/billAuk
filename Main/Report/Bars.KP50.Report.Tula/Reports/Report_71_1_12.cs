using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web.Configuration;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Tula.Properties;
using Castle.Components.DictionaryAdapter;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.EPaspXsd;
using STCLINE.KP50.Global;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.Interfaces;


namespace Bars.KP50.Report.Tula.Reports
{
    
    /// <summary>
    /// Сводный отчет по платежам для Тулы
    /// </summary>
    public class Report71001012 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71. Генератор по платежам"; }
        }

        public override string Description
        {
            get { return "Генератор по платежам"; }
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
            get { return Resources.Report_71_1_12; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
        }

        #region Параметры
        private int PeriodType { get; set; }

        /// <summary> с расчетного дня </summary>
        protected DateTime DatS { get; set; }

        /// <summary> по расчетный день </summary>
        protected DateTime DatPo { get; set; }
        
        /// <summary>Поставщики</summary>
        private string SupplierHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Заголовок место формирования</summary>
        protected string PlaceHeader { get; set; }

        /// <summary>Место формирования</summary>
        protected int FormingPlace { get; set; }

        /// <summary>Код квитанции</summary>
        protected List<int> KodSum { get; set; }

        /// <summary>Параметры</summary>
        private List<long> Params { get; set; }

        /// <summary>Статус пачки</summary>
        private int StatusPack { get; set; }

        /// <summary>Тип пачки</summary>
        private int TypePack { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }

        private bool RowCount { get; set; }

        private List<long> Services { get; set; }
        /// <summary>Заголовок Услуг</summary>       
        protected string ServicesHeader { get; set; }
        /// <summary>Заголовок тип периода </summary>       
        protected string typePeriod { get; set; }

        /// <summary>Список префиксов банков в БД</summary>  
        private List<string> PrefBanks { get; set; } 

        #endregion

        public override List<UserParam> GetUserParams()
        {
            DateTime datS=DateTime.Now;
            DateTime datPo=DateTime.Now;

            return new List<UserParam>
            {                
                new ComboBoxParameter(false)
                {
                    Name = "Тип периода",
                    Code = "PeriodType",
                    Value = 2,
                    Require = true,
                    StoreData = new List<object>
                    {
                        new { Id = 2, Name = "Дата распределения пачки оплат" },//dat_uchet
                        new { Id = 1, Name = "Дата внесения/загрузки пачки оплат" },//dat_vvod 
                        new { Id = 4, Name = "Дата распределения квитанции" },//dat_uchet                   
                        new { Id = 3, Name = "Дата внесения/загрузки квитанции" }//dat_vvod  
                    },
                },

                new PeriodParameter(datS, datPo),
                new BankSupplierParameter(),
                new FormingPlaceParameter{Name = "Место формирования"},

                new ComboBoxParameter(true)
                {
                    Name = "Код квитанции",
                    Code = "KodSum",
                    Value = 1,
                    Require = false,
                    StoreData = new List<object>
                    {
                        new { Id = 1, Name = "Ежемесячный (33)" },
                        new { Id = 2, Name = "Оплата контрагентов (49)" },
                        new { Id = 3, Name = "Оплата поставщиков (50)" },
                        new { Id = 4, Name = "Предыдущие ситемы (40,35)" }
                    }
                },

                new ComboBoxParameter(false)
                {
                    Name = "Статус пачки",
                    Code = "StatusPack",
                    Value = 1,
                    Require = true,
                    StoreData = new List<object>
                    {
                        new { Id = 1, Name = "Все" },
                        new { Id = 2, Name = "Распределена" },
                        new { Id = 3, Name = "Не распределена" },
                        new { Id = 4, Name = "Распределена с ошибками" }
                    }
                },

                new ComboBoxParameter(false)
                {
                    Name = "Тип пачки",
                    Code = "TypePack",
                    Value = 1,
                    Require = true,
                    StoreData = new List<object>
                    {
                        new { Id = 1, Name = "Все" },
                        new { Id = 10, Name = "Оплаты на счет РЦ" },
                        new { Id = 20, Name = "Оплаты УК и ПУ" },
                        new { Id = 21, Name = "Переплаты" }
                    }
                },
                
                new ServiceParameter(),

                new ComboBoxParameter(true) {
                    Name = "Параметры отчета", 
                    Code = "Params",
                    Value = 1,
                    Require = true,
                    StoreData = new List<object> {
                        new { Id = 1, Name = "N пачки"},
                        new { Id = 2, Name = "Статус пачки"},
                        new { Id = 3, Name = "Дата пачки"},
                        new { Id = 16, Name = "Дата распределения"},
                        new { Id = 4, Name = "Имя файла"},
                        new { Id = 5, Name = "Место формирования"},
                        new { Id = 6, Name = "Код квитанции"},
                        new { Id = 7, Name = "Территория"},
                        new { Id = 8, Name = "Лицевой счет"},
                        new { Id = 9, Name = "Платежный код"},
                        new { Id = 10, Name = "Дата оплаты"},
                        new { Id = 11, Name = "Операционный день"},
                        new { Id = 12, Name = "Поставщик"},
                        new { Id = 13, Name = "Распределено"},
                        new { Id = 14, Name = "Не распределено"},
                        new { Id = 15, Name = "Услуга"},
                    }
                },
            };
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            string statusPack = string.Empty;
            string typePack = string.Empty;
           
                switch (StatusPack)
                {
                    case 2:
                        statusPack = "распределена";
                        break;
                    case 3:
                        statusPack = "не распределена";
                        break;
                    case 4:
                        statusPack = "распределена с ошибками";
                        break;
                    case 1:
                        statusPack = "все";
                        break;
                }
            

            switch (PeriodType)
                {
                    case 1:
                        typePeriod = "дате внесения/загрузки пачки оплат";
                        break;
                    case 2:
                        typePeriod = "дате распределения пачки оплат";
                        break;
                    case 3:
                        typePeriod = "дате внесения/загрузки квитанции";
                        break;
                    case 4:
                        typePeriod = "дате распределения квитанции";
                        break;
                }

            switch (TypePack)
            {
                case 10:
                    typePack = "Оплаты на счет РЦ";
                    break;
                case 20:
                    typePack = "Оплаты УК и ПУ";
                    break;
                case 21:
                    typePack = "Переплаты";
                    break;
            }
            
            
            string kodSum = string.Empty;
            if (KodSum != null)
            {
                kodSum  = KodSum.Contains(1) ? "ежемесячный, " : string.Empty;
                kodSum += KodSum.Contains(2) ? "оплата контрагентов, " : string.Empty;
                kodSum += KodSum.Contains(3) ? "оплата поставщиков, " : string.Empty;
                kodSum += KodSum.Contains(4) ? "предыдущие системы, " : string.Empty;
                kodSum = kodSum.TrimEnd(',', ' ');
            }

            string period = "период с " + DatS.ToShortDateString() + " по " + DatPo.ToShortDateString();

            report.SetParameterValue("time", DateTime.Now.ToLongTimeString());
            report.SetParameterValue("dat", DateTime.Now.ToShortDateString());
            report.SetParameterValue("period", period);
            report.SetParameterValue("typePeriod", typePeriod);

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(ServicesHeader) ? "Услуги: " + ServicesHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(PlaceHeader) ? "Место формирования: " + PlaceHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(kodSum) ? "Код квитанции: " + kodSum  + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(statusPack) ? "Статус пачки: " + statusPack + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(statusPack) ? "Тип пачки: " + typePack + "\n" : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);


            string limit = ReportParams.ExportFormat == ExportFormat.Excel2007 ? "40000" : "100000";

            report.SetParameterValue("excel",
                RowCount
                    ? "Выборка записей ограничена первыми " + limit + " строками. Выберите другой формат экспортируемого файла, либо поставьте другие ограничения для отчета"
                    : "");
        }

        protected override void PrepareParams()
        {
            Services = UserParamValues["Services"].GetValue<List<long>>();
            Params = UserParamValues["Params"].GetValue<List<long>>();
            KodSum= UserParamValues["KodSum"].GetValue<List<int>>();
            StatusPack = UserParamValues["StatusPack"].GetValue<int>();
            TypePack = UserParamValues["TypePack"].GetValue<int>();
            PeriodType = UserParamValues["PeriodType"].GetValue<int>();
            FormingPlace = UserParamValues["Place"].GetValue<int>();
            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
        }

        public override DataSet GetData()
        {
           
            var period = UserParamValues["Period"].GetValue<string>();
            DateTime d1;
            DateTime d2;
            PeriodParameter.GetValues(period, out d1, out d2);
            DatS = d1;
            DatPo = d2;
            string whereSupp = GetWhereSupp("g.nzp_supp");
            string whereWp = GetwhereWp();
            string formingPl = GetFormingPlace(),
                whereServ=GetWhereServ();

            string kodSum = string.Empty;
            if (KodSum != null)
            {
                kodSum  = KodSum.Contains(1) ? "33, " : string.Empty;
                kodSum += KodSum.Contains(2) ? "49, " : string.Empty;
                kodSum += KodSum.Contains(3) ? "50, " : string.Empty;
                kodSum += KodSum.Contains(4) ? "40, " : string.Empty;
                kodSum = kodSum.TrimEnd(',', ' ');
            }
            var sql = new StringBuilder();
            var sqlc = new StringBuilder();
            sqlc.Append(" create temp table t_payments_71_1_11( ");
            if (Params.Contains(1)) { sqlc.Append(" num_pack CHAR(10), "); }
            if (Params.Contains(2)) { sqlc.Append(" \"flag\" integer, "); }
            if (Params.Contains(3)) { sqlc.Append(" dat_pack DATE, "); }
            if (Params.Contains(16)) { sqlc.Append(" dat_rasp DATE, "); }
            if (Params.Contains(4)) { sqlc.Append(" file_name CHAR(200), "); }
            if (Params.Contains(5)) { sqlc.Append(" nzp_bank integer, "); }//nzp_payer sp.payer
            if (Params.Contains(6)) { sqlc.Append(" kod_sum integer, "); }
            if (Params.Contains(8) || Params.Contains(7)) { sqlc.Append(" num_ls integer, "); }
            if (Params.Contains(9)) { sqlc.Append(" pkod " + DBManager.sDecimalType + "(13,0), "); }
            if (Params.Contains(10)) { sqlc.Append(" dat_vvod DATE, "); }
            if (Params.Contains(11)) { sqlc.Append(" dat_uchet DATE, "); }
            if (Params.Contains(12)) { sqlc.Append(" nzp_supp integer, "); }//name_supp
            if (Params.Contains(13)) { sqlc.Append(" sum_raspr " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(14)) { sqlc.Append(" sum_neraspr " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(15)) { sqlc.Append(" nzp_serv integer, "); }

            if (sqlc.Length > 0) sqlc.Remove(sqlc.Length - 2, 2);
            sqlc.Append(") ");
            ExecSQL(sqlc.ToString());
            var isLC = GetSelectedKvars();
            
            #region Запрос для не распределенных оплат
            sql.Clear();
                    sql.Append(" insert into t_payments_71_1_11( ");
                    if (Params.Contains(1)) { sql.Append(" num_pack, "); }
                    if (Params.Contains(2)) { sql.Append(" \"flag\", "); }
                    if (Params.Contains(3)) { sql.Append(" dat_pack, "); }
                    if (Params.Contains(16)) { sql.Append(" dat_rasp, "); }
                    if (Params.Contains(4)) { sql.Append(" file_name, "); }
                    if (Params.Contains(5)) { sql.Append(" nzp_bank, ");  }
                    if (Params.Contains(6)) { sql.Append(" kod_sum, "); }
                    if (Params.Contains(8) || Params.Contains(7)) { sql.Append(" num_ls, "); }
                    if (Params.Contains(9)) { sql.Append(" pkod, "); }
                    if (Params.Contains(10)) { sql.Append(" dat_vvod, "); }
                    if (Params.Contains(11)) { sql.Append(" dat_uchet, "); }
                    if (Params.Contains(12)) { sql.Append(" nzp_supp, ");  }
                    if (Params.Contains(13)) { sql.Append(" sum_raspr, "); }
                    if (Params.Contains(14)) { sql.Append(" sum_neraspr, ");}
                    if (Params.Contains(15)) { sql.Append(" nzp_serv, "); }
                    if (sql.Length > 0) sql.Remove(sql.Length - 2, 2);
                    sql.Append(") ");
                    sql.Append(" select ");
                    if (Params.Contains(1)) { sql.Append(" num_pack, "); }
                    if (Params.Contains(2)) { sql.Append(" pp.\"flag\", ");}
                    if (Params.Contains(3)) { sql.Append(" dat_pack, "); }
                    if (Params.Contains(16)) { sql.Append(" pp.dat_uchet, "); }
                    if (Params.Contains(4)) { sql.Append(" file_name, "); }
                    if (Params.Contains(5)) { sql.Append(" pp.nzp_bank, "); }
                    if (Params.Contains(6)) { sql.Append(" pl.kod_sum, ");  }
                    if (Params.Contains(8) || Params.Contains(7)) {  sql.Append(" pl.num_ls, "); }
                    if (Params.Contains(9)) { sql.Append(" sk.pkod, "); }
                    if (Params.Contains(10)) { sql.Append(" pl.dat_vvod, "); }
                    if (Params.Contains(11)) { sql.Append(" pl.dat_uchet, "); }
                    if (Params.Contains(12)) { sql.Append(" pl.nzp_supp, ");}
                    if (Params.Contains(13)) { sql.Append(" 0 as sum_raspr, "); }
                    if (Params.Contains(14)) { sql.Append(" g_sum_ls, "); }
                    if (Params.Contains(15)) { sql.Append(" -1, "); }
                    if (sql.Length > 0) sql.Remove(sql.Length - 2, 2);
            sql.Append(" from {0} pp, "); // 0 - пачка
            sql.Append(" {1} pl, ");       // 1 - квитанция
            if (isLC)
                sql.Append(" selected_kvars sk ");
            else 
                sql.Append(" {2} sk ");    //2-квартиры  список или все
            sql.Append(
                " where pp.nzp_pack=pl.nzp_pack ");
            sql.Append(" AND pl.dat_uchet is null ");
            sql.Append(" and sk.num_ls= pl.num_ls ");
                    if (StatusPack == 2)
                    {
                        sql.Append(" AND \"flag\"= 21 ");
                    }
                    if (StatusPack == 4)
                    {
                        sql.Append(" AND \"flag\"= 22 ");
                    }
                    if (StatusPack == 3)
                    {
                        sql.Append(" AND \"flag\"= 23 ");
                    }
                    if (kodSum != string.Empty)
                    {
                        sql.Append(" AND pl.kod_sum in ( " + kodSum + ") ");
                    }
                    sql.Append(whereWp);
                    sql.Append(formingPl);
                    switch (PeriodType)
                    {
                        case 1:
                            sql.Append(" and pp.dat_vvod >= date(" + STCLINE.KP50.Global.Utils.EStrNull(DatS.ToShortDateString()) + ") and pp.dat_vvod <= date(" + STCLINE.KP50.Global.Utils.EStrNull(DatPo.ToShortDateString()) + ")");
                            break;
                        case 2:
                            sql.Append(" and coalesce(pp.dat_uchet,date(" +
                            STCLINE.KP50.Global.Utils.EStrNull(Points.DateOper.ToShortDateString()) + ")) >= date(" +
                            STCLINE.KP50.Global.Utils.EStrNull(DatS.ToShortDateString()) + ")" +
                            " and coalesce(pp.dat_uchet,date(" +
                            STCLINE.KP50.Global.Utils.EStrNull(Points.DateOper.ToShortDateString()) + ")) <= date(" +
                            STCLINE.KP50.Global.Utils.EStrNull(DatPo.ToShortDateString()) + ")"); 
                            break;
                        case 3:
                            sql.Append(" AND  pl.dat_vvod >= '" + DatS.ToShortDateString() + "' AND pl.dat_vvod <=  '" +
                                       DatPo.ToShortDateString() + "'");
                            break;
                        case 4:
                            sql.Append(" and coalesce(pl.dat_uchet,date(" +
                            STCLINE.KP50.Global.Utils.EStrNull(Points.DateOper.ToShortDateString()) + ")) >= date(" +
                            STCLINE.KP50.Global.Utils.EStrNull(DatS.ToShortDateString()) + ")" +
                            " and coalesce(pl.dat_uchet,date(" +
                            STCLINE.KP50.Global.Utils.EStrNull(Points.DateOper.ToShortDateString()) + ")) <= date(" +
                            STCLINE.KP50.Global.Utils.EStrNull(DatPo.ToShortDateString()) + ")"); 
                            break;
                    }
                        if (TypePack != 1) { sql.Append(" and pp.pack_type="+TypePack); }
                         
            string sqlInsNRasp = sql.ToString();

        #endregion  

            #region Запрос для распределенных оплат
            sql.Clear();
            sql.Append(" insert into t_payments_71_1_11( ");
            if (Params.Contains(1)) { sql.Append(" num_pack, "); }
            if (Params.Contains(2)) { sql.Append(" \"flag\", "); }
            if (Params.Contains(3)) { sql.Append(" dat_pack, "); }
            if (Params.Contains(16)) { sql.Append(" dat_rasp, "); }
            if (Params.Contains(4)) { sql.Append(" file_name, "); }
            if (Params.Contains(5)) { sql.Append(" nzp_bank, "); }
            if (Params.Contains(6)) { sql.Append(" kod_sum, "); }
            if (Params.Contains(8) || Params.Contains(7)) { sql.Append(" num_ls, "); }
            if (Params.Contains(9)) { sql.Append(" pkod, "); }
            if (Params.Contains(10)) { sql.Append(" dat_vvod, "); }
            if (Params.Contains(11)) { sql.Append(" dat_uchet, "); }
            if (Params.Contains(12)) { sql.Append(" nzp_supp, "); }
            if (Params.Contains(13)) { sql.Append(" sum_raspr, "); }
            if (Params.Contains(14)) { sql.Append(" sum_neraspr, "); }
            if (Params.Contains(15)) { sql.Append(" nzp_serv, "); }
            if (sql.Length > 0) sql.Remove(sql.Length - 2, 2);
            sql.Append(") ");
            sql.Append(" select ");
            if (Params.Contains(1)) { sql.Append(" num_pack, "); }
            if (Params.Contains(2)) { sql.Append(" pp.\"flag\", "); }
            if (Params.Contains(3)) { sql.Append(" dat_pack, "); }
            if (Params.Contains(16)) { sql.Append(" pp.dat_uchet, "); }
            if (Params.Contains(4)) { sql.Append(" file_name, "); }
            if (Params.Contains(5)) { sql.Append(" pp.nzp_bank, "); }
            if (Params.Contains(6)) { sql.Append(" pl.kod_sum, "); }
            if (Params.Contains(8) || Params.Contains(7)) { sql.Append(" pl.num_ls, "); }
            if (Params.Contains(9)) { sql.Append(" sk.pkod, "); }
            if (Params.Contains(10)) { sql.Append(" pl.dat_vvod, "); }
            if (Params.Contains(11)) { sql.Append(" pl.dat_uchet, "); }
            if (Params.Contains(12)) { sql.Append(" g.nzp_supp, "); }
            if (Params.Contains(13)) { sql.Append(" g.sum_prih, "); }
            if (Params.Contains(14)) { sql.Append(" 0 as sum_neraspr, "); }
            if (Params.Contains(15)) { sql.Append(" nzp_serv, "); }
            if (sql.Length > 0) sql.Remove(sql.Length - 2, 2);
            sql.Append(" from {0} pp, "); // 0 - пачка
            sql.Append(" {1} pl, ");       // 1 - квитанция
            sql.Append(" {2} g  ");    //  2 - распределение 33-fn_supplier, 
            if (isLC)
                sql.Append(" , selected_kvars sk ");
            else
                sql.Append(" , {3} sk ");    //3-квартиры  список или все
            sql.Append(
                " where pp.nzp_pack=pl.nzp_pack ");
            sql.Append(" AND pl.nzp_pack_ls=g.nzp_pack_ls AND g.kod_sum=pl.kod_sum ");
            sql.Append(" and sk.num_ls = pl.num_ls ");
            if (StatusPack == 2)
            {
                sql.Append(" AND \"flag\"= 21 ");
            }
            if (StatusPack == 4)
            {
                sql.Append(" AND \"flag\"= 22 ");
            }
            if (StatusPack == 3)
            {
                sql.Append(" AND \"flag\"= 23 ");
            }
            if (kodSum != string.Empty)
            {
                sql.Append(" AND pl.kod_sum in ( " + kodSum + ") ");
            }
            sql.Append(whereSupp);
            sql.Append(whereServ);
            sql.Append(formingPl);
            switch (PeriodType)
            {
                case 1:
                    sql.Append(" and pp.dat_vvod >= date(" + STCLINE.KP50.Global.Utils.EStrNull(DatS.ToShortDateString()) + ") and pp.dat_vvod <= date(" + STCLINE.KP50.Global.Utils.EStrNull(DatPo.ToShortDateString()) + ")");
                    break;
                case 2:
                    sql.Append(" and coalesce(pp.dat_uchet,date(" +
                    STCLINE.KP50.Global.Utils.EStrNull(Points.DateOper.ToShortDateString()) + ")) >= date(" +
                    STCLINE.KP50.Global.Utils.EStrNull(DatS.ToShortDateString()) + ")" +
                    " and coalesce(pp.dat_uchet,date(" +
                    STCLINE.KP50.Global.Utils.EStrNull(Points.DateOper.ToShortDateString()) + ")) <= date(" +
                    STCLINE.KP50.Global.Utils.EStrNull(DatPo.ToShortDateString()) + ")");
                    break;
                case 3:
                    sql.Append(" AND  pl.dat_vvod >= '" + DatS.ToShortDateString() + "' AND pl.dat_vvod <=  '" +
                               DatPo.ToShortDateString() + "'");
                    break;
                case 4:
                    sql.Append(" and coalesce(pl.dat_uchet,date(" +
                    STCLINE.KP50.Global.Utils.EStrNull(Points.DateOper.ToShortDateString()) + ")) >= date(" +
                    STCLINE.KP50.Global.Utils.EStrNull(DatS.ToShortDateString()) + ")" +
                    " and coalesce(pl.dat_uchet,date(" +
                    STCLINE.KP50.Global.Utils.EStrNull(Points.DateOper.ToShortDateString()) + ")) <= date(" +
                    STCLINE.KP50.Global.Utils.EStrNull(DatPo.ToShortDateString()) + ")");
                    break;
            }
            if (TypePack != 1) { sql.Append(" and pp.pack_type=" + TypePack); }

            string sqlInsRasp = sql.ToString();

            #endregion  
            
                       
            for (var i = DatS.Year*12 + DatS.Month;
                i <=
                DatPo.Year*12 + DatPo.Month ;
                i++)
            {
                var year = i/12;
                var month = i%12;
                if (month == 0)
                {
                    year--;
                    month = 12;
                }

                string pack = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") +
                              DBManager.tableDelimiter +
                              "pack ";
                string pack_ls = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") +
                                 DBManager.tableDelimiter +
                                 "pack_ls ";
                if (TempTableInWebCashe(pack) && TempTableInWebCashe(pack_ls))
                {
                    foreach (var pref in PrefBanks)
                    {
                        string fnspl = pref + "_charge_" + (year - 2000).ToString("00") +
                                       DBManager.tableDelimiter + "fn_supplier" + month.ToString("00");
                        string kvar = pref + DBManager.sDataAliasRest + " kvar ";
                        if (TempTableInWebCashe(fnspl))
                        {
                            string bs = string.Format(sqlInsRasp, pack, pack_ls, fnspl, kvar);
                            ExecSQL(bs);
                        }
                    }
                }
            }

            string kvart = ReportParams.Pref + DBManager.sDataAliasRest + " kvar";
            for (var i = DatS.Year;i <=DatPo.Year;i++)
            {
                string pack = ReportParams.Pref + "_fin_" + (i - 2000).ToString("00") +
                              DBManager.tableDelimiter +
                              "pack ";
                string pack_ls = ReportParams.Pref + "_fin_" + (i - 2000).ToString("00") +
                                 DBManager.tableDelimiter +
                                 "pack_ls ";
                if (TempTableInWebCashe(pack) && TempTableInWebCashe(pack_ls))
                {
                    string nrasp = string.Format(sqlInsNRasp, pack, pack_ls, kvart);
                    ExecSQL(nrasp);

                    foreach (var pref in PrefBanks)
                    {
                        string fromspl = pref + "_charge_" + (i - 2000).ToString("00") +
                                         DBManager.tableDelimiter + "from_supplier";
                        string kvar = pref + DBManager.sDataAliasRest + " kvar ";
                        if (TempTableInWebCashe(fromspl))
                        {
                            string bs = string.Format(sqlInsRasp, pack, pack_ls, fromspl, kvar);
                            ExecSQL(bs);
                        }
                    }
                } 
            }

            string order =FillTmPTable();
            string res = (Params.Contains(7) && !Params.Contains(8))
                ? " select num_pack, name_st, dat_pack, dat_rasp, file_name, payer, kod_sum, point, num_ls, pkod, dat_vvod," +
                  " dat_uchet, name_supp,service, sum(sum_raspr) as sum_raspr, sum(sum_neraspr) as sum_neraspr from t_res_payments_71_1_11 " +
                  " group by num_pack, name_st, dat_pack, dat_rasp, file_name, payer, kod_sum, point, num_ls, pkod, dat_vvod, dat_uchet, name_supp,service"
                : " select num_pack, name_st, dat_pack, dat_rasp, file_name, payer, kod_sum, point, num_ls, pkod, dat_vvod," +
                  " dat_uchet, name_supp,service, sum_raspr, sum_neraspr from t_res_payments_71_1_11 ";
            DataTable dt;
            try
            {
                dt = ExecSQLToTable(res);
                var dv = new DataView(dt) { Sort = order };
                dt = dv.ToTable();
                RowCount = false;
            }
            catch (Exception)
            {
                dt = ExecSQLToTable(DBManager.SetLimitOffset(res, 100000, 0));
                var dv = new DataView(dt) { Sort = order };
                dt = dv.ToTable();
                RowCount = true;
            }
            dt.TableName = "Q_master";

            if (dt.Rows.Count >= 100000)
            {
                if (ReportParams.ExportFormat == ExportFormat.Excel2007)
                {
                    var dtr = dt.Rows.Cast<DataRow>().Skip(40000).ToArray();
                    dtr.ForEach(dt.Rows.Remove);
                }
                else
                {
                    var dtr = dt.Rows.Cast<DataRow>().Skip(100000).ToArray();
                    dtr.ForEach(dt.Rows.Remove);
                }
                RowCount = true;
            }
            else
            {
                RowCount = false;
            }

            var sql2 = new StringBuilder();
            sql2.Remove(0, sql2.Length);
            sql2.Append(" insert into t_title_71_1_11 values( ");
            sql2.Append(Params.Contains(1) ? " '№ пачки' , " : " '' , ");
            sql2.Append(Params.Contains(2) ? " 'Статус пачки' , " : " '' , ");
            sql2.Append(Params.Contains(3) ? " 'Дата пачки' , " : " '' , ");
            sql2.Append(Params.Contains(16) ? " 'Дата распределения' , " : " '' , ");
            sql2.Append(Params.Contains(4) ? " 'Имя файла' , " : " '' , ");
            sql2.Append(Params.Contains(5) ? " 'Место формирования' , " : " '' , ");
            sql2.Append(Params.Contains(6) ? " 'Код квитанции' , " : " '' , ");
            sql2.Append(Params.Contains(7) ? " 'Территория' , " : " '' , ");
            sql2.Append(Params.Contains(8) ? " 'ЛС' , " : " '' , ");
            sql2.Append(Params.Contains(9) ? " 'Пл. код' , " : " '' , ");
            sql2.Append(Params.Contains(10) ? " 'Дата оплаты' , " : " '' , ");
            sql2.Append(Params.Contains(11) ? " 'Операционный день' , " : " '' , ");
            sql2.Append(Params.Contains(12) ? " 'Поставщик' , " : " '' , ");
            sql2.Append(Params.Contains(15) ? " 'Услуга' , " : " '' , ");
            sql2.Append(Params.Contains(13) ? " 'Распределено' , " : " '' , ");
            sql2.Append(Params.Contains(14) ? " 'Не распределено' , " : " '' , ");
            if (sql2.Length>0) sql2.Remove(sql2.Length - 2, 2);
            sql2.Append(") ");
            ExecSQL(sql2.ToString());
            DataTable dt1 = ExecSQLToTable(" select * from t_title_71_1_11 ");
            dt1.TableName = "Q_master1";

            if (dt.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = dt.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(dt.Rows.Remove);
            }
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

            sql.Append(" insert into t_res_payments_71_1_11( ");
            if (Params.Contains(1)) { sql.Append(" num_pack, "); }
            if (Params.Contains(2)) { sql.Append(" \"flag\", "); }
            if (Params.Contains(3)) { sql.Append(" dat_pack, "); }
            if (Params.Contains(16)) { sql.Append(" dat_rasp, "); }
            if (Params.Contains(4)) { sql.Append(" file_name, "); }
            if (Params.Contains(5)) { sql.Append(" nzp_bank, "); }
            if (Params.Contains(6)) { sql.Append(" kod_sum, "); }
            if (Params.Contains(8) || Params.Contains(7)) { sql.Append(" num_ls, "); }
            if (Params.Contains(9)) { sql.Append(" pkod, "); }
            if (Params.Contains(10)) { sql.Append(" dat_vvod, "); }
            if (Params.Contains(11)) { sql.Append(" dat_uchet, "); }
            if (Params.Contains(12)) { sql.Append(" nzp_supp, ");  }
            if (Params.Contains(13)) { sql.Append(" sum_raspr, " ); }
            if (Params.Contains(14)) { sql.Append(" sum_neraspr, " ); }
            if (Params.Contains(15)) { sql.Append(" nzp_serv, "); }
            if (sql.Length > 0) sql.Remove(sql.Length - 2, 2);
            sql.Append(") ");
            sql.Append(" select ");
            if (Params.Contains(1)) { sql.Append(" num_pack, "); grouper.Append(" num_pack, "); order.Append(" num_pack, "); }
            if (Params.Contains(2)) { sql.Append(" \"flag\", "); grouper.Append(" \"flag\", "); order.Append(" name_st, "); }
            if (Params.Contains(3)) { sql.Append(" dat_pack, "); grouper.Append(" dat_pack, "); order.Append(" dat_pack, "); }
            if (Params.Contains(16)) { sql.Append(" dat_rasp, "); grouper.Append(" dat_rasp, "); order.Append(" dat_rasp, "); }
            if (Params.Contains(4)) { sql.Append(" file_name, "); grouper.Append(" file_name, "); order.Append(" file_name, "); }
            if (Params.Contains(5)) { sql.Append(" nzp_bank, "); grouper.Append(" nzp_bank, "); order.Append(" payer, "); }
            if (Params.Contains(6)) { sql.Append(" kod_sum, "); grouper.Append(" kod_sum, "); order.Append(" kod_sum, "); }
            if (Params.Contains(7)) { order.Append(" point, "); }
            if (Params.Contains(8)) { order.Append(" num_ls, "); }
            if (Params.Contains(8) || Params.Contains(7)) { sql.Append(" num_ls, "); grouper.Append(" num_ls, "); }
            if (Params.Contains(9))  { sql.Append(" pkod, "); grouper.Append(" pkod, "); order.Append(" pkod, "); }
            if (Params.Contains(10)) { sql.Append(" dat_vvod, "); grouper.Append(" dat_vvod, "); order.Append(" dat_vvod, "); }
            if (Params.Contains(11)) { sql.Append(" dat_uchet, "); grouper.Append(" dat_uchet, "); order.Append(" dat_uchet, "); }
            if (Params.Contains(12)) { sql.Append(" nzp_supp, "); grouper.Append(" nzp_supp, "); order.Append(" name_supp, "); }
            if (Params.Contains(13)) { sql.Append(" SUM(sum_raspr) as sum_raspr, "); }
            if (Params.Contains(14)) { sql.Append(" SUM(sum_neraspr) as sum_neraspr, "); }
            if (Params.Contains(15)) { sql.Append(" nzp_serv, "); grouper.Append(" nzp_serv, "); order.Append(" service, "); }
            if (sql.Length > 0) sql.Remove(sql.Length - 2, 2);
            sql.Append(" from t_payments_71_1_11 ");

            if (grouper.Length > 0){ grouper.Remove(grouper.Length - 2, 2);sql.Append(" group by " + grouper);}
            if (order.Length > 0) order.Remove(order.Length - 2, 2);
            

            if(grouper.Length>0)ExecSQL(" create index svod_index on t_payments_71_1_11(" + grouper + ") ");
            ExecSQL(DBManager.sUpdStat + " t_payments_71_1_11 ");
            ExecSQL(sql.ToString());

            if (Params.Contains(2))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_payments_71_1_11  set name_st = (" +
                           " select name_st from " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_status a" +
                           " where t_res_payments_71_1_11.\"flag\" = a.nzp_st) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(5))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_payments_71_1_11  set payer = (" +
                           " select payer from " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_bank sb," +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer sp" +
                           " where sb.nzp_bank=t_res_payments_71_1_11.nzp_bank AND sp.nzp_payer=sb.nzp_payer) ");
                ExecSQL(sql.ToString());

                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_payments_71_1_11  set payer = 'Стороннике оплаты на счет РЦ' " +
                          " where payer is null and (kod_sum=49 or kod_sum=50) ");
                ExecSQL(sql.ToString());

                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_payments_71_1_11  set payer = 'Сторонние оплаты на счет УК и ПУ' " +
                           " where payer is null and kod_sum=33 ");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains(7))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_payments_71_1_11  set point = (" +
                           " select point  from " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar kv," +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "s_point sp" +
                           " where kv.num_ls=t_res_payments_71_1_11.num_ls AND kv.nzp_wp=sp.nzp_wp) ");
                ExecSQL(sql.ToString());
            
                if (!Params.Contains(8)) 
                {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_payments_71_1_11  set num_ls = null ");
                ExecSQL(sql.ToString());
                }     
            }


            if (Params.Contains(12))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_payments_71_1_11  set name_supp  = (" +
                           " select name_supp from " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier a" +
                           " where a.nzp_supp = t_res_payments_71_1_11 .nzp_supp) ");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains(15))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_payments_71_1_11  set service  = (" +
                           " select trim(service) from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services a" +
                           " where a.nzp_serv = t_res_payments_71_1_11.nzp_serv) ");
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
                    whereSupp += " and nzp_payer_supp in (" + supp.TrimEnd(',') + ")";
                }

                if (BankSupplier.Principals != null)
                {

                    string supp = string.Empty;
                    supp = BankSupplier.Principals.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                    whereSupp += " and nzp_payer_princip in (" + supp.TrimEnd(',') + ")";
                }

                if (BankSupplier.Agents != null)
                {

                    string supp = string.Empty;
                    supp = BankSupplier.Agents.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                    whereSupp += " and nzp_payer_agent in (" + supp.TrimEnd(',') + ")";
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
                string sql = " SELECT name_supp from " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                             " WHERE nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += "(" + dr["name_supp"].ToString().Trim() + "), ";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',', ' ');  }
            return String.IsNullOrEmpty(whereSupp) ? string.Empty : " and " + fieldPref + " in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + whereSupp + ")";
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
                string sql = " SELECT bd_kernel FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 and flag=2";
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    PrefBanks.Add(row["bd_kernel"].ToString().Trim());
                }
            }
            string whereWpRes =!String.IsNullOrEmpty(whereWp) ? "AND pl.num_ls in (SELECT num_ls FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point s," 
                       + ReportParams.Pref + DBManager.sDataAliasRest+"kvar kv " +
                       "where kv.nzp_wp=s.nzp_wp AND s.nzp_wp in ( " + whereWp + ") )":String.Empty;
            return  whereWpRes ;
        }

        private string GetFormingPlace()
        {
            if (( FormingPlace != 0))
            {
                string sql = " SELECT payer FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer WHERE nzp_payer in ("+FormingPlace+")";
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                   PlaceHeader += row["payer"].ToString().Trim() + ", ";
                }
                PlaceHeader = PlaceHeader.TrimEnd(',', ' ');
            }
            else
            {
                PlaceHeader = String.Empty;  
            }

            string Res = ( FormingPlace !=0 ) ? " and pp.nzp_bank in ( select sb.nzp_bank from " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_bank sb where  sb.nzp_payer in(" + FormingPlace + "))" : String.Empty;
            return Res;
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
            result = !String.IsNullOrEmpty(result) ? " AND nzp_serv in (" + result + ")" : String.Empty;
            if (!String.IsNullOrEmpty(result))
            {
                string sql = " SELECT service " +
                             " from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services " +
                             " WHERE nzp_serv > 1 " + result;
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

            string sql1 = " CREATE TEMP TABLE t_res_payments_71_1_11( " +
                          " num_pack CHAR(10), " +
                          " \"flag\" INTEGER, " +
                          " name_st CHAR(40)," +
                          " dat_rasp DATE, " +
                          " dat_pack DATE, " +
                          " file_name CHAR(200), " +
                          " nzp_bank INTEGER, " +
                          " payer CHAR(200), " +
                          " kod_sum SMALLINT, " +
                          " point CHAR(100)," +
                          " num_ls INTEGER, " +
                          " pkod " + DBManager.sDecimalType + "(13,0)," +
                          " dat_vvod DATE, " +
                          " dat_uchet DATE, " +
                          " nzp_supp INTEGER, " +
                          " name_supp CHAR(100), " +
                          " nzp_serv INTEGER, " +
                          " service CHAR(100), " +
                          " sum_raspr " + DBManager.sDecimalType + "(14,2)," +
                          " sum_neraspr " + DBManager.sDecimalType + "(14,2) )"
                          + DBManager.sUnlogTempTable;
                ExecSQL(sql1);

            string sqls2 = " CREATE TEMP TABLE t_title_71_1_11( " +
                           " num_pack CHAR(10), " +
                           " name_st CHAR(50), " + 
                           " dat_pack CHAR(50), " +
                           " dat_rasp CHAR(50), " +
                           " file_name CHAR(50), " +
                           " payer CHAR(50), " +
                           " kod_sum CHAR(50), " +
                           " point CHAR(50), " +
                           " num_ls CHAR(50), " +
                           " pkod CHAR(50), " +
                           " dat_vvod CHAR(50), " +
                           " dat_uchet CHAR(50), " +
                           " name_supp CHAR(100), " +
                           " service CHAR(100), " +
                           " sum_raspr CHAR(50), " +
                           " sum_neraspr CHAR(50)) "
                           + DBManager.sUnlogTempTable;
                ExecSQL(sqls2);

                if (ReportParams.CurrentReportKind == ReportKind.ListLC)
                {
                    ExecSQL(" create temp table selected_kvars(nzp_kvar integer, num_ls integer) " + DBManager.sUnlogTempTable);
                }                                                             
        }

        protected override void DropTempTable()
        {
            try
            {
                ExecSQL(" DROP TABLE t_payments_71_1_11 ");
                ExecSQL(" DROP TABLE t_res_payments_71_1_11 ");
                ExecSQL(" DROP TABLE t_title_71_1_11 ");
                if (ReportParams.CurrentReportKind == ReportKind.ListLC)
                    ExecSQL(" drop table selected_kvars ", true);
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Отчет 'Генератор по платежам' " + e.Message, MonitorLog.typelog.Error, false);
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
                        string sql = " insert into selected_kvars (nzp_kvar, num_ls) " +
                                     " select nzp_kvar, num_ls from " + tSpls;
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