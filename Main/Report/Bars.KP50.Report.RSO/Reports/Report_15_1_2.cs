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
using STCLINE.KP50.Global;
using Bars.KP50.Report.RSO.Properties;

namespace Bars.KP50.Report.RSO.Reports
{
    class Report1512 : BaseSqlReport
    {
        public override string Name
        {
            get { return "15.1.2 Отчёт о принятых платежах для поставщиков"; }
        }

        public override string Description
        {
            get { return "Отчёт о принятых платежах для поставщиков"; }
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
            get { return Resources.Report_15_1_2; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }


        /// <summary> Период - с </summary>
        protected DateTime Dats { get; set; }

        /// <summary> Период -  по </summary>
        protected DateTime Datpo { get; set; }

        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }

        /// <summary>Группировка</summary>
        protected int Grouper { get; set; }

        /// <summary>Поставщики</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Услуги</summary>
        protected string ServiceHeader { get; set; }

        /// <summary>Тип пачки</summary>
        protected Byte TypePack { get; set; }

        /// <summary>Дополнительные параметры</summary>
        protected List<int> FioServ { get; set; }


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
                new ServiceParameter(),
                new ComboBoxParameter
                {
                    Name = "Группировка",
                    Code = "Grouper",
                    Value = 1,
                    MultiSelect = false,
                    Require = true,
                    StoreData = new List<object>
                    {
                        new {Id = 1, Name = "Без группировки"},
                        new {Id = 2, Name = "По дате"},
                        new {Id = 3, Name = "По пунктам приема платежей"},
                        new {Id = 4, Name = "По улице"},
                        new {Id = 5, Name = "По домам"}
                    }
                },                
                new ComboBoxParameter
                {
                    Name = "Тип пачки",
                    Code = "TypePack",
                    Value = 1,
                    MultiSelect = false,
                    Require = true,
                    StoreData = new List<object>
                    {
                        new {Id = 1, Name = "средства РЦ"},
                        new {Id = 2, Name = "оплаты ПУ и УК"}
                    }
                },              
                new ComboBoxParameter(true)
                {
                    Name = "Дополнительно",
                    Code = "FioServ",
                    Value = 1,
                    Require = true,
                    StoreData = new List<object>
                    {
                        new {Id = 1, Name = "ФИО"},
                        new {Id = 2, Name = "Услуга"},
                        new {Id = 3, Name = "Количество жильцов"},
                        new {Id = 4, Name = "Площадь"}
                    }
                },
            };
        }

        protected override void PrepareParams()
        {
            var period = UserParamValues["Period"].GetValue<string>();
            DateTime d1;
            DateTime d2;
            PeriodParameter.GetValues(period, out d1, out d2);
            Dats = d1;
            Datpo = d2;

            Services = UserParamValues["Services"].Value.To<List<int>>();
            TypePack = UserParamValues["TypePack"].Value.To<Byte>();
            FioServ = UserParamValues["FioServ"].Value.To<List<int>>();
            Grouper = UserParamValues["Grouper"].GetValue<int>();
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
            if (!String.IsNullOrEmpty(SupplierHeader))
            {
                report.SetParameterValue("supp", SupplierHeader);
            }
            else
            {
                report.SetParameterValue("supp", "Все");
            } 
            if (!String.IsNullOrEmpty(ServiceHeader))
            {
                report.SetParameterValue("serv", ServiceHeader);
            }
            else
            {
                report.SetParameterValue("serv", "Все");
            }

            DataTable kolPlat = ExecSQLToTable(" select count(sum_prih) as kol_plat from t_report_15_1_2 ");
            report.SetParameterValue("kol_plat", kolPlat.Rows[0][0].ToString().Trim());

            DataTable sumPlat = ExecSQLToTable(" select sum(sum_prih) as sum_plat from t_report_15_1_2 ");
            report.SetParameterValue("sum_plat", sumPlat.Rows[0][0].ToString().Trim());

            report.SetParameterValue("dats", Dats.ToShortDateString());
            report.SetParameterValue("datpo", Datpo.ToShortDateString());

            report.SetParameterValue("date", DateTime.Now.ToShortDateString());
            report.SetParameterValue("time", DateTime.Now.ToLongTimeString());

            report.SetParameterValue("fio", FioServ.Contains(1));
            report.SetParameterValue("service", FioServ.Contains(2));
            report.SetParameterValue("gil", FioServ.Contains(3));
            report.SetParameterValue("pl", FioServ.Contains(4));

            if (Grouper == 1)
            {
                report.SetParameterValue("VisualGroup",true);
            }
        }

        public override DataSet GetData()
        {
            var sql = new StringBuilder();
            MyDataReader reader;
 
            sql.Append(" SELECT * " + 
                   " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                   " WHERE nzp_wp>1 " + GetwhereWp());

            ExecRead(out reader, sql.ToString());

            while (reader.Read())
            {
                var pref = reader["bd_kernel"].ToString().ToLower().Trim();

                if (TypePack == 1)
                    for (int i = Dats.Year*12 + Dats.Month; i < Datpo.Year*12 + Datpo.Month + 1; i++)
                    {
                        var year = i/12;
                        var month = i%12;
                        if (month == 0)
                        {
                            year--;
                            month = 12;
                        }
                        string finPack = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") +
                                         DBManager.tableDelimiter + "pack ";
                        string finPackLs = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") +
                                           DBManager.tableDelimiter + "pack_ls ";
                        string fnSupp = pref + "_charge_" + (year-2000).ToString("00") + DBManager.tableDelimiter + "fn_supplier" +
                                        month.ToString("00");

                        if (TempTableInWebCashe(finPack) && TempTableInWebCashe(finPackLs) && TempTableInWebCashe(fnSupp))
                        {
                            sql.Remove(0, sql.Length);
                            sql.Append(" INSERT INTO t_report_15_1_2(num_ls, bank, dat_uchet, sum_prih, nzp_serv)" +
                                       " SELECT s.num_ls, bank, p.dat_uchet, s.sum_prih, s.nzp_serv " +
                                       " FROM " + finPack + " p, " +
                                       finPackLs + " pls, " +
                                       fnSupp + " s, " +
                                       ReportParams.Pref + DBManager.sKernelAliasRest + "s_bank b " +
                                       " WHERE pls.nzp_pack = p.nzp_pack and p.nzp_bank = b.nzp_bank and pls.nzp_pack_ls = s.nzp_pack_ls " +
                                       " AND pls.dat_uchet>='" + Dats.ToShortDateString() + "' " +
                                       " AND pls.dat_uchet<='" + Datpo.ToShortDateString() + "' " +
                                       GetWhereSupp() + GetWhereServ());
                            ExecSQL(sql.ToString());
                        }
                    }
                else
                    for (int year = Dats.Year; year <= Datpo.Year; year++)
                    {
                        string finPack = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") +
                            DBManager.tableDelimiter + "pack ";
                        string finPackLs = ReportParams.Pref + "_fin_" + (year - 2000).ToString("00") +
                                           DBManager.tableDelimiter + "pack_ls ";
                        string fromSupp = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                          "from_supplier";
                        if (TempTableInWebCashe(finPack) && TempTableInWebCashe(finPackLs) && TempTableInWebCashe(fromSupp))
                        {
                            sql.Remove(0, sql.Length);
                            sql.Append(" INSERT INTO t_report_15_1_2(num_ls, bank, dat_uchet, sum_prih, nzp_serv)" +
                                       " SELECT s.num_ls, bank, p.dat_uchet, s.sum_prih, s.nzp_serv " +
                                       " FROM " + finPack + " p, " +
                                       finPackLs + " pls, " +
                                       fromSupp + " s, " +
                                       ReportParams.Pref + DBManager.sKernelAliasRest + "s_bank b " +
                                       " WHERE pls.nzp_pack = p.nzp_pack and p.nzp_bank = b.nzp_bank " +
                                       " and pls.nzp_pack_ls = s.nzp_pack_ls " +
                                       " AND pls.dat_uchet>='" + Dats.ToShortDateString() + "' " +
                                       " AND pls.dat_uchet<='" + Datpo.ToShortDateString() + "' "+
                                       GetWhereSupp() + GetWhereServ());
                            ExecSQL(sql.ToString());
                        }
                    }

                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_report_15_1_2 " +
                           " SET gil = (SELECT MAX(REPLACE(val_prm,',','.') " + DBManager.sConvToNum + ") ");
                           sql.Append(" FROM " + pref + DBManager.sDataAliasRest + "prm_1 p, " +
                                                 pref + DBManager.sDataAliasRest + "kvar kv " +
                                      " WHERE is_actual = 1 " +
                                        " AND dat_s <= '" + Dats.ToShortDateString() + "' " +
                                        " AND dat_po >= '" + Datpo.ToShortDateString() + "' " +
                                        " AND nzp_prm = 5 " +
                                        " AND p.nzp = kv.nzp_kvar " +
                                        " AND kv.num_ls = t_report_15_1_2.num_ls) ");
                ExecSQL(sql.ToString());

                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_report_15_1_2 " +
                           " SET pl = (SELECT MAX(REPLACE(val_prm,',','.') " + DBManager.sConvToNum + ") ");
                sql.Append(" FROM " + pref + DBManager.sDataAliasRest + "prm_1 p, " +
                                      pref + DBManager.sDataAliasRest + "kvar kv " +
                           " WHERE is_actual = 1 " +
                             " AND dat_s <= '" + Dats.ToShortDateString() + "' " +
                             " AND dat_po >= '" + Datpo.ToShortDateString() + "' " +
                             " AND nzp_prm = 4 " +
                             " AND p.nzp = kv.nzp_kvar " +
                             " AND kv.num_ls = t_report_15_1_2.num_ls) ");
                ExecSQL(sql.ToString());
            }
            
            sql.Remove(0, sql.Length);
            sql.Append(" SELECT ");
            switch (Grouper)
            {
                case 1: sql.Append(" '' as grouper, "); break;
                case 2: sql.Append(" dat_uchet as grouper, "); break;
                case 3: sql.Append(" bank as grouper, "); break;
                case 4: sql.Append(" (case when trim(rajon)='-' then TRIM(town) else trim(town)||' '||trim(rajon) end) || ', ул.' || trim(ulica) as grouper, "); break;
                case 5: sql.Append("  (case when trim(rajon)='-' then TRIM(town) else trim(town)||' '||trim(rajon) end) || ', ул.' || trim(ulica) || ', д.' || trim(ndom)  as grouper, "); break;
            }
            sql.Append(FioServ.Contains(1) ? " fio, " : " '' as fio, ");
            sql.Append(FioServ.Contains(2) ? " service, " : " '' as service, ");
            sql.Append(FioServ.Contains(3) ? " gil, " : " 0 as gil, ");
            sql.Append(FioServ.Contains(4) ? " pl, " : " 0 as pl, ");
            sql.Append(" pkod, dat_uchet, ");
            sql.Append(" case when rajon='-' then town else trim(town)||' '||trim(rajon) end as town, ulica, ndom, idom," +
                       " case when nkor<>'-' then nkor end as nkor, " +
                       " case when nkvar<>'-' then 'кв.'||nkvar end as nkvar, ikvar," +
                       " case when nkvar_n<>'-' then nkvar_n end as nkvar_n, sum(sum_prih) as sum_prih ");
            sql.Append(" FROM t_report_15_1_2 t, " +
                         ReportParams.Pref + DBManager.sDataAliasRest + "s_town st, " +
                         ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                         ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                         ReportParams.Pref + DBManager.sDataAliasRest + "dom d, " +
                         ReportParams.Pref + DBManager.sDataAliasRest + "kvar k, " +
                         ReportParams.Pref + DBManager.sKernelAliasRest + "services se " + 
                       " where t.num_ls = k.num_ls and k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul and se.nzp_serv = t.nzp_serv " +
                       " and u.nzp_raj = r.nzp_raj and r.nzp_town = st.nzp_town " +
                       " group by 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15 ");
            
            sql.Append(" ORDER BY ");
            if (Grouper == 2 || Grouper == 3)
            {
                sql.Append(" 1, ");
            }
            sql.Append(" town, ulica, idom, ndom, ikvar, nkvar ");
            DataTable dt = ExecSQLToTable(sql.ToString());
            dt.TableName = "Q_master";

            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }        
        
        /// <summary>
        /// Получить условия органичения по услугам
        /// </summary>
        /// <returns></returns>
        private string GetWhereServ()
        {
            string whereServ = String.Empty;
            if (Services != null)
            {
                whereServ = Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ","));
            }
            whereServ = whereServ.TrimEnd(',');
            if (String.IsNullOrEmpty(whereServ)) whereServ = ReportParams.GetRolesCondition(Constants.role_sql_serv);
            whereServ = !String.IsNullOrEmpty(whereServ) ? " AND nzp_serv in (" + whereServ + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereServ) & String.IsNullOrEmpty(ServiceHeader))
            {
                string sql = " SELECT service from " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "services " +
                             " WHERE nzp_serv > 1 " + whereServ;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    ServiceHeader += dr["service"].ToString().Trim() + ",";
                }
                ServiceHeader = ServiceHeader.TrimEnd(',');
            }
            return whereServ;
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp()
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
                result = " AND s.nzp_supp in (" + result + ")";

                SupplierHeader = String.Empty;
                var sql = " SELECT name_supp from " +
                          ReportParams.Pref + DBManager.sKernelAliasRest + "supplier s WHERE nzp_supp > 0 " + result;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += dr["name_supp"].ToString().Trim() + ",";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',');
            }
            return result;
        }

        /// <summary>
        /// Получить условия органичения по банкам
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
            string sql = " CREATE TEMP TABLE t_report_15_1_2( " +
                               " num_ls INTEGER, " +
                               " bank CHARACTER(40), " +
                               " dat_uchet DATE, " +
                               " nzp_serv INTEGER, " +
                               " gil " + DBManager.sDecimalType + "(14,2), " +
                               " pl " + DBManager.sDecimalType + "(14,2), " +
                               " sum_prih " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql); 
        }

        protected override void DropTempTable()
        {
            ExecSQL("DROP TABLE t_report_15_1_2");
        }
    }
}
