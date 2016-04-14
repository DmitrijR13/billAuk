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

namespace STCLINE.KP50.DataBase
{
    public partial class Debitor : DataBaseHead
    {


        /// <summary>
        /// лист должников
        /// </summary>
        /// <param name="str">входная строка</param>
        /// <returns>выходная строка</returns>
        public List<Debt> GetDebitors(DebtFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Debt> list = new List<Debt>();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            string tXX_debt = "t" + finder.nzp_user + "_debt";
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
                    MonitorLog.WriteLog("Debitors : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }
                #endregion

                sql.Remove(0, sql.Length);
                sql.Append("SELECT s.mark, s.area, s.nzp_debt, s.adr, s.fio, s.phone, s.debt_money, s.pref, s.nzp_kvar, s.is_priv, max(d.nzp_deal) as nzp_deal ");
                sql.Append("FROM public." + tXX_debt + " s ");
                sql.Append("LEFT JOIN " + Points.Pref + "_debt.deal d ON ");
                sql.Append("d.nzp_kvar = s.nzp_kvar and nzp_deal_status <> 1 ");
                sql.Append(" Group by s.mark, s.area, s.nzp_debt, s.adr, s.fio, s.phone, s.debt_money, s.pref, s.nzp_kvar, s.is_priv");
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
                                if (info.Name == "nzp_debt") property = "nzp_debt";
                                else if (info.Name == "area") property = "area";
                                else if (info.Name == "adr") property = "adr";
                                else if (info.Name == "fio") property = "fio";
                                else if (info.Name == "phone") property = "phone";
                                else if (info.Name == "debt_money") property = "debt_money";
                                else if (info.Name == "deal") property = "nzp_deal";
                                else if (info.Name == "status") property = "status";
                                else if (info.Name == "unpayment_days") property = "unpayment_days";
                                else if (info.Name == "children_count") property = "children_count";
                                else if (info.Name == "is_priv") property = "is_priv";
                                else property = "";

                             //   sql.Append(" where nzp_deal_status =3 ");


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
                sql.Append("LIMIT " + finder.rows + " OFFSET " + finder.skip);

                ret = ExecRead(con_db, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + ret.text+ ret.sql_error, MonitorLog.typelog.Error, 20, 201, true);
                    return null;
                }
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        Debt debitor = new Debt();

                        if (reader["mark"] != DBNull.Value) debitor.mark = Convert.ToInt32(reader["mark"]);
                        if (reader["nzp_debt"] != DBNull.Value) debitor.nzp_debt = Convert.ToInt32(reader["nzp_debt"]);
                    /*    if (reader["nzp_deal_status"] != DBNull.Value)
                        {
                            int a = Convert.ToInt32(reader["nzp_deal_status"]);
                            if (a != 1)
                                debitor.deal = "Есть";
                            else
                                debitor.deal = "";
                        }
                        else
                            debitor.deal = "";*/
                        if (reader["nzp_deal"] != DBNull.Value)
                        {
                            int a = Convert.ToInt32(reader["nzp_deal"]);
                            if (a != 0)
                                debitor.deal = "Есть";
                            else
                                debitor.deal = "";
                        }
                        else
                            debitor.deal = "";
                        if (reader["area"] != DBNull.Value) debitor.area = Convert.ToString(reader["area"]);
                        if (reader["adr"] != DBNull.Value) debitor.adr = Convert.ToString(reader["adr"]);
                        if (reader["fio"] != DBNull.Value) debitor.fio = Convert.ToString(reader["fio"]);
                        if (reader["phone"] != DBNull.Value) debitor.phone = Convert.ToString(reader["phone"]);
                        if (reader["debt_money"] != DBNull.Value) debitor.debt_money = Convert.ToDecimal(reader["debt_money"]);
                        //if (reader["unpayment_days"] != DBNull.Value) debitor.unpayment_days = Convert.ToInt32(reader["unpayment_days"]);
                        if (reader["pref"] != DBNull.Value) debitor.pref = Convert.ToString(reader["pref"]).Trim();
                        if (reader["nzp_kvar"] != DBNull.Value) debitor.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
                        if (reader["is_priv"] != DBNull.Value)
                            debitor.is_priv = "Да";
                        else
                            debitor.is_priv = "";
                        //if (reader["children_count"] != DBNull.Value) debitor.children_count = Convert.ToInt32(reader["children_count"]);
                        list.Add(debitor);
                    }
                }
                
                var sqlStr = " SELECT COUNT(*) FROM (SELECT 1 FROM " + sDefaultSchema + tXX_debt + " s " +
                             " LEFT JOIN " + Points.Pref + "_debt.deal d ON " +
                             " d.nzp_kvar = s.nzp_kvar and nzp_deal_status <> 1 " +
                             " group by s.mark, s.area, s.nzp_debt, s.adr, s.fio, s.phone, s.debt_money, s.pref, s.nzp_kvar, s.is_priv)" +
                             " rec_counts";
                object count = ExecScalar(con_db, sqlStr, out ret, true);
                if (ret.result && count != null && count != DBNull.Value)
                {
                    ret.tag = Convert.ToInt32(count);
                }

                return list;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры FindDebt : " + ex.Message, MonitorLog.typelog.Error, true);
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

        public Returns SaveDebtChanges(Deal finder)
        {
            Returns ret = Utils.InitReturns();
            IDbConnection conn_db = null;
            #region Коннект к базе
            conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion
            StringBuilder sql = new StringBuilder();

            sql.Append(" update " + Points.Pref + "_debt" + tableDelimiter + "deal set (nzp_deal_status, debt_money,debt_date,responsible_name,comment)=");
            sql.Append("(" + finder.nzp_deal + "," + finder.debt_money + ",'" + finder.debt_fix_date.ToString("dd.MM.yyyy") + "','" + finder.responsible_name + "','" + finder.comment + "') where nzp_deal=" + finder.nzp_deal + "");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("SaveDealChanges: " + sql.ToString(), MonitorLog.typelog.Error, true);
                ret.text = "Ошибка при сохранении изменений в Деле";
                ret.result = false;
                return ret;
            }
            return ret;
        }

        public Returns SaveDealChecked(List<Deal> finder)
        {
            if (finder == null || finder.Count == 0) return new Returns(false, "Не заданы входные параметры", -1);
            if (finder[0].nzp_user <= 0) new Returns(false, "Пользователь не определен", -1);

            IDbConnection connDb = null;
            #region Коннект к базе
            connDb = GetConnection(Constants.cons_Kernel);
            var ret = OpenDb(connDb, true);
            if (!ret.result) return ret;
            #endregion
            var sql = new StringBuilder();
            var spis_deal = "";
            foreach (var d in finder)
            {
                if (spis_deal != "") spis_deal += ", " + d.nzp_deal;
                else spis_deal += d.nzp_deal;
            }
            sql = new StringBuilder();
            sql.Append(" update public" + tableDelimiter + "t" + finder[0].nzp_user + "_deal" + " set mark = null ");
            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("SaveDealChecked: " + sql.ToString(), MonitorLog.typelog.Error, true);
                ret.text = "Ошибка при сохранении помеченных Дел";
                ret.result = false;
                return ret;
            }

            sql = new StringBuilder();
            sql.Append(" update public" + tableDelimiter + "t" + finder[0].nzp_user + "_deal" + " set mark = 1 ");
            sql.Append(" where nzp_deal in (" + spis_deal + ")");
            ret = ExecSQL(connDb, sql.ToString(), true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("SaveDealChecked: " + sql.ToString(), MonitorLog.typelog.Error, true);
                ret.text = "Ошибка при сохранении помеченных Дел";
                ret.result = false;
                return ret;
            }
            return ret;
        }
    }
}
