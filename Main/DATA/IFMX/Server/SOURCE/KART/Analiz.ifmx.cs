﻿using System;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;


namespace STCLINE.KP50.DataBase
{
	//----------------------------------------------------------------------
	public class DbAnaliz : DbAnalizClient
	//----------------------------------------------------------------------
	{
#if PG
		private readonly string defaultPgSchema = "public";
#else
#endif
		public void LoadAdres(Finder finder, out Returns ret, int year, bool reload) //загрузить pxx_spdom
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

			string analiz1 = "anl" + year;

			//если analiz1 еще не был создан, то вызовем процедуру заполнения за текущий год
			if (!TableInWebCashe(conn_web, analiz1))
			{
				LoadAnaliz1(out ret, year, true);

				if (!ret.result) 
				{
					conn_web.Close();
					return;
				}
			}

			//string pXX_spdom = "p" + Convert.ToString(finder.nzp_user) + "_spdom";
			string pXX_spdom = "anl" + year + "_dom";

			if (reload)
			{
				ExecSQL(conn_web, " Drop table " + pXX_spdom, false);
			}

			reload = !TableInWebCashe(conn_web, pXX_spdom);

			if (!reload)
			{
				conn_web.Close();
				return;
			}

			CreateAnlDom(conn_web, pXX_spdom, out ret);
			if (!ret.result)
			{
				conn_web.Close();
				return;
			}


#if PG
			string webdata = defaultPgSchema;
			string pXX_spdom_full = webdata + "." + pXX_spdom;
			analiz1 = webdata + "." + analiz1;
#else
string webdata = conn_web.Database + "@" + DBManager.getServer(conn_web);
			string pXX_spdom_full = webdata + ":" + pXX_spdom;
			analiz1 = webdata + ":" + analiz1;
#endif

			//заполнить webdata:pXX_spdom
			if (Points.IsFabric)
			{
				//открываем цикл по серверам БД
				foreach (_Server server in Points.Servers)
				{
					GoPointDom(conn_web, server.nzp_server, webdata, pXX_spdom_full, analiz1, out ret);
					if (!ret.result)
						break;
				}
			}
			else
				GoPointDom(conn_web, 0, webdata, pXX_spdom_full, analiz1, out ret);

#if PG
			if (ret.result) ret = ExecSQL(conn_web, " analyze  " + pXX_spdom, true);
#else
			if (ret.result) ret = ExecSQL(conn_web, " Update statistics for table  " + pXX_spdom, true);
#endif
			conn_web.Close();
		}//LoadAdres

		void GoPointDom(IDbConnection conn_web, int nzp_server, string webdata, string pXX_spdom_full, string analiz1, out Returns ret)
		{
			string conn_kernel = Points.GetConnByServer(nzp_server);
			IDbConnection conn_db = GetConnection(conn_kernel);
			ret = OpenDb(conn_db, true);
			if (!ret.result) return;

			try
			{
				foreach (_Point zap in Points.PointList)
				{
					if (nzp_server > 0)
					{
						if (zap.nzp_server != nzp_server) continue;
					}
					//Наверное, для заполнения анализа нужны все привилегия!!!
					//if (nzp_wp > 0)
					//{
					//    if (zap.nzp_wp != nzp_wp) continue;
					//}
					//if (!Utils.IsInRoleVal(finder.RolesVal, zap.nzp_wp.ToString(), Constants.role_sql, Constants.role_sql_wp)) continue;

					//проверка на доступность банка данных

#if PG
					if (!TempTableInWebCashe(conn_db, zap.pref + "_data.dom"))
					{
						MonitorLog.WriteLog("Первый запуск приложения. Загрузка адресов. Банк данных. " + zap.pref + " не доступен", MonitorLog.typelog.Warn, true);
						continue;
					}
					StringBuilder sql = new StringBuilder();
					sql.Append(" Insert into " + pXX_spdom_full + " (pref,nzp_wp,point, nzp_dom,nzp_ul,nzp_area,nzp_geu,idom, ndom,ulica, area,geu) ");
					sql.Append(" Select distinct '" + zap.pref + "'," + zap.nzp_wp.ToString() + ",'" + zap.point + "', ");
					sql.Append(" z.nzp_dom,d.nzp_ul,z.nzp_area,z.nzp_geu,idom,  ");
					sql.Append(" 'дом ' || trim(coalesce(d.ndom,''))||' корп. '||trim(coalesce(d.nkor,'')) as ndom, ");
					sql.Append(" trim(coalesce(ulicareg,'улица'))||' '||trim(coalesce(u.ulica,''))||' / '||trim(coalesce(r.rajon,'')), area,geu ");
					sql.Append(" From " + analiz1 + " z left outer join " + Points.Pref + "_data.s_area a on (z.nzp_area=a.nzp_area) ");
					sql.Append(" left outer join " + Points.Pref + "_data.s_geu g on (z.nzp_geu=g.nzp_geu), ");
					sql.Append(  zap.pref + "_data.dom d, " + Points.Pref + "_data.s_ulica u ");
					sql.Append(" left outer join " + Points.Pref + "_data.s_rajon r on (u.nzp_raj=r.nzp_raj) ");
					sql.Append(" Where z.nzp_dom = d.nzp_dom and d.nzp_ul=u.nzp_ul ");
					//sql.Append(" and 1 > ( Select count(*) From "+ zap.pref + "_data.dom d1 Where z.nzp_dom = d1.nzp_supp )");
#else
 if (!TempTableInWebCashe(conn_db, zap.pref + "_data:dom"))
					{
						MonitorLog.WriteLog("Первый запуск приложения. Загрузка адресов. Банк данных: " + zap.pref + " не доступен", MonitorLog.typelog.Warn, true);
						continue;
					}
					StringBuilder sql = new StringBuilder();
					sql.Append(" Insert into " + pXX_spdom_full + " (pref,nzp_wp,point, nzp_dom,nzp_ul,nzp_area,nzp_geu,idom, ndom,ulica, area,geu) ");
					sql.Append(" Select unique '" + zap.pref + "'," + zap.nzp_wp.ToString() + ",'" + zap.point + "', ");
					sql.Append(" z.nzp_dom,d.nzp_ul,z.nzp_area,z.nzp_geu,idom,  ");
					sql.Append(" 'дом ' || trim(nvl(d.ndom,''))||' корп. '||trim(nvl(d.nkor,'')) as ndom, ");
					sql.Append(" trim(nvl(ulicareg,'улица'))||' '||trim(nvl(u.ulica,''))||' / '||trim(nvl(r.rajon,'')), area,geu ");
					sql.Append(" From " + analiz1 + " z, " + zap.pref + "_data:dom d, ");
					sql.Append(Points.Pref + "_data:s_ulica u, ");
					sql.Append(" outer " + Points.Pref + "_data:s_rajon r, ");
					sql.Append(" outer " + Points.Pref + "_data:s_area a,  ");
					sql.Append(" outer " + Points.Pref + "_data:s_geu g    ");
					sql.Append(" Where z.nzp_dom = d.nzp_dom and d.nzp_ul=u.nzp_ul and u.nzp_raj=r.nzp_raj and z.nzp_area=a.nzp_area and z.nzp_geu=g.nzp_geu  ");
					//sql.Append(" and 1 > ( Select count(*) From "+ zap.pref + "_data:dom d1 Where z.nzp_dom = d1.nzp_supp )");
#endif

					//записать текст sql в лог-журнал
					//int key = LogSQL(conn_web, finder.nzp_user, sql.ToString());
					//int key = LogSQL(conn_web, finder.nzp_user, pXX_spdom_full + ":" + whereString);

					ret = ExecSQL(conn_db, sql.ToString(), true);
					if (!ret.result) return;
				}
			}
			finally
			{
				conn_db.Close();
			}
		}

		//----------------------------------------------------------------------
		public void LoadSupp(AnlSupp finder, out Returns ret, int year, bool reload) //загрузить pxx_spdom
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

			string analiz1 = "anl" + year;

			//если analiz1 еще не был создан, то вызовем процедуру заполнения за текущий год
			if (!TableInWebCashe(conn_web, analiz1))
			{
				LoadAnaliz1(out ret, year, true);

				if (!ret.result)
				{
					conn_web.Close();
					return;
				}
			}
#if PG
			ret = ExecSQL(conn_web, " analyze  " + analiz1, true);
#else
			ret = ExecSQL(conn_web, " Update statistics for table  " + analiz1, true);
#endif
			//string pXX_spsupp = "p" + Convert.ToString(finder.nzp_user) + "_spsupp";
			string pXX_spsupp = "anl" + year + "_supp";

			if (reload)
			{
				ExecSQL(conn_web, " Drop table " + pXX_spsupp, false);
			}

			reload = !TableInWebCashe(conn_web, pXX_spsupp);

			if (!reload)
			{
				conn_web.Close();
				return;
			}

			//создать anlxx_supp
			CreateAnlSupp(conn_web, pXX_spsupp, out ret);
			if (!ret.result)
			{
				conn_web.Close();
				return;
			}

			//заполнить webdata:pXX_spsupp
#if PG
			string webdata = defaultPgSchema;
			string pXX_spsupp_full = webdata + "." + pXX_spsupp;
			analiz1 = webdata + "." + analiz1;
#else
string webdata = conn_web.Database + "@" + DBManager.getServer(conn_web);
			string pXX_spsupp_full = webdata + ":" + pXX_spsupp;
			analiz1 = webdata + ":" + analiz1;
#endif
			if (Points.IsFabric)
			{
				//открываем цикл по серверам БД
				foreach (_Server server in Points.Servers)
				{
					GoPointSupp(conn_web, server.nzp_server, webdata, pXX_spsupp_full, analiz1, out ret);
					if (!ret.result)
						break;
				}
			}
			else
				GoPointSupp(conn_web, 0, webdata, pXX_spsupp_full, analiz1, out ret);

			if (!ret.result)
				return;

#if PG
			ret = ExecSQL(conn_web, " analyze  " + pXX_spsupp, true);
#else
ret = ExecSQL(conn_web, " Update statistics for table  " + pXX_spsupp, true);
#endif
			conn_web.Close();
			return;
		}//LoadSupp

		//----------------------------------------------------------------------
		void GoPointSupp(IDbConnection conn_web, int nzp_server, string webdata, string pXX_spsupp_full, string analiz1, out Returns ret)
		//----------------------------------------------------------------------
		{
			string conn_kernel = Constants.cons_Kernel;
			if (nzp_server > 0)
				conn_kernel = Points.GetConnByServer(nzp_server);

			IDbConnection conn_db = GetConnection(conn_kernel);
			ret = OpenDb(conn_db, true);
			if (!ret.result)
			{
				conn_web.Close();
				return;
			}

			//цикл по s_point
			foreach (_Point zap in Points.PointList)
			{
				if (nzp_server > 0)
				{
					if (zap.nzp_server != nzp_server) continue;
				}

				//проверка на доступность банка данных
#if PG
				if (!TempTableInWebCashe(conn_db, zap.pref + "_data.s_area"))
				{
					MonitorLog.WriteLog("Первый запуск приложения.Загрузка поставщиков.Банк данных . " + zap.pref + " не доступен", MonitorLog.typelog.Warn, true);
					continue;
				}
				ExecSQL(conn_db, " Drop table ttt_supp ", false);
				StringBuilder sql = new StringBuilder();
				sql.Append(" Select distinct z.nzp_area,z.nzp_geu,z.nzp_supp,z.nzp_serv,z.nzp_frm, ");
				sql.Append(" area,geu, name_supp,service, trim(coalesce(f.name_frm,''))||' ('||trim(coalesce(m.measure,''))||')' as name_frm ");
				sql.Append(" Into unlogged ttt_supp ");
				sql.Append(" From " + analiz1 + " z ");
				sql.Append(" left outer join " + zap.pref + "_data.s_area a on (z.nzp_area = a.nzp_area) ");
				sql.Append(" left outer join " + zap.pref + "_data.s_geu g on (z.nzp_geu = g.nzp_geu)  ");
				sql.Append(" left outer join " + zap.pref + "_kernel.services s on (z.nzp_serv = s.nzp_serv)  ");
				sql.Append(" left outer join " + zap.pref + "_kernel.supplier p on (z.nzp_supp = p.nzp_supp)  ");
				sql.Append(" left outer join " + zap.pref + "_kernel.formuls f on (z.nzp_frm = f.nzp_frm) " );
				sql.Append(" left outer join " + zap.pref + "_kernel.s_measure m on (f.nzp_measure = m.nzp_measure) ");
				sql.Append(" Where z.nzp_wp = " + zap.nzp_wp );
				sql.Append("   and 1 > ( Select count(*) From " + pXX_spsupp_full + " pp ");
				sql.Append("  Where z.nzp_area = pp.nzp_area and z.nzp_geu = pp.nzp_geu and z.nzp_supp = pp.nzp_supp ");
				sql.Append("    and z.nzp_serv = pp.nzp_serv and z.nzp_frm = pp.nzp_frm ) ");

				//int key = LogSQL(conn_web, finder.nzp_user, pXX_spsupp_full + "." + whereString);
#else
if (!TempTableInWebCashe(conn_db, zap.pref + "_data:s_area"))
				{
					MonitorLog.WriteLog("Первый запуск приложения.Загрузка поставщиков.Банк данных : " + zap.pref + " не доступен", MonitorLog.typelog.Warn, true);
					continue;
				}
				ExecSQL(conn_db, " Drop table ttt_supp ", false);
				StringBuilder sql = new StringBuilder();
				sql.Append(" Select unique z.nzp_area,z.nzp_geu,z.nzp_supp,z.nzp_serv,z.nzp_frm, ");
				sql.Append(" area,geu, name_supp,service, trim(nvl(f.name_frm,''))||' ('||trim(nvl(m.measure,''))||')' as name_frm ");
				sql.Append(" From " + analiz1 + " z, ");
				sql.Append(" outer " + zap.pref + "_data:s_area a,");
				sql.Append(" outer " + zap.pref + "_data:s_geu g, ");
				sql.Append(" outer " + zap.pref + "_kernel:services s, ");
				sql.Append(" outer " + zap.pref + "_kernel:supplier p, ");
				sql.Append(" outer (" + zap.pref + "_kernel:formuls f, " + zap.pref + "_kernel:s_measure m )");
				sql.Append(" Where z.nzp_wp = " + zap.nzp_wp + " and z.nzp_area = a.nzp_area and z.nzp_geu = g.nzp_geu and z.nzp_serv = s.nzp_serv and z.nzp_supp = p.nzp_supp ");
				sql.Append("   and z.nzp_frm = f.nzp_frm and f.nzp_measure = m.nzp_measure ");
				sql.Append("   and 1 > ( Select count(*) From " + pXX_spsupp_full + " pp ");
				sql.Append("  Where z.nzp_area = pp.nzp_area and z.nzp_geu = pp.nzp_geu and z.nzp_supp = pp.nzp_supp ");
				sql.Append("    and z.nzp_serv = pp.nzp_serv and z.nzp_frm = pp.nzp_frm ) ");
				sql.Append(" Into temp ttt_supp With no log ");
				//int key = LogSQL(conn_web, finder.nzp_user, pXX_spsupp_full + ":" + whereString);
#endif
				//int key = LogSQL(conn_web, finder.nzp_user, pXX_spsupp_full + ":" + whereString);

				ret = ExecSQL(conn_db, sql.ToString(), true, 800);
				if (!ret.result)
				{
					//if (key > 0) LogSQL_Error(conn_web, key, ret.text);

					conn_db.Close();
					conn_web.Close();
					return;
				}

#if PG
				ret = ExecSQL(conn_db,
										   " Insert into " + pXX_spsupp_full + " (nzp_area,nzp_geu,nzp_supp,nzp_serv,nzp_frm, area,geu, name_supp,service, name_frm)" +
										   " Select nzp_area,nzp_geu,nzp_supp,nzp_serv,nzp_frm, area,geu, name_supp,service, name_frm " +
										   " From ttt_supp ", true);
#else
ret = ExecSQL(conn_db,
						   " Insert into " + pXX_spsupp_full + " (nzp_area,nzp_geu,nzp_supp,nzp_serv,nzp_frm, area,geu, name_supp,service, name_frm)" +
						   " Select nzp_area,nzp_geu,nzp_supp,nzp_serv,nzp_frm, area,geu, name_supp,service, name_frm " +
						   " From ttt_supp ", true);
#endif
				if (!ret.result)
				{
					conn_db.Close();
					conn_web.Close();
					return;
				}

				ExecSQL(conn_db, " Drop table ttt_supp ", false);

			}

			conn_db.Close(); //закрыть соединение с основной базой
		}

		//----------------------------------------------------------------------
		public void LoadAnaliz1(out Returns ret, int year, bool reload) //заполняет годовую статистику по адресам, поставщикам, услугам и формулам
		//----------------------------------------------------------------------
		{
			ret = Utils.InitReturns();

			bool b_create = false;

			IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
			ret = OpenDb(conn_web, true); 
			if (!ret.result) return;

			string analiz1 = "anl" + year;

			if (reload)
			{
				ExecSQL(conn_web, " Drop table " + analiz1, false);
			}

			if (!TableInWebCashe(conn_web, analiz1))
			{
				//и попутно (пере)создадим в кэш-базе таблицы
				DbAdres db = new DbAdres();
				db.WebArea();
				db.WebGeu();
				db.Close();
				DbSprav dbs = new DbSprav();
				dbs.WebService();
				dbs.WebSupplier();
				dbs.WebPoint();
				dbs.Close();

				//наличие прообраза таблицы
				if (TableInWebCashe(conn_web, "__" + analiz1))
				{
					ret.result = false;
					ret.tag = Constants.workinfon;
					ret.text = "Выполняется подсчет данных для аналитики!";
					return;
				}

				b_create = true;
				CreateAnlXX(conn_web, "__" + analiz1, out ret);

				if (!ret.result)
				{
					conn_web.Close();
					return;
				}
			}

			//заполнить webdata:analiz1
#if PG
			string __analiz1_full = defaultPgSchema + ".__" + analiz1;
			string analiz1_full = defaultPgSchema + "." + analiz1;
			string tmpt = " tmp_" + analiz1;
#else
string __analiz1_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":__" + analiz1;
			string analiz1_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + analiz1;
			string tmpt = " tmp_" + analiz1;
#endif
			if (Points.IsFabric)
			{
				//открываем цикл по серверам БД
				foreach (_Server server in Points.Servers)
				{
					GoPointAnl(conn_web, server.nzp_server, __analiz1_full, analiz1_full, tmpt, b_create, year, analiz1, out ret);
					if (!ret.result)
						break;
				}
			}
			else
				GoPointAnl(conn_web, 0, __analiz1_full, analiz1_full, tmpt, b_create, year, analiz1, out ret);


			if (b_create && ret.result)
			{
#if PG
				ret = ExecSQL(conn_web, " alter table  __" + analiz1 + " rename to " + analiz1, true);
#else
				ret = ExecSQL(conn_web, " Rename table  __" + analiz1 + " to " + analiz1, true);
#endif
			}
#if PG
			ExecSQL(conn_web, " analyze  " + analiz1, false);
#else
			ExecSQL(conn_web, " Update statistics for table  " + analiz1, false);
#endif
			conn_web.Close();
		}//Anlz1

		void GoPointAnl(IDbConnection conn_web, int nzp_server, string __analiz1_full, string analiz1_full, string tmpt, bool b_create, int year, string analiz1, out Returns ret)
		{
			string conn_kernel = Points.GetConnByServer(nzp_server);
			IDbConnection conn_db = GetConnection(conn_kernel);
			ret = OpenDb(conn_db, true);
			if (!ret.result) return;

			try
			{
				//цикл по s_point
				foreach (_Point zap in Points.PointList)
				{
					if (nzp_server > 0)
					{
						if (zap.nzp_server != nzp_server) continue;
					}
					StringBuilder sql = new StringBuilder();

					//вытащить открытые лс
					ExecSQL(conn_db, " Drop table topen_ls1 ", false);
					ExecSQL(conn_db, " Drop table " + tmpt, false);

#if PG
					sql.Append(" CREATE TEMP TABLE topen_ls1 AS ( ");
					sql.Append(" SELECT DISTINCT ");
					sql.Append(" K .nzp_kvar, ");
					sql.Append(" K .nzp_dom, ");
					sql.Append(" K .nzp_area, ");
					sql.Append(" K .nzp_geu ");
					sql.Append(" FROM ");
					sql.Append(zap.pref + "_data.kvar K, ");
					sql.Append(zap.pref + "_data.prm_3 P ");
					sql.Append(" WHERE ");
					sql.Append(" K .nzp_kvar = P .nzp ");
					sql.Append(" AND K .num_ls > 0 ");
					sql.Append(" AND P .nzp_prm = 51 ");
					sql.Append(" AND P .val_prm IN ('1', '2') ");
					sql.Append(" AND P .is_actual <> 100 ");
					sql.Append(" AND P .dat_s <= mdy (12, 31, " + year + ") ");
					sql.Append(" AND P .dat_po >= mdy (1, 1, " + year + ") ");
					sql.Append(" ) ");

#else
					sql.Append(" Select unique k.nzp_kvar, k.nzp_dom, k.nzp_area, k.nzp_geu ");
					sql.Append(" From " + zap.pref + "_data:kvar k, " + zap.pref + "_data:prm_3 p ");
					sql.Append(" Where k.nzp_kvar = p.nzp and k.num_ls > 0 and p.nzp_prm = 51 and p.val_prm in ('1','2') and p.is_actual <> 100 ");
					sql.Append(" and p.dat_s <= mdy(12,31," + year + ") and p.dat_po >= mdy(1,1," + year + ") ");
					sql.Append(" Into temp topen_ls1 With no log ");
#endif

					ret = ExecSQL(conn_db, sql.ToString(), true, 600);
					if (!ret.result) return;

#if PG
					ret = ExecSQL(conn_db, " Create unique index ix_topen_ls1 on topen_ls1 (nzp_kvar) ", true);
					if (!ret.result) return;
					ret = ExecSQL(conn_db, " analyze topen_ls1 ", true);
					if (!ret.result) return;
					sql.Remove(0, sql.Length);
					sql.Append(" Select distinct 0 as kod, k.nzp_dom," + zap.nzp_wp.ToString() + " as nzp_wp, k.nzp_area, k.nzp_geu, t.nzp_serv, t.nzp_supp, t.nzp_frm ");
					sql.Append(" Into unlogged " + tmpt );
					sql.Append(" From topen_ls1 k, " + zap.pref + "_data.tarif t ");
					sql.Append(" Where k.nzp_kvar = t.nzp_kvar and t.dat_s <= mdy(12,31," + year.ToString() + ") and t.dat_po >= mdy(1,1," + year.ToString() + ") ");
					sql.Append(" and t.is_actual <> 100 ");
					ret = ExecSQL(conn_db, sql.ToString(), true, 600);
					ExecSQL(conn_db, " Drop table topen_ls1 ", false);
					if (!ret.result) return;
					ret = ExecSQL(conn_db,
						" Create index ix_" + tmpt.Trim() + " on " + tmpt + " (nzp_dom) "
						, true);
					if (!ret.result) return;
					ret = ExecSQL(conn_db,
						" Update " + tmpt +
						" Set nzp_area = ( " +
							 " Select nzp_area From " + zap.pref + "_data.dom d " +
							 " Where " + tmpt + ".nzp_dom = d.nzp_dom  ) " +
						" Where nzp_area < 1 " +
						"   and exists ( Select nzp_dom From " + zap.pref + "_data.dom d Where " + tmpt + ".nzp_dom = d.nzp_dom ) "
						, true);
					if (!ret.result) return;
					ret = ExecSQL(conn_db,
						" Update " + tmpt +
						" Set nzp_geu = ( " +
							 " Select nzp_geu From " + zap.pref + "_data.dom d " +
							 " Where " + tmpt + ".nzp_dom = d.nzp_dom  ) " +
						" Where nzp_geu < 1 " +
						"   and exists ( Select nzp_dom From " + zap.pref + "_data.dom d Where " + tmpt + ".nzp_dom = d.nzp_dom ) "
						, true);
					if (!ret.result) return;
					if (b_create)
					{
						//полная вставка
						/*
						ret = ExecSQL(conn_db, 
							" Insert into " + __analiz1_full + " (nzp_dom,nzp_wp,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_frm) " +
							" Select distinct nzp_dom,nzp_wp,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_frm "+
							" From " + tmpt + " Where nzp_area > 0 and nzp_geu > 0 ", true, 1000);
						*/
						ExecByStep(conn_db, tmpt, "oid",
							" Insert into " + __analiz1_full + " (nzp_dom,nzp_wp,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_frm) " +
							" Select distinct nzp_dom,nzp_wp,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_frm " +
							" From " + tmpt + " Where nzp_area > 0 and nzp_geu > 0 "
						  , 10000, "", out ret);
						if (!ret.result) return;
					}
					else
					{
						//иначе, вставим только отсутствующие связки
						ret = ExecSQL(conn_db, " Create distinct index " + tmpt + "_1 on " + tmpt + " (nzp_dom, nzp_area,nzp_geu,nzp_supp, nzp_serv, nzp_wp, nzp_frm) ", true);
						if (ret.result)
							ret = ExecSQL(conn_db, " Create index " + tmpt + "_2 on " + tmpt + " (nzp_area,nzp_geu, nzp_serv,nzp_supp, nzp_frm) ", true);
						if (ret.result)
							ret = ExecSQL(conn_db, " analyze " + tmpt, true);
						if (ret.result)
							ret = ExecSQL(conn_db, " Update " + tmpt + " Set kod = 1 " +
												   " Where 1 > ( Select count(*) " +
												   " From " + analiz1_full + ".nzp_dom  = " + tmpt + ".nzp_dom " +
												   "  and " + analiz1_full + ".nzp_area = " + tmpt + ".nzp_area " +
												   "  and " + analiz1_full + ".nzp_geu  = " + tmpt + ".nzp_geu " +
												   "  and " + analiz1_full + ".nzp_supp = " + tmpt + ".nzp_supp " +
												   "  and " + analiz1_full + ".nzp_serv = " + tmpt + ".nzp_serv " +
												   "  and " + analiz1_full + ".nzp_wp   = " + tmpt + ".nzp_wp " +
												   "  and " + analiz1_full + ".nzp_frm  = " + tmpt + ".nzp_frm"
												   , true, 1000);
						if (ret.result)
							ret = ExecSQL(conn_db, " Insert into " + analiz1_full + " (nzp_dom,nzp_wp,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_frm) " +
												   " Select nzp_dom,nzp_wp,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_frm From " + tmpt + " Where kod = 1 ", true);
						if (!ret.result) return;
					}
#else
ret = ExecSQL(conn_db, " Create unique index ix_topen_ls1 on topen_ls1 (nzp_kvar) ", true);
					if (!ret.result) return;
					ret = ExecSQL(conn_db, " Update statistics for table topen_ls1 ", true);
					if (!ret.result) return;
					sql.Remove(0, sql.Length);
					sql.Append(" Select unique 0 as kod, k.nzp_dom," + zap.nzp_wp.ToString() + " as nzp_wp, k.nzp_area, k.nzp_geu, t.nzp_serv, t.nzp_supp, t.nzp_frm ");
					sql.Append(" From topen_ls1 k, " + zap.pref + "_data:tarif t ");
					sql.Append(" Where k.nzp_kvar = t.nzp_kvar and t.dat_s <= mdy(12,31," + year.ToString() + ") and t.dat_po >= mdy(1,1," + year.ToString() + ") ");
					sql.Append(" and t.is_actual <> 100 ");
					sql.Append(" Into temp " + tmpt + " With no log ");
					ret = ExecSQL(conn_db, sql.ToString(), true, 600);
					ExecSQL(conn_db, " Drop table topen_ls1 ", false);
					if (!ret.result) return;
					ret = ExecSQL(conn_db,
						" Create index ix_" + tmpt.Trim() + " on " + tmpt + " (nzp_dom) "
						, true);
					if (!ret.result) return;
					ret = ExecSQL(conn_db,
						" Update " + tmpt +
						" Set nzp_area = ( " +
							 " Select nzp_area From " + zap.pref + "_data:dom d " +
							 " Where " + tmpt + ".nzp_dom = d.nzp_dom  ) " +
						" Where nzp_area < 1 " +
						"   and exists ( Select nzp_dom From " + zap.pref + "_data:dom d Where " + tmpt + ".nzp_dom = d.nzp_dom ) "
						, true);
					if (!ret.result) return;
					ret = ExecSQL(conn_db,
						" Update " + tmpt +
						" Set nzp_geu = ( " +
							 " Select nzp_geu From " + zap.pref + "_data:dom d " +
							 " Where " + tmpt + ".nzp_dom = d.nzp_dom  ) " +
						" Where nzp_geu < 1 " +
						"   and exists ( Select nzp_dom From " + zap.pref + "_data:dom d Where " + tmpt + ".nzp_dom = d.nzp_dom ) "
						, true);
					if (!ret.result) return;
					if (b_create)
					{
						//полная вставка
						/*
						ret = ExecSQL(conn_db, 
							" Insert into " + __analiz1_full + " (nzp_dom,nzp_wp,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_frm) " +
							" Select unique nzp_dom,nzp_wp,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_frm "+
							" From " + tmpt + " Where nzp_area > 0 and nzp_geu > 0 ", true, 1000);
						*/
						ExecByStep(conn_db, tmpt, "rowid",
							" Insert into " + __analiz1_full + " (nzp_dom,nzp_wp,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_frm) " +
							" Select unique nzp_dom,nzp_wp,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_frm " +
							" From " + tmpt + " Where nzp_area > 0 and nzp_geu > 0 "
						  , 10000, "", out ret);
						if (!ret.result) return;
					}
					else
					{
						//иначе, вставим только отсутствующие связки
						ret = ExecSQL(conn_db, " Create unique index " + tmpt + "_1 on " + tmpt + " (nzp_dom, nzp_area,nzp_geu,nzp_supp, nzp_serv, nzp_wp, nzp_frm) ", true);
						if (ret.result)
							ret = ExecSQL(conn_db, " Create index " + tmpt + "_2 on " + tmpt + " (nzp_area,nzp_geu, nzp_serv,nzp_supp, nzp_frm) ", true);
						if (ret.result)
							ret = ExecSQL(conn_db, " Update statistics for table " + tmpt, true);
						if (ret.result)
							ret = ExecSQL(conn_db, " Update " + tmpt + " Set kod = 1 " +
												   " Where 1 > ( Select count(*) " +
												   " From " + analiz1_full + ".nzp_dom  = " + tmpt + ".nzp_dom " +
												   "  and " + analiz1_full + ".nzp_area = " + tmpt + ".nzp_area " +
												   "  and " + analiz1_full + ".nzp_geu  = " + tmpt + ".nzp_geu " +
												   "  and " + analiz1_full + ".nzp_supp = " + tmpt + ".nzp_supp " +
												   "  and " + analiz1_full + ".nzp_serv = " + tmpt + ".nzp_serv " +
												   "  and " + analiz1_full + ".nzp_wp   = " + tmpt + ".nzp_wp " +
												   "  and " + analiz1_full + ".nzp_frm  = " + tmpt + ".nzp_frm"
												   , true, 1000);
						if (ret.result)
							ret = ExecSQL(conn_db, " Insert into " + analiz1_full + " (nzp_dom,nzp_wp,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_frm) " +
												   " Select nzp_dom,nzp_wp,nzp_area,nzp_geu,nzp_serv,nzp_supp,nzp_frm From " + tmpt + " Where kod = 1 ", true);
						if (!ret.result) return;
					}
#endif

				}
			}
			finally
			{
				conn_db.Close(); //закрыть соединение с основной базой
			}
		}
	}
}