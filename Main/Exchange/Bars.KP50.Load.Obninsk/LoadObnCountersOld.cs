using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using FastReport;
using STCLINE.KP50;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;
using Bars.KP50.Report;

namespace Bars.KP50.Load.Obninsk
{

   
    /// <summary>Сводный отчет по начислениям для Тулы</summary>
    public class LoadObnCounters : BaseSqlLoad
    {

        public override string Name
        {
            get { return "Загрузка счетчиков"; }
        }

        public override string Description
        {
            get { return "Загрузка счетчиков в простом формате"; }
        }

        protected override byte[] Template
        {
            get { return null; }
        }

        /// <summary>
        /// Массив максимально-допустимых разниц показаний счетчиков
        /// </summary>
        private Dictionary<string, Dictionary<int, decimal>> _maxDiffBetweenValues;

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

            try
            {
                SetProcessPercent(0, ExcelUtility.Statuses.InProcess);


                #region Загрузка данных из файла
                var fs = new FileStream(TemporaryFileName, FileMode.Open, FileAccess.Read);
                DataTable tableCounters;
                //tableCounters = STCLINE.KP50.Global.Utils.ConvertDBFtoDataTable(fs, "1251", true, out ret);
                if (Points.Region == Regions.Region.Samarskaya_obl)
                {
                    tableCounters = STCLINE.KP50.Global.Utils.ConvertDBFtoDataTable(fs, "1251", true, out ret);
                }
                else
                {
                    tableCounters = STCLINE.KP50.Global.Utils.ConvertDBFtoDataTableFox(fs, "1251", true, out ret);
                }
                fs.Close();
                

                if (!ret.result)
                {

                    MonitorLog.WriteLog("Ошибка разбора файла со счетчиками " + FileName + ret.text,
                        MonitorLog.typelog.Error, 20, 201, true);
                    Protokol.AddComment("Ошибка разбора файла со счетчиками " + FileName + ret.text);
                    return;
                }

                #endregion

              

                //Разбор реестра
                ret = ParseTableCounters(tableCounters);

                if (!ret.result) throw  new UserException(ret.text);

                //записываем данные в систему: pack,pack_ls,pu_vals 
                ret = SaveCountersInBase();
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка записи счетчиков в систему",
                        MonitorLog.typelog.Error, 20, 201, true);

                }
                else
                {
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
                RollbackReestr();
            }
            
        }


      

      


        /// <summary>
        /// Разбор Таблицы со счетчиками и сохранение в базе данных 
        /// </summary>
        /// <param name="tableCounters">Таблица, считанная из файла</param>
        /// <returns></returns>
        private Returns ParseTableCounters(DataTable tableCounters)
        {
            if (tableCounters == null)
            {
                return new Returns(false, "В таблице счетчиков отсуствует записи");
            }

            _rowsCount = tableCounters.Rows.Count;
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();

          
            foreach (DataRow dr in tableCounters.Rows)
            {
                string uk = dr["company"].ToString().Trim();

                string pkod = dr["paccount"].ToString().Trim();
                string s = dr["date_pay"].ToString().Trim();

                DateTime datePay;
                if (!DateTime.TryParse(s, out datePay))
                {
                    Protokol.AddUncorrectedRow("Неорректная дата в исходном файле, ожидается ДД.ММ.ГГГГ: " +
                            " УК = " + uk +
                            ", date_pay = " + s +
                            ", paccount = " + pkod);
                }
                

                foreach (DataColumn dc in tableCounters.Columns)
                {
                    string colName = dc.ColumnName.ToLower();
                    int nzpServ = -1;
                    if (colName == "company" ||
                        colName == "date_pay" ||
                        colName == "paccount") continue;

                    string num = colName.Substring(colName.Length - 1);

                    if (colName.IndexOf("cold", StringComparison.Ordinal) > -1)
                    {
                        nzpServ = 6;
                    }
                    else if (colName.IndexOf("hot", StringComparison.Ordinal) > -1)
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
                        
                    string sql = " insert into " + Points.Pref + DBManager.sDataAliasRest+
                                 " simple_counters(nzp_load, uk, date_pay, paccount, nzp_serv, num, val_cnt)" +
                                 " values(" + NzpLoad + ",'" + uk + "','" + datePay.ToShortDateString() + "'," +
                                 pkod + "," + nzpServ + "," + num + "," + dr[dc.ColumnName] + ")";
                    ExecSQL(sql);
                    }
                    catch (Exception)
                    {

                        Protokol.AddUncorrectedRow("Неорректная строка в исходном файле: "+
                            " УК = "+uk+
                            ", date_pay = "+datePay+
                            ", paccount = "+pkod+
                            ", num ="+num+
                            ", val_cnt = " + dr[dc.ColumnName]);
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Сохранение счетчиков в базе данных
        /// </summary>
        /// <returns></returns>
        private Returns SaveCountersInBase()
        {
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();
            _maxDiffBetweenValues = GetMaxDiffBetweenValuesDict();

            #region Собираем данные во временную таблицу

            string sql = "Create temp table t_counts (nzp_kvar integer, " +
                         " nzp_serv integer," +
                         " nzp_wp integer," +
                         " bad_cnt integer default 0," +
                         " date_pay Date," +
                         " last_dat_uchet Date," +
                         " paccount " + DBManager.sDecimalType + "(13,0)," +
                         " val_cnt " + DBManager.sDecimalType + "(14,4)," +
                         " last_val " + DBManager.sDecimalType + "(14,4)," +
                         " rashod " + DBManager.sDecimalType + "(14,4)," +
                         " nzp_counter integer)" + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " insert into t_counts(nzp_kvar, nzp_wp, paccount, date_pay, nzp_serv, val_cnt) " +
                  " select k.nzp_kvar, k.nzp_wp, a.paccount, " +
                  (DBManager.tableDelimiter == "."
                      ? "date_trunc('month',a.date_pay + interval '1 month')"
                      : "date('01'||'.'||month(dat_saldo)||'.'||year(dat_saldo)) + 1 units month)") +
                  "  as date_pay, " +
                  " a.nzp_serv, sum(val_cnt) as val_cnt " +
                  " from " + Points.Pref + DBManager.sDataAliasRest + "simple_counters a left outer join " +
                  "      " + Points.Pref + DBManager.sDataAliasRest + "kvar k " +
                  " on a.paccount=k.pkod " +
                  " where nzp_load = " + NzpLoad + " and val_cnt<>0 " +
                  " group by 1,2,3,4,5 ";
            ExecSQL(sql);

            sql = " select a.nzp_wp, p.bd_kernel as pref " +
                  " from t_counts a, " + Points.Pref + DBManager.sKernelAliasRest + "s_point p" +
                  " where a.nzp_wp=p.nzp_wp " +
                  " group by 1,2";
            DataTable preflist = ExecSQLToTable(sql);
            foreach (DataRow dr in preflist.Rows)
            {
                // обновление nzp_counter
                sql = " update t_counts set nzp_counter = (select max(nzp_counter) " +
                      " from " + dr["pref"].ToString().Trim() + DBManager.sDataAliasRest + "counters_spis s" +
                      " where t_counts.nzp_kvar=s.nzp " +
                      " and t_counts.nzp_serv=s.nzp_serv)" +
                      " where nzp_wp=" + dr["nzp_wp"];
                ExecSQL(sql);

                // дата последнего введенного показания
                sql = " update t_counts set last_dat_uchet = (select max(dat_uchet) " +
                      " from " + dr["pref"].ToString().Trim() + DBManager.sDataAliasRest + "counters s" +
                      " where t_counts.nzp_counter=s.nzp_counter and s.is_actual <> 100)" +
                      " where nzp_counter is not null and nzp_wp=" + dr["nzp_wp"];
                ExecSQL(sql);
                // последнее введенное значение // bcghfdbnm объединить
                sql = " update t_counts t set last_val = (select max(s.val_cnt) " +
                      " from " + dr["pref"].ToString().Trim() + DBManager.sDataAliasRest + "counters s  " +
                      " where t.last_dat_uchet=s.dat_uchet and t.last_dat_uchet is not null and  t.nzp_counter=s.nzp_counter and s.is_actual <> 100)" +
                      " where nzp_counter is not null and nzp_wp=" + dr["nzp_wp"];
                ExecSQL(sql);
                // счетчики, по которым уже есть показания за дату, указанную в файле, т.е. у которых max(dat_uchet)>=dat_pay
                sql = " update t_counts set bad_cnt = 1 " +
                      " where 1=(select max(1) " +
                      " from " + dr["pref"].ToString().Trim() + DBManager.sDataAliasRest + "counters s" +
                      " where t_counts.nzp_counter=s.nzp_counter and s.dat_uchet >= t_counts.date_pay  and is_actual <> 100)" +
                      " and nzp_counter is not null and nzp_wp=" + dr["nzp_wp"];
                ExecSQL(sql);
                sql = " update t_counts set rashod = val_cnt-last_val where last_val is not null ";
                ExecSQL(sql);
                // извлечь те bad счетчики, у которых dat_pay больше текущего расчетного месяца+1
                RecordMonth rm = Points.GetCalcMonth(new CalcMonthParams(dr["pref"].ToString().Trim()));
                sql = " update t_counts set bad_cnt = 5 where date_pay>'" + rm.RecordDateTime.AddMonths(1).ToShortDateString() + "' and nzp_wp=" + dr["nzp_wp"];
                ExecSQL(sql);
                // счетчики с нулевым расходом

                #region Проверки

                sql = " update t_counts set bad_cnt = 2 where rashod<0 and nzp_wp=" + dr["nzp_wp"];
                ExecSQL(sql);

                sql = " update t_counts set bad_cnt = 3 " +
                      " where rashod>" + GetDiffByServ(dr["pref"].ToString().Trim(), 9) + " and nzp_serv=9 and nzp_wp=" + dr["nzp_wp"];
                ExecSQL(sql);

                sql = " update t_counts set bad_cnt = 3 " +
                      " where rashod>" + GetDiffByServ(dr["pref"].ToString().Trim(), 6) + " and nzp_serv=6 and nzp_wp=" + dr["nzp_wp"];
                ExecSQL(sql);

                sql = " update t_counts set bad_cnt = 3 " +
                      " where rashod>" + GetDiffByServ(dr["pref"].ToString().Trim(), 25) + " and nzp_serv=25 and nzp_wp=" + dr["nzp_wp"];
                ExecSQL(sql);

                sql = " update t_counts set bad_cnt = 3 " +
                      " where rashod>" + GetDiffByServ(dr["pref"].ToString().Trim(), 210) + " and nzp_serv=210 and nzp_wp=" + dr["nzp_wp"];
                ExecSQL(sql);

                #endregion
            }
            sql = " update t_counts set bad_cnt = 4 where nzp_counter is null";
            ExecSQL(sql);


            #endregion

            #region Выбираем проблеммные счетчики

            sql = " select * from t_counts  " +
                  " where bad_cnt >0 ";
            var badCounterTable = ExecSQLToTable(sql);
            foreach (DataRow dr in badCounterTable.Rows)
            {
                if (dr["nzp_kvar"] == DBNull.Value)
                    Protokol.AddUnrecognisedRow("Платежный код  " + dr["paccount"] +
                                                " не зарегистрирован в системе " +
                                                " Счетчик по услуге " + GetServName((int)dr["nzp_serv"]) +
                                                " показание " + dr["val_cnt"]);
                else if (dr["nzp_counter"] == DBNull.Value)
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
                        case 5:
                            s = "Дата оплаты больше текущего расчетного месяца";
                            break;

                    }
                    Protokol.AddComment(s + " для счетчика по услуге " + GetServName((int)dr["nzp_serv"]) +
                                        " показание " + dr["val_cnt"] +
                                        ", данные не загружены." + " платежный код  " + dr["paccount"]);



                }
            }

            #endregion

            #region Добавляем хорошие счетчики

            foreach (DataRow dr in preflist.Rows)
            {
                string localData = dr["pref"].ToString().Trim() + DBManager.sDataAliasRest;
                sql = " insert into " + localData + "counters " +
                      " (nzp_counter, num_ls, nzp_kvar, nzp_cnttype, is_actual, nzp_serv, " +
                      " nzp_user, num_cnt, val_cnt, dat_uchet, dat_when, ist)" +
                      " select a.nzp_counter, k.num_ls, a.nzp_kvar, s.nzp_cnttype, 1, a.nzp_serv, " +
                      ReportParams.User.nzp_user + ", s.num_cnt, a.val_cnt, date_pay, " +
                      "" + DBManager.sCurDate + ", 6 " +
                      " from t_counts a, " + localData + "kvar k, " + localData + "counters_spis s " +
                      " where a.nzp_kvar=k.nzp_kvar and a.nzp_counter=s.nzp_counter and bad_cnt =0" +
                      " and nzp_wp = " + dr["nzp_wp"];
                ExecSQL(sql, true);
                // выставить признак перерасчета
                ret = checkNeedMustCalc(dr["pref"].ToString().Trim(), (int)dr["nzp_wp"]);
                if (!ret.result) return ret;
            }

            #endregion

            ExecSQL("drop table t_counts");
            return ret;
        }

        /// <summary>
        /// Проверяет и выставляет период перерасчета в случае необходимости
        /// </summary>
        /// <param name="pref"></param>
        /// <param name="nzp_wp"></param>
        private Returns checkNeedMustCalc(string pref, int nzp_wp)
        {
            RecordMonth rm = Points.GetCalcMonth(new CalcMonthParams(pref));
            string sql = " select t.nzp_counter, t.nzp_kvar, t.nzp_serv, date_pay, last_dat_uchet, s.service_name " +
                         " from t_counts t left outer join " + Points.Pref + DBManager.sKernelAliasRest + "services s on t.nzp_serv=s.nzp_serv where " +
            " bad_cnt =0 and last_dat_uchet is not null  " +
                         " and nzp_wp = " + nzp_wp;
            IDataReader reader;
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();
            ExecRead(out reader, sql, true);
            try
            {

                DateTime datePay;
                DateTime lastDateUchet;
                DateTime must_calc_po = new DateTime();
                DateTime must_calc_s = new DateTime();
                DbMustCalcNew db = new DbMustCalcNew(Connection);
                while (reader.Read())
                {
                    // если нет даты учета, ничего выставлять не нужно
                    if (reader["last_dat_uchet"] == DBNull.Value) continue; // период перерасчета выставлять не нужно
                    if (reader["nzp_counter"] == DBNull.Value) continue;
                    if (reader["nzp_kvar"] == DBNull.Value) continue;
                    if (reader["nzp_serv"] == DBNull.Value) continue;
                    int nzp_counter = (int)reader["nzp_counter"];
                    int nzp_kvar = (int)reader["nzp_kvar"];
                    int nzp_serv = (int)reader["nzp_serv"];
                    string service_name = "";
                    if (reader["service_name"] != DBNull.Value)
                    {
                        service_name = reader["service_name"].ToString();
                    }
                    if (!DateTime.TryParse(reader["last_dat_uchet"].ToString(), out lastDateUchet))
                    {
                        Protokol.AddUncorrectedRow("У лицевого счета №" + nzp_kvar + ", счетчик №" + nzp_counter + ", услуга " + service_name + " дата последнего введенного показания указана некорректно");
                        continue;
                    }
                    if (reader["date_pay"] == DBNull.Value || !DateTime.TryParse(reader["date_pay"].ToString(), out datePay))
                    {
                        Protokol.AddUncorrectedRow("У лицевого счета №" + nzp_kvar + ", счетчик №" + nzp_counter + ", услуга " + service_name + "  указана некорректная дата оплаты");
                        continue;
                    }
                    if (lastDateUchet >= datePay) continue;
                    must_calc_s = lastDateUchet;
                    // если дата из файла  равна дате текущего расчетного месяца +1 м
                    if (datePay == rm.RecordDateTime.AddMonths(1))
                    {
                        // если разница между датой из файла и датой последней даты учета составляет 2 месяца
                        if (datePay.Year > lastDateUchet.Year ||
                            (datePay.Year == lastDateUchet.Year && datePay.Month - lastDateUchet.Month > 1))
                        {
                            must_calc_po = datePay.AddMonths(-1).AddDays(-1);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    // если дата из файла не равна дате текущего  расчетного месяца
                    else
                    {
                        // конечная будет последним числом месяца, предшествующей дате оплаты
                        must_calc_po = datePay.AddDays(-1);
                    }
                    string database = pref + "_" + "data";
                    MustCalcTable mustCalcTable = new MustCalcTable();
                    mustCalcTable.DatS = must_calc_s;
                    mustCalcTable.DatPo = must_calc_po;
                    mustCalcTable.Reason = MustCalcReasons.Counter;
                    mustCalcTable.NzpServ = nzp_serv;
                    mustCalcTable.NzpSupp = 0;
                    mustCalcTable.NzpKvar = nzp_kvar;
                    mustCalcTable.Month = rm.month_;
                    mustCalcTable.Year = rm.year_;
                    mustCalcTable.Kod2 = 0;
                    mustCalcTable.NzpUser = Nzp_user;
                    mustCalcTable.Comment = " Загрузка расхода по счетчикам из файла " + Path.GetFileName(TemporaryFileName);
                    ret = db.InsertReason(database, mustCalcTable);
                    if (!ret.result)
                    {
                        Protokol.AddUncorrectedRow("Ошибка выставления периода перерасчета для ЛС №" + nzp_kvar + " и услуги " + service_name + ": " + ret.text);
                        return ret;
                    }
                    if (ret.tag == -1)
                    {
                        Protokol.AddUncorrectedRow("Период перерасчета для ЛС №" + nzp_kvar + " с услугой " + service_name + " уже существует");
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Загрузка расхода по счетчикам. " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }
            return ret;
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
                    return "Дневно электроснабжение";
                case 210:
                    return "Ночное электроснабжение";
            }
            return "Неопределенная услуга";
        }

        /// <summary>
        /// Откат 
        /// </summary>
        private void RollbackReestr()
        {

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
                {210, GetMaxDiffBetweenValuesOneServ(pref, 2081)},
                {9, GetMaxDiffBetweenValuesOneServ(pref, 2082)},
                {6, GetMaxDiffBetweenValuesOneServ(pref, 2083)},
                {10, GetMaxDiffBetweenValuesOneServ(pref, 2084)}
            };
            return resDictionary;
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
                " LEFT JOIN " + pref + DBManager.sDataAliasRest + "prm_10 p ON pn.nzp_prm = p.nzp_prm  and p.is_actual=1 " +
                " WHERE pn.nzp_prm = " + param;

            object obj = ExecScalar(sql, out ret, true);
            decimal result = (ret.result && obj != DBNull.Value) ? Convert.ToDecimal(obj) : 10000m;
            return result;
        }



        /// <summary>
        /// заполняем максимальную разницу показаний по верхнему и всем локальным банкам
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, Dictionary<int, decimal>> GetMaxDiffBetweenValuesDict()
        {
            var resDictionary = new Dictionary<string, Dictionary<int, decimal>>();
            string sql =
                " SELECT distinct trim(bd_kernel) as bd_kernel " +
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

            var rep = new FastReport.Report();


            if (Protokol.UnrecognizedRows.Rows.Count > 0 || Protokol.Comments.Rows.Count > 0)
            {
                Protokol.SetProcent(100, ExcelUtility.Statuses.Failed);
                statusName = "Загружено с ошибками";
            }
            if (Protokol.UncorrectRows.Rows.Count > 0)
            {
                statusName = "Загружено с ошибками";
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
                fileName = InputOutput.SaveOutputFile(Constants.ExcelDir + fileName);
            }

            ProtocolFileName = fileName;


            ExecSQL("UPDATE " + Points.Pref + DBManager.sDataAliasRest + "simple_load " +
                    " SET nzp_exc=" + NzpExcelUtility +
                    " WHERE nzp_load=" + NzpLoad);
            
            myFile.SetFileState(new ExcelUtility
            {
                nzp_exc = NzpExcelUtility,
                status = ExcelUtility.Statuses.Success,
                exc_path = fileName
            });

            #endregion

            return String.Empty;

        }

    }
}