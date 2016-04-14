using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using Bars.KP50.Report.RT.Properties;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.RT.Reports
{
    class Report16311 : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.3.11 Состояние начисления и оплаты населения услуги "; }
        }

        public override string Description
        {
            get { return "3.11. Состояние начисления и оплаты населения услуги "; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> {ReportGroup.Reports};
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.SostNachislIOplatNaselUslugi; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }





        /// <summary> с расчетного дня </summary>
        private string DatS { get; set; }

        /// <summary> по расчетный год </summary>
        private string DatPo { get; set; }

        /// <summary>Услуги</summary>
        private int Services { get; set; }

        /// <summary>Поставщики</summary>
        private List<long> Suppliers { get; set; }

        /// <summary>Управляющие компании</summary>
        private List<int> Areas { get; set; }

        /// <summary>Список услуг в заголовке</summary>
        private string ServiceHeader { get; set; }

        /// <summary>Список Поставщиков в заголовке</summary>
        private string SupplierHeader { get; set; }

        /// <summary>Список балансодержателей (Управляющих компаний)</summary>
        private string AreaHeader { get; set; }

        /// <summary>Банки</summary>
        private List<int> Banks { get; set; }

        ///// <summary>Единицы измерения в заголовке</summary>
        //private string EdIzmer { get; set; }

        /// <summary>Тип ЛС</summary>
        private int TypeLs { get; set; }
        
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
                new AreaParameter(),
                new SupplierAndBankParameter(),
                new ServiceParameter(false) {Require = true, Name = "Услуга"},       
                new ComboBoxParameter
                {
                    Code = "TypeLs",
                    Name = "Лицевой счет",
                    Value = "1",
                    MultiSelect=false,
                    StoreData = new List<object>
                    {
                      
                        new { Id = "1", Name = "Открыт" },
                        new { Id = "2", Name = "Закрыт" },
                        new { Id = "3", Name = "Все" }
                    }
                },
            
            };
        }

        public override DataSet GetData()
        {
            MyDataReader reader;
            var sql = new StringBuilder();
            string whereSupp = GetWhereSupp("");
            string whereArea = GetWhereAdr("kv.");
            string whereServ = GetWhereServ("");

            sql.Remove(0, sql.Length);
            sql.Append(" SELECT * ");
            sql.Append(" FROM  " + ReportParams.Pref  + DBManager.sKernelAliasRest + "s_point ");
            sql.Append(" WHERE nzp_wp>1 " + GetwhereWp());
            ExecRead(out reader, sql.ToString());
            while (reader.Read())
            {
                string pref = reader["bd_kernel"].ToString().ToLower().Trim();
                for (int i = DatS.ToDateTime().Year * 12 + DatS.ToDateTime().Month; i < DatPo.ToDateTime().Year * 12 + DatPo.ToDateTime().Month + 1; i++)
                {
                    int year = i / 12;
                    int month = i % 12;
                    if (month == 0)
                    {
                        year--;
                        month = 12;
                    }

                    sql.Remove(0, sql.Length);
                    sql.Append(" INSERT INTO t_svod( ");
                    sql.Append(" nzp_serv, nzp_supp, nzp_area, num_ls, nkvar_n,	nkvar, ");
                    sql.Append(" ndom, nkor, ulica,	fio, tarif, money_to, ");
                    sql.Append(" reval_tarif, reval_lgota, reval, money_del, sum_money,");
                    sql.Append(" money_from, sum_outsaldo, sum_insaldo, sum_real, sum_tarif, sum_lgota, ");
                    sql.Append(" sum_subsidy, sum_smo, real_charge,	sum_charge) ");

                    sql.Append(" SELECT ");
                    sql.Append(" c.nzp_serv, c.nzp_supp, kv.nzp_area, c.num_ls, trim(case when  nkvar_n='-' then '' else  'ком.'||nkvar_n  end ) as	nkvar_n, ");
                    sql.Append(" trim(case when  nkvar='0' then '' else  'кв.'||nkvar  end ) as  nkvar, ");
                    sql.Append(" trim(ndom), trim(case when  nkor='-' or trim(nkor)=''  then '' else  nkor  end ) as nkor,	trim(ulica), ");
                    sql.Append(" trim(fio),	tarif, money_to, ");
                
                    sql.Append(" reval_tarif, reval_lgota, reval, money_del, sum_money, ");
                    sql.Append(" money_from, sum_outsaldo, sum_insaldo,	sum_real, c.sum_tarif, c.sum_lgota,");
                    sql.Append(" sum_subsidy, sum_smo, real_charge,	sum_charge");
                    sql.Append(" FROM " + pref  + DBManager.sDataAliasRest + "kvar kv, ");
                    sql.Append(pref  + DBManager.sDataAliasRest + "s_ulica su, ");
                    sql.Append(pref  + DBManager.sDataAliasRest + "dom d, ");
                    switch (TypeLs)
                    {
                        case 1:
                            sql.Append(pref + "_data" + DBManager.tableDelimiter + "prm_3 p, ");
                            break;
                        case 2:
                            sql.Append(pref + "_data" + DBManager.tableDelimiter + "prm_3 p, ");
                            break;
                    }
                    sql.Append(pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + month.ToString("00") + " c  ");
                    
                    sql.Append(" WHERE   kv.nzp_dom = d.nzp_dom AND d.nzp_ul = su.nzp_ul "+
                        "  AND	c.nzp_serv > 1 AND c.nzp_kvar = kv.nzp_kvar  AND c.dat_charge IS NULL " + whereArea + whereSupp + whereServ);
                    switch (TypeLs)
                    {
                        case 1:
                            sql.Append(" AND p.val_prm='1' AND nzp_prm = 51 AND is_actual=1 AND kv.nzp_kvar = p.nzp AND p.dat_s <= '" + DatPo + "' AND dat_po>= '" + DatS + "' ");
                            break;
                        case 2:
                            sql.Append(" AND p.val_prm='2' AND nzp_prm = 51 AND is_actual=1 AND kv.nzp_kvar = p.nzp AND p.dat_s <= '" + DatPo + "' AND dat_po>= '" + DatS + "' ");
                            break;
                    }
                    ExecRead(out reader, sql.ToString());
                }

            }
            reader.Close();
            sql.Remove(0, sql.Length);
            sql.Append("select nzp_serv, nzp_supp, nzp_area, num_ls, nkvar_n, nkvar, ndom, nkor, ulica, fio, tarif, " +
                " SUM(money_to) as money_to, " +
                " SUM(money_from) as money_from," +
                " SUM(sum_insaldo) as sum_insaldo, " +
                " SUM(sum_tarif) as sum_tarif, " +
                " SUM(sum_lgota) as sum_lgota, " +
                " SUM(sum_subsidy) as sum_subsidy, " +
                " SUM(sum_smo) as sum_smo, " +
                " SUM(real_charge) as real_charge, " +
                " SUM(reval_lgota) as reval_lgota, " +
                " SUM(reval_tarif) as reval_tarif, " +
                "(SUM (sum_insaldo + sum_tarif + real_charge + reval_tarif - (sum_lgota - reval_lgota))) as h, " +
                " SUM (CASE	WHEN tarif = 0 THEN	0 ELSE round((sum_insaldo + sum_tarif + real_charge + reval_tarif - (sum_lgota - reval_lgota)) / tarif, 2) END) as r, " +
                " SUM (CASE WHEN tarif = 0 THEN 0 ELSE round((money_to + money_from) / tarif, 2) END) as f, " +
                " SUM ((money_to + money_from)) as A, " +
                "(SUM (sum_insaldo + sum_tarif + real_charge + reval_tarif - (sum_lgota - reval_lgota)) - SUM ((money_to + money_from))) as ha, "+
                " SUM (CASE WHEN tarif = 0 THEN 0 ELSE round(((sum_insaldo + sum_tarif + real_charge + reval_tarif - (sum_lgota - reval_lgota))-((money_to + money_from)))/tarif,2 ) END ) as hat, " +
                " SUM (sum_insaldo + sum_tarif + real_charge + reval) as sum4 , " +
                " SUM (sum_lgota + reval_lgota) as vsego, " + 
                " SUM(sum_charge) as sum_charge, " + 
                " SUM(reval) as reval, " + 
                " SUM(money_del) as money_del  "+
                " FROM t_svod " +
                " GROUP BY 1,2,3,4,5,6,7,8,9,10,11 ");
            DataTable dt = ExecSQLToTable(sql.ToString());

            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        private string GetWhereServ(string tablePrefix)
        {
            var result = "";
            if (Services != 0)
            {
                result = " and " + tablePrefix + "nzp_serv in (" + Services + ") ";
                ServiceHeader = String.Empty;
                var sql = " SELECT service from " +
                            ReportParams.Pref + DBManager.sKernelAliasRest + "services " + tablePrefix.TrimEnd('.') +
                          " WHERE " + tablePrefix + "nzp_serv > 0 " + result;
                ServiceHeader = ExecSQLToTable(sql).CastAs<DataTable>().Rows[0][0].ToString().Trim();
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
                      ReportParams.Pref + DBManager.sDataAliasRest + "s_area " + tablePrefix.TrimEnd('.') +
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
            report.SetParameterValue("su", !String.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader : String.Empty);
            report.SetParameterValue("area", !String.IsNullOrEmpty(AreaHeader) ? "Балансодержатель: " + AreaHeader : String.Empty);
            report.SetParameterValue("service", ServiceHeader);
            report.SetParameterValue("ed_izmer", ""); 
            report.SetParameterValue("date", DateTime.Now.ToLongDateString());
            report.SetParameterValue("time", DateTime.Now.ToLongTimeString());
            report.SetParameterValue("dats", DatS);
            report.SetParameterValue("datpo", DatPo);
            switch (TypeLs)
            {
                case 1:
                    report.SetParameterValue("s", "Открытые ЛС");
                    break;
                case 2:
                    report.SetParameterValue("s", "Закрытые  ЛС");
                    break;
                case 3:
                    report.SetParameterValue("s", "Все ЛС");
                    break;
            }

            report.SetParameterValue("dateprint", DateTime.Now.ToShortDateString());
            report.SetParameterValue("ver", "1.0");
        }

        protected override void PrepareParams()
        {
            Services = UserParamValues["Services"].GetValue<int>();
            TypeLs= UserParamValues["TypeLs"].GetValue<int>();
            Areas = UserParamValues["Areas"].GetValue<List<int>>();
            //Month = UserParamValues["Month"].GetValue<int>();
            //Year = UserParamValues["Year"].GetValue<int>();

            string period = UserParamValues["Period"].GetValue<string>();
            DateTime d1;
            DateTime d2;
            PeriodParameter.GetValues(period, out d1, out d2);
            DatS = d1.ToShortDateString();
            DatPo = d2.ToShortDateString();
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
            string sql = "create temp table t_svod ( " +
          "  nzp_serv integer, " +
          "  nzp_area integer, " +
          "  nzp_supp integer,   " +
          "  nzp_kvar integer, " +
          "  num_ls integer, " +
          "  nkvar_n CHAR(3), " +
          "  nkvar CHAR(10), " +
          "  nkor CHAR(3), " +
          "  ulica CHAR(40), " +
          "  ndom CHAR(10), " +
          "  fio CHAR(40), " +
          "  tarif " + DBManager.sDecimalType + "(14,2), " +
          "  money_to " + DBManager.sDecimalType + "(14,2), " +
          "  money_from " + DBManager.sDecimalType + "(14,2), " +
          "  sum_insaldo " + DBManager.sDecimalType + "(14,2), " +
          "  sum_tarif " + DBManager.sDecimalType + "(14,2), " +
          "  sum_lgota " + DBManager.sDecimalType + "(14,2), " +
          "  sum_real " + DBManager.sDecimalType + "(14,2), " +
          "  sum_charge " + DBManager.sDecimalType + "(14,2)," +
          "  sum_subsidy " + DBManager.sDecimalType + "(14,2)," +
          "  reval_tarif " + DBManager.sDecimalType + "(14,2)," +
          "  reval_lgota " + DBManager.sDecimalType + "(14,2)," +
          "  reval_sub " + DBManager.sDecimalType + "(14,2)," +
          "  reval_smo " + DBManager.sDecimalType + "(14,2)," +
          "  reval " + DBManager.sDecimalType + "(14,2)," +
          "  money_del " + DBManager.sDecimalType + "(14,2)," +
          "  sum_money " + DBManager.sDecimalType + "(14,2)," +
          "  sum_outsaldo " + DBManager.sDecimalType + "(14,2)," +                
          "  real_charge " + DBManager.sDecimalType + "(14,2)," +
         // "sum_tarif" + DBManager.sDecimalType + "(14,2)," +
          "  sum_smo " + DBManager.sDecimalType + "(14,2), rashod " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_svod ", true);
        }

    }
}
