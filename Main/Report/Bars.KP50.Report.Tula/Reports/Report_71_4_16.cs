using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Report.Tula.Properties;
using globalsUtils = STCLINE.KP50.Global.Utils;

namespace Bars.KP50.Report.Tula.Reports
{
    public class Report71004016 : BaseSqlReportCounterFlow
    {
        private const int houseCounterType = 1;
        private const int groupCounterType = 2;
        private const int individualCounterType = 3;
        
        public override string Name
        {
            get { return "71.4.16 Сведения о расходах по квартирным приборам учета"; }
        }

        public override string Description
        {
            get { return "Сведения о расходах по приборам учета"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> {ReportGroup.Reports};
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_71_4_16; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
        }


        /// <summary>Расчетный месяц</summary>
        private int MonthS { get; set; }

        /// <summary>Расчетный год</summary>
        private int YearS { get; set; }


        /// <summary>Расчетный месяц</summary>
        private int MonthPo { get; set; }

        /// <summary>Расчетный год</summary>
        private int YearPo { get; set; }

        /// <summary>Услуги</summary>
        private List<int> Services { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }

        /// <summary>Заголовок отчета</summary>
        private string SupplierHeader { get; set; }

        /// <summary>Услуги</summary>
        private string ServiceHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        private string TerritoryHeader { get; set; }

        /// <summary>
        /// Дата начала периода
        /// </summary>
        private DateTime DatUchet { get; set; }
        
        /// <summary>
        /// Дата окончания периода
        /// </summary>
        private DateTime DatUchetPo { get; set; }

        // только новые ПУ
        private bool IsNew { get; set; }

        // 1-Все, 2-МКД, 3-Частный сектор
        private int IsMkd { get; set; }

        /// <summary> Вид ПУ </summary>
        private int TypePU { get; set; }

        private string _where_serv = "";
        private string _where_supp = "";

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Name = "Месяц с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new MonthParameter {Name = "Месяц по", Code = "Month1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год по", Code = "Year1", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new BankSupplierParameter(),
                new ServiceParameter(),
                new ComboBoxParameter
                {
                    Code = "TypePU",
                    Name = "Вид ПУ",
                    Value = "3",
                    StoreData =  new List<object>
                    {
                        new { Id = "3", Name = "Индивидуальный" },
                        new { Id = "1", Name = "Домовой" },
                        new { Id = "2", Name = "Групповой" }
                    }
                },
                new ComboBoxParameter
                {
                    
                    Code = "isNew",
                    Name = "Только новые ПУ",
                    Value = "1",
                    StoreData = new List<object>
                    {
                        new {Id = "1", Name = "Нет"},
                        new {Id = "2", Name = "Да"},
                    }
                },
                  new ComboBoxParameter
                {
                    
                    Code = "isMkd",
                    Name = "Дома",
                    Value = "1",
                    StoreData = new List<object>
                    {
                        new {Id = "1", Name = "Все"},
                        new {Id = "2", Name = "МКД"},
                        new {Id = "3", Name = "Частный сектор"}
                    }
                }
            };
        }

        private int pCountIteration = 0;
        private int pCurrIteration = 0;

        public override DataSet GetData()
        {
            string sql;

            List<string> prefList = GetPrefList();

            pCountIteration = 4 * prefList.Count + 3;

            _where_serv = GetWhereServ("cs.");
            _where_supp = GetWhereSupp("cs.nzp_supp"); 
            
            string tableCounter = string.Empty;
            switch (TypePU)
            {
                case 1: tableCounter = "counters_dom"; break;
                case 2: tableCounter = "counters_group"; break;
                case 3: tableCounter = "counters"; break;
            }

            bool getFromUserSelected = GetUserSelectedKvars();
            SetProgress();
            
            foreach (string curPref in prefList)
            {
                if (TypePU == groupCounterType)
                {
                    // получить дома
                    GetDom(curPref, getFromUserSelected);
                }
                else
                {
                    // получить ЛС
                    GetKvar(curPref, getFromUserSelected); 
                }
                SetProgress();

                GetCounters(curPref, tableCounter);
                SetProgress();

                /// Получить показания ПУ и расход
                GetCounterFlow("t_couns", TypePU, curPref, DatUchet, DatUchetPo);
                SetProgress();

                InsertDataIntoTempReport(curPref);
                SetProgress();
            }

            // уточнить цель использования id_counter
            if (TypePU == groupCounterType)
            {
                sql = " INSERT INTO selected_counter (ulica, idom, ndom, nkor, nzp_counter) " +
                      " SELECT DISTINCT ulica, idom, ndom, nkor, nzp_counter FROM t_report_71_4_16 " +
                      " ORDER BY ulica, idom, ndom, nkor, nzp_counter ";
                ExecSQL(sql);

                ExecSQL("create index ix_selected_counter_nzp_counter on selected_counter(nzp_counter)");
                ExecSQL(DBManager.sUpdStat + " selected_counter");
            }

            sql = " Select DISTINCT ikvar, TRIM(nkvar) AS nkvar, TRIM(nkvar_n) AS nkvar_n, num_ls, TRIM(fio) AS fio, " +
                    " nzp_ul, TRIM(ulica) AS ulica, " +
                    " nzp_dom, idom, TRIM(ndom) AS ndom, TRIM(nkor) AS nkor, nzp_counter, " +
                    " (select s.id_counter from selected_counter s where s.nzp_counter = t_report_71_4_16.nzp_counter) as id_counter, " +
                    " TRIM(num_cnt) AS num_cnt, TRIM(name_type) AS name_type, cnt_stage, mmnog, TRIM(measure) AS measure, ngp_cnt, " +
                    " TRIM(service) AS service, val_cnt_pred, dat_uchet_pred, val_cnt, dat_uchet, dat_prov, dat_ust, rashod " +
                " From t_report_71_4_16 " +
                (TypePU == houseCounterType ? " Where 1=0 " : "") + // при просмотре домовых ПУ не заполнять данными Q_master
                " order by ulica, idom, ndom, nkor, service," + (TypePU == groupCounterType ? "nzp_counter, " : string.Empty) + "ikvar, nkvar, nkvar_n, num_cnt, dat_uchet, dat_prov ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            sql = " Select DISTINCT nzp_town, TRIM(town) AS town, nzp_raj, TRIM(rajon) AS rajon, " +
                    " nzp_ul, TRIM(ulica) AS ulica, " +
                    " nzp_dom, idom, TRIM(ndom) AS ndom, TRIM(nkor) AS nkor, " +
                    " nzp_counter, TRIM(num_cnt) AS num_cnt, TRIM(name_type) AS name_type, cnt_stage, mmnog, TRIM(measure) AS measure, ngp_cnt, " +
                    " TRIM(service) AS service, val_cnt_pred, dat_uchet_pred, val_cnt, dat_uchet, dat_prov, dat_ust, rashod " +
                " From t_report_71_4_16  " +
                (TypePU != houseCounterType ? " Where 1=0 " : "") + // при просмотре НЕдомовых ПУ не заполнять данными Q_master1
                " order by town, rajon,  ulica, idom, ndom, nkor, service, num_cnt, dat_uchet, dat_prov ";
            DataTable dt1 = ExecSQLToTable(sql);
            dt1.TableName = "Q_master1";

            sql = " Select DISTINCT TRIM(geu) AS geu, ikvar, TRIM(nkvar) AS nkvar, TRIM(nkvar_n) AS nkvar_n, num_ls, TRIM(fio) AS fio, " +
                    " TRIM(ulica) AS ulica, " +
                    " idom, TRIM(ndom) AS ndom, TRIM(nkor) AS nkor, " +
                    " TRIM(num_cnt) AS num_cnt, cnt_stage, mmnog, " +
                    " TRIM(service) AS service, val_cnt_pred, val_cnt, dat_ust, rashod " +
                " From t_report_71_4_16  " +
                " where 1=0 " + // отчет для самары, в Туле не показывать
                " order by geu, ulica, idom, ndom, nkor, ikvar, nkvar, nkvar_n, service, num_cnt";
            DataTable dt2 = ExecSQLToTable(sql);
            dt2.TableName = "Q_master2";

            SetProgress();

            DataView dv = dt.AsDataView();
            DataView dv1 = dt1.AsDataView();
            DataView dv2 = dt2.AsDataView();

            var fDataSet = new DataSet();
            fDataSet.Tables.Add(dt);
            fDataSet.Tables.Add(dt1);
            fDataSet.Tables.Add(dt2);
            return fDataSet;
        }

        private void SetProgress()
        { 
#if PG
            pCurrIteration++;
            string sql = " update public.excel_utility set progress = " + (((decimal)pCurrIteration) / pCountIteration) + " where nzp_exc = " + ReportParams.NzpExcelUtility;
            ExecSQL(sql);
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pref"></param>
        private void InsertDataIntoTempReport(string pref)
        {
            string sql = " ";
            string centralData = ReportParams.Pref + DBManager.sDataAliasRest;
            string prefKernel = pref + DBManager.sKernelAliasRest;
            string prefData = pref + DBManager.sDataAliasRest;

            if (TypePU == houseCounterType)
            {
                ExecSQL("create index ix_t_couns_nzp_dom on t_couns (nzp_dom)");
                ExecSQL(DBManager.sUpdStat + " t_couns");

                sql = " INSERT INTO t_report_71_4_16 (geu, nzp_town, town, nzp_raj, rajon, " +
                    " nzp_ul, ulica, nzp_dom, idom, ndom, nkor, nzp_counter, num_cnt, name_type, cnt_stage, mmnog, measure, ngp_cnt," +
                    " service, val_cnt_pred, dat_uchet_pred, val_cnt, dat_uchet, dat_prov, dat_ust, rashod) " +
                 " Select DISTINCT g.geu, t.nzp_town, t.town, r.nzp_raj, r.rajon, " + 
                    " u.nzp_ul, u.ulica, " +
                    " d.nzp_dom, d.idom, d.ndom, d.nkor, " +
                    " cs.nzp_counter, cs.num_cnt, cs.name_type, cs.cnt_stage, cs.mmnog, cs.measure, cs.ngp_cnt, " +
                    " s.service, val_cnt_pred, dat_uchet_pred, val_cnt, dat_uchet, dat_prov, cs.dat_ust, cs.rashod " +
                 " From t_couns cs, " +
                    prefData + "kvar k, " +
                    prefData + "dom d, " +
                    prefData + " s_ulica u, " +
                    centralData + "s_town t, " + 
                    centralData + "s_rajon r, " +
                    prefKernel + " services s, " +
                    prefData + "s_geu g " +
                " Where cs.nzp_dom = d.nzp_dom " + 
                "   AND u.nzp_raj = r.nzp_raj " + 
                "   AND r.nzp_town = t.nzp_town " +
                "   and k.nzp_dom = d.nzp_dom " +
                "   and d.nzp_ul = u.nzp_ul " +
                "   and cs.nzp_serv = s.nzp_serv " +
                "   and k.nzp_geu = g.nzp_geu ";
            }
            else
            {
                ExecSQL("create index ix_t_couns_nzp_kvar on t_couns (nzp_kvar)");
                ExecSQL(DBManager.sUpdStat + " t_couns");

                sql = " INSERT INTO t_report_71_4_16 (geu, ikvar, nkvar, nkvar_n, num_ls, fio, " + 
                    " nzp_ul, ulica, nzp_dom, idom, ndom, nkor, nzp_counter, num_cnt, name_type, cnt_stage, mmnog, measure, ngp_cnt," +
                    " service, val_cnt_pred, dat_uchet_pred, val_cnt, dat_uchet, dat_prov, dat_ust, rashod) " +
                " Select g.geu, k.ikvar, k.nkvar, k.nkvar_n, k.num_ls, k.fio, " +
                    " u.nzp_ul, (case when r.rajon = '-' then trim(t.town)||' '||trim(u.ulica) else trim(r.rajon)||' '||trim(u.ulica) end) as ulica, " +
                    " d.nzp_dom, d.idom, d.ndom, d.nkor, " +
                    " cs.nzp_counter, cs.num_cnt, cs.name_type, cs.cnt_stage, cs.mmnog, cs.measure, cs.ngp_cnt, " +
                    " s.service, val_cnt_pred, dat_uchet_pred, val_cnt, dat_uchet, dat_prov, cs.dat_ust, cs.rashod " +
                " From t_couns cs, " + 
                    prefData + "kvar k, " +
                    prefData + "dom d, " +
                    prefData + " s_ulica u, " +
                    prefData + "s_rajon r, " +
                    prefData + "s_town t, " +
                    prefKernel + " services s, " +
                    prefData + "s_geu g " +
                " Where cs.nzp_kvar = k.nzp_kvar " + 
                    " and k.nzp_dom = d.nzp_dom " +
                    " and d.nzp_ul = u.nzp_ul " +
                    " and cs.nzp_serv = s.nzp_serv " +
                    " and k.nzp_geu = g.nzp_geu " + 
                    " and u.nzp_raj = r.nzp_raj " + 
                    " and r.nzp_town = t.nzp_town";
            }
            
            ExecSQL(sql);    
        }
        
        /// <summary>
        /// Получить приборы учета
        /// </summary>
        /// <param name="pref"></param>
        /// <param name="tableCounter"></param>
        private void GetCounters(string pref, string counterValTable)
        {
            string prefKernel = pref + DBManager.sKernelAliasRest;
            string prefData = pref + DBManager.sDataAliasRest;

            ExecSQL("drop table t_couns");

            string sql = " Create temp table t_couns(" +
                " nzp_town INTEGER, " +
                " nzp_raj INTEGER, " +
                " nzp_dom INTEGER, " +
                " nzp_kvar integer, " +
                " nzp_type INTEGER, " +
                " nzp_counter integer," +
                " nzp_serv integer," +
                " num_cnt char(20), " +
                " cnt_stage integer," +
                " mmnog " + DBManager.sDecimalType + "(8,4)," +
                " measure char(20)," +
                " name_type char(40)," +
                " ngp_cnt  " + DBManager.sDecimalType + "(14,4) default 0, " +
                " ngp_lift " + DBManager.sDecimalType + "(14,4), " +
                " dat_uchet Date, " +
                " val_cnt " + DBManager.sDecimalType + "(14,4)," +
                " dat_uchet_pred Date, " +
                " val_cnt_pred " + DBManager.sDecimalType + "(14,4)," +
                " dat_prov Date, " +
                " dat_ust  varchar(12), " +
                " rashod   " + DBManager.sDecimalType + "(20,8) default 0.00 " +
                ")" + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = "INSERT INTO t_couns (nzp_kvar, nzp_dom, nzp_serv, nzp_counter, num_cnt, name_type, cnt_stage, mmnog, measure, dat_prov) " +
                " SELECT k.nzp_kvar, k.nzp_dom, cs.nzp_serv, cs.nzp_counter, cs.num_cnt, t.name_type, t.cnt_stage, t.mmnog, m.measure, " +
                // дата поверки
                "   (CASE WHEN cs.dat_prov IS NOT NULL AND cs.dat_provnext IS NOT NULL AND cs.dat_prov > cs.dat_provnext THEN cs.dat_prov ELSE cs.dat_provnext END) " +
                " FROM " +
                prefData + "counters_spis cs, " +
                prefKernel + "s_counts cc, " +
                prefKernel + "s_counttypes t, " +
                prefKernel + "s_measure m, " +
                " selected_kvar k " + // выбранные ЛС/дома
                " WHERE    cs.nzp_serv = cc.nzp_serv " +
                "   and cs.nzp_cnttype = t.nzp_cnttype " +
                "   and cc.nzp_measure = m.nzp_measure ";

            switch (TypePU)
            {
                case houseCounterType:
                    sql += " and cs.nzp = k.nzp_dom ";
                    break;
                case groupCounterType:
                    sql += " and exists (select 1 from " + prefData + "counters_link cl " + 
                        " where cl.nzp_counter = cs.nzp_counter and cl.nzp_kvar = k.nzp_kvar " + DBManager.Limit1 + ")";
                    break;
                case individualCounterType: 
                    sql += " and cs.nzp = k.nzp_kvar ";
                    break;
            }

            sql += " and cs.nzp_type = " + TypePU +
                " and cs.dat_close is null " + // только открытые ПУ
                _where_serv; 

            if (IsNew)
            {
                sql += " and (select min(cd.dat_uchet) from " + prefData + counterValTable + " cd where cd.nzp_counter = cs.nzp_counter " +
                    "   and cd.is_actual <> 100) between " + globalsUtils.EStrNull(DatUchet.ToShortDateString()) +
                    "   and " + globalsUtils.EStrNull(DatUchet.AddMonths(1).ToShortDateString());
            }    
            
            ExecSQL(sql);

            ExecSQL("create index ix_t_couns_nzp_counter on t_couns (nzp_counter)");
            ExecSQL(DBManager.sUpdStat + " t_couns");

            // проставить дату установки
            sql = " UPDATE t_couns t SET " +
                " dat_ust = (SELECT p.val_prm FROM " + prefData + "prm_17 p" +
                " WHERE p.nzp = t.nzp_counter " +
                "   and p.nzp_prm = 2025 " +
                "   and p.is_actual = 1 " +
                "   and p.dat_s  <= " + globalsUtils.EStrNull(DatUchet.ToShortDateString()) +
                "   and p.dat_po >= " + globalsUtils.EStrNull(DatUchet.AddMonths(1).ToShortDateString()) + ")";
            ExecSQL(sql, true);
        }

        /// <summary>
        /// Получить список ЛС, выбранных пользователем
        /// </summary>
        private bool GetUserSelectedKvars()
        {
            string sql = "";
            bool getFromUserSelected = false;

            ExecSQL("drop table tmp_user_selected", true);
            sql = " create temp table tmp_user_selected (" +
                " nzp_kvar integer, " +
                " nzp_dom integer) " +
                DBManager.sUnlogTempTable;
            ExecSQL(sql);

            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                string tSpls = "";              
                using (IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata))
                {
                    if (!DBManager.OpenDb(connWeb, true).result) throw new Exception("Не удалось установить связь c web");
                    tSpls = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter + "t" + ReportParams.User.nzp_user + "_spls";
                }

                if (TempTableInWebCashe(tSpls))
                {
                    sql = " insert into tmp_user_selected (nzp_kvar, nzp_dom) " +
                    " select nzp_kvar, nzp_dom from " + tSpls +
                    " group by 1,2"; // вместо distinct
                    ExecSQL(sql);

                    ExecSQL("create index ix_tmp_user_selected_nzp_kvar on tmp_user_selected (nzp_kvar)");
                    ExecSQL("create index ix_tmp_user_selected_nzp_dom on tmp_user_selected (nzp_dom)");
                    ExecSQL(DBManager.sUpdStat + " tmp_user_selected");

                    getFromUserSelected = true;
                }
            }

            return getFromUserSelected;
        }

        /// <summary>
        /// Получить ЛС
        /// </summary>
        /// <param name="curPref"></param>
        /// <param name="getUserSelected"></param>
        private void GetKvar(string curPref, bool getUserSelected)
        {
            string sql = "";

            CreateSelectedKvar();

            if (!String.IsNullOrEmpty(SupplierHeader))
            {
                sql = " insert into selected_kvar (nzp_kvar, nzp_dom) " +
                    " select k.nzp_kvar, k.nzp_dom " +
                    " from " + 
                    curPref + DBManager.sDataAliasRest + "tarif cs, " +
                    curPref + DBManager.sDataAliasRest + "kvar k " +
                    (getUserSelected ? ", tmp_user_selected b " : "") + // ЛС, выбранные пользователем
                    " where k.nzp_kvar = cs.nzp_kvar " +
                    (getUserSelected ? " and k.nzp_kvar = b.nzp_kvar " : "") +
                    "   and cs.is_actual = 1 " +
                    "   and cs.dat_s < " + globalsUtils.EStrNull(DatUchetPo.AddMonths(1).ToShortDateString()) +
                    "   and cs.dat_po >= " + globalsUtils.EStrNull(DatUchet.ToShortDateString()) + " " +
                    _where_supp +
                    _where_serv + 
                    GetMkdClause(curPref, "k") + 
                    " group by 1,2 "; // distinct использует неявный group by
            }
            else
            {
                sql = " insert into selected_kvar (nzp_kvar, nzp_dom) " +
                    " select k.nzp_kvar, k.nzp_dom " +
                    " from " +
                    curPref + DBManager.sDataAliasRest + "kvar k " +
                    (getUserSelected ? ", tmp_user_selected b " : "") + // ЛС, выбранные пользователем
                    " where 1=1 " +
                    (getUserSelected ? " and k.nzp_kvar = b.nzp_kvar " : "") +
                    GetMkdClause(curPref, "k");
            }

            ExecSQL(sql);

            ExecSQL("create index ix_selected_kvar_nzp_kvar on selected_kvar (nzp_kvar)");
            ExecSQL("create index ix_selected_kvar_nzp_dom  on selected_kvar (nzp_dom)");
            ExecSQL(DBManager.sUpdStat + " selected_kvar");
        }

        /// <summary>
        /// Получить дома
        /// </summary>
        /// <param name="curPref"></param>
        /// <param name="getUserSelected"></param>
        private void GetDom(string curPref, bool getUserSelected)
        {
            string sql = "";

            CreateSelectedKvar();


            if (!String.IsNullOrEmpty(SupplierHeader))
            {
                sql = " insert into selected_kvar (nzp_dom) " +
                    " select k.nzp_dom, " +
                    " from " +
                    curPref + DBManager.sDataAliasRest + "tarif cs, " +
                    curPref + DBManager.sDataAliasRest + "kvar k " +
                    (getUserSelected ? ", tmp_user_selected b " : "") + // ЛС, выбранные пользователем
                    " where cs.is_actual = 1 " +
                    (getUserSelected ? " and k.nzp_dom = b.nzp_dom " : "") +
                    "   and cs.dat_s < " + globalsUtils.EStrNull(DatUchetPo.AddMonths(1).ToShortDateString()) +
                    "   and cs.dat_po >= " + globalsUtils.EStrNull(DatUchet.ToShortDateString()) + " " +
                    _where_supp +
                    _where_serv +
                    GetMkdClause(curPref, "k") +
                    " group by 1 "; // distinct использует неявный group by
            }
            else
            {
                sql = " insert into selected_kvar (nzp_dom) " +
                    " select d.nzp_dom " +
                    " from " +
                    curPref + DBManager.sDataAliasRest + "dom d " +
                    (getUserSelected ? ", tmp_user_selected b " : "") + // ЛС, выбранные пользователем
                    " where 1=1 " +
                    (getUserSelected ? " and k.nzp_dom = b.nzp_dom " : "") +
                    GetMkdClause(curPref, "d");
            }

            ExecSQL(sql);

            ExecSQL("create index ix_selected_kvar_nzp_dom on selected_kvar (nzp_dom)");
            ExecSQL(DBManager.sUpdStat + " selected_kvar");    
        }

        private void CreateSelectedKvar()
        {
            ExecSQL("Drop table selected_kvar", false);

            string sql = "Create temp table selected_kvar (" +
                " nzp_kvar integer default 0, " +
                " nzp_dom  integer default 0)" + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        /// <summary>
        /// Условие на вид дома
        /// </summary>
        /// <param name="pref"></param>
        /// <param name="nameTable"></param>
        /// <returns></returns>
        private string GetMkdClause(string pref, string nameTable)
        {
            if (IsMkd == 1) return "";

            return " and " + (IsMkd == 2 ? "" : "not") + " exists " +
                " (select 1 from " + pref + DBManager.sDataAliasRest + "prm_2 a " +
                " where a.nzp = " + nameTable + ".nzp_dom " +
                "   and a.nzp_prm = 2030 " +
                "   and a.is_actual = 1 " +
                "   and a.val_prm = '1' " + DBManager.Limit1 + ")";
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
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_supp in (" + supp.TrimEnd(',') + ")";
            }

            if (BankSupplier != null && BankSupplier.Principals != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Principals.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_princip in (" + supp.TrimEnd(',') + ")";
            }
            if (BankSupplier != null && BankSupplier.Agents != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Agents.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                //whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
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
        /// Получить условия органичения по услугам
        /// </summary>
        /// <returns></returns>
        private string GetWhereServ(string tablePrefix)
        {
            
            string whereServ = String.Empty;
            whereServ = Services != null ? Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_serv);
            whereServ = whereServ.TrimEnd(',');
            whereServ = !String.IsNullOrEmpty(whereServ) ? " AND " + tablePrefix + "nzp_serv in (" + whereServ + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereServ))
            {
                string sql = " SELECT service " +
                             " from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services " + tablePrefix.TrimEnd('.') +
                             " WHERE " + tablePrefix + "nzp_serv > 1 " + whereServ;
                DataTable serv = ExecSQLToTable(sql);
                foreach (DataRow dr in serv.Rows)
                {
                    ServiceHeader += dr["service"].ToString().Trim() + ", ";
                }
                ServiceHeader = ServiceHeader.TrimEnd(',', ' ');
            }
            return whereServ;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("month", months[MonthS]);
            report.SetParameterValue("year", YearS);

            string mkd, typePU;
            switch (IsMkd)
            {
                case 1:
                    mkd = "МКД + частный сектор";
                    break;
                case 2:
                    mkd = "МКД";
                    break;
                case 3:
                    mkd = "частный сектор";
                    break;
                default : mkd = "";
                    break;
            }

            switch (TypePU)
            {
                case 1:
                    typePU = "Общедомовые приборы учета";
                    break;
                case 2:
                    typePU = "Групповые приборы учета";
                    break;
                case 3:
                    typePU = "Индивидуальные приборы учета";
                    break;
                default:
                    typePU = string.Empty;
                    break;
            }

            // дом
            // месяц
            string pmonth;

            if (DatUchet == DatUchetPo) pmonth = DatUchet.ToString("MMMM yyyy").ToLower() + " г.";
            else pmonth = DatUchet.ToString("MMMM yyyy").ToLower() + " г." 
                + " - " + DatUchetPo.ToString("MMMM yyyy").ToLower() + " г.";
            report.SetParameterValue("psupplier", string.IsNullOrEmpty(SupplierHeader) ? "<Все>": SupplierHeader);
            report.SetParameterValue("pservice", string.IsNullOrEmpty(ServiceHeader) ? "<Все>": ServiceHeader);
            report.SetParameterValue("pdom", string.IsNullOrEmpty(mkd) ? "<Все>": mkd);
            report.SetParameterValue("pmonth", pmonth);
            report.SetParameterValue("isSmr",  "0");
            report.SetParameterValue("parea", string.IsNullOrEmpty(TerritoryHeader)? "<Все>" : TerritoryHeader);
            report.SetParameterValue("pcounter", IsNew ? "Новые" : "<Все>");
            report.SetParameterValue("ptypepu", typePU);
            report.SetParameterValue("idtypepu", TypePU);

        }
        
        private string GetWhereWp()
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
            TerritoryHeader = "";
            if (!string.IsNullOrEmpty(whereWp))
            {
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

        protected override void PrepareParams()
        {
            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].Value.To<int>();
            MonthPo = UserParamValues["Month1"].GetValue<int>();
            YearPo = UserParamValues["Year1"].Value.To<int>();
            DatUchet = new DateTime(YearS, MonthS, 1);
            DatUchetPo = new DateTime(YearPo, MonthPo, 1);
            
            Services = UserParamValues["Services"].Value.To<List<int>>();
            IsNew = UserParamValues["isNew"].Value.To<int>() == 2;
            IsMkd = UserParamValues["isMkd"].Value.To<int>();
            TypePU = UserParamValues["TypePU"].Value.To<int>();

            
            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
    
        }

        protected override void CreateTempTable()
        {
            string sql = "";
                
            sql = " CREATE TEMP TABLE t_report_71_4_16 (" +
                  " geu CHARACTER(60), " +
                  " nzp_town INTEGER, " +
                  " town CHARACTER(30), " +
                  " nzp_raj INTEGER, " +
                  " rajon CHARACTER(30), " +
                  " ikvar INTEGER, " +
                  " nkvar CHARACTER(10), " +
                  " nkvar_n CHARACTER(10), " +
                  " num_ls INTEGER, " +
                  " fio CHARACTER(50), " +
                  " nzp_ul INTEGER, " +
                  " ulica varchar(300), " +
                  " nzp_dom INTEGER, " +
                  " idom INTEGER, " +
                  " ndom CHARACTER(10), " +
                  " nkor CHARACTER(3), " +
                  " nzp_counter INTEGER, " +
                  " num_cnt CHARACTER(20), " +
                  " dat_ust CHARACTER(12), " +
                  " name_type CHARACTER(40), " +
                  " cnt_stage INTEGER, " +
                  " mmnog " + DBManager.sDecimalType + "(8,4), " +
                  " measure CHARACTER(20), " +
                  " ngp_cnt " + DBManager.sDecimalType + "(14,4), " +
                  " service CHARACTER(100), " +
                  " val_cnt_pred " + DBManager.sDecimalType + "(14,4), " +
                  " dat_uchet_pred DATE, " +
                  " val_cnt " + DBManager.sDecimalType + "(14,4), " +
                  " dat_uchet DATE, " +
                  " dat_prov DATE, " +
                  " rashod " + DBManager.sDecimalType + "(20,8)" +
                  ") " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = "CREATE TEMP TABLE selected_counter (" +
                            " id_counter SERIAL NOT NULL,  " +
                            " ulica CHARACTER(40), " +
                            " idom INTEGER, " +
                            " ndom CHARACTER(10), " +
                            " nkor CHARACTER(3), " +
                            " nzp_counter INTEGER)" + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table tmp_user_selected ", false);
            ExecSQL(" drop table t_couns ", false);
            ExecSQL(" drop table selected_kvar ", false);
            ExecSQL(" drop table t_report_71_4_16 ", false);
            ExecSQL(" drop table selected_counter ");
        }

        /// <summary>
        /// Получение префиксов БД
        /// </summary>
        /// <returns></returns>
        private List<string> GetPrefList()
        {
            var prefList = new List<string>();
            
            IDataReader reader; 
            string sql = " select bd_kernel as pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point "+
                         " where 1=1 " + GetWhereWp();

            ExecRead(out reader, sql);
            while (reader.Read())
            {
               prefList.Add(Convert.ToString(reader["pref"]).Trim());
            }
            reader.Close();
            return prefList;
        }

     
    }
}
