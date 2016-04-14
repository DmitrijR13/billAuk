using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using Bars.KP50.Report.Tula.Properties;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bars.KP50.Report.Tula.Reports
{
    class Report71001015_1 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.1.15.1 Отчет для сверки загруженных данных по услугам и тарифам"; }
        }

        public override string Description
        {
            get { return "71.1.15.1 Отчет для сверки загруженных данных по услугам и тарифам"; }
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
            get { return Resources.Report_71_1_15_1; }
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
            

                sql = " Create temp table t_cursv( " +
                      " nzp_supp integer," +
                      " nzp_serv integer," +
                      " nzp_prm_tarif_ls integer,"+
                      " nzp_prm_tarif_dm integer,"+
                      " nzp_prm_tarif_su integer,"+
                      " nzp_prm_tarif_bd integer)";
                ExecSQL(sql);

                sql = " Create temp table tmp_cursv( " +
                      " nzp_supp integer," +
                      " nzp_serv integer," +
                      " tarif_type integer," +
                      " tarif  " + DBManager.sDecimalType + "(14,4) )";
                ExecSQL(sql);


                    sql = " insert into t_cursv(nzp_serv, nzp_supp, nzp_prm_tarif_ls,nzp_prm_tarif_dm,nzp_prm_tarif_su,nzp_prm_tarif_bd )" +
                          " select t.nzp_serv, t.nzp_supp, nzp_prm_tarif_ls, nzp_prm_tarif_dm, nzp_prm_tarif_su, nzp_prm_tarif_bd" +
                          " from "+prefData + "tarif t, "+prefKernel+"formuls_opis f  where is_actual =1 and f.nzp_frm=t.nzp_frm "
                          +GetWhereSupp()
                          +GetWhereServ()
                          +" and t.dat_s<= '" + Dat + "'" 
                          +" and t.dat_po>='" + Dat + "'"
                          +" group by 1,2,3,4,5,6";
                    ExecSQL(sql);
                            
                ExecSQL("create index ix_t_cursv01 on t_cursv(nzp_supp)");
                ExecSQL("create index ix_t_cursv02 on t_cursv(nzp_serv)");
                ExecSQL("create index ix_t_cursv03 on t_cursv(nzp_prm_tarif_ls)");
                ExecSQL("create index ix_t_cursv04 on t_cursv(nzp_prm_tarif_dm)");
                ExecSQL("create index ix_t_cursv05 on t_cursv(nzp_prm_tarif_su)");
                ExecSQL("create index ix_t_cursv06 on t_cursv(nzp_prm_tarif_bd)");

                ExecSQL(DBManager.sUpdStat + " t_cursv");

                string[] tables =new string[4]{"prm_1","prm_2","prm_11","prm_5"};
                string[] columns = new string[4] { "nzp_prm_tarif_ls", "nzp_prm_tarif_dm", "nzp_prm_tarif_su", "nzp_prm_tarif_bd" };
                for (int i = 0; i < 4; ++i)
                {
                    sql = " insert into tmp_cursv( nzp_serv, nzp_supp, tarif_type, tarif )"
                          + " select  nzp_serv, nzp_supp, " + i + ", val_prm"+ DBManager.sConvToNum
                          + " from t_cursv t, " + prefData + tables[i]
                          + " where is_actual=1 "
                          + " and dat_s<= '" + Dat + "'" 
                          + " and dat_po>='" + Dat + "'"
                          + " and nzp_prm=" + columns[i]
                          + ((i==2)? " and nzp = nzp_supp" :"" )
                          + " group by 1,2,3,4";
                    ExecSQL(sql);
                }

                ExecSQL("create index ix_tmp_cursv01 on tmp_cursv(nzp_supp)");
                ExecSQL("create index ix_tmp_cursv02 on tmp_cursv(nzp_serv)");

                sql = " insert into t_svod_71_1_15_1( rajon, service, name_supp, tarif_type,tarif)"
                    + " select '" + point + "' , service, sp.name_supp, tarif_type, tarif "
                    + " from tmp_cursv t,"
                    + prefKernel + "services sv,"
                    + prefKernel + "supplier sp "
                    + " where t.nzp_supp=sp.nzp_supp "
                    + " and t.nzp_serv=sv.nzp_serv";
                ExecSQL(sql);

                ExecSQL("drop table t_cursv");
                ExecSQL("drop table tmp_cursv");
            }
            #endregion

            sql = " select rajon, name_supp, service, tarif_type, tarif" +
                  " from t_svod_71_1_15_1 " +
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
        /// 
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
            result = !String.IsNullOrEmpty(result) ? " AND t.nzp_serv in (" + result + ")" : String.Empty;
            if (!String.IsNullOrEmpty(result))
            {
                string sql = " SELECT service " +
                             " from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services t" +
                             " WHERE t.nzp_serv > 1 " + result;
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

            string sql = " CREATE TEMP TABLE t_svod_71_1_15_1( " +
                  " rajon char(100), " +
                  " name_supp char(100), " +
                  " service char(60), " +
                  " tarif_type integer, " +
                  " tarif  " + DBManager.sDecimalType + "(14,4) " +
                  " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_svod_71_1_15_1 ");
        }
    }
}
