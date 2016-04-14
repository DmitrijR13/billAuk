using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Castle.Core.Internal;
using FastReport;
using STCLINE.KP50;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;
using Bars.KP50.Report;
using Constants = STCLINE.KP50.Global.Constants;

namespace Bars.KP50.Load.Obninsk
{

   
    /// <summary>Сводный отчет по начислениям для Тулы</summary>
    public class LoadSuppCharge : BaseSqlLoad
    {
        private int counterValCnt = 3;
        
        private string tableSuppCharge;
       
        public override string Name
        {
            get { return "Загрузка начислений сторонних поставщиков"; }
        }

        public override string Description
        {
            get { return "Загрузка начислений сторонних поставщиков в простом формате"; }
        }

        protected override byte[] Template
        {
            get { return null; }
        }

       

        /// <summary>
        /// Количество загружаемых строк
        /// </summary>
        private int _rowsCount;

        public override List<UserParam> GetUserParams()
        {
           return null;
        }

        protected override void PrepareParams()
        {
           
        }

      
        public override void LoadData()
        {
            
            Returns ret;
            STCLINE.KP50.Global.Utils.setCulture(); // установка региональных настроек
            tableSuppCharge =Points.Pref + "_upload" + DBManager.tableDelimiter +
            "file_supp_charge";

            try
            {
                SetProcessPercent(0, ExcelUtility.Statuses.InProcess);


                #region Загрузка данных из файла
                var fs = new FileStream(TemporaryFileName, FileMode.Open, FileAccess.Read);
                var buffer = new byte[fs.Length];
                fs.Position = 0;
                fs.Read(buffer, 0, buffer.Length);

                //win кодировка
                string st1251 = System.Text.Encoding.GetEncoding(1251).GetString(buffer);
                fs.Close();

                string[] listSt = st1251.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                _rowsCount = 0;
                #region Определяем заголовки

                ExecSQL("drop table t_supp_charge", false);

                string createTable = "Create temp table t_supp_charge(" +
                                     " nzp_s Serial," +
                                     " date_calc Date," +
                                     " adres char(200)," +
                                     " num_ls_supp char(40)," +
                                     " service char(40)," +
                                     " tarif "+DBManager.sDecimalType+"(14,2)," +
                                     " c_calc "+DBManager.sDecimalType+"(14,4)," +
                                     " measure char(20)," +
                                     " sum_real "+DBManager.sDecimalType+"(14,4)," +
                                     " sum_outsaldo "+DBManager.sDecimalType+"(14,2)," +
                                     " num_cnt1 char(20), " +
                                     " val_cnt1 " + DBManager.sDecimalType + "(14,2),"+
                                     " num_cnt2 char(20), " +
                                     " val_cnt2 " + DBManager.sDecimalType + "(14,2),"+
                                     " num_cnt3 char(20), " +
                                     " val_cnt3 " + DBManager.sDecimalType + "(14,2))"+
                                     DBManager.sUnlogTempTable;
                ExecSQL(createTable);
                
                var singleString = listSt[0].Split(';');

                string[] goodField =
                {
                    "Дата", "АдресАбонента", "ЛицевойСчетПоставщика", "Услуга",
                    "Тариф", "Расход", "Е", "Начисление", "ИсходящееСальдо",
                    "ПоказанияИПУ"
                };
                string[] goodRecord = new string[goodField.Length];
                string[] disassembledRecord;  // полностью разобранная строка из файла
                string[] counterVals;   // массив с показаниями ПУ

                List<int> rightColumn = new List<int>();
                int fieldIndex = 0;

                for (int j = 0; j < singleString.Length; j++)
                {
                    fieldIndex = Array.IndexOf(goodField, singleString[j]);
                    if (fieldIndex > -1)
                    {
                        rightColumn.Add(fieldIndex);
                    }
                }
                #endregion

                #region Считывание файла во временную таблицу
                for (int i = 1; i<listSt.Length; i++)
                {
                    var singleStrings = listSt[i].Split(';');
                    for (int j = 0; j < rightColumn.Count; j++)
                    {
                        goodRecord[rightColumn[j]] = singleStrings[j];
                    }

                    // ... если показания ПУ указаны
                    if (!String.IsNullOrEmpty(goodRecord[9]))
                    {
                        counterVals = goodRecord[9].Split('#');
                        
                        for (int counterValIndex = 0; counterValIndex < counterVals.Length; counterValIndex++)
                        {
                            if (counterVals[counterValIndex] == "НЕТ") counterVals[counterValIndex] = null;
                        }

                        disassembledRecord = new string[goodRecord.Length - 1 + counterVals.Length];

                        // ... порядок важен
                        // ... сначала вся информация, кроме показаний ПУ
                        goodRecord.CopyTo(disassembledRecord, 0);
                        // ... только потом показания ПУ
                        counterVals.CopyTo(disassembledRecord, goodRecord.Length - 1);
                    }
                    else
                    {
                        disassembledRecord = new string[goodRecord.Length + counterValCnt * 2 - 1];
                        goodRecord.CopyTo(disassembledRecord, 0);
                    }


                    try
                    {

                        string sql = " insert into t_supp_charge(date_calc, adres, num_ls_supp, service, tarif," +
                                     "c_calc, measure, sum_real, sum_outsaldo, " + 
                                     "num_cnt1, val_cnt1, num_cnt2, val_cnt2, num_cnt3, val_cnt3)" +
                                     "values('" + disassembledRecord[0] + "'," +
                                     "'" + disassembledRecord[1] + "'," +
                                     "'" + disassembledRecord[2] + "'," +
                                     "'" + disassembledRecord[3] + "'," +
                                     (String.IsNullOrEmpty(disassembledRecord[4]) ? "NULL" : disassembledRecord[4]) + "," +
                                     (String.IsNullOrEmpty(disassembledRecord[5]) ? "NULL" : disassembledRecord[5]) + "," +
                                     (String.IsNullOrEmpty(disassembledRecord[6]) ? "NULL" : "'" + disassembledRecord[6] + "'") + "," +
                                     (String.IsNullOrEmpty(disassembledRecord[7]) ? "NULL" : "'" + disassembledRecord[7] + "'") + "," +
                                     (String.IsNullOrEmpty(disassembledRecord[8]) ? "NULL" : disassembledRecord[8]) + "," +
                                     (String.IsNullOrEmpty(disassembledRecord[9]) ? "NULL" : "'" + disassembledRecord[9] + "'") + "," +
                                     (String.IsNullOrEmpty(disassembledRecord[10]) ? "NULL" : disassembledRecord[10]) + "," +
                                     (String.IsNullOrEmpty(disassembledRecord[11]) ? "NULL" : "'" + disassembledRecord[11] + "'") + "," +
                                     (String.IsNullOrEmpty(disassembledRecord[12]) ? "NULL" : disassembledRecord[12]) + "," +
                                     (String.IsNullOrEmpty(disassembledRecord[13]) ? "NULL" : "'" + disassembledRecord[13] + "'") + "," +
                                     (String.IsNullOrEmpty(disassembledRecord[14]) ? "NULL" : disassembledRecord[14]) + ")";
                        ExecSQL(sql);
                        _rowsCount++;
                    }
                    catch 
                    {

                        Protokol.AddUncorrectedRow("Строка № "+i+"   " + listSt[i]);
                    }


                }

                #endregion

                #endregion


                #region Сохранение начислений в постоянную таблицу

                string sqls = " insert into " + tableSuppCharge+
                              "(nzp_load, nzp_supp, calc_year, calc_month, date_calc, adres, num_ls_supp, service, tarif," +
                              "        c_calc, measure, sum_real, sum_outsaldo, num_cnt1, val_cnt1, num_cnt2, val_cnt2, num_cnt3, val_cnt3) " +
                              " select "+NzpLoad+", " + NzpSupp + "," + DateLoad.Year + "," + DateLoad.Month + ",date_calc, adres, num_ls_supp, " +
                              "  service, tarif," +
                              "        c_calc, measure, sum_real, sum_outsaldo, num_cnt1, val_cnt1, num_cnt2, val_cnt2, num_cnt3, val_cnt3" +
                              " from t_supp_charge";
                ExecSQL(sqls);

                ExecSQL("drop table t_counts");
                #endregion

             
                //записываем данные в систему: pack,pack_ls,pu_vals 
                ret = SaveChargeInBase();
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка записи счетчиков в систему",
                        MonitorLog.typelog.Error, 20, 201, true);

                }
                else
                {
                    ExecSQL("UPDATE " + Points.Pref + DBManager.sDataAliasRest + "simple_load SET " + 
                        " parsing_status = 1 " + 
                        " WHERE nzp_load=" + NzpLoad);
                    SetProcessPercent(100, ExcelUtility.Statuses.Success);
                }

            }
            catch (UserException ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                Protokol.AddComment(ex.Message);
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            
        }

        protected override IDbConnection Connection { get; set; }
        
        protected  override void InsertReestr()
        {
            var myFile = new DBMyFiles();
            var ret = myFile.AddFile(new ExcelUtility
            {
                nzp_user = ReportParams.User.nzp_user,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = Name,
                is_shared = 1
            });
            if (!ret.result) return;
            NzpExcelUtility = ret.tag;

            string sqlStr = "INSERT INTO " + Points.Pref + DBManager.sDataAliasRest + "simple_load " +
                            "(file_name, nzp, nzp_wp, month_, year_, " +
                            " created_by, created_on, tip, download_status ) " +
                            "VALUES " +
                            " ( '" + FileName + "' ,"+
                            NzpSupp + "," + NzpWp + "," + DateLoad.Month + "," + DateLoad.Year + "," +
                            ReportParams.User.nzp_user + ", " + DBManager.sCurDateTime + ", " + (int)SimpLoadTypeFile + ", "+2+" )";

            ExecSQL(sqlStr);
            NzpLoad = GetSerialValue();
        }

        /// <summary>
        /// Сохранение счетчиков в базе данных
        /// </summary>
        /// <returns></returns>
        private Returns SaveChargeInBase()
        {
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();
        
            if (NzpWp > 0)
            { 
                string sqlStr = " select trim(bd_kernel) as pref from " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "s_point where nzp_wp = " + NzpWp;
                DataTable dt = ExecSQLToTable(sqlStr);
                Pref = dt.Rows[0][0].ToString().Trim();
            }

            #region Проставляем лицевые счета
            string sql = " update  " + tableSuppCharge + " set nzp_wp = " + NzpWp + " where nzp_load = " + NzpLoad;
            ExecSQL(sql);

            sql = " update  " + tableSuppCharge + " set nzp_kvar =" +
                  " (select max(nzp_kvar) " +
                  " from " + Pref + DBManager.sDataAliasRest + "alias_ls ls" +
                  " where ls.numls=num_ls_supp and ls.nzp_supp = " + tableSuppCharge + ".nzp_supp) " +
                  " where nzp_load = " + NzpLoad;
            ExecSQL(sql);
            #endregion

            #region Получение протокола несопоставленных записей

            sql = " select * from " + tableSuppCharge + 
                  " where nzp_load=" + NzpLoad +
                  " and nzp_kvar is null";
            var badLsTable = ExecSQLToTable(sql);
            foreach (DataRow dr in badLsTable.Rows)
            {
                Protokol.AddUnrecognisedRow(" Не удалось соспоставить ЛС "+dr["num_ls_supp"].ToString().Trim()
                    +" адрес "+dr["adres"].ToString().Trim());
            }

            #endregion

            return ret;
        }


        /// <summary>
        /// Получение протокола по загрузке
        /// </summary>
        /// <returns></returns>
        public override string GetProtocolName()
        {
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();
            if (!ret.result)
            {
                return String.Empty;
            }

            #region Формирование протокола

            var myFile = new DBMyFiles();
            
            
            string statusName = "Успешно";
            int download_status = 1;

            var rep = new FastReport.Report();
            
            if (Protokol.UnrecognizedRows.Rows.Count > 0 || Protokol.Comments.Rows.Count > 0)
            {
                Protokol.SetProcent(100, ExcelUtility.Statuses.Failed);
                statusName = "Загружено с ошибками";
                download_status = 0;
            }
            if (Protokol.UncorrectRows.Rows.Count > 0)
            {
                statusName = "Загружено с ошибками";
                download_status = 0;
            }

            var env = new EnvironmentSettings();
            env.ReportSettings.ShowProgress = false;

            DataSet fDataSet = new DataSet();
            fDataSet.Tables.Add(Protokol.UnrecognizedRows);
            fDataSet.Tables.Add(Protokol.Comments);
            fDataSet.Tables.Add(Protokol.UncorrectRows);

            string template = PathHelper.GetReportTemplatePath("protocol_std.frx");
            rep.Load(template);
            rep.RegisterData(fDataSet);
            rep.GetDataSource("comment").Enabled = true;
            rep.GetDataSource("unrecog").Enabled = true;
            rep.GetDataSource("uncorrect").Enabled = true;
            rep.SetParameterValue("status", statusName);
            rep.SetParameterValue("count_rows", _rowsCount);
            rep.Prepare();

            var exportXls = new FastReport.Export.OoXML.Excel2007Export();
            string fileName = "protocol_" + DateTime.Now.ToShortDateString().Replace(".", "_") + "_" +
                              DateTime.Now.ToShortTimeString().Replace(":", "_") + ".xlsx";
            exportXls.ShowProgress = false;
            MonitorLog.WriteLog(fileName, MonitorLog.typelog.Info, 20, 201, true);
            try
            {
                if (!Directory.Exists(Constants.Directories.ReportDir))
                {
                    Directory.CreateDirectory(Constants.Directories.ReportDir);
                }
                exportXls.Export(rep, Path.Combine(Constants.Directories.ReportDir, fileName));
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
            }

            rep.Dispose();


            //перенос  на ftp сервер
            if (InputOutput.useFtp)
            {
                fileName = InputOutput.SaveOutputFile(STCLINE.KP50.Global.Constants.ExcelDir + fileName);
            }

            ProtocolFileName = fileName;


            ExecSQL("UPDATE " + Points.Pref + DBManager.sDataAliasRest + "simple_load " +
                    " SET nzp_exc=" + NzpExcelUtility + ", download_status = " + download_status +", temp_file='"+TemporaryFileName+"'"+
                    " WHERE nzp_load=" + NzpLoad);
            
            myFile.SetFileState(new ExcelUtility
            {
                nzp_exc = NzpExcelUtility,
                status = ExcelUtility.Statuses.Success,
                exc_path = fileName
            });

            #endregion

            return ProtocolFileName;

        }

      

    }
}