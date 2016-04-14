using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bars.KP50.Report.Samara
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Data;
    using Report;
    using Base;
    using Properties;
    using Utils;

    using FastReport;

    using STCLINE.KP50.DataBase;
    using STCLINE.KP50.Global;
    class Turn_off_gku : BaseSqlReport
    {
        public override string Name
        {
            get { return "30.1.9 Справка по отключениям подачи жилищных и коммунальных услуг"; }
        }

        public override string Description
        {
            get { return "Справка по отключениям подачи жилищных и коммунальных услуг"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Finans };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Turn_off_gku; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>Дата с </summary>
        protected DateTime DatS { get; set; }

        /// <summary>Дата по </summary>
        protected DateTime DatPo { get; set; }

        /// <summary>УК</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AreaHeader { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }


        public override List<UserParam> GetUserParams()
        {
            
            return new List<UserParam>
            {
                new MonthParameter {Value = DateTime.Today.Month },
                new YearParameter {Value = DateTime.Today.Year },
                new SupplierAndBankParameter(),
                new AreaParameter()
            };
        }

        protected override void PrepareReport(Report report)
        {
            string[] months =
            {"","январь","февраль",
                "март","апрель","май","июнь","июль","август","сентябрь",
                "октябрь","ноябрь","декабрь"};
            report.SetParameterValue("dat", DateTime.Now.ToLongDateString());
            report.SetParameterValue("time", DateTime.Now.ToLongTimeString());
            report.SetParameterValue("month", months[DatS.Month]);
            report.SetParameterValue("year", DatS.Year);
            //report.SetParameterValue("area", AreaHeader);
            report.SetParameterValue("town", "г.о. Жигулевск");
        }


        protected override void PrepareParams()
        {
            DatS = new DateTime(UserParamValues["Year"].GetValue<int>(),UserParamValues["Month"].GetValue<int>(),1);
            DatPo = DatS.AddMonths(1).AddDays(-1);
            Areas = UserParamValues["Areas"].GetValue<List<int>>();

            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
        }

        public override DataSet GetData()
        {
            MyDataReader reader;
            string whereArea = GetWhereArea("k.");
            string whereSupp = GetWhereSupp("ch.");

            string sql = " select bd_kernel as pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " where nzp_wp>1 " + GetwhereWp();

            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();
                sql = " insert into t_kdu (nzp_area, nzp_kvar, nzp_dom, nzp_ul, nzp_raj, num_ls, ulica, idom, nkor, ikvar, nkvar, gil) " +
                      " select k.nzp_area, k.nzp_kvar, d.nzp_dom, u.nzp_ul, u.nzp_raj, k.num_ls, " +
                      " u.ulica, d.idom, d.nkor, k.ikvar, k.nkvar, max(round(gil)) " +
                      " from " + pref + DBManager.sDataAliasRest + "kvar k, " +
                        pref + DBManager.sDataAliasRest + "dom d, " +
                        pref + DBManager.sDataAliasRest + "s_ulica u, " +
                        pref + "_charge_" + (DatS.Year - 2000) + DBManager.tableDelimiter + "calc_gku_" + DatS.Month.ToString("00") + " cg " +
                      " where k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul and k.nzp_kvar = cg.nzp_kvar " + whereArea +
                      " group by 1,2,3,4,5,6,7,8,9,10,11 ";
                ExecSQL(sql);
                ExecSQL(" create index kdu_ndx on t_kdu(nzp_kvar) ");
                ExecSQL(DBManager.sUpdStat + " t_kdu ");

                sql = " insert into t_svod (ulica, idom, nkor, nzp_supp, nzp_vinovnik, nzp_serv, " +
                      " kvar_count, dat_s, dat_po, count_day, count_hour, gil, nedop) " +
                      " select ulica, idom, case when nkor<>'-' then nkor end as nkor, " +
                      " nk.nzp_supp, nk.nzp_supp as nzp_vinovnik, nk.nzp_serv, " +
                      " count(distinct nk.nzp_kvar) as kvar_count, " +
                      " nk.dat_s, " +
                      " nk.dat_po, " +
                        DBManager.SetInterval("dat_po  - dat_s", "day") + " as count_day, " +
                        DBManager.SetInterval("dat_po  - dat_s", "hour") + " as count_hour, " +
                      " sum(gil) as gil, " +
                      " sum(" + DBManager.sNvlWord + "(sum_nedop,0) - " + DBManager.sNvlWord + "(sum_nedop_p,0)) as nedop " +
                      " from t_kdu t, " +
                        pref + "_charge_" + (DatS.Year - 2000) + DBManager.tableDelimiter + "charge_" + DatS.Month.ToString("00") + " ch, " +
                        pref + DBManager.sDataAliasRest +"nedop_kvar nk " +
                      " where t.nzp_kvar = ch.nzp_kvar and t.nzp_kvar = nk.nzp_kvar " + whereSupp +
                      " and ch.nzp_serv > 1 and ch.dat_charge is null and abs(sum_nedop-sum_nedop_p)>0.001 " +
                      " and dat_s>= date('" + DatS.ToShortDateString() + "') and dat_po<= date('" + DatPo.ToShortDateString() + "') " +
                      " group by 1,2,3,4,5,6,8,9 ";
                ExecSQL(sql);
                ExecSQL(" delete from t_kdu ");
                ExecSQL(" drop index kdu_ndx ");
            }
            reader.Close();
            #region Выборка на экран
            sql = " select ulica, idom, nkor, " +
                  " name_supp, name_supp as vinovnik, service, ordering, " +
                  " max(kvar_count) as kvar_count, " +
                  " t.dat_s, " +
                  " t.dat_po, " +
                  " sum(count_day) as count_day, " +
                  " sum(count_hour) as count_hour, " +
                  " max(gil) as gil, " +
                  " sum(nedop) as nedop " +
                  " from t_svod t, " +
                    ReportParams.Pref + DBManager.sKernelAliasRest + "services se, " +
                    ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su " +
                  " where t.nzp_supp = su.nzp_supp and t.nzp_serv = se.nzp_serv and t.nzp_vinovnik = su.nzp_supp " +
                  " group by 1,2,3,4,5,6,7,9,10 " +
                  " order by 1,2,3,4,5,7,9,10 ";
            #endregion
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            DataSet ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;

        }

        /// <summary>
        /// Получить условия органичения по УК
        /// </summary>
        /// <returns></returns>
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
            whereArea = !String.IsNullOrEmpty(whereArea) ? " AND " + tablePrefix + "nzp_area in (" + whereArea + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereArea))
            {
                string sql = " SELECT area from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area " + tablePrefix.TrimEnd(',') + 
                             "  WHERE " + tablePrefix+ "nzp_area > 0 " + whereArea;
                DataTable area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    AreaHeader += dr["area"].ToString().Trim() + ", ";
                }
                AreaHeader = AreaHeader.TrimEnd(',', ' ');
            }
            return whereArea;
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp(string tablePrefix)
        {
            string whereSupp = String.Empty;
            if (Suppliers != null)
            {
                whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
            }
            else
            {
                whereSupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);
            }
            whereSupp = whereSupp.TrimEnd(',');
            whereSupp = !String.IsNullOrEmpty(whereSupp) ? " AND " + tablePrefix + "nzp_supp in (" + whereSupp + ")" : String.Empty;
            return whereSupp;
        }

        /// <summary>
        /// Получить условия органичения по банкам данных
        /// </summary>
        /// <returns></returns>
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
            return whereWp;
        }

        protected override void CreateTempTable()
        {
            string sql = " create temp table t_svod( " +
                        " ulica char(40), " +
                        " idom integer, " +
                        " nkor char(10), " +
                        " nzp_supp integer, " +
                        " nzp_vinovnik integer, " +
                        " nzp_serv integer, " +
                        " kvar_count integer, " +
                        " dat_s char(100), " +
                        " dat_po char(100), " +
                        " count_day integer, " +
                        " count_hour integer, " +
                        " gil integer, " +
                        " nedop " + DBManager.sDecimalType + "(14,4))";
            ExecSQL(sql);
            sql = " create temp table t_kdu( " +
                        " nzp_area integer," +
                        " nzp_kvar integer," +
                        " nzp_dom integer," +
                        " nzp_ul integer," +
                        " nzp_raj integer," +
                        " num_ls integer," +
                        " ulica char(40), " +
                        " idom integer, " +
                        " nkor char(10), " +
                        " ikvar integer, " +
                        " nkvar char(10), " +
                        " gil integer) ";
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_svod ");
            ExecSQL(" drop table t_kdu ");
        }

    }
}
