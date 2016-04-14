using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Main.Properties;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.Report.Main.Reports
{
    public class WebOperInfo : BaseSqlReport
    {
        public override string Name
        {
            get { return "Информация о перечислениях денежных средств "; }
        }

        public override string Description
        {
            get { return "Оперативная информация о перечислениях денежных средств поставщикам коммунальных услуг"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                List<ReportGroup> result = new List<ReportGroup> {ReportGroup.Reports};
                //   result.Add(ReportGroup.Finans);
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Web_oper_info; }
        }

        public override ReportKind ReportKind
        {
            get { return ReportKind.Base; }
        }


        /// <summary>Период С</summary>
        protected DateTime DateS { get; set; }

        /// <summary>Период по</summary>
        protected DateTime DatePo { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AreaHeader { get; set; }

        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new PeriodParameter(DateTime.Now, DateTime.Now),
                new SupplierParameter(),
                new AreaParameter()
            };
        }

        public override DataSet GetData()
        {
            IDataReader reader;
            var sql = new StringBuilder();

            string whereArea = "";
            string whereSupp = "";
            string[] months = {"","январе","феврале",
                "марте","апреле","мае","июне","июле","августе","сентябре",
                "октябре","ноябре","декабре"};

            //ограничения

            #region Управляющие компании
            if (Areas != null)
            {
                whereArea = Areas.Aggregate(whereArea, (current, nzpArea) => current + (nzpArea + ","));
            }

            whereArea = whereArea.TrimEnd(',');
            if (!String.IsNullOrEmpty(whereArea)) whereArea = " AND kv.nzp_area in (" + whereArea + ") ";
            #endregion

            #region Поставщики
            if (Suppliers != null)
                whereSupp = Suppliers.Cast<int>().Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
            whereSupp = whereSupp.TrimEnd(',');


            if (!String.IsNullOrEmpty(whereSupp)) whereSupp = " AND ch.nzp_supp in (" + whereSupp + ")";
            
            #endregion

            #region выборка в temp таблицу
            sql.Remove(0, sql.Length);
            sql.Append(" SELECT bd_kernel as pref ");
            sql.AppendFormat(" FROM {0}{1}s_point ", DBManager.GetFullBaseName(Connection), DBManager.tableDelimiter);
            sql.Append(" WHERE nzp_wp>1 ");

            ExecRead(out reader, sql.ToString());

            
            while (reader.Read())
            {
                if (reader["pref"] != null)
                {
                    string pref = reader["pref"].ToStr().Trim();
                    sql.Remove(0, sql.Length);
                    sql.Append(" INSERT INTO t_operinfo(id, supplier, area, sum_money) ");
                    sql.Append(" SELECT * FROM ( ");
                        sql.Append(" SELECT 1 as id, sup.name_supp AS supplier, s_a.area AS area, SUM(ch.sum_money) AS sum_money ");
                        sql.Append(" FROM " + pref + "_charge_" + (DateS.Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + DateS.Month.ToString("00") + " ch, ");
                            sql.Append( ReportParams.Pref + DBManager.sKernelAliasRest + "supplier sup, ");
                            sql.Append(pref + DBManager.sDataAliasRest + "kvar kv, ");
                            sql.Append( ReportParams.Pref + DBManager.sDataAliasRest + "s_area s_a ");
                        sql.Append(" WHERE ch.nzp_supp = sup.nzp_supp ");
                            sql.Append(" AND  ch.nzp_kvar=kv.nzp_kvar ");
                            sql.Append(" AND kv.nzp_area=s_a.nzp_area ");
                            sql.Append( whereArea + whereSupp );
                        sql.Append(" GROUP BY 2,3 ");

                        sql.Append(" UNION ");
                    
                        sql.Append(" SELECT 2 AS id, supplier, 'Перечислено в " + months[DateS.Month] + ", в т.ч.:' AS area, SUM(sum_money) AS sum_money ");
                        sql.Append(" FROM ( ");
                            sql.Append(" SELECT 1 as id, sup.name_supp AS supplier, s_a.area AS area, SUM(ch.sum_money) AS sum_money ");
                            sql.Append(" FROM " + pref + "_charge_" + (DateS.Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + DateS.Month.ToString("00") + " ch, ");
                                sql.Append(ReportParams.Pref + DBManager.sKernelAliasRest + "supplier sup, ");
                                sql.Append(pref + DBManager.sDataAliasRest + "kvar kv, ");
                                sql.Append(ReportParams.Pref + DBManager.sDataAliasRest + "s_area s_a ");
                            sql.Append(" WHERE ch.nzp_supp = sup.nzp_supp ");
                                sql.Append(" AND  ch.nzp_kvar=kv.nzp_kvar ");
                                sql.Append(" AND kv.nzp_area=s_a.nzp_area ");
                                sql.Append( whereArea + whereSupp );
                            sql.Append(" GROUP BY 2,3) ");
                        sql.Append(" GROUP BY 2 ");

                        sql.Append(" UNION ");

                        sql.Append(" SELECT 3 AS id, sup.name_supp AS supplier, 'Поступило от населения за ЖКУ (по поставщикам)' AS area, SUM(sum_money) AS sum_money ");
                        sql.Append(" FROM " + pref + "_charge_" + (DateS.Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + DateS.Month.ToString("00") + " ch, ");
                            sql.Append( ReportParams.Pref + DBManager.sKernelAliasRest + "supplier sup ");
                        sql.Append(" WHERE ch.nzp_supp = sup.nzp_supp ");
                            sql.Append( whereSupp );
                        sql.Append(" GROUP BY 2 ");

                        sql.Append(" UNION ");

                        sql.Append(" SELECT 4 AS id, sup.name_supp AS supplier, 'Начислено населению за ЖКУ (по поставщикам)' AS area, SUM(sum_charge) AS sum_money ");
                        sql.Append(" FROM " + pref + "_charge_" + (DateS.Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + DateS.Month.ToString("00") + " ch, ");
                            sql.Append( ReportParams.Pref + DBManager.sKernelAliasRest + "supplier sup ");
                        sql.Append(" WHERE ch.nzp_supp = sup.nzp_supp ");
                            sql.Append( whereSupp );
                        sql.Append(" GROUP BY 2  ");

                        sql.Append(" UNION ");

                        sql.Append(" SELECT 5 AS id, sup.name_supp AS supplier, 'Остаток на "+DateS.ToShortDateString()+"г.' AS area, SUM(sum_insaldo) AS sum_money ");
                        sql.Append(" FROM " + pref + "_charge_" + (DateS.Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + DateS.Month.ToString("00") + " ch, ");
                            sql.Append( ReportParams.Pref + DBManager.sKernelAliasRest + "supplier sup ");
                        sql.Append(" WHERE ch.nzp_supp = sup.nzp_supp ");
                            sql.Append( whereSupp );
                        sql.Append(" GROUP BY 2 ) ");
                    ExecSQL(sql.ToString());
                }
            }
            #endregion

            sql.Remove(0, sql.Length);
            sql.Append(" SELECT id, supplier, area, SUM(sum_money) AS sum_money  ");
            sql.Append(" FROM  t_operinfo ");
            sql.Append(" GROUP BY 1,2,3 ");
            sql.Append(" ORDER BY 1 DESC,2,3 ");

            DataTable dt = ExecSQLToTable(sql.ToString());
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        protected override void CreateTempTable()
        {
            var sql = new StringBuilder();
            sql.Append(" CREATE TEMP TABLE t_operinfo( ");
            sql.Append(" id INTEGER, ");//нумерация групп строк
            sql.Append(" supplier CHARACTER(70), ");//Поставщик
            sql.Append(" area CHARACTER(70), ");//Управляющая компания
            sql.Append(" sum_money " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable);//Сумма
            ExecSQL(sql.ToString());
        }

        protected override void DropTempTable()
        {

            ExecSQL("DROP TABLE t_operinfo");
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("dats", DateS.ToShortDateString());
            report.SetParameterValue("datpo", DatePo.ToShortDateString());
            report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
            report.SetParameterValue("TIME", DateTime.Now.ToLongTimeString());
        }

        protected override void PrepareParams()
        {
            DateTime begin;
            DateTime end;
            string period = UserParamValues["Period"].GetValue<string>();
            PeriodParameter.GetValues(period, out begin, out end);
            DateS = begin;
            DatePo = end;
            Suppliers = UserParamValues["Suppliers"].Value.To<List<long>>();
            Areas = UserParamValues["Areas"].Value.To<List<int>>();
        }
    }
}
