using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Main.Properties;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.Report.Main.Reports
{
    public class Report6531 : BaseSqlReport
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

        public override ReportKind ReportKind
        {
            get { return ReportKind.Base; }
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
            int year = DateTime.Today.Year,
                month = DateTime.Today.Month;
            return new List<UserParam>
            {
                new PeriodParameter(DateTime.Parse("1."+month+"."+year), 
                    DateTime.Parse(DateTime.DaysInMonth(year,month)+"."+month+"."+year)),
                new RaionsParameter(),
                new AreaParameter()
            };
        }

        public override DataSet GetData()
        {
            IDataReader reader;
            var sql = new StringBuilder();

            string whereArea = "";
            string whereRajon = "";

            //ограничения

            #region Управляющие компании
            if (Areas != null)
            {
                whereArea = Areas.Aggregate(whereArea, (current, area) => string.Format(current + "{0}", (area.ToString(CultureInfo.InvariantCulture) + ",")));
            }

            whereArea = whereArea.TrimEnd(',');
            if (!String.IsNullOrEmpty(whereArea))
            {

                //УК
                sql.Remove(0, sql.Length);
                sql.Append(" SELECT area from ");
                sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "s_area ");
                sql.Append(" WHERE nzp_area > 0 and nzp_area in(" + whereArea + ") ");
                DataTable area = ExecSQLToTable(sql.ToString());
                foreach (DataRow dr in area.Rows)
                {
                    AreaHeader += dr["area"].ToString().Trim() + ",";
                }
                AreaHeader = AreaHeader.TrimEnd(',');
                whereArea = " AND  kv.nzp_area IN (" + whereArea + ") ";
            }
            #endregion

            #region Район
            if (Rajon != null)
            {
                whereRajon = Rajon.Aggregate(whereRajon, (current, nzpRajon) => current + (nzpRajon.ToString(CultureInfo.InvariantCulture) + ","));
            }

            whereRajon = whereRajon.TrimEnd(',');
            if (!String.IsNullOrEmpty(whereRajon))
            {

                //УК
                sql.Remove(0, sql.Length);
                sql.Append(" SELECT rajon from ");
                sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon ");
                sql.Append(" WHERE nzp_raj > 0 and nzp_raj in(" + whereRajon + ") ");
                DataTable area = ExecSQLToTable(sql.ToString());
                foreach (DataRow dr in area.Rows)
                {
                    RajonHeader += dr["rajon"].ToString().Trim() + ",";
                }
                RajonHeader = RajonHeader.TrimEnd(',');
                whereRajon = " AND  sr.nzp_raj IN (" + whereRajon + ") ";
            }
            #endregion


            #region выборка в temp таблицу
            sql.Remove(0, sql.Length);
            sql.Append(" SELECT bd_kernel as pref ");
            sql.AppendFormat(" FROM {0}{1}s_point ", DBManager.GetFullBaseName(Connection), DBManager.tableDelimiter);
            sql.Append(" WHERE nzp_wp>1 ");

            ExecRead(out reader, sql.ToString());

            while (reader.Read())
            {
                if (reader["pref"] != null)
                {
                    string pref = reader["pref"].ToStr().Trim();
                    sql.Remove(0, sql.Length);
                    sql.Append(" INSERT INTO t_lst_house_sel(nzp_kvar, fio, nkvar, nkvar_n, num_ls, ikvar, nzp_ul, nzp_dom, ndom, idom, nkor, ulica) ");
                    sql.Append(" SELECT * FROM ( ");
                        sql.Append(" SELECT  kv.nzp_kvar, kv.fio, kv.nkvar, kv.nkvar_n, kv.num_ls, kv.ikvar, su.nzp_ul, d.nzp_dom, ndom, idom, nkor, ulica ");
                        sql.Append(" FROM " + pref + DBManager.sDataAliasRest + "kvar kv, " + pref + DBManager.sDataAliasRest + "dom d, " + pref + DBManager.sDataAliasRest + "s_ulica su ");
                        sql.Append(" WHERE  kv.nzp_dom=d.nzp_dom ");
                          sql.Append(whereArea);
                          sql.Append(" AND d.nzp_ul=su.nzp_ul ) a");
                    sql.Append(" WHERE nzp_kvar IN ( ");
                        sql.Append(" SELECT nzp ");
                        sql.Append(" FROM " + pref + DBManager.sDataAliasRest + "prm_3 b ");
                        sql.Append(" WHERE a.nzp_kvar=b.nzp ");
                          sql.Append(" AND nzp_prm=51 ");
                          sql.Append(" AND is_actual<>100 ");
                          sql.Append(" AND val_prm='1' ");
                          sql.Append(" AND dat_s<='" + DateS.ToShortDateString() + "' ");
                          sql.Append(" AND dat_po>='" + DatePo.ToShortDateString() + "') ");
                    ExecSQL(sql.ToString());

                    sql.Remove(0, sql.Length);
                    sql.Append(" INSERT INTO t_lst_house_kv(nzp_kvar, pl_kvar, kolgil) ");
                    sql.Append(" SELECT nzp_kvar, SUM(REPLACE(val_prm,',','.')+0) AS pl_kvar, ");
                        sql.Append(" (SELECT SUM(gil) ");
                        sql.Append(" FROM " + pref + "_charge_" + (DateS.Year - 2000).ToString("00") + DBManager.tableDelimiter + "calc_gku_" + DateS.Month.ToString("00") + " cal, ");
                            sql.Append(pref + DBManager.sDataAliasRest + "kvar kv  ");
                        sql.Append(" WHERE kv.nzp_kvar=cal.nzp_kvar ");
                          sql.Append(" AND kv.nzp_kvar=a.nzp_kvar ");
                          sql.Append(whereArea+" ) AS kolgil ");
                        sql.Append(" FROM t_lst_house_sel a, OUTER " + pref + DBManager.sDataAliasRest + "prm_1 b ");
                    sql.Append(" WHERE a.nzp_kvar=b.nzp ");
                    sql.Append(" AND nzp_prm=4 ");
                    sql.Append(" AND is_actual<>100 ");
                    sql.Append(" AND dat_s<='" + DatePo.ToShortDateString() + "' ");
                    sql.Append(" AND dat_po>='" + DateS.ToShortDateString() + "' ");
                    sql.Append(" GROUP BY 1,3 ");
                    ExecSQL(sql.ToString());

                    sql.Remove(0, sql.Length);
                    sql.Append(" INSERT INTO t_lst_house_pv(nzp_kvar, pol_kvar) ");
                    sql.Append(" SELECT nzp_kvar,SUM(REPLACE(val_prm,',','.')+0) AS pol_kvar ");
                    sql.Append(" FROM t_lst_house_sel a, OUTER " + pref + DBManager.sDataAliasRest + "prm_1 b ");
                    sql.Append(" WHERE a.nzp_kvar=b.nzp ");
                      sql.Append(" AND nzp_prm=6 ");
                      sql.Append(" AND is_actual<>100 ");
                      sql.Append(" AND dat_s<='" + DatePo.ToShortDateString() + "' ");
                      sql.Append(" AND dat_po>='" + DateS.ToShortDateString() + "' ");
                    sql.Append(" GROUP BY 1 ");
                    ExecSQL(sql.ToString());

                    sql.Remove(0, sql.Length);
                    sql.Append(" INSERT INTO t_lst_house_priv(nzp_kvar, priv) ");
                    sql.Append(" SELECT nzp_kvar,val_prm AS priv ");
                    sql.Append(" FROM t_lst_house_sel a, " + pref + DBManager.sDataAliasRest + "prm_1 b ");
                    sql.Append(" WHERE a.nzp_kvar=b.nzp ");
                      sql.Append(" AND nzp_prm=8 ");
                      sql.Append(" AND is_actual<>100 ");
                      sql.Append(" AND dat_s<='" + DatePo.ToShortDateString() + "' ");
                      sql.Append(" AND dat_po>='" + DateS.ToShortDateString() + "' ");
                    sql.Append(" GROUP BY 1,2 ");
                    ExecSQL(sql.ToString());

                    sql.Remove(0, sql.Length);
                    sql.Append(" INSERT INTO t_lst_house_p(nzp_kvar, vp, day_vp) ");
                    sql.Append(" SELECT a.nzp_kvar, ");
                        sql.Append(" MAX(p.val_prm+0) AS vp, ");
                        sql.Append(" SUM((CASE WHEN dat_po>DATE('" + DatePo.ToShortDateString() + "') THEN DATE('" + DatePo.ToShortDateString() + "') ELSE dat_po END) - ");
                        sql.Append(" (CASE WHEN dat_s<DATE('" + DateS.ToShortDateString() + "') THEN DATE('" + DateS.ToShortDateString() + "') ELSE dat_s END)+1) AS day_vp  ");
                    sql.Append(" FROM t_lst_house_sel a, " + pref + DBManager.sDataAliasRest + "prm_1 p ");
                    sql.Append(" WHERE a.nzp_kvar=p.nzp  ");
                      sql.Append(" AND p.nzp_prm=131 ");
                      sql.Append(" AND p.is_actual<>100  ");
                      sql.Append(" AND p.dat_s<='" + DatePo.ToShortDateString() + "' ");
                      sql.Append(" AND p.dat_po>='" + DateS.ToShortDateString() + "' ");
                    sql.Append(" GROUP BY 1 ");
                    ExecSQL(sql.ToString());

                    sql.Remove(0, sql.Length);
                    sql.Append(" INSERT INTO t_lst_house_v(nzp_kvar, vv, day_vv) ");
                    sql.Append(" SELECT a.nzp_kvar, ");
                        sql.Append(" COUNT(unique nzp_gilec) AS vv, ");
                        sql.Append(" SUM((CASE WHEN dat_po>date('" + DatePo.ToShortDateString() + "') THEN DATE('" + DatePo.ToShortDateString() + "') ELSE dat_po END) - ");
                        sql.Append(" (CASE WHEN dat_s<date('" + DateS.ToShortDateString() + "') THEN DATE('" + DateS.ToShortDateString() + "') ELSE dat_s END)+1) AS day_vv ");
                    sql.Append(" FROM t_lst_house_sel a," + pref + DBManager.sDataAliasRest + "gil_periods g ");
                    sql.Append(" WHERE a.nzp_kvar=g.nzp_kvar ");
                      sql.Append(" AND g.is_actual<>100 ");
                      sql.Append(" AND g.dat_s<='" + DatePo.ToShortDateString() + "' ");
                      sql.Append(" AND g.dat_po>='" + DateS.ToShortDateString() + "' ");
                      sql.Append(" AND g.dat_po>g.dat_s+4 UNITS DAY ");
                      sql.Append(" AND g.dat_po>date('01.06.2006') + 4 UNITS DAY ");
                    sql.Append(" GROUP BY 1 ");
                    ExecSQL(sql.ToString());
  
                }
            }
            reader.Close();
            #endregion
            sql.Remove(0, sql.Length);
            sql.Append(" SELECT distinct b.nzp_kvar,ROUND(b.pl_kvar,2) AS pl_kvar,b.kolgil, ROUND(pol_kvar,2) AS pol_kvar, town, rajon, a.ulica, ndom, idom, nkor, nkvar, ikvar, nkvar_n, ");
                sql.Append(" num_ls, fio,  priv, vv, vp, day_vv, day_vp ");                       
            sql.Append(" FROM t_lst_house_sel a, t_lst_house_kv b, t_lst_house_pv c, OUTER t_lst_house_priv t, OUTER t_lst_house_p, OUTER t_lst_house_v, ");
                sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "s_town st, ");
                sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica su, " + ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon sr ");
            sql.Append(" WHERE a.nzp_kvar=b.nzp_kvar ");
              sql.Append(" AND a.nzp_kvar=c.nzp_kvar ");
              sql.Append(" AND su.nzp_ul=a.nzp_ul ");
              sql.Append(" AND su.nzp_raj=sr.nzp_raj ");
              sql.Append(whereRajon);
              sql.Append(" AND t.nzp_kvar=a.nzp_kvar ");
              sql.Append(" AND a.nzp_kvar=t_lst_house_v.nzp_kvar ");
              sql.Append(" AND a.nzp_kvar=t_lst_house_p.nzp_kvar ");
              sql.Append(" AND sr.nzp_town = st.nzp_town ");
              sql.Append(" AND b.pl_kvar IS NOT NULL ");
              sql.Append(" AND pol_kvar IS NOT NULL ");
              //sql.Append(" AND  b.nzp_kvar=24544 ");//*
              sql.Append(" ORDER BY town, rajon, ulica, idom, ikvar, nkor, nkvar_n");

            DataTable dt = ExecSQLToTable(sql.ToString());
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;

        }

        protected override void CreateTempTable()
        {
            var sql = new StringBuilder();
            sql.Append(" CREATE TEMP TABLE t_lst_house_sel( ");
            sql.Append(" nzp_kvar INTEGER, ");
            sql.Append(" fio NCHAR(40), ");
            sql.Append(" nkvar NCHAR(10), ");
            sql.Append(" nkvar_n NCHAR(3), ");
            sql.Append(" num_ls INTEGER, ");
            sql.Append(" ikvar INTEGER, ");
            sql.Append(" nzp_ul INTEGER, ");
            sql.Append(" nzp_dom INTEGER, ");
            sql.Append(" ndom NCHAR(10), ");
            sql.Append(" idom INTEGER, ");
            sql.Append(" nkor NCHAR(3), ");
            sql.Append(" ulica CHAR(40)) " + DBManager.sUnlogTempTable);
            ExecSQL(sql.ToString());

            sql.Remove(0, sql.Length);
            sql.Append(" CREATE TEMP TABLE t_lst_house_kv( ");
            sql.Append(" nzp_kvar INTEGER, ");
            sql.Append(" pl_kvar CHAR(100), ");
            sql.Append(" kolgil INTEGER) " + DBManager.sUnlogTempTable);
            ExecSQL(sql.ToString());

            sql.Remove(0, sql.Length);
            sql.Append(" CREATE TEMP TABLE t_lst_house_pv( ");
            sql.Append(" nzp_kvar INTEGER, ");
            sql.Append(" pol_kvar CHAR(100)) " + DBManager.sUnlogTempTable);
            ExecSQL(sql.ToString());

            sql.Remove(0, sql.Length);
            sql.Append(" CREATE TEMP TABLE t_lst_house_priv( ");
            sql.Append(" nzp_kvar INTEGER, ");
            sql.Append(" priv INTEGER) " + DBManager.sUnlogTempTable);
            ExecSQL(sql.ToString());

            sql.Remove(0, sql.Length);
            sql.Append(" CREATE TEMP TABLE t_lst_house_p( ");
            sql.Append(" nzp_kvar INTEGER, ");
            sql.Append(" vp INTEGER, ");
            sql.Append(" day_vp INTEGER) " + DBManager.sUnlogTempTable);
            ExecSQL(sql.ToString());

            sql.Remove(0, sql.Length);
            sql.Append(" CREATE TEMP TABLE t_lst_house_v( ");
            sql.Append(" nzp_kvar INTEGER, ");
            sql.Append(" vv INTEGER, ");
            sql.Append(" day_vv INTEGER) " + DBManager.sUnlogTempTable);
            ExecSQL(sql.ToString());
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
