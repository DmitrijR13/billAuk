using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Base.Parameters;
using Bars.KP50.Report.Main.Properties;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Report.Main.Reports
{
    class IncomeRevise : BaseSqlReport
    {
        public override string Name
        {
            get { return "Сверка поступлений"; }
        }

        public override string Description
        {
            get { return "Сверка поступлений"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get { return new List<ReportGroup> { ReportGroup.Finans }; }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Income_revise; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>С</summary>
        protected DateTime DatS { get; set; }

        /// <summary>По</summary>
        protected DateTime DatPo { get; set; }

        /// <summary>Место формирования</summary>
        protected int Place { get; set; }

        /// <summary>Территория</summary>
        protected List<int> Banks { get; set; }

        /// <summary>Расчетный счет</summary>
        protected int RS { get; set; }

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            DateTime datS = curCalcMonthYear != null
                ? new DateTime(Convert.ToInt32(curCalcMonthYear.Rows[0]["yearr"]),
                    Convert.ToInt32(curCalcMonthYear.Rows[0]["month_"]), 1)
                : DateTime.Now;
            DateTime datPo = curCalcMonthYear != null
                ? datS.AddMonths(1).AddDays(-1)
                : DateTime.Now;
            return new List<UserParam>
            {
                new PeriodParameter(datS, datPo),
                new BankParameter(),
                new FormingPlaceParameter { MultiSelect = false },
                new AccountParameter { MultiSelect = false }
            };
        }

        public override DataSet GetData()
        {
            string sql = string.Empty;
            for (int i = DatS.Year - 1; i <= DatPo.Year + 1; i++)
            {
                string pack = Points.Pref + "_fin_" + (i - 2000).ToString("00") + DBManager.tableDelimiter + "pack";
                string packLs = Points.Pref + "_fin_" + (i - 2000).ToString("00") + DBManager.tableDelimiter + "pack_ls";
                if (TempTableInWebCashe(pack) && TempTableInWebCashe(packLs))
                {
                    sql = " insert into t_revise (nzp_wp, nzp_bank, prefix_ls, dat_pack, payer," +
                          " count_kvit, g_sum_ls, sum_ls, penya, komiss, allover) " +
                          " select nzp_wp, sb.nzp_bank, " +
                          " substr(a.pkod" + DBManager.sConvToVarChar + ",1,3) as prefix_ls, dat_pack," +
                          " (case when payer is null then ( case when sb.nzp_bank is not null then 'Кассы' else (case when kod_sum=50 or kod_sum=49 then 'Сторонние оплаты на счет УК и ПУ' when kod_sum=33 then 'Сторонние оплаты на счет РЦ' end ) end) else payer end) as payer, " +
                          " count(*) as count_kvit, " +
                          " sum(g_sum_ls) as g_sum_ls, " +
                          " sum(0) as sum_ls, sum(0) as penya, sum(0) as komiss, " +
                          " sum(g_sum_ls) as allover  " +
                          " from  " + packLs + " a, " + pack + " b left outer join " +
                          Points.Pref + DBManager.sKernelAliasRest +
                          " s_bank sb on b.nzp_bank=sb.nzp_bank left outer join " +
                          Points.Pref + DBManager.sKernelAliasRest + "s_payer p on sb.nzp_payer = p.nzp_payer, " +
                          Points.Pref + DBManager.sDataAliasRest + "kvar k " +
                          " where " +
                          " coalesce(a.dat_uchet,date(" +
                            STCLINE.KP50.Global.Utils.EStrNull(Points.DateOper.ToShortDateString()) + ")) >= date(" +
                            STCLINE.KP50.Global.Utils.EStrNull(DatS.ToShortDateString()) + ")" +
                            " and coalesce(a.dat_uchet,date(" +
                            STCLINE.KP50.Global.Utils.EStrNull(Points.DateOper.ToShortDateString()) + ")) <= date(" +
                            STCLINE.KP50.Global.Utils.EStrNull(DatPo.ToShortDateString()) + ")"+ 
                          " and a.nzp_pack=b.nzp_pack and a.num_ls=k.num_ls   " +
                          GetWhereWp("k.") +
                          (Place != 0 ? " and p.nzp_payer = " + Place : "") +
                          (RS != 0
                              ? " and " + DBManager.sNvlWord + "(substr(a.pkod" + DBManager.sConvToVarChar +
                                ",1,3),'') = '" + RS + "' "
                              :"") +
                          " group by 1,2,3,4,5,sb.nzp_payer ";
                    ExecSQL(sql);
                }
            }
            sql = " select point, payer, prefix_ls, dat_pack, " +
                  " sum(count_kvit) as count_kvit, " +
                  " sum(g_sum_ls) as g_sum_ls, " +
                  " sum(sum_ls) as sum_ls, sum(penya) as penya, sum(komiss) as komiss, " +
                  " sum(allover) as allover  " +
                  " from   t_revise t, " +
                  Points.Pref + DBManager.sKernelAliasRest + "s_point pt" +
                  " where t.nzp_wp = pt.nzp_wp " +
                  " group by 1,2,3,4 " +
                  " order by point, payer, prefix_ls, dat_pack ";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }
        private string GetWhereWp(string tablePrefix)
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
            return whereWp != String.Empty ? " and " + tablePrefix + "nzp_wp in (" + whereWp + ") " : "";
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("date", DateTime.Now.ToShortDateString());
            report.SetParameterValue("time", DateTime.Now.ToShortTimeString());
            report.SetParameterValue("dats", DatS.ToLongDateString());
            report.SetParameterValue("datpo", DatPo.ToLongDateString());
        }

        protected override void PrepareParams()
        {
            DateTime begin;
            DateTime end;
            var period = UserParamValues["Period"].GetValue<string>();
            PeriodParameter.GetValues(period, out begin, out end);
            DatS = begin;
            DatPo = end;
            Banks = UserParamValues["Banks"].GetValue<List<int>>();
            Place = UserParamValues["Place"].GetValue<int>();
            RS = UserParamValues["RS"].GetValue<int>();
        }

        protected override void CreateTempTable()
        {
            string sql = " create temp table t_revise ( " +
                         " payer character(100), " +
                         " prefix_ls character(3), " +
                         " dat_pack date, " +
                         " nzp_wp integer, " +
                         " nzp_bank integer, " +
                         " count_kvit integer, " +
                         " g_sum_ls " + DBManager.sDecimalType + "(14,2), " +
                         " sum_ls " + DBManager.sDecimalType + "(14,2), " +
                         " penya " + DBManager.sDecimalType + "(14,2), " +
                         " komiss " + DBManager.sDecimalType + "(14,2), " +
                         " allover " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_revise ", true);
        }

    }
}
