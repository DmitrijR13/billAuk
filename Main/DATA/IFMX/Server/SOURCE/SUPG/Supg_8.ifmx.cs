using FastReport;
using SevenZip;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class Supg : DataBaseHead
    //----------------------------------------------------------------------
    {
        /// <summary>
        /// Обновить данные плановой работы
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool UpdatePlannedWork(ref SupgAct finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            try
            {
                #region Открываем соединение с базами

                con_db = GetConnection(Constants.cons_Kernel);


                ret = OpenDb(con_db, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }
                #endregion

                string sToday = Utils.GetSupgCurDate("D", "");
                // назначить номер акта, если номер акта пустой
                if (finder.is_actual != 100)
                {
                    if (finder.number.Trim() == "")
                    {
                        DbEditInterData dbEID = new DbEditInterData();
                        int number = dbEID.GetSupgSeriesProc(17, out ret);
                        dbEID.Close();
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("Ошибка назначения номера акта) : " + ret.text, MonitorLog.typelog.Error, true);
                            return false;
                        }
                        if (number != 0) finder.number = number.ToString();
                    }
                    // Назначить текущую дату, если дата акта пустая
                    if (finder._date == "") finder._date = sToday;
                }

                //экранирование символов
                finder.comment = this.SymbolsScreening(finder.comment);
                finder.reply_comment = this.SymbolsScreening(finder.reply_comment);

                //назначить номер документа, если номер документа пустой                
                if (finder.plan_number == "")
                {
                    DbEditInterData dbEID = new DbEditInterData();
                    int plan_number = dbEID.GetSupgSeriesProc(16, out ret);
                    dbEID.Close();
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка назначения номера документа) : " + ret.text, MonitorLog.typelog.Error, true);
                        return false;
                    }
                    if (plan_number != 0) finder.plan_number = plan_number.ToString();
                }
                // вернуть старую дату документа, если дата документа пустая
                string str_plan_date = "plan_date";
                if (finder.plan_date.Trim() != "")
                { 
                    str_plan_date = "'" + finder.plan_date + "'"; 
                }
                //else 
                //{
                //    str_plan_date = sToday;
                //}

                //обновление плановой работы
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" update  " + Points.Pref + "_supg . act ");
                sql.Append(" set (plan_number , plan_date,nzp_supp_plant,nzp_work_type,nzp_serv,nzp_kind,dat_s,dat_po,nzp_supp,comment, ");
                sql.Append(" is_actual, _date , number ,  reply_date ,reply_comment, tn  ) =");
                sql.Append(" ('" + finder.plan_number + "'," + str_plan_date + "," + finder.nzp_supp_plant + ", ");
                sql.Append(finder.nzp_work_type + "," + finder.nzp_serv + "," + finder.nzp_kind + ",'" + finder.dat_s + ":00:00");
                sql.Append("','" + finder.dat_po + ":00:00" + "'," + finder.nzp_supp + ",'" + finder.comment + "', ");
                sql.Append(finder.is_actual + ", " + Utils.EDateNull(finder._date) + ", '" + finder.number + "', " + Utils.EDateNull(finder.reply_date) + ", '" + finder.reply_comment + "', " + finder.tn + ")");
                sql.Append(" where nzp_act = " + finder.nzp_act);
#else
sql.Append(" update  " + Points.Pref + "_supg : act ");
                sql.Append(" set (plan_number , plan_date,nzp_supp_plant,nzp_work_type,nzp_serv,nzp_kind,dat_s,dat_po,nzp_supp,comment, ");
                sql.Append(" is_actual, _date , number ,  reply_date ,reply_comment, tn ) =");
                sql.Append(" ('" + finder.plan_number + "'," + str_plan_date + "," + finder.nzp_supp_plant + ", ");
                sql.Append(finder.nzp_work_type + "," + finder.nzp_serv + "," + finder.nzp_kind + ",'" + finder.dat_s);
                sql.Append("','" + finder.dat_po + "'," + finder.nzp_supp + ",'" + finder.comment + "', ");
                sql.Append(finder.is_actual + ", '" + finder._date + "', '" + finder.number + "', '" + finder.reply_date + "', '" + finder.reply_comment + "', " + finder.tn + ")");
                sql.Append(" where nzp_act = " + finder.nzp_act);
#endif
                if (!ExecSQL(con_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка обновления плановой работы " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }

                //удаляем все адреса
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" delete from " + Points.Pref + "_supg.  act_obj where nzp_act = " + finder.nzp_act);
#else
                sql.Append(" delete from " + Points.Pref + "_supg:  act_obj where nzp_act = " + finder.nzp_act);
#endif
                if (!ExecSQL(con_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка удаления адресов плановой работы " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }

                //цикл по адресам
                foreach (Ls ls in finder.adrList)
                {

                    //добавление плановой работы по адресу
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" insert into " + Points.Pref + "_supg.  act_obj ( nzp_act, nzp_ul, nzp_dom, nzp_kvar) ");
                    sql.Append(" values (" + finder.nzp_act + ",");
                    sql.Append(ls.nzp_ul + "," + ls.nzp_dom + "," + ls.nzp_kvar + "); ");
#else
 sql.Append(" insert into " + Points.Pref + "_supg:  act_obj ( nzp_act, nzp_ul, nzp_dom, nzp_kvar) ");
                    sql.Append(" values (" + finder.nzp_act + ",");
                    sql.Append(ls.nzp_ul + "," + ls.nzp_dom + "," + ls.nzp_kvar + "); ");
#endif

                    if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка добавления адресов плановых работ " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return false;
                    }

                }
                // очистить список 
                finder.adrList = new List<Ls>();

                return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UpdatePlannedWork : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
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
        /// Получние данных плановой работы
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns>список работ, сосотоящий из одного искомого элемента.</returns>
        public List<SupgAct> GetPlannedWork(SupgAct finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            List<SupgAct> retList = new List<SupgAct>();

            try
            {
                #region Открываем соединение с базами

                con_db = GetConnection(Constants.cons_Kernel);


                ret = OpenDb(con_db, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }
                #endregion


                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select  nzp_act, plan_number, plan_date, nzp_supp_plant, nzp_work_type, ");
                sql.Append(" nzp_serv, nzp_kind, dat_s, dat_po, nzp_supp, comment, ");
                sql.Append("  is_actual, _date, number, reply_date,reply_comment,tn  ");
                sql.Append(" from   " + Points.Pref + "_supg.  act a ");
                sql.Append(" where nzp_act = " + finder.nzp_act + " ");
#else
sql.Append(" select  nzp_act, plan_number, plan_date, nzp_supp_plant, nzp_work_type, ");
                sql.Append(" nzp_serv, nzp_kind, dat_s, dat_po, nzp_supp, comment, ");
                sql.Append("  is_actual, _date, number, reply_date,reply_comment,tn  ");
                sql.Append(" from   " + Points.Pref + "_supg:  act a ");
                sql.Append(" where nzp_act = " + finder.nzp_act + " ");
#endif

                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка получения данных плановой работы " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                if (reader != null)
                {
                    while (reader.Read())
                    {
                        SupgAct sa = new SupgAct();

                        if (reader["nzp_act"] != DBNull.Value) sa.nzp_act = Convert.ToInt32(reader["nzp_act"]);
                        if (reader["plan_number"] != DBNull.Value) sa.plan_number = Convert.ToString(reader["plan_number"]);
                        if (reader["plan_date"] != null) sa.plan_date = Convert.ToDateTime(reader["plan_date"]).ToShortDateString();
                        if (reader["nzp_supp_plant"] != DBNull.Value) sa.nzp_supp_plant = Convert.ToInt32(reader["nzp_supp_plant"]);
                        if (reader["nzp_work_type"] != DBNull.Value) sa.nzp_work_type = Convert.ToInt32(reader["nzp_work_type"]);
                        if (reader["nzp_serv"] != DBNull.Value) sa.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                        if (reader["nzp_kind"] != DBNull.Value) sa.nzp_kind = Convert.ToInt32(reader["nzp_kind"]);
                        if (reader["dat_s"] != DBNull.Value) sa.dat_s = Convert.ToDateTime(reader["dat_s"]).ToString();
                        if (reader["dat_po"] != DBNull.Value) sa.dat_po = Convert.ToDateTime(reader["dat_po"]).ToString();
                        if (reader["nzp_supp"] != DBNull.Value) sa.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                        if (reader["comment"] != DBNull.Value) sa.comment = Convert.ToString(reader["comment"]);

                        if (reader["is_actual"] != DBNull.Value) sa.is_actual = Convert.ToUInt16(reader["is_actual"]);
                        if (reader["_date"] != DBNull.Value) sa._date = Convert.ToDateTime(reader["_date"]).ToShortDateString();
                        if (reader["number"] != DBNull.Value) sa.number = Convert.ToString(reader["number"]);
                        if (reader["reply_date"] != DBNull.Value) sa.reply_date = Convert.ToDateTime(reader["reply_date"]).ToShortDateString();
                        if (reader["reply_comment"] != DBNull.Value) sa.reply_comment = Convert.ToString(reader["reply_comment"]).Trim();
                        if (reader["tn"] != DBNull.Value) sa.tn = Convert.ToInt32(reader["tn"]);

                        retList.Add(sa);
                    }
                }

                reader.Close();
                reader = null;

                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select  ac.nzp_act, ac.nzp_ul, d.nzp_dom, k.nzp_kvar, r.rajon, u.ulica, d.ndom, d.idom, k.nkvar, k.num_ls, k.ikvar, k.phone ");
                sql.Append(" from " + Points.Pref + "_supg .  act_obj ac ");
                sql.Append(" left outer join " + new DbTables(con_db).dom + " d on ac.nzp_dom = d.nzp_dom ");
                sql.Append(" left outer join " + new DbTables(con_db).kvar + " k  on ac.nzp_kvar = k.nzp_kvar, ");
                sql.Append(" " + new DbTables(con_db).ulica + " u, ");
                sql.Append(" " + new DbTables(con_db).rajon + " r ");
                sql.Append(" where  ac.nzp_ul = u.nzp_ul");
                //sql.Append(" and u.nzp_ul = d.nzp_ul ");
                sql.Append(" and u.nzp_raj = r.nzp_raj ");
                sql.Append(" and nzp_act = " + finder.nzp_act + " ");
                sql.Append(" order by u.ulica, r.rajon, d.idom, k.ikvar ");
#else
sql.Append(" select  ac.nzp_act, ac.nzp_ul, d.nzp_dom, k.nzp_kvar, r.rajon, u.ulica, d.ndom, d.idom, k.nkvar, k.num_ls, k.ikvar, k.phone ");
                sql.Append(" from " + Points.Pref + "_supg :  act_obj ac, ");
                sql.Append(" " + new DbTables(con_db).ulica + " u, ");
                sql.Append(" " + new DbTables(con_db).rajon + " r, "); 
                sql.Append(" outer(" + new DbTables(con_db).dom + " d, ");
                sql.Append(" outer( " + new DbTables(con_db).kvar + " k))");
                sql.Append(" where  ac.nzp_ul = u.nzp_ul");
                sql.Append(" and ac.nzp_dom = d.nzp_dom ");
                sql.Append(" and u.nzp_ul = d.nzp_ul ");
                sql.Append(" and u.nzp_raj = r.nzp_raj ");
                sql.Append(" and d.nzp_dom = k.nzp_dom ");
                sql.Append(" and ac.nzp_kvar = k.nzp_kvar ");
                sql.Append(" and nzp_act = " + finder.nzp_act + " ");
                sql.Append(" order by u.ulica, r.rajon, d.idom, k.ikvar ");
#endif
                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка получения адресов плановой работы " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                List<Ls> adrList = new List<Ls>();
                int r = 1;
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        Ls ls = new Ls();
                        if (reader["nzp_ul"] != DBNull.Value) ls.nzp_ul = Convert.ToInt32(reader["nzp_ul"]);
                        if (reader["nzp_dom"] != DBNull.Value) ls.nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
                        if (reader["nzp_kvar"] != DBNull.Value)
                        {
                            ls.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
                        }
                        else
                        {
                            ls.nzp_kvar = 0;
                        }

                        if (reader["num_ls"] != DBNull.Value)
                        {
                            ls.num = Convert.ToString(reader["num_ls"]);
                        }
                        else
                        {
                            ls.num = "-";
                        }
                        if (reader["phone"] != DBNull.Value) ls.phone = Convert.ToString(reader["phone"]);
                        if (reader["ikvar"] != DBNull.Value) ls.ikvar = Convert.ToInt32(reader["ikvar"]);
                        if (String.IsNullOrEmpty(ls.phone))
                        {
                            ls.phone = "-";
                        }

                        if (reader["rajon"] != DBNull.Value) ls.rajon = Convert.ToString(reader["rajon"]);
                        if (ls.rajon == "-")
                        {
                            ls.rajon = "";
                        }

                        if (reader["ulica"] != DBNull.Value) ls.ulica = Convert.ToString(reader["ulica"]);
                        if (reader["ndom"] != DBNull.Value) ls.ndom = Convert.ToString(reader["ndom"]);
                        if (reader["nkvar"] != DBNull.Value)
                        {
                            ls.nkvar = Convert.ToString(reader["nkvar"]);
                        }
                        else
                        {
                            ls.nkvar = "";
                        }

                        //Адрес 
                        ls.adr = "ул." + ls.ulica;
                        if (ls.rajon != "")
                        {
                            ls.adr += " / " + ls.rajon;
                        }
                        if (ls.ndom != "")
                        {
                            ls.adr += " д." + ls.ndom;
                        }
                        if (ls.nkvar != "")
                        {
                            ls.adr += " кв." + ls.nkvar;
                        }
                        //порядовый номер
                        ls.rows = r;

                        adrList.Add(ls);

                        r++;
                    }
                }

                if (retList.Count > 0)
                {
                    retList[0].adrList = adrList;
                }


                return retList; ;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetPlannedWorks : " + ex.Message, MonitorLog.typelog.Error, true);
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
        /// Получить справочник 
        /// </summary>
        /// <returns></returns>
        public List<Dest> GetClaimsCatalog(out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            List<Dest> retList = new List<Dest>();

            try
            {
                #region Открываем соединение с базами

                con_db = GetConnection(Constants.cons_Kernel);


                ret = OpenDb(con_db, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }
                #endregion

#if PG
                sql.Append(" select d.nzp_dest, s.service, d.dest_name, d.term_days, d.term_hours ");
                sql.Append(" from " + Points.Pref + "_supg. s_dest d, " + new DbTables(con_db).services + " s ");
                sql.Append(" where d.nzp_serv=s.nzp_serv ");
                sql.Append(" order by 2,3 ");
#else
                sql.Append(" select d.nzp_dest, s.service, d.dest_name, d.term_days, d.term_hours ");
                sql.Append(" from " + Points.Pref + "_supg: s_dest d, " + new DbTables(con_db).services + " s ");
                sql.Append(" where d.nzp_serv=s.nzp_serv ");
                sql.Append(" order by 2,3 ");
#endif
                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка получения справочника претензий " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        Dest d = new Dest();

                        if (reader["nzp_dest"] != DBNull.Value) d.nzp_dest = Convert.ToInt32(reader["nzp_dest"]);
                        if (reader["service"] != DBNull.Value) d.service = Convert.ToString(reader["service"]);
                        if (reader["dest_name"] != DBNull.Value) d.dest_name = Convert.ToString(reader["dest_name"]);
                        if (reader["term_days"] != DBNull.Value) d.term_days = Convert.ToInt32(reader["term_days"]);
                        if (reader["term_hours"] != DBNull.Value) d.term_hours = Convert.ToInt32(reader["term_hours"]);

                        retList.Add(d);
                    }
                }

                return retList;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetClaimsCatalog : " + ex.Message, MonitorLog.typelog.Error, true);
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
        /// Обновить справочник претензий
        /// </summary>
        /// <returns>результат</returns>
        public bool UpdateClaimsCatalog(Dest finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;
            StringBuilder sql = new StringBuilder();

            try
            {
                #region Открываем соединение с базами

                con_db = GetConnection(Constants.cons_Kernel);


                ret = OpenDb(con_db, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }
                #endregion

#if PG
                sql.Append(" update " + Points.Pref + "_supg. s_dest ");
                sql.Append(" set  term_days = " + finder.term_days + ", term_hours = " + finder.term_hours);
                sql.Append(" where nzp_dest = " + finder.nzp_dest);
#else
                sql.Append(" update " +  Points.Pref + "_supg: s_dest ");
                sql.Append(" set  term_days = " + finder.term_days + ", term_hours = " + finder.term_hours);
                sql.Append(" where nzp_dest = " + finder.nzp_dest);
#endif
                ExecScalar(con_db, sql.ToString(), out ret, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка получения справочника претензий " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }

                return true;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetClaimsCatalog : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                #region Закрытие соединений

                if (con_db != null)
                {
                    con_db.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        /// Обновить справочник служб
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool UpdateServiceCatalog(ServiceForwarding finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            try
            {
                #region Открываем соединение с базами

                con_db = GetConnection(Constants.cons_Kernel);


                ret = OpenDb(con_db, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }
                #endregion

                #region Проверить существование другой записи с сохраняемым наименованием службы
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select count(*) cnt from " + Points.Pref + "_supg . s_slug ");
                sql.Append(" where slug_name='" + finder.slug_name + "'");
#else
                sql.Append(" select count(*) cnt from " + Points.Pref + "_supg : s_slug ");
                sql.Append(" where slug_name='" + finder.slug_name + "'");
#endif
                if (Convert.ToInt32(finder.nzp_slug) != 0)
                {
                    sql.Append(" and nzp_slug <> " + finder.nzp_slug.ToString());
                }

                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    ret.text = "Ошибка сканирования справочника служб";
                    MonitorLog.WriteLog(ret.text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        if (reader["cnt"] != DBNull.Value)
                        {
                            if (Convert.ToInt32(reader["cnt"]) > 0)
                            {
                                ret.text = "Служба с наименованием '" + finder.slug_name.ToString().Trim() + "' уже существует в справочнике";
                                MonitorLog.WriteLog(ret.text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                                ret.result = false;
                                return false;
                            }
                        }
                    }
                }
                #endregion

                #region Сохранить данные

                int nzp = Convert.ToInt32(finder.nzp_slug);
                if (nzp == 0)
                {
                    // добавление записи справочника
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" insert into " + Points.Pref + "_supg . s_slug ");
                    sql.Append(" (slug_name, phone, dat_s, dat_po ) values ");
                    sql.Append(" ('" + finder.slug_name + "','" + finder.phone + "'," + Utils.EDateNull(finder.dat_s) + ", " + Utils.EDateNull(finder.dat_po) + ")");
#else
sql.Append(" insert into " + Points.Pref + "_supg : s_slug ");
                   sql.Append(" (nzp_slug,slug_name, phone, dat_s, dat_po ) values ");
                   sql.Append(" (0, '" + finder.slug_name + "','" + finder.phone + "','" + finder.dat_s + "', '" + finder.dat_po + "')");
#endif
                    if (!ExecSQL(con_db, sql.ToString(), true).result)
                    {
                        ret.text = "Не удалось добавить значение в справочник служб";
                        MonitorLog.WriteLog(ret.text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return false;
                    }
                }
                else
                {
                    // обновление записи справочника
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" update  " + Points.Pref + "_supg . s_slug ");
                    sql.Append(" set (slug_name, phone, dat_s, dat_po ) = ");
                    sql.Append(" ('" + finder.slug_name + "','" + finder.phone + "'," + Utils.EDateNull(finder.dat_s) + ", " + Utils.EDateNull(finder.dat_po) + ")");
                    sql.Append(" where nzp_slug = " + finder.nzp_slug);
#else
sql.Append(" update  " + Points.Pref + "_supg : s_slug ");
                    sql.Append(" set (slug_name, phone, dat_s, dat_po ) = ");
                    sql.Append(" ('" + finder.slug_name + "','" + finder.phone + "',\'" + finder.dat_s + "\', \'" + finder.dat_po + "\')");
                    sql.Append(" where nzp_slug = " + finder.nzp_slug);
#endif
                    if (!ExecSQL(con_db, sql.ToString(), true).result)
                    {
                        ret.text = "Не удалось сохранить изменения в справочнике служб";
                        MonitorLog.WriteLog("Ошибка обновлении справочника служб " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return false;
                    }
                }

                #endregion

                return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UpdateServiceCatalog : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
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
        /// возвращает Справочник Тематика из базы
        /// </summary>
        /// <returns></returns>
        public List<Sprav> GetThemesCatalog(out Returns ret)
        {
            Dictionary<int, string> TematicaLib = new Dictionary<int, string>();
            ret = Utils.InitReturns();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();
            List<Sprav> sprav = new List<Sprav>();

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
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }
                #endregion

                sql.Remove(0, sql.Length);
#if PG
                sql.Append("Select nzp_ztype, zvk_type From " + Points.Pref + "_supg. s_zvktype ");
#else
                sql.Append("Select nzp_ztype, zvk_type From " + Points.Pref + "_supg: s_zvktype ");
#endif
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
                        Sprav zap = new Sprav();
                        zap.nzp_sprav = reader.GetInt32(0).ToString();
                        zap.name_sprav = reader.GetString(1);
                        sprav.Add(zap);
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры Find_Orders : " + ex.Message, MonitorLog.typelog.Error, true);
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
            return sprav;
        }

        /// Обновить справочник классификации сообщений
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool UpdateThemesCatalog(Sprav finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            try
            {
                #region Открываем соединение с базами

                con_db = GetConnection(Constants.cons_Kernel);


                ret = OpenDb(con_db, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }
                #endregion

                #region Проверить существование другой записи с сохраняемым наименованием службы
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select count(*) cnt from " + Points.Pref + "_supg . s_zvktype ");
                sql.Append(" where zvk_type='" + finder.nzp_sprav + "'");
#else
                sql.Append(" select count(*) cnt from " + Points.Pref + "_supg : s_zvktype ");
                sql.Append(" where zvk_type='" + finder.nzp_sprav + "'");
#endif
                if (Convert.ToInt32(finder.nzp_sprav) != 0)
                {
                    sql.Append(" and nzp_ztype <> " + finder.nzp_sprav.ToString());
                }

                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    ret.text = "Ошибка сканирования справочника Классификация сообщения";
                    MonitorLog.WriteLog(ret.text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        if (reader["cnt"] != DBNull.Value)
                        {
                            if (Convert.ToInt32(reader["cnt"]) > 0)
                            {
                                ret.text = "Значение '" + finder.name_sprav.ToString().Trim() + "' уже существует в справочнике";
                                MonitorLog.WriteLog(ret.text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                                ret.result = false;
                                return false;
                            }
                        }
                    }
                }
                #endregion

                #region Сохранить данные

                int nzp = Convert.ToInt32(finder.nzp_sprav);
                if (nzp == 0)
                {
                    // добавление записи справочника
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" insert into " + Points.Pref + "_supg . s_zvktype ");
                    sql.Append(" (nzp_ztype, zvk_type ) values ");
                    sql.Append(" (0, '" + finder.name_sprav + "')");
#else
sql.Append(" insert into " + Points.Pref + "_supg : s_zvktype ");
                    sql.Append(" (nzp_ztype, zvk_type ) values ");
                    sql.Append(" (0, '" + finder.name_sprav+ "')");
#endif
                    if (!ExecSQL(con_db, sql.ToString(), true).result)
                    {
                        ret.text = "Не удалось добавить значение в справочник Классификация сообщений";
                        MonitorLog.WriteLog(ret.text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return false;
                    }
                }
                else
                {
                    // обновление записи справочника
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" update  " + Points.Pref + "_supg . s_zvktype ");
                    sql.Append(" set (zvk_type ) = ");
                    sql.Append(" (\'" + finder.name_sprav + "\')");
                    sql.Append(" where nzp_ztype = " + finder.nzp_sprav);
#else
sql.Append(" update  " + Points.Pref + "_supg : s_zvktype ");
                    sql.Append(" set (zvk_type ) = ");
                    sql.Append(" (\'" + finder.name_sprav + "\')");
                    sql.Append(" where nzp_ztype = " + finder.nzp_sprav);
#endif
                    if (!ExecSQL(con_db, sql.ToString(), true).result)
                    {
                        ret.text = "Не удалось сохранить изменения в справочнике Классификация сообщений";
                        MonitorLog.WriteLog("Ошибка обновлении справочника Классификация сообщений " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return false;
                    }
                }

                #endregion

                return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UpdateZTypeCatalog : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
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

        /// Выбрать список возможных телефонов по квартире
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public Dictionary<int, string> GetPhoneList(string pref, int nzp_kvar, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();
            Dictionary<int, string> Phones = new Dictionary<int, string>();

            try
            {
                #region Открываем соединение с базами

                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_db, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return Phones;
                }
                #endregion

                #region Выбрать список возможных телефонов из уже существующих заявок и телефон из таблицы kvar
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select distinct phone from " + new DbTables(DBManager.getServer(con_db)).kvar + " k where nzp_kvar=" + nzp_kvar.ToString() + " and phone is not null ");
                sql.Append(" union ");
                sql.Append(" select phone from " + Points.Pref + "_supg.zvk k where nzp_kvar=" + nzp_kvar.ToString() + " and phone is not null");
#else
                sql.Append(" select unique phone from " + new DbTables(DBManager.getServer(con_db)).kvar + " k where nzp_kvar=" + nzp_kvar.ToString() + " and phone is not null ");
                sql.Append(" union ");
                sql.Append(" select phone from " + Points.Pref + "_supg:zvk k where nzp_kvar=" + nzp_kvar.ToString() + " and phone is not null");
#endif
                if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                {
                    ret.text = "Ошибка выполнения выборки списка телефонов";
                    MonitorLog.WriteLog(ret.text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                }
                if (reader != null)
                {
                    int i = 0;
                    while (reader.Read())
                    {
                        if (reader["phone"] != DBNull.Value)
                        {
                            Phones.Add(i, Convert.ToString(reader["phone"]));
                            i++;
                        }
                    }
                }
                #endregion

                return Phones;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetKvarPhones : " + ex.Message, MonitorLog.typelog.Error, true);
                return Phones;
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


        /// Выбрать email для подрядчика
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool GetSuppEMail(string nzp_supp, out Returns ret)
        {
            ret = Utils.InitReturns();
            return true;
            /*
               IDbConnection con_db = null;
               IDataReader reader = null;
               StringBuilder sql = new StringBuilder();
               bool res = false;
            
               try
               {
                   #region Открываем соединение с базой

                   con_db = GetConnection(Constants.cons_Kernel);

                   ret = OpenDb(con_db, true);

                   if (!ret.result)
                   {
                       MonitorLog.WriteLog("Supg : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                       return res;
                   }
                   #endregion

                   #region e-mail

                   sql.Remove(0, sql.Length);
                   #if PG
    sql.Append("select case when s.sendemail=1 then s.email else \' \' end as email, (select p.val_prm  from " + 
                       Points.Pref + "_data. prm_10 p where p.nzp_prm = 1123) as val_prm from " +
                       Points.Pref + "_kernel.supplier s " + 
                       " where s.nzp_supp = " + nzp_supp);
   #else
    sql.Append("select case when s.sendemail=1 then s.email else \' \' end as email, (select p.val_prm  from " + 
                       Points.Pref + "_data: prm_10 p where p.nzp_prm = 1123) as val_prm from " +
                       Points.Pref + "_kernel:supplier s " + 
                       " where s.nzp_supp = " + nzp_supp);
   #endif
                   ret = ExecRead(con_db, out reader, sql.ToString(), true);

                   if (!ret.result)
                   {
                       MonitorLog.WriteLog("Ошибка выборки электронного адреса в GetSuppEMail: " + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                       con_db.Close();
                       return false;
                   }

                   try
                   {
                       if (reader != null)
                       {
                           while (reader.Read())
                           {
                               if (reader["email"] != DBNull.Value) ret.text = Convert.ToString(reader["email"]).Trim() + "&&";
                               if (reader["val_prm"] != DBNull.Value) ret.text += Convert.ToString(reader["val_prm"]).Trim();
                           }
                       }
                   }
                   catch (Exception ex)
                   {
                       MonitorLog.WriteLog("Ошибка чтения данных эл.адреса подрядчика " + ex.Message, MonitorLog.typelog.Error, true);
                       con_db.Close();
                       reader.Close();
                       return false;
                   }

                   return true;

                   #endregion
                                
               }
               catch (Exception ex)
               {
                   MonitorLog.WriteLog("Ошибка выполнения процедуры GetSuppEMail : " + ex.Message, MonitorLog.typelog.Error, true);
                   return false;
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
             */
        }



        /// <summary>
        /// отчет по реестру счетчиков по лицевым счетам
        /// </summary>
        /// <param name="nzp_user">номер пользователя</param>
        /// <param name="nzp_serv">код услуги</param>
        /// <param name="_date_to">период по</param>
        /// <returns></returns>

        //------------------------------------------------------------------------------------------------------
        public DataTable GetRegisterCounters(SupgFinder finder, out Returns ret)
        {
            if (GlobalSettings.WorkOnlyWithCentralBank)
            {
                ret = new Returns(false, "Данные для отчета недоступны, т.к. установлен режим работы с центральным банком данных", -1);
                return null;
            }

            ret = Utils.InitReturns();
            DataTable res_table = new DataTable();
            DataTable temp_table = new DataTable();
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }

            try
            {
                #region Открытие соединения с БД

                con_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(con_web, true);
                if (!ret.result) return null;

                con_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(con_db, true);
                if (!ret.result) return null;

                #endregion
#if PG
                string tXX_spls = "public." + "t" + finder.nzp_user + "_spls";
                ret = ExecSQL(con_db, "create unlogged table t_serv (nzp_serv integer)", true);
#else
                string tXX_spls = con_web.Database + "@" + DBManager.getServer(con_web) + ":" + "t" + finder.nzp_user + "_spls";
                ret = ExecSQL(con_db, "create temp table t_serv (nzp_serv integer)", true);
#endif
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка создания временной таблицы ", MonitorLog.typelog.Error, true);
                    return null;
                }

                System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                culture.NumberFormat.NumberDecimalSeparator = ".";
                culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;

                List<_Point> prefixs = new List<_Point>();
                _Point point = new _Point();
                if (finder.pref != "")
                {
                    point.pref = finder.pref;
                    prefixs.Add(point);
                }
                else
                {
                    prefixs = Points.PointList;
                }


#if PG
                foreach (_Point items in prefixs)
                {
                    if (finder.nzp_serv != 0)
                    {
                        ExecSQL(con_db, "insert into t_serv select nzp_serv from " + new DbTables(DBManager.getServer(con_db)).services + " where nzp_serv=" + finder.nzp_serv.ToString(), true);
                    }
                    else
                    {
                        ExecSQL(con_db, "insert into t_serv select nzp_serv from " + new DbTables(DBManager.getServer(con_db)).services, true);
                    }
                }
#else
foreach (_Point items in prefixs)
                {
                    if (finder.nzp_serv != 0)
                    {
                        ExecSQL(con_db, "insert into t_serv select nzp_serv from " + new DbTables(DBManager.getServer(con_db)).services + " where nzp_serv=" + finder.nzp_serv.ToString(), true);
                    }
                    else
                    {
                        ExecSQL(con_db, "insert into t_serv select nzp_serv from " + new DbTables(DBManager.getServer(con_db)).services, true);
                    }
                }
#endif

                #region создание временной таблицы
#if PG
                ExecSQL(con_db, "create unlogged table t_svod ( " +
                  " num_ls integer, " +
                  " nzp_serv integer, " +
                  " num_cnt char(20), " +
                  " nzp_cnttype integer, " +
                  " is_close integer default 0, " +
                  " type_cnt char(40), " +
                  " cnt_stage integer, " +
                  " date_first date, " +
                  " val_first numeric(14,4)," +
                  " date_last date, " +
                  " val_last numeric(14,4)," +
                  " val_average numeric(14,4), " +
                  " date_prov date, " +
                  " date_nextprov date)", true);
#else
                ExecSQL(con_db, "create temp table t_svod ( " +
                  " num_ls integer, " +
                  " nzp_serv integer, " +
                  " num_cnt char(20), " +
                  " nzp_cnttype integer, " +
                  " is_close integer default 0, " +
                  " type_cnt char(40), " +
                  " cnt_stage integer, " +
                  " date_first date, " +
                  " val_first decimal(14,4)," +
                  " date_last date, " +
                  " val_last decimal(14,4)," +
                  " val_average Decimal(14,4), " +
                  " date_prov date, " +
                  " date_nextprov date)", true);
#endif

                #endregion
                string po = Convert.ToDateTime(finder._date_to).ToString("dd.MM.yyyy");


                #region заполнение временной таблицы
#if PG
                ExecSQL(con_db, "create unlogged table t_close ( " +
                    " num_ls integer, " +
                    " nzp_cnttype integer, " +
                    " nzp_serv integer, " +
                    " num_cnt char(20)) ", true);
                foreach (_Point items in prefixs)
                {
                    ExecSQL(con_db, " insert into t_close(num_ls, nzp_cnttype, nzp_serv, num_cnt)" +
                    " Select a1.num_ls, nzp_cnttype, nzp_serv, num_cnt" +
                    " From " + items.pref + "_data.counters a1, " + tXX_spls + " f " +
                    " Where a1.is_actual=1" +
                    " and a1.dat_close is not null" +
                    " and a1.dat_uchet<='" + po + "' " +
                    " and a1.num_ls=f.num_ls ", true);
                }
                ExecSQL(con_db, "create index ixtmpp_01 on t_close(num_ls, nzp_serv, nzp_cnttype, num_cnt)", true);
                ExecSQL(con_db, "analyze t_close", true);
                foreach (_Point items in prefixs)
                {
                    sql.Remove(0, sql.Length);
                    sql.Append("insert into t_svod(num_ls, nzp_serv, num_cnt, nzp_cnttype, type_cnt, cnt_stage, " +
                   " date_first, date_last, date_prov, date_nextprov) " +
                   " select a.num_ls, a.nzp_serv, num_cnt, a.nzp_cnttype, " +
                   " name_type, cnt_stage, min(dat_uchet), max(dat_uchet) as maxdate, " +
                   " max(dat_prov), max(dat_provnext) " +
                   " from " + items.pref + "_data.counters a, " + items.pref + "_kernel.s_counttypes s, t_serv t, " +
                    new DbTables(DBManager.getServer(con_db)).dom + " d, " + new DbTables(DBManager.getServer(con_db)).kvar + " k, " + tXX_spls + " f " +
                   " where a.nzp_cnttype=s.nzp_cnttype " +
                   " and is_actual=1 and dat_uchet<='" + po +
                   "' and a.nzp_serv = t.nzp_serv " +
                   " and a.num_ls=k.num_ls and k.nzp_dom=d.nzp_dom " +
                   " and a.num_ls= f.num_ls " +
                   " group by 1,2,3,4,5,6 ");
                    ExecSQL(con_db, sql.ToString(), true);
                }
                sql.Remove(0, sql.Length);
#else
                ExecSQL(con_db, "create temp table t_close ( " +
                    " num_ls integer, " +
                    " nzp_cnttype integer, " +
                    " nzp_serv integer, " +
                    " num_cnt char(20)) ", true);
                foreach (_Point items in prefixs)
                {
                    ExecSQL(con_db, " insert into t_close(num_ls, nzp_cnttype, nzp_serv, num_cnt)" +
                    " Select a1.num_ls, nzp_cnttype, nzp_serv, num_cnt" +
                    " From " + items.pref + "_data:counters a1, " + tXX_spls + " f " +
                    " Where a1.is_actual=1" +
                    " and a1.dat_close is not null" +
                    " and a1.dat_uchet<='" + po + "' " +
                    " and a1.num_ls=f.num_ls ", true);
                }
                ExecSQL(con_db, "create index ixtmpp_01 on t_close(num_ls, nzp_serv, nzp_cnttype, num_cnt)", true);
                ExecSQL(con_db, "update statistics for table t_close", true);
                foreach (_Point items in prefixs)
                {
                    sql.Remove(0, sql.Length);
                    sql.Append("insert into t_svod(num_ls, nzp_serv, num_cnt, nzp_cnttype, type_cnt, cnt_stage, " +
                   " date_first, date_last, date_prov, date_nextprov) " +
                   " select a.num_ls, a.nzp_serv, num_cnt, a.nzp_cnttype, " +
                   " name_type, cnt_stage, min(dat_uchet), max(dat_uchet) as maxdate, " +
                   " max(dat_prov), max(dat_provnext) " +
                   " from " + items.pref + "_data:counters a, " + items.pref + "_kernel:s_counttypes s, t_serv t, " +
                    new DbTables(DBManager.getServer(con_db)).dom + " d, " + new DbTables(DBManager.getServer(con_db)).kvar + " k, " + tXX_spls + " f " +
                   " where a.nzp_cnttype=s.nzp_cnttype " +
                   " and is_actual=1 and dat_uchet<='" + po +
                   "' and a.nzp_serv = t.nzp_serv " +
                   " and a.num_ls=k.num_ls and k.nzp_dom=d.nzp_dom " +
                   " and a.num_ls= f.num_ls " +
                   " group by 1,2,3,4,5,6 ");
                    ExecSQL(con_db, sql.ToString(), true);
                }
                sql.Remove(0, sql.Length);
#endif

                #endregion

#if PG
                ExecSQL(con_db, " create index ixtmpp_02 on t_svod(num_ls, nzp_serv, nzp_cnttype, num_cnt)", false);
                ExecSQL(con_db, " analyze t_svod", false);
                ExecSQL(con_db, "update t_svod set is_close = 1 " +
                    " where  0<(select count(*) from t_close " +
                    " where t_svod.num_ls=t_close.num_ls " +
                    " and t_svod.nzp_serv=t_close.nzp_serv " +
                    " and t_svod.nzp_cnttype=t_close.nzp_cnttype " +
                    " and t_svod.num_cnt=t_close.num_cnt)", true);
                ExecSQL(con_db, " drop table t_serv", true);
                ExecSQL(con_db, " drop table t_close", true);
                ExecSQL(con_db, " create index ixtmpp_03 on t_svod (is_close)", true);
                ExecSQL(con_db, "  analyze t_svod", true);
#else
                ExecSQL(con_db, " create index ixtmpp_02 on t_svod(num_ls, nzp_serv, nzp_cnttype, num_cnt)", false);
                ExecSQL(con_db, " update statistics for table t_svod", false);
                ExecSQL(con_db, "update t_svod set is_close = 1 " +
                    " where  0<(select count(*) from t_close " +
                    " where t_svod.num_ls=t_close.num_ls " +
                    " and t_svod.nzp_serv=t_close.nzp_serv " +
                    " and t_svod.nzp_cnttype=t_close.nzp_cnttype " +
                    " and t_svod.num_cnt=t_close.num_cnt)", true);
                ExecSQL(con_db, " drop table t_serv", true);
                ExecSQL(con_db, " drop table t_close", true);
                ExecSQL(con_db, " create index ixtmpp_03 on t_svod (is_close)", true);
                ExecSQL(con_db, "  update statistics for table t_svod", true);
#endif

#if PG
                foreach (_Point items in prefixs)
                {
                    ExecSQL(con_db, "update t_svod set (val_first,val_last)=((coalesce ((select max(val_cnt) " +
                  "from " + items.pref + "_data.counters a " +
                  "Where t_svod.num_ls    = a.num_ls " +
                  "and t_svod.nzp_cnttype = a.nzp_cnttype " +
                  "and t_svod.nzp_serv    = a.nzp_serv " +
                  "and t_svod.num_cnt     = a.num_cnt " +
                  "and a.is_actual=1 and is_close = 0 " +
                  "and t_svod.date_first=a.dat_uchet ) , val_first )), " +
                  "(coalesce ((select max(val_cnt) " +
                  "from " + items.pref + "_data.counters a1 " +
                  "Where t_svod.num_ls    = a1.num_ls " +
                  "and t_svod.nzp_cnttype = a1.nzp_cnttype " +
                  "and t_svod.nzp_serv    = a1.nzp_serv " +
                  "and t_svod.num_cnt     = a1.num_cnt " +
                  "and a1.is_actual=1 and is_close = 0 " +
                  "and t_svod.date_last=a1.dat_uchet ), val_last)))", true);
                }
                ExecSQL(con_db, " update t_svod set val_average=(case when coalesce(val_first,0)>coalesce(val_last,0) " +
                    " then pow(10,coalesce(cnt_stage,0))-coalesce(val_first,0)+coalesce(val_last,0) else -coalesce(val_first,0)+coalesce(val_last,0) end) ", true);
                ExecSQL(con_db, " update t_svod set val_average=val_average/(month(date_last)+year(date_last)*12-month(date_first)-year(date_first)*12) " +
                    " where date_last is not null and month(date_last)+year(date_last)*12>month(date_first)+year(date_first)*12", true);
#else
foreach (_Point items in prefixs)
                {
                    ExecSQL(con_db, "update t_svod set (val_first,val_last)=((nvl ((select max(val_cnt) " +
                  "from " + items.pref + "_data:counters a " +
                  "Where t_svod.num_ls    = a.num_ls " +
                  "and t_svod.nzp_cnttype = a.nzp_cnttype " +
                  "and t_svod.nzp_serv    = a.nzp_serv " +
                  "and t_svod.num_cnt     = a.num_cnt " +
                  "and a.is_actual=1 and is_close = 0 " +
                  "and t_svod.date_first=a.dat_uchet ) , val_first )), " +
                  "(nvl ((select max(val_cnt) " +
                  "from " + items.pref + "_data:counters a1 " +
                  "Where t_svod.num_ls    = a1.num_ls " +
                  "and t_svod.nzp_cnttype = a1.nzp_cnttype " +
                  "and t_svod.nzp_serv    = a1.nzp_serv " +
                  "and t_svod.num_cnt     = a1.num_cnt " +
                  "and a1.is_actual=1 and is_close = 0 " +
                  "and t_svod.date_last=a1.dat_uchet ), val_last)))", true);
                }
                ExecSQL(con_db, " update t_svod set val_average=(case when nvl(val_first,0)>nvl(val_last,0) " +
                    " then pow(10,nvl(cnt_stage,0))-nvl(val_first,0)+nvl(val_last,0) else -nvl(val_first,0)+nvl(val_last,0) end) ", true);
                ExecSQL(con_db, " update t_svod set val_average=val_average/(month(date_last)+year(date_last)*12-month(date_first)-year(date_first)*12) " +
                    " where date_last is not null and month(date_last)+year(date_last)*12>month(date_first)+year(date_first)*12", true);
#endif
                foreach (_Point items in prefixs)
                {
#if PG
                    sql.Append("select a.*,service_small,ulica,ndom,nkor,nkvar,nkvar_n,ikvar,idom " +
                          " from t_svod a, " + new DbTables(con_db).services + " b, " + new DbTables(con_db).kvar + " k, " +
                          new DbTables(con_db).dom + " d," + new DbTables(con_db).ulica + " s " +
                          " where a.nzp_serv=b.nzp_serv and a.num_ls=k.num_ls and k.nzp_dom=d.nzp_dom " +
                          " and a.num_ls=k.num_ls and k.nzp_dom=d.nzp_dom " +
                          " and d.nzp_ul=s.nzp_ul and is_close = 0 " +
                          " order by service_small,ulica,idom,ndom,nkor,ikvar,nkvar,nkvar_n ");
#else
                    sql.Append("select a.*,service_small,ulica,ndom,nkor,nkvar,nkvar_n,ikvar,idom " +
                          " from t_svod a, " + new DbTables(con_db).services + " b, " + new DbTables(con_db).kvar + " k, " +
                          new DbTables(con_db).dom + " d," + new DbTables(con_db).ulica + " s " +
                          " where a.nzp_serv=b.nzp_serv and a.num_ls=k.num_ls and k.nzp_dom=d.nzp_dom " +
                          " and a.num_ls=k.num_ls and k.nzp_dom=d.nzp_dom " +
                          " and d.nzp_ul=s.nzp_ul and is_close = 0 " +
                          " order by service_small,ulica,idom,ndom,nkor,ikvar,nkvar,nkvar_n ");
#endif

                    if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки GetRegisterCounters " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return null;
                    }
                    sql.Remove(0, sql.Length);
                    try
                    {
                        if (reader != null)
                        {
                            res_table.Load(reader, LoadOption.PreserveChanges);
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка при записи данных в DataTable в GetRegisterCounters " + ex.Message, MonitorLog.typelog.Error, true);
                        reader.Close();
                        return null;
                    }
                }



                return res_table;
            }

            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры  GetRegisterCounters : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                ExecSQL(con_db, "drop table t_svod", true);
                #region Закрытие соединений

                if (con_web != null)
                {
                    con_web.Close();
                }

                if (con_db != null)
                {
                    con_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                #endregion
            }
        }


        /// <summary>
        /// Отчет по сальдовой оборотной ведомости по услугам
        /// </summary>
        /// <param name="nzp_user">номер пользователя</param>
        /// <param name="nzp_serv">код услуги</param>
        /// <param name="nzp_supp">код поставщика</param>
        /// <param name="nzp_area">код балансодержателя</param>
        /// <param name="nzp_geu">код участка</param>
        /// <param name="po_date">период по</param>
        /// <param name="s_date">период до</param>
        /// <returns></returns>
        public DataTable SaldoStatmentServices(SupgFinder finder, int supp, out Returns ret)
        {
            if (GlobalSettings.WorkOnlyWithCentralBank)
            {
                ret = new Returns(false, "Данные для отчета недоступны, т.к. установлен режим работы с центральным банком данных", -1);
                return null;
            }

            ret = Utils.InitReturns();
            IDataReader reader = null;
            IDbConnection con_web = null;
            IDbConnection con_db = null;
            DataTable res_table = new DataTable();
            StringBuilder sql = new StringBuilder();
            List<_Point> prefixs = new List<_Point>();
            string table = "t_" + System.DateTime.Now.Second.ToString();
            _Point point = new _Point();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }
            if (finder.pref != "")
            {
                point.pref = finder.pref;
                prefixs.Add(point);
            }
            else
            {
                prefixs = Points.PointList;
            }
            try
            {

                con_web = GetConnection(Constants.cons_Webdata);
                con_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(con_web, true);
                if (ret.result)
                {
                    ret = OpenDb(con_db, true);
                }
                if (!ret.result)
                {
                    MonitorLog.WriteLog("ExcelReport : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }

                DbTables tables = new DbTables(con_db);

                #region Определение кол-ва выбранных пользователем записей
                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" select count(*)  as co from t" + finder.nzp_user + "_spls ");
                string tXX_spls = "public." + "t" + finder.nzp_user + "_spls";
#else
                sql.Append(" select count(*)  as co from t" + finder.nzp_user + "_spls ");
                string tXX_spls = con_web.Database + "@" + DBManager.getServer(con_web) + ":" + "t" + finder.nzp_user + "_spls";
#endif
                if (!ExecRead(con_web, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    con_web.Close();
                    con_db.Close();
                    sql.Remove(0, sql.Length);
                    ret.result = false;
                    return null;
                }
                int count_all = 0;
                if (reader.Read())
                {
                    count_all = System.Convert.ToInt32(reader["co"]);
                }
                reader = null;

                #endregion

                #region Создание временной таблицы
                try
                {
#if PG
                    sql.Remove(0, sql.Length);
                    sql.Append("Create unlogged table " + table +
                                 " (" +
                                  "nzp_serv integer," +
                                  "nzp_supp integer," +
                                  "nzp_area integer," +
                                  "nzp_geu integer," +
                                  " s1 numeric(14,2), " +
                                  " s2 numeric(14,2), " +
                                  " s3 numeric(14,2), " +
                                  " s4 numeric(14,2), " +
                                  " s5 numeric(14,2), " +
                                  " s6 numeric(14,2), " +
                                  " s7 numeric(14,2), " +
                                  " s71 numeric(14,2), " +
                                  " s8 numeric(14,2), " +
                                  " s9 numeric(14,2), " +
                                  " s10 numeric(14,2)) "
                               );
#else
sql.Remove(0, sql.Length);
                    sql.Append("Create temp table " + table +
                                 " (" +
                                  "nzp_serv integer," +
                                  "nzp_supp integer," +
                                  "nzp_area integer," +
                                  "nzp_geu integer," +
                                  " s1 Decimal(14,2), " +
                                  " s2 Decimal(14,2), " +
                                  " s3 Decimal(14,2), " +
                                  " s4 Decimal(14,2), " +
                                  " s5 Decimal(14,2), " +
                                  " s6 Decimal(14,2), " +
                                  " s7 Decimal(14,2), " +
                                  " s71 Decimal(14,2), " +
                                  " s8 Decimal(14,2), " +
                                  " s9 Decimal(14,2), " +
                                  " s10 Decimal(14,2)) with no log "
                               );
#endif

                    ret = ExecSQL(con_db, sql.ToString(), true);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при создании таблицы supg" + ex.Message, MonitorLog.typelog.Error, true);
                    return null;
                }

                //проверка создания
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка создания временной таблицы " + ret.text, MonitorLog.typelog.Error, true);
                    return null;
                }

                string po = Convert.ToDateTime(finder._date_to).ToString("yyyy-MM-dd HH:mm:ss");
                string po_fact_year = Convert.ToDateTime(finder._date_to).ToString("yy");
                string po_fact_month = Convert.ToDateTime(finder._date_to).ToString("MM");


                if (!ExecRead(con_web, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки SaldoStatmentServices" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
                culture.NumberFormat.NumberDecimalSeparator = ".";
                culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;

                try
                {
                    if (reader != null)
                    {
                        res_table.Load(reader, LoadOption.PreserveChanges);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка при записи данных в DataTable в SaldoStatmentServices " + ex.Message, MonitorLog.typelog.Error, true);
                    reader.Close();
                    return null;
                }
                #endregion

                #region заполнение временной таблицы

#if PG
                foreach (_Point items in prefixs)
                {
                    sql.Remove(0, sql.Length);
                    ExecSQL(con_db, "SELECT lb.nzp_bl, lb.yearr , sb.dbname, sb.dbserver  FROM " + tables.kvar + " k, " +
                        items.pref + "_data.lsbase lb, " + items.pref + "_kernel.s_baselist sb" +
                   " WHERE lb.num_ls=k.num_ls and lb.nzp_bl=sb.nzp_bl" +
                   " AND lb.yearr=" + po_fact_year + "   GROUP BY 1,2,3,4", true);
                    sql.Remove(0, sql.Length);
                }
#else
                foreach (_Point items in prefixs)
                {
                    sql.Remove(0, sql.Length);
                    ExecSQL(con_db, "SELECT lb.nzp_bl, lb.yearr , sb.dbname, sb.dbserver  FROM " + tables.kvar +" k, " + 
                        items.pref + "_data:lsbase lb, " + items.pref + "_kernel:s_baselist sb" +
                   " WHERE lb.num_ls=k.num_ls and lb.nzp_bl=sb.nzp_bl" +
                   " AND lb.yearr=" + po_fact_year + "   GROUP BY 1,2,3,4", true);
                    sql.Remove(0, sql.Length);
                }
#endif

                foreach (_Point items in prefixs)
                {
                    try
                    {
#if PG
                        if (reader != null)
                        {
                            sql.Append(
                            " insert into " + table + " (nzp_serv,nzp_supp,nzp_area,nzp_geu,s1,s2,s3,s4,s5,s6,s7,s71,s8,s9,s10) select " +
                            " a.nzp_serv,a.nzp_supp,b.nzp_area,b.nzp_geu, " +
                            " sum(CASE WHEN a.sum_insaldo<0 then a.sum_insaldo else 0 end) as s1, " +
                            " sum(CASE WHEN a.sum_insaldo>0 then a.sum_insaldo else 0 end) as s2, " +
                            " sum(a.sum_insaldo) as s3, " +
                            " sum(a.real_charge+ a.reval) as s4, " +
                            " sum(a.sum_real) as s5, " +
                            " sum(a.money_to) as s6, " +
                            " sum(a.money_from) as s7, " +
                            " sum(a.money_del) as s71, " +
                            " sum(CASE WHEN a.sum_outsaldo<0 then a.sum_outsaldo else 0 end) as s8, " +
                            " sum(CASE WHEN a.sum_outsaldo>0 then a.sum_outsaldo else 0 end) as s9, " +
                            " sum(a.sum_outsaldo) as s10 " +
                            " from " + items.pref + "_charge_" + po_fact_year + ".charge_" + po_fact_month + " a, " + tables.kvar + " b , " + tXX_spls + " s " +
                            " where a.dat_charge is null and " +
                            " a.nzp_kvar=b.nzp_kvar and a.nzp_serv>1 " + " and b.num_ls=s.num_ls ");
                            if (supp > 0) sql.Append(" and a.nzp_supp=" + supp.ToString());
                            sql.Append(" group by 1,2,3,4 ");
                            ExecSQL(con_db, sql.ToString(), true);
                            sql.Remove(0, sql.Length);
                        }
#else
if (reader != null)
                        {
                            sql.Append(
                            " insert into " + table + " (nzp_serv,nzp_supp,nzp_area,nzp_geu,s1,s2,s3,s4,s5,s6,s7,s71,s8,s9,s10) select " +
                            " a.nzp_serv,a.nzp_supp,b.nzp_area,b.nzp_geu, " +
                            " sum(CASE WHEN a.sum_insaldo<0 then a.sum_insaldo else 0 end) as s1, " +
                            " sum(CASE WHEN a.sum_insaldo>0 then a.sum_insaldo else 0 end) as s2, " +
                            " sum(a.sum_insaldo) as s3, " +
                            " sum(a.real_charge+ a.reval) as s4, " +
                            " sum(a.sum_real) as s5, " +
                            " sum(a.money_to) as s6, " +
                            " sum(a.money_from) as s7, " +
                            " sum(a.money_del) as s71, " +
                            " sum(CASE WHEN a.sum_outsaldo<0 then a.sum_outsaldo else 0 end) as s8, " +
                            " sum(CASE WHEN a.sum_outsaldo>0 then a.sum_outsaldo else 0 end) as s9, " +
                            " sum(a.sum_outsaldo) as s10 " +
                            " from " + items.pref + "_charge_" + po_fact_year + ":charge_" + po_fact_month + " a, " + tables.kvar + " b , " + tXX_spls + " s " +
                            " where a.dat_charge is null and " +
                            " a.nzp_kvar=b.nzp_kvar and a.nzp_serv>1 " + " and b.num_ls=s.num_ls ");
                            if (supp > 0) sql.Append(" and a.nzp_supp=" + supp.ToString());
                            sql.Append(" group by 1,2,3,4 ");
                            ExecSQL(con_db, sql.ToString(), true);
                            sql.Remove(0, sql.Length);
                        }
#endif
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка при записи данных в DataTable в SaldoStatmentServices " + ex.Message, MonitorLog.typelog.Error, true);
                        reader.Close();
                        return null;
                    }
                }

                foreach (_Point items in prefixs)
                {

                    int i = 0;
#if PG
                    sql.Append("select d.service_small,sum(s1) as s1,sum(s2) as s2,sum(s3) as s3, " +
                                                   "sum(s4) as s4,sum(s5) as s5,sum(s6) as s6,sum(s7) as s7,sum(s71) as s71,sum(s8) as s8, " +
                                                   "sum(s9) as s9,sum(s10) as s10 ");
#else
sql.Append("select d.service_small,sum(s1) as s1,sum(s2) as s2,sum(s3) as s3, " +
                               "sum(s4) as s4,sum(s5) as s5,sum(s6) as s6,sum(s7) as s7,sum(s71) as s71,sum(s8) as s8, " +
                               "sum(s9) as s9,sum(s10) as s10 ");
#endif
                    if (supp != -1)
                    {
                        sql.Append(" , c.name_supp ");
                    }
                    if (finder.nzp_area != 0)
                    {
                        sql.Append(" ,e.area ");
                    }
                    if (finder.nzp_geu != 0)
                    {
                        sql.Append(" ,a.geu");
                    }

                    sql.Append(" from " + table + " b, " + tables.services + " d  ");
                    if (finder.nzp_geu != 0)
                    {
                        sql.Append(", " + tables.geu + " a ");
                    }
                    if (supp != -1)
                    {
                        sql.Append(", " + tables.supplier + " c ");
                    }
                    if (finder.nzp_area != 0)
                    {
                        sql.Append(", " + tables.area + " e  ");
                    }
                    sql.Append(" where b.nzp_serv=d.nzp_serv  ");
                    if (supp != -1)
                    {
                        sql.Append(" and c.nzp_supp=" + supp.ToString());
                        sql.Append(" and b.nzp_supp=c.nzp_supp ");
                    }
                    if (finder.nzp_area != 0)
                    {
                        sql.Append(" and e.nzp_area=" + finder.nzp_area.ToString());
                        sql.Append(" and b.nzp_area=e.nzp_area ");
                    }
                    if (finder.nzp_geu != 0)
                    {
                        sql.Append(" and a.nzp_geu=" + finder.nzp_geu.ToString());
                        sql.Append(" and b.nzp_geu=a.nzp_geu ");
                    }
                    sql.Append(" group by 1");
                    if (supp != -1)
                    {
                        sql.Append(" ," + 13);
                        i++;
                    }
                    if (finder.nzp_area != 0)
                    {
                        int k = 13 + i;
                        sql.Append(" ," + k);
                        i++;
                    }
                    if (finder.nzp_geu != 0)
                    {
                        int k = 13 + i;
                        sql.Append(" ," + k);
                    }

                    sql.Append(" order by 1 ");

                    reader = null;
                    if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("Ошибка выборки SaldoStatmentServices" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.result = false;
                        return null;
                    }

                    try
                    {
                        if (reader != null)
                        {
                            res_table.Load(reader, LoadOption.PreserveChanges);
                        }
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка при записи данных в DataTable в SaldoStatmentServices " + ex.Message, MonitorLog.typelog.Error, true);
                        reader.Close();
                        return null;
                    }

                    sql.Remove(0, sql.Length);
                    return res_table;
                }
                #endregion

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры SaldoStatmentServices : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений
#if PG
                ExecSQL(con_db, "drop table " + table, true);
                ExecSQL(con_db, " Drop table " + table + "; ", false);
#else
                ExecSQL(con_db, "drop table " + table, true);
                ExecSQL(con_db, " Drop table " + table + "; ", false);
#endif
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
            return res_table;
        }
    }
}
