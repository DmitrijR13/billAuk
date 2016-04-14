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
	public partial class Debitor: DataBaseHead
	{
		/// <summary>
		/// формирование результата поиска долгов
		/// </summary>
		/// <param name="finder"></param>
		/// <param name="ret"></param>
		/// <returns></returns>
		public void FindDebt(DebtFinder finder, out Returns ret)
		{
			ret = Utils.InitReturns();
			string whereString = "";

			if (finder.nzp_user < 1)
			{
				ret.result = false;
				ret.text = "Не определен пользователь";
				return;
			}

			//для учитывания шаблона поиска лиц счетов
			string spls_from = "";
			//для учитывания шаблона поиска лиц счетов
			string spls_where = "";
			
			StringBuilder sql = new StringBuilder();
			IDbConnection conn_web = null;
			IDbConnection conn_db = null;
			
			//для поиска по шаблону поиска
			string tXX_debt = "t" + finder.nzp_user + "_debt";

			//соединение с БД
			conn_web = GetConnection(Constants.cons_Webdata);
			ret = OpenDb(conn_web, true);
			if (!ret.result)
			{
				MonitorLog.WriteLog("Ошибка при открытии соединения с БД в FindDebt", MonitorLog.typelog.Error, true);
				conn_web.Close();
				return;
			}


			//соединение с БД
			conn_db = GetConnection(Constants.cons_Kernel);
			ret = OpenDb(conn_db, true);
			if (!ret.result)
			{
				MonitorLog.WriteLog("Ошибка при открытии соединения с БД в FindZvk", MonitorLog.typelog.Error, true);
				conn_db.Close();
				return;
			}

			#region Удаление существующей таблицы

			if (TableInWebCashe(conn_web, tXX_debt))
			{
				ExecSQL(conn_web, "SET search_path to public; Drop table " + tXX_debt, false);
			}

			#endregion

			#region Создание таблицы

			try
			{
				ret = ExecSQL(conn_web,
					" SET search_path to public; " +
					" Create table   " + tXX_debt +
					" (nzp_debt       SERIAL," + 
					"  mark           integer default 1," + 
					"  area           char(100), " +
					"  adr            char(100), " +
					"  fio            char(100), " +
					"  phone          char(50), " +
					"  pref           char(10)," + 
					"  nzp_kvar       integer,"  +
					"  debt_money     numeric, " +
					"  unpayment_days integer, " +
					"  is_priv        char(10)," + 
					"  children_count int" + 
					" ) ", true);
			}
			catch (Exception ex)
			{
				MonitorLog.WriteLog("Ошибка при создании таблицы " + tXX_debt + " " + ex.Message, MonitorLog.typelog.Error, true);
				return;
			}

			#endregion

			#region учитываем шаблон поиска лиц счетов/либо нет

			if (Utils.GetParams(finder.prms, Constants.act_findls))
			{
				//вызов следует из шаблона адресов, поэтому
				//надо прежде заполнить список адресов
				DbAdres db = new DbAdres();

				db.FindLs((Ls)finder, out ret);
				db.Close();

				if (!ret.result)
				{
					return;
				}
				else
				{
					spls_from = ",t" + finder.nzp_user + "_spls spls, ";
					spls_where = " AND spls.nzp_kvar = kv.nzp_kvar ";
				}
			}
			else
			{
				if (finder.RolesVal != null)
					foreach (_RolesVal role in finder.RolesVal)
					{
						if (role.tip == Constants.role_sql)
						{
							if (role.kod == Constants.role_sql_area) whereString += " AND kv.nzp_area IN (" + role.val + ")";
							else if (role.kod == Constants.role_sql_geu) whereString += " AND kv.nzp_geu IN (" + role.val + ")";
						}
					}
			}

			#endregion

			#region получение доступных префиксов БД

			string where_wp = "";
			if (finder.RolesVal != null)
				foreach (_RolesVal role in finder.RolesVal)
				{
					if (role.tip == Constants.role_sql)
					{
						if (role.kod == Constants.role_sql_wp) where_wp += " and nzp_wp in (" + role.val + ") ";
					}
				}

			sql.Remove(0, sql.Length);
			sql.Append(" select * ");
			sql.Append(" from  " + Points.Pref + "_kernel" + ".s_point ");
			sql.Append(" where nzp_wp > 1 " + where_wp);

			IDataReader reader = null;
			if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
			{
				MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
				conn_db.Close();
				sql.Remove(0, sql.Length);
				ret.result = false;
				return;
			}
			List<string> dbs = new List<string>();
			while (reader.Read())
			{
				dbs.Add(reader["bd_kernel"].ToString().ToLower().Trim());
			}
			reader.Close();

			#endregion

			#region работа с параметрами поиска

			StringBuilder sumDebtCond = new StringBuilder();

			if (finder.sum_debt_from != 0 && finder.sum_debt_to != 0)
			{
				sumDebtCond.Append(" AND res.debt BETWEEN " + finder.sum_debt_from + " AND " + finder.sum_debt_to + " ");
			}
			else
			{
				if (finder.sum_debt_from != 0)
					sumDebtCond.Append(" AND res.debt >= " + finder.sum_debt_from + " ");

				if (finder.sum_debt_to != 0)
					sumDebtCond.Append(" AND res.debt <= " + finder.sum_debt_to);
			}

			#endregion

			foreach (string _pref in dbs)
			{
				#region расчет долга

				//предыдущий месяц
				DateTime before = (new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1)).AddMonths(-1);

				ExecSQL(conn_db, "DROP TABLE tmp_" + finder.nzp_user + "_charge_month;", false);
				sql.Remove(0, sql.Length);
				sql.Append("CREATE TEMP TABLE tmp_" + finder.nzp_user + "_charge_month ");
				sql.Append("(nzp_kvar integer, sum_insaldo numeric, sum_money numeric, reval numeric, real_charge numeric);");
				ret = ExecSQL(conn_db, sql.ToString(), true);

				sql.Remove(0, sql.Length);
				sql.Append(" Insert into tmp_" + finder.nzp_user + "_charge_month");
				sql.Append(" (nzp_kvar, sum_insaldo, sum_money, reval, real_charge) ");
				sql.Append(" SELECT");
				sql.Append(" ch.nzp_kvar,");
				sql.Append(" SUM(ch.sum_insaldo) as sum_insaldo,");
				sql.Append(" SUM(ch.sum_money),");
				sql.Append(" SUM(CASE WHEN ch.reval >= 0 then 0 ELSE ch.reval END) as reval,");
				sql.Append(" SUM(CASE WHEN ch.real_charge >= 0 then 0 ELSE ch.real_charge END) as real_charge");
				sql.Append(" FROM ");
				sql.Append(_pref + "_charge_" + before.Year.ToString().Substring(2,2) + ".charge_" + before.Month.ToString("00") + " ch,");
				sql.Append(_pref + "_data.kvar kv ");
				sql.Append(" WHERE");
				sql.Append(" ch.dat_charge IS NULL ");
				sql.Append(" AND ch.nzp_serv > 1 ");
				sql.Append(" AND ch.nzp_kvar = kv.nzp_kvar ");
				sql.Append(whereString);
				sql.Append(" GROUP BY kv.nzp_kvar, ch.nzp_kvar;");
				ret = ExecSQL(conn_db, sql.ToString(), true);

				sql.Remove(0, sql.Length);
				sql.Append(" Insert into tmp_" + finder.nzp_user + "_charge_month");
				sql.Append(" (nzp_kvar, sum_money, reval, real_charge) ");
				sql.Append(" SELECT");
				sql.Append(" ch.nzp_kvar,");
				sql.Append(" SUM(ch.sum_money),");
				sql.Append(" SUM(CASE WHEN ch.reval >= 0 then 0 ELSE ch.reval END) as reval,");
				sql.Append(" SUM(CASE WHEN ch.real_charge >= 0 then 0 ELSE ch.real_charge END) as real_charge");
				sql.Append(" FROM ");
				sql.Append(_pref + "_charge_" + Points.CalcMonth.year_.ToString().Substring(2, 2) + ".charge_" + Points.CalcMonth.month_.ToString("00") + " ch,");
				sql.Append(_pref + "_data.kvar kv ");
				sql.Append(" WHERE");
				sql.Append(" ch.dat_charge IS NULL ");
				sql.Append(" AND ch.nzp_serv > 1 ");
				sql.Append(" AND ch.nzp_kvar = kv.nzp_kvar ");
				sql.Append(whereString);
				sql.Append(" GROUP BY kv.nzp_kvar, ch.nzp_kvar;");
				ret = ExecSQL(conn_db, sql.ToString(), true);


				//итоговая таблица
				ExecSQL(conn_db, "DROP TABLE tmp_" + finder.nzp_user + "_charge_month_result;", false);
				sql.Remove(0, sql.Length);
				sql.Append("CREATE TEMP TABLE tmp_" + finder.nzp_user + "_charge_month_result ");
				sql.Append("(nzp_kvar integer, debt numeric);");
				ret = ExecSQL(conn_db, sql.ToString(), true);

				sql.Remove(0, sql.Length);
				sql.Append(" Insert into tmp_" + finder.nzp_user + "_charge_month_result");
				sql.Append(" (nzp_kvar, debt) ");
				sql.Append(" SELECT");
				sql.Append(" nzp_kvar,");
				sql.Append(" SUM(res.sum_insaldo) - SUM(res.sum_money)  + SUM(res.reval) + SUM(res.real_charge) ");
				sql.Append(" FROM ");
				sql.Append(" tmp_" + finder.nzp_user + "_charge_month as res ");
				sql.Append(" GROUP BY nzp_kvar;");
				ret = ExecSQL(conn_db, sql.ToString(), true);

				#endregion

				#region итоговая выборка

				sql.Remove(0, sql.Length);
				sql.Append(" Insert into public." + tXX_debt);
				sql.Append(" (area, adr, fio, phone, debt_money, pref, nzp_kvar, is_priv) ");
				sql.Append("SELECT  area.area,");

                sql.Append("trim(t.town)||' '||(case when trim(COALESCE(r.rajon,''))='-' then ' ' else trim(COALESCE(r.rajon,'')) end)||' '||");
                sql.Append("trim(COALESCE(u.ulica,''))||' д.'||trim(COALESCE(d.ndom,''))||' '||(case when trim(COALESCE(d.nkor,''))='-' then '' ");
                sql.Append("else trim(COALESCE(d.nkor,'')) end)||' кв.' ||kv.nkvar ");

				sql.Append("as adr,");
				sql.Append("kv.fio,");
				sql.Append("kv.phone,");
				sql.Append("res.debt as debt,");
				sql.Append("'" + _pref + "',");
				sql.Append("kv.nzp_kvar,");
				sql.Append("(SELECT MAX(val_prm) FROM " + _pref + "_data.prm_1 WHERE is_actual <> 100 AND nzp = kv.nzp_kvar AND nzp_prm = 8  AND mdy(" + Points.CalcMonth.month_ + ",1," + Points.CalcMonth.year_ + ") BETWEEN dat_s AND dat_po) as privatiz ");
				
                sql.Append("FROM ");
				sql.Append(_pref + "_data.s_area area,");
                sql.Append(spls_from);
                sql.Append("tmp_" + finder.nzp_user + "_charge_month_result res, ");
                sql.Append(Points.Pref + "_data.kvar kv ");


                sql.Append("LEFT JOIN " + _pref + "_data.dom d ON kv.nzp_dom = d.nzp_dom ");
                sql.Append("LEFT JOIN " + _pref + "_data.s_ulica u  ");
                sql.Append("LEFT JOIN " + _pref + "_data.s_rajon r  ");
                sql.Append("LEFT JOIN " + _pref + "_data.s_town t ");

                sql.Append("ON r.nzp_town = t.nzp_town ");
                sql.Append("ON u.nzp_raj  = r.nzp_raj ");
                sql.Append("ON d.nzp_ul  = u.nzp_ul ");
				
				sql.Append("WHERE ");
				sql.Append("res.nzp_kvar = kv.nzp_kvar ");
				sql.Append("AND area.nzp_area = kv.nzp_area ");
				sql.Append("AND res.debt > 0 ");
				sql.Append(sumDebtCond.ToString());
				sql.Append(spls_where);
				sql.Append(whereString);
				ret = ExecSQL(conn_db, sql.ToString(), true);

				#endregion
			}
			if (!ret.result)
			{
				MonitorLog.WriteLog("Ошибка при заполнении данными " + tXX_debt + " в FindDebt " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
				conn_web.Close();
				return;
			}

			if (ret.result)
			{
				if (conn_db != null)
					conn_db.Close();
				if (conn_web != null)
					conn_web.Close();
				if (reader != null)
					reader.Close();
			}
			return;
		}

		/// <summary>
		/// формирование результата поиска дел
		/// </summary>
		/// <param name="finder"></param>
		/// <param name="ret"></param>
		public void FindDeal(DealFinder finder, out Returns ret)
		{
			ret = Utils.InitReturns();
			string whereString = "";

			if (finder.nzp_user < 1)
			{
				ret.result = false;
				ret.text = "Не определен пользователь";
				return;
			}

			//для учитывания шаблона поиска лиц счетов
			string spls_from = "";
			//для учитывания шаблона поиска лиц счетов
			//string spls_where = "";

			StringBuilder sql = new StringBuilder();
			IDbConnection conn_web = null;
			IDbConnection conn_db = null;

			//для поиска по шаблону поиска
			string tXX_deal = "t" + finder.nzp_user + "_deal";

			//соединение с БД
			conn_web = GetConnection(Constants.cons_Webdata);
			ret = OpenDb(conn_web, true);
			if (!ret.result)
			{
				MonitorLog.WriteLog("Ошибка при открытии соединения с БД в FindDeal", MonitorLog.typelog.Error, true);
				conn_web.Close();
				return;
			}

			//соединение с БД
			conn_db = GetConnection(Constants.cons_Kernel);
			ret = OpenDb(conn_db, true);
			if (!ret.result)
			{
				MonitorLog.WriteLog("Ошибка при открытии соединения с БД в FindZvk", MonitorLog.typelog.Error, true);
				conn_db.Close();
				return;
			}

			#region Удаление существующей таблицы

			if (TableInWebCashe(conn_web, tXX_deal))
			{
				ExecSQL(conn_web, "SET search_path to public; Drop table " + tXX_deal, false);
			}

			#endregion

			#region Создание таблицы

			try
			{
				ret = ExecSQL(conn_web,
					" SET search_path to public; " +
					" Create table   " + tXX_deal +
					" (nzp_deal       integer," +
					"  mark           integer default 1," +
					"  nzp_area       integer," + 
					"  area           char(100), " +
					"  fio            char(100), " +
					"  debt_money     numeric, " +
					"  status         varchar(255)," + 
					"  pref           char(10)," +
					"  nzp_kvar       integer" +
					" ) ", true);
			}
			catch (Exception ex)
			{
				MonitorLog.WriteLog("Ошибка при создании таблицы " + tXX_deal + " " + ex.Message, MonitorLog.typelog.Error, true);
				return;
			}

			#endregion

			#region учитываем шаблон поиска лиц счетов/либо нет

			if (Utils.GetParams(finder.prms, Constants.act_findls))
			{
				//вызов следует из шаблона адресов, поэтому
				//надо прежде заполнить список адресов
				DbAdres db = new DbAdres();

				db.FindLs((Ls)finder, out ret);
				db.Close();

				if (!ret.result)
				{
					return;
				}
				else
				{
					spls_from = "," + finder.nzp_user + "_spls spls ";
					//spls_where = " AND spls.nzp_kvar = deal.nzp_kvar ";
				}
			}
			else
			{
				if (finder.RolesVal != null)
					foreach (_RolesVal role in finder.RolesVal)
					{
						if (role.tip == Constants.role_sql)
						{
							if (role.kod == Constants.role_sql_area) whereString += " AND kv.nzp_area IN (" + role.val + ")";
							else if (role.kod == Constants.role_sql_geu) whereString += " AND kv.nzp_geu IN (" + role.val + ")";
						}
					}
			}

			#endregion

			#region получение доступных префиксов БД

			string where_wp = "";
			if (finder.RolesVal != null)
				foreach (_RolesVal role in finder.RolesVal)
				{
					if (role.tip == Constants.role_sql)
					{
						if (role.kod == Constants.role_sql_wp) where_wp += " and nzp_wp in (" + role.val + ") ";
					}
				}

			sql.Remove(0, sql.Length);
			sql.Append(" select * ");
			sql.Append(" from  " + Points.Pref + "_kernel" + ".s_point ");
			sql.Append(" where nzp_wp > 1 " + where_wp);

			IDataReader reader = null;
			if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
			{
				MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
				conn_db.Close();
				sql.Remove(0, sql.Length);
				ret.result = false;
				return;
			}
			List<string> dbs = new List<string>();
			while (reader.Read())
			{
				dbs.Add(reader["bd_kernel"].ToString().ToLower().Trim());
			}
			reader.Close();

			#endregion

			#region работа с параметрами поиска

			StringBuilder cond = new StringBuilder();
			//тип жилья
			if (finder.type_gil != -1)
				cond.Append(" AND d.is_priv = " + finder.type_gil + " ");
			//статус дела
			if (finder.nzp_deal_stat != 0)
				cond.Append(" AND d.nzp_deal_status = " + finder.nzp_deal_stat + " ");
			//дата фиксации долга
			if (finder.debt_fix_date != DateTime.MinValue)
				cond.Append(" AND d.debt_date = " + finder.debt_fix_date + " ");
			//дети до 18 лет
			if (finder.children)
				cond.Append(" AND d.children_count > 0 ");
			//сумма долга со знаком
			if (finder.mode != 0 && finder.sum_debt > 0)
			{
				switch ((EnumMarks)Enum.Parse(typeof(EnumMarks), finder.mode.ToString()))
				{
					case EnumMarks.LessEqual:
						{
							cond.Append(" AND d.debt_money <= ");
							break;
						}
					case EnumMarks.Equal:
						{
							cond.Append(" AND d.debt_money = ");
							break;
						}
					case EnumMarks.MoreEqual:
						{
							cond.Append(" AND d.debt_money >= ");
							break;
						}
				}
				cond.Append(finder.sum_debt + " ");
			}
			//ответственный
			if(finder.fio != "")
				sql.Append(" AND d.fio LIKE ('%" + finder.fio + "%')");

			sql.Remove(0, sql.Length);
			sql.Append(" Insert into public." + tXX_deal);
			sql.Append(" (nzp_deal, nzp_kvar, nzp_area, fio, debt_money, status, pref)");
			sql.Append(" SELECT d.nzp_deal,");
			sql.Append(" d.nzp_kvar,");
			sql.Append(" d.nzp_area,");
			sql.Append(" d.fio,");
			sql.Append(" d.debt_money,");
			sql.Append(" sds.name_deal_status,");
			sql.Append(" d.pref ");
			sql.Append("FROM " + Points.Pref + "_debt.deal d,");
			sql.Append(Points.Pref + "_debt.s_deal_statuses sds ");
			sql.Append("WHERE ");
			sql.Append("d.nzp_deal_status = sds.nzp_deal_status ");
			sql.Append(cond);
			ret = ExecSQL(conn_db, sql.ToString(), true);

			#endregion

			foreach (_Point _pref in Points.PointList)
			{   
				sql.Remove(0, sql.Length);
				sql.Append("UPDATE public." + tXX_deal + " ");
				sql.Append("SET area = ar.area FROM " + _pref.pref + "_data.s_area ar ");
				sql.Append("WHERE ar.nzp_area = public." + tXX_deal + ".nzp_area;");
				ret = ExecSQL(conn_db, sql.ToString(), true);
			}
			if (!ret.result)
			{
				MonitorLog.WriteLog("Ошибка при заполнении данными " + tXX_deal + " в FindDeal " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
				conn_web.Close();
				return;
			}
			return;
		}

		/// <summary>
		/// Получает библиотеку со списками для заполнения полей поиска
		/// </summary>
		/// <returns>Библиотека со списками для заполнения полей поиска</returns>
		public Dictionary<string, Dictionary<int, string>> GetDebitorLists(DealFinder finder, out Returns ret)
		{
			Dictionary<string, Dictionary<int, string>> List = new Dictionary<string, Dictionary<int, string>>();
			ret = Utils.InitReturns();
			IDbConnection con_db = null;
			IDataReader reader = null;
			StringBuilder sql = new StringBuilder();

			try
			{
				#region Открываем соединение с базой

				con_db = GetConnection(Constants.cons_Kernel);

				ret = OpenDb(con_db, true);
				if (!ret.result)
				{
					MonitorLog.WriteLog("Ошибка при открытии соединения с БД в GetDebitorLists", MonitorLog.typelog.Error, true);
					return null;
				}
				#endregion

				#region Соглашение

				sql.Remove(0, sql.Length);
				sql.Append("SELECT nzp_agr_statuses, name_agr_statuses FROM " + Points.Pref + "_debt.s_agr_statuses;");

				if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
				{
					MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
					ret.result = false;
					return null;
				}

				if (reader != null)
				{
					Dictionary<int, string> temp_dict = new Dictionary<int, string>();
					while (reader.Read())
					{
						int a = 0;
						string b = "";
						if (reader["nzp_agr_statuses"] != DBNull.Value) a = Convert.ToInt32(reader["nzp_agr_statuses"]);
						if (reader["name_agr_statuses"] != DBNull.Value) b = Convert.ToString(reader["name_agr_statuses"]).Trim();
						temp_dict.Add(a, b);
					}
					List.Add("Соглашение", temp_dict);
				}

				#endregion

				#region Статус дела

				sql.Remove(0, sql.Length);
				sql.Append("Select nzp_deal_status, name_deal_status from " + Points.Pref + "_debt.s_deal_statuses");

				if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
				{
					MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
					ret.result = false;
					return null;
				}
				if (reader != null)
				{
					Dictionary<int, string> temp_dict = new Dictionary<int, string>();
					while (reader.Read())
					{
						int a = 0;
						string b = "";
						if (reader["nzp_deal_status"] != DBNull.Value) a = Convert.ToInt32(reader["nzp_deal_status"]);
						if (reader["name_deal_status"] != DBNull.Value) b = Convert.ToString(reader["name_deal_status"]).Trim();
						temp_dict.Add(a, b);
					}
					List.Add("Статус дела", temp_dict);
				}

				#endregion

				return List;
			}
			catch (Exception ex)
			{
				MonitorLog.WriteLog("Ошибка выполнения процедуры GetDebitorLists : " + ex.Message, MonitorLog.typelog.Error, true);
				return null;
			}
			finally
			{
				#region Закрытие соединений

				if (con_db != null)
				{
					con_db.Close();
				}

				if (reader != null)
				{
					reader.Close();
				}

				sql.Remove(0, sql.Length);

				#endregion
			}
		}

		/// <summary>
		/// работа с пени
		/// </summary>
		/// <param name="nzp_kvar">идентификатор жильца</param>
		/// <param name="pref">префикс базы данных</param>
		/// <param name="date">месяц и год</param>
		/// <param name="flag">начисление пени - true, удаление пени - false</param>
		public void PennyOperations(int nzp_user, int nzp_kvar, string pref, DateTime date, bool flag, out Returns ret)
		{
			ret = Utils.InitReturns();
			IDbConnection con_db = null;
			StringBuilder sql = new StringBuilder();
			try
			{
				if (flag)
				{
					#region начисление пени

					#region Открываем соединение с базой
					con_db = GetConnection(Constants.cons_Kernel);
					ret = OpenDb(con_db, true);
					if (!ret.result)
					{
						MonitorLog.WriteLog("Ошибка при открытии соединения с БД в PennyOperations", MonitorLog.typelog.Error, true);
						ret.result = false;
						return;
					}
					#endregion

					sql.Remove(0, sql.Length);
					sql.Append(" SELECT COUNT(*) ");
					sql.Append("FROM " + pref + "_data.tarif ");
					sql.Append("WHERE ");
					sql.Append("nzp_kvar = " + nzp_kvar + " ");
					sql.Append("AND nzp_serv = 500 ");
					sql.Append("AND is_actual <> 100 ");
					sql.Append("AND p.dat_s <= '" + date.Year + "-" + date.Month.ToString("00") + "-" + DateTime.DaysInMonth(date.Year, date.Month).ToString("00") + "' ");
					sql.Append("AND p.dat_po >= '" + date.Year + "-" + date.Month.ToString("00") + "-01'" + ";");

					object res = ExecScalar(con_db, sql.ToString(), out ret, true);
					int count = 0;
					if (ret.result && res != null && res != DBNull.Value)
						count = Convert.ToInt32(res);

					if (count == 0)
					{
						sql.Remove(0, sql.Length);
						sql.Append("INSERT INTO " + pref + "_data.tarif ");
						sql.Append("(nzp_kvar, num_ls, nzp_serv, nzp_supp, nzp_frm, ");
						sql.Append("dat_s, dat_po, is_actual, nzp_user, month_calc) ");
						sql.Append("VALUES ");
						sql.Append(nzp_kvar + ",");
						sql.Append(nzp_kvar + ",");
						sql.Append("500,");
                        sql.Append("(SELECT set.nzp_supp FROM settings_requisites set, " + pref + "_data.kvar kv WHERE kv.nzp_kvar = " + nzp_kvar + " AND kv.nzp_area = set.nzp_area),");
						sql.Append("500,");
						sql.Append("'" + date.Year + "-" + date.Month.ToString("00") + "-" + "-01' ,");
                        sql.Append("'3000-01-01' ,");
                        sql.Append("1,");
                        sql.Append(nzp_user + ",");
                        sql.Append("'01-" + Points.CalcMonth.month_.ToString("00") + "-" + Points.CalcMonth.year_ +"'");
						ret = ExecSQL(con_db, sql.ToString(), true);
					}

					#endregion
				}
				else
				{
					#region удаление пени

					#region Открываем соединение с базой
					con_db = GetConnection(Constants.cons_Kernel);

					ret = OpenDb(con_db, true);
					if (!ret.result)
					{
						MonitorLog.WriteLog("Ошибка при открытии соединения с БД в PennyOperations", MonitorLog.typelog.Error, true);
						return;
					}
					#endregion

					sql.Remove(0, sql.Length);
					sql.Append("UPDATE " + pref + "_data.tarif ");
					sql.Append("SET dat_po = " + date.ToString("yyyy-MM-dd") + ", ");
					sql.Append("is_actual = 100 ");
					sql.Append("WHERE ");
					sql.Append("nzp_kvar = " + nzp_kvar + " ");
					sql.Append("AND nzp_serv = 500 ");
					sql.Append("AND is_actual <> 100 ");
					sql.Append("AND p.dat_s <= '" + date.Year + "-" + date.Month.ToString("00") + "-" + DateTime.DaysInMonth(date.Year, date.Month).ToString("00") + "' ");
					sql.Append("AND p.dat_po >= '" + date.Year + "-" + date.Month.ToString("00") + "-01'" + ";");
					ret = ExecSQL(con_db, sql.ToString(), true);

					#endregion
				}
			}
			catch (Exception ex)
			{
				MonitorLog.WriteLog("Ошибка в процедуре PennyOperations: " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
				ret.result = false;
			}
			finally
			{
				if (con_db != null)
					con_db.Close();
			}
		}
	}
}
