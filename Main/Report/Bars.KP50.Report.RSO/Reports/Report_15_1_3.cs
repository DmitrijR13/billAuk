

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Bars.KP50.Report.RSO.Properties;

namespace Bars.KP50.Report.RSO.Reports
{
    /// <summary>Сводный отчет по начислениям для Тулы</summary>
    public class Report1513 : BaseSqlReport
    {
        public override string Name
        {
            get { return "15.1.3 Сводный отчет по начислениям"; }
        }

        public override string Description
        {
            get { return "Сводный отчет по начислениям для Тулы"; }
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
            get { return Resources.Report_15_1_3; }
        }
        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AreaHeader { get; set; }

        /// <summary> с расчетного дня </summary>
        protected DateTime DatS { get; set; }

        /// <summary> по расчетный год </summary>
        protected DateTime DatPo { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected int ReportTitle { get; set; }

        /// <summary>Вид начислено</summary>
        protected int TypeNacl { get; set; }

        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }
        
        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            DateTime datS = curCalcMonthYear != null
                ? new DateTime(Convert.ToInt32(curCalcMonthYear.Rows[0]["yearr"]),
                    Convert.ToInt32(curCalcMonthYear.Rows[0]["month_"]), 1)
                : DateTime.Now;
            DateTime datPo = curCalcMonthYear != null
                ? datS.AddMonths(1).AddDays(-1)
                : DateTime.Now;
            return new List<UserParam>
            {
                new PeriodParameter(datS, datPo),
                new ComboBoxParameter
                {
                    Code = "ReportTitle",
                    Name = "Заголовок отчета",
                    Value = "1",
                    StoreData = new List<object>
                    {
                        new { Id = "1", Name = "Сводный отчет по начислениям за жилищно-коммунальные услуги" },
                        new { Id = "2", Name = "Сводный отчет по начислениям за наем жилого помещения" },
                        new { Id = "3", Name = "Сводный отчет по начислениям за жилищные услуги" },
                        new { Id = "4", Name = "Сводный отчет по начислениям за коммунальные услуги" }
                    }
                },
                new ComboBoxParameter
                {
                    Code = "TypeNacl",
                    Name = "Вид начислено",
                    Value = "3",
                    StoreData = new List<object>
                    {
                        new { Id = "1", Name = "Начислено к оплате" },
                        new { Id = "2", Name = "Расчитано по тарифу" },
                        new { Id = "3", Name = "Начислено за месяц (без учета долга) с уч. перерасчета" }
                    }
                },
                new SupplierAndBankParameter(),
                new ServiceParameter(),
                new AreaParameter()
            };
        }

        public override DataSet GetData()
        {
            /* Валидация параметров */

            MyDataReader reader;

            var sql  = " SELECT * "+
                   " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point "+
                   " WHERE nzp_wp>1 " + GetwhereWp();

            ExecRead(out reader, sql);

            while (reader.Read())
            {
                var pref = reader["bd_kernel"].ToString().ToLower().Trim();

                for (int i = DatS.Year*12 + DatS.Month; i < DatPo.Year*12 + DatPo.Month + 1; i++)
                {
                    var year = i/12;
                    var month = i%12;
                    if (month == 0)
                    {
                        year--;
                        month = 12;
                    }

                    var chargeXx = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                   "charge_" + month.ToString("00");

                    string vidNach;
                    switch (TypeNacl)
                    {
                        case 1:
                            vidNach = "a.sum_charge";
                            break;
                        case 2:
                            vidNach = "a.rsum_tarif";
                            break;
                        default:
                            vidNach = "a.sum_tarif + a.real_charge + a.reval";
                            break;
                    }


                    sql = " INSERT INTO t_nach (nzp_area,nzp_serv, nzp_supp, sum_nach, sum_reval )" +
                          " SELECT k.nzp_area, a.nzp_serv, a.nzp_supp, sum(" + vidNach + "), sum(a.reval) " +
                          " FROM " + chargeXx + " a, " + pref + DBManager.sDataAliasRest + "kvar k," +
                          pref + DBManager.sDataAliasRest + "dom d, " +
                          pref + DBManager.sDataAliasRest + "s_ulica u " +
                          " WHERE a.nzp_kvar=k.nzp_kvar " +
                          "        AND dat_charge is null " +
                          "        AND a.nzp_serv>1 " +
                          "        AND k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul " +
                          GetWhereAdr("k.") + GetWhereSupp("a.") + GetWhereServ("a.") + 
                          " GROUP BY  1,2,3            ";
                    if (TempTableInWebCashe(chargeXx)) ExecSQL(sql);
                }
            }

            reader.Close();


            sql = " SELECT sa.area, s.service, su.name_supp, " +
                  "        sum(t.sum_nach) as sum_charge, sum(t.sum_reval) as sum_reval " +
                  " FROM t_nach t, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "services s, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_area sa " +
                  " WHERE t.nzp_supp = su.nzp_supp " +
                  "        AND t.nzp_serv = s.nzp_serv " +
                  "        AND t.nzp_area = sa.nzp_area " +
                  " GROUP BY 1,2,3 " +
                  " ORDER BY 1,2,3 ";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
        

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        private string GetWhereServ(string tablePrefix)
        {
            var result = String.Empty;
            if (Services != null)
            {
                result = Services.Aggregate(result, (current, nzpServ) => current + (nzpServ + ","));
            }
            else
            {
                result = ReportParams.GetRolesCondition(Constants.role_sql_serv);
            }
            result = result.TrimEnd(',');
            result = !String.IsNullOrEmpty(result)
                ? " AND " + tablePrefix + "nzp_serv in (" + result + ")"
                : String.Empty;

            //Дополнительно фильтруем услугив в зависисости от заголовка
            switch (ReportTitle)
            {
                case 1:
                    break;
                case 2:
                    result = " and nzp_serv=15 ";
                    break;
                case 3:
                    result += " and nzp_serv not in (select nzp_serv from " + ReportParams.Pref + DBManager.sKernelAliasRest +
                                 "s_counts )";
                    break;
                case 4:
                    result += " and (nzp_serv in (select nzp_serv from " +
                                 ReportParams.Pref + DBManager.sKernelAliasRest + "s_counts) " +
                                 " or nzp_serv in (select nzp_serv from " + ReportParams.Pref + DBManager.sKernelAliasRest +
                                 "serv_odn)) ";
                    break;
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
                      ReportParams.Pref + DBManager.sDataAliasRest + "s_area " + tablePrefix.TrimEnd('.')+
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

        private string GetWhereSupp(string tablePrefix)
        {
            string result = String.Empty;
            if (Suppliers != null)
            {
                result = Suppliers.Aggregate(result, (current, nzpSupp) => current + (nzpSupp + ","));
            }
            else
            {
                result = ReportParams.GetRolesCondition(Constants.role_sql_supp);
            }
            result = result.TrimEnd(',');


            if (!String.IsNullOrEmpty(result))
            {
                result = " AND " + tablePrefix + "nzp_supp in (" + result + ")";

                //Поставщики
                SupplierHeader = String.Empty;
                var sql = " SELECT name_supp from " +
                          ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " + tablePrefix.TrimEnd('.') +
                          " WHERE " + tablePrefix + "nzp_supp > 0 " + result;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += dr["name_supp"].ToString().Trim() + ",";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',');
            }
            return result;
        }

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

        protected override void PrepareReport(FastReport.Report report)
        {
            string period;

            if (DatS==DatPo)
            {
                period = DatS.ToShortDateString() + " г.";
            }
            else
            {
                period = "период с " + DatS.ToShortDateString() + " г. по " + DatPo.ToShortDateString() + " г.";
            }
            report.SetParameterValue("period", period);


            report.SetParameterValue("rajon", "");
            report.SetParameterValue("ercName", GetErcName());
            switch (ReportTitle)
            {
                case 1:
                    report.SetParameterValue("reportHeader", "Сводный отчет по начислениям за жилищно-коммунальные услуги");
                    break;
                case 2:
                    report.SetParameterValue("reportHeader", "Сводный отчет по начислениям за наем жилого помещения");
                    break;
                case 3:
                    report.SetParameterValue("reportHeader", "Сводный отчет по начислениям за жилищные услуги");
                    break;
                case 4:
                    report.SetParameterValue("reportHeader", "Сводный отчет по начислениям за коммунальные услуги");
                    break;

            }
            
            switch (TypeNacl)
            {
                case 1:
                    report.SetParameterValue("sumHeader", "Начислено к оплате");
                    break;
                case 2:
                    report.SetParameterValue("sumHeader", "Расчитано по тарифу");
                    break;
                case 3:
                    report.SetParameterValue("sumHeader", "Начислено за месяц (без учета долга) с уч. перерасчета");
                    break;
            }
            report.SetParameterValue("principal", AreaHeader +" "+ SupplierHeader);

            /*
            var reportAreaList = UserParams["reportAreaList"].GetValue<List<UserParam>>();

            // Определение принциала
            var principal = reportAreaList.Aggregate(string.Empty, (current, kp) => current + (string.IsNullOrEmpty(current) ? string.Empty : ", ") + kp.Value);
            if (string.IsNullOrEmpty(principal))
            {
                var reportSuppList = UserParams["reportSuppList"].GetValue<List<UserParam>>();
                principal = reportSuppList.Aggregate(principal, (current, kp) => current + (string.IsNullOrEmpty(current) ? string.Empty : ", ") + kp.Value);
            }
            
            report.SetParameterValue("principal", principal);*/
        }

        protected override void PrepareParams()
        {
            DateTime begin;
            DateTime end;
            string period = UserParamValues["Period"].GetValue<string>();
            PeriodParameter.GetValues(period, out begin, out end);
            DatS = begin;
            DatPo = end;

            ReportTitle = UserParamValues["ReportTitle"].GetValue<int>();
            TypeNacl = UserParamValues["TypeNacl"].GetValue<int>();

            Services = UserParamValues["Services"].GetValue<List<int>>();
            Areas = UserParamValues["Areas"].GetValue<List<int>>();

            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
        }

        /// <summary>
        /// Получает условия ограничения по району
        /// </summary>
        /// <returns></returns>
        private string GetErcName()
        {
            string result = "Не определено наименование Расчетного центра";
            string sql = " select val_prm " +
                         " from " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_10 " +
                         " where nzp_prm=80 and is_actual=1 and dat_s<=" + DBManager.sCurDate +
                         " and dat_po>=" + DBManager.sCurDate;
            DataTable erc = ExecSQLToTable(sql);
            if (erc != null)
                if (erc.Rows.Count > 0)
                {
                    result = erc.Rows[0]["val_prm"].ToString().Trim();
                }


            return result;
        }

        protected override void CreateTempTable()
        {
            var sql = new StringBuilder();
            sql.Remove(0, sql.Length);
            sql.Append(" create " + DBManager.sCrtTempTable + " table t_nach (     ");
            sql.Append(" nzp_area integer default 0,");
            sql.Append(" nzp_serv integer default 0,");
            sql.Append(" nzp_supp integer default 0,");
            sql.Append(" sum_nach " + DBManager.sDecimalType + "(14,2) default 0.00, ");
            sql.Append(" sum_reval " + DBManager.sDecimalType + "(14,2) default 0.00 ) " + DBManager.sUnlogTempTable);

            ExecSQL(sql.ToString());
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_nach ", true);
        }
    }
}