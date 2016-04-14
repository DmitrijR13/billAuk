using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.RSO.Properties;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Bars.KP50.Report.RSO.Reports
{
    public class Report1514 : BaseSqlReport
    {
        public override string Name
        {
            get { return "15.1.4 Справка об образовавшейся задолженности "; }
        }

        public override string Description
        {
            get { return "Справка об образовавшейся задолженности ЖКУ за данный период, по заданному адресу для Тулы"; }
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
            get { return Resources.Report_15_1_4; }
        }


        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }


        /// <summary>Улица</summary>
        protected string Ulica { get; set; }

        /// <summary>Код улицы</summary>
        protected int NzpUlica { get; set; }

        /// <summary>Номер дома</summary>
        protected string Ndom { get; set; }

        /// <summary>Корпус</summary>
        protected string Nkor { get; set; }

        /// <summary>Квартира</summary>
        protected string Nkvar { get; set; }

        /// <summary>Номер комнаты</summary>
        protected string NkvarN { get; set; }
        /// <summary> с расчетного дня </summary>
        protected DateTime DatS { get; set; }
     
        /// <summary> по расчетный год </summary>
        protected DateTime DatPo { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Районы</summary>
        protected string AddressHeader { get; set; }

        /// <summary> Наименование РЦ </summary>
        private string ERC { get; set; }

        /// <summary>Адрес</summary>
        protected AddressParameterValue Address { get; set; }


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
                new SupplierAndBankParameter(),
                new AreaParameter(),
                new AddressParameter(),
                new StringParameter{Code ="Nkvar", Name = "квартира"},
                new StringParameter{Code ="Nkvar_n", Name = "комната", Value = "-"}
            };
        }


        public override DataSet GetData()
        {

            #region Выборка по локальным банкам


            MyDataReader reader;

            string sql = " SELECT * " +
                         " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " WHERE nzp_wp>1 " + GetwhereWp();
            string whereAddress = GetWhereAdr();

            ExecRead(out reader, sql);
                while (reader.Read())
                {
                    var pref = reader["bd_kernel"].ToString().ToLower().Trim();
                 

                    for (int i = DatS.Year * 12 +DatS.Month; i < DatPo.Year * 12 +DatPo.Month + 1; i++)
                    {
                        var year = i / 12;
                        var month = i % 12;
                        if (month == 0)
                        {
                            year--;
                            month = 12;
                        }
                        string chargeXx = pref + "_charge_" + (year-2000).ToString("00") +
                     DBManager.tableDelimiter + "charge_" + month.ToString("00");


                        sql = " insert into t_svod(month_, year_, nzp_serv, nzp_supp, sum_insaldo, " +
                              " sum_tarif, reval, real_pere, sum_money, sum_outsaldo)  " +
                              " select " + month + "," + year +
                              ",a.nzp_serv, a.nzp_supp, sum(sum_insaldo) as sum_insaldo, " +
                              "       sum(sum_tarif) as sum_tarif, " +
                              "       sum(reval) as reval, " +
                              "       sum(real_pere) as real_pere, " +
                              "       sum(sum_money) as sum_money, " +
                              "       sum(sum_outsaldo) as sum_outsaldo " +
                              "       from " + chargeXx + " a, " +
                              pref + DBManager.sDataAliasRest + "kvar k, " +
                              pref + DBManager.sDataAliasRest + "dom d, " +
                              pref + DBManager.sDataAliasRest + "s_ulica u " +
                              " where nzp_serv>1 and dat_charge is null and a.nzp_kvar=k.nzp_kvar " +
                              " and d.nzp_dom=k.nzp_dom and d.nzp_ul = u.nzp_ul " +
                              whereAddress +
                              " GROUP BY  1,2,3,4           ";
                        ExecSQL( sql);

                    }

                }
            reader.Close();

            #endregion

            sql = " SELECT val_prm" +
                  " FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_10 " +
                  " WHERE is_actual = 1" +
                    " AND nzp_prm = 80 " +
                    " AND dat_s <= '" + DateTime.Now.ToShortDateString() + "' " +
                    " AND dat_po >= '" + DateTime.Now.ToShortDateString() + "' ";
            DataTable ercTable = ExecSQLToTable(sql);
            if (ercTable.Rows.Count != 0)
                ERC = ercTable.Rows[0]["val_prm"].ToString().TrimEnd();

            #region Выборка на экран

            sql = " SELECT MDY(month_,01,year_) as dat_month, service, name_supp,  sum(sum_insaldo) as sum_insaldo, " +
                  "        sum(sum_tarif) as sum_tarif, " +
                  "        sum(reval) as reval, " +
                  "        sum(real_pere) as real_pere, " +
                  "        sum(sum_money) as sum_money, " +
                  "        sum(reval+sum_tarif) as sum_nach, " +
                  "        sum(real_pere+sum_money) as sum_money_all, " +
                  "        sum(sum_money) as sum_money, " +
                  "        sum(sum_outsaldo) as sum_outsaldo" +
                  " FROM t_svod a, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "services s," +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su" +
                  " WHERE a.nzp_supp=su.nzp_supp " +
                  "        AND a.nzp_serv=s.nzp_serv " +
                  " GROUP BY 1,2,3 ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            #endregion
        


            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
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

        private string GetWhereAdr()
        {
            var result = String.Empty;
            string rajon = String.Empty,
                street = String.Empty,
                house = String.Empty;
            string prefData = ReportParams.Pref + DBManager.sDataAliasRest;
            if (Areas != null)
            {
                result = Areas.Aggregate(result, (current, nzpArea) => current + (nzpArea + ","));
            }
            else
            {
                result = ReportParams.GetRolesCondition(Constants.role_sql_area);
            }

            if (Address.Raions != null)
            {
                rajon = Address.Raions.Aggregate(rajon, (current, nzpRajon) => current + (nzpRajon + ","));
                rajon = rajon.TrimEnd(',');
            }
            if (Address.Streets != null)
            {
                street = Address.Streets.Aggregate(street, (current, nzpStreet) => current + (nzpStreet + ","));
                street = street.TrimEnd(',');
            }
            if (Address.Houses != null)
            {
                house = Address.Houses.Aggregate(house, (current, nzpHouse) => current + (nzpHouse + ","));
                house = house.TrimEnd(',');
            }

            result = result.TrimEnd(',');
            result = !String.IsNullOrEmpty(result) ? " AND k.nzp_area in (" + result + ")" : String.Empty;
            if (!string.IsNullOrEmpty(house))
            {
                result += !String.IsNullOrEmpty(Nkvar.Trim())
                    ? " AND nkvar = " + STCLINE.KP50.Global.Utils.EStrNull(Nkvar) + ""
                    : String.Empty;
                result += NkvarN.Trim() != "-"
                    ? " AND nkvar_n = " + STCLINE.KP50.Global.Utils.EStrNull(NkvarN) + ""
                    : String.Empty;
            }
            result += !String.IsNullOrEmpty(rajon) ? " AND u.nzp_raj IN ( " + rajon + ") " : string.Empty;
            result += !String.IsNullOrEmpty(street) ? " AND u.nzp_ul IN ( " + street + ") " : string.Empty;
            result += !String.IsNullOrEmpty(house) ? " AND d.nzp_dom IN ( " + house + ") " : string.Empty;
            if (!String.IsNullOrEmpty(house))
            {
                var sql = " SELECT TRIM(town) AS  town, TRIM(rajon) AS rajon, TRIM(ulica) AS ulica, TRIM(ndom) AS ndom, TRIM(nkor) AS nkor " +
                          " FROM " + prefData + "dom d INNER JOIN " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                                                     " INNER JOIN " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                                                     " INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town" +
                          " WHERE nzp_dom IN (" + house + ") ";
                DataTable addressTable = ExecSQLToTable(sql);
                foreach (DataRow row in addressTable.Rows)
                {
                    AddressHeader = "," + AddressHeader;
                    AddressHeader += !string.IsNullOrEmpty(row["town"].ToString().Trim())
                        ? "," + row["town"].ToString().Trim() + "/"
                        : ",-/";
                    AddressHeader += !string.IsNullOrEmpty(row["rajon"].ToString().Trim())
                        ? row["rajon"].ToString().Trim() + ","
                        : "-,";
                    AddressHeader += !string.IsNullOrEmpty(row["ulica"].ToString().Trim())
                        ? "ул. " + row["ulica"].ToString().Trim() + ","
                        : string.Empty;
                    AddressHeader += !string.IsNullOrEmpty(row["ndom"].ToString().Trim())
                        ? row["ndom"].ToString().Trim() != "-"
                            ? "д. " + row["ndom"].ToString().Trim() + ","
                            : string.Empty
                        : string.Empty;
                    AddressHeader += !string.IsNullOrEmpty(row["nkor"].ToString().Trim())
                        ? row["nkor"].ToString().Trim() != "-"
                            ? "кор. " + row["nkor"].ToString().Trim() + ","
                            : string.Empty
                        : string.Empty;
                    AddressHeader += !String.IsNullOrEmpty(Nkvar)
                        ? "кв. " + Nkvar + ","
                        : string.Empty;
                    AddressHeader += !string.IsNullOrEmpty(NkvarN)
                        ? NkvarN != "-"
                            ? "ком. " + NkvarN
                            : string.Empty
                        : string.Empty;
                    AddressHeader = AddressHeader.TrimEnd(',');

                }
                AddressHeader = AddressHeader.TrimEnd(',');
            }
            else if (!String.IsNullOrEmpty(street))
            {
                var sql = " SELECT TRIM(town) AS  town, TRIM(rajon) AS rajon, TRIM(ulica) AS ulica " +
                          " FROM " + prefData + "s_ulica u INNER JOIN " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                                                         " INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town " +
                          " WHERE nzp_ul IN (" + street + ") ";
                DataTable addressTable = ExecSQLToTable(sql);
                foreach (DataRow row in addressTable.Rows)
                {
                    AddressHeader += !string.IsNullOrEmpty(row["town"].ToString().Trim())
                        ? "," + row["town"].ToString().Trim() + "/"
                        : ",-/";
                    AddressHeader += !string.IsNullOrEmpty(row["rajon"].ToString().Trim())
                        ? row["rajon"].ToString().Trim() + ","
                        : "-,";
                    AddressHeader += !string.IsNullOrEmpty(row["ulica"].ToString().Trim())
                        ? "ул. " + row["ulica"].ToString().Trim() + ","
                        : string.Empty;
                    AddressHeader = AddressHeader.TrimEnd(',');
                }
                AddressHeader = AddressHeader.TrimEnd(',');
            }
            else if (!String.IsNullOrEmpty(rajon))
            {
                var sql = " SELECT TRIM(town) AS  town, TRIM(rajon) AS rajon " +
                          " FROM " + prefData + "s_rajon r INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town " +
                          " WHERE nzp_raj IN (" + rajon + ") ";
                DataTable addressTable = ExecSQLToTable(sql);
                foreach (DataRow row in addressTable.Rows)
                {
                    AddressHeader += !string.IsNullOrEmpty(row["town"].ToString().Trim())
                        ? "," + row["town"].ToString().Trim() + "/"
                        : ",-/";
                    AddressHeader += !string.IsNullOrEmpty(row["rajon"].ToString().Trim())
                        ? row["rajon"].ToString().Trim() + ","
                        : "-,";
                    AddressHeader = AddressHeader.TrimEnd(',');
                }
                AddressHeader = AddressHeader.TrimEnd(',');
            }
            if (!string.IsNullOrEmpty(AddressHeader)) 
                AddressHeader = AddressHeader.TrimStart(',');


            return result;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("dat_s", DatS.ToShortDateString());
            report.SetParameterValue("dat_po", DatPo.ToShortDateString());
            AddressHeader = !string.IsNullOrEmpty(AddressHeader) ? "Адрес: " + AddressHeader : string.Empty;
            report.SetParameterValue("address", AddressHeader);
            report.SetParameterValue("erc", ERC ?? "");
        }

        protected override void PrepareParams()
        {

            Nkvar = UserParamValues["Nkvar"].GetValue<string>() ?? String.Empty;
            NkvarN = UserParamValues["Nkvar_n"].GetValue<string>() ?? String.Empty;
            Address = UserParamValues["Address"].GetValue<AddressParameterValue>();
            var period = UserParamValues["Period"].GetValue<string>();
            DateTime d1;
            DateTime d2;
            PeriodParameter.GetValues(period, out d1, out d2);
            DatS = d1;
            DatPo = d2;
            Areas = UserParamValues["Areas"].GetValue<List<int>>();

            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
        
        }

        protected override void CreateTempTable()
        {
            const string sql = " create temp table t_svod (     " +
                               " month_ integer, " +
                               " year_ integer, " +
                               " nzp_serv integer, " +
                               " nzp_supp integer, " +
                               " sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
                               " sum_tarif " + DBManager.sDecimalType + "(14,2), " +
                               " reval " + DBManager.sDecimalType + "(14,2), " +
                               " real_pere " + DBManager.sDecimalType + "(14,2), " +
                               " sum_money " + DBManager.sDecimalType + "(14,2), " +
                               " sum_outsaldo " + DBManager.sDecimalType + "(14,2))  " +
                               DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_svod ", true);
        }

    }
}
