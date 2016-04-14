using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Main.Properties;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Main.Reports
{
    class ReportChanges : BaseSqlReport
    {
        public override string Name
        {
            get { return "Базовый Отчет по изменениям"; }
        }

        public override string Description
        {
            get { return "Отчет по изменениям"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Cards };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_changes; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.LC }; }
        }

        /// <summary>Дата с</summary>
        protected DateTime BeginDate { get; set; }

        /// <summary>Дата по</summary>
        protected DateTime EndDate { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Банки данных</summary>
        protected string Reason { get; set; }



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
                new SupplierParameter(),     
                new StringParameter { Code = "Reason", Name = "Основание" }
            };
        }

        public override DataSet GetData()
        {
            MyDataReader reader;
            var sql = " select pref " +
                      " from  " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar " +
                      " where nzp_kvar=" + ReportParams.NzpObject + GetwhereWp();
            ExecRead(out reader, sql);

            if (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();
                
                int prmIterator = 1;//выбор всех параметров для ЛС
                while (prmIterator <= 20)
                {
                    string prmTable = pref + DBManager.sDataAliasRest + "prm_" + prmIterator;
                    if (TempTableInWebCashe(prmTable))
                    {
                        sql =
                            " insert into t_prm (nzp_key, nzp, nzp_prm, dat_s, dat_po, is_actual, val_prm, dat_when) " +
                            " select nzp_key, nzp, nzp_prm, dat_s, dat_po, is_actual, val_prm, dat_when " +
                            " from " + prmTable +
                            " where dat_s <= '" + EndDate.ToShortDateString() + "' " +
                            " and dat_po >= '" + BeginDate.ToShortDateString() + "' " +
                            " and nzp = " + ReportParams.NzpObject;
                        ExecSQL(sql);
                    }
                    prmIterator++;
                }

                ExecSQL(" update t_prm set val_prm = '0' where is_actual = 100 ");
                //выбор параметров которых больше одного
                sql = " insert into t_prm_changed (nzp_key, nzp, nzp_prm, dat_s, dat_po, is_actual, val_prm, dat_when) " + 
                      " select nzp_key, nzp, nzp_prm, dat_s, dat_po, is_actual, val_prm, dat_when " +
                      " from t_prm " +
                      " where 1 < (select distinct count(nzp_prm) from t_prm t where t.nzp_prm = t_prm.nzp_prm) ";
                ExecSQL(sql);
                //старые значения параметров
                sql = " insert into t_prm_min (nzp_key, nzp, nzp_prm, dat_s, dat_po, is_actual, val_prm, dat_when) " +
                      " select nzp_key, nzp, nzp_prm, dat_s, dat_po, is_actual, val_prm, dat_when " +
                      " from t_prm_changed " +
                      " where dat_po in (select distinct min(dat_po) from t_prm t where t.nzp_prm = t_prm_changed.nzp_prm) ";
                ExecSQL(sql);
                //новые значения параметров
                sql = " insert into t_prm_max (nzp_key, nzp, nzp_prm, dat_s, dat_po, is_actual, val_prm, dat_when) " +
                      " select nzp_key, nzp, nzp_prm, dat_s, dat_po, is_actual, val_prm, dat_when " +
                      " from t_prm_changed " +
                      " where dat_po in (select distinct max(dat_po) from t_prm t where t.nzp_prm = t_prm_changed.nzp_prm) ";
                ExecSQL(sql);

                sql = " insert into t_changes (date_change, changes, changes_old, changes_now, changes_minus, changes_plus) " +
                      " select distinct " +
                      " n.dat_s," +
                      " pn.name_prm, " +
                      " p.val_prm, " +
                      " n.val_prm, " +
                      " case when p.val_prm " + DBManager.sMatchesWord + " '" + 
                      DBManager.sRegularExpressionAnySymbol +"[0-9]" + 
                      DBManager.sRegularExpressionAnySymbol +
                      "' and n.val_prm " + DBManager.sMatchesWord + " '" +
                      DBManager.sRegularExpressionAnySymbol + "[0-9]" +
                      DBManager.sRegularExpressionAnySymbol +
                      "' then (case when p.val_prm > n.val_prm then round(p.val_prm" +
                      DBManager.sConvToNum + " - n.val_prm" + DBManager.sConvToNum + ",2) " +
                      " else 0 end) else 0 end, " +
                      " case when p.val_prm " + DBManager.sMatchesWord + " '" + 
                      DBManager.sRegularExpressionAnySymbol + "[0-9]" +
                      DBManager.sRegularExpressionAnySymbol +
                      "' and n.val_prm " + DBManager.sMatchesWord + " '" +
                      DBManager.sRegularExpressionAnySymbol + "[0-9]" +
                      DBManager.sRegularExpressionAnySymbol +
                      "' then (case when p.val_prm < n.val_prm then round(n.val_prm" + 
                      DBManager.sConvToNum + " - p.val_prm" + DBManager.sConvToNum + ",2) " + 
                      " else 0 end) else 0 end " +
                      " from t_prm_min p, " +
                      " t_prm_max n, " +
                        ReportParams.Pref + DBManager.sKernelAliasRest + "prm_name pn " +
                      " where p.nzp_prm = n.nzp_prm and pn.nzp_prm = p.nzp_prm ";
                ExecSQL(sql);

                sql = " insert into t_prm_unchanged (nzp_key, nzp, nzp_prm, dat_s, dat_po, is_actual, val_prm, dat_when) " +
                      " select nzp_key, nzp, nzp_prm, dat_s, dat_po, is_actual, val_prm, dat_when " +
                      " from t_prm " +
                      " where nzp_prm not in (select distinct nzp_prm from t_prm_changed) ";
                ExecSQL(sql);

                sql = " insert into t_changes (changes, changes_old, changes_now, changes_minus, changes_plus) " +
                      " select " +
                      " pn.name_prm, " +
                      " p.val_prm, " +
                      " p.val_prm, " +
                      " 0 , " +
                      " 0 " +
                      " from t_prm_unchanged p, " +
                        ReportParams.Pref + DBManager.sKernelAliasRest + "prm_name pn " +
                      " where pn.nzp_prm = p.nzp_prm ";
                ExecSQL(sql);


                sql = " update t_changes set num_ls = (select num_ls from " + pref + DBManager.sDataAliasRest +
                      "kvar where nzp_kvar = " + ReportParams.NzpObject + ") ";
                ExecSQL(sql);
                sql = " update t_changes set fio = (select fio from " + pref + DBManager.sDataAliasRest + "kvar where nzp_kvar = " +
                      ReportParams.NzpObject + ")";
                ExecSQL(sql);
                sql = " update t_changes set adr = (select " +
                      " case when rajon = '-' then trim(town) else trim(rajon) end || " +
                      " ' / ' || trim(ulica) || ' д.' || trim(ndom) || " +
                      " case when nkor <> '-' then trim(nkor) else '' end || ' кв.' || " +
                      " case when nkvar <> '0' and nkvar <> '-' then trim(nkvar) else '' end " +
                      " from " +
                      pref + DBManager.sDataAliasRest + "s_town t, " +
                      pref + DBManager.sDataAliasRest + "s_rajon r, " +
                      pref + DBManager.sDataAliasRest + "s_ulica u, " +
                      pref + DBManager.sDataAliasRest + "dom d, " +
                      pref + DBManager.sDataAliasRest + "kvar k " +
                      " where k.nzp_dom = d.nzp_dom " +
                      " and d.nzp_ul = u.nzp_ul " +
                      " and u.nzp_raj = r.nzp_raj " +
                      " and r.nzp_town = t.nzp_town " +
                      " and k.nzp_kvar = " + ReportParams.NzpObject + ") ";
                ExecSQL(sql);
                ExecSQL(" update t_changes set reason = '" + Reason + "' ");
                ExecSQL(" update t_changes set oper = '" + ReportParams.User.uname + "' ");
            }
            reader.Close();

            var dt = ExecSQLToTable(
                " select " +
                " date_change, " +
                " num_ls, " +
                " adr, " +
                " fio, " +
                " name_supp, " +
                " changes, " +
                " changes_old, " +
                " changes_now, " +
                " changes_minus, " +
                " changes_plus, " +
                " reason, " +
                " oper " +
                " from t_changes " +
                " order by date_change ");
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }


        private string GetwhereWp()
        {
            string whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            return whereWp;
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
            report.SetParameterValue("date", DateTime.Now.ToLongDateString());
            report.SetParameterValue("dats", BeginDate.ToShortDateString());
            report.SetParameterValue("datpo", EndDate.ToShortDateString());
            report.SetParameterValue("oper_name", ReportParams.User.uname);

            var oper =
                ExecSQLToTable(" select val_prm from " + ReportParams.Pref + DBManager.sDataAliasRest +
                               " prm_10 where nzp_prm = 80 and is_actual = 1 " +
                               " and dat_s <= '" + DateTime.Now.ToShortDateString() +
                               "' and dat_po >= '" + DateTime.Now.ToShortDateString() + "' ");
            report.SetParameterValue("oper", "Оператор " + (oper.Rows.Count == 1 ? oper.Rows[0][0].ToString().Trim() : "") + "__________________");
        }

        protected override void PrepareParams()
        {
            var period = UserParamValues["Period"].GetValue<string>();
            DateTime d1;
            DateTime d2;
            PeriodParameter.GetValues(period, out d1, out d2);
            BeginDate = d1;
            EndDate = d2;
            Reason = UserParamValues["Reason"].GetValue<string>();
            Suppliers = UserParamValues["Suppliers"].GetValue<List<long>>();
        }

        protected override void CreateTempTable()
        {
            string sql = "create temp table t_changes ( " +
                               " date_change date, " +
                               " num_ls integer, " +
                               " adr character(100), " +
                               " fio character(100), " +
                               " name_supp character(100), " +
                               " changes character(100), " +
                               " changes_old character(20), " +
                               " changes_now character(20), " +
                               " changes_minus character(20), " +
                               " changes_plus character(20), " +
                               " reason character(100), " +
                               " oper character(100)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            ExecSQL(" create temp table t_prm (nzp_key integer, nzp integer, nzp_prm integer, dat_s date, dat_po date, is_actual integer, val_prm character(100), dat_when date) " + DBManager.sUnlogTempTable);
            ExecSQL(" create temp table t_prm_changed (nzp_key integer, nzp integer, nzp_prm integer, dat_s date, dat_po date, is_actual integer, val_prm character(100), dat_when date) " + DBManager.sUnlogTempTable);
            ExecSQL(" create temp table t_prm_min (nzp_key integer, nzp integer, nzp_prm integer, dat_s date, dat_po date, is_actual integer, val_prm character(100), dat_when date) " + DBManager.sUnlogTempTable);
            ExecSQL(" create temp table t_prm_max (nzp_key integer, nzp integer, nzp_prm integer, dat_s date, dat_po date, is_actual integer, val_prm character(100), dat_when date) " + DBManager.sUnlogTempTable);
            ExecSQL(" create temp table t_prm_unchanged (nzp_key integer, nzp integer, nzp_prm integer, dat_s date, dat_po date, is_actual integer, val_prm character(100), dat_when date) " + DBManager.sUnlogTempTable);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_changes ", true);
            ExecSQL(" drop table t_prm ", true);
            ExecSQL(" drop table t_prm_changed ", true);
            ExecSQL(" drop table t_prm_unchanged ", true);
            ExecSQL(" drop table t_prm_min ", true);
            ExecSQL(" drop table t_prm_max ", true);
        }

    }
}
