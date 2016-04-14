using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Office.Interop.Excel;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Globalization;
using Constants = STCLINE.KP50.Global.Constants;

namespace STCLINE.KP50.REPORT
{
    public partial class ReportGen
    {

        public void SetReportSing(ExcelLoader excelL, int arow, bool dislokacii)
        {
            SetReportSing(excelL, arow, dislokacii, 0);
        }

        public void SetReportSing(ExcelLoader excelL, int arow, bool dislokacii, int nzp_user)
        {

            excelL.ExlWs.PageSetup.RightFooter = "&P";
            if (!Interfaces.Points.IsSmr) return;
            var excell = excelL.ExlWs.Range["A" + arow, "C" + arow];

            if (!dislokacii)
            {

                excell.Merge(Type.Missing);
                excell.Value2 = SignNames(1291);

                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                excell = excelL.ExlWs.Range["D" + arow, "E" + arow];
                excell.Merge(Type.Missing);
                excell.Value2 = "_________";

                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;


                excell = excelL.ExlWs.Range["F" + arow, "H" + arow];
                excell.Merge(Type.Missing);
                excell.Value2 = SignNames(1294);

                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;


                excell = excelL.ExlWs.Range["A" + (arow + 2), "C" + (arow + 2)];
                excell.Merge(Type.Missing);
                excell.Value2 = SignNames(1292);

                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                excell = excelL.ExlWs.Range["D" + (arow + 2), "E" + (arow + 2)];
                excell.Merge(Type.Missing);
                excell.Value2 = "_________";

                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;


                excell = excelL.ExlWs.Range["F" + (arow + 2), "H" + (arow + 2)];
                excell.Merge(Type.Missing);
                excell.Value2 = SignNames(1295);

                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                excell = excelL.ExlWs.Range["A" + (arow + 4), "C" + (arow + 4)];
                excell.Merge(Type.Missing);
                excell.Value2 = SignNames(1293);

                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                excell = excelL.ExlWs.Range["D" + (arow + 4), "E" + (arow + 4)];
                excell.Merge(Type.Missing);
                excell.Value2 = "_________";
                //excell.Font.Size = 10;
                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;


                excell = excelL.ExlWs.Range["F" + (arow + 4), "H" + (arow + 4)];
                excell.Merge(Type.Missing);
                excell.Value2 = SignNames(1296);
                excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
            }


            excell = excelL.ExlWs.Range["A" + (arow + 6), "C" + (arow + 6)];
            excell.Merge(Type.Missing);
            excell.Value2 = "Исполнитель";
            //excell.Font.Size = 10;
            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

            excell = excelL.ExlWs.Range["D" + (arow + 6), "E" + (arow + 6)];
            excell.Merge(Type.Missing);
            excell.Value2 = "_________";
            //excell.Font.Size = 10;
            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;


            excell = excelL.ExlWs.Range["F" + (arow + 6), "H" + (arow + 6)];
            excell.Merge(Type.Missing);
            if (nzp_user != 0)
            {
                IDbConnection conn_web = DBManager.GetConnection(Constants.cons_Webdata);
                conn_web.Open();
                DBManager.ExecSQL(conn_web, " set encryption password '" + DBManager.BasePwd + "'", false);
                var uname = DBManager.ExecSQLToTable(conn_web,
                    " select decrypt_char(uname) as uname from " + DBManager.GetFullBaseName(conn_web) + DBManager.tableDelimiter + "users" +
                    " where nzp_user = " + nzp_user);
                excell.Value2 = uname.Rows.Count == 1 ? Utils.GetCorrectFIO(uname.Rows[0]["uname"].ToString().Trim()) : "Стрельцова И.Д.";
                conn_web.Close();
            }
            else
            {
                excell.Value2 = "Стрельцова И.Д.";
            }

            //excell.Font.Size = 10;
            excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

        }

        private string SignNames(int nzp_prm)
        {
            IDbConnection conn_web = DBManager.GetConnection(Constants.cons_Webdata);
            try
            {
                conn_web.Open();
                var principal = DBManager.ExecSQLToTable(conn_web,
                    " SELECT val_prm " +
                    " FROM " + Interfaces.Points.Pref + DBManager.sDataAliasRest + "prm_10 " +
                    " WHERE is_actual = 1 " +
                    " AND dat_s <='" + DateTime.DaysInMonth(Interfaces.Points.CalcMonth.year_, Interfaces.Points.CalcMonth.month_) + "." +
                    Interfaces.Points.CalcMonth.month_ + "." + Interfaces.Points.CalcMonth.year_ + "' " +
                    " AND dat_po >='1." + Interfaces.Points.CalcMonth.month_ + "." + Interfaces.Points.CalcMonth.year_ + "' " +
                    " AND nzp_prm = " + nzp_prm);

                switch (nzp_prm)
                {
                    case 1291: return principal.Rows.Count == 1 ? principal.Rows[0]["val_prm"].ToString().Trim() : "Директор  ";
                    case 1292: return principal.Rows.Count == 1 ? principal.Rows[0]["val_prm"].ToString().Trim() : "Нач. отдела по расщеплению платежей";
                    case 1293: return principal.Rows.Count == 1 ? principal.Rows[0]["val_prm"].ToString().Trim() : "Нач. отдела бюджетирования и учета";
                    case 1294: return principal.Rows.Count == 1 ? principal.Rows[0]["val_prm"].ToString().Trim() : "Звягинцев А.В.";
                    case 1295: return principal.Rows.Count == 1 ? principal.Rows[0]["val_prm"].ToString().Trim() : "Миллер Ю.А.";
                    case 1296: return principal.Rows.Count == 1 ? principal.Rows[0]["val_prm"].ToString().Trim() : "Соковых И.А."; 
                }
            }
            catch(Exception ex)
            {
                MonitorLog.WriteException("Ошибка при заполнении подвала отчета " + ex.Message, ex);
                switch (nzp_prm)
                {
                    case 1291: return "Директор  ";
                    case 1292: return "Нач. отдела по расщеплению платежей";
                    case 1293: return "Нач. отдела бюджетирования и учета";
                    case 1294: return "Звягинцев А.В.";
                    case 1295: return "Миллер Ю.А."; 
                    case 1296: return "Соковых И.А.";
                }
            }
            finally
            {
                conn_web.Close();
            }
            return "";
        }


        //Отчет: Справка по поставщикам Форма 3 Самара
        public Returns GetSpravSuppSamara(Prm prm, int nzp_user, ref string fileName)
        {


            Returns ret = Utils.InitReturns();
            ExcelLoader ExcelL = null;



            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetSpravSuppSamara(prm);
            ExR.Close();


            if (DT == null)
            {
                MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                ret.result = false;
                ret.text = "Создание DataTable : ОШИБКА!";
                return ret;
            }

            #region Устанавливаем разделитель '.'
            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            #endregion



            Microsoft.Office.Interop.Excel.Range excells1;
            #region Заполнение шапки
            Dictionary<string, string> Dic = new Dictionary<string, string>();
            Dic.Add("ERCName", SetReportHeader());


            Dic.Add("month", Utils.GetMonthName(prm.month_));//месяц
            Dic.Add("year", prm.year_.ToString());//год
            Dic.Add("DATE", DateTime.Now.ToShortDateString());//год
            #endregion


            ExcelL = new ExcelLoader();
            ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("SuppSprav3.xls"), Dic);
            ExcelFormater exform = new ExcelFormater();
            excells1 = ExcelL.ExlWs.get_Range("D3", "L4");
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = SetReportConditional(nzp_user);
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
            excells1.Font.Size = 8;
            excells1.RowHeight = 23;


            //формирование Excel файла                 
            try
            {


                #region Пишем тело

                int ExcelRow = 8;
                int i = 0;
                int start_suppindex = ExcelRow;
                int end_suppindex = start_suppindex;
                //int count_no = 0;

                string old_supp = "";
                decimal rsum_tarif = 0;
                decimal sum_nedop = 0;
                decimal reval_k = 0;
                decimal reval_d = 0;
                decimal sum_charge = 0;
                decimal sum_money = 0;
                DataRow[] drSelect = DT.Select("ord = 1");

                i = 0;
                foreach (DataRow dr in drSelect)
                {
                    #region Итого по поставщику
                    if (dr["name_supp"].ToString().Trim() != old_supp)
                    {
                        if (i > 0)
                        {
                            ExcelL.ExlWs.Cells[ExcelRow, 2] = "Итого";



                            end_suppindex = ExcelRow;
                            excells1 = ExcelL.ExlWs.get_Range("A" + start_suppindex, "A" + end_suppindex);
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = old_supp;
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excells1.WrapText = true;
                            excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;


                            ExcelL.ExlWs.Cells[ExcelRow, 9] = rsum_tarif;
                            ExcelL.ExlWs.Cells[ExcelRow, 10] = sum_nedop;
                            ExcelL.ExlWs.Cells[ExcelRow, 11] = reval_k;
                            ExcelL.ExlWs.Cells[ExcelRow, 12] = reval_d;
                            ExcelL.ExlWs.Cells[ExcelRow, 13] = sum_charge;
                            ExcelL.ExlWs.Cells[ExcelRow, 14] = 0.00;
                            ExcelL.ExlWs.Cells[ExcelRow, 15] = sum_charge;
                            ExcelL.ExlWs.Cells[ExcelRow, 16] = sum_money;

                            excells1 = ExcelL.ExlWs.get_Range("I" + ExcelRow, "U" + ExcelRow);
                            excells1.Font.Bold = true;

                            old_supp = dr["name_supp"].ToString().Trim();
                            rsum_tarif = 0;
                            sum_nedop = 0;
                            reval_k = 0;
                            reval_d = 0;
                            sum_charge = 0;
                            sum_money = 0;
                            ExcelRow++;
                            start_suppindex = ExcelRow;

                        }
                        else
                        {
                            old_supp = dr["name_supp"].ToString().Trim();
                        }
                    }
                    #endregion

                    ExcelL.ExlWs.Cells[ExcelRow, 1] = dr["name_supp"].ToString().Trim();
                    ExcelL.ExlWs.Cells[ExcelRow, 2] = dr["service"].ToString().Trim();
                    ExcelL.ExlWs.Cells[ExcelRow, 3] = dr["pl_izol"];
                    ExcelL.ExlWs.Cells[ExcelRow, 4] = dr["pl_komm"];
                    ExcelL.ExlWs.Cells[ExcelRow, 5] = dr["count_gil"];
                    ExcelL.ExlWs.Cells[ExcelRow, 6] = dr["count_ls"];
                    ExcelL.ExlWs.Cells[ExcelRow, 7] = dr["measure"].ToString().Trim();
                    ExcelL.ExlWs.Cells[ExcelRow, 8] = dr["c_calc"];
                    ExcelL.ExlWs.Cells[ExcelRow, 9] = dr["rsum_tarif"];
                    ExcelL.ExlWs.Cells[ExcelRow, 10] = dr["sum_nedop"];
                    ExcelL.ExlWs.Cells[ExcelRow, 11] = dr["reval_k"];
                    ExcelL.ExlWs.Cells[ExcelRow, 12] = dr["reval_d"];
                    ExcelL.ExlWs.Cells[ExcelRow, 13] = dr["sum_charge"];
                    ExcelL.ExlWs.Cells[ExcelRow, 14] = 0.00;
                    ExcelL.ExlWs.Cells[ExcelRow, 15] = dr["sum_charge"];
                    ExcelL.ExlWs.Cells[ExcelRow, 16] = dr["sum_money"];

                    if (System.Convert.ToDecimal(dr["sum_charge"]) > 0.001m)
                        ExcelL.ExlWs.Cells[ExcelRow, 17] =
                            System.Convert.ToDecimal(dr["sum_money"]) /
                            System.Convert.ToDecimal(dr["sum_charge"]);
                    else
                        ExcelL.ExlWs.Cells[ExcelRow, 17] = 0.00;
                    ExcelL.ExlWs.Cells[ExcelRow, 18] = 0.00;
                    ExcelL.ExlWs.Cells[ExcelRow, 19] = 0.00;
                    ExcelL.ExlWs.Cells[ExcelRow, 20] = 0.00;
                    ExcelL.ExlWs.Cells[ExcelRow, 21] = 0.00;

                    rsum_tarif += dr["rsum_tarif"] != DBNull.Value ? System.Convert.ToDecimal(dr["rsum_tarif"]) : 0;
                    sum_nedop += dr["sum_nedop"] != DBNull.Value ? System.Convert.ToDecimal(dr["sum_nedop"]) : 0;
                    reval_k += dr["reval_k"] != DBNull.Value ? System.Convert.ToDecimal(dr["reval_k"]) : 0;
                    reval_d += dr["reval_d"] != DBNull.Value ? System.Convert.ToDecimal(dr["reval_d"]) : 0;
                    sum_charge += dr["sum_charge"] != DBNull.Value ? System.Convert.ToDecimal(dr["sum_charge"]) : 0;
                    sum_money += dr["sum_money"] != DBNull.Value ? System.Convert.ToDecimal(dr["sum_money"]) : 0;

                    #region Электроснабжение
                    if ((System.Convert.ToInt32(dr["nzp_serv"]) == 25) & (
                        (System.Convert.ToDecimal(dr["rsum_tarif_odn"]) +
                         System.Convert.ToDecimal(dr["sum_charge_odn"]) +
                         System.Math.Abs(System.Convert.ToDecimal(dr["sum_money_odn"]))) > 0.001m
                        ))
                    {
                        ExcelL.ExlWs.Cells[ExcelRow + 1, 2] = "ОДН-Электроснабжение";
                        ExcelL.ExlWs.Cells[ExcelRow + 1, 9] = dr["rsum_tarif_odn"];
                        ExcelL.ExlWs.Cells[ExcelRow + 1, 10] = dr["sum_nedop_odn"];
                        ExcelL.ExlWs.Cells[ExcelRow + 1, 11] = dr["reval_k_odn"];
                        ExcelL.ExlWs.Cells[ExcelRow + 1, 12] = dr["reval_d_odn"];
                        ExcelL.ExlWs.Cells[ExcelRow + 1, 13] = dr["sum_charge_odn"];
                        ExcelL.ExlWs.Cells[ExcelRow + 1, 14] = 0.00;
                        ExcelL.ExlWs.Cells[ExcelRow + 1, 15] = dr["sum_charge_odn"];
                        ExcelL.ExlWs.Cells[ExcelRow + 1, 16] = dr["sum_money_odn"];

                        rsum_tarif += dr["rsum_tarif_odn"] != DBNull.Value ? System.Convert.ToDecimal(dr["rsum_tarif_odn"]) : 0;
                        sum_nedop += dr["sum_nedop_odn"] != DBNull.Value ? System.Convert.ToDecimal(dr["sum_nedop_odn"]) : 0;
                        reval_k += dr["reval_k_odn"] != DBNull.Value ? System.Convert.ToDecimal(dr["reval_k_odn"]) : 0;
                        reval_d += dr["reval_d_odn"] != DBNull.Value ? System.Convert.ToDecimal(dr["reval_d_odn"]) : 0;
                        sum_charge += dr["sum_charge_odn"] != DBNull.Value ? System.Convert.ToDecimal(dr["sum_charge_odn"]) : 0;
                        sum_money += dr["sum_money_odn"] != DBNull.Value ? System.Convert.ToDecimal(dr["sum_money_odn"]) : 0;

                        excells1 = ExcelL.ExlWs.get_Range("C" + ExcelRow, "C" + (ExcelRow + 1).ToString());
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = dr["pl_izol"];
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;

                        excells1 = ExcelL.ExlWs.get_Range("D" + ExcelRow, "D" + (ExcelRow + 1).ToString());
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = dr["pl_komm"];
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;


                        excells1 = ExcelL.ExlWs.get_Range("E" + ExcelRow, "E" + (ExcelRow + 1).ToString());
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = dr["count_gil"];
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;


                        excells1 = ExcelL.ExlWs.get_Range("F" + ExcelRow, "F" + (ExcelRow + 1).ToString());
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = dr["count_ls"];
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;

                        excells1 = ExcelL.ExlWs.get_Range("G" + ExcelRow, "G" + (ExcelRow + 1).ToString());
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = "кВт";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        ExcelRow++;

                    }
                    #endregion

                    ExcelRow++;
                    i++;
                }

                #region Итого по поставщику
                if (i > 0)
                {

                    ExcelL.ExlWs.Cells[ExcelRow, 2] = "Итого ";

                    excells1 = ExcelL.ExlWs.get_Range("A" + start_suppindex, "A" + ExcelRow);
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = old_supp;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                    excells1 = ExcelL.ExlWs.get_Range("I" + ExcelRow, "U" + ExcelRow);
                    excells1.Font.Bold = true;

                    ExcelL.ExlWs.Cells[ExcelRow, 9] = rsum_tarif;
                    ExcelL.ExlWs.Cells[ExcelRow, 10] = sum_nedop;
                    ExcelL.ExlWs.Cells[ExcelRow, 11] = reval_k;
                    ExcelL.ExlWs.Cells[ExcelRow, 12] = reval_d;
                    ExcelL.ExlWs.Cells[ExcelRow, 13] = sum_charge;
                    ExcelL.ExlWs.Cells[ExcelRow, 14] = 0.00;
                    ExcelL.ExlWs.Cells[ExcelRow, 15] = sum_charge;
                    ExcelL.ExlWs.Cells[ExcelRow, 16] = sum_money;
                    ExcelRow++;
                    start_suppindex = ExcelRow;
                }

                #endregion

                #endregion


                #region Пишем тело2


                drSelect = DT.Select("ord = 2");
                rsum_tarif = 0;
                sum_nedop = 0;
                reval_k = 0;
                reval_d = 0;
                sum_charge = 0;
                sum_money = 0;

                i = 0;
                foreach (DataRow dr in drSelect)
                {


                    ExcelL.ExlWs.Cells[ExcelRow, 1] = dr["name_supp"].ToString().Trim(); ;
                    ExcelL.ExlWs.Cells[ExcelRow, 2] = dr["service"].ToString().Trim(); ;
                    ExcelL.ExlWs.Cells[ExcelRow, 3] = dr["pl_izol"];
                    ExcelL.ExlWs.Cells[ExcelRow, 4] = dr["pl_komm"];
                    ExcelL.ExlWs.Cells[ExcelRow, 5] = dr["count_gil"];
                    ExcelL.ExlWs.Cells[ExcelRow, 6] = dr["count_ls"];
                    ExcelL.ExlWs.Cells[ExcelRow, 7] = dr["measure"].ToString().Trim(); ;
                    ExcelL.ExlWs.Cells[ExcelRow, 8] = dr["c_calc"];
                    ExcelL.ExlWs.Cells[ExcelRow, 9] = dr["rsum_tarif"];
                    ExcelL.ExlWs.Cells[ExcelRow, 10] = dr["sum_nedop"];
                    ExcelL.ExlWs.Cells[ExcelRow, 11] = dr["reval_k"];
                    ExcelL.ExlWs.Cells[ExcelRow, 12] = dr["reval_d"];
                    ExcelL.ExlWs.Cells[ExcelRow, 13] = dr["sum_charge"];
                    ExcelL.ExlWs.Cells[ExcelRow, 14] = 0.00;
                    ExcelL.ExlWs.Cells[ExcelRow, 15] = dr["sum_charge"];
                    ExcelL.ExlWs.Cells[ExcelRow, 16] = dr["sum_money"];

                    if (System.Convert.ToDecimal(dr["sum_charge"]) > 0.001m)
                        ExcelL.ExlWs.Cells[ExcelRow, 17] =
                            System.Convert.ToDecimal(dr["sum_money"]) /
                            System.Convert.ToDecimal(dr["sum_charge"]);
                    else
                        ExcelL.ExlWs.Cells[ExcelRow, 17] = 0.00;
                    ExcelL.ExlWs.Cells[ExcelRow, 18] = 0.00;
                    ExcelL.ExlWs.Cells[ExcelRow, 19] = 0.00;
                    ExcelL.ExlWs.Cells[ExcelRow, 20] = 0.00;
                    ExcelL.ExlWs.Cells[ExcelRow, 21] = 0.00;

                    rsum_tarif += dr["rsum_tarif"] != DBNull.Value ? System.Convert.ToDecimal(dr["rsum_tarif"]) : 0;
                    sum_nedop += dr["sum_nedop"] != DBNull.Value ? System.Convert.ToDecimal(dr["sum_nedop"]) : 0;
                    reval_k += dr["reval_k"] != DBNull.Value ? System.Convert.ToDecimal(dr["reval_k"]) : 0;
                    reval_d += dr["reval_d"] != DBNull.Value ? System.Convert.ToDecimal(dr["reval_d"]) : 0;
                    sum_charge += dr["sum_charge"] != DBNull.Value ? System.Convert.ToDecimal(dr["sum_charge"]) : 0;
                    sum_money += dr["sum_money"] != DBNull.Value ? System.Convert.ToDecimal(dr["sum_money"]) : 0;

                    #region Электроснабжение
                    if ((System.Convert.ToInt32(dr["nzp_serv"]) == 25) & (
                           (System.Convert.ToDecimal(dr["rsum_tarif_odn"]) +
                            System.Convert.ToDecimal(dr["sum_charge_odn"]) +
                            System.Math.Abs(System.Convert.ToDecimal(dr["sum_money_odn"]))) > 0.001m
                           ))
                    {
                        ExcelL.ExlWs.Cells[ExcelRow + 1, 2] = "ОДН-Электроснабжение";
                        ExcelL.ExlWs.Cells[ExcelRow + 1, 9] = dr["rsum_tarif_odn"];
                        ExcelL.ExlWs.Cells[ExcelRow + 1, 10] = dr["sum_nedop_odn"];
                        ExcelL.ExlWs.Cells[ExcelRow + 1, 11] = dr["reval_k_odn"];
                        ExcelL.ExlWs.Cells[ExcelRow + 1, 12] = dr["reval_d_odn"];
                        ExcelL.ExlWs.Cells[ExcelRow + 1, 13] = dr["sum_charge_odn"];
                        ExcelL.ExlWs.Cells[ExcelRow + 1, 14] = 0.00;
                        ExcelL.ExlWs.Cells[ExcelRow + 1, 15] = dr["sum_charge_odn"];
                        ExcelL.ExlWs.Cells[ExcelRow + 1, 16] = dr["sum_money_odn"];

                        rsum_tarif += dr["rsum_tarif_odn"] != DBNull.Value ? System.Convert.ToDecimal(dr["rsum_tarif_odn"]) : 0;
                        sum_nedop += dr["sum_nedop_odn"] != DBNull.Value ? System.Convert.ToDecimal(dr["sum_nedop_odn"]) : 0;
                        reval_k += dr["reval_k_odn"] != DBNull.Value ? System.Convert.ToDecimal(dr["reval_k_odn"]) : 0;
                        reval_d += dr["reval_d_odn"] != DBNull.Value ? System.Convert.ToDecimal(dr["reval_d_odn"]) : 0;
                        sum_charge += dr["sum_charge_odn"] != DBNull.Value ? System.Convert.ToDecimal(dr["sum_charge_odn"]) : 0;
                        sum_money += dr["sum_money_odn"] != DBNull.Value ? System.Convert.ToDecimal(dr["sum_money_odn"]) : 0;

                        excells1 = ExcelL.ExlWs.get_Range("C" + ExcelRow, "C" + (ExcelRow + 1).ToString());
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = dr["pl_izol"];
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;

                        excells1 = ExcelL.ExlWs.get_Range("D" + ExcelRow, "D" + (ExcelRow + 1).ToString());
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = dr["pl_komm"];
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;


                        excells1 = ExcelL.ExlWs.get_Range("E" + ExcelRow, "E" + (ExcelRow + 1).ToString());
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = dr["count_gil"];
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;


                        excells1 = ExcelL.ExlWs.get_Range("F" + ExcelRow, "F" + (ExcelRow + 1).ToString());
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = dr["count_ls"];
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;

                        excells1 = ExcelL.ExlWs.get_Range("G" + ExcelRow, "G" + (ExcelRow + 1).ToString());
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = "кВт";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        ExcelRow++;
                    }
                    #endregion

                    ExcelRow++;
                    i++;
                }

                #region Всего
                if (i > 0)
                {

                    ExcelL.ExlWs.Cells[ExcelRow, 2] = "Итого ";

                    excells1 = ExcelL.ExlWs.get_Range("A" + start_suppindex, "A" + ExcelRow);
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Всего";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                    excells1 = ExcelL.ExlWs.get_Range("I" + ExcelRow, "U" + ExcelRow);
                    excells1.Font.Bold = true;


                    ExcelL.ExlWs.Cells[ExcelRow, 9] = rsum_tarif;
                    ExcelL.ExlWs.Cells[ExcelRow, 10] = sum_nedop;
                    ExcelL.ExlWs.Cells[ExcelRow, 11] = reval_k;
                    ExcelL.ExlWs.Cells[ExcelRow, 12] = reval_d;
                    ExcelL.ExlWs.Cells[ExcelRow, 13] = sum_charge;
                    ExcelL.ExlWs.Cells[ExcelRow, 14] = 0.00;
                    ExcelL.ExlWs.Cells[ExcelRow, 15] = sum_charge;
                    ExcelL.ExlWs.Cells[ExcelRow, 16] = sum_money;
                }

                #endregion

                #endregion

                excells1 = ExcelL.ExlWs.get_Range("I" + ExcelRow.ToString(), "U" + ExcelRow.ToString());
                excells1.Font.Bold = true;

                excells1 = ExcelL.ExlWs.get_Range("I8", "U" + ExcelRow.ToString());
                excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                excells1 = ExcelL.ExlWs.get_Range("H8", "U" + ExcelRow.ToString());
                excells1.NumberFormat = "# ##0,00";

                excells1 = ExcelL.ExlWs.get_Range("C8", "D" + ExcelRow.ToString());
                excells1.NumberFormat = "# ##0,00";

                excells1 = ExcelL.ExlWs.get_Range("Q8", "Q" + ExcelRow.ToString());
                excells1.NumberFormat = "0,00%";

                SetReportSing(ExcelL, ExcelRow + 3, false);

                #region Сохраняем файл
                try
                {

                    fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SpravPoOtklUslDomVinovnik" +
                        prm.year_.ToString() + "_" +
                        prm.month_.ToString() + "_" + nzp_user) + ".xls";
                    ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                    if (InputOutput.useFtp)
                    {
                        fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                }

                #endregion

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Формирование Excel ошибка :" + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;
            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }



            return ret;

        }

        //Отчет: Справка по поставщикам коммунальных услуг ф.3 самара
        public void GetSpravSuppSamara(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Справка по поставщикам коммунальных услуг форма 3\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";


            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Справка по поставщикам коммунальных услуг форма 3\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetSpravSuppSamara(cont.listprm[0], cont.nzp_user, ref fileName);

            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }

            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        //Отчет: Лицевые счета + домовые или квартирные параметры(Самара)
        public Returns LsKvarDomParam(List<Prm> listprm, int nzp_user, ref string fileName)
        {

            Returns ret = Utils.InitReturns();
            ExcelLoader ExcelL = null;

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetDataReportGenertor(out ret, nzp_user.ToString());
            ExR.Close();

            if (DT == null)
            {
                MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                ret.result = false;
                ret.text = "Создание DataTable : ОШИБКА!";
                return ret;
            }

            #region добавление пустых параметров к DataTable(что бы показать что их действительно нет)
            try
            {
                var listCol = DT.Columns.OfType<DataColumn>();
                var columns_names = (from c in listCol where c.ColumnName.Contains("val") select c.ColumnName.Substring(c.ColumnName.IndexOf('_') + 1)).ToList<string>();
                var par_names = (from pn in listprm where !columns_names.Contains(pn.nzp_prm.ToString()) select pn.name_prm).ToList<string>();

                foreach (string new_name in par_names)
                {
                    DT.Columns.Add(new_name);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка добавления пустых параметров : " + ex.Message, MonitorLog.typelog.Error, true);
                //дальше продолжаем выполнение, т.к. пустые параметры не так важны как сам отчет
            }
            #endregion


            ExcelFormater Eformat = new ExcelFormater();

            //создаем массив заголовков
            string[] HeaderData = new string[DT.Columns.Count];
            //создаем массив форматов
            string[] TypeHeader = new string[HeaderData.Length];
            //футер
            string[] footer_data = null;

            //заполняем массивы
            HeaderData[0] = "УК";
            HeaderData[1] = "ЖЭУ";
            HeaderData[2] = "Лицевой счет";
            HeaderData[3] = "Улица";
            HeaderData[4] = "Дом";
            HeaderData[5] = "Квартира";
            int index = 6;
            int indextype = 6;

            for (int i = 6; i < DT.Columns.Count; i++)
            {
                DataColumn dc = DT.Columns[i];
                Prm p = Eformat.GetPrmByColName(dc.ColumnName, listprm);
                if (p != null)
                {
                    HeaderData[index] = p.name_prm;
                    TypeHeader[indextype] = p.type_prm;
                }
                else
                {
                    HeaderData[index] = dc.ColumnName;
                }
                index++;
                indextype++;
            }


            //формирование Excel файла                 
            try
            {
                #region Создание Excel документа

                ExcelL = new ExcelLoader();
                ExcelL.ExlWs.Rows.Font.Name = "Arial";
                ExcelCreater ExcelCr = new ExcelCreater();

                //создаем название отчета
                ret = ExcelCr.MakeName("Значения характеристик лицевых счетов " + DateTime.Now.ToShortDateString(), "A1", DT.Columns.Count, 2, ref ExcelL.ExlWs);

                //Создаем шапку
                ret = ExcelCr.MakeHeader(HeaderData, 1, 0, ref ExcelL.ExlWs);


                //Создаем тело               
                if (ret.result && DT.Rows.Count < 65001)
                {
                    ret = ExcelCr.MakeBody(DT, 2, 0, ref ExcelL.ExlWs);
                }


                //Создаем подвал                
                if (footer_data != null && ret.result)
                {
                    ret = ExcelCr.MakeFooter(footer_data, DT.Rows.Count + 1, 0, ref ExcelL.ExlWs);
                }
                //стандартный формат
                if (ret.result)
                {
                    ret = ExcelL.ApplyStandartFormat(DT.Rows.Count, 3, HeaderData.Length, TypeHeader);
                }
                #endregion

                #region Надпись "Нет данных" если данных нет)
                if (DT.Rows.Count == 0)
                {
                    ExcelCr.MakeName("Нет данных", "A4", DT.Columns.Count, 6, ref ExcelL.ExlWs);
                }

                if (DT.Rows.Count > 65000)
                {
                    ExcelCr.MakeName(" Не все версии Excel поддерживают " + Environment.NewLine +
                                     " количество строк более 65000, " + Environment.NewLine +
                                     " ограничьте выборку, " + Environment.NewLine +
                                     " количество записей " + DT.Rows.Count, "A4", DT.Columns.Count, 6, ref ExcelL.ExlWs);
                }
                #endregion

                if (ret.result)
                {
                    //имя файла                                                         
                    fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SpLs_" + DateTime.Now.GetHashCode() + nzp_user) + ".xls";

                    //Сохранение
                    ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                    ExcelL.ExlWb.Close();
                    ExcelL.ExlWb = null;
                    if (InputOutput.useFtp)
                    {
                        fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                    }


                }
                else
                {
                    MonitorLog.WriteLog("Ошибка создания Excel файла : " + ret.text, MonitorLog.typelog.Error, true);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Формирование Excel ошибка :" + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;
            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }
            //процедура формирования файла Excel
            //ret = ExcelL.CreateExcelReport(1, DT, HeaderData, null, Constants.ExcelDir, nzp_user.ToString(), TypeHeader);               

            return ret;

        }

        //Отчет: Лицевые счета + домовые или квартирные параметры(Самара)
        public void LsKvarDomParam(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();
            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Лицевые счета\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";



            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Генератор по параметрам\"", ref time, cont.comment);

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 1, "", time);

            //Старт формирования
            ret = this.LsKvarDomParam(cont.listprm, cont.nzp_user, ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании(статус 2)
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 1, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 1, "", time);
            }

            excelRepDb.Close();
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);

        }

        public Returns GetVedOplLs(Prm prm_, string Nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetVedOplLs(prm_, out ret, Nzp_user);

            ExcelLoader ExcelL = null;
            try
            {
                ExcelL = new ExcelLoader();

                if (DT != null)
                {

                    ExcelCreater ExcelCr = new ExcelCreater();


                    #region Создаем шапку
                    Microsoft.Office.Interop.Excel.Range excells1;

                    Dictionary<string, string> Dic = new Dictionary<string, string>();
                    Dic.Add("ERCName", SetReportHeader());




                    Dic.Add("month", Utils.GetMonthName(prm_.month_));//месяц
                    Dic.Add("year", prm_.year_.ToString());//год
                    Dic.Add("DATE", DateTime.Now.ToShortDateString());//год


                    #endregion


                    ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("ved_opl.xls"), Dic);


                    excells1 = ExcelL.ExlWs.get_Range("D3", "O4");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = SetReportConditional(System.Convert.ToInt32(Nzp_user));
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.Font.Size = 8;
                    excells1.RowHeight = 23;

                    #region Пишем тело

                    int ExcelRow = 7;
                    int i = 0;

                    while (i < DT.Rows.Count)
                    {


                        ExcelL.ExlWs.Cells[ExcelRow, 1] =
                            DateTime.Parse(DT.Rows[i]["dat_pack"].ToString()).ToShortDateString();
                        ExcelL.ExlWs.Cells[ExcelRow, 2] = DT.Rows[i]["num_pack"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 3] = DT.Rows[i]["geu"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 4] = DT.Rows[i]["pkod10"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 5] = DT.Rows[i]["ulica"].ToString().Trim() +
                            ", д." + DT.Rows[i]["ndom"].ToString().Trim() +
                            DT.Rows[i]["nkor"].ToString().Trim() + "-" +
                            DT.Rows[i]["nkvar"].ToString().Trim() +
                            DT.Rows[i]["nkvar_n"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 6] = DT.Rows[i]["fio"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 7] = DateTime.Parse(DT.Rows[i]["dat_vvod"].ToString()).ToShortDateString();
                        ExcelL.ExlWs.Cells[ExcelRow, 8] = DateTime.Parse(DT.Rows[i]["dat_month"].ToString()).ToShortDateString();
                        ExcelL.ExlWs.Cells[ExcelRow, 9] = Decimal.Parse(DT.Rows[i]["sum_ls"].ToString().Trim());
                        ExcelL.ExlWs.Cells[ExcelRow, 10] = Decimal.Parse(DT.Rows[i]["g_sum_ls"].ToString().Trim());
                        ExcelL.ExlWs.Cells[ExcelRow, 11] = Decimal.Parse(DT.Rows[i]["peni"].ToString().Trim());
                        ExcelL.ExlWs.Cells[ExcelRow, 12] = DT.Rows[i]["anketa"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 13] = DT.Rows[i]["num_pack"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 14] = DT.Rows[i]["prefix_ls"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 15] = DT.Rows[i]["payer"].ToString().Trim();

                        ExcelRow++;
                        i++;
                    }
                    #endregion

                    #region форматируем
                    if (ExcelRow > 7) ExcelRow--;



                    excells1 = ExcelL.ExlWs.get_Range("I7", "K" + ExcelRow.ToString());
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.EntireColumn.NumberFormat = "# ##0,00";

                    excells1 = ExcelL.ExlWs.get_Range("A7", "O" + ExcelRow.ToString());
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Font.Size = 6;

                    SetReportSing(ExcelL, ExcelRow + 3, false);

                    #endregion

                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "VedOPL" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString() + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }

                    #endregion

                }

                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetVedOplLs DataTable : ОШИБКА!";

                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "VedOPL" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }

                    //удаление объекта
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                try
                {
                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "VedOPL" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет GetVedOplLs", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetVedOplLs Excel File : ОШИБКА!";
                }

            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }

            return ret;

        }

        public Returns GetVedPere(Prm prm_, string Nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetVedPere(prm_, out ret, Nzp_user);

            ExcelLoader ExcelL = null;
            try
            {
                ExcelL = new ExcelLoader();

                if (DT != null)
                {

                    ExcelCreater ExcelCr = new ExcelCreater();


                    #region Создаем шапку
                    Microsoft.Office.Interop.Excel.Range excells1;

                    Dictionary<string, string> Dic = new Dictionary<string, string>();
                    Dic.Add("ERCName", SetReportHeader());


                    Dic.Add("month", Utils.GetMonthName(prm_.month_));//месяц
                    Dic.Add("year", prm_.year_.ToString());//год
                    Dic.Add("DATE", DateTime.Now.ToShortDateString());//год
                    #endregion


                    ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("ved_pere.xls"), Dic);


                    #region Пишем тело

                    int ExcelRow = 7;
                    int i = 0;
                    decimal reval_k = 0;
                    decimal reval_d = 0;
                    while (i < DT.Rows.Count)
                    {

                        ExcelL.ExlWs.Cells[ExcelRow, 1] = (i + 1).ToString();
                        ExcelL.ExlWs.Cells[ExcelRow, 2] = DT.Rows[i]["pkod10"].ToString();
                        ExcelL.ExlWs.Cells[ExcelRow, 3] = DT.Rows[i]["fio"].ToString();
                        ExcelL.ExlWs.Cells[ExcelRow, 4] = DT.Rows[i]["service"].ToString();
                        ExcelL.ExlWs.Cells[ExcelRow, 5] = Decimal.Parse(DT.Rows[i]["reval_k"].ToString());
                        ExcelL.ExlWs.Cells[ExcelRow, 6] = Decimal.Parse(DT.Rows[i]["reval_d"].ToString());
                        ExcelL.ExlWs.Cells[ExcelRow, 7] = "";
                        ExcelL.ExlWs.Cells[ExcelRow, 8] = "Перер. с " + DT.Rows[i]["dat_s"].ToString() + "г. по " +
                            DT.Rows[i]["dat_po"].ToString() + "г. " + DT.Rows[i]["reason"].ToString();
                        reval_k = reval_k + Decimal.Parse(DT.Rows[i]["reval_k"].ToString());
                        reval_d = reval_d + Decimal.Parse(DT.Rows[i]["reval_d"].ToString());
                        ExcelRow++;
                        i++;
                    }
                    #endregion

                    #region форматируем




                    excells1 = ExcelL.ExlWs.get_Range("C" + ExcelRow.ToString(), "D" + ExcelRow.ToString());
                    excells1.Merge(Type.Missing);
                    excells1.Value2 = "Итого по ведомости: ";
                    ExcelL.ExlWs.Cells[ExcelRow, 5] = reval_k;
                    ExcelL.ExlWs.Cells[ExcelRow, 6] = reval_d;

                    excells1 = ExcelL.ExlWs.get_Range("C7", "D" + ExcelRow.ToString());
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.EntireColumn.NumberFormat = "# ##0,00";

                    excells1 = ExcelL.ExlWs.get_Range("A7", "G" + ExcelRow.ToString());
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Font.Size = 8;

                    SetReportSing(ExcelL, ExcelRow + 3, false);
                    #endregion

                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "VedPere" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString() + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }

                    #endregion

                }

                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetVedPere DataTable : ОШИБКА!";

                    //удаление объекта
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                try
                {
                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "VedPere" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет GetVedPere", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetVedPere Excel File : ОШИБКА!";
                }

            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }

            return ret;

        }

        public Returns GetDolgSved(Prm prm_, string Nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetDolgSved(prm_, out ret, Nzp_user);

            ExcelLoader ExcelL = null;
            try
            {
                ExcelL = new ExcelLoader();
                ExcelL.ExlWs.Rows.Font.Name = "Arial";
                if (DT != null)
                {

                    ExcelCreater ExcelCr = new ExcelCreater();


                    #region Создаем шапку
                    Microsoft.Office.Interop.Excel.Range excells1;

                    Dictionary<string, string> Dic = new Dictionary<string, string>();
                    Dic.Add("ERCName", SetReportHeader());

                    Dic.Add("month", Utils.GetMonthName(prm_.month_));//месяц
                    Dic.Add("year", prm_.year_.ToString());//год
                    Dic.Add("DATE", DateTime.Now.ToShortDateString());//год
                    #endregion


                    ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("dolg_sved.xls"), Dic);

                    excells1 = ExcelL.ExlWs.get_Range("C3", "E4");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = SetReportConditional(System.Convert.ToInt32(Nzp_user));
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.Font.Size = 8;
                    excells1.RowHeight = 23;


                    #region Пишем тело

                    int ExcelRow = 7;
                    int i = 0;
                    decimal count_ls = 0;
                    decimal sum_outsaldo = 0;
                    decimal allcount_ls = 0;
                    decimal allsum_outsaldo = 0;

                    string mes_dolg = "";
                    while (i < DT.Rows.Count)
                    {
                        if (i == 0) mes_dolg = DT.Rows[i]["mes_dolg"].ToString();

                        if (mes_dolg != DT.Rows[i]["mes_dolg"].ToString())
                        {
                            ExcelL.ExlWs.Cells[ExcelRow, 1] = "итого";
                            ExcelL.ExlWs.Cells[ExcelRow, 3] = count_ls;
                            ExcelL.ExlWs.Cells[ExcelRow, 5] = sum_outsaldo;
                            sum_outsaldo = 0;
                            count_ls = 0;
                            mes_dolg = DT.Rows[i]["mes_dolg"].ToString();
                            ExcelRow++;

                        }

                        switch (Convert.ToInt32(DT.Rows[i]["mes_dolg"].ToString()))
                        {
                            case -1:
                                {
                                    ExcelL.ExlWs.Cells[ExcelRow, 1] = "без начислений";
                                }
                                break;
                            case 0:
                                {
                                    ExcelL.ExlWs.Cells[ExcelRow, 1] = "менее 1 месяца";
                                }
                                break;
                            case 1:
                                {
                                    ExcelL.ExlWs.Cells[ExcelRow, 1] = "1 месяц";
                                }
                                break;
                            case 2:
                                {
                                    ExcelL.ExlWs.Cells[ExcelRow, 1] = DT.Rows[i]["mes_dolg"].ToString() + " месяца";
                                }
                                break;
                            case 3:
                                {
                                    ExcelL.ExlWs.Cells[ExcelRow, 1] = DT.Rows[i]["mes_dolg"].ToString() + " месяца";
                                }
                                break;
                            case 4:
                                {
                                    ExcelL.ExlWs.Cells[ExcelRow, 1] = DT.Rows[i]["mes_dolg"].ToString() + " месяца";
                                }
                                break;

                            case 5:
                            case 6:
                            case 7:
                            case 8:
                            case 9:
                            case 10:
                            case 11:
                                {
                                    ExcelL.ExlWs.Cells[ExcelRow, 1] = DT.Rows[i]["mes_dolg"].ToString() + " месяцев";
                                }
                                break;
                            case 12:
                                {
                                    ExcelL.ExlWs.Cells[ExcelRow, 1] = "более года";
                                }
                                break;

                        }

                        sum_outsaldo = sum_outsaldo + Decimal.Parse(DT.Rows[i]["sum_outsaldo"].ToString());
                        count_ls = count_ls + System.Convert.ToInt32(DT.Rows[i]["count_ls"].ToString());

                        allsum_outsaldo = allsum_outsaldo + Decimal.Parse(DT.Rows[i]["sum_outsaldo"].ToString());
                        allcount_ls = allcount_ls + System.Convert.ToInt32(DT.Rows[i]["count_ls"].ToString());


                        ExcelL.ExlWs.Cells[ExcelRow, 2] = DT.Rows[i]["geu"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 3] = DT.Rows[i]["count_ls"].ToString();
                        ExcelL.ExlWs.Cells[ExcelRow, 4] = Decimal.Parse(DT.Rows[i]["proc_dolg"].ToString());
                        ExcelL.ExlWs.Cells[ExcelRow, 5] = Decimal.Parse(DT.Rows[i]["sum_outsaldo"].ToString());
                        ExcelRow++;
                        i++;
                    }
                    #endregion

                    ExcelL.ExlWs.Cells[ExcelRow, 1] = "ИТОГО";
                    ExcelL.ExlWs.Cells[ExcelRow, 3] = allcount_ls;
                    ExcelL.ExlWs.Cells[ExcelRow, 5] = allsum_outsaldo;

                    #region форматируем





                    excells1 = ExcelL.ExlWs.get_Range("E7", "E" + ExcelRow.ToString());
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.EntireColumn.NumberFormat = "# ##0,00";

                    excells1 = ExcelL.ExlWs.get_Range("A7", "E" + ExcelRow.ToString());
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Font.Size = 8;

                    SetReportSing(ExcelL, ExcelRow + 3, false);
                    #endregion

                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SvedDolg" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString() + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }

                    #endregion

                }

                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetDolgSved DataTable : ОШИБКА!";

                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "DolgSved" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }

                    //удаление объекта
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                try
                {
                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "DolgSved" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет GetDolgSved", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetDolgSved Excel File : ОШИБКА!";
                }

            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }

            return ret;

        }

        public Returns GetVedNormPotr(Prm prm_, string Nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetVedNormPotr(prm_, out ret, Nzp_user);

            ExcelLoader ExcelL = null;
            try
            {
                ExcelL = new ExcelLoader();

                if (DT != null)
                {

                    ExcelCreater ExcelCr = new ExcelCreater();


                    #region Создаем шапку
                    Microsoft.Office.Interop.Excel.Range excells1;

                    Dictionary<string, string> Dic = new Dictionary<string, string>();
                    Dic.Add("ERCName", SetReportHeader());


                    Dic.Add("month", Utils.GetMonthName(prm_.month_));//месяц
                    Dic.Add("year", prm_.year_.ToString());//год
                    Dic.Add("DATE", DateTime.Now.ToShortDateString());//год
                    #endregion



                    ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("ved_norm_potr.xls"), Dic);
                    ExcelFormater exform = new ExcelFormater();
                    excells1 = ExcelL.ExlWs.get_Range("D3", "Q4");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = SetReportConditional(System.Convert.ToInt32(Nzp_user));
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.Font.Size = 6;
                    excells1.RowHeight = 23;
                    decimal irsum_tarif = 0;
                    decimal ireval_k = 0;
                    decimal ireval_d = 0;
                    decimal isum_nedop = 0;
                    decimal isum_charge = 0;


                    #region Пишем тело

                    int ExcelRow = 7;
                    int i = 0;
                    int count_serv = 1;
                    string oldServ = "";
                    decimal[] servMas = new decimal[9];
                    for (int j = 0; j < 9; j++) servMas[j] = 0;


                    while (i < DT.Rows.Count)
                    {


                        if (i == 0)
                        {
                            oldServ = DT.Rows[i]["service"].ToString().Trim();
                            ExcelL.ExlWs.Cells[ExcelRow, 2] = oldServ;
                            ExcelL.ExlWs.Cells[ExcelRow, 1] = "1";
                        }


                        if (oldServ != DT.Rows[i]["service"].ToString().Trim())
                        {
                            count_serv++;

                            ExcelL.ExlWs.Cells[ExcelRow, 2] = "Итого";

                            ExcelL.ExlWs.Cells[ExcelRow, 5] = servMas[0];
                            ExcelL.ExlWs.Cells[ExcelRow, 6] = servMas[1];
                            ExcelL.ExlWs.Cells[ExcelRow, 7] = servMas[2];
                            ExcelL.ExlWs.Cells[ExcelRow, 9] = servMas[3];
                            ExcelL.ExlWs.Cells[ExcelRow, 13] = servMas[4];
                            ExcelL.ExlWs.Cells[ExcelRow, 14] = servMas[5];
                            ExcelL.ExlWs.Cells[ExcelRow, 15] = servMas[6];
                            ExcelL.ExlWs.Cells[ExcelRow, 16] = servMas[7];
                            ExcelL.ExlWs.Cells[ExcelRow, 17] = servMas[8];

                            excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "Q" + ExcelRow.ToString());
                            excells1.Font.Bold = true;

                            //for (int j = 0; j < 12; j++)
                            //    ExcelL.ExlWs.Cells[ExcelRow, ExcelCol + j] = domMas[j];

                            for (int j = 0; j < 9; j++) servMas[j] = 0;

                            oldServ = DT.Rows[i]["service"].ToString().Trim();
                            ExcelRow++;
                            ExcelL.ExlWs.Cells[ExcelRow, 2] = oldServ;
                            ExcelL.ExlWs.Cells[ExcelRow, 1] = count_serv;
                        }

                        ExcelL.ExlWs.Cells[ExcelRow, 3] = DT.Rows[i]["measuref"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 4] = Decimal.Parse(DT.Rows[i]["norma"].ToString().Trim());
                        ExcelL.ExlWs.Cells[ExcelRow, 5] = Int32.Parse(DT.Rows[i]["count_gil"].ToString().Trim());
                        ExcelL.ExlWs.Cells[ExcelRow, 6] = Decimal.Parse(DT.Rows[i]["pl_kvar"].ToString().Trim());
                        ExcelL.ExlWs.Cells[ExcelRow, 7] = Decimal.Parse(DT.Rows[i]["serv_calc"].ToString().Trim());
                        ExcelL.ExlWs.Cells[ExcelRow, 8] = Decimal.Parse(DT.Rows[i]["kfkor"].ToString().Trim());

                        if ((Decimal.Parse(DT.Rows[i]["serv_calc"].ToString().Trim()) == 0) &
                           (Decimal.Parse(DT.Rows[i]["kfkor"].ToString().Trim()) == 0))
                        {
                            ExcelL.ExlWs.Cells[ExcelRow, 7] = Decimal.Parse(DT.Rows[i]["c_calc"].ToString().Trim());
                            servMas[2] = servMas[2] + Decimal.Parse(DT.Rows[i]["c_calc"].ToString());
                        }

                        ExcelL.ExlWs.Cells[ExcelRow, 9] = Decimal.Parse(DT.Rows[i]["c_calc"].ToString().Trim());
                        ExcelL.ExlWs.Cells[ExcelRow, 10] = DT.Rows[i]["frm_name"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 11] = DT.Rows[i]["measure"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 12] = Decimal.Parse(DT.Rows[i]["tarif"].ToString().Trim());
                        ExcelL.ExlWs.Cells[ExcelRow, 13] = Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString().Trim());
                        ExcelL.ExlWs.Cells[ExcelRow, 14] = Decimal.Parse(DT.Rows[i]["reval_k"].ToString().Trim());
                        ExcelL.ExlWs.Cells[ExcelRow, 15] = Decimal.Parse(DT.Rows[i]["reval_d"].ToString().Trim());
                        ExcelL.ExlWs.Cells[ExcelRow, 16] = Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString().Trim());
                        ExcelL.ExlWs.Cells[ExcelRow, 17] = Decimal.Parse(DT.Rows[i]["sum_charge"].ToString().Trim());


                        servMas[0] = servMas[0] + Decimal.Parse(DT.Rows[i]["count_gil"].ToString());
                        servMas[1] = servMas[1] + Decimal.Parse(DT.Rows[i]["pl_kvar"].ToString());
                        servMas[2] = servMas[2] + Decimal.Parse(DT.Rows[i]["serv_calc"].ToString());
                        servMas[3] = servMas[3] + Decimal.Parse(DT.Rows[i]["c_calc"].ToString());
                        servMas[4] = servMas[4] + Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());
                        servMas[5] = servMas[5] + Decimal.Parse(DT.Rows[i]["reval_k"].ToString());
                        servMas[6] = servMas[6] + Decimal.Parse(DT.Rows[i]["reval_d"].ToString());
                        servMas[7] = servMas[7] + Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString());
                        servMas[8] = servMas[8] + Decimal.Parse(DT.Rows[i]["sum_charge"].ToString());

                        irsum_tarif = irsum_tarif + Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());
                        ireval_k = ireval_k + Decimal.Parse(DT.Rows[i]["reval_k"].ToString());
                        ireval_d = ireval_d + Decimal.Parse(DT.Rows[i]["reval_d"].ToString());
                        isum_nedop = isum_nedop + Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString());
                        isum_charge = isum_charge + Decimal.Parse(DT.Rows[i]["sum_charge"].ToString());

                        ExcelRow++;
                        i++;
                    }



                    if (ExcelRow > 7)
                    {
                        count_serv++;

                        ExcelL.ExlWs.Cells[ExcelRow, 2] = "Итого";

                        ExcelL.ExlWs.Cells[ExcelRow, 5] = servMas[0];
                        ExcelL.ExlWs.Cells[ExcelRow, 6] = servMas[1];
                        ExcelL.ExlWs.Cells[ExcelRow, 7] = servMas[2];
                        ExcelL.ExlWs.Cells[ExcelRow, 9] = servMas[3];
                        ExcelL.ExlWs.Cells[ExcelRow, 13] = servMas[4];
                        ExcelL.ExlWs.Cells[ExcelRow, 14] = servMas[5];
                        ExcelL.ExlWs.Cells[ExcelRow, 15] = servMas[6];
                        ExcelL.ExlWs.Cells[ExcelRow, 16] = servMas[7];
                        ExcelL.ExlWs.Cells[ExcelRow, 17] = servMas[8];

                        excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "Q" + ExcelRow.ToString());
                        excells1.Font.Bold = true;


                    }


                    #endregion

                    #region Пишем Итог
                    ExcelRow++;
                    ExcelL.ExlWs.Cells[ExcelRow, 2] = "ВСЕГО";

                    ExcelL.ExlWs.Cells[ExcelRow, 13] = irsum_tarif;
                    ExcelL.ExlWs.Cells[ExcelRow, 14] = ireval_k;
                    ExcelL.ExlWs.Cells[ExcelRow, 15] = ireval_d;
                    ExcelL.ExlWs.Cells[ExcelRow, 16] = isum_nedop;
                    ExcelL.ExlWs.Cells[ExcelRow, 17] = isum_charge;

                    excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "Q" + ExcelRow.ToString());
                    excells1.Font.Bold = true;
                    #endregion

                    #region Форматируем
                    excells1 = ExcelL.ExlWs.get_Range("D7", "I" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";

                    excells1 = ExcelL.ExlWs.get_Range("D7", "I" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";

                    excells1 = ExcelL.ExlWs.get_Range("E7", "E" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "0";

                    excells1 = ExcelL.ExlWs.get_Range("L7", "Q" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";

                    excells1 = ExcelL.ExlWs.get_Range("J7", "K" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                    excells1 = ExcelL.ExlWs.get_Range("B7", "C" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                    excells1 = ExcelL.ExlWs.get_Range("A7", "Q" + ExcelRow.ToString());
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Font.Size = 5;



                    #endregion

                    SetReportSing(ExcelL, ExcelRow + 3, false);
                    for (int c = ExcelRow + 3; c < ExcelRow + 3 + 10; c++)
                    {
                        excells1 = ExcelL.ExlWs.get_Range("A" + c.ToString(), "H" + c.ToString());
                        excells1.Font.Size = 5;
                    }
                    //ExcelL.ExlWs.Cells[ExcelRow + 3, 1] = "Директор";
                    //ExcelL.ExlWs.Cells[ExcelRow + 5, 1] = "Начальник ПЭО";
                    //ExcelL.ExlWs.Cells[ExcelRow + 7, 1] = "Начальник ОНУПН";
                    //ExcelL.ExlWs.Cells[ExcelRow + 3, 3] = " Мякишев М.В.";
                    //ExcelL.ExlWs.Cells[ExcelRow + 5, 3] = " Соковых И.А.";
                    //ExcelL.ExlWs.Cells[ExcelRow + 7, 3] = " Старкова Л.А.";


                    //#endregion

                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "VedNormPotr" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString() + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }

                    #endregion

                }

                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetVedNormPotr DataTable : ОШИБКА!";

                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "VedNormPotr" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }

                    //удаление объекта
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                try
                {
                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "VedNormPotr" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет GetSpravSoderg", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSoderg Excel File : ОШИБКА!";
                }

            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }

            return ret;

        }

        //Отчет: Ведомость оплаты
        public void GetVedOplLs(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Ведомость оплаты\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Ведомость оплаты\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetVedOplLs(cont.listprm[0], cont.nzp_user.ToString(), ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        //Отчет: Ведомость разовых начислений
        public void GetVedPere(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Ведомость разовых начислений\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Ведомость разовых начислений\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetVedPere(cont.listprm[0], cont.nzp_user.ToString(), ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }

        //Отчет: Сведения о просроченной задолженности
        public void GetDolgSved(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Сведения о задолженности\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2,
                "Отчет \"Сведения о задолженности\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetDolgSved(cont.listprm[0], cont.nzp_user.ToString(), ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }


        //Отчет: Сводная ведомость нормативов потребления
        public void GetVedNormPotr(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Сводная ведомость по нормативам потребления КУ\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2,
                "Отчет \"Сводная ведомость по нормативам потребления КУ\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetVedNormPotr(cont.listprm[0], cont.nzp_user.ToString(), ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user +
                    "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }



        public void MakeListKartAnalis(DataRow[] drSelect, ExcelLoader ExcelL, int numList, Prm prm, string nzp_user)
        {
            int excelRow = 7;

            ExcelL.GetWs(numList, "Лист " + numList);

            var sumColls = new decimal[11];

            for (int i = 0; i < 11; i++)
                sumColls[i] = 0;


            #region Создаем шапку

            Range excells1 = ExcelL.ExlWs.Range["A1", "K1"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Карта аналитического учета";
            excells1.Font.Bold = true;
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

            excells1 = ExcelL.ExlWs.Range["A2", "K2"];
            excells1.Merge(Type.Missing);
            excells1.Font.Bold = true;
            //string ERC_name = "";

            //if (STCLINE.KP50.Interfaces.Points.IsDemo == true)
            //    ERC_name = "Н-ска";
            //else
            //    ERC_name = "Самары";
            string sName = SetReportHeader();

            excells1.FormulaR1C1 = sName + " за " +
                prm.month_.ToString("00") + " " + prm.year_ + "г.";
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

            excells1 = ExcelL.ExlWs.Range["A3", "A3"];
            excells1.Merge(Type.Missing);
            excells1.Font.Bold = true;
            excells1.FormulaR1C1 = DateTime.Today.ToShortDateString();
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

            excells1 = ExcelL.ExlWs.Range["B3", "K4"];
            excells1.Font.Bold = true;
            excells1.Merge(Type.Missing);
            //                    excells1.FormulaR1C1 = "Управляющая организация:" + listprm[0].area;
            excells1.FormulaR1C1 = SetReportConditional(Convert.ToInt32(nzp_user));
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
            excells1.RowHeight = 23;

            excells1 = ExcelL.ExlWs.Range["B5", "K5"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "";
            if (numList > 1)
            {
                excells1.FormulaR1C1 = "ЖЭУ: " + drSelect[0]["geu"];
            }
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
            excells1.Font.Bold = true;


            excells1 = ExcelL.ExlWs.Range["A6", "A7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Вид услуги:";
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
            excells1.Font.Size = 8;
            excells1.Font.Bold = true;
            excells1.ColumnWidth = 29;

            excells1 = ExcelL.ExlWs.Range["B6", "B7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Сальдо 01." + prm.month_.ToString("00") + "." + prm.year_ + " г.";
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
            excells1.Font.Size = 8;
            excells1.Font.Bold = true;
            excells1.WrapText = true;
            excells1.ColumnWidth = 11;


            excells1 = ExcelL.ExlWs.Range["C6", "H6"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Начислено";
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
            excells1.Font.Size = 8;
            excells1.Font.Bold = true;

            excells1 = ExcelL.ExlWs.Range["C7", "C7"];
            excells1.FormulaR1C1 = "постоянно";
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
            excells1.Font.Size = 8;
            excells1.Font.Bold = true;

            excells1 = ExcelL.ExlWs.Range["D7", "D7"];
            excells1.FormulaR1C1 = "кор-ка " + Convert.ToChar(10) + "тарифа " + Convert.ToChar(10) + "(МОП)";
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
            excells1.Font.Size = 8;
            excells1.Font.Bold = true;

            excells1 = ExcelL.ExlWs.Range["E7", "E7"];
            excells1.FormulaR1C1 = "возврат";
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
            excells1.Font.Size = 8;
            excells1.Font.Bold = true;

            excells1 = ExcelL.ExlWs.Range["F7", "F7"];
            excells1.FormulaR1C1 = "красные";
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
            excells1.Font.Size = 8;
            excells1.Font.Bold = true;


            excells1 = ExcelL.ExlWs.Range["G7", "G7"];
            excells1.FormulaR1C1 = "черные";
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
            excells1.Font.Size = 8;
            excells1.Font.Bold = true;

            excells1 = ExcelL.ExlWs.Range["H7", "H7"];
            excells1.FormulaR1C1 = "итого";
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
            excells1.Font.Size = 8;
            excells1.Font.Bold = true;

            excells1 = ExcelL.ExlWs.Range["I6", "I7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "к оплате";
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
            excells1.Font.Size = 8;
            excells1.Font.Bold = true;

            excells1 = ExcelL.ExlWs.Range["J6", "J7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "оплачено";
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
            excells1.Font.Size = 8;
            excells1.Font.Bold = true;

            excells1 = ExcelL.ExlWs.Range["K6", "K7"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Сальдо " +
                DateTime.DaysInMonth(Convert.ToInt32(prm.year_),
                Convert.ToInt32(prm.month_)) +
                "." + prm.month_.ToString("00") + "." + prm.year_ + " г.";
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
            excells1.WrapText = true;
            excells1.ColumnWidth = 11.86;
            excells1.Font.Size = 8;
            excells1.Font.Bold = true;
            excelRow++;
            #endregion

            #region Пишем тело
            foreach (DataRow dr in drSelect)
            {
                /*  if (((int)dr["nzp_serv"] == 17) || ((int)dr["nzp_serv"] == 25))
                  {
                      ExcelL.ExlWs.Cells[excelRow, 1] = dr["service"].ToString().Trim();
                      ExcelL.ExlWs.Cells[excelRow, 3] = dr["rsum_tarif"];
                      excelRow++;
                      if ((int)dr["nzp_serv"] == 17)
                      {
                          ExcelL.ExlWs.Cells[excelRow, 1] = "Возврат по э/э (ОДН по Постановлению)";
                      }
                      else
                      {
                          ExcelL.ExlWs.Cells[excelRow, 1] = "э/э на ОДН";
                      }
                      excells1 = ExcelL.ExlWs.get_Range("A" + excelRow.ToString(), "A" + (excelRow).ToString());
                      excells1.Rows.AutoFit();


                      ExcelL.ExlWs.Cells[excelRow - 1, 2] = dr["sum_insaldo"];
                      ExcelL.ExlWs.Cells[excelRow, 2] = dr["sum_insaldo_odn"];

                      ExcelL.ExlWs.Cells[excelRow, 3] = dr["sum_odn"];

                      excells1 = ExcelL.ExlWs.get_Range("D" + excelRow.ToString(), "D" + (excelRow - 1).ToString());
                      excells1.Merge(Type.Missing);
                      excells1.FormulaR1C1 = dr["izm_tarif"];

                      excells1 = ExcelL.ExlWs.get_Range("E" + excelRow.ToString(), "E" + (excelRow - 1).ToString());
                      excells1.Merge(Type.Missing);
                      excells1.FormulaR1C1 = dr["vozv"];

                      excells1 = ExcelL.ExlWs.get_Range("F" + excelRow.ToString(), "F" + (excelRow - 1).ToString());
                      excells1.Merge(Type.Missing);
                      excells1.FormulaR1C1 = dr["reval_k"];

                      excells1 = ExcelL.ExlWs.get_Range("G" + excelRow.ToString(), "G" + (excelRow - 1).ToString());
                      excells1.Merge(Type.Missing);
                      excells1.FormulaR1C1 = dr["reval_d"];

                      excells1 = ExcelL.ExlWs.get_Range("H" + excelRow.ToString(), "H" + (excelRow - 1).ToString());
                      excells1.Merge(Type.Missing);
                      excells1.FormulaR1C1 = dr["sum_ito"];

                      excells1 = ExcelL.ExlWs.get_Range("I" + excelRow.ToString(), "I" + (excelRow - 1).ToString());
                      excells1.Merge(Type.Missing);
                      excells1.FormulaR1C1 = dr["sum_charge"];


                      excells1 = ExcelL.ExlWs.get_Range("J" + excelRow.ToString(), "J" + (excelRow - 1).ToString());
                      excells1.Merge(Type.Missing);
                      excells1.FormulaR1C1 = dr["sum_money"];

                      excells1 = ExcelL.ExlWs.get_Range("K" + excelRow.ToString(), "K" + (excelRow - 1).ToString());
                      excells1.Merge(Type.Missing);
                      excells1.FormulaR1C1 = dr["sum_outsaldo"];

                  }
                  else
                  {*/
                ExcelL.ExlWs.Cells[excelRow, 1] = dr["service"].ToString().Trim();
                ExcelL.ExlWs.Cells[excelRow, 2] = dr["sum_insaldo"];
                ExcelL.ExlWs.Cells[excelRow, 3] = dr["rsum_tarif"];
                ExcelL.ExlWs.Cells[excelRow, 4] = dr["izm_tarif"];
                ExcelL.ExlWs.Cells[excelRow, 5] = dr["vozv"];
                ExcelL.ExlWs.Cells[excelRow, 6] = dr["reval_k"];
                ExcelL.ExlWs.Cells[excelRow, 7] = dr["reval_d"];
                ExcelL.ExlWs.Cells[excelRow, 8] = dr["sum_ito"];
                ExcelL.ExlWs.Cells[excelRow, 9] = dr["sum_charge"];
                ExcelL.ExlWs.Cells[excelRow, 10] = dr["sum_money"];
                ExcelL.ExlWs.Cells[excelRow, 11] = dr["sum_outsaldo"];
                // }
                excelRow++;
                decimal dl;
                if (Decimal.TryParse(dr["sum_insaldo"].ToString(), out dl)) sumColls[1] = sumColls[1] + dl;
                if (Decimal.TryParse(dr["sum_insaldo_odn"].ToString(), out dl)) sumColls[1] = sumColls[1] + dl;
                if (Decimal.TryParse(dr["rsum_tarif"].ToString(), out dl)) sumColls[2] = sumColls[2] + dl;
                if (Decimal.TryParse(dr["sum_odn"].ToString(), out dl)) sumColls[2] = sumColls[2] + dl;
                if (Decimal.TryParse(dr["izm_tarif"].ToString(), out dl)) sumColls[3] = sumColls[3] + dl;
                if (Decimal.TryParse(dr["vozv"].ToString(), out dl)) sumColls[4] = sumColls[4] + dl;
                if (Decimal.TryParse(dr["reval_k"].ToString(), out dl)) sumColls[5] = sumColls[5] + dl;
                if (Decimal.TryParse(dr["reval_d"].ToString(), out dl)) sumColls[6] = sumColls[6] + dl;
                if (Decimal.TryParse(dr["sum_ito"].ToString(), out dl)) sumColls[7] = sumColls[7] + dl;
                if (Decimal.TryParse(dr["sum_charge"].ToString(), out dl)) sumColls[8] = sumColls[8] + dl;
                if (Decimal.TryParse(dr["sum_money"].ToString(), out dl)) sumColls[9] = sumColls[9] + dl;
                if (Decimal.TryParse(dr["sum_outsaldo"].ToString(), out dl)) sumColls[10] = sumColls[10] + dl;
            }

            #endregion

            #region Пишем подвал

            int footerRow = excelRow;


            ExcelL.ExlWs.Cells[footerRow, 1] = "Итого";

            for (int i = 1; i < 11; i++)
            {
                ExcelL.ExlWs.Cells[footerRow, i + 1] = sumColls[i];
            }

            var colarray = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K" };
            foreach (string ch in colarray)
            {
                excells1 = ExcelL.ExlWs.Range[ch + (footerRow), ch + (footerRow)];
                if (ch != "A")
                {
                    excells1.EntireColumn.NumberFormat = "# ##0,00";
                }
                excells1.Font.Bold = true;
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                excells1.EntireColumn.Font.Size = 8;
                if ((ch != "A") & (ch != "B") & (ch != "K"))
                    excells1.EntireColumn.AutoFit();

                excells1 = ExcelL.ExlWs.Range[ch + "7", ch + (footerRow)];
                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
            }

            excells1 = ExcelL.ExlWs.Range["A7", "A" + (footerRow)];
            excells1.EntireRow.AutoFit();

            SetReportSing(ExcelL, footerRow + 3, false);

            ExcelL.ExlWs.PageSetup.Orientation = XlPageOrientation.xlLandscape;


            #endregion


        }



        public Returns GetPaspRasCommon(Prm prm_, string Nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetPaspRasCommon(prm_, out ret, Nzp_user);

            ExcelLoader ExcelL = null;
            try
            {
                ExcelL = new ExcelLoader();
                if (DT != null)
                {
                    ExcelCreater ExcelCr = new ExcelCreater();

                    #region Создаем шапку
                    Microsoft.Office.Interop.Excel.Range excells1;

                    Dictionary<string, string> Dic = new Dictionary<string, string>();
                    Dic.Add("ERCName", SetReportHeader());
                    string strMonthName = "";

                    #region Получаем название месяца

                    switch (prm_.month_)
                    {
                        case 1:
                            strMonthName = "ЯНВАРЬ";
                            break;
                        case 2:
                            strMonthName = "ФЕВРАЛЬ";
                            break;
                        case 3:
                            strMonthName = "МАРТ";
                            break;
                        case 4:
                            strMonthName = "АПРЕЛЬ";
                            break;
                        case 5:
                            strMonthName = "МАЙ";
                            break;
                        case 6:
                            strMonthName = "ИЮНЬ";
                            break;
                        case 7:
                            strMonthName = "ИЮЛЬ";
                            break;
                        case 8:
                            strMonthName = "АВГУСТ";
                            break;
                        case 9:
                            strMonthName = "СЕНТЯБРЬ";
                            break;
                        case 10:
                            strMonthName = "ОКТЯБРЬ";
                            break;
                        case 11:
                            strMonthName = "НОЯБРЬ";
                            break;
                        case 12:
                            strMonthName = "ДЕКАБРЬ";
                            break;
                        default:
                            break;
                    }
                    #endregion

                    Dic.Add("month", strMonthName);//месяц
                    Dic.Add("year", prm_.year_.ToString());//год
                    Dic.Add("DATE", DateTime.Now.ToShortDateString());//год
                    #endregion

                    ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("pasp_ras.xls"), Dic);
                    excells1 = ExcelL.ExlWs.get_Range("D3", "K4");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = SetReportConditional(System.Convert.ToInt32(Nzp_user));
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.Font.Size = 8;
                    excells1.RowHeight = 23;

                    #region Пишем тело

                    int ExcelRow = 7;
                    int i = 0;

                    while (i < DT.Rows.Count)
                    {

                        ExcelL.ExlWs.Cells[ExcelRow, 1] = i + 1;
                        ExcelL.ExlWs.Cells[ExcelRow, 2] = DT.Rows[i]["num_ls"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 3] = DT.Rows[i]["ulica"].ToString().Trim().Replace("/ -", "");
                        ExcelL.ExlWs.Cells[ExcelRow, 4] = DT.Rows[i]["ndom"].ToString().Trim().Replace("-", " ");
                        ExcelL.ExlWs.Cells[ExcelRow, 5] = DT.Rows[i]["nkor"].ToString().Trim().Replace("-", " ");
                        ExcelL.ExlWs.Cells[ExcelRow, 6] = DT.Rows[i]["nkvar"].ToString().Trim().Replace("-", " ");
                        ExcelL.ExlWs.Cells[ExcelRow, 7] = DT.Rows[i]["nkvar_n"].ToString().Trim().Replace("-", " ");
                        ExcelL.ExlWs.Cells[ExcelRow, 8] = DT.Rows[i]["fio"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 9] = DT.Rows[i]["sost"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 10] = DT.Rows[i]["kol_gil"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 11] = DT.Rows[i]["kol_prm"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 12] = DT.Rows[i]["kol_prm_pasp"].ToString();
                        //                        ExcelL.ExlWs.Cells[ExcelRow, 7] = Decimal.Parse(DT.Rows[i]["sum_insaldo"].ToString());
                        ExcelRow++;
                        i++;
                    }
                    #endregion

                    #region форматируем
                    if (ExcelRow > 7) ExcelRow--;
                    //excells1 = ExcelL.ExlWs.get_Range("G7", "J" + ExcelRow.ToString());
                    //excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    //excells1.EntireColumn.NumberFormat = "# ##0,00";

                    excells1 = ExcelL.ExlWs.get_Range("A7", "L" + ExcelRow.ToString());
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Font.Size = 8;

                    //                    SetReportSing(ExcelL, ExcelRow+3);

                    #endregion

                    SetReportSing(ExcelL, ExcelRow + 3, false);

                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "PaspRas" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString() + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }

                    #endregion
                }
                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetPaspRas DataTable : ОШИБКА!";

                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "PaspRas" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                try
                {
                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "PaspRas" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет GetPaspRas", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetPaspRas Excel File : ОШИБКА!";
                }
            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }
            return ret;
        }


        public Returns GetSostGilFond(Prm prm_, string Nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            GetSostGilFondZhig(prm_, Nzp_user, ref fileName);
            return ret;
            //создание dataTable
            //ExcelRep ExR = new ExcelRep();
            //System.Data.DataTable DT = ExR.GetGilFondStat(prm_, out ret, Nzp_user);
            //System.Data.DataTable DT_kvar = ExR.GetKvarStat(prm_, out ret, Nzp_user);

            //ExcelLoader ExcelL = null;
            //try
            //{
            //    ExcelL = new ExcelLoader();

            //    if (DT != null)
            //    {

            //        ExcelCreater ExcelCr = new ExcelCreater();


            //        #region Создаем шапку
            //        Microsoft.Office.Interop.Excel.Range excells1;

            //        Dictionary<string, string> Dic = new Dictionary<string, string>();
            //        Dic.Add("ERCName", SetReportHeader());
            //        string strMonthName = "";

            //        #region Получаем название месяца

            //        switch (prm_.month_)
            //        {
            //            case 1:
            //                strMonthName = "ЯНВАРЬ";
            //                break;
            //            case 2:
            //                strMonthName = "ФЕВРАЛЬ";
            //                break;
            //            case 3:
            //                strMonthName = "МАРТ";
            //                break;
            //            case 4:
            //                strMonthName = "АПРЕЛЬ";
            //                break;
            //            case 5:
            //                strMonthName = "МАЙ";
            //                break;
            //            case 6:
            //                strMonthName = "ИЮНЬ";
            //                break;
            //            case 7:
            //                strMonthName = "ИЮЛЬ";
            //                break;
            //            case 8:
            //                strMonthName = "АВГУСТ";
            //                break;
            //            case 9:
            //                strMonthName = "СЕНТЯБРЬ";
            //                break;
            //            case 10:
            //                strMonthName = "ОКТЯБРЬ";
            //                break;
            //            case 11:
            //                strMonthName = "НОЯБРЬ";
            //                break;
            //            case 12:
            //                strMonthName = "ДЕКАБРЬ";
            //                break;
            //            default:
            //                break;
            //        }
            //        #endregion

            //        Dic.Add("month", strMonthName);//месяц
            //        Dic.Add("year", prm_.year_.ToString());//год
            //        Dic.Add("DATE", DateTime.Now.ToShortDateString());//год
            //        #endregion


            //        ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("SostGilfond.xls"), Dic);

            //        excells1 = ExcelL.ExlWs.get_Range("D3", "L4");
            //        excells1.Merge(Type.Missing);
            //        excells1.FormulaR1C1 = SetReportConditional(System.Convert.ToInt32(Nzp_user));
            //        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
            //        excells1.Font.Size = 5;
            //        excells1.RowHeight = 23;

            //        #region Заполняем таблицу по услугам
            //        int ExcelRow = 15;

            //        DataRow[] dr1 = DT.Select("tip_str = 1");

            //        int countDom = 0;
            //        decimal plKvarYear = 0;

            //        foreach (DataRow dataRow in dr1)
            //        {
            //            ExcelL.ExlWs.Cells[ExcelRow, 1] = dataRow["frm_name"].ToString().Trim();
            //            ExcelL.ExlWs.Cells[ExcelRow, 2] = Decimal.Parse(dataRow["pl_kvar"].ToString().Trim());
            //            ExcelL.ExlWs.Cells[ExcelRow, 3] = Decimal.Parse(dataRow["pl_gil"].ToString().Trim());
            //            ExcelL.ExlWs.Cells[ExcelRow, 4] = Decimal.Parse(dataRow["pl_calc"].ToString().Trim());
            //            ExcelL.ExlWs.Cells[ExcelRow, 5] = dataRow["count_ls"].ToString().Trim();
            //            ExcelL.ExlWs.Cells[ExcelRow, 6] = dataRow["count_gil"].ToString().Trim();
            //            countDom = Math.Max(countDom, Int32.Parse(dataRow["count_dom"].ToString().Trim()));
            //            plKvarYear = Math.Max(plKvarYear, Decimal.Parse(dataRow["pl_kvar_year"].ToString().Trim()));

            //            ExcelRow++;

            //            if (ExcelRow > 22)
            //            {
            //                excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "L" + ExcelRow.ToString());
            //                excells1.EntireRow.Insert(Microsoft.Office.Interop.Excel.XlInsertShiftDirection.xlShiftDown, Type.Missing);
            //            }
            //        }




            //        if (ExcelRow > 15) ExcelRow--;

            //        excells1 = ExcelL.ExlWs.get_Range("B15", "D15" + ExcelRow.ToString());
            //        excells1.EntireColumn.NumberFormat = "# ##0,00";

            //        excells1 = ExcelL.ExlWs.get_Range("A15", "F" + ExcelRow.ToString());
            //        excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
            //        excells1.Font.Size = 5;
            //        #endregion


            //        #region Заполняем верхнее итого
            //        DataRow[] dr2 = DT.Select("tip_str = 2");

            //        foreach (DataRow dataRow in dr2)
            //        {
            //            ExcelL.ExlWs.Cells[5, 4] = dataRow["pl_dom"];
            //            ExcelL.ExlWs.Cells[6, 4] = Decimal.Parse(dataRow["pl_kvar"].ToString().Trim());
            //            ExcelL.ExlWs.Cells[7, 4] = Decimal.Parse(dataRow["pl_calc"].ToString().Trim());
            //            ExcelL.ExlWs.Cells[8, 4] = dataRow["pl_ngil"];
            //            ExcelL.ExlWs.Cells[9, 4] = dataRow["pl_mop"];
            //            ExcelL.ExlWs.Cells[10, 4] = dataRow["count_gil"].ToString().Trim();
            //            ExcelL.ExlWs.Cells[11, 4] = dataRow["count_gilp"].ToString().Trim();

            //            ExcelL.ExlWs.Cells[6, 9] = Decimal.Parse(dataRow["pl_close"].ToString().Trim());
            //            ExcelL.ExlWs.Cells[7, 9] = Decimal.Parse(dataRow["pl_calc_close"].ToString().Trim());


            //        }
            //        #endregion

            //        if (ExcelRow > 22) ExcelRow = ExcelRow + 6;
            //        else ExcelRow = 28;

            //        #region Вторую таблицу

            //        DataRow[] dr3 = DT.Select("tip_str = 0");

            //        decimal[] itoMas = new decimal[21];
            //        decimal[] itoMasDom = new decimal[21];

            //        for (int i = 1; i < 20; i++) itoMas[i] = 0;
            //        for (int i = 1; i < 20; i++) itoMasDom[i] = 0;

            //        string oldTipDoma = "";
            //        int oldRowTipDoma = ExcelRow;
            //        foreach (DataRow dataRow in dr3)
            //        {
            //            if (oldTipDoma != dataRow["tip_doma"].ToString().Trim())
            //            {
            //                for (int i = 2; i < 20; i++) ExcelL.ExlWs.Cells[oldRowTipDoma, i] = itoMasDom[i];


            //                ExcelL.ExlWs.Cells[ExcelRow, 1] = dataRow["tip_doma"].ToString().Trim();
            //                excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "S" + ExcelRow.ToString());
            //                excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
            //                //excells1.EntireColumn.NumberFormat = "# ##0,00";
            //                excells1.Font.Bold = true;

            //                oldRowTipDoma = ExcelRow;
            //                ExcelRow++;
            //                oldTipDoma = dataRow["tip_doma"].ToString().Trim();
            //                for (int i = 2; i < 20; i++) itoMasDom[i] = 0;

            //            }
            //            ExcelL.ExlWs.Cells[ExcelRow, 1] = dataRow["frm_name"].ToString().Trim();
            //            ExcelL.ExlWs.Cells[ExcelRow, 2] = Decimal.Parse(dataRow["pl_kvar"].ToString().Trim());
            //            ExcelL.ExlWs.Cells[ExcelRow, 3] = Decimal.Parse(dataRow["pl_gil"].ToString().Trim());
            //            ExcelL.ExlWs.Cells[ExcelRow, 4] = Decimal.Parse(dataRow["pl_calc"].ToString().Trim());
            //            ExcelL.ExlWs.Cells[ExcelRow, 5] = dataRow["count_ls"].ToString().Trim();
            //            ExcelL.ExlWs.Cells[ExcelRow, 6] = dataRow["count_gil"].ToString().Trim();
            //            ExcelL.ExlWs.Cells[ExcelRow, 7] = Decimal.Parse(dataRow["rsum_tarif_i"].ToString().Trim()) +
            //                Decimal.Parse(dataRow["rsum_tarif_k"].ToString().Trim()) +
            //                Decimal.Parse(dataRow["rsum_tarif_close"].ToString().Trim());
            //            ExcelL.ExlWs.Cells[ExcelRow, 8] = dataRow["count_close"].ToString().Trim();
            //            ExcelL.ExlWs.Cells[ExcelRow, 9] = Decimal.Parse(dataRow["pl_calc_close"].ToString().Trim());
            //            ExcelL.ExlWs.Cells[ExcelRow, 10] = dataRow["count_ls_i"].ToString().Trim();
            //            ExcelL.ExlWs.Cells[ExcelRow, 11] = dataRow["count_gil_i"].ToString().Trim();
            //            ExcelL.ExlWs.Cells[ExcelRow, 12] = Decimal.Parse(dataRow["tarif_i"].ToString().Trim());
            //            ExcelL.ExlWs.Cells[ExcelRow, 13] = Decimal.Parse(dataRow["pl_calc_i"].ToString().Trim());
            //            ExcelL.ExlWs.Cells[ExcelRow, 14] = Decimal.Parse(dataRow["rsum_tarif_i"].ToString().Trim());
            //            ExcelL.ExlWs.Cells[ExcelRow, 15] = dataRow["count_ls_k"].ToString().Trim();
            //            ExcelL.ExlWs.Cells[ExcelRow, 16] = dataRow["count_gil_k"].ToString().Trim();
            //            ExcelL.ExlWs.Cells[ExcelRow, 17] = Decimal.Parse(dataRow["tarif_k"].ToString().Trim());
            //            ExcelL.ExlWs.Cells[ExcelRow, 18] = Decimal.Parse(dataRow["pl_calc_k"].ToString().Trim());
            //            ExcelL.ExlWs.Cells[ExcelRow, 19] = Decimal.Parse(dataRow["rsum_tarif_k"].ToString().Trim());

            //            itoMas[2] = itoMas[2] + Decimal.Parse(dataRow["pl_kvar"].ToString().Trim());
            //            itoMas[3] = itoMas[3] + Decimal.Parse(dataRow["pl_gil"].ToString().Trim());
            //            itoMas[4] = itoMas[4] + Decimal.Parse(dataRow["pl_calc"].ToString().Trim());
            //            itoMas[5] = itoMas[5] + System.Convert.ToInt32(dataRow["count_ls"].ToString().Trim());
            //            itoMas[6] = itoMas[6] + System.Convert.ToInt32(dataRow["count_gil"].ToString().Trim());
            //            itoMas[7] = itoMas[7] + Decimal.Parse(dataRow["rsum_tarif_i"].ToString().Trim()) +
            //                Decimal.Parse(dataRow["rsum_tarif_k"].ToString().Trim()) +
            //                Decimal.Parse(dataRow["rsum_tarif_close"].ToString().Trim());
            //            itoMas[8] = itoMas[8] + System.Convert.ToInt32(dataRow["count_close"].ToString().Trim());
            //            itoMas[9] = itoMas[9] + Decimal.Parse(dataRow["pl_calc_close"].ToString().Trim());
            //            itoMas[10] = itoMas[10] + System.Convert.ToInt32(dataRow["count_ls_i"].ToString().Trim());
            //            itoMas[11] = itoMas[11] + System.Convert.ToInt32(dataRow["count_gil_i"].ToString().Trim());
            //            itoMas[12] = itoMas[12] + Decimal.Parse(dataRow["tarif_i"].ToString().Trim());
            //            itoMas[13] = itoMas[13] + Decimal.Parse(dataRow["pl_calc_i"].ToString().Trim());
            //            itoMas[14] = itoMas[14] + Decimal.Parse(dataRow["rsum_tarif_i"].ToString().Trim());
            //            itoMas[15] = itoMas[15] + System.Convert.ToInt32(dataRow["count_ls_k"].ToString().Trim());
            //            itoMas[16] = itoMas[16] + System.Convert.ToInt32(dataRow["count_gil_k"].ToString().Trim());
            //            itoMas[17] = itoMas[17] + Decimal.Parse(dataRow["tarif_k"].ToString().Trim());
            //            itoMas[18] = itoMas[18] + Decimal.Parse(dataRow["pl_calc_k"].ToString().Trim());
            //            itoMas[19] = itoMas[19] + Decimal.Parse(dataRow["rsum_tarif_k"].ToString().Trim());

            //            itoMasDom[2] = itoMasDom[2] + Decimal.Parse(dataRow["pl_kvar"].ToString().Trim());
            //            itoMasDom[3] = itoMasDom[3] + Decimal.Parse(dataRow["pl_gil"].ToString().Trim());
            //            itoMasDom[4] = itoMasDom[4] + Decimal.Parse(dataRow["pl_calc"].ToString().Trim());
            //            itoMasDom[5] = itoMasDom[5] + System.Convert.ToInt32(dataRow["count_ls"].ToString().Trim());
            //            itoMasDom[6] = itoMasDom[6] + System.Convert.ToInt32(dataRow["count_gil"].ToString().Trim());
            //            itoMasDom[7] = itoMasDom[7] + Decimal.Parse(dataRow["rsum_tarif_i"].ToString().Trim()) +
            //                Decimal.Parse(dataRow["rsum_tarif_k"].ToString().Trim()) +
            //                Decimal.Parse(dataRow["rsum_tarif_close"].ToString().Trim());
            //            itoMasDom[8] = itoMasDom[8] + System.Convert.ToInt32(dataRow["count_close"].ToString().Trim());
            //            itoMasDom[9] = itoMasDom[9] + Decimal.Parse(dataRow["pl_calc_close"].ToString().Trim());
            //            itoMasDom[10] = itoMasDom[10] + System.Convert.ToInt32(dataRow["count_ls_i"].ToString().Trim());
            //            itoMasDom[11] = itoMasDom[11] + System.Convert.ToInt32(dataRow["count_gil_i"].ToString().Trim());
            //            itoMasDom[12] = itoMasDom[12] + Decimal.Parse(dataRow["tarif_i"].ToString().Trim());
            //            itoMasDom[13] = itoMasDom[13] + Decimal.Parse(dataRow["pl_calc_i"].ToString().Trim());
            //            itoMasDom[14] = itoMasDom[14] + Decimal.Parse(dataRow["rsum_tarif_i"].ToString().Trim());
            //            itoMasDom[15] = itoMasDom[15] + System.Convert.ToInt32(dataRow["count_ls_k"].ToString().Trim());
            //            itoMasDom[16] = itoMasDom[16] + System.Convert.ToInt32(dataRow["count_gil_k"].ToString().Trim());
            //            itoMasDom[17] = itoMasDom[17] + Decimal.Parse(dataRow["tarif_k"].ToString().Trim());
            //            itoMasDom[18] = itoMasDom[18] + Decimal.Parse(dataRow["pl_calc_k"].ToString().Trim());
            //            itoMasDom[19] = itoMasDom[19] + Decimal.Parse(dataRow["rsum_tarif_k"].ToString().Trim());

            //            excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "S" + ExcelRow.ToString());
            //            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
            //            ExcelRow++;

            //        }
            //        #endregion

            //        for (int i = 2; i < 20; i++) ExcelL.ExlWs.Cells[ExcelRow, i] = itoMas[i];
            //        ExcelL.ExlWs.Cells[ExcelRow, 1] = "Всего";
            //        for (int i = 2; i < 20; i++) ExcelL.ExlWs.Cells[oldRowTipDoma, i] = itoMasDom[i];
            //        excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "S" + ExcelRow.ToString());
            //        excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
            //        excells1.Font.Bold = true;


            //        #region Заполняем таблицу по комнатам

            //        for (int i = 1; i < 20; i++) itoMas[i] = 0;
            //        for (int i = 0; i < DT_kvar.Rows.Count; i++)
            //        {
            //            for (int j = 1; j < 14; j++) itoMas[j] = itoMas[j] + Decimal.Parse(DT_kvar.Rows[i][j].ToString().Trim());
            //        }


            //        ExcelRow = ExcelRow + 3;
            //        excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "E" + ExcelRow.ToString());
            //        excells1.Merge(Type.Missing);
            //        excells1.Font.Bold = true;
            //        excells1.Value2 = "Число жилых квартир";
            //        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
            //        ExcelRow++;

            //        ExcelL.ExlWs.Cells[ExcelRow, 1] = "Наименование";
            //        ExcelL.ExlWs.Cells[ExcelRow, 2] = "Кол-во единиц";
            //        ExcelL.ExlWs.Cells[ExcelRow, 3] = "в т.ч. изол. квартир";
            //        ExcelL.ExlWs.Cells[ExcelRow, 4] = "в т.ч. коммун. квартир";
            //        ExcelL.ExlWs.Cells[ExcelRow, 5] = "в т.ч. приватиз. квартир";
            //        ExcelL.ExlWs.Cells[ExcelRow, 6] = "в т.ч. втор/приватиз. квартир";
            //        ExcelL.ExlWs.Cells[ExcelRow, 7] = "в т.ч. служебных квартир";
            //        ExcelL.ExlWs.Cells[ExcelRow, 8] = "в т.ч. собственных квартир";
            //        ExcelL.ExlWs.Cells[ExcelRow, 9] = "в т.ч. Юр. лица";
            //        ExcelL.ExlWs.Cells[ExcelRow, 10] = "в т.ч. неприватиз. квартир";
            //        ExcelL.ExlWs.Cells[ExcelRow, 11] = "Их общая площадь кв.м.";
            //        ExcelL.ExlWs.Cells[ExcelRow, 12] = "Их жилая площадь кв.м.";
            //        ExcelL.ExlWs.Cells[ExcelRow, 13] = "в т.ч. площадь дял расчетов кв.м.";
            //        ExcelL.ExlWs.Cells[ExcelRow, 14] = "число постоянно проживающих, чел";

            //        excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "N" + ExcelRow.ToString());
            //        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
            //        excells1.Font.Size = 5;
            //        excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
            //        excells1.WrapText = true;
            //        ExcelRow++;
            //        excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "N" + (ExcelRow + DT_kvar.Rows.Count).ToString());
            //        excells1.Font.Size = 5;
            //        excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;


            //        excells1 = ExcelL.ExlWs.get_Range("B" + ExcelRow.ToString(), "J" + (ExcelRow + DT_kvar.Rows.Count).ToString());
            //        excells1.NumberFormat = "0";
            //        excells1 = ExcelL.ExlWs.get_Range("K" + ExcelRow.ToString(), "M" + (ExcelRow + DT_kvar.Rows.Count).ToString());
            //        excells1.NumberFormat = "# ##0,00";
            //        excells1 = ExcelL.ExlWs.get_Range("N" + ExcelRow.ToString(), "N" + (ExcelRow + DT_kvar.Rows.Count).ToString());
            //        excells1.NumberFormat = "0";

            //        ExcelL.ExlWs.Cells[ExcelRow, 1] = "ВСЕГО КВАРТИР";
            //        for (int j = 1; j < 14; j++) ExcelL.ExlWs.Cells[ExcelRow, j + 1] = itoMas[j];
            //        ExcelRow++;
            //        for (int i = 0; i < DT_kvar.Rows.Count; i++)
            //        {
            //            if (DT_kvar.Rows[i][0].ToString() == "4")
            //                ExcelL.ExlWs.Cells[ExcelRow, 1] = "в т.ч." + DT_kvar.Rows[i][0].ToString().Trim() + "-комнатных и более ";
            //            else
            //                ExcelL.ExlWs.Cells[ExcelRow, 1] = "в т.ч." + DT_kvar.Rows[i][0].ToString().Trim() + "-комнатных";
            //            for (int j = 1; j < 14; j++) ExcelL.ExlWs.Cells[ExcelRow, j + 1] = Decimal.Parse(DT_kvar.Rows[i][j].ToString().Trim());
            //            ExcelRow++;
            //        }

            //        #endregion


            //        #region Заполнение Итого квартир по году
            //        ExcelRow += 3;
            //        ExcelL.ExlWs.Cells[ExcelRow, 1] = "Наличие площади на начало года - всего, кв.м :";
            //        ExcelL.ExlWs.Cells[ExcelRow + 1, 1] = "Число жилых строений - всего :";
            //        ExcelL.ExlWs.Cells[ExcelRow, 3] = plKvarYear;
            //        ExcelL.ExlWs.Cells[ExcelRow + 1, 3] = countDom;
            //        excells1 = ExcelL.ExlWs.get_Range("C" + (ExcelRow + 1).ToString(), "C" + (ExcelRow + 1).ToString());
            //        excells1.NumberFormat = "0";

            //        #endregion

            //        SetReportSing(ExcelL, ExcelRow + 3, false);

            //        for (int c = ExcelRow + 3; c < ExcelRow + 3 + 10; c++)
            //        {
            //            excells1 = ExcelL.ExlWs.get_Range("A" + c.ToString(), "H" + c.ToString());
            //            excells1.Font.Size = 5;
            //        }
            //        //ExcelL.ExlWs.Cells[ExcelRow + 3, 1] = "Директор";
            //        //ExcelL.ExlWs.Cells[ExcelRow + 5, 1] = "Начальник ПЭО";
            //        //ExcelL.ExlWs.Cells[ExcelRow + 7, 1] = "Начальник ОНУПН";
            //        //ExcelL.ExlWs.Cells[ExcelRow + 3, 3] = " Мякишев М.В.";
            //        //ExcelL.ExlWs.Cells[ExcelRow + 5, 3] = " Соковых И.А.";
            //        //ExcelL.ExlWs.Cells[ExcelRow + 7, 3] = " Старкова Л.А.";

            //        //#region форматируем

            //        //excells1 = ExcelL.ExlWs.get_Range("G7", "J" + ExcelRow.ToString());
            //        //excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
            //        //excells1.EntireColumn.NumberFormat = "# ##0,00";

            //        //excells1 = ExcelL.ExlWs.get_Range("A7", "L" + ExcelRow.ToString());
            //        //excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
            //        //excells1.Font.Size = 6;


            //        //#endregion

            //        #region Сохраняем файл
            //        try
            //        {

            //            fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SostGilFond" +
            //                prm_.year_.ToString() + "_" +
            //                prm_.month_.ToString() + "_" + Nzp_user) + ".xls";
            //            ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
            //            if (InputOutput.useFtp)
            //            {
            //                fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
            //            }

            //        }
            //        catch (Exception ex)
            //        {
            //            MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
            //        }

            //        #endregion

            //    }

            //    else
            //    {
            //        MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
            //        ret.result = false;
            //        ret.text = "Создание GetSostGilFond DataTable : ОШИБКА!";

            //        if (ExcelL != null)
            //        {
            //            fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SostGilFond" +
            //                prm_.year_.ToString() + "_" +
            //                prm_.month_.ToString()
            //                + "_" + Nzp_user) + ".xls";
            //            ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
            //            if (InputOutput.useFtp)
            //            {
            //                fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
            //            }

            //        }

            //        //удаление объекта
            //        return ret;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
            //    try
            //    {
            //        if (ExcelL != null)
            //        {
            //            fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SostGilFond" +
            //                prm_.year_.ToString() + "_" +
            //                prm_.month_.ToString()
            //                + "_" + Nzp_user) + ".xls";
            //            ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
            //            if (InputOutput.useFtp)
            //            {
            //                fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
            //            }

            //        }
            //    }
            //    catch
            //    {
            //        MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет GetSostGilFond", MonitorLog.typelog.Warn, true);
            //        ret.result = false;
            //        ret.text = "Создание GetSostGilFond Excel File : ОШИБКА!";
            //    }

            //}
            //finally
            //{
            //    //удаление объекта
            //    if (ExcelL != null)
            //    {
            //        ExcelL.DeleteObject();
            //    }
            //}

            //return ret;

        }


        public Returns GetSostGilFondZhig(Prm prm, string nzpUser, ref string fileName)
        {
            Returns ret;

            //создание dataTable
            var exR = new ExcelRep();
            System.Data.DataTable dt = exR.GetGilFondStat(prm, out ret, nzpUser);
            System.Data.DataTable dtKvar = exR.GetKvarStat(prm, out ret, nzpUser);

            ExcelLoader excelL = null;
            try
            {
                excelL = new ExcelLoader();

                if (dt != null)
                {

                    //   ExcelCreater excelCr = new ExcelCreater();


                    #region Создаем шапку

                    var dic = new Dictionary<string, string> { { "ERCName", SetReportHeader() } };
                    string strMonthName = "";

                    #region Получаем название месяца

                    switch (prm.month_)
                    {
                        case 1:
                            strMonthName = "ЯНВАРЬ";
                            break;
                        case 2:
                            strMonthName = "ФЕВРАЛЬ";
                            break;
                        case 3:
                            strMonthName = "МАРТ";
                            break;
                        case 4:
                            strMonthName = "АПРЕЛЬ";
                            break;
                        case 5:
                            strMonthName = "МАЙ";
                            break;
                        case 6:
                            strMonthName = "ИЮНЬ";
                            break;
                        case 7:
                            strMonthName = "ИЮЛЬ";
                            break;
                        case 8:
                            strMonthName = "АВГУСТ";
                            break;
                        case 9:
                            strMonthName = "СЕНТЯБРЬ";
                            break;
                        case 10:
                            strMonthName = "ОКТЯБРЬ";
                            break;
                        case 11:
                            strMonthName = "НОЯБРЬ";
                            break;
                        case 12:
                            strMonthName = "ДЕКАБРЬ";
                            break;
                    }
                    #endregion

                    dic.Add("month", strMonthName);//месяц
                    dic.Add("year", prm.year_.ToString(CultureInfo.InvariantCulture));//год
                    dic.Add("DATE", DateTime.Now.ToShortDateString());//год
                    #endregion


                    excelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("SostGilfondZhig.xls"), dic);

                    Range excells1 = excelL.ExlWs.Range["D3", "L4"];
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = SetReportConditional(Convert.ToInt32(nzpUser));
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.Font.Size = 5;
                    excells1.RowHeight = 23;

                    #region Заполняем таблицу по услугам
                    int excelRow = 12;

                    DataRow[] dr1 = dt.Select("tip_str = 1");

                    int countDom = 0;
                    decimal plKvarYear = 0;

                    foreach (DataRow dataRow in dr1)
                    {
                        excelL.ExlWs.Cells[excelRow, 1] = dataRow["frm_name"].ToString().Trim();
                        excelL.ExlWs.Cells[excelRow, 2] = Decimal.Parse(dataRow["pl_kvar"].ToString().Trim());
                        excelL.ExlWs.Cells[excelRow, 3] = Decimal.Parse(dataRow["pl_gil"].ToString().Trim());
                        excelL.ExlWs.Cells[excelRow, 4] = Decimal.Parse(dataRow["pl_calc"].ToString().Trim());
                        excelL.ExlWs.Cells[excelRow, 5] = dataRow["count_ls"].ToString().Trim();
                        excelL.ExlWs.Cells[excelRow, 6] = dataRow["count_gil"].ToString().Trim();
                        countDom = Math.Max(countDom, Int32.Parse(dataRow["count_dom"].ToString().Trim()));
                        plKvarYear = Math.Max(plKvarYear, Decimal.Parse(dataRow["pl_kvar_year"].ToString().Trim()));

                        excelRow++;

                        if (excelRow > 22)
                        {
                            excells1 = excelL.ExlWs.Range["A" + excelRow, "L" + excelRow];
                            excells1.EntireRow.Insert(XlInsertShiftDirection.xlShiftDown, Type.Missing);
                        }
                    }




                    if (excelRow > 15) excelRow--;

                    excells1 = excelL.ExlWs.Range["B12", "D" + excelRow];
                    excells1.EntireColumn.NumberFormat = "# ##0,00";

                    excells1 = excelL.ExlWs.Range["A12", "F" + excelRow];
                    excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                    excells1.Font.Size = 5;
                    #endregion


                    #region Заполняем верхнее итого
                    DataRow[] dr2 = dt.Select("tip_str = 2");

                    foreach (DataRow dataRow in dr2)
                    {
                        excelL.ExlWs.Cells[5, 4] = Decimal.Parse(dataRow["pl_kvar"].ToString().Trim());
                        excelL.ExlWs.Cells[6, 4] = Decimal.Parse(dataRow["pl_calc"].ToString().Trim());
                        excelL.ExlWs.Cells[7, 4] = dataRow["count_gil"].ToString().Trim();
                        excelL.ExlWs.Cells[8, 4] = dataRow["count_gilp"].ToString().Trim();

                        excelL.ExlWs.Cells[5, 9] = Decimal.Parse(dataRow["pl_close"].ToString().Trim());
                        excelL.ExlWs.Cells[6, 9] = Decimal.Parse(dataRow["pl_calc_close"].ToString().Trim());


                    }
                    #endregion

                    excelRow = excelRow + 2;





                    #region Заполняем таблицу по комнатам

                    var itoMas = new decimal[20];
                    for (int i = 1; i < 20; i++) itoMas[i] = 0;
                    for (int i = 0; i < dtKvar.Rows.Count; i++)
                    {
                        for (int j = 1; j < 14; j++) itoMas[j] = itoMas[j] + Decimal.Parse(dtKvar.Rows[i][j].ToString().Trim());
                    }


                    excelRow = excelRow + 3;
                    excells1 = excelL.ExlWs.Range["A" + excelRow, "E" + excelRow];
                    excells1.Merge(Type.Missing);
                    excells1.Font.Bold = true;
                    excells1.Value2 = "Число жилых квартир";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excelRow++;

                    excelL.ExlWs.Cells[excelRow, 1] = "Наименование";
                    excelL.ExlWs.Cells[excelRow, 2] = "Кол-во единиц";
                    excelL.ExlWs.Cells[excelRow, 3] = "в т.ч. изол. квартир";
                    excelL.ExlWs.Cells[excelRow, 4] = "в т.ч. коммун. квартир";
                    excelL.ExlWs.Cells[excelRow, 5] = "в т.ч. приватиз. квартир";
                    excelL.ExlWs.Cells[excelRow, 6] = "в т.ч. втор/приватиз. квартир";
                    excelL.ExlWs.Cells[excelRow, 7] = "в т.ч. служебных квартир";
                    excelL.ExlWs.Cells[excelRow, 8] = "в т.ч. собственных квартир";
                    excelL.ExlWs.Cells[excelRow, 9] = "в т.ч. Юр. лица";
                    excelL.ExlWs.Cells[excelRow, 10] = "в т.ч. неприватиз. квартир";
                    excelL.ExlWs.Cells[excelRow, 11] = "Их общая площадь кв.м.";
                    excelL.ExlWs.Cells[excelRow, 12] = "Их жилая площадь кв.м.";
                    excelL.ExlWs.Cells[excelRow, 13] = "в т.ч. площадь дял расчетов кв.м.";
                    excelL.ExlWs.Cells[excelRow, 14] = "число постоянно проживающих, чел";

                    excells1 = excelL.ExlWs.Range["A" + excelRow, "N" + excelRow];
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Font.Size = 5;
                    excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                    excells1.WrapText = true;
                    excelRow++;
                    excells1 = excelL.ExlWs.Range["A" + excelRow, "N" + (excelRow + dtKvar.Rows.Count)];
                    excells1.Font.Size = 5;
                    excells1.Borders.LineStyle = XlLineStyle.xlContinuous;


                    excells1 = excelL.ExlWs.Range["B" + excelRow, "J" + (excelRow + dtKvar.Rows.Count)];
                    excells1.NumberFormat = "0";
                    excells1 = excelL.ExlWs.Range["K" + excelRow, "M" + (excelRow + dtKvar.Rows.Count)];
                    excells1.NumberFormat = "# ##0,00";
                    excells1 = excelL.ExlWs.Range["N" + excelRow, "N" + (excelRow + dtKvar.Rows.Count)];
                    excells1.NumberFormat = "0";

                    excelL.ExlWs.Cells[excelRow, 1] = "ВСЕГО КВАРТИР";
                    for (int j = 1; j < 14; j++) excelL.ExlWs.Cells[excelRow, j + 1] = itoMas[j];
                    excelRow++;
                    for (int i = 0; i < dtKvar.Rows.Count; i++)
                    {
                        if (dtKvar.Rows[i][0].ToString() == "4")
                            excelL.ExlWs.Cells[excelRow, 1] = "в т.ч." + dtKvar.Rows[i][0].ToString().Trim() + "-комнатных и более ";
                        else
                            excelL.ExlWs.Cells[excelRow, 1] = "в т.ч." + dtKvar.Rows[i][0].ToString().Trim() + "-комнатных";
                        for (int j = 1; j < 14; j++) excelL.ExlWs.Cells[excelRow, j + 1] = Decimal.Parse(dtKvar.Rows[i][j].ToString().Trim());
                        excelRow++;
                    }

                    #endregion


                    #region Заполнение Итого квартир по году
                    excelRow += 3;
                    excelL.ExlWs.Cells[excelRow, 1] = "Наличие площади на начало года - всего, кв.м :";
                    excelL.ExlWs.Cells[excelRow + 1, 1] = "Число жилых строений - всего :";
                    excelL.ExlWs.Cells[excelRow, 3] = plKvarYear;
                    excelL.ExlWs.Cells[excelRow + 1, 3] = countDom;
                    excells1 = excelL.ExlWs.Range["C" + (excelRow + 1), "C" + (excelRow + 1)];
                    excells1.NumberFormat = "0";

                    #endregion

                    SetReportSing(excelL, excelRow + 3, false);

                    for (int c = excelRow + 3; c < excelRow + 3 + 10; c++)
                    {
                        excells1 = excelL.ExlWs.Range["A" + c, "H" + c];
                        excells1.Font.Size = 5;
                    }


                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(Global.Constants.ExcelDir, "SostGilFond" +
                            prm.year_ + "_" +
                            prm.month_ + "_" + nzpUser) + ".xls";
                        ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName, ref excelL.ExlWb, ref excelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }

                    #endregion

                }

                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSostGilFond DataTable : ОШИБКА!";

                    fileName = GetFileName(Global.Constants.ExcelDir, "SostGilFond" +
                                                                      prm.year_ + "_" +
                                                                      prm.month_
                                                                      + "_" + DateTime.Now.GetHashCode() + nzpUser) + ".xls";
                    ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName, ref excelL.ExlWb, ref excelL.ExlApp);
                    excelL.ExlWb.Close(false, Type.Missing, Type.Missing);
                    excelL.ExlWb = null;
                    if (InputOutput.useFtp)
                    {
                        fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                    }

                    //удаление объекта
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                try
                {
                    if (excelL != null)
                    {
                        fileName = GetFileName(Global.Constants.ExcelDir, "SostGilFond" +
                            prm.year_ + "_" +
                            prm.month_
                            + "_" + nzpUser) + ".xls";
                        ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName, ref excelL.ExlWb, ref excelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет GetSostGilFond", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSostGilFond Excel File : ОШИБКА!";
                }

            }
            finally
            {
                //удаление объекта
                if (excelL != null)
                {
                    excelL.DeleteObject();
                }
            }

            return ret;

        }

        public Returns GetInfPoRaschetNasel(Ls finder, int nzp_user, int nzp_supp, int month, int year, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            ExcelLoader ExcelL = null;
            System.Data.DataTable dt = null;

            #region Получение DataTable
            ExcelRep ER = new ExcelRep();
            dt = ER.GetInfPoRaschetNasel(finder, nzp_supp, month, year);
            ER.Close();

            if (dt == null)
            {
                MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                ret.result = false;
                ret.text = "Создание DataTable : ОШИБКА!";
                return ret;
            }

            //модификация DataTable
            try
            {
                if (dt.Rows.Count > 0)
                {
                    decimal sum_SaldoIn = 0;
                    decimal sum_Nachkilovat = 0;
                    decimal sum_NachRub = 0;
                    decimal sum_PostupOplat = 0;
                    decimal sum_SaldoOut = 0;

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (dt.Rows[i]["Saldo_in"] != DBNull.Value)
                        {
                            sum_SaldoIn += Convert.ToDecimal(dt.Rows[i]["Saldo_in"]);
                        }
                        if (dt.Rows[i]["nach_kilovat"] != DBNull.Value)
                        {
                            sum_Nachkilovat += Convert.ToDecimal(dt.Rows[i]["nach_kilovat"]);
                        }
                        if (dt.Rows[i]["nach_rub"] != DBNull.Value)
                        {
                            sum_NachRub += Convert.ToDecimal(dt.Rows[i]["nach_rub"]);
                        }
                        if (dt.Rows[i]["sumOplat"] != DBNull.Value)
                        {
                            sum_PostupOplat += Convert.ToDecimal(dt.Rows[i]["sumOplat"]);
                        }
                        if (dt.Rows[i]["Saldo_out"] != DBNull.Value)
                        {
                            sum_SaldoOut += Convert.ToDecimal(dt.Rows[i]["Saldo_out"]);
                        }



                        int typek = -1;
                        if (dt.Rows[i]["typek"] != DBNull.Value)
                        {
                            typek = Convert.ToInt16(dt.Rows[i]["typek"]);
                        }
                        switch (typek)
                        {
                            case 1:
                                {
                                    if (dt.Rows[i]["area"] != DBNull.Value)
                                    {
                                        dt.Rows[i]["area"] = dt.Rows[i]["area"].ToString().Trim() + "\n(население)";
                                    }
                                    break;
                                }
                            case 3:
                                {
                                    if (dt.Rows[i]["area"] != DBNull.Value)
                                    {
                                        dt.Rows[i]["area"] = dt.Rows[i]["area"].ToString().Trim() + "\n(юр.лица)";
                                    }
                                    break;
                                }
                        }
                    }

                    //Добавление Всего
                    dt.Rows.Add("Всего", 0, sum_SaldoIn, sum_Nachkilovat, sum_NachRub, sum_PostupOplat, sum_SaldoOut, null);


                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка модификации DataTable (население, юр. лица): " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            #endregion

            #region Загрузка шаблона
            //создание объекта Excel
            ExcelL = new ExcelLoader();

            Dictionary<string, string> dicData = new Dictionary<string, string>();
            string strMonthName = "";

            #region Получаем название месяца

            switch (Convert.ToInt32(month))
            {
                case 1:
                    strMonthName = "ЯНВАРЬ";
                    break;
                case 2:
                    strMonthName = "ФЕВРАЛЬ";
                    break;
                case 3:
                    strMonthName = "МАРТ";
                    break;
                case 4:
                    strMonthName = "АПРЕЛЬ";
                    break;
                case 5:
                    strMonthName = "МАЙ";
                    break;
                case 6:
                    strMonthName = "ИЮНЬ";
                    break;
                case 7:
                    strMonthName = "ИЮЛЬ";
                    break;
                case 8:
                    strMonthName = "АВГУСТ";
                    break;
                case 9:
                    strMonthName = "СЕНТЯБРЬ";
                    break;
                case 10:
                    strMonthName = "ОКТЯБРЬ";
                    break;
                case 11:
                    strMonthName = "НОЯБРЬ";
                    break;
                case 12:
                    strMonthName = "ДЕКАБРЬ";
                    break;
                default:
                    break;
            }
            #endregion
            dicData.Add("month", strMonthName);
            dicData.Add("year", year.ToString());

            ret = ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("InfPoRaschet_template.xls"), dicData);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка загрузки шаблона InfPoRaschet_template :" + ret.text, MonitorLog.typelog.Error, true);
                return ret;
            }
            #endregion

            try
            {
                //#region Устанавливаем разделитель '.'
                //System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                //culture.NumberFormat.NumberDecimalSeparator = ".";
                //culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                //System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                //System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                //#endregion

                #region Создание Excel документа

                //создаем массив форматов
                string[] TypeHeader = new string[] { "char", "char", "float", "float",
                                                 "float", "float", "float", "float"};
                ////футер
                //string[] footer_data = null;

                #region Вставка данных
                Microsoft.Office.Interop.Excel.Range NaimUk = ExcelL.ExlWs.get_Range("A10", Type.Missing);
                Microsoft.Office.Interop.Excel.Range SaldoIn = ExcelL.ExlWs.get_Range("E10", Type.Missing);
                Microsoft.Office.Interop.Excel.Range NachKilovat = ExcelL.ExlWs.get_Range("G10", Type.Missing);
                Microsoft.Office.Interop.Excel.Range NachRub = ExcelL.ExlWs.get_Range("H10", Type.Missing);
                Microsoft.Office.Interop.Excel.Range PostupOpl = ExcelL.ExlWs.get_Range("I10", Type.Missing);
                Microsoft.Office.Interop.Excel.Range SaldoOut = ExcelL.ExlWs.get_Range("K10", Type.Missing);
                Microsoft.Office.Interop.Excel.Range Procent = ExcelL.ExlWs.get_Range("M10", Type.Missing);

                //с какой строки вставляем данные
                int r = 10;

                for (int i = 0; i < dt.Rows.Count; i++)
                {

                    //Выборка диапазона
                    NaimUk = ExcelL.ExlWs.get_Range("A" + r, "C" + r);
                    SaldoIn = ExcelL.ExlWs.get_Range("E" + r, "F" + r);
                    NachKilovat = ExcelL.ExlWs.get_Range("G" + r, Type.Missing);
                    NachRub = ExcelL.ExlWs.get_Range("H" + r, Type.Missing);
                    PostupOpl = ExcelL.ExlWs.get_Range("I" + r, "J" + r);
                    SaldoOut = ExcelL.ExlWs.get_Range("K" + r, "L" + r);
                    Procent = ExcelL.ExlWs.get_Range("M" + r, "O" + r);

                    //Объединение ячеек
                    NaimUk.Merge(Type.Missing);
                    SaldoIn.Merge(Type.Missing);
                    NachKilovat.Merge(Type.Missing);
                    NachRub.Merge(Type.Missing);
                    PostupOpl.Merge(Type.Missing);
                    SaldoOut.Merge(Type.Missing);
                    Procent.Merge(Type.Missing);

                    //рамка на всю строку
                    Microsoft.Office.Interop.Excel.Range Temp_Range = ExcelL.ExlWs.get_Range("A" + r, "O" + r);
                    Temp_Range.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                    //Ширина строки                    
                    Temp_Range.RowHeight = 37.5;

                    //общее выравнивание строки
                    Temp_Range.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    //шрифт 10
                    Temp_Range.Font.Size = 10;

                    //Жирная строка Итого
                    if (i == dt.Rows.Count - 1)
                    {
                        Temp_Range.Font.Bold = true;
                    }

                    //Выравнивание чисел
                    Temp_Range = ExcelL.ExlWs.get_Range("E" + r, "O" + r);
                    Temp_Range.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;





                    //вставка данных
                    NaimUk.Value2 = dt.Rows[i]["area"].ToString().Trim();
                    SaldoIn.Value2 = dt.Rows[i]["Saldo_in"];
                    NachKilovat.Value2 = dt.Rows[i]["nach_kilovat"];
                    NachRub.Value2 = dt.Rows[i]["nach_rub"];
                    PostupOpl.Value2 = dt.Rows[i]["sumOplat"];
                    SaldoOut.Value2 = dt.Rows[i]["Saldo_out"];
                    Procent.Value2 = dt.Rows[i]["Proc"];

                    r++;

                }

                ////Выравнивание по содержимому
                //int s_row = 1;
                //r = 10;

                //NaimUk = ExcelL.ExlWs.get_Range("A" + s_row, "C" + (r + dt.Rows.Count -1));
                //SaldoIn = ExcelL.ExlWs.get_Range("E" + s_row, "F" + (r + dt.Rows.Count));
                //NachKilovat = ExcelL.ExlWs.get_Range("G" + s_row, "G" + (r + dt.Rows.Count));
                //NachRub = ExcelL.ExlWs.get_Range("H" + s_row, "H" + (r + dt.Rows.Count));
                //PostupOpl = ExcelL.ExlWs.get_Range("I" + s_row, "J" + (r + dt.Rows.Count));
                //SaldoOut = ExcelL.ExlWs.get_Range("K" + s_row, "L" + (r + dt.Rows.Count));
                //Procent = ExcelL.ExlWs.get_Range("M" + s_row, "O" + (r + dt.Rows.Count));

                //NaimUk.AutoFit();
                //SaldoIn.AutoFit();
                //NachKilovat.AutoFit();
                //NachRub.AutoFit();
                //PostupOpl.AutoFit();
                //SaldoOut.AutoFit();
                //Procent.AutoFit();



                #endregion

                #region Надпись "Нет данных" если данных нет)
                ExcelCreater ExcelCr = new ExcelCreater();
                if (dt.Rows.Count == 0)
                {
                    ExcelCr.MakeName("Нет данных", "A10", 14, 9, ref ExcelL.ExlWs);
                }
                #endregion


                if (ret.result)
                {
                    //имя файла                                                         
                    fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "InfPoRachet" + "_" + nzp_user) + ".xls";

                    //Сохранение
                    ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                    if (InputOutput.useFtp)
                    {
                        fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                    }
                }
                else
                {
                    MonitorLog.WriteLog("Ошибка создания Excel файла : " + ret.text, MonitorLog.typelog.Error, true);
                }




                #endregion
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Формирование Excel ошибка :" + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;
            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }


            return ret;
        }



        public Returns GetCalcTarif(Prm prm, string Nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();

            ExcelLoader ExcelL = null;

            try
            {
                ExcelL = new ExcelLoader();
                ExcelL.ExlWs.Rows.Font.Name = "Arial";
                //создание dataTable
                ExcelRep ExR = new ExcelRep();
                System.Data.DataTable DT = ExR.GetCalcTarif(prm, out ret, Nzp_user);

                if (DT != null)
                {

                    //ExcelCreater ExcelCr = new ExcelCreater();

                    #region Создаем шапку

                    Microsoft.Office.Interop.Excel.Range excells1;

                    //спустили шапку
                    excells1 = ExcelL.ExlWs.get_Range("A1", "G1");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Карта аналитического учета по типу услуги ''содеpж.жилья''";
                    excells1.Font.Bold = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excells1 = ExcelL.ExlWs.get_Range("A2", "G2");
                    excells1.Merge(Type.Missing);
                    excells1.Font.Bold = true;
                    //string ERC_name = "";

                    //if (STCLINE.KP50.Interfaces.Points.IsDemo == true)
                    //    ERC_name = "Н-ска";
                    //else
                    //    ERC_name = "Самары";
                    string s_name = SetReportHeader();
                    //excells1.FormulaR1C1 = "по МП городского округа " + ERC_name + " ''ЕИРЦ'' за " +
                    excells1.FormulaR1C1 = s_name + " за " +
                        prm.month_.ToString("00") + " " + prm.year_.ToString() + "г.";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excells1 = ExcelL.ExlWs.get_Range("A3", "A3");
                    excells1.Merge(Type.Missing);
                    excells1.Font.Bold = true;
                    excells1.FormulaR1C1 = System.DateTime.Today.ToShortDateString();
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                    excells1 = ExcelL.ExlWs.get_Range("B3", "G4");
                    excells1.Font.Bold = true;
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = SetReportConditional(System.Convert.ToInt32(Nzp_user));
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.RowHeight = 23;

                    excells1 = ExcelL.ExlWs.get_Range("A5", "A6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Название статьи:";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.ColumnWidth = 32;

                    excells1 = ExcelL.ExlWs.get_Range("B5", "E5");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Начислено";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                    excells1 = ExcelL.ExlWs.get_Range("B6", "B6");
                    excells1.FormulaR1C1 = "постоянно";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                    excells1 = ExcelL.ExlWs.get_Range("C6", "C6");
                    excells1.FormulaR1C1 = "возврат";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                    excells1 = ExcelL.ExlWs.get_Range("D6", "D6");
                    excells1.FormulaR1C1 = "красные";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;


                    excells1 = ExcelL.ExlWs.get_Range("E6", "E6");
                    excells1.FormulaR1C1 = "черные";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;


                    excells1 = ExcelL.ExlWs.get_Range("F5", "F6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "к оплате";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                    excells1 = ExcelL.ExlWs.get_Range("G5", "G6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "оплачено";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                    excells1 = ExcelL.ExlWs.get_Range("A7", "G7");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Калькуляция вида услуги 'содеpж.жилья'";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;


                    #endregion

                    #region Пишем тело

                    decimal[] sum_colls = new decimal[DT.Columns.Count];
                    decimal[] sum_delta = new decimal[DT.Columns.Count];
                    decimal[] sum_last_row = new decimal[DT.Columns.Count];

                    for (int i = 0; i < DT.Columns.Count; i++)
                    {
                        sum_colls[i] = 0;
                        sum_delta[i] = 0;
                        sum_last_row[i] = 0;
                    }

                    Decimal dl;
                    bool b;
                    int ExcelRow = 0;
                    for (int i = 0; i < DT.Rows.Count; i++)
                    {
                        if (System.Convert.ToInt32(DT.Rows[i][0].ToString()) > 1000) break;
                        for (int j = 1; j < DT.Columns.Count; j++)
                        {
                            b = Decimal.TryParse(DT.Rows[i][j].ToString(), out dl);
                            if (b)
                            {
                                ExcelL.ExlWs.Cells[i + 8, j] = System.Math.Round(dl, 2);
                                sum_last_row[j] = System.Math.Round(dl, 2);
                                sum_colls[j] = sum_colls[j] + dl;
                                sum_delta[j] = sum_delta[j] + System.Math.Round(dl, 2);
                            }
                            else
                                ExcelL.ExlWs.Cells[i + 8, j] = DT.Rows[i][j].ToString().Trim();

                        }
                        ExcelRow++;
                    }

                    #endregion

                    #region Пишем подвал1

                    int FooterRow = 8 + ExcelRow;


                    ExcelL.ExlWs.Cells[FooterRow, 1] = "";

                    for (int i = 2; i < DT.Columns.Count; i++)
                    {
                        ExcelL.ExlWs.Cells[FooterRow, i] = sum_colls[i];
                    }

                    for (int i = 2; i < DT.Columns.Count; i++)
                    {
                        if (sum_delta[i] != 0)
                            ExcelL.ExlWs.Cells[FooterRow - 1, i] = sum_last_row[i] + sum_colls[i] - sum_delta[i];
                    }


                    excells1 = ExcelL.ExlWs.get_Range("B" + (FooterRow).ToString(), "G" + (FooterRow).ToString());
                    excells1.EntireColumn.NumberFormat = "# ##0,00";
                    excells1.Font.Bold = true;
                    #endregion

                    #region Пишем подвал2
                    ExcelRow++;
                    for (int i = ExcelRow; i < DT.Rows.Count + 1; i++)
                    {
                        for (int j = 1; j < DT.Columns.Count; j++)
                        {
                            b = Decimal.TryParse(DT.Rows[i - 1][j].ToString(), out dl);
                            if (b)
                            {
                                ExcelL.ExlWs.Cells[i + 8, j] = System.Math.Round(dl, 2);
                                sum_last_row[j] = System.Math.Round(dl, 2);
                                sum_colls[j] = sum_colls[j] + dl;
                            }
                            else
                                ExcelL.ExlWs.Cells[i + 8, j] = DT.Rows[i - 1][j].ToString().Trim();

                        }
                        ExcelRow++;
                    }
                    FooterRow = ExcelRow + 8;
                    ExcelL.ExlWs.Cells[FooterRow, 1] = "Всего:";
                    for (int i = 2; i < DT.Columns.Count; i++)
                    {
                        ExcelL.ExlWs.Cells[FooterRow, i] = sum_colls[i];
                    }


                    string[] colarray = new string[] { "A", "B", "C", "D", "E", "F", "G" };
                    foreach (string ch in colarray)
                    {
                        excells1 = ExcelL.ExlWs.get_Range(ch + (FooterRow).ToString(), ch + (FooterRow).ToString());
                        if (ch != "A")
                        {
                            excells1.EntireColumn.NumberFormat = "# ##0,00";
                        }
                        excells1.Font.Bold = true;
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excells1.EntireColumn.Font.Size = 10;
                        if (ch != "A")
                            excells1.EntireColumn.AutoFit();

                        excells1 = ExcelL.ExlWs.get_Range(ch + "7", ch + (FooterRow).ToString());
                        excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    }

                    ExcelL.ExlWs.PageSetup.Orientation = XlPageOrientation.xlLandscape;


                    #endregion

                    SetReportSing(ExcelL, FooterRow + 3, false);
                    //ExcelL.ExlWs.Cells[ExcelRow + 3, 2] = "Директор";
                    //ExcelL.ExlWs.Cells[ExcelRow + 5, 2] = "Начальник ПЭО";
                    //ExcelL.ExlWs.Cells[ExcelRow + 7, 2] = "Начальник ОНУПН";
                    //ExcelL.ExlWs.Cells[ExcelRow + 3, 3] = " Мякишев М.В.";
                    //ExcelL.ExlWs.Cells[ExcelRow + 5, 3] = " Соковых И.А. ";
                    //ExcelL.ExlWs.Cells[ExcelRow + 7, 3] = " Старкова Л.А.";

                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "CalcTarif_" +
                            prm.year_.ToString() + "_" +
                            prm.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }
                    finally
                    {
                        ////удаление объекта
                        //ExcelL.DeleteObject();
                    }
                    #endregion

                }
                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetAnalisKartTable DataTable : ОШИБКА!";
                    ////удаление объекта
                    //ExcelL.DeleteObject();
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }

            return ret;

        }


        public Returns GetProtCalcOdn2(Prm prm_, string Nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.ProtCalcOdn2(prm_, out ret, Nzp_user);

            ExcelLoader ExcelL = null;
            try
            {


                if (DT != null)
                {
                    ExcelL = new ExcelLoader();
                    ExcelCreater ExcelCr = new ExcelCreater();


                    #region Создаем шапку
                    Microsoft.Office.Interop.Excel.Range excells1;

                    Dictionary<string, string> Dic = new Dictionary<string, string>();
                    Dic.Add("ERCName", SetReportHeader());

                    Dic.Add("month", Utils.GetMonthName(prm_.month_));//месяц
                    Dic.Add("year", prm_.year_.ToString());//год
                    Dic.Add("DATE", DateTime.Now.ToShortDateString());//год


                    ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("prot_calc_odn2.xls"), Dic);
                    ExcelFormater exform = new ExcelFormater();
                    excells1 = ExcelL.ExlWs.get_Range("D3", "H4");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = SetReportConditional(System.Convert.ToInt32(Nzp_user));
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.Font.Size = 8;
                    excells1.RowHeight = 23;
                    #endregion

                    #region Пишем тело


                    int ExcelRow = 9;
                    int lastExcelRow = 9;
                    int i = 0;
                    string old_adres = "";
                    string oldDom = "";
                    string dom = "";
                    string oldUlica = "";
                    string ulica = "";

                    string adres = "";

                    while (i < DT.Rows.Count)
                    {
                        if (old_adres == "")
                        {
                            old_adres = DT.Rows[i]["ulica"].ToString().Trim() + "," +
                            DT.Rows[i]["ndom"].ToString().Trim() + " " +
                            DT.Rows[i]["nkor"].ToString().Trim();
                            oldDom = DT.Rows[i]["ndom"].ToString().Trim() + " " +
                            DT.Rows[i]["nkor"].ToString().Trim().Replace("-", "");
                            oldUlica = DT.Rows[i]["ulica"].ToString().Trim();

                        }
                        adres = DT.Rows[i]["ulica"].ToString().Trim() + "," +
                            DT.Rows[i]["ndom"].ToString().Trim() + " " +
                            DT.Rows[i]["nkor"].ToString().Trim();
                        ulica = DT.Rows[i]["ulica"].ToString().Trim();
                        dom = DT.Rows[i]["ndom"].ToString().Trim() + " " +
                            DT.Rows[i]["nkor"].ToString().Trim().Replace("-", "");
                        if (old_adres != adres)
                        {
                            excells1 = ExcelL.ExlWs.get_Range("A" + lastExcelRow.ToString(), "A" + (ExcelRow - 1).ToString());
                            excells1.Merge(Type.Missing);
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                            excells1.WrapText = true;

                            excells1 = ExcelL.ExlWs.get_Range("B" + lastExcelRow.ToString(), "B" + (ExcelRow - 1).ToString());
                            excells1.Merge(Type.Missing);
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                            excells1.WrapText = true;

                            excells1 = ExcelL.ExlWs.get_Range("C" + lastExcelRow.ToString(), "C" + (ExcelRow - 1).ToString());
                            excells1.Merge(Type.Missing);
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                            excells1.WrapText = true;

                            excells1 = ExcelL.ExlWs.get_Range("D" + lastExcelRow.ToString(), "D" + (ExcelRow - 1).ToString());
                            excells1.Merge(Type.Missing);
                            excells1.WrapText = true;
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                            lastExcelRow = ExcelRow;
                            old_adres = adres;
                            oldDom = dom;
                            oldUlica = ulica;
                        }

                        ExcelL.ExlWs.Cells[ExcelRow, 1] = DT.Rows[i]["geu"].ToString().Trim().Replace("ЖЭУ", "");
                        ExcelL.ExlWs.Cells[ExcelRow, 2] = DT.Rows[i]["area"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 3] = ulica;
                        ExcelL.ExlWs.Cells[ExcelRow, 4] = dom;
                        ExcelL.ExlWs.Cells[ExcelRow, 5] = DT.Rows[i]["litera"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 6] = "01." + prm_.month_.ToString() + "." + prm_.year_.ToString();

                        if (DT.Rows[i]["date_calc"] != DBNull.Value)
                        {
                            ExcelL.ExlWs.Cells[ExcelRow, 10] = ((DateTime)DT.Rows[i]["date_calc"]).ToShortDateString();
                            ExcelL.ExlWs.Cells[ExcelRow, 15] = ((DateTime)DT.Rows[i]["date_calc"]).ToShortDateString();
                            ExcelL.ExlWs.Cells[ExcelRow, 20] = ((DateTime)DT.Rows[i]["date_calc"]).ToShortDateString();
                            ExcelL.ExlWs.Cells[ExcelRow, 25] = ((DateTime)DT.Rows[i]["date_calc"]).ToShortDateString();
                        }

                        decimal kf_hv = Decimal.Parse(DT.Rows[i]["kf_hv"].ToString().Trim());
                        decimal kf_hv_m = Decimal.Parse(DT.Rows[i]["kf_hv_m"].ToString().Trim());
                        decimal kf_hv_h = Decimal.Parse(DT.Rows[i]["kf_hv_h"].ToString().Trim());
                        decimal kf_gv = Decimal.Parse(DT.Rows[i]["kf_gv"].ToString().Trim());
                        decimal kf_gv_m = Decimal.Parse(DT.Rows[i]["kf_gv_m"].ToString().Trim());
                        decimal kf_gv_h = Decimal.Parse(DT.Rows[i]["kf_gv_h"].ToString().Trim());
                        decimal kf_el = Decimal.Parse(DT.Rows[i]["kf_el"].ToString().Trim());
                        decimal kf_el_m = Decimal.Parse(DT.Rows[i]["kf_el_m"].ToString().Trim());
                        decimal kf_el_h = Decimal.Parse(DT.Rows[i]["kf_el_h"].ToString().Trim());


                        decimal kf_gaz_m = Decimal.Parse(DT.Rows[i]["kf_gaz_m"].ToString().Trim());
                        decimal kf_gaz_h = Decimal.Parse(DT.Rows[i]["kf_gaz_h"].ToString().Trim());

                        if ((kf_hv_m == 0) & (kf_hv_h == 0))
                        {
                            kf_hv = 0;
                            ExcelL.ExlWs.Cells[ExcelRow, 10] = "";
                        }
                        if ((kf_gv_m == 0) & (kf_gv_h == 0))
                        {
                            kf_gv = 0;
                            ExcelL.ExlWs.Cells[ExcelRow, 15] = "";
                        }
                        if ((kf_el_m == 0) & (kf_el_h == 0))
                        {
                            kf_el = 0;
                            ExcelL.ExlWs.Cells[ExcelRow, 20] = "";
                        }
                        if ((kf_gaz_m == 0) & (kf_gaz_h == 0))
                        {

                            ExcelL.ExlWs.Cells[ExcelRow, 25] = "";
                        }


                        if (kf_hv > 0.00001m) ExcelL.ExlWs.Cells[ExcelRow, 7] = kf_hv;
                        else
                            ExcelL.ExlWs.Cells[ExcelRow, 7] = "-";


                        if (kf_hv_m > 0.00001m) ExcelL.ExlWs.Cells[ExcelRow, 8] = kf_hv_m;
                        else ExcelL.ExlWs.Cells[ExcelRow, 8] = "-";

                        if (System.Math.Abs(kf_hv_h) > 0.00001m) ExcelL.ExlWs.Cells[ExcelRow, 9] = kf_hv_h;
                        else ExcelL.ExlWs.Cells[ExcelRow, 9] = "-";

                        if (kf_hv < kf_hv_m) ExcelL.ExlWs.Cells[ExcelRow, 11] = "+";



                        if (kf_gv > 0.00001m) ExcelL.ExlWs.Cells[ExcelRow, 12] = kf_gv;
                        else ExcelL.ExlWs.Cells[ExcelRow, 12] = "-";

                        if (kf_gv_m > 0.00001m) ExcelL.ExlWs.Cells[ExcelRow, 13] = kf_gv_m;
                        else ExcelL.ExlWs.Cells[ExcelRow, 13] = "-";

                        if (System.Math.Abs(kf_gv_h) > 0.00001m) ExcelL.ExlWs.Cells[ExcelRow, 14] = kf_gv_h;
                        else ExcelL.ExlWs.Cells[ExcelRow, 14] = "-";


                        if (kf_hv < kf_hv_m) ExcelL.ExlWs.Cells[ExcelRow, 16] = "+";

                        if (kf_el > 0.00001m) ExcelL.ExlWs.Cells[ExcelRow, 17] = kf_el;
                        else ExcelL.ExlWs.Cells[ExcelRow, 17] = "-";

                        if (kf_el_m > 0.00001m) ExcelL.ExlWs.Cells[ExcelRow, 18] = kf_el_m;
                        else ExcelL.ExlWs.Cells[ExcelRow, 18] = "-";

                        if (System.Math.Abs(kf_el_h) > 0.00001m) ExcelL.ExlWs.Cells[ExcelRow, 19] = kf_el_h;
                        else ExcelL.ExlWs.Cells[ExcelRow, 19] = "-";


                        if (kf_el < kf_el_m) ExcelL.ExlWs.Cells[ExcelRow, 21] = "+";

                        ExcelL.ExlWs.Cells[ExcelRow, 22] = "-";

                        if (System.Math.Abs(Decimal.Parse(DT.Rows[i]["kf_gaz_m"].ToString().Trim())) > 0.00001m)
                        {

                            ExcelL.ExlWs.Cells[ExcelRow, 23] = Decimal.Parse(DT.Rows[i]["kf_gaz_m"].ToString().Trim());
                        }
                        else
                        {
                            ExcelL.ExlWs.Cells[ExcelRow, 23] = "-";
                        }

                        if (System.Math.Abs(Decimal.Parse(DT.Rows[i]["kf_gaz_h"].ToString().Trim())) > 0.00001m)
                        {
                            ExcelL.ExlWs.Cells[ExcelRow, 24] = Decimal.Parse(DT.Rows[i]["kf_gaz_h"].ToString().Trim());
                        }
                        else
                        {
                            ExcelL.ExlWs.Cells[ExcelRow, 24] = "-";
                        }
                        i++;
                        ExcelRow++;

                    }



                    #endregion


                    #region форматируем
                    excells1 = ExcelL.ExlWs.get_Range("A9", "Z" + (ExcelRow - 1).ToString());
                    excells1.Font.Size = 5;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                    excells1 = ExcelL.ExlWs.get_Range("G9", "I" + (ExcelRow - 1).ToString());
                    excells1.EntireColumn.NumberFormat = "# ##0,0000";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;

                    excells1 = ExcelL.ExlWs.get_Range("L9", "N" + (ExcelRow - 1).ToString());
                    excells1.EntireColumn.NumberFormat = "# ##0,0000";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;

                    excells1 = ExcelL.ExlWs.get_Range("Q9", "S" + (ExcelRow - 1).ToString());
                    excells1.EntireColumn.NumberFormat = "# ##0,0000";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;

                    excells1 = ExcelL.ExlWs.get_Range("V9", "X" + (ExcelRow - 1).ToString());
                    excells1.EntireColumn.NumberFormat = "# ##0,0000";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;

                    #endregion


                    SetReportSing(ExcelL, ExcelRow + 3, false);


                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "ProtCalcOdn2" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString() + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }

                    #endregion

                }

                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSoderg DataTable : ОШИБКА!";

                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "ProtCalcOdn" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }

                    //удаление объекта
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                try
                {
                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "ProtCalcOdn" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет ProtCalcOdn", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSoderg Excel File : ОШИБКА!";
                }

            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }

            return ret;

        }


        public Returns GetDomNach(List<Prm> listprm, string nzpUser, ref string fileName)
        {
            Returns ret;

            ExcelLoader excelL = null;

            //создание dataTable
            var exR = new ExcelRep();
            System.Data.DataTable dt = exR.GetDomNachReport(listprm, out ret, nzpUser);

            try
            {
                excelL = new ExcelLoader();
                excelL.ExlWs.Rows.Font.Name = "Arial";

                if (dt != null)
                {

                    //ExcelCreater ExcelCr = new ExcelCreater();


                    #region Создаем шапку
                    dt.Columns.Remove("nzp_serv");
                    dt.Columns.Remove("idom");

                    //спустили шапку
                    Range excells1 = excelL.ExlWs.Range["A1", "I1"];
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "РАСШИФРОВКА ПО ДОМАМ - НАЧИСЛЕНО";
                    excells1.Font.Bold = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excells1 = excelL.ExlWs.Range["A2", "I2"];
                    excells1.Merge(Type.Missing);
                    excells1.Font.Bold = true;
                    //string ERC_name = "";

                    //if (STCLINE.KP50.Interfaces.Points.IsDemo == true)
                    //    ERC_name = "Н-ска";
                    //else
                    //    ERC_name = "Самары";

                    //excells1.FormulaR1C1 = "по МП городского округа " + ERC_name + " ''ЕИРЦ'' за " +

                    string sName = SetReportHeader();
                    excells1.FormulaR1C1 = sName + " за " +
                        listprm[0].month_.ToString("00") + " " + listprm[0].year_ + "г.";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excells1 = excelL.ExlWs.Range["B3", "B3"];
                    excells1.Merge(Type.Missing);
                    excells1.Font.Bold = true;
                    excells1.FormulaR1C1 = DateTime.Today.ToShortDateString();
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                    excells1 = excelL.ExlWs.Range["C3", "K4"];
                    excells1.Font.Bold = true;
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = SetReportConditional(Convert.ToInt32(nzpUser)) + "\n" + (listprm[0].isLoadParamInfo ? "С учетом кап.ремонта" : "Без учета кап.ремонта"); 
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.RowHeight = 25;

                    excells1 = excelL.ExlWs.Range["A5", "A6"];
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "№ п/п";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                    excells1.RowHeight = 23;

                    excells1 = excelL.ExlWs.Range["B5", "B6"];
                    excells1.Merge(Type.Missing);
                    //excells1.FormulaR1C1 = "Сальдо 01." + listprm[0].month_.ToString("00") + "." + listprm[0].year_.ToString() + " г.";
                    excells1.FormulaR1C1 = "Адрес";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = XlLineStyle.xlContinuous;


                    excells1 = excelL.ExlWs.Range["C5", "C6"];
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Оплачив." + Convert.ToChar(10) + " площ. " + Convert.ToChar(10) + " кв.м.";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                    excells1 = excelL.ExlWs.Range["D5", "D6"];
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Кол-во " + Convert.ToChar(10) + " прож-х";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                    excells1 = excelL.ExlWs.Range["E5", "E6"];
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Кол-во " + Convert.ToChar(10) + " ЛС";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                    excells1 = excelL.ExlWs.Range["F5", "F6"];
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = " ";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = XlLineStyle.xlContinuous;


                    excells1 = excelL.ExlWs.Range["G5", "G6"];
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Сумма всего";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                    //excells1 = excelL.ExlWs.Range["H5", "I5"];
                    //excells1.Merge(Type.Missing);
                    //excells1.FormulaR1C1 = "В том числе по видам услуг";
                    //excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    //excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                    excells1 = excelL.ExlWs.Range["A5", "I6"];
                    excells1.Font.Size = 8;

                    #endregion

                    if (dt.Rows.Count > 0)
                    {


                        #region Преподготовка

                        string oldDom = dt.Rows[0]["nzp_dom"].ToString();
                        int servCount = 0;
                        var exform = new ExcelFormater();
                        //       string str_source;
                        //      string [] str_temp;

                        while ((servCount < dt.Rows.Count) & (dt.Rows[servCount]["nzp_dom"].ToString() == oldDom))
                        {
                            excells1 = excelL.ExlWs.Range[exform.BukvaList[servCount + 7] + "6", exform.BukvaList[servCount + 7] + "6"];
                            /*        str_source = DT.Rows[serv_count]["service"].ToString().Trim();
                                    if (str_source.Length > 10)
                                    {
                                        str_temp = str_source.Split(' ');
                                        str_source = "";
                                        int k = 0;
                                        foreach ( string s in str_temp)
                                        {
                                            str_source = str_source + " "+ s;
                                            if (k % 2 == 1) str_source = str_source + " "+System.Convert.ToChar(10);
                                            k++;
                                        }

                                    }
                                    excells1.FormulaR1C1 = str_source;*/
                            excells1.FormulaR1C1 = dt.Rows[servCount]["service"].ToString().Trim();
                            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                            excells1.Font.Size = 8;
                            excells1.ColumnWidth = 12;
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.WrapText = true;
                            servCount++;
                        }

                        excells1 = excelL.ExlWs.Range["H5" ,exform.BukvaList[servCount + 6] + "5"];
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = "В том числе по видам услуг";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                        excells1.Columns.AutoFit();

                        #endregion

                        #region Пишем тело

                        //  decimal[] sum_colls = new decimal[servCount];

                        int excelRow = 7;
                        //int i = 0;

                        #region Данные по домам
                        DataRow[] drSelect = dt.Select("order_=1");
                        int excelCol = 8;
                        decimal sumInsaldo = 0;
                        decimal sumCharge = 0;
                        decimal sumMoney = 0;
                        decimal sumOutsaldo = 0;
                        decimal sumInsaldoOdn = 0;
                        decimal sumChargeOdn = 0;
                        decimal sumMoneyOdn = 0;
                        decimal sumOutsaldoOdn = 0;
                        decimal realCharge = 0;

                        int numRow = 1;
                        oldDom = "-1";
                        foreach (DataRow dr in drSelect)
                        {

                            #region Итого по дому
                            if (oldDom != dr["nzp_dom"].ToString())
                            {

                                if (oldDom != "-1")
                                {
                                    excelL.ExlWs.Cells[excelRow, 7] = sumInsaldo;
                                    excelL.ExlWs.Cells[excelRow + 1, 7] = sumInsaldoOdn;
                                    excelL.ExlWs.Cells[excelRow + 2, 7] = sumCharge;
                                    excelL.ExlWs.Cells[excelRow + 3, 7] = sumChargeOdn;
                                    excelL.ExlWs.Cells[excelRow + 4, 7] = sumMoney;
                                    excelL.ExlWs.Cells[excelRow + 5, 7] = sumMoneyOdn;
                                    excelL.ExlWs.Cells[excelRow + 6, 7] = realCharge;
                                    excelL.ExlWs.Cells[excelRow + 7, 7] = sumOutsaldo;
                                    excelL.ExlWs.Cells[excelRow + 8, 7] = sumOutsaldoOdn;
                                    excelRow = excelRow + 9;
                                    numRow++;
                                }

                                oldDom = dr["nzp_dom"].ToString();

                                excelCol = 8;
                                sumInsaldo = 0;
                                sumCharge = 0;
                                sumMoney = 0;
                                sumOutsaldo = 0;
                                sumInsaldoOdn = 0;
                                sumChargeOdn = 0;
                                sumMoneyOdn = 0;
                                sumOutsaldoOdn = 0;
                                realCharge = 0;




                                //Номер п/п
                                excells1 = excelL.ExlWs.Range["A" + excelRow, "A" + (excelRow + 8)];
                                excells1.Merge(Type.Missing);
                                excells1.FormulaR1C1 = numRow;
                                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                                //Адрес дома
                                excells1 = excelL.ExlWs.Range["B" + excelRow, "B" + (excelRow + 8)];
                                excells1.Merge(Type.Missing);
                                excells1.FormulaR1C1 = dr["ulica"].ToString().Trim() + " " + dr["ndom"].ToString().Trim();
                                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                                //Площадь
                                excells1 = excelL.ExlWs.Range["C" + excelRow, "C" + (excelRow + 8)];
                                excells1.Merge(Type.Missing);
                                excells1.FormulaR1C1 = Decimal.Parse(dr["pl_kvar"].ToString());
                                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                                //Количество жильцов
                                excells1 = excelL.ExlWs.Range["D" + excelRow, "D" + (excelRow + 8)];
                                excells1.Merge(Type.Missing);
                                excells1.FormulaR1C1 = dr["count_gil"].ToString();
                                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                                //Количество ЛС
                                excells1 = excelL.ExlWs.Range["E" + excelRow, "E" + (excelRow + 8)];
                                excells1.Merge(Type.Missing);
                                excells1.FormulaR1C1 = dr["count_ls"].ToString();
                                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                                excelL.ExlWs.Cells[excelRow, 6] = "Сальдо 01." + listprm[0].month_.ToString("00") + "." + listprm[0].year_ + " г.";
                                excelL.ExlWs.Cells[excelRow + 1, 6] = "в т.ч. сальдо ОДН";
                                excelL.ExlWs.Cells[excelRow + 2, 6] = "Начислено";
                                excelL.ExlWs.Cells[excelRow + 3, 6] = "в т.ч. начислено ОДН";
                                excelL.ExlWs.Cells[excelRow + 4, 6] = "Оплачено";
                                excelL.ExlWs.Cells[excelRow + 5, 6] = "в т.ч. оплачено ОДН";
                                excelL.ExlWs.Cells[excelRow + 6, 6] = "Списано сальдо";
                                excelL.ExlWs.Cells[excelRow + 7, 6] = "Сальдо " + DateTime.DaysInMonth(Convert.ToInt32(listprm[0].year_),
                                        Convert.ToInt32(listprm[0].month_)) +
                                        "." + listprm[0].month_.ToString("00") + "." + listprm[0].year_ + " г.";
                                excelL.ExlWs.Cells[excelRow + 8, 6] = "в т.ч. сальдо ОДН";

                            }

                            #endregion

                            excelL.ExlWs.Cells[excelRow, excelCol] = dr["sum_insaldo"];
                            excelL.ExlWs.Cells[excelRow + 1, excelCol] = dr["sum_insaldo_odn"];
                            excelL.ExlWs.Cells[excelRow + 2, excelCol] = dr["sum_charge"];
                            excelL.ExlWs.Cells[excelRow + 3, excelCol] = dr["sum_charge_odn"];
                            excelL.ExlWs.Cells[excelRow + 4, excelCol] = dr["sum_money"];
                            excelL.ExlWs.Cells[excelRow + 5, excelCol] = dr["sum_money_odn"];
                            excelL.ExlWs.Cells[excelRow + 6, excelCol] = dr["real_charge"];
                            excelL.ExlWs.Cells[excelRow + 7, excelCol] = dr["sum_outsaldo"];
                            excelL.ExlWs.Cells[excelRow + 8, excelCol] = dr["sum_outsaldo_odn"];


                            sumInsaldo = sumInsaldo + Decimal.Parse(dr["sum_insaldo"].ToString());
                            sumCharge = sumCharge + Decimal.Parse(dr["sum_charge"].ToString());
                            sumMoney = sumMoney + Decimal.Parse(dr["sum_money"].ToString());
                            sumOutsaldo = sumOutsaldo + Decimal.Parse(dr["sum_outsaldo"].ToString());


                            sumInsaldoOdn = sumInsaldoOdn + Decimal.Parse(dr["sum_insaldo_odn"].ToString());
                            sumChargeOdn = sumChargeOdn + Decimal.Parse(dr["sum_charge_odn"].ToString());
                            sumMoneyOdn = sumMoneyOdn + Decimal.Parse(dr["sum_money_odn"].ToString());
                            sumOutsaldoOdn = sumOutsaldoOdn + Decimal.Parse(dr["sum_outsaldo_odn"].ToString());
                            realCharge = realCharge + Decimal.Parse(dr["real_charge"].ToString());

                            excelCol++;

                        }
                        #endregion

                        if (oldDom != "-1")
                        {
                            excelL.ExlWs.Cells[excelRow, 7] = sumInsaldo;
                            excelL.ExlWs.Cells[excelRow + 1, 7] = sumInsaldoOdn;
                            excelL.ExlWs.Cells[excelRow + 2, 7] = sumCharge;
                            excelL.ExlWs.Cells[excelRow + 3, 7] = sumChargeOdn;
                            excelL.ExlWs.Cells[excelRow + 4, 7] = sumMoney;
                            excelL.ExlWs.Cells[excelRow + 5, 7] = sumMoneyOdn;
                            excelL.ExlWs.Cells[excelRow + 6, 7] = realCharge;
                            excelL.ExlWs.Cells[excelRow + 7, 7] = sumOutsaldo;
                            excelL.ExlWs.Cells[excelRow + 8, 7] = sumOutsaldoOdn;
                            excelRow = excelRow + 9;
                        }

                        #region Итого
                        drSelect = dt.Select("order_=2");
                        excelCol = 8;
                        sumInsaldo = 0;
                        sumCharge = 0;
                        sumMoney = 0;
                        sumOutsaldo = 0;
                        sumInsaldoOdn = 0;
                        sumChargeOdn = 0;
                        sumMoneyOdn = 0;
                        sumOutsaldoOdn = 0;
                        realCharge = 0;


                        oldDom = "-1";
                        foreach (DataRow dr in drSelect)
                        {

                            #region Итого по дому
                            if (oldDom != dr["nzp_dom"].ToString())
                            {


                                oldDom = dr["nzp_dom"].ToString();




                                //Адрес дома
                                excells1 = excelL.ExlWs.Range["B" + excelRow, "B" + (excelRow + 8)];
                                excells1.Merge(Type.Missing);
                                excells1.FormulaR1C1 = dr["ulica"].ToString().Trim() + " " + dr["ndom"].ToString().Trim();
                                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                                //Площадь
                                excells1 = excelL.ExlWs.Range["C" + excelRow, "C" + (excelRow + 8)];
                                excells1.Merge(Type.Missing);
                                excells1.FormulaR1C1 = Decimal.Parse(dr["pl_kvar"].ToString());
                                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                                //Количество жильцов
                                excells1 = excelL.ExlWs.Range["D" + excelRow, "D" + (excelRow + 8)];
                                excells1.Merge(Type.Missing);
                                excells1.FormulaR1C1 = dr["count_gil"].ToString();
                                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                                //Количество ЛС
                                excells1 = excelL.ExlWs.Range["E" + excelRow, "E" + (excelRow + 8)];
                                excells1.Merge(Type.Missing);
                                excells1.FormulaR1C1 = dr["count_ls"].ToString();
                                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                                excelL.ExlWs.Cells[excelRow, 6] = "Сальдо 01." + listprm[0].month_.ToString("00") + "." + listprm[0].year_ + " г.";
                                excelL.ExlWs.Cells[excelRow + 1, 6] = "в т.ч. сальдо ОДН";
                                excelL.ExlWs.Cells[excelRow + 2, 6] = "Начислено";
                                excelL.ExlWs.Cells[excelRow + 3, 6] = "в т.ч. начислено ОДН";
                                excelL.ExlWs.Cells[excelRow + 4, 6] = "Оплачено";
                                excelL.ExlWs.Cells[excelRow + 5, 6] = "в т.ч. оплачено ОДН";
                                excelL.ExlWs.Cells[excelRow + 6, 6] = "Списано сальдо";
                                excelL.ExlWs.Cells[excelRow + 7, 6] = "Сальдо " + DateTime.DaysInMonth(Convert.ToInt32(listprm[0].year_),
                                        Convert.ToInt32(listprm[0].month_)) +
                                        "." + listprm[0].month_.ToString("00") + "." + listprm[0].year_ + " г.";
                                excelL.ExlWs.Cells[excelRow + 8, 6] = "в т.ч. сальдо ОДН";

                            }

                            #endregion

                            excelL.ExlWs.Cells[excelRow, excelCol] = dr["sum_insaldo"];
                            excelL.ExlWs.Cells[excelRow + 1, excelCol] = dr["sum_insaldo_odn"];
                            excelL.ExlWs.Cells[excelRow + 2, excelCol] = dr["sum_charge"];
                            excelL.ExlWs.Cells[excelRow + 3, excelCol] = dr["sum_charge_odn"];
                            excelL.ExlWs.Cells[excelRow + 4, excelCol] = dr["sum_money"];
                            excelL.ExlWs.Cells[excelRow + 5, excelCol] = dr["sum_money_odn"];
                            excelL.ExlWs.Cells[excelRow + 6, excelCol] = dr["real_charge"];
                            excelL.ExlWs.Cells[excelRow + 7, excelCol] = dr["sum_outsaldo"];
                            excelL.ExlWs.Cells[excelRow + 8, excelCol] = dr["sum_outsaldo_odn"];


                            sumInsaldo = sumInsaldo + Decimal.Parse(dr["sum_insaldo"].ToString());
                            sumCharge = sumCharge + Decimal.Parse(dr["sum_charge"].ToString());
                            sumMoney = sumMoney + Decimal.Parse(dr["sum_money"].ToString());
                            sumOutsaldo = sumOutsaldo + Decimal.Parse(dr["sum_outsaldo"].ToString());


                            sumInsaldoOdn = sumInsaldoOdn + Decimal.Parse(dr["sum_insaldo_odn"].ToString());
                            sumChargeOdn = sumChargeOdn + Decimal.Parse(dr["sum_charge_odn"].ToString());
                            sumMoneyOdn = sumMoneyOdn + Decimal.Parse(dr["sum_money_odn"].ToString());
                            sumOutsaldoOdn = sumOutsaldoOdn + Decimal.Parse(dr["sum_outsaldo_odn"].ToString());
                            realCharge = realCharge + Decimal.Parse(dr["real_charge"].ToString());

                            excelCol++;

                        }

                        if (oldDom != "-1")
                        {
                            excelL.ExlWs.Cells[excelRow, 7] = sumInsaldo;
                            excelL.ExlWs.Cells[excelRow + 1, 7] = sumInsaldoOdn;
                            excelL.ExlWs.Cells[excelRow + 2, 7] = sumCharge;
                            excelL.ExlWs.Cells[excelRow + 3, 7] = sumChargeOdn;
                            excelL.ExlWs.Cells[excelRow + 4, 7] = sumMoney;
                            excelL.ExlWs.Cells[excelRow + 5, 7] = sumMoneyOdn;
                            excelL.ExlWs.Cells[excelRow + 6, 7] = realCharge;
                            excelL.ExlWs.Cells[excelRow + 7, 7] = sumOutsaldo;
                            excelL.ExlWs.Cells[excelRow + 8, 7] = sumOutsaldoOdn;
                            excelRow = excelRow + 9;

                        }

                        #endregion


                        #endregion

                        #region Пишем подвал

                        if (excelRow > 7)
                        {
                            excells1 = excelL.ExlWs.Range["A" + (excelRow - 9), exform.BukvaList[servCount + 6] + (excelRow - 1)];
                            excells1.Font.Bold = true;
                        }

                        excells1 = excelL.ExlWs.Range[exform.BukvaList[6] + "5", exform.BukvaList[servCount + 6] + "5"];
                        excells1.Borders.LineStyle = XlLineStyle.xlContinuous;



                        excells1 = excelL.ExlWs.Range[exform.BukvaList[6] + "7", exform.BukvaList[servCount + 6] + (excelRow - 1)];
                        excells1.EntireColumn.NumberFormat = "# ##0,00";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                        excells1 = excelL.ExlWs.Range["C7", "C" + (excelRow - 1)];
                        excells1.EntireColumn.NumberFormat = "# ##0,00";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;

                        excells1 = excelL.ExlWs.Range["A5", exform.BukvaList[servCount + 6] + (excelRow - 1)];
                        excells1.Font.Size = 8;
                        //excells1.Columns.AutoFit();
                        //excells1.EntireColumn.AutoFit();
                        excells1 = excelL.ExlWs.Range["A5", "G" + (excelRow - 1)];
                        excells1.Columns.AutoFit();


                        excells1 = excelL.ExlWs.Range["B7", "B" + (excelRow - 1)];
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                        excells1 = excelL.ExlWs.Range["F5", "F" + (excelRow - 1)];
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                        SetReportSing(excelL, excelRow + 3, false);
                        //ExcelL.ExlWs.Cells[ExcelRow + 3, 2] = "Директор";
                        //ExcelL.ExlWs.Cells[ExcelRow + 5, 2] = "Начальник ПЭО";
                        //ExcelL.ExlWs.Cells[ExcelRow + 7, 2] = "Начальник ОНУПН";
                        //ExcelL.ExlWs.Cells[ExcelRow + 3, 3] = " Мякишев М.В.";
                        //ExcelL.ExlWs.Cells[ExcelRow + 5, 3] = " Соковых И.А. ";
                        //ExcelL.ExlWs.Cells[ExcelRow + 7, 3] = " Старкова Л.А";

                        #endregion
                    }
                    else
                    {

                        excells1 = excelL.ExlWs.Range["A5", "I5"];
                        excells1.EntireColumn.AutoFit();
                        excelL.ExlWs.Cells[10, 2] = "Статистика по домам по начислениям не подсчитана";
                    }

                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(Global.Constants.ExcelDir, "DomNach" +
                            listprm[0].nzp_area + "_" +
                            listprm[0].nzp_geu + "_" +
                            listprm[0].year_ + "_" +
                            listprm[0].month_
                            + "_" + nzpUser) + ".xls";
                        ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName, ref excelL.ExlWb, ref excelL.ExlApp);
                        excelL.ExlWb.Close();
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }

                    #endregion

                }
                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetDomNach DataTable : ОШИБКА!";
                    ////удаление объекта
                    //ExcelL.DeleteObject();
                    return ret;
                }

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при формирование DomNach " + ex.Message, MonitorLog.typelog.Warn, true);
                ret.result = false;
                ret.text = "Создание GetDomNach DataTable : ОШИБКА!" + ex.Message;
                ////удаление объекта
                //ExcelL.DeleteObject();
                return ret;

            }
            finally
            {
                //удаление объекта
                if (excelL != null)
                {
                    excelL.DeleteObject();
                }
            }
            return ret;

        }



        //Отчет начисления по поставщикам
        public Returns GetNachSupp(int supp, SupgFinder finder, int yearr, bool serv, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            ExcelLoader ExcelL = new ExcelLoader();
            ExcelCreater ExcelCr = new ExcelCreater();
            ExcelFormater ExcelFor = new ExcelFormater();
            ArrayList services = new ArrayList();
            Microsoft.Office.Interop.Excel.Range excell;
            Supg sg = new Supg();
            string s_fact = "";
            string po_fact = "";
            string n_supp = "";
            string n_serv = "";

            System.Data.DataTable DT_Body = sg.GetNachSupp(supp, finder, out ret, yearr, serv);
            System.Data.DataTable DT_Info = sg.GetReportInfo(finder.nzp_user, out ret);

            Dictionary<string, string> Dic = new Dictionary<string, string>();

            try
            {
                string dat_s = Convert.ToDateTime(finder._date_from).ToString("MM.yyyy");
                s_fact = "01." + dat_s;
                string dat_po = Convert.ToDateTime(finder._date_to).ToString("MM.yyyy");
                po_fact = "01." + dat_po;

                //  po_fact = Convert.ToDateTime(finder._date_to).ToString("dd.MM.yyyy");
                //  s_fact = Convert.ToDateTime(finder._date_from).ToString("dd.MM.yyyy");
            }
            catch
            {

            }

            if (finder.zk_nzp_supp == -1)
            {

                n_supp += "Все поставщики";
            }
            else
            {
                if (!serv)
                {
                    n_supp += finder.porch.ToString();
                }
                else
                {
                    n_supp += "Все поставщики";
                }
            }

            string constr = SetReportConditional(finder.nzp_user).ToString().Trim();
            Dic.Add("condition", constr);

            constr = constr.Replace("\n", " ");
            char splitter = ' ';
            string[] tmas_str = constr.Split(splitter);

            string t_str = "";
            int k = 0;
            bool b_area = false, b_geu = false;
            while (k < tmas_str.Count())
            {
                if (tmas_str[k] != "\r")
                {
                    if (tmas_str[k] == "Управляющая организация:")
                    {
                        t_str = "";
                        while (tmas_str[k] != "\r" && tmas_str[k] != "Отделение:")
                        {
                            t_str += tmas_str[k] + " ";
                            k++;
                        }

                        Dic.Add("area", t_str.Substring(11));
                        b_area = true;
                    }
                    if (tmas_str[k] == "Отделение:")
                    {
                        t_str = "";
                        while (tmas_str[k] != "\r" && tmas_str[k] != "\rУлица")
                        {
                            t_str += tmas_str[k] + " ";
                            k++;
                        }
                        Dic.Add("geu", t_str);
                        b_geu = true;
                        break;
                    }
                }
                k++;
            }

            if (!b_area)
            {
                Dic.Add("area", "Все балансодержатели");
            }
            if (!b_geu)
            {
                Dic.Add("geu", "Все участки");
            }

            if (finder.nzp_serv == -1)
            {
                n_serv += "Все услуги";
            }
            else
            {
                if (serv)
                {
                    n_serv += finder.num.ToString();
                }
                else
                {
                    n_serv += "Все услуги";
                }
            }

            Dic.Add("p1", s_fact);
            Dic.Add("p2", po_fact);
            Dic.Add("p3", n_serv);

            Dic.Add("p4", n_supp);



            Dic.Add("date", System.DateTime.Today.ToShortDateString());
            Dic.Add("time", System.DateTime.Now.ToShortTimeString());

            string[,] str;
            object[,] dec;

            try
            {
                str = new string[DT_Body.Rows.Count, 1];
                if (serv)
                {
                    dec = new object[DT_Body.Rows.Count, 9];
                }
                else
                {
                    dec = new object[DT_Body.Rows.Count, 8];
                }
            }
            catch (Exception)
            {
                str = new string[100, 1];

                if (serv)
                {
                    dec = new object[100, 9];
                }
                else
                {
                    dec = new object[100, 8];
                }

            }


            decimal sum_tarif = 0;
            decimal sum_lgota = 0;
            decimal sum_subsidy = 0;
            decimal sum_smo = 0;
            decimal real_charge = 0;
            decimal perr_nach = 0;
            decimal rashod = 0;
            decimal sum_charge = 0;

            try
            {
                int indexer = 8;
                if (serv)
                {
                    ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("nachsupp2.xls"), Dic);
                }
                else
                {
                    ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("nachsupp1.xls"), Dic);
                }



                if (DT_Body.Rows.Count != 0)
                {
                    for (int r = 0; r < DT_Body.Rows.Count; r++)
                    {



                        str[r, 0] = DT_Body.Rows[r][0].ToString();


                        dec[r, 0] = DT_Body.Rows[r][1];
                        sum_tarif += Convert.ToDecimal(DT_Body.Rows[r][1]);



                        dec[r, 1] = DT_Body.Rows[r][3];
                        sum_lgota += Convert.ToDecimal(DT_Body.Rows[r][3]);



                        dec[r, 2] = DT_Body.Rows[r][5];
                        sum_subsidy += Convert.ToDecimal(DT_Body.Rows[r][5]);


                        if (serv)
                        {
                            dec[r, 3] = DT_Body.Rows[r][9];
                            sum_smo += Convert.ToDecimal(DT_Body.Rows[r][9]);

                        }
                        else
                        {
                            dec[r, 3] = DT_Body.Rows[r][8];
                            sum_smo += Convert.ToDecimal(DT_Body.Rows[r][8]);
                        }



                        dec[r, 4] = DT_Body.Rows[r][7];
                        real_charge += Convert.ToDecimal(DT_Body.Rows[r][7]);





                        dec[r, 5] = DT_Body.Rows[r][6];
                        perr_nach += Convert.ToDecimal(DT_Body.Rows[r][6]);

                        if (serv)
                        {

                            dec[r, 6] = DT_Body.Rows[r][8];
                            rashod += Convert.ToDecimal(DT_Body.Rows[r][8]);


                        }
                        else
                        {

                            dec[r, 6] = DT_Body.Rows[r][4];
                            sum_charge += Convert.ToDecimal(DT_Body.Rows[r][4]);


                        }
                        if (serv)
                        {

                            dec[r, 7] = DT_Body.Rows[r][4];
                            sum_charge += Convert.ToDecimal(DT_Body.Rows[r][4]);
                        }

                        if (serv)
                        {
                            excell = ExcelL.ExlWs.get_Range("A" + (indexer + r).ToString(), "I" + (indexer + r).ToString());
                            excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        }
                        else
                        {
                            excell = ExcelL.ExlWs.get_Range("A" + (indexer + r).ToString(), "H" + (indexer + r).ToString());
                            excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                            excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        }
                    }
                }
                else
                {
                    if (serv)
                    {
                        excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "I" + (indexer + 4).ToString());
                        excell.Borders[XlBordersIndex.xlEdgeTop].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excell.Borders.Weight = 3;
                        excell.Borders[XlBordersIndex.xlEdgeRight].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excell.Borders.Weight = 3;
                        excell.Borders[XlBordersIndex.xlEdgeLeft].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excell.Borders.Weight = 3;
                        excell.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excell.Borders.Weight = 3;
                        excell.Merge(Type.Missing);
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        //  excell.Font.Size = 12;
                        excell.Font.Bold = true;
                        excell.Value2 = "нет данных";
                    }
                    else
                    {
                        excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "H" + (indexer + 4).ToString());
                        excell.Borders[XlBordersIndex.xlEdgeTop].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excell.Borders.Weight = 3;
                        excell.Borders[XlBordersIndex.xlEdgeRight].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excell.Borders.Weight = 3;
                        excell.Borders[XlBordersIndex.xlEdgeLeft].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excell.Borders.Weight = 3;
                        excell.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excell.Borders.Weight = 3;
                        excell.Merge(Type.Missing);
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        //   excell.Font.Size = 12;
                        excell.Font.Bold = true;
                        excell.Value2 = "нет данных";
                    }

                }

                if (DT_Body.Rows.Count != 0)
                {
                    #region Итого


                    excell = ExcelL.ExlWs.get_Range("A8", "A" + (indexer + DT_Body.Rows.Count).ToString());
                    excell.ColumnWidth = 30;
                    excell.Value2 = str;
                    str = null;

                    if (serv)
                    {
                        excell = ExcelL.ExlWs.get_Range("B8", "I" + (indexer + DT_Body.Rows.Count).ToString());
                        excell.Value2 = dec;
                        dec = null;
                    }
                    else
                    {
                        excell = ExcelL.ExlWs.get_Range("B8", "H" + (indexer + DT_Body.Rows.Count).ToString());
                        excell.Value2 = dec;
                        dec = null;
                    }



                    excell = ExcelL.ExlWs.get_Range("A" + (indexer + DT_Body.Rows.Count).ToString());
                    //  excell.ColumnWidth = 30;
                    excell.NumberFormat = "@";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.WrapText = true;
                    excell.Value2 = "Итого";

                    excell = ExcelL.ExlWs.get_Range("B" + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.Value2 = sum_tarif;

                    excell = ExcelL.ExlWs.get_Range("C" + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.Value2 = sum_lgota;

                    excell = ExcelL.ExlWs.get_Range("D" + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.Value2 = sum_subsidy;

                    excell = ExcelL.ExlWs.get_Range("E" + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.Value2 = sum_smo;

                    excell = ExcelL.ExlWs.get_Range("F" + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.Value2 = real_charge;

                    excell = ExcelL.ExlWs.get_Range("G" + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.Value2 = perr_nach;

                    if (serv)
                    {
                        excell = ExcelL.ExlWs.get_Range("H" + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                        excell.Value2 = rashod;
                    }
                    else
                    {
                        excell = ExcelL.ExlWs.get_Range("H" + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                        excell.Value2 = sum_charge;
                    }
                    if (serv)
                    {
                        excell = ExcelL.ExlWs.get_Range("I" + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                        excell.Value2 = sum_charge;
                    }



                    if (serv)
                    {
                        excell = ExcelL.ExlWs.get_Range("B8", "I" + (indexer + DT_Body.Rows.Count).ToString());
                        //  excell.ColumnWidth = 15;
                        excell.NumberFormat = "# ##0,00";
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                    }
                    else
                    {
                        excell = ExcelL.ExlWs.get_Range("B8", "H" + (indexer + DT_Body.Rows.Count).ToString());
                        //   excell.ColumnWidth = 15;
                        excell.NumberFormat = "# ##0,00";
                        excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;

                    }
                    if (serv)
                    {
                        excell = ExcelL.ExlWs.get_Range("A" + (indexer + DT_Body.Rows.Count).ToString(), "I" + (indexer + DT_Body.Rows.Count).ToString());
                        excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.Font.Bold = true;

                    }
                    else
                    {
                        excell = ExcelL.ExlWs.get_Range("A" + (indexer + DT_Body.Rows.Count).ToString(), "H" + (indexer + DT_Body.Rows.Count).ToString());
                        excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excell.Font.Bold = true;

                    }

                    excell = ExcelL.ExlWs.get_Range("B7", "I7");
                    //  excell.ColumnWidth = 15;
                    excell.NumberFormat = "@";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;



                    #endregion
                }
                SetReportSing(ExcelL, indexer + DT_Body.Rows.Count + 3, false);


                if (ret.result)
                {
                    fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "suppnach" + finder.nzp_user) + ".xls";
                    //Сохранение
                    ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                    if (InputOutput.useFtp)
                    {
                        fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                    }
                }
                else
                {
                    MonitorLog.WriteLog("Ошибка создания Excel файла : " + ret.text, MonitorLog.typelog.Error, true);

                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Формирование Excel ошибка :" + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;

            }
            finally
            {
                //удаление объекта
                ExcelL.DeleteObject();
            }
            return ret;
        }





        public Returns GetSpravPULs(Prm prm_, string Nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetSparavPuLs(prm_, out ret, Nzp_user);

            ExcelLoader ExcelL = null;
            try
            {
                ExcelL = new ExcelLoader();

                if (DT != null)
                {

                    ExcelCreater ExcelCr = new ExcelCreater();


                    #region Создаем шапку
                    Microsoft.Office.Interop.Excel.Range excells1;

                    Dictionary<string, string> Dic = new Dictionary<string, string>();
                    Dic.Add("ERCName", SetReportHeader());
                    Dic.Add("rajon", "");//жэу


                    Dic.Add("DATE", System.DateTime.Now.ToShortDateString());//месяц
                    Dic.Add("month", Utils.GetMonthName(prm_.month_));//месяц
                    Dic.Add("year", prm_.year_.ToString());//год
                    #endregion

                    ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("SpravPuLs.xls"), Dic);

                    excells1 = ExcelL.ExlWs.get_Range("D3", "L4");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = SetReportConditional(System.Convert.ToInt32(Nzp_user));
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.Font.Size = 8;
                    excells1.RowHeight = 23;

                    #region Пишем тело

                    int ExcelRow = 8;
                    int i = 0;
                    string adres = "";
                    string old_adres = "";
                    while (i < DT.Rows.Count)
                    {

                        if (DT.Rows[i]["ulica"].ToString().Trim() != "ВСЕГО")
                        {
                            adres = DT.Rows[i]["ulica"].ToString().Trim() + " дом." +
                            DT.Rows[i]["ndom"].ToString().Trim();
                            if (DT.Rows[i]["nkor"].ToString().Trim() != "-")
                            {
                                adres = adres + " корп. " + DT.Rows[i]["nkor"].ToString().Trim();
                            }
                        }
                        else
                            adres = "ВСЕГО";



                        if (adres != old_adres)
                        {
                            ExcelL.ExlWs.Cells[ExcelRow, 1] = adres;
                            old_adres = adres;
                        }


                        ExcelL.ExlWs.Cells[ExcelRow, 2] = DT.Rows[i]["service"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 3] = DT.Rows[i]["count_ls"].ToString();
                        ExcelL.ExlWs.Cells[ExcelRow, 4] = DT.Rows[i]["count_pu"].ToString();
                        ExcelL.ExlWs.Cells[ExcelRow, 5] = DT.Rows[i]["count_npu"].ToString();
                        ExcelL.ExlWs.Cells[ExcelRow, 6] = DT.Rows[i]["count_gil"].ToString();
                        ExcelL.ExlWs.Cells[ExcelRow, 7] = Decimal.Parse(DT.Rows[i]["c_calc"].ToString());
                        ExcelL.ExlWs.Cells[ExcelRow, 8] = Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());
                        ExcelL.ExlWs.Cells[ExcelRow, 9] = Decimal.Parse(DT.Rows[i]["reval"].ToString());
                        ExcelL.ExlWs.Cells[ExcelRow, 10] = Decimal.Parse(DT.Rows[i]["sum_charge"].ToString());
                        ExcelL.ExlWs.Cells[ExcelRow, 11] = Decimal.Parse(DT.Rows[i]["c_sn"].ToString());
                        ExcelL.ExlWs.Cells[ExcelRow, 12] = Decimal.Parse(DT.Rows[i]["c_sv"].ToString());

                        ExcelRow++;
                        i++;
                    }
                    #endregion

                    #region форматируем
                    if (ExcelRow > 8) ExcelRow--;

                    /* excells1 = ExcelL.ExlWs.get_Range("B" + ExcelRow.ToString(), "L" + ExcelRow.ToString());
                     excells1.Font.Bold = true;*/

                    excells1 = ExcelL.ExlWs.get_Range("G8", "L" + ExcelRow.ToString());
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Font.Size = 8;
                    excells1.EntireColumn.NumberFormat = "# ##0,00";

                    excells1 = ExcelL.ExlWs.get_Range("A8", "F" + ExcelRow.ToString());
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Font.Size = 8;


                    #endregion

                    SetReportSing(ExcelL, ExcelRow + 3, false);
                    //ExcelL.ExlWs.Cells[ExcelRow + 3, 2] = "Директор";
                    //ExcelL.ExlWs.Cells[ExcelRow + 5, 2] = "Начальник ПЭО";
                    //ExcelL.ExlWs.Cells[ExcelRow + 7, 2] = "Начальник ОНУПН";
                    //ExcelL.ExlWs.Cells[ExcelRow + 3, 3] = " Мякишев М.В.";
                    //ExcelL.ExlWs.Cells[ExcelRow + 5, 3] = " Соковых И.А.";
                    //ExcelL.ExlWs.Cells[ExcelRow + 7, 3] = " Старкова Л.А.";

                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SpravPULs" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }

                    #endregion

                }

                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSuppNachHar DataTable : ОШИБКА!";

                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SpravPULs" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }

                    //удаление объекта
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                try
                {
                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SpravPULs" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет SpravSuppNachHar", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSuppNach Excel File : ОШИБКА!";
                }

            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }

            return ret;

        }



        /// <summary>
        /// Заполнение строки по одному дому
        /// </summary>
        /// <param name="excells1"></param>
        /// <param name="excelRow"></param>
        /// <param name="countDom"></param>
        /// <param name="ExcelL"></param>
        /// <param name="adres"></param>
        /// <param name="exform"></param>
        /// <param name="nachData"></param>
        public void FillOneRowSpravSodergOdn9(Range excells1,
            int excelRow, int countDom, ExcelLoader ExcelL, List<string> adres,
            ExcelFormater exform, decimal[,] nachData,
            System.Data.DataTable listVal)
        {

            const int rowCount = 6;

            if (adres[0] == "Итого")
            {
                excells1 = ExcelL.ExlWs.get_Range("A" + excelRow,
                "E" + (excelRow + rowCount - 1));
                excells1.Merge(Type.Missing);
                excells1.Font.Size = 6;
                excells1.FormulaR1C1 = "Итого";
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;


            }
            else
            {

                excells1 = ExcelL.ExlWs.Range["A" + excelRow, "A" + (excelRow + rowCount - 1)];
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = countDom;


                excells1 = ExcelL.ExlWs.Range["B" + excelRow, "B" + (excelRow + rowCount - 1)];
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = adres[0];


                excells1 = ExcelL.ExlWs.Range["C" + excelRow, "C" + (excelRow + rowCount - 1)];
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = adres[1];

                excells1 = ExcelL.ExlWs.Range["D" + excelRow, "D" + (excelRow + rowCount - 1)];
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = adres[2];

                excells1 = ExcelL.ExlWs.Range["E" + excelRow, "E" + (excelRow + rowCount - 1)];
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = adres[3];
            }

            //excells1 = ExcelL.ExlWs.Range["F" + excelRow, "F" + excelRow];

            ExcelL.ExlWs.Cells[excelRow, 6] = "Численность, чел";
            ExcelL.ExlWs.Cells[excelRow + 1, 6] = "Объем л/с, м3";
            ExcelL.ExlWs.Cells[excelRow + 2, 6] = "объем ОДН, м3";
            ExcelL.ExlWs.Cells[excelRow + 3, 6] = "площадь, м2";
            ExcelL.ExlWs.Cells[excelRow + 4, 6] = "Объем л/с, Гкал";
            ExcelL.ExlWs.Cells[excelRow + 5, 6] = "объем ОДН, Гкал";



            int maxExcelCol = 7;
            string excelLetter;
            //Присваиваем данные по формулам

            for (int j = 0; j < listVal.Rows.Count; j++)
            {
                excelLetter = exform.BukvaList[7 + j - 1];
                DataRow dr = listVal.Rows[j];

                if ((decimal)dr["countGil"] != 0m)
                {
                    ExcelL.ExlWs.Cells[excelRow, 7 + j] = dr["countGil"];
                    excells1 = ExcelL.ExlWs.Range[excelLetter + (excelRow),
                                                      excelLetter + (excelRow)];
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "0";
                }
                if (Math.Abs((decimal)dr["valKub"]) > 0m)
                {
                    ExcelL.ExlWs.Cells[excelRow + 1, 7 + j] = dr["valKub"];
                    excells1 = ExcelL.ExlWs.Range[excelLetter + (excelRow + 1),
                                                      excelLetter + (excelRow + 1)];
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00000";

                }
                if (Math.Abs((decimal)dr["valKubOdn"]) > 0m)
                {
                    ExcelL.ExlWs.Cells[excelRow + 2, 7 + j] = dr["valKubOdn"];
                    excells1 = ExcelL.ExlWs.Range[excelLetter + (excelRow + 2), excelLetter + (excelRow + 2)];
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00000";

                }

                //if (((decimal)dr["squareFlat"] > 0m) & (Math.Abs((decimal)dr["valGkal"]) +
                //    Math.Abs((decimal)dr["valGkalOdn"]) + Math.Abs((decimal)dr["valKub"]) +
                //    Math.Abs((decimal)dr["valKubOdn"]) > 0.0001m))
                if ((decimal)dr["squareFlat"] > 0m)
                {
                    ExcelL.ExlWs.Cells[excelRow + 3, 7 + j] = dr["squareFlat"];
                    excells1 = ExcelL.ExlWs.Range[excelLetter + (excelRow + 3),
                                                      excelLetter + (excelRow + 3)];
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";

                }


                if (Math.Abs((decimal)dr["valGkal"]) > 0m)
                {
                    ExcelL.ExlWs.Cells[excelRow + 4, 7 + j] = dr["valGkal"];

                    excells1 = ExcelL.ExlWs.Range[excelLetter + (excelRow + 4),
                                                      excelLetter + (excelRow + 4)];
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00000";


                }

                if (Math.Abs((decimal)dr["valGkalOdn"]) > 0m)
                {
                    ExcelL.ExlWs.Cells[excelRow + 5, 7 + j] = dr["valGkalOdn"];

                    excells1 = ExcelL.ExlWs.Range[excelLetter + (excelRow + 5),
                                                      excelLetter + (excelRow + 5)];
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00000";


                }

                maxExcelCol++;
            }



            //Присваиваем данные по начислениям
            for (int j = 0; j < nachData.GetLength(1); j++)
            {
                excelLetter = exform.BukvaList[maxExcelCol + j - 1];
                for (int i = 0; i < 5; i++)
                {

                    if (nachData[i, j] != 0m)
                    {
                        ExcelL.ExlWs.Cells[excelRow + i, maxExcelCol + j] = nachData[i, j];
                    }
                }
                if ((j == 0) || (j == 1) || (j == 2) || (j > 5))
                {
                    excells1 = ExcelL.ExlWs.Range[excelLetter +
                    excelRow, excelLetter + (excelRow + rowCount - 1)];
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.Merge(Type.Missing);
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;

                }
                else
                {
                    excells1 = ExcelL.ExlWs.Range[excelLetter +
                    (excelRow), excelLetter + (excelRow + 1)];
                    excells1.NumberFormat = "# ##0,00000";

                    excells1 = ExcelL.ExlWs.Range[excelLetter +
                    (excelRow + 2), excelLetter + (excelRow + 3)];

                    excells1.Merge(Type.Missing);

                    excells1 = ExcelL.ExlWs.Range[excelLetter +
                  (excelRow + 4), excelLetter + (excelRow + 5)];
                    excells1.NumberFormat = "# ##0,00000";
                }

            }




        }

        /// <summary>
        /// Справка по услуге ГВС 
        /// </summary>
        /// <param name="prm_"></param>
        /// <param name="Nzp_user"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Returns GetSpravSodergOdn9(Prm prm_, string Nzp_user, ref string fileName)
        {
            Returns ret;
            string nameSupp = "";

            //создание dataTable
            var exR = new ExcelRep();
            System.Data.DataTable dt = exR.GetSpravSoderg(prm_, out ret, Nzp_user);

            ExcelLoader excelL = null;
            try
            {


                if (dt != null)
                {
                    excelL = new ExcelLoader();



                    #region Создаем шапку

                    var dic = new Dictionary<string, string>
                    {
                        {"ERCName", SetReportHeader()},
                        {"month", Utils.GetMonthName(prm_.month_)},
                        {"year", prm_.year_.ToString(CultureInfo.InvariantCulture)},
                        {"DATE", DateTime.Now.ToShortDateString()}
                    };
                    if (prm_.has_pu == "1")
                    {
                        dic.Add("ODPU", "(все дома)");
                    }
                    else
                        if (prm_.has_pu == "2")
                        {
                            dic.Add("ODPU", "(дома с ОДПУ)");
                        }
                        else
                        {
                            dic.Add("ODPU", "(дома без ОДПУ)");
                        }



                    // Массив заголовков
                    var columnHeaderList = new List<string>
                    {
                        "Начислено (тариф*кол-во), руб.",
                        "Постоянное начисление, руб.",
                        "начислено ОДН, руб",
                        "Возврат за услуги, руб.",
                        "Начислено раз. красным, руб.",
                        "Начислено раз черным, руб.",
                        "Итого к оплате, руб.",
                        "Начислено к оплате, руб."
                    };

                    #endregion


                    #region Определение услуги
                    var dbSprav = new DbSprav();
                    var finder = new Finder { nzp_user = prm_.nzp_serv, RolesVal = new List<_RolesVal>() };
                    var p = new _RolesVal
                    {
                        kod = Global.Constants.role_sql_serv,
                        tip = Global.Constants.role_sql,
                        val = prm_.nzp_serv.ToString(CultureInfo.InvariantCulture)
                    };
                    finder.RolesVal.Add(p);
                    List<_Service> lserv = dbSprav.ServiceLoad(finder, out ret);
                    dic.Add("SERVICE", lserv.Count > 0 ? lserv[0].service : "Услуга неопределена");
                    dbSprav.Close();
                    #endregion

                    dic.Add("NAMESUPP", prm_.nzp_key > -1 ? prm_.name_prm : "Все поставщики");
                    dbSprav.Close();


                    excelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("sprav_soderg.xls"), dic);
                    var exform = new ExcelFormater();
                    var excells1 = excelL.ExlWs.Range["D3", "L4"];
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = SetReportConditional(Convert.ToInt32(Nzp_user));
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.Font.Size = 8;
                    excells1.RowHeight = 23;
                    excelL.ExlWs.PageSetup.PrintTitleRows = "$6:$7";

                    #region Пишем тело
                    excells1 = excelL.ExlWs.Range["F6", "F7"];
                    excells1.Merge(Type.Missing);
                    excells1.Font.Size = 6;
                    excells1.FormulaR1C1 = "";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                    excells1.WrapText = true;

                    int excelRow = 8;

                    int excelCol = 7;
                    int countDom = 0;
                    string oldAdres = "";
                    string oldUlica = "";
                    string oldDom = "";
                    string oldLitera = "";
                    string ulica = "";
                    string dom = "";
                    string litera = "";
                    string geu = "";


                    var listVal = new System.Data.DataTable();
                    var listValItogo = new System.Data.DataTable();
                    listVal.Columns.Add("countGil", typeof(decimal));
                    listVal.Columns.Add("valKub", typeof(decimal));
                    listVal.Columns.Add("valKubOdn", typeof(decimal));
                    listVal.Columns.Add("valGkal", typeof(decimal));
                    listVal.Columns.Add("valGkalOdn", typeof(decimal));
                    listVal.Columns.Add("squareFlat", typeof(decimal));
                    listValItogo.Columns.Add("countGil", typeof(decimal));
                    listValItogo.Columns.Add("valKub", typeof(decimal));
                    listValItogo.Columns.Add("valKubOdn", typeof(decimal));
                    listValItogo.Columns.Add("valGkal", typeof(decimal));
                    listValItogo.Columns.Add("valGkalOdn", typeof(decimal));
                    listValItogo.Columns.Add("squareFlat", typeof(decimal));


                    var adresMas = new List<string>();


                    var nachData = new decimal[5, 8];
                    var itonachData = new decimal[5, 8];

                    for (int j = 0; j < 5; j++)
                        for (int k = 0; k < 8; k++)
                        {
                            nachData[j, k] = 0;
                            itonachData[j, k] = 0;
                        }

                    int i = 0;

                    while (i < dt.Rows.Count)
                    {

                        string adres = dt.Rows[i]["ulica"].ToString().Trim() + "," +
                                       dt.Rows[i]["ndom"].ToString().Trim() +
                                       dt.Rows[i]["nkor"].ToString().Trim();
                        geu = dt.Rows[i]["geu"].ToString().Trim();
                        ulica = dt.Rows[i]["ulica"].ToString().Trim();
                        dom = dt.Rows[i]["ndom"].ToString().Trim();
                        litera = dt.Rows[i]["nkor"].ToString().Trim();

                        if (i == 0)
                        {
                            oldAdres = adres;
                            oldUlica = dt.Rows[i]["ulica"].ToString().Trim();
                            oldDom = dt.Rows[i]["ndom"].ToString().Trim();
                            oldLitera = dt.Rows[i]["nkor"].ToString().Trim();
                        }

                        #region Записываем Итого по дому
                        if (oldAdres != adres)
                        {
                            countDom++;
                            adresMas.Clear();
                            adresMas.Add(geu);
                            adresMas.Add(oldUlica);
                            adresMas.Add(oldDom);
                            adresMas.Add(oldLitera);


                            FillOneRowSpravSodergOdn9(excells1, excelRow, countDom, excelL, adresMas,
                            exform, nachData, listVal);


                            for (int n = 0; n < listVal.Rows.Count; n++)
                            {
                                listVal.Rows[n]["countGil"] = 0;

                                listVal.Rows[n]["valKub"] = 0;

                                listVal.Rows[n]["valKubOdn"] = 0;

                                listVal.Rows[n]["valGkal"] = 0;

                                listVal.Rows[n]["valGkalOdn"] = 0;

                                listVal.Rows[n]["squareFlat"] = 0;
                            }



                            for (int j = 0; j < 5; j++)
                                for (int k = 0; k < 8; k++)
                                {
                                    nachData[j, k] = 0;
                                }
                            excelCol = 7;
                            oldAdres = adres;
                            oldUlica = ulica;
                            oldDom = dom;
                            oldLitera = litera;
                            excelRow += 6;


                        }
                        #endregion


                        #region Пишем заголовок формул
                        if (excelRow == 8)
                        {
                            excelL.ExlWs.Cells[7, excelCol] = dt.Rows[i]["name_frm"].ToString();
                            excells1 = excelL.ExlWs.Range[exform.BukvaList[excelCol - 1] + "7", exform.BukvaList[excelCol - 1] + "7"];
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                            excells1.WrapText = true;
                            excells1.ColumnWidth = 10;
                            excells1.Font.Size = 6;
                        }
                        #endregion


                        //Проверка на количество формул
                        if (excelCol - 7 >= listVal.Rows.Count)
                        {

                            listVal.Rows.Add(0, 0, 0, 0, 0, 0);
                            listValItogo.Rows.Add(0, 0, 0, 0, 0, 0);
                        }

                        listVal.Rows[excelCol - 7]["countGil"] = (decimal)listVal.Rows[excelCol - 7]["countGil"] +
                            Int32.Parse(dt.Rows[i]["count_gil"].ToString());

                        listVal.Rows[excelCol - 7]["valKub"] = (decimal)listVal.Rows[excelCol - 7]["valKub"] +
                           Decimal.Parse(dt.Rows[i]["c_calc_kub"].ToString());

                        listVal.Rows[excelCol - 7]["valKubOdn"] = (decimal)listVal.Rows[excelCol - 7]["valKubOdn"] +
                         Decimal.Parse(dt.Rows[i]["c_calc_kub_odn"].ToString());

                        listVal.Rows[excelCol - 7]["valGkal"] = (decimal)listVal.Rows[excelCol - 7]["valGkal"] +
                          Decimal.Parse(dt.Rows[i]["c_calc_gkal"].ToString());

                        listVal.Rows[excelCol - 7]["valGkalOdn"] = (decimal)listVal.Rows[excelCol - 7]["valGkalOdn"] +
                         Decimal.Parse(dt.Rows[i]["c_calc_gkal_odn"].ToString());

                        listVal.Rows[excelCol - 7]["squareFlat"] = (decimal)listVal.Rows[excelCol - 7]["squareFlat"] +
                        Decimal.Parse(dt.Rows[i]["pl_kvar"].ToString());



                        listValItogo.Rows[excelCol - 7]["countGil"] = (decimal)listValItogo.Rows[excelCol - 7]["countGil"] +
                            Int32.Parse(dt.Rows[i]["count_gil"].ToString());

                        listValItogo.Rows[excelCol - 7]["valKub"] = (decimal)listValItogo.Rows[excelCol - 7]["valKub"] +
                           Decimal.Parse(dt.Rows[i]["c_calc_kub"].ToString());

                        listValItogo.Rows[excelCol - 7]["valKubOdn"] = (decimal)listValItogo.Rows[excelCol - 7]["valKubOdn"] +
                         Decimal.Parse(dt.Rows[i]["c_calc_kub_odn"].ToString());

                        listValItogo.Rows[excelCol - 7]["valGkal"] = (decimal)listValItogo.Rows[excelCol - 7]["valGkal"] +
                          Decimal.Parse(dt.Rows[i]["c_calc_gkal"].ToString());

                        listValItogo.Rows[excelCol - 7]["valGkalOdn"] = (decimal)listValItogo.Rows[excelCol - 7]["valGkalOdn"] +
                         Decimal.Parse(dt.Rows[i]["c_calc_gkal_odn"].ToString());

                        listValItogo.Rows[excelCol - 7]["squareFlat"] = (decimal)listValItogo.Rows[excelCol - 7]["squareFlat"] +
                        Decimal.Parse(dt.Rows[i]["pl_kvar"].ToString());





                        nachData[0, 0] = nachData[0, 0] + Decimal.Parse(dt.Rows[i]["rsum_tarif_all"].ToString());
                        nachData[1, 0] = 0;
                        nachData[2, 0] = 0;
                        nachData[3, 0] = 0;
                        nachData[4, 0] = 0;


                        nachData[0, 1] = nachData[0, 1] + Decimal.Parse(dt.Rows[i]["rsum_tarif"].ToString());
                        nachData[1, 1] = 0;
                        nachData[2, 1] = 0;
                        nachData[3, 1] = 0;
                        nachData[4, 1] = 0;

                        nachData[0, 2] = nachData[0, 2] + Decimal.Parse(dt.Rows[i]["sum_odn"].ToString());
                        nachData[1, 2] = 0;
                        nachData[2, 2] = 0;
                        nachData[3, 2] = 0;
                        nachData[4, 2] = 0;

                        nachData[0, 3] = nachData[0, 3] + Decimal.Parse(dt.Rows[i]["sum_nedop"].ToString());
                        nachData[1, 3] = nachData[1, 3] + Decimal.Parse(dt.Rows[i]["c_nedop_kub"].ToString());
                        nachData[2, 3] = 0;
                        nachData[3, 3] = 0;
                        nachData[4, 3] = nachData[4, 3] + Decimal.Parse(dt.Rows[i]["c_nedop_gkal"].ToString());

                        nachData[0, 4] = nachData[0, 4] + Decimal.Parse(dt.Rows[i]["reval_k"].ToString());
                        nachData[1, 4] = nachData[1, 4] + Decimal.Parse(dt.Rows[i]["c_reval_k_kub"].ToString());
                        nachData[2, 4] = 0;
                        nachData[3, 4] = 0;
                        nachData[4, 4] = nachData[4, 4] + Decimal.Parse(dt.Rows[i]["c_reval_k_gkal"].ToString());

                        nachData[0, 5] = nachData[0, 5] + Decimal.Parse(dt.Rows[i]["reval_d"].ToString());
                        nachData[1, 5] = nachData[1, 5] + Decimal.Parse(dt.Rows[i]["c_reval_d_kub"].ToString());
                        nachData[2, 5] = 0;
                        nachData[3, 5] = 0;
                        nachData[4, 5] = nachData[4, 5] + Decimal.Parse(dt.Rows[i]["c_reval_d_gkal"].ToString());

                        nachData[0, 6] = nachData[0, 6] + Decimal.Parse(dt.Rows[i]["sum_charge"].ToString());
                        nachData[1, 6] = 0;
                        nachData[2, 6] = 0;
                        nachData[3, 6] = 0;
                        nachData[4, 6] = 0;

                        nachData[0, 7] = nachData[0, 7] + Decimal.Parse(dt.Rows[i]["sum_charge"].ToString());
                        nachData[1, 7] = 0;
                        nachData[2, 7] = 0;
                        nachData[3, 7] = 0;
                        nachData[4, 7] = 0;


                        itonachData[0, 0] = itonachData[0, 0] + Decimal.Parse(dt.Rows[i]["rsum_tarif_all"].ToString());
                        itonachData[1, 0] = 0;
                        itonachData[2, 0] = 0;
                        itonachData[3, 0] = 0;
                        itonachData[4, 0] = 0;

                        itonachData[0, 1] = itonachData[0, 1] + Decimal.Parse(dt.Rows[i]["rsum_tarif"].ToString());
                        itonachData[1, 1] = 0;
                        itonachData[2, 1] = 0;
                        itonachData[3, 1] = 0;
                        itonachData[4, 1] = 0;

                        itonachData[0, 2] = itonachData[0, 2] + Decimal.Parse(dt.Rows[i]["sum_odn"].ToString());
                        itonachData[1, 2] = 0;
                        itonachData[2, 2] = 0;
                        itonachData[3, 2] = 0;
                        itonachData[4, 2] = 0;

                        itonachData[0, 3] = itonachData[0, 3] + Decimal.Parse(dt.Rows[i]["sum_nedop"].ToString());
                        itonachData[1, 3] = itonachData[1, 3] + Decimal.Parse(dt.Rows[i]["c_nedop_kub"].ToString());
                        itonachData[2, 3] = 0;
                        itonachData[3, 3] = 0;
                        itonachData[4, 3] = itonachData[4, 3] + Decimal.Parse(dt.Rows[i]["c_nedop_gkal"].ToString());

                        itonachData[0, 4] = itonachData[0, 4] + Decimal.Parse(dt.Rows[i]["reval_k"].ToString());
                        itonachData[1, 4] = itonachData[1, 4] + Decimal.Parse(dt.Rows[i]["c_reval_k_kub"].ToString());
                        itonachData[2, 4] = 0;
                        itonachData[3, 4] = 0;
                        itonachData[4, 4] = itonachData[4, 4] + Decimal.Parse(dt.Rows[i]["c_reval_k_gkal"].ToString());

                        itonachData[0, 5] = itonachData[0, 5] + Decimal.Parse(dt.Rows[i]["reval_d"].ToString());
                        itonachData[1, 5] = itonachData[1, 5] + Decimal.Parse(dt.Rows[i]["c_reval_d_kub"].ToString());
                        itonachData[2, 5] = 0;
                        itonachData[3, 5] = 0;
                        itonachData[4, 5] = itonachData[4, 5] + Decimal.Parse(dt.Rows[i]["c_reval_d_gkal"].ToString());

                        itonachData[0, 6] = itonachData[0, 6] + Decimal.Parse(dt.Rows[i]["sum_charge"].ToString());
                        itonachData[1, 6] = 0;
                        itonachData[2, 6] = 0;
                        itonachData[3, 6] = 0;
                        itonachData[4, 6] = 0;

                        itonachData[0, 7] = itonachData[0, 7] + Decimal.Parse(dt.Rows[i]["sum_charge"].ToString());
                        itonachData[1, 7] = 0;
                        itonachData[2, 7] = 0;
                        itonachData[3, 7] = 0;
                        itonachData[4, 7] = 0;


                        excelCol++;
                        i++;
                    }

                    countDom++;
                    if (dt.Rows.Count > 0)
                    {
                        adresMas.Clear();
                        adresMas.Add(geu);
                        adresMas.Add(ulica);
                        adresMas.Add(dom);
                        adresMas.Add(litera);


                        FillOneRowSpravSodergOdn9(excells1, excelRow, countDom, excelL, adresMas,
                        exform, nachData, listVal);

                        for (int n = 0; n < listVal.Rows.Count; n++)
                        {
                            listVal.Rows[n]["countGil"] = 0;

                            listVal.Rows[n]["valKub"] = 0;

                            listVal.Rows[n]["valKubOdn"] = 0;

                            listVal.Rows[n]["valGkal"] = 0;

                            listVal.Rows[n]["valGkalOdn"] = 0;

                            listVal.Rows[n]["squareFlat"] = 0;
                        }

                        #region Пишем заголовки колонок
                        for (int k = 0; k < columnHeaderList.Count; k++)
                        {
                            excells1 = excelL.ExlWs.Range[exform.BukvaList[excelCol - 1 + k] + "6",
                                exform.BukvaList[excelCol - 1 + k] + "7"];
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = columnHeaderList[k];
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                            excells1.WrapText = true;
                            excells1.ColumnWidth = 10;
                        }
                        #endregion

                        excells1 = excelL.ExlWs.Range["G6", exform.BukvaList[excelCol - 2] + "6"];
                        excells1.Merge(Type.Missing);
                        excells1.Font.Size = 6;
                        excells1.FormulaR1C1 = "Количество жильцов/нат.показатель";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                        excells1.WrapText = true;


                        //    ExcelCol = 7;




                    }
                    #endregion

                    if (dt.Rows.Count > 0)
                    {
                        #region Пишем итого
                        excelRow += 6;
                        adresMas.Clear();
                        adresMas.Add("Итого");
                        adresMas.Add("");
                        adresMas.Add("");
                        adresMas.Add("");

                        FillOneRowSpravSodergOdn9(excells1, excelRow, countDom, excelL, adresMas,
                        exform, itonachData, listValItogo);


                        #endregion

                        excelRow += 5;
                        #region форматируем
                        excells1 = excelL.ExlWs.Range["A6", exform.BukvaList[excelCol + columnHeaderList.Count - 2] + excelRow];
                        excells1.Font.Size = 6;
                        excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                        //excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[ExcelCol - 1].ToString() + "8",
                        //    exform.BukvaList[ExcelCol + columnHeaderList.Count - 2].ToString() + ExcelRow);
                        //excells1.EntireColumn.NumberFormat = "# ##0,00";
                        //excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;

                        excells1 = excelL.ExlWs.Range["M8", "O" + excelRow];
                        excells1.EntireColumn.NumberFormat = "# ##0,00###";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excells1 = excelL.ExlWs.Range["S8", "T" + excelRow];
                        excells1.EntireColumn.NumberFormat = "# ##0,00###";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        // Вместить поля по ширине листа
                        excelL.ExlWs.PageSetup.Zoom = false;
                        excelL.ExlWs.PageSetup.FitToPagesWide = 1;
                        excelL.ExlWs.PageSetup.FitToPagesTall = false;
                        #endregion
                    }

                    SetReportSing(excelL, excelRow + 3, true);



                    if (prm_.nzp_key != -1)
                    {
                        nameSupp = nameSupp + prm_.name_prm;
                    }
                    nameSupp = nameSupp.Replace(">", " ").Replace("<", " ").Replace("?", " ").Replace("[", " ");
                    nameSupp = nameSupp.Replace("]", " ").Replace(":", " ").Replace("|", " ").Replace("*", " ").Replace(@"\", " ");

                    nameSupp = nameSupp.Replace(@"/", " ").Replace(" ", "_");

                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(Global.Constants.ExcelDir,
                            prm_.month_ + "_Дислокация_ГВС_" + nameSupp +
                             "_" + DateTime.Now.GetHashCode() + Nzp_user) + ".xls";
                        ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName, ref excelL.ExlWb, ref excelL.ExlApp);
                        excelL.ExlWb.Close(false, Type.Missing, Type.Missing);
                        excelL.ExlWb = null;
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }

                    #endregion

                }

                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSoderg DataTable : ОШИБКА!";


                    //удаление объекта
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                try
                {
                    if (excelL != null)
                    {
                        fileName = GetFileName(Global.Constants.ExcelDir,
                            prm_.month_ + "_Дислокация_ГВС_" + nameSupp +
                             "_" + DateTime.Now.GetHashCode() + Nzp_user) + ".xls";
                        ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName, ref excelL.ExlWb, ref excelL.ExlApp);
                        excelL.ExlWb.Close(false, Type.Missing, Type.Missing);
                        excelL.ExlWb = null;
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет GetSpravSoderg", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSoderg Excel File : ОШИБКА!";
                }

            }
            finally
            {
                //удаление объекта
                if (excelL != null)
                {
                    excelL.DeleteObject();
                }
            }

            return ret;

        }

        //Отчет: Справка по отключениям подачи коммунальных услуг по домам с указанием виновника
        public Returns GetSpravPoOtklUslugDomVinovnik(Prm prm, int nzp_user, ref string fileName)
        {
            ////пример использования загрузки шаблонов
            //ExcelLoader el = new ExcelLoader();
            //el.LoadTemlate(@"D:\work_Oleg\KOMPLAT.50\WEB\WebKomplat5\App_Data\t1.xls", new Dictionary<string, string>() { { "Data1", "I" }, { "Data2", "can" }, { "Data3", "do" }, { "Data4", "it!" } });
            //el.DeleteObject();            

            Returns ret = Utils.InitReturns();
            ExcelLoader ExcelL = null;



            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetDT_SpravkaPoOtklKomUslVinovnik(prm, 2);
            ExR.Close();


            if (DT == null)
            {
                MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                ret.result = false;
                ret.text = "Создание DataTable : ОШИБКА!";
                return ret;
            }

            #region Устанавливаем разделитель '.'
            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            #endregion



            Microsoft.Office.Interop.Excel.Range excells1;
            #region Заполнение шапки
            Dictionary<string, string> Dic = new Dictionary<string, string>();
            Dic.Add("ERCName", SetReportHeader());

            Dic.Add("month", Utils.GetMonthName(prm.month_));//месяц
            Dic.Add("year", prm.year_.ToString());//год
            Dic.Add("DATE", DateTime.Now.ToShortDateString());//год
            #endregion


            ExcelL = new ExcelLoader();
            ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("sprav_otkl_vinovn_dom.xls"), Dic);
            ExcelFormater exform = new ExcelFormater();
            excells1 = ExcelL.ExlWs.get_Range("D3", "L4");
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = SetReportConditional(nzp_user);
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
            excells1.Font.Size = 8;
            excells1.RowHeight = 23;


            //формирование Excel файла                 
            try
            {


                #region Пишем тело

                int ExcelRow = 7;
                int i = 0;
                int count_no = 0;
                decimal sum_nedop = 0;
                decimal sum_nedop_all = 0;
                string old_supp = "";
                string old_serv = "";
                decimal sum_nedop_serv = 0;
                while (i < DT.Rows.Count)
                {
                    if (DT.Rows[i][0].ToString().Trim() != old_supp)
                    {
                        if (i > 0)
                        {
                            ExcelL.ExlWs.Cells[ExcelRow, 3] = "Итого по услуге";
                            ExcelL.ExlWs.Cells[ExcelRow, 11] = sum_nedop_serv;
                            ExcelRow++;

                            ExcelL.ExlWs.Cells[ExcelRow, 2] = "Итого по поставщику";
                            ExcelL.ExlWs.Cells[ExcelRow, 11] = sum_nedop;
                            ExcelRow++;
                        }
                        sum_nedop = 0;
                        sum_nedop_serv = 0;
                        old_supp = DT.Rows[i]["name_supp"].ToString().Trim();
                        old_serv = DT.Rows[i]["service"].ToString().Trim();
                        count_no++;
                        ExcelL.ExlWs.Cells[ExcelRow, 1] = count_no;
                        ExcelL.ExlWs.Cells[ExcelRow, 2] = DT.Rows[i]["name_supp"].ToString().Trim();
                    }
                    if ((DT.Rows[i]["service"].ToString().Trim() != old_serv) & (i > 1))
                    {
                        ExcelL.ExlWs.Cells[ExcelRow, 2] = "Итого по услуге";
                        ExcelL.ExlWs.Cells[ExcelRow, 11] = sum_nedop;
                        ExcelRow++;
                        sum_nedop_serv = 0;
                        old_serv = DT.Rows[i]["service"].ToString().Trim();
                    }
                    // ExcelL.ExlWs.Cells[ExcelRow, 2] = DT.Rows[i]["name_supp"].ToString().Trim();
                    ExcelL.ExlWs.Cells[ExcelRow, 3] = DT.Rows[i]["service"].ToString().Trim();
                    ExcelL.ExlWs.Cells[ExcelRow, 4] = DT.Rows[i]["stat_calc"].ToString().Trim();
                    ExcelL.ExlWs.Cells[ExcelRow, 5] = DT.Rows[i]["geu"].ToString().Trim();
                    ExcelL.ExlWs.Cells[ExcelRow, 6] = DT.Rows[i]["ulica"].ToString().Trim() + " " +
                            DT.Rows[i]["ndom"].ToString().Trim() + " " +
                            DT.Rows[i]["nkor"].ToString().Trim().Replace("-", "");
                    ExcelL.ExlWs.Cells[ExcelRow, 7] = DT.Rows[i]["count_kvar"].ToString().Trim();
                    ExcelL.ExlWs.Cells[ExcelRow, 8] = Decimal.Parse(DT.Rows[i]["count_daynedo"].ToString().Trim());
                    ExcelL.ExlWs.Cells[ExcelRow, 9] = 0;
                    ExcelL.ExlWs.Cells[ExcelRow, 10] = DT.Rows[i]["count_gil"].ToString().Trim();
                    ExcelL.ExlWs.Cells[ExcelRow, 11] = Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString().Trim());

                    sum_nedop = sum_nedop + Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString().Trim());
                    sum_nedop_serv = sum_nedop_serv + Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString().Trim());
                    sum_nedop_all = sum_nedop_all + Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString().Trim());

                    ExcelRow++;
                    i++;
                }
                if (i > 0)
                {
                    ExcelL.ExlWs.Cells[ExcelRow, 3] = "Итого по услуге";
                    ExcelL.ExlWs.Cells[ExcelRow, 11] = sum_nedop_serv;
                    ExcelRow++;

                    ExcelL.ExlWs.Cells[ExcelRow, 2] = "Итого по поставщику";
                    ExcelL.ExlWs.Cells[ExcelRow, 11] = sum_nedop;
                    ExcelRow++;
                    ExcelL.ExlWs.Cells[ExcelRow, 2] = "ИТОГО:";
                    ExcelL.ExlWs.Cells[ExcelRow, 11] = sum_nedop_all;

                }



                #endregion



                excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "K" + ExcelRow.ToString());
                excells1.Font.Bold = true;

                excells1 = ExcelL.ExlWs.get_Range("A7", "K" + ExcelRow.ToString());
                excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                excells1 = ExcelL.ExlWs.get_Range("G7", "J" + ExcelRow.ToString());
                excells1.NumberFormat = 0;

                SetReportSing(ExcelL, ExcelRow + 3, false);

                #region Сохраняем файл
                try
                {

                    fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SpravPoOtklUslDomVinovnik" +
                        prm.year_.ToString() + "_" +
                        prm.month_.ToString() + "_" + DateTime.Now.GetHashCode() + nzp_user) + ".xls";
                    ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                    ExcelL.ExlWb.Close(false, Type.Missing, Type.Missing);
                    ExcelL.ExlWb = null;
                    if (InputOutput.useFtp)
                    {
                        fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                }

                #endregion

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Формирование Excel ошибка :" + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;
            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }



            return ret;

        }


        //Отчет: Справка по отключениям подачи коммунальных услуг по ЖЭУ с указанием виновника
        public Returns GetSpravPoOtklUslugGeuVinovnik(Prm prm, int nzp_user, ref string fileName)
        {
            ////пример использования загрузки шаблонов
            //ExcelLoader el = new ExcelLoader();
            //el.LoadTemlate(@"D:\work_Oleg\KOMPLAT.50\WEB\WebKomplat5\App_Data\t1.xls", new Dictionary<string, string>() { { "Data1", "I" }, { "Data2", "can" }, { "Data3", "do" }, { "Data4", "it!" } });
            //el.DeleteObject();            

            Returns ret = Utils.InitReturns();
            ExcelLoader ExcelL = null;



            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetDT_SpravkaPoOtklKomUslVinovnik(prm, 3);
            ExR.Close();


            if (DT == null)
            {
                MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                ret.result = false;
                ret.text = "Создание DataTable : ОШИБКА!";
                return ret;
            }

            #region Устанавливаем разделитель '.'
            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            #endregion



            Microsoft.Office.Interop.Excel.Range excells1;
            #region Заполнение шапки
            Dictionary<string, string> Dic = new Dictionary<string, string>();
            Dic.Add("ERCName", "");

            Dic.Add("month", Utils.GetMonthName(prm.month_));//месяц
            Dic.Add("year", prm.year_.ToString());//год
            Dic.Add("DATE", DateTime.Now.ToShortDateString());//год
            #endregion


            ExcelL = new ExcelLoader();
            ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("sprav_otkl_vinovn_geu.xls"), Dic);
            ExcelFormater exform = new ExcelFormater();
            excells1 = ExcelL.ExlWs.get_Range("D3", "L4");
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = SetReportConditional(nzp_user);
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
            excells1.Font.Size = 8;
            excells1.RowHeight = 23;


            //формирование Excel файла                 
            try
            {


                #region Пишем тело

                int ExcelRow = 7;
                int i = 0;
                int count_no = 0;
                decimal sum_nedop = 0;
                decimal sum_nedop_all = 0;
                string old_supp = "";
                string old_vinovnik = "";
                decimal sum_nedop_vinovnik = 0;
                while (i < DT.Rows.Count)
                {
                    if (DT.Rows[i][0].ToString().Trim() != old_supp)
                    {
                        if (i > 0)
                        {
                            ExcelL.ExlWs.Cells[ExcelRow, 3] = "Итого по виновнику";
                            ExcelL.ExlWs.Cells[ExcelRow, 11] = sum_nedop_vinovnik;
                            ExcelRow++;

                            ExcelL.ExlWs.Cells[ExcelRow, 2] = "Итого по поставщику";
                            ExcelL.ExlWs.Cells[ExcelRow, 11] = sum_nedop;
                            ExcelRow++;
                        }
                        sum_nedop = 0;
                        sum_nedop_vinovnik = 0;
                        old_supp = DT.Rows[i]["name_supp"].ToString().Trim();
                        old_vinovnik = DT.Rows[i]["vinovnik"].ToString().Trim();
                        count_no++;
                        ExcelL.ExlWs.Cells[ExcelRow, 1] = count_no;
                        ExcelL.ExlWs.Cells[ExcelRow, 2] = DT.Rows[i]["name_supp"].ToString().Trim();
                    }
                    if ((DT.Rows[i]["vinovnik"].ToString().Trim() != old_vinovnik) & (i > 1))
                    {
                        ExcelL.ExlWs.Cells[ExcelRow, 2] = "Итого по виновнику";
                        ExcelL.ExlWs.Cells[ExcelRow, 11] = sum_nedop;
                        ExcelRow++;
                        sum_nedop_vinovnik = 0;
                        old_vinovnik = DT.Rows[i]["vinovnik"].ToString().Trim();
                    }
                    // ExcelL.ExlWs.Cells[ExcelRow, 2] = DT.Rows[i]["name_supp"].ToString().Trim();
                    ExcelL.ExlWs.Cells[ExcelRow, 3] = DT.Rows[i]["vinovnik"].ToString().Trim();
                    ExcelL.ExlWs.Cells[ExcelRow, 4] = DT.Rows[i]["service"].ToString().Trim();
                    ExcelL.ExlWs.Cells[ExcelRow, 5] = DT.Rows[i]["stat_calc"].ToString().Trim();
                    ExcelL.ExlWs.Cells[ExcelRow, 6] = DT.Rows[i]["geu"].ToString().Trim();
                    ExcelL.ExlWs.Cells[ExcelRow, 7] = DT.Rows[i]["count_kvar"].ToString().Trim();
                    ExcelL.ExlWs.Cells[ExcelRow, 8] = Decimal.Parse(DT.Rows[i]["count_daynedo"].ToString().Trim());
                    ExcelL.ExlWs.Cells[ExcelRow, 9] = 0;
                    ExcelL.ExlWs.Cells[ExcelRow, 10] = DT.Rows[i]["count_gil"].ToString().Trim();
                    ExcelL.ExlWs.Cells[ExcelRow, 11] = Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString().Trim());

                    sum_nedop = sum_nedop + Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString().Trim());
                    sum_nedop_vinovnik = sum_nedop_vinovnik + Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString().Trim());
                    sum_nedop_all = sum_nedop_all + Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString().Trim());

                    ExcelRow++;
                    i++;
                }
                if (i > 0)
                {
                    ExcelL.ExlWs.Cells[ExcelRow, 3] = "Итого по виновнику";
                    ExcelL.ExlWs.Cells[ExcelRow, 11] = sum_nedop_vinovnik;
                    ExcelRow++;

                    ExcelL.ExlWs.Cells[ExcelRow, 2] = "Итого по поставщику";
                    ExcelL.ExlWs.Cells[ExcelRow, 11] = sum_nedop;
                    ExcelRow++;
                    ExcelL.ExlWs.Cells[ExcelRow, 2] = "ИТОГО:";
                    ExcelL.ExlWs.Cells[ExcelRow, 11] = sum_nedop_all;

                }



                #endregion



                excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "K" + ExcelRow.ToString());
                excells1.Font.Bold = true;

                excells1 = ExcelL.ExlWs.get_Range("A7", "K" + ExcelRow.ToString());
                excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                excells1 = ExcelL.ExlWs.get_Range("G7", "J" + ExcelRow.ToString());
                excells1.NumberFormat = 0;

                SetReportSing(ExcelL, ExcelRow + 3, false);

                #region Сохраняем файл
                try
                {

                    fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SpravPoOtklUslGeuVinovnik" +
                        prm.year_.ToString() + "_" +
                        prm.month_.ToString() + "_" + nzp_user) + ".xls";
                    ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                    ExcelL.ExlWb.Close();
                    if (InputOutput.useFtp)
                    {
                        fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                }

                #endregion

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Формирование Excel ошибка :" + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;
            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }



            return ret;

        }



        //Отчет: Справка по отключениям подачи коммунальных услуг по домам
        public Returns GetSpravPoOtklUslugDom(Prm prm, int nzp_user, ref string fileName)
        {
            ////пример использования загрузки шаблонов
            //ExcelLoader el = new ExcelLoader();
            //el.LoadTemlate(@"D:\work_Oleg\KOMPLAT.50\WEB\WebKomplat5\App_Data\t1.xls", new Dictionary<string, string>() { { "Data1", "I" }, { "Data2", "can" }, { "Data3", "do" }, { "Data4", "it!" } });
            //el.DeleteObject();            

            Returns ret = Utils.InitReturns();
            ExcelLoader ExcelL = null;



            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetDT_SpravkaPoOtklKomUslDom(new Ls { nzp_user = nzp_user }, prm.nzp_serv, prm.month_, prm.year_);
            ExR.Close();


            if (DT == null)
            {
                MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                ret.result = false;
                ret.text = "Создание DataTable : ОШИБКА!";
                return ret;
            }

            #region Устанавливаем разделитель '.'
            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            #endregion

            #region Вставка строк Итого

            try
            {
                if (DT.Rows.Count > 0)
                {
                    //новая таблица (содержит строки ИТОГО)
                    System.Data.DataTable DT_2 = DT.Clone();

                    DataRow temp = DT.Rows[0];
                    decimal sum = 0;
                    decimal global_sum = 0;
                    Int64 sum_kvar_dney = 0;
                    Int64 global_sum_kvar_dney = 0;

                    foreach (DataRow r in DT.Rows)
                    {
                        if (r["name_supp"].ToString().Trim() == temp["name_supp"].ToString().Trim())
                        {
                            sum += Convert.ToDecimal(r["sum_nedop"]);
                            sum_kvar_dney += Convert.ToInt64(r["count_daynedo"]);
                        }
                        //добавляем строку итого
                        else
                        {
                            DT_2.Rows.Add("итого по поставщику:", "", "", "", null, sum_kvar_dney, null, null, sum);


                            global_sum += sum;
                            global_sum_kvar_dney += sum_kvar_dney;

                            sum = Convert.ToDecimal(r["sum_nedop"]);
                            sum_kvar_dney = Convert.ToInt64(r["count_daynedo"]);
                        }

                        DT_2.ImportRow(r);

                        temp = r;

                        //(особая ситуация)если последняя строка то добавляем итого
                        if (DT.Rows.IndexOf(r) == (DT.Rows.Count - 1))
                        {
                            global_sum += sum;
                            global_sum_kvar_dney += sum_kvar_dney;

                            DT_2.Rows.Add("Итого по поставщику:", "", "", "", null, sum_kvar_dney, null, null, sum);
                        }
                    }

                    //добавление глобального ИТОГО
                    DT_2.Rows.Add("ИТОГО:", "", "", "", null, global_sum_kvar_dney, null, null, global_sum);


                    //Удаление одинаковых поставщиков услуг
                    temp = DT_2.Rows[0];
                    for (int i = 1; i < DT_2.Rows.Count; i++)
                    {
                        if (DT_2.Rows[i]["name_supp"].ToString().Trim() == temp["name_supp"].ToString().Trim())
                        {
                            DT_2.Rows[i]["name_supp"] = "";
                        }
                        else
                        {
                            temp = DT_2.Rows[i];
                        }
                    }

                    //получение ссылки на новую таблицу
                    DT = DT_2;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка вставки строк итого GetSpravPoOtklUslugDom : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = ex.Message;
                return ret;
            }
            #endregion


            //формирование Excel файла                 
            try
            {


                #region Пишем тело

                int ExcelRow = 8;
                int i = 0;

                while (i < DT.Rows.Count)
                {
                    if (DT.Rows[i][0].ToString().Trim() != "")
                    {
                        i++;
                        ExcelL.ExlWs.Cells[ExcelRow, 1] = i;
                    }
                    ExcelL.ExlWs.Cells[ExcelRow, 2] = DT.Rows[i][0].ToString().Trim();
                    ExcelL.ExlWs.Cells[ExcelRow, 3] = DT.Rows[i][1].ToString().Trim();
                    ExcelL.ExlWs.Cells[ExcelRow, 4] = "";
                    ExcelL.ExlWs.Cells[ExcelRow, 5] = DT.Rows[i]["geu"].ToString().Trim();
                    ExcelL.ExlWs.Cells[ExcelRow, 6] = " ул." +
                        DT.Rows[i]["ulica"].ToString().Trim() +
                        DT.Rows[i]["ndom"].ToString().Trim() +
                        DT.Rows[i]["nkor"].ToString().Trim();
                    ExcelL.ExlWs.Cells[ExcelRow, 7] = DT.Rows[i]["count_kvar"].ToString().Trim();
                    ExcelL.ExlWs.Cells[ExcelRow, 8] = DT.Rows[i]["count_daynedo"].ToString().Trim();
                    ExcelL.ExlWs.Cells[ExcelRow, 9] = 0;
                    ExcelL.ExlWs.Cells[ExcelRow, 10] = DT.Rows[i]["count_gil"].ToString().Trim();
                    ExcelL.ExlWs.Cells[ExcelRow, 11] = DT.Rows[i]["sum_nedop"].ToString().Trim();

                    //sum_nedop = sum_nedop + Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString().Trim());

                    ExcelRow++;
                    i++;
                }
                #endregion

                //ExcelL.ExlWs.Cells[ExcelRow, 1] = "ВСЕГО";
                Microsoft.Office.Interop.Excel.Range excells1;

                excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "K" + ExcelRow.ToString());
                excells1.Font.Bold = true;

                excells1 = ExcelL.ExlWs.get_Range("A8", "K" + ExcelRow.ToString());
                excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                excells1 = ExcelL.ExlWs.get_Range("B8", "B" + ExcelRow.ToString());
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                //excells1 = ExcelL.ExlWs.get_Range("D8", "D" + ExcelRow.ToString());
                //excells1.NumberFormat = "0";


                SetReportSing(ExcelL, ExcelRow + 3, false);


                //#endregion

                #region Сохраняем файл
                try
                {

                    fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SpravGroupSoderg" +
                        prm.year_.ToString() + "_" +
                        prm.month_.ToString() + "_" + nzp_user) + ".xls";
                    ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                    if (InputOutput.useFtp)
                    {
                        fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                    }

                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                }

                #endregion

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Формирование Excel ошибка :" + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;
            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }



            return ret;

        }




        //Отчет: Справка по отключениям подачи коммунальных услуг
        public Returns GetSpravPoOtklUslug(int nzp_user, int nzp_serv, int m, int y, ref string fileName)
        {
            ////пример использования загрузки шаблонов
            //ExcelLoader el = new ExcelLoader();
            //el.LoadTemlate(@"D:\work_Oleg\KOMPLAT.50\WEB\WebKomplat5\App_Data\t1.xls", new Dictionary<string, string>() { { "Data1", "I" }, { "Data2", "can" }, { "Data3", "do" }, { "Data4", "it!" } });
            //el.DeleteObject();            

            Returns ret = Utils.InitReturns();
            ExcelLoader ExcelL = null;



            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            //System.Data.DataTable DT = ExR.GetDT_SpravkaPoOtklKomUsl(new Ls { nzp_user = nzp_user },nzp_serv ,m, y);
            Prm prm = new Prm();
            prm.nzp_user = nzp_user;
            prm.month_ = m;
            prm.year_ = y;
            prm.nzp_serv = nzp_serv;
            prm.nzp_key = -1;

            System.Data.DataTable DT = ExR.GetDT_SpravkaPoOtklKomUslVinovnik(prm, 4);
            ExR.Close();

            /////временно проверить
            //ExcelRep ER = new ExcelRep();
            //System.Data.DataTable dt = ER.GetInfPoRaschetNasel(new Ls() { nzp_user = nzp_user }, m, y);
            //ER.Close();

            if (DT == null)
            {
                MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                ret.result = false;
                ret.text = "Создание DataTable : ОШИБКА!";
                return ret;
            }

            #region Устанавливаем разделитель '.'
            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            #endregion

            #region Вставка строк Итого

            try
            {
                if (DT.Rows.Count > 0)
                {
                    //новая таблица (содержит строки ИТОГО)
                    System.Data.DataTable DT_2 = DT.Clone();

                    DataRow temp = DT.Rows[0];
                    decimal sum = 0;
                    decimal global_sum = 0;
                    Int64 sum_kvar_dney = 0;
                    Int64 global_sum_kvar_dney = 0;

                    foreach (DataRow r in DT.Rows)
                    {
                        if (r["name_supp"].ToString().Trim() == temp["name_supp"].ToString().Trim())
                        {
                            sum += Convert.ToDecimal(r["sum_nedop"]);
                            sum_kvar_dney += Convert.ToInt64(r["count_daynedo"]);
                        }
                        //добавляем строку итого
                        else
                        {
                            DT_2.Rows.Add("итого по поставщику:", "", "", "", "", null, sum_kvar_dney, null, null, sum);


                            global_sum += sum;
                            global_sum_kvar_dney += sum_kvar_dney;

                            sum = Convert.ToDecimal(r["sum_nedop"]);
                            sum_kvar_dney = Convert.ToInt64(r["count_daynedo"]);
                        }

                        DT_2.ImportRow(r);

                        temp = r;

                        //(особая ситуация)если последняя строка то добавляем итого
                        if (DT.Rows.IndexOf(r) == (DT.Rows.Count - 1))
                        {
                            global_sum += sum;
                            global_sum_kvar_dney += sum_kvar_dney;

                            DT_2.Rows.Add("Итого по поставщику:", "", "", "", "", null, sum_kvar_dney, null, null, sum);
                        }
                    }

                    //добавление глобального ИТОГО
                    DT_2.Rows.Add("ИТОГО:", "", "", "", "", null, global_sum_kvar_dney, null, null, global_sum);


                    //Удаление одинаковых поставщиков услуг
                    temp = DT_2.Rows[0];
                    for (int i = 1; i < DT_2.Rows.Count; i++)
                    {
                        if (DT_2.Rows[i]["name_supp"].ToString().Trim() == temp["name_supp"].ToString().Trim())
                        {
                            DT_2.Rows[i]["name_supp"] = "";
                        }
                        else
                        {
                            temp = DT_2.Rows[i];
                        }
                    }

                    //получение ссылки на новую таблицу
                    DT = DT_2;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка вставки строк итого GetSpravPoOtklUslug : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = ex.Message;
                return ret;
            }
            #endregion

            //создаем массив заголовков
            string[] HeaderData = new string[] { "Поставщики к/услуг", "Виды услуг", "ЖЭУ", "Статья калькуляции","Адрес" ,"Кол.Квар", 
                                                 "Кол.квар/дней", "Кол.квар/часов", "Кол. жил.", "Сумма возврата" };
            //создаем массив форматов
            string[] TypeHeader = new string[] { "char", "char", "char", "char" ,"char" ,"int",
                                                 "int", "int", "int", "float"};
            //футер
            string[] footer_data = null;

            //формирование Excel файла                 
            try
            {
                #region Создание Excel документа

                ExcelL = new ExcelLoader();
                ExcelL.ExlWs.Rows.Font.Name = "Arial";
                //альбомная ориентация
                ExcelL.ExlWs.PageSetup.Orientation = XlPageOrientation.xlLandscape;

                ExcelCreater ExcelCr = new ExcelCreater();

                //создаем название отчета
                //ret = ExcelCr.MakeName("Справка по отключениям подачи коммунальных услуг...\n123", "A1", DT.Columns.Count, 2, ref ExcelL.ExlWs);
                //string ERC_name = "";

                //if (STCLINE.KP50.Interfaces.Points.IsDemo == true)
                //    ERC_name = "Н-ска";
                //else
                //    ERC_name = "Самары";
                string s_name = SetReportHeader();

                //string name = "Справка по отключениям подачи коммунальных услуг \n" +
                //              "по МП городского округа " + ERC_name + " \"ЕИРЦ\" за " + ExcelFormater.months[m] + " " + y + "г.";

                string name = "Справка по отключениям подачи коммунальных услуг \n" +
                              s_name + " за " + ExcelFormater.months[m] + " " + y + "г.";


                ret = ExcelCr.MakeName(name, "A1", 6, 4, true, Microsoft.Office.Interop.Excel.Constants.xlLeft, Microsoft.Office.Interop.Excel.Constants.xlCenter, 12, ref ExcelL.ExlWs);

                ret = ExcelCr.MakeName("Дата печати: " + DateTime.Now.ToShortDateString().Replace('.', '/'), "A5", 3, 2, false, Microsoft.Office.Interop.Excel.Constants.xlLeft, Microsoft.Office.Interop.Excel.Constants.xlCenter, 12, ref ExcelL.ExlWs);

                //критерий поиска
                string strCondition = SetReportConditional(nzp_user);
                if (strCondition != "")
                {

                }


                //Создаем шапку
                ret = ExcelCr.MakeHeader(HeaderData, 4, 0, ref ExcelL.ExlWs);


                //Создаем тело               
                if (ret.result)
                {
                    ret = ExcelCr.MakeBody(DT, 5, 0, ref ExcelL.ExlWs);
                }

                //Создаем подвал                
                if (footer_data != null && ret.result)
                {
                    ret = ExcelCr.MakeFooter(footer_data, DT.Rows.Count + 1, 0, ref ExcelL.ExlWs);
                }

                #region Шрифт
                //шрифт
                ExcelFormater efotmat = new ExcelFormater();
                Microsoft.Office.Interop.Excel.Range font_range = ExcelL.ExlWs.get_Range("A8", efotmat.BukvaList[DT.Columns.Count - 1] + (DT.Rows.Count + 7));
                font_range.Font.Size = 10;
                #endregion
                //стандартный формат
                if (ret.result)
                {
                    ret = ExcelL.ApplyStandartFormat(DT.Rows.Count, 8, HeaderData.Length, TypeHeader);
                }

                //выравнивание по левому краю
                Microsoft.Office.Interop.Excel.Range allign_range = ExcelL.ExlWs.get_Range("A1", "B" + (DT.Rows.Count + 7));
                allign_range.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                #endregion

                #region Добваление жирного шрифта к ИТОГО
                if (DT.Rows.Count > 0)
                {
                    ExcelFormater eformat = new ExcelFormater();

                    for (int i = 1; i <= DT.Rows.Count + 8; i++)
                    {
                        if (ExcelL.ExlWs.get_Range("A" + i, Type.Missing).Value2 != null)
                        {
                            if (ExcelL.ExlWs.get_Range("A" + i, Type.Missing).Value2.ToString().ToUpper().Contains("ИТОГО"))
                            {
                                Microsoft.Office.Interop.Excel.Range excBold = ExcelL.ExlWs.get_Range("A" + i, eformat.BukvaList[DT.Columns.Count - 1] + i);
                                excBold.Font.Bold = true;
                            }
                        }
                    }
                }
                #endregion

                #region Надпись "Нет данных" если данных нет)
                if (DT.Rows.Count == 0)
                {
                    ExcelCr.MakeName("Нет данных", "A9", DT.Columns.Count, 9, ref ExcelL.ExlWs);
                }
                #endregion

                if (ret.result)
                {
                    SetReportSing(ExcelL, DT.Rows.Count + 21, false);
                    //имя файла                                                         
                    fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SpravOtkl" + "_" + nzp_user) + ".xls";

                    //Сохранение
                    ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                    ExcelL.ExlWb.Close();
                    if (InputOutput.useFtp)
                    {
                        fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                    }
                }
                else
                {
                    MonitorLog.WriteLog("Ошибка создания Excel файла : " + ret.text, MonitorLog.typelog.Error, true);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Формирование Excel ошибка :" + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;
            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }



            return ret;

        }


        public Returns GetSpravGroupSodergGil(Prm prm_, string Nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetGroupSprav(prm_, out ret, Nzp_user);

            ExcelLoader ExcelL = null;
            try
            {
                ExcelL = new ExcelLoader();

                if (DT != null)
                {

                    ExcelCreater ExcelCr = new ExcelCreater();
                    ExcelL.ExlWs.Rows.Font.Name = "Arial";

                    #region Создаем шапку
                    Microsoft.Office.Interop.Excel.Range excells1;

                    Dictionary<string, string> Dic = new Dictionary<string, string>();
                    Dic.Add("ERCName", SetReportHeader());

                    Dic.Add("month", Utils.GetMonthName(prm_.month_));//месяц
                    Dic.Add("year", prm_.year_.ToString());//год
                    Dic.Add("DATE", DateTime.Now.ToShortDateString());//год
                    #endregion



                    ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("sprav_group.xls"), Dic);
                    ExcelFormater exform = new ExcelFormater();
                    excells1 = ExcelL.ExlWs.get_Range("D3", "L4");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = SetReportConditional(System.Convert.ToInt32(Nzp_user));
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.Font.Size = 5;
                    excells1.RowHeight = 23;

                    #region Пишем тело

                    int ExcelRow = 8;
                    int i = 0;
                    decimal[] itoMas = new decimal[11];
                    for (int j = 0; j < 11; j++) itoMas[j] = 0;


                    while (i < DT.Rows.Count)
                    {

                        ExcelL.ExlWs.Cells[ExcelRow, 1] = DT.Rows[i][0].ToString().Trim();
                        for (int j = 1; j < 11; j++)
                        {
                            ExcelL.ExlWs.Cells[ExcelRow, j + 1] = Decimal.Parse(DT.Rows[i][j].ToString().Trim());
                            itoMas[j] = itoMas[j] + Decimal.Parse(DT.Rows[i][j].ToString().Trim());
                        }
                        ExcelRow++;
                        i++;
                    }
                    #endregion

                    ExcelL.ExlWs.Cells[ExcelRow, 1] = "ВСЕГО";
                    for (int j = 1; j < 11; j++) ExcelL.ExlWs.Cells[ExcelRow, j + 1] = itoMas[j];

                    excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "K" + ExcelRow.ToString());
                    excells1.Font.Bold = true;

                    excells1 = ExcelL.ExlWs.get_Range("A8", "K" + ExcelRow.ToString());
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                    excells1 = ExcelL.ExlWs.get_Range("A8", "A" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                    excells1 = ExcelL.ExlWs.get_Range("D8", "D" + ExcelRow.ToString());
                    excells1.NumberFormat = "0";



                    SetReportSing(ExcelL, ExcelRow + 3, false);
                    for (int c = ExcelRow + 3; c < ExcelRow + 3 + 10; c++)
                    {
                        excells1 = ExcelL.ExlWs.get_Range("A" + c.ToString(), "H" + c.ToString());
                        excells1.Font.Size = 5;
                    }

                    //#endregion

                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SpravGroupSoderg" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString() + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }

                    #endregion

                }

                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravGroupSodergGil DataTable : ОШИБКА!";

                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SpravGroupSoderg" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }

                    //удаление объекта
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                try
                {
                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SpravGroupSoderg" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет GetSpravGroupSodergGil", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravGroupSodergGil Excel File : ОШИБКА!";
                }

            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }

            return ret;

        }




        public Returns GetSpravSoderg2Kan(Prm prm_, string Nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetSpravSoderg2Kan(prm_, out ret, Nzp_user);

            ExcelLoader ExcelL = null;
            try
            {
                if (DT != null)
                {
                    ExcelL = new ExcelLoader();
                    ExcelCreater ExcelCr = new ExcelCreater();

                    Microsoft.Office.Interop.Excel.Range excells1;

                    Dictionary<string, string> Dic = new Dictionary<string, string>();
                    Dic.Add("ERCName", SetReportHeader());

                    #region Пишем заголовок
                    Dic.Add("month", Utils.GetMonthName(prm_.month_));//месяц
                    Dic.Add("year", prm_.year_.ToString());//год
                    Dic.Add("DATE", DateTime.Now.ToShortDateString());//год
                    if (prm_.has_pu == "1")
                    {
                        Dic.Add("ODPU", "(все дома)");
                    }
                    else
                        if (prm_.has_pu == "2")
                        {
                            Dic.Add("ODPU", "(дома с ОДПУ)");
                        }
                        else
                        {
                            Dic.Add("ODPU", "(дома без ОДПУ)");
                        }

                    if (prm_.nzp_key > -1)
                        Dic.Add("NAMESUPP", prm_.name_prm);
                    else
                        Dic.Add("NAMESUPP", "Все поставщики");


                    Returns ret_cond = Utils.InitReturns();
                    ExcelRep ExR_cond = new ExcelRep();
                    System.Data.DataTable DT_cond = ExR.GetFindTemplate(out ret, Nzp_user);
                    //string s_res = "";
                    string nameUk = "";

                    if (DT_cond != null)
                    {
                        for (int k = 0; k < DT_cond.Rows.Count; k++)
                        {

                            if (DT_cond.Rows[k]["name"].ToString().Trim() == "Управляющая организация")
                            {
                                nameUk = DT_cond.Rows[k]["value"].ToString().Trim();
                            }

                        }
                    }
                    Dic.Add("NAMEUK", nameUk);
                    #endregion


                    ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("sprav_soderg2_kan.xls"), Dic);





                    ExcelFormater exform = new ExcelFormater();





                    #region Пишем тело

                    int ExcelRow = 13;
                    int i = 0;
                    decimal[] itogString = new decimal[13];
                    for (int j = 1; j < 13; j++) itogString[j] = 0;

                    while (i < DT.Rows.Count)
                    {
                        excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "A" + ExcelRow.ToString());
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = (i + 1);

                        excells1 = ExcelL.ExlWs.get_Range("B" + ExcelRow.ToString(), "B" + ExcelRow.ToString());
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = DT.Rows[i]["nzp_dom"];

                        excells1 = ExcelL.ExlWs.get_Range("C" + ExcelRow.ToString(), "C" + ExcelRow.ToString());
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = DT.Rows[i]["ulica"].ToString().Trim();

                        excells1 = ExcelL.ExlWs.get_Range("D" + ExcelRow.ToString(), "D" + ExcelRow.ToString());
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = DT.Rows[i]["ndom"].ToString().Trim();
                        ;

                        excells1 = ExcelL.ExlWs.get_Range("E" + ExcelRow.ToString(), "E" + ExcelRow.ToString());
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excells1.FormulaR1C1 = DT.Rows[i]["nkor"].ToString().Replace("-", "");

                        excells1 = ExcelL.ExlWs.get_Range("F" + ExcelRow.ToString(), "F" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["count_gil_ipu"];

                        excells1 = ExcelL.ExlWs.get_Range("G" + ExcelRow.ToString(), "G" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["count_gil_npu"];

                        excells1 = ExcelL.ExlWs.get_Range("H" + ExcelRow.ToString(), "H" + ExcelRow.ToString());
                        if (DT.Rows[i]["volume_all_kub"].ToString() != "0.00")
                            excells1.FormulaR1C1 = DT.Rows[i]["volume_all_kub"];

                        excells1 = ExcelL.ExlWs.get_Range("I" + ExcelRow.ToString(), "I" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["vol_ipu_kub"];

                        excells1 = ExcelL.ExlWs.get_Range("J" + ExcelRow.ToString(), "J" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["vol_npu_kub"];

                        excells1 = ExcelL.ExlWs.get_Range("K" + ExcelRow.ToString(), "K" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["rsum_tarif"];

                        excells1 = ExcelL.ExlWs.get_Range("L" + ExcelRow.ToString(), "L" + ExcelRow.ToString());
                        if (DT.Rows[i]["vozv_kub"].ToString() != "0.00")
                            excells1.FormulaR1C1 = DT.Rows[i]["vozv_kub"];

                        excells1 = ExcelL.ExlWs.get_Range("M" + ExcelRow.ToString(), "M" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["sum_nedop"];

                        excells1 = ExcelL.ExlWs.get_Range("N" + ExcelRow.ToString(), "N" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["reval_kub"];

                        excells1 = ExcelL.ExlWs.get_Range("O" + ExcelRow.ToString(), "O" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["reval"];

                        excells1 = ExcelL.ExlWs.get_Range("P" + ExcelRow.ToString(), "P" + ExcelRow.ToString());
                        if (DT.Rows[i]["vol_charge_kub"].ToString() != "0.00")
                            excells1.FormulaR1C1 = DT.Rows[i]["vol_charge_kub"];

                        excells1 = ExcelL.ExlWs.get_Range("Q" + ExcelRow.ToString(), "Q" + ExcelRow.ToString());
                        excells1.NumberFormat = "# ##0,00";
                        excells1.FormulaR1C1 = DT.Rows[i]["sum_charge"];




                        if (DT.Rows[i]["count_gil_ipu"] != DBNull.Value)
                            itogString[1] = itogString[1] + System.Convert.ToDecimal(DT.Rows[i]["count_gil_ipu"]);
                        if (DT.Rows[i]["count_gil_npu"] != DBNull.Value)
                            itogString[2] = itogString[2] + System.Convert.ToDecimal(DT.Rows[i]["count_gil_npu"]);
                        if (DT.Rows[i]["volume_all_kub"] != DBNull.Value)
                            itogString[3] = itogString[3] + System.Convert.ToDecimal(DT.Rows[i]["volume_all_kub"]);
                        if (DT.Rows[i]["vol_ipu_kub"] != DBNull.Value)
                            itogString[4] = itogString[4] + System.Convert.ToDecimal(DT.Rows[i]["vol_ipu_kub"]);
                        if (DT.Rows[i]["vol_npu_kub"] != DBNull.Value)
                            itogString[5] = itogString[5] + System.Convert.ToDecimal(DT.Rows[i]["vol_npu_kub"]);
                        if (DT.Rows[i]["rsum_tarif"] != DBNull.Value)
                            itogString[6] = itogString[6] + System.Convert.ToDecimal(DT.Rows[i]["rsum_tarif"]);
                        if (DT.Rows[i]["vozv_kub"] != DBNull.Value)
                            itogString[7] = itogString[7] + System.Convert.ToDecimal(DT.Rows[i]["vozv_kub"]);
                        if (DT.Rows[i]["sum_nedop"] != DBNull.Value)
                            itogString[8] = itogString[8] + System.Convert.ToDecimal(DT.Rows[i]["sum_nedop"]);
                        if (DT.Rows[i]["reval_kub"] != DBNull.Value)
                            itogString[9] = itogString[9] + System.Convert.ToDecimal(DT.Rows[i]["reval_kub"]);
                        if (DT.Rows[i]["reval"] != DBNull.Value)
                            itogString[10] = itogString[10] + System.Convert.ToDecimal(DT.Rows[i]["reval"]);
                        if (DT.Rows[i]["vol_charge_kub"] != DBNull.Value)
                            itogString[11] = itogString[11] + System.Convert.ToDecimal(DT.Rows[i]["vol_charge_kub"]);
                        if (DT.Rows[i]["sum_charge"] != DBNull.Value)
                            itogString[12] = itogString[12] + System.Convert.ToDecimal(DT.Rows[i]["sum_charge"]);



                        i++;
                        ExcelRow++;
                    }

                    excells1 = ExcelL.ExlWs.get_Range("C" + ExcelRow.ToString(), "C" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.FormulaR1C1 = "Итого";

                    for (int j = 1; j < 13; j++)
                        ExcelL.ExlWs.Cells[ExcelRow, j + 5] = itogString[j];


                    excells1 = ExcelL.ExlWs.get_Range("C" + ExcelRow.ToString(), "V" + ExcelRow.ToString());
                    excells1.Font.Bold = true;

                    ExcelRow++;


                    excells1 = ExcelL.ExlWs.get_Range("F13", "F" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1 = ExcelL.ExlWs.get_Range("G13", "G" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1 = ExcelL.ExlWs.get_Range("H13", "Q" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";


                    excells1 = ExcelL.ExlWs.get_Range("A13", "Q" + (ExcelRow - 1).ToString());

                    excells1.Borders[XlBordersIndex.xlEdgeTop].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Borders.Weight = 2;
                    excells1.Borders[XlBordersIndex.xlEdgeRight].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Borders.Weight = 2;
                    excells1.Borders[XlBordersIndex.xlEdgeLeft].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Borders.Weight = 2;
                    excells1.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Borders.Weight = 2;
                    ExcelL.ExlWs.Rows.Font.Name = "Times New Roman";
                    excells1.Font.Size = 11;


                    SetReportSing(ExcelL, ExcelRow + 3, true);


                    #endregion

                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "РСО_Водоотведение_" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString() + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }

                    #endregion

                }

                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSoderg DataTable : ОШИБКА!";

                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "РСО_Водоотведение_" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }

                    //удаление объекта
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                try
                {
                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "РСО_Водоотведение_" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет по РСО Водоотведение", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSoderg2HvWater Excel File : ОШИБКА!";
                }

            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }

            return ret;

        }




        public Returns GetSpravSoderg2HvWater(Prm prm_, string Nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetSpravSoderg2HvWater(prm_, out ret, Nzp_user);

            ExcelLoader ExcelL = null;
            try
            {
                if (DT != null)
                {
                    ExcelL = new ExcelLoader();
                    ExcelCreater ExcelCr = new ExcelCreater();

                    Microsoft.Office.Interop.Excel.Range excells1;

                    Dictionary<string, string> Dic = new Dictionary<string, string>();
                    Dic.Add("ERCName", SetReportHeader());

                    #region Пишем заголовок
                    Dic.Add("month", Utils.GetMonthName(prm_.month_));//месяц
                    Dic.Add("year", prm_.year_.ToString());//год
                    Dic.Add("DATE", DateTime.Now.ToShortDateString());//год
                    if (prm_.has_pu == "1")
                    {
                        Dic.Add("ODPU", "(все дома)");
                    }
                    else
                        if (prm_.has_pu == "2")
                        {
                            Dic.Add("ODPU", "(дома с ОДПУ)");
                        }
                        else
                        {
                            Dic.Add("ODPU", "(дома без ОДПУ)");
                        }

                    if (prm_.nzp_key > -1)
                        Dic.Add("NAMESUPP", prm_.name_prm);
                    else
                        Dic.Add("NAMESUPP", "Все поставщики");


                    Returns ret_cond = Utils.InitReturns();
                    ExcelRep ExR_cond = new ExcelRep();
                    System.Data.DataTable DT_cond = ExR.GetFindTemplate(out ret, Nzp_user);
                    //string s_res = "";
                    string nameUk = "";

                    if (DT_cond != null)
                    {
                        for (int k = 0; k < DT_cond.Rows.Count; k++)
                        {

                            if (DT_cond.Rows[k]["name"].ToString().Trim() == "Управляющая организация")
                            {
                                nameUk = DT_cond.Rows[k]["value"].ToString().Trim();
                            }

                        }
                    }
                    Dic.Add("NAMEUK", nameUk);
                    #endregion


                    if (prm_.has_pu == "3")
                    {
                        ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("sprav_soderg2_hvwater.xls"), Dic);
                    }
                    else
                    {
                        ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("sprav_soderg2_hvwater.xls"), Dic);
                    }




                    ExcelFormater exform = new ExcelFormater();





                    #region Пишем тело

                    int ExcelRow = 13;
                    int i = 0;
                    decimal[] itogString = new decimal[18];
                    for (int j = 1; j < 18; j++) itogString[j] = 0;

                    while (i < DT.Rows.Count)
                    {
                        excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "A" + ExcelRow.ToString());
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = (i + 1);

                        excells1 = ExcelL.ExlWs.get_Range("B" + ExcelRow.ToString(), "B" + ExcelRow.ToString());
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = DT.Rows[i]["nzp_dom"];

                        excells1 = ExcelL.ExlWs.get_Range("C" + ExcelRow.ToString(), "C" + ExcelRow.ToString());
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = DT.Rows[i]["ulica"].ToString().Trim();

                        excells1 = ExcelL.ExlWs.get_Range("D" + ExcelRow.ToString(), "D" + ExcelRow.ToString());
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = DT.Rows[i]["ndom"].ToString().Trim();
                        ;

                        excells1 = ExcelL.ExlWs.get_Range("E" + ExcelRow.ToString(), "E" + ExcelRow.ToString());
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excells1.FormulaR1C1 = DT.Rows[i]["nkor"].ToString().Replace("-", "");

                        excells1 = ExcelL.ExlWs.get_Range("F" + ExcelRow.ToString(), "F" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["count_gil_ipu"];

                        excells1 = ExcelL.ExlWs.get_Range("G" + ExcelRow.ToString(), "G" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["count_gil_npu"];

                        excells1 = ExcelL.ExlWs.get_Range("H" + ExcelRow.ToString(), "H" + ExcelRow.ToString());
                        if (DT.Rows[i]["volume_all_kub"].ToString() != "0.00")
                            excells1.FormulaR1C1 = DT.Rows[i]["volume_all_kub"];

                        excells1 = ExcelL.ExlWs.get_Range("I" + ExcelRow.ToString(), "I" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["vol_ipu_kub"];

                        excells1 = ExcelL.ExlWs.get_Range("J" + ExcelRow.ToString(), "J" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["vol_npu_kub"];

                        excells1 = ExcelL.ExlWs.get_Range("K" + ExcelRow.ToString(), "K" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["vol_odn_kub"];

                        excells1 = ExcelL.ExlWs.get_Range("L" + ExcelRow.ToString(), "L" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["rsum_tarif"];

                        excells1 = ExcelL.ExlWs.get_Range("M" + ExcelRow.ToString(), "M" + ExcelRow.ToString());
                        if (DT.Rows[i]["rsum_tarif_odn"].ToString() != "0.00")
                            excells1.FormulaR1C1 = DT.Rows[i]["rsum_tarif_odn"];

                        excells1 = ExcelL.ExlWs.get_Range("N" + ExcelRow.ToString(), "N" + ExcelRow.ToString());
                        if (DT.Rows[i]["vozv_kub"].ToString() != "0.00")
                            excells1.FormulaR1C1 = DT.Rows[i]["vozv_kub"];

                        excells1 = ExcelL.ExlWs.get_Range("O" + ExcelRow.ToString(), "O" + ExcelRow.ToString());
                        if (DT.Rows[i]["vozv_odn_kub"].ToString() != "0.0000")
                            excells1.FormulaR1C1 = DT.Rows[i]["vozv_odn_kub"];

                        excells1 = ExcelL.ExlWs.get_Range("P" + ExcelRow.ToString(), "P" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["sum_nedop"];

                        excells1 = ExcelL.ExlWs.get_Range("Q" + ExcelRow.ToString(), "Q" + ExcelRow.ToString());
                        if (DT.Rows[i]["sum_nedop_odn"].ToString() != "0.0000")
                            excells1.FormulaR1C1 = DT.Rows[i]["sum_nedop_odn"];

                        excells1 = ExcelL.ExlWs.get_Range("R" + ExcelRow.ToString(), "R" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["reval_kub"];

                        excells1 = ExcelL.ExlWs.get_Range("S" + ExcelRow.ToString(), "S" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["reval"];

                        excells1 = ExcelL.ExlWs.get_Range("T" + ExcelRow.ToString(), "T" + ExcelRow.ToString());
                        if (DT.Rows[i]["vol_charge_kub"].ToString() != "0.00")
                            excells1.FormulaR1C1 = DT.Rows[i]["vol_charge_kub"];

                        excells1 = ExcelL.ExlWs.get_Range("U" + ExcelRow.ToString(), "U" + ExcelRow.ToString());
                        excells1.NumberFormat = "# ##0,00";
                        excells1.FormulaR1C1 = DT.Rows[i]["sum_charge"];

                        if (prm_.has_pu != "3")
                        {
                            excells1 = ExcelL.ExlWs.get_Range("V" + ExcelRow.ToString(), "V" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = DT.Rows[i]["odpu_kub"];

                        }



                        if (DT.Rows[i]["count_gil_ipu"] != DBNull.Value)
                            itogString[1] = itogString[1] + System.Convert.ToDecimal(DT.Rows[i]["count_gil_ipu"]);
                        if (DT.Rows[i]["count_gil_npu"] != DBNull.Value)
                            itogString[2] = itogString[2] + System.Convert.ToDecimal(DT.Rows[i]["count_gil_npu"]);
                        if (DT.Rows[i]["volume_all_kub"] != DBNull.Value)
                            itogString[3] = itogString[3] + System.Convert.ToDecimal(DT.Rows[i]["volume_all_kub"]);
                        if (DT.Rows[i]["vol_ipu_kub"] != DBNull.Value)
                            itogString[4] = itogString[4] + System.Convert.ToDecimal(DT.Rows[i]["vol_ipu_kub"]);
                        if (DT.Rows[i]["vol_npu_kub"] != DBNull.Value)
                            itogString[5] = itogString[5] + System.Convert.ToDecimal(DT.Rows[i]["vol_npu_kub"]);
                        if (DT.Rows[i]["vol_odn_kub"] != DBNull.Value)
                            itogString[6] = itogString[6] + System.Convert.ToDecimal(DT.Rows[i]["vol_odn_kub"]);
                        if (DT.Rows[i]["rsum_tarif"] != DBNull.Value)
                            itogString[7] = itogString[7] + System.Convert.ToDecimal(DT.Rows[i]["rsum_tarif"]);
                        if (DT.Rows[i]["rsum_tarif_odn"] != DBNull.Value)
                            itogString[8] = itogString[8] + System.Convert.ToDecimal(DT.Rows[i]["rsum_tarif_odn"]);
                        if (DT.Rows[i]["vozv_kub"] != DBNull.Value)
                            itogString[9] = itogString[9] + System.Convert.ToDecimal(DT.Rows[i]["vozv_kub"]);
                        if (DT.Rows[i]["vozv_odn_kub"] != DBNull.Value)
                            itogString[10] = itogString[10] + System.Convert.ToDecimal(DT.Rows[i]["vozv_odn_kub"]);
                        if (DT.Rows[i]["sum_nedop"] != DBNull.Value)
                            itogString[11] = itogString[11] + System.Convert.ToDecimal(DT.Rows[i]["sum_nedop"]);
                        if (DT.Rows[i]["sum_nedop_odn"] != DBNull.Value)
                            itogString[12] = itogString[12] + System.Convert.ToDecimal(DT.Rows[i]["sum_nedop_odn"]);
                        if (DT.Rows[i]["reval_kub"] != DBNull.Value)
                            itogString[13] = itogString[13] + System.Convert.ToDecimal(DT.Rows[i]["reval_kub"]);
                        if (DT.Rows[i]["reval"] != DBNull.Value)
                            itogString[14] = itogString[14] + System.Convert.ToDecimal(DT.Rows[i]["reval"]);
                        if (DT.Rows[i]["vol_charge_kub"] != DBNull.Value)
                            itogString[15] = itogString[15] + System.Convert.ToDecimal(DT.Rows[i]["vol_charge_kub"]);
                        if (DT.Rows[i]["sum_charge"] != DBNull.Value)
                            itogString[16] = itogString[16] + System.Convert.ToDecimal(DT.Rows[i]["sum_charge"]);
                        if (DT.Rows[i]["odpu_kub"] != DBNull.Value)
                            itogString[17] = itogString[17] + System.Convert.ToDecimal(DT.Rows[i]["odpu_kub"]);



                        i++;
                        ExcelRow++;
                    }

                    excells1 = ExcelL.ExlWs.get_Range("C" + ExcelRow.ToString(), "C" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.FormulaR1C1 = "Итого";

                    for (int j = 1; j < 18; j++)
                        ExcelL.ExlWs.Cells[ExcelRow, j + 5] = itogString[j];


                    excells1 = ExcelL.ExlWs.get_Range("C" + ExcelRow.ToString(), "V" + ExcelRow.ToString());
                    excells1.Font.Bold = true;

                    ExcelRow++;


                    excells1 = ExcelL.ExlWs.get_Range("F13", "F" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1 = ExcelL.ExlWs.get_Range("G13", "G" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1 = ExcelL.ExlWs.get_Range("H13", "V" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";
                    excells1 = ExcelL.ExlWs.get_Range("O13", "O" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,0000";
                    excells1 = ExcelL.ExlWs.get_Range("R13", "R" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";
                    excells1 = ExcelL.ExlWs.get_Range("T13", "T" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,0000";

                    if (prm_.has_pu != "3")
                    {
                        excells1 = ExcelL.ExlWs.get_Range("V" + ExcelRow.ToString(), "V" + ExcelRow.ToString());
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excells1.NumberFormat = "# ##0,00";

                    }



                    if (prm_.has_pu != "3")
                    {
                        excells1 = ExcelL.ExlWs.get_Range("A13", "V" + (ExcelRow - 1).ToString());
                    }
                    else
                    {
                        excells1 = ExcelL.ExlWs.get_Range("A13", "V" + (ExcelRow - 1).ToString());
                    }
                    excells1.Borders[XlBordersIndex.xlEdgeTop].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Borders.Weight = 2;
                    excells1.Borders[XlBordersIndex.xlEdgeRight].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Borders.Weight = 2;
                    excells1.Borders[XlBordersIndex.xlEdgeLeft].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Borders.Weight = 2;
                    excells1.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Borders.Weight = 2;
                    ExcelL.ExlWs.Rows.Font.Name = "Times New Roman";
                    excells1.Font.Size = 11;


                    SetReportSing(ExcelL, ExcelRow + 3, true);


                    #endregion

                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "РСО_ХВС_" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString() + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }

                    #endregion

                }

                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSoderg DataTable : ОШИБКА!";

                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "РСО_ХВС_" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }

                    //удаление объекта
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                try
                {
                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "РСО_ХВС_" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет по РСО ХВС", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSoderg2HvWater Excel File : ОШИБКА!";
                }

            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }

            return ret;

        }



        public Returns GetSpravSoderg2Water(Prm prm_, string Nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            if (prm_.nzp_serv == 6)
            {
                GetSpravSoderg2HvWater(prm_, Nzp_user, ref fileName);
                return ret;
            }
            else if (prm_.nzp_serv == 7)
            {
                GetSpravSoderg2Kan(prm_, Nzp_user, ref fileName);
                return ret;
            }

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetSpravSoderg2Water(prm_, out ret, Nzp_user);

            ExcelLoader ExcelL = null;
            try
            {
                if (DT != null)
                {
                    ExcelL = new ExcelLoader();
                    ExcelCreater ExcelCr = new ExcelCreater();

                    Microsoft.Office.Interop.Excel.Range excells1;

                    Dictionary<string, string> Dic = new Dictionary<string, string>();
                    Dic.Add("ERCName", SetReportHeader());
                    //  string strMonthName = "";


                    #region Пишем заголовок
                    Dic.Add("month", Utils.GetMonthName(prm_.month_));//месяц
                    Dic.Add("year", prm_.year_.ToString());//год
                    Dic.Add("DATE", DateTime.Now.ToShortDateString());//год
                    if (prm_.has_pu == "1")
                    {
                        Dic.Add("ODPU", "(все дома)");
                    }
                    else
                        if (prm_.has_pu == "2")
                        {
                            Dic.Add("ODPU", "(дома с ОДПУ)");
                        }
                        else
                        {
                            Dic.Add("ODPU", "(дома без ОДПУ)");
                        }

                    if (prm_.nzp_key > -1)
                        Dic.Add("NAMESUPP", prm_.name_prm);
                    else
                        Dic.Add("NAMESUPP", "Все поставщики");


                    Returns ret_cond = Utils.InitReturns();
                    ExcelRep ExR_cond = new ExcelRep();
                    System.Data.DataTable DT_cond = ExR.GetFindTemplate(out ret, Nzp_user);
                    //string s_res = "";
                    string nameUk = "";

                    if (DT_cond != null)
                    {
                        for (int k = 0; k < DT_cond.Rows.Count; k++)
                        {

                            if (DT_cond.Rows[k]["name"].ToString().Trim() == "Управляющая организация")
                            {
                                nameUk = DT_cond.Rows[k]["value"].ToString().Trim();
                            }

                        }
                    }
                    Dic.Add("NAMEUK", nameUk);
                    #endregion


                    if (prm_.has_pu == "3")
                    {
                        ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("sprav_soderg2_water.xls"), Dic);
                    }
                    else
                    {
                        ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("sprav_soderg2_water2.xls"), Dic);
                    }




                    ExcelFormater exform = new ExcelFormater();





                    #region Пишем тело

                    int ExcelRow = 14;
                    int i = 0;
                    decimal[] itogString = new decimal[27];
                    for (int j = 1; j < 27; j++) itogString[j] = 0;

                    while (i < DT.Rows.Count)
                    {
                        excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "A" + ExcelRow.ToString());
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = (i + 1);

                        excells1 = ExcelL.ExlWs.get_Range("B" + ExcelRow.ToString(), "B" + ExcelRow.ToString());
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = DT.Rows[i]["nzp_dom"];

                        excells1 = ExcelL.ExlWs.get_Range("C" + ExcelRow.ToString(), "C" + ExcelRow.ToString());
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = DT.Rows[i]["ulica"].ToString().Trim();

                        excells1 = ExcelL.ExlWs.get_Range("D" + ExcelRow.ToString(), "D" + ExcelRow.ToString());
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = DT.Rows[i]["ndom"].ToString().Trim();
                        ;

                        excells1 = ExcelL.ExlWs.get_Range("E" + ExcelRow.ToString(), "E" + ExcelRow.ToString());
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excells1.FormulaR1C1 = DT.Rows[i]["nkor"].ToString().Replace("-", "");

                        excells1 = ExcelL.ExlWs.get_Range("F" + ExcelRow.ToString(), "F" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["count_gil_ipu"];

                        excells1 = ExcelL.ExlWs.get_Range("G" + ExcelRow.ToString(), "G" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["count_gil_npu"];

                        excells1 = ExcelL.ExlWs.get_Range("H" + ExcelRow.ToString(), "H" + ExcelRow.ToString());
                        if (DT.Rows[i]["volume_all_kub"].ToString() != "0.00")
                            excells1.FormulaR1C1 = DT.Rows[i]["volume_all_kub"];

                        excells1 = ExcelL.ExlWs.get_Range("I" + ExcelRow.ToString(), "I" + ExcelRow.ToString());
                        if (DT.Rows[i]["volume_all_gkal"].ToString() != "0.0000")
                            excells1.FormulaR1C1 = DT.Rows[i]["volume_all_gkal"];

                        excells1 = ExcelL.ExlWs.get_Range("J" + ExcelRow.ToString(), "J" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["vol_ipu_kub"];

                        excells1 = ExcelL.ExlWs.get_Range("K" + ExcelRow.ToString(), "K" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["vol_ipu_gkal"];

                        excells1 = ExcelL.ExlWs.get_Range("L" + ExcelRow.ToString(), "L" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["vol_npu_kub"];

                        excells1 = ExcelL.ExlWs.get_Range("M" + ExcelRow.ToString(), "M" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["vol_npu_gkal"];

                        excells1 = ExcelL.ExlWs.get_Range("N" + ExcelRow.ToString(), "N" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["vol_odn_kub"];

                        excells1 = ExcelL.ExlWs.get_Range("O" + ExcelRow.ToString(), "O" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["vol_odn_gkal"];

                        excells1 = ExcelL.ExlWs.get_Range("P" + ExcelRow.ToString(), "P" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["rsum_tarif"];

                        excells1 = ExcelL.ExlWs.get_Range("Q" + ExcelRow.ToString(), "Q" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["rsum_tarif_odn"];

                        excells1 = ExcelL.ExlWs.get_Range("R" + ExcelRow.ToString(), "R" + ExcelRow.ToString());
                        if (DT.Rows[i]["vozv_kub"].ToString() != "0.00")
                            excells1.FormulaR1C1 = DT.Rows[i]["vozv_kub"];

                        excells1 = ExcelL.ExlWs.get_Range("S" + ExcelRow.ToString(), "S" + ExcelRow.ToString());
                        if (DT.Rows[i]["vozv_gkal"].ToString() != "0.0000")
                            excells1.FormulaR1C1 = DT.Rows[i]["vozv_gkal"];

                        excells1 = ExcelL.ExlWs.get_Range("T" + ExcelRow.ToString(), "T" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["vozv_odn_kub"];

                        excells1 = ExcelL.ExlWs.get_Range("U" + ExcelRow.ToString(), "U" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["vozv_odn_gkal"];

                        excells1 = ExcelL.ExlWs.get_Range("V" + ExcelRow.ToString(), "V" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["sum_nedop"];

                        excells1 = ExcelL.ExlWs.get_Range("W" + ExcelRow.ToString(), "W" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["sum_nedop_odn"];

                        excells1 = ExcelL.ExlWs.get_Range("X" + ExcelRow.ToString(), "X" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["reval_kub"];

                        excells1 = ExcelL.ExlWs.get_Range("Y" + ExcelRow.ToString(), "Y" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["reval_gkal"];

                        excells1 = ExcelL.ExlWs.get_Range("Z" + ExcelRow.ToString(), "Z" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["reval"];

                        excells1 = ExcelL.ExlWs.get_Range("AA" + ExcelRow.ToString(), "AA" + ExcelRow.ToString());
                        if (DT.Rows[i]["vol_charge_kub"].ToString() != "0.00")
                            excells1.FormulaR1C1 = DT.Rows[i]["vol_charge_kub"];

                        excells1 = ExcelL.ExlWs.get_Range("AB" + ExcelRow.ToString(), "AB" + ExcelRow.ToString());
                        if (DT.Rows[i]["vol_charge_gkal"].ToString() != "0.0000")
                            excells1.FormulaR1C1 = DT.Rows[i]["vol_charge_gkal"];

                        excells1 = ExcelL.ExlWs.get_Range("AC" + ExcelRow.ToString(), "AC" + ExcelRow.ToString());
                        excells1.NumberFormat = "# ##0,00";
                        excells1.FormulaR1C1 = DT.Rows[i]["sum_charge"];

                        if (prm_.has_pu != "3")
                        {
                            excells1 = ExcelL.ExlWs.get_Range("AD" + ExcelRow.ToString(), "AD" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = DT.Rows[i]["odpu_kub"];

                            excells1 = ExcelL.ExlWs.get_Range("AE" + ExcelRow.ToString(), "AE" + ExcelRow.ToString());
                            excells1.FormulaR1C1 = DT.Rows[i]["odpu_gkal"];
                        }



                        if (DT.Rows[i]["count_gil_ipu"] != DBNull.Value)
                            itogString[1] = itogString[1] + System.Convert.ToDecimal(DT.Rows[i]["count_gil_ipu"]);
                        if (DT.Rows[i]["count_gil_npu"] != DBNull.Value)
                            itogString[2] = itogString[2] + System.Convert.ToDecimal(DT.Rows[i]["count_gil_npu"]);
                        if (DT.Rows[i]["volume_all_kub"] != DBNull.Value)
                            itogString[3] = itogString[3] + System.Convert.ToDecimal(DT.Rows[i]["volume_all_kub"]);
                        if (DT.Rows[i]["volume_all_gkal"] != DBNull.Value)
                            itogString[4] = itogString[4] + System.Convert.ToDecimal(DT.Rows[i]["volume_all_gkal"]);
                        if (DT.Rows[i]["vol_ipu_kub"] != DBNull.Value)
                            itogString[5] = itogString[5] + System.Convert.ToDecimal(DT.Rows[i]["vol_ipu_kub"]);
                        if (DT.Rows[i]["vol_ipu_gkal"] != DBNull.Value)
                            itogString[6] = itogString[6] + System.Convert.ToDecimal(DT.Rows[i]["vol_ipu_gkal"]);
                        if (DT.Rows[i]["vol_npu_kub"] != DBNull.Value)
                            itogString[7] = itogString[7] + System.Convert.ToDecimal(DT.Rows[i]["vol_npu_kub"]);
                        if (DT.Rows[i]["vol_npu_gkal"] != DBNull.Value)
                            itogString[8] = itogString[8] + System.Convert.ToDecimal(DT.Rows[i]["vol_npu_gkal"]);
                        if (DT.Rows[i]["vol_odn_kub"] != DBNull.Value)
                            itogString[9] = itogString[9] + System.Convert.ToDecimal(DT.Rows[i]["vol_odn_kub"]);
                        if (DT.Rows[i]["vol_odn_gkal"] != DBNull.Value)
                            itogString[10] = itogString[10] + System.Convert.ToDecimal(DT.Rows[i]["vol_odn_gkal"]);
                        if (DT.Rows[i]["rsum_tarif"] != DBNull.Value)
                            itogString[11] = itogString[11] + System.Convert.ToDecimal(DT.Rows[i]["rsum_tarif"]);
                        if (DT.Rows[i]["rsum_tarif_odn"] != DBNull.Value)
                            itogString[12] = itogString[12] + System.Convert.ToDecimal(DT.Rows[i]["rsum_tarif_odn"]);
                        if (DT.Rows[i]["vozv_kub"] != DBNull.Value)
                            itogString[13] = itogString[13] + System.Convert.ToDecimal(DT.Rows[i]["vozv_kub"]);
                        if (DT.Rows[i]["vozv_gkal"] != DBNull.Value)
                            itogString[14] = itogString[14] + System.Convert.ToDecimal(DT.Rows[i]["vozv_gkal"]);
                        if (DT.Rows[i]["vozv_odn_kub"] != DBNull.Value)
                            itogString[15] = itogString[15] + System.Convert.ToDecimal(DT.Rows[i]["vozv_odn_kub"]);
                        if (DT.Rows[i]["vozv_odn_gkal"] != DBNull.Value)
                            itogString[16] = itogString[16] + System.Convert.ToDecimal(DT.Rows[i]["vozv_odn_gkal"]);
                        if (DT.Rows[i]["sum_nedop"] != DBNull.Value)
                            itogString[17] = itogString[17] + System.Convert.ToDecimal(DT.Rows[i]["sum_nedop"]);
                        if (DT.Rows[i]["sum_nedop_odn"] != DBNull.Value)
                            itogString[18] = itogString[18] + System.Convert.ToDecimal(DT.Rows[i]["sum_nedop_odn"]);
                        if (DT.Rows[i]["reval_kub"] != DBNull.Value)
                            itogString[19] = itogString[19] + System.Convert.ToDecimal(DT.Rows[i]["reval_kub"]);
                        if (DT.Rows[i]["reval_gkal"] != DBNull.Value)
                            itogString[20] = itogString[20] + System.Convert.ToDecimal(DT.Rows[i]["reval_gkal"]);
                        if (DT.Rows[i]["reval"] != DBNull.Value)
                            itogString[21] = itogString[21] + System.Convert.ToDecimal(DT.Rows[i]["reval"]);
                        if (DT.Rows[i]["vol_charge_kub"] != DBNull.Value)
                            itogString[22] = itogString[22] + System.Convert.ToDecimal(DT.Rows[i]["vol_charge_kub"]);
                        if (DT.Rows[i]["vol_charge_gkal"] != DBNull.Value)
                            itogString[23] = itogString[23] + System.Convert.ToDecimal(DT.Rows[i]["vol_charge_gkal"]);
                        if (DT.Rows[i]["sum_charge"] != DBNull.Value)
                            itogString[24] = itogString[24] + System.Convert.ToDecimal(DT.Rows[i]["sum_charge"]);
                        if (DT.Rows[i]["odpu_kub"] != DBNull.Value)
                            itogString[25] = itogString[25] + System.Convert.ToDecimal(DT.Rows[i]["odpu_kub"]);
                        if (DT.Rows[i]["odpu_gkal"] != DBNull.Value)
                            itogString[26] = itogString[26] + System.Convert.ToDecimal(DT.Rows[i]["odpu_gkal"]);



                        i++;
                        ExcelRow++;
                    }

                    excells1 = ExcelL.ExlWs.get_Range("C" + ExcelRow.ToString(), "C" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.FormulaR1C1 = "Итого";

                    for (int j = 1; j < 25; j++)
                        ExcelL.ExlWs.Cells[ExcelRow, j + 5] = itogString[j];


                    excells1 = ExcelL.ExlWs.get_Range("C" + ExcelRow.ToString(), "AD" + ExcelRow.ToString());
                    excells1.Font.Bold = true;

                    ExcelRow++;


                    excells1 = ExcelL.ExlWs.get_Range("F14", "F" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1 = ExcelL.ExlWs.get_Range("G14", "G" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1 = ExcelL.ExlWs.get_Range("H14", "H" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";
                    excells1 = ExcelL.ExlWs.get_Range("I14", "I" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,0000";
                    excells1 = ExcelL.ExlWs.get_Range("J14", "J" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";
                    excells1 = ExcelL.ExlWs.get_Range("K14", "K" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,0000";
                    excells1 = ExcelL.ExlWs.get_Range("L14", "L" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";
                    excells1 = ExcelL.ExlWs.get_Range("M14", "M" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,0000";
                    excells1 = ExcelL.ExlWs.get_Range("N14", "N" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";
                    excells1 = ExcelL.ExlWs.get_Range("O14", "O" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,0000";
                    excells1 = ExcelL.ExlWs.get_Range("P14", "P" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";
                    excells1 = ExcelL.ExlWs.get_Range("Q14", "Q" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";
                    excells1 = ExcelL.ExlWs.get_Range("R14", "R" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";
                    excells1 = ExcelL.ExlWs.get_Range("S14", "S" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,0000";
                    excells1 = ExcelL.ExlWs.get_Range("T14", "T" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";
                    excells1 = ExcelL.ExlWs.get_Range("U14", "U" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,0000";
                    excells1 = ExcelL.ExlWs.get_Range("V14", "V" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";
                    excells1 = ExcelL.ExlWs.get_Range("W14", "W" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";
                    excells1 = ExcelL.ExlWs.get_Range("X14", "X" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";
                    excells1 = ExcelL.ExlWs.get_Range("Y14", "Y" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,0000";
                    excells1 = ExcelL.ExlWs.get_Range("Z14", "Z" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";
                    excells1 = ExcelL.ExlWs.get_Range("AA14", "AA" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";
                    excells1 = ExcelL.ExlWs.get_Range("AB14", "AB" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,0000";
                    excells1 = ExcelL.ExlWs.get_Range("AC14", "AC" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";
                    if (prm_.has_pu != "3")
                    {
                        excells1 = ExcelL.ExlWs.get_Range("AD" + ExcelRow.ToString(), "AD" + ExcelRow.ToString());
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excells1.NumberFormat = "# ##0,00";

                        excells1 = ExcelL.ExlWs.get_Range("AE" + ExcelRow.ToString(), "AE" + ExcelRow.ToString());
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excells1.NumberFormat = "# ##0,0000";
                    }



                    if (prm_.has_pu != "3")
                    {
                        excells1 = ExcelL.ExlWs.get_Range("A14", "AE" + (ExcelRow - 1).ToString());
                    }
                    else
                    {
                        excells1 = ExcelL.ExlWs.get_Range("A14", "AC" + (ExcelRow - 1).ToString());
                    }
                    excells1.Borders[XlBordersIndex.xlEdgeTop].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Borders.Weight = 2;
                    excells1.Borders[XlBordersIndex.xlEdgeRight].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Borders.Weight = 2;
                    excells1.Borders[XlBordersIndex.xlEdgeLeft].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Borders.Weight = 2;
                    excells1.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Borders.Weight = 2;
                    ExcelL.ExlWs.Rows.Font.Name = "Times New Roman";
                    excells1.Font.Size = 11;


                    SetReportSing(ExcelL, ExcelRow + 3, true);


                    #endregion

                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SpravSoderg2" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString() + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        ExcelL.ExlWb.Close(false, Type.Missing, Type.Missing);
                        ExcelL.ExlWb = null;
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }

                    #endregion

                }

                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSoderg DataTable : ОШИБКА!";

                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SpravSoderg2" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }

                    //удаление объекта
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                try
                {
                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SpravSoderg2" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет GetSpravSoderg", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSoderg Excel File : ОШИБКА!";
                }

            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }

            return ret;

        }


        public void MakeListSpravSuppNachHar(System.Data.DataTable DT, ExcelLoader ExcelL, int numList, Prm prm, string nzp_user)
        {
            ExcelL.GetWs(numList, "Лист " + numList.ToString());
            #region Создаем шапку
            Microsoft.Office.Interop.Excel.Range excells1;

            //спустили шапку
            excells1 = ExcelL.ExlWs.get_Range("A1", "D1");
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "СПРАВКА ПО ПОСТАВЩИКАМ КОММУНАЛЬНЫХ УСЛУГ";
            excells1.Font.Bold = true;
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

            excells1 = ExcelL.ExlWs.get_Range("A2", "D2");
            excells1.Merge(Type.Missing);
            excells1.Font.Bold = true;
            //string ERC_name = "";

            //if (STCLINE.KP50.Interfaces.Points.IsDemo == true)
            //    ERC_name = "Н-ска";
            //else
            //    ERC_name = "Самары";

            //excells1.FormulaR1C1 = "по МП городского округа " + ERC_name + " ''ЕИРЦ'' за " +
            string s_name = SetReportHeader();
            excells1.FormulaR1C1 = s_name + " за " +
                prm.month_.ToString("00") + " " + prm.year_.ToString() + "г.";
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

            excells1 = ExcelL.ExlWs.get_Range("A3", "A3");
            excells1.Merge(Type.Missing);
            excells1.Font.Bold = true;
            excells1.FormulaR1C1 = System.DateTime.Today.ToShortDateString();
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

            excells1 = ExcelL.ExlWs.get_Range("B3", "K4");
            excells1.Font.Bold = true;
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = SetReportConditional(System.Convert.ToInt32(nzp_user));
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
            excells1.RowHeight = 23;

            excells1 = ExcelL.ExlWs.get_Range("B5", "K5");
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "";
            if (numList > 1)
            {
                excells1.FormulaR1C1 = "ЖЭУ: " + DT.Rows[0]["geu"];
            }
            excells1.Font.Bold = true;

            excells1 = ExcelL.ExlWs.get_Range("A6", "A7");
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Поставщик к/услуг";

            excells1 = ExcelL.ExlWs.get_Range("B6", "B7");
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Виды услуг";

            excells1 = ExcelL.ExlWs.get_Range("C6", "C7");
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Оплачи- ваемая площадь изолиро- ванных квартир, м2";
            excells1.ColumnWidth = 13;

            excells1 = ExcelL.ExlWs.get_Range("D6", "D7");
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Оплачи- ваемая площадь комму- нальных квартир, м2";
            excells1.ColumnWidth = 12;

            excells1 = ExcelL.ExlWs.get_Range("E6", "E7");
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Коли- чество прожи- ваю- щих";
            excells1.ColumnWidth = 8.43;

            excells1 = ExcelL.ExlWs.get_Range("F7", "F7");
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Посто- янные начис- ления";

            excells1 = ExcelL.ExlWs.get_Range("G7", "G7");
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Возв- раты/ доплаты";

            excells1 = ExcelL.ExlWs.get_Range("H7", "H7");
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Разовые коррек- тировки ''''красные''''";

            excells1 = ExcelL.ExlWs.get_Range("I7", "I7");
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Разовые коррек- тировки ''''черные''''";

            excells1 = ExcelL.ExlWs.get_Range("J7", "J7");
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Субси- дии/ списано сальдо";

            excells1 = ExcelL.ExlWs.get_Range("K7", "K7");
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Коррек- тировка за отопи- тельный период";

            excells1 = ExcelL.ExlWs.get_Range("L7", "L7");
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Итого к оплате";

            excells1 = ExcelL.ExlWs.get_Range("M6", "M7");
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Оплачено, руб";

            excells1 = ExcelL.ExlWs.get_Range("F6", "K6");
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Начислено,руб";

            excells1 = ExcelL.ExlWs.get_Range("A6", "M7");
            excells1.Font.Size = 8;
            excells1.WrapText = true;
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

            #endregion

            #region Пишем тело

            int ExcelRow = 8;
            int StartRow;
            int i = 0;
            decimal sum_charge;
            decimal sum_money;
            decimal rsum_tarif;
            decimal sum_nedop;
            decimal reval_k;
            decimal reval_d;
            decimal sum_otopl;
            string old_supp = "";
            string new_supp = "";

            while (i < DT.Rows.Count - 1)
            {
                sum_charge = 0;
                sum_money = 0;
                rsum_tarif = 0;
                sum_nedop = 0;
                reval_k = 0;
                reval_d = 0;
                sum_otopl = 0;
                sum_charge = 0;
                sum_money = 0;
                StartRow = ExcelRow;

                old_supp = DT.Rows[i]["name_supp"].ToString().Trim();
                new_supp = old_supp;
                while ((i < DT.Rows.Count) & (String.Equals(old_supp, new_supp) == true))
                {
                    ExcelL.ExlWs.Cells[ExcelRow, 2] = DT.Rows[i]["service"].ToString().Trim();
                    if ((int)DT.Rows[i]["ord_serv"] != 515)
                    {
                        ExcelL.ExlWs.Cells[ExcelRow, 3] = Decimal.Parse(DT.Rows[i]["isol_pl"].ToString());
                        ExcelL.ExlWs.Cells[ExcelRow, 4] = Decimal.Parse(DT.Rows[i]["komm_pl"].ToString());
                        ExcelL.ExlWs.Cells[ExcelRow, 5] = Decimal.Parse(DT.Rows[i]["count_gil"].ToString());
                    }
                    else
                    {
                        if (i > 0)
                            if ((int)DT.Rows[i - 1]["ord_serv"] == 25)
                            {
                                excells1 = ExcelL.ExlWs.get_Range("C" + (ExcelRow - 1).ToString(), "C" + ExcelRow.ToString());
                                excells1.Merge(Type.Missing);
                                excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excells1 = ExcelL.ExlWs.get_Range("D" + (ExcelRow - 1).ToString(), "D" + ExcelRow.ToString());
                                excells1.Merge(Type.Missing);
                                excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                                excells1 = ExcelL.ExlWs.get_Range("E" + (ExcelRow - 1).ToString(), "E" + ExcelRow.ToString());
                                excells1.Merge(Type.Missing);
                                excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            }

                    }


                    ExcelL.ExlWs.Cells[ExcelRow, 6] = Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());
                    ExcelL.ExlWs.Cells[ExcelRow, 7] = Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString());
                    ExcelL.ExlWs.Cells[ExcelRow, 8] = Decimal.Parse(DT.Rows[i]["reval_k"].ToString());
                    ExcelL.ExlWs.Cells[ExcelRow, 9] = Decimal.Parse(DT.Rows[i]["reval_d"].ToString());
                    ExcelL.ExlWs.Cells[ExcelRow, 10] = 0;
                    ExcelL.ExlWs.Cells[ExcelRow, 11] = Decimal.Parse(DT.Rows[i]["sum_otopl"].ToString());
                    ExcelL.ExlWs.Cells[ExcelRow, 12] = Decimal.Parse(DT.Rows[i]["sum_charge"].ToString());
                    ExcelL.ExlWs.Cells[ExcelRow, 13] = Decimal.Parse(DT.Rows[i]["sum_money"].ToString());

                    rsum_tarif = rsum_tarif + Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());
                    sum_nedop = sum_nedop + Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString());
                    reval_k = reval_k + Decimal.Parse(DT.Rows[i]["reval_k"].ToString());
                    reval_d = reval_d + Decimal.Parse(DT.Rows[i]["reval_d"].ToString());
                    sum_otopl = sum_otopl + Decimal.Parse(DT.Rows[i]["sum_otopl"].ToString());
                    sum_charge = sum_charge + Decimal.Parse(DT.Rows[i]["sum_charge"].ToString());
                    sum_money = sum_money + Decimal.Parse(DT.Rows[i]["sum_money"].ToString());

                    ExcelRow++;

                    i++;
                    if (i < DT.Rows.Count)
                    {
                        new_supp = DT.Rows[i]["name_supp"].ToString().Trim();
                    }
                    else
                    {
                        new_supp = "";
                    }
                }

                ExcelL.ExlWs.Cells[ExcelRow, 2] = "Итого";
                ExcelL.ExlWs.Cells[ExcelRow, 6] = rsum_tarif;
                ExcelL.ExlWs.Cells[ExcelRow, 7] = sum_nedop;
                ExcelL.ExlWs.Cells[ExcelRow, 8] = reval_k;
                ExcelL.ExlWs.Cells[ExcelRow, 9] = reval_d;
                ExcelL.ExlWs.Cells[ExcelRow, 10] = 0;
                ExcelL.ExlWs.Cells[ExcelRow, 11] = sum_otopl;
                ExcelL.ExlWs.Cells[ExcelRow, 12] = sum_charge;
                ExcelL.ExlWs.Cells[ExcelRow, 13] = sum_money;

                excells1 = ExcelL.ExlWs.get_Range("B" + ExcelRow.ToString(), "M" + ExcelRow.ToString());
                excells1.Font.Bold = true;

                //Поставщик
                excells1 = ExcelL.ExlWs.get_Range("A" + StartRow.ToString(), "A" + ExcelRow.ToString());
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = old_supp;
                excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                ExcelRow++;

            }
            #endregion

            #region Пишем подвал

            if (ExcelRow > 6)
            {
                ExcelRow--;
                excells1 = ExcelL.ExlWs.get_Range("F7", "M" + ExcelRow.ToString());
                excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                excells1.Font.Size = 8;
                excells1.EntireColumn.AutoFit();

                excells1 = ExcelL.ExlWs.get_Range("A6", "B" + ExcelRow.ToString());
                excells1.Font.Size = 8;
                excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                excells1.EntireColumn.AutoFit();

                excells1 = ExcelL.ExlWs.get_Range("C7", "E" + ExcelRow.ToString());
                excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                excells1.Font.Size = 8;

                excells1 = ExcelL.ExlWs.get_Range("C7", "M" + ExcelRow.ToString());
                excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                excells1.EntireColumn.NumberFormat = "# ##0,00";
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;

                excells1 = ExcelL.ExlWs.get_Range("E7", "E" + ExcelRow.ToString());
                excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                excells1.EntireColumn.NumberFormat = "# ##0";
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;

            }

            SetReportSing(ExcelL, ExcelRow + 3, false, Convert.ToInt32(nzp_user));


            #endregion
        }


        //Отчет: справка о наличии долга
        public void GetSpravHasDolg(object container)
        {
            MonitorLog.WriteLog("Старт " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
            Returns ret = Utils.InitReturns();
            ParamContainer cont = (ParamContainer)container;
            ExcelRep excelRepDb = new ExcelRep();

            #region Проверка на наличие Excel
            if (!STCLINE.KP50.Global.Constants.ExcelIsInstalled)
            {
                //Excel не установлен
                string time2 = "";
                //запись в БД о постановки в поток(статус 0)
                ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 1, "Отчет \"Справка о наличии долга\"", ref time2, cont.comment);
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: Отсутствие уставновленной версии Excel", MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-2", 1, "", time2);
                return;
            }
            #endregion

            //путь, по которому скачивается файл
            string path = "";
            //время записи в БД
            string time = "";
            //Имя файла отчета
            string fileName = "";

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddToPoolThread(cont.nzp_user, "empty", 2, "Отчет \"Справка о наличии долга\" ", ref time, "");

            //запись в БД о старте формирования(сейчас пока для порядка)(статус 1)
            ret = excelRepDb.MarkStartPoolThread(cont.nzp_user, "1", 2, "", time);

            //Старт формирования
            ret = this.GetSpravHasDolg(cont.nzp_kvar.ToString(), cont.yy, cont.mm, cont.nzp_user.ToString(), ref fileName);
            if (ret.result)
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Формирование отчета успешно завершено. ", MonitorLog.typelog.Info, true);
                //Запись в БД об успешном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "2", 2, path + fileName, time);
            }
            else
            {
                MonitorLog.WriteLog("Пользователь: " + cont.nzp_user + "Ошибка формирования отчета. Текст ошибки: " + ret.text, MonitorLog.typelog.Error, true);
                //запись об неудачном формировании
                ret = excelRepDb.MarkPoolThread(cont.nzp_user, "-1", 2, "", time);
            }
            MonitorLog.WriteLog("Стоп " + System.Reflection.MethodBase.GetCurrentMethod().Name, MonitorLog.typelog.Warn, 20, 201, true);
        }



        public Returns GetSpravSoderg2Heat(Prm prm_, string Nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetSpravSoderg2Heat(prm_, out ret, Nzp_user);

            ExcelLoader ExcelL = null;
            try
            {
                if (DT != null)
                {
                    ExcelL = new ExcelLoader();
                    ExcelCreater ExcelCr = new ExcelCreater();

                    Microsoft.Office.Interop.Excel.Range excells1;

                    Dictionary<string, string> Dic = new Dictionary<string, string>();
                    Dic.Add("ERCName", SetReportHeader());


                    #region Пишем заголовок

                    Dic.Add("month", Utils.GetMonthName(prm_.month_));//месяц
                    Dic.Add("year", prm_.year_.ToString());//год
                    Dic.Add("DATE", DateTime.Now.ToShortDateString());//год
                    if (prm_.has_pu == "1")
                    {
                        Dic.Add("ODPU", "(все дома)");
                    }
                    else
                        if (prm_.has_pu == "2")
                        {
                            Dic.Add("ODPU", "(дома с ОДПУ)");
                        }
                        else
                        {
                            Dic.Add("ODPU", "(дома без ОДПУ)");
                        }

                    if (prm_.nzp_key > -1)
                        Dic.Add("NAMESUPP", prm_.name_prm);
                    else
                        Dic.Add("NAMESUPP", "Все поставщики");

                    Returns ret_cond = Utils.InitReturns();
                    ExcelRep ExR_cond = new ExcelRep();
                    System.Data.DataTable DT_cond = ExR.GetFindTemplate(out ret, Nzp_user);
                    //string s_res = "";
                    string nameUk = "";

                    if (DT_cond != null)
                    {
                        for (int k = 0; k < DT_cond.Rows.Count; k++)
                        {

                            if (DT_cond.Rows[k]["name"].ToString().Trim() == "Управляющая организация")
                            {
                                nameUk = DT_cond.Rows[k]["value"].ToString().Trim();
                            }

                        }
                    }
                    Dic.Add("NAMEUK", nameUk);
                    #endregion

                    ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("sprav_soderg2_heat.xls"), Dic);
                    ExcelFormater exform = new ExcelFormater();

                    #region Пишем тело

                    int ExcelRow = 13;
                    int i = 0;
                    decimal[] itogString = new decimal[27];
                    for (int j = 1; j < 27; j++) itogString[j] = 0;


                    while (i < DT.Rows.Count)
                    {
                        excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "A" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = (i + 1);

                        excells1 = ExcelL.ExlWs.get_Range("B" + ExcelRow.ToString(), "B" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["nzp_dom"];

                        excells1 = ExcelL.ExlWs.get_Range("C" + ExcelRow.ToString(), "C" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["ulica"];

                        excells1 = ExcelL.ExlWs.get_Range("D" + ExcelRow.ToString(), "D" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["ndom"].ToString().Trim();
                        ;


                        excells1 = ExcelL.ExlWs.get_Range("E" + ExcelRow.ToString(), "E" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["nkor"].ToString().Replace("-", "");

                        excells1 = ExcelL.ExlWs.get_Range("F" + ExcelRow.ToString(), "F" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["pl_all"];

                        excells1 = ExcelL.ExlWs.get_Range("G" + ExcelRow.ToString(), "G" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["pl_izol"];

                        excells1 = ExcelL.ExlWs.get_Range("H" + ExcelRow.ToString(), "H" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["pl_komm_all"];

                        excells1 = ExcelL.ExlWs.get_Range("I" + ExcelRow.ToString(), "I" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["pl_komm_gil"];

                        excells1 = ExcelL.ExlWs.get_Range("J" + ExcelRow.ToString(), "J" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["pl_ngil"];

                        excells1 = ExcelL.ExlWs.get_Range("K" + ExcelRow.ToString(), "K" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["pl_mop"];

                        excells1 = ExcelL.ExlWs.get_Range("L" + ExcelRow.ToString(), "L" + ExcelRow.ToString());
                        if (DT.Rows[i]["c_calc_gkal"].ToString() != "0.0000")
                            excells1.FormulaR1C1 = DT.Rows[i]["c_calc_gkal"];

                        excells1 = ExcelL.ExlWs.get_Range("M" + ExcelRow.ToString(), "M" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["rsum_tarif"];

                        excells1 = ExcelL.ExlWs.get_Range("N" + ExcelRow.ToString(), "N" + ExcelRow.ToString());
                        if (DT.Rows[i]["vozv_rso_gkal"].ToString() != "0.0000")
                            excells1.FormulaR1C1 = DT.Rows[i]["vozv_rso_gkal"];

                        excells1 = ExcelL.ExlWs.get_Range("O" + ExcelRow.ToString(), "O" + ExcelRow.ToString());
                        if (DT.Rows[i]["vozv_uk_gkal"].ToString() != "0.0000")
                            excells1.FormulaR1C1 = DT.Rows[i]["vozv_uk_gkal"];

                        excells1 = ExcelL.ExlWs.get_Range("P" + ExcelRow.ToString(), "P" + ExcelRow.ToString());
                        if (DT.Rows[i]["sum_nedop"].ToString() != "0.00")
                            excells1.FormulaR1C1 = DT.Rows[i]["sum_nedop"];

                        excells1 = ExcelL.ExlWs.get_Range("Q" + ExcelRow.ToString(), "Q" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["reval_gkal"];

                        excells1 = ExcelL.ExlWs.get_Range("R" + ExcelRow.ToString(), "R" + ExcelRow.ToString());
                        if (DT.Rows[i]["reval"].ToString() != "0.00")
                            excells1.FormulaR1C1 = DT.Rows[i]["reval"];

                        //excells1 = ExcelL.ExlWs.get_Range("S" + ExcelRow.ToString(), "S" + ExcelRow.ToString());
                        //excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        //excells1.Merge(Type.Missing);
                        //excells1.NumberFormat = "# ##0,00";
                        //if (DT.Rows[i]["p19"].ToString() == "")
                        //    excells1.FormulaR1C1 = 0;
                        //else
                        //    excells1.FormulaR1C1 = DT.Rows[i]["p19"];

                        //excells1 = ExcelL.ExlWs.get_Range("T" + ExcelRow.ToString(), "T" + ExcelRow.ToString());
                        //excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        //excells1.Merge(Type.Missing);
                        //excells1.NumberFormat = "# ##0,00";
                        //if (DT.Rows[i]["p20"].ToString() == "")
                        //    excells1.FormulaR1C1 = 0;
                        //else
                        //    excells1.FormulaR1C1 = DT.Rows[i]["p20"];

                        excells1 = ExcelL.ExlWs.get_Range("U" + ExcelRow.ToString(), "U" + ExcelRow.ToString());
                        if (DT.Rows[i]["sum_charge_gkal"].ToString() != "0.0000")
                            excells1.FormulaR1C1 = DT.Rows[i]["sum_charge_gkal"];

                        excells1 = ExcelL.ExlWs.get_Range("V" + ExcelRow.ToString(), "V" + ExcelRow.ToString());
                        excells1.FormulaR1C1 = DT.Rows[i]["sum_charge"];



                        if (DT.Rows[i]["pl_all"] != DBNull.Value)
                            itogString[1] = itogString[1] + System.Convert.ToDecimal(DT.Rows[i]["pl_all"]);
                        if (DT.Rows[i]["pl_izol"] != DBNull.Value)
                            itogString[2] = itogString[2] + System.Convert.ToDecimal(DT.Rows[i]["pl_izol"]);
                        if (DT.Rows[i]["pl_komm_all"] != DBNull.Value)
                            itogString[3] = itogString[3] + System.Convert.ToDecimal(DT.Rows[i]["pl_komm_all"]);
                        if (DT.Rows[i]["pl_komm_gil"] != DBNull.Value)
                            itogString[4] = itogString[4] + System.Convert.ToDecimal(DT.Rows[i]["pl_komm_gil"]);
                        if (DT.Rows[i]["pl_ngil"] != DBNull.Value)
                            itogString[5] = itogString[5] + System.Convert.ToDecimal(DT.Rows[i]["pl_ngil"]);
                        if (DT.Rows[i]["pl_mop"] != DBNull.Value)
                            itogString[6] = itogString[6] + System.Convert.ToDecimal(DT.Rows[i]["pl_mop"]);
                        if (DT.Rows[i]["c_calc_gkal"] != DBNull.Value)
                            itogString[7] = itogString[7] + System.Convert.ToDecimal(DT.Rows[i]["c_calc_gkal"]);
                        if (DT.Rows[i]["rsum_tarif"] != DBNull.Value)
                            itogString[8] = itogString[8] + System.Convert.ToDecimal(DT.Rows[i]["rsum_tarif"]);
                        if (DT.Rows[i]["vozv_rso_gkal"] != DBNull.Value)
                            itogString[9] = itogString[9] + System.Convert.ToDecimal(DT.Rows[i]["vozv_rso_gkal"]);
                        if (DT.Rows[i]["vozv_uk_gkal"] != DBNull.Value)
                            itogString[10] = itogString[10] + System.Convert.ToDecimal(DT.Rows[i]["vozv_uk_gkal"]);
                        if (DT.Rows[i]["sum_nedop"] != DBNull.Value)
                            itogString[11] = itogString[11] + System.Convert.ToDecimal(DT.Rows[i]["sum_nedop"]);
                        if (DT.Rows[i]["reval_gkal"] != DBNull.Value)
                            itogString[12] = itogString[12] + System.Convert.ToDecimal(DT.Rows[i]["reval_gkal"]);
                        if (DT.Rows[i]["reval"] != DBNull.Value)
                            itogString[13] = itogString[13] + System.Convert.ToDecimal(DT.Rows[i]["reval"]);

                        itogString[14] = 0;
                        itogString[15] = 0;
                        if (DT.Rows[i]["sum_charge_gkal"] != DBNull.Value)
                            itogString[16] = itogString[16] + System.Convert.ToDecimal(DT.Rows[i]["sum_charge_gkal"]);
                        if (DT.Rows[i]["sum_charge"] != DBNull.Value)
                            itogString[17] = itogString[17] + System.Convert.ToDecimal(DT.Rows[i]["sum_charge"]);


                        i++;
                        ExcelRow++;
                    }

                    excells1 = ExcelL.ExlWs.get_Range("C" + ExcelRow.ToString(), "C" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.FormulaR1C1 = "Итого";

                    for (int j = 1; j < 18; j++)
                        ExcelL.ExlWs.Cells[ExcelRow, j + 5] = itogString[j];


                    excells1 = ExcelL.ExlWs.get_Range("C" + ExcelRow.ToString(), "V" + ExcelRow.ToString());
                    excells1.Font.Bold = true;

                    ExcelRow++;

                    excells1 = ExcelL.ExlWs.get_Range("A13", "B" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1 = ExcelL.ExlWs.get_Range("C13", "C13" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1 = ExcelL.ExlWs.get_Range("D13", "D13" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1 = ExcelL.ExlWs.get_Range("E13", "E13" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;


                    excells1 = ExcelL.ExlWs.get_Range("F13", "K" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";
                    excells1 = ExcelL.ExlWs.get_Range("L13", "L" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,0000";
                    excells1 = ExcelL.ExlWs.get_Range("M13", "M" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";
                    excells1 = ExcelL.ExlWs.get_Range("N13", "O" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";
                    excells1 = ExcelL.ExlWs.get_Range("P13", "P" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";
                    excells1 = ExcelL.ExlWs.get_Range("Q13", "Q" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,0000";
                    excells1 = ExcelL.ExlWs.get_Range("R13", "R" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";
                    excells1 = ExcelL.ExlWs.get_Range("U13", "U" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,0000";
                    excells1 = ExcelL.ExlWs.get_Range("V13", "V" + ExcelRow.ToString());
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";

                    excells1 = ExcelL.ExlWs.get_Range("A13", "V" + (ExcelRow - 1).ToString());
                    excells1.Borders[XlBordersIndex.xlEdgeTop].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Borders.Weight = 2;
                    excells1.Borders[XlBordersIndex.xlEdgeRight].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Borders.Weight = 2;
                    excells1.Borders[XlBordersIndex.xlEdgeLeft].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Borders.Weight = 2;
                    excells1.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Borders.Weight = 2;
                    ExcelL.ExlWs.Rows.Font.Name = "Times New Roman";
                    excells1.Font.Size = 11;

                    SetReportSing(ExcelL, ExcelRow + 3, true);

                    #endregion

                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SpravSoderg2" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString() + "_" + DateTime.Now.GetHashCode() + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        ExcelL.ExlWb.Close(false, Type.Missing, Type.Missing);
                        ExcelL.ExlWb = null;
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }

                    #endregion

                }

                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSoderg DataTable : ОШИБКА!";

                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SpravSoderg2" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }

                    //удаление объекта
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                try
                {
                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "SpravSoderg2" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет GetSpravSoderg", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSoderg Excel File : ОШИБКА!";
                }

            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }

            return ret;
        }



        public Returns GetSpravSoderg7(Prm prm_, string Nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            string name_supp = "";

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetSpravSoderg(prm_, out ret, Nzp_user);

            ExcelLoader ExcelL = null;
            try
            {


                if (DT != null)
                {
                    ExcelL = new ExcelLoader();
                    ExcelCreater ExcelCr = new ExcelCreater();


                    #region Создаем шапку
                    Microsoft.Office.Interop.Excel.Range excells1;

                    Dictionary<string, string> Dic = new Dictionary<string, string>();
                    Dic.Add("ERCName", SetReportHeader());

                    Dic.Add("month", Utils.GetMonthName(prm_.month_));//месяц
                    Dic.Add("year", prm_.year_.ToString());//год
                    Dic.Add("DATE", DateTime.Now.ToShortDateString());//год
                    if (prm_.has_pu == "1")
                    {
                        Dic.Add("ODPU", "(все дома)");
                    }
                    else
                        if (prm_.has_pu == "2")
                        {
                            Dic.Add("ODPU", "(дома с ОДПУ)");
                        }
                        else
                        {
                            Dic.Add("ODPU", "(дома без ОДПУ)");
                        }



                    // Массив заголовков
                    List<string> columnHeaderList = new List<string>();
                    columnHeaderList.Add("Начислено (тариф*кол-во), руб.");
                    columnHeaderList.Add("Постоянное начисление, руб.");
                    columnHeaderList.Add("Возврат за услуги, руб.");
                    columnHeaderList.Add("Начислено раз. красным, руб.");
                    columnHeaderList.Add("Начислено раз черным, руб.");
                    columnHeaderList.Add("Итого к оплате, руб.");
                    columnHeaderList.Add("Начислено к оплате, руб.");





                    #endregion


                    #region Определение услуги
                    DbSprav dbSprav = new DbSprav();
                    Finder finder = new Finder();
                    finder.nzp_user = prm_.nzp_serv;
                    finder.RolesVal = new List<_RolesVal>();
                    _RolesVal p = new _RolesVal();
                    p.kod = STCLINE.KP50.Global.Constants.role_sql_serv;
                    p.tip = STCLINE.KP50.Global.Constants.role_sql;
                    p.val = prm_.nzp_serv.ToString();
                    finder.RolesVal.Add(p);
                    List<_Service> lserv = dbSprav.ServiceLoad(finder, out ret);
                    if (lserv.Count > 0)
                        Dic.Add("SERVICE", lserv[0].service);
                    else
                        Dic.Add("SERVICE", "Услуга неопределена");
                    dbSprav.Close();
                    #endregion

                    if (prm_.nzp_key > -1)
                        Dic.Add("NAMESUPP", prm_.name_prm);
                    else
                        Dic.Add("NAMESUPP", "Все поставщики");
                    dbSprav.Close();


                    ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("sprav_soderg.xls"), Dic);
                    ExcelFormater exform = new ExcelFormater();
                    excells1 = ExcelL.ExlWs.get_Range("D3", "L4");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = SetReportConditional(System.Convert.ToInt32(Nzp_user));
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.Font.Size = 8;
                    excells1.RowHeight = 23;

                    #region Пишем тело
                    excells1 = ExcelL.ExlWs.get_Range("F6", "F7");
                    excells1.Merge(Type.Missing);
                    excells1.Font.Size = 6;
                    excells1.FormulaR1C1 = "";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.WrapText = true;

                    int ExcelRow = 8;

                    int ExcelCol = 7;
                    int count_dom = 0;
                    int maxExcelCol = 7;
                    string oldAdres = "";
                    string oldUlica = "";
                    string oldDom = "";
                    string oldLitera = "";
                    string ulica = "";
                    string dom = "";
                    string litera = "";
                    string adres = "";
                    string geu = "";
                    string oldGeu = "";



                    List<Decimal> gilMas = new List<Decimal>();
                    List<Decimal> kubMas = new List<Decimal>();
                    List<string> adresMas = new List<string>();
                    List<Decimal> gilMasDom = new List<Decimal>();
                    List<Decimal> kubMasDom = new List<Decimal>();


                    decimal[,] nachData = new decimal[2, 7];
                    decimal[,] ItonachData = new decimal[2, 7];

                    for (int j = 0; j < 2; j++)
                        for (int k = 0; k < 7; k++)
                        {
                            nachData[j, k] = 0;
                            ItonachData[j, k] = 0;
                        }

                    int i = 0;

                    while (i < DT.Rows.Count)
                    {

                        adres = DT.Rows[i]["ulica"].ToString().Trim() + "," +
                            DT.Rows[i]["ndom"].ToString().Trim() +
                            DT.Rows[i]["nkor"].ToString().Trim();
                        geu = DT.Rows[i]["geu"].ToString().Trim();
                        ulica = DT.Rows[i]["ulica"].ToString().Trim();
                        dom = DT.Rows[i]["ndom"].ToString().Trim();
                        litera = DT.Rows[i]["nkor"].ToString().Trim();

                        if (i == 0)
                        {
                            oldAdres = adres;
                            oldUlica = DT.Rows[i]["ulica"].ToString().Trim();
                            oldDom = DT.Rows[i]["ndom"].ToString().Trim();
                            oldGeu = DT.Rows[i]["geu"].ToString().Trim();
                            oldLitera = DT.Rows[i]["nkor"].ToString().Trim();
                        }

                        #region Записываем Итого по дому
                        if (oldAdres != adres)
                        {
                            count_dom++;
                            adresMas.Clear();
                            adresMas.Add(geu);
                            adresMas.Add(oldUlica);
                            adresMas.Add(oldDom);
                            adresMas.Add(oldLitera);


                            FillOneRowSpravSoderg7(excells1, ExcelRow, count_dom, ExcelL, adresMas,
                            exform, nachData, gilMasDom, kubMasDom);


                            for (int j = 0; j < gilMasDom.Count; j++)
                            {
                                gilMasDom[j] = 0;
                                kubMasDom[j] = 0;

                            }

                            for (int j = 0; j < 2; j++)
                                for (int k = 0; k < 7; k++)
                                {
                                    nachData[j, k] = 0;
                                }


                            ExcelCol = 7;
                            oldAdres = adres;
                            oldUlica = ulica;
                            oldDom = dom;
                            oldGeu = geu;
                            oldLitera = litera;
                            ExcelRow += 2;


                        }
                        #endregion


                        #region Пишем заголовок формул
                        if (ExcelRow == 8)
                        {
                            ExcelL.ExlWs.Cells[7, ExcelCol] = DT.Rows[i]["name_frm"].ToString();
                            excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[ExcelCol - 1].ToString() + "7", exform.BukvaList[ExcelCol - 1].ToString() + "7");
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                            excells1.WrapText = true;
                            excells1.ColumnWidth = 10;
                            excells1.Font.Size = 6;
                        }
                        #endregion

                        if (ExcelCol - 7 >= gilMasDom.Count)
                        {
                            gilMasDom.Add(0);
                            kubMasDom.Add(0);
                            gilMas.Add(0);
                            kubMas.Add(0);
                        }

                        gilMasDom[ExcelCol - 7] = gilMasDom[ExcelCol - 7] + Decimal.Parse(DT.Rows[i]["count_gil"].ToString());
                        kubMasDom[ExcelCol - 7] = kubMasDom[ExcelCol - 7] + Decimal.Parse(DT.Rows[i]["c_calc"].ToString());

                        gilMas[ExcelCol - 7] = gilMas[ExcelCol - 7] + Decimal.Parse(DT.Rows[i]["count_gil"].ToString());
                        kubMas[ExcelCol - 7] = kubMas[ExcelCol - 7] + Decimal.Parse(DT.Rows[i]["c_calc"].ToString());

                        nachData[0, 0] = nachData[0, 0] + Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());
                        nachData[1, 0] = 0;

                        nachData[0, 1] = nachData[0, 1] + Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());
                        nachData[1, 1] = 0;

                        nachData[0, 2] = nachData[0, 2] + Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString());
                        nachData[1, 2] = nachData[1, 2] + Decimal.Parse(DT.Rows[i]["c_nedop"].ToString());

                        nachData[0, 3] = nachData[0, 3] + Decimal.Parse(DT.Rows[i]["reval_k"].ToString());
                        nachData[1, 3] = nachData[1, 3] + Decimal.Parse(DT.Rows[i]["c_reval_k"].ToString());

                        nachData[0, 4] = nachData[0, 4] + Decimal.Parse(DT.Rows[i]["reval_d"].ToString());
                        nachData[1, 4] = nachData[1, 4] + Decimal.Parse(DT.Rows[i]["c_reval_d"].ToString());

                        nachData[0, 5] = nachData[0, 5] + Decimal.Parse(DT.Rows[i]["sum_charge"].ToString());
                        nachData[1, 5] = 0;

                        nachData[0, 6] = nachData[0, 6] + Decimal.Parse(DT.Rows[i]["sum_charge"].ToString());
                        nachData[1, 6] = 0;

                        ItonachData[0, 0] = ItonachData[0, 0] + Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());
                        ItonachData[1, 0] = 0;

                        ItonachData[0, 1] = ItonachData[0, 1] + Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());
                        ItonachData[1, 1] = 0;


                        ItonachData[0, 2] = ItonachData[0, 2] + Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString());
                        ItonachData[1, 2] = ItonachData[1, 2] + Decimal.Parse(DT.Rows[i]["c_nedop"].ToString());

                        ItonachData[0, 3] = ItonachData[0, 3] + Decimal.Parse(DT.Rows[i]["reval_k"].ToString());
                        ItonachData[1, 3] = ItonachData[1, 3] + Decimal.Parse(DT.Rows[i]["c_reval_k"].ToString());

                        ItonachData[0, 4] = ItonachData[0, 4] + Decimal.Parse(DT.Rows[i]["reval_d"].ToString());
                        ItonachData[1, 4] = ItonachData[1, 4] + Decimal.Parse(DT.Rows[i]["c_reval_d"].ToString());

                        ItonachData[0, 5] = ItonachData[0, 5] + Decimal.Parse(DT.Rows[i]["sum_charge"].ToString());
                        ItonachData[1, 5] = 0;

                        ItonachData[0, 6] = ItonachData[0, 6] + Decimal.Parse(DT.Rows[i]["sum_charge"].ToString());
                        ItonachData[1, 6] = 0;


                        ExcelCol++;
                        i++;
                    }

                    count_dom++;
                    if (DT.Rows.Count > 0)
                    {
                        adresMas.Clear();
                        adresMas.Add(geu);
                        adresMas.Add(ulica);
                        adresMas.Add(dom);
                        adresMas.Add(litera);


                        FillOneRowSpravSoderg7(excells1, ExcelRow, count_dom, ExcelL, adresMas,
                        exform, nachData, gilMasDom, kubMasDom);



                        maxExcelCol = ExcelCol;

                        #region Пишем заголовки колонок
                        for (int k = 0; k < columnHeaderList.Count; k++)
                        {
                            excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[ExcelCol - 1 + k].ToString() + "6",
                                exform.BukvaList[ExcelCol - 1 + k].ToString() + "7");
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = columnHeaderList[k];
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                            excells1.WrapText = true;
                            excells1.ColumnWidth = 10;
                        }
                        #endregion

                        excells1 = ExcelL.ExlWs.get_Range("G6", exform.BukvaList[ExcelCol - 2].ToString() + "6");
                        excells1.Merge(Type.Missing);
                        excells1.Font.Size = 6;
                        excells1.FormulaR1C1 = "Количество жильцов/нат.показатель";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excells1.WrapText = true;


                        //    ExcelCol = 7;




                    }
                    #endregion

                    if (DT.Rows.Count > 0)
                    {
                        #region Пишем итого
                        ExcelRow += 2;
                        adresMas.Clear();
                        adresMas.Add("Итого");
                        adresMas.Add("");
                        adresMas.Add("");
                        adresMas.Add("");

                        FillOneRowSpravSoderg7(excells1, ExcelRow, count_dom, ExcelL, adresMas,
                        exform, ItonachData, gilMas, kubMas);


                        #endregion

                        ExcelRow += 1;
                        #region форматируем
                        excells1 = ExcelL.ExlWs.get_Range("A6",
                            exform.BukvaList[ExcelCol + columnHeaderList.Count - 2].ToString() + ExcelRow);
                        excells1.Font.Size = 6;
                        excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                        excells1 = ExcelL.ExlWs.get_Range("G8",
                            exform.BukvaList[ExcelCol + columnHeaderList.Count - 2].ToString() + ExcelRow);
                        excells1.EntireColumn.NumberFormat = "# ##0,00";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        #endregion
                    }

                    SetReportSing(ExcelL, ExcelRow + 3, true);



                    if (prm_.nzp_key != -1)
                    {
                        name_supp = name_supp + prm_.name_prm;
                    }
                    name_supp.Replace(">", " ").Replace("<", " ").Replace("?", " ").Replace("[", " ");
                    name_supp = name_supp.Replace("]", " ").Replace(":", " ").Replace("|", " ").Replace("*", " ").Replace(@"\", " ");

                    name_supp = name_supp.Replace(@"/", " ").Replace(" ", "_");

                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir,
                            prm_.month_.ToString() + "_Дислокация_Водоотведение_" + name_supp +
                             "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        ExcelL.ExlWb.Close();
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }

                    #endregion

                }

                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSoderg DataTable : ОШИБКА!";

                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir,
                            prm_.month_.ToString() + "_Дислокация_Водоотведение_" + name_supp +
                             "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }

                    //удаление объекта
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                try
                {
                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir,
                            prm_.month_.ToString() + "_Дислокация_Водоотведение_" + name_supp +
                             "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет GetSpravSoderg", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSoderg Excel File : ОШИБКА!";
                }

            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }

            return ret;

        }


        // Отчет: Сальдовая оборотная ведомость для всех поставщиков
        public Returns GetSaldoServices(SupgFinder finder, int supp, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            ExcelLoader ExcelL = new ExcelLoader();
            ExcelCreater ExcelCr = new ExcelCreater();
            ExcelFormater ExcelFor = new ExcelFormater();
            ArrayList services = new ArrayList();
            Microsoft.Office.Interop.Excel.Range excell;
            Supg sg = new Supg();
            string po_fact = "";
            string n_supp = "";

            System.Data.DataTable DT_Body = sg.SaldoStatmentServices(finder, supp, out ret);
            System.Data.DataTable DT_Info = sg.GetReportInfo(finder.nzp_user, out ret);

            Dictionary<string, string> Dic = new Dictionary<string, string>();

            string constr = SetReportConditional(finder.nzp_user).ToString().Trim();
            Dic.Add("condition", constr);

            constr = constr.Replace("\n", " ");
            char splitter = ' ';
            string[] tmas_str = constr.Split(splitter);

            string t_str = "";
            int k = 0;
            bool b_area = false, b_geu = false;
            while (k < tmas_str.Count())
            {
                if (tmas_str[k] != "\r")
                {
                    if (tmas_str[k] == "Управляющая организация:")
                    {
                        t_str = "";
                        while (tmas_str[k] != "\r" && tmas_str[k] != "Отделение:")
                        {
                            t_str += tmas_str[k] + " ";
                            k++;
                        }

                        Dic.Add("area", t_str.Substring(11));
                        b_area = true;
                    }
                    if (tmas_str[k] == "Отделение:")
                    {
                        t_str = "";
                        while (tmas_str[k] != "\r" && tmas_str[k] != "\rУлица")
                        {
                            t_str += tmas_str[k] + " ";
                            k++;
                        }
                        Dic.Add("geu", t_str);
                        b_geu = true;
                        break;
                    }
                }
                k++;
            }

            if (!b_area)
            {
                Dic.Add("area", "Все балансодержатели");
            }
            if (!b_geu)
            {
                Dic.Add("geu", "Все участки");
            }
            try
            {
                po_fact = Convert.ToDateTime(finder._date_to).ToString("dd.MM.yyyy");
            }
            catch
            {

            }
            if (supp == -1)
            {
                n_supp += "Все поставщики";
            }
            else
            {
                n_supp += "Поставщик: " + finder.num.ToString();
            }
            string[] months = { "", "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };
            string month = months[int.Parse(Convert.ToDateTime(finder._date_to).ToString("MM"))];
            Dic.Add("supp", n_supp);
            Dic.Add("p1", month + " " + Convert.ToDateTime(finder._date_to).ToString("yyyy"));
            Dic.Add("p5", SetReportConditional(finder.nzp_user).ToString().Trim());
            Dic.Add("date", System.DateTime.Today.ToShortDateString());
            Dic.Add("time", System.DateTime.Now.ToLongTimeString());

            string[,] str;
            object[,] dec;

            try
            {
                str = new string[DT_Body.Rows.Count, 1];
                dec = new object[DT_Body.Rows.Count, 12];
            }
            catch (Exception)
            {
                str = new string[100, 1];
                dec = new object[100, 12];
            }



            Decimal inexec_bf_sum = 0;
            Decimal crt_p_sum = 0;
            Decimal crt_pp_sum = 0;
            Decimal otm_p_sum = 0;
            Decimal fact_p_sum = 0;
            Decimal fact_pp_sum = 0;
            Decimal fact_np_sum = 0;
            Decimal plan_p_sum = 0;
            Decimal fact_s_next = 0;
            Decimal fact_p_next = 0;
            Decimal fact_l_next = 0;

            try
            {
                int indexer = 8;

                ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("SaldoServices.xls"), Dic);

                if (DT_Body.Rows.Count != 0)
                {
                    for (int r = 0; r < DT_Body.Rows.Count; r++)
                    {

                        str[r, 0] = DT_Body.Rows[r][0].ToString();

                        dec[r, 0] = DT_Body.Rows[r][1];
                        inexec_bf_sum += Convert.ToDecimal(DT_Body.Rows[r][1]);

                        dec[r, 1] = DT_Body.Rows[r][2];
                        crt_p_sum += Convert.ToDecimal(DT_Body.Rows[r][2]);

                        dec[r, 2] = DT_Body.Rows[r][3];
                        crt_pp_sum += Convert.ToDecimal(DT_Body.Rows[r][3]);

                        dec[r, 3] = DT_Body.Rows[r][4];
                        otm_p_sum += Convert.ToDecimal(DT_Body.Rows[r][4]);

                        dec[r, 4] = DT_Body.Rows[r][5];
                        plan_p_sum += Convert.ToDecimal(DT_Body.Rows[r][5]);

                        dec[r, 5] = DT_Body.Rows[r][6];
                        fact_p_sum += Convert.ToDecimal(DT_Body.Rows[r][6]);

                        dec[r, 6] = DT_Body.Rows[r][7];
                        fact_np_sum += Convert.ToDecimal(DT_Body.Rows[r][7]);

                        dec[r, 7] = DT_Body.Rows[r][8];
                        fact_s_next += Convert.ToDecimal(DT_Body.Rows[r][8]);

                        dec[r, 8] = DT_Body.Rows[r][9];
                        fact_pp_sum += Convert.ToDecimal(DT_Body.Rows[r][9]);

                        dec[r, 9] = DT_Body.Rows[r][10];
                        fact_l_next += Convert.ToDecimal(DT_Body.Rows[r][10]);

                        dec[r, 10] = DT_Body.Rows[r][11];
                        fact_p_next += Convert.ToDecimal(DT_Body.Rows[r][11]);

                        excell = ExcelL.ExlWs.get_Range("A" + (indexer + r).ToString(), "L" + (indexer + r).ToString());
                        excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                        excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    }
                }
                else
                {
                    excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "L" + (indexer + 4).ToString());
                    excell.Borders[XlBordersIndex.xlEdgeTop].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeRight].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeLeft].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Merge(Type.Missing);
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.Font.Size = 12;
                    excell.Font.Bold = true;
                    excell.Value2 = "нет данных";

                }

                if (DT_Body.Rows.Count != 0)
                {

                    #region Итого


                    excell = ExcelL.ExlWs.get_Range("A8", "A" + (indexer + DT_Body.Rows.Count).ToString());
                    excell.Value2 = str;
                    str = null;

                    excell = ExcelL.ExlWs.get_Range("B8", "L" + (indexer + DT_Body.Rows.Count).ToString());
                    excell.Value2 = dec;
                    dec = null;

                    excell = ExcelL.ExlWs.get_Range("A" + (indexer + DT_Body.Rows.Count).ToString());
                    //excell.ColumnWidth = 25;
                    excell.NumberFormat = "@";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.WrapText = true;
                    excell.Value2 = "Итого";
                    excell.Font.Size = 8;

                    excell = ExcelL.ExlWs.get_Range("B" + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);//
                    excell.Value2 = inexec_bf_sum;

                    excell = ExcelL.ExlWs.get_Range("C" + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.Value2 = crt_p_sum;

                    excell = ExcelL.ExlWs.get_Range("D" + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.Value2 = crt_pp_sum;

                    excell = ExcelL.ExlWs.get_Range("E" + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.Value2 = otm_p_sum;

                    excell = ExcelL.ExlWs.get_Range("F" + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.Value2 = plan_p_sum;

                    excell = ExcelL.ExlWs.get_Range("G" + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.Value2 = fact_p_sum;

                    excell = ExcelL.ExlWs.get_Range("H" + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.Value2 = fact_np_sum;

                    excell = ExcelL.ExlWs.get_Range("I" + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.Value2 = fact_s_next;

                    excell = ExcelL.ExlWs.get_Range("J" + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.Value2 = fact_pp_sum;

                    excell = ExcelL.ExlWs.get_Range("K" + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.Value2 = fact_l_next;

                    excell = ExcelL.ExlWs.get_Range("L" + (indexer + DT_Body.Rows.Count).ToString(), Type.Missing);
                    excell.Value2 = fact_p_next;

                    excell = ExcelL.ExlWs.get_Range("B8", "L" + (indexer + DT_Body.Rows.Count).ToString());
                    // excell.ColumnWidth = 15;
                    excell.NumberFormat = "# ##0,00";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.Font.Size = 8;

                    excell = ExcelL.ExlWs.get_Range("A" + (indexer + DT_Body.Rows.Count).ToString(), "L" + (indexer + DT_Body.Rows.Count).ToString());
                    excell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.Font.Bold = true;

                    excell = ExcelL.ExlWs.get_Range("A2", "L2");
                    excell.Font.Size = 12;



                    SetReportSing(ExcelL, indexer + DT_Body.Rows.Count + 3, false);
                    #endregion
                }
                if (ret.result)
                {
                    fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "Saldo_Services" + finder.nzp_user) + ".xls";
                    //Сохранение
                    ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                    if (InputOutput.useFtp)
                    {
                        fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                    }
                }
                else
                {
                    MonitorLog.WriteLog("Ошибка создания Excel файла : " + ret.text, MonitorLog.typelog.Error, true);

                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Формирование Excel ошибка :" + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;

            }
            finally
            {
                //удаление объекта
                ExcelL.DeleteObject();
            }
            return ret;
        }




        public Returns GetDomNachPere(List<Prm> listprm, string Nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();

            ExcelLoader ExcelL = null;

            //создание dataTable
            ExcelRep ExR = new ExcelRep();

            System.Data.DataTable DT = ExR.GetDomNachReport(listprm, out ret, Nzp_user);

            try
            {
                ExcelL = new ExcelLoader();
                ExcelL.ExlWs.Rows.Font.Name = "Arial";
                if (DT != null)
                {

                    //ExcelCreater ExcelCr = new ExcelCreater();


                    #region Создаем шапку
                    DT.Columns.Remove("nzp_serv");
                    DT.Columns.Remove("idom");
                    Microsoft.Office.Interop.Excel.Range excells1;

                    //спустили шапку
                    excells1 = ExcelL.ExlWs.get_Range("A1", "I1");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "РАСШИФРОВКА ПО ДОМАМ - НАЧИСЛЕНО (перерасчеты)";
                    excells1.Font.Bold = true;
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excells1 = ExcelL.ExlWs.get_Range("A2", "I2");
                    excells1.Merge(Type.Missing);
                    excells1.Font.Bold = true;
                    //string ERC_name = "";

                    //if (STCLINE.KP50.Interfaces.Points.IsDemo == true)
                    //    ERC_name = "Н-ска";
                    //else
                    //    ERC_name = "Самары";

                    //excells1.FormulaR1C1 = "по МП городского округа " + ERC_name + " ''ЕИРЦ'' за " +
                    string s_name = SetReportHeader();
                    excells1.FormulaR1C1 = s_name + " за " +
                        listprm[0].month_.ToString("00") + " " + listprm[0].year_.ToString() + "г.";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excells1 = ExcelL.ExlWs.get_Range("B3", "B3");
                    excells1.Merge(Type.Missing);
                    excells1.Font.Bold = true;
                    excells1.FormulaR1C1 = System.DateTime.Today.ToShortDateString();
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                    excells1 = ExcelL.ExlWs.get_Range("C3", "K4");
                    excells1.Font.Bold = true;
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = SetReportConditional(Convert.ToInt32(Nzp_user)) + "\n" + (listprm[0].isLoadParamInfo ? "С учетом кап.ремонта" : "Без учета кап.ремонта");
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.RowHeight = 25;

                    excells1 = ExcelL.ExlWs.get_Range("A5", "A6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "№ п/п";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.RowHeight = 23;

                    excells1 = ExcelL.ExlWs.get_Range("B5", "B6");
                    excells1.Merge(Type.Missing);
                    //excells1.FormulaR1C1 = "Сальдо 01." + listprm[0].month_.ToString("00") + "." + listprm[0].year_.ToString() + " г.";
                    excells1.FormulaR1C1 = "Адрес";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;


                    excells1 = ExcelL.ExlWs.get_Range("C5", "C6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Оплачив." + System.Convert.ToChar(10) + " площ. " + System.Convert.ToChar(10) + " кв.м.";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                    excells1 = ExcelL.ExlWs.get_Range("D5", "D6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Кол-во " + System.Convert.ToChar(10) + " прож-х";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                    excells1 = ExcelL.ExlWs.get_Range("E5", "E6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Кол-во " + System.Convert.ToChar(10) + " ЛС";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                    excells1 = ExcelL.ExlWs.get_Range("F5", "F6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = " ";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;


                    excells1 = ExcelL.ExlWs.get_Range("G5", "G6");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = "Сумма всего";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                    //excells1 = ExcelL.ExlWs.get_Range("H5", "I5");
                    //excells1.Merge(Type.Missing);
                    //excells1.FormulaR1C1 = "В том числе по видам услуг";
                    //excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    //excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                    excells1 = ExcelL.ExlWs.get_Range("A5", "I6");
                    excells1.Font.Size = 8;

                    #endregion

                    if (DT.Rows.Count > 0)
                    {


                        #region Преподготовка

                        string old_dom = DT.Rows[0]["nzp_dom"].ToString();
                        int serv_count = 0;
                        ExcelFormater exform = new ExcelFormater();
                        //       string str_source;
                        //      string [] str_temp;

                        while ((serv_count < DT.Rows.Count) & (DT.Rows[serv_count]["nzp_dom"].ToString() == old_dom))
                        {
                            excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[serv_count + 7].ToString() + "6",
                                exform.BukvaList[serv_count + 7].ToString() + "6");

                            excells1.FormulaR1C1 = DT.Rows[serv_count]["service"].ToString().Trim();
                            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                            excells1.Font.Size = 8;
                            excells1.ColumnWidth = 12;
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.WrapText = true;
                            serv_count++;
                        }

                        excells1 = ExcelL.ExlWs.Range["H5", exform.BukvaList[serv_count + 6] + "5"];
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = "В том числе по видам услуг";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                        excells1.Columns.AutoFit();

                        #endregion

                        #region Пишем тело

                        decimal[] sum_colls = new decimal[serv_count];

                        int ExcelRow = 7;
                        int ExcelCol;
                        int i = 0;
                        decimal sum_nedop;
                        decimal sum_odn;
                        decimal sum_charge;
                        decimal rsum_tarif;
                        decimal reval_k;
                        decimal reval_d;
                        while (i < DT.Rows.Count)
                        {

                            //Номер п/п
                            excells1 = ExcelL.ExlWs.get_Range("A" + ExcelRow.ToString(), "A" + (ExcelRow + 5).ToString());
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = ((ExcelRow - 3) / 6).ToString();
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                            //Адрес дома
                            excells1 = ExcelL.ExlWs.get_Range("B" + ExcelRow.ToString(), "B" + (ExcelRow + 5).ToString());
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = DT.Rows[i]["ulica"].ToString().Trim() + " " + DT.Rows[i]["ndom"].ToString().Trim();
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                            //Площадь
                            excells1 = ExcelL.ExlWs.get_Range("C" + ExcelRow.ToString(), "C" + (ExcelRow + 5).ToString());
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = Decimal.Parse(DT.Rows[i]["pl_kvar"].ToString());
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                            //Количество жильцов
                            excells1 = ExcelL.ExlWs.get_Range("D" + ExcelRow.ToString(), "D" + (ExcelRow + 5).ToString());
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = DT.Rows[i]["count_gil"].ToString();
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                            //Количество ЛС
                            excells1 = ExcelL.ExlWs.get_Range("E" + ExcelRow.ToString(), "E" + (ExcelRow + 5).ToString());
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = DT.Rows[i]["count_ls"].ToString();
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                            ExcelL.ExlWs.Cells[ExcelRow, 6] = "Начислено";
                            ExcelL.ExlWs.Cells[ExcelRow + 1, 6] = "в т.ч. ОДН";
                            ExcelL.ExlWs.Cells[ExcelRow + 2, 6] = "возврат";
                            ExcelL.ExlWs.Cells[ExcelRow + 3, 6] = "раз. красн.";
                            ExcelL.ExlWs.Cells[ExcelRow + 4, 6] = "раз. черн. ";
                            ExcelL.ExlWs.Cells[ExcelRow + 5, 6] = "Итого";


                            ExcelCol = 8;
                            sum_charge = 0;
                            rsum_tarif = 0;
                            sum_odn = 0;
                            sum_nedop = 0;
                            reval_k = 0;
                            reval_d = 0;

                            while ((i < DT.Rows.Count) & (old_dom == DT.Rows[i]["nzp_dom"].ToString()) )  // 
                            {

                                ExcelL.ExlWs.Cells[ExcelRow, ExcelCol] = DT.Rows[i]["rsum_tarif"];
                                ExcelL.ExlWs.Cells[ExcelRow + 1, ExcelCol] = DT.Rows[i]["sum_odn"];
                                ExcelL.ExlWs.Cells[ExcelRow + 2, ExcelCol] = DT.Rows[i]["sum_nedop"];
                                ExcelL.ExlWs.Cells[ExcelRow + 3, ExcelCol] = DT.Rows[i]["reval_k"];
                                ExcelL.ExlWs.Cells[ExcelRow + 4, ExcelCol] = DT.Rows[i]["reval_d"];
                                ExcelL.ExlWs.Cells[ExcelRow + 5, ExcelCol] = DT.Rows[i]["sum_charge"];

                                sum_nedop = sum_nedop + Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString());
                                sum_odn = sum_odn + Decimal.Parse(DT.Rows[i]["sum_odn"].ToString());
                                sum_charge = sum_charge + Decimal.Parse(DT.Rows[i]["sum_charge"].ToString());
                                rsum_tarif = rsum_tarif + Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());
                                reval_d = reval_d + Decimal.Parse(DT.Rows[i]["reval_d"].ToString());
                                reval_k = reval_k + Decimal.Parse(DT.Rows[i]["reval_k"].ToString());

                                ExcelCol++;
                                i++;
                                if (i == DT.Rows.Count) break;
                            }
                            //if (i == DT.Rows.Count - 1)
                            //{
                            //    ExcelL.ExlWs.Cells[ExcelRow, ExcelCol] = DT.Rows[i]["rsum_tarif"];
                            //    ExcelL.ExlWs.Cells[ExcelRow + 1, ExcelCol] = DT.Rows[i]["sum_odn"];
                            //    ExcelL.ExlWs.Cells[ExcelRow + 2, ExcelCol] = DT.Rows[i]["sum_nedop"];
                            //    ExcelL.ExlWs.Cells[ExcelRow + 3, ExcelCol] = DT.Rows[i]["reval_k"];
                            //    ExcelL.ExlWs.Cells[ExcelRow + 4, ExcelCol] = DT.Rows[i]["reval_d"];
                            //    ExcelL.ExlWs.Cells[ExcelRow + 5, ExcelCol] = DT.Rows[i]["sum_charge"];

                            //    sum_nedop = sum_nedop + Decimal.Parse(DT.Rows[i]["sum_nedop"].ToString());
                            //    sum_odn = sum_odn + Decimal.Parse(DT.Rows[i]["sum_odn"].ToString());
                            //    sum_charge = sum_charge + Decimal.Parse(DT.Rows[i]["sum_charge"].ToString());
                            //    rsum_tarif = rsum_tarif + Decimal.Parse(DT.Rows[i]["rsum_tarif"].ToString());
                            //    reval_d = reval_d + Decimal.Parse(DT.Rows[i]["reval_d"].ToString());
                            //    reval_k = reval_k + Decimal.Parse(DT.Rows[i]["reval_k"].ToString());

                            //}

                            ExcelL.ExlWs.Cells[ExcelRow, 7] = rsum_tarif;
                            ExcelL.ExlWs.Cells[ExcelRow + 1, 7] = sum_odn;
                            ExcelL.ExlWs.Cells[ExcelRow + 2, 7] = sum_nedop;
                            ExcelL.ExlWs.Cells[ExcelRow + 3, 7] = reval_k;
                            ExcelL.ExlWs.Cells[ExcelRow + 4, 7] = reval_d;
                            ExcelL.ExlWs.Cells[ExcelRow + 5, 7] = sum_charge;

                            if (i == DT.Rows.Count) { ExcelRow = ExcelRow + 6; break; }
                            old_dom = DT.Rows[i]["nzp_dom"].ToString();

                            ExcelRow = ExcelRow + 6;

                        }
                        #endregion

                        #region Пишем подвал

                        if (ExcelRow > 7)
                        {
                            excells1 = ExcelL.ExlWs.get_Range("A" + (ExcelRow - 6), exform.BukvaList[serv_count + 6] + (ExcelRow - 1));
                            excells1.Font.Bold = true;
                        }

                        excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[6] + "5", exform.BukvaList[serv_count + 6] + "5");
                        excells1.Borders.LineStyle = XlLineStyle.xlContinuous;



                        excells1 = ExcelL.ExlWs.get_Range(exform.BukvaList[6] + "7", exform.BukvaList[serv_count + 6] + (ExcelRow - 1));
                        excells1.EntireColumn.NumberFormat = "# ##0,00";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                        excells1 = ExcelL.ExlWs.get_Range("C7", "C" + (ExcelRow - 1));
                        excells1.EntireColumn.NumberFormat = "# ##0,00";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;

                        excells1 = ExcelL.ExlWs.get_Range("A5", exform.BukvaList[serv_count + 6] + (ExcelRow - 1));
                        excells1.Font.Size = 8;
                        //excells1.Columns.AutoFit();
                        //excells1.EntireColumn.AutoFit();
                        excells1 = ExcelL.ExlWs.get_Range("A5", "G" + (ExcelRow - 1));
                        excells1.Columns.AutoFit();


                        excells1 = ExcelL.ExlWs.get_Range("B7", "B" + (ExcelRow - 1));
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;

                        excells1 = ExcelL.ExlWs.get_Range("F5", "F" + (ExcelRow - 1));
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                        SetReportSing(ExcelL, ExcelRow + 3, false);
                        //ExcelL.ExlWs.Cells[ExcelRow + 3, 2] = "Директор";
                        //ExcelL.ExlWs.Cells[ExcelRow + 5, 2] = "Начальник ПЭО";
                        //ExcelL.ExlWs.Cells[ExcelRow + 7, 2] = "Начальник ОНУПН";
                        //ExcelL.ExlWs.Cells[ExcelRow + 3, 3] = " Мякишев М.В.";
                        //ExcelL.ExlWs.Cells[ExcelRow + 5, 3] = " Соковых И.А.";
                        //ExcelL.ExlWs.Cells[ExcelRow + 7, 3] = " Старкова Л.А";

                        #endregion
                    }
                    else
                    {

                        excells1 = ExcelL.ExlWs.get_Range("A5", "I5");
                        excells1.EntireColumn.AutoFit();
                        ExcelL.ExlWs.Cells[10, 2] = "Статистика по домам по начислениям не подсчитана";
                    }

                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "DomNachPere" +
                            listprm[0].nzp_area.ToString() + "_" +
                            listprm[0].nzp_geu.ToString() + "_" +
                            listprm[0].year_.ToString() + "_" +
                            listprm[0].month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        ExcelL.ExlWb.Close();
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }
                    finally
                    {
                        ////удаление объекта
                        //ExcelL.DeleteObject();
                    }
                    #endregion

                }
                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetDomNach DataTable : ОШИБКА!";
                    ////удаление объекта
                    //ExcelL.DeleteObject();
                    return ret;
                }

            }
            catch
            {
                MonitorLog.WriteLog("Ошибка при формирование DomNach", MonitorLog.typelog.Warn, true);
                ret.result = false;
                ret.text = "Создание GetDomNachPere DataTable : ОШИБКА!";
                ////удаление объекта
                //ExcelL.DeleteObject();
                return ret;

            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }
            return ret;

        }


        public Returns GetProtCalcOdn(Prm prm_, string Nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.ProtCalcOdn(prm_, out ret, Nzp_user);

            ExcelLoader ExcelL = null;
            try
            {


                if (DT != null)
                {
                    ExcelL = new ExcelLoader();
                    ExcelCreater ExcelCr = new ExcelCreater();


                    #region Создаем шапку
                    Microsoft.Office.Interop.Excel.Range excells1;

                    Dictionary<string, string> Dic = new Dictionary<string, string>();
                    Dic.Add("ERCName", SetReportHeader());


                    Dic.Add("month", Utils.GetMonthName(prm_.month_));//месяц
                    Dic.Add("year", prm_.year_.ToString());//год
                    Dic.Add("DATE", DateTime.Now.ToShortDateString());//год


                    ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("prot_calc_odn.xls"), Dic);
                    ExcelFormater exform = new ExcelFormater();
                    excells1 = ExcelL.ExlWs.get_Range("D3", "H4");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = SetReportConditional(System.Convert.ToInt32(Nzp_user));
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.Font.Size = 8;
                    excells1.RowHeight = 23;
                    #endregion

                    #region Пишем тело


                    int ExcelRow = 8;
                    int lastExcelRow = 8;
                    int i = 0;
                    string old_adres = "";
                    string adres = "";

                    while (i < DT.Rows.Count)
                    {
                        if (old_adres == "")
                        {
                            old_adres = DT.Rows[i]["ulica"].ToString().Trim() + "," +
                            DT.Rows[i]["ndom"].ToString().Trim() +
                            DT.Rows[i]["nkor"].ToString().Trim();
                        }
                        adres = DT.Rows[i]["ulica"].ToString().Trim() + "," +
                            DT.Rows[i]["ndom"].ToString().Trim() +
                            DT.Rows[i]["nkor"].ToString().Trim();
                        if (old_adres != adres)
                        {
                            excells1 = ExcelL.ExlWs.get_Range("A" + lastExcelRow.ToString(), "A" + (ExcelRow - 1).ToString());
                            excells1.Merge(Type.Missing);
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                            excells1.WrapText = true;

                            excells1 = ExcelL.ExlWs.get_Range("B" + lastExcelRow.ToString(), "B" + (ExcelRow - 1).ToString());
                            excells1.Merge(Type.Missing);
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                            excells1.WrapText = true;

                            excells1 = ExcelL.ExlWs.get_Range("C" + lastExcelRow.ToString(), "C" + (ExcelRow - 1).ToString());
                            excells1.Merge(Type.Missing);
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                            excells1.WrapText = true;

                            lastExcelRow = ExcelRow;
                        }

                        ExcelL.ExlWs.Cells[ExcelRow, 1] = DT.Rows[i]["geu"].ToString().Trim().Replace("ЖЭУ", "");
                        ExcelL.ExlWs.Cells[ExcelRow, 2] = DT.Rows[i]["area"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 3] = adres;
                        ExcelL.ExlWs.Cells[ExcelRow, 4] = DT.Rows[i]["litera"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 5] = "01." + prm_.month_.ToString() + "." + prm_.year_.ToString();
                        if (Decimal.Parse(DT.Rows[i]["kf_hv"].ToString().Trim()) != 0)
                            ExcelL.ExlWs.Cells[ExcelRow, 6] = Decimal.Parse(DT.Rows[i]["kf_hv"].ToString().Trim());
                        if (Decimal.Parse(DT.Rows[i]["kf_gv"].ToString().Trim()) != 0)
                            ExcelL.ExlWs.Cells[ExcelRow, 7] = Decimal.Parse(DT.Rows[i]["kf_gv"].ToString().Trim());
                        if (Decimal.Parse(DT.Rows[i]["kf_el"].ToString().Trim()) != 0)
                            ExcelL.ExlWs.Cells[ExcelRow, 8] = Decimal.Parse(DT.Rows[i]["kf_el"].ToString().Trim());
                        i++;
                        ExcelRow++;

                    }



                    #endregion


                    #region форматируем
                    excells1 = ExcelL.ExlWs.get_Range("A8", "H" + (ExcelRow - 1).ToString());
                    excells1.Font.Size = 6;
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                    excells1 = ExcelL.ExlWs.get_Range("F8", "H" + (ExcelRow - 1).ToString());
                    excells1.EntireColumn.NumberFormat = "# ##0,0000";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    #endregion


                    SetReportSing(ExcelL, ExcelRow + 3, false);


                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "ProtCalcOdn" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString() + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }

                    #endregion

                }

                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSoderg DataTable : ОШИБКА!";

                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "ProtCalcOdn" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }

                    //удаление объекта
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                try
                {
                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "ProtCalcOdn" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет ProtCalcOdn", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSoderg Excel File : ОШИБКА!";
                }

            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }

            return ret;

        }



        // Отчет: Реестр счетчиков лицевых счетов
        public Returns GetRegisterCounters(SupgFinder finder, ref string fileName)
        {
            Returns ret = Utils.InitReturns();
            ExcelLoader ExcelL = new ExcelLoader();
            ExcelCreater ExcelCr = new ExcelCreater();
            ExcelFormater ExcelFor = new ExcelFormater();
            ArrayList services = new ArrayList();
            Microsoft.Office.Interop.Excel.Range excell;
            Supg sg = new Supg();
            string s_fact = "";
            string po_fact = "";

            System.Data.DataTable DT_Body = sg.GetRegisterCounters(finder, out ret);
            System.Data.DataTable DT_Info = sg.GetReportInfo(finder.nzp_user, out ret);

            Dictionary<string, string> Dic = new Dictionary<string, string>();

            try
            {
                po_fact = Convert.ToDateTime(finder._date_to).ToString("dd.MM.yyyy");
                s_fact = Convert.ToDateTime(finder._date_from).ToString("dd.MM.yyyy");
            }
            catch
            {

            }

            string constr = SetReportConditional(finder.nzp_user).ToString().Trim();
            Dic.Add("condition", constr);

            constr = constr.Replace("\n", " ");
            char splitter = ' ';
            string[] tmas_str = constr.Split(splitter);

            string t_str = "";
            int k = 0;
            bool b_area = false, b_geu = false;
            while (k < tmas_str.Count())
            {
                if (tmas_str[k] != "\r")
                {
                    if (tmas_str[k] == "Управляющая организация:")
                    {
                        t_str = "";
                        while (tmas_str[k] != "\r" && tmas_str[k] != "Отделение:")
                        {
                            t_str += tmas_str[k] + " ";
                            k++;
                        }
                        Dic.Add("area", t_str);
                        b_area = true;
                    }
                    if (tmas_str[k] == "Отделение:")
                    {
                        t_str = "";
                        while (tmas_str[k] != "\r" && tmas_str[k] != "\rУлица")
                        {
                            t_str += tmas_str[k] + " ";
                            k++;
                        }
                        Dic.Add("geu", t_str);
                        b_geu = true;
                        break;
                    }
                }
                k++;
            }

            if (!b_area)
            {
                Dic.Add("area", "Управляющая организация: По выбранному списку");
            }
            if (!b_geu)
            {
                Dic.Add("geu", "Отделение: По выбранному списку");
            }

            Dic.Add("p1", finder._date_to);
            Dic.Add("date", System.DateTime.Today.ToShortDateString());
            Dic.Add("time", System.DateTime.Now.ToShortTimeString());
            if (finder.nzp_serv == 0)
            {
                Dic.Add("serv", "Все услуги");
            }
            else
            {
                Dic.Add("serv", finder.num);
            }


            try
            {
                int indexer = 8;
                string[,] mas_adress = new string[DT_Body.Rows.Count, 1];
                object[,] mas_num_ls = new object[DT_Body.Rows.Count, 1];
                string[,] mas_serv = new string[DT_Body.Rows.Count, 1];
                string[,] mas_type_cnt = new string[DT_Body.Rows.Count, 1];
                object[,] mas_num_cnt = new object[DT_Body.Rows.Count, 1];
                object[,] mas_stage = new object[DT_Body.Rows.Count, 1];
                string[,] mas_first_date = new string[DT_Body.Rows.Count, 1];
                object[,] mas_first_val = new object[DT_Body.Rows.Count, 1];
                string[,] mas_last_date = new string[DT_Body.Rows.Count, 1];
                object[,] mas_last_val = new object[DT_Body.Rows.Count, 1];
                object[,] mas_average = new object[DT_Body.Rows.Count, 1];
                string[,] mas_date_pov = new string[DT_Body.Rows.Count, 1];
                string[,] mas_date_next_pov = new string[DT_Body.Rows.Count, 1];


                ExcelL.LoadTemlate(PathHelper.GetReportTemplatePath("reg_cnt.xls"), Dic);


                int r = 0;
                if (DT_Body.Rows.Count != 0)
                {
                    string old_ls = "";

                    int ls_count = 0;
                    for (r = 0; r < DT_Body.Rows.Count; r++)
                    {

                        #region заполнение файла

                        string str = "";
                        str += "ул. " + DT_Body.Rows[r][15].ToString().Trim() + " д. " + DT_Body.Rows[r][16].ToString().Trim();
                        if (DT_Body.Rows[r][17].ToString().Trim() != "-")
                        {
                            str += " кор. " + DT_Body.Rows[r][17].ToString().Trim();
                        }
                        str += " кв. " + DT_Body.Rows[r][18].ToString().Trim();
                        mas_adress[r, 0] = str.Trim();


                        mas_num_ls[r, 0] = DT_Body.Rows[r][0];
                        if (DT_Body.Rows[r][0].ToString() != old_ls)
                        {
                            ls_count++;
                        }
                        old_ls = DT_Body.Rows[r][0].ToString();


                        mas_serv[r, 0] = DT_Body.Rows[r][14].ToString();


                        mas_type_cnt[r, 0] = DT_Body.Rows[r][5].ToString();


                        mas_num_cnt[r, 0] = DT_Body.Rows[r][2];


                        mas_stage[r, 0] = DT_Body.Rows[r][6];


                        mas_first_date[r, 0] = DT_Body.Rows[r][7].ToString().Substring(0, 10);


                        mas_first_val[r, 0] = DT_Body.Rows[r][8];


                        mas_last_date[r, 0] = DT_Body.Rows[r][9].ToString().Substring(0, 10);


                        mas_last_val[r, 0] = DT_Body.Rows[r][10];


                        mas_average[r, 0] = DT_Body.Rows[r][11];

                        if (DT_Body.Rows[r][12].ToString() != "")
                        {
                            mas_date_pov[r, 0] = DT_Body.Rows[r][12].ToString().Substring(0, 10);
                        }

                        if (DT_Body.Rows[r][13].ToString() != "")
                        {
                            mas_date_next_pov[r, 0] = DT_Body.Rows[r][13].ToString().Substring(0, 10);
                        }

                    }

                    excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "A" + (indexer + r - 1).ToString());
                    excell.Value2 = mas_adress;
                    excell = ExcelL.ExlWs.get_Range("B" + indexer.ToString(), "B" + (indexer + r - 1).ToString());
                    excell.Value2 = mas_num_ls;
                    excell = ExcelL.ExlWs.get_Range("C" + indexer.ToString(), "C" + (indexer + r - 1).ToString());
                    excell.Value2 = mas_serv;
                    excell = ExcelL.ExlWs.get_Range("D" + indexer.ToString(), "D" + (indexer + r - 1).ToString());
                    excell.Value2 = mas_type_cnt;
                    excell = ExcelL.ExlWs.get_Range("E" + indexer.ToString(), "E" + (indexer + r - 1).ToString());
                    excell.Value2 = mas_num_cnt;
                    excell = ExcelL.ExlWs.get_Range("F" + indexer.ToString(), "F" + (indexer + r - 1).ToString());
                    excell.Value2 = mas_stage;
                    excell = ExcelL.ExlWs.get_Range("G" + indexer.ToString(), "G" + (indexer + r - 1).ToString());
                    excell.Value2 = mas_first_date;
                    excell = ExcelL.ExlWs.get_Range("H" + indexer.ToString(), "H" + (indexer + r - 1).ToString());
                    excell.Value2 = mas_first_val;
                    excell = ExcelL.ExlWs.get_Range("I" + indexer.ToString(), "I" + (indexer + r - 1).ToString());
                    excell.Value2 = mas_last_date;
                    excell = ExcelL.ExlWs.get_Range("J" + indexer.ToString(), "J" + (indexer + r - 1).ToString());
                    excell.Value2 = mas_last_val;
                    excell = ExcelL.ExlWs.get_Range("K" + indexer.ToString(), "K" + (indexer + r - 1).ToString());
                    excell.Value2 = mas_average;
                    excell = ExcelL.ExlWs.get_Range("L" + indexer.ToString(), "L" + (indexer + r - 1).ToString());
                    excell.Value2 = mas_date_pov;
                    excell = ExcelL.ExlWs.get_Range("M" + indexer.ToString(), "M" + (indexer + r - 1).ToString());
                    excell.Value2 = mas_date_next_pov;


                    excell = ExcelL.ExlWs.get_Range("A" + (indexer + r).ToString(), "C" + (indexer + r).ToString());
                    excell.Merge();
                    excell.Value2 = "Число записей: " + r.ToString();
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.Font.Size = 8;

                    excell = ExcelL.ExlWs.get_Range("A" + (indexer + r + 1).ToString(), "C" + (indexer + r + 1).ToString());
                    excell.Merge();
                    excell.Value2 = "Число личных счетов: " + ls_count.ToString();
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.Font.Size = 8;


                    excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "A" + (indexer + r - 1).ToString());
                    excell.NumberFormat = "@";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excell = ExcelL.ExlWs.get_Range("B" + indexer.ToString(), "B" + (indexer + r - 1).ToString());
                    excell.NumberFormat = "0";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excell = ExcelL.ExlWs.get_Range("C" + indexer.ToString(), "C" + (indexer + r - 1).ToString());
                    excell.NumberFormat = "@";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excell = ExcelL.ExlWs.get_Range("D" + indexer.ToString(), "D" + (indexer + r - 1).ToString());
                    excell.NumberFormat = "@";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excell = ExcelL.ExlWs.get_Range("E" + indexer.ToString(), "E" + (indexer + r - 1).ToString());
                    excell.NumberFormat = "0";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excell = ExcelL.ExlWs.get_Range("F" + indexer.ToString(), "F" + (indexer + r - 1).ToString());
                    excell.NumberFormat = "0";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excell = ExcelL.ExlWs.get_Range("G" + indexer.ToString(), "G" + (indexer + r - 1).ToString());
                    excell.NumberFormat = "@";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excell = ExcelL.ExlWs.get_Range("H" + indexer.ToString(), "H" + (indexer + r - 1).ToString());
                    excell.NumberFormat = "0,00";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excell = ExcelL.ExlWs.get_Range("I" + indexer.ToString(), "I" + (indexer + r - 1).ToString());
                    excell.NumberFormat = "@";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excell = ExcelL.ExlWs.get_Range("J" + indexer.ToString(), "J" + (indexer + r - 1).ToString());
                    excell.NumberFormat = "0,00";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excell = ExcelL.ExlWs.get_Range("K" + indexer.ToString(), "K" + (indexer + r - 1).ToString());
                    excell.NumberFormat = "0,00";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

                    excell = ExcelL.ExlWs.get_Range("L" + indexer.ToString(), "M" + (indexer + r - 1).ToString());
                    excell.NumberFormat = "@";
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;



                    excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "M" + (indexer + r - 1).ToString());
                    excell.Borders[XlBordersIndex.xlEdgeTop].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 2;
                    excell.Borders[XlBordersIndex.xlEdgeRight].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 2;
                    excell.Borders[XlBordersIndex.xlEdgeLeft].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 2;
                    excell.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 2;
                    excell.Font.Size = 8;

                }
                else
                {
                    excell = ExcelL.ExlWs.get_Range("A" + indexer.ToString(), "M" + (indexer + 4).ToString());
                    excell.Borders[XlBordersIndex.xlEdgeTop].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeRight].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeLeft].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Borders[XlBordersIndex.xlEdgeBottom].LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excell.Borders.Weight = 3;
                    excell.Merge(Type.Missing);
                    excell.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excell.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    //   excell.Font.Size = 12;
                    excell.Font.Bold = true;
                    excell.Value2 = "нет данных";
                }

                SetReportSing(ExcelL, indexer + r + 3, false);

                        #endregion


                if (ret.result)
                {
                    fileName = ReportGen.GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "regcnt" + finder.nzp_user) + ".xls";
                    //Сохранение
                    ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                    if (InputOutput.useFtp)
                    {
                        fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                    }
                }
                else
                {
                    MonitorLog.WriteLog("Ошибка создания Excel файла : " + ret.text, MonitorLog.typelog.Error, true);

                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Формирование Excel ошибка :" + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;

            }
            finally
            {
                //удаление объекта
                ExcelL.DeleteObject();
            }
            return ret;
        }


        public Returns GetPaspRas(Prm prm_, string Nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetPaspRas(prm_, out ret, Nzp_user);

            ExcelLoader ExcelL = null;
            try
            {
                ExcelL = new ExcelLoader();
                if (DT != null)
                {
                    ExcelCreater ExcelCr = new ExcelCreater();

                    #region Создаем шапку
                    Microsoft.Office.Interop.Excel.Range excells1;

                    Dictionary<string, string> Dic = new Dictionary<string, string>();
                    Dic.Add("ERCName", SetReportHeader());
                    string strMonthName = "";

                    #region Получаем название месяца

                    switch (prm_.month_)
                    {
                        case 1:
                            strMonthName = "ЯНВАРЬ";
                            break;
                        case 2:
                            strMonthName = "ФЕВРАЛЬ";
                            break;
                        case 3:
                            strMonthName = "МАРТ";
                            break;
                        case 4:
                            strMonthName = "АПРЕЛЬ";
                            break;
                        case 5:
                            strMonthName = "МАЙ";
                            break;
                        case 6:
                            strMonthName = "ИЮНЬ";
                            break;
                        case 7:
                            strMonthName = "ИЮЛЬ";
                            break;
                        case 8:
                            strMonthName = "АВГУСТ";
                            break;
                        case 9:
                            strMonthName = "СЕНТЯБРЬ";
                            break;
                        case 10:
                            strMonthName = "ОКТЯБРЬ";
                            break;
                        case 11:
                            strMonthName = "НОЯБРЬ";
                            break;
                        case 12:
                            strMonthName = "ДЕКАБРЬ";
                            break;
                        default:
                            break;
                    }
                    #endregion

                    Dic.Add("month", strMonthName);//месяц
                    Dic.Add("year", prm_.year_.ToString());//год
                    Dic.Add("DATE", DateTime.Now.ToShortDateString());//год
                    #endregion

                    ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("pasp_ras.xls"), Dic);
                    excells1 = ExcelL.ExlWs.get_Range("D3", "K4");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = SetReportConditional(System.Convert.ToInt32(Nzp_user));
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.Font.Size = 8;
                    excells1.RowHeight = 23;

                    #region Пишем тело

                    int ExcelRow = 7;
                    int i = 0;

                    while (i < DT.Rows.Count)
                    {

                        ExcelL.ExlWs.Cells[ExcelRow, 1] = i + 1;
                        ExcelL.ExlWs.Cells[ExcelRow, 2] = DT.Rows[i]["num_ls"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 3] = DT.Rows[i]["ulica"].ToString().Trim().Replace("/ -", "");
                        ExcelL.ExlWs.Cells[ExcelRow, 4] = DT.Rows[i]["ndom"].ToString().Trim().Replace("-", " ");
                        ExcelL.ExlWs.Cells[ExcelRow, 5] = DT.Rows[i]["nkor"].ToString().Trim().Replace("-", " ");
                        ExcelL.ExlWs.Cells[ExcelRow, 6] = DT.Rows[i]["nkvar"].ToString().Trim().Replace("-", " ");
                        ExcelL.ExlWs.Cells[ExcelRow, 7] = DT.Rows[i]["nkvar_n"].ToString().Trim().Replace("-", " ");
                        ExcelL.ExlWs.Cells[ExcelRow, 8] = DT.Rows[i]["fio"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 9] = DT.Rows[i]["sost"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 10] = DT.Rows[i]["kol_gil"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 11] = DT.Rows[i]["kol_prm"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 12] = DT.Rows[i]["kol_prm_pasp"].ToString();
                        //                        ExcelL.ExlWs.Cells[ExcelRow, 7] = Decimal.Parse(DT.Rows[i]["sum_insaldo"].ToString());
                        ExcelRow++;
                        i++;
                    }
                    #endregion

                    #region форматируем
                    if (ExcelRow > 7) ExcelRow--;
                    //excells1 = ExcelL.ExlWs.get_Range("G7", "J" + ExcelRow.ToString());
                    //excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    //excells1.EntireColumn.NumberFormat = "# ##0,00";

                    excells1 = ExcelL.ExlWs.get_Range("A7", "L" + ExcelRow.ToString());
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Font.Size = 8;

                    //                    SetReportSing(ExcelL, ExcelRow+3);

                    #endregion

                    SetReportSing(ExcelL, ExcelRow + 3, false);

                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "PaspRas" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString() + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }

                    #endregion
                }
                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetPaspRas DataTable : ОШИБКА!";

                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "PaspRas" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                try
                {
                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "PaspRas" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет GetPaspRas", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetPaspRas Excel File : ОШИБКА!";
                }
            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }
            return ret;
        }



        public Returns GetDolgSpis(Prm prm_, string Nzp_user, ref string fileName)
        {
            Returns ret = Utils.InitReturns();

            //создание dataTable
            ExcelRep ExR = new ExcelRep();
            System.Data.DataTable DT = ExR.GetDolgSpis(prm_, out ret, Nzp_user);

            ExcelLoader ExcelL = null;
            try
            {
                ExcelL = new ExcelLoader();

                if (DT != null)
                {

                    ExcelCreater ExcelCr = new ExcelCreater();


                    #region Создаем шапку
                    Microsoft.Office.Interop.Excel.Range excells1;

                    Dictionary<string, string> Dic = new Dictionary<string, string>();
                    Dic.Add("ERCName", SetReportHeader());
                    string strMonthName = "";

                    #region Получаем название месяца

                    switch (prm_.month_)
                    {
                        case 1:
                            strMonthName = "ЯНВАРЬ";
                            break;
                        case 2:
                            strMonthName = "ФЕВРАЛЬ";
                            break;
                        case 3:
                            strMonthName = "МАРТ";
                            break;
                        case 4:
                            strMonthName = "АПРЕЛЬ";
                            break;
                        case 5:
                            strMonthName = "МАЙ";
                            break;
                        case 6:
                            strMonthName = "ИЮНЬ";
                            break;
                        case 7:
                            strMonthName = "ИЮЛЬ";
                            break;
                        case 8:
                            strMonthName = "АВГУСТ";
                            break;
                        case 9:
                            strMonthName = "СЕНТЯБРЬ";
                            break;
                        case 10:
                            strMonthName = "ОКТЯБРЬ";
                            break;
                        case 11:
                            strMonthName = "НОЯБРЬ";
                            break;
                        case 12:
                            strMonthName = "ДЕКАБРЬ";
                            break;
                        default:
                            break;
                    }
                    #endregion

                    Dic.Add("month", strMonthName);//месяц
                    Dic.Add("year", prm_.year_.ToString());//год
                    Dic.Add("DATE", DateTime.Now.ToShortDateString());//год
                    #endregion


                    ExcelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("spis_dolg.xls"), Dic);

                    excells1 = ExcelL.ExlWs.get_Range("D3", "L4");
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = SetReportConditional(System.Convert.ToInt32(Nzp_user));
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.Font.Size = 8;
                    excells1.RowHeight = 23;

                    #region Пишем тело

                    int ExcelRow = 9;
                    int i = 0;
                    decimal allCount_ls = 0;
                    decimal allSum_insaldo = 0;
                    decimal allSum_charge = 0;
                    decimal allSum_money = 0;
                    decimal allSum_outsaldo = 0;
                    decimal geuCount_ls = 0;
                    decimal geuSum_insaldo = 0;
                    decimal geuSum_charge = 0;
                    decimal geuSum_money = 0;
                    decimal geuSum_outsaldo = 0;

                    int rowGeu = 8;
                    string oldGeu = "";
                    while (i < DT.Rows.Count)
                    {
                        if (i == 0) oldGeu = DT.Rows[i]["geu"].ToString();


                        if (oldGeu != DT.Rows[i]["geu"].ToString())
                        {
                            ExcelL.ExlWs.Cells[rowGeu, 1] = oldGeu.Trim();
                            ExcelL.ExlWs.Cells[rowGeu, 2] = geuCount_ls;
                            ExcelL.ExlWs.Cells[rowGeu, 7] = geuSum_insaldo;
                            ExcelL.ExlWs.Cells[rowGeu, 8] = geuSum_charge;
                            ExcelL.ExlWs.Cells[rowGeu, 9] = geuSum_money;
                            ExcelL.ExlWs.Cells[rowGeu, 10] = geuSum_outsaldo;

                            excells1 = ExcelL.ExlWs.get_Range("A" + rowGeu.ToString(), "L" + rowGeu.ToString());
                            excells1.Font.Bold = true;

                            geuCount_ls = 0;
                            geuSum_insaldo = 0;
                            geuSum_charge = 0;
                            geuSum_money = 0;
                            geuSum_outsaldo = 0;
                            rowGeu = ExcelRow;
                            oldGeu = DT.Rows[i]["geu"].ToString();
                            ExcelRow++;

                        }


                        geuCount_ls++;
                        ExcelL.ExlWs.Cells[ExcelRow, 2] = geuCount_ls;
                        geuSum_insaldo = geuSum_insaldo + Decimal.Parse(DT.Rows[i]["sum_insaldo"].ToString());
                        geuSum_charge = geuSum_charge + Decimal.Parse(DT.Rows[i]["sum_charge"].ToString());
                        geuSum_money = geuSum_money + Decimal.Parse(DT.Rows[i]["sum_money"].ToString());
                        geuSum_outsaldo = geuSum_outsaldo + Decimal.Parse(DT.Rows[i]["sum_outsaldo"].ToString());


                        allCount_ls++;
                        allSum_insaldo = allSum_insaldo + Decimal.Parse(DT.Rows[i]["sum_insaldo"].ToString());
                        allSum_charge = allSum_charge + Decimal.Parse(DT.Rows[i]["sum_charge"].ToString());
                        allSum_money = allSum_money + Decimal.Parse(DT.Rows[i]["sum_money"].ToString());
                        allSum_outsaldo = allSum_outsaldo + Decimal.Parse(DT.Rows[i]["sum_outsaldo"].ToString());

                        ExcelL.ExlWs.Cells[ExcelRow, 1] = "";

                        ExcelL.ExlWs.Cells[ExcelRow, 3] = DT.Rows[i]["ulica"].ToString().Trim().Replace("/ -", "");
                        ExcelL.ExlWs.Cells[ExcelRow, 4] = DT.Rows[i]["ndom"].ToString().Trim().Replace("-", " ");
                        ExcelL.ExlWs.Cells[ExcelRow, 5] = DT.Rows[i]["nkvar"].ToString().Trim().Replace("-", " ");
                        ExcelL.ExlWs.Cells[ExcelRow, 6] = DT.Rows[i]["fio"].ToString().Trim();
                        ExcelL.ExlWs.Cells[ExcelRow, 7] = Decimal.Parse(DT.Rows[i]["sum_insaldo"].ToString());
                        ExcelL.ExlWs.Cells[ExcelRow, 8] = Decimal.Parse(DT.Rows[i]["sum_charge"].ToString());
                        ExcelL.ExlWs.Cells[ExcelRow, 9] = Decimal.Parse(DT.Rows[i]["sum_money"].ToString());
                        ExcelL.ExlWs.Cells[ExcelRow, 10] = Decimal.Parse(DT.Rows[i]["sum_outsaldo"].ToString());
                        if (System.Convert.ToInt32(DT.Rows[i]["privat"].ToString()) == 1)
                        {
                            ExcelL.ExlWs.Cells[ExcelRow, 11] = "приватизирована";
                        }
                        else
                        {
                            ExcelL.ExlWs.Cells[ExcelRow, 11] = "неприватизирована";
                        }
                        ExcelL.ExlWs.Cells[ExcelRow, 12] = DT.Rows[i]["mes_dolg"].ToString();
                        ExcelRow++;
                        i++;
                    }
                    #endregion

                    ExcelL.ExlWs.Cells[rowGeu, 1] = oldGeu.Trim();
                    ExcelL.ExlWs.Cells[rowGeu, 2] = geuCount_ls;
                    ExcelL.ExlWs.Cells[rowGeu, 7] = geuSum_insaldo;
                    ExcelL.ExlWs.Cells[rowGeu, 8] = geuSum_charge;
                    ExcelL.ExlWs.Cells[rowGeu, 9] = geuSum_money;
                    ExcelL.ExlWs.Cells[rowGeu, 10] = geuSum_outsaldo;
                    excells1 = ExcelL.ExlWs.get_Range("A" + rowGeu.ToString(), "L" + rowGeu.ToString());
                    excells1.Font.Bold = true;


                    ExcelL.ExlWs.Cells[7, 1] = "ВСЕГО";
                    ExcelL.ExlWs.Cells[7, 2] = allCount_ls;
                    ExcelL.ExlWs.Cells[7, 7] = allSum_insaldo;
                    ExcelL.ExlWs.Cells[7, 8] = allSum_charge;
                    ExcelL.ExlWs.Cells[7, 9] = allSum_money;
                    ExcelL.ExlWs.Cells[7, 10] = allSum_outsaldo;
                    excells1 = ExcelL.ExlWs.get_Range("A7", "L7");
                    excells1.Font.Bold = true;


                    #region форматируем
                    if (ExcelRow > 9) ExcelRow--;
                    excells1 = ExcelL.ExlWs.get_Range("G7", "J" + ExcelRow.ToString());
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.EntireColumn.NumberFormat = "# ##0,00";

                    excells1 = ExcelL.ExlWs.get_Range("A7", "L" + ExcelRow.ToString());
                    excells1.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                    excells1.Font.Size = 6;

                    SetReportSing(ExcelL, ExcelRow + 3, false);

                    #endregion

                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "DolgSpis" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString() + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }

                    #endregion

                }

                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetDolgSpis DataTable : ОШИБКА!";

                    fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "DolgSpis" +
                                                                                   prm_.year_.ToString() + "_" +
                                                                                   prm_.month_.ToString()
                                                                                   + "_" + Nzp_user) + ".xls";
                    ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                    if (InputOutput.useFtp)
                    {
                        fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                    }

                    //удаление объекта
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                try
                {
                    if (ExcelL != null)
                    {
                        fileName = GetFileName(STCLINE.KP50.Global.Constants.ExcelDir, "DolgSpis" +
                            prm_.year_.ToString() + "_" +
                            prm_.month_.ToString()
                            + "_" + Nzp_user) + ".xls";
                        ret = ExcelL.SaveFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName, fileName, ref ExcelL.ExlWb, ref ExcelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет GetDolgSpis", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetDolgSpis Excel File : ОШИБКА!";
                }

            }
            finally
            {
                //удаление объекта
                if (ExcelL != null)
                {
                    ExcelL.DeleteObject();
                }
            }

            return ret;

        }


        /// <summary>
        /// Заполнение строки по одному дому
        /// </summary>
        /// <param name="excells1"></param>
        /// <param name="excelRow"></param>
        /// <param name="countDom"></param>
        /// <param name="excelL"></param>
        /// <param name="adres"></param>
        /// <param name="exform"></param>
        /// <param name="nachData"></param>
        /// <param name="gilMas"></param>
        /// <param name="kvMas"></param>
        /// <param name="gkalMas"></param>
        public void FillOneRowSpravSoderg8(Range excells1,
            int excelRow, int countDom, ExcelLoader excelL, List<string> adres,
            ExcelFormater exform, decimal[,] nachData, List<decimal> gilMas,
            List<decimal> kvMas, List<decimal> gkalMas)
        {
            if (excells1 == null) throw new ArgumentNullException("excells1");

            const int rowCount = 3;

            if (adres[0] == "Итого")
            {
                excells1 = excelL.ExlWs.Range["A" + excelRow, "E" + (excelRow + rowCount - 1)];
                excells1.Merge(Type.Missing);
                excells1.Font.Size = 6;
                excells1.FormulaR1C1 = "Итого";
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                excells1.Borders.LineStyle = XlLineStyle.xlContinuous;


            }
            else
            {

                excells1 = excelL.ExlWs.Range["A" + excelRow, "A" + (excelRow + rowCount - 1)];
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = countDom;


                excells1 = excelL.ExlWs.Range["B" + excelRow, "B" + (excelRow + rowCount - 1)];
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = adres[0];


                excells1 = excelL.ExlWs.Range["C" + excelRow, "C" + (excelRow + rowCount - 1)];
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = adres[1];

                excells1 = excelL.ExlWs.Range["D" + excelRow, "D" + (excelRow + rowCount - 1)];
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = adres[2];

                excells1 = excelL.ExlWs.Range["E" + excelRow, "E" + (excelRow + rowCount - 1)];
                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                excells1.Merge(Type.Missing);
                excells1.FormulaR1C1 = adres[3];
            }

            excells1 = excelL.ExlWs.Range["F" + excelRow, "F" + excelRow];
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Численность, чел";

            excells1 = excelL.ExlWs.Range["F" + (excelRow + 1), "F" + (excelRow + 1)];
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Площадь, м2";

            excells1 = excelL.ExlWs.Range["F" + (excelRow + 2), "F" + (excelRow + 2)];
            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Объем, Гкал";

            int maxExcelCol = 7;
            //Присваиваем данные по формулам
            for (int j = 0; j < gilMas.Count; j++)
            {
                if (gilMas[j] > 0m)
                {
                    excelL.ExlWs.Cells[excelRow, 7 + j] = gilMas[j];
                    excells1 = excelL.ExlWs.Range[exform.BukvaList[7 + j - 1]
                          + (excelRow), exform.BukvaList[7 + j - 1] + (excelRow)];
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "0";
                }
                if (kvMas[j] > 0m)
                {
                    excelL.ExlWs.Cells[excelRow + 1, 7 + j] = kvMas[j];
                    excells1 = excelL.ExlWs.Range[exform.BukvaList[7 + j - 1]
                          + (excelRow + 1), exform.BukvaList[7 + j - 1] + (excelRow + 1)];
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00";
                }

                if (gkalMas[j] > 0m)
                {
                    excelL.ExlWs.Cells[excelRow + 2, 7 + j] = gkalMas[j];

                    excells1 = excelL.ExlWs.Range[exform.BukvaList[7 + j - 1]
                          + (excelRow + 2), exform.BukvaList[7 + j - 1] + (excelRow + 2)];
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                    excells1.NumberFormat = "# ##0,00000";
                }

                maxExcelCol++;
            }



            //Присваиваем данные по начислениям
            for (var j = 0; j < nachData.GetLength(1); j++)
            {
                for (var i = 0; i < 3; i++)
                {

                    if (nachData[i, j] != 0m)
                    {
                        excelL.ExlWs.Cells[excelRow + i, maxExcelCol + j] = nachData[i, j];
                    }
                }
                if ((j == 0) || (j == 1) || (j > 5))
                {
                    excells1 = excelL.ExlWs.Range[exform.BukvaList[maxExcelCol + j - 1] +
                    excelRow, exform.BukvaList[maxExcelCol + j - 1] + (excelRow + 2)];
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.Merge(Type.Missing);
                    excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;

                }

            }




        }


        /// <summary>
        /// Справка по услуге ГВС 
        /// </summary>
        /// <param name="prm"></param>
        /// <param name="nzpUser"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Returns GetSpravSoderg8(Prm prm, string nzpUser, ref string fileName)
        {
            Returns ret;
            string nameSupp = "";

            //создание dataTable
            var exR = new ExcelRep();
            System.Data.DataTable dt = exR.GetSpravSoderg(prm, out ret, nzpUser);

            ExcelLoader excelL = null;
            try
            {


                if (dt != null)
                {
                    excelL = new ExcelLoader();



                    #region Создаем шапку

                    var dic = new Dictionary<string, string>
                    {
                        {"ERCName", SetReportHeader()},
                        {"month", Utils.GetMonthName(prm.month_)},
                        {"year", prm.year_.ToString(CultureInfo.InvariantCulture)},
                        {"DATE", DateTime.Now.ToShortDateString()}
                    };
                    if (prm.has_pu == "1")
                    {
                        dic.Add("ODPU", "(все дома)");
                    }
                    else
                        if (prm.has_pu == "2")
                        {
                            dic.Add("ODPU", "(дома с ОДПУ)");
                        }
                        else
                        {
                            dic.Add("ODPU", "(дома без ОДПУ)");
                        }



                    // Массив заголовков
                    var columnHeaderList = new List<string>
                    {
                        "Начислено (тариф*кол-во), руб.",
                        "Постоянное начисление, руб.",
                        "Возврат за услуги, руб.",
                        "Начислено раз. красным, руб.",
                        "Начислено раз черным, руб.",
                        "Корректировка за отопительный перииод, руб.",
                        "Итого к оплате, руб.",
                        "Начислено к оплате, руб."
                    };

                    #endregion


                    #region Определение услуги
                    var dbSprav = new DbSprav();
                    var finder = new Finder { nzp_user = prm.nzp_serv, RolesVal = new List<_RolesVal>() };
                    var p = new _RolesVal
                    {
                        kod = Global.Constants.role_sql_serv,
                        tip = Global.Constants.role_sql,
                        val = prm.nzp_serv.ToString(CultureInfo.InvariantCulture)
                    };
                    finder.RolesVal.Add(p);
                    List<_Service> lserv = dbSprav.ServiceLoad(finder, out ret);
                    dic.Add("SERVICE", lserv.Count > 0 ? lserv[0].service : "Услуга неопределена");
                    dbSprav.Close();
                    #endregion

                    dic.Add("NAMESUPP", prm.nzp_key > -1 ? prm.name_prm : "Все поставщики");
                    dbSprav.Close();


                    excelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("sprav_soderg.xls"), dic);
                    var exform = new ExcelFormater();
                    Range excells1 = excelL.ExlWs.Range["D3", "L4"];
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = SetReportConditional(Convert.ToInt32(nzpUser));
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.Font.Size = 8;
                    excells1.RowHeight = 23;

                    excelL.ExlWs.PageSetup.PrintTitleRows = "$6:$7";

                    #region Пишем тело
                    excells1 = excelL.ExlWs.Range["F6", "F7"];
                    excells1.Merge(Type.Missing);
                    excells1.Font.Size = 6;
                    excells1.FormulaR1C1 = "";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                    excells1.WrapText = true;

                    int excelRow = 8;

                    int excelCol = 7;
                    int countDom = 0;
                    string oldAdres = "";
                    string oldUlica = "";
                    string oldDom = "";
                    string oldLitera = "";
                    string ulica = "";
                    string dom = "";
                    string litera = "";
                    string geu = "";



                    var gilMas = new List<Decimal>();
                    var kvMas = new List<Decimal>();
                    var gkalMas = new List<Decimal>();
                    var adresMas = new List<string>();
                    var gilMasDom = new List<Decimal>();
                    var kvMasDom = new List<Decimal>();
                    var gkalMasDom = new List<Decimal>();

                    var nachData = new decimal[3, 8];
                    var itonachData = new decimal[3, 8];

                    for (int j = 0; j < 3; j++)
                        for (int k = 0; k < 7; k++)
                        {
                            nachData[j, k] = 0;
                            itonachData[j, k] = 0;
                        }

                    int i = 0;

                    while (i < dt.Rows.Count)
                    {

                        string adres = dt.Rows[i]["ulica"].ToString().Trim() + "," +
                                       dt.Rows[i]["ndom"].ToString().Trim() +
                                       dt.Rows[i]["nkor"].ToString().Trim();
                        geu = dt.Rows[i]["geu"].ToString().Trim();
                        ulica = dt.Rows[i]["ulica"].ToString().Trim();
                        dom = dt.Rows[i]["ndom"].ToString().Trim();
                        litera = dt.Rows[i]["nkor"].ToString().Trim();

                        if (i == 0)
                        {
                            oldAdres = adres;
                            oldUlica = dt.Rows[i]["ulica"].ToString().Trim();
                            oldDom = dt.Rows[i]["ndom"].ToString().Trim();
                            oldLitera = dt.Rows[i]["nkor"].ToString().Trim();
                        }

                        #region Записываем Итого по дому
                        if (oldAdres != adres)
                        {
                            countDom++;
                            adresMas.Clear();
                            adresMas.Add(geu);
                            adresMas.Add(oldUlica);
                            adresMas.Add(oldDom);
                            adresMas.Add(oldLitera);


                            FillOneRowSpravSoderg8(excells1, excelRow, countDom, excelL, adresMas,
                            exform, nachData, gilMasDom, kvMasDom, gkalMasDom);


                            for (int j = 0; j < gilMasDom.Count; j++)
                            {
                                gilMasDom[j] = 0;
                                kvMasDom[j] = 0;
                                gkalMasDom[j] = 0;
                            }


                            for (int j = 0; j < 3; j++)
                                for (int k = 0; k < 7; k++)
                                {
                                    nachData[j, k] = 0;
                                }
                            excelCol = 7;
                            oldAdres = adres;
                            oldUlica = ulica;
                            oldDom = dom;
                            oldLitera = litera;
                            excelRow += 3;


                        }
                        #endregion


                        #region Пишем заголовок формул
                        if (excelRow == 8)
                        {
                            excelL.ExlWs.Cells[7, excelCol] = dt.Rows[i]["name_frm"].ToString();
                            excells1 = excelL.ExlWs.Range[exform.BukvaList[excelCol - 1] + "7", exform.BukvaList[excelCol - 1] + "7"];
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                            excells1.WrapText = true;
                            excells1.ColumnWidth = 10;
                            excells1.Font.Size = 6;
                        }
                        #endregion

                        if (excelCol - 7 >= gilMasDom.Count)
                        {
                            gilMasDom.Add(0);
                            kvMasDom.Add(0);
                            gkalMasDom.Add(0);
                            gilMas.Add(0);
                            kvMas.Add(0);
                            gkalMas.Add(0);
                        }

                        gilMasDom[excelCol - 7] = gilMasDom[excelCol - 7] + Decimal.Parse(dt.Rows[i]["count_gil"].ToString());
                        kvMasDom[excelCol - 7] = kvMasDom[excelCol - 7] + Decimal.Parse(dt.Rows[i]["c_calc_kv"].ToString());
                        gkalMasDom[excelCol - 7] = gkalMasDom[excelCol - 7] + Decimal.Parse(dt.Rows[i]["c_calc_gkal"].ToString());

                        gilMas[excelCol - 7] = gilMas[excelCol - 7] + Decimal.Parse(dt.Rows[i]["count_gil"].ToString());
                        kvMas[excelCol - 7] = kvMas[excelCol - 7] + Decimal.Parse(dt.Rows[i]["c_calc_kv"].ToString());
                        gkalMas[excelCol - 7] = gkalMas[excelCol - 7] + Decimal.Parse(dt.Rows[i]["c_calc_gkal"].ToString());

                        nachData[0, 0] = nachData[0, 0] + Decimal.Parse(dt.Rows[i]["rsum_tarif"].ToString());
                        nachData[1, 0] = 0;
                        nachData[2, 0] = 0;

                        nachData[0, 1] = nachData[0, 1] + Decimal.Parse(dt.Rows[i]["rsum_tarif"].ToString());
                        nachData[1, 1] = 0;
                        nachData[2, 1] = 0;

                        nachData[0, 2] = nachData[0, 2] + Decimal.Parse(dt.Rows[i]["sum_nedop"].ToString());
                        nachData[1, 2] = nachData[1, 2] + Decimal.Parse(dt.Rows[i]["c_nedop_kv"].ToString());
                        nachData[2, 2] = nachData[2, 2] + Decimal.Parse(dt.Rows[i]["c_nedop_gkal"].ToString());

                        nachData[0, 3] = nachData[0, 3] + Decimal.Parse(dt.Rows[i]["reval_k"].ToString());
                        nachData[1, 3] = nachData[1, 3] + Decimal.Parse(dt.Rows[i]["c_reval_k_kv"].ToString());
                        nachData[2, 3] = nachData[2, 3] + Decimal.Parse(dt.Rows[i]["c_reval_k_gkal"].ToString());

                        nachData[0, 4] = nachData[0, 4] + Decimal.Parse(dt.Rows[i]["reval_d"].ToString());
                        nachData[1, 4] = nachData[1, 4] + Decimal.Parse(dt.Rows[i]["c_reval_d_kv"].ToString());
                        nachData[2, 4] = nachData[2, 4] + Decimal.Parse(dt.Rows[i]["c_reval_d_gkal"].ToString());


                        nachData[0, 5] = nachData[0, 5] + Decimal.Parse(dt.Rows[i]["sum_otopl"].ToString());
                        nachData[1, 5] = 0;
                        nachData[2, 5] = nachData[2, 5] + Decimal.Parse(dt.Rows[i]["c_otopl"].ToString());

                        nachData[0, 6] = nachData[0, 6] + Decimal.Parse(dt.Rows[i]["sum_charge"].ToString());
                        nachData[1, 6] = 0;
                        nachData[2, 6] = 0;

                        nachData[0, 7] = nachData[0, 7] + Decimal.Parse(dt.Rows[i]["sum_charge"].ToString());
                        nachData[1, 7] = 0;
                        nachData[2, 7] = 0;


                        itonachData[0, 0] = itonachData[0, 0] + Decimal.Parse(dt.Rows[i]["rsum_tarif"].ToString());
                        itonachData[1, 0] = 0;
                        itonachData[2, 0] = 0;

                        itonachData[0, 1] = itonachData[0, 1] + Decimal.Parse(dt.Rows[i]["rsum_tarif"].ToString());
                        itonachData[1, 1] = 0;
                        itonachData[2, 1] = 0;

                        itonachData[0, 2] = itonachData[0, 2] + Decimal.Parse(dt.Rows[i]["sum_nedop"].ToString());
                        itonachData[1, 2] = itonachData[1, 2] + Decimal.Parse(dt.Rows[i]["c_nedop_kv"].ToString());
                        itonachData[2, 2] = itonachData[2, 2] + Decimal.Parse(dt.Rows[i]["c_nedop_gkal"].ToString());

                        itonachData[0, 3] = itonachData[0, 3] + Decimal.Parse(dt.Rows[i]["reval_k"].ToString());
                        itonachData[1, 3] = itonachData[1, 3] + Decimal.Parse(dt.Rows[i]["c_reval_k_kv"].ToString());
                        itonachData[2, 3] = itonachData[2, 3] + Decimal.Parse(dt.Rows[i]["c_reval_k_gkal"].ToString());

                        itonachData[0, 4] = itonachData[0, 4] + Decimal.Parse(dt.Rows[i]["reval_d"].ToString());
                        itonachData[1, 4] = itonachData[1, 4] + Decimal.Parse(dt.Rows[i]["c_reval_d_kv"].ToString());
                        itonachData[2, 4] = itonachData[2, 4] + Decimal.Parse(dt.Rows[i]["c_reval_d_gkal"].ToString());

                        itonachData[0, 5] = itonachData[0, 5] + Decimal.Parse(dt.Rows[i]["sum_otopl"].ToString());
                        itonachData[1, 5] = 0;
                        itonachData[2, 5] = itonachData[2, 5] + Decimal.Parse(dt.Rows[i]["c_otopl"].ToString());

                        itonachData[0, 6] = itonachData[0, 6] + Decimal.Parse(dt.Rows[i]["sum_charge"].ToString());
                        itonachData[1, 6] = 0;
                        itonachData[2, 6] = 0;

                        itonachData[0, 7] = itonachData[0, 7] + Decimal.Parse(dt.Rows[i]["sum_charge"].ToString());
                        itonachData[1, 7] = 0;
                        itonachData[2, 7] = 0;


                        excelCol++;
                        i++;
                    }

                    countDom++;
                    if (dt.Rows.Count > 0)
                    {
                        adresMas.Clear();
                        adresMas.Add(geu);
                        adresMas.Add(ulica);
                        adresMas.Add(dom);
                        adresMas.Add(litera);


                        FillOneRowSpravSoderg8(excells1, excelRow, countDom, excelL, adresMas,
                        exform, nachData, gilMasDom, kvMasDom, gkalMasDom);





                        #region Пишем заголовки колонок
                        for (int k = 0; k < columnHeaderList.Count; k++)
                        {
                            excells1 = excelL.ExlWs.Range[exform.BukvaList[excelCol - 1 + k] + "6",
                                exform.BukvaList[excelCol - 1 + k] + "7"];
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = columnHeaderList[k];
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                            excells1.WrapText = true;
                            excells1.ColumnWidth = 10;
                        }
                        #endregion

                        excells1 = excelL.ExlWs.Range["G6", exform.BukvaList[excelCol - 2] + "6"];
                        excells1.Merge(Type.Missing);
                        excells1.Font.Size = 6;
                        excells1.FormulaR1C1 = "Количество жильцов/нат.показатель";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                        excells1.WrapText = true;


                        //    ExcelCol = 7;




                    }
                    #endregion

                    if (dt.Rows.Count > 0)
                    {
                        #region Пишем итого
                        excelRow += 3;
                        adresMas.Clear();
                        adresMas.Add("Итого");
                        adresMas.Add("");
                        adresMas.Add("");
                        adresMas.Add("");

                        FillOneRowSpravSoderg8(excells1, excelRow, countDom, excelL, adresMas,
                        exform, itonachData, gilMas, kvMas, gkalMas);


                        #endregion

                        excelRow += 2;
                        #region форматируем
                        excells1 = excelL.ExlWs.Range["A6", exform.BukvaList[excelCol + columnHeaderList.Count - 2] + excelRow];
                        excells1.Font.Size = 6;
                        excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                        excells1 = excelL.ExlWs.Range[exform.BukvaList[excelCol - 1] + "8",
                            exform.BukvaList[excelCol + columnHeaderList.Count - 2] + excelRow];
                        excells1.EntireColumn.NumberFormat = "# ##0,00###";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        // Вместить поля по ширине листа
                        excelL.ExlWs.PageSetup.Zoom = false;
                        excelL.ExlWs.PageSetup.FitToPagesWide = 1;
                        excelL.ExlWs.PageSetup.FitToPagesTall = false;
                        #endregion
                    }

                    SetReportSing(excelL, excelRow + 3, true);


                    if (prm.nzp_key != -1)
                    {
                        nameSupp = nameSupp + prm.name_prm;
                    }
                    nameSupp = nameSupp.Replace(">", " ").Replace("<", " ").Replace("?", " ").Replace("[", " ");
                    nameSupp = nameSupp.Replace("]", " ").Replace(":", " ").Replace("|", " ").Replace("*", " ").Replace(@"\", " ");

                    nameSupp = nameSupp.Replace(@"/", " ").Replace(" ", "_");

                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(Global.Constants.ExcelDir,
                            prm.month_ + "_Дислокация_Отопление_" + nameSupp +
                             "_" + nzpUser) + ".xls";
                        ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName, ref excelL.ExlWb, ref excelL.ExlApp);
                        excelL.ExlWb.Close(false, Type.Missing, Type.Missing);
                        excelL.ExlWb = null;
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }

                    #endregion

                }

                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSoderg DataTable : ОШИБКА!";
                    //удаление объекта
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                try
                {
                    if (excelL != null)
                    {
                        fileName = GetFileName(Global.Constants.ExcelDir,
                            prm.month_ + "_Дислокация_Отопление_" + nameSupp +
                             "_" + nzpUser) + ".xls";
                        ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName, ref excelL.ExlWb, ref excelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }
                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет GetSpravSoderg", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSoderg Excel File : ОШИБКА!";
                }

            }
            finally
            {
                //удаление объекта
                if (excelL != null)
                {
                    excelL.DeleteObject();
                }
            }

            return ret;

        }




        public Returns GetSpravSoderg(Prm prm, string nzpUser, ref string fileName)
        {
            Returns ret;
            string nameSupp = "";
            string serviceName = "";
            if (prm.nzp_serv == 9)
            {
                ret = GetSpravSoderg9(prm, nzpUser, ref fileName);
                return ret;
            }
            if (prm.nzp_serv == 7)
            {
                ret = GetSpravSoderg7(prm, nzpUser, ref fileName);
                return ret;
            }
            if (prm.nzp_serv == 8)
            {
                ret = GetSpravSoderg8(prm, nzpUser, ref fileName);
                return ret;
            }

            //создание dataTable
            var exR = new ExcelRep();
            var dt = exR.GetSpravSoderg(prm, out ret, nzpUser);

            ExcelLoader excelL = null;
            try
            {


                if (dt != null)
                {
                    excelL = new ExcelLoader();



                    #region Создаем шапку

                    var dic = new Dictionary<string, string>
                    {
                        {"ERCName", SetReportHeader()},
                        {"month", Utils.GetMonthName(prm.month_)},
                        {"year", prm.year_.ToString(CultureInfo.InvariantCulture)},
                        {"DATE", DateTime.Now.ToShortDateString()}
                    };
                    if (prm.has_pu == "1")
                    {
                        dic.Add("ODPU", "(все дома)");
                    }
                    else
                        if (prm.has_pu == "2")
                        {
                            dic.Add("ODPU", "(дома с ОДПУ)");
                        }
                        else
                        {
                            dic.Add("ODPU", "(дома без ОДПУ)");
                        }

                    bool hasOdn = ((prm.nzp_serv == 6) || (prm.nzp_serv == 25) || (prm.nzp_serv == 14) || (prm.nzp_serv == 10));

                    // Массив заголовков
                    var columnHeaderList = new List<string>();
                    columnHeaderList.Add("Начислено (тариф*кол-во), руб.");
                    columnHeaderList.Add("Постоянное начисление, руб.");
                    if (hasOdn)
                    {
                        columnHeaderList.Add("Начислено ОДН, руб.");
                    }
                    columnHeaderList.Add("Возврат за услуги, руб.");
                    columnHeaderList.Add("Начислено раз. красным, руб.");
                    columnHeaderList.Add("Начислено раз черным, руб.");
                    columnHeaderList.Add("Итого к оплате, руб.");
                    columnHeaderList.Add("Тариф поставщика, руб.");

                    var rowHeaderList = new List<string> { "жильцов, чел." };

                    if (hasOdn)
                    {
                        if (prm.nzp_serv == 25)
                        {
                            rowHeaderList.Add("Объем л/счетов, квт*ч");
                            rowHeaderList.Add("Объем ОДН, квт*ч");
                            rowHeaderList.Add("площадь, м2");
                        }
                        else
                        {
                            rowHeaderList.Add("Объем л/счетов, м3");
                            rowHeaderList.Add("Объем ОДН, м3");
                            rowHeaderList.Add("площадь, м2");
                        }
                    }
                    else
                    {
                        rowHeaderList.Add(prm.nzp_serv == 24 ? "Объем л/счетов, чел." : "Объем л/счетов, м2");
                    }


                    var fieldList = new List<string> { "rsum_tarif", "rsum_tarif_wodn" };

                    //fieldList.Add("c_reval");
                    if (hasOdn)
                    {
                        fieldList.Add("sum_odn");
                    }
                    fieldList.Add("sum_nedop");
                    fieldList.Add("reval_k");
                    fieldList.Add("reval_d");
                    fieldList.Add("sum_charge");
                    fieldList.Add("tarif");

                    fieldList.Add("c_nedop");
                    fieldList.Add("c_reval_k");
                    fieldList.Add("c_reval_d");



                    var domMas = new decimal[fieldList.Count];
                    for (int j = 0; j < fieldList.Count; j++) domMas[j] = 0;

                    var itoMas = new decimal[fieldList.Count];
                    for (int j = 0; j < fieldList.Count; j++) itoMas[j] = 0;


                    #endregion


                    #region Определение услуги
                    var dbSprav = new DbSprav();
                    var finder = new Finder { nzp_user = prm.nzp_serv, RolesVal = new List<_RolesVal>() };
                    var p = new _RolesVal
                    {
                        kod = Global.Constants.role_sql_serv,
                        tip = Global.Constants.role_sql,
                        val = prm.nzp_serv.ToString(CultureInfo.InvariantCulture)
                    };
                    finder.RolesVal.Add(p);
                    List<_Service> lserv = dbSprav.ServiceLoad(finder, out ret);
                    if (lserv.Count > 0)
                    {
                        dic.Add("SERVICE", lserv[0].service);
                        serviceName = lserv[0].service;
                    }
                    else
                        dic.Add("SERVICE", "Услуга неопределена");
                    dbSprav.Close();
                    #endregion

                    dic.Add("NAMESUPP", prm.nzp_key > -1 ? prm.name_prm : "Все поставщики");
                    dbSprav.Close();


                    excelL.LoadTemlateNewFile(PathHelper.GetReportTemplatePath("sprav_soderg.xls"), dic);
                    var exform = new ExcelFormater();
                    Range excells1 = excelL.ExlWs.Range["D3", "L4"];
                    excells1.Merge(Type.Missing);
                    excells1.FormulaR1C1 = SetReportConditional(Convert.ToInt32(nzpUser));
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                    excells1.Font.Size = 8;
                    excells1.RowHeight = 23;

                    #region Пишем тело
                    excells1 = excelL.ExlWs.Range["F6", "F7"];
                    excells1.Merge(Type.Missing);
                    excells1.Font.Size = 6;
                    excells1.FormulaR1C1 = "";
                    excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                    excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                    excells1.WrapText = true;

                    int excelRow = 8;
                    int i = 0;
                    int excelCol = 7;
                    int countDom = 0;
                    int maxExcelCol = 7;
                    string oldAdres = "";
                    string oldUlica = "";
                    string oldDom = "";
                    string ulica = "";
                    string dom = "";
                    string litera = "";
                    string geu = "";
                    decimal revalOdndom = 0;
                    decimal revalOdnall = 0;


                    var calcMas = new List<Decimal>();
                    var gilMas = new List<Decimal>();
                    var plMas = new List<Decimal>();
                    var odnMas = new List<Decimal>();





                    while (i < dt.Rows.Count)
                    {

                        string adres = dt.Rows[i]["ulica"].ToString().Trim() + "," +
                                       dt.Rows[i]["ndom"].ToString().Trim() +
                                       dt.Rows[i]["nkor"].ToString().Trim();
                        geu = dt.Rows[i]["geu"].ToString().Trim();
                        ulica = dt.Rows[i]["ulica"].ToString().Trim();
                        dom = dt.Rows[i]["ndom"].ToString().Trim();
                        litera = dt.Rows[i]["nkor"].ToString().Trim();

                        if (i == 0)
                        {
                            oldAdres = adres;
                            oldUlica = dt.Rows[i]["ulica"].ToString().Trim();
                            oldDom = dt.Rows[i]["ndom"].ToString().Trim();
                        }

                        #region Записываем Итого по дому
                        if (oldAdres != adres)
                        {
                            countDom++;

                            excells1 = excelL.ExlWs.Range["A" + excelRow, "A" + (excelRow + rowHeaderList.Count - 1)];
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = countDom;


                            excells1 = excelL.ExlWs.Range["B" + excelRow, "B" + (excelRow + rowHeaderList.Count - 1)];
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = dt.Rows[i]["geu"].ToString().Trim();


                            excells1 = excelL.ExlWs.Range["C" + excelRow, "C" + (excelRow + rowHeaderList.Count - 1)];
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = oldUlica;

                            excells1 = excelL.ExlWs.Range["D" + excelRow, "D" + (excelRow + rowHeaderList.Count - 1)];
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = oldDom;

                            excells1 = excelL.ExlWs.Range["E" + excelRow, "E" + (excelRow + rowHeaderList.Count - 1)];
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = litera;

                            for (int k = 0; k < rowHeaderList.Count; k++)
                            {
                                excells1 = excelL.ExlWs.Range["F" + (excelRow + k), "F" + (excelRow + k)];
                                excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                                excells1.Merge(Type.Missing);
                                if (!hasOdn)
                                {
                                    if (prm.nzp_serv == 18)
                                    {
                                        if (Int32.Parse(dt.Rows[i]["nzp_measure"].ToString()) == 2)
                                        {
                                            rowHeaderList[1] = "Объем л/счетов, с чел.в мес.";
                                        }
                                        else
                                        {
                                            rowHeaderList[1] = "Объем л/счетов, м2";
                                        }
                                    }
                                }
                                excells1.FormulaR1C1 = rowHeaderList[k];
                            }

                            for (int j = 0; j < fieldList.Count - 3; j++) //////!!!!!!!!!!!!
                            {
                                if (Math.Abs(domMas[j]) > 0.001m)
                                    excelL.ExlWs.Cells[excelRow, excelCol + j] = domMas[j];
                            }

                            //Объем по недопоставке
                            int exCol = excelCol + fieldList.Count - 8;
                            if (Math.Abs(domMas[fieldList.Count - 3]) > 0.001m)
                                excelL.ExlWs.Cells[excelRow + 1, exCol] = domMas[fieldList.Count - 3];


                            //Объем по перерасчету -
                            exCol = excelCol + fieldList.Count - 7;
                            if (Math.Abs(domMas[fieldList.Count - 2]) > 0.001m)
                                excelL.ExlWs.Cells[excelRow + 1, exCol] = domMas[fieldList.Count - 2];


                            //Объем по перерасчету +
                            exCol = excelCol + fieldList.Count - 6;
                            if (Math.Abs(domMas[fieldList.Count - 1]) > 0.001m)
                                excelL.ExlWs.Cells[excelRow + 1, exCol] = domMas[fieldList.Count - 1];

                            for (int j = 0; j < fieldList.Count - 3; j++)
                            {
                                exCol = excelCol + j - 1;
                                int exRow;
                                if ((fieldList[j] == "sum_nedop") || (fieldList[j] == "reval_k") ||
                                    (fieldList[j] == "reval_d"))
                                    exRow = excelRow + 1;
                                else exRow = excelRow;

                                excells1 = excelL.ExlWs.Range[exform.BukvaList[exCol] + exRow,
                                                           exform.BukvaList[exCol] +
                                                           (excelRow + rowHeaderList.Count - 1)];
                                excells1.Merge(Type.Missing);
                                excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                            }





                            for (int j = 0; j < fieldList.Count; j++) domMas[j] = 0;
                            revalOdndom = 0;

                            excelCol = 7;
                            oldAdres = adres;
                            oldUlica = ulica;
                            oldDom = dom;
                            excelRow += rowHeaderList.Count;


                        }
                        #endregion

                        #region Проживающие
                        if (Int32.Parse(dt.Rows[i]["count_gil"].ToString()) > 0)
                            excelL.ExlWs.Cells[excelRow, excelCol] = dt.Rows[i]["count_gil"].ToString();
                        if (gilMas.Count < excelCol + 1)
                        {
                            for (int k = gilMas.Count; k < excelCol + 1; k++) gilMas.Add(0);
                        }

                        gilMas[excelCol] = gilMas[excelCol] + Decimal.Parse(dt.Rows[i]["count_gil"].ToString());
                        #endregion


                        #region Объем
                        if (calcMas.Count < excelCol + 1)
                        {
                            for (int k = calcMas.Count; k < excelCol + 1; k++) calcMas.Add(0);
                        }
                        if (dt.Rows[i]["c_calc"].ToString().Trim() != "")
                            if (prm.nzp_serv == 6 || prm.nzp_serv == 7 || prm.nzp_serv == 9 || prm.nzp_serv == 14 || prm.nzp_serv == 10)
                            {
                                if (Math.Abs(Decimal.Parse(dt.Rows[i]["volume"].ToString())) > 0.001m)
                                {
                                    excelL.ExlWs.Cells[excelRow + 1, excelCol] = Decimal.Parse(dt.Rows[i]["volume"].ToString());
                                    calcMas[excelCol] = calcMas[excelCol] + Decimal.Parse(dt.Rows[i]["volume"].ToString());
                                }
                            }
                            else
                            {
                                if (Math.Abs(Decimal.Parse(dt.Rows[i]["c_calc"].ToString())) > 0.001m)
                                {

                                    excelL.ExlWs.Cells[excelRow + 1, excelCol] = Decimal.Parse(dt.Rows[i]["c_calc"].ToString());
                                    calcMas[excelCol] = calcMas[excelCol] + Decimal.Parse(dt.Rows[i]["c_calc"].ToString());
                                }
                            }

                        #endregion

                        if (hasOdn)
                        {
                            #region ОДН
                            if (Math.Abs(Decimal.Parse(dt.Rows[i]["c_calc_odn"].ToString())) > 0)
                                excelL.ExlWs.Cells[excelRow + 2, excelCol] = Decimal.Parse(dt.Rows[i]["c_calc_odn"].ToString());
                            if (odnMas.Count < excelCol + 1)
                            {
                                for (int k = odnMas.Count; k < excelCol + 2; k++) odnMas.Add(0);
                            }
                            odnMas[excelCol] = odnMas[excelCol] + Decimal.Parse(dt.Rows[i]["c_calc_odn"].ToString());
                            #endregion

                            #region площадь
                            if (Math.Abs(Decimal.Parse(dt.Rows[i]["pl_kvar"].ToString())) > 0)
                                excelL.ExlWs.Cells[excelRow + 3, excelCol] = Decimal.Parse(dt.Rows[i]["pl_kvar"].ToString());
                            if (plMas.Count < excelCol + 1)
                            {
                                for (int k = plMas.Count; k < excelCol + 2; k++) plMas.Add(0);
                            }
                            plMas[excelCol] = plMas[excelCol] + Decimal.Parse(dt.Rows[i]["pl_kvar"].ToString());
                            #endregion
                        }

                        #region Пишем заголовок формул
                        if (excelRow == 8)
                        {
                            excelL.ExlWs.Cells[7, excelCol] = dt.Rows[i]["name_frm"].ToString();
                            excells1 = excelL.ExlWs.Range[exform.BukvaList[excelCol - 1] + "7", exform.BukvaList[excelCol - 1] + "7"];
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                            excells1.WrapText = true;
                            excells1.ColumnWidth = 10;
                            excells1.Font.Size = 6;
                        }
                        #endregion


                        for (int k = 0; k < fieldList.Count; k++)
                        {
                            if ((k != 7 && hasOdn) || (k != 6 && !hasOdn))
                            {
                                domMas[k] = domMas[k] + Decimal.Parse(dt.Rows[i][fieldList[k]].ToString());
                            }
                            else
                            {
                                if (Decimal.Parse(dt.Rows[i][fieldList[k]].ToString()) > 0)
                                {
                                    domMas[k] = Decimal.Parse(dt.Rows[i][fieldList[k]].ToString());
                                }
                            }
                        }

                        for (int k = 0; k < fieldList.Count; k++)
                        {

                            if ((k != 7 && hasOdn) || (k != 6 && !hasOdn))
                            {
                                itoMas[k] = itoMas[k] + Decimal.Parse(dt.Rows[i][fieldList[k]].ToString());
                            }
                            else
                            {
                                if (Decimal.Parse(dt.Rows[i][fieldList[k]].ToString()) > 0)
                                {
                                    itoMas[k] = Decimal.Parse(dt.Rows[i][fieldList[k]].ToString());
                                }
                            }


                        }

                        revalOdnall = revalOdnall + Decimal.Parse(dt.Rows[i]["c_reval_odn"].ToString());
                        revalOdndom = revalOdndom + Decimal.Parse(dt.Rows[i]["c_reval_odn"].ToString());

                        excelCol++;
                        i++;
                    }

                    countDom++;
                    if (dt.Rows.Count > 0)
                    {
                        excells1 = excelL.ExlWs.Range["A" + excelRow, "A" + (excelRow + rowHeaderList.Count - 1)];
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = countDom;

                        excells1 = excelL.ExlWs.Range["B" + excelRow, "B" + (excelRow + rowHeaderList.Count - 1)];
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = geu;


                        excells1 = excelL.ExlWs.Range["C" + excelRow, "C" + (excelRow + rowHeaderList.Count - 1)];
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = ulica;

                        excells1 = excelL.ExlWs.Range["D" + excelRow, "D" + (excelRow + rowHeaderList.Count - 1)];
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = dom;

                        excells1 = excelL.ExlWs.Range["E" + excelRow, "E" + (excelRow + rowHeaderList.Count - 1)];
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                        excells1.Merge(Type.Missing);
                        excells1.FormulaR1C1 = litera;

                        for (int k = 0; k < rowHeaderList.Count; k++)
                        {
                            excells1 = excelL.ExlWs.Range["F" + (excelRow + k), "F" + (excelRow + k)];
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = rowHeaderList[k];
                        }



                        for (int j = 0; j < fieldList.Count - 3; j++)
                        {
                            if (Math.Abs(domMas[j]) > 0.001m)
                                excelL.ExlWs.Cells[excelRow, excelCol + j] = domMas[j];

                        }


                        //Объем по недопоставке
                        int exCol = excelCol + fieldList.Count - 8;
                        if (Math.Abs(domMas[fieldList.Count - 3]) > 0.001m)
                            excelL.ExlWs.Cells[excelRow + 1, exCol] = domMas[fieldList.Count - 3];


                        //Объем по перерасчету -
                        exCol = excelCol + fieldList.Count - 7;
                        if (Math.Abs(domMas[fieldList.Count - 2]) > 0.001m)
                            excelL.ExlWs.Cells[excelRow + 1, exCol] = domMas[fieldList.Count - 2];


                        //Объем по перерасчету +
                        exCol = excelCol + fieldList.Count - 6;
                        if (Math.Abs(domMas[fieldList.Count - 1]) > 0.001m)
                            excelL.ExlWs.Cells[excelRow + 1, exCol] = domMas[fieldList.Count - 1];

                        for (int j = 0; j < fieldList.Count - 3; j++)
                        {
                            exCol = excelCol + j - 1;
                            int exRow;
                            if ((fieldList[j] == "sum_nedop") || (fieldList[j] == "reval_k") ||
                                (fieldList[j] == "reval_d"))
                                exRow = excelRow + 1;
                            else exRow = excelRow;

                            excells1 = excelL.ExlWs.Range[exform.BukvaList[exCol] +
                                                      (exRow),
                                                       exform.BukvaList[exCol] +
                                                       (excelRow + rowHeaderList.Count - 1)];
                            excells1.Merge(Type.Missing);
                            excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                        }


                        maxExcelCol = excelCol;

                        #region Пишем заголовки колонок
                        for (int k = 0; k < columnHeaderList.Count; k++)
                        {
                            excells1 = excelL.ExlWs.Range[exform.BukvaList[excelCol - 1 + k] + "6",
                                exform.BukvaList[excelCol - 1 + k] + "7"];
                            excells1.Merge(Type.Missing);
                            excells1.FormulaR1C1 = columnHeaderList[k];
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                            excells1.WrapText = true;
                            excells1.ColumnWidth = 10;
                        }
                        #endregion




                        excells1 = excelL.ExlWs.Range["G6", exform.BukvaList[excelCol - 2] + "6"];
                        excells1.Merge(Type.Missing);
                        excells1.Font.Size = 6;
                        excells1.FormulaR1C1 = "Количество жильцов/нат.показатель";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;
                        excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                        excells1.WrapText = true;




                    }
                    #endregion

                    if (dt.Rows.Count > 0)
                    {
                        #region Пишем итого
                        excelRow += rowHeaderList.Count;


                        for (int j = 4; j < maxExcelCol; j++)
                            excelL.ExlWs.Cells[excelRow, j] = gilMas[j];

                        for (int j = 4; j < maxExcelCol; j++)
                            excelL.ExlWs.Cells[excelRow + 1, j] = calcMas[j];

                        if (hasOdn)
                        {
                            for (int j = 4; j < maxExcelCol; j++)
                                excelL.ExlWs.Cells[excelRow + 2, j] = odnMas[j];

                            for (int j = 4; j < maxExcelCol; j++)
                                excelL.ExlWs.Cells[excelRow + 3, j] = plMas[j];
                        }

                        for (int j = 0; j < fieldList.Count - 4; j++)
                        {
                            excelL.ExlWs.Cells[excelRow, maxExcelCol + j] = itoMas[j];

                        }

                        //Объем по недопоставке
                        int exCol = excelCol + fieldList.Count - 8;
                        excelL.ExlWs.Cells[excelRow + 1, exCol] = itoMas[fieldList.Count - 3];


                        //Объем по перерасчету -
                        exCol = excelCol + fieldList.Count - 7;
                        excelL.ExlWs.Cells[excelRow + 1, exCol] = itoMas[fieldList.Count - 2];


                        //Объем по перерасчету +
                        exCol = excelCol + fieldList.Count - 6;
                        excelL.ExlWs.Cells[excelRow + 1, exCol] = itoMas[fieldList.Count - 1];

                        for (int j = 0; j < fieldList.Count - 3; j++)
                        {
                            exCol = excelCol + j - 1;
                            int exRow;
                            if ((fieldList[j] == "sum_nedop") || (fieldList[j] == "reval_k") ||
                                (fieldList[j] == "reval_d"))
                                exRow = excelRow + 1;
                            else exRow = excelRow;

                            excells1 = excelL.ExlWs.Range[exform.BukvaList[exCol] +
                                                      (exRow),
                                                       exform.BukvaList[exCol] +
                                                       (excelRow + rowHeaderList.Count - 1)];
                            excells1.Merge(Type.Missing);
                            excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                        }



                        excells1 = excelL.ExlWs.Range["A" + excelRow,
                            "E" + (excelRow + rowHeaderList.Count - 1)];
                        excells1.Merge(Type.Missing);
                        excells1.Font.Size = 6;
                        excells1.FormulaR1C1 = "Итого";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        excells1.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlTop;
                        excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                        for (int k = 0; k < rowHeaderList.Count; k++)
                        {
                            excells1 = excelL.ExlWs.Range["F" + (excelRow + k), "F" + (excelRow + k)];
                            excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlLeft;
                            excells1.FormulaR1C1 = rowHeaderList[k];
                            excells1.Font.Size = 6;
                            excells1.Borders.LineStyle = XlLineStyle.xlContinuous;
                        }



                        #endregion

                        excelRow += rowHeaderList.Count - 1;
                        #region форматируем
                        excells1 = excelL.ExlWs.Range["A6", exform.BukvaList[excelCol + columnHeaderList.Count - 2] + excelRow];
                        excells1.Font.Size = 6;
                        excells1.Borders.LineStyle = XlLineStyle.xlContinuous;

                        excells1 = excelL.ExlWs.Range["G8", exform.BukvaList[excelCol + columnHeaderList.Count - 2] + excelRow];
                        excells1.EntireColumn.NumberFormat = "# ##0,00";
                        excells1.HorizontalAlignment = Microsoft.Office.Interop.Excel.Constants.xlRight;
                        #endregion
                    }

                    SetReportSing(excelL, excelRow + 3, true);


                    nameSupp = serviceName + "_" + nameSupp;
                    if (prm.nzp_key != -1)
                    {
                        nameSupp = nameSupp + prm.name_prm;
                    }
                    nameSupp = nameSupp.Replace(">", " ").Replace("<", " ").Replace("?", " ").Replace("[", " ");
                    nameSupp = nameSupp.Replace("]", " ").Replace(":", " ").Replace("|", " ").Replace("*", " ").Replace(@"\", " ");


                    nameSupp = nameSupp.Replace(@"/", " ").Replace(" ", "_");


                    #region Сохраняем файл
                    try
                    {

                        fileName = GetFileName(Global.Constants.ExcelDir, prm.month_ + "_Дислокация_" + nameSupp +
                             "_" + nzpUser) + ".xls";
                        ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName, ref excelL.ExlWb, ref excelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    }

                    #endregion

                }

                else
                {
                    MonitorLog.WriteLog("DT = NULL!!!!", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSoderg DataTable : ОШИБКА!";

                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                try
                {
                    if (excelL != null)
                    {
                        fileName = GetFileName(Global.Constants.ExcelDir,
                            prm.month_ + " Дислокация " + nameSupp +
                             "_" + nzpUser) + ".xls";
                        ret = excelL.SaveFile(Global.Constants.ExcelDir + fileName, fileName, ref excelL.ExlWb, ref excelL.ExlApp);
                        if (InputOutput.useFtp)
                        {
                            fileName = InputOutput.SaveOutputFile(Global.Constants.ExcelDir + fileName);
                        }

                    }
                }
                catch
                {
                    MonitorLog.WriteLog("Ошибка сохранения Excel файла отчет GetSpravSoderg", MonitorLog.typelog.Warn, true);
                    ret.result = false;
                    ret.text = "Создание GetSpravSoderg Excel File : ОШИБКА!";
                }

            }
            finally
            {
                //удаление объекта
                if (excelL != null)
                {
                    excelL.DeleteObject();
                }
            }

            return ret;

        }


    }
}
