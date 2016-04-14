using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Bars.KP50.Report;
using FastReport;
using Newtonsoft.Json;
using STCLINE.KP50;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;

namespace Bars.KP50.Load.Obninsk
{
    public class LoadPayments : BaseSqlLoad
    {
        public override string Name
        {
            get { return "Загрузка платежей от платежных агентов"; }
        }

        public override string Description
        {
            get { return "Загрузка платежей от платежных агентов из файлов формата DBF"; }
        }

        protected override byte[] Template
        {
            get { return null; }
        }

        public override List<UserParam> GetUserParams()
        {
           return null;
        }

        /// <summary>
        /// Количество загружаемых строк
        /// </summary>
        private int _rowsCount;

        private string _inn;
        private string rashSchet;
        private string kod;
        private string nFile;
        private string data;
        private DateTime datePack;
        private string finAlias;
        private string extractDirectory;

        protected override void PrepareParams()
        {
           
        }

        private string datPrev;
        private string year;
        private string operDay;

        /// <summary>
        /// Массив максимально-допустимых разниц показаний счетчиков
        /// </summary>
        private Dictionary<string, Dictionary<int, decimal>> _maxDiffBetweenValues;

        public override void LoadData()
        {
            CultureInfo ci = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            ci.NumberFormat.NumberDecimalSeparator = ".";
            Thread.CurrentThread.CurrentCulture = ci;
            var ret = STCLINE.KP50.Global.Utils.InitReturns();
            try
            {
                #region Загрузка данных из файла

                var fs = new FileStream(TemporaryFileName, FileMode.Open, FileAccess.Read);
                var extension = System.IO.Path.GetExtension(TemporaryFileName);
                if (extension != null && extension.ToLower().Trim() == ".zip")
                {
                    fs = DecriptFilePack(out ret, fs, TemporaryFileName);
                }
                else
                {
                    Protokol.AddComment("Данный тип файла не поддерживается, данные не загружены");
                    fs.Close();
                    return;
                }

                if (!ret.result)
                {
                    Protokol.AddComment(ret.text);
                    return;
                }

                //разбор имени файла
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(TemporaryFileName);
                if (fileNameWithoutExtension == null)
                {
                    Protokol.AddComment("Некорректное имя файла, данные не загружены");
                    fs.Close();
                    return;
                }
                
                string[] fname = fileNameWithoutExtension.Split('_');

                if (fname.Length >= 5)
                {
                    _inn = fname[0].Trim();
                    rashSchet = fname[1].Trim();
                    kod = fname[2].Trim();
                    nFile = fname[3].Trim();
                    data = fname[4].Trim();
                    try
                    {
                        datePack = DateTime.ParseExact(data.Substring(1).Trim(), "ddMMyyyy",
                            CultureInfo.InvariantCulture);
                    }
                    catch (Exception)
                    {
                        Protokol.AddComment("Некорректная дата в имени файла, данные не загружены");
                        fs.Close();
                        return;
                    }
                }
                else
                {
                    Protokol.AddComment("Некорректное имя файла, данные не загружены");
                    fs.Close();
                    return;
                }
                
                var tablePayments = ConvertDbfToDataTable(fs, null, true, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка разбора файла: " + FileName,
                        MonitorLog.typelog.Error, 20, 201, true);
                    Protokol.AddComment("Ошибка разбора файла: " + FileName);
                    return;
                }
                
                fs.Close();

                #endregion

                datPrev = "'" +
                             new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(-1)
                                 .ToShortDateString() + "'"; //предыдущий рассчетный месяц
                year = (Points.CalcMonth.year_ - 2000).ToString("00");

                operDay = (new DateTime(Points.DateOper.Year, Points.DateOper.Month, Points.DateOper.Day)).ToString(
                    "dd.MM.yyyy");

                finAlias = Points.Pref + "_fin_" + year + DBManager.tableDelimiter;

                //разбор реестра
                ParseReestr(out ret, tablePayments);
                if (!ret.result)
                {
                    Protokol.AddComment(ret.text);
                    return;
                }

                if (LoadPayOrIpuType != SimpleLoadPayOrIpuType.Ipu)
                {
                    //сохранение в pack и pack_ls
                    InsertPack(out ret);
                    if (!ret.result)
                    {
                        //throw new Exception(ret.text);
                        return;
                    }
                }
                if (LoadPayOrIpuType != SimpleLoadPayOrIpuType.Pay)
                {
                    //сохранение показаний ПУ
                    SaveCountersVals(out ret);
                    if (!ret.result)
                    {
                        //throw new Exception(ret.text);
                    }
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                //удаление файлов
                if(Directory.Exists(extractDirectory))
                    Directory.Delete(extractDirectory, true);
            }
        }

        /// <summary>
        /// Разархивировать файл
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="filename"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public FileStream DecriptFilePack(out Returns ret, FileStream fs, string filename)
        {
            FileStream newfs = null;
            try
            {
                ret = STCLINE.KP50.Global.Utils.InitReturns();

                extractDirectory = Path.Combine(Constants.Directories.ImportAbsoluteDir,
                    Path.GetFileNameWithoutExtension(filename));
                string[] files = Archive.GetInstance(ArchiveFormat.Zip)
                    .Decompress(fs, extractDirectory);

                if (files.Length > 2)
                {
                    ret = new Returns(false, "В архиве допускается не более 1 файла с данными", -1);
                    return null;
                }

                newfs = new FileStream(Path.Combine(extractDirectory, files[0]), FileMode.Open, FileAccess.ReadWrite);

                fs.Close();
                fs.Dispose();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка разархивирования файла: " + ex, MonitorLog.typelog.Error, true);
                ret = new Returns(false, "Ошибка разархивирования файла", -1);
            }

            return newfs;
        }

        /// <summary>
        /// Разбор реестра
        /// </summary>
        /// <param name="ret"> Результат </param>
        /// <param name="dtTable"> Таблица платежей </param>
        /// <returns></returns>
        private void ParseReestr(out Returns ret, DataTable dtTable)
        {
            ret = STCLINE.KP50.Global.Utils.InitReturns();
            if (dtTable == null)
            {
                ret = new Returns(false, "В файле отсуствует записи", -1);
                return;
            }

            _rowsCount = dtTable.Rows.Count;


            #region разбор реестра

            foreach (DataRow row in dtTable.Rows)
            {
                try
                {
                    string pkod = row["l_sh"].ToString().Trim();
                    string s = row["date_pay"].ToString().Trim();
                    string sumPay = row["sum"].ToString().Trim();
                    DateTime datePay;
                    if (!DateTime.TryParse(s, out datePay))
                    {
                        Protokol.AddUncorrectedRow("Некорректная дата в исходном файле, ожидается ДД.ММ.ГГГГ: " +
                                                   " date_pay = " + s +
                                                   ", paccount = " + pkod);
                        continue;
                    }

                    if (LoadPayOrIpuType != SimpleLoadPayOrIpuType.Ipu)
                    {
                        //запись в реестр платежей
                        InsertPayment(pkod, datePay, sumPay);
                    }

                    if (LoadPayOrIpuType != SimpleLoadPayOrIpuType.Pay)
                    {
                        //сохранение показаний ПУ
                        foreach (DataColumn dc in dtTable.Columns)
                        {
                            string colName = dc.ColumnName.ToLower();
                            int nzpServ = -1;
                            if (colName == "l_sh" ||
                                colName == "date_pay" ||
                                colName == "sum" ||
                                colName == "bar_code" ||
                                colName == "adress" ||
                                colName == "filial" ||
                                colName == "r_sh") continue;

                            string num = colName.Substring(colName.Length - 1);

                            if (colName.IndexOf("xv_", StringComparison.Ordinal) > -1)
                            {
                                nzpServ = 6;
                            }
                            else if (colName.IndexOf("gv_", StringComparison.Ordinal) > -1)
                            {
                                nzpServ = 9;
                            }
                            else if (colName.IndexOf("elec_d", StringComparison.Ordinal) > -1)
                            {
                                nzpServ = 25;
                                num = "1";
                            }
                            else if (colName.IndexOf("elec_n", StringComparison.Ordinal) > -1)
                            {
                                nzpServ = 210;
                                num = "1";
                            }

                            try
                            {

                                string sql = " INSERT INTO " + Points.Pref + DBManager.sDataAliasRest +
                                             " simple_cnt_reestr(nzp_load, date_pay, paccount, nzp_serv, num, val_cnt)" +
                                             " VALUES(" + NzpLoad + ", '" + datePay.ToShortDateString() + "'," +
                                             pkod + "," + nzpServ + "," + num + "," + row[dc.ColumnName] + ")";
                                ExecSQL(sql);
                            }
                            catch (Exception)
                            {

                                Protokol.AddUncorrectedRow("Некорректная строка в исходном файле: " +
                                                           ", date_pay = " + datePay +
                                                           ", l_sh = " + pkod +
                                                           ", num =" + num +
                                                           ", val_cnt = " + row[dc.ColumnName]);
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    ret = new Returns(false, "Ошибка разбора реестра.", -1);
                    MonitorLog.WriteLog("Ошибка разбора реестра: " + ex, MonitorLog.typelog.Error, true);
                }
                
            }

            #endregion

            
        }


        /// <summary>
        /// Запись строки реестра в таблицу
        /// </summary>
        /// <param name="pkod"> Платежный код </param>
        /// <param name="datePay"> Дата платежа </param>
        /// <param name="sumPay"> Сумма платежа </param>
        /// <returns></returns>
        private bool InsertPayment(string pkod, DateTime datePay, string sumPay)
        {
            try
            {
                if (pkod == null || sumPay == null)
                {
                    return false;
                }
                
                decimal plkod = Convert.ToDecimal(pkod);

                if (plkod > 9999999999999)
                {
                    Protokol.AddUncorrectedRow("Некорректный платежный код в исходном файле: " +
                                               " l_sh = " + pkod +
                                               " date_pay = " + datePay.ToShortDateString() +
                                               " sum = " + sumPay);
                    return false;
                }

                decimal sum = Convert.ToDecimal(sumPay);

                //проверка на существование платежа
                string sqlStr = " SELECT SUM(sum) as sum" +
                                " FROM " + Points.Pref + "_data" + DBManager.tableDelimiter + "simple_pay_reestr " +
                                " WHERE pkod = " + pkod + " " +
                                " AND date_pay = '" + datePay.ToShortDateString() + "' " +
                                " AND sum = " + sum;

                Returns ret;
                object obj = DBManager.ExecScalar(Connection, sqlStr, out ret, true);
                if (obj != null && obj != DBNull.Value)
                {
                    

                    if (Convert.ToDecimal(obj) > 0)
                    {
                        if (sum == Convert.ToDecimal(obj))
                        {
                            Protokol.AddComment("Платеж был загружен ранее - " +
                                                     " платежный код: " + pkod + ", дата платежа: " + datePay.ToShortDateString() +
                                                     ", сумма: " + sum + ". Данные не загружены.");
                        }

                    }
                    return false;
                }

                //добавление платежа в реестр
                string sql =
                    " INSERT INTO " + Points.Pref + DBManager.sDataAliasRest + "simple_pay_reestr " +
                    " (nzp_load, pkod, date_pay, sum) " +
                    " VALUES ( " + NzpLoad + ", " + pkod + ", '" + datePay.ToShortDateString() + "', " + sum + 
                    " )";
                ExecSQL(sql);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка записи платежа в таблицу: " + ex, MonitorLog.typelog.Error, true);
                return false;
            }
            return true;

        }

        /// <summary>
        /// Сохранение в pack, pack_ls 
        /// </summary>
        /// <param name="ret"></param>
        private void InsertPack(out Returns ret)
        {
            ret = STCLINE.KP50.Global.Utils.InitReturns();
            
            //Связать с ЛС из системы
            string sql =
                " UPDATE " + Points.Pref + DBManager.sDataAliasRest + "simple_pay_reestr " +
                " SET nzp_kvar = " +
                " (SELECT k.nzp_kvar FROM " + Points.Pref + DBManager.sDataAliasRest + "kvar k " +
                " WHERE k.pkod = " + Points.Pref + DBManager.sDataAliasRest + "simple_pay_reestr.pkod) " +
                " WHERE nzp_load = " + NzpLoad + " ";
            ret = DBManager.ExecSQL(Connection,sql, true, 6000);
            if (!ret.result)
            {
                ret = new Returns(false, "Ошибка сопоставления платежных кодов", -1);
                return;
            }

            sql =
                " SELECT pkod FROM " + Points.Pref + DBManager.sDataAliasRest + "simple_pay_reestr " +
                " WHERE nzp_kvar IS NULL AND nzp_load = " + NzpLoad;
            IDataReader reader;
            ExecRead(out reader, sql);
            while (reader.Read())
            {
                Protokol.AddUnrecognisedRow("Платежный код: " + (reader["pkod"] != DBNull.Value ? reader["pkod"].ToString() : "") +
                    " не зарегистрирован в системе.");
            }
            
            decimal sumPack = 0;

            var insertedRowsForPack = 0;
            sql = " SELECT COUNT(*) FROM " + Points.Pref + "_data" + DBManager.tableDelimiter + "simple_pay_reestr " +
                  " WHERE sum > 0 and nzp_load = " + NzpLoad;
            object obj = ExecScalar(sql, out ret, true);

            if (obj != null && obj != DBNull.Value)
            {
                insertedRowsForPack = Convert.ToInt32(obj);
            }
            else
            {
                MonitorLog.WriteLog("Ошибка получения кол-ва строк для пачки: " + sql,
                    MonitorLog.typelog.Error, true);
            }

            if (insertedRowsForPack == 0)
            {
                //ret = new Returns(false, "Нет загруженных оплат", -1);
                return;
            }
            //достаем реальные значения оплаты для пачки, т.к. некоторые записи уже могли быть загружены ранее в период.реестре
            sql = " SELECT SUM(sum) FROM " + Points.Pref + DBManager.sDataAliasRest + "simple_pay_reestr " +
                         " WHERE nzp_load = " + NzpLoad ;
             obj = ExecScalar(sql, out ret, true);

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
           

            #region Сохранение в pack, pack_ls
            sql = " INSERT INTO " + Points.Pref + "_fin_" + year + DBManager.tableDelimiter + "pack  " +
                  " (pack_type, " +
                  " nzp_bank,   " +
                  " num_pack,   " +
                  " dat_pack,   " +
                  " count_kv,   " +
                  " sum_pack,   " +
                  " real_count, " +
                  " flag,       " +
                  " dat_vvod,   " +
                  " file_name,  " +
                  " dat_uchet)  " +
                  " SELECT 10,  " +
                  " b.nzp_bank, " +
                  " '" + nFile + "', " +
                  "'" + datePack.ToShortDateString() + "', " +
                  insertedRowsForPack + ", " +
                  + sumPack + ", " +
                  " " + insertedRowsForPack + ", " +
                  " 11, " +
                  " " + DBManager.sCurDateTime + ", " +
                  "k.file_name " +
                  ", '" + operDay + "' " +
                  " FROM " + Points.Pref + DBManager.sKernelAliasRest + "s_bank b, " +
                  "      " + Points.Pref + DBManager.sKernelAliasRest + "s_payer p, " +
                  "      " + Points.Pref + DBManager.sDataAliasRest + "simple_load k " +
                  " WHERE k.nzp_load =" + NzpLoad +
                  " AND p.inn = '" + _inn + "'" +
                  " AND b.nzp_payer = p.nzp_payer";
            ret = DBManager.ExecSQL(Connection, sql, true, 6000);
            if (!ret.result)
            {
                ret = new Returns(false, "Ошибка записи пачек оплаты", -1);
                return;
            }

            //код вставленной записи в pack
            var nzpPack = GetSerialValue();

            //Добавляем оплаты по ЛС
            sql = " INSERT INTO " + finAlias + "pack_ls " +
                  " (nzp_pack,      " +
                  " num_ls,         " +
                  " g_sum_ls,       " +
                  " sum_ls,         " +
                  " kod_sum,        " +
                  " paysource,      " +
                  " id_bill,        " +
                  " dat_vvod,       " +
                  //" info_num,       " +
                  " inbasket,       " +
                  " alg,            " +
                  " unl,            " +
                  " incase,         " +
                  " pkod,           " +
                  " nzp_user,       " +
                  " dat_month)      " +
                  " SELECT          " + 
                  nzpPack + ",      " +
                  " k1.num_ls,      " +
                  " f.sum,          " +
                  " 0 as sum_ls,    " +
                  " 33, " +
                  " 1 as paysource, " +
                  "  0 as id_bill,  " +
                  " f.date_pay,     " +
                // костыль
                  " (CASE WHEN f.nzp_kvar IS NOT NULL THEN 0 ELSE 1 END) AS inbasket, " +
                  " 0 AS alg,       " +
                  " 0 AS unl,       " +
                  " 0 AS incase,    " +
                  " (CASE WHEN f.pkod IS NULL OR f.pkod>9999999999999 THEN 0 ELSE f.pkod END), " +
                  ReportParams.User.nzp_user + " ," + datPrev + 
                  " FROM            " +
                  Points.Pref + "_data" + DBManager.tableDelimiter + "simple_pay_reestr f " +
                  " LEFT OUTER JOIN " + Points.Pref + "_data" + DBManager.tableDelimiter +
                  " kvar k1  on f.nzp_kvar = k1.nzp_kvar  " +
                  " WHERE " +
                  "       f.nzp_load = " + NzpLoad + " ";
            ret = DBManager.ExecSQL(Connection, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка записи оплат по лицевым счетам";
                return;
            }

            //записываем не соотнесенные с системой ЛС в pack_ls_err  
            sql = " INSERT INTO " + finAlias + "pack_ls_err (nzp_pack_ls, nzp_err, note) " +
                  " SELECT nzp_pack_ls, 666, pkod " +
                  " FROM  " + finAlias + "pack_ls " +
                  " WHERE nzp_pack = " + nzpPack + " AND inbasket = 1 ";
            if (!DBManager.ExecSQL(Connection, sql, true).result)
            {
                ret.text = "Ошибка записи не сопоставленных лицевых счетов";
                ret.result = false;
                return;
            }
            #endregion

        }

        /// <summary>
        /// Сохранение показаний ПУ
        /// </summary>
        /// <param name="ret"></param>
        private void SaveCountersVals(out Returns ret)
        {
            try
            {
                ret = STCLINE.KP50.Global.Utils.InitReturns();

                _maxDiffBetweenValues = GetMaxDiffBetweenValuesDict();

                #region Собираем данные во временную таблицу

                string sql = " DROP TABLE t_counts;";
                ExecSQL(sql);

                sql = " CREATE TEMP TABLE t_counts " +
                             " (                          " +
                             " nzp_kvar INTEGER,          " +
                             " nzp_serv INTEGER,          " +
                             " nzp_wp INTEGER,            " +
                             " bad_cnt INTEGER DEFAULT 0, " +
                             " date_pay DATE,             " +
                             " paccount " + DBManager.sDecimalType + "(13,0)," +
                             " val_cnt " + DBManager.sDecimalType + "(14,4), " +
                             " last_val " + DBManager.sDecimalType + "(14,4)," +
                             " rashod " + DBManager.sDecimalType + "(14,4),  " +
                             " nzp_counter INTEGER)       ";
                ExecSQL(sql);

                sql = " INSERT INTO t_counts (nzp_kvar, nzp_wp, paccount, date_pay, nzp_serv, val_cnt) " +
                  " SELECT k.nzp_kvar, k.nzp_wp, a.paccount, " +
                  (DBManager.tableDelimiter == "."
                  ? "date_trunc('month',a.date_pay + interval '1 month')"
                  : "date('01'||'.'||month(dat_saldo)||'.'||year(dat_saldo)) + 1 units month)") +
                  "  AS date_pay, " +
                  " a.nzp_serv, SUM(val_cnt) AS val_cnt " +
                  " FROM " + Points.Pref + DBManager.sDataAliasRest + "simple_cnt_reestr a left outer join " +
                  "      " + Points.Pref + DBManager.sDataAliasRest + "kvar k " +
                  " ON a.paccount = k.pkod " +
                  " WHERE nzp_load = " + NzpLoad + " AND val_cnt <> 0 " +
                  " GROUP BY 1,2,3,4,5 ";
                ExecSQL(sql);

                sql = " SELECT a.nzp_wp, p.bd_kernel AS pref " +
                  " FROm t_counts a, " + Points.Pref + DBManager.sKernelAliasRest + "s_point p" +
                  " WHERE a.nzp_wp = p.nzp_wp " +
                  " GROUP BY 1,2";
                DataTable preflist = ExecSQLToTable(sql);
                foreach (DataRow dr in preflist.Rows)
                {
                    sql = " UPDATE t_counts SET nzp_counter = (SELECT MAX(nzp_counter) " +
                          " FROM " + dr["pref"].ToString().Trim() + DBManager.sDataAliasRest + "counters_spis s" +
                          " WHERE t_counts.nzp_kvar = s.nzp " +
                          " AND t_counts.nzp_serv = s.nzp_serv)" +
                          " WHERE nzp_wp = " + dr["nzp_wp"];
                    ExecSQL(sql);

                    sql = " UPDATE t_counts SET last_val = (SELECT MAX(val_cnt) " +
                          " FROM " + dr["pref"].ToString().Trim() + DBManager.sDataAliasRest + "counters s" +
                          " WHERE t_counts.nzp_counter = s.nzp_counter and s.is_actual <> 100)" +
                          " WHERE nzp_counter IS NOT NULL AND nzp_wp=" + dr["nzp_wp"];
                    ExecSQL(sql);

                    sql = " UPDATE t_counts SET bad_cnt = 1 " +
                          " WHERE 1 = (SELECT MAX(1) " +
                          " FROM " + finAlias + DBManager.tableDelimiter + "pu_vals s  " +
                          //dr["pref"].ToString().Trim() + DBManager.sDataAliasRest + "counters s" +
                          " WHERE t_counts.nzp_counter = s.nzp_counter AND t_counts.date_pay = s.dat_month)" +
                          " AND nzp_counter IS NOT NULL ";//"AND nzp_wp = " + dr["nzp_wp"];
                    ExecSQL(sql);

                    sql = " UPDATE t_counts SET rashod = val_cnt - last_val WHERE last_val IS NOT NULL ";
                    ExecSQL(sql);

                    #region Проверки
                    sql = " UPDATE t_counts SET bad_cnt = 2 WHERE rashod < 0 AND nzp_wp = " + dr["nzp_wp"];
                    ExecSQL(sql);

                    sql = " UPDATE t_counts SET bad_cnt = 3 " +
                          " WHERE rashod > " + GetDiffByServ(dr["pref"].ToString().Trim(), 9) + " AND nzp_serv = 9 AND nzp_wp = " + dr["nzp_wp"];
                    ExecSQL(sql);

                    sql = " UPDATE t_counts SET bad_cnt = 3 " +
                          " WHERE rashod > " + GetDiffByServ(dr["pref"].ToString().Trim(), 6) + " AND nzp_serv = 6 AND nzp_wp = " + dr["nzp_wp"];
                    ExecSQL(sql);

                    sql = " UPDATE t_counts SET bad_cnt = 3 " +
                          " WHERE rashod > " + GetDiffByServ(dr["pref"].ToString().Trim(), 25) + " AND nzp_serv = 25 AND nzp_wp = " + dr["nzp_wp"];
                    ExecSQL(sql);

                    sql = " UPDATE t_counts SET bad_cnt = 3 " +
                          " WHERE rashod > " + GetDiffByServ(dr["pref"].ToString().Trim(), 210) + " AND nzp_serv = 210 AND nzp_wp = " + dr["nzp_wp"];
                    ExecSQL(sql);

                    #endregion
                }
                sql = " UPDATE t_counts SET bad_cnt = 4 WHERE nzp_counter IS NULL";
                ExecSQL(sql);

                #endregion

                #region Выбираем проблеммные счетчики

                sql = " SELECT * FROM t_counts  " +
                      " WHERE bad_cnt > 0 ";

                var badCounterTable = ExecSQLToTable(sql);

                foreach (DataRow dr in badCounterTable.Rows)
                {
                    if (dr["nzp_kvar"] == DBNull.Value)
                        Protokol.AddUnrecognisedRow("Платежный код  " + dr["paccount"] +
                            " не зарегистрирован в системе " +
                            " Счетчик по услуге " + GetServName((int)dr["nzp_serv"]) +
                            " показание " + dr["val_cnt"]);
                    else
                        if (dr["nzp_counter"] == DBNull.Value)
                            Protokol.AddComment(" Счетчик по услуге " + GetServName((int)dr["nzp_serv"]) +
                                " не зарегистрирован в системе платежный код  " + dr["paccount"] + " показание " +
                                dr["val_cnt"]);
                        else if (dr["rashod"] != DBNull.Value)
                        {
                            string s = String.Empty;
                            switch ((int)dr["bad_cnt"])
                            {
                                case 1:
                                    s = "На " + ((DateTime)dr["date_pay"]).ToShortDateString() + " уже есть внесенное показание";
                                    break;
                                case 2:
                                    s = "Предыдущее значение счетчика " + dr["last_val"] + " больше текущего ";
                                    break;
                                case 3:
                                    s = "Превышен лимит расхода по услуге, текущий расход " + dr["rashod"];
                                    break;
                            }
                            Protokol.AddComment(s + " для счетчика по услуге " + GetServName((int)dr["nzp_serv"]) +
                                                " показание " + dr["val_cnt"] +
                                                ", данные не загружены." + " платежный код  " + dr["paccount"]);



                        }
                }

                #endregion

                #region Добавляем хорошие счетчики
                foreach (DataRow pref in preflist.Rows)
                {
                    //получаем рассчетный месяц локального банка
                    var localDate = Points.GetCalcMonth(new CalcMonthParams(pref["pref"].ToString().Trim()));
                    if (localDate.month_ == 0 || localDate.year_ == 0) localDate = Points.CalcMonth;

                    string datNextMonth = "'" + new DateTime(localDate.year_, localDate.month_, 1).AddMonths(1)
                        .ToShortDateString() + "'";

                    sql = " INSERT INTO  " + finAlias + DBManager.tableDelimiter + "pu_vals  " +
                          " (nzp_pack_ls, num_ls, nzp_counter, val_cnt, dat_month, cur_unl) " +
                          " SELECT pl.nzp_pack_ls, f.nzp_kvar, nzp_counter, val_cnt, " + datNextMonth + ",  " +
                          NzpLoad +
                          " FROM " + finAlias + DBManager.tableDelimiter + "pack_ls pl," +
                          " t_counts f " +
                          " WHERE f.paccount = pl.pkod " +
                          " AND nzp_wp = " + pref["nzp_wp"] +
                          " AND bad_cnt = 0";
                    ExecSQL(sql, true);
                #endregion

                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при сохранении показаний ПУ: " + ex, MonitorLog.typelog.Error, true);
                ret = new Returns(false, "Ошибка при сохранении показаний ПУ", -1);
            }
            finally
            {
                ExecSQL("DROP TABLE t_counts");
            }
        }

        /// <summary>
        /// Формирование протокола загрузки
        /// </summary>
        /// <returns></returns>
        public override string GetProtocolName()
        {
            var ret = STCLINE.KP50.Global.Utils.InitReturns();
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

            var fDataSet = new DataSet();
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
            rep.SetParameterValue("file_name", FileName);
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


           // перенос  на ftp сервер
            if (InputOutput.useFtp)
            {
                fileName = InputOutput.SaveOutputFile(Constants.Directories.ReportDir + fileName);
            }

            ProtocolFileName = Constants.Directories.ReportDir + fileName;

            ExecSQL("UPDATE " + Points.Pref + DBManager.sDataAliasRest + "simple_load " +
                                " SET nzp_exc = " + NzpExcelUtility + ", download_status = " + download_status + ", temp_file = '" + TemporaryFileName + "', " +
                                " nzp = " + Nzp +
                                " WHERE nzp_load = " + NzpLoad);

            myFile.SetFileState(new ExcelUtility
            {
                nzp_exc = NzpExcelUtility,
                status = ExcelUtility.Statuses.Success,
                exc_path = fileName
            });

            #endregion

            return ProtocolFileName;
        }
        
        /// <summary>
        /// Конвертирование DBF файла в DataTable
        /// </summary>
        /// <param name="fs">Файловый Поток</param>
        /// <param name="codePage">Кодовая страница, если известна</param>
        /// <param name="suppessFormat">Подавлять формат 0х03</param>
        /// <param name="ret">Результат конвертации</param>
        /// <returns></returns>
        private static DataTable ConvertDbfToDataTable(Stream fs, string codePage, bool suppessFormat, out Returns ret)
        {
            DataTable dt = new DataTable();
            ret = STCLINE.KP50.Global.Utils.InitReturns();

            int tag = 0;
            try
            {
                // определение кодировки файла
                var buffer = new byte[1];
                fs.Position = 0x00;
                fs.Read(buffer, 0, buffer.Length);
                if (buffer[0] != 0x03 && !suppessFormat)
                {
                    ret.result = false;
                    ret.text = "Данный формат DBF файла не поддерживается";
                    ret.tag = -1;
                    return null;
                }

                // определение кодировки файла (взято из http://ru.wikipedia.org/wiki/DBF)
                Encoding encoding;
                if (codePage == "866") encoding = Encoding.GetEncoding(866);
                else if (codePage == "1251") encoding = Encoding.GetEncoding(1251);
                else
                {
                    buffer = new byte[1];
                    fs.Position = 0x1D;
                    fs.Read(buffer, 0, buffer.Length);
                    if (buffer[0] != 0x65 &&    //Codepage_866_Russian_MSDOS
                        buffer[0] != 0x26 &&    //кодовая страница 866 DOS Russian
                        buffer[0] != 0xC9 &&    //Codepage_1251_Russian_Windows
                        buffer[0] != 0x57)    //кодовая страница 1251 Windows ANSI
                    {
                        ret = new Returns(false, "Кодовая страница не задана или не поддерживается", -1);
                        return null;
                    }
                    if (buffer[0] == 0x65 || buffer[0] == 0x26)
                        encoding = Encoding.GetEncoding(866);
                    else
                        encoding = Encoding.GetEncoding(1251);
                }

                buffer = new byte[4]; // Кол-во записей: 4 байтa, начиная с 5-го
                fs.Position = 4;
                fs.Read(buffer, 0, buffer.Length);
                int RowsCount = buffer[0] + (buffer[1] * 0x100) + (buffer[2] * 0x10000) + (buffer[3] * 0x1000000);
                buffer = new byte[2]; // Кол-во полей: 2 байтa, начиная с 9-го
                fs.Position = 8;
                fs.Read(buffer, 0, buffer.Length);
                int FieldCount = (((buffer[0] + (buffer[1] * 0x100)) - 1) / 32) - 1;
                string[] FieldName = new string[FieldCount]; // Массив названий полей
                string[] FieldType = new string[FieldCount]; // Массив типов полей
                byte[] FieldSize = new byte[FieldCount]; // Массив размеров полей
                byte[] FieldDigs = new byte[FieldCount]; // Массив размеров дробной части
                buffer = new byte[32 * FieldCount]; // Описание полей: 32 байтa * кол-во, начиная с 33-го
                fs.Position = 32;
                fs.Read(buffer, 0, buffer.Length);
                int FieldsLength = 0;
                DataColumn col;
                for (int i = 0; i < FieldCount; i++)
                {
                    // Заголовки
                    FieldName[i] = System.Text.Encoding.Default.GetString(buffer, i * 32, 10).TrimEnd(new char[] { (char)0x00 });
                    FieldType[i] = "" + (char)buffer[i * 32 + 11];
                    FieldSize[i] = buffer[i * 32 + 16];
                    FieldDigs[i] = buffer[i * 32 + 17];
                    FieldsLength = FieldsLength + FieldSize[i];
                    // Создаю колонки
                    switch (FieldType[i])
                    {
                        case "L": dt.Columns.Add(FieldName[i], Type.GetType("System.Boolean")); break;
                        case "D": dt.Columns.Add(FieldName[i], Type.GetType("System.DateTime")); break;
                        case "N":
                            {
                                if (FieldDigs[i] == 0)
                                    dt.Columns.Add(FieldName[i], Type.GetType("System.Int64"));
                                else
                                {
                                    col = new DataColumn(FieldName[i], Type.GetType("System.Decimal"));
                                    col.ExtendedProperties.Add("precision", FieldSize[i]);
                                    col.ExtendedProperties.Add("scale", FieldDigs[i]);
                                    col.ExtendedProperties.Add("length", FieldSize[i] + FieldDigs[i]);
                                    dt.Columns.Add(col);
                                }
                                break;
                            }
                        case "F": dt.Columns.Add(FieldName[i], Type.GetType("System.Double")); break;
                        default:
                            col = new DataColumn(FieldName[i], Type.GetType("System.String"));
                            col.MaxLength = FieldSize[i];
                            dt.Columns.Add(col);
                            break;
                    }
                }
                fs.ReadByte(); // Пропускаю разделитель схемы и данных Должен быть равен 13
                System.Globalization.DateTimeFormatInfo dfi = new System.Globalization.CultureInfo("ru-RU", false).DateTimeFormat;
                System.Globalization.NumberFormatInfo nfi = new System.Globalization.CultureInfo("ru-RU", false).NumberFormat;
                nfi.NumberDecimalSeparator = ".";
                nfi.CurrencyDecimalSeparator = ".";


                buffer = new byte[FieldsLength];
                dt.BeginLoadData();
                //fs.ReadByte(); // Пропускаю стартовый байт элемента данных
                int delPriznak = 0;
                char[] numericValidChars = new char[] { '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.' };

                for (int j = 0; j < RowsCount; j++)
                {

                    delPriznak = fs.ReadByte(); // Пропускаю стартовый байт элемента данных

                    fs.Read(buffer, 0, buffer.Length);
                    System.Data.DataRow R = dt.NewRow();
                    int Index = 0;



                    for (int i = 0; i < FieldCount; i++)
                    {
                        
                        string l = encoding.GetString(buffer, Index, FieldSize[i]).TrimEnd(new char[] { (char)0x00, (char)0x20 });
                        Index = Index + FieldSize[i];

                        if (l != "")
                            switch (FieldType[i])
                            {
                                case "L": R[i] = l == "T" ? true : false; break;
                                case "D":
                                    try
                                    {
                                        R[i] = DateTime.ParseExact(l, "yyyyMMdd", dfi);
                                    }
                                    catch
                                    {
                                        tag = -1;
                                        throw new Exception("Ожидалась дата в формате ГГГГММДД в строке " + j + " в поле " + FieldName[i] + ". Значение из файла: " + l);
                                    }
                                    break;
                                case "N":
                                    {
                                        l = l.Trim().Replace(",", ".");
                                        string val = "";
                                        foreach (char c in l.ToCharArray()) if (numericValidChars.Contains(c)) val += c; else break;

                                        if (FieldDigs[i] == 0)
                                        {
                                            try
                                            {
                                                R[i] = long.Parse(val, nfi);
                                            }
                                            catch
                                            {
                                                tag = -1;
                                                throw new Exception("Ожидалось целое число в строке " + j + " в поле " + FieldName[i] + ". Значение из файла: " + val);
                                            }
                                        }
                                        else
                                        {
                                            try
                                            {
                                                R[i] = decimal.Parse(val, nfi);
                                            }
                                            catch
                                            {
                                                tag = -1;
                                                throw new Exception("Ожидалось вещественное число в строке " + j + " в поле " + FieldName[i] + ". Значение из файла: " + val);
                                            }
                                        }
                                        break;
                                    }
                                case "F": R[i] = double.Parse(l.Trim(), nfi); break;
                                default: R[i] = l; break;
                            }
                        else
                            R[i] = DBNull.Value;
                    }
                    if (delPriznak == 32)
                        dt.Rows.Add(R);
                }
                dt.EndLoadData();
                fs.Close();
                return dt;
            }
            catch (Exception e)
            {
                ret.result = false;
                if (tag < 0) ret.text = e.Message;
                else ret.text = "Ошибка конвертации DBF файла";
                ret.tag = tag;
                MonitorLog.WriteLog("Ошибка конвертации DBF файла: " + e.Message, MonitorLog.typelog.Error, true);
                return null;
            }
        }


        /// <summary>
        ///  заполняем макимальную разницу показаний по одной услуге для отдельного банка
        /// </summary>
        /// <param name="pref">префикс банка</param>
        /// <param name="param">код параметра</param>
        /// <returns></returns>
        private decimal GetMaxDiffBetweenValuesOneServ(string pref, int param)
        {
            Returns ret;
            string sql =
                " SELECT " + DBManager.sNvlWord + "(max(p.val_prm " + DBManager.sConvToNum + "), 10000) " +
                " FROM " + pref + DBManager.sKernelAliasRest + "prm_name pn " +
                " LEFT JOIN " + pref + DBManager.sDataAliasRest + "prm_10 p ON pn.nzp_prm = p.nzp_prm " +
                " WHERE pn.nzp_prm = " + param;

            object obj = ExecScalar(sql, out ret, true);
            decimal result = (ret.result && obj != DBNull.Value) ? Convert.ToDecimal(obj) : 10000m;
            return result;
        }


        /// <summary>
        /// заполняем макимальную разницу показаний для отдельного банка
        /// </summary>
        /// <param name="pref">префикс банка</param>
        /// <returns></returns>
        private Dictionary<int, decimal> GetMaxDiffBetweenValuesDictForOneBank(string pref)
        {
            var resDictionary = new Dictionary<int, decimal>
            {
                {25, GetMaxDiffBetweenValuesOneServ(pref, 2081)},
                {9, GetMaxDiffBetweenValuesOneServ(pref, 2082)},
                {6, GetMaxDiffBetweenValuesOneServ(pref, 2083)},
                {10, GetMaxDiffBetweenValuesOneServ(pref, 2084)}
            };
            return resDictionary;
        }


        /// <summary>
        /// заполняем максимальную разницу показаний по верхнему и всем локальным банкам
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, Dictionary<int, decimal>> GetMaxDiffBetweenValuesDict()
        {
            var resDictionary = new Dictionary<string, Dictionary<int, decimal>>();
            string sql =
                " SELECT DISTINCT trim(bd_kernel) AS bd_kernel " +
                " FROM " + Points.Pref + DBManager.sKernelAliasRest + "s_point";
            var pref = ExecSQLToTable(sql);
            foreach (DataRow r in pref.Rows)
                resDictionary.Add(r["bd_kernel"].ToString(),
                    GetMaxDiffBetweenValuesDictForOneBank(r["bd_kernel"].ToString()));
            return resDictionary;
        }


        /// <summary>
        /// Получить максимально допустимую разницу между показаниями ПУ
        /// </summary>
        /// <param name="pref"></param>
        /// <param name="nzpServ"></param>
        /// <returns></returns>
        public decimal GetDiffByServ(string pref, int nzpServ)
        {
            if (_maxDiffBetweenValues.ContainsKey(pref))
            {
                if (_maxDiffBetweenValues[pref].ContainsKey(nzpServ))
                {
                    return _maxDiffBetweenValues[pref][nzpServ];
                }


            }
            else if (_maxDiffBetweenValues.ContainsKey(Points.Pref))
            {
                if (_maxDiffBetweenValues[Points.Pref].ContainsKey(nzpServ))
                {
                    return _maxDiffBetweenValues[Points.Pref][nzpServ];
                }
            }
            return 1000000;

        }

        /// <summary>
        /// Определенеи имени услуги
        /// </summary>
        /// <param name="nzpServ">Код услуги</param>
        /// <returns></returns>
        private string GetServName(int nzpServ)
        {
            switch (nzpServ)
            {
                case 6:
                    return "Холодная вода";
                case 9:
                    return "Горячая вода";
                case 25:
                    return "Дневное электроснабжение";
                case 210:
                    return "Ночное электроснабжение";
            }
            return "Неопределенная услуга";
        }

        protected override NameValueCollection GetNameValueCollection(FilesImported finder)
        {
            DateTime? date_load = null;

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
                        LoadPayOrIpuType = finder.LoadPayOrIpuType,
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

            string sqlStr = " INSERT INTO " + Points.Pref + DBManager.sDataAliasRest + "simple_load " +
                            " (file_name, nzp, month_, year_, " +
                            " created_by, created_on, tip, download_status ) " +
                            " VALUES " +
                            " ( '" + FileName + "'," +
                            0 + "," + DateLoad.Month + "," + DateLoad.Year + "," +
                            ReportParams.User.nzp_user + ", " + DBManager.sCurDateTime + ", " + (int)SimpLoadTypeFile + "," + 2 + " )";

            ExecSQL(sqlStr);
            NzpLoad = GetSerialValue();
            sqlStr = " INSERT INTO " + Points.Pref + DBManager.sDataAliasRest + "simple_pay_file" +
                     " (nzp_load, data_type) values (" + NzpLoad + ", " + (int) LoadPayOrIpuType + ");";
            ExecSQL(sqlStr);
        }

        
    }
}
