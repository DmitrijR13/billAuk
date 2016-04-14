using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;

namespace  STCLINE.KP50.DataBase
{
    class DbFtrReestrFromBank : DbBaseReestrFromBank
    {
        private List<string> paymentsList= new List<string>();
        private List<string> countersList= new List<string>();
        public const string paymentTempTable = "paymentTemp";
        public const string countersTempTable = "countersTemp";


        public DbFtrReestrFromBank(IDbConnection connDB, FileNameStruct fileArgs, DbLoadProtokol loadProtokol) : base(connDB, fileArgs, loadProtokol)
        {
            DropTempTables();
            createTempTables();
        }

        public void DropTempTables()
        {
            ExecSQL(_connDB, "Drop table " + paymentTempTable, false);
            ExecSQL(_connDB, "Drop table " + countersTempTable, false);
        }

        public void createTempTables()
        {
            Returns ret = Utils.InitReturns();
            // Извлечем наименование последовательности, которая установлена на tula_file_reestr
            string query = "SELECT s.relname as sequence_name " +
                           "FROM pg_class s  " +
                           "JOIN pg_depend d ON d.objid = s.oid  " +
                           "JOIN pg_class t ON d.objid = s.oid AND d.refobjid = t.oid " +
                           "JOIN pg_namespace n ON n.oid = s.relnamespace  " +
                           "WHERE s.relkind = 'S' " +
                           "AND t.relname = 'tula_file_reestr' " +
                           "AND n.nspname = '" + Points.Pref + "_data'";
            object seqname = ExecScalar(_connDB, query, out ret, true);
            if (!ret.result)
            {
                throw new UserException("Ошибка определения наименования последовательности таблицы tula_file_reestr, см. логи");
            }
            if (seqname == null || String.IsNullOrWhiteSpace(seqname.ToString()))
            {
                throw new UserException("Таблица tula_file_reestr не содержит последовательность");
            }
            query = "Create temp table " + paymentTempTable + " ( " +
                    "nzp_reestr_d integer default nextval('" + Points.Pref + sDataAliasRest + seqname + "')," +
                    "pkod numeric(13,0), " +
                    "nzp_kvar integer, " +
                    "sum_charge numeric (12,4), " +
                    "transaction_id char(255), " +
                    "nomer_plat_poruch char(30), " +
                    "date_plat_poruch date, " +
                    "service_field varchar(30), " +
                    "payment_date_time timestamp, " +
                    "address char(255)," +
                    "cnt1 char(100), " +
                    "val_cnt1 numeric(14,2), " +
                    "cnt2 char(100), " +
                    "val_cnt2 numeric(14,2), " +
                    "cnt3 char(100), " +
                    "val_cnt3 numeric(14,2), " +
                    "cnt4 char(100), " +
                    "val_cnt4 numeric(14,2), " +
                    "cnt5 char(100), " +
                    "val_cnt5 numeric(14,2), " +
                    "cnt6 char(100), " +
                    "val_cnt6 numeric(14,2), " +
                    "bad_payment integer default 0) "+ DBManager.sUnlogTempTable;;
            ret=ExecSQL(_connDB, query, true);
            if (!ret.result)
            {
                throw new UserException("Ошибка создания временной таблицы оплат, см. логи");
            }
            query = "create index on " + paymentTempTable + " (pkod, transaction_id)";
            ret = ExecSQL(_connDB, query, true);
            if (!ret.result)
            {
                throw new UserException("Ошибка добавления индекса для платежного кода и кода транзакции временной таблицы оплат, см. логи");
            }
            query = "create index on " + paymentTempTable + " (pkod)";
            ret = ExecSQL(_connDB, query, true);
            if (!ret.result)
            {
                throw new UserException("Ошибка добавления индекса для платежного кода временной таблицы оплат, см. логи");
            }
            query = "CREATE INDEX ON " + paymentTempTable + " (bad_payment) where bad_payment=0";
            ret = ExecSQL(_connDB, query, true);
            if (!ret.result)
            {
                throw new UserException("Ошибка добавления индекса для оплат не прошедших проверку, см. логи");
            }
            query = "Create temp table " + countersTempTable + " ( " +
                    "nzp_kvar integer, " +
                    "pkod numeric(13,0), " +
                    "nzp_counter integer, " +
                    "val_cnt numeric (14,4), " +
                    "nzp_reestr_d integer," +
                    "transaction_id char(255), " +
                    "bad_cnt integer default 0 ) "+DBManager.sUnlogTempTable;
            ExecSQL(_connDB, query, true);
            if (!ret.result)
            {
                throw new UserException("Ошибка создания временной таблицы показаний ПУ, см. логи");
            }
            query = "CREATE INDEX ON " + countersTempTable + " (pkod, transaction_id)";
            ret = ExecSQL(_connDB, query, true);
            if (!ret.result)
            {
                throw new UserException("Ошибка добавления индекса для временной таблицы показаний ПУ, см. логи");
            }
        }

        public override Returns ParseReestr(FilesImported finder, string[] reestrStrings)
        {
            //сопоставление указанного в квитанции для реестра кол-ва строк и суммы оплат с данными в файле реестра 
            var ret = CheckReestrAtr(reestrStrings.Length, finder.saved_name);
            if (!ret.result)
            {
                return ret;
            }
            double costOneRow = Math.Round(30d/Math.Max(finder.count_rows, 1), 4);
            int counter = 0;
            double currentProgres = 0;
            //заголовочная структура
            foreach (var str in reestrStrings)
            {
                _numRow++;
                counter++;
                ParseOneString(str);
                if (counter%10 == 0)
                {
                    currentProgres += costOneRow*10;
                    _loadProtokol.SetProcent(currentProgres, (int) StatusWWB.InProcess);
                }
            }
            // вставка оплат во временную таблицу
            string mainQuery = "INSERT INTO " + paymentTempTable + " " +
                               "(pkod,sum_charge, transaction_id," +
                               "nomer_plat_poruch, date_plat_poruch, service_field," +
                               "payment_date_time, address, " +
                               "cnt1, val_cnt1," +
                               "cnt2, val_cnt2," +
                               "cnt3, val_cnt3," +
                               "cnt4, val_cnt4," +
                               "cnt5, val_cnt5," +
                               "cnt6, val_cnt6 ) VALUES ";
            ret = insertParsedRowsToTempTable(paymentsList, mainQuery);
            if (!ret.result)
            {
                throw new UserException("Ошибка вставки оплат во временную таблицу, см. логи");
            }
            // вставка показаний ПУ во временную таблицу
            mainQuery = "INSERT INTO " + countersTempTable + " (pkod, transaction_id, nzp_counter, val_cnt) VALUES ";
            ret = insertParsedRowsToTempTable(countersList, mainQuery);
            if (!ret.result)
            {
                throw new UserException("Ошибка вставки показаний ПУ во временную таблицу, см. логи");
            }
            return ret;
        }

        private Returns insertParsedRowsToTempTable(List<string> listToInsert, string mainQuery )
        {
            Returns ret= Utils.InitReturns();
            const int takeRows = 2000;
            if (listToInsert.Count <= 0) return ret;
            int groupCount = listToInsert.Count / takeRows;
            for (int i = 0; i <= groupCount; i++)
            {
                List<string> valuesList = listToInsert.Skip(i * takeRows).Take(takeRows).ToList();
                if (valuesList.Count == 0) continue;
                string query =mainQuery +String.Join(",", valuesList);
                ret = ExecSQL(_connDB, query, true);
                if (!ret.result)
                {
                    return ret;
                }
            }
            return ret;
        }
        protected override Returns ParseOneString(string str)
        {
            string[] fields = str.Split(';');
            Returns ret = Utils.InitReturns();
            ReestrBody bodyReestr = FillBody(fields);
            _loadProtokol.SumCharge += bodyReestr.SumPlat;
            // значения показаний ПУ для вставки во временную таблицу оплат
            var countersVals = bodyReestr.counterList
                .Select(c =>(String.IsNullOrWhiteSpace(c.Cnt)?"null": "'" + c.Cnt + "'")+ // при пустом nzp_counter вставляем null
                "|" + (String.IsNullOrWhiteSpace(c.ValCnt) ? "null" : c.ValCnt)) // при пустом показании ПУ вставляем null
                .SelectMany(c => c.Split('|')).ToList();
            // значения для вставки во временную таблицу с оплатами
            paymentsList.Add("(" + bodyReestr.LSKod + ","
                             + bodyReestr.SumPlat + "," +
                             "'" + bodyReestr.TransactionID + "'," +
                             "'" + bodyReestr.NomerPlatPoruch + "'," +
                             "'" + bodyReestr.DatePlatPoruch + "'," +
                             "'" + bodyReestr.ServiceField + "'," +
                             "'"+bodyReestr.PaymentDateFromTransactionID+"'," +
                             "'" + bodyReestr.Address + "'," +
                             " " + String.Join(",", countersVals) + ")");
            // Если показания ПУ для данной оплаты отсутствуют, выйти
            if (!bodyReestr.counterList.Any(cnt => !String.IsNullOrWhiteSpace(cnt.Cnt) && !String.IsNullOrWhiteSpace(cnt.ValCnt))) return ret;
            // значения счетчиков для вставки во временную таблицу
            foreach (ReestrCounter rc in bodyReestr.counterList.Where(cnt => !String.IsNullOrWhiteSpace(cnt.Cnt) && !String.IsNullOrWhiteSpace(cnt.ValCnt)))
            {
                countersList.Add("(" + bodyReestr.LSKod + ", " +
                                 "'" + bodyReestr.TransactionID + "'," +
                                 rc.Cnt + ", " +
                                 rc.ValCnt + ")");
            }
            return ret;
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
            _loadProtokol.TotalSumPack = 0;
            for (int i = 0; i < rows.Length; i++)
            {
                bool add = false;
                string err = "";

                char[] splitSymbols = { ';', '|' };
                //проверяем структуру строки на целостность
                string[] rowEl = rows[i].Split(splitSymbols);
                if (rowEl.Length != 17)
                {
                    add = true;
                    err += (err != ""
                        ? ", Нарушен формат реестра: Неверное число полей в строке"
                        : " Нарушен формат реестра: Неверное число полей в строке");
                }

                //число полей по формату
                if (rowEl.Length == 17)
                {
                    //Код лицевого счета
                    if (!Regex.IsMatch(rowEl[0], @"^\d+$") || rowEl[0].Trim().Length > 20)
                    {
                        add = true;
                        err += (err != ""
                            ? ", Нарушен формат поля \"Код лицевого счета\""
                            : "Нарушен формат поля \"Код лицевого счета\"");
                    }

                    //Сумма платежа           
                    if (!Regex.IsMatch(rowEl[1], @"^[0-9]+(\.[0-9]+)?$"))
                    {
                        add = true;
                        err += (err != ""
                            ? ", Нарушен формат поля \"Сумма платежа\""
                            : "Нарушен формат поля \"Сумма платежа\"");
                    }
                    else
                    {
                        decimal sumOpl;
                        Decimal.TryParse(rowEl[1], out sumOpl);
                        _loadProtokol.TotalSumPack += sumOpl;
                    }

                    //Номер транзакции 
                    if (!Regex.IsMatch(rowEl[2], @"\d{26}"))
                    {
                        add = true;
                        err += (err != ""
                            ? ", Нарушен формат поля \"Номер транзакции\""
                            : "Нарушен формат поля \"Номер транзакции\"");
                    }
                    else
                    {
                        if (rowEl[2].Length >= 6)
                        {
                            //выковыриваем дату платежа из кода транзакции
                            DateTime paymentDay;
                            if (!DateTime.TryParseExact(rowEl[2].Substring(0, 6), "ddMMyy", CultureInfo.InvariantCulture,
                                    DateTimeStyles.None, out paymentDay))
                            {
                                add = true;
                                err += (err != ""
                                    ? ", Дата платежа в поле \"Номер транзакции\" не определена"
                                    : "Дата платежа в поле \"Номер транзакции\" не определена");
                            }
                            else
                                if (paymentDay > Points.DateOper.AddYears(1))
                                {
                                    add = true;
                                    err += (err != ""
                                        ? ", Дата платежа в поле \"Номер транзакции\" выходит за границы допустимого значения"
                                        : "Дата платежа в поле \"Номер транзакции\" выходит за границы допустимого значения");
                                }
                        }
                        else
                        {
                            add = true;
                            err += (err != ""
                                ? ", Дата платежа в поле \"Номер транзакции\" не определена"
                                : "Дата платежа в поле \"Номер транзакции\" не определена");
                        }
                    }

                    //Номер платежного поручения 
                    if (!Regex.IsMatch(rowEl[3], @"^[0-9]\d*$"))
                    {
                        add = true;
                        err += (err != ""
                            ? ", Нарушен формат поля \"Номер платежного поручения\""
                            : "Нарушен формат поля \"Номер платежного поручения\"");
                    }

                    //Дата платёжного поручения
                    if (!Regex.IsMatch(rowEl[4], @"^(\d{2}).\d{2}.(\d{4})$"))
                    {
                        add = true;
                        err += (err != ""
                            ? ", Нарушен формат поля \"Дата платёжного поручения\""
                            : "Нарушен формат поля \"Дата платёжного поручения\"");
                    }

                    int start_pos = 4;
                    //пробегаем по номерам ПУ и их показаниям
                    for (int j = 1; j <= 12; j += 2)
                    {
                        //Код счетчика 
                        if (!Regex.IsMatch(rowEl[start_pos + j], @"^[0-9]+(\.[0-9]+)?$|^НЕТ$"))
                        {
                            add = true;
                            err += (err != ""
                                ? ", Нарушен формат поля \"Код счетчика №" + (j / 2 + 1) + "\""
                                : "Нарушен формат поля \"Код счетчика №" + (j / 2 + 1) + "\"");
                        }

                        //Показание счетчика текущее
                        if (!Regex.IsMatch(rowEl[start_pos + j + 1], @"^[0-9]+(\.[0-9]+)?$|^НЕТ$"))
                        {
                            add = true;
                            err += (err != ""
                                ? ", Нарушен формат поля \"Показание счетчика текущее №" + (j / 2 + 1) + "\""
                                : "Нарушен формат поля \"Показание счетчика текущее №" + (j / 2 + 1) + "\"");
                        }
                    }

                }

                //добавляем номер строки в список строк с ошибками
                if (add) numRows.Add(i + 1, err);
            }
            if (numRows.Count > 0)
            {
                ret.result = false;
                _loadProtokol.AddUncorrectedRows("Номера строк с ошибками:\r\n");
                foreach (var num in numRows)
                {
                    _loadProtokol.AddUncorrectedRows("Строка: " + num.Key + ", Ошибки: " + num.Value);
                    // ret.text += "Строка: " + num.Key + ", Ошибки: " + num.Value + ";\r\n";
                }
                // ret.text = ret.text.Remove(ret.text.Length - 1, 1);
            }

            return ret;
        }

        /// <summary>
        /// Проверка строк по списку возможных ошибок в заполнении файла
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        protected override ReestrBody FillBody(string[] fields)
        {

            var body = new ReestrBody();
            int index = 0;
            bool add = false;
            try
            {
                if (fields[index].Trim() != "")
                {
                    body.LSKod = fields[index].Trim();
                    if (body.LSKod.Length != 13)
                    {
                        MonitorLog.WriteLog("Ошибка, неверный лицевой счет ", MonitorLog.typelog.Error, 20, 201, true);
                    }
                }
                index++;

                if (fields[index].Trim() != "")
                {
                    body.SumPlat = Convert.ToDecimal(fields[index].Trim());
                }
                index++;

                if (fields[index].Trim() != "")
                {
                    body.TransactionID = fields[index].Trim();
                    if (body.TransactionID.Length > 30) body.TransactionID = body.TransactionID.Substring(0, 30);
                    if (body.TransactionID.Length >= 6)
                    {
                        //выковыриваем дату платежа из кода транзакции
                        body.PaymentDateFromTransactionID = DateTime.ParseExact(body.TransactionID.Substring(0, 6),
                            "ddMMyy",
                            CultureInfo.InvariantCulture);
                    }
                }
                index++;

                if (fields[index].Trim() != "")
                {
                    body.NomerPlatPoruch = fields[index].Trim();
                }
                index++;

                if (fields[index].Trim() != "")
                {
                    body.DatePlatPoruch = Convert.ToDateTime(fields[index].Trim()).ToString("dd.MM.yyyy");
                }
                index++;

                ReestrCounter counter = null;

                while (index < fields.Length)
                {
                    counter = new ReestrCounter();

                    if (fields[index].Trim() != "")
                    {
                        counter.Cnt = fields[index].Trim();
                        if (counter.Cnt == "НЕТ") counter.Cnt = String.Empty;
                    }
                    index++;

                    if (fields[index].Trim() != "")
                    {
                        counter.ValCnt = fields[index].Trim();
                        if (String.IsNullOrEmpty(counter.Cnt) && counter.ValCnt != "НЕТ") add = true;
                    }
                    index++;

                    body.counterList.Add(counter);
                }

                for (int i = 0; i < body.counterList.Count; i++)
                {
                    if (body.counterList[i].ValCnt == "НЕТ") body.counterList[i].ValCnt = String.Empty;
                }

                if (add)
                {
                    _loadProtokol.AddUncorrectedRows("Строка " + _numRow +
                                                     ": Нераспределенные показания приборов учета (нет номеров ПУ);");
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Ошибка парсинга данных реестра", ex);
                throw;
            }

            return body;
        }

        /// <summary>
        /// сопоставление указанного в квитанции для реестра кол-ва строк и суммы оплат с данными в файле реестра 
        /// </summary>
        /// <param name="reestrRowsCount"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public override Returns CheckReestrAtr(int reestrRowsCount, string fileName)
        {
            Returns ret;

            string sqlString = "SELECT COUNT(*) FROM " + Points.Pref + sDataAliasRest + "tula_kvit_reestr " +
                        " WHERE file_name='" + fileName + "' " +
                        " AND sum_plat=" + _loadProtokol.TotalSumPack +
                        " AND count_rows=" + reestrRowsCount;
            object obj = ExecScalar(_connDB, sqlString, out ret, true);
            int num = obj != DBNull.Value ? Convert.ToInt32(obj) : 0;
            if (num == 0)
            {
                ret.result = false;
                _loadProtokol.AddComment("\nВ файле реестра сумма платежей или количество строк не " +
                           " совпадает с данными в квитанции ");
            }
            else
            {
                ret.result = true;
            }
            return ret;
        }

        /// <summary>
        /// Добавление платежа в БД
        /// </summary>
        /// <param name="bodyReestr"></param>
        /// <returns></returns>
        protected override bool InsertPayment(ReestrBody bodyReestr)
        {
            return true;
        }

        /// <summary>
        /// Получить уникальный код счетчика 
        /// </summary>
        /// <param name="numCnt"></param>
        /// <returns></returns>
        override protected string GetNzpCounter(string nzp_counter, string pkod, string valCnt)
        {
            return "";
        }

        protected override bool IsPaymentExists(string pkod, string transactionID, decimal sumCharge)
        {
            return true;
        }
    }
}
