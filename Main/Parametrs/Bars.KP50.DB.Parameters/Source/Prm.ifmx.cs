using System;
using System.CodeDom.Compiler;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Collections;
using System.Data.Common;


namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class DbParameters : DataBaseHead
    //----------------------------------------------------------------------
    {
#if PG
        private readonly string pgDefaultDb = "public";
#else
#endif  
        /// <summary>
        /// Облегченный список справочника параметров, иначе было тяжелым и не передавалось на web
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<LightPrm> LoadShortParams(Prm finder, out Returns ret)
        {
            var listResult = new List<LightPrm>();
            var listPrm = LoadParams(finder, out ret);
            foreach (var prm in listPrm)
            {
                listResult.Add(new LightPrm().CopyFromPrm(prm));
            }
            return listResult;
        }




        /// <summary> Возвращает справочник параметров
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<Prm> LoadParams(Prm finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            #region проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }
            if (finder.pref == "") finder.pref = Points.Pref;

            #endregion

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            #region фильтр по услугам и поставщикам
            string filter = "";
            string filterPrm = "";
            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql)
                    {
                        if (role.kod == Constants.role_sql_serv) filter += " and b.nzp_serv in (" + role.val + ")";
                        else if (role.kod == Constants.role_sql_supp) filter = " and b.nzp_supp in (" + role.val + ")";
                        else if (role.kod == Constants.role_sql_prm) filterPrm += " and n.nzp_prm in (" + role.val + ")";
                    }
                }

            string where = "Where 1=1 ";

            // СНЯТЬ это хитроумное условие:
            // аргументы: 1. нужно показывать все
            //            2. не надо связывать между собой параметры и ограничения по услугам 
            /*if (finder.spis_prm == "1")
            {
                where = " Where 1=1 ";
            }
            else
            {
#if PG
                where = " Where 0 < (select count(*) from " + finder.pref + "_kernel.prm_frm a," + finder.pref + "_kernel.l_foss b " +
                                    " where n.nzp_prm=a.nzp_prm and a.is_prm = 1 and (a.nzp_frm=b.nzp_frm or a.nzp_frm=-1) " + filter + ") ";
#else
                where = " Where 0 < (select count(*) from " + finder.pref + "_kernel:prm_frm a," + finder.pref + "_kernel:l_foss b " +
                                    " where n.nzp_prm=a.nzp_prm and a.is_prm = 1 and (a.nzp_frm=b.nzp_frm or a.nzp_frm=-1) " + filter + ") ";
#endif
            }*/

            #endregion

            where += filterPrm;
            if (finder.dopFind != null && finder.dopFind.Count > 0)
            {
                if (finder.dopFind[0] != "")
                    where += " and n.prm_num in " + finder.dopFind[0];

                if (finder.dopFind.Count > 1 && finder.dopFind[1] != "")
                    where += " and n.prm_type_id in " + finder.dopFind[1];
            }

            if (finder.prm_num == 1 && Utils.GetParams(finder.dopprm, Constants.act_showallprm))
            {
                where += " and n.prm_num in (1,18,3)";
            }
            else if (finder.prm_num > 0)
            {
                where += " and n.prm_num = " + finder.prm_num;
            }
            if (finder.nzp_prm > 0)
            {
                where += " and n.nzp_prm = " + finder.nzp_prm;
            }

            string order = " n.name_prm ";
            if (finder.numer > 0)
            {
                where += " and n.numer is not null and n.numer > 0 ";
                order = " n.numer";
            }

            if (finder.name_prm != "") where += " and n.name_prm like '%" + finder.name_prm + "%'";

            string sql;
            #region определить число записей
            int total_record_count = 0;

            sql = " SELECT COUNT(*) FROM " + finder.pref.Trim() + "_kernel" + DBManager.tableDelimiter + "prm_name n " + where;

            object count = ExecScalar(conn_db, sql, out ret, true);
            if (ret.result)
            {
                try
                {
                    total_record_count = Convert.ToInt32(count);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    conn_db.Close();
                    return null;
                }
            }
            else
            {
                conn_db.Close();
                return null;
            }
            #endregion

            //выбрать список
            if (Points.isNewNorms)
            {
                sql =
                    " SELECT n.nzp_prm,n.name_prm," +
                    "(CASE WHEN n.type_prm = 'sprav' AND coalesce(nps.nzp_prm, 0) > 0 THEN 'norm' else n.type_prm END) as type_prm," +
                    "n.nzp_res,n.numer,n.prm_num,n.low_,n.high_,n.digits_,n.old_field,n.is_day_uchet, n.prm_type_id, " +
                    "(case when " + sNvlWord +
                    "(n.prm_type_id, 0) = 0 then 'Не назначен' else t.type_name end) as type_name " +
                    "FROM " + finder.pref.Trim() + "_kernel" + DBManager.tableDelimiter + "prm_name n " +
                    " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest + "norm_prm_serv nps ON nps.nzp_prm = n.nzp_prm " +
#if PG
 "left outer join " + Points.Pref + "_kernel" + DBManager.tableDelimiter +
                    "prm_types t on n.prm_type_id = t.id "
#else          
               " , outer "+ Points.Pref + "_kernel" + DBManager.tableDelimiter + "prm_types t " 
#endif
 + where +
#if PG
#else          
               " and n.prm_type_id = t.id " +
#endif
 " ORDER BY " + order;
            }
            else
            {
                sql =
                    " SELECT n.nzp_prm,n.name_prm,n.type_prm,n.nzp_res,n.numer," +
                    " n.prm_num,n.low_,n.high_,n.digits_,n.old_field,n.is_day_uchet, n.prm_type_id, " +
                    "(case when " + sNvlWord +
                    "(n.prm_type_id, 0) = 0 then 'Не назначен' else t.type_name end) as type_name " +
                    "FROM " + finder.pref.Trim() + "_kernel" + DBManager.tableDelimiter + "prm_name n " +
#if PG
                        "left outer join " + Points.Pref + "_kernel" + DBManager.tableDelimiter +
                    "prm_types t on n.prm_type_id = t.id "
#else          
               " , outer "+ Points.Pref + "_kernel" + DBManager.tableDelimiter + "prm_types t " 
#endif
                    + where +
#if PG
#else          
               " and n.prm_type_id = t.id " +
#endif
                        " ORDER BY " + order;
            }
            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                List<Prm> spParam = new List<Prm>();
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;

                    Prm zap = new Prm();
                    zap.pref = finder.pref.Trim();

                    zap.num = i.ToString();
                    if (reader["nzp_prm"] != DBNull.Value)
                    {
                        zap.nzp_prm = (int)reader["nzp_prm"];
                        zap.nzp_key = zap.nzp_prm;
                    }
                    if (reader["name_prm"] != DBNull.Value) zap.name_prm = ((string)reader["name_prm"]).Trim();

                    if (reader["type_name"] != DBNull.Value) zap.type_name = ((string)reader["type_name"]).Trim();
                    if (reader["prm_num"] != DBNull.Value) zap.prm_num = Convert.ToInt32(reader["prm_num"]);
                    if (reader["prm_type_id"] != DBNull.Value) zap.prm_type_id = Convert.ToInt32(reader["prm_type_id"]);
                    if (reader["is_day_uchet"] != DBNull.Value) zap.is_day_uchet = Convert.ToInt32(reader["is_day_uchet"]);
                    if (reader["numer"] != DBNull.Value) zap.numer = Convert.ToInt32(reader["numer"]);
                    if (reader["type_prm"] != DBNull.Value) zap.type_prm = Convert.ToString(reader["type_prm"]);
                    if (reader["nzp_res"] != DBNull.Value) zap.nzp_res = Convert.ToInt32(reader["nzp_res"]);
                    if (reader["old_field"] != DBNull.Value) zap.old_field = Convert.ToString(reader["old_field"]);
                    if (reader["low_"] != DBNull.Value) zap.low_ = Convert.ToDecimal(reader["low_"]);
                    if (reader["high_"] != DBNull.Value) zap.high_ = Convert.ToDecimal(reader["high_"]);
                    if (reader["digits_"] != DBNull.Value) zap.digits_ = Convert.ToInt32(reader["digits_"]);
                    if (zap.nzp_prm > 100000) zap.is_system = 0;
                    else zap.is_system = 1;

                    switch (zap.prm_num)
                    {
                        case 1:
                        case 3:
                        case 18:
                        case 19: zap.type_prm_name = "Квартирный"; break;
                        case 2:
                        case 4: zap.type_prm_name = "Домовой"; break;
                        case 7: zap.type_prm_name = "Управляющая организация"; break;
                        case 8: zap.type_prm_name = "Отделение"; break;
                        case 11: zap.type_prm_name = "Поставщики"; break;
                        case 12: zap.type_prm_name = "Услуги"; break;
                        case 6: zap.type_prm_name = "Улицы"; break;
                        case 17: zap.type_prm_name = "Приборы учета"; break;
                        case 5:
                        case 10: zap.type_prm_name = "Общесистемные"; break;
                        default: zap.type_prm_name = "Не определен"; break;
                    }

                    switch (zap.type_prm.ToLower().Trim())
                    {
                        case "bool": zap.type_val_prm_name = "Логический"; break;
                        case "int": zap.type_val_prm_name = "Целочисленный"; break;
                        case "float": zap.type_val_prm_name = "Вещественный"; break;
                        case "char": zap.type_val_prm_name = "Текстовый"; break;
                        case "date": zap.type_val_prm_name = "Дата"; break;
                        case "sprav": zap.type_val_prm_name = "Справочник"; break;
                        case "table": zap.type_val_prm_name = "Таблица"; break;
                        default: zap.type_val_prm_name = "Не определен"; break;
                    }

                    if (finder.is_show > 0) // определить показывается ли этот параметр на формах (например, в реквизитах ЛС ...)
                    {
                        if (ParamNums.domParams.Contains(zap.prm_num)) zap.nzp_reg = 2;
                        else if (ParamNums.lsParams.Contains(zap.prm_num)) zap.nzp_reg = 1;
                        if (zap.nzp_reg > 0)
                        {
                            sql = "select count(*) from " + Points.Pref.Trim() + "_kernel" + DBManager.tableDelimiter +
                                  "s_reg_prm where nzp_prm = " + zap.nzp_prm + " and nzp_reg = " + zap.nzp_reg;
                            object obj = ExecScalar(conn_db, sql, out ret, true);
                            if (!ret.result) throw new Exception(ret.text);
                            int totalRecordCount = 0;
                            try { totalRecordCount = Convert.ToInt32(obj); }
                            catch { throw new Exception("Ошибка определения числа записей"); }
                            if (totalRecordCount > 0) zap.is_show = 1;
                            else zap.is_show = 0;
                        }
                    }
                    spParam.Add(zap);

                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }

                reader.Close();
                conn_db.Close();

                ret.tag = total_record_count;
                return spParam;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения справочника параметров " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        public List<PrmTypes> LoadPrmTypes(PrmTypes finder, out Returns ret)
        {
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string sql = "select * from " + Points.Pref + "_kernel" + tableDelimiter + "prm_types order by type_name";

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                List<PrmTypes> spTypeParam = new List<PrmTypes>();
                while (reader.Read())
                {
                    PrmTypes zap = new PrmTypes();
                    if (reader["id"] != DBNull.Value) zap.id = (int)reader["id"];
                    if (reader["type_name"] != DBNull.Value) zap.type_name = ((string)reader["type_name"]).Trim();

                    spTypeParam.Add(zap);
                }

                reader.Close();
                conn_db.Close();

                return spTypeParam;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения справочника типов параметра " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
        }

        private void fillCommonFields(IDataReader reader, Prm prm)
        {
            if (reader["not_hist"] == DBNull.Value) prm.not_hist = 0;
            else prm.not_hist = Convert.ToInt32(reader["not_hist"]);

            if (reader["nzp_prm"] == DBNull.Value) prm.nzp_prm = 0;
            else prm.nzp_prm = Convert.ToInt32(reader["nzp_prm"]);

            if (reader["pref"] == DBNull.Value) prm.pref = "";
            else prm.pref = Convert.ToString(reader["pref"]);

            if (reader["name_prm"] == DBNull.Value) prm.name_prm = "";
            else prm.name_prm = Convert.ToString(reader["name_prm"]);

            if (reader["nzp"] == DBNull.Value) prm.nzp = 0;
            else prm.nzp = Convert.ToInt32(reader["nzp"]);

            if (reader["type_prm"] == DBNull.Value) prm.type_prm = "";
            else prm.type_prm = Convert.ToString(reader["type_prm"]);

            if (reader["nzp_res"] == DBNull.Value) prm.nzp_res = 0;
            else prm.nzp_res = Convert.ToInt32(reader["nzp_res"]);

            if (reader["prm_num"] == DBNull.Value) prm.prm_num = 0;
            else prm.prm_num = Convert.ToInt32(reader["prm_num"]);

            if (reader["name_tab"] == DBNull.Value) prm.name_tab = "";
            else prm.name_tab = Convert.ToString(reader["name_tab"]);

            if (reader["name_link"] == DBNull.Value) prm.name_link = "";
            else prm.name_link = Convert.ToString(reader["name_link"]);
        }


        /// <summary> Берет из кэша значения параметров
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<Prm> GetPrm(Prm finder, out Returns ret)
        {
            #region проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            #endregion

            #region соединение с БД кеш
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;


#if PG
            ExecSQL(conn_web, "set search_path to 'public'", false);
#endif
            #endregion

#if PG
            string tXX_spkvprm_full = pgDefaultDb + ".t" + Convert.ToString(finder.nzp_user) + "_spkvprm";
#else
            string tXX_spkvprm_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":t" + Convert.ToString(finder.nzp_user) + "_spkvprm";
#endif

            #region соединение с БД
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }
            #endregion

            string sWhere, s;

            #region условия для запроса
#if PG
            string skip = finder.skip > 0 ? " offset " + finder.skip : "";
#else
            string skip = finder.skip > 0 ? " skip " + finder.skip.ToString() : "";

#endif
            switch (finder.prm_num)
            {
                case 1: sWhere = " where prm_num in (1,3,15,18,19)"; break;
                case 0:
                case -1: sWhere = " where 1=1"; break;
                default: sWhere = " where prm_num=" + finder.prm_num.ToString(); break;
            }

            if (finder.name_prm != "") sWhere += " and name_prm ilike '%" + finder.name_prm + "%'";

            if (finder.nzp_prm > 0) sWhere += " and nzp_prm = " + finder.nzp_prm.ToString();
            if (finder.spis_prm != "") sWhere += " and nzp_prm in (" + finder.spis_prm.ToString() + ") ";
            if (finder.nzp_key > 0) sWhere += " and 0 < (select count(*) from " + tXX_spkvprm_full + " b where b.nzp_prm = a.nzp_prm and b.nzp = a.nzp and b.nzp_key = " + finder.nzp_key.ToString() + ")";
            if (finder.nzp > 0) sWhere += " and nzp = " + finder.nzp;

            if (Utils.GetParams(finder.dopprm, Constants.act_showallprm.ToString())) sWhere += " and ((1=1 ";

            if (finder.is_actual > 0) sWhere += " and is_actual = " + finder.is_actual.ToString(); //+ finder.is_actual.ToString();
            else if (finder.is_actual < 0) sWhere += " and is_actual <> " + (-1 * finder.is_actual).ToString(); // + finder.is_actual.ToString();


#warning костыль prm
            string specialWhere = sWhere;

            if ((finder.month_ > 0) && (finder.year_ > 0) &&
                !Utils.GetParams(finder.dopprm, Constants.act_showallprm.ToString()))
            {
#if PG
                sWhere += " and dat_s  <= public.mdy(" + finder.month_.ToString() + ",28," + finder.year_.ToString() +
                            ")" +
                            " and dat_po >= public.mdy(" + finder.month_.ToString() + ", 1," + finder.year_.ToString() +
                            ")";
#else
            sWhere += " and dat_s  <= mdy(" + finder.month_.ToString() + ",28," + finder.year_.ToString() + ")" +
                        " and dat_po >= mdy(" + finder.month_.ToString() + ", 1," + finder.year_.ToString() + ")";
#endif
            }


            if (finder.date_begin != "") sWhere += " and dat_po >= " + Utils.EStrNull(finder.date_begin);

            if (Utils.GetParams(finder.dopprm, Constants.act_showallprm.ToString())) sWhere += ") or (is_actual is null)) ";
            #endregion

            IDataReader readerprm = null, readerprm2 = null;
            int totalRecordCount = 0;
            List<Prm> spPrm = new List<Prm>();
            try
            {
                #region список параметров для текущего расчетного месяца
                if ((finder.month_ > 0) && (finder.year_ > 0))
                {
                    #region определить число записей
                    s = " Select 1 From " + tXX_spkvprm_full + sWhere + " group by nzp_prm, nzp, prm_num";
                    ret = ExecRead(conn_db, out readerprm, s, true);
                    if (!ret.result) throw new Exception(ret.text);

                    totalRecordCount = 0;
                    while (readerprm.Read()) totalRecordCount++;
                    readerprm.Close();
                    #endregion


#warning костыль prm
                    #region определить число доп записей
                    if ((finder.month_ > 0) && (finder.year_ > 0) &&
                        !Utils.GetParams(finder.dopprm, Constants.act_showallprm.ToString()) && finder.visible == 111)
                    {
                        s = " Select Distinct nzp_prm From " + tXX_spkvprm_full + specialWhere +
                            " AND nzp_prm not in" +
                            " (Select nzp_prm From " + tXX_spkvprm_full + sWhere + " group by nzp_prm, nzp, prm_num)" +
                            " group by nzp_prm, nzp, prm_num";
                        ret = ExecRead(conn_db, out readerprm, s, true);
                        if (!ret.result) throw new Exception(ret.text);

                        while (readerprm.Read()) totalRecordCount++;
                        readerprm.Close();
                    }

                    #endregion

                    #region получить данные
#if PG
                    s = " Select nzp_prm,name_prm,type_prm,nzp_res,prm_num,name_tab,name_link,nzp, pref, not_hist From " + tXX_spkvprm_full + sWhere
                        + " group by 1,2,3,4,5,6,7,8,9,10  order by name_prm " + skip;
#else
                    s = " Select " + skip + " nzp_prm,name_prm,type_prm,nzp_res,prm_num,name_tab,name_link,nzp, pref From " +
                    tXX_spkvprm_full + sWhere + " group by 1,2,3,4,5,6,7,8,9,10  order by name_prm ";

#endif
#warning костыль prm
                    #region определить число доп записей
                    if ((finder.month_ > 0) && (finder.year_ > 0) &&
                        !Utils.GetParams(finder.dopprm, Constants.act_showallprm.ToString()) && finder.visible == 111)
                    {
#if PG
                        s = "(" + s + ") " +
                            " UNION" +
                            " (Select nzp_prm,name_prm,type_prm,nzp_res,prm_num,name_tab,name_link,nzp, pref, not_hist" +
                            " From " + tXX_spkvprm_full + specialWhere + " AND nzp_prm not in" +
                            " (Select nzp_prm From " + tXX_spkvprm_full + sWhere + " group by nzp_prm, nzp, prm_num)" +
                            " group by 1,2,3,4,5,6,7,8,9,10 order by name_prm " + skip + ")  order by name_prm";
#else
                     s = "(" + s + ") " +
                            "UNION"+
" (Select " + skip + " nzp_prm,name_prm,type_prm,nzp_res,prm_num,name_tab,name_link,nzp, pref, not_hist From " +
                    tXX_spkvprm_full + specialWhere + " AND nzp_prm not in" +
                            " (Select nzp_prm From " + tXX_spkvprm_full + sWhere + " group by nzp_prm, nzp, prm_num)" + 
                        " group by 1,2,3,4,5,6,7,8,9,10 order by name_prm )";

#endif
                    }

                    #endregion
                    ret = ExecRead(conn_web, out readerprm, s, true);
                    if (!ret.result) throw new Exception(ret.text);

                    int i = 0;
                    while (readerprm.Read())
                    {
                        Prm zap = new Prm();
                        i++;
                        zap.num = (i + finder.skip).ToString() + " ";
                        zap.cnt = "1";

                        fillCommonFields(readerprm, zap);

                        #region получение значений val_prm, val_prm_link
                        s = "select nzp_key, val_prm, val_prm_link from " + tXX_spkvprm_full + sWhere +
                            " and nzp_prm = " + zap.nzp_prm.ToString() +
                            " and nzp = " + zap.nzp.ToString() +
                            " and ((is_actual <> 100 " +
#if PG
 " and dat_s  <= public.mdy(" + finder.month_.ToString() + ",28," + finder.year_.ToString() + ")" +
                                " and dat_po >= public.mdy(" + finder.month_.ToString() + ", 1," + finder.year_.ToString() + "))" +
#else
 " and dat_s  <= mdy(" + finder.month_.ToString() + ",28," + finder.year_.ToString() + ")" +
                                " and dat_po >= mdy(" + finder.month_.ToString() + ", 1," + finder.year_.ToString() + "))" +
#endif
 " or is_actual is null)";
                        ret = ExecRead(conn_db, out readerprm2, s, true);
                        if (!ret.result) throw new Exception(ret.text);

                        if (readerprm2.Read())
                        {
                            if (readerprm2["nzp_key"] == DBNull.Value) zap.nzp_key = 0;
                            else zap.nzp_key = Convert.ToInt32(readerprm2["nzp_key"]);

                            if (readerprm2["val_prm"] == DBNull.Value) zap.val_prm = "";
                            else zap.val_prm = Convert.ToString(readerprm2["val_prm"]).Trim().Replace("&quot;", "\"");

                            if (zap.type_prm == "bool" ||
                                zap.type_prm == "table" ||
                                zap.type_prm == "norm" ||
                                (zap.type_prm == "sprav" && !finder.isLoadParamInfo))
                            {
                                zap.val_prm = Convert.ToString(readerprm2["val_prm_link"]).Replace("&quot;", "\"");
                            }
                        }
                        else
                        {
                            readerprm2.Close();
                            s = "select nzp_key from " + tXX_spkvprm_full + sWhere + " and nzp_prm = " + zap.nzp_prm.ToString() + " and nzp = " + zap.nzp.ToString();
                            ret = ExecRead(conn_db, out readerprm2, s, true);
                            if (!ret.result) throw new Exception(ret.text);

                            if (readerprm2.Read())
                            {
                                if (readerprm2["nzp_key"] == DBNull.Value) zap.nzp_key = 0;
                                else zap.nzp_key = Convert.ToInt32(readerprm2["nzp_key"]);
                            }
                        }
                        readerprm2.Close();
                        #endregion

                        #region получить информацию о параметре
                        if (finder.isLoadParamInfo)
                        {
                            Param param = new Param();
                            param.pref = zap.pref;
                            param.nzp_prm = zap.nzp_prm;
                            zap.param = FindParam(conn_db, param, out ret);
                            if (!ret.result) throw new Exception(ret.text);
                        }
                        #endregion

                        spPrm.Add(zap);

                        if (i >= finder.rows) break;
                    }

                    readerprm.Close();
                    #endregion
                }
                #endregion

                #region список параметров по периодам
                else
                {
                    sWhere += " and dat_s is not null";

                    #region определить число записей
                    s = " Select count(*) From " + tXX_spkvprm_full + " a " + sWhere;
                    object obj = ExecScalar(conn_db, s, out ret, true);
                    if (!ret.result) throw new Exception(ret.text);

                    try { totalRecordCount = Convert.ToInt32(obj); }
                    catch { throw new Exception("Ошибка определения числа записей"); }
                    #endregion

                    #region получить данные
                    StringBuilder sql = new StringBuilder();
#if PG
                    sql.Append(" Select");
#else
                    sql.Append(" Select" + skip);

#endif
                    sql.Append(" nzp_key,pref, nzp_prm,name_prm, is_day_uchet,type_prm,nzp_res,prm_num,val_prm,val_prm_link,name_tab,name_link,not_hist,dat_s,dat_po,is_actual, nzp, dat_when, user_name, dat_del, delname");
                    sql.Append(" From " + tXX_spkvprm_full + " a " + sWhere);
#if PG
                    sql.Append(" Order by name_prm,name_link,dat_s desc " + skip);
#else
                    sql.Append(" Order by name_prm,name_link,dat_s desc ");

#endif
                    ret = ExecRead(conn_db, out readerprm, sql.ToString(), true);
                    if (!ret.result) throw new Exception(ret.text);

                    int i = 0;
                    DateTime tempdate;
                    while (readerprm.Read())
                    {
                        Prm zap = new Prm();

                        i++; zap.num = (i + finder.skip).ToString() + " ";

                        zap.cnt = "1";

                        fillCommonFields(readerprm, zap);

                        #region Проверка блокировки записей
                        string bl = "";
                        if (i == 1)
                        {
                            #region определение локального пользователя
                            finder.pref = zap.pref;
                            int nzpUser = finder.nzp_user;
                            /*DbWorkUser db = new DbWorkUser();
                            int nzpUser = db.GetLocalUser(conn_db, finder, out ret);
                            db.Close();
                            if (!ret.result) throw new Exception(ret.text);*/
                            #endregion

                            string prm_N = "prm_" + zap.prm_num;
#if PG
                            string prm_N_full = zap.pref + "_data." + prm_N;
#else
                            string prm_N_full = zap.pref + "_data:" + prm_N;
#endif

                            sql = new StringBuilder();
#if PG
                            sql.Append("Select p.dat_block, p.user_block, u.comment as user_name_block, (now() - INTERVAL " + string.Format(" '{0} minutes')", Constants.blocking_lifetime) + " as cur_dat");
#else
                            sql.Append("Select p.dat_block, p.user_block, u.comment as user_name_block, (current year to second - " + Constants.blocking_lifetime.ToString() + " units minute) as cur_dat");
#endif
                            sql.Append(" From " + prm_N_full + " p");
#if PG
                            sql.Append(" left outer join " + Points.Pref + "_data.users u on p.user_block = u.nzp_user");
                            sql.Append(" Where p.nzp_prm = " + zap.nzp_prm + " and p.nzp = " + zap.nzp);
#else
                            sql.Append(", outer " + Points.Pref + "_data:users u ");
                            sql.Append(" Where p.nzp_prm = " + zap.nzp_prm + " and p.nzp = " + zap.nzp);
                            sql.Append(" and p.user_block = u.nzp_user ");

#endif
                            ret = ExecRead(conn_db, out readerprm2, sql.ToString(), true);
                            if (!ret.result) throw new Exception(ret.text);

                            if (readerprm2.Read())
                            {
                                DateTime dt_block = DateTime.MinValue;
                                DateTime dt_cur = DateTime.MinValue;
                                int user_block = 0;
                                string userNameBlock = "";

                                if (readerprm2["user_block"] != DBNull.Value) user_block = (int)readerprm2["user_block"]; //пользователь, который заблокировал
                                if (readerprm2["user_name_block"] != DBNull.Value) userNameBlock = ((string)readerprm2["user_name_block"]).Trim(); //имя пользователь, который заблокировал
                                if (readerprm2["dat_block"] != DBNull.Value) dt_block = Convert.ToDateTime(readerprm2["dat_block"]);//дата блокировки
                                if (readerprm2["cur_dat"] != DBNull.Value) dt_cur = Convert.ToDateTime(readerprm2["cur_dat"]);//текущее время/дата - XX мин

                                if (user_block > 0 && dt_block != DateTime.MinValue) //заблокирован прибор учета
                                    if (user_block != nzpUser && dt_cur < dt_block) //если заблокирована запись другим пользователем и XX мин не прошло
                                        bl = "Параметр заблокирован пользователем " + userNameBlock + " " + dt_block.ToString("dd.MM.yyyy в HH:mm") + ". Редактировать данные запрещено.";

                                if (bl == "") // действующей блокировки нет, или она сделана самим пользователем
                                {
                                    if (Utils.GetParams(finder.prms, Constants.act_mode_edit.ToString())) //если берут данные на изменение
                                    {
#if PG
                                        //ret = ExecSQL(conn_db, "update " + prm_N_full + " set dat_block = date_trunc('minute', now()), user_block = " + nzpUser + " where nzp_prm = " + zap.nzp_prm + " and nzp = " + zap.nzp, true);
#else
                                        //ret = ExecSQL(conn_db, "update " + prm_N_full + " set dat_block = current year to minute, user_block = " + nzpUser + " where nzp_prm = " + zap.nzp_prm + " and nzp = " + zap.nzp, true);
#endif
                                    }
                                    else //если  на просмотр
                                    {
                                        ret = ExecSQL(conn_db, "update " + prm_N_full + " set dat_block = null, user_block = null where nzp_prm = " + zap.nzp_prm + " and nzp = " + zap.nzp, true);
                                    }
                                    if (!ret.result) throw new Exception("Ошибка обновления таблицы prm_" + prm_N);
                                }
                            }
                            if (readerprm2 != null) readerprm2.Close();
                        }
                        zap.block = bl;
                        #endregion

                        if (readerprm["nzp_key"] == DBNull.Value) zap.nzp_key = 0;
                        else zap.nzp_key = Convert.ToInt32(readerprm["nzp_key"]);

                        if (readerprm["is_actual"] == DBNull.Value) zap.is_actual = 0;
                        else zap.is_actual = Convert.ToInt32(readerprm["is_actual"]);

                        if (readerprm["is_day_uchet"] == DBNull.Value) zap.is_day_uchet = 0;
                        else zap.is_day_uchet = Convert.ToInt32(readerprm["is_day_uchet"]);

                        Param param = new Param();
                        param.nzp_prm = zap.nzp_prm;
                        param.is_day_uchet = zap.is_day_uchet;

                        switch (param.intvtype)
                        {
                            case enIntvType.intv_Month:
                                if (readerprm["dat_s"] == DBNull.Value) zap.dat_s = "";
                                else zap.dat_s = String.Format("{0:MM.yyyy}", readerprm["dat_s"]);
                                if (zap.dat_s != "")
                                    if (DateTime.TryParse("01." + zap.dat_s, out tempdate))
                                        if (tempdate <= new DateTime(1900, 1, 1)) zap.dat_s = "";

                                if (readerprm["dat_po"] == DBNull.Value) zap.dat_po = "";
                                else zap.dat_po = String.Format("{0:MM.yyyy}", readerprm["dat_po"]);
                                if (zap.dat_po == "01.3000") zap.dat_po = "";
                                break;
                            default:
                                if (readerprm["dat_s"] == DBNull.Value) zap.dat_s = "";
                                else zap.dat_s = String.Format("{0:dd.MM.yyyy}", readerprm["dat_s"]);
                                if (zap.dat_s != "")
                                    if (DateTime.TryParse("01." + zap.dat_s, out tempdate))
                                        if (tempdate <= new DateTime(1900, 1, 1)) zap.dat_s = "";

                                if (readerprm["dat_po"] == DBNull.Value) zap.dat_po = "";
                                else zap.dat_po = String.Format("{0:dd.MM.yyyy}", readerprm["dat_po"]);
                                if (zap.dat_po == "01.01.3000") zap.dat_po = "";
                                break;
                        }

                        if (readerprm["val_prm"] == DBNull.Value) zap.val_prm = "";
                        else zap.val_prm = Convert.ToString(readerprm["val_prm"]).Trim().Replace("&quot;", "\"");

                        if (readerprm["dat_when"] == DBNull.Value) zap.dat_when = "";
                        else
                        {
                            zap.dat_when = String.Format("{0:dd.MM.yyyy}", readerprm["dat_when"]);
                            if (readerprm["user_name"] != DBNull.Value)
                                if (Convert.ToString(readerprm["user_name"]).Trim() != "")
                                    zap.dat_when += " (" + Convert.ToString(readerprm["user_name"]).Trim() + ")";
                        }

                        if (readerprm["dat_del"] == DBNull.Value) zap.dat_del = "";
                        else
                        {
                            zap.dat_del = String.Format("{0:dd.MM.yyyy}", readerprm["dat_del"]);
                            if (readerprm["delname"] != DBNull.Value)
                                if (Convert.ToString(readerprm["delname"]).Trim() != "")
                                    zap.dat_del += " (" + Convert.ToString(readerprm["delname"]).Trim() + ")";
                        }

                        if (zap.type_prm == "sprav" ||
                            zap.type_prm == "norm" ||
                            zap.type_prm == "bool" ||
                            zap.type_prm == "table") zap.val_prm = Convert.ToString(readerprm["val_prm_link"]).Replace("&quot;", "\"");

                        spPrm.Add(zap);
                    }

                    /*if (spPrm.Count == 0)
                    {
                        if (finder.nzp_prm > 0) s = "select nzp_key, name_prm from " + tXX_spkvprm + " where nzp_prm = " + finder.nzp_prm.ToString();
                        else if (finder.nzp_key > 0) s = "select nzp_key, name_prm from " + tXX_spkvprm + " where nzp_key = " + finder.nzp_key.ToString();

                        if (s != "")
                        {
                            if (ExecRead(conn_web, out readerprm, s, true).result)
                            {
                                if (readerprm.Read())
                                {
                                    Prm zap = new Prm();

                                    if (readerprm["nzp_key"] != DBNull.Value) zap.nzp_key = (int)readerprm["nzp_key"];

                                    if (readerprm["name_prm"] == DBNull.Value) zap.name_prm = "";
                                    else zap.name_prm = Convert.ToString(readerprm["name_prm"]);

                                    spPrm.Add(zap);
                                }
                            }
                        }
                    }*/

                    readerprm.Close();
                    #endregion
                }
                #endregion
            }
            catch (Exception ex)
            {
                if (readerprm != null) readerprm.Close();
                if (readerprm2 != null) readerprm2.Close();
                conn_web.Close();
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка поиска параметров GetPrm " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            conn_web.Close();
            conn_db.Close();
            ret.tag = totalRecordCount;
            return spPrm;
        }


        /// <summary> Загружает в кэш список параметров со значениями
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        public void FindPrm(Prm finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return;
            }

            if (finder.pref.Trim() == "" && finder.prm_num != 5 && finder.prm_num != 7 && finder.prm_num != 8 && finder.prm_num != 9 && finder.prm_num != 10 && finder.prm_num != 12)
            {
                ret = new Returns(false, "Не определен префикс базы данных");
                return;
            }
            else if (finder.pref.Trim() == "") finder.pref = Points.Pref;

            if (finder.prm_num == 7)
            {
                if (finder.nzp <= 0)
                {
                    ret = new Returns(false, "Не определена Управляющая организация");
                    return;
                }
            }
            else if (finder.prm_num == 8)
            {
                if (finder.nzp <= 0)
                {
                    ret = new Returns(false, "Не определен участок");
                    return;
                }
            }
            else if (finder.prm_num == 9)
            {
                if (finder.nzp <= 0)
                {
                    ret = new Returns(false, "Не определен котрагент");
                    return;
                }
            }
            else if (finder.prm_num == 11)
            {
                if (finder.nzp <= 0)
                {
                    ret = new Returns(false, "Не определен поставщик");
                    return;
                }
            }
            else if (finder.prm_num == 12)
            {
                if (finder.nzp <= 0)
                {
                    ret = new Returns(false, "Не определена услуга");
                    return;
                }
            }
            else if (finder.prm_num == 17)
            {
                if (finder.nzp <= 0)
                {
                    ret = new Returns(false, "Не определен прибор учета");
                    return;
                }
            }
            else if (finder.prm_num != 5 && finder.prm_num != 10)
            {
                if (finder.nzp_dom.ToString() == "")
                {
                    ret = new Returns(false, "Не определен дом");
                    return;
                }
                if (finder.nzp_kvar < 0 && finder.nzp_dom < 0)
                {
                    ret = new Returns(false, "Не определен л/с");
                    return;
                }
            }
            #endregion

            ret = Utils.InitReturns();

            #region соединение с бд Webdata
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

#if PG
            ret = ExecSQL(conn_web, "set search_path to 'public'", false);
#endif

            #endregion


#if PG
            string tXX_spkvprm = pgDefaultDb + "." + "t" + Convert.ToString(finder.nzp_user) + "_spkvprm";
            string tXX_spkvprm_full = tXX_spkvprm;
#else
            string tXX_spkvprm = "t" + Convert.ToString(finder.nzp_user) + "_spkvprm";
            string tXX_spkvprm_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spkvprm;
#endif

            if (finder.nzp_prm < 1)
            {
                #region удалить существующую таблицу tXX_spkvprm
                if (TempTableInWebCashe(conn_web, tXX_spkvprm))
                {
                    ret = ExecSQL(conn_web, " Drop table " + tXX_spkvprm, false);
                }
                #endregion
            }

            bool key = false;
            if (!TempTableInWebCashe(conn_web, tXX_spkvprm))
            {
                #region создать таблицу webdata:tXX_spkvprm
                ret = ExecSQL(conn_web,
                          " Create table " + tXX_spkvprm + " (" +
                          "   nzp_key serial not null," +
                          "   nzp_prm  integer," +
                          "   pref     char(20)," +
                          "   name_prm char(100)," +
                          "   type_prm char(10)," +
                          "   nzp_res  integer," +
                          "   prm_num  integer," +
                          "   nzp      integer," +
                          "   not_hist integer default  0," +
                          "   val_prm  varchar(255)," +
                          "   val_prm_link  varchar(255)," +
                          "   name_tab char(20)," +
                          "   name_link char(100), " +
                          "   is_actual integer, " +
                          "   dat_s date, " +
                          "   dat_po date, " +
                          "   dat_when date, " +
                          "   nzp_user integer, " +
                          "   user_name char(100), " +
                          "   dat_del date, " +
                          "   userdel integer, " +
                          "   delname char(100), " +
                          "   is_day_uchet integer DEFAULT 0 " +
                          " ) "
#if PG
 + " with oids "
#endif
, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return;
                }
                key = true;
                #endregion
            }
            else if (finder.nzp_prm < 1)
                ret = ExecSQL(conn_web, " delete from " + tXX_spkvprm, false);
            else
                ret = ExecSQL(conn_web, " delete from " + tXX_spkvprm + " where nzp_prm = " + finder.nzp_prm, false);

            #region соединение с бд Kernel
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }
#if PG
            ret = ExecSQL(conn_db, "set search_path to '" + finder.pref + "_data'", true);
#else
            ret = ExecSQL(conn_db, "database " + finder.pref + "_data", true);
#endif
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }
            #endregion

            #region фильтр по л/с
            string sFndKvar = "";
            string sFndDom = "";
            if (finder.nzp_kvar > 0) sFndKvar = " and k.nzp_kvar=" + finder.nzp_kvar.ToString();
            else if (finder.nzp_dom > 0) sFndDom = " and k.nzp_dom=" + finder.nzp_dom.ToString();
            #endregion

            #region фильтр по услугам и поставщикам
            string filter = "";
            string filterPrm = "";
            string filterSupp = "";
            if (finder.RolesVal != null && finder.RolesVal.Count > 0)
            {
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip != Constants.role_sql) continue;
                    switch (role.kod)
                    {
                        case Constants.role_sql_serv:
                            filter += " and b.nzp_serv in (" + role.val + ")";
                            break;
                        case Constants.role_sql_supp:
                            filter += " and b.nzp_supp in (" + role.val + ")";
                            filterSupp += " and p.nzp in (" + role.val + ")";
                            break;
                        case Constants.role_sql_prm:
                            filterPrm += " and n.nzp_prm in (" + role.val + ")";
                            break;
                    }
                }
            }
            if (finder.nzp_serv > 0) filter += " and b.nzp_serv = " + finder.nzp_serv;

            string sSuppFilter = "";
            if (filter != "")
#if PG
                sSuppFilter += " and ( 0<(select count(*) from " + finder.pref + "_kernel.prm_frm a," + finder.pref + "_kernel.l_foss b " +
                                    " where n.nzp_prm=a.nzp_prm and a.is_prm = 1 and (a.nzp_frm=b.nzp_frm or a.nzp_frm=-1) " + filter + " )) ";
#else
                sSuppFilter += " and ( 0<(select count(*) from " + finder.pref + "_kernel:prm_frm a," + finder.pref + "_kernel:l_foss b " +
                                    " where n.nzp_prm=a.nzp_prm and a.is_prm = 1 and (a.nzp_frm=b.nzp_frm or a.nzp_frm=-1) " + filter + " )) ";
#endif
            #endregion

            filter = filterPrm;
            if (finder.nzp_serv > 0 & finder.nzp_kvar > 0)
            {
#if PG
                filter += " and 0 < (select count(*) from " + finder.pref + "_data.tarif t1, " + finder.pref + "_kernel.prm_frm p1 where t1.nzp_kvar = " + finder.nzp_kvar + " and t1.nzp_serv = " + finder.nzp_serv + " and t1.nzp_frm = p1.nzp_frm and p1.nzp_prm = n.nzp_prm)";
#else
                filter += " and 0 < (select count(*) from " + finder.pref + "_data:tarif t1, " + finder.pref + "_kernel:prm_frm p1 where t1.nzp_kvar = " + finder.nzp_kvar + " and t1.nzp_serv = " + finder.nzp_serv + " and t1.nzp_frm = p1.nzp_frm and p1.nzp_prm = n.nzp_prm)";
#endif
            }

            if (finder.nzp_prm >= 1) filter += " and n.nzp_prm = " + finder.nzp_prm;
            if (finder.nzp > 0) filter += finder.dopprm.Length == 0 ? (" and p.nzp = " + finder.nzp) : "";
            if (finder.date_begin != "") filter += " and p.dat_po >= " + Utils.EStrNull(finder.date_begin);

            string isDayUchet = (finder.prm_num == 10 && finder.pref == Points.Pref) ? string.Empty : "n.is_day_uchet,";

            string insert = " Insert into " + tXX_spkvprm_full;

            string values1 =
                    " (nzp_prm, pref, name_prm, is_day_uchet, type_prm,val_prm,nzp_res,prm_num,nzp,name_tab,is_actual,dat_s,dat_po, dat_when, nzp_user, user_name, dat_del, userdel, delname, not_hist) ";
            string select1 = " select n.nzp_prm, '" + finder.pref
                             + "', n.name_prm, n.is_day_uchet,n.type_prm,p.val_prm,n.nzp_res,n.prm_num,p.nzp,t.name_tab, p.is_actual, p.dat_s, p.dat_po, p.dat_when, p.nzp_user, trim(u.comment), p.dat_del, p.user_del, trim(u1.comment), (case when p.nzp_key is null then 1 else 0 end ) ";
            string select_kvar = " select n.nzp_prm, '" + finder.pref
                                 + "', n.name_prm, n.is_day_uchet,n.type_prm,p.val_prm,n.nzp_res,n.prm_num,k.nzp_kvar,t.name_tab, p.is_actual, p.dat_s, p.dat_po, p.dat_when, p.nzp_user, trim(u.comment), p.dat_del, p.user_del, trim(u1.comment), (case when p.nzp_key is null then 1 else 0 end )  ";

            string select_dom = " select n.nzp_prm, '" + finder.pref
                                         + "', n.name_prm, n.is_day_uchet,n.type_prm,p.val_prm,n.nzp_res,n.prm_num,k.nzp_dom,t.name_tab, p.is_actual, p.dat_s, p.dat_po, p.dat_when, p.nzp_user, trim(u.comment), p.dat_del, p.user_del, trim(u1.comment) , (case when p.nzp_key is null then 1 else 0 end ) ";

            string select_dom2 = " select n.nzp_prm, '" + finder.pref + "', n.name_prm, n.is_day_uchet,n.type_prm,p.val_prm,n.nzp_res,n.prm_num,"
                                    + finder.nzp_dom.ToString()
                                    + ",t.name_tab, p.is_actual, p.dat_s, p.dat_po, p.dat_when, p.nzp_user, trim(u.comment), p.dat_del, p.user_del, trim(u1.comment), (case when p.nzp_key is null then 1 else 0 end )  ";
            string values2 =
                " (nzp_prm,pref,name_prm, is_day_uchet,type_prm,val_prm,nzp_res,prm_num,nzp,name_tab,name_link,is_actual,dat_s,dat_po, dat_when, nzp_user, user_name, dat_del, userdel, delname, not_hist) ";
            string select2 = " Select n.nzp_prm,'" + finder.pref
                             + "',n.name_prm, n.is_day_uchet,n.type_prm,p.val_prm,n.nzp_res,n.prm_num,p.nzp,t.name_tab,s.name_supp, p.is_actual, p.dat_s, p.dat_po, p.dat_when, p.nzp_user, trim(u.comment), p.dat_del, p.user_del, trim(u1.comment), (case when p.nzp_key is null then 1 else 0 end )  ";
            string select3 = " Select n.nzp_prm,'" + finder.pref
                             + "',n.name_prm, n.is_day_uchet,n.type_prm,p.val_prm,n.nzp_res,n.prm_num,p.nzp,t.name_tab,s.service, p.is_actual, p.dat_s, p.dat_po, p.dat_when, p.nzp_user, trim(u.comment), p.dat_del, p.user_del, trim(u1.comment), (case when p.nzp_key is null then 1 else 0 end )  ";

            string values4 =
                 " (nzp_prm,pref,name_prm, " + isDayUchet.TrimStart('n', '.') + "type_prm,val_prm,nzp_res,prm_num,nzp,name_tab, dat_when, nzp_user, user_name, dat_del, userdel, delname, is_actual, dat_s, dat_po, not_hist) ";

            string select4 = " Select n.nzp_prm,'" + finder.pref
                                   + "',n.name_prm, " + isDayUchet + "n.type_prm,p.val_prm,n.nzp_res,n.prm_num,p.nzp,t.name_tab, p.dat_when, p.nzp_user,trim(u.comment), p.dat_del, p.user_del, trim(u1.comment), p.is_actual, p.dat_s, p.dat_po, (case when p.nzp_key is null then 1 else 0 end )  ";

#if PG
            string prm_name_n = Points.Pref + "_kernel.prm_name n ";
            string prm_table_t = finder.pref + "_kernel.prm_table t ";
            string prm_1_p = finder.pref + "_data.prm_1 p ";
            string prm_2_p = finder.pref + "_data.prm_2 p ";
            string prm_3_p = finder.pref + "_data.prm_3 p ";
            string prm_5_p = finder.pref + "_data.prm_5 p ";
            string prm_7_p = finder.pref + "_data.prm_7 p ";
            string prm_8_p = finder.pref + "_data.prm_8 p ";
            string prm_9_p = finder.pref + "_data.prm_9 p ";
            string prm_10_p = finder.pref + "_data.prm_10 p ";
            string prm_11_p = finder.pref + "_data.prm_11 p ";
            string prm_12_p = finder.pref + "_data.prm_12 p ";
            string prm_15_p = finder.pref + "_data.prm_15 p ";
            string prm_17_p = finder.pref + "_data.prm_17 p ";
            string prm_18_p = finder.pref + "_data.prm_18 p ";
            string prm_19_p = finder.pref + "_data.prm_19 p ";
            string kvar_k = finder.pref + "_data.kvar k ";
            string tarif_f = finder.pref + "_data.tarif f ";
            string supplier_s = finder.pref + "_kernel.supplier s";
            string services_s = finder.pref + "_kernel.services s";
            string area_a = finder.pref + "_data.s_area a";
            string payer_pr = finder.pref + "_kernel.s_payer pr";
            string geu_g = finder.pref + "_data.s_geu g";
            string users_u = Points.Pref + "_data.users u";
            string users_u1 = Points.Pref + "_data.users u1";
#else
            string prm_name_n = Points.Pref + "_kernel:prm_name n ";
            string prm_table_t = finder.pref + "_kernel:prm_table t ";
            string prm_1_p = finder.pref + "_data:prm_1 p ";
            string prm_2_p = finder.pref + "_data:prm_2 p ";
            string prm_3_p = finder.pref + "_data:prm_3 p ";
            string prm_5_p = finder.pref + "_data:prm_5 p ";
            string prm_7_p = finder.pref + "_data:prm_7 p ";
            string prm_8_p = finder.pref + "_data:prm_8 p ";
            string prm_10_p = finder.pref + "_data:prm_10 p ";
            string prm_11_p = finder.pref + "_data:prm_11 p ";
            string prm_12_p = finder.pref + "_data:prm_12 p ";
            string prm_15_p = finder.pref + "_data:prm_15 p ";
            string prm_17_p = finder.pref + "_data:prm_17 p ";
            string prm_18_p = finder.pref + "_data:prm_18 p ";
            string prm_19_p = finder.pref + "_data:prm_19 p ";
            string kvar_k = finder.pref + "_data:kvar k ";
            string tarif_f = finder.pref + "_data:tarif f ";
            string supplier_s = finder.pref + "_kernel:supplier s";
            string services_s = finder.pref + "_kernel:services s";
            string area_a = finder.pref + "_data:s_area a";
            string geu_g = finder.pref + "_data:s_geu g";
            string users_u = Points.Pref + "_data:users u";
            string users_u1 = Points.Pref + "_data:users u1";
#endif

            StringBuilder sql;

            #region квартирные параметры
            if (finder.nzp_kvar > 0)
            {
                // параметры л/с из prm_1
                sql = new StringBuilder(insert + values1 + select_kvar);
#if PG
                /*   sql.Append("From " + kvar_k);
                sql.AppendFormat(" left outer join {0} on k.nzp_kvar=p.nzp ", prm_1_p);
                sql.AppendFormat(" left outer join {0} on p.nzp_prm=n.nzp_prm ", prm_name_n);
                sql.AppendFormat(" left outer join {0} on n.prm_num=t.prm_num ", prm_table_t);
                sql.AppendFormat(" left outer join {0} on p.nzp_user = u.nzp_user ", users_u);
                sql.AppendFormat(" left outer join {0} on p.user_del = u1.nzp_user ", users_u1);
                sql.Append(" Where  n.prm_num=1 ");*/

                sql.Append("From " + prm_table_t + ", " + prm_name_n +
                     " inner join " + kvar_k + " on 1=1 " + sFndKvar +
                    " left outer join " + prm_1_p +

                    " left outer join " + users_u + " on p.nzp_user = u.nzp_user " +
                    " left outer join " + users_u1 + " on p.user_del = u1.nzp_user " +
                    " on p.nzp_prm=n.nzp_prm  and k.nzp_kvar=p.nzp");
                sql.Append(" Where n.prm_num=t.prm_num and n.prm_num=1 ");
                sql.Append(sSuppFilter + filter);
#else
                sql.Append("From " + prm_name_n + ", " + kvar_k + ", " + prm_table_t + ", outer (" + prm_1_p + ", outer " + users_u + ", outer " + users_u1 + ")");
                sql.Append(" Where k.nzp_kvar=p.nzp and p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=1 and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user ");
                sql.Append(sFndKvar + sSuppFilter + filter);
#endif

                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }

                // параметры л/с из prm_3
#if PG
                /* sql = new StringBuilder(insert + values1 + select_kvar);
                sql.Append(" From " + kvar_k)
                    .AppendFormat(" left outer join {0} on k.nzp_kvar=p.nzp ", prm_3_p)
                    .AppendFormat(" left outer join {0} on p.nzp_prm=n.nzp_prm ", prm_name_n)
                    .AppendFormat(" left outer join {0} on n.prm_num=t.prm_num ", prm_table_t)
                    .AppendFormat(" left outer join {0} on p.nzp_user = u.nzp_user ", users_u)
                    .AppendFormat(" left outer join {0} on p.user_del = u1.nzp_user ", users_u1)
                    .Append(" where n.prm_num=3");
                sql.Append(sFndKvar);
                sql.Append(filter);*/

                sql = new StringBuilder(insert + values1 + select_kvar +
                    " From " + prm_table_t + "," + kvar_k + "," + prm_name_n + "," +
                    prm_3_p +
                    " left outer join " + users_u + " on p.nzp_user = u.nzp_user " +
                    " left outer join " + users_u1 + " on p.user_del = u1.nzp_user " +
                    " where k.nzp_kvar=p.nzp and p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=3 " +
                sFndKvar + filter);
#else
                sql = new StringBuilder(insert + values1 + select_kvar +
                    " From " + prm_name_n + "," + prm_3_p + "," + prm_table_t + "," + kvar_k + ", outer " + users_u + ", outer " + users_u1 +
                    " where k.nzp_kvar=p.nzp and p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=3 and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user " +
                sFndKvar + filter);
#endif

                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }

                // параметры л/с из prm_15
#if PG
                /* sql = new StringBuilder(insert + values1 + select_kvar);
                sql.AppendFormat(" From {0}", kvar_k);
                sql.AppendFormat(" left outer join {0} on k.nzp_kvar=p.nzp ", prm_15_p);
                sql.AppendFormat(" left outer join {0} on p.nzp_prm=n.nzp_prm ", prm_name_n);
                sql.AppendFormat(" left outer join {0} on n.prm_num=t.prm_num ", prm_table_t);
                sql.AppendFormat(" left outer join {0} on p.nzp_user = u.nzp_user ", users_u);
                sql.AppendFormat(" left outer join {0} on p.user_del = u1.nzp_user ", users_u1);
                sql.Append(" where n.prm_num=15 ");
                sql.Append(sFndKvar);
                sql.Append(filter);*/

                sql = new StringBuilder(insert + values1 + select_kvar +
                                        " From " + prm_name_n + "," + prm_table_t + "," + kvar_k + "," + prm_15_p +
                                        " left outer join " + users_u + " on p.nzp_user = u.nzp_user " +
                                        " left outer join " + users_u1 + " on p.user_del = u1.nzp_user " +
                                        " where k.nzp_kvar=p.nzp and p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=15 " +
                                        sFndKvar + filter);
#else
                sql = new StringBuilder(insert + values1 + select_kvar +
                                        " From " + prm_name_n + "," + prm_15_p + "," + prm_table_t + "," + kvar_k + ", outer " + users_u + ", outer " + users_u1 +
                                        " where k.nzp_kvar=p.nzp and p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=15 and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user " +
                                        sFndKvar + filter);

#endif
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }

                if (Points.IsSmr && TableInWebCashe(conn_db, "prm_18"))
                {
                    // параметры л/с из prm_18
#if PG
                    /*   sql = new StringBuilder(insert + values1 + select_kvar);
                    sql.Append(" From " + kvar_k);
                    sql.AppendFormat(" left outer join {0} on k.nzp_kvar=p.nzp ", prm_18_p);
                    sql.AppendFormat(" left outer join {0} on p.nzp_prm=n.nzp_prm ", prm_name_n);
                    sql.AppendFormat(" left outer join {0} on n.prm_num=t.prm_num ", prm_table_t);
                    sql.AppendFormat(" left outer join {0} on p.nzp_user = u.nzp_user", users_u);
                    sql.AppendFormat(" left outer join {0} on p.user_del = u1.nzp_user", users_u1);
                    sql.Append(" Where n.prm_num=18");
                    sql.Append(sFndKvar + filter);*/

                    sql = new StringBuilder(insert + values1 + select_kvar +
                                            " From " + prm_table_t + "," + prm_name_n +
                                            " left outer join " + prm_18_p +
                                            " inner join " + kvar_k + " on k.nzp_kvar=p.nzp " + sFndKvar +
                                            " left outer join " + users_u + " on p.nzp_user = u.nzp_user " +
                                            " left outer join " + users_u1 + " on p.user_del = u1.nzp_user" +
                                            " on  p.nzp_prm=n.nzp_prm" +
                                            " Where  n.prm_num=t.prm_num  and n.prm_num=18 " +
                                              filter);
#else
                    sql = new StringBuilder(insert + values1 + select_kvar +
                                            " From " + prm_name_n + "," + kvar_k + "," + prm_table_t + ", outer (" + prm_18_p + ", outer " + users_u + ", outer " + users_u1 + ")" +
                                            " Where k.nzp_kvar=p.nzp and p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=18 and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user " +
                                            sFndKvar + filter);

#endif
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        conn_web.Close();
                        return;
                    }
                }

                if (Points.IsSmr && TableInWebCashe(conn_db, "prm_19"))
                {
                    // параметры л/с для паспортистки из prm_19
#if PG
                    /* sql = new StringBuilder(insert + values1 + select_kvar);
                    sql.AppendFormat(" From " + kvar_k)
                        .AppendFormat(" left outer join {0} on k.nzp_kvar=p.nzp ", prm_19_p)
                        .AppendFormat(" left outer join {0} on p.nzp_prm=n.nzp_prm ", prm_name_n)
                        .AppendFormat(" left outer join {0} on n.prm_num=t.prm_num ", prm_table_t)
                        .AppendFormat(" left outer join {0} on p.nzp_user = u.nzp_user ", users_u)
                        .AppendFormat(" left outer join {0} on p.user_del = u1.nzp_user ", users_u1)
                        .Append(" Where  n.prm_num=19")
                        .Append(sFndKvar + filter);*/

                    sql = new StringBuilder(insert + values1 + select_kvar +
                                            " From " + prm_table_t + "," + prm_name_n +
                                            " left outer join " + prm_19_p +
                                            " inner join " + kvar_k + " on k.nzp_kvar=p.nzp " + sFndKvar +
                                            " left outer join " + users_u + " on p.nzp_user = u.nzp_user " +
                                            " left outer join " + users_u1 + " on p.user_del = u1.nzp_user " +
                                            " on p.nzp_prm=n.nzp_prm " +
                                            " Where n.prm_num=t.prm_num  and n.prm_num=19  " +
                                            filter);
#else
                    sql = new StringBuilder(insert + values1 + select_kvar +
                                            " From " + prm_name_n + "," + kvar_k + "," + prm_table_t + ", outer (" + prm_19_p + ", outer " + users_u + ", outer " + users_u1 + ")" +
                                            " Where k.nzp_kvar=p.nzp and p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=19 and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user " +
                                            sFndKvar + filter);

#endif
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        conn_web.Close();
                        return;
                    }
                }
            }
            #endregion

            #region параметры дома из prm_2
            if (finder.nzp_kvar > 0 || finder.nzp_dom > 0)
            {
                if (finder.nzp_kvar > 0)
                {
                    sql = new StringBuilder(insert + values1 + select_dom);
#if PG

                    /*  var sqlBuilder = new SqlBuilder();
                    sqlBuilder.Append(insert + values1 + select_dom);
                    sqlBuilder.From(kvar_k, prm_table_t, prm_2_p, prm_name_n, users_u, users_u1)
                        .Where("p.nzp=k.nzp_dom")
                        .And("n.prm_num=t.prm_num")
                        .And("p.nzp_user = u.nzp_user")
                        .And("p.user_del = u1.nzp_user")
                        .And("n.prm_num=2")
                        .Append(sFndKvar + sSuppFilter + filter);

                    sql = sqlBuilder.AsStringBuilder();*/

                    sql.Append(" from " + prm_table_t + ", " + prm_name_n +
                        "  inner join " + kvar_k + " on 1=1 " + sFndKvar +
                        " left outer join " + prm_2_p +

                        "  left outer join " + users_u + " on p.nzp_user = u.nzp_user " +
                        "   left outer join " + users_u1 + "  on p.user_del = u1.nzp_user " +
                        " on p.nzp_prm=n.nzp_prm and p.nzp=k.nzp_dom" +
                        " where n.prm_num=2  and n.prm_num=t.prm_num  " +
                          sSuppFilter + filter);

#else
                    sql.Append("From " + prm_name_n + ", " + kvar_k + ", " + prm_table_t + ", outer (" + prm_2_p + ", outer " + users_u + ", outer " + users_u1 + ")");
                    sql.Append(" Where p.nzp=k.nzp_dom and p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=2 and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user " +
                        sFndKvar + sSuppFilter + filter);

#endif
                }
                else
                {
                    sql = new StringBuilder(insert + values1 + select_dom2);
#if PG

                    /*  string podzapros = "From " + prm_table_t + ", " + prm_name_n + 
                                  "  LEFT OUTER JOIN (	SELECT		u. COMMENT,		P .nzp,		P .val_prm,		P .is_actual,		P .dat_s,		P .dat_po,		P .dat_when,		P .nzp_prm,		P .nzp_user,		TRIM (u. COMMENT) AS comm,	"
                                  +
                                  "  TRIM (u1. COMMENT) AS comm1,		P .dat_del,		P .user_del 	FROM  "
                                  + prm_2_p + " LEFT OUTER JOIN " + users_u + "  ON P .nzp_user = u.nzp_user    " +
                                  " 	LEFT OUTER JOIN " + users_u1 +
                                  " ON P .user_del = u1.nzp_user ) AS pp ON pp.nzp =  " + finder.nzp_dom.ToString() +
                                  " AND pp.nzp_prm = n.nzp_prm  WHERE n.prm_num = 2   AND n.prm_num = T .prm_num";

                    sql.Append(podzapros);*/
                    //sql.Append("From " + prm_name_n + ", " + prm_table_t + "," + prm_2_p + "," + users_u + "," + users_u1);
                    //sql.Append(" Where p.nzp=" + finder.nzp_dom.ToString() + " and p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=2 and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user " +
                    //    sSuppFilter + filter);

                    sql.Append("From " + prm_table_t + ", " + prm_name_n +
                        " left outer join " + prm_2_p +
                        " left outer join " + users_u + " on p.nzp_user = u.nzp_user " +
                        " left outer join " + users_u1 + " on p.user_del = u1.nzp_user " +
                        " on p.nzp=" + finder.nzp_dom.ToString() + " and p.nzp_prm = n.nzp_prm ");
                    sql.Append(" Where n.prm_num=t.prm_num and n.prm_num=2 " +
                        sSuppFilter + filter);
#else
                    sql.Append("From " + prm_name_n + ", " + prm_table_t + ", outer (" + prm_2_p + ", outer " + users_u + ", outer " + users_u1 + ")");
                    sql.Append(" Where p.nzp=" + finder.nzp_dom.ToString() + " and p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=2 and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user " +
                        sSuppFilter + filter);

#endif
                }

                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
            }
            #endregion

            #region параметры поставщика из prm_11
            if (finder.prm_num == 11 && !Utils.GetParams(finder.prms, Constants.page_spisprm))
            {
#if PG
                /*sql = new StringBuilder(insert + values2 +
                    " Select n.nzp_prm,'" + finder.pref + "',n.name_prm,n.type_prm,p.val_prm,n.nzp_res,n.prm_num," + finder.nzp + ",t.name_tab,s.name_supp, p.is_actual, p.dat_s, p.dat_po, p.dat_when, p.nzp_user, trim(u.comment), p.dat_del, p.user_del, trim(u1.comment) " +
                    " From " + prm_name_n + "," + prm_table_t + ", " + supplier_s + "," + prm_11_p + "," + users_u + "," + users_u1 +
                    " where n.prm_num = 11 and s.nzp_supp = " + finder.nzp + filter + " and p.nzp = s.nzp_supp and p.nzp_prm = n.nzp_prm and n.prm_num = t.prm_num and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user " +
                    filterSupp);*/

                sql = new StringBuilder(insert + values2 +
                    " Select n.nzp_prm,'" + finder.pref + "',n.name_prm, n.is_day_uchet,n.type_prm,p.val_prm,n.nzp_res,n.prm_num," + finder.nzp + ",t.name_tab,s.name_supp, p.is_actual, p.dat_s, p.dat_po, p.dat_when, p.nzp_user, trim(u.comment), p.dat_del, p.user_del, trim(u1.comment), (case when p.nzp_key is null then 1 else 0 end )  " +
                    " From " + prm_table_t + "," + prm_name_n +
                    " inner join " + supplier_s + " on  s.nzp_supp = " + finder.nzp +
                    " left outer join " + prm_11_p +

                    " left outer join " + users_u + " on p.nzp_user = u.nzp_user " +
                    " left outer join " + users_u1 + " on p.user_del = u1.nzp_user " +
                    " on p.nzp = s.nzp_supp and p.nzp_prm = n.nzp_prm " +
                    " where n.prm_num = t.prm_num and  n.prm_num = 11 " + filter +
                    filterSupp);
#else
                sql = new StringBuilder(insert + values2 +
                    " Select n.nzp_prm,'" + finder.pref + "',n.name_prm, n.is_day_uchet,n.type_prm,p.val_prm,n.nzp_res,n.prm_num," + finder.nzp + ",t.name_tab,s.name_supp, p.is_actual, p.dat_s, p.dat_po, p.dat_when, p.nzp_user, trim(u.comment), p.dat_del, p.user_del, trim(u1.comment), (case when p.nzp_key is null then 1 else 0 end )  " +
                    " From " + prm_name_n + "," + prm_table_t + ", " + supplier_s + ", outer (" + prm_11_p + ", outer " + users_u + ", outer " + users_u1 + ")" +
                    " where n.prm_num = 11 and s.nzp_supp = " + finder.nzp + filter + " and p.nzp = s.nzp_supp and p.nzp_prm = n.nzp_prm and n.prm_num = t.prm_num and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user " +
                    filterSupp);

#endif
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
            }
            else if (finder.prm_num <= 0 || Utils.GetParams(finder.prms, Constants.page_spisprm))
            {
                if (finder.nzp_kvar > 0)
#if PG
                    /* sql = new StringBuilder(insert + values2 + select2 +
                                            " From " + prm_name_n + "," + prm_11_p + "," + prm_table_t + "," + supplier_s + "," + users_u + "," + users_u1 +
                                            " where p.nzp=s.nzp_supp and p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=11 and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user " +
                                            " and (0 < (select count(*) from " + kvar_k + ", " + tarif_f + " where k.nzp_kvar=f.nzp_kvar and f.nzp_supp=p.nzp " + sFndKvar + ")) " +
                                            sSuppFilter + filter);*/
                    sql = new StringBuilder(insert + values2 + select2 +
                                           " From " + prm_table_t + "," + supplier_s + "," + prm_name_n + "," +
                                           prm_11_p +
                                           " left outer join " + users_u + " on p.nzp_user = u.nzp_user " +
                                           " left outer join " + users_u1 + " on p.user_del = u1.nzp_user " +
                                           " where p.nzp=s.nzp_supp and p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=11 " +
                                           " and (0 < (select count(*) from " + kvar_k + ", " + tarif_f + " where k.nzp_kvar=f.nzp_kvar and f.nzp_supp=p.nzp " + sFndKvar + ")) " +
                                           sSuppFilter + filter);
#else
                    sql = new StringBuilder(insert + values2 + select2 +
                                            " From " + prm_name_n + "," + prm_11_p + "," + prm_table_t + "," + supplier_s + ", outer " + users_u + ", outer " + users_u1 +
                                            " where p.nzp=s.nzp_supp and p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=11 and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user " +
                                            " and (0 < (select count(*) from " + kvar_k + ", " + tarif_f + " where k.nzp_kvar=f.nzp_kvar and f.nzp_supp=p.nzp " + sFndKvar + ")) " +
                                            sSuppFilter + filter);

#endif
                else
                {
                    ExecSQL(conn_db, "drop table t_supp", false);
#if PG
                    ExecSQL(conn_db, "select distinct f.nzp_supp from " + kvar_k + ", " + tarif_f + " where k.nzp_kvar=f.nzp_kvar " + sFndDom + " into temp t_supp with no log", false);
#else
                    ExecSQL(conn_db, "select unique f.nzp_supp from " + kvar_k + ", " + tarif_f + " where k.nzp_kvar=f.nzp_kvar " + sFndDom + " into temp t_supp with no log", false);
#endif
#if PG
                    /*     sql = new StringBuilder(insert + values2 + select2 +
                                            " From " + prm_name_n + "," + prm_11_p + "," + prm_table_t + "," + supplier_s + "," + users_u + "," + users_u1 +
                                            " where p.nzp=s.nzp_supp and p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=11 and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user " +
                                            " and (0 < (select count(*) from t_supp f where f.nzp_supp=p.nzp)) " +
                                            sSuppFilter + filter);*/
                    sql = new StringBuilder(insert + values2 + select2 +
                                            " From " + prm_name_n + "," + prm_table_t + "," + supplier_s + "," + prm_11_p +
                                            " left outer join " + users_u + " on p.nzp_user = u.nzp_user " +
                                            " left outer join " + users_u1 + " on p.user_del = u1.nzp_user " +
                                            " where p.nzp=s.nzp_supp and p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=11 " +
                                            " and (0 < (select count(*) from t_supp f where f.nzp_supp=p.nzp)) " +
                                            sSuppFilter + filter);
#else
                    sql = new StringBuilder(insert + values2 + select2 +
                                            " From " + prm_name_n + "," + prm_11_p + "," + prm_table_t + "," + supplier_s + ", outer " + users_u + ", outer " + users_u1 +
                                            " where p.nzp=s.nzp_supp and p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=11 and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user " +
                                            " and (0 < (select count(*) from t_supp f where f.nzp_supp=p.nzp)) " +
                                            sSuppFilter + filter);
#endif
                }
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
            }
            #endregion

            #region параметры Управляющая организация из prm_7
            if (finder.prm_num == 7)
            {
#if PG
                //айрат

                /*  sql = new StringBuilder(insert + values2 +
                      " select n.nzp_prm, '"+finder.pref+"' , n.name_prm, n.type_prm, p.val_prm, n.nzp_res, n.prm_num, "+finder.nzp+" , t.name_tab, p.area, p.is_actual, p.dat_s, p.dat_po, p.dat_when, p.nzp_user, p.comment1, p.dat_del, p.user_del, p.comment2 "+
                      " From " +prm_table_t+" , " + prm_name_n+ " left  outer join (select p.nzp_prm, p.val_prm, a.area, p.is_actual, p.dat_s, p.dat_po, p.dat_when, p.nzp_user, trim(u.comment) as comment1, p.dat_del, p.user_del, trim(u1.comment) as comment2 " +
                      " from " +prm_7_p+ " left  outer join " + area_a +" on  p.nzp = a.nzp_area "+
                      " left  outer join " + users_u +" on  p.nzp_user = u.nzp_user "+
                      " left  outer join " + users_u1 +" on  p.user_del = u1.nzp_user "+
                      " where p.nzp = "+finder.nzp+" ) as p on p.nzp_prm = n.nzp_prm where n.prm_num = t.prm_num and n.prm_num = 7");*/

                sql = new StringBuilder(insert + values2 +
                    " Select n.nzp_prm, '" + finder.pref + "', n.name_prm, n.is_day_uchet, n.type_prm, p.val_prm, n.nzp_res, n.prm_num, " + finder.nzp + ", t.name_tab, a.area, p.is_actual, p.dat_s, p.dat_po, p.dat_when, p.nzp_user, trim(u.comment), p.dat_del, p.user_del, trim(u1.comment), (case when p.nzp_key is null then 1 else 0 end )  " +
                    " From " + prm_table_t + "," + prm_name_n +
                    " left outer join " + prm_7_p +
                    " left outer join " + area_a + " on p.nzp = a.nzp_area " +
                    " left outer join " + users_u + " on p.nzp_user = u.nzp_user " +
                    " left outer join " + users_u1 + " on p.user_del = u1.nzp_user " +
                    " on p.nzp = " + finder.nzp + " and p.nzp_prm = n.nzp_prm" +
                    " Where  n.prm_num = 7 and n.prm_num = t.prm_num");
#else
                sql = new StringBuilder(insert + values2 +
                    " Select n.nzp_prm, '" + finder.pref + "', n.name_prm, n.is_day_uchet, n.type_prm, p.val_prm, n.nzp_res, n.prm_num, " + finder.nzp + ", t.name_tab, a.area, p.is_actual, p.dat_s, p.dat_po, p.dat_when, p.nzp_user, trim(u.comment), p.dat_del, p.user_del, trim(u1.comment), (case when p.nzp_key is null then 1 else 0 end )  " +
                    " From " + prm_name_n + "," + prm_table_t + ", outer (" + prm_7_p + ", outer " + area_a + ", outer " + users_u + ", outer " + users_u1 + ")" +
                    " Where p.nzp = " + finder.nzp + " and n.prm_num = 7 and p.nzp = a.nzp_area and p.nzp_prm = n.nzp_prm and n.prm_num = t.prm_num" +
                    " and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user");

#endif
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
            }
            #endregion

            #region параметры участков из prm_8
            if (finder.prm_num == 8)
            {
#if PG
                /* sql = new StringBuilder(insert + values2 +
                                                 " Select n.nzp_prm, '" + finder.pref +
                                                 "', n.name_prm, n.type_prm, pp.val_prm, n.nzp_res, n.prm_num, "
                                                 + finder.nzp +
                                                 ", t.name_tab, pp.geu, pp.is_actual, pp.dat_s, pp.dat_po, pp.dat_when, pp.nzp_user, pp.comment1, pp.dat_del, pp.user_del, pp.comment2 " +

                                                 "From  " + prm_table_t + "," + prm_name_n + "   left outer join ( " +
                                                 " select  p.nzp, p.nzp_prm, p.val_prm, g.geu, p.is_actual, p.dat_s, p.dat_po, p.dat_when, p.nzp_user, trim(u.comment) as comment1, p.dat_del, p.user_del, trim(u1.comment) as comment2"
                                                 + " from   " + prm_8_p + " left outer join " + geu_g + "  on p.nzp = g.nzp_geu " +
                                                 " left outer join " + users_u + "  on p.nzp_user = u.nzp_user " +
                                                 "left outer join " + users_u1 + " on p.user_del = u1.nzp_user) " +
                                                 " as pp on pp.nzp = 602 and pp.nzp_prm = n.nzp_prm  where n.prm_num = 8 and n.prm_num = t.prm_num ");*/

                sql = new StringBuilder(insert + values2 +
                   " Select n.nzp_prm, '" + finder.pref + "', n.name_prm, n.is_day_uchet, n.type_prm, p.val_prm, n.nzp_res, n.prm_num, " + finder.nzp + ", t.name_tab, g.geu, p.is_actual, p.dat_s, p.dat_po, p.dat_when, p.nzp_user, trim(u.comment), p.dat_del, p.user_del, trim(u1.comment), (case when p.nzp_key is null then 1 else 0 end )  " +
                   " From " + prm_table_t + "," + prm_name_n +
                   " left outer join " + prm_8_p +
                   " left outer join " + geu_g + " on p.nzp = g.nzp_geu " +
                   " left outer join " + users_u + " on p.nzp_user = u.nzp_user " +
                   " left outer join " + users_u1 + " on p.user_del = u1.nzp_user " +
                   " on p.nzp = " + finder.nzp + " and p.nzp_prm = n.nzp_prm" +
                   " Where n.prm_num = 8 and n.prm_num = t.prm_num");
#else
                    sql = new StringBuilder(insert + values2 +
                    " Select n.nzp_prm, '" + finder.pref + "', n.name_prm, n.is_day_uchet, n.type_prm, p.val_prm, n.nzp_res, n.prm_num, " + finder.nzp + ", t.name_tab, g.geu, p.is_actual, p.dat_s, p.dat_po, p.dat_when, p.nzp_user, trim(u.comment), p.dat_del, p.user_del, trim(u1.comment), (case when p.nzp_key is null then 1 else 0 end )  " + 
                    " From " + prm_name_n + "," + prm_table_t + ", outer (" + prm_8_p + ", outer " + geu_g + ", outer " + users_u + ", outer " + users_u1 + ")" +
                    " Where p.nzp = " + finder.nzp + " and n.prm_num = 8 and p.nzp = g.nzp_geu and p.nzp_prm = n.nzp_prm and n.prm_num = t.prm_num" +
                    " and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user");

#endif
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
            }
            #endregion

            #region параметры Котрагента из prm_9
            if (finder.prm_num == 9)
            {
#if PG
                sql = new StringBuilder(insert + values2 +
                                        " Select n.nzp_prm, '" + finder.pref +
                                        "', n.name_prm, n.is_day_uchet, n.type_prm, p.val_prm, n.nzp_res, n.prm_num, " +
                                        finder.nzp +
                                        ", t.name_tab, pr.payer, p.is_actual, p.dat_s, p.dat_po, p.dat_when, p.nzp_user, trim(u.comment), p.dat_del, p.user_del, trim(u1.comment), (case when p.nzp_key is null then 1 else 0 end )  " +
                                        " From " + prm_table_t + "," + prm_name_n +
                                        " left outer join " + prm_9_p +
                                        " left outer join " + payer_pr + " on p.nzp = pr.nzp_payer " +
                                        " left outer join " + users_u + " on p.nzp_user = u.nzp_user " +
                                        " left outer join " + users_u1 + " on p.user_del = u1.nzp_user " +
                                        " on p.nzp = " + finder.nzp + " and p.nzp_prm = n.nzp_prm" +
                                        " Where  n.prm_num = 9 and n.prm_num = t.prm_num");
#else

#endif
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
            }
            #endregion

            #region параметры услуг из prm_12
            if (finder.prm_num <= 0 || finder.prm_num == 12 || Utils.GetParams(finder.prms, Constants.page_spisprm))
            {
                if (finder.nzp > 0 && finder.nzp_kvar < 1 && finder.nzp_dom < 1)
                {
#if PG
                    /*sql = new StringBuilder(insert + values2 +
                        " Select n.nzp_prm,'" + finder.pref + "',n.name_prm,n.type_prm,p.val_prm,n.nzp_res,n.prm_num," + finder.nzp + ",t.name_tab,s.service, p.is_actual, p.dat_s, p.dat_po, p.dat_when, p.nzp_user, trim(u.comment), p.dat_del, p.user_del, trim(u1.comment) " +
                        " From " + prm_table_t + "," + prm_name_n + 
                        " left outer join " + prm_12_p + " on (p.nzp_prm=n.nzp_prm and p.nzp = " + finder.nzp + ") " +
                        " left outer join " + services_s + " on (p.nzp=s.nzp_serv) "+
                        " left outer join " + users_u + " on (p.nzp_user = u.nzp_user)" + 
                        " left outer join " + users_u1 + " on (p.user_del = u1.nzp_user )" +
                        " where n.prm_num=12 and n.prm_num=t.prm_num");*/
                    sql = new StringBuilder(insert + values2 +
                        " Select n.nzp_prm,'" + finder.pref + "',n.name_prm, n.is_day_uchet,n.type_prm,p.val_prm,n.nzp_res,n.prm_num," + finder.nzp + ",t.name_tab,s.service, p.is_actual, p.dat_s, p.dat_po, p.dat_when, p.nzp_user, trim(u.comment), p.dat_del, p.user_del, trim(u1.comment), (case when p.nzp_key is null then 1 else 0 end )  " +
                        " From " + prm_table_t + "," + prm_name_n +
                        " left outer join " + prm_12_p +
                        " left outer join " + services_s + " on p.nzp=s.nzp_serv " +
                        " left outer join " + users_u + " on p.nzp_user = u.nzp_user " +
                        " left outer join " + users_u1 + " on p.user_del = u1.nzp_user " +
                        " on p.nzp = " + finder.nzp + " and p.nzp_prm=n.nzp_prm " +
                        " where n.prm_num=12 and n.prm_num=t.prm_num");
#else
                    sql = new StringBuilder(insert + values2 +
                        " Select n.nzp_prm,'" + finder.pref + "',n.name_prm, n.is_day_uchet,n.type_prm,p.val_prm,n.nzp_res,n.prm_num," + finder.nzp + ",t.name_tab,s.service, p.is_actual, p.dat_s, p.dat_po, p.dat_when, p.nzp_user, trim(u.comment), p.dat_del, p.user_del, trim(u1.comment), (case when p.nzp_key is null then 1 else 0 end )  " +
                        " From " + prm_name_n + "," + prm_table_t + ", outer (" + prm_12_p + ", outer " + services_s + ", outer " + users_u + ", outer " + users_u1 + ")" +
                        " where p.nzp = " + finder.nzp + " and n.prm_num=12 and p.nzp=s.nzp_serv and p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num" +
                        " and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user ");

#endif
                }
                else if (finder.nzp_kvar > 0)
                {
#if PG
                    /*  sql = new StringBuilder(insert + values2 + select3 +
                        " From " + prm_name_n + "," + prm_12_p + "," + prm_table_t + "," + services_s + "," + users_u + "," + users_u1 +
                        " where p.nzp=s.nzp_serv and p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=12 and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user " +
                        " and (0 < (select count(*) from " + kvar_k + ", " + tarif_f + " where k.nzp_kvar=f.nzp_kvar and f.nzp_serv=p.nzp " + sFndKvar + ")) " +
                        sSuppFilter + filter);*/
                    sql = new StringBuilder(insert + values2 + select3 +
                        " From " + prm_name_n + "," + prm_table_t + "," + services_s + "," + prm_12_p +
                        " left outer join " + users_u + " on p.nzp_user = u.nzp_user " +
                        " left outer join " + users_u1 + " on p.user_del = u1.nzp_user " +
                        " where p.nzp=s.nzp_serv and p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=12 " +
                        " and (0 < (select count(*) from " + kvar_k + ", " + tarif_f + " where k.nzp_kvar=f.nzp_kvar and f.nzp_serv=p.nzp " + sFndKvar + ")) " +
                        sSuppFilter + filter);
#else
                    sql = new StringBuilder(insert + values2 + select3 +
                        " From " + prm_name_n + "," + prm_12_p + "," + prm_table_t + "," + services_s + ", outer " + users_u + ", outer " + users_u1 +
                        " where p.nzp=s.nzp_serv and p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=12 and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user " +
                        " and (0 < (select count(*) from " + kvar_k + ", " + tarif_f + " where k.nzp_kvar=f.nzp_kvar and f.nzp_serv=p.nzp " + sFndKvar + ")) " +
                        sSuppFilter + filter);

#endif
                }
                else
                {
                    ExecSQL(conn_db, "drop table t_serv", false);
#if PG
                    ExecSQL(conn_db, "select distinct f.nzp_serv from " + kvar_k + ", " + tarif_f + " where k.nzp_kvar=f.nzp_kvar " + sFndDom + " into temp t_serv with no log", false);
#else
                    ExecSQL(conn_db, "select unique f.nzp_serv from " + kvar_k + ", " + tarif_f + " where k.nzp_kvar=f.nzp_kvar " + sFndDom + " into temp t_serv with no log", false);
#endif
#if PG
                    sql = new StringBuilder(insert + values2 + select3 +
                        " From " + prm_name_n + "," + prm_table_t + "," + services_s + "," + prm_12_p +
                        " left outer join " + users_u + " on p.nzp_user = u.nzp_user " +
                        " left outer join " + users_u1 + " on p.user_del = u1.nzp_user " +
                        " where p.nzp=s.nzp_serv and p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=12 " +
                        " and (0 < (select count(*) from t_serv f where f.nzp_serv=p.nzp)) " +
                        sSuppFilter + filter);
#else
                    sql = new StringBuilder(insert + values2 + select3 +
                        " From " + prm_name_n + "," + prm_12_p + "," + prm_table_t + "," + services_s + ", outer " + users_u + ", outer " + users_u1 +
                        " where p.nzp=s.nzp_serv and p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=12 and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user " +
                        " and (0 < (select count(*) from t_serv f where f.nzp_serv=p.nzp)) " +
                        sSuppFilter + filter);

#endif
                }
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
            }
            #endregion

            #region параметры на всю БД из prm_5
            if (finder.prm_num <= 0 || finder.prm_num == 5 || Utils.GetParams(finder.prms, Constants.page_spisprm))
            {
#if PG
                /* sql = new StringBuilder(insert + values1 + select1 +
                                        " From " + prm_name_n + "," + prm_5_p + "," + prm_table_t + "," + users_u + "," + users_u1 +
                                        " where p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=5 and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user " +
                                        sSuppFilter + filter);*/
                sql = new StringBuilder(insert + values1 + select1 +
                                       " From " + prm_name_n + "," + prm_table_t + "," + prm_5_p +
                                       " left outer join " + users_u + " on p.nzp_user = u.nzp_user " +
                                       " left outer join " + users_u1 + " on p.user_del = u1.nzp_user " +
                                       " where p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=5 " +
                                       sSuppFilter + filter);
#else
                sql = new StringBuilder(insert + values1 + select1 +
                                        " From " + prm_name_n + "," + prm_5_p + "," + prm_table_t + ", outer " + users_u + ", outer " + users_u1 +
                                        " where p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=5 and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user " +
                                        sSuppFilter + filter);
#endif
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
            }
            #endregion

            #region системные параметры из prm_10
            if (finder.prm_num <= 0 || finder.prm_num == 10 || Utils.GetParams(finder.prms, Constants.page_spisprm))
            {
#if PG
                /*  sql = new StringBuilder(insert + values4 + select4 +
                                        " From " + prm_name_n +
                                        " left outer join " + prm_10_p +
                                            " left outer join " + users_u + " on p.nzp_user = u.nzp_user" +
                                            " left outer join " + users_u1 + " on p.user_del = u1.nzp_user" +
                                        " on p.nzp_prm = n.nzp_prm" +
                                        " inner join " + prm_table_t + " on n.prm_num = t.prm_num" +
                                        " where n.prm_num=10 " + sSuppFilter + filter);*/
                sql = new StringBuilder(insert + values4 + select4 +
                                        " From " + prm_name_n +
                                        " left outer join " + prm_table_t + " on n.prm_num=t.prm_num " +
                                        " left outer join " + prm_10_p +
                                        " left outer join " + users_u + " on p.nzp_user = u.nzp_user " +
                                        " left outer join " + users_u1 + " on p.user_del = u1.nzp_user " +
                                        " on  p.nzp_prm=n.nzp_prm " +
                                        " where n.prm_num=10 " + sSuppFilter + filter);
#else
                sql = new StringBuilder(insert + values4 + select4 +
                                        " From " + prm_name_n + ", outer (" + prm_10_p + "," + prm_table_t + ", outer " + users_u + ", outer " + users_u1 + ") " +
                                        " where p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=10 and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user " + sSuppFilter + filter);

#endif
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
            }
            #endregion

            #region параметры ПУ из prm_17
            if (finder.prm_num == 17)
            {
#if PG
                /*sql = new StringBuilder(insert + values1 + select1 +
                    " From " + prm_name_n + "," + prm_17_p + "," + prm_table_t + "," + users_u + "," + users_u1 +
                    " where p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=17 and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user " +
                    sSuppFilter + filter);*/

                sql = new StringBuilder(insert + values1 + select1 +
                   " From " + prm_name_n +
                   " left outer join " + prm_table_t + " on n.prm_num=t.prm_num  " +
                   " left outer join " + prm_17_p +
                   " left outer join  " + users_u + " on p.nzp_user = u.nzp_user " +
                   " left outer join  " + users_u1 + " on p.user_del = u1.nzp_user " +
                   " on p.nzp_prm=n.nzp_prm " +
                   " where n.prm_num=17 " +
                   sSuppFilter + filter);
#else
                sql = new StringBuilder(insert + values1 + select1 +
                    " From " + prm_name_n + ", outer (" + prm_17_p + "," + prm_table_t + ", outer " + users_u + ", outer " + users_u1 + ")" +
                    " where p.nzp_prm=n.nzp_prm and n.prm_num=t.prm_num and n.prm_num=17 and p.nzp_user = u.nzp_user and p.user_del = u1.nzp_user " +
                    sSuppFilter + filter);

#endif
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
            }
            #endregion

            // добавить фиктивные записи для параметров из prm_1, prm_2, prm_7, prm_8, prm_9,prm_11, prm_12, prm_18, prm_19
            // у которых нет ни одного действующего значения, а есть только недействующие
            // чтобы была возможность увидеть их в списке и установить значения
            sql = new StringBuilder();

#if PG
            sql.Append(insert + " (nzp_prm,pref,name_prm,type_prm,nzp_res,prm_num,nzp,val_prm,val_prm_link,name_tab,name_link,not_hist)");
            // sql.Append(" select distinct on (nzp_prm) 0+oid::int,nzp_prm,pref,name_prm,type_prm,nzp_res,prm_num,nzp,'','',name_tab,name_link");
            sql.Append(" select distinct nzp_prm,pref,name_prm,type_prm,nzp_res,prm_num,nzp,'','',name_tab,name_link,1");
#else
            sql.Append(insert + " (nzp_key,nzp_prm,pref,name_prm,type_prm,nzp_res,prm_num,nzp,val_prm,val_prm_link,name_tab,name_link,not_hist)");
            sql.Append(" select unique 0,nzp_prm,pref,name_prm,type_prm,nzp_res,prm_num,nzp,'','',name_tab,name_link,1");
#endif
            sql.Append(" from " + tXX_spkvprm_full + " a");
            sql.Append(" where prm_num in (1,2,7,8,9 " + (finder.prm_num == 11 ? ",11" : "") + ",12,18,19)");
            sql.Append(" and not exists (select * from " + tXX_spkvprm_full + " b where b.nzp_prm = a.nzp_prm and b.nzp = a.nzp and b.pref = a.pref and (is_actual is null or is_actual <> 100))");

            ret = ExecSQL(conn_web, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }

            //Если включены редактируемые нормативы
            if (Points.isNewNorms)
            {
                sql = new StringBuilder(
                " UPDATE " + tXX_spkvprm_full + " set type_prm = 'norm' " +
                " WHERE type_prm = 'sprav' AND EXISTS " +
                " (SELECT 1 FROM " + Points.Pref + sKernelAliasRest + "norm_prm_serv nps" +
                "  WHERE nps.nzp_prm = " + tXX_spkvprm_full + ".nzp_prm)");

                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
                ret = SetNormValPrmLink(conn_db, finder, tXX_spkvprm_full);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
            }

            // установка справочных значений
            sql = new StringBuilder(
                " update " + tXX_spkvprm_full + " set val_prm_link = " +
                " (SELECT max(r.name_y) FROM " + finder.pref + "_kernel" + DBManager.tableDelimiter + "res_y r" +
                "   WHERE r.nzp_res=" + tXX_spkvprm_full + ".nzp_res" +
#if PG
 " AND r.nzp_y = NULLIF(" + tXX_spkvprm_full + ".val_prm, '')::integer)" +
#else
                " and r.nzp_y=(" + tXX_spkvprm_full + ".val_prm+0)) " +
                
#endif
 " where type_prm = 'sprav' ");

            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }


            // установка булевых значений
            sql = new StringBuilder(" update " + tXX_spkvprm_full +
                                    " set val_prm_link = 'да' where type_prm='bool' and val_prm='1' ");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }

            // установка значений типа "table"
            IDataReader reader;
#if PG
            sql = new StringBuilder("select distinct a.nzp_res, b.table_name, b.db_name, b.key_field, b.display_field" +
                            " from " + tXX_spkvprm_full + " a, " + Points.Pref + "_kernel.prm_table_descr b" +
                            " where a.type_prm = 'table' and a.nzp_res = b.nzp_table and a.nzp_res is not null");
#else
            sql = new StringBuilder("select unique a.nzp_res, b.table_name, b.db_name, b.key_field, b.display_field" +
                            " from " + tXX_spkvprm_full + " a, " + Points.Pref + "_kernel:prm_table_descr b" +
                            " where a.type_prm = 'table' and a.nzp_res = b.nzp_table and a.nzp_res is not null");
#endif
            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }
            string tableName, dbName, keyField, displayField;
            int nzpRes;
            while (reader.Read())
            {
                nzpRes = Convert.ToInt32(reader["nzp_res"]);
                tableName = Convert.ToString(reader["table_name"]).Trim();
                dbName = Convert.ToString(reader["db_name"]).Trim();
                keyField = Convert.ToString(reader["key_field"]).Trim();
                displayField = Convert.ToString(reader["display_field"]).Trim();
                sql = new StringBuilder("update " + tXX_spkvprm_full + " set val_prm_link = " +
                    " (select b." + displayField + " from " + finder.pref + dbName + tableDelimiter + tableName + " b where " + keyField + " = ('0'||" + tXX_spkvprm_full + ".val_prm)::integer)" +
                    " where type_prm = 'table' and nzp_res = " + nzpRes);
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    CloseReader(ref reader);
                    conn_db.Close();
                    conn_web.Close();
                    return;
                }
            }
            CloseReader(ref reader);

            ret = ExecSQL(conn_web, " update " + tXX_spkvprm_full + " set val_prm='' where val_prm is null ", true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }

            ret = ExecSQL(conn_web, " update " + tXX_spkvprm_full + " set val_prm_link ='' where val_prm_link is null ", true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }

            ret = ExecSQL(conn_web, " update " + tXX_spkvprm_full + " set name_link ='' where name_link is null ", true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return;
            }

            conn_db.Close(); //закрыть соединение с основной базой

            if (key)
            {
#if PG
                ret = ExecSQL(conn_db, "set search_path to 'public'", true);
#else
#endif

                //далее работаем с кешем
                //создаем индексы на tXX_spkvprm
                string ix = "ix" + Convert.ToString(finder.nzp_user) + "_spkvprm";

                ret = ExecSQL(conn_web, " Create unique index " + ix + "_1 on " + tXX_spkvprm + " (nzp_key) ", true);
                if (ret.result) ret = ExecSQL(conn_web, " Create index " + ix + "_2 on " + tXX_spkvprm + " (nzp_prm,prm_num) ", true);
                if (ret.result) ret = ExecSQL(conn_web, " Create index " + ix + "_3 on " + tXX_spkvprm + " (name_prm) ", true);
#if PG
                if (ret.result) ret = ExecSQL(conn_web, " analyze  " + tXX_spkvprm, true);
#else
                if (ret.result) ret = ExecSQL(conn_web, " Update statistics for table  " + tXX_spkvprm, true);
#endif
            }

            conn_web.Close();

            return;
        }

        private Returns SetNormValPrmLink(IDbConnection conn_db, Prm finder, string tXX_spkvprm_full)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                string sql;

                sql = " SELECT * FROM " + Points.Pref + sKernelAliasRest + "norm_prm_serv";
                DataTable norm_serv_prm_dt = DBManager.ExecSQLToTable(conn_db, sql);

                foreach (DataRow row in norm_serv_prm_dt.Rows)
                {
                    #region подтягиваем значения
                    //получаем id норматива
                    sql =
                        " SELECT max(nt.id)" +
                        " FROM " + Points.Pref + sKernelAliasRest + "norm_types nt, " +
                        Points.Pref + sKernelAliasRest + "norm_banks nb," +
                        tXX_spkvprm_full + " t" +
                        " WHERE nb.norm_type_id = nt.id AND nt.is_finished = true" +
                        " AND nt.nzp_serv = " + row["nzp_serv"] + " AND nt.nzp_measure  = " + row["nzp_measure"] +
                        " AND t.type_prm = 'norm' AND t.nzp_prm = " + row["nzp_prm"] + "" +
                        " AND nb.nzp_wp = " +
                        "     (SELECT nzp_wp FROM " + Points.Pref + sKernelAliasRest + "s_point " +
                        "     WHERE bd_kernel = '" + finder.pref + "')" +
                        " AND nt.date_from <= t.dat_s" +
                        //"       AND nt.date_to >=" + tXX_spkvprm_full + ".dat_po" + //задача #147339
                        " AND EXISTS" +
                        " (SELECT 1 FROM " + Points.Pref + sKernelAliasRest + "norm_types_sign s" +
                        " WHERE s.norm_type_id = nt.id AND (nzp_prm_ls = t.nzp_prm or (" + sNvlWord + "(nzp_prm_ls,-1) < 1 and nzp_prm_house = t.nzp_prm)))";
                    object norm_type_id_obj = DBManager.ExecScalar(conn_db, sql, out ret, true);
                    int norm_type_id = 0;
                    if ((ret.result && norm_type_id_obj != null &&
                         Int32.TryParse(norm_type_id_obj.ToString(), out norm_type_id)))
                    {
                        sql =
                            " UPDATE " + tXX_spkvprm_full +
                            " SET val_prm_link = " +
                            " (SELECT DISTINCT " +
                            " CASE WHEN (length(i.name_prm_val) + length(nts.name_type_norm)) < 210 THEN" +
                            " i.name_prm_val||' ('||nts.name_type_norm||' '" +
                            "||(nts.date_from " + DBManager.sConvToDate + ")||' - '" +
                            "||(nts.date_to" + DBManager.sConvToDate + ")||')' " +
                            " ELSE i.name_prm_val END " +
                            " FROM " + Points.Pref + sKernelAliasRest + "influence_params i," +
                            Points.Pref + sKernelAliasRest + "norm_types nts," +
                            Points.Pref + sKernelAliasRest + "norm_tables nt" +
                            " WHERE i.nzp_prm=" + tXX_spkvprm_full + ".nzp_prm " +
                            " AND nt.id = i.norm_tables_id AND nt.norm_type_id = nts.id" +
                            " AND nt.norm_type_id = " + norm_type_id + "" +
#if PG
                                " AND i.param_value1 = NULLIF(" + tXX_spkvprm_full + ".val_prm, '')::integer)" +
#else
                " and r.param_value1=(" + tXX_spkvprm_full + ".val_prm+0)) " +
                
#endif
                                " WHERE type_prm = 'norm' AND " + tXX_spkvprm_full + ".nzp_prm = " + row["nzp_prm"] +
                            " ";


                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result)
                        {
                            return ret;
                        }
                    }

                    #endregion

                    //В нормативе может быть одновременно на один параметр выбран и домовой, и лицевой параметр в одной строке. 
                    //В этом случае приоритет отдается параметру ЛС, и все записывается на параметр ЛС.
                    //Сейчас мы это учтем и вытащим для домового параметра по параметру ЛС

                    #region Вытаскиваем норматив на домовой параметр, у которого есть в паре лицевой параметр на этом нормативе
                    //выбираем соответствующие параметры ЛС
                    
                    sql =
                        " SELECT max(nt.id) as id, nts.nzp_prm_house " +
                        " FROM " + Points.Pref + sKernelAliasRest + "norm_types nt, " +
                        Points.Pref + sKernelAliasRest + "norm_types_sign nts, " +
                        Points.Pref + sKernelAliasRest + "norm_banks nb, " +
                        tXX_spkvprm_full + " t" +
                        " WHERE nb.norm_type_id = nt.id AND nt.is_finished = true" +
                        " AND nt.nzp_serv = " + row["nzp_serv"] + " AND nt.nzp_measure  = " + row["nzp_measure"] + 
                        " AND nts.norm_type_id = nt.id" +
                        " AND nts.nzp_prm_house = t.nzp_prm AND " + sNvlWord + "(nzp_prm_ls, 0) > 0  " +
                        " AND t.type_prm = 'norm' AND t.nzp_prm = " + row["nzp_prm"] + "" +
                        " AND nb.nzp_wp = " +
                        "     (SELECT nzp_wp FROM " + Points.Pref + sKernelAliasRest + "s_point " +
                        "     WHERE bd_kernel = '" + finder.pref + "') " +
                        " AND nt.date_from <= t.dat_s" +
                        //"       AND nt.date_to >=" + tXX_spkvprm_full + ".dat_po" + //задача #147339
                        " GROUP BY nzp_prm_house";

                    DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                    foreach (DataRow r in dt.Rows)
                    {
                        sql =
                           " UPDATE " + tXX_spkvprm_full +
                           " SET val_prm_link = " +
                           " (SELECT DISTINCT " +
                           " CASE WHEN (length(i.name_prm_val) + length(ntypes.name_type_norm)) < 210 THEN" +
                           " i.name_prm_val||' ('||ntypes.name_type_norm||' '" +
                           "||(ntypes.date_from " + DBManager.sConvToDate + ")||' - '" +
                            "||(ntypes.date_to" + DBManager.sConvToDate + ")||')' " +
                           " ELSE i.name_prm_val END " +
                           " FROM " + Points.Pref + sKernelAliasRest + "influence_params i," +
                          Points.Pref + sKernelAliasRest + "norm_types_sign nts, " +
                           Points.Pref + sKernelAliasRest + "norm_types ntypes," +
                           Points.Pref + sKernelAliasRest + "norm_tables nt" +
                           " WHERE i.nzp_prm = nts.nzp_prm_ls" +
                           " AND nts.nzp_prm_house = " + tXX_spkvprm_full + ".nzp_prm " +
                           " AND nt.id = i.norm_tables_id AND nt.norm_type_id = ntypes.id" +
                           " AND nt.norm_type_id = " + r["id"] + "" +
#if PG
 " AND i.param_value1 = NULLIF(" + tXX_spkvprm_full + ".val_prm, '')::integer)" +
#else
                " and r.param_value1=(" + tXX_spkvprm_full + ".val_prm+0)) " +
                
#endif
 " WHERE type_prm = 'norm' AND " + tXX_spkvprm_full + ".nzp_prm = " + r["nzp_prm_house"] +
                           " ";


                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result)
                        {
                            return ret;
                        }

                        ////-----------
                        //sql =
                        //   " SELECT DISTINCT param_value1, name_prm_val " +
                        //   " FROM " + Points.Pref + sKernelAliasRest + "influence_params" +
                        //   " WHERE nzp_prm =" + r["nzp_prm_ls"] +
                        //   " AND norm_tables_id in" +
                        //   " (SELECT id FROM " + Points.Pref + sKernelAliasRest + "norm_tables" +
                        //   " WHERE norm_type_id = " + finder.nzp + " ) " +
                        //   " ORDER BY param_value1";
                        //dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                        //foreach (DataRow rp in dt.Rows)
                        //{
                        //    PrmTypes pt = new PrmTypes();
                        //    pt.id = Convert.ToInt32(rp["param_value1"]);
                        //    pt.type_name = rp["name_prm_val"].ToString();
                        //    list.Add(pt);
                        //}
                    }

                    #endregion
                }


                // установка справочных значений по нормативу, если нет соответствующего норматива
                sql =
                    " update " + tXX_spkvprm_full + " set val_prm_link = " +
                    " (SELECT max(r.name_y) FROM " + finder.pref + "_kernel" + DBManager.tableDelimiter + "res_y r" +
                    "   WHERE r.nzp_res=" + tXX_spkvprm_full + ".nzp_res" +
#if PG
 " AND r.nzp_y = NULLIF(" + tXX_spkvprm_full + ".val_prm, '')::integer)" +
#else
                " and r.nzp_y=(" + tXX_spkvprm_full + ".val_prm+0)) " +
                
#endif
 " where type_prm = 'norm' AND " + sNvlWord + "(val_prm_link,'') = ''";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return ret;
                }

            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка SetNormValPrmLink() " + "\n" + ex.Message + " " + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
            }
            return ret;
        }

        public Returns SavePrmName(Prm finder)
        {
            if (finder.nzp_user <= 0) return new Returns(false, "Не задан пользователь");
            if (finder.nzp_prm > 0 && finder.nzp_prm < 100000) return new Returns(false, "Системный параметр редактировать нельзя", -1);

            StringBuilder sql = new StringBuilder();

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            DbTables tables = new DbTables(conn_db);

            if (finder.nzp_prm <= 0) //добавление
            {
                //определить nzp_prm
                sql.Remove(0, sql.Length);
                sql.Append("select max(nzp_prm) from " + tables.prm_name + " where nzp_prm > 100000");
                object count = ExecScalar(conn_db, sql.ToString(), out ret, true);
                try { finder.nzp_prm = Convert.ToInt32(count); }
                catch (Exception e)
                {
                    ret = new Returns(false, "Ошибка при определении идентификатора параметра: " + (Constants.Debug ? e.Message : ""));
                    MonitorLog.WriteLog("Ошибка SavePrmName() " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                    conn_db.Close();
                    return ret;
                }
                if (finder.nzp_prm <= 100000) finder.nzp_prm = 100001;
                else finder.nzp_prm++;
            }

            List<string> prefs = new List<string>();
            foreach (_Point p in Points.PointList) prefs.Add(p.pref.Trim());
            if (!prefs.Contains(Points.Pref.Trim())) prefs.Insert(0, Points.Pref.Trim());

            foreach (string pref in prefs)
            {
                finder.pref = pref;
                ret = SavePrmName(conn_db, finder);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
            }

            conn_db.Close();
            ret.tag = finder.nzp_prm;
            return ret;
        }

        private Returns SavePrmName(IDbConnection conn_db, Prm finder)
        {
            if (finder.nzp_prm <= 0) return new Returns(false, "Не задан параметр", -1);
            if (finder.pref.Trim() == "") return new Returns(false, "Не задан банк данных", -1);

            string prm_name = finder.pref.Trim() + "_kernel" + tableDelimiter + "prm_name";
            StringBuilder sql = new StringBuilder();
            sql.Append("select count(*) from " + prm_name + " where nzp_prm = " + finder.nzp_prm);
            int count = 0;
            Returns ret;

            object obj = ExecScalar(conn_db, sql.ToString(), out ret, true);
            try { count = Convert.ToInt32(obj); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка SavePrmName() " + (Constants.Viewerror ? "\n" + e.Message : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }

            if (count > 0) //редактирование
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update " + prm_name + " set name_prm = '" + finder.name_prm.Trim().Replace("\"", "'") + "', ");
                sql.Append(" old_field = " + finder.old_field);
                sql.Append(" where nzp_prm = " + finder.nzp_prm);
            }
            else //добавление
            {
                sql.Remove(0, sql.Length);
                sql.Append(" insert into " + prm_name + "(nzp_prm,name_prm,old_field,type_prm,prm_num,low_,high_,digits_,prm_type_id, is_day_uchet)");
                sql.Append(" values (" + finder.nzp_prm + ",'" + finder.name_prm.Trim().Replace("\"", "'") + "' ,'");
                sql.Append(finder.old_field + "','" + finder.type_prm + "'," + finder.prm_num + ",");
                sql.Append(finder.low_ + "," + finder.high_ + "," + finder.digits_ + "," + finder.prm_type_id + "," + finder.is_day_uchet + ")");
            }
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return ret;

            if (finder.pref == Points.Pref)
            {
                string s_reg_prm = finder.pref.Trim() + "_kernel" + tableDelimiter + "s_reg_prm";
                sql.Remove(0, sql.Length);
                sql.Append(" delete from " + s_reg_prm);
                sql.Append(" where nzp_prm = " + finder.nzp_prm + " and nzp_reg = " + finder.nzp_reg);
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result) return ret;
                if (finder.is_show == 1)
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" insert into " + s_reg_prm + " (nzp_reg, nzp_prm, numer, is_show)");
                    sql.Append(" values (" + finder.nzp_reg + ", " + finder.nzp_prm + ",");
                    sql.Append(" (select " + sNvlWord + "(max(numer),0)+1 from " + s_reg_prm + " where nzp_reg = " + finder.nzp_prm + " and ");
                    sql.Append(" nzp_reg = " + finder.nzp_reg + "),1)");
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result) return ret;
                }
            }

            return ret;
        }

        private bool PrmNameLinksExist(IDbConnection conn_db, Prm finder, string pref, out Returns ret)
        {
            MyDataReader reader = null, reader2 = null;
            int count = 0;

            StringBuilder sql = new StringBuilder("select dbname from " + pref + "_kernel" + tableDelimiter + "s_baselist");
            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result) return false;
            while (reader.Read())
            {
                string dbname = "";
                if (reader["dbname"] != DBNull.Value) dbname = Convert.ToString(reader["dbname"]).Trim();

                if (dbname == "") continue;
                sql.Remove(0, sql.Length);
#if PG
                sql.Append("Select table_name as tabname From information_schema.columns where lower(trim(column_name)) = 'nzp_prm' and lower(table_schema)='" + dbname + "'");
#else
                if (!TempTableInWebCashe(conn_db, dbname + tableDelimiter + "syscolumns")) continue;                
                sql.Append("select tabname from "+dbname+tableDelimiter+"syscolumns c, "+
                    dbname+ tableDelimiter + "systables t where lower(trim(c.colname)) = 'nzp_prm' and c.tabid = t.tabid");
#endif
                ExecRead(conn_db, out reader2, sql.ToString(), true);
                if (!ret.result) continue;

                while (reader2.Read())
                {
                    string tabname = "";
                    if (reader2["tabname"] != DBNull.Value) tabname = Convert.ToString(reader2["tabname"]).Trim();

                    if (tabname.ToLower() == "prm_name" || tabname.ToLower() == "s_reg_prm") continue;
                    if (!TempTableInWebCashe(conn_db, dbname + tableDelimiter + tabname)) continue;
                    sql.Remove(0, sql.Length);
                    sql.Append("select count (*) from " + dbname + tableDelimiter + tabname + " where nzp_prm = " + finder.nzp_prm);
                    object obj = ExecScalar(conn_db, sql.ToString(), out ret, true);

                    try { count = Convert.ToInt32(obj); }
                    catch (Exception e)
                    {
                        ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                        MonitorLog.WriteLog("Ошибка PrmNameLinksExist() " + (Constants.Viewerror ? "\n" + e.Message : ""),
                            MonitorLog.typelog.Error, 20, 201, true);
                        if (reader2 != null) reader2.Close();
                        if (reader != null) reader.Close();
                        return false;
                    }

                    if (count > 0)
                        break;
                }
                if (count > 0)
                    break;
            }

            if (reader2 != null) reader2.Close();
            if (reader != null) reader.Close();

            if (count > 0) return true;
            else return false;
        }

        public Returns DeletePrmName(Prm finder)
        {
            if (finder.nzp_prm <= 0) return new Returns(false, "Не задан параметр", -1);

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            List<string> prefs = new List<string>();
            bool res = false;
            foreach (_Point p in Points.PointList) prefs.Add(p.pref.Trim());
            if (!prefs.Contains(Points.Pref.Trim())) prefs.Insert(0, Points.Pref.Trim());
            foreach (string pref in prefs)
            {
                res = PrmNameLinksExist(conn_db, finder, pref, out ret);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
                if (res) break;
            }


            if (!res)
            {
                string s_reg_prm = Points.Pref.Trim() + "_kernel" + tableDelimiter + "s_reg_prm";

                StringBuilder sql = new StringBuilder(" delete from " + s_reg_prm);
                sql.Append(" where nzp_prm = " + finder.nzp_prm);
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }

                foreach (string pref in prefs)
                {
                    finder.pref = pref;
                    ret = DeletePrmName(conn_db, finder);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }
                }
            }
            else
            {
                ret = new Returns(false, "Удалить параметр нельзя, т.к. на него имеются ссылки в других таблицах", -1);
            }
            conn_db.Close();
            return ret;
        }

        private Returns DeletePrmName(IDbConnection conn_db, Prm finder)
        {
            if (finder.nzp_prm <= 0) return new Returns(false, "Не задан параметр", -1);
            if (finder.pref.Trim() == "") return new Returns(false, "Не задан банк данных", -1);

            string prm_name = finder.pref.Trim() + "_kernel" + tableDelimiter + "prm_name";
            StringBuilder sql = new StringBuilder();
            sql.Append("delete from " + prm_name + " where nzp_prm = " + finder.nzp_prm);
            Returns ret = ExecSQL(conn_db, sql.ToString(), true);

            return ret;
        }

        /// <summary> Получает информацию об одном параметре
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public Param FindPrmInfo(Prm finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (finder.nzp_user < 1 ||
                (finder.nzp_key <= 0 && finder.nzp_prm <= 0))
            {
                ret.result = false;
                ret.text = "Не определены входные параметры";
                return null;
            }
            /*if (finder.nzp_key <= 0 && finder.pref.Trim() == "")
            {
                ret.result = false;
                ret.text = "Не задан префикс базы данных";
                return null;
            }*/
            if (finder.pref.Trim() == "") finder.pref = Points.Pref;

            Param prm = new Param();
            IDataReader reader;//, reader2;
            string sql;

            if (finder.nzp_key > 0)
            {
                #region соединение с бд Webdata
                IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result) return null;

#if PG
                ret = ExecSQL(conn_web, "set search_path to 'public'", false);
#endif

                #endregion

                string tXX_spkvprm = "t" + Convert.ToString(finder.nzp_user) + "_spkvprm";
#if PG
                string tXX_spkvprm_full = conn_web.Database + "." + tXX_spkvprm;
#else
                string tXX_spkvprm_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spkvprm;

#endif
                sql = "select nzp_prm, nzp_res, pref, nzp from " + tXX_spkvprm + " where nzp_key = " + finder.nzp_key;
                if (!ExecRead(conn_web, out reader, sql, true).result)
                {
                    conn_web.Close();
                    return null;
                }
                if (reader.Read())
                {
                    try
                    {
                        if (reader["nzp_prm"] != DBNull.Value) prm.nzp_prm = (int)reader["nzp_prm"];
                        if (reader["pref"] != DBNull.Value) prm.pref = (string)reader["pref"];
                    }
                    catch (Exception ex)
                    {
                        reader.Close();
                        conn_web.Close();
                        ret.result = false;
                        ret.text = ex.Message;
                        MonitorLog.WriteLog("Ошибка получения информации о параметре", MonitorLog.typelog.Error, 20, 201, true);
                        return null;
                    }
                }
                reader.Close();
                conn_web.Close();
            }
            else
            {
                prm.nzp_prm = finder.nzp_prm;
                prm.pref = finder.pref;
            }

            if (prm.pref.Trim() == "" || prm.nzp_prm < 1)
            {
                ret.result = false;
                ret.text = "Параметр не найден";
                return null;
            }

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            if (Points.isNewNorms)
            {
                sql = " SELECT p.name_prm, " +
                      " (CASE WHEN p.type_prm = 'sprav' AND coalesce(nps.nzp_prm, 0) > 0 THEN 'norm' else p.type_prm END) as type_prm," +
                      " p.nzp_res, p.prm_num, p.low_, p.is_day_uchet, p.high_, p.digits_" +
                      " FROM " + Points.Pref + sKernelAliasRest + "prm_name p " +
                      " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest + "norm_prm_serv nps ON nps.nzp_prm = p.nzp_prm" +
                      " WHERE p.nzp_prm = " + prm.nzp_prm;
            }
            else
            {
                sql = 
                    " SELECT name_prm, type_prm, nzp_res, prm_num, low_, is_day_uchet, high_, digits_" +
                    " FROM " + Points.Pref + sKernelAliasRest + "prm_name " +
                    " WHERE nzp_prm = " + prm.nzp_prm;
            }

            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                return null;
            }

            if (reader.Read())
            {
                try
                {
                    if (reader["name_prm"] != DBNull.Value) prm.name_prm = ((string)reader["name_prm"]).Trim();
                    if (reader["nzp_res"] != DBNull.Value) prm.nzp_res = (int)reader["nzp_res"];
                    if (reader["type_prm"] != DBNull.Value) prm.type_prm = (string)reader["type_prm"];
                    if (reader["prm_num"] != DBNull.Value) prm.prm_num = (int)reader["prm_num"];
                    if (reader["low_"] != DBNull.Value) prm.low_ = Convert.ToDecimal(reader["low_"]);
                    if (reader["high_"] != DBNull.Value) prm.high_ = Convert.ToDecimal(reader["high_"]);
                    if (reader["digits_"] != DBNull.Value) prm.digits_ = (int)reader["digits_"];
                    if (reader["is_day_uchet"] != DBNull.Value) prm.is_day_uchet = (int)reader["is_day_uchet"];

                    #region если параметр справочник
                    if (prm.nzp_res > 0)
                    {
                        if (prm.type_prm == "norm")
                        {
                            prm.norm_sprav_values = LoadSpravValueFromNormativ(conn_db, prm, out ret);
                        }
                        else
                        {
                            prm.values = LoadSpravValueForPrm(conn_db, prm, out ret);
                        }


                        if (!ret.result)
                        {
                            reader.Close();
                            conn_db.Close();
                            ret.result = false;
                            return null;
                        }
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    reader.Close();
                    conn_db.Close();
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка получения информации о параметре", MonitorLog.typelog.Error, 20, 201, true);
                    return null;
                }
            }
            reader.Close();

            //определить показывать ли банк данных s_reg_prm, nzp_reg = 5
            sql = "select count(*) from " + prm.pref + "_kernel" + tableDelimiter + "prm_name p, " +
                  Points.Pref + "_kernel" + tableDelimiter + "s_reg_prm r " +
                  " where p.nzp_prm = " + prm.nzp_prm + " and p.nzp_prm = r.nzp_prm " +
                  " and r.nzp_reg = " + SRegPrm.ShowPoint.GetHashCode();
            object count = ExecScalar(conn_db, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка FindPrmInfo() " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                recordsTotalCount = 0;
            }
            if (recordsTotalCount > 0) prm.show_point = 1;
            else prm.show_point = 0;
            conn_db.Close();

            return prm;
        }

        private List<PrmTypes> LoadSpravValueFromNormativ(IDbConnection conn_db, Param prm, out Returns ret, DateTime dateBetweenNormPeriod = new DateTime())
        {
            ret = Utils.InitReturns();
            List<PrmTypes> list = new List<PrmTypes>();
            if (prm.pref != "")
            {
                try
                {
                    string datWhere = dateBetweenNormPeriod.Year > 1
                        ? " AND " + Utils.EStrNull(dateBetweenNormPeriod.ToShortDateString()) + " between nt.date_from and nt.date_to" : "";
                    string sql =
                        " SELECT nt.id, nt.name_type_norm, nt.date_from, nt.date_to " +
                        " FROM " + Points.Pref + sKernelAliasRest + "norm_types nt, " +
                        Points.Pref + sKernelAliasRest + "norm_banks nb" +
                        " WHERE nb.norm_type_id = nt.id AND nb.nzp_wp = " +
                        "   (SELECT nzp_wp FROM " + Points.Pref + sKernelAliasRest + "s_point" +
                        "   WHERE lower(trim(bd_kernel)) = '" + prm.pref.Trim().ToLower() + "') " +
                        " AND EXISTS" +
                        "   (SELECT ip.id FROM " + Points.Pref + sKernelAliasRest + "influence_params ip" +
                        "   WHERE ip.nzp_prm = " + prm.nzp_prm + " AND ip.norm_tables_id in" +
                        "       (SELECT nts.id FROM " + Points.Pref + sKernelAliasRest + "norm_tables nts" +
                        "       WHERE nts.norm_type_id = nt.id ))" + datWhere +
                        " ORDER BY nt.date_from DESC ";
                    DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                    foreach (DataRow r in dt.Rows)
                    {
                        PrmTypes np = new PrmTypes();
                        np.type_name = r["name_type_norm"] + " (" +
                            Convert.ToDateTime(r["date_from"]).ToShortDateString() + " - " +
                            Convert.ToDateTime(r["date_to"]).ToShortDateString() + ")";
                        np.id = Convert.ToInt32(r["id"]);
                        list.Add(np);
                    }  

                    //В нормативе может быть одновременно на один параметр выбран и домовой, и лицевой параметр в одной строке. 
                    //В этом случае приоритет отдается параметру ЛС, и все записывается на параметр ЛС.
                    //Сейчас мы это учтем и вытащим нормативы для домового параметра по параметру ЛС

                    #region Вытаскиваем норматив на домовой параметр, у которого есть в паре лицевой параметр на этом нормативе
                    //выбираем соответствующие параметры ЛС
                    sql =
                        " SELECT nt.id, nts.nzp_prm_ls " +
                        " FROM " + Points.Pref + sKernelAliasRest + "norm_types nt, " +
                        Points.Pref + sKernelAliasRest + "norm_types_sign nts, " +
                        Points.Pref + sKernelAliasRest + "norm_banks nb" +
                        " WHERE nb.norm_type_id = nt.id AND nts.norm_type_id = nt.id" +
                        " AND nts.nzp_prm_house = " + prm.nzp_prm + " AND " + sNvlWord + "(nzp_prm_ls, 0) > 0  " +
                        " AND nb.nzp_wp = " +
                        "   (SELECT nzp_wp FROM " + Points.Pref + sKernelAliasRest + "s_point" +
                        "   WHERE lower(trim(bd_kernel)) = '" + prm.pref.Trim().ToLower() + "') " + datWhere +
                        " ORDER BY nt.date_from DESC ";
                    dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                    foreach (DataRow r in dt.Rows)
                    {
                        sql =
                           " SELECT nt.id, nt.name_type_norm, nt.date_from, nt.date_to " +
                           " FROM " + Points.Pref + sKernelAliasRest + "norm_types nt " +
                           " WHERE nt.id = " + r["id"] + 
                           " AND EXISTS" +
                           "   (SELECT ip.id FROM " + Points.Pref + sKernelAliasRest + "influence_params ip" +
                           "   WHERE ip.nzp_prm = " + r["nzp_prm_ls"] + " AND ip.norm_tables_id in" +
                           "       (SELECT nts.id FROM " + Points.Pref + sKernelAliasRest + "norm_tables nts" +
                           "       WHERE nts.norm_type_id = nt.id ))" + datWhere +
                           " ORDER BY nt.date_from DESC ";
                        dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                        foreach (DataRow r1 in dt.Rows)
                        {
                            PrmTypes np = new PrmTypes();
                            np.type_name = r1["name_type_norm"] + " (" +
                                Convert.ToDateTime(r1["date_from"]).ToShortDateString() + " - " +
                                Convert.ToDateTime(r1["date_to"]).ToShortDateString() + ")";
                            np.id = Convert.ToInt32(r1["id"]);
                            list.Add(np);
                        }
                    }

                    #endregion
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка получения значений параметра: " + ex.Message + " " + ex.StackTrace,
                        MonitorLog.typelog.Error, true);
                    ret.result = false;
                    ret.text = "Ошибка получения значений параметра.";
                }
            }
            else
            {
                ret.result = false;
                ret.text = "Не определен банк для данного ЛС";
            }
            return list;
        }


        public List<Res_y> LoadSpravValueForPrm(Param finder, out Returns ret)
        {
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            return LoadSpravValueForPrm(conn_db, finder, out ret);
        }

        private List<Res_y> LoadSpravValueForPrm(IDbConnection conn_db, Param prm, out Returns ret)
        {
            if (prm.pref == "") prm.pref = Points.Pref;
            List<Res_y> list = new List<Res_y>();
            ret = Utils.InitReturns();
            #region если параметр справочник
            if (prm.nzp_res > 0)
            {
                string sql = "select nzp_y, name_y from " + prm.pref + "_kernel" + tableDelimiter + "res_y where nzp_res = " + prm.nzp_res + " order by name_y";
                IDataReader reader2;
                ret = ExecRead(conn_db, out reader2, sql, true);
                if (!ret.result) return null;

                Res_y val;
                while (reader2.Read())
                {
                    try
                    {
                        val = new Res_y();
                        if (reader2["nzp_y"] != DBNull.Value) val.nzp_y = (int)reader2["nzp_y"];
                        if (reader2["name_y"] != DBNull.Value) val.name_y = (string)reader2["name_y"];
                        val.name_y = val.name_y.Trim();
                        list.Add(val);
                    }
                    catch (Exception ex)
                    {
                        reader2.Close();
                        ret.result = false;
                        ret.text = ex.Message;
                        MonitorLog.WriteLog("Ошибка получения справочника для параметра", MonitorLog.typelog.Error, 20, 201, true);
                        return null;
                    }
                }
                reader2.Close();
            }
            #endregion
            return list;
        }

        public Returns SaveResY(Res_y finder)
        {
            if (finder.nzp_res == Constants._ZERO_) return new Returns(false, "Не выбран справочник", -1);
            if (finder.nzp_user <= 0) return new Returns(false, "Пользователь не определен", -1);
            Returns ret;
            if (finder.pref == "") finder.pref = Points.Pref;
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(con_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("SaveResY : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return ret;
            }

            if (finder.is_del > 0)
            {
                bool res = CheckNzpY(con_db, finder, out ret);
                if (!ret.result) return ret;
                if (!res) return new Returns(false, "Удалить запись нельзя, так как на нее есть ссылки", -1);
            }


            StringBuilder sql = new StringBuilder();
            int total_record_count = finder.nzp_y;
            if (finder.nzp_y == Constants._ZERO_) //добавить в центральный банк
            {
                object count = ExecScalar(con_db, "select max(" + sNvlWord + "(nzp_y,0)) from " + Points.Pref +
                    "_kernel" + tableDelimiter + "res_y where nzp_res= " + finder.nzp_res, out ret, true);

                if (ret.result)
                {
                    try
                    {
                        if (count == null) total_record_count = 1;
                        else
                        {
                            Int32.TryParse(count.ToString(), out total_record_count);
                            total_record_count++;
                        }
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = ex.Message;
                        con_db.Close();
                        return ret;
                    }
                }
                else
                {
                    con_db.Close();
                    return ret;
                }

                sql.AppendFormat("insert into {0}_kernel{1}res_y (name_y, nzp_res, nzp_y) values('{2}', {3}, {4})",
                    Points.Pref, tableDelimiter, finder.name_y, finder.nzp_res, total_record_count);
                ret = ExecSQL(con_db, sql.ToString(), true);
                if (!ret.result)
                {
                    con_db.Close();
                    return ret;
                }
            }
            else
            {
                if (finder.is_del > 0)
                {
                    sql.Remove(0, sql.Length);
                    sql.AppendFormat("delete from {0}_kernel{1}res_y where nzp_y = {2} and nzp_res = {3}",
                    Points.Pref, tableDelimiter, finder.nzp_y, finder.nzp_res);
                }
                else
                {
                    sql.Remove(0, sql.Length);
                    sql.AppendFormat("update {0}_kernel{1}res_y set name_y = '{2}' where nzp_y = {3} and nzp_res={4}",
                        Points.Pref, tableDelimiter, finder.name_y, finder.nzp_y, finder.nzp_res);
                }
                ret = ExecSQL(con_db, sql.ToString(), true);
                if (!ret.result)
                {
                    con_db.Close();
                    return ret;
                }
            }
            List<bool> reslist = new List<bool>();
            foreach (_Point point in Points.PointList)
            {
                if (point.pref == Points.Pref) continue;
                if (finder.nzp_y != Constants._ZERO_)
                {
                    if (finder.is_del > 0)
                    {
                        sql.Remove(0, sql.Length);
                        sql.AppendFormat("delete from {0}_kernel{1}res_y where nzp_y = {2} and nzp_res = {3}",
                        point.pref, tableDelimiter, finder.nzp_y, finder.nzp_res);
                    }
                    else
                    {
                        sql.Remove(0, sql.Length);
                        sql.AppendFormat("update {0}_kernel{1}res_y set name_y = '{2}' where nzp_y = {3} and nzp_res={4}",
                            point.pref, tableDelimiter, finder.name_y, finder.nzp_y, finder.nzp_res);
                    }
                }
                else
                {
                    sql.Remove(0, sql.Length);

                    sql.AppendFormat("insert into {0}_kernel{1}res_y (name_y, nzp_res, nzp_y) values('{2}', {3}, {4})",
                        point.pref, tableDelimiter, finder.name_y, finder.nzp_res, total_record_count);
                }
                ret = ExecSQL(con_db, sql.ToString(), true);
                if (!ret.result) reslist.Add(false);
            }
            if (reslist.Count > 0) ret = new Returns(false, "Сохранение выполнено с ошибками", -1);
            con_db.Close();
            return ret;
        }

        public Returns SaveResolution(Resolution finder)
        {
            if (finder.nzp_user <= 0) return new Returns(false, "Пользователь не определен", -1);
            if (finder.nzp_prm <= 0) return new Returns(false, "Не задан параметр, для которого сохранять справочник", -1);
            Returns ret;
            if (finder.pref == "") finder.pref = Points.Pref;
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(con_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("SaveResolution : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return ret;
            }
            StringBuilder sql = new StringBuilder();

            sql.AppendFormat("insert into {0}_kernel{1}resolution ( name_short, name_res, is_readonly ) values ('{2}','{3}', 0)", Points.Pref, tableDelimiter, finder.name_short, finder.name_res);
            ret = ExecSQL(con_db, sql.ToString(), true);
            int nzp_res = 0;
            if (ret.result) nzp_res = GetSerialValue(con_db);
            if (!ret.result)
            {
                con_db.Close();
                return ret;
            }
            if (nzp_res > 0)
            {
                sql.Remove(0, sql.Length);
                sql.AppendFormat("update {0}_kernel{1}prm_name set nzp_res = {2} where nzp_prm = {3}", Points.Pref, tableDelimiter, nzp_res, finder.nzp_prm);
                ret = ExecSQL(con_db, sql.ToString(), true);
                if (!ret.result)
                {
                    con_db.Close();
                    return ret;
                }
            }

            foreach (_Point point in Points.PointList)
            {
                if (point.pref == Points.Pref) continue;
                if (point.pref != Points.Pref)
                {
                    sql.Remove(0, sql.Length);
                    sql.AppendFormat("insert into {0}_kernel{1}resolution (nzp_res, name_short, name_res, is_readonly ) values (" + nzp_res + ", '{2}','{3}', 0)", point.pref, tableDelimiter, finder.name_short, finder.name_res);
                    ret = ExecSQL(con_db, sql.ToString(), true);
                }
                if (nzp_res > 0)
                {
                    sql.Remove(0, sql.Length);
                    sql.AppendFormat("update {0}_kernel{1}prm_name set nzp_res = {2} where nzp_prm = {3}", point.pref, tableDelimiter, nzp_res, finder.nzp_prm);
                    ret = ExecSQL(con_db, sql.ToString(), true);
                }
            }
            con_db.Close();
            return ret;
        }

        private bool CheckNzpY(IDbConnection conn_db, Res_y finder, out Returns ret)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("select nzp_prm, prm_num from {0}_kernel{1}prm_name where trim(lower(type_prm))='sprav' and nzp_res = {2}",
                Points.Pref, tableDelimiter, finder.nzp_res);
            MyDataReader reader, reader2;
            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                return false;
            }


            while (reader.Read())
            {
                sql.Remove(0, sql.Length);
                sql.AppendFormat("select 1 from {0}_data{1}prm_{2} where val_prm='{3}' and nzp_prm = {4}",
                Points.Pref, tableDelimiter, Convert.ToString(reader["prm_num"]), finder.nzp_y, Convert.ToString(reader["nzp_prm"]));
                ret = ExecRead(conn_db, out reader2, sql.ToString(), true);
                if (!ret.result)
                {
                    return false;
                }
                if (reader2.Read())
                {
                    return false;
                }

                foreach (_Point point in Points.PointList)
                {
                    sql.Remove(0, sql.Length);
                    sql.AppendFormat("select 1 from {0}_data{1}prm_{2} where val_prm='{3}' and nzp_prm = {4}",
                    point.pref, tableDelimiter, Convert.ToString(reader["prm_num"]), finder.nzp_y, Convert.ToString(reader["nzp_prm"]));
                    ret = ExecRead(conn_db, out reader2, sql.ToString(), true);
                    if (!ret.result)
                    {
                        return false;
                    }
                    if (reader2.Read())
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public List<PrmTypes> NormParamValues(Param finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<PrmTypes> list = new List<PrmTypes>();
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(con_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("NormParamValues : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return list;
            }
            try
            {
                string sql = " SELECT DISTINCT param_value1, name_prm_val " +
                             " FROM " + Points.Pref + sKernelAliasRest + "influence_params" +
                             " WHERE nzp_prm =" + finder.nzp_prm +
                             " AND norm_tables_id in" +
                             " (SELECT id FROM " + Points.Pref + sKernelAliasRest + "norm_tables" +
                             " WHERE norm_type_id = " + finder.nzp + " ) " +
                             " ORDER BY param_value1";
                DataTable param = ClassDBUtils.OpenSQL(sql, con_db).resultData;
                foreach (DataRow rp in param.Rows)
                {
                    PrmTypes pt = new PrmTypes();
                    pt.id = Convert.ToInt32(rp["param_value1"]);
                    pt.type_name = rp["name_prm_val"].ToString();
                    list.Add(pt);
                }

                if (param.Rows.Count < 1)
                {

                    //В нормативе может быть одновременно на один параметр выбран и домовой, и лицевой параметр в одной строке. 
                    //В этом случае приоритет отдается параметру ЛС, и все записывается на параметр ЛС.
                    //Сейчас мы это учтем и вытащим для домового параметра по параметру ЛС

                    #region Вытаскиваем норматив на домовой параметр, у которого есть в паре лицевой параметр на этом нормативе
                    //выбираем соответствующие параметры ЛС
                    sql =
                        " SELECT nts.nzp_prm_ls " +
                        " FROM " + Points.Pref + sKernelAliasRest + "norm_types nt, " +
                        Points.Pref + sKernelAliasRest + "norm_types_sign nts " +
                        " WHERE  nts.norm_type_id = nt.id" +
                        " AND nts.nzp_prm_house = " + finder.nzp_prm + " AND " + sNvlWord + "(nzp_prm_ls, 0) > 0  " +
                        " AND nt.id = " + finder.nzp;
                    DataTable dt = ClassDBUtils.OpenSQL(sql, con_db).resultData;
                    foreach (DataRow r in dt.Rows)
                    {
                        sql =
                           " SELECT DISTINCT param_value1, name_prm_val " +
                           " FROM " + Points.Pref + sKernelAliasRest + "influence_params" +
                           " WHERE nzp_prm =" + r["nzp_prm_ls"] +
                           " AND norm_tables_id in" +
                           " (SELECT id FROM " + Points.Pref + sKernelAliasRest + "norm_tables" +
                           " WHERE norm_type_id = " + finder.nzp + " ) " +
                           " ORDER BY param_value1";
                        dt = ClassDBUtils.OpenSQL(sql, con_db).resultData;
                        foreach (DataRow rp in dt.Rows)
                        {
                            PrmTypes pt = new PrmTypes();
                            pt.id = Convert.ToInt32(rp["param_value1"]);
                            pt.type_name = rp["name_prm_val"].ToString();
                            list.Add(pt);
                        }
                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка получения списка услуг  значений параметров";
                MonitorLog.WriteLog("NormParamValues : Ошибка получения списка значений параметров " +
                    ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
                return list;
            }
            finally
            {
                con_db.Close();
            }

            return list;
        }


        /// <summary> Копирование характеристик и перечня услуг из одного ЛС в другой ЛС
        /// </summary>
        /// <param name="finderFrom"></param>
        /// <param name="finderTo"></param>
        /// <returns></returns>
        public Returns CopyLsParams(Ls finderFrom, Ls finderTo)
        {
            #region Проверка параметров
            if (finderFrom.nzp_user < 1) return new Returns(false, "Не определен пользователь");
            if (finderFrom.pref.Trim() == "" || finderTo.pref.Trim() == "") return new Returns(false, "Не определен префикс базы данных");
            if (finderFrom.nzp_kvar.ToString() == "" || finderTo.nzp_kvar.ToString() == "") return new Returns(false, "Не определен лицевой счет");
            #endregion
            Returns ret = Utils.InitReturns();

            #region Подключение к БД
            string connectionString = Points.GetConnByPref(finderFrom.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            ret = CopyLsParams(conn_db, finderFrom, finderTo);

            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            conn_db.Close();
            return ret;
        }

        /// <summary> Копирование характеристик и перечня услуг из одного ЛС в другой ЛС
        /// </summary>
        /// <param name="finderFrom"></param>
        /// <param name="finderTo"></param>
        /// <returns></returns>
        public Returns CopyLsParams(IDbConnection conn_db, Ls finderFrom, Ls finderTo)
        {
            Returns ret = Utils.InitReturns();

            IDbTransaction transaction;
            try { transaction = conn_db.BeginTransaction(); }
            catch { transaction = null; }

            ret = CopyLsParams(conn_db, transaction, finderFrom, finderTo);

            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                return ret;
            }
            if (transaction != null) transaction.Commit();
            return ret;
        }



        /// <summary> Копирование характеристик и перечня услуг из одного ЛС в другой ЛС
        /// </summary>
        /// <param name="finderFrom"></param>
        /// <param name="finderTo"></param>
        /// <returns></returns>
        public Returns CopyLsParams(IDbConnection conn_db, IDbTransaction transaction, Ls finderFrom, Ls finderTo)
        {
            //todo postrgeSQL ALL FUNCTION
            #region Проверка параметров
            if (finderFrom.nzp_user < 1) return new Returns(false, "Не определен пользователь");
            if (finderFrom.pref.Trim() == "" || finderTo.pref.Trim() == "") return new Returns(false, "Не определен префикс базы данных");
            if (finderFrom.nzp_kvar.ToString() == "" || finderTo.nzp_kvar.ToString() == "") return new Returns(false, "Не определен лицевой счет");
            #endregion

            Returns ret = Utils.InitReturns();

            #region определение локального пользователя
            int nzpUser = finderFrom.nzp_user;

            /*DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, transaction, finderFrom, out ret);
            db.Close();
            if (!ret.result)
            {
                return ret;
            }*/
            #endregion

            string sql = "";
            string dat_s = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).ToShortDateString() + "'"; //начало тек.расчетного периода
            // Копирование квартирных параметров
#if PG
            if (finderFrom.moving)//перенос в новую УК
            {

                sql = "Insert into " + finderTo.pref.Trim() + "_data.prm_1 (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,cur_unl,nzp_wp,nzp_user,dat_when)" +
                " Select " + finderTo.nzp_kvar + ",nzp_prm," + dat_s + ",dat_po,val_prm,is_actual,cur_unl,nzp_wp," + nzpUser + ",now()" +
                " From " + finderFrom.pref.Trim() + "_data.prm_1" +
                " Where is_actual <> 100 and " + Utils.EStrNull(finderFrom.stateValidOn) + " between dat_s and dat_po and nzp = " + finderFrom.nzp_kvar;
            }
            else
            {
                sql = "Insert into " + finderTo.pref.Trim() + "_data.prm_1 (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,cur_unl,nzp_wp,nzp_user,dat_when)" +
                " Select " + finderTo.nzp_kvar + ",nzp_prm,dat_s,dat_po,val_prm,is_actual,cur_unl,nzp_wp," + nzpUser + ",now()" +
                " From " + finderFrom.pref.Trim() + "_data.prm_1" +
                " Where is_actual <> 100 and " + Utils.EStrNull(finderFrom.stateValidOn) + " between dat_s and dat_po and nzp = " + finderFrom.nzp_kvar;
            }
#else
            if (finderFrom.moving)//перенос в новую УК
            {

                sql = "Insert into " + finderTo.pref.Trim() + "_data:prm_1 (nzp_key,nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,cur_unl,nzp_wp,nzp_user,dat_when)" +
                     " Select 0," + finderTo.nzp_kvar + ",nzp_prm," + dat_s + ",dat_po,val_prm,is_actual,cur_unl,nzp_wp," + nzpUser + ",current" +
                     " From " + finderFrom.pref.Trim() + "_data:prm_1" +
                     " Where is_actual <> 100 and " + Utils.EStrNull(finderFrom.stateValidOn) + " between dat_s and dat_po and nzp = " + finderFrom.nzp_kvar;
            }
            else
            {
                sql = "Insert into " + finderTo.pref.Trim() + "_data:prm_1 (nzp_key,nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,cur_unl,nzp_wp,nzp_user,dat_when)" +
                   " Select 0," + finderTo.nzp_kvar + ",nzp_prm,dat_s,dat_po,val_prm,is_actual,cur_unl,nzp_wp," + nzpUser + ",current" +
                   " From " + finderFrom.pref.Trim() + "_data:prm_1" +
                   " Where is_actual <> 100 and " + Utils.EStrNull(finderFrom.stateValidOn) + " between dat_s and dat_po and nzp = " + finderFrom.nzp_kvar;
            }
#endif

            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result)
            {
                return ret;
            }

            // Копирование перечня услуг
#if PG
            sql = "Insert into " + finderTo.pref.Trim() + "_data.tarif (nzp_kvar,num_ls,nzp_serv,nzp_supp,nzp_frm,tarif,dat_s,dat_po,is_actual,nzp_user,dat_when,cur_unl,nzp_wp)" +
                " Select " + finderTo.nzp_kvar + ", (select num_ls from " + finderTo.pref.Trim() + "_data" + tableDelimiter + "kvar where nzp_kvar = " +
                finderTo.nzp_kvar + "),nzp_serv,nzp_supp,nzp_frm,tarif,dat_s,dat_po,is_actual," + nzpUser + ",now(),cur_unl,nzp_wp" +
                " From " + finderFrom.pref.Trim() + "_data.tarif" +
                " Where is_actual <> 100 and " + Utils.EStrNull(finderFrom.stateValidOn) + " between dat_s and dat_po and nzp_kvar = " + finderFrom.nzp_kvar;
#else
            if (finderFrom.moving)
            {
                sql = "Insert into " + finderTo.pref.Trim() + "_data:tarif (nzp_tarif,nzp_kvar,num_ls,nzp_serv,nzp_supp,nzp_frm,tarif,dat_s,dat_po,is_actual,nzp_user,dat_when,cur_unl,nzp_wp)" +
                   " Select 0," + finderTo.nzp_kvar + ", (select num_ls from " + finderTo.pref.Trim() + "_data" + tableDelimiter + "kvar where nzp_kvar = " + 
                finderTo.nzp_kvar+"),nzp_serv,nzp_supp,nzp_frm,tarif," + dat_s + ",dat_po,is_actual," + nzpUser + ",current,cur_unl,nzp_wp" +
                   " From " + finderFrom.pref.Trim() + "_data:tarif" +
                   " Where is_actual <> 100 and " + Utils.EStrNull(finderFrom.stateValidOn) + " between dat_s and dat_po and nzp_kvar = " + finderFrom.nzp_kvar;
            }
            else
            {
                sql = "Insert into " + finderTo.pref.Trim() + "_data:tarif (nzp_tarif,nzp_kvar,num_ls,nzp_serv,nzp_supp,nzp_frm,tarif,dat_s,dat_po,is_actual,nzp_user,dat_when,cur_unl,nzp_wp)" +
                 " Select 0," + finderTo.nzp_kvar + ", (select num_ls from " + finderTo.pref.Trim() + "_data" + tableDelimiter + "kvar where nzp_kvar = " + 
                finderTo.nzp_kvar+"),nzp_serv,nzp_supp,nzp_frm,tarif,dat_s,dat_po,is_actual," + nzpUser + ",current,cur_unl,nzp_wp" +
                 " From " + finderFrom.pref.Trim() + "_data:tarif" +
                 " Where is_actual <> 100 and " + Utils.EStrNull(finderFrom.stateValidOn) + " between dat_s and dat_po and nzp_kvar = " + finderFrom.nzp_kvar;
            }
#endif

            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result)
            {
                return ret;
            }

            //            // Обновление номера лицевого счета в перечне услуг
            //#if PG
            //            sql = "Update " + finderTo.pref.Trim() + "_data.tarif " +
            //                " Set num_ls = (select num_ls from " + finderTo.pref.Trim() + "_data.kvar k where nzp_kvar = " + finderTo.nzp_kvar + ")" +
            //                " Where nzp_kvar = " + finderTo.nzp_kvar;
            //#else
            //            sql = "Update " + finderTo.pref.Trim() + "_data:tarif " +
            //                " Set num_ls = (select num_ls from " + finderTo.pref.Trim() + "_data:kvar k where nzp_kvar = " + finderTo.nzp_kvar + ")" +
            //                " Where nzp_kvar = " + finderTo.nzp_kvar;

            //#endif
            //            ret = ExecSQL(conn_db, transaction, sql, true);

            //перенос параметров из prm_3(кроме nzp_prm=51), prm_18(если есть) 
            if (finderFrom.moving)
            {
                string base_name = finderFrom.pref.Trim() + "_data";

#if PG
                sql = "Insert into " + finderTo.pref.Trim() + "_data.prm_3 (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,cur_unl,nzp_user, " +
                   " dat_when,dat_del,user_del,dat_block,user_block,month_calc) " +
                    " Select " + finderTo.nzp_kvar + ",nzp_prm," + dat_s + ",dat_po,val_prm,is_actual,cur_unl," + nzpUser + ",now(),dat_del,user_del,dat_block,user_block,month_calc" +
                   " From " + finderFrom.pref.Trim() + "_data.prm_3" +
                   " Where is_actual <> 100 and " + Utils.EStrNull(finderFrom.stateValidOn) + " between dat_s and dat_po and nzp = " + finderFrom.nzp_kvar;
#else
                sql = "Insert into " + finderTo.pref.Trim() + "_data:prm_3 (nzp_key,nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,cur_unl,nzp_user, " +
                   " dat_when,dat_del,user_del,dat_block,user_block,month_calc) " +
                    " Select 0," + finderTo.nzp_kvar + ",nzp_prm," + dat_s + ",dat_po,val_prm,is_actual,cur_unl," + nzpUser + ",current,dat_del,user_del,dat_block,user_block,month_calc" +
                   " From " + finderFrom.pref.Trim() + "_data:prm_3" +
                   " Where is_actual <> 100 and " + Utils.EStrNull(finderFrom.stateValidOn) + " between dat_s and dat_po and nzp = " + finderFrom.nzp_kvar;
#endif

                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result)
                {
                    return ret;
                }

                if (Points.IsSmr && TableInBase(conn_db, transaction, base_name, "prm_18"))
                {

#if PG
                    sql = "Insert into " + finderTo.pref.Trim() + "_data.prm_18 (nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,cur_unl,nzp_wp,nzp_user, " +
                                       " dat_when,dat_del,user_del,dat_block,user_block,month_calc)" +
                                       " Select " + finderTo.nzp_kvar + ",nzp_prm," + dat_s + ",dat_po,val_prm,is_actual,cur_unl,nzp_wp," + nzpUser + ",now(),dat_del,user_del,dat_block,user_block,month_calc" +
                                       " From " + finderFrom.pref.Trim() + "_data.prm_18" +
                                       " Where is_actual <> 100 and " + Utils.EStrNull(finderFrom.stateValidOn) + " between dat_s and dat_po and nzp = " + finderFrom.nzp_kvar;
#else
 sql = "Insert into " + finderTo.pref.Trim() + "_data:prm_18 (nzp_key,nzp,nzp_prm,dat_s,dat_po,val_prm,is_actual,cur_unl,nzp_wp,nzp_user, " +
                    " dat_when,dat_del,user_del,dat_block,user_block,month_calc)" +
                    " Select 0," + finderTo.nzp_kvar + ",nzp_prm," + dat_s + ",dat_po,val_prm,is_actual,cur_unl,nzp_wp," + nzpUser + ",current,dat_del,user_del,dat_block,user_block,month_calc" +
                    " From " + finderFrom.pref.Trim() + "_data:prm_18" +
                    " Where is_actual <> 100 and " + Utils.EStrNull(finderFrom.stateValidOn) + " between dat_s and dat_po and nzp = " + finderFrom.nzp_kvar;
#endif
                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result)
                    {
                        return ret;
                    }
                }
            }
            return ret;
        }

        private enum SaveModes
        {
            None = 0x00,            // Не определен
            SingleEntity = 0x01,    // Редактирование характеристики, привязанной к какоу-либо сущности (параметры ЛС, дома, услуги и т.п.)
            SingleCommon = 0x02,    // редактирование системного параметра, на всю базу и др., не привязанных к конкретной сущности
            GroupKvar = 0x03,       // групповые операции с характистиками жилья выбранных ЛС
            GroupDom = 0x04,        // групповые операции с параметрами выбранных домов
            GroupKvarForDom = 0x05  // групповые операции с характеристиками жилья для лицевых счетов выбранных домов
        }

        /// <summary> Сохранить или удалить значения параметров
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public Returns SavePrm(Param finder)
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return ret;
            }

            ret = SavePrm(conn_db, null, finder);

            conn_db.Close();
            return ret;
        }

        public Returns SavePrm(IDbConnection conn_db, IDbTransaction transaction, Param finder)
        {
            //Returns ret;
            //IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            //ret = OpenDb(conn_web, true);
            //if (!ret.result) return ret;

            //ret = SavePrm(conn_db, transaction, finder, conn_web);

            //conn_web.Close();
            //return ret;

            var dbSavePrm = new DbSavePrm(conn_db, transaction);
            return dbSavePrm.Save(finder);

        }

        /// <summary> Сохранить или удалить значения параметров
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public Returns SavePrm(IDbConnection conn_db, IDbTransaction transaction, Param finder, IDbConnection conn_web)
        {
            Returns ret;

            #region проверка входных параметров
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");


            SaveModes mode = SaveModes.None;
            if (ParamNums.lsParams.Contains(finder.prm_num))
            {
                if (Utils.GetParams(finder.prms, Constants.page_group_ls_prm_dom)) mode = SaveModes.GroupKvarForDom;
                else if (finder.nzp > 0) mode = SaveModes.SingleEntity;
                else mode = SaveModes.GroupKvar;
            }
            else if (finder.prm_num == 2)
            {
                if (finder.nzp > 0) mode = SaveModes.SingleEntity;
                else mode = SaveModes.GroupDom;
            }
            else if (ParamNums.generalParams.Contains(finder.prm_num))
            {
                mode = SaveModes.SingleCommon;
            }
            else if (finder.prm_num == 4 || finder.prm_num == 6 || finder.prm_num == 7 || finder.prm_num == 8 || finder.prm_num == 11 || finder.prm_num == 12 || finder.prm_num == 17)
            {
                mode = SaveModes.SingleEntity;
            }

            if (mode == SaveModes.None) return new Returns(false, "Неверные входные параметры");

            if (!(finder.prm_num == 7 || finder.prm_num == 8 || finder.prm_num == 12))
                if (finder.nzp > 0 && finder.pref.Trim() == "") return new Returns(false, "Не задан префикс базы данных");

            if (mode == SaveModes.GroupKvar || mode == SaveModes.GroupDom || mode == SaveModes.GroupKvarForDom)
                if (finder.listNumber < 0)
                {
                    return new Returns(false, "Список лицевых счетов не сформирован", -1);
                }
            #endregion

            // Алгоритм
            // 1. Если это групповая операция
            //   1.1. Определить список префиксов БД
            //   1.2. Организовать цикл по префиксам
            //     1.2.1. Для каждого префикса выполнить сохранение
            // 2. Если это операция с одним домом или ЛС
            //   2.1. Выполнить сохранение


            if (mode == SaveModes.GroupKvar || mode == SaveModes.GroupDom || mode == SaveModes.GroupKvarForDom) // групповая операция по выбранным спискам ЛС или домов
            {
                string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_selectedls" + finder.listNumber;//"_spls";
#if PG
                string tXX_spls_full = "public" + DBManager.tableDelimiter + tXX_spls;
#else
                string tXX_spls_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spls;

#endif
                string tXX_spdom = "t" + Convert.ToString(finder.nzp_user) + "_spdom";
#if PG
                string tXX_spdom_full = "public" + DBManager.tableDelimiter + tXX_spdom;
#else
                string tXX_spdom_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXX_spdom;

#endif
                List<string> prefics = new List<string>(); // список префиксов

                if (mode == SaveModes.GroupKvar && !TableInWebCashe(conn_web, tXX_spls))
                {
                    return new Returns(false, "Не выбран список лицевых счетов", -1);
                }

                if ((mode == SaveModes.GroupDom || mode == SaveModes.GroupKvarForDom) &&
                    !TableInWebCashe(conn_web, tXX_spdom))
                {
                    return new Returns(false, "Не выбран список домов", -1);
                }

                // Получить список префиксов из списка выбранных ЛС, домов
                string sql = "", where_pref = "";
                if (finder.pref_sprav != "") where_pref = " and pref='" + finder.pref_sprav + "'";
#if PG
                if (mode == SaveModes.GroupKvar) sql = "select distinct pref from " + tXX_spls + " where mark = 1" + where_pref;
                else if (mode == SaveModes.GroupDom || mode == SaveModes.GroupKvarForDom) sql = "select distinct pref from " + tXX_spdom + " where mark = 1" + where_pref;
#else
                if (mode == SaveModes.GroupKvar) sql = "select unique pref from " + tXX_spls + " where mark = 1" + where_pref;
                else if (mode == SaveModes.GroupDom || mode == SaveModes.GroupKvarForDom) sql = "select unique pref from " + tXX_spdom + " where mark = 1" + where_pref;
#endif

                IDataReader reader;
                ret = ExecRead(conn_web, out reader, sql, true);
                if (!ret.result)
                {
                    return ret;
                }

                while (reader.Read())
                {
                    if (reader["pref"] != DBNull.Value)
                        prefics.Add(((string)reader["pref"]).Trim());
                }
                reader.Close();

                foreach (string pref in prefics) // для каждого префикса вызвать функцию сохранения параметров
                {
                    EditInterData editData = new EditInterData();

                    editData.pref = pref;                                       // префикс БД
                    //DbSprav db = new DbSprav();
                    //editData.nzp_wp = db.GetPoint(editData.pref).nzp_wp;
                    //db.Close();
                    editData.nzp_wp = Points.GetPoint(editData.pref).nzp_wp;

                    editData.nzp_user = finder.nzp_user;                        // Пользователь, от имени которого ведется сохранение
                    editData.webLogin = finder.webLogin;
                    editData.webUname = finder.webUname;
                    editData.primary = "nzp_key";                               // Первичный ключ целевой таблицы
                    editData.table = "prm_" + finder.prm_num;                   // Указываем таблицу для редактирования

                    //указываем вставляемый период
                    if (finder.dat_s == "") editData.dat_s = "01.01.1901";
                    else editData.dat_s = finder.dat_s;
                    if (finder.dat_po == "") editData.dat_po = "01.01.3000";
                    else editData.dat_po = finder.dat_po;

                    Param param = new Param();
                    param.nzp_prm = finder.nzp_prm;
                    editData.intvType = param.intvtype;

#if PG
                    string kvar = pref + "_data.kvar";
#else
                    string kvar = pref + "_data:kvar";
#endif
                    string kvar_filter = " 1=0 ";
                    string dom_filter = " 1=0 ";
                    string mc_filter = " 1=0 ";

                    //условие выборки данных из целевой таблицы
                    editData.dopFind = new List<string>();
                    sql = " and nzp_prm = " + finder.nzp_prm;
                    if (mode == SaveModes.GroupKvar)
                    {
                        kvar_filter = " in (select nzp_kvar from " + tXX_spls_full + " where pref = " + Utils.EStrNull(pref) + " and  mark = 1) ";
                        sql += " and nzp" + kvar_filter;
                        mc_filter = " and p.nzp" + kvar_filter;
                    }
                    else if (mode == SaveModes.GroupDom)
                    {
                        mc_filter = " and p.nzp in (select nzp_dom from " + tXX_spdom_full + " where pref = " + Utils.EStrNull(pref) + " and  mark = 1) ";
                        sql += " and nzp in (select nzp_dom from " + tXX_spdom_full + " where pref = " + Utils.EStrNull(pref) + " and  mark = 1) ";
                    }
                    else if (mode == SaveModes.GroupKvarForDom)
                    {
                        kvar_filter = " in (select b.nzp_kvar from " + tXX_spdom_full + " a, " + kvar + " b where a.pref = " + Utils.EStrNull(pref) + " and a.mark = 1 and a.nzp_dom = b.nzp_dom) ";
                        dom_filter = "nzp_dom in (select nzp_dom from " + tXX_spdom_full + " where pref = " + Utils.EStrNull(pref) + " and mark = 1) ";
                        sql += " and nzp " + kvar_filter;
                        mc_filter = " and p.nzp " + kvar_filter;
                    }
                    editData.dopFind.Add(sql);

                    //перечисляем ключевые поля и значения (со знаком сравнения!)
                    Dictionary<string, string> keys = new Dictionary<string, string>();
                    switch (finder.prm_num)
                    {
                        case 1:
                        case 3:
                        case 18:
                        case 19:
                            {
                                if (mode == SaveModes.GroupKvar)
                                    keys.Add("nzp", "1|nzp_kvar|" + tXX_spls_full + "|nzp_kvar" + kvar_filter); //ссылка на ключевую таблицу
                                else if (mode == SaveModes.GroupKvarForDom)
                                    keys.Add("nzp", "1|nzp_kvar|kvar|" + dom_filter); //ссылка на ключевую таблицу
                                break;
                            }
                        case 2:
                            {
                                keys.Add("nzp", "1|nzp_dom|" + tXX_spdom_full + "|nzp_dom in (select nzp_dom from " + tXX_spdom_full + " where pref = " + Utils.EStrNull(pref) + " and  mark = 1) ");
                                break;
                            }
                    }
                    keys.Add("nzp_prm", "2|" + finder.nzp_prm);
                    editData.keys = keys;

                    //перечисляем поля и значения этих полей, которые вставляются
                    Dictionary<string, string> vals = new Dictionary<string, string>();
                    vals.Add("val_prm", finder.val_prm);
                    editData.vals = vals;
                    editData.todelete = Utils.GetParams(finder.prms, Constants.act_del_val);

                    //вызов сервиса
                    DbEditInterData dbi = new DbEditInterData();
                    dbi.Saver(conn_db, transaction, editData, out ret);
                    if (!ret.result)
                    {
                        dbi.Close();
                        return ret;
                    }

                    if (Points.RecalcMode == RecalcModes.AutomaticWithCancelAbility)
                    {
                        EditInterDataMustCalc eid = new EditInterDataMustCalc();

                        if (ParamNums.lsParams.Contains(finder.prm_num))
                        {
                            eid.mcalcType = enMustCalcType.mcalc_Prm1;
                        }
                        else if (ParamNums.domParams.Contains(finder.prm_num))
                        {
                            eid.mcalcType = enMustCalcType.mcalc_Prm2;
                        }
                        else eid.mcalcType = enMustCalcType.None;

                        if (eid.mcalcType != enMustCalcType.None)
                        {
                            eid.nzp_wp = editData.nzp_wp;
                            eid.pref = editData.pref;
                            eid.nzp_user = editData.nzp_user;
                            eid.webLogin = editData.webLogin;
                            eid.webUname = editData.webUname;
                            eid.dat_s = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).ToShortDateString() + "'";
                            eid.dat_po = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(1).AddDays(-1).ToShortDateString() + "'";
                            eid.intvType = editData.intvType;
                            eid.table = editData.table;
                            eid.primary = editData.primary;

                            eid.keys = new Dictionary<string, string>();
                            eid.vals = new Dictionary<string, string>();

                            eid.dopFind = new List<string>();
                            mc_filter += " and p.nzp_prm = " + finder.nzp_prm;
                            eid.dopFind.Add(mc_filter);
                            eid.comment_action = finder.comment_action;
                            dbi.MustCalc(conn_db, transaction, eid, out ret);
                        }

                        if (!ret.result)
                        {
                            dbi.Close();
                            return ret;
                        }
                    }

                    dbi.Close();
                }
            }
            else // это операция с одним домом или ЛС
            {
                EditInterData editData = new EditInterData();

                //укзываем таблицу для редактирования
                if (finder.pref == "") finder.pref = Points.Pref;
                editData.pref = finder.pref;

                //DbSprav dbs = new DbSprav();
                //editData.nzp_wp = dbs.GetPoint(editData.pref).nzp_wp;
                //dbs.Close();
                editData.nzp_wp = Points.GetPoint(editData.pref).nzp_wp;

                editData.nzp_user = finder.nzp_user;
                editData.webLogin = finder.webLogin;
                editData.webUname = finder.webUname;
                editData.primary = "nzp_key";
                editData.table = "prm_" + finder.prm_num;

                //указываем вставляемый период
                if (finder.dat_s == "") editData.dat_s = "01.01.1901";
                else editData.dat_s = finder.dat_s;
                if (finder.dat_po == "") editData.dat_po = "01.01.3000";
                else editData.dat_po = finder.dat_po;

                ParamCommon param = new ParamCommon();
                param.nzp_prm = finder.nzp_prm;
                editData.intvType = param.intvtype;

                //условие выборки данных из целевой таблицы
                editData.dopFind = new List<string>();
                editData.dopFind.Add(" and nzp_prm = " + finder.nzp_prm);
                editData.dopFind.Add(" and nzp = " + finder.nzp);

                string mc_filter = " and p.nzp = " + finder.nzp;

                //перечисляем ключевые поля и значения (со знаком сравнения!)
                Dictionary<string, string> keys = new Dictionary<string, string>();
                switch (finder.prm_num)
                {
                    case 1:
                    case 3:
                    case 18:
                    case 19:
                        {
                            keys.Add("nzp", "1|nzp_kvar|kvar|nzp_kvar = " + finder.nzp); //ссылка на ключевую таблицу
                            break;
                        }
                    case 2:
                    case 4:
                        {
                            keys.Add("nzp", "1|nzp_dom|dom|nzp_dom = " + finder.nzp);
                            break;
                        }
                    case 6:
                        {
                            keys.Add("nzp", "1|nzp_ul|s_ulica|nzp_ul = " + finder.nzp);
                            break;
                        }
                    case 7:
                        {
                            keys.Add("nzp", "1|nzp_area|s_area|nzp_area = " + finder.nzp);
                            break;
                        }
                    case 8:
                        {
                            keys.Add("nzp", "1|nzp_geu|s_geu|nzp_geu = " + finder.nzp);
                            break;
                        }
                    case 11:
                        {
                            keys.Add("nzp", "1|nzp_supp|supplier|nzp_supp = " + finder.nzp);
                            break;
                        }
                    case 12:
                        {
                            keys.Add("nzp", "1|nzp_serv|services|nzp_serv = " + finder.nzp);
                            break;
                        }
                    case 17:
                        {
                            keys.Add("nzp", "1|nzp_counter|counters_spis|nzp_counter = " + finder.nzp);
                            break;
                        }
                    case 5:
                    case 10:
                        {
                            keys.Add("nzp", "5|0");
                            mc_filter = "";
                            break;
                        }
                }
                keys.Add("nzp_prm", "2|" + finder.nzp_prm);
                editData.keys = keys;

                //перечисляем поля и значения этих полей, которые вставляются
                Dictionary<string, string> vals = new Dictionary<string, string>();
                vals.Add("val_prm", finder.val_prm);
                editData.vals = vals;
                editData.todelete = finder.prms == Constants.act_del_val.ToString();

                //вызов сервиса
                DbEditInterData db = new DbEditInterData();
                db.Saver(conn_db, transaction, editData, out ret);

                if (ret.result)
                {
                    #region Добавление признаков перерасчетов
                    if (Points.RecalcMode == RecalcModes.AutomaticWithCancelAbility)
                    {
                        EditInterDataMustCalc eid = new EditInterDataMustCalc();

                        eid.mcalcType = enMustCalcType.None;
                        if (ParamNums.lsParams.Contains(finder.prm_num))
                        {
                            eid.mcalcType = enMustCalcType.mcalc_Prm1;
                        }
                        else if (ParamNums.domParams.Contains(finder.prm_num))
                        {
                            eid.mcalcType = enMustCalcType.mcalc_Prm2;
                        }
                        else if (ParamNums.counterParams.Contains(finder.prm_num))
                        {
                            //eid.mcalcType = enMustCalcType.Prm17;
                        }

                        if (eid.mcalcType != enMustCalcType.None)
                        {
                            eid.nzp_wp = editData.nzp_wp;
                            eid.pref = editData.pref;
                            eid.nzp_user = editData.nzp_user;
                            eid.webLogin = editData.webLogin;
                            eid.webUname = editData.webUname;
                            eid.dat_s = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).ToShortDateString() + "'";
                            eid.dat_po = "'" + new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(1).AddDays(-1).ToShortDateString() + "'";
                            eid.intvType = editData.intvType;
                            eid.table = editData.table;
                            eid.primary = editData.primary;
                            eid.kod2 = finder.nzp_prm;

                            eid.keys = new Dictionary<string, string>();
                            eid.vals = new Dictionary<string, string>();

                            eid.dopFind = new List<string>();
                            mc_filter += " and p.nzp_prm = " + finder.nzp_prm;
                            eid.dopFind.Add(mc_filter);
                            eid.comment_action = finder.comment_action;
                            db.MustCalc(conn_db, transaction, eid, out ret);
                        }

                        if (!ret.result)
                        {
                            db.Close();
                            return ret;
                        }
                    }
                    #endregion
                }

                if (finder.prm_num == 3 && finder.nzp_prm == 51 && finder.val_prm == "2")
                {
                    #region Добавление в sys_events события 'Закрытие лицевого счёта'
                    DbAdmin.InsertSysEvent(new SysEvents()
                    {
                        pref = editData.pref,
                        nzp_user = finder.nzp_user,
                        nzp_dict = 6483,
                        nzp_obj = Convert.ToInt32(finder.nzp),
                        note = "Лицевой счет был закрыт"
                    }, transaction, conn_db);
                    #endregion
                }

                db.Close();
            }
            return ret;
        }


        /// <summary>Сохранить параметр 1272 - ФИО начальника абонентского отдела</summary>
        /// <param name="finder">Информационный параметр</param>
        /// <returns>Информация о выполение функции</returns>
        public Returns SavePrmArea(Prm finder)
        {
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");
            if (finder.nzp == 0 || string.IsNullOrEmpty(finder.val_prm))
                return new Returns(false, "Не заданы параметры");

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(connDB, true);
            if (!ret.result) return ret;

            try
            {
                string prefData = Points.Pref + DBManager.sDataAliasRest;

                DateTime nowDate = DateTime.Now;

                string sql = " UPDATE " + prefData + "prm_7 SET (dat_po) = (DATE('" + nowDate.ToShortDateString() + "')) " +
                             " WHERE nzp_prm = 1272 " +
                               " AND is_actual <> 100 " +
                               " AND nzp = " + finder.nzp +
                               " AND dat_s <= DATE('" + nowDate.ToShortDateString() + "') ";
                ret = ExecSQL(connDB, sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " UPDATE " + prefData + "prm_7 SET (dat_s,dat_po) = (DATE('" + nowDate.ToShortDateString() + "')," +
                                                                     " DATE('" + DateTime.MaxValue.ToShortDateString() + "'))" +
                      " WHERE nzp_prm = 1272 " +
                        " AND nzp = " + finder.nzp +
                        " AND is_actual <> 100 " +
                        " AND dat_s > DATE('" + nowDate.ToShortDateString() + "') ";
                ret = ExecSQL(connDB, sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " UPDATE " + prefData + "prm_7 SET (dat_del) = (DATE('" + nowDate.ToShortDateString() + "'))" +
                      " WHERE nzp_prm = 1272 " +
                        " AND nzp = " + finder.nzp +
                        " AND is_actual <> 100 ";
                ret = ExecSQL(connDB, sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " UPDATE " + prefData + "prm_7 SET (user_del) = (" + finder.nzp_user + ")" +
                      " WHERE nzp_prm = 1272 " +
                        " AND nzp = " + finder.nzp +
                        " AND is_actual <> 100 ";
                ret = ExecSQL(connDB, sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " UPDATE " + prefData + "prm_7 SET (is_actual) = (100)" +
                      " WHERE nzp_prm = 1272 " +
                        " AND nzp = " + finder.nzp +
                        " AND is_actual <> 100 ";
                ret = ExecSQL(connDB, sql);
                if (!ret.result) throw new Exception(ret.text);

                //открываем новые параметры 
                sql = " INSERT INTO " + prefData + "prm_7 (nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual,dat_when,nzp_user)" +
                      " VALUES (" + finder.nzp + ", 1272, '" + nowDate.ToShortDateString() + "', '01.01.3000', '" + finder.val_prm + "',1, " +
                                    " '" + nowDate.ToShortDateString() + "', " + finder.nzp_user + ") ";
                ret = ExecSQL(connDB, sql);
                if (!ret.result) throw new Exception(ret.text);
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("SavePrmArea:" + e.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка Сохранения параметра";
                ret.result = false;
                return ret;
            }
            finally
            {
                connDB.Close();
            }

            return ret;
        }


        public Prm FindSimplePrmValue(Prm finder, out Returns ret)
        {
            IDbConnection conn_db = null;
            try
            {
                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения с БД.", MonitorLog.typelog.Error, true);
                    return null;
                }

                return this.FindSimplePrmValue(conn_db, finder, out ret);
            }
            finally
            {
                if (conn_db != null) conn_db.Close();
            }
        }

        public Prm FindSimplePrmValue(IDbConnection conn_db, Prm finder, out Returns ret)
        {
            #region
            if (finder.nzp_user < 1) { ret = new Returns(false, "Не определен пользователь"); return null; }
            if (finder.pref == "") { ret = new Returns(false, "Не определен префикс БД"); return null; }
            if (finder.prm_num < 1) { ret = new Returns(false, "Не задан prm_num"); return null; }
            if (finder.nzp_prm < 1) { ret = new Returns(false, "Не задан nzp_prm"); return null; }
            if (finder.nzp < 1 && finder.prm_num != 10 && finder.prm_num != 5 && finder.prm_num != 20) { ret = new Returns(false, "Не задан nzp"); return null; }
            if (finder.year_ < 1) { ret = new Returns(false, "Не задан год"); return null; }
            if (finder.month_ < 1) { ret = new Returns(false, "Не задан месяц"); return null; }
            #endregion

            DateTime dat = new DateTime(finder.year_, finder.month_, 1);

            Prm prm = new Prm();

            if (TempTableInWebCashe(conn_db, finder.pref + sDataAliasRest + "prm_" + finder.prm_num))

            {
                string sql = 
                    " Select val_prm From " + finder.pref + sDataAliasRest + "prm_" + finder.prm_num +
                    " Where nzp_prm = " + finder.nzp_prm + " and nzp = " + finder.nzp + " and is_actual <> 100" +
                    " and " + Utils.EStrNull(dat.ToShortDateString()) + " between dat_s and dat_po";

                IDataReader reader;
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) return null;

                if (reader.Read())
                {
                    if (reader["val_prm"] != DBNull.Value) prm.val_prm = Convert.ToString(reader["val_prm"]).Trim();
                }
                reader.Close();
            }

            else
            {
                ret = Utils.InitReturns();
            }
            return prm;
        }

        /// <summary> Определить значение параметра
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public Prm FindPrmValue(IDbConnection conn_db, Prm finder, out Returns ret)
        {
            return FindPrmValue(conn_db, null, finder, out ret);
        }

        public Prm FindPrmValue(IDbConnection conn_db, IDbTransaction transaction, Prm finder, out Returns ret)
        {
            int nzpUser = 0;
            if (finder.checkDataBlocking == 1)
            {
                nzpUser = finder.nzp_user;

                /*#region определение локального пользователя
                DbWorkUser db = new DbWorkUser();
                nzpUser = db.GetLocalUser(conn_db, transaction, finder, out ret);
                db.Close();
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }
                #endregion*/
            }

#if PG
            string prm_name_n = finder.pref + "_kernel.prm_name n ";
            string prm_N = "prm_" + finder.prm_num;
            string prm_N_full = finder.pref + "_data." + prm_N;
#else
            string prm_name_n = finder.pref + "_kernel@" + DBManager.getServer(conn_db) + ":prm_name n ";
            string prm_N = "prm_" + finder.prm_num;
            string prm_N_full = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":" + prm_N;
#endif
            string prm_N_p = prm_N_full + " p ";

            ret = Utils.InitReturns();

            if (!TempTableInWebCashe(conn_db, transaction, prm_N_full))
            {
                return new Prm();
            }

            DateTime dat = new DateTime(finder.year_, finder.month_, 1);
            string sql;

            if (Points.isNewNorms)
            {
                sql =
                    " SELECT n.name_prm," +
                    " (CASE WHEN n.type_prm = 'sprav' AND coalesce(nps.nzp_prm, 0) > 0 THEN 'norm' else n.type_prm END) as type_prm, " +
                    " n.nzp_res, n.prm_num, n.low_, n.is_day_uchet, n.high_, n.digits_,p.*, u.comment as user_name_block," +
#if PG
                    " (now() - INTERVAL " + string.Format("'{0} minutes')", Constants.blocking_lifetime) + " as cur_dat" +
#else
                " (current year to second - " + Constants.blocking_lifetime.ToString() + " units minute) as cur_dat" +
#endif
 " FROM " + prm_name_n +
                    " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest + "norm_prm_serv nps on nps.nzp_prm = n.nzp_prm, " + prm_N_p +
                    " LEFT OUTER JOIN " + Points.Pref + sDataAliasRest + "users u on p.user_block = u.nzp_user " +
                    " WHERE p.nzp_prm=n.nzp_prm and p.nzp_prm = " + finder.nzp_prm + " and p.nzp = " + finder.nzp +
                    " AND " + Utils.EStrNull(dat.ToShortDateString()) + " between p.dat_s and p.dat_po" +
                    " AND p.is_actual <> 100 ";
            }
            else
            {
                sql =
                    " SELECT *, u.comment as user_name_block," +
#if PG
                        " (now() - INTERVAL " + string.Format("'{0} minutes')", Constants.blocking_lifetime) +
                    " as cur_dat" +
#else
                " (current year to second - " + Constants.blocking_lifetime.ToString() + " units minute) as cur_dat" +
#endif
                        " FROM " + prm_name_n + ", " + prm_N_p +
                    " LEFT OUTER JOIN " + Points.Pref + sDataAliasRest + "users u on p.user_block = u.nzp_user " +
                    " WHERE p.nzp_prm=n.nzp_prm and p.nzp_prm = " + finder.nzp_prm + " and p.nzp = " + finder.nzp +
                    " AND " + Utils.EStrNull(dat.ToShortDateString()) + " between p.dat_s and p.dat_po" +
                    " AND p.is_actual <> 100 ";
            }
            IDataReader reader;
            ret = ExecRead(conn_db, transaction, out reader, sql, true);
            if (!ret.result) return null;
            if (!reader.Read())
            {
                ret = new Returns(false, "Значение параметра не найдено", -1);
                return null;
            }

            string bl = "";
            if (finder.checkDataBlocking == 1)
            {
                #region Проверка блокировки записей
                DateTime dt_block = DateTime.MinValue;
                DateTime dt_cur = DateTime.MinValue;
                int user_block = 0;
                string userNameBlock = "";

                if (reader["user_block"] != DBNull.Value) user_block = (int)reader["user_block"]; //пользователь, который заблокировал
                if (reader["user_name_block"] != DBNull.Value) userNameBlock = ((string)reader["user_name_block"]).Trim(); //имя пользователь, который заблокировал
                if (reader["dat_block"] != DBNull.Value) dt_block = Convert.ToDateTime(reader["dat_block"]);//дата блокировки
                if (reader["cur_dat"] != DBNull.Value) dt_cur = Convert.ToDateTime(reader["cur_dat"]);//текущее время/дата - XX мин

                if (user_block > 0 && dt_block != DateTime.MinValue) //заблокирован прибор учета
                    if (user_block != nzpUser && dt_cur < dt_block) //если заблокирована запись другим пользователем и XX мин не прошло
                        bl = "Параметр заблокирован пользователем " + userNameBlock + " " + dt_block.ToString("dd.MM.yyyy в HH:mm") + ". Редактировать данные запрещено.";

                if (bl == "") // действующей блокировки нет, или она сделана самим пользователем
                {
                    if (Utils.GetParams(finder.prms, Constants.act_mode_edit.ToString())) //если берут данные на изменение
                    {
                        ret = ExecSQL(conn_db, transaction, "update " + prm_N_full + " set dat_block = current year to minute, user_block = " + nzpUser + " where nzp_prm = " + finder.nzp_prm + " and nzp = " + finder.nzp, true);
                        if (!ret.result)
                        {
                            ret.result = false;
                            ret.text = "Ошибка обновления таблицы " + prm_N;
                            return null;
                        }
                    }
                    else //если  на просмотр
                    {
                        ret = ExecSQL(conn_db, transaction, "update " + prm_N_full + " set dat_block = null, user_block = null where nzp_prm = " + finder.nzp_prm + " and nzp = " + finder.nzp, true);
                        if (!ret.result)
                        {
                            ret.result = false;
                            ret.text = "Ошибка обновления таблицы prm_" + prm_N;
                            return null;
                        }
                    }
                }
                #endregion
            }
            Prm prm = new Prm();

            if (reader["nzp_key"] != DBNull.Value) prm.nzp_key = ((Int32)reader["nzp_key"]);
            if (reader["nzp"] != DBNull.Value) prm.nzp = ((Int32)reader["nzp"]);
            if (reader["nzp_prm"] != DBNull.Value) prm.nzp_prm = ((Int32)reader["nzp_prm"]);
            if (reader["dat_s"] != DBNull.Value) prm.dat_s = Convert.ToDateTime(reader["dat_s"]).ToString("dd.MM.yyyy");
            if (reader["dat_po"] != DBNull.Value) prm.dat_po = Convert.ToDateTime(reader["dat_po"]).ToString("dd.MM.yyyy");
            if (reader["val_prm"] != DBNull.Value) prm.val_prm = ((string)reader["val_prm"]).Trim();
            if (reader["is_actual"] != DBNull.Value) prm.is_actual = ((Int32)reader["is_actual"]);
            if (reader["dat_del"] != DBNull.Value) prm.dat_del = Convert.ToDateTime(reader["dat_del"]).ToString("dd.MM.yyyy");
            if (reader["user_del"] != DBNull.Value) prm.user_del = Convert.ToInt32(reader["user_del"]);
            if (reader["dat_when"] != DBNull.Value) prm.dat_when = Convert.ToDateTime(reader["dat_when"]).ToString("dd.MM.yyyy");
            if (reader["nzp_user"] != DBNull.Value) prm.nzp_res = Convert.ToInt32(reader["nzp_user"]);

            if (reader["name_prm"] != DBNull.Value) prm.name_prm = ((string)reader["name_prm"]).Trim();
            if (reader["type_prm"] != DBNull.Value) prm.type_prm = ((string)reader["type_prm"]).Trim();
            if (reader["nzp_res"] != DBNull.Value) prm.nzp_res = Convert.ToInt32(reader["nzp_res"]);
            if (reader["prm_num"] != DBNull.Value) prm.prm_num = Convert.ToInt32(reader["prm_num"]);

            prm.user_name = "";
            prm.block = bl;

            if (prm.type_prm == "bool")
            {
                if (prm.val_prm == "1") prm.val_prm = "да";
                else prm.val_prm = "нет";
            } 
            if (prm.type_prm == "norm")
            {
                if (prm.param == null)
                {
                    prm.param = new Param { pref = finder.pref, nzp_prm = prm.nzp_prm, val_prm = prm.val_prm };
                }
                DateTime datNormBetween = Points.DateOper;
                prm.param.norm_sprav_values = LoadSpravValueFromNormativ(conn_db, prm.param, out ret, datNormBetween);
                if (!String.IsNullOrEmpty(prm.val_prm))
                {
                    prm.param.val_prm_sprav = Convert.ToInt32(prm.val_prm);
                    prm.val_prm_sprav = Convert.ToInt32(prm.val_prm);
                }
                prm.month_ = Points.DateOper.Month;
                prm.year_ = Points.DateOper.Year;
                prm.pref = finder.pref;
                Param param = prm.param;
                GetSelectedNormIdByVal(conn_db, prm, ref param, out ret);
                prm.old_val_prm = param.old_val_prm;
            }
            else if (prm.type_prm == "sprav")
            {
                int valPrm;
                if (Int32.TryParse(prm.val_prm, out valPrm))
                {
                    prm.val_prm_sprav = valPrm;
                    reader.Close();

#if PG
                    sql = "Select max(r.name_y) as val From " + finder.pref + "_kernel.res_y r" +
#else
                    sql = "Select max(r.name_y) as val From " + finder.pref + "_kernel@" + DBManager.getServer(conn_db) + ":res_y r" +
#endif
 " Where r.nzp_res=" + prm.nzp_res + " and r.nzp_y = " + valPrm;
                    ret = ExecRead(conn_db, transaction, out reader, sql, true);
                    if (!ret.result)
                    {
                        return null;
                    }
                    if (!reader.Read())
                    {
                        ret = new Returns(false, "Справочное значение параметра не найдено", -1);
                        return null;
                    }
                    if (reader["val"] != DBNull.Value)
                        prm.val_prm = ((string)reader["val"]).Trim();
                    else
                    {
                        reader.Close();
                        ret = new Returns(false, "Справочное значение параметра имеет пустое значение", -1);
                        return null;
                    }
                }
                else
                {
                    reader.Close();

                    ret = new Returns(false, "Значение параметра имеет неверный формат", -1);
                    return null;
                }
            }

            reader.Close();
            return prm;
        }


        /// <summary> Определить значение параметров
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<Prm> FindPrmValues(IDbConnection conn_db, Prm finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            int nzpUser = finder.nzp_user;

            #region определение локального пользователя
            /*DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, finder, out ret);
            db.Close();
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }*/
            #endregion

            string prm_name_n = finder.pref + sKernelAliasRest + "prm_name n ";
            string prm_N = "prm_" + finder.prm_num;
            string prm_N_full = finder.pref + sDataAliasRest + prm_N;

            string prm_N_p = prm_N_full + " p ";

            DateTime dat = new DateTime(finder.year_, finder.month_, 1);

            Prm p = new Prm();
            string rows = "";

            if (finder.rows > 0) rows = sFirstWord + " " + finder.rows.ToString();
            else rows = "";
            string sql;

            if (Points.isNewNorms)
            {
                sql = "Select " +
#if PG
#else
                rows +
#endif
 " val_prm," +
                      " (CASE WHEN n.type_prm = 'sprav' AND coalesce(nps.nzp_prm, 0) > 0 THEN 'norm' else n.type_prm END) as type_prm, " +
                      " n.name_prm, nzp_res, p.dat_block, p.dat_s,p.dat_po," +
                      " p.nzp, p.nzp_prm, p.user_block, u.comment as user_name_block," +
#if PG
 " (now () - interval " + "'" + Constants.blocking_lifetime.ToString() + " minutes'" + "   minute) as cur_dat" +
#else
                         " (current year to second - " + Constants.blocking_lifetime.ToString() + " units minute) as cur_dat" +
#endif
 " From " + prm_name_n + 
                      " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest + "norm_prm_serv nps on nps.nzp_prm = n.nzp_prm, " + 
                      prm_N_p +
                      " LEFT OUTER JOIN " + Points.Pref + sDataAliasRest + "users u on P .user_block = u.nzp_user " +
                      " Where p.nzp_prm=n.nzp_prm and p.nzp = " + (finder.prm_num != 10 ? finder.nzp : 0) +
                      " and " + Utils.EStrNull(dat.ToShortDateString()) + " between p.dat_s and p.dat_po" +
                      " and p.is_actual <> 100 and p.user_block = u.nzp_user ";
#if PG
                sql += rows;
#endif
            }
            else
            {
                sql = "Select " +
#if PG
#else
                rows +
#endif
                    " val_prm, n.type_prm, n.name_prm, nzp_res, p.dat_block, p.dat_s,p.dat_po," +
                      " p.nzp, p.nzp_prm, p.user_block, u.comment as user_name_block," +
#if PG
                    " (now () - interval " + "'" + Constants.blocking_lifetime.ToString() + " minutes'" +
                      "   minute) as cur_dat" +
#else
                         " (current year to second - " + Constants.blocking_lifetime.ToString() + " units minute) as cur_dat" +
#endif
                    " From " + prm_name_n + ", " + prm_N_p +
                      " LEFT OUTER JOIN " + Points.Pref + sDataAliasRest + "users u   on P .user_block = u.nzp_user   " +
                      " Where p.nzp_prm=n.nzp_prm and p.nzp = " + (finder.prm_num != 10 ? finder.nzp : 0) +
                      " and " + Utils.EStrNull(dat.ToShortDateString()) + " between p.dat_s and p.dat_po" +
                      " and p.is_actual <> 100 and p.user_block = u.nzp_user ";
#if PG
                sql += rows;
#endif
            }

            IDataReader reader;
            IDataReader reader2;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return null;
            Prm prm;
            List<Prm> plist = new List<Prm>();
            while (reader.Read())
            {
                prm = new Prm();

                if (reader["nzp"] != DBNull.Value) prm.nzp = Convert.ToInt32(reader["nzp"]);
                if (reader["nzp_prm"] != DBNull.Value) prm.nzp_prm = Convert.ToInt32(reader["nzp_prm"]);
                if (reader["val_prm"] != DBNull.Value) prm.val_prm = ((string)reader["val_prm"]).Trim();
                if (reader["type_prm"] != DBNull.Value) prm.type_prm = ((string)reader["type_prm"]).Trim();
                if (reader["dat_s"] != DBNull.Value) prm.dat_s = System.Convert.ToDateTime(reader["dat_s"]).ToShortDateString();
                if (reader["dat_po"] != DBNull.Value) prm.dat_po = System.Convert.ToDateTime(reader["dat_s"]).ToShortDateString();
                if (reader["type_prm"] != DBNull.Value) prm.type_prm = ((string)reader["type_prm"]).Trim();
                if (reader["nzp_res"] != DBNull.Value) prm.nzp_res = Convert.ToInt32(reader["nzp_res"]);
                if (prm.type_prm == "bool")
                {
                    if (prm.val_prm == "1") prm.val_prm = "да";
                    else prm.val_prm = "нет";
                }
                else if (prm.type_prm == "sprav")
                {
                    int valPrm;
                    if (Int32.TryParse(prm.val_prm, out valPrm))
                    {


#if PG
                        sql = "Select max(r.name_y) as val From " + finder.pref + "_kernel.res_y r" +
#else
                        sql = "Select max(r.name_y) as val From " + finder.pref + "_kernel:res_y r" +
#endif
 " Where r.nzp_res=" + prm.nzp_res + " and r.nzp_y = " + valPrm;
                        ret = ExecRead(conn_db, out reader2, sql, true);
                        if (!ret.result)
                        {
                            return null;
                        }
                        if (!reader2.Read())
                        {
                            ret = new Returns(false, "Справочное значение параметра не найдено", -1);
                            return null;
                        }
                        if (reader2["val"] != DBNull.Value)
                            prm.val_prm = ((string)reader2["val"]).Trim();
                        else
                        {
                            reader2.Close();
                            ret = new Returns(false, "Справочное значение параметра имеет пустой значение", -1);
                            return null;
                        }
                    }
                    else
                    {
                        reader.Close();

                        ret = new Returns(false, "Значение параметра имеет неверный формат", -1);
                        return null;
                    }
                }
                plist.Add(prm);
            }

            reader.Close();
            return plist;
        }

        void FindPrmValue2(IDbConnection conn_db, IDataReader reader, ref Prm prm, out Returns ret)
        {
            ret = new Returns(true);
            if (reader["val_prm"] != DBNull.Value) prm.val_prm = ((string)reader["val_prm"]).Trim();
            if (reader["type_prm"] != DBNull.Value) prm.type_prm = ((string)reader["type_prm"]).Trim();
            if (reader["nzp_res"] != DBNull.Value) prm.nzp_res = Convert.ToInt32(reader["nzp_res"]);
            if (prm.type_prm == "bool")
            {
                if (prm.val_prm == "1") prm.val_prm = "да";
                else prm.val_prm = "нет";
            }
            else if (prm.type_prm == "sprav")
            {
                int valPrm;
                if (Int32.TryParse(prm.val_prm, out valPrm))
                {
#if PG
                    string sql = "Select max(r.name_y) as val From " + prm.pref + "_kernel.res_y r" +
#else
                    string sql = "Select max(r.name_y) as val From " + prm.pref + "_kernel:res_y r" +
#endif
 " Where r.nzp_res=" + prm.nzp_res + " and r.nzp_y = " + valPrm;

                    IDataReader reader2;
                    ret = ExecRead(conn_db, out reader2, sql, true);
                    if (!ret.result) return;
                    if (!reader2.Read())
                    {
                        ret = new Returns(false, "Справочное значение параметра не найдено", -1);
                        return;
                    }
                    if (reader2["val"] != DBNull.Value)
                        prm.val_prm = ((string)reader2["val"]).Trim();
                    else
                    {
                        reader2.Close();
                        ret = new Returns(false, "Справочное значение параметра имеет пустой значение", -1);
                    }
                    reader2.Close();
                }
                else ret = new Returns(false, "Значение параметра имеет неверный формат", -1);
            }
        }

        public Prm FindPrmValue(Prm finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }
            if (finder.pref.Trim() == "")
            {
                ret.result = false;
                ret.text = "Не определен банк данных";
                return null;
            }

            #region соединение с БД
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion

            if (finder.month_ < 1 || finder.year_ < 1)
            {
                finder.month_ = Points.CalcMonth.month_;
                finder.year_ = Points.CalcMonth.year_;
            }
            return FindPrmValue(conn_db, finder, out ret);
        }


        public List<Prm> FindPrmValues(Prm finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }
            if (finder.pref.Trim() == "")
            {
                ret.result = false;
                ret.text = "Не определен банк данных";
                return null;
            }

            #region соединение с БД
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion

            if (finder.month_ < 1 || finder.year_ < 1)
            {
                finder.month_ = Points.CalcMonth.month_;
                finder.year_ = Points.CalcMonth.year_;
            }
            return FindPrmValues(conn_db, finder, out ret);
        }

        public List<Param> LoadParamsWithNumer(Prm finder, out Returns ret)
        {
            #region соединение с Kernel
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion

            MyDataReader reader = null;
            MyDataReader reader2 = null;

            if (finder.pref == "") finder.pref = Points.Pref;

            string nzpprms = "";
            string sql;
            List<int> prm_nums = new List<int>();
            List<Param> list = new List<Param>();

            try
            {
                if (finder.nzp_reg > 0)
                {
                    sql = "select nzp_prm from " + Points.Pref + "_kernel" + tableDelimiter + "s_reg_prm where is_show = 1 and nzp_reg = " + finder.nzp_reg + " order by numer";
                    ret = ExecRead(conn_db, out reader, sql, true);

                    while (reader.Read())
                    {
                        if (reader["nzp_prm"] != DBNull.Value)
                        {
                            if (nzpprms == "") nzpprms += Convert.ToString(reader["nzp_prm"]);
                            else nzpprms += "," + Convert.ToString(reader["nzp_prm"]);
                        }
                    }
                }

                string where = "";
                if (finder.prm_num == 1) where += " and n.prm_num in (1,3,18)";
                else if (finder.prm_num > 0)
                {
                    where += " and n.prm_num = " + finder.prm_num;
                }

                if (finder.spis_prm != "") where += " and n.nzp_prm in (" + finder.spis_prm + ")";

                if (nzpprms != "") where += " and n.nzp_prm in (" + nzpprms + ")";

                if (finder.RolesVal != null)
                    foreach (_RolesVal role in finder.RolesVal)
                        if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_prm) where += " and n.nzp_prm in (" + role.val + ")";

                sql = "Select nzp_prm,prm_num From " + Points.Pref + "_kernel" + tableDelimiter + "prm_name n where 1=1  " + where;

                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    return null;
                }

                string nzp_prms = "";

                int i = 0;
                while (reader.Read())
                {
                    if (reader["prm_num"] != DBNull.Value)
                    {
                        bool key = false;
                        for (int j = 0; j < prm_nums.Count; j++)
                        {
                            if (prm_nums[j] == (int)reader["prm_num"])
                            {
                                key = true;
                                break;
                            }
                        }
                        if (!key)
                        {
                            if (TempTableInWebCashe(conn_db, finder.pref + "_data" + tableDelimiter + "prm_" + (int)reader["prm_num"]))
                                prm_nums.Add((int)reader["prm_num"]);
                        }
                    }
                    if (reader["nzp_prm"] != DBNull.Value)
                    {
                        nzp_prms += (i > 0 ? "," : "") + Convert.ToString(reader["nzp_prm"]);
                    }
                    i++;
                }
                reader.Close();
                if (prm_nums.Count == 0 || nzp_prms == "")
                {
                    return new List<Param>();
                }

                #region определение локального пользователя
                int nzpUser = finder.nzp_user;

                /*DbWorkUser db = new DbWorkUser();
                int nzpUser = db.GetLocalUser(conn_db, finder, out ret);
                db.Close();
                if (!ret.result)
                {
                    return null;
                }*/
                #endregion

                ExecSQL(conn_db, "drop table tem_prm", false);

#if PG
                sql = "create temp table tem_prm (" +
                    " val_prm char(250)," +
                    " prm_num integer," +
                    " nzp_prm integer," +
                    " dat_block timestamp, " +
                    " user_block integer, " +
                    " nzp integer, " +
                    " user_name_block CHAR(100), " +
                    " cur_dat timestamp)";
#else
                sql = "create temp table tem_prm (" +
                    " val_prm char(250)," +
                    " prm_num integer," +
                    " nzp_prm integer," +
                    " dat_block datetime year to minute, " +
                    " user_block integer, " +
                    " nzp integer, " +
                    " user_name_block CHAR(100), " +
                    " cur_dat datetime year to minute) with no log";
#endif
                ExecSQL(conn_db, sql, false);

                DateTime dat = new DateTime(finder.year_, finder.month_, 1);
                for (int k = 0; k < prm_nums.Count; k++)
                {
                    if (!TempTableInWebCashe(conn_db, finder.pref + "_data" + tableDelimiter + "prm_" + prm_nums[k])) continue;
#if PG
                    var sqlBuilder =
                        new StringBuilder(" Insert into tem_prm (val_prm, prm_num, nzp_prm, dat_block, user_block, nzp, user_name_block, cur_dat)")
                            .AppendFormat(" Select p.val_prm,{0}, p.nzp_prm, p.dat_block, p.user_block, p.nzp, u.comment, now() - INTERVAL '{1} minutes' ", prm_nums[k], Constants.blocking_lifetime)
                            .AppendFormat(" From {0}_kernel.prm_name n left outer join {1}_data.prm_{2} p ", Points.Pref, finder.pref, prm_nums[k])
                            .AppendFormat(" left outer join {0}_data.users u on p.user_block = u.nzp_user", Points.Pref)
                            .AppendFormat(" on p.nzp_prm = n.nzp_prm and {0} between p.dat_s and p.dat_po", Utils.EStrNull(dat.ToShortDateString()))
                            .AppendFormat(" and p.is_actual <> 100 and p.nzp = {0}", finder.nzp)
                            .AppendFormat(" where n.nzp_prm in ({0})", nzp_prms);

                    sql = sqlBuilder.ToString();
#else
                    sql = "Insert into tem_prm (val_prm, prm_num, nzp_prm, dat_block, user_block, nzp, user_name_block, cur_dat)" +
                        " Select p.val_prm," + prm_nums[k] + ", p.nzp_prm, p.dat_block, p.user_block, p.nzp, u.comment, current year to second - " + Constants.blocking_lifetime + " units minute " +
                        " From " + finder.pref + "_data:prm_" + prm_nums[k] + " p " + ", outer " + Points.Pref + "_data:users u " +
                        " Where p.nzp_prm in (" + nzp_prms + ") and " + Utils.EStrNull(dat.ToShortDateString()) + " between p.dat_s and p.dat_po" +
                        " and p.is_actual <> 100 and p.user_block = u.nzp_user and nzp = " + finder.nzp;
#endif
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        return null;
                    }
                }

                string ix = "ix" + finder.nzp_user + "_tem_prm";
                ret = ExecSQL(conn_db, " Create index " + ix + "_1 on tem_prm (nzp_prm) ", false);

                if (ret.result) ret = ExecSQL(conn_db, sUpdStat + " tem_prm", true);

                if (Points.isNewNorms)
                {
                    sql =
                        " Select n.nzp_prm, n.name_prm, " +
                        " (CASE WHEN n.type_prm = 'sprav' AND " + sNvlWord + "(nps.nzp_prm, 0) > 0 THEN 'norm' else n.type_prm END) as type_prm, " +
                        " n.nzp_res, n.prm_num, n.low_, n.high_, n.digits_, n.old_field " +
                        ", t.val_prm, t.dat_block, t.user_block, t.user_name_block, t.cur_dat, t.nzp" +
                        (finder.nzp_reg > 0 ? ", sreg.numer" : ",0 as numer") +
                        " From " + (finder.nzp_reg > 0 ? Points.Pref + sKernelAliasRest + "s_reg_prm sreg, " : "") +
                        Points.Pref + sKernelAliasRest + "prm_name n " +
                        " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest + "norm_prm_serv nps on nps.nzp_prm = n.nzp_prm" +
                        " left outer join tem_prm t on t.nzp_prm = n.nzp_prm " +
                        (finder.nzp > 0 ? " and t.nzp = " + finder.nzp : "") +
                        " Where 1=1 " + where +
                        (finder.nzp_reg > 0
                        ? " and sreg.nzp_prm = n.nzp_prm and sreg.nzp_reg = " + finder.nzp_reg
                        : "") +
                        (finder.nzp_reg > 0 ? " Order by sreg.numer" : " Order by n.name_prm ");
                }
                else
                {
                    sql =
                        " Select n.nzp_prm, n.name_prm, n.type_prm, n.nzp_res, n.prm_num, n.low_, n.high_, n.digits_, n.old_field " +
                        ", t.val_prm, t.dat_block, t.user_block, t.user_name_block, t.cur_dat, t.nzp" +
                        (finder.nzp_reg > 0 ? ", sreg.numer" : ",0 as numer") +
                        " From " + (finder.nzp_reg > 0 ? Points.Pref + sKernelAliasRest + "s_reg_prm sreg, " : "") +
                        Points.Pref + sKernelAliasRest + "prm_name n " +
                        " left outer join tem_prm t on t.nzp_prm = n.nzp_prm " +
                        (finder.nzp > 0 ? " and t.nzp = " + finder.nzp : "") +
                        " Where 1=1 " + where +
                        (finder.nzp_reg > 0
                        ? " and sreg.nzp_prm = n.nzp_prm and sreg.nzp_reg = " + finder.nzp_reg
                        : "") +
                        (finder.nzp_reg > 0 ? " Order by sreg.numer" : " Order by n.name_prm ");
                }

                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    return null;
                }

                string prm_N = "";

                Param param;
                while (reader.Read())
                {
                    #region Проверка блокировки записей
                    string bl = "";
                    DateTime dt_block = DateTime.MinValue;
                    DateTime dt_cur = DateTime.MinValue;
                    int user_block = 0;
                    string userNameBlock = "";
                    int prm_num = 0, nzp_prm = 0, nzp = 0;

                    if (reader["user_block"] != DBNull.Value) user_block = (int)reader["user_block"]; //пользователь, который заблокировал
                    if (reader["user_name_block"] != DBNull.Value) userNameBlock = ((string)reader["user_name_block"]).Trim(); //имя пользователь, который заблокировал
                    if (reader["dat_block"] != DBNull.Value) dt_block = Convert.ToDateTime(reader["dat_block"]);//дата блокировки
                    if (reader["cur_dat"] != DBNull.Value) dt_cur = Convert.ToDateTime(reader["cur_dat"]);//текущее время/дата - XX мин
                    if (reader["prm_num"] != DBNull.Value) prm_num = (int)reader["prm_num"]; prm_N = "prm_" + prm_num.ToString();
                    if (reader["nzp_prm"] != DBNull.Value) nzp_prm = (int)reader["nzp_prm"];
                    if (reader["nzp"] != DBNull.Value) nzp = (int)reader["nzp"];

                    if (user_block > 0 && dt_block != DateTime.MinValue) //заблокирован параметр
                        if (user_block != nzpUser && dt_cur < dt_block) //если заблокирована запись другим пользователем и XX мин не прошло
                            bl = "Параметр заблокирован пользователем " + userNameBlock + " " + dt_block.ToString("dd.MM.yyyy в HH:mm") + ". Редактировать данные запрещено.";

                    if (bl == "") // действующей блокировки нет, или она сделана самим пользователем
                    {
                        #region закомментировано
                        if (Utils.GetParams(finder.prms, Constants.act_mode_edit.ToString())) //если берут данные на изменение
                        {
#if PG
                            //
                            //ret = ExecSQL(conn_db, "update " + finder.pref + "_data." + prm_N + " set dat_block = now(), user_block = " + nzpUser + " where nzp_prm = " + nzp_prm + " and nzp = " + nzp, true);
#else
                            //ret = ExecSQL(conn_db, "update " + finder.pref + "_data:" + prm_N + " set dat_block = current year to minute, user_block = " + nzpUser + " where nzp_prm = " + nzp_prm + " and nzp = " + nzp, true);
#endif
                            if (!ret.result)
                            {
                                ret.result = false;
                                ret.text = "Ошибка обновления таблицы " + prm_N;
                                return null;
                            }
                        }
                        else //если  на просмотр
                        {
#if PG
                            //ret = ExecSQL(conn_db, "update " + finder.pref + "_data." + prm_N + " set dat_block = null, user_block = null where nzp_prm = " + nzp_prm.ToString() + " and nzp = " + nzp.ToString(), true);
#else
                            //ret = ExecSQL(conn_db, "update " + finder.pref + "_data:" + prm_N + " set dat_block = null, user_block = null where nzp_prm = " + nzp_prm.ToString() + " and nzp = " + nzp.ToString(), true);
#endif
                            if (!ret.result)
                            {
                                ret.result = false;
                                ret.text = "Ошибка обновления таблицы prm_" + prm_N;
                                return null;
                            }
                        }
                        #endregion
                    }
                    #endregion

                    param = new Param();
                    param.block = bl;
                    if (reader["nzp_prm"] != DBNull.Value) param.nzp_prm = (int)reader["nzp_prm"];
                    if (reader["name_prm"] != DBNull.Value) param.name_prm = ((string)reader["name_prm"]).Trim();
                    if (reader["prm_num"] != DBNull.Value) param.prm_num = Convert.ToInt32(reader["prm_num"]);
                    if (reader["numer"] != DBNull.Value) param.numer = Convert.ToInt32(reader["numer"]);
                    if (reader["type_prm"] != DBNull.Value) param.type_prm = Convert.ToString(reader["type_prm"]);
                    if (reader["nzp_res"] != DBNull.Value) param.nzp_res = Convert.ToInt32(reader["nzp_res"]);
                    if (reader["low_"] != DBNull.Value) param.low_ = Convert.ToDecimal(reader["low_"]);
                    if (reader["high_"] != DBNull.Value) param.high_ = Convert.ToDecimal(reader["high_"]);
                    if (reader["digits_"] != DBNull.Value) param.digits_ = (int)reader["digits_"];

                    if (reader["val_prm"] != DBNull.Value) param.val_prm = ((string)reader["val_prm"]).Trim().Replace("&quot;", "\"");
                    if (reader["old_field"] != DBNull.Value) param.old_field = ((string)reader["old_field"]).Trim();

                    if (param.old_field.Length > 0)
                    {
                        if (param.old_field[0] == '1') param.is_required = 1;
                        else param.is_required = 0;
                    }

                    if (param.type_prm == "bool")
                    {
                        if (param.val_prm == "1") param.val_prm = "Да";
                        else param.val_prm = "Нет";
                    }
                    else if (param.type_prm == "sprav")
                    {
                        sql = "select nzp_y, name_y from " + finder.pref + "_kernel" + tableDelimiter + "res_y where nzp_res = " + param.nzp_res + " order by name_y";
                        if (!ExecRead(conn_db, out reader2, sql, true).result)
                        {
                            return null;
                        }
                        Res_y val;
                        while (reader2.Read())
                        {
                            val = new Res_y();
                            if (reader2["nzp_y"] != DBNull.Value) val.nzp_y = (int)reader2["nzp_y"];
                            if (reader2["name_y"] != DBNull.Value) val.name_y = (string)reader2["name_y"];
                            param.values.Add(val);
                        }
                        reader2.Close();
                    }
                    else if (param.type_prm == "norm")
                    {
                        if (param.pref == "") param.pref = finder.pref;
                        DateTime datNormBetween = new DateTime(finder.year_, finder.month_, 1);
                        param.norm_sprav_values = LoadSpravValueFromNormativ(conn_db, param, out ret, datNormBetween);
                        if (!String.IsNullOrEmpty(param.val_prm))
                            param.val_prm_sprav = Convert.ToInt32(param.val_prm);
                        GetSelectedNormIdByVal(conn_db, finder, ref param, out ret);
                    }
                    else if (param.type_prm == "table")
                    {
                        sql = 
                            " select table_name, db_name, key_field, display_field" +
                            " from " + Points.Pref + sKernelAliasRest + "prm_table_descr" +
                            " where nzp_table = " + param.nzp_res;
                        ret = ExecRead(conn_db, out reader2, sql, true);
                        if (!ret.result)
                        {
                            return null;
                        }
                        string tableName, dbName, keyField, displayField;
                        if (reader2.Read())
                        {
                            tableName = Convert.ToString(reader2["table_name"]).Trim();
                            dbName = Convert.ToString(reader2["db_name"]).Trim();
                            keyField = Convert.ToString(reader2["key_field"]).Trim();
                            displayField = Convert.ToString(reader2["display_field"]).Trim();

                            reader2.Close();

                            sql = "select " + keyField + " as key_field, " + displayField + " as display_field from " + finder.pref + dbName + ":" + tableName + " order by " + displayField;
                            ret = ExecRead(conn_db, out reader2, sql, true);
                            if (!ret.result)
                            {
                                return null;
                            }
                            Res_y val;
                            while (reader2.Read())
                            {
                                val = new Res_y();
                                if (reader2["key_field"] != DBNull.Value) val.nzp_y = Convert.ToInt32(reader2["key_field"]);
                                if (reader2["display_field"] != DBNull.Value) val.name_y = (string)reader2["display_field"];
                                param.values.Add(val);
                            }
                            reader2.Close();
                        }
                        else
                            reader2.Close();
                    }
                    else if (param.type_prm == "char")
                    {
                        switch (param.prm_num)
                        {
                            case 1:
                            case 2:
                            case 4:
                            case 5:
                            case 6:
                            case 9:
                            case 11:
                            case 12:
                            case 13:
                            case 14:
                            case 15:
                            case 17:
                                param.high_ = 40;
                                break;
                            case 3:
                                param.high_ = 40;
                                break;
                            case 19:
                                param.high_ = 40;
                                break;
                            case 8:
                                param.high_ = 60;
                                break;
                            case 7:
                            case 10:
                                param.high_ = 100;
                                break;
                            case 18:
                                param.high_ = 250;
                                break;
                        }
                    }

                    list.Add(param);
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LoadParamsWithNumer\n " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                ExecSQL(conn_db, "drop table tem_prm", false);
                if (reader != null) reader.Close();
                if (reader2 != null) reader2.Close();
                conn_db.Close();
            }

            return list;
        }
        /// <summary>
        /// Получаем какой норматив соответствует данному значению
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <param name="param"></param>
        /// <param name="ret"></param>
        private void GetSelectedNormIdByVal(IDbConnection conn_db, Prm finder, ref Param param, out Returns ret)
        {
            ret = Utils.InitReturns();
            try
            {
                string sql;

                sql = " SELECT * FROM " + Points.Pref + sKernelAliasRest + "norm_prm_serv " +
                      " WHERE nzp_prm = " + param.nzp_prm;
                DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);
                if (dt.Rows.Count != 1) return;

                DateTime dat = new DateTime(finder.year_, finder.month_, 1);

                sql = " SELECT max(nt.id) as id" +
                      " FROM " + Points.Pref + sKernelAliasRest + "norm_types nt," +
                      Points.Pref + sKernelAliasRest + "norm_banks nb" +
                      " WHERE nb.norm_type_id = nt.id AND nt.is_finished = true" +
                      " AND nt.nzp_serv = " + dt.Rows[0]["nzp_serv"] + " AND nt.nzp_measure  = " + dt.Rows[0]["nzp_measure"] +
                      " AND " +
                      " nb.nzp_wp = " +
                      " (SELECT nzp_wp FROM " + Points.Pref + sKernelAliasRest + "s_point " +
                      " WHERE bd_kernel = '" + finder.pref + "')" +
                      " AND '" + dat.ToShortDateString() + "' between nt.date_from and nt.date_to";
                dt = DBManager.ExecSQLToTable(conn_db, sql);
                
                object norm_type_id_obj = dt.Rows[0]["id"];
                int norm_type_id = 0;
                if (ret.result && norm_type_id_obj != null &&
                    Int32.TryParse(norm_type_id_obj.ToString(), out norm_type_id))
                    param.norm_type_id = norm_type_id;
                else return;
                
                sql = " SELECT DISTINCT ordering, name_prm_val" +
                      " FROM " + Points.Pref + sKernelAliasRest + "influence_params i," +
                      Points.Pref + sKernelAliasRest + "norm_tables nt" +
                      " WHERE i.nzp_prm=" + param.nzp_prm + " AND nt.id = i.norm_tables_id" +
                      " AND nt.norm_type_id = " + norm_type_id +
                      " ORDER BY ordering";
                DataTable dt_name_prm_val = DBManager.ExecSQLToTable(conn_db, sql);
                if (dt_name_prm_val.Rows.Count < 1)
                {
                    //В нормативе может быть одновременно на один параметр выбран и домовой, и лицевой параметр в одной строке. 
                    //В этом случае приоритет отдается параметру ЛС, и все записывается на параметр ЛС.
                    //Сейчас мы это учтем и вытащим нормативы для домового параметра по параметру ЛС

                    #region Вытаскиваем норматив на домовой параметр, у которого есть в паре лицевой параметр на этом нормативе


                    sql = " SELECT max(nt.id) as id" +
                          " FROM " + Points.Pref + sKernelAliasRest + "norm_types nt," +
                          Points.Pref + sKernelAliasRest + "norm_banks nb," +
                          Points.Pref + sKernelAliasRest + "norm_types_sign nts, " +
                          Points.Pref + sKernelAliasRest + "norm_prm_serv nps" +
                          " WHERE nb.norm_type_id = nt.id AND nt.is_finished = true" +
                          " AND nts.norm_type_id = nt.id" +
                          " AND nps.nzp_prm = nts.nzp_prm_ls AND nts.nzp_prm_house = " + param.nzp_prm +
                          " AND nt.nzp_serv = nps.nzp_serv AND nt.nzp_measure  = nps.nzp_measure AND " +
                          " nb.nzp_wp = " +
                          " (SELECT nzp_wp FROM " + Points.Pref + sKernelAliasRest + "s_point " +
                          " WHERE bd_kernel = '" + finder.pref + "')" +
                          " AND '" + dat.ToShortDateString() + "' between nt.date_from and nt.date_to";
                    dt = DBManager.ExecSQLToTable(conn_db, sql);

                    object norm_type_id_obj1 = dt.Rows[0]["id"];
                    int norm_type_id1 = 0;
                    if (ret.result && norm_type_id_obj1 != null &&
                        Int32.TryParse(norm_type_id_obj1.ToString(), out norm_type_id1))
                        param.norm_type_id = norm_type_id1;
                    else return;

                    sql = " SELECT DISTINCT i.ordering, i.name_prm_val" +
                          " FROM " + Points.Pref + sKernelAliasRest + "influence_params i," +
                          Points.Pref + sKernelAliasRest + "norm_types_sign nts, " +
                          Points.Pref + sKernelAliasRest + "norm_tables nt" +
                          " WHERE i.nzp_prm=nts.nzp_prm_ls AND nt.id = i.norm_tables_id" +
                          " AND nts.norm_type_id = nt.norm_type_id AND nts.nzp_prm_house = " + param.nzp_prm +
                          " AND nt.norm_type_id = " + norm_type_id1 +
                          " ORDER BY ordering";
                    DataTable dt_name_prm_val1 = DBManager.ExecSQLToTable(conn_db, sql);
                    List<Res_y> listResY1 = new List<Res_y>();
                    foreach (DataRow row in dt_name_prm_val1.Rows)
                    {
                        listResY1.Add(new Res_y { nzp_y = Convert.ToInt32(row["ordering"]), name_y = row["name_prm_val"].ToString() });
                    }
                    param.values = listResY1;
                    int val_prm1 = 0;
                    if (!String.IsNullOrEmpty(param.val_prm))
                        val_prm1 = Convert.ToInt32(param.val_prm);
                    if (val_prm1 > 0 && listResY1.Exists(x => x.nzp_y == val_prm1))
                        param.old_val_prm = listResY1.Where(x => x.nzp_y == val_prm1).Select(x => x.name_y).FirstOrDefault();


                    sql = " SELECT nt.name_type_norm, nt.date_from, nt.date_to" +
                          " FROM " + Points.Pref + sKernelAliasRest + "norm_types nt" +
                          " WHERE id = " + norm_type_id1;
                    dt = DBManager.ExecSQLToTable(conn_db, sql);
                    if (!String.IsNullOrEmpty(param.old_val_prm) && dt.Rows[0]["name_type_norm"].ToString() != null &&
                        !String.IsNullOrEmpty(dt.Rows[0]["name_type_norm"].ToString()))
                    {
                        param.old_val_prm = param.old_val_prm + "(" + dt.Rows[0]["name_type_norm"] + " " +
                            (dt.Rows[0]["date_from"] == null ? "" : dt.Rows[0]["date_from"].ToString().Substring(0, 10) + " - ") +
                            (dt.Rows[0]["date_to"] == null ? "" : dt.Rows[0]["date_to"].ToString().Substring(0, 10)) +
                            ")";
                    }
                    return;

                    #endregion
                }
                List<Res_y> listResY = new List<Res_y>();
                foreach (DataRow row in dt_name_prm_val.Rows)
                {
                    listResY.Add(new Res_y { nzp_y = Convert.ToInt32(row["ordering"]), name_y = row["name_prm_val"].ToString() });
                }
                param.values = listResY;
                int val_prm = 0;
                if(!String.IsNullOrEmpty(param.val_prm))
                    val_prm = Convert.ToInt32(param.val_prm);
                if (val_prm > 0 && listResY.Exists(x => x.nzp_y == val_prm))
                    param.old_val_prm = listResY.Where(x => x.nzp_y == val_prm).Select(x => x.name_y).FirstOrDefault();


                sql = " SELECT nt.name_type_norm, nt.date_from, nt.date_to" +
                      " FROM " + Points.Pref + sKernelAliasRest + "norm_types nt" +
                      " WHERE id = " + norm_type_id;
                dt = DBManager.ExecSQLToTable(conn_db, sql);
                if (!String.IsNullOrEmpty(param.old_val_prm) && dt.Rows[0]["name_type_norm"].ToString() != null &&
                    !String.IsNullOrEmpty(dt.Rows[0]["name_type_norm"].ToString()))
                {
                    param.old_val_prm = param.old_val_prm + "(" + dt.Rows[0]["name_type_norm"] + " " +
                        (dt.Rows[0]["date_from"] == null ? "" : dt.Rows[0]["date_from"].ToString().Substring(0, 10) + " - ") +
                        (dt.Rows[0]["date_to"] == null ? "" : dt.Rows[0]["date_to"].ToString().Substring(0, 10) ) +
                        ")";
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog(
                    "Ошибка " + System.Reflection.MethodBase.GetCurrentMethod().Name + "\n" + ex.Message + " " +
                    ex.StackTrace,
                    MonitorLog.typelog.Error, true);
            }
            ;
        }

        public List<Param> GetParamsByPrmNum(Prm finder, out Returns ret)
        {
            if (finder.prm_num < 1)
            {
                ret = new Returns(false, "Неверные входные параметры");
                return null;
            }
            if (!ParamNums.generalParams.Contains(finder.prm_num) && finder.nzp < 1)
            {
                ret = new Returns(false, "Не выбрана запись", -1);
                return null;
            }

            #region соединение с Kernel
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion

            MyDataReader reader = null;
            MyDataReader reader2 = null;

            if (finder.pref == "") finder.pref = Points.Pref;

            List<Param> list = new List<Param>();

            try
            {
                if (!TempTableInWebCashe(conn_db, finder.pref + "_data" + tableDelimiter + "prm_" + finder.prm_num))
                {
                    ret.text = "Таблица со значениям параметра не найдена";
                    return list;
                }

                #region Выбрать список параметров без значений
                DateTime dat = new DateTime(finder.year_, finder.month_, 1);
#if PG
                string someMinutesAgo = " now() - interval '" + Constants.blocking_lifetime + " minutes' ";
#else
                string someMinutesAgo = " current - " + Constants.blocking_lifetime + " units minute ";
#endif
                StringBuilder sqlBuilder = new StringBuilder();
                string join = "";
                if (Utils.GetParams(finder.prms, Constants.act_mode_edit) || Utils.GetParams(finder.prms, Constants.act_showallprm))
                {
                    join = " left outer join ";
                }
                else
                {
                    join = " inner join ";
                }

                if (Utils.GetParams(finder.prms, Constants.act_mode_edit)) //режим редактирования
                {
                    sqlBuilder.Append(" Select n.nzp_prm, n.name_prm, n.type_prm, n.nzp_res, n.prm_num, n.low_, n.high_, n.digits_");
                    sqlBuilder.Append(", p.nzp, p.val_prm, " + someMinutesAgo + "as cur_dat, p.dat_block, p.user_block, u.comment as user_name_block");
                    sqlBuilder.Append(" From " + finder.pref + "_kernel" + tableDelimiter + "prm_name n");
                    sqlBuilder.Append(join + finder.pref + "_data" + tableDelimiter + "prm_" + finder.prm_num + " p");
                    sqlBuilder.Append(" left outer join " + Points.Pref + "_data" + tableDelimiter + "users u on p.user_block = u.nzp_user");
                    sqlBuilder.Append(" on p.nzp_prm = n.nzp_prm and p.is_actual <> 100 and " + Utils.EStrNull(dat.ToShortDateString()) + " between p.dat_s and p.dat_po");
                    if (!ParamNums.generalParams.Contains(finder.prm_num)) sqlBuilder.Append(" and p.nzp = " + finder.nzp);
                }

                sqlBuilder.Append(" where n.prm_num = " + finder.prm_num);
                if (finder.prm_num == ParamNums.General10 && finder.pref == Points.Pref) sqlBuilder.Append(" and n.nzp_prm in (1131,1132,1133,1134,1135,1136,1137,1266,1273,1274)");
                if (finder.spis_prm != "") sqlBuilder.Append(" and n.nzp_prm in (" + finder.spis_prm + ")");

                if (finder.RolesVal != null)
                    foreach (_RolesVal role in finder.RolesVal)
                        if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_prm) sqlBuilder.Append(" and n.nzp_prm in (" + role.val + ")");

                sqlBuilder.Append(" Order by n.name_prm");

                ret = ExecRead(conn_db, out reader, sqlBuilder.ToString(), true);
                if (!ret.result) return null;
                #endregion

                #region определение локального пользователя
                int nzpUser = finder.nzp_user;
                /*
                DbWorkUser db = new DbWorkUser();
                int nzpUser = db.GetLocalUser(conn_db, finder, out ret);
                db.Close();
                if (!ret.result) return null;*/
                #endregion

                string prm_N = "prm_" + finder.prm_num;

                string sql;

                Param param;
                while (reader.Read())
                {
                    #region Проверка блокировки записей
                    string bl = "";
                    DateTime dt_block = DateTime.MinValue;
                    DateTime dt_cur = DateTime.MinValue;
                    int user_block = 0;
                    string userNameBlock = "";
                    int prm_num = 0, nzp_prm = 0, nzp = 0;

                    if (reader["user_block"] != DBNull.Value) user_block = (int)reader["user_block"]; //пользователь, который заблокировал
                    if (reader["user_name_block"] != DBNull.Value) userNameBlock = ((string)reader["user_name_block"]).Trim(); //имя пользователь, который заблокировал
                    if (reader["dat_block"] != DBNull.Value) dt_block = Convert.ToDateTime(reader["dat_block"]);//дата блокировки
                    if (reader["cur_dat"] != DBNull.Value) dt_cur = Convert.ToDateTime(reader["cur_dat"]);//текущее время/дата - XX мин
                    if (reader["prm_num"] != DBNull.Value) prm_num = (int)reader["prm_num"];
                    if (reader["nzp_prm"] != DBNull.Value) nzp_prm = (int)reader["nzp_prm"];
                    if (reader["nzp"] != DBNull.Value) nzp = (int)reader["nzp"];

                    if (user_block > 0 && dt_block != DateTime.MinValue) //заблокирован параметр
                        if (user_block != nzpUser && dt_cur < dt_block) //если заблокирована запись другим пользователем и XX мин не прошло
                            bl = "Параметр заблокирован пользователем " + userNameBlock + " " + dt_block.ToString("dd.MM.yyyy в HH:mm") + ". Редактировать данные запрещено.";

                    if (bl == "") // действующей блокировки нет, или она сделана самим пользователем
                    {
                        if (Utils.GetParams(finder.prms, Constants.act_mode_edit.ToString())) //если берут данные на изменение
                        {
                            ret = ExecSQL(conn_db, "update " + finder.pref + "_data" + tableDelimiter + prm_N + " set dat_block = " + sCurDateTime + ", user_block = " + nzpUser + " where nzp_prm = " + nzp_prm + " and nzp = " + nzp, true);
                            if (!ret.result)
                            {
                                ret.result = false;
                                ret.text = "Ошибка обновления таблицы " + prm_N;
                                return null;
                            }
                        }
                        else //если  на просмотр
                        {
                            ret = ExecSQL(conn_db, "update " + finder.pref + "_data" + tableDelimiter + prm_N + " set dat_block = null, user_block = null where nzp_prm = " + nzp_prm + " and nzp = " + nzp, true);
                            if (!ret.result)
                            {
                                ret.result = false;
                                ret.text = "Ошибка обновления таблицы prm_" + prm_N;
                                return null;
                            }
                        }
                    }
                    #endregion

                    param = new Param();
                    param.block = bl;
                    if (reader["nzp_prm"] != DBNull.Value) param.nzp_prm = (int)reader["nzp_prm"];
                    if (reader["name_prm"] != DBNull.Value) param.name_prm = ((string)reader["name_prm"]).Trim();
                    if (reader["prm_num"] != DBNull.Value) param.prm_num = Convert.ToInt32(reader["prm_num"]);
                    if (reader["type_prm"] != DBNull.Value) param.type_prm = Convert.ToString(reader["type_prm"]);
                    if (reader["nzp_res"] != DBNull.Value) param.nzp_res = Convert.ToInt32(reader["nzp_res"]);
                    if (reader["low_"] != DBNull.Value) param.low_ = Convert.ToDecimal(reader["low_"]);
                    if (reader["high_"] != DBNull.Value) param.high_ = Convert.ToDecimal(reader["high_"]);
                    if (reader["digits_"] != DBNull.Value) param.digits_ = (int)reader["digits_"];

                    if (reader["val_prm"] != DBNull.Value) param.val_prm = ((string)reader["val_prm"]).Trim().Replace("&quot;", "\"");

                    if (param.type_prm == "bool")
                    {
                        if (param.val_prm == "1") param.val_prm = "Да";
                        else param.val_prm = "Нет";
                    }
                    else if (param.type_prm == "sprav")
                    {
                        sql = "select nzp_y, name_y from " + finder.pref + "_kernel" + tableDelimiter + "res_y where nzp_res = " + param.nzp_res;
                        if (!ExecRead(conn_db, out reader2, sql, true).result)
                        {
                            return null;
                        }
                        Res_y val;
                        while (reader2.Read())
                        {
                            val = new Res_y();
                            if (reader2["nzp_y"] != DBNull.Value) val.nzp_y = (int)reader2["nzp_y"];
                            if (reader2["name_y"] != DBNull.Value) val.name_y = (string)reader2["name_y"];
                            param.values.Add(val);
                        }
                        reader2.Close();
                    }
                    else if (param.type_prm == "table")
                    {
                        sql = "select table_name, db_name, key_field, display_field" +
                            " from " + Points.Pref + "_kernel" + tableDelimiter + "prm_table_descr" +
                            " where nzp_table = " + param.nzp_res;
                        ret = ExecRead(conn_db, out reader2, sql, true);
                        if (!ret.result) return null;
                        string tableName, dbName, keyField, displayField;
                        if (reader2.Read())
                        {
                            tableName = Convert.ToString(reader2["table_name"]).Trim();
                            dbName = Convert.ToString(reader2["db_name"]).Trim();
                            keyField = Convert.ToString(reader2["key_field"]).Trim();
                            displayField = Convert.ToString(reader2["display_field"]).Trim();

                            reader2.Close();

                            sql = "select " + keyField + " as key_field, " + displayField + " as display_field from " + finder.pref + dbName + tableDelimiter + tableName + " order by " + displayField;
                            ret = ExecRead(conn_db, out reader2, sql, true);
                            if (!ret.result) return null;

                            Res_y val;
                            while (reader2.Read())
                            {
                                val = new Res_y();
                                if (reader2["key_field"] != DBNull.Value) val.nzp_y = Convert.ToInt32(reader2["key_field"]);
                                if (reader2["display_field"] != DBNull.Value) val.name_y = (string)reader2["display_field"];
                                param.values.Add(val);
                            }
                        }
                        reader2.Close();
                    }
                    else if (param.type_prm == "char")
                    {
                        switch (param.prm_num)
                        {
                            case 1:
                            case 2:
                            case 4:
                            case 5:
                            case 6:
                            case 9:
                            case 11:
                            case 12:
                            case 13:
                            case 14:
                            case 15:
                            case 17:
                                param.high_ = 60;
                                break;
                            case 3:
                                param.high_ = 40;
                                break;
                            case 19:
                                param.high_ = 40;
                                break;
                            case 8:
                                param.high_ = 60;
                                break;
                            case 7:
                            case 10:
                                param.high_ = 100;
                                break;
                            case 18:
                                param.high_ = 250;
                                break;
                        }
                    }

                    list.Add(param);
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                MonitorLog.WriteLog("Ошибка LoadParamsWithNumer\n " + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
                if (reader2 != null) reader2.Close();
                conn_db.Close();
            }

            return list;
        }

        [Obsolete("Не использовать. Для архива")]
        public List<Prm> FindPrmTarifOld(Prm finder, out Returns ret)
        {
            #region проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (!Utils.GetParams(finder.prms, Constants.act_groupby_service.ToString()))
            {
                if (finder.pref == "")
                {
                    ret = new Returns(false, "Не определен префикс базы данных");
                    return null;
                }
                if (finder.year_ == 0 || finder.month_ == 0)
                {
                    ret = new Returns(false, "Не задан расчетный месяц");
                    return null;
                }
            }
            #endregion

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            /*if (!TableInWebCashe(conn_db, "prm_tarifs"))
            {
                ret = new Returns(false, "Отсутствует таблица prm_tarifs");
                conn_db.Close();
                return null;
            }*/

            string where = "";

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_serv) where += " and t.nzp_serv in (" + role.val + ")";
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_serv) where += " and t.nzp_prm in (" + role.val + ")";
                }

            if (finder.nzp_serv > 0) where += " and t.nzp_serv = " + finder.nzp_serv;
            if (finder.nzp_prm > 0) where += " and t.nzp_prm = " + finder.nzp_prm;

            string sql = "";
            int total_record_count = 0;

            if (Utils.GetParams(finder.prms, Constants.act_groupby_service.ToString()))
            {

#if PG
                sql = "Select distinct t.nzp_serv, s.service" +
                    " From " + Points.Pref + "_kernel.prm_tarifs t" +
                        ", " + Points.Pref + "_kernel.prm_name p" +
                        ", " + Points.Pref + "_kernel.services s" +
                    " Where p.prm_num=5 and t.nzp_prm = p.nzp_prm and t.nzp_serv = s.nzp_serv" + where +
                    " Order by s.service";
#else
                sql = "Select unique t.nzp_serv, s.service, s.ordering " +
                    " From " + Points.Pref + "_kernel:prm_tarifs t" +
                        ", " + Points.Pref + "_kernel:prm_name p" +
                        ", " + Points.Pref + "_kernel:services s" +
                    " Where p.prm_num=5 and t.nzp_prm = p.nzp_prm and t.nzp_serv = s.nzp_serv" + where +
                    " Order by s.ordering";
#endif
            }
            else
            {

                #region Определяем количество записей
#if PG
                sql = "Select count(distinct p.nzp_prm ) " +                  //||coalesce(f.nzp_measure,0)
                    " From " + Points.Pref + "_kernel.prm_tarifs t" +
                        ", " + Points.Pref + "_kernel.prm_name p" +
                        "," + finder.pref + "_kernel.formuls f" +
                                    "," + finder.pref + "_kernel.s_measure m" +
                    " Where p.prm_num=5 and t.nzp_prm = p.nzp_prm and t.nzp_frm = f.nzp_frm and f.nzp_measure = m.nzp_measure" + where;
#else
                sql = "Select count(unique p.nzp_prm ) " +                  //||nvl(f.nzp_measure,0)
                    " From " + Points.Pref + "_kernel:prm_tarifs t" +
                        ", " + Points.Pref + "_kernel:prm_name p" +
                        ", outer (" + finder.pref + "_kernel:formuls f" +
                                    ", outer " + finder.pref + "_kernel:s_measure m)" +
                    " Where p.prm_num=5 and t.nzp_prm = p.nzp_prm and t.nzp_frm = f.nzp_frm and f.nzp_measure = m.nzp_measure" + where;

#endif

                object count = ExecScalar(conn_db, sql, out ret, true);
                if (ret.result)
                {
                    try
                    {
                        total_record_count = Convert.ToInt32(count);
                    }
                    catch (Exception ex)
                    {
                        ret.result = false;
                        ret.text = ex.Message;
                        conn_db.Close();
                        return null;
                    }
                }
                else
                {
                    conn_db.Close();
                    return null;
                }
                #endregion

#if PG
                sql = "Select distinct t.nzp_prm, p.name_prm, m.measure, p.prm_num" +
                    " From " + Points.Pref + "_kernel.prm_tarifs t" +
                        ", " + Points.Pref + "_kernel.prm_name p" +
                        "," + finder.pref + "_kernel.formuls f" +
                                    "," + finder.pref + "_kernel.s_measure m" +
                    " Where p.prm_num=5 and t.nzp_prm = p.nzp_prm and t.nzp_frm = f.nzp_frm and f.nzp_measure = m.nzp_measure" + where +
                    " Order by p.name_prm";
#else
                sql = "Select unique t.nzp_prm, p.name_prm, m.measure, p.prm_num" +
                    " From " + Points.Pref + "_kernel:prm_tarifs t" +
                        ", " + Points.Pref + "_kernel:prm_name p" +
                        ", outer (" + finder.pref + "_kernel:formuls f" +
                                    ", outer " + finder.pref + "_kernel:s_measure m)" +
                    " Where p.prm_num=5 and t.nzp_prm = p.nzp_prm and t.nzp_frm = f.nzp_frm and f.nzp_measure = m.nzp_measure" + where +
                    " Order by p.name_prm";

#endif
            }

            IDataReader reader = null;
            IDataReader reader2 = null;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            List<Prm> list = new List<Prm>();
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    Prm zap = new Prm();
                    if (Utils.GetParams(finder.prms, Constants.act_groupby_service.ToString()))
                    {
                        if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                        if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]).Trim();
                    }
                    else
                    {
                        if (reader["nzp_prm"] != DBNull.Value) zap.nzp_prm = Convert.ToInt32(reader["nzp_prm"]);
                        if (reader["name_prm"] != DBNull.Value) zap.name_prm = Convert.ToString(reader["name_prm"]).Trim();
                        if (reader["measure"] != DBNull.Value) zap.measure = Convert.ToString(reader["measure"]).Trim();
                        if (reader["prm_num"] != DBNull.Value) zap.prm_num = Convert.ToInt32(reader["prm_num"]);
                        zap.pref = finder.pref;

                        sql = "Select p.val_prm " +
#if PG
 " From " + Points.Pref + "_kernel.prm_name n, " + finder.pref + "_data.prm_" + zap.prm_num + " p" +
#else
 " From " + Points.Pref + "_kernel:prm_name n, " + finder.pref + "_data:prm_" + zap.prm_num + " p" +
#endif
 " Where p.nzp_prm = n.nzp_prm and p.nzp_prm = " + zap.nzp_prm +
                            " and p.is_actual <> 100" +
#if PG
 " and dat_s  <= public.mdy(" + finder.month_.ToString() + ",28," + finder.year_.ToString() + ")" +
                            " and dat_po >= public.mdy(" + finder.month_.ToString() + ", 1," + finder.year_.ToString() + ")";
#else
 " and dat_s  <= mdy(" + finder.month_.ToString() + ",28," + finder.year_.ToString() + ")" +
                            " and dat_po >= mdy(" + finder.month_.ToString() + ", 1," + finder.year_.ToString() + ")";
#endif
                        ret = ExecRead(conn_db, out reader2, sql, true);
                        if (!ret.result) throw new Exception(ret.text);
                        if (reader2.Read())
                        {
                            if (reader2["val_prm"] != DBNull.Value) zap.val_prm = Convert.ToString(reader2["val_prm"]).Trim();
                        }
                        else if (!Utils.GetParams(finder.prms, Constants.act_showallprm))
                        {
                            // действующего значения нет, и нет признака показывать все параметры
                            // поэтому данный параметр в результирующий список не включаем
                            reader2.Close();


                            continue;
                        }
                        reader2.Close();
                    }
                    zap.num = (++i).ToString();



                    if (finder.rows > 0 && i <= finder.skip + finder.rows && i > finder.skip)
                    {
                        list.Add(zap);
                    }


                }

                total_record_count = i;
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                if (reader2 != null) reader2.Close();
                conn_db.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка FindPrmTarif " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            reader.Close();
            conn_db.Close();

            ret.tag = total_record_count;

            return list;
        }

        /// <summary>
        /// Получить список формул для калькуляции
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<Prm> FindPrmCalculationFormuls(Prm finder, out Returns ret)
        {
            #region проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (!Utils.GetParams(finder.prms, Constants.act_groupby_service.ToString()))
            {
                if (finder.pref == "")
                {
                    ret = new Returns(false, "Не определен префикс базы данных");
                    return null;
                }
                if (finder.year_ == 0 || finder.month_ == 0)
                {
                    ret = new Returns(false, "Не задан расчетный месяц");
                    return null;
                }
            }
            #endregion

            #region Выборка данных
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            IDataReader reader = null;
            try
            {
                ret = OpenDb(conn_db, true);
                if (!ret.result) return null;

                StringBuilder sql = new StringBuilder();

                DateTime tarifMonth = new DateTime(finder.year_, finder.month_, 1);
                DateTime tarifMonthNext = tarifMonth.AddMonths(1);


                sql.Append(" select f.nzp_frm,f. name_frm, p.nzp_prm ");
                sql.Append(" from " + finder.pref + "_kernel: formuls_opis fo, ");
                sql.Append(" " + finder.pref + "_kernel:  formuls f, ");
                sql.Append(" " + finder.pref + "_data: prm_5 p ");
                sql.Append(" where fo. nzp_frm = f. nzp_frm ");
                sql.Append(" and p.nzp_prm =  nzp_prm_tarif_bd ");
                sql.Append(" and p.is_actual <> 100 ");
                sql.Append(" and p.dat_s < mdy(" + tarifMonthNext.Month + ",1," + tarifMonthNext.Year + ") and p.dat_po >= mdy(" + tarifMonth.Month + ",1," + tarifMonth.Year + ") ");
                sql.Append(" and  p.nzp_prm = " + finder.nzp_prm + " ");


                ret = ExecRead(conn_db, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    return null;
                }


                List<Prm> listPrm = new List<Prm>();
                while (reader.Read())
                {
                    Prm p = new Prm();

                    p.nzp_frm = reader["nzp_frm"] != DBNull.Value ? Convert.ToInt32(reader["nzp_frm"]) : 0;
                    p.name_frm = reader["name_frm"] != DBNull.Value ? Convert.ToString(reader["name_frm"]) : "";
                    listPrm.Add(p);
                }

                return listPrm;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка FindPrmCalculationFormuls " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                reader.Close();
                reader.Dispose();
                if (conn_db != null) conn_db.Close();
            }
            #endregion
        }


        /// <summary>
        /// Ошибка выборки калькуляции(по всем ук за выбранный период)
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns>список калькуляций по всем УК за выбранный период</returns>
        public List<Prm> FindPrmCalculation(Prm finder, out Returns ret)
        {
            #region проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (!Utils.GetParams(finder.prms, Constants.act_groupby_service.ToString()))
            {
                if (finder.pref == "")
                {
                    ret = new Returns(false, "Не определен префикс базы данных");
                    return null;
                }
                if (finder.year_ == 0 || finder.month_ == 0)
                {
                    ret = new Returns(false, "Не задан расчетный месяц");
                    return null;
                }
            }
            #endregion

            #region Выборка данных
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            IDataReader reader = null;
            try
            {
                ret = OpenDb(conn_db, true);
                if (!ret.result) return null;

                StringBuilder sql = new StringBuilder();

                DateTime tarifMonth = new DateTime(finder.year_, finder.month_, 1);
                DateTime tarifMonthNext = tarifMonth.AddMonths(1);


                sql.Append(" select t.nzp_tarif, t.dat_s, t.dat_po,t.sumt, t.nzp_area,a.area ");
                sql.Append(" from " + finder.pref + "_data: s_calc_trf t, ");
                sql.Append("      " + Points.Pref + "_data:s_area a ");
                sql.Append(" where t.nzp_area = a.nzp_area ");
                sql.Append(" and t.nzp_serv = 17  ");
                sql.Append(" and t.dat_s < mdy(" + tarifMonthNext.Month + ",1," + tarifMonthNext.Year + ") and t.dat_po >= mdy(" + tarifMonth.Month + ",1," + tarifMonth.Year + ") ");
                sql.Append(" and  t.nzp_prm_bd = " + finder.nzp_prm + " ");


                ret = ExecRead(conn_db, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    return null;
                }


                List<Prm> listPrm = new List<Prm>();
                while (reader.Read())
                {
                    Prm p = new Prm();

                    p.nzp_tarif = reader["nzp_tarif"] != DBNull.Value ? Convert.ToInt32(reader["nzp_tarif"]) : 0;
                    p.dat_s = reader["dat_s"] != DBNull.Value ? Convert.ToDateTime(reader["dat_s"]).ToShortDateString() : "";
                    p.dat_po = reader["dat_po"] != DBNull.Value ? Convert.ToDateTime(reader["dat_po"]).ToShortDateString() : "";
                    p.tarif = reader["sumt"] != DBNull.Value ? Convert.ToDecimal(reader["sumt"]) : 0;
                    p.nzp_area = reader["nzp_area"] != DBNull.Value ? Convert.ToInt32(reader["nzp_area"]) : 0;
                    p.area = reader["area"] != DBNull.Value ? Convert.ToString(reader["area"]) : "";

                    listPrm.Add(p);
                }

                return listPrm;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка FindPrmCalculation " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                reader.Close();
                reader.Dispose();
                if (conn_db != null) conn_db.Close();
            }
            #endregion
        }


        /// <summary>
        /// Возвращает раскладку по тарифам для тарифа по услуге Содержание жилья
        /// </summary>
        /// <param name="finder">параметры поиска</param>        
        /// <returns>список тарифов</returns>
        public List<Prm> FindPrmTarifCalculation(Prm finder, out Returns ret)
        {
            #region проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (!Utils.GetParams(finder.prms, Constants.act_groupby_service.ToString()))
            {
                if (finder.pref == "")
                {
                    ret = new Returns(false, "Не определен префикс базы данных");
                    return null;
                }
                //if (finder.year_ == 0 || finder.month_ == 0)
                //{
                //    ret = new Returns(false, "Не задан расчетный месяц");
                //    return null;
                //}
            }
            #endregion

            #region Выборка данных
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            IDataReader reader = null;
            try
            {
                ret = OpenDb(conn_db, true);
                if (!ret.result) return null;

                StringBuilder sql = new StringBuilder();

                // DateTime tarifMonth = new DateTime(finder.year_, finder.month_, 1);
                //DateTime tarifMonthNext = tarifMonth.AddMonths(1);

                //sql.Append(" select s.nzp_serv ");
                //sql.Append(" ,se.service ");
                //sql.Append(" , s.nzp_convbd*1000000+17*1000+a.ktr nzp_prm, v.val_prm ");
                //sql.Append(" ,v.dat_s,v.dat_po  ");
                //sql.Append(" ,a.dt,a.ktr,a.nzp_conv_db, a.kkst ");
                //sql.Append(" ,max(a.sum) tarif ");
                //sql.Append(" FROM " + finder.pref + "_data:arx9 a, ");
                //sql.Append("      " + finder.pref + "_data:s_calc_line s, ");
                //sql.Append("      " + finder.pref + "_data:prm_5 v ");
                //sql.Append("      ," + finder.pref + "_kernel: services se ");
                //sql.Append(" where s.nzp_convbd=a.nzp_conv_db  ");
                //sql.Append("       and s.nzp_serv = se.nzp_serv ");
                //sql.Append("       and a.kkst=s.kodin ");
                //sql.Append("       and a.sum>0.001 ");
                //sql.Append("       and v.nzp_prm=(s.nzp_convbd*1000000+17*1000+a.ktr) ");
                //sql.Append("       and v.is_actual<>100 ");
                //sql.Append("       and v.nzp_prm = " + finder.nzp_prm + " ");
                //sql.Append("       and a.dt = mdy(" + tarifMonth.Month + ",1," + tarifMonth.Year + ") ");
                //sql.Append("       and v.dat_s < mdy(" + tarifMonthNext.Month + ",1," + tarifMonthNext.Year + ") and v.dat_po >= mdy(" + tarifMonth.Month + ",1," + tarifMonth.Year + ") ");
                //sql.Append(" group by 1,2,3,4,5,6,7,8,9,10 ");
                //sql.Append(" order by 3,4 ");


                #region Страрая структура закомментирована
                //sql.Append(" select unique c.nzp_serv, s.service, c.tarif, c.nzp_prm_calc, c.nzp_area  ");
                //sql.Append(" from  " + Points.Pref + "_data:tarif_calculation c, ");
                //sql.Append(" " + finder.pref + "_data: prm_5 p, ");
                //sql.Append(" " + Points.Pref + "_kernel: services s ");
                //sql.Append(" where ");
                //sql.Append(" c.nzp_prm = p.nzp_prm ");
                //sql.Append(" and s.nzp_serv = c.nzp_serv ");
                //sql.Append(" and c.nzp_prm =  " + finder.nzp_prm + " ");
                //sql.Append(" and p.is_actual <> 100 ");
                ////sql.Append(" and p.dat_s = c.dat_s and p.dat_po = c.dat_po ");
                //sql.Append(" and c.dat_s <= mdy(" + tarifMonth.Month + ",1," + tarifMonth.Year + ")  and c.dat_po >= mdy(" + tarifMonth.Month + ",1," + tarifMonth.Year + ")   ");
                //sql.Append(" order by 2 ");
                #endregion


                sql.Append(" select t.nzp_tarif, l.nzp_trfl, l.nzp_serv, s.service, l.sump, t.nzp_area ");
                sql.Append(" from " + finder.pref + sDataAliasRest + "s_calc_trf t, ");
                sql.Append(" " + finder.pref + sDataAliasRest + "s_calc_trf_lnk l, ");
                sql.Append(" " + Points.Pref + sKernelAliasRest + "services s ");
                sql.Append(" where ");
                sql.Append(" t.nzp_tarif = l.nzp_tarif ");
                sql.Append(" and s.nzp_serv = l.nzp_serv ");
                sql.Append(" and l.nzp_tarif = " + finder.nzp_tarif);
                //sql.Append(" and t.nzp_serv = 17 ");
                //sql.Append(" and t.nzp_area = " + finder.nzp_area + " ");
                //sql.Append(" and t.dat_s < mdy(" + tarifMonthNext.Month + ",1," + tarifMonthNext.Year + ") and t.dat_po >= mdy(" + tarifMonth.Month + ",1," + tarifMonth.Year + ") ");
                //sql.Append(" and  t.nzp_prm_bd = " + finder.nzp_prm + " ");




                ret = ExecRead(conn_db, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    return null;
                }


                List<Prm> listPrm = new List<Prm>();
                while (reader.Read())
                {
                    Prm p = new Prm();

                    p.nzp_tarif = reader["nzp_tarif"] != DBNull.Value ? Convert.ToInt32(reader["nzp_tarif"]) : 0;
                    p.nzp_trfl = reader["nzp_trfl"] != DBNull.Value ? Convert.ToInt32(reader["nzp_trfl"]) : 0;
                    p.nzp_serv = reader["nzp_serv"] != DBNull.Value ? Convert.ToInt32(reader["nzp_serv"]) : 0;
                    p.service = reader["service"] != DBNull.Value ? Convert.ToString(reader["service"]) : "";
                    p.tarif = reader["sump"] != DBNull.Value ? Convert.ToDecimal(reader["sump"]) : 0;
                    p.nzp_area = reader["nzp_area"] != DBNull.Value ? Convert.ToInt32(reader["nzp_area"]) : 0;

                    //p.arx9_dt = reader["dt"] != DBNull.Value ? Convert.ToDateTime(reader["dt"]) : DateTime.MinValue;
                    //p.arx9_ktr = reader["ktr"] != DBNull.Value ? Convert.ToString(reader["ktr"]) : "";
                    //p.arx9_nzp_conv_db = reader["nzp_conv_db"] != DBNull.Value ? Convert.ToInt32(reader["nzp_conv_db"]) : 0;
                    //p.arx9_kkst = reader["kkst"] != DBNull.Value ? Convert.ToInt32(reader["kkst"]) : 0;

                    listPrm.Add(p);
                }

                return listPrm;
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка FindPrmTarifCalculation " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                reader.Close();
                reader.Dispose();
                if (conn_db != null) conn_db.Close();
            }
            #endregion
        }


        /// <summary>
        /// Обновить значения калькуляции
        /// </summary>
        /// <param name="finder">список параметров калькуляции</param>
        /// <param name="ret">результат</param>        
        public void UpdatePrmTarifCalculation(List<Prm> finder, out Returns ret)
        {
            #region проверка входных параметров
            if (finder.Count == 0)
            {
                ret = new Returns(false, "Необходимо выбрать параметры калькуляции для обновления");
                ret.tag = 3;
                return;
            }
            if (finder.Any(x => x.nzp_user < 1))
            {
                ret = new Returns(false, "Не определен пользователь");
                ret.tag = 3;
                return;
            }
            //if (finder.Any(x => !Utils.GetParams(x.prms, Constants.act_groupby_service.ToString())))
            //{
            //    if (finder.Any(x=>x.pref == ""))
            //    {
            //        ret = new Returns(false, "Не определен префикс базы данных");
            //        ret.tag = 3;
            //        return;
            //    }
            //    //if (finder.Any(x => x.year_ == 0 || x.month_ == 0))
            //    //{
            //    //    ret = new Returns(false, "Не задан расчетный месяц");
            //    //    return;
            //    //}
            //}
            #endregion

            #region Обновление данных
            string connectionString = Points.GetConnByPref(finder.First().pref);
            IDbConnection conn_db = GetConnection(connectionString);
            IDbTransaction t = null;
            try
            {
                ret = OpenDb(conn_db, true);
                if (!ret.result) return;

                StringBuilder sql = new StringBuilder();
                t = conn_db.BeginTransaction();
                foreach (Prm p in finder)
                {
                    sql.Append(" update " + p.pref + "_data:s_calc_trf_lnk");
                    sql.Append(" set sump = " + p.tarif + " ");
                    sql.Append(" where nzp_trfl =  " + p.nzp_trfl);

                    //sql.Append(" update  " + p.pref + "_data:arx9 ");
                    //sql.Append(" set sum = " + p.tarif + " ");
                    //sql.Append(" where dt = mdy(" + p.arx9_dt.Month + ",1," + p.arx9_dt.Year + ") ");
                    //sql.Append(" and ktr = " + p.arx9_ktr + " ");
                    //sql.Append(" and nzp_conv_db= " + p.arx9_nzp_conv_db + " ");
                    //sql.Append(" and kkst = " + p.arx9_kkst);

                    ret = ExecSQL(conn_db, t, sql.ToString(), true);
                    if (!ret.result)
                    {
                        return;
                    }
                    sql.Remove(0, sql.Length);
                }
                t.Commit();
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка UpdatePrmTarifCalculation " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (conn_db != null) conn_db.Close();
            }
            #endregion
        }

        /// <summary>
        /// Добавить новый заголовок калькуляции
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        public void AddPrmCalculation(List<Prm> finder, out Returns ret)
        {
            #region проверка входных параметров
            if (finder.FirstOrDefault() == null)
            {
                ret = new Returns(false, "Отсутсвуют данные для сохранения");
                return;
            }
            if (finder.Any(x => x.nzp_user < 1))
            {
                ret = new Returns(false, "Не определен пользователь");
                ret.tag = 3;
                return;
            }
            if (finder.Any(x => x.pref == ""))
            {
                ret = new Returns(false, "Не определен префикс базы данных");
                ret.tag = 3;
                return;
            }
            if (finder.Any(x => x.dat_s == "" || x.dat_po == ""))
            {
                ret = new Returns(false, "Не задан период");
                return;
            }
            #endregion

            #region Обновление данных
            string connectionString = Points.GetConnByPref(finder.First().pref);
            IDbConnection conn_db = GetConnection(connectionString);
            //IDataReader reader = null;
            Prm p = finder.First();
            try
            {
                ret = OpenDb(conn_db, true);
                if (!ret.result) return;

                StringBuilder sql = new StringBuilder();

                #region Проверка на перескующиеся периоды по выбранной УК

                DateTime dat_s, dat_po;
                DateTime.TryParse(p.dat_s, out dat_s);
                DateTime.TryParse(p.dat_po, out dat_po);

                sql.Append(" select count(*) ");
                sql.Append(" from " + p.pref + "_data: s_calc_trf t ");
                sql.Append(" where t.nzp_serv = 17 ");
                sql.Append(" and  t.dat_s <= mdy(" + dat_po.Month + "," + dat_po.Day + "," + dat_po.Year + ") and t.dat_po >= mdy(" + dat_s.Month + "," + dat_s.Day + "," + dat_s.Year + ") ");
                sql.Append(" and  t.nzp_prm_bd = " + p.nzp_prm + " ");
                sql.Append(" and  t.nzp_area = " + p.nzp_area + " ");
                int countRows = Convert.ToInt32(ExecScalar(conn_db, sql.ToString(), out ret, true));
                if (!ret.result)
                {
                    return;
                }
                if (countRows > 0)
                {
                    ret = new Returns(false, "Введенный период пересекается с существующим периодом по выбранной УК.");
                    ret.tag = 3;
                    return;
                }
                #endregion

                sql.Remove(0, sql.Length);
                sql.Append(" Insert into " + p.pref + "_data:s_calc_trf ");
                sql.Append(" (nzp_serv, nzp_area, nzp_frm, nzp_prm_ls, nzp_prm_dom, nzp_prm_bd, sumt, dat_s, dat_po) ");
                sql.Append(" VALUES (");
                sql.Append(" 17," + p.nzp_area + ", " + p.nzp_frm + ",0,0," + p.nzp_prm + "," + p.tarif + ",'" + p.dat_s + "', '" + p.dat_po + "'");
                sql.Append(")");

                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    return;
                }

                //#region ID Последняя запись
                //sql.Remove(0, sql.Length);

                //sql.Append("SELECT first 1 dbinfo('sqlca.sqlerrd1') as co from systables");
                //if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                //{
                //    ret.result = false;
                //    ret.text = " Ошибка получения первичного ключа из таблицы s_calc_trf при вставке записи";
                //    return;
                //}
                //if (reader.Read())
                //{
                //    ret.tag = Convert.ToInt32(reader["co"].ToString());
                //}
                //else
                //{
                //    ret.result = false;
                //    ret.text = " Ошибка получения первичного ключа из таблицы s_calc_trf при вставке записи";
                //    return;
                //}
                //reader.Close();
                //#endregion                                
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка AddPrmCalculation " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (conn_db != null) conn_db.Close();
            }
            #endregion
        }

        /// <summary>
        /// Добавить новую услугу в калькуляцию
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        public void AddPrmTarifCalculation(List<Prm> finder, out Returns ret)
        {
            #region проверка входных параметров
            if (finder.FirstOrDefault() == null)
            {
                ret = new Returns(false, "Сохраняемая услуга не выбрана");
                return;
            }
            if (finder.Any(x => x.nzp_user < 1))
            {
                ret = new Returns(false, "Не определен пользователь");
                ret.tag = 3;
                return;
            }
            if (finder.Any(x => x.pref == ""))
            {
                ret = new Returns(false, "Не определен префикс базы данных");
                ret.tag = 3;
                return;
            }
            //if (finder.Any(x => x.dat_s == "" || x.dat_po == ""))
            //{
            //    ret = new Returns(false, "Не задан период");
            //    return;
            //}
            #endregion

            #region Обновление данных
            string connectionString = Points.GetConnByPref(finder.First().pref);
            IDbConnection conn_db = GetConnection(connectionString);
            IDbTransaction t = null;
            Prm p = finder.First();
            try
            {
                ret = OpenDb(conn_db, true);
                if (!ret.result) return;

                StringBuilder sql = new StringBuilder();
                t = conn_db.BeginTransaction();

                sql.Append(" Insert into " + p.pref + "_data:s_calc_trf_lnk ");
                sql.Append(" (nzp_tarif, nzp_serv, sump) ");
                sql.Append(" VALUES (");
                sql.Append(p.nzp_tarif + ", " + p.nzp_serv + ", " + p.tarif);
                sql.Append(")");

                ret = ExecSQL(conn_db, t, sql.ToString(), true);
                if (!ret.result)
                {
                    return;
                }
                t.Commit();
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка AddPrmTarifCalculation " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (conn_db != null) conn_db.Close();
            }
            #endregion
        }

        /// <summary>
        /// Удаление заголовка раскладки
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        public void DelPrmCalculation(List<Prm> finder, out Returns ret)
        {
            #region проверка входных параметров
            if (finder.Count == 0)
            {
                ret = new Returns(false, "Необходимо выбрать заголовок для обновления");
                ret.tag = 3;
                return;
            }
            if (finder.Any(x => x.nzp_user < 1))
            {
                ret = new Returns(false, "Не определен пользователь");
                ret.tag = 3;
                return;
            }
            #endregion

            #region Обновление данных
            string connectionString = Points.GetConnByPref(finder.First().pref);
            IDbConnection conn_db = GetConnection(connectionString);
            IDbTransaction t = null;
            try
            {
                ret = OpenDb(conn_db, true);
                if (!ret.result) return;

                StringBuilder sql = new StringBuilder();
                t = conn_db.BeginTransaction();
                Prm p = finder.First();

                sql.Append(" delete from " + p.pref + "_data:s_calc_trf_lnk");
                sql.Append(" where nzp_tarif =" + p.nzp_tarif);

                ret = ExecSQL(conn_db, t, sql.ToString(), true);
                if (!ret.result)
                {
                    return;
                }

                sql.Remove(0, sql.Length);
                sql.Append(" delete from " + p.pref + "_data:s_calc_trf");
                sql.Append(" where nzp_tarif =" + p.nzp_tarif);

                ret = ExecSQL(conn_db, t, sql.ToString(), true);
                if (!ret.result)
                {
                    return;
                }

                t.Commit();
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка DelPrmCalculation " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (conn_db != null) conn_db.Close();
            }
            #endregion
        }

        /// <summary>
        /// Удаление раскладочной услуги из базы
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        public void DelPrmTarifCalculation(List<Prm> finder, out Returns ret)
        {
            #region проверка входных параметров
            if (finder.Count == 0)
            {
                ret = new Returns(false, "Необходимо выбрать параметры калькуляции для обновления");
                ret.tag = 3;
                return;
            }
            if (finder.Any(x => x.nzp_user < 1))
            {
                ret = new Returns(false, "Не определен пользователь");
                ret.tag = 3;
                return;
            }
            #endregion

            #region Обновление данных
            string connectionString = Points.GetConnByPref(finder.First().pref);
            IDbConnection conn_db = GetConnection(connectionString);
            IDbTransaction t = null;
            try
            {
                ret = OpenDb(conn_db, true);
                if (!ret.result) return;

                StringBuilder sql = new StringBuilder();
                t = conn_db.BeginTransaction();
                Prm p = finder.First();

                sql.Append(" delete from " + p.pref + "_data:s_calc_trf_lnk");
                sql.Append(" where nzp_trfl =" + p.nzp_trfl);

                ret = ExecSQL(conn_db, t, sql.ToString(), true);
                if (!ret.result)
                {
                    return;
                }


                t.Commit();
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка DelPrmTarifCalculation " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                if (conn_db != null) conn_db.Close();
            }
            #endregion
        }



        /// <summary>
        /// Возвращает информацию о параметре
        /// </summary>
        /// <param name="conn_db">Соединение с БД</param>
        /// <param name="finder">Объект с параметрами поиска</param>
        /// <param name="ret">Результат выполнения функции</param>
        /// <returns>Искомый параметр</returns>
        public Param FindParam(IDbConnection conn_db, Param finder, out Returns ret)
        {
            return FindParam(conn_db, null, finder, out ret);
        }

        public Param FindParam(IDbConnection conn_db, IDbTransaction transaction, Param finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder == null)
            {
                ret = new Returns(false, "Не заданы параметры поиска");
                return null;
            }
            if (finder.pref == "")
            {
                ret = new Returns(false, "Не задан префикс базы данных");
                return null;
            }
            if (finder.nzp_prm == 0)
            {
                ret = new Returns(false, "Не задан код параметра");
                return null;
            }
            #endregion

            ret = Utils.InitReturns();
            string sql;

            if (Points.isNewNorms)
            {
                sql =
                    " Select n.nzp_prm, n.name_prm, n.prm_num, n.numer," +
                    " (CASE WHEN n.type_prm = 'sprav' AND coalesce(nps.nzp_prm, 0) > 0 THEN 'norm' else n.type_prm END) as type_prm," +
                    " n.nzp_res, n.low_, n.high_, n.digits_, n.is_day_uchet " +
                    " From " + finder.pref + sKernelAliasRest + "prm_name n " +
                    " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest + "norm_prm_serv nps on nps.nzp_prm = n.nzp_prm" +
                    " Where n.nzp_prm = " + finder.nzp_prm +
                    " Order by n.numer";
            }
            else
            {
                sql =
                    " Select n.nzp_prm, n.name_prm, n.prm_num, n.numer, n.type_prm, n.nzp_res, n.low_, n.high_, n.digits_, n.is_day_uchet " +
                    " From " + finder.pref + sKernelAliasRest + "prm_name n" +
                    " Where n.nzp_prm = " + finder.nzp_prm +
                    " Order by n.numer";
            }

            IDataReader reader;
            IDataReader reader2 = null;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return null;

            Param param = null;

            try
            {
                if (reader.Read())
                {
                    param = new Param();
                    if (reader["nzp_prm"] != DBNull.Value) param.nzp_prm = (int)reader["nzp_prm"];
                    if (reader["name_prm"] != DBNull.Value) param.name_prm = ((string)reader["name_prm"]).Trim();
                    if (reader["prm_num"] != DBNull.Value) param.prm_num = Convert.ToInt32(reader["prm_num"]);
                    if (reader["numer"] != DBNull.Value) param.numer = Convert.ToInt32(reader["numer"]);
                    if (reader["type_prm"] != DBNull.Value) param.type_prm = Convert.ToString(reader["type_prm"]).Trim();
                    if (reader["nzp_res"] != DBNull.Value) param.nzp_res = Convert.ToInt32(reader["nzp_res"]);
                    if (reader["low_"] != DBNull.Value) param.low_ = Convert.ToDecimal(reader["low_"]);
                    if (reader["high_"] != DBNull.Value) param.high_ = Convert.ToDecimal(reader["high_"]);
                    if (reader["digits_"] != DBNull.Value) param.digits_ = (int)reader["digits_"];
                    if (reader["is_day_uchet"] != DBNull.Value) param.is_day_uchet = (int)reader["is_day_uchet"];

                    if (param.type_prm == "sprav")
                    {
#if PG
                        sql = "select nzp_y, name_y from " + finder.pref + "_kernel.res_y where nzp_res = " + param.nzp_res;
#else
                        sql = "select nzp_y, name_y from " + finder.pref + "_kernel:res_y where nzp_res = " + param.nzp_res;
#endif
                        ret = ExecRead(conn_db, out reader2, sql, true);
                        if (!ret.result)
                        {
                            reader.Close();
                            return null;
                        }
                        param.values = new List<Res_y>();
                        Res_y val;
                        while (reader2.Read())
                        {
                            val = new Res_y();
                            if (reader2["nzp_y"] != DBNull.Value) val.nzp_y = (int)reader2["nzp_y"];
                            if (reader2["name_y"] != DBNull.Value) val.name_y = (string)reader2["name_y"];
                            param.values.Add(val);
                        }
                        reader2.Close();
                    }
                    else if (param.type_prm == "norm")
                    {
                        if (param.pref == "") param.pref = finder.pref;
                        DateTime datNormBetween = Points.DateOper;
                        param.norm_sprav_values = LoadSpravValueFromNormativ(conn_db, param, out ret, datNormBetween);
                        if (!String.IsNullOrEmpty(param.val_prm))
                            param.val_prm_sprav = Convert.ToInt32(param.val_prm);
                        Prm finderPrm = new Prm { pref = finder.pref, month_ = Points.DateOper.Month, year_ = Points.DateOper.Year};
                        GetSelectedNormIdByVal(conn_db, finderPrm, ref param, out ret);
                    }
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                if (reader2 != null) reader2.Close();
                ret = new Returns(false, ex.Message);
                MonitorLog.WriteLog("Ошибка FindParam " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            return param;
        }

        public ReturnsObjectType<Param> FindParam(Param finder, IDbConnection IDbConnection)
        {
            Returns ret = Utils.InitReturns();
            Param param = FindParam(IDbConnection, finder, out ret);
            return new ReturnsObjectType<Param>(param, ret.result, ret.text, ret.tag);
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

            Param prm = new Param();
            prm.dat_s = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).ToShortDateString();
            prm.nzp_user = finder.nzp_user;
            prm.webLogin = finder.webLogin;
            prm.webUname = finder.webUname;
            prm.pref = finder.pref;
            prm.nzp = finder.nzp_kvar;
            prm.nzp_prm = 51;
            prm.val_prm = "3"; // неопределено
            prm.prm_num = 3;
            ret = SavePrm(prm);
            if (!ret.result) return ret;

            #region обновление данных в выбранном списке л/с
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spls";

            if (TableInWebCashe(conn_web, tXX_spls))
            {
                string sql = "delete from " + tXX_spls + " where nzp_kvar = " + finder.nzp_kvar + " and pref = '" + finder.pref + "'";
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


        public List<Prm> GetNorms(Prm prm, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Prm> resList = new List<Prm>();
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();
            con_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(con_db, true);

            if (!ret.result)
            {
                MonitorLog.WriteLog("GetNorms : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }

            sql.Remove(0, sql.Length);
#if PG
            sql.Append("Select a.nzp_prm, b.name_prm from " + prm.pref + "_kernel.s_sn_prm a, " + prm.pref + "_kernel.prm_name b Where a.nzp_prm = b.nzp_prm and a.type_val = 2 order by b.name_prm");
#else
            sql.Append("Select a.nzp_prm, b.name_prm from " + prm.pref + "_kernel:s_sn_prm a, " + prm.pref + "_kernel:prm_name b Where a.nzp_prm = b.nzp_prm and a.type_val = 2 order by b.name_prm");
#endif

            if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка при получения списка нормативов " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return null;
            }

            if (reader != null)
            {
                while (reader.Read())
                {
                    Prm temp_prm = new Prm();
                    if (reader["nzp_prm"] != DBNull.Value) temp_prm.nzp_prm = Convert.ToInt32(reader["nzp_prm"]);
                    if (reader["name_prm"] != DBNull.Value) temp_prm.name_prm = Convert.ToString(reader["name_prm"]);
                    resList.Add(temp_prm);
                }
            }
            return resList;
        }

        public List<Prm> GetPeriod(Prm prm, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<Prm> resList = new List<Prm>();
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();
            con_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(con_db, true);

            if (!ret.result)
            {
                MonitorLog.WriteLog("GetPeriod : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }

            //DbWorkUser db = new DbWorkUser();

            sql.Remove(0, sql.Length);
            sql.Append("select p.dat_s, " +
#if PG
 "(case when (select extract (year from p.dat_po) = 3000) then null else p.dat_po end) as dat_po, " +
                       "r.name_short, " +
                       "(case when  p.is_actual = 1 then 'действует' else 'не действует' end) as is_actual, " +
                       "u.comment, " +
                         "p.dat_when, p.dat_del, r.nzp_res, p.nzp_key from " + prm.pref + "_data.prm_13 p, " +
                       prm.pref + "_kernel.resolution r, " +
                       Points.Pref + "_data. users u " +
                        "where p.val_prm::int = r.nzp_res and p.nzp_prm = " + prm.nzp_prm + " and u.nzp_user = p.nzp_user " +
#else
                        "(case when year(p.dat_po) = 3000 then null else p.dat_po end) as dat_po, " +
                       "r.name_short, " +
                       "(case when  p.is_actual = 1 then 'действует' else 'не действует' end) as is_actual, " +
                       "u.comment, " +
                         "p.dat_when, p.dat_del, r.nzp_res, p.nzp_key from " + prm.pref + "_data:prm_13 p, " +
                       prm.pref + "_kernel:resolution r, " +
                       Points.Pref + "_data: users u " +
                        "where p.val_prm + 0 = r.nzp_res and p.nzp_prm = " + prm.nzp_prm + " and u.nzp_user = p.nzp_user " + 
#endif
 " order by p.is_actual,p.dat_s desc");

            if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка при получения данных периода нормативов " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return null;
            }

            if (reader != null)
            {
                while (reader.Read())
                {
                    Prm temp_prm = new Prm();
                    if (reader["dat_s"] != DBNull.Value) temp_prm.dat_s = Convert.ToString(reader["dat_s"]).Substring(0, Convert.ToString(reader["dat_s"]).Length - 8);
                    if (reader["dat_po"] != DBNull.Value) temp_prm.dat_po = Convert.ToString(reader["dat_po"]).Substring(0, Convert.ToString(reader["dat_po"]).Length - 8); ;
                    if (reader["name_short"] != DBNull.Value) temp_prm.name_prm = Convert.ToString(reader["name_short"]);
                    if (reader["is_actual"] != DBNull.Value) temp_prm.type_prm = Convert.ToString(reader["is_actual"]);
                    if (reader["comment"] != DBNull.Value) temp_prm.user_name = Convert.ToString(reader["comment"]);
                    if (reader["dat_when"] != DBNull.Value) temp_prm.dat_when = Convert.ToString(reader["dat_when"]).Substring(0, Convert.ToString(reader["dat_when"]).Length - 8); ;
                    if (reader["dat_del"] != DBNull.Value) temp_prm.dat_del = Convert.ToString(reader["dat_del"]).Substring(0, Convert.ToString(reader["dat_del"]).Length - 8); ;
                    if (reader["nzp_res"] != DBNull.Value) temp_prm.nzp_res = Convert.ToInt32(reader["nzp_res"]);
                    if (reader["nzp_key"] != DBNull.Value) temp_prm.nzp_key = Convert.ToInt32(reader["nzp_key"]);
                    resList.Add(temp_prm);
                }
            }
            return resList;
        }

        public List<Resolution> GetListResolution(Resolution finder, out Returns ret)
        {
            if (finder.nzp_user <= 0)
            {
                ret = new Returns(false, "Пользователь не определен", -1);
                return null;
            }

            IDbConnection conn_db = null;
            MyDataReader reader = null;
            conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("GetListResolution : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }

            if (finder.pref == "") finder.pref = Points.Pref;
            string where = "";
            if (finder.nzp_res > 0) where = " and nzp_res = " + finder.nzp_res;
            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("select * from {0}_kernel{1}resolution where 1=1 {2}", finder.pref, tableDelimiter, where);
            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            List<Resolution> list = new List<Resolution>();
            try
            {
                while (reader.Read())
                {
                    Resolution res = new Resolution();
                    if (reader["nzp_res"] != DBNull.Value) res.nzp_res = Convert.ToInt32(reader["nzp_res"]);
                    if (reader["name_short"] != DBNull.Value) res.name_short = Convert.ToString(reader["name_short"]);
                    if (reader["name_res"] != DBNull.Value) res.name_res = Convert.ToString(reader["name_res"]);
                    if (reader["is_readonly"] != DBNull.Value) res.is_readonly = Convert.ToInt32(reader["is_readonly"]);
                    list.Add(res);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при получении данных " + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (reader != null) reader.Close();
                conn_db.Close();

                if (list != null)
                {
                    ret.tag = list.Count;
                    if (finder.skip > 0 && list.Count > finder.skip) list.RemoveRange(0, finder.skip);
                    if (finder.rows > 0 && list.Count > finder.rows) list.RemoveRange(finder.rows, list.Count - finder.rows);
                }

            }
            return list;
        }

        public bool DeletePeriod(Prm prm, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;

            con_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(con_db, true);

            if (!ret.result)
            {
                MonitorLog.WriteLog("DeletePeriod : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return false;
            }

            try
            {
#if PG
                ret = ExecSQL(con_db, "update " + prm.pref + "_data.prm_13 set is_actual = 100 where nzp_key = " + prm.nzp_key, true);
#else
                ret = ExecSQL(con_db, "update " + prm.pref + "_data:prm_13 set is_actual = 100 where nzp_key = " + prm.nzp_key, true);
#endif
                return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при удалении периода действия норматива " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
        }

        public bool UpdateTableData(Prm prm, ArrayList list, int nzp_y, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection con_db = null;

            con_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(con_db, true);

            if (!ret.result)
            {
                MonitorLog.WriteLog("UpdateTableData : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return false;
            }

            try
            {
                for (int i = 1; i <= list.Count; i++)
                {
#if PG
                    ret = ExecSQL(con_db, "update " + prm.pref + "_kernel.res_values set value = " + list[i - 1] + " where nzp_res = " + prm.nzp_res + " and nzp_y " + nzp_y + " and nzp_x = " + i, true);
#else
                    ret = ExecSQL(con_db, "update " + prm.pref + "_kernel:res_values set value = " + list[i - 1] + " where nzp_res = " + prm.nzp_res + " and nzp_y " + nzp_y + " and nzp_x = " + i, true);
#endif
                }
                return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при изменении данных в таблице норматива " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
        }

        public DataTable GetTableData(Prm prm, out Returns ret)
        {
            DataTable Res_Table = new DataTable();
            Res_Table.TableName = "table";
            ret = Utils.InitReturns();
            List<Res_y> resList = new List<Res_y>();
            IDbConnection con_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();
            con_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(con_db, true);
            int col_count = 0;
            int row_count = 0;
            string sql_string = "";

            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("ru-RU");
            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;


            if (!ret.result)
            {
                MonitorLog.WriteLog("GetColNames : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return null;
            }

            int nzpUser = prm.nzp_user;

            /*DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(con_db, prm, out ret);*/

            sql.Remove(0, sql.Length);

#if PG
            sql.Append("select name_x from " + prm.pref + "_kernel.res_x where nzp_res = " + prm.nzp_res + " ORDER BY nzp_x ");
#else
            sql.Append("select name_x from " + prm.pref + "_kernel:res_x where nzp_res = " + prm.nzp_res);
#endif
            if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка при получения названий колонок " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return null;
            }

            if (reader != null)
            {
                DataColumn dc = new DataColumn("Строки\\Столбцы");
                Res_Table.Columns.Add(dc);
                while (reader.Read())
                {
                    col_count++;
                    string temp_col = "";
                    if (reader["name_x"] != DBNull.Value) temp_col = Convert.ToString(reader["name_x"]);
                    DataColumn dcl = new DataColumn(temp_col);
                    Res_Table.Columns.Add(dcl);

                }
            }

            sql.Remove(0, sql.Length);

#if PG
            sql.Append("select name_y from " + prm.pref + "_kernel.res_y where nzp_res = " + prm.nzp_res + " ORDER BY nzp_y ");
#else
            sql.Append("select name_y from " + prm.pref + "_kernel:res_y where nzp_res = " + prm.nzp_res);
#endif
            if (!ExecRead(con_db, out reader, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("Ошибка при получения названий строк " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return null;
            }

            ArrayList list = new ArrayList();
            if (reader != null)
            {
                while (reader.Read())
                {
                    row_count++;
                    string temp_row = "";
                    if (reader["name_y"] != DBNull.Value) temp_row = Convert.ToString(reader["name_y"]);
                    list.Add(temp_row);
                }
            }



            if (col_count != 0)
            {
                sql.Remove(0, sql.Length);
                sql_string = "select ";

                for (int k = 1; k <= col_count; k++)
                {
                    if (k != col_count)
                    {
                        sql_string += "r" + k.ToString() + ".value,";
                    }
                    else
                    {
                        sql_string += "r" + k.ToString() + ".value from ";
                    }

                }
                for (int l = 1; l <= col_count; l++)
                {
                    if (l != col_count)
                    {
#if PG
                        sql_string += prm.pref + "_kernel.res_values r" + l.ToString() + ",";
#else
                        sql_string += prm.pref + "_kernel: res_values r" + l.ToString() + ",";
#endif
                    }
                    else
                    {
#if PG
                        sql_string += prm.pref + "_kernel.res_values r" + l.ToString() + " where ";
#else
                        sql_string += prm.pref + "_kernel: res_values r" + l.ToString() + " where ";
#endif
                    }

                }
                for (int m = 1; m <= col_count; m++)
                {
                    sql_string += "r" + m.ToString() + ".nzp_res = " + prm.nzp_res + " and r" + m.ToString() + ".nzp_x = " + m.ToString() + " and ";
                }
                for (int n = 1; n <= col_count - 1; n++)
                {
                    if (n != col_count - 1)
                    {
                        sql_string += "r" + n.ToString() + ".nzp_y = " + "r" + (n + 1).ToString() + ".nzp_y and ";
                    }
                    else
                    {
                        sql_string += "r" + n.ToString() + ".nzp_y = " + "r" + (n + 1).ToString() + ".nzp_y";
                    }
                }

                //сортировка
                sql_string += " order by r1.nzp_y;  ";
            }
            else
            {
                MonitorLog.WriteLog("Ошибка при формировании запроса GetTableData() ", MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return null;
            }

            if (!ExecRead(con_db, out reader, sql_string, true).result)
            {
                MonitorLog.WriteLog("Ошибка при получения данных таблицы " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return null;
            }

            int i = 0;
            if (reader != null)
            {
                while (reader.Read())
                {
                    DataRow dr = Res_Table.NewRow();
                    dr[0] = list[i];

                    for (int g = 0; g < reader.FieldCount; g++)
                    {
                        if (reader != null)
                        {
                            dr[g + 1] = reader.GetString(g).Trim();
                        }
                        else
                        {
                            dr[g] = "";
                        }
                    }
                    Res_Table.Rows.Add(dr);
                    i++;
                }
            }
            return Res_Table;
        }

        public ReturnsObjectType<DataTable> GetKvarPrmList(Prm finder, IDbConnection IDbConnection)
        {
            #region Проверка входных параметров
            if (!(finder.nzp_user > 0))
                return new ReturnsObjectType<DataTable>(null, false, "Не задан пользователь", -1);
            if (finder.pref.Trim() == "")
                return new ReturnsObjectType<DataTable>(null, false, "Не задан банк данных", -1);
            else
                finder.pref = finder.pref.Trim();
            //if (!(finder.nzp_prm > 0))
            //    return new ReturnsObjectType<DataTable>(null, false, "Не задан параметр", -1);
            #endregion

            // Разобрать параметры подключение к базе web, получить имя БД web
            string baseWeb = DBManager.getDbName(Constants.cons_Webdata);
            string tableUserSpLsLocal = "t" + finder.nzp_user + "_spls";
            Returns ret;
#if PG
            string tableUserSpLs = "public." + tableUserSpLsLocal;
#else
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return new ReturnsObjectType<DataTable>(null, false, ret.text, -1);
            baseWeb += "@" + DBManager.getServer(conn_web);
            conn_web.Close();
            string tableUserSpLs = baseWeb + ":" + tableUserSpLsLocal;
#endif

            #region Определить локального пользователя
            Int32 nzpUser = finder.nzp_user;

            /*DbWorkUser db = new DbWorkUser();
            Int32 nzpUser = db.GetLocalUser(IDbConnection, finder, out ret);
            db.Close();
            if (!ret.result)
                return new ReturnsObjectType<DataTable>(null, false, ret.text, -1);*/
            #endregion

            string sqlText = "";
            DataTable dt = null;

            // Проверить наличие таблицы результатов поиска ЛС
#if PG
            sqlText = "select count(*) from information_schema.tables where table_name = '" + tableUserSpLsLocal.ToLower() + "' and table_schema = 'public'";
#else
            sqlText = " select count(*) from " + baseWeb + ":systables where lower(tabname) = '" + tableUserSpLsLocal.ToLower() + "'";
#endif
            dt = ClassDBUtils.OpenSQL(sqlText, IDbConnection).GetData();
            if (Convert.ToInt32(dt.Rows[0][0]) == 0)
                return new ReturnsObjectType<DataTable>(null, false, "Не выбран список лицевых счетов", -1);

            // Получить интервал отображаемых ЛС из результатов поиска пользователя
            sqlText = " drop table alx_tmp_show_ls ";
            ClassDBUtils.ExecSQL(sqlText, IDbConnection, ClassDBUtils.ExecMode.Log);
#if PG
            sqlText =
                string.Format(" select " +
                "     num_ls, nzp_kvar, adr " +
                " into temp alx_tmp_show_ls " +
                " from " + tableUserSpLs +
                " where lower(trim(pref)) = lower('" + finder.pref.Trim().ToLower() + "') " +
                " order by adr, num_ls offset {0} limit {1}", finder.skip, finder.rows);
#else
            sqlText =
                " select " +
                ((finder.skip > 0) ? " skip " + finder.skip : "") +
                ((finder.rows > 0) ? " first " + finder.rows : "") +
                "     num_ls, nzp_kvar, adr " +
                " from " + tableUserSpLs +
                " where lower(trim(pref)) = lower('" + finder.pref.Trim().ToLower() + "') " +
                " order by adr, num_ls " +
                " into temp alx_tmp_show_ls with no log ";

#endif
            ClassDBUtils.ExecSQL(sqlText, IDbConnection);
            sqlText = " create index ix1_alx_tmp_show_ls on alx_tmp_show_ls (num_ls) ";
            ClassDBUtils.ExecSQL(sqlText, IDbConnection);
            sqlText = " create index ix2_alx_tmp_show_ls on alx_tmp_show_ls (nzp_kvar) ";
            ClassDBUtils.ExecSQL(sqlText, IDbConnection);
#if PG
            sqlText = " analyze alx_tmp_show_ls ";
#else
            sqlText = " update statistics for table alx_tmp_show_ls ";
#endif
            ClassDBUtils.ExecSQL(sqlText, IDbConnection);
  // Получить список префиксов банков данных для ЛС из результатов поиска пользователя
            List<string> listPref = new List<string>();
            sqlText =
                " select distinct pref " +
                " from " + tableUserSpLs;
            dt = ClassDBUtils.OpenSQL(sqlText, IDbConnection).GetData();
            foreach (DataRow row in dt.Rows)
                listPref.Add(row["pref"].ToString().Trim());

            if (finder.nzp_prm > 0)
            {
                // Определить prm_num выбранного параметра
                sqlText =" select prm_num from " + Points.Pref + "_kernel"+tableDelimiter+"prm_name where nzp_prm = " + finder.nzp_prm;
                dt = ClassDBUtils.OpenSQL(sqlText, IDbConnection).GetData();
                if (dt.Rows.Count == 0) return new ReturnsObjectType<DataTable>(null, false, "Не задан параметр", -1);
                finder.prm_num = Convert.ToInt32(dt.Rows[0]["prm_num"]);

                #region Блокировки на список
                // Снять свои блокировки + чужие истекшие
                foreach (string pref in listPref)
                {
                    sqlText =
#if PG
                        " update " + pref + "_data.prm_" + finder.prm_num + " set " +
                        "   (user_block, dat_block) = (null, null) " +
                        " where nzp_prm = " + finder.nzp_prm +
                        " and (user_block = " + nzpUser +
                        "     OR user_block <> " + nzpUser + " and (coalesce(dat_block,public.mdy(1,1,1900)) < (now() - INTERVAL " + string.Format("'{0} minutes'))", Constants.blocking_lifetime) +
                        " ) " +
                        " and nzp in ( " +
                        "     select nzp_kvar from " + tableUserSpLs + // снять блокировки на всем списке (все режимы - просмотр, редактирование)
                        (Utils.GetParams(finder.prms, Constants.act_mode_edit)
                        ? "   where not num_ls in (select num_ls from alx_tmp_show_ls) " : "") + // в режиме редактирования - кроме текущей страницы
                        " ) ";
#else
 " update " + pref + "_data:prm_" + finder.prm_num + " set " +
                        "   (user_block, dat_block) = (null, null) " +
                        " where nzp_prm = " + finder.nzp_prm +
                        " and (user_block = " + nzpUser +
                        "     OR user_block <> " + nzpUser + " and (nvl(dat_block,mdy(1,1,1900)) < (current year to second - " + Constants.blocking_lifetime + " units minute)) " +
                        " ) " +
                        " and nzp in ( " +
                        "     select nzp_kvar from " + tableUserSpLs + // снять блокировки на всем списке (все режимы - просмотр, редактирование)
                        (Utils.GetParams(finder.prms, Constants.act_mode_edit)
                        ? "   where not num_ls in (select num_ls from alx_tmp_show_ls) " : "") + // в режиме редактирования - кроме текущей страницы
                        " ) ";

#endif
                    ClassDBUtils.ExecSQL(sqlText, IDbConnection);
                }

                // Установить блокировки на параметры текущей страницы списка ЛС
                if (Utils.GetParams(finder.prms, Constants.act_mode_edit)) // только в режиме редактирования
                {
                    foreach (string pref in listPref)
                    {
                        sqlText =
#if PG
                            " update " + pref + "_data.prm_" + finder.prm_num + " set " +
                            "   (user_block, dat_block) = (" + nzpUser + ", current) " +
                            " where nzp_prm = " + finder.nzp_prm +
                            " and nzp in ( select nzp_kvar from alx_tmp_show_ls ) " +
                            " and dat_s  <= public.mdy(" + Points.DateOper.ToString("MM,dd,yyyy") + ") " +
                            " and dat_po >= public.mdy(" + Points.DateOper.ToString("MM,dd,yyyy") + ") " +
                             " and is_actual <> 100 " +
                            " and (user_block is null OR user_block = " + nzpUser +
                            "     OR user_block <> " + nzpUser + " and (coalesce(dat_block,public.mdy(1,1,1900)) < (now() - INTERVAL " + string.Format("'{0} minutes')", Constants.blocking_lifetime) + " ) ";
#else
 " update " + pref + "_data:prm_" + finder.prm_num + " set " +
                            "   (user_block, dat_block) = (" + nzpUser + ", current) " +
                            " where nzp_prm = " + finder.nzp_prm +
                            " and nzp in ( select nzp_kvar from alx_tmp_show_ls ) " +
                            " and dat_s  <= mdy(" + Points.DateOper.ToString("MM,dd,yyyy") + ") " +
                            " and dat_po >= mdy(" + Points.DateOper.ToString("MM,dd,yyyy") + ") " +
                            " and is_actual <> 100 " +
                            " and (user_block is null OR user_block = " + nzpUser +
                            "     OR user_block <> " + nzpUser + " and (nvl(dat_block,mdy(1,1,1900)) < (current year to second - " + Constants.blocking_lifetime + " units minute)) " +
                            " ) ";

#endif
                        ClassDBUtils.ExecSQL(sqlText, IDbConnection);
                    }
                }
                #endregion
            }
            else
            {
                finder.prm_num = 0;
            }

            // Общее кол-во записей
            sqlText =
                " select count(*) from " + tableUserSpLs + " where lower(trim(pref)) = lower('" + finder.pref.Trim().ToLower() + "') ";
            dt = ClassDBUtils.OpenSQL(sqlText, IDbConnection).GetData();
            Int32 cntRowsTotal = Convert.ToInt32(dt.Rows[0][0]);
            RecordMonth r_m = Points.GetCalcMonth(new CalcMonthParams(finder.pref));
            DateTime calcMonth = new DateTime(r_m.year_, r_m.month_, 1);
            // Получить таблицу ЛС по результатам поиска пользователя
            sqlText =
                " select *, '' user_name, '' block, 0 num, '' old_val_prm " +
                " from " + tableUserSpLs + " ls " +
                " left outer join " + finder.pref + "_data.prm_" + ((finder.prm_num > 0) ? finder.prm_num : 1) + " p " +
                " inner join " + Points.Pref + "_kernel.prm_name n on p.nzp_prm=n.nzp_prm on  ls.nzp_kvar=p.nzp " +
                " and  p.nzp_prm = " + finder.nzp_prm + " and is_actual=1 and '"+calcMonth.ToShortDateString()+"' between dat_s and dat_po" +
                " where " +
                "  num_ls in (select num_ls from alx_tmp_show_ls) " + // текущая страница ЛС
                " order by adr ";
            DataTable dtLs = ClassDBUtils.OpenSQL(sqlText, IDbConnection).GetData();
            foreach (DataColumn column in dtLs.Columns)
            {
                column.AllowDBNull = true;
                column.ReadOnly = false;
            }
            for (var i = 0; i < dtLs.Rows.Count; i++)
            {
                DataRow rowLs = dtLs.Rows[i];
                rowLs["num"] = finder.skip + i + 1;
                if (finder.nzp_prm > 0)
                {
                    finder.nzp = Convert.ToInt32(dtLs.Rows[i]["nzp_kvar"]);
                    finder.month_ = Points.DateOper.Month;
                    finder.year_ = Points.DateOper.Year;
                    Prm prm = FindPrmValue(IDbConnection, finder, out ret);
                    if (ret.result)
                    {
                        rowLs["nzp_key"] = prm.nzp_key;
                        rowLs["nzp"] = prm.nzp;
                        rowLs["nzp_prm"] = prm.nzp_prm;
                        if (prm.dat_s != "")
                            rowLs["dat_s"] = DateTime.Parse(prm.dat_s);
                        if (prm.dat_po != "")
                            rowLs["dat_po"] = DateTime.Parse(prm.dat_po);
                        if (prm.type_prm.Trim().ToLower() == "sprav" || prm.type_prm.Trim().ToLower() == "norm")
                            rowLs["val_prm"] = prm.val_prm_sprav;
                        else
                            rowLs["val_prm"] = prm.val_prm;
                        rowLs["old_val_prm"] = prm.old_val_prm;
                        rowLs["is_actual"] = prm.is_actual;
                        if (prm.dat_del != "")
                            rowLs["dat_del"] = DateTime.Parse(prm.dat_del);
                        rowLs["user_del"] = prm.user_del;
                        if (prm.dat_when != "")
                            rowLs["dat_when"] = DateTime.Parse(prm.dat_when);
                        rowLs["nzp_user"] = prm.nzp_user;
                        rowLs["user_name"] = prm.user_name;
                        rowLs["block"] = prm.block;
                        rowLs["name_prm"] = prm.name_prm;
                        rowLs["type_prm"] = prm.type_prm;
                        rowLs["nzp_res"] = prm.nzp_res;
                        rowLs["prm_num"] = prm.prm_num;
                      
                    }
                }
            }

            return new ReturnsObjectType<DataTable>(dtLs) { tag = cntRowsTotal };
        }

        //        public Returns DbUpdateMovedHousesPkod(string connString)
        //        {
        //            Returns ret = Utils.InitReturns();
        //            IDbConnection conn_web = DBManager.newDbConnection(connString);
        //            try
        //            {
        //                ret = OpenDb(conn_web, true);
        //                if (!ret.result) return ret;

        //                string sql = "select bd_kernel from s_point where nzp_graj = 0";
        //                IDbCommand IDbCommand = DBManager.newDbCommand(sql, conn_web);
        //                Points.Pref = IDbCommand.ExecuteScalar().ToString().Trim();
        //                Points.IsSmr = true;
        //                IDbCommand.Dispose();

        //                sql = " select bd_kernel from " + Points.Pref + "_kernel:s_point where nzp_graj = 1 ";
        //                IDbCommand = DBManager.newDbCommand(sql, conn_web);
        //                var dt = ClassDBUtils.OpenSQL(sql, conn_web);
        //                IDbCommand.Dispose();
        //                var resRows = dt.resultData.Rows;

        //                MyDataReader reader = null;

        //                for (int i = 0; i < resRows.Count; i++)
        //                {
        //                    // определение платежного кода
        //#if PG
        //                    //sql = " SELECT k.num_ls, k.pkod10, d.nzp_area_n, d.nzp_geu_n, d.nzp_kvar_n FROM " + Points.Pref + "_data.dom_moved d, " + bankReader["bd_kernel"].ToString().Trim() + "_data.kvar k where k.nzp_kvar = d.nzp_kvar_n and d.is_to_move = 1";
        //#else
        //                    sql = " SELECT k.num_ls, k.pkod10, d.nzp_area_n, d.nzp_geu_n, d.nzp_kvar_n FROM " + Points.Pref + "_data:dom_moved d, " + resRows[i]["bd_kernel"].ToString().Trim() + "_data:kvar k where k.nzp_kvar = d.nzp_kvar_n and d.is_to_move = 1";
        //#endif

        //                    if (!ExecRead(conn_web, null, out reader, sql, true).result)
        //                    {
        //                        ret.text = "Ошибка получения данных по ключу квартиры";
        //                        ret.result = false;
        //                        return ret;
        //                    }

        //                    var counter = 0;
        //                    while (reader.Read())
        //                    {
        //                        counter++;
        //                        var num_ls = (reader["num_ls"] != DBNull.Value) ? Convert.ToInt32(reader["num_ls"]) : 0;
        //                        var nzp_kvar_n = (reader["nzp_kvar_n"] != DBNull.Value) ? Convert.ToInt32(reader["nzp_kvar_n"]) : 0;
        //                        var pkod10 = (reader["pkod10"] != DBNull.Value) ? Convert.ToInt32(reader["pkod10"]) : 0;
        //                        var nzp_area = (reader["nzp_area_n"] != DBNull.Value) ? Convert.ToInt32(reader["nzp_area_n"]) : 0;
        //                        var nzp_geu = (reader["nzp_geu_n"] != DBNull.Value) ? Convert.ToInt32(reader["nzp_geu_n"]) : 0;
        //                        var pkod = "";

        //                        //вытащить коды ЕРЦ
        //                        _KodERC erc = GetKodErc(conn_web, resRows[i]["bd_kernel"].ToString().Trim(), nzp_area, nzp_geu, out ret);
        //                        if (!ret.result)
        //                        {
        //                            ret.text = "Ошибка определения кода РЦ: " + ret.text;
        //                            return ret;
        //                        }


        //                        if (erc.kod_erc > 1000) //Для регионов
        //                        {

        //                            //формирование платежного кода
        //                            pkod = erc.kod_erc.ToString();
        //                            pkod += pkod10.ToString("00000");
        //                            pkod = Utils.BarcodeCRC13(pkod).ToString();

        //                            if (pkod.Length != 13)
        //                            {
        //                                ret.text = "Ошибка определения платежного кода: " + pkod;
        //                                ret.result = false;
        //                                return ret;
        //                            }
        //                        }
        //                        else if (Points.IsSmr) //признак Самары
        //                        {
        //                            //toto Postgres
        //                            //получение третьей с конца цифры
        //                            var p = "";
        //                            for (int j = 0; j < resRows.Count; j++)
        //                            {
        //                                sql = " select dat_s from " + resRows[j]["bd_kernel"].ToString().Trim() + "_data:prm_1 where nzp = " + nzp_kvar_n + " and nzp_prm = 2004 ";
        //                                IDbCommand = DBManager.newDbCommand(sql, conn_web);
        //                                var resp = IDbCommand.ExecuteScalar();
        //                                IDbCommand.Dispose();
        //                                if (resp != null)
        //                                {
        //                                    p = resp.ToString().Trim().Substring(9, 1);
        //                                    break;
        //                                }
        //                            }

        //                            //формирование платежного кода
        //                            pkod = erc.kod_erc + (Convert.ToInt32(nzp_geu) - 600).ToString("00");
        //                            pkod += (pkod10.ToString().Length == 6) ? pkod10.ToString("000000") : pkod10.ToString("00000") + p;
        //                            pkod += Utils.GetKontrSamara(pkod);

        //                            if (pkod.Length != 13)
        //                            {
        //                                ret.text = "Ошибка определения платежного кода: " + pkod;
        //                                ret.result = false;
        //                                return ret;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            long lpkod = Utils.EncodePKod(erc.kod_erc.ToString(), num_ls);

        //                            try
        //                            {
        //                                //пока в лоб определение казанских УК
        //                                if (Points.Pref.Substring(0, 2) == "uk")
        //                                {
        //                                    lpkod = Utils.EncodePKod(erc.kod_erc.ToString(), pkod10);
        //                                }
        //                            }
        //                            catch { }


        //                            pkod = lpkod.ToString();
        //                            if (lpkod < 1 || pkod.Length != 10)
        //                            {
        //                                ret.text = "Ошибка определения платежного кода: " + pkod;
        //                                ret.result = false;
        //                                return ret;
        //                            }
        //                        }

        //                        //проапдейтить базу
        //#if PG
        //                        //sql = "update " + bankReader["bd_kernel"].ToString().Trim() + "_data.kvar set pkod = " + pkod + " where nzp_kvar = " + reader["nzp_kvar_n"].ToString();
        //#else
        //                        sql = "update " + resRows[i]["bd_kernel"].ToString().Trim() + "_data:kvar set pkod = " + pkod + " where nzp_kvar = " + reader["nzp_kvar_n"].ToString();
        //#endif
        //                        IDbCommand = DBManager.newDbCommand(sql, conn_web);
        //                        IDbCommand.ExecuteNonQuery();
        //                        IDbCommand.Dispose();
        //                    }
        //                    reader.Close();
        //                }

        //                conn_web.Close();

        //                return ret;
        //            }
        //            catch (DbException ex)
        //            {
        //                ret.text = ex.Message;
        //                ret.result = false;
        //                conn_web.Close();
        //                return ret;
        //            }
        //        }

        //----------------------------------------------------------------------
        public _KodERC GetKodErc(IDbConnection conn_db, string pref, int nzp_area, long nzp_geu, out Returns ret) //вытащить коды ЕРЦ
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            _KodERC kod = new _KodERC();

            kod.bdf = "";
            kod.erc = "";
            kod.kod_erc = Constants._ZERO_;

            MyDataReader reader;

            string month_ = "";
            string year_ = "";
#if PG
            string sql = " Select month_,yearr From " + Points.Pref + "_data.saldo_date " +
#else
            string sql = " Select month_,yearr From " + Points.Pref + "_data:saldo_date " +
#endif
 " Where iscurrent = 0 ";
            ret = ExecRead(conn_db, null, out reader, sql, true);
            if (!ret.result)
            {
                return kod;
            }
            if (reader.Read())
            {
                month_ = reader["month_"].ToString().Trim();
                year_ = reader["yearr"].ToString().Trim();
            }
            reader.Close();

            //префикс УК по-умолчанию
#if PG
            sql = " Select kod as kod_erc, erc From " + pref + "_kernel.s_erck ";
#else
            sql = " Select kod as kod_erc, erc From " + pref + "_kernel:s_erck ";

#endif
            ret = ExecRead(conn_db, null, out reader, sql, true);
            if (!ret.result)
            {
                return kod;
            }
            if (reader.Read())
            {
                if (reader["kod_erc"] != DBNull.Value)
                    kod.kod_erc = (int)reader["kod_erc"];
                if (reader["erc"] != DBNull.Value)
                    kod.erc = (string)reader["erc"];
            }
            reader.Close();

            try
            {
#if PG
                sql = " Select bdf From " + pref + "_kernel.uk_setup ";
#else
                sql = " Select bdf From " + pref + "_kernel:uk_setup ";
#endif
                if (ExecRead(conn_db, null, out reader, sql, false).result)
                {
                    if (reader.Read())
                    {
                        if (reader["bdf"] != DBNull.Value)
                            kod.bdf = (string)reader["bdf"];
                    }
                }
                reader.Close();
            }
            catch
            {
                //нет uk_setup
            }

            if (nzp_area > 0)
            {
#if PG
                sql = " Select val_prm From " + pref + "_data.prm_7 " +
#else
                sql = " Select val_prm From " + pref + "_data:prm_7 " +
#endif
 " Where nzp_prm = 995 " +
#if PG
 "   and dat_s  <= public.mdy(" + month_ + ",1," + year_ + ")" +
                      "   and dat_po >= public.mdy(" + month_ + ",1," + year_ + ")" +
#else
 "   and dat_s  <= mdy(" + month_ + ",1," + year_ + ")" +
                      "   and dat_po >= mdy(" + month_ + ",1," + year_ + ")" +
#endif
 "   and is_actual <> 100 " +
                      "   and nzp = " + nzp_area;
                IDbCommand IDbCommand = DBManager.newDbCommand(sql, conn_db);
                var resp = IDbCommand.ExecuteScalar();
                IDbCommand.Dispose();
                if (resp != DBNull.Value)
                {
                    string s = (string)resp;
                    int i;
                    if (Int32.TryParse(s, out i))
                    {
                        kod.kod_erc = i;
                    }
                }
            }
            if (nzp_geu > 0)
            {
#if PG
                sql = " Select val_prm From " + pref + "_data.prm_8 " +
#else
                sql = " Select val_prm From " + pref + "_data:prm_8 " +
#endif
 " Where nzp_prm = 708 " +
#if PG
 "   and dat_s  <= public.mdy(" + month_ + ",1," + year_ + ")" +
                      "   and dat_po >= public.mdy(" + month_ + ",1," + year_ + ")" +
#else
 "   and dat_s  <= mdy(" + month_ + ",1," + year_ + ")" +
                      "   and dat_po >= mdy(" + month_ + ",1," + year_ + ")" +
#endif
 "   and is_actual <> 100 " +
                      "   and nzp = " + nzp_geu;
                IDbCommand IDbCommand = DBManager.newDbCommand(sql, conn_db);
                var resp = IDbCommand.ExecuteScalar();
                IDbCommand.Dispose();
                if (resp != DBNull.Value)
                {
                    string s = (string)resp;
                    int i;
                    if (Int32.TryParse(s, out i))
                    {
                        kod.kod_erc = i;
                    }
                }
            }

            //reader.Close();

            return kod;
        }

        //получаем примечание для ЖЭУ
        public string GetSetRemarkForGeu(Prm finder, bool edit, string rem, out Returns ret)
        {

            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            string sql = "";

            string remark = "";

            if (!edit)
            {
#if PG
                sql = "select remark from " + Points.Pref + "_data.s_remark where nzp_geu=" + finder.nzp;
#else
                sql = "select remark from " + Points.Pref + "_data:s_remark where nzp_geu=" + finder.nzp;
#endif
                IDbCommand IDbCommand = DBManager.newDbCommand(sql, conn_db);
                var resp = IDbCommand.ExecuteScalar();
                IDbCommand.Dispose();
                if (resp != DBNull.Value)
                {
                    remark = Convert.ToString(resp);
                }
            }
            else
            {
#if PG


                sql = " with upsert as (update " + Points.Pref + "_data.s_remark b set (remark) = ( " + Utils.EStrNull(rem) + " )" +
       " from (Select nzp_geu  from " + Points.Pref + "_data.s_geu where nzp_geu= " + finder.nzp + ") e where (remark) = ( " + Utils.EStrNull(rem) + ")" +
       "returning b.*) " + " insert into  " + Points.Pref + "_data.s_remark (nzp_area, nzp_geu, nzp_dom, remark) values (0, " + finder.nzp + ", 0, " + Utils.EStrNull(rem) + " )";

                //sql = "  MERGE INTO " + Points.Pref + "_data.s_remark b " +
                //               " USING (Select nzp_geu  from  " + Points.Pref + "_data.s_geu where nzp_geu=" + finder.nzp + ") e " +
                //               " ON (b.nzp_geu = e.nzp_geu) " +
                //               " WHEN MATCHED THEN  " +
                //               " update set (remark) = (" + Utils.EStrNull(rem) + ")" +
                //               " WHEN NOT MATCHED THEN " + " insert (nzp_area, nzp_geu, nzp_dom, remark) values (0, " + finder.nzp + ", 0, " + Utils.EStrNull(rem) + " )";




#else
                sql = "  MERGE INTO " + Points.Pref + "_data:s_remark b " +
                   " USING (Select nzp_geu  from  " + Points.Pref + "_data:s_geu where nzp_geu=" + finder.nzp + ") e " +
                   " ON (b.nzp_geu = e.nzp_geu) " +
                   " WHEN MATCHED THEN  " +
                   " update set (remark) = (" + Utils.EStrNull(rem) + ")" +
                   " WHEN NOT MATCHED THEN " + " insert (nzp_area, nzp_geu, nzp_dom, remark) values (0, " + finder.nzp + ", 0, " + Utils.EStrNull(rem) + " )";



#endif

                IDbCommand IDbCommand = DBManager.newDbCommand(sql, conn_db);
                IDbCommand.ExecuteNonQuery();
                IDbCommand.Dispose();
            }
            return remark.Trim();
        }

        public List<NormTreeView> GetServForNorm(out Returns ret, int nzp_wp, bool showOld)
        {
            List<NormTreeView> list = new List<NormTreeView>();
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(con_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("GetServForNorm : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return list;
            }
            try
            {
                string sql = " SELECT nzp_serv, service " +
                             " FROM " + Points.Pref + sKernelAliasRest + "s_serv_for_norm " +
                             " ORDER BY ordering ";
                DataTable dt = ClassDBUtils.OpenSQL(sql, con_db).resultData;
                foreach (DataRow r in dt.Rows)
                {
                    NormTreeView pt = new NormTreeView();
                    pt.id = Convert.ToInt32(r["nzp_serv"]);
                    pt.type_name = r["service"].ToString().Trim();

                    List<PrmTypes> oneServNorms = new List<PrmTypes>();
                    sql = " SELECT nt.id, nt.name_type_norm, nt.date_from, nt.date_to " +
                          " FROM " + Points.Pref + sKernelAliasRest + "norm_types nt," +
                          Points.Pref + sKernelAliasRest + "norm_banks nb" +
                          " WHERE nt.nzp_serv = " + Convert.ToInt32(r["nzp_serv"]) +
                          " AND nb.norm_type_id = nt.id AND nb.nzp_wp = " + nzp_wp +
                          (showOld ? "" : " AND nt.date_from<>nt.date_to ") +
                          " ORDER BY 1";
                    DataTable dtTypes = ClassDBUtils.OpenSQL(sql, con_db).resultData;
                    foreach (DataRow rtype in dtTypes.Rows)
                    {
                        NormTreeView type = new NormTreeView();
                        type.id = Convert.ToInt32(rtype["id"]);
                        string ext_name = rtype["date_from"].ToString().Substring(0, 10) != rtype["date_to"].ToString().Substring(0, 10) ?
                            " (" + rtype["date_from"].ToString().Substring(0, 10) +
                            " - " + rtype["date_to"].ToString().Substring(0, 10) + ")" : "(Тип норматива не действует)";
                        type.type_name = rtype["name_type_norm"].ToString().Trim() + ext_name;

                        oneServNorms.Add(type);
                    }
                    pt.NormTypes = oneServNorms;

                    list.Add(pt);
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка получения списка услуг для справочника нормативов";
                MonitorLog.WriteLog("GetServForNorm : Ошибка получения списка услуг для справочника нормативов " +
                    ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
                return list;
            }
            finally
            {
                con_db.Close();
            }

            return list;
        }

        public List<NormParam> GetAddNormParam(out Returns ret, int id)
        {
            List<NormParam> list = new List<NormParam>();
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(con_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("GetAddNormParam : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return list;
            }
            try
            {
                string sql = " SELECT nt.id, prm1.name_prm as prm_ls, prm1.nzp_prm as nzp_prm_ls," +
                          " prm2.name_prm as prm_dom, prm2.nzp_prm as nzp_prm_dom, nt.type_val_sign_id as id_type_val, " +
                          " st.description as type_val, nt.min_val, nt.max_val, nt.max_count, nt.ordering" +
                          " FROM " + Points.Pref + sKernelAliasRest + "s_type_val_sign st," +
                          Points.Pref + sKernelAliasRest + "norm_types n," +
                          Points.Pref + sKernelAliasRest + "norm_types_sign nt" +
                          " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest + "prm_name prm1 on nt.nzp_prm_ls = prm1.nzp_prm" +
                          " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest + "prm_name prm2 on nt.nzp_prm_house = prm2.nzp_prm " +
                          " WHERE nt.type_val_sign_id = st.id AND n.id = nt.norm_type_id AND n.id = " + id;

                DataTable param = ClassDBUtils.OpenSQL(sql, con_db).resultData;
                foreach (DataRow r in param.Rows)
                {
                    NormParam np = new NormParam();
                    np.id = Convert.ToInt32(r["id"]);
                    np.str_prm_ls = r["prm_ls"].ToString().Trim();
                    np.nzp_prm_ls = r["nzp_prm_ls"] != DBNull.Value ? Convert.ToInt32(r["nzp_prm_ls"]) : 0;
                    np.str_prm_dom = r["prm_dom"].ToString().Trim();
                    np.nzp_prm_dom = r["nzp_prm_dom"] != DBNull.Value ? Convert.ToInt32(r["nzp_prm_dom"]) : 0;
                    np.str_type_val = r["type_val"].ToString().Trim();
                    np.id_type_val = r["id_type_val"] != DBNull.Value ? Convert.ToInt32(r["id_type_val"]) : 0;
                    if (np.id_type_val == 2 || np.id_type_val == 3)
                    {
                        np.min_val = "01.01.1900";
                        np.max_val = "01.01.3000";
                    }
                    else
                    {
                        np.min_val = r["min_val"].ToString().Trim();
                        np.max_val = r["max_val"].ToString().Trim();
                    }
                    np.max_amount = Convert.ToInt32(r["max_count"]);
                    np.ordering = Convert.ToInt32(r["ordering"]);
                    list.Add(np);
                }


            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка получения списка добавленных параметров для справочника нормативов";
                MonitorLog.WriteLog("GetAddNormParam : Ошибка получения списка добавленных параметров для справочника нормативов " +
                    ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
                return list;
            }
            finally
            {
                con_db.Close();
            }

            return list;
        }


        public List<PrmTypes> GetMeasuresForNorm(out Returns ret)
        {
            List<PrmTypes> list = new List<PrmTypes>();
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(con_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("GetMeasuresForNorm : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return list;
            }
            try
            {
                string sql = " SELECT nzp_measure, measure " +
                             " FROM " + Points.Pref + sKernelAliasRest + "s_measure  ";
                DataTable dt = ClassDBUtils.OpenSQL(sql, con_db).resultData;
                foreach (DataRow r in dt.Rows)
                {
                    PrmTypes pt = new PrmTypes();
                    pt.id = Convert.ToInt32(r["nzp_measure"]);
                    pt.type_name = r["measure"].ToString().Trim();
                    list.Add(pt);
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка получения списка единиц измерения для справочника нормативов";
                MonitorLog.WriteLog("GetMeasuresForNorm : Ошибка получения списка единиц измерения для справочника нормативов " +
                    ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
                return list;
            }
            finally
            {
                con_db.Close();
            }

            return list;
        }


        public List<PrmTypes> GetKindNorm(out Returns ret)
        {
            List<PrmTypes> list = new List<PrmTypes>();
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(con_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("GetKindNorm : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return list;
            }
            try
            {
                string sql = " SELECT id, description " +
                             " FROM " + Points.Pref + sKernelAliasRest + "s_kind_norm ";
                DataTable dt = ClassDBUtils.OpenSQL(sql, con_db).resultData;
                foreach (DataRow r in dt.Rows)
                {
                    PrmTypes pt = new PrmTypes();
                    pt.id = Convert.ToInt32(r["id"]);
                    pt.type_name = r["description"].ToString().Trim();
                    list.Add(pt);
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка получения списка типов нормативов для справочника нормативов";
                MonitorLog.WriteLog("GetKindNorm : Ошибка получения списка типов нормативов для справочника нормативов " +
                    ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
                return list;
            }
            finally
            {
                con_db.Close();
            }

            return list;
        }


        public List<PrmTypes> GetServNorm(out Returns ret)
        {
            List<PrmTypes> list = new List<PrmTypes>();
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(con_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("GetServNorm : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return list;
            }
            try
            {
                string sql = " SELECT nzp_serv, service " +
                " FROM " + Points.Pref + sKernelAliasRest + "s_serv_for_norm " +
                " ORDER BY ordering ";
                DataTable dt = ClassDBUtils.OpenSQL(sql, con_db).resultData;
                foreach (DataRow r in dt.Rows)
                {
                    PrmTypes pt = new PrmTypes();
                    pt.id = Convert.ToInt32(r["nzp_serv"]);
                    pt.type_name = r["service"].ToString().Trim();
                    list.Add(pt);
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка получения списка услуг для справочника нормативов";
                MonitorLog.WriteLog("GetServNorm : Ошибка получения списка услуг для справочника нормативов " +
                    ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
                return list;
            }
            finally
            {
                con_db.Close();
            }

            return list;
        }


        public List<PrmTypes> GetParamNorm(int TypePrm, int NormTypeId, out Returns ret)
        {
            List<PrmTypes> list = new List<PrmTypes>();
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(con_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("GetParamNorm : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return list;
            }
            try
            {
                var not_in = "";
                string dopPrmNum = "";
                if (TypePrm == 1)
                {
                    dopPrmNum = ",5"; //для параметра проверки наличия услуги у ЛС (nzp_prm=1984 )
                }
                if (TypePrm == 17)
                {
                    not_in = " and p.nzp_prm not in (2024,1426) ";
                }
                string sql = " SELECT nzp_prm, name_prm " +
                             " FROM " + Points.Pref + sKernelAliasRest + "prm_name p" +
                             " WHERE trim(type_prm) <> 'sprav' " +
                             " AND prm_num in (" + TypePrm + dopPrmNum + ")" + not_in +
                             " UNION " +
                             " SELECT p.nzp_prm as nzp_prm, p.name_prm as name_prm " +
                             " FROM " + Points.Pref + sKernelAliasRest + "prm_name p," +
                             Points.Pref + sKernelAliasRest + "norm_prm_serv nps," +
                             Points.Pref + sKernelAliasRest + "norm_types nt" +
                             " WHERE trim(type_prm) = 'sprav' " +
                             " AND nps.nzp_prm = p.nzp_prm AND (nt.nzp_serv = nps.nzp_serv OR nt.nzp_serv = nps.nzp_serv_slave)" +
                             " AND nt.nzp_measure = nps.nzp_measure AND nt.id = " + NormTypeId +
                             " AND p.prm_num in (" + TypePrm + dopPrmNum + ")" + not_in +
                             " ORDER BY name_prm";
                DataTable dt = ClassDBUtils.OpenSQL(sql, con_db).resultData;
                foreach (DataRow r in dt.Rows)
                {
                    PrmTypes pt = new PrmTypes();
                    pt.id = Convert.ToInt32(r["nzp_prm"]);
                    pt.type_name = r["name_prm"].ToString().Trim();
                    list.Add(pt);
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка получения списка параметров для справочника нормативов";
                MonitorLog.WriteLog("GetParamNorm : Ошибка получения списка услуг для справочника нормативов " +
                    ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
                return list;
            }
            finally
            {
                con_db.Close();
            }

            return list;
        }

        public List<PrmTypes> GetValueTypesNorm(out Returns ret)
        {
            List<PrmTypes> list = new List<PrmTypes>();
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(con_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("GetParamNorm : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return list;
            }
            try
            {
                string sql = " SELECT id, description " +
                             " FROM " + Points.Pref + sKernelAliasRest + "s_type_val_sign";
                DataTable dt = ClassDBUtils.OpenSQL(sql, con_db).resultData;
                foreach (DataRow r in dt.Rows)
                {
                    PrmTypes pt = new PrmTypes();
                    pt.id = Convert.ToInt32(r["id"]);
                    pt.type_name = r["description"].ToString().Trim();
                    list.Add(pt);
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка получения списка значений параметров для справочника нормативов";
                MonitorLog.WriteLog("GetValueTypesNorm : Ошибка получения списка значений параметров для справочника нормативов " +
                    ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
                return list;
            }
            finally
            {
                con_db.Close();
            }

            return list;
        }

        public Norm GetNormParam(out Returns ret, int id_norm)
        {
            Norm norm = new Norm();
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(con_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("GetNormParam : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return norm;
            }
            try
            {
                string sql = " SELECT  nt.*,m.measure, kn.description,m.nzp_measure, kn.id as id_kind_norm, s.service as serv_name, " +
                             " trim(u.comment) as creator_uname, trim(u1.comment) as changer_uname " +
                             " FROM " + Points.Pref + sKernelAliasRest + "s_kind_norm kn," +
                             Points.Pref + sKernelAliasRest + "s_measure m," +
                             Points.Pref + sKernelAliasRest + "s_serv_for_norm s," +
                             Points.Pref + sKernelAliasRest + "norm_types nt  " +
                             "LEFT OUTER JOIN " + Points.Pref + DBManager.sDataAliasRest + "users u ON u.nzp_user=nt.created_by " +
                             "LEFT OUTER JOIN " + Points.Pref + DBManager.sDataAliasRest + "users u1 ON u1.nzp_user=nt.changed_by " +
                             " WHERE nt.nzp_serv = s.nzp_serv AND nt.id =" + id_norm +
                             " AND m.nzp_measure = nt.nzp_measure " +
                             " AND kn.id = nt.s_kind_norm_id";
                DataTable dt = ClassDBUtils.OpenSQL(sql, con_db).resultData;
                if (dt.Rows.Count > 0)
                {
                    norm.str_kind_norm = dt.Rows[0]["description"].ToString().Trim();
                    norm.id_kind_norm = Convert.ToInt32(dt.Rows[0]["id_kind_norm"]);
                    norm.measure = dt.Rows[0]["measure"].ToString().Trim();
                    norm.nzp_measure = Convert.ToInt32(dt.Rows[0]["nzp_measure"]);
                    norm.nzp_serv = Convert.ToInt32(dt.Rows[0]["nzp_serv"]);
                    norm.service = dt.Rows[0]["serv_name"].ToString().Trim();
                    norm.dat_from = dt.Rows[0]["date_from"].ToString().Substring(0, 10).Trim();
                    norm.dat_po = dt.Rows[0]["date_to"].ToString().Substring(0, 10).Trim();

                    norm.dat_from_old = dt.Rows[0]["date_from_old"] != DBNull.Value
                        ? Convert.ToDateTime(dt.Rows[0]["date_from_old"]).ToString("dd.MM.yyyy") : "";
                    norm.dat_to_old = dt.Rows[0]["date_to_old"] != DBNull.Value
                  ? Convert.ToDateTime(dt.Rows[0]["date_to_old"]).ToString("dd.MM.yyyy") : "";

                    norm.is_finished = Convert.ToBoolean(dt.Rows[0]["is_finished"]);
                    norm.is_day_norm = Convert.ToBoolean(dt.Rows[0]["is_day_period"]);
                    norm.norm_name = dt.Rows[0]["name_type_norm"].ToString().Trim();
                    norm.created_by = dt.Rows[0]["creator_uname"] != DBNull.Value ? dt.Rows[0]["creator_uname"].ToString().Trim() : "";
                    norm.changed_by = dt.Rows[0]["changer_uname"] != DBNull.Value ? dt.Rows[0]["changer_uname"].ToString().Trim() : "";
                    norm.created_on = dt.Rows[0]["created_on"] != DBNull.Value ? Convert.ToDateTime(dt.Rows[0]["created_on"]).ToString("dd.MM.yyyy HH:mm:ss") : "";
                    norm.changed_on = dt.Rows[0]["changed_on"] != DBNull.Value ? Convert.ToDateTime(dt.Rows[0]["changed_on"]).ToString("dd.MM.yyyy HH:mm:ss") : "";

                    sql = " SELECT nzp_wp FROM " + Points.Pref + sKernelAliasRest + "norm_banks" +
                          " WHERE norm_type_id = " + id_norm + " ";
                    DataTable banks = ClassDBUtils.OpenSQL(sql, con_db).resultData;
                    foreach (DataRow r in banks.Rows)
                    {
                        norm.nzp_wp.Add(Convert.ToInt32(r["nzp_wp"]));
                    }

                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка получения списка услуг для справочника нормативов";
                MonitorLog.WriteLog("GetNormParam : Ошибка получения списка услуг для справочника нормативов " +
                    ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
                return norm;
            }
            finally
            {
                con_db.Close();
            }

            return norm;
        }

        public Norm GetNormParamValuesOnLoadPage(out Returns ret, int id_norm)
        {
            Norm norm = new Norm();
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(con_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("GetNormParamValuesOnLoadPage : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return norm;
            }
            try
            {
                string sql =
                    " SELECT prm1.name_prm as prm_ls, prm1.nzp_prm as nzp_prm_ls, nt.id," +
                      " prm2.name_prm as prm_dom, prm2.nzp_prm as nzp_prm_dom, nt.type_val_sign_id as id_type_val, " +
                      " st.description as type_val, nt.min_val, nt.max_val, nt.max_count, nt.ordering, nt.is_finished" +
                      " FROM " + Points.Pref + sKernelAliasRest + "s_type_val_sign st," +
                      Points.Pref + sKernelAliasRest + "norm_types n," +
                      Points.Pref + sKernelAliasRest + "norm_types_sign nt" +
                      " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest +
                      "prm_name prm1 on nt.nzp_prm_ls = prm1.nzp_prm" +
                      " LEFT OUTER JOIN " + Points.Pref + sKernelAliasRest +
                      "prm_name prm2 on nt.nzp_prm_house = prm2.nzp_prm " +
                      " WHERE nt.type_val_sign_id = st.id AND n.id = nt.norm_type_id AND n.id = " + id_norm;
                DataTable param = ClassDBUtils.OpenSQL(sql, con_db).resultData;
                foreach (DataRow r in param.Rows)
                {
                    NormParam np = new NormParam();
                    np.id = Convert.ToInt32(r["id"]);
                    np.str_prm_ls = r["prm_ls"].ToString().Trim();
                    np.nzp_prm_ls = r["nzp_prm_ls"] != DBNull.Value ? Convert.ToInt32(r["nzp_prm_ls"]) : 0;
                    np.str_prm_dom = r["prm_dom"].ToString().Trim();
                    np.nzp_prm_dom = r["nzp_prm_dom"] != DBNull.Value ? Convert.ToInt32(r["nzp_prm_dom"]) : 0;
                    np.str_type_val = r["type_val"].ToString().Trim();
                    np.id_type_val = r["id_type_val"] != DBNull.Value ? Convert.ToInt32(r["id_type_val"]) : 0;
                    np.min_val = r["min_val"].ToString().Trim();
                    np.max_val = r["max_val"].ToString().Trim();
                    np.max_amount = Convert.ToInt32(r["max_count"]);
                    np.ordering = Convert.ToInt32(r["ordering"]);
                    np.is_finished = Convert.ToBoolean(r["is_finished"]);
                    norm.norm_params.Add(np);
                }
                //если не заполнена таблица norm_tables, дать возможность заполнять значения параметров ret.tag = 999
                sql = " SELECT * FROM " + Points.Pref + sKernelAliasRest + "norm_tables" +
                      " WHERE norm_type_id = " + id_norm;
                norm.id_kind_norm = CastValue<int>(ExecScalar(con_db, "Select s_kind_norm_id from " + Points.Pref + sKernelAliasRest + "norm_types where id = " + id_norm, out ret, false));
                DataTable dtFromNT = ClassDBUtils.OpenSQL(sql, con_db).resultData;
                if (dtFromNT.Rows.Count == 0) ret.tag = 999;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка получения списка услуг для справочника нормативов";
                MonitorLog.WriteLog("GetNormParam : Ошибка получения списка услуг для справочника нормативов " +
                                    ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
                return norm;
            }
            finally
            {
                con_db.Close();
            }

            return norm;
        }

        public List<NormParamValue> GetNormParamValues(out Returns ret, int nzp_prm, int norm_type_id)
        {
            List<NormParamValue> list = new List<NormParamValue>();
            IDbConnection con_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(con_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("GetNormParamValues : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return list;
            }
            try
            {
                string sql = " SELECT DISTINCT param_value1, param_value2, date_value1," +
                             " date_value2, name_prm_val, ordering " +
                             " FROM " + Points.Pref + sKernelAliasRest + "influence_params" +
                             " WHERE nzp_prm =" + nzp_prm +
                             " AND norm_tables_id in" +
                             " (SELECT id FROM " + Points.Pref + sKernelAliasRest + "norm_tables" +
                             " WHERE norm_type_id = " + norm_type_id + " )" +
                             " ORDER BY ordering";
                DataTable dt = ClassDBUtils.OpenSQL(sql, con_db).resultData;

                if (dt.Rows.Count == 0)
                {

                    //В нормативе может быть одновременно на один параметр выбран и домовой, и лицевой параметр в одной строке. 
                    //В этом случае приоритет отдается параметру ЛС, и все записывается на параметр ЛС.
                    //Сейчас мы это учтем и вытащим нормативы для домового параметра по параметру ЛС

                    #region Вытаскиваем норматив на домовой параметр, у которого есть в паре лицевой параметр на этом нормативе

                    sql = " SELECT DISTINCT i.param_value1, i.param_value2, i.date_value1," +
                             " i.date_value2, i.name_prm_val, i.ordering " +
                             " FROM " + Points.Pref + sKernelAliasRest + "norm_types_sign nts, " +
                             Points.Pref + sKernelAliasRest + "influence_params i" +
                             " WHERE nts.norm_type_id = " + norm_type_id + 
                             " AND nts.nzp_prm_ls = i.nzp_prm" +
                             " AND nts.nzp_prm_house =" + nzp_prm +
                             " AND norm_tables_id in" +
                             " (SELECT id FROM " + Points.Pref + sKernelAliasRest + "norm_tables" +
                             " WHERE norm_type_id = " + norm_type_id + " )" +
                             " ORDER BY ordering";
                   dt = ClassDBUtils.OpenSQL(sql, con_db).resultData;
                    #endregion
                }
                foreach (DataRow r in dt.Rows)
                {
                    NormParamValue n = new NormParamValue();
                    n.val_name = r["name_prm_val"].ToString().Trim();
                    n.ordering = Convert.ToInt32(r["ordering"]);
                    n.val_1 = r["param_value1"] != DBNull.Value ? Convert.ToDouble(r["param_value1"]).ToString() :
                        (r["date_value1"] != DBNull.Value ? Convert.ToDateTime(r["date_value1"]).ToShortDateString() : "");
                    n.val_2 = r["param_value2"] != DBNull.Value ? Convert.ToDouble(r["param_value2"]).ToString() :
                        (r["date_value2"] != DBNull.Value ? Convert.ToDateTime(r["date_value2"]).ToShortDateString() : "");
                    list.Add(n);
                }

            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка получения списка услуг для справочника нормативов";
                MonitorLog.WriteLog("GetNormParam : Ошибка получения списка услуг для справочника нормативов " +
                    ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
                return list;
            }
            finally
            {
                con_db.Close();
            }

            return list;
        }

        public Returns SaveNormFirstStage(Norm norm)
        {
            Returns ret = Utils.InitReturns();

            IDbConnection con_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(con_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("SaveNormFirstStage : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return ret;
            }
            try
            {
                string where = norm.id > 0 ? " AND id <>" + norm.id : "";
                string sql;

                /*
                 ret = CheckNameSaveNormFirstStage(con_db, norm, where); //проверка на существование одноименных в одном банке
                 if (!ret.result)
                     return ret;
                 */
                if (!norm.is_refreshing)
                {
                    ret = CheckExistingTypeNorms(con_db, norm, where); //проверка на существование типов нормативов для одной услуги, периодов и ед.измерения
                    if (!ret.result)
                        return ret;
                }

                int norm_type_id;
                if (norm.id > 0)
                {
                    norm_type_id = norm.id;
                    sql = " UPDATE " + Points.Pref + sKernelAliasRest + "norm_types " +
                          " SET (nzp_serv, s_kind_norm_id, nzp_measure, name_type_norm, date_from," +
                          " date_to, is_finished, is_day_period, changed_by, changed_on) =" +
                          " (" + norm.nzp_serv + ", " + norm.id_kind_norm + ", " +
                          norm.nzp_measure + ", '" + norm.norm_name + "', '" + norm.dat_from + "', '" +
                          norm.dat_po + "', false, " + norm.is_day_norm + ", " + norm.nzp_user + ", " + sCurDateTime + ")" +
                          " WHERE id = " + norm.id;
                    ret = ExecSQL(con_db, sql, true);
                    sql = " DELETE FROM " + Points.Pref + sKernelAliasRest + "norm_banks " +
                          " WHERE norm_type_id = " + norm.id;
                    ret = ExecSQL(con_db, sql, true);
                }
                else
                {
                    sql = " INSERT INTO " + Points.Pref + sKernelAliasRest + "norm_types " +
                          " (nzp_serv, s_kind_norm_id, nzp_measure, name_type_norm, date_from," +
                          " date_to, is_finished, is_day_period, created_by, created_on) " +
                          " VALUES (" + norm.nzp_serv + ", " + norm.id_kind_norm + ", " +
                          norm.nzp_measure + ", '" + norm.norm_name + "', '" + norm.dat_from + "', '" +
                          norm.dat_po + "', false, " + norm.is_day_norm + ", " + norm.nzp_user + ", " + sCurDateTime + ")";
                    ret = ExecSQL(con_db, sql, true);
                    if (ret.result) norm_type_id = GetSerialValue(con_db);
                    else return new Returns(false, "Ошибка добавления норматива в справочник нормативов.", -1);
                }
                foreach (int nzp_wp in norm.nzp_wp)
                {
                    sql = " INSERT INTO " + Points.Pref + sKernelAliasRest + "norm_banks" +
                          " (norm_type_id, nzp_wp)" +
                          " VALUES (" + norm_type_id + ", " + nzp_wp + ")";
                    ret = ExecSQL(con_db, sql, true);
                }

                //переопределение периодов типов нормативов
                if (norm.is_refreshing)
                {
                    norm.id = norm_type_id;
                    List<int> listOldTypresId = GetRefreshingTypeNorms(con_db, norm);
                    listOldTypresId.Add(0);
                    //ограничиваем период действия старых типов нормативов

                    DateTime dateCloseOld = DateTime.Parse(norm.dat_from).AddDays(-1);

                    sql =
                      string.Format(
                          " UPDATE {1}{2}norm_types SET date_from_old=date_from,date_to_old=date_to " +
                          "  WHERE id in ({0}) AND date_from_old is null",
                          string.Join(",", listOldTypresId), Points.Pref, sKernelAliasRest);
                    ret = ExecSQL(con_db, sql, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("SaveNormFirstStage : Ошибка добавления норматива ", MonitorLog.typelog.Error, true);
                        return ret;
                    }

                    sql =
                        string.Format(
                            " UPDATE {1}{2}norm_types SET date_to={3}, changed_on={4}, changed_by={5}" +
                            "  WHERE id in ({0})",
                            string.Join(",", listOldTypresId), Points.Pref, sKernelAliasRest,
                            Utils.EStrNull(dateCloseOld.ToShortDateString()), sCurDateTime, norm.nzp_user);
                    ret = ExecSQL(con_db, sql, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("SaveNormFirstStage : Ошибка добавления норматива ", MonitorLog.typelog.Error, true);
                        return ret;
                    }

                    sql =
                    string.Format(
                        " UPDATE {1}{2}norm_types SET date_from=date_to WHERE id in ({0}) AND date_from>date_to ",
                        string.Join(",", listOldTypresId), Points.Pref, sKernelAliasRest);
                    ret = ExecSQL(con_db, sql, true);
                }

                ret.tag = norm_type_id;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("SaveNormFirstStage : Ошибка добавления норматива " +
                    ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
                return new Returns(false, "Ошибка добавления норматива.", (int)NormKodRet.Error);
            }
            finally
            {
                con_db.Close();
            }
            return ret;
        }


        private Returns CheckExistingTypeNorms(IDbConnection con_db, Norm norm, string where)
        {
            Returns ret = Utils.InitReturns();
            foreach (var nzp_wp in norm.nzp_wp)
            {
                string sql =
                     " SELECT nt.id" +
                     " FROM " + Points.Pref + sKernelAliasRest + "norm_types nt, " +
                     Points.Pref + sKernelAliasRest + "norm_banks nb" +
                     " WHERE nt.nzp_serv = " + norm.nzp_serv +
                     " AND nt.s_kind_norm_id =" + norm.id_kind_norm + 
                     " AND nt.nzp_measure = " + norm.nzp_measure +
                     " AND nt.date_from <= ('" + norm.dat_po + "' " + sConvToDate + ")" +
                     " AND nt.date_to >= ('" + norm.dat_from + "' " + sConvToDate + ")" +
                     " AND nb.norm_type_id = nt.id AND nb.nzp_wp = " + nzp_wp + " "
                     + where;
                DataTable dt = ClassDBUtils.OpenSQL(sql, con_db).resultData;
                if (dt.Rows.Count > 0)
                {
                    string bank = (from b in Points.PointList
                                   where b.nzp_wp == nzp_wp
                                   select b.point).FirstOrDefault();
                    if (bank != null) bank = " в " + bank;
                    ret.result = false;
                    ret.text = "Тип нормативов на эту услугу " + bank + " с этой единицей измерения и пересекающимся периодом уже имеется.\r\n" +
                               " Вы действительно хотите создать новый тип нормативов? " +
                               " Текущий тип нормативов будет переопределен полностью или его дата окончания периода действия " +
                               " будет ограничена датой начала нового типа нормативов.  ";
                    ret.tag = (int)NormKodRet.Exist;
                    return ret;
                }
            }
            return ret;
        }

        private List<int> GetRefreshingTypeNorms(IDbConnection con_db, Norm norm)
        {
            var res = new List<int>();
            foreach (var nzp_wp in norm.nzp_wp)
            {
                string sql =
                    " SELECT nt.id" +
                    " FROM " + Points.Pref + sKernelAliasRest + "norm_types nt, " +
                    Points.Pref + sKernelAliasRest + "norm_banks nb" +
                    " WHERE nt.nzp_serv = " + norm.nzp_serv +
                    " AND nt.s_kind_norm_id =" + norm.id_kind_norm + 
                    " AND nt.nzp_measure = " + norm.nzp_measure +
                    " AND nt.date_from <= ('" + norm.dat_po + "' " + sConvToDate + ")" +
                    " AND nt.date_to >= ('" + norm.dat_from + "' " + sConvToDate + ")" +
                    " AND nb.norm_type_id = nt.id AND nb.nzp_wp = " + nzp_wp + " AND nt.id<>" + norm.id;
                DataTable DT = ClassDBUtils.OpenSQL(sql, con_db).resultData;
                for (int i = 0; i < DT.Rows.Count; i++)
                {
                    res.Add(DT.Rows[i]["id"] != DBNull.Value ? Convert.ToInt32(DT.Rows[i]["id"]) : 0);
                }
            }
            return res;
        }



        private Returns CheckNameSaveNormFirstStage(IDbConnection con_db, Norm norm, string where)
        {
            Returns ret = Utils.InitReturns();
            string sql = " SELECT id" +
                         " FROM " + Points.Pref + sKernelAliasRest + "norm_types nt, " + Points.Pref + sKernelAliasRest +
                         "norm_banks nb " +
                         " WHERE upper(trim(name_type_norm)) = '" + norm.norm_name.Trim().ToUpper() + "'" +
                         " and nb.norm_type_id=nt.id and nb.nzp_wp in (" +
                         string.Join(",", norm.nzp_wp.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray()) + ")"
                         + where;
            DataTable dt = ClassDBUtils.OpenSQL(sql, con_db).resultData;
            if (dt.Rows.Count > 0)
            {
                ret.result = false;
                ret.text = "Норматив c названием " + norm.norm_name.Trim() + " уже имеется в списке выбранных районов.";
                ret.tag = (int)NormKodRet.Error;
            }
            return ret;
        }

        public Returns SaveNormParamStatus(int normTypeId)
        {
            Returns ret = Utils.InitReturns();

            IDbConnection con_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(con_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("SaveNormParamStatus : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return ret;
            }
            try
            {
                string sql =
                    " SELECT * FROM " + Points.Pref + sKernelAliasRest + "norm_types_sign" +
                    " WHERE norm_type_id = " + normTypeId;
                DataTable dt = DBManager.ExecSQLToTable(con_db, sql);
                if (dt.Rows.Count == 0)
                {
                    return new Returns(false, "Не добавлен ни один параметр!", (int)NormKodRet.Error);
                }
                
                sql =
                    " UPDATE " + Points.Pref + sKernelAliasRest + "norm_types_sign" +
                    " SET is_finished = true" +
                    " WHERE norm_type_id = " + normTypeId;
                ret = ExecSQL(con_db, sql, true);

                sql = 
                    " UPDATE " + Points.Pref + sKernelAliasRest + "norm_types" +
                    " SET is_finished = true" +
                    " WHERE id = " + normTypeId;
                ret = ExecSQL(con_db, sql, true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("SaveNormParamStatus : Ошибка добавления норматива " +
                    ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
                return new Returns(false, "Ошибка добавления норматива.", (int)NormKodRet.Error);
            }
            finally
            {
                con_db.Close();
            }
            return ret;
        }

        public Returns SaveParamSecondStage(NormTypesSign finder)
        {
            Returns ret = Utils.InitReturns();

            IDbConnection con_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(con_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("SaveParamSecondStage : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return ret;
            }
            try
            {
                string sql;
                ret = CheckSpravParam(con_db, finder);
                if (!ret.result) return ret;

                string min_val = "0";
                string max_val = "0";

                switch (finder.type_val_sign_id)
                {
                    case (int)STypeValSign.Num:
                        {
                            min_val = finder.min_val.ToString();
                            max_val = finder.max_val.ToString();
                            break;
                        }
                    case (int)STypeValSign.Date:
                    case (int)STypeValSign.Sprav:
                    case (int)STypeValSign.Period:
                        {
                            min_val = "null";
                            max_val = "null";
                            break;
                        }
                    case (int)STypeValSign.NumPeriod:
                        {
                            min_val = finder.min_val.ToString();
                            max_val = finder.max_val.ToString();
                            break;
                        }
                    case (int)STypeValSign.Boolean:
                        {
                            min_val = "0";
                            max_val = "1";
                            finder.max_count = 2;
                            break;
                        }
                }

                sql = " INSERT INTO " + Points.Pref + sKernelAliasRest + "norm_types_sign" +
                             " (norm_type_id, nzp_prm_ls, nzp_prm_house, min_val, max_val," +
                             " max_count, type_val_sign_id, is_finished, ordering)" +
                             " VALUES" +
                             " (" + finder.id + ", " + (finder.nzp_prm_ls == 0 ? "null" : finder.nzp_prm_ls.ToString()) + ", " +
                             (finder.nzp_prm_house == 0 ? "null" : finder.nzp_prm_house.ToString()) + ", " +
                             min_val + ", " + max_val + ", " + finder.max_count + ", " + finder.type_val_sign_id + ", " +
                             " false, (SELECT count(*) FROM " + Points.Pref + sKernelAliasRest + "norm_types_sign" +
                             " WHERE norm_type_id = " + finder.id + ") + 1" +
                             ")";
                ret = ExecSQL(con_db, sql, true);
                if (ret.result)
                {
                    sql = " UPDATE " + Points.Pref + sKernelAliasRest + "norm_types" +
                          " SET is_finished = true" +
                          " WHERE id = " + finder.id;
                    ret = ExecSQL(con_db, sql, true);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("SaveParamSecondStage : Ошибка добавления параметра норматива " +
                    ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = "Ошибка добавления параметра норматива.";
                ret.tag = (int)NormKodRet.Error;
            }
            finally
            {
                con_db.Close();
            }

            return ret;
        }

        private Returns CheckSpravParam(IDbConnection con_db, NormTypesSign finder)
        {
            Returns ret = Utils.InitReturns();

            if (finder.nzp_prm_ls > 0)
            {
                ret = CheckNormParam(con_db, finder.nzp_prm_ls, finder.type_val_sign_id);
                if (!ret.result) return ret;
            }
            if (finder.nzp_prm_house > 0)
            {
                ret = CheckNormParam(con_db, finder.nzp_prm_house, finder.type_val_sign_id);
                if (!ret.result) return ret;
            }
            /*
            //проверка для типа справочник - не используется ли в других нормативах в этом же банке в этом же периоде
            if (finder.type_val_sign_id == (int)STypeValSign.Sprav)
            {
                if (finder.nzp_prm_ls > 0)
                {
                    string where = "nzp_prm_ls = " + finder.nzp_prm_ls;
                    ret = CheckOneSpravParam(con_db, finder, where);
                    if (!ret.result) return ret;
                }
                if (finder.nzp_prm_house > 0)
                {
                    string where = "nzp_prm_house = " + finder.nzp_prm_house;
                    ret = CheckOneSpravParam(con_db, finder, where);
                    if (!ret.result) return ret;
                }
            }*/

            return ret;
        }

        private Returns CheckOneSpravParam(IDbConnection con_db, NormTypesSign finder, string where)
        {
            Returns ret = Utils.InitReturns();
            string sql =
                        " SELECT nzp_wp" +
                        " FROM " + Points.Pref + sKernelAliasRest + "norm_banks nb" +
                        " WHERE norm_type_id = " + finder.id;
            DataTable dt = ClassDBUtils.OpenSQL(sql, con_db).resultData;
            foreach (DataRow r in dt.Rows)
            {
                sql = " SELECT nt.id" +
                      " FROM " + Points.Pref + sKernelAliasRest + "norm_types nt," +
                      Points.Pref + sKernelAliasRest + "norm_banks nb, " +
                      Points.Pref + sKernelAliasRest + "norm_types_sign ns " +
                      " WHERE nt.id <> " + finder.id +
                      " AND nt.id = nb.norm_type_id AND nb.nzp_wp = " + r["nzp_wp"] +
                      " AND nt.nzp_serv <> " +
                      " (SELECT nt2.nzp_serv FROM " + Points.Pref + sKernelAliasRest + "norm_types nt2" +
                      " WHERE nt2.id = " + finder.id + ") " +
                      " AND ns.norm_type_id = nt.id AND ns." + where;
                DataTable dtRes = ClassDBUtils.OpenSQL(sql, con_db).resultData;
                if (dtRes.Rows.Count > 0)
                {
                    ret.result = false;
                    ret.text = "Этот справочный параметр используется с другой услугой.";
                    ret.tag = (int)NormKodRet.Error;
                    return ret;
                }
            }
            return ret;
        }

        private Returns CheckNormParam(IDbConnection con_db, int nzp_prm, int type_val_sign_id)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                //проверки для параметров - соответствие типу справочник
                string sql = " SELECT lower(trim(type_prm))" +
                             " FROM " + Points.Pref + sKernelAliasRest + "prm_name" +
                             " WHERE nzp_prm = " + nzp_prm;
                var prmLsType = ExecScalar(con_db, sql, out ret, true);
                if (prmLsType != null && (prmLsType.ToString() == "norm" || prmLsType.ToString() == "sprav") &&
                    type_val_sign_id != (int)STypeValSign.Sprav)
                {
                    ret.result = false;
                    ret.text = "Выбранный параметр имеет тип \"Справочник\". Выберите правильный тип.";
                    ret.tag = (int)NormKodRet.Error;
                    return ret;
                }
                if (prmLsType != null && prmLsType.ToString() == "bool" &&
                    type_val_sign_id != (int)STypeValSign.Boolean)
                {
                    ret.result = false;
                    ret.text = "Выбранный параметр имеет тип \"Да/Нет\". Выберите правильный тип.";
                    ret.tag = (int)NormKodRet.Error;
                    return ret;
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("SaveParamSecondStage : Ошибка добавления параметра норматива " +
                    ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = "Ошибка добавления параметра норматива.";
                ret.tag = (int)NormKodRet.Error;
            }
            return ret;
        }


        /// <summary>
        /// Функция получения числа сочетаний значений параметров
        /// </summary>
        /// <param name="NormTypeId"></param>
        /// <returns></returns>
        public List<NormParamCombination> GetCombinations(NormFinder finder, out Returns ret)
        {
            List<NormTypesSign> listSign = new List<NormTypesSign>();
            ret = Utils.InitReturns();
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("GetCombinations : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return new List<NormParamCombination>();
            }
            int countParams = 0;
            string sql = "SELECT count(*) FROM " + Points.Pref + sKernelAliasRest + "norm_types_sign WHERE norm_type_id=" + finder.NormTypeId;
            var countRows = ExecScalar(conn_db, sql, out ret, true);
            if (countRows != null && countRows != DBNull.Value)
            {
                countParams = Convert.ToInt32(countRows);
            }

            //если размерность <=2 
            if (countParams <= 2)
            {
                ret.tag = 1;
                return new List<NormParamCombination>();
            }

            sql = "SELECT * FROM " + Points.Pref + sKernelAliasRest + "norm_types_sign WHERE norm_type_id=" + finder.NormTypeId + " and ordering>2";
            var DT = ClassDBUtils.OpenSQL(sql.ToString(), conn_db).resultData;
            for (int i = 0; i < DT.Rows.Count; i++)
            {
                NormTypesSign normSign = new NormTypesSign();
                normSign.id = (DT.Rows[i]["id"] != DBNull.Value ? Convert.ToInt32(DT.Rows[i]["id"]) : 0);
                normSign.ordering = (DT.Rows[i]["ordering"] != DBNull.Value ? Convert.ToInt32(DT.Rows[i]["ordering"]) : 0);
                normSign.nzp_prm_ls = (DT.Rows[i]["nzp_prm_ls"] != DBNull.Value ? Convert.ToInt32(DT.Rows[i]["nzp_prm_ls"]) : 0);
                normSign.nzp_prm_house = (DT.Rows[i]["nzp_prm_house"] != DBNull.Value ? Convert.ToInt32(DT.Rows[i]["nzp_prm_house"]) : 0);
                normSign.type_val_sign_id = (DT.Rows[i]["type_val_sign_id"] != DBNull.Value ? Convert.ToInt32(DT.Rows[i]["type_val_sign_id"]) : 0);
                listSign.Add(normSign);
            }

            DataTable DT1 = null;
            List<NormParamCombination>[] Combinations = new List<NormParamCombination>[listSign.Count()];
            for (int k = 0; k < Combinations.Count(); k++)
            {
                Combinations[k] = new List<NormParamCombination>();
            }


            for (int i = 0; i < listSign.Count(); i++)
            {

                #region Получение комбинаций значений параметров
                switch (listSign[i].type_val_sign_id)
                {
                    case (int)STypeValSign.Sprav:
                        {
                            sql = "SELECT i.param_value1,i.ordering, p.name_prm,i.nzp_prm,s.type_val_sign_id,i.name_prm_val  " +
                              " FROM " + Points.Pref + sKernelAliasRest + "norm_types n1, " + Points.Pref + sKernelAliasRest + "norm_tables n2, " + Points.Pref + sKernelAliasRest + "influence_params i," +
                            Points.Pref + sKernelAliasRest + "prm_name p, " + Points.Pref + sKernelAliasRest + "norm_types_sign s   " +
                            " WHERE n1.id=" + finder.NormTypeId + " and n1.id=n2.norm_type_id and n2.id=i.norm_tables_id and i.nzp_prm="
                             + (listSign[i].nzp_prm_ls > 0 ? listSign[i].nzp_prm_ls : listSign[i].nzp_prm_house) + "and p.nzp_prm=i.nzp_prm and s.norm_type_id=n1.id " +
                             "and (s.nzp_prm_ls=" + listSign[i].nzp_prm_ls + " and s.nzp_prm_house=" + listSign[i].nzp_prm_house + ") " +
                             " GROUP BY 1,2,3,4,5,6 ORDER BY i.ordering";
                            break;
                        }
                    case (int)STypeValSign.Num:
                    case (int)STypeValSign.Boolean:
                        {
                            sql = "SELECT i.param_value1,i.ordering, p.name_prm,i.nzp_prm,s.type_val_sign_id  " +
                              " FROM " + Points.Pref + sKernelAliasRest + "norm_types n1, " + Points.Pref + sKernelAliasRest + "norm_tables n2, " + Points.Pref + sKernelAliasRest + "influence_params i," +
                            Points.Pref + sKernelAliasRest + "prm_name p, " + Points.Pref + sKernelAliasRest + "norm_types_sign s   " +
                            " WHERE n1.id=" + finder.NormTypeId + " and n1.id=n2.norm_type_id and n2.id=i.norm_tables_id and i.nzp_prm="
                             + (listSign[i].nzp_prm_ls > 0 ? listSign[i].nzp_prm_ls : listSign[i].nzp_prm_house) + "and p.nzp_prm=i.nzp_prm and s.norm_type_id=n1.id " +
                             "and (s.nzp_prm_ls=" + listSign[i].nzp_prm_ls + " and s.nzp_prm_house=" + listSign[i].nzp_prm_house + ") " +
                             " GROUP BY 1,2,3,4,5 ORDER BY i.ordering";
                            break;
                        }
                    case (int)STypeValSign.NumPeriod:
                        {
                            sql = "SELECT i.param_value1,i.param_value2,i.ordering, p.name_prm,i.nzp_prm,s.type_val_sign_id  " +
                           " FROM " + Points.Pref + sKernelAliasRest + "norm_types n1, " + Points.Pref + sKernelAliasRest + "norm_tables n2, " + Points.Pref + sKernelAliasRest + "influence_params i," +
                          Points.Pref + sKernelAliasRest + "prm_name p, " + Points.Pref + sKernelAliasRest + "norm_types_sign s   " +
                           " WHERE n1.id=" + finder.NormTypeId + " and n1.id=n2.norm_type_id and n2.id=i.norm_tables_id and i.nzp_prm="
                           + (listSign[i].nzp_prm_ls > 0 ? listSign[i].nzp_prm_ls : listSign[i].nzp_prm_house) + "and p.nzp_prm=i.nzp_prm and s.norm_type_id=n1.id " +
                           "and (s.nzp_prm_ls=" + listSign[i].nzp_prm_ls + " and s.nzp_prm_house=" + listSign[i].nzp_prm_house + ") " +
                           " GROUP BY 1,2,3,4,5,6 ORDER BY i.ordering";
                            break;
                        }
                    case (int)STypeValSign.Date:
                        {
                            sql = "SELECT i.date_value1,i.ordering, p.name_prm,i.nzp_prm,s.type_val_sign_id  " +
                           " FROM " + Points.Pref + sKernelAliasRest + "norm_types n1, " + Points.Pref + sKernelAliasRest + "norm_tables n2, " + Points.Pref + sKernelAliasRest + "influence_params i," +
                          Points.Pref + sKernelAliasRest + "prm_name p, " + Points.Pref + sKernelAliasRest + "norm_types_sign s   " +
                           " WHERE n1.id=" + finder.NormTypeId + " and n1.id=n2.norm_type_id and n2.id=i.norm_tables_id and i.nzp_prm="
                           + (listSign[i].nzp_prm_ls > 0 ? listSign[i].nzp_prm_ls : listSign[i].nzp_prm_house) + "and p.nzp_prm=i.nzp_prm and s.norm_type_id=n1.id " +
                           "and (s.nzp_prm_ls=" + listSign[i].nzp_prm_ls + " and s.nzp_prm_house=" + listSign[i].nzp_prm_house + ") " +
                           " GROUP BY 1,2,3,4,5 ORDER BY i.ordering";
                            break;
                        }
                    case (int)STypeValSign.Period:
                        {
                            sql = "SELECT i.date_value1,i.date_value2,i.ordering, p.name_prm,i.nzp_prm,s.type_val_sign_id  " +
                                 " FROM " + Points.Pref + sKernelAliasRest + "norm_types n1, " + Points.Pref + sKernelAliasRest + "norm_tables n2, " + Points.Pref + sKernelAliasRest + "influence_params i," +
                                Points.Pref + sKernelAliasRest + "prm_name p, " + Points.Pref + sKernelAliasRest + "norm_types_sign s   " +
                                 " WHERE n1.id=" + finder.NormTypeId + " and n1.id=n2.norm_type_id and n2.id=i.norm_tables_id and i.nzp_prm="
                                 + (listSign[i].nzp_prm_ls > 0 ? listSign[i].nzp_prm_ls : listSign[i].nzp_prm_house) + "and p.nzp_prm=i.nzp_prm and s.norm_type_id=n1.id " +
                                 "and (s.nzp_prm_ls=" + listSign[i].nzp_prm_ls + " and s.nzp_prm_house=" + listSign[i].nzp_prm_house + ") " +
                                 " GROUP BY 1,2,3,4,5,6 ORDER BY i.ordering";
                            break;
                        }

                }
                DT1 = ClassDBUtils.OpenSQL(sql.ToString(), conn_db).resultData;
                #endregion


                #region Обработка комбинаций значений параметров
                for (int j = 0; j < DT1.Rows.Count; j++)
                {
                    int type_val_sign_id = Convert.ToInt32(DT1.Rows[j]["type_val_sign_id"]);
                    NormParamCombination combination = new NormParamCombination();
                    switch (type_val_sign_id)
                    {
                        case (int)STypeValSign.Sprav:
                            {
                                combination.param_value = (DT1.Rows[j]["name_prm_val"] != DBNull.Value ? DT1.Rows[j]["name_prm_val"].ToString().Trim() : "");
                                combination.param_value1 = (DT1.Rows[j]["param_value1"] != DBNull.Value ? Convert.ToDouble(DT1.Rows[j]["param_value1"]) : 0);
                                break;
                            }
                        case (int)STypeValSign.Num:
                            {
                                combination.param_value = (DT1.Rows[j]["param_value1"] != DBNull.Value ? Convert.ToDouble(DT1.Rows[j]["param_value1"]).ToString().Trim() : "");
                                combination.param_value1 = (DT1.Rows[j]["param_value1"] != DBNull.Value ? Convert.ToDouble(DT1.Rows[j]["param_value1"]) : 0);
                                break;
                            }
                        case (int)STypeValSign.Boolean:
                            {
                                combination.param_value = (DT1.Rows[j]["param_value1"] != DBNull.Value ? (Convert.ToInt32(DT1.Rows[j]["param_value1"]) == 0 ? "Нет" : "Да") : "");
                                combination.param_value1 = (DT1.Rows[j]["param_value1"] != DBNull.Value ? Convert.ToDouble(DT1.Rows[j]["param_value1"]) : 0);
                                break;
                            }
                        case (int)STypeValSign.NumPeriod:
                            {
                                double from = (DT1.Rows[j]["param_value1"] != DBNull.Value ? Convert.ToDouble(DT1.Rows[j]["param_value1"]) : 0);
                                double to = (DT1.Rows[j]["param_value2"] != DBNull.Value ? Convert.ToDouble(DT1.Rows[j]["param_value2"]) : 0);
                                combination.param_value = from + " - " + to;
                                combination.param_value1 = (DT1.Rows[j]["param_value1"] != DBNull.Value ? Convert.ToDouble(DT1.Rows[j]["param_value1"]) : 0);
                                combination.param_value2 = (DT1.Rows[j]["param_value2"] != DBNull.Value ? Convert.ToDouble(DT1.Rows[j]["param_value2"]) : 0);
                                break;
                            }
                        case (int)STypeValSign.Date:
                            {
                                combination.param_value = (DT1.Rows[j]["date_value1"] != DBNull.Value ? Convert.ToDateTime(DT1.Rows[j]["date_value1"]).ToString("dd.MM.yyyy") : "");
                                combination.date_value1 = (DT1.Rows[j]["date_value1"] != DBNull.Value ? Convert.ToDateTime(DT1.Rows[j]["date_value1"]) : DateTime.MinValue);
                                break;
                            }
                        case (int)STypeValSign.Period:
                            {
                                DateTime from = (DT1.Rows[j]["date_value1"] != DBNull.Value ? Convert.ToDateTime(DT1.Rows[j]["date_value1"]) : DateTime.MinValue);
                                DateTime to = (DT1.Rows[j]["date_value2"] != DBNull.Value ? Convert.ToDateTime(DT1.Rows[j]["date_value2"]) : DateTime.MinValue);
                                combination.param_value = from.ToShortDateString() + " - " + to.ToShortDateString();
                                combination.date_value1 = (DT1.Rows[j]["date_value1"] != DBNull.Value ? Convert.ToDateTime(DT1.Rows[j]["date_value1"]) : DateTime.MinValue);
                                combination.date_value2 = (DT1.Rows[j]["date_value2"] != DBNull.Value ? Convert.ToDateTime(DT1.Rows[j]["date_value2"]) : DateTime.MinValue);
                                break;
                            }

                    }
                    combination.type_val_sign = type_val_sign_id;
                    combination.nzp_prm = (DT1.Rows[j]["nzp_prm"] != DBNull.Value ? Convert.ToInt32(DT1.Rows[j]["nzp_prm"]) : 0);
                    combination.ordering = (DT1.Rows[j]["ordering"] != DBNull.Value ? Convert.ToInt32(DT1.Rows[j]["ordering"]) : 0);
                    combination.name_prm = (DT1.Rows[j]["name_prm"] != DBNull.Value ? DT1.Rows[j]["name_prm"].ToString().Trim() : "");
                    Combinations[i].Add(combination);
                }
                #endregion


            }
            var x = AllCombinationsOf(Combinations);
            ret.tag = x.Count();

            return x[(finder.PageNumber <= 0 ? 0 : finder.PageNumber - 1)];
        }

        /// <summary>
        /// Получение набора комбинаций значений параметров
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sets"></param>
        /// <returns></returns>
        public static List<List<T>> AllCombinationsOf<T>(List<T>[] sets)
        {
            var combinations = new List<List<T>>();

            foreach (var value in sets[0])
                combinations.Add(new List<T> { value });

            foreach (var set in sets.Skip(1))
                combinations = AddExtraSet(combinations, set);

            return combinations;
        }

        private static List<List<T>> AddExtraSet<T>
             (List<List<T>> combinations, List<T> set)
        {
            var newCombinations = from value in set
                                  from combination in combinations
                                  select new List<T>(combination) { value };

            return newCombinations.ToList();
        }


        public DataTable GetNormatives(NormFinder finder, out Returns ret)
        {
            return GetNormatives(finder, out  ret, false);
        }

        public DataTable GetNormatives(NormFinder finder, out Returns ret, bool isExtented)
        {
            DataTable ResTable = new DataTable() { TableName = "Нормативы" };
            DataTable ExtentedTable = new DataTable() { TableName = "ExtentedTable" };
            ret = Utils.InitReturns();
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("GetNormatives : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return new DataTable();
            }
            List<NormParamCombination> listValPrm = GetCombinations(finder, out ret);
            if (!ret.result)
            {
                MonitorLog.WriteLog("GetNormatives : Ошибка при получении значений параметров влияющих на нормативы", MonitorLog.typelog.Error, true);
                return new DataTable();
            }

            string tempTableName = "t_" + DateTime.Now.Ticks;
            string sql = "";

            sql = " DROP TABLE " + tempTableName;
            ret = ExecSQL(conn_db, sql, false);

#if PG
            sql = "SELECT t.*,i.norm_tables_id,i.nzp_prm,param_value1,param_value2,date_value1,date_value2,i.ordering,i.name_prm_val  " +
                  "INTO TEMP " + tempTableName +
#else
            sql = "SELECT t.*,i.norm_tables_id,i.nzp_prm,param_value1,param_value2,date_value1,date_value2,i.ordering,i.name_prm_val  " +
#endif

 " FROM " + Points.Pref + sKernelAliasRest + "norm_tables t, " + Points.Pref + sKernelAliasRest + "influence_params i " +

#if PG
 " WHERE t.id=i.norm_tables_id and t.norm_type_id=" + finder.NormTypeId;
#else
            " WHERE t.id=i.norm_tables_id and t.norm_type_id=" + finder.NormTypeId + "  INTO TEMP  "+tempTableName;
#endif


            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return ResTable;
            }
            //оставляем только нужные нормативы
            for (int i = 0; i < listValPrm.Count(); i++)
            {
                switch (listValPrm[i].type_val_sign)
                {
                    case (int)STypeValSign.Num:
                    case (int)STypeValSign.Sprav:
                    case (int)STypeValSign.Boolean:
                        {
                            sql = "DELETE FROM " + tempTableName +
                            " WHERE id in (SELECT id FROM " + tempTableName + " where nzp_prm=" + listValPrm[i].nzp_prm + " and param_value1<>" + listValPrm[i].param_value1 + ")";
                            break;
                        }
                    case (int)STypeValSign.NumPeriod:
                        {
                            sql = "DELETE FROM " + tempTableName +
                           " WHERE id in (SELECT id FROM " + tempTableName + " where nzp_prm=" + listValPrm[i].nzp_prm +
                           " and param_value1<>" + listValPrm[i].param_value1 + " and param_value2<>" + listValPrm[i].param_value2 + ")";
                            break;
                        }
                    case (int)STypeValSign.Date:
                        {
                            sql = "DELETE FROM " + tempTableName +
                         " WHERE id in (SELECT id FROM " + tempTableName + " where nzp_prm=" + listValPrm[i].nzp_prm + " and date_value1<>" + Utils.EStrNull(listValPrm[i].date_value1.ToShortDateString()) + ")";
                            break;
                        }
                    case (int)STypeValSign.Period:
                        {
                            sql = "DELETE FROM " + tempTableName +
                       " WHERE id in (SELECT id FROM " + tempTableName + " where nzp_prm=" + listValPrm[i].nzp_prm + " and date_value1<>"
                       + Utils.EStrNull(listValPrm[i].date_value1.ToShortDateString()) + " and date_value2<>" + Utils.EStrNull(listValPrm[i].date_value2.ToShortDateString()) + ")";
                            break;
                        }
                }
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return ResTable;
                }

            }

            //получаем параметры для таблицы нормативов 
            Dictionary<int, int> TwoPrms = new Dictionary<int, int>();

            string prms = string.Join(",", listValPrm.Select(x => x.nzp_prm.ToString()).ToArray());
            if (listValPrm.Count == 0) prms = "-1";
            sql = " SELECT DISTINCT nzp_prm  FROM " + tempTableName + " WHERE nzp_prm NOT IN (" + prms + ")";
            //получили список отображаемых в таблице параметров
            var DT = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            for (int i = 0; i < DT.Rows.Count; i++)
            {
                int nzp_prm = (DT.Rows[i]["nzp_prm"] != DBNull.Value ? Convert.ToInt32(DT.Rows[i]["nzp_prm"]) : 0);
                sql = "SELECT ordering FROM " + Points.Pref + sKernelAliasRest + "norm_types_sign WHERE norm_type_id=" + finder.NormTypeId + " and (nzp_prm_ls=" + nzp_prm + " or nzp_prm_house=" + nzp_prm + ")";
                var obj = ExecScalar(conn_db, sql, out ret, true);
                if (!ret.result)
                {
                    return ResTable;
                }
                if (obj != null && obj != DBNull.Value)
                {
                    TwoPrms.Add(Convert.ToInt32(obj), nzp_prm);
                }
            }

            if (TwoPrms.Count > 1)
            {
                //получаем названия столбцов
                sql = "SELECT DISTINCT i.name_prm_val,i.ordering " +
                " FROM " + Points.Pref + sKernelAliasRest + "norm_tables t, " + Points.Pref + sKernelAliasRest + "influence_params i " +
                " WHERE t.id=i.norm_tables_id and t.norm_type_id=" + finder.NormTypeId + " and i.nzp_prm=" + TwoPrms[1] +
                " ORDER BY i.ordering";
                var DTCols = ClassDBUtils.OpenSQL(sql, conn_db).resultData;

                ResTable.Columns.Add(TwoPrms.Count > 1
                    ? new DataColumn() { ColumnName = "Строки\\Столбцы", Caption = "Строки\\Столбцы" }
                    : new DataColumn() { ColumnName = "Строки" });
                for (int i = 0; i < DTCols.Rows.Count; i++)
                {
                    DataColumn col = new DataColumn();
                    col.Caption = DTCols.Rows[i]["name_prm_val"] != DBNull.Value ? (DTCols.Rows[i]["name_prm_val"]).ToString() : "";
                    col.ColumnName = DTCols.Rows[i]["ordering"] != DBNull.Value ? (DTCols.Rows[i]["ordering"]).ToString() : "";
                    ResTable.Columns.Add(col);
                }
            }
            else
            {
                DataColumn col = new DataColumn();
                col.ColumnName = "Строки";
                ResTable.Columns.Add(col);
                col = new DataColumn();
                col.ColumnName = "1";
                col.Caption = "Значения";
                ResTable.Columns.Add(col);
            }


            //формируем строки
            int nzp = 0;
            //если число параметров >1 
            if (TwoPrms.Count > 1)
            {
                nzp = TwoPrms[2];
            }
            else
            {
                if (TwoPrms.Count > 0)
                    nzp = TwoPrms[1];
                else
                {
                    /*  DataRow row = ResTable.NewRow();
                    row[1] = "Нормативы не заданы";
                     ResTable.Rows.Add(row);*/
                    return ResTable;
                }
            }

            sql = "SELECT DISTINCT i.name_prm_val,i.ordering,i.param_value1,param_value2, date_value1, date_value2 " +
          " FROM " + Points.Pref + sKernelAliasRest + "norm_tables t, " + Points.Pref + sKernelAliasRest + "influence_params i " +
          " WHERE t.id=i.norm_tables_id and t.norm_type_id=" + finder.NormTypeId + " and i.nzp_prm=" + nzp +
          " ORDER BY i.ordering";



            var DTRows = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            for (int i = 0; i < DTRows.Rows.Count; i++)
            {
                DataRow row = ResTable.NewRow();
                row[0] = DTRows.Rows[i]["name_prm_val"] != DBNull.Value ? (DTRows.Rows[i]["name_prm_val"]).ToString() : "";
                ResTable.Rows.Add(row);
            }


            int type_id = -1;
            sql = "SELECT type_val_sign_id FROM " + Points.Pref + sKernelAliasRest + "norm_types_sign WHERE norm_type_id=" +
                 finder.NormTypeId + " and (nzp_prm_ls=" + nzp + " or nzp_prm_house=" + nzp + ")";
            var type = ExecScalar(conn_db, sql, out ret, true);
            if (!ret.result)
            {
                return ResTable;
            }
            if (type != null && type != DBNull.Value)
            {
                type_id = Convert.ToInt32(type);
            }

            for (int j = 0; j < DTRows.Rows.Count; j++)
            {
                #region Условие на значения параметра-строки в зависимости от типа параметра
                string where_val_prm = "";
                switch (type_id)
                {
                    case (int)STypeValSign.Num:
                    case (int)STypeValSign.Boolean:
                    case (int)STypeValSign.Sprav:
                        {
                            where_val_prm = "i.param_value1=" + (DTRows.Rows[j]["param_value1"] != DBNull.Value ? DTRows.Rows[j]["param_value1"].ToString().Trim() : "-1");
                            break;
                        }
                    case (int)STypeValSign.NumPeriod:
                        {
                            where_val_prm = "i.param_value1=" + (DTRows.Rows[j]["param_value1"] != DBNull.Value ? DTRows.Rows[j]["param_value1"].ToString().Trim() : "-1") +
                            " and i.param_value2=" + (DTRows.Rows[j]["param_value2"] != DBNull.Value ? DTRows.Rows[j]["param_value2"].ToString().Trim() : "-1");
                            break;
                        }
                    case (int)STypeValSign.Date:
                        {
                            where_val_prm = "i.date_value1=" +
                                Utils.EStrNull((DTRows.Rows[j]["date_value1"] != DBNull.Value ?
                                Convert.ToDateTime(DTRows.Rows[j]["date_value1"]).ToShortDateString() :
                                DateTime.MinValue.ToShortDateString()));
                            break;
                        }
                    case (int)STypeValSign.Period:
                        {
                            where_val_prm = "i.date_value1=" +
                            Utils.EStrNull((DTRows.Rows[j]["date_value1"] != DBNull.Value ?
                            Convert.ToDateTime(DTRows.Rows[j]["date_value1"]).ToShortDateString() :
                           DateTime.MinValue.ToShortDateString())) +
                           " and i.date_value2=" +
                            Utils.EStrNull((DTRows.Rows[j]["date_value2"] != DBNull.Value ?
                            Convert.ToDateTime(DTRows.Rows[j]["date_value2"]).ToShortDateString() :
                           DateTime.MinValue.ToShortDateString()));
                            break;
                        }
                }
                #endregion

                if (TwoPrms.Count > 1)
                {
                    sql = "SELECT t.id, ii.ordering,max(i.name_prm_val) as row_name,max(ii.name_prm_val) as col_name,t.norm_value " +
                    " FROM " + tempTableName + " t," + tempTableName + " i, " + tempTableName + " ii " +
                    " WHERE t.id=i.norm_tables_id and t.norm_type_id=" + finder.NormTypeId + " and t.id=ii.norm_tables_id and " +
                    " (i.nzp_prm=" + TwoPrms[2] + " and  " + where_val_prm + ") and ii.nzp_prm=" + TwoPrms[1] + " " +
                    " GROUP BY t.id,t.norm_value, i.ordering, ii.ordering  " +
                    " ORDER BY i.ordering, ii.ordering  ";
                }
                else
                {
                    sql = "SELECT t.id, i.ordering,max(i.name_prm_val) as row_name,t.norm_value " +
                  " FROM " + tempTableName + " t," + tempTableName + " i" +
                  " WHERE t.id=i.norm_tables_id and t.norm_type_id=" + finder.NormTypeId + " and " +
                  " (i.nzp_prm=" + TwoPrms[1] + " and  " + where_val_prm + ") " +
                  " GROUP BY t.id,t.norm_value, i.ordering " +
                  " ORDER BY i.ordering ";
                }

                var DTNormRow = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                if (isExtented)
                {
                    if (ExtentedTable.Columns.Count == 0)
                    {
                        ExtentedTable = DTNormRow;
                    }
                    else
                    {
                        foreach (DataRow row in DTNormRow.Rows)
                        {
                            var array = row.ItemArray;
                            ExtentedTable.Rows.Add(array);
                        }
                    }
                }
                for (int c = 0; c < DTNormRow.Rows.Count; c++)
                {
                    //столбец с индексом 0 зарезервирован под название строки
                    ResTable.Rows[j][c + 1] = DTNormRow.Rows[c]["norm_value"] != DBNull.Value ? Convert.ToDouble(DTNormRow.Rows[c]["norm_value"]).ToString() : "";
                }
            }
            
            sql = " DROP TABLE " + tempTableName;
            ExecSQL(conn_db, sql, false);

            //возвращаем число влияющих параметров в двумерной таблице
            ret.tag = TwoPrms.Count;
            return (!isExtented ? ResTable : ExtentedTable);
        }

        public Returns SaveNormatives(NormFinder finder)
        {
            Returns ret = Utils.InitReturns();
            #region проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return ret;
            }
            #endregion

            string sql = "";
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("GetCombinations : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return ret;
            }

            var oldValues = new DataTable();
            var oldValuesExt = new DataTable();
            /*var listValPrm = GetCombinations(finder, out ret);
            if (!ret.result)
            {
                MonitorLog.WriteLog("GetNormatives : Ошибка при получении значений параметров влияющих на нормативы", MonitorLog.typelog.Error, true);
                return ret;
            }*/
            var listChangedValues = new List<NewNormValue>();

            oldValues = GetNormatives(finder, out ret);
            for (int i = 0; i < finder.DT.Rows.Count; i++)
            {
                for (int j = 0; j < finder.DT.Columns.Count; j++)
                {
                    //сравнением находим измененные элементы
                    //oldValues.Rows[i][j + 1] - не сравниваем первый столбец (название строк)
                    if (finder.DT.Rows[i][j].ToString() != oldValues.Rows[i][j + 1].ToString())
                    {
                        NewNormValue norm = new NewNormValue();
                        norm.ordering_x = i;
                        norm.ordering_y = Convert.ToInt32(oldValues.Columns[j + 1].ColumnName);
                        norm.new_value = Convert.ToDouble(finder.DT.Rows[i][j]);
                        norm.old_value = Convert.ToDouble(oldValues.Rows[i][j + 1]);
                        listChangedValues.Add(norm);
                    }
                }
            }
            //получаем расширенные данные по нормативам (id,ordering)
            oldValuesExt = GetNormatives(finder, out ret, true);
            int count_prms = ret.tag;
            int idUpdatingRow = 0;
            foreach (NewNormValue norm in listChangedValues)
            {
                //при одном влияющем параметре используем координаты только по оси y
                var updRow = oldValuesExt.AsEnumerable().ToList();
                //если параметров больше одного добавляется ось координат x;
                if (count_prms > 1)
                {
                    updRow = oldValuesExt.AsEnumerable().Where(r => Convert.ToInt32(r["ordering"]) == norm.ordering_y).ToList();
                }
                if (updRow.Count >= norm.ordering_x)
                    idUpdatingRow = (updRow[norm.ordering_x]["id"] != DBNull.Value ? Convert.ToInt32(updRow[norm.ordering_x]["id"]) : 0);


                if (idUpdatingRow > 0)
                {
                    //подставляем новое значение норматива
                    sql = "UPDATE " + Points.Pref + sKernelAliasRest + "norm_tables SET (norm_value,changed_by,changed_on)= " +
                        "(" + norm.new_value + "," + finder.nzp_user + ",'" + DateTime.Now.ToShortDateString() + "') where id=" + idUpdatingRow;
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        return ret;
                    }
                }
            }

            return ret;
        }

        public Returns InsertNewNormValues(NormFinder finder, DataSet NewNorms)
        {
            Returns ret = Utils.InitReturns();
            #region проверка входных параметров
            if (!(NewNorms.Tables.Count > 0))
            {
                ret.result = false;
                return ret;
            }
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return ret;
            }
            #endregion

            string sql = "";
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("InsertNewNormValues : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return ret;
            }

            //вызываем перенумерацию ordering по числу параметров
            ret = ToOrderingNormSign(finder, conn_db);

            sql = "SELECT * FROM " + Points.Pref + sKernelAliasRest + "norm_types_sign where norm_type_id=" + finder.NormTypeId;
            var DTParams = ClassDBUtils.OpenSQL(sql, conn_db).resultData as DataTable;
            if (DTParams != null)
            {
                int countParams = DTParams.Rows.Count;
                if (countParams != NewNorms.Tables.Count)
                {
                    ret.result = false;
                    ret.text = "Число параметров не совпадает с заявленным!";
                    return ret;
                }
            }

            var ParamsSet = new List<NormParamCombination>[NewNorms.Tables.Count];
            for (int k = 0; k < ParamsSet.Count(); k++)
            {
                ParamsSet[k] = new List<NormParamCombination>();
            }

            for (int i = 0; i < NewNorms.Tables.Count; i++)
            {
                DataTable DT = NewNorms.Tables[i];
                //получаем id параметра
                int norm_type_sign_id = Convert.ToInt32(DT.TableName);

                DataRow row_param = DTParams.Select("id=" + norm_type_sign_id).First();
                int type_val_sign_id = Convert.ToInt32(row_param["type_val_sign_id"]);
                int nzp_prm = (Convert.ToInt32(row_param["nzp_prm_ls"]) > 0 ? Convert.ToInt32(row_param["nzp_prm_ls"]) : Convert.ToInt32(row_param["nzp_prm_house"]));

                //Создаем list с набором значений этого параметра
                var Params = new List<NormParamCombination>();
                for (int j = 0; j < DT.Rows.Count; j++)
                {
                    var param_value = new NormParamCombination();
                    #region Добавление значений параметров в зависимости от типа
                    switch (type_val_sign_id)
                    {
                        case (int)STypeValSign.Boolean:
                            {
                                param_value.param_value1 = (DT.Rows[j]["val1"].ToString().Trim() == "Нет" ? 0 : 1);
                                break;
                            }
                        case (int)STypeValSign.Num:
                        case (int)STypeValSign.Sprav:
                            {
                                param_value.param_value1 = Convert.ToDouble(DT.Rows[j]["val1"]);
                                break;
                            }
                        case (int)STypeValSign.Date:
                            {
                                param_value.date_value1 = Convert.ToDateTime(DT.Rows[j]["val1"]);
                                break;
                            }
                        case (int)STypeValSign.NumPeriod:
                            {
                                param_value.param_value1 = Convert.ToDouble(DT.Rows[j]["val1"]);
                                param_value.param_value2 = Convert.ToDouble(DT.Rows[j]["val2"]);
                                break;
                            }
                        case (int)STypeValSign.Period:
                            {
                                param_value.date_value1 = Convert.ToDateTime(DT.Rows[j]["val1"]);
                                param_value.date_value2 = Convert.ToDateTime(DT.Rows[j]["val2"]);
                                break;
                            }
                    }
                    #endregion
                    param_value.name_prm = DT.Rows[j]["valName"].ToString();
                    param_value.nzp_prm = nzp_prm;
                    param_value.ordering = j + 1;
                    param_value.type_val_sign = type_val_sign_id;
                    Params.Add(param_value);
                }
                ParamsSet[i] = Params;
            }
            //получаем все комбинации значений параметров
            var AllCombination = AllCombinationsOf(ParamsSet);
            foreach (List<NormParamCombination> combination in AllCombination)
            {
                //Делаем запись-норматив =0
                sql = "INSERT INTO " + Points.Pref + sKernelAliasRest + "norm_tables (norm_type_id,norm_value,created_on) VALUES " +
                    "(" + finder.NormTypeId + ",0,'" + DateTime.Now.ToShortDateString() + "')";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return ret;
                }
                int norm_tables_id = GetSerialValue(conn_db);
                for (int c = 0; c < combination.Count; c++)
                {
                    string values_str = "";
                    string value = "";

                    #region Формируем набор записываемых парметров в зависимости от типа
                    switch (combination[c].type_val_sign)
                    {
                        case (int)STypeValSign.Boolean:
                        case (int)STypeValSign.Num:
                        case (int)STypeValSign.Sprav:
                            {
                                values_str = "param_value1";
                                value = combination[c].param_value1.ToString();
                                break;
                            }
                        case (int)STypeValSign.Date:
                            {
                                values_str = "date_value1";
                                value = "'" + combination[c].date_value1.ToString() + "'";
                                break;
                            }
                        case (int)STypeValSign.NumPeriod:
                            {
                                values_str = "param_value1,param_value2";
                                value = string.Join(",", new string[2] { combination[c].param_value1.ToString(), combination[c].param_value2.ToString() });
                                break;
                            }
                        case (int)STypeValSign.Period:
                            {
                                values_str = "date_value1,date_value2";
                                value = string.Join(",", new string[2] { Utils.EStrNull(combination[c].date_value1.ToShortDateString()),
                                    Utils.EStrNull(combination[c].date_value2.ToShortDateString()) });
                                break;
                            }
                    }
                    #endregion

                    sql = "INSERT INTO " + Points.Pref + sKernelAliasRest + "influence_params" +
                          " (norm_tables_id,nzp_prm," + values_str + ",name_prm_val,ordering,created_on) VALUES " +
                          "(" + norm_tables_id + "," + combination[c].nzp_prm + "," + value + ",'" + combination[c].name_prm +
                          "'," + combination[c].ordering + ",'" + DateTime.Now.ToShortDateString() + "')";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        return ret;
                    }
                }
                //Связанные с ним строки-параметры
            }

            return ret;
        }

        /// <summary>
        /// Проставляем ordering для параметров по числу значений параметров.
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        private Returns ToOrderingNormSign(NormFinder finder, IDbConnection conn_db)
        {
            Returns ret = Utils.InitReturns();
            if (finder.NormTypeId > 0)
            {
#if PG
                string sql = " UPDATE " + Points.Pref + sKernelAliasRest + "norm_types_sign SET ordering=(" +
                             " SELECT rownum FROM (SELECT row_number() over (ORDER BY max_count DESC) as rownum, id " +
                             " FROM  " + Points.Pref + sKernelAliasRest + "norm_types_sign n where n.norm_type_id=" + finder.NormTypeId + ") t2 " +
                             " WHERE " + Points.Pref + sKernelAliasRest + "norm_types_sign.id=t2.id ) " +
                             " WHERE norm_type_id=" + finder.NormTypeId;
                ret = ExecSQL(conn_db, sql, true);
#endif
            }
            return ret;
        }

        public NormParam GetTypePrmIdByNzpPrm(string nzp_prm, int normId, out Returns ret)
        {
            NormParam np = new NormParam();
            ret = Utils.InitReturns();
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().Name +
                    " : Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                return np;
            }
            try
            {
                string sql =
                    " SELECT s.id as id_type_val, n.*" +
                    " FROM " + Points.Pref + sKernelAliasRest + "s_type_val_sign s," +
                    Points.Pref + sKernelAliasRest + "norm_types_sign n" +
                    " WHERE n.type_val_sign_id = s.id" +
                    " AND n.norm_type_id = " + normId +
                    " AND n.id = " + nzp_prm + "";
                DataTable dt = DBManager.ExecSQLToTable(conn_db, sql);
                if (dt.Rows.Count == 0)
                {
                    MonitorLog.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().Name +
                    " : Не найдена запись для параметра с nzp_prm = " + nzp_prm + " и норматива с id = " + normId,
                    MonitorLog.typelog.Error, true);
                    return np;
                }
                if (dt.Rows.Count > 1)
                {
                    MonitorLog.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().Name +
                    " : Найдено более 1 записи для параметра с nzp_prm = " + nzp_prm + " и норматива с id = " + normId,
                    MonitorLog.typelog.Error, true);
                    return np;
                }
                if (!String.IsNullOrEmpty(dt.Rows[0]["min_val"].ToString()))
                    np.min_val = dt.Rows[0]["min_val"].ToString();
                if (!String.IsNullOrEmpty(dt.Rows[0]["max_val"].ToString()))
                    np.max_val = dt.Rows[0]["max_val"].ToString();
                int id_type_val;
                if (!Int32.TryParse(dt.Rows[0]["id_type_val"].ToString(), out id_type_val)) ret.result = false;
                np.id_type_val = id_type_val;
                int max_amount;
                if (Int32.TryParse(dt.Rows[0]["max_count"].ToString(), out max_amount)) np.max_amount = max_amount;
                if ((np.id_type_val != (int) STypeValSign.Sprav) &&
                    (String.IsNullOrEmpty(dt.Rows[0]["min_val"].ToString())
                     || String.IsNullOrEmpty(dt.Rows[0]["max_val"].ToString()) || max_amount < 1)) ret.result = false;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().Name +
                    " : Ошибка получения типа значения параметра для параметра " + nzp_prm + " и норматива с id = " + normId, 
                    MonitorLog.typelog.Error, true);
                return np;
            }
            finally
            {
               conn_db.Close();
            }
            return np;
        }
    } //end class

} //end namespace