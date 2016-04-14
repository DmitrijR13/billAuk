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

using System.Globalization;

using System.Text;

//using Castle.Core.Internal;
using Constants = STCLINE.KP50.Global.Constants;

namespace Bars.KP50.Report.Tula.Reports
{
    class Report71001015_3 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.1.15.3 Отчет для сверки загруженных данных в разрезе нормативов, тарифов и услуг"; }
        }

        public override string Description
        {
            get { return "71.1.15.3 Отчет для сверки загруженных данных в разрезе нормативов, тарифов и услуг"; }
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
            get { return Resources.Report_71_1_15_3; }
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
        /// <summary>Адрес</summary>
        protected AddressParameterValue Address { get; set; }
        /// <summary>Район</summary>
        private string Raions { get; set; }
        /// <summary>Улица</summary>
        private string Streets { get; set; }
        /// <summary>Дом</summary>
        private string Houses { get; set; }
        /// <summary>Дата</summary>     
        protected string Dat { get; set; }
        /// <summary>Дата</summary>     
        protected DateTime dat { get; set; }

        public override List<UserParam> GetUserParams()
        {            
            return new List<UserParam>
            {       
                new SupplierAndBankParameter(),
                new AddressParameter(),
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

            Address = UserParamValues["Address"].GetValue<AddressParameterValue>();
            if (Address.Raions != null)
            {
                Raions = String.Join(",", Address.Raions.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray());
                Raions = "and u.nzp_raj in (" + Raions + ") ";
            }
            if (Address.Streets != null)
            {
                Streets = String.Join(",", Address.Streets.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray());
                Streets = "and u.nzp_ul in (" + Streets + ") ";
            }
            if (Address.Houses != null)
            {
                List<string> goodHouses = Address.Houses.FindAll(x => x.Trim() != "" && x.Contains("'") == false);
                if (goodHouses.IsNotNull() && goodHouses.IsNotEmpty())
                    Houses = "and k.nzp_dom in (" + String.Join(",", goodHouses.Select(x => "'" + x + "'").ToArray()) + ") ";
            }    
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
                      " nzp_supp integer," +
                      " nzp_serv integer," +
                      " nzp_dom integer," +
                      " num_ls integer," +
                      " nzp_kvar integer," +
                      " count_ls integer, " +
                      " norm  " + DBManager.sDecimalType + "(14,4) ," +
                      " nzp_prm_tarif_ls integer," +
                      " nzp_prm_tarif_dm integer," +
                      " nzp_prm_tarif_su integer," +
                      " nzp_prm_tarif_bd integer)";
                ExecSQL(sql);

                sql = " Create temp table tmp_cursv( " +
                      " nzp_supp integer," +
                      " nzp_serv integer," +
                      " nzp_dom integer," +
                      " nzp_kvar integer," +
                      " count_ls integer, " +
                      " tarif_type integer," +
                      " norm  " + DBManager.sDecimalType + "(14,4) ,"+
                      " tarif  " + DBManager.sDecimalType + "(14,4) )";
                ExecSQL(sql);


                sql = " insert into t_cursv(nzp_serv, nzp_supp, nzp_dom,num_ls,  nzp_prm_tarif_ls, nzp_prm_tarif_dm, nzp_prm_tarif_su, nzp_prm_tarif_bd, norm )" +
                      " select t.nzp_serv, t.nzp_supp, k.nzp_dom, t.num_ls,  nzp_prm_tarif_ls, nzp_prm_tarif_dm, nzp_prm_tarif_su, nzp_prm_tarif_bd, rash_norm_one " +
                      " from " + prefData + "tarif t, " + prefKernel + "formuls_opis f, "+prefData+"kvar k, "+gkuTable+" g"
                      + " where is_actual =1 and g.stek = 3 and f.nzp_frm=t.nzp_frm and k.nzp_kvar=t.nzp_kvar   "  
                      + Houses
                      + GetWhereSupp()
                      + GetWhereServ()
                      + " and t.dat_s<= '" + Dat + "'"
                      + " and t.dat_po>='" + Dat + "'"
                      + " and g.nzp_supp=t.nzp_supp and g.nzp_serv=t.nzp_serv and k.nzp_kvar=g.nzp_kvar "  //and f.nzp_frm=g.nzp_frm
                      + " group by 1,2,3,4,5,6,7,8,9";
                ExecSQL(sql);

                ExecSQL("create index ix_t_cursv01 on t_cursv(nzp_supp)");
                ExecSQL("create index ix_t_cursv02 on t_cursv(nzp_serv)");
                ExecSQL("create index ix_t_cursv03 on t_cursv(nzp_prm_tarif_ls)");
                ExecSQL("create index ix_t_cursv04 on t_cursv(nzp_prm_tarif_dm)");
                ExecSQL("create index ix_t_cursv05 on t_cursv(nzp_prm_tarif_su)");
                ExecSQL("create index ix_t_cursv06 on t_cursv(nzp_prm_tarif_bd)");

                ExecSQL(DBManager.sUpdStat + " t_cursv");  
                

                sql = " insert into tmp_cursv( nzp_serv, nzp_supp, nzp_dom, norm,tarif_type, tarif, count_ls)"
                      + " select  t.nzp_serv, t.nzp_supp, t.nzp_dom,norm, 0,  val_prm" + DBManager.sConvToNum + ", count(num_ls) "
                      + " from t_cursv t, " + prefData+"prm_1"
                      + " where is_actual=1 "
                      + " and dat_s<= '" + Dat + "'"
                      + " and dat_po>='" + Dat + "'"
                      + " and nzp_prm=nzp_prm_tarif_ls" 
                      + " and nzp = t.num_ls" 
                      + " group by 1,2,3,4,5,6";
                ExecSQL(sql);

                sql = " insert into tmp_cursv( nzp_serv, nzp_supp, nzp_dom, norm,tarif_type, tarif, count_ls)"
                      + " select  t.nzp_serv, t.nzp_supp, t.nzp_dom,norm, 1,  val_prm" + DBManager.sConvToNum +
                      ", count(num_ls) "
                      + " from t_cursv t, " + prefData + "prm_2"
                      + " where is_actual=1 "
                      + " and dat_s<= '" + Dat + "'"
                      + " and dat_po>='" + Dat + "'"
                      + " and nzp_prm=nzp_prm_tarif_ls"
                      + " and nzp = t.nzp_dom"
                      + " group by 1,2,3,4,5,6";
                ExecSQL(sql);

                sql = " insert into tmp_cursv( nzp_serv, nzp_supp, nzp_dom, norm, tarif_type, tarif, count_ls)"
                      + " select  t.nzp_serv, t.nzp_supp, t.nzp_dom,norm, 2,  val_prm" + DBManager.sConvToNum + ", count(num_ls) "
                      + " from t_cursv t, " + prefData + "prm_11"
                      + " where is_actual=1 "
                      + " and dat_s<= '" + Dat + "'"
                      + " and dat_po>='" + Dat + "'"
                      + " and nzp_prm=nzp_prm_tarif_su"
                      + " and nzp = t.nzp_supp"
                      + " group by 1,2,3,4,5,6";
                ExecSQL(sql);

                sql = " insert into tmp_cursv( nzp_serv, nzp_supp, nzp_dom, norm,tarif_type, tarif, count_ls)"
                      + " select  t.nzp_serv, t.nzp_supp, t.nzp_dom,norm, 3,  val_prm" + DBManager.sConvToNum + ", count(num_ls) "
                      + " from t_cursv t, " + prefData + "prm_5"
                      + " where is_actual=1 "
                      + " and dat_s<= '" + Dat + "'"
                      + " and dat_po>='" + Dat + "'"
                      + " and nzp_prm=nzp_prm_tarif_bd"
                      + " group by 1,2,3,4,5,6";
                ExecSQL(sql);              

                ExecSQL("create index ix_tmp_cursv01 on tmp_cursv(nzp_supp)");
                ExecSQL("create index ix_tmp_cursv02 on tmp_cursv(nzp_serv)");

                sql = " insert into t_svod_71_1_15_3( rajon, MO, service, name_supp, ulica, ulicareg, idom, ndom, nkor,norm, tarif_type,tarif, count_ls)"
                    + " select '" + point + "' , town, service, sp.name_supp,"
                    + " ulica, ulicareg,idom, ndom, nkor, norm "
                    + ", tarif_type, tarif, count_ls"
                    + " from tmp_cursv t, "
                    + prefKernel + "services sv,"
                    + prefKernel + "supplier sp, "
                    + prefData + "s_ulica u, "
                    + prefData + "s_town tw, "
                    + prefData + "dom d "
                    + " where t.nzp_supp=sp.nzp_supp "
                    + " and t.nzp_serv=sv.nzp_serv "
                    + " and tw.nzp_town=d.nzp_town "
                    + " and u.nzp_ul=d.nzp_ul "
                    + " and d.nzp_dom=t.nzp_dom ";
                ExecSQL(sql);

                ExecSQL("drop table t_cursv");
                ExecSQL("drop table tmp_cursv");
            }
            #endregion

            sql = " select rajon, MO, name_supp, service, ulica,ulicareg, idom, ndom, nkor, norm, tarif_type, tarif, count_ls " +
                  " from t_svod_71_1_15_3 " +
                  " order by rajon, MO, ulica,ulicareg, idom, ndom, nkor, name_supp, service, norm, tarif_type, tarif ";

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

            string sql = " CREATE TEMP TABLE t_svod_71_1_15_3( " +
                  " rajon char(100), " +
                  " MO char(100), " +
                  " dom char(100), " +
                  " name_supp char(100), " +
                  " service char(60), " +
                  " norm " + DBManager.sDecimalType + "(14,4), " +
                  " ulica char(60), " +
                  " ulicareg " + DBManager.sCharType + "(40), " +
                  " ndom char(10), " +
                  " nkor char(10), " +
                  " idom integer, " +
                  " count_ls integer, " +
                  " tarif_type integer, " +
                  " tarif  " + DBManager.sDecimalType + "(14,4) " +
                  " ) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);     
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_svod_71_1_15_3 ");
        }
    }
}
