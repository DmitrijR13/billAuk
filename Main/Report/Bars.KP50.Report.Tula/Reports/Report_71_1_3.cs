using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Tula.Properties;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.EPaspXsd;
using STCLINE.KP50.Global;
using Bars.KP50.Utils;
using Newtonsoft.Json;

namespace Bars.KP50.Report.Tula.Reports
{
    public class Report7113 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.1.3 Справка об образовавшейся задолженности "; }
        }

        public override string Description
        {
            get { return "Справка об образовавшейся задолженности ЖКУ за данный период, по заданному адресу для Тулы"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get { return new List<ReportGroup> { ReportGroup.Reports }; }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_71_1_3; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC}; }}


        /// <summary>Квартира</summary>
        protected string Nkvar { get; set; }

        /// <summary>Номер комнаты</summary>
        protected string NkvarN { get; set; }

        /// <summary> с расчетного дня </summary>
        protected DateTime DatS { get; set; }
     
        /// <summary> по расчетный год </summary>
        protected DateTime DatPo { get; set; }

        ///// <summary>Управляющие компании</summary>
        //protected List<int> Areas { get; set; }

        ///// <summary>Заголовок УК</summary>
        //protected string AreaHeader { get; set; }

        /// <summary>Районы</summary>
        protected string AddressHeader { get; set; }

        /// <summary>Адрес</summary>
        protected AddressParameterValue Address { get; set; }

        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }

        /// <summary>Поставщики</summary>
        private string SupplierHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }


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
                new BankSupplierParameter(),
                new AddressParameter(),
                new StringParameter{Code ="Nkvar", Name = "квартира"},
                new StringParameter{Code ="Nkvar_n", Name = "комната", Value = "-"}
            };
        }


        public override DataSet GetData()
        {

            #region Выборка по локальным банкам
            string whereSupp = GetWhereSupp("a.nzp_supp"); 
            MyDataReader reader;
            string sql;
            string whereAdr = GetWhereAdr();

            string kvar = ReportParams.Pref + DBManager.sDataAliasRest + "kvar ";
                    
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {     
                GetSelectedKvars();  
                kvar = " selected_kvars ";  
            }

            if (!string.IsNullOrEmpty(whereAdr))
            {
                sql = " CREATE TEMP TABLE adr_kvar ( " +
                              " nzp_kvar INTEGER)";
                ExecSQL(sql);
                sql = " INSERT INTO adr_kvar (nzp_kvar)" +
                      " SELECT nzp_kvar " +
                      " FROM " + kvar + " k," +
                      ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                      ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u " +
                      " WHERE d.nzp_dom=k.nzp_dom and d.nzp_ul = u.nzp_ul" + whereAdr;
                ExecSQL(sql);
                kvar = "adr_kvar";
            }

            sql = " SELECT * " +
                         " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
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

                        string chargeXX = pref + "_charge_" + (year - 2000).ToString("00") +
                                          DBManager.tableDelimiter + "charge_" + month.ToString("00");

                        string tablePerekidka = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                        "perekidka";

                        if (string.IsNullOrEmpty(whereAdr) && ReportParams.CurrentReportKind != ReportKind.ListLC && ReportParams.CurrentReportKind != ReportKind.LC)
                            kvar = pref + DBManager.sDataAliasRest + "kvar ";

                        if (TempTableInWebCashe(chargeXX))
                        {

                            sql = " insert into t_svod(pref, month_, year_, nzp_kvar, nzp_serv, nzp_supp, sum_insaldo, rsum_tarif, " +
                                  " sum_tarif, reval,  sum_money, sum_outsaldo)  " +
                                  " select '" + pref + "'," + month + "," + year +
                                  ", a.nzp_kvar,a.nzp_serv, a.nzp_supp, sum(sum_insaldo) , sum(rsum_tarif), " +
                                  "       sum(sum_tarif) as sum_tarif, " +
                                  "       sum(reval) as reval, " +
                                  "       sum(sum_money) as sum_money, " +
                                  "       sum(sum_outsaldo) as sum_outsaldo " +
                                  "       from " + chargeXX + " a, " +
                                  kvar + " k " +
                                  " where nzp_serv>1 and dat_charge is null and a.nzp_kvar=k.nzp_kvar " +
                                  " GROUP BY  1,2,3,4,5,6  ";
                        ExecSQL(sql);

                            sql = " UPDATE t_svod " +
                                  " SET real_insaldo = ( SELECT SUM(sum_rcl)" +
                                  " FROM " + tablePerekidka + " p " +
                                  " WHERE p.type_rcl in ( 100,20) " +
                                  " AND p.nzp_kvar = t_svod.nzp_kvar " +
                                  " AND p.nzp_supp = t_svod.nzp_supp " +
                                  " AND p.nzp_serv = t_svod.nzp_serv " + GetWhereSupp("p.nzp_supp") +
                                  " AND p.month_ = " + month +
                                  ") " +
                                  " WHERE month_ = " + month + "" +
                                  " AND year_= " + year +
                                  " AND pref= '" + pref + "'";
                        ExecSQL(sql);
                            sql = " UPDATE t_svod " +
                                  " SET real_charge = ( SELECT SUM(sum_rcl)" +
                                  " FROM " + tablePerekidka + " p " +
                                  " WHERE p.type_rcl not in ( 100,20) " +
                                  " AND p.nzp_kvar = t_svod.nzp_kvar " +
                                  " AND p.nzp_supp = t_svod.nzp_supp " +
                                  " AND p.nzp_serv = t_svod.nzp_serv " + GetWhereSupp("p.nzp_supp") +
                                  " AND p.month_ = " + month +
                                  ") " +
                                  " WHERE month_ = " + month + "" +
                                  " AND year_= " + year +
                                  " AND pref= '" + pref + "'";
                        ExecSQL(sql);
                    }
                }

                }     
            reader.Close(); 
            #endregion

            #region Выборка на экран
                      string prefData = ReportParams.Pref + DBManager.sDataAliasRest;
            sql = " SELECT TRIM(town) AS  town, TRIM(rajon) AS rajon, TRIM(" + DBManager.sNvlWord +
                      "(ulicareg,''))||' '||TRIM(ulica) AS ulica, TRIM(ndom) AS ndom, TRIM(nkor) AS nkor,k.nkvar, k.nkvar_n, k.num_ls, k.fio, idom, ikvar  " +
                      " FROM " + prefData + "dom d INNER JOIN " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                      " INNER JOIN " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                      " INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town" +
                      " INNER JOIN " + prefData + "kvar k ON k.nzp_dom = d.nzp_dom" +
                      " WHERE  k.nzp_kvar in (SELECT distinct nzp_kvar FROM t_svod )" +
                      " ORDER BY town, rajon, ulica, idom, ikvar ";
                DataTable addressTable = ExecSQLToTable(sql);
            foreach (DataRow row in addressTable.Rows)
            {
                AddressHeader += !string.IsNullOrEmpty(row["town"].ToString().Trim())
                    ? row["town"].ToString().Trim() + "/"
                    : ", -/";
                AddressHeader += !string.IsNullOrEmpty(row["rajon"].ToString().Trim())
                    ? row["rajon"].ToString().Trim() + ", "
                    : "-,";
                AddressHeader += !string.IsNullOrEmpty(row["ulica"].ToString().Trim())
                    ? row["ulica"].ToString().Trim() + ","
                    : string.Empty;
                AddressHeader += !string.IsNullOrEmpty(row["ndom"].ToString().Trim())
                    ? row["ndom"].ToString().Trim() != "-"
                        ? " д. " + row["ndom"].ToString().Trim() + ","
                        : string.Empty
                    : string.Empty;
                AddressHeader += !string.IsNullOrEmpty(row["nkor"].ToString().Trim())
                    ? row["nkor"].ToString().Trim() != "-"
                        ? " кор. " + row["nkor"].ToString().Trim() + ","
                        : string.Empty
                    : string.Empty;
                AddressHeader += !String.IsNullOrEmpty(row["nkvar"].ToString().Trim())
                    ? " кв. " + row["nkvar"].ToString().Trim() + ","
                    : string.Empty;
                AddressHeader += !string.IsNullOrEmpty(row["nkvar_n"].ToString().Trim())
                    ? row["nkvar_n"].ToString().Trim() != "-"
                        ? " ком. " + row["nkvar_n"].ToString().Trim()
                        : string.Empty
                    : string.Empty;
                AddressHeader += !string.IsNullOrEmpty(row["num_ls"].ToString().Trim())
                    ? " ЛС " + row["num_ls"].ToString().Trim() + ","
                    : string.Empty;
                AddressHeader += !string.IsNullOrEmpty(row["fio"].ToString().Trim())
                    ? row["fio"].ToString().Trim() + ","
                    : string.Empty;
                AddressHeader = AddressHeader + '\n'; 
            }
            if (!string.IsNullOrEmpty(AddressHeader))
            {
                AddressHeader = AddressHeader.TrimEnd('\n');
                AddressHeader = AddressHeader.TrimEnd(',');
            }
            sql = " UPDATE t_svod " +
                  " SET real_charge = 0 " +
                  " WHERE real_charge is null ";
            ExecSQL(sql);

            sql = " UPDATE t_svod " +
                  " SET real_insaldo = 0 " +
                  " WHERE real_insaldo is null ";
            ExecSQL(sql);


            sql = " SELECT MDY(month_,01,year_) as dat_month, a.nzp_serv, a.nzp_supp, sum(sum_insaldo) as sum_insaldo, " +
                  "        sum(sum_tarif) as sum_tarif, " +
                  "        sum(reval) as reval, " +
                  "        sum(sum_tarif+real_charge+reval) as sum_nach, " +
                  "        sum(real_charge) as real_charge, " +
                  "        sum(real_insaldo) as real_insaldo, " +
                  "        sum(sum_money) as sum_money, " +
                  "        sum(sum_outsaldo) as sum_outsaldo" +
                  " into temp t_sv "+
                  " FROM t_svod a " +
                  " WHERE 1 = 1 " + whereSupp +
                  " GROUP BY 1,2,3 ";
            ExecSQL(sql);

            sql = " SELECT dat_month,  service, name_supp, a.nzp_serv, CASE WHEN ps.id>0 THEN 1 ELSE 0 END as nzp_peni, sum(sum_insaldo) as sum_insaldo, " +
              "        sum(sum_tarif) as sum_tarif, " +
              "        sum(reval) as reval, " +
              "        sum(sum_nach) as sum_nach, " +
              "        sum(real_charge) as real_charge, " +
              "        sum(real_insaldo) as real_insaldo, " +
              "        sum(sum_money) as sum_money, " +
              "        sum(sum_outsaldo) as sum_outsaldo" +
              " FROM t_sv a JOIN " +
              ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su ON a.nzp_supp=su.nzp_supp JOIN  " +
              ReportParams.Pref + DBManager.sKernelAliasRest + "services s ON a.nzp_serv=s.nzp_serv LEFT JOIN " +
              ReportParams.Pref + DBManager.sKernelAliasRest + "peni_settings ps ON s.nzp_serv=ps.nzp_serv " + 
              " GROUP BY 1,2,3,4,5 " +
              " ORDER BY 1,3,2 ";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            #endregion

            if (dt.Rows.Count > 65000 && ReportParams.ExportFormat == ExportFormat.Excel2007)
            {
                var dtr = dt.Rows.Cast<DataRow>().Skip(65000).ToArray();
                dtr.ForEach(dt.Rows.Remove);
            }

            var ds = new DataSet();
            ds.Tables.Add(dt);

            return ds;
        }

        private string GetwhereWp()
        {
            string whereWp = String.Empty;
            if (BankSupplier != null && BankSupplier.Banks != null)
            {
                whereWp = BankSupplier.Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","));
            }
            else
            {
                whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            }
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            if (!string.IsNullOrEmpty(whereWp))
            {
                TerritoryHeader = string.Empty;
                string sql = " SELECT point FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWp;
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }
            return whereWp;
        }

        private string GetWhereAdr()
        {
           // var result = GetwhereTeritorry();
            var result = string.Empty;
            string rajon = string.Empty,
                street = string.Empty,
                house = string.Empty;
            string prefData = ReportParams.Pref + DBManager.sDataAliasRest;
            

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

            string str = string.Empty;
            if (!string.IsNullOrEmpty(house))
            {
                result += !string.IsNullOrEmpty(Nkvar.Trim())
                          ? " AND nkvar = " + STCLINE.KP50.Global.Utils.EStrNull(Nkvar) + ""
                         : String.Empty;

                result += (NkvarN.Trim() != "-" && !string.IsNullOrEmpty(NkvarN.Trim()))
                    ? " AND nkvar_n = " + STCLINE.KP50.Global.Utils.EStrNull(NkvarN) + ""
                    : String.Empty;

                str = result;
            }

            result += !string.IsNullOrEmpty(rajon) ? " AND u.nzp_raj IN ( " + rajon + ") " : string.Empty;
            result += !string.IsNullOrEmpty(street) ? " AND u.nzp_ul IN ( " + street + ") " : string.Empty;
            result += !string.IsNullOrEmpty(house) ? " AND d.nzp_dom IN ( " + house + ") " : string.Empty;
            //if (!string.IsNullOrEmpty(house))
            //{
            //    var sql = " SELECT TRIM(town) AS  town, TRIM(rajon) AS rajon, TRIM(" + DBManager.sNvlWord + "(ulicareg,''))||' '||TRIM(ulica) AS ulica, TRIM(ndom) AS ndom, TRIM(nkor) AS nkor,k.nkvar, k.nkvar_n, k.num_ls, k.fio, idom, ikvar  " +
            //              " FROM " + prefData + "dom d INNER JOIN " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
            //                                         " INNER JOIN " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
            //                                         " INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town" +
            //                                         " INNER JOIN " + prefData + "kvar k ON k.nzp_dom = d.nzp_dom" +
            //              " WHERE d.nzp_dom IN (" + house + ") "+str+ 
            //              " ORDER BY town, rajon, ulica, idom, ikvar ";
            //    DataTable addressTable = ExecSQLToTable(sql);
            //    foreach (DataRow row in addressTable.Rows)
            //    {
            //        AddressHeader += !string.IsNullOrEmpty(row["town"].ToString().Trim())
            //            ? row["town"].ToString().Trim() + "/"
            //            : ", -/";
            //        AddressHeader += !string.IsNullOrEmpty(row["rajon"].ToString().Trim())
            //            ? row["rajon"].ToString().Trim() + ", "
            //            : "-,";
            //        AddressHeader += !string.IsNullOrEmpty(row["ulica"].ToString().Trim())
            //            ? row["ulica"].ToString().Trim() + ","
            //            : string.Empty;
            //        AddressHeader += !string.IsNullOrEmpty(row["ndom"].ToString().Trim())
            //            ? row["ndom"].ToString().Trim() != "-"
            //                ? " д. " + row["ndom"].ToString().Trim() + ","
            //                : string.Empty
            //            : string.Empty;
            //        AddressHeader += !string.IsNullOrEmpty(row["nkor"].ToString().Trim())
            //            ? row["nkor"].ToString().Trim() != "-"
            //                ? " кор. " + row["nkor"].ToString().Trim() + ","
            //                : string.Empty
            //            : string.Empty;
            //        AddressHeader += !String.IsNullOrEmpty(row["nkvar"].ToString().Trim())
            //            ? " кв. " + row["nkvar"].ToString().Trim() + ","
            //            : string.Empty;
            //        AddressHeader += !string.IsNullOrEmpty(row["nkvar_n"].ToString().Trim())
            //            ? row["nkvar_n"].ToString().Trim() != "-"
            //                ? " ком. " + row["nkvar_n"].ToString().Trim()
            //                : string.Empty
            //            : string.Empty;
            //        AddressHeader += !string.IsNullOrEmpty(row["num_ls"].ToString().Trim())
            //            ? " ЛС "+ row["num_ls"].ToString().Trim() + ","
            //            : string.Empty;
            //        AddressHeader += !string.IsNullOrEmpty(row["fio"].ToString().Trim())
            //            ? row["fio"].ToString().Trim() + ","
            //            : string.Empty;
            //        AddressHeader = AddressHeader+'\n';
            //    }
            //}
            //else if (!String.IsNullOrEmpty(street))
            //{
            //    var sql = " SELECT TRIM(town) AS  town, TRIM(rajon) AS rajon, TRIM(" + DBManager.sNvlWord + "(ulicareg,''))||' '||TRIM(ulica) AS ulica, TRIM(ndom) AS ndom, TRIM(nkor) AS nkor, k.nkvar, k.nkvar_n,  k.num_ls, k.fio, idom, ikvar " +
            //              " FROM " + prefData + "dom d INNER JOIN " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
            //                                         " INNER JOIN " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
            //                                         " INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town" +
            //                                         " INNER JOIN " + prefData + "kvar k ON k.nzp_dom = d.nzp_dom" +
            //              " WHERE u.nzp_ul IN (" + street + ") "+
            //              " ORDER BY town, rajon, ulica, idom, ikvar ";
            //    DataTable addressTable = ExecSQLToTable(sql);
            //    foreach (DataRow row in addressTable.Rows)
            //    {
            //        AddressHeader += !string.IsNullOrEmpty(row["town"].ToString().Trim())
            //            ? row["town"].ToString().Trim() + "/"
            //            : ",-/";
            //        AddressHeader += !string.IsNullOrEmpty(row["rajon"].ToString().Trim())
            //            ? row["rajon"].ToString().Trim() + ","
            //            : "-,";
            //        AddressHeader += !string.IsNullOrEmpty(row["ulica"].ToString().Trim())
            //            ? row["ulica"].ToString().Trim() + ","
            //            : string.Empty;
            //        AddressHeader += !string.IsNullOrEmpty(row["ndom"].ToString().Trim())
            //            ? row["ndom"].ToString().Trim() != "-"
            //            ? " д. " + row["ndom"].ToString().Trim() + ","
            //            : string.Empty
            //            : string.Empty;
            //        AddressHeader += !string.IsNullOrEmpty(row["nkor"].ToString().Trim())
            //            ? row["nkor"].ToString().Trim() != "-"
            //                ? " кор. " + row["nkor"].ToString().Trim() + ","
            //                : string.Empty
            //            : string.Empty;
            //        AddressHeader += !String.IsNullOrEmpty(row["nkvar"].ToString().Trim())
            //            ? " кв. " + row["nkvar"].ToString().Trim() + ","
            //            : string.Empty;
            //        AddressHeader += !string.IsNullOrEmpty(row["nkvar_n"].ToString().Trim())
            //            ? row["nkvar_n"].ToString().Trim() != "-"
            //                ? " ком. " + row["nkvar_n"].ToString().Trim()
            //                : string.Empty
            //            : string.Empty;
            //        AddressHeader += !string.IsNullOrEmpty(row["num_ls"].ToString().Trim())
            //            ? " ЛС " + row["num_ls"].ToString().Trim() + ","
            //            : string.Empty;
            //        AddressHeader += !string.IsNullOrEmpty(row["fio"].ToString().Trim())
            //            ? row["fio"].ToString().Trim() + ","
            //            : string.Empty;
            //        AddressHeader = AddressHeader + '\n';
            //    }
            //}
            //else if (!string.IsNullOrEmpty(rajon))
            //{
            //    var sql = " SELECT TRIM(town) AS  town, TRIM(rajon) AS rajon, TRIM("+ DBManager.sNvlWord+"(ulicareg,''))||' '||TRIM(ulica) AS ulica, TRIM(ndom) AS ndom, TRIM(nkor) AS nkor, k.nkvar, k.nkvar_n,  k.num_ls, k.fio, idom, ikvar " +
            //              " FROM " + prefData + "dom d INNER JOIN " + prefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
            //                         " INNER JOIN " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
            //                         " INNER JOIN " + prefData + "s_town t ON t.nzp_town = r.nzp_town" +
            //                         " INNER JOIN " + prefData + "kvar k ON k.nzp_dom = d.nzp_dom" +
            //              " WHERE u.nzp_raj IN (" + rajon + ") " +
            //              " ORDER BY town, rajon, ulica, idom, ikvar ";

            //    DataTable addressTable = ExecSQLToTable(sql);
            //    foreach (DataRow row in addressTable.Rows)
            //    {
            //        AddressHeader += !string.IsNullOrEmpty(row["town"].ToString().Trim())
            //            ? row["town"].ToString().Trim() + "/"
            //            : ",-/";
            //        AddressHeader += !string.IsNullOrEmpty(row["rajon"].ToString().Trim())
            //            ? row["rajon"].ToString().Trim() + ","
            //            : "-,";
            //        AddressHeader += !string.IsNullOrEmpty(row["ulica"].ToString().Trim())
            //            ? row["ulica"].ToString().Trim() + ","
            //            : string.Empty;
            //        AddressHeader += !string.IsNullOrEmpty(row["ndom"].ToString().Trim())
            //            ? row["ndom"].ToString().Trim() != "-"
            //            ? " д. " + row["ndom"].ToString().Trim() + ","
            //            : string.Empty
            //            : string.Empty;
            //        AddressHeader += !string.IsNullOrEmpty(row["nkor"].ToString().Trim())
            //            ? row["nkor"].ToString().Trim() != "-"
            //                ? " кор. " + row["nkor"].ToString().Trim() + ","
            //                : string.Empty
            //            : string.Empty;
            //        AddressHeader += !String.IsNullOrEmpty(row["nkvar"].ToString().Trim())
            //            ? " кв. " + row["nkvar"].ToString().Trim() + ","
            //            : string.Empty;
            //        AddressHeader += !string.IsNullOrEmpty(row["nkvar_n"].ToString().Trim())
            //            ? row["nkvar_n"].ToString().Trim() != "-"
            //                ? " ком. " + row["nkvar_n"].ToString().Trim()
            //                : string.Empty
            //            : string.Empty;
            //        AddressHeader += !string.IsNullOrEmpty(row["num_ls"].ToString().Trim())
            //            ? " ЛС " + row["num_ls"].ToString().Trim() + ","
            //            : string.Empty;
            //        AddressHeader += !string.IsNullOrEmpty(row["fio"].ToString().Trim())
            //            ? row["fio"].ToString().Trim() + ","
            //            : string.Empty;
            //        AddressHeader = AddressHeader + '\n';
            //    }

            //}
            //if (!string.IsNullOrEmpty(AddressHeader))
            //{
            //    AddressHeader = AddressHeader.TrimEnd('\n');
            //    AddressHeader = AddressHeader.TrimEnd(',');
            //}

            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                int startIndex = Constants.cons_Webdata.IndexOf("Database=", StringComparison.Ordinal) + 9;
                int endIndex = Constants.cons_Webdata.Substring(startIndex, Constants.cons_Webdata.Length - startIndex).IndexOf(";", StringComparison.Ordinal);
                var tSpls = Constants.cons_Webdata.Substring(startIndex, endIndex) + DBManager.tableDelimiter + "t" + ReportParams.User.nzp_user + "_spls";
                if (TempTableInWebCashe(tSpls))
                {
                    result = " and k.nzp_kvar in (select nzp_kvar from " + tSpls + ")";
                    return result;
                }
            }
            return result;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("dat_s", DatS.ToShortDateString());
            report.SetParameterValue("dat_po", DatPo.ToShortDateString());

            AddressHeader = !string.IsNullOrEmpty(AddressHeader) ? "Адрес: " + AddressHeader : string.Empty;

            report.SetParameterValue("address", AddressHeader);
            DataTable dt = ExecSQLToTable("select distinct val_prm from " + ReportParams.Pref + DBManager.sDataAliasRest + "prm_10 where nzp_prm = 80 ");
            string agent = dt.Rows.Count > 0 ? dt.Rows[0][0].ToString().Trim():string.Empty;
            report.SetParameterValue("agent", agent);


            string area = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader : string.Empty;
            area += !string.IsNullOrEmpty(SupplierHeader) ? "\nПоставщики: " + SupplierHeader  : string.Empty;
            
            report.SetParameterValue("area", !string.IsNullOrEmpty(area) ? area : string.Empty);
        }

        protected override void PrepareParams()
        {
            Address = UserParamValues["Address"].GetValue<AddressParameterValue>();

            Nkvar = UserParamValues["Nkvar"].GetValue<string>() ?? string.Empty;
            NkvarN = UserParamValues["Nkvar_n"].GetValue<string>() ?? string.Empty;

            var period = UserParamValues["Period"].GetValue<string>();
            DateTime d1;
            DateTime d2;
            PeriodParameter.GetValues(period, out d1, out d2);
            DatS = d1;
            DatPo = d2;
            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
        }

        protected override void CreateTempTable()
        {
            const string sql = " create temp table t_svod (     " +
                               " pref char(20), " +
                               " month_ integer, " +
                               " year_ integer, " +
                               " nzp_kvar integer, " +
                               " nzp_serv integer, " +
                               " nzp_supp integer, " +
                               " sum_insaldo " + DBManager.sDecimalType + "(14,2)  default 0, " +
                               " sum_tarif " + DBManager.sDecimalType + "(14,2)  default 0, " +
                               " rsum_tarif " + DBManager.sDecimalType + "(14,2)  default 0, " +
                               " reval " + DBManager.sDecimalType + "(14,2) default 0, " +
                               " sum_real " + DBManager.sDecimalType + "(14,2)  default 0, " +
                               " real_charge " + DBManager.sDecimalType + "(14,2)  default 0, " +
                               " real_insaldo " + DBManager.sDecimalType + "(14,2) default 0, " +
                               " sum_money " + DBManager.sDecimalType + "(14,2) default 0, " +
                               " sum_outsaldo " + DBManager.sDecimalType + "(14,2) default 0)  " +
                               DBManager.sUnlogTempTable;
            ExecSQL(sql);
            ExecSQL(" CREATE INDEX ix_t_svod ON t_svod(nzp_supp,nzp_serv,month_,year_) ");

            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                const string sqls = " create temp table selected_kvars(" +
                                    " nzp_kvar integer," +
                                    " nkvar char(10)," +
                                    " nkvar_n char(10)," +
                                    " num_ls integer," +
                                    " nzp_dom integer)" +
                                    DBManager.sUnlogTempTable;
                ExecSQL(sqls);
            }
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_svod ", true);
            ExecSQL(" drop table t_sv ", true);
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
                ExecSQL(" drop table selected_kvars ", true);
            if (TempTableInWebCashe("adr_kvar"))
                ExecSQL(" drop table adr_kvar ", true);
        }

        private string GetWhereSupp(string fieldPref)
        {
            string whereSupp = String.Empty;
            if (BankSupplier != null)
            {
                if (BankSupplier.Suppliers != null)
                {

                    string supp = string.Empty;
                    supp = BankSupplier.Suppliers.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                    whereSupp += " and nzp_payer_supp in (" + supp.TrimEnd(',') + ")";
                }

                if (BankSupplier.Principals != null)
                {

                    string supp = string.Empty;
                    supp = BankSupplier.Principals.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                    whereSupp += " and nzp_payer_princip in (" + supp.TrimEnd(',') + ")";
                }

                if (BankSupplier.Agents != null)
                {

                    string supp = string.Empty;
                    supp = BankSupplier.Agents.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                    whereSupp += " and nzp_payer_agent in (" + supp.TrimEnd(',') + ")";
                }
            }

            string oldsupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);

            whereSupp = whereSupp.TrimEnd(',');

            if (!String.IsNullOrEmpty(whereSupp) || !String.IsNullOrEmpty(oldsupp))
            {
                if (!String.IsNullOrEmpty(oldsupp))
                    whereSupp += " AND nzp_supp in (" + oldsupp + ")";

                SupplierHeader = String.Empty;
                string sql = " SELECT name_supp from " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                             " WHERE nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += "(" + dr["name_supp"].ToString().Trim() + "), ";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',', ' ');
            }


            return String.IsNullOrEmpty(whereSupp) ? string.Empty : " and " + fieldPref + " in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + whereSupp + ")";
        }

        /// <summary>
        /// Выборка списка квартир в картотеке
        ///  </summary>
        /// <returns></returns>
        private bool GetSelectedKvars()
        {
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                using (IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata))
                {
                    if (!DBManager.OpenDb(connWeb, true).result) return false;

                    string tSpls = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter +
                                   "t" + ReportParams.User.nzp_user + "_spls";
                    if (TempTableInWebCashe(tSpls))
                    {
                        string sql = " insert into selected_kvars (nzp_kvar, num_ls, nzp_dom, nkvar, nkvar_n ) " +
                                     " select nzp_kvar, num_ls, nzp_dom, nkvar, nkvar_n from " + tSpls;
                        ExecSQL(sql);
                        ExecSQL("create index ix_sel_kvar_09 on selected_kvars(nzp_kvar)");
                        ExecSQL(DBManager.sUpdStat + " selected_kvars");
                        return true;
                    }
                }
            }
            return false;
        }

    }
}
