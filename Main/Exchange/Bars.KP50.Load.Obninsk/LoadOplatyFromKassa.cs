using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Bars.KP50.DataImport.CHECK;
using FastReport;
using Newtonsoft.Json;
using Npgsql;
using STCLINE.KP50;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;

namespace Bars.KP50.Load.Obninsk
{
    public class LoadOplatyKassa : BaseSqlLoad
    {
        
        protected override void PrepareParams()
        {

        }

        protected override NameValueCollection GetNameValueCollection(FilesImported finder)
        {
            DateTime? date_load;
            if (finder.date == null)
            {
                CalcMonthParams prm = new CalcMonthParams(Points.GetPref(finder.nzp_wp));
                RecordMonth rm = Points.GetCalcMonth(prm);
                date_load = new DateTime(rm.year_, rm.month_, 1);
            }
            else
            {
                date_load = finder.date;
            }

            return new NameValueCollection
            {
                {
                    "SystemParams", JsonConvert.SerializeObject(new
                    {
                        NzpUser = finder.nzp_user,
                        NzpExcelUtility = 2,
                        UserLogin = finder.webLogin,
                        PathForSave = finder.ex_path,
                        UserFileName = finder.saved_name,
                        SimpLdTypeFile = finder.SimpLdFileType,
                        uploadFormat = finder.upload_format,
                        DateLoad = date_load.Value.ToString("dd.MM.yyyy")
                    })
                },
                {
                    "UserParamValues", JsonConvert.SerializeObject(new
                    {
                        Test = "test"
                    })
                }
            };
        }

        protected override void InsertReestr()
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
                            "(file_name, nzp, month_, year_, " +
                            " created_by, created_on, tip, download_status ) " +
                            "VALUES " +
                            " ( '" + FileName + "'," +
                            0 + "," + DateLoad.Month + "," + DateLoad.Year + "," +
                            ReportParams.User.nzp_user + ", " + DBManager.sCurDateTime + ", " + (int)SimpLoadTypeFile + ","+(int)DownloadStatuses.InProgress+" )";

            ExecSQL(sqlStr);
            NzpLoad = GetSerialValue();
        }

        public override string Name
        {
            get { return "Загрузка оплат из кассы"; }
        }

        public override string Description
        {
            get { return "Загрузка оплат из кассы"; }
        }

        protected override byte[] Template
        {
            get { return null; }
        }

        public override List<Report.UserParam> GetUserParams()
        {
            return null;
        }

        public override void LoadData()
        {
            CultureInfo ci = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            ci.NumberFormat.NumberDecimalSeparator = ".";
            Thread.CurrentThread.CurrentCulture = ci;
            if (UploadFormat == (int) FilesImported.UploadFormat.TxtGeneral)
            {
                FileStream fs = new FileStream(TemporaryFileName, FileMode.Open, FileAccess.Read);
                var buffer = new byte[fs.Length];
                fs.Position = 0;
                fs.Read(buffer, 0, buffer.Length);
                var extension = System.IO.Path.GetExtension(TemporaryFileName);
                if (extension != null && extension.ToLower().Trim() == ".7z")
                {
                    fs = DecriptFilePack(fs, TemporaryFileName);
                }
                var buf = new byte[3]; // Кол-во записей: 3 байтa
                fs.Position = 0;
                fs.Read(buf, 0, 3);
                if ((System.Text.Encoding.ASCII.GetString(buf) == "***") ||
                    (System.Text.Encoding.ASCII.GetString(buf) == "###")) //Универсальный формат
                {
                    var memstr = new MemoryStream();
                    var buffers = new byte[fs.Length]; // Кол-во записей: 3 байтa
                    fs.Position = 0;
                    fs.Read(buffers, 0, buffers.Length);
                    memstr.Write(buffers, 0, buffers.Length);
                    DbPack db = new DbPack();
                    // объект для сбора информации (ошибок, предупреждений, количесва вставленных строк и т.д.) в процессе загрузки пачек
                    AddedPacksInfo packsInfo= new AddedPacksInfo();
                    // подписка на событие простановки прогресса
                    packsInfo.PackLoadProgress += new DBMyFiles().SetFileProgress;
                    packsInfo.Nzp = NzpExcelUtility;
                    // загрузка пачек
                    DataTable errdt = db.LoadUniversalFormat(memstr, FileName, packsInfo);
                    // количество вставленных строк
                    Protokol.CountInsertedRows = packsInfo.InsertedCountRows;
                    // Номер пачки
                    Nzp = packsInfo.InsertedNzpPack;
                    // Добавить полученные предупреждения в таблицы, используемые в протоколе
                    addErrorAndWarningMsgToProtokol(errdt);
                }
                else
                {
                    Protokol.CountInsertedRows = 0;
                    Protokol.AddUnrecognisedRow("Файл " + FileName +
                                        " не является пачкой оплат в установленном формате версии");
                }
                fs.Close();
            }
            else if (UploadFormat == (int) FilesImported.UploadFormat.Dbf)
            {
                DataTable dtErr = LoadDBFFile();
                addErrorAndWarningMsgToProtokol(dtErr);
            }
            else
            {
                Protokol.CountInsertedRows = 0;
                Protokol.AddUnrecognisedRow("Файл " + FileName +
                                            " не является пачкой оплат в установленном формате версии");
            }
        }

        private void addErrorAndWarningMsgToProtokol(DataTable errdt)
        {
            if (errdt != null && errdt.Rows.Count != 0)
            {
                foreach (DataRow row in errdt.Rows)
                {
                    if (row.Field<int>("number_string") == (int)DownloadMessageTypes.Warning)
                    {
                        Protokol.AddUnrecognisedRow(row.Field<string>("mes"), row.Field<string>("bank"));
                    }
                    else
                    {
                        Protokol.AddUncorrectedRow(row.Field<string>("mes"), row.Field<string>("bank"));
                    }
                }
            }
        }

        public override string GetProtocolName()
        {
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();
            if (!ret.result)
            {
                return String.Empty;
            }

            #region Формирование протокола
            var myFile = new DBMyFiles();
            String fileName = "protocol_opl_kassa_" + DateTime.Now.ToShortDateString().Replace(".", "_") + "_" +
                          DateTime.Now.ToLongTimeString().Replace(":", "_") + ".xlsx";
            string statusName = "Успешно";
            int download_status = (int)DownloadStatuses.Success;
            if (Protokol.CountInsertedRows == 0)
            {
                statusName = "Не загружен";
                download_status = (int)DownloadStatuses.NotLoaded;
                if (Protokol.UnrecognizedRows.Rows.Count == 0 && Protokol.Comments.Rows.Count == 0 && Protokol.UncorrectRows.Rows.Count == 0)
                {
                    Protokol.AddComment("Файл " + FileName +
                                      " не является пачкой оплат в установленном формате ");
                }
                myFile.SetFileState(new ExcelUtility
                {
                    nzp_exc = NzpExcelUtility,
                    status = ExcelUtility.Statuses.Failed,
                    exc_path = fileName
                });
            }
            else
            {
                ExcelUtility.Statuses excelStatus= ExcelUtility.Statuses.Success;
                if (Protokol.UnrecognizedRows.Rows.Count > 0 || Protokol.Comments.Rows.Count > 0)
                {
                    statusName = "Загружено с ошибками";
                    download_status = (int)DownloadStatuses.LoadedWithErrors;

                }
                if (Protokol.UncorrectRows.Rows.Count > 0)
                {
                    excelStatus = ExcelUtility.Statuses.Failed;
                    statusName = "Загружено с ошибками";
                    download_status = (int)DownloadStatuses.LoadedWithErrors;
                }
                myFile.SetFileState(new ExcelUtility
                {
                    nzp_exc = NzpExcelUtility,
                    status = excelStatus,
                    exc_path = fileName
                });
            }

            #region Если все успешно загружено, то формируем должников с делами, которые оплатили

            DataTable debt;
            var debtComment = DebtLsForReport(download_status, out debt);
            #endregion

            var rep = new FastReport.Report();
            try
            {
                var env = new EnvironmentSettings();
                env.ReportSettings.ShowProgress = false;
                DataSet fDataSet = new DataSet();
                fDataSet.Tables.Add(Protokol.UnrecognizedRows);
                fDataSet.Tables.Add(Protokol.Comments);
                fDataSet.Tables.Add(Protokol.UncorrectRows);
                fDataSet.Tables.Add(debt);
                string template = PathHelper.GetReportTemplatePath("protokol_load_opl.frx");
                rep.Load(template);
                rep.RegisterData(fDataSet);
                rep.GetDataSource("comment").Enabled = true;
                rep.GetDataSource("unrecog").Enabled = true;
                rep.GetDataSource("uncorrect").Enabled = true;
                rep.GetDataSource("debt").Enabled = true;
                rep.SetParameterValue("status", statusName);
                rep.SetParameterValue("count_rows", Protokol.CountInsertedRows);
                rep.SetParameterValue("file_name", FileName);
                rep.SetParameterValue("debt_comment", debtComment);
                rep.Prepare();
                var exportXls = new FastReport.Export.OoXML.Excel2007Export();
                exportXls.ShowProgress = false;
                MonitorLog.WriteLog(fileName, MonitorLog.typelog.Info, 20, 201, true);

                if (!Directory.Exists(Constants.Directories.ReportDir))
                {
                    Directory.CreateDirectory(Constants.Directories.ReportDir);
                }
                exportXls.Export(rep, Path.Combine(Constants.Directories.ReportDir, fileName));
                //перенос  на ftp сервер
                if (InputOutput.useFtp)
                {
                    fileName = InputOutput.SaveOutputFile(STCLINE.KP50.Global.Constants.Directories.ReportDir + fileName);
                }
                ProtocolFileName = STCLINE.KP50.Global.Constants.Directories.ReportDir + fileName;
            }
            catch (Exception ex)
            {
                myFile.SetFileState(new ExcelUtility
                {
                    nzp_exc = NzpExcelUtility,
                    status = ExcelUtility.Statuses.Failed,
                    exc_path = fileName
                });
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {

                ExecSQL("UPDATE " + Points.Pref + DBManager.sDataAliasRest + "simple_load " +
                        " SET nzp_exc=" + NzpExcelUtility + ", download_status = " + download_status + ", temp_file='" + TemporaryFileName + "'," +
                        "nzp=" + Nzp +
                        " WHERE nzp_load=" + NzpLoad);
                rep.Dispose();
            }
            #endregion
            return ProtocolFileName;
        }

        /// <summary>
        /// собирает данные по лс с делами в пс должники, совершившим платеж
        /// </summary>
        /// <param name="download_status"></param>
        /// <param name="debt"></param>
        /// <returns></returns>
        private string DebtLsForReport(int download_status, out DataTable debt)
        {
            string debtComment = "";
            debt = new DataTable {TableName = "debt"};
            debt.Columns.Add("bank", typeof (string));
            debt.Columns.Add("fio", typeof (string));
            debt.Columns.Add("adres", typeof (string));
            debt.Columns.Add("sum_money", typeof (decimal));
            debt.Columns.Add("sum_debt", typeof (decimal));
            if (download_status == (int) DownloadStatuses.Success)
            {
                string sqlStr =
                    " SELECT * " +
                    " FROM " + Points.Pref + "_fin_" + (Points.DateOper.Year%100).ToString("00") + DBManager.tableDelimiter +
                    "pack " +
                    " WHERE nzp_pack = " + Nzp;
                DataTable dtSuperPack = ExecSQLToTable(sqlStr);
                if (dtSuperPack.Rows.Count != 1)
                {
                    debtComment += "Ошибка поиска пачки при формировании отчета по ЛС с делами - найдена не ровно одна пачка";
                }

                DataTable dtPack;
                if (dtSuperPack.Rows[0]["par_pack"] != null && Convert.ToInt32(dtSuperPack.Rows[0]["par_pack"]) == Nzp)
                {
                    //суперпачка
                    sqlStr =
                        " SELECT sp.point, d.debt_money, pl.g_sum_ls, k.fio, " +
                        " trim(r.rajon)||' ул.'||trim(u.ulica)||' д.'||trim(dom.ndom)||'/'||trim(dom.nkor)||' кв.'||trim(k.nkvar) as adres " +
                        " FROM " + Points.Pref + DBManager.sDataAliasRest + "dom dom," +
                        Points.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                        Points.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                        Points.Pref + DBManager.sDataAliasRest + "kvar k, " +
                        Points.Pref + DBManager.sKernelAliasRest + "s_point sp, " +
                        Points.Pref + "_debt" + DBManager.tableDelimiter + "deal d, " +
                        Points.Pref + "_fin_" + (Points.DateOper.Year%100).ToString("00") + DBManager.tableDelimiter +
                        "pack p, " +
                        Points.Pref + "_fin_" + (Points.DateOper.Year%100).ToString("00") + DBManager.tableDelimiter +
                        "pack_ls pl " +
                        " WHERE d.nzp_deal_status > 1 AND pl.num_ls = k.num_ls" +
                        " AND k.nzp_dom = dom.nzp_dom AND dom.nzp_ul = u.nzp_ul" +
                        " AND r.nzp_raj = u.nzp_raj and k.nzp_kvar = d.nzp_kvar" +
                        " AND sp.bd_kernel = d.pref AND pl.nzp_pack = p.nzp_pack" +
                        " AND p.par_pack = " + Nzp +
                        " ORDER BY point, adres";
                    dtPack = ExecSQLToTable(sqlStr);
                }
                else
                {
                    //обычная пачка
                    sqlStr =
                        " SELECT sp.point, d.debt_money, pl.g_sum_ls, k.fio, " +
                        " trim(r.rajon)||' ул.'||trim(u.ulica)||' д.'||trim(dom.ndom)||'/'||trim(dom.nkor)||' кв.'||trim(k.nkvar) as adres " +
                        " FROM " + Points.Pref + DBManager.sDataAliasRest + "dom dom," +
                        Points.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                        Points.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                        Points.Pref + DBManager.sDataAliasRest + "kvar k, " +
                        Points.Pref + DBManager.sKernelAliasRest + "s_point sp, " +
                        Points.Pref + "_debt" + DBManager.tableDelimiter + "deal d, " +
                        Points.Pref + "_fin_" + (Points.DateOper.Year%100).ToString("00") + DBManager.tableDelimiter +
                        "pack_ls pl " +
                        " WHERE d.nzp_deal_status > 1 AND pl.num_ls = k.num_ls" +
                        " AND k.nzp_dom = dom.nzp_dom AND dom.nzp_ul = u.nzp_ul" +
                        " AND r.nzp_raj = u.nzp_raj and k.nzp_kvar = d.nzp_kvar" +
                        " AND sp.bd_kernel = d.pref AND pl.nzp_pack = " + Nzp +
                        " ORDER BY point, adres";
                    dtPack = ExecSQLToTable(sqlStr);
                }

                foreach (DataRow row in dtPack.Rows)
                {
                    debt.Rows.Add(row["point"], row["fio"], row["adres"], row["g_sum_ls"], row["debt_money"]);
                }
            }
            return debtComment;
        }

        public System.IO.FileStream DecriptFilePack(System.IO.FileStream fs, string filename)
        {
            string ExtractDirectory = Path.Combine(Constants.Directories.FilesDir,
                Path.GetFileNameWithoutExtension(filename));
            string[] files = Archive.GetInstance(ArchiveFormat.SevenZip)
                .Decompress(fs, ExtractDirectory, "WorkOnlyWithCentralBank");
            FileStream newfs = null;
            if (files.Length > 2)
            {
                Directory.Delete(ExtractDirectory, true);
                throw new Exception("В архиве допускается не более 1 файла с данными помимо файлас с прочими платежами");
            }
            if (files[0].IndexOf("Prochie", StringComparison.Ordinal) < 0)
                newfs = new FileStream(Path.Combine(ExtractDirectory, files[0]), FileMode.Open, FileAccess.ReadWrite);
            else Directory.Delete(ExtractDirectory, true);
            fs.Close();
            fs.Dispose();

            return newfs;
        }
        #region Загрузка DBF файла
        /// <summary>
        ////Загрузка  DBF файла
        /// </summary>
        /// <returns>таблица с ошибками</returns>
        private DataTable LoadDBFFile()
        {
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();
            DataTable dtErr= new DataTable();
            dtErr.Columns.Add("number_string", typeof(int));
            dtErr.Columns.Add("mes", typeof(string));
            dtErr.Columns.Add("bank", typeof(string));
            // преобразование в datatable
            DataTable dataTable = ConvertDBFtoDataTable(new FileStream(TemporaryFileName, FileMode.Open, FileAccess.Read), out ret);
            if (!ret.result)
            {
                dtErr.Rows.Add((int)DownloadMessageTypes.Error, ret.text);
                return dtErr;
            }
            dataTable.Columns.Remove("ADR");
            var finder = new Finder { nzp_user = Nzp_user };
            DbPackClient dbPack = new DbPackClient();
            // сохранение пачек в кэш
            ret = dbPack.UploadDBFPacktoCache(finder, dataTable);
            if (!ret.result || ret.tag < 0)
            {
                dtErr.Rows.Add((int)DownloadMessageTypes.Error, ret.text);
                return dtErr;
            }
            AddedPacksInfo packsInfo;
            DbPack dbpack = new DbPack();
            // сохраненеие  в fin_xx
            ret = dbpack.UploadPackFromDBF(Nzp_user.ToString(), FileName, out packsInfo);
            // количество вставленных строк
            Protokol.CountInsertedRows = packsInfo.InsertedCountRows;
            // номер пачки
            Nzp = packsInfo.InsertedNzpPack;
            dbpack.Close();
            if (!ret.result || ret.tag < 0)
            {
                dtErr.Rows.Add((int)DownloadMessageTypes.Error, ret.text);
                return dtErr;
            }
            // ошибки и предупреждения в процессе загрузки
            packsInfo.InsertErrMsg(dtErr);
            packsInfo.InsertWarnMsg(dtErr);
            return dtErr;
        }
        private DataTable ConvertDBFtoDataTable(System.IO.Stream fs, out Returns ret)
        {
            ret = STCLINE.KP50.Global.Utils.InitReturns();
            //читаем DBF - файл
            //  FileStream fs = null;
            //Описание формата http://www.hardline.ru/3/36/687/
            //http://articles.org.ru/docum/dbfall.php
            //FoxBASE+/dBASE III +, без memo - 0х03
            var dt = new DataTable();
            try
            {
                //fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
                var buffer = new byte[4]; // Кол-во записей: 4 байтa, начиная с 5-го
                fs.Position = 4;
                fs.Read(buffer, 0, buffer.Length);
                int rowsCount = buffer[0] + (buffer[1] * 0x100) + (buffer[2] * 0x10000) + (buffer[3] * 0x1000000);
                buffer = new byte[2]; // Кол-во полей: 2 байтa, начиная с 9-го
                fs.Position = 8;
                fs.Read(buffer, 0, buffer.Length);
                int fieldCount = (((buffer[0] + (buffer[1] * 0x100)) - 1) / 32) - 1;
                var fieldName = new string[fieldCount]; // Массив названий полей
                var fieldType = new string[fieldCount]; // Массив типов полей
                var fieldSize = new byte[fieldCount]; // Массив размеров полей
                var fieldDigs = new byte[fieldCount]; // Массив размеров дробной части
                buffer = new byte[32 * fieldCount]; // Описание полей: 32 байтa * кол-во, начиная с 33-го
                fs.Position = 32;
                fs.Read(buffer, 0, buffer.Length);
                int fieldsLength = 0;
                for (int i = 0; i < fieldCount; i++)
                {
                    // Заголовки
                    fieldName[i] = System.Text.Encoding.Default.GetString(buffer, i * 32, 10).TrimEnd(new[] { (char)0x00 });
                    fieldType[i] = "" + (char)buffer[i * 32 + 11];
                    fieldSize[i] = buffer[i * 32 + 16];
                    fieldDigs[i] = buffer[i * 32 + 17];
                    fieldsLength = fieldsLength + fieldSize[i];
                    // Создаю колонки
                    DataColumn col;
                    switch (fieldType[i])
                    {
                        case "L": dt.Columns.Add(fieldName[i], typeof(bool)); break;
                        case "D": dt.Columns.Add(fieldName[i], typeof(DateTime)); break;
                        case "N":
                            {
                                if (fieldDigs[i] == 0)
                                    dt.Columns.Add(fieldName[i], typeof(int));
                                else
                                {
                                    //dt.Columns.Add(FieldName[i], Type.GetType("System.Decimal"));
                                    col = new DataColumn(fieldName[i], typeof(decimal));
                                    col.ExtendedProperties.Add("precision", fieldSize[i]);
                                    col.ExtendedProperties.Add("scale", fieldDigs[i]);
                                    col.ExtendedProperties.Add("length", fieldSize[i] + fieldDigs[i]);
                                    dt.Columns.Add(col);
                                }
                                break;
                            }
                        case "F": dt.Columns.Add(fieldName[i], typeof(double)); break;
                        default: //dt.Columns.Add(FieldName[i], Type.GetType("System.String")); 
                            col = new DataColumn(fieldName[i], typeof(string)) { MaxLength = fieldSize[i] };
                            dt.Columns.Add(col);
                            break;
                    }
                }
                fs.ReadByte(); // Пропускаю разделитель схемы и данных
                var dfi = new CultureInfo("ru-RU", false).DateTimeFormat;
                var nfi = new CultureInfo("ru-RU", false).NumberFormat;
                nfi.NumberDecimalSeparator = ".";
                nfi.CurrencyDecimalSeparator = ".";


                buffer = new byte[fieldsLength];
                dt.BeginLoadData();

                int delPriznak = fs.ReadByte();

                for (int j = 0; j < rowsCount; j++)
                {

                    if ((delPriznak == 0) & (j == 0))
                        delPriznak = fs.ReadByte(); // Пропускаю стартовый байт элемента данных

                    if (j > 0)
                        delPriznak = fs.ReadByte(); // Пропускаю стартовый байт элемента данных


                    fs.Read(buffer, 0, buffer.Length);
                    DataRow r = dt.NewRow();
                    int index = 0;



                    for (int i = 0; i < fieldCount; i++)
                    {
                        string l = System.Text.Encoding.GetEncoding(
                            CultureInfo.CurrentCulture.TextInfo.OEMCodePage).GetString(buffer, index,
                                                    fieldSize[i]).TrimEnd(new[] { (char)0x00 }).TrimEnd(new[] { (char)0x20 });
                        index = index + fieldSize[i];

                        if (l != "")
                            switch (fieldType[i])
                            {
                                case "L": r[i] = l == "T"; break;
                                case "D": r[i] = DateTime.ParseExact(l, "yyyyMMdd", dfi); break;
                                case "N":
                                    {
                                        if (fieldDigs[i] == 0)
                                            r[i] = int.Parse(l, nfi);
                                        else
                                            r[i] = decimal.Parse(l.Trim(), nfi);
                                        break;
                                    }
                                case "F": r[i] = double.Parse(l.Trim(), nfi); break;
                                default: r[i] = l;

                                    break;
                            }
                        else
                            r[i] = DBNull.Value;
                    }
                    if (delPriznak == 32)
                        dt.Rows.Add(r);
                }
                dt.EndLoadData();
                fs.Close();
                return dt;
            }
            catch (Exception e)
            {
                ret.result = false;
                ret.text= "Ошибка формата пачки! Ожидается файл DBF (FoxBASE+/dBASE III +)" +
                             " с наличием обязательных полей (PREDPR, PLDAT, NPLP, KODLS, GEU, DT, PLATA, MES_OPL ";
                MonitorLog.WriteLog(ret.text+ " " + e.Message, MonitorLog.typelog.Error, true);
                return null;
            }
        }
        #endregion
    }
}
