using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Bars.KP50.Report.Tula.Properties;
using FastReport.Design;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bars.KP50.Report.Tula.Reports
{
    class Report71001015_2 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.1.15.2 Отчет для сверки загруженных данных в разрезе норматива услуг"; }
        }

        public override string Description
        {
            get { return "71.1.15.2 Отчет для сверки загруженных данных в разрезе норматива услуг"; }
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
            get { return Resources.Report_71_1_15_2; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>Заголовок поставщиков</summary>
        protected string SupplierHeader { get; set; }
        /// <summary>Поставщики</summary>
        private List<long> Suppliers { get; set; }
        /// <summary>Банки данных</summary>
        private List<int> Banks { get; set; }
        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }
        /// <summary>Услуги</summary>
        private List<long> Services { get; set; }
        /// <summary>Заголовок Услуг</summary>
        protected string ServicesHeader { get; set; }

        /// <summary>Дата</summary>
        protected string Dat { get; set; }
        /// <summary>Дата</summary>     
        protected DateTime dat { get; set; }


        public override List<UserParam> GetUserParams()
        {            
            return new List<UserParam>
            {
                new SupplierAndBankParameter(),
                new ServiceParameter()
            };
        }

        protected override void PrepareParams()
        {
            Services = UserParamValues["Services"].GetValue<List<long>>();
            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
            Dat = DateTime.Now.ToShortDateString();
            dat = DateTime.Now;
        }
                      
        protected override void PrepareReport(FastReport.Report report)
        {
            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(ServicesHeader) ? "Услуги: " + ServicesHeader + "\n" : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);
            report.SetParameterValue("Dat", Dat);
        }

        public override DataSet GetData()
        {
            MyDataReader reader;

            #region заполнение временной таблицы
            string sql = " SELECT bd_kernel AS pref, point " +
                  " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                  " WHERE nzp_wp>1 " + GetwhereWp();
            ExecRead(out reader, sql);

            while (reader.Read())
            {
                string pref = reader["pref"].ToString().Trim();
                string point = reader["point"].ToString().Trim();
                string prefData = pref + DBManager.sDataAliasRest,
                       prefKernel = pref + DBManager.sKernelAliasRest;
                string gkuTable = pref + "_charge_" + dat.ToString("yy") +
                       DBManager.tableDelimiter + "calc_gku_" + dat.ToString("MM");
              
                sql = " Create temp table t_cursv( " +
                      " nzp_town integer," +
                      " nzp_kvar integer," +
                      " nzp_serv integer," +
                      " num_ls integer," +
                      " name_norm char(100)," +
                      " norm " + DBManager.sDecimalType + "(14,4))" + DBManager.sUnlogTempTable;
                ExecSQL(sql);

                sql = " insert into t_cursv (nzp_town, nzp_serv, norm, num_ls )" +
                      " select d.nzp_town, g.nzp_serv, g.rash_norm_one, k.num_ls  " +
                      " from " + gkuTable + " g, " + prefData + "dom d, " + prefData + "kvar k "
                      + " where  g.nzp_dom=d.nzp_dom and g.nzp_kvar=k.nzp_kvar and g.stek = 3  AND nzp_serv>1 "
                      + GetWhereServ()
                      + GetWhereSupp()
                      + " group by 1,2,3,4 ";
                ExecSQL(sql); 

                sql = "create index ix_t_cursv01 on t_cursv(nzp_serv)";
                ExecSQL(sql);
                sql = "create index ix_t_cursv02 on t_cursv(num_ls)";
                ExecSQL(sql);

                ExecSQL(DBManager.sUpdStat + " t_cursv");

                sql = " update t_cursv set name_norm=(select max(name_y) "
                    + " from " + prefKernel + "res_y y, " + prefKernel + "prm_name n, " + prefData + "prm_1 p "
                    + " where t_cursv.nzp_serv=6 and p.nzp=num_ls and p.nzp_prm=7  and n.nzp_prm=7 and p.val_prm= y.nzp_y" + DBManager.sConvToVarChar + 
                    " and  y.nzp_res=n.nzp_res " +
                      " and p.is_actual=1 "
                      + " and p.dat_s<= '" + Dat + "'"
                      + " and p.dat_po>='" + Dat + "'"
                    +")";
                ExecSQL(sql);  

                sql = " insert into t_svod(rajon, town, service, name_norm, norm, count_ls )" +
                      " select '" + point + "', town, service, name_norm, norm, count(num_ls) " +
                      " from t_cursv t, " + prefData+"s_town st,"+ prefKernel+"services s" +
                      " where t.nzp_town=st.nzp_town and s.nzp_serv=t.nzp_serv" +
                      " group by 1,2,3,4,5 ";
                ExecSQL(sql);  

                ExecSQL("drop table t_cursv");
            }
            #endregion

            sql = " select *" +
                  " from t_svod " +
                  " order by 1,2,3,4 ";

            var ds = new DataSet();
            DataTable supplierTable = ExecSQLToTable(sql);
            supplierTable.TableName = "Q_master";

            ds.Tables.Add(supplierTable);

            return ds;
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
            whereWp = whereWp.TrimEnd(',', ' ');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND bd_kernel in (" + whereWp + ")" : String.Empty;
            if (!string.IsNullOrEmpty(whereWp))
            {
                string sql1 = " SELECT point FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWp;
                DataTable terrTable = ExecSQLToTable(sql1);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }

            return whereWp;
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
            whereSupp = !String.IsNullOrEmpty(whereSupp) ? " AND t.nzp_supp in (" + whereSupp + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereSupp))
            {
                string sql = " SELECT name_supp " +
                             " from " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier t" +
                             " WHERE nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += dr["name_supp"].ToString().Trim() + ", ";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',', ' ');
            }
            return whereSupp;
        }

        private string GetWhereServ()
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
            result = !String.IsNullOrEmpty(result) ? " AND g.nzp_serv in (" + result + ")" : String.Empty;
            if (!String.IsNullOrEmpty(result))
            {
                string sql = " SELECT service " +
                             " from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services g" +
                             " WHERE g.nzp_serv > 1 " + result;
                DataTable serv = ExecSQLToTable(sql);
                foreach (DataRow dr in serv.Rows)
                {
                    ServicesHeader += dr["service"].ToString().Trim() + ", ";
                }
                ServicesHeader = ServicesHeader.TrimEnd(',', ' ');
            }

            return result;
        }

        protected override void CreateTempTable()
        {

            string sql = " CREATE TEMP TABLE t_svod( " +
                  " rajon char(100), " +
                  " town char(100), " +//MO
                  " name_supp char(60), " +
                  " service char(60), " +
                  " count_ls integer, " +
                  " name_norm char(100), " +
                  " norm  " + DBManager.sDecimalType + "(14,4) " +
                  " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_svod ");
        }
    }
}
