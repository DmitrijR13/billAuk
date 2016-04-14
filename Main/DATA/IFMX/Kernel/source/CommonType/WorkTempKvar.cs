using System;
using System.Collections.Generic;
using STCLINE.KP50.DataBase;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.IFMX.Kernel.source.CommonType
{
    using System.Globalization;

    public class WorkTempKvar : DataBaseHead
    {
        #region Функция обновления статистик по Charge_XX
        /// <summary>
        /// Обновить статистику таблицы в базе данных PREF_charge_XX за расчетный месяц
        /// </summary>
        /// <param name="alldata"></param>
        /// <param name="pc">испольуются поля nzp_kvar, nzp_dom, pref, calc_yy</param>
        /// <param name="tab">наименование таблицы</param>
        /// <param name="ret">результат выполнения операции</param>
        public void UpdateStatistics(bool alldata, CalcTypes.ParamCalc pc, string tab, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (pc.nzp_kvar == 0 && (alldata || pc.nzp_dom == 0))
            {

                string conn_kernel = Points.GetConnByPref(pc.pref);
#if PG

                using (IDbConnection conn_db2 = GetConnection(conn_kernel))
                {
                    string schemaPref = pc.pref + "_charge_" + (pc.calc_yy - 2000).ToString("00") + ".";
                    if (tab.IndexOf('.') > -1) schemaPref = "";
                    ret = OpenDb(conn_db2, true);
                    if (ret.result)
                        ret = ExecSQL(conn_db2, sUpdStat + " " + schemaPref + tab, true);
                }


#else
                IDbConnection conn_db2 = GetConnection(conn_kernel);

                ret = OpenDb(conn_db2, true);
                if (!ret.result)
                {
                    return;
                }

                ret = ExecSQL(conn_db2, " Database " + pc.pref + "_charge_" + (pc.calc_yy - 2000).ToString("00")

, true);
                if (!ret.result)
                {
                    conn_db2.Close();
                    return;
                }

                ret = ExecSQL(conn_db2, sUpdStat + " " + tab, true);

                ret = ExecSQL(conn_db2, " Database " + Points.Pref + "_kernel", true);

                if (!ret.result)
                {
                    conn_db2.Close();
                    return;
                }
                conn_db2.Close();
#endif
            }


        }
        #endregion Функция обновления статистик по Charge_XX


        public ReturnsObjectType<Ls> GetLsLocation(Ls finder, IDbConnection connection, IDbTransaction transaction)
        {
            string sql = "select pref from " + Points.Pref + "_data" + tableDelimiter + "kvar k where 1=1";

            if (finder.nzp_kvar > 0) sql += " and nzp_kvar = " + finder.nzp_kvar;
            else if (finder.num_ls > 0) sql += " and num_ls = " + finder.num_ls;
            else if (finder.pkod != "") sql += " and pkod = " + finder.pkod;

            IntfResultTableType table = ClassDBUtils.OpenSQL(sql, connection, transaction);

            if (table.resultData != null && table.resultData.Rows != null && table.resultData.Rows.Count > 0)
                return new ReturnsObjectType<Ls>(new Ls() { pref = Convert.ToString(table.resultData.Rows[0]["pref"] ?? "") }, true, "");
            else return new ReturnsObjectType<Ls>(false, "Лицевой счет не найден");
        }


        public Returns DeleteLs(Ls finder)
        {
            Returns ret = Utils.InitReturns();
            if (finder.nzp_user <= 0)
            {
                ret.text = "Пользователь не известен";
                ret.result = false;
                ret.tag = -1;
                return ret;
            }
            if (finder.pref == "")
            {
                ret.text = "Не указан банк данных";
                ret.result = false;
                ret.tag = -1;
                return ret;
            }
            if (finder.nzp_kvar <= 0)
            {
                ret.text = "Не выбран лицевой счет";
                ret.result = false;
                ret.tag = -1;
                return ret;
            }

            #region Соединение
            IDbConnection connection = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connection, true);
            if (!ret.result) return ret;
            #endregion

            var sql = string.Format(" select count(*) from {0} where nzp_kvar = {1} "
                                   , string.Format("{0}{1}kvar", finder.pref, sDataAliasRest), finder.nzp_kvar);
            var count = ExecScalar(connection, sql, out ret, true);
            if (ret.result && Convert.ToInt32(count) == 0)
            {
                connection.Close();
                ret.result = false;
                ret.text = "Не возможно удалить ЛС, т.к. ЛС удален ";
                return ret;
            }
            #region переменные
            MyDataReader reader;
            var calc_month = Points.GetPoint(finder.pref).CalcMonth;
            var prm_3 = string.Format("{0}{1}prm_3", finder.pref, sDataAliasRest);
            var pack_ls = string.Format("{0}_fin_{1}.pack_ls", Points.Pref, calc_month.year_ % 1000);
            var fn_supplier = string.Format("{0}_charge_{1}.fn_supplier{2}", finder.pref, calc_month.year_ % 1000,
                Points.CalcMonth.month_ < 10 ? "0" + calc_month.month_ : calc_month.month_.ToString(CultureInfo.InvariantCulture));
            var locKvar = string.Format("{0}{1}kvar", finder.pref, sDataAliasRest);
            var locKvarArx = string.Format("{0}{1}kvar_arx", finder.pref, sDataAliasRest);
            var centrKvar = string.Format("{0}{1}kvar", Points.Pref, sDataAliasRest);
            var centrKvarArx = string.Format("{0}{1}kvar_arx", Points.Pref, sDataAliasRest);
            var calcMonth = new DateTime(calc_month.year_, calc_month.month_, 1);
            var num_ls = Convert.ToInt32(ExecScalar(connection, string.Format("select num_ls from {0} where nzp_kvar = {1}", locKvar, finder.nzp_kvar), out ret, true));
            var last_charge = string.Format("{0}_charge_{1}.charge_{2}", finder.pref, calc_month.year_ % 1000,
                (calc_month.month_ - 1) < 10 ? "0" + (calc_month.month_ - 1) : (calc_month.month_ - 1).ToString(CultureInfo.InvariantCulture));
            var pack_ls_arh = string.Format("{0}_fin_{1}.pack_ls_arh", Points.Pref, calc_month.year_ % 1000);
            var lnk_charge = string.Format("{0}_charge_{1}.lnk_charge_{2}", finder.pref, calc_month.year_ % 1000,
            (calc_month.month_) < 10 ? "0" + (calc_month.month_) : (calc_month.month_).ToString(CultureInfo.InvariantCulture));
            #endregion

            #region Проверки перед удалением ЛС
            //ЛС создан в текущем расчетном месяце 
            sql = string.Format(" select count(*) from {0} where nzp = {1} and nzp_prm = 51 and (month_calc <> '{2}' or (month_calc is null and dat_when not between '{2}' and '{3}')) " 
                                   , prm_3, finder.nzp_kvar, calcMonth,calcMonth.AddMonths(1).AddDays(-1));
            count = ExecScalar(connection, sql, out ret, true);
            if (ret.result && Convert.ToInt32(count) > 0)
            {
                connection.Close();
                ret.result = false;
                ret.text = "Не возможно удалить ЛС, т.к. ЛС создан не в текущем расчетном месяце ";
                return ret;
            }
            else if (!ret.result)
            {
                connection.Close();
                return ret;
            }
            //нет расщепленных оплат 
            sql = string.Format("select dat_uchet from {0} pl where pl.num_ls  = {1} and pl.dat_uchet <> null ", pack_ls, num_ls);
            ret = ExecRead(connection, out reader, sql, true);
            if (!ret.result)
            {
                connection.Close();
                return ret;
            }
            while (reader.Read())
            {
                var date = Convert.ToDateTime(reader["dat_uchet"]);
                fn_supplier = string.Format("{0}_charge_{1}.fn_supplier{2}", finder.pref, date.Year % 1000, date.Month < 10 ? "0" + date.Month : date.Month.ToString());
                sql = string.Format("select count(*) from {0} where num_ls = {1} and dat_uchet = '{2}'", fn_supplier, num_ls, date);
                count = ExecScalar(connection, sql, out ret, true);
                if (ret.result && Convert.ToInt32(count) > 0)
                {
                    connection.Close();
                    ret.result = false;
                    ret.text = "Не возможно удалить ЛС, т.к. есть расщепленные оплаты  ";
                    return ret;
                }
                else if (!ret.result)
                {
                    connection.Close();
                    return ret;
                }
            }
            reader.Close();
            //Нет начислений/сальдо в прошлом месяце относительно текущего расчетного месяца 
            sql = string.Format(" select count(*) from {0} where nzp_kvar = {1} and nzp_serv>1 and dat_charge is null and " +
                                " (abs(sum_insaldo) + abs(rsum_tarif) + abs(real_charge) + abs(reval) + abs(sum_money) + abs(money_to) + abs(sum_outsaldo))>0.001", last_charge, finder.nzp_kvar);
            count = ExecScalar(connection, sql, out ret, true);
            if (ret.result && Convert.ToInt32(count) > 0)
            {
                connection.Close();
                ret.result = false;
                ret.text = "Не возможно удалить ЛС, т.к. есть начисления/сальдо в прошлом месяце относительно текущего расчетного месяца ";
                return ret;
            }
            else if (!ret.result)
            {
                connection.Close();
                return ret;
            }
            #endregion

            #region Удаление

            var transaction = connection.BeginTransaction();
            try
            {
                #region Очистка таблиц в схеме fin, за исключением pack_ls

                sql = string.Format(" Select distinct c.table_name from " +
                                    " INFORMATION_SCHEMA.columns c where c.table_schema " +
                                    " ilike '%{0}%' and c.COLUMN_NAME = 'num_ls' and c.table_name <> 'pack_ls' "
                    , string.Format("{0}_fin_{1}", Points.Pref, calc_month.year_ % 1000));
                ret = ExecRead(connection, transaction, out reader, sql, true);
                if (!ret.result)
                {
                    throw new Exception("Ошибка при Очистке таблиц в схеме fin, за исключением pack_ls");
                }
                while (reader.Read())
                {
                    var table_name = reader["table_name"];
                    sql = string.Format("delete from {0} where num_ls = {1} ",
                        string.Format("{0}_fin_{1}", Points.Pref, calc_month.year_ % 1000) +
                        tableDelimiter + table_name, num_ls);
                    ret = ExecSQL(connection, transaction, sql, true);
                    if (!ret.result)
                    {
                        throw new Exception("Ошибка при Очистке таблиц в схеме fin, за исключением pack_ls");
                    }
                }
                sql = string.Format(" Select distinct c.table_name from " +
                                    " INFORMATION_SCHEMA.columns c where c.table_schema " +
                                    " ilike '%{0}%' and c.COLUMN_NAME = 'nzp_kvar' and c.table_name <> 'pack_ls'"
                    , string.Format("{0}_fin_{1}", Points.Pref, calc_month.year_ % 1000));
                ret = ExecRead(connection, transaction, out reader, sql, true);
                if (!ret.result)
                {
                    throw new Exception("Ошибка при Очистке таблиц в схеме fin, за исключением pack_ls");
                }
                while (reader.Read())
                {
                    var table_name = reader["table_name"];
                    sql = string.Format("delete from {0} where nzp_kvar = {1} ",
                        string.Format("{0}_fin_{1}", Points.Pref, calc_month.year_ % 1000) +
                        tableDelimiter + table_name, finder.nzp_kvar);
                    ret = ExecSQL(connection, transaction, sql, true);
                    if (!ret.result)
                    {
                        throw new Exception("Ошибка при Очистке таблиц в схеме fin, за исключением pack_ls");
                    }
                }

                #endregion

                #region удаляются все оплаты по ЛС
                sql = string.Format("delete from {0} where num_ls = {1}", pack_ls, num_ls);
                ret = ExecSQL(connection, transaction, sql, true);
                if (!ret.result)
                {
                    throw new Exception("Ошибка при удалении оплат лс");
                }

                #endregion

                #region удаляются все перерасчетные начисления по ЛС

                sql = string.Format("select * from {0} where nzp_kvar = {1}", lnk_charge, finder.nzp_kvar);
                ret = ExecRead(connection, transaction, out reader, sql, true);
                if (!ret.result)
                {
                    throw new Exception("Ошибка при удалении лс");
                }
                while (reader.Read())
                {
                    var month_ = Convert.ToInt32(reader["month_"]);
                    var year_ = Convert.ToInt32(reader["year_"]);
                    var charge_xx = string.Format("{0}_charge_{1}.charge_{2}", finder.pref, year_%1000,
                        (month_) < 10
                            ? "0" + (month_)
                            : (month_).ToString(CultureInfo.InvariantCulture));
                    sql = string.Format("delete from {0} where nzp_kvar = {1} ", charge_xx, finder.nzp_kvar);
                    ret = ExecSQL(connection, transaction, sql, true);
                    if (!ret.result)
                    {
                        throw new Exception("Ошибка при удалении лс");
                    }
                }
                reader.Close();
                sql = string.Format("delete from {0} where nzp_kvar = {1} ", lnk_charge, finder.nzp_kvar);
                ret = ExecSQL(connection, transaction, sql, true);
                if (!ret.result)
                {
                    throw new Exception("Ошибка при удалении перерасчетов лс");
                }

                #endregion

                #region удаляются все начисления по ЛС

                var charge = string.Format("{0}_charge_{1}.charge_{2}", finder.pref, calc_month.year_ % 1000,
                    (Points.CalcMonth.month_) < 10
                        ? "0" + (Points.CalcMonth.month_)
                        : (Points.CalcMonth.month_).ToString(CultureInfo.InvariantCulture));
                sql = string.Format("delete from {0} where nzp_kvar = {1} ", charge, finder.nzp_kvar);
                ret = ExecSQL(connection, transaction, sql, true);
                if (!ret.result)
                {
                    throw new Exception("Ошибка при удалении начислений лс");
                }

                #endregion

                #region Очистка таблиц схемы charge

                sql = string.Format(" Select distinct c.table_name from " +
                                    " INFORMATION_SCHEMA.columns c where c.table_schema " +
                                    " ilike '%{0}%' and c.COLUMN_NAME = 'num_ls' "
                    , string.Format("{0}_charge_{1}", finder.pref, calc_month.year_ % 1000));
                ret = ExecRead(connection, transaction, out reader, sql, true);
                if (!ret.result)
                {
                    throw new Exception("Ошибка при Очистке таблиц схемы charge");
                }
                while (reader.Read())
                {
                    var table_name = reader["table_name"];
                    sql = string.Format("delete from {0} where num_ls = {1} ",
                        string.Format("{0}_charge_{1}", finder.pref, calc_month.year_ % 1000)
                        + tableDelimiter + table_name, num_ls);
                    ret = ExecSQL(connection, transaction, sql, true);
                    if (!ret.result)
                    {
                        throw new Exception("Ошибка при Очистке таблиц схемы charge");
                    }
                }
                sql = string.Format(" Select distinct c.table_name from " +
                                    " INFORMATION_SCHEMA.columns c where c.table_schema " +
                                    " ilike '%{0}%' and c.COLUMN_NAME = 'nzp_kvar'"
                    , string.Format("{0}_charge_{1}", finder.pref, calc_month.year_ % 1000));
                ret = ExecRead(connection, transaction, out reader, sql, true);
                if (!ret.result)
                {
                    throw new Exception("Ошибка при Очистке таблиц схемы charge");
                }
                while (reader.Read())
                {
                    var table_name = reader["table_name"];
                    sql = string.Format("delete from {0} where nzp_kvar = {1} ",
                        string.Format("{0}_charge_{1}", finder.pref, calc_month.year_ % 1000)
                        + tableDelimiter + table_name, finder.nzp_kvar);
                    ret = ExecSQL(connection, transaction, sql, true);
                    if (!ret.result)
                    {
                        throw new Exception("Ошибка при Очистке таблиц схемы charge");
                    }
                }

                #endregion

                #region  пересчитать аналитические таблицы начислений по домам

                sql = string.Format(" select nzp_dom from {0} where nzp_kvar = {1}", centrKvar, finder.nzp_kvar);
                var nzp_dom = Convert.ToInt32(ClassDBUtils.OpenSQL(sql, connection, transaction).resultData.Rows[0][0]);

                var calcfon = new CalcFonTask(Points.GetCalcNum(finder.pref.Trim()))
                {
                    TaskType = CalcFonTask.Types.taskCalcReport,
                    Status = FonTask.Statuses.New,
                    nzp = nzp_dom,
                    month_ = calc_month.month_,
                    year_ = calc_month.year_,
                    pref = finder.pref,
                    nzpt = Points.GetPref(finder.pref)
                };
                var dbCalc = new DbCalcQueueClient();
                ret = dbCalc.AddTask(calcfon);
                if (!ret.result)
                    throw new Exception(ret.text);

                #endregion

                #region Очистка таблиц схемы local_data

                sql = string.Format(" Select distinct c.table_name from " +
                                    " INFORMATION_SCHEMA.columns c where c.table_schema " +
                                    " ilike '%{0}%' and c.COLUMN_NAME = 'num_ls' and c.table_name <> 'kvar'"
                    , string.Format("{0}_data", finder.pref));
                ret = ExecRead(connection, transaction, out reader, sql, true);
                if (!ret.result)
                {
                    throw new Exception("Ошибка при Очистке таблиц схемы local_data");
                }
                while (reader.Read())
                {
                    var table_name = reader["table_name"];
                    sql = string.Format("delete from {0} where num_ls = {1} ",
                        string.Format("{0}_data", finder.pref) + tableDelimiter + table_name, num_ls);
                    ret = ExecSQL(connection, transaction, sql, true);
                    if (!ret.result)
                    {
                        throw new Exception("Ошибка при Очистке таблиц схемы local_data");
                    }
                }
                sql = string.Format(" Select distinct c.table_name from " +
                                    " INFORMATION_SCHEMA.columns c where c.table_schema " +
                                    " ilike '%{0}%' and c.COLUMN_NAME = 'nzp_kvar' and c.table_name <> 'kvar'"
                    , string.Format("{0}_data", finder.pref));
                ret = ExecRead(connection, transaction, out reader, sql, true);
                if (!ret.result)
                {
                    throw new Exception("Ошибка при Очистке таблиц схемы local_data");
                }
                while (reader.Read())
                {
                    var table_name = reader["table_name"];
                    sql = string.Format("delete from {0} where nzp_kvar = {1} ",
                        string.Format("{0}_data", finder.pref) + tableDelimiter + table_name, finder.nzp_kvar);
                    ret = ExecSQL(connection, transaction, sql, true);
                    if (!ret.result)
                    {
                        throw new Exception("Ошибка при Очистке таблиц схемы local_data");
                    }
                }
                sql = string.Format("delete from {0} where nzp = {1} ",
                        string.Format("{0}_data", finder.pref) + tableDelimiter + "prm_1", finder.nzp_kvar);
                ret = ExecSQL(connection, transaction, sql, true);
                if (!ret.result)
                {
                    throw new Exception("Ошибка при Очистке таблиц схемы local_data");
                }
                sql = string.Format("delete from {0} where nzp = {1} ",
                        string.Format("{0}_data", finder.pref) + tableDelimiter + "prm_3", finder.nzp_kvar);
                ret = ExecSQL(connection, transaction, sql, true);
                if (!ret.result)
                {
                    throw new Exception("Ошибка при Очистке таблиц схемы local_data");
                }
                #endregion

                #region Удаление записи лс из локального банка

                sql =
                    string.Format(
                        "INSERT INTO {0} (nzp_kvar, nzp_area, nzp_geu, nzp_dom, nkvar, nkvar_n, num_ls, porch, phone, " +
                        " dat_notp_s, dat_notp_po, fio, ikvar, uch, gil_s, remark, typek, pkod, pkod10) " +
                        " (Select nzp_kvar, nzp_area, nzp_geu, nzp_dom, nkvar, nkvar_n, num_ls, porch, phone, " +
                        " dat_notp_s, dat_notp_po, fio, ikvar, uch, gil_s, remark, typek, pkod, pkod10 from {1} where nzp_kvar = {2})"
                        , locKvarArx, locKvar, finder.nzp_kvar);
                ret = ExecSQL(connection, transaction, sql, true);
                if (!ret.result)
                {
                    throw new Exception("Ошибка при удалении записи лс из локального банка");
                }
                sql = string.Format("delete from {0} where nzp_kvar = {1}", locKvar, finder.nzp_kvar);
                ret = ExecSQL(connection, transaction, sql, true);
                if (!ret.result)
                {
                    throw new Exception("Ошибка при удалении записи лс из локального банка");
                }

                #endregion

                #region Очистка таблиц схемы central_data

                sql = string.Format(" Select distinct c.table_name from " +
                                    " INFORMATION_SCHEMA.columns c where c.table_schema " +
                                    " ilike '%{0}%' and c.COLUMN_NAME = 'num_ls' and c.table_name <> 'kvar' " +
                                    " and c.table_name not like 'peni_%'"
                    , string.Format("{0}_data", Points.Pref));
                ret = ExecRead(connection, transaction, out reader, sql, true);
                if (!ret.result)
                {
                    throw new Exception("Ошибка при Очистке таблиц схемы central_data");
                }
                while (reader.Read())
                {
                    var table_name = reader["table_name"];
                    sql = string.Format("delete from {0} where num_ls::integer = {1} ",
                        string.Format("{0}_data", Points.Pref) + tableDelimiter + table_name, num_ls);
                    ret = ExecSQL(connection, transaction, sql, true);
                    if (!ret.result)
                    {
                        throw new Exception("Ошибка при Очистке таблиц схемы central_data");
                    }
                }
                sql = string.Format(" Select distinct c.table_name from " +
                                    " INFORMATION_SCHEMA.columns c where c.table_schema " +
                                    " ilike '%{0}%' and c.COLUMN_NAME = 'nzp_kvar' and c.table_name <> 'kvar' and c.table_name not like 'peni_%'"
                    , string.Format("{0}_data", Points.Pref));
                ret = ExecRead(connection, transaction, out reader, sql, true);
                if (!ret.result)
                {
                    throw new Exception("Ошибка при Очистке таблиц схемы central_data");
                }
                while (reader.Read())
                {
                    var table_name = reader["table_name"];
                    sql = string.Format("delete from {0} where nzp_kvar::integer = {1} ",
                        string.Format("{0}_data", Points.Pref) + tableDelimiter + table_name, finder.nzp_kvar);
                    ret = ExecSQL(connection, transaction, sql, true);
                    if (!ret.result)
                    {
                        throw new Exception("Ошибка при Очистке таблиц схемы central_data");
                    }
                }

                #endregion

                #region Удаление записи по лс из центрального банка

                sql =
                    string.Format(
                        "INSERT INTO {0} (nzp_kvar, nzp_area, nzp_geu, nzp_dom, nkvar, nkvar_n, num_ls, porch, phone, " +
                        " dat_notp_s, dat_notp_po, fio, ikvar, uch, gil_s, remark, typek, pkod, pkod10) " +
                        " (Select nzp_kvar, nzp_area, nzp_geu, nzp_dom, nkvar, nkvar_n, num_ls, porch, phone, " +
                        " dat_notp_s, dat_notp_po, fio, ikvar, uch, gil_s, remark, typek, pkod, pkod10 from {1} where nzp_kvar = {2})"
                        , centrKvarArx, centrKvar, finder.nzp_kvar);
                ret = ExecSQL(connection, transaction, sql, true);
                if (!ret.result)
                {
                    throw new Exception("Ошибка при удалении записи лс из центрального банка");
                }
                sql = string.Format("delete from {0} where nzp_kvar = {1}", centrKvar, finder.nzp_kvar);
                ret = ExecSQL(connection, transaction, sql, true);
                if (!ret.result)
                {
                    throw new Exception("Ошибка при удалении записи лс из центрального банка");
                }

                #endregion

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return new Returns(false, ex.Message);
            }
            finally
            {
                connection.Close();
            }

            #endregion

            #region обновление данных в выбранном списке л/с
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spls";

            if (TableInWebCashe(conn_web, tXX_spls))
            {
                sql = "delete from " + tXX_spls + " where nzp_kvar = " + finder.nzp_kvar + " and pref = '" + finder.pref + "'";
                ret = ExecSQL(conn_web, sql, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return ret;
                }
            }
            #endregion

            conn_web.Close();
            return ret;
        }
    }
}
