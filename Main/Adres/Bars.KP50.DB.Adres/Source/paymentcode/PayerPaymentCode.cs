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
    /// Класс генерации платежных кодов по контрагентам
    /// </summary>
    public partial class DbPayerPaymentCode : AbstractPaymentCode
    {

        /// <summary>
        /// Название контрагента
        /// </summary>
        /// <param name="parentName"></param>
        /// <returns></returns>
        protected override string GetParentName(string parentName)
        {
            return "контрагента " + parentName;
        }

        /// <summary>
        /// Условие на код контрагента
        /// </summary>
        /// <param name="parentID"></param>
        /// <returns></returns>
        protected override string GetParentIDCondition(int parentID)
        {
            return " and nzp_payer = " + parentID;
        }

        /// <summary>
        /// Заполнение объекта PaymentCodeRequest для генерации платежного кода
        /// </summary>
        /// <param name="dr">Строка DataTable</param>
        protected override void FillPaymentCodeRequest(DataRow dr, ref PaymentCodeRequest req)
        {
            if (dr["id"] != DBNull.Value) req.keyID = Convert.ToInt32(dr["id"]);
            if (dr["nzp_payer"] != DBNull.Value) req.parentID = Convert.ToInt32(dr["nzp_payer"]);
            if (dr["payer"] != DBNull.Value) req.parentName = Convert.ToString(dr["payer"]);
        }

        /// <summary>
        /// Подготовка таблицы с заполнеными полями num_ls, nzp_kvar, pref, payer, nzp_payer, is_princip
        /// </summary>
        /// <param name="finder">используется dopFind[0], в котором передается код таблицы с выбранным ЛС</param>
        /// </param>
        /// <returns></returns>
        protected override Returns FillTableDataForGenerate(string lsTableName, out string preptable)
        {
            Returns ret = Utils.InitReturns();

#if PG
            ExecSQL("set search_path to 'public'", false);
#endif

            //таблица на основе которой будут сгенерированы платежные коды
            preptable = "";

            StringBuilder sql = new StringBuilder();
            MyDataReader reader;

            //временная таблица temptablels включающая 
            //num_ls - нужен для комментария
            //nzp_kvar, pref - для идентификации ЛС
            //код агента и принципала, dpd - признак генерировать платежный код принципала
            ExecSQL("drop table temptablels", false);
            ret = ExecSQL("create temp table temptablels (num_ls integer, nzp_kvar integer, pref varchar(20), nzp_payer_agent integer, nzp_payer_princip integer, dpd smallint)", true);
            if (!ret.result) return ret;

            //заполнение temptablels. Таблица включает все ЛС из tXX
            ret = ExecSQL("insert into temptablels (nzp_kvar, num_ls, pref) select nzp_kvar, num_ls, pref from " + lsTableName, true);
            if (!ret.result) return ret;

            //создание индекса на поле nzp_kvar для ускорения работы запросов
            ret = ExecSQL("create index ix_temptablels_1 on temptablels (nzp_kvar)", true);
            if (!ret.result) return ret;

            //цикл по pref для обращения к таблице loc_data.tarif
            sql.Remove(0, sql.Length);
            sql.Append("select distinct pref from temptablels");
            ExecRead(out reader, sql.ToString());
            while (reader.Read())
            {
                string pref = "";
                if (reader["pref"] != DBNull.Value) pref = Convert.ToString(reader["pref"]);

                //создается временная таблица включающая все договора ЛС
                sql.Remove(0, sql.Length);
                sql.Append("drop table tsupp");
                ret = ExecSQL(sql.ToString(), false);
                if (!ret.result) return ret;
                sql.Remove(0, sql.Length);
                sql.Append("select distinct supp.nzp_supp, supp.nzp_payer_agent, supp.nzp_payer_princip, supp.dpd, t.nzp_kvar, s.pref into temp tsupp ");
                sql.AppendFormat("from {0}_data.tarif t, temptablels s, {1}_kernel.supplier supp  ", pref, Points.Pref);
                sql.AppendFormat("where s.pref = '{0}'  and is_actual<>100 ", pref);
                sql.Append("and t.nzp_kvar = s.nzp_kvar and supp.nzp_supp = t.nzp_supp ");
                ret = ExecSQL(sql.ToString(), false);
                if (!ret.result) return ret;

                //обновление temptablels
                //заполнение кодов агентов и принципалов для каждого ЛС, у которого есть договор
                sql.Remove(0, sql.Length);
                sql.Append(" update temptablels set ");
                sql.Append(" nzp_payer_agent = tsupp.nzp_payer_agent, nzp_payer_princip = tsupp.nzp_payer_princip, dpd = tsupp.dpd ");
                sql.AppendFormat(" from tsupp where tsupp.pref = '{0}'  and temptablels.nzp_kvar = tsupp.nzp_kvar ", pref);
                ret = ExecSQL(sql.ToString(), true);
                if (!ret.result) return ret;
            }

            //создание таблицы temptablels2, на основе которой будут сгенерированы плат коды
            preptable = "temptablels2";
            ExecSQL("drop table " + preptable, false);
            ret = ExecSQL(" create temp table " + preptable +
                          " (id serial not null, num_ls integer, nzp_kvar integer, pref varchar(20), payer varchar(40), nzp_payer integer, " +
                           " is_princip integer, area_code integer, pkod10 integer, pkod decimal(13,0))", true);
            if (!ret.result) return ret;

            //в таблицу temptablels2 добавляются все агенты
            sql.Remove(0, sql.Length);
            sql.Append(" insert into " + preptable + " (num_ls, nzp_kvar, pref, nzp_payer, is_princip)");
            sql.Append(" select distinct num_ls, nzp_kvar, pref, nzp_payer_agent, 0 from temptablels");
            ret = ExecSQL(sql.ToString(), true);
            if (!ret.result) return ret;

            //в таблицу temptablels2 добавляются только те принципалы, у которых в договоре установлен признак генерировать плат код
            sql.Remove(0, sql.Length);
            sql.Append(" insert into " + preptable + " (num_ls, nzp_kvar, pref, nzp_payer, is_princip) ");
            sql.Append(" select distinct num_ls, nzp_kvar, pref, nzp_payer_princip, 1 from temptablels ");
            ret = ExecSQL(sql.ToString());
            if (!ret.result) return ret;

            //из таблицы temptablels2 удаляются те записи, которые соответствуют имеющимся
            //платежным кодам в таблице f_data.kvar_pkodes
            sql.Remove(0, sql.Length);
            sql.Append(" delete from " + preptable + " where  exists (");
            sql.AppendFormat(" select 1 from {0}_data{1}kvar_pkodes kp where ", Points.Pref, tableDelimiter);
            sql.AppendFormat(" {0}.nzp_kvar = kp.nzp_kvar and kp.nzp_payer = {0}.nzp_payer and kp.is_default = 1 and kp.is_princip = {0}.is_princip)", preptable);
            ret = ExecSQL(sql.ToString(), true);
            if (!ret.result) return ret;

            // проставить поставщиков
            sql.Remove(0, sql.Length);
            sql.Append("update " + preptable + " t set " +
                " payer = (select p.payer from " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "s_payer p where p.nzp_payer = t.nzp_payer) ");
            ret = ExecSQL(sql.ToString(), true);
            if (!ret.result) return ret;

            return ret;
        }

        /// <summary>
        /// В таблицу tlogs записать платежные коды, которые уже есть в kvarpkodes
        /// </summary>
        /// <returns></returns>
        protected override Returns PrepareDublicatePaymentCodeTable(string preptable)
        {
            Returns ret = new Returns(true);

            ExecSQL("drop table tlogs", false);

            string sql = " create temp table tlogs (" +
                " num_ls     integer, " +
                " is_princip integer, " +
                " payer      varchar(40), " +
                " area_code  integer, " +
                " pkod       " + DBManager.sDecimalType + "(13,0)" +
                " nzp_payer  integer)";
            ExecSQL(sql.ToString(), true);

            sql = " insert into tlogs (num_ls, is_princip, payer, area_code, pkod, nzp_payer) " +
                " select pt.num_ls, pt.is_princip, pt.payer, pt.area_code, pt.pkod, pt.nzp_payer " +
                "from " + preptable + " pt, " +
                Points.Pref + "_data" + DBManager.tableDelimiter + "kvar_pkodes kpk " +
                " where kpk.pkod = pt.pkod";
            ExecSQL(sql.ToString(), true);

            return ret;
        }

        /// <summary>
        /// Сохранить в kvar_pkodes сгенерированные платежные коды из preptable, исключая те, которые попали в таблицу tlogs
        /// </summary>
        protected override Returns SavePaymentCode(Finder finder, string preptable)
        {
            Returns ret = new Returns(true);

            /*#region Определить пользователя
            DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(finder, out ret); //локальный пользователь      
            db.Close();
            #endregion*/

            //Записать в kvar_pkodes сгенерированные платежные коды из preptable, исключая те, которые попали в таблицу tlogs
            string sql = " insert into " + Points.Pref + "_data" + DBManager.tableDelimiter + "kvar_pkodes " +
                " (nzp_kvar, nzp_payer, is_princip, area_code, pkod10, pkod, is_default, changed_on, changed_by) " +
                " select nzp_kvar, nzp_payer, is_princip, area_code, pkod10, pkod, 1, " + DBManager.sCurDateTime + ", " + finder.nzp_user +
                " from " + preptable +
                " where " + DBManager.sNvlWord + "(pkod,0) <> 0 and pkod not in (select pkod from tlogs)";
            ExecSQL(sql.ToString(), true);

            return ret;
        }

        /// <summary>
        /// Получить список cгенерированных и сохраненных платежных кодов
        /// </summary>
        /// <param name="listSucces"></param>
        /// <returns></returns>
        protected override Returns GetSuccessPaymentCodeList(string preptable, out List<string> listSucces)
        {
            //подготовить список listSucces, который запишется в протокол
            //список содержит информацию о сгенерированных и сохраненных в 
            //kvar_pkodes платежных кодах

            MyDataReader reader = null;
            listSucces = new List<string>();
            Returns ret = new Returns(true);

            try
            {
                string sql = "select num_ls,is_princip, p.payer, area_code, pkod " +
                    " from " + preptable + " a, " +
                    Points.Pref + "_kernel" + DBManager.tableDelimiter + "s_payer p " +
                    " where a.pkod not in (select pkod from tlogs) and a.nzp_payer = p.nzp_payer";

                ExecRead(out reader, sql.ToString());

                while (reader.Read())
                {
                    listSucces.Add(
                        String.Format("ЛС: {0}, {1}: {2}, Платежный код контрагента: {3}, Сгенерированный платежный код: {4}",
                        (reader["num_ls"] == DBNull.Value ? "" : Convert.ToString(reader["num_ls"]).Trim()),
                        ((((reader["is_princip"] == DBNull.Value ? 0 : Convert.ToInt32(reader["is_princip"]))) == 1) ? "Принципал" : "Агент"),
                        (reader["payer"] == DBNull.Value ? "" : Convert.ToString(reader["payer"]).Trim()),
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
        /// Получить список платежных кодов, которые дублируются
        /// </summary>
        /// <param name="listSucces"></param>
        /// <returns></returns>
        protected override Returns GetDublicatePaymentCodeList(out List<string> listDuplicate)
        {
            //подготовить список listSucces, который запишется в протокол
            //список содержит информацию о сгенерированных и сохраненных в 
            //kvar_pkodes платежных кодах

            MyDataReader reader = null;
            listDuplicate = new List<string>();
            Returns ret = new Returns(true);

            try
            {
                //подготовить список listError
                //содержит информацию из tlogs
                //дублирование платежных кодов
                string sql = "select a.num_ls, a.is_princip, p.payer, a.area_code, a.pkod " +
                    " from tlogs a, " +
                    Points.Pref + "_kernel" + DBManager.tableDelimiter + "s_payer p " +
                    " where p.nzp_payer=a.nzp_payer";

                ExecRead(out reader, sql.ToString());

                while (reader.Read())
                {
                    listDuplicate.Add(
                        String.Format("ЛС: {0}, {1}: {2}, Платежный код контрагента: {3}, Дублирующийся платежный код: {4}",
                        (reader["num_ls"] == DBNull.Value ? "" : Convert.ToString(reader["num_ls"]).Trim()),
                        ((((reader["is_princip"] == DBNull.Value ? 0 : Convert.ToInt32(reader["is_princip"]))) == 1) ? "Принципал" : "Агент"),
                        (reader["payer"] == DBNull.Value ? "" : Convert.ToString(reader["payer"]).Trim()),
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
