using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using Bars.KP50.Report.RT.Properties;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.RT.Reports
{
    class Report16624 : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.6.24. Анализ потребления "; }
        }

        public override string Description
        {
            get { return "6.24. Анализ потребления"; }
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
            get { return Resources._6_24_Analiz_potreb; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>Расчетный месяц</summary>
        private int Month { get; set; }

        /// <summary>Расчетный год</summary>
        private int Year { get; set; }

        /// <summary>Услуги</summary>
        private int Services { get; set; }

        /// <summary>Поставщики</summary>
        private List<long> Suppliers { get; set; }

        /// <summary>Управляющие компании</summary>
        private List<int> Areas { get; set; }

        /// <summary>Список услуг в заголовке</summary>
        private string ServiceHeader { get; set; }

        /// <summary>Единицы измерения в заголовке</summary>
        private string EdIzmer { get; set; }
        
        /// <summary>Список балансодержателей (Управляющих компаний)</summary>
        private string AreaHeader { get; set; }

        /// <summary>Выбрать откр или  зак или все ЛС </summary>
        private int TypeLs { get; set; }

        /// <summary>Выбрать откр или  зак или все ЛС </summary>
        private string NameTypeLs { get; set; }

        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
                new AreaParameter(),
                new ServiceParameter(false),    
            };
        }

        public override DataSet GetData()
        {
            MyDataReader reader;
            var sql = new StringBuilder();
            string whereArea = GetWhereAdr("k.");
            string whereServ = GetWhereServ("co.");

            sql.Remove(0, sql.Length);
            sql.Append(" SELECT * ");
            sql.Append(" FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point ");
            sql.Append(" WHERE nzp_wp>1 " + GetwhereWp());
            ExecRead(out reader, sql.ToString());
            while (reader.Read())
            {
                var pref = reader["bd_kernel"].ToString().ToLower().Trim();

                var countersTable = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter +
                                       "counters_" + Month.ToString("00");
                var calcTable = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter +
                                "calc_gku_" + Month.ToString("00");

                if (TempTableInWebCashe(countersTable) && TempTableInWebCashe(calcTable) && TempColumnInWebCashe(calcTable, "is_device"))
                {

                    //с приборами учета
                    sql.Remove(0, sql.Length);
                    sql.Append(
                        " insert into t1( nzp_type, nzp_dom, num_ls, ikvar,	ulica,	ndom, nkor,	 count_gil,	dat_s,	val_s, ");
                    sql.Append(
                        " dat_po, val_po, rashod, rashod_mes, rashod_norm, rashod_odn, rashod_pu, kf307, val3, nzp_kvar ) ");

                    sql.Append(" SELECT  co.nzp_type, co.nzp_dom,  ");
                    sql.Append(" num_ls, ikvar, ");
                    sql.Append(" TRIM (ulica) as ulica, TRIM (ndom) as ndom, ");
                    sql.Append(" TRIM (CASE WHEN nkor = '-' OR TRIM (nkor) = '' THEN ''  ELSE  nkor END   ) AS nkor, ");
                    sql.Append(" gil AS count_gil, ");
                    sql.Append(" co.dat_s, co.val_s, co.dat_po, ");
                    sql.Append(" co.val_po,  (co.val_po - co.val_s) as  rashod,");
                    sql.Append(
                        " (((co.val_po - co.val_s) / (co.dat_po - co.dat_s))*(co.dat_po - co.dat_s)) AS rashod_mes, ");
                    sql.Append(" (CASE WHEN is_device = 0 THEN rashod_norm ELSE   0 END   	) AS rashod_norm, ");
                    sql.Append(" ca.rashod AS rashod_odn, ");
                    sql.Append(
                        " (((co.val_po - co.val_s) / (co.dat_po - co.dat_s))*(co.dat_po - co.dat_s)) AS rashod_pu, kf307, val3, co.nzp_kvar ");


                    sql.Append(" From " + countersTable + " co  left outer join  ");
                    sql.Append(pref + DBManager.sDataAliasRest +
                               "kvar k on K .nzp_kvar = co.nzp_kvar LEFT outer  JOIN  ");
                    sql.Append(pref + DBManager.sDataAliasRest + "dom d  on  K .nzp_dom = d.nzp_dom  LEFT outer  JOIN  ");
                    sql.Append(pref + DBManager.sDataAliasRest + "s_ulica su on d.nzp_ul = su.nzp_ul left outer join ");
                    sql.Append(calcTable + " ca ");
                    sql.Append(" on  K .nzp_kvar = ca.nzp_kvar   and  co.nzp_kvar = ca.nzp_kvar ");
                    sql.Append(" WHERE	co.stek = 1 ");
                    sql.Append(whereServ + whereArea);
                    sql.Append(" group by 2,1,4,3,5,6,7,20,9,10,11,12,15,16,17,18,19,8");


                    ExecSQL(sql.ToString());

                    //без приборов учета
                    sql.Remove(0, sql.Length);
                    sql.Append(
                        " insert into t1( nzp_type, nzp_dom, num_ls, ikvar,	ulica,	ndom, nkor,	 count_gil,	dat_s,	val_s, ");
                    sql.Append(
                        " dat_po, val_po, rashod, rashod_mes, rashod_norm, rashod_odn, rashod_pu, kf307, val3, nzp_kvar ) ");

                    sql.Append(" SELECT  co.nzp_type, co.nzp_dom,  ");
                    sql.Append(" num_ls, ikvar, ");
                    sql.Append(" TRIM (ulica) as ulica, TRIM (ndom) as ndom, ");
                    sql.Append(" TRIM (CASE WHEN nkor = '-' OR TRIM (nkor) = '' THEN ''  ELSE  nkor END   ) AS nkor, ");
                    sql.Append(" gil AS count_gil, ");
                    sql.Append(" co.dat_s, co.val_s, co.dat_po, ");
                    sql.Append(" co.val_po,  (co.val_po - co.val_s) as  rashod,");
                    sql.Append(
                        " (((co.val_po - co.val_s) / (co.dat_po - co.dat_s))*(co.dat_po - co.dat_s)) AS rashod_mes, ");
                    sql.Append(" (CASE WHEN is_device = 0 THEN rashod_norm ELSE   0 END   	) AS rashod_norm, ");
                    sql.Append(" ca.rashod AS rashod_odn, ");
                    sql.Append(
                        " ( CASE WHEN is_device <> 0 THEN  (co.val1) ELSE 0 END) AS rashod_pu, kf307, val3, co.nzp_kvar ");


                    sql.Append(" From " + countersTable + " co  left outer join  ");
                    sql.Append(pref + DBManager.sDataAliasRest +
                               "kvar k on K .nzp_kvar = co.nzp_kvar LEFT outer  JOIN  ");
                    sql.Append(pref + DBManager.sDataAliasRest + "dom d  on  K .nzp_dom = d.nzp_dom  LEFT outer  JOIN  ");
                    sql.Append(pref + DBManager.sDataAliasRest + "s_ulica su on d.nzp_ul = su.nzp_ul left outer join ");
                    sql.Append(calcTable + " ca ");
                    sql.Append(" on  K .nzp_kvar = ca.nzp_kvar   and  co.nzp_kvar = ca.nzp_kvar ");
                    sql.Append(" WHERE	co.stek = 3 and co.nzp_kvar not in ( select nzp_kvar from t1)  ");

                    sql.Append(whereServ + whereArea);
                    sql.Append(" group by 3,1,4,2,5,6,7,20,9,10,11,12,15,16,17,18,19,8 ");
                    ExecSQL(sql.ToString());
                }

                //вторая часть отчета
                sql.Remove(0, sql.Length);
                sql.Append(" insert into t2(nzp_dom, rash_ls, dom) ");
                sql.Append(" SELECT nzp_dom, sum(rashod_odn) as rash_ls, ");
                sql.Append(" TRIM (CASE  WHEN nkor = '-'   	OR TRIM (nkor) = '' THEN ");
                sql.Append(" 'ул.'||TRIM (ulica)||' д.'||TRIM (ndom) 	ELSE ");
                sql.Append(" 'ул.'||TRIM (ulica)||' д.'||TRIM (ndom)||nkor END ) as dom ");             
                sql.Append(" FROM  t1 where nzp_type=3  group by 1,3");
                ExecSQL(sql.ToString());

                sql.Remove(0, sql.Length);
                sql.Append(" insert into t3(nzp_dom, rash_dom) ");
                sql.Append(" SELECT nzp_dom, sum(rashod_mes) as rash_dom ");
                sql.Append(" FROM  t1 where nzp_type=1  group by 1");
                ExecSQL(sql.ToString());
            }
            reader.Close();

            sql.Remove(0, sql.Length);
            sql.Append("select  nzp_type, nzp_dom, num_ls, ikvar,	ulica,	ndom, nkor,	count_gil,	dat_s,	val_s," +
                " dat_po, val_po, rashod, rashod_mes, rashod_norm, rashod_odn, rashod_pu, kf307   from t1  order by ikvar, nzp_type DESC  ");
            DataTable dt = ExecSQLToTable(sql.ToString());

            sql.Remove(0, sql.Length);
            sql.Append(" SELECT distinct dom, rash_ls, rash_dom, (rash_dom-rash_ls) as rash_ls_dom from "+
                     "  t2 left outer join t3 on t2.nzp_dom=t3.nzp_dom ");       
            DataTable dt1 = ExecSQLToTable(sql.ToString());
            dt.TableName = "Q_master";
            dt1.TableName = "Q_master1";
           
            var ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables.Add(dt1);
           
            return ds;
        }

        private string GetWhereServ(string tablePrefix)
        {
            var result = "";
            if (Services != 0)
            {
                result = " and " + tablePrefix + "nzp_serv in (" + Services + ") ";
                ServiceHeader = String.Empty;
                var sql = " SELECT service from " +
                            ReportParams.Pref + DBManager.sKernelAliasRest + "services " + tablePrefix.TrimEnd('.') +
                          " WHERE " + tablePrefix + "nzp_serv > 0 " + result;
                ServiceHeader = ExecSQLToTable(sql).CastAs<DataTable>().Rows[0][0].ToString().Trim();
            }
            return result;
        }

        private string GetWhereAdr(string tablePrefix)
        {
            var result = String.Empty;
            if (Areas != null)
            {
                result = Areas.Aggregate(result, (current, nzpArea) => current + (nzpArea + ","));
            }
            else
            {
                result = ReportParams.GetRolesCondition(Constants.role_sql_area);
            }

            result = result.TrimEnd(',');
            if (!String.IsNullOrEmpty(result))
            {
                result = " AND " + tablePrefix + "nzp_area in (" + result + ")";

                AreaHeader = String.Empty;
                var sql = " SELECT area from " +
                      ReportParams.Pref + DBManager.sDataAliasRest + "s_area " + tablePrefix.TrimEnd('.') +
                      " WHERE " + tablePrefix + "nzp_area > 0 " + result;
                var area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    AreaHeader += dr["area"].ToString().Trim() + ",";
                }
                AreaHeader = AreaHeader.TrimEnd(',');
            }
            return result;
        }


        private string GetwhereWp()
        {
            string whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            return whereWp;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};

            report.SetParameterValue("serv", ServiceHeader);
                
            report.SetParameterValue("date", DateTime.Now.ToLongDateString());
            report.SetParameterValue("time", DateTime.Now.ToLongTimeString());
           
            report.SetParameterValue("dateprint", DateTime.Now.ToShortDateString());
            report.SetParameterValue("ver", "1.0");
            report.SetParameterValue("area", AreaHeader);
            report.SetParameterValue("mounth", months[Month]);
            report.SetParameterValue("year", Year);

        }

        protected override void PrepareParams()
        {
            Services = UserParamValues["Services"].GetValue<int>();
            Areas = UserParamValues["Areas"].GetValue<List<int>>();
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
        }

        protected override void CreateTempTable()
        {
            string sql = "create temp table t1 ( " +
                 "  nzp_kvar integer, " +  
                 " kf307 integer," +
          "  nzp_type integer, " +  
          "  nzp_dom integer, " +  
          "  num_ls integer, " +             
          //"  ikvar CHAR(10), " +
            "  ikvar integer, " +
          "  ulica CHAR(40), " +
          "  ndom CHAR(10), " +
          "  nkor CHAR(3), " +
          "  val3 " + DBManager.sDecimalType + "(14,2),"+
          "  count_gil integer,"+
          "  dat_s char(40),"+
          "  val_s " + DBManager.sDecimalType + "(14,2)," +
          "  dat_po char(40),"+
          "  val_po " + DBManager.sDecimalType + "(14,2)," +
          "  rashod_mes " + DBManager.sDecimalType + "(14,2)," +
          "  rashod_norm " + DBManager.sDecimalType + "(14,2)," +
          "  rashod_odn " + DBManager.sDecimalType + "(14,2)," +
          "  rashod_pu " + DBManager.sDecimalType + "(14,2)," +
          "  rashod " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql="";
            sql = "create temp table t2 ( " +
                 " nzp_dom integer, " + 
                 " rash_ls " + DBManager.sDecimalType + "(14,2)," +
                 " dom char(40) " +                
                 " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = "";
            sql = "create temp table t3 ( " +
                 " nzp_dom integer, " + 
                 " rash_dom " + DBManager.sDecimalType + "(14,2)" +
                 " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t1 ", true);
            ExecSQL(" drop table t2 ", true);
            ExecSQL(" drop table t3 ", true);
        }

    }
}
