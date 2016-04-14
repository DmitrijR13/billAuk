using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Castle.Core.Internal;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Samara.Properties;
using Bars.KP50.Utils;
using Castle.Windsor.Installer;
using STCLINE.KP50.DataBase;
using Constants = STCLINE.KP50.Global.Constants;

namespace Bars.KP50.Report.Samara.Reports
{
    class Report3001017 : BaseSqlReport
    {
        public override string Name
        {
            get { return "30.1.17 Информация по домам"; }
        }

        public override string Description
        {
            get { return "Информация по домам"; }
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
            get { return Resources.Report_30_1_17; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
        }



        /// <summary>Районы</summary>
        private string Raions { get; set; }

        /// <summary>Улицы</summary>
        private string Streets { get; set; }

        /// <summary>Дома</summary>
        private string Houses { get; set; }

        /// <summary>Расчетный месяц</summary>
        private int Month { get; set; }

        /// <summary>Расчетный год</summary>
        private int Year { get; set; }

        /// <summary> Статус лицевого счета </summary>
        private int statusLs { get; set; }

        /// <summary> Признак параметра снятия с баланса дома </summary>
        private byte IsActual { get; set; }

        public override List<UserParam> GetUserParams()
        {

            return new List<UserParam>
            {
                new MonthParameter {Value = DateTime.Today.Month },
                new YearParameter {Value = DateTime.Today.Year },
                new AddressParameter(),
                new ComboBoxParameter(false)
                {
                    Name = "Статус ЛС", 
                    Code = "Filter",
                    Value = 1,
                    Require = true,
                    StoreData = new List<object> {
                        new { Id = 4, Name = "Все"},
                        new { Id = 1, Name = "Открытые"},
                        new { Id = 2, Name = "Закрытые"}
                    }
                },
                new ComboBoxParameter(false)
                {
                    Name = "Учитывать дома снятые с баланса",
                    Code = "isActual",
                    Value = 2,
                    Require = true,
                    StoreData = new List<object>
                    {
                        new { Id = 1, Name = "Да"},
                        new { Id = 2, Name = "Нет"}
                    }
                    
                }
                
            };
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();

            IsActual = UserParamValues["isActual"].GetValue<byte>();

            var adr = UserParamValues["Address"].GetValue<AddressParameterValue>();

            statusLs = UserParamValues["Filter"].GetValue<int>();

            if (!adr.Raions.IsNullOrEmpty())
            {
                Raions = adr.Raions.Aggregate(Raions, (current, nzpRajon) => current + (nzpRajon + ","));
                Raions = Raions.TrimEnd(',');
                Raions = "and u.nzp_raj in (" + Raions + ") ";
            }
            else return;


            if (!adr.Streets.IsNullOrEmpty())
            {
                Streets = adr.Streets.Aggregate(Streets, (current, nzpStreet) => current + (nzpStreet + ","));
                Streets = Streets.TrimEnd(',');
                Streets = "and u.nzp_ul in (" + Streets + ") ";
            }
            else return;

            if (!adr.Houses.IsNullOrEmpty())
            {
                List<string> goodHouses = adr.Houses.FindAll(x => x.Trim() != "" && x.Contains("'") == false);
                if (!goodHouses.IsNullOrEmpty())
                    Houses = "and d.ndom in (" + String.Join(",", goodHouses.Select(x => "'" + x + "'").ToArray()) + ") ";
            }
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};

            report.SetParameterValue("month", months[Month]);
            report.SetParameterValue("year", Year);

        }


        public void MakeSelectedKvar()
        {
            string sql;
            if (ReportParams.CurrentReportKind == ReportKind.ListLC)
            {
                var tSpls = WebBase + DBManager.tableDelimiter + "t" + ReportParams.User.nzp_user + "_spls";

                sql = " insert into selected_kvar" +
                             " select nzp_kvar " +
                             " from " + tSpls;

            }
            else
            {


                sql = " insert into selected_kvar" +
                             " select nzp_kvar " +
                             " from " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar k," +
                             ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                             ReportParams.Pref + DBManager.sDataAliasRest + "dom d " +
                             " where k.nzp_dom=d.nzp_dom and d.nzp_ul=u.nzp_ul " +
                             Raions + Streets + Houses;
            }
            ExecSQL(sql);



            ExecSQL(" create index ix_tmp_ls_01 on selected_kvar(nzp_kvar)");
            ExecSQL(DBManager.sUpdStat + " selected_kvar");
        }

        public override DataSet GetData()
        {


            #region выборка в temp таблицу

            MakeSelectedKvar();

            MyDataReader reader;

            string sql = " SELECT bd_kernel AS pref " +
                         " FROM " + DBManager.GetFullBaseName(Connection) + DBManager.tableDelimiter + "s_point " +
                         " WHERE nzp_wp>1 " + GetwhereWp();

            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();


                sql = " CREATE TEMP TABLE  t_kv(" +
                      " nzp_kvar integer, " +
                      " nzp_dom integer, " +
                      " is_actual INTEGER default 0, " +
                      " et integer default 0, " +
                      " is_open integer default 0, " +
                      " is_priv integer default 0, " +
                      " pl_kvar " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable;
                ExecSQL(sql);

                sql = " INSERT INTO t_kv (nzp_kvar, nzp_dom)" +
                      " SELECT a.nzp_kvar, a.nzp_dom " +
                      " FROM " + pref + DBManager.sDataAliasRest + "kvar a," +
                      "         selected_kvar b" +
                      " WHERE a.nzp_kvar=b.nzp_kvar";
                ExecSQL(sql);

                ExecSQL("CREATE INDEX ix_tmp_01 ON t_kv(nzp_kvar)");
                ExecSQL(DBManager.sUpdStat + " t_kv");

                sql = " UPDATE t_kv SET is_priv = 1 " +
                      " WHERE nzp_kvar IN (SELECT nzp " +
                      "     FROM " + pref + DBManager.sDataAliasRest + "prm_1 " +
                      "     WHERE nzp_prm=8 and val_prm='1' " +
                      "         AND is_actual=1 " +
                      "         AND dat_s<=date('01." + Month + "." + Year + "')" +
                      "         AND dat_po>=date('01." + Month + "." + Year + "'))";
                ExecSQL(sql);

                sql = " UPDATE t_kv SET is_open = 1 " +
                      " WHERE nzp_kvar IN (SELECT nzp " +
                      "         FROM " + pref + DBManager.sDataAliasRest + "prm_3 " +
                      "         WHERE nzp_prm=51 " +
                      "             AND val_prm='" + statusLs + "' " +
                      "             AND is_actual=1 " +
                      "             AND dat_s<=date('01." + Month + "." + Year + "')" +
                      "             AND dat_po>=date('01." + Month + "." + Year + "'))";
                ExecSQL(sql);

                sql = " UPDATE t_kv SET pl_kvar = (SELECT sum(val_prm" + DBManager.sConvToNum + ") " +
                      " FROM " + pref + DBManager.sDataAliasRest + "prm_1 p " +
                      " WHERE t_kv.nzp_kvar=p.nzp " +
                      "       AND p.nzp_prm=4 and p.is_actual=1 " +
                      "       AND p.dat_s<=date('01." + Month + "." + Year + "')" +
                      "       AND p.dat_po>=date('01." + Month + "." + Year + "'))";
                ExecSQL(sql);

                sql = " UPDATE t_kv SET et = (SELECT sum(val_prm" + DBManager.sConvToNum + ") " +
                      " FROM " + pref + DBManager.sDataAliasRest + "prm_2 p " +
                      " WHERE t_kv.nzp_dom=p.nzp " +
                      "       AND p.nzp_prm=37 and p.is_actual=1 " +
                      "       AND p.dat_s<=date('01." + Month + "." + Year + "')" +
                      "       AND p.dat_po>=date('01." + Month + "." + Year + "'))";
                ExecSQL(sql);

                sql = " UPDATE t_kv SET is_actual = (CASE WHEN ( SELECT COUNT(nzp) " +
                      " FROM " + pref + DBManager.sDataAliasRest + "prm_2 p " +
                      " WHERE t_kv.nzp_dom=p.nzp " +
                      "       AND p.nzp_prm=2029 and p.is_actual=1 " +
                      "       AND p.dat_s<=date('01." + Month + "." + Year + "')" +
                      "       AND date(val_prm) <= date('01." + Month + "." + Year + "')" +
                      "       AND p.dat_po>=date('01." + Month + "." + Year + "')) > 0 THEN 1 ELSE 0 END)";
                ExecSQL(sql);

                sql = " INSERT INTO t_svod(nzp_dom, et, count_priv_ls, count_npriv_ls, pl_priv,  pl_npriv) " +
                      " SELECT  nzp_dom, et,sum(case when is_priv = 1 then 1 else 0 end)," +
                      "     sum(case when is_priv = 1 then 0 else 1 end), " +
                      "     sum(case when is_priv = 1 then pl_kvar else 0 end)," +
                      "     sum(case when is_priv = 1 then 0 else pl_kvar end)" +
                      " FROM t_kv";
                if (statusLs != 4)
                    if (IsActual == 2) sql += " WHERE is_open = 1 AND is_actual = 0 ";
                    else sql += " WHERE is_open = 1 ";
                else if (IsActual == 2) sql += " WHERE is_actual = 0 ";
                sql += " GROUP BY 1,2";
                ExecSQL(sql);
                ExecSQL("drop table t_kv");
            }

            reader.Close();
            #endregion

            sql = " SELECT town, CASE WHEN rajon='-' THEN town ELSE rajon END as rajon, " +
                  " ulica, ndom, " +
                  " CASE WHEN nkor<>'-' THEN nkor END as nkor, idom, count_priv_ls, count_npriv_ls," +
                  " pl_priv,  pl_npriv, et,  count_priv_ls + count_npriv_ls as count_ls, " +
                  " pl_priv + pl_npriv as pl_all" +
                  " FROM t_svod tr, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_town t, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                  ReportParams.Pref + DBManager.sDataAliasRest + "dom d " +
                  " WHERE tr.nzp_dom = d.nzp_dom " +
                  "     AND d.nzp_ul = u.nzp_ul " +
                  "     AND u.nzp_raj = r.nzp_raj " +
                  "     AND r.nzp_town = t.nzp_town " +
                  " ORDER BY 5,2,3,4,1,6 ";
            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }



        /// <summary>
        /// Получить условия органичения по банкам данных
        /// </summary>
        /// <returns></returns>
        private string GetwhereWp()
        {
            string whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            return whereWp;
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_svod ( " +
                              " nzp_dom integer, " +
                              " et integer, " +
                              " count_priv_ls integer," +
                              " count_npriv_ls integer," +
                              " pl_priv " + DBManager.sDecimalType + "(14,2),  " +
                              " pl_npriv " + DBManager.sDecimalType + "(14,2)) " +
                              DBManager.sUnlogTempTable;
            ExecSQL(sql);
            sql = " CREATE TEMP TABLE selected_kvar ( " +
                  " nzp_kvar integer) " +
                  DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_svod ");
            ExecSQL(" DROP TABLE selected_kvar ");
            try
            {
                ExecSQL(" DROP TABLE t_kv ", false);
            }
            catch
            {
            }
        }
    }
}
