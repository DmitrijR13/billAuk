namespace Bars.KP50.Report.Main
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Data;
    using System.Text;
    using Bars.KP50.Report;
    using Bars.KP50.Report.Base;
    using Bars.KP50.Report.Main.Properties;
    using Bars.KP50.Utils;

    using FastReport;

    using STCLINE.KP50.DataBase;
    using STCLINE.KP50.Global;
    class Analiz_potreb : BaseSqlReport
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
                List<ReportGroup> result = new List<ReportGroup>();
                result.Add(ReportGroup.Reports);
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

        public override ReportKind ReportKind
        {
            get { return ReportKind.Base; }
        }

        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }


        /// <summary>Услуги</summary>
        protected int Services { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Список услуг в заголовке</summary>
        protected string ServicesHeader { get; set; }

        /// <summary>Единицы измерения в заголовке</summary>
        protected string ed_izmer { get; set; }

        /// <summary>Список Поставщиков в заголовке</summary>
        protected string SuppliersHeader { get; set; }

        /// <summary>Список балансодержателей (Управляющих компаний)</summary>
        protected string AreasHeader { get; set; }

        /// <summary>Дата печати</summary>
        protected DateTime dateprint { get; set; }
        /// <summary>Выбрать откр или  зак или все ЛС </summary>
        protected int TypeLs { get; set; }
        protected string NameTypeLs { get; set; }

        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new MonthParameter{ Value = DateTime.Now.Month },
                new YearParameter{ Value = DateTime.Now.Year },
           //     new SupplierParameter(),
                new AreaParameter(),
                new ServiceParameter(false),       
             ///   new PeriodParameter (DateTime.Now, DateTime.Now)             
            
            };
        }

        public override DataSet GetData()
        {
            MyDataReader reader;
            StringBuilder sql = new StringBuilder();
            StringBuilder sql1 = new StringBuilder();
            string where_supp = "";
            string where_area = "";
            string where_serv = "";
            string where_ls = "";    

            #region Выборка по локальным банкам
      
            where_serv += Services.ToString() + ",";
          
            where_serv = where_serv.TrimEnd(',');

            if (!String.IsNullOrEmpty(where_serv))
            {
                where_serv = " and co.nzp_serv in(" + where_serv + ")";
                //Поставщики
                sql.Remove(0, sql.Length);
                sql.Append(" SELECT service from ");
                sql.Append(ReportParams.Pref + DBManager.sKernelAliasRest + "services co ");
                sql.Append(" WHERE co.nzp_serv > 0 " + where_serv);
            
            }

            if (Suppliers != null)
            {
                foreach (int nzp_supp in Suppliers)
                    where_supp += nzp_supp.ToString() + ",";
                where_supp = where_supp.TrimEnd(',');
            }

            if (!String.IsNullOrEmpty(where_supp))
            {
                where_supp = " and nzp_supp in(" + where_supp + ")";
                //Поставщики
                sql.Remove(0, sql.Length);
                sql.Append(" SELECT name_supp from ");
                sql.Append(ReportParams.Pref + DBManager.sKernelAliasRest + "supplier ");
                sql.Append(" WHERE nzp_supp > 0 " + where_supp);
                DataTable supp = ExecSQLToTable(sql.ToString());
                foreach (DataRow dr in supp.Rows)
                {
                    SuppliersHeader += dr["name_supp"].ToString().Trim() + ",";
                }
                SuppliersHeader = SuppliersHeader.TrimEnd(',');
            }

            if (Areas != null)
            {
                foreach (int nzp_area in Areas)
                    where_area += nzp_area.ToString() + ",";
                where_area = where_area.TrimEnd(',');
            }

            if (!String.IsNullOrEmpty(where_area))
            {   
                sql.Remove(0, sql.Length);
                sql.Append(" SELECT area from ");
                sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "s_area ");
                sql.Append(" WHERE nzp_area > 0 and nzp_area in(" + where_area + ") ");
                DataTable area = ExecSQLToTable(sql.ToString());
                foreach (DataRow dr in area.Rows)
                {
                    AreasHeader += dr["area"].ToString().Trim() + ",";
                }
                AreasHeader = AreasHeader.TrimEnd(',');
                where_area = "and kv.nzp_area in(" + where_area + ") and d.nzp_area in (" + where_area + ") ";
            }
            sql.Remove(0, sql.Length);
            sql.Append(" SELECT * ");
            sql.Append(" FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point ");
            sql.Append(" WHERE nzp_wp>1 ");
            ExecRead(out reader, sql.ToString());
            while (reader.Read())
            {
                string pref = reader["bd_kernel"].ToString().ToLower().Trim();

                //с приборами учета
                sql.Remove(0, sql.Length);
                sql.Append(" insert into t1( nzp_type, nzp_dom, num_ls, ikvar,	ulica,	ndom, nkor,	 count_gil,	dat_s,	val_s, ");
                sql.Append(" dat_po, val_po, rashod, rashod_mes, rashod_norm, rashod_odn, rashod_pu, kf307, val3, nzp_kvar ) ");

                sql.Append(" SELECT  co.nzp_type, co.nzp_dom,  ");
                sql.Append(" num_ls, ikvar, ");
                sql.Append(" TRIM (ulica) as ulica, TRIM (ndom) as ndom, ");
                sql.Append("  TRIM (CASE WHEN nkor = '-' OR TRIM (nkor) = '' THEN ''  ELSE  nkor END   ) AS nkor, ");
                sql.Append("  gil AS count_gil, ");
                sql.Append("  co.dat_s, co.val_s, co.dat_po, ");
                sql.Append("  co.val_po,  (co.val_po - co.val_s) as  rashod,");
                sql.Append("   (((co.val_po - co.val_s) / (co.dat_po - co.dat_s))*(co.dat_po - co.dat_s)) AS rashod_mes, ");
                sql.Append("   (CASE WHEN is_device = 0 THEN rashod_norm ELSE   0 END   	) AS rashod_norm, ");
                sql.Append("   ca.rashod AS rashod_odn, ");
                sql.Append("   (((co.val_po - co.val_s) / (co.dat_po - co.dat_s))*(co.dat_po - co.dat_s)) AS rashod_pu, kf307, val3, co.nzp_kvar ");
             
               
                sql.Append(" From " + pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "counters_" + Month.ToString("00") + " co  left outer join  ");
                sql.Append(pref + DBManager.sDataAliasRest + "kvar k on K .nzp_kvar = co.nzp_kvar LEFT outer  JOIN  ");
                sql.Append(pref + DBManager.sDataAliasRest + "dom d  on  K .nzp_dom = d.nzp_dom  LEFT outer  JOIN  ");
                sql.Append(pref + DBManager.sDataAliasRest + "s_ulica su on d.nzp_ul = su.nzp_ul left outer join ");                
                sql.Append(pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "calc_gku_" + Month.ToString("00") + " ca ");
                sql.Append(" on  K .nzp_kvar = ca.nzp_kvar   and  co.nzp_kvar = ca.nzp_kvar ");
                sql.Append(" WHERE	co.stek = 1 ");
             
                sql.Append(where_serv + " " + where_area);
                sql.Append("  group by  2,1,4,3,5,6,7,20,9,10,11,12,15,16,17,18,19, 8");
           
   
                ExecSQL(sql.ToString());

                //без приборов учета
                sql.Remove(0, sql.Length);
                sql.Append(" insert into t1( nzp_type, nzp_dom, num_ls, ikvar,	ulica,	ndom, nkor,	 count_gil,	dat_s,	val_s, ");
                sql.Append(" dat_po, val_po, rashod, rashod_mes, rashod_norm, rashod_odn, rashod_pu, kf307, val3, nzp_kvar ) ");

                sql.Append(" SELECT  co.nzp_type, co.nzp_dom,  ");
                sql.Append(" num_ls, ikvar, ");
                sql.Append(" TRIM (ulica) as ulica, TRIM (ndom) as ndom, ");
                sql.Append("  TRIM (CASE WHEN nkor = '-' OR TRIM (nkor) = '' THEN ''  ELSE  nkor END   ) AS nkor, ");
                sql.Append("  gil AS count_gil, ");
                sql.Append("  co.dat_s, co.val_s, co.dat_po, ");
                sql.Append("  co.val_po,  (co.val_po - co.val_s) as  rashod,");
                sql.Append("   (((co.val_po - co.val_s) / (co.dat_po - co.dat_s))*(co.dat_po - co.dat_s)) AS rashod_mes, ");
                sql.Append("   (CASE WHEN is_device = 0 THEN rashod_norm ELSE   0 END   	) AS rashod_norm, ");
                sql.Append("   ca.rashod AS rashod_odn, ");
                sql.Append("   ( CASE WHEN is_device <> 0 THEN  (co.val1) ELSE 0 END) AS rashod_pu, kf307, val3, co.nzp_kvar ");


                sql.Append(" From " + pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "counters_" + Month.ToString("00") + " co  left outer join  ");
                sql.Append(pref + DBManager.sDataAliasRest + "kvar k on K .nzp_kvar = co.nzp_kvar LEFT outer  JOIN  ");
                sql.Append(pref + DBManager.sDataAliasRest + "dom d  on  K .nzp_dom = d.nzp_dom  LEFT outer  JOIN  ");
                sql.Append(pref + DBManager.sDataAliasRest + "s_ulica su on d.nzp_ul = su.nzp_ul left outer join ");
                sql.Append(pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "calc_gku_" + Month.ToString("00") + " ca ");
                sql.Append(" on  K .nzp_kvar = ca.nzp_kvar   and  co.nzp_kvar = ca.nzp_kvar ");
                sql.Append(" WHERE	co.stek = 3 and co.nzp_kvar not in ( select nzp_kvar from t1)  ");

                sql.Append(where_serv + " " + where_area);
                sql.Append("  group by  3,1,4,2,5,6,7,20,9,10,11,12,15,16,17,18,19, 8 ");
                ExecSQL(sql.ToString());

              //вторая часть отчета
                sql.Remove(0, sql.Length);
                sql.Append(" insert into t2(nzp_dom, rash_ls, dom) ");
                sql.Append("  SELECT nzp_dom, sum(rashod_odn) as rash_ls, ");
                sql.Append(" TRIM (CASE  WHEN nkor = '-'   	OR TRIM (nkor) = '' THEN ");
                sql.Append(" 'ул.'||TRIM (ulica)||' д.'||TRIM (ndom) 	ELSE ");
                sql.Append(" 'ул.'||TRIM (ulica)||' д.'||TRIM (ndom)||nkor END ) as dom ");             
                sql.Append(" FROM  t1 where nzp_type=3  group by 1,3");
                ExecSQL(sql.ToString());

                sql.Remove(0, sql.Length);
                sql.Append(" insert into t3(nzp_dom, rash_dom) ");
                sql.Append("  SELECT nzp_dom, sum(rashod_mes) as rash_dom ");
                sql.Append(" FROM  t1 where nzp_type=1  group by 1");
                ExecSQL(sql.ToString());
            }
            reader.Close();

            string sql228 = "select  nzp_type, nzp_dom, num_ls, ikvar,	ulica,	ndom, nkor,	count_gil,	dat_s,	val_s," +
                " dat_po, val_po, rashod, rashod_mes, rashod_norm, rashod_odn, rashod_pu, kf307   from t1  order by ikvar, nzp_type DESC  ";
            DataTable dt = ExecSQLToTable(sql228);
            
            sql228="";
            sql228 = " SELECT distinct dom, rash_ls, rash_dom, (rash_dom-rash_ls) as rash_ls_dom from "+
                     "  t2 left outer join t3 on t2.nzp_dom=t3.nzp_dom ";       
             DataTable dt1 = ExecSQLToTable(sql228);
            dt.TableName = "Q_master";
            dt1.TableName = "Q_master1";
           
            #endregion
            var ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables.Add(dt1);
           
            return ds;
        }

        protected override void PrepareReport(Report report)
        {
            string[] months = new string[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            try
            {
                report.SetParameterValue("su", SuppliersHeader);
                report.SetParameterValue("serv", ServicesHeader);
                
                report.SetParameterValue("date", DateTime.Now.ToLongDateString());
                report.SetParameterValue("time", DateTime.Now.ToLongTimeString());
           
                report.SetParameterValue("dateprint", dateprint);
                report.SetParameterValue("ver", "1.0");
                report.SetParameterValue("area", AreasHeader);
                report.SetParameterValue("mounth", months[Month]);
                report.SetParameterValue("year", Year.ToString());
                
            }
            catch (Exception ex)
            {
                throw new ReportException("Произошла ошибка при формировании отчета " + ex.Message, ex);
            }
        }

        protected override void PrepareParams()
        {
            try
            {
                Services = UserParamValues["Services"].GetValue<int>();
                Areas = UserParamValues["Areas"].GetValue<List<int>>();
                Month = UserParamValues["Month"].GetValue<int>();
                Year = UserParamValues["Year"].GetValue<int>();
            }
            catch (Exception ex)
            {
                throw new ReportException("Произошла ошибка при формировании отчета " + ex.Message, ex);
            }
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
