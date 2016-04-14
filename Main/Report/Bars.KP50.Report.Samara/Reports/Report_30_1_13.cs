using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Samara.Properties;
using Bars.KP50.Utils;
using Castle.Core.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using Constants = STCLINE.KP50.Global.Constants;

namespace Bars.KP50.Report.Samara.Reports
{
    class Report30113 : BaseSqlReport
    {
        public override string Name
        {
            get { return "30.1.13 Оборотно-сальдовая ведомость"; }
        }

        public override string Description
        {
            get { return "Оборотно-сальдовая ведомость"; }
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
            get { return Resources.Report_30_1_13; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }


        /// <summary>Дата</summary>
        private int Month { get; set; }        
        
        /// <summary>Дата</summary>
        private int Year { get; set; }

        /// <summary>Районы</summary>
        private string Raions { get; set; }

        /// <summary>Улицы</summary>
        private string Streets { get; set; }

        /// <summary>Дома</summary>
        private string Houses { get; set; }

        /// <summary>УК</summary>
        private int Area { get; set; }

        /// <summary>УК</summary>
        private string AreaHeader { get; set; }

        /// <summary>Поставщики</summary>
        private List<long> Suppliers { get; set; }

        /// <summary>Банки данных</summary>
        private List<int> Banks { get; set; }


        public override List<UserParam> GetUserParams()
        {

            return new List<UserParam>
            {
                new MonthParameter {Value = DateTime.Today.Month },
                new YearParameter {Value = DateTime.Today.Year },
                new SupplierAndBankParameter(),
                new AreaParameter(false),
                new AddressParameter(),
            };
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            Area = UserParamValues["Areas"].GetValue<int>();

            var adr = UserParamValues["Address"].GetValue<AddressParameterValue>();

            if (!adr.Raions.IsNullOrEmpty())
            {
                Raions = adr.Raions.Aggregate(Raions, (current, nzpRajon) => current + (nzpRajon + ","));
                Raions = Raions.TrimEnd(',');
                Raions = "and r.nzp_raj in (" + Raions + ") ";
            }
            else return;

            if (!adr.Streets.IsNullOrEmpty())
            {
                Streets = adr.Streets.Aggregate(Streets, (current, nzpStreet) => current + (nzpStreet + ","));
                Streets = Streets.TrimEnd(',');
                Streets = "and u.nzp_ul in (" + Streets + ") ";
            }
            else return;

            if (!adr.Houses.IsNullOrEmpty())
            {
                List<string> goodHouses = adr.Houses.FindAll(x => x.Trim() != "" && x.Contains("'") == false);
                if (!goodHouses.IsNullOrEmpty())
                    Houses = "and d.ndom in (" + String.Join(",", goodHouses.Select(x => "'" + x + "'").ToArray()) + ") ";
            }

            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            
            report.SetParameterValue("month", months[Month]);
            report.SetParameterValue("year", Year);
            report.SetParameterValue("area", AreaHeader);
        }

        public override DataSet GetData()
        {
            IDataReader reader;
            var sql = new StringBuilder();
            #region выборка в temp таблицу

            string whereArea = GetWhereAdr("k.");
            string whereSupp = GetWhereSupp();
            var datS = new DateTime(Year, Month, 1);
            var datPo = datS.AddMonths(1).AddDays(-1);

            sql.Append(" SELECT bd_kernel AS pref " +
                  " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                  " WHERE nzp_wp>1 " + GetwhereWp());

            ExecRead(out reader, sql.ToString());
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();

                string chargeTable = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + Month.ToString("00");
                string tarifTable = pref + DBManager.sDataAliasRest + "tarif";
                if (TempTableInWebCashe(chargeTable) && TempTableInWebCashe(tarifTable))
                {
                    sql.Remove(0, sql.Length);
                    sql.Append("insert into t_adr (nzp_kvar, rajon, geu, ulica, idom, ndom, nkor, ikvar, nkvar, fio) " +
                        " SELECT nzp_kvar, " +
                        " case when rajon='-' then town else rajon||' район' end as rajon, " +  
                        " geu, ulica, idom, ndom, " + 
                        " case when nkor<>'-' then nkor end as nkor, ikvar, " + 
                        " case when nkvar<>'-' and nkvar<>'0' then nkvar end as nkvar, fio " +
                        " FROM " + 
                          pref + DBManager.sDataAliasRest + "s_town t, " +
                          pref + DBManager.sDataAliasRest + "s_rajon r, " +
                          pref + DBManager.sDataAliasRest + "s_ulica u, " +
                          pref + DBManager.sDataAliasRest + "dom d, " +
                          pref + DBManager.sDataAliasRest + "kvar k, " +
                          pref + DBManager.sDataAliasRest + "s_geu g " +
                        " WHERE k.nzp_dom = d.nzp_dom " + 
                        " AND d.nzp_ul = u.nzp_ul " + 
                        " AND u.nzp_raj = r.nzp_raj " + 
                        " AND r.nzp_town = t.nzp_town " + 
                        " AND k.nzp_geu = g.nzp_geu " +
                          whereArea + Raions + Streets + Houses);
                    ExecSQL(sql.ToString());

                    sql.Remove(0, sql.Length);
                    sql.Append(" insert into t_ved(nzp_kvar, sum_insaldo, nzp_supp, saldo_17, " +
                          " saldo_6, saldo_8, saldo_9, saldo_14, " +
                          " saldo_25, saldo_213, saldo_15, " +
                          " saldo_2, saldo_7, saldo_22, saldo_233) " +
                          " select nzp_kvar, " + 
                          " sum_insaldo, " +
                          " nzp_supp, " +
                          " case when nzp_serv = 17 then sum_insaldo end as saldo_17, " +
                          " case when nzp_serv = 6 then sum_insaldo end as saldo_6, " +
                          " case when nzp_serv = 8 then sum_insaldo end as saldo_8, " +
                          " case when nzp_serv = 9 then sum_insaldo end as saldo_9, " +
                          " case when nzp_serv = 14 then sum_insaldo end as saldo_14, " +
                          " case when nzp_serv = 25 then sum_insaldo end as saldo_25, " +
                          " case when nzp_serv = 213 then sum_insaldo end as saldo_213, " +
                          " case when nzp_serv = 15 then sum_insaldo end as saldo_15, " +
                          " case when nzp_serv = 2 then sum_insaldo end as saldo_2, " +
                          " case when nzp_serv = 7 then sum_insaldo end as saldo_7, " +
                          " case when nzp_serv = 22 then sum_insaldo end as saldo_22, " +
                          " case when nzp_serv = 233 then sum_insaldo end as saldo_233 " +
                          " from " + chargeTable +
                          " where nzp_supp > 1 and dat_charge is null " + whereSupp +
                          " and nzp_serv in (17,6,8,9,14,25,213,15,2,7,22,233) ");
                    ExecSQL(sql.ToString());

                    sql.Remove(0, sql.Length);
                    sql.Append(" update t_ved set is_actual = " +
                          " (case when nzp_kvar in (select nzp_kvar from " + tarifTable + " where is_actual <> 1 " +
                          " and dat_s < '" + datPo.ToShortDateString() + "' " +
                          " and dat_po > '" + datS.ToShortDateString() + "') then 'закрыт' else 'открыт' end) ");
                    ExecSQL(sql.ToString());

                    ExecSQL("create index ved_index on t_ved(nzp_kvar,nzp_supp,is_actual)");
                    ExecSQL(DBManager.sUpdStat + " t_ved");

                    sql.Remove(0, sql.Length);
                    sql.Append("insert into t_vedomost(nzp_kvar, sum_insaldo, nzp_supp, is_actual, saldo_17, " +  
                          " saldo_6, saldo_8, saldo_9, saldo_14,  " +
                          " saldo_25, saldo_213, saldo_15,  " +
                          " saldo_2, saldo_7, saldo_22, saldo_233) " + 
                          " select nzp_kvar, " +
                          " sum(sum_insaldo) as sum_insaldo, " + 
                          " nzp_supp, is_actual, " +
                          " sum(saldo_17) as saldo_17, " + 
                          " sum(saldo_6) as saldo_6, " +
                          " sum(saldo_8) as saldo_8, " +
                          " sum(saldo_9) as saldo_9, " +
                          " sum(saldo_14) as saldo_14, " + 
                          " sum(saldo_25) as saldo_25, " +
                          " sum(saldo_213) as saldo_213, " +
                          " sum(saldo_15) as saldo_15, " + 
                          " sum(saldo_2) as saldo_2, " +
                          " sum(saldo_7) as saldo_7, " + 
                          " sum(saldo_22) as saldo_22, " +
                          " sum(saldo_233) as saldo_233 " +
                          " from t_ved " +
                          " group by 1,3,4 ");
                    ExecSQL(sql.ToString());

                    ExecSQL(" delete from t_ved ");
                    ExecSQL(" drop index ved_index ");
                }
            }

            reader.Close();
            #endregion

            ExecSQL(" create index vedomost_index on t_vedomost(nzp_kvar) ");
            ExecSQL(DBManager.sUpdStat + " t_vedomost ");
            ExecSQL(" create index adr_index on t_adr(nzp_kvar) ");
            ExecSQL(DBManager.sUpdStat + " t_adr ");

            sql.Remove(0, sql.Length);
            sql.Append(" SELECT rajon as raion, geu, ulica, idom, ndom, nkor, ikvar, nkvar, fio, " + 
                    " sum_insaldo, " +  
                    " name_supp as supp, " +  
                    " v.is_actual, " +
                    " saldo_17, " +
                    " saldo_6, " +
                    " saldo_8, " +
                    " saldo_9, " +
                    " saldo_14, " +
                    " saldo_25, " +
                    " saldo_213, " +
                    " saldo_15, " +
                    " saldo_2, " +
                    " saldo_7, " +
                    " saldo_22, " +
                    " saldo_233 " +
                    " FROM t_vedomost v, t_adr a, " +
                      ReportParams.Pref + DBManager.sKernelAliasRest + "supplier s " +
                    " WHERE v.nzp_kvar = a.nzp_kvar " +
                    " AND v.nzp_supp = s.nzp_supp " + 
                    " ORDER BY 1,2,3,4,5,6,7,8,9 ");
            DataTable dt = ExecSQLToTable(sql.ToString());
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }        
        
        /// <summary>
        /// Получить условия органичения по УК
        /// </summary>
        /// <returns></returns>
        private string GetWhereAdr(string tablePrefix)
        {
            string result = Area != 0 ? " AND " + tablePrefix + "nzp_area in (" + Area + ")" : ReportParams.GetRolesCondition(Constants.role_sql_area);

            if (!String.IsNullOrEmpty(result))
            {
                AreaHeader = String.Empty;
                var sql = " SELECT area from " +
                            ReportParams.Pref + DBManager.sDataAliasRest + "s_area " + tablePrefix.TrimEnd('.') +
                          " WHERE nzp_area > 0 " + result;
                var area = ExecSQLToTable(sql);
                AreaHeader = "по " + area.Rows[0][0].ToString().Trim();
            }
            return result;
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp()
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
            whereSupp = !String.IsNullOrEmpty(whereSupp) ? " AND nzp_supp in (" + whereSupp + ")" : String.Empty;
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
            string sql = " CREATE TEMP TABLE t_vedomost ( " +
                                " nzp_kvar integer, " +
                                " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                                " nzp_supp integer, " +
                                " is_actual character(10), " +
                                " saldo_17 " + DBManager.sDecimalType + "(14,2), " +
                                " saldo_6 " + DBManager.sDecimalType + "(14,2), " +
                                " saldo_8 " + DBManager.sDecimalType + "(14,2), " +
                                " saldo_9 " + DBManager.sDecimalType + "(14,2), " +
                                " saldo_14 " + DBManager.sDecimalType + "(14,2), " +
                                " saldo_25 " + DBManager.sDecimalType + "(14,2), " +
                                " saldo_213 " + DBManager.sDecimalType + "(14,2), " +
                                " saldo_15 " + DBManager.sDecimalType + "(14,2), " +
                                " saldo_2 " + DBManager.sDecimalType + "(14,2), " +
                                " saldo_7 " + DBManager.sDecimalType + "(14,2), " +
                                " saldo_22 " + DBManager.sDecimalType + "(14,2), " +
                                " saldo_233 " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql); 
            
            sql = " CREATE TEMP TABLE t_ved ( " +
                     " nzp_kvar integer, " +
                     " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                     " nzp_supp integer, " +
                     " is_actual character(10), " +
                     " saldo_17 " + DBManager.sDecimalType + "(14,2), " +
                     " saldo_6 " + DBManager.sDecimalType + "(14,2), " +
                     " saldo_8 " + DBManager.sDecimalType + "(14,2), " +
                     " saldo_9 " + DBManager.sDecimalType + "(14,2), " +
                     " saldo_14 " + DBManager.sDecimalType + "(14,2), " +
                     " saldo_25 " + DBManager.sDecimalType + "(14,2), " +
                     " saldo_213 " + DBManager.sDecimalType + "(14,2), " +
                     " saldo_15 " + DBManager.sDecimalType + "(14,2), " +
                     " saldo_2 " + DBManager.sDecimalType + "(14,2), " +
                     " saldo_7 " + DBManager.sDecimalType + "(14,2), " +
                     " saldo_22 " + DBManager.sDecimalType + "(14,2), " +
                     " saldo_233 " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " create temp table t_adr (nzp_kvar integer, rajon character(30), geu character(60)," +
                  " ulica character(40), idom integer, ndom character(15), nkor character(15), ikvar integer, nkvar character(10), fio character(40))" +
                  DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_vedomost ");
            ExecSQL(" DROP TABLE t_adr ");
            ExecSQL(" DROP TABLE t_ved ");
        }
    }
}
