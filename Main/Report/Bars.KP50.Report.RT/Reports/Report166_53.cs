using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using Bars.KP50.Report.RT.Properties;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.RT.Reports
{
    public class Report166_53 : BaseSqlReport
    {
        public override string Name
        {
            get { return "16.6.53 Отчёт по датам поверки счетчиков по услуге"; }
        }

        public override string Description
        {
            get { return "6.53 Отчёт по датам поверки счетчиков по услуге"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                List<ReportGroup> result = new List<ReportGroup> {ReportGroup.Reports};
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_16_6_53; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }


        /// <summary>Период С</summary>
        protected DateTime DateS { get; set; }

        /// <summary>Период по</summary>
        protected DateTime DatePo { get; set; }

        /// <summary>Услуги</summary>
        protected string Service { get; set; }

        /// <summary>Тип счетчика</summary>
        protected byte TypeCounter { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AreaHeader { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string ServicesHeader { get; set; }


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
                new ServiceParameter(multiSelect:false){Require=true},
                new ComboBoxParameter
                {
                    Code = "TypeCounter",
                    Name = "Тип счетчика",
                    Value = "1",
                    MultiSelect=false,
                    StoreData = new List<object>
                    {
                        new { Id = "1", Name = "Домовой счетчик" },
                        new { Id = "2", Name = "Квартирный счетчик" },
                        new { Id = "3", Name = "Групповой счетчик" }
                    }
                },
                new AreaParameter()
            };
        }

        public override DataSet GetData()
        {
            IDataReader reader;
            var sql = new StringBuilder();

            string whereArea = GetWhereArea("dom.");
            string whereServ = GetWhereServ("cnt.");

            #region выборка в temp таблицу
            sql.Remove(0, sql.Length);
            sql.Append(" SELECT bd_kernel as pref ");
            sql.AppendFormat(" FROM {0}{1}s_point ", DBManager.GetFullBaseName(Connection), DBManager.tableDelimiter);
            sql.Append(" WHERE nzp_wp>1 " + GetWhereWp());

            ExecRead(out reader, sql.ToString());

            while (reader.Read())
            {
                if (reader["pref"] != null)
                {
                    string pref = reader["pref"].ToStr().Trim();
                    string prefData = pref + DBManager.sDataAliasRest,
                            prefKernel = pref + DBManager.sKernelAliasRest;
                    switch (TypeCounter)
                    {
                        case 1:
                            sql.Remove(0, sql.Length);
                            sql.Append(" INSERT INTO t_pover(nzp_counter,town,ulica, ndom,nkor,nkvar,num_cnt,name_type,cnt_stage,dat_prov,dat_provnext) ");
                            sql.Append(" SELECT " + DBManager.sUniqueWord + " cnt.nzp_counter as nzp_counter, t.town as town, u.ulica as ulica, dom.ndom as ndom, dom.nkor as nkor, '' as nkvar, num_cnt, s_cnt.name_type as name_type,");
                            sql.Append(" s_cnt.cnt_stage as cnt_stage, dat_prov, dat_provnext ");
                            sql.Append(" FROM " + prefData + "counters_dom cnt INNER JOIN " + prefData + "dom dom ON dom.nzp_dom = cnt.nzp_dom " +
                                                                             " INNER JOIN " + prefData + "s_ulica u ON dom.nzp_ul = u.nzp_ul " +
                                                                             " INNER JOIN " + prefData + "s_rajon r ON u.nzp_raj=r.nzp_raj " +
                                                                             " INNER JOIN " + prefData + "s_town t ON r.nzp_town = t.nzp_town " +
                                                                             " INNER JOIN " + prefKernel + "s_counttypes s_cnt ON cnt.nzp_cnttype = s_cnt.nzp_cnttype ");
                            sql.Append(" WHERE is_actual = 1 ");
                            sql.Append(" AND '" + DateS.ToShortDateString() + "' <= dat_provnext AND '" + DatePo.ToShortDateString() + "' >= dat_provnext ");
                            sql.Append(" AND dat_provnext is not null ");
                            sql.Append(whereArea + whereServ);
                            break;
                        case 2:
                            sql.Remove(0, sql.Length);
                            sql.Append(" INSERT INTO t_pover(nzp_counter,town,ulica, ndom,nkor,nkvar,num_cnt,name_type,cnt_stage,dat_prov,dat_provnext) ");
                            sql.Append(" SELECT " + DBManager.sUniqueWord + " cnt.nzp_counter as nzp_counter, t.town as town, u.ulica as ulica, dom.ndom as ndom, dom.nkor as nkor, kv.nkvar as nkvar, num_cnt, s_cnt.name_type as name_type, ");
                            sql.Append(" s_cnt.cnt_stage as cnt_stage, dat_prov, dat_provnext ");
                            sql.Append(" FROM " + prefData + "counters cnt INNER JOIN " + prefData + "kvar kv ON kv.nzp_kvar = cnt.nzp_kvar " +
                                                                         " INNER JOIN " + prefData + "dom dom ON dom.nzp_dom = kv.nzp_dom " +
                                                                         " INNER JOIN " + prefData + "s_ulica u ON dom.nzp_ul = u.nzp_ul " +
                                                                         " INNER JOIN " + prefData + "s_rajon r ON u.nzp_raj=r.nzp_raj " +
                                                                         " INNER JOIN " + prefData + "s_town t ON r.nzp_town = t.nzp_town " +
                                                                         " INNER JOIN " + prefKernel + "s_counttypes s_cnt ON cnt.nzp_cnttype = s_cnt.nzp_cnttype ");
                            sql.Append(" WHERE is_actual = 1 ");
                            sql.Append(" AND '" + DateS.ToShortDateString() + "' <= dat_provnext AND '" + DatePo.ToShortDateString() + "' >= dat_provnext ");
                            sql.Append(" AND dat_provnext is not null ");
                            sql.Append(whereArea + whereServ);
                            break;
                        case 3:
                            sql.Remove(0, sql.Length);
                            sql.Append(" INSERT INTO t_pover(nzp_counter,town,ulica, ndom,nkor,nkvar,num_cnt,name_type,cnt_stage,dat_prov,dat_provnext) ");
                            sql.Append(" SELECT " + DBManager.sUniqueWord + " cnt.nzp_counter as nzp_counter, t.town as town, u.ulica as ulica, dom.ndom as ndom, dom.nkor as nkor, '' as nkvar, num_cnt, s_cnt.name_type as name_type, ");
                            sql.Append(" s_cnt.cnt_stage as cnt_stage, dat_prov, dat_provnext ");
                            sql.Append(" FROM " + prefData + "counters_domspis cnt INNER JOIN " + prefData + "dom dom ON dom.nzp_dom = cnt.nzp_dom " +
                                                                                 " INNER JOIN " + prefData + "s_ulica u ON dom.nzp_ul = u.nzp_ul " +
                                                                                 " INNER JOIN " + prefData + "s_rajon r ON u.nzp_raj=r.nzp_raj " +
                                                                                 " INNER JOIN " + prefData + "s_town t ON r.nzp_town = t.nzp_town " +
                                                                                 " INNER JOIN " + prefKernel + "s_counttypes s_cnt ON cnt.nzp_cnttype = s_cnt.nzp_cnttype ");
                            sql.Append(" WHERE is_actual = 1 ");
                            sql.Append(" AND '" + DateS.ToShortDateString() + "' <= dat_provnext AND '" + DatePo.ToShortDateString() + "' >= dat_provnext ");
                            sql.Append(" AND dat_provnext is not null ");
                            sql.Append(whereArea + whereServ);
                            break;
                    }
                    ExecSQL(sql.ToString());
                }
            }

            reader.Close();
            #endregion

            sql.Remove(0, sql.Length);
            sql.Append(" SELECT town, ulica, ndom, nkor, nkvar, num_cnt, name_type, cnt_stage, " + DBManager.sNvlWord + "(dat_prov,NULL) AS dat_prov, dat_provnext  ");
            sql.Append(" FROM t_pover WHERE dat_provnext >= " + DBManager.sNvlWord + "(Date(dat_prov), '01.01.1990') ORDER BY 1,2,3,4,5 ");
            DataTable dt = ExecSQLToTable(sql.ToString());
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        /// <summary>
        /// Получить условия органичения по локальному банку
        /// </summary>
        private string GetWhereWp()
        {
            var result = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            return !String.IsNullOrEmpty(result) ? " AND nzp_wp IN (" + result + ") " : "";
        }

        /// <summary>
        /// Получить условия органичения по УК
        /// </summary>
        private string GetWhereArea(string tablePrefix)
        {
            string whereArea = String.Empty;
            if (Areas != null)
            {
                whereArea = Areas.Aggregate(whereArea, (current, nzpArea) => current + (nzpArea + ","));
            }
            else
            {
                whereArea = ReportParams.GetRolesCondition(Constants.role_sql_area);
            }

            whereArea = whereArea.TrimEnd(',');
            if (!String.IsNullOrEmpty(whereArea))
            {
                whereArea = " AND " + tablePrefix + "nzp_area in (" + whereArea + ")";
                AreaHeader = String.Empty;

                string sql = " SELECT area from " +
                             ReportParams.Pref + DBManager.sDataAliasRest + "s_area " + tablePrefix.TrimEnd('.') +
                             " WHERE " + tablePrefix + "nzp_area > 0 " + whereArea;
                DataTable area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    AreaHeader += dr["area"].ToString().Trim() + ",";
                }
                AreaHeader = AreaHeader.TrimEnd(',');
            }
            return whereArea;
        }

        /// <summary>
        /// Получить условия органичения по услугам
        /// </summary>
        private string GetWhereServ(string tablePrefix)
        {
            string whereServ = String.Empty;
            if (Service != null)
            {
                whereServ = Service.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ","));
            }
            else
            {
                whereServ = ReportParams.GetRolesCondition(Constants.role_sql_serv);
            }
            whereServ = whereServ.TrimEnd(',');
            if (!String.IsNullOrEmpty(whereServ))
            {
                whereServ = " AND " + tablePrefix + "nzp_serv in (" + whereServ + ")";
                ServicesHeader = String.Empty;

                string sql = " SELECT service FROM " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "services " + tablePrefix.TrimEnd('.') + " , " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "s_counts cou  " +
                             " WHERE " + tablePrefix + "nzp_serv > 0 " +
                               " AND cou.nzp_serv=" + tablePrefix + "nzp_serv " + whereServ;
                DataTable ser = ExecSQLToTable(sql);
                foreach (DataRow dr in ser.Rows)
                {
                    ServicesHeader += dr["service"].ToString().Trim() + ",";
                }
                ServicesHeader = ServicesHeader.TrimEnd(',');
            }
            return whereServ;
        }

        protected override void CreateTempTable()
        {
            var sql = new StringBuilder();
            sql.Append(" CREATE TEMP TABLE t_pover( ");
            sql.Append(" nzp_counter INTEGER, ");//id счётчик
            sql.Append(" town CHARACTER(100), ");//Город
            sql.Append(" ulica CHARACTER(40), ");//Улица
            sql.Append(" ndom CHARACTER(10), ");//Дом
            sql.Append(" nkor CHARACTER(3), ");//Корпус
            sql.Append(" nkvar CHARACTER(10), ");//Квартира
            sql.Append(" num_cnt CHARACTER(20), ");//Номер счётчика
            sql.Append(" name_type CHARACTER(40), ");//тип счётчика
            sql.Append(" cnt_stage INTEGER, ");//Разрядность
            sql.Append(" dat_prov DATE, ");//Дата проверки
            sql.Append(" dat_provnext DATE) " + DBManager.sUnlogTempTable);//Дата следующей проверки
            ExecSQL(sql.ToString());
        }

        protected override void DropTempTable()
        {
            ExecSQL("DROP TABLE t_pover");
        }


        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("area", AreaHeader);
            report.SetParameterValue("service",  ServicesHeader);
            report.SetParameterValue("dats", DateS.ToShortDateString());
            report.SetParameterValue("datpo", DatePo.ToShortDateString());
            report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
            report.SetParameterValue("TIME", DateTime.Now.ToLongTimeString());
            switch (TypeCounter)
            {
                case 1: report.SetParameterValue("t_counter", "Домовой"); break ;
                case 2: report.SetParameterValue("t_counter", "Квартирный"); break;
                case 3: report.SetParameterValue("t_counter", "Групповой"); break;
            }
        }

        protected override void PrepareParams()
        {
            DateTime begin;
            DateTime end;
            string period = UserParamValues["Period"].GetValue<string>();
            PeriodParameter.GetValues(period, out begin, out end);
            DateS = begin;
            DatePo = end;
            Service = UserParamValues["Services"].Value.ToString();
            Areas = UserParamValues["Areas"].Value.To<List<int>>();
            TypeCounter = UserParamValues["TypeCounter"].Value.To<byte>();
        }
    }
}
