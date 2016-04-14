using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Bars.KP50.Load.Obninsk.CountersLoad.Interfaces;
using Bars.KP50.Load.Obninsk.Progress.Interfaces;
using Bars.KP50.Report;
using Bars.KP50.Utils.Annotations;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.Load.Obninsk
{
    /// <summary>
    /// Загрузка счетчиков из файла, в котором содержится nzp_counter
    /// </summary>
    public class CountersRSOByNzpCounter : ConnectionToDB, ICountersLoad
    {
        private IBaseLoadProtokol protokol;
        private IMustCalc mustCalcCnt;
        private IPermissibleDiffValCounters diffValCounters;
        private IProgressWork<ProgressEventArgs> progress;
        private Int32 nzpLoadFile;
        private Int32 nzp_user;

        public string Name { get { return "Загрузка показаний счетчиков"; }}
        public string Description { get { return "Загрузка показаний счетчиков"; } }

        public CountersRSOByNzpCounter(IMustCalc mustCalc, IPermissibleDiffValCounters permissibleDiffVal,
            IProgressWork<ProgressEventArgs> progress)
        {
            mustCalcCnt = mustCalc;
            diffValCounters = permissibleDiffVal;
            this.progress = progress;
        }
        public void Init(IDbConnection connection, IBaseLoadProtokol protokol,[NotNull] EventHandler<ProgressEventArgs> handler, int nzp_user)
        {
            Connection = connection;
            this.protokol = protokol;
            this.nzp_user = nzp_user;
            progress.RaiseProgressEvent += handler;
        }
       
        /// <summary>
        /// Преобразует потоковые данные в читаьельный вид
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public string ChangeFileCodePage(Stream fs, out Returns ret)
        {
            ret = STCLINE.KP50.Global.Utils.InitReturns();
            ret.result = false;
            var buffer = new byte[fs.Length];
            fs.Position = 0;
            fs.Read(buffer, 0, buffer.Length);

            //win кодировка
            string st1251 = System.Text.Encoding.GetEncoding(1251).GetString(buffer);

            if (System.Text.RegularExpressions.Regex.Replace(st1251, "[0-9А-яЁё|.#_№@*,a-zA-Z;\n\r-!:-]", "").Trim().Length == 0)
            {
                //Кодировка 1251
                ret.result = true;
                return st1251;
            }
            string st866 = System.Text.Encoding.GetEncoding(866).GetString(buffer);
            if (System.Text.RegularExpressions.Regex.Replace(st866,
                "[0-9А-яЁё|.#_№@*,a-zA-Z;\n\r-!:-]", "").Trim().Length == 0)
            {
                //Кодировка 866
                ret.result = true;
                return st866;
            }
            string st65001 = System.Text.Encoding.GetEncoding(65001).GetString(buffer);
            if (System.Text.RegularExpressions.Regex.Replace(st65001,
                "[0-9А-яЁё|.#_№@*,a-zA-Z;\n\r-!:-]", "").Trim().Length == 0)
            {
                //Кодировка 866
                ret.result = true;
                return st65001;
            }

            else
            {
                //  Master.ShowMessage(MessageTemplate.MsgType.Information,
                //"<b>Файл " + FileUploadPack.FileName +
                //" не является пачкой оплат в установленном формате версии </b> ");
                ret.result = true;
                return st866;
            }


        }

        /// <summary>
        /// Разбирает строки файла
        /// </summary>
        /// <param name="fileNameExthPath"></param>
        /// <param name="nzpLoad"></param>
        public bool ParseFileRows(string fileNameExthPath, int nzpLoad)
        {
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();
            int numParsingRow = 0; // номер текущей строки, которая разбирается
            bool resultParse = true;
            try
            {
                var extension = System.IO.Path.GetExtension(fileNameExthPath);
                if (extension != ".csv")
                {
                    throw new InvalidOperationException("Загружаемый файл имеет расширение " + extension + ". Ожидается файл с расширением csv");
                }
                // это поместить в метод Load
                CultureInfo ci = (CultureInfo) Thread.CurrentThread.CurrentCulture.Clone();
                ci.NumberFormat.NumberDecimalSeparator = ".";
                Thread.CurrentThread.CurrentCulture = ci;

                #region Cчитывание данных из файла

                // считывание данных из файла
                FileStream fs = new FileStream(fileNameExthPath, FileMode.Open, FileAccess.Read);
                var memstr = new MemoryStream();
                var buffers = new byte[fs.Length];
                fs.Read(buffers, 0, buffers.Length);
                memstr.Write(buffers, 0, buffers.Length);
                var fst = ChangeFileCodePage(memstr, out ret);
                // преобразовать данные из файла в массив строк
                string[] listFileRows = fst.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                // при пустом файле выходим
                if (listFileRows.Length == 0)
                {
                    throw new InvalidOperationException("Файл не содержит данных");
                }

                #endregion

                #region Разбор строк файла

                nzpLoadFile = nzpLoad;
                // Для каждой строки файла
                foreach (string fileRow in listFileRows)
                {
                    numParsingRow++;
                    if (String.IsNullOrWhiteSpace(fileRow))
                    {
                        continue;
                    }
                    // поля одной строки
                    string[] rowFields = fileRow.Split(';');
                    if (rowFields.Length < 7)
                    {
                        resultParse = false;
                        protokol.AddUncorrectedRow("Строка в файле " + numParsingRow + " содержит " + rowFields.Length + " полей, ожидается 7 полей");
                        continue;
                    }
                    PuVals pu = new PuVals();
                    Int64 parsed_pkod = 0;
                    string uk = "";
                    for (int i = 0; i < rowFields.Length; i++)
                    {
                        switch (i)
                        {
                                // наименование УК
                            case 0:
                                if (rowFields[i].Trim().Length > 100)
                                {
                                    resultParse = false;
                                    protokol.AddUncorrectedRow("Строка в файле " + numParsingRow + ". Поле с наименованием УК" +
                                                               " имеет длину " + rowFields[i].Length +
                                                               " Ожидается не более 100 символов ");
                                    break;
                                }
                                uk = rowFields[i].Trim();
                                break;
                                // платежный код 
                            case 1:
                                // проверка на длину
                                var pkodLength = rowFields[i].Trim().Length;
                                if (pkodLength != 10 && pkodLength != 13)
                                {
                                    resultParse = false;
                                    protokol.AddUncorrectedRow("Строка в файле " + numParsingRow + " содержит " +
                                                               " не корректный платежный код. Ожидается число из 10 или 13 цифр ");
                                    break;
                                }
                                if (!Int64.TryParse(rowFields[i], out parsed_pkod))
                                {
                                    protokol.AddUncorrectedRow("Строка в файле " + numParsingRow + " содержит " +
                                                               " не корректный платежный код. Ожидается число из 10 или 13 цифр ");
                                }

                                break;
                                // код счетчика (nzp_counter)
                            case 3:
                                if (String.IsNullOrWhiteSpace(rowFields[i]))
                                {
                                    break;
                                }
                                int parsed_nzp_counter;
                                if (!Int32.TryParse(rowFields[i], out parsed_nzp_counter))
                                {
                                    resultParse = false;
                                    protokol.AddUncorrectedRow("Строка в файле " + numParsingRow + " содержит " +
                                                               " не корректный код счетчика (nzp_counter). Ожидается целое число ");
                                }
                                pu.nzp_counter = parsed_nzp_counter;
                                break;
                                // заводской номер счетчика
                            case 4:
                                pu.num_cnt = rowFields[i].Trim();
                                break;
                                // дата ввода показаний счетчика
                            case 5:
                                DateTime parsed_dat_uchet;
                                if (!DateTime.TryParse(rowFields[i], out parsed_dat_uchet))
                                {
                                    resultParse = false;
                                    protokol.AddUncorrectedRow("Строка в файле " + numParsingRow + " содержит " +
                                                               " не корректную дату ввода показаний. Ожидается дата в формате дд.мм.гггг ");
                                }
                                pu.dat_uchet = parsed_dat_uchet.ToShortDateString();
                                break;
                                // показание счетчика
                            case 6:
                                Decimal parsed_val_cnt;
                                if (!Decimal.TryParse(rowFields[i], out parsed_val_cnt))
                                {
                                    resultParse = false;
                                    protokol.AddUncorrectedRow("Строка в файле " + numParsingRow + " содержит " +
                                                               " некорректное показание счетчика. Ожидается число, с разделителем '.'");
                                }
                                pu.val_cnt = parsed_val_cnt;
                                break;

                        }
                    }
                    // Сохраняем одну строку во временной таблице
                    string sql = " insert into " + Points.Pref + DBManager.sDataAliasRest +
                                 " simple_counters(nzp_load, uk, date_pay, paccount, nzp_counter, num_cnt, val_cnt)" +
                                 " values(" + nzpLoad + ",'" + uk + "','" + pu.dat_uchet + "'," +
                                 parsed_pkod + "," + pu.nzp_counter + ",'" + pu.num_cnt + "'," + pu.val_cnt + ")";
                    ExecSQL(sql);
                }

                # endregion
            }
            catch (InvalidOperationException ex)
            {
                resultParse = false;
                protokol.AddUncorrectedRow(ex.Message);
                MonitorLog.WriteLog("Ошибка счетчиков от РСО. " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                    (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
            catch (ReportException)
            {
                resultParse = false;
                protokol.AddUncorrectedRow("Ошибка сохранения " + numParsingRow + " строки из файла, см. логи");
            }
            catch (Exception ex)
            {
                resultParse = false;
                protokol.AddUncorrectedRow("Неорректная строка c номером " + numParsingRow + " в исходном файле, см. логи");
                MonitorLog.WriteLog("Ошибка счетчиков от РСО. Наименование файла "+Path.GetFileName(fileNameExthPath)+". Номер строки в файле "+numParsingRow+". " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                    (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
            return resultParse;
        }
        /// <summary>
        /// Сохранение в базе и выставление периодов перерасчета
        /// </summary>
        public void SaveCountersInDB(string userLoadedFileName)
        {
            try
            {
                // определить хорошие и плохие счетчики
                determineGoodAndBadCounters();
                // положить плохие счетчики в протокол
                passBadCounterToProtokol();
                // сохранить хорошие и выставить периоды перерасчета
                saveGoodCounters(userLoadedFileName);
            }
            catch (InvalidOperationException ex)
            {
                protokol.AddUncorrectedRow(ex.Message);
                MonitorLog.WriteLog("Ошибка счетчиков от РСО. " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                    (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
            catch (ReportException ex)
            {
                protokol.AddUncorrectedRow("Ошибка выполнения операции в базе данных, см. логи");
            }
            catch (Exception ex)
            {
                protokol.AddUncorrectedRow("Ошибка выполненя оперции при сохранении счетчиков в базе, см. логи");
                MonitorLog.WriteLog("Ошибка счетчиков от РСО. " + System.Reflection.MethodBase.GetCurrentMethod().Name +
                                    (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
        }

        /// <summary>
        /// Выявляет хорошие и плохие счетчики
        /// </summary>
        void determineGoodAndBadCounters()
        {
            #region Собираем данные во временную таблицу
            string sql = "Create temp table t_counts (nzp_kvar integer, " +
                         " nzp_serv integer," +
                         " nzp_wp integer," +
                         " bad_cnt integer default 0," +
                         " date_pay Date," +
                         " last_dat_uchet Date," +
                         " num_cnt char(255)," +
                         " isusenzpcounter boolean default 'f'," +
                         " paccount " + DBManager.sDecimalType + "(13,0)," +
                         " val_cnt " + DBManager.sDecimalType + "(14,4)," +
                         " last_val " + DBManager.sDecimalType + "(14,4)," +
                         " rashod " + DBManager.sDecimalType + "(14,4)," +
                         " nzp_counter integer)" + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " insert into t_counts(nzp_kvar, nzp_counter, nzp_wp, paccount, date_pay, val_cnt, num_cnt) " +
                  " select k.nzp_kvar, a.nzp_counter, k.nzp_wp, a.paccount, " +
                  " date_trunc('month',a.date_pay + interval '1 month')  as date_pay,  val_cnt, num_cnt " +
                  " from " + Points.Pref + DBManager.sDataAliasRest + "simple_counters a left outer join " +
                  "      " + Points.Pref + DBManager.sDataAliasRest + "kvar k " +
                  " on a.paccount=k.pkod " +
                  " where nzp_load = " + nzpLoadFile + " and val_cnt<>0 ";
            ExecSQL(sql);

            sql = " select a.nzp_wp, p.bd_kernel as pref " +
                  " from t_counts a, " + Points.Pref + DBManager.sKernelAliasRest + "s_point p" +
                  " where a.nzp_wp=p.nzp_wp " +
                  " group by 1,2";
            DataTable preflist = ExecSQLToTable(sql);
            List<int> list_nzp_wp = preflist.Select().Select(r => (int)r["nzp_wp"]).ToList();
            diffValCounters.Init(Connection, list_nzp_wp);
            foreach (DataRow dr in preflist.Rows)
            {
                string pref = dr["pref"].ToString().Trim();
                // определение услуги
                string counters_spis_table = pref + DBManager.sDataAliasRest + "counters_spis";
                // выявить счетчики, по которым нет nzp_counter и нет num_cnt
                sql = "update t_counts set bad_cnt=7 where nzp_wp = " + dr["nzp_wp"] + " and " +
                    DBManager.sNvlWord + "(nzp_counter,0)=0  and  " + DBManager.sNvlWord + "(num_cnt,'')=''";
                ExecSQL(sql);
                // выявить счетчики, nzp_counter которых не найден в базе
                sql = "update t_counts t set bad_cnt=8 where nzp_wp = " + dr["nzp_wp"] + " and " +
                    " not exists (select 1 from " + counters_spis_table + " cs " +
                    " where  cs.is_actual<>100 and t.nzp_counter=cs.nzp_counter) and " + DBManager.sNvlWord + "(t.nzp_counter,0)>0 " +
                      "and bad_cnt=0";
                ExecSQL(sql);
                // выявить счетчики c отсутствущим nzp_counter, num_cnt которых не найден в базе
                sql = "update t_counts t set bad_cnt=9 where nzp_wp = " + dr["nzp_wp"] + " and " +
                    " not exists (select 1 from " + counters_spis_table + " cs " +
                    " where  cs.is_actual<>100 and t.num_cnt=cs.num_cnt) and " + DBManager.sNvlWord + "(t.nzp_counter,0)=0 and bad_cnt=0";
                ExecSQL(sql);
                // счетчики, для которых отсутствуют nzp_counter и для которых в базе найден num_cnt, но определен не однозначно (т.е. с количеством больше 1)
                sql = "update t_counts t set bad_cnt=10 where nzp_wp = " + dr["nzp_wp"] + " and " +
    "  (select count(*) from " + counters_spis_table + " cs " +
    " where t.num_cnt=cs.num_cnt)>1 and " + DBManager.sNvlWord + "(t.nzp_counter,0)=0 and bad_cnt=0";
                ExecSQL(sql);
                // обновить nzp_counter для счетчиков, у которых в базе найден num_cnt и у которых nzp_counter=0
                sql = "update t_counts t set nzp_counter=(select nzp_counter from  "+counters_spis_table+" cs " +
                      "where cs.is_actual<>100 and t.num_cnt=cs.num_cnt) " +
                      "where nzp_wp = " + dr["nzp_wp"] + " and " +  DBManager.sNvlWord + "(t.nzp_counter,0)=0 and bad_cnt=0";
                ExecSQL(sql);
                // определение услуги
                sql = "update t_counts t set nzp_serv = (select max(nzp_serv) from " + counters_spis_table + " cs " +
                      "where t.nzp_counter=cs.nzp_counter and cs.is_actual<>100) where bad_cnt=0 and nzp_wp=" + dr["nzp_wp"];
                ExecSQL(sql);
                // дата последнего введенного показания
                sql = " update t_counts set last_dat_uchet = (select max(dat_uchet) " +
                      " from " + dr["pref"].ToString().Trim() + DBManager.sDataAliasRest + "counters s" +
                      " where t_counts.nzp_counter=s.nzp_counter and s.is_actual <> 100)" +
                      " where bad_cnt=0 and nzp_wp=" + dr["nzp_wp"];
                ExecSQL(sql);
                // последнее введенное значение 
                sql = " update t_counts t set last_val = (select max(s.val_cnt) " +
                      " from " + pref + DBManager.sDataAliasRest + "counters s  " +
                      " where t.last_dat_uchet=s.dat_uchet and t.last_dat_uchet is not null and  t.nzp_counter=s.nzp_counter and s.is_actual <> 100)" +
                      " where bad_cnt=0 and nzp_wp=" + dr["nzp_wp"];
                ExecSQL(sql);
                // счетчики, по которым уже есть показания за дату, указанную в файле, т.е. у которых max(dat_uchet)>=dat_pay
                sql = " update t_counts set bad_cnt = 1 " +
                      " where 1=(select max(1) " +
                      " from " + dr["pref"].ToString().Trim() + DBManager.sDataAliasRest + "counters s" +
                      " where t_counts.nzp_counter=s.nzp_counter and s.dat_uchet >= t_counts.date_pay  and is_actual <> 100)" +
                      " and bad_cnt=0 and nzp_wp=" + dr["nzp_wp"];
                ExecSQL(sql);
                sql = " update t_counts set rashod = val_cnt-last_val where last_val is not null ";
                ExecSQL(sql);
                // извлечь те bad счетчики, у которых dat_pay больше текущего расчетного месяца+1
                RecordMonth rm = Points.GetCalcMonth(new CalcMonthParams(pref));
                sql = " update t_counts set bad_cnt = 5 where date_pay>'" + rm.RecordDateTime.AddMonths(1).ToShortDateString() + "' and bad_cnt=0 and nzp_wp=" + dr["nzp_wp"];
                ExecSQL(sql);
                // счетчики с отрицательным расходом 
                #region Проверки

                sql = " update t_counts set bad_cnt = 2 where bad_cnt=0 and rashod<0 and nzp_wp=" + dr["nzp_wp"];
                ExecSQL(sql);

                sql = " update t_counts set bad_cnt = 3 " +
                      " where rashod>" + diffValCounters.GetDiffByServ(pref, 9) + " and bad_cnt=0 and nzp_serv=9 and nzp_wp=" + dr["nzp_wp"];
                ExecSQL(sql);

                sql = " update t_counts set bad_cnt = 3 " +
                      " where rashod>" + diffValCounters.GetDiffByServ(pref, 6) + " and bad_cnt=0 and nzp_serv=6 and nzp_wp=" + dr["nzp_wp"];
                ExecSQL(sql);

                sql = " update t_counts set bad_cnt = 3 " +
                      " where rashod>" + diffValCounters.GetDiffByServ(pref, 25) + " and bad_cnt=0 and nzp_serv=25 and nzp_wp=" + dr["nzp_wp"];
                ExecSQL(sql);

                sql = " update t_counts set bad_cnt = 3 " +
                      " where rashod>" + diffValCounters.GetDiffByServ(pref, 210) + " and bad_cnt=0 and nzp_serv=210 and nzp_wp=" + dr["nzp_wp"];
                ExecSQL(sql);
                sql = "update t_counts set isusenzpcounter= 't' where bad_cnt=0 and " + DBManager.sNvlWord + "(nzp_counter,0)>0 ";
                ExecSQL(sql);

                #endregion
            }
            //sql = " update t_counts set bad_cnt = 4 where nzp_counter is null";
            
            #endregion
        }
        /// <summary>
        /// Перенести в протокол проблемные счетчики
        /// </summary>
        void passBadCounterToProtokol()
        {
            #region Выбираем проблемные счетчики

           string sql = " select * from t_counts  " +
                  " where bad_cnt >0 ";
            var badCounterTable = ExecSQLToTable(sql);
            foreach (DataRow dr in badCounterTable.Rows)
            {
                string servName="не определена";
                if (dr["nzp_serv"] != DBNull.Value)
                {
                    servName = diffValCounters.GetServName((int)dr["nzp_serv"]);
                }
                string val_cnt="";
                if (dr["val_cnt"] != DBNull.Value)
                {
                    val_cnt = dr["val_cnt"].ToString().Trim();
                }
                string num_cnt = "";
                if (dr["num_cnt"] != DBNull.Value)
                {
                    num_cnt = dr["num_cnt"].ToString().Trim();
                }
                string paccount = "";
                if (dr["paccount"] != DBNull.Value)
                {
                    paccount = dr["paccount"].ToString().Trim();
                }

                if (dr["nzp_kvar"] == DBNull.Value)
                {
                    protokol.AddUnrecognisedRow("Платежный код  " + paccount +
                                                " не зарегистрирован в системе " +
                                                " Счетчик по услуге " + servName +
                                                " показание " + val_cnt);
                    continue;
                }

                    string s = String.Empty;
                    switch ((int)dr["bad_cnt"])
                    {
                        case 1:
                            string date_pay = "";
                            if (dr["date_pay"] != DBNull.Value)
                            {
                                date_pay = dr["date_pay"].ToString().Trim();
                            }
                            s = "На дату " + date_pay + " уже есть внесенное показание";
                            break;
                        case 2:
                            string last_val_cnt="";
                            if (dr["last_val"] != DBNull.Value)
                            {
                                last_val_cnt = dr["last_val"].ToString().Trim();
                            }
                            s = "Предыдущее значение счетчика " + last_val_cnt + " больше текущего ";
                            break;
                        case 3:
                            string rashod = "";
                            if (dr["rashod"] != DBNull.Value)
                            {
                                rashod = dr["rashod"].ToString().Trim();
                            }
                            s = "Превышен лимит расхода по услуге, текущий расход " + rashod;
                            break;
                        case 5:
                            s = "Дата оплаты больше текущего расчетного месяца";
                            break;
                        case 7:
                            s = "В файле не заполнены поля 'код счетчика' и 'зав. №' ";
                            break;
                        case 8:
                            string nzp_counter = "";
                            if (dr["nzp_counter"] != DBNull.Value)
                            {
                                nzp_counter = dr["nzp_counter"].ToString().Trim();
                            }
                            s = "В базе не найден счетчик с кодом " + nzp_counter;
                            break;
                        case 9:
                            s = "В базе не найден счетчик с заводским номером " + num_cnt;
                            break;
                        case 10:
                            s = "В базе найдено несколько счетчиков с заводским номером " + num_cnt;
                            break;

                    }
                    protokol.AddUnrecognisedRow(s + " для счетчика по услуге " + servName +
                                        " показание " + val_cnt +
                                        " платежный код  " + paccount + ". Данные не загружены.");

            }

            #endregion
        }
        /// <summary>
        /// Сохранить счетчики, по которым не было ошибок в базе и выставить им периоды перерасчета
        /// </summary>
        void saveGoodCounters(string userLoadedFileName)
        {
            
            #region Добавляем хорошие счетчики
            string sql = " select distinct nzp_wp from t_counts  " +
                 " where bad_cnt =0 ";
            var prefList = ExecSQLToTable(sql);
            progress.Init(prefList.Rows.Count);
            mustCalcCnt.Init(Connection, "t_counts", nzp_user);
            int countBanks=0;
            foreach (DataRow dr in prefList.Rows)
            {
                string pref = Points.GetPref((int) dr["nzp_wp"]);
                string localData = pref + DBManager.sDataAliasRest;
                sql = " insert into " + localData + "counters " +
                      " (nzp_counter, num_ls, nzp_kvar, nzp_cnttype, is_actual, nzp_serv, " +
                      " nzp_user, num_cnt, val_cnt, dat_uchet, dat_when, ist)" +
                      " select a.nzp_counter, k.num_ls, a.nzp_kvar, s.nzp_cnttype, 1, a.nzp_serv, " +
                      nzp_user + ", s.num_cnt, a.val_cnt, date_pay, " +
                      "" + DBManager.sCurDate + ", 6 " +
                      " from t_counts a, " + Points.Pref + DBManager.sDataAliasRest + "kvar k, " + localData + "counters_spis s " +
                      " where a.nzp_kvar=k.nzp_kvar and a.nzp_counter=s.nzp_counter and bad_cnt =0" +
                      " and a.nzp_wp = " + dr["nzp_wp"];
                IntfResultTableType result = ExecSQLDbUtils(sql);
                mustCalcCnt.PrepareGroupMustcalc(pref);
                mustCalcCnt.SetGroupMustCalc("Загрузка счетчиков из файла " + Path.GetFileName(userLoadedFileName) + " от " + DateTime.Now.ToShortDateString());
                protokol.CountInsertedRows += result.resultAffectedRows;
                progress.IncrementProgress(++countBanks);
            }

            #endregion
        }
        /// <summary>
        /// Удалить все временные таблицы
        /// </summary>
        public void Dispose()
        {
            ExecSQL("drop table t_counts", false);
            if (mustCalcCnt != null)
            {
                mustCalcCnt.Dispose();
            }
        }
    }


}

