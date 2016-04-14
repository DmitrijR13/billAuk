using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;

namespace  STCLINE.KP50.DataBase
{
    class DbFtrPaymentsFromBank:DbBasePaymentsFromBank
    {

        /// <summary>
        /// Загрузка реестра оплат
        /// </summary>
        /// <param name="finder">Описатель реестра</param>
        public override Returns UploadReestr(FilesImported finder)
        {

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(connDB, true);
            if (!ret.result)
            {
                return ret;
            }

            Utils.setCulture(); // установка региональных настроек
            _fileArgs = getFileName(finder.saved_name);

            _loadProtokol = new DbLoadProtokol(connDB, _fileArgs);

            ret = SaveKvitReestr(connDB, finder);
            if (!ret.result)
            {
                ret.text = "\nОшибка при сохранении файла";
                return ret;
            }

            //получаем массив строк из файла
            string[] reestrStrings = ReadReestrFile(finder.ex_path);

            //удаляем промежуточный файл на хосте
            if (InputOutput.useFtp) File.Delete(finder.ex_path);

            _fileArgs.RowsCount = finder.count_rows = reestrStrings.Length;
            DbFtrReestrFromBank reestr = null;
            string backTransaction = string.Empty;

            //запись в реестр загрузок
            InsertIntoTulaDownloaded(connDB, finder.saved_name, _fileArgs, finder);
            _loadProtokol.SetProcent(0, (int)StatusWWB.InProcess);

            if (Regex.Replace(reestrStrings[0], "[0-9А-яЁё|.#_№@*,a-zA-Z;\n\r-!:-]", "").
                 Replace("/", "").
                 Replace(@"\", "").
                 Trim().Length != 0)
            {
                _loadProtokol.AddComment(" Кодировка файла не соответствует WIN-1251");
                ret = new Returns(false, " Кодировка файла не соответствует WIN-1251");
                connDB.Close();
                return ret;
            }

            try
            {
                if (_fileArgs.FileType == FileNameStruct.ReestrTypes.SvodKvit)
                {
                    var reestKvit = new DbReestKvit(connDB);
                    ret = reestKvit.Parse(reestrStrings, _fileArgs);
                }
                else if (_fileArgs.FileType == FileNameStruct.ReestrTypes.Svod)
                {
                    //Разбор реестра
                    if (finder.upload_format == (int) FilesImported.UploadFormat.Tula)
                    {
                        reestr = new DbFtrReestrFromBank(connDB, _fileArgs, _loadProtokol);
                        //выполняем проверки и получаем контрольную сумму оплат
                        ret = reestr.FindErrors(reestrStrings);
                        if (!ret.result)
                        {
                            return ret;
                        }

                        ////выполняем поиск соответствующей квитанции для реестра
                        //if (_fileArgs.FileType != FileNameStruct.ReestrTypes.SvodKvit)
                        if (!GetKvitId(connDB, finder))
                        {
                            ret.result = false;
                            ret.text = "\nНе найдена соответствующая квитанция (квитанция с названием:\'" + finder.saved_name + "\' и " +
                                       " контрольной суммой: " + _loadProtokol.TotalSumPack + " )";
                            return ret;
                        }

                        //разбираем файл 
                        ret = reestr.ParseReestr(finder, reestrStrings);
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
                    DbFtrSaveReestr saveReestr = new DbFtrSaveReestr(connDB, _fileArgs, _loadProtokol, 0, DbFtrReestrFromBank.paymentTempTable, DbFtrReestrFromBank.countersTempTable);
                    //записываем данные в систему: pack,pack_ls,pu_vals 
                    if (!saveReestr.SyncLsAndInsertPack(finder).result)
                    {
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка записи пачки оплат в систему",
                            MonitorLog.typelog.Error, 20, 201, true);
                        ret.text += "\nОшибка при записи данных в систему. " + ret.text;
                    }
                }
                if (ret.result)
                {
                    _loadProtokol.SetProcent(100, (int) StatusWWB.Success);

                    if (_fileArgs.FileType == FileNameStruct.ReestrTypes.Period || _fileArgs.FileType == FileNameStruct.ReestrTypes.Svod)
                        PackDist(_loadProtokol.NzpPack, finder); //распределение пачек                
                }
            }
            catch (UserException ex)
            {
                ret.result = false;
                MonitorLog.WriteException("Ошибка загрузки реестра", ex);
                _loadProtokol.AddComment(ex.Message);
                RollbackReestrTula(connDB, finder, _fileArgs.NzpDownload);
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteException("Ошибка загрузки реестра", ex);
                _loadProtokol.AddComment("Ошибка загрузки реестра, см. логи");
                RollbackReestrTula(connDB, finder, _fileArgs.NzpDownload);

            }
            finally
            {
                if (reestr!=null) reestr.DropTempTables();
                if (connDB != null) connDB.Close();
            }

            

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
        /// Заполнение файловой структуры
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <returns></returns>
        override public FileNameStruct getFileName(string fileName)
        {
            var name = new FileNameStruct();
            string[] fileN = fileName.Split('.');
            try
            {
                name.Number = fileN[0].Substring(0, 5);
                name.Month = int.Parse(fileN[0].Substring(5, 1), NumberStyles.AllowHexSpecifier);
                name.Day = Convert.ToInt32(fileN[0].Substring(6, 2));

                switch (fileN[1].Substring(0, 1))
                {
                    case "K":
                        {
                            name.FileType = FileNameStruct.ReestrTypes.SvodKvit;
                            name.OtdNumber = fileN[1].Substring(1);
                        } break;

                    case "0":
                        {
                            name.FileType = FileNameStruct.ReestrTypes.Svod;
                            name.OtdNumber = fileN[1].Substring(1);
                        } break;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка разбора имени файла: " + ex.Message, MonitorLog.typelog.Error, true);
            }
            return name;
        }

        /// <summary>
        /// Сохранить файл
        /// </summary>
        /// <param name="finder"></param>
        /// <returns>Returns</returns>
        override protected Returns SaveKvitReestr(IDbConnection _connDB, FilesImported finder)
        {
            return new Returns(true, "");
        }

        /// <summary>
        /// Проверка файла перед загрузкой
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        override public FilesImported FastCheck(FilesImported finder, out Returns ret)
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

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result)
            {
                return finder;
            }
            try
            {
                //проверка на существование такого же загруженного файла
                if (!ExistFile(connDB, finder.saved_name))
                {
                    ret.text = "\nФайл с таким именем в этом году уже был загружен";
                    ret.result = false;
                    ret.tag = 996;
                    if (InputOutput.useFtp) File.Delete(finder.ex_path);
                    return finder;
                }


                FileNameStruct fileArgs = getFileName(finder.saved_name);
                if (fileArgs.FileType == 0)
                {
                    ret.text = "Расширение файла недопустимо для этого банка.";
                    ret.result = false;
                    if (InputOutput.useFtp) File.Delete(finder.ex_path);
                    return finder;
                }

                //проверка на существование отделения банка
                ret = CheckBank(connDB, fileArgs);
                if (!ret.result)
                {
                    if (ret.tag == 999) // предложить создать нового платежного агента
                    {
                        ret.text = NotExistBranch;
                        ret.sql_error = fileArgs.OtdNumber.ToUpper();
                    }
                    return finder;
                }


                if (fileArgs.FileType == FileNameStruct.ReestrTypes.Period || fileArgs.FileType == FileNameStruct.ReestrTypes.Svod)
                    //проверка на существование квитанции для реестра
                    if (!CheckReestrName(connDB, finder.saved_name, fileArgs))
                    {
                        ret.result = false;
                        ret.tag = 995;
                        ret.text = "\nДля файла итогового реестра не найдена квитанция";
                        return finder;
                    }
            }
            finally
            {
                connDB.Close();
            }

            return finder;
        }
    }
}
