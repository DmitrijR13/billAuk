using System;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Text;
//using System.Text.RegularExpressions;
using System.Collections.Generic;
using STCLINE.KP50.Client;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Globalization;
using System.Threading;

namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class DbCounterClient : DataBaseHead
    //----------------------------------------------------------------------
    {
        /// <summary> Получить список ПУ из кэш'а
        /// </summary>
        public List<Counter> GetPu(Counter finder, out Returns ret) //вытащить ПУ для грида
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Не определен пользователь";
                return null;
            }

            List<Counter> Spis = new List<Counter>();

            Spis.Clear();

            //выбрать общее кол-во
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string tXX_cnt = sDefaultSchema + "t" + Convert.ToString(finder.nzp_user) + "_pu";

            if (finder.nzp_kvar > 0 || finder.nzp_dom > 0)
            {
                tXX_cnt += "2";
            }

            if (!TempTableInWebCashe(conn_web, tXX_cnt))
            {
                conn_web.Close();
                ret.tag = -22;
                ret.result = false;
                ret.text = "Данные не были выбраны";

                return null;
            }

            string where = " where 1=1 ";
            if (finder.nzp_counter > 0) where += " and nzp_counter = " + finder.nzp_counter.ToString() + " and pref = '" + finder.pref + "'";

            if (finder.nzp_type > 0) where += " and nzp_type = " + finder.nzp_type.ToString();

            IDbCommand cmd = DBManager.newDbCommand(" Select count(*) From " + tXX_cnt + where, conn_web);
            try
            {
                string s = Convert.ToString(cmd.ExecuteScalar());
                ret.tag = Convert.ToInt32(s);
            }
            catch (Exception ex)
            {
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения списка ПУ " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            string skip = "";
            string pgSkip = "";
#if PG
            if (finder.skip > 0) pgSkip = " offset " + finder.skip.ToString();
#else
            if (finder.skip > 0) skip = " skip " + finder.skip.ToString();
#endif
            string orderby = " Order by adr, service, nzp_counter_old, dat_close, num_cnt, name_type, nzp_cnttype ";
            /*
                        switch (finder.sortby)
                        {
                            case Constants.sortby_adr: orderby = " Order by ulica,idom,ndom,ikvar "; break;
                            case Constants.sortbyCounter: orderby = " Order by numCounter "; break;
                        }
            */

            //выбрать список
            IDataReader reader;

            string sql = 
                " Select " + skip + " nzp_kvar, num_ls, nzp_dom, nzp_type, nzp_serv, nzp_counter, service," +
                " num_cnt, name_type, cnt_stage, mmnog, dat_close, dat_prov, dat_provnext, adr, ikvar, idom," +
                " pref, dat_oblom, dat_poch, nzp_cnttype, name_uchet, is_pl, comments, nzp_counter_old " +
                " From " + tXX_cnt + where + orderby + pgSkip;
            Returns ret2;
            if (!ExecRead(conn_web, out reader, sql, true).result)
            {
                conn_web.Close();
                return null;
            }
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i = i + 1;
                    Counter zap = new Counter();

                    zap.num = (i + finder.skip).ToString();

                    if (reader["num_ls"] == DBNull.Value)
                        zap.num_ls = 0;
                    else
                        zap.num_ls = (int)reader["num_ls"];

                    if (reader["nzp_kvar"] == DBNull.Value)
                        zap.nzp_kvar = 0;
                    else
                        zap.nzp_kvar = (int)reader["nzp_kvar"];

                    if (reader["nzp_dom"] == DBNull.Value)
                        zap.nzp_dom = 0;
                    else
                        zap.nzp_dom = (int)reader["nzp_dom"];

                    if (reader["nzp_counter"] == DBNull.Value)
                        zap.nzp_counter = 0;
                    else
                        zap.nzp_counter = (int)reader["nzp_counter"];

                    if (reader["pref"] == DBNull.Value)
                        zap.pref = "";
                    else
                        zap.pref = (string)reader["pref"];

                    if (reader["adr"] == DBNull.Value)
                        zap.adr = "";
                    else
                        zap.adr = (string)reader["adr"];

                    if (reader["service"] == DBNull.Value)
                        zap.service = "";
                    else
                        zap.service = (string)reader["service"];

                    if (reader["nzp_serv"] == DBNull.Value)
                        zap.nzp_serv = 0;
                    else
                        zap.nzp_serv = (int)reader["nzp_serv"];

                    if (reader["nzp_type"] == DBNull.Value)
                        zap.nzp_type = 0;
                    else
                        zap.nzp_type = (int)reader["nzp_type"];

                    if (zap.nzp_type == CounterKinds.Kvar.GetHashCode())
                    {
                        sql = "select val_prm from "+zap.pref+"_data"+tableDelimiter +
                            "prm_17 where nzp_prm=974 and nzp = " + zap.nzp_counter +
                            " and is_actual = 1 and now() between dat_s and dat_po ";
                        object obj = ExecScalar(conn_web, sql, out ret2, true);
                        if (!ret2.result)
                        {
                            conn_web.Close();
                            return null;
                        }
                        string val;
                        if (obj != null) val = obj.ToString().Trim();
                        else val = "";
                        string prm = "";
                        if (val != "")
                        {
                            sql = "select name_y from " + zap.pref + "_kernel"+tableDelimiter+"res_y where nzp_res = 9990 and nzp_y =" + val;
                            object obj2 = ExecScalar(conn_web, sql, out ret2, true);
                            if (!ret2.result)
                            {
                                conn_web.Close();
                                return null;
                            }
                            
                            if (obj2 != null) prm = obj2.ToString().Trim();
                        }
                        string comment = "";
                        if (reader["comments"] != DBNull.Value) comment = Convert.ToString(reader["comments"]);
                        if (comment != "")
                            if (prm != "") zap.comment = comment + " (Вид помещения для ПУ: " + prm + ")";
                            else zap.comment = comment;
                        else if(prm != "") zap.comment ="Вид помещения для ПУ: " + prm;
                    }

                    zap.cnt_type = CounterKind.GetKindNameById(zap.nzp_type);

                    if (reader["name_type"] == DBNull.Value)
                        zap.name_type = "";
                    else
                        zap.name_type = (string)reader["name_type"];

                    if (reader["is_pl"] == DBNull.Value)
                        zap.is_pl = 0;
                    else
                        zap.is_pl = (int)reader["is_pl"];

                    if (reader["name_uchet"] == DBNull.Value)
                        zap.name_uchet = "";
                    else
                        zap.name_uchet = (string)reader["name_uchet"];

                    if (reader["nzp_cnttype"] == DBNull.Value)
                        zap.nzp_cnttype = 0;
                    else
                        zap.nzp_cnttype = (int)reader["nzp_cnttype"];

                    if (reader["cnt_stage"] == DBNull.Value)
                        zap.cnt_stage = 0;
                    else
                        zap.cnt_stage = (int)reader["cnt_stage"];

                    if (reader["mmnog"] == DBNull.Value)
                        zap.mmnog = 0;
                    else
                        zap.mmnog = Convert.ToDecimal(reader["mmnog"]);

                    if (reader["num_cnt"] == DBNull.Value)
                        zap.num_cnt = "";
                    else
                        zap.num_cnt = (string)reader["num_cnt"];

                    zap.smmnog = zap.cnt_stage + " / " + zap.mmnog;
                    zap.sname_type = zap.name_type.Trim() + " № " + zap.num_cnt.Trim();


                    if (reader["dat_close"] == DBNull.Value)
                        zap.dat_close = "";
                    else
                        zap.dat_close = (string)reader["dat_close"];
                    if (reader["dat_prov"] == DBNull.Value)
                        zap.dat_prov = "";
                    else
                        zap.dat_prov = (string)reader["dat_prov"];
                    if (reader["dat_provnext"] == DBNull.Value)
                        zap.dat_provnext = "";
                    else
                        zap.dat_provnext = (string)reader["dat_provnext"];

                    if (reader["dat_oblom"] == DBNull.Value)
                        zap.dat_oblom = "";
                    else
                        zap.dat_oblom = (string)reader["dat_oblom"];
                    if (reader["dat_poch"] == DBNull.Value)
                        zap.dat_poch = "";
                    else
                        zap.dat_poch = (string)reader["dat_poch"];

                    if (reader["nzp_counter_old"] == DBNull.Value)
                        zap.nzp_counter_old = 0;
                    else
                        zap.nzp_counter_old = (int)reader["nzp_counter_old"];


                    Spis.Add(zap);

                    if (i >= finder.rows) break;


                }

                reader.Close();
                conn_web.Close();
                return Spis;
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";

                MonitorLog.WriteLog("Ошибка заполнения списка ПУ " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }

        }//GetPu

        public string MakeWhereString(Counter finder, out Returns ret, enDopFindType tip)
        {
            //учитвается в поисках других данных (например, адресов)
            ret = Utils.InitReturns();

            string whereString = "";
            if (tip != enDopFindType.dft_CntKvar || finder.has_pu != "0")
            {
                whereString = WhereString(finder);
                if(whereString=="" && finder.has_pu !="1") return "";
            }

            StringBuilder sql = new StringBuilder();

            if (tip == enDopFindType.dft_CntKvar) //ищем л/с
            {
                if (finder.has_pu == "0") //найти ЛС без ИПУ
                {
                    // все построенные условия строятся по следующему шаблону: 0 < (условие)
                    // чтобы найти ЛС без ПУ и подставить его в вышеуказанный шаблон нужно
                    // 1) посчитать количество ПУ ЛС, если кол-во ПУ = 0, то это искомый ЛС
                    // 2) вычесть кол-во ЛС из 1, чтобы сработал шаблон
                    // если кол-во ПУ > 0, то 1 - кол-во ПУ больше нуля даст отрицательное значение

                    sql.Append("Select (1 - count(*)) from PREFX_data" + DBManager.tableDelimiter + "counters_spis c" +
                        " where c.nzp_type = " + (int)CounterKinds.Kvar +
                        (finder.nzp_serv > 0 ? " and c.nzp_serv = " + finder.nzp_serv : "") +
                        " and c.nzp = k.nzp_kvar");
                }
                else
                {
                    string wls = WhereVal(finder, "PREFX");

                    sql.Append(" Select 1 From PREFX_data" + DBManager.tableDelimiter + "counters_spis c, PREFX_kernel" + DBManager.tableDelimiter + "s_counttypes n ");
                    sql.Append(" Where c.nzp_cnttype = n.nzp_cnttype ");

                    if (finder.nzp_type <= 0)
                    {
                        sql.Append(" and ((c.nzp_type = " + (int)CounterKinds.Kvar + " and c.nzp = k.nzp_kvar) or " +
                            " (c.nzp_type = " + (int)CounterKinds.Dom + " and c.nzp = {ALIAS}.nzp_dom) or " +
                            " (c.nzp_type in (" + (int)CounterKinds.Group + ", " + (int)CounterKinds.Communal + ") " + 
                            " and exists (select 1 from PREFX_data" + DBManager.tableDelimiter + "counters_link cl where cl.nzp_counter = c.nzp_counter and cl.nzp_kvar=k.nzp_kvar))) ");
                    }
                    else if (finder.nzp_type == (int)CounterKinds.Dom)
                    {
                        sql.Append(" and c.nzp = {ALIAS}.nzp_dom ");
                    }
                    else if (finder.nzp_type == (int)CounterKinds.Kvar)
                    {
                        sql.Append(" and c.nzp = k.nzp_kvar ");
                    }
                    else if (finder.nzp_type == (int)CounterKinds.Group || finder.nzp_type == (int)CounterKinds.Communal)
                    {

                        sql.Append(" and exists (select 1 from PREFX_data" + DBManager.tableDelimiter + "counters_link cl where cl.nzp_counter=c.nzp_counter and cl.nzp_kvar=k.nzp_kvar) ");
                    }
                    sql.Append(whereString);
                    sql.Append(wls);
                }
            }
            else if (tip == enDopFindType.dft_CntDom) //ищем дома
            {
                string wdm = WhereVal(finder, "PREFX");

                sql.Append(" Select count(*) From PREFX_data" + DBManager.tableDelimiter + "counters_spis c, PREFX_kernel" + DBManager.tableDelimiter + "s_counttypes n ");
                sql.Append(" Where c.nzp_cnttype = n.nzp_cnttype ");

                if (finder.nzp_type <= 0)
                {

                    sql.Append(" and ((c.nzp_type = " + (int)CounterKinds.Kvar + " and exists (select 1 from PREFX_data" + DBManager.tableDelimiter + "kvar k where c.nzp = k.nzp_kvar)) or " +
                        " (c.nzp_type = " + (int)CounterKinds.Dom + " and c.nzp = d.nzp_dom) or " +
                        " (c.nzp_type in (" + (int)CounterKinds.Group + ", " + (int)CounterKinds.Communal + ") and c.nzp = d.nzp_dom)) ");
                }
                else if (finder.nzp_type == (int)CounterKinds.Dom)
                {
                    sql.Append(" and c.nzp = {ALIAS}.nzp_dom ");
                }
                else if (finder.nzp_type == (int)CounterKinds.Kvar)
                {

                    sql.Append(" and exists (select 1 from PREFX_data" + DBManager.tableDelimiter + "kvar k where c.nzp = k.nzp_kvar) ");
                }
                else if (finder.nzp_type == (int)CounterKinds.Group || finder.nzp_type == (int)CounterKinds.Communal)
                {
                    sql.Append(" and c.nzp = {ALIAS}.nzp_dom ");
                }

                sql.Append(whereString);
                sql.Append(wdm);
            }

            return sql.ToString();
        }

        public DataTable GetUploadedCounterFile(Finder finder, out Returns ret)
        {
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Пользователь не задан");
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string tXX_uplcnt = "t" + Convert.ToString(finder.nzp_user) + "_uplcnt";

            if (!TableInWebCashe(conn_web, tXX_uplcnt))
            {
                ret = new Returns(false, "Данные не загружены", -1);
                return null;
            }

            string where = "";
            if (finder.dopFind != null && finder.dopFind.Count > 0 && finder.dopFind[0] == "uncorrelated_only")
                where = " where nzp_counter is null or nzp_counter < 1";

#if PG
            string sql = "select * from " + tXX_uplcnt + " limit " + finder.rows + " offset " + finder.skip + where;
#else
            string sql = "select skip " + finder.skip + " first " + finder.rows + " * from " + tXX_uplcnt + where;
#endif
            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            DataTable dt = new DataTable();
            dt.Load(reader);
            reader.Close();

            sql = "select count(*) num from " + tXX_uplcnt + where;
            var num = ExecScalar(conn_web, sql, out ret, true);

            if (ret.result) ret.tag = Convert.ToInt32(num);

            conn_web.Close();
            return dt;
        }

        public Returns UploadCounterFile(Finder finder, DataTable dt)
        {
            if (dt == null) return new Returns(false, "Таблица не задана");
            if (finder.nzp_user < 1) return new Returns(false, "Пользователь не задан");

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            DBUtils db = new DBUtils();
            ret = db.SaveDataTable(conn_web, finder, "uplcnt", dt);
            db.Close();
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }
            ret = ExecSQL(conn_web, "alter table t" + finder.nzp_user + "_uplcnt add nzp_kvar integer, add pref char(10), add nzp_counter integer, add subpkod char(11) ,add nzp_serv integer", true);

            conn_web.Close();

            return ret;
        }

        public Returns UpdateOneUploadedCounterReading(int recordID, int nzp_counter, int nzp_user)
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

            ret = ExecSQL(conn_web, "update t" + nzp_user + "_uplcnt set nzp_counter = " + nzp_counter + " where id = " + recordID, true);

            conn_web.Close();

            return ret;
        }

        public Returns PrepareDataForGenerationLsPu(Finder finder, List<Counter> listCounters, List<puRooms> selectedRooms)
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            string table_name = DBManager.sDefaultSchema + "t" + finder.nzp_user + "_gengrouplspu" + finder.listNumber;
            if (!ret.result) return ret;
            try
            {
                if (TempTableInWebCashe(conn_web, table_name)) return ret;
                string sql =
                    " CREATE TABLE " + table_name +
                    " (nzp_cnt INTEGER," +
                    " cnt_ls INTEGER," +
                    " nzp_cnttype INTEGER," +
                    " nzp_serv INTEGER," +
                    " nzp_room CHAR(10))"; //вид помещения
                ret = ExecSQL(conn_web, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка постановки задачи в фоновые процессы. Смотрите логи.";
                    return ret;
                }

                foreach (var counter in listCounters)
                {
                    List<PrmTypes> rooms =
                        selectedRooms.Where(x => x.nzp_serv == counter.nzp_serv)
                            .Select(x => x.roomList)
                            .FirstOrDefault();
                    foreach (var room in rooms)
                    {
                        sql = " INSERT INTO " + table_name +
                              " (nzp_cnt, cnt_ls, nzp_cnttype, nzp_serv, nzp_room)" +
                              " VALUES" +
                              " ( " + counter.nzp_cnt + ", 1, " + counter.nzp_cnttype + ", " +
                              counter.nzp_serv + ", " + room.type_name.Trim() + " )";
                        ret = ExecSQL(conn_web, sql, true);
                        if (!ret.result)
                        {
                            ret.text = "Ошибка постановки задачи в фоновые процессы. Смотрите логи.";
                            return ret;
                        }
                    }
                    
                }

            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = " Ошибка постановки задачи в фоновые процессы. Смотрите логи.";
                MonitorLog.WriteLog(" Ошибка метода " + System.Reflection.MethodBase.GetCurrentMethod().Name + " " + 
                    ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (!ret.result)
                {
                    string sql = "DROP TABLE " + table_name;
                    ExecSQL(conn_web, sql);
                    sql = "DROP TABLE " + DBManager.sDefaultSchema + DBManager.tableDelimiter +
                                "t" + finder.nzp_user + "_selectedls" + finder.listNumber;
                    ExecSQL(conn_web, sql);
                }
                conn_web.Close();
            }

            return ret;
        }


        protected string WhereString(Counter finder)
        {
            string whereString = "";

            StringBuilder swhere = new StringBuilder();

            if (finder.nzp_kvar > 0 && finder.nzp_type == (int) CounterKinds.Kvar)
                //вызов списка ПУ из списка лс (не для шаблона поиска)
            {
                swhere.Append(" and c.nzp_type = 3 and c.nzp = " + finder.nzp_kvar.ToString());
            }
            else if (finder.nzp_kvar > 0 && finder.nzp_type == (int) CounterKinds.Communal)
            {
                swhere.Append(" and c.nzp_type = 4 and exists" +
                              " (SELECT 1 FROM PREFX_data" + tableDelimiter + "counters_link l" +
                              " WHERE l.nzp_counter = c.nzp_counter and l.nzp_kvar = " + finder.nzp_kvar + ") ");
            }
            else if (finder.nzp_kvar > 0 && finder.nzp_type == (int) CounterKinds.Group)
            {
                swhere.Append(" and c.nzp_type = 2 and exists" +
                              " (SELECT 1 FROM PREFX_data" + tableDelimiter + "counters_link l" +
                              " WHERE l.nzp_counter = c.nzp_counter and l.nzp_kvar = " + finder.nzp_kvar + ") ");
            }
            else if (finder.nzp_dom > 0) //вызов списка ПУ из списка домов (не для шаблона поиска)
            {
                if (finder.nzp_type == (int) CounterKinds.Group || finder.nzp_type == (int) CounterKinds.Dom ||
                    finder.nzp_type == (int) CounterKinds.Communal)
                    swhere.Append(" and c.nzp_type = " + finder.nzp_type + " and c.nzp = " + finder.nzp_dom);
            }
            if (finder.nzp_cnttype > 0)
            {
                swhere.Append(" and c.nzp_cnttype = " + finder.nzp_cnttype.ToString());
            }

            if (finder.nzp_serv > 0)
            {
                swhere.Append(" and c.nzp_serv = " + finder.nzp_serv.ToString());
            }
            else
            {
                //if (finder.get_koss) swhere.Append(" and c.nzp_serv in (25,210,11,242) ");
                if (finder.RolesVal != null)
                    foreach (_RolesVal role in finder.RolesVal)
                        if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_serv)
                            swhere.Append(" and c.nzp_serv in (" + role.val + ") ");
            }

            if (finder.nzp_type > 0)
            {
                swhere.Append(" and c.nzp_type =" + finder.nzp_type.ToString());
            }

            if (finder.num_cnt != "")
            {
                swhere.Append(" and c.num_cnt = " + Utils.EStrNull(finder.num_cnt));
            }
            if (finder.cnt_stage > 0)
            {
                swhere.Append(" and n.cnt_stage = " + finder.cnt_stage.ToString());
            }
            if (finder.mmnog > 0)
            {
                swhere.Append(" and n.mmnog = " + finder.mmnog.ToString());
            }
            switch (finder.CounterState)
            {
                case -1:
                    swhere.Append(" and c.dat_close is not null ");
                    break;
                case 1:
                    swhere.Append(" and c.dat_close is null ");
                    break;
            }

            string s;
            s = Datas("dat_close", finder.dat_close, finder.dat_close_po);
            swhere.Append(s);
            //s = Datas("dat_uchet", finder.dat_uchet, finder.dat_uchet_po); swhere.Append(s);
            s = Datas("dat_prov", finder.dat_prov, finder.dat_prov_po);
            swhere.Append(s);
            s = Datas("dat_provnext", finder.dat_provnext, finder.dat_provnext_po);
            swhere.Append(s);
            s = Datas("dat_oblom", finder.dat_oblom, finder.dat_oblom_po);
            swhere.Append(s);
            s = Datas("dat_poch", finder.dat_poch, finder.dat_poch_po);
            swhere.Append(s);


            if (swhere.Length > 0)
                whereString = swhere.ToString();

            return whereString;
        }

        protected string WhereVal(Counter finder, string pref)
        {
            if (finder.nzp_kvar > 0 || finder.nzp_dom > 0) return "";

            string res = "";

            if (finder.RolesVal != null)
                foreach (_RolesVal role in finder.RolesVal)
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_serv) res += " and c.nzp_serv in (" + role.val + ") ";

            StringBuilder swhere = new StringBuilder();

            if (finder.dat_uchet_po != "")
            {
                swhere.Append(" and v.dat_uchet <= " + Utils.EStrNull(finder.dat_uchet_po));

                if (finder.dat_uchet != "")
                {
                    swhere.Append(" and v.dat_uchet >= " + Utils.EStrNull(finder.dat_uchet));
                }
            }
            else
            {
                if (finder.dat_uchet != "")
                {
                    swhere.Append(" and v.dat_uchet = " + Utils.EStrNull(finder.dat_uchet));
                }
            }

            if (finder.val_cnt_po > 0)
            {
                swhere.Append(" and v.val_cnt <= " + finder.val_cnt_po.ToString());

                if (finder.val_cnt > 0)
                {
                    swhere.Append(" and v.val_cnt >= " + finder.val_cnt.ToString());
                }
            }
            else
            {
                if (finder.val_cnt > 0)
                {
                    swhere.Append(" and v.val_cnt = " + finder.val_cnt.ToString());
                }
            }

            if (swhere.Length > 0)
            {
                if (finder.nzp_type <= 0)
                {
                    res += " and ((c.nzp_type = " + (int)CounterKinds.Kvar + " and exists (Select 1 From " + pref + "_data" + DBManager.tableDelimiter + "counters v Where v.nzp_counter = c.nzp_counter and is_actual <> 100 " + swhere.ToString() + ")) or " +
                        " (c.nzp_type = " + (int)CounterKinds.Dom + " and exists (Select 1 From " + pref + "_data" + DBManager.tableDelimiter + "counters_dom v Where v.nzp_counter = c.nzp_counter and is_actual <> 100 " + swhere.ToString() + ")) or " +
                        " (c.nzp_type in (" + (int)CounterKinds.Group + ", " + (int)CounterKinds.Communal + ") and exists (Select 1 From " + pref + "_data" + DBManager.tableDelimiter + "counters_group v Where v.nzp_counter = c.nzp_counter and is_actual <> 100 " + swhere.ToString() + "))) ";
                }
                else if (finder.nzp_type == (int)CounterKinds.Dom)
                {
                    res += " and exists (Select 1 From " + pref + "_data" + DBManager.tableDelimiter + "counters_dom v Where v.nzp_counter = c.nzp_counter and is_actual <> 100 " + swhere.ToString() + ") ";
                }
                else if (finder.nzp_type == (int)CounterKinds.Kvar)
                {
                    res += " and exists (Select 1 From " + pref + "_data" + DBManager.tableDelimiter + "counters v Where v.nzp_counter = c.nzp_counter and is_actual <> 100 " + swhere.ToString() + ") ";
                }
                else if (finder.nzp_type == (int)CounterKinds.Group || finder.nzp_type == (int)CounterKinds.Communal)
                {
                    res += " and exists (Select 1 From " + pref + "_data" + DBManager.tableDelimiter + "counters_group v Where v.nzp_counter = c.nzp_counter and is_actual <> 100 " + swhere.ToString() + ") ";
                }
            }
            return res;
        }

        protected string Datas(string dat_nam, string dat_s, string dat_po)
        {
            StringBuilder swhere = new StringBuilder();

            if (dat_po != "")
            {
                swhere.Append(" and c." + dat_nam + " <= " + Utils.EStrNull(dat_po));

                if (dat_s != "")
                {
                    swhere.Append(" and c." + dat_nam + " >= " + Utils.EStrNull(dat_s));
                }
            }
            else
            {
                if (dat_s != "")
                {
                    swhere.Append(" and c." + dat_nam + " = " + Utils.EStrNull(dat_s));
                }
            }

            return swhere.ToString();
        }
    }
}
