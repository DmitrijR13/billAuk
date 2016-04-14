using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using STCLINE.KP50.Global;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using STCLINE.KP50.Interfaces;
using System.IO;
using SevenZip;
using System.Web;
using System.Data.OleDb;
using System.Data.Common;
using Bars.KP50.Utils;

namespace STCLINE.KP50.DataBase
{
	using System.Linq;
	using System.Text.RegularExpressions;

	public partial class DbAdres : DbAdresKernel
	{
#if PG
		private readonly string pgDefaultDb = "public";
#else
#endif

		public Returns SaveUlica(Ulica finder)
		{
			if (finder.pref == "") finder.pref = Points.Pref;

			if (finder.nzp_user < 1) return new Returns(false, "Не задан пользователь");
			if (finder.nzp_raj < 1) return new Returns(false, "Не задан район");
			if (finder.ulica.Trim() == "") return new Returns(false, "Не задано наименование улицы");
			if ((finder.nzp_ul < 1) && (finder.pref != Points.Pref)) return new Returns(false, "Не задан код улицы для локального банка");

			if (finder.ulica.IndexOf("'") != -1) return new Returns(false, "Кавычки в названии улицы недопустимы");
			if (finder.ulica.IndexOf("\"") != -1) return new Returns(false, "Кавычки в названии улицы недопустимы");

			finder.ulica = finder.ulica.Trim().ToUpper();
			finder.ulicareg = finder.ulicareg.Trim().ToUpper();

			#region подключение к базе
			string conn_kernel = Points.GetConnByPref(Points.Pref);
			IDbConnection conn_db = GetConnection(conn_kernel);
			Returns ret = OpenDb(conn_db, true);
			if (!ret.result) return ret;
			#endregion

#if PG
			string table = finder.pref + "_data.s_ulica";
#else
			string table = finder.pref + "_data:s_ulica";
#endif
			string sql;

			#region проверки
			if (finder.pref == Points.Pref)
			{
				sql = "select nzp_ul from " + table + " where Upper(ulica) = '" + finder.ulica + "' and nzp_raj = " + finder.nzp_raj;
				if (finder.nzp_ul > 0) sql += "and nzp_ul <> " + finder.nzp_ul;

				IDataReader reader;
				ret = ExecRead(conn_db, out reader, sql, true);
				if (!ret.result)
				{
					conn_db.Close();
					return ret;
				}
				if (reader.Read())
				{
					CloseReader(ref reader);
					conn_db.Close();
					return new Returns(false, "Уже есть улица с таким именем.");
				}
			}
			#endregion

			bool isNew;
			if (finder.nzp_ul > 0)
			{
				isNew = false;
				sql = "select nzp_ul from " + table + " where nzp_ul = " + finder.nzp_ul;
				IDataReader reader;
				ret = ExecRead(conn_db, out reader, sql, true);
				if (!ret.result)
				{
					conn_db.Close();
					return ret;
				}
				if (reader.Read())
				{
					sql = "Update " + table +
						" Set ulica = " + Utils.EStrNull(finder.ulica) + "," +
						" ulicareg = " + Utils.EStrNull(finder.ulicareg) +
						" Where nzp_ul = " + finder.nzp_ul;
				}
				else
				{
					sql = "insert into " + table + " (nzp_ul, nzp_raj, ulica, ulicareg ) values (" + finder.nzp_ul + "," + finder.nzp_raj + ", '" + finder.ulica + "', '" + finder.ulicareg + "')";
				}
				CloseReader(ref reader);
			}
			else
			{
				isNew = true;
				DbSprav db = new DbSprav();
				ret = db.GetNewId(conn_db, Series.Types.Ulica);
				if (!ret.result)
				{
					conn_db.Close();
					return ret;
				}

				finder.nzp_ul = ret.tag;

				sql = "insert into " + table + " (nzp_ul, nzp_raj, ulica, ulicareg) values (" + finder.nzp_ul + "," + finder.nzp_raj + ",'" + finder.ulica + "', '" + finder.ulicareg + "')";
			}

			ret = ExecSQL(conn_db, sql, true);

			if (ret.result)
			{
				ret.tag = finder.nzp_ul;
			}

			if ((finder.pref == Points.Pref) && !isNew)
			{
				foreach (_Point zap in Points.PointList)
				{
#if PG
					sql = "Update " + zap.pref + "_data.s_ulica" +
											" Set ulica = " + Utils.EStrNull(finder.ulica) + "," +
											" ulicareg = " + Utils.EStrNull(finder.ulicareg) +
											" Where nzp_ul = " + finder.nzp_ul;
#else
					sql = "Update " + zap.pref + "_data:s_ulica" +
											" Set ulica = " + Utils.EStrNull(finder.ulica) + "," +
											" ulicareg = " + Utils.EStrNull(finder.ulicareg) +
											" Where nzp_ul = " + finder.nzp_ul;
#endif
					ExecSQL(conn_db, sql, false);
				}
			}

			conn_db.Close();

			return ret;
		}

		//----------------------------------------------------------------------
		public int Update(Dom dom, out Returns ret) //исправить данные дома
		//----------------------------------------------------------------------
		{
			string conn_kernel = Points.GetConnByPref(dom.pref);
			IDbConnection conn_db = GetConnection(conn_kernel);
			ret = OpenDb(conn_db, true);
			if (!ret.result)
			{
				return 0;
			}
			int nzp_dom = Update(conn_db, dom, out ret);
			conn_db.Close();
			return nzp_dom;
		}
		public int Update(IDbConnection conn_db, Dom dom, out Returns ret)
		{
			ret = Utils.InitReturns();

			if (dom.nkor.Trim() == "") dom.nkor = "-";

			DbTables tables = new DbTables(conn_db);

			#region получить r.nzp_raj, t.nzp_town, s.nzp_stat, l.nzp_land
			IDataReader reader;
			string sql = "select r.nzp_raj, t.nzp_town, s.nzp_stat, l.nzp_land " +
						 " from " + tables.ulica + " u " +
						 " inner join " + tables.rajon + " r on u.nzp_raj = r.nzp_raj " +
						 " inner join " + tables.town + " t on t.nzp_town = r.nzp_town " +
						 " inner join " + tables.stat + " s on s.nzp_stat = t.nzp_stat " +
						 " inner join " + tables.land + " l on l.nzp_land = s.nzp_land " +
						 " where u.nzp_ul = " + dom.nzp_ul;
			if (!ExecRead(conn_db, out reader, sql, true).result)
			{
				ret.result = false;
				return 0;
			}
			try
			{
				if (reader.Read())
				{
					//if (reader["nzp_raj"] != DBNull.Value) dom.nzp_raj = (int)reader["nzp_raj"];
					if (reader["nzp_town"] != DBNull.Value) dom.nzp_town = (int)reader["nzp_town"];
					if (reader["nzp_stat"] != DBNull.Value) dom.nzp_stat = (int)reader["nzp_stat"];
					if (reader["nzp_land"] != DBNull.Value) dom.nzp_land = (int)reader["nzp_land"];
				}
				reader.Close();
			}
			catch (Exception ex)
			{
				reader.Close();

				ret.result = false;
				ret.text = ex.Message;

				string err;
				if (Constants.Viewerror)
					err = " \n " + ex.Message;
				else
					err = "";

				MonitorLog.WriteLog("Ошибка Update домов " + err, MonitorLog.typelog.Error, 20, 201, true);

				return 0;
			}
			#endregion

			#region проверка на существование дома
#if PG
			sql = "select count(*) as num from " + dom.pref + "_data.dom " +
								" where idom   =" + Utils.GetInt(dom.ndom) +
									" and ndom     =" + Utils.EStrNull(dom.ndom.Trim()) +
									" and nkor     =" + Utils.EStrNull(dom.nkor.Trim()) +
									" and nzp_ul   =" + dom.nzp_ul;
#else
			sql = "select count(*) as num from " + dom.pref + "_data:dom " +
								" where idom   =" + Utils.GetInt(dom.ndom) +
									" and ndom     =" + Utils.EStrNull(dom.ndom.Trim()) +
									" and nkor     =" + Utils.EStrNull(dom.nkor.Trim()) +
									" and nzp_ul   =" + dom.nzp_ul;
#endif
			if (dom.nzp_dom > Constants._ZERO_) sql += " and nzp_dom <> " + dom.nzp_dom;
			int num = 0;
			if (!ExecRead(conn_db, out reader, sql, true).result)
			{
				ret.result = false;
				return 0;
			}
			try
			{
				if (reader.Read())
				{
					if (reader["num"] != DBNull.Value) num = Convert.ToInt32(reader["num"]);
				}
				reader.Close();
				if (num > 0)
				{
					ret.result = false;
					ret.tag = 1;
					ret.text = "Дом с таким номером уже существует";
					return 0;
				}
			}
			catch (Exception ex)
			{
				reader.Close();

				ret.result = false;
				ret.text = ex.Message;

				string err;
				if (Constants.Viewerror)
					err = " \n " + ex.Message;
				else
					err = "";

				MonitorLog.WriteLog("Ошибка Update домов " + err, MonitorLog.typelog.Error, 20, 201, true);

				return 0;
			}
			#endregion

			bool edit = false;
			#region если редактирование
			if (dom.nzp_dom != Constants._ZERO_)
			{
				edit = true;

				if (dom.clear_remark) { dom.remark = ""; };
				sql = "select nzp_dom from " + tables.s_remark + " where nzp_dom=" + dom.nzp_dom + "";
				if (!ExecRead(conn_db, out reader, sql, true).result)
				{
					ret.result = false;
					return 0;
				}
				try
				{
					if (reader.Read())
					{
						sql = "update " + tables.s_remark + " " + "set (nzp_area, nzp_geu, nzp_dom, remark) = (0, 0, " + dom.nzp_dom + ", "
							  + Utils.EStrNull(dom.remark) + ") " + "where nzp_dom = " + dom.nzp_dom + "";
						ret = ExecSQL(conn_db, sql, true);
						if (!ret.result)
						{
							ret.text = "Ошибка записи данных дома";
							return 0;
						}
					}
					else
					{
						sql = " insert into " + tables.s_remark + "" + " (nzp_area, nzp_geu, nzp_dom, remark)" + " values (0, 0, " + dom.nzp_dom
							  + ", " + Utils.EStrNull(dom.remark) + ")";
						ret = ExecSQL(conn_db, sql, true);
						if (!ret.result)
						{
							ret.text = "Ошибка записи данных дома";
							return 0;
						}
					}

				}
				catch (Exception ex)
				{
					MonitorLog.WriteException("Ошибка функции Update ", ex);
					reader.Close();
					return 0;
				}
				reader.Close();

				if (num > 0)
				{
					ret.result = false;
					ret.tag = 1;
					ret.text = "Дом с таким номером уже существует";
					return 0;
				}

				#region Добавление в sys_events события 'Изменение адреса дома'
				try
				{
					string ndom = "";
					string nkor = "";
					var nzp_ul = 0;
					var nzp_raj = 0;
					var nzp_town = 0;
					var nzp_area = 0;
					var nzp_geu = 0;

					sql = " select * from " + dom.pref + "_data" + tableDelimiter + "dom d where d.nzp_dom = " + dom.nzp_dom;
					ExecRead(conn_db, out reader, sql, true);

					if (reader.Read())
					{
						if (reader["ndom"] != DBNull.Value) ndom = Convert.ToString(reader["ndom"]).Trim();
						if (reader["nkor"] != DBNull.Value) nkor = Convert.ToString(reader["nkor"]).Trim();
						if (reader["nzp_ul"] != DBNull.Value) nzp_ul = Convert.ToInt32(reader["nzp_ul"]);
						if (reader["nzp_raj"] != DBNull.Value) nzp_raj = Convert.ToInt32(reader["nzp_raj"]);
						if (reader["nzp_town"] != DBNull.Value) nzp_town = Convert.ToInt32(reader["nzp_town"]);
						if (reader["nzp_area"] != DBNull.Value) nzp_area = Convert.ToInt32(reader["nzp_area"]);
						if (reader["nzp_geu"] != DBNull.Value) nzp_geu = Convert.ToInt32(reader["nzp_geu"]);
					}
					reader.Close();

					string note = "";
					//если изменился адрес
					if (ndom.Trim() != dom.ndom)
					{
						note += "Номер дома был изменен с " + ndom + " на " + dom.ndom + ". ";
					}
					if (nkor.Trim() != dom.nkor)
					{
						note += "Корпус был изменен с " + nkor + " на " + dom.nkor + ". ";
					}
					if (nzp_ul != dom.nzp_ul)
					{
						var var_old = "пусто";
						var var_new = "пусто";
						if (dom.nzp_ul != 0 && dom.nzp_ul != -1)
						{
							var r = ExecScalar(conn_db, "select ulica from " + Points.Pref + "_data" + tableDelimiter + "s_ulica where nzp_ul = " + dom.nzp_ul, out ret, true);
							if (r != null)
								var_old = r.ToString().Trim();
						}
						if (nzp_ul != 0)
						{
							var r = ExecScalar(conn_db, "select ulica from " + Points.Pref + "_data" + tableDelimiter + "s_ulica where nzp_ul = " + nzp_ul, out ret, true);
							if (r != null)
								var_new = r.ToString().Trim();
						}
						note += "Улица с " + var_old + " на " + var_new + ". ";
					}
					if (nzp_raj != dom.nzp_raj)
					{
						var var_old = "пусто";
						var var_new = "пусто";
						if (dom.nzp_raj != 0 && dom.nzp_raj != -1)
						{
							var r = ExecScalar(conn_db, "select rajon from " + Points.Pref + "_data" + tableDelimiter + "s_rajon where nzp_raj = " + dom.nzp_raj, out ret, true);
							if (r != null)
								var_old = r.ToString().Trim();
						}
						if (nzp_raj != 0)
						{
							var r = ExecScalar(conn_db, "select rajon from " + Points.Pref + "_data" + tableDelimiter + "s_rajon where nzp_raj = " + nzp_raj, out ret, true);
							if (r != null)
								var_new = r.ToString().Trim();
						}
						note += "Район с " + var_old + " на " + var_new + ". ";
					}
					if (nzp_town != dom.nzp_town)
					{
						var var_old = "пусто";
						var var_new = "пусто";
						if (dom.nzp_town != 0 && dom.nzp_town != -1)
						{
							var r = ExecScalar(conn_db, "select town from " + Points.Pref + "_data" + tableDelimiter + "s_town where nzp_town = " + dom.nzp_town, out ret, true);
							if (r != null)
								var_old = r.ToString().Trim();
						}
						if (nzp_town != 0)
						{
							var r = ExecScalar(conn_db, "select town from " + Points.Pref + "_data" + tableDelimiter + "s_town where nzp_town = " + nzp_town, out ret, true);
							if (r != null)
								var_new = r.ToString().Trim();
						}
						note += "Город с " + var_old + " на " + var_new + ". ";
					}
					if (nzp_area != dom.nzp_area)
					{
						var var_old = "пусто";
						var var_new = "пусто";
						if (dom.nzp_area != 0 && dom.nzp_area != -1)
						{
							var r = ExecScalar(conn_db, "select area from " + Points.Pref + "_data" + tableDelimiter + "s_area where nzp_area = " + dom.nzp_area, out ret, true);
							if (r != null)
								var_old = r.ToString().Trim();
						}
						if (nzp_area != 0)
						{
							var r = ExecScalar(conn_db, "select area from " + Points.Pref + "_data" + tableDelimiter + "s_area where nzp_area = " + nzp_area, out ret, true);
							if (r != null)
								var_new = r.ToString().Trim();
						}
						note += "Управляющая организация с " + var_old + " на " + var_new + ". ";
					}
					if (nzp_geu != dom.nzp_geu)
					{
						var var_old = "пусто";
						var var_new = "пусто";
						if (dom.nzp_geu != 0 && dom.nzp_geu != -1)
						{
							var r = ExecScalar(conn_db, "select geu from " + Points.Pref + "_data" + tableDelimiter + "s_geu where nzp_geu = " + dom.nzp_geu, out ret, true);
							if (r != null)
								var_old = r.ToString().Trim();
						}
						if (nzp_geu != 0)
						{
							var r = ExecScalar(conn_db, "select geu from " + Points.Pref + "_data" + tableDelimiter + "s_geu where nzp_geu = " + nzp_geu, out ret, true);
							if (r != null)
								var_new = r.ToString().Trim();
						}
						note += "ЖЭУ с " + var_old + " на " + var_new + ". ";
					}

					if (note != "")
					{
						DbAdmin.InsertSysEvent(new SysEvents()
						{
							pref = dom.pref,
							nzp_user = dom.nzp_user,
							nzp_dict = 8215,
							nzp_obj = dom.nzp_dom,
							note = note
						}, conn_db);
					}
				}
				catch (Exception ex)
				{
					MonitorLog.WriteLog("Ошибка добавления события в sys_events" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
				}
				#endregion

#if PG
				sql =
									" Update " + dom.pref + "_data.dom " +
									" Set idom     =" + Utils.GetInt(dom.ndom) +
										",ndom     =" + Utils.EStrNull(dom.ndom) +
										",nkor     =" + Utils.EStrNull(dom.nkor) +
										",nzp_area =" + dom.nzp_area +
										",nzp_geu  =" + dom.nzp_geu +
										",nzp_ul   =" + dom.nzp_ul +
										",nzp_land =" + dom.nzp_land +
										",nzp_stat =" + dom.nzp_stat +
										",nzp_town =" + dom.nzp_town +
										",nzp_raj  =" + dom.nzp_raj +
									" Where nzp_dom= " + dom.nzp_dom;
#else
				sql =
									" Update " + dom.pref + "_data:dom " +
									" Set idom     =" + Utils.GetInt(dom.ndom) +
										",ndom     =" + Utils.EStrNull(dom.ndom) +
										",nkor     =" + Utils.EStrNull(dom.nkor) +
										",nzp_area =" + dom.nzp_area +
										",nzp_geu  =" + dom.nzp_geu +
										",nzp_ul   =" + dom.nzp_ul +
										",nzp_land =" + dom.nzp_land +
										",nzp_stat =" + dom.nzp_stat +
										",nzp_town =" + dom.nzp_town +
										",nzp_raj  =" + dom.nzp_raj +
									" Where nzp_dom= " + dom.nzp_dom;
#endif

				ret = RefreshDom(conn_db, dom);
				if (!ret.result)
				{
					ret.text = "Ошибка записи данных дома";
					return 0;
				}
			}
			#endregion
			#region добавление
			else
			{
				Series series = new Series(new int[] { 3 });
				DbSprav db = new DbSprav();
				db.GetSeries(dom.pref, series, out ret);
				db.Close();
				if (!ret.result)
				{
					ret.text = "Ошибка получения ключей: " + ret.text;
					return 0;
				}

				bool b = false;
				_Series val = series.GetSeries(3);
				if (val.cur_val != Constants._ZERO_) dom.nzp_dom = val.cur_val; else b = true;

				if (b)
				{
					ret.text = "Внутренняя ошибка получения ключей: " + ret.text;
					ret.result = false;
					return 0;
				}

				#region Добавление в sys_events события 'Добавление дома'
				try
				{
					string ndom = "";
					string nkor = "";
					string ulica = "";
					string rajon = "";
					string town = "";

					sql = "  select '" + dom.ndom + "' as ndom, '" + dom.nkor + "' as nkor, * " +
						" from " + dom.pref + "_data" + tableDelimiter + "s_ulica ul " +
						" left join " + dom.pref + "_data" + tableDelimiter + "s_town t on t.nzp_town = " + dom.nzp_town +
						" left join " + dom.pref + "_data" + tableDelimiter + "s_rajon r on r.nzp_raj = " + dom.nzp_raj +
						" where ul.nzp_ul = " + dom.nzp_ul;
					ExecRead(conn_db, out reader, sql, true);

					if (reader.Read())
					{
						ndom = reader["ndom"] != DBNull.Value ? Convert.ToString(reader["ndom"]).Trim() + " " : "";
						nkor = reader["nkor"] != DBNull.Value ? Convert.ToString(reader["nkor"]).Trim() + " " : "";
						ulica = reader["ulica"] != DBNull.Value ? Convert.ToString(reader["ulica"]).Trim() + ", " : "";
						rajon = reader["rajon"] != DBNull.Value ? Convert.ToString(reader["rajon"]).Trim() + ", " : "";
						town = reader["town"] != DBNull.Value ? Convert.ToString(reader["town"]).Trim() + ", " : "";
					}
					reader.Close();

					DbAdmin.InsertSysEvent(new SysEvents()
					{
						pref = dom.pref,
						nzp_user = dom.nzp_user,
						nzp_dict = 6484,
						nzp_obj = dom.nzp_dom,
						note = "Был добавлен дом по адресу " + town + rajon + ulica + ndom + nkor
					}, conn_db);
				}
				catch (Exception ex)
				{
					MonitorLog.WriteLog("Ошибка добавления события в sys_events" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
				}
				#endregion

				sql =
			   " Insert into " + tables.dom +
			   " (nzp_dom,nzp_area,nzp_geu,nzp_ul,idom,ndom,nkor,nzp_land,nzp_stat,nzp_town,nzp_raj) " +
			   " Values (" + dom.nzp_dom + "," + dom.nzp_area + "," + dom.nzp_geu + "," + dom.nzp_ul + "," +
			   Utils.GetInt(dom.ndom) + "," + Utils.EStrNull(dom.ndom) + "," + Utils.EStrNull(dom.nkor) + "," +
			   dom.nzp_land + "," + dom.nzp_stat + "," + dom.nzp_town + "," + dom.nzp_raj + ")"; ;
			   ret = ExecSQL(conn_db, sql, true);
			   if (!ret.result)
			   {
				   ret.text = "Ошибка записи данных дома в центральный банк";
				   return 0;
			   }
#if PG
				sql = " Insert into " + dom.pref + "_data.dom (nzp_dom,nzp_area,nzp_geu,nzp_ul,idom,ndom,nkor,nzp_land,nzp_stat,nzp_town,nzp_raj) "
					  + " Values (" + dom.nzp_dom + "," + dom.nzp_area + "," + dom.nzp_geu + "," + dom.nzp_ul + "," + Utils.GetInt(dom.ndom) + ","
					  + Utils.EStrNull(dom.ndom) + "," + Utils.EStrNull(dom.nkor) + "," + dom.nzp_land + "," + dom.nzp_stat + "," + dom.nzp_town + ","
					  + dom.nzp_raj + ")";
#else
				sql =
									" Insert into " + dom.pref + "_data:dom (nzp_dom,nzp_area,nzp_geu,nzp_ul,idom,ndom,nkor,nzp_land,nzp_stat,nzp_town,nzp_raj) " +
									" Values (" + dom.nzp_dom + "," + dom.nzp_area + "," + dom.nzp_geu + "," + dom.nzp_ul + "," +
											   Utils.GetInt(dom.ndom) + "," + Utils.EStrNull(dom.ndom) + "," + Utils.EStrNull(dom.nkor) + "," +
											   dom.nzp_land + "," + dom.nzp_stat + "," + dom.nzp_town + "," + dom.nzp_raj + ")";
#endif
			}
			#endregion
			ret = ExecSQL(conn_db, sql, true);
			if (!ret.result)
			{
				ret.text = "Ошибка записи данных дома";
				return 0;
			}

			if (dom.clear_remark) { dom.remark = ""; };
			sql = "select nzp_dom from " + tables.s_remark + " where nzp_dom=" + dom.nzp_dom + "";
			if (!ExecRead(conn_db, out reader, sql, true).result)
			{
				ret.result = false;
				return 0;
			}
			try
			{
				if (reader.Read())
				{
					sql = "update " + tables.s_remark + " " +
					"set (nzp_area, nzp_geu, nzp_dom, remark) = (0, 0, " + dom.nzp_dom + ", " + Utils.EStrNull(dom.remark) + ") " +
					"where nzp_dom = " + dom.nzp_dom + "";
					ret = ExecSQL(conn_db, sql, true);
					if (!ret.result)
					{
						ret.text = "Ошибка записи данных дома";
						return 0;
					}
				}
				else
				{
					sql = " insert into " + tables.s_remark + "" +
					" (nzp_area, nzp_geu, nzp_dom, remark)" +
					" values (0, 0, " + dom.nzp_dom + ", " + Utils.EStrNull(dom.remark) + ")";
					ret = ExecSQL(conn_db, sql, true);
					if (!ret.result)
					{
						ret.text = "Ошибка записи данных дома";
						return 0;
					}
				}

			}
			catch (Exception ex)
			{
				MonitorLog.WriteException("Ошибка функции Update ", ex);
				reader.Close();
				return 0;
			}
			reader.Close();


			ret = RefreshDom(conn_db, dom);
			if (!ret.result)
			{
				ret.text = "Ошибка записи данных дома";
				return 0;
			}

			if (edit)
			{
				#region обновление данных в выбранном списке домов
				IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
				ret = OpenDb(conn_web, true);
				if (!ret.result)
				{
					return Constants._ZERO_;
				}

				string tXX_spdom = "t" + Convert.ToString(dom.nzp_user) + "_spdom";

				if (TableInWebCashe(conn_web, tXX_spdom))
				{
					Ls ls = new Ls();
					ls.nzp_dom = dom.nzp_dom;
					ls.pref = dom.pref;
					ls.nzp_user = dom.nzp_user;


					List<Ls> list = LoadLs(ls, out ret);
					if (!ret.result)
					{
						conn_web.Close();
						return Constants._ZERO_;
					}

					sql = "update " + tXX_spdom + " set idom     =" + Utils.GetInt(list[0].ndom) +
						",ndom     =  'дом " + list[0].ndom + " корп. " + list[0].nkor + "'" +
						",nzp_area =" + list[0].nzp_area +
						",nzp_geu  =" + list[0].nzp_geu +
						",area = '" + dom.area + "'" +
						",geu = '" + dom.geu + "'" +
						",nzp_ul   =" + list[0].nzp_ul +
						",ulica ='" + list[0].ulicareg + " " + list[0].ulica + " / " + list[0].rajon + "'"
						+ " where nzp_dom = " + ls.nzp_dom + " and pref = '" + ls.pref + "'";
					ret = ExecSQL(conn_web, sql, true);
					if (!ret.result)
					{
						conn_web.Close();
						return Constants._ZERO_;
					}

					#region обновление данных в выбранном списке л/с
					string tXX_spls = "t" + Convert.ToString(ls.nzp_user) + "_spls";

					if (TableInWebCashe(conn_web, tXX_spls))
					{
						sql = "select nzp_kvar, pref from " + tXX_spls + " where nzp_dom = " + ls.nzp_dom + " and pref = '" + ls.pref + "'";
						if (!ExecRead(conn_web, out reader, sql, true).result)
						{
							ret.result = false;
							conn_web.Close();
							return Constants._ZERO_;
						}
						try
						{
							List<Ls> list2;
							Ls ls2;
							while (reader.Read())
							{
								ls2 = new Ls();
								if (reader["nzp_kvar"] != DBNull.Value) ls2.nzp_kvar = (int)reader["nzp_kvar"];
								if (reader["pref"] != DBNull.Value) ls2.pref = (string)reader["pref"];
								ls2.nzp_user = dom.nzp_user;
								list2 = LoadLs(ls2, out ret);
								if (!ret.result)
								{
									reader.Close();
									conn_web.Close();
									return Constants._ZERO_;
								}

								sql = "update " + tXX_spls + " set nzp_area = " + list2[0].nzp_area + ", nzp_geu = " + list2[0].nzp_geu + ", fio = '" + list2[0].fio +
									"', nkvar = '" + list2[0].nkvar + " " + list2[0].nkvar_n + "', adr ='" + list2[0].adr + "'"
									+ " where nzp_kvar = " + ls2.nzp_kvar + " and pref = '" + ls2.pref + "'";
								ret = ExecSQL(conn_web, sql, true);
								if (!ret.result)
								{
									reader.Close();
									conn_web.Close();
									return Constants._ZERO_;
								}
							}
							reader.Close();
						}
						catch (Exception ex)
						{
							reader.Close();

							ret.result = false;
							ret.text = ex.Message;

							string err;
							if (Constants.Viewerror)
								err = " \n " + ex.Message;
							else
								err = "";

							MonitorLog.WriteLog("Ошибка Update домов " + err, MonitorLog.typelog.Error, 20, 201, true);

							return 0;
						}

					}
					#endregion

				}
				#endregion
			}
			else
			{
				#region обновление данных в выбранном списке домов
				IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
				ret = OpenDb(conn_web, true);
				if (!ret.result)
				{
					return Constants._ZERO_;
				}

				string tXX_spdom = "t" + Convert.ToString(dom.nzp_user) + "_spdom";

				if (TableInWebCashe(conn_web, tXX_spdom))
				{
					Ls ls = new Ls();
					ls.nzp_dom = dom.nzp_dom;
					ls.pref = dom.pref;
					ls.nzp_user = dom.nzp_user;


					List<Ls> list = LoadLs(ls, out ret);
					if (!ret.result)
					{
						conn_web.Close();
						return Constants._ZERO_;
					}

					int nzp_wp = 0;
					string point = "";
					foreach (_Point zap in Points.PointList)
					{
						if (zap.pref != dom.pref) continue;

						nzp_wp = zap.nzp_wp;
						point = zap.point;

						break;
					}
					if (list != null && list.Count > 0)
					{
						sql = "insert into " + tXX_spdom + " (nzp_dom, nzp_ul, nzp_area, nzp_geu, nzp_wp, area, geu, ulica, ndom, idom, pref, point) " +
							  " values (" + dom.nzp_dom + "," + dom.nzp_ul + "," + dom.nzp_area + "," + dom.nzp_geu + "," + nzp_wp + ",'" + list[0].area + "','" +
										  list[0].geu + "','" + list[0].ulica + "','дом " + dom.ndom + " корп. " + dom.nkor.Trim() + "'," + Utils.GetInt(dom.ndom) + ",'" + dom.pref + "','" + point + "')";
						ret = ExecSQL(conn_web, sql, true);
						if (!ret.result)
						{
							conn_web.Close();
							return Constants._ZERO_;
						}
					}
				}
				#endregion
			}

			return dom.nzp_dom;
		}
		//----------------------------------------------------------------------
		public int Update(Ls kvar, out Returns ret) //исправить данные квартиры
		//----------------------------------------------------------------------
		{
			ret = Utils.InitReturns();
			int rez = Constants._ZERO_;

			#region Проверка входных параметров
			if (kvar.nzp_user < 1)
			{
				ret.result = false;
				ret.tag = -1;
				ret.text = "Не определен пользователь";
				return rez;
			}
			if (kvar.pref == "")
			{
				ret.result = false;
				ret.tag = -2;
				ret.text = "Не определен префикс";
				return rez;
			}
			#endregion

			#region Подключение к БД
			string connectionString = Points.GetConnByPref(kvar.pref);
			IDbConnection conn_db = GetConnection(connectionString);
			ret = OpenDb(conn_db, true);
			if (!ret.result) return Constants._ZERO_;

			IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
			ret = OpenDb(conn_web, true);
			if (!ret.result)
			{
				conn_db.Close();
				return Constants._ZERO_;
			}
			#endregion

			rez = Update(conn_db, conn_web, kvar, out ret);

			if (!ret.result)
			{
				conn_web.Close();
				conn_db.Close();
				return rez;
			}
			conn_web.Close();
			conn_db.Close();
			return rez;
		}
		//----------------------------------------------------------------------
		public int Update(IDbConnection conn_db, IDbConnection conn_web, Ls kvar, out Returns ret) //исправить данные квартиры
		//----------------------------------------------------------------------
		{
			ret = Utils.InitReturns();
			int rez = Constants._ZERO_;

			IDbTransaction transaction;
			try { transaction = conn_db.BeginTransaction(); }
			catch { transaction = null; }

			rez = Update(conn_db, transaction, conn_web, kvar, out ret);

			if (!ret.result)
			{
				if (transaction != null) transaction.Rollback();
				return rez;
			}
			if (transaction != null) transaction.Commit();
			return rez;
		}

		//----------------------------------------------------------------------
		public int Update(IDbConnection conn_db, IDbTransaction transaction, IDbConnection conn_web, Ls kvar, out Returns ret) //исправить данные квартиры
		//---------------------------------------------------------------------
		{
			ret = Utils.InitReturns();//инициализация результата
			string sql = "";
			IDataReader reader;

			#region Проверка существования л/с по определенному адресу при добавлении нового или редактировании л/с
			//если переменная kvar.chekexistls=1, значит функция вызывается в первый раз и надо проверить не
			//существует ли л/с по этому адресу (нужно для того, чтобы выдать предупреждения)
			if (kvar.chekexistls == 1)
			{
				bool b = false; //признак - продолжать проверку на существование л/с
				#region если редактирование, проверить не изменился ли адрес
				if (kvar.nzp_kvar != Constants._ZERO_)
				{
#if PG
					sql = "select ikvar, nkvar, nkvar_n, nzp_dom from " + kvar.pref + "_data.kvar where nzp_kvar = " + kvar.nzp_kvar;
#else
					sql = "select ikvar, nkvar, nkvar_n, nzp_dom from " + kvar.pref + "_data:kvar where nzp_kvar = " + kvar.nzp_kvar;
#endif
					if (!ExecRead(conn_db, transaction, out reader, sql, true).result)
					{
						return Constants._ZERO_;
					}
					try
					{
						int ikvar = 0;
						int nzp_dom = 0;
						string nkvar = "";
						string nkvar_n = "";
						if (reader.Read())
						{
							if (reader["ikvar"] != DBNull.Value) ikvar = Convert.ToInt32(reader["ikvar"]);
							if (reader["nkvar"] != DBNull.Value) nkvar = Convert.ToString(reader["nkvar"]);
							if (reader["nkvar_n"] != DBNull.Value) nkvar_n = Convert.ToString(reader["nkvar_n"]);
							if (reader["nzp_dom"] != DBNull.Value) nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
						}
						reader.Close();
						if (ikvar == Utils.GetInt(kvar.nkvar) &&
							nkvar.Trim() == kvar.nkvar &&
							nkvar_n.Trim() == kvar.nkvar_n &&
							nzp_dom == kvar.nzp_dom) b = false;
						else b = true;
					}
					catch (Exception ex)
					{
						reader.Close();
						ret.result = false;
						ret.text = ex.Message;
						string err;
						if (Constants.Viewerror) err = " \n " + ex.Message;
						else err = "";
						MonitorLog.WriteLog("Ошибка получения информации о лс " + err, MonitorLog.typelog.Error, 20, 201, true);
						return Constants._ZERO_;
					}

				}
				#endregion

				#region если добавление или редактирование (с изменением адреса), то проверка на существования лс по адресу
				if (kvar.nzp_kvar == Constants._ZERO_ || b)
				{
#if PG
					sql = "select count(*) from " + kvar.pref + "_data.kvar " + " where nzp_dom  = " + kvar.nzp_dom + " and ikvar = " + Utils.GetInt(kvar.nkvar);
#else
					sql = "select count(*) from " + kvar.pref + "_data:kvar " + " where nzp_dom  = " + kvar.nzp_dom + " and ikvar = " + Utils.GetInt(kvar.nkvar);
#endif
					if (kvar.nkvar.Trim() == "") sql += " and (nkvar = '' or nkvar = '-' or nkvar is NULL)"; else sql += " and nkvar = " + Utils.EStrNull(kvar.nkvar.Trim());
					if (kvar.nkvar_n.Trim() == "") sql += " and (nkvar_n = '' or nkvar_n = '-' or nkvar_n is NULL)"; else sql += " and nkvar_n = " + Utils.EStrNull(kvar.nkvar_n.Trim());

					IDbCommand cmd = DBManager.newDbCommand(sql, conn_db, transaction);
					try
					{
						string s = Convert.ToString(cmd.ExecuteScalar());
						ret.tag = Convert.ToInt32(s);
						if (ret.tag > 0)
						{
							ret.tag = -11;

							ret.text = "Л/с по этому адресу уже существует. Продолжить сохранение л/с?";

							ret.result = false;
							return Constants._ZERO_;
						}
					}
					catch (Exception ex)
					{
						ret.result = false;
						ret.text = ex.Message;
						string err;
						if (Constants.Viewerror) err = " \n " + ex.Message; else err = "";
						MonitorLog.WriteLog("Ошибка определения количества лс" + err, MonitorLog.typelog.Error, 20, 201, true);
						return Constants._ZERO_;
					}
				}
				#endregion
			}
			#endregion

			#region редактирование л/с
			if (kvar.nzp_kvar != Constants._ZERO_)
			{
				#region Определить пользователя
				DbWorkUser db = new DbWorkUser();
				int nzpUser = db.GetLocalUser(conn_db, transaction, kvar, out ret); //локальный пользователь      
				db.Close();
				if (!ret.result) return Constants._ZERO_;
				#endregion

				#region проверить не заблокирован ли лс, если нет то заблокировать
#if PG
				sql = "select nzp_user, dat_when,  (now() - INTERVAL " + string.Format("'{0} minutes')", Constants.users_min) + " as cur_date from " + kvar.pref +
					  "_data.kvar_block where nzp_kvar = " + kvar.nzp_kvar + " order by dat_when desc";
#else
				sql = "select nzp_user, dat_when,  (current year to second - " + Constants.users_min.ToString() + " units minute) as cur_date from " + kvar.pref +
					  "_data:kvar_block where nzp_kvar = " + kvar.nzp_kvar + " order by dat_when desc";
#endif
				if (!ExecRead(conn_db, transaction, out reader, sql, true).result)
				{
					return Constants._ZERO_;
				}

				try
				{
					DateTime datwhen = DateTime.MinValue;
					DateTime curdate = DateTime.MinValue;
					int nzpuser = 0;

					if (reader.Read())
					{
						if (reader["dat_when"] != DBNull.Value) datwhen = Convert.ToDateTime(reader["dat_when"]);
						if (reader["cur_date"] != DBNull.Value) curdate = Convert.ToDateTime(reader["cur_date"]);
						if (reader["nzp_user"] != DBNull.Value) nzpuser = Convert.ToInt32(reader["nzp_user"]);

						if (nzpuser > 0 && datwhen != DateTime.MinValue) //заблокирован лицевой счет
						{
							if (nzpuser != nzpUser && curdate <= datwhen) //если заблокирована запись другим пользователем и 20 мин не прошло
							{
								ret.result = false;
								ret.text = "Редактировать данные запрещено, поскольку с ними работает другой пользователь";
								ret.tag = -12;
								reader.Close();
								return Constants._ZERO_;
							}
						}


					}
					reader.Close();

					#region Удалить все записи для kvar.nzp_kvar
#if PG
					ret = ExecSQL(conn_db, transaction, "delete from " + kvar.pref + "_data.kvar_block where nzp_kvar = " + kvar.nzp_kvar, true);
#else
					ret = ExecSQL(conn_db, transaction, "delete from " + kvar.pref + "_data:kvar_block where nzp_kvar = " + kvar.nzp_kvar, true);
#endif
					if (!ret.result)
					{
						ret.result = false;
						ret.text = "Ошибка удаления из таблицы kvar_block";
						return Constants._ZERO_;
					}
					#endregion

					#region Заблокировать л/с
#if PG
					ret = ExecSQL(conn_db, transaction, "insert into " + kvar.pref + "_data.kvar_block (nzp_kvar,nzp_user, dat_when,kod) values(" +
														kvar.nzp_kvar + "," + nzpUser + ",now()," + Constants.ist + ")", true);
#else
					ret = ExecSQL(conn_db, transaction, "insert into " + kvar.pref + "_data:kvar_block (nzp_kvar,nzp_user, dat_when,kod) values(" +
														kvar.nzp_kvar + "," + nzpUser + ",current year to second," + Constants.ist + ")", true);
#endif
					if (!ret.result)
					{
						ret.result = false;
						ret.text = "Ошибка добавления записи о блокировке в таблицу kvar_block";
						return Constants._ZERO_;
					}
					#endregion
				}
				catch (Exception ex)
				{
					reader.Close();
					ret.result = false;
					ret.text = ex.Message;
					string err;
					if (Constants.Viewerror) err = " \n " + ex.Message;
					else err = "";
					MonitorLog.WriteLog("Ошибка получения информации о блокировки пользователя " + err, MonitorLog.typelog.Error, 20, 201, true);
					return 0;
				}
				#endregion

				#region Добавление в sys_events события 'Изменение адреса лицевого счёта'  и 'Изменение ФИО ответственного квартиросъемщика'
				var tsql = "select * from " + kvar.pref + "_data" + tableDelimiter + "kvar " +
					" where nzp_kvar = " + kvar.nzp_kvar;

				ExecRead(conn_db, transaction, out reader, tsql, true);
				try
				{
					string nkvar = "";
					string nkvar_n = "";
					string fio = "";
					string phone = "";
					string porch = "";
					string uch = "";
					string remark = "";
					int nzp_area = 0;
					int nzp_geu = 0;
					if (reader.Read())
					{
						if (reader["nkvar"] != DBNull.Value) nkvar = Convert.ToString(reader["nkvar"]).Trim();
						if (reader["nkvar_n"] != DBNull.Value) nkvar_n = Convert.ToString(reader["nkvar_n"]).Trim();
						if (reader["fio"] != DBNull.Value) fio = Convert.ToString(reader["fio"]).Trim();
						if (reader["phone"] != DBNull.Value) phone = Convert.ToString(reader["phone"]).Trim();
						if (reader["remark"] != DBNull.Value) remark = Convert.ToString(reader["remark"]).Trim();
						if (reader["uch"] != DBNull.Value) uch = Convert.ToString(reader["uch"]).Trim();
						if (reader["porch"] != DBNull.Value) porch = Convert.ToString(reader["porch"]).Trim();
						if (reader["nzp_area"] != DBNull.Value) nzp_area = Convert.ToInt32(reader["nzp_area"]);
						if (reader["nzp_geu"] != DBNull.Value) nzp_geu = Convert.ToInt32(reader["nzp_geu"]);
					}
					reader.Close();

					//если изменился адрес
					if (nkvar.Trim() != kvar.nkvar ||
						nkvar_n.Trim() != kvar.nkvar_n)
					{
						#region Добавление в sys_events события 'Изменение адреса лицевого счёта'
						DbAdmin.InsertSysEvent(new SysEvents()
						{
							pref = kvar.pref,
							nzp_user = kvar.nzp_user,
							nzp_dict = 8214,
							nzp_obj = kvar.nzp_kvar,
							note = "Адрес был изменен c кв." + nkvar + ", комн." + nkvar_n + " на кв." + kvar.nkvar + ", комн." + kvar.nkvar_n
						}, transaction, conn_db);
						#endregion
					}
					else if (fio.Trim() != kvar.fio)                   //если изменилось фио                    
					{
						#region Добавление в sys_events события 'Изменение адреса лицевого счёта'
						DbAdmin.InsertSysEvent(new SysEvents()
						{
							pref = kvar.pref,
							nzp_user = kvar.nzp_user,
							nzp_dict = 8216,
							nzp_obj = kvar.nzp_kvar,
							note = "ФИО были изменены с " + (fio != "" ? fio : "пусто") + " на " + kvar.fio
						}, transaction, conn_db);
						#endregion
					}
					else
					{
						#region Добавление в sys_events события 'Изменение адреса лицевого счёта'
						string changed_fields = "";
						if (phone.Trim() != kvar.phone)
						{
							changed_fields += "Телефон с " + (phone.Trim() != "" ? phone.Trim() : "пусто" ) + " на " + kvar.phone + ". ";
						}
						if (remark.Trim() != kvar.remark)
						{
							changed_fields += "Примечание с " + (remark.Trim() != "" ? remark.Trim() : "пусто") + " на " + kvar.remark + ". ";
						}
						if(uch.Trim() != kvar.uch)
						{
							changed_fields += "Участок с " + (uch.Trim() != "" ? uch.Trim() : "пусто") + " на " + kvar.uch + ". ";
						}
						if (porch.Trim() != kvar.porch)
						{
							changed_fields += "Подъезд с " + (porch.Trim() != "" ? porch.Trim() : "пусто") + " на " + kvar.porch + ". ";
						}
						if (nzp_area != kvar.nzp_area)
						{
							var area_old = "пусто";
							var area_new = "пусто";
							if (kvar.nzp_area != 0 && kvar.nzp_area != -1)
							{
								var r = ExecScalar(conn_db, transaction, "select area from " + kvar.pref + "_data" + tableDelimiter + "s_area where nzp_area = " + kvar.nzp_area, out ret, true);
								if (r != null)
									area_old = r.ToString();
							}
							if (nzp_area != 0)
							{
								var r = ExecScalar(conn_db, transaction, "select area from " + kvar.pref + "_data" + tableDelimiter + "s_area where nzp_area = " + nzp_area, out ret, true);
								if (r != null)
									area_old = r.ToString();
							}
							changed_fields += "Управляющая организация с " + area_old + " на " + area_new + ". ";
						}
						if (nzp_geu != kvar.nzp_geu)
						{
							var area_old = "пусто";
							var area_new = "пусто";
							if (kvar.nzp_geu != 0 && kvar.nzp_geu != -1)
							{
								var r = ExecScalar(conn_db, transaction, "select geu from " + kvar.pref + "_data" + tableDelimiter + "s_geu where nzp_geu = " + kvar.nzp_geu, out ret, true);
								if (r != null)
									area_old = r.ToString();
							}
							if (nzp_geu != 0)
							{
								var r = ExecScalar(conn_db, transaction, "select geu from " + kvar.pref + "_data" + tableDelimiter + "s_geu where nzp_geu = " + nzp_geu, out ret, true);
								if (r != null)
									area_old = r.ToString();
							}
							changed_fields += "ЖЭУ с " + area_old + " на " + area_new + ". ";
						}

						if (changed_fields != "")
						{
							DbAdmin.InsertSysEvent(new SysEvents()
							{
								pref = kvar.pref,
								nzp_user = kvar.nzp_user,
								nzp_dict = 6495,
								nzp_obj = kvar.nzp_kvar,
								note = "Были изменены: " + changed_fields
							}, transaction, conn_db);
						}
						#endregion
					}
				}
				catch (Exception ex)
				{
					MonitorLog.WriteLog("Ошибка добавления события в sys_events" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
				}
				#endregion

#if PG
				sql =
									" Update " + kvar.pref + "_data.kvar " +
									" Set  phone = " + Utils.EStrNull(kvar.phone) +
										", porch = " + Utils.EStrNull(kvar.porch) +
										", fio   = " + Utils.EStrNull(kvar.fio) +
										", nkvar = " + Utils.EStrNull(kvar.nkvar) +
										", ikvar = " + Utils.GetInt(kvar.nkvar) +
										", nkvar_n=" + Utils.EStrNull(kvar.nkvar_n) +
										", uch    =" + Utils.EStrNull(kvar.uch) +
										", remark =" + Utils.EStrNull(kvar.remark) +
										", nzp_area = " + kvar.nzp_area +
										", nzp_geu  = " + kvar.nzp_geu +
									" Where nzp_kvar= " + kvar.nzp_kvar;
#else
				sql =
					" Update " + kvar.pref + "_data:kvar " +
					" Set  phone = " + Utils.EStrNull(kvar.phone) +
						", porch = " + Utils.EStrNull(kvar.porch) +
						", fio   = " + Utils.EStrNull(kvar.fio) +
						", nkvar = " + Utils.EStrNull(kvar.nkvar) +
						", ikvar = " + Utils.GetInt(kvar.nkvar) +
						", nkvar_n=" + Utils.EStrNull(kvar.nkvar_n) +
						", uch    =" + Utils.EStrNull(kvar.uch) +
						", remark =" + Utils.EStrNull(kvar.remark) +
						", nzp_area = " + kvar.nzp_area +
						", nzp_geu  = " + kvar.nzp_geu +
					" Where nzp_kvar= " + kvar.nzp_kvar;
#endif
				ret = ExecSQL(conn_db, transaction, sql, true);
				if (!ret.result)
				{
					ret.text = "Ошибка записи данных лицевого счета";
					return Constants._ZERO_;
				}

				ret = RefreshKvar(conn_db, transaction, kvar);
				if (!ret.result)
				{
					ret.text = "Ошибка записи данных лицевого счета";
					return Constants._ZERO_;
				}

#if PG
				sql = "delete from " + kvar.pref + "_data.kvar_block where nzp_kvar = " + kvar.nzp_kvar;
#else
				sql = "delete from " + kvar.pref + "_data:kvar_block where nzp_kvar = " + kvar.nzp_kvar;
#endif
				ret = ExecSQL(conn_db, transaction, sql, true);
				if (!ret.result)
				{
					ret.text = "Ошибка записи данных лицевого счета";
					return Constants._ZERO_;
				}

				#region обновление данных в выбранном списке л/с
				string tXX_spls = "t" + Convert.ToString(kvar.nzp_user) + "_spls";

				if (TableInWebCashe(conn_web, tXX_spls))
				{
					List<Ls> list = LoadLs(conn_db, transaction, kvar, out ret);
					if (!ret.result)
					{
						return Constants._ZERO_;
					}

					if (list != null && list.Count > 0)
					{
						sql = "update " + tXX_spls + " set nzp_area = " + list[0].nzp_area + ", nzp_geu = " + list[0].nzp_geu + ", fio = '" + list[0].fio +
							"', nkvar = '" + list[0].nkvar + " " + list[0].nkvar_n + "', adr ='" + list[0].adr + "', sostls = " + Utils.EStrNull(list[0].state) +
							" where nzp_kvar = " + kvar.nzp_kvar + " and pref = '" + kvar.pref + "'";
						ret = ExecSQL(conn_web, sql, true);
						if (!ret.result)
						{
							return Constants._ZERO_;
						}
					}
				}
				#endregion

			}
			#endregion

			#region добавление л/с
			else
			{
			   
					var series = new Series(new int[] {1, 2});

					var db = new DbSpravKernel();
					db.GetSeries(conn_db, transaction, kvar.pref, series, out ret);
					db.Close();
					if (!ret.result)
					{
						ret.text = "Ошибка получения ключей: " + ret.text;
						return Constants._ZERO_;
					}

					_Series val = series.GetSeries(1);
					kvar.nzp_kvar = (val.cur_val != Constants._ZERO_) ? val.cur_val : 0;

					val = series.GetSeries(2);
					kvar.num_ls = (val.cur_val != Constants._ZERO_) ? val.cur_val : 0;
				
					//int pkod10 = 0;
					////в Самаре для переноса ЛС в другую УК pkod10 из старого ЛС
					//if (Points.IsSmr && kvar.moving)
					//{
					//    pkod10 = kvar.pkod10;
					//}

				int areaCode, pkod10 = (Points.IsSmr && kvar.moving) ? kvar.pkod10 : 0;
				kvar.pkod = GeneratePkodOneLS(
					new Ls()
					{
						nzp_area = kvar.nzp_area,
						nzp_geu = kvar.nzp_geu,
						nzp_kvar = kvar.nzp_kvar,
						pref = kvar.pref
					}, transaction, out areaCode, ref pkod10, out ret);
							   
				if (kvar.pkod == "" || !ret.result)
				{
					MonitorLog.WriteLog("Ошибка генерации платежного кода: " + ret.text, MonitorLog.typelog.Error, true);
					return Constants._ZERO_;
				}
	  
				kvar.pkod10 = pkod10;
				kvar.area_code = areaCode;
			  
				//GeneratePkod(conn_db, transaction, kvar, out ret);
			 
				//if (!ret.result)
				//{
				//    MonitorLog.WriteLog("Ошибка генерации платежного кода: в вызываемой функции GeneratePkod произошла ошибка" + kvar.pkod + "; " + ret.text, MonitorLog.typelog.Error, true);
				//    return Constants._ZERO_;
				//}

				//if (Points.IsSmr && kvar.moving)
				//{
				//    kvar.pkod10 = pkod10;
				//}              

				int ikvar = 0;
				if (kvar.nkvar.Trim().Length > 0)
				{
					if (!int.TryParse(kvar.nkvar.Trim(), out ikvar)) ikvar = 0;
				}
				string dop_field = "", dop_values = "";
				if (kvar.fio != "")
				{
					dop_field += ",fio";
					dop_values += ",'" + kvar.fio + "'";
				}

				
				//int code = GetAreaCodes(conn_db, transaction, kvar, out ret);
				//if (!ret.result)
				//{
				//    MonitorLog.WriteLog("Ошибка получения текущего area_codes.code", MonitorLog.typelog.Error, true);
				//    return Constants._ZERO_;
				//}
				//string cd = "0";
				//if (code > 0) cd = code.ToString();

				DbTables tables = new DbTables(conn_db);
				sql = " Insert into " + tables.kvar +
					  " (nzp_kvar,num_ls,area_code,pkod,pkod10, nkvar,nkvar_n,ikvar,nzp_dom,nzp_area,nzp_geu,typek" +
					  dop_field + ") " +
					  " Values (" + kvar.nzp_kvar + "," + kvar.num_ls + "," + kvar.area_code + "," + kvar.pkod + "," +
					  kvar.pkod10 + "," +
					  (kvar.nkvar.Trim() == "" ? "'-'" : "'" + kvar.nkvar.Trim() + "'") + "," +         // номер квартиры
					  (kvar.nkvar_n.Trim() == "" ? "'-'" : "'" + kvar.nkvar_n.Trim() + "'") + "," + // номер комнаты 
					  ikvar + "," + // числовой номер квартиры
					  kvar.nzp_dom + "," + kvar.nzp_area + "," + kvar.nzp_geu + ',' + kvar.typek + dop_values + ")";
				ret = ExecSQL(conn_db, transaction, sql, true);
				if (!ret.result)
				{
					ret.text = "Ошибка записи данных лицевого счета";
					return Constants._ZERO_;
				}

				sql =" Insert into " + kvar.pref + "_data"+tableDelimiter+"kvar (nzp_kvar,num_ls,pkod,pkod10, nkvar,nkvar_n,ikvar,nzp_dom,nzp_area,nzp_geu,typek" + dop_field + ") " +
					 " Values (" + kvar.nzp_kvar + "," + kvar.num_ls + "," + kvar.pkod + "," + kvar.pkod10 + "," +
								  (kvar.nkvar.Trim() == "" ? "'-'" : "'" + kvar.nkvar.Trim() + "'") + "," + // номер квартиры
								  (kvar.nkvar_n.Trim() == "" ? "'-'" : "'" + kvar.nkvar_n.Trim() + "'") + "," + // номер комнаты 
								  ikvar + "," +  // числовой номер квартиры
								  kvar.nzp_dom + "," + kvar.nzp_area + "," + kvar.nzp_geu + ',' + kvar.typek + dop_values + ")";

				ret = ExecSQL(conn_db, transaction, sql, true);
				if (!ret.result)
				{
					ret.text = "Ошибка записи данных лицевого счета";
					return Constants._ZERO_;
				}

				if (kvar.stateID > 0)
				{
					Param prm = new Param();
					kvar.CopyTo(prm);
					RecordMonth r_m = Points.GetCalcMonth(new CalcMonthParams(kvar.pref));
					prm.dat_s = new DateTime(r_m.year_, r_m.month_, 1).ToShortDateString();
					prm.nzp = kvar.nzp_kvar;
					prm.nzp_prm = 51;
					prm.val_prm = kvar.stateID.ToString();
					prm.prm_num = 3;

					DbParameters dbparam = new DbParameters();
					ret = dbparam.SavePrm(conn_db, transaction, prm);
					dbparam.Close();

					if (!ret.result)
					{
						return Constants._ZERO_;
					}
				}

				ret = RefreshKvar(conn_db, transaction, kvar);
				if (!ret.result)
				{
					ret.text = "Ошибка записи данных лицевого счета";
					return Constants._ZERO_;
				}

				#region Добавление в sys_events события 'Добавление лицевого счёта'
				try
				{
					string area = "";
					var tsql = "select sa.area from " + kvar.pref + "_data" + tableDelimiter + "kvar k  " +
					   " join " + kvar.pref + "_data" + tableDelimiter + "s_area sa on k.nzp_area = sa.nzp_area " +
					   " where nzp_kvar = " + kvar.nzp_kvar;

					ExecRead(conn_db, transaction, out reader, tsql, true);

					if (reader.Read())
					{
						if (reader["area"] != DBNull.Value) area = Convert.ToString(reader["area"]).Trim();
					}
					reader.Close();

					DbAdmin.InsertSysEvent(new SysEvents()
					{
						pref = kvar.pref,
						nzp_user = kvar.nzp_user,
						nzp_dict = 6481,
						nzp_obj = kvar.nzp_kvar,
						note = "УК " + area
					}, transaction, conn_db);
				}
				catch (Exception ex)
				{
					MonitorLog.WriteLog("Ошибка добавления события в sys_events" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
				}
				#endregion

				#region обновление данных в выбранном списке л/с

				string tXX_spls = "t" + Convert.ToString(kvar.nzp_user) + "_spls";

				if (TableInWebCashe(conn_web, tXX_spls) && !kvar.moving) // при переносе ЛС не заходим
				{
					List<Ls> list = LoadLs(conn_db, transaction, kvar, out ret);
					if (!ret.result)
					{
						return Constants._ZERO_;
					}

					if (list != null && list.Count > 0)
					{
						sql = " Insert into " + tXX_spls +
							  " ( nzp_kvar, num_ls, pkod,pkod10, nzp_dom, nzp_area, nzp_geu, nzp_ul, typek, fio, adr, ulica, ndom," +
							  "   idom, nkvar, ikvar, stypek, sostls, pref ) " +
							  " Values(" + kvar.nzp_kvar + "," + kvar.num_ls + "," + kvar.pkod + "," + kvar.pkod10 + "," + kvar.nzp_dom + "," + kvar.nzp_area + "," + kvar.nzp_geu + "," +
									   list[0].nzp_ul + "," + kvar.typek + ",'" + kvar.fio + "','" + list[0].adr + "','" +
									   list[0].ulica + "','" + list[0].ndom + "'," + Utils.GetInt(list[0].ndom) + ",'" +
									   list[0].nkvar + "'," + Utils.GetInt(list[0].nkvar) + ",'" + list[0].stypek + "'," + Utils.EStrNull(list[0].state) + ",'" + kvar.pref + "')";
						ret = ExecSQL(conn_web, sql, true);
						if (!ret.result)
						{
							return Constants._ZERO_;
						}
					}
				}
				#endregion
			}
			#endregion

			return kvar.nzp_kvar;
		}
   

		/// <summary>
		/// Добавить ЛС в tXX_spls
		/// </summary>
		/// <param name="conn_db"></param>
		/// <param name="transaction"></param>
		/// <param name="conn_web"></param>
		/// <param name="kvar"></param>
		/// <param name="ret"></param>
		/// <returns></returns>
		private Returns AddLsToCache(IDbConnection conn_db, IDbTransaction transaction, IDbConnection conn_web, Ls kvar)
		{
			string tXX_spls = "t" + Convert.ToString(kvar.nzp_user) + "_spls";

			Returns ret;

			List<Ls> list = LoadLs(conn_db, transaction, kvar, out ret);
			if (!ret.result)
			{
				return ret;
			}
			if (list == null || list.Count == 0)
			{
				return new Returns(false, "Лицевой счет по адресу " + kvar.adr + " не найден");
			}

			string sql = "select nzp_kvar from " + tXX_spls + " where nzp_kvar = " + kvar.nzp_kvar;

			IDataReader reader;
			ret = ExecRead(conn_web, out reader, sql, true);
			if (!ret.result) return ret;

			if (reader.Read())
			{
				sql = "update " + tXX_spls + " set num_ls = " + list[0].num_ls +
					", pkod10 = " + list[0].pkod10 +
					", pkod = " + list[0].pkod +
					", nzp_area = " + list[0].nzp_area +
					", nzp_geu = " + list[0].nzp_geu +
					", typek = " + list[0].typek +
					", stypek ='" + list[0].stypek + "'" +
					", fio = '" + list[0].fio + "'" +
					", adr ='" + list[0].adr + "'" +
					", nkvar = '" + list[0].nkvar + " " + list[0].nkvar_n + "'" +
					", ikvar = " + Utils.GetInt(list[0].nkvar) +
					", sostls ='" + list[0].state + "'" +
					" where nzp_kvar = " + kvar.nzp_kvar;
			}
			else
			{
				sql = " Insert into " + tXX_spls +
					  " ( nzp_kvar, num_ls, pkod, pkod10, nzp_dom, nzp_area, nzp_geu, nzp_ul, typek, fio, adr, ulica, ndom," +
					  "   idom, nkvar, ikvar, stypek, sostls, pref ) " +
					  " Values(" + kvar.nzp_kvar +
					  "," + list[0].num_ls +
					  "," + list[0].pkod +
					  "," + list[0].pkod10 +
					  "," + list[0].nzp_dom +
					  "," + list[0].nzp_area + "," + list[0].nzp_geu + "," +
							   list[0].nzp_ul + "," + list[0].typek + ",'" + list[0].fio + "','" + list[0].adr + "','" +
							   list[0].ulica + "','" + list[0].ndom + "'," + Utils.GetInt(list[0].ndom) + ",'" +
							   list[0].nkvar + "'," + Utils.GetInt(list[0].nkvar) + ",'" + list[0].stypek + "','','" + list[0].pref + "')";
			}

			ret = ExecSQL(conn_web, sql, true);
			return ret;
		}


		//----------------------------------------------------------------------
		public _Rekvizit GetLsRevizit(Ls finder, out Returns ret)
		//----------------------------------------------------------------------
		{
			ret = Utils.InitReturns();
			if (finder.nzp_user < 1)
			{
				ret.result = false;
				ret.text = "Не определен пользователь";
				return null;
			}
			string pref = finder.pref;
			if (pref == "") pref = Points.Pref;



			//заполнить webdata:tXX_spls
			string conn_kernel = Points.GetConnByPref(pref);
			IDbConnection conn_db = GetConnection(conn_kernel);
			//IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
			ret = OpenDb(conn_db, true);
			if (!ret.result)
			{
				conn_db.Close();
				return null;
			}


			_Rekvizit zap = new _Rekvizit();

			string sql = "";
			IDataReader reader;

			if (finder.nzp_kvar != 0)
			{
#if PG
				sql = " Select a.* from " + pref + "_data.s_bankstr a, " + pref + "_data.kvar b "
					  + " where a.nzp_area=b.nzp_area and a.nzp_geu=b.nzp_geu  " + " and nzp_kvar = " + finder.nzp_kvar.ToString();
#else
				sql = " Select a.* from " + pref + "_data:s_bankstr a, " + pref + "_data:kvar b " +
					  " where a.nzp_area=b.nzp_area and a.nzp_geu=b.nzp_geu  " +
					  " and nzp_kvar = " + finder.nzp_kvar.ToString();
#endif
			}
			else
			{
#if PG
				sql = " Select * from " + pref + "_data.s_bankstr a " + " where nzp_area = " + finder.nzp_area.ToString();
#else
				sql = " Select * from " + pref + "_data:s_bankstr a " + " where nzp_area = " + finder.nzp_area.ToString();
#endif
				//   " and nzp_geu = " + finder.nzp_geu.ToString();
			}
			if (!ExecRead(conn_db, out reader, sql, true).result)
			{
				MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
				conn_db.Close();
				return null;
			}
			else
			{
				if (reader.Read())
				{
					zap.filltext = 1;


					if (reader["sb1"] != DBNull.Value)
					{
						if (Convert.ToString(reader["sb1"]).Trim() != "")
							zap.bank = zap.bank + Convert.ToString(reader["sb1"]).Trim();
					}
					if (reader["sb2"] != DBNull.Value)
					{
						if (Convert.ToString(reader["sb2"]).Trim() != "")
							zap.bank = zap.bank + Environment.NewLine + Convert.ToString(reader["sb2"]).Trim();
					}
					if (reader["sb3"] != DBNull.Value)
					{
						if (Convert.ToString(reader["sb3"]).Trim() != "")
							zap.bank = zap.bank + Environment.NewLine + Convert.ToString(reader["sb3"]).Trim();
					}
					if (reader["sb4"] != DBNull.Value)
					{
						if (Convert.ToString(reader["sb4"]).Trim() != "")
							zap.bank = zap.bank + Environment.NewLine + Convert.ToString(reader["sb4"]).Trim();
					}
					if (reader["sb5"] != DBNull.Value)
					{
						if (Convert.ToString(reader["sb5"]).Trim() != "")
							zap.bank = zap.bank + Environment.NewLine + Convert.ToString(reader["sb5"]).Trim();
					}
					if (reader["sb6"] != DBNull.Value)
					{
						if (Convert.ToString(reader["sb6"]).Trim() != "")
							zap.bank = zap.bank + Environment.NewLine + Convert.ToString(reader["sb6"]).Trim();
					}
					if (reader["sb7"] != DBNull.Value)
					{
						if (Convert.ToString(reader["sb7"]).Trim() != "")
							zap.bank = zap.bank + Environment.NewLine + Convert.ToString(reader["sb7"]).Trim();
					}
					if (reader["sb8"] != DBNull.Value)
					{
						if (Convert.ToString(reader["sb8"]).Trim() != "")
							zap.bank = zap.bank + Environment.NewLine + Convert.ToString(reader["sb8"]).Trim();
					}
					if (reader["sb9"] != DBNull.Value)
					{
						if (Convert.ToString(reader["sb9"]).Trim() != "")
							zap.bank = zap.bank + Environment.NewLine + Convert.ToString(reader["sb9"]).Trim();
					}
					if (reader["sb10"] != DBNull.Value)
					{
						if (Convert.ToString(reader["sb10"]).Trim() != "")
							zap.bank = zap.bank + Environment.NewLine + Convert.ToString(reader["sb10"]).Trim();
					}
					if (reader["sb11"] != DBNull.Value)
					{
						if (Convert.ToString(reader["sb11"]).Trim() != "")
							zap.bank = zap.bank + Environment.NewLine + Convert.ToString(reader["sb11"]).Trim();
					}
					if (reader["sb12"] != DBNull.Value)
					{
						if (Convert.ToString(reader["sb12"]).Trim() != "")
							zap.bank = zap.bank + Environment.NewLine + Convert.ToString(reader["sb12"]).Trim();
					}
					if (reader["sb13"] != DBNull.Value)
					{
						if (Convert.ToString(reader["sb13"]).Trim() != "")
							zap.bank = zap.bank + Environment.NewLine + Convert.ToString(reader["sb13"]).Trim();
					}
					if (reader["sb14"] != DBNull.Value)
					{
						if (Convert.ToString(reader["sb14"]).Trim() != "")
							zap.bank = zap.bank + Environment.NewLine + Convert.ToString(reader["sb14"]).Trim();
					}
					if (reader["sb15"] != DBNull.Value)
					{
						if (Convert.ToString(reader["sb15"]).Trim() != "")
							zap.bank = zap.bank + Environment.NewLine + Convert.ToString(reader["sb15"]).Trim();
					}


					if (reader.FieldCount > 18)
						if (reader["filltext"].ToString().Trim() == "0")
						{
							if (reader["nzp_geu"] != DBNull.Value) zap.nzp_geu = System.Convert.ToInt32(reader["nzp_geu"]);
							if (reader["nzp_area"] != DBNull.Value) zap.nzp_area = System.Convert.ToInt32(reader["nzp_area"]);
							if (reader["sb1"] != DBNull.Value) zap.poluch = System.Convert.ToString(reader["sb1"]).Trim();
							if (reader["sb2"] != DBNull.Value) zap.bank = System.Convert.ToString(reader["sb2"]).Trim();
							if (reader["sb3"] != DBNull.Value) zap.rschet = System.Convert.ToString(reader["sb3"]).Trim();
							if (reader["sb4"] != DBNull.Value) zap.korr_schet = System.Convert.ToString(reader["sb4"]).Trim();
							if (reader["sb5"] != DBNull.Value) zap.bik = System.Convert.ToString(reader["sb5"]).Trim();
							if (reader["sb6"] != DBNull.Value) zap.inn = System.Convert.ToString(reader["sb6"]).Trim();
							if (reader["sb7"] != DBNull.Value) zap.phone = System.Convert.ToString(reader["sb7"]).Trim();
							if (reader["sb8"] != DBNull.Value) zap.adres = System.Convert.ToString(reader["sb8"]).Trim();
							if (reader["sb9"] != DBNull.Value) zap.pm_note = System.Convert.ToString(reader["sb9"]).Trim();
							if (reader["sb10"] != DBNull.Value) zap.poluch2 = System.Convert.ToString(reader["sb10"]).Trim();
							if (reader["sb11"] != DBNull.Value) zap.bank2 = System.Convert.ToString(reader["sb11"]).Trim();
							if (reader["sb12"] != DBNull.Value) zap.rschet2 = System.Convert.ToString(reader["sb12"]).Trim();
							if (reader["sb13"] != DBNull.Value) zap.korr_schet2 = System.Convert.ToString(reader["sb13"]).Trim();
							if (reader["sb14"] != DBNull.Value) zap.bik2 = System.Convert.ToString(reader["sb14"]).Trim();
							if (reader["sb15"] != DBNull.Value) zap.inn2 = System.Convert.ToString(reader["sb15"]).Trim();
							if (reader["sb16"] != DBNull.Value) zap.phone2 = System.Convert.ToString(reader["sb16"]).Trim();
							if (reader["sb17"] != DBNull.Value) zap.adres2 = System.Convert.ToString(reader["sb17"]).Trim();
							if (reader["filltext"] != DBNull.Value) zap.filltext = System.Convert.ToInt32(reader["filltext"]);
							zap.filltext = 0;
						}


				}
				reader.Close();
			}
			zap.code_uk = "0";



			if (finder.nzp_kvar != 0)
			{

#if PG
				sql = " Select * from " + pref + "_data.prm_8 a, " + pref + "_data.kvar b " +
												 " where a.nzp=b.nzp_geu  and a.is_actual<>100 and " +
												 " dat_s<=current_date and dat_po>=current_date and nzp_prm=714 " +
												 " and nzp_kvar = " + finder.nzp_kvar.ToString();
#else
				sql = " Select * from " + pref + "_data:prm_8 a, " + pref + "_data:kvar b " +
												 " where a.nzp=b.nzp_geu  and a.is_actual<>100 and " +
												 " dat_s<=today and dat_po>=today and nzp_prm=714 " +
												 " and nzp_kvar = " + finder.nzp_kvar.ToString();
#endif

			}
			else
			{
#if PG
				sql = " Select * from " + pref + "_data.prm_8 " +
								 " where nzp=" + finder.nzp_geu + "  and is_actual<>100 and " +
								 " dat_s<=current_date and dat_po>=current_date and nzp_prm=714 ";
#else
				sql = " Select * from " + pref + "_data:prm_8 " +
								 " where nzp=" + finder.nzp_geu + "  and is_actual<>100 and " +
								 " dat_s<=today and dat_po>=today and nzp_prm=714 ";
#endif

			}
			if (!ExecRead(conn_db, out reader, sql, true).result)
			{
				MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
				conn_db.Close();
				return null;
			}
			else
			{
				if (reader.Read())
					if (reader["val_prm"] != DBNull.Value) zap.code_uk = Convert.ToString(reader["val_prm"]);
				reader.Close();
			}
			if (DBManager.getServer(conn_db) != "")
			{
				DbTables tables = new DbTables(conn_db);
				sql = " select re.remark from " + tables.s_remark + " re where re.nzp_area=" + finder.nzp_area + "";
				if (!ExecRead(conn_db, out reader, sql, true).result)
				{
					MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
					conn_db.Close();
					return null;
				}
				if (reader.Read())
				{
					if (reader["remark"] != DBNull.Value)
					{
						string remark = (System.Convert.ToString(reader["remark"]));
						Encoding codepage = Encoding.Default;
						try
						{
							zap.remark = codepage.GetString(Convert.FromBase64String(remark.Trim()));
						}
						catch
						{
							zap.remark = remark;
						}
					}
				}

			}
			conn_db.Close();

			if (zap.code_uk == "0")
			{

				conn_kernel = Points.GetConnByPref(Points.Pref);
				IDbConnection conn_central = GetConnection(conn_kernel);
				ret = OpenDb(conn_central, true);
				if (!ret.result)
				{
					conn_central.Close();
					MonitorLog.WriteLog("Ошибка определения центральной базы данных  ", MonitorLog.typelog.Error, 20, 201, true);
					return null;
				}



#if PG
				sql = " Select * from " + Points.Pref + "_kernel.s_erc_code where is_current = 1 ";
#else
				sql = " Select * from " + Points.Pref + "_kernel:s_erc_code where is_current = 1 ";
#endif
				if (!ExecRead(conn_central, out reader, sql, true).result)
				{
					MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
					conn_central.Close();
					return null;
				}
				else
				{
					if (reader.Read())
						if (reader["erc_code"] != DBNull.Value) zap.code_uk = Convert.ToString(reader["erc_code"]);
					reader.Close();
					conn_central.Close();

				}
			}
			return zap;
		}

		//----------------------------------------------------------------------
		public bool SaveLsRevizit(string pref, _Rekvizit uk, out Returns ret)
		//----------------------------------------------------------------------
		{
			ret = Utils.InitReturns();
			if (pref == "")
			{
				pref = Points.Pref;
			}


			string conn_kernel = Points.GetConnByPref(pref);
			IDbConnection conn_db = GetConnection(conn_kernel);
			IDataReader reader;

			ret = OpenDb(conn_db, true);
			if (!ret.result)
			{
				conn_db.Close();
				ret.result = false;
				ret.text = "Ошибка подключения к базе данных";
				return false;
			}


			_Rekvizit zap = new _Rekvizit();
			bool has_record = false;
			int max_field_count = 17;
			string sql = "";




#if PG
			sql = " Select * from " + pref + "_data.s_bankstr a " +
				" where nzp_area = " + uk.nzp_area.ToString() +
				" and nzp_geu = " + uk.nzp_geu.ToString();
#else
			sql = " Select * from " + pref + "_data:s_bankstr a " +
				" where nzp_area = " + uk.nzp_area.ToString() +
				" and nzp_geu = " + uk.nzp_geu.ToString();
#endif
			if (!ExecRead(conn_db, out reader, sql, true).result)
			{
				MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
				ret.result = false;
				ret.text = "Ошибка выборки";
				conn_db.Close();
				return false;
			}
			else
			{
				if (reader.Read())
				{
					has_record = true;

				}
				max_field_count = reader.FieldCount;
				reader.Close();
			}

			#region Изменение структуры таблицы
			if (max_field_count == 17)
			{
				sql = " database " + pref + "_data";
				if (!ExecRead(conn_db, out reader, sql, true).result)
				{
					MonitorLog.WriteLog("Ошибка изменения количества строк таблицы реквизитов " +
						sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
					ret.result = false;
					ret.text = "Ошибка изменения количества строк таблицы ";
					conn_db.Close();
					return false;
				}
				ExecRead(conn_db, out reader, " alter table s_bankstr add sb16 char(50) ", false);
				ExecRead(conn_db, out reader, " alter table s_bankstr add sb17 char(50) ", false);
				ExecRead(conn_db, out reader, " alter table s_bankstr add filltext integer ", false);
				ExecRead(conn_db, out reader, " database " + pref + "_kernel", false);
			}
			#endregion


			#region Подготовка
			string val_st = "";
			if (uk.filltext == 1)
			{
				char[] delimiters = new char[] { '\r', '\n' };
				string[] st = uk.bank.Split(delimiters);
				int i = 0;
				foreach (string s in st)
				{
					if (i < 17)
						val_st = val_st + s.Replace("'", "") + "','";
					i++;
				}
				for (int j = i; j < 17; j++)
				{
					val_st = val_st + "','";
				}
				if (val_st != "") val_st = val_st.Substring(0, val_st.Length - 2);
			}
			#endregion




			if (has_record)
			{
				if (val_st != "")
				{

#if PG
					sql = " update  " + pref + "_data.s_bankstr set " +
						"(sb1, sb2, sb3, sb4, sb5, sb6, sb7, sb8, sb9, sb10, sb11, sb12, sb13, sb14, sb15, sb16, sb17, filltext)" +
						" = ('" + val_st + ", " + uk.filltext.ToString() + ")" +
						" where nzp_area = " + uk.nzp_area.ToString() +
						" and nzp_geu = " + uk.nzp_geu.ToString();
#else
					sql = " update  " + pref + "_data:s_bankstr set " +
						"(sb1, sb2, sb3, sb4, sb5, sb6, sb7, sb8, sb9, sb10, sb11, sb12, sb13, sb14, sb15, sb16, sb17, filltext)" +
						" = ('" + val_st + ", " + uk.filltext.ToString() + ")" +
						" where nzp_area = " + uk.nzp_area.ToString() +
						" and nzp_geu = " + uk.nzp_geu.ToString();
#endif
				}
				else
				{
#if PG
					sql = " update  " + pref + "_data.s_bankstr set " +
#else
					sql = " update  " + pref + "_data:s_bankstr set " +
#endif
 "(sb1, sb2, sb3, sb4, sb5, sb6, sb7, sb8, sb9, sb10, sb11, sb12, sb13, sb14, sb15, sb16, sb17, filltext)" +
						" =('" + uk.poluch.Replace("'", "") + "','" +
							uk.bank.Replace("'", "") + "','" +
							uk.rschet.Replace("'", "") + "','" +
							uk.korr_schet.Replace("'", "") + "','" +
							uk.bik.Replace("'", "") + "','" +
							uk.inn.Replace("'", "") + "','" +
							uk.phone.Replace("'", "") + "','" +
							uk.adres.Replace("'", "") + "','" +
							uk.pm_note.Replace("'", "") + "','" +
							uk.poluch2.Replace("'", "") + "','" +
							uk.bank2.Replace("'", "") + "','" +
							uk.rschet2.Replace("'", "") + "','" +
							uk.korr_schet2.Replace("'", "") + "','" +
							uk.bik2.Replace("'", "") + "','" +
							uk.inn2.Replace("'", "") + "','" +
							uk.phone2.Replace("'", "") + "','" +
							uk.adres2.Replace("'", "") + "','" + uk.filltext.ToString() + "')" +
						" where nzp_area = " + uk.nzp_area.ToString() +
						" and nzp_geu = " + uk.nzp_geu.ToString();
				}
			}
			else
			{
				if (val_st != "")
				{

#if PG
					sql = " insert into  " + pref + "_data.s_bankstr " +
						"(nzp_area, nzp_geu, sb1, sb2, sb3, sb4, sb5, sb6, sb7, sb8, sb9, sb10, sb11, sb12, sb13, sb14, sb15, sb16, sb17, filltext)" +
						" values(" + uk.nzp_area.ToString() + "," + uk.nzp_geu.ToString() + ",'" + val_st + "," + uk.filltext.ToString() + ")";
#else
					sql = " insert into  " + pref + "_data:s_bankstr " +
						"(nzp_area, nzp_geu, sb1, sb2, sb3, sb4, sb5, sb6, sb7, sb8, sb9, sb10, sb11, sb12, sb13, sb14, sb15, sb16, sb17, filltext)" +
						" values(" + uk.nzp_area.ToString() + "," + uk.nzp_geu.ToString() + ",'" + val_st + "," + uk.filltext.ToString() + ")";
#endif
				}
				else
				{
#if PG
					sql = " insert into   " + pref + "_data.s_bankstr " +
#else
					sql = " insert into   " + pref + "_data:s_bankstr " +
#endif
 "(nzp_area, nzp_geu, sb1, sb2, sb3, sb4, sb5, sb6, sb7, sb8, sb9, sb10, sb11, sb12, sb13, sb14, sb15, sb16, sb17, filltext)" +
					   " values(" + uk.nzp_area.ToString() + "," + uk.nzp_geu.ToString() + ",'" +
						   uk.poluch.Replace("'", "") + "','" +
						   uk.bank.Replace("'", "") + "','" +
						   uk.rschet.Replace("'", "") + "','" +
						   uk.korr_schet.Replace("'", "") + "','" +
						   uk.bik.Replace("'", "") + "','" +
						   uk.inn.Replace("'", "") + "','" +
						   uk.phone.Replace("'", "") + "','" +
						   uk.adres.Replace("'", "") + "','" +
						   uk.pm_note.Replace("'", "") + "','" +
						   uk.poluch2.Replace("'", "") + "','" +
						   uk.bank2.Replace("'", "") + "','" +
						   uk.rschet2.Replace("'", "") + "','" +
						   uk.korr_schet2.Replace("'", "") + "','" +
						   uk.bik2.Replace("'", "") + "','" +
						   uk.inn2.Replace("'", "") + "','" +
						   uk.phone2.Replace("'", "") + "','" +
						   uk.adres2.Replace("'", "") + "','" + uk.filltext.ToString() + "')";
				}
			}

			if (!ExecRead(conn_db, out reader, sql, true).result)
			{
				MonitorLog.WriteLog("Ошибка сохранения реквизитов " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
				conn_db.Close();
				ret.result = false;
				ret.text = "Ошибка сохранения реквизитов";

				return false;
			}

			DbTables tables = new DbTables(conn_db);

#if PG
			sql = "WITH upsert as" +
			"(update " + tables.s_remark + " b set (remark) = (" + Utils.EStrNull(uk.remark) + ") from (Select nzp_area  from " + pref + "_data.s_bankstr where nzp_area=" + uk.nzp_area + " limit 1) d where b.nzp_area = d.nzp_area  RETURNING b.*)" +
			"insert into " + pref + "_data.s_remark select a.nzp_area, 0, 0,  " + Utils.EStrNull(uk.remark) + "  from (Select nzp_area  from " + pref + "_data.s_bankstr where nzp_area= " + uk.nzp_area + " limit 1) a where a.nzp_area not in (select b.nzp_area from upsert b);";
 
//sql = " MERGE INTO " + tables.s_remark + " b" +
//                      " USING (Select nzp_area  from " + pref + "_data.s_bankstr where nzp_area= " + uk.nzp_area + " limit 1) e" +
//                       " ON (b.nzp_area = e.nzp_area)" +
//                       " WHEN MATCHED THEN" +
//                       " update set (remark) = (" + Utils.EStrNull(uk.remark) + ") " +
//                       " WHEN NOT MATCHED THEN" +
//                       " insert (nzp_area, nzp_geu, nzp_dom, remark) values (e.nzp_area, 0, 0, " + Utils.EStrNull(uk.remark) + ")";
#else
			Encoding codepage = Encoding.Default;
			sql = " MERGE INTO " + tables.s_remark + " b" +
								  " USING (Select FIRST 1 nzp_area  from " + pref + "_data:s_bankstr where nzp_area= " + uk.nzp_area + ") e" +
								   " ON (b.nzp_area = e.nzp_area)" +
								   " WHEN MATCHED THEN" +
				//" update set (remark) = (" + Utils.EStrNull(uk.remark) + ") " +

								   " update set (remark) = ('" +
								   System.Convert.ToBase64String(codepage.GetBytes(uk.remark)) + "') " +
								   " WHEN NOT MATCHED THEN" +
								   " insert (nzp_area, nzp_geu, nzp_dom, remark) values (e.nzp_area, 0, 0, '" +
								   System.Convert.ToBase64String(codepage.GetBytes(uk.remark)) + "')";
#endif
			if (!ExecRead(conn_db, out reader, sql, true).result)
			{
				MonitorLog.WriteLog("Ошибка сохранения примечания " + sql, MonitorLog.typelog.Error, 20, 201, true);
				conn_db.Close();
				ret.result = false;
				ret.text = "Ошибка сохранения примечания";

				return false;
			}

			conn_db.Close();
			ret.result = true;
			return true;
		}


		//----------------------------------------------------------------------
		public string GetKolGil(MonthLs finder, out Returns ret)
		//----------------------------------------------------------------------
		{
			ret = Utils.InitReturns();
			string kol_gil = "0";
			if (finder.nzp_user < 1)
			{
				ret.result = false;
				ret.text = "Не определен пользователь";
				return kol_gil;
			}

			if (finder.pref == "")
			{
				ret.text = "Префикс базы данных не задан";
				return kol_gil;
			}

			//заполнить webdata:tXX_spls
			string conn_kernel = Points.GetConnByPref(finder.pref);
			IDbConnection conn_db = GetConnection(conn_kernel);
			//IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
			ret = OpenDb(conn_db, true);
			if (!ret.result)
			{
				conn_db.Close();
				return kol_gil;
			}

			string sql = "";

			IDataReader reader;

			//finder.

#if PG
			sql = " Select " + finder.pref + "_data.get_kol_gil('01." + finder.dat_month.Month.ToString() + "." +
				finder.dat_month.Year.ToString() + "','" + DateTime.DaysInMonth(finder.dat_month.Year, finder.dat_month.Month).ToString() +
				"." + finder.dat_month.Month.ToString() + "." + finder.dat_month.Year.ToString() + "',15,nzp_kvar) as kol_gil " +
				   "from " + finder.pref + "_data.kvar b " +
				  " where  nzp_kvar = " + finder.nzp_kvar.ToString();
#else
			sql = " Select " + finder.pref + "_data:get_kol_gil('01." + finder.dat_month.Month.ToString() + "." +
				finder.dat_month.Year.ToString() + "','" + DateTime.DaysInMonth(finder.dat_month.Year, finder.dat_month.Month).ToString() +
				"." + finder.dat_month.Month.ToString() + "." + finder.dat_month.Year.ToString() + "',15,nzp_kvar) as kol_gil " +
				   "from " + finder.pref + "_data:kvar b " +
				  " where  nzp_kvar = " + finder.nzp_kvar.ToString();
#endif
			if (!ExecRead(conn_db, out reader, sql, true).result)
			{
				MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
				conn_db.Close();
				return null;
			}
			else
			{
				if (reader.Read())
				{
					if (reader["kol_gil"] != DBNull.Value) kol_gil = Convert.ToString(reader["kol_gil"]);

				}
				reader.Close();
				conn_db.Close();
			}
			return kol_gil;
		}

		/// <summary>
		/// Генерирует лицевые счета (с приборами учета) или приборы учета
		/// </summary>
		/// <param name="kvar">Если задан, генерируются лицевые счета</param>
		/// <param name="CounterList">Если задан, генерируются приборы учета</param>
		/// <param name="ret"></param>
		/// <returns></returns>
		public Returns GenerateLsPu(Ls kvar, List<Counter> CounterList)
		{
			#region Проверка входных параметров
			string sPref = "";
			int nzpUser = 0;
			if (CounterList != null)
			{
				if (CounterList.Count == 0)
				{
					return new ReturnsType() { result = false, tag = -1, text = "Список данных для генерации счетчиков пуст" }.GetReturns();
				}
				if (CounterList[0].nzp_user < 1)
				{
					return new ReturnsType() { result = false, tag = -1, text = "Не определен пользователь" }.GetReturns();
				}
				else nzpUser = CounterList[0].nzp_user;

				if (CounterList[0].pref.Trim() == "")
				{
					return new ReturnsType() { result = false, tag = -1, text = "Не определен префикс базы данных" }.GetReturns();
				}
				else sPref = CounterList[0].pref.Trim();
			}
			if (kvar != null)
			{
				if (kvar.nzp_user < 1)
				{
					return new ReturnsType() { result = false, tag = -1, text = "Не определен пользователь" }.GetReturns();
				}
				else nzpUser = kvar.nzp_user;

				if (kvar.pref.Trim() == "")
				{
					return new ReturnsType() { result = false, tag = -1, text = "Не определен префикс базы данных" }.GetReturns();
				}
				else sPref = kvar.pref.Trim();
			}
			if (nzpUser < 1)
			{
				return new ReturnsType() { result = false, tag = -1, text = "Не определен пользователь" }.GetReturns();
			}
			if (sPref.Trim() == "")
			{
				return new ReturnsType() { result = false, tag = -1, text = "Не определен префикс базы данных" }.GetReturns();
			}
			#endregion

			#region Инициализация переменых
			Returns ret = Utils.InitReturns();
			int inKvar = 0;
			int inKvar_po = 0;
			#endregion

			if (kvar != null)
			{
				#region Проверка входных параметров
				try { inKvar = Int32.Parse(kvar.nkvar); }
				catch
				{
					return new ReturnsType() { result = false, tag = -1, text = "Некорректный интервал для генерации л/c" }.GetReturns();
				}
				try { inKvar_po = Int32.Parse(kvar.nkvar_po); }
				catch
				{
					return new ReturnsType() { result = false, tag = -1, text = "Некорректный интервал для генерации л/c" }.GetReturns();
				}

				if ((kvar.gen_pu == 1) && (CounterList == null))
				{
					return new ReturnsType() { result = false, tag = -1, text = "Не задан список услуг для генерации приборов учета" }.GetReturns();
				}
				#endregion

				#region Для копирования характеристик
				Ls from = new Ls();
				if (kvar.copy_ls == 1)
				{
					kvar.CopyTo(from);
					RecordMonth r_m = Points.GetCalcMonth(new CalcMonthParams(kvar.pref));
					from.stateValidOn = new DateTime(r_m.year_, r_m.month_, 1).ToString("dd.MM.yyyy");
					//from.num_ls = kvar.copy_ls_from;
					from.nzp_kvar = kvar.copy_ls_from;
					from.pref = "";

					List<Ls> list = LoadLs(from, out ret);
					if (!ret.result)
					{
						return ret;
					}
					if ((list == null) || (list.Count == 0))
					{
						return new ReturnsType() { result = false, tag = -1, text = "Не указан лицевой счет, характеристики которого необходимо копировать" }.GetReturns();
					}
					from.nzp_kvar = list[0].nzp_kvar;
					from.pref = list[0].pref;
				}
				#endregion

				#region Подключение к БД
				string connectionString = Points.GetConnByPref(sPref);
				IDbConnection conn_db = GetConnection(connectionString);
				ret = OpenDb(conn_db, true);
				if (!ret.result)
				{
					return ret;
				}
				IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
				ret = OpenDb(conn_web, true);
				if (!ret.result)
				{
					conn_db.Close();
					return ret;
				}
				#endregion

				string tXX_spls = "t" + Convert.ToString(kvar.nzp_user) + "_spls";
				bool isListLsInCacheExists = TableInWebCashe(conn_web, tXX_spls);

				IDbTransaction transaction = null;
				///try { transaction = conn_db.BeginTransaction(); }
				///catch { transaction = null; }
				try
				{
					for (int iCount = inKvar; iCount <= inKvar_po; iCount++)
					{
						transaction = conn_db.BeginTransaction();

						#region Создание Л/c
						kvar.nzp_kvar = Constants._ZERO_;
						kvar.num_ls = Constants._ZERO_;
						kvar.chekexistls = 1;
						kvar.nkvar = iCount.ToString();

						kvar.nzp_kvar = Update(conn_db, transaction, conn_web, kvar, out ret);

						if (!ret.result)
							if (ret.tag == -11)
							{
								transaction.Rollback();
								return new ReturnsType() { result = false, tag = -11, text = "Л/с по этому адресу уже существует!" }.GetReturns();
							}
						#endregion

						#region сохранить состояние ЛС
						/*Param prm = new Param();
					prm.dat_s = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).ToShortDateString();
					prm.nzp_user = kvar.nzp_user;

					prm.pref = kvar.pref;
					prm.nzp = kvar.nzp_kvar;
					prm.nzp_prm = 51;
					prm.val_prm = kvar.stateID.ToString();
					prm.prm_num = 3;

					DbParameters dbprm = new DbParameters();
					ret = dbprm.SavePrm(prm, conn_web);
					if (!ret.result) return Constants._ZERO_;*/
						#endregion

						#region Копирование характеристик
						if (kvar.copy_ls == 1)
						{
							DbParameters dbparam = new DbParameters();
							ret = dbparam.CopyLsParams(conn_db, transaction, from, kvar);
							if (!ret.result)
							{
								transaction.Rollback();
								return ret;
							}
						}
						#endregion

						#region Создание ПУ
						if (kvar.gen_pu == 1)
						{
							#region Цикл по счетчикам
							for (int iCnt1 = 0; iCnt1 < CounterList.Count; ++iCnt1)
							{
								int iCount2 = CounterList[iCnt1].cnt_ls;
								if (iCount2 == 0) iCount2 = 1;

								Counter newCounter = new Counter();
								CounterList[iCnt1].CopyTo(newCounter);
								newCounter.nzp_type = 3;
								newCounter.cnt_ls = 0;
								newCounter.nzp = kvar.nzp_kvar;
								newCounter.nzp_kvar = kvar.nzp_kvar;
								newCounter.nzp_serv = CounterList[iCnt1].nzp_serv;
								newCounter.nzp_cnt = CounterList[iCnt1].nzp_cnt;
								newCounter.nzp_cnttype = CounterList[iCnt1].nzp_cnttype;
								newCounter.nzp_user = CounterList[iCnt1].nzp_user;
								newCounter.pref = sPref;

								for (int iCnt2 = 1; iCnt2 <= iCount2; ++iCnt2)
								{
									DbCounter dbcounter = new DbCounter();

									if (newCounter.nzp_serv > 0)
										newCounter.num_cnt = "К-" + kvar.nzp_kvar.ToString() + "-" + newCounter.nzp_serv.ToString() + "-" + iCnt2.ToString();
									else
										newCounter.num_cnt = "К-" + kvar.nzp_kvar.ToString() + "-C" + newCounter.nzp_cnt.ToString() + "-" + iCnt2.ToString();
									RecordMonth r_m = Points.GetCalcMonth(new CalcMonthParams(sPref));
									ret = dbcounter.SaveCounter(conn_db, transaction, newCounter, new DateTime(r_m.year_, r_m.month_, 1).ToString("dd.MM.yyyy"));

									if (!ret.result)
									{
										return ret;
									}
								}
							}
							#endregion
						}
						#endregion

						//добавление ЛС в выбранный список
						if (isListLsInCacheExists)
						{
							AddLsToCache(conn_db, transaction, conn_web, kvar);
						}

						transaction.Commit();
						transaction = null;
					}
				}
				catch (Exception ex)
				{
					ret.result = false;
					ret.text = ex.Message;
					MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
					if (transaction != null) transaction.Rollback();
				}
				finally
				{
					conn_db.Close();
					conn_web.Close();
				}
			}
			else
			{
				#region Проверка входных параметров
				if (CounterList == null)
				{
					return new ReturnsType() { result = false, tag = -1, text = "Не задан список услуг для генерации приборов учета" }.GetReturns();
				}
				#endregion

				#region Подключение к БД
				string connectionString = Points.GetConnByPref(sPref);
				IDbConnection conn_db = GetConnection(connectionString);
				ret = OpenDb(conn_db, true);
				if (!ret.result)
				{
					return ret;
				}
				#endregion

				#region обновление данных в выбранном списке Л/с
				IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
				ret = OpenDb(conn_web, true);
				if (!ret.result)
				{
					conn_db.Close();
					return ret;
				}

				string tXX_spls = "t" + Convert.ToString(nzpUser) + "_spls";

				IDbTransaction transaction = null;
				IDataReader reader = null;
				try
				{
					if (TableInWebCashe(conn_web, tXX_spls))
					{
						#region подсчитать количество выбранных лицевых счетов, если их больше 500, то генерацию ПУ не делать
						string sql = "select count(*) as ls_cnt from " + tXX_spls + " where pref = '" + sPref + "'";
						ret = ExecRead(conn_web, out reader, sql, true);
						if (!ret.result)
						{
							return ret;
						}
						int ls_cnt = 0;

						if (reader.Read())
						{
							if (reader["ls_cnt"] != DBNull.Value) ls_cnt = Convert.ToInt32(reader["ls_cnt"]);
						}
						reader.Close();
						reader.Dispose();

						if (ls_cnt > 500)
						{
							ret.result = true;
							ret.tag = Constants._ZERO_;
							ret.text = "Найдено более 500 лицевых счетов. Уточните параметры поиска. Генерация ПУ не выполнена.";
							return ret;
						}
						#endregion

						sql = "select nzp_kvar, pref from " + tXX_spls + " where pref = '" + sPref + "'";
						ret = ExecRead(conn_web, out reader, sql, true);
						if (!ret.result)
						{
							return ret;
						}
						int nzp_kvarpu = 0;

						while (reader.Read())
						{
							if (reader["nzp_kvar"] != DBNull.Value) nzp_kvarpu = (int)reader["nzp_kvar"];
							if (nzp_kvarpu > 0)
							{
								transaction = conn_db.BeginTransaction();

								#region Цикл по счетчикам
								for (int iCnt1 = 0; iCnt1 < CounterList.Count; ++iCnt1)
								{
									int iCount2 = CounterList[iCnt1].cnt_ls;
									if (iCount2 == 0) iCount2 = 1;

									Counter newCounter = new Counter();
									CounterList[iCnt1].CopyTo(newCounter);
									newCounter.nzp_type = 3;
									newCounter.cnt_ls = 0;
									newCounter.nzp = nzp_kvarpu;
									newCounter.nzp_kvar = nzp_kvarpu;
									newCounter.nzp_serv = CounterList[iCnt1].nzp_serv;
									newCounter.nzp_cnt = CounterList[iCnt1].nzp_cnt;
									newCounter.nzp_cnttype = CounterList[iCnt1].nzp_cnttype;
									newCounter.nzp_user = CounterList[iCnt1].nzp_user;
									newCounter.pref = sPref;

									for (int iCnt2 = 1; iCnt2 <= iCount2; ++iCnt2)
									{
										DbCounter dbcounter = new DbCounter();

										if (newCounter.nzp_serv > 0)
											newCounter.num_cnt = "К-" + nzp_kvarpu.ToString() + "-" + newCounter.nzp_serv.ToString() + "-" + iCnt2.ToString();
										else
											newCounter.num_cnt = "К-" + nzp_kvarpu.ToString() + "-C" + newCounter.nzp_cnt.ToString() + "-" + iCnt2.ToString();
										RecordMonth r_m = Points.GetCalcMonth(new CalcMonthParams(newCounter.pref));
										ret = dbcounter.SaveCounter(conn_db, transaction, newCounter, new DateTime(r_m.year_, r_m.month_, 1).ToString("dd.MM.yyyy"));

										if (!ret.result)
										{
											transaction.Rollback();
											return ret;
										}
									}
								}
								transaction.Commit();
								transaction = null;
								#endregion
							}
						}
					}
				}
				catch (Exception ex)
				{
					ret.result = false;
					ret.text = ex.Message;
					MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
					if (transaction != null) transaction.Rollback();
				}
				finally
				{
					if (reader != null && !reader.IsClosed)
					{
						reader.Close();
						reader.Dispose();
					}
					conn_db.Close();
					conn_web.Close();
				}
				#endregion
			}

			return ret;
		}

		/// <summary>
		/// групповая операция исправить данные домов
		/// </summary>
		/// <param name="dom">данные для изменения</param>
		/// <param name="ret">результат выполнения операции</param>
		public void UpdateGroup(Dom dom, out Returns ret)
		{
			//инициализация результата
			ret = Utils.InitReturns();

			if (dom.nzp_user <= 0)
			{
				ret.result = false;
				ret.text = "Пользователь не определен";
				return;
			}

			//соединение с основной БД
			string conn_kernel = Points.GetConnByPref(dom.pref);
			IDbConnection conn_db = GetConnection(conn_kernel);
			//IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
			ret = OpenDb(conn_db, true);
			if (!ret.result) return;

			//соединение с кэш БД 
			IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
			ret = OpenDb(conn_web, true);
			if (!ret.result) return;

			//сформировать запрос для сохранения
			string what = "";
			if (dom.nzp_area > 0) what += " nzp_area = " + dom.nzp_area.ToString();
			if (dom.nzp_geu > 0)
			{
				if (what != "") what += " , ";
				what += " nzp_geu = " + dom.nzp_geu.ToString();
			}
			if (what == "")
			{
				ret.result = false;
				ret.text = "Нет данных для сохранения";
				conn_db.Close();
				conn_web.Close();
				return;
			}

			//получить список префиксов в выбранном списке домов в кэше
			List<string> preflist = new List<string>();
			IDataReader reader;
			string tXX_spdom = "t" + Convert.ToString(dom.nzp_user) + "_spdom";
			string sql = "select distinct pref from " + tXX_spdom + " where mark =1";
			if (!ExecRead(conn_web, out reader, sql, true).result)
			{
				ret.result = false;
				conn_db.Close();
				conn_web.Close();
				return;
			}
			try
			{
				while (reader.Read())
					if (reader["pref"] != DBNull.Value) preflist.Add(((string)reader["pref"]).Trim());
				reader.Close();
			}
			catch (Exception ex)
			{
				reader.Close();
				conn_db.Close();
				conn_web.Close();
				ret.result = false;
				ret.text = ex.Message;

				string err;
				if (Constants.Viewerror) err = " \n " + ex.Message;
				else err = "";

				MonitorLog.WriteLog("Ошибка UpdateGroup домов " + err, MonitorLog.typelog.Error, 20, 201, true);
				return;
			}

			//Андрей, объясни мне что это за код
			foreach (string pref in preflist)
			{
#if PG
				sql = "select count(*) as num from " + pref + "_data.s_area where nzp_area = " + dom.nzp_area;
#else
				sql = "select count(*) as num from " + pref + "_data:s_area where nzp_area = " + dom.nzp_area;
#endif
				IDbCommand cmd = DBManager.newDbCommand(sql, conn_db);
				try
				{
					string s = Convert.ToString(cmd.ExecuteScalar());
					ret.tag = Convert.ToInt32(s);
					if (ret.tag == 0)
					{
#if PG
						sql = " Insert into " + pref + "_data.s_area (nzp_area, area, nzp_supp) select nzp_area, area, nzp_supp " +
							  " From " + Points.Pref + "_data.s_area " +
							  " Where " + " nzp_area = " + dom.nzp_area;
#else
						sql = " Insert into " + pref + "_data:s_area (nzp_area, area, nzp_supp) select nzp_area, area, nzp_supp " +
							  " From " + Points.Pref + "_data:s_area " +
							  " Where " + " nzp_area = " + dom.nzp_area;
#endif
						ret = ExecSQL(conn_db, sql, true);
						if (!ret.result)
						{
							ret.text = "Ошибка записи данных дома в групповых операциях при добавлении Управляющей организации в банк " + pref;
							conn_db.Close();
							conn_web.Close();
							return;
						}
					}
				}
				catch (Exception ex)
				{
					conn_db.Close();
					conn_web.Close();
					ret.result = false;
					ret.text = ex.Message;
					string err;
					if (Constants.Viewerror) err = " \n " + ex.Message; else err = "";
					MonitorLog.WriteLog("Ошибка записи данных дома в групповых операциях при проверке Управляющих организаций" + err, MonitorLog.typelog.Error, 20, 201, true);
					return;
				}

#if PG
				sql = "select count(*) as num from " + pref + "_data.s_geu where nzp_geu = " + dom.nzp_geu;
#else
				sql = "select count(*) as num from " + pref + "_data:s_geu where nzp_geu = " + dom.nzp_geu;
#endif
				cmd = DBManager.newDbCommand(sql, conn_db);
				try
				{
					string s = Convert.ToString(cmd.ExecuteScalar());
					ret.tag = Convert.ToInt32(s);
					if (ret.tag == 0)
					{
#if PG
						sql = "insert into " + pref + "_data.s_geu (nzp_geu, geu) select nzp_geu, geu from " + Points.Pref + "_data.s_geu where " +
							" nzp_geu = " + dom.nzp_geu;
#else
						sql = "insert into " + pref + "_data:s_geu (nzp_geu, geu) select nzp_geu, geu from " + Points.Pref + "_data:s_geu where " +
							" nzp_geu = " + dom.nzp_geu;
#endif
						ret = ExecSQL(conn_db, sql, true);
						if (!ret.result)
						{
							ret.text = "Ошибка записи данных дома в групповых операциях при добавлении ЖЭУ в банк " + pref;
							conn_db.Close();
							conn_web.Close();
							return;
						}
					}
				}
				catch (Exception ex)
				{
					conn_db.Close();
					conn_web.Close();
					ret.result = false;
					ret.text = ex.Message;
					string err;
					if (Constants.Viewerror) err = " \n " + ex.Message; else err = "";
					MonitorLog.WriteLog("Ошибка записи данных дома в групповых операциях при проверке ЖЭУ" + err, MonitorLog.typelog.Error, 20, 201, true);
					return;
				}

				DbTables tables = new DbTables(conn_db);
				if (dom.clear_remark == true) { dom.remark = ""; };
				if (dom.remark == "" && dom.clear_remark == true || dom.remark != "")
				{
#if PG
					sql = " MERGE INTO " + tables.s_remark + " b" +
											   " USING (select d.nzp_dom from " + pgDefaultDb + "." + tXX_spdom + " d where mark=1 and d.pref = '" + pref + "') e" +
												" ON (b.nzp_dom = e.nzp_dom)" +
												" WHEN MATCHED THEN" +
												" update set (remark) = (" + Utils.EStrNull(dom.remark) + ") " +
												" WHEN NOT MATCHED THEN" +
												" insert (nzp_area, nzp_geu, nzp_dom, remark) values (0, 0, e.nzp_dom, " + Utils.EStrNull(dom.remark) + ")";
#else
					sql = " MERGE INTO " + tables.s_remark + " b" +
											   " USING (select d.nzp_dom from " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spdom + " d where mark=1 and d.pref = '" + pref + "') e" +
												" ON (b.nzp_dom = e.nzp_dom)" +
												" WHEN MATCHED THEN" +
												" update set (remark) = (" + Utils.EStrNull(dom.remark) + ") " +
												" WHEN NOT MATCHED THEN" +
												" insert (nzp_area, nzp_geu, nzp_dom, remark) values (0, 0, e.nzp_dom, " + Utils.EStrNull(dom.remark) + ")";
#endif

					ret = ExecSQL(conn_db, sql, true);
					if (!ret.result)
					{
						ret.text = "Ошибка записи данных дома в групповых операциях";
						conn_db.Close();
						conn_web.Close();
						return;
					}
				}

#if PG
				sql = "update " + pref + "_data.dom " +
							" Set " + what +
							" Where nzp_dom in (select d.nzp_dom from " + pgDefaultDb + "." + tXX_spdom + " d where mark=1 and d.pref = '" + pref + "')";
#else
				sql = "update " + pref + "_data:dom " +
							" Set " + what +
							" Where nzp_dom in (select d.nzp_dom from " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spdom + " d where mark=1 and d.pref = '" + pref + "')";
#endif
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result)
				{
					ret.text = "Ошибка записи данных дома в групповых операциях";
					conn_db.Close();
					conn_web.Close();
					return;
				}
			}

			string dop = "";
			if (dom.area.Trim() != "") dop += " , area = '" + dom.area.Trim() + "'";
			if (dom.geu.Trim() != "") dop += " , geu = '" + dom.geu.Trim() + "'";
			sql = "update " + tXX_spdom + " set " + what + dop;
			ret = ExecSQL(conn_web, sql, true);
			if (!ret.result)
			{
				ret.text = "Ошибка записи данных дома в групповых операциях";
				conn_db.Close();
				conn_web.Close();
				return;
			}

			//обновить информацию в центральном банке
			sql = "select pref, nzp_dom from " + tXX_spdom + " where mark = 1";

			if (!ExecRead(conn_web, out reader, sql, true).result)
			{
				ret.result = false;
				conn_db.Close();
				conn_web.Close();
				return;
			}
			try
			{
				while (reader.Read())
				{
					Dom finderls = new Dom();
					if (reader["pref"] != DBNull.Value) finderls.pref = ((string)reader["pref"]).Trim();
					if (reader["nzp_dom"] != DBNull.Value) finderls.nzp_dom = Convert.ToInt32(reader["nzp_dom"]);

					ret = RefreshDom(conn_db, finderls);
					if (!ret.result)
					{
						ret.text = "Не удалось корректно обновить данные в центральном БД";
					}
				}
				reader.Close();
			}
			catch (Exception ex)
			{
				reader.Close();
				conn_db.Close();
				conn_web.Close();
				ret.result = false;
				ret.text = ex.Message;

				string err;
				if (Constants.Viewerror) err = " \n " + ex.Message;
				else err = "";

				MonitorLog.WriteLog("Ошибка UpdateGroup домов " + err, MonitorLog.typelog.Error, 20, 201, true);
				return;
			}

			conn_web.Close();
			conn_db.Close();
			return;
		}

		/// <summary>
		/// Групповая операция с лицевыми счетами
		/// </summary>
		/// <param name="ls">данные для изменения</param>
		/// <param name="ret">результат</param>
		public Returns UpdateGroup(Ls ls)
		{
			//инициализация результата
			Returns ret = Utils.InitReturns();

			if (ls.nzp_user <= 0)
			{
				ret.result = false;
				ret.text = "Пользователь не определен";
				return ret;
			}

			//соединение с основной БД
			string conn_kernel = Points.GetConnByPref(ls.pref);
			IDbConnection conn_db = GetConnection(conn_kernel);
			//IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
			ret = OpenDb(conn_db, true);
			if (!ret.result) return ret;

			//соединение с кэш БД 
			IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
			ret = OpenDb(conn_web, true);
			if (!ret.result) return ret;

			//сформировать запрос для сохранения
			string what = "";
			if (ls.nzp_area > 0) what += " nzp_area = " + ls.nzp_area.ToString();
			if (ls.nzp_geu > 0)
			{
				if (what != "") what += " , ";
				what += " nzp_geu = " + ls.nzp_geu.ToString();
			}
			if (ls.uch != "")
			{
				if (what != "") what += " , ";
				what += " uch = " + ls.uch;
			}
			if (what == "")
			{
				ret.result = false;
				ret.text = "Нет данных для сохранения";
				conn_db.Close();
				conn_web.Close();
				return ret;
			}

			//получить список префиксов в выбранном списке домов в кэше
			List<string> preflist = new List<string>();
			IDataReader reader;
			string tXX_selectedls = "t" + Convert.ToString(ls.nzp_user) + "_selectedls" + ls.listNumber;
			string tXX_spls = "t" + Convert.ToString(ls.nzp_user) + "_spls";

			if (!TempTableInWebCashe(conn_web, tXX_selectedls))
			{
				ret.result = false;
				ret.text = "Список лицевых счетов не выбран";
				conn_db.Close();
				conn_web.Close();
				return ret;
			}

			string sql = "select distinct pref from " + tXX_selectedls + " where mark=1";
			ret = ExecRead(conn_web, out reader, sql, true);
			if (!ret.result)
			{
				conn_db.Close();
				conn_web.Close();
				return ret;
			}
			try
			{
				while (reader.Read())
					if (reader["pref"] != DBNull.Value) preflist.Add(((string)reader["pref"]).Trim());
				reader.Close();
			}
			catch (Exception ex)
			{
				reader.Close();
				conn_db.Close();
				conn_web.Close();
				ret.result = false;
				ret.text = ex.Message;

				string err;
				if (Constants.Viewerror) err = " \n " + ex.Message;
				else err = "";

				MonitorLog.WriteLog("Ошибка UpdateGroup лицевых счетов " + err, MonitorLog.typelog.Error, 20, 201, true);
				return ret;
			}

			foreach (string pref in preflist)
			{
				sql = "select count(*) as num from " + pref + "_data" + DBManager.tableDelimiter + "s_area where nzp_area = " + ls.nzp_area;
				IDbCommand cmd = DBManager.newDbCommand(sql, conn_db);
				try
				{
					string s = Convert.ToString(cmd.ExecuteScalar());
					ret.tag = Convert.ToInt32(s);
					if (ret.tag == 0)
					{
#if PG
						sql = "insert into " + pref + "_data.s_area (nzp_area, area, nzp_supp) select nzp_area, area, nzp_supp from " + Points.Pref + "_data.s_area where " +
							" nzp_area = " + ls.nzp_area;
#else
						sql = "insert into " + pref + "_data:s_area (nzp_area, area, nzp_supp) select nzp_area, area, nzp_supp from " + Points.Pref + "_data:s_area where " +
							" nzp_area = " + ls.nzp_area;
#endif
						ret = ExecSQL(conn_db, sql, true);
						if (!ret.result)
						{
							ret.text = "Ошибка записи данных лиц счета в групповых операциях при добавлении Управляющей организации в банк " + pref;
							conn_db.Close();
							conn_web.Close();
							return ret;
						}
					}
				}
				catch (Exception ex)
				{
					conn_db.Close();
					conn_web.Close();
					ret.result = false;
					ret.text = ex.Message;
					string err;
					if (Constants.Viewerror) err = " \n " + ex.Message; else err = "";
					MonitorLog.WriteLog("Ошибка записи данных лиц счета в групповых операциях при проверке Управляющих организаций" + err, MonitorLog.typelog.Error, 20, 201, true);
					return ret;
				}

#if PG
				sql = "select count(*) as num from " + pref + "_data.s_geu where nzp_geu = " + ls.nzp_geu;
#else
				sql = "select count(*) as num from " + pref + "_data:s_geu where nzp_geu = " + ls.nzp_geu;
#endif
				cmd = DBManager.newDbCommand(sql, conn_db);
				try
				{
					string s = Convert.ToString(cmd.ExecuteScalar());
					ret.tag = Convert.ToInt32(s);
					if (ret.tag == 0)
					{
#if PG
						sql = "insert into " + pref + "_data.s_geu (nzp_geu, geu) select nzp_geu, geu from " + Points.Pref + "_data.s_geu where " +
							" nzp_geu = " + ls.nzp_geu;
#else
						sql = "insert into " + pref + "_data:s_geu (nzp_geu, geu) select nzp_geu, geu from " + Points.Pref + "_data:s_geu where " +
							" nzp_geu = " + ls.nzp_geu;
#endif
						ret = ExecSQL(conn_db, sql, true);
						if (!ret.result)
						{
							ret.text = "Ошибка записи данных лиц счета в групповых операциях при добавлении ЖЭУ в банк " + pref;
							conn_db.Close();
							conn_web.Close();
							return ret;
						}
					}
				}
				catch (Exception ex)
				{
					conn_db.Close();
					conn_web.Close();
					ret.result = false;
					ret.text = ex.Message;
					string err;
					if (Constants.Viewerror) err = " \n " + ex.Message; else err = "";
					MonitorLog.WriteLog("Ошибка записи данных лиц счета в групповых операциях при проверке ЖЭУ" + err, MonitorLog.typelog.Error, 20, 201, true);
					return ret;
				}

#if PG
				sql = "update " + pref + "_data.kvar " +
							" Set " + what +
							" Where nzp_kvar in (select k.nzp_kvar from " + pgDefaultDb + "." + tXX_selectedls + " k where k.pref = '" + pref + "' and mark = 1)";
#else
				sql = "update " + pref + "_data:kvar " +
							" Set " + what +
							" Where nzp_kvar in (select k.nzp_kvar from " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_selectedls + " k where k.pref = '" + pref + "' and mark = 1)";
#endif
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result)
				{
					ret.text = "Ошибка записи данных лиц счета в групповых операциях";
					conn_db.Close();
					conn_web.Close();
					return ret;
				}
			}


			what = "";
			if (ls.nzp_area > 0) what += " nzp_area = " + ls.nzp_area.ToString();
			if (ls.nzp_geu > 0)
			{
				if (what != "") what += " , ";
				what += " nzp_geu = " + ls.nzp_geu.ToString();
			}
			if (what != "")
			{

				if (TempTableInWebCashe(conn_web, tXX_spls))
				{
					sql = "update " + tXX_spls + " set " + what + " where nzp_kvar in (select nzp_kvar from " + tXX_selectedls + ")";
					ret = ExecSQL(conn_web, sql, true);
					if (!ret.result)
					{
						ret.text = "Ошибка записи данных лиц счетов в групповых операциях";
						conn_db.Close();
						conn_web.Close();
						return ret;
					}
				}
			}

			//обновить информацию в центральном банке
			sql = "select pref, nzp_kvar from " + tXX_selectedls + " where mark = 1";
			ret = ExecRead(conn_web, out reader, sql, true);
			if (!ret.result)
			{
				conn_db.Close();
				conn_web.Close();
				return ret;
			}
			try
			{
				while (reader.Read())
				{
					Ls finderls = new Ls();
					if (reader["pref"] != DBNull.Value) finderls.pref = ((string)reader["pref"]).Trim();
					if (reader["nzp_kvar"] != DBNull.Value) finderls.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);

					ret = RefreshKvar(conn_db, null, finderls);
					if (!ret.result)
					{
						ret.text = "Не удалось корректно обновить данные в центральном БД";
					}
				}
				reader.Close();
			}
			catch (Exception ex)
			{
				reader.Close();
				conn_db.Close();
				conn_web.Close();
				ret.result = false;
				ret.text = ex.Message;

				string err;
				if (Constants.Viewerror) err = " \n " + ex.Message;
				else err = "";

				MonitorLog.WriteLog("Ошибка UpdateGroup лицевых счетов " + err, MonitorLog.typelog.Error, 20, 201, true);
				return ret;
			}


			conn_web.Close();
			conn_db.Close();
			return ret;
		}



		/// <summary>
		/// Определить состояние ЛС на stateValidOn или сегодня
		/// </summary>
		public Returns LoadLsState(Ls finder, IDbConnection connection, IDbTransaction transaction)
		{
			Returns ret = Utils.InitReturns();

			DateTime d;
			if (!DateTime.TryParse(finder.stateValidOn, out d)) d = DateTime.Now;

#if PG
			string res_y = Points.Pref + "_kernel.res_y";
#else
			string res_y = Points.Pref + "_kernel@" + DBManager.getServer(connection) + ":res_y";

#endif
			string s = "";

			if (!GlobalSettings.WorkOnlyWithCentralBank)
			{
				if (finder.pref == "") return new Returns(false, "Не указан префикс БД");

#if PG
				string prm_3 = finder.pref + "_data.prm_3";
#else
				string prm_3 = finder.pref + "_data@" + DBManager.getServer(connection) + ":prm_3";
#endif

				if (!TempTableInWebCashe(connection, transaction, prm_3)) return ret;

#if PG
				s = " Select p.dat_s, ry.nzp_y, ry.name_y" +
					" From " + prm_3 + " p, " + res_y + " ry " +
					" Where nzp = " + finder.nzp_kvar +
						" and nzp_prm = 51 and is_actual <> 100 " +
						" and to_date('" + d.Month + "," + d.Day + "," + d.Year + "', 'MM,DD,YYYY') between  dat_s and  dat_po " +
						" and ry.nzp_res = 18 and trim(p.val_prm) = trim(ry.nzp_y||'')";
#else
				s = " Select p.dat_s, ry.nzp_y, ry.name_y" +
					" From " + prm_3 + " p, " + res_y + " ry " +
					" Where nzp = " + finder.nzp_kvar +
						" and nzp_prm = 51 and is_actual <> 100 " +
						" and mdy(" + d.Month + "," + d.Day + "," + d.Year + ") between  dat_s and  dat_po " +
						" and ry.nzp_res = 18 and trim(p.val_prm) = trim(ry.nzp_y||'')";
#endif

			}
			else
			{
				if (d < System.Convert.ToDateTime(Points.DateOper.Date))
				{
#if PG
					s = " Select to_date('" + d.Month + "," + d.Day + "," + d.Year + "', 'MM,DD,YYYY') dat_s, ry.nzp_y, ry.name_y" +
						" From " + res_y + " ry " +
						" Where ry.nzp_res = 18 and \'3\' = trim(ry.nzp_y||'')";
#else
					s = " Select mdy(" + d.Month + "," + d.Day + "," + d.Year + ") dat_s, ry.nzp_y, ry.name_y" +
						" From " + res_y + " ry " +
						" Where ry.nzp_res = 18 and \'3\' = trim(ry.nzp_y||'')";
#endif

				}
				else
				{
#if PG
					string kvar = Points.Pref + "_data.kvar";
#else
					string kvar = Points.Pref + "_data@" + DBManager.getServer(connection) + ":kvar";

#endif
#if PG
					s = " Select to_date('" + System.Convert.ToDateTime(Points.DateOper.Date).ToString("MM,dd,yyyy") + "', 'MM,DD,YYYY') dat_s, ry.nzp_y, ry.name_y" +
						" From " + kvar + " k, " + res_y + " ry " +
						" Where k.nzp_kvar = " + finder.nzp_kvar +
							" and ry.nzp_res = 18 and trim(k.is_open) = trim(ry.nzp_y||'')";
#else
					s = " Select mdy(" + System.Convert.ToDateTime(Points.DateOper.Date).ToString("MM,dd,yyyy") + ") dat_s, ry.nzp_y, ry.name_y" +
						" From " + kvar + " k, " + res_y + " ry " +
						" Where k.nzp_kvar = " + finder.nzp_kvar +
							" and ry.nzp_res = 18 and trim(k.is_open) = trim(ry.nzp_y||'')";
#endif

				}
			}

			IDataReader readerDO;
			ret = ExecRead(connection, transaction, out readerDO, s, true);
			if (!ret.result)
			{
				return ret;
			}

			if (readerDO.Read())
			{
				if (readerDO["dat_s"] != DBNull.Value)
				{
					d = Convert.ToDateTime(readerDO["dat_s"]);
					if (d > new DateTime(1900, 1, 1))
						finder.stateValidOn = d.ToShortDateString();
				}
				if (readerDO["nzp_y"] != DBNull.Value) finder.stateID = (int)readerDO["nzp_y"];
				if (readerDO["name_y"] != DBNull.Value) finder.state = ((string)readerDO["name_y"]).Trim();
			}
			readerDO.Close();
			return ret;
		}

		public List<Ls> LoadLs(IDbConnection conn_db, IDbTransaction transaction, Ls finder, out Returns ret) //найти и заполнить адрес для nzp_kvar
		{
			ret = new Returns(true);
			List<Ls> Listls = new List<Ls>();
			IDataReader reader;

			string swhere = ""; //условия
			StringBuilder sql = new StringBuilder();

			// если mode = 0 - информация о л/с
			// если mode = 1 - информация о доме
			// если mode = 2 - информация об улице
			// если mode = 3 - информация о ЖЭУ
			// если mode = 4 - информация о Управляющая организация
			int mode = 0;

			DbTables tables = new DbTables(conn_db);

			if (finder.nzp_kvar > 0 || finder.num_ls > 0 || finder.pkod != "" || finder.pkod10 > 0)
			{
				if (finder.nzp_kvar > 0)
				{
#if PG
					swhere += " where k.nzp_kvar = " + finder.nzp_kvar;
#else
					swhere += " and k.nzp_kvar = " + finder.nzp_kvar;

#endif
				}
				if (finder.num_ls > 0)
				{
#if PG
					swhere += (swhere.Contains("where") ? " and" : " where") + " k.num_ls = " + finder.num_ls;
#else
					swhere += " and k.num_ls = " + finder.num_ls;

#endif
				}
				if (finder.pkod != "")
				{
#if PG
					swhere += (swhere.Contains("where") ? " and" : " where") + "  k.pkod = " + finder.pkod;
#else
					swhere += " and k.pkod = " + finder.pkod;

#endif
				}

				if (finder.pkod10 > 0)
				{
#if PG
					swhere += (swhere.Contains("where") ? " and" : " where") + " k.pkod10 = " + finder.pkod10;
#else
					swhere += " and k.pkod10 = " + finder.pkod10;

#endif
				}
#if PG
				sql.Append(" Select distinct d.nzp_wp, twn.nzp_town, k.num_ls, k.pkod10, k.nzp_dom,k.pkod,k.fio, k.nzp_kvar, k.phone, k.porch, k.nkvar, k.nkvar_n, k.uch, k.typek, a.nzp_area, a.area, g.nzp_geu, g.geu, k.remark, d.nzp_ul,d.nzp_raj,  ");
				sql.Append("   trim(coalesce(ulicareg,'улица'))||' '||trim(coalesce(u.ulica,''))||' / '||trim(coalesce(r.rajon,''))||'   дом '||" +
						   "   trim(coalesce(ndom,''))||'  корп. '|| trim(coalesce(nkor,''))||'  кв. '||trim(coalesce(nkvar,''))||'  ком. '||trim(coalesce(nkvar_n,'')) as adr");
				sql.Append(", trim(coalesce(ulicareg,'улица'))||' '||trim(coalesce(u.ulica,''))||' / '||trim(coalesce(r.rajon,'')) as ulica_rajon");
				sql.Append(", trim(coalesce(ulicareg,'улица'))||' '||trim(coalesce(u.ulica,'')) as ulica, r.rajon");
				sql.Append(", rd.rajon_dom");
				sql.Append(", twn.town");
				sql.Append(", t.name_y stypek, ulica, ulicareg, ndom, nkor");
				sql.Append(", round(k.pkod)||'' as spkod");
				sql.Append(", k.pref, k.is_open, t2.name_y state");
#else
				sql.Append(" Select first 1 unique d.nzp_wp, twn.nzp_town, k.num_ls, k.pkod10, k.nzp_dom,k.pkod,k.fio, k.nzp_kvar, k.phone, k.porch, k.nkvar, k.nkvar_n, k.uch, k.typek, a.nzp_area, a.area, g.nzp_geu, g.geu, k.remark, d.nzp_ul,d.nzp_raj,  ");
				sql.Append("   trim(nvl(ulicareg,'улица'))||' '||trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,''))||'   дом '||" +
						   "   trim(nvl(ndom,''))||'  корп. '|| trim(nvl(nkor,''))||'  кв. '||trim(nvl(nkvar,''))||'  ком. '||trim(nvl(nkvar_n,'')) as adr");
				sql.Append(", trim(nvl(ulicareg,'улица'))||' '||trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,'')) as ulica_rajon");
				sql.Append(", trim(nvl(ulicareg,'улица'))||' '||trim(nvl(u.ulica,'')) as ulica, r.rajon");
				sql.Append(", rd.rajon_dom");
				sql.Append(", twn.town");
				sql.Append(", t.name_y stypek, ulica, ulicareg, ndom, nkor");
				sql.Append(", round(k.pkod)||'' as spkod");
				sql.Append(", k.pref, k.is_open, t2.name_y state");
#endif


#if PG
				sql.Append(" From " + tables.kvar + " k");
				sql.Append(" left outer join " + tables.dom + " d on k.nzp_dom=d.nzp_dom");
				sql.Append(" left outer join " + tables.area + " a on a.nzp_area=k.nzp_area");
				sql.Append(" left outer join " + tables.ulica + " u on d.nzp_ul=u.nzp_ul");
				sql.Append(" left outer join " + tables.rajon + " r on u.nzp_raj=r.nzp_raj");
				sql.Append(" left outer join " + tables.geu + " g on g.nzp_geu=k.nzp_geu");
				sql.Append(" left outer join " + tables.res_y + " t on k.typek = t.nzp_y and t.nzp_res = 9999");
				sql.AppendFormat(" left outer join " + tables.res_y + " t2 on {0} = t2.nzp_y and t2.nzp_res = 18", "k.is_open".CastTo("INTEGER"));
				sql.Append(" left outer join  " + tables.town + " twn on twn.nzp_town=r.nzp_town");
				sql.Append(" left outer join  " + tables.rajon_dom + " rd on d.nzp_raj = rd.nzp_raj_dom");
				sql.Append(swhere);
				sql.Append(" limit 1");
#else
				sql.Append(" From " + tables.kvar + " k" +
					", outer " + tables.res_y + " t" +
					", outer " + tables.res_y + " t2" +
					", " + tables.dom + " d" +
					", " + tables.ulica + " u" +
					", outer ( " + tables.rajon + " r, outer  " + tables.town + " twn)" +
					", outer  " + tables.rajon_dom + " rd" +
					", outer " + tables.area + " a" +
					", outer " + tables.geu + " g");
				sql.Append(" where k.nzp_dom=d.nzp_dom and twn.nzp_town=r.nzp_town and d.nzp_ul=u.nzp_ul and u.nzp_raj=r.nzp_raj and a.nzp_area=k.nzp_area and  g.nzp_geu=k.nzp_geu ");
				sql.Append(" and k.typek = t.nzp_y and t.nzp_res = 9999 ");
				sql.Append(" and k.is_open = t2.nzp_y and t2.nzp_res = 18 ");
				sql.Append(" and d.nzp_raj = rd.nzp_raj_dom");
				sql.Append(swhere);
#endif


				mode = 0;
			}
			else if (finder.nzp_dom > 0)
			{
#if PG
				sql.Append("Select d.nzp_wp, d.nzp_dom, twn.nzp_town, d.nzp_raj, u.nzp_ul, a.nzp_area, g.nzp_geu, g.geu, a.area, ");
				sql.Append("trim(coalesce(u.ulicareg,'улица'))||' '||trim(coalesce(u.ulica,''))||' / '||trim(coalesce(r.rajon,''))||'   дом '|| ");
				sql.Append("trim(coalesce(d.ndom,''))||'  корп. '|| trim(coalesce(d.nkor,'')) as adr, ");
				sql.Append("trim(coalesce(ulicareg,'улица'))||' '||trim(coalesce(u.ulica,''))||' / '||trim(coalesce(r.rajon,'')) as ulica_rajon, ndom, nkor ");
				sql.Append(", trim(coalesce(ulicareg,'улица'))||' '||trim(coalesce(u.ulica,'')) as ulica2, u.ulica, u.ulicareg, r.rajon");
				sql.Append(", rd.rajon_dom");
				sql.Append(", twn.town");
				sql.Append(", d.pref");
				sql.Append(", (select re.remark from " + tables.s_remark + " re where d.nzp_dom=re.nzp_dom) as remark");
				sql.Append(" From " + tables.dom + " d");
				sql.Append(" left outer join " + tables.ulica + " u on d.nzp_ul=u.nzp_ul");
				sql.Append(" left outer join " + tables.rajon + " r on r.nzp_raj=u.nzp_raj");
				sql.Append(" left outer join  " + tables.town + " twn on twn.nzp_town=r.nzp_town");
				sql.Append(" left outer join " + tables.area + " a on a.nzp_area=d.nzp_area");
				sql.Append(" left outer join " + tables.geu + " g on g.nzp_geu=d.nzp_geu");
				sql.Append(" left outer join  " + tables.rajon_dom + " rd on d.nzp_raj = rd.nzp_raj_dom");
				sql.Append(" where d.nzp_dom = " + finder.nzp_dom);
#else
				sql.Append("Select d.nzp_wp, twn.nzp_town, d.nzp_dom, d.nzp_raj, u.nzp_ul, a.nzp_area, g.nzp_geu, g.geu, a.area, ");
				sql.Append("trim(nvl(u.ulicareg,'улица'))||' '||trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,''))||'   дом '|| ");
				sql.Append("trim(nvl(d.ndom,''))||'  корп. '|| trim(nvl(d.nkor,'')) as adr, ");
				sql.Append("trim(nvl(ulicareg,'улица'))||' '||trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,'')) as ulica_rajon, ndom, nkor ");
				sql.Append(", trim(nvl(ulicareg,'улица'))||' '||trim(nvl(u.ulica,'')) as ulica2, u.ulica, u.ulicareg, r.rajon");
				sql.Append(", rd.rajon_dom");
				sql.Append(", twn.town");
				sql.Append(", d.pref");
				sql.Append(", (select re.remark from " + tables.s_remark + " re where d.nzp_dom=re.nzp_dom) as remark");
				sql.Append(" From " + tables.dom + " d" +
					", " + tables.ulica + " u" +
					", outer (" + tables.rajon + " r, outer  " + tables.town + " twn)" +
					", outer " + tables.area + " a" +
					", outer " + tables.geu + " g" +
					", outer  " + tables.rajon_dom + " rd");
				sql.Append(" Where d.nzp_ul=u.nzp_ul and twn.nzp_town=r.nzp_town and r.nzp_raj=u.nzp_raj and a.nzp_area=d.nzp_area and  g.nzp_geu=d.nzp_geu ");
				sql.Append(" and d.nzp_raj = rd.nzp_raj_dom");
				sql.Append(" and d.nzp_dom = " + finder.nzp_dom);
#endif

				mode = 1;
			}
			else if (finder.nzp_ul > 0)
			{
#if PG
				sql.Append("select trim(coalesce(u.ulicareg,'улица'))||' '||trim(coalesce(u.ulica,''))||' / '||trim(coalesce(r.rajon,'')) as adr ");
				sql.AppendFormat("from {0} u", tables.ulica);
				sql.AppendFormat(" left outer join {0} r on u.nzp_raj = r.nzp_raj", tables.rajon);
				sql.AppendFormat(" left outer join {0} r on r.nzp_town = twn.nzp_town", tables.town);
				sql.Append(" where u.nzp_ul = " + finder.nzp_ul);
#else
				sql.Append("select trim(nvl(u.ulicareg,'улица'))||' '||trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,'')) as adr ");
				sql.Append("from " + tables.ulica + " u, outer (" + tables.rajon + " r, outer  " + tables.town + " twn)");
				sql.Append(" where u.nzp_raj = r.nzp_raj and r.nzp_town = twn.nzp_town");
				sql.Append(" and u.nzp_ul = " + finder.nzp_ul);
#endif

				mode = 2;
			}
			else if (finder.nzp_geu > 0)
			{
				sql.Append("select geu from " + tables.geu + " where nzp_geu = " + finder.nzp_geu);
				mode = 3;
			}
			else if (finder.nzp_area > 0)
			{
				sql.Append("select area from " + tables.area + " where nzp_area = " + finder.nzp_area);
				mode = 4;
			}

			if (sql.Length <= 0) return Listls;

			if (!ExecRead(conn_db, transaction, out reader, sql.ToString(), true).result)
			{
				return Listls;
			}

			try
			{
				if (reader.Read())
				{
					Ls ls = new Ls();
					ls.nzp_kvar = finder.nzp_kvar;
					ls.nzp_dom = finder.nzp_dom;
					ls.nzp_ul = finder.nzp_ul;
					if (mode <= 2)
					{
						if (reader["adr"] == DBNull.Value) ls.adr = "";
						else ls.adr = Convert.ToString(reader["adr"]);

						if (mode <= 1)
						{
							if (reader["pref"] != DBNull.Value) ls.pref = ((string)reader["pref"]).Trim();
							if (reader["town"] != DBNull.Value) ls.town = Convert.ToString(reader["town"]).Trim();
							if (reader["nzp_raj"] != DBNull.Value) ls.nzp_raj = Convert.ToInt32(reader["nzp_raj"]);
							if (reader["nzp_ul"] != DBNull.Value) ls.nzp_ul = Convert.ToInt32(reader["nzp_ul"]);
							if (reader["nzp_dom"] != DBNull.Value) ls.nzp_dom = (int)reader["nzp_dom"];
							if (reader["ndom"] != DBNull.Value) ls.ndom = Convert.ToString(reader["ndom"]);
							if (reader["nkor"] != DBNull.Value) ls.nkor = Convert.ToString(reader["nkor"]);
							if (reader["nzp_area"] != DBNull.Value) ls.nzp_area = (int)reader["nzp_area"];
							if (reader["nzp_geu"] != DBNull.Value) ls.nzp_geu = (int)reader["nzp_geu"];
							if (reader["remark"] != DBNull.Value) ls.remark = (string)reader["remark"];
							if (reader["nzp_town"] != DBNull.Value) ls.nzp_town = reader["nzp_town"].ToInt();
							if (reader["nzp_wp"] != DBNull.Value) ls.nzp_wp = reader["nzp_wp"].ToInt();

							if (mode == 0)
							{
								if (reader["rajon"] != DBNull.Value) ls.rajon = Convert.ToString(reader["rajon"]).Trim();
								if (ls.rajon == "" || ls.rajon == "-")
								{
									if (reader["rajon_dom"] != DBNull.Value) ls.rajon = Convert.ToString(reader["rajon_dom"]).Trim();
									if (reader["ulica"] != DBNull.Value) ls.ulica = Convert.ToString(reader["ulica"]).Trim();
								}
								else
								{
									if (reader["ulica_rajon"] != DBNull.Value) ls.ulica = Convert.ToString(reader["ulica_rajon"]);
								}
							}
							else if (mode == 1)
							{
								if (reader["rajon"] != DBNull.Value) ls.rajon = Convert.ToString(reader["rajon"]).Trim();
								if (ls.rajon == "" || ls.rajon == "-")
								{
									if (reader["rajon_dom"] != DBNull.Value) ls.rajon = Convert.ToString(reader["rajon_dom"]).Trim();
								}
								if (reader["ulica"] != DBNull.Value) ls.ulica = Convert.ToString(reader["ulica"]);
								if (reader["ulicareg"] != DBNull.Value) ls.ulicareg = Convert.ToString(reader["ulicareg"]);

								ls.adr = ls.getAddress();
							}

							if (mode == 0)
							{
								if (GlobalSettings.WorkOnlyWithCentralBank == false)
									if (finder.prms == Constants.act_mode_edit.ToString()) //если данные необходимы для режима изменения
									{
										#region Определить пользователя
										finder.pref = ls.pref;
										DbWorkUser db = new DbWorkUser();
										int nzpUser = db.GetLocalUser(conn_db, transaction, finder, out ret); //локальный пользователь      
										db.Close();
										if (!ret.result) return Listls;
										#endregion

										#region проверить не заблокирован ли лс, если нет то заблокировать
										IDataReader reader2;
#if PG
										string sqltxt = "select nzp_user, dat_when,  (now() - INTERVAL " + string.Format("'{0} minutes')", Constants.users_min) + " as cur_date from " + ls.pref +
											  "_data.kvar_block where nzp_kvar = " + finder.nzp_kvar + " order by dat_when desc";
#else
										string sqltxt = "select nzp_user, dat_when,  (current year to second - " + Constants.users_min.ToString() + " units minute) as cur_date from " + ls.pref +
											  "_data:kvar_block where nzp_kvar = " + finder.nzp_kvar + " order by dat_when desc";
#endif

										if (!ExecRead(conn_db, transaction, out reader2, sqltxt, true).result)
										{
											return null;
										}

										try
										{
											DateTime datwhen = DateTime.MinValue;
											DateTime curdate = DateTime.MinValue;
											int nzpuser = 0;

											if (reader2.Read())
											{
												if (reader2["dat_when"] != DBNull.Value) datwhen = Convert.ToDateTime(reader2["dat_when"]);
												if (reader2["cur_date"] != DBNull.Value) curdate = Convert.ToDateTime(reader2["cur_date"]);
												if (reader2["nzp_user"] != DBNull.Value) nzpuser = Convert.ToInt32(reader2["nzp_user"]);

												if (nzpuser > 0 && datwhen != DateTime.MinValue) //заблокирован лицевой счет
												{
													if (nzpuser != nzpUser && curdate <= datwhen) //если заблокирована запись другим пользователем и 20 мин не прошло
														ls.is_blocked = 1;
												}
											}
											reader2.Close();

											if (ls.is_blocked != 1) //если л/с не заблокирован или заблокирован тем же пользователем
											{
												#region Удалить все записи для finder.nzp_kvar
#if PG
												ret = ExecSQL(conn_db, transaction, "delete from " + ls.pref + "_data.kvar_block where nzp_kvar = " + finder.nzp_kvar, true);
#else
												ret = ExecSQL(conn_db, transaction, "delete from " + ls.pref + "_data:kvar_block where nzp_kvar = " + finder.nzp_kvar, true);
#endif

												if (!ret.result)
												{
													ret.result = false;
													ret.text = "Ошибка удаления из таблицы kvar_block";
													return Listls;
												}
												#endregion

												#region Заблокировать л/с
#if PG
												ret = ExecSQL(conn_db, transaction, "insert into " + ls.pref + "_data.kvar_block (nzp_kvar,nzp_user, dat_when,kod) values(" +
																					finder.nzp_kvar + "," + nzpUser + ",now()," + Constants.ist + ")", true);
#else
												ret = ExecSQL(conn_db, transaction, "insert into " + ls.pref + "_data:kvar_block (nzp_kvar,nzp_user, dat_when,kod) values(" +
																					finder.nzp_kvar + "," + nzpUser + ",current year to second," + Constants.ist + ")", true);
#endif

												if (!ret.result)
												{
													ret.result = false;
													ret.text = "Ошибка добавления записи о блокировке в таблицу kvar_block";
													return Listls;
												}
												#endregion
											}
										}
										catch (Exception ex)
										{
											reader.Close();
											ret.result = false;
											ret.text = ex.Message;
											string err;
											if (Constants.Viewerror) err = " \n " + ex.Message;
											else err = "";
											MonitorLog.WriteLog("Ошибка получения информации о блокировки пользователя " + err, MonitorLog.typelog.Error, 20, 201, true);
											return Listls;
										}
										#endregion
									}

								if (reader["num_ls"] == DBNull.Value) ls.num_ls = 0;
								else ls.num_ls = (int)(reader["num_ls"]);

								if (reader["pkod10"] == DBNull.Value) ls.pkod10 = 0;
								else ls.pkod10 = (int)(reader["pkod10"]);

								if (reader["spkod"] == DBNull.Value) ls.pkod = "0";
								else ls.pkod = Convert.ToString(reader["spkod"]).Trim();

								if (Points.IsSmr)
								{
									//int litera = GetLitera(finder, out ret, conn_db, transaction);
									//if (litera > 0) ls.num_ls_litera = ls.pkod10.ToString() + " " + litera.ToString();
									//else ls.num_ls_litera = ls.pkod10.ToString();

									if (ls.pkod == null || ls.pkod.Length != 13 || ls.pkod.Substring(10, 1) == "0")
										ls.num_ls_litera = ls.pkod10.ToString();
									else ls.num_ls_litera = ls.pkod10 + " " + ls.pkod.Substring(10, 1);
								}

								if (reader["fio"] == DBNull.Value) ls.fio = "";
								else ls.fio = Convert.ToString(reader["fio"]);
								/*ls.fio = "xxxxxxxx xxxxxx xxxxxx";*/

								if (reader["typek"] != DBNull.Value) ls.typek = (int)reader["typek"];

								if (reader["stypek"] == DBNull.Value) ls.stypek = "";
								else ls.stypek = Convert.ToString(reader["stypek"]);

								if (reader["is_open"] != DBNull.Value)
								{
									int stateID;
									if (Int32.TryParse(((string)reader["is_open"]).Trim(), out stateID))
										ls.stateID = stateID;
									else
									{
										ls.stateID = 0;
									}
								}
								if (reader["state"] != DBNull.Value) ls.state = ((string)reader["state"]).Trim();

								if (reader["nzp_kvar"] == DBNull.Value) ls.nzp_kvar = 0;
								else ls.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);

								if (reader["nkvar"] != DBNull.Value) ls.nkvar = Convert.ToString(reader["nkvar"]);
								if (reader["nkvar_n"] != DBNull.Value) ls.nkvar_n = Convert.ToString(reader["nkvar_n"]);
								if (reader["phone"] != DBNull.Value) ls.phone = ((string)reader["phone"]).Trim();
								if (reader["porch"] != DBNull.Value) ls.porch = Convert.ToString(reader["porch"]).Trim();
								if (reader["uch"] != DBNull.Value) ls.uch = ((int)reader["uch"]).ToString();
								if (reader["remark"] != DBNull.Value) ls.remark = ((string)reader["remark"]).Trim();
							}
						}
					}

					if (mode == 0 || mode == 1 || mode == 3)
					{
						if (reader["geu"] == DBNull.Value) ls.geu = "";
						else ls.geu = Convert.ToString(reader["geu"]);
					}

					if (mode == 0 || mode == 1 || mode == 4)
					{
						if (reader["area"] == DBNull.Value) ls.area = "";
						else ls.area = Convert.ToString(reader["area"]);
					}

					/*
					IDataReader readerSD;
					if (!ExecRead(conn_db, out readerSD, "select dat_saldo from " + pref + "_data:saldo_date where iscurrent = 0", true).result)
					{
							conn_db.Close();
							return Listls;
					}
					string saldo_date = "";
					while (readerSD.Read())
						if (readerSD["dat_saldo"] != DBNull.Value)
							saldo_date =String.Format("{0:dd.MM.yyyy}",readerSD["dat_saldo"]);

					if (saldo_date != "")
					{
						IDataReader readerDO;
						string s = "select dat_s from " + pref + "_data:prm_3 where nzp_prm=51 and val_prm=1 and is_actual<>100 and nzp=" +
							Convert.ToString(ls.nzp_kvar) + " and \"" + saldo_date + "\" between  dat_s and  dat_po";
						if (!ExecRead(conn_db, out readerDO,s , true).result)
						{
							conn_db.Close();
							return Listls;
						}

						while (readerDO.Read())
							if (readerDO["dat_s"] != DBNull.Value)
								ls.dat_open = String.Format("{0:dd.MM.yyyy}", readerDO["dat_s"]);
					}
					*/

					if (mode == 0 && !GlobalSettings.WorkOnlyWithCentralBank)
					{
						ls.pref = ls.pref;
						ls.stateValidOn = finder.stateValidOn;
						ret = LoadLsState(ls, conn_db, transaction);
						if (!ret.result)
						{
							reader.Close();
							return Listls;
						}

						DateTime d;
						// дата последнего расчета лицевого счета
						if (DateTime.TryParse(finder.dat_calc, out d))
						{
							string kvar_calc = ls.pref + "_charge_" + (d.Year % 100).ToString("00") + ":kvar_calc_" + d.Month.ToString("00");

							if (TempTableInWebCashe(conn_db, transaction, kvar_calc))
							{
								string s = "select dat_calc from " + kvar_calc + " where nzp_kvar = " + finder.nzp_kvar;
								IDataReader readerDO;
								ret = ExecRead(conn_db, transaction, out readerDO, s, true);
								if (!ret.result)
								{
									reader.Close();
									return Listls;
								}
								if (readerDO.Read())
								{
									if (readerDO["dat_calc"] != DBNull.Value) ls.dat_calc = Convert.ToDateTime(readerDO["dat_calc"]).ToShortDateString();
								}
							}
						}
					}

					Listls.Add(ls);
					ret.result = true;
				}

				reader.Close();
			}
			catch (Exception ex)
			{
				reader.Close();
				reader.Dispose();

				ret.result = false;
				ret.text = ex.Message;

				string err;
				if (Constants.Viewerror) err = " \n " + ex.Message;
				else err = "";

				MonitorLog.WriteLog("Ошибка заполнения Адреса " + err, MonitorLog.typelog.Error, 20, 201, true);
			}

			if (ret.result)
			{
				if (!GlobalSettings.WorkOnlyWithCentralBank)
					if (Listls != null && Listls.Count > 0 /*&& (finder.is_pasportist > 0 || Points.Region == Regions.Region.Samarskaya_obl)*/)
					{
						Returns ret2 = Utils.InitReturns();
						List<Prm> list = new List<Prm>();
						Prm finderprm = new Prm();
						finderprm.checkDataBlocking = 0; // не проверять блокировку
						finderprm.pref = Listls[0].pref;
						RecordMonth r_m = Points.GetCalcMonth(new CalcMonthParams(finderprm.pref));
						finderprm.month_ = r_m.month_;
						finderprm.year_ = r_m.year_;


						if (finder.nzp_kvar > 0)
						{
							finderprm.prm_num = 1;
							finderprm.nzp = finder.nzp_kvar;
						}
						else
						{
							finderprm.prm_num = 2;
							finderprm.nzp = finder.nzp_dom;
						}

						if (finder.nzp_kvar > 0) finderprm.nzp_prm = 4;
						else finderprm.nzp_prm = 40;

						DbParameters dbparam = new DbParameters();
						Prm prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
						if (prm == null) prm = new Prm();
						prm.nzp_prm = finderprm.nzp_prm;
						list.Add(prm);

						if (finder.nzp_kvar > 0) finderprm.nzp_prm = 6;
						else finderprm.nzp_prm = 36;
						prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
						if (prm == null) prm = new Prm();
						prm.nzp_prm = finderprm.nzp_prm;
						list.Add(prm);

						if (finder.num_page == Constants.page_carddom)//реквизиты дома
						{
							finderprm.nzp_prm = 2049;//МОП площадь
							prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);

							finderprm.nzp_prm = 4;
							prm = FindPrmValueForOpenLs(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);

							finderprm.nzp_prm = 5;
							prm = FindPrmValueForOpenLs(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);

							finderprm.nzp_prm = 2005;
							prm = FindPrmValueForOpenLs(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);
						}
						else
						{
							finderprm.nzp_prm = 8; //приватизация
							prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);

							finderprm.nzp_prm = 572; //лоджии
							prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);
							finderprm.nzp_prm = 573; //лоджии
							prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);
							finderprm.nzp_prm = 574; //лоджии
							prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);

							finderprm.nzp_prm = 3;//комфортность
							prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);

							finderprm.nzp_prm = 107;//количество комнат в квартире
							prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);

							finderprm.nzp_prm = 2009;//статус жилья
							prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);

							finderprm.nzp_prm = 5;//количество проживающих
							prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);

							finderprm.nzp_prm = 2005;//колич прописанных
							prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);

							finderprm.nzp_prm = 17;//водопровод
							finderprm.prm_num = 1;
							finderprm.nzp = finder.nzp_kvar;
							finderprm.type_prm = "bool";
							prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);

							finderprm.nzp_prm = 15;//канализация
							finderprm.prm_num = 1;
							finderprm.nzp = finder.nzp_kvar;
							finderprm.type_prm = "bool";
							prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);

							finderprm.nzp_prm = 14;//лифт
							finderprm.prm_num = 1;
							finderprm.nzp = finder.nzp_kvar;
							finderprm.type_prm = "bool";
							prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);

							finderprm.nzp_prm = 37;//этажность
							finderprm.prm_num = 2;
							finderprm.nzp = finder.nzp_dom;
							finderprm.type_prm = "";
							prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);

							finderprm.nzp_prm = 150;//год постройки
							finderprm.prm_num = 2;
							finderprm.nzp = finder.nzp_dom;
							finderprm.type_prm = "";
							prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);

							finderprm.nzp_prm = 1240;//тип отопления
							finderprm.prm_num = 1;
							finderprm.nzp = finder.nzp_kvar;
							finderprm.type_prm = "sprav";
							finderprm.nzp_res = 3018;
							prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);

							finderprm.nzp_prm = 2009;//статус жилья
							finderprm.prm_num = 1;
							finderprm.nzp = finder.nzp_kvar;
							finderprm.type_prm = "sprav";
							finderprm.nzp_res = 3001;
							prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);

							finderprm.nzp_prm = 1241;//Наличие электроосвещения
							finderprm.prm_num = 1;
							finderprm.nzp = finder.nzp_kvar;
							finderprm.type_prm = "bool";
							prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);

							finderprm.nzp_prm = 1242;//Наличие ванны
							finderprm.prm_num = 1;
							finderprm.nzp = finder.nzp_kvar;
							finderprm.type_prm = "bool";
							prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);

							finderprm.nzp_prm = 1243;//Наличие газа
							finderprm.prm_num = 1;
							finderprm.nzp = finder.nzp_kvar;
							finderprm.type_prm = "bool";
							prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);

							finderprm.nzp_prm = 1244;//Тип ГВС
							finderprm.prm_num = 1;
							finderprm.nzp = finder.nzp_kvar;
							finderprm.type_prm = "sprav";
							finderprm.nzp_res = 3019;
							prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);

							finderprm.nzp_prm = 35;//Открытый водозабор горячей воды
							finderprm.prm_num = 2;
							finderprm.nzp = finder.nzp_dom;
							finderprm.type_prm = "bool";
							prm = dbparam.FindPrmValue(conn_db, transaction, finderprm, out ret2);
							if (prm == null) prm = new Prm();
							prm.nzp_prm = finderprm.nzp_prm;
							list.Add(prm);
						}

						Listls[0].dopParams = list;
						Listls[0].is_pasportist = finder.is_pasportist;
					}
			}
			return Listls;
		}

		public Prm FindPrmValueForOpenLs(IDbConnection conn_db, IDbTransaction transaction, Prm finder, out Returns ret)
		{
			Prm prm = new Prm();

			DateTime dat = new DateTime(finder.year_, finder.month_, 1);
#if PG
			string prm_N_p = finder.pref + "_data." + "prm_1 p ";
			string prm_3 = finder.pref + "_data." + "prm_3";
			string sqldop = "Select nzp_kvar from " + finder.pref + "_data.kvar k " +
				  " Where k.nzp_dom = " + finder.nzp + " and (select val_prm from " + prm_3 + " p3 where p3.nzp_prm=51 and p3.nzp=k.nzp_kvar " +
				  " and " + Utils.EStrNull(dat.ToShortDateString()) + " between p3.dat_s and p3.dat_po and p3.is_actual <> 100)='1'";
			string type = "numeric(14,2)";
			if (finder.nzp_prm == 5 || finder.nzp_prm == 2005) type = "int";
			string sqlstr = "Select sum(cast(coalesce(p.val_prm,'0') as " + type + ")) as val From " + prm_N_p +
				  " Where  p.nzp_prm = " + finder.nzp_prm + " and p.nzp in (" + sqldop + ") " +
				  " and " + Utils.EStrNull(dat.ToShortDateString()) + " between p.dat_s and p.dat_po and p.is_actual <> 100";
#else
			string prm_N_p = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":" + "prm_1 p ";
			string prm_3 = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":" + "prm_3";
			string sqldop = "Select nzp_kvar from " + finder.pref + "_data@" + DBManager.getServer(conn_db) + ":kvar k " +
				  " Where k.nzp_dom = " + finder.nzp + " and (select val_prm from " + prm_3 + " p3 where p3.nzp_prm=51 and p3.nzp=k.nzp_kvar " +
				  " and " + Utils.EStrNull(dat.ToShortDateString()) + " between p3.dat_s and p3.dat_po and p3.is_actual <> 100)=1";
			string type = "decimal(14,2)";
			if (finder.nzp_prm == 5 || finder.nzp_prm == 2005) type = "int";
			string sqlstr = "Select sum(cast(nvl(p.val_prm,0) as " + type + ")) as val From " + prm_N_p +
				  " Where  p.nzp_prm = " + finder.nzp_prm + " and p.nzp in (" + sqldop + ") " +
				  " and " + Utils.EStrNull(dat.ToShortDateString()) + " between p.dat_s and p.dat_po and p.is_actual <> 100";
#endif

			IDataReader reader;
			ret = ExecRead(conn_db, transaction, out reader, sqlstr, true);
			if (!ret.result)
			{
				reader.Close();
				return prm;
			}
			if (reader.Read())
			{
				if (reader["val"] != DBNull.Value) prm.val_prm = Convert.ToString(reader["val"]);
				prm.nzp_prm = finder.nzp_prm;
			}
			reader.Close();
			return prm;
		}

	   

		//----------------------------------------------------------------------
		/// <summary>
		/// найти и заполнить адрес
		/// </summary>
		/// <param name="finder">если заполнено поле nzp_kvar - информация о л/с,
		/// nzp_dom - информация о доме, nzp_ul - об улице, nzp_area - о Управляющая организация,
		/// nzp_geu - о ЖЭУ
		/// </param>
		/// <param name="ret">результат</param>
		/// <returns>список объектов Ls</returns>
		public List<Ls> LoadLs(Ls finder, out Returns ret) //найти и заполнить адрес для nzp_kvar
		//----------------------------------------------------------------------
		{
			ret = Utils.InitReturns();
			if (finder.nzp_user < 1)
			{
				ret.result = false;
				ret.text = "Не определен пользователь";
				return null;
			}

			List<Ls> Listls = new List<Ls>();

			string pref = finder.pref == "" ? Points.Pref : finder.pref;

			#region соединение с БД
			string conn_kernel = Points.GetConnByPref(pref);
			IDbConnection conn_db = GetConnection(conn_kernel);
			//IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
			ret = OpenDb(conn_db, true);
			if (!ret.result) return Listls;
			#endregion

			Listls = LoadLs(conn_db, null, finder, out ret);

			conn_db.Close();
			return Listls;
		}

		//----------------------------------------------------------------------
		public Returns Generator(List<Prm> listprm, int nzp_user)
		//----------------------------------------------------------------------
		{
			Returns ret = Utils.InitReturns();

			IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
			ret = OpenDb(conn_web, true);
			if (!ret.result) return ret;

			IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
			ret = OpenDb(conn_db, true);
			if (!ret.result)
			{
				conn_web.Close();
				return ret;
			}

			//string tXX_spls = "t" + nzp_user + "_spls";
			//string tXX_spls_full = conn_web.Database + "@" + conn_web.Server + ":" + tXX_spls;


			//постройка данных
			FindPrmAll(conn_web, conn_db, listprm, nzp_user, out ret);

			conn_db.Close();
			conn_web.Close();


			return ret;
		}
		//----------------------------------------------------------------------
		public Returns Generator(List<int> listint, int nzp_user, int yy, int mm)
		//----------------------------------------------------------------------
		{
			return Generator(listint, nzp_user, yy, mm, true);
		}
		//----------------------------------------------------------------------
		public Returns Generator(List<int> listint, int nzp_user, int yy, int mm, bool all_ls)
		//----------------------------------------------------------------------
		{
			Returns ret = Utils.InitReturns();

			IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
			ret = OpenDb(conn_web, true);
			if (!ret.result) return ret;

			IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
			ret = OpenDb(conn_db, true);
			if (!ret.result)
			{
				conn_web.Close();
				return ret;
			}

			//постройка данных
			FindSaldoAll(conn_web, conn_db, listint, nzp_user, yy, mm, all_ls, out ret);

			conn_db.Close();
			conn_web.Close();


			return ret;
		}
		//----------------------------------------------------------------------
		private bool FindSaldoAll(IDbConnection conn_web, IDbConnection conn_db, List<int> listint, int nzp_user, int yy, int mm, bool all_ls, out Returns ret)
		//----------------------------------------------------------------------
		{
			ret = Utils.InitReturns();
			string tXX_spls = "t" + nzp_user + "_spls";
#if PG
			string tXX_spls_full = pgDefaultDb + "." + tXX_spls;
#else
			string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;
#endif

			string tXX_spall = "t" + nzp_user + "_saldoall ";
#if PG
			string tXX_spall_full = pgDefaultDb + "." + tXX_spall;
#else
			string tXX_spall_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spall;
#endif


			/*
			0- Услуга
			1- Поставщик
			2- Тариф
			3- Вход. сальдо
			4- Расчет за месяц
			5- Перерасчет
			6- Изменения
			7- Недопоставка
			8- Оплачено
			9- К оплате
			10-Исход. сальдо
			*/

			string s_sel =
				" Insert into " + tXX_spall_full +
				" Select t.nzp_kvar";

			string s_sel_sum = "";
			string s_table_sum = "";

			string s_sel_serv = "";
			string s_table_serv = "";
			string s_sel_supp = "";
			string s_table_supp = "";

			int groupby = 2;
			string s_table =
				" Create table " + tXX_spall +
				" ( no serial not null, nzp_kvar integer ";

			foreach (int i in listint)
			{
				switch (i)
				{
					case 0:
						{
							s_sel_serv = ", service,ordering ";
							s_table_serv = ", service char(40), ordering integer ";
							groupby += 2;
							break;
						}
					case 1:
						{
							s_sel_supp = ", name_supp ";
							s_table_supp = ", name_supp char(40) ";
							groupby += 1;
							break;
						}
					case 2:
						{
							s_sel_sum += ", max(tarif) as tarif ";
#if PG
							s_table_sum += ", tarif numeric(14,2) default 0.00 ";
#else
							s_table_sum += ", tarif decimal(14,2) default 0.00 ";
#endif

							break;
						}
					case 3:
						{
							s_sel_sum += ", sum(sum_insaldo) as sum_insaldo ";
#if PG
							s_table_sum += ", sum_insaldo numeric(14,2) default 0.00 ";
#else
							s_table_sum += ", sum_insaldo decimal(14,2) default 0.00 ";
#endif

							break;
						}
					case 4:
						{
							s_sel_sum += ", sum(sum_tarif) as sum_tarif ";
#if PG
							s_table_sum += ", sum_tarif numeric(14,2) default 0.00 ";
#else
							s_table_sum += ", sum_tarif decimal(14,2) default 0.00 ";
#endif

							break;
						}
					case 5:
						{
							s_sel_sum += ", sum(reval) as reval ";
#if PG
							s_table_sum += ", reval numeric(14,2) default 0.00 ";
#else
							s_table_sum += ", reval decimal(14,2) default 0.00 ";
#endif

							break;
						}
					case 6:
						{
							s_sel_sum += ", sum(real_charge) as real_charge ";
#if PG
							s_table_sum += ", real_charge numeric(14,2) default 0.00 ";
#else
							s_table_sum += ", real_charge decimal(14,2) default 0.00 ";
#endif

							break;
						}
					case 7:
						{
							s_sel_sum += ", sum(sum_nedop) as sum_nedop ";
#if PG
							s_table_sum += ", sum_nedop numeric(14,2) default 0.00 ";
#else
							s_table_sum += ", sum_nedop decimal(14,2) default 0.00 ";
#endif

							break;
						}
					case 8:
						{
							s_sel_sum += ", sum(sum_money) as sum_money ";
#if PG
							s_table_sum += ", sum_money numeric(14,2) default 0.00 ";
#else
							s_table_sum += ", sum_money decimal(14,2) default 0.00 ";
#endif

							break;
						}
					case 9:
						{
							s_sel_sum += ", sum(sum_charge) as sum_charge ";
#if PG
							s_table_sum += ", sum_charge numeric(14,2) default 0.00 ";
#else
							s_table_sum += ", sum_charge decimal(14,2) default 0.00 ";
#endif

							break;
						}
					case 10:
						{
							s_sel_sum += ", sum(sum_outsaldo) as sum_outsaldo ";
#if PG
							s_table_sum += ", sum_outsaldo numeric(14,2) default 0.00 ";
#else
							s_table_sum += ", sum_outsaldo decimal(14,2) default 0.00 ";
#endif

							break;
						}
				}
			}

			ExecSQL(conn_web, " Drop table " + tXX_spall, false);

			//создать t_saldoall
			s_table = s_table + s_table_serv.Replace("#nzp_serv#", "nzp_serv") + s_table_supp.Replace("#nzp_supp#", "nzp_supp") + s_table_sum + ")";
			ret = ExecSQL(conn_web, s_table, true);
			if (!ret.result)
			{
				return false;
			}
#if PG
			ExecSQL(conn_web, " analyze " + tXX_spall, true);
#else
			ExecSQL(conn_web, " Update statistics for table " + tXX_spall, true);
#endif



			s_sel = s_sel + s_sel_serv + s_sel_supp + s_sel_sum +
				" From " + tXX_spls_full + " t, CHARGE_XX ch, SERVICES_XX s, SUPPLIER_XX p " +
				" Where t.nzp_kvar = ch.nzp_kvar " +
				"   and ch.nzp_serv > 1 and dat_charge is null " +
				"   and ch.nzp_serv = s.nzp_serv " +
				"   and ch.nzp_supp = p.nzp_supp " +
				" Group by 1 ";

			for (int i = 2; i <= groupby; i++)
				s_sel = s_sel + "," + i;


			// цикл по pref 
			IDataReader reader;
#if PG
			if (!ExecRead(conn_db, out reader, " Select distinct pref From " + tXX_spls_full, true).result)
#else
			if (!ExecRead(conn_db, out reader, " Select unique pref From " + tXX_spls_full, true).result)
#endif
			{
				return false;
			}
			try
			{
				//заполнить t_saldoall
				while (reader.Read())
				{
					string pref = (string)(reader["pref"]);
					pref = pref.Trim();

#if PG
					string charge_xx = pref + "_charge_" + (yy % 100).ToString("00") + ".charge_" + mm.ToString("00");
					string services = pref + "_kernel.services ";
					string supplier = pref + "_kernel.supplier ";
#else
					string charge_xx = pref + "_charge_" + (yy % 100).ToString("00") + ":charge_" + mm.ToString("00");
					string services = pref + "_kernel:services ";
					string supplier = pref + "_kernel:supplier ";
#endif


					string s_sql;
					s_sql = s_sel.Replace("CHARGE_XX", charge_xx);
					s_sql = s_sql.Replace("SERVICES_XX", services);
					s_sql = s_sql.Replace("SUPPLIER_XX", supplier);

					ret = ExecSQL(conn_db, s_sql, true);
					if (!ret.result)
					{
						return false;
					}
				}
			}
			catch (Exception ex)
			{
				reader.Close();

				ret.result = false;
				ret.text = ex.Message;

				string err;
				if (Constants.Viewerror)
					err = " \n " + ex.Message;
				else
					err = "";

				MonitorLog.WriteLog("Ошибка заполнения выбранных значений " + err, MonitorLog.typelog.Error, 20, 201, true);

				return false;
			}


			ExecSQL(conn_web, " Create unique index ix1_" + tXX_spall + " on " + tXX_spall + "(no)", true);
			ExecSQL(conn_web, " Create index ix2_" + tXX_spall + " on " + tXX_spall + "(nzp_kvar)", true);
#if PG
			ExecSQL(conn_web, " analyze " + tXX_spall, true);
#else
			ExecSQL(conn_web, " Update statistics for table " + tXX_spall, true);
#endif



			if (all_ls)
			{
				//добавить недостающие лс в saldoall
				ExecSQL(conn_web, " Drop table ttt1_sld ", false);

#if PG
				ExecSQL(conn_web,
									" Select distinct nzp_kvar  Into UNLOGGED ttt1_sld  From " + tXX_spls +
									" Where nzp_kvar not in ( Select nzp_kvar From " + tXX_spall + " ) "
									, true);
#else
				ExecSQL(conn_web,
					" Select unique nzp_kvar From " + tXX_spls +
					" Where nzp_kvar not in ( Select nzp_kvar From " + tXX_spall + " ) " +
					" Into temp ttt1_sld With no log "
					, true);
#endif

				ExecSQL(conn_web,
					" Insert into " + tXX_spall + " (nzp_kvar) Select nzp_kvar From ttt1_sld "
					, true);

				ExecSQL(conn_web, " Drop table ttt1_sld ", false);

#if PG
				ExecSQL(conn_web, " analyze " + tXX_spall, true);
#else
				ExecSQL(conn_web, " Update statistics for table " + tXX_spall, true);
#endif

			}


			//пример выборки
			/*
			s_sel = " Select a.* From t1_saldoall a, t_spls b Where a.nzp_kvar = b.nzp_kvar Order by b.num_ls";
			string s_order = "";
			if (isTableHasColumn(conn_web, "t1_saldoall", "ordering"))
				s_order = " ordering ";
			if (isTableHasColumn(conn_web, "t1_saldoall", "name_supp"))
				s_order += ", name_supp ";
			s_sel += s_order;
			*/

			return true;
		}
		//----------------обертка для FindSaldoAll-------------------------
		public bool FindSaldoAll(string conn_web, string conn_db, List<int> listint, int nzp_user, int yy, int mm, out Returns ret)
		{
			ret = Utils.InitReturns();

			IDbConnection conn_web_ = GetConnection(conn_web);//new IDbConnection(conn_web);
			IDbConnection conn_db_ = GetConnection(conn_db);//new IDbConnection(conn_db);

			ret = OpenDb(conn_web_, true);
			if (ret.result)
			{
				ret = OpenDb(conn_db_, true);
			}
			if (!ret.result)
			{
				MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
				return false;
			}

			return this.FindSaldoAll(conn_web_, conn_db_, listint, nzp_user, yy, mm, true, out ret);
		}


		//----------------------------------------------------------------------
		private bool FindPrmAll(IDbConnection conn_web, IDbConnection conn_db, List<Prm> listprm, int nzp_user, out Returns ret)
		//----------------------------------------------------------------------
		{
#if PG
			string tXX_spls_full ="public"+ ".t" + nzp_user + "_spls";
#else
			string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":t" + nzp_user + "_spls";
#endif

			ret = Utils.InitReturns();

			int l_prm_xx = 30;  //максимальное число prm_xx
			int l_nzp_prm = 100; //максимальное кол-во nzp_prm

			bool[] ar = new bool[l_prm_xx];

			ExecSQL(conn_db, " Drop table ttt_prmall ", false);

			if (!(listprm != null && listprm.Count > 0))
			{
				return false;
			}

			string p_1 = "";

			foreach (Prm prm in listprm)
			{
				ar[prm.prm_num] = true;
				p_1 += "," + prm.nzp_prm;
			}

			string sqlp = "";
			bool b_first = true;

			//-------------------- цикл по pref ------------------------
			IDataReader reader;
#if PG
			if (!ExecRead(conn_db, out reader, " Select distinct pref From " + tXX_spls_full, true).result)
#else
			if (!ExecRead(conn_db, out reader, " Select unique pref From " + tXX_spls_full, true).result)
#endif

			{
				return false;
			}
#if PG
			string into_temp = " INTO    UNLOGGED  ttt_prmall";
#else

#endif
			try
			{
				while (reader.Read())
				{
					string pref = (string)(reader["pref"]);
					pref = pref.Trim();

					sqlp = "";

					for (int i = 1; i <= l_prm_xx - 1; i++)
					{
						if (!ar[i]) continue;

						if (sqlp != "")
						{
							sqlp += " Union ";
#if PG
							into_temp = "";
#else

#endif
						}

#if PG                        
						sqlp +=
												" Select distinct '" + pref + "' as pref, t.nzp_kvar, p.nzp_prm, p.val_prm, n.nzp_res, 0 as nzp_yy " + into_temp+
												" From " + tXX_spls_full + " t, " + pref + "_data.prm_" + i + " p, " + pref + "_kernel.prm_name n " +
												" Where p.nzp_prm = n.nzp_prm " +
												"   and p.nzp_prm in (-1" + p_1 + ")" +
												"   and p.is_actual <> 100 " +
												"   and p.dat_s <= current_date " +
												"   and p.dat_po>= current_date ";
#else
						sqlp +=
												" Select unique '" + pref + "' as pref, t.nzp_kvar, p.nzp_prm, p.val_prm, n.nzp_res, 0 as nzp_yy " +
												" From " + tXX_spls_full + " t, " + pref + "_data:prm_" + i + " p, " + pref + "_kernel:prm_name n " +
												" Where p.nzp_prm = n.nzp_prm " +
												"   and p.nzp_prm in (-1" + p_1 + ")" +
												"   and p.is_actual <> 100 " +
												"   and p.dat_s <= today " +
												"   and p.dat_po>= today ";
#endif



						if (i == 1 || i == 3 || i == 15)
							sqlp += " and t.nzp_kvar = p.nzp ";
						else
							sqlp += " and t.nzp_dom = p.nzp ";
					}

					if (b_first)
					{
#if PG
					
#else
						sqlp += " Into temp ttt_prmall With no log ";
#endif

						b_first = false;

						ret = ExecSQL(conn_db, sqlp, true);
						if (!ret.result)
						{
							return false;
						}

						ret = ExecSQL(conn_db, " Create index ix1_ttt_prmall on ttt_prmall (nzp_kvar,nzp_prm,nzp_res) ", true);
						if (!ret.result)
						{
							return false;
						}
						ret = ExecSQL(conn_db, " Create index ix2_ttt_prmall on ttt_prmall (pref,nzp_res) ", true);
						if (!ret.result)
						{
							return false;
						}
					}
					else
					{
						sqlp = " Insert into ttt_prmall (pref,nzp_kvar,nzp_prm,val_prm,nzp_res,nzp_yy) Select * from ( " + sqlp + " )";

						ret = ExecSQL(conn_db, sqlp, true);
						if (!ret.result)
						{
							return false;
						}
					}

#if PG
					ExecSQL(conn_db, " analyze ttt_prmall ", true);
#else
					ExecSQL(conn_db, " Update statistics for table ttt_prmall ", true);
#endif


					//заменить справочные значения
#if PG
					ret = ExecSQL(conn_db,
									 " Update ttt_prmall " +
									 " SET nzp_yy = CAST(val_prm as INTEGER) +0 " +
									 " Where pref = '" + pref + "'" +
									   " and nzp_res is not null "
									 , true);
#else
					ret = ExecSQL(conn_db,
									 " Update ttt_prmall " +
									 " Set nzp_yy = val_prm+0 " +
									 " Where pref = '" + pref + "'" +
									   " and nzp_res is not null "
									 , true);
#endif
					if (!ret.result)
					{
						return false;
					}
#if PG
					ret = ExecSQL(conn_db,
						" Update ttt_prmall " +
						" Set val_prm = ( Select max(name_y) From " + pref + "_kernel.res_y ry " +
										" Where ttt_prmall.nzp_res = ry.nzp_res and nzp_yy = ry.nzp_y )::char(20) " +
						" Where pref = '" + pref + "'" +
						"   and nzp_res is not null " +
						"   and nzp_res in ( Select nzp_res From " + pref + "_kernel.res_y  ) "
						, true);
#else
					ret = ExecSQL(conn_db,
						" Update ttt_prmall " +
						" Set val_prm = ( Select max(name_y) From " + pref + "_kernel:res_y ry " +
										" Where ttt_prmall.nzp_res = ry.nzp_res and nzp_yy = ry.nzp_y ) " +
						" Where pref = '" + pref + "'" +
						"   and nzp_res is not null " +
						"   and nzp_res in ( Select nzp_res From " + pref + "_kernel:res_y  ) "
						, true);
#endif

					if (!ret.result)
					{
						return false;
					}


				}
			}
			catch (Exception ex)
			{
				reader.Close();

				ret.result = false;
				ret.text = ex.Message;

				string err;
				if (Constants.Viewerror)
					err = " \n " + ex.Message;
				else
					err = "";

				MonitorLog.WriteLog("Ошибка заполнения выбранных значений " + err, MonitorLog.typelog.Error, 20, 201, true);

				return false;
			}





			//построить таблицу по выбранным параметрам!
			string tXX_spall = "t" + nzp_user + "_prmall ";
#if PG
			string tXX_spall_full = "public." + tXX_spall;
#else
			string tXX_spall_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spall;
#endif

			ExecSQL(conn_web, " Drop table " + tXX_spall, false);

			sqlp = " Create table " + tXX_spall +
				   " ( nzp_kvar integer ";

			int l_cur = 0;
			int[] ar_prm = new int[l_nzp_prm];



			//IDataReader reader;
#if PG
			if (!ExecRead(conn_db, out reader, " Select distinct nzp_prm From ttt_prmall ", true).result)
#else
			if (!ExecRead(conn_db, out reader, " Select unique nzp_prm From ttt_prmall ", true).result)
#endif

			{
				return false;
			}
			try
			{
				while (reader.Read())
				{
					l_cur += 1;
					if (l_cur > l_nzp_prm) break;

					int nzp_prm = (int)(reader["nzp_prm"]);
					//sqlp += " ,val_" + nzp_prm + " char(40)";

					ar_prm[l_cur] = nzp_prm;
				}
			}
			catch (Exception ex)
			{
				reader.Close();

				ret.result = false;
				ret.text = ex.Message;

				string err;
				if (Constants.Viewerror)
					err = " \n " + ex.Message;
				else
					err = "";

				MonitorLog.WriteLog("Ошибка заполнения выбранных значений " + err, MonitorLog.typelog.Error, 20, 201, true);

				return false;
			}

			//надо соблюсти порядок следования параметров!
			foreach (Prm prm in listprm)
			{
				for (int i = 1; i <= l_cur; i++)
				{
					if (ar_prm[i] == prm.nzp_prm)
						sqlp += " ,val_" + ar_prm[i] + " char(40)";
				}
			}
			sqlp += " ) ";


			//создать таблицу
			ret = ExecSQL(conn_web, sqlp, true);
			if (!ret.result)
			{
				reader.Close();
				return false;
			}

			//вставить лицевые счета
#if PG
			ret = ExecSQL(conn_db,
				" Insert into " + tXX_spall_full + " (nzp_kvar) Select distinct nzp_kvar From ttt_prmall "
				, true);
#else
			ret = ExecSQL(conn_db,
				" Insert into " + tXX_spall_full + " (nzp_kvar) Select unique nzp_kvar From ttt_prmall "
				, true);
#endif

			if (!ret.result)
			{
				return false;
			}

			ret = ExecSQL(conn_web, " Create index ix_" + tXX_spall + " on " + tXX_spall + " (nzp_kvar) ", true);
			if (!ret.result)
			{
				return false;
			}
#if PG
			ExecSQL(conn_web, " analyze " + tXX_spall, true);
#else
			ExecSQL(conn_web, " Update statistics for table " + tXX_spall, true);
#endif




			//выставить значения в строку
			for (int i = 1; i <= l_cur; i++)
			{
				ret = ExecSQL(conn_db,
					" Update " + tXX_spall_full +
					" Set val_" + ar_prm[i] + " = ( " +
								" Select max(val_prm) From ttt_prmall p " +
								" Where p.nzp_kvar = " + tXX_spall_full + ".nzp_kvar " +
								"   and p.nzp_prm = " + ar_prm[i] + " ) " +
					" Where 0 < ( Select count(*) From ttt_prmall p " +
								" Where p.nzp_kvar = " + tXX_spall_full + ".nzp_kvar " +
								"   and p.nzp_prm = " + ar_prm[i] + " ) "
					, true);
				if (!ret.result)
				{
					return false;
				}
			}

			ExecSQL(conn_db, " Drop table ttt_prmall ", false);


			return true;
		}
		//----------------------------------------------------------------------
		private bool FindPrmOverLs(IDbConnection conn_db, Ls finder, out Returns ret, out bool fls, out bool fdom)
		//----------------------------------------------------------------------
		{
			ret = Utils.InitReturns();
			fls = false;
			fdom = false;

			ExecSQL(conn_db, " Drop table ttt_prmls ", false);

			if (finder.num_ls > 0 || !string.IsNullOrEmpty(finder.pkod))
				return false;

			if (finder.dopParams != null && finder.dopParams.Count > 0)
			{
				string sqlp = "";
				string prm_n;

				foreach (Prm prm in finder.dopParams)
				{
#if PG
					prm_n = finder.pref + "_data.prm_" + prm.prm_num;
#else
					prm_n = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":prm_" + prm.prm_num;
#endif


					if (!TempTableInWebCashe(conn_db, null, prm_n)) continue;
					if (!fls) fls = (prm.prm_num == 1 || prm.prm_num == 3);
					if (!fdom) fdom = (prm.prm_num == 2 || prm.prm_num == 4);

					if (sqlp != "") sqlp += " Union ";                           

					string date_where = "";

					if (!string.IsNullOrEmpty(prm.dat_po))
					{
						date_where += " and dat_s <= '" + prm.dat_po + "'";
					}
					if (!string.IsNullOrEmpty(prm.dat_s))
					{
						date_where += " and dat_po >= '" + prm.dat_s + "'";
					}

					if (!string.IsNullOrEmpty(prm.dat_when_po))
					{
						date_where += " and dat_when <= '" + prm.dat_when_po + "'";
					}

					if (!string.IsNullOrEmpty(prm.dat_when))
					{
						if (string.IsNullOrEmpty(prm.dat_when_po))
							date_where += " and dat_when = '" + prm.dat_when + "'";
						else
							date_where += " and dat_when >= '" + prm.dat_when + "'";
					}

#if PG
					string val = "coalesce(val_prm,'0')";
#else
					string val = "nvl(val_prm,'0')";
#endif

					string ss = "'";

					if (prm.type_prm == "date")
					{
#if PG
						val = " cast (coalesce(val_prm,to_date('1,1,1991', 'MM,DD,YYYY')) as date) ";
#else
						val = " cast (nvl(val_prm,mdy(1,1,1991)) as date) ";
#endif

					}
					if (prm.type_prm == "int")
					{
#if PG
						val = "coalesce(val_prm,'0')::int";
#else
						val = "nvl(val_prm,'0')+0";
#endif

						ss = "";
					}
					if (prm.type_prm == "float")
					{
#if PG
						val = "coalesce(val_prm,'0')::numeric(14,2) ";
#else
						val = "nvl(val_prm,'0')+0.00";

#endif
						ss = "";


					}

					if (prm.type_prm == "bool")
					{
						prm.val_prm = prm.val_prm == "Да" ? "1" : "0";
					}


					//Ошибка Эли!!
					if ((prm.type_prm == "sprav") && (prm.val_prm == "-1"))
					{
						prm.val_prm = "";
					}

					if (prm.criteria == enCriteria.missing) // если поиск по отсутствию параметра
					{
						sqlp += " select " + prm.prm_num + " as tab, nzp_kvar as nzp  From " + finder.pref + "_data" + tableDelimiter + "kvar where nzp_kvar not in " +
							" (select distinct nzp from " + finder.pref + "_data" + tableDelimiter + "prm_" + prm.prm_num +
							" Where is_actual <> 100 " + date_where + " group by nzp, nzp_prm having nzp_prm = " + prm.nzp_prm + " ) "; 
					}
					else
					{
						sqlp += " Select " + prm.prm_num + " as tab, nzp  From " + finder.pref + "_data" + tableDelimiter + "prm_" + prm.prm_num +
							" Where is_actual <> 100 and nzp_prm = " + prm.nzp_prm + date_where;

						if (!string.IsNullOrEmpty(prm.val_prm) && !string.IsNullOrEmpty(prm.val_prm_po))
						{
							string prm_val_prm = ss + prm.val_prm + ss;
							string prm_val_prm_po = ss + prm.val_prm_po + ss;
#if PG
							string s1 = val + " >= '" + prm_val_prm + "' and " + val + " <= '" + prm_val_prm_po +"'";
#else
							string s1 = val + " >= " + prm_val_prm + " and " + val + " <= " + prm_val_prm_po;
#endif
							if (prm.criteria == enCriteria.not_equal)
								s1 = " not (" + s1 + ")";

							sqlp += " and " + s1;
						}
						else
						{
							if (!string.IsNullOrEmpty(prm.val_prm))
							{
								string prm_val_prm = ss + prm.val_prm + ss;

								string s1 = " = ";

								if (prm.criteria == enCriteria.not_equal)
									s1 = " <> ";

								sqlp += " and " + val + s1 + prm_val_prm;
							}
						}
					}
				}

				if (sqlp != "")
				{
#if PG
					sqlp = sqlp.AddIntoStatement("Into temp ttt_prmls");
#else
					sqlp += " Into temp ttt_prmls With no log ";
#endif


					ret = ExecSQL(conn_db, sqlp, true);
					if (!ret.result)
					{
						return false;
					}

					ret = ExecSQL(conn_db, " Create index ix1_ttt_prmls on ttt_prmls (tab,nzp) ", true);
					if (!ret.result)
					{
						return false;
					}
					ret = ExecSQL(conn_db, " Create index ix2_ttt_prmls on ttt_prmls (nzp,tab) ", true);
					if (!ret.result)
					{
						return false;
					}

#if PG
					ExecSQL(conn_db, " analyze ttt_prmls ", true);
#else
					ExecSQL(conn_db, " Update statistics for table ttt_prmls ", true);
#endif

					return true;
				}
				else
				{
					return false;
				}
			}
			else
				return false;
		}


		//----------------------------------------------------------------------
		public void FindLs(Ls finder, out Returns ret) //найти и заполнить список адресов
		//----------------------------------------------------------------------
		{
			ret = Utils.InitReturns();

			if (!Points.IsFabric || (finder.nzp_wp > 0))
			{
				//поиск в конкретном банке
				FindLs00(finder, out ret, 0);
			}
			else
			{
				//параллельный поиск по серверам БД
				FindLsInThreads(finder, out ret);
			}
		}
		//----------------------------------------------------------------------
		private void FindLsInThreads(Ls finder, out Returns ret) //
		//----------------------------------------------------------------------
		{
			ret = Utils.InitReturns();
			if (finder.nzp_user < 1)
			{
				ret.result = false;
				ret.text = "Не определен пользователь";
				return;
			}

			IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
			ret = OpenDb(conn_web, true);
			if (!ret.result) return;

			string tXX_meta = "t" + Convert.ToString(finder.nzp_user) + "_meta";

			//создать таблицу контроля
			if (TableInWebCashe(conn_web, tXX_meta))
			{
				ExecSQL(conn_web, " Drop table " + tXX_meta, false);
			}

#if PG
			ret = ExecSQL(conn_web,
					  " Create table " + tXX_meta +
					  " ( nzp_server integer," +
					  "   dat_in     timestamp, " +
					  "   dat_work   timestamp, " +
					  "   dat_out    timestamp, " +
					  "   kod        integer default 0 " +
					  " ) ", true);
#else
			ret = ExecSQL(conn_web,
					  " Create table " + tXX_meta +
					  " ( nzp_server integer," +
					  "   dat_in     datetime year to minute, " +
					  "   dat_work   datetime year to minute, " +
					  "   dat_out    datetime year to minute, " +
					  "   kod        integer default 0 " +
					  " ) ", true);
#endif

			if (!ret.result)
			{
				conn_web.Close();
				return;
			}
#if PG
#else
			ret = ExecSQL(conn_web, " Alter table " + tXX_meta + "  lock mode (row) ", true);
#endif

			//открываем цикл по серверам БД
			foreach (_Server server in Points.Servers)
			{
				int nzp_server = server.nzp_server;
				System.Threading.Thread thServer =
								new System.Threading.Thread(delegate() { FindLs01(finder, nzp_server); });
				thServer.Start();
				//MyThread1.Join();
			}

			//а пока создаим кэш-таблицы лицевых счетов и домов, чтобы время не терять пока потоки делают свою работу
			string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spls";
			string tXX_spdom = "t" + Convert.ToString(finder.nzp_user) + "_spdom";

			ret = CreateTableWebLs(conn_web, tXX_spls, true);
			if (!ret.result)
			{
				conn_web.Close();
				return;
			}

			CreateTableWebDom(conn_web, tXX_spdom, true, out ret);
			if (!ret.result)
			{
				conn_web.Close();
				return;
			}

			Thread.Sleep(1000); //подождем, чтобы процессы стартовали

			//дождаться и соединить результаты
			while (true)
			{
				IDataReader reader;
				string sql = " Select * From " + tXX_meta + " Where kod in (-1,0,1) ";

				ret = ExecRead(conn_web, out reader, sql, true);
				if (!ret.result)
				{
					conn_web.Close();
					return;
				}

				bool b = true;
				try
				{
					while (reader.Read())
					{
						int kod = (int)reader["kod"];
						int nzp_server = (int)reader["nzp_server"];

						if (kod == 1) //банк готов!
						{
							//заполняем буфер в кеше
							string tXX_spls_local = "t" + Convert.ToString(finder.nzp_user) + "_spls" + nzp_server;
							string tXX_spdom_local = "t" + Convert.ToString(finder.nzp_user) + "_spdom" + nzp_server;

							ret = ExecSQL(conn_web, " Insert into " + tXX_spls + " Select * From " + tXX_spls_local, true);
							if (!ret.result)
							{
								reader.Close();
								conn_web.Close();
								return;
							}
							ret = ExecSQL(conn_web, " Insert into " + tXX_spdom + " Select * From " + tXX_spdom_local, true);
							if (!ret.result)
							{
								reader.Close();
								conn_web.Close();
								return;
							}

							//признак, что буфер заполнен
							ret = ExecSQL(conn_web,
								" Update " + tXX_meta +
								" Set kod = 2, dat_out = current  " +
								" Where nzp_server = " + nzp_server, true);
							if (!ret.result)
							{
								reader.Close();
								conn_web.Close();
								return;
							}

							//reader.Close();
							b = false;
							break;

						}
						if (kod == -1) //ошибка выполнения!!
						{
							reader.Close();
							conn_web.Close();
							ret.result = false;
							return;
						}
						if (kod == 0) //еще есть невыполненные задания
						{
							b = false;
							break;
						}
					}
				}
				catch (Exception ex)
				{
					conn_web.Close();

					ret.result = false;
					ret.text = ex.Message;

					MonitorLog.WriteLog("Ошибка контроля выполнения " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
					return;
				}
				reader.Close();

				if (b)
				{
					//все потоки выполнились, выходим 
					break;
				}
				Thread.Sleep(1000); //продолжаем ждать
			}




			//построим индексы
			ret = CreateTableWebLs(conn_web, tXX_spls, false);
			if (!ret.result)
			{
				conn_web.Close();
				return;
			}
			CreateTableWebDom(conn_web, tXX_spdom, false, out ret);
			if (!ret.result)
			{
				conn_web.Close();
				return;
			}

			conn_web.Close();
		}
		//----------------------------------------------------------------------
		private void FindLs01(Ls finder, int nzp_server) //вызов из потока
		//----------------------------------------------------------------------
		{
			Returns ret = new Returns();
			FindLs00(finder, out ret, nzp_server);

			if (!ret.result)
			{
				//вылетел по ошибке - надо собщить контролю
				IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
				ret = OpenDb(conn_web, true);
				if (!ret.result) return;

				string tXX_meta = "t" + Convert.ToString(finder.nzp_user) + "_meta";

				//создать таблицу контроля
				ExecSQL(conn_web,
					" Update " + tXX_meta +
					" Set kod = -1, dat_work = current " +
					" Where nzp_server = " + nzp_server, true);

				conn_web.Close();
			}
		}

		private void AddToUserProc(Finder finder, string table_name, string procId, IDbConnection conn_web)
		{
#if PG
			ExecSQL(conn_web,
					" Delete from " + pgDefaultDb + "." + "user_processes where table_name = '" + table_name + "'", true);
			ExecSQL(conn_web, "insert into " + pgDefaultDb + "." + "user_processes (nzp_user, table_name, procId) " +
				"values (" + finder.nzp_user + ",'" + table_name + "', '" + procId + "')", true);
#else
			ExecSQL(conn_web,
					" Delete from " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "user_processes where table_name = '" + table_name + "'", true);
			ExecSQL(conn_web, "insert into " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "user_processes (nzp_user, table_name, procId) " +
				"values (" + finder.nzp_user + ",'" + table_name + "', '" + procId + "')", true);
#endif

		}

		private string GetUserProcId(string table_name, IDbConnection conn_web, out Returns ret)
		{
#if PG
			string sql = "select procId from " + pgDefaultDb + "." + "user_processes " +
				" where table_name = '" + table_name + "'";
#else
			string sql = "select procId from " + conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "user_processes " +
				" where table_name = '" + table_name + "'";
#endif

			IDataReader reader;
			ret = ExecRead(conn_web, out reader, sql, true);
			if (!ret.result) return "";
			string prId = "";
			try
			{
				if (reader.Read())
					if (reader["procId"] != DBNull.Value)
						prId = ((string)reader["procId"]).Trim();
				reader.Close();
				reader.Dispose();
			}
			catch (Exception ex)
			{
				ret = new Returns(false, ex.Message);
				MonitorLog.WriteLog("Ошибка GetUserProcId\n " + ex.Message, MonitorLog.typelog.Error, true);
			}
			return prId;
		}

		private void FindLs00(Ls finder, out Returns ret, int nzp_server) //найти и заполнить список адресов
		//----------------------------------------------------------------------
		{
			ret = Utils.InitReturns();

			#region Проверка finder
			if (finder.nzp_user < 1)
			{
				ret.result = false;
				ret.text = "Не определен пользователь";
				return;
			}

			string conn_kernel = Points.GetConnKernel(finder.nzp_wp, nzp_server);
			if (conn_kernel == "")
			{
				ret.result = false;
				ret.text = "Не определен connect к БД";
				return;
			}
			#endregion

			#region соединение conn_web
			IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
			ret = OpenDb(conn_web, true);
			if (!ret.result) return;
			#endregion

			#region проверка существования таблицы user_processes в БД
#if PG
			if (!TempTableInWebCashe(conn_web, pgDefaultDb + "." + "user_processes"))
#else
			if (!TempTableInWebCashe(conn_web, conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + "user_processes"))
#endif

			{
				ret.result = false;
				ret.text = "Нет таблицы user_processes";
				conn_web.Close();
				MonitorLog.WriteLog("Ошибка FindLs00: Нет таблицы user_processes ", MonitorLog.typelog.Error, 20, 201, true);
				return;
			}
			#endregion

			#region наименование таблиц tXX_spls/tXX_spls_full/tXX_meta
			string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spls";
			if (Utils.GetParams(finder.prms, Constants.page_perechen_lsdom)) tXX_spls += "dom";
			if (nzp_server > 0) tXX_spls += nzp_server.ToString();
#if PG
			string tXX_spls_full = pgDefaultDb + "." + tXX_spls;
#else
			string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;
#endif

			string tXX_meta = "t" + Convert.ToString(finder.nzp_user) + "_meta";
			#endregion

			#region сообщить контролю, что процесс стартовал
			if (nzp_server > 0)
				ExecSQL(conn_web,
					" Insert into " + tXX_meta + "(nzp_server, dat_in) " +
					" Values (" + nzp_server + ", current )", true);
			#endregion

			string procId = Guid.NewGuid().ToString();

			#region обновить данные в таблице user_processes
			AddToUserProc((Finder)finder, tXX_spls, procId, conn_web);
			#endregion

			#region сохранение finder
			SaveFinder(finder, Constants.page_spisls);
			#endregion

			#region соединение conn_db
			IDbConnection conn_db = GetConnection(conn_kernel);
			ret = OpenDb(conn_db, true);
			if (!ret.result)
			{
				conn_web.Close();
				return;
			}
			#endregion
			DbTables tables = new DbTables(conn_db);
			#region заполнение whereString
			string cur_pref;
			string whereString = " and d.nzp_dom = -1111 "; //чтобы ничего не выбиралось

			if (finder.nzp_dom > 0) whereString = " and k.nzp_dom = " + finder.nzp_dom;
			else if (finder.num_ls > Constants._ZERO_) whereString = " and k.num_ls = " + finder.num_ls;
			else if (finder.pkod != "") whereString = " and k.pkod = " + finder.pkod;
			else if (finder.pkod10 > 0 && !Points.IsSmr) whereString = " and k.pkod10 = " + finder.pkod10;
			else
			{
				StringBuilder swhere = new StringBuilder();
				int i;

				if (finder.pkod10 > 0 && Points.IsSmr) swhere.Append(" and k.pkod10 = " + finder.pkod10);

#if PG
				if (finder.stateID > 0) swhere.Append(" and cast(k.is_open as integer) = " + finder.stateID);
#else
				if (finder.stateID > 0) swhere.Append(" and k.is_open = " + finder.stateID);

#endif

				else if (finder.stateIDs != null && finder.stateIDs.Count > 0)
				{
#if PG
					string states = " and cast(k.is_open as integer) in (" + finder.stateIDs[0];
#else
					string states = " and k.is_open in (" + finder.stateIDs[0];

#endif

					for (int k = 1; k < finder.stateIDs.Count; k++) states += "," + finder.stateIDs[k];
					states += ") ";
					swhere.Append(states);
				}
				else swhere.Append(" and k.is_open in ('" + Ls.States.Open.GetHashCode() + "','" + Ls.States.Closed.GetHashCode() + "')");

				if (finder.typek > 0) swhere.Append(" and k.typek = " + finder.typek.ToString());

				if (finder.uch.Trim() != "") swhere.Append(" and k.uch = " + Convert.ToInt32(finder.uch));

				if (finder.nzp_kvar > 0) swhere.Append(" and k.nzp_kvar = " + finder.nzp_kvar.ToString());

				if (finder.nzp_raj > 0 && finder.nzp_town > 0)
				{
					swhere.Append("and d.nzp_ul in (select u.nzp_ul from " + tables.ulica +
						" u where u.nzp_ul=d.nzp_ul and u.nzp_raj = " + finder.nzp_raj +
						" and u.nzp_raj in (select r.nzp_raj from " + tables.rajon + " r where " +
						" r.nzp_town = " + finder.nzp_town + " and u.nzp_raj = r.nzp_raj))");
				}
				else if (finder.nzp_raj > 0)
				{
					swhere.Append("and d.nzp_ul in (select u.nzp_ul from " + tables.ulica +
						" u where u.nzp_ul=d.nzp_ul and u.nzp_raj = " + finder.nzp_raj + ")");
				}
				else if (finder.nzp_town > 0)
				{
					swhere.Append("and d.nzp_ul in (select u.nzp_ul from " + tables.ulica +
						" u where u.nzp_ul=d.nzp_ul " +
						" and u.nzp_raj in (select r.nzp_raj from " + tables.rajon + " r where " +
						" r.nzp_town = " + finder.nzp_town + " and u.nzp_raj = r.nzp_raj))");
				}

				if (finder.list_nzp_area != null && finder.list_nzp_area.Count > 0)
				{
					string str = "";
					for (int it = 0; it < finder.list_nzp_area.Count; it++)
					{
						if (it == 0) str += finder.list_nzp_area[it];
						else str += ", " + finder.list_nzp_area[it];
					}
					if (str != "") swhere.Append(" and k.nzp_area in (" + str + ")");
				}
				else if (finder.nzp_area > 0) swhere.Append(" and k.nzp_area = " + finder.nzp_area.ToString());

				if (finder.nzp_geu > 0) swhere.Append(" and k.nzp_geu = " + finder.nzp_geu.ToString());

				if (finder.nzp_ul > 0) swhere.Append(" and d.nzp_ul = " + finder.nzp_ul.ToString());

				if (finder.ndom_po != "")
				{
					i = Utils.GetInt(finder.ndom_po);
					if (i > 0) swhere.Append(" and d.idom <= " + i.ToString());

					i = Utils.GetInt(finder.ndom);
					if (i > 0) swhere.Append(" and d.idom >= " + i.ToString());
				}
				else if (finder.ndom != "") swhere.Append(" and upper(d.ndom) = " + Utils.EStrNull(finder.ndom.ToUpper()));

				if (finder.nkor != "") swhere.Append(" and upper(d.nkor) = " + Utils.EStrNull(finder.nkor.ToUpper()));

				if (finder.nkvar_po != "")
				{
					i = Utils.GetInt(finder.nkvar_po);
					if (i > 0) swhere.Append(" and k.ikvar <= " + i.ToString());

					i = Utils.GetInt(finder.nkvar);
					if (i > 0) swhere.Append(" and k.ikvar >= " + i.ToString());
				}
				else if (finder.nkvar != "") swhere.Append(" and k.nkvar = " + Utils.EStrNull(finder.nkvar));

				if (finder.fio != "") swhere.Append(" and upper(k.fio) like '%" + finder.fio.ToUpper() + "%'");

				if (finder.phone != "") swhere.Append(" and upper(k.phone) like '%" + finder.phone.ToUpper() + "%'");

				if (finder.nzp_wp > 0) swhere.Append(" and k.nzp_wp = " + finder.nzp_wp);
				else if (finder.dopPointList != null && finder.dopPointList.Count > 0)
				{
					if (finder.dopPointList.Count == 1) swhere.Append(" and k.nzp_wp = " + finder.dopPointList[0]);
					else
					{
						string s = " and k.nzp_wp in (" + finder.dopPointList[0];
						for (int k = 1; k < finder.dopPointList.Count; k++) s += "," + finder.dopPointList[k];
						s += ")";
						swhere.Append(s);
					}
				}

				whereString = swhere.ToString();
			}

			if (finder.RolesVal != null)
				foreach (_RolesVal role in finder.RolesVal)
				{
					if (role.tip == Constants.role_sql)
					{
						if (role.kod == Constants.role_sql_area) whereString += " and k.nzp_area in (" + role.val + ")";
						else if (role.kod == Constants.role_sql_wp) whereString += " and k.nzp_wp in (" + role.val + ")";
						else if (role.kod == Constants.role_sql_geu) whereString += " and k.nzp_geu in (" + role.val + ")";
					}
				}
			#endregion



			#region выборка из таблиц kvar,dom во временную таблицу t_selected_kvar по условию whereString
			ExecSQL(conn_db, "drop table t_selected_kvar", false);

#if PG
			string sql = "select k.* into temp t_selected_kvar from " + tables.kvar + " k, " + tables.dom
						 + " d Where k.nzp_dom = d.nzp_dom and k.nzp_wp is not null " + whereString;
#else
			string sql = "Select k.* from " + tables.kvar + " k, " + tables.dom + " d Where k.nzp_dom = d.nzp_dom and k.nzp_wp is not null " + whereString +
							" into temp t_selected_kvar with no log";
#endif


			int key = LogSQL(conn_web, finder.nzp_user, tXX_spls_full + ": " + sql);

			ret = ExecSQL(conn_db, sql, true);
			if (!ret.result)
			{
				if (key > 0) LogSQL_Error(conn_web, key, ret.text);

				conn_db.Close();
				conn_web.Close();
				return;
			}

			ret = ExecSQL(conn_db, "Create index ix_selected_kvar_1 on t_selected_kvar (nzp_kvar)", true);
			ret = ExecSQL(conn_db, "Create index ix_selected_kvar_2 on t_selected_kvar (pref)", true);
			ret = ExecSQL(conn_db, "Create index ix_selected_kvar_3 on t_selected_kvar (num_ls)", true);
			ret = ExecSQL(conn_db, "Create index ix_selected_kvar_4 on t_selected_kvar (nzp_dom)", true);
#if PG
			ret = ExecSQL(conn_db, "analyze t_selected_kvar", true);
#else
			ret = ExecSQL(conn_db, "Update statistics for table t_selected_kvar", true);
#endif

			#endregion

			#region удаление временной таблицы temp_tXX_spls(содержит результат поиска)
			ExecSQL(conn_db, "drop table temp_tXX_spls", false);
			#endregion

			#region создание temp_tXX_spls
#if PG
			ret = ExecSQL(conn_db, "CREATE temp TABLE temp_tXX_spls (" +
											 "nzp_kvar INTEGER, " +
											 " num_ls INTEGER, " +
											 " num_ls_litera char(250)," +
											 " pkod10 INTEGER," +
											 " pkod NUMERIC(13)," +
											 " nzp_dom INTEGER," +
											 " nzp_area INTEGER," +
											 " nzp_geu INTEGER," +
											 " nzp_ul INTEGER," +
											 " typek INTEGER," +
											 " fio CHAR(60)," +
											 " adr CHAR(160)," +
											 " kod_ls CHAR(12)," +
											 " ulica CHAR(80)," +
											 " ndom CHAR(20)," +
											 " idom INTEGER," +
											 " nkvar CHAR(20)," +
											 " ikvar INTEGER," +
											 " stypek CHAR(20)," +
											 " sostls CHAR(20)," +
											 " pref CHAR(20)," +
											 " mark INTEGER," +
											 " has_pu INTEGER)", true);
#else
			ret = ExecSQL(conn_db, "CREATE temp TABLE temp_tXX_spls (" +
											 "nzp_kvar INTEGER, " +
											 " num_ls INTEGER, " +
											 " num_ls_litera char(250)," +
											 " pkod10 INTEGER," +
											 " pkod DECIMAL(13)," +
											 " nzp_dom INTEGER," +
											 " nzp_area INTEGER," +
											 " nzp_geu INTEGER," +
											 " nzp_ul INTEGER," +
											 " typek INTEGER," +
											 " fio CHAR(60)," +
											 " adr CHAR(160)," +
											 " kod_ls CHAR(12)," +
											 " ulica CHAR(80)," +
											 " ndom CHAR(20)," +
											 " idom INTEGER," +
											 " nkvar CHAR(20)," +
											 " ikvar INTEGER," +
											 " stypek CHAR(20)," +
											 " sostls CHAR(20)," +
											 " pref CHAR(20)," +
											 " mark INTEGER," +
											 " has_pu INTEGER)", true);
#endif
			if (!ret.result)
			{
				conn_db.Close();
				conn_web.Close();
				return;
			}
			#endregion

			#region главный запрос
#if PG
			var insertFormat = new StringBuilder();
			insertFormat.Append(
								"insert into temp_tXX_spls (nzp_kvar,num_ls,pkod10,pkod,typek,pref,nzp_dom,nzp_area,nzp_geu,fio,ikvar,idom,kod_ls,ndom,nkvar,nzp_ul, ulica,adr,sostls,stypek,mark,has_pu)");
			insertFormat.Append(" Select distinct k.nzp_kvar,k.num_ls,k.pkod10,k.pkod,k.typek, k.pref, k.nzp_dom,k.nzp_area,k.nzp_geu,k.fio, ikvar,idom, gil_s, ");
			insertFormat.Append("   trim(coalesce(d.ndom,''))||' '||trim(coalesce(d.nkor,'')) as ndom, ");
			insertFormat.Append("   trim(coalesce(k.nkvar,''))||' '||trim(coalesce(k.nkvar_n,'')) as nkvar, ");
			insertFormat.Append("   d.nzp_ul, trim(u.ulica)||' / '||trim(coalesce(r.rajon,'')) as ulica, ");
			insertFormat.Append(
								"   trim(coalesce(ulicareg,'улица'))||' '||trim(coalesce(u.ulica,''))||' / '||trim(coalesce(r.rajon,''))||'   дом '||");
			insertFormat.Append("   trim(coalesce(ndom,''))||'  корп. '|| trim(coalesce(nkor,''))||'  кв. '||trim(coalesce(nkvar,''))||'  ком. '||trim(coalesce(nkvar_n,'')) as adr, ");
			insertFormat.AppendFormat("   ry.name_y as sostls, {0} stypek, 1 as mark, 0 as has_pu ", "t.name_y".CastTo("CHARACTER", "20"));
			insertFormat.Append(" From t_selected_kvar k");
			insertFormat.AppendFormat(" left outer join {0} d on k.nzp_dom = d.nzp_dom", tables.dom);
			insertFormat.AppendFormat(" left outer join {0} u on d.nzp_ul = u.nzp_ul", tables.ulica);
			insertFormat.AppendFormat(" left outer join {0} r on u.nzp_raj = r.nzp_raj", tables.rajon);
			insertFormat.AppendFormat(" left outer join {0} t on k.typek = t.nzp_y", tables.res_y);
			insertFormat.AppendFormat(" left outer join {0} ry on cast(k.is_open as integer) = ry.nzp_y", tables.res_y);
			insertFormat.Append(" where ry.nzp_res = 18");
			insertFormat.Append(" and t.nzp_res = 9999");

			var sqlInsert = insertFormat.ToString();
#else

			string sqlInsert = "insert into temp_tXX_spls (nzp_kvar,num_ls,pkod10,pkod,typek,pref,nzp_dom,nzp_area,nzp_geu,fio,ikvar,idom,kod_ls,ndom,nkvar,nzp_ul, ulica,adr,sostls,stypek,mark,has_pu)" +
				" Select unique k.nzp_kvar,k.num_ls,k.pkod10,k.pkod,k.typek, k.pref, k.nzp_dom,k.nzp_area,k.nzp_geu,k.fio, ikvar,idom, gil_s, ";
			sqlInsert += "   trim(nvl(d.ndom,''))||' '||trim(nvl(d.nkor,'')) as ndom, " +
						 "   trim(nvl(k.nkvar,''))||' '||trim(nvl(k.nkvar_n,'')) as nkvar, " +
						 "   d.nzp_ul, trim(u.ulica)||' / '||trim(nvl(r.rajon,'')) as ulica, ";
			sqlInsert += "   trim(nvl(ulicareg,'улица'))||' '||trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,''))||'   дом '||" +
						 "   trim(nvl(ndom,''))||'  корп. '|| trim(nvl(nkor,''))||'  кв. '||trim(nvl(nkvar,''))||'  ком. '||trim(nvl(nkvar_n,'')) as adr, ";
			sqlInsert += "   ry.name_y as sostls, t.name_y stypek, 1 as mark, 0 as has_pu ";
			sqlInsert += " From t_selected_kvar k" +
				", " + tables.dom + " d" +
				", " + tables.ulica + " u" +
				", outer " + tables.rajon + " r" +
				", outer " + tables.res_y + " t" +
				", outer " + tables.res_y + " ry";
			sqlInsert += " Where k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul and u.nzp_raj = r.nzp_raj";
			sqlInsert += " and k.is_open = ry.nzp_y and ry.nzp_res = 18";
			sqlInsert += " and k.typek = t.nzp_y and t.nzp_res = 9999 ";
#endif
			#endregion Главный запрос

			if (GlobalSettings.WorkOnlyWithCentralBank)
			{
				#region выполнение главного запроса

				ret = ExecSQL(conn_db, sqlInsert, true);

				key = LogSQL(conn_web, finder.nzp_user, tXX_spls_full + " completed: " + sql);
				if (!ret.result)
				{
					if (key > 0) LogSQL_Error(conn_web, key, ret.text);

					conn_db.Close();
					conn_web.Close();
					return;
				}
				ret = ExecSQL(conn_db, "Create index ix_temp_tXX_spls_1 on temp_tXX_spls (nzp_kvar)", true);
				ret = ExecSQL(conn_db, "Create index ix_temp_tXX_spls_2 on temp_tXX_spls (pref)", true);
				ret = ExecSQL(conn_db, "Create index ix_temp_tXX_spls_3 on temp_tXX_spls (num_ls)", true);
				ret = ExecSQL(conn_db, "Create index ix_temp_tXX_spls_4 on temp_tXX_spls (nzp_dom)", true);
#if PG
				ret = ExecSQL(conn_db, "analyze temp_tXX_spls", true);
#else
				ret = ExecSQL(conn_db, "Update statistics for table temp_tXX_spls", true);
#endif

				#endregion
			}
			else
			{

#if PG
				sql = "select distinct pref from t_selected_kvar";
#else
				sql = "select unique pref from t_selected_kvar";
#endif


				IDataReader reader;
				ret = ExecRead(conn_db, out reader, sql, true);
				if (!ret.result)
				{
					conn_db.Close();
					conn_web.Close();
					return;
				}

				while (reader.Read())
				{
					#region добавление доп условий к главному запросу и его выполнение
					cur_pref = reader["pref"] != DBNull.Value ? ((string)reader["pref"]).Trim() : "";
					if (cur_pref == "") continue;

					whereString = "";

					if (finder.dopFind != null)
						foreach (string s in finder.dopFind)
							if (s.Trim() != "") whereString += " and 0 < (" + s.Replace("PREFX", cur_pref) + ")";

					Ls prmfound = new Ls();
					prmfound = finder;
					prmfound.pref = cur_pref;

					bool fls = false;
					bool fdom = false;

					bool findPrmOverLs = FindPrmOverLs(conn_db, prmfound, out ret, out fls, out fdom);
					if (!ret.result)
					{
						conn_db.Close();
						conn_web.Close();
						return;
					}

					sql = sqlInsert + " and k.pref = " + Utils.EStrNull(cur_pref);
					sql += whereString;

					if (findPrmOverLs)
					{
						if (fls) sql += " and k.nzp_kvar in ( Select nzp From ttt_prmls Where tab in (1,3) ) ";
						if (fdom) sql += " and k.nzp_dom  in ( Select nzp From ttt_prmls Where tab in (2,4) ) ";
					}

					//записать текст sql в лог-журнал
					//int key = LogSQL(conn_web, finder.nzp_user, sql.ToString());
					key = LogSQL(conn_web, finder.nzp_user, tXX_spls_full + ": " + whereString);

					ret = ExecSQL(conn_db, sql, true);
					key = LogSQL(conn_web, finder.nzp_user, tXX_spls_full + " completed: " + sql);
					if (!ret.result)
					{
						if (key > 0) LogSQL_Error(conn_web, key, ret.text);

						conn_db.Close();
						conn_web.Close();
						return;
					}
					#endregion

					#region обновление данных в temp_tXX_spls
#if PG
					if (TempTableInWebCashe(conn_db, null, cur_pref + "_data.counters_spis"))
#else
					if (TempTableInWebCashe(conn_db, null, cur_pref + "_data:counters_spis"))
#endif

					{
#if PG
						sql = " Update temp_tXX_spls " + //tXX_spls_full +
							" Set has_pu = 1 " +
							" Where pref = '" + cur_pref + "'" +
							"   and nzp_kvar in ( Select nzp From " + cur_pref + "_data.counters_spis " +
												" Where nzp_type = " + (int)CounterKinds.Kvar +
												"   and is_actual <> 100 ) ";
#else
						sql = " Update temp_tXX_spls " + //tXX_spls_full +
							" Set has_pu = 1 " +
							" Where pref = '" + cur_pref + "'" +
							"   and nzp_kvar in ( Select nzp From " + cur_pref + "_data:counters_spis " +
												" Where nzp_type = " + (int)CounterKinds.Kvar +
												"   and is_actual <> 100 ) ";
#endif

						ret = ExecSQL(conn_db, sql.ToString(), true);
						if (!ret.result)
						{
							conn_db.Close();
							conn_web.Close();
							return;
						}

#if PG
						sql = " Update temp_tXX_spls " +// tXX_spls_full +
							" Set has_pu = 1 " +
							" Where pref = '" + cur_pref + "'" +
							"   and exists ( Select 1 From " + cur_pref + "_data.counters_spis cs, " + cur_pref + "_data.counters_link cl " +
												" Where cs.nzp_type in (" + (int)CounterKinds.Group + "," + (int)CounterKinds.Communal + ") " +
												"   and cs.nzp_counter = cl.nzp_counter" +
												"   and cs.is_actual <> 100 " +
												"   and temp_tXX_spls.nzp_kvar = cl.nzp_kvar)";
#else
						sql = " Update temp_tXX_spls " +// tXX_spls_full +
							" Set has_pu = 1 " +
							" Where pref = '" + cur_pref + "'" +
							"   and exists ( Select 1 From " + cur_pref + "_data:counters_spis cs, " + cur_pref + "_data:counters_link cl " +
												" Where cs.nzp_type in (" + (int)CounterKinds.Group + "," + (int)CounterKinds.Communal + ") " +
												"   and cs.nzp_counter = cl.nzp_counter" +
												"   and cs.is_actual <> 100 " +
												"   and temp_tXX_spls.nzp_kvar = cl.nzp_kvar)";
#endif

						ret = ExecSQL(conn_db, sql.ToString(), true);
						if (!ret.result)
						{
							conn_db.Close();
							conn_web.Close();
							return;
						}
					}
					#endregion
				}
			}

			if (Points.IsSmr)
			{
				//IDataReader reader;
				//sql = "select pref,pkod10,nzp_kvar from temp_tXX_spls";
				//ret = ExecRead(conn_db, out reader, sql, true);
				//if (!ret.result)
				//{
				//    conn_db.Close();
				//    conn_web.Close();
				//    return;
				//}

				//int pkod10;
				//while (reader.Read())
				//{
				//IDataReader reader;
				//sql = "select pref,pkod10,nzp_kvar,pkod from temp_tXX_spls";
				//ret = ExecRead(conn_db, out reader, sql, true);
				//if (!ret.result)
				//{
				//    conn_db.Close();
				//    conn_web.Close();
				//    return;
				//}

				//int pkod10;
				//while (reader.Read())
				//{
				//    Ls ls = new Ls();
				//    if (reader["pref"] == DBNull.Value) ls.pref = ""; else ls.pref = (string)reader["pref"];                    
				//    if (reader["pkod10"] == DBNull.Value) pkod10 = 0; else pkod10 = (int)reader["pkod10"];
				//    if (reader["nzp_kvar"] == DBNull.Value) ls.nzp_kvar = 0; else ls.nzp_kvar = (int)reader["nzp_kvar"];
				//    string num_ls_litera = pkod10.ToString();
				//    int value_num_ls_litera = GetLitera(ls, out ret, conn_db, null);
				//    if (value_num_ls_litera != 0) num_ls_litera += " " + value_num_ls_litera.ToString();
				//    sql = "update temp_tXX_spls set num_ls_litera = '" + num_ls_litera+"' where nzp_kvar = "+ls.nzp_kvar+
				//        " and pref = '"+ls.pref+"'";
				//    ret = ExecSQL(conn_db, sql, true);
				//}
				//}

				sql = "update temp_tXX_spls set num_ls_litera = case when substr(pkod||'',11,1) = '0' then pkod10||'' else pkod10||' '||substr(pkod||'',11,1) end";
				ret = ExecSQL(conn_db, sql, true);
			}

			#region проверка равенства procId и procid из таблицы user_processes
			string actualProcId = GetUserProcId(tXX_spls, conn_web, out ret);
			if (!ret.result)
			{
				conn_db.Close();
				conn_web.Close();
				return;
			}

			if (actualProcId != "" && actualProcId != procId)
			{
				ret.result = false;
				ret.text = "Обнаружен новый запрос поиска";
				ret.tag = -1;
				conn_db.Close();
				conn_web.Close();
				return;
			}
			#endregion

			#region создать кэш-таблицу
			ret = CreateTableWebLs(conn_web, tXX_spls, true);
			if (!ret.result)
			{
				conn_db.Close();
				conn_web.Close();
				return;
			}
			#endregion

			#region запись данных из temp_tXX_spls в кэш таблицу spls
			sql = " Insert into " + tXX_spls_full +
					" (nzp_kvar,num_ls,num_ls_litera,pkod10,pkod,typek,pref,nzp_dom,nzp_area,nzp_geu,fio, ikvar,idom, kod_ls, " +
					" ndom,nkvar,nzp_ul,ulica, adr,sostls,stypek, mark, has_pu) " +
				" Select nzp_kvar,num_ls,num_ls_litera,pkod10,pkod,typek,pref,nzp_dom,nzp_area,nzp_geu,fio, ikvar,idom, kod_ls, " +
					" ndom,nkvar,nzp_ul,ulica, adr,sostls,stypek, mark, has_pu" +
				" From temp_tXX_spls";
			ret = ExecSQL(conn_db, sql.ToString(), true);
			if (!ret.result)
			{
				conn_db.Close();
				conn_web.Close();
				return;
			}
			#endregion

			ExecSQL(conn_db, "drop table temp_tXX_spls", false);

			#region создаем индексы на tXX_spls
			ret = CreateTableWebLs(conn_web, tXX_spls, false);
			if (!ret.result)
			{
				conn_db.Close();
				conn_web.Close();
				return;
			}
			#endregion

			#region заполнение списка домов
			if (ret.result)
			{
				if (!Utils.GetParams(finder.prms, Constants.page_perechen_lsdom))
				{
					//проверить наличие spdom, если нет, то создать 
					Dom Dom = new Dom();
					Dom.nzp_user = finder.nzp_user;
					Dom.spls = tXX_spls_full;
					Dom.nzp_wp = finder.nzp_wp;
					FindDom00(conn_db, null, conn_web, null, Dom, out ret, nzp_server); //найти и заполнить список домов
				}
			}
			#endregion

			#region сообщить контролю, что все успешо выполнено
			if (nzp_server > 0)
				ExecSQL(conn_web,
					" Update " + tXX_meta +
					" Set kod = 1, dat_work = current " +
					" Where nzp_server = " + nzp_server
					, true);
			#endregion

			conn_db.Close();
			conn_web.Close();

			return;
		}//FindSpisLs

		private Returns SaveFinder(Ls finder, int nzp_page)
		{
			Returns ret = SaveFinder((Dom)finder, nzp_page);
			if (!ret.result) return ret;
			//соединение с БД
			IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
			ret = OpenDb(conn_web, true);
			if (!ret.result) return ret;

			string tXX_spfinder = "t" + Convert.ToString(finder.nzp_user) + "_spfinder";

			if (ret.result)
			{
				string sql = "";
				if (Points.IsSmr)
				{
					if (finder.pkod10 > 0)
					{
						sql = "insert into " + tXX_spfinder + " values (0,\'Лицевой счет\',\'" + finder.pkod10.ToString() + "\'," + nzp_page.ToString() + ")";
						ret = ExecSQL(conn_web, sql, true);
						if (!ret.result)
						{
							conn_web.Close();
							return ret;
						}
					}
				}
				else if (finder.num_ls > 0)
				{
					sql = "insert into " + tXX_spfinder + " values (0,\'Лицевой счет\',\'" + finder.num_ls.ToString() + "\'," + nzp_page.ToString() + ")";
					ret = ExecSQL(conn_web, sql, true);
					if (!ret.result)
					{
						conn_web.Close();
						return ret;
					}
				}

				if (finder.pkod.Trim() != "")
				{
					sql = "insert into " + tXX_spfinder + " values (0,\'Платежный код\',\'" + finder.pkod.ToString() + "\'," + nzp_page.ToString() + ")";
					ret = ExecSQL(conn_web, sql, true);
					if (!ret.result)
					{
						conn_web.Close();
						return ret;
					}
				}

				if (finder.town.Trim() != "")
				{
					sql = "insert into " + tXX_spfinder + " values (0,\'Город/район\',\'" + finder.town.ToString() + "\'," + nzp_page.ToString() + ")";
					ret = ExecSQL(conn_web, sql, true);
					if (!ret.result)
					{
						conn_web.Close();
						return ret;
					}
				}

				if (finder.rajon.Trim() != "")
				{
					sql = "insert into " + tXX_spfinder + " values (0,\'Населенный пункт\',\'" + finder.rajon.ToString() + "\'," + nzp_page.ToString() + ")";
					ret = ExecSQL(conn_web, sql, true);
					if (!ret.result)
					{
						conn_web.Close();
						return ret;
					}
				}

				if (finder.stateID > 0)
				{
					sql = "insert into " + tXX_spfinder + " values (0,\'Состояние\',\'" + finder.state + "\'," + nzp_page + ")";
					ret = ExecSQL(conn_web, sql, true);
					if (!ret.result)
					{
						conn_web.Close();
						return ret;
					}
				}

				if (finder.typek > 0)
				{
					sql = "insert into " + tXX_spfinder + " values (0,\'Тип счета\',\'" + finder.stypek.ToString() + "\'," + nzp_page.ToString() + ")";
					ret = ExecSQL(conn_web, sql, true);
					if (!ret.result)
					{
						conn_web.Close();
						return ret;
					}
				}

				if (finder.porch.Trim() != "")
				{
					sql = "insert into " + tXX_spfinder + " values (0,\'Подъезд\',\'" + finder.porch.ToString() + "\'," + nzp_page.ToString() + ")";
					ret = ExecSQL(conn_web, sql, true);
					if (!ret.result)
					{
						conn_web.Close();
						return ret;
					}
				}

				if (finder.nkvar.Trim() != "")
				{
					sql = "insert into " + tXX_spfinder + " values (0,\'Квартира с\',\'" + finder.nkvar.ToString() + "\'," + nzp_page.ToString() + ")";
					ret = ExecSQL(conn_web, sql, true);
					if (!ret.result)
					{
						conn_web.Close();
						return ret;
					}
				}

				if (finder.nkvar_po.Trim() != "")
				{
					sql = "insert into " + tXX_spfinder + " values (0,\'Квартира по\',\'" + finder.nkvar_po.ToString() + "\'," + nzp_page.ToString() + ")";
					ret = ExecSQL(conn_web, sql, true);
					if (!ret.result)
					{
						conn_web.Close();
						return ret;
					}
				}

				if (finder.nkvar_n.Trim() != "")
				{
					sql = "insert into " + tXX_spfinder + " values (0,\'Комната\',\'" + finder.nkvar_n.ToString() + "\'," + nzp_page.ToString() + ")";
					ret = ExecSQL(conn_web, sql, true);
					if (!ret.result)
					{
						conn_web.Close();
						return ret;
					}
				}

				if (finder.phone.Trim() != "")
				{
					sql = "insert into " + tXX_spfinder + " values (0,\'Телефон квартиры\',\'" + finder.phone.ToString() + "\'," + nzp_page.ToString() + ")";
					ret = ExecSQL(conn_web, sql, true);
					if (!ret.result)
					{
						conn_web.Close();
						return ret;
					}
				}

				if (finder.fio.Trim() != "")
				{
					sql = "insert into " + tXX_spfinder + " values (0,\'Квартиросъемщик\',\'" + finder.fio.ToString() + "\'," + nzp_page.ToString() + ")";
					ret = ExecSQL(conn_web, sql, true);
					if (!ret.result)
					{
						conn_web.Close();
						return ret;
					}
				}

				if (finder.uch.Trim() != "")
				{
					sql = "insert into " + tXX_spfinder + " values (0,\'Участок\',\'" + finder.uch.ToString() + "\'," + nzp_page.ToString() + ")";
					ret = ExecSQL(conn_web, sql, true);
					if (!ret.result)
					{
						conn_web.Close();
						return ret;
					}
				}
			}
			conn_web.Close();
			return ret;
		}

		private Returns SaveFinder(IDbConnection connection, IDbTransaction transaction, Dom finder, int nzp_page)
		{
			//todo pg
			string tXX_spfinder = "t" + Convert.ToString(finder.nzp_user) + "_spfinder";

			Returns ret = new Returns(true);

			//проверка наличия таблицы в БД
			if (!TableInWebCashe(connection, tXX_spfinder))
			{
				//создать таблицу webdata
				ret = ExecSQL(connection,
						  " Create table " + tXX_spfinder +
						  " (nzp_finder serial, " +
						  "  name char(100), " +
						  "  value char(255), " +
						  "  nzp_page integer " +
						  " ) ", true);
			}
			if (!ret.result) return ret;

			string sql = "delete from " + tXX_spfinder + " where nzp_page = " + nzp_page.ToString();
			ret = ExecSQL(connection, sql, true);
			if (!ret.result)
			{
				return ret;
			}

			sql = "";
			if (finder.nzp_wp > 0)
			{
				sql = "insert into " + tXX_spfinder + " values (0,\'Банк данных\',\'" + finder.point.ToString() + "\'," + nzp_page.ToString() + ")";
			}
			else if (finder.dopPointList != null && finder.dopPointList.Count > 0)
			{
				sql = "insert into " + tXX_spfinder + " values (0,\'Банк данных\',\'" + finder.point.ToString() + "\'," + nzp_page.ToString() + ")";
			}
			if (sql != "")
			{
				ret = ExecSQL(connection, sql, true);
				if (!ret.result)
				{
					return ret;
				}
			}

			sql = "";
			if (finder.nzp_area > 0)
				sql = "insert into " + tXX_spfinder + " values (0,\'Управляющая организация\',\'" + finder.area.ToString() + "\'," + nzp_page.ToString() + ")";

			else if (finder.list_nzp_area != null && finder.list_nzp_area.Count > 0)
				sql = "insert into " + tXX_spfinder + " values (0,\'Управляющая организация\',\'" + finder.area.ToString() + "\'," + nzp_page.ToString() + ")";

			if (sql != "")
			{
				ret = ExecSQL(connection, sql, true);
				if (!ret.result)
				{
					return ret;
				}
			}

			if (finder.nzp_geu > 0)
			{
				sql = "insert into " + tXX_spfinder + " values (0,\'Отделение\',\'" + finder.geu.ToString() + "\'," + nzp_page.ToString() + ")";
				ret = ExecSQL(connection, sql, true);
				if (!ret.result)
				{
					return ret;
				}
			}

			if (finder.nzp_ul > 0)
			{
				sql = "insert into " + tXX_spfinder + " values (0,\'Улица\',\'" + finder.ulica.ToString() + "\'," + nzp_page.ToString() + ")";
				ret = ExecSQL(connection, sql, true);
				if (!ret.result)
				{
					return ret;
				}
			}

			if (finder.ndom.Trim() != "")
			{
				sql = "insert into " + tXX_spfinder + " values (0,\'Номер дома с\',\'" + finder.ndom.ToString() + "\'," + nzp_page.ToString() + ")";
				ret = ExecSQL(connection, sql, true);
				if (!ret.result)
				{
					return ret;
				}
			}

			if (finder.ndom_po.Trim() != "")
			{
				sql = "insert into " + tXX_spfinder + " values (0,\'Номер дома по\',\'" + finder.ndom_po.ToString() + "\'," + nzp_page.ToString() + ")";
				ret = ExecSQL(connection, sql, true);
				if (!ret.result)
				{
					return ret;
				}
			}

			if (finder.nkor.Trim() != "")
			{
				sql = "insert into " + tXX_spfinder + " values (0,\'Корпус\',\'" + finder.nkor.ToString() + "\'," + nzp_page.ToString() + ")";
				ret = ExecSQL(connection, sql, true);
				if (!ret.result)
				{
					return ret;
				}
			}

			return ret;
		}

		private Returns SaveFinder(Dom finder, int nzp_page)
		{
			if (finder.nzp_user <= 0)
			{
				return new Returns(false, "Пользователь не определен");
			}

			Returns ret = Utils.InitReturns();

			//соединение с БД
			IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
			ret = OpenDb(conn_web, true);
			if (!ret.result) return ret;

			ret = SaveFinder(conn_web, null, finder, nzp_page);

			conn_web.Close();
			return ret;
		}

		//----------------------------------------------------------------------
		private void CreateTableWebDom(IDbConnection conn_web, string tXX_spdom, bool onCreate, out Returns ret) //
		//----------------------------------------------------------------------
		{
			if (onCreate)
			{
				if (TableInWebCashe(conn_web, tXX_spdom))
				{
					ExecSQL(conn_web, " Drop table " + tXX_spdom, false);
				}

				//создать таблицу webdata:tXX_spDom
				ret = ExecSQL(conn_web,
						  " Create table " + tXX_spdom +
						  " ( nzp_dom    integer, " +
						  "   nzp_ul     integer, " +
						  "   nzp_area   integer, " +
						  "   nzp_geu    integer, " +
						  "   nzp_wp     integer, " +

						  "   area     char(60)," +
						  "   geu      char(60)," +

						  "   ulica    char(40)," +
						  "   ulicareg    char(40)," +
						  "   rajon    char(40)," +
						  "   town    char(40)," +
						  "   ndom     char(20)," +
						  "   idom     integer, " +
						  "   pref     char(10)," +
						  "   point    char(60)," +
						  "   mark     integer," +
						  "   has_pu   integer" +
						  " ) ", true);
				if (!ret.result)
				{
					return;
				}

			}
			else
			{
				ret = ExecSQL(conn_web, " Create index ix1_" + tXX_spdom + " on " + tXX_spdom + " (nzp_dom) ", true);
				ret = ExecSQL(conn_web, " Create index ix2_" + tXX_spdom + " on " + tXX_spdom + " (ulica,idom) ", true);
				ret = ExecSQL(conn_web, " Create index ix3_" + tXX_spdom + " on " + tXX_spdom + " (area,ulica,idom) ", true);

				if (!ret.result)
				{
#if PG
					ret = ExecSQL(conn_web, " analyze  " + tXX_spdom, true);
#else
					ret = ExecSQL(conn_web, " Update statistics for table  " + tXX_spdom, true);
#endif

				}
			}
		}
		//----------------------------------------------------------------------
		public void FindDom(Dom finder, out Returns ret) //
		//----------------------------------------------------------------------
		{
			ret = Utils.InitReturns();
			FindDom00(finder, out ret, 0);
		}

		/// <summary>
		/// Найти и заполнить список домов
		/// </summary>
		/// <param name="finder"></param>
		/// <param name="ret"></param>
		/// <param name="nzp_server"></param>
		private void FindDom00(Dom finder, out Returns ret, int nzp_server)
		{
			if (finder.nzp_user < 1)
			{
				ret = new Returns(false, "Не определен пользователь");
				return;
			}

			string conn_kernel = Points.GetConnKernel(finder.nzp_wp, nzp_server);
			if (conn_kernel == "")
			{
				ret = new Returns(false, "Не определен connect к БД");
				return;
			}

			IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
			ret = OpenDb(conn_web, true);
			if (!ret.result) return;

			IDbConnection conn_db = GetConnection(conn_kernel);
			ret = OpenDb(conn_db, true);
			if (!ret.result)
			{
				conn_web.Close();
				return;
			}

			FindDom00(conn_db, null, conn_web, null, finder, out ret, nzp_server);

			conn_db.Close(); //закрыть соединение с основной базой
			conn_web.Close();
			return;
		}

		private void FindDom00(IDbConnection conn_db, IDbTransaction trans_db, IDbConnection conn_web, IDbTransaction trans_web, Dom finder, out Returns ret, int nzp_server) //найти и заполнить список домов
		{
			string tXX_spdom = "t" + finder.nzp_user + "_spdom" + (nzp_server > 0 ? nzp_server.ToString() : "");
#if PG
			string tXX_spdom_full = pgDefaultDb + "." + tXX_spdom;
#else
			string tXX_spdom_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spdom;
#endif


			string procId = Guid.NewGuid().ToString();

			#region обновить данные в таблице user_processes
			AddToUserProc((Finder)finder, tXX_spdom, procId, conn_web);
			#endregion

			#region сохранение finder
			ret = SaveFinder(conn_web, trans_web, finder, Constants.page_spisdom);
			if (!ret.result) return;
			#endregion

			#region условия whereString
			string whereString;
			if (finder.spls != "") whereString = " and d.nzp_dom in ( Select nzp_dom From " + finder.spls + " ) ";
			else
			{
				StringBuilder swhere = new StringBuilder();
				int i;

				if (finder.nzp_dom > 0) swhere.Append(" and d.nzp_dom = " + finder.nzp_dom.ToString());

				if (finder.nzp_area > 0) swhere.Append(" and d.nzp_area = " + finder.nzp_area.ToString());

				if (finder.nzp_geu > 0) swhere.Append(" and d.nzp_geu = " + finder.nzp_geu.ToString());

				if (finder.nzp_ul > 0) swhere.Append(" and d.nzp_ul = " + finder.nzp_ul.ToString());

				if (finder.ndom_po != "")
				{
					i = Utils.GetInt(finder.ndom_po);
					if (i > 0) swhere.Append(" and d.idom <= " + i.ToString());

					i = Utils.GetInt(finder.ndom);
					if (i > 0) swhere.Append(" and d.idom >= " + i.ToString());
				}
				else if (finder.ndom != "") swhere.Append(" and upper(d.ndom) = " + Utils.EStrNull(finder.ndom.ToUpper()));

				if (finder.nzp_wp > 0) swhere.Append(" and d.nzp_wp = " + finder.nzp_wp);
				else if (finder.dopPointList != null && finder.dopPointList.Count > 0)
				{
					if (finder.dopPointList.Count == 1) swhere.Append(" and d.nzp_wp = " + finder.dopPointList[0]);
					else
					{
						string s = " and d.nzp_wp in (" + finder.dopPointList[0];
						for (int k = 1; k < finder.dopPointList.Count; k++) s += "," + finder.dopPointList[k];
						s += ")";
						swhere.Append(s);
					}
				}

				whereString = swhere.ToString();
			}

			if (finder.RolesVal != null)
			{
				foreach (_RolesVal role in finder.RolesVal)
				{
					if (role.tip == Constants.role_sql)
					{
						if (role.kod == Constants.role_sql_area) whereString += " and d.nzp_area in (" + role.val + ")";
						else if (role.kod == Constants.role_sql_wp) whereString += " and d.nzp_wp in (" + role.val + ")";
						else if (role.kod == Constants.role_sql_geu) whereString += " and d.nzp_geu in (" + role.val + ")";
					}
				}
			}
			#endregion

			#region выборка из таблицы dom при условии whereString
			DbTables tables = new DbTables(conn_db);
			ExecSQL(conn_db, "drop table t_selected_dom", false);

#if PG
			string sql = "select d.* into temp t_selected_dom from " + tables.dom + " d Where d.nzp_wp is not null " + whereString;
#else
			string sql = "Select d.* from " + tables.dom + " d Where d.nzp_wp is not null " + whereString +
							" into temp t_selected_dom with no log";
#endif



			int key = LogSQL(conn_web, finder.nzp_user, tXX_spdom_full + ": " + sql);

			ret = ExecSQL(conn_db, sql, true);
			if (!ret.result)
			{
				if (key > 0) LogSQL_Error(conn_web, key, ret.text);
				return;
			}

			ret = ExecSQL(conn_db, "Create index ix_selected_dom_1 on t_selected_dom (nzp_wp)", true);
			ret = ExecSQL(conn_db, "Create index ix_selected_dom_2 on t_selected_dom (pref)", true);
			ret = ExecSQL(conn_db, "Create index ix_selected_dom_3 on t_selected_dom (nzp_dom)", true);
			ret = ExecSQL(conn_db, "Create index ix_selected_dom_4 on t_selected_dom (nzp_ul)", true);
#if PG
			ret = ExecSQL(conn_db, "analyze t_selected_dom", true);
#else
			ret = ExecSQL(conn_db, "Update statistics for table t_selected_dom", true);
#endif

			#endregion

			#region удаление временной таблицы temp_tXX_spls(содержит результат поиска)
			ExecSQL(conn_db, "drop table temp_tXX_spdom", false);
			#endregion

			#region создание таблицы temp_tXX_spdom
			ret = ExecSQL(conn_db, "CREATE temp TABLE temp_tXX_spdom( " +
							"    nzp_dom INTEGER, " +
							"    nzp_ul INTEGER, " +
							"    nzp_area INTEGER, " +
							"    nzp_geu INTEGER, " +
							"    nzp_wp INTEGER, " +
							"    area CHAR(60), " +
							"    geu CHAR(60), " +
							"    ulica CHAR(40), " +
							"    ulicareg CHAR(40), " +
							"    rajon CHAR(40), " +
							"    town CHAR(40), " +
							"    ndom CHAR(20), " +
							"    idom INTEGER, " +
							"    pref CHAR(10), " +
							"    point CHAR(30), " +
							"    mark INTEGER, " +
							"    has_pu INTEGER)", true);
			if (!ret.result)
			{
				return;
			}
			#endregion

			#region запрос
#if PG
			var sqlBuilder = new StringBuilder();

			sqlBuilder.Append(
							  "insert into temp_tXX_spdom (pref, nzp_wp, point, nzp_dom, nzp_ul, nzp_area, nzp_geu, idom, ndom, ulica, ulicareg, rajon, town, area, geu, mark, has_pu)");
			sqlBuilder.AppendFormat(" Select d.pref, d.nzp_wp, {0}, d.nzp_dom, d.nzp_ul, d.nzp_area, d.nzp_geu, d.idom,  ", "p.point".CastTo("CHARACTER", "30"));
			sqlBuilder.Append(" 'дом ' || trim(coalesce(d.ndom,''))||' корп. '||trim(coalesce(d.nkor,'')) as ndom, ");
			sqlBuilder.Append(" u.ulica, u.ulicareg, r.rajon, t.town, area, geu, 1 as mark, 0 as has_pu ");
			sqlBuilder.Append(" From t_selected_dom d");
			sqlBuilder.AppendFormat(" left outer join {0} p on p.nzp_wp = d.nzp_wp", tables.point);
			sqlBuilder.AppendFormat(" left outer join {0} u on d.nzp_ul = u.nzp_ul", tables.ulica);
			sqlBuilder.AppendFormat(" left outer join {0} r on u.nzp_raj = r.nzp_raj", tables.rajon);
			sqlBuilder.AppendFormat(" left outer join {0} t on r.nzp_town = t.nzp_town", tables.town);
			sqlBuilder.AppendFormat(" left outer join {0} a on d.nzp_area = a.nzp_area", tables.area);
			sqlBuilder.AppendFormat(" left outer join {0} g on d.nzp_geu = g.nzp_geu", tables.geu);

			var sqlInsert = sqlBuilder.ToString();
			
#else
			string sqlInsert = //" Insert into " + tXX_spdom_full + " (pref,nzp_wp,point, nzp_dom,nzp_ul,nzp_area,nzp_geu,idom, ndom,ulica, area,geu,mark) " +
				"insert into temp_tXX_spdom (pref, nzp_wp, point, nzp_dom, nzp_ul, nzp_area, nzp_geu, idom, ndom, ulica, ulicareg, rajon, town, area, geu, mark, has_pu)" +
				" Select d.pref, d.nzp_wp, p.point, d.nzp_dom, d.nzp_ul, d.nzp_area, d.nzp_geu, d.idom,  " +
				" 'дом ' || trim(nvl(d.ndom,''))||' корп. '||trim(nvl(d.nkor,'')) as ndom, " +
				" u.ulica, u.ulicareg, r.rajon, t.town, area, geu, 1 as mark, 0 as has_pu " +
				" From t_selected_dom d" +
					", " + tables.ulica + " u" +
					", " + tables.point + " p" +
					", outer (" + tables.rajon + " r, outer " + tables.town + " t)" +
					", outer " + tables.area + " a" +
					", outer " + tables.geu + " g" +
					" Where p.nzp_wp = d.nzp_wp and d.nzp_ul = u.nzp_ul and u.nzp_raj = r.nzp_raj and r.nzp_town = t.nzp_town and d.nzp_area = a.nzp_area and d.nzp_geu = g.nzp_geu";
#endif

			#endregion

			if (GlobalSettings.WorkOnlyWithCentralBank)
			{
				#region выполнение запроса

				//записать текст sql в лог-журнал
				key = LogSQL(conn_web, finder.nzp_user, tXX_spdom_full + ":" + sql);

				ret = ExecSQL(conn_db, sqlInsert, true);
				if (!ret.result)
				{
					if (key > 0) LogSQL_Error(conn_web, key, ret.text);
					return;
				}
				#endregion
			}
			else
			{
#if PG
				sql = "select distinct pref from t_selected_dom";
#else
				sql = "select unique pref from t_selected_dom";

#endif
				IDataReader reader;
				ret = ExecRead(conn_db, out reader, sql, true);
				if (!ret.result)
				{
					return;
				}

				string cur_pref;

				while (reader.Read())
				{
					cur_pref = reader["pref"] != DBNull.Value ? ((string)reader["pref"]).Trim() : "";

					if (cur_pref == "") continue;

					#region доп условия  к запросу
					whereString = "";

					if (finder.dopFind != null)
						if (finder.dopFind.Count > 0) //учесть дополнительные шаблоны
						{
							foreach (string s in finder.dopFind)
							{
								if (s.Trim() != "") whereString += " and 0 < (" + s.Replace("PREFX", cur_pref) + ")";
							}
						}

					sql = string.Format(
										"{0} {1} d.pref = {2}",
						sqlInsert,
						sqlInsert.IndexOf("where", StringComparison.OrdinalIgnoreCase) == -1 ? "where" : "and",
						Utils.EStrNull(cur_pref));
					sql += whereString;
					#endregion

					#region выполнение запроса
					//записать текст sql в лог-журнал
					key = LogSQL(conn_web, finder.nzp_user, tXX_spdom_full + ":" + sql);

					ret = ExecSQL(conn_db, sql, true);
					if (!ret.result)
					{
						if (key > 0) LogSQL_Error(conn_web, key, ret.text);
						return;
					}
					#endregion

					#region обновление таблицы temp_tXX_spdom
#if PG
					if (TempTableInWebCashe(conn_db, null, cur_pref + "_data.counters_spis"))
#else
					if (TempTableInWebCashe(conn_db, null, cur_pref + "_data:counters_spis"))
#endif
					{
#if PG
						sql = "Update temp_tXX_spdom " +// tXX_spdom_full +
							" Set has_pu = 1" +
							" Where pref = '" + cur_pref + "'" +
								" and nzp_dom in (select nzp from " + cur_pref + "_data.counters_spis" +
												" where nzp_type = " + (int)CounterKinds.Dom +
													" and is_actual <> 100)";
#else
						sql = "Update temp_tXX_spdom " +// tXX_spdom_full +
							" Set has_pu = 1" +
							" Where pref = '" + cur_pref + "'" +
								" and nzp_dom in (select nzp from " + cur_pref + "_data:counters_spis" +
												" where nzp_type = " + (int)CounterKinds.Dom +
													" and is_actual <> 100)";
#endif

						ret = ExecSQL(conn_db, sql.ToString(), true);
						if (!ret.result)
						{
							return;
						}

#if PG
						sql = " Update temp_tXX_spdom " + //tXX_spdom_full +
							" Set has_pu = 1 " +
							" Where pref = '" + cur_pref + "'" +
							"   and exists ( Select 1 From " + cur_pref + "_data.counters_spis cs, " + cur_pref + "_data.counters_link cl, " + tables.kvar + " k " +
												" Where cs.nzp_type in (" + (int)CounterKinds.Group + "," + (int)CounterKinds.Communal + ") " +
												"   and cs.nzp_counter = cl.nzp_counter" +
												"   and cs.is_actual <> 100 " +
												" and cl.nzp_kvar = k.nzp_kvar" +
												" and k.nzp_dom = temp_tXX_spdom" /*+ tXX_spdom_full */+ ".nzp_dom)";
#else
						sql = " Update temp_tXX_spdom " + //tXX_spdom_full +
							" Set has_pu = 1 " +
							" Where pref = '" + cur_pref + "'" +
							"   and exists ( Select 1 From " + cur_pref + "_data:counters_spis cs, " + cur_pref + "_data:counters_link cl, " + tables.kvar + " k " +
												" Where cs.nzp_type in (" + (int)CounterKinds.Group + "," + (int)CounterKinds.Communal + ") " +
												"   and cs.nzp_counter = cl.nzp_counter" +
												"   and cs.is_actual <> 100 " +
												" and cl.nzp_kvar = k.nzp_kvar" +
												" and k.nzp_dom = temp_tXX_spdom" /*+ tXX_spdom_full */+ ".nzp_dom)";
#endif

						ret = ExecSQL(conn_db, sql, true);
						if (!ret.result)
						{
							return;
						}
					}
					#endregion
				}
			}

			#region проверка равенства procId и procid из таблицы user_processes
			string actualProcId = GetUserProcId(tXX_spdom, conn_web, out ret);
			if (!ret.result)
			{
				conn_db.Close();
				conn_web.Close();
				return;
			}

			if (actualProcId != "" && actualProcId != procId)
			{
				ret.result = false;
				ret.text = "Обнаружен новый запрос поиска";
				ret.tag = -1;
				conn_db.Close();
				conn_web.Close();
				return;
			}
			#endregion

			#region создать кэш-таблицу
			CreateTableWebDom(conn_web, tXX_spdom, true, out ret);
			if (!ret.result) return;
			#endregion

			#region запись данных из temp_tXX_spdom в кэш таблицу spdom
			sql = " Insert into " + tXX_spdom_full +
				" (pref,nzp_wp,point, nzp_dom,nzp_ul,nzp_area,nzp_geu,idom, ndom,ulica,ulicareg,rajon,town, area,geu,mark,has_pu) " +
				" select pref,nzp_wp,point, nzp_dom,nzp_ul,nzp_area,nzp_geu,idom, ndom,ulica,ulicareg,rajon,town, area,geu,mark,has_pu from temp_tXX_spdom";
			ret = ExecSQL(conn_db, sql.ToString(), true);
			if (!ret.result) return;
			#endregion

			ExecSQL(conn_db, "drop table temp_tXX_spdom", false);

			#region создаем индексы на tXX_spDom
			CreateTableWebDom(conn_web, tXX_spdom, false, out ret);
			#endregion
		}

		//----------------------------------------------------------------------
		public void FindUlica(Ulica finder, out Returns ret) //найти и заполнить список улиц
		//----------------------------------------------------------------------
		{
			ret = Utils.InitReturns();
			if (finder.nzp_user < 1)
			{
				ret.result = false;
				ret.text = "Не определен пользователь";
				return;
			}

			IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
			ret = OpenDb(conn_web, true);
			if (!ret.result) return;

			string tXX_spul = "t" + Convert.ToString(finder.nzp_user) + "_spul";

			if (finder.spls.Trim() != "")
			{
				//вызов из создания spls
				IDataReader reader;
				if (ExecRead(conn_web, out reader, " Select * From " + tXX_spul, false).result)
				{
					//таблицы была создана, ничего не дедаем, выходим
					conn_web.Close();
					return;
				}
				reader.Close();
			}
			if (TableInWebCashe(conn_web, tXX_spul))
			{
				ExecSQL(conn_web, " Drop table " + tXX_spul, false);
			}

			//создать таблицу webdata:tXX_spul
			ret = ExecSQL(conn_web,
					  " Create table " + tXX_spul +
					  " ( nzp_ul   integer, " +
					  "   ulica    char(80) " +
					  " ) ", true);
			if (!ret.result)
			{
				conn_web.Close();
				return;
			}

			//заполнить webdata:tXX_spDom
			IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
			ret = OpenDb(conn_db, true);
			if (!ret.result)
			{
				conn_web.Close();
				return;
			}

#if PG
			string webdata = conn_web.Database
				;
#else
			string webdata = conn_web.Database + "@" + DBManager.getServer(conn_web);
#endif

			string tXX_spul_full = webdata + ":" + tXX_spul;

			string whereString;
			if (finder.spls != "")
				whereString = " and u.nzp_ul in ( Select nzp_ul From " + finder.spls + " ) ";
			else
			{
				StringBuilder swhere = new StringBuilder();

				if (finder.nzp_ul > 0)
				{
					swhere.Append(" and d.nzp_ul = " + finder.nzp_ul.ToString());
				}
				if (finder.ulica != "")
				{
#if PG
					swhere.Append(" and upper(ulica) SIMILAR TO upper( '" + finder.ulica + "*')");
#else
					swhere.Append(" and upper(ulica) matches upper( '" + finder.ulica + "*')");
#endif

				}

				whereString = swhere.ToString();
			}

			StringBuilder sql = new StringBuilder();

			sql.Append(" Insert into " + tXX_spul_full + " (nzp_ul,ulica) ");
#if PG
			sql.Append(" Select nzp_ul, trim(coalesce(u.ulica,''))||' / '||trim(coalesce(r.rajon,'')) ");
			sql.Append(" From " + Points.Pref + "_data.s_ulica u left outer join " + Points.Pref + "_data.s_rajon r ");
#else
			sql.Append(" Select nzp_ul, trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,'')) ");
			sql.Append(" From " + Points.Pref + "_data:s_ulica u, outer " + Points.Pref + "_data:s_rajon r ");
#endif

#if PG
			sql.Append(" on u.nzp_raj=r.nzp_raj ");
#else
			sql.Append(" Where u.nzp_raj=r.nzp_raj ");

#endif
			sql.Append(whereString);

			ret = ExecSQL(conn_db, sql.ToString(), true);
			if (!ret.result)
			{
				conn_db.Close();
				conn_web.Close();
				return;
			}

			conn_db.Close(); //закрыть соединение с основной базой

			//далее работаем с кешем
			//создаем индексы на tXX_spDom
			string ix = "ix" + Convert.ToString(finder.nzp_user) + "_spul";

			ret = ExecSQL(conn_web, " Create index " + ix + "_1 on " + tXX_spul + " (nzp_ul) ", true);
			if (!ret.result)
			{
#if PG
				ret = ExecSQL(conn_web, " analyze  " + tXX_spul, true);
#else
				ret = ExecSQL(conn_web, " Update statistics for table  " + tXX_spul, true);
#endif

			}

			conn_web.Close();
			return;
		}//FindUlica

		//----------------------------------------------------------------------
		public ReturnsObjectType<List<Ulica>> LoadUlica(Ulica finder, IDbConnection connectionID)
		//----------------------------------------------------------------------
		{
			string whereString = "";
			string whereStringt1 = "";
			string whereStringt2 = "";
			string whereStringt3 = "";
			string fromString = "";
			if (finder.ulica.Trim() != "")
			{
				whereStringt1 = whereString += " and  (upper(u.ulica) LIKE  upper( '" + finder.ulica + "%') "
				   + " or upper(u.ulica) LIKE  upper( '%" + finder.ulica + "') "
				   + " or upper(u.ulica) LIKE  upper( '%" + finder.ulica + "%')  )";
#if PG
			  //  whereString += " and upper(u.ulica) SIMILAR TO upper( '" + finder.ulica + "%')";
			
				whereString += " and  (upper(u.ulica) LIKE  upper( '" + finder.ulica + "%') "
					+" or upper(u.ulica) LIKE  upper( '%" + finder.ulica + "') "
					+ " or upper(u.ulica) LIKE  upper( '%" + finder.ulica + "%')  )";
#else
			   // whereString += " and upper(u.ulica) matches upper( '" + finder.ulica + "*')";
			 whereString += " and  (upper(u.ulica) LIKE  upper( '" + finder.ulica + "%') "
					+" or upper(u.ulica) LIKE  upper( '%" + finder.ulica + "') "
					+ " or upper(u.ulica) LIKE  upper( '%" + finder.ulica + "%')  )";
#endif

			}
			if ((finder.nzp_area > 0 || (finder.list_nzp_area.Count > 0)) || ((finder.list_nzp_wp.Count > 0)))
			{
			   // whereStringt1 = " and d.nzp_ul = u.nzp_ul ";
#if PG
				fromString = ", " + Points.Pref + "_data.dom d";
#else
				fromString = ", " + Points.Pref + "_data:dom d";
#endif

				whereString += " and d.nzp_ul = u.nzp_ul ";
			  

				if (finder.list_nzp_area.Count > 0)
				{
					string str = "";
					for (int i = 0; i < finder.list_nzp_area.Count; i++)
					{
						if (i == 0) str += finder.list_nzp_area[i];
						else str += ", " + finder.list_nzp_area[i];
					}
					if (str != "")
					{
						whereString += " and d.nzp_area in (" + str + ")";
						whereStringt2 += " and d.nzp_area in (" + str + ")";
					}
					else
					{
						whereString += " and d.nzp_area = " + finder.nzp_area;
						whereStringt2 += " and d.nzp_area = " + finder.nzp_area; 
					}
				}
				else if (finder.nzp_area > 0)
				{
					whereString += " and d.nzp_area = " + finder.nzp_area;
					whereStringt2 += " and d.nzp_area = " + finder.nzp_area;
				}

				if (finder.list_nzp_wp.Count > 0)
				{
					string str = "";
					for (int i = 0; i < finder.list_nzp_wp.Count; i++)
					{
						if (i == 0) str += finder.list_nzp_wp[i];
						else str += ", " + finder.list_nzp_wp[i];
					}
					if (str != "")
					{
						whereString += " and d.nzp_wp in (" + str + ")";
						whereStringt2 += " and d.nzp_wp in (" + str + ")";
					}
				}
			}

			//Фильтр по населенному пункту
			if (finder.nzp_town > 0)
			{
				whereString = string.Format("{0} and t.nzp_town = {1}", whereString, finder.nzp_town);
				whereStringt3 = string.Format(" and t.nzp_town = {0}",  finder.nzp_town);
			}

			//Фильтр по населенному пункту
			if (finder.nzp_raj > 0)
			{
				whereString = string.Format("{0} and r.nzp_raj = {1}", whereString, finder.nzp_raj);
				whereStringt3 = string.Format(" and u.nzp_raj = {0}",  finder.nzp_raj);
			}
			if (!string.IsNullOrEmpty(finder.nzp_rajs))
			{
				whereString = string.Format("{0} and r.nzp_raj IN ({1})", whereString, finder.nzp_rajs);
				whereStringt3 = string.Format(" and u.nzp_raj IN ({0})", finder.nzp_rajs);
			}
			DbTables tables = new DbTables(connectionID);

			string zap="";
			string zap1 = ",";
			string whereString11 =  " and d.nzp_ul = u.nzp_ul ";
			if (fromString.Trim().Length == 0) {
				zap = " , ";
				whereString11 = " ";
			}
  
			ExecSQL(connectionID, " Drop table sqlt1", false);
			ExecSQL(connectionID, " Drop table sqlt2", false);
			ExecSQL(connectionID, " Drop table sqlt3", false);
			//if (!ExecSQL(connectionID, " Drop table sqlt1", false).result)
			//{
			//    connectionID.Close();
			//    return null;
			//}
			//if (!ExecSQL(connectionID, " Drop table sqlt2 ", false).result)
			//{
			//    connectionID.Close();
			//    return null;
			//}
			//if (!ExecSQL(connectionID, " Drop table sqlt3", false).result)
			//{
			//    connectionID.Close();
			//    return null;
			//}


			string sqlt1 = "Select u.nzp_ul, u.ulica, u.ulicareg, r.rajon, r.nzp_town, r.nzp_raj "+
				(tableDelimiter == "." ? " into temp sqlt1  " : "") + 
				   " from " + tables.ulica + " u " + fromString+zap1+ tables.rajon + " r " +
					"  Where u.nzp_raj = r.nzp_raj " + whereStringt1+whereStringt2+  whereString11+
					 (tableDelimiter == ":" ? " into temp sqlt1  with no log " : "")+ ";";
			if (!ExecSQL(connectionID, sqlt1, true).result)
			{
				connectionID.Close();
				return null;
			}

		  string sqlt2 = "Select u.nzp_ul, u.ulica, u.ulicareg, u.rajon , t.town" +
			  (tableDelimiter == "." ? " into temp sqlt2 " : "") + 
		   
			  " from  sqlt1 u,  " + tables.town + " t " +
			  "  where  u.nzp_town = t.nzp_town "    +whereStringt3+
			  (tableDelimiter == ":" ? " into temp sqlt2  with no log " : "") + ";";
		  if (!ExecSQL(connectionID, sqlt2, true).result)
		  {
			  connectionID.Close();
			  return null;
		  }


		  string sqlt3 = "Select u.nzp_ul, u.ulica, u.ulicareg, u.rajon , u.town" +

			  (tableDelimiter == "." ? " into temp sqlt3 " : "") + 
			   " from  sqlt2 u   "  +
				" Group by u.nzp_ul, u.ulica, u.ulicareg, u.rajon, u.town " +
				" Order by 2, 3"+
				(tableDelimiter == ":" ? " into temp sqlt3  with no log " : "") + ";";
 
		  if (!ExecSQL(connectionID, sqlt3, true).result)
		  {
			  connectionID.Close();
			  return null;
		  }


		  //string sqlString = "Select u.nzp_ul, u.ulica, u.ulicareg, r.rajon, t.town " +
		  //                    " From " + tables.ulica + " u" + fromString + "," + tables.rajon + " r, " + tables.town + " t " +
		  //                    "  Where u.nzp_raj = r.nzp_raj and r.nzp_town = t.nzp_town " + whereString +
		  //                    " Group by u.nzp_ul, u.ulica, u.ulicareg, r.rajon, t.town " +
		  //                    " Order by 2, 3";

		  string sqlString = "Select u.nzp_ul, u.ulica, u.ulicareg, u.rajon, u.town " +
							  " From sqlt3 u ;"; 
							 

		  DataTable dt = ClassDBUtils.OpenSQL(sqlString, connectionID).GetData();

		  ExecSQL(connectionID, " Drop table sqlt1", false);
		  ExecSQL(connectionID, " Drop table sqlt2", false);
		  ExecSQL(connectionID, " Drop table sqlt3", false);
			List<Ulica> ulicaList = STCLINE.KP50.Utility.OrmConvert.ConvertDataRows<Ulica>(dt.Rows, DbAdres.ToUlicaValue);

			if (ulicaList != null)
			{
				if (finder.skip > 0 && ulicaList.Count > finder.skip) ulicaList.RemoveRange(0, finder.skip);
				if (finder.rows > 0 && ulicaList.Count > finder.rows) ulicaList.RemoveRange(finder.rows, ulicaList.Count - finder.rows);
			}

			return new ReturnsObjectType<List<Ulica>>(ulicaList) { tag = ulicaList.Count };
		}

		//----------------------------------------------------------------------
		public List<_Geu> LoadGeu(Finder finder, out Returns ret)
		//----------------------------------------------------------------------
		{
			ret = Utils.InitReturns();

			Geus spis = new Geus();
			spis.GeuList.Clear();

			IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
			ret = OpenDb(conn_db, true);
			if (!ret.result) return null;

			string where = "";

			if (finder.dopFind != null && finder.dopFind.Count > 0)
				where += " and upper(geu) like '%" + finder.dopFind[0].ToUpper().Replace("'", "''").Replace("*", "%") + "%' ";

			if (finder.RolesVal != null)
				foreach (_RolesVal role in finder.RolesVal)
					if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_geu) where += " and nzp_geu in (" + role.val + ")";

			if (finder.pref.Trim() == "") finder.pref = Points.Pref;

			//Определить общее количество записей
#if PG
			string sql = "Select count(*) From " + finder.pref + "_data.s_geu Where 1 = 1 " + where;
#else
			string sql = "Select count(*) From " + finder.pref + "_data:s_geu Where 1 = 1 " + where;
#endif

			object count = ExecScalar(conn_db, sql, out ret, true);
			int recordsTotalCount;
			try { recordsTotalCount = Convert.ToInt32(count); }
			catch (Exception e)
			{
				ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
				MonitorLog.WriteLog("Ошибка LoadGeu " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
				conn_db.Close();
				return null;
			}

			//выбрать список
			IDataReader reader;
#if PG
			if (!ExecRead(conn_db, out reader,
				" Select nzp_geu, geu From " + finder.pref + "_data.s_geu Where 1 = 1 " + where + " Order by geu", true).result)
#else
			if (!ExecRead(conn_db, out reader,
				" Select nzp_geu, geu From " + finder.pref + "_data:s_geu Where 1 = 1 " + where + " Order by geu", true).result)
#endif

			{
				conn_db.Close();
				return null;
			}
			try
			{
				int i = 0;
				while (reader.Read())
				{
					i++;
					if (i <= finder.skip) continue;
					_Geu zap = new _Geu();

					if (reader["nzp_geu"] != DBNull.Value) zap.nzp_geu = (int)reader["nzp_geu"];
					if (reader["geu"] != DBNull.Value) zap.geu = Convert.ToString(reader["geu"]).Trim();

					spis.GeuList.Add(zap);
					if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
				}

				ret.tag = recordsTotalCount;

				reader.Close();
				conn_db.Close();
				return spis.GeuList;
			}
			catch (Exception ex)
			{
				reader.Close();
				conn_db.Close();
				ret = new Returns(false, ex.Message);
				MonitorLog.WriteLog("Ошибка заполнения справочника отделений " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
				return null;
			}
		}
		//----------------------------------------------------------------------
		public List<_Area> LoadArea(Finder finder, out Returns ret)
		//----------------------------------------------------------------------
		{
			ret = Utils.InitReturns();
			Areas spis = new Areas();
			spis.AreaList.Clear();
			IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
			ret = OpenDb(conn_db, true);
			if (!ret.result) return null;

			string where = "";
			if (finder.RolesVal != null)
				foreach (_RolesVal role in finder.RolesVal)
				{
					if (role.tip == Constants.role_sql & role.kod == Constants.role_sql_area)
						where += " and nzp_area in (" + role.val + ")";
				}

			if (finder.dopFind != null)
				foreach (string str in finder.dopFind)
				{
					if (str.Contains("FiltrOnDistrib"))
					{
						//фильтровать справочники по fn_distrib
						where += DbSprav.FiltrOnDistrib("nzp_area", finder.dopFind);
					}
					else
					{
						where += " and upper(area) like '%" + str.ToUpper().Replace("'", "''").Replace("*", "%") + "%'";
					}
				}

			if (finder.pref.Trim() == "") finder.pref = Points.Pref;

			//выбрать список
			IDataReader reader;
#if PG
			if (!ExecRead(conn_db, out reader,
				" Select a.nzp_area,a.area,a.nzp_supp, s.name_supp From " + finder.pref + "_data.s_area a " +
				" left outer join " + finder.pref + "_kernel.supplier s " +
				" on a.nzp_supp = s.nzp_supp and 1 = 1 " + where +
				" Order by area ", true).result)
#else
			if (!ExecRead(conn_db, out reader,
				" Select a.nzp_area,a.area,a.nzp_supp, s.name_supp From " + finder.pref + "_data:s_area a, " +
				" outer " + finder.pref + "_kernel:supplier s " +
				" Where a.nzp_supp = s.nzp_supp and 1 = 1 " + where +
				" Order by area ", true).result)
#endif

			{
				conn_db.Close();
				return null;
			}
			try
			{
				int i = 0;
				while (reader.Read())
				{
					i++;
					if (i <= finder.skip) continue;
					_Area zap = new _Area();

					if (reader["nzp_area"] == DBNull.Value)
						zap.nzp_area = 0;
					else
						zap.nzp_area = (int)reader["nzp_area"];
					if (reader["area"] == DBNull.Value)
						zap.area = "";
					else
					{
						zap.area = (string)reader["area"];
						zap.area = zap.area.Trim();
					}

					if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
					if (reader["name_supp"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["name_supp"]);

					spis.AreaList.Add(zap);
					if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
				}
				reader.Close();

#if PG
				object count = ExecScalar(conn_db, "select count(*) from " + Points.Pref + "_data.s_area where 1 = 1 " + where, out ret, true);
#else
				object count = ExecScalar(conn_db, "select count(*) from " + Points.Pref + "_data:s_area where 1 = 1 " + where, out ret, true);
#endif

				if (ret.result)
				{
					try
					{
						ret.tag = Convert.ToInt32(count);
					}
					catch (Exception ex)
					{
						ret.result = false;
						ret.text = ex.Message;
						return null;
					}
				}

				conn_db.Close();
				return spis.AreaList;
			}
			catch (Exception ex)
			{
				reader.Close();
				conn_db.Close();

				ret.result = false;
				ret.text = ex.Message;

				string err;
				if (Constants.Viewerror)
					err = " \n " + ex.Message;
				else
					err = "";

				MonitorLog.WriteLog("Ошибка заполнения справочника Управляющих организаций " + err, MonitorLog.typelog.Error, 20, 201, true);

				return null;
			}
		}
		//----------------------------------------------------------------------
		public Returns WebArea()
		//----------------------------------------------------------------------
		{
			return WebArea(0, true);
		}
		//----------------------------------------------------------------------
		public Returns WebArea(int nzp_server, bool is_insert)
		//----------------------------------------------------------------------
		{
			Returns ret = Utils.InitReturns();

			IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
			ret = OpenDb(conn_web, true);
			if (!ret.result) return ret;

			string srv = "";
			if (nzp_server > 0)
				srv = "_" + nzp_server;

			string s_area = "s_area" + srv;
#if PG
			string s_area_full = pgDefaultDb + "." + s_area;
#else
			string s_area_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + s_area;
#endif


			CreateWebArea(conn_web, s_area, out ret);
			conn_web.Close();

			if (is_insert)
			{
				IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
				ret = OpenDb(conn_db, true);
				if (!ret.result)
				{
					conn_web.Close();
					return ret;
				}

				//выбрать список
#if PG
				ret = ExecSQL(conn_db,
					" Insert into " + s_area_full + " (nzp_area, area, nzp_supp) " +
					" Select nzp_area, area, nzp_supp From " + Points.Pref + "_data.s_area", true);
#else
				ret = ExecSQL(conn_db,
					" Insert into " + s_area_full + " (nzp_area, area, nzp_supp) " +
					" Select nzp_area, area, nzp_supp From " + Points.Pref + "_data:s_area", true);
#endif


				conn_db.Close();
			}

			return ret;
		}
		//----------------------------------------------------------------------
		public Returns WebGeu()
		//----------------------------------------------------------------------
		{
			return WebGeu(0, true);
		}
		//----------------------------------------------------------------------
		public Returns WebGeu(int nzp_server, bool is_insert)
		//----------------------------------------------------------------------
		{
			Returns ret = Utils.InitReturns();

			IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
			ret = OpenDb(conn_web, true);
			if (!ret.result) return ret;

			string srv = "";
			if (nzp_server > 0)
				srv = "_" + nzp_server;

			string s_geu = "s_geu" + srv;
#if PG
			string s_geu_full = pgDefaultDb + "." + s_geu;
#else
			string s_geu_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + s_geu;
#endif


			CreateWebGeu(conn_web, s_geu, out ret);
			conn_web.Close();

			if (is_insert)
			{
				IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
				ret = OpenDb(conn_db, true);
				if (!ret.result)
				{
					conn_web.Close();
					return ret;
				}

				//выбрать список
#if PG
				ret = ExecSQL(conn_db,
					" Insert into " + s_geu_full + " (nzp_geu, geu) " +
					" Select nzp_geu, geu From " + Points.Pref + "_data.s_geu", true);
#else
				ret = ExecSQL(conn_db,
					" Insert into " + s_geu_full + " (nzp_geu, geu) " +
					" Select nzp_geu, geu From " + Points.Pref + "_data:s_geu", true);
#endif


				conn_db.Close();
			}

			return ret;
		}
		//----------------------------------------------------------------------
		public string GetFakturaName(Ls finder, out Returns ret)
		//----------------------------------------------------------------------
		{
			ret = Utils.InitReturns();

			List<Ls> LsInfo = LoadLs(finder, out ret);
			string pkod = "";
			string std_fact = "~/App_Data/web_zel.frx";

			if (ret.result)
			{
				if (LsInfo.Count > 0) pkod = LsInfo[0].pkod;
			}

			if ((pkod != "") & (pkod.Length > 2))
			{
				if (pkod.Substring(0, 3) == "270") return "~/App_Data/web_avia.frx";
				if (pkod.Substring(0, 3) == "201") return "~/App_Data/web_zel.frx";
				// return std_fact; 
			}

			if (finder.nzp_user < 1)
			{
				ret.result = false;
				ret.text = "Не определен пользователь";
				return std_fact;
			}

			if (finder.pref == "")
			{
				ret.text = "Префикс базы данных не задан";
				return std_fact;
			}

			//заполнить webdata:tXX_spls
			string conn_kernel = Points.GetConnByPref(finder.pref);
			IDbConnection conn_db = GetConnection(conn_kernel);
			//IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
			ret = OpenDb(conn_db, true);
			if (!ret.result)
			{
				conn_db.Close();
				return std_fact;
			}



			string sql = "";

			IDataReader reader;

#if PG
			sql = " Select * from " + finder.pref + "_kernel.s_listfactura where townfilter=63  ";
#else
			sql = " Select * from " + finder.pref + "_kernel:s_listfactura where townfilter=63  ";
#endif

			if (!ExecRead(conn_db, out reader, sql, true).result)
			{
				MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
				reader.Close();
				conn_db.Close();
				return std_fact;
			}
			else
			{
				if (reader.Read())
				{
					if (reader["file_name"] != DBNull.Value) std_fact = Convert.ToString(reader["file_name"]).Trim();
				}

			}
			reader.Close();
			conn_db.Close();
			return std_fact;

		}

		/// <summary>
		/// Получить список объектов на карте
		/// </summary>
		/// <param name="finder"></param>
		/// <param name="ret"></param>
		/// <returns>Список оъектов карты</returns>
		public List<MapObject> GetMapObjects(MapObject finder, out Returns ret)
		{
			ret = Utils.InitReturns();

			List<MapObject> mapObjects = new List<MapObject>();

			//соединение с БД
			IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
			ret = OpenDb(conn_web, true);
			if (!ret.result) return mapObjects;

			IDataReader reader, readerPoint;

			string sql = " Select nzp_mo, tip, kod, nzp_wp, object_type, note from map_objects";
			sql += " Where tip = " + finder.tip.GetHashCode();
			sql += " and kod = " + finder.kod.ToString();
			if (finder.nzp_wp > 0) sql += " and nzp_wp = " + finder.nzp_wp.ToString();

			ret = ExecRead(conn_web, out reader, sql, true);
			if (!ret.result)
			{
				conn_web.Close();
				return mapObjects;
			}

			try
			{
				while (reader.Read())
				{
					MapObject mapObject = new MapObject();
					if (reader["nzp_mo"] != DBNull.Value) mapObject.nzp_mo = Convert.ToInt32(reader["nzp_mo"]);
					if (reader["tip"] != DBNull.Value) mapObject.tip = MapObject.getTip(Convert.ToInt32(reader["tip"]));
					if (reader["kod"] != DBNull.Value) mapObject.kod = Convert.ToInt32(reader["kod"]);
					if (reader["nzp_wp"] != DBNull.Value) mapObject.nzp_wp = Convert.ToInt32(reader["nzp_wp"]);
					if (reader["object_type"] != DBNull.Value) mapObject.object_type = Convert.ToInt32(reader["object_type"]);
					//if (reader["note"] != DBNull.Value) mapObject.note = Convert.ToString(reader["note"]).Trim();

					#region Заполнение комментария к объекту
					Ls finderLs = new Ls();
					finderLs.nzp_user = finder.nzp_user;
					finderLs.nzp_wp = finder.nzp_wp;
					for (int i = 0; i < Points.PointList.Count; i++)
						if (Points.PointList[i].nzp_wp == finder.nzp_wp)
						{
							finderLs.pref = Points.PointList[i].pref;
							break;
						}
					Returns ret2;
					List<Ls> list;
					switch (mapObject.tip)
					{
						case MapObject.Tip.dom:
							finderLs.nzp_dom = mapObject.kod;
							list = LoadLs(finderLs, out ret2);
							if (ret2.result && list.Count > 0) mapObject.note = list[0].adr;
							break;
						case MapObject.Tip.ulica:
							finderLs.nzp_ul = mapObject.kod;
							list = LoadLs(finderLs, out ret2);
							if (ret2.result && list.Count > 0) mapObject.note = list[0].adr;
							break;
						case MapObject.Tip.geu:
							finderLs.nzp_geu = mapObject.kod;
							list = LoadLs(finderLs, out ret2);
							if (ret2.result && list.Count > 0) mapObject.note = list[0].geu;
							break;
						case MapObject.Tip.area:
							finderLs.nzp_area = Convert.ToInt32(mapObject.kod);
							list = LoadLs(finderLs, out ret2);
							if (ret2.result && list.Count > 0) mapObject.note = list[0].area;
							break;
					}
					#endregion

					sql = "Select nzp_mp, nzp_mo, x, y, ordering From map_points Where nzp_mo = " + mapObject.nzp_mo.ToString() + " Order by ordering";
					ret = ExecRead(conn_web, out readerPoint, sql, true);
					if (!ret.result)
					{
						conn_web.Close();
						return mapObjects;
					}
					while (readerPoint.Read())
					{
						_MapPoint mapPoint = new _MapPoint();
						if (readerPoint["nzp_mp"] != DBNull.Value) mapPoint.nzp_mp = Convert.ToInt32(readerPoint["nzp_mp"]);
						if (readerPoint["nzp_mo"] != DBNull.Value) mapPoint.nzp_mo = Convert.ToInt32(readerPoint["nzp_mo"]);
						if (readerPoint["x"] != DBNull.Value) mapPoint.x = Convert.ToSingle(readerPoint["x"]);
						if (readerPoint["y"] != DBNull.Value) mapPoint.y = Convert.ToSingle(readerPoint["y"]);
						if (readerPoint["ordering"] != DBNull.Value) mapPoint.ordering = Convert.ToInt32(readerPoint["ordering"]);
						mapObject.points.Add(mapPoint);
					}
					readerPoint.Close();
					mapObjects.Add(mapObject);
				}
				reader.Close();
			}
			catch (Exception ex)
			{
				reader.Close();
				ret.result = false;
				ret.text = ex.Message;
				string err = "";
				if (Constants.Viewerror) err = " \n " + ex.Message;
				MonitorLog.WriteLog("Ошибка при определении списка объектов на карте: " + err, MonitorLog.typelog.Error, 20, 201, true);
			}

			conn_web.Close();
			return mapObjects;
		}

		public Returns UpdateLsInCache(Ls finder)
		{
			Returns ret = Utils.InitReturns();
			if (finder.nzp_user < 1)
			{
				ret.result = false;
				ret.text = "Не определен пользователь";
				return ret;
			}

			#region обновление данных в выбранном списке л/с
			IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
			ret = OpenDb(conn_web, true);
			if (!ret.result)
			{
				return ret;
			}

			string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spls";

			if (TableInWebCashe(conn_web, tXX_spls))
			{
				List<Ls> list = LoadLs(finder, out ret);
				if (!ret.result)
				{
					conn_web.Close();
					return ret;
				}
				if (list.Count > 0)
				{
					string sostls = list[0].stateID == Ls.States.Open.GetHashCode() ? "открыт" : "закрыт";

					string sql = "update " + tXX_spls + " set nzp_area = " + list[0].nzp_area + ", nzp_geu = " + list[0].nzp_geu + ", fio = '" + list[0].fio +
						"', nkvar = '" + list[0].nkvar + " " + list[0].nkvar_n + "', adr ='" + list[0].adr + "', sostls ='" + sostls + "' "
						+ " where nzp_kvar = " + finder.nzp_kvar + " and pref = '" + finder.pref + "'";
					ret = ExecSQL(conn_web, sql, true);
					if (!ret.result)
					{
						conn_web.Close();
						return ret;
					}
				}
			}
			return ret;
			#endregion
		}

		//обновлние АП
		//----------------------------------------------------------------------
		bool DropRefresh(IDbConnection conn_db)
		//----------------------------------------------------------------------
		{
			ExecSQL(conn_db, "Drop table ttt_all", false);
			ExecSQL(conn_db, "Drop table ttt_kvar", false);
			ExecSQL(conn_db, "Drop table ttt_all_dom", false);
			ExecSQL(conn_db, "Drop table ttt_dom", false);
			ExecSQL(conn_db, "Drop table ttt_area", false);
			ExecSQL(conn_db, "Drop table ttt_geu", false);
			ExecSQL(conn_db, "Drop table ttt_ulica", false);
			ExecSQL(conn_db, "Drop table ttt_land", false);
			ExecSQL(conn_db, "Drop table ttt_stat", false);
			ExecSQL(conn_db, "Drop table ttt_town", false);
			ExecSQL(conn_db, "Drop table ttt_rajon", false);
			ExecSQL(conn_db, "Drop table ttt_rajondom", false);
			return false;
		}

		public Returns RefreshAP(Finder finder)
		{
			if (GlobalSettings.WorkOnlyWithCentralBank)
			{
				return new Returns(false, "Функция обновления адресного пространства недоступна, т.к. установлен режим работы с центральным банком данных", -1);
			}

			#region подключение к базе
			string conn_kernel = Points.GetConnByPref(Points.Pref);
			IDbConnection conn_db = GetConnection(conn_kernel);
			Returns ret = OpenDb(conn_db, true);
			if (!ret.result) return ret;
			#endregion

			Returns ret2;

			foreach (_Point point in Points.PointList)
			{
				RefreshAP(conn_db, point.pref, out ret2);
				if (!ret2.result)
				{
					if (ret2.tag >= 0) return ret2;
					else
					{
						ret.text += (ret.text != "" ? ", " : "") + ret2.text;
						ret.tag = ret2.tag;
					}
				}
			}

			conn_db.Close();

			if (ret.text != "") ret.text = "Обновление адресного пространства прошло с предупреждениями: " + ret.text;
			return ret;
		}

		//----------------------------------------------------------------------
		public bool RefreshAP(IDbConnection conn_db, string pref, out Returns ret)
		//----------------------------------------------------------------------
		{
			ret = Utils.InitReturns();

			DropRefresh(conn_db);
			string ol_srv = "@" + DBManager.getServer(conn_db);
			string sql;

			DbTables tables = new DbTables(conn_db);

#if PG
			string local_kvar = pref + "_data.kvar";
			string local_dom = pref + "_data.dom";
			string s_point = Points.Pref + "_kernel.s_point";
#else
			string local_kvar = pref + "_data@" + DBManager.getServer(conn_db) + ":kvar";
			string local_dom = pref + "_data@" + DBManager.getServer(conn_db) + ":dom";
			string s_point = Points.Pref + "_kernel@" + DBManager.getServer(conn_db) + ":s_point";
#endif


			if (!TempTableInWebCashe(conn_db, local_kvar))
			{
				ret.result = false;
				ret.text = "Банк данных \"" + Points.GetPoint(pref).point + "\" не доступен";
				ret.tag = -1;
				return false;
			}

			if (Points.Pref != pref)
			{
				//лицевые счета
#if PG
				sql =
						" Select nzp_kvar, num_ls, nzp_dom  Into temp ttt_all From " + local_kvar;
		/*" Where nzp_kvar in " +
			" ( Select nzp From " + pref + "_data" + ol_srv + ".prm_3 " +
			"   Where nzp_prm = 51 and today between dat_s and dat_po"+
				"  and is_actual = 1 " +
				"  and trim(coalesce(val_prm,'0')) in ('1','2') " +
			" ) " +*/
					   ;
#else
				sql =
									" Select nzp_kvar, num_ls, nzp_dom From " + local_kvar +
					/*" Where nzp_kvar in " +
						" ( Select nzp From " + pref + "_data" + ol_srv + ":prm_3 " +
						"   Where nzp_prm = 51 and today between dat_s and dat_po"+
							"  and is_actual = 1 " +
							"  and trim(nvl(val_prm,'0')) in ('1','2') " +
						" ) " +*/
									" Into temp ttt_all with no log ";
#endif


				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

				ret = ExecSQL(conn_db, "Create index inx_tall_1 on ttt_all (nzp_kvar)", true);
				ret = ExecSQL(conn_db, "Create index inx_tall_2 on ttt_all (num_ls)", true);
				ret = ExecSQL(conn_db, "Create index inx_tall_3 on ttt_all (nzp_dom)", true);
#if PG
				ret = ExecSQL(conn_db, "analyze ttt_all", true);
#else
				ret = ExecSQL(conn_db, "Update statistics for table ttt_all", true);
#endif


#if PG
				sql =
					" Select a.nzp_kvar,nzp_area,nzp_geu,a.nzp_dom,nkvar,nkvar_n,a.num_ls,fio,ikvar " +
					 " Into temp ttt_kvar "+
					" From " + local_kvar + " a, ttt_all b " +
					" Where a.nzp_kvar = b.nzp_kvar " +
					"   and a.num_ls > 0 " +
					"   and a.nzp_kvar not in ( Select nzp_kvar From " + tables.kvar + ") " ;
#else
				sql =
					" Select a.nzp_kvar,nzp_area,nzp_geu,a.nzp_dom,nkvar,nkvar_n,a.num_ls,fio,ikvar " +
					" From " + local_kvar + " a, ttt_all b " +
					" Where a.nzp_kvar = b.nzp_kvar " +
					"   and a.num_ls > 0 " +
					"   and a.nzp_kvar not in ( Select nzp_kvar From " + tables.kvar + ") " +
					" Into temp ttt_kvar with no log ";
#endif


				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

				sql =
					" Insert into " + tables.kvar +
					" (nzp_kvar,nzp_area,nzp_geu,nzp_dom,nkvar,nkvar_n,num_ls,fio,ikvar) " +
					" Select nzp_kvar,nzp_area,nzp_geu,nzp_dom,nkvar,nkvar_n,num_ls,fio,ikvar " +
					" From ttt_kvar ";

				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

#if PG
				sql = " Update " + tables.kvar +
					" Set pref = '" + pref + "'" +
						", nzp_wp = (select max(nzp_wp) from " + s_point + " p where p.bd_kernel = '" + pref + "')" +
						", is_open = coalesce(( Select max(val_prm) From " + pref + "_data.prm_3 " +
									" Where " + tables.kvar + ".nzp_kvar = nzp and nzp_prm = 51 and current_date between dat_s and dat_po and is_actual = 1)::int, " + Ls.States.Undefined.GetHashCode() + ") " +
					" Where nzp_kvar in ( Select nzp_kvar From ttt_all )";
#else
				sql = " Update " + tables.kvar +
					" Set pref = '" + pref + "'" +
						", nzp_wp = (select max(nzp_wp) from " + s_point + " p where p.bd_kernel = '" + pref + "')" +
						", is_open = nvl(( Select max(val_prm) From " + pref + "_data" + ol_srv + ":prm_3 " +
									" Where " + tables.kvar + ".nzp_kvar = nzp and nzp_prm = 51 and today between dat_s and dat_po and is_actual = 1), " + Ls.States.Undefined.GetHashCode() + ") " +
					" Where nzp_kvar in ( Select nzp_kvar From ttt_all )";
#endif

				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

				//дома
#if PG
				sql =
					" Select nzp_dom Into temp ttt_all_dom From " + local_dom;
#else
				sql =
					" Select nzp_dom From " + local_dom +
					" Into temp ttt_all_dom with no log ";
#endif


				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

#if PG
				sql =
					" Select nzp_dom,nzp_land,nzp_stat,nzp_town,nzp_raj,nzp_ul,nzp_area,nzp_geu, " +
					"        idom,ndom,nkor,indecs,nzp_bh,kod_uch Into temp ttt_dom From " + local_dom +
					" Where nzp_dom not in        " +
					" ( Select nzp_dom From " + tables.dom + ") ";
#else
				sql =
									" Select nzp_dom,nzp_land,nzp_stat,nzp_town,nzp_raj,nzp_ul,nzp_area,nzp_geu, " +
									"        idom,ndom,nkor,indecs,nzp_bh,kod_uch From " + local_dom +
									" Where nzp_dom not in        " +
									" ( Select nzp_dom From " + tables.dom + ") " +
									" Into temp ttt_dom with no log ";
#endif


				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

				sql =
					" Insert into " + tables.dom +
					" (nzp_dom,nzp_land,nzp_stat,nzp_town,nzp_raj,nzp_ul,nzp_area,nzp_geu, " +
					"        idom,ndom,nkor,indecs,nzp_bh,kod_uch ) " +
					" Select nzp_dom,nzp_land,nzp_stat,nzp_town,nzp_raj,nzp_ul,nzp_area,nzp_geu, " +
					"        idom,ndom,nkor,indecs,nzp_bh,kod_uch From ttt_dom ";

				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

				sql =
					" Update " + tables.dom +
					" Set pref = '" + pref + "'" +
					", nzp_wp = (select max(nzp_wp) from " + s_point + " p where p.bd_kernel = '" + pref + "')" +
					" Where nzp_dom in ( Select nzp_dom From ttt_all_dom )";

				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

				//area
#if PG
				sql =
					" Select nzp_area,area Into temp ttt_area From " + pref + "_data.s_area " +
					" Where nzp_area not in " +
					" ( Select nzp_area From " + tables.area + ") ";
#else
				sql =
								   " Select nzp_area,area From " + pref + "_data" + ol_srv + ":s_area " +
								   " Where nzp_area not in " +
								   " ( Select nzp_area From " + tables.area + ") " +
								   " Into temp ttt_area with no log ";
#endif


				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

				sql =
					" Insert into " + tables.area + " (nzp_area,area) " +
					" Select nzp_area,area From ttt_area ";

				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

				//geu
#if PG
				sql =
					" Select nzp_geu,geu Into temp ttt_geu From " + pref + "_data.s_geu " +
					" Where nzp_geu not in " +
					" ( Select nzp_geu From " + tables.geu + " ) ";
#else
				sql =
								   " Select nzp_geu,geu From " + pref + "_data" + ol_srv + ":s_geu " +
								   " Where nzp_geu not in " +
								   " ( Select nzp_geu From " + tables.geu + " ) " +
								   " Into temp ttt_geu with no log ";
#endif


				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

				sql =
					" Insert into " + tables.geu + " (nzp_geu,geu) " +
					" Select nzp_geu,geu From ttt_geu ";

				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

				//ulica
#if PG
				sql =
					" Select nzp_ul,ulica,nzp_raj Into temp ttt_ulica From " + pref + "_data.s_ulica " +
					" Where nzp_ul not in " +
					" ( Select nzp_ul From " + tables.ulica + " ) ";
#else
				sql =
									" Select nzp_ul,ulica,nzp_raj From " + pref + "_data" + ol_srv + ":s_ulica " +
									" Where nzp_ul not in " +
									" ( Select nzp_ul From " + tables.ulica + " ) " +
									" Into temp ttt_ulica with no log ";
#endif


				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

				sql =
					" Insert into " + tables.ulica + " (nzp_ul,ulica,nzp_raj) " +
					" Select nzp_ul,ulica,nzp_raj From ttt_ulica ";

				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

				//kvar
#if PG
				sql = "Update " + tables.kvar + " Set nzp_area = k.nzp_area, nzp_geu = k.nzp_geu, nzp_dom = k.nzp_dom, " +
													" num_ls=k.num_ls, nkvar=k.nkvar, nkvar_n=k.nkvar_n, porch=k.porch, phone=k.phone, uch=k.uch, " +
													" ikvar=k.ikvar, fio=k.fio, pkod=k.pkod, pkod10=k.pkod10, typek=k.typek, remark=k.remark "+
					" From "+local_kvar+ " k "+
					" Where "+tables.kvar +".nzp_kvar = k.nzp_kvar and "+tables.kvar +".nzp_kvar in ( Select nzp_kvar From ttt_all )";
#else
				sql =
									" Update " + tables.kvar +
									" Set (nzp_area, nzp_geu, nzp_dom, num_ls, nkvar, nkvar_n, porch, phone, uch, ikvar, fio, pkod, pkod10, typek, remark) =" +
									" ((Select nzp_area, nzp_geu, nzp_dom, num_ls, nkvar, nkvar_n, porch, phone, uch, ikvar, fio, pkod, pkod10, typek, remark From " + local_kvar + " k Where " + tables.kvar + ".nzp_kvar = k.nzp_kvar ))" +
									" Where nzp_kvar in ( Select nzp_kvar From ttt_all ) ";
#endif

				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

				//dom
#if PG
				sql = " Update " + tables.dom + " Set nzp_area = d.nzp_area, nzp_geu = d.nzp_geu, nzp_bh = d.nzp_bh, nzp_ul=d.nzp_ul, ndom=d.ndom, nkor=d.nkor "+
					  " From " + local_dom + " d "+
					  " Where " + tables.dom + ".nzp_dom=d.nzp_dom and " + tables.dom + ".nzp_dom in ( Select nzp_dom From ttt_all_dom ) ";
#else
				sql =
								   " Update " + tables.dom +
								   " Set (nzp_area, nzp_geu, nzp_geu, nzp_bh, nzp_ul, ndom, nkor) = ((Select nzp_area, nzp_geu, nzp_geu, nzp_bh, nzp_ul, ndom, nkor From " + local_dom + " d Where " + tables.dom + ".nzp_dom=d.nzp_dom))" +
								   " Where nzp_dom in ( Select nzp_dom From ttt_all_dom ) ";
#endif

				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

				//land
#if PG
				sql =
					" Select nzp_land, land, land_t, soato Into temp ttt_land From " + pref + "_data.s_land " +
					" Where nzp_land not in " +
					" ( Select nzp_land From " + tables.land + ") ";
#else
				sql =
					" Select nzp_land, land, land_t, soato From " + pref + "_data" + ol_srv + ":s_land " +
					" Where nzp_land not in " +
					" ( Select nzp_land From " + tables.land + ") " +
					" Into temp ttt_land with no log ";
#endif
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

				sql =
					" Insert into " + tables.land + " ( nzp_land, land, land_t, soato) " +
					" Select  nzp_land, land, land_t, soato From ttt_land ";

				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

				//stat
#if PG
				sql =
					" Select nzp_stat, nzp_land, stat, stat_t, soato Into temp ttt_stat From " + pref + "_data.s_stat " +
					" Where nzp_stat not in " +
					" ( Select nzp_stat From " + tables.stat + ") ";
#else
				sql =
					" Select nzp_stat, nzp_land, stat, stat_t, soato From " + pref + "_data" + ol_srv + ":s_stat " +
					" Where nzp_stat not in " +
					" ( Select nzp_stat From " + tables.stat + ") " +
					" Into temp ttt_stat with no log ";
#endif
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

				sql =
					" Insert into " + tables.stat + " (nzp_stat, nzp_land, stat, stat_t, soato) " +
					" Select nzp_stat, nzp_land, stat, stat_t, soato From ttt_stat ";

				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

				//town
#if PG
				sql =
					" Select nzp_town, nzp_stat, town, town_t, soato Into temp ttt_town From " + pref + "_data.s_town " +
					" Where nzp_town not in " +
					" ( Select nzp_town From " + tables.town + ") ";
#else
				sql =
					" Select nzp_town, nzp_stat, town, town_t, soato From " + pref + "_data" + ol_srv + ":s_town " +
					" Where nzp_town not in " +
					" ( Select nzp_town From " + tables.town + ") " +
					" Into temp ttt_town with no log ";
#endif
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

				sql =
					" Insert into " + tables.town + " (nzp_town, nzp_stat, town, town_t, soato) " +
					" Select nzp_town, nzp_stat, town, town_t, soato From ttt_town ";

				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

				//s_rajon
#if PG
				sql =
					" Select nzp_raj, nzp_town, rajon, rajon_t, soato  Into unlogged ttt_rajon "+
					 " From " + pref + "_data.s_rajon " +
					" Where not  exists " +
					" ( Select nzp_raj From " + tables.rajon + " r where r.nzp_raj = nzp_raj) ";
#else
				sql =
					" Select nzp_raj, nzp_town, rajon, rajon_t, soato From " + pref + "_data" + ol_srv + ":s_rajon " +
					" Where nzp_raj not in " +
					" ( Select nzp_raj From " + tables.rajon + ") " +
					" Into temp ttt_rajon with no log ";
#endif
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

				sql =
					" Insert into " + tables.rajon + " (nzp_raj, nzp_town, rajon, rajon_t, soato) " +
					" Select nzp_raj, nzp_town, rajon, rajon_t, soato From ttt_rajon ";

				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

				//s_rajondom
#if PG
				string local_rajondom = pref + "_data.s_rajon_dom";
#else
				string local_rajondom = pref + "_data" + ol_srv + ":s_rajon_dom";
#endif
				if (TempTableInWebCashe(conn_db, local_rajondom) && TempTableInWebCashe(conn_db, tables.rajon_dom))
				{
#if PG
				sql =
					" Select nzp_raj_dom, rajon_dom, alt_rajon_dom Into temp ttt_rajondom From " + local_rajondom +
					" Where nzp_raj_dom not in " +
					" ( Select nzp_raj_dom From " + tables.rajon_dom + ") ";
#else
					sql =
						" Select nzp_raj_dom, rajon_dom, alt_rajon_dom From " + local_rajondom +
						" Where nzp_raj_dom not in " +
						" ( Select nzp_raj_dom From " + tables.rajon_dom + ") " +
						" Into temp ttt_rajondom with no log ";
#endif
					ret = ExecSQL(conn_db, sql, true);
					if (!ret.result) return DropRefresh(conn_db);

					sql =
						" Insert into " + tables.rajon_dom + " (nzp_raj_dom, rajon_dom, alt_rajon_dom) " +
						" Select nzp_raj_dom, rajon_dom, alt_rajon_dom From ttt_rajondom ";

					ret = ExecSQL(conn_db, sql, true);
					if (!ret.result) return DropRefresh(conn_db);
				}

			}
			else
			{
#if PG
				sql = " Update " + tables.kvar +
					" Set pref = '" + pref + "'" +
						", nzp_wp = (select max(nzp_wp) from " + s_point + " p where p.bd_kernel = '" + pref + "')" +
						", is_open = coalesce(( Select max(val_prm) From " + pref + "_data.prm_3 " +
									" Where " + tables.kvar + ".nzp_kvar = nzp and nzp_prm = 51 and current_date between dat_s and dat_po and is_actual = 1)::int, " + Ls.States.Undefined.GetHashCode() + ") ";
#else
				sql = " Update " + tables.kvar +
					" Set pref = '" + pref + "'" +
						", nzp_wp = (select max(nzp_wp) from " + s_point + " p where p.bd_kernel = '" + pref + "')" +
						", is_open = nvl(( Select max(val_prm) From " + pref + "_data" + ol_srv + ":prm_3 " +
									" Where " + tables.kvar + ".nzp_kvar = nzp and nzp_prm = 51 and today between dat_s and dat_po and is_actual = 1), " + Ls.States.Undefined.GetHashCode() + ") ";
#endif

				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return DropRefresh(conn_db);

				sql =
					" Update " + tables.dom +
					" Set pref = '" + pref + "'" +
					", nzp_wp = (select max(nzp_wp) from " + s_point + " p where p.bd_kernel = '" + pref + "')";
			}



			DropRefresh(conn_db);
			return true;
		}
			

		public Returns RefreshKvar(IDbConnection conn_db, IDbTransaction transaction, Ls finder)
		{
			if (finder.pref == "") return new Returns(false, "Не задан префикс БД");

			DbTables tables = new DbTables(conn_db);
#if PG
			string local_kvar = finder.pref + "_data.kvar";
			string s_point = Points.Pref + "_kernel.s_point";
			string prm_3 = finder.pref + "_data.prm_3";
#else
			string local_kvar = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":kvar";
			string s_point = Points.Pref + "_kernel@" + DBManager.getServer(conn_db) + ":s_point";
			string prm_3 = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":prm_3";
#endif


			string sql = "select nzp_kvar from " + tables.kvar + " where nzp_kvar = " + finder.nzp_kvar;
			IDataReader reader;
			Returns ret = ExecRead(conn_db, transaction, out reader, sql, true);
			if (!ret.result) return ret;

			if (!reader.Read())
			{
				int code = GetAreaCodes(conn_db, transaction, finder, out ret);
				if (!ret.result)
				{
					MonitorLog.WriteLog("Ошибка получения текущего area_codes.code", MonitorLog.typelog.Error, true);
				}
				string cd = "0";
				if (code > 0) cd = code.ToString();

				sql =
				 " Insert into " + tables.kvar +
				 " (nzp_kvar,nzp_area,nzp_geu,nzp_dom,nkvar,nkvar_n,num_ls,fio,ikvar,area_code) " +
				 " Select nzp_kvar,nzp_area,nzp_geu,nzp_dom,nkvar,nkvar_n,num_ls,fio,ikvar, " + cd +
				 " From " + local_kvar + " where nzp_kvar = " + finder.nzp_kvar;

				ret = ExecSQL(conn_db, transaction, sql, true);
				if (!ret.result) return ret;
			}
			reader.Close();
			reader.Dispose();

			sql = " Update " + tables.kvar +
			 " Set pref = '" + finder.pref + "'" +
				", nzp_wp = (select max(nzp_wp) from " + s_point + " p where p.bd_kernel = '" + finder.pref + "')" +
			 " Where nzp_kvar =" + finder.nzp_kvar;

			ret = ExecSQL(conn_db, transaction, sql, true);
			if (!ret.result) return ret;

#if PG
			sql = " Update " + tables.kvar +
			 " Set is_open = coalesce(" + ("( Select max(val_prm) From " + prm_3 +
							 " Where " + tables.kvar + ".nzp_kvar = nzp and nzp_prm = 51 and current_date between dat_s and dat_po and is_actual = 1)").CastTo("INTEGER") + ", " + Ls.States.Undefined.GetHashCode() + ") " +
			 " Where nzp_kvar =" + finder.nzp_kvar;
#else
			sql = " Update " + tables.kvar +
			 " Set is_open = nvl(( Select max(val_prm) From " + prm_3 +
							 " Where " + tables.kvar + ".nzp_kvar = nzp and nzp_prm = 51 and today between dat_s and dat_po and is_actual = 1), " + Ls.States.Undefined.GetHashCode() + ") " +
			 " Where nzp_kvar =" + finder.nzp_kvar;
#endif
			ret = ExecSQL(conn_db, transaction, sql, true);
			if (!ret.result) return ret;

#if PG
			sql = " Update " + tables.kvar;
			sql = sql.UpdateSet(
								"nzp_area, nzp_geu, nzp_dom, num_ls, nkvar, nkvar_n, porch, phone, uch, ikvar, fio, pkod, pkod10, typek, remark",
				"nzp_area, nzp_geu, nzp_dom, num_ls, nkvar, nkvar_n, porch, phone, uch, ikvar, fio, pkod, pkod10, typek, remark", "sub");
			sql += " from ( select nzp_area, nzp_geu, nzp_dom, num_ls, nkvar, nkvar_n, porch, phone, uch, ikvar, fio, pkod, pkod10, typek, remark From " + local_kvar + " k Where k.nzp_kvar = " + finder.nzp_kvar + ") as sub" + " Where nzp_kvar = " + finder.nzp_kvar;
#else
			sql = " Update " + tables.kvar +
			 " Set (nzp_area, nzp_geu, nzp_dom, num_ls, nkvar, nkvar_n, porch, phone, uch, ikvar, fio, pkod, pkod10, typek, remark) =" +
			 " ((Select nzp_area, nzp_geu, nzp_dom, num_ls, nkvar, nkvar_n, porch, phone, uch, ikvar, fio, pkod, pkod10, typek, remark From " + local_kvar + " k Where " + tables.kvar + ".nzp_kvar = k.nzp_kvar ))" +
			 " Where nzp_kvar = " + finder.nzp_kvar;
#endif
			ret = ExecSQL(conn_db, transaction, sql, true);
			return ret;
		}

		public Returns RefreshDom(IDbConnection conn_db, Dom finder)
		{
			if (finder.pref == "") return new Returns(false, "Не задан префикс БД");

			DbTables tables = new DbTables(conn_db);
#if PG
			string local_dom = finder.pref + "_data.dom";
			string s_point = Points.Pref + "_kernel.s_point";
#else
			string local_dom = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":dom";
			string s_point = Points.Pref + "_kernel@" + DBManager.getServer(conn_db) + ":s_point";
#endif


			string sql = "select nzp_dom from " + tables.dom + " where nzp_dom = " + finder.nzp_dom;
			IDataReader reader;
			Returns ret = ExecRead(conn_db, out reader, sql, true);
			if (!ret.result) return ret;

			if (!reader.Read())
			{
				sql =
				 " Insert into " + tables.dom +
				 " (nzp_dom,nzp_area,nzp_geu,nzp_ul,idom,ndom,nkor,nzp_land,nzp_stat,nzp_town,nzp_raj) " +
				 " Select nzp_dom,nzp_area,nzp_geu,nzp_ul,idom,ndom,nkor,nzp_land,nzp_stat,nzp_town,nzp_raj " +
				 " From " + local_dom + " where nzp_dom = " + finder.nzp_dom;

				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result) return ret;
			}
			reader.Close();
			reader.Dispose();

			sql = " Update " + tables.dom +
			 " Set pref = '" + finder.pref + "'" +
				", nzp_wp = (select max(nzp_wp) from " + s_point + " p where p.bd_kernel = '" + finder.pref + "')" +
			 " Where nzp_dom =" + finder.nzp_dom;

			ret = ExecSQL(conn_db, sql, true);
			if (!ret.result) return ret;

#if PG
			sql = "WITH tt AS " +
				  "(SELECT d.nzp_area, " +
						  "d.nzp_geu, " +
						  "d.nzp_ul, " +
						  "d.idom, " +
						  "d.ndom, " +
						  "d.nkor, " +
						  "d.nzp_land, " +
						  "d.nzp_stat, " +
						  "d.nzp_town, " +
						  "d.nzp_raj " +
				   "FROM " + local_dom + " d WHERE d.nzp_dom = " + finder.nzp_dom + ") " +
				   "UPDATE " + tables.dom + " " +
				   "SET nzp_area = tt.nzp_area, " +
				   "nzp_geu = tt.nzp_geu, " +
				   "nzp_ul = tt.nzp_ul, " +
				   "idom = tt.idom, " +
				   "ndom = tt.ndom, " +
				   "nkor = tt.nkor, " +
				   "nzp_land = tt.nzp_land, " +
				   "nzp_stat = tt.nzp_stat, " +
				   "nzp_town = tt.nzp_town, " +
				   "nzp_raj = tt.nzp_raj " +
				   "FROM tt " +
				   "WHERE nzp_dom = " + finder.nzp_dom;
#else
			sql = " Update " + tables.dom +
				" Set (nzp_area,nzp_geu,nzp_ul,idom,ndom,nkor,nzp_land,nzp_stat,nzp_town,nzp_raj) =" +
				" ((Select nzp_area,nzp_geu,nzp_ul,idom,ndom,nkor,nzp_land,nzp_stat,nzp_town,nzp_raj From " + local_dom + " d Where " + tables.dom + ".nzp_dom = d.nzp_dom ))" +
				" Where nzp_dom = " + finder.nzp_dom;
#endif

			ret = ExecSQL(conn_db, sql, true);
			return ret;
		}


		public Returns SaveGeu(Geu finder)
		{
			if (finder.nzp_user < 1) return new Returns(false, "Не задан пользователь");
			if (finder.geu.Trim() == "") return new Returns(false, "Не задано наименование отделения");
			finder.geu = finder.geu.Trim();

			#region подключение к базе
			string conn_kernel = Points.GetConnByPref(Points.Pref);
			IDbConnection conn_db = GetConnection(conn_kernel);
			Returns ret = OpenDb(conn_db, true);
			if (!ret.result) return ret;
			#endregion

			if (finder.pref == "") finder.pref = Points.Pref;
			string table = finder.pref + "_data" + DBManager.tableDelimiter + "s_geu";
			string sql;
			if (finder.nzp_geu > 0)
			{
				sql = "select nzp_geu from " + table + " where nzp_geu = " + finder.nzp_geu;
				IDataReader reader;
				ret = ExecRead(conn_db, out reader, sql, true);
				if (!ret.result)
				{
					conn_db.Close();
					return ret;
				}
				if (reader.Read())
				{
					sql = "update " + table + " set geu = " + Utils.EStrNull(finder.geu) + " where nzp_geu = " + finder.nzp_geu;
				}
				else
				{
					sql = "insert into " + table + " (nzp_geu, geu) values (" + finder.nzp_geu + ", " + Utils.EStrNull(finder.geu) + ")";
				}
				CloseReader(ref reader);
			}
			else
			{
				DbSprav db = new DbSprav();
				ret = db.GetNewId(conn_db, Series.Types.Geu);
				if (!ret.result)
				{
					conn_db.Close();
					return ret;
				}

				finder.nzp_geu = ret.tag;

				sql = "insert into " + table + " (nzp_geu, geu) values (" + finder.nzp_geu + ", " + Utils.EStrNull(finder.geu) + ")";
			}

			ret = ExecSQL(conn_db, sql, true);
			if (ret.result && finder.nzp_geu < 1)
			{
				ret.tag = GetSerialValue(conn_db);
			}
			conn_db.Close();

			return ret;
		}

		public ReturnsObjectType<Ls> GetLsLocation(Ls finder, IDbConnection connection)
		{
		   var wk = new WorkTempKvar();
		   var result = wk.GetLsLocation(finder, connection, null);
		   wk.Close();
		   return result;
		}

	 


		//----------------------------------------------------------------------
		public List<Ulica> UlicaLoad(Ulica finder, out Returns ret)
		//----------------------------------------------------------------------
		{
			ret = Utils.InitReturns();
			if (finder.pref == "") finder.pref = Points.Pref;
			List<Ulica> spis = new List<Ulica>();

			IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
			ret = OpenDb(conn_db, true);
			if (!ret.result) return null;

			DbTables tables = new DbTables(conn_db);

			// Условия поиска
			string where = "";

			if ((finder.nzp_ul != 0) && (finder.nzp_ul != Constants._ZERO_))
				where += " and s.nzp_ul = " + finder.nzp_ul;

			if ((finder.nzp_raj != 0) && (finder.nzp_raj != Constants._ZERO_))
				where += " and s.nzp_raj = " + finder.nzp_raj;

			if (finder.ulica.Trim() != "")
				where += " and upper(s.ulica) like '%" + finder.ulica.ToUpper().Replace("'", "''").Replace("*", "%") + "%' ";

			//if (finder.RolesVal != null)
			//    foreach (_RolesVal role in finder.RolesVal)
			//        if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_supp) where += " and s.nzp_supp in (" + role.val + ")";

			//Определить общее количество записей
			string sql = "Select count(*) From " + tables.ulica + " s Where 1 = 1 " + where;
			object count = ExecScalar(conn_db, sql, out ret, true);
			int recordsTotalCount;
			try { recordsTotalCount = Convert.ToInt32(count); }
			catch (Exception e)
			{
				ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
				MonitorLog.WriteLog("Ошибка UlicaLoad " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
				conn_db.Close();
				return null;
			}

			//выбрать список
			sql = " Select * " +
				  " From " + tables.ulica + " s " +
				  " Where 1 = 1 " + where +
				  " Order by ulica";

			IDataReader reader;
			if (!ExecRead(conn_db, out reader, sql, true).result)
			{
				conn_db.Close();
				return null;
			}
			try
			{
				int i = 0;
				while (reader.Read())
				{
					i++;
					if (i <= finder.skip) continue;
					Ulica zap = new Ulica();
					zap.num = i.ToString();

					if (reader["nzp_ul"] != DBNull.Value) zap.nzp_ul = Convert.ToInt32(reader["nzp_ul"]);
					if (reader["ulica"] != DBNull.Value) zap.ulica = Convert.ToString(reader["ulica"]).Trim();
					if (reader["ulicareg"] != DBNull.Value) zap.ulicareg = Convert.ToString(reader["ulicareg"]).Trim();

					spis.Add(zap);
					if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
				}

				ret.tag = recordsTotalCount;

				reader.Close();
				conn_db.Close();
				return spis;
			}
			catch (Exception ex)
			{
				reader.Close();
				conn_db.Close();
				ret = new Returns(false, ex.Message);
				MonitorLog.WriteLog("Ошибка заполнения списка улиц " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
				return null;
			}
		}

		//----------------------------------------------------------------------
		public Prefer GetPrefer(out Returns ret)
		//----------------------------------------------------------------------
		{
			ret = Utils.InitReturns();
			Prefer prfr = new Prefer();

			IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
			ret = OpenDb(conn_db, true);
			if (!ret.result) return null;

			//выбрать 
#if PG
			String sql = " Select * " + " From " + Points.Pref + "_data.prefer s ";
#else
			String sql = " Select * " + " From " + Points.Pref + "_data:prefer s ";
#endif


			IDataReader reader;
			if (!ExecRead(conn_db, out reader, sql, true).result)
			{
				conn_db.Close();
				return null;
			}
			try
			{
				while (reader.Read())
				{
					if (Convert.ToString(reader["p_name"]).Trim() == "nzp_lang_rg")
						prfr.land = Convert.ToString(reader["p_value"]).Trim();
					if (Convert.ToString(reader["p_name"]).Trim() == "nzp_lang_rg")
						prfr.nzp_land = Convert.ToInt32(reader["p_value"]);
					if (Convert.ToString(reader["p_name"]).Trim() == "stat_rg")
						prfr.stat = Convert.ToString(reader["p_value"]).Trim();
					if (Convert.ToString(reader["p_name"]).Trim() == "nzp_stat_rg")
						prfr.nzp_stat = Convert.ToInt32(reader["p_value"]);
					if (Convert.ToString(reader["p_name"]).Trim() == "town_rg")
						prfr.town = Convert.ToString(reader["p_value"]).Trim();
					if (Convert.ToString(reader["p_name"]).Trim() == "nzp_town_rg")
						prfr.nzp_town = Convert.ToInt32(reader["p_value"]);
					if (Convert.ToString(reader["p_name"]).Trim() == "rajon_rg")
						prfr.rajon = Convert.ToString(reader["p_value"]).Trim();
					if (Convert.ToString(reader["p_name"]).Trim() == "nzp_raj_rg")
						prfr.nzp_raj = Convert.ToInt32(reader["p_value"]);
				}
				reader.Close();
				conn_db.Close();
				return prfr;

			}
			catch (Exception ex)
			{
				reader.Close();
				conn_db.Close();
				ret = new Returns(false, ex.Message);
				MonitorLog.WriteLog("Ошибка заполнения настройки на район " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
				return null;
			}
		}

		/// <summary>
		/// Создать кэш-таблицу для последних показаний приборов учета
		/// </summary>
		private void CreateTableWebUniquePointAreaGet(IDbConnection conn_web, string tXX_cv, bool onCreate, out Returns ret) //
		{
			if (onCreate)
			{
				if (TableInWebCashe(conn_web, tXX_cv))
				{
					ExecSQL(conn_web, " Drop table " + tXX_cv, false);
				}

				//создать таблицу webdata:tXX_cv
#if PG
				ret = ExecSQL(conn_web,
					  " Create table " + tXX_cv + "(" +
					  " nzp_wp   integer," +
					  " nzp_area integer," +
					  " nzp_geu  integer," +
					  " point    CHARACTER(100)," +
					  " area     CHARACTER(40)," +
					  " geu      CHARACTER(60) " +
					  ")", true);
#else
				ret = ExecSQL(conn_web,
					  " Create table " + tXX_cv + "(" +
					  " nzp_wp   integer," +
					  " nzp_area integer," +
					  " nzp_geu  integer," +
					  " point    nchar(100)," +
					  " area     nchar(40)," +
					  " geu      nchar(60) " +
					  ")", true);
#endif


				if (!ret.result)
				{
					conn_web.Close();
					return;
				}
			}
			else
			{
				ret = ExecSQL(conn_web, " Create index ix1_" + tXX_cv + " on " + tXX_cv + " (nzp_wp) ", true);
				if (ret.result) { ret = ExecSQL(conn_web, " Create index ix_2" + tXX_cv + " on " + tXX_cv + " (nzp_area) ", true); }
				if (ret.result) { ret = ExecSQL(conn_web, " Create index ix_3" + tXX_cv + " on " + tXX_cv + " (nzp_geu) ", true); }
			}
		}

		/// <summary>
		/// Получить cписок всех уникальных сочетаний банков данных, Управляющая организация, отделений
		/// </summary>
		/// <param name="finder"></param>
		/// <param name="ret"></param>
		/// <returns></returns>
		public List<Ls> GetUniquePointAreaGeu(Ls finder, out Returns ret)
		{
			if (finder.nzp_user < 1)
			{
				ret = new Returns(false, "Не определен пользователь", -1);
				return null;
			}

			ret = Utils.InitReturns();

			IDbConnection conn_db = GetConnection(Points.GetConnByPref(Points.Pref));
			ret = OpenDb(conn_db, true);
			if (!ret.result) return null;

#if PG
			string from = Points.Pref + "_data.kvar k, " + Points.Pref + "_data.s_area a, " + Points.Pref + "_data.s_geu g, " + Points.Pref + "_kernel.s_point p ";
#else
			string from = Points.Pref + "_data: kvar k, " + Points.Pref + "_data: s_area a, " + Points.Pref + "_data: s_geu g, " + Points.Pref + "_kernel: s_point p ";
#endif

			string where = " k.nzp_area = a.nzp_area " +
						   " and k.nzp_geu = g.nzp_geu " +
						   " and k.pref = p.bd_kernel";

			string sql = " Select distinct k.nzp_area, k.nzp_geu, p.nzp_wp, p.point, a.area, g.geu " +
				" From " + from + " Where " + where + " Order by p.point, a.area, g.geu ";

			IDataReader reader;

			ret = ExecRead(conn_db, out reader, sql, true);
			if (!ret.result)
			{
				conn_db.Close();
				return null;
			}
			try
			{
				List<Ls> Spis = new List<Ls>();
				Ls zap;

				int i = 0;
				while (reader.Read())
				{
					zap = new Ls();

					i++;
					zap.num = i.ToString();

					if (reader["nzp_wp"] != DBNull.Value) zap.nzp_wp = Convert.ToInt32(reader["nzp_wp"]);
					if (reader["point"] != DBNull.Value) zap.point = Convert.ToString(reader["point"]);
					if (reader["nzp_area"] != DBNull.Value) zap.nzp_area = Convert.ToInt32(reader["nzp_area"]);
					if (reader["area"] != DBNull.Value) zap.area = Convert.ToString(reader["area"]).Trim();
					if (reader["nzp_geu"] != DBNull.Value) zap.nzp_geu = Convert.ToInt32(reader["nzp_geu"]);
					if (reader["geu"] != DBNull.Value) zap.geu = Convert.ToString(reader["geu"]).Trim();

					Spis.Add(zap);
				}

				reader.Close();
				conn_db.Close();
				ret.tag = i--;
				return Spis;
			}
			catch (Exception ex)
			{
				reader.Close();
				conn_db.Close();

				ret = new Returns(false, ex.Message, -1);
				MonitorLog.WriteLog("Ошибка заполнения списка для добавления заданий на формирование платежных документов\n" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);

				return null;
			}
		}

		public List<Vill> LoadVill(Vill finder, out Returns ret)
		{
			ret = Utils.InitReturns();
			if (finder.nzp_user <= 0)
			{
				ret.result = false;
				ret.text = "Не задан пользователь";
				return null;
			}

			string where = "";
			string sql = "";

			if (finder.nzp_vill > 0)
			{
				where += " and nzp_vill = " + finder.nzp_vill;
			}

			if (finder.vill.Trim() != "")
			{
				where += " and vill like%'" + finder.vill + "'%";
			}

			string connectionString = Points.GetConnByPref(finder.pref);
			IDbConnection conn_db = GetConnection(connectionString);
			ret = OpenDb(conn_db, true);
			if (!ret.result) return null;
			IDataReader reader = null;
			List<Vill> list = new List<Vill>();
			try
			{
				DbTables tables = new DbTables(conn_db);

				#region определить количество записей
				sql = "select count(*) from " + tables.vill + " v, " + tables.sr_rajon + " r where r.kod_raj = v.kod_raj " + where;
				object count = ExecScalar(conn_db, sql, out ret, true);
				int recordsTotalCount;
				try { recordsTotalCount = Convert.ToInt32(count); }
				catch (Exception e)
				{
					ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
					MonitorLog.WriteLog("Ошибка LoadVill " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
					conn_db.Close();
					return null;
				}
				#endregion

				sql = "select nzp_vill, vill, rajon " +
						"from  " + tables.vill + " v, " + tables.sr_rajon + " r where r.kod_raj = v.kod_raj " + where + " order by vill";

				ret = ExecRead(conn_db, out reader, sql, true);
				if (!ret.result)
				{
					conn_db.Close();
					return null;
				}
				int i = 0;
				while (reader.Read())
				{
					i++;
					if (i <= finder.skip) continue;
					Vill vill = new Vill();
					vill.num = i.ToString();
					if (reader["nzp_vill"] != DBNull.Value) vill.nzp_vill = Convert.ToDecimal(reader["nzp_vill"]);
					if (reader["vill"] != DBNull.Value) vill.vill = Convert.ToString(reader["vill"]).Trim();

					if (reader["rajon"] != DBNull.Value) vill.vill += " / " + Convert.ToString(reader["rajon"]).Trim();

					if (finder.nzp_vill > 0)
					{
						sql = "select count(*) from " + tables.rajon_vill + " where nzp_vill = " + vill.nzp_vill;
						object obj = ExecScalar(conn_db, sql, out ret, true);
						try { vill.vill += " (Количество населенных пунктов: " + Convert.ToString(obj) + ")"; }
						catch (Exception e)
						{
							ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
							MonitorLog.WriteLog("Ошибка LoadVill " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
							conn_db.Close();
							return null;
						}
					}

					list.Add(vill);
					if (finder.rows > 0 && list.Count >= finder.rows) break;
				}
				reader.Close();

				ret.tag = recordsTotalCount;
				return list;
			}
			catch (Exception ex)
			{

				CloseReader(ref reader);
				conn_db.Close();

				ret.result = false;
				ret.text = ex.Message;

				string err = "";
				if (Constants.Viewerror) err = " \n " + ex.Message;

				MonitorLog.WriteLog("Ошибка получения LoadVill " + err, MonitorLog.typelog.Error, 20, 201, true);
				return null;
			}
		}

		public List<Rajon> LoadVillRajon(Rajon finder, out Returns ret)
		{
			ret = Utils.InitReturns();
			if (finder.nzp_user <= 0)
			{
				ret.result = false;
				ret.text = "Не задан пользователь";
				return null;
			}

			string where = "";
			string sql = "";

			if (finder.nzp_raj > 0)
			{
				where += " and nzp_raj = " + finder.nzp_raj;
			}

			string connectionString = Points.GetConnByPref(finder.pref);
			IDbConnection conn_db = GetConnection(connectionString);
			ret = OpenDb(conn_db, true);
			if (!ret.result) return null;

			DbTables tables = new DbTables(conn_db);
			string table_rajon_vill = "";
			if (finder.nzp_vill > 0 && finder.mode == Constants.act_mode_view)
			{
				table_rajon_vill = ", " + tables.rajon_vill + " rv ";
				where += " and rv.nzp_raj = r.nzp_raj and rv.nzp_vill = " + finder.nzp_vill;
			}

			if (finder.nzp_vill > 0 && finder.mode == Constants.act_mode_edit)
			{
				where += " and r.nzp_raj not in (select rvill.nzp_raj from " + tables.rajon_vill + " rvill where rvill.nzp_vill <> " + finder.nzp_vill + ")";
			}

			IDataReader reader = null, reader2 = null;
			List<Rajon> list = new List<Rajon>();
			try
			{

				#region определить количество записей
				sql = "select count(*) from " + tables.rajon + " r, " + tables.town + " t, " + tables.stat + " s " +
					table_rajon_vill + " where t.nzp_town = r.nzp_town and s.nzp_stat = t.nzp_stat and s.nzp_stat=(select p_value from " + tables.prefer + " where p_name='nzp_stat_rg') " + where;
				object count = ExecScalar(conn_db, sql, out ret, true);
				int recordsTotalCount;
				try { recordsTotalCount = Convert.ToInt32(count); }
				catch (Exception e)
				{
					ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
					MonitorLog.WriteLog("Ошибка LoadVillRajon " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
					conn_db.Close();
					return null;
				}
				#endregion

				sql = "select trim(t.town) || '/' || trim(r.rajon) as rajon, r.nzp_raj " +
					  "from " + tables.rajon + " r, " + tables.town + " t, " + tables.stat + " s " + table_rajon_vill +
					  " where t.nzp_town = r.nzp_town and s.nzp_stat = t.nzp_stat and s.nzp_stat=(select p_value from " + tables.prefer + " where p_name='nzp_stat_rg') " + where +
					  " order by t.town, r.rajon";

				ret = ExecRead(conn_db, out reader, sql, true);
				if (!ret.result)
				{
					conn_db.Close();
					return null;
				}
				int i = 0;
				while (reader.Read())
				{
					i++;
					if (i <= finder.skip) continue;
					Rajon raj = new Rajon();
					raj.num = i.ToString();
					if (reader["nzp_raj"] != DBNull.Value) raj.nzp_raj = Convert.ToInt32(reader["nzp_raj"]);
					if (reader["rajon"] != DBNull.Value) raj.rajon = Convert.ToString(reader["rajon"]);

					if (finder.mode == Constants.act_mode_edit)
					{
						sql = "select nzp_vill from " + tables.rajon_vill + " where nzp_raj = " + raj.nzp_raj;
						ret = ExecRead(conn_db, out reader2, sql, true);
						if (!ret.result)
						{
							CloseReader(ref reader);
							conn_db.Close();
							return null;
						}
						if (reader2.Read())
						{
							if (reader2["nzp_vill"] != DBNull.Value) raj.nzp_vill = Convert.ToDecimal(reader2["nzp_vill"]);
						}
						reader2.Close();
					}
					else raj.nzp_vill = finder.nzp_vill;

					list.Add(raj);
					if (finder.rows > 0 && list.Count >= finder.rows) break;
				}


				reader.Close();

				ret.tag = recordsTotalCount;
				return list;
			}
			catch (Exception ex)
			{

				CloseReader(ref reader);
				conn_db.Close();

				ret.result = false;
				ret.text = ex.Message;

				string err = "";
				if (Constants.Viewerror) err = " \n " + ex.Message;

				MonitorLog.WriteLog("Ошибка получения LoadVillRajon " + err, MonitorLog.typelog.Error, 20, 201, true);
				return null;
			}
		}


		public List<Rajon> LoadRajon(Rajon finder, out Returns ret)
		{
			ret = Utils.InitReturns();
			if (finder.nzp_user <= 0)
			{
				ret.result = false;
				ret.text = "Не задан пользователь";
				return null;
			}

			string where = String.Empty;



			if (finder.nzp_raj > 0)
			{
				where += " and r.nzp_raj = " + finder.nzp_raj;
			}

			if (finder.nzp_raj > 0)
			{
				where += " and t.nzp_town = " + finder.nzp_town;
			}


			string connectionString = Points.GetConnByPref(finder.pref);
			IDbConnection connDb = GetConnection(connectionString);
			ret = OpenDb(connDb, true);
			if (!ret.result) return null;

			DbTables tables = new DbTables(connDb);

			MyDataReader reader = null;
			var list = new List<Rajon>();
			try
			{


				string sql = " select trim(t.town) || '/' || trim(r.rajon) as rajon, r.nzp_raj " +
						" from " + tables.rajon + " r, " + tables.town + " t,  " + tables.ulica + " u, " + tables.dom + " d " +
						" where t.nzp_town = r.nzp_town  and r.nzp_raj=u.nzp_raj and u.nzp_ul=d.nzp_ul " +
						where +
						" group by 1,2 " +
						" order by 1,2";

				ret = ExecRead(connDb, out reader, sql, true);
				if (!ret.result) return null;
				while (reader.Read())
				{
					var raj = new Rajon();
					if (reader["nzp_raj"] != DBNull.Value) raj.nzp_raj = Convert.ToInt32(reader["nzp_raj"]);
					if (reader["rajon"] != DBNull.Value) raj.rajon = Convert.ToString(reader["rajon"]);
					list.Add(raj);
				}

				return list;
			}
			catch (Exception ex)
			{

				ret.result = false;
				ret.text = ex.Message;
				string err = "";
				if (Constants.Viewerror) err = " \n " + ex.Message;

				MonitorLog.WriteLog("Ошибка получения LoadVillRajon " + err, MonitorLog.typelog.Error, 20, 201, true);
				return null;
			}
			finally
			{
				if (reader != null) reader.Close();
				connDb.Close();
			}
		}

		public Returns SaveVillRajon(Rajon finder, List<Rajon> list_checked)
		{
			Returns ret = Utils.InitReturns();
			if (!(finder.nzp_user > 0))
			{
				ret.text = "Пользователь не задан";
				ret.result = false;
				return ret;
			}

			if (list_checked.Count == 0)
			{
				ret.text = "Нет данных для сохранения";
				ret.result = false;
				return ret;
			}

			if (!(finder.nzp_vill > 0))
			{
				ret.text = "МО не задано";
				ret.result = false;
				return ret;
			}

			string connectionString = Points.GetConnByPref(finder.pref);
			IDbConnection conn_db = GetConnection(connectionString);
			ret = OpenDb(conn_db, true);
			if (!ret.result) return ret;

			string sql = "";

			DbTables tables = new DbTables(conn_db);

			try
			{
				IDbTransaction transaction;
				transaction = conn_db.BeginTransaction();
				string str_nzp_raj = "";
				foreach (Rajon nzp in list_checked)
				{
					if (str_nzp_raj == "") str_nzp_raj += nzp.nzp_raj.ToString();
					else str_nzp_raj += "," + nzp.nzp_raj.ToString();
				}

				sql = "delete from " + tables.rajon_vill +
						" where nzp_vill = " + finder.nzp_vill; //+" and nzp_raj not in ("+str_nzp_raj+")";

				ret = ExecSQL(conn_db, transaction, sql, true);
				if (!ret.result)
				{
					if (transaction != null) transaction.Rollback();
					conn_db.Close();
					return ret;
				}

				foreach (Rajon raj in list_checked)
				{
					sql = "insert into " + tables.rajon_vill + " (nzp_raj, nzp_vill) " +
						  " values (" + raj.nzp_raj + "," + finder.nzp_vill + ")";

					ret = ExecSQL(conn_db, transaction, sql, true);
					if (!ret.result)
					{
						if (transaction != null) transaction.Rollback();
						conn_db.Close();
						return ret;
					}
				}



				transaction.Commit();
				return ret;

			}
			catch (Exception ex)
			{
				conn_db.Close();

				ret.result = false;
				ret.text = ex.Message;

				string err = "";
				if (Constants.Viewerror) err = " \n " + ex.Message;

				MonitorLog.WriteLog("Ошибка SaveVillRajon " + err, MonitorLog.typelog.Error, 20, 201, true);
				return ret;
			}
		}


		/// <summary>
		/// Процедура генерации платежного кода лицевым счетам  (для тех у кого его нет)
		/// </summary>
		/// <returns></returns>
		public Returns GeneratePkodToLs()
		{
			IDbConnection conn_db = DBManager.newDbConnection(Constants.cons_Kernel);
			Returns ret = Utils.InitReturns();
			try
			{
				//Цикл
				foreach (_Point p in Points.PointList)
				{
					//получить список квартир

				}


				return ret = Utils.InitReturns();
			}
			catch (Exception)
			{
				return ret = Utils.InitReturns();
			}
			finally
			{
				conn_db.Close();
			}

		}

		/// <summary>
		/// Данные для выписки из лицевого счета по поданным показаниям квартирных приборов учета
		/// </summary>
		/// <returns></returns>
		public DataTable PrepareLsPuVipiska(Ls finder, out Returns ret)
		{
			ret = Utils.InitReturns();

			#region проверка значений
			//-----------------------------------------------------------------------
			// проверка наличия пользователя
			if (finder.nzp_user < 1)
			{
				ret = new Returns(false, "Не определен пользователь", -1);
				return null;
			}

			// проверка улицы
			if (finder.nzp_kvar <= 0)
			{
				ret = new Returns(false, "Не задан лицевой счет", -2);
				return null;
			}

			// проверка улицы
			if (finder.pref.Trim() == "")
			{
				ret = new Returns(false, "Не задан префикс", -3);
				return null;
			}
			//-----------------------------------------------------------------------
			#endregion

			string _where = " and cs.nzp = " + finder.nzp_kvar;

			#region собрать условие
			//------------------------------------------------------------------------------------------------------------------------------------------------------------------            
			// роли
			if (finder.RolesVal != null)
			{
				foreach (_RolesVal role in finder.RolesVal)
					if (role.tip == Constants.role_sql)
						switch (role.kod)
						{
							case Constants.role_sql_serv:
								_where += " and cc.nzp_serv in (" + role.val + ")";
								break;
						}
			}
			//------------------------------------------------------------------------------------------------------------------------------------------------------------------                   
			#endregion

			string conn_kernel = Points.GetConnByPref(Points.Pref);
			IDbConnection conn_db = GetConnection(conn_kernel);

			ret = OpenDb(conn_db, true);
			if (!ret.result)
			{
				return null;
			}

			DataTable table = new DataTable();
			table.TableName = "Q_master";
			table.Columns.Add("service", typeof(string));

			table.Columns.Add("dat_uchet", typeof(string));
			table.Columns.Add("dat_uchet_po", typeof(string));

			table.Columns.Add("num", typeof(string));
			table.Columns.Add("dat_s", typeof(string));

			table.Columns.Add("dat_close", typeof(string));
			table.Columns.Add("val_cnt_s", typeof(decimal));
			table.Columns.Add("mmnog", typeof(decimal));
			table.Columns.Add("val_cnt", typeof(decimal));
			table.Columns.Add("rashod", typeof(string));
			table.Columns.Add("rashod_d", typeof(decimal));

			IDataReader reader;

			CounterVal cv;
			List<CounterVal> listVal = new List<CounterVal>();

			try
			{
				#region
				//----------------------------------------------------------------------------              
#if PG
#warning Вероятна ошибка для постгре, пока не знаю как отловить этот запрос
				string sql = " Select s.service, v.dat_uchet, " +
					" cs.dat_close, t.mmnog, t.cnt_stage, v.val_cnt, " +
					" p.val_cnt as val_cnt_pred, p.dat_uchet as dat_uchet_pred, " +
					" min(f.dat_uchet) as dat_s, min(f.val_cnt) as val_cnt_s " +
					" From " +
						finder.pref + "_data.counters_spis cs," +
						finder.pref + "_kernel.s_counts cc, " +
						finder.pref + "_kernel.s_counttypes t, " +
						finder.pref + "_kernel.s_measure m, " +
						finder.pref + "_kernel.services s, " +
						finder.pref + "_data.counters v " +
						" left outer join " + finder.pref + "_data.counters p on (p.nzp_counter = v.nzp_counter) " +
						" left outer join " + finder.pref + "_data.counters f on (f.nzp_counter = v.nzp_counter) " +
					" Where cs.nzp_cnttype = t.nzp_cnttype " +
						" and cs.nzp_serv = s.nzp_serv " +
						" and cs.nzp_serv = cc.nzp_serv " +
						" and cc.nzp_measure = m.nzp_measure " +
						" and cs.nzp_type = 3 " +
						" and cs.nzp_counter = v.nzp_counter " +
						" and cs.is_actual <> 100 " +
						" and v.is_actual <> 100 and v.val_cnt is not null " +
						" and p.is_actual <> 100 and p.val_cnt is not null " +
						" and p.dat_uchet = (select max(dat_uchet) from " + finder.pref + "_data.counters b where b.nzp_counter = cs.nzp_counter and b.val_cnt is not null and b.is_actual <> 100 and b.dat_uchet < v.dat_uchet) " +
						" and f.is_actual <> 100 and f.val_cnt is not null " +
						_where +
					" group by 1,2,3,4,5,6,7,8 " +
					" Order by 1,2,10";
#else
				string sql = " Select s.service, v.dat_uchet, " +
					" cs.dat_close, t.mmnog, t.cnt_stage, v.val_cnt, " +
					" p.val_cnt as val_cnt_pred, p.dat_uchet as dat_uchet_pred, " +
					" min(f.dat_uchet) as dat_s, min(f.val_cnt) as val_cnt_s " +
					" From " +
						finder.pref + "_data:counters_spis cs," +
						finder.pref + "_kernel:s_counts cc, " +
						finder.pref + "_kernel:s_counttypes t, " +
						finder.pref + "_kernel:s_measure m, " +
						finder.pref + "_kernel:services s, " +
						finder.pref + "_data:counters v " +
						" left outer join " + finder.pref + "_data:counters p on (p.nzp_counter = v.nzp_counter) " +
						" left outer join " + finder.pref + "_data:counters f on (f.nzp_counter = v.nzp_counter) " +
					" Where cs.nzp_cnttype = t.nzp_cnttype " +
						" and cs.nzp_serv = s.nzp_serv " +
						" and cs.nzp_serv = cc.nzp_serv " +
						" and cc.nzp_measure = m.nzp_measure " +
						" and cs.nzp_type = 3 " +
						" and cs.nzp_counter = v.nzp_counter " +
						" and cs.is_actual <> 100 " +
						" and v.is_actual <> 100 and v.val_cnt is not null " +
						" and p.is_actual <> 100 and p.val_cnt is not null " +
						" and p.dat_uchet = (select max(dat_uchet) from " + finder.pref + "_data:counters b where b.nzp_counter = cs.nzp_counter and b.val_cnt is not null and b.is_actual <> 100 and b.dat_uchet < v.dat_uchet) " +
						" and f.is_actual <> 100 and f.val_cnt is not null " +
						_where +
					" group by 1,2,3,4,5,6,7,8 " +
					" Order by 1,2,10";
#endif


				ret = ExecRead(conn_db, out reader, sql, true);
				if (!ret.result) throw new Exception(ret.text);

				while (reader.Read())
				{
					cv = new CounterVal();
					if (reader["service"] != DBNull.Value) cv.service = Convert.ToString(reader["service"]).Trim();
					if (reader["dat_uchet"] != DBNull.Value) cv.dat_uchet = Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString();

					cv.dat_uchet_po = cv.dat_uchet;

					if (reader["dat_s"] != DBNull.Value) cv.dat_s = Convert.ToDateTime(reader["dat_s"]).ToShortDateString();
					if (reader["dat_close"] != DBNull.Value) cv.dat_close = Convert.ToDateTime(reader["dat_close"]).ToShortDateString();
					if (reader["val_cnt_s"] != DBNull.Value) cv.val_cnt_s = Convert.ToString(reader["val_cnt_s"]).Trim();
					if (reader["mmnog"] != DBNull.Value) cv.mmnog = Convert.ToDecimal(reader["mmnog"]);
					if (reader["val_cnt"] != DBNull.Value) cv.val_cnt = Convert.ToDecimal(reader["val_cnt"]);
					if (reader["val_cnt_pred"] != DBNull.Value) cv.val_cnt_pred = Convert.ToDecimal(reader["val_cnt_pred"]);
					if (reader["dat_uchet_pred"] != DBNull.Value) cv.dat_uchet_pred = Convert.ToString(reader["dat_uchet_pred"]);
					cv.rashod_d = cv.calculatedRashod;

					listVal.Add(cv);
				}

				if (listVal.Count > 0)
				{
					#region привести список к виду, который требуется в отчете
					//-----------------------------------------------------------------
					string dat_uchet = listVal[0].dat_uchet;

					int i = 0;
					int cnt;
					double totalRashod;

					while (i < listVal.Count)
					{
						int j = i + 1;
						listVal[i].num = 1.ToString("00");
						cnt = 1;
						totalRashod = listVal[i].rashod_d;

						if (j >= listVal.Count) break;

						while (listVal[i].dat_uchet == listVal[j].dat_uchet)
						{
							listVal[j].dat_uchet = "";
							cnt += 1;
							listVal[j].num = cnt.ToString("00");
							totalRashod += listVal[j].rashod_d;
							listVal[j].rashod = "";
						}

						listVal[i].rashod = totalRashod.ToString("n").Replace(".", ",");

						i += cnt;
					}
					//-----------------------------------------------------------------
					#endregion

					for (i = 0; i < listVal.Count; i++)
					{
						if (listVal[i].dat_close.Trim() == "") listVal[i].dat_close = "  /  /    ";

						table.Rows.Add(
							listVal[i].service,
							listVal[i].dat_uchet.Replace(".", "/"),
							listVal[i].dat_uchet_po,
							listVal[i].num,
							listVal[i].dat_s.Replace(".", "/"),
							listVal[i].dat_close.Replace(".", "/"),
							Convert.ToDecimal(listVal[i].val_cnt_s),
							listVal[i].mmnog,
							Convert.ToDecimal(listVal[i].val_cnt),
							listVal[i].rashod,
							listVal[i].rashod_d
						);
					}

				}

				reader.Close();
				reader = null;
				//----------------------------------------------------------------------------               
				#endregion

			}
			catch (Exception ex)
			{
				ret = new Returns(false, ex.Message);
				conn_db.Close();
				listVal.Clear();

				return null;
			}

			conn_db.Close();
			listVal.Clear();

			return table;
		}


		public int Management_companies(string pref, StreamWriter writer, IDbConnection conn, bool flag, int year, int month)
		{
			string sqlString = "";
			string month_new = "";
			string year_new = "";
			if (month == 12)
			{
				month_new = "01";
				year_new = (++year).ToString();
			}
			else
			{
				month_new = month.ToString();
				year_new = year.ToString();
			}

			if (!ExecSQL(conn,
		   " drop table t_temp;", false).result) { }

			sqlString = "create temp table t_temp( " +
						"nzp_area integer, " +
						"area char(25), " +
						" y_adr char(100), " +
					   " fact_adr char(100), " +
					   "inn char(20), " +
#if PG
						" kpp char(20)) UNLOGGED";
#else
 " kpp char(20)) with no log";
#endif

			ClassDBUtils.OpenSQL(sqlString, conn);

#if PG
			sqlString = "insert into t_temp (nzp_area,area) select nzp_area,area from " + pref + "_data.s_area a";
			ClassDBUtils.OpenSQL(sqlString, conn);
			sqlString = "update t_temp set y_adr=(select max(replace(val_prm, ',', '.')) from " + pref + "_data.prm_7 p where nzp_prm=296 and t_temp.nzp_area=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
			ClassDBUtils.OpenSQL(sqlString, conn);
			sqlString = "update t_temp set fact_adr=(select max(replace(val_prm, ',', '.')) from " + pref + "_data.prm_7 p where nzp_prm=296 and t_temp.nzp_area=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
			ClassDBUtils.OpenSQL(sqlString, conn);
			sqlString = "update t_temp set inn=(select max(replace(val_prm, ',', '.')) from " + pref + "_data.prm_7 p where nzp_prm=876 and t_temp.nzp_area=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
			ClassDBUtils.OpenSQL(sqlString, conn);
			sqlString = "update t_temp set kpp=(select max(replace(val_prm, ',', '.')) from " + pref + "_data.prm_7 p where nzp_prm=877 and t_temp.nzp_area=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
			ClassDBUtils.OpenSQL(sqlString, conn);
#else
			sqlString = "insert into t_temp (nzp_area,area) select nzp_area,area from " + pref + "_data:s_area a";
			ClassDBUtils.OpenSQL(sqlString, conn);
			sqlString = "update t_temp set y_adr=(select max(replace(val_prm, ',', '.')) from " + pref + "_data:prm_7 p where nzp_prm=296 and t_temp.nzp_area=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
			ClassDBUtils.OpenSQL(sqlString, conn);
			sqlString = "update t_temp set fact_adr=(select max(replace(val_prm, ',', '.')) from " + pref + "_data:prm_7 p where nzp_prm=296 and t_temp.nzp_area=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
			ClassDBUtils.OpenSQL(sqlString, conn);
			sqlString = "update t_temp set inn=(select max(replace(val_prm, ',', '.')) from " + pref + "_data:prm_7 p where nzp_prm=876 and t_temp.nzp_area=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
			ClassDBUtils.OpenSQL(sqlString, conn);
			sqlString = "update t_temp set kpp=(select max(replace(val_prm, ',', '.')) from " + pref + "_data:prm_7 p where nzp_prm=877 and t_temp.nzp_area=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
			ClassDBUtils.OpenSQL(sqlString, conn);
#endif

			sqlString = "select * from t_temp order by area";
			IntfResultTableType dt = ClassDBUtils.OpenSQL(sqlString, conn);

			if (!flag)
			{
				for (int i = 0; i < dt.resultData.Rows.Count; i++)
				{
					writer.Write("2|");
					writer.Write((dt.resultData.Rows[i]["nzp_area"]).ToString().Trim() + "|");
					writer.Write((dt.resultData.Rows[i]["area"]).ToString().Trim() + "|");
					writer.Write((dt.resultData.Rows[i]["y_adr"]).ToString().Trim() + "|");
					writer.Write((dt.resultData.Rows[i]["fact_adr"]).ToString().Trim() + "|");
					writer.Write((dt.resultData.Rows[i]["inn"]).ToString().Trim() + "|");
					writer.Write((dt.resultData.Rows[i]["kpp"]).ToString().Trim() + "|||||");
					writer.WriteLine();
				}
			}
			else
			{
				for (int i = 0; i < dt.resultData.Rows.Count; i++)
				{
					writer.Write("2|");
					writer.Write((dt.resultData.Rows[i]["nzp_area"]).ToString().Trim() + "|");
					writer.Write(("Территория " + dt.resultData.Rows[i]["nzp_area"]).ToString().Trim() + "|");
					writer.Write((dt.resultData.Rows[i]["y_adr"]).ToString().Trim() + "|");
					writer.Write((dt.resultData.Rows[i]["fact_adr"]).ToString().Trim() + "|");
					writer.Write((dt.resultData.Rows[i]["inn"]).ToString().Trim() + "|");
					writer.Write((dt.resultData.Rows[i]["kpp"]).ToString().Trim() + "|||||");
					writer.WriteLine();
				}
			}
			sqlString = "drop table t_temp";
			ClassDBUtils.OpenSQL(sqlString, conn);
			return dt.resultData.Rows.Count;
		}

		public int Homes(List<string> pref, StreamWriter writer, IDbConnection conn, bool flag, int year, int month)
		{
			string sqlString = "";
			string month_new = "";
			string year_new = "";
			if (month == 12)
			{
				month_new = "01";
				year_new = (++year).ToString();
			}
			else
			{
				month_new = month.ToString();
				year_new = year.ToString();
			}
			if (!ExecSQL(conn,
		   " drop table t_temp;", false).result) { }

			IDataReader reader = null;
#if PG
			sqlString = "create temp table t_temp( ykds integer, nzp_dom char(20),nzp_town integer,nzp_raj integer,nzp_ul integer, town char(30), rajon char(30), ulica char(40), " +
	   " ndom char(10), nkor char(3), nzp_area integer, etaj integer, date_postr date, obch_ploch numeric, mest_obch_pol numeric, " +
	   " polezn_ploch numeric, kol_ls integer, kol_str integer ) UNLOGGED;";
#else
			sqlString = "create temp table t_temp( ykds integer, nzp_dom char(20),nzp_town integer,nzp_raj integer,nzp_ul integer, town char(30), rajon char(30), ulica char(40), " +
	   " ndom char(10), nkor char(3), nzp_area integer, etaj integer, date_postr date, obch_ploch decimal, mest_obch_pol decimal, " +
	   " polezn_ploch decimal, kol_ls integer, kol_str integer ) with no log;";
#endif

			ClassDBUtils.OpenSQL(sqlString, conn);

			foreach (var p in pref)
			{
#if PG
				sqlString = "insert into t_temp (nzp_dom,nzp_town,town,nzp_raj,rajon,nzp_ul,ulica,ndom,nzp_area,nkor) select d.nzp_dom,t.nzp_town,t.town,r.nzp_raj,r.rajon,u.nzp_ul,u.ulica,d.ndom,d.nzp_area,d.nkor " +
				"from " + p + "_data.s_area a," + p + "_data.s_town t, " + p + "_data.s_rajon r," + p + "_data.s_ulica u, " + p + "_data.dom d " +
				"where a.nzp_area=d.nzp_area and t.nzp_town=r.nzp_town and r.nzp_raj=u.nzp_raj and d.nzp_ul  = u.nzp_ul ";
#else
				sqlString = "insert into t_temp (nzp_dom,nzp_town,town,nzp_raj,rajon,nzp_ul,ulica,ndom,nzp_area,nkor) select d.nzp_dom,t.nzp_town,t.town,r.nzp_raj,r.rajon,u.nzp_ul,u.ulica,d.ndom,d.nzp_area,d.nkor " +
				"from " + p + "_data:s_area a," + p + "_data:s_town t, " + p + "_data:s_rajon r," + p + "_data:s_ulica u, " + p + "_data:dom d " +
				"where a.nzp_area=d.nzp_area and t.nzp_town=r.nzp_town and r.nzp_raj=u.nzp_raj and d.nzp_ul  = u.nzp_ul ";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}
			foreach (var p in pref)
			{
#if PG
				sqlString = "update t_temp set ykds=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_4 p where nzp_prm=890 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
				sqlString = "update t_temp set ykds=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_4 p where nzp_prm=890 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}
			foreach (var p in pref)
			{
#if PG
				sqlString = "update t_temp set etaj=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_2 p where nzp_prm=37 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
				sqlString = "update t_temp set etaj=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_2 p where nzp_prm=37 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}
			foreach (var p in pref)
			{
#if PG
				sqlString = "update t_temp set date_postr=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_2 p where nzp_prm=150 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
				sqlString = "update t_temp set date_postr=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_2 p where nzp_prm=150 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}
			foreach (var p in pref)
			{
#if PG
				sqlString = "update t_temp set obch_ploch=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_2 p where nzp_prm=40 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
				sqlString = "update t_temp set obch_ploch=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_2 p where nzp_prm=40 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}
			foreach (var p in pref)
			{
#if PG
				sqlString = "update t_temp set mest_obch_pol=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_2 p where nzp_prm=2049 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
				sqlString = "update t_temp set mest_obch_pol=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_2 p where nzp_prm=2049 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}
			foreach (var p in pref)
			{
#if PG
				sqlString = "update t_temp set polezn_ploch=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_2 p where nzp_prm=36 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
				sqlString = "update t_temp set polezn_ploch=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_2 p where nzp_prm=36 and t_temp.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}

#if PG
			sqlString = "update t_temp set kol_ls=(select count(*) from temp_ls l where t_temp.nzp_dom=l.nzp_dom)";
#else
			sqlString = "update t_temp set kol_ls=(select count(*) from temp_ls l where t_temp.nzp_dom=l.nzp_dom)";
#endif

			ClassDBUtils.OpenSQL(sqlString, conn);

#if PG
			sqlString = "update t_temp set kol_str=(select count(*) from temp_counters l where t_temp.nzp_dom=l.nzp_dom)";
#else
			sqlString = "update t_temp set kol_str=(select count(*) from temp_counters l where t_temp.nzp_dom=l.nzp_dom)";
#endif

			ClassDBUtils.OpenSQL(sqlString, conn);

			sqlString = "select * from t_temp order by town, rajon, ulica, ndom";
			int i = 0;

			if (!ExecRead(conn, out reader, sqlString, true).result)
			{
				conn.Close();
				return 0;
			}
			try
			{
				if (reader != null)
				{

					while (reader.Read())
					{
						string str = "3|" +
						(reader["ykds"] != DBNull.Value ? ((int)reader["ykds"]) + "|" : "|") +
						(reader["nzp_dom"] != DBNull.Value ? ((string)reader["nzp_dom"]).ToString().Trim() + "|" : "|") +
						(flag != true ? (reader["town"] != DBNull.Value ? ((string)reader["town"]).ToString().Trim() + "|" : "|") : reader["nzp_town"] != DBNull.Value ? "Город " + ((int)reader["nzp_town"]) + "|" : "|") +
						(flag != true ? (reader["rajon"] != DBNull.Value ? ((string)reader["rajon"]).ToString().Trim() + "|" : "|") : reader["nzp_raj"] != DBNull.Value ? "Район " + ((int)reader["nzp_raj"]) + "|" : "|") +
						(flag != true ? (reader["ulica"] != DBNull.Value ? ((string)reader["ulica"]).ToString().Trim() + "|" : "|") : reader["nzp_ul"] != DBNull.Value ? "Улица " + ((int)reader["nzp_ul"]) + "|" : "|") +
						(reader["ndom"] != DBNull.Value ? ((string)reader["ndom"]).ToString().Trim() + "|" : "|") +
						(reader["nkor"] != DBNull.Value ? ((string)reader["nkor"]).ToString().Trim() + "|" : "|") +
						(reader["nzp_area"] != DBNull.Value ? ((int)reader["nzp_area"]) + "|" : "|") +
						(reader["etaj"] != DBNull.Value ? ((int)reader["etaj"]) + "|" : "|") +
						(reader["date_postr"] != DBNull.Value ? ((DateTime)reader["date_postr"]).ToString("dd.MM.yyyy") + "|" : "|") +
						(reader["obch_ploch"] != DBNull.Value ? ((Decimal)reader["obch_ploch"]).ToString("0.00").Trim() + "|" : "|") +
						(reader["mest_obch_pol"] != DBNull.Value ? ((Decimal)reader["mest_obch_pol"]).ToString("0.00").Trim() + "|" : "|") +
						(reader["polezn_ploch"] != DBNull.Value ? ((Decimal)reader["polezn_ploch"]).ToString("0.00").Trim() + "||" : "||") +
						(reader["kol_ls"] != DBNull.Value ? ((int)reader["kol_ls"]) + "|" : "|") +
						(reader["kol_str"] != DBNull.Value ? ((int)reader["kol_str"]) + "|" : "|");
						writer.WriteLine(str);
						i++;
					}

				}
			}
			catch (Exception ex)
			{
				MonitorLog.WriteLog("Ошибка при записи данных  " + ex.Message, MonitorLog.typelog.Error, true);
				reader.Close();
				return 0;
			}

			writer.Flush();
			return i;
		}

		public StreamWriter LsUpload(List<string> pref, StreamWriter writer, IDbConnection conn, bool is_il, int year, int month)
		{

			string sqlString = "";
			string month_new = "";
			string year_new = "";
			if (month == 12)
			{
				month_new = "01";
				year_new = (++year).ToString();
			}
			else
			{
				month_new = month.ToString();
				year_new = year.ToString();
			}
			if (!ExecSQL(conn,
			" drop table temp_ls;", false).result) { }

#if PG
			sqlString = "  create temp table temp_ls(ukas integer, nzp_dom integer, num_ls char(20), typek integer, " +
			  " fio char(100), nkvar char(10), nkvar_n char(3),date_ls_open date,date_ls_close date, nzp_kvar integer, kol_prib integer, " +
			  " kol_vr_prib integer, kol_vr_ubiv integer,kol_kom integer,obch_ploch numeric, gil_ploch numeric,otapl_ploch numeric, " +
			  " kom_kvar integer,  nal_el_plit integer, nal_gaz_plit integer,  nal_gaz_kol integer,nal_ognev_plit integer,kod_tip_gil integer, " +
			  " kod_tip_gil_otopl integer,kod_tip_gil_kan integer, nal_zab integer,kol_uslyga integer,kol_perer integer, kol_ind_prib_ucheta integer " +
			  " ) ";
#else
			sqlString = "  create temp table temp_ls(ukas integer, nzp_dom integer, num_ls char(20), typek integer, " +
			  " fio char(100), nkvar char(10), nkvar_n char(3),date_ls_open date,date_ls_close date, nzp_kvar integer, kol_prib integer, " +
			  " kol_vr_prib integer, kol_vr_ubiv integer,kol_kom integer,obch_ploch decimal, gil_ploch decimal,otapl_ploch decimal, " +
			  " kom_kvar integer,  nal_el_plit integer, nal_gaz_plit integer,  nal_gaz_kol integer,nal_ognev_plit integer,kod_tip_gil integer, " +
			  " kod_tip_gil_otopl integer,kod_tip_gil_kan integer, nal_zab integer,kol_uslyga integer,kol_perer integer, kol_ind_prib_ucheta integer " +
			  " )with no log ";
#endif

			ClassDBUtils.OpenSQL(sqlString, conn);

			foreach (var p in pref)
			{
#if PG
				sqlString = "   insert into temp_ls(nzp_dom,num_ls,typek, fio, nkvar, nkvar_n, nzp_kvar, kol_prib)  select k.nzp_dom,k.num_ls,( case when k.typek=3 then 2 when k.typek<>3 then 1 end ),k.fio,k.nkvar,k.nkvar_n,k.nzp_kvar," + p + "_data.get_kol_gil(mdy(" + month + ",1," + year + "),mdy(" + month_new + ",1," + year_new + "),15,k.nzp_kvar,0) from " + p + "_data.kvar k  ";
#else
				sqlString = "   insert into temp_ls(nzp_dom,num_ls,typek, fio, nkvar, nkvar_n, nzp_kvar, kol_prib)  select k.nzp_dom,k.num_ls,( case when k.typek=3 then 2 when k.typek<>3 then 1 end ),k.fio,k.nkvar,k.nkvar_n,k.nzp_kvar," + p + "_data:get_kol_gil(mdy(" + month + ",1," + year + "),mdy(" + month_new + ",1," + year_new + "),15,k.nzp_kvar,0) from " + p + "_data:kvar k  ";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}
			foreach (var p in pref)
			{
#if PG
				sqlString = "update temp_ls set ukas=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_15 p where nzp_prm=162" +
							" and temp_ls.nzp_kvar=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
				sqlString = "update temp_ls set ukas=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_15 p where nzp_prm=162" +
							" and temp_ls.nzp_kvar=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
				ClassDBUtils.OpenSQL(sqlString, conn);
			}
			foreach (var p in pref)
			{
#if PG
				sqlString = "update temp_ls set date_ls_open=(select min(dat_s) from " + p + "_data.prm_3 p where nzp_prm=51 and temp_ls.nzp_kvar=p.nzp and p.is_actual<>100 " +
							"and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + ") and val_prm='1')";
#else
				sqlString = "update temp_ls set date_ls_open=(select min(dat_s) from " + p + "_data:prm_3 p where nzp_prm=51 and temp_ls.nzp_kvar=p.nzp and p.is_actual<>100 " +
							"and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + ") and val_prm='1')";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}
			foreach (var p in pref)
			{
#if PG
				sqlString = "update temp_ls set date_ls_close=(select max(dat_s) from " + p + "_data.prm_3 p where nzp_prm=51 and" +
							" temp_ls.nzp_kvar=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + ") and val_prm='2')";
#else
				sqlString = "update temp_ls set date_ls_close=(select max(dat_s) from " + p + "_data:prm_3 p where nzp_prm=51 and" +
							" temp_ls.nzp_kvar=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + ") and val_prm='2')";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}
			foreach (var p in pref)
			{
#if PG
				sqlString = "update temp_ls set kol_vr_prib=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_1 p" +
							" where nzp_prm=131 and temp_ls.nzp_kvar=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
				sqlString = "update temp_ls set kol_vr_prib=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_1 p" +
							" where nzp_prm=131 and temp_ls.nzp_kvar=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}
			foreach (var p in pref)
			{
#if PG
				sqlString = "  update temp_ls set kol_vr_ubiv= (select count( distinct nzp_gilec) from " + p + "_data.gil_periods p" +
							" where temp_ls.nzp_kvar=p.nzp_kvar  and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
				sqlString = "  update temp_ls set kol_vr_ubiv= (select count( unique nzp_gilec) from " + p + "_data:gil_periods p" +
							" where temp_ls.nzp_kvar=p.nzp_kvar  and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}
			foreach (var p in pref)
			{
#if PG
				sqlString = "update temp_ls set kol_kom=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_1 p" +
							" where nzp_prm=107 and temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
				sqlString = "update temp_ls set kol_kom=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_1 p" +
							" where nzp_prm=107 and temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}
			foreach (var p in pref)
			{
#if PG
				sqlString = "update temp_ls set obch_ploch=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_1 p where" +
							" nzp_prm=4 and temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
				sqlString = "update temp_ls set obch_ploch=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_1 p where" +
							" nzp_prm=4 and temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}
			foreach (var p in pref)
			{
#if PG
				sqlString = "update temp_ls set gil_ploch=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_1 p where nzp_prm=6 and " +
							"temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
				sqlString = "update temp_ls set gil_ploch=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_1 p where nzp_prm=6 and " +
							"temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}

			foreach (var p in pref)
			{
#if PG
				sqlString = "update temp_ls set otapl_ploch=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_1 p" +
							" where nzp_prm=133 and temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
				sqlString = "update temp_ls set otapl_ploch=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_1 p" +
							" where nzp_prm=133 and temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}

			sqlString = "update temp_ls set kom_kvar = 0";
			ClassDBUtils.OpenSQL(sqlString, conn);

			foreach (var p in pref)
			{
#if PG
				sqlString = "  update temp_ls set kom_kvar= (select 1 from " + p + "_data.prm_1 p where p.val_prm='2' and p.nzp_prm=3 and p.nzp=temp_ls.nzp_kvar " +
							" and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
				sqlString = "  update temp_ls set kom_kvar= (select 1 from " + p + "_data:prm_1 p where p.val_prm='2' and p.nzp_prm=3 and p.nzp=temp_ls.nzp_kvar " +
							" and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}

			sqlString = "update temp_ls set nal_el_plit = 0";
			ClassDBUtils.OpenSQL(sqlString, conn);

			foreach (var p in pref)
			{
#if PG
				sqlString = "  update temp_ls set nal_el_plit= (select 1 from " + p + "_data.prm_1 p where p.val_prm='1' and p.nzp_prm=19 and" +
							" p.nzp=temp_ls.nzp_kvar  and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
				sqlString = "  update temp_ls set nal_el_plit= (select 1 from " + p + "_data:prm_1 p where p.val_prm='1' and p.nzp_prm=19 and" +
							" p.nzp=temp_ls.nzp_kvar  and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}

			sqlString = "update temp_ls set nal_gaz_plit = 0";
			ClassDBUtils.OpenSQL(sqlString, conn);

			foreach (var p in pref)
			{
#if PG
				sqlString = "  update temp_ls set nal_gaz_plit= (select 1 from " + p + "_data.prm_1 p where p.val_prm='1' and p.nzp_prm=551" +
							" and p.nzp=temp_ls.nzp_kvar  and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
				sqlString = "  update temp_ls set nal_gaz_plit= (select 1 from " + p + "_data:prm_1 p where p.val_prm='1' and p.nzp_prm=551" +
							" and p.nzp=temp_ls.nzp_kvar  and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}

			sqlString = "update temp_ls set nal_gaz_kol = 0";
			ClassDBUtils.OpenSQL(sqlString, conn);

			foreach (var p in pref)
			{
#if PG
				sqlString = "  update temp_ls set nal_gaz_kol= (select 1 from  " + p + "_data.prm_1 p where p.val_prm='1' and p.nzp_prm=1 and p.nzp=temp_ls.nzp_kvar " +
							" and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
				sqlString = "  update temp_ls set nal_gaz_kol= (select 1 from  " + p + "_data:prm_1 p where p.val_prm='1' and p.nzp_prm=1 and p.nzp=temp_ls.nzp_kvar " +
							" and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}

			sqlString = "update temp_ls set nal_ognev_plit = 0";
			ClassDBUtils.OpenSQL(sqlString, conn);

			foreach (var p in pref)
			{
#if PG
				sqlString = "  update temp_ls set nal_ognev_plit= (select 1 from " + p + "_data.prm_1 p where p.val_prm='1' and p.nzp_prm=1172 and p.nzp=temp_ls.nzp_kvar " +
							" and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
				sqlString = "  update temp_ls set nal_ognev_plit= (select 1 from " + p + "_data:prm_1 p where p.val_prm='1' and p.nzp_prm=1172 and p.nzp=temp_ls.nzp_kvar " +
							" and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}

			foreach (var p in pref)
			{
#if PG
				sqlString = "update temp_ls set kod_tip_gil=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_1 p where nzp_prm=7 and temp_ls.nzp_dom=p.nzp" +
							" and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
				sqlString = "update temp_ls set kod_tip_gil=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_1 p where nzp_prm=7 and temp_ls.nzp_dom=p.nzp" +
							" and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}


			foreach (var p in pref)
			{
#if PG
				sqlString = "update temp_ls set kod_tip_gil_otopl=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_1 p where nzp_prm=894 and temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
				sqlString = "update temp_ls set kod_tip_gil_otopl=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_1 p where nzp_prm=894 and temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}

			foreach (var p in pref)
			{
#if PG
				sqlString = "update temp_ls set kod_tip_gil_otopl=(select max(replace(val_prm, ',', '.')) from " + p + "_data.prm_2 p where nzp_prm=38 and temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
				sqlString = "update temp_ls set kod_tip_gil_otopl=(select max(replace(val_prm, ',', '.')) from " + p + "_data:prm_2 p where nzp_prm=38 and temp_ls.nzp_dom=p.nzp and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}

			sqlString = "update temp_ls set  nal_zab = 0";
			ClassDBUtils.OpenSQL(sqlString, conn);

			foreach (var p in pref)
			{
#if PG
				sqlString = "  update temp_ls set nal_zab= (select 1 from " + p + "_data.prm_2 p where p.val_prm='1' and p.nzp_prm=35 and p.nzp=temp_ls.nzp_kvar  and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#else
				sqlString = "  update temp_ls set nal_zab= (select 1 from " + p + "_data:prm_2 p where p.val_prm='1' and p.nzp_prm=35 and p.nzp=temp_ls.nzp_kvar  and p.is_actual<>100 and dat_s<mdy(" + month_new + ",1," + year_new + ") and dat_po>=mdy(" + month + ",1," + year + "))";
#endif

				ClassDBUtils.OpenSQL(sqlString, conn);
			}
			sqlString = "update temp_ls set  kol_uslyga = (select count(*) from tmp_inf_serv s where temp_ls.num_ls=s.num_ls)";
			ClassDBUtils.OpenSQL(sqlString, conn);
			sqlString = "update temp_ls set  kol_perer = (select count(*) from temp_ls_perer s where temp_ls.nzp_kvar=s.nzp_kvar)";
			ClassDBUtils.OpenSQL(sqlString, conn);
			//     sqlString = "update temp_ls set  kol_ind_prib_ucheta = (select count(*) from temp_counters_ind s where temp_ls.num_ls=s.num_ls)";
			//    ClassDBUtils.OpenSQL(sqlString, conn);

			return writer;
		}

		public int WriteLs(StreamWriter writer, IDbConnection conn, bool is_il)
		{

			IDataReader reader = null;
			string sqlString = "";
			sqlString = " select * from temp_ls order by  nzp_dom,fio, nkvar, nkvar_n, nzp_kvar, num_ls";
			int i = 0;
			if (!ExecRead(conn, out reader, sqlString, true).result)
			{
				conn.Close();
				return 0;
			}
			try
			{
				if (reader != null)
				{

					while (reader.Read())
					{
						string[] names = (reader["fio"] != DBNull.Value ? ((string)reader["fio"]).ToString().Trim() : "").Split(' ');

						string first_name = (names.Length == 3 ? names[0] : "");
						string name = (names.Length == 3 ? names[1] : "");
						string second_name = (names.Length == 3 ? names[2] : "");

						string str = "4|" +
				  (reader["ukas"] != DBNull.Value ? ((int)reader["ukas"]) + "|" : "|") +
				  (reader["nzp_dom"] != DBNull.Value ? ((int)reader["nzp_dom"]) + "|" : "|") +
				  (reader["num_ls"] != DBNull.Value ? ((string)reader["num_ls"]).ToString().Trim() + "|" : "|") +
				  (reader["typek"] != DBNull.Value ? ((int)reader["typek"]) + "|" : "|") +
				  (is_il != true ? (first_name + "|" + name + "|" + second_name + "||") : "ФИО " + (reader["num_ls"] != DBNull.Value ? ((string)reader["num_ls"]).ToString().Trim() + "|" : "|")) +
				  (reader["nkvar"] != DBNull.Value ? ((string)reader["nkvar"]).ToString().Trim() + "|" : "|") +
				  (reader["nkvar_n"] != DBNull.Value ? ((string)reader["nkvar_n"]).ToString().Trim() + "|" : "|") +
				  (reader["date_ls_open"] != DBNull.Value ? ((DateTime)reader["date_ls_open"]).ToString("dd.MM.yyyy") + "||" : "||") +
				  (reader["date_ls_close"] != DBNull.Value ? ((DateTime)reader["date_ls_close"]).ToString("dd.MM.yyyy") + "||" : "||") +
				  (reader["nzp_kvar"] != DBNull.Value ? ((int)reader["nzp_kvar"]) + "|" : "|") +
				  (reader["kol_prib"] != DBNull.Value ? ((int)reader["kol_prib"]) + "|" : "|") +
				  (reader["kol_vr_prib"] != DBNull.Value ? ((int)reader["kol_vr_prib"]) + "|" : "|") +
				  (reader["kol_vr_ubiv"] != DBNull.Value ? ((int)reader["kol_vr_ubiv"]) + "|" : "|") +
				  (reader["kol_kom"] != DBNull.Value ? ((int)reader["kol_kom"]) + "|" : "|") +
				  (reader["obch_ploch"] != DBNull.Value ? ((Decimal)reader["obch_ploch"]).ToString("0.00").Trim() + "|" : "|") +
				  (reader["gil_ploch"] != DBNull.Value ? ((Decimal)reader["gil_ploch"]).ToString("0.00").Trim() + "|" : "|") +
				  (reader["otapl_ploch"] != DBNull.Value ? ((Decimal)reader["otapl_ploch"]).ToString("0.00").Trim() + "||" : "||") +
				  (reader["kom_kvar"] != DBNull.Value ? ((int)reader["kom_kvar"]) + "|" : "|") +
				  (reader["nal_el_plit"] != DBNull.Value ? ((int)reader["nal_el_plit"]) + "|" : "|") +
				  (reader["nal_gaz_plit"] != DBNull.Value ? ((int)reader["nal_gaz_plit"]) + "|" : "|") +
				  (reader["nal_gaz_kol"] != DBNull.Value ? ((int)reader["nal_gaz_kol"]) + "|" : "|") +
				  (reader["nal_ognev_plit"] != DBNull.Value ? ((int)reader["nal_ognev_plit"]) + "||" : "||") +
				  (reader["kod_tip_gil"] != DBNull.Value ? ((int)reader["kod_tip_gil"]) + "|" : "|") +
				  (reader["kod_tip_gil_otopl"] != DBNull.Value ? ((int)reader["kod_tip_gil_otopl"]) + "|" : "|") +
				  (reader["kod_tip_gil_kan"] != DBNull.Value ? ((int)reader["kod_tip_gil_kan"]) + "|" : "|") +
				  (reader["nal_zab"] != DBNull.Value ? ((int)reader["nal_zab"]) + "||" : "||") +
				  (reader["kol_uslyga"] != DBNull.Value ? ((int)reader["kol_uslyga"]) + "|" : "|") +
				  (reader["kol_perer"] != DBNull.Value ? ((int)reader["kol_perer"]) + "|" : "|") +
				  (reader["kol_ind_prib_ucheta"] != DBNull.Value ? ((int)reader["kol_ind_prib_ucheta"]) + "|" : "|");

						writer.WriteLine(str);
						i++;
					}

				}
			}
			catch (Exception ex)
			{
				MonitorLog.WriteLog("Ошибка при записи данных  " + ex.Message, MonitorLog.typelog.Error, true);
				reader.Close();
				return 0;
			}

			writer.Flush();

			return i;
		}

		public string Title(string pref, StreamWriter writer, IDbConnection conn, int month, int year, int count)
		{
			string sqlString = "";
#if PG
			sqlString = "select val_prm from " + pref + "_data.prm_10 where nzp_prm=80 and is_actual<>100 and current_date between dat_s and dat_po";
#else
			sqlString = "select val_prm from " + pref + "_data.prm_10 where nzp_prm=80 and is_actual<>100 and today between dat_s and dat_po";
#endif
			IntfResultTableType dt = ClassDBUtils.OpenSQL(sqlString, conn);
			string naim_org = "";
			try
			{
				naim_org = (dt.resultData.Rows[0]["val_prm"]).ToString().Trim();
			}
			catch { }
			string nomer = "1";
			string time = DateTime.Now.ToString("dd.MM.yyyy");

#if PG
			sqlString = "select val_prm from " + pref + "_data.prm_10 where nzp_prm=96 and is_actual<>100 and current_date between dat_s and dat_po";
#else
			sqlString = "select val_prm from " + pref + "_data.prm_10 where nzp_prm=96 and is_actual<>100 and today between dat_s and dat_po";
#endif
			dt = ClassDBUtils.OpenSQL(sqlString, conn);
			string telephone = "";
			try
			{
				telephone = (dt.resultData.Rows[0]["val_prm"]).ToString().Trim();
			}
			catch { }
			string fio = "Иванов Иван Иванович";
			string kol_vo = count.ToString();

			string str = "1|" + naim_org + "||||" + nomer + "|" + time + "|" + telephone + "|" + fio + "|" + month + "." + year.ToString() + "|" + kol_vo;

			return str;
		}

		public Returns UpdateSosLS(Ls finder)
		{
			if (finder.pref == "") return new Returns(false, "Префикс не задан");
			if (finder.nzp_kvar <= 0) return new Returns(false, "ЛС не задан");
			if (finder.nzp_user <= 0) return new Returns(false, "Пользователь не задан");

			Returns ret = Utils.InitReturns();

			string connectionString = Points.GetConnByPref(finder.pref);
			IDbConnection conn_db = GetConnection(connectionString);
			ret = OpenDb(conn_db, true);
			if (!ret.result) return ret;

			ret = RefreshKvar(conn_db, null, finder);
			if (!ret.result)
			{
				ret.text = "Ошибка записи данных лицевого счета в центральный банк";
				conn_db.Close();
				return ret;
			}

			#region соединение conn_web
			IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
			ret = OpenDb(conn_web, true);
			if (!ret.result) return ret;
			#endregion

			#region наименование таблиц tXX_spls/tXX_spls_full
			string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spls";
#if PG
			string tXX_spls_full = pgDefaultDb + "." + tXX_spls;
#else
			string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;
#endif

			#endregion

			if (!TempTableInWebCashe(conn_web, tXX_spls_full))
			{
				conn_web.Close();
				conn_db.Close();
				return ret;
			}
			string sostls = finder.stateID == Ls.States.Open.GetHashCode() ? "открыт" : "закрыт";

			string sql = " update " + tXX_spls_full + " set sostls = " + Utils.EStrNull(sostls) +
						 " where nzp_kvar = " + finder.nzp_kvar + " and pref = " + Utils.EStrNull(finder.pref);
			ret = ExecSQL(conn_db, sql, true);
			if (!ret.result)
			{
				ret.text = "Ошибка записи состояния лицевого счета в кэш таблицу";
				return ret;
			}

			return ret;
		}




		/// <summary>
		/// Выгрузка сальдо в банк
		/// </summary>
		/// <returns></returns>
		/// 

		public DataTable PrepareGubCurrCharge(Charge finder, int reportId, out Returns ret)
		{
			ret = Utils.InitReturns();

			#region проверка значений
			//-----------------------------------------------------------------------
			// проверка наличия пользователя
			if (finder.nzp_user < 1)
			{
				ret = new Returns(false, "Не определен пользователь", -1);
				return null;
			}

			if (finder.dat_calc.Length == 0)
			{
				ret = new Returns(false, "Не задан расчетный месяц", -1);
				return null;
			}

			DateTime calc_month;

			try
			{
				calc_month = Convert.ToDateTime(finder.dat_calc);
			}
			catch
			{
				ret = new Returns(false, "Неверный формат даты расчетного месяца", -1);
				return null;
			}
			//-----------------------------------------------------------------------
			#endregion

			string _where_kvar = "";
			string _where_serv = "";
			string _where_wp = "";

			#region собрать условие
			//------------------------------------------------------------------------------------------------------------------------------------------------------------------            
			// роли
			if (finder.RolesVal != null)
			{
				foreach (_RolesVal role in finder.RolesVal)
					if (role.tip == Constants.role_sql)
						switch (role.kod)
						{
							case Constants.role_sql_serv:
								_where_serv += " and cc.nzp_serv in (" + role.val + ")";
								break;
							case Constants.role_sql_area:
								_where_kvar += " and k.nzp_area in (" + role.val + ")";
								break;
							case Constants.role_sql_geu:
								_where_kvar += " and k.nzp_geu in (" + role.val + ")";
								break;
							case Constants.role_sql_wp:
								_where_wp += " and k.nzp_wp in (" + role.val + ")";
								break;
						}
			}

			if (finder.nzp_ul > 0) _where_kvar += " and d.nzp_ul = " + finder.nzp_ul;
			if (finder.nzp_dom > 0) _where_kvar += " and k.nzp_dom = " + finder.nzp_dom;
			if (finder.nzp_kvar > 0) _where_kvar += " and k.nzp_kvar = " + finder.nzp_kvar;
			if (finder.nzp_area > 0) _where_kvar += " and k.nzp_area = " + finder.nzp_area;
			//------------------------------------------------------------------------------------------------------------------------------------------------------------------                   
			#endregion

			string conn_kernel = Points.GetConnByPref(Points.Pref);
			IDbConnection conn_db = GetConnection(conn_kernel);

			ret = OpenDb(conn_db, true);
			if (!ret.result)
			{
				return null;
			}

			IDataReader reader;
			List<string> prefList = new List<string>();

			try
			{
				// получить список префиксов
#if PG
				string sql = " Select distinct k.pref from " + Points.Pref + "_data.kvar k, " + Points.Pref + "_data.dom d Where d.nzp_dom = k.nzp_dom " + _where_kvar + _where_wp;
#else
				string sql = " Select distinct k.pref from " + Points.Pref + "_data:kvar k, " + Points.Pref + "_data:dom d Where d.nzp_dom = k.nzp_dom " + _where_kvar + _where_wp;
#endif

				ret = ExecRead(conn_db, out reader, sql, true);
				if (!ret.result) throw new Exception(ret.text);

				while (reader.Read())
				{
					if (reader["pref"] != DBNull.Value) prefList.Add(Convert.ToString(reader["pref"]).Trim());
				}

				DataTable total = null;

				#region
				//----------------------------------------------------------------------------              
				foreach (string cur_pref in prefList)
				{
					string charge = cur_pref + "_charge_" + (calc_month.Year % 100).ToString("00") + ":charge_" + calc_month.Month.ToString("00");

#if PG
					sql = " select a.area, u.ulica, d.idom, d.ndom, s.ordering, s.service_small as service, " +
						(reportId == Constants.act_report_gub_curr_charge ? " sum(ch.sum_charge) as sum_charge " : " sum(ch.sum_money) as sum_charge ") +
						" from " + cur_pref + "_data.s_ulica u, " + cur_pref + "_data.dom d, " + cur_pref + "_data.kvar k, " + cur_pref + "_data.s_area a, " + cur_pref + "_kernel.services s, " + charge + " ch " +
						" where u.nzp_ul = d.nzp_ul " +
							" and d.nzp_dom = k.nzp_dom " +
							" and k.nzp_area = a.nzp_area " +
							" and ch.nzp_kvar = k.nzp_kvar " +
							" and s.nzp_serv = ch.nzp_serv " +
							" and s.nzp_serv > 1 " + _where_kvar + _where_serv +
						" group by 1,2,3,4,5,6 " +
						" order by 1,2,3,4,5,6";
#else
					sql = " select a.area, u.ulica, d.idom, d.ndom, s.ordering, s.service_small as service, " +
						(reportId == Constants.act_report_gub_curr_charge ? " sum(ch.sum_charge) as sum_charge " : " sum(ch.sum_money) as sum_charge ") +
						" from " + cur_pref + "_data:s_ulica u, " + cur_pref + "_data: dom d, " + cur_pref + "_data: kvar k, " + cur_pref + "_data: s_area a, " + cur_pref + "_kernel:services s, " + charge + " ch " +
						" where u.nzp_ul = d.nzp_ul " +
							" and d.nzp_dom = k.nzp_dom " +
							" and k.nzp_area = a.nzp_area " +
							" and ch.nzp_kvar = k.nzp_kvar " +
							" and s.nzp_serv = ch.nzp_serv " +
							" and s.nzp_serv > 1 " + _where_kvar + _where_serv +
						" group by 1,2,3,4,5,6 " +
						" order by 1,2,3,4,5,6";
#endif

					IntfResultTableType dt = ClassDBUtils.OpenSQL(sql, conn_db);

					if (total == null)
					{
						total = dt.GetData().Copy();
					}
					else
					{
						foreach (DataRow dr in dt.GetData().Rows) total.Rows.Add(dr.ItemArray);
					}
				}
				//----------------------------------------------------------------------------               
				#endregion

				DataView dv = new DataView(total);
				dv.Sort = "area asc, ulica asc, idom asc, ndom asc, ordering asc";
				DataTable sortedDt = dv.ToTable();

				DataTable table = sortedDt.DefaultView.ToTable(false, "area", "ulica", "ndom", "service", "sum_charge");
				table.TableName = "Q_master";

				conn_db.Close();
				return table;
			}
			catch (Exception ex)
			{
				ret = new Returns(false, ex.Message);
				conn_db.Close();
				return null;
			}
		}

		public Returns DbUpdateMovedHousesPkod(string connString)
		{
			IDbConnection conn_web = DBManager.newDbConnection(connString);
			IDbTransaction transaction = null;

			Returns ret = OpenDb(conn_web, true);
			if (!ret.result) return ret;

			try
			{
				string sql = "select bd_kernel from s_point where nzp_graj = 0";
				IDbCommand IDbCommand = DBManager.newDbCommand(sql, conn_web, transaction);
				Points.Pref = IDbCommand.ExecuteScalar().ToString().Trim();
				Points.IsSmr = true;
				IDbCommand.Dispose();

				sql = " select bd_kernel from " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "s_point where nzp_graj = 1 ";
				IDbCommand = DBManager.newDbCommand(sql, conn_web);
				var dt = ClassDBUtils.OpenSQL(sql, conn_web, transaction);
				IDbCommand.Dispose();
				var resRows = dt.resultData.Rows;

				MyDataReader reader = null;
				string pref;

				for (int i = 0; i < resRows.Count; i++)
				{
					pref = resRows[i]["bd_kernel"].ToString().Trim();

					// определение платежного кода
					sql = "SELECT d.nzp_kvar_n FROM " + Points.Pref + "_data" + tableDelimiter + "dom_moved d, " + pref + "_data" + tableDelimiter + "kvar k where k.nzp_kvar = d.nzp_kvar_n and d.is_to_move = 1";

					ret = ExecRead(conn_web, transaction, out reader, sql, true);
					if (!ret.result)
					{
						return ret;
					}

					var counter = 0;
					while (reader.Read())
					{
						counter++;
						var nzp_kvar_n = (reader["nzp_kvar_n"] != DBNull.Value) ? Convert.ToInt32(reader["nzp_kvar_n"]) : 0;

						//получить платежный код
						string pkod = GeneratePkod(conn_web, null, new Ls() { nzp_kvar = nzp_kvar_n, pref = pref }, out ret);
						//pkod = GeneratePkodOneLS(new Ls(), transaction,)

						//проапдейтить локальную базу
						sql = "update " + pref + "_data" + tableDelimiter + "kvar set pkod = " + pkod + " where nzp_kvar = " + nzp_kvar_n;
						IDbCommand = DBManager.newDbCommand(sql, conn_web, transaction);
						ClassDBUtils.ExecSQL(sql, conn_web, transaction);
						IDbCommand.Dispose();

						//проапдейтить центральную базу
						sql = "update " + Points.Pref + "_data" + tableDelimiter + "kvar set pkod = " + pkod + " where nzp_kvar = " + nzp_kvar_n;
						IDbCommand = DBManager.newDbCommand(sql, conn_web, transaction);
						ClassDBUtils.ExecSQL(sql, conn_web, transaction);
						IDbCommand.Dispose();
					}
					reader.Close();
				}

			}
			catch (DbException ex)
			{
				ret.text = ex.Message;
				ret.result = false;
				MonitorLog.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message, MonitorLog.typelog.Error, true);
			}
			finally
			{
				conn_web.Close();
			}
			return ret;
		}

		/// <summary>
		/// Генерация платежных кодов(постановка в фоновый процесс)
		/// </summary>
		/// <param name="finder">пользователь</param>
		/// <returns>Результат</returns>
		public Returns GeneratePkodFonAddTask(Finder finder)
		{                         
			CalcFon calcfon = new CalcFon(Points.GetCalcNum(0));
			calcfon.task = FonTaskTypeIds.taskGeneratePkod;
			calcfon.status = FonTaskStatusId.New; //на выполнение                
			calcfon.nzp_user = finder.nzp_user;
			calcfon.txt = "Процедура генерации платежных кодов по выбранному списку лицевых счетов";
			calcfon.nzp_user = finder.nzp_user;
			DbCalc dbCalc = new DbCalc();
			Returns ret = dbCalc.AddTask(calcfon);        
			return ret;
		}

		/// <summary>
		/// Генерация платежных кодов
		/// </summary>
		/// <param name="finder">nzp_user</param>
		/// <returns>результат</returns>
		public Returns GeneratePkodFon(Finder finder)
		{
			Returns ret = Utils.InitReturns();

			DbAdresKernel db = new DbAdresKernel();
			ret =
				db.GeneratePkodOnLsList(new Ls()
				{
					nzp_user = finder.nzp_user                                            
				});
			db.Close();

			return ret;
		}

	}
}