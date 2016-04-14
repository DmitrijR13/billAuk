using System;
using System.Data;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Base.Parameters;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using Bars.KP50.Report.Tula.Properties;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Tula.Reports
{
    class Report7124 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.2.4 Временно отсутствующие граждане"; }
        }

        public override string Description
        {
            get { return "Временно отсутствующие граждане"; }
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

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_71_2_4; }
        }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; } 
        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }
        /// <summary>Список постфиксов банков в БД</summary>  
        private List<string> PrefBanks { get; set; }  
        /// <summary> с расчетного дня </summary>
        protected DateTime DatS { get; set; }   
        /// <summary> по расчетный год </summary>
        protected DateTime DatPo { get; set; }   
        /// <summary>Адрес</summary>
        protected AddressParameterValue Address { get; set; }
        /// <summary> предоставлены документы </summary> 1-не предоставлены, 2- предоставлены , 3 - все
        protected int predost { get; set; }   

        public override List<UserParam> GetUserParams()
        {
            DateTime datS=DateTime.Now;
            DateTime datPo=DateTime.Now;            
            return new List<UserParam>
            {
                new PeriodParameter(datS, datPo),
                new BankParameter(),
                new AddressParameter(),
                                 new ComboBoxParameter(false)
                {
                    Code = "Docks",
                    Name = "Подтверждающие документы",
                    Value = 1,
                    StoreData = new List<object>
                    {
                        new { Id = 1, Name = "не предоставлены" },
                        new { Id = 2, Name = "предоставлены" },
                        new { Id = 3, Name = "все" }
                    }   
                }
            };
        }

        protected override void PrepareParams()
        {
            Banks = UserParamValues["Banks"].GetValue<List<int>>();
            Address = UserParamValues["Address"].GetValue<AddressParameterValue>();
            predost = UserParamValues["Docks"].GetValue<int>();
            var period = UserParamValues["Period"].GetValue<string>();
            DateTime d1;
            DateTime d2;
            PeriodParameter.GetValues(period, out d1, out d2);
            DatS = d1;
            DatPo = d2;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            string predostdock = string.Empty;
            switch (predost)
            {
                case 1:
                    predostdock ="не предоставиввших документы ";
                    break;
                case 2:
                    predostdock ="предоставиввших документы ";
                    break;
            }
            report.SetParameterValue("predost", predostdock);
            report.SetParameterValue("dat_s", DatS.ToShortDateString());
            report.SetParameterValue("dat_po", DatPo.ToShortDateString());
            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);
            report.SetParameterValue("date",DateTime.Now.ToShortDateString());
        }



        public override DataSet GetData()
        {
            string fpref = ReportParams.Pref + DBManager.sDataAliasRest;
            string adr = GetWhereAdr();
            GetwhereWp();
            string kvar;
            var sql = new StringBuilder();
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                GetSelectedKvars();
                kvar = " selected_kvars kv ";
            }
            else
                kvar = ReportParams.Pref + DBManager.sDataAliasRest + " kvar kv ";

            foreach (var pref in PrefBanks)
            {
                sql.Remove(0, sql.Length);
                sql.Append(" INSERT INTO t_report_71_2_4 (nzp_kvar, fam, ima, otch, dat_rog, dat_s, dat_po, id_departure_types)");
                sql.Append(" SELECT t.nzp_kvar,  k.fam, k.ima, k.otch, k.dat_rog, t.dat_s, t.dat_po, t.id_departure_types  ");
                sql.Append(" FROM " + pref + DBManager.sDataAliasRest+"gil_periods t,");
                sql.Append(  pref + DBManager.sDataAliasRest + "kart k, ");
                sql.Append(kvar);
                sql.Append(" WHERE t.nzp_gilec=k.nzp_gil and kv.nzp_kvar=t.nzp_kvar ");
                sql.Append(" AND  dat_s<= '" + DatPo.ToShortDateString() + "'");
                sql.Append(" AND  dat_po>= '" + DatS.ToShortDateString() + "'");
                sql.Append(" AND  t.is_actual <> 100 AND t.nzp_tkrt=2 ");
                sql.Append(" AND  nzp_gilec=nzp_gil ");
                switch (predost)
                {
                    case 1:
                        sql.Append(" AND t.no_podtv_docs = 1 ");
                        break;
                    case 2:
                        sql.Append(" AND t.no_podtv_docs <> 1 ");
                        break;      
                }
                ExecSQLToTable(sql.ToString()); 
            }

            ExecSQL(" create index svod_index on t_report_71_2_4 (nzp_kvar) ");
            ExecSQL(DBManager.sUpdStat + " t_report_71_2_4 ");

            var rsql = " SELECT  dt.type_name as cel, fam, ima, otch, dat_rog, dat_s, dat_po, " +
                       " num_ls, TRIM(town) AS town, TRIM(rajon) AS rajon,  TRIM(ulica) AS ulica, TRIM(ndom) AS ndom, TRIM(nkor) AS nkor, TRIM(nkvar) AS nkvar" +
                       " FROM t_report_71_2_4 tp left outer join " + fpref + "s_departure_types dt on tp.id_departure_types = dt.id, " +
                              fpref + "kvar k INNER JOIN " + fpref + "dom d ON d.nzp_dom = k.nzp_dom " +
                              " INNER JOIN " + fpref + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                              " INNER JOIN " + fpref + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                              " INNER JOIN " + fpref + "s_town t ON t.nzp_town = r.nzp_town "+
                       " WHERE tp.nzp_kvar=k.nzp_kvar "+ adr +
                       " ORDER BY town, rajon, ulica, idom, ndom, ikvar, nkvar ";

            DataTable dt = ExecSQLToTable(rsql);
            dt.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }


        private string GetwhereWp()
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
            PrefBanks = new List<string>();
            if (!string.IsNullOrEmpty(whereWp))
            {
                TerritoryHeader = String.Empty;
                string sql = " SELECT point,bd_kernel FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWp;
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
            return whereWp;
        }

        private string GetWhereAdr()
        {
            // var result = GetwhereTeritorry();
            var result = string.Empty;
            string rajon = string.Empty,
                street = string.Empty,
                house = string.Empty;

            if (Address.Raions != null)
            {
                rajon = Address.Raions.Aggregate(rajon, (current, nzpRajon) => current + (nzpRajon + ","));
                rajon = rajon.TrimEnd(',');
            }
            if (Address.Streets != null)
            {
                street = Address.Streets.Aggregate(street, (current, nzpStreet) => current + (nzpStreet + ","));
                street = street.TrimEnd(',');
            }
            if (Address.Houses != null)
            {
                house = Address.Houses.Aggregate(house, (current, nzpHouse) => current + (nzpHouse + ","));
                house = house.TrimEnd(',');
            }

            result += !string.IsNullOrEmpty(rajon) ? " AND u.nzp_raj IN ( " + rajon + ") " : string.Empty;
            result += !string.IsNullOrEmpty(street) ? " AND u.nzp_ul IN ( " + street + ") " : string.Empty;
            result += !string.IsNullOrEmpty(house) ? " AND d.nzp_dom IN ( " + house + ") " : string.Empty;

            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                int startIndex = Constants.cons_Webdata.IndexOf("Database=", StringComparison.Ordinal) + 9;
                int endIndex = Constants.cons_Webdata.Substring(startIndex, Constants.cons_Webdata.Length - startIndex).IndexOf(";", StringComparison.Ordinal);
                var tSpls = Constants.cons_Webdata.Substring(startIndex, endIndex) + DBManager.tableDelimiter + "t" + ReportParams.User.nzp_user + "_spls";
                if (TempTableInWebCashe(tSpls))
                {
                    result = " and k.nzp_kvar in (select nzp_kvar from " + tSpls + ")";
                    return result;
                }
            }
            return result;
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
                        string sql = " insert into selected_kvars (nzp_kvar, num_ls, nzp_dom) " +
                                     " select nzp_kvar, num_ls, nzp_dom from " + tSpls;
                        ExecSQL(sql);
                        ExecSQL("create index ix_sel_kvar_09 on selected_kvars(nzp_kvar)");
                        ExecSQL(DBManager.sUpdStat + " selected_kvars");
                        return true;
                    }
                }
            }
            return false;
        }
        protected override void CreateTempTable()
        {
            const string sql = " CREATE TEMP TABLE t_report_71_2_4(" +
                         " NZP_KVAR integer," +
                         " fam CHAR(100)," +
                         " ima CHAR(100)," +
                         " otch CHAR(100)," +
                         " dat_rog DATE," +
                         " dat_s DATE," +
                         " dat_po DATE," +
                         " id_departure_types integer," +
                         " osnovanie CHAR(100)" +
                         "  )" + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                ExecSQL(" create temp table selected_kvars(nzp_kvar integer, nzp_dom integer, nzp_wp integer, num_ls integer) " + DBManager.sUnlogTempTable);
            } 
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_report_71_2_4 ");
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                ExecSQL(" DROP TABLE  selected_kvars ");
            } 
        }
    }
}
