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
    /// Загрузка реестра из банка для Байкальска (ВСТКБ)
    /// </summary>
    public class DbPaymentsFromBankIssrpF112 : DbBasePaymentsFromBankBft
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


    //******************************************************************************************************************************************
    /// <summary>
    /// Класс для разбора строки платежа по реестру для ВСТКБ (Байкальск)
    /// </summary>
    public class DbReestrFromBankIssrpF112 : DbBaseReestrFromBankBft
    {
        public DbReestrFromBankIssrpF112(IDbConnection connDB, FileNameStruct fileArgs, DbLoadProtokol loadProtokol)
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
            IDataReader reader = null;
            string nzp_coutner = "";

            try
            {
                string sql = "select pref, nzp_kvar from " + Points.Pref + "_data" + DBManager.tableDelimiter + "kvar where pkod = " + pkod;
                Returns ret = new Returns(true);

                ret = ExecRead(_connDB, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                if (reader.Read())
                {
                    string nzp_kvar = reader["nzp_kvar"].ToString();
                    string pref = reader["pref"].ToString().Trim();
                    string num_cnt = numCnt.Replace(" ", "_");

                    sql = "select nzp_counter from " + pref + "_data" + DBManager.tableDelimiter + "counters_spis " +
                        " where nzp = " + nzp_kvar +
                        " and nzp_type = 3 " +
                        " and num_cnt like '%" + num_cnt + "'";

                    DataTable resTable = ClassDBUtils.OpenSQL(sql, _connDB).GetData();
                    if (resTable.Rows.Count != 1) throw new Exception("Загрузка данных от платежных агентов. Не удалось однозначно определить код ПУ");
                    else
                    {
                        nzp_coutner = resTable.Rows[0][0].ToString();
                    }

                    resTable.Dispose();
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка определения кода ПУ  " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);

                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                    reader = null;
                }

                nzp_coutner = "";
            }
            finally
            {
                reader.Close();
                reader.Dispose();
                reader = null;
            }

            return nzp_coutner;
        }
    }


    public class DbSaveReestrIssrpF112 : DbSaveReestrBft
    {
        public DbSaveReestrIssrpF112(IDbConnection connDb, FileNameStruct fileArgs, DbLoadProtokol loadProtokol, int nzpBank)
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



}
