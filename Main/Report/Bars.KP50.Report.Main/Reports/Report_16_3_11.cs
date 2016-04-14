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
    class SostNachislIOplatNaselUslugi : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.3.11 Состояние начисления и оплаты населения услуги "; }
        }

        public override string Description
        {
            get { return "3.11. Состояние начисления и оплаты населения услуги "; }
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
            get { return Resources.SostNachislIOplatNaselUslugi; }
        }

        public override ReportKind ReportKind
        {
            get { return ReportKind.Base; }
        }




        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        /// <summary> с расчетного дня </summary>
        protected string dat_s { get; set; }

        /// <summary> по расчетный год </summary>
        protected string dat_po { get; set; }


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
             //   new MonthParameter{ Value = DateTime.Now.Month },
              //  new YearParameter{ Value = DateTime.Now.Year },
                new SupplierParameter(),
                new AreaParameter(),
                new ServiceParameter(false),       
                new PeriodParameter (DateTime.Now, DateTime.Now),
                   new ComboBoxParameter
                {
                    Code = "TypeLs",
                    Name = "Лицевой счет",
                    Value = "1",
                    MultiSelect=false,
                    StoreData = new List<object>
                    {
                      
                        new { Id = "1", Name = "Открыт" },
                        new { Id = "2", Name = "Закрыт" },
                        new { Id = "3", Name = "Все" }
                    }
                },
            
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
            int m1 = dat_s.ToDateTime().Month;
            int m2 = dat_po.ToDateTime().Month;
            int y1 = dat_s.ToDateTime().Year;
            int y2 = dat_po.ToDateTime().Year;

            #region Выборка по локальным банкам
            bool ifAllLs = false;
            if (TypeLs == 1) {
                where_ls = " and p.val_prm='1' and is_actual=1 AND kv.nzp_kvar = p.nzp  and ('" + dat_s + "' BETWEEN  p.dat_s and p.dat_po or '" + dat_po + "' BETWEEN  p.dat_s and p.dat_po) ";
                NameTypeLs = "Открытые ЛС";

            }
            if (TypeLs == 2)
            {
                where_ls = " and p.val_prm='2' and is_actual=1 AND kv.nzp_kvar = p.nzp  and  ('" + dat_s + "' BETWEEN  p.dat_s and p.dat_po or '" + dat_po + "' BETWEEN  p.dat_s and p.dat_po) ";
                  NameTypeLs="Закрытые  ЛС";
            }
            if (TypeLs == 3)
            {
                ifAllLs = true;
                where_ls = "";
                NameTypeLs = "Все ЛС";

            }





            //if (Services != null)
            //{
                where_serv += Services.ToString() + ",";
            //}
            where_serv = where_serv.TrimEnd(',');

            if (!String.IsNullOrEmpty(where_serv))
            {
                where_serv = " and nzp_serv in(" + where_serv + ")";
                //Поставщики
                sql.Remove(0, sql.Length);
                sql.Append(" SELECT service, ed_izmer from ");
                sql.Append(ReportParams.Pref  + DBManager.sKernelAliasRest + "services ");
                sql.Append(" WHERE nzp_serv > 0 " + where_serv);
                DataTable serv = ExecSQLToTable(sql.ToString());
                foreach (DataRow dr in serv.Rows)
                {
                    ServicesHeader += dr["service"].ToString().Trim();
                    ed_izmer += dr["ed_izmer"].ToString().Trim();
                }
              //  ServicesHeader = ServicesHeader.TrimEnd(',');
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
                sql.Append(ReportParams.Pref  + DBManager.sKernelAliasRest + "supplier ");
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
            {                //УК
                sql.Remove(0, sql.Length);
                sql.Append(" SELECT area from ");
                sql.Append(ReportParams.Pref  + DBManager.sDataAliasRest + "s_area ");
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
            sql.Append(" FROM  " + ReportParams.Pref  + DBManager.sKernelAliasRest + "s_point ");
            sql.Append(" WHERE nzp_wp>1 ");
            ExecRead(out reader, sql.ToString());
            while (reader.Read())
            {
                string pref = reader["bd_kernel"].ToString().ToLower().Trim();
                for (int i = dat_s.ToDateTime().Year * 12 + dat_s.ToDateTime().Month; i < dat_po.ToDateTime().Year * 12 + dat_po.ToDateTime().Month + 1; i++)
                {
                    int year_ = i / 12;
                    int month_ = i % 12;
                    if (month_ == 0)
                    {
                        year_--;
                        month_ = 12;
                    }

                    sql.Remove(0, sql.Length);
                    sql.Append(" insert into t_svod( ");
                    sql.Append(" nzp_serv, nzp_supp,	nzp_area,	num_ls,	nkvar_n,	nkvar, ");//
                    sql.Append(" ndom,	nkor,	ulica,	fio,	tarif, money_to, ");///
                    sql.Append(" reval_tarif,	reval_lgota,	reval,	money_del, sum_money,");////
                    sql.Append(" money_from, sum_outsaldo, sum_insaldo, sum_real,	sum_tarif, sum_lgota, ");/////
                    sql.Append(" sum_subsidy,	sum_smo, real_charge,	sum_charge) ");//////

                    sql.Append(" SELECT ");
                    sql.Append("C .nzp_serv, C .nzp_supp, kv.nzp_area, C .num_ls, trim(case when  nkvar_n='-' then '' else  'ком.'||nkvar_n  end ) as	nkvar_n, ");
                    sql.Append(" trim(case when  nkvar='0' then '' else  'кв.'||nkvar  end ) as  nkvar, ");//
                    sql.Append(" trim(ndom),	 trim(case when  nkor='-' or trim(nkor)=''  then '' else  nkor  end ) as nkor,	trim(ulica), ");
                    sql.Append(" trim(fio),	tarif, money_to, ");///
                
                    sql.Append(" 	reval_tarif,	reval_lgota,	reval,	money_del,	sum_money, ");
                    sql.Append(" 	money_from, 	sum_outsaldo,	sum_insaldo,	sum_real,	C .sum_tarif,	C .sum_lgota,");
                    sql.Append("  sum_subsidy,	sum_smo,	real_charge,	sum_charge");
                    sql.Append(" From " + pref  + DBManager.sDataAliasRest + "kvar kv, ");
                    sql.Append(pref  + DBManager.sDataAliasRest + "s_ulica su, ");
                    sql.Append(pref  + DBManager.sDataAliasRest + "dom d, ");
                    if (ifAllLs)
                    {                       
                    }
                    else {
                        sql.Append(pref + "_data" + DBManager.tableDelimiter + "prm_3 p, ");
                    }
                    sql.Append(pref + "_charge_" + year_.ToString().Substring(2, 2) + DBManager.tableDelimiter + "charge_" + month_.ToString("00") + " c  ");
                    
                    sql.Append(" WHERE   kv.nzp_dom = d.nzp_dom AND d.nzp_ul = su.nzp_ul "+
                        "  AND	C .nzp_serv > 1 AND C .nzp_kvar = kv.nzp_kvar  AND c.dat_charge IS NULL " + where_area + where_supp + where_ls + where_serv);
               
                    ExecRead(out reader, sql.ToString());
                }

            }
            reader.Close();
        
            string sql228 = "select nzp_serv, nzp_supp, nzp_area, num_ls,  nkvar_n, nkvar, ndom, nkor, ulica, fio, "+
                " tarif, sum( money_to) as money_to,  sum(money_from) as money_from, sum(sum_insaldo) as sum_insaldo, sum(sum_tarif) as sum_tarif, sum(sum_lgota) as sum_lgota, "
                + " sum(sum_subsidy) as sum_subsidy,sum(sum_smo) as sum_smo , sum(real_charge) as real_charge, sum(reval_lgota) as reval_lgota, sum(reval_tarif) as reval_tarif,  " +
                "	(SUM (sum_insaldo + sum_tarif + real_charge + reval_tarif - (sum_lgota - reval_lgota))) AS h, " +
                " SUM (CASE	WHEN tarif = 0 THEN	 0	ELSE round((	sum_insaldo + sum_tarif + real_charge + reval_tarif - (sum_lgota - reval_lgota)) / tarif, 2) END) AS r, " +
                "	SUM (CASE WHEN tarif = 0 THEN 0	ELSE round((money_to + money_from) / tarif,2) END	) AS f, " +
                "	SUM ((money_to + money_from)) AS A, " +
                " (SUM (sum_insaldo + sum_tarif + real_charge + reval_tarif - (sum_lgota - reval_lgota))-SUM ((money_to + money_from))) as ha, "+
                " 	sum ( CASE	WHEN tarif = 0 THEN	 0	ELSE round( ((sum_insaldo + sum_tarif + real_charge + reval_tarif - (sum_lgota - reval_lgota))-((money_to + money_from)))/tarif,2 ) END ) AS hat, "+
                " sum(sum_insaldo+sum_tarif+real_charge+reval ) as sum4 , "+
                 "SUM (sum_lgota+reval_lgota) as vsego,  sum(sum_charge) as sum_charge, sum(reval) as reval, sum(money_del) AS money_del  "+
                 "from t_svod    GROUP BY 1,2,3,4,5,6,7,8,9,10,11 ;";
            DataTable dt = ExecSQLToTable(sql228);
         

            dt.TableName = "Q_master";
            #endregion
            var ds = new DataSet();
            ds.Tables.Add(dt);
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
                report.SetParameterValue("service", ServicesHeader);
                report.SetParameterValue("ed_izmer", ed_izmer); 
                report.SetParameterValue("date", DateTime.Now.ToLongDateString());
                report.SetParameterValue("time", DateTime.Now.ToLongTimeString());
                report.SetParameterValue("dats", dat_s);
                report.SetParameterValue("datpo", dat_po);
                report.SetParameterValue("s", NameTypeLs);
                report.SetParameterValue("dateprint", dateprint);
                report.SetParameterValue("ver", "1.0");
                report.SetParameterValue("area", AreasHeader);
              //  report.SetParameterValue("mes", months[Month]);
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
                TypeLs= UserParamValues["TypeLs"].GetValue<int>();
                Suppliers = UserParamValues["Suppliers"].GetValue<List<long>>();
                Areas = UserParamValues["Areas"].GetValue<List<int>>();
                //Month = UserParamValues["Month"].GetValue<int>();
                //Year = UserParamValues["Year"].GetValue<int>();

                string period = UserParamValues["Period"].GetValue<string>();
                DateTime d1;
                DateTime d2;
                PeriodParameter.GetValues(period, out d1, out d2);
                dat_s = d1.ToShortDateString();
                dat_po = d2.ToShortDateString();
            }
            catch (Exception ex)
            {
                throw new ReportException("Произошла ошибка при формировании отчета " + ex.Message, ex);
            }
        }

        protected override void CreateTempTable()
        {
            string sql = "create temp table t_svod ( " +
          "  nzp_serv integer, " +
          "  nzp_area integer, " +
          "  nzp_supp integer,   " +
          "  nzp_kvar integer, " +
          "  num_ls integer, " +
          "  nkvar_n CHAR(3), " +
          "  nkvar CHAR(10), " +
          "  nkor CHAR(3), " +
          "  ulica CHAR(40), " +
          "  ndom CHAR(10), " +
          "  fio CHAR(40), " +
          "  tarif " + DBManager.sDecimalType + "(14,2), " +
          "  money_to " + DBManager.sDecimalType + "(14,2), " +
          "  money_from " + DBManager.sDecimalType + "(14,2), " +
          "  sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
          "  sum_tarif " + DBManager.sDecimalType + "(14,2), " +
          "  sum_lgota " + DBManager.sDecimalType + "(14,2), " +
          "  sum_real " + DBManager.sDecimalType + "(14,2), " +
          "  sum_charge " + DBManager.sDecimalType + "(14,2)," +
          "  sum_subsidy " + DBManager.sDecimalType + "(14,2)," +
          "  reval_tarif " + DBManager.sDecimalType + "(14,2)," +
          "  reval_lgota " + DBManager.sDecimalType + "(14,2)," +
          "  reval_sub " + DBManager.sDecimalType + "(14,2)," +
          "  reval_smo " + DBManager.sDecimalType + "(14,2)," +
          "  reval " + DBManager.sDecimalType + "(14,2)," +
          "  money_del " + DBManager.sDecimalType + "(14,2)," +
          "  sum_money " + DBManager.sDecimalType + "(14,2)," +
          "  sum_outsaldo " + DBManager.sDecimalType + "(14,2)," +                
          "  real_charge " + DBManager.sDecimalType + "(14,2)," +
         // "sum_tarif" + DBManager.sDecimalType + "(14,2)," +
          "  sum_smo " + DBManager.sDecimalType + "(14,2), rashod " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_svod ", true);
        }

    }
}
