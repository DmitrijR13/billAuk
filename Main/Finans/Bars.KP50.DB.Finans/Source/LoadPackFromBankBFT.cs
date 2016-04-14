using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using FastReport;
using Globals.SOURCE.Utility;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;
using Constants = STCLINE.KP50.Global.Constants;
using Excel = Microsoft.Office.Interop.Excel; 

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// Загрузка реестра из банка
    /// </summary>
    abstract public class DbBasePaymentsFromBankBft : DataBaseHead
    {

        /// <summary>
        /// Протокол загрузки файла
        /// </summary>
        private DbLoadProtokol _loadProtokol;

        public string NotExistBranch = "";

        protected FileNameStruct _fileArgs;


        /// <summary>
        /// Фоновая загрузка реестра оплат
        /// </summary>
        /// <param name="finder">Описатель файла</param>
        /// <returns></returns>
        public Returns UploadReestrInFon(FilesImported finder)
        {


            Returns ret = Utils.InitReturns();
            try
            {
                //загрузка в фоне
                ret = UploadReestr(finder);
                if (!ret.result)
                {
                    _loadProtokol.AddComment(ret.text.Contains("\n") ? ret.text : "");
                }


                if (_fileArgs.FileType == FileNameStruct.ReestrTypes.Svod)
                    ret.tag = _loadProtokol.GetProtocolWwb(finder, ret.result).tag;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка добавления итогового реестра " + ex + " " +
                                    finder, MonitorLog.typelog.Error, 20, 201, true);
            }
            if (ret.result)
            {
                if (ret.tag == 2)
                {
                    _loadProtokol.SetProcent(100, (int)StatusWWB.WithErrors);
                }
                else
                {
                    _loadProtokol.SetProcent(100, (int)StatusWWB.Success);
                }
            }
            else
            {
                //если файл не прошел проверку на формат
                if (_loadProtokol.UncorrectRows.Rows.Count > 0)
                {
                    _loadProtokol.SetProcent(-1, (int)StatusWWB.FormatErr);
                }
                else
                {
                    _loadProtokol.SetProcent(-1, (int)StatusWWB.Fail);
                }
            }

            return ret;
        }


        /// <summary>
        /// Проверка файла перед загрузкой
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        abstract public FilesImported FastCheck(FilesImported finder, out Returns ret);


        /// <summary>
        /// Получение уникального кода квитанции
        /// </summary>
        /// <param name="connDB"></param>
        /// <param name="fileName">Имя файла</param>
        /// <returns></returns>
        private bool GetKvitId(IDbConnection connDB, string fileName)
        {
            bool result = true;

            Returns ret;
            string sqlStr = "select max(nzp_kvit_reestr) from " + Points.Pref + sDataAliasRest + "tula_kvit_reestr " +
                     "where file_name='" + fileName + "' ";
#if PG
            sqlStr += "AND extract(year from date_plat)=extract(year from now())";
#else
            sqlStr += "AND  YEAR(date_plat)= YEAR(TODAY)";
#endif
            object obj = ExecScalar(connDB, sqlStr, out ret, true);
            if (obj != null && obj != DBNull.Value)
            {
                _fileArgs.KvitID = Convert.ToInt32(obj);
                if (_fileArgs.KvitID == 0)
                {
                    MonitorLog.WriteLog("obj=null", MonitorLog.typelog.Info, 20, 201, true);
                    result = false;
                    _loadProtokol.AddComment("\nНе найдена соответствующая квитанция");
                }
            }
            else
            {
                MonitorLog.WriteLog("Не найдена соответствующая квитанция", MonitorLog.typelog.Error, 20, 201, true);
            }
            return result;
        }

        /// <summary>
        /// Сохранить файл
        /// </summary>
        /// <param name="finder"></param>
        /// <returns>Returns</returns>
        abstract protected Returns SaveKvitReestr(IDbConnection connDb, FilesImported finder);

        /// <summary>
        /// Загрузка реестра оплат
        /// </summary>
        /// <param name="finder">Описатель реестра</param>
        public Returns UploadReestr(FilesImported finder)
        {

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                return ret;
            }

            Utils.setCulture(); // установка региональных настроек
            _fileArgs = getFileName(finder.saved_name);

            _loadProtokol = new DbLoadProtokol(connDb, _fileArgs);

            ret = SaveKvitReestr(connDb, finder);
            if (!ret.result)
            {
                ret.text = "\nОшибка при сохранении квитанции";
                return ret;
            }


            if (_fileArgs.FileType != FileNameStruct.ReestrTypes.SvodKvit)
                if (!GetKvitId(connDb, finder.saved_name))
                {
                    ret.result = false;
                    ret.text = "\nНе найдена соответствующая квитанция";
                    return ret;
                }


            //получаем массив строк из файла
            string[] reestrStrings = ReadReestrFile(finder.ex_path);

            //удаляем промежуточный файл на хосте
            if (InputOutput.useFtp) File.Delete(finder.ex_path);

            _fileArgs.RowsCount = reestrStrings.Length;
            finder.count_rows = _fileArgs.RowsCount;
            string backTransaction = string.Empty;
            try
            {
                //запись в реестр загрузок
                InsertIntoTulaDownloaded(connDb, finder.saved_name, _fileArgs, finder);

                _loadProtokol.SetProcent(0, (int)StatusWWB.InProcess);

                if (_fileArgs.FileType == FileNameStruct.ReestrTypes.SvodKvit)
                {
                    var reestKvit = new DbReestKvit(connDb);
                    ret = reestKvit.Parse(reestrStrings, _fileArgs);
                }
                else if (_fileArgs.FileType == FileNameStruct.ReestrTypes.Svod)
                {
                    DbBaseReestrFromBankBft reestr = null;

                    //Разбор реестра
                    if (finder.upload_format == (int)FilesImported.UploadFormat.BaikalskVstkb)
                    {
                        reestr = new DbReestrFromBankBaikalVstkb(connDb, _fileArgs, _loadProtokol);
                        // получаем дату формирования реестра
                        _fileArgs.Day = Convert.ToInt32(reestrStrings[8].Split(' ')[1].Split('/')[0]);
                        _fileArgs.Month = Convert.ToInt32(reestrStrings[8].Split(' ')[1].Split('/')[1]);
                        _fileArgs.Year = Convert.ToInt32(reestrStrings[8].Split(' ')[1].Split('/')[2]);
                        //убираем хэдер файла (лишние прочитанные строки)
                        reestrStrings = reestrStrings.Where(x => x.Trim()[0] != '#').ToArray();
                    } else
                    if (finder.upload_format == (int)FilesImported.UploadFormat.BaikalskSber)
                    {
                        reestr = new DbReestrFromBankBaikalSber(connDb, _fileArgs, _loadProtokol);
                        // получаем дату формирования реестра
                        _fileArgs.Day = Convert.ToInt32(reestrStrings[8].Split(' ')[1].Split('/')[0]);
                        _fileArgs.Month = Convert.ToInt32(reestrStrings[8].Split(' ')[1].Split('/')[1]);
                        _fileArgs.Year = Convert.ToInt32(reestrStrings[8].Split(' ')[1].Split('/')[2]);
                        //убираем хэдер файла (лишние прочитанные строки)
                        reestrStrings = reestrStrings.Where(x => x.Trim()[0] != '#').ToArray();
                    } else
                    if (finder.upload_format == (int)FilesImported.UploadFormat.BaikalskSocProtectS)
                    {
                        reestr = new DbReestrFromBankBaikalSocProtectS(connDb, _fileArgs, _loadProtokol);
                    } else
                    if (finder.upload_format == (int)FilesImported.UploadFormat.BaikalskSocProtectL)
                    {
                        reestr = new DbReestrFromBankBaikalSocProtectL(connDb, _fileArgs, _loadProtokol);
                    } else
                    if (finder.upload_format == (int)FilesImported.UploadFormat.TagilSber)
                    {
                        reestr = new DbReestrFromBankTagilSber(connDb, _fileArgs, _loadProtokol);
                    } else
                    if (finder.upload_format == (int)FilesImported.UploadFormat.IssrpF112)
                    {
                        reestr = new DbReestrFromBankIssrpF112(connDb, _fileArgs, _loadProtokol);
                    } else
                    {
                        //reestr = new DbBaseReestrFromBankBft(connDB, _fileArgs, _loadProtokol);
                    }
                    if (reestr != null)
                    {
                        ret = reestr.ParseReestr(finder, reestrStrings);
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog(ret.text + " ", MonitorLog.typelog.Error, 20, 201, true);
                        }
                        if (!reestr.CheckAllPayments())
                        {
                            ret.text = "\nОшибка в платежах!";
                            ret.result = false;
                            MonitorLog.WriteLog(ret.text + " ", MonitorLog.typelog.Error, 20, 201, true);
                        }

                        backTransaction = reestr.BackTransaction;
                    }
                }
                else
                {
                    ret.text = "\nНеверный формат файла. Количество полей в файле не совпадает с форматом.";
                    ret.result = false;
                    MonitorLog.WriteLog("Неверное количество полей в файле ", MonitorLog.typelog.Error, 20, 201, true);
                }


                if (!ret.result) return ret;

                if (_fileArgs.FileType == FileNameStruct.ReestrTypes.Period || _fileArgs.FileType == FileNameStruct.ReestrTypes.Svod)
                {
                    DbSaveReestrBft saveReestr = null;

                    if (finder.upload_format == (int)FilesImported.UploadFormat.BaikalskVstkb)
                    {
                        saveReestr = new DbSaveReestrBaikalVstkb(connDb, _fileArgs, _loadProtokol, finder.nzp_bank);
                    } else
                    if (finder.upload_format == (int)FilesImported.UploadFormat.BaikalskSber)
                    {
                        saveReestr = new DbSaveReestrBaikalVstkb(connDb, _fileArgs, _loadProtokol, finder.nzp_bank);
                    } else
                    if (finder.upload_format == (int)FilesImported.UploadFormat.BaikalskSocProtectL)
                    {
                        saveReestr = new DbSaveReestrBaikalSocProtectL(connDb, _fileArgs, _loadProtokol, finder.nzp_bank);
                    } else
                    if (finder.upload_format == (int)FilesImported.UploadFormat.BaikalskSocProtectS)
                    {
                        saveReestr = new DbSaveReestrBaikalSocProtectS(connDb, _fileArgs, _loadProtokol, finder.nzp_bank);
                    } else
                    if (finder.upload_format == (int)FilesImported.UploadFormat.TagilSber)
                    {
                        saveReestr = new DbSaveReestrTagilSber(connDb, _fileArgs, _loadProtokol, finder.nzp_bank);
                    } else
                    if (finder.upload_format == (int)FilesImported.UploadFormat.IssrpF112)
                    {
                        saveReestr = new DbSaveReestrIssrpF112(connDb, _fileArgs, _loadProtokol, finder.nzp_bank);
                    } else
                    {
                        //saveReestr = new DbTulaSaveReestr(connDB, _fileArgs, _loadProtokol, finder.nzp_bank);
                    }

                    //записываем данные в систему: pack,pack_ls,pu_vals 
                    if (saveReestr != null && !saveReestr.SyncLsAndInsertPack(finder).result)
                    {
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка записи пачки оплат в систему",
                            MonitorLog.typelog.Error, 20, 201, true);
                        ret.text = "\nОшибка при записи данных в систему";
                    }

                }
                if (ret.result)
                {
                    _loadProtokol.SetProcent(100, (int)StatusWWB.Success);

                    if (_fileArgs.FileType == FileNameStruct.ReestrTypes.Period || _fileArgs.FileType == FileNameStruct.ReestrTypes.Svod)
                        PackDist(_loadProtokol.NzpPack, finder); //распределение пачек                
                }

            }
            catch (UserException ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                _loadProtokol.AddComment(ex.Message);
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                RollbackReestrTula(connDb, finder, _fileArgs.NzpDownload);
            }

            connDb.Close();

            //предупреждения
            if (backTransaction != "")
            {
                ret.text += " " + backTransaction;
            }
            if (NotExistBranch != "")
            {
                ret.text += " " + NotExistBranch;
            }

            return ret;
        }


        /// <summary>
        /// Прочитать строки файла оплат
        /// </summary>
        /// <param name="path">Путь к файлу</param>
        /// <returns></returns>
        public virtual string[] ReadReestrFile(string path)
        {
            var fstream = new FileStream(path, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[fstream.Length];
            fstream.Position = 0;
            fstream.Read(buffer, 0, buffer.Length);
            fstream.Close();

            string reestrFileString = Encoding.GetEncoding(1251).GetString(buffer);
            string[] stSplit = { Environment.NewLine };
            string[] reestrStrings = reestrFileString.Split(stSplit, StringSplitOptions.RemoveEmptyEntries);
            return reestrStrings;
        }

        /// <summary>
        /// Проверка реестра на существование
        /// </summary>
        /// <param name="connDB"></param>
        /// <param name="fileName"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        protected bool CheckReestrName(IDbConnection connDB, string fileName, FileNameStruct file)
        {
            bool result;

            string sqlStr = "select * from " + Points.Pref + "_data" + tableDelimiter + "tula_kvit_reestr where file_name='" + fileName + "' ";

            if (file.FileType == FileNameStruct.ReestrTypes.Svod)
            {
                sqlStr += " and is_itog=1";
            }
            var dt = ClassDBUtils.OpenSQL(sqlStr, connDB);
            if (dt.resultData.Rows.Count > 0)
            {
                result = true;
            }
            else result = false;
            return result;
        }

        /// <summary>
        /// Определение банка откуда пришли оплаты
        /// </summary>
        /// <param name="connDB"></param>
        /// <param name="branchID"></param>
        /// <returns></returns>
        private int GetNzpBankByBranchId(IDbConnection connDB, FileNameStruct fileArgs)
        {
            Returns ret;
            int nzpBank = 0;
            string branch_name = fileArgs.FileType == FileNameStruct.ReestrTypes.SvodKvit ? "branch_id" : "branch_id_reestr";
            string sqlString = " SELECT nzp_bank FROM " + Points.Pref + DBManager.sDataAliasRest +
                               "tula_s_bank where is_actual<>100 " +
                               " and " + branch_name + "=" + Utils.EStrNull(fileArgs.OtdNumber.ToUpper());
            nzpBank = CastValue<int>(ExecScalar(connDB, sqlString, out ret, true));
            return nzpBank;
        }

        /// <summary>
        /// Сохранение записи о реестре в Базу данных
        /// </summary>
        /// <param name="connDB">Подключение</param>
        /// <param name="fileName">Имя Файла</param>
        /// <param name="file">Описатель</param>
        /// <param name="finder"></param>
        private void InsertIntoTulaDownloaded(IDbConnection connDB, string fileName, FileNameStruct file, FilesImported finder)
        {

            //var db = new DbWorkUser();
            finder.pref = Points.Pref;
            Returns ret;
            /*int nzpUser = db.GetLocalUser(connDB, finder, out ret);
            db.Close();*/

            int nzpUser = finder.nzp_user;
            finder.nzp_user_main = nzpUser;

            int nzpBank = 0;

            if (finder.nzp_bank > 0)
            {
                nzpBank = finder.nzp_bank;
            }
            else
            {
                nzpBank = GetNzpBankByBranchId(connDB, file);
            }

            if (nzpBank == 0)
            {
                ret.result = false;
                ret.text = "\r\n Не определен банк";
                return;
            }

            var dateD = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss").Replace('.', '-');

            string sqlStr = "INSERT INTO " + Points.Pref + sDataAliasRest + "tula_reestr_downloads " +
                            "(file_name,nzp_type,date_download,user_downloaded,day,month, branch_id,nzp_bank) " +
                            "VALUES " +
                            " ( '" + fileName + "'," + (int)file.FileType + ",'" + dateD + "'," + nzpUser + "," +
                            file.Day + "," + file.Month + ",'" + file.OtdNumber + "'," + nzpBank + " )";

            ClassDBUtils.ExecSQL(sqlStr, connDB);

            file.NzpDownload = GetSerialValue(connDB);

        }

        /// <summary>
        /// Проверка на повторную загрузку файла
        /// </summary>
        /// <param name="connDB"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected bool ExistFile(IDbConnection connDB, string fileName)
        {
            bool ret = true;
            int num;
            Returns retur;

            string sqlString = " SELECT count(*) FROM " + Points.Pref + sDataAliasRest + "tula_reestr_downloads " +
                               " where file_name='" + fileName + "'";
#if PG
            sqlString += "AND extract(year from date_download)=extract(year from now())";
#else
            sqlString += "AND  YEAR(date_download)= YEAR(TODAY)";
#endif

            object obj = ExecScalar(connDB, sqlString, out retur, true);
            num = Convert.ToInt32(obj);
            if (num > 0)
            {
                ret = false;
            }
            return ret;
        }

        /// <summary>
        /// Заполнение файловой структуры
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <param name="branch_id">Код отделения для Марий Эл</param>
        /// <returns></returns>
        abstract public FileNameStruct getFileName(string fileName);


        /// <summary>
        /// Определение платежного агента
        /// </summary>
        /// <param name="connDB"></param>
        /// <param name="fileArgs"></param>
        /// <returns></returns>
        protected Returns CheckBank(IDbConnection connDB, FileNameStruct fileArgs)
        {
            Returns ret;
            var branch_name = "";
            branch_name = fileArgs.FileType == FileNameStruct.ReestrTypes.SvodKvit ? "branch_id" : "branch_id_reestr";

            string sqlString = "select nzp_bank from " + Points.Pref + DBManager.sDataAliasRest + "tula_s_bank " +
                               "where is_actual<>100 and  " + branch_name + "=" + Utils.EStrNull(fileArgs.OtdNumber.ToUpper());
            string otd = CastValue<string>(ExecScalar(connDB, sqlString, out ret, true));
            if (!ret.result) return ret;
            if (otd.Length == 0)
            {
                ret.tag = 999;
                ret.result = false;
                NotExistBranch = "\r\nПлатежный агент не определен. Добавьте платежного агента в справочнике платежных агентов";
            }
            return ret;
        }


        /// <summary>
        /// Распределение пачек
        /// </summary>
        /// <param name="nzpPack"></param>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns PackDist(List<int> nzpPack, FilesImported finder)
        {
            if (finder == null) throw new ArgumentNullException("finder");
            Returns ret = Utils.InitReturns();
            ret.result = true;
            #region распределение пачек
            //в зависимости от настроек распределяем пачки сразу 
            if (Points.packDistributionParameters.DistributePackImmediately)
            {

                DbCalcPack db1 = new DbCalcPack();
                DbPack pack = new DbPack();
                for (int i = 0; i < nzpPack.Count; i++)
                {
                    Pack finderp = new Pack();
                    db1.PackFonTasks(nzpPack[i], Points.DateOper.Year, finder.nzp_user, CalcFonTask.Types.DistributePack, out ret);  // Отдаем пачку на распределение                 
                    finderp.flag = Pack.Statuses.WaitingForDistribution.GetHashCode();
                    finderp.nzp_user = finder.nzp_user;
                    finderp.nzp_pack = nzpPack[i];
                    finderp.year_ = Points.DateOper.Year;//Points.CalcMonth.year_;
                    if (ret.result)
                    {
                        db1.UpdatePackStatus(finderp);
                    }
                }
                db1.Close();
                pack.Close();
            }

            #endregion
            return ret;
        }



        /// <summary>
        /// Откат загрузки тульского реестра
        /// </summary>
        /// <param name="connDB"></param>
        /// <param name="finder"></param>
        /// <param name="nzpDownload"></param>
        /// <returns></returns>
        public Returns RollbackReestrTula(IDbConnection connDB, Finder finder, int nzpDownload)
        {
            Returns ret;

            string sql = " select nzp_type from " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads " +
                         " where nzp_download=" + nzpDownload + " ";
            object obj = ExecScalar(connDB, sql, out ret, true);
            if (!ret.result)
            {
                return ret;
            }

            if (obj == null)
            {
                ret.result = false;
                ret.text = "Ошибка удаления данных";
                return ret;
            }

            int nzpType = Convert.ToInt32(obj);
            //Удаляем квитанцию, она удаляется только вместе с файлом реестра

            string finAlias = Points.Pref + "_fin_" + (Points.CalcMonth.year_ - 2000).ToString("00") +
                              tableDelimiter;
            string nameNewFile = "";
            var datePlat = DateTime.MinValue;
            if (nzpType == 2 || nzpType == 4)
            {

                #region Получение имени файла по загрузке
                sql = " select nzp_kvit_reestr from " + Points.Pref + DBManager.sDataAliasRest +
                  "tula_kvit_reestr where nzp_download=" + nzpDownload + " ";
                var nzp_kvit = CastValue<int>(ExecScalar(connDB, sql, out ret, true));

                if (nzp_kvit > 0)
                {
                    sql = "select trim(file_name) from " + Points.Pref + DBManager.sDataAliasRest +
                          "tula_kvit_reestr where nzp_kvit_reestr=" + nzp_kvit;
                    nameNewFile = CastValue<string>(ExecScalar(connDB, sql, out ret, true));
                    sql = "select date_plat from " + Points.Pref + DBManager.sDataAliasRest +
                         "tula_kvit_reestr where nzp_kvit_reestr=" + nzp_kvit;
                    datePlat = CastValue<DateTime>(ExecScalar(connDB, sql, out ret, true));
                }
                else
                {
                    sql = " select file_name from " + Points.Pref + DBManager.sDataAliasRest +
                   "tula_reestr_downloads where nzp_download=" + nzpDownload + " ";

                    object name = ExecScalar(connDB, sql, out ret, true);
                    if (!ret.result)
                    {
                        return ret;
                    }
                    if (name == null)
                    {
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка получения данных о удаляемом файле", MonitorLog.typelog.Error, true);
                        return ret;
                    }
                    string type;
                    var nameFile = Convert.ToString(name).Trim().Split('.');
                    if (nzpType == 2)
                    {
                        type = ".0" + nameFile[1].Substring(1, 2);
                    }
                    else
                    {
                        type = ".00" + nameFile[1].Substring(2);
                    }
                    nameNewFile = nameFile[0] + type;
                }
                #endregion

                #region Проверка распределенных сумм

                sql = " select count(*) as count_rasp " +
                      " from " + finAlias + "pack p," +
                      "      " + finAlias + "pack_ls pl " +
                      " where file_name=" + Utils.EStrNull(nameNewFile) +
                      " and p.nzp_pack=pl.nzp_pack " +
                      " and pl.dat_uchet is not null" +
                        (datePlat != DateTime.MinValue ? " and pl.dat_vvod=" + Utils.EStrNull(datePlat.ToShortDateString()) : "");
                object countRasp = ExecScalar(connDB, sql, out ret, true);
                if ((countRasp != DBNull.Value) && (Int32.Parse(countRasp.ToString()) > 0))
                {
                    ret = new Returns(false, "Есть распределенные оплаты по реестру, " + Environment.NewLine +
                                             "сначала отмените распределение по данному реестру", -1) { result = false };
                    return ret;
                }

                #region Удаляет показания счетчиков
                sql = " delete from " + finAlias + "pu_vals " +
                      " where nzp_pack_ls in (select nzp_pack_ls " +
                      " from " + finAlias + "pack_ls pl, " +
                      " " + finAlias + "pack p" +
                      " where pl.nzp_pack=p.nzp_pack " +
                      " and file_name=" + Utils.EStrNull(nameNewFile) + "" +
                        (datePlat != DateTime.MinValue ? " and pl.dat_vvod=" + Utils.EStrNull(datePlat.ToShortDateString()) : "") +
                      ")";
                if (!ExecSQL(connDB, sql, true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }

                #endregion


                sql = "select nzp_pack from " + finAlias + "pack_ls where nzp_pack in (select nzp_pack" +
                                  " from " + finAlias + "pack " +
                                  " where file_name=" + Utils.EStrNull(nameNewFile) + " )  " +
                                  (datePlat != DateTime.MinValue
                                      ? " and dat_vvod=" + Utils.EStrNull(datePlat.ToShortDateString())
                                      : "") +
                                  " group by 1";
                var DT = ClassDBUtils.OpenSQL(sql, connDB).resultData;
                List<string> l_nzp_pack = new List<string>();
                foreach (DataRow row in DT.Rows)
                {
                    l_nzp_pack.Add(CastValue<string>(row["nzp_pack"]));
                }
                var s_nzp_pack = string.Join(",", l_nzp_pack);


                //Удаление оплат
                sql = " delete  " +
                      " from " + finAlias + "pack_ls " +
                      " where nzp_pack in (select nzp_pack" +
                      " from " + finAlias + "pack " +
                      " where file_name=" + Utils.EStrNull(nameNewFile) + ")" +
                      (datePlat != DateTime.MinValue ? " and dat_vvod=" + Utils.EStrNull(datePlat.ToShortDateString()) : "");
                ExecSQL(connDB, sql, true);


                //Удаление пачек
                sql = " delete  " +
                      " from " + finAlias + "pack " +
                      " where nzp_pack in (" + s_nzp_pack + ")";
                ExecSQL(connDB, sql, true);


                #endregion


                #region Удаляем записи в реестре загрузок


                sql = " delete from " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads " +
                      " where  nzp_download = " + nzpDownload + " ";
                if (!ExecSQL(connDB, sql, true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }


                //Удаляем запись реестра

                sql = "delete from " + Points.Pref + DBManager.sDataAliasRest +
                      "tula_reestr_downloads " +
                      " where  file_name = '" + nameNewFile + "'  and date_download>=" + Utils.EStrNull(Points.DateOper.ToShortDateString());
                if (!ExecSQL(connDB, sql, true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }
                #endregion

                #region Получаем код реестра квитанций

                sql = " select nzp_kvit_reestr" +
                      " from  " + Points.Pref + DBManager.sDataAliasRest + "tula_kvit_reestr " +
                      " where file_name='" + nameNewFile + "' ";
#if PG
                sql += "AND extract(year from date_plat)=extract(year from now())";
#else
            sql += "AND  YEAR(date_plat)= YEAR(TODAY)";
#endif
                object nzpKvit = ExecScalar(connDB, sql, out ret, true);
                if (!ret.result)
                {
                    return ret;
                }
                if (nzpKvit == null)
                {
                    return ret;
                }
                int nzpKvitReestr = Convert.ToInt32(nzpKvit);
                #endregion

                #region Удаляем счетчики

                sql = " delete from " + Points.Pref + DBManager.sDataAliasRest + "tula_counters_reestr " +
                      " where nzp_reestr_d in (select nzp_reestr_d " +
                      "from " + Points.Pref + DBManager.sDataAliasRest + "tula_file_reestr " +
                      " where nzp_kvit_reestr=" + nzpKvitReestr + ")";
                if (!ExecSQL(connDB, sql, true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }


                #endregion

                #region Удаляем платежи
                sql = " delete from " + Points.Pref + DBManager.sDataAliasRest + "tula_file_reestr " +
                     " where nzp_kvit_reestr=" + nzpKvitReestr + "";
                if (!ExecSQL(connDB, sql, true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }
                #endregion

                #region Удаляем квитанцию
                sql = " delete from " + Points.Pref + DBManager.sDataAliasRest + "tula_kvit_reestr " +
                      " where nzp_kvit_reestr=" + nzpKvitReestr + "";
                if (!ExecSQL(connDB, sql, true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }
                #endregion

            }
            else
            {

                #region Проверка распределенных сумм

                sql = " select count(*) as count_rasp " +
                      " from " + finAlias + "pack p," +
                      "      " + finAlias + "pack_ls pl, " +
                      "      " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d " +
                      " where  d.nzp_download=" + nzpDownload +
                      "     and p.file_name=d.file_name " +
                      "     and p.nzp_pack=pl.nzp_pack " +
                      "     and pl.dat_uchet is not null";
                object countRasp = ExecScalar(connDB, sql, out ret, true);
                if ((countRasp != DBNull.Value) && (Int32.Parse(countRasp.ToString()) > 0))
                {
                    ret = new Returns(false, "Есть распределенные оплаты по реестру, " + Environment.NewLine +
                                             "сначала отмените распределение по данному реестру", -1) { result = false };
                    return ret;
                }

                sql = " delete from " + finAlias + "pu_vals " +
                      " where nzp_pack_ls in (select nzp_pack_ls " +
                      " from " + finAlias + "pack_ls pl, " +
                      " " + finAlias + "pack p, " +
                      "      " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d " +
                      " where pl.nzp_pack=p.nzp_pack " +
                      " and p.file_name=d.file_name)";
                if (!ExecSQL(connDB, sql, true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }

                #endregion

                //Удаление оплат
                sql = " delete  " +
                      " from " + finAlias + "pack_ls " +
                      " where nzp_pack in (select nzp_pack" +
                      " from " + finAlias + "pack p, " +
                      "      " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d " +
                      "  where d.nzp_download=" + nzpDownload +
                      "     and p.file_name=d.file_name)";
                ExecSQL(connDB, sql, true);

                //Удаление пачек
                sql = " delete  " +
                      " from " + finAlias + "pack p " +
                      " where file_name in (select file_name" +
                      " from     " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d " +
                      "  where d.nzp_download=" + nzpDownload + ")";
                ExecSQL(connDB, sql, true);





                #region Удаляем счетчики

                sql = " delete " +
                      " from " + Points.Pref + DBManager.sDataAliasRest + "tula_counters_reestr " +
                      " where nzp_reestr_d in (select nzp_reestr_d " +
                      " from  " + Points.Pref + DBManager.sDataAliasRest + "tula_kvit_reestr kv,  " +
                      Points.Pref + DBManager.sDataAliasRest + "tula_file_reestr t, " +
                      Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d " +
                      " where trim(d.file_name)=trim(kv.file_name) " +
                      " and t.nzp_kvit_reestr =kv.nzp_kvit_reestr " +
                      " and d.nzp_download=" + nzpDownload + ") ";
                if (!ExecSQL(connDB, sql, true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }


                #endregion


                //Удаляем данные реестра
                sql = " delete from  " + Points.Pref + DBManager.sDataAliasRest +
                      "tula_file_reestr where nzp_kvit_reestr = " +
                      " (select nzp_kvit_reestr from  " + Points.Pref + DBManager.sDataAliasRest +
                      "tula_kvit_reestr kv,  " +
                      Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads d " +
                      " where trim(d.file_name)=trim(kv.file_name) " +
                      " and d.nzp_download=" + nzpDownload + ") ";
                if (!ExecSQL(connDB, sql, true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных реестра";
                    return ret;
                }


                //Удаляем запись реестра

                sql = " delete from " + Points.Pref + DBManager.sDataAliasRest + "tula_reestr_downloads " +
                      " where  nzp_download = " + nzpDownload + " ";
                if (!ExecSQL(connDB, sql, true).result)
                {
                    ret.result = false;
                    ret.text = "Ошибка удаления данных загрузки";
                    return ret;
                }

            }

            return ret;
        }

    }

    /// <summary>
    /// Загрузка реестра из банка для Байкальска (Соцзащита, льготы)
    /// </summary>
    public class DbPaymentsFromBankSocProtectL : DbPaymentsFromBankBaikalVstkb
    {

        /// <summary>
        /// Прочитать строки файла оплат
        /// </summary>
        /// <param name="path">Путь к файлу</param>
        /// <returns></returns>
        public override string[] ReadReestrFile(string path)
        {
            var exlApp = new Excel.Application();
            exlApp.DisplayAlerts = false;

            var exlWb = exlApp.Workbooks.Open(path, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            var exlWs = (Excel.Worksheet)exlWb.Worksheets.get_Item(1);
            exlWs.Visible = Excel.XlSheetVisibility.xlSheetVisible;

            var reestr = new List<string>();
            var currDay = DateTime.Now;
            var payDay = currDay.ToString("dd.MM.yyyy");
            var transactionId = Math.Abs(currDay.GetHashCode());
            string operDay = (new DateTime(Points.DateOper.Year, Points.DateOper.Month, Points.DateOper.Day)).ToString("dd.MM.yyyy");

            try
            {
                var iRowCount = exlWs.UsedRange.Rows.Count;
                //var iColumnCount = exlWs.UsedRange.Columns.Count;

                for (var i = 2; i <= iRowCount; i++) // в первой строке у нас заголовок
                {
                    var valNum = (Excel.Range)exlWs.Cells[i, 1];
                    string valCellNum = valNum.get_Value(Type.Missing) != null ? valNum.get_Value(Type.Missing).ToString().Trim().Replace("_", "") : "";
                    var valAddress = (Excel.Range)exlWs.Cells[i, 3];
                    string valCellAddress = valAddress.get_Value(Type.Missing) != null ? valAddress.get_Value(Type.Missing).ToString().Trim() : "";
                    var valMoney = (Excel.Range)exlWs.Cells[i, 4];
                    string valCellMoney = valMoney.get_Value(Type.Missing) != null ? valMoney.get_Value(Type.Missing).ToString().Trim() : "";

                    if (valCellNum != "" && valCellMoney != "" && valCellAddress != "")
                    {
                        var index = reestr.FindIndex(x => x.Contains(valCellNum));
                        if (index >= 0) // такой банковский счет уже встречался
                        {
                            var findedStr = reestr[index];
                            string[] findedStrFields = findedStr.Split(';');

                            var sum = Convert.ToDecimal(findedStrFields[2]) + Convert.ToDecimal(valCellMoney);
                            findedStrFields[2] = sum.ToString();
                            reestr[index] = findedStrFields[0] + ";" + findedStrFields[1] + ";" + findedStrFields[2] + ";" + findedStrFields[3] + ";" + findedStrFields[4] + ";" + findedStrFields[5];
                        }
                        else
                        {
                            reestr.Add(valCellNum + ";" + valCellAddress + ";" + valCellMoney.Replace(',', '.') + ";" + operDay + ";" + transactionId.ToString().PadRight(26, '0') + ";" + transactionId);
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                // удалить реестр
                throw new Exception("Ошибка при записи данных, функция GetUploadReestr " + ex.Message);
            }
            finally
            {
                if (exlWb != null)
                {
                    Marshal.ReleaseComObject(exlWs);
                    exlWb.Close(false, Type.Missing, Type.Missing);
                    Marshal.ReleaseComObject(exlWb);
                    exlWb = null;
                }
                exlApp.Quit();
                Marshal.ReleaseComObject(exlApp);
                exlApp = null;
            }


            return reestr.ToArray();
        }
    }

    /// <summary>
    /// Загрузка реестра из банка для Байкальска (Соцзащита, субсидии)
    /// </summary>
    public class DbPaymentsFromBankSocProtectS : DbPaymentsFromBankSocProtectL
    {

    }

    /// <summary>
    /// Загрузка реестра из банка для Байкальска (ВСТКБ)
    /// </summary>
    public class DbPaymentsFromBankBaikalVstkb : DbBasePaymentsFromBankBft
    {

        public override FilesImported FastCheck(FilesImported finder, out Returns ret)
        {
            //директория файла   
            string fDirectory;
            Utils.setCulture(); // установка региональных настроек
            if (InputOutput.useFtp)
            {
                fDirectory = InputOutput.GetInputDir();
                InputOutput.DownloadFile(finder.loaded_name, fDirectory + finder.saved_name, true);
            }
            else
            {
                fDirectory = Constants.Directories.ImportAbsoluteDir;
            }
            finder.ex_path = Path.Combine(fDirectory, finder.saved_name);

            #region проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return finder;
            }
            #endregion

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                return finder;
            }
            try
            {
                //проверка на существование такого же загруженного файла
                if (!ExistFile(connDb, finder.saved_name))
                {
                    ret.text = "\nФайл с таким именем уже был загружен";
                    ret.result = false;
                    ret.tag = 996;
                    if (InputOutput.useFtp) File.Delete(finder.ex_path);
                    return finder;
                }
            }
            finally
            {
                connDb.Close();
            }

            return finder;
        }

        /// <summary>
        /// Сохранить в реестр квитанций
        /// </summary>
        /// <param name="connDb"></param>
        /// <param name="finder"></param>
        /// <returns>Returns</returns>
        override protected Returns SaveKvitReestr(IDbConnection connDb, FilesImported finder)
        {
            string sqlStr = "INSERT INTO " + Points.Pref + sDataAliasRest + "tula_kvit_reestr (date_plat,file_name, branch_id, is_itog) " +
                            " VALUES " +
                            " ( " + DBManager.sCurDate + ", " + Utils.EStrNull(finder.saved_name) + ", '" + _fileArgs.OtdNumber + "', 1)";

            Returns ret = ExecSQL(connDb, sqlStr, true);

            if (!ret.result) throw new UserException("Ошибка при записи данных квитанции итогового реестра");

            return ret;
        }

        /// <summary>
        /// Заполнение файловой структуры
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <returns></returns>
        public override FileNameStruct getFileName(string fileName)
        {
            var fileStruct = new FileNameStruct();
            string[] fileN = fileName.Split('.');
            try
            {
                fileStruct.Number = fileN[0];
                fileStruct.FileType = FileNameStruct.ReestrTypes.Svod;
                fileStruct.OtdNumber = fileN[1];
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка разбора имени файла: " + ex.Message, MonitorLog.typelog.Error, true);
            }
            return fileStruct;
        }
    }

    /// <summary>
    /// Загрузка реестра из банка для Байкальска (Соцзащита, субсидии)
    /// </summary>
    public class DbPaymentsFromBankTagilSber : DbPaymentsFromBankBaikalVstkb
    {
        /// <summary>
        /// Сохранить в реестр квитанций
        /// </summary>
        /// <param name="connDb"></param>
        /// <param name="finder"></param>
        /// <returns>Returns</returns>
        override protected Returns SaveKvitReestr(IDbConnection connDb, FilesImported finder)
        {
            string sqlStr = "INSERT INTO " + Points.Pref + sDataAliasRest + "tula_kvit_reestr (date_plat,file_name, branch_id, is_itog) " +
                            " VALUES " +
                            " ( '" + _fileArgs.Day + "." + _fileArgs.Month + "." + DateTime.Now.Year + "', " + Utils.EStrNull(finder.saved_name) + ", '" + _fileArgs.OtdNumber + "', 1)";

            Returns ret = ExecSQL(connDb, sqlStr, true);

            if (!ret.result) throw new UserException("Ошибка при записи данных квитанции итогового реестра");

            return ret;
        }

        /// <summary>
        /// Заполнение файловой структуры
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <returns></returns>
        public override FileNameStruct getFileName(string fileName)
        {
            var fileStruct = new FileNameStruct();
            string[] fileN = fileName.Split('.');
            try
            {
                fileStruct.Number = fileN[0];
                fileStruct.Day = Convert.ToInt32(fileN[0].Substring(4, 2));
                fileStruct.Month = Convert.ToInt32(fileN[0].Substring(6, 2));
                fileStruct.FileType = FileNameStruct.ReestrTypes.Svod;
                fileStruct.OtdNumber = fileN[1];
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка разбора имени файла: " + ex.Message, MonitorLog.typelog.Error, true);
            }
            return fileStruct;
        }

        /// <summary>
        /// Прочитать строки файла оплат
        /// </summary>
        /// <param name="path">Путь к файлу</param>
        /// <returns></returns>
        public override string[] ReadReestrFile(string path)
        {
            var reestr = new List<string>();
            try
            {
                using (DataTable tbl = new exDBF("TableName").Load(path, 866))
                {
                    for (int i = 0; i < tbl.Rows.Count; i++)
                    {
                        var row = tbl.Rows[i];
                        var datePayment = (row["DATA"] != DBNull.Value) ? Convert.ToDateTime(row["DATA"]).ToShortDateString() : "";
                        if (Convert.ToInt32(row["STRING"]) == 0 && datePayment.Length != 0)
                        {
                            var transactionId = (row["IDEN"] != DBNull.Value) ? Convert.ToString(row["IDEN"]).Replace("_", "").PadRight(26, '0') : "";
                            var pkod = (row["LS"] != DBNull.Value) ? Convert.ToString(row["LS"]) : "";
                            var sum = (row["SUMMA"] != DBNull.Value) ? Convert.ToString(row["SUMMA"]).Replace(',', '.') : "";
                            var npp = (row["NPP"] != DBNull.Value) ? Convert.ToString(row["NPP"]) : "";
                            var datePp = (row["DATPP"] != DBNull.Value) ? Convert.ToDateTime(row["DATPP"]).ToShortDateString() : "";
                            reestr.Add(transactionId + ";" + datePayment + ";" + pkod + ";" + sum + ";" + npp + ";" + datePp);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при записи данных, функция ReadReestrFile " + ex.Message);
            }

            return reestr.ToArray();
        }

    }

    /// <summary>
    /// Абстрактный класс для разбора строки платежа по реестру
    /// </summary>
    abstract public class DbBaseReestrFromBankBft : DataBaseHead
    {
        protected class ReestrCounter
        {
            public string Cnt { get; set; }
            public string ValCnt { get; set; }

            public ReestrCounter()
            {
                Cnt = String.Empty;
                ValCnt = String.Empty;
            }
        }

        protected class ReestrBody
        {
            public string LSKod { get; set; }
            public decimal SumPlat { get; set; }
            public string TransactionID { get; set; }
            public string NomerPlatPoruch { get; set; }
            public string DatePlatPoruch { get; set; }
            public string ServiceField { get; set; }
            public string PaymentDate { get; set; }
            public string PaymentTime { get; set; }
            public string Address { get; set; }
            public int ServiceNumber { get; set; }

            public List<ReestrCounter> counterList = new List<ReestrCounter>();
        }


        protected readonly DbLoadProtokol _loadProtokol;
        protected readonly FileNameStruct _fileArgs;
        public string BackTransaction = "";

        protected int _numRow;

        protected readonly IDbConnection _connDB;

        public DbBaseReestrFromBankBft(IDbConnection connDB, FileNameStruct fileArgs, DbLoadProtokol loadProtokol)
        {
            _loadProtokol = loadProtokol;
            _connDB = connDB;
            _fileArgs = fileArgs;
        }


        /// <summary>
        /// Проверка счетчиков на допустимое значение
        /// </summary>
        /// <param name="numCnt"></param>
        /// <param name="valCnt"></param>
        /// <returns></returns>
        protected static string FillNumCounter(string numCnt, string valCnt)
        {
            var numResult = String.IsNullOrEmpty(numCnt)
                ? "null"
                : "'" + numCnt + "'";
            var valResult = (valCnt == "НЕТ" || valCnt == "" || valCnt == " " || valCnt == null)
                ? "null"
                : valCnt;

            return numResult + ',' + valResult;
        }


        /// <summary>
        /// Проверка на существование платежа
        /// </summary>
        /// <param name="bodyReestr"></param>
        /// <returns></returns>
        virtual protected bool IsPaymentExists(ReestrBody bodyReestr)
        {
            bool result = true;

            string sqlStr = " select sum(sum_charge) as sum_charge" +
                            " from " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr " +
                            " where pkod=" + bodyReestr.LSKod + " " +
                            " and transaction_id='" + bodyReestr.TransactionID + "'";

            Returns ret;
            object obj = ExecScalar(_connDB, sqlStr, out ret, true);
            if (obj != null && obj != DBNull.Value)
            {
                result = false;

                if (Convert.ToDecimal(obj) > 0)
                {
                    if (bodyReestr.SumPlat != Convert.ToDecimal(obj))
                    {
                        _loadProtokol.AddComment("Дублированный номер банковской транзакции " +
                                                 " платежный код " + bodyReestr.LSKod + " номер транзации " + bodyReestr.TransactionID);
                        result = true;
                    }

                }
            }

            return result;
        }

        /// <summary>
        /// Заполнение структуры платежа из строки
        /// </summary>
        /// <param name="fields">набор полей строки</param>
        /// <returns></returns>
        abstract protected ReestrBody FillBody(string[] fields);

        /// <summary>
        /// Проверка всех платежей
        /// </summary>
        /// <returns></returns>
        abstract public bool CheckAllPayments();

        /// <summary>
        /// Построчная разборка реестра квитанций
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="reestrStrings"></param>
        /// <returns></returns>
        public Returns ParseReestr(FilesImported finder, string[] reestrStrings)
        {
            //вызываем проверку на ошибки в заполнении строк
            Returns ret = FindErrors(reestrStrings);
            if (!ret.result)
            {
                _loadProtokol.AddUncorrectedRows(ret.text);
                ret.text = "";
                return ret;
            }

            //сопоставление указанного в квитанции для реестра кол-ва строк и суммы оплат с данными в файле реестра 
            ret = CheckReestrAtr(reestrStrings.Length, finder.saved_name);
            if (!ret.result)
                return ret;

            try
            {
                double costOneRow = Math.Round(30d / Math.Max(finder.count_rows, 1), 4);
                int counter = 0;
                double currentProgres = 0;
                //заголовочная структура

                foreach (var str in reestrStrings)
                {
                    _numRow++;
                    counter++;

                    ret = ParseOneString(str);
                    if (!ret.result) return ret;

                    if (counter % 10 == 0)
                    {
                        currentProgres += costOneRow * 10;
                        _loadProtokol.SetProcent(currentProgres, (int)StatusWWB.InProcess);
                    }
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "\nОшибка в строке " + _numRow + " при разборе файла оплат от банка ";
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 20, 201, true);
            }

            return ret;
        }

        /// <summary>
        /// Проверка строк по списку возможных ошибок в заполнении файла
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        abstract public Returns FindErrors(string[] rows);

        /// <summary>
        /// сопоставление указанного в квитанции для реестра кол-ва строк и суммы оплат с данными в файле реестра 
        /// </summary>
        /// <param name="reestrRowsCount"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        abstract public Returns CheckReestrAtr(int reestrRowsCount, string fileName);

        /// <summary>
        /// Добавление платежа в БД
        /// </summary>
        /// <param name="bodyReestr"></param>
        /// <returns></returns>
        abstract protected bool InsertPayment(ReestrBody bodyReestr);

        /// <summary>
        /// Определить код счетчика по номеру
        /// </summary>
        /// <param name="bodyReestr"></param>
        /// <returns></returns>
        abstract protected string GetNzpCounter(string numCnt, string pkod);

        /// <summary>
        /// Добавление счетчиков
        /// </summary>
        /// <param name="bodyReestr">Строка реестра оплат</param>
        /// <param name="nzpReestrD">Код реестра</param>
        protected void InsertCounters(ReestrBody bodyReestr, int nzpReestrD)
        {
            string sqlStr;

            for (int i = 0; i < bodyReestr.counterList.Count; i++)
            {
                if (!String.IsNullOrEmpty(bodyReestr.counterList[i].Cnt) && !String.IsNullOrEmpty(bodyReestr.counterList[i].ValCnt))
                {
                    sqlStr = "INSERT INTO " + Points.Pref + "_data" + tableDelimiter + "tula_counters_reestr " +
                             " (nzp_reestr_d, nzp_counter, cnt, val_cnt) " +
                             " VALUES ( " + nzpReestrD + ", " + GetNzpCounter(bodyReestr.counterList[i].Cnt, bodyReestr.LSKod) + "," +
                             FillNumCounter(bodyReestr.counterList[i].Cnt, bodyReestr.counterList[i].ValCnt) + ")";
                    ExecSQL(_connDB, sqlStr, true);
                }
            }
        }

        /// <summary>
        /// Разбор строки платежа
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        virtual protected Returns ParseOneString(string str)
        {
            string[] fields = str.Split(';');
            Returns ret = Utils.InitReturns();
            var bodyReestr = FillBody(fields);
            _loadProtokol.SumCharge += bodyReestr.SumPlat;

            //проверка на существование оплаты(если уже есть, то не пишем)
            if (IsPaymentExists(bodyReestr))
            {
                if (!InsertPayment(bodyReestr))
                {
                    ret.result = false;
                    string numStr = "\n Номер строки с ошибкой: " + _numRow + ". Данные платежа: ЛС " + bodyReestr.LSKod + ", сумма " + bodyReestr.SumPlat;
                    ret.text = "\nОшибка при записи данных итогового реестра" + numStr;
                    return ret;

                }
                _loadProtokol.CountInsertedRows++;
            }
            return ret;
        }
    }


    //******************************************************************************************************************************************
    /// <summary>
    /// Класс для разбора строки платежа по реестру для Соцзащиты, льготы (Байкальск)
    /// </summary>
    public class DbReestrFromBankBaikalSocProtectL : DbReestrFromBankBaikalVstkb
    {
        public DbReestrFromBankBaikalSocProtectL(IDbConnection connDB, FileNameStruct fileArgs, DbLoadProtokol loadProtokol)
            : base(connDB, fileArgs, loadProtokol)
        {
        }

        /// <summary>
        /// Проверка на существование платежа
        /// </summary>
        /// <param name="bodyReestr"></param>
        /// <returns></returns>
        override protected bool IsPaymentExists(ReestrBody bodyReestr)
        {
            return true;
        }

        /// <summary>
        /// Заполнение полей реестра из разбитой строки платежа
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        protected override ReestrBody FillBody(string[] fields)
        {
            var body = new ReestrBody();

            // Банковский счет
            if (fields[0].Trim() != "")
            {
                body.LSKod = fields[0].Trim();
            }

            // Адрес плательщика 
            if (fields[1].Trim() != "")
            {
                body.Address = fields[1].Trim();
            }

            // Сумма платежа
            if (fields[2].Trim() != "")
            {
                body.SumPlat = Convert.ToDecimal(fields[2].Trim());
            }

            // Дата совершения платежа
            if (fields[3].Trim() != "")
            {
                body.PaymentDate = fields[3].Trim();
            }

            // Уникальный номер операции в системе платежного агента
            if (fields[4].Trim() != "")
            {
                body.TransactionID = fields[4].Trim();
            }

            // Уникальный номер операции в системе платежного агента
            if (fields[5].Trim() != "")
            {
                body.NomerPlatPoruch = fields[5].Trim();
            }

            return body;
        }

        public override bool CheckAllPayments()
        {
            return true;
        }

        /// <summary>
        /// Проверка строк по списку возможных ошибок в заполнении файла
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        public override Returns FindErrors(string[] rows)
        {
            Returns ret = Utils.InitReturns();
            Utils.setCulture(); // установка региональных настроек
            var numRows = new Dictionary<int, string>();
            bool bankAccountError = false;

            for (int i = 0; i < rows.Length; i++)
            {
                bool add = false;
                string err = "";

                //проверяем структуру строки на целостность
                string[] rowEl = rows[i].Split(';');
                if (rowEl.Length != 6)
                {
                    add = true;
                    err += (err != ""
                        ? ", Нарушен формат реестра: Неверное число полей в строке"
                        : " Нарушен формат реестра: Неверное число полей в строке");
                }

                //число полей по формату
                if (rowEl.Length == 6)
                {
                    //Код лицевого счета
                    if (!Regex.IsMatch(rowEl[0], @"^\d+$") || rowEl[0].Trim().Length > 20)
                    {
                        add = true;
                        err += (err != ""
                            ? ", Нарушен формат поля \"Код банковского счета\""
                            : "Нарушен формат поля \"Код банковского счета\"");
                    }

                    //Сумма платежа           
                    if (!Regex.IsMatch(rowEl[2], @"^[0-9]+(\.[0-9]+)?$"))
                    {
                        add = true;
                        err += (err != ""
                            ? ", Нарушен формат поля \"Сумма платежа\""
                            : "Нарушен формат поля \"Сумма платежа\"");
                    }
                    else
                    {
                        decimal sumOpl;
                        Decimal.TryParse(rowEl[2], out sumOpl);
                        _loadProtokol.TotalSumPack += sumOpl;
                    }

                    //Проверяем есть ли номер банковского счета в базе
                    var sqlCheck = "select count(*) from " + Points.Pref + sDataAliasRest + ".bank_accounts where bank_account_number=" + Utils.EStrNull(rowEl[0].Trim());
                    object obj = ExecScalar(_connDB, sqlCheck, out ret, true);
                    int num = 0;
                    if (obj != null && obj != DBNull.Value)
                    {
                        num = Convert.ToInt32(obj);
                    }
                    if (num == 0)
                    {
                        bankAccountError = true;
                        _loadProtokol.AddUncorrectedRows("Банковский счет отсутствует - " + rowEl[0].Trim());
                    }

                    //остальные поля нас не интересуют
                }

                //добавляем номер строки в список строк с ошибками
                if (add) numRows.Add(i + 1, err);
            }
            if (numRows.Count > 0)
            {
                ret.result = false;
                ret.text = "Номера строк с ошибками:\r\n";
                foreach (var num in numRows)
                {
                    ret.text += "Строка: " + num.Key + ", Ошибки: " + num.Value + ";\r\n";
                }
                ret.text = ret.text.Remove(ret.text.Length - 1, 1);
            }
            if (bankAccountError)
            {
                ret.result = false;
            }

            return ret;
        }

        /// <summary>
        /// Добавление платежа в БД
        /// </summary>
        /// <param name="bodyReestr"></param>
        /// <returns></returns>
        override protected bool InsertPayment(ReestrBody bodyReestr)
        {
            Returns ret;

            string sqlStr = "update " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr " +
                            " set sum_charge = sum_charge + " + bodyReestr.SumPlat +
                            " where pkod = (select distinct k.pkod from " + Points.Pref + "_data" + tableDelimiter + "bank_accounts b left join " + Points.Pref + "_data" + tableDelimiter + "kvar k on k.nzp_kvar=b.nzp_kvar where b.bank_account_number = '" + bodyReestr.LSKod + "')" +
                            " and nzp_kvit_reestr = " + _fileArgs.KvitID +
                            " and transaction_id = " + Utils.EStrNull(bodyReestr.TransactionID) +
                            " and date_plat_poruch = " + Utils.EStrNull(bodyReestr.PaymentDate) +
                            " and nomer_plat_poruch = " + Utils.EStrNull(bodyReestr.NomerPlatPoruch);
            ret = ExecSQL(_connDB, sqlStr, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка обновления  " + (Constants.Viewerror ? "\n" + sqlStr : ""), MonitorLog.typelog.Error, 20, 201, true);
                _loadProtokol.AddComment("\nОшибка обновления данных реестра");
                return false;
            }

            var sqlCheck = "select count(*) from " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr " +
                            " where pkod = (select distinct k.pkod from " + Points.Pref + "_data" + tableDelimiter + "bank_accounts b left join " + Points.Pref + "_data" + tableDelimiter + "kvar k on k.nzp_kvar=b.nzp_kvar where b.bank_account_number = '" + bodyReestr.LSKod + "')" +
                            " and nzp_kvit_reestr = " + _fileArgs.KvitID +
                            " and transaction_id = " + Utils.EStrNull(bodyReestr.TransactionID) +
                            " and date_plat_poruch = " + Utils.EStrNull(bodyReestr.PaymentDate) +
                            " and nomer_plat_poruch = " + Utils.EStrNull(bodyReestr.NomerPlatPoruch);

            object obj = ExecScalar(_connDB, sqlCheck, out ret, true);
            int num = 0;
            if (obj != null && obj != DBNull.Value)
            {
                num = Convert.ToInt32(obj);
            }

            if (num > 0)
            {
                return true;
            }

            sqlStr = "INSERT INTO " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr " +
                            " (pkod, nzp_kvit_reestr, sum_charge, transaction_id, address, date_plat_poruch, nomer_plat_poruch) VALUES (" +
                            "(select distinct k.pkod from " + Points.Pref + "_data" + tableDelimiter + "bank_accounts b left join " + Points.Pref + "_data" + tableDelimiter + "kvar k on k.nzp_kvar=b.nzp_kvar where b.bank_account_number = '" + bodyReestr.LSKod + "'), " +
                            _fileArgs.KvitID + ", " +
                            bodyReestr.SumPlat + ", " +
                            Utils.EStrNull(bodyReestr.TransactionID) + ", " +
                            Utils.EStrNull(bodyReestr.Address) + "," +
                            Utils.EStrNull(bodyReestr.PaymentDate) + ", " +
                            Utils.EStrNull(bodyReestr.NomerPlatPoruch) + ")";

            ret = ExecSQL(_connDB, sqlStr, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка сохранения  " + (Constants.Viewerror ? "\n" + sqlStr : ""), MonitorLog.typelog.Error, 20, 201, true);
                _loadProtokol.AddComment("\nОшибка сохранения данных реестра");
                if (bodyReestr.TransactionID.Length > 30)
                    _loadProtokol.AddComment(
                        ". Превышена длина поля \"Номер транзакции\", по формату " +
                        "длина поля составляет 30 символов");
                return false;
            }

            return true;
        }

    }

    //******************************************************************************************************************************************
    /// <summary>
    /// Класс для разбора строки платежа по реестру для Соцзащиты, субсидии (Байкальск)
    /// </summary>
    public class DbReestrFromBankBaikalSocProtectS : DbReestrFromBankBaikalSocProtectL
    {
        public DbReestrFromBankBaikalSocProtectS(IDbConnection connDB, FileNameStruct fileArgs, DbLoadProtokol loadProtokol)
            : base(connDB, fileArgs, loadProtokol)
        {
        }

    }

    //******************************************************************************************************************************************
    /// <summary>
    /// Класс для разбора строки платежа по реестру для Cбербанка (Байкальск)
    /// </summary>
    public class DbReestrFromBankBaikalSber : DbReestrFromBankBaikalVstkb
    {
        public DbReestrFromBankBaikalSber(IDbConnection connDB, FileNameStruct fileArgs, DbLoadProtokol loadProtokol)
            : base(connDB, fileArgs, loadProtokol)
        {
            _realNumbers = new Dictionary<string, string>();
        }

        private Dictionary<string, string> _realNumbers;

        /// <summary>
        /// Проверка на существование платежа
        /// </summary>
        /// <param name="bodyReestr"></param>
        /// <returns></returns>
        override protected bool IsPaymentExists(ReestrBody bodyReestr)
        {
            return true;
        }

        /// <summary>
        /// Добавление платежа в БД
        /// </summary>
        /// <param name="bodyReestr"></param>
        /// <returns></returns>
        override protected bool InsertPayment(ReestrBody bodyReestr)
        {
            Returns ret;

            string sqlStr = "update " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr " +
                            " set sum_charge = sum_charge + " + bodyReestr.SumPlat +
                            " where pkod = " + bodyReestr.LSKod +
                            " and nzp_serv = " + bodyReestr.ServiceNumber +
                            " and nzp_kvit_reestr = " + _fileArgs.KvitID +
                            " and transaction_id = " + Utils.EStrNull(bodyReestr.TransactionID) +
                            " and date_plat_poruch = " + Utils.EStrNull(bodyReestr.PaymentDate) +
                            " and nomer_plat_poruch = " + Utils.EStrNull(bodyReestr.NomerPlatPoruch);
            ret = ExecSQL(_connDB, sqlStr, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка обновления  " + (Constants.Viewerror ? "\n" + sqlStr : ""), MonitorLog.typelog.Error, 20, 201, true);
                _loadProtokol.AddComment("\nОшибка обновления данных реестра");
                return false;
            }

            var sqlCheck = "select count(*) from " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr " +
                            " where pkod = " + bodyReestr.LSKod +
                            " and nzp_serv = " + bodyReestr.ServiceNumber +
                            " and nzp_kvit_reestr = " + _fileArgs.KvitID +
                            " and transaction_id = " + Utils.EStrNull(bodyReestr.TransactionID) +
                            " and date_plat_poruch = " + Utils.EStrNull(bodyReestr.PaymentDate) +
                            " and nomer_plat_poruch = " + Utils.EStrNull(bodyReestr.NomerPlatPoruch);

            object obj = ExecScalar(_connDB, sqlCheck, out ret, true);
            int num = 0;
            if (obj != null && obj != DBNull.Value)
            {
                num = Convert.ToInt32(obj);
            }

            if (num > 0)
            {
                return true;
            }

            sqlStr = "INSERT INTO " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr " +
                            " (pkod, nzp_kvit_reestr, sum_charge, transaction_id, nzp_serv, address, date_plat_poruch, nomer_plat_poruch) VALUES (" +
                            bodyReestr.LSKod + ", " + _fileArgs.KvitID + ", " +
                            bodyReestr.SumPlat + ", " +
                            Utils.EStrNull(bodyReestr.TransactionID) + ", " +
                            (bodyReestr.ServiceNumber == 0 ? "null" : bodyReestr.ServiceNumber.ToString()) + ", " +
                            Utils.EStrNull(bodyReestr.Address) + "," +
                            Utils.EStrNull(bodyReestr.PaymentDate) + ", " +
                            Utils.EStrNull(bodyReestr.NomerPlatPoruch) + ")";

            ret = ExecSQL(_connDB, sqlStr, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка сохранения  " + (Constants.Viewerror ? "\n" + sqlStr : ""), MonitorLog.typelog.Error, 20, 201, true);
                _loadProtokol.AddComment("\nОшибка сохранения данных реестра");
                if (bodyReestr.TransactionID.Length > 30)
                    _loadProtokol.AddComment(
                        ". Превышена длина поля \"Номер транзакции\", по формату " +
                        "длина поля составляет 30 символов");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Разбор строки платежа
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        override protected Returns ParseOneString(string str)
        {
            string[] fields = str.Split(';');
            Returns ret = Utils.InitReturns();
            var bodyReestr = FillBody(fields);

            // Т.к. Сбер присылает не совсем нужные платежные коды, то исправляем это
            string keyLsDate = String.Format("{0}:{1}", bodyReestr.LSKod, bodyReestr.PaymentDate);
            string value;
            if (_realNumbers.TryGetValue(keyLsDate, out value))
            {
                bodyReestr.NomerPlatPoruch = value;
            }
            else
            {
                _realNumbers.Add(key: keyLsDate, value: bodyReestr.NomerPlatPoruch);
            }

            // оставляем только 5 цифр номера платежного поручения
            var lenNomerPlatPoruch = bodyReestr.NomerPlatPoruch.Length;
            if (lenNomerPlatPoruch > 5)
            {
                bodyReestr.TransactionID = bodyReestr.NomerPlatPoruch.PadRight(26, '0');
                bodyReestr.NomerPlatPoruch = bodyReestr.NomerPlatPoruch.Remove(0, lenNomerPlatPoruch - 5);
            }

            _loadProtokol.SumCharge += bodyReestr.SumPlat;
            //проверка на существование оплаты(если уже есть, то не пишем)
            if (IsPaymentExists(bodyReestr))
            {
                if (!InsertPayment(bodyReestr))
                {
                    ret.result = false;
                    string numStr = "\n Номер строки с ошибкой: " + _numRow + ". Данные платежа: ЛС " + bodyReestr.LSKod + ", сумма " + bodyReestr.SumPlat;
                    ret.text = "\nОшибка при записи данных итогового реестра" + numStr;
                    return ret;

                }
                _loadProtokol.CountInsertedRows++;
            }
            return ret;
        }

    }

    //******************************************************************************************************************************************
    /// <summary>
    /// Класс для разбора строки платежа по реестру для ВСТКБ (Байкальск)
    /// </summary>
    public class DbReestrFromBankBaikalVstkb : DbBaseReestrFromBankBft
    {
        public DbReestrFromBankBaikalVstkb(IDbConnection connDB, FileNameStruct fileArgs, DbLoadProtokol loadProtokol)
            : base(connDB, fileArgs, loadProtokol)
        {

        }

        /// <summary>
        /// Проверка на существование платежа
        /// </summary>
        /// <param name="bodyReestr"></param>
        /// <returns></returns>
        override protected bool IsPaymentExists(ReestrBody bodyReestr)
        {
            bool result = true;

            string sqlStr = " select sum(sum_charge) as sum_charge" +
                            " from " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr " +
                            " where pkod=" + bodyReestr.LSKod + " " +
                            " and nzp_kvit_reestr = " + _fileArgs.KvitID +
                            " and transaction_id='" + bodyReestr.TransactionID + "' " +
                            " and nzp_serv = " + bodyReestr.ServiceNumber;

            Returns ret;
            object obj = ExecScalar(_connDB, sqlStr, out ret, true);
            if (obj != null && obj != DBNull.Value)
            {
                result = false;

                if (Convert.ToDecimal(obj) > 0)
                {
                    if (bodyReestr.SumPlat != Convert.ToDecimal(obj))
                    {
                        _loadProtokol.AddComment("Дублированный номер банковской транзакции " +
                                                 " платежный код " + bodyReestr.LSKod + ", номер транзации " + bodyReestr.TransactionID + ", код услуги " + bodyReestr.ServiceNumber);
                        result = true;
                    }

                }
            }

            return result;
        }

        /// <summary>
        /// Заполнение полей реестра из разбитой строки платежа
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        protected override ReestrBody FillBody(string[] fields)
        {
            var body = new ReestrBody();

            // Уникальный номер операции в системе платежного агента
            if (fields[10].Trim() != "")
            {
                body.TransactionID = fields[10].Trim().PadRight(26, '0');
            }

            // Номер платежного поручения
            if (fields[10].Trim() != "")
            {
                body.NomerPlatPoruch = fields[10].Trim();
            }

            // Номер услуги
            if (fields[4].Trim() != "")
            {
                body.ServiceNumber = Convert.ToInt32(fields[4].Trim());
                // хак подмены кодов услуг
                if (body.ServiceNumber == 800)
                {
                    body.ServiceNumber = 8;
                }
                else if (body.ServiceNumber == 900)
                {
                    body.ServiceNumber = 9;
                }
            }

            // Дата совершения платежа
            if (fields[8].Trim() != "")
            {
                body.PaymentDate = fields[8].Trim().Replace("/", ".");
            }

            // Код ЛС
            if (fields[2].Trim() != "")
            {
                body.LSKod = fields[2].Trim();
            }

            // Адрес плательщика 
            if (fields[1].Trim() != "")
            {
                body.Address = fields[1].Trim();
            }

            // Сумма платежа
            if (fields[3].Trim() != "")
            {
                body.SumPlat = Convert.ToDecimal(fields[3].Trim());
            }

            return body;
        }

        public override bool CheckAllPayments()
        {
            bool result = true;

            var sqlBuilder = new StringBuilder();
            foreach (var bank in Points.PointList)
            {
                sqlBuilder.AppendFormat(@"
select fr.nzp_kvit_reestr, kv.nzp_kvar, fr.sum_charge, fr.nzp_serv, s.service_name
from {0}{1}tula_file_reestr fr
left join {3}{1}kvar kv on fr.pkod = kv.pkod
inner join {3}{1}tarif t on t.nzp_kvar = kv.nzp_kvar and t.nzp_serv = fr.nzp_serv and t.dat_po <= cast(fr.date_plat_poruch as date)
left join {3}{2}services s on s.nzp_serv = fr.nzp_serv
where fr.nzp_kvit_reestr={4}
and not exists
(select *
from {3}{1}tarif t
where t.nzp_kvar = kv.nzp_kvar and t.nzp_serv = fr.nzp_serv and
cast(fr.date_plat_poruch as date) BETWEEN t.dat_s and t.dat_po)
and
(not EXISTS (select *
from {3}_charge_{6}.charge_{5} ch
where ch.nzp_kvar = kv.nzp_kvar and ch.nzp_serv = fr.nzp_serv
))
            ", Points.Pref, sDataAliasRest, sKernelAliasRest, bank.pref, _fileArgs.KvitID, _fileArgs.Month.ToString().PadLeft(2, '0'), _fileArgs.Year.ToString().Substring(2));

                var badPayments = ClassDBUtils.OpenSQL(sqlBuilder.ToString(), _connDB).GetData();
                if (badPayments.Rows.Count > 0)
                {
                    result = false;
                }

                foreach (DataRow row in badPayments.Rows)
                {
                    _loadProtokol.AddComment("Платеж по закрытой услуге. ЛС - " + row["nzp_kvar"] + "; сумма -  " + row["sum_charge"] + "; услуга - " + row["service_name"]);
                }

                sqlBuilder.Clear();
            }

            return result;
        }

        /// <summary>
        /// Проверка строк по списку возможных ошибок в заполнении файла
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        public override Returns FindErrors(string[] rows)
        {
            Returns ret = Utils.InitReturns();
            Utils.setCulture(); // установка региональных настроек
            var numRows = new Dictionary<int, string>();

            for (int i = 0; i < rows.Length; i++)
            {
                bool add = false;
                string err = "";

                //проверяем структуру строки на целостность
                string[] rowEl = rows[i].Split(';');
                if (rowEl.Length != 11)
                {
                    add = true;
                    err += (err != ""
                        ? ", Нарушен формат реестра: Неверное число полей в строке"
                        : " Нарушен формат реестра: Неверное число полей в строке");
                }

                //число полей по формату
                if (rowEl.Length == 11)
                {
                    //Код лицевого счета
                    if (!Regex.IsMatch(rowEl[2].Trim(), @"^\d+$") || rowEl[2].Trim().Length > 20)
                    {
                        add = true;
                        err += (err != ""
                            ? ", Нарушен формат поля \"Код лицевого счета\""
                            : "Нарушен формат поля \"Код лицевого счета\"");
                    }

                    //Сумма платежа           
                    if (!Regex.IsMatch(rowEl[3].Trim(), @"^[0-9]+(\.[0-9]+)?$"))
                    {
                        add = true;
                        err += (err != ""
                            ? ", Нарушен формат поля \"Сумма платежа\""
                            : "Нарушен формат поля \"Сумма платежа\"");
                    }
                    else
                    {
                        decimal sumOpl;
                        Decimal.TryParse(rowEl[3].Trim(), out sumOpl);
                        _loadProtokol.TotalSumPack += sumOpl;
                    }

                    //Услуга          
                    if (!Regex.IsMatch(rowEl[4].Trim(), @"^\d+$"))
                    {
                        add = true;
                        err += (err != ""
                            ? ", Нарушен формат поля \"Услуга\""
                            : "Нарушен формат поля \"Услуга\"");
                    }

                    //Дата платёжного поручения
                    if (!Regex.IsMatch(rowEl[8].Trim(), @"^(\d{2})/\d{2}/(\d{4})$"))
                    {
                        add = true;
                        err += (err != ""
                            ? ", Нарушен формат поля \"Дата платёжного поручения\""
                            : "Нарушен формат поля \"Дата платёжного поручения\"");
                    }

                    //Номер платежного поручения 
                    if (!Regex.IsMatch(rowEl[10].Trim(), @"^[0-9]\d*$"))
                    {
                        add = true;
                        err += (err != ""
                            ? ", Нарушен формат поля \"Номер платежного поручения\""
                            : "Нарушен формат поля \"Номер платежного поручения\"");
                    }

                    //остальные поля нас не интересуют
                }

                //добавляем номер строки в список строк с ошибками
                if (add) numRows.Add(i + 1, err);
            }
            if (numRows.Count > 0)
            {
                ret.result = false;
                ret.text = "Номера строк с ошибками:\r\n";
                foreach (var num in numRows)
                {
                    ret.text += "Строка: " + num.Key + ", Ошибки: " + num.Value + ";\r\n";
                }
                ret.text = ret.text.Remove(ret.text.Length - 1, 1);
            }

            return ret;
        }

        /// <summary>
        /// сопоставление указанного в квитанции для реестра кол-ва строк и суммы оплат с данными в файле реестра 
        /// </summary>
        /// <param name="reestrRowsCount"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        override public Returns CheckReestrAtr(int reestrRowsCount, string fileName)
        {
            return new Returns(true, "", 0);
        }

        /// <summary>
        /// Добавление платежа в БД
        /// </summary>
        /// <param name="bodyReestr"></param>
        /// <returns></returns>
        override protected bool InsertPayment(ReestrBody bodyReestr)
        {
            string sqlStr = "INSERT INTO " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr " +
                            " (pkod, nzp_kvit_reestr, sum_charge, transaction_id, nzp_serv, address, date_plat_poruch, nomer_plat_poruch) VALUES (" +
                            bodyReestr.LSKod + ", " + _fileArgs.KvitID + ", " +
                            bodyReestr.SumPlat + ", " +
                            Utils.EStrNull(bodyReestr.TransactionID) + ", " +
                            (bodyReestr.ServiceNumber == 0 ? "null" : bodyReestr.ServiceNumber.ToString()) + ", " +
                            Utils.EStrNull(bodyReestr.Address) + "," +
                            Utils.EStrNull(bodyReestr.PaymentDate) + ", " +
                            Utils.EStrNull(bodyReestr.NomerPlatPoruch) + ")";

            if (!ExecSQL(_connDB, sqlStr, true).result)
            {
                MonitorLog.WriteLog("Ошибка сохранения  " + (Constants.Viewerror ? "\n" + sqlStr : ""), MonitorLog.typelog.Error, 20, 201, true);
                _loadProtokol.AddComment("\nОшибка сохранения данных реестра");
                if (bodyReestr.TransactionID.Length > 30)
                    _loadProtokol.AddComment(
                        ". Превышена длина поля \"Номер транзакции\", по формату " +
                        "длина поля составляет 30 символов");
                return false;
            }

            //            int nzpReestrD = GetSerialValue(_connDB);

            return true;
        }

        protected override string GetNzpCounter(string numCnt, string pkod)
        {
            return null;
        }
    }

    //******************************************************************************************************************************************
    /// <summary>
    /// Класс для разбора строки платежа по реестру для Сбербанка (Тагил)
    /// </summary>
    public class DbReestrFromBankTagilSber : DbReestrFromBankBaikalVstkb
    {
        public DbReestrFromBankTagilSber(IDbConnection connDB, FileNameStruct fileArgs, DbLoadProtokol loadProtokol)
            : base(connDB, fileArgs, loadProtokol)
        {
        }

        public override bool CheckAllPayments()
        {
            return true;
        }

        /// <summary>
        /// Проверка на существование платежа
        /// </summary>
        /// <param name="bodyReestr"></param>
        /// <returns></returns>
        override protected bool IsPaymentExists(ReestrBody bodyReestr)
        {
            bool result = true;

            string sqlStr = " select sum(sum_charge) as sum_charge" +
                            " from " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr " +
                            " where pkod=" + bodyReestr.LSKod + " " +
                            " and transaction_id='" + bodyReestr.TransactionID + "'";

            Returns ret;
            object obj = ExecScalar(_connDB, sqlStr, out ret, true);
            if (obj != null && obj != DBNull.Value)
            {
                result = false;

                if (Convert.ToDecimal(obj) > 0)
                {
                    if (bodyReestr.SumPlat != Convert.ToDecimal(obj))
                    {
                        _loadProtokol.AddComment("Дублированный номер банковской транзакции " +
                                                 " платежный код " + bodyReestr.LSKod + ", номер транзации " + bodyReestr.TransactionID + ", сумма " + bodyReestr.SumPlat);
                        result = true;
                    }

                }
            }

            return result;
        }

        /// <summary>
        /// Заполнение полей реестра из разбитой строки платежа
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        protected override ReestrBody FillBody(string[] fields)
        {
            var body = new ReestrBody();

            // Уникальный номер операции в системе платежного агента
            if (fields[0].Trim() != "")
            {
                body.TransactionID = fields[0].Trim().PadRight(26, '0');
            }

            // Номер платежного поручения
            if (fields[4].Trim() != "")
            {
                body.NomerPlatPoruch = fields[4].Trim();
            }

            // Дата совершения платежа
            if (fields[1].Trim() != "")
            {
                body.PaymentDate = fields[1].Trim().Replace("/", ".");
            }

            // Код ЛС
            if (fields[2].Trim() != "")
            {
                body.LSKod = fields[2].Trim();
            }

            // Дата платежного поручения 
            if (fields[5].Trim() != "")
            {
                body.DatePlatPoruch = fields[5].Trim().Replace("/", ".");
            }

            // Сумма платежа
            if (fields[3].Trim() != "")
            {
                body.SumPlat = Convert.ToDecimal(fields[3].Trim());
            }

            return body;
        }

        /// <summary>
        /// Проверка строк по списку возможных ошибок в заполнении файла
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        public override Returns FindErrors(string[] rows)
        {
            Returns ret = Utils.InitReturns();
            Utils.setCulture(); // установка региональных настроек
            var numRows = new Dictionary<int, string>();

            for (int i = 0; i < rows.Length; i++)
            {
                bool add = false;
                string err = "";

                //проверяем структуру строки на целостность
                string[] rowEl = rows[i].Split(';');
                if (rowEl.Length != 6)
                {
                    add = true;
                    err += (err != ""
                        ? ", Нарушен формат реестра: Неверное число полей в строке"
                        : " Нарушен формат реестра: Неверное число полей в строке");
                }

                //число полей по формату
                if (rowEl.Length == 6)
                {
                    //Код лицевого счета
                    if (!Regex.IsMatch(rowEl[2].Trim(), @"^\d+$") || rowEl[2].Trim().Length > 20)
                    {
                        add = true;
                        err += (err != ""
                            ? ", Нарушен формат поля \"Код лицевого счета\""
                            : "Нарушен формат поля \"Код лицевого счета\"");
                    }

                    //Сумма платежа           
                    if (!Regex.IsMatch(rowEl[3].Trim(), @"^[0-9]+(\.[0-9]+)?$"))
                    {
                        add = true;
                        err += (err != ""
                            ? ", Нарушен формат поля \"Сумма платежа\""
                            : "Нарушен формат поля \"Сумма платежа\"");
                    }
                    else
                    {
                        decimal sumOpl;
                        Decimal.TryParse(rowEl[3].Trim(), out sumOpl);
                        _loadProtokol.TotalSumPack += sumOpl;
                    }

                    //Номер транзакции 
                    if (!Regex.IsMatch(rowEl[0].Trim(), @"\d{26}"))
                    {
                        add = true;
                        err += (err != ""
                            ? ", Нарушен формат поля \"Номер транзакции\""
                            : "Нарушен формат поля \"Номер транзакции\"");
                    }

                    //Номер платежного поручения 
                    if (!Regex.IsMatch(rowEl[4].Trim(), @"^[0-9]\d*$"))
                    {
                        add = true;
                        err += (err != ""
                            ? ", Нарушен формат поля \"Номер платежного поручения\""
                            : "Нарушен формат поля \"Номер платежного поручения\"");
                    }

                    //Дата платёжного поручения
                    if (!Regex.IsMatch(rowEl[5].Trim(), @"^(\d{2}).\d{2}.(\d{4})$"))
                    {
                        add = true;
                        err += (err != ""
                            ? ", Нарушен формат поля \"Дата платёжного поручения\""
                            : "Нарушен формат поля \"Дата платёжного поручения\"");
                    }

                    //Дата приема платежа
                    if (!Regex.IsMatch(rowEl[1].Trim(), @"^(\d{2}).\d{2}.(\d{4})$"))
                    {
                        add = true;
                        err += (err != ""
                            ? ", Нарушен формат поля \"Дата приема платежа\""
                            : "Нарушен формат поля \"Дата приема платежа\"");
                    }
                    //остальные поля нас не интересуют
                }

                //добавляем номер строки в список строк с ошибками
                if (add) numRows.Add(i + 1, err);
            }
            if (numRows.Count > 0)
            {
                ret.result = false;
                ret.text = "Номера строк с ошибками:\r\n";
                foreach (var num in numRows)
                {
                    ret.text += "Строка: " + num.Key + ", Ошибки: " + num.Value + ";\r\n";
                }
                ret.text = ret.text.Remove(ret.text.Length - 1, 1);
            }

            return ret;
        }

        /// <summary>
        /// Добавление платежа в БД
        /// </summary>
        /// <param name="bodyReestr"></param>
        /// <returns></returns>
        override protected bool InsertPayment(ReestrBody bodyReestr)
        {
            string sqlStr = "INSERT INTO " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr " +
                            " (pkod, nzp_kvit_reestr, sum_charge, transaction_id, date_plat_poruch, nomer_plat_poruch, payment_datetime) VALUES (" +
                            bodyReestr.LSKod + ", " + _fileArgs.KvitID + ", " +
                            bodyReestr.SumPlat + ", " +
                            Utils.EStrNull(bodyReestr.TransactionID) + ", " +
                            Utils.EStrNull(bodyReestr.DatePlatPoruch) + ", " +
                            Utils.EStrNull(bodyReestr.NomerPlatPoruch) + ", " +
                            Utils.EStrNull(bodyReestr.PaymentDate) +
                            ")";

            if (!ExecSQL(_connDB, sqlStr, true).result)
            {
                MonitorLog.WriteLog("Ошибка сохранения  " + (Constants.Viewerror ? "\n" + sqlStr : ""), MonitorLog.typelog.Error, 20, 201, true);
                _loadProtokol.AddComment("\nОшибка сохранения данных реестра");
                if (bodyReestr.TransactionID.Length > 30)
                    _loadProtokol.AddComment(
                        ". Превышена длина поля \"Номер транзакции\", по формату " +
                        "длина поля составляет 30 символов");
                return false;
            }

            return true;
        }

    }

    /// <summary>
    /// Класс для сохранения реестра оплат
    /// </summary>
    abstract public class DbSaveReestrBft : DataBaseHead
    {
        protected readonly FileNameStruct _fileArgs;
        protected readonly DbLoadProtokol _loadProtokol;
        protected IDbConnection _connDb;
        protected int _nzpBank;

        public DbSaveReestrBft(IDbConnection connDb, FileNameStruct fileArgs, DbLoadProtokol loadProtokol, int nzpBank)
        {
            _fileArgs = fileArgs;
            _loadProtokol = loadProtokol;
            _connDb = connDb;
            _nzpBank = nzpBank;
        }

        /// <summary>
        /// Управляющая функция
        /// </summary>

        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns SyncLsAndInsertPack(FilesImported finder)
        {
            Returns ret = Utils.InitReturns();

            #region связываем с ЛС из системы

            string sql = " update  " + Points.Pref + sDataAliasRest + "tula_file_reestr " +
                         " set nzp_kvar= " +
                         " (select k.nzp_kvar from " + Points.Pref + sDataAliasRest + "kvar k " +
                         " where k.pkod=" + Points.Pref + sDataAliasRest + "tula_file_reestr.pkod) " +
                         " where nzp_kvit_reestr=" + _fileArgs.KvitID + " ";
            if (!ExecSQL(_connDb, sql, true).result)
            {
                ret.text = "Ошибка сопоставления платежных кодов";
                ret.result = false;
                MonitorLog.WriteLog("Ошибка сопоставления платежных кодов: " + sql, MonitorLog.typelog.Error, true);
                return ret;
            }

            #endregion

            _loadProtokol.SetProcent(40, -999);
            //запись в систему показаний ПУ и оплат 
            ret = InsertPack(finder);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка записи реестра " + _fileArgs.Number + _fileArgs.Month +
                    _fileArgs.Day + _fileArgs.OtdNumber + " в систему: " + ret.text, MonitorLog.typelog.Error, true);
            }

            return ret;
        }


        //запись в систему: пачки, оплаты, показания ПУ
        protected virtual Returns InsertPack(FilesImported finder)
        {
            Returns ret;

            string datPrev = "'" +
                             new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(-1)
                                 .ToShortDateString() + "'"; //предыдущий рассчетный месяц
            string year = (Points.DateOper.Year - 2000).ToString("00");

            string operDay = (new DateTime(Points.DateOper.Year, Points.DateOper.Month, Points.DateOper.Day)).ToString(
                "dd.MM.yyyy");

            //получаем локального пользователя
            /*var db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(_connDb, finder, out ret);
            db.Close();*/

            int nzpUser = finder.nzp_user;

            #region Пачки оплат

            try
            {
                //проверяем кол-во пачек. если >1 создаем суперпачку и связываем с ней остальные пачки
                string sql = " SELECT nomer_plat_poruch, date_plat_poruch " +
                             " FROM " + Points.Pref + sDataAliasRest + "tula_file_reestr " +
                             " WHERE nzp_kvit_reestr=" + _fileArgs.KvitID + "" +
                             " GROUP BY nomer_plat_poruch, date_plat_poruch ";
                var packs = ClassDBUtils.OpenSQL(sql, _connDb).resultData;

                if (packs.Rows.Count > 1)
                {
                    int progress = 30 / packs.Rows.Count;
                    int superPack = SaveSuperPack(year, operDay);

                    for (int i = 0; i < packs.Rows.Count; i++)
                    {
                        ret = SaveOnePack(packs.Rows[i], year, superPack, operDay, nzpUser, datPrev);
                        if (!ret.result) return ret;
                        _loadProtokol.SetProcent(40 + progress * i, (int)StatusWWB.InProcess);
                    }

                }
                else if (packs.Rows.Count == 1)
                {
                    ret = SaveOnePack(packs.Rows[0], year, 0, operDay, nzpUser, datPrev);
                    if (!ret.result) return ret;
                }




                _loadProtokol.SetProcent(70, (int)StatusWWB.InProcess);

                #region Сохранение счетчиков

                var dbSaveReestrCounters = new DbSaveReestrCountersBft(_connDb, _fileArgs, _loadProtokol);
                ret = dbSaveReestrCounters.SavePackCounters(packs, year);

                #endregion
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка в функции InsertPack " + ex,
                    MonitorLog.typelog.Error, true);
                return new Returns(false, "Ошибка при загрузке пачек оплат в режиме взаимодействие с Банком");
            }
            #endregion
            return ret;
        }

        /// <summary>
        /// Сохранение пачки в pack
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="year"></param>
        /// <param name="insertedRowsForPack"></param>
        /// <param name="sumPack"></param>
        /// <param name="operDay"></param>
        /// <returns></returns>
        abstract protected Returns InsertPack(DataRow pack, string year, int insertedRowsForPack, decimal sumPack, string parPack, string operDay);

        /// <summary>
        /// Сохранение одной пачки с платежами
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="year"></param>
        /// <param name="idSuperPack"></param>
        /// <param name="operDay"></param>
        /// <param name="nzpUser"></param>
        /// <param name="datPrev"></param>
        /// <returns></returns>
        protected virtual Returns SaveOnePack(DataRow pack, string year, int idSuperPack,
            string operDay, int nzpUser, string datPrev)
        {
            Returns ret;

            string finAlias = Points.Pref + "_fin_" + year + tableDelimiter;
            decimal sumPack = 0;
            //достаем реальные значения оплаты для пачки, т.к. некоторые записи уже могли быть загружены ранее в период.реестре
            string sql = " SELECT sum(sum_charge) FROM " + Points.Pref + sDataAliasRest + "tula_file_reestr" +
                         " WHERE nzp_kvit_reestr=" + _fileArgs.KvitID +
                         " AND nomer_plat_poruch='" + pack["nomer_plat_poruch"] + "'" +
                         " AND date_plat_poruch='" + pack["date_plat_poruch"] + "'";
            object obj = ExecScalar(_connDb, sql, out ret, true);

            if (obj != null && obj != DBNull.Value)
            {
                sumPack = Convert.ToDecimal(obj);
            }
            else
            {
                MonitorLog.WriteLog("Ошибка получения суммы оплат для пачки: " + sql,
                    MonitorLog.typelog.Error, true);
            }


            //кол-во строк в пачке
            var insertedRowsForPack = 0;
            sql = " SELECT count(*) FROM " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr" +
                  " WHERE nzp_kvit_reestr=" + _fileArgs.KvitID +
                  " AND nomer_plat_poruch='" + pack["nomer_plat_poruch"] + "'" +
                  " AND date_plat_poruch='" + pack["date_plat_poruch"] + "' ";
            obj = ExecScalar(_connDb, sql, out ret, true);

            if (obj != null && obj != DBNull.Value)
            {
                insertedRowsForPack = Convert.ToInt32(obj);
            }
            else
            {
                MonitorLog.WriteLog("Ошибка получения кол-ва строк для пачки: " + sql,
                    MonitorLog.typelog.Error, true);
            }

            string parPack;
            if (idSuperPack == 0) parPack = "NULL";
            else parPack = idSuperPack.ToString(CultureInfo.InvariantCulture);

            //записываем в pack
            ret = InsertPack(pack, year, insertedRowsForPack, sumPack, parPack, operDay);
            if (!ret.result)
            {
                return new Returns(false, "Ошибка записи пачек оплаты");
            }

            var thisPack = GetSerialValue(_connDb);
            _loadProtokol.NzpPack.Add(thisPack);

            //Добавляем оплаты по ЛС
            ret = InsertToPackLs(pack, thisPack, nzpUser, datPrev, finAlias);
            if (!ret.result)
            {
                ret.text = "Ошибка записи оплат по лицевым счетам";
                return ret;
            }

            //записываем не соотнесенные с системой ЛС в pack_ls_err  
            sql = " insert into " + finAlias + "pack_ls_err (nzp_pack_ls, nzp_err, note) " +
                  " select nzp_pack_ls, 666,pkod " +
                  " from  " + finAlias + "pack_ls " +
                  " where nzp_pack= " + thisPack + " and inbasket=1";
            if (!ExecSQL(_connDb, sql, true).result)
            {
                ret.text = "Ошибка записи не сопоставленных лицевых счетов";
                ret.result = false;
                return ret;
            }

            return ret;
        }

        /// <summary>
        /// Добавление оплаты по ЛС
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="packId"></param>
        /// <param name="nzpUser"></param>
        /// <param name="datPrev"></param>
        /// <param name="finAlias"></param>
        /// <returns></returns>
        virtual protected Returns InsertToPackLs(DataRow pack, int packId, int nzpUser, string datPrev, string finAlias)
        {
            var sql = "";
            sql = " insert into " + finAlias + "pack_ls " +
                  " (nzp_pack, num_ls, g_sum_ls, sum_ls, kod_sum,  paysource, id_bill, dat_vvod, info_num, " +
                  " inbasket, alg, unl, incase, pkod, nzp_user,dat_month, transaction_id) " +
                  " select " + packId + ", k1.num_ls, f.sum_charge, 0 as sum_ls, 33, 1 as paysource," +
                  "  0 as id_bill, k.date_plat, " +
                // костыль
                  " (case when length(f.transaction_id) >= 21 then substr(f.transaction_id,21,6) else f.transaction_id end)" + sConvToNum + " as num_oper, " +
                  " (case when f.nzp_kvar is not null then 0 else 1 end) as inbasket, " +
                  " 0 as alg, 0 as unl, 0 as incase, " +
                  " (case when f.pkod is null or f.pkod>9999999999999 then 0 else f.pkod end), " +
                  nzpUser + " ," + datPrev + ", f.transaction_id " +
                  " from " +
                  Points.Pref + "_data" + tableDelimiter + "tula_kvit_reestr k, " +
                  Points.Pref + "_data" + tableDelimiter + "tula_file_reestr f " +
                  " LEFT OUTER JOIN " + Points.Pref + "_data" + tableDelimiter +
                  "kvar k1  on f.nzp_kvar=k1.nzp_kvar  " +
                  " where k.nzp_kvit_reestr=f.nzp_kvit_reestr " +
                  "       and k.nzp_kvit_reestr=" + _fileArgs.KvitID + " " +
                  "       and f.nomer_plat_poruch='" + pack["nomer_plat_poruch"] + "'" +
                  "       and f.date_plat_poruch='" + pack["date_plat_poruch"].ToString().Trim() + "'";
            Returns ret = ExecSQL(_connDb, sql, true);
            return ret;
        }

        /// <summary>
        /// Сохранение cуперпачки в pack
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="year"></param>
        /// <param name="insertedRowsForPack"></param>
        /// <param name="sumPack"></param>
        /// <param name="operDay"></param>
        /// <returns></returns>
        protected Returns InsertSuperPack(string year, decimal sumPack, string operDay)
        {
            string sql = " insert into " + Points.Pref + "_fin_" + year + tableDelimiter + "pack  " +
                  "(pack_type, nzp_bank,num_pack, dat_pack, count_kv, sum_pack, real_count, flag, dat_vvod,  file_name,dat_uchet) " +
                  " select 10, 1000, substr(k.file_name,0,8), k.date_plat, " + _loadProtokol.CountInsertedRows + ", " +
                  sumPack + "," + _loadProtokol.CountInsertedRows + ", 11, " + sCurDateTime + ",k.file_name " +
                  ", '" + operDay + "' " +
                  " from " + Points.Pref + "_data" + tableDelimiter + "tula_kvit_reestr k " +
                  " where k.nzp_kvit_reestr=" + _fileArgs.KvitID;
            return ExecSQL(_connDb, sql, true);
        }

        /// <summary>
        /// Сохранение суперпачки
        /// </summary>
        /// <param name="year"></param>
        /// <param name="operDay"></param>
        /// <returns></returns>
        private int SaveSuperPack(string year, string operDay)
        {
            Returns ret;
            int result = 0;
            decimal sumPack;
            //достаем реальные значения оплаты для пачки, т.к. некоторые записи уже могли быть 
            //загружены ранее в период.реестре
            string sql = " select sum(sum_charge) " +
                         " from " + Points.Pref + sDataAliasRest + "tula_file_reestr " +
                         " where nzp_kvit_reestr=" + _fileArgs.KvitID + "";

            object obj = ExecScalar(_connDb, sql, out ret, true);

            if (obj != null && obj != DBNull.Value)
            {
                sumPack = Convert.ToDecimal(obj);
            }
            else
            {
                MonitorLog.WriteLog("Ошибка получения суммы оплат для суперпачки: " + sql,
                    MonitorLog.typelog.Error, true);
                return result;

            }

            ret = InsertSuperPack(year, sumPack, operDay);
            if (!ret.result)
                throw new UserException("Ошибка записи пачек оплаты");

            result = GetSerialValue(_connDb);


            sql = " update " + Points.Pref + "_fin_" + year + tableDelimiter + "pack " +
                  " set par_pack=" + result + " " +
                  " where nzp_pack=" + result;
            if (!ExecSQL(_connDb, sql, true).result)
                throw new UserException("Ошибка записи пачек оплаты");

            return result;


        }
    }

    public class DbSaveReestrBaikalSocProtectL : DbSaveReestrBft
    {
        public DbSaveReestrBaikalSocProtectL(IDbConnection connDb, FileNameStruct fileArgs, DbLoadProtokol loadProtokol, int nzpBank)
            : base(connDb, fileArgs, loadProtokol, nzpBank)
        {
        }
        protected override Returns InsertPack(DataRow pack, string year, int insertedRowsForPack, decimal sumPack, string parPack, string operDay)
        {
            //записываем в pack
            //            var branch = _fileArgs.FileType == FileNameStruct.ReestrTypes.SvodKvit ? "branch_id" : "branch_id_reestr";
            string sql = " INSERT INTO " + Points.Pref + "_fin_" + year + tableDelimiter + "pack  " +
                  "(pack_type, nzp_bank,num_pack, par_pack , dat_pack, count_kv, " +
                  " sum_pack, real_count, flag, dat_vvod,  file_name,dat_uchet) " +
                  " select 10, " + _nzpBank + ", '" + pack["nomer_plat_poruch"] + "'," +
                  parPack + ",'" + pack["date_plat_poruch"] + "', " + insertedRowsForPack + ", " +
                  +sumPack + "," + insertedRowsForPack + ", 11, " + sCurDateTime + ",k.file_name " +
                  ", '" + operDay + "' " +
                  " from " + Points.Pref + sDataAliasRest + "tula_kvit_reestr k " +
                  " where k.nzp_kvit_reestr=" + _fileArgs.KvitID;
            return ExecSQL(_connDb, sql, true);
        }

    }

    public class DbSaveReestrBaikalSocProtectS : DbSaveReestrBaikalSocProtectL
    {
        public DbSaveReestrBaikalSocProtectS(IDbConnection connDb, FileNameStruct fileArgs, DbLoadProtokol loadProtokol, int nzpBank)
            : base(connDb, fileArgs, loadProtokol, nzpBank)
        {
        }
    }

    public class DbSaveReestrBaikalVstkb : DbSaveReestrBft
    {
        public DbSaveReestrBaikalVstkb(IDbConnection connDb, FileNameStruct fileArgs, DbLoadProtokol loadProtokol, int nzpBank)
            : base(connDb, fileArgs, loadProtokol, nzpBank)
        {
        }

        /// <summary>
        /// Запись в систему: пачки, оплаты
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        protected override Returns InsertPack(FilesImported finder)
        {
            Returns ret;

            string datPrev = "'" +
                             new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(-1)
                                 .ToShortDateString() + "'"; //предыдущий рассчетный месяц
            string year = (Points.CalcMonth.year_ - 2000).ToString("00");

            string operDay = (new DateTime(Points.DateOper.Year, Points.DateOper.Month, Points.DateOper.Day)).ToString(
                "dd.MM.yyyy");

            //получаем локального пользователя
            /*var db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(_connDb, finder, out ret);
            db.Close();*/

            int nzpUser = finder.nzp_user;

            try
            {
                ret = SaveOnePack(null, year, 0, operDay, nzpUser, datPrev);
                if (!ret.result) return ret;
                /*                
                                //проверяем кол-во пачек. если >1 создаем суперпачку и связываем с ней остальные пачки
                                string sql = " SELECT date_plat_poruch " +
                                             " FROM " + Points.Pref + sDataAliasRest + "tula_file_reestr " +
                                             " WHERE nzp_kvit_reestr=" + _fileArgs.KvitID + "" +
                                             " GROUP BY date_plat_poruch ";
                                var packs = ClassDBUtils.OpenSQL(sql, _connDb).resultData;

                                if (packs.Rows.Count > 1)
                                {
                                    int progress = 30 / packs.Rows.Count;
                                    int superPack = SaveSuperPack(year, operDay);

                                    for (int i = 0; i < packs.Rows.Count; i++)
                                    {
                                        ret = SaveOnePack(packs.Rows[i], year, superPack, operDay, nzpUser, datPrev);
                                        if (!ret.result) return ret;
                                        _loadProtokol.SetProcent(40 + progress * i, (int)StatusWWB.InProcess);
                                    }

                                }
                                else if (packs.Rows.Count == 1)
                                {
                                    ret = SaveOnePack(packs.Rows[0], year, 0, operDay, nzpUser, datPrev);
                                    if (!ret.result) return ret;
                                }
                */
                _loadProtokol.SetProcent(70, (int)StatusWWB.InProcess);

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка в функции InsertPack " + ex,
                    MonitorLog.typelog.Error, true);
                return new Returns(false, "Ошибка при загрузке пачек оплат в режиме взаимодействие с Банком");
            }

            return ret;
        }

        /// <summary>
        /// Сохранение одной пачки с платежами
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="year"></param>
        /// <param name="idSuperPack"></param>
        /// <param name="operDay"></param>
        /// <param name="nzpUser"></param>
        /// <param name="datPrev"></param>
        /// <returns></returns>
        protected override Returns SaveOnePack(DataRow pack, string year, int idSuperPack, string operDay, int nzpUser, string datPrev)
        {
            Returns ret;

            string finAlias = Points.Pref + "_fin_" + year + tableDelimiter;
            decimal sumPack = 0;
            //достаем реальные значения оплаты для пачки, т.к. некоторые записи уже могли быть загружены ранее в период.реестре
            string sql = " SELECT sum(sum_charge) FROM " + Points.Pref + sDataAliasRest + "tula_file_reestr" +
                         " WHERE nzp_kvit_reestr=" + _fileArgs.KvitID;
            //                         " AND nomer_plat_poruch='" + pack["nomer_plat_poruch"] + "'" +
            //                         " AND date_plat_poruch='" + pack["date_plat_poruch"] + "'";
            object obj = ExecScalar(_connDb, sql, out ret, true);

            if (obj != null && obj != DBNull.Value)
            {
                sumPack = Convert.ToDecimal(obj);
            }
            else
            {
                MonitorLog.WriteLog("Ошибка получения суммы оплат для пачки: " + sql,
                    MonitorLog.typelog.Error, true);
            }


            //кол-во строк в пачке
            var insertedRowsForPack = 0;
            sql = " SELECT nomer_plat_poruch FROM " + Points.Pref + "_data" + tableDelimiter + "tula_file_reestr" +
                  " WHERE nzp_kvit_reestr=" + _fileArgs.KvitID +
                  " group by nomer_plat_poruch";
            var data = ClassDBUtils.OpenSQL(sql, _connDb).resultData;
            insertedRowsForPack = data.Rows.Count;

            string parPack = idSuperPack == 0 ? "NULL" : idSuperPack.ToString(CultureInfo.InvariantCulture);

            //записываем в pack
            ret = InsertPack(pack, year, insertedRowsForPack, sumPack, parPack, operDay);
            if (!ret.result)
            {
                return new Returns(false, "Ошибка записи пачек оплаты");
            }

            var thisPack = GetSerialValue(_connDb);
            _loadProtokol.NzpPack.Add(thisPack);

            //Добавляем оплаты по ЛС
            ret = InsertToPackLs(pack, thisPack, nzpUser, datPrev, finAlias);
            if (!ret.result){return ret;}

            //записываем не соотнесенные с системой ЛС в pack_ls_err  
            sql = " insert into " + finAlias + "pack_ls_err (nzp_pack_ls, nzp_err, note) " +
                  " select nzp_pack_ls, 666,pkod " +
                  " from  " + finAlias + "pack_ls " +
                  " where nzp_pack= " + thisPack + " and inbasket=1";
            if (!ExecSQL(_connDb, sql, true).result)
            {
                ret.text = "Ошибка записи не сопоставленных лицевых счетов";
                ret.result = false;
                return ret;
            }

            //Добавляем оплаты по ЛС
            ret = InsertPaymentsForLs(pack, thisPack, finAlias, operDay);
            if (!ret.result)
            {
                ret.text = "Ошибка записи расщепленных оплат";
                return ret;
            }

            return ret;
        }

        /// <summary>
        /// Запись пачки
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="year"></param>
        /// <param name="insertedRowsForPack"></param>
        /// <param name="sumPack"></param>
        /// <param name="parPack"></param>
        /// <param name="operDay"></param>
        /// <returns></returns>
        protected override Returns InsertPack(DataRow pack, string year, int insertedRowsForPack, decimal sumPack, string parPack, string operDay)
        {
            //записываем в pack
            //            var branch = _fileArgs.FileType == FileNameStruct.ReestrTypes.SvodKvit ? "branch_id" : "branch_id_reestr";
            string sql = " INSERT INTO " + Points.Pref + "_fin_" + year + tableDelimiter + "pack  " +
                  "(pack_type, nzp_bank,num_pack, par_pack , dat_pack, count_kv, " +
                  " sum_pack, real_count, flag, dat_vvod,  file_name,dat_uchet) " +
                  " select 10, " + _nzpBank + ", '" + _fileArgs.KvitID + "'," +
                  parPack + ",'" + _fileArgs.Day + "." + _fileArgs.Month + "." + _fileArgs.Year + "', " + insertedRowsForPack + ", " +
                  +sumPack + "," + insertedRowsForPack + ", 11, " + sCurDateTime + ",k.file_name " +
                  ", '" + operDay + "' " +
                  " from " + Points.Pref + sDataAliasRest + "tula_kvit_reestr k " +
                  " where k.nzp_kvit_reestr=" + _fileArgs.KvitID;
            return ExecSQL(_connDb, sql, true);
        }

        /// <summary>
        /// Запись оплат по услугам для конкретного ЛС (в gil_sums)
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="packId"></param>
        /// <param name="finAlias"></param>
        /// <param name="operDay"></param>
        /// <returns></returns>
        protected virtual Returns InsertPaymentsForLs(DataRow pack, int packId, string finAlias, string operDay)
        {
            //dat_uchet = operDay
            var sql = "";
            sql = " insert into " + finAlias + "gil_sums " +
                    " (nzp_pack_ls, num_ls, nzp_serv, sum_oplat, dat_month, dat_uchet) " +
                    " select p.nzp_pack_ls, p.num_ls, f.nzp_serv, f.sum_charge, cast(f.date_plat_poruch as date), '" + operDay + "' " +
                    " from " + Points.Pref + sDataAliasRest + "tula_file_reestr f, " + finAlias + "pack_ls p " +
                    " where p.nzp_pack = " + packId +
                    "       and p.num_ls = f.nzp_kvar " +
                    "       and p.transaction_id=f.transaction_id " +
                    "       and f.nzp_kvit_reestr=" + _fileArgs.KvitID;

            return ExecSQL(_connDb, sql, true);
        }

        /// <summary>
        /// Добавление оплаты по ЛС
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="packId"></param>
        /// <param name="nzpUser"></param>
        /// <param name="datPrev"></param>
        /// <param name="finAlias"></param>
        /// <returns></returns>
        protected override Returns InsertToPackLs(DataRow pack, int packId, int nzpUser, string datPrev, string finAlias)
        {
            var sql = "";
            sql = " insert into " + finAlias + "pack_ls " +
                  " (nzp_pack, num_ls, g_sum_ls, sum_ls, kod_sum,  paysource, id_bill, dat_vvod, info_num, " +
                  " inbasket, alg, unl, incase, pkod, nzp_user,dat_month, transaction_id) " +
                  " select " + packId + ", k1.num_ls, sum(f.sum_charge), 0 as sum_ls, 33, 1 as paysource," +
                  "  0 as id_bill, cast(f.date_plat_poruch as date), " +
                // костыль
                //                  " (case when length(f.transaction_id) >= 21 then substr(f.transaction_id,21,6) else f.transaction_id end)" + sConvToNum + " as num_oper, " +
                //                  pack["nomer_plat_poruch"] + " as info_num, " +
                  "cast(f.nomer_plat_poruch as integer) as info_num, " +
                  " (case when f.nzp_kvar is not null then 0 else 1 end) as inbasket, " +
                  " 0 as alg, 0 as unl, 0 as incase, " +
                  " (case when f.pkod is null or f.pkod>9999999999999 then 0 else f.pkod end), " +
                  nzpUser + " , cast(" + datPrev + " as date) as dat_month, f.transaction_id " +
                  " from " +
                  Points.Pref + sDataAliasRest + "tula_kvit_reestr k, " +
                  Points.Pref + sDataAliasRest + "tula_file_reestr f " +
                  " LEFT OUTER JOIN " + Points.Pref + sDataAliasRest + "kvar k1  on f.nzp_kvar=k1.nzp_kvar  " +
                  " where k.nzp_kvit_reestr=f.nzp_kvit_reestr " +
                  "       and k.nzp_kvit_reestr=" + _fileArgs.KvitID + " " +
                //                  "       and f.nomer_plat_poruch='" + pack["nomer_plat_poruch"] + "'" +
                //                  "       and f.date_plat_poruch='" + pack["date_plat_poruch"].ToString().Trim() + "' " +
                  " group by 1, 2, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17";
            Returns ret = ExecSQL(_connDb, sql, true);
            return ret;
        }

    }

    public class DbSaveReestrTagilSber : DbSaveReestrBaikalVstkb
    {
        public DbSaveReestrTagilSber(IDbConnection connDb, FileNameStruct fileArgs, DbLoadProtokol loadProtokol, int nzpBank)
            : base(connDb, fileArgs, loadProtokol, nzpBank)
        {
        }

        /// <summary>
        /// Запись оплат по услугам для конкретного ЛС (в gil_sums)
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="packId"></param>
        /// <param name="finAlias"></param>
        /// <param name="operDay"></param>
        /// <returns></returns>
        protected override Returns InsertPaymentsForLs(DataRow pack, int packId, string finAlias, string operDay)
        {
            // Оплаты приходят не по услугам
            return new Returns(true, "", 0);
        }
    }


    /// <summary>
    /// Класс сохранения счетчиков из оплат от банка
    /// </summary>
    public class DbSaveReestrCountersBft : DbSaveReestrCounters
    {

        public DbSaveReestrCountersBft(IDbConnection connDb, FileNameStruct fileArgs, DbLoadProtokol loadProtokol)
            : base(connDb, fileArgs, loadProtokol)
        {
        }
    }

}
