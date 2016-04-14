using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using Bars.KP50.DB.Exchange.UnloadKapRem;
using Castle.Core.Internal;
using FastReport;
using STCLINE.KP50;
using STCLINE.KP50.DataBase;
using System.IO;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;
using Constants = STCLINE.KP50.Global.Constants;

namespace Bars.KP50.DB.Exchange.UnloadKapRem
{
    public class UnloadKapRemont
    {
        /// <summary>
        /// Выгрузка данных по услугам Капитальный ремонт и Наем
        /// </summary>
        /// <returns></returns>
        public void StartWithObject(object obj)
        {
            var ret = Utils.InitReturns();
            var unloadKapRem = new UnloadKapRem();
            var nzpUser = ((FilesImported)obj).nzp_user;
            var month = ((FilesImported)obj).month;
            var year = ((FilesImported)obj).year;
            var pref = ((FilesImported)obj).pref;
            unloadKapRem.StartUnloadKapRem(out ret, nzpUser, year, month, pref);
        }
    }

    public class UnloadKapRem : BaseUnloadKapRem
    {

        
        protected string ArchiveName;

        protected string DatS;
        protected string DatPo;
        protected string MyCharge;
        protected string ChargeXx;
        protected string[] Months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
        protected string point;
        protected int _year;
        protected int _month;
        protected string Pref;
        protected List<string> ListFileName;
        protected List<_Point> _listPrefix; 

        /// <summary>
        /// Выгрузка данных
        /// </summary>
        /// <param name="nzpUser"> Код пользователя </param>
        /// <param name="year"> Год </param>
        /// <param name="month"> Месяц </param>
        /// <param name="pref"> Префикс банка данных </param>
        /// <returns></returns>
        public override Returns StartUnloadKapRem(out Returns ret, int nzpUser, string year, string month, string pref)
        {
            //Returns ret = Utils.InitReturns();
            ListFileName = new List<string>();
            _listPrefix = new List<_Point>();
            try
            {
                //открыть соединение
                OpenConnection();
                
                //проверка переданных параметров
                if (!CheckInParams(out ret, nzpUser, year, month, pref))
                {
                    ChangeStatus(ExcelUtility.Statuses.Failed);
                    //return ret;
                }

                //добавление записи в excel_utility
                InsertReest();

                //создание временной таблицы t_kaprem
                CreateTempTableKapRem();

                int countBank = _listPrefix.Count;
                int progress = 0;

                //по банкам данных
                foreach (var prefix in _listPrefix)
                {
                    point = prefix.point;
                    Pref = prefix.pref;
                    MyCharge = Pref + ChargeXx;
                    
                    
                    //сформировать файл
                    GetFileUnload(out ret);
                    if (!ret.result)
                    {
                        ChangeStatus(ExcelUtility.Statuses.Failed);
                        DeleteFiles(ListFileName);
                        return ret;
                    }
                    
                    //установить процент выполнения
                    progress++;
                    ChangeProcess((decimal)(progress*(90/countBank))/100);

                }
                
                //сформировать отчет
                GetReport(out ret);
                if (!ret.result)
                {
                    CommentList.Add("Файл отчета не сформирован");
                }

                //сформировать протокол
                GetProtokol(out ret);
                if (!ret.result)
                {
                    ChangeStatus(ExcelUtility.Statuses.Failed);
                    DeleteFiles(ListFileName);
                    return ret;
                }

                //формирование архива с полученными файлами выгрузки
                GetArchive(out ret);
                if (!ret.result)
                {
                    ChangeStatus(ExcelUtility.Statuses.Failed);
                    DeleteFiles(ListFileName);
                    return ret;
                }
                
                ChangeProcessStatus(1, ExcelUtility.Statuses.Success);

                //удаление временной таблицы t_kaprem
                DropTempTableKapRem();

                return ret;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка в функции UnloadKapRem.StartUnloadKapRem()\n" + ex, MonitorLog.typelog.Error, true);
                ret = new Returns(false, "Ошибка выгрузки данных", -1);
                return ret;
            }

            finally
            {
                CloseConnection();
            }
        }

        /// <summary> Cформировать строку для записи в файл </summary>
        /// <param name="row">DataRow</param>
        /// <returns>string</returns>
        protected string CreateString(DataRow row)
        {
            int nzp_serv = (row["nzp_serv"] != DBNull.Value ? Convert.ToInt32(row["nzp_serv"]) : 0);
            decimal kap_tarif = (row["kap_tarif"] != DBNull.Value ? Convert.ToDecimal(row["kap_tarif"]) : 0);
            string privat = (row["privat"] != DBNull.Value ? row["privat"].ToString().Trim() : "");
            if (privat == "true" && kap_tarif > 0)
            {
                if (nzp_serv == 15)
                {
                    CommentList.Add("Банк данных - " + point + ": ЛС №" + (row["num_ls"] != DBNull.Value ? row["num_ls"].ToString().Trim() : "") + 
                        " есть начисления по услуге \"Наем\", помещение \"Приватизировано\": Должно быть \"Не приватизировано\";");
                }
            }
            else
            {
                if (privat == "false" && kap_tarif > 0)
                {
                    if (nzp_serv == 269 || nzp_serv == 356)
                    {
                        CommentList.Add("Банк данных - " + point + ": ЛС №" + (row["num_ls"] != DBNull.Value ? row["num_ls"].ToString().Trim() : "") +
                            " есть начисления по услуге \"Капитальный ремонт\", помещение \"Не приватизировано\": Должно быть \"Приватизировано\";");
                    }
                }
            }
            string s = String.Empty;
            s += (row["id_file"] != DBNull.Value ? row["id_file"].ToString().Trim() + "|" : "|");
            s += (row["id_bill"] != DBNull.Value ? row["id_bill"].ToString().Trim() + "|" : "|");
            s += (row["id_ish"] != DBNull.Value ? row["id_ish"].ToString().Trim() + "|" : "|");
            s += (row["num_ls"] != DBNull.Value ? row["num_ls"].ToString().Trim() + "|" : "|");
            s += (row["nkvar"] != DBNull.Value ? row["nkvar"].ToString().Trim() + "|" : "|");
            s += (row["ob_pl_kv"] != DBNull.Value ? row["ob_pl_kv"].ToString().Trim() + "|" : "|");
            s += (row["gil_pl_kv"] != DBNull.Value ? row["gil_pl_kv"].ToString().Trim() + "|" : "|");
            s += (row["kol_komn"] != DBNull.Value ? row["kol_komn"].ToString().Trim() + "|" : "|");
            s += (row["ls_state"] != DBNull.Value ? row["ls_state"].ToString().Trim() + "|" : "|");
            s += (row["ne_gil"] != DBNull.Value ? row["ne_gil"].ToString().Trim() + "|" : "|");
            s += (row["period"] != DBNull.Value ? Convert.ToDateTime(row["period"].ToString().Trim()).ToShortDateString() + "|" : "|");
            s += (row["pkod"] != DBNull.Value ? row["pkod"].ToString().Trim() + "|" : "|");
            s += (row["sum_insaldo"] != DBNull.Value ? row["sum_insaldo"].ToString().Trim() + "|" : "|");
            s += (row["sum_outsaldo"] != DBNull.Value ? row["sum_outsaldo"].ToString().Trim() + "|" : "|");
            s += (row["sum_real"] != DBNull.Value ? row["sum_real"].ToString().Trim() + "|" : "|");
            s += (row["sum_charge"] != DBNull.Value ? row["sum_charge"].ToString().Trim() + "|" : "|");
            s += (row["sum_money"] != DBNull.Value ? row["sum_money"].ToString().Trim() + "|" : "|");
            s += (row["reval"] != DBNull.Value ? row["reval"].ToString().Trim() + "|" : "|");
            s += (row["kap_tarif"] != DBNull.Value ? row["kap_tarif"].ToString().Trim() + "|" : "|");
            s += (row["ls_dat_s"] != DBNull.Value ? Convert.ToDateTime(row["ls_dat_s"].ToString().Trim()).ToShortDateString() + "|" : "|");
            s += (row["ls_dat_po"] != DBNull.Value ? Convert.ToDateTime(row["ls_dat_po"].ToString().Trim()).ToShortDateString() + "|" : "|");
            s += (row["nkom"] != DBNull.Value ? row["nkom"].ToString().Trim() + "|" : "|");
            s += (row["nzp_serv"] != DBNull.Value ? row["nzp_serv"].ToString().Trim() + "|" : "|");
            s += (row["privat"] != DBNull.Value ? row["privat"].ToString().Trim() + "|" : "|");
            s += (row["graj"] != DBNull.Value ? row["graj"].ToString().Trim() + "|" : "|");
            s += (row["town"] != DBNull.Value ? row["town"].ToString().Trim() + "|" : "|");
            s += (row["ulica"] != DBNull.Value ? row["ulica"].ToString().Trim() + "|" : "|");
            s += (row["ndom"] != DBNull.Value ? row["ndom"].ToString().Trim() + "|" : "|");
            s += (row["nkor"] != DBNull.Value ? row["nkor"].ToString().Trim() + "|" : "|");
            return s;
        }

        /// <summary> Создание временной таблицы t_kaprem </summary>
        public override void CreateTempTableKapRem()
        {
            string tblName = " t_kaprem ";
            string columnsName = " id_file      INTEGER,                 " +
                                       " id_bill      CHAR(20),                " +
                                       " id_ish       CHAR (40),               " +
                                       " num_ls       INTEGER,                 " +
                                       " nkvar        CHAR(10),                " +
                                       " ob_pl_kv     CHAR(20),                " +
                                       " gil_pl_kv    CHAR(20),                " +
                                       " kol_komn     CHAR(20) ,               " +
                                       " ls_state     CHAR(100),               " +
                                       " ne_gil       CHAR(5) DEFAULT 'false', " +
                                       " period       DATE,                    " +
                                       " pkod         " + DBManager.sDecimalType + " (13, 0), " +
                                       " sum_insaldo  " + DBManager.sDecimalType + " (14, 2), " +
                                       " sum_outsaldo " + DBManager.sDecimalType + " (14, 2), " +
                                       " sum_real     " + DBManager.sDecimalType + " (14, 2), " +
                                       " sum_charge   " + DBManager.sDecimalType + " (14, 2), " +
                                       " sum_money    " + DBManager.sDecimalType + " (14, 2), " +
                                       " reval        " + DBManager.sDecimalType + " (14, 2), " +
                                       " kap_tarif    " + DBManager.sDecimalType + " (14, 2), " +
                                       " ls_dat_s     DATE,                    " +
                                       " ls_dat_po    DATE,                    " +
                                       " nkom         CHAR(10),                " +
                                       " nzp_dom      INTEGER,                 " +
                                       " nzp_kvar     INTEGER,                 " +
                                       " nzp_serv     INTEGER,                 " +
                                       " privat       CHAR(5) DEFAULT 'false', " +
                                       " graj         CHAR(40),                " +
                                       " town         CHAR(30),                " +
                                       " ulica        CHAR(40),                " +
                                       " ndom         CHAR(15),                " +
                                       " nkor         CHAR(15)                 ";

            ExecSQL(" DROP TABLE " + tblName, false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1}) ", tblName, columnsName));

            ExecSQL(" CREATE INDEX ix_tkr1 ON t_kaprem (num_ls); ");
            ExecSQL(" CREATE INDEX ix_tkr2 ON t_kaprem (nzp_kvar); ");
            
            tblName = " t_report ";
            columnsName =
                " ne_gil       CHAR(5) DEFAULT 'false', " +
                " sum_real     " + DBManager.sDecimalType + " (14, 2), " +
                " sum_money    " + DBManager.sDecimalType + " (14, 2), " +
                " nzp_dom      INTEGER,                 " +
                " nzp_serv     INTEGER,                 " +
                " graj         CHAR(40),                " +
                " town         CHAR(30),                " +
                " ulica        CHAR(40),                " +
                " ndom         CHAR(15),                " +
                " nkor         CHAR(15),                " +
                " period       DATE,                    " +
                " ob_pl_kv     " + DBManager.sDecimalType + " (14, 2) ";

            ExecSQL(" DROP TABLE " + tblName, false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1}) ", tblName, columnsName));

            tblName = " t_report_p ";
            columnsName =
                " ne_gil       CHAR(5) DEFAULT 'false', " +
                " sum_real     " + DBManager.sDecimalType + " (14, 2), " +
                " sum_money    " + DBManager.sDecimalType + " (14, 2), " +
                " nzp_dom      INTEGER,                 " +
                " nzp_serv     INTEGER,                 " +
                " graj         CHAR(40),                " +
                " town         CHAR(30),                " +
                " ulica        CHAR(40),                " +
                " ndom         CHAR(15),                " +
                " nkor         CHAR(15),                " +
                " period       DATE,                    " +
                " ob_pl_kv     CHAR(20)                ";

            ExecSQL(" DROP TABLE " + tblName, false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1}) ", tblName, columnsName));
        }

        public void CreateTempTableServKaprem()
        {
            string tblName = "t_serv_kaprem";
            string columnsName =
                " nzp_serv SERIAL NOT NULL, " +
                " nzp_serv_k INTEGER, " +
                " nzp_supp INTEGER, " +
                " dat_s DATE, " +
                " dat_po DATE ";

            ExecSQL(" DROP TABLE " + tblName, false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1}) ", tblName, columnsName));
            ExecSQL(String.Format(" CREATE UNIQUE INDEX ix_serv_kap_2 ON {0} (nzp_serv, nzp_supp, dat_s, dat_po); ", tblName));
        }

        public void DropTempTableServKaprem()
        {
            string tblName = "t_serv_kaprem";
            
            ExecSQL(" DROP TABLE " + tblName, false);
        }

        /// <summary> Удаление временной таблицы t_kaprem </summary>
        public override void DropTempTableKapRem()
        {
            ExecSQL(" DROP TABLE t_kaprem ", false);

            ExecSQL(" DROP TABLE t_report_p ", false);

            ExecSQL(" DROP TABLE t_report ", false);
        }

        /// <summary>
        /// Проверка переданных параметров
        /// </summary>
        /// <param name="ret"></param>
        /// <param name="nzp_user"> Код пользователя</param>
        /// <param name="year"> Год выгрузки </param>
        /// <param name="month"> Месяц выгрузки </param>
        /// <param name="pref">Префикс банка данных</param>
        /// <returns></returns>
        private bool CheckInParams(out Returns ret, int nzp_user, string year, string month, string pref)
        {
            ret = Utils.InitReturns();
            try
            {
                if (nzp_user < 1)
                {
                    ret.result = false;
                    ret.text = "Не определен пользователь";
                    return false;
                }

                if (year == null || month == null)
                {
                    ret.result = false;
                    ret.text = "Не определен расчетный месяц";
                    return false;
                }

                if (!pref.IsNullOrEmpty())
                {
                    _listPrefix.Add(new _Point { pref = pref });
                }
                else
                {
                    _listPrefix = Points.PointList;
                }
                //{
                //    ret.result = false;
                //    ret.text = "Не определен банк данных для выгрузки";
                //    return false;
                //}
                _year = Convert.ToInt32(year);
                _month = Convert.ToInt32(month);
                
                DatS = " CAST ('" + year + "-" + month + "-01' AS DATE) ";
                DatPo = " CAST ('" + year + "-" + month + "-" + DateTime.DaysInMonth(_year, _month) + "' AS DATE) ";

                NzpUser = nzp_user;

                ChargeXx =  "_charge_" + _year.ToString().Substring(2, 2) + DBManager.tableDelimiter + "charge_" +
                               _month.ToString("00");
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Не корректные входные данные";
                MonitorLog.WriteLog("CheckInParams(out Returns ret, int nzp_user, string year, string month, List<string> listPrefs): Ошибка\n" + ex, MonitorLog.typelog.Error, true);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Получить имя файла выгрузки
        /// </summary>
        /// <param name="ret"> </param>
        private void GetFilename(out Returns ret)
        {
            //ret = Utils.InitReturns();
            try
            {
                //наименование расчетного центра
                string rajon = GetNameRc(out ret);

                if (ret.result)
                {
                    string raj = rajon.Replace(" ", "_");
                    var dat_s = new DateTime(_year, _month, 1);
                    var dat_po = new DateTime(_year, _month, DateTime.DaysInMonth(_year, _month));
                    var curDate = DateTime.Now;
                    FileName = "KapRem_" + dat_s.ToString("yyyyMMdd") + "_" + dat_po.ToString("yyyyMMdd") + raj + "_" +
                               curDate.ToString("yyyyMMdd_HHmmssfffffff") + ".csv";
                    if (FileName.IsNullOrEmpty())
                        ret = new Returns(false, "Ошибка при формировании имени файла", -1);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при формировании имени файла\n" + ex, MonitorLog.typelog.Error, true);
                ret = new Returns(false, "Ошибка при формировании имени файла", -1);
            }
        }

        /// <summary>
        /// Получить имя файла архива
        /// </summary>
        /// <param name="ret"></param>
        /// <returns></returns>
        private void GetArchiveFilename(out Returns ret)
        {
            ret = Utils.InitReturns();

            ArchiveName = String.Empty;

            try
            {
                var dat_s = new DateTime(_year, _month, 1);
                var dat_po = new DateTime(_year, _month, DateTime.DaysInMonth(_year, _month));
                var curDate = DateTime.Now;
                ArchiveName = "KapRem_" + dat_s.ToString("yyyyMMdd") + "_" + dat_po.ToString("yyyyMMdd") + "_" +
                              curDate.ToString("yyyyMMdd_HHmmssff") + ".zip";
                if (ArchiveName.IsNullOrEmpty())
                    ret = new Returns(false, "Ошибка при формировании имени архива", -1);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при формировании имени архива\n" + ex, MonitorLog.typelog.Error, true);
                ret = new Returns(false, "Ошибка при формировании имени архива", -1);
            }

        }

        /// <summary>
        /// Добавление записи о выгрузке в базу данных
        /// </summary>
        private void InsertReest()
        {
            var myFile = new DBMyFiles();
            var ret = myFile.AddFile(new ExcelUtility()
            {
                nzp_user = NzpUser,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = "Выгрузка данных по услугам \"Капитальный ремонт\" и \"Наем\" за " + Months[_month] + " " + _year,
                is_shared = 1
            });
            
            if(!ret.result) return;

            NzpExcelUtility = ret.tag;
        }
        
        /// <summary>
        /// Запись о процесссе и статусе выгрузки в базу данных
        /// </summary>
        /// <param name="progress"> Прогресс </param>
        /// <param name="status"> Статус </param>
        private void ChangeProcessStatus(decimal progress, ExcelUtility.Statuses status)
        {
            var myFile = new DBMyFiles();
            myFile.SetFileProgress(NzpExcelUtility, progress);
            myFile.SetFileStatus(NzpExcelUtility, status);
            if (status == ExcelUtility.Statuses.Success || status == ExcelUtility.Statuses.Failed)
            {
                string sql = " UPDATE " + DBManager.sDefaultSchema + "excel_utility " +
                      " SET dat_out= " + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) +
                      " WHERE nzp_exc = " + NzpExcelUtility;
                ExecSQL(sql);

                if (status == ExcelUtility.Statuses.Success)
                {
                    myFile.SetFilePath(new ExcelUtility
                    {
                        nzp_exc = NzpExcelUtility,
                        exc_path = ArchiveName
                    });
                }
            }
        }

        /// <summary>
        /// Установить прогресс вызрузки
        /// </summary>
        /// <param name="progress"> Прогресс </param>
        private void ChangeProcess(decimal progress)
        {
            var myFile = new DBMyFiles();
            myFile.SetFileProgress(NzpExcelUtility, progress);
        }

        /// <summary>
        /// Установить статус вызрузки
        /// </summary>
        /// <param name="status"> Статус </param>
        private void ChangeStatus(ExcelUtility.Statuses status)
        {
            var myFile = new DBMyFiles();
            myFile.SetFileStatus(NzpExcelUtility, status);
            if (status == ExcelUtility.Statuses.Success || status == ExcelUtility.Statuses.Failed)
            {
                string sql = " UPDATE " + DBManager.sDefaultSchema + "excel_utility " +
                      " SET dat_out= " + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) +
                      " WHERE nzp_exc = " + NzpExcelUtility;
                ExecSQL(sql);

                if (status == ExcelUtility.Statuses.Success)
                {
                    myFile.SetFilePath(new ExcelUtility
                    {
                        nzp_exc = NzpExcelUtility,
                        exc_path = ArchiveName
                    });
                }
            }
        }

        /// <summary>
        /// Удаление файлов при неудачной архивации
        /// </summary>
        /// <param name="files"> Список файлов </param>
        private void DeleteFiles(IEnumerable<string> files)
        {
            if (files == null) throw new ArgumentNullException("files");
            try
            {
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка удаления файлов " + ex, MonitorLog.typelog.Error, true);
            }
        }

        /// <summary>
        /// Формирование архива с файлами
        /// </summary>
        /// <param name="ret"></param>
        private void GetArchive(out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                //формирование имени файла архива
                GetArchiveFilename(out ret);

                if (ret.result)
                {
                    if (ListFileName.Count != 0)
                    {
                        Archive.GetInstance(ArchiveFormat.Zip)
                            .Compress(Path.Combine(Constants.Directories.ReportDir, ArchiveName), ListFileName.ToArray(),
                                true);
                    }
                    else
                    {
                        ret = new Returns(false, "Нет сформированных файлов", -1);
                        MonitorLog.WriteLog("GetArchive(out Returns ret, List<string> listFile, string archName): " +
                                            "Нет сформированных файлов для архивации", MonitorLog.typelog.Error, true);
                        return;
                    }

                    if (File.Exists(Path.Combine(Constants.Directories.ReportDir, ArchiveName)))
                    {
                        if (InputOutput.useFtp)
                            ArchiveName =
                                InputOutput.SaveOutputFile(Path.Combine(Constants.Directories.ReportDir, ArchiveName));
                    }
                    else
                    {
                        MonitorLog.WriteLog("GetArchive(out Returns ret, List<string> listFile, string archName):" +
                                            " Файл " + ArchiveName + " не сформирован", MonitorLog.typelog.Error, true);
                        ret = new Returns(false, "Файл не сформирован", -1);
                    }
                }
            }
            catch(Exception ex)
            {
                MonitorLog.WriteLog("Ошибка архивации\n" + ex, MonitorLog.typelog.Error, true);
                ret = new Returns(false, "Ошибка архивации", -1);
            }
        }

        /// <summary>
        /// Получить наименование расчетного центра
        /// </summary>
        /// <param name="ret"></param>
        /// <returns></returns>
        private string GetNameRc(out Returns ret)
        {
            ret = Utils.InitReturns();
            string nameRc = String.Empty;
            try
            {
                MyDataReader reader;
                string sql = " SELECT val_prm FROM " + Pref + DBManager.sDataAliasRest + "prm_10 " +
                             " WHERE nzp_prm = 80 AND is_actual <> 100 AND " +
                             " dat_s <= " + DatPo + " AND dat_po >= " + DatS;
                ExecRead(out reader, sql);

                while (reader.Read())
                {
                    nameRc += (reader["val_prm"] != DBNull.Value ? "_" + reader["val_prm"].ToString().Trim() : "");
                }

                //банк данных
                if (point.IsNullOrEmpty())
                {
                    sql = " SELECT point FROM " + Points.Pref + DBManager.sKernelAliasRest + "s_point " +
                          " WHERE bd_kernel = '" + Pref + "' ";
                    ExecRead(out reader, sql);

                    while (reader.Read())
                    {
                        point = (reader["point"] != DBNull.Value ? reader["point"].ToString().Trim() : "");
                    }
                    if (point.IsNullOrEmpty()) point = "";
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка GetNameRc(string pref):\n" + ex, MonitorLog.typelog.Error, true);
                ret = new Returns(false, "Ошибка при определении наименования расчетного центра", -1);
            }
            return nameRc;
        }

        /// <summary>
        /// Заполнение временной таблицы t_kaprem
        /// </summary>
        /// <param name="ret"></param>
        private void SelectFromDb(out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                string sql;
                string myData = Pref + DBManager.sDataAliasRest;
                string myKernel = Pref + DBManager.sKernelAliasRest;

                ExecSQL(" TRUNCATE t_kaprem; ");

                

                #region Заполнение t_kaprem

                bool regOp = true;
                IDataReader reader;
                if (!TempTableInWebCashe(myKernel + "serv_kaprem"))
                {
                    //создание временных таблиц
                    CreateTempTables();
                    CreateTempTableServKaprem();

                    sql =
                        " INSERT INTO t_services VALUES (269); " +
                        " INSERT INTO t_services VALUES (251); " +
                        " INSERT INTO t_services VALUES (206); " +
                        " INSERT INTO t_services VALUES (267); " +
                        " INSERT INTO t_services VALUES (356); ";
                    ExecSQL(sql);

                    sql =
                        " INSERT INTO t_supplier VALUES ('региональный оператор'); " +
                        " INSERT INTO t_supplier VALUES ('но фонд жкх рт'); ";
                    ExecSQL(sql);

                    sql =
                        " DELETE FROM t_serv_kaprem ";
                    ExecSQL(sql);

                    sql =
                        " INSERT INTO t_serv_kaprem (nzp_serv, nzp_serv_k, nzp_supp, dat_s, dat_po) " +
                        " SELECT nzp_serv, 356, COALESCE(nzp_supp,0), '2014-06-01', '3000-01-01' " +
                        " FROM  t_services, " + myKernel + "supplier s, " +
                        " t_supplier ts WHERE ts.name_supp=lower(s.name_supp)";
                    ExecSQL(sql);

                    sql =
                        " INSERT INTO t_serv_kaprem " +
                        " SELECT nzp_serv, 269, 0, '2014-01-01', '2014-05-31' " +
                        " FROM  t_services; ";
                    ExecSQL(sql);

                    sql = " DELETE FROM t_services;";
                    ExecSQL(sql);

                    sql =
                        " INSERT INTO t_services VALUES (15); " +
                        " INSERT INTO t_services VALUES (268); " +
                        " INSERT INTO t_services VALUES (280); ";
                    ExecSQL(sql);

                    sql =
                        " INSERT INTO t_serv_kaprem " +
                        " SELECT nzp_serv, 15, 0, '2014-01-01', '3000-01-01' " +
                        " FROM  t_services; ";
                    ExecSQL(sql);

                    sql =
                        " SELECT COUNT(*) cnt FROM t_serv_kaprem " +
                        " WHERE nzp_serv_k = 356 " +
                        " AND dat_s <= " + DatPo + " " +
                        " AND dat_po >= " + DatS + " " +
                        " AND nzp_supp<>0 ";
                    ExecRead(out reader, sql);

                    if (reader.Read())
                    {
                        if (reader["cnt"] != DBNull.Value)
                        {
                            if (Convert.ToInt32(reader["cnt"]) <= 0)
                                regOp = false;

                        }
                    }


                    if (regOp)
                    {
                        sql =
                            " INSERT INTO t_kaprem (num_ls,nzp_serv,kap_tarif, sum_insaldo, sum_real, sum_charge, sum_money, sum_outsaldo,reval) " +
                            " SELECT  num_ls, sk.nzp_serv_k, MAX(tarif) tarif, SUM(sum_insaldo) sum_insaldo, SUM(sum_real) sum_real, " +
                            " SUM(sum_charge) sum_charge, SUM(sum_money) sum_money, SUM(sum_outsaldo) sum_outsaldo, SUM(reval + real_charge) reval " +
                            " FROM   " + MyCharge + " ch, " + " t_serv_kaprem sk " +
                            " WHERE dat_charge is null " +
                            " AND ch.nzp_serv = sk.nzp_serv " +
                            " AND sk.nzp_supp*ch.nzp_supp = sk.nzp_supp*sk.nzp_supp " +
                            " AND sk.dat_s <= " + DatPo + " " +
                            " AND sk.dat_po >= " + DatS + " " +
                            " AND (ABS(sum_insaldo) + ABS(sum_real + reval + real_charge) + ABS(sum_outsaldo) + ABS(sum_money) > 0.001) " +
                            " GROUP BY num_ls, sk.nzp_serv_k ";
                        ExecSQL(sql);
                    }
                }

                if (regOp)
                {
                    sql =
                        " INSERT INTO t_kaprem (num_ls,nzp_serv,kap_tarif, sum_insaldo, sum_real, sum_charge, sum_money, sum_outsaldo,reval) " +
                        " SELECT  num_ls, sk.nzp_serv_k, MAX(tarif) tarif, SUM(sum_insaldo) sum_insaldo, SUM(sum_real) sum_real, " +
                        " SUM(sum_charge) sum_charge, SUM(sum_money) sum_money, SUM(sum_outsaldo) sum_outsaldo, SUM(reval + real_charge) reval " +
                        " FROM   " + MyCharge + " ch, " + myKernel +"serv_kaprem sk " +
                        " WHERE dat_charge is null " +
                        " AND ch.nzp_serv = sk.nzp_serv " +
                        " AND sk.nzp_supp*ch.nzp_supp = sk.nzp_supp*sk.nzp_supp " +
                        " AND sk.dat_s <= " + DatPo + " " +
                        " AND sk.dat_po >= " + DatS + " " +
                        " AND (ABS(sum_insaldo) + ABS(sum_real + reval + real_charge) + ABS(sum_outsaldo) + ABS(sum_money) > 0.001) " +
                        " GROUP BY num_ls, sk.nzp_serv_k ";
                    ExecSQL(sql);
                }

                sql =
                    " SELECT COUNT(*) cnt FROM t_kaprem WHERE nzp_serv = 356 ";
                ExecRead(out reader, sql);
                if (reader.Read())
                {
                    if (reader["cnt"] != DBNull.Value)
                    {
                        if (Convert.ToInt32(reader["cnt"]) == 0)
                        {
                            if (!TempTableInWebCashe(myKernel + "serv_kaprem"))
                            {
                                sql =
                                    " DELETE FROM t_serv_kaprem; ";
                                ExecSQL(sql);

                                sql =
                                    " INSERT INTO t_serv_kaprem " +
                                    " SELECT nzp_serv, 15, 0, '2014-01-01', '3000-01-01' " +
                                    " FROM t_services; ";
                                ExecSQL(sql);

                                sql =
                                    " DELETE FROM t_services; ";
                                ExecSQL(sql);

                                sql =
                                    " INSERT INTO t_services VALUES (269); " +
                                    " INSERT INTO t_services VALUES (251); " +
                                    " INSERT INTO t_services VALUES (206); " +
                                    " INSERT INTO t_services VALUES (267); " +
                                    " INSERT INTO t_services VALUES (356); ";
                                ExecSQL(sql);

                                sql =
                                    " INSERT INTO t_serv_kaprem " +
                                    " SELECT nzp_serv, 269, 0, '2014-01-01', '3000-01-01' " +
                                    " FROM t_services; ";
                                ExecSQL(sql);

                                sql =
                                    " DELETE FROM t_kaprem ";
                                ExecSQL(sql);

                                sql =
                                    " INSERT INTO t_kaprem (num_ls, nzp_serv, kap_tarif, sum_insaldo, sum_real, sum_charge, sum_money, sum_outsaldo,reval) " +
                                    " SELECT num_ls, sk.nzp_serv_k, MAX(tarif) tarif, SUM(sum_insaldo) sum_insaldo, SUM(sum_real) sum_real," +
                                    " SUM(sum_charge) sum_charge, SUM(sum_money) sum_money, SUM(sum_outsaldo) sum_outsaldo, SUM(reval+real_charge) reval " +
                                    " FROM   " + MyCharge + " ch, " + "t_serv_kaprem sk " +
                                    " WHERE dat_charge is null " +
                                    " AND ch.nzp_serv = sk.nzp_serv" +
                                    " AND sk.nzp_supp*ch.nzp_supp = sk.nzp_supp*sk.nzp_supp " +
                                    " AND sk.dat_s <= " + DatPo + " " +
                                    " AND sk.dat_po >= " + DatS + " " +
                                    " AND (ABS(sum_insaldo) + ABS(sum_real + reval + real_charge) + ABS(sum_outsaldo) + ABS(sum_money) > 0.001)" +
                                    " GROUP BY num_ls, sk.nzp_serv_k ";
                                ExecSQL(sql);
                            }
                            else
                            {
                                sql =
                                   " INSERT INTO t_kaprem (num_ls, nzp_serv, kap_tarif, sum_insaldo, sum_real, sum_charge, sum_money, sum_outsaldo,reval) " +
                                   " SELECT num_ls, sk.nzp_serv_k, MAX(tarif) tarif, SUM(sum_insaldo) sum_insaldo, SUM(sum_real) sum_real," +
                                   " SUM(sum_charge) sum_charge, SUM(sum_money) sum_money, SUM(sum_outsaldo) sum_outsaldo, SUM(reval+real_charge) reval " +
                                   " FROM   " + MyCharge + " ch, " + myKernel + "serv_kaprem sk " +
                                   " WHERE dat_charge is null " +
                                   " AND ch.nzp_serv = sk.nzp_serv" +
                                   " AND sk.nzp_supp*ch.nzp_supp = sk.nzp_supp*sk.nzp_supp " +
                                   " AND sk.dat_s <= " + DatPo + " " +
                                   " AND sk.dat_po >= " + DatS + " " +
                                   " AND (ABS(sum_insaldo) + ABS(sum_real + reval + real_charge) + ABS(sum_outsaldo) + ABS(sum_money) > 0.001)" +
                                   " GROUP BY num_ls, sk.nzp_serv_k ";
                                ExecSQL(sql);
                            }
                        }
                    }
                }
                #endregion

                //обновление полей платежный код, номер квартиры, кол-во комнат, период,
                //нежилое помещение, код дома, код ЛС
                sql =
                    " UPDATE t_kaprem SET (pkod, nkvar, nkom, period, ne_gil, nzp_dom, nzp_kvar) = " +
                    " (" +
                    "  (" +
                    "   SELECT  kv.pkod " +
                    "   FROM " + myData + "kvar kv WHERE kv.num_ls = t_kaprem.num_ls " +
                    "  )," +
                    "  (" +
                    "   SELECT kv.nkvar " +
                    "   FROM " + myData + "kvar kv WHERE kv.num_ls = t_kaprem.num_ls " +
                    "  )," +
                    "  (" +
                    "   SELECT kv.nkvar_n as nkom " +
                    "   FROM " + myData + "kvar kv WHERE kv.num_ls = t_kaprem.num_ls " +
                    "  ), " +
                    "  (" +
                    "   SELECT CAST ('" + _year + "-" + _month + "-01'as date) as period " +
                    "   FROM " + myData + "kvar kv WHERE kv.num_ls = t_kaprem.num_ls " +
                    "  ), " +
                    "  ( " +
                    "   SELECT CASE WHEN kv.typek = 1 THEN 'false' ELSE 'true' END as ne_gil " +
                    "   FROM " + myData + "kvar kv WHERE kv.num_ls = t_kaprem.num_ls " +
                    "  ), " +
                    "  (" +
                    "   SELECT kv.nzp_dom " +
                    "   FROM " + myData + "kvar kv WHERE kv.num_ls = t_kaprem.num_ls " +
                    "  ), " +
                    "  (" +
                    "   SELECT kv.nzp_kvar " +
                    "   FROM " + myData + "kvar kv WHERE kv.num_ls = t_kaprem.num_ls " +
                    "  )" +
                    " )" +
                    " WHERE EXISTS " +
                    " (" +
                    "  SELECT 1 " +
                    "  FROM " + myData + "kvar kv" +
                    "  WHERE kv.num_ls = t_kaprem.num_ls " +
                    " );";
                ExecSQL(sql);

                //уникальный код файла
                sql =
                    " UPDATE t_kaprem SET id_file = " + DateTime.Now.ToString("HHmmssff");
                ExecSQL(sql);

                //обновление поля Идентификатор дома в ИШ
                sql =
                    " UPDATE t_kaprem " +
                    " SET id_ish = " +
                    " (SELECT MAX(val_prm)" +
                    " FROM " + myData + "prm_4 p" +
                    " WHERE " +
                    " is_actual = 1 " +
                    " AND dat_s <= " + DatPo +
                    " AND dat_po >= " + DatS +
                    " AND nzp_prm = 866 " +
                    " AND p.nzp = t_kaprem.nzp_dom)" +
                    " WHERE EXISTS (" +
                    " SELECT 1 " +
                    " FROM " + myData + "prm_4 p " +
                    " WHERE p.nzp = t_kaprem.nzp_dom);";
                ExecSQL(sql);

                //обновление поля Идентификатор дома в БИЛЛ
                sql =
                    " UPDATE t_kaprem " +
                    " SET id_bill = " +
                    " (SELECT MAX(val_prm)" +
                    " FROM " + myData + "prm_4 p" +
                    " WHERE " +
                    " is_actual = 1 " +
                    " AND dat_s <= " + DatPo +
                    " AND dat_po >= " + DatS +
                    " AND nzp_prm = 890 " +
                    " AND p.nzp = t_kaprem.nzp_dom)" +
                    " WHERE EXISTS (" +
                    " SELECT 1 " +
                    " FROM " + myData + "prm_4 p " +
                    " WHERE p.nzp = t_kaprem.nzp_dom);";
                ExecSQL(sql);

                //обновление поля общая площадь квартиры
                sql =
                    " UPDATE t_kaprem " +
                    " SET ob_pl_kv = " +
                    " (SELECT MAX(val_prm)" +
                    " FROM " + myData + "prm_1 p" +
                    " WHERE " +
                    " is_actual = 1 " +
                    " AND dat_s <= " + DatPo +
                    " AND dat_po >= " + DatS +
                    " AND nzp_prm = 4 " +
                    " AND p.nzp = t_kaprem.nzp_kvar)" +
                    " WHERE EXISTS (" +
                    " SELECT  1 " +
                    " FROM " + myData + "prm_1 p " +
                    " WHERE p.nzp = t_kaprem.nzp_kvar);";
                ExecSQL(sql);

                //обновление поля жилая площадь квартиры
                sql =
                    " UPDATE t_kaprem " +
                    " SET gil_pl_kv = " +
                    " (SELECT MAX(val_prm)" +
                    " FROM " + myData + "prm_1 p" +
                    " WHERE " +
                    " is_actual = 1 " +
                    " AND dat_s <= " + DatPo +
                    " AND dat_po >= " + DatS +
                    " AND nzp_prm = 6 " +
                    " AND p.nzp = t_kaprem.nzp_kvar)" +
                    " WHERE EXISTS (" +
                    " SELECT 1 " +
                    " FROM " + myData + "prm_1 p " +
                    " WHERE p.nzp = t_kaprem.nzp_kvar);";

                ExecSQL(sql);

                //обновление поля количество комнат
                sql =
                    " UPDATE t_kaprem " +
                    " SET kol_komn = " +
                    " (SELECT MAX(val_prm)" +
                    " FROM " + myData + "prm_1 p" +
                    " WHERE " +
                    " is_actual = 1 " +
                    " AND dat_s <= " + DatPo +
                    " AND dat_po >= " + DatS +
                    " AND nzp_prm = 107 " +
                    " AND p.nzp = t_kaprem.nzp_kvar)" +
                    " WHERE EXISTS (" +
                    " SELECT 1 " +
                    " FROM " + myData + "prm_1 p " +
                    " WHERE p.nzp = t_kaprem.nzp_kvar);";
                ExecSQL(sql);

                //обновление поля приватизировано
                sql =
                    " UPDATE t_kaprem " +
                    " SET privat = " +
                    " (SELECT case when max(val_prm)= '1' then 'true' else 'false' end " +
                    " FROM " + myData + "prm_1 p" +
                    " WHERE " +
                    " is_actual = 1 " +
                    " AND dat_s <= " + DatPo +
                    " AND dat_po >= " + DatS +
                    " AND nzp_prm = 8 " +
                    " AND p.nzp = t_kaprem.nzp_kvar)" +
                    " WHERE EXISTS (" +
                    " SELECT 1 " +
                    " FROM " + myData + "prm_1 p " +
                    " WHERE p.nzp = t_kaprem.nzp_kvar);";
                ExecSQL(sql);

                //обновление полей состояние лицевого счета, дата открытия, дата закрытия
                sql =
                    " UPDATE t_kaprem " +
                    " SET (ls_state, ls_dat_s, ls_dat_po) = " +
                    " (" +
                    " (SELECT CASE WHEN (MAX(p.val_prm)) = '1' THEN 'открыт' " +
                    "              WHEN (MAX(p.val_prm)) = '2' THEN 'закрыт' " +
                    "              WHEN (MAX(p.val_prm)) = '1' THEN 'неопределено' " +
                    " END as ls_state " +
                    " FROM " + myData + "prm_3 p" +
                    " WHERE is_actual = 1 " +
                    " AND dat_s <= " + DatPo +
                    " AND dat_po >= " + DatS +
                    " AND nzp_prm = 51 " +
                    " AND p.nzp = t_kaprem.nzp_kvar)," +
                    " (SELECT MAX(p.dat_s) as ls_dat_s " +
                    " FROM " + myData + "prm_3 p" +
                    " WHERE is_actual = 1 " +
                    " AND dat_s <= " + DatPo +
                    " AND dat_po >= " + DatS +
                    " AND nzp_prm = 51 " +
                    " AND p.nzp = t_kaprem.nzp_kvar)," +
                    " (SELECT MAX(p.dat_po) as ls_dat_po " +
                    " FROM " + myData + "prm_3 p" +
                    " WHERE is_actual = 1 " +
                    " AND dat_s <= " + DatPo +
                    " AND dat_po >= " + DatS +
                    " AND nzp_prm = 51 " +
                    " AND p.nzp = t_kaprem.nzp_kvar)" +
                    " )" +
                    " WHERE EXISTS (" +
                    " SELECT 1 " +
                    " FROM " + myData + "prm_3 p " +
                    " WHERE p.nzp = t_kaprem.nzp_kvar" +
                    " ) ";
                ExecSQL(sql);

                //обновление полей район, улица, номер дома, корпус
                sql =
                    " UPDATE t_kaprem " +
                    " SET (graj, town, ulica, ndom, nkor) = " +
                    " ('" + point + "', " +
                    " (SELECT " +
                    " CASE WHEN r.rajon  = '-' THEN t.town ELSE r.rajon END as town " +
                    " FROM " + myData + "s_town t, " + myData + "s_rajon r, " + myData + "dom d, " + myData + "s_ulica u " +
                    " WHERE t.nzp_town = r.nzp_town " +
                    " AND r.nzp_raj = u.nzp_raj" +
                    " AND d.nzp_ul = u.nzp_ul " +
                    " AND t_kaprem.nzp_dom = d.nzp_dom" +
                    " ), " +
                    " (SELECT " +
                    " u.ulica " +
                    " FROM " + myData + "s_town t, " + myData + "s_rajon r, " + myData + "dom d, " + myData + "s_ulica u " +
                    " WHERE t.nzp_town = r.nzp_town " +
                    " AND r.nzp_raj = u.nzp_raj" +
                    " AND d.nzp_ul = u.nzp_ul " +
                    " AND t_kaprem.nzp_dom = d.nzp_dom" +
                    " ), " +
                    " (SELECT " +
                    " ndom " +
                    " FROM " + myData + "s_town t, " + myData + "s_rajon r, " + myData + "dom d, " + myData + "s_ulica u " +
                    " WHERE t.nzp_town = r.nzp_town " +
                    " AND r.nzp_raj = u.nzp_raj" +
                    " AND d.nzp_ul = u.nzp_ul " +
                    " AND t_kaprem.nzp_dom = d.nzp_dom" +
                    " ), " +
                    " (SELECT " +
                    " nkor " +
                    " FROM " + myData + "s_town t, " + myData + "s_rajon r, " + myData + "dom d, " + myData + "s_ulica u " +
                    " WHERE t.nzp_town = r.nzp_town " +
                    " AND r.nzp_raj = u.nzp_raj" +
                    " AND d.nzp_ul = u.nzp_ul " +
                    " AND t_kaprem.nzp_dom = d.nzp_dom" +
                    " )" +
                    " )" +
                    " WHERE EXISTS (" +
                    " SELECT 1 " +
                    " FROM " + myData + "s_town t, " + myData + "s_rajon r, " + myData + "dom d, " + myData + "s_ulica u " +
                    " WHERE t.nzp_town = r.nzp_town " +
                    " AND r.nzp_raj = u.nzp_raj" +
                    " AND d.nzp_ul = u.nzp_ul " +
                    " AND t_kaprem.nzp_dom = d.nzp_dom " +
                    ")";
                ExecSQL(sql);
                
                //для отчета
                sql =
                    " INSERT INTO t_report_p (period, nzp_dom, graj, town, ulica, ndom, nkor, ne_gil, nzp_serv, sum_real," +
                    " sum_money, ob_pl_kv) " +
                    " SELECT period, nzp_dom, graj, town, ulica, ndom, nkor, ne_gil, nzp_serv, sum_real, sum_money," +
                    " CASE WHEN kap_tarif <> 0 THEN ob_pl_kv ELSE '0' END AS ob_pl_kv FROM t_kaprem ";
                ExecSQL(sql);

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("SelectFromDb(out Returns ret, string pref):\n" + ex, MonitorLog.typelog.Error, true);
                ret = new Returns(false, "Ошибка выборки данных", -1);
            }
            finally
            {
                DropTempTables();
                DropTempTableServKaprem();
            }
        }

        /// <summary>
        /// Формирование отчета выгрузки
        /// </summary>
        /// <param name="ret"></param>
        private void GetReport(out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                //данные для отчета
                string sql =
                    " INSERT INTO t_report (period, nzp_dom, graj, town, ulica, ndom, nkor, ne_gil, nzp_serv, sum_real, " +
                    " sum_money, ob_pl_kv) " +
                    " select period, nzp_dom, graj, town, ulica, ndom, nkor, ne_gil, nzp_serv, sum(sum_real) sum_real, " +
                    " sum(sum_money) sum_money, sum(ob_pl_kv" + DBManager.sConvToNum + " + 0) ob_pl FROM t_report_p " +
                    " GROUP BY 1,2,3,4,5,6,7,8,9 ";
                ExecSQL(sql);

                var blank = ExecSQLToTable(" select * from t_report");
                blank.TableName = "Q_master";

                if (blank.Rows.Count > 0)
                {
                    var dsRep = new DataSet();
                    dsRep.Tables.Add(blank);

                    //имя файла отчета
                    string nameFileRep = Path.Combine(Constants.Directories.ReportDir, "KapRem_" + new DateTime(_year, _month, 1).ToString("yyyyMMdd") + 
                        "_" + new DateTime(_year, _month, DateTime.DaysInMonth(_year, _month)).ToString("yyyyMMdd") + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmssFFFF") + ".frx");

                    //формирование отчета
                    var rep = new FastReport.Report();
                    rep.Load(Directory.GetCurrentDirectory() + @"\Template\Kaprem.frx");
                    rep.RegisterData(dsRep);
                    rep.SetParameterValue("dat_s", new DateTime(_year, _month, 1).ToString("dd.MM.yyyy"));
                    rep.SetParameterValue("dat_po",
                        new DateTime(_year, _month, DateTime.DaysInMonth(_year, _month)).ToString("dd.MM.yyyy"));
                    rep.SetParameterValue("cur_date", DateTime.Now.ToString("yyyy.MM.dd"));
                    rep.SetParameterValue("cur_time", DateTime.Now.ToString("HH:mm"));
                    var env = new EnvironmentSettings();
                    env.ReportSettings.ShowProgress = false;
                    rep.Prepare();
                    rep.SavePrepared(nameFileRep);

                    if (File.Exists(nameFileRep))
                    {
                        ListFileName.Add(nameFileRep);
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("GetReport(out Returns ret): Ошибка при формировании отчета\n" + ex, MonitorLog.typelog.Error, true);
                ret = new Returns(false, "Ошибка при формировании отчета", -1);
            }
        }

        /// <summary>
        /// Сформировать файл выгрузки
        /// </summary>
        /// <param name="ret"></param>
        private void GetFileUnload(out Returns ret)
        {
            ret = Utils.InitReturns();

            try
            {
                //сформировать имя файла
                GetFilename(out ret);

                if (ret.result)
                {
                    //заполнение t_kaprem
                    SelectFromDb(out ret);
                    if (ret.result)
                    {

                        CommentList.Add("Банк данных - " + point +
                                        ": Начало выгрузки по услугам \"Капитальный ремонт\" и \"Наем\" за " +
                                        Months[_month] + " месяц " + _year + " года;");
                        var dt =
                            ExecSQLToTable(
                                " SELECT id_bill, id_ish, num_ls, nkvar, ob_pl_kv, gil_pl_kv, kol_komn, ls_state, ne_gil, period, pkod, " +
                                " SUM(sum_insaldo) sum_insaldo, " +
                                " SUM(sum_outsaldo) sum_outsaldo, SUM(sum_real) sum_real, SUM(sum_charge) sum_charge, SUM(sum_money) sum_money, " +
                                " SUM(reval) reval, MAX(kap_tarif) kap_tarif, ls_dat_s, ls_dat_po, nkom, nzp_serv, privat, id_file, " +
                                " graj, town, ulica, ndom, nkor " +
                                " FROM t_kaprem " +
                                " WHERE COALESCE(nzp_kvar,0) <>0 " +
                                " GROUP BY 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29; ");



                        if (dt.Rows.Count > 0)
                        {

                            var memstr = new FileStream(Path.Combine(Constants.Directories.ReportDir, FileName),
                                FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                            var writer = new StreamWriter(memstr, Encoding.GetEncoding(1251));

                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                writer.WriteLine(CreateString(dt.Rows[i]));
                            }
                            writer.Flush();
                            writer.Close();
                            memstr.Close();
                        }
                        else
                        {
                            CommentList.Add("Банк данных - " + point +
                                            ": Нет начислений по услугам \"Капитальный ремонт\" и \"Наем\" за " +
                                            Months[_month] + " месяц " + _year + " года;");
                        }

                        if (File.Exists(Path.Combine(Constants.Directories.ReportDir, FileName)))
                        {
                            ListFileName.Add(Path.Combine(Constants.Directories.ReportDir, FileName));
                        }

                        CommentList.Add("Банк данных - " + point +
                                        ": Конец выгрузки по услугам \"Капитальный ремонт\" и \"Наем\" за " +
                                        Months[_month] + " месяц " + _year + " года;\r\n");
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("GetFileUnload(out Returns ret): Ошибка при формировании файла выгрузки\n" + ex, MonitorLog.typelog.Error, true);
                ret = new Returns(false, "Ошибка при формировании файла выгрузки", -1);
            }
        }


        /// <summary>
        /// Сформировать протокол выгрузки
        /// </summary>
        /// <param name="ret"></param>
        private void GetProtokol(out Returns ret)
        {
            ret = Utils.InitReturns();

            try
            {
                if (CommentList.Count > 0)
                {
                    var dat_s = new DateTime(_year, _month, 1);
                    var dat_po = new DateTime(_year, _month, DateTime.DaysInMonth(_year, _month));
                    var curDate = DateTime.Now;
                    string fileNameProt = "protocol_" + dat_s.ToString("yyyyMMdd") + "_" + dat_po.ToString("yyyyMMdd") + "_" +
                               curDate.ToString("yyyyMMdd_HHmmssffff") + ".txt";
                    var memstr = new FileStream(Path.Combine(Constants.Directories.ReportDir, fileNameProt),
                        FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                    var writer = new StreamWriter(memstr, Encoding.GetEncoding(1251));

                    foreach (var com in CommentList)
                    {
                        writer.WriteLine(com);
                    }
                    writer.Flush();
                    writer.Close();
                    memstr.Close();

                    if (File.Exists(Path.Combine(Constants.Directories.ReportDir, fileNameProt)))
                    {
                        ListFileName.Add(Path.Combine(Constants.Directories.ReportDir, fileNameProt));
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("GetProtokol(out Returns ret): Ошибка при формировании протокола выгрузки" + ex, MonitorLog.typelog.Error, true);
                ret = new Returns(false, "Ошибка при формировании протокола выгрузки");
            }
        }

        /// <summary>
        /// Создание временных таблиц t_services и t_supplier
        /// </summary>
        private void CreateTempTables()
        {
            string tableName = "t_services";
            string columnsName =
                " nzp_serv INTEGER ";

            ExecSQL(String.Format(" DROP TABLE {0}", tableName), false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1})", tableName, columnsName));

            tableName = "t_supplier";
            columnsName =
                " name_supp CHAR(30) ";

            ExecSQL(String.Format(" DROP TABLE {0}", tableName), false);

            ExecSQL(String.Format(" CREATE TEMP TABLE {0} ({1})", tableName, columnsName));
        }

        /// <summary>
        /// Удаление временных таблиц t_services и t_supplier
        /// </summary>
        private void DropTempTables()
        {
            ExecSQL(" DROP TABLE t_services", false);
            ExecSQL(" DROP TABLE t_supplier", false);
        }
    }
}
