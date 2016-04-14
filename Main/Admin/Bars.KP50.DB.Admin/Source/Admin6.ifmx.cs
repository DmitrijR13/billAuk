using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Text;
using System.IO;
using System.Data.OleDb;
using System.Linq;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using STCLINE.KP50.Utility;


namespace STCLINE.KP50.DataBase
{
    public partial class DbAdmin : DbAdminClient
    {
        public List<TransferHome> GetHouseList(TransferHome finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не выбран пользователь";
                return null;
            }
            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            var list = new List<TransferHome>();
            MyDataReader reader;
            #endregion

            try
            {
                var sql =
                    string.Format(
                        "select d.nzp_dom,trim(a.town)||' '||(case when trim({1}(g.rajon,''))='-' then ' ' else trim({1}(g.rajon,'')) end)  " +
                        " ||' '||trim({1}(u.ulicareg,''))||' '||trim({1}(u.ulica,''))|| " +
                        "   ' д.'||trim({1}(d.ndom,''))||' '||(case when trim({1}(d.nkor,''))='-' then '' else trim({1}(d.nkor,'')) end) address " +
                        "    from {0}dom d  " +
                        "    left outer join {0}s_ulica u   " +
                        "    left outer join {0}s_rajon g  " +
                        "    left outer join {0}s_town a   " +
                        "    on g.nzp_town = a.nzp_town   " +
                        "    on u.nzp_raj  = g.nzp_raj  " +
                        "    on d.nzp_ul  = u.nzp_ul   " +
                        "    where nzp_wp = {3} and (trim('{2}') = '' or trim(a.town)||' '||(case when trim({1}(g.rajon,''))='-' then ' ' else trim({1}(g.rajon,'')) end)  " +
                        "   ||' '||trim({1}(u.ulicareg,''))||' '||trim({1}(u.ulica,''))|| " +
                        "   ' д.'||trim({1}(d.ndom,''))||' '||(case when trim({1}(d.nkor,''))='-' then '' else trim({1}(d.nkor,'')) end) ilike '%{2}%') "
                        ,
                        Points.Pref + DBManager.sDataAliasRest, DBManager.sNvlWord,
                        finder.address, finder.nzp_wp);
                var where = ((finder.skip > 0) ? " offset " + finder.skip : "") +
                            "  " + ((finder.rows > 0) ? " limit " + finder.rows : "");

                ret = ExecRead(conn_db, out reader, sql + where, true);
                if (!ret.result)
                {
                    return null;
                }
                while (reader.Read())
                {
                    var transfer = new TransferHome
                    {
                        address = reader["address"] == DBNull.Value ? "" : reader["address"].ToString(),
                        nzp_dom = reader["nzp_dom"] == DBNull.Value ? 0 : Convert.ToInt32(reader["nzp_dom"])
                    };
                    list.Add(transfer);
                }
                ret.tag = ClassDBUtils.OpenSQL(sql, conn_db).resultData.Rows.Count;
            }
            catch
            {
                return null;
            }
            finally
            {
                conn_db.Close();
                conn_db.Dispose();
            }
            return new List<TransferHome>(list);
        }
        #region Подготовка проводок для первого расчета пени

        /// <summary>
        /// Переподготовить проводки по списку ЛС
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="table_with_kvars"></param>
        /// <param name="typePrepare">объем списка лс для переформирования проводок</param>
        /// <returns></returns>
        public Returns RePrepareProvsOnListLs(Ls finder, TypePrepareProvs typePrepare)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                if (finder.nzp_user <= 0)
                {
                    ret.result = false;
                    ret.text = "Не определен пользователь";
                    return ret;
                }
                if (finder.nzp_kvar <= 0 && typePrepare == TypePrepareProvs.OneLs)
                {
                    ret.result = false;
                    ret.text = "Не определен номер лицевого счета";
                    return ret;
                }
                using (var conn_db = DBManager.GetConnection(Points.GetConnByPref(Points.Pref)))
                {
                    ret = DBManager.OpenDb(conn_db, true);
                    if (!ret.result)
                    {
                        return ret;
                    }
                    var temp_provs = "t_provs" + finder.nzp_user + "_" + DateTime.Now.Ticks;
                    var where_nzp_kvar = " AND nzp_kvar=" + finder.nzp_kvar;
                    switch (typePrepare)
                    {
                        case TypePrepareProvs.OneLs: break;
                        case TypePrepareProvs.ListLs:
                            {
                                ExecSQL(conn_db, "DROP TABLE " + temp_provs, false);
                                ret = ExecSQL(conn_db,
                                    "CREATE TEMP TABLE " + temp_provs + " AS SELECT nzp_kvar FROM " + sDefaultSchema + "t" + finder.nzp_user +
                                    "_spls WHERE mark=1", true);
                                if (!ret.result)
                                {
                                    return ret;
                                }
                                CreateIndexIfNotExists(conn_db, "ix1_" + temp_provs, temp_provs, "nzp_kvar");

                                where_nzp_kvar = " AND nzp_kvar in (SELECT nzp_kvar FROM " + temp_provs + ")";
                                break;
                            }
                        default:
                            {
                                ret.text = "Не определен режим переформирования проводок";
                                ret.result = false;
                                return ret;
                            }
                    }


                    var sql = "SELECT nzp_wp,pref FROM " + Points.Pref + sDataAliasRest + "kvar WHERE 1=1" + where_nzp_kvar +
                              " GROUP BY 1,2";
                    var pointsDT = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                    //цикл по банкам данных
                    for (int i = 0; i < pointsDT.Rows.Count; i++)
                    {
                        finder.nzp_wp = CastValue<int>(pointsDT.Rows[i]["nzp_wp"]);
                        finder.pref = CastValue<string>(pointsDT.Rows[i]["pref"]);

                        var table = "t" + finder.nzp_user + "_provs_prepare_" + DateTime.Now.Ticks;
                        ExecSQL(conn_db, "DROP TABLE " + table, false);

                        sql = " CREATE TEMP TABLE " + table + " AS " +
                              " SELECT num_ls FROM " + Points.Pref + sDataAliasRest + "kvar WHERE nzp_wp=" + finder.nzp_wp +
                              " " + where_nzp_kvar;
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result)
                        {
                            return ret;
                        }

                        ExecSQL(conn_db, "CREATE INDEX ix1_" + table + " ON " + table + "(num_ls)", false);

                        ret = RePrepareProvs(finder, conn_db, " AND num_ls in (SELECT num_ls FROM " + table + " k)");
                        if (!ret.result)
                        {
                            return ret;
                        }
                        ExecSQL(conn_db, "DROP TABLE " + table, false);
                    }
                }
                return ret;


            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка переформирования проводок: \n" + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
                return ret;
            }

        }


        /// <summary>
        /// Подготовка первого расчета пени по банку данных
        /// пишем проводки по начислениям и оплатам
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="list_nzp_kvar">таблица со списком ЛС для переформирования проводок</param>
        /// <returns></returns>
        public Returns RePrepareProvs(Finder finder, IDbConnection conn_db = null, string where = "", int idCalcFonTask = 0)
        {
            var ret = Utils.InitReturns();
            if (finder.nzp_wp <= 0)
            {
                ret.result = false;
                ret.text = "Не выбран банк по которому записывать проводки";
                return ret;
            }
            if (conn_db == null)
            {
                conn_db = DBManager.GetConnection(Points.GetConnByPref(Points.Pref));
                ret = DBManager.OpenDb(conn_db, true);
                if (!ret.result)
                {
                    return ret;
                }
            }

            SetStatusForTaskPrepareProvs(conn_db, finder, idCalcFonTask, 0, "Начало записи проводок");

            try
            {
                //текущий расчетный месяц
                var prm = new CalcMonthParams();
                prm.pref = finder.pref;
                var rec = Points.GetCalcMonth(prm);
                //текущий расчетный месяц этого банка
                var CalcDate = new DateTime(rec.year_, rec.month_, 1);

                DateTime min_date = GetDateStartPeni(conn_db, new _Point { pref = finder.pref }, out ret);
                //дата старта пени, параметр =99, у месяца всегда 1й день 
                if (min_date == DateTime.MinValue)
                {
                    ret.result = false;
                    ret.text = "\n Не задана дата начала расчета пени!";
                    return ret;
                }
                DateTime max_date = CalcDate.AddDays(-1); //последний день закрытого месяца в этом банке
                //за какой период подготовили проводки
                CreateRecordInReestr(conn_db,
                    where == "" ? peni_actions_type.PrepareFirstCalcPeni : peni_actions_type.RePrepareProvs, min_date,
                    max_date, finder);

                SetStatusForTaskPrepareProvs(conn_db, finder, idCalcFonTask, 10, "Подготовка данных");

                //определяем список опердней по которым пишем и удаляем проводки
                var listDays = GetListOperDaysForProv(conn_db, min_date, finder, out ret);
                if (!ret.result)
                    return ret;

                //определяем список месяцев по которым пишем и удаляем проводки
                var listMonths = GetListCalcMonths(conn_db, finder, min_date, out ret);
                if (!ret.result)
                    return ret;

                SetStatusForTaskPrepareProvs(conn_db, finder, idCalcFonTask, 20, "Удаление предыдущих проводок");

                if (listDays.Count > 0)
                {
                    //Архивируем проводки за полученный период
                    ret = ArchivProvForBackDay(conn_db, finder, listDays, where);
                    if (!ret.result)
                    {
                        return ret;
                    }
                }
                if (listMonths.Count > 0)
                {
                    ret = ArchivProvForBackMonth(conn_db, finder, listMonths, where);
                    if (!ret.result)
                    {
                        return ret;
                    }
                }
                if (listDays.Count > 0)
                {
                    SetStatusForTaskPrepareProvs(conn_db, finder, idCalcFonTask, 60, "Запись проводок по оплатам");
                    //записываем проводки за этот период
                    ret = InsertProvOnCloseOperDay(conn_db, finder, listDays, where);
                    if (!ret.result)
                    {
                        return ret;
                    }
                }
                if (listMonths.Count > 0)
                {
                    SetStatusForTaskPrepareProvs(conn_db, finder, idCalcFonTask, 80, "Запись проводок по начислениям");
                    ret = InsertProvOnClosedMonths(conn_db, finder, listMonths, where, true);
                    if (!ret.result)
                    {
                        return ret;
                    }
                }
            }
            catch (Exception ex)
            {
                if (!ret.result)
                {
                    ret.text = "Ошибка записи проводок (см. логи)";
                    MonitorLog.WriteLog("Ошибка записи проводок: " + (Constants.Viewerror ? "\n " + ex.Message : "") + ";\n " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                }
            }
            finally
            {
                SetStatusForTaskPrepareProvs(conn_db, finder, idCalcFonTask, 100, ret.result ? "Успешно" : "Ошибка");
                if (conn_db != null)
                {
                    conn_db.Close();
                    conn_db.Dispose();
                }
            }

            return ret;
        }

        /// <summary>
        /// Обновление статуса задачи "Подготовка расчета пени"
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <param name="idCalcFonTask">nzp_key из calc_fon</param>
        /// <param name="txt"></param>
        private void SetStatusForTaskPrepareProvs(IDbConnection conn_db, Finder finder, int idCalcFonTask, float procent, string txt)
        {
            var ret = Utils.InitReturns();
            if (idCalcFonTask <= 0)
            {
                return;
            }
            var calc_fon = sDefaultSchema + "calc_fon_" + Points.GetCalcNum(0);
            var progress = procent > 0 ? procent / 100 : 0;
            var sql = string.Format("UPDATE {0} SET txt='Банк данных \"{1}\": {2} (пользователь: {3})' , progress={4} WHERE nzp_key={5}",
                calc_fon, Points.GetPoint(finder.nzp_wp).point, txt, finder.webUname, (progress > 1 ? 1 : progress), idCalcFonTask);
            ExecSQL(conn_db, sql, true);
        }


        #region Запись проводок по оплатам
        /// <summary>
        /// Записываем проводки при закрытии опердня 
        /// вызов производится после операции закрытия месяца
        /// </summary>
        /// <returns></returns>
        public Returns InsertProvOnCloseOperDay(IDbConnection conn_db, Finder finder, List<OperDayForProv> listDays, string where)
        {
            Returns ret = Utils.InitReturns();
            var sql = "";

            #region  цикл по всем банкам
            foreach (var day in listDays)
            {
                //выбираем 
                DateTime min_date = (from date in day.list_dat_uchet select date).Min();
                DateTime max_date = (from date in day.list_dat_uchet select date).Max();
                var reestrId = CreateRecordInReestr(conn_db, peni_actions_type.InsertProvCloseDay, min_date, max_date, finder);
                try
                {
                    //схема откуда берем данные
                    var schemaNameFrom = "";
                    //схема куда эти данные записываем
                    var schemaNameTo = "";
                    var tableName = "";
                    for (int j = 0; j < day.list_dat_uchet.Count; j++)
                    {
                        var startDatePeni = GetDateStartPeni(conn_db, new _Point { pref = day.pref }, out ret);

                        #region from_supplier
                        schemaNameFrom = day.pref + "_charge_" + (day.list_dat_uchet[j].Year - 2000).ToString("00");
                        var DT = ClassDBUtils.OpenSQL("SELECT dat_prih FROM " + schemaNameFrom + tableDelimiter +
                                                 "from_supplier WHERE dat_uchet in (" + Utils.EStrNull(day.list_dat_uchet[j].ToShortDateString()) +
                                                 ") " + where + " GROUP BY 1",
                       conn_db).resultData;

                        for (int i = 0; i < DT.Rows.Count; i++)
                        {
                            var dat_prih = CastValue<DateTime>(DT.Rows[i]["dat_prih"]);
                            var dat_prih_source = CastValue<DateTime>(DT.Rows[i]["dat_prih"]);

                            //Если оплата за предыдущий месяц(до 1 числа месяца начала расчета пени) учтена в следующем месяце она не будет учтена,
                            //для этого записываем ее на 1е число месяца даты начала расчета пени
                            if (dat_prih < startDatePeni)
                            {
                                dat_prih = startDatePeni;
                            }
                            schemaNameTo = day.pref + "_charge_" + (dat_prih.Year - 2000).ToString("00");
                            tableName = schemaNameTo + tableDelimiter + "peni_provodki_" + dat_prih.Year +
                                        dat_prih.Month.ToString("00") + "_" +
                                        day.nzp_wp;

                            sql = " INSERT INTO " + tableName +
                                  " (num_ls,nzp_kvar,nzp_dom,nzp_serv,nzp_supp,nzp_wp,s_prov_types_id,nzp_source,sum_prih,date_prov,date_obligation,created_on,created_by,peni_actions_id) " +
                                  " SELECT k.num_ls,k.nzp_kvar,k.nzp_dom,nzp_serv,nzp_supp," + day.nzp_wp + "," +
                                  (int)s_prov_types.PaymentFromSupp +
                                  ",nzp_to,sum_prih,dat_uchet," + Utils.EStrNull(dat_prih.ToShortDateString()) + "," + sCurDateTime + "," + finder.nzp_user + "," +
                                  reestrId +
                                  " FROM " + schemaNameFrom + tableDelimiter + "from_supplier f, " + day.pref + sDataAliasRest + "kvar k " +
                                  " WHERE NOT EXISTS (SELECT 1 FROM " + Points.Pref + sKernelAliasRest + "peni_settings s WHERE s.nzp_peni_serv=f.nzp_serv)" +
                                  " AND f.num_ls=k.num_ls  " + where.Replace("num_ls", "k.num_ls") +
                                  " AND dat_uchet in (" + Utils.EStrNull(day.list_dat_uchet[j].ToShortDateString()) + ") " +
                                  " AND abs(sum_prih)>0 AND dat_prih=" + Utils.EStrNull(dat_prih_source.ToShortDateString());
                            ret = ExecSQL(conn_db, sql, false);
                            if (!ret.result)
                            {
                                if (!CheckExistTableProv(conn_db, day.nzp_wp, dat_prih).result)
                                {
                                    throw new Exception(ret.text);
                                }
                                ret = ExecSQL(conn_db, sql, true);
                                if (!ret.result)
                                    throw new Exception(ret.text);
                            }

                        }

                        #endregion

                        #region fn_supplier_xx
                        DT =
                            ClassDBUtils.OpenSQL(
                                "SELECT dat_prih FROM " + schemaNameFrom + tableDelimiter + "fn_supplier" +
                                day.list_dat_uchet[j].Month.ToString("00") +
                                " WHERE dat_uchet in (" + Utils.EStrNull(day.list_dat_uchet[j].ToShortDateString()) + ") " + where + " GROUP BY 1", conn_db).resultData;

                        for (int i = 0; i < DT.Rows.Count; i++)
                        {

                            var dat_prih = CastValue<DateTime>(DT.Rows[i]["dat_prih"]); //дата платежа может быть изменена, если она меньше даты начала расчета пени
                            var dat_prih_source = CastValue<DateTime>(DT.Rows[i]["dat_prih"]);

                            //Если оплата за предыдущий месяц(до 1 числа месяца начала расчета пени) учтена в следующем месяце она не будет учтена,
                            //для этого записываем ее на 1е число месяца даты начала расчета пени
                            if (dat_prih < startDatePeni)
                            {
                                dat_prih = startDatePeni;
                            }

                            schemaNameTo = day.pref + "_charge_" + (dat_prih.Year - 2000).ToString("00");
                            tableName = schemaNameTo + tableDelimiter + "peni_provodki_" + dat_prih.Year +
                                        dat_prih.Month.ToString("00") + "_" +
                                        day.nzp_wp;

                            sql = " INSERT INTO " + tableName +
                                  " (num_ls,nzp_kvar,nzp_dom,nzp_serv,nzp_supp,nzp_wp,s_prov_types_id,nzp_source,sum_prih,date_prov,date_obligation,created_on,created_by, peni_actions_id) " +
                                  " SELECT k.num_ls,k.nzp_kvar,k.nzp_dom,nzp_serv,nzp_supp," + day.nzp_wp + "," + (int)s_prov_types.Payment +
                                  ",nzp_to,sum_prih,dat_uchet," + Utils.EStrNull(dat_prih.ToShortDateString()) + "," + sCurDateTime + "," + finder.nzp_user + "," +
                                  reestrId +
                                  " FROM " + schemaNameFrom + tableDelimiter + "fn_supplier" + day.list_dat_uchet[j].Month.ToString("00") + " f, " + day.pref + sDataAliasRest + "kvar k " +
                                  " WHERE NOT EXISTS (SELECT 1 FROM " + Points.Pref + sKernelAliasRest + "peni_settings s WHERE s.nzp_peni_serv=f.nzp_serv)" +
                                  " AND f.num_ls=k.num_ls  " + where.Replace("num_ls", "k.num_ls") +
                                  " AND dat_uchet in (" + Utils.EStrNull(day.list_dat_uchet[j].ToShortDateString()) + ") AND abs(sum_prih)>0 AND dat_prih=" +
                                  Utils.EStrNull(dat_prih_source.ToShortDateString());
                            ret = ExecSQL(conn_db, sql, false);
                            if (!ret.result)
                            {
                                if (!CheckExistTableProv(conn_db, day.nzp_wp, dat_prih).result)
                                {
                                    throw new Exception(ret.text);
                                }
                                ret = ExecSQL(conn_db, sql, true);
                                if (!ret.result)
                                    throw new Exception(ret.text);
                            }

                        }

                        #endregion

                        #region del_supplier

                        DT = ClassDBUtils.OpenSQL("SELECT dat_account FROM " + schemaNameFrom + tableDelimiter + " " +
                                                  "del_supplier WHERE dat_uchet in (" + Utils.EStrNull(day.list_dat_uchet[j].ToShortDateString()) + ") " + where + " GROUP BY 1",
                            conn_db).resultData;
                        for (int i = 0; i < DT.Rows.Count; i++)
                        {
                            var dat_account = CastValue<DateTime>(DT.Rows[i]["dat_account"]);
                            var dat_account_source = CastValue<DateTime>(DT.Rows[i]["dat_account"]);
                            //Если оплата за предыдущий месяц(до 1 числа месяца начала расчета пени) учтена в следующем месяце она не будет учтена,
                            //для этого записываем ее на 1е число месяца даты начала расчета пени
                            if (dat_account < startDatePeni)
                            {
                                dat_account = startDatePeni;
                            }
                            schemaNameTo = day.pref + "_charge_" + (dat_account.Year - 2000).ToString("00");
                            tableName = schemaNameTo + tableDelimiter + "peni_provodki_" + dat_account.Year +
                                        dat_account.Month.ToString("00") + "_" +
                                        day.nzp_wp;

                            sql = " INSERT INTO " + tableName +
                                  " (num_ls,nzp_kvar,nzp_dom,nzp_serv,nzp_supp,nzp_wp,s_prov_types_id,nzp_source,sum_prih,date_prov,date_obligation,created_on,created_by,peni_actions_id) " +
                                  " SELECT k.num_ls,k.nzp_kvar,k.nzp_dom,nzp_serv,nzp_supp," + day.nzp_wp + "," + (int)s_prov_types.Perekidki +
                                  ",nzp_to,sum_prih,dat_uchet," + Utils.EStrNull(dat_account_source.ToShortDateString()) + "," + sCurDateTime + "," + finder.nzp_user + "," +
                                  reestrId +
                                  " FROM " + schemaNameFrom + tableDelimiter + "del_supplier " + " f, " + day.pref + sDataAliasRest + "kvar k " +
                                  " WHERE NOT EXISTS (SELECT 1 FROM " + Points.Pref + sKernelAliasRest + "peni_settings s WHERE s.nzp_peni_serv=f.nzp_serv) " +
                                  " AND f.num_ls=k.num_ls  " + where.Replace("num_ls", "k.num_ls") +
                                  " AND dat_uchet in (" + day.list_dat_uchet[j] + ") AND dat_account=" +
                                  Utils.EStrNull(dat_account_source.ToShortDateString()) + " AND abs(sum_prih)>0";
                            ret = ExecSQL(conn_db, sql, false);
                            if (!ret.result)
                            {
                                if (!CheckExistTableProv(conn_db, day.nzp_wp, dat_account).result)
                                {
                                    throw new Exception(ret.text);
                                }
                                ret = ExecSQL(conn_db, sql, true);
                                if (!ret.result)
                                    throw new Exception(ret.text);

                            }

                        }

                        #endregion
                    }

                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка записи проводок при закрытии опердня: " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                    return ret;
                }
            }
            #endregion

            return ret;
        }

        /// <summary>
        /// Получить список опердней по которым необходимо записать проводки
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<OperDayForProv> GetListOperDaysForProv(IDbConnection conn_db, DateTime beginDateStart, Finder finder, out Returns ret)
        {
            var res = new List<OperDayForProv>();
            var points = Points.PointList.Where(x => x.nzp_wp == finder.nzp_wp);
            ret = Utils.InitReturns();

            // дата до которой включительно будем получать проводки
            var endDate = Points.DateOper.AddDays(-1);
            foreach (var point in points)
            {
                var day = new OperDayForProv();
                for (var beginDate = beginDateStart; beginDate <= endDate; beginDate = beginDate.AddDays(1))
                    day.list_dat_uchet.Add(beginDate);
                day.nzp_wp = point.nzp_wp;
                day.pref = point.pref;
                day.dats_uchet = String.Join(",",
                    day.list_dat_uchet.Select(x => "'" + x.ToShortDateString() + "'").ToArray());
                res.Add(day);
            }

            return res;
        }



        /// <summary>
        /// Заархивировать проводки по оплатам за день на который был произведен откат
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns ArchivProvForBackDay(IDbConnection conn_db, Finder finder, List<OperDayForProv> daysForArch, string where = "")
        {
            var ret = Utils.InitReturns();


            foreach (var point in daysForArch)
            {
                if (daysForArch.Count > 0 && where == "")
                {
                    ExecSQL(conn_db, sUpdStat + " " + point.pref + sDataAliasRest + "peni_provodki", true, 6000);
                }


                var tableNameRefs = point.pref + sDataAliasRest + "peni_provodki_refs";
                DateTime min_date = (from date in point.list_dat_uchet select date).Min();
                DateTime max_date = (from date in point.list_dat_uchet select date).Max();
                var reestrId = CreateRecordInReestr(conn_db, peni_actions_type.InsertArch, min_date, max_date, finder);

                var sqlString = "SELECT date_obligation FROM " + point.pref + sDataAliasRest +
                                "peni_provodki WHERE date_prov in (" +
                                point.dats_uchet +
                                ") and nzp_wp=" + finder.nzp_wp +
                                " AND s_prov_types_id in " +
                                " (" + (int)s_prov_types.Payment + "," + (int)s_prov_types.PaymentFromSupp + "," +
                                (int)s_prov_types.Perekidki + ")" + where +
                                " GROUP BY 1";

                var DT = ClassDBUtils.OpenSQL(sqlString, "table", null, conn_db, null, ClassDBUtils.ExecMode.Exception, 6000).resultData;
                for (int i = 0; i < DT.Rows.Count; i++)
                {
                    var archDay = CastValue<DateTime>(DT.Rows[i]["date_obligation"]);
                    var tableNameProv = point.pref + "_charge_" + (archDay.Year - 2000).ToString("00") + tableDelimiter +
                                        "peni_provodki_" + archDay.Year + archDay.Month.ToString("00") + "_" +
                                        point.nzp_wp;
                    var tableNameArch = point.pref + "_charge_" + (archDay.Year - 2000).ToString("00") + tableDelimiter +
                                        "peni_provodki_arch_" + archDay.Year + archDay.Month.ToString("00") + "_" +
                                        point.nzp_wp;

                    var sql = " INSERT INTO " + tableNameArch +
                                 " (num_ls,nzp_kvar,nzp_dom,nzp_serv,nzp_supp,nzp_wp,s_prov_types_id,nzp_source,sum_prih,rsum_tarif,sum_nedop,sum_reval," +
                              " date_prov,date_obligation,created_on,created_by, peni_actions_id) " +
                              " SELECT num_ls,nzp_kvar,nzp_dom,nzp_serv,nzp_supp,nzp_wp,s_prov_types_id,nzp_source,sum_prih,rsum_tarif,sum_nedop,sum_reval," +
                              " date_prov,date_obligation,created_on,created_by " +
                              "," + reestrId +
                              " FROM " + tableNameProv +
                              " WHERE date_obligation=" + Utils.EStrNull(archDay.ToShortDateString()) + where +
                              " AND s_prov_types_id in " +
                              " (" + (int)s_prov_types.Payment + "," + (int)s_prov_types.PaymentFromSupp + "," +
                              (int)s_prov_types.Perekidki + ")";
                    ret = ExecSQL(conn_db, sql, false);
                    if (!ret.result)
                    {
                        if (!CheckExistTableProv(conn_db, point.nzp_wp, archDay, true).result)
                        {
                            throw new Exception(ret.text);
                        }
                        else
                        {
                            ret = ExecSQL(conn_db, sql, true);
                            if (!ret.result)
                                if (!CheckExistTableProv(conn_db, point.nzp_wp, archDay).result)
                                {
                                    throw new Exception(ret.text);
                                }
                            ret = ExecSQL(conn_db, sql, true);
                            if (!ret.result)
                                return ret;
                        }
                    }
                    sql = "  WITH deleted_rows AS ( DELETE FROM " + tableNameProv +
                          " WHERE date_obligation=" + Utils.EStrNull(archDay.ToShortDateString()) +
                          " AND s_prov_types_id in " +
                          " (" + (int)s_prov_types.Payment + "," + (int)s_prov_types.PaymentFromSupp + "," +
                          (int)s_prov_types.Perekidki + ")" + where + "  RETURNING id)" +
                          " DELETE FROM " + tableNameRefs + " r WHERE" +
                          " r.date_obligation=" + Utils.EStrNull(archDay.ToShortDateString()) +
                          " AND EXISTS (SELECT 1 FROM deleted_rows d WHERE r.peni_provodki_id=d.id)" +
                          " AND r.nzp_wp=" + point.nzp_wp + "";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        throw new Exception(ret.text);
                    }
                    if (where == "")
                    {
                        ExecSQL(conn_db, sUpdStat + " " + tableNameProv, false);
                        ExecSQL(conn_db, sUpdStat + " " + tableNameArch, false);
                        ExecSQL(conn_db, sUpdStat + " " + tableNameRefs, false);
                    }

                }
            }
            return ret;
        }

        #endregion Запись проводок по оплатам

        #region Запись проводок по закрытию месяца
        public Returns InsertProvOnClosedMonths(IDbConnection conn_db, Finder finder, List<CalcMonthForProv> listMonths, string where, bool withSaldo)
        {
            Returns ret = Utils.InitReturns();

            #region  цикл по выбранным банкам
            try
            {
                foreach (var month in listMonths)
                {
                    DateTime min_date = (from date in month.listCalcMonths select date).Min();
                    DateTime max_date = (from date in month.listCalcMonths select date).Max();
                    var reestrId = CreateRecordInReestr(conn_db, peni_actions_type.InsertProvCloseMonth, min_date, max_date, finder);

                    bool firstMonth = true;
                    //цикл по закрытым месяцам
                    foreach (var calcMonth in month.listCalcMonths)
                    {
                        //последний день закрытого месяца - будем считать за дату проводки
                        var lastDayCalcMonth = new DateTime(calcMonth.Year, calcMonth.Month,
                            DateTime.DaysInMonth(calcMonth.Year, calcMonth.Month));
                        //схема откуда берем данные
                        var tableChargeFrom = month.pref + "_charge_" + (calcMonth.Year - 2000).ToString("00") +
                            tableDelimiter + "charge_" + calcMonth.Month.ToString("00");
                        var tableRevalFrom = month.pref + "_charge_" + (calcMonth.Year - 2000).ToString("00") +
                         tableDelimiter + "reval_" + calcMonth.Month.ToString("00");
                        //получаем дней до даты обязательств
                        //по умолчанию 10 дней
                        var countDays = GetCountDayToDateObligation(conn_db, month.pref, out ret);
                        //получаем дату обязательств
                        var dateObligation = lastDayCalcMonth.AddDays(countDays);
                        //дата начала расчета пени
                        var dateStartPeni = GetDateStartPeni(conn_db, new _Point { pref = month.pref, nzp_wp = month.nzp_wp }, out ret);
                        if (!ret.result)
                        {
                            throw new Exception(ret.text);
                        }
                        var sDateStartPeni = Utils.EStrNull(dateStartPeni.ToShortDateString());
                        //куда записываем
                        var tableName = month.pref + "_charge_" + (dateObligation.Year - 2000).ToString("00") +
                            tableDelimiter + "peni_provodki_" + dateObligation.Year + dateObligation.Month.ToString("00") + "_" + month.nzp_wp;
                        //создаем выборку для ускорения
                        var tempTableFrom = "t_charge_" + calcMonth.Month.ToString("00") + "_" + DateTime.Now.Ticks;

                        ExecSQL(conn_db, "DROP TABLE " + tempTableFrom, false);
                        var sql = "CREATE TEMP TABLE  " + tempTableFrom +
                                  " (nzp_kvar integer default 0" +
                                  " ,num_ls integer default 0" +
                                  " ,nzp_dom integer default 0" +
                                  " ,nzp_serv integer default 0" +
                                  " ,nzp_supp integer default 0" +
                                  " ,nzp_charge integer default 0" +
                                  " ,rsum_tarif numeric (14,7) default 0" +
                                  " ,sum_nedop numeric (14,7) default 0" +
                                  " ,sum_insaldo numeric (14,7) default 0" +
                                  " ,sum_money numeric (14,7) default 0" +
                                  " ,real_charge numeric (14,7) default 0)";
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result)
                        {
                            throw new Exception(ret.text);
                        }

                        sql = " INSERT INTO " + tempTableFrom +
                              " (nzp_kvar,num_ls,nzp_dom,nzp_serv,nzp_supp,nzp_charge,rsum_tarif,sum_nedop,sum_insaldo,sum_money,real_charge)" +
                              " SELECT ch.nzp_kvar,ch.num_ls,k.nzp_dom,ch.nzp_serv,ch.nzp_supp,ch.nzp_charge,ch.rsum_tarif,ch.sum_nedop,ch.sum_insaldo,ch.sum_money,ch.real_charge" +
                              " FROM " + tableChargeFrom + " ch, " + month.pref + sDataAliasRest + "kvar k " +
                              " WHERE nzp_serv NOT IN (1)" +
                              " AND NOT EXISTS (SELECT 1 FROM " + Points.Pref + sKernelAliasRest + "peni_settings s WHERE s.nzp_peni_serv=ch.nzp_serv)" +
                              " AND dat_charge IS NULL" +
                              " AND ch.nzp_kvar=k.nzp_kvar" + where.Replace("num_ls", "k.num_ls");
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result)
                        {
                            throw new Exception(ret.text);
                        }

                        ExecSQL(conn_db,
                            "CREATE INDEX ix1_" + tempTableFrom + " ON " + tempTableFrom + " (abs(rsum_tarif))");

                        ExecSQL(conn_db,
                            "CREATE INDEX ix2_" + tempTableFrom + " ON " + tempTableFrom + " (abs(sum_nedop))");

                        ExecSQL(conn_db,
                            "CREATE INDEX ix3_" + tempTableFrom + " ON " + tempTableFrom + " (abs(sum_insaldo-sum_money))");

                        ExecSQL(conn_db,
                            "CREATE INDEX ix4_" + tempTableFrom + " ON " + tempTableFrom + " (abs(real_charge))");

                        #region rsum_tarif - начислено без учета недопоставок
                        sql = " INSERT INTO " + tableName +
                                  " (nzp_kvar,num_ls,nzp_dom,nzp_serv,nzp_supp,nzp_wp,s_prov_types_id,nzp_source,rsum_tarif,date_prov,date_obligation,created_on,created_by,peni_actions_id) " +
                                  " SELECT nzp_kvar,num_ls,nzp_dom,nzp_serv,nzp_supp," + month.nzp_wp + "," +
                                  (int)s_prov_types.Charges +
                                  ",nzp_charge," + sNvlWord + "(rsum_tarif,0)," + Utils.EStrNull(lastDayCalcMonth.ToShortDateString()) + "," +
                                  Utils.EStrNull(dateObligation.ToShortDateString()) + "," + sCurDateTime + "," + finder.nzp_user + "," +
                                  reestrId +
                                  " FROM " + tempTableFrom + " WHERE abs(rsum_tarif)>0";

                        ret = ExecSQL(conn_db, sql, false);
                        if (!ret.result)
                        {
                            if (!CheckExistTableProv(conn_db, month.nzp_wp, calcMonth).result)
                            {
                                throw new Exception(ret.text);
                            }
                            else
                            {
                                ret = ExecSQL(conn_db, sql, true);
                                if (!ret.result)
                                    throw new Exception(ret.text);
                            }

                        }
                        #endregion

                        #region sum_nedop - сумма недопоставок
                        sql = " INSERT INTO " + tableName +
                                 " (nzp_kvar,num_ls,nzp_dom,nzp_serv,nzp_supp,nzp_wp,s_prov_types_id,nzp_source,sum_nedop,date_prov,date_obligation,created_on,created_by,peni_actions_id) " +
                                 " SELECT nzp_kvar,num_ls,nzp_dom,nzp_serv,nzp_supp," + month.nzp_wp + "," +
                                 (int)s_prov_types.Nedop +
                                 ",nzp_charge," + sNvlWord + "(sum_nedop,0)," + Utils.EStrNull(lastDayCalcMonth.ToShortDateString()) + "," +
                                 Utils.EStrNull(dateObligation.ToShortDateString()) + "," + sCurDateTime + "," + finder.nzp_user + "," +
                                 reestrId +
                                 " FROM " + tempTableFrom + " WHERE abs(sum_nedop)>0";

                        ret = ExecSQL(conn_db, sql, false);
                        if (!ret.result)
                        {
                            if (!CheckExistTableProv(conn_db, month.nzp_wp, calcMonth).result)
                            {
                                throw new Exception(ret.text);
                            }
                            else
                            {
                                ret = ExecSQL(conn_db, sql, true);
                                if (!ret.result)
                                    throw new Exception(ret.text);
                            }

                        }
                        #endregion

                        #region sum_reval - перерасчет

                        //перерасчеты пишем по новым правилам: 
                        //если положительный - пишем текущим месяцем (как обычно)
                        sql = " INSERT INTO " + tableName +
                              " (nzp_kvar,num_ls,nzp_dom,nzp_serv,nzp_supp,nzp_wp," +
                              " s_prov_types_id,nzp_source,sum_reval,date_prov,date_obligation,created_on,created_by,peni_actions_id)" +
                              " SELECT ch.nzp_kvar,k.num_ls,k.nzp_dom,ch.nzp_serv,ch.nzp_supp," + month.nzp_wp + "," +
                              (int)s_prov_types.Reval +
                              " ,ch.nzp_reval,ch.reval," + Utils.EStrNull(lastDayCalcMonth.ToShortDateString()) + "," +
                              Utils.EStrNull(dateObligation.ToShortDateString()) + "," + sCurDateTime + "," +
                              finder.nzp_user + "," + reestrId +
                              " FROM " + tableRevalFrom + " ch, " + month.pref + sDataAliasRest + "kvar k " +
                              " WHERE nzp_serv NOT IN (1)" +
                              " AND NOT EXISTS (SELECT 1 FROM " + Points.Pref + sKernelAliasRest + "peni_settings s WHERE s.nzp_peni_serv=ch.nzp_serv)" +
                              " AND reval>0" +
                              " AND ch.nzp_kvar=k.nzp_kvar" + where.Replace("num_ls", "k.num_ls");
                        ret = ExecSQL(conn_db, sql, false);
                        if (!ret.result)
                        {
                            if (!CheckExistTableProv(conn_db, month.nzp_wp, calcMonth).result)
                            {
                                throw new Exception(ret.text);
                            }
                            else
                            {
                                ret = ExecSQL(conn_db, sql, true);
                                if (!ret.result)
                                    throw new Exception(ret.text);
                            }

                        }
                        //если перерасчет отрицательный - пишем его с датой обязательства = перерасчитываемому месяцу


                        var tableRevalDates = "t_reval_dates_" + DateTime.Now.Ticks;
                        sql = " CREATE TEMP TABLE " + tableRevalDates + " AS " +
                              " SELECT ch.nzp_kvar, k.num_ls,k.nzp_dom, ch.nzp_reval, " +
                            //дата проводки (по идее это указатель месяца из которого была получена проводка)
                              Utils.EStrNull(lastDayCalcMonth.ToShortDateString()) + "::DATE as date_prov," +
                            //последний день месяца за который был проведен перерасчет + кол-во дней до даты обязательств
                              " CONCAT('1.',month_,'.',year_)::DATE + interval '1 month' - interval '1 day' + interval '1 day' * p.val_prm::int  as date_obligation" +
                              " FROM " + tableRevalFrom + " ch, " + month.pref + sDataAliasRest + "kvar k,  " +
                              month.pref + sDataAliasRest + "prm_10 p " +
                              " WHERE ch.nzp_serv NOT IN (1)" +
                              " AND NOT EXISTS (SELECT 1 FROM " + Points.Pref + sKernelAliasRest + "peni_settings s WHERE s.nzp_peni_serv=ch.nzp_serv)" +
                              " AND ch.reval<0" +
                              " AND ch.nzp_kvar=k.nzp_kvar" +
                              " AND p.nzp_prm=1375 and p.is_actual<>100" +
                            //ограничение датой начала расчета пени
                             " AND " + Utils.EStrNull((calcMonth < dateStartPeni ? dateStartPeni : calcMonth).ToShortDateString()) + "::DATE BETWEEN p.dat_s AND p.dat_po" +
                              where.Replace("num_ls", "k.num_ls");
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result)
                            throw new Exception(ret.text);

                        ret = ExecSQL(conn_db,
                            "CREATE INDEX ix0_" + tableRevalDates + " ON " + tableRevalDates + " (date_obligation)");
                        if (!ret.result)
                            throw new Exception(ret.text);
                        //ограничиваем дату обязательства (Д.О.) датой начала расчета пени (Д.Н.)
                        //если Д.О. < Д.Н., то кладем с Д.О. = Д.Н.
                        sql = "UPDATE " + tableRevalDates + " SET date_obligation=" + sDateStartPeni +
                              " WHERE date_obligation<" + sDateStartPeni;
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result)
                            throw new Exception(ret.text);

                        ret = ExecSQL(conn_db,
                            "CREATE INDEX ix1_" + tableRevalDates + " ON " + tableRevalDates + " (nzp_reval,nzp_kvar)");
                        if (!ret.result)
                            throw new Exception(ret.text);


                        var datesObligationReval = ClassDBUtils.OpenSQL("SELECT DISTINCT date_obligation FROM " + tableRevalDates, "table",
                            null, conn_db, null, ClassDBUtils.ExecMode.Exception, 6000).resultData;
                        foreach (DataRow objDateObligationReval in datesObligationReval.Rows)
                        {
                            var dateObligationReval = CastValue<DateTime>(objDateObligationReval["date_obligation"]);
                            //куда записываем
                            var tableNameReval = month.pref + "_charge_" + (dateObligationReval.Year - 2000).ToString("00") +
                                tableDelimiter + "peni_provodki_" + dateObligationReval.Year + dateObligationReval.Month.ToString("00") + "_" + month.nzp_wp;

                            sql = " INSERT INTO " + tableNameReval +
                                  " (nzp_kvar,num_ls,nzp_dom,nzp_serv,nzp_supp,nzp_wp," +
                                  " s_prov_types_id,nzp_source,sum_reval,date_prov,date_obligation,created_on,created_by,peni_actions_id)" +
                                  " SELECT ch.nzp_kvar,d.num_ls,d.nzp_dom,ch.nzp_serv,ch.nzp_supp," + month.nzp_wp + "," +
                                  (int)s_prov_types.Reval + ", ch.nzp_reval,ch.reval," +
                                  " d.date_prov, d.date_obligation," + sCurDateTime + "," +
                                  finder.nzp_user + "," + reestrId +
                                  " FROM " + tableRevalFrom + " ch, " + tableRevalDates + " d " +
                                  " WHERE  ch.nzp_reval =d.nzp_reval " +
                                  " AND ch.nzp_kvar=d.nzp_kvar" +
                                  " AND d.date_obligation=" + Utils.EStrNull(dateObligationReval.ToShortDateString());
                            ret = ExecSQL(conn_db, sql, false);
                            if (!ret.result)
                            {
                                if (!CheckExistTableProv(conn_db, month.nzp_wp, calcMonth).result)
                                {
                                    throw new Exception(ret.text);
                                }
                                else
                                {
                                    ret = ExecSQL(conn_db, sql, true);
                                    if (!ret.result)
                                        throw new Exception(ret.text);
                                }

                            }
                        }

                        #endregion

                        #region real_charge корректировки
                        sql = " INSERT INTO " + tableName +
                                 " (nzp_kvar,num_ls,nzp_dom,nzp_serv,nzp_supp,nzp_wp,s_prov_types_id,nzp_source,sum_reval,date_prov,date_obligation,created_on,created_by,peni_actions_id) " +
                                 " SELECT nzp_kvar,num_ls,nzp_dom,nzp_serv,nzp_supp," + month.nzp_wp + "," +
                                 (int)s_prov_types.RealCharge +
                                 ",nzp_charge," + sNvlWord + "(real_charge,0)," + Utils.EStrNull(lastDayCalcMonth.ToShortDateString()) + "," +
                                 Utils.EStrNull(dateObligation.ToShortDateString()) + "," + sCurDateTime + "," + finder.nzp_user + "," +
                                 reestrId +
                                 " FROM " + tempTableFrom + " WHERE  abs(real_charge)>0 ";

                        ret = ExecSQL(conn_db, sql, false);
                        if (!ret.result)
                        {
                            if (!CheckExistTableProv(conn_db, month.nzp_wp, calcMonth).result)
                            {
                                throw new Exception(ret.text);
                            }
                            else
                            {
                                ret = ExecSQL(conn_db, sql, true);
                                if (!ret.result)
                                    throw new Exception(ret.text);
                            }

                        }
                        #endregion

                        #region sum_insaldo - сумма входящего сальдо - пишем только для первого месяца
                        if (firstMonth && withSaldo)
                        {
                            //дата обязательств для такой записи - последнее число месяца за которое поступили начисления + 1 день
                            firstMonth = false;
                            sql = " INSERT INTO " + tableName +
                                     " (nzp_kvar,num_ls,nzp_dom,nzp_serv,nzp_supp,nzp_wp,s_prov_types_id,nzp_source,rsum_tarif,date_prov,date_obligation,created_on,created_by,peni_actions_id) " +
                                     " SELECT nzp_kvar,num_ls,nzp_dom,nzp_serv,nzp_supp," + month.nzp_wp + "," +
                                     (int)s_prov_types.InSaldo +
                                     ",nzp_charge," + sNvlWord + "(sum_insaldo,0)-" + sNvlWord + "(sum_money,0)," + Utils.EStrNull(lastDayCalcMonth.ToShortDateString()) + "," +
                                     Utils.EStrNull(lastDayCalcMonth.AddDays(1).ToShortDateString()) + "," + sCurDateTime + "," + finder.nzp_user + "," +
                                     reestrId +
                                     " FROM " + tempTableFrom + " WHERE abs(sum_insaldo-sum_money)>0 ";

                            ret = ExecSQL(conn_db, sql, false);
                            if (!ret.result)
                            {
                                if (!CheckExistTableProv(conn_db, month.nzp_wp, calcMonth).result)
                                {
                                    throw new Exception(ret.text);
                                }

                                ret = ExecSQL(conn_db, sql, true);
                                if (!ret.result)
                                    throw new Exception(ret.text);
                            }

                        }
                        #endregion

                        ExecSQL(conn_db, "DROP TABLE " + tableRevalDates, false);
                        ExecSQL(conn_db, "DROP TABLE " + tempTableFrom, false);
                    }



                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(
                    "Ошибка записи проводок при закрытии расчетного месяца: " +
                    (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            #endregion



            return ret;
        }

        /// <summary>
        /// Получить список расчетных месяцев по которым необходимо записать проводки
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <param name="beginDateStart"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<CalcMonthForProv> GetListCalcMonths(IDbConnection conn_db, Finder finder, DateTime beginDateStart, out Returns ret)
        {
            var res = new List<CalcMonthForProv>();

            ret = Utils.InitReturns();
            var listPoints = Points.PointList.Where(x => x.nzp_wp == finder.nzp_wp);

            var prm = new CalcMonthParams();
            prm.pref = finder.pref;
            var rec = Points.GetCalcMonth(prm);
            //текущий расчетный месяц этого банка
            var CalcMonth = new DateTime(rec.year_, rec.month_, 1);
            //потому как можем считать пени по закрытому месяцу
            //кладем начисления в закрытый месяц из месяца идущего до него
            beginDateStart = beginDateStart.AddMonths(-1);

            //кладем начисления из предпоследнего закрытого месяца в закрытый, потому что считаем пени по закрытому месяцу
            var endDate = CalcMonth.AddMonths(-1);

            foreach (var point in listPoints)
            {
                var month = new CalcMonthForProv();
                for (var beginDate = beginDateStart; beginDate <= endDate; beginDate = beginDate.AddMonths(1))
                    month.listCalcMonths.Add(beginDate);
                month.nzp_wp = point.nzp_wp;
                month.pref = point.pref;
                res.Add(month);
            }


            return res;
        }




        /// <summary>
        /// Получить число дней до даты обязательств для банка данных
        /// если параметр на банк не выставлен, то определяем значение параметра= 10 дней
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="pref"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public int GetCountDayToDateObligation(IDbConnection conn_db, string pref, out Returns ret)
        {
            ret = Utils.InitReturns();
            var res = 0;
            //текущий расчетный месяц
            var CalcDate = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1);
            var sql = "SELECT max(val_prm) FROM " + pref + sDataAliasRest +
                      "prm_10 WHERE nzp_prm=1375 and is_actual<>100 " +
                      " and " + Utils.EStrNull(CalcDate.ToShortDateString()) + " between dat_s and dat_po ";
            res = CastValue<int>(ExecScalar(conn_db, sql, out ret, true));
            return res == 0 ? 10 : res;
        }


        /// <summary>
        /// Получить число дней до даты обязательств для банка данных
        /// если параметр на банк не выставлен, то определяем значение параметра= 10 дней (Перегруженная версия для finder)
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="pref"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public int GetCountDayToDateObligation(Finder finder, out Returns ret)
        {
            var res = 0;
            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return res;
            #endregion
            //текущий расчетный месяц
            var CalcDate = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1);
            var sql = "SELECT max(val_prm) FROM " + finder.pref + sDataAliasRest +
                      "prm_10 WHERE nzp_prm=1375 and is_actual<>100 " +
                      " and " + Utils.EStrNull(CalcDate.ToShortDateString()) + " between dat_s and dat_po ";
            res = CastValue<int>(ExecScalar(conn_db, sql, out ret, true));
            return res == 0 ? 10 : res;
        }

        /// <summary>
        /// Заархивировать проводки за расчетный месяц на который был произведен откат
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="listMonths"></param>
        /// <returns></returns>
        public Returns ArchivProvForBackMonth(IDbConnection conn_db, Finder finder, List<CalcMonthForProv> listMonths, string where = "")
        {
            var ret = Utils.InitReturns();


            foreach (var point in listMonths)
            {
                DateTime min_date = (from date in point.listCalcMonths select date).Min();
                DateTime max_date = (from date in point.listCalcMonths select date).Max();
                var reestrId = CreateRecordInReestr(conn_db, peni_actions_type.InsertArch, min_date, max_date, finder);

                var countDays = GetCountDayToDateObligation(conn_db, point.pref, out ret);
                foreach (var calcMonth in point.listCalcMonths)
                {
                    //дата по которой мы проводим ахивацию - последний день расчетного месяца
                    var dateProv = new DateTime(calcMonth.Year, calcMonth.Month,
                         DateTime.DaysInMonth(calcMonth.Year, calcMonth.Month));
                    var archMonth = dateProv.AddDays(countDays);

                    var tableNameProv = point.pref + "_charge_" + (archMonth.Year - 2000).ToString("00") + tableDelimiter +
                                        "peni_provodki_" + archMonth.Year + archMonth.Month.ToString("00") + "_" +
                                        point.nzp_wp;
                    var tableNameArch = point.pref + "_charge_" + (archMonth.Year - 2000).ToString("00") + tableDelimiter +
                                        "peni_provodki_arch_" + archMonth.Year + archMonth.Month.ToString("00") + "_" +
                                        point.nzp_wp;

                    ret = ArchiveProv(conn_db, @where, tableNameArch, reestrId, tableNameProv, dateProv, point, archMonth,
                        s_prov_types.RealCharge, s_prov_types.Charges, s_prov_types.Nedop, s_prov_types.InSaldo);
                    if (!ret.result)
                    {
                        return ret;
                    }
                    //из-за неоднородного расположения проводок с отрицательными перерасчетами пришлось вводить доп.логику..


                    var datesObligationReval = ClassDBUtils.OpenSQL("SELECT DISTINCT date_obligation FROM " + point.pref + sDataAliasRest + "peni_provodki " +
                        " WHERE nzp_wp =" + point.nzp_wp + " AND s_prov_types_id=" + (int)s_prov_types.Reval + " AND date_prov=" + Utils.EStrNull(dateProv.ToShortDateString()) + where, "table",
                        null, conn_db, null, ClassDBUtils.ExecMode.Exception, 6000).resultData;
                    foreach (DataRow objDateObligationReval in datesObligationReval.Rows)
                    {
                        var archMonthReval = CastValue<DateTime>(objDateObligationReval["date_obligation"]);
                        var tableNameProvReval = point.pref + "_charge_" + (archMonthReval.Year - 2000).ToString("00") + tableDelimiter +
                                        "peni_provodki_" + archMonthReval.Year + archMonthReval.Month.ToString("00") + "_" +
                                        point.nzp_wp;
                        var tableNameArchReval = point.pref + "_charge_" + (archMonthReval.Year - 2000).ToString("00") + tableDelimiter +
                                            "peni_provodki_arch_" + archMonthReval.Year + archMonthReval.Month.ToString("00") + "_" +
                                            point.nzp_wp;
                        ret = ArchiveProv(conn_db, @where, tableNameArchReval, reestrId, tableNameProvReval, dateProv, point, archMonthReval,
                      s_prov_types.Reval);
                        if (!ret.result)
                        {
                            return ret;
                        }
                    }

                }
            }
            return ret;
        }


        private Returns ArchiveProv(IDbConnection conn_db, string @where, string tableNameArch, int reestrId,
            string tableNameProv, DateTime dateProv, CalcMonthForProv point, DateTime archMonth, params s_prov_types[] provTypes)
        {
            Returns ret;
            var sql = " INSERT INTO " + tableNameArch +
                      " (nzp_kvar,num_ls,nzp_dom,nzp_serv,nzp_supp,nzp_wp,s_prov_types_id,nzp_source,rsum_tarif,sum_nedop,sum_reval,sum_prih,date_prov," +
                      " date_obligation,created_on,created_by, peni_actions_id) " +
                      " SELECT nzp_kvar,num_ls,nzp_dom,nzp_serv,nzp_supp,nzp_wp,s_prov_types_id,nzp_source,rsum_tarif,sum_nedop,sum_reval,sum_prih,date_prov," +
                      " date_obligation,created_on,created_by " +
                      "," + reestrId +
                      " FROM " + tableNameProv +
                      " WHERE date_prov=" + Utils.EStrNull(dateProv.ToShortDateString()) + @where +
                      " AND s_prov_types_id in " +
                      " (" + string.Join(",", provTypes.Select(x => (int)x)) + ")";


            ret = ExecSQL(conn_db, sql, false);
            if (!ret.result)
            {
                if (!CheckExistTableProv(conn_db, point.nzp_wp, archMonth, true).result)
                {
                    throw new Exception(ret.text);
                }
                ret = ExecSQL(conn_db, sql, false);
                if (!ret.result)
                    if (!CheckExistTableProv(conn_db, point.nzp_wp, archMonth).result)
                    {
                        throw new Exception(ret.text);
                    }
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;
            }

            var tableNameRefs = point.pref + sDataAliasRest + "peni_provodki_refs";

            sql = " WITH deleted_rows AS (DELETE FROM " + tableNameProv +
                  " WHERE date_prov=" + Utils.EStrNull(dateProv.ToShortDateString()) +
                  " AND s_prov_types_id in " +
                  " (" + string.Join(",", provTypes.Select(x => (int)x)) + ")" + @where + " RETURNING id)" +
                  " DELETE FROM " + tableNameRefs + " r WHERE " +
                  " EXISTS (SELECT 1 FROM deleted_rows d WHERE d.id=r.peni_provodki_id)";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                throw new Exception(ret.text);
            }
            return ret;
        }


        public DateTime GetDateStartPeni(Finder finder, out Returns ret)
        {
            var res = new DateTime();
            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return res;
            #endregion
            //текущий расчетный месяц
            var prm = new CalcMonthParams();
            prm.pref = finder.pref;
            var rec = Points.GetCalcMonth(prm);
            //текущий расчетный месяц этого банка
            var CalcMonth = new DateTime(rec.year_, rec.month_, 1);
            var sqlString = "select val_prm" +
                         " from " + finder.pref + sDataAliasRest + "prm_10 where is_actual<>100  " +
                         " and nzp_prm in (99) " +
                         " and dat_s <= " + sDefaultSchema + "mdy(" + CalcMonth.Month.ToString("00") + "," +
                         System.DateTime.DaysInMonth(CalcMonth.Year, CalcMonth.Month).ToString("00") + ","
                         + CalcMonth.Year.ToString("0000") + ")" +
                         " and dat_po>= " + sDefaultSchema + "mdy(" + CalcMonth.Month + ",01," + CalcMonth.Year + ") ";
            object date = ExecScalar(conn_db, sqlString, out ret, true);
            //if (date==null || !DateTime.TryParse(date.ToString(), out res))
            //{
            //    ret.result = false;
            //    MonitorLog.WriteLog("Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + "Ошибка преобразования извлеченной даты", MonitorLog.typelog.Error, 2, 100, true);
            //}
            res = CastValue<DateTime>(date);
            if (res == DateTime.MinValue)
            {
                ret.result = false;
                res = new DateTime(CalcMonth.Year, CalcMonth.Month, 1).AddMonths(-1);
                ret.text = "\n Отсутствует значение системного параметра \"Дата начала расчета пени\".";
            }
            res = new DateTime(res.Year, res.Month, 1);
            return res;
        }
        public static DateTime MaxDate(DateTime a, DateTime b)
        {
            return a > b ? a : b;
        }
        #endregion Запись проводок при закрытии месяца
        #endregion




        #region Запись проводок по оплатам

        public Returns GetProvForDefinedDay(Finder finder, DateTime datoper_old)
        {
            var ret = GetProvForClosedOperDay(finder, datoper_old, false);
            return ret;
        }

        public Returns GetProvForClosedOperDay(Finder finder, bool Archiving = false)
        {
            var dayBack = new DateTime();
            if (Archiving)
            {
                //если был произведен откат, то архивируем текущий опердень
                dayBack = Points.DateOper;
            }
            else
            {
                //закрытый опердень
                dayBack = Points.DateOper.AddDays(-1);
            }

            var ret = GetProvForClosedOperDay(finder, dayBack, Archiving);
            return ret;
        }

        /// <summary>
        /// Записываем проводки по закрытому опердню
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="Archiving">Указывает заархивироавть проводки или записать</param>
        /// <returns></returns>
        public Returns GetProvForClosedOperDay(Finder finder, DateTime dayBack, bool Archiving)
        {
            var ret = Utils.InitReturns();
            try
            {
                IDbConnection conn_db = GetConnection(Points.GetConnByPref(Points.Pref));
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    return ret;
                }

                //определяем список опердней по которым пишем и удаляем проводки
                var listDays = GetListOperDaysForProv(conn_db, finder, dayBack, out ret);
                if (!ret.result)
                    return ret;

                //Архивируем проводки за полученный период 
                ret = ArchivProvForBackDay(conn_db, finder, listDays);
                if (!ret.result)
                {
                    return ret;
                }

                if (!Archiving)
                {

                    //записываем проводки за этот период
                    ret = InsertProvOnCloseOperDay(conn_db, finder, listDays, "");
                    if (!ret.result)
                    {
                        return ret;
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Ошибка при записи проводок по оплатам", ex);
            }

            return ret;
        }


        /// <summary>
        /// Получить список опердней по которым необходимо записать проводки
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <param name="dayBack"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<OperDayForProv> GetListOperDaysForProv(IDbConnection conn_db, Finder finder, DateTime dayBack, out Returns ret)
        {
            var res = new List<OperDayForProv>();

            var points = Points.PointList;
            if (finder.nzp_wp > 0)
                points = Points.PointList.Where(x => x.nzp_wp == finder.nzp_wp).ToList();
            ret = Utils.InitReturns();

            foreach (var point in points)
            {
                var dateStartPeni = GetDateStartPeni(conn_db, point, out ret);
                if (dateStartPeni == DateTime.MinValue)
                {
                    //если для банка не определена дата начала расчета пени, то не пишем по нему проводки
                    continue;
                }

                if (!GetParamIsNewPeni(conn_db, point, 1382))
                {
                    //если в банке не включен новый режим расчета пени, то проводки не пишем
                    continue;
                }


                var day = new OperDayForProv();
                day.list_dat_uchet.Add(dayBack);
                day.nzp_wp = point.nzp_wp;
                day.pref = point.pref;
                day.dats_uchet = String.Join(",",
                day.list_dat_uchet.Select(x => "'" + x.ToShortDateString() + "'").ToArray());
                res.Add(day);
            }

            return res;
        }



        /// <summary>
        /// проверка на существование таблицы проводок, если ее нет, то она будет автоматически создана
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="nzp_wp"></param>
        /// <param name="date_obligation">дата обязательств: для начисления =дата расчета+число дней из prm_11:nzp_prm=1375</param>
        /// <param name="archive"></param>
        /// <returns></returns>
        public Returns CheckExistTableProv(IDbConnection conn_db, int nzp_wp, DateTime date_obligation, bool archive = false)
        {
            var ret = Utils.InitReturns();

            var pref = Points.GetPref(nzp_wp);
            var table = "peni_provodki";
            if (archive)
            {
                table += "_arch";
            }

            string sql = " INSERT INTO " + pref + sDataAliasRest + table + " (nzp_serv,nzp_supp,nzp_wp,date_obligation,s_prov_types_id,peni_actions_id) " +
                         " VALUES (0,0," + nzp_wp + "," + Utils.EStrNull(date_obligation.ToShortDateString()) + "," + (int)s_prov_types.None + ",0)";
            ret = ExecSQL(conn_db, sql);
            if (!ret.result) return ret;
            var id = DBManager.GetSerialValue(conn_db);
            sql = "DELETE FROM " + pref + sDataAliasRest + table +
                " WHERE id=" + id + "AND nzp_wp=" + nzp_wp + " AND date_obligation=" +
                 Utils.EStrNull(date_obligation.ToShortDateString());
            ret = ExecSQL(conn_db, sql);
            return ret;
        }


        public bool GetParamIsNewPeni(IDbConnection conn_db, _Point point, int nzpPrm)
        {
            CalcTypes.ParamCalc prmCalc = new CalcTypes.ParamCalc();
            prmCalc.pref = point.pref;
            return GetParamIsNewPeni(conn_db, prmCalc, nzpPrm);
        }
        /// <summary>
        ///  Получить параметр действия нового режима получения пени для банка данных
        /// </summary>
        /// <returns></returns>
        private bool GetParamIsNewPeni(IDbConnection conn_db, CalcTypes.ParamCalc finder, int nzpPrm)
        {
            //текущий расчетный месяц
            var prm = new CalcMonthParams();
            prm.pref = finder.pref;
            var rec = Points.GetCalcMonth(prm);
            //текущий расчетный месяц этого банка
            var CalcMonth = new DateTime(rec.year_, rec.month_, 1);
            var tableName = finder.pref + sDataAliasRest + "prm_10";
            var ret = Utils.InitReturns();
            var sql = " Select max(val_prm) " +
                      " From " + tableName + " p " +
                      " Where p.nzp_prm =  " + nzpPrm +
                      "   AND p.is_actual = 1 " +
                      "   AND  " + Utils.EStrNull(CalcMonth.ToShortDateString()) + " BETWEEN p.dat_s AND p.dat_po";
            var res = CastValue<int>(ExecScalar(conn_db, sql, out ret, true));
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка получения параметра типа алгоритма расчета пени GetParamIsNewPeni: " +
                     ret.text, MonitorLog.typelog.Error, 1, 2, true);
            }
            return res == 2;
        }

        #endregion Запись проводок по оплатам


        #region Запись проводок по закрытию месяца

        /// <summary>
        /// Подготовка первого расчета пени по банку данных
        /// пишем проводки по начислениям и оплатам
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="Archiving">Указывает что произошел откат месяца</param>
        /// <returns></returns>
        public Returns GetProvForClosedMonth(Finder finder, bool Archiving = false)
        {
            var ret = Utils.InitReturns();
            try
            {
                using (IDbConnection conn_db = DBManager.GetConnection(Points.GetConnByPref(Points.Pref)))
                {
                    ret = DBManager.OpenDb(conn_db, true);
                    if (!ret.result)
                    {
                        return ret;
                    }

                    //определяем список месяцев по которым пишем и удаляем проводки
                    var listMonths = GetListCalcMonths(conn_db, finder, Archiving, out ret);
                    if (!ret.result)
                        return ret;

                    //архивируем проводки
                    ret = ArchivProvForBackMonth(conn_db, finder, listMonths);
                    if (!ret.result)
                    {
                        return ret;
                    }

                    if (!Archiving)
                    {
                        ret = InsertProvOnClosedMonths(conn_db, finder, listMonths, "", false);
                        if (!ret.result)
                        {
                            return ret;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Ошибка при записи проводок по начислениям", ex);
            }
            return ret;
        }


        /// <summary>
        /// Получить список расчетных месяцев по которым необходимо записать проводки
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <param name="Archiving"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<CalcMonthForProv> GetListCalcMonths(IDbConnection conn_db, Finder finder, bool Archiving, out Returns ret)
        {
            var res = new List<CalcMonthForProv>();
            ret = Utils.InitReturns();
            var listPoints = Points.PointList;
            if (finder.nzp_wp > 0)
            {
                listPoints = Points.PointList.Where(x => x.nzp_wp == finder.nzp_wp).ToList();
            }
            if (finder.pref.Length > 0)
            {
                listPoints = Points.PointList.Where(x => x.pref == finder.pref).ToList();
            }

            foreach (var point in listPoints)
            {
                var dateStartPeni = GetDateStartPeni(conn_db, point, out ret);
                if (dateStartPeni == DateTime.MinValue)
                {
                    //если для банка не определена дата начала расчета пени, то не пишем по нему проводки
                    continue;
                }

                if (!GetParamIsNewPeni(conn_db, point, 1382))
                {
                    //если в банке не включен новый режим расчета пени, то проводки не пишем
                    continue;
                }

                if (Archiving)
                {
                    var prm = new CalcMonthParams();
                    prm.pref = point.pref;
                    var rec = Points.GetCalcMonth(prm);
                    //текущий расчетный месяц этого банка
                    var CalcMonth = new DateTime(rec.year_, rec.month_, 1);
                    //архивируем проводки за месяц который заново открыли
                    var MonthForProv = CalcMonth.AddMonths(-1);
                    //-1 потому что при получении списка месяцов для архивации учитывается + N-ое число дней до даты обязательств
                    var month = new CalcMonthForProv();
                    month.listCalcMonths.Add(MonthForProv);
                    month.nzp_wp = point.nzp_wp;
                    month.pref = point.pref;
                    res.Add(month);
                }
                else
                {
                    var prm = new CalcMonthParams();
                    prm.pref = point.pref;
                    var rec = Points.GetCalcMonth(prm);
                    //текущий расчетный месяц этого банка
                    var CalcMonth = new DateTime(rec.year_, rec.month_, 1);
                    //потому как можем считать пени по закрытому месяцу
                    //кладем начисления в закрытый месяц из месяца идущего до него
                    var month = new CalcMonthForProv();
                    //month.listCalcMonths.Add(CalcMonth.AddMonths(-2));
                    month.listCalcMonths.Add(CalcMonth.AddMonths(-1)); //если включен учет проводок в тек.месяце
                    month.nzp_wp = point.nzp_wp;
                    month.pref = point.pref;
                    res.Add(month);
                }
            }

            return res;
        }




        /// <summary>
        /// Запись в реестр действия по пени
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="action"></param>
        /// <param name="dateFrom"></param>
        /// <param name="dateFrom"></param>
        /// <param name="dateTo"></param>
        /// <param name="finder"></param>
        /// <returns></returns>
        public int CreateRecordInReestr(IDbConnection conn_db, peni_actions_type action, DateTime dateFrom, DateTime dateTo, Finder finder)
        {
            var s_dateFrom = Utils.EStrNull(dateFrom.ToShortDateString());
            var s_dateTo = Utils.EStrNull(dateTo.ToShortDateString());
            Returns ret = ExecSQL(conn_db, "INSERT INTO " + Points.Pref + sDataAliasRest + "peni_actions " +
                              " (peni_actions_type_id,created_by,date_from,date_to,nzp_wp)" +
                             " VALUES (" + (int)action + "," + finder.nzp_user + "," + s_dateFrom + "," + s_dateTo + "," + finder.nzp_wp + ")");
            return GetSerialValue(conn_db);
        }

        public DateTime GetDateStartPeni(IDbConnection conn_db, _Point point, out Returns ret)
        {
            var res = new DateTime();
            //текущий расчетный месяц
            var prm = new CalcMonthParams();
            prm.pref = point.pref;
            var rec = Points.GetCalcMonth(prm);
            //текущий расчетный месяц этого банка
            var CalcMonth = new DateTime(rec.year_, rec.month_, 1);

            var sqlString = "select val_prm" +
                         " from " + point.pref + sDataAliasRest + "prm_10 where is_actual<>100  " +
                         " and nzp_prm in (99) " +
                         " and dat_s <= " + sDefaultSchema + "mdy(" + CalcMonth.Month.ToString("00") + "," +
                         DateTime.DaysInMonth(CalcMonth.Year, CalcMonth.Month).ToString("00") + ","
                         + CalcMonth.Year.ToString("0000") + ")" +
                         " and dat_po>= " + sDefaultSchema + "mdy(" + CalcMonth.Month + ",01," + CalcMonth.Year + ") ";
            res = CastValue<DateTime>(ExecScalar(conn_db, sqlString, out ret, true));

            return res;
        }


        #endregion Запись проводок по закрытию месяца
        /// <summary>
        /// Удаляет текущую пользовательскую роль
        /// </summary>
        public Returns DeleteCurrentRole(Finder finder)
        {
            Returns ret = Utils.InitReturns();
            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка в функции DeleteCurrentRole(), не выбран пользователь ", MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            string sql = "select count(*) from " + sDefaultSchema + "userp where nzp_role=" + finder.nzp_role;
            object countRow = ExecScalar(conn_db, sql, out ret, true);
            int parsedCountRow;
            if (!int.TryParse(countRow.ToString(), out parsedCountRow))
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка в функции DeleteCurrentRole(), не удалось привести object countRow к типу int ", MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            // если ни у одного из пользователей нет выбранной пользовательской роли 
            if (parsedCountRow == 0)
            {
                // удаление возможно
                List<string> sqlList = new List<string>
                {
                    "delete from " + sDefaultSchema + "roleskey where nzp_role=" + finder.nzp_role,
                    "delete from " + sDefaultSchema + "s_roles where nzp_role=" + finder.nzp_role
                };
                foreach (string sqlItem in sqlList)
                {
                    ret = ExecSQL(conn_db, sqlItem, true);
                    if (!ret.result)
                    {
                        return ret;
                    }
                }
            }
            else
            {
                //    // перечислить пользователей, используют эту пользоватльскую роль
                //    IDataReader reader = null;
                //    try
                //    {

                //        sql =
                //            "select  u.nzp_user, u.uname, u.login from " + sDefaultSchema + "userp up left outer join " +
                //            sDefaultSchema + "users u on (u.nzp_user=up.nzp_user) where up.nzp_role=" + finder.nzp_role +
                //            " order by nzp_user;";
                //        ret = ExecRead(conn_db, out reader, sql, true);
                //        if (!ret.result)
                //        {
                //            return ret;
                //        }
                //        string users = "";
                //        int i = 0;
                //        while (reader.Read())
                //        {
                //            i++;
                //            string name = reader["uname"] == DBNull.Value ? "" : reader["uname"].ToString();
                //            string login = reader["login"] == DBNull.Value ? "" : reader["login"].ToString();
                //            users += name + (login == "" ? "" : "(" + login + ")") + (i == parsedCountRow ? "" : ", ");
                //        }
                //        ret.text = "Удаление невозможно, т.к. роль еще присутствует у следующих пользователей: " + users;
                //        ret.tag = -1;
                //    }
                //    catch (Exception ex)
                //    {
                //        reader.Close();
                //        conn_db.Close();

                //        ret.result = false;
                //        ret.text = ex.Message;
                //        MonitorLog.WriteLog("Ошибка в функции DeleteCurrentRole() " + ex.Message, MonitorLog.typelog.Error, 20, 201,
                //            true);
                //    }
                //    finally
                //    {
                //        reader.Close();
                //        conn_db.Close();
                //    }
                //}
                ret.text = "Удаление невозможно, т.к. роль используется другими пользователями";
                ret.tag = -1;
            }
            return ret;
        }
    }
}
