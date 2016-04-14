using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using System.Data;

namespace STCLINE.KP50.DataBase
{
    public partial class Debitor : DataBaseHead
    {
        /// <summary>
        /// Функция проверяет возможность совершения групповой операции над списком выбранных дел
        /// </summary>
        /// <param name="finder">finder.nzp_user = пользователь</param>
        /// <returns></returns>
        public Returns CheckDealsBeforeGroupOperation(Deal finder, int nzp_oper)
        {
            Returns ret = Utils.InitReturns();

            // проверка текущего пользователя
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Не определен пользователь!";
                return ret;
            }

            // проверка доступа к БД
            var conn = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn, true);
            if (!ret.result)
            {
                return ret;
            }

            // проверка существования найденных дел
            ExecSQL(conn, "SET search_path TO public;", true);
            if (!TableInWebCashe(conn, "t" + finder.nzp_user + "_debt"))
            {
                conn.Close();
                ret.result = false;
                ret.text = "Данные не были выбраны! Выполните поиск.";
                return ret;
            }

            // поиск количества дел над которыми идет операция
            #region SQL find count
            StringBuilder sql = new StringBuilder();
            sql.Append(" SELECT COUNT(*) ");
            sql.Append(" FROM " + Points.Pref + "_debt.deal ");
            sql.Append(" WHERE nzp_deal IN (");
            sql.Append(" SELECT nzp_deal ");
            sql.Append(" FROM " + Points.Pref + "_debt.groups_operations_details ");
            sql.Append(" WHERE nzp_group IN ( ");
            sql.Append(" SELECT nzp_group ");
            sql.Append(" FROM " + Points.Pref + "_debt.groups_operations ");
            sql.Append(" WHERE nzp_status IN (" + s_opers_statuses.Ready.GetHashCode() + "," + s_opers_statuses.InProcess.GetHashCode() + ")");
            sql.Append("))");
            #endregion
            var count = ExecScalar(conn, sql.ToString(), out ret, true);

            if (!ret.result)
            {
                conn.Close();
                return ret;
            }


            try
            {
                if (Convert.ToInt32(count) > 0)
                {
                    conn.Close();
                    ret.result = false;
                    ret.text = "В списке выбранных дел присутствуют дела, над котороыми в данный момент совершается операция. Попробуйте повторить операцию позднее или выполните новый поиск.";
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Ошибка получения количества выбранных дел в операции", ex);
                conn.Close();
                ret.result = false;
                ret.text = " Ошибка получения количества выбранных дел в операции.";
                return ret;
            }

            // есть дела, готовые для групповой операции
            ret.result = true;
            return ret;
        }

        /// <summary>
        /// Добавление групповой операции
        /// </summary>
        /// <param name="finder">finder.nzp_user = пользователь</param>
        /// <param name="nzp_oper">код операции</param>
        /// <param name="type">тип операции</param>
        /// <returns></returns>
        public int AddGroupOperation(Deal finder, int nzp_oper, ReportType type, out Returns ret)
        {
            ret = Utils.InitReturns();

            // Проверка возможности создание групповой операции
            ret = CheckDealsBeforeGroupOperation(finder, nzp_oper);
            if (!ret.result)
            {
                return 0;
            }

            // проверка доступа к БД
            var conn = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn, true);
            if (!ret.result)
            {
                return 0;
            }

            // добавление новой операции
            #region SQL insert group
            StringBuilder sql = new StringBuilder();
            sql.Append(" INSERT INTO ");
            sql.Append(Points.Pref + "_debt.groups_operations ( ");
            sql.Append(" nzp_status, oper_date, nzp_user, format, nzp_oper ");
            sql.Append(" ) VALUES ( ");
            sql.Append(s_opers_statuses.Ready.GetHashCode() + ", ");
            sql.Append(" now(), ");
            sql.Append(finder.nzp_user + ", ");
            sql.Append(type.GetHashCode() + ", ");
            sql.Append(nzp_oper);
            sql.Append(" ) RETURNING nzp_group ");
            #endregion
            var nzp_group = ExecScalar(conn, sql.ToString(), out ret, true);
            if (!ret.result)
            {
                conn.Close();
                return 0;
            }

            // добавление деталей операции
            #region INSERT group details
            sql.Remove(0, sql.Length);
            sql.Append(" INSERT INTO ");
            sql.Append(Points.Pref + "_debt.groups_operations_details (nzp_group, nzp_deal) ");
            sql.Append(" SELECT " + Convert.ToString(nzp_group) + ", nzp_deal ");
            sql.Append(" FROM " + Points.Pref + "_debt.deal ");
            sql.Append(" WHERE nzp_kvar IN ");
            sql.Append(" (SELECT DISTINCT nzp_kvar FROM public.t" + finder.nzp_user + "_debt) ");
            #endregion

            ret = ExecSQL(conn, sql.ToString(), true);
            if (!ret.result)
            {
                // изменение состояния операции при неуспешном добавлении
                ChangeGroupOperationStatus(Convert.ToInt32(nzp_group), s_opers_statuses.Error);
                conn.Close();
                return 0;
            }
            conn.Close();

            return Convert.ToInt32(nzp_group);
        }

        /// <summary>
        /// Изменение статуса группвой операции
        /// </summary>
        /// <param name="nzp_group">номер операции</param>
        /// <param name="status">новый статус операции</param>
        /// <param name="conn"></param>
        /// <returns></returns>
        public Returns ChangeGroupOperationStatus(int nzp_group, s_opers_statuses status)
        {
            Returns ret = Utils.InitReturns();
            // проверка доступа к БД
            var conn = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn, true);
            if (!ret.result)
            {
                return ret;
            }
            #region UPDATE group status
            StringBuilder sql = new StringBuilder();
            sql.Append(" UPDATE " + Points.Pref + "_debt.groups_operations ");
            sql.Append(" SET nzp_status = " + status.GetHashCode());
            sql.Append(" WHERE nzp_group = " + nzp_group);
            #endregion
            ret = ExecSQL(conn, sql.ToString(), true);
            conn.Close();
            return ret;
        }

        /// <summary>
        /// Возвращает список поставщиков, по которым есть долги
        /// </summary>
        /// <param name="finder">nzp</param>
        /// <returns></returns>
        public List<Supplier> GetListSupplier(Deal finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            #region Проверки
            // проверка текущего пользователя
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Не определен пользователь!";
                return null;
            }

            // проверка текущего дела
            if (finder.nzp_deal <= 0)
            {
                ret.result = false;
                ret.text = "Не определено дело!";
                return null;
            }

            // проверка доступа к БД
            var conn = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn, true);
            if (!ret.result)
            {
                return null;
            }
            #endregion

            // получение nzp_kvar и pref по nzp_deal
            var kvarPref = ClassDBUtils.OpenSQL("SELECT nzp_kvar, pref FROM " + Points.Pref + "_debt.deal WHERE nzp_deal = " + finder.nzp_deal, conn);

            int nzp_kvar = 0;
            string pref = "";

            if ((kvarPref.resultCode != 0) || (kvarPref.resultData.Rows.Count == 0))
            {
                ret.result = false;
                ret.text = "Не определено дело!" + kvarPref.resultMessage;
                return null;
            }

            try
            {
                nzp_kvar = Convert.ToInt32(kvarPref.resultData.Rows[0]["nzp_kvar"]);
                pref = Convert.ToString(kvarPref.resultData.Rows[0]["pref"]);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка преобразования данных: " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = "Ошибка преобразования данных!";
                return null;
            }

            #region SQL select suppliers
            DateTime calcdate = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1);
            string curr_charge = pref + "_charge_" + (Points.CalcMonth.year_ % 100).ToString("00") + ".charge_" + Points.CalcMonth.month_.ToString("00");
            string prev_charge = pref + "_charge_" + (Points.CalcMonth.month_ > 1
                ? Points.CalcMonth.year_ % 100
                : (Points.CalcMonth.year_ - 1) % 100
                ).ToString("00") + ".charge_" +
                (Points.CalcMonth.month_ > 1
                ? Points.CalcMonth.month_ - 1
                : 12
                ).ToString("00");
            StringBuilder sql = new StringBuilder();
            sql.Append(" SELECT DISTINCT ");
            sql.Append(" c1.nzp_supp, ");
            sql.Append(" s.name_supp ");
            sql.Append(" FROM ");
            sql.Append(prev_charge + " c1 ");
            sql.Append(" LEFT JOIN " + curr_charge + " AS c2 ON c1.nzp_kvar = c2.nzp_kvar ");
            sql.Append(" AND c1.nzp_serv = c2.nzp_serv ");
            sql.Append(" AND c1.nzp_supp = c2.nzp_supp ");
            sql.Append(" LEFT JOIN (SELECT nzp_kvar, nzp_serv, nzp_supp, SUM(sum_rcl) as sum_rcl ");
            sql.Append(" FROM " + pref + "_charge_" + (Points.CalcMonth.year_ % 100).ToString("00") + ".perekidka ");
            sql.Append(" WHERE date_rcl >= '" + calcdate.ToString("yyyy-MM-dd") + "' AND date_rcl < '" + calcdate.AddMonths(1).ToString("yyyy-MM-dd") + "' AND type_rcl = 255 GROUP BY nzp_kvar, nzp_supp, nzp_serv) AS p ON c1.nzp_kvar = p.nzp_kvar ");
            sql.Append(" AND c1.nzp_serv = p.nzp_serv ");
            sql.Append(" AND c1.nzp_supp = p.nzp_supp, ");
            sql.Append(pref + "_kernel.supplier s ");
            sql.Append(" WHERE ");
            sql.Append(" c1.nzp_kvar = " + nzp_kvar);
            sql.Append(" AND c1.nzp_supp = s.nzp_supp ");
            sql.Append(" AND c1.sum_insaldo - c1.sum_money + ( ");
            sql.Append(" CASE ");
            sql.Append(" WHEN c1.reval < 0 THEN ");
            sql.Append(" c1.reval ");
            sql.Append(" ELSE ");
            sql.Append(" 0 ");
            sql.Append(" END ");
            sql.Append(" ) + ( ");
            sql.Append(" CASE ");
            sql.Append(" WHEN c1.real_charge < 0 THEN ");
            sql.Append(" c1.real_charge ");
            sql.Append(" ELSE ");
            sql.Append(" 0 ");
            sql.Append(" END ");
            sql.Append(" ) - COALESCE ( ");
            sql.Append(" c2.sum_money - ( ");
            sql.Append(" CASE ");
            sql.Append(" WHEN c2.reval < 0 THEN ");
            sql.Append(" c2.reval ");
            sql.Append(" ELSE ");
            sql.Append(" 0 ");
            sql.Append(" END ");
            sql.Append(" ) - ( ");
            sql.Append(" CASE ");
            sql.Append(" WHEN c2.real_charge < 0 THEN ");
            sql.Append(" c2.real_charge ");
            sql.Append(" ELSE ");
            sql.Append(" 0 ");
            sql.Append(" END ");
            sql.Append(" ), ");
            sql.Append(" 0 ");
            sql.Append(" ) + COALESCE(p.sum_rcl, 0) > 0 ");
            #endregion

            var supps = ClassDBUtils.OpenSQL(sql.ToString(), conn);
            if (supps.resultCode != 0)
            {
                ret.result = false;
                ret.text = "Ошибка поиска поставщиков!" + kvarPref.resultMessage;
                return null;
            }

            try
            {
                List<Supplier> res = supps.resultData.AsEnumerable().Select(x => new Supplier()
                {
                    nzp_supp = Convert.ToInt32(x["nzp_supp"]),
                    name_supp = Convert.ToString(x["name_supp"])
                }).ToList();
                return res;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка конвертирования в список: " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = "Ошибка конвертирования в список!";
                return null;
            }
        }

        /// <summary>
        /// Возвращает список поставщиков, по которым есть долги
        /// </summary>
        /// <param name="finder">nzp</param>
        /// <returns></returns>
        public List<Service> GetListServices(Deal finder, int nzp_supp, out Returns ret)
        {
            ret = Utils.InitReturns();

            #region Проверки
            // проверка текущего пользователя
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Не определен пользователь!";
                return null;
            }

            // проверка текущего дела
            if (finder.nzp_deal <= 0)
            {
                ret.result = false;
                ret.text = "Не определено дело!";
                return null;
            }

            // проверка текущего дела
            if (nzp_supp <= 0)
            {
                ret.result = false;
                ret.text = "Не определен поставщик!";
                return null;
            }

            // проверка доступа к БД
            var conn = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn, true);
            if (!ret.result)
            {
                return null;
            }
            #endregion

            // получение nzp_kvar и pref по nzp_deal
            var kvarPref = ClassDBUtils.OpenSQL("SELECT nzp_kvar, pref FROM " + Points.Pref + "_debt.deal WHERE nzp_deal = " + finder.nzp_deal, conn);

            int nzp_kvar = 0;
            string pref = "";

            if ((kvarPref.resultCode != 0) || (kvarPref.resultData.Rows.Count == 0))
            {
                ret.result = false;
                ret.text = "Не определено дело!" + kvarPref.resultMessage;
                return null;
            }

            try
            {
                nzp_kvar = Convert.ToInt32(kvarPref.resultData.Rows[0]["nzp_kvar"]);
                pref = Convert.ToString(kvarPref.resultData.Rows[0]["pref"]);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка преобразования данных: " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = "Ошибка преобразования данных!";
                return null;
            }

            #region SQL select suppliers
            DateTime calcdate = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1);
            string curr_charge = pref + "_charge_" + (Points.CalcMonth.year_ % 100).ToString("00") + ".charge_" + Points.CalcMonth.month_.ToString("00");
            string prev_charge = pref + "_charge_" + (Points.CalcMonth.month_ > 1
                ? Points.CalcMonth.year_ % 100
                : (Points.CalcMonth.year_ - 1) % 100
                ).ToString("00") + ".charge_" +
                (Points.CalcMonth.month_ > 1
                ? Points.CalcMonth.month_ - 1
                : 12
                ).ToString("00");
            StringBuilder sql = new StringBuilder();
            sql.Append(" SELECT ");
            sql.Append(" c1.nzp_serv, ");
            sql.Append(" s.service_name, ");
            sql.Append(" c1.sum_insaldo - c1.sum_money + ( ");
            sql.Append(" CASE ");
            sql.Append(" WHEN c1.reval < 0 THEN ");
            sql.Append(" c1.reval ");
            sql.Append(" ELSE ");
            sql.Append(" 0 ");
            sql.Append(" END ");
            sql.Append(" ) + ( ");
            sql.Append(" CASE ");
            sql.Append(" WHEN c1.real_charge < 0 THEN ");
            sql.Append(" c1.real_charge ");
            sql.Append(" ELSE ");
            sql.Append(" 0 ");
            sql.Append(" END ");
            sql.Append(" ) - COALESCE ( ");
            sql.Append(" c2.sum_money - ( ");
            sql.Append(" CASE ");
            sql.Append(" WHEN c2.reval < 0 THEN ");
            sql.Append(" c2.reval ");
            sql.Append(" ELSE ");
            sql.Append(" 0 ");
            sql.Append(" END ");
            sql.Append(" ) - ( ");
            sql.Append(" CASE ");
            sql.Append(" WHEN c2.real_charge < 0 THEN ");
            sql.Append(" c2.real_charge ");
            sql.Append(" ELSE ");
            sql.Append(" 0 ");
            sql.Append(" END ");
            sql.Append(" ), ");
            sql.Append(" 0 ");
            sql.Append(" ) + COALESCE(p.sum_rcl, 0) as dolg ");
            sql.Append(" FROM ");
            sql.Append(prev_charge + " c1 ");
            sql.Append(" LEFT JOIN " + curr_charge + " AS c2 ON c1.nzp_kvar = c2.nzp_kvar ");
            sql.Append(" AND c1.nzp_serv = c2.nzp_serv ");
            sql.Append(" AND c1.nzp_supp = c2.nzp_supp ");
            sql.Append(" LEFT JOIN (SELECT nzp_kvar, nzp_serv, nzp_supp, SUM(sum_rcl) as sum_rcl ");
            sql.Append(" FROM " + pref + "_charge_" + (Points.CalcMonth.year_ % 100).ToString("00") + ".perekidka ");
            sql.Append(" WHERE date_rcl >= '" + calcdate.ToString("yyyy-MM-dd") + "' AND date_rcl < '" + calcdate.AddMonths(1).ToString("yyyy-MM-dd") + "' AND type_rcl = 255 GROUP BY nzp_kvar, nzp_supp, nzp_serv) AS p ON c1.nzp_kvar = p.nzp_kvar ");
            sql.Append(" AND c1.nzp_serv = p.nzp_serv ");
            sql.Append(" AND c1.nzp_supp = p.nzp_supp, ");
            sql.Append(pref + "_kernel.services s ");
            sql.Append(" WHERE ");
            sql.Append(" c1.nzp_kvar = " + nzp_kvar);
            sql.Append(" AND c1.nzp_serv = s.nzp_serv ");
            sql.Append(" AND c1.nzp_supp = " + nzp_supp);
            sql.Append(" AND c1.sum_insaldo - c1.sum_money + ( ");
            sql.Append(" CASE ");
            sql.Append(" WHEN c1.reval < 0 THEN ");
            sql.Append(" c1.reval ");
            sql.Append(" ELSE ");
            sql.Append(" 0 ");
            sql.Append(" END ");
            sql.Append(" ) + ( ");
            sql.Append(" CASE ");
            sql.Append(" WHEN c1.real_charge < 0 THEN ");
            sql.Append(" c1.real_charge ");
            sql.Append(" ELSE ");
            sql.Append(" 0 ");
            sql.Append(" END ");
            sql.Append(" ) - COALESCE ( ");
            sql.Append(" c2.sum_money - ( ");
            sql.Append(" CASE ");
            sql.Append(" WHEN c2.reval < 0 THEN ");
            sql.Append(" c2.reval ");
            sql.Append(" ELSE ");
            sql.Append(" 0 ");
            sql.Append(" END ");
            sql.Append(" ) - ( ");
            sql.Append(" CASE ");
            sql.Append(" WHEN c2.real_charge < 0 THEN ");
            sql.Append(" c2.real_charge ");
            sql.Append(" ELSE ");
            sql.Append(" 0 ");
            sql.Append(" END ");
            sql.Append(" ), ");
            sql.Append(" 0 ");
            sql.Append(" ) + COALESCE(p.sum_rcl, 0) > 0 ");
            #endregion

            var supps = ClassDBUtils.OpenSQL(sql.ToString(), conn);
            if (supps.resultCode != 0)
            {
                ret.result = false;
                ret.text = "Ошибка поиска услуг! " + kvarPref.resultMessage;
                return null;
            }

            try
            {
                List<Service> res = supps.resultData.AsEnumerable().Select(x => new Service()
                {
                    nzp_serv = Convert.ToInt32(x["nzp_serv"]),
                    service_name = Convert.ToString(x["service_name"]) + " (" + Convert.ToDecimal(x["dolg"]).ToString("G") + " руб.)"
                }).ToList();
                return res;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка конвертирования в список: " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = "Ошибка конвертирования в список!";
                return null;
            }
        }

        /// <summary>
        /// Добавление перекидки
        /// </summary>
        /// <param name="finder">дело</param>
        /// <param name="money">сумма перекидки</param>
        /// <returns></returns>
        public Returns AddPerekidka(Deal finder, decimal money)
        {
            Returns ret = Utils.InitReturns();
            ret.result = true;
            #region Проверка входных условий
            // проверка текущего пользователя
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Не определен пользователь!";
            }

            // проверка текущего пользователя
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Не определен пользователь!";
            }

            // проверка текущего дела
            if (finder.nzp_deal <= 0)
            {
                ret.result = false;
                ret.text = "Не определено дела!";
            }

            // проверка полей
            if (finder.nzp_supp <= 0 || finder.nzp_serv <= 0)
            {
                ret.result = false;
                ret.text = "Не все данные заполнены!";
            }

            // проверка полей
            if (money <= 0)
            {
                ret.result = false;
                ret.text = "Не все данные заполнены!";
            }

            if (!ret.result)
            {
                return ret;
            }

            // проверка доступа к БД
            var conn = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn, true);
            if (!ret.result)
            {
                return ret;
            }
            #endregion

            var res = ClassDBUtils.OpenSQL(" SELECT nzp_kvar, pref, debt_money FROM " + Points.Pref + "_debt.deal WHERE nzp_deal = " + finder.nzp_deal, conn);

            if ((res.resultCode != 0) || (res.resultData.Rows.Count == 0 ))
            {
                ret.text = " Ошибка получения данных по делу!";
                ret.result = false;
                return ret;
            }

            int nzp_kvar = 0;
            string pref = "";
            try
            {
                nzp_kvar = Convert.ToInt32(res.resultData.Rows[0]["nzp_kvar"]);
                pref = Convert.ToString(res.resultData.Rows[0]["pref"]);
                finder.debt_money = Convert.ToDecimal(res.resultData.Rows[0]["debt_money"]) - money;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = "Ошибка конвертирования данных!";
                return ret;
            }

            DateTime calcdate = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1);
            var trans = conn.BeginTransaction();
            ret = ExecSQL(conn, trans, "INSERT INTO " + pref + "_charge_" + (Points.CalcMonth.year_  % 100).ToString("00")+ ".perekidka " +
                "(nzp_kvar, num_ls, nzp_serv, nzp_supp, type_rcl, date_rcl, tarif, volum, sum_rcl, month_, comment, nzp_user) VALUES (" +
                nzp_kvar + "," + nzp_kvar + "," + finder.nzp_serv + "," + finder.nzp_supp + ",255,'" + calcdate.ToString("yyyy-MM-dd") + "',0,0,'-" + money.ToString("G") + "'," + Points.CalcMonth.month_ + ",'Списание долга'," + finder.nzp_user + ");", true);

            if (!ret.result)
            {
                trans.Rollback();
                conn.Close();
                return ret;
            }

            deal_states_history deal = new deal_states_history();
            deal.nzp_user = finder.nzp_user;
            deal.minus = money;
            deal.nzp_oper = EnumOpers.DebtDown.GetHashCode();
            deal.nzp_deal = finder.nzp_deal;
            MakeOperOnDeal(deal, conn, trans, out ret);
            if (!ret.result)
            {
                trans.Rollback();
                conn.Close();
                return ret;
            }

            ChangeMoneyOnDeal(finder, conn, trans, out ret);
            if (!ret.result)
            {
                trans.Rollback();
                conn.Close();
                return ret;
            }

            trans.Commit();
            conn.Close();
            return ret;
        }

        /// <summary>
        /// Функция обновляет долг после расчета
        /// </summary>
        /// <param name="finder">nzp_user - пользователь, nzp_kvar - ЛС, pref - префикс</param>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        public Returns RecalcDeal(Deal finder, int month, int year)
        {
            Returns ret = Utils.InitReturns();

            #region Проверки
            // проверка текущего пользователя
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Не определен пользователь!";
                return ret;
            }

            // проверка текущего пользователя
            if (finder.nzp_kvar <= 0)
            {
                ret.result = false;
                ret.text = "Не определен ЛС!";
                return ret;
            }

            // проверка текущего пользователя
            if (string.IsNullOrEmpty(finder.pref))
            {
                finder.pref = Points.Pref;
            }

            // проверка доступа к БД
            var conn = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn, true);
            if (!ret.result)
            {
                return ret;
            }
            var trans = conn.BeginTransaction();
            #endregion

            // проверка существования дела и получение nzp_deal
            var deal = ClassDBUtils.OpenSQL("SELECT nzp_deal FROM " + Points.Pref + " WHERE nzp_kvar = " + finder.nzp_kvar + " AND pref = '" + finder.pref + "';", conn);
            if ((deal.resultCode != 0) || (deal.resultData.Rows.Count == 0))
            {
                ret.result = deal.resultCode == 0;
                ret.text = "Не найдено дело!" + deal.resultMessage;
                return ret;
            }

            try
            {
                finder.nzp_deal = Convert.ToInt32(deal.resultData.Rows[0]["nzp_deal"]);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка получения дела: " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = "Ошибка получения дела!";
                return ret;
            }

            // получение текущего долга
            decimal debt_curr = GetDebtForKvar(finder, month, year, conn, trans, out ret);
            if (!ret.result)
            {
                trans.Rollback();
                conn.Close();
                return ret;
            }
            // Получение предыдущего долга
            month--;
            if (month == 0)
            {
                year--;
                month = 12;
            }
            decimal debt_prev = GetDebtForKvar(finder, month, year, conn, trans, out ret);
            if (!ret.result)
            {
                trans.Rollback();
                conn.Close();
                return ret;
            }

            // изменение долга
            finder.debt_money = debt_curr;
            ChangeMoneyOnDeal(finder, conn, trans, out ret);

            if (!ret.result)
            {
                trans.Rollback();
                conn.Close();
                return ret;
            }

            if (debt_curr > 0)
            {
                if (debt_prev != debt_curr)
                {
                    deal_states_history finder2 = new deal_states_history();
                    finder2.nzp_deal = finder.nzp_deal;
                    finder2.debt_money = finder.debt_money;
                    if (debt_curr < debt_prev)
                    {
                        finder2.minus = debt_prev - debt_curr;
                    }
                    else
                    {
                        finder2.plus = debt_curr - debt_prev;
                    }
                    MakeOperOnDeal(finder2, conn, trans, out ret);
                    if (!ret.result)
                    {
                        trans.Rollback();
                        return ret;
                    }
                }

                // проверка рассрочки
            }
            else
            {
                // закрытие дела
            }

            trans.Commit();
            conn.Close();
            return ret;
        }

        /// <summary>
        /// Получение долга для ЛС
        /// </summary>
        /// <param name="finder">ЛС</param>
        /// <param name="month">месяц</param>
        /// <param name="year">год</param>
        /// <param name="trans">транзакция</param>
        /// <param name="ret">доп. информация о резальтатах</param>
        /// <returns>возращает величину долга</returns>
        public decimal GetDebtForKvar(Deal finder, int month, int year, IDbConnection conn, IDbTransaction trans, out Returns ret)
        {
            StringBuilder sql = new StringBuilder();
            string curr_charge = finder.pref + "_charge_" + (year % 100).ToString("G") + ".charge_" + month--.ToString("00");
            if (month == 0)
            {
                year--;
                month = 12;
            }
            string prev_charge = finder.pref + "_charge_" + (year % 100).ToString("G") + ".charge_" + month.ToString("00");
            #region SQL change debt_money
            sql.Append(" SELECT ");
            sql.Append(" SUM(c1.sum_insaldo - c1.sum_money + ( ");
            sql.Append(" CASE ");
            sql.Append(" WHEN c1.reval < 0 THEN ");
            sql.Append(" c1.reval ");
            sql.Append(" ELSE ");
            sql.Append(" 0 ");
            sql.Append(" END ");
            sql.Append(" ) + ( ");
            sql.Append(" CASE ");
            sql.Append(" WHEN c1.real_charge < 0 THEN ");
            sql.Append(" c1.real_charge ");
            sql.Append(" ELSE ");
            sql.Append(" 0 ");
            sql.Append(" END ");
            sql.Append(" ) - COALESCE ( ");
            sql.Append(" c2.sum_money - ( ");
            sql.Append(" CASE ");
            sql.Append(" WHEN c2.reval < 0 THEN ");
            sql.Append(" c2.reval ");
            sql.Append(" ELSE ");
            sql.Append(" 0 ");
            sql.Append(" END ");
            sql.Append(" ) - ( ");
            sql.Append(" CASE ");
            sql.Append(" WHEN c2.real_charge < 0 THEN ");
            sql.Append(" c2.real_charge ");
            sql.Append(" ELSE ");
            sql.Append(" 0 ");
            sql.Append(" END ");
            sql.Append(" ), ");
            sql.Append(" 0 ");
            sql.Append(" )) as dolg ");
            sql.Append(" FROM ");
            sql.Append(prev_charge + " c1 ");
            sql.Append(" LEFT JOIN " + curr_charge + " AS c2 ON c1.nzp_kvar = c2.nzp_kvar AND c2.nzp_serv > 1 AND c2.dat_charge IS NULL ");
            sql.Append(" AND c1.nzp_serv = c2.nzp_serv ");
            sql.Append(" AND c1.nzp_supp = c2.nzp_supp ");
            sql.Append(" WHERE ");
            sql.Append(" c1.nzp_serv > 1 ");
            sql.Append(" AND c1.dat_charge IS NULL ");
            sql.Append(" AND c1.nzp_kvar = " + finder.nzp_kvar);
            #endregion

            if (conn == null)
            {
                conn = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn, true);
                if (!ret.result)
                {
                    return decimal.Zero;
                }
            }

            var debt = ExecScalar(conn, trans, sql.ToString(), out ret, true);
            if (!ret.result)
            {
                return decimal.Zero;
            }

            try
            {
                return Convert.ToDecimal(debt);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка получения долга: " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = "Ошибка получения долга!";
                return decimal.Zero;
            }
        }

        /// <summary>
        /// Проверка соблюдения соглашения
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="prev_debt"></param>
        /// <param name="curr_debt"></param>
        /// <returns></returns>
        public Returns CheckArgeement(Deal finder, decimal change_debt, int month, int year, IDbConnection conn, IDbTransaction trans)
        {
            Returns ret = Utils.InitReturns();

            #region Проверки
            // проверка текущего пользователя
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "Не определен пользователь!";
                return ret;
            }

            // проверка текущего дела
            if (finder.nzp_deal <= 0)
            {
                ret.result = false;
                ret.text = "Не определено дело!";
                return ret;
            }

            // проверка доступа к БД
            if (conn == null)
            {
                conn = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn, true);
                if (!ret.result)
                {
                    return ret;
                }
            }

            if (year < 100)
            {
                year += ((int)DateTime.Now.Year / 100) * 100;
            }
            #endregion

            #region SQL получение текущего соглашения
            string mindate = year.ToString() + "-" + month++.ToString() + "-01";
            if (month == 13)
            {
                year++;
                month = 1;
            }
            string maxdate = year.ToString() + "-" + month.ToString() + "-01";
            StringBuilder sql = new StringBuilder();
            sql.Append(" SET search_path TO '" + Points.Pref + "_debt';");
            sql.Append(" SELECT nzp_agr, incoming_balance as ib, outgoing_balance as ob ");
            sql.Append(" FROM agreement_details ad ");
            sql.Append(" WHERE ad.datemonth >= '" + mindate + "' AND ad.datemonth < '" + maxdate + "' ");
            sql.Append(" AND nzp_agr = (SELECT nzp_agr FROM agreement WHERE nzp_deal = " + finder.nzp_deal + " AND nzp_agr_status = " + s_agr_statuses.Сelebrates.GetHashCode() + ") ");
            #endregion

            var res = ClassDBUtils.OpenSQL(sql.ToString(), conn, trans);
            if (res.resultCode != 0 || res.resultData.Rows.Count == 0)
            {
                ret.result = res.resultCode == 0; // если запрос не вернул данные, то соглашения нет
                ret.text = res.resultMessage;
                return ret;
            }

            try
            {
                var nzp_agr = Convert.ToInt32(res.resultData.Rows[0]["nzp_agr"]);
                var need_pay = Convert.ToDecimal(res.resultData.Rows[0]["ib"]) - Convert.ToDecimal(res.resultData.Rows[0]["ob"]);
                if (need_pay > change_debt)
                {
                    // просрочено
                    sql.Remove(0, sql.Length);
                    sql.Append(" UPDATE deal SET nzp_deal_status = " + s_deal_statuses.AgreementViolated.GetHashCode() + ";");
                    sql.Append(" UPDATE agreement SET nzp_agr_status = " + s_agr_statuses.Violated.GetHashCode() + " WHERE nzp_agr = " + nzp_agr + ";");
                }
                else if (need_pay < change_debt)
                {
                    // пересчитать

                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка проверки соглашения: " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = "Ошибка проверки соглашения!";
                return ret;
            }

            ret.result = true;
            return ret;
        }

        /// <summary>
        /// Перерасчет соглашение
        /// </summary>
        /// <returns></returns>
        public Returns RecalcArgeement(Deal finder, DateTime date, int nzp_agr,IDbConnection conn, IDbTransaction trans)
        {
            Returns ret = Utils.InitReturns();

            // проверка доступа к БД
            if (conn == null)
            {
                conn = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn, true);
                if (!ret.result)
                {
                    return ret;
                }
            }

            // получение информации о рассрочке
            Agreement agr = new Agreement();
            agr.nzp_user = finder.nzp_user;
            agr.nzp_agr = nzp_agr;
            var details = GetArgDetail(agr, out ret);
            if (!ret.result)
            {
                return ret;
            }

            // обновление информации
            string sql = " SET search_path TO '" + Points.Pref + "_debt'; DELETE FROM agreement_details WHERE nzp_agr = " + nzp_agr + ";";
            ret = ExecSQL(conn, trans, sql, true);
            if (!ret.result)
            {
                return ret;
            }

            int count = details.Where(x => x.agr_dat > date).Count();
            if (count == 0)
            {
                ret.text = "Не найдены месяцы для рассрочки!";
                ret.result = false;
                return ret;
            }

            // удаление изменяемых месяцев
            details.RemoveAll(x => x.agr_dat > date);
            foreach (var detail in details)
            {
                if (detail.agr_dat.Year == date.Year && detail.agr_dat.Month == date.Month)
                {
                    detail.sum_outsaldo = finder.debt_money;
                    detail.debt_money = detail.sum_insaldo - detail.sum_outsaldo;
                }
            }

            // заполнение новыми данными
            decimal change = finder.debt_money / count;
            for (var i = 0; i < count; i++)
            {
                details.Add(new AgreementDetails() 
                {
                    agr_dat = date.AddMonths(i+1),
                    sum_insaldo = finder.debt_money - change * i,
                    sum_outsaldo = finder.debt_money - change * (i + 1),
                    debt_money = change
                });
            }

            details.First().nzp_deal = finder.nzp_deal;
            details.First().nzp_agr = nzp_agr;

            SaveAgreement(details, out ret);
            if (!ret.result)
            {
                return ret;
            }

            ret.result = true;
            return ret;
        }
    }
}
