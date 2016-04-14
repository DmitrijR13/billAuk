using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// Класс генерации платежных кодов по УК
    /// </summary>
    public partial class DbAreaPaymentCode : AbstractPaymentCode
    {
        /// <summary>
        /// Имя УК
        /// </summary>
        /// <param name="parentName"></param>
        /// <returns></returns>
        protected override string GetParentName(string parentName)
        {
            return "управляющей организации " + parentName;
        }

        /// <summary>
        /// Условие на ключ УК
        /// </summary>
        /// <param name="parentID"></param>
        /// <returns></returns>
        protected override string GetParentIDCondition(int parentID)
        {
            return " and nzp_area = " + parentID;
        }

        /// <summary>
        /// Подготовка таблицы _preptable с заполнеными полями id, num_ls, nzp_kvar, pref, nzp_area, area
        /// </summary>
        /// <param name="finder">таблица лицевых счетов</param>
        /// <param name="preptable">наименование сформированной таблицы, в которую записываются ЛС и УК, для которых нужно сгенерировать платежный код</param>
        /// <returns></returns>
        protected override Returns FillTableDataForGenerate(string lsTableName, out string preptable)
        {
            Returns ret = Utils.InitReturns();

            preptable = "temptablels";

#if PG
            ExecSQL("set search_path to 'public'", false);
#endif

            ExecSQL("drop table " + preptable, false);

            string sql = "create temp table " + preptable + " ( " +
                " id        serial not null, " +
                " nzp_kvar  integer, " +
                " num_ls    integer, " +
                " pref      varchar(20), " +
                " nzp_area  integer, " +
                " area      varchar(40) " +
                " area_code integer, " +
                " pkod10    integer, " +
                " pkod      " + DBManager.sDecimalType + "(13,0) " +
                ")";
            ExecSQL(sql, true);

            //заполнение temptablels. Таблица включает все ЛС из tXX
            sql = "insert into " + preptable + " (nzp_kvar, num_ls, pref) " +
                " select nzp_kvar, num_ls, pref from " + lsTableName;
            ExecSQL(sql, true);

            //создать индекс на поле nzp_kvar 
            ExecSQL("create index ix_temptablels_1 on " + preptable + " (nzp_kvar)", true);

            // обновить статистику
            ExecSQL(DBManager.sUpdStat + " " + preptable, true);

            // проставить коды УК
            sql = "update " + preptable + " t set " +
                " nzp_area = (select k.nzp_area from " + Points.Pref + "_data" + DBManager.tableDelimiter + "kvar k where k.nzp_kvar = t.nzp_kvar) ";
            ExecSQL(sql, true);

            // проставить названия УК
            sql = "update " + preptable + " t set " +
                " area = (select a.area from " + Points.Pref + "_data" + DBManager.tableDelimiter + "s_area a where a.nzp_area = t.nzp_area) ";
            ExecSQL(sql, true);

            return ret;
        }

        /// <summary>
        /// Получить ключ и название УК для генерации платежного кода
        /// </summary>
        /// <param name="dr"></param>
        /// <param name="req"></param>
        protected override void FillPaymentCodeRequest(DataRow dr, ref PaymentCodeRequest req)
        {
            if (dr["id"] != DBNull.Value) req.keyID = Convert.ToInt32(dr["id"]);
            if (dr["nzp_area"] != DBNull.Value) req.parentID = Convert.ToInt32(dr["nzp_area"]);
            if (dr["area"] != DBNull.Value) req.parentName = Convert.ToString(dr["area"]);
        }

        /// <summary>
        /// В таблицу tlogs записать платежные коды, которые уже есть в kvar
        /// </summary>
        /// <returns></returns>
        protected override Returns PrepareDublicatePaymentCodeTable(string preptable)
        {
            Returns ret = new Returns(true);

            ExecSQL("drop table tlogs", false);

            string sql = " create temp table tlogs (" +
                " num_ls     integer, " +
                " nzp_area   integer, " +
                " area   varchar(40), " +
                " area_code  integer, " +
                " pkod       " + DBManager.sDecimalType + "(13,0)" +
                ")";
            ExecSQL(sql.ToString(), true);

            sql = " insert into tlogs (num_ls, nzp_area, area, area_code, pkod) " +
                " select pt.num_ls, pt.nzp_area, pt.area, pt.area_code, pt.pkod " +
                "from " + preptable + " pt, " +
                Points.Pref + "_data" + DBManager.tableDelimiter + "kvar k " +
                " where k.pkod = pt.pkod";
            ExecSQL(sql.ToString(), true);

            return ret;
        }

        /// <summary>
        /// Сохранить в kvar сгенерированные платежные коды из _preptable, исключая те, которые попали в таблицу tlogs
        /// </summary>
        protected override Returns SavePaymentCode(Finder finder, string preptable)
        {
            Returns ret = new Returns(true);

            // обновление данных в верхнем банке
            string sql = " update " + Points.Pref + "_data" + DBManager.tableDelimiter + "kvar k set " +
                " area_code = (select t.area_code from " + preptable + " t where t.nzp_kvar = k.nzp_kvar), " +
                " pkod10    = (select t.pkod10    from " + preptable + " t where t.nzp_kvar = k.nzp_kvar), " +
                " pkod      = (select t.pkod      from " + preptable + " t where t.nzp_kvar = k.nzp_kvar) " +
                " where exists (select 1 from " + preptable + " t where t.nzp_kvar = k.nzp_kvar) ";

            ExecSQL(sql, true);

            // пройтись по нижним банкам
            string pref = "";

            sql = "select distinct pref from " + preptable;
            IntfResultTableType rt = OpenSQL(sql);
            if (rt.resultCode < 0) throw new Exception(rt.resultMessage);

            for (int prefCnt = 0; prefCnt < rt.resultData.Rows.Count; prefCnt++)
            {
                pref = rt.resultData.Rows[prefCnt]["pref"].ToString();

                sql = " update " + pref + "_data" + DBManager.tableDelimiter + "kvar k set " +
                    " pkod10 = (select t.pkod10    from " + preptable + " t where t.nzp_kvar = k.nzp_kvar), " +
                    " pkod   = (select t.pkod      from " + preptable + " t where t.nzp_kvar = k.nzp_kvar) " +
                    " where exists (select 1 from " + preptable + " t " +
                    "       where t.nzp_kvar = k.nzp_kvar " +
                    "           and t.pref = " + Utils.EStrNull(pref) + ") ";

                ExecSQL(sql, true);
            }

            return ret;
        }

        protected override Returns GetSuccessPaymentCodeList(string preptable, out List<string> listSucces)
        {
            MyDataReader reader = null;
            listSucces = new List<string>();
            Returns ret = new Returns(true);

            try
            {
                string sql = "select num_ls, p.area, area_code, pkod " +
                    " from " + preptable + " a " +
                    " where a.pkod not in (select l.pkod from tlogs l) ";

                ExecRead(out reader, sql.ToString());

                while (reader.Read())
                {
                    listSucces.Add(
                        String.Format("ЛС: {0}, УК: {1}, Активный код УК: {2}, Сгенерированный платежный код: {3}",
                        (reader["num_ls"] == DBNull.Value ? "" : Convert.ToString(reader["num_ls"]).Trim()),
                        (reader["area"] == DBNull.Value ? "" : Convert.ToString(reader["payer"]).Trim()),
                        (reader["area_code"] == DBNull.Value ? "" : Convert.ToString(reader["area_code"]).Trim()),
                        (reader["pkod"] == DBNull.Value ? "" : Convert.ToString(reader["pkod"]).Trim()))
                    );
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }

            return ret;
        }

        /// <summary>
        /// Получить список дублирующихся кодов в таблице kvar
        /// </summary>
        /// <param name="listDuplicate"></param>
        /// <returns></returns>
        protected override Returns GetDublicatePaymentCodeList(out List<string> listDuplicate)
        {
            MyDataReader reader = null;
            listDuplicate = new List<string>();
            Returns ret = new Returns(true);

            try
            {
                string sql = "select a.num_ls, a.area, a.area_code, a.pkod from tlogs a ";

                ExecRead(out reader, sql.ToString());

                while (reader.Read())
                {
                    listDuplicate.Add(
                        String.Format("ЛС: {0}, УК: {1}, Активный код УК: {2}, Дублирующийся платежный код: {3}",
                        (reader["num_ls"] == DBNull.Value ? "" : Convert.ToString(reader["num_ls"]).Trim()),
                        (reader["area"] == DBNull.Value ? "" : Convert.ToString(reader["payer"]).Trim()),
                        (reader["area_code"] == DBNull.Value ? "" : Convert.ToString(reader["area_code"]).Trim()),
                        (reader["pkod"] == DBNull.Value ? "" : Convert.ToString(reader["pkod"]).Trim()))
                    );
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }

            return ret;
        }
    }
}
