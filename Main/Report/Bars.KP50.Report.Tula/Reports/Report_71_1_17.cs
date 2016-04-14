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
    public class Report71001017 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.1.17 Отчет по изменениям проведенным в системе"; }
        }

        public override string Description
        {
            get { return "Генератор по перерасчетам/изменеиям сальдо"; }
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
            get { return Resources.Report_71_1_17; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
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

        /// <summary>Параметры</summary>
        private List<string> TerritoryList { get; set; }

        /// <summary>Заголовок Услуг</summary>     
        protected string ServicesHeader { get; set; }

        /// <summary>Список постфиксов банков в БД</summary>  
        private List<string> PrefBanks { get; set; }

        /// <summary>Типы изменений</summary>
        private List<long> ChangeType { get; set; }

        /// <summary>Статус</summary>
        protected int StatusLS { get; set; }

        private bool RowCount { get; set; } 
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
                    Value = "3",
                    StoreData = new List<object>
                    {
                        
                        new { Id = "3", Name = "Все" },
                        new { Id = "1", Name = "Только открытые" },
                        new { Id = "2", Name = "Только закрытые" }
                    }
                }, 

                                new ComboBoxParameter(true) {
                    Name = "Типы изменения", 
                    Code = "ChangeType",
                    Value = 1,
                    StoreData = new List<object> {
                       
                        new { Id = 0, Name = "перерасчет"},
                        new { Id = 100, Name = "Корректировка входящего сальдо"},
                        new { Id = 101, Name = "Корректировка исходящего сальдо"},
                        new { Id = 102, Name = "Корректировка начисления"},
                        new { Id = 103, Name = "Корректировка оплаты"},
                        new { Id = 104, Name = "Автоматическая, внутри поставщика оплатами"},
                        new { Id = 105, Name = "Автоматическая, внутри принципала, между поставщиками оплатами"},
                        new { Id = 106, Name = "Автоматическая, между принципалами оплатами"},
                        new { Id = 107, Name = "Автоматическая, перенос сальдо между поставщиками"},
                        new { Id = 114, Name = "Автоматическая, внутри поставщика изменением сальдо"},
                        new { Id = 115, Name = "Автоматическая, внутри принципала, между поставщиками изменением сальдо"},
                        new { Id = 116, Name = "Автоматическая, между принципалами изменением сальдо"},
                        new { Id = 163, Name = "Корректировка расхода"},
                    }
                },
               
                new ComboBoxParameter(true) {
                    Name = "Параметры отчета", 
                    Code = "Params",
                    Value = 1,
                    Require = true,
                    StoreData = new List<object> {
                        new { Id = 0, Name = "Территория"},
                        new { Id = 4, Name = "УК"},
                        new { Id = 5, Name = "ЖЭУ"},
                        new { Id = 7, Name = "Населенный пункт"},
                        new { Id = 8, Name = "Улица"},
                        new { Id = 9, Name = "Дом"},
                        new { Id = 10, Name = "Квартира"},
                        new { Id = 3, Name = "ФИО"},
                        new { Id = 1, Name = "ЛС"},
                        new { Id = 2, Name = "Статус ЛС"},
                        new { Id = 11, Name = "Поставщик"},
                        new { Id = 12, Name = "Услуга"},
                        new { Id = 13, Name = "Тип изменения"}     // Перерасчет-1, Недопоставка-2, Корректировка-0 (в случае перекидки).
,                       new { Id = 20, Name = "Тип коректировки"}, //Тип перекидки
                        new { Id = 19, Name = "Основание изменения"}, // 
                        new { Id = 14, Name = "Сумма изменения"},
                        new { Id = 17, Name = "Дата изменения"},
                        new { Id = 22, Name = "Пользователь, создавший изменение"}                                                                      
                    }
                },
            };
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            string[] MonthStr = new string[] { "", "январь", "февраль", "март", "апрель", "май", "июнь", 
                                            "июль", "август", "сентябрь", "октябрь", "ноябрь", "декабрь" };

            report.SetParameterValue("time", DateTime.Now.ToLongTimeString());
            report.SetParameterValue("dat", DateTime.Now.ToShortDateString());
            report.SetParameterValue("pMonth",MonthStr[Month]);

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(ServicesHeader) ? "Услуги: " + ServicesHeader + "\n" : string.Empty;
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
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].Value.To<int>();
            Services = UserParamValues["Services"].GetValue<List<long>>();
            Params = UserParamValues["Params"].GetValue<List<long>>();
            StatusLS = UserParamValues["StatusLS"].GetValue<int>();
            ChangeType = UserParamValues["ChangeType"].GetValue<List<long>>();
            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
        }

        public override DataSet GetData()
        {   
            GetwhereWp();
            string whereSupp = GetWhereSupp("t.nzp_supp");
            string whereServ = GetWhereServ();
            string typCh = GetTC();

            var sqlc = new StringBuilder();
            sqlc.Append(" create temp table t_71_1_17( prizn integer,  ");
            if (Params.Contains(0)) { sqlc.Append(" bank char(50), "); }
            if (Params.Contains(1) || Params.Contains(3) || Params.Contains(4) || Params.Contains(5)||
                Params.Contains(6) || Params.Contains(7) || Params.Contains(8) || Params.Contains(9) || Params.Contains(10)) 
            { sqlc.Append(" nzp_kvar integer, "); }
            if (Params.Contains(2))  { sqlc.Append(" ls_st char(1), "); }
            if (Params.Contains(11)) { sqlc.Append(" nzp_supp integer, "); }
            if (Params.Contains(12)) { sqlc.Append(" nzp_serv integer, "); }
            if (Params.Contains(20)) { sqlc.Append(" nzp_type  integer, "); }
            if (Params.Contains(14)) { sqlc.Append(" sum_ " + DBManager.sDecimalType + "(14, 2), " ); }
            if (Params.Contains(17)) { sqlc.Append(" dat_ date, "); } 
            if (Params.Contains(19)) { sqlc.Append(" nzp_osn integer, osnovanie char(150), "); }
            if (Params.Contains(22)) { sqlc.Append(" nzp_user integer, "); } 
            if (sqlc.Length > 0) sqlc.Remove(sqlc.Length - 2, 2);
            sqlc.Append(") ");
            var sc = sqlc.ToString();
            ExecSQL(sc);
            string charge = "_charge_" + (Year % 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + Month.ToString("00");
            string date = new DateTime(Year, Month, 1).ToShortDateString();


            var sql = new StringBuilder();
            var sqlgrouper = new StringBuilder();
            sql.Append(" insert into t_71_1_17(prizn, "); sqlgrouper.Append(" prizn, ");
                    if (Params.Contains(0)) { sql.Append(" bank, "); sqlgrouper.Append(" bank, "); }
                    if (Params.Contains(1) || Params.Contains(3) || Params.Contains(4) || Params.Contains(5) ||
                        Params.Contains(7) || Params.Contains(8) || Params.Contains(9) || Params.Contains(10)) { sql.Append(" nzp_kvar, ");  sqlgrouper.Append(" t.nzp_kvar, "); }
                    if (Params.Contains(2)) { sql.Append(" ls_st, "); sqlgrouper.Append(" ls_st, "); }
                    if (Params.Contains(11)) { sql.Append(" nzp_supp, "); sqlgrouper.Append(" t.nzp_supp, "); }
                    if (Params.Contains(12)) { sql.Append(" nzp_serv, "); sqlgrouper.Append(" t.nzp_serv, "); }
                    if (Params.Contains(20)) { sql.Append(" nzp_type, "); }
                    if (Params.Contains(14)) { sql.Append(" sum_, "); sqlgrouper.Append(" sum_, "); }
                    if (Params.Contains(17)) { sql.Append(" dat_, "); sqlgrouper.Append(" dat_, ");}
                    if (Params.Contains(19)) { sql.Append(" nzp_osn, osnovanie, "); sqlgrouper.Append(" osnovanie, "); }
                    if (Params.Contains(22)) { sql.Append(" nzp_user, "); sqlgrouper.Append(" c.nzp_user, ");}
                    if (sql.Length > 0) sql.Remove(sql.Length - 2, 2);
                    sql.Append(") ");

                if (sqlgrouper.Length > 0) sqlgrouper.Remove(sqlgrouper.Length - 2, 2);
                var gr = sqlgrouper.Length == 0 ? "" : " group by " + sqlgrouper.ToString();

            string insrt = sql.ToString();
            int i=0;
                foreach (var pref in PrefBanks)
                {
                    
                    sql.Remove(0, sql.Length);
                    sql.Append(insrt);
                    sql.Append(" select 1 as prizn, ");
                    if (Params.Contains(0)) { sql.Append("'" + TerritoryList[i] + "' as bank, "); }
                    if (Params.Contains(1) || Params.Contains(3) || Params.Contains(4) || Params.Contains(5) ||
                        Params.Contains(7) || Params.Contains(8) || Params.Contains(9) || Params.Contains(10)) { sql.Append(" t.nzp_kvar, "); }
                    if (Params.Contains(2))  { sql.Append(" trim(p3.val_prm) as ls_st, "); }
                    if (Params.Contains(11)) { sql.Append(" t.nzp_supp, "); }
                    if (Params.Contains(12)) { sql.Append(" t.nzp_serv, "); }
                    if (Params.Contains(20)) { sql.Append(" null, "); }
                    if (Params.Contains(14)) { sql.Append(" reval as sum_, "); }
                    if (Params.Contains(17)) { sql.Append(" c.dat_when as dat_, "); }
                    if (Params.Contains(19)) { sql.Append(" max(kod1), trim(comment) as osnovanie, "); }
                    if (Params.Contains(22)) { sql.Append(" c.nzp_user, "); }
                    if (sql.Length > 0) sql.Remove(sql.Length - 2, 2); 
                    sql.Append(" from "+pref+charge +" t ,"+pref+DBManager.sDataAliasRest+"must_calc c");
                    if (StatusLS != 3 || Params.Contains(2)) { sql.Append("," + pref + DBManager.sDataAliasRest + "prm_3 p3"); }
                    sql.Append(" where reval<>0 and c.nzp_kvar=t.nzp_kvar and c.nzp_supp= t.nzp_supp and c.nzp_serv= t.nzp_serv ");
                    sql.Append(" and c.month_="+Month);
                    sql.Append(" and c.year_=" + Year);
                    if (StatusLS != 3 || Params.Contains(2)) { sql.Append(" and p3.nzp=t.nzp_kvar and p3.nzp_prm=51  and p3.is_actual<>100 "); }
                    if (StatusLS != 3 ) { sql.Append(" and p3.val_prm='"+StatusLS+"'"); }
                    sql.Append(whereSupp);
                    sql.Append(whereServ);
                    sql.Append(gr);
                    var st = sql.ToString();
                    ExecSQL(st);                   
        

                    sql.Remove(0, sql.Length);
                    sql.Append(insrt);
                    sql.Append(" select 0,");
                    if (Params.Contains(0)) { sql.Append("'"+TerritoryList[i] + "' as bank, "); }
                    if (Params.Contains(1) || Params.Contains(3) || Params.Contains(4) || Params.Contains(5) ||
                        Params.Contains(7) || Params.Contains(8) || Params.Contains(9) || Params.Contains(10)) { sql.Append(" t.nzp_kvar, "); }
                    if (Params.Contains(2))  { sql.Append(" trim(p3.val_prm), "); }
                    if (Params.Contains(11)) { sql.Append(" t.nzp_supp, "); }
                    if (Params.Contains(12)) { sql.Append(" t.nzp_serv, "); }
                    if (Params.Contains(20)) { sql.Append(" type_rcl, "); }
                    if (Params.Contains(14)) { sql.Append(" real_charge, "); }
                    if (Params.Contains(17)) { sql.Append(" p.date_rcl , "); }
                    if (Params.Contains(19)) { sql.Append(" p.nzp_doc_base, null, "); }
                    if (Params.Contains(22)) { sql.Append(" p.nzp_user, "); }
                    if (sql.Length > 0) sql.Remove(sql.Length - 2, 2);
                    sql.Append(" from " + pref + charge + " t," + pref + "_charge_" + (Year % 2000).ToString("00") + DBManager.tableDelimiter + "perekidka p");
                    if(StatusLS != 3 || Params.Contains(2)){ sql.Append(","+pref + DBManager.sDataAliasRest + "prm_3 p3");}   
                    sql.Append(" where real_charge<>0 and p.nzp_kvar=t.nzp_kvar and p.nzp_supp=t.nzp_supp and p.nzp_serv= t.nzp_serv ");
                    if (StatusLS != 3 || Params.Contains(2)) { sql.Append(" and p3.nzp=t.nzp_kvar and p3.nzp_prm=51 and p3.is_actual<>100 "); }
                    if (StatusLS != 3) { sql.Append(" and p3.val_prm='" + StatusLS + "'"); }
                    sql.Append(whereSupp);
                    sql.Append(whereServ);
                    var s = sql.ToString();
                    ExecSQL(s);

                    sql.Remove(0, sql.Length);
                    sql.Append(insrt);
                    sql.Append(" select 2,");
                    if (Params.Contains(0)) { sql.Append("'" + TerritoryList[i] + "' as bank, "); }
                    if (Params.Contains(1) || Params.Contains(3) || Params.Contains(4) || Params.Contains(5) ||
                        Params.Contains(7) || Params.Contains(8) || Params.Contains(9) || Params.Contains(10)) { sql.Append(" t.nzp_kvar, "); }
                    if (Params.Contains(2)) { sql.Append(" trim(p3.val_prm), "); }
                    if (Params.Contains(11)) { sql.Append(" t.nzp_supp, "); }
                    if (Params.Contains(12)) { sql.Append(" t.nzp_serv, "); }
                    if (Params.Contains(20)) { sql.Append(" null, "); }
                    if (Params.Contains(14)) { sql.Append(" sum_nedop, "); }
                    if (Params.Contains(17)) { sql.Append(" p.dat_when , "); }
                    if (Params.Contains(19)) { sql.Append(" p.nzp_kind, null, "); }
                    if (Params.Contains(22)) { sql.Append(" p.nzp_user, "); }
                    if (sql.Length > 0) sql.Remove(sql.Length - 2, 2);
                    sql.Append(" from " + pref + charge + " t," + pref + DBManager.sDataAliasRest + "nedop_kvar p");
                    if (StatusLS != 3 || Params.Contains(2)) { sql.Append("," + pref + DBManager.sDataAliasRest + "prm_3 p3"); }
                    sql.Append(" where sum_nedop<>0 and p.nzp_kvar=t.nzp_kvar and p.nzp_supp=t.nzp_supp and p.nzp_serv= t.nzp_serv and p.is_actual<>100 ");
                    sql.Append(" and p.month_calc='"+date+"'");
                    if (StatusLS != 3 || Params.Contains(2)) { sql.Append(" and p3.nzp=t.nzp_kvar and p3.nzp_prm=51 and p3.is_actual<>100"); }
                    if (StatusLS != 3) { sql.Append(" and p3.val_prm='" + StatusLS + "'"); }
                    sql.Append(whereSupp);
                    sql.Append(whereServ);
                    sql.Append(typCh);
                    var sа = sql.ToString();
                    ExecSQL(sа);
                    ++i;
                }
          
            string order =FillTmPTable();
           
            string res =  " select bank, num_ls , ls_st ,  nkvar , ndom, ulica , area , "+
                " town , geu , name_supp , service , change,   type_ , user_name , dat_ , osnovanie , "+
                " fio , SUM(sum_) as sum_ from t_res_71_1_17 "+
                " group by bank, num_ls , ls_st ,   nkvar , ndom, ulica , area , "+
                " town , geu ,  name_supp , service , change, type_ , user_name , dat_ , osnovanie , fio ";
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
            sql2.Append(" insert into t_title_71_1_17 values( ");
            sql2.Append(Params.Contains(0) ? " 'Территория' , " : " '' , ");
            sql2.Append(Params.Contains(4) ? " 'УК' , " : " '' , ");
            sql2.Append(Params.Contains(5) ? " 'ЖЭУ' , " : " '' , ");
            sql2.Append(Params.Contains(7) ? " 'Населенный пункт' , " : " '' , ");
            sql2.Append(Params.Contains(8) ? " 'Улица' , " : " '' , ");
            sql2.Append(Params.Contains(9) ? " 'Дом' , " : " '' , ");
            sql2.Append(Params.Contains(10) ? " 'Квартира' , " : " '' , "); 
            sql2.Append(Params.Contains(1) ? " 'ЛС' , " : " '' , ");
            sql2.Append(Params.Contains(2) ? " 'Статус ЛС' , " : " '' , ");
            sql2.Append(Params.Contains(3) ? " 'ФИО' , " : " '' , ");  
            sql2.Append(Params.Contains(11) ? " 'Поставщик' , " : " '' , ");
            sql2.Append(Params.Contains(12) ? " 'Услуга' , " : " '' , ");
            sql2.Append(Params.Contains(13) ? " 'Тип изменения' , " : " '' , ");
            sql2.Append(Params.Contains(20) ? " 'Тип корректировки' , " : " '' , ");
            sql2.Append(Params.Contains(19) ? " 'Основание изменения' , " : " '' , ");
            sql2.Append(Params.Contains(14) ? " 'Сумма изменения' , " : " '' , ");
            sql2.Append(Params.Contains(17) ? " 'Дата  изменения' , " : " '' , ");
            sql2.Append(Params.Contains(22) ? " 'Пользователь, создавший изменение' , " : " '' , ");
            if (sql2.Length>0) sql2.Remove(sql2.Length - 2, 2);
            sql2.Append(") ");
            ExecSQL(sql2.ToString());
            DataTable dt1 = ExecSQLToTable(" select * from t_title_71_1_17 ");
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
                                                                      
            sql.Append(" insert into t_res_71_1_17( prizn, ");
            if (Params.Contains(0)) { sql.Append(" bank, "); }
            if (Params.Contains(2)) { sql.Append(" ls_st, "); }
            if (Params.Contains(1) || Params.Contains(3) || Params.Contains(4) || Params.Contains(5) ||
                Params.Contains(6) || Params.Contains(7) || Params.Contains(8) || Params.Contains(9) || Params.Contains(10)) { sql.Append(" nzp_kvar, "); }
            if (Params.Contains(11)) { sql.Append(" nzp_supp, "); }
            if (Params.Contains(12)) { sql.Append(" nzp_serv, ");  }
            if (Params.Contains(20)) { sql.Append(" nzp_type, "); }
            if (Params.Contains(14)) { sql.Append(" sum_, "); }
            if (Params.Contains(17)) { sql.Append(" dat_, "); }
            if (Params.Contains(19)) { sql.Append(" nzp_osn, osnovanie, "); }
            if (Params.Contains(22)) { sql.Append(" nzp_user, "); }
            if (sql.Length > 0) sql.Remove(sql.Length - 2, 2);
            sql.Append(") ");
            sql.Append(" select prizn, "); grouper.Append(" prizn, ");
            if (Params.Contains(0)) { sql.Append(" bank, "); grouper.Append(" bank, "); order.Append(" bank, "); }
            if (Params.Contains(2)) { sql.Append(" ls_st, "); grouper.Append(" ls_st, ");  }
            if (Params.Contains(1) || Params.Contains(3) || Params.Contains(4) || Params.Contains(5) ||
                Params.Contains(8) || Params.Contains(9) || Params.Contains(10)) { sql.Append(" nzp_kvar, "); grouper.Append(" nzp_kvar, ");  }
            if (Params.Contains(11)) { sql.Append(" nzp_supp, "); grouper.Append(" nzp_supp, "); order.Append(" name_supp, ");  }
            if (Params.Contains(12)) { sql.Append(" nzp_serv, "); grouper.Append(" nzp_serv, "); order.Append(" service, "); }
            if (Params.Contains(20)) { sql.Append(" nzp_type, "); grouper.Append(" nzp_type, "); }
            if (Params.Contains(14)) { sql.Append(" SUM(sum_), "); }
            if (Params.Contains(17)) { sql.Append(" dat_, "); grouper.Append(" dat_, "); order.Append(" dat_, "); }
            if (Params.Contains(19)) { sql.Append(" nzp_osn, osnovanie, "); grouper.Append(" nzp_osn, osnovanie, "); }
            if (Params.Contains(22)) { sql.Append(" nzp_user, "); grouper.Append(" nzp_user, ");  }
            if (sql.Length > 0) sql.Remove(sql.Length - 2, 2);
            sql.Append(" from t_71_1_17 ");

            if (grouper.Length > 0){ grouper.Remove(grouper.Length - 2, 2);sql.Append(" group by " + grouper);}
            if (order.Length > 0) order.Remove(order.Length - 2, 2);
            

            if(grouper.Length>0)ExecSQL(" create index svod_index on t_71_1_17(" + grouper + ") ");
            ExecSQL(DBManager.sUpdStat + " t_71_1_17 ");
            var ttt = sql.ToString();
            ExecSQL(ttt);



            if (Params.Contains(1))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_17 set num_ls = (" +
                           " select num_ls from " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a " +
                           " where a.nzp_kvar = t_res_71_1_17.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains(3))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_17 set fio = (" +
                           " select fio from " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a " +
                           " where a.nzp_kvar = t_res_71_1_17.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains(4))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_17 set area = (" +
                           " select area from " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a, " +
                           ReportParams.Pref + DBManager.sDataAliasRest + "s_area s " +
                           " where a.nzp_kvar = t_res_71_1_17.nzp_kvar and s.nzp_area=a.nzp_area) ");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains(5))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_17 set geu = (" +
                           " select geu from " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a, " +
                           ReportParams.Pref + DBManager.sDataAliasRest + "s_geu s " +
                           " where a.nzp_kvar = t_res_71_1_17.nzp_kvar and s.nzp_geu=a.nzp_geu) ");
                ExecSQL(sql.ToString());
            }


            if (Params.Contains(6))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_17 set rajon = (" +
                           " select town from " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a, " +
                           ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                           ReportParams.Pref + DBManager.sDataAliasRest + "s_town s  " +
                           " where a.nzp_kvar = t_res_71_1_17.nzp_kvar and a.nzp_dom=d.nzp_dom and s.nzp_town=d.nzp_town) ");
                ExecSQL(sql.ToString());
            }


            if (Params.Contains(7))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_17 set town  = (" +
                           " select rajon from " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a, " +
                           ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                           ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon s  " +
                           " where  a.nzp_kvar = t_res_71_1_17.nzp_kvar and a.nzp_dom=d.nzp_dom and s.nzp_raj=d.nzp_raj) ");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains(8))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_17 set ulica = (" +
                           " select ulica||" + DBManager.sNvlWord + "(' '||ulicareg,'') from " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a, " +
                           ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                           ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica s  " +
                           " where  a.nzp_kvar = t_res_71_1_17.nzp_kvar and a.nzp_dom=d.nzp_dom and s.nzp_ul=d.nzp_ul) ");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains(9))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_17 set ndom = (" +
                           " select ndom||"+DBManager.sNvlWord+"(' корп.'||nkor,'') from " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a, " +
                           ReportParams.Pref + DBManager.sDataAliasRest + "dom d " +
                           " where a.nzp_kvar = t_res_71_1_17.nzp_kvar and a.nzp_dom=d.nzp_dom) ");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains(10))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_17 set nkvar = (" +
                           " select nkvar from " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a " +
                           " where a.nzp_kvar = t_res_71_1_17.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains(11))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_17 set name_supp = (" +
                           " select name_supp from " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier a " +
                           " where a.nzp_supp = t_res_71_1_17.nzp_supp) ");
                ExecSQL(sql.ToString());
            }
            
            if (Params.Contains(12))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_17 set service = (" +
                           " select service from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services a " +
                           " where a.nzp_serv =t_res_71_1_17.nzp_serv) ");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains(20))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_17 set type_ = (" +
                           " select typename from " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_typercl a " +
                           " where a.type_rcl =t_res_71_1_17.nzp_type) " +
                           " where prizn=0");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains(18))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_17 set osnovanie = (" +
                           " select comment from " + ReportParams.Pref + DBManager.sDataAliasRest + "document_base a " +
                           " where a.nzp_doc_base =t_res_71_1_17.nzp_osn) ");
                sql.Append(" where prizn=0");
                ExecSQL(sql.ToString());

                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_17 set osnovanie = (" +
                           " select comment from " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_reason a " +
                           " where a.nzp_reason=t_res_71_1_17.nzp_osn) ");
                sql.Append(" where prizn=1 and osnovanie = null");
                ExecSQL(sql.ToString());

                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_17 set osnovanie = (" +
                           " select comment from " + ReportParams.Pref + DBManager.sDataAliasRest + "upg_s_kind_nedop a " +
                           " where a.nzp_kind =t_res_71_1_17.nzp_osn) ");
                sql.Append(" where prizn=2");
                ExecSQL(sql.ToString());
            }

            if (Params.Contains(20))
            {
                ExecSQL(" update t_res_71_1_17 set change = 'перерасчет' where prizn=1 ");
                ExecSQL(" update t_res_71_1_17 set change = 'корректировка' where prizn=0 "); 
                ExecSQL(" update t_res_71_1_17 set change = 'недопоставка' where prizn=2 ");
            } 
            
            if (Params.Contains(22))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_res_71_1_17 set user_name = (" +
                           " select comment from " + ReportParams.Pref + DBManager.sDataAliasRest + "users a " +
                           " where a.nzp_user =t_res_71_1_17.nzp_user) ");
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
            result = !String.IsNullOrEmpty(result) ? " AND type_rcl in (" + result + ")" : String.Empty;
            return result;
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
            TerritoryList = new List<string>();
            if (!string.IsNullOrEmpty(whereWpsql))
            {
                TerritoryHeader = String.Empty;
                string sql = " SELECT point,bd_kernel FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWpsql;
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                    PrefBanks.Add(row["bd_kernel"].ToString().Trim());
                    TerritoryList.Add(row["point"].ToString().Trim());
                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }
            else
            {
                string sql = " SELECT bd_kernel, point FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 and flag=2";
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    PrefBanks.Add(row["bd_kernel"].ToString().Trim());
                    TerritoryList.Add(row["point"].ToString().Trim());
                }
            }
            string whereWpRes =!String.IsNullOrEmpty(whereWp) ? "AND pl.num_ls in (SELECT num_ls FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point s," 
                       + ReportParams.Pref + DBManager.sDataAliasRest+"kvar kv " +
                       "where kv.nzp_wp=s.nzp_wp AND s.nzp_wp in ( " + whereWp + ") )":String.Empty;
            return  whereWpRes ;
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
                             " from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services t " +
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

                string sql1 = " CREATE TEMP TABLE t_res_71_1_17( " +
                              " bank CHAR(60), " +          
                              " num_ls INTEGER, " +
                              " ls_st CHAR(1),"+
                              " nzp_kvar INTEGER, " +
                              " nkvar CHAR(60)," +
                              " ndom CHAR(60)," +
                              " ulica CHAR(60)," +
                              " area CHAR(60)," +
                              " town CHAR(60)," +
                              " geu CHAR(60)," +
                              " nzp_supp INTEGER, " +
                              " nzp_serv INTEGER, " +
                              " name_supp CHAR(200)," +
                              " fio CHAR(100)," +
                              " service CHAR(60)," +
                              " nzp_type INTEGER, " +
                              " type_ CHAR(60)," +
                              " nzp_user INTEGER, " +
                              " user_name CHAR(60)," +
                              " dat_ DATE, " +
                              " nzp_osn INTEGER, " +
                              " osnovanie CHAR(200), " +
                              " comment CHAR(200), " +
                              " change CHAR(20), " +
                              " prizn integer, "+//0 - perekidca, 1 - must calc
                              " sum_ " + DBManager.sDecimalType + "(14,2) )"
                              + DBManager.sUnlogTempTable;
                ExecSQL(sql1);

                string sqls2 = " CREATE TEMP TABLE t_title_71_1_17( " +
                  " bank CHAR(50), " +
                  " area CHAR(50), " +
                  " geu CHAR(50), " +
                  " nas_p CHAR(50), " +
                  " ulica CHAR(50), " +
                  " ndom CHAR(50), " +
                  " nkvar CHAR(50), " + 
                  " num_ls CHAR(10), " + 
                  " ls_st CHAR(50), " +                 
                  " fio CHAR(50), " +
                  " name_supp CHAR(50), " +
                  " service CHAR(50), " +
                  " change CHAR(50), " +
                  " type_ CHAR(50), " +
                  " osnovanie CHAR(50), " +                  
                  " sum_ CHAR(50), " + 
                  " dat CHAR(50), " + 
                  " user_name CHAR(50)) "
                  + DBManager.sUnlogTempTable;
                ExecSQL(sqls2);
        }

        protected override void DropTempTable()
        {
            try
            {
                ExecSQL(" DROP TABLE t_71_1_17 ");
                ExecSQL(" DROP TABLE t_res_71_1_17 ");
                ExecSQL(" DROP TABLE t_title_71_1_17 ");
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Отчет 'Генератор по перерасчетам/изменеиям сальдо' " + e.Message, MonitorLog.typelog.Error, false);
            }
        }

    }
}