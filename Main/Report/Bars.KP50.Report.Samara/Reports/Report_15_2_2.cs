using System;
using System.Collections.Generic;
using System.Data;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Samara.Properties;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Report.Samara.Reports
{
    public class Report1522 : BaseSqlReport
    {
        public override string Name
        {
            get { return "15.2.2 Выписка из лицевого счета"; }
        }

        public override string Description
        {
            get { return "Выписка из лицевого счета"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Cards };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_15_2_2; }
        }

        public override ReportKind ReportKind
        {
            get { return ReportKind.LC; }
        }

        /// <summary>Выписка для кого</summary>
        protected string Who { get; set; }


        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new StringParameter { Code = "Who", Name = "Для", DefaultValue = ""}
            };
        }

        protected override void PrepareParams()
        {
            Who = UserParamValues["Who"].GetValue<string>();
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Января","Февраля",
                 "Марта","Апреля","Мая","Июня","Июля","Августа","Сентября",
                 "Октября","Ноября","Декабря"};
            report.SetParameterValue("number_vip", ReportParams.NzpObject);
            report.SetParameterValue("day", DateTime.Now.Day);
            report.SetParameterValue("month", months[DateTime.Now.Month]);
            report.SetParameterValue("year", DateTime.Now.Year);
            report.SetParameterValue("who", Who); 
            report.SetParameterValue("director", "______________");
            report.SetParameterValue("oper", ReportParams.User.uname);
        }

        public override DataSet GetData()
        {

            MyDataReader reader;


            DataTable dt;

            var sql = " select pref " +
                         "from  " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar " +
                         "where nzp_kvar=" + ReportParams.NzpObject;
            ExecRead(out reader, sql);

            if (reader.Read())
            {
                string pref = reader["pref"].ToString().Trim();
                
                //Общая площадь

                sql = " INSERT INTO t_ls (ob_s) " +
                      " SELECT val_prm as ob_s " +
                      " FROM " + pref + DBManager.sDataAliasRest + "prm_1 " +
                      " WHERE nzp=" + ReportParams.NzpObject +
                      "        AND is_actual=1 " +
                      "        AND dat_s<=" + DBManager.sCurDate +
                      "        AND dat_po>=" + DBManager.sCurDate +
                      "        AND nzp_prm = 4 ";
                ExecSQL(sql);


                //Количество жильцов

                sql = " INSERT INTO t_ls (gil) " +
                      " SELECT round(gil) as gil " +
                      " FROM " + pref + "_charge_" + (DateTime.Now.Year - 2000).ToString("00") +
                        DBManager.tableDelimiter + "calc_gku_" + DateTime.Now.Month.ToString("00") +
                      " WHERE nzp_kvar=" + ReportParams.NzpObject +
                      "        AND dat_charge is null ";
                ExecSQL(sql);
                

                //Квартирные параметры

                sql = " INSERT INTO t_ls (town, rajon, ulica, ndom, nkor, nkvar, nkvar_n, fio) " +
                      " SELECT town, rajon, ulica, ndom, nkor, trim(case when nkvar <> '-' then nkvar end) as nkvar, " +
                      " (case when nkvar_n <> '-' then ' комн.' || nkvar_n end) as nkvar_n, fio " +
                      " FROM " + pref + DBManager.sDataAliasRest + "s_town t, " +
                        pref + DBManager.sDataAliasRest + "s_rajon r, " +
                        pref + DBManager.sDataAliasRest + "s_ulica u, " +
                        pref + DBManager.sDataAliasRest + "dom d, " +
                        pref + DBManager.sDataAliasRest + "kvar k " +
                      " WHERE k.nzp_kvar = " + ReportParams.NzpObject +
                      "        AND k.nzp_dom = d.nzp_dom " +
                      "        AND d.nzp_ul = u.nzp_ul " +
                      "        AND u.nzp_raj = r.nzp_raj " +
                      "        AND r.nzp_town = t.nzp_town ";
                ExecSQL(sql);


                sql = " SELECT max(case when town ='-' then '' else town end) as town, " +
                      " max(rajon) as rajon, " +
                      " max(ulica) as ulica, " +
                      " max(ndom) as ndom, " +
                      " max(case when nkor ='-' then '' else nkor end) as nkor, " +
                      " max(nkvar) as nkvar, " +
                      " max(" + DBManager.sNvlWord + "(nkvar_n,'')) as nkvar_n," +
                      " max(" + DBManager.sNvlWord + "(fio,'')) as fio, " +
                      " max(" + DBManager.sNvlWord + "(ob_s,'')) as ob_s, " +
                      " max(" + DBManager.sNvlWord + "(gil,'')) as gil " +
                      " FROM t_ls ";
                dt = ExecSQLToTable(sql);

            }
            else
            {
                dt = new DataTable();
                dt.Columns.Add("ob_s", typeof(string));
                dt.Columns.Add("gil", typeof(string));
                dt.Columns.Add("town", typeof(string));
                dt.Columns.Add("rajon", typeof(string));
                dt.Columns.Add("ulica", typeof(string));
                dt.Columns.Add("ndom", typeof(string));
                dt.Columns.Add("nkor", typeof(string));
                dt.Columns.Add("nkvar", typeof(string));
                dt.Columns.Add("nkvar_n", typeof(string));
                dt.Columns.Add("fio", typeof(string));
            }
            dt.TableName = "Q_master";

            sql = " SELECT bd_kernel as pref " +
             " FROM " + DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "s_point " +
             " WHERE nzp_wp>1 " + GetwhereWp();

            ExecRead(out reader, sql);
            while (reader.Read())
            {
                if (reader["pref"] != null)
                {
                    string pref = reader["pref"].ToStr().Trim();
                    string chargeTable = pref + "_charge_" + (DateTime.Now.Year - 2000).ToString("00") + DBManager.tableDelimiter +
                     "charge_" + DateTime.Now.Month.ToString("00");
                    if (TempTableInWebCashe(chargeTable))
                    {
                        sql =
                            " INSERT INTO t_svod(nzp_serv, rsum_tarif, debt) " +
                            " SELECT  nzp_serv, rsum_tarif, " +
                            " sum_insaldo - sum_money + reval + real_charge as debt " +
                            " FROM " + chargeTable +
                            " WHERE nzp_kvar = " + ReportParams.NzpObject + " AND dat_charge is null and nzp_serv>1 ";
                        ExecSQL(sql);
                    }
                }
            }
            sql = " SELECT service, sum(rsum_tarif) as rsum_tarif, sum(debt) as debt " +
                   " FROM t_svod t, " +
                     ReportParams.Pref + DBManager.sKernelAliasRest + "services s " +
                   " where t.nzp_serv = s.nzp_serv " +
                   " group by 1 " +
                   " order by 1 ";
            DataTable dt1 = ExecSQLToTable(sql);
            dt1.TableName = "Q_master1";

            var ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables.Add(dt1);

            return ds;
        }

        private string GetwhereWp()
        {
            var result = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            return !String.IsNullOrEmpty(result) ? " and nzp_wp in (" + result + ") " : "";
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_ls ( " +
                               " ob_s " + DBManager.sDecimalType + "(14,2), " +
                               " gil integer, " +
                               " town char(30), " +
                               " rajon char(30), " +
                               " ulica char(40), " +
                               " ndom char(10), " +
                               " nkor char(3), " +
                               " nkvar char(10), " +
                               " nkvar_n char(3), " +
                               " fio char(40)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql); 
            sql = " CREATE TEMP TABLE t_svod ( " +
                  " nzp_serv integer, " +
                  " rsum_tarif " + DBManager.sDecimalType + "(14,2), " +
                  " debt " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_ls; DROP TABLE t_svod; ");
        }
    }
}
