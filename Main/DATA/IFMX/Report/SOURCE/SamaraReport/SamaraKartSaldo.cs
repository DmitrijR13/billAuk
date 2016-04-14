using System;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using Excel = Microsoft.Office.Interop.Excel;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Runtime.InteropServices; 

namespace STCLINE.KP50.IFMX.Report.SOURCE.SamaraReport
{

    public class ExcelNewLoader
    {
        //Приложение Excel
        public Excel.Application ExlApp;
        //
        public Excel.Workbook ExlWb;
        //
        public Excel.Worksheet ExlWs;
        //
        readonly object _misValue = System.Reflection.Missing.Value;

        //Конструктор
        public ExcelNewLoader()
        {
            //Создание сервера Excel
            ExlApp = new Excel.Application {DisplayAlerts = false};
            
        }

        //обновление страницы(случай переопределения книги)
        public void RefreshWs()
        {
            ExlWs = (Excel.Worksheet)ExlWb.Worksheets.Item[1];
            ExlWs.Visible = Excel.XlSheetVisibility.xlSheetVisible;
        }

        public void GetWorkBook()
        {
            //Создание рабочей книги
            ExlWb = ExlApp.Workbooks.Add(_misValue);
            //Получение ссылки на лист1
            ExlWs = (Excel.Worksheet)ExlWb.Worksheets.Item[1];
            ExlWs.Visible = Excel.XlSheetVisibility.xlSheetVisible;
        }


        //обновление страницы(случай переопределения книги)
        public void GetWs(int i, string wsName)
        {
            if (i < 4)
            {
                ExlWs = (Excel.Worksheet)ExlWb.Worksheets[i];
                CheckWsName(ref wsName);
                ExlWs.Name = wsName;
            }
            else
            {
                //создаем новую страницу
                ExlWs = (Excel.Worksheet)ExlWb.Worksheets.Add(Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                ExlWs = (Excel.Worksheet)ExlWb.Worksheets[1];
                CheckWsName(ref wsName);
                ExlWs.Name = wsName;
            }
        }

        //
        public void CheckWsName(ref string wsName)
        {
            wsName = Regex.Replace(wsName, @"\s+", " ");

            if (wsName.Length >= 31)
                wsName = wsName.Substring(0, 28) + "...";
            wsName = wsName.Replace('/', '_');
            wsName = wsName.Replace('\'', '_');
            wsName = wsName.Replace('[', '_');
            wsName = wsName.Replace(']', '_');
            wsName = wsName.Replace('*', '_');
            wsName = wsName.Replace('?', '_');
        }

        //сортировка старниц книги по названию
        public void SortWs()
        {
            var sorted = new List<Excel.Worksheet>();

            foreach (Excel.Worksheet ws in ExlWb.Worksheets)
            {
                sorted.Add(ws);
            }

            //сортировка имен страниц книги
            sorted.Sort(delegate(Excel.Worksheet ws1, Excel.Worksheet ws2)
            {
                return String.Compare(ws1.Name, ws2.Name, StringComparison.Ordinal);
            });

            for (int i = 0; i < sorted.Count; i++)
            {
                for (int j = 1; j <= ExlWb.Worksheets.Count; j++)
                {
                    var action = (Excel.Worksheet)ExlWb.Worksheets[j];
                    if (sorted[i].Name == action.Name)
                    {
                        var wst = (Excel.Worksheet)ExlWb.Worksheets[i + 1];
                        action.Move(wst, Type.Missing);
                    }
                }
            }
            ((Excel.Worksheet)ExlWb.Sheets[1]).Select(Type.Missing);
        }

   
        //Сохранение файла на диск
        public Returns SaveFile(string path)
        {
            Returns ret = Utils.InitReturns();
            //Устанавливаем формат
            ExlApp.DefaultSaveFormat = Excel.XlFileFormat.xlWorkbookNormal;
            //???
            ExlWb.Saved = true;
            //Не Отображать сообщение о замене существующего
            ExlApp.DisplayAlerts = false;
            //Формат сохраняемого файла
            try
            {
                ExlWb.SaveAs(path, Excel.XlFileFormat.xlWorkbookNormal, //Excel.XlFileFormat.xlExcel9795,
                    Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Excel.XlSaveAsAccessMode.xlNoChange, Type.Missing,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                ExlWb.Save();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при сохранения документа : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = ex.Message;
                ret.result = false;
                return ret;
            }
            finally
            {
                
                ExlWb.Close();

            }
            return ret;
        }


        //Удаление объектов
        public void DeleteObject()//(ref Excel.Application ExlApp, ref Excel.Workbook ExlWb, ref Excel.Worksheet ExlWs)
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();

                Marshal.ReleaseComObject(ExlWs);
                ExlWb.Close(false, Type.Missing, Type.Missing);
                //ExlWb.Close(Type.Missing, Type.Missing, Type.Missing);
                Marshal.ReleaseComObject(ExlWb);

                ///////////////////////////////
                //добавить какой нибудь рестарт
                ///////////////////////////////
                ExlApp.Quit();
                Marshal.ReleaseComObject(ExlApp);

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("!!!" + ex.Message, MonitorLog.typelog.Error, true);
            }
        }

   

 
        /// <summary>
        /// Загружает шаблон в рабочую книгу. (шаблон должен находиться на первой странице)
        /// </summary>
        /// <param name="pathTemplate">Путь к файлу шаблона</param>
        /// <param name="data">Список параметров типа ключ - значение. Ключи в шаблоне заменяются на значения.</param>
        /// <returns>тип Returns</returns>
        public Returns LoadTemlate(string pathTemplate, Dictionary<string, string> data)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                //открыть шаблон
                ExlWb = ExlApp.Workbooks.Open(pathTemplate, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                //обновить ссылка на страницу(обязательно при смене ссылки на книгу)
                RefreshWs();
                //Количество строк с данными
                int iRowCount = ExlWs.UsedRange.Rows.Count + 2;

                //количество столбцов с данными
                int iColumnCount = ExlWs.UsedRange.Columns.Count;


                for (int j = 1; j <= iColumnCount; j++)
                {
                    for (int i = 1; i <= iRowCount; i++)
                    {
                        if (ExlWs.Cells[i, j] != null)
                        {
                            //значение ячейки 
                            var val = (Excel.Range)ExlWs.Cells[i, j];
                            string valCel = val.Value[Type.Missing] != null ? val.Value[Type.Missing].ToString() : "";

                            //if (valCel.Contains('[') && valCel.Contains(']'))
                            while (valCel.Contains("[") && valCel.Contains("]"))
                            {

                                string datName = valCel.Substring(valCel.IndexOf('[') + 1, valCel.IndexOf(']') - valCel.IndexOf('[') - 1);
                                string datValue;
                                if (data.TryGetValue(datName, out datValue))
                                {
                                    string strVal = valCel.Replace(datName, datValue);
                                    //strVal = strVal.Replace('[', ' '); 
                                    //strVal = strVal.Replace(']', ' ');
                                    strVal = strVal.Remove(strVal.IndexOf('['), 1);
                                    strVal = strVal.Remove(strVal.IndexOf(']'), 1);
                                    valCel = strVal.Trim();
                                    val.Value2 = valCel;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }

                ////УБРАТЬ! Тестовое сохранение!
                ////сохранение
                //this.SaveFile(@"C:\123.xls", "", ref this.ExlWb, ref this.ExlApp);

                return ret;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка замены данных в шаблоне : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = ex.Message;
            }
           

            return ret;
        }
        

    }


    public class SamaraKartSaldo : BaseSamaraReport
    {


        public SamaraKartSaldo(IDbConnection connDb, ExcelNewLoader excelL, int month, int year)
            : base(connDb,  excelL,  month,  year)
        {
           
        }

       

        private DataTable GetAnalisKartTable(int nzpArea, int nzpSupp)
        {

           
            DataTable localTable = null;

            try
            {

                DBManager.ExecSQL(_connDb, "drop table t_svod", false);


                string sql = " Create temp table t_svod( " +
                             " nzp_geu integer, " +
                             " nzp_serv integer, " +
                             " sum_insaldo " + DBManager.sDecimalType + "(14,2)," +
                             " rsum_tarif " + DBManager.sDecimalType + "(14,2)," +
                             " izm_tarif " + DBManager.sDecimalType + "(14,2)," +
                             " vozv " + DBManager.sDecimalType + "(14,2), " +
                             " reval_k " + DBManager.sDecimalType + "(14,2), " +
                             " reval_d " + DBManager.sDecimalType + "(14,2), " +
                             " sum_ito " + DBManager.sDecimalType + "(14,2), " +
                             " sum_charge " + DBManager.sDecimalType + "(14,2), " +
                             " sum_odn " + DBManager.sDecimalType + "(14,2), " +
                             " sum_insaldo_odn " + DBManager.sDecimalType + "(14,2), " +
                             " sum_money " + DBManager.sDecimalType + "(14,2), " +
                             " sum_outsaldo " + DBManager.sDecimalType + "(14,2))" +
                             DBManager.sUnlogTempTable;
                DBManager.ExecSQL(_connDb, sql, true);


                sql = " INSERT INTO t_svod(nzp_geu, nzp_serv,  sum_insaldo," +
                      "         rsum_tarif, izm_tarif, vozv, reval_k, reval_d,sum_ito, sum_charge " +
                      "         sum_odn, sum_insaldo_odn, sum_money, sum_outsaldo)" +
                      " SELECT nzp_geu, nzp_serv, sum(sum_insaldo) as sum_insaldo, " +
                      "        sum(rsum_tarif) as rsum_tarif, sum(izm_tarif) as izm_tarif, " +
                      "        sum(vozv) as vozv, sum(reval_k) as reval_k," +
                      "        sum(reval_d) as reval_d, sum(sum_charge) as sum_charge, " +
                      "        sum(sum_charge) as sum_charge, sum(0) as sum_odn, " +
                      "        sum(0) as sum_insaldo_odn, sum(sum_money) as sum_money, " +
                      "        sum(sum_outsaldo) as sum_outsaldo " +
                      " FROM t_calcreport";
                if (nzpArea > 0) sql += " AND nzp_area =" + nzpArea;
                if (nzpSupp > 0) sql += " AND nzp_supp =" + nzpSupp;
                sql +=" GROUP BY 1,2";

                DBManager.ExecSQL(_connDb, sql, true);


                
                sql = "update t_svod set nzp_serv = 17 "+
                      "where nzp_serv = 11";
                DBManager.ExecSQL(_connDb, sql, true);



                sql = "select 2 as tip, geu,abs(a.nzp_serv) as nzp_serv," +
                      " (case when a.nzp_serv in (6,7,14) then 'ВК '||trim(service)" +
                      "       else service end) as service ," +
                      " ordering, " +
                      "sum(sum_insaldo) as sum_insaldo, " +
                      "sum(rsum_tarif) as rsum_tarif, " +
                      "sum(0) as izm_tarif, " +
                      "sum(vozv) as vozv, " +
                      "sum(-1*" + DBManager.sNvlWord + "(reval_k,0)) as reval_k, " +
                      "sum(reval_d) as reval_d, " +
                      "sum(sum_ito) as sum_ito, " +
                      "sum(sum_charge) as sum_charge, " +
                      "sum(sum_money) as sum_money, " +
                      "sum(sum_outsaldo) as sum_outsaldo, " +
                      "sum(sum_odn) as sum_odn, " +
                      "sum(sum_insaldo_odn) as sum_insaldo_odn " +
                      "from  t_svod a, " +
                      Points.Pref + DBManager.sKernelAliasRest + "services s, " +
                      Points.Pref + DBManager.sDataAliasRest + "s_geu g " +
                      "where (case when a.nzp_serv in (-6,-7,-14) then -a.nzp_serv else a.nzp_serv end)=s.nzp_serv " +
                      " and a.nzp_geu=g.nzp_geu " +
                      " group by 1, 2, ordering,3, 4 " +
                      " UNION ALL " +
                      "select 1 as tip,'0',abs(a.nzp_serv) as nzp_serv, " +
                      "(case when a.nzp_serv in (6,7,14) then 'ВК '||trim(service) " +
                      "else service end) as service ," +
                      " ordering, " +
                      "sum(sum_insaldo) as sum_insaldo, " +
                      "sum(rsum_tarif) as rsum_tarif, " +
                      "sum(0) as izm_tarif, " +
                      "sum(vozv) as vozv, " +
                      "sum(-1*" + DBManager.sNvlWord + "(reval_k,0)) as reval_k, " +
                      "sum(reval_d) as reval_d, " +
                      "sum(sum_ito) as sum_ito, " +
                      "sum(sum_charge) as sum_charge, " +
                      "sum(sum_money) as sum_money, " +
                      "sum(sum_outsaldo) as sum_outsaldo, " +
                      "sum(sum_odn) as sum_odn, " +
                      "sum(sum_insaldo_odn) as sum_insaldo_odn " +
                      "from  t_svod a, " +
                      Points.Pref + DBManager.sKernelAliasRest + "services s, " +
                      Points.Pref + DBManager.sDataAliasRest + "s_geu g " +
                      "where (case when a.nzp_serv in (-6,-7,-14) then -a.nzp_serv" +
                      " else a.nzp_serv end)=s.nzp_serv " +
                      " and a.nzp_geu=g.nzp_geu " +
                      " group by 1, 2, 3,4,5 order by  1,2, 5, 4";
                localTable = DBManager.ExecSQLToTable(_connDb, sql);

                localTable.Columns.Remove("ordering");

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка формирования отчета Карточка аналитического учета " +
                    ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                DBManager.ExecSQL(_connDb, "drop table t_svod", false);
            }

            return localTable;
           
        }

        public List<string> GetReport(int nzpExcelUtility)
        {
            List<string> result = new List<string>();
            string[] months =
            {"","Январь","Февраль",
                "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                "Октябрь","Ноябрь","Декабрь"};

            string sql = " select a.nzp_area, b.area " +
                         " from t_calcreport a,"  +
                         Points.Pref + DBManager.sDataAliasRest+ "s_area b"+
                         " where a.nzp_area=b.nzp_area group by 1";
            try
            {

                DataTable dArea = DBManager.ExecSQLToTable(_connDb, sql);
                foreach (DataRow dr in dArea.Rows)
                {
                    DataTable dt = GetAnalisKartTable(Convert.ToInt32(dr["nzp_area"]), -1);
                    _fileName = nzpExcelUtility + " " +
                                months[_month] + " " + _year + " " +
                                dr["area"].ToString().Trim() + "Карта аналитического учета ";
                    GetAnalisKart(dt);
                    result.Add(_fileName);
                }
            }

            catch (Exception e)
            {
                MonitorLog.WriteLog("Карта аналитического учета " + e.Message,
                    MonitorLog.typelog.Error, true);
            }

            return result;
        }

        private void GetAnalisKart(DataTable dt)
        {
            if (dt == null) throw new Exception("Карта аналитического учета, нет данных");

            ExcelL.GetWorkBook();
            ExcelL.ExlWs.Rows.Font.Name = "Arial";

            DataRow[] drSelect = dt.Select("tip = 1");

            MakeListKartAnalis(drSelect, 1);

            drSelect = dt.Select("tip = 2");

            string oldGeu = "";
            int j = 2;
            foreach (DataRow dr in drSelect)
            {
                if (oldGeu != dr["geu"].ToString().Trim())
                {
                    oldGeu = dr["geu"].ToString().Trim();
                    DataRow[] drSelect2 = dt.Select("geu = '" + oldGeu + "'");
                    MakeListKartAnalis(drSelect2, j);
                    j++;
                }
            }

            ExcelL.SortWs();
            ExcelL.SaveFile(Constants.ExcelDir + _fileName);

        }

        public void MakeListKartAnalis(DataRow[] drSelect, int numList)
        {
            int excelRow = 7;

            ExcelL.GetWs(numList, "Лист " + numList);

            var sumColls = new decimal[11];

            for (int i = 0; i < 11; i++)
                sumColls[i] = 0;


            #region Создаем шапку

            Excel.Range excells1 = ExcelL.ExlWs.Range["A1", "K1"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "Карта аналитического учета";
            excells1.Font.Bold = true;
            excells1.HorizontalAlignment = Excel.Constants.xlCenter;

            excells1 = ExcelL.ExlWs.Range["A2", "K2"];
            excells1.Merge(Type.Missing);
            excells1.Font.Bold = true;

            string sName =String.Empty;//string s_name = SetReportHeader();

            excells1.FormulaR1C1 = sName + " за " +_month.ToString("00") + " " + _year + "г.";
            excells1.HorizontalAlignment = Excel.Constants.xlCenter;

            excells1 = ExcelL.ExlWs.Range["A3", "A3"];
            excells1.Merge(Type.Missing);
            excells1.Font.Bold = true;
            excells1.FormulaR1C1 = DateTime.Today.ToShortDateString();
            excells1.HorizontalAlignment = Excel.Constants.xlLeft;

            excells1 = ExcelL.ExlWs.Range["B3", "K4"];
            excells1.Font.Bold = true;
            excells1.Merge(Type.Missing);
            //                    excells1.FormulaR1C1 = "Управляющая организация:" + listprm[0].area;
            //excells1.FormulaR1C1 = SetReportConditional(Convert.ToInt32(nzp_user));
            excells1.HorizontalAlignment = Excel.Constants.xlLeft;
            excells1.RowHeight = 23;

            excells1 = ExcelL.ExlWs.Range["B5", "K5"];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = "";
            if (numList > 1)
            {
                excells1.FormulaR1C1 = "ЖЭУ: " + drSelect[0]["geu"];
            }
            excells1.HorizontalAlignment = Excel.Constants.xlLeft;
            excells1.Font.Bold = true;


            SetColumnName("A6", "A7", "Вид услуги:",29);
            SetColumnName("B6", "B7", "Сальдо 01." + _month.ToString("00") + "." + _year + " г.", 11);
            SetColumnName("C6", "H6", "Начислено", 0);
            SetColumnName("C7", "C7", "постоянно", 0);
            SetColumnName("D7", "D7", "кор-ка " + Convert.ToChar(10) + "тарифа " + Convert.ToChar(10) + "(МОП)", 0);
            SetColumnName("C7", "C7", "возврат", 0);
            SetColumnName("F7", "F7", "красные", 0);
            SetColumnName("G7", "G7", "черные", 0);
            SetColumnName("H7", "H7", "итого", 0);
            SetColumnName("I6", "I7", "к оплате", 0);
            SetColumnName("J6", "J7", "оплачено", 0);
            SetColumnName("K6", "K7", "Сальдо " + DateTime.DaysInMonth(_year, _month) +
                "." + _month.ToString("00") + "." + _year + " г.", 11);
            excelRow++;
            #endregion

            #region Пишем тело
            foreach (DataRow dr in drSelect)
            {
               
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

            string[] colarray = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K" };
            foreach (string ch in colarray)
            {
                excells1 = ExcelL.ExlWs.Range[ch + (footerRow), ch + (footerRow)];
                if (ch != "A")
                {
                    excells1.EntireColumn.NumberFormat = "# ##0,00";
                }
                excells1.Font.Bold = true;
                excells1.HorizontalAlignment = Excel.Constants.xlRight;
                excells1.EntireColumn.Font.Size = 8;
                if ((ch != "A") & (ch != "B") & (ch != "K"))
                    excells1.EntireColumn.AutoFit();

                excells1 = ExcelL.ExlWs.Range[ch + "7", ch + (footerRow)];
                excells1.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
            }

            excells1 = ExcelL.ExlWs.Range["A7", "A" + (footerRow)];
            excells1.EntireRow.AutoFit();

            //SetReportSing(_ExcelL, footerRow + 3);

            ExcelL.ExlWs.PageSetup.Orientation = Excel.XlPageOrientation.xlLandscape;


            #endregion


        }

        private void SetColumnName(string fromLetter, string toLetter, string colName, int defWidth)
        {
            Excel.Range excells1 = ExcelL.ExlWs.Range[fromLetter, toLetter];
            excells1.Merge(Type.Missing);
            excells1.FormulaR1C1 = colName;
            excells1.HorizontalAlignment = Excel.Constants.xlCenter;
            excells1.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
            excells1.Font.Size = 8;
            excells1.Font.Bold = true;
            if (defWidth > 0)
            excells1.ColumnWidth = defWidth;
        }
    }



}

