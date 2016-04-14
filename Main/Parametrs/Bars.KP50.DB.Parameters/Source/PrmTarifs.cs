using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DB.Parameters.Source
{
    /// <summary>
    /// Класс работы с тарифами для ЛС/Дома/Поставщика/БД
    /// </summary>
    public class DBPrmTarifs : DataBaseHead
    {

        /// <summary>
        /// Получить список тарифов для разных раскладок
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public Dictionary<TypeTarif, PrmTarifs> GetGroupedTarifsData(PrmTarifFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            var res = new Dictionary<TypeTarif, PrmTarifs>();
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "С ошибками: Не определен код пользователя";
                return res;
            }
            using (var conn_db = GetConnection(Points.GetConnByPref(Points.Pref)))
            {
                #region Проверки
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    ret.text = "С ошибками: Ошибка открытия соединения с базой данных";
                    return res;
                }
                var tableName = string.Format("t{0}_selectedls" + finder.listNumber, finder.nzp_user);
                if (!ExistsTable(conn_db, "public", tableName))
                {
                    ret.result = false;
                    ret.text = "С ошибками: Таблицы с выбранными лицевыми счетами не существует";
                    return res;
                }
                #endregion

                ret = GetAllTarifs(finder, conn_db, ref res);
                if (!ret.result)
                {
                    ret.text = "С ошибками: Ошибка получения данных";
                    res = new Dictionary<TypeTarif, PrmTarifs>();
                    return res;
                }
            }

            return res;
        }

        private Returns GetAllTarifs(PrmTarifFinder finder, IDbConnection conn_db, ref Dictionary<TypeTarif, PrmTarifs> res)
        {
            var ret = Utils.InitReturns();
            foreach (TypeTarif type in Enum.GetValues(typeof(TypeTarif)))
            {
                var item = GetTarifForAny(conn_db, type, finder, out ret);
                if (!ret.result) return ret;
                res.Add(item.Key, item.Value);
            }
            return ret;
        }

        private KeyValuePair<TypeTarif, PrmTarifs> GetTarifForAny(IDbConnection conn_db, TypeTarif type,
            PrmTarifFinder finder, out Returns ret)
        {
            var res = new KeyValuePair<TypeTarif, PrmTarifs>();
            var prmTarif = new PrmTarifs();
            var tableName = string.Format("{1}t{0}_selectedls" + finder.listNumber, finder.nzp_user, sDefaultSchema);



            #region create temp tables

            var tableTarifs = CreateTempTableTarifs(conn_db, out ret);
            if (!ret.result) return res;
            var tableResults = CreateTempTableResults(conn_db, out ret);
            if (!ret.result) return res;

            #endregion create temp tables

            var sql = " SELECT DISTINCT pref FROM " + tableName;
            var ListPrefs = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            foreach (DataRow prefObj in ListPrefs.Rows)
            {
                if (CastValue<string>(prefObj["pref"]).Trim() != finder.pref) continue;
                ret = SwitcherDataPrepare(conn_db, type, finder, prefObj, tableTarifs);
                if (!ret.result) return res;

                ret = SwitcherTarifsDataPrepare(conn_db, type, finder, prefObj, tableTarifs, tableResults);
                if (!ret.result) return res;
            }

            prmTarif.ListServices = GetListServices(conn_db, finder, tableResults);
            prmTarif.ListTarifs = GetListTarifs(conn_db, ref ret, tableResults);

            return new KeyValuePair<TypeTarif, PrmTarifs>(type, prmTarif);
        }

        private List<Tarif> GetListTarifs(IDbConnection conn_db, ref Returns ret, string tableResults)
        {

            var tarifsRows = new List<Tarif>();
            try
            {

                var sql = " SELECT nzp_serv,nzp_supp,nzp_frm,nzp_frm_typ nzp_frm_type,nzp_prm,tarif," +
                          " SUM(count_ls) count_ls,SUM(count_houses) count_houses ," +
                          " MAX(TRIM(name_supp)) as name_supp," +
                          " MAX(TRIM(name_frm)) as name_frm,  " +
                          " MAX(TRIM(service)) as service," +
                          " MAX(TRIM(name_prm)) as name_prm,date_begin as \"DateBegin\",date_end as \"DateEnd\" " +
                          " FROM " + tableResults +
                          " GROUP BY 1,2,3,4,5,6,13,14" +
                          " ORDER BY service,name_supp,name_frm,name_prm,\"DateBegin\",\"DateEnd\"";
                tarifsRows = Query<Tarif>(conn_db, sql, out ret);
                if (!ret.result) return tarifsRows;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Ошибка получения списка тарифов для ведения тарифов", ex);
            }
            return tarifsRows;
        }

        private Dictionary<int, string> GetListServices(IDbConnection conn_db, PrmTarifFinder finder, string tableResults)
        {
            var dictServices = new Dictionary<int, string>();
            try
            {
                var sql = " SELECT nzp_serv,MAX(service) as service" +
                          " FROM " + tableResults +
                          " WHERE " +
                          (finder.show_all ? " 1=1 " : " tarif IS NOT NULL ") +
                          " GROUP BY 1" +
                          " ORDER BY 2";
                var ListServices = ClassDBUtils.OpenSQL(sql, conn_db).resultData.Rows;
                foreach (DataRow serv in ListServices)
                {
                    dictServices.Add(CastValue<int>(serv["nzp_serv"]), CastValue<string>(serv["service"]).Trim());
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Ошибка получения списка услуг для ведения тарифов", ex);
            }
            return dictServices;
        }

        private Returns SwitcherTarifsDataPrepare(IDbConnection conn_db, TypeTarif type, PrmTarifFinder finder, DataRow prefObj,
            string tableTarifs, string tableResults)
        {
            var prmNum = 0;
            switch (type)
            {
                case TypeTarif.Ls: prmNum = 1; break;
                case TypeTarif.House: prmNum = 2; break;
                case TypeTarif.Supplier: prmNum = 11; break;
                case TypeTarif.DataBase: prmNum = 5; break;
            }
            var ret = PrepareTarifsData(conn_db, finder, prefObj, tableTarifs, tableResults, prmNum);
            if (!ret.result) return ret;
            return ret;
        }

        private Returns PrepareTarifsData(IDbConnection conn_db, PrmTarifFinder finder, DataRow prefObj,
            string tableTarifs, string tableResults, int prmNum)
        {
            var pref = CastValue<string>(prefObj["pref"]).Trim();
            var nzp_wp = Points.GetPoint(pref).nzp_wp;
            var fieldKey = "";
            switch (prmNum)
            {
                case 1: fieldKey = "p1.nzp = t.nzp_kvar"; break;
                case 2: fieldKey = "p1.nzp = t.nzp_dom"; break;
                case 5: fieldKey = "1=1"; break;
                case 11: fieldKey = "p1.nzp = t.nzp_supp"; break;
            }

            var sql = " INSERT INTO " + tableResults + "(nzp_serv,nzp_supp,nzp_frm,nzp_frm_typ,nzp_prm,tarif," +
                      " count_ls,count_houses,name_supp,name_frm,service,name_prm,date_begin,date_end)" +
                      " SELECT t.nzp_serv,sp.nzp_supp,t.nzp_frm,t.nzp_frm_typ, " +
                      " t.nzp_prm_tarif as nzp_prm ," +
                      " CAST(p1.val_prm as numeric) as tarif," +
                      " COUNT(DISTINCT t.nzp_kvar) as count_ls,COUNT(DISTINCT t.nzp_dom) as count_houses," +
                      " MAX(sp.name_supp) as name_supp," +
                      " MAX(ff.name_frm) as name_frm,  " +
                      " MAX(s.service) as service," +
                      " MAX(p.name_prm) as name_prm, p1.dat_s,p1.dat_po " +
                      " FROM " +
                      Points.Pref + sKernelAliasRest + "services s, " +
                      Points.Pref + sKernelAliasRest + "formuls ff, " +
                      Points.Pref + sKernelAliasRest + "prm_name p, " +
                      Points.Pref + sKernelAliasRest + "supplier sp, " +
                      tableTarifs + " t" +
                      " LEFT OUTER JOIN " + pref + sDataAliasRest + "prm_" + prmNum +
                      " p1 ON " + fieldKey + " AND p1.nzp_prm=t.nzp_prm_tarif " +
                      " AND p1.is_actual<>100 " +
                      " AND p1.dat_s<=" + Utils.EStrNull(finder.date_to.ToShortDateString()) +
                      " AND p1.dat_po>=" + Utils.EStrNull(finder.date_from.ToShortDateString()) +
                      " WHERE t.nzp_frm=ff.nzp_frm AND t.nzp_serv=s.nzp_serv AND t.nzp_prm_tarif=p.nzp_prm " +
                      (finder.show_all ? "" : " AND p1.val_prm IS NOT NULL ") +
                      " AND t.nzp_wp=" + nzp_wp +
                      " AND t.nzp_supp=sp.nzp_supp" +
                      " GROUP BY 1,2,3,4,5,6,13,14";
            var ret = ExecSQL(conn_db, sql, true);
            return ret;
        }

        /// <summary>
        /// Подготавливает данные в зависимости от типа вкладки 
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="type"></param>
        /// <param name="finder"></param>
        /// <param name="prefObj"></param>
        /// <param name="tableTarifs"></param>
        /// <param name="addWhere"></param>
        /// <param name="nzpPrm"></param>
        /// <returns></returns>
        private Returns SwitcherDataPrepare(IDbConnection conn_db, TypeTarif type, PrmTarifFinder finder, DataRow prefObj,
            string tableTarifs, string addWhere = "", int nzpPrm = 0)
        {
            Returns ret = Utils.InitReturns();
            string whereFrmType;
            string prmTarifField;
            var pref = CastValue<string>(prefObj["pref"]).Trim();
            switch (type)
            {
                case TypeTarif.Ls:
                    {
                        #region
                        whereFrmType = "nzp_frm_typ not in (312,912,2) " + addWhere;
                        prmTarifField = "nzp_prm_tarif_ls";
                        if (nzpPrm > 0) whereFrmType += " AND " + prmTarifField + "=" + nzpPrm;
                        ret = PrepareDataParams(conn_db, finder, pref, tableTarifs, whereFrmType, prmTarifField, type);
                        if (!ret.result) return ret;

                        //для этого типа тарифов получаем данные если на лс есть параметр "Ставки ЭОТ по лиц.счетам"
                        whereFrmType = "nzp_frm_typ=2 " + addWhere;
                        whereFrmType += string.Format(" AND EXISTS (SELECT 1 FROM {0}prm_1 p1 " +
                                                      " WHERE p1.nzp=t.nzp_kvar AND p1.nzp_prm=335 " +
                                                      " AND p1.is_actual<>100 AND p1.dat_s<={1} AND p1.dat_po>={2})",
                            pref + sDataAliasRest, Utils.EStrNull(finder.date_to.ToShortDateString()),
                            Utils.EStrNull(finder.date_from.ToShortDateString()));
                        prmTarifField = "nzp_prm_tarif_ls";
                        if (nzpPrm > 0) whereFrmType += " AND " + prmTarifField + "=" + nzpPrm;
                        ret = PrepareDataParams(conn_db, finder, pref, tableTarifs, whereFrmType, prmTarifField, type);
                        if (!ret.result) return ret;

                        whereFrmType = "nzp_frm_typ in (312,912)" + addWhere;
                        prmTarifField = "nzp_prm_tarif_dm";
                        if (nzpPrm > 0) whereFrmType += " AND " + prmTarifField + "=" + nzpPrm;
                        ret = PrepareDataParams(conn_db, finder, pref, tableTarifs, whereFrmType, prmTarifField, type);
                        if (!ret.result) return ret;

                        whereFrmType = "nzp_frm_typ in (312,912)" + addWhere;
                        prmTarifField = "nzp_prm_tarif_su";
                        if (nzpPrm > 0) whereFrmType += " AND " + prmTarifField + "=" + nzpPrm;
                        ret = PrepareDataParams(conn_db, finder, pref, tableTarifs, whereFrmType, prmTarifField, type);
                        if (!ret.result) return ret;
                        break;
                        #endregion
                    }
                case TypeTarif.House:
                    {
                        #region
                        whereFrmType = "nzp_frm_typ not in (312,912 ,12,412)" + addWhere;
                        prmTarifField = "nzp_prm_tarif_dm";
                        if (nzpPrm > 0) whereFrmType += " AND " + prmTarifField + "=" + nzpPrm;
                        ret = PrepareDataParams(conn_db, finder, pref, tableTarifs, whereFrmType, prmTarifField, type);
                        if (!ret.result) return ret;

                        whereFrmType = "nzp_frm_typ in (26)" + addWhere;
                        prmTarifField = "nzp_prm_tarif_su";
                        if (nzpPrm > 0) whereFrmType += " AND " + prmTarifField + "=" + nzpPrm;
                        ret = PrepareDataParams(conn_db, finder, pref, tableTarifs, whereFrmType, prmTarifField, type);
                        if (!ret.result) return ret;

                        break;
                        #endregion
                    }
                case TypeTarif.Supplier:
                    {
                        #region
                        //если включен параметр "Ставки ЭОТ по поставщикам"
                        if (DBManager.GetParamValueInPeriod<bool>(conn_db, pref, 336, 5, finder.date_from, finder.date_to, out ret))
                        {
                            whereFrmType = "nzp_frm_typ not in (312,912 ,12,412 ,26)" + addWhere;
                            prmTarifField = "nzp_prm_tarif_su";
                            if (nzpPrm > 0) whereFrmType += " AND " + prmTarifField + "=" + nzpPrm;
                            ret = PrepareDataParams(conn_db, finder, pref, tableTarifs, whereFrmType, prmTarifField, type);
                            if (!ret.result) return ret;
                        }
                        if (!ret.result) return ret;
                        break;
                        #endregion
                    }
                case TypeTarif.DataBase:
                    {
                        #region
                        whereFrmType = "nzp_frm_typ not in (312,912 ,12,412 ,26)" + addWhere;
                        prmTarifField = "nzp_prm_tarif_bd";
                        if (nzpPrm > 0) whereFrmType += " AND " + prmTarifField + "=" + nzpPrm;
                        ret = PrepareDataParams(conn_db, finder, pref, tableTarifs, whereFrmType, prmTarifField, type);
                        if (!ret.result) return ret;

                        whereFrmType = "nzp_frm_typ in (12,412)" + addWhere;
                        prmTarifField = "nzp_prm_tarif_dm";
                        if (nzpPrm > 0) whereFrmType += " AND " + prmTarifField + "=" + nzpPrm;
                        ret = PrepareDataParams(conn_db, finder, pref, tableTarifs, whereFrmType, prmTarifField, type);
                        if (!ret.result) return ret;

                        whereFrmType = "nzp_frm_typ in (12,412)" + addWhere;
                        prmTarifField = "nzp_prm_tarif_su";
                        if (nzpPrm > 0) whereFrmType += " AND " + prmTarifField + "=" + nzpPrm;
                        ret = PrepareDataParams(conn_db, finder, pref, tableTarifs, whereFrmType, prmTarifField, type);
                        if (!ret.result) return ret;
                        break;
                        #endregion
                    }
            }
            return ret;
        }



        private Returns PrepareDataParams(IDbConnection conn_db, PrmTarifFinder finder, string pref,
            string tableTarifs, string whereFrmType, string prmTarifField, TypeTarif type)
        {
            Returns ret = Utils.InitReturns();
            var nzp_wp = Points.GetPoint(pref).nzp_wp;
            var tableName = string.Format("{1}t{0}_selectedls" + finder.listNumber, finder.nzp_user, sDefaultSchema);
            var serviceFilter = finder.nzp_servs[type];
            var sql = " INSERT INTO " + tableTarifs +
                         " (nzp_wp,nzp_dom,nzp_kvar,nzp_serv,nzp_supp,nzp_frm,nzp_frm_typ,nzp_prm_tarif)" +
                         " SELECT  " + nzp_wp +
                         ", k.nzp_dom,k.nzp_kvar,t.nzp_serv,t.nzp_supp,f.nzp_frm,f.nzp_frm_typ,f." + prmTarifField  +
                         " FROM  " + pref + sDataAliasRest + "tarif t, " + pref + sKernelAliasRest +
                         "formuls_opis f, " +
                         tableName + " k " +
                         " WHERE k.nzp_kvar=t.nzp_kvar and f.nzp_frm=t.nzp_frm and k.pref = '" +finder.pref.Trim() + "' " +
                          (serviceFilter > 0 ? " AND t.nzp_serv=" + serviceFilter : " ") +
                         " AND f." + whereFrmType + " AND f." + prmTarifField + ">0 " +
                         " AND t.is_actual<>100 AND t.dat_s<=" + Utils.EStrNull(finder.date_to.ToShortDateString()) +
                         " AND t.dat_po>=" + Utils.EStrNull(finder.date_from.ToShortDateString()) +
                         " GROUP BY 2,3,4,5,6,7,8";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return ret;
            }
            return ret;
        }


        /// Проверка на существование таблицы 
        /// <param name="conn_db"></param>
        /// <param name="Schema"></param>
        /// <param name="TableName"></param>
        /// <returns></returns>
        private bool ExistsTable(IDbConnection conn_db, string Schema, string TableName)
        {
            Returns ret;
            return ExecScalar<int>(conn_db, " SELECT COUNT(1) FROM information_schema.tables " +
                                            " WHERE LOWER(table_schema)=" + Utils.EStrNull(Schema.ToLower()) +
                                            " AND LOWER(table_name)=" + Utils.EStrNull(TableName.ToLower())
                                            , out ret, true) > 0;

        }

        /// <summary>
        /// Сохранение новых тарифов
        /// </summary>
        /// <param name="newTarifs"></param>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public Returns SetGroupedTarifsData(Dictionary<TypeTarif, PrmTarifs> newTarifs,
            PrmTarifFinder finder)
        {
            var ret = Utils.InitReturns();
            var res = new Dictionary<TypeTarif, PrmTarifs>();
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "С ошибками: Не определен код пользователя";
                return ret;
            }
            if (finder.date_from > finder.date_to)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "С ошибками: Дата \"с\" не может быть больше даты \"по\"";
                return ret;
            }
            using (var conn_db = GetConnection(Points.GetConnByPref(Points.Pref)))
            {
                #region Проверки
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    ret.result = false;
                    ret.tag = -1;
                    ret.text = "С ошибками: Ошибка открытия соединения с базой данных";
                    return ret;
                }
                var tableName = string.Format("t{0}_selectedls" + finder.listNumber, finder.nzp_user);
                if (!ExistsTable(conn_db, "public", tableName))
                {
                    ret.tag = -1;
                    ret.result = false;
                    ret.text = "С ошибками: Таблицы с выбранными лицевыми счетами не существует";
                    return ret;
                }
                if (newTarifs.SelectMany(typeTarif => typeTarif.Value.ListTarifs).
                    Any(newTarif => newTarif.nzp_serv == 0 ||
                                    newTarif.nzp_supp == 0 ||
                                    newTarif.nzp_frm == 0 ||
                                    newTarif.nzp_prm == 0 ||
                                    newTarif.nzp_frm_type == 0))
                {
                    ret.result = false;
                    ret.tag = -1;
                    ret.text = "С ошибками: Не определен один из ключевых параметров";
                    return ret;
                }

                #endregion

                //сохраняем изменения 
                ret = SetAllTarifs(conn_db, finder, newTarifs);
                if (!ret.result)
                {
                    ret.tag = -1;
                    ret.text = "С ошибками: Ошибка сохранения данных";
                    res = new Dictionary<TypeTarif, PrmTarifs>();
                    return ret;
                }
            }
            return ret;
        }

        private Returns SetAllTarifs(IDbConnection conn_db, PrmTarifFinder finder, Dictionary<TypeTarif, PrmTarifs> newTarifs)
        {
            var ret = Utils.InitReturns();
            foreach (TypeTarif type in Enum.GetValues(typeof(TypeTarif)))
            {
                ret = SetTarifForAny(conn_db, type, newTarifs[type], finder);
                if (!ret.result) return ret;
            }
            return ret;
        }


        private Returns SetTarifForAny(IDbConnection conn_db, TypeTarif type, PrmTarifs newTarifs, PrmTarifFinder finder)
        {
            var ret = Utils.InitReturns();
            var tableName = string.Format("{1}t{0}_selectedls" + finder.listNumber, finder.nzp_user, sDefaultSchema);
            var tableTarifs = "";

            var sql = " SELECT DISTINCT pref FROM " + tableName;
            var ListPrefs = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            foreach (DataRow prefObj in ListPrefs.Rows)
            {
                if (CastValue<string>(prefObj["pref"]).Trim() != finder.pref) continue;
                foreach (var newTarif in newTarifs.ListTarifs)
                {
                    try
                    {

                        //создали таблицу для текущего префикса
                        tableTarifs = CreateTempTableTarifs(conn_db, out ret);
                        if (!ret.result) return ret;
                        //ограничиваем выборку 
                        var where = string.Format(" AND t.nzp_serv={0} AND t.nzp_supp={1} AND f.nzp_frm={2} AND f.nzp_frm_typ={3}",
                            newTarif.nzp_serv, newTarif.nzp_supp, newTarif.nzp_frm, newTarif.nzp_frm_type);
                        //выбрали данные для работы с текущим префиксом и с доп.условиями
                        ret = SwitcherDataPrepare(conn_db, type, finder, prefObj, tableTarifs, where, newTarif.nzp_prm);
                        if (!ret.result) return ret;
                        //сохраняем данные
                        ret = SwitcherSetTarifs(conn_db, type, finder, newTarif, prefObj, tableTarifs);
                        if (!ret.result) return ret;
                    }
                    finally
                    {
                        DropTempTable(conn_db, tableTarifs);
                    }
                }
            }

            return ret;
        }

        private Returns DropTempTable(IDbConnection conn_db, string tableName)
        {
            return ExecSQL(conn_db, "DROP TABLE " + tableName);
        }

        private string CreateTempTableTarifs(IDbConnection conn_db, out Returns ret)
        {
            var tableTarifs = "temp_table_tarifs_" + DateTime.Now.Ticks;

            ExecSQL(conn_db, "DROP TABLE " + tableTarifs, false);
            var sql = " CREATE TEMP TABLE " + tableTarifs + " " +
                      " (nzp_wp integer," +
                      " nzp_dom integer, " +
                      " nzp_kvar integer," +
                      " nzp_serv integer, " +
                      " nzp_supp integer, " +
                      " nzp_frm integer, " +
                      " nzp_frm_typ integer," +
                      " nzp_prm_tarif integer)";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) return tableTarifs;

            ret = ExecSQL(conn_db,
                " CREATE INDEX ix1_" + tableTarifs + " ON " + tableTarifs +
                "(nzp_kvar,nzp_serv,nzp_supp,nzp_frm,nzp_prm_tarif,nzp_wp)");
            if (!ret.result) return tableTarifs;
            return tableTarifs;
        }

        private string CreateTempTableResults(IDbConnection conn_db, out Returns ret)
        {
            var tableResults = "temp_table_results_" + DateTime.Now.Ticks;
            var sql = " CREATE TEMP TABLE " + tableResults + " (" +
                   " nzp_serv integer, " +
                   " nzp_supp integer, " +
                   " nzp_frm integer, " +
                   " nzp_frm_typ integer," +
                   " nzp_prm integer," +
                   " tarif numeric," +
                   " count_ls integer," +
                   " count_houses integer," +
                   " name_supp varchar," +
                   " name_frm varchar," +
                   " service varchar," +
                   " name_prm varchar," +
                   " date_begin date," +
                   " date_end date" +
                   ")";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) return tableResults;
            ret = ExecSQL(conn_db,
                " CREATE INDEX ix1_" + tableResults + " ON " + tableResults + "(nzp_serv,nzp_supp,nzp_frm,nzp_frm_typ,nzp_prm,tarif)");
            if (!ret.result) return tableResults;
            return tableResults;
        }

        private Returns SwitcherSetTarifs(IDbConnection conn_db, TypeTarif type, PrmTarifFinder finder,
            Tarif newTarif, DataRow prefObj, string tableTarifs)
        {
            var prmNum = 0;
            switch (type)
            {
                case TypeTarif.Ls: prmNum = 1; break;
                case TypeTarif.House: prmNum = 2; break;
                case TypeTarif.Supplier: prmNum = 11; break;
                case TypeTarif.DataBase: prmNum = 5; break;
            }
            var ret = SetTarifs(conn_db, finder, prefObj, newTarif, tableTarifs, prmNum);
            if (!ret.result) return ret;
            return ret;
        }



        private Returns SetTarifs(IDbConnection conn_db, PrmTarifFinder finder, DataRow prefObj,
            Tarif newTarif, string tableTarifs, int prmNum)
        {
            var pref = CastValue<string>(prefObj["pref"]).Trim();
            var ret = Utils.InitReturns();
            var fieldKey = "";
            var prmTable = pref + sDataAliasRest + "prm_" + prmNum;
            var date_from = Utils.EStrNull(finder.date_from.ToShortDateString());
            var date_to = Utils.EStrNull(finder.date_to.ToShortDateString());
            var nzp = "";
            switch (prmNum)
            {
                case 1: fieldKey = "p.nzp = t.nzp_kvar";
                    nzp = "nzp_kvar";
                    break;
                case 2: fieldKey = "p.nzp = t.nzp_dom";
                    nzp = "nzp_dom";
                    break;
                case 5: fieldKey = "1=1";
                    nzp = "0"; break;
                case 11: fieldKey = "p.nzp = t.nzp_supp";
                    nzp = "nzp_supp";
                    break;
            }

            //ничего не делать
            if (!newTarif.tarif.HasValue && !newTarif.new_tarif.HasValue)
            {
                return ret;
            }


            //из-за того, что postgres не соблюдает порядок join условий и валится на CAST as NUMERIC приходится делать так:
            var preparedPrmTable = "temp_prepared_prm_table_" + DateTime.Now.Ticks;

            //те, у кого вообще установлено хоть какое то значение параметра в выбранном периоде
            var sql = " CREATE TEMP TABLE " + preparedPrmTable + " AS " +
                         " SELECT t.nzp_dom,t.nzp_kvar,t.nzp_supp, t.nzp_prm_tarif, t.nzp_serv, p.val_prm::numeric" +
                         " FROM " + prmTable + " p JOIN " + tableTarifs + " t ON t.nzp_prm_tarif=p.nzp_prm AND " + fieldKey +
                         " WHERE p.is_actual<>100 " +
                         " AND p.dat_s<=" + date_to + " AND p.dat_po>=" + date_from;
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) return ret;

            ret = ExecSQL(conn_db,
                " CREATE INDEX ix1_" + preparedPrmTable + " ON " + preparedPrmTable +
                " (val_prm)", true);
            if (!ret.result) return ret;
            ret = ExecSQL(conn_db,
                " CREATE INDEX ix2_" + preparedPrmTable + " ON " + preparedPrmTable +
                " (nzp_kvar)", true);
            if (!ret.result) return ret;

            //4 ВАРИАНТА ПЕРЕСЕЧЕНИЙ
            #region Работа с периодами

            if (newTarif.tarif.HasValue)
            {
                #region Вытаскиваем именно те лс у которых установлено нужное значение параметра

                //ограничиваем выборку лицевыми счетам  у которых действует данный тариф в выбранном периоде
                var preparedTableTarifs = "temp_table_prep_" + DateTime.Now.Ticks;
                sql = " CREATE TEMP TABLE " + preparedTableTarifs + " AS " +
                      " SELECT t.nzp_dom,t.nzp_kvar,t.nzp_supp, t.nzp_prm_tarif" +
                      " FROM " + preparedPrmTable + " t " +
                      " WHERE t.val_prm=" + newTarif.tarif +
                      " GROUP BY 1,2,3,4";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;
                
                #endregion

                #region indexes

                ret = ExecSQL(conn_db,
                    " CREATE INDEX ix1_" + preparedTableTarifs + " ON " + preparedTableTarifs +
                    " (nzp_dom,nzp_prm_tarif)", true);
                if (!ret.result) return ret;
                ret = ExecSQL(conn_db,
                    " CREATE INDEX ix2_" + preparedTableTarifs + " ON " + preparedTableTarifs +
                    " (nzp_kvar,nzp_prm_tarif)", true);
                if (!ret.result) return ret;
                ret = ExecSQL(conn_db,
                    " CREATE INDEX ix3_" + preparedTableTarifs + " ON " + preparedTableTarifs +
                    " (nzp_supp,nzp_prm_tarif)", true);
                if (!ret.result) return ret;

                #endregion indexes


                //1) Новый период входит в уже существующий
                //      |-------------|
                //--------------------------------

                #region

                var tableFirstVariant = "temp_table_first" + DateTime.Now.Ticks;
                sql = " CREATE TEMP TABLE " + tableFirstVariant + " AS " +
                      " SELECT p.nzp_key,p.nzp,p.nzp_prm,p.dat_s,p.dat_po,p.val_prm," +
                      " p.nzp_user,p.dat_when,p.dat_del,p.user_del" +
                      " FROM " + prmTable + " p, " + preparedTableTarifs + " t" +
                      " WHERE " + fieldKey +
                      " AND p.is_actual<>100 " +
                      " AND t.nzp_prm_tarif=p.nzp_prm " +
                      " AND p.dat_s<" + date_from + " AND p.dat_po>" + date_to;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                ret = ExecSQL(conn_db,
                    " CREATE INDEX ix1_" + tableFirstVariant + " ON " + tableFirstVariant +
                    " (nzp_key,nzp)", true);
                if (!ret.result) return ret;
                
                //1.1) обрезаем старый период ----]
                sql = " UPDATE " + prmTable + " p" +
                      " SET dat_po = " + date_from + "::DATE - INTERVAL '1 DAY', " +
                      " dat_when=now(), " +
                      " nzp_user=" + finder.nzp_user +
                      " FROM " + tableFirstVariant + " t " +
                      " WHERE p.nzp_key=t.nzp_key AND p.nzp=t.nzp ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                //1.2)вставляем старое значение [-/-/-/-] с is_actual=100 и новым периодом - для истории
                sql = " INSERT INTO " + prmTable +
                      "(nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when,dat_del,user_del)" +
                      " SELECT DISTINCT ON (nzp,nzp_prm,val_prm,nzp_user,dat_when)" +
                      " nzp,nzp_prm," + date_from + "," + date_to + ",val_prm, 100 is_actual, " +
                      " nzp_user,dat_when,now() dat_del, " + finder.nzp_user + " user_del" +
                      " FROM " + tableFirstVariant;

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                //1.3) обрезаем старый период в буфере [----
                sql = " UPDATE " + tableFirstVariant + " SET dat_s = " + date_to + "::DATE + INTERVAL '1 DAY'";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                //1.4)//вставляем второй кусок старого периода [-----
                sql = " INSERT INTO " + prmTable +
                      "(nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when)" +
                      " SELECT DISTINCT ON (nzp,nzp_prm,val_prm)" +
                      " nzp,nzp_prm,dat_s,dat_po,val_prm,1, " +
                      finder.nzp_user + " nzp_user, now() dat_when " +
                      " FROM " + tableFirstVariant +
                      " WHERE dat_s<=dat_po";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                DropTempTable(conn_db, tableFirstVariant);

                #endregion

                //2) Cтарый период входит в новый или равен ему
                //      |-------------|
                //      ---------------
                //          ------

                #region

                var tableSecondVariant = "temp_table_second_" + DateTime.Now.Ticks;
                sql = " CREATE TEMP TABLE " + tableSecondVariant + " AS " +
                      " SELECT p.nzp_key,p.nzp " +
                      " FROM " + prmTable + " p, " + preparedTableTarifs + " t" +
                      " WHERE " + fieldKey +
                      " AND p.is_actual<>100 " +
                      " AND t.nzp_prm_tarif=p.nzp_prm " +
                      " AND p.dat_s>=" + date_from + " AND p.dat_po<=" + date_to;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                ret = ExecSQL(conn_db,
                    " CREATE INDEX ix1_" + tableSecondVariant + " ON " + tableSecondVariant +
                    " (nzp_key,nzp)", true);
                if (!ret.result) return ret;
               
                //2.1) удаляем старый период
                sql = " UPDATE " + prmTable + " p" +
                      " SET is_actual=100 , " +
                      " dat_del=now(), " +
                      " user_del=" + finder.nzp_user +
                      " FROM " + tableSecondVariant + " t " +
                      " WHERE p.nzp_key=t.nzp_key AND p.nzp=t.nzp ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                DropTempTable(conn_db, tableSecondVariant);

                #endregion


                //3) Новый период пересекается с существующим 
                //      |-------------|
                //-----------
                //--------------------          

                #region

                var tableThirdVariant = "temp_table_third" + DateTime.Now.Ticks;
                sql = " CREATE TEMP TABLE " + tableThirdVariant + " AS " +
                      " SELECT p.nzp_key,p.nzp,p.nzp_prm,p.dat_s,p.dat_po,p.val_prm," +
                      " p.nzp_user,p.dat_when,p.dat_del,p.user_del" +
                      " FROM " + prmTable + " p, " + preparedTableTarifs + " t" +
                      " WHERE " + fieldKey +
                      " AND p.is_actual<>100 " +
                      " AND t.nzp_prm_tarif=p.nzp_prm " +
                      " AND p.dat_s<" + date_from + " AND p.dat_po<=" + date_to +
                      " AND p.dat_po>=" + date_from;                      
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                ret = ExecSQL(conn_db,
                    " CREATE INDEX ix1_" + tableThirdVariant + " ON " + tableThirdVariant +
                    " (nzp_key,nzp)", true);
                if (!ret.result) return ret;
                
                //3.1) обрезаем старый период ----]
                sql = " UPDATE " + prmTable + " p" +
                      " SET dat_po = " + date_from + "::DATE - INTERVAL '1 DAY', " +
                      " dat_when=now(), " +
                      " nzp_user=" + finder.nzp_user +
                      " FROM " + tableThirdVariant + " t " +
                      " WHERE p.nzp_key=t.nzp_key AND p.nzp=t.nzp ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                //3.2)вставляем старое значение [-/-/- с is_actual=100 с обрезанным периодом
                sql = " INSERT INTO " + prmTable +
                      "(nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when,dat_del,user_del)" +
                      " SELECT DISTINCT ON (nzp,nzp_prm,val_prm,dat_po)" +
                      "  nzp,nzp_prm," + date_from + ",dat_po,val_prm, 100 is_actual, " +
                      " nzp_user,dat_when,now() dat_del, " + finder.nzp_user + " user_del" +
                      " FROM " + tableThirdVariant +
                      " WHERE " + date_from + " <=dat_po";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                DropTempTable(conn_db, tableThirdVariant);

                #endregion

                //4) Новый период пересекается с существующим 
                //      |-------------|
                //              -----------
                //      --------------------          

                #region

                var tableFourthVariant = "temp_table_fourth" + DateTime.Now.Ticks;
                sql = " CREATE TEMP TABLE " + tableFourthVariant + " AS " +
                      " SELECT p.nzp_key,p.nzp,p.nzp_prm,p.dat_s,p.dat_po,p.val_prm," +
                      " p.nzp_user,p.dat_when,p.dat_del,p.user_del" +
                      " FROM " + prmTable + " p, " + preparedTableTarifs + " t" +
                      " WHERE " + fieldKey +
                      " AND p.is_actual<>100 " +
                      " AND t.nzp_prm_tarif=p.nzp_prm " +
                      " AND p.dat_s>=" + date_from + " AND p.dat_po>" + date_to +
                      " AND p.dat_s<=" + date_to;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                ret = ExecSQL(conn_db,
                    " CREATE INDEX ix1_" + tableFourthVariant + " ON " + tableFourthVariant +
                    " (nzp_key,nzp)", true);
                if (!ret.result) return ret;

                //3.1) обрезаем старый период ----]
                sql = " UPDATE " + prmTable + " p" +
                      " SET dat_s = " + date_to + "::DATE + INTERVAL '1 DAY', " +
                      " dat_when=now(), " +
                      " nzp_user=" + finder.nzp_user +
                      " FROM " + tableFourthVariant + " t " +
                      " WHERE p.nzp_key=t.nzp_key AND p.nzp=t.nzp ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                //3.2)вставляем старое значение -/-/-] с is_actual=100 с обрезанным периодом
                sql = " INSERT INTO " + prmTable +
                      "(nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when,dat_del,user_del)" +
                      " SELECT DISTINCT ON (nzp,nzp_prm,val_prm,dat_s)" +
                      "  nzp,nzp_prm,dat_s," + date_to + ",val_prm, 100 is_actual, " +
                      " nzp_user,dat_when,now() dat_del, " + finder.nzp_user + " user_del" +
                      " FROM " + tableFourthVariant +
                      " WHERE dat_s<=" + date_to;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;


                DropTempTable(conn_db, tableFourthVariant);

                #endregion

                //если не удаление и было старое значение
                if (newTarif.new_tarif.HasValue)
                {
                    //5) вставляем новое значение  [-----]
                    sql = " INSERT INTO " + prmTable +
                          "(nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when)" +
                          " SELECT DISTINCT " + nzp + "," + newTarif.nzp_prm + "," + date_from + "::date," + date_to + "::date," +
                          newTarif.new_tarif + ", 1 is_actual, " +
                          finder.nzp_user + "nzp_user,now() dat_when" +
                          " FROM " + preparedTableTarifs;
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) return ret;
                }

                ret = PrepareDataForMustCalc(conn_db, finder, newTarif, pref, preparedTableTarifs);
                if (!ret.result) return ret;
            }
            else
            {

                //5) вставляем новое значение  [-----]
                //если не удаление и не было старого периода

                var where = " WHERE NOT EXISTS " +
                     "     ( SELECT 1 FROM " + preparedPrmTable + " ex WHERE ex.nzp_kvar=a.nzp_kvar)";
                sql = " INSERT INTO " + prmTable +
                      "(nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,nzp_user,dat_when)" +
                      " SELECT DISTINCT " + nzp + "," + newTarif.nzp_prm + "," + date_from + "::date," + date_to + "::date," +
                      newTarif.new_tarif + ", 1 is_actual, " +
                      finder.nzp_user + "nzp_user,now() dat_when" +
                      " FROM " + tableTarifs + " a " +
                      where;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                ret = PrepareDataForMustCalc(conn_db, finder, newTarif, pref, tableTarifs, where);
                if (!ret.result) return ret;
            }
            #endregion Работа с периодами

            return ret;
        }

        private Returns PrepareDataForMustCalc(IDbConnection conn_db, PrmTarifFinder finder,
            Tarif newTarif, string pref, string TableFrom, string where = "")
        {
            var ret = Utils.InitReturns();
            using (var db = new DbEditInterData())
            {
                //текущий расчетный месяц
                var prm = new CalcMonthParams();
                prm.pref = pref;
                var rec = Points.GetCalcMonth(prm);
                var preparedTableForMustCalc = "temp_for_mustcalc_" + DateTime.Now.Ticks;
                var sql = " CREATE TEMP TABLE " + preparedTableForMustCalc + " AS " +
                          " SELECT DISTINCT nzp_kvar" +
                          " FROM " + TableFrom + " a " +
                          " " + where;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                ret = ExecSQL(conn_db,
                   " CREATE INDEX ix1_" + preparedTableForMustCalc + " ON " + preparedTableForMustCalc + " (nzp_kvar)", true);
                if (!ret.result) return ret;

                var mustCalcFinder = new MustCalc()
                {
                    pref = pref,
                    dat_s = finder.date_from.ToShortDateString(),
                    dat_po = finder.date_to.ToShortDateString(),
                    nzp_serv = newTarif.nzp_serv,
                    nzp_supp = newTarif.nzp_supp,
                    nzp_user = finder.nzp_user,
                    month_ = rec.month_,
                    year_ = rec.year_,
                    kod1 = (int)MustCalcReasons.Tarif
                };

                db.AddMustCalcFromTable(conn_db, mustCalcFinder, preparedTableForMustCalc,
                    "Изменение значения тарифа с " + (newTarif.tarif == null ? "<Пусто>" : newTarif.tarif.ToString()) +
                    " на " + (newTarif.new_tarif == null ? "<Пусто>" : newTarif.new_tarif.ToString()));
            }
            return ret;
        }

        public List<long> GetListLsByTarif(PrmTarifFinder finder, TypeTarif type, Tarif tarif, out Returns ret)
        {
            var res = new List<long>();
            ret = Utils.InitReturns();
            if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.text = "С ошибками: Не определен код пользователя";
                return res;
            }
            if (finder.date_from > finder.date_to)
            {
                ret.result = false;
                ret.text = "С ошибками: Дата \"с\" не может быть больше даты \"по\"";
                return res;
            }

            var tableTarifs = String.Empty;
            var tableResults = String.Empty;
            using (var conn_db = GetConnection(Points.GetConnByPref(Points.Pref)))
            {
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    ret.text = "С ошибками: Ошибка открытия соединения с базой данных";
                    return res;
                }
                var tableName = string.Format("t{0}_selectedls" + finder.listNumber, finder.nzp_user);
                if (!ExistsTable(conn_db, "public", tableName))
                {
                    ret.result = false;
                    ret.text = "С ошибками: Таблицы с выбранными лицевыми счетами не существует";
                    return res;
                }
                var sql = " SELECT DISTINCT pref FROM " + sDefaultSchema + tableName;
                var ListPrefs = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                tableTarifs = CreateTempTableTarifs(conn_db, out ret);
                if (!ret.result) return res;
                tableResults = "temp_table_results_" + DateTime.Now.Ticks;
                sql = " CREATE TEMP TABLE " + tableResults + " (nzp_kvar integer)";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return res;

                ret = ExecSQL(conn_db,
                  " CREATE INDEX ix1_" + tableResults + " ON " + tableResults +
                  " (nzp_kvar)", true);
                if (!ret.result) return res;

                foreach (DataRow prefObj in ListPrefs.Rows)
                {
                    var pref = CastValue<string>(prefObj["pref"]).Trim();

                    //ограничиваем выборку 
                    var where = string.Format(" AND t.nzp_serv={0} AND t.nzp_supp={1} AND f.nzp_frm={2} AND f.nzp_frm_typ={3}",
                            tarif.nzp_serv, tarif.nzp_supp, tarif.nzp_frm, tarif.nzp_frm_type);
                    //выбрали данные для работы с текущим префиксом и с доп.условиями
                    ret = SwitcherDataPrepare(conn_db, type, finder, prefObj, tableTarifs, where,
                        tarif.nzp_prm);
                    if (!ret.result) return res;

                    ret = GetLsWithTarif(conn_db, tableTarifs, tableResults, type, finder, pref, tarif);
                    if (!ret.result) return res;
                }

                sql = "SELECT DISTINCT nzp_kvar FROM " + tableResults;
                var DT = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                for (int i = 0; i < DT.Rows.Count; i++)
                {
                    res.Add(CastValue<long>(DT.Rows[i]["nzp_kvar"]));
                }

                return res;
            }
        }

        private Returns GetLsWithTarif(IDbConnection conn_db, string tableTarifs, string tableResults,
            TypeTarif type, PrmTarifFinder finder, string pref, Tarif tarif)
        {
            Returns ret;
            string sql;
            var date_from = Utils.EStrNull(tarif.DateBegin.Value.ToShortDateString());
            var date_to = Utils.EStrNull(tarif.DateEnd.Value.ToShortDateString());
            var prmNum = 0;
            string fieldKey = String.Empty;
            #region switch
            switch (type)
            {
                case TypeTarif.Ls: prmNum = 1; break;
                case TypeTarif.House: prmNum = 2; break;
                case TypeTarif.Supplier: prmNum = 11; break;
                case TypeTarif.DataBase: prmNum = 5; break;
            }
            switch (prmNum)
            {
                case 1: fieldKey = "p.nzp = t.nzp_kvar";
                    break;
                case 2: fieldKey = "p.nzp = t.nzp_dom";
                    break;
                case 5: fieldKey = "1=1";
                    break;
                case 11: fieldKey = "p.nzp = t.nzp_supp";
                    break;
            }
            #endregion switchs

            if (tarif.tarif.HasValue)
            {
                //из-за того, что postgres не соблюдает порядок join условий и валится на CAST as NUMERIC приходится делать так:
                var preparedPrmTable = "temp_prepared_prm_table_" + DateTime.Now.Ticks;
                var prmTable = pref + sDataAliasRest + "prm_" + prmNum;
                //те, у кого вообще установлено хоть какое то значение параметра в выбранном периоде
                sql = " CREATE TEMP TABLE " + preparedPrmTable + " AS " +
                      " SELECT t.nzp_dom,t.nzp_kvar,t.nzp_supp, t.nzp_prm_tarif, t.nzp_serv, p.val_prm::numeric" +
                      " FROM " + prmTable + " p JOIN " + tableTarifs + " t ON t.nzp_prm_tarif=p.nzp_prm AND " + fieldKey +
                      " WHERE p.is_actual<>100 " +
                      " AND p.dat_s=" + date_from + " AND p.dat_po=" + date_to;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                ret = ExecSQL(conn_db,
                    " CREATE INDEX ix1_" + preparedPrmTable + " ON " + preparedPrmTable + " (val_prm)", true);
                if (!ret.result) return ret;
                ret = ExecSQL(conn_db,
                    " CREATE INDEX ix2_" + preparedPrmTable + " ON " + preparedPrmTable + " (nzp_kvar)", true);
                if (!ret.result) return ret;

                sql = " INSERT INTO " + tableResults + " (nzp_kvar)" +
                      " SELECT nzp_kvar FROM " + preparedPrmTable +
                      " WHERE val_prm=" + tarif.tarif;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

            }
            else
            {
                sql = " INSERT INTO " + tableResults + " (nzp_kvar)" +
                      " SELECT nzp_kvar FROM " + tableTarifs;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;
            }
            return ret;
        }

    }



}
