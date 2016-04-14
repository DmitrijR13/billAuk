using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.DataBase;
using System.Collections;
using System.Data;
using FastReport;
using System.Globalization;
using SevenZip;

namespace STCLINE.KP50.DataBase
{
    public partial class Debitor : DataBaseHead
    {
        /// <summary>
        /// Получечение списка дел
        /// </summary>
        /// <param name="nzp_user">пользователь</param>
        /// <param name="ret"></param>
        /// <returns>bool</returns>
        public List<Deal> GetDeals(DealFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            string tXX_deal = "t" + finder.nzp_user + "_deal";
           
            List<Deal> listDeals = new List<Deal>();
            StringBuilder sql = new StringBuilder();
            try
            {
                #region Открываем соединение с базами

                con_web = GetConnection(Constants.cons_Webdata);
                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_web, true);
                if (ret.result)
                {
                    ret = OpenDb(con_db, true);
                }
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Debitor : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }
                #endregion

                sql.Remove(0, sql.Length);
                sql.Append("SELECT nzp_deal, area, fio, debt_money, status, nzp_kvar, pref FROM " + tXX_deal + " ");

                #region сортировка

                if (finder.orderings != null && finder.orderings.Count > 0)
                {
                    string property;
                    bool isFirst = true;
                    foreach (_OrderingField order in finder.orderings)
                    {
                        try
                        {
                            Type type = finder.GetType();
                            System.Reflection.MemberInfo info = type.GetProperty(order.fieldName);
                            if (info != null)
                            {
                                if (info.Name == "area") property = "area";
                                else if (info.Name == "fio") property = "fio";
                                else if (info.Name == "debt_money") property = "debt_money";
                                else if (info.Name == "status") property = "status";
                                else property = "";

                                if (property != "")
                                {
                                    if (isFirst)
                                    {
                                        sql.Append(" ORDER BY ");
                                        isFirst = false;
                                    }
                                    else sql.Append(", ");
                                    sql.Append(" " + property + " " + (order.orderingDirection == OrderingDirection.Ascending ? " ASC " : " DESC "));
                                }
                            }
                        }
                        catch { }
                    }
                }

                #endregion

                sql.Append("LIMIT " + finder.rows + " OFFSET " + finder.skip + " ");

                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        Deal deal = new Deal();
                        if (reader["nzp_deal"] != DBNull.Value) deal.nzp_deal = Convert.ToInt32(reader["nzp_deal"]);
                        if (reader["area"] != DBNull.Value) deal.area = Convert.ToString(reader["area"]).Trim();
                        if (reader["fio"] != DBNull.Value) deal.fio = Convert.ToString(reader["fio"]).Trim();
                        if (reader["debt_money"] != DBNull.Value) deal.debt_money = Convert.ToDecimal(reader["debt_money"]);
                        if (reader["status"] != DBNull.Value) deal.status = Convert.ToString(reader["status"]).Trim();
                        if (reader["pref"] != DBNull.Value) deal.pref = Convert.ToString(reader["pref"]).Trim();
                        if (reader["nzp_kvar"] != DBNull.Value) deal.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);

                        listDeals.Add(deal);
                    }
                }
                sql.Remove(0, sql.Length);
                sql.Append("SELECT COUNT(*) FROM " + tXX_deal);
                object count = ExecScalar(con_db, sql.ToString(), out ret, true);
                if (ret.result && count != null && count != DBNull.Value)
                {
                    ret.tag = Convert.ToInt32(count);
                }

                return listDeals;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetListDeals : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (con_db != null)
                {
                    con_db.Close();
                }

                if (con_web != null)
                {
                    con_web.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

    }
}
