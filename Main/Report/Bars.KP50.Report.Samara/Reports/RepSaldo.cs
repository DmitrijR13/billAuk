using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Bars.KP50.Report;
using Bars.KP50.Report.Base;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using Castle.MicroKernel.Registration;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

using System.Globalization;
//using Bars.KP50.Utils;
using Castle.Core.Internal;
using Constants = STCLINE.KP50.Global.Constants;
using Bars.KP50.Report.Samara.Properties;
using Bars.KP50.Utils;
using System.IO;
using System.Web;

namespace Bars.KP50.Report.Samara.Reports
{

    public class RetData
    {
        public void ReturnDBF(string strFilePath)
        {
            StreamWriter sw = new StreamWriter(@"C:\Temp\retDbf.txt", true);
            try
            {
                byte[] bts = File.ReadAllBytes(strFilePath);
                sw.WriteLine("4");
                System.Web.HttpContext.Current.Response.Clear();
                sw.WriteLine("5");
                System.Web.HttpContext.Current.Response.ClearHeaders();
                sw.WriteLine("6");
                System.Web.HttpContext.Current.Response.AddHeader("Content-Type", "application/x-msdownload");
                sw.WriteLine("7");
                System.Web.HttpContext.Current.Response.AddHeader("Content-Length", bts.Length.ToString());
                sw.WriteLine("8");
                sw.WriteLine(bts.Length.ToString());
                System.Web.HttpContext.Current.Response.AddHeader("Content-Disposition", "attachment;filename=Report.DBF");
                sw.WriteLine("9");
                System.Web.HttpContext.Current.Response.BinaryWrite(bts);
                System.Web.HttpContext.Current.Response.Flush();
                System.Web.HttpContext.Current.Response.End();
            }
            catch (Exception e)
            {
                sw.WriteLine(e.ToString());
            }
            sw.Close();
        }
    }
    /// <summary>Пример написания отчета</summary>
    class ReportSaldo : BaseSqlReport
    {
        /// <summary>Наименование отчета, которое видет пользователь</summary>
        public override string Name
        {
            get { return "Сальдо"; }
        }

        /// <summary>Описание отчета, пока только для служебных целей кратко описываем предназначение отчета</summary>
        public override string Description
        {
            get { return "Сальдо"; }
        }

        /// <summary>К каким группам относится отчет, определяет подсистему из которой доступен отчет</summary>
        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Reports };
                return result;
            }
        }

        /// <summary>Вид отчета</summary>
        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base, ReportKind.ListLC }; }
        }

        /// <summary>
        /// Предпросмотрт.
        /// Если true, то отчет принудительно формируется в формате fpx и выводится пользователю на просмотр.
        /// </summary>
        public override bool IsPreview
        {
            get { return false; }
        }

        /// <summary>Шаблон отчета</summary>
        protected override byte[] Template
        {
            get { return Resources.RepSaldo; }
        }

        #region Значения параметров отчета

        /// <summary>Расчетный месяц</summary>
        private int MonthS { get; set; }

        /// <summary>Расчетный год</summary>
        private int YearS { get; set; }

        #endregion

        /// <summary>
        /// Пользовательские параметры.
        /// Отображаются на форме печати.
        /// </summary>
        /// <returns>Список пользовательских параметров</returns>
        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new MonthParameter {Name = "Месяц с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["month_"] : DateTime.Today.Month },
                new YearParameter {Name = "Год с", Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year }
            };
        }

        /// <summary>
        /// Осносной метод по формированию данных для генерации отчета. 
        /// </summary>
        /// <returns>Заполненный DataSet</returns>
        public override DataSet GetData()
        {
            StreamWriter sw = new StreamWriter(@"C:\Temp\987654.txt", true);
            GetSelectedKvars();
            
            string sql = "insert into tmp_saldo_local (pref, num_ls,tsg,vu,sumn,peni,sumd,predpr,geu,kod,kodls,kc,rso,ulica,ndom,nkvar,fio)  " + 
                "Select 'bill01', k.num_ls,'' as tsg, '01' as vu, 0 as sumn, 0 as peni, 0 as sumd, substring(k.pkod::varchar(13) from 1 for 3)as predpr, " + 
                "substring(k.pkod::varchar(13) from 4 for 2)as geu, substring(k.pkod::varchar(13) from 6 for 5)as kod, substring(k.pkod::varchar(13) from 11 for 1)::integer as kodls, " + 
                "substring(k.pkod::varchar(13) from 12 for 2)as kc , '00' as rso, u.ulica,d.ndom,k.nkvar,k.fio " + 
                "From bill01_data.kvar k, bill01_data.dom d, bill01_data.s_ulica u, bill01_data.s_area a, bill01_data.s_geu g " +
                "Where k.nzp_dom = d.nzp_dom and d.nzp_ul  = u.nzp_ul and k.nzp_area = a.nzp_area and k.nzp_geu  = g.nzp_geu and k.num_ls > 0 and k.nzp_kvar in (select nzp_kvar from selected_kvars)";
            ExecSQL(sql.ToString());
            ExecSQL("create index tmp_saldo_local_1 on tmp_saldo_local(num_ls)");
            ExecSQL("analyze tmp_saldo_local");
            ExecSQL("UPDATE tmp_saldo_local SET sumn = (SELECT sum(m.SUM_CHARGE) from bill01_charge_"+ (YearS - 2000).ToString("00") + ".charge_"+MonthS.ToString("00") + 
                " m  where nzp_serv > 1 and dat_charge is null and m.num_ls = tmp_saldo_local.num_ls )");
            ExecSQL("UPDATE tmp_saldo_local SET sumd = (SELECT sum(m.SUM_INSALDO-m.sum_money) from bill01_charge_" + (YearS - 2000).ToString("00") + ".charge_" + MonthS.ToString("00") +
                " m  where nzp_serv>1 and dat_charge is null and m.num_ls=tmp_saldo_local.num_ls ) ");
            ExecSQL("insert into tmp_saldo_main select * from tmp_saldo_local");
            ExecSQL("update tmp_saldo_main set kodls = case when kodls = '0' then null else kodls end");
            ExecSQL("create index tmp_saldo_main_1 on tmp_saldo_main(predpr)");
            ExecSQL("analyze tmp_saldo_main");
            var dt2 = ExecSQLToTable("select * from tmp_saldo_main order by predpr");
            DataTable dt = new DataTable();
            dt.Columns.Add("PREDPR", typeof(String));
            dt.Columns.Add("GEU", typeof(String));
            dt.Columns.Add("KOD", typeof(String));
            dt.Columns.Add("KODLS", typeof(String));
            dt.Columns.Add("KC", typeof(String));
            dt.Columns.Add("ADR", typeof(String));
            dt.Columns.Add("FIO", typeof(String));
            dt.Columns.Add("IMYA", typeof(String));
            dt.Columns.Add("OTCH", typeof(String));
            dt.Columns.Add("MES_OPL", typeof(String));
            dt.Columns.Add("SUMN", typeof(Decimal));
            
            string[] names;
            string fio, first_name, name, second_name;
            string TSG, VU, MES_OPL, PREDP, RSO, GEU, KOD, KODLS, ADR, KC, FIO, IMYA, OTCH;
            decimal SUMN, PENI, SUMD;
            sw.WriteLine("1");
            DataRow row2;
            STCLINE.KP50.Global.Utils.setCulture();
            try
            {
                for (int i = 0; i < dt2.Rows.Count; i++)
                {
                    fio = dt2.Rows[i]["fio"] != DBNull.Value
                            ? ((string)dt2.Rows[i]["fio"]).Trim().Replace("'", "''")
                            : "";
                    names = fio.Split(' ');

                    first_name = (names.Length == 3 ? names[0] : fio);
                    name = (names.Length == 3 ? names[1] : "");
                    second_name = (names.Length == 3 ? names[2] : "");
                    row2 = dt.NewRow();
                    row2["PREDPR"] = (dt2.Rows[i]["predpr"] != DBNull.Value ? ((string)dt2.Rows[i]["predpr"]).ToString().Trim() : "");
                    row2["GEU"] = (dt2.Rows[i]["geu"] != DBNull.Value ? ((string)dt2.Rows[i]["geu"]).ToString().Trim() : "");
                    row2["KOD"] = (dt2.Rows[i]["kod"] != DBNull.Value ? ((string)dt2.Rows[i]["kod"]).ToString().Trim() : "");
                    row2["KODLS"] = (dt2.Rows[i]["kodls"] != DBNull.Value ? ((int)dt2.Rows[i]["kodls"]).ToString().Trim() : "");
                    row2["KC"] = (dt2.Rows[i]["kc"] != DBNull.Value ? ((string)dt2.Rows[i]["kc"]).ToString().Trim() : "");
                    row2["ADR"] = (dt2.Rows[i]["ulica"] != DBNull.Value ? ((string)dt2.Rows[i]["ulica"]).Trim().Replace("'", "''") : "").Trim() + "," +
                    (dt2.Rows[i]["ndom"] != DBNull.Value ? ((string)dt2.Rows[i]["ndom"]).Trim() : "").Trim() + "-" + (dt2.Rows[i]["nkvar"] != DBNull.Value ? ((string)dt2.Rows[i]["nkvar"]).Trim() : "").Trim();
                    row2["FIO"] = first_name.Trim();
                    row2["IMYA"] = name.Trim();
                    row2["OTCH"] = second_name.Trim();
                    row2["MES_OPL"] = "" + MonthS.ToString("00") + (YearS - 2000).ToString("00");
                    row2["SUMN"] = (dt2.Rows[i]["sumn"] != DBNull.Value ? ((Decimal)dt2.Rows[i]["sumn"]) : 0);
                    dt.Rows.Add(row2);
                }
            }
            catch(Exception e)
            {
                sw.WriteLine(e.ToString());
            }
            sw.WriteLine("2");
            sw.Close(); 
            var ds = new DataSet();
            dt.TableName = "RepSaldo";
            ds.Tables.Add(dt);
            return ds;
        }

        private string GetWhereSupp(string fieldPref)
        {
            string whereSupp = String.Empty;
            string oldsupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);
            if (!String.IsNullOrEmpty(whereSupp) || !String.IsNullOrEmpty(oldsupp))
            {
                if (!String.IsNullOrEmpty(oldsupp))
                    whereSupp += " AND nzp_supp in (" + oldsupp + ")";
            }
            return " and " + fieldPref + " in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + whereSupp + ")";
        }


        /// <summary>
        /// Метод для обработки значений пользовательских параметров.
        /// Вызывается перед методом GetData()
        /// </summary>
        protected override void PrepareParams()
        {
            MonthS = UserParamValues["Month"].GetValue<int>();
            YearS = UserParamValues["Year"].Value.To<int>();
        }

        /// <summary>Подготовить отчет, например, добавить параметры вызова отчета, произвести другие действия перед сохранением</summary>
        /// <param name="report">Отчет</param>
        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};

            report.SetParameterValue("date", DateTime.Today.ToShortDateString());
        }

        /// <summary>
        /// Создание временных таблиц.
        /// Вызывается до метода GetData()
        /// </summary>
        protected override void CreateTempTable()
        {
            string sql;
                sql = " create temp table selected_kvars(" +
                " num_ls integer, " +
                " nzp_kvar integer, " +
                " nzp_dom integer, " +
                " nzp_area integer) " +
                DBManager.sUnlogTempTable;
                ExecSQL(sql);

            ExecSQL("CREATE temp TABLE tmp_saldo_main(  " +
                "pref char(20), num_ls integer, tsg char(20), vu char(20), sumn numeric(14,2), peni numeric(14,2), sumd numeric(14,2), " +
                "predpr char(3), geu char(2), kod char(5), kodls integer, kc char(2), rso char(2), ulica char(100), ndom char(20), nkvar char(20), fio char(250) )");

            ExecSQL("CREATE temp TABLE tmp_saldo_local ( " +
                    "pref char(20), num_ls integer, tsg char(20), vu char(20), sumn numeric(14,2), peni numeric(14,2), sumd numeric(14,2), " +
                    " predpr char(3), geu char(2), kod char(5), kodls integer, kc char(2), rso char(2), ulica char(100), ndom char(20), nkvar char(20), fio char(250) )");

        }

        /// <summary>
        /// Удаление временных таблиц.
        /// Вызывается после метода GetData()
        /// </summary>
        protected override void DropTempTable()
        {
            ExecSQL(" drop table selected_kvars ", true);
            ExecSQL(" drop table tmp_saldo_main ", true);
            ExecSQL(" drop table tmp_saldo_local ", true);
        }

        private bool GetSelectedKvars()
        {

                using (IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata))
                {
                    if (!DBManager.OpenDb(connWeb, true).result) return false;

                    string tSpls = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter +"t" + ReportParams.User.nzp_user + "_spls";

                    if (TempTableInWebCashe(tSpls))
                    {
                        string sql = " insert into selected_kvars (num_ls, nzp_kvar, nzp_dom, nzp_area) " +
                            " select num_ls, nzp_kvar, nzp_dom, nzp_area from " + tSpls;
                        ExecSQL(sql);
                        ExecSQL("create index ix_tmpsaldo_dom_001 on selected_kvars(num_ls) ");
                        ExecSQL(DBManager.sUpdStat + " selected_kvars ");
                        return true;
                    }
                }
            return false;
        }
    }
}