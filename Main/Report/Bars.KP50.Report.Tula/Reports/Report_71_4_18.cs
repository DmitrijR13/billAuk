using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Report.Tula.Properties;
using STCLINE.KP50.Interfaces;
using globalsUtils = STCLINE.KP50.Global.Utils;

namespace Bars.KP50.Report.Tula.Reports
{
    public class Report71004018 : BaseSqlReportCounterFlow
    {
        public override string Name
        {
            get { return "71.4.18 Сведения о расходах по приборам учета по домам"; }
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
            get { return Resources.Report_71_4_18; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> {ReportKind.Base, ReportKind.ListLC}; }
        }

        #region Параметры

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

        /// <summary>Заголовок отчета</summary>
        private string TerritoryHeader { get; set; }

        /// <summary>Услуги</summary>
        private string ServiceHeader { get; set; }

        /// <summary>
        /// Дата начала периода
        /// </summary>
        private DateTime DatUchet { get; set; }

        /// <summary>
        /// Дата окончания периода
        /// </summary>
        private DateTime DatUchetPo { get; set; }

        /// <summary>
        /// Список схем данных
        /// </summary>
        private List<string> PrefList { get; set; }

        /// <summary>
        /// Список периодов учета счетчиков
        /// </summary>
        private List<DateTime> Periods { get; set; }

        /// <summary>
        /// Список услуг
        /// </summary>
        private List<_Service> ListService { get; set; }

        /// <summary>
        /// Ограничение по услугам
        /// </summary>
        private string ServClause { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private string SuppClause { get; set; }

        /// <summary>
        /// День начала периода учета
        /// </summary>
        private DateTime CurMonth { get; set; }
        
        /// <summary>
        /// Текущий префикс
        /// </summary>
        private string CurPrefData { get; set; }

        // 1-Все, 2-МКД, 3-Частный сектор
        private int IsMkd { get; set; }

        /// <summary>
        /// Тип ПУ 1-все, 2-ИПУ, 3-ГПУ, 4-ОДПУ
        /// </summary>
        private int TypePU { get; set; } 
        #endregion

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter
                {
                    Name = "Месяц с",
                    Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month
                },
                new YearParameter
                {
                    Name = "Год с",
                    Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year
                },
                new MonthParameter
                {
                    Name = "Месяц по",
                    Code = "Month1",
                    Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month
                },
                new YearParameter
                {
                    Name = "Год по",
                    Code = "Year1",
                    Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year
                },
                new BankSupplierParameter(),
                new ServiceParameter(),

                new ComboBoxParameter
                {

                    Code = "typePU",
                    Name = "Тип ПУ",
                    Value = "1",
                    StoreData = new List<object>
                    {
                        new {Id = "1", Name = "Все"},
                        new {Id = "2", Name = "ОДПУ"},
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

        public override DataSet GetData()
        {
            ServClause = GetWhereServ("a.");
            SuppClause = GetWhereSupp("a.nzp_supp"); 
            
            PrefList = GetPrefList();
            GetSelectedKvars();

            DataTable table = new DataTable();
            table.TableName = "Q_master";
            table.Columns.Add("nzp_serv", typeof (string));
            table.Columns.Add("service", typeof (string));
            table.Columns.Add("measure", typeof (string));
            table.Columns.Add("ulica", typeof (string));
            table.Columns.Add("ndom", typeof (string));
            table.Columns.Add("nkor", typeof (string));
            table.Columns.Add("dom_rashod", typeof (decimal));
            table.Columns.Add("negil_rashod", typeof (decimal));
            table.Columns.Add("kvar_rashod", typeof (decimal));
            table.Columns.Add("ipu_ngp_cnt", typeof (decimal));
            table.Columns.Add("group_rashod", typeof (decimal));
            table.Columns.Add("dat_uchet", typeof (string));

            table.Columns.Add("dat_ust", typeof(string));
            table.Columns.Add("num_cnt", typeof(string));
            table.Columns.Add("dat_prov", typeof(string));

            // периоды учета                                 
            //---------------------------------------------------
            Periods = new List<DateTime>();
            var dt = DatUchet;
            while (dt <= DatUchetPo)
            {
                Periods.Add(dt);
                dt = dt.AddMonths(1);
            }

            ListService = GetLisServ(ServClause, PrefList);

            foreach (_Service serv in ListService)
            {
                foreach (DateTime curDate in Periods)
                {
                    CurMonth = curDate;
                    
                    switch (TypePU)
                    {
                        case 1:
                            CalculateOdpu(curDate, serv);
                            CalculateIpu(curDate, serv);
                            CalculateGpu(curDate, serv);
                            break;                        
                        case 2:
                            GetOdpuInfo(curDate, serv);
                            break;
                    }
                }
            }

            MyDataReader reader;

            ExecSQL(" create index ix_tmpd_012 on tmp_rashod (nzp_dom) ", false);
            ExecSQL(DBManager.sUpdStat + "  tmp_rashod ", false);

            string sql = "";

            if (TypePU == 2)
            {
                sql = " Select distinct t.nzp_serv, t.service, t.measure, num_cnt, dat_prov, dat_ust, " +
                "   (case when sr.rajon = '-' then trim(town)||'\'||trim(s.ulica) else trim(sr.rajon)||'\'||trim(s.ulica) end) as ulica, " +
                "   d.nzp_dom, d.idom, d.ndom, d.nkor, t.dat_uchet, t.rashod, t.ngp_cnt " +
                "   0.00 as ipu_rashod, 0.00 as ipu_ngp_cnt, 0.00 as gpu_rashod " +
                " From tmp_rashod t , " +
                ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica s, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon sr, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "s_town st " +
                " where t.nzp_dom   = d.nzp_dom " +
                "   and d.nzp_ul    = s.nzp_ul " +
                "   and s.nzp_raj   = sr.nzp_raj " +
                "   and sr.nzp_town = st.nzp_town " +
                " Order by t.service, t.measure, ulica, d.idom, d.ndom, d.nkor, t.dat_uchet ";
            }
            else
            {
                sql = " Select distinct t.nzp_serv, t.service, t.measure, num_cnt, dat_prov, dat_ust, " +
                " (case when sr.rajon = '-' then trim(town)||' '||trim(s.ulica) else trim(sr.rajon)||' '||trim(s.ulica) end) as ulica, " +
                " d.nzp_dom, d.idom, d.ndom, d.nkor, t.dat_uchet, " +
                // ОДПУ
                " (Select  d.rashod From tmp_rashod d where d.nzp_dom = t.nzp_dom and d.nzp_type = 1 and d.nzp_serv = t.nzp_serv and d.dat_uchet = t.dat_uchet) as odpu_rashod, " +
                " (Select d.ngp_cnt From tmp_rashod d where d.nzp_dom = t.nzp_dom and d.nzp_type = 1 and d.nzp_serv = t.nzp_serv and d.dat_uchet = t.dat_uchet) as odpu_ngp_cnt, " +
                // ИПУ
                " (Select  i.rashod From tmp_rashod i where i.nzp_dom = t.nzp_dom and i.nzp_type = 3 and i.nzp_serv = t.nzp_serv and i.dat_uchet = t.dat_uchet) as ipu_rashod, " +
                " (Select i.ngp_cnt From tmp_rashod i where i.nzp_dom = t.nzp_dom and i.nzp_type = 3 and i.nzp_serv = t.nzp_serv and i.dat_uchet = t.dat_uchet) as ipu_ngp_cnt, " +
                // ГПУ
                " (Select  g.rashod From tmp_rashod g where g.nzp_dom = t.nzp_dom and g.nzp_type = 2 and g.nzp_serv = t.nzp_serv and g.dat_uchet = t.dat_uchet) as gpu_rashod " +
                " From tmp_rashod t , " +
                ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica s, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon sr, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "s_town st " +
                " where t.nzp_dom=d.nzp_dom " +
                "   and d.nzp_ul=s.nzp_ul " +
                "   and s.nzp_raj=sr.nzp_raj " +
                "   and sr.nzp_town=st.nzp_town " +
                " Order by t.service, 3, ulica, d.idom, d.ndom, d.nkor, t.dat_uchet ";
            }
            ExecRead(out reader, sql);

            var cv = new CounterVal();
            while (reader.Read())
            {
                Decimal odpuRashod = 0;
                Decimal odpuNgpCnt = 0;
                Decimal ipuRashod = 0;
                Decimal ipuNgpCnt = 0;
                Decimal gpuRashod = 0;
                string odpuDatUst = "";
                
                if (reader["nzp_serv"] != DBNull.Value) cv.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                if (reader["service"] != DBNull.Value) cv.service = Convert.ToString(reader["service"]).Trim();
                if (reader["measure"] != DBNull.Value) cv.measure = Convert.ToString(reader["measure"]).Trim();
                if (reader["ulica"] != DBNull.Value) cv.ulica = Convert.ToString(reader["ulica"]).Trim();
                if (reader["ndom"] != DBNull.Value) cv.ndom = Convert.ToString(reader["ndom"]).Trim();
                if (reader["nkor"] != DBNull.Value) cv.nkor = Convert.ToString(reader["nkor"]).Trim();
                if (reader["dat_uchet"] != DBNull.Value)
                    cv.dat_uchet = Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString();

                if (reader["odpu_rashod"] != DBNull.Value) odpuRashod = Convert.ToDecimal(reader["odpu_rashod"]);
                if (reader["odpu_ngp_cnt"] != DBNull.Value) odpuNgpCnt = Convert.ToDecimal(reader["odpu_ngp_cnt"]);

                if (reader["ipu_rashod"] != DBNull.Value) ipuRashod = Convert.ToDecimal(reader["ipu_rashod"]);
                if (reader["ipu_ngp_cnt"] != DBNull.Value) ipuNgpCnt = Convert.ToDecimal(reader["ipu_ngp_cnt"]);

                if (reader["gpu_rashod"] != DBNull.Value) gpuRashod = Convert.ToDecimal(reader["gpu_rashod"]);

                if (reader["dat_prov"] != DBNull.Value) cv.dat_prov = Convert.ToDateTime(reader["dat_prov"]).ToShortDateString();
                if (reader["dat_ust"] != DBNull.Value) odpuDatUst = Convert.ToDateTime(reader["dat_ust"]).ToShortDateString();
                if (reader["num_cnt"] != DBNull.Value) cv.num_cnt = Convert.ToString(reader["num_cnt"]).Trim();

                table.Rows.Add(
                    cv.nzp_serv,
                    cv.service,
                    cv.measure,
                    cv.ulica,
                    cv.ndom,
                    cv.nkor,

                    odpuRashod,
                    odpuNgpCnt,
                    ipuRashod,
                    ipuNgpCnt,
                    gpuRashod,
                    cv.dat_uchet,
                    odpuDatUst,
                    cv.num_cnt,
                    cv.dat_prov);
            }

            reader.Close();
            ExecSQL(" Drop table tmp_rashod ", false);   

            PrefList.Clear();
            Periods.Clear();    

            DataSet fDataSet = new DataSet();
            fDataSet.Tables.Add(table);   

            return fDataSet;
        }


        /// <summary>
        /// Получить выборку квартир с ограничением по поставщику
        /// </summary>
        private void GetSelectedKvar()
        {
            ExecSQL(" Drop table selected_kvar ", false);
            
            string sql = "Create temp table selected_kvar (nzp_kvar integer, nzp_dom integer)" + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            if (!String.IsNullOrEmpty(SupplierHeader))
            {
                sql = " insert into selected_kvar (nzp_kvar,nzp_dom) " +
                      " select a.nzp_kvar, b.nzp_dom " +
                      " from " + CurPrefData + "tarif a, sel_kvar b" +
                      " where is_actual=1  and a.nzp_kvar=b.nzp_kvar " +
                      "   and a.dat_s < " + globalsUtils.EStrNull(DatUchetPo.AddMonths(1).ToShortDateString()) +
                      "   and a.dat_po >= " + globalsUtils.EStrNull(DatUchet.ToShortDateString()) + " " +
                      SuppClause + 
                      GetWhereServ("a.") + 
                      GetMkdClause(CurPrefData);
                ExecSQL(sql);
            }
            else
            {
                sql = " insert into selected_kvar (nzp_kvar,nzp_dom ) " +
                      " select distinct nzp_kvar,nzp_dom  from sel_kvar where nzp_kvar>0 " + GetMkdClause(CurPrefData);
                ExecSQL(sql);
            }
            ExecSQL("create index ix_tskvs_01 on selected_kvar (nzp_kvar)");
            ExecSQL("create index ix_tskvs_03 on selected_kvar (nzp_dom)");
            ExecSQL(DBManager.sUpdStat + " selected_kvar");
        }

        private void GetOdpuInfo(DateTime period, _Service serv)
        {
            string sql = "";
            string dat_s = globalsUtils.EStrNull(CurMonth.AddDays(1).ToShortDateString());
            string dat_po = globalsUtils.EStrNull(CurMonth.AddMonths(1).ToShortDateString());
            
            foreach (string curPref in PrefList)
            {
                CurPrefData = curPref + DBManager.sDataAliasRest;

                // ... создать временную таблицу
                CreateTempTableInsertPu();

                GetSelectedKvar();

                sql = " insert into tmp_insert_pu (nzp_dom, nzp_counter, cnt_stage, mmnog, num_cnt, dat_prov) " +
                    " Select k.nzp_dom, cs.nzp_counter, t.cnt_stage, t.mmnog, cs.num_cnt, cs.dat_prov " +
                    " From selected_kvar k," +
                    curPref + DBManager.sKernelAliasRest + "s_counttypes t, " +
                    CurPrefData + "counters_spis cs " +
                    " Where cs.nzp_type = 1 " +
                    "   and cs.dat_close is null " +
                    "   and cs.nzp = k.nzp_dom " + 
                    "   and cs.nzp_cnttype = t.nzp_cnttype " +
                    "   and cs.nzp_serv = " + serv.nzp_serv + 
                    " group by 1,2,3,4,5,6 ";
                ExecSQL(sql);

                CreateTempTableInsertPuIndex();

                // ... дата установки
                sql = " UPDATE tmp_insert_pu t SET " +
                    " dat_ust = (SELECT cast(val_prm as date) FROM " + CurPrefData + "prm_17 p" +
                    "   WHERE p.nzp = t.nzp_counter and p.nzp_prm = 2025 and p.is_actual = 1 " +
                    "       and " + globalsUtils.EStrNull(CurMonth.ToShortDateString()) + " between p.dat_s and p.dat_po " + DBManager.Limit1 + ")" +
                    " where exists (select 1 from " + CurPrefData + "prm_17 p" + " where p.nzp = t.nzp_counter and p.nzp_prm = 2025 and p.is_actual = 1 " + DBManager.Limit1 + ")";
                ExecSQL(sql, true);

                // ... получить расходы ПУ
                GetCounterFlow("tmp_insert_pu", 1, curPref, CurMonth, CurMonth);

                // ... сохранить расходы
                sql = " Insert into tmp_rashod (nzp_dom, nzp_counter, num_cnt, dat_prov, dat_ust, rashod, ngp_cnt, nzp_serv, dat_uchet, service, measure, nzp_type) " +
                    " Select t.nzp_dom, t.nzp_counter, t.num_cnt, t.dat_prov, t.dat_ust, t.rashod, t.ngp_cnt + t.ngp_lift, " +
                    serv.nzp_serv + "," +
                    STCLINE.KP50.Global.Utils.EStrNull(period.ToShortDateString()) + ", " +
                    STCLINE.KP50.Global.Utils.EStrNull(serv.service) + ", " +
                    STCLINE.KP50.Global.Utils.EStrNull(serv.ed_izmer) + ", 1 " +
                    " From tmp_insert_pu t ";
                ExecSQL(sql);
            }

            ExecSQL(" Drop table tmp_insert_pu ", false);
            ExecSQL(" Drop table selected_kvar ", false);
        }

        /// <summary>
        /// Подсчитать данные по групповым приборам учета
        /// </summary>
        private void CalculateGpu(DateTime period, _Service serv)
        {
            string sql = "";
            string dat_s = globalsUtils.EStrNull(CurMonth.AddDays(1).ToShortDateString());
            string dat_po = globalsUtils.EStrNull(CurMonth.AddMonths(1).ToShortDateString());
            
            foreach (string curPref in PrefList)
            {
                CurPrefData = curPref + DBManager.sDataAliasRest;
                
                // ... создать временную таблицу
                CreateTempTableInsertPu();

                // ... получить информацию по ПУ
                GetSelectedKvar();

                sql = " insert into tmp_insert_pu (nzp_dom, nzp_counter, cnt_stage, mmnog) " +
                    " Select k.nzp_dom, cs.nzp_counter, t.cnt_stage, t.mmnog " +
                    " From selected_kvar k," +
                    curPref + DBManager.sKernelAliasRest + "s_counttypes t, " +
                    CurPrefData + "counters_spis cs " +
                    " Where cs.nzp_type = 2 " + //ГПУ
                    "     and cs.dat_close is null " +
                    "     and exists (select 1 from " + CurPrefData + "counters_link cl where cl.nzp_counter = cs.nzp_counter and cl.nzp_kvar = k.nzp_kvar) " +
                    "     and cs.nzp_cnttype = t.nzp_cnttype " +
                    "     and cs.nzp_serv = " + serv.nzp_serv + // услуга
                    " group by 1,2,3,4 ";
                ExecSQL(sql, true);

                CreateTempTableInsertPuIndex();

                // ... получить расходы ПУ
                GetCounterFlow("tmp_insert_pu", 2, curPref, CurMonth, CurMonth);

                // ... сохранить расходы
                sql = " Insert into tmp_rashod (nzp_dom, rashod, nzp_serv, dat_uchet, service, measure, nzp_type) " +
                    " Select t.nzp_dom, sum(t.rashod), " +
                    serv.nzp_serv + "," +
                    STCLINE.KP50.Global.Utils.EStrNull(period.ToShortDateString()) + ", " +
                    STCLINE.KP50.Global.Utils.EStrNull(serv.service) + ", " +
                    STCLINE.KP50.Global.Utils.EStrNull(serv.ed_izmer) + ", 2 " +
                    " From tmp_insert_pu t " +
                    " group by 1";
                ExecSQL(sql);
            }

            ExecSQL(" Drop table tmp_insert_pu ", false);
            ExecSQL(" Drop table selected_kvar ", false);
        }
        
        /// <summary>
        /// Подсчитать расходы по индивидуальным ПУ
        /// </summary>
        private void CalculateIpu(DateTime period, _Service serv)
        {
            string sql = "";
            string dat_s = globalsUtils.EStrNull(CurMonth.AddDays(1).ToShortDateString());
            string dat_po = globalsUtils.EStrNull(CurMonth.AddMonths(1).ToShortDateString());

            foreach (string curPref in PrefList)
            {
                CurPrefData = curPref + DBManager.sDataAliasRest;

                // ... создать временную таблицу
                CreateTempTableInsertPu();

                // ... получить информацию по ПУ
                GetSelectedKvar();
                
                sql = " insert into tmp_insert_pu (nzp_dom, nzp_counter, cnt_stage, mmnog) " +
                    " Select k.nzp_dom, cs.nzp_counter, t.cnt_stage, t.mmnog " +
                    " From selected_kvar k," +
                    curPref + DBManager.sKernelAliasRest + "s_counttypes t, " +
                    CurPrefData + "counters_spis cs " +
                    " Where cs.nzp_type = 3 " + //ИПУ
                    "     and cs.dat_close is null " +
                    "     and cs.nzp = k.nzp_kvar " + //дом
                    "     and cs.nzp_cnttype = t.nzp_cnttype " +
                    "     and cs.nzp_serv = " + serv.nzp_serv + // услуга
                    " group by 1,2,3,4 ";
                ExecSQL(sql, true);

                CreateTempTableInsertPuIndex();

                // ... получить расходы ПУ
                GetCounterFlow("tmp_insert_pu", 3, curPref, CurMonth, CurMonth);

                // ... сохранить расходы
                sql = " Insert into tmp_rashod (nzp_dom, rashod, nzp_serv, dat_uchet, service, measure, ngp_cnt, nzp_type) " +
                    " Select t.nzp_dom, sum(t.rashod), " +
                    serv.nzp_serv + "," + 
                    STCLINE.KP50.Global.Utils.EStrNull(period.ToShortDateString()) + ", " +
                    STCLINE.KP50.Global.Utils.EStrNull(serv.service) + ", " +
                    STCLINE.KP50.Global.Utils.EStrNull(serv.ed_izmer) + ", " +
                    " 0.00, 3 " + 
                    " From tmp_insert_pu t " + 
                    " group by 1";
                ExecSQL(sql);
            }

            ExecSQL(" Drop table tmp_insert_pu ", false);
            ExecSQL(" Drop table selected_kvar ", false);
        }

        /// <summary>
        /// Получение данных по Общедомовым приборам учета
        /// </summary>
        private void CalculateOdpu(DateTime period, _Service serv)
        {
            string sql = "";
            string dat_s = globalsUtils.EStrNull(CurMonth.AddDays(1).ToShortDateString());
            string dat_po = globalsUtils.EStrNull(CurMonth.AddMonths(1).ToShortDateString());

            foreach (string curPref in PrefList)
            {
                CurPrefData = curPref + DBManager.sDataAliasRest;

                // ... создать временную таблицу
                CreateTempTableInsertPu();

                GetSelectedKvar();

                sql = " insert into tmp_insert_pu (nzp_dom, nzp_counter, cnt_stage, mmnog) " +
                    " Select k.nzp_dom, cs.nzp_counter, t.cnt_stage, t.mmnog " +
                    " From selected_kvar k," +
                    curPref + DBManager.sKernelAliasRest + "s_counttypes t, " +
                    CurPrefData + "counters_spis cs " +
                    " Where cs.nzp_type = 1 " + //ОДПУ
                    "   and cs.dat_close is null " +
                    "   and cs.nzp = k.nzp_dom " + //дом
                    "   and cs.nzp_cnttype = t.nzp_cnttype " +
                    "   and cs.nzp_serv = " + serv.nzp_serv + // услуга
                    " group by 1,2,3,4 ";
                ExecSQL(sql);

                CreateTempTableInsertPuIndex();

                // ... получить расходы ПУ
                GetCounterFlow("tmp_insert_pu", 1, curPref, CurMonth, CurMonth);

                // ... сохранить расходы
                sql = " Insert into tmp_rashod (nzp_dom, rashod, ngp_cnt, nzp_serv, dat_uchet, service, measure, nzp_type) " +
                    " Select t.nzp_dom, sum(t.rashod), sum(t.ngp_cnt + t.ngp_lift), " +
                    serv.nzp_serv + "," +
                    STCLINE.KP50.Global.Utils.EStrNull(period.ToShortDateString()) + ", " +
                    STCLINE.KP50.Global.Utils.EStrNull(serv.service) + ", " +
                    STCLINE.KP50.Global.Utils.EStrNull(serv.ed_izmer) + ", 1 " +
                    " From tmp_insert_pu t " +
                    " group by 1";
                ExecSQL(sql);
            }

            ExecSQL(" Drop table tmp_insert_pu ", false);
            ExecSQL(" Drop table selected_kvar ", false);
        }

        private void CreateTempTableInsertPu()
        {
            ExecSQL(" Drop table tmp_insert_pu ", false);
            string sql = " Create temp table tmp_insert_pu (" +
                  " nzp_dom integer," +
                  " nzp_counter integer," +
                  " cnt_stage integer," +
                  " num_cnt     char(20), " +
                  " dat_prov    date, " +
                  " dat_ust     date, " +
                  " mmnog " + DBManager.sDecimalType + "(8,4)," +
                  " ngp_cnt  " + DBManager.sDecimalType + "(14,4)," +
                  " ngp_lift " + DBManager.sDecimalType + "(14,4)," +
                  " val_cnt " + DBManager.sDecimalType + "(14,4)," +
                  " val_cnt_pred " + DBManager.sDecimalType + "(14,4) default 0," +
                  " dat_uchet Date," +
                  " dat_uchet_pred Date, " +
                  " rashod   " + DBManager.sDecimalType + "(20,8) default 0.00 " +
                  ")" + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        private void CreateTempTableInsertPuIndex()
        {
            ExecSQL("create index ix_tipp_01 on tmp_insert_pu(nzp_counter)");
            ExecSQL(DBManager.sUpdStat + " tmp_insert_pu");
        }

        /// <summary>
        /// Получить список услуг
        /// </summary>
        /// <param name="servClause"></param>
        /// <param name="prefList"></param>
        /// <returns></returns>
        private List<_Service> GetLisServ(string servClause, List<string> prefList)
        {
            var listService = new List<_Service>();
            MyDataReader reader;

            string sql = " Select distinct s.nzp_serv, s.service, m.measure " +
                         " From " + Points.Pref + DBManager.sKernelAliasRest + "s_counts a, " +
                         Points.Pref + DBManager.sKernelAliasRest + "s_measure m, " +
                         Points.Pref + DBManager.sKernelAliasRest + "services s " +
                         " Where a.nzp_measure = m.nzp_measure " +
                         " and a.nzp_serv = s.nzp_serv " + servClause +
                         " Order by s.service";

            ExecRead(out reader, sql);
            while (reader.Read())
            {
                var service = new _Service();
                if (reader["nzp_serv"] != DBNull.Value) service.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                if (reader["service"] != DBNull.Value) service.service = Convert.ToString(reader["service"]).Trim();
                if (reader["measure"] != DBNull.Value) service.ed_izmer = Convert.ToString(reader["measure"]).Trim();

                int totalCounterCount = 0;

                #region по всем банкам подсчитать количество приборов учета для конкретной услуги

                //----------------------------------------------------------------------------
                foreach (string curPref in prefList)
                {
                    sql = " Select " + DBManager.sNvlWord + "(count(*), 0) " +
                          " From " + curPref + DBManager.sDataAliasRest + "counters_spis cs " +
                          " Where is_actual = 1 and cs.nzp_serv = " + service.nzp_serv + 
                          "     and cs.dat_close is null ";
                    object obj = ExecScalar(sql);

                    totalCounterCount += obj != null ? Convert.ToInt32(obj) : 0;
                }
                //----------------------------------------------------------------------------

                #endregion

                if (totalCounterCount > 0) listService.Add(service);
            }

            reader.Close();

            return listService;
        }

        /// <summary>
        /// Получить список выбранных квартир
        /// </summary>
        private void GetSelectedKvars()
        {
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                using (IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata))
                {
                    if (!DBManager.OpenDb(connWeb, true).result) return;

                    string tSpls = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter +
                                   "t" + ReportParams.User.nzp_user + "_spls";
                    if (TempTableInWebCashe(tSpls))
                    {
                        string sql = " insert into sel_kvar (nzp_kvar, nzp_dom) " +
                                     " select nzp_kvar, nzp_dom from " + tSpls;
                        ExecSQL(sql);
                    }
                }
            }
            else
            {

                string sql = " insert into sel_kvar (nzp_kvar, nzp_dom) " +
                             " select nzp_kvar, nzp_dom " +
                             " from " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar " +
                             " Where nzp_kvar>0 " +
                             GetWhereWp();

                ExecSQL(sql);
            }
            ExecSQL("create index ix_tskvs_02 on sel_kvar (nzp_kvar)");
            ExecSQL(DBManager.sUpdStat + " sel_kvar");

        }

     

        /// <summary>
        /// Ограничение на МКД
        /// </summary>
        /// <param name="pref"></param>
        /// <returns></returns>
        private string GetMkdClause(string pref)
        {
            if (IsMkd == 1) return "";

            return " and nzp_dom " + (IsMkd == 2 ? " in " : " not in") +
                   " (select nzp from " + pref + "prm_2 a " +
                   " where nzp_prm=2030 and is_actual=1 " +
                   "        and val_prm='1')";
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
            whereServ = Services != null
                ? Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ","))
                : ReportParams.GetRolesCondition(Constants.role_sql_serv);
            whereServ = whereServ.TrimEnd(',');
            whereServ = !String.IsNullOrEmpty(whereServ)
                ? " AND " + tablePrefix + "nzp_serv in (" + whereServ + ")"
                : String.Empty;
            if (!String.IsNullOrEmpty(whereServ))
            {
                string sql = " SELECT service " +
                             " from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services a" +
                             " WHERE a.nzp_serv > 1 " + whereServ;
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
            var months = new[]
            {
                "", "Январь", "Февраль",
                "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь",
                "Октябрь", "Ноябрь", "Декабрь"
            };
            report.SetParameterValue("month", months[MonthS]);
            report.SetParameterValue("year", YearS); 

            string mkd;
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
                default: mkd = "";
                    break;
            }

            string typePU;
            switch (TypePU)
            {
                case 1:
                    typePU = "Все";
                    break;
                case 2:
                    typePU = "ОДПУ";
                    break;
                default: typePU = "";
                    break;
            }

            // дом
            string pdom;
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
                pdom = "По списку ЛС " + mkd;
            else
                pdom = " " + mkd;
            // месяц
            string pmonth;

            if (DatUchet == DatUchetPo) pmonth = DatUchet.ToString("MMMM yyyy").ToLower() + " г.";
            else
                pmonth = DatUchet.ToString("MMMM yyyy").ToLower() + " г."
                         + " - " + DatUchetPo.ToString("MMMM yyyy").ToLower() + " г.";

            report.SetParameterValue("pmonth", pmonth);            
            
            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(ServiceHeader) ? "Услуги: " + ServiceHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(pdom) ? "Дом: " + pdom + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(typePU) ? "Тип ПУ: " + typePU + "\n" : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);
            report.SetParameterValue("typePU",typePU);
        }


        /// <summary>
        /// Получение ограничения на список схем
        /// </summary>
        /// <returns></returns>
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
            IsMkd = UserParamValues["isMkd"].Value.To<int>();
            TypePU = UserParamValues["typePU"].Value.To<int>();

            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString()); 
        }


        /// <summary>
        /// Создать временные таблицы
        /// </summary>
        protected override void CreateTempTable()
        {
            string sql = " create temp table sel_kvar(" +
                         " nzp_kvar integer, " +
                         " nzp_dom integer) " +
                         DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = "Create temp table tmp_rashod" +
                  "( nzp_serv   integer, " +
                  "  nzp_dom    integer, " +
                  "  nzp_counter    integer, " +
                  "  dat_uchet  date, " +
                  "  service    char(100), " +
                  "  measure    char(20), " +
                  "  ulica      char(40), " +
                  "  idom       integer, " +
                  "  ndom       char(15), " +
                  "  nkor       char(15), " +
                  " rashod      " + DBManager.sDecimalType + "(18,4), " +
                  " ngp_cnt     " + DBManager.sDecimalType + "(14,4), " +
                  " nzp_type    integer, " +
                  " num_cnt     char(20), " +
                  " dat_prov  date, " +
                  " dat_ust  date " +
                  ") " + DBManager.sUnlogTempTable;
            ExecSQL(sql, true);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table sel_kvar ", true);
            ExecSQL(" drop table tmp_rashod ", true);

            if (TempTableInWebCashe("tmp_dom_itog"))
                ExecSQL(" Drop table tmp_dom_itog ", false);
            
            if (TempTableInWebCashe("tmp_insert_pu"))
                ExecSQL(" Drop table tmp_insert_pu ", false); 
        }


        /// <summary>
        /// Получение префиксов БД
        /// </summary>
        /// <returns></returns>
        private List<string> GetPrefList()
        {
            var prefList = new List<string>();
            MyDataReader reader;
            string sql = " select bd_kernel as pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " where nzp_wp>1 " + GetWhereWp();

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
