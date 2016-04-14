using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Samara.Properties;
using Bars.KP50.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Samara.Reports
{
    class Report3016 : BaseSqlReport
    {
        public override string Name
        {
            get { return "30.1.6 Отчет о жилом фонде"; }
        }

        public override string Description
        {
            get { return "Отчет о жилом фонде"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                List<ReportGroup> result = new List<ReportGroup> { ReportGroup.Reports };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_30_1_6; }
        }


        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>Расчетный месяц</summary>
        protected int Month { get; set; }

        /// <summary>Расчетный год</summary>
        protected int Year { get; set; }

        /// <summary>Управляющие компании</summary>
        protected List<int> Areas { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string AreaHeader { get; set; }

        /// <summary>Наличие площади на начало года</summary>
        protected string NumberArea { get; set; }

        /// <summary>Число жилых строений</summary>
        protected string NumberHouse { get; set; }

        /// <summary>Общая площадь жилых помещений</summary>
        protected string ObchyaPl { get; set; }

        /// <summary>Расчетная площадь</summary>
        protected string RaschetPl { get; set; }

        /// <summary>Число постоянно проживающих</summary>
        protected string PostProzh { get; set; }

        /// <summary>Число прописанных жильцов</summary>
        protected string PropisZhil { get; set; }

        /// <summary>Закрыте лицевые счета</summary>
        protected string ZakrLicChet { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }

        public override List<UserParam> GetUserParams()
        {

            return new List<UserParam>
            {
                new MonthParameter {Value = DateTime.Today.Month },
                new YearParameter {Value = DateTime.Today.Year },
                new SupplierAndBankParameter(),
                new AreaParameter()
            };
        }

        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].Value.To<int>();
            Areas = UserParamValues["Areas"].Value.To<List<int>>();
            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};

            if (AreaHeader == null)
            {
                report.SetParameterValue("invisible_info", false);
            }
            else
            {
                report.SetParameterValue("invisible_info", true);
                AreaHeader = AreaHeader != null ? "Балансодержатель: " + AreaHeader : AreaHeader;
            }
            report.SetParameterValue("area", AreaHeader);

            report.SetParameterValue("period_month", months[Month] + " " + Year + " г.");
            report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
            report.SetParameterValue("TIME", DateTime.Now.ToLongTimeString());
            report.SetParameterValue("number_area", NumberArea.Replace(',', '.'));
            report.SetParameterValue("number_house", NumberHouse);
            report.SetParameterValue("obchya_pl", ObchyaPl.Replace(',','.'));
            report.SetParameterValue("raschet_pl", RaschetPl.Replace(',', '.'));
            report.SetParameterValue("post_prozh", PostProzh);
            report.SetParameterValue("propis_zhil", PropisZhil);
            report.SetParameterValue("zakr_lic_chet", ZakrLicChet);
            report.SetParameterValue("town", "г.о. Жигулевск");
        }

        public override DataSet GetData()
        {
            IDataReader reader;

            string whereArea = GetWhereArea();

            string firstDay = "1." + Month + "." + Year;
            #region выборка в temp таблицу

            string sql = " SELECT bd_kernel as pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " WHERE nzp_wp>1 " + GetwhereWp();
            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();
                string calcGkuTable = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "calc_gku_" + Month.ToString("00");
                string prefData = pref + DBManager.sDataAliasRest,
                        prefKernel = pref + DBManager.sKernelAliasRest;

                if (TempTableInWebCashe(calcGkuTable))
                {
                    sql = " INSERT INTO t_temp_otzhdus(nzp_kvar, ls, nzp_dom) " +
                       " SELECT kv.nzp_kvar, " +
                              " 1 AS ls, " +
                              " kv.nzp_dom " +
                       " FROM " + prefData + "kvar kv INNER JOIN " + prefData + "dom d ON (kv.nzp_dom=d.nzp_dom " +
                                                                                     " AND kv.nzp_area=d.nzp_area) " +
                       " WHERE 0 = (SELECT COUNT(*) " +
                                  " FROM " + prefData + "prm_2 p " +
                                  " WHERE kv.nzp_dom = p.nzp " +
                                    " AND p.is_actual=1 " +
                                    " AND p.nzp_prm=2029 " +
                                    " AND p.dat_s<='" + firstDay + "' " +
                                    " AND p.dat_po>='" + firstDay + "' " +
                                    " AND date(p.val_prm) <= date('" + firstDay + "')) " +
                           whereArea +
                       " GROUP BY 1,2,3 ";
                    ExecSQL(sql);

                    sql = DBManager.sUpdStat + " t_temp_otzhdus"; 
                    ExecSQL(sql);

                    sql = " UPDATE t_temp_otzhdus " +
                          " SET zak_gil = (SELECT DISTINCT 1 " +
                                           " FROM " + prefData + "prm_3 pr3 " +
                                           " WHERE pr3.is_actual=1 " +
                                             " AND pr3.nzp_prm=51 " +
                                             " AND pr3.dat_s<='" + firstDay + "' " +
                                             " AND pr3.dat_po>='" + firstDay + "' " +
                                             " AND val_prm='2' " +
                                             " AND pr3.nzp = t_temp_otzhdus.nzp_kvar) ";
                    ExecSQL(sql);

                    sql = " UPDATE t_temp_otzhdus " +
                          " SET gil = (SELECT MAX(ROUND(gil)) " +
                                           " FROM " + calcGkuTable + " cal " +
                                           " WHERE cal.tarif>0  " +
                                             " AND cal.nzp_kvar = t_temp_otzhdus.nzp_kvar) ";
                    ExecSQL(sql);

                    sql = " UPDATE t_temp_otzhdus " +
                          " SET rpl = (SELECT MAX(squ) " +
                                           " FROM " + calcGkuTable + " cal " +
                                           " WHERE cal.tarif>0  " +
                                             " AND cal.nzp_kvar = t_temp_otzhdus.nzp_kvar) ";
                    ExecSQL(sql);

                    sql = " UPDATE t_temp_otzhdus " +
                          " SET tpl = (SELECT MAX(REPLACE(pr1.val_prm,',','.')" + DBManager.sConvToNum + ") " +
                                         " FROM " + prefData + "prm_1 pr1 " +
                                         " WHERE pr1.is_actual=1 " +
                                           " AND pr1.nzp_prm=4 " +
                                           " AND pr1.dat_s<='" + firstDay + "' " +
                                           " AND pr1.dat_po>='" + firstDay + "' " +
                                           " AND pr1.nzp = t_temp_otzhdus.nzp_kvar) ";
                    ExecSQL(sql);

                    sql = " UPDATE t_temp_otzhdus " +
                          " SET pl_year = (SELECT MAX(REPLACE(pr1.val_prm,',','.')" + DBManager.sConvToNum + ") " +
                                         " FROM " + prefData + "prm_1 pr1 " +
                                         " WHERE pr1.is_actual=1 " +
                                           " AND pr1.nzp_prm=4 " +
                                           " AND pr1.dat_s<='1.1." + Year + "' " +
                                           " AND pr1.dat_po>='1.1." + Year + "' " +
                                           " AND pr1.nzp = t_temp_otzhdus.nzp_kvar) ";
                    ExecSQL(sql);

                    sql = " UPDATE t_temp_otzhdus " +
                          " SET gpl = (SELECT MAX(REPLACE(pr1.val_prm,',','.')" + DBManager.sConvToNum + ") " +
                                         " FROM " + prefData + "prm_1 pr1 " +
                                         " WHERE pr1.is_actual=1 " +
                                           " AND pr1.nzp_prm=6 " +
                                           " AND pr1.dat_s<='" + firstDay + "' " +
                                           " AND pr1.dat_po>='" + firstDay + "' " +
                                           " AND pr1.nzp = t_temp_otzhdus.nzp_kvar) ";
                    ExecSQL(sql);

                    sql = " UPDATE t_temp_otzhdus " +
                          " SET const_gil = (SELECT MAX(pr1.val_prm" + DBManager.sConvToNum + ") " +
                                           " FROM " + prefData + "prm_1 pr1 " +
                                           " WHERE pr1.is_actual=1 " +
                                             " AND pr1.nzp_prm=2005 " +
                                             " AND pr1.dat_s<='" + firstDay + "' " +
                                             " AND pr1.dat_po>='" + firstDay + "' " +
                                             " AND pr1.nzp = t_temp_otzhdus.nzp_kvar) ";
                    ExecSQL(sql);

                    sql = " INSERT INTO t_temp_otzhdus3(nzp_kvar, nzp_dom, tpl, pl_year, rpl, const_gil, pr_gil, zak_gil) " +
                          " SELECT nzp_kvar, " +
                                 " nzp_dom, " +
                                 " MAX(tpl) AS tpl, " +
                                 " MAX(pl_year) AS pl_year, " +
                                 " MAX(rpl) AS rpl, " +  
                                 " MAX(gil) AS const_gil, " +
                                 " MAX(const_gil) AS pr_gil, " +  
                                 " MAX(zak_gil) AS zak_gil " +
                          " FROM t_temp_otzhdus " +
                          " GROUP BY nzp_kvar, nzp_dom ";
                    ExecSQL(sql);

                    sql = " INSERT INTO t_temp_otzhdus1(nzp_kvar, nzp_serv, tpl, gpl, rpl, ls, gil) " +
                          " SELECT ta.nzp_kvar, " +
                                 " ta.nzp_serv, " +
                                 " MAX(tpl) AS tpl, " +
                                 " MAX(gpl) AS gpl, " +
                                 " MAX(rpl) AS rpl, " +
                                 " MAX(ls) AS ls, " +
                                 " MAX(gil) AS gil " +
                          " FROM t_temp_otzhdus t INNER JOIN " + prefData + "tarif ta ON (ta.nzp_kvar = t.nzp_kvar " +
                                                                                    " AND ta.dat_s<='" + firstDay + "' " +
                                                                                    " AND ta.dat_po>='" + firstDay + "' " +
                                                                                    " AND ta.is_actual=1 " +
                                                                                    " AND ta.nzp_serv NOT IN (510,513,515,17,15)) " +
                          " GROUP BY 1,2 ";
                    ExecSQL(sql);

                    ExecSQL(" UPDATE t_temp_otzhdus1 SET nzp_serv = 9 WHERE nzp_serv in (513,514) "); // не выводить ОДН-ГВС и ОДН-ХВС для ГВС #100298 

                    sql = " INSERT INTO t_temp_otzhdus2(nzp_serv, pref, service, sort, sum_tpl, sum_gpl, sum_rpl, sum_ls, sum_gil) " +
                          " SELECT t.nzp_serv, " +
                                 " '" + pref + "' AS pref, " +
                                 " TRIM(service) AS service, " +
                                 " ordering AS sort, " +
                                 " SUM(tpl) AS sum_tpl, " +
                                 " SUM(gpl) AS sum_gpl, " +
                                 " SUM(rpl) AS sum_rpl, " +
                                 " SUM(ls) AS sum_ls," +
                                 " SUM(gil) AS sum_gil " +
                          " FROM t_temp_otzhdus1 t INNER JOIN " + prefKernel + "services s ON t.nzp_serv = s.nzp_serv " +
                          " GROUP BY 1,2,3,4 ";
                    ExecSQL(sql);

                    sql = " DELETE FROM t_temp_otzhdus ";
                    ExecSQL(sql);


                    sql = " INSERT INTO t_temp_otzhdkv(nzp_kvar,kvar, count_room, kv_isol, kv_komun, type_kv, kv_tpl, kv_zhpl, rpl, const_gil) " +
                          " SELECT kv.nzp_kvar, " +
                                 " TRIM(kv.nzp_dom||' '||kv.nkvar) AS kvar, " +
                                 " 0 AS count_room, " +
                                 " 1 AS sum_kv_isol, " +
                                 " 0 AS sum_kv_komun, " +
                                 " 0 AS type_kv, " +
                                 " 0 AS sum_kv_tpl, " +
                                 " 0 AS sum_kv_zhpl, " +
                                 " 0 AS rpl, " +
                                 " 0 AS const_gil " +
                            " FROM " + prefData + "kvar kv " +
                            " WHERE 0 = (SELECT COUNT(*) " +
                                       " FROM " + prefData + "prm_2 p " +
                                       " WHERE kv.nzp_dom = p.nzp " +
                                         " AND p.is_actual=1 " +
                                         " AND p.nzp_prm=2029 " +
                                         " AND p.dat_s<='" + firstDay + "' " +
                                         " AND p.dat_po>='" + firstDay + "' " +
                                         " AND date(p.val_prm) <= date('" + firstDay + "')) " +
                            " AND nzp_kvar in (SELECT nzp " +
                                             " FROM " + prefData + "prm_3 pr3 " +
                                             " WHERE pr3.nzp_prm=51 " +  
                                             " AND pr3.is_actual=1 " +
                                             " AND pr3.dat_s<='" + firstDay + "' " +
                                             " AND pr3.dat_po>='" + firstDay + "' " +
                                             " AND pr3.val_prm='1') " +
                              whereArea + 
                          " GROUP BY 1,2 ";
                    ExecSQL(sql);


                    sql = DBManager.sUpdStat + " t_temp_otzhdkv";
                    ExecSQL(sql);

                    sql = " UPDATE t_temp_otzhdkv " +
                          " SET kv_komun = (SELECT DISTINCT 1 " +
                                          " FROM " + prefData + "prm_1 pr1 " +
                                          " WHERE is_actual=1 " +
                                            " AND nzp_prm=3 " +
                                            " AND dat_s<='" + firstDay + "' " +
                                            " AND dat_po>='" + firstDay + "' " +
                                            " AND val_prm='2' " +
                                            " AND pr1.nzp = t_temp_otzhdkv.nzp_kvar) ";
                    ExecSQL(sql);

                    sql = " UPDATE t_temp_otzhdkv " +
                          " SET count_room = (SELECT MAX(pr1.val_prm" + DBManager.sConvToInt + ") AS val_prm " +
                                            " FROM " + prefData + "prm_1 pr1 " +
                                            " WHERE is_actual=1 " +
                                              " AND nzp_prm=107 " +
                                              " AND dat_s<='" + firstDay + "' " +
                                              " AND dat_po>='" + firstDay + "' " +
                                              " AND pr1.nzp = t_temp_otzhdkv.nzp_kvar) ";
                    ExecSQL(sql);

                    sql = " UPDATE t_temp_otzhdkv  SET count_room=0 " +
                          " WHERE count_room IS NULL; ";
                    ExecSQL(sql);

                    sql = " UPDATE t_temp_otzhdkv " +
                          " SET kv_tpl = (SELECT MAX(REPLACE(val_prm,',','.')" + DBManager.sConvToNum + ") " +
                                        " FROM " + prefData + "prm_1 pr1 " +
                                        " WHERE is_actual=1 " +
                                          " AND nzp_prm=4 " +
                                          " AND dat_s<='" + firstDay + "' " +
                                          " AND dat_po>='" + firstDay + "' " +
                                          " AND pr1.nzp = t_temp_otzhdkv.nzp_kvar) ";
                    ExecSQL(sql);

                    sql = " UPDATE t_temp_otzhdkv " +
                          " SET kv_zhpl = (SELECT MAX(REPLACE(val_prm,',','.')" + DBManager.sConvToNum + ") " +
                                         " FROM " + prefData + "prm_1 pr1 " +
                                         " WHERE is_actual=1 " +
                                           " AND nzp_prm=6 " +
                                           " AND dat_s<='" + firstDay + "' " +
                                           " AND dat_po>='" + firstDay + "' " +
                                           " AND pr1.nzp = t_temp_otzhdkv.nzp_kvar) ";
                    ExecSQL(sql);

                    sql = " UPDATE t_temp_otzhdkv " +
                          " SET const_gil = (SELECT MAX(ROUND(gil)) " +
                                           " FROM " + calcGkuTable + " cal " +
                                           " WHERE cal.tarif>0 " +
                                           " AND cal.nzp_kvar = t_temp_otzhdkv.nzp_kvar); ";
                    ExecSQL(sql);

                    sql = " UPDATE t_temp_otzhdkv " +
                          " SET rpl = (SELECT MAX(squ) " +
                                     " FROM " + calcGkuTable + " cal " +
                                     " WHERE cal.tarif>0 " +
                                     " AND cal.nzp_kvar = t_temp_otzhdkv.nzp_kvar); ";
                    ExecSQL(sql);

                    sql = " UPDATE t_temp_otzhdkv " +
                          " SET type_kv = (SELECT MAX(val_prm" + DBManager.sConvToNum + ") " +
                                           " FROM " + prefData + "prm_1 pr1 " +
                                           " WHERE is_actual=1 " +
                                             " AND nzp_prm=2009 " +
                                             " AND dat_s<='" + firstDay + "' " +
                                             " AND dat_po>='" + firstDay + "' " +
                                             " AND pr1.nzp = t_temp_otzhdkv.nzp_kvar) ";
                    ExecSQL(sql);

                    sql = " UPDATE t_temp_otzhdkv SET type_kv=0 WHERE type_kv IS NULL ";
                    ExecSQL(sql);

                    sql = " INSERT INTO t_temp_otzhdkv1 (kvar, count_room, kv_isol, kv_komun, type_kv, kv_tpl, kv_zhpl, rpl, const_gil) " +
                          " SELECT kvar, " +
                                 " SUM(count_room) AS count_room, " +
                                 " SUM(kv_isol) AS kv_isol, " +
                                 " 0 AS kv_komun, " +
                                 " MAX(type_kv) AS type_kv, " +
                                 " SUM(kv_tpl) AS kv_tpl, " +
                                 " SUM(kv_zhpl) AS kv_zhpl, " +
                                 " SUM(rpl) AS rpl, " +
                                 " SUM(const_gil) AS const_gil " +
                          " FROM t_temp_otzhdkv " +
                          " GROUP BY kvar ";
                    ExecSQL(sql);

                    sql = " INSERT INTO t_temp_otzhdkv2(count_room, pref, count_kvar, kv_isol, kv_komun, kv_privat, kv_vtprivat, kv_sluz, kv_sobs, kv_urlic, kv_nepriv, kv_tpl, kv_zhpl, rpl, const_gil) " +
                          " SELECT " + DBManager.sNvlWord + "(count_room,0) AS count_room, " +
                                 " '" + pref + "' AS pref, " +
                                 " COUNT(DISTINCT kvar) AS count_kvar, " +
                                 " SUM(CASE WHEN kv_isol=1 THEN kv_isol ELSE 0 END) AS kv_isol, " +
                                 " 0 AS kv_komun, " +
                                 " SUM(CASE WHEN type_kv = 1 THEN 1 ELSE 0 END) AS kv_privat, " +   
                                 " SUM(CASE WHEN type_kv = 2 THEN 1 ELSE 0 END) AS kv_vtprivat, " +   
                                 " SUM(CASE WHEN type_kv = 3 THEN 1 ELSE 0 END) AS kv_sluz, " +   
                                 " SUM(CASE WHEN type_kv = 4 THEN 1 ELSE 0 END) AS kv_sobs, " +   
                                 " SUM(CASE WHEN type_kv = 5 THEN 1 ELSE 0 END) AS kv_urlic, " +   
                                 " SUM(CASE WHEN type_kv = 0 THEN 1 ELSE 0 END) AS kv_nepriv, " +
                                 " SUM(kv_tpl) AS kv_tpl, " +
                                 " SUM(kv_zhpl) AS kv_zhpl, " +
                                 " SUM(rpl) AS rpl, " +
                                 " SUM(const_gil) AS const_gil " +
                          " FROM t_temp_otzhdkv1 " +
                          " GROUP BY 1, 2 ";
                    ExecSQL(sql);

                    sql = " DELETE FROM t_temp_otzhdkv ";
                    ExecSQL(sql);

                    sql = " DELETE FROM t_temp_otzhdkv1 ";
                    ExecSQL(sql);

                    sql = " INSERT INTO t_report_30_1_6 (pref) " +
                      " VALUES ('" + pref + "') ";
                    ExecSQL(sql);

                    sql = " UPDATE t_report_30_1_6" +
                          " SET nzp_town = ( SELECT MAX(r.nzp_town)" +
                                           " FROM t_temp_otzhdus1 t INNER JOIN " + prefData + "kvar k ON k.nzp_kvar = t.nzp_kvar " +
                                                                  " INNER JOIN " + prefData + "dom d ON k.nzp_dom = d.nzp_dom " +
                                                                  " INNER JOIN " + prefData + "s_ulica u ON d.nzp_ul = u.nzp_ul " +
                                                                  " INNER JOIN " + prefData + "s_rajon r ON r.nzp_raj = u.nzp_raj) " +
                          " WHERE t_report_30_1_6.pref = '" + pref + "' ";
                    ExecSQL(sql);

                    GetNameResponsible(pref, 80); // Имя агента
                    GetNameResponsible(pref, 1291); // Наименование должности директора РЦ
                    GetNameResponsible(pref, 1292); // Наименоваине должности начальника отдела начислений 
                    GetNameResponsible(pref, 1293); // Наименование должности начальника отдела финансов
                    GetNameResponsible(pref, 1294); // ФИО директора РЦ      
                    GetNameResponsible(pref, 1295); // ФИО начальника отдела начислений
                    GetNameResponsible(pref, 1296); // ФИО начальника отдела финансов

                    sql = " DELETE FROM t_temp_otzhdus1 ";
                    ExecSQL(sql);
                }

            }
            reader.Close();
            #endregion

            sql = " SELECT SUM(tpl) AS sum_tpl, " +
                         " SUM(rpl) AS sum_rpl, " +
                         " SUM(const_gil) AS const_gil, " +
                         " SUM(pr_gil) AS sum_pr_gil, " +
                         " SUM(zak_gil) AS sum_zak_gil, " +
                         " SUM(pl_year) AS pl_year, " +
                         " COUNT(DISTINCT nzp_dom) AS count_dom " +
                  " FROM (SELECT nzp_kvar, " +
                               " nzp_dom, " +
                               " MAX(tpl) AS tpl, " +
                               " MAX(pl_year) AS pl_year, " +
                               " MAX(rpl) AS rpl, " +
                               " MAX(const_gil) AS const_gil, " +
                               " MAX(pr_gil) AS pr_gil, " +
                               " MAX(zak_gil) AS zak_gil " +
                        " FROM t_temp_otzhdus3 " +
                        " GROUP BY nzp_kvar, nzp_dom) AS innerselect ";
            DataTable tempParam = ExecSQLToTable(sql);
            foreach (DataRow dr in tempParam.Rows)
            {
                ObchyaPl = dr["sum_tpl"].ToString().Trim();
                RaschetPl = dr["sum_rpl"].ToString().Trim();
                PostProzh = dr["const_gil"].ToString().Trim();
                PropisZhil = dr["sum_pr_gil"].ToString().Trim();
                ZakrLicChet = dr["sum_zak_gil"].ToString().Trim();
                NumberArea = dr["pl_year"].ToString().Trim();
                NumberHouse = dr["count_dom"].ToString().Trim();
            }

            var ds = new DataSet();

            sql = " SELECT  t.nzp_serv, " +
                          " TRIM(pref) AS pref, " +
                          " TRIM(service) AS service," +
                          " sort, " +
                          " SUM(sum_tpl) AS sum_tpl, " +
                          " SUM(sum_gpl) AS sum_gpl, " +
                          " SUM(sum_rpl) AS sum_rpl, " +
                          " SUM(sum_ls) AS sum_ls, " +
                          " SUM(sum_gil) AS sum_gil " +
                  " FROM t_temp_otzhdus2 t " +
                  " GROUP BY t.nzp_serv, pref, service, sort " +
                  " ORDER BY 2, 4 ";

            DataTable dt1 = ExecSQLToTable(sql);
            dt1.TableName = "Q_master1";
            ds.Tables.Add(dt1);

            sql = " SELECT TRIM(pref) AS pref, " +
                         " (CASE WHEN " + DBManager.sNvlWord + "(count_room,0)>=4 THEN 4 ELSE count_room END) AS num_val, " +
                         " (CASE WHEN " + DBManager.sNvlWord + "(count_room,0)>=4 THEN 'в т.ч. 4-комнатных и более' ELSE 'в т.ч. ' || count_room || '-комнатных' END) AS val_prm, " +
                         " SUM(count_kvar) AS sum_kv, " +
                         " SUM(kv_isol) AS sum_kv_isol, " +
                         " SUM(count_kvar - kv_isol) AS sum_kv_komun, " +
                         " SUM(kv_privat) AS sum_kv_privat, " +
                         " SUM(kv_vtprivat) AS sum_kv_vtprivat, " +
                         " SUM(kv_sluz) AS sum_kv_sluz, " +
                         " SUM(kv_sobs) AS sum_kv_sobs, " +
                         " SUM(kv_urlic) AS sum_kv_urlic," +
                         " SUM(kv_nepriv) AS sum_kv_nepriv, " +
                         " SUM(kv_tpl) AS sum_kv_tpl, " +
                         " SUM(kv_zhpl) AS sum_kv_zhpl, " +
                         " SUM(rpl) AS sum_rpl, " +
                         " SUM(const_gil) AS sum_const_gil " +
                 " FROM t_temp_otzhdkv2 t  " +
                 " GROUP BY 1,2,3 " +
                 " ORDER BY 1, 2";

            DataTable dt2 = ExecSQLToTable(sql);
            dt2.TableName = "Q_master2";
            ds.Tables.Add(dt2);

            sql = " SELECT DISTINCT TRIM(t1.pref) AS pref, " +
                         " TRIM(town) AS town, " +
                         " TRIM(name_agent) AS name_agent, " +
                         " TRIM(director_post) AS director_post, " +
                         " TRIM(chief_charge_post) AS chief_charge_post, " +
                         " TRIM(chief_finance_post) AS chief_finance_post, " +
                         " TRIM(director_name) AS director_name, " +
                         " TRIM(chief_charge_name) AS chief_charge_name, " +
                         " TRIM(chief_finance_name) AS chief_finance_name, " +
                         " TRIM('" + ReportParams.User.uname + "') AS executor_name  " +
                  " FROM t_report_30_1_6 t1 INNER JOIN t_temp_otzhdkv2 t2 ON t1.pref = t2.pref " +
                                          " INNER JOIN " + ReportParams.Pref + DBManager.sDataAliasRest + "s_town t ON t.nzp_town = t1.nzp_town " +
                  " ORDER BY 1 ";
            DataTable dt3 = ExecSQLToTable(sql);
            dt3.TableName = "Q_master3";
            ds.Tables.Add(dt3);

            return ds;
        }

        /// <summary>
        /// Получить условия органичения по УК
        /// </summary>
        /// <returns></returns>
        private string GetWhereArea()
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

        /// <summary>
        /// Получить условия органичения по банкам данных
        /// </summary>
        /// <returns></returns>
        private string GetwhereWp()
        {
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
            return whereWp;
        }

        /// <summary>Запись во временную таблицу фамилии ответсвенных</summary>
        /// <param name="pref"> Приставка локального банка в базе данных </param>
        /// <param name="nzpPrm"> Идентификатор параметра </param>
        private void GetNameResponsible(string pref, int nzpPrm)
        {
            int day = Month == DateTime.Now.Month && Year == DateTime.Now.Year ? DateTime.Now.Day : 1;
            string nameColumn = string.Empty;
            switch (nzpPrm)
            {
                case 80: nameColumn = "name_agent"; break;
                case 1291: nameColumn = "director_post"; break;
                case 1292: nameColumn = "chief_charge_post"; break;
                case 1293: nameColumn = "chief_finance_post"; break;
                case 1294: nameColumn = "director_name"; break;
                case 1295: nameColumn = "chief_charge_name"; break;
                case 1296: nameColumn = "chief_finance_name"; break;
            }

            var nameTable = ExecSQLToTable(" SELECT val_prm " + " FROM " + pref + DBManager.sDataAliasRest + "prm_10 " +
               " WHERE is_actual = 1 " +
               " AND dat_s <='" + day + "." + Month + "." + Year + "' " +
               " AND dat_po >='" + day + "." + Month + "." + Year + "' " +
               " AND nzp_prm = " + nzpPrm);

            if (nameTable.Rows.Count == 1)
            {
                string sql = " UPDATE t_report_30_1_6 " +
                             " SET " + nameColumn + " = '" + nameTable.Rows[0][0].ToString().Trim() + "' " +
                             " WHERE t_report_30_1_6.pref = '" + pref + "' ";
                ExecSQL(sql);
            }
            else
            {
                string sql = " UPDATE t_report_30_1_6 " +
                             " SET " + nameColumn + " = '";
                switch (nzpPrm)
                {
                    case 80: sql += "ЖКХ"; break;
                    case 1291: sql += "Врио директора  "; break;
                    case 1292: sql += "Нач. отдела по расщеплению платежей"; break;
                    case 1293: sql += "Нач. отдела бюджетирования и учета"; break;
                    case 1294: sql += "Звягинцев А.В."; break;
                    case 1295: sql += "Миллер Ю.А."; break;
                    case 1296: sql += "Соковых И.А."; break;
                }
                sql += "' WHERE t_report_30_1_6.pref = '" + pref + "' ";
                ExecSQL(sql);
            }
        }

        protected override void CreateTempTable() 
        {
            string sql = " CREATE TEMP TABLE t_temp_otzhdus( " +
                               " nzp_kvar INTEGER, " +
                               " nzp_dom INTEGER, " + 
                               " nzp_serv INTEGER, " +                                      
                               " tpl " + DBManager.sDecimalType + "(14,2), " +          //общая площадь          
                               " gpl " + DBManager.sDecimalType + "(14,2), " +          //жилая площадь
                               " rpl " + DBManager.sDecimalType + "(14,2), " +          //площадь для расчетов
                               " ls INTEGER, " +                                        //кол-во л/с
                               " gil INTEGER, " +                                       //кол-во проживающих
                               " const_gil INTEGER, " +                                 //число постоянно проживающих
                               " pl_year decimal(14,2), " + 
                               " pr_gil INTEGER, " +                                    //число прописанных
                               " zak_gil INTEGER)" + DBManager.sUnlogTempTable;         //число прописанных по закрытым л/с
            ExecSQL(sql);

            sql = "CREATE INDEX ixtzhildomus on t_temp_otzhdus(nzp_kvar)";
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_temp_otzhdus1( " +
                               " nzp_kvar INTEGER, " +
                               " nzp_serv INTEGER, " +
                               " tpl " + DBManager.sDecimalType + "(14,2), " +                    
                               " gpl " + DBManager.sDecimalType + "(14,2), " +          
                               " rpl " + DBManager.sDecimalType + "(14,2), " +          
                               " ls INTEGER, " +                                        
                               " gil INTEGER)" + DBManager.sUnlogTempTable;         
            ExecSQL(sql);


            sql = " CREATE TEMP TABLE t_temp_otzhdus2( " +
                               " pref CHARACTER(100)," +
                               " nzp_serv INTEGER, " +
                               " service CHARACTER(100), " +
                               " sort INTEGER, " +
                               " sum_tpl " + DBManager.sDecimalType + "(14,2), " +
                               " sum_gpl " + DBManager.sDecimalType + "(14,2), " +
                               " sum_rpl " + DBManager.sDecimalType + "(14,2), " +
                               " sum_ls INTEGER, " +
                               " sum_gil INTEGER)" + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_temp_otzhdus3( " +
                               " nzp_kvar INTEGER, " +
                               " nzp_dom INTEGER, " +
                               " tpl " + DBManager.sDecimalType + "(14,2), " +
                               " pl_year " + DBManager.sDecimalType + "(14,2), " +
                               " rpl " + DBManager.sDecimalType + "(14,2), " +
                               " gil INTEGER, " +
                               " pr_gil INTEGER, " +
                               " const_gil INTEGER, " +
                               " zak_gil INTEGER)" + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_temp_otzhdkv( " +
                  " nzp_kvar INTEGER, " +
                  " kvar CHARACTER(20), " +
                  " count_room INTEGER DEFAULT 0, " +                                                       //количество комнат
                  " kv_isol INTEGER DEFAULT 1, " +                                                      //изолированных квартир
                  " kv_komun INTEGER, " +                                                               //коммунальных квартир
                  " type_kv INTEGER, " +                                                                                                                         
                  " kv_tpl " + DBManager.sDecimalType + "(14,2)," +                                     //общая площадь
                  " kv_zhpl " + DBManager.sDecimalType + "(14,2)," +                                    //жилая площадь
                  " rpl " + DBManager.sDecimalType + "(14,2)," +                                        //площадь для расчетов
                  " const_gil INTEGER) " + DBManager.sUnlogTempTable;                     
            ExecSQL(sql);

            sql = "CREATE INDEX ixtzhildomkv on t_temp_otzhdkv(nzp_kvar)";
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_temp_otzhdkv1( " +
                  " kvar CHARACTER(20), " +
                  " count_room INTEGER DEFAULT 0, " +
                  " kv_isol INTEGER DEFAULT 1, " +                                                                
                  " kv_komun INTEGER, " +
                  " type_kv INTEGER, " +                                                                                                                         
                  " kv_tpl " + DBManager.sDecimalType + "(14,2)," +                                     
                  " kv_zhpl " + DBManager.sDecimalType + "(14,2)," +                                    
                  " rpl " + DBManager.sDecimalType + "(14,2)," +                                        
                  " const_gil INTEGER) " + DBManager.sUnlogTempTable;                       
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_temp_otzhdkv2( " +
                  " pref CHARACTER(100)," +
                  " count_kvar INTEGER, " +
                  " count_room INTEGER DEFAULT 0, " +
                  " kv_isol INTEGER DEFAULT 1, " +
                  " kv_komun INTEGER, " +
                  " kv_privat INTEGER, " +                                      //приватизированных квартир
                  " kv_vtprivat INTEGER, " +                                    //втор. приватиз. квартир
                  " kv_sluz INTEGER, " +                                        //служебных квартир
                  " kv_sobs INTEGER, " +                                        //собственников квартир
                  " kv_urlic INTEGER, " +                                       //юр.лиц. квартир
                  " kv_nepriv INTEGER, " +                                      //неприватизированных квартир
                  " kv_tpl " + DBManager.sDecimalType + "(14,2)," +
                  " kv_zhpl " + DBManager.sDecimalType + "(14,2)," +
                  " rpl " + DBManager.sDecimalType + "(14,2)," +
                  " const_gil INTEGER) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_report_30_1_6 ( " +
                        " pref CHARACTER(100)," +
                        " nzp_town INTEGER, " +
                        " director_post CHARACTER(60), " +
                        " chief_charge_post CHARACTER(60), " +
                        " chief_finance_post CHARACTER(60), " +
                        " director_name CHARACTER(30), " +
                        " chief_charge_name CHARACTER(30), " +
                        " chief_finance_name CHARACTER(30), " +
                        " name_agent CHARACTER(30))" + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_temp_otzhdus ");
            ExecSQL(" DROP TABLE t_temp_otzhdus1 ");
            ExecSQL(" DROP TABLE t_temp_otzhdus2 ");
            ExecSQL(" DROP TABLE t_temp_otzhdus3 ");

            ExecSQL(" DROP TABLE t_temp_otzhdkv ");
            ExecSQL(" DROP TABLE t_temp_otzhdkv1 ");
            ExecSQL(" DROP TABLE t_temp_otzhdkv2 ");

            ExecSQL(" DROP TABLE t_report_30_1_6 ");  
        }
    }
}
