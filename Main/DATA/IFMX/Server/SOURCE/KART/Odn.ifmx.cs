using System;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public class DbOdn : DbOdnClient
    //----------------------------------------------------------------------
    {
        static Odn ishZap = null;
        static List<Odn> Spis;
        static int position;            // позиция, в которую надо вставить исходную строку
        static int id;                  // уникальный код записи
        static int parent_id;           // уникальный код родительской записи
        static string tXXOdn = "";            // имя кэш-таблицы для хранения расчетов ОДН
        static string tXXOdn_full = "";       // полное имя кэш-таблицы для хранения расчетов ОДН

        /// <summary> Сформировать условие отбора данных
        /// </summary>
        void WhereStringForGet(OdnFinder finder, ref string whereString)
        {
            WhereStringForFind(finder, "a", ref whereString);

            StringBuilder swhere = new StringBuilder();

            //if (finder.get_koss) swhere.Append(" and a.nzp_serv in (25,210,11,242) ");
            if (finder.RolesVal != null && finder.RolesVal.Count > 0)
            {
                foreach (_RolesVal role in finder.RolesVal)
                {
                    if (role.tip == Constants.role_sql && role.kod == Constants.role_sql_serv)
                        swhere.Append(" and a.nzp_serv in (" + role.val + ") ");
                }
            }

            // определить месяцы начислений
            if (finder.year_ > 0)
            {
                swhere.Append(" and a.dat_month <= '" + String.Format("31.12.{0}", finder.YM.year_.ToString("0000")) + "'");
                swhere.Append(" and a.dat_month >= '" + String.Format("01.01.{0}", finder.YM.year_.ToString("0000")) + "'");
            }
            if (finder.date_begin != "") swhere.Append(" and a.dat_month >= " + Utils.EStrNull(finder.date_begin));
            if (finder.nzp_serv > 0) swhere.Append(" and a.nzp_serv = "+finder.nzp_serv.ToString());
            if (finder.pref != "") swhere.Append(" and a.pref = '" + finder.pref.ToString()+"'");

            whereString += swhere.ToString();
        }

        /// <summary> Добавить запись в список
        /// </summary>
        /// <param name="zap">Добавляемая запись</param>
        /// <param name="list">Список, в который добавляется запись</param>
        /// <param name="position">Позиция, в которую добавляется исходная запись</param>
        private void AddZapToList(ref Odn zap, ref List<Odn> list, ref int position)
        {
            if (zap == null) return;
            
            if (zap.dat_charge == "")
            {
                list.Insert(position, zap);
                position = list.Count;
            }
            else
            {
                if (Convert.ToDateTime(zap.dat_charge) > Convert.ToDateTime(zap.dat_month))
                {
                    if (ishZap != null) ishZap.has_future_reval = 1;
                }
                if (Convert.ToDateTime(zap.dat_charge) < Convert.ToDateTime(zap.dat_month))
                {
                    if (ishZap != null) ishZap.has_past_reval = 1;
                }
                list.Add(zap);
            }
        }

        /// <summary> Заполнить часть записи из Reader'а, специфичной для родительской записи
        /// </summary>
        private void FillPartOfIshZap(ref Odn zap, IDataReader reader)
        {
            AddZapToList(ref ishZap, ref Spis, ref position);

            zap.dat_charge = "";
            zap.parent_id = 0;
            parent_id = id;     // Запись с исходным расчетом идет первой, все связанные перерасчеты потом по убыванию даты перерасчета
            // поэтому как только встречается запись с пустым dat_charge, используем id этой записи как parent_id последующих записей 
            ishZap = zap;       // Запомним запись с исходным расчетом, чтобы затем задать ему признаки наличия прошлых и будущих перерасчетов
            if (reader["dat_month"] != DBNull.Value)
            {
                zap.dat_month = Convert.ToDateTime(reader["dat_month"]).ToShortDateString();
                zap.YM.month_ = Convert.ToDateTime(reader["dat_month"]).Month;
                zap.YM.year_ = Convert.ToDateTime(reader["dat_month"]).Year;
            }
        }

        /// <summary> Заполнить часть записи из Reader'а, специфичной для дочерней записи
        /// </summary>
        private void FillPartOfChildZap(ref Odn zap, IDataReader reader)
        {
            zap.dat_charge = Convert.ToDateTime(reader["dat_charge"]).ToShortDateString();
            zap.parent_id = parent_id;
            zap.dat_month = Convert.ToDateTime(reader["dat_month"]).ToShortDateString();
            zap.YM.month_ = Convert.ToDateTime(reader["dat_charge"]).Month;
            zap.YM.year_ = Convert.ToDateTime(reader["dat_charge"]).Year;
        }

        /// <summary> Заполнить запись из Reader'а
        /// </summary>
        private void FillZap(ref Odn zap, IDataReader reader)
        {
            if (reader["nzp_dom"] != DBNull.Value) zap.nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
            if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
            if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]);

            if (reader["nzp_correct"] != DBNull.Value) zap.nzp_correct = Convert.ToInt32(reader["nzp_correct"]);
            if (reader["is_gkal"] != DBNull.Value) zap.is_gkal = Convert.ToInt32(reader["is_gkal"]);
            if (reader["rval_real"] != DBNull.Value && Convert.ToDecimal(reader["rval_real"]) != 0) zap.rval_real = Convert.ToDecimal(reader["rval_real"]);
            if (reader["rval_real_p"] != DBNull.Value && Convert.ToDecimal(reader["rval_real_p"]) != 0) zap.rval_real_p = Convert.ToDecimal(reader["rval_real_p"]);
            if (reader["rval"] != DBNull.Value && Convert.ToDecimal(reader["rval"]) != 0) zap.rval = Convert.ToDecimal(reader["rval"]);
            if (reader["rval_p"] != DBNull.Value && Convert.ToDecimal(reader["rval_p"]) != 0) zap.rval_p = Convert.ToDecimal(reader["rval_p"]);
            if (reader["rvaldlt"] != DBNull.Value && Convert.ToDecimal(reader["rvaldlt"]) != 0) zap.rvaldlt = Convert.ToDecimal(reader["rvaldlt"]);
            if (reader["rvaldlt_p"] != DBNull.Value && Convert.ToDecimal(reader["rvaldlt_p"]) != 0) zap.rvaldlt_p = Convert.ToDecimal(reader["rvaldlt_p"]);
            if (reader["dnow"] != DBNull.Value)
                if (Convert.ToDateTime(reader["dnow"]) > DateTime.Parse("01.01.1900"))
                    zap.dnow = Convert.ToDateTime(reader["dnow"]).ToShortDateString();
            if (reader["dnow_p"] != DBNull.Value)
                if (Convert.ToDateTime(reader["dnow_p"]) > DateTime.Parse("01.01.1900")) 
                    zap.dnow_p = Convert.ToDateTime(reader["dnow_p"]).ToShortDateString();
            if (reader["dpred"] != DBNull.Value)
                if (Convert.ToDateTime(reader["dpred"]) > DateTime.Parse("01.01.1900")) 
                    zap.dpred = Convert.ToDateTime(reader["dpred"]).ToShortDateString();
            if (reader["dpred_p"] != DBNull.Value)
                if (Convert.ToDateTime(reader["dpred_p"]) > DateTime.Parse("01.01.1900")) 
                    zap.dpred_p = Convert.ToDateTime(reader["dpred_p"]).ToShortDateString();
            if (reader["rval_now"] != DBNull.Value && Convert.ToDecimal(reader["rval_now"]) != 0) zap.rval_now = Convert.ToDecimal(reader["rval_now"]);
            if (reader["rval_now_p"] != DBNull.Value && Convert.ToDecimal(reader["rval_now_p"]) != 0) zap.rval_now_p = Convert.ToDecimal(reader["rval_now_p"]);
            if (reader["rval_pred"] != DBNull.Value && Convert.ToDecimal(reader["rval_pred"]) != 0) zap.rval_pred = Convert.ToDecimal(reader["rval_pred"]);
            if (reader["rval_pred_p"] != DBNull.Value && Convert.ToDecimal(reader["rval_pred_p"]) != 0) zap.rval_pred_p = Convert.ToDecimal(reader["rval_pred_p"]);
            if (reader["cnt_ls_val"] != DBNull.Value) zap.cnt_ls_val = Convert.ToInt32(reader["cnt_ls_val"]);
            if (reader["cnt_ls_val_p"] != DBNull.Value) zap.cnt_ls_val_p = Convert.ToInt32(reader["cnt_ls_val_p"]);
            if (reader["sum_ls_val"] != DBNull.Value && Convert.ToDecimal(reader["sum_ls_val"]) != 0) zap.sum_ls_val = Convert.ToDecimal(reader["sum_ls_val"]);
            if (reader["sum_ls_val_p"] != DBNull.Value && Convert.ToDecimal(reader["sum_ls_val_p"]) != 0) zap.sum_ls_val_p = Convert.ToDecimal(reader["sum_ls_val_p"]);
            if (reader["cnt_ls_norm"] != DBNull.Value) zap.cnt_ls_norm = Convert.ToInt32(reader["cnt_ls_norm"]);
            if (reader["cnt_ls_norm_p"] != DBNull.Value) zap.cnt_ls_norm_p = Convert.ToInt32(reader["cnt_ls_norm_p"]);
            if (reader["sum_ls_norm"] != DBNull.Value && Convert.ToDecimal(reader["sum_ls_norm"]) != 0) zap.sum_ls_norm = Convert.ToDecimal(reader["sum_ls_norm"]);
            if (reader["sum_ls_norm_p"] != DBNull.Value && Convert.ToDecimal(reader["sum_ls_norm_p"]) != 0) zap.sum_ls_norm_p = Convert.ToDecimal(reader["sum_ls_norm_p"]);
            if (reader["cnt_ls_25val"] != DBNull.Value) zap.cnt_ls_25val = Convert.ToInt32(reader["cnt_ls_25val"]);
            if (reader["cnt_ls_25val_p"] != DBNull.Value) zap.cnt_ls_25val_p = Convert.ToInt32(reader["cnt_ls_25val_p"]);
            if (reader["sum_ls_25val"] != DBNull.Value && Convert.ToDecimal(reader["sum_ls_25val"]) != 0) zap.sum_ls_25val = Convert.ToDecimal(reader["sum_ls_25val"]);
            if (reader["sum_ls_25val_p"] != DBNull.Value && Convert.ToDecimal(reader["sum_ls_25val_p"]) != 0) zap.sum_ls_25val_p = Convert.ToDecimal(reader["sum_ls_25val_p"]);
            if (reader["cnt_ls_25norm"] != DBNull.Value) zap.cnt_ls_25norm = Convert.ToInt32(reader["cnt_ls_25norm"]);
            if (reader["cnt_ls_25norm_p"] != DBNull.Value) zap.cnt_ls_25norm_p = Convert.ToInt32(reader["cnt_ls_25norm_p"]);
            if (reader["sum_ls_25norm"] != DBNull.Value && Convert.ToDecimal(reader["sum_ls_25norm"]) != 0) zap.sum_ls_25norm = Convert.ToDecimal(reader["sum_ls_25norm"]);
            if (reader["sum_ls_25norm_p"] != DBNull.Value && Convert.ToDecimal(reader["sum_ls_25norm_p"]) != 0) zap.sum_ls_25norm_p = Convert.ToDecimal(reader["sum_ls_25norm_p"]);
            if (reader["cnt_ls_221val"] != DBNull.Value) zap.cnt_ls_221val = Convert.ToInt32(reader["cnt_ls_221val"]);
            if (reader["cnt_ls_221val_p"] != DBNull.Value) zap.cnt_ls_221val_p = Convert.ToInt32(reader["cnt_ls_221val_p"]);
            if (reader["sum_ls_221val"] != DBNull.Value && Convert.ToDecimal(reader["sum_ls_221val"]) != 0) zap.sum_ls_221val = Convert.ToDecimal(reader["sum_ls_221val"]);
            if (reader["sum_ls_221val_p"] != DBNull.Value && Convert.ToDecimal(reader["sum_ls_221val_p"]) != 0) zap.sum_ls_221val_p = Convert.ToDecimal(reader["sum_ls_221val_p"]);
            if (reader["cnt_ls_221norm"] != DBNull.Value) zap.cnt_ls_221norm = Convert.ToInt32(reader["cnt_ls_221norm"]);
            if (reader["cnt_ls_221norm_p"] != DBNull.Value) zap.cnt_ls_221norm_p = Convert.ToInt32(reader["cnt_ls_221norm_p"]);
            if (reader["sum_ls_221norm"] != DBNull.Value && Convert.ToDecimal(reader["sum_ls_221norm"]) != 0) zap.sum_ls_221norm = Convert.ToDecimal(reader["sum_ls_221norm"]);
            if (reader["sum_ls_221norm_p"] != DBNull.Value && Convert.ToDecimal(reader["sum_ls_221norm_p"]) != 0) zap.sum_ls_221norm_p = Convert.ToDecimal(reader["sum_ls_221norm_p"]);
            if (reader["cnt_ls_210val"] != DBNull.Value) zap.cnt_ls_210val = Convert.ToInt32(reader["cnt_ls_210val"]);
            if (reader["cnt_ls_210val_p"] != DBNull.Value) zap.cnt_ls_210val_p = Convert.ToInt32(reader["cnt_ls_210val_p"]);
            if (reader["sum_ls_210val"] != DBNull.Value && Convert.ToDecimal(reader["sum_ls_210val"]) != 0) zap.sum_ls_210val = Convert.ToDecimal(reader["sum_ls_210val"]);
            if (reader["sum_ls_210val_p"] != DBNull.Value && Convert.ToDecimal(reader["sum_ls_210val_p"]) != 0) zap.sum_ls_210val_p = Convert.ToDecimal(reader["sum_ls_210val_p"]);
            if (reader["cnt_ls_210norm"] != DBNull.Value) zap.cnt_ls_210norm = Convert.ToInt32(reader["cnt_ls_210norm"]);
            if (reader["cnt_ls_210norm_p"] != DBNull.Value) zap.cnt_ls_210norm_p = Convert.ToInt32(reader["cnt_ls_210norm_p"]);
            if (reader["sum_ls_210norm"] != DBNull.Value && Convert.ToDecimal(reader["sum_ls_210norm"]) != 0) zap.sum_ls_210norm = Convert.ToDecimal(reader["sum_ls_210norm"]);
            if (reader["sum_ls_210norm_p"] != DBNull.Value && Convert.ToDecimal(reader["sum_ls_210norm_p"]) != 0) zap.sum_ls_210norm_p = Convert.ToDecimal(reader["sum_ls_210norm_p"]);
            if (reader["cnt_gils"] != DBNull.Value && Convert.ToDecimal(reader["cnt_gils"]) != 0) zap.cnt_gils = Convert.ToDecimal(reader["cnt_gils"]);
            if (reader["cnt_gils_p"] != DBNull.Value && Convert.ToDecimal(reader["cnt_gils_p"]) != 0) zap.cnt_gils_p = Convert.ToDecimal(reader["cnt_gils_p"]);
            if (reader["cnt_pl_pu"] != DBNull.Value && Convert.ToDecimal(reader["cnt_pl_pu"]) != 0) zap.cnt_pl_pu = Convert.ToDecimal(reader["cnt_pl_pu"]);
            if (reader["cnt_pl_pu_p"] != DBNull.Value && Convert.ToDecimal(reader["cnt_pl_pu_p"]) != 0) zap.cnt_pl_pu_p = Convert.ToDecimal(reader["cnt_pl_pu_p"]);
            if (reader["cnt_gils_pu"] != DBNull.Value && Convert.ToDecimal(reader["cnt_gils_pu"]) != 0) zap.cnt_gils_pu = Convert.ToDecimal(reader["cnt_gils_pu"]);
            if (reader["cnt_gils_pu_p"] != DBNull.Value && Convert.ToDecimal(reader["cnt_gils_pu_p"]) != 0) zap.cnt_gils_pu_p = Convert.ToDecimal(reader["cnt_gils_pu_p"]);
            if (reader["cnt_pl_norm"] != DBNull.Value && Convert.ToDecimal(reader["cnt_pl_norm"]) != 0) zap.cnt_pl_norm = Convert.ToDecimal(reader["cnt_pl_norm"]);
            if (reader["cnt_pl_norm_p"] != DBNull.Value && Convert.ToDecimal(reader["cnt_pl_norm_p"]) != 0) zap.cnt_pl_norm_p = Convert.ToDecimal(reader["cnt_pl_norm_p"]);
            if (reader["cnt_gils_norm"] != DBNull.Value && Convert.ToDecimal(reader["cnt_gils_norm"]) != 0) zap.cnt_gils_norm = Convert.ToDecimal(reader["cnt_gils_norm"]);
            if (reader["cnt_gils_norm_p"] != DBNull.Value && Convert.ToDecimal(reader["cnt_gils_norm_p"]) != 0) zap.cnt_gils_norm_p = Convert.ToDecimal(reader["cnt_gils_norm_p"]);
            if (reader["nzp_type_alg"] != DBNull.Value) zap.nzp_type_alg = Convert.ToInt32(reader["nzp_type_alg"]);
            if (reader["ntalg_short"] != DBNull.Value) zap.ntalg_short = Convert.ToString(reader["ntalg_short"]);
            if (reader["nzp_type_alg_p"] != DBNull.Value) zap.nzp_type_alg_p = Convert.ToInt32(reader["nzp_type_alg_p"]);
            if (reader["ntalg_p_short"] != DBNull.Value) zap.ntalg_p_short = Convert.ToString(reader["ntalg_p_short"]);
            if (reader["is_uchet"] != DBNull.Value) zap.is_uchet = Convert.ToInt32(reader["is_uchet"]);
            if (reader["dat_when"] != DBNull.Value) zap.dat_when = Convert.ToDateTime(reader["dat_when"]).ToShortDateString();
        }

        /// <summary> Найти и добавить прошлые перерасчеты
        /// </summary>
        private bool AddPastRevals(IDbConnection conn_web, string select)
        {
            if (ishZap == null) return true;

            // Извлечь прошлые перерасчеты
            StringBuilder sql = new StringBuilder();
            sql.Append(" Select a.dat_month as dat_charge, a.dat_charge as dat_month,  " + select);
            sql.Append(" From " + tXXOdn + " a ");
            sql.Append(" Where a.nzp_dom = " + ishZap.nzp_dom.ToString());
            sql.Append("   and a.nzp_serv = " + ishZap.nzp_serv.ToString());
            sql.Append("   and a.dat_charge = '" + Convert.ToDateTime(ishZap.dat_month).ToShortDateString() + "'");
            sql.Append("   and a.dat_charge <> a.dat_month ");
            sql.Append(" Order by 1 desc ");

            IDataReader reader;
            if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
            {
                conn_web.Close();
                return false;
            }

            Odn zap;
            while (reader.Read())
            {
                zap = new Odn();
                zap.id = ++id;
                zap.num = zap.id.ToString();
                FillPartOfChildZap(ref zap, reader);
                FillZap(ref zap, reader);
                AddZapToList(ref zap, ref Spis, ref position);
            }
            reader.Close();

            return true;
        }

        /// <summary> Найти расчет ОДН в КЭШ
        /// </summary>
        public List<Odn> GetOdn(OdnFinder finder, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return null;
            }
            if (finder.date_begin != "")
            {
                DateTime dateBegin;
                if (!DateTime.TryParse(finder.date_begin, out dateBegin))
                {
                    ret.result = false;
                    ret.text = "Ошибка во входных параметрах";
                    return null;
                }
            }

            Spis = new List<Odn>();
            position = 0;           
            parent_id = 0;          
                
            //выбрать общее кол-во
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            tXXOdn = "t" + finder.nzp_user.ToString() + "_cntcor";

            IDataReader reader;

            // проверить наличие данных по заданному дому
            // если их нет, то подггрузить из основной БД
            StringBuilder sql = new StringBuilder();

            sql.Append("select * from systables where tabname = '" + tXXOdn + "'");
            if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
            {
                conn_web.Close();
                return null;
            }
            if (!reader.Read()) // таблица еще не создана или мы хотим получить данные по всем домам
            {
                FindOdn(finder, out ret);
                if (!ret.result)
                {
                    conn_web.Close();
                    return null;
                }
            }
            else
            {
                sql = new StringBuilder();
#if PG
                sql.Append(" select  * from " + tXXOdn + " limit 1 ");
#else
                sql.Append(" select first 1 * from " + tXXOdn);
#endif
                if (finder.nzp_dom > 0) sql.Append(" Where nzp_dom = " + finder.nzp_dom.ToString());
                if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
                {
                    conn_web.Close();
                    return null;
                }
                if (!reader.Read())
                {
                    FindOdn(finder, out ret);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return null;
                    }
                }
            }
            reader.Close();


            #region выбрать данные из КЭШ таблицы
            // Поля для выборки
            string Select = "dat_month, pref, nzp_correct, nzp_dom," +
                " nzp_counter, nzp_serv, service, is_gkal,rval_real,rval_real_p,rval,rval_p,rvaldlt,rvaldlt_p," +
                " dnow,dnow_p,dpred,dpred_p,rval_now,rval_now_p,rval_pred,rval_pred_p,cnt_ls_val,cnt_ls_val_p,sum_ls_val,sum_ls_val_p,cnt_ls_norm," +
                " cnt_ls_norm_p,sum_ls_norm,sum_ls_norm_p,cnt_ls_25val,cnt_ls_25val_p,sum_ls_25val,sum_ls_25val_p,cnt_ls_25norm,cnt_ls_25norm_p," +
                " sum_ls_25norm,sum_ls_25norm_p,cnt_ls_221val,cnt_ls_221val_p,sum_ls_221val,sum_ls_221val_p,cnt_ls_221norm,cnt_ls_221norm_p,sum_ls_221norm," +
                " sum_ls_221norm_p,cnt_ls_210val,cnt_ls_210val_p,sum_ls_210val,sum_ls_210val_p,cnt_ls_210norm,cnt_ls_210norm_p,sum_ls_210norm,sum_ls_210norm_p," +
                " cnt_gils,cnt_gils_p,cnt_gils_pu,cnt_gils_pu_p,cnt_pl_norm,cnt_pl_norm_p, nzp_type_alg, ntalg_short, nzp_type_alg_p, ntalg_p_short, is_uchet,nzp_user,dat_when," +
                " cnt_gils_norm,cnt_gils_norm_p,cnt_pl_pu,cnt_pl_pu_p";

            // Извлечь расчеты вместе с будущими перерасчетами
            sql = new StringBuilder();
            sql.Append(" Select (case when a.dat_month = a.dat_charge then mdy(1,1,3000) else a.dat_charge end) as dat_charge, "+Select);
            sql.Append(" From " + tXXOdn + " a ");
            string where = " Where 1=1 ";
            WhereStringForGet(finder, ref where);
            sql.Append(where);
            sql.Append(" Order by a.service asc, a.nzp_serv asc, a.dat_month asc, 1 desc ");

            if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
            {
                conn_web.Close();
                return null;
            }
            try
            {
                id = 0;
                ishZap = null;
                Odn zap;
                while (reader.Read())
                {
                    if (Convert.ToDateTime(reader["dat_charge"]) == DateTime.Parse("01.01.3000"))
                    {
                        AddPastRevals(conn_web, Select);
                    }

                    zap = new Odn();
                    
                    zap.id = ++id;
                    zap.num = zap.id.ToString();

                    if (Convert.ToDateTime(reader["dat_charge"]) == DateTime.Parse("01.01.3000"))
                    {
                        FillPartOfIshZap(ref zap, reader);
                    }
                    else
                    {
                        FillPartOfChildZap(ref zap, reader);                        
                    }

                    FillZap(ref zap, reader);

                    if (zap.dat_charge != "") AddZapToList(ref zap, ref Spis, ref position);
                }
                
                AddPastRevals(conn_web, Select);
                AddZapToList(ref ishZap, ref Spis, ref position);

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

                MonitorLog.WriteLog("Ошибка заполнения списка расчетов ОДН " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }
            #endregion

        }//GetOdn

        

        

        

        /// <summary> Выполнить поиск и заполнить кэш-таблицу
        /// </summary>
        public void FindOdn(OdnFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return;
            }

            if (finder.pref == "")
            {
                ret.text = "Префикс базы данных не задан";
                return;
            }

            // соединение с кэш базой
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return;

            tXXOdn = "t" + Convert.ToString(finder.nzp_user) + "_cntcor";
#if PG
            tXXOdn_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + "." + tXXOdn;
#else
            tXXOdn_full = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":" + tXXOdn;
#endif
            bool isTableCreated = false;

            IDataReader reader;
            StringBuilder sql = new StringBuilder();
            sql.Append("select * from systables where tabname = '" + tXXOdn + "'");
            if (!ExecRead(conn_web, out reader, sql.ToString(), true).result)
            {
                conn_web.Close();
                return;
            }
            if (!reader.Read())
            {
                #region создать таблицу webdata:tXX_odn
                sql = new StringBuilder();
#if PG
                sql.Append(" CREATE TABLE " + tXXOdn + "(" +
                                    "pref CHAR(20), " +
                                    "nzp_correct int , " +
                                    "dat_month DATE, " +
                                    "dat_charge DATE, " +
                                    "nzp_dom INTEGER , " +
                                    "nzp_counter INTEGER , " +
                                    "nzp_serv INTEGER , " +
                                    "service CHAR(100), " +
                                    "is_gkal INTEGER , " +
                                    "rval_real NUMERIC(14,7) , " +
                                    "rval_real_p NUMERIC(14,7) , " +
                                    "rval NUMERIC(14,7) , " +
                                    "rval_p NUMERIC(14,7) , " +
                                    "rvaldlt NUMERIC(14,7) , " +
                                    "rvaldlt_p NUMERIC(14,7) , " +
                                    "dnow DATE, " +
                                    "dnow_p DATE, " +
                                    "dpred DATE, " +
                                    "dpred_p DATE, " +
                                    "rval_now NUMERIC(19,7) , " +
                                    "rval_now_p NUMERIC(19,7) , " +
                                    "rval_pred NUMERIC(19,7) , " +
                                    "rval_pred_p NUMERIC(19,7) , " +
                                    "cnt_ls_val INTEGER , " +
                                    "cnt_ls_val_p INTEGER , " +
                                    "sum_ls_val NUMERIC(14,7) , " +
                                    "sum_ls_val_p NUMERIC(14,7) , " +
                                    "cnt_ls_norm INTEGER , " +
                                    "cnt_ls_norm_p INTEGER , " +
                                    "sum_ls_norm NUMERIC(14,7) , " +
                                    "sum_ls_norm_p NUMERIC(14,7) , " +
                                    "cnt_ls_25val INTEGER , " +
                                    "cnt_ls_25val_p INTEGER , " +
                                    "sum_ls_25val NUMERIC(14,7) , " +
                                    "sum_ls_25val_p NUMERIC(14,7) , " +
                                    "cnt_ls_25norm INTEGER , " +
                                    "cnt_ls_25norm_p INTEGER , " +
                                    "sum_ls_25norm NUMERIC(14,7) , " +
                                    "sum_ls_25norm_p NUMERIC(14,7) , " +
                                    "cnt_ls_221val INTEGER , " +
                                    "cnt_ls_221val_p INTEGER , " +
                                    "sum_ls_221val NUMERIC(14,7) , " +
                                    "sum_ls_221val_p NUMERIC(14,7) , " +
                                    "cnt_ls_221norm INTEGER , " +
                                    "cnt_ls_221norm_p INTEGER , " +
                                    "sum_ls_221norm NUMERIC(14,7) , " +
                                    "sum_ls_221norm_p NUMERIC(14,7) , " +
                                    "cnt_ls_210val INTEGER , " +
                                    "cnt_ls_210val_p INTEGER , " +
                                    "sum_ls_210val NUMERIC(14,7) , " +
                                    "sum_ls_210val_p NUMERIC(14,7) , " +
                                    "cnt_ls_210norm INTEGER , " +
                                    "cnt_ls_210norm_p INTEGER , " +
                                    "sum_ls_210norm NUMERIC(14,7) , " +
                                    "sum_ls_210norm_p NUMERIC(14,7) , " +
                                    "cnt_gils NUMERIC(14,7) default 0 , " +
                                    "cnt_gils_p NUMERIC(14,7) default 0 , " +
                                    "cnt_gils_pu NUMERIC(14,7) default 0 , " +
                                    "cnt_gils_pu_p NUMERIC(14,7) default 0 , " +
                                    "cnt_pl_norm NUMERIC(14,7) default 0 , " +
                                    "cnt_pl_norm_p NUMERIC(14,7) default 0 , " +
                                    "nzp_type_alg INTEGER , " +
                                    "ntalg_short CHAR(15) , " +
                                    "nzp_type_alg_p INTEGER , " +
                                    "ntalg_p_short CHAR(15) , " +
                                    "is_uchet INTEGER , " +
                                    "nzp_user INTEGER, " +
                                    "dat_when DATE, " +
                                    "cnt_gils_norm NUMERIC(14,7) default 0 , " +
                                    "cnt_gils_norm_p NUMERIC(14,7) default 0 , " +
                                    "cnt_pl_pu NUMERIC(14,7) default 0 , " +
                                    "cnt_pl_pu_p NUMERIC(14,7) default 0 ) "
                                    );
#else
sql.Append(" CREATE TABLE " + tXXOdn + "(" +
                    "pref CHAR(20), " +
                    "nzp_correct int , " +
                    "dat_month DATE, " +
                    "dat_charge DATE, "+
                    "nzp_dom INTEGER , "+
                    "nzp_counter INTEGER , "+
                    "nzp_serv INTEGER , "+
                    "service CHAR(100), " +
                    "is_gkal INTEGER , " +
                    "rval_real DECIMAL(14,7) , "+
                    "rval_real_p DECIMAL(14,7) , "+
                    "rval DECIMAL(14,7) , "+
                    "rval_p DECIMAL(14,7) , "+
                    "rvaldlt DECIMAL(14,7) , "+
                    "rvaldlt_p DECIMAL(14,7) , "+
                    "dnow DATE, "+
                    "dnow_p DATE, "+
                    "dpred DATE, "+
                    "dpred_p DATE, "+
                    "rval_now DECIMAL(19,7) , "+
                    "rval_now_p DECIMAL(19,7) , "+
                    "rval_pred DECIMAL(19,7) , "+
                    "rval_pred_p DECIMAL(19,7) , "+
                    "cnt_ls_val INTEGER , "+
                    "cnt_ls_val_p INTEGER , "+
                    "sum_ls_val DECIMAL(14,7) , "+
                    "sum_ls_val_p DECIMAL(14,7) , "+
                    "cnt_ls_norm INTEGER , "+
                    "cnt_ls_norm_p INTEGER , "+
                    "sum_ls_norm DECIMAL(14,7) , "+
                    "sum_ls_norm_p DECIMAL(14,7) , "+
                    "cnt_ls_25val INTEGER , "+
                    "cnt_ls_25val_p INTEGER , "+
                    "sum_ls_25val DECIMAL(14,7) , "+
                    "sum_ls_25val_p DECIMAL(14,7) , "+
                    "cnt_ls_25norm INTEGER , "+
                    "cnt_ls_25norm_p INTEGER , "+
                    "sum_ls_25norm DECIMAL(14,7) , "+
                    "sum_ls_25norm_p DECIMAL(14,7) , "+
                    "cnt_ls_221val INTEGER , "+
                    "cnt_ls_221val_p INTEGER , "+
                    "sum_ls_221val DECIMAL(14,7) , "+
                    "sum_ls_221val_p DECIMAL(14,7) , "+
                    "cnt_ls_221norm INTEGER , "+
                    "cnt_ls_221norm_p INTEGER , "+
                    "sum_ls_221norm DECIMAL(14,7) , "+
                    "sum_ls_221norm_p DECIMAL(14,7) , "+
                    "cnt_ls_210val INTEGER , "+
                    "cnt_ls_210val_p INTEGER , "+
                    "sum_ls_210val DECIMAL(14,7) , "+
                    "sum_ls_210val_p DECIMAL(14,7) , "+
                    "cnt_ls_210norm INTEGER , "+
                    "cnt_ls_210norm_p INTEGER , "+
                    "sum_ls_210norm DECIMAL(14,7) , "+
                    "sum_ls_210norm_p DECIMAL(14,7) , "+
                    "cnt_gils DECIMAL(14,7) default 0 , "+
                    "cnt_gils_p DECIMAL(14,7) default 0 , "+
                    "cnt_gils_pu DECIMAL(14,7) default 0 , "+
                    "cnt_gils_pu_p DECIMAL(14,7) default 0 , "+
                    "cnt_pl_norm DECIMAL(14,7) default 0 , "+
                    "cnt_pl_norm_p DECIMAL(14,7) default 0 , "+
                    "nzp_type_alg INTEGER , "+
                    "ntalg_short CHAR(15) , " +
                    "nzp_type_alg_p INTEGER , "+
                    "ntalg_p_short CHAR(15) , " +
                    "is_uchet INTEGER , "+
                    "nzp_user INTEGER, "+
                    "dat_when DATE, "+
                    "cnt_gils_norm DECIMAL(14,7) default 0 , "+
                    "cnt_gils_norm_p DECIMAL(14,7) default 0 , "+
                    "cnt_pl_pu DECIMAL(14,7) default 0 , "+
                    "cnt_pl_pu_p DECIMAL(14,7) default 0 ) "
                    );
#endif
                ret = ExecSQL(conn_web, sql.ToString(), false);
                if (!ret.result)
                {
                    conn_web.Close();
                    return;
                }
                else isTableCreated = true;
                #endregion
            }

            // удалить имеющиеся данные из кэш
            sql = new StringBuilder();
            sql.Append("delete from " + tXXOdn);
            if (finder.nzp_dom > 0) sql.Append(" where nzp_dom = "+finder.nzp_dom.ToString());
            ret = ExecSQL(conn_web, sql.ToString(), true);

            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            //заполнить webdata:tXXOdn
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return;
            }

            string cur_pref = finder.pref;

            string whereString = "";
            WhereStringForFind(finder, "a", ref whereString);

#if PG
            string Insert = " Insert into " + tXXOdn_full +
                    " ( pref, nzp_correct, dat_month,dat_charge,nzp_dom,nzp_counter, nzp_serv, service, is_gkal,rval_real,rval_real_p,rval,rval_p,rvaldlt,rvaldlt_p," +
                    " dnow,dnow_p,dpred,dpred_p,rval_now,rval_now_p,rval_pred,rval_pred_p,cnt_ls_val,cnt_ls_val_p,sum_ls_val,sum_ls_val_p,cnt_ls_norm," +
                    " cnt_ls_norm_p,sum_ls_norm,sum_ls_norm_p,cnt_ls_25val,cnt_ls_25val_p,sum_ls_25val,sum_ls_25val_p,cnt_ls_25norm,cnt_ls_25norm_p," +
                    " sum_ls_25norm,sum_ls_25norm_p,cnt_ls_221val,cnt_ls_221val_p,sum_ls_221val,sum_ls_221val_p,cnt_ls_221norm,cnt_ls_221norm_p,sum_ls_221norm," +
                    " sum_ls_221norm_p,cnt_ls_210val,cnt_ls_210val_p,sum_ls_210val,sum_ls_210val_p,cnt_ls_210norm,cnt_ls_210norm_p,sum_ls_210norm,sum_ls_210norm_p," +
                    " cnt_gils,cnt_gils_p,cnt_gils_pu,cnt_gils_pu_p,cnt_pl_norm,cnt_pl_norm_p,nzp_type_alg, ntalg_short, nzp_type_alg_p, ntalg_p_short, is_uchet,nzp_user,dat_when," +
                    " cnt_gils_norm,cnt_gils_norm_p,cnt_pl_pu,cnt_pl_pu_p ) ";
            // загрузка исходных начислений и будущих перерасчетов
            sql = new StringBuilder();
            sql.Append(Insert);
            sql.Append(" Select '" + finder.pref + "', nzp_correct,dat_month,dat_charge,nzp_dom,nzp_counter, a.nzp_serv, s.service, is_gkal,rval_real,rval_real_p,rval,rval_p,rvaldlt,rvaldlt_p," +
                " dnow,dnow_p,dpred,dpred_p,rval_now,rval_now_p,rval_pred,rval_pred_p,cnt_ls_val,cnt_ls_val_p,sum_ls_val,sum_ls_val_p,cnt_ls_norm," +
                " cnt_ls_norm_p,sum_ls_norm,sum_ls_norm_p,cnt_ls_25val,cnt_ls_25val_p,sum_ls_25val,sum_ls_25val_p,cnt_ls_25norm,cnt_ls_25norm_p," +
                " sum_ls_25norm,sum_ls_25norm_p,cnt_ls_221val,cnt_ls_221val_p,sum_ls_221val,sum_ls_221val_p,cnt_ls_221norm,cnt_ls_221norm_p,sum_ls_221norm," +
                " sum_ls_221norm_p,cnt_ls_210val,cnt_ls_210val_p,sum_ls_210val,sum_ls_210val_p,cnt_ls_210norm,cnt_ls_210norm_p,sum_ls_210norm,sum_ls_210norm_p," +
                " cnt_gils,cnt_gils_p,cnt_gils_pu,cnt_gils_pu_p,cnt_pl_norm,cnt_pl_norm_p, a.nzp_type_alg, ta.name_short, a.nzp_type_alg_p, ta_p.name_short, is_uchet,nzp_user,dat_when," +
                " cnt_gils_norm,cnt_gils_norm_p,cnt_pl_pu,cnt_pl_pu_p ");
            sql.Append(" From " + cur_pref + "_data.counters_correct a ");
            sql.Append(" left outer join " + cur_pref + "_kernel.services s ");
            sql.Append(" left outer join " + cur_pref + "_kernel.s_type_alg ta ");
            sql.Append(" left outer join " + cur_pref + "_kernel.s_type_alg ta_p ");
            sql.Append(" Where a.nzp_serv = s.nzp_serv and a.nzp_type_alg = ta.nzp_type_alg and a.nzp_type_alg_p = ta_p.nzp_type_alg");
            sql.Append(whereString);
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (ret.result)
            {
                sql = new StringBuilder();
                sql.Append("Insert into " + tXXOdn + " (pref, nzp_dom, nzp_serv, service, dat_month, dat_charge)");
                sql.Append("select distinct pref, nzp_dom, nzp_serv, service, dat_charge, dat_charge from " + tXXOdn + " a " +
                    " Where a.dat_charge not in (select dat_month from " + tXXOdn + " b where b.pref = a.pref and b.nzp_dom = a.nzp_dom and b.nzp_serv = a.nzp_serv) ");
                ExecSQL(conn_web, sql.ToString(), true);
                sql = new StringBuilder();
                sql.Append("Insert into " + tXXOdn + " (pref, nzp_dom, nzp_serv, service, dat_month, dat_charge)");
                sql.Append("select distinct pref, nzp_dom, nzp_serv, service, dat_month, dat_month from " + tXXOdn + " a " +
                    " Where a.dat_month not in (select dat_charge from " + tXXOdn + " b where b.pref = a.pref and b.nzp_dom = a.nzp_dom and b.nzp_serv = a.nzp_serv and b.dat_month = a.dat_month) ");
                ExecSQL(conn_web, sql.ToString(), true);
            }
#else
            string Insert = " Insert into " + tXXOdn_full +
                    " ( pref, nzp_correct, dat_month,dat_charge,nzp_dom,nzp_counter, nzp_serv, service, is_gkal,rval_real,rval_real_p,rval,rval_p,rvaldlt,rvaldlt_p," +
                    " dnow,dnow_p,dpred,dpred_p,rval_now,rval_now_p,rval_pred,rval_pred_p,cnt_ls_val,cnt_ls_val_p,sum_ls_val,sum_ls_val_p,cnt_ls_norm,"+
                    " cnt_ls_norm_p,sum_ls_norm,sum_ls_norm_p,cnt_ls_25val,cnt_ls_25val_p,sum_ls_25val,sum_ls_25val_p,cnt_ls_25norm,cnt_ls_25norm_p,"+
                    " sum_ls_25norm,sum_ls_25norm_p,cnt_ls_221val,cnt_ls_221val_p,sum_ls_221val,sum_ls_221val_p,cnt_ls_221norm,cnt_ls_221norm_p,sum_ls_221norm,"+
                    " sum_ls_221norm_p,cnt_ls_210val,cnt_ls_210val_p,sum_ls_210val,sum_ls_210val_p,cnt_ls_210norm,cnt_ls_210norm_p,sum_ls_210norm,sum_ls_210norm_p,"+
                    " cnt_gils,cnt_gils_p,cnt_gils_pu,cnt_gils_pu_p,cnt_pl_norm,cnt_pl_norm_p,nzp_type_alg, ntalg_short, nzp_type_alg_p, ntalg_p_short, is_uchet,nzp_user,dat_when," +
                    " cnt_gils_norm,cnt_gils_norm_p,cnt_pl_pu,cnt_pl_pu_p ) ";
            // загрузка исходных начислений и будущих перерасчетов
            sql = new StringBuilder();
            sql.Append(Insert);
            sql.Append(" Select '"+finder.pref+"', nzp_correct,dat_month,dat_charge,nzp_dom,nzp_counter, a.nzp_serv, s.service, is_gkal,rval_real,rval_real_p,rval,rval_p,rvaldlt,rvaldlt_p,"+
                " dnow,dnow_p,dpred,dpred_p,rval_now,rval_now_p,rval_pred,rval_pred_p,cnt_ls_val,cnt_ls_val_p,sum_ls_val,sum_ls_val_p,cnt_ls_norm,"+
                " cnt_ls_norm_p,sum_ls_norm,sum_ls_norm_p,cnt_ls_25val,cnt_ls_25val_p,sum_ls_25val,sum_ls_25val_p,cnt_ls_25norm,cnt_ls_25norm_p,"+
                " sum_ls_25norm,sum_ls_25norm_p,cnt_ls_221val,cnt_ls_221val_p,sum_ls_221val,sum_ls_221val_p,cnt_ls_221norm,cnt_ls_221norm_p,sum_ls_221norm,"+
                " sum_ls_221norm_p,cnt_ls_210val,cnt_ls_210val_p,sum_ls_210val,sum_ls_210val_p,cnt_ls_210norm,cnt_ls_210norm_p,sum_ls_210norm,sum_ls_210norm_p,"+
                " cnt_gils,cnt_gils_p,cnt_gils_pu,cnt_gils_pu_p,cnt_pl_norm,cnt_pl_norm_p, a.nzp_type_alg, ta.name_short, a.nzp_type_alg_p, ta_p.name_short, is_uchet,nzp_user,dat_when," +
                " cnt_gils_norm,cnt_gils_norm_p,cnt_pl_pu,cnt_pl_pu_p ");
            sql.Append(" From " + cur_pref + "_data:counters_correct a ");
            sql.Append(" , outer " + cur_pref + "_kernel:services s ");
            sql.Append(" , outer " + cur_pref + "_kernel:s_type_alg ta ");
            sql.Append(" , outer " + cur_pref + "_kernel:s_type_alg ta_p ");
            sql.Append(" Where a.nzp_serv = s.nzp_serv and a.nzp_type_alg = ta.nzp_type_alg and a.nzp_type_alg_p = ta_p.nzp_type_alg");
            sql.Append(whereString);
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (ret.result)
            {
                sql = new StringBuilder();
                sql.Append("Insert into " + tXXOdn + " (pref, nzp_dom, nzp_serv, service, dat_month, dat_charge)");
                sql.Append("select distinct pref, nzp_dom, nzp_serv, service, dat_charge, dat_charge from " + tXXOdn + " a " +
                    " Where a.dat_charge not in (select dat_month from " + tXXOdn + " b where b.pref = a.pref and b.nzp_dom = a.nzp_dom and b.nzp_serv = a.nzp_serv) ");
                ExecSQL(conn_web, sql.ToString(), true);
                sql = new StringBuilder();
                sql.Append("Insert into " + tXXOdn + " (pref, nzp_dom, nzp_serv, service, dat_month, dat_charge)");
                sql.Append("select distinct pref, nzp_dom, nzp_serv, service, dat_month, dat_month from " + tXXOdn + " a " +
                    " Where a.dat_month not in (select dat_charge from " + tXXOdn + " b where b.pref = a.pref and b.nzp_dom = a.nzp_dom and b.nzp_serv = a.nzp_serv and b.dat_month = a.dat_month) ");
                ExecSQL(conn_web, sql.ToString(), true);
            }
#endif
            conn_db.Close(); //закрыть соединение с основной базой

            //далее работаем с кешем
            //создаем индексы на tXX_charge
            if (isTableCreated)
            {
                string ix = "ix" + finder.nzp_user.ToString() + "_cntcor";
                ret = ExecSQL(conn_web, " Create index " + ix + "_1 on " + tXXOdn + " (dat_month, dat_charge, nzp_dom, nzp_serv, pref) ", false);
            }
#if PG
            if (ret.result) ret = ExecSQL(conn_web, " analyze  " + tXXOdn, true);
#else
            if (ret.result) ret = ExecSQL(conn_web, " Update statistics for table  " + tXXOdn, true);
#endif
            conn_web.Close();

            return;

        } //FindOdn

        
    }

}
