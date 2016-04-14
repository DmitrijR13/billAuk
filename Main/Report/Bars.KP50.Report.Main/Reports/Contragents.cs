using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Main.Properties;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Main.Reports
{
    class Contragents : BaseSqlReport
    {
        public override string Name
        {
            get { return "Список ЛС по контрагентам"; }
        }

        public override string Description
        {
            get { return "Список ЛС по контрагентам"; }
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
            get { return Resources.Contragents; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        /// <summary>Услуги</summary>
        protected List<int> Services { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Управляющие компании</summary>
        protected int Areas { get; set; }

        /// <summary>Управляющие компании</summary>
        protected string AreaHeader { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }
        


        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                //new MonthParameter{ Value = DateTime.Now.Month },
                //new YearParameter{ Value = DateTime.Now.Year },
                new SupplierAndBankParameter(),
                new AreaParameter(false),
                new ServiceParameter(),                  
            };
        }

        //public override DataSet GetData()
        //{
        //    MyDataReader reader;
        //    var sql = new StringBuilder();
      

         

        //    #region Выборка по локальным банкам

        //    sql.Remove(0, sql.Length);
        //    sql.Append(" SELECT * ");
        //    sql.Append(" FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point ");
        //    sql.Append(" WHERE nzp_wp>1 " + GetwhereWp());
        //    ExecRead(out reader, sql.ToString());
        //    sql.Remove(0, sql.Length);
        //    sql.Append(" insert into t_sprav (area, name_supp,service,ls_count_open,ls_count_close)   ");
        //    sql.Append(" select area, name_supp,service,sum(ls_count_open) as ls_count_open ,sum(ls_count_close) as ls_count_close from ( ");
        //    while (reader.Read()) {
        //        string pref = reader["bd_kernel"].ToString().ToLower().Trim();

        //        sql.Append(" select area, name_supp, service, " +
        //            " count(distinct k.nzp_kvar) as ls_count_open, " +
        //            " 0 as ls_count_close " +
        //            " from " +
        //                ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su, " +
        //                ReportParams.Pref + DBManager.sKernelAliasRest + "services se, " +
        //                ReportParams.Pref + DBManager.sDataAliasRest + "s_area a, " +
        //                pref + DBManager.sDataAliasRest + "tarif t, " +
        //                ReportParams.Pref + DBManager.sDataAliasRest + "kvar k " +
        //            " where dat_s<'" + DateTime.Now.ToShortDateString() + "' and dat_po>'" + DateTime.Now.ToShortDateString() + "'" +
        //            " and se.nzp_serv = t.nzp_serv and su.nzp_supp = t.nzp_supp and t.num_ls = k.num_ls " +
        //              GetWhereArea() + GetWhereServ() + GetWhereSupp() +
        //            " and is_actual = 1 " +
        //            " group by 1,2,3 " +
        //            " union " +
        //            " select area, name_supp, service, " +
        //            " 0 as ls_count_open, " +
        //            " count(distinct k.nzp_kvar) as ls_count_close " +
        //            " from " +
        //                ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su, " +
        //                ReportParams.Pref + DBManager.sKernelAliasRest + "services se, " +
        //                ReportParams.Pref + DBManager.sDataAliasRest + "s_area a, " +
        //                pref + DBManager.sDataAliasRest + "tarif t, " +
        //                ReportParams.Pref + DBManager.sDataAliasRest + "kvar k " +
        //            " where dat_s<'" + DateTime.Now.ToShortDateString() + "' and dat_po>'" + DateTime.Now.ToShortDateString() + "'" +
        //            " and se.nzp_serv = t.nzp_serv and su.nzp_supp = t.nzp_supp and t.num_ls = k.num_ls " +
        //              GetWhereArea() + GetWhereServ() + GetWhereSupp() +
        //            " and is_actual <> 1 " +
        //            " group by 1,2,3 " +
        //            " union ");               
        //    }
        //    sql.Remove(sql.Length -7, 7);
        //    sql.Append(" ) group by 1,2,3 ");

        //    ExecSQL(sql.ToString());

        //    sql.Remove(0, sql.Length);
        //    sql.Append(" SELECT * ");
        //    sql.Append(" FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point ");
        //    sql.Append(" WHERE nzp_wp>1 " + GetwhereWp());
        //    ExecRead(out reader, sql.ToString());
        //    sql.Remove(0, sql.Length);
        //    sql.Append(" insert into t_sprav_2 (count_open,count_close)   ");
        //    sql.Append(" select sum(count_open) as count_open, sum(count_close) as count_close from ( ");
        //    while (reader.Read())
        //    {
        //        string pref = reader["bd_kernel"].ToString().ToLower().Trim();

        //        sql.Append(" select  " +
        //            " count (distinct k.nzp_kvar) as count_open, " +
        //            " 0 as count_close  " +
        //            " from " +
        //                ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su, " +
        //                ReportParams.Pref + DBManager.sKernelAliasRest + "services se, " +
        //                ReportParams.Pref + DBManager.sDataAliasRest + "s_area a, " +
        //                pref + DBManager.sDataAliasRest + "tarif t, " +
        //                ReportParams.Pref + DBManager.sDataAliasRest + "kvar k " +
        //            " where dat_s<'" + DateTime.Now.ToShortDateString() + "' and dat_po>'" + DateTime.Now.ToShortDateString() + "'" +
        //            " and se.nzp_serv = t.nzp_serv and su.nzp_supp = t.nzp_supp and t.num_ls = k.num_ls " +
        //              GetWhereArea() + GetWhereServ() + GetWhereSupp() +
        //            " and t.is_actual = 1 " +
        //            " union " +
        //            " select  " +
        //            " 0 as count_open, " +
        //            " count (distinct k.nzp_kvar) as count_close  " +
        //            " from " +
        //                ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su, " +
        //                ReportParams.Pref + DBManager.sKernelAliasRest + "services se, " +
        //                ReportParams.Pref + DBManager.sDataAliasRest + "s_area a, " +
        //                pref + DBManager.sDataAliasRest + "tarif t, " +
        //                ReportParams.Pref + DBManager.sDataAliasRest + "kvar k " +
        //            " where dat_s<'" + DateTime.Now.ToShortDateString() + "' and dat_po>'" + DateTime.Now.ToShortDateString() + "'" +
        //            " and se.nzp_serv = t.nzp_serv and su.nzp_supp = t.nzp_supp and t.num_ls = k.num_ls " +
        //              GetWhereArea() + GetWhereServ() + GetWhereSupp() +
        //            " and t.is_actual <> 1 " +
        //            " union ");
        //    }
        //    reader.Close();
        //    sql.Remove(sql.Length - 7, 7);
        //    sql.Append(" ) ");
        //    ExecSQL(sql.ToString());
        //    DataTable dt = ExecSQLToTable(" select * from t_sprav ");
        //    dt.TableName = "Q_master";
        //    DataTable dt1 = ExecSQLToTable(" select * from t_sprav_2 ");
        //    dt1.TableName = "Q_master1";
        //    #endregion

        //    var ds = new DataSet();
        //    ds.Tables.Add(dt);
        //    ds.Tables.Add(dt1);
        //    return ds;
        //}

        public override DataSet GetData()
        {
            MyDataReader reader;


            #region Выборка по локальным банкам


            string sql = " SELECT * " +
                         " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " WHERE nzp_wp>1 " + GetwhereWp();
            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["bd_kernel"].ToString().ToLower().Trim();

                sql = " Create temp table t_ls (" +
                      " nzp_area integer, " +
                      " nzp_supp integer, " +
                      " nzp_serv integer, " +
                      " nzp_kvar integer," +
                      " is_open integer)" + DBManager.sUnlogTempTable;
                ExecSQL(sql);

                sql = " insert into t_ls (nzp_area, nzp_supp, nzp_serv, nzp_kvar, is_open) " +
                      " select nzp_area, nzp_supp, nzp_serv, k.nzp_kvar, max(val_prm" + DBManager.sConvToNum +")"+
                      " from " + pref + DBManager.sDataAliasRest + "tarif t, " +
                                 pref + DBManager.sDataAliasRest + "kvar k, " +
                                 pref + DBManager.sDataAliasRest + "prm_3 p " +
                      " where t.dat_s<=" + DBManager.sCurDate +
                      "      and t.dat_po>=" + DBManager.sCurDate + "" +
                      "      and t.num_ls = k.num_ls " + GetWhereArea() + GetWhereServ() + GetWhereSupp() +
                      "      and t.nzp_kvar = p.nzp " +
                      "      and p.nzp_prm = 51 " +
                      "      and t.is_actual = 1 " +
                      "      and p.dat_s<=" + DBManager.sCurDate +
                      "      and p.dat_po>=" + DBManager.sCurDate + 
                      "      and p.is_actual = 1 " +
                      " group by 1,2,3,4 ";
                ExecSQL(sql);
                
                sql = " insert into t_sprav (nzp_area, nzp_supp, nzp_serv, ls_count_open, ls_count_close)   "+
                      " select nzp_area, nzp_supp,nzp_serv,sum(case when is_open =1 then 1 else 0 end)," +
                      "        sum(case when is_open =1 then 0 else 1 end) " +
                      " from t_ls " +
                      " group by 1,2,3";
                ExecSQL(sql);

                sql = " insert into t_sprav2 (nzp_area, ls_count_open, ls_count_close)   " +
                      " select nzp_area, count(distinct nzp_kvar), sum(0)" +
                      " from t_ls " +
                      " where is_open =1 "+
                      " group by 1";
                ExecSQL(sql);

                sql = " insert into t_sprav2 (nzp_area, ls_count_open, ls_count_close)   " +
                      " select nzp_area,  sum(0), count(distinct nzp_kvar)" +
                      " from t_ls " +
                      " where is_open >1 " +
                      " group by 1";
                ExecSQL(sql);
       

                ExecSQL("drop table t_ls");
                 
            }


            sql = " select area, name_supp, service, sum(ls_count_open) as ls_count_open, " +
                  "        sum(ls_count_close) as ls_count_close " +
                  " from t_sprav a,  " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_area sa, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "services se, " +
                  ReportParams.Pref + DBManager.sKernelAliasRest + "supplier su " +
                  " where a.nzp_area=sa.nzp_area " +
                  "     and a.nzp_serv=se.nzp_serv " +
                  "     and a.nzp_supp=su.nzp_supp "+
                  " group by 1,2,3 "+
                  " order by 2,1,3 ";    
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";

            sql = " select area, sum(ls_count_open) as count_open, " +
                  "        sum(ls_count_close) as count_close " +
                  " from t_sprav2 a,  " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_area sa " +
                  " where a.nzp_area=sa.nzp_area " +
                  " group by 1 "+
                  " order by 1 ";
            DataTable dt1 = ExecSQLToTable(sql);
            dt1.TableName = "Q_master1";



            sql = " select sum(ls_count_open) as count_open, " +
                  "        sum(ls_count_close) as count_close " +
                  " from t_sprav2 a,  " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_area sa " +
                  " where a.nzp_area=sa.nzp_area ";
            DataTable dt2 = ExecSQLToTable(sql);
            dt2.TableName = "Q_master2";
            #endregion

            var ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables.Add(dt1);
            ds.Tables.Add(dt2);
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

        private string GetWhereServ()
        {
            string whereServ = String.Empty;
            if (Services != null)
            {
                whereServ = Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ","));
                whereServ = whereServ.TrimEnd(',');
            }
            if (String.IsNullOrEmpty(whereServ))
                whereServ = ReportParams.GetRolesCondition(Constants.role_sql_serv);


            if (!String.IsNullOrEmpty(whereServ))
            {
                whereServ = " and nzp_serv in(" + whereServ + ") ";
            }
            return whereServ;
        }

        private string GetWhereArea()
        {
            
            string whereArea = String.Empty;

            if (Areas.ToString(CultureInfo.InvariantCulture) != "0")
            {
                whereArea = Areas.ToString(CultureInfo.InvariantCulture);
                if (string.IsNullOrEmpty(whereArea))
                    whereArea = ReportParams.GetRolesCondition(Constants.role_sql_area);

                string sql = " SELECT area from " +
                             ReportParams.Pref + DBManager.sDataAliasRest + "s_area " +
                             " WHERE nzp_area > 0 and nzp_area in(" + whereArea + ") ";
                DataTable area = ExecSQLToTable(sql);
                AreaHeader = area.Rows[0][0].ToString().Trim();
                whereArea = " and nzp_area in (" + whereArea + ") ";
            }

            
            return whereArea;
        }

        private string GetWhereSupp()
        {
            string whereSupp = String.Empty;
            if (Suppliers != null)
            {
                whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp = whereSupp.TrimEnd(',');
            }
            if (String.IsNullOrEmpty(whereSupp))
                whereSupp = ReportParams.GetRolesCondition(Constants.role_sql_serv);
            if (!String.IsNullOrEmpty(whereSupp))
            {
                whereSupp = " and nzp_supp in (" + whereSupp + ") ";
            }
            return whereSupp;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("dat", DateTime.Now.ToShortDateString());
            report.SetParameterValue("time", DateTime.Now.ToShortTimeString());     
        }

        protected override void PrepareParams()
        {
            Services = UserParamValues["Services"].GetValue<List<int>>();
            Areas = UserParamValues["Areas"].GetValue<int>();
            //Month = UserParamValues["Month"].GetValue<int>();
            //Year = UserParamValues["Year"].GetValue<int>();
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
            string sql = "create temp table t_sprav ( " +
                               " nzp_supp integer, " +
                               " nzp_serv integer, " +
                               " nzp_area integer, " +
                               " ls_count_open integer default 0, " +
                               " ls_count_close integer default 0 ) "+DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = "create temp table t_sprav2 ( " +
                               " nzp_area integer, " +
                               " ls_count_open integer default 0, " +
                               " ls_count_close integer default 0 ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

         
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_sprav ", true);
            ExecSQL(" drop table t_sprav2 ", true);

        }

    }
}
