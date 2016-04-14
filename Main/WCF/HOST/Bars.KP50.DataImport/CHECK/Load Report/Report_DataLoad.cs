using System;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.CHECK.Load_Report
{
// ReSharper disable once InconsistentNaming
    public class Report_DataLoad : ReportLoadTemplate
    {
// ReSharper disable InconsistentNaming
        private string t_general { get { return "t_general_check" + NzpFile; } }
        private string t_sq { get { return "t_sq_check" + NzpFile; } }
        private string t_formul { get { return "t_formul_check" + NzpFile; } }
        private string t_svod_serv { get { return "t_svod_serv_check" + NzpFile; } }
        private string t_svod_supp { get { return "t_svod_supp_check" + NzpFile; } }
        private string t_svod_address { get { return "t_svod_address_check" + NzpFile; } }
// ReSharper restore InconsistentNaming


        public Report_DataLoad()
        {
            reportFrxSource = "Unload.frx";
            fileName = "Сводный отчет по загруженным данным";
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE " + t_general + "(" +
                            " total_sq " + DBManager.sDecimalType + "(14,2)," +
                            " kol_ls INTEGER," +
                            " kol_komm_kv INTEGER," +
                            " kol_odpu INTEGER," +
                            " kol_ipu INTEGER)";
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE " + t_sq + "(" +
                         " nzp_supp INTEGER," +
                         " nzp_serv INTEGER," +
                         " total_sq " + DBManager.sDecimalType + "(14,2)," +
                         " otap_sq " + DBManager.sDecimalType + "(14,2))";
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE " + t_formul + "(" +
                         " supp CHAR(100)," +
                         " serv CHAR(100)," +
                         " formul INTEGER)";
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE " + t_svod_serv + "( " +
                  " nzp_serv INTEGER," +
                  " sum_insaldo " + DBManager.sDecimalType + "(14,2)," +
                  " sum_insaldo_k " + DBManager.sDecimalType + "(14,2)," +
                  " sum_insaldo_d " + DBManager.sDecimalType + "(14,2)," +
                  " sum_real " + DBManager.sDecimalType + "(14,2)," +
                  " reval " + DBManager.sDecimalType + "(14,2)," +
                  " reval_charge " + DBManager.sDecimalType + "(14,2)," +
                  " real_charge " + DBManager.sDecimalType + "(14,2)," +
                  " sum_outsaldo " + DBManager.sDecimalType + "(14,2)," +
                  " sum_outsaldo_k " + DBManager.sDecimalType + "(14,2)," +
                  " sum_outsaldo_d " + DBManager.sDecimalType + "(14,2)," +
                  " sum_money " + DBManager.sDecimalType + "(14,2))";
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE " + t_svod_supp + "( " +
                  " nzp_supp INTEGER," +
                  " sum_insaldo " + DBManager.sDecimalType + "(14,2)," +
                  " sum_insaldo_k " + DBManager.sDecimalType + "(14,2)," +
                  " sum_insaldo_d " + DBManager.sDecimalType + "(14,2)," +
                  " sum_real " + DBManager.sDecimalType + "(14,2)," +
                  " reval " + DBManager.sDecimalType + "(14,2)," +
                  " reval_charge " + DBManager.sDecimalType + "(14,2)," +
                  " real_charge " + DBManager.sDecimalType + "(14,2)," +
                  " sum_outsaldo " + DBManager.sDecimalType + "(14,2)," +
                  " sum_outsaldo_k " + DBManager.sDecimalType + "(14,2)," +
                  " sum_outsaldo_d " + DBManager.sDecimalType + "(14,2)," +
                  " sum_money " + DBManager.sDecimalType + "(14,2))";
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE " + t_svod_address + "( " +
                         " nzp_dom INTEGER," +
                         " nzp_supp INTEGER," +
                         " nzp_serv INTEGER," +
                         " sum_insaldo " + DBManager.sDecimalType + "(14,2)," +
                         " sum_insaldo_k " + DBManager.sDecimalType + "(14,2)," +
                         " sum_insaldo_d " + DBManager.sDecimalType + "(14,2)," +
                         " sum_real " + DBManager.sDecimalType + "(14,2)," +
                         " reval " + DBManager.sDecimalType + "(14,2)," +
                         " reval_charge " + DBManager.sDecimalType + "(14,2)," +
                         " real_charge " + DBManager.sDecimalType + "(14,2)," +
                         " sum_outsaldo " + DBManager.sDecimalType + "(14,2)," +
                         " sum_outsaldo_k " + DBManager.sDecimalType + "(14,2)," +
                         " sum_outsaldo_d " + DBManager.sDecimalType + "(14,2)," +
                         " sum_money " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE " + t_general);
            ExecSQL(" DROP TABLE " + t_sq);
            ExecSQL(" DROP TABLE " + t_formul);
            ExecSQL(" DROP TABLE " + t_svod_serv);
            ExecSQL(" DROP TABLE " + t_svod_supp);
            ExecSQL(" DROP TABLE " + t_svod_address);
        }

        protected override void FillTempTable()
        {
            #region Общие показатели Q_mester1

            string sql = " INSERT INTO " + t_general + " (total_sq, kol_ls, kol_komm_kv, kol_odpu, kol_ipu)" +
                         " VALUES " +
                         " ((SELECT SUM(val_prm " + DBManager.sConvToNum + ") " +
                           " FROM " + LoadPrefData + "prm_1 " +
                           " WHERE is_actual <> 100 " +
                             " AND nzp_prm = 4 " +
                             " AND user_del = " + NzpFile + " " +
                             " AND dat_s <= '" + CalcMonth.ToShortDateString() + "' " +
                             " AND dat_po > '" + CalcMonth.ToShortDateString() + "'), " +
                          " (SELECT COUNT(*) " +
                           " FROM " + PrefUpload + " file_kvar " +
                           " WHERE nzp_file = " + NzpFile + 
                             " AND " + DBManager.sNvlWord + "(nzp_kvar,0) > 0), " +
                          " (SELECT COUNT(*) " +
                           " FROM " + LoadPrefData + "prm_1 " +
                           " WHERE is_actual <> 100 " +
                             " AND nzp_prm = 3 " +
                             " AND (val_prm" + DBManager.sConvToInt + ") = 1 " +
                             " AND (val_prm " + DBManager.sConvToInt + ") = 2 " +
                             " AND user_del = " + NzpFile + 
                             " AND dat_s <= '" + CalcMonth.ToShortDateString() + "' " +
                             " AND dat_po > '" + CalcMonth.ToShortDateString() + "'), " +
                          " (SELECT COUNT(*) " +
                           " FROM " + PrefData + "counters_spis " +
                           " WHERE nzp_type = 1 " +
                             " AND user_block = " + NzpFile + " ), " +
                          " (SELECT count(*) " +
                           " FROM " + PrefData + "counters_spis " +
                           " WHERE nzp_type = 3 " +
                             " AND user_block = " + NzpFile + " ))";
            ExecSQL(sql);
            #endregion 
            
            #region Площади в разрезе поставщик/услуга Q_master2
            //заполняем nzp_supp, nzp_serv
            sql = " INSERT INTO  " + t_sq + " (nzp_supp, nzp_serv) " +
                  " SELECT DISTINCT t.nzp_supp, t.nzp_serv " +
                  " FROM " + LoadPrefData + "tarif t " +
                  " WHERE t.dat_s <= '" + CalcMonth.ToShortDateString() + "' " +
                    " AND t.dat_po > '" + CalcMonth.ToShortDateString() + "' " +
                    " AND t.nzp_kvar IN (SELECT nzp_kvar " +
                                       " FROM " + PrefUpload + " file_kvar " +
                                       " WHERE nzp_file = " + NzpFile + 
                                         " AND " + DBManager.sNvlWord + "(nzp_kvar,0) > 0)";
            ExecSQL(sql);

            //заполняем total_sq
            sql = " UPDATE " + t_sq + " set total_sq = " + 
                  " (SELECT " + DBManager.sNvlWord + "(SUM(p1.val_prm " + DBManager.sConvToNum + "),0) " +
                   " FROM " + LoadPrefData + "prm_1 p1" +
                   " WHERE p1.is_actual <> 100 " +
                     " AND p1.user_del = " + NzpFile +
                     " AND p1.nzp_prm = 4 " +
                     " AND p1.dat_s <= '" + CalcMonth.ToShortDateString() + "' " +
                     " AND p1.dat_po > '" + CalcMonth.ToShortDateString() + "'" +
                     " AND p1.nzp IN (SELECT nzp_kvar " +
                                    " FROM " + LoadPrefData + "tarif t " +
                                    " WHERE " + t_sq + ".nzp_supp = t.nzp_supp " +
                                      " AND " + t_sq + ".nzp_serv = t.nzp_serv " +
                                      " AND dat_s <= '" + CalcMonth.ToShortDateString() + "' " +
                                      " AND dat_po > '" + CalcMonth.ToShortDateString() + "'))";
            ExecSQL(sql);

            //заполняем total_sq
            sql = " UPDATE " + t_sq + " set otap_sq = " +
                  " (SELECT " + DBManager.sNvlWord + "(SUM(p1.val_prm " + DBManager.sConvToNum + "),0) " +
                   " FROM " + LoadPrefData + "prm_1 p1 " +
                   " WHERE p1.is_actual <> 100 " +
                     " AND p1.user_del = " + NzpFile +
                     " AND p1.nzp_prm = 133 " +
                     " AND p1.dat_s <= '" + CalcMonth.ToShortDateString() + "' " +
                     " AND p1.dat_po > '" + CalcMonth.ToShortDateString() + "' " +
                     " AND p1.nzp IN (SELECT nzp_kvar " +
                                    " FROM " + LoadPrefData + "tarif t " +
                                    " WHERE " + t_sq + ".nzp_supp = t.nzp_supp " +
                                      " AND " + t_sq + ".nzp_serv = t.nzp_serv " +
                                      " AND dat_s <= '" + CalcMonth.ToShortDateString() + "' " +
                                      " AND dat_po > '" + CalcMonth.ToShortDateString() + "'))";
            ExecSQL(sql);
            #endregion

            #region Формулы Q_master3
            sql = " INSERT INTO " + t_formul + "(supp, serv, formul) " +
                  " SELECT DISTINCT su.name_supp AS supp, se.service AS serv, t.nzp_frm AS formul " +
                  " FROM " + PrefKernel + "supplier su, " +
                             PrefKernel + "services se, " +
                             LoadPrefData + "tarif t " +
                  " WHERE t.nzp_supp = su.nzp_supp " +
                    " AND t.nzp_serv = se.nzp_serv" +
                    " AND t.dat_s <= '" + CalcMonth.ToShortDateString() + "' " +
                    " AND t.dat_po > '" + CalcMonth.ToShortDateString() + "' " +
                    " AND t.nzp_kvar IN (SELECT nzp_kvar " +
                                       " FROM " + PrefUpload + "file_kvar " +
                                       " WHERE nzp_file = " + NzpFile + 
                                         " AND " + DBManager.sNvlWord + "(nzp_kvar,0) > 0) ";
            ExecSQL(sql);
            #endregion 
            
            #region Сводный по услуге  Q_master4
            string tableCharge = Bank.pref + "_charge_" + (Year - 2000).ToString("00") +
                                 DBManager.tableDelimiter + "charge_" +
                                 Month.ToString("00");

            if (TempTableInWebCashe(tableCharge))
            {
                sql = " INSERT INTO " + t_svod_serv + "(nzp_serv, sum_insaldo_k, sum_insaldo_d, sum_insaldo, sum_real, reval, real_charge, sum_money, " +
                                                        " sum_outsaldo_k, sum_outsaldo_d, sum_outsaldo)" +
                      " SELECT nzp_serv , " +
                             " SUM(CASE WHEN sum_insaldo < 0 THEN sum_insaldo ELSE 0 END) AS sum_insaldo_k, " +
                             " SUM(CASE WHEN sum_insaldo < 0 THEN 0 ELSE sum_insaldo END) AS sum_insaldo_d, " +
                             " SUM(sum_insaldo) AS sum_insaldo, " +
                             " SUM(sum_real) AS sum_real, " +
                             " SUM(reval) AS reval, " +
                             " SUM(real_charge) AS real_charge, " +
                             " SUM(sum_money) AS sum_money, " +
                             " SUM(CASE WHEN sum_outsaldo < 0 THEN sum_outsaldo ELSE 0 END) AS sum_outsaldo_k, " +
                             " SUM(CASE WHEN sum_outsaldo < 0 THEN 0 ELSE sum_outsaldo END) AS sum_outsaldo_d, " +
                             " SUM(sum_outsaldo) AS sum_outsaldo " +
                      " FROM " + tableCharge + " a " +
                      " WHERE nzp_serv > 1 " +
                        " AND dat_charge IS NULL " +
                        " AND nzp_kvar IN (SELECT nzp_kvar " +
                                         " FROM " + PrefUpload + " file_kvar " +
                                         " WHERE nzp_file = " + NzpFile + 
                                           " AND " + DBManager.sNvlWord + "(nzp_kvar,0) > 0) " +
                      " GROUP BY 1 ";
                ExecSQL(sql);
            }
            #endregion

            #region Сводный по поставщикам Q_master5
            if (TempTableInWebCashe(tableCharge))
            {
                sql = " INSERT INTO " + t_svod_supp + "(nzp_supp, sum_insaldo_k, sum_insaldo_d, sum_insaldo, sum_real, reval, real_charge, sum_money, " +
                                                            " sum_outsaldo_k, sum_outsaldo_d, sum_outsaldo) " +
                      " select nzp_supp , " +
                             " SUM(CASE WHEN sum_insaldo < 0 THEN sum_insaldo ELSE 0 END) AS sum_insaldo_k, " +
                             " SUM(CASE WHEN sum_insaldo < 0 THEN 0 ELSE sum_insaldo END) AS sum_insaldo_d, " +
                             " SUM(sum_insaldo) AS sum_insaldo, " +
                             " SUM(sum_real) AS sum_real, " +
                             " SUM(reval) AS reval, " +
                             " SUM(real_charge) AS real_charge, " +
                             " SUM(sum_money) AS sum_money, " +
                             " SUM(CASE WHEN sum_outsaldo < 0 THEN sum_outsaldo ELSE 0 END) AS sum_outsaldo_k, " +
                             " SUM(CASE WHEN sum_outsaldo < 0 THEN 0 ELSE sum_outsaldo END) AS sum_outsaldo_d, " +
                             " SUM(sum_outsaldo) AS sum_outsaldo " +
                      " FROM " + tableCharge + " a " +
                      " WHERE nzp_serv > 1 " +
                        " AND dat_charge IS NULL " +
                        " AND nzp_kvar IN (SELECT nzp_kvar " +
                                         " FROM " + Points.Pref + DBManager.sUploadAliasRest + " file_kvar " +
                                         " WHERE nzp_file = " + NzpFile + 
                                           " AND " + DBManager.sNvlWord + "(nzp_kvar,0) > 0) " +
                      " GROUP BY 1 ";
                ExecSQL(sql);
            }
            #endregion

            #region Сводный по поставщикам Q_master6
            if (TempTableInWebCashe(tableCharge))
            {
                sql = " INSERT INTO " + t_svod_address + "(nzp_dom, nzp_serv, nzp_supp, sum_insaldo_k, sum_insaldo_d, sum_insaldo, " +
                                                        " sum_real, reval, real_charge, sum_money, " +
                                                        " sum_outsaldo_k, sum_outsaldo_d, sum_outsaldo)" +
                      " SELECT nzp_dom, " +
                             " nzp_serv, " +
                             " nzp_supp, " +
                             " SUM(CASE WHEN sum_insaldo < 0 THEN sum_insaldo ELSE 0 END) AS sum_insaldo_k," +
                             " SUM(CASE WHEN sum_insaldo < 0 THEN 0 ELSE sum_insaldo END) AS sum_insaldo_d," +
                             " SUM(sum_insaldo) AS sum_insaldo," +
                             " SUM(sum_real) AS sum_real," +
                             " SUM(reval) AS reval," +
                             " SUM(real_charge) AS real_charge," +
                             " SUM(sum_money) AS sum_money," +
                             " SUM(CASE WHEN sum_outsaldo < 0 THEN sum_outsaldo ELSE 0 END) AS sum_outsaldo_k," +
                             " SUM(CASE WHEN sum_outsaldo < 0 THEN 0 ELSE sum_outsaldo END) AS sum_outsaldo_d," +
                             " SUM(sum_outsaldo) AS sum_outsaldo" +
                      " FROM " + tableCharge + " a INNER JOIN " + PrefData + "kvar k ON k.nzp_kvar = a.nzp_kvar " +
                      " WHERE nzp_serv > 1 " +
                        " AND dat_charge IS NULL " +
                        " AND a.nzp_kvar IN (SELECT nzp_kvar " +
                                         " FROM " + PrefUpload + " file_kvar " +
                                         " WHERE nzp_file = " + NzpFile + 
                                           " AND " + DBManager.sNvlWord + "(nzp_kvar,0) > 0)" +
                      " GROUP BY 1, 2, 3";
                ExecSQL(sql);
            }

            #endregion
        }

        protected override void SetParamValues(FastReport.Report rep)
        {
            var months = new[] {"","Январь","Февраль","Март","Апрель","Май","Июнь",
                                   "Июль","Август","Сентябрь","Октябрь","Ноябрь","Декабрь"};
            rep.SetParameterValue("period_month", months[Month] + " " + Convert.ToInt32(Year));
            rep.SetParameterValue("file_name", FileName);
        }

        protected override DataSet AddDataSource()
        {
            string sql = " SELECT total_sq, kol_ls, kol_komm_kv, kol_odpu, kol_ipu FROM " + t_general;
            DataTable dt1 = ExecSQLToTable(sql);
            dt1.TableName = "Q_master1";

            sql = " SELECT su.name_supp AS supp, se.service AS serv , t.total_sq, t.otap_sq " +
                  " FROM " + t_sq + " t, " +
                             PrefKernel + "supplier su, " +
                             PrefKernel + "services se " +
                  " WHERE su.nzp_supp = t.nzp_supp " +
                    " AND se.nzp_serv = t.nzp_serv ";
            DataTable dt2 = ExecSQLToTable(sql);
            dt2.TableName = "Q_master2";

            sql = " SELECT supp, serv, formul FROM " + t_formul;
            DataTable dt3 = ExecSQLToTable(sql);
            dt3.TableName = "Q_master3";

            sql = " SELECT service, " +
                         " SUM(sum_insaldo_k) AS sum_insaldo_k, " +
                         " SUM(sum_insaldo_d) AS sum_insaldo_d, " +
                         " SUM(sum_insaldo) AS sum_insaldo, " +
                         " SUM(sum_real) AS sum_real, " +
                         " SUM(reval) AS reval, " +
                         " SUM(real_charge) AS real_charge," +
                         " SUM(reval) + sum(real_charge) AS reval_charge, " +
                         " SUM(sum_money) AS sum_money, " +
                         " SUM(sum_outsaldo_k) AS sum_outsaldo_k, " +
                         " SUM(sum_outsaldo_d) AS sum_outsaldo_d, " +
                         " SUM(sum_outsaldo) AS sum_outsaldo " +
                " FROM " + t_svod_serv + " a, " +
                           PrefKernel + "services s " +
                " WHERE a.nzp_serv = s.nzp_serv " +
                " GROUP BY service " +
                " ORDER BY service ";
            DataTable dt4 = ExecSQLToTable(sql);
            dt4.TableName = "Q_master4";

            sql = " select name_supp AS supplier, " +
                         " SUM(sum_insaldo_k) AS sum_insaldo_k, " +
                         " SUM(sum_insaldo_d) AS sum_insaldo_d, " +
                         " SUM(sum_insaldo) AS sum_insaldo, " +
                         " SUM(sum_real) AS sum_real, " +
                         " SUM(reval) AS reval, " +
                         " SUM(real_charge) AS real_charge, " +
                         " SUM(reval) + sum(real_charge) AS reval_charge, " +
                         " SUM(sum_money) AS sum_money, " +
                         " SUM(sum_outsaldo_k) AS sum_outsaldo_k, " +
                         " SUM(sum_outsaldo_d) AS sum_outsaldo_d, " +
                         " SUM(sum_outsaldo) AS sum_outsaldo " +
                  " FROM " + t_svod_supp + " a, " +
                             PrefKernel + "supplier s " +
                  " WHERE a.nzp_supp=s.nzp_supp " +
                  " GROUP BY name_supp " +
                  " ORDER BY name_supp ";
            DataTable dt5 = ExecSQLToTable(sql);
            dt5.TableName = "Q_master5";

            sql = " SELECT (CASE WHEN TRIM(town) = '-' OR town IS NULL THEN '' ELSE TRIM(town) END) || " +
                         " (CASE WHEN TRIM(rajon) = '-' OR rajon IS NULL THEN '' ELSE ', ' || TRIM(rajon) END) || " +
                         " (CASE WHEN TRIM(ulicareg) = '' OR TRIM(ulicareg) = '-' OR ulicareg IS NULL THEN '' ELSE ', ' ||TRIM(ulicareg) || ' ' END) || " +
                         " (CASE WHEN TRIM(ulica) = '-' OR ulica IS NULL THEN '' ELSE TRIM(ulica) END) || " +
                         " (CASE WHEN TRIM(ndom) = '-' OR ndom IS NULL THEN '' ELSE ', д. ' || TRIM(ndom) END) || " +
                         " (CASE WHEN TRIM(nkor) = '' OR TRIM(nkor) = '-' OR nkor IS NULL THEN '' ELSE ', корп. ' || TRIM(nkor) END) AS address, " + 
                         " TRIM(name_supp) AS supplier, " +
                         " TRIM(service) AS service, " +
                         " SUM(sum_insaldo_k) AS sum_insaldo_k," +
                         " SUM(sum_insaldo_d) AS sum_insaldo_d," +
                         " SUM(sum_insaldo) AS sum_insaldo," +
                         " SUM(sum_real) AS sum_real," +
                         " SUM(reval) + SUM(real_charge) AS reval_charge," +
                         " SUM(sum_money) AS sum_money," +
                         " SUM(sum_outsaldo_k) AS sum_outsaldo_k," +
                         " SUM(sum_outsaldo_d) AS sum_outsaldo_d," +
                         " SUM(sum_outsaldo) AS sum_outsaldo" +
                  " FROM " + t_svod_address + " a INNER JOIN " + PrefData + "dom d ON d.nzp_dom = a.nzp_dom " +
                                                " INNER JOIN " + PrefData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                                                " INNER JOIN " + PrefData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                                                " INNER JOIN " + PrefData + "s_town t ON t.nzp_town = r.nzp_town " +
                                                " INNER JOIN " + PrefKernel + "supplier s ON s.nzp_supp = a.nzp_supp " +
                                                " INNER JOIN " + PrefKernel + "services se ON se.nzp_serv = a.nzp_serv " +
                  " GROUP BY address, name_supp, service " +
                  " ORDER BY address, name_supp, service ";
            DataTable dt6 = ExecSQLToTable(sql);
            dt6.TableName = "Q_master6";

            var ds = new DataSet();
            ds.Tables.Add(dt1);
            ds.Tables.Add(dt2);
            ds.Tables.Add(dt3);
            ds.Tables.Add(dt4);
            ds.Tables.Add(dt5);
            ds.Tables.Add(dt6);

            return ds;
        }

    }
}
