using System;
using System.Data;
using System.Collections.Generic;
using System.Web.Compilation;
using Bars.KP50.Report.Base;
using STCLINE.KP50.DataBase;
using Bars.KP50.Report.Tula.Properties;
using STCLINE.KP50.EPaspXsd;
using STCLINE.KP50.Interfaces;


namespace Bars.KP50.Report.Tula.Reports
{
    class Report710209 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.2.9 Годовой баланс по ЛС"; }
        }

        public override string Description
        {
            get { return "Годовой баланс по ЛС"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Cards};
                return result;
            }
        }
        public override bool IsPreview
        {
            get { return false; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.LC}; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_71_2_9; }
        }

        /// <summary> Год </summary>
        private int year { get; set; }
        private  string Sob { get; set; }
        private string Gil{ get; set; }
        private string fio { get; set; }
        private string adr { get; set; }
        private string area { get; set; }
        private string s_ob { get; set; }
        private string s_gil { get; set; }
        private string start_dolg { get; set; }
        private string fin_dolg { get; set; }
        public override List<UserParam> GetUserParams()
        {
            var curCalcMonthYear = DBManager.GetCurMonthYear();
            return new List<UserParam>
            {
                new YearParameter{Value = curCalcMonthYear != null ? curCalcMonthYear.Rows[0]["yearr"] : DateTime.Today.Year },
            };
        }

        protected override void PrepareParams()
        {
            year = UserParamValues["Year"].GetValue<int>();  
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("_year", year );
            report.SetParameterValue("pUser", STCLINE.KP50.Global.Utils.GetCorrectFIO(ReportParams.User.uname));
            report.SetParameterValue("adr", adr);
            report.SetParameterValue("Otv", fio);
            report.SetParameterValue("kod", ReportParams.NzpObject);
            report.SetParameterValue("Sob", Sob);
            report.SetParameterValue("Gil", Gil);
            report.SetParameterValue("s_ob", string.IsNullOrEmpty(s_ob.Trim()) ? "-" : s_ob.Trim() );
            report.SetParameterValue("s_gil",  string.IsNullOrEmpty(s_gil.Trim()) ? "-" : s_gil.Trim() );
            report.SetParameterValue("area", area);
            report.SetParameterValue("start_dolg", string.IsNullOrEmpty(start_dolg.Trim()) ? "0.00" : start_dolg);
            report.SetParameterValue("fin_dolg", string.IsNullOrEmpty(fin_dolg.Trim()) ? "0.00" : fin_dolg);
        }

        public override DataSet GetData()
        {
            MyDataReader reader ;
            DataTable tempTable, tempTable1, tempTable2;
            int colMonth = 0;

            var sql = " SELECT pref " +
                      " FROM  " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar " +
                      " WHERE nzp_kvar=" + ReportParams.NzpObject;
            ExecRead(out reader, sql);

            if(reader.Read())
            {
                MyDataReader rdr1;
                string pref = reader["pref"].ToString().Trim();
                string prefData = pref + DBManager.sDataAliasRest;

                sql = " SELECT fam, ima, otch " +
                      " FROM " + prefData + "sobstw s"+ 
                      " WHERE is_actual=1" +
                      " and s.nzp_kvar="+ReportParams.NzpObject;
                ExecRead(out rdr1, sql);
                Sob = string.Empty;
                while (rdr1.Read())
                {
                    Sob += string.Concat(rdr1["fam"].ToString().Trim().ToUpper(), " ", rdr1["ima"].ToString().Trim().ToUpper(), " ",
                        rdr1["otch"].ToString().Trim().ToUpper(), ", ");
                }
                Sob = Sob.Trim(' ', ',');

                sql = " SELECT fam, ima, otch " +
                      " FROM " + prefData + " kart k " +
                      " WHERE isactual='1' and nzp_tkrt=1 " +
                      " and k.nzp_kvar=" + ReportParams.NzpObject;
                ExecRead(out rdr1, sql);
                Gil = string.Empty;
                while (rdr1.Read())
                {
                    Gil += string.Concat(rdr1["fam"].ToString().Trim().ToUpper()," ",rdr1["ima"].ToString().Trim().ToUpper()," ",
                        rdr1["otch"].ToString().Trim().ToUpper(), ", ");
                }
                Gil = Gil.Trim(' ', ',');

                sql = " SELECT area, fio, nkvar, nkvar_n, ndom, nkor, ulica, ulicareg, rajon, town  " +
                      " FROM " + prefData + " kvar k left outer join "
                      + prefData + "s_area a on k.nzp_area=a.nzp_area left outer join "
                      + prefData + "dom d  on  k.nzp_dom=d.nzp_dom, "
                      + prefData + "s_ulica u, "
                      + prefData + "s_rajon sr left outer join " 
                      + prefData + "s_town st on sr.nzp_town=st.nzp_town " 
                      +" WHERE k.nzp_kvar=" + ReportParams.NzpObject
                      + " and d.nzp_ul=u.nzp_ul " 
                      + " and u.nzp_raj=sr.nzp_raj ";
                ExecRead(out rdr1, sql);

                while (rdr1.Read())
                {
                    area = rdr1["area"].ToString().Trim();
                    fio = rdr1["fio"].ToString().ToUpper().Trim();
                    adr = (rdr1["town"].ToString().Trim().ToUpper() == "-"
                        ? ""
                        : rdr1["town"].ToString().Trim().ToUpper()) + " "
                          +
                          (rdr1["rajon"].ToString().Trim().ToUpper() == "-"
                              ? ""
                              : rdr1["rajon"].ToString().Trim().ToUpper()) + " "
                          + rdr1["ulicareg"].ToString().Trim().ToUpper() + " " +
                          rdr1["ulica"].ToString().Trim().ToUpper() + " д."
                          + rdr1["ndom"].ToString().Trim().ToUpper() + " "
                          +
                          (rdr1["nkor"].ToString().Trim().ToUpper() == "-"
                              ? ""
                              : "корп." + rdr1["nkor"].ToString().Trim().ToUpper()) + " "
                          + (rdr1["nkvar"].ToString().Trim().ToUpper() == "-"
                              ? ""
                              : "кв." + rdr1["nkvar"].ToString().Trim().ToUpper()) + " " + " "
                          +
                          (rdr1["nkvar_n"].ToString().Trim().ToUpper() == "-"
                              ? ""
                              : "ком." + rdr1["nkvar_n"].ToString().Trim().ToUpper()) + " ";
                }

                sql = " SELECT CASE WHEN nzp_prm = 4 then val_prm end as ob_s, " +
                      "        CASE WHEN nzp_prm = 6 then val_prm end as gil_s " +
                      " FROM " + pref + DBManager.sDataAliasRest + "prm_1 " +
                      " WHERE nzp=" + ReportParams.NzpObject +
                      "        AND is_actual=1 " +
                      "        AND dat_s<=" + DBManager.sCurDate +
                      "        AND dat_po>=" + DBManager.sCurDate +
                      "        AND nzp_prm in (4,6)";
                ExecRead(out rdr1, sql);
                s_gil = string.Empty;
                s_ob = string.Empty;
                while (rdr1.Read())
                {
                    if (s_gil == string.Empty) s_gil = rdr1["gil_s"].ToString();
                    if (s_ob == string.Empty) s_ob = rdr1["ob_s"].ToString();
                }

                //определения месяца, он не должен быть равен текущему рассчетному

                var rec = Points.GetCalcMonth(new CalcMonthParams { pref = pref });
                if (year <= rec.year_)
                    colMonth = year == rec.year_ ? rec.month_ - 1 : 12;
                   
                for (int i = 1; i <= colMonth; ++i)
                {
                    sql = " INSERT INTO t_ch_71_2_9 " +
                          " (nzp_serv, sum_real, sum_lgota, reval, sum_nedop, real_charge, sum_money, sum_pere,sum_charge, month )" +
                          " SELECT nzp_serv, sum(sum_real), sum(sum_lgota), sum(reval), sum(sum_nedop), sum(real_charge), sum(sum_money),sum(sum_insaldo),sum(sum_charge), '" + i + "'" +
                          " FROM " + pref + "_charge_"+(year-2000).ToString("00")+DBManager.tableDelimiter+"charge_"+i.ToString("00")+
                          " WHERE dat_charge is null and nzp_serv>1 and num_ls=" + ReportParams.NzpObject+
                          " GROUP BY 1";
                    if (TempTableInWebCashe(pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + i.ToString("00"))) 
                        ExecSQL(sql);
                } 
            }
            sql = " INSERT INTO t_res_71_2_9 (nzp_serv, type) SELECT distinct nzp_serv, 1 FROM  t_ch_71_2_9 ";
            ExecSQL(sql);

            for (int i = 1; i <= colMonth; ++i)
            {
                sql = " UPDATE t_res_71_2_9 SET m"+i+" = (SELECT sum_real" +
                      " FROM  t_ch_71_2_9 " +
                      " WHERE t_res_71_2_9.nzp_serv=t_ch_71_2_9.nzp_serv and month="+i+")";
                ExecSQL(sql);    
            }
            sql = " UPDATE t_res_71_2_9 SET itog = (SELECT sum(sum_real) " +
                  " FROM  t_ch_71_2_9 " +
                  " WHERE t_res_71_2_9.nzp_serv=t_ch_71_2_9.nzp_serv)";
            ExecSQL(sql);
            #region
            sql = " INSERT INTO t_res_71_2_9 (name, type, m1, m2, m3, m4, m5, m6, m7, m8, m9, m10, m11, m12, itog )" +
                      " SELECT 'Начислено', 2, " +
                      "         sum(case when month = 1 then sum_real end), " +
                      "         sum(case when month = 2 then sum_real end), " +
                      "         sum(case when month = 3 then sum_real end), " +
                      "         sum(case when month = 4 then sum_real end), " +
                      "         sum(case when month = 5 then sum_real end), " +
                      "         sum(case when month = 6 then sum_real end), " +
                      "         sum(case when month = 7 then sum_real end), " +
                      "         sum(case when month = 8 then sum_real end), " +
                      "         sum(case when month = 9 then sum_real end), " +
                      "         sum(case when month = 10 then sum_real end), " +
                      "         sum(case when month = 11 then sum_real end), " +
                      "         sum(case when month = 12 then sum_real end), " +
                      "         sum( sum_real ) " +
                      " FROM t_ch_71_2_9";
            ExecSQL(sql);

            sql = " INSERT INTO t_res_71_2_9 (name, type, m1, m2, m3, m4, m5, m6, m7, m8, m9, m10, m11, m12, itog )" +
                  " SELECT 'Льгота', 2, " +
                  "         sum(case when month = 1 then sum_lgota end), " +
                  "         sum(case when month = 2 then sum_lgota end), " +
                  "         sum(case when month = 3 then sum_lgota end), " +
                  "         sum(case when month = 4 then sum_lgota end), " +
                  "         sum(case when month = 5 then sum_lgota end), " +
                  "         sum(case when month = 6 then sum_lgota end), " +
                  "         sum(case when month = 7 then sum_lgota end), " +
                  "         sum(case when month = 8 then sum_lgota end), " +
                  "         sum(case when month = 9 then sum_lgota end), " +
                  "         sum(case when month = 10 then sum_lgota end), " +
                  "         sum(case when month = 11 then sum_lgota end), " +
                  "         sum(case when month = 12 then sum_lgota end), " +
                  "         sum( sum_lgota ) " +
                  " FROM t_ch_71_2_9";
            ExecSQL(sql);

            sql = " INSERT INTO t_res_71_2_9 (name, type, m1, m2, m3, m4, m5, m6, m7, m8, m9, m10, m11, m12, itog )" +
                  " SELECT 'Перерасчет', 2, " +
                  "         sum(case when month = 1 then reval end), " +
                  "         sum(case when month = 2 then reval end), " +
                  "         sum(case when month = 3 then reval end), " +
                  "         sum(case when month = 4 then reval end), " +
                  "         sum(case when month = 5 then reval end), " +
                  "         sum(case when month = 6 then reval end), " +
                  "         sum(case when month = 7 then reval end), " +
                  "         sum(case when month = 8 then reval end), " +
                  "         sum(case when month = 9 then reval end), " +
                  "         sum(case when month = 10 then reval end), " +
                  "         sum(case when month = 11 then reval end), " +
                  "         sum(case when month = 12 then reval end), " +
                  "         sum( reval ) " +
                  " FROM t_ch_71_2_9";
            ExecSQL(sql);

            sql = " INSERT INTO t_res_71_2_9 (name, type, m1, m2, m3, m4, m5, m6, m7, m8, m9, m10, m11, m12, itog )" +
                  " SELECT 'Недопоставка', 2, " +
                  "         sum(case when month = 1 then sum_nedop end), " +
                  "         sum(case when month = 2 then sum_nedop end), " +
                  "         sum(case when month = 3 then sum_nedop end), " +
                  "         sum(case when month = 4 then sum_nedop end), " +
                  "         sum(case when month = 5 then sum_nedop end), " +
                  "         sum(case when month = 6 then sum_nedop end), " +
                  "         sum(case when month = 7 then sum_nedop end), " +
                  "         sum(case when month = 8 then sum_nedop end), " +
                  "         sum(case when month = 9 then sum_nedop end), " +
                  "         sum(case when month = 10 then sum_nedop end), " +
                  "         sum(case when month = 11 then sum_nedop end), " +
                  "         sum(case when month = 12 then sum_nedop end), " +
                  "         sum( sum_nedop ) " +
                  " FROM t_ch_71_2_9";
            ExecSQL(sql);

            sql = " INSERT INTO t_res_71_2_9 (name, type, m1, m2, m3, m4, m5, m6, m7, m8, m9, m10, m11, m12, itog )" +
                  " SELECT 'Бух. поправка', 2, " +
                  "         sum(case when month = 1 then real_charge end), " +
                  "         sum(case when month = 2 then real_charge end), " +
                  "         sum(case when month = 3 then real_charge end), " +
                  "         sum(case when month = 4 then real_charge end), " +
                  "         sum(case when month = 5 then real_charge end), " +
                  "         sum(case when month = 6 then real_charge end), " +
                  "         sum(case when month = 7 then real_charge end), " +
                  "         sum(case when month = 8 then real_charge end), " +
                  "         sum(case when month = 9 then real_charge end), " +
                  "         sum(case when month = 10 then real_charge end), " +
                  "         sum(case when month = 11 then real_charge end), " +
                  "         sum(case when month = 12 then real_charge end), " +
                  "         sum( real_charge ) " +
                  " FROM t_ch_71_2_9";
            ExecSQL(sql);

            sql = " INSERT INTO t_res_71_2_9 (name, type, m1, m2, m3, m4, m5, m6, m7, m8, m9, m10, m11, m12, itog )" +
                  " SELECT 'Долг', 3, " +
                  "         sum(case when month = 1 then sum_pere end), " +
                  "         sum(case when month = 2 then sum_pere end), " +
                  "         sum(case when month = 3 then sum_pere end), " +
                  "         sum(case when month = 4 then sum_pere end), " +
                  "         sum(case when month = 5 then sum_pere end), " +
                  "         sum(case when month = 6 then sum_pere end), " +
                  "         sum(case when month = 7 then sum_pere end), " +
                  "         sum(case when month = 8 then sum_pere end), " +
                  "         sum(case when month = 9 then sum_pere end), " +
                  "         sum(case when month = 10 then sum_pere end), " +
                  "         sum(case when month = 11 then sum_pere end), " +
                  "         sum(case when month = 12 then sum_pere end), " +
                  "         sum(case when month = 12 then sum_pere end) " +
                  " FROM t_ch_71_2_9";
            ExecSQL(sql);

            sql = " INSERT INTO t_res_71_2_9 (name, type, m1, m2, m3, m4, m5, m6, m7, m8, m9, m10, m11, m12, itog )" +
                  " SELECT 'Выставлено', 3, " +
                  "         sum(case when month = 1 then sum_charge end), " +
                  "         sum(case when month = 2 then sum_charge end), " +
                  "         sum(case when month = 3 then sum_charge end), " +
                  "         sum(case when month = 4 then sum_charge end), " +
                  "         sum(case when month = 5 then sum_charge end), " +
                  "         sum(case when month = 6 then sum_charge end), " +
                  "         sum(case when month = 7 then sum_charge end), " +
                  "         sum(case when month = 8 then sum_charge end), " +
                  "         sum(case when month = 9 then sum_charge end), " +
                  "         sum(case when month = 10 then sum_charge end), " +
                  "         sum(case when month = 11 then sum_charge end), " +
                  "         sum(case when month = 12 then sum_charge end), " +
                  "         sum( sum_charge ) " +
                  " FROM t_ch_71_2_9";
            ExecSQL(sql);

            sql = " INSERT INTO t_res_71_2_9 (name, type, m1, m2, m3, m4, m5, m6, m7, m8, m9, m10, m11, m12, itog )" +
                  " SELECT 'Оплата', 3, " +
                  "         sum(case when month = 1 then sum_money end), " +
                  "         sum(case when month = 2 then sum_money end), " +
                  "         sum(case when month = 3 then sum_money end), " +
                  "         sum(case when month = 4 then sum_money end), " +
                  "         sum(case when month = 5 then sum_money end), " +
                  "         sum(case when month = 6 then sum_money end), " +
                  "         sum(case when month = 7 then sum_money end), " +
                  "         sum(case when month = 8 then sum_money end), " +
                  "         sum(case when month = 9 then sum_money end), " +
                  "         sum(case when month = 10 then sum_money end), " +
                  "         sum(case when month = 11 then sum_money end), " +
                  "         sum(case when month = 12 then sum_money end), " +
                  "         sum( sum_money ) " +
                  " FROM t_ch_71_2_9";
            ExecSQL(sql);
            #endregion
            
            sql = " SELECT * " +
                  " FROM  t_res_71_2_9 t, "+
                  ReportParams.Pref+DBManager.sKernelAliasRest+"services s " +
                  " WHERE t.nzp_serv=s.nzp_serv and type=1";
            tempTable = ExecSQLToTable(sql);
            tempTable.TableName = "Q_master";

            sql = " SELECT * " +
                  " FROM  t_res_71_2_9 t " +
                  " WHERE type=2 ";

            tempTable1 = ExecSQLToTable(sql);
            tempTable1.TableName = "Q_master1";

            sql = " SELECT name, m1, m2, m3, m4, m5, m6, m7, m8, m9, m10, m11, m12, itog  " +
                  " FROM  t_res_71_2_9 t " +
                  " WHERE type=3 ";

            tempTable2 = ExecSQLToTable(sql);
            tempTable2.TableName = "Q_master2";
            
            start_dolg=tempTable2.Rows[0][1].ToString();
            fin_dolg = colMonth==0 ? "0.00" : tempTable2.Rows[0][colMonth].ToString();

            var ds = new DataSet();
            ds.Tables.Add(tempTable);
            ds.Tables.Add(tempTable1);
            ds.Tables.Add(tempTable2);

            return ds;
        }

        protected override void CreateTempTable()
        {
            const string sql = " CREATE TEMP TABLE t_ch_71_2_9( " +
                                " nzp_serv integer, " +
                                " sum_real " + DBManager.sDecimalType + "(14,2), " +   //начислено
                                " sum_lgota " + DBManager.sDecimalType + "(14,2), " +   //льгота
                                " reval " + DBManager.sDecimalType + "(14,2), " +   //перерасчет
                                " sum_nedop " + DBManager.sDecimalType + "(14,2), " +   //недопоставка
                                " real_charge " + DBManager.sDecimalType + "(14,2), " +   //корректировка
                                " sum_charge " + DBManager.sDecimalType + "(14,2), " +   // к оплате
                                " sum_money " + DBManager.sDecimalType + "(14,2), " +  //оплачено
                                " sum_pere " + DBManager.sDecimalType + "(14,2), " +  //долг
                                " month integer)" + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            const string sql1 = " CREATE TEMP TABLE t_res_71_2_9( " +
                    " nzp_serv integer, " +
                    " type integer, " +
                    " name char(30), " +
                    " m1 " + DBManager.sDecimalType + "(14,2), " +  
                    " m2 " + DBManager.sDecimalType + "(14,2), " +
                    " m3 " + DBManager.sDecimalType + "(14,2), " +
                    " m4 " + DBManager.sDecimalType + "(14,2), " +
                    " m5 " + DBManager.sDecimalType + "(14,2), " +
                    " m6 " + DBManager.sDecimalType + "(14,2), " +
                    " m7 " + DBManager.sDecimalType + "(14,2), " +
                    " m8 " + DBManager.sDecimalType + "(14,2), " +
                    " m9 " + DBManager.sDecimalType + "(14,2), " +
                    " m10 " + DBManager.sDecimalType + "(14,2), " +
                    " m11 " + DBManager.sDecimalType + "(14,2), " +
                    " m12 " + DBManager.sDecimalType + "(14,2), " +  
                    " itog " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql1);
        }

        protected override void DropTempTable()
        {
            ExecSQL("DROP TABLE t_ch_71_2_9");
            ExecSQL("DROP TABLE t_res_71_2_9");
            ExecSQL("DROP TABLE t_n_res_71_2_9");
        }
    }
}
