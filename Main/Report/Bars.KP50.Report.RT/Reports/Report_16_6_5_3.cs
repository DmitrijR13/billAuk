using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using Bars.KP50.Report.RT.Properties;
using  STCLINE.KP50.Global;

namespace Bars.KP50.Report.RT.Reports
{
    public class Report16653 : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.6.5.3 Список по домам поквартирно (с учетом временно выбывших, прибывших)"; }
        }

        public override string Description
        {
            get { return "6.5.3.1 Список по домам поквартирно ( с учетом временно выбывших, прибывших )"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get { return new List<ReportGroup>(0); }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_16_6_5_3; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>Период С</summary>
        protected DateTime DateS { get; set; }

        /// <summary>Период по</summary>
        protected DateTime DatePo { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Rajon { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AreaHeader { get; set; }

        /// <summary>Управляющие компании</summary>
        protected string RajonHeader { get; set; }

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
                new RaionsParameter(),
                new AreaParameter()
            };
        }

        public override DataSet GetData()
        {
            IDataReader reader;
            string sql;

            string whereArea = GetWhereArea("kv.");
            string whereRajon = GetWhereRaj("sr.");


            #region выборка в temp таблицу

            sql = " SELECT bd_kernel AS pref " +
                  " FROM " + DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter+"s_point " +
                  " WHERE nzp_wp>1 " + GetWhereWp();

            ExecRead(out reader, sql);

            while (reader.Read())
            {
                if (reader["pref"] != null)
                {
                    string pref = reader["pref"].ToStr().Trim();
                    string calcGkuTable = pref + "_charge_" + (DateS.Year - 2000).ToString("00") + DBManager.tableDelimiter + "calc_gku_" + DateS.Month.ToString("00");
                    string prefData = pref + DBManager.sDataAliasRest;

                    if (TempTableInWebCashe(calcGkuTable))
                    {

                        sql = " INSERT INTO t_lst_house_sel(nzp_kvar, fio, nkvar, nkvar_n, num_ls, ikvar, nzp_ul, nzp_dom, ndom, idom, nkor, ulica) " +
                              " SELECT * FROM ( " +
                                 " SELECT  kv.nzp_kvar, kv.fio, kv.nkvar, kv.nkvar_n, kv.num_ls, kv.ikvar, su.nzp_ul, d.nzp_dom, ndom, idom, nkor, ulica " +
                                 " FROM " + prefData + "kvar kv, " + prefData + "dom d, " + prefData + "s_ulica su " +
                                 " WHERE kv.nzp_dom=d.nzp_dom " +
                                         whereArea +
                                   " AND d.nzp_ul=su.nzp_ul ) a" +
                              " WHERE nzp_kvar IN ( " +
                                                " SELECT nzp " +
                                                " FROM " + prefData + "prm_3 b " +
                                                " WHERE a.nzp_kvar=b.nzp " +
                                                " AND nzp_prm=51 " +
                                                " AND is_actual<>100 " +
                                                " AND val_prm='1' " +
                                                " AND dat_s<='" + DateS.ToShortDateString() + "' " +
                                                " AND dat_po>='" + DatePo.ToShortDateString() + "') ";
                        ExecSQL(sql);

                        sql = " INSERT INTO t_lst_house_kv(nzp_kvar, pl_kvar, kolgil) " +
                              " SELECT nzp_kvar, SUM(CAST(REPLACE(val_prm,',','.') AS " + DBManager.sDecimalType + "(14,2))) AS pl_kvar, " +
                                     " (SELECT SUM(gil) " +
                                      " FROM " + calcGkuTable + " cal, " +
                                         prefData + "kvar kv  " +
                                      " WHERE kv.nzp_kvar=cal.nzp_kvar " +
                                        " AND kv.nzp_kvar=a.nzp_kvar " +
                                          whereArea + " ) AS kolgil " +
                              " FROM t_lst_house_sel a LEFT OUTER JOIN " + prefData + "prm_1 b ON (a.nzp_kvar=b.nzp) " +
                              " WHERE nzp_prm=4" +
                              " AND is_actual<>100 " +
                              " AND dat_s<='" + DatePo.ToShortDateString() + "' " +
                              " AND dat_po>='" + DateS.ToShortDateString() + "' " +
                              " GROUP BY 1,3 ";
                        ExecSQL(sql);

                        sql = " INSERT INTO t_lst_house_pv(nzp_kvar, pol_kvar) " +
                              " SELECT nzp_kvar,SUM(CAST(REPLACE(val_prm,',','.') AS " + DBManager.sDecimalType + "(14,2))) AS pol_kvar " +
                              " FROM t_lst_house_sel a LEFT OUTER JOIN " + prefData + "prm_1 b ON (a.nzp_kvar=b.nzp) " +
                              " WHERE nzp_prm=6 " +
                                " AND is_actual<>100 " +
                                " AND dat_s<='" + DatePo.ToShortDateString() + "' " +
                                " AND dat_po>='" + DateS.ToShortDateString() + "' " +
                             " GROUP BY 1 ";
                        ExecSQL(sql);

                        sql = " INSERT INTO t_lst_house_priv(nzp_kvar, priv) " +
                              " SELECT nzp_kvar,CAST(val_prm AS INTEGER) AS priv " +
                              " FROM t_lst_house_sel a, " + prefData + "prm_1 b " +
                              " WHERE a.nzp_kvar=b.nzp " +
                                " AND nzp_prm=8 " +
                                " AND is_actual<>100 " +
                                " AND dat_s<='" + DatePo.ToShortDateString() + "' " +
                                " AND dat_po>='" + DateS.ToShortDateString() + "' " +
                              " GROUP BY 1,2 ";
                        ExecSQL(sql);

                        sql = " INSERT INTO t_lst_house_p(nzp_kvar, vp, day_vp) " +
                              " SELECT a.nzp_kvar, " +
                                     " MAX(CAST(p.val_prm AS INTEGER)) AS vp, " +
                                     " SUM((CASE WHEN dat_po>DATE('" + DatePo.ToShortDateString() + "') THEN DATE('" +
                                                                       DatePo.ToShortDateString() + "') ELSE dat_po END) - " +
                             " (CASE WHEN dat_s<DATE('" + DateS.ToShortDateString() + "') THEN DATE('" +
                                                          DateS.ToShortDateString() + "') ELSE dat_s END)+1) AS day_vp  " +
                             " FROM t_lst_house_sel a, " + prefData + "prm_1 p " +
                             " WHERE a.nzp_kvar=p.nzp  " +
                               " AND p.nzp_prm=131 " +
                               " AND p.is_actual<>100  " +
                               " AND p.dat_s<='" + DatePo.ToShortDateString() + "' " +
                               " AND p.dat_po>='" + DateS.ToShortDateString() + "' " +
                               " GROUP BY 1 ";
                        ExecSQL(sql);

                        sql = " INSERT INTO t_lst_house_v(nzp_kvar, vv, day_vv) " +
                              " SELECT a.nzp_kvar, " +
                                     " COUNT(distinct nzp_gilec) AS vv, " +
                                     " SUM((CASE WHEN dat_po>date('" + DatePo.ToShortDateString() + "') THEN DATE('" +
                                                                       DatePo.ToShortDateString() + "') ELSE dat_po END) - " +
                                     " (CASE WHEN dat_s<date('" + DateS.ToShortDateString() + "') THEN DATE('" +
                                                                  DateS.ToShortDateString() + "') ELSE dat_s END)+1) AS day_vv " +
                              " FROM t_lst_house_sel a," + prefData + "gil_periods g " +
                              " WHERE a.nzp_kvar=g.nzp_kvar " +
                                " AND g.is_actual<>100 " +
                                " AND g.dat_s<='" + DatePo.ToShortDateString() + "' " +
                                " AND g.dat_po>='" + DateS.ToShortDateString() + "' " +
#if PG
                                " AND g.dat_po>g.dat_s + INTERVAL '4 DAY' " +
                                " AND g.dat_po>date('01.06.2006') + INTERVAL '4 DAY' " +
#else
                                " AND g.dat_po>g.dat_s+4 UNITS DAY " +
                                " AND g.dat_po>date('01.06.2006') + 4 UNITS DAY " +
#endif
                              " GROUP BY 1 ";
                        ExecSQL(sql);
                    }

                }
            }
            reader.Close();
            #endregion

            sql = " SELECT distinct b.nzp_kvar,ROUND(CAST(b.pl_kvar AS " + DBManager.sDecimalType + "(14,2)),2) AS pl_kvar,b.kolgil, ROUND(CAST(pol_kvar AS " + DBManager.sDecimalType + "(14,2)),2) AS pol_kvar, town, rajon, a.ulica, ndom, idom, nkor, nkvar, ikvar, nkvar_n, " +
                         " num_ls, fio,  priv, vv, vp, day_vv, day_vp " +
                  " FROM  t_lst_house_kv b INNER JOIN t_lst_house_sel a ON a.nzp_kvar=b.nzp_kvar " +
                                         " INNER JOIN t_lst_house_pv c ON a.nzp_kvar=c.nzp_kvar " +
                                         " INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica su ON su.nzp_ul=a.nzp_ul " +
                                         " INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon sr ON su.nzp_raj=sr.nzp_raj " +
                                         " INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "s_town st ON sr.nzp_town = st.nzp_town " +
                                         " LEFT OUTER JOIN t_lst_house_priv t ON (t.nzp_kvar=a.nzp_kvar) " +
                                         " LEFT OUTER JOIN t_lst_house_p ON (a.nzp_kvar=t_lst_house_p.nzp_kvar) " +
                                         " LEFT OUTER JOIN t_lst_house_v ON (a.nzp_kvar=t_lst_house_v.nzp_kvar) " +
                  " WHERE b.pl_kvar IS NOT NULL " +
                      whereRajon +
                    " AND pol_kvar IS NOT NULL " +
                  " ORDER BY town, rajon, ulica, idom, ikvar, nkor, nkvar_n";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;

        }

        /// <summary>
        /// Получить условия органичения по локальному банку
        /// </summary>
        private string GetWhereWp()
        {
            var result = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            return !String.IsNullOrEmpty(result) ? " AND nzp_wp IN (" + result + ") " : "";
        }

        /// <summary>
        /// Получить условия органичения по УК
        /// </summary>
        private string GetWhereArea(string tablePrefix)
        {
            string whereArea = String.Empty;
            if (Areas != null)
            {
                whereArea = Areas.Aggregate(whereArea, (current, nzpArea) => current + (nzpArea + ","));
            }
            else
            {
                whereArea = ReportParams.GetRolesCondition(Constants.role_sql_area);
            }

            whereArea = whereArea.TrimEnd(',');
            if (!String.IsNullOrEmpty(whereArea))
            {
                whereArea = " AND " + tablePrefix + "nzp_area in (" + whereArea + ")";
                AreaHeader = String.Empty;

                string sql = " SELECT area from " +
                             ReportParams.Pref + DBManager.sDataAliasRest + "s_area " + tablePrefix.TrimEnd('.') +
                             " WHERE " + tablePrefix + "nzp_area > 0 " + whereArea;
                DataTable area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    AreaHeader += dr["area"].ToString().Trim() + ",";
                }
                AreaHeader = AreaHeader.TrimEnd(',');
            }
            return whereArea;
        }

        /// <summary>
        /// Получить условия органичения по районам
        /// </summary>
        private string GetWhereRaj(string tablePrefix)
        {
            var result = String.Empty;
            if (Rajon != null)
            {
                result = Rajon.Aggregate(result, (current, nzpRaj) => current + (nzpRaj + ","));
            }
            result = result.TrimEnd(',');

            if (!String.IsNullOrEmpty(result))
            {
                RajonHeader = String.Empty;
                var sql =
                    " select trim(t.town) || '/' || trim(u.rajon) as rajon, u.nzp_raj " +
                    " from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon u, " +
                    ReportParams.Pref + DBManager.sDataAliasRest + "s_town t " +
                    " where t.nzp_town = u.nzp_town   " +
                    " AND u.nzp_raj in (" + result + ")" +
                    " group by 1,2 " +
                    " order by 1,2";
                DataTable raj = ExecSQLToTable(sql);
                foreach (DataRow dr in raj.Rows)
                {
                    RajonHeader += dr["rajon"].ToString().Trim() + ",";
                }
                RajonHeader = RajonHeader.TrimEnd(',');
                result = " AND " + tablePrefix + "nzp_raj in (" + result + ")";
            }
            return result;
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_lst_house_sel( " +
                         " nzp_kvar INTEGER, " +
                         " fio NCHAR(40), " +
                         " nkvar NCHAR(10), " +
                         " nkvar_n NCHAR(3), " +
                         " num_ls INTEGER, " +
                         " ikvar INTEGER, " +
                         " nzp_ul INTEGER, " +
                         " nzp_dom INTEGER, " +
                         " ndom NCHAR(10), " +
                         " idom INTEGER, " +
                         " nkor NCHAR(3), " +
                         " ulica CHAR(40)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_lst_house_kv( " +
                  " nzp_kvar INTEGER, " +
                  " pl_kvar CHAR(100), " +
                  " kolgil INTEGER) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_lst_house_pv( " +
                  " nzp_kvar INTEGER, " +
                  " pol_kvar CHAR(100)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_lst_house_priv( " +
                  " nzp_kvar INTEGER, " +
                  " priv INTEGER) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_lst_house_p( " +
                  " nzp_kvar INTEGER, " +
                  " vp INTEGER, " +
                  " day_vp INTEGER) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_lst_house_v( " +
                  " nzp_kvar INTEGER, " +
                  " vv INTEGER, " +
                  " day_vv INTEGER) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL("DROP TABLE t_lst_house_sel");
            ExecSQL("DROP TABLE t_lst_house_kv");
            ExecSQL("DROP TABLE t_lst_house_pv");
            ExecSQL("DROP TABLE t_lst_house_priv");
            ExecSQL("DROP TABLE t_lst_house_p");
            ExecSQL("DROP TABLE t_lst_house_v");
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("dats", DateS.ToShortDateString());
            report.SetParameterValue("datpo", DatePo.ToShortDateString());
            report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
            report.SetParameterValue("TIME", DateTime.Now.ToLongTimeString());
            report.SetParameterValue("area", AreaHeader);
            report.SetParameterValue("rajon",RajonHeader);
        }

        protected override void PrepareParams()
        {
            DateTime begin;
            DateTime end;
            string period = UserParamValues["Period"].GetValue<string>();
            PeriodParameter.GetValues(period, out begin, out end);
            DateS = begin;
            DatePo = end;
            Areas = UserParamValues["Areas"].Value.To<List<int>>();
            Rajon = UserParamValues["Raions"].Value.To<List<int>>();
        }

    }
}
