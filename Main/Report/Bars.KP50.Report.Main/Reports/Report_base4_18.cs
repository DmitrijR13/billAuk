using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Bars.KP50.Report.Main.Properties;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Report.Main.Reports
{
    public class ReportPU004018 : BaseSqlReport
    {
        public override string Name
        {
            get { return "Базовый - Сведения о расходах по приборам учета (4.18)"; }
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
            get { return Resources.Report4_18; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> {ReportKind.Base, ReportKind.ListLC}; }
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

        /// <summary>Поставщики</summary>
        private List<long> Suppliers { get; set; }

        /// <summary>Управляющие компании</summary>
        private List<int> Areas { get; set; }

        /// <summary>Заголовок отчета</summary>
        private string SupplierHeader { get; set; }

        /// <summary>Услуги</summary>
        private string ServiceHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        private string AreaHeader { get; set; }

        /// <summary>Банки данных</summary>
        private List<int> Banks { get; set; }

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

        private List<string> PrefList { get; set; }

        private List<DateTime> Periods { get; set; }

        private List<_Service> ListService { get; set; }

        private string ServClause { get; set; }

        private string StartDayPeriod { get; set; }
        private string EndDayPeriod { get; set; }

        private string CurPrefData { get; set; }

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
                new SupplierAndBankParameter(),
                new ServiceParameter(),
                new AreaParameter(),
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
                }
            };
        }

        public override DataSet GetData()
        {
            ServClause = GetWhereServ("a.");
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

            // периоды учета                                 
            //---------------------------------------------------
            Periods = new List<DateTime>();
            while (DatUchet <= DatUchetPo)
            {
                Periods.Add(DatUchet);
                DatUchet = DatUchet.AddMonths(1);
            }

            ListService = GetLisServ(ServClause, PrefList);

            foreach (_Service serv in ListService)
            {
                foreach (DateTime curDate  in Periods)
                {
                    StartDayPeriod = STCLINE.KP50.Global.Utils.EStrNull(curDate.AddDays(1).ToShortDateString());
                    EndDayPeriod = STCLINE.KP50.Global.Utils.EStrNull(curDate.AddMonths(1).ToShortDateString());

                    CalculateOdpu(curDate, serv);
                    CalculateIpu(curDate, serv);
                    CalculateGpu(curDate, serv);

                    
                }
            }

            MyDataReader reader;

            ExecSQL(" create index ix_tmpd_012 on tmp_rashod (nzp_dom) ", false);
            ExecSQL(DBManager.sUpdStat + "  tmp_rashod ", false);


            string sql =
                " Select distinct t.nzp_serv, t.service, t.measure, " +
                " (case when sr.rajon = '-' then trim(town)||'\'||trim(s.ulica) " +
                " else trim(sr.rajon)||'\'||trim(s.ulica) end) as ulica, " +
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
                " and d.nzp_ul=s.nzp_ul " +
                " and s.nzp_raj=sr.nzp_raj " +
                " and sr.nzp_town=st.nzp_town " +
                " Order by t.service, 3, d.idom, d.ndom, d.nkor, t.dat_uchet ";
            ExecRead(out reader, sql);


            var cv = new CounterVal();

            while (reader.Read())
            {
                Decimal odpuRashod = 0;
                Decimal odpuNgpCnt = 0;
                Decimal ipuRashod = 0;
                Decimal ipuNgpCnt = 0;
                Decimal gpuRashod = 0;

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
                    cv.dat_uchet
                    );
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
            string sql = "Create temp table selected_kvar (nzp_kvar integer, nzp_dom integer)" + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            if (!String.IsNullOrEmpty(SupplierHeader))
            {
                sql = " insert into selected_kvar (nzp_kvar,nzp_dom) " +
                      " select a.nzp_kvar, b.nzp_dom " +
                      " from " + CurPrefData + "tarif a, sel_kvar b" +
                      " where is_actual=1  and a.nzp_kvar=b.nzp_kvar " +
                      " and a.dat_uchet between " +
                      STCLINE.KP50.Global.Utils.EStrNull(DatUchet.AddDays(1).ToShortDateString()) +
                      " and " + STCLINE.KP50.Global.Utils.EStrNull(DatUchetPo.AddMonths(1).ToShortDateString()) +
                      "  " + GetWhereSupp("") + GetWhereServ("a.");
                ExecSQL(sql);
            }
            else
            {
                sql = " insert into selected_kvar (nzp_kvar,nzp_dom ) " +
                      " select nzp_kvar,nzp_dom  from sel_kvar ";
                ExecSQL(sql);
            }
            ExecSQL("create index ix_tskvs_01 on selected_kvar (nzp_kvar)");
            ExecSQL("create index ix_tskvs_03 on selected_kvar (nzp_dom)");
            ExecSQL(DBManager.sUpdStat + " selected_kvar");
        }


        /// <summary>
        /// Подсчитать данные по групповым приборам учета
        /// </summary>
        private void CalculateGpu(DateTime period, _Service serv)
        {
            string sql;

            #region 11. Очистить таблицу

            ExecSQL(" Delete From tmp_rashod_dom", true);

            #endregion

            foreach (string curPref in PrefList)
            {
                CurPrefData = curPref + DBManager.sDataAliasRest;
                sql = " Create temp table tmp_insert_pu (" +
                      " ulica char(100)," +
                      " idom integer, " +
                      " ndom char(10)," +
                      " nkor char(10)," +
                      " nzp_dom integer," +
                      " nzp_counter integer," +
                      " cnt_stage integer," +
                      " mmnog " + DBManager.sDecimalType + "(8,4)," +
                      " ngp_cnt " + DBManager.sDecimalType + "(14,4)," +
                      " ngp_lift " + DBManager.sDecimalType + "(14,4)," +
                      " val_cnt " + DBManager.sDecimalType + "(14,4)," +
                      " val_cnt_pred " + DBManager.sDecimalType + "(14,4)," +
                      " dat_uchet Date," +
                      " dat_uchet_pred Date)" + DBManager.sUnlogTempTable;
                ExecSQL(sql);

                #region 12. Получить данные по расходу
                GetSelectedKvar();

                sql = " insert into tmp_insert_pu(nzp_dom, nzp_counter, " +
                      "     cnt_stage, mmnog, val_cnt, dat_uchet) " +
                      " Select " +
                      "     k.nzp_dom, cs.nzp_counter, t.cnt_stage, t.mmnog, " +
                      "     v.val_cnt, max(v.dat_uchet) as dat_uchet " +
                      " From selected_kvar k," +
                      curPref + DBManager.sKernelAliasRest + "s_counttypes t, " +
                      CurPrefData + "counters_spis cs , " +
                      CurPrefData + "counters_group v " +
                      " Where cs.nzp_type = 2 " + //ГПУ
                      " and cs.nzp = k.nzp_dom " + //дом
                      // соединение таблиц
                      " and cs.nzp_cnttype = t.nzp_cnttype " +
                      " and cs.nzp_counter = v.nzp_counter " +
                      // услуга
                      " and cs.nzp_serv = " + serv.nzp_serv +
                      // текущее показание
                      " and v.dat_uchet between " + StartDayPeriod + " and " + EndDayPeriod +
                      " and v.val_cnt is not null " +
                      " and v.is_actual = 1 " +
                      " group by 1,2,3,4,5 ";
                ExecSQL(sql);

                ExecSQL("create index ix_tipp_01 on tmp_insert_pu(nzp_counter)");
                ExecSQL(DBManager.sUpdStat + " tmp_insert_pu");


                sql = " update tmp_insert_pu set dat_uchet_pred = (select max(p.dat_uchet) " +
                      " from " + CurPrefData + "counters_group p " +
                      " where tmp_insert_pu.nzp_counter =p.nzp_counter " +
                      "     and p.is_actual<>100 and p.dat_uchet<= " +
                      STCLINE.KP50.Global.Utils.EStrNull(period.ToShortDateString()) +
                      "     and p.val_cnt is not null) ";

                ExecSQL(sql);


                sql = " update tmp_insert_pu set val_cnt_pred = " + DBManager.sNvlWord + "((select max(p.val_cnt) " +
                      "                 from " + CurPrefData + "counters_group p " +
                      "                 where tmp_insert_pu.nzp_counter =p.nzp_counter " +
                      "                         and p.is_actual<>100 " +
                      "                         and tmp_insert_pu.dat_uchet_pred = p.dat_uchet" +
                      "                         and p.val_cnt is not null),0) " +
                      " where dat_uchet_pred is not null ";
                ExecSQL(sql);

                #endregion

                #region 13. Сохранить данные


                sql =
                    " Insert into tmp_rashod_dom (nzp_dom, nzp_counter, cnt_stage, mmnog, val_cnt_pred, dat_uchet_pred, val_cnt, dat_uchet) " +
                    " Select t.nzp_dom, t.nzp_counter, t.cnt_stage, t.mmnog, t.val_cnt_pred, t.dat_uchet_pred, t.val_cnt, t.dat_uchet " +
                    " From tmp_insert_pu t ";
                ExecSQL(sql);



                #endregion

                ExecSQL(" Drop table tmp_insert_pu ", false);
                ExecSQL(" Drop table selected_kvar ", false);
            }

            #region 14. Подсчитать расходы




            sql = " Create temp table tmp_dom_itog( " +
                  " nzp_dom integer," +
                  " rashod " + DBManager.sDecimalType + "(18,4)," +
                  " ngp_cnt " + DBManager.sDecimalType + "(18,4)) " +
                  DBManager.sUnlogTempTable;

            ExecSQL(sql);


            sql = " insert into tmp_dom_itog(nzp_dom, rashod) " +
                  " Select t.nzp_dom, " +
                  " sum(case when t.val_cnt >= " +
                  DBManager.sNvlWord + "(t.val_cnt_pred,0) then (t.val_cnt - " +
                  DBManager.sNvlWord + "(t.val_cnt_pred,0)) * t.mmnog " +
                  " else (pow(10, t.cnt_stage) + t.val_cnt - " +
                  DBManager.sNvlWord + "(t.val_cnt_pred,0)) * t.mmnog end) as rashod " +
                  "  From tmp_rashod_dom t " +
                  " Group by 1 ";

            ExecSQL(sql);


            #endregion

            #region 15. Сохранить итоги

            sql =
                " Insert into tmp_rashod (nzp_serv, nzp_dom, dat_uchet, service, measure, rashod, nzp_type) " +
                " Select " + serv.nzp_serv + ", t.nzp_dom, " +
                STCLINE.KP50.Global.Utils.EStrNull(period.ToShortDateString()) + ", " +
                STCLINE.KP50.Global.Utils.EStrNull(serv.service) + ", " +
                STCLINE.KP50.Global.Utils.EStrNull(serv.ed_izmer) + ", " +
                "  t.rashod, 2 From tmp_dom_itog t ";
            ExecSQL(sql);

            ExecSQL(" Drop table tmp_dom_itog ", false);
            #endregion

        }



        /// <summary>
        /// Подсчитать расходы по индивидуальным ПУ
        /// </summary>
        private void CalculateIpu(DateTime period, _Service serv)
        {
            string sql;



            ExecSQL(" Delete From tmp_rashod_dom", true);


            foreach (string curPref in PrefList)
            {
                CurPrefData = curPref + DBManager.sDataAliasRest;
                sql = " Create temp table tmp_insert_pu (" +
                      " ulica char(100)," +
                      " idom integer, " +
                      " ndom char(10)," +
                      " nkor char(10)," +
                      " nzp_dom integer," +
                      " nzp_counter integer," +
                      " cnt_stage integer," +
                      " mmnog " + DBManager.sDecimalType + "(8,4)," +
                      " ngp_cnt " + DBManager.sDecimalType + "(14,4)," +
                      " ngp_lift " + DBManager.sDecimalType + "(14,4)," +
                      " val_cnt " + DBManager.sDecimalType + "(14,4)," +
                      " val_cnt_pred " + DBManager.sDecimalType + "(14,4) default 0," +
                      " dat_uchet Date," +
                      " dat_uchet_pred Date)" + DBManager.sUnlogTempTable;
                ExecSQL(sql, true);

                #region 7. Получить данные по расходу

                GetSelectedKvar();

                sql = " insert into tmp_insert_pu(nzp_dom, nzp_counter, " +
                      " cnt_stage, mmnog, ngp_cnt, val_cnt, dat_uchet) " +
                      " Select k.nzp_dom, cs.nzp_counter, t.cnt_stage, " +
                      " t.mmnog, 0 , " +
                      " v.val_cnt, max(v.dat_uchet) as dat_uchet  " +
                      "  From selected_kvar k," +
                      curPref + DBManager.sKernelAliasRest + "s_counttypes t, " +
                      CurPrefData + "counters_spis cs " + ", " +
                      CurPrefData + "counters v " +
                      " Where cs.nzp_type = 3 " + //ИПУ
                      " and cs.nzp = k.nzp_kvar " + //дом
                      // соединение таблиц
                      " and cs.nzp_cnttype = t.nzp_cnttype " +
                      " and cs.nzp_counter = v.nzp_counter " +
                      // услуга
                      " and cs.nzp_serv = " + serv.nzp_serv +
                      // текущее показание
                      " and v.dat_uchet between " + StartDayPeriod + " and " + EndDayPeriod +
                      " and v.val_cnt is not null " +
                      " and v.is_actual = 1 " +

                      " group by 1,2,3,4,5,6 ";
                ExecSQL(sql, true);

                ExecSQL("create index ix_tipp_01 on tmp_insert_pu(nzp_counter)");
                ExecSQL(DBManager.sUpdStat + " tmp_insert_pu");

                sql = " update tmp_insert_pu set dat_uchet_pred = (select max(p.dat_uchet) " +
                      " from " + CurPrefData + "counters p " +
                      " where tmp_insert_pu.nzp_counter =p.nzp_counter " +
                      "     and p.is_actual<>100 and p.dat_uchet<=" +
                      STCLINE.KP50.Global.Utils.EStrNull(period.ToShortDateString()) + "  " +
                      "     and p.val_cnt is not null) ";

                ExecSQL(sql);


                sql = " update tmp_insert_pu set val_cnt_pred =" + DBManager.sNvlWord + "((select max(p.val_cnt) " +
                      "                 from " + CurPrefData + "counters p " +
                      "                 where tmp_insert_pu.nzp_counter =p.nzp_counter " +
                      "                         and p.is_actual<>100 " +
                      "                         and tmp_insert_pu.dat_uchet_pred = p.dat_uchet" +
                      "                         and p.val_cnt is not null),0) " +
                      " where dat_uchet_pred is not null ";
                ExecSQL(sql);

                #endregion

                #region 8. Сохранить данные


                sql =
                    " Insert into tmp_rashod_dom (nzp_dom, nzp_counter, cnt_stage, mmnog, ngp_cnt, val_cnt_pred, dat_uchet_pred, val_cnt, dat_uchet) " +
                    " Select t.nzp_dom, t.nzp_counter, t.cnt_stage, t.mmnog, t.ngp_cnt, t.val_cnt_pred, t.dat_uchet_pred, t.val_cnt, t.dat_uchet " +
                    " From tmp_insert_pu t ";
                ExecSQL(sql);


                #endregion

                ExecSQL(" Drop table tmp_insert_pu ", false);
                ExecSQL(" Drop table selected_kvar ", false);
            }

            #region 9. Подсчитать расходы



            sql = " Create temp table tmp_dom_itog( " +
                  " nzp_dom integer," +
                  " rashod " + DBManager.sDecimalType + "(18,4)," +
                  " ngp_cnt " + DBManager.sDecimalType + "(18,4)) " +
                  DBManager.sUnlogTempTable;

            ExecSQL(sql, true);

            sql = " insert into tmp_dom_itog( nzp_dom, rashod,  ngp_cnt) " +
                  " Select t.nzp_dom, " +
                  "     sum(case when t.val_cnt >= t.val_cnt_pred " +
                  "  then (t.val_cnt - " + DBManager.sNvlWord + "(t.val_cnt_pred,0)) * t.mmnog - " +
                  DBManager.sNvlWord + "(t.ngp_cnt,0) - " +
                  DBManager.sNvlWord + "(t.ngp_lift,0)    else (pow(10, t.cnt_stage) + t.val_cnt - " +
                  DBManager.sNvlWord + "(t.val_cnt_pred,0)) * t.mmnog - " +
                  DBManager.sNvlWord + "(t.ngp_cnt,0) - " +
                  DBManager.sNvlWord + "(t.ngp_lift,0) end) as rashod, " +
                  " sum(" + DBManager.sNvlWord + "(t.ngp_cnt,0) + " +
                  DBManager.sNvlWord + "(t.ngp_lift,0)) as ngp_cnt " +
                  " From tmp_rashod_dom t " +
                  " Group by 1  ";

            ExecSQL(sql);

            //---------------------------------------------------------------------------------------------------------

            #endregion

            #region 10. Сохранить итоги

            //---------------------------------------------------------------------------------------------------------
            sql =
                " Insert into tmp_rashod (nzp_serv, nzp_dom, dat_uchet, service, measure, rashod, ngp_cnt, nzp_type) " +
                " Select " + serv.nzp_serv + ", t.nzp_dom, " +
                STCLINE.KP50.Global.Utils.EStrNull(period.ToShortDateString()) + ", " +
                STCLINE.KP50.Global.Utils.EStrNull(serv.service) + ", " +
                STCLINE.KP50.Global.Utils.EStrNull(serv.ed_izmer) + ", " +
                " t.rashod, t.ngp_cnt, 3 From tmp_dom_itog t ";
            ExecSQL(sql);
            ExecSQL(" Drop table tmp_dom_itog ", false);

            #endregion

        }



        /// <summary>
        /// Получение данных по Общедомовым приборам учета
        /// </summary>
        private void CalculateOdpu(DateTime period, _Service serv)
        {
            string sql;



            ExecSQL(" Delete From tmp_rashod_dom", true);


            foreach (string curPref in PrefList)
            {
                CurPrefData = curPref + DBManager.sDataAliasRest;
                #region 2. Получить данные по расходу

                sql = " Create temp table tmp_insert_pu (" +
                      " nzp_dom integer," +
                      " nzp_counter integer," +
                      " cnt_stage integer," +
                      " mmnog " + DBManager.sDecimalType + "(8,4)," +
                      " ngp_cnt " + DBManager.sDecimalType + "(14,4)," +
                      " ngp_lift " + DBManager.sDecimalType + "(14,4)," +
                      " val_cnt " + DBManager.sDecimalType + "(14,4)," +
                      " val_cnt_pred " + DBManager.sDecimalType + "(14,4)," +
                      " dat_uchet Date," +
                      " dat_uchet_pred Date)" + DBManager.sUnlogTempTable;
                ExecSQL(sql);

                GetSelectedKvar();

                sql = " insert into tmp_insert_pu(nzp_dom, nzp_counter, " +
                      " cnt_stage, mmnog, ngp_cnt, ngp_lift, val_cnt, dat_uchet) " +
                      " Select k.nzp_dom, cs.nzp_counter, t.cnt_stage, t.mmnog, " +
                      DBManager.sNvlWord + "(v.ngp_cnt, 0) as ngp_cnt, " +
                      DBManager.sNvlWord + "(v.ngp_lift, 0) as ngp_lift, " +
                      "     v.val_cnt, max(v.dat_uchet) as dat_uchet " +
                      " From selected_kvar k," +
                      "      " + curPref + DBManager.sKernelAliasRest + "s_counttypes t, " +
                      "      " + CurPrefData + "counters_spis cs, " +
                      "      " + CurPrefData + "counters_dom v " +
                      " Where cs.nzp_type = 1 " + //ОДПУ
                      " and cs.nzp = k.nzp_dom " + //дом
                      // соединение таблиц
                      " and cs.nzp_cnttype = t.nzp_cnttype " +
                      " and cs.nzp_counter = v.nzp_counter " +
                      // услуга
                      " and cs.nzp_serv = " + serv.nzp_serv +
                      // текущее показание
                      " and v.dat_uchet between " + StartDayPeriod + " and " + EndDayPeriod +
                      " and v.val_cnt is not null " +
                      " and v.is_actual = 1 " +
                      " group by 1,2,3,4,5,6,7 ";
                ExecSQL(sql);

                ExecSQL("create index ix_tipp_01 on tmp_insert_pu(nzp_counter)");
                ExecSQL(DBManager.sUpdStat + " tmp_insert_pu");

                sql = " update tmp_insert_pu set dat_uchet_pred = (select max(p.dat_uchet) " +
                      " from " + CurPrefData + "counters_dom p " +
                      " where tmp_insert_pu.nzp_counter =p.nzp_counter " +
                      "     and p.is_actual<>100 and p.dat_uchet<= " +
                      STCLINE.KP50.Global.Utils.EStrNull(period.ToShortDateString()) +
                      "     and p.val_cnt is not null) ";

                ExecSQL(sql);


                sql = " update tmp_insert_pu set val_cnt_pred = " + DBManager.sNvlWord + "((select max(p.val_cnt) " +
                      "                 from " + CurPrefData + "counters_dom p " +
                      "                 where tmp_insert_pu.nzp_counter =p.nzp_counter " +
                      "                         and p.is_actual<>100 " +
                      "                         and tmp_insert_pu.dat_uchet_pred = p.dat_uchet" +
                      "                         and p.val_cnt is not null),0) " +
                      " where dat_uchet_pred is not null ";
                ExecSQL(sql, true);



                #endregion

                #region 3. Сохранить данные

                sql = " Insert into tmp_rashod_dom (nzp_dom, " +
                      " nzp_counter, cnt_stage, mmnog, ngp_cnt, ngp_lift, val_cnt_pred, " +
                      " dat_uchet_pred, val_cnt, dat_uchet) " +
                      " Select t.nzp_dom, t.nzp_counter, " +
                      " t.cnt_stage, t.mmnog, t.ngp_cnt, t.ngp_lift, t.val_cnt_pred, " +
                      " t.dat_uchet_pred, t.val_cnt, t.dat_uchet " +
                      " From tmp_insert_pu t ";
                ExecSQL(sql);

                ExecSQL(" Drop table tmp_insert_pu ", false);
                ExecSQL(" Drop table selected_kvar ", false);
                #endregion


            }

            #region 4. Подсчитать расходы

            sql = " Create temp table tmp_dom_itog( " +
                  " nzp_dom integer," +
                  " rashod " + DBManager.sDecimalType + "(18,4)," +
                  " ngp_cnt " + DBManager.sDecimalType + "(14,4)) " + 
                  DBManager.sUnlogTempTable;

            ExecSQL(sql, true);



            sql = " insert into tmp_dom_itog(nzp_dom, rashod,  ngp_cnt) " +
                  " Select  t.nzp_dom, " +
                  "     sum(case when t.val_cnt >= " +
                  DBManager.sNvlWord + "(t.val_cnt_pred,0) then (t.val_cnt - " +
                  DBManager.sNvlWord + "(t.val_cnt_pred,0)) * t.mmnog - " +
                  DBManager.sNvlWord + "(t.ngp_cnt,0) - " +
                  DBManager.sNvlWord + "(t.ngp_lift,0) " +
                  "     else (pow(10, t.cnt_stage) + " +
                  DBManager.sNvlWord + "(t.val_cnt,0) - " +
                  DBManager.sNvlWord + "(t.val_cnt_pred,0)) * t.mmnog - " +
                  DBManager.sNvlWord + "(t.ngp_cnt,0) - " +
                  DBManager.sNvlWord + "(t.ngp_lift,0) end) as rashod, " +
                  " sum(" +
                  DBManager.sNvlWord + "(t.ngp_cnt,0) + " +
                  DBManager.sNvlWord + "(t.ngp_lift,0)) as ngp_cnt " +
                  " From tmp_rashod_dom t " +
                  " Group by 1  ";


            ExecSQL(sql, true);

            //---------------------------------------------------------------------------------------------------------

            #endregion

            #region 5. Сохранить итоги


            sql =
                " Insert into tmp_rashod (nzp_serv, nzp_dom, dat_uchet, service, measure, rashod, ngp_cnt, nzp_type) " +
                " Select " + serv.nzp_serv + ", t.nzp_dom, " +
                STCLINE.KP50.Global.Utils.EStrNull(period.ToShortDateString()) + ", " +
                STCLINE.KP50.Global.Utils.EStrNull(serv.service) + ", " +
                STCLINE.KP50.Global.Utils.EStrNull(serv.ed_izmer) +
                ", " +
                " t.rashod, t.ngp_cnt, 1 " +
                " From tmp_dom_itog t ";
            ExecSQL(sql, true);


            ExecSQL(" Drop table tmp_dom_itog ", false);

            #endregion

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
                          " Where is_actual = 1 and cs.nzp_serv = " + service.nzp_serv;
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
                             GetWhereArea("") +
                             GetWhereWp();

                ExecSQL(sql);
            }
            ExecSQL("create index ix_tskvs_02 on sel_kvar (nzp_kvar)");
            ExecSQL(DBManager.sUpdStat + " sel_kvar");

        }

        /// <summary>
        /// Получить условия органичения по УК
        /// </summary>
        /// <returns></returns>
        private string GetWhereArea(string tablePrefix)
        {
            string whereArea = String.Empty;
            whereArea = Areas != null
                ? Areas.Aggregate(whereArea, (current, nzpArea) => current + (nzpArea + ","))
                : ReportParams.GetRolesCondition(Constants.role_sql_area);
            whereArea = whereArea.TrimEnd(',');
            whereArea = !String.IsNullOrEmpty(whereArea)
                ? " AND " + tablePrefix + "nzp_area in (" + whereArea + ")"
                : String.Empty;
            if (!String.IsNullOrEmpty(whereArea))
            {
                string sql = " SELECT area from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area " +
                             tablePrefix.TrimEnd('.') + " WHERE nzp_area > 0 " + whereArea;
                DataTable area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    AreaHeader += dr["area"].ToString().Trim() + ", ";
                }
                AreaHeader = AreaHeader.TrimEnd(',', ' ');
            }
            return whereArea;
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp(string tablePrefix)
        {
            string whereSupp = String.Empty;
            whereSupp = Suppliers != null
                ? Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","))
                : ReportParams.GetRolesCondition(Constants.role_sql_supp);
            whereSupp = whereSupp.TrimEnd(',');
            whereSupp = !String.IsNullOrEmpty(whereSupp)
                ? " AND " + tablePrefix + "nzp_supp in (" + whereSupp + ")"
                : String.Empty;
            if (!String.IsNullOrEmpty(whereSupp))
            {
                string sql = " SELECT name_supp " +
                             " from " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                             " WHERE nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += dr["name_supp"].ToString().Trim() + ", ";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',', ' ');
            }
            return whereSupp;
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




            // дом
            string pdom;
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
                pdom = "По списку ЛС";
            else
                pdom = "";
            // месяц
            string pmonth;

            if (DatUchet == DatUchetPo) pmonth = DatUchet.ToString("MMMM yyyy").ToLower() + " г.";
            else
                pmonth = DatUchet.ToString("MMMM yyyy").ToLower() + " г."
                         + " - " + DatUchetPo.ToString("MMMM yyyy").ToLower() + " г.";
            report.SetParameterValue("psupplier", SupplierHeader);
            report.SetParameterValue("pservice", ServiceHeader);
            report.SetParameterValue("pdom", pdom);
            report.SetParameterValue("pmonth", pmonth);
            report.SetParameterValue("isSmr", "0");
            report.SetParameterValue("parea", AreaHeader);
            report.SetParameterValue("pcounter", IsNew ? "Новые" : "Все");

        }

        private string GetWhereWp()
        {
            string whereWp = String.Empty;
            if (Banks != null)
            {
                whereWp = Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","));
            }
            else
            {
                whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            }
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
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
            Areas = UserParamValues["Areas"].Value.To<List<int>>();
            IsNew = UserParamValues["isNew"].Value.To<int>() == 1;

            var values =
                JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;

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
                  "  dat_uchet  date, " +
                  "  service    char(100), " +
                  "  measure    char(20), " +
                  "  ulica      char(40), " +
                  "  idom       integer, " +
                  "  ndom       char(15), " +
                  "  nkor       char(15), " +
                  " rashod      " + DBManager.sDecimalType + "(18,4), " +
                  " ngp_cnt     " + DBManager.sDecimalType + "(14,4), " +
                  " nzp_type    integer " +
                  ") " + DBManager.sUnlogTempTable;
            ExecSQL(sql, true);

            sql = "Create temp table tmp_rashod_dom" +
                  "(ulica          char(40), " +
                  " idom           integer, " +
                  " ndom           char(15), " +
                  " nkor           char(15), " +
                  " nzp_dom        integer, " +
                  " nzp_counter    integer, " +
                  " cnt_stage      integer, " +
                  " mmnog          " + DBManager.sDecimalType + "(8,4), " +
                  " ngp_cnt        " + DBManager.sDecimalType + "(14,4), " +
                  " ngp_lift       " + DBManager.sDecimalType + "(14,4), " +
                  " val_cnt_pred   " + DBManager.sDecimalType + "(18,4), " +
                  " dat_uchet_pred date, " +
                  " val_cnt        " + DBManager.sDecimalType + "(18,4), " +
                  " dat_uchet      date " +
                  ") " + DBManager.sUnlogTempTable;

            ExecSQL(sql);

        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table sel_kvar ", true);
            ExecSQL(" drop table tmp_rashod ", true);
            ExecSQL(" drop table tmp_rashod_dom ", true);
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
