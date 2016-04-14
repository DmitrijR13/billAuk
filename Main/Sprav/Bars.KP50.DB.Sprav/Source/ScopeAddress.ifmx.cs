using System;
using System.Collections.Generic;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DB.Sprav.Source
{
   public class DbScopeAddress:DataBaseHead
   {
        public Returns AddNewToScopeAdress(ScopeAdress finder)
        {
            Returns ret = Utils.InitReturns();
            if (finder.nzp_user < 0)
            {
                return new Returns(false, "Не задан пользователь", -1);
            }
            if (finder.nzp_wp <= 0)
            {
                return new Returns(false, "Не задан банк", -1);
            }
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return ret;
            }
            string whereUpRules = "";
            string whereExists = "";
            string whereDownRules = "";
            string whereOutOfParentScope = "";
            string insert = "";
            switch (finder.ScopeLevel)
            {
                case ScopeAdress.ScopeUntil.Bank:
                    // проверка на существование добавляемой области действия
                    whereExists = " and nzp_wp=" + finder.nzp_wp + " and (nzp_town is null OR nzp_town<=0)";
                    // проверка существования вышестоящей области действия 
                    whereUpRules = "";
                    // проверка существования нижестоящей области действия 
                    whereDownRules = " and nzp_wp=" + finder.nzp_wp + " and nzp_town>0";
                    // проверка на выход за границы родительской области действия (только для случая finder.parent_nzp_scope>0)
                    if (finder.parent_nzp_scope > 0)
                    {
                        whereOutOfParentScope = " and nzp_wp=" + finder.nzp_wp + " and (nzp_town is null OR nzp_town<=0)";
                    }
                    // вставляемые поля
                    insert = "(nzp_scope, nzp_wp, changed_by, changed_on) VALUES (" + finder.cur_nzp_scope + "," + finder.nzp_wp + ","+finder.nzp_user+",'"+DateTime.Now+"')";
                    break;
                case ScopeAdress.ScopeUntil.Town:
                    whereExists = " and nzp_wp=" + finder.nzp_wp + " and nzp_town=" + finder.nzp_town+" and (nzp_raj is null OR nzp_raj<=0)";
                    whereUpRules = " and nzp_wp=" + finder.nzp_wp + " and (nzp_town is null OR nzp_town<=0)";
                    whereDownRules = " and nzp_wp=" + finder.nzp_wp + " and nzp_town=" + finder.nzp_town + " and nzp_raj>0";
                    if (finder.parent_nzp_scope > 0)
                    {
                        whereOutOfParentScope = " and nzp_wp=" + finder.nzp_wp + " and ((nzp_town is null OR nzp_town<=0) " +
                            "OR (nzp_town=" + finder.nzp_town + " and (nzp_raj is null OR nzp_raj<=0)))";
                    }
                    insert = "(nzp_scope, nzp_wp, nzp_town, changed_by, changed_on) VALUES (" + finder.cur_nzp_scope + "," + finder.nzp_wp + "," + finder.nzp_town + "," + finder.nzp_user + ",'" + DateTime.Now + "')";
                    break;
                case ScopeAdress.ScopeUntil.Rajon:
                    whereExists = " and nzp_wp=" + finder.nzp_wp + " and nzp_town=" + finder.nzp_town +
                                  " and nzp_raj=" + finder.nzp_raj+" and (nzp_ul is null OR nzp_ul<=0)";
                    whereUpRules = " and nzp_wp=" + finder.nzp_wp + " and ((nzp_town is null OR nzp_town<=0) " +
                            "OR (nzp_town=" + finder.nzp_town + " and (nzp_raj is null OR nzp_raj<=0)))";
                    whereDownRules = " and nzp_wp=" + finder.nzp_wp + " and nzp_town=" + finder.nzp_town +
                                  " and nzp_raj=" + finder.nzp_raj + " and nzp_ul>0";
                    if (finder.parent_nzp_scope > 0)
                    {
                        whereOutOfParentScope = " and nzp_wp=" + finder.nzp_wp + " and ((nzp_town is null OR nzp_town<=0) OR (nzp_town=" + finder.nzp_town +
                            " and ((nzp_raj is null OR nzp_raj<=0) OR (nzp_raj=" + finder.nzp_raj + " and (nzp_ul is null OR nzp_ul<=0)))))";
                    }
                    insert = "(nzp_scope, nzp_wp, nzp_town, nzp_raj, changed_by, changed_on) " +
                             "VALUES (" + finder.cur_nzp_scope + "," + finder.nzp_wp + "," + finder.nzp_town + "," + finder.nzp_raj + "," + finder.nzp_user + ",'" + DateTime.Now + "')";
                    break;
                case ScopeAdress.ScopeUntil.Ulica:
                    whereExists = " and nzp_wp=" + finder.nzp_wp + " and nzp_town=" + finder.nzp_town +
                                  " and nzp_raj=" + finder.nzp_raj + " and nzp_ul=" + finder.nzp_ul+ " and (nzp_dom is null OR nzp_dom<=0)";
                    whereUpRules = " and nzp_wp=" + finder.nzp_wp + " and ((nzp_town is null OR nzp_town<=0) OR (nzp_town=" + finder.nzp_town +
                            " and ((nzp_raj is null OR nzp_raj<=0) OR (nzp_raj=" + finder.nzp_raj + " and (nzp_ul is null OR nzp_ul<=0)))))";
                    whereDownRules = " and nzp_wp=" + finder.nzp_wp + " and nzp_town=" + finder.nzp_town +
                                  " and nzp_raj=" + finder.nzp_raj + " and nzp_ul=" + finder.nzp_ul + " and nzp_dom>0";
                    if (finder.parent_nzp_scope > 0)
                    {
                        whereOutOfParentScope = " and nzp_wp=" + finder.nzp_wp + " and ((nzp_town is null OR nzp_town<=0) OR (nzp_town=" + finder.nzp_town +
                            " and ((nzp_raj is null OR nzp_raj<=0) OR (nzp_raj=" + finder.nzp_raj +
                            " and ((nzp_ul is null OR nzp_ul<=0) OR (nzp_ul=" + finder.nzp_ul + " and (nzp_dom is null OR nzp_dom<=0)))))))";
                    }
                    insert = "(nzp_scope, nzp_wp, nzp_town, nzp_raj, nzp_ul, changed_by, changed_on)" +
                      " VALUES (" + finder.cur_nzp_scope + "," + finder.nzp_wp + "," +
                      finder.nzp_town + "," + finder.nzp_raj + "," + finder.nzp_ul + "," + finder.nzp_user + ",'" + DateTime.Now + "')";
                    break;
                case ScopeAdress.ScopeUntil.Dom:
                    whereExists = " and nzp_wp=" + finder.nzp_wp + " and nzp_town=" + finder.nzp_town +
                                  " and nzp_raj=" + finder.nzp_raj + " and nzp_ul=" + finder.nzp_ul + " and nzp_dom=" + finder.nzp_dom;
                    whereUpRules = " and nzp_wp=" + finder.nzp_wp + " and ((nzp_town is null OR nzp_town<=0) OR (nzp_town=" + finder.nzp_town +
                            " and ((nzp_raj is null OR nzp_raj<=0) OR (nzp_raj=" + finder.nzp_raj +
                            " and ((nzp_ul is null OR nzp_ul<=0) OR (nzp_ul=" + finder.nzp_ul + " and (nzp_dom is null OR nzp_dom<=0)))))))";
                    whereDownRules = "";
                    if (finder.parent_nzp_scope > 0)
                    {
                        whereOutOfParentScope = " and nzp_wp=" + finder.nzp_wp + " and ((nzp_town is null OR nzp_town<=0) OR (nzp_town=" + finder.nzp_town +
                            " and ((nzp_raj is null OR nzp_raj<=0) OR (nzp_raj=" + finder.nzp_raj +
                            " and ((nzp_ul is null OR nzp_ul<=0) OR (nzp_ul=" + finder.nzp_ul + " and (nzp_dom is null OR nzp_dom<=0 OR nzp_dom="+finder.nzp_dom+")))))))";
                    }
                    insert = "(nzp_scope, nzp_wp, nzp_town, nzp_raj, nzp_ul, nzp_dom, changed_by, changed_on) " +
                             "VALUES (" + finder.cur_nzp_scope + "," + finder.nzp_wp + "," + finder.nzp_town + "," + finder.nzp_raj + ","
                               + finder.nzp_ul + "," + finder.nzp_dom + "," + finder.nzp_user + ",'" + DateTime.Now + "')";
                    break;
                case ScopeAdress.ScopeUntil.None:
                    conn_db.Close();
                    return new Returns(false, "Неверные входные параметры", -1);
            }
            ret = addNewRules(conn_db, whereExists, whereUpRules, whereDownRules, whereOutOfParentScope, finder, insert);
            conn_db.Close();
            return ret;
        }

        private Returns addNewRules(IDbConnection connDB, string whereExists, string whereUpRules, string whereDownRules, string whereOutOfParentScope,  ScopeAdress finder, string insert)
        {
            Returns ret = Utils.InitReturns();
            string sql = "SELECT exists (select 1 from " + Points.Pref + sDataAliasRest + "fn_scope_adres where "+
                         " nzp_scope=" + finder.cur_nzp_scope + whereExists + ")";
            // Проверка добавляемого правила на существование
            object isRulesExists = ExecScalar(connDB, sql, out ret, true);
            if (!ret.result)
            {
                return ret;
            }
            if ((bool) isRulesExists)
            {
                return new Returns(true, "Указанная область дейсвия уже существует", -1);
            }
            // Только для областей, у которых имеется родительская область.
            // Проверка на выход за границы родительской области действия
            if (whereOutOfParentScope != "")
            {
                sql = "SELECT exists (select 1 from " + Points.Pref + sDataAliasRest + "fn_scope_adres where " +
                      " nzp_scope=" + finder.parent_nzp_scope + whereOutOfParentScope + ")";
                object isUpRulesExists = ExecScalar(connDB, sql, out ret, true);
                if (!ret.result)
                {
                    return ret;
                }
                if (!(bool) isUpRulesExists)
                {
                    return new Returns(true, "Указанная область действия не будет добавлена, т.к. выходит за границы области действия договора ЕРЦ", -1);
                }
            }
            // Проверка наличия вышестоящего правила
            if (whereUpRules!="")
            {
                sql = "SELECT exists (select 1 from " + Points.Pref + sDataAliasRest + "fn_scope_adres where nzp_wp=" + finder.nzp_wp +
                      " and nzp_scope=" + finder.cur_nzp_scope + whereUpRules + ")";
                object isUpRulesExists = ExecScalar(connDB, sql, out ret, true);
                if (!ret.result)
                {
                    return ret;
                }
                if ((bool)isUpRulesExists)
                {
                    return new Returns(true, "Сначала удалите вышестоящие области действия", -1);
                }
            }
            // Проверка наличия нижестоящего правила
            if (whereDownRules != "")
            {
                sql = "SELECT exists (select 1 from " + Points.Pref + sDataAliasRest + "fn_scope_adres where nzp_wp=" + finder.nzp_wp +
                      " and nzp_scope=" + finder.cur_nzp_scope + whereDownRules + ")";
                object isDownRulesExists = ExecScalar(connDB, sql, out ret, true);
                if (!ret.result)
                {
                    return ret;
                }
                if ((bool)isDownRulesExists)
                {
                    return new Returns(true, "Сначала удалите нижестоящие области действия", -1);
                }
            }
            // Если все успешно, вставляем
            sql = "insert into " + Points.Pref + sDataAliasRest + "fn_scope_adres "+insert ;
            ret = ExecSQL(connDB, sql, true);
            return ret;
        }

        public List<ScopeAdress> GetAdressesByScope(ScopeAdress finder, out Returns ret)
        {
            List<ScopeAdress> list = new List<ScopeAdress>();
            ret= Utils.InitReturns();
            if (finder.nzp_user < 0)
            {
                ret.result = false;
                ret.text = "Не задан пользователь";
                ret.tag = 1;
                return list;
            }

            if (finder.cur_nzp_scope <= 0)
            {
                ret.result = false;
                ret.text = "Область действия не определена";
                ret.tag = 1;
                return list;
            }
          
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return list;
            }

            MyDataReader reader = null;
            string sql = "Select sa.*, sp.point, t.town, r.rajon, u.ulica, d.ndom from " +
                         Points.Pref + sDataAliasRest + "fn_scope_adres  sa " +
                         "left outer join  " + Points.Pref + sKernelAliasRest + ".s_point sp on (sa.nzp_wp=sp.nzp_wp) " +
                         "left outer join  " + Points.Pref + sDataAliasRest + "s_town t on (sa.nzp_town=t.nzp_town) " +
                         "left outer join  " + Points.Pref + sDataAliasRest + "s_rajon r on (sa.nzp_raj=r.nzp_raj) " +
                         "left outer join " + Points.Pref + sDataAliasRest + "s_ulica u on (sa.nzp_ul=u.nzp_ul) " +
                         "left outer join " + Points.Pref + sDataAliasRest + "dom d on (sa.nzp_dom=d.nzp_dom) " +
                         "where nzp_scope=" + finder.cur_nzp_scope + " order by nzp_scope_adres";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                MonitorLog.WriteLog("Ошибка в методе GetAdressesByScope() ", MonitorLog.typelog.Error, 20, 201, true);
                return list;
            }
            try
            {
                int i=0;
                while (reader.Read())
                {
                    i++;
                    if (finder.skip > 0 && i <= finder.skip) continue;
                    if (i > finder.skip + finder.rows) continue;
                    var fnd = new ScopeAdress();
                    if (reader["nzp_scope_adres"] != DBNull.Value) fnd.nzp_scope_adres = (int)reader["nzp_scope_adres"];
                    if (reader["nzp_scope"] != DBNull.Value) fnd.cur_nzp_scope = (int)reader["nzp_scope"];
                    if (reader["nzp_wp"] != DBNull.Value) fnd.nzp_wp = (int)reader["nzp_wp"];
                    if (reader["point"] != DBNull.Value) fnd.point = reader["point"].ToString();
                    if (reader["nzp_town"] != DBNull.Value) fnd.nzp_town = (int)reader["nzp_town"];
                    if (reader["town"] != DBNull.Value) fnd.town = reader["town"].ToString();
                    if (reader["nzp_raj"] != DBNull.Value) fnd.nzp_raj = (int)reader["nzp_raj"];
                    if (reader["rajon"] != DBNull.Value) fnd.rajon = reader["rajon"].ToString();
                    if (reader["nzp_ul"] != DBNull.Value) fnd.nzp_ul = (int)reader["nzp_ul"];
                    if (reader["ulica"] != DBNull.Value) fnd.ulica = reader["ulica"].ToString();
                    if (reader["nzp_dom"] != DBNull.Value) fnd.nzp_dom = (int)reader["nzp_dom"];
                    if (reader["ndom"] != DBNull.Value) fnd.ndom = reader["ndom"].ToString();
                    fnd.order_num = i;
                    list.Add(fnd);
                }
                ret.tag = i;
                reader.Close();
                return list;
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                ret.text = ex.Message;
                ret.result = false;
                return list;
            }
            finally
            {
                conn_db.Close();
            }
        }

        public Returns DeleteAdressFromScope(ScopeAdress finder)
        {
           Returns ret = Utils.InitReturns();
            if (finder.nzp_user < 0)
            {
                ret.result = false;
                ret.text = "Не задан пользователь";
                ret.tag = 1;
                return ret;
            }

            if (finder.cur_nzp_scope <= 0)
            {
                ret.result = false;
                ret.text = "Область действия не определена";
                ret.tag =-1;
                return ret;
            }

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return ret;
            }
            string nzp_scope_adres_items = string.Join(", ", finder.nzp_scope_adres_list);
            if (finder.parent_nzp_scope <= 0)
            {               
                if (finder.childs_nzp_scope.Count > 0)
                {
                    string childsNzpScope = string.Join(",", finder.childs_nzp_scope);
                    string sqlParentScope = "select * from " + Points.Pref + sDataAliasRest + "fn_scope_adres where nzp_scope_adres in (" + nzp_scope_adres_items + ")";

                    List<ScopeAdress> parentScopeAdress = getScopeAdresses(conn_db, out ret, sqlParentScope);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }
                    foreach (ScopeAdress scopeAdress in parentScopeAdress)
                    {
                        string deleteChildAdressScope = "delete from " + Points.Pref + sDataAliasRest + "fn_scope_adres where nzp_scope in (" + childsNzpScope + ") ";
                        switch (scopeAdress.ScopeLevel)
                        {
                            case ScopeAdress.ScopeUntil.Bank:
                                deleteChildAdressScope += " and nzp_wp=" + scopeAdress.nzp_wp;
                                break;
                            case ScopeAdress.ScopeUntil.Town:
                                deleteChildAdressScope += " and nzp_wp=" + scopeAdress.nzp_wp + " and nzp_town=" + scopeAdress.nzp_town;
                                break;
                            case ScopeAdress.ScopeUntil.Rajon:
                                deleteChildAdressScope += " and nzp_wp=" + scopeAdress.nzp_wp + " and nzp_town=" + scopeAdress.nzp_town +
                                                          " and nzp_raj=" + scopeAdress.nzp_raj;
                                break;
                            case ScopeAdress.ScopeUntil.Ulica:
                                deleteChildAdressScope += " and nzp_wp=" + scopeAdress.nzp_wp + " and nzp_town=" + scopeAdress.nzp_town +
                                                          " and nzp_raj=" + scopeAdress.nzp_raj + " and nzp_ul=" + scopeAdress.nzp_ul;
                                break;
                            case ScopeAdress.ScopeUntil.Dom:
                                deleteChildAdressScope += " and nzp_wp=" + scopeAdress.nzp_wp + " and nzp_town=" + scopeAdress.nzp_town +
                                                          " and nzp_raj=" + scopeAdress.nzp_raj + " and nzp_ul=" + scopeAdress.nzp_ul + " and nzp_dom=" + scopeAdress.nzp_dom;
                                break;
                            default:
                                ret.text = "Ошибка входных параметров";
                                ret.tag = -1;
                                ret.result = false;
                                return ret;
                        }
                        ret = ExecSQL(conn_db, deleteChildAdressScope, true);
                        if (ret.result) continue;
                        conn_db.Close();
                        return ret;
                    }
                }
            }
            string deleteAdressScope = " delete from " + Points.Pref + sDataAliasRest + "fn_scope_adres where nzp_scope_adres in (" + nzp_scope_adres_items + ")";
            ret = ExecSQL(conn_db, deleteAdressScope, true);
            conn_db.Close();
            return ret;
        }
        // проверка на наличие в дочерних областей действия удаляемой родительской области.
        public Returns CheckUsingScopeByChilds(ScopeAdress finder)
        {
            Returns ret = Utils.InitReturns();
            if (finder.nzp_user < 0)
            {
                ret.result = false;
                ret.text = "Не задан пользователь";
                ret.tag = 1;
                return ret;
            }

            if (finder.cur_nzp_scope <= 0)
            {
                ret.result = false;
                ret.text = "Область действия не определена";
                ret.tag = 1;
                return ret;
            }
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return ret;
            }
            string msg = "";
            // отмеченные для удаления адреса родительской области действия (с интерфейса передаются значения столбца PK таблицы fn_scope_adres)
            string nzp_scope_adres_items = string.Join(", ", finder.nzp_scope_adres_list);
            string sqlParentScope = "select * from " + Points.Pref + sDataAliasRest + "fn_scope_adres where nzp_scope_adres in (" + nzp_scope_adres_items + ")";
            List<ScopeAdress> parentList = getScopeAdresses(conn_db, out ret, sqlParentScope);
            if (!ret.result)
            {
                return ret;
            }
            // Для каждой удаляемой родительской области действия
            foreach (ScopeAdress parentScopeAdress in parentList)
            {
                // извлекаем дочерние области действия
                bool nextParent = false;
                foreach (int childScope in finder.childs_nzp_scope)
                {
                    string sqlChildScope = "select * from " + Points.Pref + sDataAliasRest + "fn_scope_adres where nzp_scope=" + childScope;
                    List<ScopeAdress> childList = getScopeAdresses(conn_db, out ret, sqlChildScope);
                    if (!ret.result)
                    {
                        return ret;
                    }
                    foreach (ScopeAdress chldScopeAdress in childList)
                    {
                        // 
                        // Если уровень родительской области действия больше, чем уровень дочерней области действия
                        // Т.е., например, удаляемая родительская область действия до города (равна 1), а дочерняя до банка (равна 2) 
                        // то такую область действия разрешаем удалять
                        if ((int) chldScopeAdress.ScopeLevel < (int) parentScopeAdress.ScopeLevel) continue;
                        switch (parentScopeAdress.ScopeLevel)
                        {
                            case ScopeAdress.ScopeUntil.Bank:
                                if (chldScopeAdress.nzp_wp == parentScopeAdress.nzp_wp)
                                {
                                    msg += parentScopeAdress.nzp_scope_adres + "|";
                                    nextParent = true;
                                }
                                break;
                            case ScopeAdress.ScopeUntil.Town:
                                if ((chldScopeAdress.nzp_wp == parentScopeAdress.nzp_wp)
                                    && (chldScopeAdress.nzp_town == parentScopeAdress.nzp_town))
                                {
                                    msg += parentScopeAdress.nzp_scope_adres + "|";
                                    nextParent = true;
                                }
                                break;
                            case ScopeAdress.ScopeUntil.Rajon:
                                if ((chldScopeAdress.nzp_wp == parentScopeAdress.nzp_wp)
                                    && (chldScopeAdress.nzp_town == parentScopeAdress.nzp_town)
                                    && (chldScopeAdress.nzp_raj == parentScopeAdress.nzp_raj))
                                {
                                    msg += parentScopeAdress.nzp_scope_adres + "|";
                                    nextParent = true;
                                }
                                break;
                            case ScopeAdress.ScopeUntil.Ulica:
                                if ((chldScopeAdress.nzp_wp == parentScopeAdress.nzp_wp)
                                    && (chldScopeAdress.nzp_town == parentScopeAdress.nzp_town)
                                    && (chldScopeAdress.nzp_raj == parentScopeAdress.nzp_raj)
                                    && (chldScopeAdress.nzp_ul == parentScopeAdress.nzp_ul))
                                {
                                    msg += parentScopeAdress.nzp_scope_adres + "|";
                                    nextParent = true;
                                }
                                break;
                            case ScopeAdress.ScopeUntil.Dom:
                                if ((chldScopeAdress.nzp_wp == parentScopeAdress.nzp_wp)
                                    && (chldScopeAdress.nzp_town == parentScopeAdress.nzp_town)
                                    && (chldScopeAdress.nzp_raj == parentScopeAdress.nzp_raj)
                                    && (chldScopeAdress.nzp_ul == parentScopeAdress.nzp_ul)
                                    && (chldScopeAdress.nzp_dom == parentScopeAdress.nzp_dom))
                                {
                                    msg += parentScopeAdress.nzp_scope_adres + "|";
                                    nextParent = true;
                                }
                                break;
                            default:
                                ret.text = "Ошибка входных параметров";
                                ret.tag = -1;
                                ret.result = false;
                                return ret;
                        }
                        if (nextParent) break;
                    }
                    if (nextParent) break;
                }
            }
            ret.text = msg;
            return ret;
        }

        public List<ScopeAdress> getScopeAdresses(IDbConnection conn_db, out Returns ret, string sql)
        {
            MyDataReader reader = null;
            List<ScopeAdress> list = new List<ScopeAdress>();
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка в методе CheckUsingScopeByChilds() ", MonitorLog.typelog.Error, 20, 201, true);
                return list;
            }
            try
            {
                while (reader.Read())
                {
                    var fnd = new ScopeAdress();
                    if (reader["nzp_scope_adres"] != DBNull.Value) fnd.nzp_scope_adres = (int)reader["nzp_scope_adres"];
                    if (reader["nzp_scope"] != DBNull.Value) fnd.cur_nzp_scope = (int)reader["nzp_scope"];
                    if (reader["nzp_wp"] != DBNull.Value) fnd.nzp_wp = (int)reader["nzp_wp"];
                    if (reader["nzp_town"] != DBNull.Value) fnd.nzp_town = (int)reader["nzp_town"];
                    if (reader["nzp_raj"] != DBNull.Value) fnd.nzp_raj = (int)reader["nzp_raj"];
                    if (reader["nzp_ul"] != DBNull.Value) fnd.nzp_ul = (int)reader["nzp_ul"];
                    if (reader["nzp_dom"] != DBNull.Value) fnd.nzp_dom = (int)reader["nzp_dom"];
                    list.Add(fnd);
                }
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                ret.text = ex.Message;
                ret.result = false;
                return list;
            }
            finally
            {
                conn_db.Close();
            }
            return list;
        }
    
        public Returns GenerateNzpScope(Finder finder)
        {
            Returns ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не задан пользователь";
                return ret;
            }
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            MyDataReader reader;
            int nzp_scope=-1;
            string sql = "insert into " + Points.Pref + DBManager.sDataAliasRest + "fn_scope ( changed_by, changed_on) values ("+finder.nzp_user+",'"+ DateTime.Now+"') returning nzp_scope";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка в методе GetNzpScope() ", MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            try
            {
                while (reader.Read())
                {
                    if (reader["nzp_scope"] != DBNull.Value) nzp_scope = (int) reader["nzp_scope"];
                    break;
                }
            }
            catch (Exception ex)
            {
                ret.text = ex.Message;
                ret.result = false;
                return ret;
            }
            finally
            {
                if (reader != null) reader.Close();
                conn_db.Close();
            }
            ret.tag = nzp_scope;
            return ret;
        }

        public Returns GenerateNzpScope(IDbConnection conn_db, IDbTransaction transaction)
        {
            Returns ret = Utils.InitReturns();
         
            MyDataReader reader;
            int nzp_scope = -1;
            string sql = "insert into " + Points.Pref + DBManager.sDataAliasRest + "fn_scope (nzp_scope) values (default) returning nzp_scope";
            ret = ExecRead(conn_db, transaction, out reader, sql, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка в методе GetNzpScope() ", MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            try
            {
                while (reader.Read())
                {
                    if (reader["nzp_scope"] != DBNull.Value) nzp_scope = (int)reader["nzp_scope"];
                    break;
                }
            }
            catch (Exception ex)
            {
                ret.text = ex.Message;
                ret.result = false;
                return ret;
            }
            ret.tag = nzp_scope;
            return ret;
        }

        // Этот метод только для областей, которые имеют родительские области
        // Копирует всю область действия ролительской области с новым nzp_scope
        public Returns CopyParentScopes(ScopeAdress finder)
        {
            int nzp_scope;
            Returns ret = Utils.InitReturns();
            if (finder.parent_nzp_scope <= 0)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Ошибка входных параметров. Не задана родительская область действия";
                return ret;
            }
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            ret= GenerateNzpScope(finder);
            if (!ret.result)
            {
                conn_db.Close();
                ret.text = "Ошибка генерации новой области действия";
                ret.tag = -1;
                return ret;  
            }
            if (ret.tag<=0)
            {
                conn_db.Close();
                ret.result = false;
                ret.text = "Ошибка генерации новой области действия";
                ret.tag = -1;
                return ret; 
            }
            nzp_scope = ret.tag;
            string insert = "insert into " + Points.Pref + sDataAliasRest + "fn_scope_adres (nzp_scope, nzp_wp, nzp_town, nzp_raj, nzp_ul, nzp_dom, changed_by, changed_on)" +
                            " select " + nzp_scope + ", " + " nzp_wp, nzp_town, nzp_raj, nzp_ul, nzp_dom, " +finder.nzp_user+" , '"+DateTime.Now+"'"+
                            "from " + Points.Pref + sDataAliasRest + "fn_scope_adres where nzp_scope=" + finder.parent_nzp_scope;
            ret = ExecSQL(conn_db, insert, true);
            conn_db.Close();
            if (ret.result)
            {
                ret.tag = nzp_scope;
            }
            return ret;  
        }
    }
}
