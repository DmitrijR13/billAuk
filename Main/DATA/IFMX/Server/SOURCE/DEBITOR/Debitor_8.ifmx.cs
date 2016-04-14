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
        ///  Совершить операцию над делом - смена статуса дела
        /// </summary>
        /// <param name="finder">операция, дело</param>
        /// <param name="ret">результат</param>
        public void MakeOperOnDeal(deal_states_history finder, out Returns ret)
        {
            this.MakeOperOnDeal(finder, null, null, out ret);
        }


        /// <summary>
        /// Совершить операцию над делом - смена статуса дела
        /// </summary>
        /// <param name="finder">операция, дело</param>
        /// <param name="trans">Транзакция</param>
        /// <param name="ret">результат</param>
        public void MakeOperOnDeal(deal_states_history finder, IDbConnection conn, IDbTransaction trans, out Returns ret)
        {

            #region Проверка данных
            ret = Utils.InitReturns();

            if (finder.nzp_deal == 0)
            {
                ret = new Returns(false, "Отсутствуют данные по делу", -1);
                ret.result = false;
                return;
            }

            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                ret.result = false;
                return;
            }

            if (finder.nzp_oper <= 0)
            {
                ret = new Returns(false, "Не задана операция", -1);
                ret.result = false;
                return;
            }
            #endregion
            
            StringBuilder sql = new StringBuilder();

            try
            {
                #region Открываем соединение
                if (conn == null)
                {
                    conn = GetConnection(Constants.cons_Kernel);
                    ret = OpenDb(conn, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("MakeOperOnDeal : Ошибка при открытии соединения с БД ",
                            MonitorLog.typelog.Error, true);
                        return;
                    }
                }

                #endregion


                #region Обновление статуса дела
                if (finder.nzp_oper != 0)
                {
                    sql.Append(" UPDATE " + Points.Pref + "_debt.deal ");
                    sql.Append(" set nzp_deal_status = ");
                    sql.Append(" (SELECT nzp_deal_status from " + Points.Pref + "_debt.s_opers where nzp_oper = " + finder.nzp_oper + ") ");
                    sql.Append(" where nzp_deal = " + finder.nzp_deal + "; ");
                    if (!ExecSQL(conn, trans, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("MakeOperOnDeal: Ошибка : " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.text = "Ошибка смены статуса дела";
                        ret.result = false;
                        return;
                    }
                }
                #endregion

                #region Сохранение в deal_states_history
                sql.Remove(0, sql.Length);
                sql.Append(" INSERT into " + Points.Pref + "_debt.deal_states_history (nzp_deal,date_state,debt_money,plus,minus,nzp_oper) ");
                sql.Append(" VALUES (" + finder.nzp_deal + ",CURRENT_TIMESTAMP, (select debt_money from " + Points.Pref + "_debt.deal where nzp_deal = " + finder.nzp_deal + ")," + finder.plus + "," + finder.minus + "," + (finder.nzp_oper != 0 ? finder.nzp_oper.ToString() : "NULL") + " ); ");                 
                if (!ExecSQL(conn, trans, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("MakeOperOnDeal: Ошибка : " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.text = "Ошибка смены статуса дела";
                    ret.result = false;
                    return;
                }
                #endregion

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetArgDetail : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка сохранения данных";
                ret.result = false;
                return;
            }
        }

        /// <summary>
        /// Изменение количества долга у дела.
        /// </summary>
        /// <param name="finder">дело</param>
        /// <param name="conn">подключение. Открывается автоматически по состоянию</param>
        /// <param name="ret">результат</param>
        public void ChangeMoneyOnDeal(Deal finder, IDbConnection conn, IDbTransaction trans, out Returns ret)
        {

            #region Проверка данных
            ret = Utils.InitReturns();

            if (finder.nzp_deal == 0)
            {
                ret = new Returns(false, "Отсутствуют данные по делу", -1);
                ret.result = false;
                return;
            }

            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                ret.result = false;
                return;
            }          
            #endregion
            
            StringBuilder sql = new StringBuilder();

            try
            {
                #region Открываем соединение
                if (conn == null)
                {
                    conn = GetConnection(Constants.cons_Kernel);
                    ret = OpenDb(conn, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("MakeOperOnDeal : Ошибка при открытии соединения с БД ",
                            MonitorLog.typelog.Error, true);
                        return;
                    }
                }
                #endregion


                #region Обновление долга дела
                sql.Append(" UPDATE " + Points.Pref + "_debt.deal ");
                sql.Append(" set debt_money = " + finder.debt_money+ " ");
                sql.Append(" where nzp_deal = " + finder.nzp_deal);                
                if (!ExecSQL(conn, trans, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("ChangeMoneyOnDeal: Ошибка : " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.text = "Ошибка обновления количества долга у дела";
                    ret.result = false;
                    return;
                }
                #endregion                

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры ChangeMoneyOnDeal : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка сохранения данных";
                ret.result = false;
                return;
            }
        }
    }
}
