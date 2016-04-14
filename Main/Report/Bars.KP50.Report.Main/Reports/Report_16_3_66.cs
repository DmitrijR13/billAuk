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
    class Nach_rash_mkd : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.3.66 Начисление и расход в разрезе МКД без общедомовых ПУ"; }
        }

        public override string Description
        {
            get { return "3.66 Начисление и расход в разрезе МКД без общедомовых ПУ"; }
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
            get { return Resources._3_66_nach_rash_mkd; }
        }

        public override ReportKind ReportKind
        {
            get { return ReportKind.Base; }
        }

        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>След месяц</summary>
        protected int NextMonth { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        /// <summary>Следующий год</summary>
        protected int NextYear { get; set; }

        /// <summary>Услуга</summary>
        protected int Services { get; set; }

        /// <summary>Услуга ОДН</summary>
        protected int Services_ODN { get; set; }

        /// <summary>Услуга заголовок</summary>
        protected string ServiceHeader { get; set; }





        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new MonthParameter{ Value = DateTime.Now.Month },
                new YearParameter{ Value = DateTime.Now.Year },
                //new SupplierParameter(),
                //new AreaParameter(false),                
                new ComboBoxParameter
                {
                    Code = "Serv",
                    Name = "Услуга",
                    Value = "6",
                    StoreData = new List<object>
                    {
                        new { Id = "6", Name = "Холодная вода" },
                        new { Id = "9", Name = "Горячая вода" },
                        new { Id = "25", Name = "Электроснабжение" }
                    }
                }                 
            };
        }

        public override DataSet GetData()
        {
            MyDataReader reader;
            //StringBuilder sql = new StringBuilder();
            string sql = "";
            //string where_supp = "";
            //string where_area = "";
            string where_serv = "";

            #region Ограничения


            if (Services != null)
            {
                DataTable serv = ExecSQLToTable("select service from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services s where s.nzp_serv = " + Services);
                ServiceHeader = serv.Rows[0][0].ToString().Trim();
            }

            #endregion


            #region Выборка по локальным банкам


            sql = " SELECT * " +
                  " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                  " WHERE nzp_wp>1 ";
            ExecRead(out reader, sql);

            while (reader.Read())
            {
                string pref = reader["bd_kernel"].ToString().ToLower().Trim();
                sql = " insert into t_counters_dom (nzp_dom, nzp_counter, dat_close) " +
                      " select nzp_dom, nzp_counter, max(dat_close) as dat_close from " + 
                    pref + DBManager.sDataAliasRest + "counters_dom group by 1,2 ";
                ExecSQL(sql);
            }

            sql = "create index ix_t_counters_dom on t_counters_dom (nzp_dom) ";
            ExecSQL(sql);
            sql = DBManager.sUpdStat + " t_counters_dom";
            ExecSQL(sql);

            sql = " SELECT * " +
                  " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                  " WHERE nzp_wp>1 ";
            ExecRead(out reader, sql);

            while (reader.Read())
            {
                ////test
                //String b = "123";



                ////test


                string pref = reader["bd_kernel"].ToString().ToLower().Trim();

                sql = " insert into t_svod select u.ulica, d.idom, d.nkor, "+
                        " sum(case when is_device in (0,2,4,6,8) and nzp_serv = "+Services+" then rsum_tarif else 0 end) n, "+
                        " sum(case when is_device in (1,3,5,7,9) and nzp_serv = "+Services+" then rsum_tarif else 0 end) n_pu, "+
                        " sum(case when is_device in (0,2,4,6,8) and nzp_serv = "+Services+" then c_calc else 0 end) r, "+
                        " sum(case when is_device in (1,3,5,7,9) and nzp_serv = "+Services+" then c_calc else 0 end) r_pu, "+
                        " sum(case when nzp_serv = "+Services_ODN+" then rsum_tarif else 0 end) r_odn, "+
                        " sum(case when nzp_serv = "+Services_ODN+" then c_calc else 0 end) n_odn "+
                        " from " + pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + Month.ToString("00") + " c, " +
                          ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, " +
                          ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                          ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u " +
                        " where k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul " +
                        " and c.num_ls = k.num_ls and k.num_ls >0 and nzp_serv in ("+Services+", "+Services_ODN+") "+
                        " and dat_charge is null "+
                        // не работающие
                        " and k.nzp_dom not in (select nzp_dom from "+pref+DBManager.sDataAliasRest+"counters_dom "+
                        " where dat_uchet = '01."+NextMonth.ToString("00")+"."+NextYear+"') "+
                        // ОДПУ
                        " and k.nzp_dom in (select nzp from "+pref+DBManager.sDataAliasRest+"counters_spis where nzp_type = 1) "+
                        " group by 1,2,3 ";
                ExecSQL(sql);
                sql = " insert into t_svod1 (nzp_dom, ulica, idom, nkor, n, n_pu, r, r_pu, n_odn, r_odn) "+
                        " select d.nzp_dom, u.ulica, d.idom, d.nkor, "+
                        " sum(case when is_device in (0,2,4,6,8) and nzp_serv = "+Services+" then rsum_tarif else 0 end) n, "+
                        " sum(case when is_device in (1,3,5,7,9) and nzp_serv = "+Services+" then rsum_tarif else 0 end) n_pu, "+
                        " sum(case when is_device in (0,2,4,6,8) and nzp_serv = "+Services+" then c_calc else 0 end) r, "+
                        " sum(case when is_device in (1,3,5,7,9) and nzp_serv = "+Services+" then c_calc else 0 end) r_pu, "+
                        " sum(case when nzp_serv = "+Services_ODN+" then rsum_tarif else 0 end) r_odn, "+
                        " sum(case when nzp_serv = "+Services_ODN+" then c_calc else 0 end) n_odn "+
                        " from "+pref+"_charge_"+(Year-2000).ToString("00")+DBManager.tableDelimiter+"charge_"+Month.ToString("00")+" c, " +
                          ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, " +
                          ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                          ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u " +
                        " where k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul " +
                        " and c.num_ls = k.num_ls and k.num_ls >0 and nzp_serv in ("+Services+", "+Services_ODN+") "+
                        " and dat_charge is null "+
                        " and k.nzp_dom not in (select nzp_dom from t_counters_dom where dat_close is null) " +
                        " group by 1,2,3,4 ";
                ExecSQL(sql);
                sql = " insert into t_svod2 select u.ulica, d.idom, d.nkor, "+
                        " sum(case when is_device in (0,2,4,6,8) and nzp_serv = "+Services+" then rsum_tarif else 0 end) n, "+
                        " sum(case when is_device in (1,3,5,7,9) and nzp_serv = "+Services+" then rsum_tarif else 0 end) n_pu, "+
                        " sum(case when is_device in (0,2,4,6,8) and nzp_serv = "+Services+" then c_calc else 0 end) r, "+
                        " sum(case when is_device in (1,3,5,7,9) and nzp_serv = "+Services+" then c_calc else 0 end) r_pu, "+
                        " sum(case when nzp_serv = "+Services_ODN+" then rsum_tarif else 0 end) r_odn, "+
                        " sum(case when nzp_serv = "+Services_ODN+" then c_calc else 0 end) n_odn "+
                        " from "+pref+"_charge_"+(Year-2000).ToString("00")+DBManager.tableDelimiter+"charge_"+Month.ToString("00")+" c, " +
                          ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, " +
                          ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                          ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u " +
                        " where k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul " +
                        " and c.num_ls = k.num_ls and k.num_ls >0 and nzp_serv in ("+Services+", "+Services_ODN+") "+
                        " and dat_charge is null "+
                        // ОДПУ
                        " and k.nzp_dom in (select nzp from " + pref + DBManager.sDataAliasRest + "counters_spis where nzp_type = 1) " +
                        " group by 1,2,3 ";
                ExecSQL(sql);
                sql = "update t_svod1 set pl = " +
                        "( select max(val_prm) from " + pref + DBManager.sDataAliasRest + "prm_2 where " +
                        " nzp_prm = 40 and is_actual <> 100 and " +
                        " dat_s <= '01." + Month.ToString("00") + "." + Year + "'  and dat_po >= '01." + Month.ToString("00") + "." + Year + "' and nzp = t_svod1.nzp_dom)";
                ExecSQL(sql);
                sql = " insert into t_svod3 select ulica, idom, nkor, r, r_pu, n, n_pu, r_odn, n_odn, 1 as ttype from t_Svod1; " +
                      " insert into t_svod3 select *, 2 as ttype from t_Svod; " +
                      " insert into t_svod3 select *, 3 as ttype from t_Svod2; ";
                ExecSQL(sql);
            }
            reader.Close();

            DataTable dt = ExecSQLToTable(" select ulica, idom, nkor, " + DBManager.sNvlWord + "(pl,0)" + DBManager.sConvToNum + " as pl, r_pu, n_pu, r, n, r_odn, n_odn from t_svod1 order by 1,2");
            dt.TableName = "Q_master";
            DataTable dt1 = ExecSQLToTable(" select * from t_svod order by 1,2");  
            dt1.TableName = "Q_master1";
            DataTable dt2 = ExecSQLToTable(" select * from t_svod2 order by 1,2");
            dt2.TableName = "Q_master2";
            DataTable dt3 = ExecSQLToTable(" select " +
                " (case when ttype=1 then 'без общедомовых ПУ' when ttype=2 then 'с общедомовыми неработающими ПУ' when ttype=3 then 'с общедомовыми ПУ' else '' end) as ttype, " +
                " sum(r_pu) as r_pu, sum(n_pu) as n_pu, sum(r) as r,  sum(n) as n, sum(r_odn) as r_odn, sum(n_odn) as n_odn from t_svod3 group by 1 ");
            dt3.TableName = "Q_master3";
            #endregion

            var ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables.Add(dt1);
            ds.Tables.Add(dt2);
            ds.Tables.Add(dt3);
            return ds;
        }

        protected override void PrepareReport(Report report)
        {
            string[] months = new string[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("dat", DateTime.Now.ToShortDateString());
            report.SetParameterValue("time", DateTime.Now.ToShortTimeString());
            report.SetParameterValue("month", months[Month]);
            report.SetParameterValue("year", Year);
            report.SetParameterValue("rep_name", "Начисления и расход " + ServiceHeader + " в разрезе МКД без общедомовых приборов учета");
            report.SetParameterValue("rep_name1", "Начисления и расход " + ServiceHeader + " в разрезе МКД, оборудованных общедомовыми приборами учета, но не работающими");
            report.SetParameterValue("rep_name2", "Начисления и расход " + ServiceHeader + " в разрезе МКД, оборудованных общедомовыми приборами учета");
            report.SetParameterValue("rep_name3", "Свод начислений и расход " + ServiceHeader + " по Управляющим организациям");
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            Services = UserParamValues["Serv"].GetValue<int>(); 
            switch(Services)
            {
                case 6:                     
                    Services_ODN = 510;
                    break;
                case 9:
                    Services_ODN = 513;
                    break;
                case 25:
                    Services_ODN = 515;
                    break;
            }
            if (Month == 12) { NextMonth = 1; NextYear = Year + 1; } else { NextMonth = Month + 1; NextYear = Year; }
        }

        protected override void CreateTempTable()
        {
            string sql = "create temp table t_svod ( " +
                 " ulica char(40), " +
                 " idom integer, " +
                 " nkor char(3), " +
                 " n " + DBManager.sDecimalType + "(14,2), " +
                 " n_pu " + DBManager.sDecimalType + "(14,2), " +
                 " r " + DBManager.sDecimalType + "(14,2), " +
                 " r_pu " + DBManager.sDecimalType + "(14,2), " +
                 " n_odn " + DBManager.sDecimalType + "(14,2), " +
                 " r_odn " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            sql = "create temp table t_svod1 ( " +
                 " nzp_dom integer, " +
                 " ulica char(40), " +
                 " idom integer, " +
                 " nkor char(3), " +
                 " n " + DBManager.sDecimalType + "(14,2), " +
                 " n_pu " + DBManager.sDecimalType + "(14,2), " +
                 " r " + DBManager.sDecimalType + "(14,2), " +
                 " r_pu " + DBManager.sDecimalType + "(14,2), " +
                 " n_odn " + DBManager.sDecimalType + "(14,2), " +
                 " r_odn " + DBManager.sDecimalType + "(14,2), " +
                 " pl char(20)) "  + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            sql = "create temp table t_svod2 ( " +
                 " ulica char(40), " +
                 " idom integer, " +
                 " nkor char(3), " +
                 " n " + DBManager.sDecimalType + "(14,2), " +
                 " n_pu " + DBManager.sDecimalType + "(14,2), " +
                 " r " + DBManager.sDecimalType + "(14,2), " +
                 " r_pu " + DBManager.sDecimalType + "(14,2), " +
                 " n_odn " + DBManager.sDecimalType + "(14,2), " +
                 " r_odn " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            sql = "create temp table t_svod3 ( " +
                 " ulica char(40), " +
                 " idom integer, " +
                 " nkor char(3), " +
                 " n " + DBManager.sDecimalType + "(14,2), " +
                 " n_pu " + DBManager.sDecimalType + "(14,2), " +
                 " r " + DBManager.sDecimalType + "(14,2), " +
                 " r_pu " + DBManager.sDecimalType + "(14,2), " +
                 " n_odn " + DBManager.sDecimalType + "(14,2), " +
                 " r_odn " + DBManager.sDecimalType + "(14,2), " +
                 " ttype char(1)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
            sql = "create temp table t_counters_dom ( " +
                  " nzp_dom integer, " +
                  " nzp_counter integer, " +
                  " dat_close date ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_svod; drop table t_svod1; drop table t_svod2; drop table t_svod3; drop table t_counters_dom; ", true);
        }

    }
}
