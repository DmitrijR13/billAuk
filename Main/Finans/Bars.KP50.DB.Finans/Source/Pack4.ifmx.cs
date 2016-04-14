using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;

using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;


namespace STCLINE.KP50.DataBase
{
    public partial class DbPack : DbPackClient
    {
        //--------------------------------------------------------------------------
        public Returns DeleteListPackLs(List<Pack_ls> finder)
        {
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            int i = 0;
            foreach (Pack_ls pls in finder)
            {
                ret = DeleteFinancePackLs(pls, conn_db);
                if (ret.result) i++;
            }
            conn_db.Close();
            if (i == 0) return ret;
            else return new Returns(true, "Удалено оплат " + i.ToString() + " из " + finder.Count, -1);
        }

        //--------------------------------------------------------------------------
        public Returns DeleteFinancePackLs(Pack_ls finder)
        {
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            ret = DeleteFinancePackLs(finder, conn_db);
            conn_db.Close();
            return ret;
        }

        /// <summary>
        /// удаление квитанцию об оплате
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns DeleteFinancePackLs(Pack_ls finder, IDbConnection conn_db)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                return new Returns(false, "Пользователь не определен", -1);
            }
            if (finder.nzp_pack_ls < 1)
            {
                return new Returns(false, "Не задана квитанция об оплате", -1);
            }
            if (finder.year_ < 1)
            {
                return new Returns(false, "Год не задан", -1);
            }
            #endregion
            Returns ret;
            //получить информацию о квитанции
            List<Pack_ls> list = LoadListFinancePackLs(finder, out ret, conn_db);

            if (!ret.result) return ret;

            if (list == null || list.Count == 0) return new Returns(false, "Квитанция об оплате не найдена", -1);

            Pack_ls packLs = list[0];

            //Отменить распределение, если квитанция распределена
            if (packLs.dat_uchet != "")
            {
                ret = DistributeOrCancelPack(packLs, Pack_ls.OperationsWithoutGetting.CancelDistribution, false);
            }

            if (!ret.result) return ret;

            var pls = new Pack_ls { nzp_pack_ls = packLs.nzp_pack_ls, year_ = finder.year_ };
            ret = CheckPackLsToDeleting(pls);
            if (!ret.result)
            {
                return ret;
            }

#if PG
            string table = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ".pack_ls";
            string table_pack = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ".pack";
            string table_pack_log = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ".pack_log";
            string table_gil_sums = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ".gil_sums";
            string table_pu_vals = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ".pu_vals";
#else
            string table = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ":pack_ls";
            string table_pack = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ":pack";
            string table_pack_log = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ":pack_log";
            string table_gil_sums = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ":gil_sums";
#endif

            if (!TempTableInWebCashe(conn_db, table)) return new Returns(false, "Данные не найдены", -1);

            IDbTransaction transaction;
            try { transaction = conn_db.BeginTransaction(); }
            catch { transaction = null; }

            string sql = "delete from " + table_gil_sums + " where nzp_pack_ls = " + finder.nzp_pack_ls;
            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                return ret;
            }

            sql = "delete from " + table_pu_vals + " where nzp_pack_ls = " + finder.nzp_pack_ls;
            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                return ret;
            }

            //удалить квитанцию
            sql = "delete from " + table + " where nzp_pack_ls = " + finder.nzp_pack_ls;
            ret = ExecSQL(conn_db, transaction, sql, true);
            if (ret.result)
            {
                //обновить информацию в пачке оплат
#if PG
                sql = "update " + table_pack +
                    " set count_kv=(select count(*) from " + table + " a where a.nzp_pack = " + table_pack + ".nzp_pack)," +
                    " sum_pack=(select sum(g_sum_ls) from " + table + " b where b.nzp_pack = " + table_pack + ".nzp_pack)" +
                    " where " + table_pack + ".nzp_pack = " + packLs.nzp_pack;
#else
                sql = "update " + table_pack +
                                    " set (count_kv, sum_pack) = ((select count(*), sum(g_sum_ls) from " + table + " a where a.nzp_pack = " + table_pack + ".nzp_pack))" +
                                    " where nzp_pack = " + packLs.nzp_pack;
#endif
                ret = ExecSQL(conn_db, transaction, sql, true);
                if (ret.result)
                {
                    IDataReader reader;
                    sql = "select par_pack from " + table_pack + " where nzp_pack = " + packLs.nzp_pack;
                    ret = ExecRead(conn_db, transaction, out reader, sql, true);
                    if (ret.result)
                    {
                        int par_pack = 0;
                        if (reader.Read()) if (reader["par_pack"] != DBNull.Value) par_pack = Convert.ToInt32(reader["par_pack"]);
                        if (par_pack > 0)
                        {
#if PG
                            sql = "update " + table_pack +
                                " set count_kv = (select count(*) from " + table + " a where a.nzp_pack = " + packLs.nzp_pack + "), " +
                                " sum_pack = (select sum(g_sum_ls) from " + table + " a where a.nzp_pack = " + packLs.nzp_pack + ") " +
                                " where nzp_pack = " + par_pack;
#else
                            sql = "update " + table_pack +
                                                               " set (count_kv, sum_pack) = ((select count(*), sum(g_sum_ls) from " + table + " a where a.nzp_pack = " + packLs.nzp_pack + "))" +
                                                               " where nzp_pack = " + par_pack;
#endif
                            ret = ExecSQL(conn_db, transaction, sql, true);
                        }
                        reader.Close();
                    }

                    //записать в сообщения с пачкой
#if PG
                    sql = " Insert into " + table_pack_log + " (nzp_pack,nzp_pack_ls,dat_oper,dat_log,txt_log,tip_log) " +
                                           " values (" + packLs.nzp_pack + ",0, '" + Points.DateOper.ToShortDateString() + "', now(), " +
                                           "'удалена квитанция об оплате № " + packLs.info_num + ", сумма " + packLs.g_sum_ls + " руб.', 0)";
#else
                    sql = " Insert into " + table_pack_log + " (nzp_pack,nzp_pack_ls,dat_oper,dat_log,txt_log,tip_log) " +
                                           " values (" + packLs.nzp_pack + ",0, '" + Points.DateOper.ToShortDateString() + "', current, " +
                                           "'удалена квитанция об оплате № " + packLs.info_num + ", сумма " + packLs.g_sum_ls + " руб.', 0)";
#endif
                    ret = ExecSQL(conn_db, transaction, sql, true);

                    //удалить пачку без оплат
                    if (finder.page != Pages.AddPackLs)
                    {
                        sql = "delete from " + table_pack + " where nzp_pack = " + packLs.nzp_pack + " and count_kv = 0";
                        ret = ExecSQL(conn_db, transaction, sql, true);

                        sql = "select nzp_pack from " + table_pack + " where nzp_pack = " + packLs.nzp_pack;

                        Returns ret2;
                        ret2 = ExecRead(conn_db, transaction, out reader, sql, true);
                        if (ret2.result)
                        {
                            if (!reader.Read()) ret.tag = 101; //пачка удалена
                            reader.Close();
                            reader.Dispose();
                        }
                    }

                    #region Добавление в sys_events события 'Удаление оплаты'
                    DbAdmin.InsertSysEvent(new SysEvents()
                    {
                        pref = Points.Pref,
                        bank = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00"),
                        nzp_user = finder.nzp_user,
                        nzp_dict = 6487,
                        nzp_obj = finder.nzp_pack_ls,
                        note = "Оплата была успешно удалена"
                    }, transaction, conn_db);
                    #endregion
                }
            }

            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
            }
            else
            {
                if (transaction != null) transaction.Commit();
            }

            return ret;
        }

        //--------------------------------------------------------------------------
        public Returns GetNextNumPackForOverPay(PackFinder finder)
        {
            if (finder.nzp_user <= 0) return new Returns(false, "Пользователь не определен", -1);
            if (finder.year_ <= 0) return new Returns(false, "Год не задан", -1);

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

#if PG
            string table_pack = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ".pack";
#else
            string table_pack = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ":pack";
#endif

            IDataReader reader;

#if PG
            string sql = "select max(" +
                "public.sortnum(num_pack))+1 as num from " + table_pack + " where file_name='Перенос переплат'";
#else
            string sql = "select max(" + Points.Pref + "_data" + ":sortnum(num_pack))+1 as num from " + table_pack + " where file_name='Перенос переплат'";
#endif
            ret = ExecRead(conn_db, out reader, sql, true);
            if (ret.result)
            {
                if (reader.Read()) if (reader["num"] != DBNull.Value) ret.tag = Convert.ToInt32(reader["num"]);
            }
            reader.Close();
            conn_db.Close();
            return ret;
        }

        //--------------------------------------------------------------------------

        private Returns SavePackMain(PackFinder finder, IDbConnection conn_db, IDbTransaction transaction)
        {
            Returns ret;
#if PG
            string table_pack = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ".pack";
            string table_pack_ls = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ".pack_ls";
#else
            string table_pack = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ":pack";
            string table_pack_ls = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ":pack_ls";
#endif
            if (!TempTableInWebCashe(conn_db, transaction, table_pack)) return new Returns(false, "Данные не найдены", -1);
            string sql = "";
            DbTables tables = new DbTables(DBManager.getServer(conn_db));
            MyDataReader reader;

            int nzp_supp = 0;
            int nzp_payer = 0;
            if (finder.nzp_supp > 0) nzp_supp = finder.nzp_supp;
            if (finder.nzp_payer_contragent > 0) nzp_payer = finder.nzp_payer_contragent;
            //{
            //    sql = "select nzp_supp from " + tables.payer + " where nzp_payer = " + finder.nzp_payer2;
            //    ret = ExecRead(conn_db, transaction, out reader, sql, true);
            //    if (!ret.result) return ret;
            //    if (reader.Read()) if (reader["nzp_supp"] != DBNull.Value) nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
            //    reader.Close();
            //}

            int nzpUser = finder.nzp_user;

            /*#region определение локального пользователя
            DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, transaction, finder, out ret);
            db.Close();
            if (!ret.result) return ret;
            #endregion*/

            if (finder.nzp_pack > 0)
            {
                var gilSums = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + tableDelimiter + "gil_sums";

                sql = "select nzp_pack_ls, k.pref, k.num_ls, pls.kod_sum, p.pack_type, p.nzp_supp , p.nzp_payer, payer, name_supp from " +
                         Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ".pack_ls pls" +
                         " left outer join " + Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ".pack p on p.nzp_pack = pls.nzp_pack " +
                         " left outer join " + Points.Pref + "_kernel" + tableDelimiter + "s_payer sp on sp.nzp_payer = p.nzp_payer " +
                         " left outer join " + Points.Pref + "_kernel" + tableDelimiter + "supplier supp on supp.nzp_supp = p.nzp_supp " +
                          ", " +
                         Points.Pref + "_data.kvar k " +
                         " where pls.nzp_pack = " + finder.nzp_pack + " and k.num_ls = pls.num_ls";


                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) return ret;
                var sb = new StringBuilder();
                while (reader.Read())
                {
                    Pack_ls pls = new Pack_ls();
                    if (reader["kod_sum"] != DBNull.Value) pls.kod_sum = Convert.ToInt32(reader["kod_sum"]);
                    if (reader["nzp_supp"] != DBNull.Value) pls.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["nzp_payer"] != DBNull.Value) pls.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    if (reader["nzp_pack_ls"] != DBNull.Value) pls.nzp_pack_ls = Convert.ToInt32(reader["nzp_pack_ls"]);
                    if (reader["pack_type"] != DBNull.Value) pls.pack_type = Convert.ToInt32(reader["pack_type"]);
                    if (reader["payer"] != DBNull.Value) pls.payer = Convert.ToString(reader["payer"]).Trim();
                    if (reader["name_supp"] != DBNull.Value) pls.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                    //проверка
                    if (pls.pack_type == 20)
                    {
                        if (pls.nzp_supp > 0 && pls.kod_sum == 50)
                        {
                            sb = new StringBuilder();
                            sb.AppendFormat("select count(*) from {0} where nzp_pack_ls = {1} and nzp_supp <> {2}",
                                gilSums, pls.nzp_pack_ls, pls.nzp_supp);

                            var obj = ExecScalar(conn_db, sb.ToString(), out ret, true);
                            if (!ret.result) return ret;

                            int cnt;
                            Int32.TryParse(obj.ToString(), out cnt);
                            if (cnt > 0) { return new Returns(false, "Нельзя изменить договор, т.к. есть уточненные суммы на другой договор", -121); }
                        }
                        else if (pls.nzp_payer > 0 && pls.kod_sum == 49)
                        {
                            sb = new StringBuilder();
                            sb.AppendFormat("select count(*) from {0} where nzp_pack_ls = {1} and sum_oplat<>0 and nzp_supp not in (select nzp_supp from {3} where nzp_payer_princip = {2})",
                                gilSums, pls.nzp_pack_ls, pls.nzp_payer, Points.Pref + "_kernel" + tableDelimiter + "supplier");

                            var obj = ExecScalar(conn_db, sb.ToString(), out ret, true);
                            if (!ret.result) return ret;

                            int cnt;
                            Int32.TryParse(obj.ToString(), out cnt);
                            if (cnt > 0) return new Returns(false, "Нельзя изменить принципала, т.к. есть уточненные суммы на другого принципала", -121);

                        }

                    }
                }
                reader.Close();

                bool changed = false; //изменение по типу пачки, контрагенту, договору
                #region Добавление в sys_events события 'Изменение пачки оплат'
                try
                {

                    if (finder.dat_uchet.Trim().Length != 0 || finder.dat_pack.Trim().Length != 0 || finder.snum_pack.Trim().Length != 0)
                    {
                        //получение старых значений
                        var changed_fields = "";
                        ret = ExecRead(conn_db, out reader, "select * from " + table_pack + " where nzp_pack = " + finder.nzp_pack, true);
                        while (reader.Read())
                        {
                            if (reader["dat_uchet"] != DBNull.Value && Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString().Trim() != finder.dat_uchet.ToString())
                                changed_fields += "Дата учета: c " + Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString().Trim() + " на " + finder.dat_uchet.Trim() + ". ";
                            if (reader["dat_pack"] != DBNull.Value && Convert.ToDateTime(reader["dat_pack"]).ToShortDateString().Trim() != finder.dat_pack.ToString())
                                changed_fields += "Дата пачки: c " + Convert.ToDateTime(reader["dat_pack"]).ToShortDateString().Trim() + " на " + finder.dat_pack.Trim() + ". ";
                            if (reader["num_pack"] != DBNull.Value && reader["num_pack"].ToString().Trim() != finder.snum_pack.ToString())
                                changed_fields += "Номер пачки: c " + reader["num_pack"].ToString().Trim() + " на " + finder.snum_pack + ". ";
                            if (reader["pack_type"] != DBNull.Value && Convert.ToInt32(reader["pack_type"]) != finder.pack_type)
                                changed = true;
                            if (reader["nzp_payer"] != DBNull.Value && Convert.ToInt32(reader["nzp_payer"]) != finder.nzp_payer_contragent)
                                changed = true;
                            if (reader["nzp_supp"] != DBNull.Value && Convert.ToInt32(reader["nzp_supp"]) != finder.nzp_supp)
                                changed = true;

                        }
                        if (finder.flag != Pack.Statuses.CorrespondToPackAndNotDistributed.GetHashCode())
                            DbAdmin.InsertSysEvent(new SysEvents()
                            {
                                pref = Points.Pref,
                                bank = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00"),
                                nzp_user = finder.nzp_user,
                                nzp_dict = 6498,
                                nzp_obj = finder.nzp_pack,
                                note = changed_fields != "" ? "Были изменены следующие поля: " + changed_fields : "Пачка оплат была изменена"
                            }, transaction, conn_db);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка добавления события в sys_events" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                }
                #endregion

#if PG
                sql = "update " + table_pack + " set count_kv = (select count(*) from " + table_pack_ls + " where nzp_pack = " + finder.nzp_pack + ")" +
                                        ", sum_pack = (select coalesce(sum(g_sum_ls),0) from " + table_pack_ls + " where nzp_pack = " + finder.nzp_pack + ") ";
#else
                sql = "update " + table_pack + " set count_kv = (select count(*) from " + table_pack_ls + " where nzp_pack = " + finder.nzp_pack + ")" +
                                        ", sum_pack = (select nvl(sum(g_sum_ls),0) from " + table_pack_ls + " where nzp_pack = " + finder.nzp_pack + ") ";
#endif

                if (finder.pack_type > 0)
                {
                    sql += ", pack_type = " + finder.pack_type;
                }
                if (finder.nzp_bank > 0) sql += ", nzp_bank = " + finder.nzp_bank;
                if (finder.pack_type == 20)
                {
                    if (nzp_supp > 0) sql += ", nzp_supp = " + nzp_supp;
                    else sql += ", nzp_supp = null ";
                    if (nzp_payer > 0) sql += ", nzp_payer = " + nzp_payer;
                    else sql += ", nzp_payer = null ";
                }
                else sql += ", nzp_supp = null, nzp_payer=null ";
                if (finder.snum_pack != "") sql += ", num_pack = '" + finder.snum_pack + "'";
                if (finder.dat_uchet != "") sql += ", dat_uchet = " + Utils.EStrNull(finder.dat_uchet);
                if (finder.dat_pack != "") sql += ", dat_pack = " + Utils.EStrNull(finder.dat_pack);
                if (finder.flag > 0) sql += ", flag = " + finder.flag;
                if (finder.file_name != "") sql += ", file_name = '" + finder.file_name + "'";

#if PG
                sql += " , changed_by = " + nzpUser + ", changed_on = now() ";
#else
                sql += " , changed_by = " + nzpUser + ", changed_on = current ";
#endif
                sql += " where nzp_pack = " + finder.nzp_pack;
                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result) return ret;

                int kod_sum = 0;
                switch (finder.pack_type)
                {
                    case 10: kod_sum = 33; break;
                    case 20:
                        {
                            if (nzp_supp > 0)
                                kod_sum = 50;
                            //else 
                            if (nzp_payer > 0)
                                kod_sum = 49;
                            //else kod_sum = 35;

                            break;
                        }
                    case 21: kod_sum = 100; break;
                    default: kod_sum = 0; break;
                }

                //if (kod_sum > 0)
                //{
                //    sql = "update " + table_pack_ls + " set kod_sum = " + kod_sum + " where nzp_pack = " + finder.nzp_pack;
                //    ret = ExecSQL(conn_db, transaction, sql, true);
                //    if (!ret.result) return ret;
                //}

                //  if (ret.result) ret.tag = finder.nzp_pack;

                if (finder.flag == Pack.Statuses.CorrespondToPackAndNotDistributed.GetHashCode())
                {
                    #region Добавление в sys_events события 'Закрытие пачки оплат'
                    DbAdmin.InsertSysEvent(new SysEvents()
                    {
                        pref = Points.Pref,
                        bank = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00"),
                        nzp_user = finder.nzp_user,
                        nzp_dict = 6613,
                        nzp_obj = finder.nzp_pack,
                        note = "Номер пачки: " + finder.snum_pack
                    }, transaction, conn_db);
                    #endregion
                    if (Points.packDistributionParameters.DistributePackImmediately)
                    {
                        DbCalcPack db1 = new DbCalcPack();
                        db1.PackFonTasks(finder.nzp_pack, finder.year_, finder.nzp_user, CalcFonTask.Types.DistributePack, out ret, conn_db, transaction);  // Отдаем пачку на распределение

                        finder.flag = Pack.Statuses.WaitingForDistribution.GetHashCode();
                        if (ret.result)
                        {
                            db1.UpdatePackStatus(finder, conn_db, transaction);
                        }
                        db1.Close();
                    }
                }
                //обновляем данные в оплатах данной пачки
                if (changed)
                {
                    sql = "update " + table_pack_ls + " set  nzp_supp= " + (finder.nzp_supp > 0 ? finder.nzp_supp.ToString() : "null") +
                        ", nzp_payer= " + (finder.nzp_payer_contragent > 0 ? finder.nzp_payer_contragent.ToString() : "null") +
                        ", kod_sum = " + kod_sum +
                        " where nzp_pack = " + finder.nzp_pack;
                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result) return ret;
                }
                else
                {

                    if (kod_sum > 0)
                    {
                        sql = "update " + table_pack_ls + " set kod_sum = " + kod_sum + " where nzp_pack = " + finder.nzp_pack;
                        ret = ExecSQL(conn_db, transaction, sql, true);
                        if (!ret.result) return ret;
                    }

                }

                return ret;
            }
            else
            {
                string supp_field = "", supp_value = "", payer_field = "", payer_value = "";
                if (nzp_supp > 0)
                {
                    supp_field = ", nzp_supp ";
                    supp_value = ", " + nzp_supp;
                }
                if (nzp_payer > 0)
                {
                    payer_field = ", nzp_payer ";
                    payer_value = ", " + nzp_payer;
                }

                #region определить код ЕРЦ
                string erc_code = GetErcCode(out ret, conn_db, transaction);
                if (!ret.result)
                {
                    return ret;
                }
                if (erc_code.Trim() == "")
                {
                    ret = new Returns(false, "Не удалось определить код управляющей компании", -1);
                    return ret;
                }
                #endregion

                string value = "";
                if (finder.flag == Pack.Statuses.CorrespondToPackAndNotDistributed.GetHashCode())
                {
                    value = finder.flag.ToString();
                }
                else value = Pack.Statuses.NotClosed.GetHashCode().ToString();

#if PG
                sql = "insert into " + table_pack + " (erc_code,pack_type, nzp_bank " + supp_field + payer_field + ", num_pack, dat_pack, count_kv, sum_pack, dat_uchet, flag, dat_vvod, file_name, changed_by, changed_on)" +
                                       " values (" + erc_code + "," + finder.pack_type + "," + finder.nzp_bank + supp_value + payer_value + ", " + Utils.EStrNull(finder.snum_pack) + ", " + Utils.EStrNull(finder.dat_pack) +
                                       ", 0, 0, " + Utils.EStrNull(finder.dat_uchet) + "," + value + "," + Utils.EStrNull(DateTime.Now.ToShortDateString()) + ",'" + finder.file_name + "'," + nzpUser + ", now())";
#else
                sql = "insert into " + table_pack + " (erc_code,pack_type, nzp_bank " + supp_field + payer_field +", num_pack, dat_pack, count_kv, sum_pack, dat_uchet, flag, dat_vvod, file_name, changed_by, changed_on,kod_sum)" +
                                       " values (" + erc_code + "," + finder.pack_type + "," + finder.nzp_bank + supp_value +payer_value+ ", " + Utils.EStrNull(finder.snum_pack) + ", " + Utils.EStrNull(finder.dat_pack) +
                                       ", 0, 0, " + Utils.EStrNull(finder.dat_uchet) + "," + value + "," + Utils.EStrNull(DateTime.Now.ToShortDateString()) + ",'"+finder.file_name + "',"  + nzpUser + ", current)";
#endif
                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result) return ret;
                finder.nzp_pack = GetSerialValue(conn_db, transaction);

                #region Добавление в sys_events события 'Добавление пачки оплат'
                DbAdmin.InsertSysEvent(new SysEvents()
                {
                    pref = Points.Pref,
                    bank = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00"),
                    nzp_user = finder.nzp_user,
                    nzp_dict = 6499,
                    nzp_obj = finder.nzp_pack,
                    note = "Номер пачки: " + finder.snum_pack
                }, transaction, conn_db);
                #endregion

                return ret;
            }
        }

        private Returns SavePackMain(PackFinder finder, IDbConnection conn_db)
        {
            return SavePackMain(finder, conn_db, null);
        }

        public Returns SetPackStatusNotClosed(PackFinder finder)
        {
            if (finder.nzp_user <= 0) return new Returns(false, "Пользователь не определен", -1);
            if (finder.nzp_pack <= 0) return new Returns(false, "Пачка не выбрана", -1);
            if (finder.year_ <= 0) return new Returns(false, "Финансовый год не задан", -1);

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            string table_pack = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + tableDelimiter + "pack";
            StringBuilder sql = new StringBuilder("select flag from " + table_pack + " where nzp_pack = " + finder.nzp_pack);
            int flag = 0;
            object obj = ExecScalar(conn_db, sql.ToString(), out ret, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            try { flag = Convert.ToInt32(obj); }
            catch (Exception ex)
            {
                conn_db.Close();
                ret.result = false;
                ret.text = ex.Message;
                return ret;
            }

            if (!(flag == Pack.Statuses.NotDistributed.GetHashCode()))
            {
                conn_db.Close();
                return new Returns(false, "Только не распределенную пачку можно перевести в режим редактирования");
            }

            sql.Remove(0, sql.Length);
            sql.AppendFormat("update {0} set flag = {1} where nzp_pack = {2}", table_pack, Pack.Statuses.NotClosed.GetHashCode(), finder.nzp_pack);
            ret = ExecSQL(conn_db, sql.ToString(), true);
            conn_db.Close();
            return ret;
        }

        //--------------------------------------------------------------------------
        public Pack SavePackMain(PackFinder finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Пользователь не определен", -1);
                return null;
            }
            if (finder.nzp_pack < 1 && finder.nzp_bank < 1)
            {
                ret = new Returns(false, "Неверные входные параметры", -1);
                return null;
            }
            if (finder.year_ <= 0)
            {
                ret = new Returns(false, "Не задан год", -1);
                return null;
            }
            #endregion

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            ret = SavePackMain(finder, conn_db);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            //  finder.nzp_pack = ret.tag;
            List<Pack> list = FindPack(finder, out ret, conn_db);
            Pack pack = null;
            if (ret.result && list != null && list.Count > 0) pack = list[0];

            conn_db.Close();

            return pack;
        }

        //--------------------------------------------------------------------------
        public Pack_ls SavePackLs(Pack_ls finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Пользователь не определен", -1);
                return null;
            }
            if (finder.year_ <= 0)
            {
                ret = new Returns(false, "Не задан год", -1);
                return null;
            }
            if (finder.nzp_pack <= 0)
            {
                ret = new Returns(false, "Не задана пачка", -1);
                return null;
            }
            #endregion

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            ret = SavePackLs(finder, conn_db);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            //finder.nzp_pack_ls = ret.tag;
            List<Pack_ls> list = FindFinancePackLs(finder, out ret, conn_db);
            Pack_ls packls = null;
            if (ret.result && list != null && list.Count > 0) packls = list[0];

            conn_db.Close();

            return packls;
        }

        //--------------------------------------------------------------------------
        private Returns SavePackLs(Pack_ls finder, IDbConnection conn_db)
        {
            return SavePackLs(finder, conn_db, null);
        }

        private Returns SavePackLs(Pack_ls finder, IDbConnection conn_db, IDbTransaction transaction)
        {
            Returns ret;
#if PG
            string table_pack = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ".pack";
            string table_pack_ls = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ".pack_ls";
#else
            string table_pack = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ":pack";
            string table_pack_ls = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ":pack_ls";
#endif
            if (!TempTableInWebCashe(conn_db, transaction, table_pack)) return new Returns(false, "Данные не найдены", -1);
            string sql = "";
            DbTables tables = new DbTables(DBManager.getServer(conn_db));
            MyDataReader reader;

            int kodsum = 0, packtype = 0, nzp_supp = 0, nzp_payer = 0;
            sql = "select pack_type, nzp_supp, nzp_payer from " + table_pack + " where nzp_pack = " + finder.nzp_pack;
            ret = ExecRead(conn_db, transaction, out reader, sql, true);
            if (!ret.result) return ret;
            if (reader.Read())
            {
                if (reader["pack_type"] != DBNull.Value) packtype = Convert.ToInt32(reader["pack_type"]);
                if (reader["nzp_supp"] != DBNull.Value) nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                if (reader["nzp_payer"] != DBNull.Value) nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
            }
            reader.Close();

            if (finder.kod_sum > 0)
            {
                kodsum = finder.kod_sum;
            }
            else
            {
                switch (packtype)
                {
                    case 10:
                        kodsum = 33;
                        break;
                    case 20:
                        {
                            if (nzp_supp > 0)
                                kodsum = 50;
                            //else 
                            if (nzp_payer > 0)
                                kodsum = 49;
                            //else kodsum = 35;

                            break;
                        }

                    case 21: kodsum = 100; break;
                }
            }

            if (finder.nzp_pack_ls > 0)
            {
                sql = "select k.num_ls from " + tables.kvar + " k where nzp_kvar = " +
                      finder.nzp_kvar + " and pref = " + Utils.EStrNull(finder.pref);
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
                if (!reader.Read())
                {
                    reader.Close();
                    conn_db.Close();
                    ret = new Returns(false, "Не найден лицевой счет", -1);
                    return ret;
                }
                int num_ls = 0;
                if (reader["num_ls"] != DBNull.Value) num_ls = Convert.ToInt32(reader["num_ls"]);
                if (finder.manual_mode == 1)//удалить уточнение по услугам
                {
                    ret = DeleteManualDistrib(conn_db, finder.year_, finder.nzp_pack_ls, num_ls);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }
                }
                string field = "";
                if (finder.pkod != "") field += " , pkod = " + finder.pkod;
                sql = "update " + table_pack_ls + " set prefix_ls = " + finder.prefix_ls +
                    ", num_ls = " + num_ls +
                    ", g_sum_ls = " + finder.g_sum_ls +
                    ", sum_ls = " + finder.sum_ls +
                    ", dat_month = " + Utils.EStrNull(finder.dat_month) +
                    ", kod_sum = " + kodsum +
                    field +
                    ", unl = -1" +
                    ", dat_vvod = " + Utils.EStrNull(finder.dat_vvod) +
                    (finder.nzp_supp > 0 ? ", nzp_supp=" + finder.nzp_supp : " ,nzp_supp=null") +
                    (finder.nzp_payer_contragent > 0 ? ", nzp_payer=" + finder.nzp_payer_contragent : ",nzp_payer=null") +
                    " where nzp_pack_ls = " + finder.nzp_pack_ls;
            }
            else
            {
                #region определить код ЕРЦ
                string erc_code = GetErcCode(out ret, conn_db, transaction);
                if (!ret.result)
                {
                    return ret;
                }
                if (erc_code.Trim() == "")
                {
                    ret = new Returns(false, "Не удалось определить код управляющей компании", -1);
                    return ret;
                }
                #endregion

                #region определить номер новой пачки
                sql = "select max(info_num) as info_num from " + table_pack_ls + " where nzp_pack = " + finder.nzp_pack;
                ret = ExecRead(conn_db, transaction, out reader, sql, true);
                if (!ret.result)
                {
                    return ret;
                }
                if (reader.Read())
                {
                    if (reader["info_num"] != DBNull.Value) finder.info_num = Convert.ToInt32(reader["info_num"]) + 1;
                }
                reader.Close();

                if (finder.info_num < 1) finder.info_num = 1;
                #endregion

                string field = "", value = "", field_supp = "", value_supp = "", field_payer = "", value_payer = "";
                if (finder.pkod != "")
                {
                    field = ", pkod";
                    value = "," + finder.pkod;
                }
                if (finder.nzp_supp > 0)
                {
                    field_supp = ", nzp_supp";
                    value_supp = ", " + finder.nzp_supp;
                }
                if (finder.nzp_payer_contragent > 0)
                {
                    field_payer = ", nzp_payer";
                    value_payer = ", " + finder.nzp_payer_contragent;
                }

#if PG
                sql = "insert into " + table_pack_ls + " (nzp_pack, dat_vvod, info_num, erc_code, nzp_user, prefix_ls, num_ls, g_sum_ls, sum_ls, dat_month," +
                      " kod_sum, unl" + field + field_supp + field_payer + ")" +
                                     " values (" + finder.nzp_pack +
                                     ", " + Utils.EStrNull(finder.dat_vvod) +
                                     ", " + finder.info_num +
                                     ", " + Utils.EStrNull(erc_code, "") +
                                     ", " + finder.nzp_user +
                                     ", " + finder.prefix_ls +
                                     ", " + "(select k.num_ls from " + tables.kvar + " k where nzp_kvar = " + finder.nzp_kvar + " and pref = '" + finder.pref + "')" +
                                     ", " + finder.g_sum_ls +
                                     ", " + finder.sum_ls +
                                     ", " + Utils.EStrNull(finder.dat_month) +
                                     ", " + kodsum +
                                     ", -1" + value + value_supp + value_payer + ")";
#else
                sql = "insert into " + table_pack_ls + " (nzp_pack_ls, nzp_pack, dat_vvod, info_num, erc_code, nzp_user, prefix_ls, num_ls, g_sum_ls, sum_ls, dat_month, "+
                 " kod_sum, unl" + field + field_supp + field_payer + ")" +
                                     " values (0, " + finder.nzp_pack +
                                     ", " + Utils.EStrNull(finder.dat_vvod) +
                                     ", " + finder.info_num +
                                     ", " + Utils.EStrNull(erc_code, "") +
                                     ", " + finder.nzp_user +
                                     ", " + finder.prefix_ls +
                                     ", " + "(select k.num_ls from " + tables.kvar + " k where nzp_kvar = " + finder.nzp_kvar + " and pref = '" + finder.pref + "')" +
                                     ", " + finder.g_sum_ls +
                                     ", " + finder.sum_ls +
                                     ", " + Utils.EStrNull(finder.dat_month) +
                                     ", " + kodsum +
                                     ", -1" + value + value_supp + value_payer + ")";
#endif
            }

            if (finder.nzp_pack_ls >= 1)
            {
                #region Добавление в sys_events события 'Изменение оплаты'
                try
                {
                    //получение старых значений
                    var changed_fields = "";
                    ret = ExecRead(conn_db, out reader, "select * from " + table_pack_ls + " where nzp_pack_ls = " + finder.nzp_pack_ls, true);
                    while (reader.Read())
                    {
                        if (reader["g_sum_ls"] != DBNull.Value && reader["g_sum_ls"].ToString().Trim() != finder.g_sum_ls.ToString())
                            changed_fields += "Сумма оплаты: c " + reader["g_sum_ls"].ToString().Trim() + " на " + finder.g_sum_ls + ". ";
                        if (reader["sum_ls"] != DBNull.Value && reader["sum_ls"].ToString().Trim() != finder.sum_ls.ToString())
                            changed_fields += "Сумма ЛС: c " + reader["sum_ls"].ToString().Trim() + " на " + finder.sum_ls + ". ";
                        if (reader["dat_month"] != DBNull.Value && Convert.ToDateTime(reader["dat_month"]).ToShortDateString() != finder.dat_month.ToString())
                            changed_fields += "Дата: c " + reader["dat_month"].ToString().Trim() + " на " + finder.dat_month + ". ";
                    }

                    DbAdmin.InsertSysEvent(new SysEvents()
                    {
                        pref = Points.Pref,
                        bank = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00"),
                        nzp_user = finder.nzp_user,
                        nzp_dict = 6488,
                        nzp_obj = finder.nzp_pack_ls,
                        note = changed_fields != "" ? "Были изменены следующие поля: " + changed_fields : "Оплата была изменена"
                    }, transaction, conn_db);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка добавления события в sys_events" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                }
                #endregion
            }

            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result)
            {
                return ret;
            }

            if (finder.nzp_pack_ls < 1)
            {
                finder.nzp_pack_ls = GetSerialValue(conn_db, transaction);

                #region Добавление в sys_events события 'Добавление оплаты'
                DbAdmin.InsertSysEvent(new SysEvents()
                {
                    pref = Points.Pref,
                    bank = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00"),
                    nzp_user = finder.nzp_user,
                    nzp_dict = 6486,
                    nzp_obj = finder.nzp_pack_ls,
                    note = "Сумма оплаты " + finder.g_sum_ls
                }, transaction, conn_db);
                #endregion
            }

            if (finder.errors == "")
            {
                PackFinder pack = new PackFinder()
                {
                    nzp_user = finder.nzp_user,
                    nzp_pack = finder.nzp_pack,
                    year_ = finder.year_,
                    nzp_supp = nzp_supp,
                    nzp_payer_contragent = nzp_payer,
                    pack_type = packtype
                };
                ret = SavePackMain(pack, conn_db, transaction); // обновить пачку

                if (!ret.result)
                {
                    return ret;
                }
            }
            return ret;
        }

        public Returns FormPacksSbPay(EFSReestr finder, PackFinder packfinder)
        {
            #region проверка вх данных
            if (finder.nzp_user <= 0) return new Returns(false, "Пользователь не задан");
            if (finder.nzp_efs_reestr <= 0) return new Returns(false, "Код реестра не задан");
            #endregion

            Returns ret = Utils.InitReturns();
            MyDataReader reader, reader2, reader3;
            DateTime plpordate = DateTime.MinValue;
            decimal sum_superpack = 0;
            int plpornum = 0;
            List<Pack> listpack = new List<Pack>();
            PackFinder pack = new PackFinder();

            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            #region таблицы
            DbTables tables = new DbTables(DBManager.getServer(conn_db));
            string table_reestr = Points.Pref + "_data" + tableDelimiter + "efs_reestr";
            #endregion

            #region определение записи в реестре для которой будут формироваться пачки
            string sql = " select date_uchet, packstatus from " + table_reestr + " where nzp_efs_reestr = " + finder.nzp_efs_reestr;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            if (reader.Read())
            {
                if (reader["date_uchet"] != DBNull.Value) finder.date_uchet = Convert.ToDateTime(reader["date_uchet"]).ToShortDateString();
                if (reader["packstatus"] != DBNull.Value) finder.packstatus = Convert.ToInt32(reader["packstatus"]);
            }
            reader.Close();
            #endregion

            if (finder.date_uchet == "")
            {
                conn_db.Close();
                return new Returns(false, "Не определена дата учета");
            }

            if (finder.packstatus != EFSReestr.ReestrStatuses.PackNotForm.GetHashCode())
            {
                conn_db.Close();
                return new Returns(false, "Формировать пачки можно только для реестра со статусом: пачки не сформированы", -1);
            }

            string table_pay = Points.Pref + "_fin_" + (Convert.ToDateTime(finder.date_uchet).Year - 2000).ToString("00") + tableDelimiter + "efs_pay ";
            string table_cnt = Points.Pref + "_fin_" + (Convert.ToDateTime(finder.date_uchet).Year - 2000).ToString("00") + tableDelimiter + "efs_cnt ";
            string table_pu_vals = Points.Pref + "_fin_" + (Convert.ToDateTime(finder.date_uchet).Year - 2000).ToString("00") + tableDelimiter + "pu_vals ";

            #region выборка данных из таблицы efs_cnt, соответствующих реестру
            sql = " select * from " + table_pay + "  where 1=1  and nzp_efs_reestr = " + finder.nzp_efs_reestr +
                  " order by plpor_date, plpor_num";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            List<Pack> packsraspr = new List<Pack>();
            IDbTransaction transaction = conn_db.BeginTransaction();

            while (reader.Read())
            {
                EFSPay pay = new EFSPay();
                if (reader["id_pay"] != DBNull.Value) pay.id_pay = Convert.ToInt32(reader["id_pay"]);
                if (reader["plpor_num"] != DBNull.Value) pay.plpor_num = Convert.ToInt32(reader["plpor_num"]);
                if (reader["plpor_date"] != DBNull.Value) pay.plpor_date = Convert.ToDateTime(reader["plpor_date"]).ToShortDateString();
                if (reader["ls_num"] != DBNull.Value) pay.ls_num = Convert.ToDecimal(reader["ls_num"]);
                if (reader["summa"] != DBNull.Value) pay.summa = Convert.ToDecimal(reader["summa"]);
                if (reader["barcode"] != DBNull.Value) pay.barcode = Convert.ToString(reader["barcode"]).Trim();
                sum_superpack += pay.summa;
                if (pay.plpor_date == "")
                {
                    transaction.Rollback();
                    UpdateEfsReestr(finder.nzp_efs_reestr, EFSReestr.ReestrStatuses.PackNotForm.GetHashCode(), conn_db, null);
                    reader.Close();
                    Close();
                    conn_db.Close();
                    ret = new Returns(false, "Не возможно сформировать пачки. Для кода платежа: " + pay.id_pay + " нет даты платежного поручения", -1);
                    return ret;
                }
                #region если создание новой пачки
                if (plpornum != pay.plpor_num || plpordate != Convert.ToDateTime(pay.plpor_date))
                {
                    #region для предыдущей пачки обновляется количество квитанций и сумма, пачка ставится на распределение, если установлен параметр распределять немедленно
                    if (pack.nzp_pack > 0)
                    {
                        pack.nzp_user = packfinder.nzp_user;
                        pack.nzp_bank = 0;
                        pack.pack_type = 0;
                        pack.snum_pack = "";
                        pack.dat_pack = "";
                        pack.dat_uchet = "";
                        pack.flag = 0;
                        packsraspr.Add(pack);
                        ret = SavePackMain(pack, conn_db, transaction);
                        if (!ret.result)
                        {
                            transaction.Rollback();
                            UpdateEfsReestr(finder.nzp_efs_reestr, EFSReestr.ReestrStatuses.PackNotForm.GetHashCode(), conn_db, null);
                            reader.Close();
                            Close();
                            conn_db.Close();
                            return ret;
                        }
                    }
                    #endregion

                    pack = new PackFinder();
                    pack.nzp_user = packfinder.nzp_user;
                    pack.nzp_bank = packfinder.nzp_bank;
                    pack.pack_type = 10;
                    pack.snum_pack = pay.plpor_num.ToString();
                    pack.dat_pack = pay.plpor_date;
                    pack.year_ = Points.DateOper.Year;
                    pack.dat_uchet = Points.DateOper.ToShortDateString();
                    pack.flag = Pack.Statuses.CorrespondToPackAndNotDistributed.GetHashCode();

                    ret = SavePackMain(pack, conn_db, transaction);
                    if (!ret.result)
                    {
                        transaction.Rollback();
                        UpdateEfsReestr(finder.nzp_efs_reestr, EFSReestr.ReestrStatuses.PackNotForm.GetHashCode(), conn_db, null);
                        reader.Close();
                        conn_db.Close();
                        return ret;
                    }
                    listpack.Add(pack);
                    plpornum = pay.plpor_num;
                    if (pay.plpor_date == "")
                    {
                        transaction.Rollback();
                        UpdateEfsReestr(finder.nzp_efs_reestr, EFSReestr.ReestrStatuses.PackNotForm.GetHashCode(), conn_db, null);
                        reader.Close();
                        conn_db.Close();
                        ret.result = false;
                        ret.text = "Нет даты платежного поручения";
                        ret.tag = -1;
                        return ret;
                    }
                    plpordate = Convert.ToDateTime(pay.plpor_date);
                }
                #endregion

                int nzpkvar = 0, numls = 0;
                string pref = "", dat_month = "";

                #region определение nzp_kvar, num_ls, pref по pkod
                sql = "select nzp_kvar, pref, num_ls from " + tables.kvar + " where pkod = " + Convert.ToInt64(pay.ls_num);
                ret = ExecRead(conn_db, transaction, out reader2, sql, true);
                if (!ret.result)
                {
                    transaction.Rollback();
                    UpdateEfsReestr(finder.nzp_efs_reestr, EFSReestr.ReestrStatuses.PackNotForm.GetHashCode(), conn_db, null);
                    reader.Close();
                    conn_db.Close();
                    return ret;
                }

                if (reader2.Read())
                {
                    if (reader2["nzp_kvar"] != DBNull.Value) nzpkvar = Convert.ToInt32(reader2["nzp_kvar"]);
                    if (reader2["num_ls"] != DBNull.Value) numls = Convert.ToInt32(reader2["num_ls"]);
                    if (reader2["pref"] != DBNull.Value) pref = Convert.ToString(reader2["pref"]).Trim();
                }
                reader2.Close();
                #endregion

                if (pref == "")
                {
                    transaction.Rollback();
                    UpdateEfsReestr(finder.nzp_efs_reestr, EFSReestr.ReestrStatuses.PackNotForm.GetHashCode(), conn_db, null);
                    reader.Close();
                    conn_db.Close();
                    ret.result = false;
                    ret.text = "Не найден л/с с платежным кодом " + Convert.ToInt64(pay.ls_num);
                    ret.tag = -1;
                    return ret;
                }
                string table_counters_ord = pref + "_charge_" + (Convert.ToDateTime(finder.date_uchet).Year - 2000).ToString("00") + tableDelimiter + "counters_ord ";

                #region создание оплаты
                Pack_ls packls = new Pack_ls();
                packls.nzp_user = packfinder.nzp_user;
                packls.nzp_pack = pack.nzp_pack;
                packls.year_ = Points.DateOper.Year;

                if (pay.barcode.Trim() != "" && pay.barcode.Length == 30)
                {
                    DateTime dm = new DateTime(Convert.ToInt32(pay.barcode.Substring(17, 2)) + 2000,
                        Convert.ToInt32(pay.barcode.Substring(15, 2)), 1);
                    dat_month = dm.ToShortDateString();

                    packls.kod_sum = Convert.ToInt32(pay.barcode.Substring(0, 2));
                }
                packls.nzp_kvar = nzpkvar;
                packls.pref = pref;
                packls.dat_month = dat_month;
                packls.pkod = Convert.ToInt64(pay.ls_num).ToString();
                packls.dat_vvod = Points.DateOper.ToShortDateString();
                packls.g_sum_ls = pay.summa;
                packls.errors = "не обновлять пачку";
                ret = SavePackLs(packls, conn_db, transaction);
                if (!ret.result)
                {
                    transaction.Rollback();
                    UpdateEfsReestr(finder.nzp_efs_reestr, EFSReestr.ReestrStatuses.PackNotForm.GetHashCode(), conn_db, null);
                    reader.Close();
                    conn_db.Close();
                    return ret;
                }
                #endregion

                #region выборка данных из таблицы efs_cnt, соответствующих id_pay
                sql = " select * from " + table_cnt + "  where 1=1  and nzp_efs_reestr = " + finder.nzp_efs_reestr +
                      " and id_pay = " + pay.id_pay;
                ret = ExecRead(conn_db, transaction, out reader2, sql, true);
                if (!ret.result)
                {
                    transaction.Rollback();
                    conn_db.Close();
                    return ret;
                }
                while (reader2.Read())
                {
                    EFSCnt cnt = new EFSCnt();
                    if (reader2["cnt_num"] != DBNull.Value) cnt.cnt_num = Convert.ToInt32(reader2["cnt_num"]);
                    if (reader2["cnt_val"] != DBNull.Value) cnt.cnt_val = Convert.ToDecimal(reader2["cnt_val"]);

                    int nzpcounter = 0;
                    #region если в штрих-коде нет dat_month, определяем его через таблицу sp_payment
                    if (dat_month == "")
                    {
                        #region определить БД webfon
                        sql = "select dbname,dbserver from " + Points.Pref + "_kernel" + tableDelimiter + "s_baselist where idtype = 8";
                        ret = ExecRead(conn_db, transaction, out reader3, sql, true);
                        if (!ret.result)
                        {
                            transaction.Rollback();
                            conn_db.Close();
                            return ret;
                        }
                        string dbname = "", dbserver = "", namewebfon = "";
                        if (reader3.Read())
                        {
                            if (reader3["dbname"] != DBNull.Value) dbname = Convert.ToString(reader3["dbname"]).Trim();
                            if (reader3["dbserver"] != DBNull.Value) dbserver = Convert.ToString(reader3["dbserver"]).Trim();
                        }
                        reader3.Close();
                        if (dbname != "") namewebfon = dbname;
#if PG

#else
                        if (dbserver != "") namewebfon += "@" + dbserver;
#endif
                        #endregion
                        if (TempTableInWebCashe(conn_db, namewebfon + tableDelimiter + "sb_payment"))
                        {
                            sql = "select dat_month from " + namewebfon + tableDelimiter + "sb_payment where bank_operation_id = " + pay.id_pay;
                            ret = ExecRead(conn_db, transaction, out reader3, sql, true);
                            if (!ret.result)
                            {
                                transaction.Rollback();
                                conn_db.Close();
                                return ret;
                            }
                            while (reader3.Read())
                            {
                                if (reader3["dat_month"] != DBNull.Value) dat_month = Convert.ToDateTime(reader3["dat_month"]).ToShortDateString();
                            }
                            reader3.Close();
                        }
                    }
                    #endregion

                    #region определение nzp_counter по dat_month
                    if (dat_month != "")
                    {
                        sql = "select nzp_counter from " + table_counters_ord + " where order_num = " + cnt.cnt_num +
                            " and num_ls = " + numls + " and dat_month = '" + dat_month + "'";
                        ret = ExecRead(conn_db, transaction, out reader3, sql, true);
                        if (!ret.result)
                        {
                            transaction.Rollback();
                            conn_db.Close();
                            return ret;
                        }
                        while (reader3.Read())
                        {
                            if (reader3["nzp_counter"] != DBNull.Value) nzpcounter = Convert.ToInt32(reader3["nzp_counter"]);
                        }
                        reader3.Close();
                    }
                    #endregion

                    #region определение nzp_counter по val_cnt
                    else
                    {
                        sql = "select nzp_counter from " + table_counters_ord + " where order_num = " + cnt.cnt_num +
                            " and num_ls = " + numls + " and val_cnt = " + cnt.cnt_val + " group by nzp_counter";
                        ret = ExecRead(conn_db, transaction, out reader3, sql, true);
                        if (!ret.result)
                        {
                            transaction.Rollback();
                            conn_db.Close();
                            return ret;
                        }
                        int i = 0;
                        while (reader3.Read())
                        {
                            i++;
                            if (reader3["nzp_counter"] != DBNull.Value) nzpcounter = Convert.ToInt32(reader3["nzp_counter"]);
                        }
                        reader3.Close();
                        if (i > 1) nzpcounter = 0;
                    }
                    #endregion

                    #region заполнение таблицы pu_vals
                    sql = " insert into " + table_pu_vals + " (nzp_pack_ls, num_ls, val_cnt, dat_month, nzp_counter) " +
                          " values (" + packls.nzp_pack_ls + "," + numls + "," + cnt.cnt_val + "," + Utils.EStrNull(dat_month) + "," + nzpcounter + ")";
                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result)
                    {
                        transaction.Rollback();
                        conn_db.Close();
                        return ret;
                    }
                    #endregion
                }
                reader2.Close();
                #endregion

            }
            reader.Close();
            #endregion

            #region для последней пачки обновляется количество квитанций и сумма, пачка ставится на распределение, если установлен параметр распределять немедленно
            if (pack.nzp_pack > 0)
            {
                pack.nzp_user = packfinder.nzp_user;
                pack.nzp_bank = 0;
                pack.pack_type = 0;
                pack.snum_pack = "";
                pack.dat_pack = "";
                pack.dat_uchet = "";
                pack.flag = 0;
                ret = SavePackMain(pack, conn_db, transaction);
                if (!ret.result)
                {
                    transaction.Rollback();
                    UpdateEfsReestr(finder.nzp_efs_reestr, EFSReestr.ReestrStatuses.PackNotForm.GetHashCode(), conn_db, null);
                    reader.Close();
                    Close();
                    conn_db.Close();
                    return ret;
                }
                packsraspr.Add(pack);
            }
            #endregion

            bool result = true;
            #region если было создано несколько пачек, создается суперпачка
            if (listpack.Count > 1)
            {
                listpack[0].sum_pack = sum_superpack;
                foreach (Pack p in listpack)
                {
                    p.dat_pack = pack.dat_pack;
                }
                result = SaveSuperPack(listpack, conn_db, out ret, transaction);
            }
            #endregion

            if (!result)
            {
                transaction.Rollback();
                ret = UpdateEfsReestr(finder.nzp_efs_reestr, EFSReestr.ReestrStatuses.PackNotForm.GetHashCode(), conn_db, null);
                ret = new Returns(false);
                conn_db.Close();
                return ret;
            }

            //обновление статуса efs_reestr
            ret = UpdateEfsReestr(finder.nzp_efs_reestr, EFSReestr.ReestrStatuses.PackIsForm.GetHashCode(), conn_db, transaction);
            if (!ret.result)
            {
                transaction.Rollback();
                conn_db.Close();
                return ret;
            }

            transaction.Commit();

            if (Points.packDistributionParameters.DistributePackImmediately)
            {
                DbCalcPack db1 = new DbCalcPack();
                foreach (Pack p in packsraspr)
                {
                    db1.PackFonTasks(p.nzp_pack, Points.DateOper.Year, p.nzp_user, CalcFonTask.Types.DistributePack, out ret, conn_db, transaction);  // Отдаем пачку на распределение
                    p.flag = Pack.Statuses.WaitingForDistribution.GetHashCode();
                    if (ret.result)
                    {
                        db1.UpdatePackStatus(p, conn_db, transaction);
                    }
                }

                db1.Close();
            }

            conn_db.Close();

            return ret;
        }

        private Returns UpdateEfsReestr(int nzp_efs_reestr, int packstatus, IDbConnection conn_db, IDbTransaction transaction)
        {
            string table_reestr = Points.Pref + "_data" + tableDelimiter + "efs_reestr";
            string sql = "update " + table_reestr + " set packstatus = " + packstatus +
            " where nzp_efs_reestr = " + nzp_efs_reestr;
            Returns ret = ExecSQL(conn_db, transaction, sql, true);
            return ret;
        }


    }
}
