using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using System.Data;

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
	        string sourceKvarTable;
	        string whereGeuArea;
            List<_Point> points = getInfoFromAdresPattern(finder, finder.prms , out ret, out sourceKvarTable, out whereGeuArea);
            string whereString = "";
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return;
            }
            IDbConnection conn_web;
            IDbConnection conn_db;
            //для поиска по шаблону поиска
            string tXX_debt = "t" + finder.nzp_user + "_debt";
            //соединение с БД
            conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка при открытии соединения с БД в FindDebt", MonitorLog.typelog.Error, true);
                conn_web.Close();
                return ;
            }
            //соединение с БД
            conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка при открытии соединения с БД в FindZvk", MonitorLog.typelog.Error, true);
                conn_db.Close();
                conn_web.Close();
                return ;
            }
	        try
	        {
	            #region Удаление существующей таблицы

#if PG
	            ExecSQL(conn_web, "set search_path to 'public'", false);
#endif
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
	                    "  nzp_kvar       integer," +
	                    "  debt_money     numeric, " +
	                    "  unpayment_days integer, " +
	                    "  is_priv        char(10)," +
	                    "  children_count int" +
	                    " ) ", true);

	                ExecSQL(conn_db, "create unique index ix_tdebt_098 on public." + tXX_debt + "(nzp_debt)", false);
	                ExecSQL(conn_db, "create index ix_tdebt_099 on public." + tXX_debt + "(nzp_kvar)", false);
	                ExecSQL(conn_db, "create index ix_tdebt_100 on public." + tXX_debt + "(mark)", false);
	                ExecSQL(conn_db, "create index ix_tdebt_101 on public." + tXX_debt + "(adr)", false);
	                ExecSQL(conn_db, "create index ix_tdebt_102 on public." + tXX_debt + "(debt_money)", false);
	                ExecSQL(conn_db, "create index ix_tdebt_103 on public." + tXX_debt + "(fio)", false);
	                ExecSQL(conn_db, "create index ix_tdebt_104 on public." + tXX_debt + "(pref)", false);
	            }
	            catch (Exception ex)
	            {
	                MonitorLog.WriteLog("Ошибка при создании таблицы " + tXX_debt + " " + ex.Message, MonitorLog.typelog.Error, true);
	                ret.result = false;
	                return;
	            }

	            #endregion

	            #region шаблон по долгу

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

	            if (finder.month_count > 0)
	            {
	                sumDebtCond.Append(" and (select round (t.debt/t.sum_real) from tmp_" + finder.nzp_user + "_charge_month_result t where t.nzp_kvar=kv.nzp_kvar and sum_real>0)=" + finder.month_count);
	            }

	            #endregion

	            StringBuilder sqltempTables = new StringBuilder();
	            ExecSQL(conn_db, "DROP TABLE tmp_" + finder.nzp_user + "_charge_month;", false);
	            sqltempTables.Remove(0, sqltempTables.Length);
	            sqltempTables.Append("CREATE TEMP TABLE tmp_" + finder.nzp_user + "_charge_month ");
	            sqltempTables.Append("(nzp_kvar integer, sum_insaldo numeric default 0, sum_money numeric default 0, " +
	                                 "reval numeric default 0, real_charge numeric default 0, sum_real numeric default 0);");
	            ret = ExecSQL(conn_db, sqltempTables.ToString(), true);
	            sqltempTables.Remove(0, sqltempTables.Length);
	            ExecSQL(conn_db, "DROP TABLE tmp_" + finder.nzp_user + "_charge_month_result;", false);
	            sqltempTables.Remove(0, sqltempTables.Length);
	            sqltempTables.Append("CREATE TEMP TABLE tmp_" + finder.nzp_user + "_charge_month_result ");
	            sqltempTables.Append("(nzp_kvar integer, debt numeric, sum_real numeric default 0);");
	            ret = ExecSQL(conn_db, sqltempTables.ToString(), true);

	            foreach (_Point point in points)
	            {
	                StringBuilder sql = new StringBuilder();
                    ExecSQL(conn_db, "truncate tmp_" + finder.nzp_user + "_charge_month;", false);
                    ExecSQL(conn_db, "truncate tmp_" + finder.nzp_user + "_charge_month_result;", false);
	                #region расчет долга

	                RecordMonth rm = Points.GetCalcMonth(new CalcMonthParams(point.pref));
	                //предыдущий месяц
	                DateTime before = (new DateTime(rm.year_, rm.month_, 1)).AddMonths(-1);

	                sql.Remove(0, sql.Length);
	                sql.Append(" Insert into tmp_" + finder.nzp_user + "_charge_month");
	                sql.Append(" (nzp_kvar, sum_insaldo, sum_money, reval, real_charge, sum_real) ");
	                sql.Append(" SELECT");
	                sql.Append(" ch.nzp_kvar,");
	                sql.Append(" SUM(ch.sum_insaldo) as sum_insaldo,");
	                sql.Append(" SUM(ch.sum_money),");
	                sql.Append(" SUM(CASE WHEN ch.reval >= 0 then 0 ELSE ch.reval END) as reval,");
	                sql.Append(" SUM(CASE WHEN ch.real_charge >= 0 then 0 ELSE ch.real_charge END) as real_charge, ");
	                sql.Append(" SUM(ch.sum_real) ");
	                sql.Append(" FROM ");
	                sql.Append(point.pref + "_charge_" + before.Year.ToString().Substring(2, 2) + ".charge_" + before.Month.ToString("00") + " ch,");
	                sql.Append(sourceKvarTable + " kv ");
	                sql.Append(" WHERE");
	                sql.Append(" ch.dat_charge IS NULL ");
	                sql.Append(" AND ch.nzp_serv > 1 ");
	                sql.Append(" AND ch.nzp_kvar = kv.nzp_kvar ");
	                sql.Append(whereGeuArea);
	                sql.Append(" GROUP BY kv.nzp_kvar, ch.nzp_kvar;");
	                ret = ExecSQL(conn_db, sql.ToString(), true);
	                if (!ret.result)
	                {
	                    MonitorLog.WriteLog("Ошибка при заполнении данными " + tXX_debt + " в FindDebt " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
	                    return;
	                }
	                sql.Remove(0, sql.Length);
	                sql.Append(" Insert into tmp_" + finder.nzp_user + "_charge_month");
	                sql.Append(" (nzp_kvar, sum_money, reval, real_charge) ");
	                sql.Append(" SELECT");
	                sql.Append(" ch.nzp_kvar,");
	                sql.Append(" SUM(ch.sum_money),");
	                sql.Append(" SUM(CASE WHEN ch.reval >= 0 then 0 ELSE ch.reval END) as reval,");
	                sql.Append(" SUM(CASE WHEN ch.real_charge >= 0 then 0 ELSE ch.real_charge END) as real_charge");
	                sql.Append(" FROM ");
	                sql.Append(point.pref + "_charge_" + rm.year_.ToString().Substring(2, 2) + ".charge_" + rm.month_.ToString("00") + " ch,");
	                sql.Append(sourceKvarTable + " kv ");
	                sql.Append(" WHERE");
	                sql.Append(" ch.dat_charge IS NULL ");
	                sql.Append(" AND ch.nzp_serv > 1 ");
	                sql.Append(" AND ch.nzp_kvar = kv.nzp_kvar ");
	                sql.Append( whereGeuArea);
	                sql.Append(" GROUP BY kv.nzp_kvar, ch.nzp_kvar;");
	                ret = ExecSQL(conn_db, sql.ToString(), true);
	                if (!ret.result)
	                {
	                    MonitorLog.WriteLog("Ошибка при заполнении данными " + tXX_debt + " в FindDebt " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
	                    return;
	                }

	                //итоговая таблица
	                sql.Remove(0, sql.Length);
	                sql.Append(" Insert into tmp_" + finder.nzp_user + "_charge_month_result");
	                sql.Append(" (nzp_kvar, debt, sum_real) ");
	                sql.Append(" SELECT");
	                sql.Append(" nzp_kvar,");
	                sql.Append(" SUM(res.sum_insaldo) - SUM(res.sum_money)  + SUM(res.reval) + SUM(res.real_charge), ");
	                sql.Append(" SUM(res.sum_real) ");
	                sql.Append(" FROM ");
	                sql.Append(" tmp_" + finder.nzp_user + "_charge_month as res ");
	                sql.Append(" GROUP BY nzp_kvar;");
	                ret = ExecSQL(conn_db, sql.ToString(), true);
	                if (!ret.result)
	                {
	                    MonitorLog.WriteLog("Ошибка при заполнении данными " + tXX_debt + " в FindDebt " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
	                    return;
	                }

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
	                sql.Append("'" + point.pref + "',");
	                sql.Append("kv.nzp_kvar,");
	                sql.Append("(SELECT MAX(val_prm) FROM " + point.pref + "_data.prm_1 WHERE is_actual <> 100 AND nzp = kv.nzp_kvar AND nzp_prm = 8  AND mdy(" + rm.month_ + ",1," + rm.year_ + ") BETWEEN dat_s AND dat_po) as privatiz ");

	                sql.Append("FROM ");
	                sql.Append(point.pref + "_data.s_area area,");
	                sql.Append("tmp_" + finder.nzp_user + "_charge_month_result res, ");
	                sql.Append(Points.Pref + sDataAliasRest + "kvar kv ");


	                sql.Append("LEFT JOIN " + point.pref + "_data.dom d ON kv.nzp_dom = d.nzp_dom ");
	                sql.Append("LEFT JOIN " + point.pref + "_data.s_ulica u  ");
	                sql.Append("LEFT JOIN " + point.pref + "_data.s_rajon r  ");
	                sql.Append("LEFT JOIN " + point.pref + "_data.s_town t ");

	                sql.Append("ON r.nzp_town = t.nzp_town ");
	                sql.Append("ON u.nzp_raj  = r.nzp_raj ");
	                sql.Append("ON d.nzp_ul  = u.nzp_ul ");

	                sql.Append("WHERE ");
	                sql.Append("res.nzp_kvar = kv.nzp_kvar ");
	                sql.Append("AND area.nzp_area = kv.nzp_area ");
	                sql.Append("AND res.debt > 0 ");
	                sql.Append(sumDebtCond);
	                sql.Append(whereString);
	                ret = ExecSQL(conn_db, sql.ToString(), true);
	                if (!ret.result)
	                {
	                    MonitorLog.WriteLog("Ошибка при заполнении данными " + tXX_debt + " в FindDebt " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
	                    return;
	                }
	                ExecSQL(conn_db, DBManager.sUpdStat + " " + tXX_debt, true);

	                #endregion
	            }
	            if (!ret.result)
	            {
	                MonitorLog.WriteLog("Ошибка при заполнении данными " + tXX_debt + " в FindDebt " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
	            }
	        }
	        finally
	        {
                ExecSQL(conn_db, "DROP TABLE tmp_" + finder.nzp_user + "_charge_month;", false);
                ExecSQL(conn_db, "DROP TABLE tmp_" + finder.nzp_user + "_charge_month_result;", false);
	            if (conn_db != null)
	                conn_db.Close();
	            if (conn_web != null)
	                conn_web.Close();
	        }
	    }

        private string getAvaliableRolesVal(string roleval, List<int> listFromUser, string nameColumn)
        {
            // Если RolesVal пуст
            if (String.IsNullOrWhiteSpace(roleval))
            {
                return String.Empty;
            }
            // Если список параметров от пользователя пуст
            if (listFromUser == null || listFromUser.Count <= 0)
            {
                // значения формируются из RoleVal
                return " and " + nameColumn + " in (" + roleval + ")";
            }

            var arrRolesVal = roleval.Split(',');
            // получаем пересечение данных
            var filteredList = new List<int>();
            foreach (var nzp in listFromUser)
            {
                filteredList.AddRange(from role in arrRolesVal where nzp.ToString(CultureInfo.InvariantCulture) == role select nzp);
            }
            //если ничего не отфильтровалось
            if (filteredList.Count <= 0)
            {
                return " and " + nameColumn + " in (" + roleval + ")";
            }
            return " and " + nameColumn + " in (" + String.Join(",", filteredList) + ")";
        }

	    /// <summary>
	    /// формирование результата поиска дел
	    /// </summary>
	    /// <param name="finder"></param>
	    /// <param name="ret"></param>
	    public void FindDeal(DealFinder finder, out Returns ret)
	    {
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь", -1);
                return;
            }

            StringBuilder sql = new StringBuilder();

            //для поиска по шаблону поиска
            string tXX_deal = "t" + finder.nzp_user + "_deal";

            //соединение с БД
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка при открытии соединения с БД в FindDeal", MonitorLog.typelog.Error, true);
                conn_web.Close();
                return ;
            }

            //соединение с БД
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка при открытии соединения с БД в FindDeal", MonitorLog.typelog.Error, true);
                conn_db.Close();
                conn_web.Close();
                return ;
            }

            // таблица для дат последней оплаты
            string nameTempTable = "dat_opl_temp";
	        try
	        {
	            #region Удаление существующей таблицы

	            if (TableInWebCashe(conn_web, tXX_deal))
	            {
	                ExecSQL(conn_web, "SET search_path to public; Drop table " + tXX_deal, false);
	            }

	            #endregion

	            #region Создание таблицы
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
	            if (!ret.result)
	            {
	                MonitorLog.WriteLog("Ошибка при создании таблицы " + tXX_deal + " " +ret.text, MonitorLog.typelog.Error, true);
	                return;
	            }

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
	                cond.Append(" AND d.debt_date = '" + finder.debt_fix_date.ToShortDateString() + "' ");
	            //дети до 18 лет
	            if (finder.children)
	                cond.Append(" AND d.children_count > 0 ");
	            // соглашение
	            if (finder.nzp_agr > 0)
	            {
	                cond.Append(" AND d.nzp_deal=agr.nzp_deal and nzp_agr_status=" + finder.nzp_agr);
	            }
	            //сумма долга со знаком
	            if (finder.mode != 0 && finder.sum_debt > 0)
	            {
	                switch ((EnumMarks) Enum.Parse(typeof (EnumMarks), finder.mode.ToString()))
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
               
                #region Даты последней оплаты
                // Если одно из полей с датами не пустое
	            if ((finder.last_payment_to != DateTime.MinValue && finder.last_payment_to != DateTime.MaxValue) ||
	                (finder.last_payment_from != DateTime.MinValue && finder.last_payment_from != DateTime.MaxValue))
	            {
	                int maxYear = 0;
	                int minYear = 2010;// после 2010 года искать не имеет смысла
	                string sqlWhere = " and dat_vvod between " + Utils.EDateNull(finder.last_payment_from.ToShortDateString()) +
	                                  " and " + Utils.EDateNull(finder.last_payment_to.ToShortDateString());
                    // если начальная дата не пустая
	                if (finder.last_payment_to != DateTime.MinValue && finder.last_payment_to != DateTime.MaxValue)
	                {
	                    maxYear = finder.last_payment_to.Year;
	                }
                    // если конечная дата не пустая
	                if (finder.last_payment_from != DateTime.MinValue && finder.last_payment_from != DateTime.MaxValue)
	                {
	                    minYear = finder.last_payment_from.Year;
	                }
                    // Если конечная дата оказалась пустой, то приравниваем года с и по
	                if (maxYear == 0 && minYear > 2010)
	                {
	                    maxYear = minYear;
	                    sqlWhere = " and dat_vvod=" + Utils.EDateNull(finder.last_payment_from.ToShortDateString());
	                }
                    // Если начальная дата оказалась пустой
	                else if (maxYear > 0 && minYear == 2010)
	                {
	                    sqlWhere = " and dat_vvod <=" + Utils.EDateNull(finder.last_payment_to.ToShortDateString());
	                }

	                // создать временную таблицу, в которой будут лежать ЛС, с последней датой платежа в пределах заданной
                    ExecSQL(conn_db, "Drop table " + nameTempTable, false);
                    ret = ExecSQL(conn_db, "Create temp table " + nameTempTable + " (num_ls integer, last_dat_vvod timestamp)", true);
	                if (!ret.result)
	                {
	                    MonitorLog.WriteLog("Ошибка при заполнении данными " + tXX_deal + " в FindDeal " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
	                    return;
	                }
                    // В цикле по годам проходим по таблицам Pref_fin_xx.pack_ls и извлекаем максимальное dat_vvod для ЛС указанных в таблице sourceKvarTable в пределах указанных дат
	                for (int i = maxYear; i >= minYear; i--)
	                {
	                    string finTable = Points.Pref + "_fin_" + i.ToString("0000").Substring(2, 2) +tableDelimiter +"pack_ls";
	                    if (!TempTableInWebCashe(conn_db, finTable)) continue;
                        string sqlNumLs = "insert into " + nameTempTable + " (num_ls, last_dat_vvod) " +
	                                      "select pl.num_ls, max(dat_vvod) from " +
                                          Points.Pref + "_fin_" + i.ToString("0000").Substring(2, 2) + tableDelimiter + "pack_ls pl,  " +Points.Pref + tableDelimiter + "kvar"+ " kv "+
                                          "where kv.num_ls=pl.num_ls and " + sNvlWord+ "(pl.num_ls,0)>0 " +
                                          "and not exists (select 1 from " + nameTempTable + " d where d.num_ls=pl.num_ls) " + sqlWhere + 
                                          " GROUP BY pl.num_ls ";
	                    ret = ExecSQL(conn_db, sqlNumLs, true);
	                    if (!ret.result)
	                    {
	                        MonitorLog.WriteLog("Ошибка при заполнении данными " + tXX_deal + " в FindDeal " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
	                        return;
	                    }
	                }
                    cond.Append(" and exists (select 1 from " + nameTempTable + " t where t.num_ls=kv.num_ls)");
                }
                #endregion 

                //ответственный
	            if (finder.fio != "")
	                cond.Append(" AND d.fio iLIKE ('%" + finder.fio + "%')");
                #endregion

                #region учитываем шаблон поиска лиц счетов
                var splsFrom = ""; //для учитывания шаблона поиска лиц счетов
                var splsWhere = ""; //для учитывания шаблона поиска лиц счетов
	            var whereAdr = "";
                if (Utils.GetParams(finder.prms, Constants.act_findls))
                {
                    splsFrom = ", " + Points.Pref + "_data" + tableDelimiter + "kvar kv, " + 
                        Points.Pref + "_data" + tableDelimiter + "dom d1, " +
                        Points.Pref + sDataAliasRest + "s_ulica u , " + 
                        Points.Pref + sDataAliasRest + "s_rajon r ," + 
                        Points.Pref + sDataAliasRest + "s_town t ";

                    whereAdr = " and d.nzp_kvar=kv.nzp_kvar  " +
                                     " and kv.nzp_dom = d1.nzp_dom " +
                                     " and kv.nzp_wp is not null " +
                                     " and u.nzp_ul = d1.nzp_ul " +
                                     " and r.nzp_raj = u.nzp_raj " +
                                     " and r.nzp_town=t.nzp_town ";

                    var listPoint = new List<int>();
                    var wherePointList = ""; // условие для банков
                    var listGeu = new List<int>();
                    var whereGeuList = ""; // условие для ЖЭУ
                    var listArea = new List<int>();
                    var whereAreaList = "";// условие для УК

                    decimal d = 0;
                    Decimal.TryParse(finder.pkod.Trim(), out d);

                    var whereString = " and d1.nzp_dom = -1111 "; //чтобы ничего не выбиралось
                    
                    if (finder.num_ls > Constants._ZERO_) whereString = " and kv.num_ls = " + finder.num_ls; // Указан ЛС
                    else if (finder.pkod != "" && d != 0) // Указан платежный код
                    {
                        if (!GlobalSettings.NewGeneratePkodMode) whereString = " and kv.pkod = " + finder.pkod;
                    }
                    else if (finder.pkod10 > 0 && !Points.IsSmr) whereString = " and kv.pkod10 = " + finder.pkod10;
                    else
                    {
                        var swhere = new StringBuilder();
                        int i;
                        // Для нулевого платежного кода  разрешается применять дополнительные фильтры
                        if (finder.pkod != "" && d == 0) swhere.Append(" and  " + sNvlWord + "(kv.pkod,0) = " + finder.pkod);
                        if (finder.pkod10 > 0 && Points.IsSmr) swhere.Append(" and kv.pkod10 = " + finder.pkod10);
                        if (finder.typek > 0) swhere.Append(" and kv.typek = " + finder.typek);
                        if (finder.uch.Trim() != "") swhere.Append(" and kv.uch = " + Convert.ToInt32(finder.uch));

                        // Формирование условий для УК
                        if (finder.list_nzp_area != null && finder.list_nzp_area.Count > 0)
                        {
                            listArea.AddRange(finder.list_nzp_area);
                            whereAreaList = " and kv.nzp_area in (" + String.Join(",", finder.list_nzp_area) + ")";
                        }
                        else if (finder.nzp_area > 0)
                        {
                            whereAreaList = " and kv.nzp_area = " + finder.nzp_area;
                            listArea.Add(finder.nzp_area);
                        }

                        // формирование уловий для ЖЭУ
                        if (finder.nzp_geu > 0)
                        {
                            whereGeuList = " and kv.nzp_geu = " + finder.nzp_geu;
                            listGeu.Add(finder.nzp_geu);
                        }

                        if (finder.nzp_town > 0) swhere.Append(" and t.nzp_town = " + finder.nzp_town);
                        if (finder.nzp_raj > 0) swhere.Append(" and r.nzp_raj = " + finder.nzp_raj);
                        if (finder.nzp_ul > 0) swhere.Append(" and u.nzp_ul = " + finder.nzp_ul);

                        //  Указан дом
                        if (finder.nzp_dom > 0) swhere.Append(" and kv.nzp_dom = " + finder.nzp_dom);
                        else
                        {
                            if (finder.ndom_po != "")
                            {
                                i = Utils.GetInt(finder.ndom_po);
                                if (i > 0) swhere.Append(" and d1.idom <= " + i);

                                i = Utils.GetInt(finder.ndom);
                                if (i > 0) swhere.Append(" and d1.idom >= " + i);
                            }
                            else if (finder.ndom != "") swhere.Append(" and upper(d1.ndom) = " + Utils.EStrNull(finder.ndom.ToUpper()));
                        }

                        if (finder.nkor != "") swhere.Append(" and upper(d1.nkor) = " + Utils.EStrNull(finder.nkor.ToUpper()));//корпус

                        // Указана квартира
                        if (finder.nzp_kvar > 0) swhere.Append(" and kv.nzp_kvar = " + finder.nzp_kvar);
                        else
                        {
                            if (finder.stateID > 0) swhere.Append(" and cast(kv.is_open as integer) = " + finder.stateID);
                            else if (finder.stateIDs != null && finder.stateIDs.Count > 0) swhere.Append(" and cast(kv.is_open as integer) in (" + String.Join(",", finder.stateIDs) + ")");
                            else swhere.Append(" and kv.is_open in ('" + Ls.States.Open.GetHashCode() + "','" + Ls.States.Closed.GetHashCode() + "')");

                            if (finder.nkvar_po != "")
                            {
                                i = Utils.GetInt(finder.nkvar_po);
                                if (i > 0) swhere.Append(" and kv.ikvar <= " + i);

                                i = Utils.GetInt(finder.nkvar);
                                if (i > 0) swhere.Append(" and kv.ikvar >= " + i);
                            }
                            else if (finder.nkvar != "") swhere.Append(" and kv.nkvar = " + Utils.EStrNull(finder.nkvar));
                        }

                        if (finder.fio != "") swhere.Append(" and upper(kv.fio) like '%" + finder.fio.ToUpper() + "%'");
                        if (finder.nkvar_n != "") swhere.Append(" and upper(kv.nkvar_n)= '" + finder.nkvar_n.ToUpper().Trim() + "'");
                        if (finder.phone != "") swhere.Append(" and upper(kv.phone) like '%" + finder.phone.ToUpper() + "%'");
                        if (finder.porch != "") swhere.Append(" and kv.porch=" + finder.porch.Trim());

                        if (!String.IsNullOrWhiteSpace(finder.remark)) swhere.Append(" and lower(kv.remark) like lower('%" + finder.remark.Trim() + "%')");

                        whereString = swhere.ToString();

                        // Формирование условий для банков
                        // Выбран  один банк
                        if (finder.nzp_wp > 0)
                        {
                            listPoint.Add(finder.nzp_wp);
                            wherePointList = " and kv.nzp_wp=" + finder.nzp_wp;
                        }
                        // задано несколько банков
                        else if (finder.dopPointList != null && finder.dopPointList.Count > 0)
                        {
                            listPoint.AddRange(finder.dopPointList);
                            wherePointList = " and kv.nzp_wp in (" + String.Join(",", finder.dopPointList) + ")";
                        }
                    }

                    // ограничения по ролям
                    if (finder.RolesVal != null)
                    {
                        foreach (_RolesVal role in finder.RolesVal.Where(role => role.tip == Constants.role_sql))
                        {
                            switch (role.kod)
                            {
                                case Constants.role_sql_area:
                                    whereAreaList = getAvaliableRolesVal(role.val, listArea, "kv.nzp_area");
                                    break;
                                case Constants.role_sql_wp:
                                    wherePointList = getAvaliableRolesVal(role.val, listPoint, "kv.nzp_wp");
                                    break;
                                case Constants.role_sql_geu:
                                    whereGeuList = getAvaliableRolesVal(role.val, listGeu, "kv.nzp_geu");
                                    break;
                            }
                        }
                    }

                    // формирование базового условия основного запроса
                    whereString += whereAreaList + whereGeuList + wherePointList;
                    splsWhere += whereAdr + whereString;
                }

                if (finder.nzp_kvar != 0)
                {
                    splsWhere = whereAdr + " and d.nzp_kvar = " + finder.nzp_kvar;
                }

                #endregion

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
	            sql.Append(Points.Pref + "_debt"+tableDelimiter+"s_deal_statuses sds " + splsFrom);
                if (finder.nzp_agr > 0)
                {
                    sql.Append("," + Points.Pref + "_debt" + tableDelimiter + "agreement agr ");
                }
	            sql.Append("WHERE ");
	            sql.Append("d.nzp_deal_status = sds.nzp_deal_status ");
	            sql.Append(" and kv.nzp_kvar= d.nzp_kvar");
	            sql.Append(cond);
	            sql.Append(splsWhere);
	            ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при заполнении данными " + tXX_deal + " в FindDeal " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                    return;
                }

	            if (finder.nzp_wp > 0)
	            {
                    sql.Remove(0, sql.Length);
                    sql.Append("UPDATE " + sDefaultSchema + tXX_deal + " ");
                    sql.Append("SET area = ar.area FROM " + Points.GetPref(finder.nzp_wp) + "_data" + tableDelimiter + "s_area ar ");
                    sql.Append("WHERE ar.nzp_area = " + sDefaultSchema + tXX_deal + ".nzp_area;");
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка при заполнении данными " + tXX_deal + " в FindDeal " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                    }
	            }
                else if (finder.dopPointList != null)
                {
                    foreach (var nzpWp in finder.dopPointList)
                    {
                        sql.Remove(0, sql.Length);
                        sql.Append("UPDATE " + sDefaultSchema + tXX_deal + " ");
                        sql.Append("SET area = ar.area FROM " + Points.GetPref(nzpWp) + "_data" + tableDelimiter +
                                   "s_area ar ");
                        sql.Append("WHERE ar.nzp_area = " + sDefaultSchema + tXX_deal + ".nzp_area;");
                        ret = ExecSQL(conn_db, sql.ToString(), true);
                        if (ret.result) continue;
                        MonitorLog.WriteLog("Ошибка при заполнении данными " + tXX_deal + " в FindDeal " + ret.text,
                            MonitorLog.typelog.Error, 20, 201, true);
                        return;
                    }
                }
                else
                {
                    sql.Remove(0, sql.Length);
                    sql.Append("select distinct pref from " + sDefaultSchema + tXX_deal);
                    MyDataReader reader;
                    ret = ExecRead(conn_db, out reader, sql.ToString(), true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка при заполнении данными " + tXX_deal + " в FindDeal " + ret.text,
                           MonitorLog.typelog.Error, 20, 201, true);
                        return;
                    }
                    while (reader.Read())
                    {
                        sql.Remove(0, sql.Length);
                        sql.Append("UPDATE " + sDefaultSchema + tXX_deal + " ");
                        sql.Append("SET area = ar.area FROM " + reader["pref"].ToString().Trim() + "_data" + tableDelimiter +
                                   "s_area ar ");
                        sql.Append("WHERE ar.nzp_area = " + sDefaultSchema + tXX_deal + ".nzp_area;");
                        ret = ExecSQL(conn_db, sql.ToString(), true);
                        if (ret.result) continue;
                        MonitorLog.WriteLog("Ошибка при заполнении данными " + tXX_deal + " в FindDeal " + ret.text,
                            MonitorLog.typelog.Error, 20, 201, true);
                        return;
                    }
                    reader.Close();
                }
	        }
	        finally
	        {
                ExecSQL(conn_db, "Drop table " + nameTempTable, false);
                conn_web.Close(); 
                conn_db.Close();
	        }
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
                        RecordMonth rm = Points.GetCalcMonth(new CalcMonthParams(pref));
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
                        sql.Append("'01-" + rm.month_.ToString("00") + "-" + rm.year_ +"'");
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

	    /// <summary>
	    /// Получает список банков, таблицу- источник ЛС, условия для ЖЭУ и УК
	    /// </summary>
	    /// <param name="finder"></param>
	    /// <param name="prms"></param>
	    /// <param name="ret"></param>
	    /// <param name="sourceKvarTable"></param>
	    /// <param name="whereGeuArea"></param>
	    /// <returns></returns>
	    private List<_Point> getInfoFromAdresPattern( Ls finder,string prms, out Returns ret, out string sourceKvarTable, out string whereGeuArea)
	    {
	        whereGeuArea = "";
	        ret = Utils.InitReturns();
	        List<_Point> points = new List<_Point>();

            if (Utils.GetParams(prms, Constants.act_findls))
	        {
	            sourceKvarTable = "t" + finder.nzp_user + "_spls ";
	            //вызов следует из шаблона адресов, поэтому
	            //надо прежде заполнить список адресов
	            DbAdres db = new DbAdres();

	            db.FindLs((Ls) finder, out ret);
	            db.Close();
	            if (!ret.result)
	            {
	                return points;
	            }
	            // извлечем банки с префиксами из таблицы tXX_spls
                //соединение с БД

                IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД в getPointListFromAdresPattern", MonitorLog.typelog.Error, true);
                    conn_db.Close();
                    return points;
                }

                string sqlNzpWpPref = "select " + sUniqueWord + " pref from  " + sourceKvarTable;
	            IDataReader reader;
	            ret = ExecRead(conn_db, out reader, sqlNzpWpPref, true);
	            if (!ret.result)
	            {
	                conn_db.Close();
	                return points;
	            }
	            try
	            {
	                // для каждой извлеченной записи собираем дополнительные условия в соответствии с указанными шаблонами поиска
	                while (reader.Read())
	                {
	                    string pref = reader["pref"] != DBNull.Value ? ((string) reader["pref"]).Trim() : "";
	                    if (pref == "") continue;
	                    points.Add(Points.GetPoint(pref));
	                }
	                return points;
	            }
	            catch (Exception ex)
	            {
	                conn_db.Close();
	                ret.result = false;
	                ret.text = ex.Message;
	                string err;
	                if (Constants.Viewerror)
	                    err = " \n " + ex.Message;
	                else
	                    err = "";
	                MonitorLog.WriteLog("Ошибка поиска ЛС по дополнительным шаблонам " + err, MonitorLog.typelog.Error, 20, 201, true);
	                return points;
	            }
	            finally
	            {
	                if (reader != null) reader.Close();
                    conn_db.Close();
	            }
	        }
	        sourceKvarTable = Points.Pref + sDataAliasRest + "kvar ";
	        points = Points.PointList;
	        if (finder.RolesVal != null)
	            foreach (_RolesVal role in finder.RolesVal)
	            {
	                if (role.tip == Constants.role_sql)
	                {
	                    if (role.kod == Constants.role_sql_area)
	                    {
	                        whereGeuArea += " AND kv.nzp_area IN (" + role.val + ")";
	                    }
	                    else if (role.kod == Constants.role_sql_bank)
	                    {
	                        // Получить банки ограниченные ролями
	                        try
	                        {
	                            string[] banksString = role.val.Split(',');
	                            int[] banksInt = Array.ConvertAll(banksString, Int32.Parse);
	                            if (banksInt.Length <= 0) continue;
	                            points = banksInt.Select(Points.GetPoint).ToList();
	                        }
	                        catch (Exception ex)
	                        {

	                        }
	                    }
	                    else
	                    {
	                        if (role.kod == Constants.role_sql_geu) whereGeuArea += " AND kv.nzp_geu IN (" + role.val + ")";
	                    }
	                }
	            }
	        return points;
	    }
	}
}
