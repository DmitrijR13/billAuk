using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Tula.Properties;
using Bars.KP50.Utils;
using Castle.MicroKernel.Registration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Tula.Reports
{
    public class Report71122 : BaseSqlReport
    {
        public override string Name
        {
            get { return "71.1.22 ЖКХ Отчет по итогам деятельности"; }
        }

        public override string Description
        {
            get { return "22-ЖКХ Отчет по итогам деятельности "; }
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

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_71_1_22; }
        }

        /// <summary>Дата с</summary>
        protected DateTime DatS { get; set; }

        /// <summary>Дата по</summary>
        protected DateTime DatPo { get; set; }

        /// <summary>Поставщик</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Заголовок отчета</summary>
        protected string SupplierHeader { get; set; }

        /// <summary>Заголовок территории</summary>
        protected string TerritoryHeader { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }
        /// <summary>Поставщики, Агенты, Принципалы  </summary>
        protected BankSupplierParameterValue BankSupplier { get; set; }



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
                new BankSupplierParameter()
            };
        }

        private class ServiceDictionary
        {
            public ServiceDictionary(int NzpServ, string Service, string ServiceType)
            {
                this.NzpServ = NzpServ;
                this.Service = Service;
                this.ServiceType = ServiceType;
            }

            public int NzpServ { get; set; }
            public string Service { get; set; }
            public string ServiceType { get; set; }
        }
        private class ResoursesDictionary
        {
            public ResoursesDictionary(int NzpRes, string Resourse)
            {
                this.NzpRes = NzpRes;
                this.Resourse = Resourse;
            }

            public int NzpRes { get; set; }
            public string Resourse { get; set; }
        }

        public override DataSet GetData()
        {
            MyDataReader reader;

            var sql = " SELECT * " +
                  " FROM  " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                  " WHERE nzp_wp>1 " + GetwhereWp();
            ExecRead(out reader, sql);

            var servs = new SortedList<int, ServiceDictionary> //для правильной сортировки услуг, ибо fastreport отображает в произвольном порядке
            {
                {0, new ServiceDictionary(0, "из них: плата за пользование жилым помещением (плата за найм)", "Жилищные услуги")},
                {1, new ServiceDictionary(2, "содержание и ремонт жилого помещения", "Жилищные услуги")},
                {2, new ServiceDictionary(0,"в том числе: в жилых домах со всеми видами благоустройства, включая лифты и мусоропроводы", "Жилищные услуги")},
                {3, new ServiceDictionary(0,"в жилых домах со всеми видами благоустройства, кроме лифтов и мусоропроводов", "Жилищные услуги")},
                {4, new ServiceDictionary(16, "вывоз твердых бытовых отходов", "Жилищные услуги")},
                {5, new ServiceDictionary(206, "капитальный ремонт", "Жилищные услуги")},
                {6, new ServiceDictionary(6, "водоснабжение", "Коммунальные услуги")},
                {7, new ServiceDictionary(7, "водоотведение", "Коммунальные услуги")},
                {8, new ServiceDictionary(9, "горячее водоснабжение", "Коммунальные услуги")},
                {9, new ServiceDictionary(8, "отопление", "Коммунальные услуги")},
                {10, new ServiceDictionary(25, "электроснабжение", "Коммунальные услуги")},
                {11, new ServiceDictionary(0, "в том числе: в домах с газовыми плитами", "Коммунальные услуги")},
                {12, new ServiceDictionary(0, "в домах с электроплитами", "Коммунальные услуги")},
                {13, new ServiceDictionary(10, "газоснабжение сетевым газом", "Коммунальные услуги")},
                {14, new ServiceDictionary(0, "газоснабжение сжиженным газом", "Коммунальные услуги")},
                {15, new ServiceDictionary(0, "поставка бытового газа в баллонах", "Коммунальные услуги")},
                {16, new ServiceDictionary(0, "поставка твердого топлива при наличии печного отопления", "Коммунальные услуги")},
                {17, new ServiceDictionary(0, "уголь", "Коммунальные услуги")},
                {18, new ServiceDictionary(0, "дрова", "Коммунальные услуги")}
            };

            foreach (var serv in servs)
            {
                ExecSQL(" insert into t_s3(serv_key, nzp_serv, serv_type, serv) values (" + serv.Key + "," + serv.Value.NzpServ + ",'" + serv.Value.ServiceType + "','" + serv.Value.Service + "')");
            }

            var resourses = new SortedList<int, ResoursesDictionary>
            {
                {0, new ResoursesDictionary(25, "Электрическая энергия, кВт/час")},
                {1, new ResoursesDictionary(8, "Тепловая энергия, Гкал")},
                {2, new ResoursesDictionary(6, "Холодная вода, м3")},
                {3, new ResoursesDictionary(9, "Горячая вода, м3")},
                {4, new ResoursesDictionary(10, "Сетевой газ, м3")},
                {5, new ResoursesDictionary(0, "Сжиженный газ, кг")}
            };

            foreach (var resourse in resourses)
            {
                ExecSQL(" insert into t_s4(serv_key, nzp_serv, res) values (" + resourse.Key + "," + resourse.Value.NzpRes + ",'" + resourse.Value.Resourse + "')");                
            }

            while (reader.Read())
            {
                var pref = reader["bd_kernel"].ToString().ToLower().Trim();
                string whereSupp = GetWhereSupp();
                for (int i = DatS.Year*12 + DatS.Month; i < DatPo.Year*12 + DatPo.Month + 1; i++)
                {
                    var year = i/12;
                    var month = i%12;
                    if (month == 0)
                    {
                        year--;
                        month = 12;
                    }
                    string calcTable = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                         "calc_gku_" + month.ToString("00");
                    string chargeTable = pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter +
                                         "charge_" + month.ToString("00");
                    string prmTableOne = pref + DBManager.sDataAliasRest + "prm_1 ",
                            prmTableTwo = pref + DBManager.sDataAliasRest + "prm_2 ";
                    if (TempTableInWebCashe(calcTable))
                    {
                        sql = " insert into t_section_1 (prefid, nzp_kvar, nzp_supp, serv_6, serv_9, serv_7, serv_8, serv_25, serv_10, serv_101, serv_266) " +
                              " select '" + pref + year + month + "' as prefid, nzp_kvar, nzp_supp, " +
                              " case when nzp_serv = 6 then rashod end as serv_6, " +
                              " case when nzp_serv = 9 then rashod end as serv_9, " +
                              " case when nzp_serv = 7 then rashod end as serv_7, " +
                              " case when nzp_serv = 8 then rashod end as serv_8, " +
                              //" case when nzp_serv = 9 then rashod end as serv_81, " +
                              " case when nzp_serv = 25 then rashod end as serv_25, " +
                              " case when nzp_serv = 10 then rashod end as serv_10, " +
                              " case when nzp_serv = 0 then rashod end as serv_101, " +
                              " case when nzp_serv = 266 then rashod end as serv_266 " +
                              " from " + calcTable +  
                              " where nzp_serv in (6,9,7,8,25,10,266) and stek = 3 and dat_charge is null " + whereSupp;
                        ExecSQL(sql);

                        sql = " update t_section_1 " +
                              " set serv_81 = serv_9 * " + DBManager.sNvlWord + "((SELECT replace(val_prm,',','.') " + DBManager.sConvToNum + " " +
                                                                                 " FROM " + pref + DBManager.sDataAliasRest + "kvar k INNER JOIN " + prmTableTwo + " p ON p.nzp = k.nzp_dom" +
                                                                                 " WHERE k.nzp_kvar = t_section_1.nzp_kvar " +
                                                                                   " AND p.nzp_prm = 436 " + //домовой норматив на 1 ГКал/куб.м. горячей воды
                                                                                   " AND p.is_actual <> 100" +
                                                                                   " AND p.dat_s <= '" + DatPo.ToShortDateString() + "' " +
                                                                                   " AND p.dat_po >= '" + DatS.ToShortDateString() + "' ),0) " +
                              " where serv_9 <> 0 and prefid = '" + pref + year + month + "' ";
                        ExecSQL(sql);
                    }

                    if (TempTableInWebCashe(chargeTable))
                    {
                        sql = " insert into t_section_2_1 (nzp_supp, gil, vod, kan, tepl, electr, gas, liq_gas, util, other) " +
                              " select nzp_supp, " +
                              " case when nzp_serv in (2,16,206) then sum_real + reval + real_charge end as gil, " +
                              " case when nzp_serv = 6 then sum_real + reval + real_charge end as vod, " +
                              " case when nzp_serv = 7 then sum_real + reval + real_charge end as kan, " +
                              " case when nzp_serv = 8 then sum_real + reval + real_charge end as tepl, " +
                              " case when nzp_serv = 25 then sum_real + reval + real_charge end as electr, " +
                              " case when nzp_serv = 10 then sum_real + reval + real_charge end as gas, " +
                              " case when nzp_serv = 0 then sum_real + reval + real_charge end as liq_gas, " +
                              " case when nzp_serv = 266 then sum_real + reval + real_charge end as util, " +
                              " case when nzp_serv not in (2,16,206,6,7,8,25,10,266) then sum_real + reval + real_charge end as other " +
                              " from " + chargeTable +
                              " where nzp_serv > 1 and dat_charge is null " + whereSupp;
                        ExecSQL(sql);

                        sql = " insert into t_section_2_2 (nzp_supp, gil, vod, kan, tepl, electr, gas, liq_gas, util, other) " +
                              " select nzp_supp, " +
                              " case when nzp_serv in (2,16,206) then sum_insaldo end as gil, " +
                              " case when nzp_serv = 6 then sum_insaldo end as vod, " +
                              " case when nzp_serv = 7 then sum_insaldo end as kan, " +
                              " case when nzp_serv = 8 then sum_insaldo end as tepl, " +
                              " case when nzp_serv = 25 then sum_insaldo end as electr, " +
                              " case when nzp_serv = 10 then sum_insaldo end as gas, " +
                              " case when nzp_serv = 0 then sum_insaldo end as liq_gas, " +
                              " case when nzp_serv = 266 then sum_insaldo end as util, " +
                              " case when nzp_serv not in (2,16,206,6,7,8,25,10,266) then sum_insaldo end as other " +
                              " from " + chargeTable +
                              " where nzp_serv > 1 and dat_charge is null " + whereSupp;
                        ExecSQL(sql);
                    }

                    if (TempTableInWebCashe(calcTable) && TempTableInWebCashe(chargeTable))
                    {
                        #region наполнение временной таблицы расчетов и начислений
                        sql = " insert into temp_charge_calc (nzp_kvar, nzp_supp, nzp_serv, nzp_frm, nach, sum_money, cost)" +
                              " select nzp_kvar, nzp_supp, nzp_serv, nzp_frm, " +
                              " sum_real + real_charge + reval as nach, " +
                              " sum_money, " +
                              " sum_real + real_charge + reval as cost " +
                              " from " + chargeTable +
                              " where nzp_serv > 1 and dat_charge is null " + whereSupp;
                        ExecSQL(sql);
                        sql = " insert into temp_charge_calc (nzp_kvar, nzp_supp, nzp_serv, nzp_frm, squ, gil)" +
                              " select nzp_kvar, nzp_supp, nzp_serv, nzp_frm, squ, gil " +
                              " from " + calcTable +
                              " where nzp_serv > 1 and stek = 3 and dat_charge is null " + whereSupp;
                        ExecSQL(sql);
                        sql = " insert into charge_calc (nzp_kvar, nzp_supp, nzp_serv, nzp_frm, nach, sum_money, cost, squ, gil)" +
                              " select nzp_kvar, nzp_supp, nzp_serv, nzp_frm, " +
                              " sum(nach) as nach, " +
                              " sum(sum_money) as sum_money, " +
                              " sum(cost) as cost, " + 
                              " sum(squ) as squ, " +
                              " max(gil) as gil " +
                              " from temp_charge_calc " +
                              " group by 1,2,3,4 ";
                        ExecSQL(sql);
                        #endregion

                        sql = " insert into t_section_3 (nzp_supp, serv_key, serv_type, serv, nach, fact_opl, cost, vozm_tar, vozm_fact, gil_fond, gil_count) " +
                              " select nzp_supp, serv_key, serv_type, serv, " +
                              " nach, " + 
                              " sum_money as fact_opl, " +
                              " cost, " +
                              " sum_money as vozm_tar, " +
                              " sum_money as vozm_fact, " +
                              " squ as gil_fond, " +
                              " gil as gil_count " +
                              " from t_s3 t left outer join charge_calc ch on (ch.nzp_serv = t.nzp_serv) ";
                        ExecSQL(sql);

                        //в жилых домах со всеми видами благоустройства, включая лифты и мусоропроводы
                        sql = " insert into t_section_3 (nzp_supp, serv_key, nach, fact_opl, cost, vozm_tar, vozm_fact, gil_fond, gil_count) " +
                              " select nzp_supp, 2 as serv_key, " +
                              " nach, " +
                              " sum_money as fact_opl, " +
                              " cost, " +
                              " sum_money as vozm_tar, " +
                              " sum_money as vozm_fact, " +
                              " squ as gil_fond, " +
                              " gil as gil_count " +
                              " from charge_calc " +
                              " where nzp_serv = 2 and nzp_kvar in (select nzp_kvar from charge_calc where nzp_serv in (5,19)) ";
                        ExecSQL(sql);
                        //в жилых домах со всеми видами благоустройства, без лифтов и мусоропроводов
                        sql = " insert into t_section_3 (nzp_supp, serv_key, nach, fact_opl, cost, vozm_tar, vozm_fact, gil_fond, gil_count) " +
                              " select nzp_supp, 3 as serv_key, " +
                              " nach, " +
                              " sum_money as fact_opl, " +
                              " cost, " +
                              " sum_money as vozm_tar, " +
                              " sum_money as vozm_fact, " +
                              " squ as gil_fond, " +
                              " gil as gil_count " +
                              " from charge_calc " +
                              " where nzp_serv = 2 and nzp_kvar not in (select nzp_kvar from charge_calc where nzp_serv in (5,19)) ";
                        ExecSQL(sql);
                        //в домах с газовыми плитами
                        sql = " insert into t_section_3 (nzp_supp, serv_key, nach, fact_opl, cost, vozm_tar, vozm_fact, gil_fond, gil_count) " +
                              " select nzp_supp, 11 as serv_key, " +
                              " nach, " +
                              " sum_money as fact_opl, " +
                              " cost, " +
                              " sum_money as vozm_tar, " +
                              " sum_money as vozm_fact, " +
                              " squ as gil_fond, " +
                              " gil as gil_count " +
                              " from charge_calc " +
                              " where nzp_serv = 25 and nzp_kvar in (select nzp from " +
                                prmTableOne + " where nzp_prm = 551 and is_actual = 1 and val_prm = '1' " +
                              " and dat_s <= '" + DatPo.ToShortDateString() + "' and dat_po>= '" + DatS.ToShortDateString() + "') ";
                        ExecSQL(sql);
                        //в домах с электроплитами
                        sql = " insert into t_section_3 (nzp_supp, serv_key, nach, fact_opl, cost, vozm_tar, vozm_fact, gil_fond, gil_count) " +
                              " select nzp_supp, 12 as serv_key, " +
                              " nach, " +
                              " sum_money as fact_opl, " +
                              " cost, " +
                              " sum_money as vozm_tar, " +
                              " sum_money as vozm_fact, " +
                              " squ as gil_fond, " +
                              " gil as gil_count " +
                              " from charge_calc " +
                              " where nzp_serv = 25 and nzp_kvar in (select nzp from " +
                                prmTableOne + " where nzp_prm = 19 and is_actual = 1 and val_prm = '1' " +
                              " and dat_s <= '" + DatPo.ToShortDateString() + "' and dat_po>= '" + DatS.ToShortDateString() + "') ";
                        ExecSQL(sql);

                        ExecSQL(" delete from temp_charge_calc ");
                        ExecSQL(" delete from charge_calc ");
                    }

                    if (TempTableInWebCashe(prmTableOne) && TempTableInWebCashe(calcTable))
                    {
                        sql = " insert into temp_calc (nzp_kvar, nzp_supp, nzp_serv, res_use, gil_count) " +
                              " select c.nzp_kvar, nzp_supp, nzp_serv, " +
                              " sum(rashod) as res_use, " +
                              " sum(round(gil)) as gil_count " + 
                              " from " + calcTable + " c INNER JOIN " + pref + DBManager.sDataAliasRest + "kvar k ON k.nzp_kvar = c.nzp_kvar " +
                              " where dat_charge is null and stek = 3 " + whereSupp +
                              " and k.nzp_dom in (select nzp from " + prmTableTwo + " where nzp_prm = 2030 and val_prm = '1' and is_actual = 1 " +
                              " and dat_s <= '" + DatPo.ToShortDateString() + "' and dat_po>= '" + DatS.ToShortDateString() + "') " + 
                              " group by 1,2,3 ";
                        ExecSQL(sql);

                        sql = " insert into t_section_4 (nzp_supp, serv_key, res, res_use, ob_s, gil_count)" +
                              " select nzp_supp, serv_key, res, res_use, " +
                              " sum(p1.val_prm " + DBManager.sConvToNum + ") as ob_s, gil_count " +
                              " from t_s4 t left outer join temp_calc c on (t.nzp_serv = c.nzp_serv) " +
                              " left outer join " + prmTableOne + " p1 on (c.nzp_kvar = p1.nzp and p1.nzp_prm = 4 and p1.is_actual = 1) " +
                              " group by 1,2,3,4,6 ";
                        ExecSQL(sql);

                        ExecSQL(" delete from temp_calc ");
                    }
                }
            }

            sql = " select " +
                " sum(serv_6) as serv_6, " +
                " sum(serv_9) as serv_9, " +
                " sum(serv_7) as serv_7, " +
                " sum(serv_8) as serv_8, " +
                " sum(serv_81) as serv_81, " +
                " sum(serv_25) as serv_25, " +
                " sum(serv_10) as serv_10, " +
                " sum(serv_101) as serv_101, " +
                " sum(serv_266) as serv_266 " +
                " from t_section_1 ";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master"; 
           
            sql = " select " +
                " sum(gil) as gil, " + 
                " sum(vod) as vod, " + 
                " sum(kan) as kan, " + 
                " sum(tepl) as tepl, " + 
                " sum(electr) as electr, " + 
                " sum(gas) as gas, " + 
                " sum(liq_gas) as liq_gas, " + 
                " sum(util) as util, " +
                " sum(other) as other " +
                " from t_section_2_1 ";

            DataTable dt1 = ExecSQLToTable(sql);
            dt1.TableName = "Q_master1";

            sql = " select " +
                " sum(gil) as gil, " +
                " sum(vod) as vod, " +
                " sum(kan) as kan, " +
                " sum(tepl) as tepl, " +
                " sum(electr) as electr, " +
                " sum(gas) as gas, " +
                " sum(liq_gas) as liq_gas, " +
                " sum(util) as util, " +
                " sum(other) as other " +
                " from t_section_2_2 ";

            DataTable dt2 = ExecSQLToTable(sql);
            dt2.TableName = "Q_master2";
            
            sql = " select serv_key ,serv_type, serv, " +
                " sum(nach) as nach, " + 
                " sum(fact_opl) as fact_opl, " + 
                " sum(cost) as cost, " + 
                " sum(vozm_tar) as vozm_tar, " + 
                " sum(vozm_fact) as vozm_fact, " + 
                " sum(gil_fond) gil_fond, " + 
                " sum(gil_count) as gil_count " +
                " from t_section_3 " +
                " group by 1,2,3 " +
                " order by 1 ";

            DataTable dt3 = ExecSQLToTable(sql);
            dt3.TableName = "Q_master3";

            sql = " select serv_key, res, " + 
                " sum(res_use) as res_use, " + 
                " sum(ob_s) as ob_s, " + 
                " sum(gil_count) as gil_count " +
                " from t_section_4 " +
                " group by 1,2 " +
                " order by 1 ";

            DataTable dt4 = ExecSQLToTable(sql);
            dt4.TableName = "Q_master4";


            var ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables.Add(dt1);
            ds.Tables.Add(dt2);
            ds.Tables.Add(dt3);
            ds.Tables.Add(dt4);

            return ds;
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp()
        {
            string whereSupp = String.Empty;
            if (BankSupplier != null && BankSupplier.Suppliers != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Suppliers.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_supp in (" + supp.TrimEnd(',') + ")";
            }

            if (BankSupplier != null && BankSupplier.Principals != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Principals.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_princip in (" + supp.TrimEnd(',') + ")";
            }
            if (BankSupplier != null && BankSupplier.Agents != null)
            {

                string supp = string.Empty;
                supp = BankSupplier.Agents.Aggregate(supp, (current, nzpSupp) => current + (nzpSupp + ","));
                whereSupp += " and nzp_payer_agent in (" + supp.TrimEnd(',') + ")";
            }

            string oldsupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);

            whereSupp = whereSupp.TrimEnd(',');


            if (!String.IsNullOrEmpty(whereSupp) || !String.IsNullOrEmpty(oldsupp))
            {
                if (!String.IsNullOrEmpty(oldsupp))
                    whereSupp += " AND nzp_supp in (" + oldsupp + ")";

                //Поставщики
                SupplierHeader = String.Empty;
                string sql = " SELECT name_supp from " +
                             ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                             " WHERE nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SupplierHeader += "(" + dr["name_supp"].ToString().Trim() + "), ";
                }
                SupplierHeader = SupplierHeader.TrimEnd(',', ' ');
            }
            return " and nzp_supp in (select nzp_supp from " +
                   ReportParams.Pref + DBManager.sKernelAliasRest + "supplier " +
                   " where nzp_supp>0 " + whereSupp + ")";
        }

        /// <summary>
        /// Получить условия органичения по банкам данных
        /// </summary>
        /// <returns></returns>
        private string GetwhereWp()
        {
            string whereWp = String.Empty;
            if (BankSupplier != null && BankSupplier.Banks != null)
            {
                whereWp = BankSupplier.Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","));
            }
            else
            {
                whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            }
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            if (!string.IsNullOrEmpty(whereWp))
            {
                TerritoryHeader = String.Empty;
                string sql = " SELECT point FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point WHERE nzp_wp > 0 " + whereWp;
                DataTable terrTable = ExecSQLToTable(sql);
                foreach (DataRow row in terrTable.Rows)
                {
                    TerritoryHeader += row["point"].ToString().Trim() + ", ";
                }
                TerritoryHeader = TerritoryHeader.TrimEnd(',', ' ');
            }
            return whereWp;
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            report.SetParameterValue("period", "с " + DatS.ToShortDateString() + " по " + DatPo.ToShortDateString());
            report.SetParameterValue("date", DateTime.Now.ToShortDateString());
            report.SetParameterValue("time", DateTime.Now.ToShortTimeString());

            string headerParam = !string.IsNullOrEmpty(TerritoryHeader) ? "Территория: " + TerritoryHeader + "\n" : string.Empty;
            headerParam += !string.IsNullOrEmpty(SupplierHeader) ? "Поставщики: " + SupplierHeader : string.Empty;
            headerParam = headerParam.TrimEnd('\n');
            report.SetParameterValue("headerParam", headerParam);
        }

        protected override void PrepareParams()
        {
            DateTime begin;
            DateTime end;
            var period = UserParamValues["Period"].GetValue<string>();
            PeriodParameter.GetValues(period, out begin, out end);
            DatS = begin;
            DatPo = end;

            BankSupplier = JsonConvert.DeserializeObject<BankSupplierParameterValue>(UserParamValues["BankSupplier"].Value.ToString());
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_section_1 (     " +
                        " prefid character(20), " +
                        " nzp_kvar integer, " +
                        " nzp_supp integer, " +
                        " serv_6 " + DBManager.sDecimalType + "(14,2), " +
                        " serv_9 " + DBManager.sDecimalType + "(14,2) default 0, " +
                        " serv_7 " + DBManager.sDecimalType + "(14,2), " +
                        " serv_8 " + DBManager.sDecimalType + "(14,2), " +
                        " serv_81 " + DBManager.sDecimalType + "(14,2), " +
                        " serv_25 " + DBManager.sDecimalType + "(14,2), " +
                        " serv_10 " + DBManager.sDecimalType + "(14,2), " +
                        " serv_101 " + DBManager.sDecimalType + "(14,2), " +
                        " serv_266 " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_section_2_1 (     " +
                        " nzp_supp integer, " +
                        " gil " + DBManager.sDecimalType + "(14,2), " +
                        " vod " + DBManager.sDecimalType + "(14,2), " +
                        " kan " + DBManager.sDecimalType + "(14,2), " +
                        " tepl " + DBManager.sDecimalType + "(14,2), " +
                        " electr " + DBManager.sDecimalType + "(14,2), " +
                        " gas " + DBManager.sDecimalType + "(14,2), " +
                        " liq_gas " + DBManager.sDecimalType + "(14,2), " +
                        " util " + DBManager.sDecimalType + "(14,2), " +
                        " other " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_section_2_2 (     " +
                        " nzp_supp integer, " +
                        " gil " + DBManager.sDecimalType + "(14,2), " +
                        " vod " + DBManager.sDecimalType + "(14,2), " +
                        " kan " + DBManager.sDecimalType + "(14,2), " +
                        " tepl " + DBManager.sDecimalType + "(14,2), " +
                        " electr " + DBManager.sDecimalType + "(14,2), " +
                        " gas " + DBManager.sDecimalType + "(14,2), " +
                        " liq_gas " + DBManager.sDecimalType + "(14,2), " +
                        " util " + DBManager.sDecimalType + "(14,2), " +
                        " other " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_section_3 (     " +
                        " nzp_supp integer, " +
                        " serv_key integer, " +
                        " serv_type character(100), " +
                        " serv character(100), " +
                        " nach " + DBManager.sDecimalType + "(14,2), " +
                        " fact_opl " + DBManager.sDecimalType + "(14,2), " +
                        " cost " + DBManager.sDecimalType + "(14,2), " +
                        " vozm_tar " + DBManager.sDecimalType + "(14,2), " +
                        " vozm_fact " + DBManager.sDecimalType + "(14,2), " +
                        " gil_fond " + DBManager.sDecimalType + "(14,2), " +
                        " gil_count integer) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_section_4 (     " +
                        " nzp_supp integer, " +
                        " serv_key integer, " +
                        " res character(100), " +
                        " res_use " + DBManager.sDecimalType + "(14,2), " +
                        " ob_s " + DBManager.sDecimalType + "(14,2), " +
                        " gil_count integer) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);


            ExecSQL(" create temp table t_s3 (serv_key integer, nzp_serv integer, serv_type character(100), serv character(100)) " + DBManager.sUnlogTempTable);
            ExecSQL(" create temp table t_s4 (serv_key integer, nzp_serv integer, res character(100)) " + DBManager.sUnlogTempTable);


            sql = " create temp table temp_charge_calc ( " +
                  " nzp_kvar integer, " +
                  " nzp_supp integer, " +
                  " nzp_serv integer, " +
                  " nzp_frm integer, " +
                  " nach " + DBManager.sDecimalType + "(14,2), " +
                  " sum_money " + DBManager.sDecimalType + "(14,2), " +
                  " cost " + DBManager.sDecimalType + "(14,2), " +
                  " squ " + DBManager.sDecimalType + "(14,7), " +
                  " gil " + DBManager.sDecimalType + "(14,7)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " create temp table charge_calc ( " +
                  " nzp_kvar integer, " +
                  " nzp_supp integer, " +
                  " nzp_serv integer, " +
                  " nzp_frm integer, " +
                  " nach " + DBManager.sDecimalType + "(14,2), " +
                  " sum_money " + DBManager.sDecimalType + "(14,2), " +
                  " cost " + DBManager.sDecimalType + "(14,2), " +
                  " squ " + DBManager.sDecimalType + "(14,7), " +
                  " gil " + DBManager.sDecimalType + "(14,7)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " create temp table temp_calc ( " +
                  " nzp_kvar integer, " +
                  " nzp_supp integer, " +
                  " nzp_serv integer, " +
                  " res_use " + DBManager.sDecimalType + "(14,7), " +
                  " gil_count " + DBManager.sDecimalType + "(14,7)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_section_1 ", true);
            ExecSQL(" drop table t_section_2_1 ", true);
            ExecSQL(" drop table t_section_2_2 ", true);
            ExecSQL(" drop table t_section_3 ", true);
            ExecSQL(" drop table t_section_4 ", true);
            ExecSQL(" drop table t_s3 ", true);
            ExecSQL(" drop table t_s4 ", true);
            ExecSQL(" drop table charge_calc ", true);
            ExecSQL(" drop table temp_charge_calc ", true);
            ExecSQL(" drop table temp_calc ", true);
        }

    }
}

