using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Samara.Properties;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Samara.Reports
{
    class Report3001011 : BaseSqlReport
    {
        public override string Name
        {
            get { return "30.1.11 Ведомость оплаты за коммунальные услуги"; }
        }

        public override string Description
        {
            get { return "30.1.11 Ведомость оплаты за коммунальные услуги"; }
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
            get { return Resources.Report_30_1_11; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }


        /// <summary>Дата с</summary>
        protected DateTime DatS { get; set; }

        /// <summary>Дата по</summary>
        protected DateTime DatPo { get; set; }

        /// <summary>Адрес</summary>
        protected AddressParameterValue Address { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AreaHeader { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string RajonHeader { get; set; }

        public override List<UserParam> GetUserParams()
        {
         
            DateTime datS =  DateTime.Now;
            DateTime datPo = DateTime.Now;
            return new List<UserParam>
            {
                new PeriodParameter(datS, datPo),
                new AddressParameter(),
                new AreaParameter()
            };
        }

        protected override void PrepareParams()
        {
            var period = UserParamValues["Period"].GetValue<string>();
            DateTime d1;
            DateTime d2;
            PeriodParameter.GetValues(period, out d1, out d2);
            DatS = d1;
            DatPo = d2;

            Address = UserParamValues["Address"].GetValue<AddressParameterValue>();
            Areas = UserParamValues["Areas"].Value.To<List<int>>();
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            string period;

            AreaHeader = AreaHeader != null ? "Балансодержатель: " + AreaHeader : AreaHeader;
            report.SetParameterValue("area", AreaHeader);
            report.SetParameterValue("town", RajonHeader);
            report.SetParameterValue("ercName", GetErcName());
            
            
            if (DatS == DatPo)
            {
                period = DatS.ToShortDateString() + " г.";
            }
            else
            {
                period = "период с " + DatS.ToShortDateString() + " г. по " + DatPo.ToShortDateString() + " г.";
            }
            report.SetParameterValue("period", period);
        }

        public override DataSet GetData()
        {
            string whereArea = GetWhereArea();
            string whereRajon = GetWhereAddress("Район");
            string whereUlica = GetWhereAddress("Улица");
            string whereDom = GetWhereAddress("Дом");

            #region выборка в temp таблицу

            string sql;

                string packTable = ReportParams.Pref + "_fin_" + (DateTime.Now.Year - 2000).ToString("00") + DBManager.tableDelimiter + "pack",
                        packLsTable = ReportParams.Pref + "_fin_" + (DateTime.Now.Year - 2000).ToString("00") + DBManager.tableDelimiter + "pack_ls";
                string prefData = ReportParams.Pref + DBManager.sDataAliasRest,
                        prefKernel = ReportParams.Pref + DBManager.sKernelAliasRest;

                if (TempTableInWebCashe(packTable) && TempTableInWebCashe(packLsTable))
                { 
                    sql = " INSERT INTO t_vedom_plat_zausl ( dat_pack, num_pack, geu, num_ls, town, rajon, ulica, ndom, " +
                                                            " nkor, nkvar, fio, dat_vvod, dat_month, g_sum_ls, bank, pkod) " +
                      " SELECT p.dat_pack, " +
                             " p.num_pack, " +
                             " g.geu, " +
                             " kv.num_ls, " +
                             " t.town, " +
                             " r.rajon, " +
                             " u.ulica, " +
                             " d.ndom, " +
                             " d.nkor, " +
                             " kv.nkvar, " +
                             " kv.fio, " +
                             " pl.dat_vvod, " +
                             " pl.dat_month, " +
                             " pl.g_sum_ls, " +
                             " b.bank, " +
                             " SUBSTR(CAST(kv.pkod AS CHARACTER(20)),1,3) AS pkod " +
                      " FROM " + packTable + " p INNER JOIN " + packLsTable + " pl       ON p.nzp_pack = pl.nzp_pack " +
                                               " INNER JOIN " + prefData + "kvar kv      ON kv.num_ls = pl.num_ls " +
                                               " INNER JOIN " + prefData + "dom d        ON kv.nzp_dom=d.nzp_dom " +
                                               " INNER JOIN " + prefData + "s_ulica u    ON d.nzp_ul=u.nzp_ul " +
                                               " INNER JOIN " + prefData + "s_rajon r    ON u.nzp_raj=r.nzp_raj " +
                                               " INNER JOIN " + prefData + "s_town t     ON r.nzp_town=t.nzp_town " +
                                               " INNER JOIN " + prefData + "s_geu g      ON kv.nzp_geu=g.nzp_geu " +
                                               " LEFT OUTER JOIN " + prefKernel + "s_bank b   ON p.nzp_bank=b.nzp_bank " +
                      " WHERE pl.dat_vvod BETWEEN '" + DatS.ToShortDateString() + "' AND '" + DatPo.ToShortDateString() + "' " +
                          whereArea + whereRajon + whereUlica + whereDom + GetWhereSupp();
                    ExecSQL(sql);
                }
                
            #endregion

            sql = " SELECT dat_pack, num_pack, geu, num_ls, town, rajon, ulica, ndom, " +
                      " nkor, nkvar, fio, dat_vvod, dat_month, g_sum_ls, bank, pkod " +
                  " FROM t_vedom_plat_zausl " +
                  " ORDER BY 1,2,town,rajon, ulica, ndom, nkvar ";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        /// <summary>
        /// Получить условия органичения по городу
        /// </summary>
        /// <returns></returns>
        private string GetWhereAddress(string item)
        {
            string where = string.Empty;

                switch (item)
                {
                    case "Район":
                        if (Address.Raions != null)
                        {
                            where = Address.Raions.Aggregate(where,
                                (current, nzpRajon) => current + (nzpRajon + ","));
                            where = where.TrimEnd(',');
                            where = string.IsNullOrEmpty(where)
                                ? string.Empty
                                : " AND r.nzp_raj IN (" + where + ") ";
                            RajonHeader = GetRajonHeader(where);
                        }
                    
                        break;
                    case "Улица":
                    if (Address.Streets != null)
                        {
                        where = Address.Streets.Aggregate(where, (current, nzpStreet) => current + (nzpStreet + ","));
                        where = where.TrimEnd(',');
                        where = string.IsNullOrEmpty(where) ? string.Empty : " AND u.nzp_ul IN (" + where + ") ";
                        }
                        break;
                    case "Дом":
                    if (Address.Houses != null)
                            {
                        where = Address.Houses.Aggregate(where, (current, nzpHouse) => current + (nzpHouse + ","));
                        where = where.TrimEnd(',');
                        where = string.IsNullOrEmpty(where) ? string.Empty : " AND d.nzp_dom IN (" + where + ") ";
                        }
                        break;
                }
            return where;
        }


        /// <summary>
        /// Получение наименования районов
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        private string GetRajonHeader(string where)
        {
            string result = String.Empty;
            string sql = " select town " +
                         " from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_town t," +
                         ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r " +
                         " where t.nzp_town=r.nzp_town " + @where;
            using (DataTable dr = ExecSQLToTable(sql))
            {
                result = dr.Rows.Cast<DataRow>().Aggregate(result, (current, row) => current + (row["town"].ToString().Trim() + " ,"));
                result = result.TrimEnd(',');
            }
            return result;
        }

        /// <summary>
        /// Определение наименования расчетного центра
        /// </summary>
        /// <returns></returns>
        private string GetWhereArea()
        {
            string whereArea = String.Empty;
            whereArea = Areas != null ? Areas.Aggregate(whereArea, (current, nzpArea) => current + (nzpArea + ",")) : 
                                            ReportParams.GetRolesCondition(Constants.role_sql_area);
            whereArea = whereArea.TrimEnd(',');
            whereArea = !String.IsNullOrEmpty(whereArea) ? " AND kv.nzp_area in (" + whereArea + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereArea))
            {
                string sql = " SELECT area from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area kv  WHERE kv.nzp_area > 0 " + whereArea;
                DataTable area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    AreaHeader += dr["area"].ToString().Trim() + ", ";
                }
                AreaHeader = AreaHeader.TrimEnd(',', ' ');
            }
            return whereArea;
        }


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

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp()
        {
            string whereSupp = String.Empty;
            whereSupp = Suppliers != null ? Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ",")) : 
                                                ReportParams.GetRolesCondition(Constants.role_sql_supp);
            whereSupp = whereSupp.TrimEnd(',');
            whereSupp = !String.IsNullOrEmpty(whereSupp) ? " AND p.nzp_supp in (" + whereSupp + ")" : String.Empty;
            return whereSupp;
        }

        protected override void CreateTempTable()
        {
            const string sql = " CREATE TEMP TABLE t_vedom_plat_zausl( " +
                         " dat_pack DATE, " +
                         " num_pack CHARACTER(10), " +
                         " geu CHARACTER(60), " +
                         " num_ls INTEGER, " +
                         " town CHARACTER(30), " +
                         " rajon CHARACTER(30), " +
                         " ulica CHARACTER(40), " +
                         " ndom CHARACTER(15), " +
                         " nkor CHARACTER(15), " +
                         " nkvar CHARACTER(10), " +
                         " fio CHARACTER(40), " +
                         " dat_vvod DATE, " +
                         " dat_month DATE, " +
                         " g_sum_ls " + DBManager.sDecimalType + "(14,2), " +
                         " bank CHARACTER(30), " +
                         " pkod CHARACTER(3)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_vedom_plat_zausl ");
        }
    }
}
