using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Astra.Properties;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Astra.Reports
{
    public class Report3131 : BaseSqlReport
    {
        public override string Name
        {
            get { return "31.3.1 Сводный отчет по принятым и перечисленным средствам"; }
        }

        public override string Description
        {
            get { return "Сводный отчет по принятым и перечисленным средствам для Тулы"; }
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
            get { return Resources.Report_31_3_1; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }


        /// <summary> с расчетного дня </summary>
        protected DateTime DatS { get; set; }

        /// <summary> по расчетный год </summary>
        protected DateTime DatPo { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AreaHeader { get; set; }

        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected int ReportTitle { get; set; }

        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }

        private string _ercName;

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
                // new PeriodParameter(ReportParams.CurDateOper, ReportParams.CurDateOper),
                new ComboBoxParameter
                {
                    Code = "ReportTitle",
                    Name = "Заголовок отчета",
                    Value = "1",
                    StoreData = new List<object>
                    {
                        new {Id = "1", Name = "за жилищно-коммунальные услуги"},
                        new {Id = "2", Name = "за наем жилого помещения"},
                        new {Id = "3", Name = "за жилищные услуги"},
                        new {Id = "4", Name = "за коммунальные услуги"}
                    }
                },
                new SupplierAndBankParameter(),
                new ServiceParameter(),
                new AreaParameter()
            };
        }

        public override DataSet GetData()
        {
            _ercName = GetErcName();
            
            string sql;

            for (int i = DatS.Year * 12 + DatS.Month; i < DatPo.Year * 12 + DatPo.Month + 1; i++)
            {
                int year = i / 12;
                int month = i % 12;
                if (month == 0)
                {
                    year--;
                    month = 12;
                }
                var distribXx = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") +
                        DBManager.tableDelimiter + "fn_distrib_dom_" + month.ToString("00");

                //Проверка на существование базы

                if (TempTableInWebCashe(distribXx))
                {
                    sql = " INSERT INTO t_distrib (nzp_raj,nzp_serv, nzp_supp, sum_rasp, " +
                          " sum_ud, sum_charge, sum_send, sum_in, sum_out )" +
                          " SELECT su.nzp_raj,a.nzp_serv, sp.nzp_supp, sum(a.sum_rasp), " +
                          " sum(a.sum_ud), sum(a.sum_charge),  " +
                          " sum(a.sum_send), " +
                          " sum(case when dat_oper='" + DatS.ToShortDateString() + "' then a.sum_in else 0 end), " +
                          " sum(case when dat_oper='" + DatPo.ToShortDateString() + "' then a.sum_out else 0 end) " +
                          " FROM " + distribXx + " a,  " +
                          ReportParams.Pref + DBManager.sKernelAliasRest + "s_payer sp, " +
                          ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                          ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica su " +

                          " where a.nzp_payer=sp.nzp_payer and a.nzp_dom=d.nzp_dom and d.nzp_ul=su.nzp_ul" +
                          "      and dat_oper<='" + DatPo.ToShortDateString() + "'" +
                          "      and dat_oper>='" + DatS.ToShortDateString() + "'" +
                          GetWhereSupp() + GetWhereAdr() + GetWhereServ() + GetwhereWp() +
                          " GROUP BY  1,2,3          ";
                    ExecSQL(sql, true);
                }

            }



            sql = " SELECT (CASE WHEN TRIM(sr.rajon)='-' THEN TRIM(st.town) ELSE TRIM(sr.rajon) END) as rajon, s.service, su.name_supp, " +
                  "        sum(t.sum_charge) as sum_charge, sum(sum_rasp) as sum_rasp, " +
                  "        sum(t.sum_ud) as sum_ud, sum(sum_send) as sum_send, " +
                  "        sum(t.sum_in) as sum_in, sum(sum_out) as sum_out " +
                  " FROM t_distrib t, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "services s, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon sr, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_town st " +
                  " WHERE t.nzp_supp = su.nzp_supp " +
                  "        AND t.nzp_serv = s.nzp_serv " +
                  "        AND t.nzp_raj = sr.nzp_raj " +
                  "        AND sr.nzp_town = st.nzp_town " +
                  " GROUP BY 1,2,3 ORDER BY 1,2,3 ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }


        /// <summary>
        /// Получате условия органичения по услугам
        /// </summary>
        /// <returns></returns>
        private string GetWhereServ()
        {
            string whereServ = String.Empty;
            if (Services != null)
            {
                whereServ = Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ","));
            }
            else
            {
                whereServ = ReportParams.GetRolesCondition(Constants.role_sql_serv);
            }
            whereServ = whereServ.TrimEnd(',');
            whereServ = !String.IsNullOrEmpty(whereServ) ? " AND nzp_serv in (" + whereServ + ")" : String.Empty;

            //Дополнительно фильтруем услугив в зависисости от заголовка
            switch (ReportTitle)
            {
                case 1:
                    break;
                case 2:
                    whereServ = " and nzp_serv=15 ";
                    break;
                case 3:
                    whereServ += " and nzp_serv not in (select nzp_serv from " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_counts )";
                    break;
                case 4:
                    whereServ += " and (nzp_serv in (select nzp_serv from " +
                        ReportParams.Pref + DBManager.sKernelAliasRest + "s_counts) " +
                        " or nzp_serv in (select nzp_serv from " + ReportParams.Pref + DBManager.sKernelAliasRest + "serv_odn)) ";
                    break;

            }
            return whereServ;
        }

        /// <summary>
        /// Получает условия ограничения по территории
        /// </summary>
        /// <returns></returns>
        private string GetWhereAdr()
        {
            string whereAdr = String.Empty;
            if (Areas != null)
            {
                whereAdr = Areas.Aggregate(whereAdr, (current, nzpArea) => current + (nzpArea + ","));
            }
            else
            {
                whereAdr = ReportParams.GetRolesCondition(Constants.role_sql_area);
            }

            whereAdr = whereAdr.TrimEnd(',');
            if (!String.IsNullOrEmpty(whereAdr))
            {
                whereAdr = " AND a.nzp_area in (" + whereAdr + ")";
                AreaHeader = String.Empty;

                string sql = " SELECT area from " +
                             ReportParams.Pref + DBManager.sDataAliasRest + "s_area a" +
                             " WHERE nzp_area > 0 " + whereAdr;
                DataTable area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    AreaHeader += dr["area"].ToString().Trim() + ",";
                }
                AreaHeader = AreaHeader.TrimEnd(',');
            }
            return whereAdr;
        }

        /// <summary>
        /// Получает условия ограничения по поставщику
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


            if (!String.IsNullOrEmpty(whereSupp))
            {
                whereSupp = " AND sp.nzp_supp in (" + whereSupp + ")";

                //Поставщики
                SupplierHeader = String.Empty;
                string sql = " SELECT sp.name_supp from " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "supplier sp " +
                             " WHERE sp.nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += dr["name_supp"].ToString().Trim() + ",";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',');
            }
            return whereSupp;
        }

        private string GetwhereWp()
        {
            IDataReader reader;
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

            string sql = " SELECT bd_kernel as pref " +
             " FROM " + DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "s_point " +
             " WHERE nzp_wp>1 " + whereWp;
            ExecRead(out reader, sql);
            whereWp = "";
            while (reader.Read())
            {
                whereWp = whereWp + " '" + reader["pref"].ToStr().Trim() + "', ";
            }
            whereWp = whereWp.TrimEnd(',',' ');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND d.pref in (" + whereWp + ")" : String.Empty;
            return whereWp;
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

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("dats", DatS.ToShortDateString());
            report.SetParameterValue("datpo", DatPo.ToShortDateString());
            report.SetParameterValue("ercName", _ercName);//"ОАО «Областной Единый Информационно-Расчетный Центр»"
            report.SetParameterValue("principal", AreaHeader + " " +SupplierHeader);

            switch (ReportTitle)
            {
                case 1:
                    report.SetParameterValue("reportHeader", "за жилищно-коммунальные услуги");
                    break;
                case 2:
                    report.SetParameterValue("reportHeader", "наем жилого помещения");
                    break;
                case 3:
                    report.SetParameterValue("reportHeader", "жилищные услуги");
                    break;
                case 4:
                    report.SetParameterValue("reportHeader", "коммунальные услуги");
                    break;
            }
   
        }

        protected override void PrepareParams()
        {
            ReportTitle = UserParamValues["ReportTitle"].GetValue<int>();

            Services = UserParamValues["Services"].GetValue<List<int>>();
            Areas = UserParamValues["Areas"].GetValue<List<int>>();
            var period = UserParamValues["Period"].GetValue<string>();
            DateTime d1;
            DateTime d2;
            PeriodParameter.GetValues(period, out d1, out d2);
            DatS = d1;
            DatPo = d2;

            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
        }

        protected override void CreateTempTable()
        {
            const string sql = " create temp table t_distrib (     " +
                               " nzp_raj integer default 0," +
                               " nzp_serv integer default 0," +
                               " nzp_supp integer default 0," +
                               " sum_in " + DBManager.sDecimalType + "(14,2) default 0.00, " + //Входящий остаток
                               " sum_send " + DBManager.sDecimalType + "(14,2) default 0.00, " + //Перечислено
                               " sum_out " + DBManager.sDecimalType + "(14,2) default 0.00, " + //Исходящий остаток
                               " sum_rasp " + DBManager.sDecimalType + "(14,2) default 0.00, " + //Рапределено
                               " sum_ud " + DBManager.sDecimalType + "(14,2) default 0.00, " + //Удержано
                               " sum_charge " + DBManager.sDecimalType + "(14,2) default 0.00 " + //К перечислению
                               " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_distrib ", true);
        }
    }

}
