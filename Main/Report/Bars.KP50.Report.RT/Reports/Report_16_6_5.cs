using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Castle.Core.Internal;
using Newtonsoft.Json;
using STCLINE.KP50.DataBase;
using Bars.KP50.Report.RT.Properties;
using Constants = STCLINE.KP50.Global.Constants;

namespace Bars.KP50.Report.RT.Reports
{
    class Report1665 : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.6.5 Список по домам поквартирно"; }
        }

        public override string Description
        {
            get { return "Список по домам поквартирно"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                List<ReportGroup> result = new List<ReportGroup>();
                result.Add(ReportGroup.Reports);
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        /// <summary>Управляющие компании</summary>
        private int Areas { get; set; }

        /// <summary>Улица</summary>
        private string Raions { get; set; }

        /// <summary>Улица</summary>
        private string Streets { get; set; }

        /// <summary>Дом</summary>
        private string Houses { get; set; }


        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new AreaParameter(false),
                new AddressParameter()
            };
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("dat_s", DateTime.Today.AddDays(-DateTime.Today.Day + 1));
            report.SetParameterValue("dat_po", DateTime.Today.AddMonths(1).AddDays(-DateTime.Today.Day));
            report.SetParameterValue("printDate", DateTime.Now.ToLongDateString());
            report.SetParameterValue("printTime", DateTime.Now.ToLongTimeString());
        }

        protected override byte[] Template
        {
            get { return Resources.Spis_dom; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        protected override void PrepareParams()
        {
            AddressParameterValue adr = JsonConvert.DeserializeObject<AddressParameterValue>(UserParamValues["Address"].Value.ToString());

            if (!adr.Raions.IsNullOrEmpty())
            {
                Raions = "and r.nzp_raj in (" + String.Join(",", adr.Raions.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray()) + ") ";
            }

            if (!adr.Streets.IsNullOrEmpty())
            {
                Streets = "and u.nzp_ul in (" + String.Join(",", adr.Streets.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray()) + ") ";
            }

            if (!adr.Houses.IsNullOrEmpty())
            {
                List<string> goodHouses = adr.Houses.FindAll(x => x.Trim() != "" && x.Contains("'") == false);
                if (!goodHouses.IsNullOrEmpty())
                    Houses = "and d.ndom in (" + String.Join(",", goodHouses.Select(x => "'" + x + "'").ToArray()) + ") ";
            }
            Areas = UserParamValues["Areas"].GetValue<int>();
        }

        public override DataSet GetData()
        {
            var areas = GetWhereAdr("k.");
            var sql = new StringBuilder();

            sql.Append(" insert into t_kvar_params (nzp, dat_s, dat_po, ob_s, gil_s, propis, privat) " +
                " select p1.nzp, p1.dat_s, p1.dat_po, p1.val_prm" + DBManager.sConvToNum + " as ob_s, p2.val_prm" + DBManager.sConvToNum + " as gil_as, " + 
                " p3.val_prm" + DBManager.sConvToInt + " as propis, p4.val_prm" + DBManager.sConvToInt + " as privat " +
                " from (select nzp, dat_s, dat_po, val_prm from " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_1 where nzp_prm = 4) as p1 " +
                " left join (select nzp, val_prm from " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_1 where nzp_prm = 6) as p2 on (p1.nzp=p2.nzp) " +
                " left join (select nzp, val_prm from " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_1 where nzp_prm = 5) as p3 on (p2.nzp=p3.nzp) " +
                " left join (select nzp, val_prm from " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_1 where nzp_prm = 8) as p4 on (p3.nzp=p4.nzp) ");
            ExecSQL(sql.ToString());

            sql.Remove(0, sql.Length);
            sql.Append(" select area, town, rajon, ulica, ndom, nkvar, nkvar_n, fio, p.ob_s, p.gil_s, p.propis, p.privat " +
                " from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_town t, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, " +
                ReportParams.Pref + DBManager.sDataAliasRest + "s_area a, " +
                " t_kvar_params p " +
                " where t.nzp_town = r.nzp_town " +
                " and r.nzp_raj = u.nzp_raj " +
                " and u.nzp_ul = d.nzp_ul " +
                " and d.nzp_dom = k.nzp_dom " +
                " and k.nzp_kvar = p.nzp " +
                " and k.nzp_area = a.nzp_area " +
                " and p.dat_s <= " + DBManager.sCurDate +
                " and p.dat_po >= " + DBManager.sCurDate + " " +
                  Raions + Streets + Houses + areas);

            DataTable dt = ExecSQLToTable(sql.ToString());
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        /// <summary>
        /// Получить условия органичения по УК
        /// </summary>
        /// <returns></returns>
        private string GetWhereAdr(string tablePrefix)
        {
            return Areas!=0 ? " and " + tablePrefix + "nzp_area in (" + Areas + ") " : String.Empty;
        }

        /// <summary>
        /// Получить условия органичения по банкам данных
        /// </summary>
        /// <returns></returns>
        private string GetwhereWp()
        {
            string whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            return whereWp;
        }

        protected override void CreateTempTable()
        {
            var sql = new StringBuilder();
            sql.Append("create temp table t_kvar_params ("+
                " nzp integer, " +
                " dat_s date, " +
                " dat_po date, " +
                " ob_s "+ DBManager.sDecimalType + "(14,2), " +
                " gil_s "+ DBManager.sDecimalType + "(14,2), " +
                " propis integer, " +
                " privat integer) " 
                );
            ExecSQL(sql.ToString());
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_kvar_params ");
        }

    }
}
