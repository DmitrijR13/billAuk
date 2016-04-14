
#region Список подключаемых библиотек
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Bars.KP50.DataImport.CHECK.Impl;
using FastReport.Utils;
using Globals.SOURCE.GLOBAL;
using STCLINE.KP50.Global;
using STCLINE.KP50.IFMX.Kernel.source.CreateNewBank;
using STCLINE.KP50.IFMX.Server.SOURCE.CHECK.Impl;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;
using FastReport;
using FastReport.Export;
using Bars.KP50.Utils;
#endregion Список подключаемых библиотек

namespace STCLINE.KP50.DataBase
{
    public partial class DbCharge : DbChargeClient
    {
#if PG
        private readonly string pgDefaultDb = "public";
#else
#endif

        #region Представлены функции: Закрытие и откат месяца. Проверки расчетов.
        #region Закрытие и откат месяца. Проверки расчетов
        public struct ParamCalcP
        {
            public string pref;
            public int calc_yy;
            public int calc_mm;
            public int prev_calc_yy;
            public int prev_calc_mm;
            public string sdat_s;
            public string sdat_po;

        }
        //int GEvent_nzp_user=0;

        // 
        // create table checkChMon (nzp_check serial,dat_check date,  month_    integer,yearr     integer,note      char(100),nzp_grp   integer,   pref      char(30),   name_prov char(40)    
        //  , status_ integer, is_critical integer);
        //                                          дата проверки     месяц            год               сообщение           номер группы куда ,   префикс базы данных, наименование проверки ,
        // 1- успешно 2 не успешно, 1 -критическая переход на другой месяц запрещен  0 - не критичная если статус =2 неуспешный то переход на другой месяц разрешен 
        // 

        #region Проверка правильности расчетов
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filterPref">Список банков по которым нужно выполнить проверки</param>
        /// <returns></returns>
        public ReturnsType MakeChecksBeforeCloseCalcMonth(Finder finder, List<string> filterPref = null, CalcFonTask calcfon = null)
        {
            // Журнал куда пишутся SQL
            Utility.ClassLog.InitializeLog("c:\\", "MakeChecksBeforeCloseCalcMonth.log");

            // Объявить переменные подсоединения 
            //ParamCalcP paramClose;
            IDbConnection conn_db;
            string connectionString;
            ReturnsType p_ret = new ReturnsType(true);

            //переменные для проставления процента заугрузки 
            int bank_count = filterPref != null ? filterPref.Count : Points.PointList.Count;
            int check_count = 5;
            decimal curr_bank_num = -1;

            int p_rets = 0;
            int p_rets_k = 0;
            try
            {
                // Перебор всех поинтов , последовательное закрытие месяцев 
                foreach (_Point zap in Points.PointList)
                {
                    string message = "";
                    //фильтр по банкам
                    if (filterPref != null && !filterPref.Contains(zap.pref)) continue;

                    curr_bank_num += 1;
                    if (calcfon != null) SetTaskProgress(calcfon.nzp_key, Convert.ToDecimal(curr_bank_num / bank_count), "calc_fon_" + calcfon.QueueNumber);

                    //Если локальный банк данных уже переведен следующий месяц, то выполнять проверки для него не нужно
                    if (new DateTime(zap.CalcMonth.year_, zap.CalcMonth.month_, 1) >
                        new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1)) continue;

                    connectionString = Points.GetConnByPref(zap.pref);
                    conn_db = GetConnection(connectionString);
                    // подключение к базе данных 
                    Returns ret = OpenDb(conn_db, true);
                    if (!ret.result)
                    {
                        return new ReturnsType(false, "Функция выполнения проверок перед закрытием месяца " +
                                                      " не выполнена (не удалось подключиться к базе данных)", -1);
                    }
                    ParamCalcP paramClose = getParamMonth(zap.pref);

                    using (
                        var c =
                            new CheckIsmAfterRasprOdn(new CheckBeforeClosingParams
                            {
                                Bank = new _Point { pref = zap.pref },
                                User = new User { nzp_user = finder.nzp_user }
                            }))
                    {
                        p_ret.result = c.StartCheck().result;
                        if (!p_ret.result)
                        {
                            p_rets = p_rets + 1;
                            message =
                                string.Format(
                                    "Проверки перед закрытием месяца. Для банка данных '{0}' вернулся отрицательный результат от обязательной проверки: '{1}'",
                                    zap.pref, c);
                            MonitorLog.WriteLog(message, MonitorLog.typelog.Info, true);
                        }

                    }

                    if (calcfon != null) SetTaskProgress(calcfon.nzp_key, Convert.ToDecimal(curr_bank_num / bank_count) + 0.2M / bank_count, "calc_fon_" + calcfon.QueueNumber);


                    using (var c = new CheckPayments(new CheckBeforeClosingParams { Bank = new _Point { pref = zap.pref }, User = new User { nzp_user = finder.nzp_user } }))
                    {
                        p_ret.result = c.StartCheck().result;
                        if (!p_ret.result)
                        {
                            p_rets_k = p_rets_k + 1;
                            message = string.Format("Проверки перед закрытием месяца. Для банка данных '{0}' вернулся" +
                                                     " отрицательный результат от обязательной проверки: '{1}'", zap.pref, c);
                            MonitorLog.WriteLog(message, MonitorLog.typelog.Info, true);
                        }
                    }

                    using (var c = new CheckChargeTrioUniq(new CheckBeforeClosingParams { Bank = new _Point { pref = zap.pref }, User = new User { nzp_user = finder.nzp_user } }))
                    {
                        ret.result = c.StartCheck().result;
                        if (!ret.result)
                        {
                            p_rets_k = p_rets_k + 1;
                            message = string.Format("Проверки перед закрытием месяца. Для банка данных '{0}' вернулся" +
                                                    " отрицательный результат от проверки: '{1}'", zap.pref, c);
                            MonitorLog.WriteLog(message, MonitorLog.typelog.Info, true);
                        }
                    }


                    if (calcfon != null) SetTaskProgress(calcfon.nzp_key, Convert.ToDecimal(curr_bank_num / bank_count) + 0.4M / bank_count, "calc_fon_" + calcfon.QueueNumber);


                    using (
                        var c = new CheckPUVal(new CheckBeforeClosingParams { Bank = new _Point { pref = zap.pref }, User = new User { nzp_user = finder.nzp_user } }))
                    {
                        p_ret.result = c.StartCheck().result;
                        if (!p_ret.result)
                        {
                            p_rets = p_rets + 1;
                            message = string.Format("Проверки перед закрытием месяца. Для банка данных '{0}' вернулся " +
                                                     "отрицательный результат от обязательной проверки: '{1}'", zap.pref, c);
                            MonitorLog.WriteLog(message, MonitorLog.typelog.Info, true);
                        }
                    }

                    if (calcfon != null) SetTaskProgress(calcfon.nzp_key, Convert.ToDecimal(curr_bank_num / bank_count) + 0.6M / bank_count, "calc_fon_" + calcfon.QueueNumber);


                    using (var c = new CheckNotCalcNedop(new CheckBeforeClosingParams { Bank = new _Point { pref = zap.pref }, User = new User { nzp_user = finder.nzp_user } }))
                    {
                        p_ret.result = c.StartCheck().result;
                        if (!p_ret.result)
                        {
                            p_rets = p_rets + 1;
                            message = string.Format(
                                    "Проверки перед закрытием месяца. Для банка данных '{0}' вернулся отрицательный" +
                                    " результат от обязательной проверки: '{1}'", zap.pref, c);
                            MonitorLog.WriteLog(message, MonitorLog.typelog.Info, true);
                        }
                    }

                    if (calcfon != null) SetTaskProgress(calcfon.nzp_key, Convert.ToDecimal(curr_bank_num / bank_count) + 0.7M / bank_count, "calc_fon_" + calcfon.QueueNumber);

                    using (var c = new CheckChangedParam
                        (new CheckBeforeClosingParams { Bank = new _Point { pref = zap.pref }, User = new User { nzp_user = finder.nzp_user } }))
                    {
                        p_ret.result = c.StartCheck().result;
                        if (!p_ret.result)
                        {
                            p_rets = p_rets + 1;
                            message = string.Format("Проверки перед закрытием месяца. Для банка данных '{0}' вернулся" +
                                                     " отрицательный результат от обязательной проверки: '{1}'", zap.pref, c);
                            MonitorLog.WriteLog(message, MonitorLog.typelog.Info, true);
                        }
                    }

                    using (var c = new CheckInOutSaldo
                        (new CheckBeforeClosingParams { Bank = new _Point { pref = zap.pref }, User = new User { nzp_user = finder.nzp_user } }))
                    {
                        p_ret.result = c.StartCheck().result;
                        if (!p_ret.result)
                        {
                            p_rets_k = p_rets_k + 1;
                            message = string.Format("Проверки перед закрытием месяца. Для банка данных '{0}' вернулся" +
                                                     " отрицательный результат от обязательной проверки: '{1}'", zap.pref, c);
                            MonitorLog.WriteLog(message, MonitorLog.typelog.Info, true);
                        }
                    }

                    using (var c = new CheckFinMonthOperDay
                        (new CheckBeforeClosingParams { Bank = new _Point { pref = zap.pref }, User = new User { nzp_user = finder.nzp_user } }))
                    {
                        p_ret.result = c.StartCheck().result;
                        if (!p_ret.result)
                        {
                            p_rets_k = p_rets_k + 1;
                            message = string.Format("Проверки перед закрытием месяца. Для банка данных '{0}' вернулся" +
                                                     " отрицательный результат от обязательной проверки: '{1}'", zap.pref, c);
                            MonitorLog.WriteLog(message, MonitorLog.typelog.Info, true);
                        }
                    }

                    using (var c = new CheckLsWithoutAccrual
                        (new CheckBeforeClosingParams { Bank = new _Point { pref = zap.pref }, User = new User { nzp_user = finder.nzp_user } }))
                    {
                        p_ret.result = c.StartCheck().result;
                        if (!p_ret.result)
                        {
                            p_rets = p_rets + 1;
                            message = string.Format("Проверки перед закрытием месяца. Для банка данных '{0}' вернулся" +
                                                     " отрицательный результат от обязательной проверки: '{1}'", zap.pref, c);
                            MonitorLog.WriteLog(message, MonitorLog.typelog.Info, true);
                        }
                    }

                    if (calcfon != null) SetTaskProgress(calcfon.nzp_key, Convert.ToDecimal(curr_bank_num / bank_count) + 0.8M / bank_count, "calc_fon_" + calcfon.QueueNumber);

                    using (var c = new CheckBigPayment
                        (new CheckBeforeClosingParams { Bank = new _Point { pref = zap.pref }, User = new User { nzp_user = finder.nzp_user } }))
                    {
                        p_ret.result = c.StartCheck().result;
                        if (!p_ret.result)
                        {
                            p_rets = p_rets + 1;
                            message = string.Format("Проверки перед закрытием месяца. Для банка данных '{0}' вернулся" +
                                                     " отрицательный результат от обязательной проверки: '{1}'", zap.pref, c);
                            MonitorLog.WriteLog(message, MonitorLog.typelog.Info, true);
                        }
                    }


                    using (var c = new CheckValPuWithoutPu
                        (new CheckBeforeClosingParams { Bank = new _Point { pref = zap.pref }, User = new User { nzp_user = finder.nzp_user } }))
                    {
                        p_ret.result = c.StartCheck().result;
                        if (!p_ret.result)
                        {
                            p_rets = p_rets + 1;
                            message = string.Format("Проверки перед закрытием месяца. Для банка данных '{0}' вернулся" +
                                                     " отрицательный результат отпроверки: '{1}'", zap.pref, c);
                            MonitorLog.WriteLog(message, MonitorLog.typelog.Info, true);
                        }
                    }


                    using (var c = new CheckDublValPU
                        (new CheckBeforeClosingParams { Bank = new _Point { pref = zap.pref }, User = new User { nzp_user = finder.nzp_user } }))
                    {
                        p_ret.result = c.StartCheck().result;
                        if (!p_ret.result)
                        {
                            p_rets = p_rets + 1;
                            message = string.Format("Проверки перед закрытием месяца. Для банка данных '{0}' вернулся" +
                                                     " отрицательный результат отпроверки: '{1}'", zap.pref, c);
                            MonitorLog.WriteLog(message, MonitorLog.typelog.Info, true);
                        }
                    }
                    // проверка наличия нового периода в списке s_baselist
                    p_ret = CheckBaseList(conn_db, paramClose);
                    if (!p_ret.result) { p_rets_k = p_rets_k + 1; }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения проверок :" + ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
                return new ReturnsType(false, " Внимание ! Ошибка выполнения проверок", -1);
            }

            // для отладки закрытия 
            connectionString = Points.GetConnByPref(Points.Pref);
            conn_db = GetConnection(connectionString);
            Returns ret1 = OpenDb(conn_db, true);
            if (!ret1.result)
            {
                return new ReturnsType(false, " Внимание ! Не удалось подключиться к базе данных", -1);
            }
            if (calcfon != null) SetTaskProgress(calcfon.nzp_key, 1, "calc_fon_" + calcfon.QueueNumber);

            if (p_rets_k > 0)
            {
                InsertSysEvent(conn_db, Points.Pref, 0, 0, 7428, "Функция проверки месяца выполнена. Имеются не успешные обязательные проверки  ");
                return new ReturnsType(false, "Функция выполнения проверок перед закрытием месяца выполнена. Имеются не успешные обязательные проверки, необходимо исправить данные до перехода на следующий месяц!!!    ", -1);
            }
            else
            {
                if (p_rets > 0)
                {
                    InsertSysEvent(conn_db, Points.Pref, 0, 0, 7428, "Функция проверки месяца выполнена. Присутствуют не успешные не обязательные проверки  ");
                    return new ReturnsType(true, "Функция выполнения проверок перед закрытием месяца выполнена. Присутствуют не успешные не обязательные проверки, рекомендуем исправить данные !!! Переход на следующий расчетный месяц разрешен  ", -1);
                }
                else
                {
                    InsertSysEvent(conn_db, Points.Pref, 0, 0, 7428, "Функция проверки месяца выполнена.Переход на следующий расчетный месяц разрешен !!!  ");
                    return new ReturnsType(true, "Функция выполнения проверок перед закрытием месяца выполнена. Oшибки отсутствуют !!! Переход на следующий расчетный месяц разрешен ", -1);
                }
            }

        }

        public Returns SetTaskProgress(int taskId, decimal progress, string tableName)
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string sql =
                " update " + sDefaultSchema + tableName +
                " set progress = " + progress.ToString("N4").Replace(',', '.') +
                " where nzp_key = " + taskId;
            ret = ExecSQL(conn_web, sql, true);

            conn_web.Close();
            return ret;
        }


        #endregion Проверка правильности расчетов

        public ParamCalcP getParamMonth(string pref)
        {
            ParamCalcP paramClose;
            paramClose.pref = pref;
            paramClose.calc_yy = Points.GetCalcMonth(new CalcMonthParams(pref)).year_;
            paramClose.calc_mm = Points.GetCalcMonth(new CalcMonthParams(pref)).month_;
            paramClose.prev_calc_mm = Points.GetCalcMonth(new CalcMonthParams(pref)).month_ - 1;
            paramClose.prev_calc_yy = Points.GetCalcMonth(new CalcMonthParams(pref)).year_;

            if (paramClose.prev_calc_mm == 0)
            {
                --paramClose.prev_calc_yy;
                paramClose.prev_calc_mm = 12;
            }
            paramClose.sdat_s = "01." + paramClose.calc_mm.ToString("00") + "." + paramClose.calc_yy.ToString("0000");
            paramClose.sdat_po = "28." + paramClose.calc_mm.ToString("00") + "." + paramClose.calc_yy.ToString("0000");
            return paramClose;
        }



        #region Закрытие месяца
        public ReturnsType CloseCalcMonth(Finder finder)
        {
            IDbConnection conn_db;
            string connectionString;
            int p_rets = 0;
            // Перебор всех поинтов , последовательное закрытие месяцев 
            ReturnsType p_ret;
            Returns ret;
            ParamCalcP paramClose;

            connectionString = Points.GetConnByPref(Points.Pref);
            conn_db = GetConnection(connectionString);
            InsertSysEvent(conn_db, Points.Pref, 0, 0, 7428, "Функция закрытия месяца начата ");

            foreach (_Point zap in Points.PointList)
            {
                connectionString = Points.GetConnByPref(zap.pref);
                conn_db = GetConnection(connectionString);
                ret = OpenDb(conn_db, true);

                if (!ret.result)
                {

                    return new ReturnsType(false, "Функция выполнения проверок перед закрытием месяца - не выполнена (не удалось подключиться к базе данных ) ", -1);
                }
                paramClose = getParamMonth(zap.pref);


                // DbWorkUser db = new DbWorkUser();
                // GEvent_nzp_user = db.GetLocalUser(conn_db, finder, out ret);

                p_ret = CloseCalcMonth_actions(conn_db, paramClose, finder);
                if (!p_ret.result)
                {
                    p_rets = p_rets + 1;


                };

            }

            connectionString = Points.GetConnByPref(Points.Pref);
            conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                InsertSysEvent(conn_db, Points.Pref, 0, 0, 7428, "Функция закрытия месяца - не выполнена (не удалось подключиться к базе данных)");
                return new ReturnsType(false, "Функция выполнения проверок перед закрытием месяца - не выполнена (не удалось подключиться к базе данных)", -1);
            }
            paramClose = getParamMonth(Points.Pref);


            p_ret = CloseCalcMonth_actions(conn_db, paramClose, finder);
            if (!p_ret.result) { p_rets = p_rets + 1; };

            if (p_rets > 0)
            {
                InsertSysEvent(conn_db, Points.Pref, 0, 0, 7428, "Функция закрытия месяца - не выполнена ");
                return new ReturnsType(false, "Функция выполнения проверок перед закрытием месяца - не выполнена ", -1);
            }
            else
            {
                InsertSysEvent(conn_db, Points.Pref, 0, 0, 7428, "Функция выполнения проверок перед закрытием месяца - выполнена успешно ");

                //Обновить информацию о расчетных месяцах
                using (DbSprav dbs = new DbSprav())
                {
                    dbs.PointLoad(GlobalSettings.WorkOnlyWithCentralBank, out ret);
                }

                DateTime calcMonthFirstDay = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1);
                if (Points.DateOper < calcMonthFirstDay)
                {
                    //Определение нового операционного дня
                    DateTime newOperDay;
                    if (DateTime.Today <= calcMonthFirstDay) newOperDay = calcMonthFirstDay;
                    else if (DateTime.Today > calcMonthFirstDay && DateTime.Today < calcMonthFirstDay.AddMonths(1)) newOperDay = DateTime.Today;
                    else newOperDay = calcMonthFirstDay.AddMonths(1).AddDays(-1);

                    //Сохранение операционного дня
                    using (DbPack dbp = new DbPack())
                    {
                        Pack finderPack = new Pack();
                        finderPack.nzp_user = finder.nzp_user;
                        finderPack.CopyTo(finderPack);
                        finderPack.dat_uchet = newOperDay.ToShortDateString();
                        finderPack.mode = OperDayFinder.Mode.GoDefinedOperDay.GetHashCode();

                        Returns retr = dbp.SaveOperDay(finderPack);
                        if (retr.result == false && retr.tag < 0)
                        {
                            p_ret.text = retr.text;
                            p_ret.tag = retr.tag;
                        }
                    }
                }

                return new ReturnsType(true, "Функция выполнения проверок перед закрытием месяца - выполнена успешно ", -1);
            }

        }
        #endregion Закрытие месяца

        #region Откат месяца
        public ReturnsType OpenCalcMonth()
        {
            ParamCalcP paramClose;
            IDbConnection conn_db;
            string connectionString;
            int p_rets = 0;
            // Перебор всех поинтов , последовательное открытие(откат) месяцев 
            ReturnsType p_ret;
            Returns ret;

            foreach (_Point zap in Points.PointList)
            {
                paramClose.pref = zap.pref;
                connectionString = Points.GetConnByPref(paramClose.pref);
                conn_db = GetConnection(connectionString);
                ret = OpenDb(conn_db, true);

                if (!ret.result)
                {
                    return new ReturnsType(false, "Открываем(откатываем) месяц - не выполнена ", -1);
                }

                paramClose.calc_yy = Points.CalcMonth.year_;
                paramClose.calc_mm = Points.CalcMonth.month_;

                paramClose.prev_calc_mm = Points.CalcMonth.month_ - 1;
                paramClose.prev_calc_yy = Points.CalcMonth.year_;
                paramClose.sdat_s = "01." + paramClose.calc_mm.ToString("00") + "." + paramClose.calc_yy.ToString("0000");
                paramClose.sdat_po = "28." + paramClose.calc_mm.ToString("00") + "." + paramClose.calc_yy.ToString("0000");

                if (paramClose.prev_calc_mm == 0)
                {
                    paramClose.prev_calc_yy = Points.CalcMonth.year_ - 1;
                    paramClose.prev_calc_mm = 12;
                }

                p_ret = OpenCalcMonth_actions(conn_db, paramClose);
                if (!p_ret.result) { p_rets = p_rets + 1; };


            }

            // Выборка главной базы данных для отката месяца  
            paramClose.pref = Points.Pref;
            connectionString = Points.GetConnByPref(paramClose.pref);
            conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);

            if (!ret.result)
            {
                return new ReturnsType(false, "Функция выполнения проверок перед откатом месяца - не выполнена  ", -1);
            }
            paramClose.calc_yy = Points.CalcMonth.year_;
            paramClose.calc_mm = Points.CalcMonth.month_;

            paramClose.prev_calc_mm = Points.CalcMonth.month_ - 1;
            paramClose.prev_calc_yy = Points.CalcMonth.year_;

            paramClose.sdat_s = "01." + paramClose.calc_mm.ToString("00") + "." + paramClose.calc_yy.ToString("0000");
            paramClose.sdat_po = "28." + paramClose.calc_mm.ToString("00") + "." + paramClose.calc_yy.ToString("0000");

            if (paramClose.prev_calc_mm == 0)
            {
                paramClose.prev_calc_yy = Points.CalcMonth.year_ - 1;
                paramClose.prev_calc_mm = 12;
            }


            p_ret = OpenCalcMonth_actions(conn_db, paramClose);
            if (!p_ret.result) { p_rets = p_rets + 1; };

            if (p_rets > 0) { return new ReturnsType(false, "Функция выполнения проверок перед откатом месяца - не выполнена ", -1); }
            else
            {
                Points.pointWebData.calcMonth.year_ = paramClose.calc_yy;
                Points.pointWebData.calcMonth.month_ = paramClose.calc_mm;

                return new ReturnsType(true, "Функция выполнения проверок перед откатом месяца - выполнена успешно ", -1);
            }
            //return new ReturnsType(false, "Откат месяца", -1);
        }
        #endregion Откат месяца

        #region Перечень необходимых проверок

        #region Первая очередь готова

        #region Проверка соответствия месяца финансовому дню
        //p_ret = CheckSaldoDate(conn_db, paramClose); if (!p_ret.result) { p_rets_k = p_rets_k + 1; };
        public ReturnsType CheckSaldoDate(IDbConnection conn_db, ParamCalcP paramcalc)
        {

            int Check_iMes = paramcalc.calc_mm;
            int Check_iGod = paramcalc.calc_yy;
            int finyear = Points.DateOper.Year;
            int finmonth = Points.DateOper.Month;

            if (Check_iGod * 12 + Check_iMes - finyear * 12 - finmonth > 0) return new ReturnsType(false, "Финансовый месяц не может отличаться от расчетного более 1 го месяца", -1);
            return new ReturnsType(true, "Проверка правильности финансового месяца проведена", -1);
        }

        #endregion  Проверка соответствия месяца финансовому дню

        #region проверка входящего и исходящего сальдо
        public ReturnsType CheckSaldo(IDbConnection conn_db, ParamCalcP paramcalc)
        {
            int iCheckGroup = 10006;
            int Check_iMes = paramcalc.calc_mm;
            int Check_iGod = paramcalc.calc_yy;
            int count = 0;
            string sSQL_Text;

            int minusgod = 2000;
            if (Check_iGod < 2000) { minusgod = 1900; };

            // собрать строку ограничения по группе 
            string sSQL_Group = get_sGroup(paramcalc.pref, 0);
            sSQL_Group = "";
            // Удалить временную таблицу входящих сальдо
#if PG
            sSQL_Text = "drop table if exists cin";
#else
            sSQL_Text = "drop table cin";
#endif
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);
            // удалить временную таблицу исходящих сальдо 
#if PG
            sSQL_Text = "drop table if exists cout";
#else
            sSQL_Text = "drop table cout";
#endif
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);

            // создать таблицу лицевых счетов с сальдо
#if PG
            sSQL_Text = " drop table if exists tmp_saldo;  create unlogged table tmp_saldo (nzp_kvar integer) ";
#else
            sSQL_Text = "create temp table tmp_saldo (nzp_kvar integer) with no log ";
#endif
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);

            // выбрать все строки с входящим сальдо для квартир без перерасчетов 
#if PG
            sSQL_Text = " select c.nzp_kvar,c.sum_insaldo  into unlogged cin from " +
                               paramcalc.pref + "_charge_" + ((Check_iGod - minusgod) % 100).ToString("00") + ".charge_" + Check_iMes.ToString("00") +
                               " c where c.dat_charge is null and c.nzp_serv>1 ";
            count = ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log).resultAffectedRows;
#else
            sSQL_Text = " select c.nzp_kvar,c.sum_insaldo from " +
                               paramcalc.pref + "_charge_" + (Check_iGod - minusgod).ToString("00") + ":charge_" + Check_iMes.ToString("00") +
                               " c where c.dat_charge is null and c.nzp_serv>1 " +
                               " into temp cin with no log ";
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif

            sSQL_Text = " create index ixin on cin (nzp_kvar) ";
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);
#if PG
            sSQL_Text = " analyze cin ";
#else
            sSQL_Text = " update statistics for table cin ";
#endif
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);

            // надо удалить таблицу сводных сумм по вх сальдо 
#if PG
            sSQL_Text = "drop table if exists c_in";
#else
            sSQL_Text = "drop table c_in";
#endif
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);

            // выбрать входящие сальдо по квартирам во временную таблицу с СУММИРОВАННЫМИ сальдо и ограниченные по группе 
#if PG
            sSQL_Text = " select c.nzp_kvar,sum(c.sum_insaldo) sum_insaldo into unlogged c_in from cin c," + paramcalc.pref + "_data.kvar k" +
                   " where c.nzp_kvar=k.nzp_kvar " + sSQL_Group +
                   " group by 1 ";
            count = ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log).resultAffectedRows;
#else
            sSQL_Text = " select c.nzp_kvar,sum(c.sum_insaldo) sum_insaldo from cin c," + paramcalc.pref + "_data" + tableDelimiter + "kvar k" +
                    " where c.nzp_kvar=k.nzp_kvar " + sSQL_Group +
                    " group by 1 " +
                    " into temp c_in with no log ";
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif

            sSQL_Text = " create index ix_in on c_in (nzp_kvar) ";
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);
#if PG
            sSQL_Text = " analyze c_in ";
#else
            sSQL_Text = " update statistics for table c_in ";
#endif
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);


            // Выбрать исходящие сальдо предыдущего месяца 
            int iMn = Check_iMes - 1;
#if PG
            string sAliasCharge = paramcalc.pref + "_charge_" + (Check_iGod - minusgod).ToString("00") + ".charge_" + iMn.ToString("00");
#else
            string sAliasCharge = paramcalc.pref + "_charge_" + (Check_iGod - minusgod).ToString("00") + ":charge_" + iMn.ToString("00");
#endif

            if (iMn <= 0)
            {
                iMn = 12;
#if PG
                sAliasCharge = paramcalc.pref + "_charge_" + (Check_iGod - 1 - minusgod).ToString("00") + ".charge_" + iMn.ToString("00");
#else
                sAliasCharge = paramcalc.pref + "_charge_" + (Check_iGod - 1 - minusgod).ToString("00") + ":charge_" + iMn.ToString("00");
#endif
            }


#if PG
            sSQL_Text = " select c.nzp_kvar,c.sum_outsaldo into unlogged cout from " + sAliasCharge + " c " +
                      " where c.dat_charge is null and c.nzp_serv>1 ";
            count = ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log).resultAffectedRows;
#else
            sSQL_Text = " select c.nzp_kvar,c.sum_outsaldo from " + sAliasCharge + " c " +
                      " where c.dat_charge is null and c.nzp_serv>1 into temp cout with no log ";
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif

            sSQL_Text = " create index ixout on cout (nzp_kvar)";
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);

#if PG
            sSQL_Text = " analyze cout ";
#else
            sSQL_Text = " update statistics for table cout ";
#endif
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);


            // надо удалить таблицу сводных сумм по исх  сальдо 
#if PG
            sSQL_Text = "drop table if exists c_out";
#else
            sSQL_Text = "drop table c_out";
#endif
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);
            // выбрать исходящие сальдо по квартирам во временную таблицу с СУММИРОВАННЫМИ сальдо и ограниченные по группе 
#if PG
            sSQL_Text = " select c.nzp_kvar,sum(c.sum_outsaldo) sum_outsaldo into unlogged c_out from cout c," +
                        paramcalc.pref + "_data.kvar k where c.nzp_kvar=k.nzp_kvar" + sSQL_Group +
                      " group by 1 ";
            count = ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log).resultAffectedRows;
#else
            sSQL_Text = " select c.nzp_kvar,sum(c.sum_outsaldo) sum_outsaldo from cout c," +
                        paramcalc.pref + "_data" + tableDelimiter + "kvar k where c.nzp_kvar=k.nzp_kvar" + sSQL_Group +
                      " group by 1 into temp c_out with no log";
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            sSQL_Text = " create index ix_out on c_out (nzp_kvar)";
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);

#if PG
            sSQL_Text = " analyze c_out ";
#else
            sSQL_Text = " update statistics for table c_out ";
#endif
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);

            sSQL_Text = " insert into tmp_saldo(nzp_kvar) select c_out.nzp_kvar from c_in,c_out " +
                       " where c_in.nzp_kvar=c_out.nzp_kvar and " +
                       " abs(c_out.sum_outsaldo-c_in.sum_insaldo)>0.0001 ";
#if PG
            count = ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            string strCaption;
            strCaption = "Проверка входящего и исходящего сальдо";

            if (count > 0)
            {

                insertInftocheckChMon(Check_iMes.ToString("00"), Check_iGod.ToString("0000"),
                       " Есть разница между входящим и исходящим сальдо -переход запрещен ", iCheckGroup.ToString(""), paramcalc.pref, strCaption, "2", conn_db, "1");

                // не будем пускать на след месяц, если не расчитаны лиц счета 

                return new ReturnsType(false, strCaption, -1);

            }

            sSQL_Text = " insert into tmp_saldo(nzp_kvar) select a.nzp_kvar from c_out a " +
                       " where 0=(select count(*) from c_in b where a.nzp_kvar=b.nzp_kvar) " +
                       " and abs(a.sum_outsaldo)>0.0001 ";
#if PG
            count = ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif

            if (count > 0)
            {
                insertInftocheckChMon(Check_iMes.ToString("00"), Check_iGod.ToString("0000"), " Есть исходящее сальдо пр. месяца которое не перешло в текущий (не расчитаны л.с.) ", iCheckGroup.ToString(""), paramcalc.pref, strCaption, "2", conn_db, "1");

                return new ReturnsType(false, strCaption, -1);

            }
            else
            {
                insertInftocheckChMon(Check_iMes.ToString("00"), Check_iGod.ToString("0000"), " Входящее сальдо=исх.сальдо проверка прошла успешно", iCheckGroup.ToString(""), paramcalc.pref, strCaption, "1", conn_db, "0");
                return new ReturnsType(true, strCaption + "- успешно", -1);
            }
        }
        #endregion проверка входящего и исходящего сальдо

        #endregion Первая очередь готова

        #region Вторая очередь
        #region проверка больших начислений
        public ReturnsType CheckNach(IDbConnection conn_db, ParamCalcP paramcalc)
        {
            double sSumMax = 18000;

            string strCaption = "Идет проверка наличия больших начислений по услуге ";
            int iCheckGroup = 10003; // выставить номер группы проверки 

            int Check_iMes = paramcalc.calc_mm;
            int Check_iGod = paramcalc.calc_yy;
            //int count = 0;
            string sSQL_Text;
            string sSQL_Group = "";
            //double delt = 0.0001;
            //int minusgod = 2000;
            int i = 0;
            string rSumMax;



            //if (Check_iGod < 2000) { minusgod = 1900; };

            // Собрать данные из таблицы учета оплат   
            sSQL_Text = " select  nzp_key, nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, " +
                        " dat_when, dat_del, user_del, dat_block, user_block, month_calc " +
 " from " + paramcalc.pref + "_data" + tableDelimiter + "prm_5 where nzp_prm=1046 and is_actual<>100 " +
 " and dat_s <='" + paramcalc.sdat_po + "' and dat_po>='" + paramcalc.sdat_s + "'";

            DataTable dtnote = ClassDBUtils.OpenSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log).GetData();

            foreach (DataRow rr in dtnote.Rows)
            {
                int Nzp = Convert.ToInt32(rr["nzp"]);
                i = i + 1;
                rSumMax = Convert.ToString(rr["val_prm"]);
                sSumMax = Convert.ToDouble(rSumMax);
                break;
            }

            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);

#if PG
            sSQL_Text = " drop table if exists bbb; create unlogged table bbb (nzp_kvar integer) ";
#else
            sSQL_Text = "  create temp table bbb (nzp_kvar integer) with no log ";
#endif
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);

            // собрать строку ограничения по группе 
            sSQL_Group = get_sGroup(paramcalc.pref, 0);

            sSQL_Text = "insert into bbb  select nzp_kvar from " +

#if PG
 paramcalc.pref + "_charge_" + (Check_iGod % 100).ToString("00") +
                        ".charge_" + Check_iMes.ToString("00") +
#else
 paramcalc.pref + "_charge_" + (Check_iGod % 100).ToString("00") +
                        ":charge_" + Check_iMes.ToString("00") +
#endif
 " where dat_charge is null and nzp_serv>1 and " +
                        " (rsum_tarif>" + sSumMax + " or reval>" + sSumMax + ")" + sSQL_Group +
                        " group by 1";
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);
            sSQL_Text = "create index ix_bbb on bbb (nzp_kvar)";
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);
#if PG
            sSQL_Text = "analyze bbb";
#else
            sSQL_Text = "update statistics for table bbb";
#endif
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);

#if PG
            sSQL_Text = "select nzp_kvar from bbb offset 1";
#else
            sSQL_Text = "select first 1 nzp_kvar from bbb";
#endif
            int Nzp1 = 0;
            DataTable dtnote1 = ClassDBUtils.OpenSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log).GetData();
            foreach (DataRow rr in dtnote1.Rows)
            {
                Nzp1 = Convert.ToInt32(rr["nzp_kvar"]);
                break;
            }
            //if (Nzp1 > 0) { return new ReturnsType(false, strCaption + "- не прошла", -1); }
            //else          { return new ReturnsType(true , strCaption + "- успешно", -1); };
            strCaption = "Проверка наличия больших начислений по услуге";

            if (Nzp1 > 0)
            {
                insertInftocheckChMon(Check_iMes.ToString("00"), Check_iGod.ToString("0000"), strCaption + " -не прошла ", iCheckGroup.ToString(""), paramcalc.pref, strCaption, "2", conn_db, "0");

                return new ReturnsType(false, strCaption, -1);

            }
            else
            {
                insertInftocheckChMon(Check_iMes.ToString("00"), Check_iGod.ToString("0000"), strCaption + "- успешно", iCheckGroup.ToString(""), paramcalc.pref, strCaption, "1", conn_db, "0");
                return new ReturnsType(true, strCaption + "- успешно", -1);
            }

            //  return new ReturnsType(false, "проверка больших начислений ", -1);
        }
        #endregion проверка больших начислений

        #region Вторая очередь

        #region Проверка счетчиков
        public ReturnsType CheckCounters(IDbConnection conn_db, ParamCalcP paramcalc)
        {
            string strCaption = "Проверка счетчиков";
#if PG
            string sSQL_Text = " select count(*) from " + paramcalc.pref + "_data.counters_dom where nzp_counter not in (select nzp_counter from " + paramcalc.pref + "_data.counters_spis) ";
            int count = ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log).resultAffectedRows;
#else
            string sSQL_Text = " select count(*) from " + paramcalc.pref + "_data" + tableDelimiter + "counters_dom where nzp_counter not in (select nzp_counter from " + paramcalc.pref + "_data" + tableDelimiter + "counters_spis) ";
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);
            double count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif


            if (count > 0)
            {
                insertInftocheckChMon(paramcalc.calc_mm.ToString("00"), paramcalc.calc_yy.ToString("0000"), strCaption + " -не прошла ", "0", paramcalc.pref, strCaption, "2", conn_db, "1");
                return new ReturnsType(false, strCaption + " не прошла ", -1);
            }
            else
            {
                insertInftocheckChMon(paramcalc.calc_mm.ToString("00"), paramcalc.calc_yy.ToString("0000"), strCaption + " прошла успешно ", "0", paramcalc.pref, strCaption, "1", conn_db, "0");
                return new ReturnsType(true, strCaption + " выполнена успешно  ", -1);
            }
        }
        #endregion Проверка счетчиков

        #region Проверка дублей в значениях счетчиков
        public ReturnsType CheckCountersVals(IDbConnection conn_db, ParamCalcP paramcalc, Int32 pmode)
        {
            string strCaption = "Проверка счетчиков";
            string ptable = "";
            if (pmode == 1) { ptable = "counters"; }  // Квартирные счетчики
            if (pmode == 2) { ptable = "counters_dom"; }  // Квартирные счетчики
            if (pmode == 3) { ptable = "counters_group"; }  // Квартирные счетчики

#if PG
            string sSQL_Text = " select nzp_counter,dat_uchet, count(*) as kol from " + paramcalc.pref + "_data." + ptable + " group by 1,2 having count(*) >1 ";
            int count = ClassDBUtils.OpenSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log).GetData().Rows.Count;
#else
            string sSQL_Text = " select nzp_counter,dat_uchet, count(*) as kol from " + paramcalc.pref + "_data" + tableDelimiter + "" + ptable + " group by 1,2 having count(*) >1 ";
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);
            double count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif

            if (count > 0)
            {
                insertInftocheckChMon(paramcalc.calc_mm.ToString("00"), paramcalc.calc_yy.ToString("0000"), strCaption + " -не прошла ", "0", paramcalc.pref, strCaption, "2", conn_db, "1");
                return new ReturnsType(false, strCaption + " не прошла ", -1);
            }
            else
            {
                insertInftocheckChMon(paramcalc.calc_mm.ToString("00"), paramcalc.calc_yy.ToString("0000"), strCaption + " прошла успешно ", "0", paramcalc.pref, strCaption, "1", conn_db, "0");
                return new ReturnsType(true, strCaption + " выполнена успешно  ", -1);
            }
        }
        #endregion Проверка дублей в значениях счетчиков


        #region проверка отрицательных начислений
        public ReturnsType CheckMinus()
        {
            return new ReturnsType(false, "Проверка отрицательных начислений ", -1);
        }
        #endregion проверка отрицательных начислений

        #region проверка расчета сальдо всех ЛС
        public ReturnsType CheckOutSaldo()
        {
            return new ReturnsType(false, "проверка расчета сальдо всех ЛС", -1);
        }
        #endregion проверка расчета сальдо всех ЛС

        #region Проверка дублей в услугах
        public ReturnsType CheckDubl()
        {
            return new ReturnsType(false, "Проверка дублей в услугах ", -1);
        }
        #endregion Проверка дублей в услугах

        #region проверка на изменения параметров и ПУ после расчета
        public ReturnsType CheckParamIzm()
        {
            return new ReturnsType(false, "проверка на изменения параметров и ПУ после расчета", -1);
        }
        #endregion проверка на изменения параметров и ПУ после расчета

        #region проверка Учет оплат портала гос.услуг
        public ReturnsType CheckOplPgu()
        {
            return new ReturnsType(false, "проверка Учет оплат портала гос.услуг", -1);
        }
        #endregion проверка Учет оплат портала гос.услуг

        #endregion Вторая очередь

        #region Третья очередь
        #region проверка ПТ тарифов
        public ReturnsType CheckTarifPt()
        {
            return new ReturnsType(false, "проверка ПТ тарифов ", -1);
        }
        #endregion проверка ПТ тарифов

        #region проверка расчета по паспортистке
        public ReturnsType CheckPasp()
        {
            return new ReturnsType(false, "проверка расчета по паспортистке ", -1);
        }
        #endregion проверка расчета по паспортистке

        #region проверка наличия старых формул по дотационным услугам
        public ReturnsType CheckBadFrm()
        {
            return new ReturnsType(false, "проверка наличия старых формул по дотационным услугам ", -1);
        }
        #endregion проверка наличия старых формул по дотационным услугам

        #region проверка проведения расчета всех открытых ЛС
        public ReturnsType CheckCalc()
        {
            return new ReturnsType(false, "проверка проведения расчета всех открытых ЛС ", -1);
        }
        #endregion проверка проведения расчета всех открытых ЛС

        #region проверка открытых ЛС, а начислений нет

        public ReturnsType CheckOpenCalc(IDbConnection connection, ParamCalcP prms)
        {
            string sql = "";

            const int checkGroupId = 10007;

            ReturnsType ret = new ReturnsType(true, "Проверка наличия открытых ЛС без начислений");

            #region заполняем временную таблицу данными, не прошедшими проверку

            string chargeTblFullName =
                String.Format("{0}_charge_{1}{2}charge_{3}",
                    prms.pref,
                    prms.calc_yy % 100,
                    DBManager.tableDelimiter,
                    prms.calc_mm.ToString("00"));

            string tempTableName = "t_ls_without_charge" + DateTime.Now.Ticks;

            sql = " DROP TABLE " + tempTableName;
            DBManager.ExecSQL(connection, sql, false);


            sql =
                " SELECT DISTINCT nzp_kvar " +
                " INTO TEMP " + tempTableName +
                " FROM  " + chargeTblFullName +
                " WHERE nzp_serv > 1 " +
                " AND rsum_tarif = 0 " +
                " AND sum_outsaldo = 0 ";
            DBManager.ExecSQL(connection, sql, true);

            #endregion заполняем временную таблицу данными, не прошедшими проверку


            sql = " SELECT COUNT(*) AS count FROM " + tempTableName;
            if (Convert.ToInt32(DBManager.ExecSQLToTable(connection, sql).Rows[0]["count"]) > 0)
            {
                ret.result = false;
                insertInftocheckChMon(
                    prms.calc_mm.ToString(),
                    prms.calc_yy.ToString(),
                    "Имеются открытые ЛС без начислений",
                    checkGroupId.ToString(),
                    prms.pref,
                    "Открытые ЛС без начислений",
                    "2",
                    connection,
                    "0");

                #region вносим номера ЛС в таблицу ling_group

                sql =
                    " INSERT INTO " + prms.pref + DBManager.sDataAliasRest + "link_group " +
                    " (nzp,nzp_group) " +
                    " SELECT DISTINCT nzp_kvar," + checkGroupId +
                    " FROM " + tempTableName;
                DBManager.ExecSQL(connection, sql, true);

                #endregion вносим номера ЛС в таблицу ling_group

                return ret;
            }

            sql = "DROP TABLE " + tempTableName;
            DBManager.ExecSQL(connection, sql, true);

            return ret;
        }

        #endregion проверка открытых ЛС, а начислений нет

        #region проверка ком услуг на наличие нормативов начисления для СЗ
        public ReturnsType CheckSZServ6()
        {
            return new ReturnsType(false, "проверка ХВС на наличие нормативов начисления для СЗ", -1);
            // проверка ХВС на наличие нормативов начисления для СЗ
            // проверка КАНАЛИЗАЦИИ на наличие нормативов начисления для СЗ
            // проверка ГВС на наличие нормативов начисления для СЗ
            // проверка ГАЗ на наличие нормативов начисления для СЗ
        }
        #endregion проверка ком услуг на наличие нормативов начисления для СЗ

        #region проверка Отсуствие количества комнат (электроснабжение есть)
        public ReturnsType CheckKolKomn()
        {
            return new ReturnsType(false, "проверка Отсуствие количества комнат (электроснабжение есть)", -1);
        }
        #endregion проверка Отсуствие количества комнат (электроснабжение есть)

        #region проверка Ввод ДПУ после расчета
        public ReturnsType CheckVvodDPU()
        {
            return new ReturnsType(false, "проверка Ввод ДПУ после расчета ", -1);
        }
        #endregion проверка Ввод ДПУ после расчета

        #region проверка Наличие доначисляющего перерасчета (50%)
        public ReturnsType CheckPere50p()
        {
            return new ReturnsType(false, "проверка Наличие доначисляющего перерасчета (50%)", -1);
        }
        #endregion проверка Наличие доначисляющего перерасчета (50%)

        #endregion Третья очередь

        #region Последняя очередь
        //function CheckRostNach (pItemNum: Integer):Boolean; // проверка Рост начисления по коммунальным услугам
        //function CheckRostTrfs (pItemNum: Integer):Boolean; // проверка Рост тарифов по услугам
        //function CheckRostLS   (pItemNum: Integer):Boolean; // проверка Рост суммы начислений по лиц.счету
        //function ChkOplPgu     (pMn,pYr: Integer): Boolean;
        //procedure TestTarif (pTempName, pWorkName, pServ, pCheckVal: String); // проверка ХВС/ГВС/КАН по tarif на наличие нормативов начисления для СЗ
        #endregion Последняя очередь

        // проверка наличия банка данных для нового периода
        private ReturnsType CheckBaseList(IDbConnection conn_db, ParamCalcP paramcalc)
        {
            // найдем в базе данных записи для года paramcalc.calc_yy
            Dictionary<int, bool> idtypes = new Dictionary<int, bool>()
            {
                //{BaselistTypes.Charge.GetHashCode(), false},
                {BaselistTypes.Fin.GetHashCode(), false}
            };

            // посчитаем количество записей для финансов
            string strSQLQuery =
                "SELECT count(*) as count, idtype" +
                " FROM " + Points.Pref + "_kernel" + tableDelimiter + "s_baselist" +
                " WHERE idtype in(" + BaselistTypes.Fin.GetHashCode() + ") and yearr='" + paramcalc.calc_yy + "'" +
                " group by idtype";
            DataTable data = ClassDBUtils.OpenSQL(strSQLQuery, conn_db, ClassDBUtils.ExecMode.Log).GetData();
            foreach (DataRow dr in data.Rows)
            {
                int idtype = Convert.ToInt32(dr["idtype"]);
                int count = Convert.ToInt32(dr["count"]);
                idtypes[idtype] = (count > 0);
            }
            // проверим, что есть и начисления и финансы
            List<bool> vals = new List<bool>(idtypes.Values);
            if (vals.TrueForAll(x => x))
            {
                string strCaption = "Проверка банков данных для нового периода выполнена успешно ";
                insertInftocheckChMon(paramcalc.calc_mm.ToString("00"), paramcalc.calc_yy.ToString("0000"), strCaption, "999", paramcalc.pref, "Проверка банка данных ", "1", conn_db, "0");
                return new ReturnsType(true, strCaption, -1);
            }
            else
            {
                string strCaption = "Отсутствуют банки данных для нового периода! ";
                insertInftocheckChMon(paramcalc.calc_mm.ToString("00"), paramcalc.calc_yy.ToString("0000"), strCaption, "999", paramcalc.pref, "Проверка банка данных ", "2", conn_db, "1");
                return new ReturnsType(false, strCaption, -1);
            }
        }

        #endregion Перечень необходимых проверок

        #region Перечень действий по закрытию месяца
        public ReturnsType CloseCalcMonth_actions(IDbConnection conn_db, IDbTransaction transaction, string pref, Finder finder)
        {
            return CloseCalcMonth_actions(conn_db, transaction, getParamMonth(pref), finder);
        }

        public ReturnsType CloseCalcMonth_actions(IDbConnection conn_db, string pref, Finder finder)
        {
            return CloseCalcMonth_actions(conn_db, null, getParamMonth(pref), finder);
        }

        public ReturnsType CloseCalcMonth_actions(IDbConnection conn_db, ParamCalcP paramcalc, Finder finder)
        {
            return CloseCalcMonth_actions(conn_db, null, paramcalc, finder);
        }

        public ReturnsType CloseCalcMonth_actions(IDbConnection conn_db, IDbTransaction transaction, ParamCalcP paramcalc, Finder finder)
        {
            if (new DateTime(paramcalc.calc_yy, paramcalc.calc_mm, 1) > new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1))
            {
                return new ReturnsType(true, "Банк данных " + Points.GetPoint(paramcalc.pref).point + " уже переведен на " + paramcalc.calc_mm.ToString("00") + "." + paramcalc.calc_yy.ToString("0000"), -1);
                //insertInftocheckChMon(cur_month_.ToString("00"), cur_yearr.ToString("0000"), "Месяц успешно изменен на " + cur_month_.ToString("00") + "." + cur_yearr.ToString("0000"), "0", paramcalc.pref, "Месяц изменен", "1", conn_db, transaction, "1");
            }

            ReturnsType r = new ReturnsType();

            if (paramcalc.pref != Points.Pref)
            {
                //создаем схемы, если их нет
                CreateNewCharges(conn_db, paramcalc);

                r = ChangeKreditXX(conn_db, paramcalc);
                if (!r.result) return r;

                int nzp_wp = (from point in Points.PointList
                              where point.pref == paramcalc.pref
                              select point.nzp_wp).FirstOrDefault();

                DbCalcReportStat dbCalcReportStat = new DbCalcReportStat(conn_db);
                r.result = dbCalcReportStat.CalcReportXX(paramcalc.calc_mm, paramcalc.calc_yy,
                    paramcalc.pref, nzp_wp, 0).result;
                if (!r.result)
                {
                    r.text = "Не прошло обновление статистики";
                    return r;
                }

            }
            else
            {
                CreateNewFin(conn_db, paramcalc);
            }


            // не забыть создать таблицу сохранения изменений сальдового месяца   (в идеале создать триггер который фиксирует вмешательство в сальдовый месяц)
#if PG
            string sSQL_Text = "CREATE TABLE SALDO_DATE_LOG (nzp_ch serial not null ,SALDO_OLD  DATE, SALDO_NEW  DATE, TIME_WHEN timestamp default now() ,  USER_NAME CHAR(60)) ";
#else
            string sSQL_Text = "CREATE TABLE SALDO_DATE_LOG (nzp_ch serial not null ,SALDO_OLD  DATE, SALDO_NEW  DATE, TIME_WHEN DateTime year to second default current year to second ,  USER_NAME CHAR(60)) ";
#endif
            //ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);
            // Добавить запись в журнал 

            // Добавить новый сальдовый месяц пока с признаком -1 
            sSQL_Text = " insert into " + paramcalc.pref + "_data" + tableDelimiter + "saldo_date(month_,yearr,iscurrent,dat_saldo,prev_date) " +

#if PG
 " select distinct case when a.month_+1=13 then 1 else month_+1 end, case when a.month_+1=13 then yearr+1 else yearr end, -1,  " +
                       " date((dat_saldo +1)+interval '1  MONTH' - interval '1  DAY'),dat_saldo from " + paramcalc.pref + "_data" + tableDelimiter +
                       "saldo_date a where iscurrent=0   ";
#else
 " select distinct case when a.month_+1=13 then 1 else month_+1 end, case when a.month_+1=13 then yearr+1 else yearr end, -1,  " +
                       " date((dat_saldo +1)+1 units month -1 units day),dat_saldo from " + paramcalc.pref + "_data" + tableDelimiter +
            "saldo_date a where iscurrent=0   ";

#endif
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, transaction, ClassDBUtils.ExecMode.Log);

            // перевести старый месяц в разряд архивных 
            sSQL_Text = " update " + paramcalc.pref + "_data" + tableDelimiter + "saldo_date set iscurrent=1 where iscurrent=0 ";
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, transaction, ClassDBUtils.ExecMode.Log);

            // Активировать только что вставленный период 
            sSQL_Text = " update " + paramcalc.pref + "_data" + tableDelimiter + "saldo_date set iscurrent=0 where iscurrent=-1 ";
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, transaction, ClassDBUtils.ExecMode.Log);

            // Считать параметры нового месяца во все структуры 
            sSQL_Text = "select month_,yearr,iscurrent,dat_saldo,prev_date from " + paramcalc.pref + "_data" + tableDelimiter + "saldo_date where iscurrent<=0 ";

            int cur_month_ = 0;
            int cur_yearr = 0;
            string cur_dat_saldo;
            string cur_prev_date;
            int cur_iscurrent = 1;

            DataTable dtnote = ClassDBUtils.OpenSQL(sSQL_Text, conn_db, transaction, ClassDBUtils.ExecMode.Log).GetData();
            foreach (DataRow rr in dtnote.Rows)
            {
                cur_month_ = Convert.ToInt32(rr["month_"]);
                cur_yearr = Convert.ToInt32(rr["yearr"]);
                cur_dat_saldo = Convert.ToString(rr["dat_saldo"]);
                cur_prev_date = Convert.ToString(rr["prev_date"]);
                cur_iscurrent = Convert.ToInt32(rr["iscurrent"]);
                paramcalc.prev_calc_yy = paramcalc.calc_yy;
                paramcalc.prev_calc_mm = paramcalc.calc_mm;
                paramcalc.calc_yy = cur_yearr;
                paramcalc.calc_mm = cur_month_;

                MonitorLog.WriteLog("Текущий расчетный месяц: " + cur_month_.ToString("00") + "." + cur_yearr.ToString("0000"), MonitorLog.typelog.Info, 1, 2, true);
                break;
            }
            if (cur_iscurrent == 0)
            {
                insertInftocheckChMon(cur_month_.ToString("00"), cur_yearr.ToString("0000"), "Месяц успешно изменен на " + cur_month_.ToString("00") + "." + cur_yearr.ToString("0000"),
      "0", paramcalc.pref, "Месяц изменен", "1", conn_db, transaction, "1");
                InsertSysEvent(conn_db, transaction, paramcalc.pref, 0, 0, 7428, "Месяц успешно изменен на " + cur_month_.ToString("00") + "." + cur_yearr.ToString("0000"));
                return new ReturnsType(true, "Месяц успешно изменен на " + cur_month_.ToString("00") + "." + cur_yearr.ToString("0000"), -1);
            }
            else
            {
                insertInftocheckChMon(cur_month_.ToString("00"), cur_yearr.ToString("0000"), "Месяц не изменен , текущий месяц остался " + cur_month_.ToString("00") + "." + cur_yearr.ToString("0000"),
                     "0", paramcalc.pref, "Ошибка изменения месяца", "2", conn_db, transaction, "0");
                InsertSysEvent(conn_db, transaction, paramcalc.pref, 0, 0, 7428, "Месяц не изменен , текущий месяц остался " + cur_month_.ToString("00") + "." + cur_yearr.ToString("0000"));
                return new ReturnsType(false, "Ошибка изменения месяца ", -1);
            };

        }

        private static Returns CreateNewCharges(IDbConnection conn_db, ParamCalcP paramcalc)
        {
            Returns ret =Utils.InitReturns();
            string charge_next = paramcalc.pref + "_charge_" + (paramcalc.calc_yy + 1).ToString().Substring(2, 2);
            if (!DBManager.SchemaExists(charge_next, conn_db))
            {
                string charge_current = paramcalc.pref + "_charge_" + paramcalc.calc_yy.ToString().Substring(2, 2);
                int nzpBl = DBManager.ExecScalar<int>(conn_db, null,
                    "SELECT nzp_bl FROM " + Points.Pref + DBManager.sKernelAliasRest + "s_baselist WHERE dbname = '" +
                    charge_current + "'");
                if (nzpBl <= 0)
                {
                    MonitorLog.WriteLog("Ошибка получения кода nzp_bl при создании банка " + charge_next +
                                        " в функции " + System.Reflection.MethodBase.GetCurrentMethod().Name,
                        MonitorLog.typelog.Error, 20, 201, true);
                }
                else
                {
                    BankFinder bankFinder = new BankFinder
                    {
                        NewBaseName = charge_next,
                        NewBasesYear = paramcalc.calc_yy + 1,
                        NewComment = "Начисления" + (paramcalc.calc_yy + 1),
                        NewPref = paramcalc.pref,
                        TemplateBaseName = charge_current,
                        NzpBl = nzpBl
                    };
                    BankCreator bc = new BankCreator();
                    ret = bc.Create(conn_db, bankFinder);
                }
            }

            string charge_next_next = paramcalc.pref + "_charge_" + (paramcalc.calc_yy + 2).ToString().Substring(2, 2);
            if (!DBManager.SchemaExists(charge_next_next, conn_db))
            {
                string charge_current = paramcalc.pref + "_charge_" + paramcalc.calc_yy.ToString().Substring(2, 2);
                int nzpBl = DBManager.ExecScalar<int>(conn_db, null,
                    "SELECT nzp_bl FROM " + Points.Pref + DBManager.sKernelAliasRest + "s_baselist WHERE dbname = '" +
                    charge_current + "'");
                if (nzpBl <= 0)
                {
                    MonitorLog.WriteLog("Ошибка получения кода nzp_bl при создании банка " + charge_next +
                                        " в функции " + System.Reflection.MethodBase.GetCurrentMethod().Name,
                        MonitorLog.typelog.Error, 20, 201, true);
                }
                else
                {
                    BankFinder bankFinder = new BankFinder
                    {
                        NewBaseName = charge_next_next,
                        NewBasesYear = paramcalc.calc_yy + 2,
                        NewComment = "Начисления" + (paramcalc.calc_yy + 2),
                        NewPref = paramcalc.pref,
                        TemplateBaseName = charge_current,
                        NzpBl = nzpBl
                    };
                    BankCreator bc = new BankCreator();
                    ret = bc.Create(conn_db, bankFinder);
                }
            }
            return ret;
        }

        private static Returns CreateNewFin(IDbConnection conn_db, ParamCalcP paramcalc)
        {
            Returns ret = Utils.InitReturns();
            string charge_next = Points.Pref + "_fin_" + (paramcalc.calc_yy + 1).ToString().Substring(2, 2);
            if (!DBManager.SchemaExists(charge_next, conn_db))
            {
                string charge_current = Points.Pref + "_fin_" + paramcalc.calc_yy.ToString().Substring(2, 2);
                int nzpBl = DBManager.ExecScalar<int>(conn_db, null,
                    "SELECT nzp_bl FROM " + Points.Pref + DBManager.sKernelAliasRest + "s_baselist WHERE dbname = '" +
                    charge_current + "'");
                if (nzpBl <= 0)
                {
                    MonitorLog.WriteLog("Ошибка получения кода nzp_bl при создании банка " + charge_next +
                                        " в функции " + System.Reflection.MethodBase.GetCurrentMethod().Name,
                        MonitorLog.typelog.Error, 20, 201, true);
                }
                else
                {
                    BankFinder bankFinder = new BankFinder
                    {
                        NewBaseName = charge_next,
                        NewBasesYear = paramcalc.calc_yy + 1,
                        NewComment = "Финансовый " + (paramcalc.calc_yy + 1),
                        NewPref = Points.Pref,
                        TemplateBaseName = charge_current,
                        NzpBl = nzpBl
                    };
                    BankCreator bc = new BankCreator();
                    ret = bc.Create(conn_db, bankFinder);
                }
            }

            return ret;
        }

        #endregion Перечень действий по закрытию месяца

        #region Перечень действий по откату месяца
        public ReturnsType OpenCalcMonth_actions(IDbConnection conn_db, ParamCalcP paramcalc)
        {
            int cur_month_ = 0;
            int cur_yearr = 0;
            string cur_dat_saldo;
            string cur_prev_date;
            int cur_iscurrent = 1;
            string mm, yy;

            // не забыть создать таблицу сохранения изменений сальдового месяца   (в идеале создать триггер который фиксирует вмешательство в сальдовый месяц)
#if PG
            string sSQL_Text = "CREATE TABLE SALDO_DATE_LOG (nzp_ch serial not null ,SALDO_OLD  DATE, SALDO_NEW  DATE, TIME_WHEN timestamp default now() ,  USER_NAME CHAR(60)) ";
#else
            string sSQL_Text = "CREATE TABLE SALDO_DATE_LOG (nzp_ch serial not null ,SALDO_OLD  DATE, SALDO_NEW  DATE, TIME_WHEN DateTime year to second default current year to second ,  USER_NAME CHAR(60)) ";
#endif
            // ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);
            // Добавить запись в журнал 
#if PG
            sSQL_Text = " select  case when a.month_-1=0 then 12 else month_-1 end as month_,  case when a.month_-1=0 then yearr-1 else yearr end as yearr,   " +
                        " date((dat_saldo +1)-interval '1  MONTH' - interval '1  DAY') as dat_saldo,date((dat_saldo +1)-interval '2  MONTH' - interval '2  DAY') as prev_date, iscurrent " +
#else
            sSQL_Text = " select  case when a.month_-1=0 then 12 else month_-1 end as month_,  case when a.month_-1=0 then yearr-1 else yearr end as yearr,   " +
                       " date((dat_saldo +1)-1 units month -1 units day) as dat_saldo,date((dat_saldo +1)-2 units month -1 units day) as prev_date, iscurrent " +

#endif

#if PG
 " from " + Points.Pref + "_data.saldo_date a where iscurrent=0 ";
#else
 " from " + Points.Pref + "_data" + tableDelimiter + "saldo_date a where iscurrent=0 ";
#endif
            mm = "0"; yy = "0";
            DataTable dtnote = ClassDBUtils.OpenSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log).GetData();
            foreach (DataRow rr in dtnote.Rows)
            {
                mm = Convert.ToString(rr["month_"]);
                yy = Convert.ToString(rr["yearr"]);
                cur_dat_saldo = Convert.ToString(rr["dat_saldo"]);
                cur_prev_date = Convert.ToString(rr["prev_date"]);
                cur_iscurrent = Convert.ToInt32(rr["iscurrent"]);
                MonitorLog.WriteLog("Текущий расчетный месяц: " + cur_month_.ToString("00") + "." + cur_yearr.ToString("0000"), MonitorLog.typelog.Info, 1, 2, true);
                break;
            }

            // Добавить новый сальдовый месяц пока с признаком -1 
#if PG
            sSQL_Text = " update " + paramcalc.pref + "_data.saldo_date set iscurrent=-2 where iscurrent=0   ";
#else
            sSQL_Text = " update " + paramcalc.pref + "_data" + tableDelimiter + "saldo_date set iscurrent=-2 where iscurrent=0   ";
#endif
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);
            // перевести старый месяц в разряд архивных 
#if PG
            sSQL_Text = " update " + paramcalc.pref + "_data.saldo_date set iscurrent=0 where iscurrent=1 and month_= " + mm + " and yearr =" + yy;
#else
            sSQL_Text = " update " + paramcalc.pref + "_data" + tableDelimiter + "saldo_date set iscurrent=0 where iscurrent=1 and month_= " + mm + " and yearr =" + yy;
#endif
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);
            // Активировать только что вставленный период 
#if PG
            sSQL_Text = " delete from " + paramcalc.pref + "_data.saldo_date where  iscurrent=-2  ";
#else
            sSQL_Text = " delete from " + paramcalc.pref + "_data" + tableDelimiter + "saldo_date where  iscurrent=-2  ";
#endif
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);

            // Считать параметры нового месяца во все структуры 
#if PG
            sSQL_Text = "select month_,yearr,iscurrent,dat_saldo,prev_date from " + paramcalc.pref + "_data.saldo_date where iscurrent=0 ";
#else
            sSQL_Text = "select month_,yearr,iscurrent,dat_saldo,prev_date from " + paramcalc.pref + "_data" + tableDelimiter + "saldo_date where iscurrent=0 ";
#endif

            DataTable dtnote1 = ClassDBUtils.OpenSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log).GetData();
            foreach (DataRow rr in dtnote1.Rows)
            {
                cur_month_ = Convert.ToInt32(rr["month_"]);
                cur_yearr = Convert.ToInt32(rr["yearr"]);
                cur_dat_saldo = Convert.ToString(rr["dat_saldo"]);
                cur_prev_date = Convert.ToString(rr["prev_date"]);
                cur_iscurrent = Convert.ToInt32(rr["iscurrent"]);
                MonitorLog.WriteLog("Текущий расчетный месяц: " + cur_month_.ToString("00") + "." + cur_yearr.ToString("0000"), MonitorLog.typelog.Info, 1, 2, true);
                break;
            }
            // Откатить текущий месяц на предыдущий

            if (cur_iscurrent == 0)
            {
                insertInftocheckChMon(cur_month_.ToString("00"), cur_yearr.ToString("0000"), "Месяц успешно изменен на " + cur_month_.ToString("00") + "." + cur_yearr.ToString("0000"),
                    "0", paramcalc.pref, "Месяц изменен", "0", conn_db, "");
                return new ReturnsType(true, "Месяц успешно изменен на " + cur_month_.ToString("00") + "." + cur_yearr.ToString("0000"), -1);
            }
            else
            {
                insertInftocheckChMon(cur_month_.ToString("00"), cur_yearr.ToString("0000"), "Месяц не изменен ",
                         "0", paramcalc.pref, "Ошибка изменения месяца", "1", conn_db, "");
                return new ReturnsType(false, "Ошибка изменения месяца ", -1);
            };



        }
        #endregion Перечень действий по откату месяца

        #region Перечень действий откату месяца
        public ReturnsType ReOpenCalcMonth_actions()
        {
            return new ReturnsType(false, "Перечень действий откату месяца", -1);
        }
        #endregion Перечень действий откату месяца

        // сформировать строку уточнения по группе 
        public string get_sGroup(string pref, int iCheckGroup)
        {

            if (iCheckGroup > 0)
            {
                // Если будем анализировать только группу
                string sSQL_Group =
                    " and num_ls in (select k.num_ls" +
                    " from " + pref + "_data" + tableDelimiter + "kvar k," + pref + "_data" + tableDelimiter + "link_group l" +
                    " where k.nzp_kvar=l.nzp and l.nzp_group=" + Convert.ToString(iCheckGroup) +
                    ") ";
                return sSQL_Group;
            }
            return "";
        }

        public int insertInftocheckChMon(string month_, string yearr, string note, string nzp_grp, string pref,
            string name_prov, string status_, IDbConnection conn_db, string is_critical)
        {
            return insertInftocheckChMon(month_, yearr, note, nzp_grp, pref, name_prov, status_, conn_db, null,
                is_critical);
        }

        public int insertInftocheckChMon(string month_, string yearr, string note, string nzp_grp, string pref, string name_prov, string status_, IDbConnection conn_db, IDbTransaction transaction, string is_critical)
        {
            string sSQL_Text;
            double count;

            sSQL_Text =
                " delete from  " + Points.Pref + "_data" + tableDelimiter + "checkChMon " +
                " where  month_='" + month_ + "'  and yearr='" + yearr + "' and (nzp_grp='" + nzp_grp + "' or nzp_grp='0' ) and pref='" + pref + "'";
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, transaction, ClassDBUtils.ExecMode.Log);

            sSQL_Text =
                " insert into " + Points.Pref + "_data" + tableDelimiter + "checkChMon " +
                " (dat_check,month_,yearr,note,nzp_grp,pref,name_prov, status_, is_critical ) " +
                " values ( " + sCurDate + " ,'" + month_ + "','" + yearr + "',cast('" + note + "' as char(100))," + nzp_grp + ",'" + pref + "',cast('" + name_prov + "' as char(40)),'" + status_ + "','" + is_critical + "' )";
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, transaction, ClassDBUtils.ExecMode.Log);


            // Сформировать группу
            sSQL_Text =
                " delete from " + pref + "_data" + tableDelimiter + "s_group  where nzp_group= " + nzp_grp;
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, transaction, ClassDBUtils.ExecMode.Log);

            sSQL_Text = " insert into " + pref + "_data" + tableDelimiter + "s_group(nzp_group,ngroup) values( " + nzp_grp + ",'" + name_prov + "' ) ";
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, transaction, ClassDBUtils.ExecMode.Log);

            // почистить группу в данном банке
            sSQL_Text =
                " delete from " + pref + "_data" + tableDelimiter + "link_group " +
                " where nzp_group=" + nzp_grp + " ";
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, transaction, ClassDBUtils.ExecMode.Log);

            if (status_ == "2")
            {
                // Заполнить группу если были ошибки
                if (nzp_grp == "10006")
                {
                    // проверка сальдо
                    sSQL_Text = " insert into " + pref + "_data" + tableDelimiter + "link_group (nzp,nzp_group) select nzp_kvar," + nzp_grp + " from tmp_saldo ";
                }
                else
                {
                    // Проверка оплат
                    if (nzp_grp == "10000")
                    {
                        sSQL_Text =
                            " insert into " + pref + "_data" + tableDelimiter + "link_group (nzp,nzp_group) select " + sUniqueWord + " nzp_kvar," + nzp_grp +
                            " from ccc where sum_money>0 group by 1 having abs(sum(sum_money)-sum(money_del))>0.0001  ";
                    }
                    else
                        if (nzp_grp == "10003")
                        {
                            // проверка больших начислений
                            sSQL_Text = " insert into " + pref + "_data" + tableDelimiter + "link_group (nzp,nzp_group) select nzp_kvar," + nzp_grp + " from bbb ";
                        }
                        else if (nzp_grp == "10111")
                        {
                            sSQL_Text = " insert into " + pref + "_data" + tableDelimiter + "link_group (nzp,nzp_group) select nzp_kvar," + nzp_grp + " from bbb ";
                        }
                }
#if PG
                count = ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log).resultAffectedRows;
#else
                ClassDBUtils.ExecSQL(sSQL_Text, conn_db, transaction, ClassDBUtils.ExecMode.Log);
                count = ClassDBUtils.GetAffectedRowsCount(conn_db, transaction);
#endif
            }
            return 0;
        }
        public List<CheckChMon> LoadCheckChMon(CheckChMon finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Не задан пользователь";
                return null;
            }
            if (finder.yearr <= 0)
            {
                ret.result = false;
                ret.text = "Не задан год";
                return null;
            }

            if (finder.month_ <= 0)
            {
                ret.result = false;
                ret.text = "Не задан месяц";
                return null;
            }

            string where = " and month_ = " + finder.month_ + " and yearr = " + finder.yearr;


            #region ограничение по выбранным банкам данных
            where += " and pref in ( ";
            foreach (var t in finder.dopFind)
            {
                where += "'" + t.Trim() + "', ";
            }
            where += " '') ";
            #endregion ограничение по выбранным банкам данных
            string sql = "";
            finder.pref = Points.Pref;

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
#if PG
            string tablename_checkChMon = finder.pref + "_data.checkchmon";
#else
            string tablename_checkChMon = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":checkChMon";
#endif

            if (!TempTableInWebCashe(conn_db, tablename_checkChMon))
            {
                ret.result = false;
                ret.text = "Нет данных";
                ret.tag = -1;
                return null;
            }

            IDataReader reader;

            List<CheckChMon> list = new List<CheckChMon>();
            IDataReader reader2 = null;

            sql =
                " SELECT c.dat_check, c.note, c.nzp_grp, c.pref, c.name_prov, c.status_," +
                " c.is_critical, c.nzp_exc, e.rep_name " +
                " FROM  " + tablename_checkChMon + " c " +
                " LEFT OUTER JOIN " + DBManager.sDefaultSchema + "excel_utility e on c.nzp_exc = e.nzp_exc" +
                " WHERE 1=1 " + where +
                " ORDER BY pref, name_prov ";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            try
            {
                while (reader.Read())
                {
                    CheckChMon chmon = new CheckChMon();
                    if (reader["dat_check"] != DBNull.Value) chmon.dat_check = reader["dat_check"].ToString();
                    if (reader["note"] != DBNull.Value) chmon.note = Convert.ToString(reader["note"]).Trim();
                    if (reader["nzp_grp"] != DBNull.Value) chmon.nzp_grp = Convert.ToInt32(reader["nzp_grp"]);
                    if (reader["pref"] != DBNull.Value) chmon.pref = Convert.ToString(reader["pref"]).Trim();
                    if (reader["name_prov"] != DBNull.Value) chmon.name_prov = Convert.ToString(reader["name_prov"]).Trim();
                    if (reader["status_"] != DBNull.Value) chmon.status_ = Convert.ToInt32(reader["status_"]);
                    if (reader["is_critical"] != DBNull.Value) chmon.is_critical = Convert.ToInt32(reader["is_critical"]);
                    if (reader["nzp_exc"] != DBNull.Value) chmon.nzp_exc = Convert.ToInt32(reader["nzp_exc"]);
                    if (reader["rep_name"] != DBNull.Value) chmon.rep_name = Convert.ToString(reader["rep_name"]).Trim();

                    if (chmon.status_ == 2) chmon.status = "Не успешна";
                    else if (chmon.status_ == 1) chmon.status = "Успешна";

                    if (chmon.is_critical == 1) chmon.is_critical_name = "Обязательная";
                    else if (chmon.is_critical == 0) chmon.is_critical_name = "";// "Нет";

                    if (chmon.pref.Trim() != "")
                    {
                        if (chmon.nzp_grp > 0)
                        {
#if PG
                            string tablename_s_group = chmon.pref.Trim() + "_data.s_group";
#else
                            string tablename_s_group = chmon.pref.Trim() + "_data@" + DBManager.getServer(conn_db) + ":s_group";
#endif
                            sql = "select g.ngroup from " + tablename_s_group + " g where g.nzp_group = " + chmon.nzp_grp;
                            ret = ExecRead(conn_db, out reader2, sql, true);
                            if (!ret.result)
                            {
                                CloseReader(ref reader);
                                conn_db.Close();
                                return null;
                            }
                            if (reader2.Read())
                            {
                                if (reader2["ngroup"] != DBNull.Value) chmon.ngroup = Convert.ToString(reader2["ngroup"]).Trim();
                            }
                            reader2.Close();

#if PG
                            string tablename_link_group = chmon.pref.Trim() + "_data.link_group";
#else
                            string tablename_link_group = chmon.pref.Trim() + "_data@" + DBManager.getServer(conn_db) + ":link_group";
#endif
                            sql = "select count(*) as cnt from " + tablename_link_group + " where nzp_group = " + chmon.nzp_grp;
                            ret = ExecRead(conn_db, out reader2, sql, true);
                            if (!ret.result)
                            {
                                CloseReader(ref reader);
                                conn_db.Close();
                                return null;
                            }
                            if (reader2.Read())
                            {
                                if (reader2["cnt"] != DBNull.Value) chmon.count_ls = Convert.ToInt32(reader2["cnt"]);
                            }
                            reader2.Close();
                            if (chmon.ngroup.Trim() != "")
                            {
                                chmon.ngroup += " (Кол-во л/с: " + chmon.count_ls + ")";
                            }
                        }

                    }
                    _Point pnt = Points.GetPoint(chmon.pref);
                    chmon.point = pnt.point;

                    list.Add(chmon);
                }
                reader.Close();

                return list;
            }
            catch (Exception ex)
            {
                CloseReader(ref reader);
                CloseReader(ref reader2);
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка получения LoadCheckChMon " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }

        public bool InsertSysEvent(IDbConnection conn_db, string pref, int nzp_obj, int nzp_user, int nzp_dict,
            string note)
        {
            return InsertSysEvent(conn_db, null, pref, nzp_obj, nzp_user, nzp_dict, note);
        }

        public bool InsertSysEvent(IDbConnection conn_db, IDbTransaction transaction, string pref, int nzp_obj, int nzp_user, int nzp_dict, string note)
        {
            // nzp_dict =7428
#if PG
            var sSQL_Text = " insert into " + pref + "_data.sys_events(DATE_,NZP_USER,NZP_DICT_EVENT,NZP,NOTE) " +
                               " values( now(), " + nzp_user.ToString("") + ", " + nzp_dict.ToString("") + "," + nzp_obj.ToString("") + ",'" + note + "' ) ";
#else
            string sSQL_Text = " insert into " + pref + "_data" + tableDelimiter + "sys_events(DATE_,NZP_USER,NZP_DICT_EVENT,NZP,NOTE) " +
                               " values( sysdate year to fraction, " + nzp_user.ToString("") + ", " + nzp_dict.ToString("") + "," + nzp_obj.ToString("") + ",'" + note + "' ) ";
#endif
            ClassDBUtils.ExecSQL(sSQL_Text, conn_db, transaction, ClassDBUtils.ExecMode.Log);
            return true;
        }

        #endregion Закрытие и откат месяца. Проверки расчетов

        #endregion Представлены функции: Закрытие и откат месяца. Проверки расчетов.
        #endregion

        public List<PerekidkaLsToLs> LoadSumsForPerekidkaLsToLs(PerekidkaLsToLs finder, out Returns ret)
        {
            #region проверки
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Пользователь не определен");
                return null;
            }

            if (finder.dat_uchet == "")
            {
                ret = new Returns(false, "Не задан операционный день");
                return null;
            }

            if (finder.nzp_kvar < 1)
            {
                ret = new Returns(false, "Не задан л/с");
                return null;
            }

            if (finder.nzp_kvar2 < 1)
            {
                ret = new Returns(false, "Не задан эталон");
                return null;
            }

            if (finder.etalon < 1)
            {
                ret = new Returns(false, "Не задан л/с");
                return null;
            }

            if (finder.pref == "")
            {
                ret = new Returns(false, "Не задан префикс");
                return null;
            }

            if (finder.pref2 == "")
            {
                ret = new Returns(false, "Не задан префикс");
                return null;
            }
            #endregion

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            IDataReader reader;

            List<PerekidkaLsToLs> list1 = new List<PerekidkaLsToLs>();
            List<PerekidkaLsToLs> list2 = new List<PerekidkaLsToLs>();

            string sum_etalon = "";

            switch (finder.etalon)
            {
                case 1: sum_etalon = " sum(sum_charge) "; break;
                case 2: sum_etalon = " sum(sum_tarif) "; break;
                case 3: sum_etalon = " sum(sum_insaldo) "; break;
                case 4: sum_etalon = " sum(sum_outsaldo) "; break;
                case 5: sum_etalon = " sum(rsum_tarif) "; break;
            }

            string table_charge = finder.pref + "_charge_" + (Convert.ToDateTime(finder.dat_uchet).Year % 100).ToString("00") + tableDelimiter + "charge_" +
                                  Convert.ToDateTime(finder.dat_uchet).Month.ToString("00");
            string table_charge2 = finder.pref2 + "_charge_" + (Convert.ToDateTime(finder.dat_uchet).Year % 100).ToString("00") + tableDelimiter + "charge_" +
                                  Convert.ToDateTime(finder.dat_uchet).Month.ToString("00");

            string sql = " Select ch.nzp_serv, s.service, sp.name_supp, sp.nzp_supp, " + sum_etalon + " sum_etalon ,sum(sum_outsaldo) sum_outsaldo" +
                            " From " + table_charge + " ch" +
                             ", " + finder.pref + "_kernel" + tableDelimiter + "services s" +
                             ", " + finder.pref + "_kernel" + tableDelimiter + "supplier sp" +
                                " Where ch.nzp_kvar = " + finder.nzp_kvar +
                                " and ch.dat_charge is null" +
                                " and ch.nzp_serv > 1" +
                                " and ch.nzp_serv = s.nzp_serv" +
                                " and ch.nzp_supp = sp.nzp_supp" +
                                " group by 1,2,3,4 " +
                                " order by 2, 3";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            while (reader.Read())
            {
                PerekidkaLsToLs zap = new PerekidkaLsToLs();
                if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                if (reader["sum_etalon"] != DBNull.Value) zap.sum_etalon = Convert.ToDecimal(reader["sum_etalon"]).ToString();
                if (reader["sum_outsaldo"] != DBNull.Value) zap.sum_outsaldo = Convert.ToDecimal(reader["sum_outsaldo"]).ToString();
                if (reader["name_supp"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]).Trim();
                list1.Add(zap);
            }

            sql = " Select ch.nzp_serv, s.service, sp.name_supp, sp.nzp_supp, " + sum_etalon + " as sum_etalon ,sum(sum_outsaldo) as sum_outsaldo" +
                    " From " + table_charge2 + " ch" +
                             ", " + finder.pref + "_kernel" + tableDelimiter + "services s" +
                             ", " + finder.pref + "_kernel" + tableDelimiter + "supplier sp" +
                        " Where ch.nzp_kvar = " + finder.nzp_kvar2 +
                        " and ch.dat_charge is null" +
                        " and ch.nzp_serv > 1" +
                        " and ch.nzp_serv = s.nzp_serv" +
                        " and ch.nzp_supp = sp.nzp_supp" +
                        " group by 1,2,3,4" +
                                " order by 2, 3";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            while (reader.Read())
            {
                PerekidkaLsToLs zap = new PerekidkaLsToLs();
                if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                if (reader["sum_etalon"] != DBNull.Value) zap.sum_etalon2 = Convert.ToDecimal(reader["sum_etalon"]).ToString();
                if (reader["sum_outsaldo"] != DBNull.Value) zap.sum_outsaldo2 = Convert.ToDecimal(reader["sum_outsaldo"]).ToString();
                if (reader["name_supp"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]).Trim();
                list2.Add(zap);
            }


            foreach (PerekidkaLsToLs plsls1 in list1)
            {
                foreach (PerekidkaLsToLs plsls2 in list2)
                {
                    if (plsls1.nzp_serv == plsls2.nzp_serv &&
                        plsls1.nzp_supp == plsls2.nzp_supp)
                    {
                        plsls2.added = true;
                        plsls1.sum_etalon2 = plsls2.sum_etalon2;
                        plsls1.sum_outsaldo2 = plsls2.sum_outsaldo2;
                    }
                }
            }

            foreach (PerekidkaLsToLs plsls2 in list2)
            {
                if (!plsls2.added)
                {
                    PerekidkaLsToLs plsls = new PerekidkaLsToLs();
                    plsls.sum_etalon2 = plsls2.sum_etalon2;
                    plsls.sum_outsaldo2 = plsls2.sum_outsaldo2;
                    plsls.service = plsls2.service;
                    plsls.nzp_serv = plsls2.nzp_serv;
                    plsls.nzp_supp = plsls2.nzp_supp;
                    plsls.name_supp = plsls2.name_supp;
                    list1.Add(plsls);
                }
            }

            reader.Close();
            conn_db.Close();
            return list1;
        }



        public int GetNumLsFromNzpKvar(int nzp_kvar, string pref, IDbConnection conn_db, IDbTransaction transaction)
        {
#if PG
            string sql = "select num_ls from " + pref + "_data.kvar where nzp_kvar = " + nzp_kvar;
#else
            string sql = "select num_ls from " + pref + "_data@" + DBManager.getServer(conn_db) + ":kvar where nzp_kvar = " + nzp_kvar;
#endif
            IDataReader reader;
            Returns ret = ExecRead(conn_db, transaction, out reader, sql, true);
            if (!ret.result) return 0;
            int num_ls = 0;
            if (reader.Read())
            {
                if (reader["num_ls"] != DBNull.Value) num_ls = Convert.ToInt32(reader["num_ls"]);
            }
            return num_ls;
        }

        #region Формирование статистики начислений в отдельном потоке
        public delegate void GetNachislDelegate(int mode, int month_, int year_, int user_);

        public void _ACallback_(IAsyncResult ar)
        {
            //GetNachislDelegate dlgt = (GetNachislDelegate)ar.AsyncState;
            //dlgt.EndInvoke(ar);
            //MonitorLog.WriteLog("Загрузка завершена", MonitorLog.typelog.Info, true);
        }

        public void _Invoke_(int mode, int month_, int year_, int user_)
        {
            GetNachislDelegate dlgt = new GetNachislDelegate(this.GetNachisl);
            AsyncCallback cb = new AsyncCallback(_ACallback_);
            IAsyncResult ar = dlgt.BeginInvoke(mode, month_, year_, user_, cb, dlgt);
        }


        public void GetNachisl(int mode, int month_, int year_, int user_)  //mode=0 - в разрезе ЛС, 1 - домов, 2 - участков
        {
            Returns ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;
#if PG
            //string tXX_spls_full = conn_web.Database + "." + "t" + Convert.ToString(user_) + "_spls";
            string tXX_spls_full = pgDefaultDb + "." + "t" + Convert.ToString(user_) + "_spls";
#else
            string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "t" + Convert.ToString(user_) + "_spls";
#endif
            conn_web.Close();

            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            Returns ret1 = OpenDb(conn_db, true);
            if (!ret1.result) return;

            #region Запись в таблицу файлов
            ExcelRepClient dbRep = new ExcelRepClient();
            ret = dbRep.AddMyFile(new ExcelUtility()
            {
                nzp_user = user_,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = "Статистика начислений по " + (mode == 0 ? "лицевым счетам" : (mode == 1 ? " домам" : " участкам"))
            });
            if (!ret.result) { return; }

            int nzpExc = ret.tag;
            string destinationFilename = "";
            #endregion

            try
            {
                string table_name = "t_kvars" + user_;
                #region основная таблица
                #region
                //string sql = "drop table " + table_name + "";
                //ret = ExecSQL(conn_db, sql, true);
                //sql = "create table " + table_name + " (nzp_kvar integer, num_ls integer, nzp_dom integer, nzp_area integer, nzp_geu integer," +
                //     "nzp_ul integer, fio nchar(40), ulica nchar(40), ndom nchar(10), idom nchar(10), ikvar nchar(10), pref nchar(40)," +
                //     "p1 char(2), p2 char(2), p3 char(2), p4 char(2), p5 char(2), p6 char(10), p7 char(10), p8 char(2), p9 decimal(14,2)," +
                //     "p10 decimal(14,2), p11 decimal(14,2), p12 decimal(14,2),p13 decimal(14,2),p14 decimal(14,2),p15 decimal(14,2),p16 decimal(14,2)," +
                //     "p17 decimal(14,2),p18 decimal(14,2),p19 decimal(14,2),p20 decimal(14,2),real_ch decimal(14,2)) ";
                //ret = ExecSQL(conn_db, sql, true);
                //if (!ret.result) throw new Exception(ret.text);
                //sql = "insert into " + table_name + " (nzp_kvar,num_ls,nzp_dom,nzp_area,nzp_geu,nzp_ul,fio,ulica,ndom,idom,ikvar,pref,p1,p2,p3,p4,p5,p6,p7,p8,p9," +
                //     "p10,p11,p12,p13,p14,p15,p16,p17,p18,p19,p20,real_ch) " +
                //     "select nzp_kvar, num_ls, nzp_dom, nzp_area, nzp_geu, nzp_ul, fio, ulica, ndom, idom, ikvar, pref, " +
                //       "cast( '' as char(2) ) as p1, " +
                //       "cast( '' as char(1) ) as p2, " +
                //       "cast( '' as char(2) ) as p3, " +
                //       "cast( '' as char(2) ) as p4, " +
                //       "cast( '' as char(2) ) as p5, " +
                //       "cast( '' as char(10) ) as p6, " +
                //       "cast( '' as char(10) ) as p7, " +
                //       "cast( '' as char(2) ) as p8, " +
                //       "cast( 0 as decimal(14,2) ) as p9, " +
                //       "cast( 0 as decimal(14,2) ) as p10, " +
                //       "cast( 0 as decimal(14,2) ) as p11, " +
                //       "cast( 0 as decimal(14,2) ) as p12, " +
                //       "cast( 0 as decimal(14,2) ) as p13, " +
                //       "cast( 0 as decimal(14,2) ) as p14, " +
                //       "cast( 0 as decimal(14,2) ) as p15, " +
                //       "cast( 0 as decimal(14,2) ) as p16, " +
                //       "cast( 0 as decimal(14,2) ) as p17, " +
                //       "cast( 0 as decimal(14,2) ) as p18, " +
                //       "cast( 0 as decimal(14,2) ) as p19, " +
                //       "cast( 0 as decimal(14,2) ) as p20, " +
                //       "cast( 0 as decimal(14,2) ) as real_ch " +
                //      "from  " + tXX_spls_full;
                #endregion

#if PG
                string sql = "select nzp_kvar, num_ls, nzp_dom, nzp_area, nzp_geu, nzp_ul, fio, ulica, ndom, idom, ikvar, pref, " +
                                    "cast( '' as char(2) ) as p1, " +
                                    "cast( '' as char(1) ) as p2, " +
                                    "cast( 0  as integer ) as p3, " +
                                    "cast( 0  as integer ) as p4, " +
                                    "cast( 0  as integer ) as p5, " +
                                    "cast( 0 as numeric(14,2) ) as p6, " +
                                    "cast( 0 as numeric(14,2) ) as p7, " +
                                    "cast( 0 as numeric(14,2) ) as p8, " +
                                    "cast( 0 as numeric(14,2) ) as p9, " +
                                    "cast( 0 as numeric(14,2) ) as p10, " +
                                    "cast( 0 as numeric(14,2) ) as p11, " +
                                    "cast( 0 as numeric(14,2) ) as p12, " +
                                    "cast( 0 as numeric(14,2) ) as p13, " +
                                    "cast( 0 as numeric(14,2) ) as p14, " +
                                    "cast( 0 as numeric(14,2) ) as p15, " +
                                    "cast( 0 as numeric(14,2) ) as p16, " +
                                    "cast( 0 as numeric(14,2) ) as p17, " +
                                    "cast( 0 as numeric(14,2) ) as p18, " +
                                    "cast( 0 as numeric(14,2) ) as p19, " +
                                    "cast( 0 as numeric(14,2) ) as p20, " +
                                    "cast( 0 as numeric(14,2) ) as real_ch " +
                                    " into unlogged " + table_name + " " +
                                    " from  " + tXX_spls_full +
                                    " order by ulica, ndom, idom, ikvar ";
#else
                string sql = "select nzp_kvar, num_ls, nzp_dom, nzp_area, nzp_geu, nzp_ul, fio, ulica, ndom, idom, ikvar, pref, " +
                                    "cast( '' as char(2) ) as p1, " +
                                    "cast( '' as char(1) ) as p2, " +
                                    "cast( 0  as integer ) as p3, " +
                                    "cast( 0  as integer ) as p4, " +
                                    "cast( 0  as integer ) as p5, " +
                                    "cast( 0 as decimal(14,2) ) as p6, " +
                                    "cast( 0 as decimal(14,2) ) as p7, " +
                                    "cast( '' as char(2) ) as p8, " +
                                    "cast( 0 as decimal(14,2) ) as p9, " +
                                    "cast( 0 as decimal(14,2) ) as p10, " +
                                    "cast( 0 as decimal(14,2) ) as p11, " +
                                    "cast( 0 as decimal(14,2) ) as p12, " +
                                    "cast( 0 as decimal(14,2) ) as p13, " +
                                    "cast( 0 as decimal(14,2) ) as p14, " +
                                    "cast( 0 as decimal(14,2) ) as p15, " +
                                    "cast( 0 as decimal(14,2) ) as p16, " +
                                    "cast( 0 as decimal(14,2) ) as p17, " +
                                    "cast( 0 as decimal(14,2) ) as p18, " +
                                    "cast( 0 as decimal(14,2) ) as p19, " +
                                    "cast( 0 as decimal(14,2) ) as p20, " +
                                    "cast( 0 as decimal(14,2) ) as p21, " +
                                    "cast( 0 as decimal(14,2) ) as p22, " +
                                    "cast( 0 as decimal(14,2) ) as p23, " +
                                    "cast( 0 as decimal(14,2) ) as p24, " +
                                    "cast( 0 as decimal(14,2) ) as real_ch, " +
                                    "cast( 0 as decimal(14,2) ) as sum_insaldo, " +
                                    "cast( 0 as decimal(14,2) ) as oplata " +
                                    "from  " + tXX_spls_full +
                                    " order by ulica, ndom, idom, ikvar " +
                                    " into temp " + table_name;
#endif
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                sql = "create index ix_" + table_name + " on " + table_name + " (nzp_dom , nzp_kvar)";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                sql = "create index ix_" + table_name + "1 on " + table_name + " ( nzp_kvar, p1, p2,p3,p4,p5,p6,p7,p8,p9,p10)";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                sql = "create index ix_" + table_name + "2 on " + table_name + " ( nzp_kvar, p11, p12,p13,p14,p15,p16,p17,p18,p19,p20)";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                //процент загрузки
                dbRep.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 0.01M });


                IDataReader reader;
#if PG
                sql = "select distinct pref from " + table_name;
#else
                sql = "select unique pref from " + table_name;
#endif
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                DateTime dat = new DateTime(year_, month_, 1);
                string ddat = Utils.EStrNull(dat.ToShortDateString());
                string ddat_po = "";
                if (month_ != 12)
                {
                    DateTime dat_po = new DateTime(year_, month_ + 1, 1);
                    ddat_po = Utils.EStrNull(dat_po.ToShortDateString());
                }
                else
                {
                    DateTime dat_po = new DateTime(year_ + 1, 1, 1);
                    ddat_po = Utils.EStrNull(dat_po.ToShortDateString());
                }

                //процент загрузки
                dbRep.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 0.05M });

                MonitorLog.WriteLog("Старт отчет : 1 ", MonitorLog.typelog.Info, 1, 2, true);

                while (reader.Read())
                {
                    if (reader["pref"] != DBNull.Value)
                    {
                        #region параметры ЛС
                        string pref = reader["pref"].ToString().Trim();
                        MonitorLog.WriteLog("2.характеристики  ", MonitorLog.typelog.Info, 1, 2, true);
                        // количество счетов 
#if PG
                        sql = " update " + table_name + " set p1 = ( " +
                            "select distinct b.val_prm from " + pref + "_data.prm_1 b where b.nzp_prm =21 and b.val_prm='1' and b.nzp=" + table_name + ".nzp_kvar " +
                            "and " + ddat + " between b.dat_s and b.dat_po )" +
                            " where pref = '" + pref + "'";
#else
                        sql = " update " + table_name + " set p1 = ( " +
                            "select max( b.val_prm) from " + pref + "_data" + tableDelimiter + "prm_1 b where b.nzp_prm =21 and b.val_prm=1 and b.nzp=" + table_name + ".nzp_kvar " +
                            "and " + ddat + " between b.dat_s and b.dat_po and b.is_actual<>100)" +
                            " where pref = '" + pref + "'";
#endif
                        ret = ExecSQL(conn_db, sql, true);
                        // if (!ret.result) throw new Exception(ret.text);
                        // приватизированных квартир

#if PG
                        sql = " update " + table_name + " set p2 = ( " +
                                                   "select distinct b.val_prm from " + pref + "_data.prm_1 b where b.nzp_prm =8 and b.nzp=" + table_name + ".nzp_kvar " +
                                                   "and " + ddat + " between b.dat_s and b.dat_po )" +
                                                   " where pref = '" + pref + "'";
#else
                        sql = " update " + table_name + " set p2 = ( " +
                            "select max( b.val_prm) from " + pref + "_data" + tableDelimiter + "prm_1 b where b.nzp_prm =8 and b.nzp=" + table_name + ".nzp_kvar " +
                            "and " + ddat + " between b.dat_s and b.dat_po  and b.is_actual<>100  )" +
                            " where pref = '" + pref + "'";
#endif

                        ret = ExecSQL(conn_db, sql, true);
                        // if (!ret.result) throw new Exception(ret.text);
                        // количество жильцов  
#if PG
                        sql = " update " + table_name + " set p3 = ( select sum(cast(coalesce(a.val_prm,'') as numeric(14,2))) from " + pref + "_data.prm_1 a where a.nzp_prm in(5) and a.nzp=" + table_name + ".nzp_kvar " +
                                                       " and " + ddat + " between a.dat_s and a.dat_po )" +
                                                      " where pref = '" + pref + "'";
#else
                        sql = " update " + table_name + " set p3 = ( select sum(cast(nvl(a.val_prm,0) as decimal(14,2))) from " + pref + "_data" + tableDelimiter + "prm_1 a where a.nzp_prm in(5) and a.nzp=" + table_name + ".nzp_kvar " +
                                                        " and " + ddat + " between a.dat_s and a.dat_po  and a.is_actual<>100 )" +
                                                       " where pref = '" + pref + "'";
#endif
                        ret = ExecSQL(conn_db, sql, true);
                        // if (!ret.result) throw new Exception(ret.text);
                        // количество временно прибывших 
#if PG
                        sql = " update " + table_name + " set p4 = ( select sum(cast(coalesce(a.val_prm,'') as numeric(14,2))) from " + pref + "_data.prm_1 a where a.nzp_prm in(131) and a.nzp=" + table_name + ".nzp_kvar " +
                                                       " and " + ddat + " between a.dat_s and a.dat_po )" +
                                                      " where pref = '" + pref + "'";
#else
                        sql = " update " + table_name + " set p4 = ( select sum(cast(nvl(a.val_prm,0) as decimal(14,2))) from " + pref + "_data" + tableDelimiter + "prm_1 a where a.nzp_prm in(131) and a.nzp=" + table_name + ".nzp_kvar " +
                                                       " and " + ddat + " between a.dat_s and a.dat_po   and a.is_actual<>100 )" +
                                                      " where pref = '" + pref + "'";
#endif
                        ret = ExecSQL(conn_db, sql, true);
                        // if (!ret.result) throw new Exception(ret.text);
                        // временно выбывшие 
#if PG
                        sql = " update " + table_name + " set p5 = ( select sum(cast(coalesce(a.val_prm,'') as numeric(14,2))) from " + pref + "_data.prm_1 a where a.nzp_prm in(10) and a.nzp=" + table_name + ".nzp_kvar " +
                                                       " and " + ddat + " between a.dat_s and a.dat_po )" +
                                                      " where pref = '" + pref + "'";
#else
                        sql = " update " + table_name + " set p5 = ( select sum(cast(nvl(a.val_prm,0) as decimal(14,2))) from " + pref + "_data" + tableDelimiter + "prm_1 a where a.nzp_prm in(10) and a.nzp=" + table_name + ".nzp_kvar " +
                                                     " and " + ddat + " between a.dat_s and a.dat_po  and a.is_actual<>100)" +
                                                    " where pref = '" + pref + "'";
#endif
                        ret = ExecSQL(conn_db, sql, true);
                        // if (!ret.result) throw new Exception(ret.text);
                        // общая площадь
#if PG
                        sql = " update " + table_name + " set p6 = ( " +
                                                   "select sum(cast(coalesce(a.val_prm,'') as numeric(14,2))) from " + pref + "_data.prm_1 a where a.nzp_prm =4 and a.nzp=" + table_name + ".nzp_kvar " +
                                                   " and " + ddat + " between a.dat_s and a.dat_po ) " +
                                                   " where pref = '" + pref + "'";
#else
                        sql = " update " + table_name + " set p6 = ( " +
                                                   "select sum(cast(nvl(a.val_prm,0) as decimal(14,2))) from " + pref + "_data" + tableDelimiter + "prm_1 a where a.nzp_prm =4 and a.nzp=" + table_name + ".nzp_kvar " +
                                                   " and " + ddat + " between a.dat_s and a.dat_po  and a.is_actual<>100) " +
                                                   " where pref = '" + pref + "'";
#endif
                        ret = ExecSQL(conn_db, sql, true);
                        // if (!ret.result) throw new Exception(ret.text);
                        // жилая площадь 
#if PG
                        sql = "update " + table_name + " set p7 = ( " +
                                                   "select sum(cast(coalesce(a.val_prm,'') as numeric(14,2))) from " + pref + "_data.prm_1 a where a.nzp_prm in(6) and a.nzp=" + table_name + ".nzp_kvar " +
                                                   "and " + ddat + " between a.dat_s and a.dat_po )" +
                                                   " where pref = '" + pref + "'";
#else
                        sql = "update " + table_name + " set p7 = ( " +
                                                   "select sum(cast(nvl(a.val_prm,0) as decimal(14,2))) from " + pref + "_data" + tableDelimiter + "prm_1 a where a.nzp_prm in(6) and a.nzp=" + table_name + ".nzp_kvar " +
                                                   "and " + ddat + " between a.dat_s and a.dat_po  and a.is_actual<>100 )" +
                                                   " where pref = '" + pref + "'";
#endif
                        ret = ExecSQL(conn_db, sql, true);
                        // if (!ret.result) throw new Exception(ret.text);
                        // количество комнат 
#if PG
                        sql = "update " + table_name + " set p8 = (" +
                                                 "select sum(cast(coalesce(a.val_prm,'') as numeric(14,2))) from " + pref + "_data.prm_1 a where a.nzp_prm =107 and a.nzp=" + table_name + ".nzp_kvar " +
                                                 "and " + ddat + " between a.dat_s and a.dat_po )" +
                                                 " where pref = '" + pref + "'";
#else
                        sql = "update " + table_name + " set p8 = (" +
                                                 "select sum(cast(nvl(a.val_prm,0) as decimal(14,2))) from " + pref + "_data" + tableDelimiter + "prm_1 a where a.nzp_prm =107 and a.nzp=" + table_name + ".nzp_kvar " +
                                                 "and " + ddat + " between a.dat_s and a.dat_po  and a.is_actual<>100 )" +
                                                 " where pref = '" + pref + "'";
#endif
                        ret = ExecSQL(conn_db, sql, true);
                        //if (!ret.result) throw new Exception(ret.text);
                        #endregion

                        //процент загрузки
                        dbRep.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 0.1M });

                        MonitorLog.WriteLog("3.начисления  ", MonitorLog.typelog.Info, 1, 2, true);
                        #region начисления
                        int year = year_;
                        if (Convert.ToString(year_).Trim().Length == 4)
                        {
                            year = year_ % 100;
                        }
                        string[] field_name = new string[] { "p9", "p10", "p11", "p12", "p13", "p14", "p15", "p16", "p17", "p18", "p19", "p20", "p21", "p22", "p23", "p24" };
                        int[] nzp_serv = new int[] { 2, 266, 15, 6, 7, 8, 9, 16, 5, 26, 206, 515, 251, 274, 17, 464 };
                        string serv = "";

                        for (int i = 0; i < field_name.Length; i++)
                        {
                            serv += nzp_serv[i].ToString().Trim();
                            if ((i + 1) != field_name.Length) serv += ",";

                            sql = " update " + table_name + " set " + field_name[i] + " = (" +
                                    "Select  sum(c.sum_tarif +  c.reval + " + sNvlWord + "(c.real_charge, 0))  from " + pref + "_charge_" + year.ToString() + " : charge_" + month_.ToString("00") + " c " +
                                    "Where c.nzp_kvar=" + table_name + ".nzp_kvar and c.dat_charge is null and c.nzp_serv <>1   and c.nzp_serv = " + nzp_serv[i].ToString() +
                                    " group by c.num_ls, c.nzp_serv)" +
                                    " where pref = '" + pref + "'";

                            ret = ExecSQL(conn_db, sql, true);
                            // if (!ret.result) throw new Exception(ret.text);
                        }
#if PG
                        sql = " update " + table_name + " set real_ch = (" +
                                                      "Select  sum(c.real_charge)  from " + pref + "_charge_" + year.ToString() + " . charge_" + month_.ToString("00") + " c " +
                                                      "Where c.nzp_kvar=" + table_name + ".nzp_kvar and c.dat_charge is null and c.nzp_serv in (" + serv + ") ) " +
                                                      " where pref = '" + pref + "'";
#else
                        sql = " update " + table_name + " set real_ch = (" +
                                "Select  sum(c.real_charge)  from " + pref + "_charge_" + year.ToString() + " : charge_" + month_.ToString("00") + " c " +
                                "Where c.nzp_kvar=" + table_name + ".nzp_kvar and c.dat_charge is null and c.nzp_serv <>1  and c.nzp_serv in (" + serv + ") ) " +
                                " where pref = '" + pref + "'";
#endif
                        ret = ExecSQL(conn_db, sql, true);
                        // if (!ret.result) throw new Exception(ret.text);
                        #endregion

                        //процент загрузки
                        dbRep.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 0.15M });

                        #region участок
#if PG
                        sql = " update " + table_name + " set nzp_geu = " +
                                                       "(Select  uch  from " + pref + "_data . kvar k  Where k.nzp_kvar = " + table_name + ".nzp_kvar ) " +
                                                       " where pref = '" + pref + "'";
#else
                        sql = " update " + table_name + " set nzp_geu = " +
                                                       "(Select  uch  from " + pref + "_data : kvar k  Where k.nzp_kvar = " + table_name + ".nzp_kvar ) " +
                                                       " where pref = '" + pref + "'";
#endif
                        ret = ExecSQL(conn_db, sql, true);
                        // if (!ret.result) throw new Exception(ret.text);


                        // входящее сальдо
#if PG
                        sql = " update " + table_name + " set sum_insaldo = ( select sum(cast(coalesce(a.sum_insaldo,'') as numeric(14,2))) from " + pref + "_charge_" + (year_ - 2000) + tableDelimiter +
                              " charge " + month_.ToString("00") + " a  where a.nzp_kvar = " + table_name + ".nzp_kvar and a.num_ls = " + table_name + ".num_lsand nzp_serv <> 1) where pref = '" + pref + "'";
#else
                        sql = " update " + table_name + " set sum_insaldo = ( select sum(cast(nvl(a.sum_insaldo,0) as decimal(14,2))) from " + pref + "_charge_" + (year_ - 2000) + tableDelimiter +
                              " charge_" + month_.ToString().PadLeft(2, '0') + " a  where a.nzp_kvar = " + table_name + ".nzp_kvar and a.num_ls = " + table_name + ".num_ls and nzp_serv <> 1) where pref = '" + pref + "'";
#endif
                        ret = ExecSQL(conn_db, sql, true);
                        // if (!ret.result) throw new Exception(ret.text);



                        // Оплата
#if PG
                        sql = " update " + table_name + " set oplata = ( select sum(cast(coalesce(a.sum_money,'') as numeric(14,2))) from " + pref + "_charge_" + (year_ - 2000) + tableDelimiter +
                              " charge " + month_.ToString("00") + " a  where a.nzp_kvar = " + table_name + ".nzp_kvar and a.num_ls = " + table_name + ".num_ls) where pref = '" + pref + "'";
#else
                        sql = " update " + table_name + " set oplata = ( select sum(cast(nvl(a.sum_money,0) as decimal(14,2))) from " + pref + "_charge_" + (year_ - 2000) + tableDelimiter +
                              " charge_" + month_.ToString("00") + " a  where a.nzp_kvar = " + table_name + ".nzp_kvar and a.num_ls = " + table_name + ".num_ls)  where pref = '" + pref + "'";
#endif
                        ret = ExecSQL(conn_db, sql, true);
                        // if (!ret.result) throw new Exception(ret.text);

                        #endregion
                    }
                }
                reader.Close();

                //процент загрузки
                dbRep.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 0.2M });
                #endregion

                #region датасет
                DataTable table = new DataTable();
                table.TableName = "Q_master";

                MonitorLog.WriteLog("3. Отображение  ", MonitorLog.typelog.Info, 1, 2, true);
                switch (mode)
                {
                    case 0:
                        {
                            #region По ЛС
                            table.Columns.Add("num_ls", typeof(string));
                            table.Columns.Add("ulica", typeof(string));
                            table.Columns.Add("dom", typeof(string));
                            table.Columns.Add("kv", typeof(string));
                            table.Columns.Add("kolsch", typeof(string));
                            table.Columns.Add("privat", typeof(string));
                            table.Columns.Add("fio", typeof(string));
                            table.Columns.Add("kolgil", typeof(int));
                            table.Columns.Add("kolvr", typeof(int));
                            table.Columns.Add("kolvibit", typeof(int));
                            table.Columns.Add("pl_ob", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("pl_gil", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("kolkom", typeof(string));
                            table.Columns.Add("in_saldo", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p1", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p2", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p3", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p4", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p5", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p6", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p7", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p8", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p9", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p10", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p11", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p12", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p21", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p22", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p23", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p14", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p13", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("oplata", System.Type.GetType("System.Decimal"));

#if PG
                            sql = "select num_ls, ulica, ndom as dom, nkvar as kv, coalesce(p1,'1') as kolsch, replace(coalesce(p2,''),'1','+') as privat, fio, coalesce(p3,0) as kolgil, coalesce(p4,0) as kolvr, coalesce(p5,0) as kolvibit, " +
                                                             " coalesce(p6,0) as pl_ob, coalesce(p7,0) as pl_gil, p8 as kolkom, sum_insaldo as in_saldo,coalesce(p9,0) as p1, coalesce(p10,0) as p2, coalesce(p11,0) as p3, coalesce(p12,0) as p4, coalesce(p13,0) as p5, coalesce(p14,0) as p6, " +
                                                             " coalesce(p15,0) as p7, coalesce(p16,0) as p8, coalesce(p17,0) as p9, coalesce(p18,0) as p10, coalesce(p20,0) as p11, coalesce(p19,0) as p12, coalesce(p21,0) as p21,coalesce(p22,0) as p22,coalesce(p23,0) as p23,  coalesce(real_ch,0) as p13," +
                                                             " (coalesce(sum_insaldo,0)+coalesce(p9,0)+coalesce(p10,0)+coalesce(p11,0)+coalesce(p12,0)+coalesce(p13,0)+coalesce(p14,0)+coalesce(p15,0)+coalesce(p16,0)+coalesce(p17,0)+coalesce(p18,0)+coalesce(p19,0)+coalesce(p20,0)+coalesce(p21,0)+coalesce(p22,0)+coalesce(p23,0)+coalesce(real_ch,0)) as p14, oplata " +
                                                             "from " + table_name + "";
#else
                            sql = "select num_ls, ulica, ndom as dom, ikvar as kv, nvl(p1,'1') as kolsch, replace(nvl(p2,''),'1','+') as privat, fio, nvl(p3,0) as kolgil, nvl(p4,0) as kolvr, nvl(p5,0) as kolvibit, " +
                                                             " nvl(p6,0) as pl_ob, nvl(p7,0) as pl_gil, p8 as kolkom, sum_insaldo as in_saldo,nvl(p9,0) as p1, nvl(p10,0) as p2, nvl(p11,0) as p3, nvl(p12,0) as p4, nvl(p13,0) as p5, nvl(p14,0) as p6, " +
                                                             " nvl(p15,0) as p7, nvl(p16,0) as p8, nvl(p17,0) as p9, nvl(p18,0) as p10, nvl(p20,0) as p11, nvl(p19,0) as p12, nvl(p21,0) as p21, nvl(p22,0) as p22,nvl(p23,0) as p23, nvl(real_ch,0) as p13," +
                                                             " (nvl(sum_insaldo,0)+nvl(p9,0)+nvl(p10,0)+nvl(p11,0)+nvl(p12,0)+nvl(p13,0)+nvl(p14,0)+nvl(p15,0)+nvl(p16,0)+nvl(p17,0)+nvl(p18,0)+nvl(p19,0)+nvl(p20,0)+nvl(p21,0)+nvl(p22,0)+nvl(p23,0)) as p14, oplata " +
                                                             "from " + table_name + "";
#endif


                            ret = ExecRead(conn_db, out reader, sql, true);
                            if (!ret.result) throw new Exception(ret.text);
                            MonitorLog.WriteLog("5.заполнение  ", MonitorLog.typelog.Info, 1, 2, true);

                            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                            culture.NumberFormat.NumberDecimalSeparator = ".";
                            culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                            System.Threading.Thread.CurrentThread.CurrentCulture = culture;

                            table.Load(reader, LoadOption.OverwriteChanges);

                            break;
                            #endregion
                        }
                    case 1:
                        {
                            #region По домам
                            table.Columns.Add("ulica", typeof(string));
                            table.Columns.Add("dom", typeof(string));
                            table.Columns.Add("uch", typeof(string));
                            table.Columns.Add("obS", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("in_saldo", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p1", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p2", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p3", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p4", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p5", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p6", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p7", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p8", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p9", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p10", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p11", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p12", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p21", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p22", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p23", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p14", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p13", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("oplata", System.Type.GetType("System.Decimal"));
#if PG
                            sql = "select ulica, ndom as dom, nzp_geu as uch, sum(coalesce(sum_insaldo,0)) as in_saldo, sum(coalesce(p9,0)) as p1, sum(coalesce(p10,0)) as p2, sum(coalesce(p11,0)) as p3, sum(coalesce(p12,0)) as p4, " +
                                                              "  sum(coalesce(p13,0)) as p5, sum(coalesce(p14,0)) as p6, sum(coalesce(p15,0)) as p7, sum(coalesce(p16,0)) as p8, sum(coalesce(p17,0)) as p9, sum(coalesce(p18,0)) as p10, " +
                                                              "  sum(coalesce(p19,0)) as p11, sum(coalesce(p20,0)) as p12, sum(coalesce(p21,0)) as p21, sum(coalesce(real_ch,0)) as p13, " +
                                                              " sum((coalesce(sum_insaldo,0)+coalesce(p9,0)+coalesce(p10,0)+coalesce(p11,0)+coalesce(p12,0)+coalesce(p13,0)+coalesce(p14,0)+coalesce(p15,0)+coalesce(p16,0)+coalesce(p17,0)+coalesce(p18,0)+coalesce(p19,0)+coalesce(p20,0)+coalesce(p21,0)+coalesce(p22,0)+coalesce(p23,0)+coalesce(real_ch,0))) as p14, sum(coalesce(oplata,0)) as oplata " +
                                                              " from " + table_name + " group by nzp_dom, ulica, ndom, nzp_geu ";
#else
                            sql = "select ulica, ndom as dom, nzp_geu as uch, sum(nvl(p6,0)) as obS, sum(nvl(sum_insaldo,0)) as in_saldo, sum(nvl(p9,0)) as p1, sum(nvl(p10,0)) as p2, sum(nvl(p11,0)) as p3, sum(nvl(p12,0)) as p4, " +
                                                              "  sum(nvl(p13,0)) as p5, sum(nvl(p14,0)) as p6, sum(nvl(p15,0)) as p7, sum(nvl(p16,0)) as p8, sum(nvl(p17,0)) as p9, sum(nvl(p18,0)) as p10, " +
                                                              "  sum(nvl(p20,0)) as p11, sum(nvl(p19,0)) as p12, sum(nvl(p21,0)) as p21,sum(nvl(p22,0)) as p22,sum(nvl(p23,0)) as p23, sum(nvl(real_ch,0)) as p13, " +
                                                              " sum((nvl(sum_insaldo,0)+nvl(p9,0)+nvl(p10,0)+nvl(p11,0)+nvl(p12,0)+nvl(p13,0)+nvl(p14,0)+nvl(p15,0)+nvl(p16,0)+nvl(p17,0)+nvl(p18,0)+nvl(p19,0)+nvl(p20,0)+nvl(p21,0)+nvl(p22,0)+nvl(p23,0))) as p14, sum(nvl(oplata,0)) as oplata " +
                                                              " from " + table_name + " group by nzp_dom, ulica, ndom, nzp_geu ";
#endif
                            ret = ExecRead(conn_db, out reader, sql, true);
                            if (!ret.result) throw new Exception(ret.text);

                            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                            culture.NumberFormat.NumberDecimalSeparator = ".";
                            culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                            System.Threading.Thread.CurrentThread.CurrentCulture = culture;

                            table.Load(reader, LoadOption.OverwriteChanges);
                            break;
                            #endregion
                        }
                    case 2:
                        {
                            #region По участкам
                            table.Columns.Add("uch", typeof(string));
                            table.Columns.Add("in_saldo", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p1", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p2", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p3", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p4", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p5", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p6", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p7", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p8", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p9", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p10", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p11", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p12", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p21", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p22", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p23", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p14", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("p13", System.Type.GetType("System.Decimal"));
                            table.Columns.Add("oplata", System.Type.GetType("System.Decimal"));

#if PG
                            sql = "select nzp_geu as uch, sum(coalesce(sum_insaldo,0)) as in_saldo, sum(coalesce(p9,0)) as p1, sum(coalesce(p10,0)) as p2, sum(coalesce(p11,0)) as p3, sum(coalesce(p12,0)) as p4, sum(coalesce(p13,0)) as p5, " +
                                                            " sum(coalesce(p14,0)) as p6, sum(coalesce(p15,0)) as p7, sum(coalesce(p16,0)) as p8, sum(coalesce(p17,0)) as p9, sum(coalesce(p18,0)) as p10, sum(coalesce(p19,0)) as p11, " +
                                                            " sum(coalesce(p21,0)) as p21, sum(coalesce(p20,0)) as p12, sum(coalesce(real_ch,0)) as p13, " +
                                                            " sum((coalesce(sum_insaldo,0)+coalesce(p9,0)+coalesce(p10,0)+coalesce(p11,0)+coalesce(p12,0)+coalesce(p13,0)+coalesce(p14,0)+coalesce(p15,0)+coalesce(p16,0)+coalesce(p17,0)+coalesce(p18,0)+coalesce(p19,0)+coalesce(p20,0)+coalesce(p21,0)+coalesce(p22,0)+coalesce(p23,0)+coalesce(real_ch,0))) as p14, sum(coalesce(oplata,0)) as oplata  " +
                                                            " from " + table_name + " group by nzp_geu ";
#else
                            sql = "select nzp_geu as uch, sum(nvl(sum_insaldo,0)) as in_saldo, sum(nvl(p9,0)) as p1, sum(nvl(p10,0)) as p2, sum(nvl(p11,0)) as p3, sum(nvl(p12,0)) as p4, sum(nvl(p13,0)) as p5, " +
                                                            " sum(nvl(p14,0)) as p6, sum(nvl(p15,0)) as p7, sum(nvl(p16,0)) as p8, sum(nvl(p17,0)) as p9, sum(nvl(p18,0)) as p10, sum(nvl(p20,0)) as p11, " +
                                                            " sum(nvl(p19,0)) as p12, sum(nvl(p21,0)) as p21,sum(nvl(p22,0)) as p22,sum(nvl(p23,0)) as p23,  sum(nvl(real_ch,0)) as p13, " +
                                                            " sum((nvl(sum_insaldo,0)+nvl(p9,0)+nvl(p10,0)+nvl(p11,0)+nvl(p12,0)+nvl(p13,0)+nvl(p14,0)+nvl(p15,0)+nvl(p16,0)+nvl(p17,0)+nvl(p18,0)+nvl(p19,0)+nvl(p20,0)+nvl(p21,0)+nvl(p22,0)+nvl(p23,0))) as p14, sum(nvl(oplata,0)) as oplata  " +
                                                            " from " + table_name + " group by nzp_geu ";
#endif
                            ret = ExecRead(conn_db, out reader, sql, true);
                            if (!ret.result) throw new Exception(ret.text);

                            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                            culture.NumberFormat.NumberDecimalSeparator = ".";
                            culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                            System.Threading.Thread.CurrentThread.CurrentCulture = culture;

                            table.Load(reader, LoadOption.OverwriteChanges);
                            break;
                            #endregion
                        }
                }

                //процент загрузки
                dbRep.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 0.3M });

                MonitorLog.WriteLog("6. отчет готов.  ", MonitorLog.typelog.Info, 1, 2, true);
                sql = "drop table " + table_name + "";
                ret = ExecSQL(conn_db, sql, true);
                // if (!ret.result) throw new Exception(ret.text);
                conn_db.Close();
                reader.Close();
                #endregion

                #region Сохранить отчет в файл
                DataSet FDataSet = new DataSet();
                FDataSet.Tables.Add(table);

                FastReport.Report rep = new Report();
                string SourceReportFilename = null;
                switch (mode)
                {
                    case 0: SourceReportFilename = PathHelper.GetReportTemplatePath("Web_nachisl_ls.frx"); break;
                    case 1: SourceReportFilename = PathHelper.GetReportTemplatePath("Web_nachisl_dom.frx"); break;
                    case 2: SourceReportFilename = PathHelper.GetReportTemplatePath("Web_nachisl_uch.frx"); break;
                }

                rep.Load(SourceReportFilename);
                rep.RegisterData(FDataSet);
                rep.GetDataSource("Q_master").Enabled = true;
                string[] MonthStr = new string[] { "", "январь", "февраль", "март", "апрель", "май", "июнь", 
                                            "июль", "август", "сентябрь", "октябрь", "ноябрь", "декабрь" };
                rep.SetParameterValue("month", MonthStr[month_]);
                rep.SetParameterValue("year", year_.ToString());
                try
                {
                    //rep.Design();
                    rep.Prepare();
                    ret.text = "Отчет сформирован";
                    ret.tag = rep.Report.PreparedPages.Count;

                    destinationFilename = "";
                    switch (mode)
                    {
                        case 0: destinationFilename = Constants.ExcelDir + "nachisl_ls_" + month_.ToString() + "_" + year_.ToString() + ".xlsx"; break;
                        case 1: destinationFilename = Constants.ExcelDir + "nachisl_dom_" + month_.ToString() + "_" + year_.ToString() + ".xlsx"; break;
                        case 2: destinationFilename = Constants.ExcelDir + "nachisl_uch_" + month_.ToString() + "_" + year_.ToString() + ".xlsx"; break;

                    }
                    //rep.SavePrepared(destinationFilename.Replace("/","\\"));
                    MonitorLog.WriteLog("7.  ", MonitorLog.typelog.Info, 1, 2, true);
                    FastReport.Export.OoXML.Excel2007Export exl_export = new FastReport.Export.OoXML.Excel2007Export();
                    exl_export.ShowProgress = false;

                    //процент загрузки
                    dbRep.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 0.4M });

                    MonitorLog.WriteLog("8.  ", MonitorLog.typelog.Info, 1, 2, true);
                    destinationFilename = destinationFilename.Replace("/", "\\");
                    exl_export.Export(rep, destinationFilename);
                    //процент загрузки
                    dbRep.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 0.95M });

                    MonitorLog.WriteLog("9.  ", MonitorLog.typelog.Info, 1, 2, true);
                    if (InputOutput.useFtp)
                    {
                        destinationFilename = InputOutput.SaveOutputFile(destinationFilename);
                    }
                    else destinationFilename = System.IO.Path.GetFileName(destinationFilename);


                    MonitorLog.WriteLog("10.  ", MonitorLog.typelog.Info, 1, 2, true);
                    //exl_export.Export(rep, @"C:/1.pdf");
                    ret.result = true;
                    return;
                }
                catch (Exception ex)
                {
                    ret.text = "Отчет не сформирован";
                    ret.result = false;
                    MonitorLog.WriteLog("Ошибка формирования отчета " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                    return;
                }
                #endregion
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка в функции GetNachisl:\n" + (ex.Message != "" ? ex.Message : ret.text), MonitorLog.typelog.Error, true);
            }
            finally
            {
                dbRep.Close();
                MonitorLog.WriteLog("11. finally ", MonitorLog.typelog.Info, 1, 2, true);
                conn_db.Close();
                if (ret.result)
                {
                    ExcelRepClient dbRep2 = new ExcelRepClient();
                    dbRep2.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 1 });
                    dbRep2.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Success, exc_path = destinationFilename });
                    dbRep2.Close();
                }
                else
                {
                    ExcelRepClient dbRep2 = new ExcelRepClient();
                    dbRep2.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Failed, exc_path = destinationFilename });
                    dbRep2.Close();
                }

                MonitorLog.WriteLog("12. конец finally ", MonitorLog.typelog.Info, 1, 2, true);
            }

        }
        #endregion


        public void FindOverPayments(OverPayment finder, out Returns ret)
        {
            #region проверка параметров
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Пользователь не определен", -1);
                return;
            }

            if (finder.calc_month == "")
            {
                ret = new Returns(false, "Не задан расчетный месяц");
                return;
            }

            DateTime dt = DateTime.MinValue;
            if (!DateTime.TryParse(finder.calc_month, out dt))
            {
                ret = new Returns(false, "Не задан расчетный месяц");
                return;
            }
            #endregion

            string where_area = "", where_geu_from = "", where_geu_to = "", where_sostls = "", where_overpay = "",
                where_nzp_kvar_from = "", where_nzp_ul_from = "", where_nzp_dom_from = "";
            if (finder.nzp_area_from > 0) where_area = " and k.nzp_area = " + finder.nzp_area_from;
            if (finder.nzp_geu_from > 0) where_geu_from = " and k.nzp_geu = " + finder.nzp_geu_from;
            if (finder.nzp_geu_to > 0) where_geu_to = " and k.nzp_geu = " + finder.nzp_geu_to;
            if (finder.sost_ls > 0) where_sostls = " and k.is_open = '" + finder.sost_ls + "'";
            if (finder.nzp_kvar_from > 0) where_nzp_kvar_from = " and k.nzp_kvar = " + finder.nzp_kvar_from;
            if (finder.nzp_ul_from > 0) where_nzp_ul_from = " and u.nzp_ul = " + finder.nzp_ul_from;
            if (finder.nzp_dom_from > 0) where_nzp_dom_from = " and k.nzp_dom = " + finder.nzp_dom_from;

            if (finder.sum_overpayment == 0 && finder.sum_overpayment_po == 0) where_overpay = " sum(sum_outsaldo) < 0 ";
            else if (finder.sum_overpayment > 0 && finder.sum_overpayment_po == 0)
                where_overpay = " sum(sum_outsaldo) <= " + (finder.sum_overpayment * (-1)).ToString();
            else if (finder.sum_overpayment == 0 && finder.sum_overpayment_po > 0)
                where_overpay = " sum(sum_outsaldo) < 0 and sum(sum_outsaldo) >= " + (finder.sum_overpayment_po * (-1)).ToString();
            else if (finder.sum_overpayment > 0 && finder.sum_overpayment_po > 0)
                where_overpay = " sum(sum_outsaldo) >= " + (finder.sum_overpayment_po * (-1)).ToString() + " and sum(sum_outsaldo) <= " +
                                (finder.sum_overpayment * (-1)).ToString();

            #region соединение с БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return;
            #endregion

            IDataReader reader, reader2;

            string pref = "";
            string tprm1 = "", tcharge = "";
            DbTables tables = new DbTables(DBManager.getServer(conn_db));

            ExecSQL(conn_db, " Drop table t_overpayment ", false);
#if PG
            ret = ExecSQL(conn_db,
                             " Create unlogged table t_overpayment " +
                             " ( nzp_overpay SERIAL NOT NULL, " +
                             " nzp_kvar_from INTEGER, " +
                             " pref_from char(10), " +
                             " adr_from varchar(160), " +
                             " adr_to varchar(160), " +
                             " nzp_geu_from INTEGER, " +
                             " geu_from char(60), " +
                             " nzp_area_from INTEGER, " +
                             " area_from char(40), " +
                             " nzp_area_to INTEGER, " +
                             " area_to char(40), " +
                             " nzp_kvar_to INTEGER, " +
                             " pref_to char(10), " +
                             " nzp_geu_to INTEGER, " +
                             " geu_to char(60), " +
                             " num_ls INTEGER, " +
                             " num_ls_to INTEGER, " +
                             " pkod10 INTEGER, " +
                             " litera INTEGER, " +
                             " sum_overpay NUMERIC(14,2) " +
                             " )  "
                             , true);
#else
            ret = ExecSQL(conn_db,
                             " Create temp table t_overpayment " +
                             " ( nzp_overpay SERIAL NOT NULL, " +
                             " nzp_kvar_from INTEGER, " +
                             " pref_from char(10), " +
                             " adr_from varchar(160), " +
                             " adr_to varchar(160), " +
                             " nzp_geu_from INTEGER, " +
                             " geu_from char(60), " +
                             " nzp_area_from INTEGER, " +
                             " area_from char(40), " +
                             " nzp_area_to INTEGER, " +
                             " area_to char(40), " +
                             " nzp_kvar_to INTEGER, " +
                             " pref_to char(10), " +
                             " nzp_geu_to INTEGER, " +
                             " geu_to char(60), " +
                             " num_ls INTEGER, " +
                             " num_ls_to INTEGER, " +
                             " pkod10 INTEGER, " +
                             " litera INTEGER, " +
                             " sum_overpay DECIMAL(14,2) " +
                             " ) With no log "
                             , true);
#endif
            if (!ret.result)
            {
                conn_db.Close();
                ret.result = false;
                return;
            }

            #region определяем переплаты
#if PG
            string sql = "select distinct pref from " + tables.kvar + " k where 1=1 " + where_area + where_geu_from + where_sostls + where_nzp_dom_from + where_nzp_kvar_from;
#else
            string sql = "select unique pref from " + tables.kvar + " k where 1=1 " + where_area + where_geu_from + where_sostls + where_nzp_dom_from + where_nzp_kvar_from;
#endif
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
            while (reader.Read())
            {
                if (reader["pref"] != DBNull.Value) pref = ((string)reader["pref"]).Trim();
#if PG
                tcharge = pref + "_charge_" + dt.Year.ToString("0000").Substring(2, 2) + ".charge_" + dt.Month.ToString("00");
#else
                tcharge = pref + "_charge_" + dt.Year.ToString("0000").Substring(2, 2) + "@" + DBManager.getServer(conn_db) + ":charge_" + dt.Month.ToString("00");
#endif
#if PG
#warning
                sql = "insert into t_overpayment (nzp_kvar_from,num_ls,pkod10,pref_from,adr_from,nzp_area_from,area_from,nzp_geu_from,geu_from,sum_overpay)" +
                        " select k.nzp_kvar,k.num_ls, k.pkod10, k.pref, " +
                        " trim(coalesce(ulicareg,'улица'))||' '||trim(coalesce(u.ulica,''))||' / '||trim(coalesce(r.rajon,''))||'   дом '|| " +
                        " trim(coalesce(ndom,''))||'  корп. '|| trim(coalesce(nkor,''))||'  кв. '||trim(coalesce(nkvar,''))|| " +
                        " '  ком. '||trim(coalesce(nkvar_n,'')) as adr,  " +
                        " a.nzp_area, a.area,  " +
                        " g.nzp_geu, g.geu, " +
                        " sum(sum_outsaldo) sum_outsaldo " +
                        " FROM " + tables.kvar + " k " +
                        "LEFT OUTER JOIN " + tables.area + " a ON a.nzp_area = k.nzp_area " +
                        "LEFT OUTER JOIN " + tables.geu + " g ON g.nzp_geu = k.nzp_geu, " +
                        tcharge + " ch, " +
                        tables.dom + " d " +
                        "LEFT OUTER JOIN " + tables.rajon_dom + " rd ON d.nzp_raj = rd.nzp_raj_dom, " +
                        tables.ulica + " u " +
                        "LEFT OUTER JOIN " + tables.rajon + " r ON u.nzp_raj = r.nzp_raj " +
                        "LEFT OUTER JOIN " + tables.town + " twn ON twn.nzp_town = r.nzp_town " +
                        " where 1=1 " + where_area + where_geu_from + where_sostls + where_nzp_kvar_from + where_nzp_dom_from + where_nzp_ul_from +
                        " and ch.nzp_kvar = k.nzp_kvar and ch.dat_charge is null and ch.nzp_serv > 1 " +
                        " and k.nzp_dom=d.nzp_dom " +
                        " and d.nzp_ul=u.nzp_ul  " +
                        " group by 1,2,3,4,5,6,7,8,9  " +
                        " having " + where_overpay;
#else
                sql = "insert into t_overpayment (nzp_kvar_from,num_ls,pkod10,pref_from,adr_from,nzp_area_from,area_from,nzp_geu_from,geu_from,sum_overpay)" +
                                        " select k.nzp_kvar,k.num_ls, k.pkod10, k.pref, " +
                                        " trim(nvl(ulicareg,'улица'))||' '||trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,''))||'   дом '|| " +
                                        " trim(nvl(ndom,''))||'  корп. '|| trim(nvl(nkor,''))||'  кв. '||trim(nvl(nkvar,''))|| " +
                                        " '  ком. '||trim(nvl(nkvar_n,'')) as adr,  " +
                                        " a.nzp_area, a.area,  " +
                                        " g.nzp_geu, g.geu, " +
                                        " sum(sum_outsaldo) sum_outsaldo " +
                                        " from " + tables.kvar + " k, " + tcharge + " ch, " +
                                        tables.dom + " d, " + tables.ulica + " u, " +
                                        " outer ( " + tables.rajon + " r, outer  " + tables.town + " twn),  " +
                                        " outer " + tables.rajon_dom + " rd, outer " + tables.area + " a, " +
                                        " outer " + tables.geu + " g " +
                                        " where 1=1 " + where_area + where_geu_from + where_sostls + where_nzp_kvar_from + where_nzp_dom_from + where_nzp_ul_from +
                                        " and ch.nzp_kvar = k.nzp_kvar and ch.dat_charge is null and ch.nzp_serv > 1 " +
                                        " and k.nzp_dom=d.nzp_dom and twn.nzp_town=r.nzp_town  " +
                                        " and d.nzp_ul=u.nzp_ul and u.nzp_raj=r.nzp_raj and a.nzp_area=k.nzp_area  " +
                                        " and g.nzp_geu=k.nzp_geu and d.nzp_raj = rd.nzp_raj_dom " +
                                        " group by 1,2,3,4,5,6,7,8,9  " +
                                        " having " + where_overpay;
#endif
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    CloseReader(ref reader);
                    conn_db.Close();
                    return;
                }
            }
            reader.Close();
            #endregion;

            sql = "select * from t_overpayment";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return;
            }
            string from = tables.kvar + " k, " + tables.dom + " d, " + tables.ulica + " u,  " +
                                  " outer ( " + tables.rajon + " r, outer " + tables.town + " twn),  " +
                                  " outer " + tables.rajon_dom + " rd, outer " + tables.area + " a,  " +
                                  " outer " + tables.geu + " g";
            string where = " and k.nzp_dom=d.nzp_dom and twn.nzp_town=r.nzp_town " +
                           " and d.nzp_ul=u.nzp_ul and u.nzp_raj=r.nzp_raj and a.nzp_area=k.nzp_area  " +
                           " and  g.nzp_geu=k.nzp_geu and d.nzp_raj = rd.nzp_raj_dom ";
            string fields = " k.nzp_kvar,k.num_ls, k.pref, " +
                            " trim(nvl(ulicareg,'улица'))||' '||trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,''))||'   дом '||  " +
                            " trim(nvl(ndom,''))||'  корп. '|| trim(nvl(nkor,''))||'  кв. '||trim(nvl(nkvar,''))|| " +
                            " '  ком. '||trim(nvl(nkvar_n,'')) as adr,  " +
                            " a.nzp_area, a.area,  " +
                            " g.nzp_geu, g.geu";
            try
            {
                while (reader.Read())
                {
                    OverPayment op = new OverPayment();
                    if (reader["pref_from"] != DBNull.Value) op.pref = Convert.ToString(reader["pref_from"]);
                    if (reader["pkod10"] != DBNull.Value) op.pkod10 = Convert.ToInt32(reader["pkod10"]);
                    if (reader["nzp_kvar_from"] != DBNull.Value) op.nzp_kvar_from = Convert.ToInt32(reader["nzp_kvar_from"]);
#if PG
                    tprm1 = pref + "_data.prm_1";
#else
                    tprm1 = pref + "_data@" + DBManager.getServer(conn_db) + ":prm_1";
#endif
                    sql = "Select count(*) From " + from + " where k.pkod10 = " + op.pkod10 +
#if PG
 " and (select extract(year from p.dat_s)-2000 from " + tprm1 +
#else
                    " and (select year(p.dat_s)-2000 from " + tprm1 +
#endif
 " p where p.nzp_prm=2004 and p.nzp = " + op.nzp_kvar_from + ") = " +
#if PG
 " (select extract(year from p.dat_s)-2000 from " + tprm1 +
#else
                    " (select year(dat_s)-2000 from " + tprm1 +
#endif
 " where nzp_prm=2004 and nzp = k.nzp_kvar) " + where_geu_to +
                    " and k.is_open = " + Ls.States.Open.GetHashCode() + where;

                    object count = ExecScalar(conn_db, sql, out ret, true);
                    int recordsTotalCount;
                    try { recordsTotalCount = Convert.ToInt32(count); }
                    catch (Exception e)
                    {
                        ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                        MonitorLog.WriteLog("Ошибка GetOverPayments " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                        reader.Close();
                        conn_db.Close();
                        return;
                    }

                    if (recordsTotalCount == 1)
                    {
                        sql = "select " + fields +
#if PG
 ", (select extract(year from p.dat_s)-2000 from " + tprm1 +
#else
 ", (select year(dat_s)-2000 from " + tprm1 +
#endif
 " where nzp_prm=2004 and nzp = k.nzp_kvar) as litera from " + from + " where k.pkod10 = " + op.pkod10 +
#if PG
 " and (select extract(year from p.dat_s)-2000 from " + tprm1 +
                        " p where p.nzp_prm=2004 and p.nzp = " + op.nzp_kvar_from + ") = " +
                        " (select extract(year from p.dat_s)-2000 from " + tprm1 +
#else
 " and (select year(p.dat_s)-2000 from " + tprm1 +
                        " p where p.nzp_prm=2004 and p.nzp = " + op.nzp_kvar_from + ") = " +
                        " (select year(dat_s)-2000 from " + tprm1 +
#endif
 " where nzp_prm=2004 and nzp = k.nzp_kvar) " + where_geu_to +
                        " and k.is_open = " + Ls.States.Open.GetHashCode() + where;
                        ret = ExecRead(conn_db, out reader2, sql, true);
                        if (!ret.result)
                        {
                            reader.Close();
                            conn_db.Close();
                            return;
                        }

                        if (reader2.Read())
                        {
                            if (reader2["nzp_kvar"] != DBNull.Value) op.nzp_kvar_to = Convert.ToInt32(reader2["nzp_kvar"]);
                            if (reader2["num_ls"] != DBNull.Value) op.num_ls_to = Convert.ToInt32(reader2["num_ls"]);
                            if (reader2["pref"] != DBNull.Value) op.pref_to = Convert.ToString(reader2["pref"]);
                            if (reader2["geu"] != DBNull.Value) op.geu_to = Convert.ToString(reader2["geu"]);
                            if (reader2["nzp_geu"] != DBNull.Value) op.nzp_geu_to = Convert.ToInt32(reader2["nzp_geu"]);
                            if (reader2["adr"] != DBNull.Value) op.adr_to = Convert.ToString(reader2["adr"]);
                            if (reader2["litera"] != DBNull.Value) op.litera = Convert.ToInt32(reader2["litera"]);

                            sql = " update t_overpayment set nzp_kvar_to = " + op.nzp_kvar_to + ", pref_to = " + Utils.EStrNull(op.pref_to) + ", " +
                                  " geu_to = " + Utils.EStrNull(op.geu_to) + ", nzp_geu_to = " + op.nzp_geu_to + ", litera = " + op.litera + ", " +
                                  " adr_to = " + Utils.EStrNull(op.adr_to) + ", num_ls_to = " + op.num_ls_to + " where nzp_kvar_from = " + op.nzp_kvar_from +
                                  " and pref_from = " + Utils.EStrNull(op.pref);
                            ret = ExecSQL(conn_db, sql, true);
                            if (!ret.result)
                            {
                                CloseReader(ref reader);
                                CloseReader(ref reader2);
                                conn_db.Close();
                                return;
                            }
                        }
                        reader2.Close();
                    }

                }
                reader.Close();
            }
            catch (Exception ex)
            {
                CloseReader(ref reader);
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка получения информации", MonitorLog.typelog.Error, 20, 201, true);
                return;
            }

            #region соединение conn_web
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;
            #endregion

            string tXX_overpayment = "t" + Convert.ToString(finder.nzp_user) + "_overpayment";
#if PG
            string tXX_overpayment_full = pgDefaultDb + "." + tXX_overpayment;
#else
            string tXX_overpayment_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_overpayment;
#endif

            #region создать кэш-таблицу
            ret = CreateTableWebOverPayment(conn_web, tXX_overpayment, true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }
            #endregion

            #region запись данных в кэш таблицу
            sql = " Insert into " + tXX_overpayment_full +
                    " ( nzp_overpay,nzp_kvar_from,pref_from,adr_from,adr_to,nzp_geu_from,geu_from,nzp_area_from,area_from,nzp_area_to, " +
                      " area_to,nzp_kvar_to,pref_to,nzp_geu_to,geu_to,num_ls,pkod10,num_ls_to,sum_overpay,litera,mark) " +
                " Select nzp_overpay,nzp_kvar_from,pref_from,adr_from,adr_to,nzp_geu_from,geu_from,nzp_area_from,area_from,nzp_area_to, " +
                       " area_to,nzp_kvar_to,pref_to,nzp_geu_to,geu_to,num_ls,pkod10,num_ls_to,sum_overpay,litera,1 " +
                " From t_overpayment";
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }
            #endregion

            #region создаем индексы на tXX_spls
            CreateTableWebOverPayment(conn_web, tXX_overpayment, false);
            #endregion
        }

        public List<OverPayment> GetOverPayments(OverPayment finder, out Returns ret)
        {
            #region соединение conn_web
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;
            #endregion

            string tXX_overpayment = "t" + Convert.ToString(finder.nzp_user) + "_overpayment";
#if PG
            string tXX_overpayment_full = "public." + tXX_overpayment;
#else
            string tXX_overpayment_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_overpayment;
#endif
            string sql = "Select count(*) From " + tXX_overpayment_full;
            object cnt = ExecScalar(conn_web, sql, out ret, true);
            int recordsTotCount;
            try { recordsTotCount = Convert.ToInt32(cnt); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetOverPayments " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                conn_web.Close();
                return null;
            }

            IDataReader reader;
            List<OverPayment> list = new List<OverPayment>();

            sql = "Select * From " + tXX_overpayment_full;
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    OverPayment op = new OverPayment();
                    if (reader["nzp_overpay"] != DBNull.Value) op.nzp_overpay = Convert.ToInt32(reader["nzp_overpay"]);
                    if (reader["pref_from"] != DBNull.Value) op.pref = Convert.ToString(reader["pref_from"]);
                    if (reader["pref_to"] != DBNull.Value) op.pref_to = Convert.ToString(reader["pref_to"]);
                    if (reader["num_ls"] != DBNull.Value) op.num_ls = Convert.ToInt32(reader["num_ls"]);
                    if (reader["num_ls_to"] != DBNull.Value) op.num_ls_to = Convert.ToInt32(reader["num_ls_to"]);
                    if (reader["pkod10"] != DBNull.Value) op.pkod10 = Convert.ToInt32(reader["pkod10"]);
                    if (reader["nzp_kvar_from"] != DBNull.Value) op.nzp_kvar_from = Convert.ToInt32(reader["nzp_kvar_from"]);
                    if (reader["nzp_kvar_to"] != DBNull.Value) op.nzp_kvar_to = Convert.ToInt32(reader["nzp_kvar_to"]);
                    if (reader["sum_overpay"] != DBNull.Value) op.sum_overpayment = Convert.ToDecimal(reader["sum_overpay"]) * (-1);
                    if (reader["geu_from"] != DBNull.Value) op.geu_from = Convert.ToString(reader["geu_from"]);
                    if (reader["nzp_geu_from"] != DBNull.Value) op.nzp_geu_from = Convert.ToInt32(reader["nzp_geu_from"]);
                    if (reader["adr_from"] != DBNull.Value) op.adr_from = Convert.ToString(reader["adr_from"]);
                    if (reader["geu_to"] != DBNull.Value) op.geu_to = Convert.ToString(reader["geu_to"]);
                    if (reader["nzp_geu_to"] != DBNull.Value) op.nzp_geu_to = Convert.ToInt32(reader["nzp_geu_to"]);
                    if (reader["adr_to"] != DBNull.Value) op.adr_to = Convert.ToString(reader["adr_to"]);
                    if (reader["mark"] != DBNull.Value) op.mark = Convert.ToInt32(reader["mark"]);
                    if (reader["litera"] != DBNull.Value) op.litera = Convert.ToInt32(reader["litera"]);

                    list.Add(op);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }
            }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при получении записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetOverPayments " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                CloseReader(ref reader);
                conn_web.Close();
                return null;
            }

            sql = "Select sum(sum_overpay) as sum_overpay From " + tXX_overpayment_full;
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }
            if (reader.Read())
            {
                OverPayment opay = new OverPayment();
                if (reader["sum_overpay"] != DBNull.Value) opay.sum_overpayment = Convert.ToDecimal(reader["sum_overpay"]) * (-1);
                list.Add(opay);
            }

            ret.tag = recordsTotCount;
            conn_web.Close();
            return list;
        }

        public Returns SaveAddressToForOverPay(OverPayment finder)
        {
            if (finder.nzp_overpay <= 0) return new Returns(false, "Не выбрана переплата", -1);
            if (finder.nzp_user <= 0) return new Returns(false, "Не указан пользователь", -1);
            if (finder.nzp_kvar_to <= 0) return new Returns(false, "Не указан лицевой счет", -1);
            if (finder.pref_to == "") return new Returns(false, "Не указан префикс лицевого счета", -1);

            Returns ret;
            string sql;

            #region соединение с БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            IDataReader reader;
            string fields = " k.num_ls, " +
                            " trim(nvl(ulicareg,'улица'))||' '||trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,''))||'   дом '||  " +
                            " trim(nvl(ndom,''))||'  корп. '|| trim(nvl(nkor,''))||'  кв. '||trim(nvl(nkvar,''))|| " +
                            " '  ком. '||trim(nvl(nkvar_n,'')) as adr,  " +
                            " g.nzp_geu, g.geu";
            //TODO исправить для postgresql
            string from = finder.pref_to + "_data@" + DBManager.getServer(conn_db) + ":kvar k, " +
                          finder.pref_to + "_data@" + DBManager.getServer(conn_db) + ":dom d, " +
                          finder.pref_to + "_data@" + DBManager.getServer(conn_db) + ":s_ulica u,  " +
                          " outer ( " + finder.pref_to + "_data@" + DBManager.getServer(conn_db) + ":s_rajon r, outer " +
                          finder.pref_to + "_data@" + DBManager.getServer(conn_db) + ":s_town twn),  " +
                          " outer " + finder.pref_to + "_data@" + DBManager.getServer(conn_db) + ":s_rajon_dom rd, " +
                          " outer " + finder.pref_to + "_data@" + DBManager.getServer(conn_db) + ":s_geu g";
            string where = " and k.nzp_dom=d.nzp_dom and twn.nzp_town=r.nzp_town " +
                           " and d.nzp_ul=u.nzp_ul and u.nzp_raj=r.nzp_raj  " +
                           " and  g.nzp_geu=k.nzp_geu and d.nzp_raj = rd.nzp_raj_dom ";
#if PG
            string tprm1 = finder.pref_to + "_data.prm_1";
#else
            string tprm1 = finder.pref_to + "_data@" + DBManager.getServer(conn_db) + ":prm_1";
#endif

            sql = "select " + fields +
#if PG
 ", (select extract(year from dat_s)-2000 from " + tprm1 +
#else
 ", (select year(dat_s)-2000 from " + tprm1 +
#endif
 " where nzp_prm=2004 and nzp = k.nzp_kvar) as litera from " +
                from + " where k.nzp_kvar = " + finder.nzp_kvar_to + where;

            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            if (reader.Read())
            {
                if (reader["num_ls"] != DBNull.Value) finder.num_ls_to = Convert.ToInt32(reader["num_ls"]);
                if (reader["geu"] != DBNull.Value) finder.geu_to = Convert.ToString(reader["geu"]);
                if (reader["nzp_geu"] != DBNull.Value) finder.nzp_geu_to = Convert.ToInt32(reader["nzp_geu"]);
                if (reader["adr"] != DBNull.Value) finder.adr_to = Convert.ToString(reader["adr"]);
                if (reader["litera"] != DBNull.Value) finder.litera = Convert.ToInt32(reader["litera"]);
            }

            reader.Close();
            conn_db.Close();


            #region соединение conn_web
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;
            #endregion

            string tXX_overpayment = "t" + Convert.ToString(finder.nzp_user) + "_overpayment";
#if PG
            string tXX_overpayment_full = conn_web.Database + "." + tXX_overpayment;
#else
            string tXX_overpayment_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_overpayment;
#endif
            sql = " update " + tXX_overpayment_full + " set nzp_kvar_to = " + finder.nzp_kvar_to + ", pref_to = " + Utils.EStrNull(finder.pref_to) + ", " +
                                  " geu_to = " + Utils.EStrNull(finder.geu_to) + ", nzp_geu_to = " + finder.nzp_geu_to + ", litera = " + finder.litera + ", " +
                                  " adr_to = " + Utils.EStrNull(finder.adr_to) + ", num_ls_to = " + finder.num_ls_to +
                                  " where nzp_overpay = " + finder.nzp_overpay;
            ret = ExecSQL(conn_web, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }
            conn_web.Close();
            return ret;
        }


        //удалил ф-ию : private Returns CheckCalc(IDbConnection conn_db, ParamCalcP paramcalc)
        //нельзя ее восстанавливать. причина: удаляет группы из s_group, это неверно!
        //и результат проверки никак не использовался :/

        private struct serv_check
        {
            public int nzp_serv { get; set; }
            public string serv_message { get; set; }
            public int nzp_prm { get; set; }
            public int iCheckGroup { get; set; }
        }


        #region Изменение графиков рассрочки
        private ReturnsType ChangeKreditXX(IDbConnection conn_db, ParamCalcP paramcalc)
        {
            using (DbKreditPay db = new DbKreditPay())
            {
                //в функцию рассчета рассрочки необходимо передать тот месяц, на который переведут банк
                var dt = new DateTime(paramcalc.calc_yy, paramcalc.calc_mm, 1).AddMonths(1);
                return db.UpdateKredit(conn_db, paramcalc.pref, dt.Year, dt.Month);
            }
        }

        #endregion




        /// <summary>
        /// возвращает список выбранных банков данных в таблице txx_spdom или txx_spls
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<_Point> getListCalculatePoints(Finder finder, out Returns ret)
        {
            var res = new List<_Point>();
            IDbConnection conn_web = DBManager.GetConnection(Constants.cons_Webdata);
            ret = DBManager.OpenDb(conn_web, true);
            if (!ret.result)
            {
                return res;
            }
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return res;
            }
            try
            {
                //получаем список банков
                var txx_spdom = DBManager.sDefaultSchema + "t" + finder.nzp_user + "_spdom";
                var sql = "SELECT nzp_wp, pref FROM " + txx_spdom + " WHERE mark=1 GROUP BY 1,2";
                var DT = ClassDBUtils.OpenSQL(sql, conn_web).resultData;

                if (DT.Rows.Count == 0)
                {
                    ret.result = false;
                    ret.text = "Выберите дома для расчета!";
                    return res;
                }

                foreach (DataRow row in DT.Rows)
                {
                    var point = new _Point();
                    point.nzp_wp = CastValue<int>(row["nzp_wp"]);
                    point.pref = CastValue<string>(row["pref"]).Trim();
                    res.Add(point);
                }
            }
            catch
            {
                ret.result = false;
                ret.text = "Ошибка определения списка домов для расчета";
                return res;
            }
            finally
            {
                conn_web.Close();
            }
            return res;
        }

        /// <summary>
        /// Запись списка домов для расчета по ним
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="nzpTask"></param>
        /// <returns></returns>
        public Returns InsertListHousesForCalc(Dom finder, int nzpTask)
        {
            var ret = Utils.InitReturns();
            var sql = "";
            var baseName = "";
#if PG
            baseName = "public";
#else 
            baseName = conn_web.Database + "@" + DBManager.getServer(conn_web);
#endif
            using (var conn_web = DBManager.GetConnection(Constants.cons_Webdata))
            {
                try
                {
                    ret = DBManager.OpenDb(conn_web, true);
                    if (!ret.result)
                    {
                        return ret;
                    }
                    if (finder.nzp_user <= 0)
                    {
                        ret.result = false;
                        ret.text = "Не определен пользователь";
                        return ret;
                    }


                    var tableName = baseName + tableDelimiter + "list_houses_for_calc";
                    var txx_spdom = baseName + tableDelimiter + "t" + finder.nzp_user + "_spdom";

                    if (!DBManager.TableInBase(conn_web, null, pgDefaultDb, "list_houses_for_calc"))
                    {
                        sql = "CREATE TABLE " + tableName + " (" +
                              "nzp_wp integer, nzp_dom integer,nzp_user integer,nzp_key integer)";
                        ret = ExecSQL(conn_web, sql);
                        if (!ret.result)
                        {
                            ret.text = "Ошибка создания таблицы для списка расчитываемых домов";
                            return ret;
                        }
                    }

                    sql = " DELETE FROM " + tableName + " WHERE nzp_wp=" + finder.nzp_wp + " and nzp_key in " +
                          " (SELECT c.nzp_key FROM " + baseName + tableDelimiter + "calc_fon_" +
                          Points.GetCalcNum(finder.nzp_wp) + " c," + tableName + " l" +
                          " WHERE c.nzp_key=l.nzp_key and c.kod_info in (" + (int)FonTask.Statuses.Completed + "," +
                          (int)FonTask.Statuses.Failed + " ) )";
                    ret = ExecSQL(conn_web, sql);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка удаления старых записей";
                        return ret;
                    }

                    sql = " INSERT INTO " + tableName + " (nzp_wp,nzp_dom,nzp_user,nzp_key)  " +
                          " SELECT " + finder.nzp_wp + ", nzp_dom, " + finder.nzp_user + "," + nzpTask + " FROM " +
                          " " + txx_spdom + " WHERE mark=1  and nzp_wp=" + finder.nzp_wp;
                    ret = ExecSQL(conn_web, sql);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка получения списка домов для расчета";
                        return ret;
                    }
                }
                finally
                {
                    sql = " UPDATE " + baseName + tableDelimiter + "calc_fon_" + Points.GetCalcNum(finder.nzp_wp) +
                          " SET kod_info = " + (ret.result
                              ? (int)FonTask.Statuses.InQueue
                              : (int)FonTask.Statuses.Failed) +
                          " WHERE nzp_key =" + nzpTask;
                    ret = ExecSQL(conn_web, sql);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка InsertListHousesForCalc " + ret.text, MonitorLog.typelog.Error, 30, 301, true);
                    }
                }
            }
            return ret;
        }
        public List<Prov> GetListProvs(ProvFinder finder, out Returns ret)
        {
            var res = new List<Prov>();
            using (IDbConnection conn_db = DBManager.GetConnection(Points.GetConnByPref(Points.Pref)))
            {
                ret = DBManager.OpenDb(conn_db, true);
                if (!ret.result)
                {
                    return res;
                }

                if (finder.nzp_user <= 0)
                {
                    ret.result = false;
                    ret.text = "Не определен пользователь";
                    return res;
                }
                if (finder.nzp_kvar <= 0)
                {
                    ret.result = false;
                    ret.text = "Не определен номер лицевого счета";
                    return res;
                }
                if (finder.nzp_wp <= 0)
                {
                    ret.result = false;
                    ret.text = "Не определен локальный банк для лицевого счета";
                    return res;
                }
                if (finder.date_obligation == DateTime.MinValue)
                {
                    ret.result = false;
                    ret.text = "Не период получения проводок";
                    return res;
                }


                #region Условия
                string limit = "";
                string offset = "";
                var where = " AND p.nzp_kvar=" + finder.nzp_kvar;
                if (finder.skip != 0)
                {
                    offset = " OFFSET " + finder.skip;
                }
                if (finder.rows != 0)
                {
                    limit = " LIMIT " + finder.rows;
                }
                if (finder.date_prov_from > DateTime.MinValue)
                {
                    where += " AND p.date_prov>=" + Utils.EStrNull(finder.date_prov_from.ToShortDateString());
                }
                if (finder.date_prov_to < DateTime.MaxValue)
                {
                    where += " AND p.date_prov<=" + Utils.EStrNull(finder.date_prov_to.ToShortDateString());
                }
                if (finder.nzp_serv > 0)
                {
                    where += " AND p.nzp_serv=" + finder.nzp_serv;
                }
                if (finder.nzp_supp > 0)
                {
                    where += " AND p.nzp_supp=" + finder.nzp_serv;
                }
                if (finder.type > -1)
                {
                    where += " AND p.s_prov_types_id=" + finder.type;
                }
                if (finder.nzp_wp > 0)
                {
                    where += " AND p.nzp_wp=" + finder.nzp_wp;
                }
                DateTime dateStartPeni;
                using (var db  = new DbAdmin())
                {
                    dateStartPeni = db.GetDateStartPeni(conn_db, new _Point() { pref = finder.pref }, out ret);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка получение параметра \"Дата начала расчета пени\"";
                        return res;
                    }
                }
             
                //показ проводок за весь год
                if (finder.all_year)
                {
                    where += " AND p.date_obligation BETWEEN " +
                             Utils.EStrNull(new DateTime(finder.date_obligation.Year, 1, 1).ToShortDateString()) + " AND " +
                             Utils.EStrNull(new DateTime(finder.date_obligation.Year, 12, 31).ToShortDateString());
                }
                if (dateStartPeni > DateTime.MinValue)
                {
                    where += " AND p.date_obligation>=" + Utils.EStrNull(dateStartPeni.ToShortDateString());
                }
                #endregion Условия

                var peni_prov_table = "";
                if (finder.all_year)
                {
                    peni_prov_table = finder.pref + sDataAliasRest + "peni_provodki ";
                }
                else
                {
                    var schema = finder.pref + "_charge_" + (finder.date_obligation.Year - 2000).ToString("00");
                    var table = "peni_provodki_" + finder.date_obligation.Year +
                                         finder.date_obligation.Month.ToString("00") + "_" + finder.nzp_wp;
                    peni_prov_table = schema + tableDelimiter + table;

                    if (!TableInBase(conn_db, null, schema, table))
                    {
                        return res;
                    }

                }


                var sql =
                    " SELECT p.*, s.service,st.type_prov,sp.name_supp as supplier, trim(u.name)||'('||trim(u.comment)||')' as username" +
                    " FROM  " + peni_prov_table + " p " +
                    " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest + "s_prov_types st ON st.id=p.s_prov_types_id" +
                    " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest + "services s ON p.nzp_serv=s.nzp_serv" +
                    " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest + "supplier sp ON p.nzp_supp=sp.nzp_supp" +
                    " LEFT OUTER JOIN " + Points.Pref + sDataAliasRest + "users u ON p.created_by=u.nzp_user" +
                    " WHERE 1=1 AND p.s_prov_types_id>0 " + where +
                    " ORDER BY p.date_obligation,p.nzp_supp,p.nzp_serv ";
                var DT = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                ret.tag = DT.Rows.Count;
                var list = DT.AsEnumerable().ToList();
                int num = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    var row = new Prov
                    {
                        num = ++num,
                        id = CastValue<int>(list[i]["id"]),
                        nzp_kvar = CastValue<int>(list[i]["nzp_kvar"]),
                        num_ls = CastValue<int>(list[i]["num_ls"]),
                        nzp_dom = CastValue<int>(list[i]["nzp_dom"]),
                        nzp_serv = CastValue<int>(list[i]["nzp_serv"]),
                        nzp_supp = CastValue<int>(list[i]["nzp_supp"]),
                        nzp_wp = CastValue<int>(list[i]["nzp_wp"]),
                        type = CastValue<int>(list[i]["s_prov_types_id"]),
                        nzp_source = CastValue<int>(list[i]["nzp_source"]),
                        rsum_tarif = CastValue<decimal>(list[i]["rsum_tarif"]),
                        sum_prih = CastValue<decimal>(list[i]["sum_prih"]),
                        sum_nedop = CastValue<decimal>(list[i]["sum_nedop"]),
                        sum_reval = CastValue<decimal>(list[i]["sum_reval"]),
                        date_prov = CastValue<DateTime>(list[i]["date_prov"]),
                        date_obligation = CastValue<DateTime>(list[i]["date_obligation"]),
                        created_on = CastValue<DateTime>(list[i]["created_on"]),
                        created_by = CastValue<int>(list[i]["created_by"]),
                        peni_actions_id = CastValue<int>(list[i]["peni_actions_id"]),
                        service = CastValue<string>(list[i]["service"]).Trim(),
                        supplier = CastValue<string>(list[i]["supplier"]).Trim(),
                        username = CastValue<string>(list[i]["username"]).Trim(),
                        type_prov = CastValue<string>(list[i]["type_prov"]).Trim()
                    };
                    res.Add(row);
                }
            }
            return res;
        }

        public Dictionary<int, string> GetTypesProvs(out Returns ret)
        {
            var res = new Dictionary<int, string>();
            using (IDbConnection conn_db = DBManager.GetConnection(Points.GetConnByPref(Points.Pref)))
            {
                ret = DBManager.OpenDb(conn_db, true);
                if (!ret.result)
                {
                    return res;
                }
                var sql = "SELECT id, type_prov FROM " + Points.Pref + sKernelAliasRest + "s_prov_types";
                var DT = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                for (int i = 0; i < DT.Rows.Count; i++)
                {
                    res[CastValue<int>(DT.Rows[i]["id"])] = CastValue<string>(DT.Rows[i]["type_prov"]).Trim();
                }
            }
            return res;
        }



        public List<PrmTypes> LoadUsersPercentDom(Finder finder, out Returns ret)
        {
            var res = new List<PrmTypes>();
            using (IDbConnection conn_db = DBManager.GetConnection(Points.GetConnByPref(Points.Pref)))
            {
                ret = DBManager.OpenDb(conn_db, true);
                if (!ret.result)
                {
                    return res;
                }
                var sql =
                    " SELECT DISTINCT u.name, u.nzp_user" +
                    " FROM " + Points.Pref + sDataAliasRest + "fn_percent_dom_log f," +
                    Points.Pref + sDataAliasRest + "users u" +
                    " WHERE u.nzp_user = f.changed_by" +
                    " ORDER BY u.name";
                var DT = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                for (int i = 0; i < DT.Rows.Count; i++)
                {
                    res.Add(new PrmTypes { type_name = CastValue<string>(DT.Rows[i]["name"]), id = CastValue<int>(DT.Rows[i]["nzp_user"]) });
                }
            }
            return res;
        }


        public List<PrmTypes> LoadOperTypesPercentDom(Finder finder, out Returns ret)
        {
            var res = new List<PrmTypes>();
            using (IDbConnection conn_db = DBManager.GetConnection(Points.GetConnByPref(Points.Pref)))
            {
                ret = DBManager.OpenDb(conn_db, true);
                if (!ret.result)
                {
                    return res;
                }
                var sql =
                    " SELECT DISTINCT nzp_data_operation, data_operation" +
                    " FROM " + Points.Pref + sDataAliasRest + "s_data_operation" +
                    " ORDER BY data_operation";
                var DT = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                for (int i = 0; i < DT.Rows.Count; i++)
                {
                    res.Add(new PrmTypes { type_name = CastValue<string>(DT.Rows[i]["data_operation"]), id = CastValue<int>(DT.Rows[i]["nzp_data_operation"]) });
                }
            }
            return res;
        }

    }
}

