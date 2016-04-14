﻿using System;
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
    class Report18 : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.1.8 Отчет по переплате по данным"; }
        }

        public override string Description
        {
            get { return "1.8 Отчет по переплате по данным"; }
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
            get { return Resources.Report_16_1_8; }
        }

        public override ReportKind ReportKind
        {
            get { return ReportKind.Base; }
        }


        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AreaHeader { get; set; }


        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new MonthParameter { Value = DateTime.Today.Month },
                new YearParameter { Value = DateTime.Today.Year },
                new SupplierParameter(),
                new AreaParameter()
            };
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].Value.To<int>();
            Suppliers = UserParamValues["Suppliers"].Value.To<List<long>>();
            Areas = UserParamValues["Areas"].Value.To<List<int>>();
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            const bool isShow = false;
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};

            report.SetParameterValue("month", months[Month] + " месяц");

            report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
            report.SetParameterValue("TIME", DateTime.Now.ToLongTimeString());
            if (SupplierHeader == null && AreaHeader == null)
            {
                report.SetParameterValue("invisible_info", false);
            }
            else
            {
                report.SetParameterValue("invisible_info", true);
                SupplierHeader = SupplierHeader != null ? "Поставщик: " + SupplierHeader : SupplierHeader;
                AreaHeader = AreaHeader != null && SupplierHeader != null ? "Балансодержатель: " + AreaHeader + "\n" :
                    AreaHeader != null ? "Балансодержатель: " + AreaHeader : AreaHeader;
            }
            report.SetParameterValue("supplier",SupplierHeader );
            report.SetParameterValue("area", AreaHeader);
            report.SetParameterValue("invisible_footer", isShow);
        }

        public override DataSet GetData()
        {
            IDataReader reader;
            string sql;


            string whereSupp = string.Empty;
            string whereArea = string.Empty;

            #region Поставщики
            if (Suppliers != null)
                whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp.ToString(CultureInfo.InvariantCulture) + ","));
            whereSupp = whereSupp.TrimEnd(',');


            if (!String.IsNullOrEmpty(whereSupp))
            {
                whereSupp = " AND nzp_supp IN (" + whereSupp + ") ";
                sql = " SELECT name_supp from " +
                        ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                      " WHERE nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += dr["name_supp"].ToString().Trim() + ",";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',');
            }
            #endregion

            #region Управляющие компании
            if (Areas != null)
            {
                whereArea = Areas.Aggregate(whereArea, (current, nzpArea) => current + (nzpArea.ToString(CultureInfo.InvariantCulture) + ","));
            }

            whereArea = whereArea.TrimEnd(',');
            if (!String.IsNullOrEmpty(whereArea))
            {
                sql = " SELECT area from " +
                        ReportParams.Pref + DBManager.sDataAliasRest + "s_area " +
                      " WHERE nzp_area > 0 and nzp_area in(" + whereArea + ") ";
                DataTable area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    AreaHeader += dr["area"].ToString().Trim() + ",";
                }
                AreaHeader = AreaHeader.TrimEnd(',');
                whereArea = " AND kv.nzp_area IN (" + whereArea + ") ";
            }
            #endregion

            #region выборка в temp таблицу

            sql = " SELECT bd_kernel as pref " +
                  " FROM " + DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "s_point " +
                  " WHERE nzp_wp>1 ";
            
            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();

                string chargeTable = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + Month.ToString("00");
                string prefData = pref + DBManager.sDataAliasRest;

                if (TempTableInWebCashe(chargeTable))
                {
                    sql = " INSERT INTO Otchet_perep_dan(nzp_kvar, town, rajon, ulica, idom, ndom, nkor, ikvar, nkvar, fio, num_ls, sum_real, sum_outsaldo ) " +
                          " SELECT ch.nzp_kvar,  " + 
                                 " town, " +
                                 " rajon, " +
                                 " ulica, " +
                                 " idom,  " +
                                 " ndom, " +
                                 " nkor, " +
                                 " ikvar, " +
                                 " nkvar, " +
                                 " fio, " +
                                 " ch.num_ls, " +
                                 " SUM(sum_real) AS sum_real, " +
                                 " SUM(sum_outsaldo) AS sum_outsaldo " +
                          " FROM " + chargeTable + " ch, " +
                                     prefData + "s_town t, " +
                                     prefData + "s_rajon r, " +
                                     prefData + "s_ulica u, " +
                                     prefData + "dom d, " +
                                     prefData + "kvar kv " +
                       " WHERE ch.nzp_kvar=kv.nzp_kvar " +
                            " AND dat_charge IS NULL " +
                            " AND nzp_serv>1 " +
                            " AND ch.nzp_kvar = kv.nzp_kvar " +
                            " AND kv.nzp_dom = d.nzp_dom " +
                            " AND d.nzp_ul = u.nzp_ul " +
                            " AND u.nzp_raj = r.nzp_raj " +
                            " AND r.nzp_town = t.nzp_town " +
                              whereSupp + whereArea +
                          " GROUP BY 1,2,3,4,5,6,7,8,9,10,11 ";
                    ExecSQL(sql);
                }
            }
            reader.Close();
            #endregion

            sql = " SELECT town, rajon, ulica, idom, ndom, nkor, ikvar, nkvar, fio, num_ls, SUM(sum_real) AS sum_real, SUM(sum_outsaldo) AS sum_outsaldo, " +
                         " SUM(CASE WHEN sum_real=0 THEN 0 ELSE ROUND(sum_outsaldo/sum_real,4) END) AS persent " +
                  " FROM Otchet_perep_dan " +
                  " GROUP BY 1,2,3,4,5,6,7,8,9,10" +
                  " ORDER BY town, rajon, ulica, idom, nkor, ikvar, nkvar, fio ";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        protected override void CreateTempTable()
        {
            var sql = new StringBuilder();            
            sql.Append(" CREATE TEMP TABLE Otchet_perep_dan ( ");
            sql.Append(" nzp_kvar INTEGER, ");
            sql.Append(" town CHARACTER(30), ");
            sql.Append(" rajon CHARACTER(30), ");
            sql.Append(" ulica CHARACTER(40), ");
            sql.Append(" idom INTEGER, ");
            sql.Append(" ndom CHARACTER(10), ");
            sql.Append(" nkor CHARACTER(3), ");
            sql.Append(" ikvar INTEGER, ");
            sql.Append(" nkvar CHARACTER(10), ");
            sql.Append(" fio CHARACTER(40), ");
            sql.Append(" num_ls INTEGER, ");
            sql.Append(" sum_real " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" sum_outsaldo " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable); //Процент
            ExecSQL(sql.ToString());  
            
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE Otchet_perep_dan ");
        }
    }
}
