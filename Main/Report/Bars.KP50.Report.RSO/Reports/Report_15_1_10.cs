using System;
using System.Collections.Generic;
using System.Data;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using Bars.KP50.Report.RSO.Properties;
using STCLINE.KP50.Global;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Bars.KP50.Report.RSO.Reports
{
    class Report15110 : BaseSqlReport
    {
        public override string Name
        {
            get { return "15.1.10 Рассогласование с паспортисткой"; }
        }

        public override string Description
        {
            get { return "Рассогласование с паспортисткой"; }
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

        protected override byte[] Template
        {
            get { return Resources.Report_15_1_10; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }


        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AreaHeader { get; set; }


        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                 new MonthParameter{ Value = DateTime.Now.Month },
                 new YearParameter{ Value = DateTime.Now.Year },
                 new SupplierAndBankParameter(),
                 new AreaParameter()
            };
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].Value.To<int>();
            Areas = UserParamValues["Areas"].Value.To<List<int>>();
            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            string[] month = { "", "январь", "феврль", "март", "апрель", 
                                    "май", "июнь", "июль", "август", "сентябрь",
                                        "октябрь", "ноябрь", "декабрь" };
            report.SetParameterValue("date",DateTime.Now.ToShortDateString());
            report.SetParameterValue("time",DateTime.Now.ToShortTimeString());
            report.SetParameterValue("period","за " + month[Month] + " месяц " + Year + " г." );
            report.SetParameterValue("area",!string.IsNullOrEmpty(AreaHeader) ? "Балансодержатель: " + AreaHeader : string.Empty);

        }

        public override DataSet GetData()
        {

            string sql = " SELECT bd_kernel AS pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " WHERE nzp_wp > 1 " + GetWhereWp();
            DataTable dpref = ExecSQLToTable(sql);
            var datS = new DateTime(Year, Month, 1);
            var datPo = new DateTime(Year, Month, DateTime.DaysInMonth(Year,Month));

            foreach (DataRow dr in dpref.Rows)
            {
                if (dr["pref"] == null) continue;
                string pref = dr["pref"].ToString().Trim();
                string prefData = pref + DBManager.sDataAliasRest;

                sql = " INSERT INTO t_report_15_1_10(nzp_kvar, num_ls, town, nzp_raj, rajon, ulica, idom, ndom, nkor, ikvar, nkvar, nkvar_n, fio) " +
                      " SELECT k.nzp_kvar, k.num_ls, t.town, r.nzp_raj, r.rajon, u.ulica, d.idom, d.ndom, d.nkor, k.ikvar, k.nkvar, k.nkvar_n, k.fio " +
                      " FROM " + prefData + "kvar k INNER JOIN " + prefData + "dom d ON d.nzp_dom = k.nzp_dom " +
                                                  " INNER JOIN " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                                                  " INNER JOIN " + prefData + "s_rajon r ON r.nzp_raj = d.nzp_raj " +
                                                  " INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town " +
                      " WHERE 0 = (SELECT COUNT(*) " +
                                 " FROM " + prefData + "prm_1 p1 " +
                                 " WHERE p1.nzp = k.nzp_kvar " +
                                   " AND p1.nzp_prm = 130 " +
                                   " AND p1.val_prm = '1' " +
                                   " AND p1.is_actual <> 100 " +
                                   " AND p1.dat_s <= DATE('" + datPo.ToShortDateString() + "') " +
                                   " AND p1.dat_po >= DATE('" + datS.ToShortDateString() + "')) " +
                      " AND (SELECT COUNT(nzp_kart) " +
                            " FROM " + prefData + "kart kr " +
                            " WHERE kr.nzp_kvar = k.nzp_kvar " +
                              " AND nzp_tkrt = 1 " +
                              " AND isactual = '1' ) <> " + DBManager.sNvlWord +
                                " ((SELECT MAX(REPLACE(p.val_prm,',','.') " + DBManager.sConvToNum + ") " +
                                 " FROM " + prefData + "prm_1 p " +
                                 " WHERE p.nzp = k.nzp_kvar " +
                                   " AND p.nzp_prm = 5 " +
                                   " AND p.is_actual <> 100 " +
                                   " AND p.dat_s <= DATE('" + datPo.ToShortDateString() + "') " +
                                   " AND p.dat_po >= DATE('" + datS.ToShortDateString() + "')), 0 ) " + GetWhereArea("k.");
                ExecSQL(sql);

                sql = " UPDATE t_report_15_1_10 " +
                      " SET sost = " + DBManager.sNvlWord + "(( " +
                      " SELECT CASE WHEN val_prm = '1' THEN 'открыт' ELSE 'закрыт' END  " +
                      " FROM " + prefData + "prm_3 p " +
                      " WHERE p.nzp = t_report_15_1_10.nzp_kvar " +
                        " AND p.nzp_prm = 51 " +
                        " AND p.is_actual <> 100 " +
                        " AND " + DBManager.sCurDate + " BETWEEN p.dat_s AND p.dat_po),'неопределенно') ";
                ExecSQL(sql);

                sql = " UPDATE t_report_15_1_10 " +
                      " SET kol_gil = (SELECT COUNT(nzp_kart)" +
                                     " FROM " + prefData + "kart k " +
                                     " WHERE k.nzp_kvar = t_report_15_1_10.nzp_kvar" +
                                       " AND nzp_tkrt = 1 " +
                                       " AND isactual = '1' ) ";
                ExecSQL(sql);

                sql = " UPDATE t_report_15_1_10 " +
                      " SET kol_prm = " + DBManager.sNvlWord + "( (SELECT MAX(REPLACE(p.val_prm,',','.') " + DBManager.sConvToNum + ") " +
                                     " FROM " + prefData + "prm_1 p " +
                                     " WHERE p.nzp = t_report_15_1_10.nzp_kvar " +
                                       " AND p.nzp_prm = 5 " +
                                       " AND p.is_actual <> 100 " +
                                       " AND p.dat_s <= DATE('" + datPo.ToShortDateString() + "') " +
                                       " AND p.dat_po >= DATE('" + datS.ToShortDateString() + "')), 0) ";
                ExecSQL(sql);
            }

            sql = " SELECT town, nzp_raj, rajon, ulica, idom, ndom, nkor, ikvar, nkvar, nkvar_n, fio, num_ls, sost, kol_gil, kol_prm " +
                  " FROM t_report_15_1_10 " +
                  " ORDER BY town, rajon, ulica, idom, ndom, nkor, ikvar, nkvar, nkvar_n, num_ls ";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        } 

        /// <summary>
        /// Получить условия органичения по банкам
        /// </summary>
        /// <returns></returns>
        private string GetWhereWp()
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
            return whereWp;
        }

        /// <summary>
        /// Получает условия ограничения по территории
        /// </summary>
        /// <returns></returns>
        private string GetWhereArea(string pre)
        {
            string whereAdr = String.Empty;
            whereAdr = Areas != null
                ? Areas.Aggregate(whereAdr, (current, nzpArea) => current + (nzpArea + ","))
                : ReportParams.GetRolesCondition(Constants.role_sql_area);
            whereAdr = whereAdr.TrimEnd(',');
                

            if (!String.IsNullOrEmpty(whereAdr))
            {
                whereAdr = " AND " + pre + "nzp_area in (" + whereAdr + ")";

                if (!String.IsNullOrEmpty(whereAdr))
                {
                    string sql = " SELECT area from " +
                                 ReportParams.Pref + DBManager.sDataAliasRest + "s_area " + pre.TrimEnd('.') +
                                 " WHERE nzp_area > 0 " + whereAdr;
                    DataTable area = ExecSQLToTable(sql);
                    foreach (DataRow dr in area.Rows)
                    {
                        AreaHeader += dr["area"].ToString().Trim() + ",";
                    }
                    AreaHeader = AreaHeader.TrimEnd(',');
                }
            }
            return whereAdr;
        }

        protected override void CreateTempTable()
        {
            const string sql = " CREATE TEMP TABLE t_report_15_1_10( " +
                               " nzp_kvar integer," +
                               " num_ls integer," +
                               " town char(100)," +
                               " nzp_raj integer," +
                               " rajon char(100)," +
                               " ulica char(100)," +
                               " idom integer," +
                               " ndom char(10)," +
                               " nkor char(10)," +
                               " ikvar integer," +
                               " nkvar char(10)," +
                               " nkvar_n char(10)," +
                               " fio char(50)," +
                               " sost char(15)," +
                               " kol_gil integer," +
                               " kol_prm integer) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_report_15_1_10 ");
        }
    }
}
